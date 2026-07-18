$ErrorActionPreference = "Stop"

function Get-FileSha256([string]$Path) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        return [System.BitConverter]::ToString($sha256.ComputeHash($stream)).Replace("-", "")
    }
    finally {
        $stream.Dispose()
        $sha256.Dispose()
    }
}

function Assert-SourceIconUnchanged([string]$Path, [string]$ExpectedHash, [datetime]$ExpectedLastWriteTimeUtc) {
    $iconFile = Get-Item -LiteralPath $Path
    if ((Get-FileSha256 $Path) -ne $ExpectedHash -or $iconFile.LastWriteTimeUtc -ne $ExpectedLastWriteTimeUtc) {
        throw "Build or packaging must not overwrite the tracked application icon: $Path"
    }
}

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\KidsTraining.App\KidsTraining.App.csproj"
$publishDir = Join-Path $root "src\KidsTraining.App\bin\Release\net9.0-windows\win-x64\publish"
$artifactsPublishDir = Join-Path $root "artifacts\publish\win-x64"
$msiPath = Join-Path $root "artifacts\KidsTraining.msi"
$generatedWxs = Join-Path $root "artifacts\obj\installer\KidsTraining.generated.wxs"
$decompiledDir = Join-Path $root "artifacts\msi-decompiled"
$decompiledWxs = Join-Path $decompiledDir "KidsTraining.wxs"
$sourceIcon = Join-Path (Split-Path -Parent $project) "app.ico"
if (!(Test-Path -LiteralPath $sourceIcon -PathType Leaf)) {
    throw "Missing tracked application icon: $sourceIcon"
}
$sourceIconFile = Get-Item -LiteralPath $sourceIcon
$sourceIconHash = Get-FileSha256 $sourceIcon
$sourceIconLastWriteTimeUtc = $sourceIconFile.LastWriteTimeUtc
[xml]$projectDocument = Get-Content -Raw -Encoding UTF8 $project
$versionNode = $projectDocument.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Application version was not found in project file: $project"
}
$version = $versionNode.InnerText.Trim()

$architectureSourceRoot = Join-Path $root "src\KidsTraining.App"
$programSource = Get-Content -Raw -Encoding UTF8 (Join-Path $root "src\KidsTraining.App\Program.cs")
$traySource = Get-Content -Raw -Encoding UTF8 (Join-Path $root "src\KidsTraining.App\Presentation\WinForms\TrayApplicationContext.cs")
$updateSource = [string]::Join(
    "`n",
    @(Get-ChildItem -Path (Join-Path $architectureSourceRoot "Application\Updates"), (Join-Path $architectureSourceRoot "Domain\Updates"), (Join-Path $architectureSourceRoot "Infrastructure\Updates") -Filter "*.cs" -File -Recurse |
        Sort-Object FullName |
        ForEach-Object { Get-Content -Raw -Encoding UTF8 $_.FullName }))
$parentSource = [string]::Join(
    "`n",
    @(Get-ChildItem -Path (Join-Path $architectureSourceRoot "Infrastructure\ParentControl") -Filter "*.cs" -File -Recurse |
        Sort-Object FullName |
        ForEach-Object { Get-Content -Raw -Encoding UTF8 $_.FullName }))
$parentSettingsSource = [string]::Join(
    "`n",
    @(Get-ChildItem -Path (Join-Path $architectureSourceRoot "Application\ParentControl"), (Join-Path $architectureSourceRoot "Domain\ParentControl"), (Join-Path $architectureSourceRoot "Infrastructure\Settings") -Filter "*.cs" -File -Recurse |
        Sort-Object FullName |
        ForEach-Object { Get-Content -Raw -Encoding UTF8 $_.FullName }))
