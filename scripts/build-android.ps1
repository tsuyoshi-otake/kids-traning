#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the drill-only Android APK without Gradle.

.DESCRIPTION
  Pipeline:
    1. KidsTraining.RuntimeExport regenerates kids-training.runtime.html from the
       shared learning sources and applies the mobile drill-only transform.
    2. The runtime page plus the kids-training asset folder are staged into
       android/app/src/main/assets.
    3. aapt2 compiles + links resources/manifest/assets into an unsigned APK.
    4. javac compiles MainActivity, d8 dexes it, classes.dex is stored
       uncompressed into the APK.
    5. zipalign + apksigner (debug keystore, generated on first run).

  Requires: .NET SDK on PATH, JDK (javac/jar/keytool) on PATH, Python on PATH,
  and an Android SDK at ANDROID_HOME or %LOCALAPPDATA%\Android\Sdk with a
  platform and build-tools installed.
#>
# -ProfileName defaults to the exporter's built-in name. A Japanese literal in this
# file would be misread by Windows PowerShell 5.1 without a BOM, so the parameter
# stays empty and the C# default applies unless the caller passes one.
param(
    [string]$ProfileName = "",
    [string]$OutputApk = "",
    [switch]$SkipExport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidDir = Join-Path $repoRoot "android"
$mainDir = Join-Path $androidDir "app\src\main"
$assetsDir = Join-Path $mainDir "assets"
$buildDir = Join-Path $androidDir "build"
if ([string]::IsNullOrWhiteSpace($OutputApk)) {
    $OutputApk = Join-Path $repoRoot "artifacts\KidsTrainingDrill.apk"
}

function Fail([string]$Message) { Write-Error $Message; exit 1 }

function Resolve-JavaTool([string]$Name) {
    $cmd = Get-Command "$Name.exe" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME "bin\$Name.exe"))) {
        return (Join-Path $env:JAVA_HOME "bin\$Name.exe")
    }
    Fail "$Name.exe was not found on PATH and JAVA_HOME is not set"
}

# --- Release version ----------------------------------------------------------
# The APK ships with the Windows release, so it takes its version from the app project's
# <Version>: one bump versions both builds. versionCode = major*10000 + minor*100 + patch
# grows with every release as long as minor and patch stay below 100.
$appProject = Join-Path $repoRoot "src\KidsTraining.App\KidsTraining.App.csproj"
$versionMatch = Select-String -Path $appProject -Pattern '<Version>(\d+)\.(\d+)\.(\d+)</Version>' | Select-Object -First 1
if (!$versionMatch) { Fail "<Version>X.Y.Z</Version> not found in $appProject" }
$versionGroups = $versionMatch.Matches[0].Groups
$versionMajor = [int]$versionGroups[1].Value
$versionMinor = [int]$versionGroups[2].Value
$versionPatch = [int]$versionGroups[3].Value
if ($versionMinor -ge 100 -or $versionPatch -ge 100) {
    Fail "Version $versionMajor.$versionMinor.$versionPatch does not fit the versionCode scheme (minor and patch must be below 100)"
}
$versionName = "$versionMajor.$versionMinor.$versionPatch"
$versionCode = $versionMajor * 10000 + $versionMinor * 100 + $versionPatch

# --- Locate the Android SDK -------------------------------------------------
$sdkRoot = $env:ANDROID_HOME
if ([string]::IsNullOrWhiteSpace($sdkRoot)) { $sdkRoot = $env:ANDROID_SDK_ROOT }
if ([string]::IsNullOrWhiteSpace($sdkRoot)) {
    $sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}
if (!(Test-Path $sdkRoot)) { Fail "Android SDK not found (checked ANDROID_HOME, ANDROID_SDK_ROOT, $sdkRoot)" }

$buildToolsDir = Get-ChildItem (Join-Path $sdkRoot "build-tools") -Directory -ErrorAction SilentlyContinue |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$buildToolsDir) { Fail "No build-tools found under $sdkRoot\build-tools" }
$buildTools = $buildToolsDir.FullName

$platformDir = Get-ChildItem (Join-Path $sdkRoot "platforms") -Directory -ErrorAction SilentlyContinue |
    Sort-Object { [int]($_.Name -replace 'android-', '') } -Descending | Select-Object -First 1
if (!$platformDir) { Fail "No platforms found under $sdkRoot\platforms" }
$androidJar = Join-Path $platformDir.FullName "android.jar"
if (!(Test-Path $androidJar)) { Fail "android.jar missing: $androidJar" }

