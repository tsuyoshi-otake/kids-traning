$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\KidsTraining.App\KidsTraining.App.csproj"
$publishDir = Join-Path $root "src\KidsTraining.App\bin\Release\net9.0-windows\win-x64\publish"
$artifactsPublishDir = Join-Path $root "artifacts\publish\win-x64"
$msiPath = Join-Path $root "artifacts\KidsTraining.msi"
$generatedWxs = Join-Path $root "artifacts\obj\installer\KidsTraining.generated.wxs"
$decompiledDir = Join-Path $root "artifacts\msi-decompiled"
$decompiledWxs = Join-Path $decompiledDir "KidsTraining.wxs"
$version = "1.1.0"
$programSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\Program.cs")
$traySource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\TrayApplicationContext.cs")
$updateSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\UpdateManager.cs")

if ($programSource -notmatch "TrayApplicationContext" -or $programSource -notmatch "--training" -or $programSource -notmatch "--apply-update") {
    throw "Program entry point must support tray, training, and update-runner modes"
}
if ($traySource -notmatch "TimeSpan.FromHours\(1\)" -or $traySource -notmatch "NotifyIcon") {
    throw "Tray application context must check updates hourly from the notification area"
}
if ($updateSource -notmatch "releases/latest" -or $updateSource -notmatch "KidsTraining.msi" -or $updateSource -notmatch "UpdateRunner") {
    throw "Update manager must check GitHub Releases and launch a copied update runner"
}

& dotnet publish $project -c Release -r win-x64 --self-contained true /p:Version=$version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedExe = Join-Path $publishDir "KidsTraining.App.exe"
$publishedHtml = Join-Path $publishDir "assets\kids-training.html"
$publishedFavicon = Join-Path $publishDir "assets\favicon.ico"
if (!(Test-Path $publishedExe)) {
    throw "Missing published executable: $publishedExe"
}
if (!(Test-Path $publishedHtml)) {
    throw "Missing published HTML asset: $publishedHtml"
}
if (!(Test-Path $publishedFavicon)) {
    throw "Missing published favicon asset: $publishedFavicon"
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Icon]::ExtractAssociatedIcon($publishedExe)
if ($null -eq $icon) {
    throw "Published executable does not have an associated icon"
}
$icon.Dispose()

$smoke = Start-Process -FilePath $publishedExe -ArgumentList "--smoke-test" -Wait -PassThru
if ($smoke.ExitCode -ne 0) {
    throw "Smoke test failed with exit code $($smoke.ExitCode)"
}

& (Join-Path $root "scripts\build-msi.ps1") -Version $version
if ($LASTEXITCODE -ne 0) {
    throw "MSI build failed with exit code $LASTEXITCODE"
}

if (!(Test-Path $msiPath)) {
    throw "Missing MSI artifact: $msiPath"
}
if (!(Test-Path (Join-Path $artifactsPublishDir "KidsTraining.App.exe"))) {
    throw "Missing artifacts publish executable"
}
if (!(Test-Path (Join-Path $artifactsPublishDir "assets\kids-training.html"))) {
    throw "Missing artifacts publish HTML"
}
if (!(Test-Path (Join-Path $artifactsPublishDir "assets\favicon.ico"))) {
    throw "Missing artifacts publish favicon"
}

$artifactsSmoke = Start-Process -FilePath (Join-Path $artifactsPublishDir "KidsTraining.App.exe") -ArgumentList "--smoke-test" -Wait -PassThru
if ($artifactsSmoke.ExitCode -ne 0) {
    throw "Artifacts smoke test failed with exit code $($artifactsSmoke.ExitCode)"
}

$generatedText = Get-Content -Raw $generatedWxs
if ($generatedText -match "ProgramFilesFolder") {
    throw "Generated MSI source must not reference ProgramFilesFolder"
}
if ($generatedText -notmatch "LocalAppDataFolder") {
    throw "Generated MSI source must reference LocalAppDataFolder"
}
if ($generatedText -notmatch [regex]::Escape("Software\Microsoft\Windows\CurrentVersion\Run")) {
    throw "Generated MSI source must register HKCU Run startup"
}
if ($generatedText -notmatch "--training") {
    throw "Generated MSI source must include a learning-mode shortcut"
}
if ($generatedText -notmatch "AppIcon.ico") {
    throw "Generated MSI source must include the application icon"
}

& wix msi validate $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "wix msi validate failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Force -Path $decompiledDir | Out-Null
if (Test-Path $decompiledWxs) {
    Remove-Item $decompiledWxs -Force
}

& wix msi decompile $msiPath -o $decompiledWxs
if ($LASTEXITCODE -ne 0) {
    throw "wix msi decompile failed with exit code $LASTEXITCODE"
}
if (!(Test-Path $decompiledWxs)) {
    throw "Decompiled WXS was not created"
}

Write-Host "Verification passed."