$learningSourceDir = Join-Path $root "kids-training"
$htmlTemplateSource = Join-Path $learningSourceDir "index.template.html"
$appDefinitionSource = Join-Path $learningSourceDir "app\learning-app.dc.html"
$runtimeScriptSource = Join-Path $learningSourceDir "scripts\runtime.js"
$fontCssSource = Join-Path $learningSourceDir "styles\fonts.css"
$fontSourceDir = Join-Path $learningSourceDir "fonts"
$rootFullPath = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
$publishFullPath = [System.IO.Path]::GetFullPath($publishDir)
$coreSourceFiles = @(Get-ChildItem -Path (Join-Path $architectureSourceRoot "Domain"), (Join-Path $architectureSourceRoot "Application") -Filter "*.cs" -File -Recurse)

if (!$publishFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish directory must stay inside the repository: $publishFullPath"
}
if (Test-Path $publishFullPath) {
    Remove-Item -LiteralPath $publishFullPath -Recurse -Force
}

if (!(Test-Path $htmlTemplateSource) -or !(Test-Path $appDefinitionSource) -or !(Test-Path $runtimeScriptSource) -or !(Test-Path $fontCssSource)) {
    throw "Split learning source assets are incomplete"
}
if (Test-Path (Join-Path $root "kids-training.html")) {
    throw "The legacy single-file kids-training.html bundle must not remain"
}
if (Test-Path (Join-Path $architectureSourceRoot "RuntimeHtmlPreparer.cs")) {
    throw "The legacy RuntimeHtmlPreparer must be replaced by the learning application boundary"
}
foreach ($coreSourceFile in $coreSourceFiles) {
    $coreSource = Get-Content -Raw -Encoding UTF8 $coreSourceFile.FullName
    if ($coreSource -match "System\.Windows\.Forms|Microsoft\.Web\.WebView2|\bHttpClient\b|\bProcess\.|\bFile\.|\bDirectory\.") {
        throw "Core layer has an infrastructure dependency: $($coreSourceFile.FullName)"
    }
}
$htmlTemplateText = Get-Content -Raw -Encoding UTF8 $htmlTemplateSource
$fontCssText = Get-Content -Raw -Encoding UTF8 $fontCssSource
$fontFiles = @(Get-ChildItem -Path $fontSourceDir -Filter "*.woff2" -File)
if ($htmlTemplateText -notmatch [regex]::Escape("<!--__KIDS_TRAINING_APP__-->") -or
    $htmlTemplateText -notmatch [regex]::Escape('kids-training/scripts/runtime.js') -or
    $htmlTemplateText -notmatch [regex]::Escape('kids-training/styles/fonts.css') -or
    $htmlTemplateText -match "__bundler/manifest" -or
    $fontFiles.Count -ne 366) {
    throw "Split learning source structure is invalid"
}
$fontReferences = [regex]::Matches($fontCssText, 'url\("\.\./fonts/([^"\)]+\.woff2)"\)')
if ($fontReferences.Count -ne $fontFiles.Count) {
    throw "Font CSS reference count does not match the extracted font files"
}
foreach ($fontReference in $fontReferences) {
    if (!(Test-Path (Join-Path $fontSourceDir $fontReference.Groups[1].Value))) {
        throw "Missing referenced font: $($fontReference.Groups[1].Value)"
    }
}

if ($programSource -notmatch "TrayApplicationContext" -or $programSource -notmatch "--training" -or $programSource -notmatch "--auto-training" -or $programSource -notmatch "--apply-update") {
    throw "Program entry point must support tray, training, and update-runner modes"
}
if ($traySource -notmatch "TimeSpan.FromHours\(1\)" -or $traySource -notmatch "NotifyIcon") {
    throw "Tray application context must check updates hourly from the notification area"
}