$aapt2 = Join-Path $buildTools "aapt2.exe"
$d8 = Join-Path $buildTools "d8.bat"
$zipalign = Join-Path $buildTools "zipalign.exe"
$apksigner = Join-Path $buildTools "apksigner.bat"
foreach ($tool in @($aapt2, $d8, $zipalign, $apksigner)) {
    if (!(Test-Path $tool)) { Fail "Missing build tool: $tool" }
}

$javac = Resolve-JavaTool "javac"
$keytool = Resolve-JavaTool "keytool"
$pythonCmd = Get-Command "python.exe" -ErrorAction SilentlyContinue
$python = if ($pythonCmd) { $pythonCmd.Source } else { $null }
if (!$python) { Fail "python.exe not found on PATH (needed to store classes.dex uncompressed)" }

# d8.bat and apksigner.bat honor JAVA_HOME, which points at a missing directory on
# some machines. When it is invalid, pin it to the JDK that javac resolved from.
if ([string]::IsNullOrWhiteSpace($env:JAVA_HOME) -or !(Test-Path (Join-Path $env:JAVA_HOME "bin\java.exe"))) {
    $env:JAVA_HOME = Split-Path (Split-Path $javac)
    Write-Host "JAVA_HOME was invalid; using $env:JAVA_HOME"
}

Write-Host "Version:  $versionName (versionCode $versionCode)"
Write-Host "SDK:      $sdkRoot"
Write-Host "Platform: $($platformDir.Name)   Build-tools: $($buildToolsDir.Name)"

