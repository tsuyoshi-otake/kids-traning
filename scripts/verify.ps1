$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\KidsTraining.App\KidsTraining.App.csproj"
$publishDir = Join-Path $root "src\KidsTraining.App\bin\Release\net9.0-windows\win-x64\publish"
$artifactsPublishDir = Join-Path $root "artifacts\publish\win-x64"
$msiPath = Join-Path $root "artifacts\KidsTraining.msi"
$generatedWxs = Join-Path $root "artifacts\obj\installer\KidsTraining.generated.wxs"
$decompiledDir = Join-Path $root "artifacts\msi-decompiled"
$decompiledWxs = Join-Path $decompiledDir "KidsTraining.wxs"
$version = "1.4.3"

$programSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\Program.cs")
$traySource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\TrayApplicationContext.cs")
$updateSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\UpdateManager.cs")
$runtimeSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\RuntimeHtmlPreparer.cs")
$trainingSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\TrainingForm.cs")
$parentSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\ParentControlServer.cs")
$parentSettingsSource = Get-Content -Raw (Join-Path $root "src\KidsTraining.App\ParentSettings.cs")

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
if ($parentSettingsSource -notmatch "parentPassword" -or $parentSettingsSource -notmatch "ChangeParentPassword" -or $parentSettingsSource -notmatch "NormalizePassword" -or $parentSettingsSource -notmatch "File.Move\(tempPath, AppPaths.ParentSettingsPath, overwrite: true\)") {
    throw "Parent settings must persist a configurable 4-digit parent password"
}
if ($programSource -notmatch "ParentControlServer.BuildParentPage" -or $programSource -notmatch "192.168.1.10" -or $programSource -notmatch "8.8.8.8" -or $programSource -notmatch "ParentSettings.NormalizePassword") {
    throw "Smoke test must validate parent control page, password validation, and LAN address filtering"
}
if ($runtimeSource -notmatch "add:\.05" -or $runtimeSource -notmatch "moji:\.05" -or $runtimeSource -notmatch "learningStage\(p\)" -or $runtimeSource -notmatch "effectiveGrade\(p\)" -or $runtimeSource -notmatch "genAdd\(p\)" -or $runtimeSource -notmatch "allowedTopics\(p\)" -or $runtimeSource -notmatch "weakKeys=this\.allowedTopics" -or $runtimeSource -notmatch "profileGrade:this\.gradeLabel") {
    throw "Runtime HTML patch must start beginners at level 1 and stage topic difficulty"
}
if ($runtimeSource -notmatch "PatchArithmeticVisuals" -or $runtimeSource -notmatch "linear-gradient\(135deg,#ffdad4" -or $runtimeSource -notmatch "isMulViz" -or $runtimeSource -notmatch "pickMul\(p\)" -or $runtimeSource -notmatch "op:'div'") {
    throw "Runtime HTML patch must render visual aids for non-hissan arithmetic"
}
if ($runtimeSource -notmatch "pickKokugo\(p\)" -or $runtimeSource -notmatch "subtype:'reading'" -or $runtimeSource -notmatch "subtype:'kanji-choice'" -or $runtimeSource -notmatch "kokuInstruction" -or $runtimeSource -notmatch "g:3") {
    throw "Runtime HTML patch must include grade 1-3 kanji reading and correct-kanji choice prompts"
}
if ($runtimeSource -notmatch "pickMoji\(p\)" -or $runtimeSource -notmatch "subtype:'alphabet'" -or $runtimeSource -notmatch "subtype:'hiragana'" -or $runtimeSource -notmatch "subtype:'katakana'" -or $runtimeSource -notmatch "1cm.*10mm" -or $runtimeSource -notmatch "30mm") {
    throw "Runtime HTML patch must include alphabet, hiragana, katakana, and millimeter questions"
}
if ($runtimeSource -notmatch "PatchRewardSystem" -or $runtimeSource -notmatch "gainXp" -or $runtimeSource -notmatch "xpLevel" -or $runtimeSource -notmatch "fbXp" -or $runtimeSource -notmatch "earnedXp" -or $runtimeSource -notmatch "べんきょうを つづける") {
    throw "Runtime HTML patch must include XP rewards without avatar customization"
}
if ($runtimeSource -match "avatarReady" -or $runtimeSource -match "avatarParts" -or $runtimeSource -match "finishAvatar" -or $runtimeSource -match "BuildAvatarPanelMarkup" -or $runtimeSource -match "アバター") {
    throw "Runtime HTML patch must not include avatar setup or customization"
}
if ($trainingSource -notmatch "beginnerMastery" -or $trainingSource -notmatch "kt_settings_v1" -or $trainingSource -notmatch "hasMeaningfulProgress" -or $trainingSource -notmatch "pass: 8" -or $trainingSource -notmatch "moji" -or $trainingSource -notmatch "xp") {
    throw "Training storage bootstrap must migrate only unstarted profiles to beginner defaults"
}
if ($runtimeSource -notmatch "parentPin\(\)" -or $runtimeSource -notmatch "kt_parent_pin_v1" -or $runtimeSource -notmatch "const ok=np===this.parentPin\(\)") {
    throw "Runtime HTML patch must use the configurable parent password for emergency unlock"
}
if ($trainingSource -notmatch "ReturnToComputer\(\)" -or $trainingSource -notmatch "ExitAfterUnlock" -or $trainingSource -notmatch "SetParentPassword" -or $trainingSource -notmatch "kt_parent_pin_v1") {
    throw "Training form must allow parent control to return to the PC screen and sync parent password changes"
}
if ($trainingSource -match "defaultAvatar" -or $trainingSource -match "avatarReady") {
    throw "Training storage bootstrap must not add avatar state"
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
if ($generatedText -notmatch "--auto-training") {
    throw "Generated MSI source must start fullscreen learning after login"
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