$updateStartedIndex = $traySource.IndexOf("case UpdateCheckStatus.UpdateStarted:")
$nextCaseIndex = $traySource.IndexOf("case UpdateCheckStatus.NoUpdate", $updateStartedIndex)
if ($updateStartedIndex -lt 0 -or $nextCaseIndex -le $updateStartedIndex) {
    throw "Tray application context must handle update-started state explicitly"
}
$updateStartedBlock = $traySource.Substring($updateStartedIndex, $nextCaseIndex - $updateStartedIndex)
if ($updateStartedBlock -match "ShowBalloon") {
    throw "Automatic update start must not show a user-facing notification"
}

if ($updateSource -notmatch "releases/latest" -or $updateSource -notmatch "KidsTraining.msi" -or $updateSource -notmatch "UpdateRunner") {
    throw "Update manager must check GitHub Releases and launch a copied update runner"
}
if ($traySource -notmatch "ParentControlServer" -or $traySource -notmatch "保護者画面URLをコピー" -or $traySource -notmatch "StartTrainingFromParentControl" -or $traySource -notmatch "ReturnToComputerFromParentControl") {
    throw "Tray application context must expose parent remote controls"
}
if ($parentSource -notmatch "TcpListener" -or $parentSource -notmatch "IPAddress.Any" -or $parentSource -notmatch "DefaultPort = 44567" -or $parentSource -notmatch "IsAllowedRemoteAddress" -or $parentSource -notmatch "Kids Training 保護者画面" -or $parentSource -notmatch "/api/start" -or $parentSource -notmatch "/api/return" -or $parentSource -notmatch "/api/password" -or $parentSource -notmatch "パスワードを変更") {
    throw "Parent control server must listen on LAN and expose start/return/password controls"
}
if ($parentSettingsSource -notmatch "parentPassword" -or $parentSettingsSource -notmatch "ParentPasswordService" -or $parentSettingsSource -notmatch "ParentPin.TryCreate" -or $parentSettingsSource -notmatch "File.Move\(tempPath, AppPaths.ParentSettingsPath, overwrite: true\)") {
    throw "Parent settings must persist a configurable 4-digit parent password"
}
if ($programSource -notmatch "ParentControlServer.BuildParentPage" -or $programSource -notmatch "192.168.1.10" -or $programSource -notmatch "8.8.8.8" -or $programSource -notmatch "ParentPin.TryCreate") {
    throw "Smoke test must validate parent control page, password validation, and LAN address filtering"
}
# Generated learning behavior is validated once by the shared runtime contract in
# the architecture harness and again by the published executable smoke tests below.
$architectureTests = Join-Path $root "tests\KidsTraining.ArchitectureTests\KidsTraining.ArchitectureTests.csproj"
& dotnet run --project $architectureTests -c Release -- $root
if ($LASTEXITCODE -ne 0) {
    throw "Architecture tests failed with exit code $LASTEXITCODE"
}

& dotnet publish $project -c Release -r win-x64 --self-contained true /p:Version=$version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}
Assert-SourceIconUnchanged $sourceIcon $sourceIconHash $sourceIconLastWriteTimeUtc