# --- 1. Generate the drill-only runtime page --------------------------------
$runtimeHtml = Join-Path $assetsDir "kids-training.runtime.html"
New-Item -ItemType Directory -Force $assetsDir | Out-Null
if (!$SkipExport) {
    Write-Host "`n[1/6] Generating drill-only learning runtime..."
    # The profile name is Japanese; console argument marshaling mangles non-ASCII text,
    # so the exporter reads it from the environment instead (see Program.cs).
    if (![string]::IsNullOrWhiteSpace($ProfileName)) {
        $env:KT_EXPORT_PROFILE_NAME = $ProfileName
    }
    & dotnet run --project (Join-Path $repoRoot "tools\KidsTraining.RuntimeExport\KidsTraining.RuntimeExport.csproj") `
        -c Release -- $repoRoot $runtimeHtml --mode drill-only
    if ($LASTEXITCODE -ne 0) { Fail "Runtime export failed" }
} elseif (!(Test-Path $runtimeHtml)) {
    Fail "-SkipExport was given but $runtimeHtml does not exist"
}

# --- 2. Stage the kids-training web assets ----------------------------------
Write-Host "`n[2/6] Staging kids-training assets..."
$assetLearningDir = Join-Path $assetsDir "kids-training"
$sourceLearningDir = Join-Path $repoRoot "kids-training"
# /MIR keeps the staged copy exact; CLAUDE.md matches the Windows app's exclusion.
& robocopy $sourceLearningDir $assetLearningDir /MIR /XF "CLAUDE.md" /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { Fail "robocopy failed with exit code $LASTEXITCODE" }

# --- 3. Compile resources + link base APK -----------------------------------
Write-Host "`n[3/6] aapt2 compile + link..."
New-Item -ItemType Directory -Force $buildDir | Out-Null
$compiledRes = Join-Path $buildDir "res.zip"
& $aapt2 compile --dir (Join-Path $mainDir "res") -o $compiledRes
if ($LASTEXITCODE -ne 0) { Fail "aapt2 compile failed" }

$baseApk = Join-Path $buildDir "base.apk"
$genDir = Join-Path $buildDir "gen"
New-Item -ItemType Directory -Force $genDir | Out-Null
& $aapt2 link -o $baseApk -I $androidJar `
    --manifest (Join-Path $mainDir "AndroidManifest.xml") `
    --version-code $versionCode --version-name $versionName --replace-version `
    --java $genDir -A $assetsDir $compiledRes
if ($LASTEXITCODE -ne 0) { Fail "aapt2 link failed" }

# --- 4. Compile Java + dex ---------------------------------------------------
Write-Host "`n[4/6] javac + d8..."
$classesDir = Join-Path $buildDir "classes"
$dexDir = Join-Path $buildDir "dex"
New-Item -ItemType Directory -Force $classesDir | Out-Null
New-Item -ItemType Directory -Force $dexDir | Out-Null
$javaSources = (Get-ChildItem (Join-Path $mainDir "java") -Recurse -Filter *.java | ForEach-Object FullName) +
    (Get-ChildItem $genDir -Recurse -Filter *.java | ForEach-Object FullName)
& $javac --release 8 -encoding UTF-8 -classpath $androidJar -d $classesDir $javaSources
if ($LASTEXITCODE -ne 0) { Fail "javac failed" }

# The Windows commit limit can be nearly exhausted even with free RAM, which kills
# JVMs asking for their default 1 GB heap. Constrain the JVM and retry once: d8 for a
# single-class app needs almost nothing.
$d8Args = @("-JXmx256m", "--release", "--min-api", "26", "--lib", $androidJar,
    "--output", $dexDir) + (Get-ChildItem $classesDir -Recurse -Filter *.class | ForEach-Object FullName)
$savedJavaOpts = $env:JAVA_TOOL_OPTIONS
$env:JAVA_TOOL_OPTIONS = "-XX:TieredStopAtLevel=1 -XX:CICompilerCount=1"
try {
    & $d8 @d8Args
    if ($LASTEXITCODE -ne 0) {
        Write-Host "d8 failed once; retrying..."
        Start-Sleep -Seconds 2
        & $d8 @d8Args
    }
} finally {
    $env:JAVA_TOOL_OPTIONS = $savedJavaOpts
}
if ($LASTEXITCODE -ne 0) { Fail "d8 failed" }

# --- 5. Merge classes.dex (stored) + zipalign --------------------------------
Write-Host "`n[5/6] Merging classes.dex + zipalign..."
$dexPath = Join-Path $dexDir "classes.dex"
if (!(Test-Path $dexPath)) { Fail "d8 did not produce classes.dex" }
$mergedApk = Join-Path $buildDir "merged.apk"
$env:KT_BASE_APK = $baseApk
$env:KT_DEX = $dexPath
$env:KT_MERGED = $mergedApk
& $python -c @"
import os, shutil, zipfile
base, dex, merged = os.environ['KT_BASE_APK'], os.environ['KT_DEX'], os.environ['KT_MERGED']
with zipfile.ZipFile(base) as src, zipfile.ZipFile(merged, 'w') as dst:
    for item in src.infolist():
        if item.filename == 'classes.dex':
            continue
        dst.writestr(item, src.read(item.filename))
    dst.write(dex, 'classes.dex', zipfile.ZIP_STORED)
"@
if ($LASTEXITCODE -ne 0) { Fail "classes.dex merge failed" }

$alignedApk = Join-Path $buildDir "aligned.apk"
& $zipalign -f -p 4 $mergedApk $alignedApk
if ($LASTEXITCODE -ne 0) { Fail "zipalign failed" }

# --- 6. Sign ------------------------------------------------------------------
Write-Host "`n[6/6] Signing..."
$keystore = Join-Path $androidDir "debug.keystore"
if (!(Test-Path $keystore)) {
    & $keytool -genkeypair -v -keystore $keystore -alias androiddebugkey `
        -storepass android -keypass android -keyalg RSA -keysize 2048 `
        -validity 10957 -dname "CN=KidsTraining Debug,O=KidsTraining,C=JP"
    if ($LASTEXITCODE -ne 0) { Fail "keytool failed" }
}

New-Item -ItemType Directory -Force (Split-Path $OutputApk) | Out-Null
$savedJavaOpts = $env:JAVA_TOOL_OPTIONS
$env:JAVA_TOOL_OPTIONS = "-XX:TieredStopAtLevel=1 -XX:CICompilerCount=1"
try {
    & $apksigner -JXmx256m sign --ks $keystore --ks-pass pass:android --key-pass pass:android `
        --out $OutputApk $alignedApk
    if ($LASTEXITCODE -ne 0) {
        Write-Host "apksigner failed once; retrying..."
        Start-Sleep -Seconds 2
        & $apksigner -JXmx256m sign --ks $keystore --ks-pass pass:android --key-pass pass:android `
            --out $OutputApk $alignedApk
    }
    if ($LASTEXITCODE -eq 0) {
        & $apksigner -JXmx256m verify --print-certs $OutputApk | Out-Null
    }
} finally {
    $env:JAVA_TOOL_OPTIONS = $savedJavaOpts
}
if ($LASTEXITCODE -ne 0) { Fail "apksigner failed" }

$size = (Get-Item $OutputApk).Length
Write-Host "`nDone: $OutputApk ($([math]::Round($size / 1MB, 1)) MB)"