$publishedExe = Join-Path $publishDir "KidsTraining.App.exe"
$publishedLearningDir = Join-Path $publishDir "assets\kids-training"
$publishedHtml = Join-Path $publishedLearningDir "index.template.html"
$publishedAppDefinition = Join-Path $publishedLearningDir "app\learning-app.dc.html"
$publishedRuntimeScript = Join-Path $publishedLearningDir "scripts\runtime.js"
$publishedFonts = Join-Path $publishedLearningDir "fonts"
$publishedFavicon = Join-Path $publishDir "assets\favicon.ico"
if (!(Test-Path $publishedExe)) {
    throw "Missing published executable: $publishedExe"
}
if (!(Test-Path $publishedHtml)) {
    throw "Missing published HTML asset: $publishedHtml"
}
if (Test-Path (Join-Path $publishDir "assets\kids-training.html")) {
    throw "Legacy single-file HTML remains in the publish output"
}
if (Test-Path (Join-Path $publishedLearningDir "CLAUDE.md")) {
    throw "Repository guidance leaked into published learning assets"
}
if (!(Test-Path $publishedAppDefinition) -or !(Test-Path $publishedRuntimeScript) -or
    @(Get-ChildItem -Path $publishedFonts -Filter "*.woff2" -File).Count -ne 366) {
    throw "Published split learning assets are incomplete"
}
if (!(Test-Path $publishedFavicon)) {
    throw "Missing published favicon asset: $publishedFavicon"
}
if ((Get-FileSha256 $publishedFavicon) -ne $sourceIconHash) {
    throw "Published favicon must be copied from the tracked application icon"
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
Assert-SourceIconUnchanged $sourceIcon $sourceIconHash $sourceIconLastWriteTimeUtc

if (!(Test-Path $msiPath)) {
    throw "Missing MSI artifact: $msiPath"
}
if (!(Test-Path (Join-Path $artifactsPublishDir "KidsTraining.App.exe"))) {
    throw "Missing artifacts publish executable"
}
if (!(Test-Path (Join-Path $artifactsPublishDir "assets\kids-training\index.template.html")) -or
    !(Test-Path (Join-Path $artifactsPublishDir "assets\kids-training\app\learning-app.dc.html")) -or
    !(Test-Path (Join-Path $artifactsPublishDir "assets\kids-training\scripts\runtime.js"))) {
    throw "Missing artifacts publish learning assets"
}
if (Test-Path (Join-Path $artifactsPublishDir "assets\kids-training.html")) {
    throw "Legacy single-file HTML remains in the artifacts publish output"
}
if (Test-Path (Join-Path $artifactsPublishDir "assets\kids-training\CLAUDE.md")) {
    throw "Repository guidance leaked into artifacts publish output"
}
$artifactsFavicon = Join-Path $artifactsPublishDir "assets\favicon.ico"
if (!(Test-Path $artifactsFavicon)) {
    throw "Missing artifacts publish favicon"
}
if ((Get-FileSha256 $artifactsFavicon) -ne $sourceIconHash) {
    throw "Artifacts publish favicon must be copied from the tracked application icon"
}

$artifactsSmoke = Start-Process -FilePath (Join-Path $artifactsPublishDir "KidsTraining.App.exe") -ArgumentList "--smoke-test" -Wait -PassThru
if ($artifactsSmoke.ExitCode -ne 0) {
    throw "Artifacts smoke test failed with exit code $($artifactsSmoke.ExitCode)"
}

$generatedText = Get-Content -Raw -Encoding UTF8 $generatedWxs
if ($generatedText -match "ProgramFilesFolder") {
    throw "Generated MSI source must not reference ProgramFilesFolder"
}
if ($generatedText -notmatch "LocalAppDataFolder") {
    throw "Generated MSI source must reference LocalAppDataFolder"
}
if ($generatedText -notmatch [regex]::Escape("Software\Microsoft\Windows\CurrentVersion\Run")) {
    throw "Generated MSI source must register HKCU Run startup"
}
if ($generatedText -notmatch "--auto-training") {
    throw "Generated MSI source must start fullscreen learning after login"
}
if ($generatedText -notmatch "--training") {
    throw "Generated MSI source must include a learning-mode shortcut"
}
[xml]$generatedDocument = $generatedText
$wixNamespace = New-Object System.Xml.XmlNamespaceManager($generatedDocument.NameTable)
$wixNamespace.AddNamespace("wix", "http://wixtoolset.org/schemas/v4/wxs")
$generatedIcon = $generatedDocument.SelectSingleNode("/wix:Wix/wix:Package/wix:Icon[@Id='AppIcon.ico']", $wixNamespace)
if ($null -eq $generatedIcon -or
    !([System.IO.Path]::GetFullPath($generatedIcon.SourceFile)).Equals(
        [System.IO.Path]::GetFullPath($sourceIcon),
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Generated MSI source must reuse the tracked application icon"
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
