param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.3.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\KidsTraining.App\KidsTraining.App.csproj"
$artifacts = Join-Path $root "artifacts"
$publishDir = Join-Path $artifacts "publish\$Runtime"
$objDir = Join-Path $artifacts "obj\installer"
$wxsPath = Join-Path $objDir "KidsTraining.generated.wxs"
$msiPath = Join-Path $artifacts "KidsTraining.msi"
$iconPath = Join-Path $objDir "app.ico"

New-Item -ItemType Directory -Force -Path $publishDir, $objDir | Out-Null

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "generate-icon.ps1") -OutputPath $iconPath
if ($LASTEXITCODE -ne 0) {
    throw "icon generation failed with exit code $LASTEXITCODE"
}

& dotnet publish $project -c $Configuration -r $Runtime --self-contained true -o $publishDir /p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $publishDir "KidsTraining.App.exe"
$htmlPath = Join-Path $publishDir "assets\kids-training.html"
if (!(Test-Path $exePath)) {
    throw "Published app executable was not found: $exePath"
}
if (!(Test-Path $htmlPath)) {
    throw "Published HTML asset was not found: $htmlPath"
}

function ConvertTo-WixText([string]$Value) {
    return [System.Security.SecurityElement]::Escape($Value)
}

function Get-StableId([string]$Prefix, [string]$Text) {
    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        $hash = $sha1.ComputeHash($bytes)
        $hex = [System.BitConverter]::ToString($hash).Replace("-", "").Substring(0, 24)
        return "$Prefix$hex"
    }
    finally {
        $sha1.Dispose()
    }
}

function Get-StableGuid([string]$Text) {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("KidsTraining|$Text"))
        $bytes[6] = ($bytes[6] -band 0x0F) -bor 0x30
        $bytes[8] = ($bytes[8] -band 0x3F) -bor 0x80
        return ([Guid]::new($bytes)).ToString("B").ToUpperInvariant()
    }
    finally {
        $md5.Dispose()
    }
}

function Get-RelativePath([string]$BasePath, [string]$TargetPath) {
    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)

    if ($targetFull.Equals($baseFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        return "."
    }

    if ($targetFull.StartsWith($baseFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $targetFull.Substring($baseFull.Length)
    }

    throw "Path is outside base path. Base: $BasePath Target: $TargetPath"
}

$files = Get-ChildItem -Path $publishDir -File -Recurse | Sort-Object FullName
$dirIds = @{ "" = "INSTALLFOLDER" }
$directoryRefs = New-Object System.Collections.Generic.List[string]
$componentRefs = New-Object System.Collections.Generic.List[string]
$fileComponents = New-Object System.Collections.Generic.List[string]
$cleanupRemoveFolders = New-Object System.Collections.Generic.List[string]

$directories = $files |
    ForEach-Object { Get-RelativePath $publishDir $_.DirectoryName } |
    Where-Object { $_ -and $_ -ne "." } |
    Sort-Object { $_.Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Count }, { $_ } -Unique

foreach ($dir in $directories) {
    $parent = Split-Path -Parent $dir
    if ($null -eq $parent) {
        $parent = ""
    }

    $name = Split-Path -Leaf $dir
    $dirId = Get-StableId "DIR" $dir
    $dirIds[$dir] = $dirId
    $parentId = $dirIds[$parent]
    $directoryRefs.Add("    <DirectoryRef Id=`"$parentId`">")
    $directoryRefs.Add("      <Directory Id=`"$dirId`" Name=`"$(ConvertTo-WixText $name)`" />")
    $directoryRefs.Add("    </DirectoryRef>")
    $cleanupRemoveFolders.Add("        <RemoveFolder Id=`"RM$($dirId.Substring(3))`" Directory=`"$dirId`" On=`"uninstall`" />")
}

$cleanupRemoveFolders.Add("        <RemoveFolder Id=`"RMINSTALLFOLDER`" Directory=`"INSTALLFOLDER`" On=`"uninstall`" />")

foreach ($file in $files) {
    $relativeFile = Get-RelativePath $publishDir $file.FullName
    $relativeDir = Get-RelativePath $publishDir $file.DirectoryName
    if ($relativeDir -eq ".") {
        $relativeDir = ""
    }

    $dirId = $dirIds[$relativeDir]
    $componentId = Get-StableId "CMP" $relativeFile
    $fileId = Get-StableId "FIL" $relativeFile
    $guid = Get-StableGuid "file|$relativeFile"
    $source = ConvertTo-WixText $file.FullName
    $name = ConvertTo-WixText $file.Name

    $fileComponents.Add("    <DirectoryRef Id=`"$dirId`">")
    $fileComponents.Add("      <Component Id=`"$componentId`" Guid=`"$guid`">")
    $fileComponents.Add("        <File Id=`"$fileId`" Name=`"$name`" Source=`"$source`" />")
    $fileComponents.Add("        <RegistryValue Root=`"HKCU`" Key=`"Software\KidsTraining\InstalledFiles`" Name=`"$componentId`" Type=`"integer`" Value=`"1`" KeyPath=`"yes`" />")
    $fileComponents.Add("      </Component>")
    $fileComponents.Add("    </DirectoryRef>")
    $componentRefs.Add("      <ComponentRef Id=`"$componentId`" />")
}

$shortcutComponentGuid = Get-StableGuid "component|shortcut"
$startupComponentGuid = Get-StableGuid "component|startup"
$cleanupComponentGuid = Get-StableGuid "component|cleanup"
$componentRefs.Add("      <ComponentRef Id=`"ApplicationShortcutComponent`" />")
$componentRefs.Add("      <ComponentRef Id=`"StartupComponent`" />")
$componentRefs.Add("      <ComponentRef Id=`"ProfileCleanupComponent`" />")

$wxsLines = New-Object System.Collections.Generic.List[string]
$wxsLines.Add("<?xml version=`"1.0`" encoding=`"utf-8`"?>")
$wxsLines.Add("<Wix xmlns=`"http://wixtoolset.org/schemas/v4/wxs`">")
$wxsLines.Add("  <Package Name=`"Kids Training`" Manufacturer=`"tsuyoshi-otake`" Version=`"$Version`" UpgradeCode=`"{B5747390-13DA-4A84-B2E0-D92E2BB0E102}`" Scope=`"perUser`">")
$wxsLines.Add("    <MajorUpgrade DowngradeErrorMessage=`"A newer version of Kids Training is already installed.`" />")
$wxsLines.Add("    <MediaTemplate EmbedCab=`"yes`" />")
$wxsLines.Add("    <Icon Id=`"AppIcon.ico`" SourceFile=`"$(ConvertTo-WixText $iconPath)`" />")
$wxsLines.Add("    <Property Id=`"ARPPRODUCTICON`" Value=`"AppIcon.ico`" />")
$wxsLines.Add("    <StandardDirectory Id=`"LocalAppDataFolder`">")
$wxsLines.Add("      <Directory Id=`"INSTALLFOLDER`" Name=`"KidsTraining`" />")
$wxsLines.Add("    </StandardDirectory>")
$wxsLines.Add("    <StandardDirectory Id=`"ProgramMenuFolder`">")
$wxsLines.Add("      <Directory Id=`"ApplicationProgramsFolder`" Name=`"Kids Training`" />")
$wxsLines.Add("    </StandardDirectory>")
$wxsLines.AddRange($directoryRefs)
$wxsLines.AddRange($fileComponents)
$wxsLines.Add("    <DirectoryRef Id=`"ApplicationProgramsFolder`">")
$wxsLines.Add("      <Component Id=`"ApplicationShortcutComponent`" Guid=`"$shortcutComponentGuid`">")
$wxsLines.Add("        <Shortcut Id=`"ApplicationStartMenuShortcut`" Name=`"Kids Training`" Description=`"Kids Training tray app`" Target=`"[INSTALLFOLDER]KidsTraining.App.exe`" WorkingDirectory=`"INSTALLFOLDER`" Icon=`"AppIcon.ico`" />")
$wxsLines.Add("        <Shortcut Id=`"ApplicationTrainingShortcut`" Name=`"Kids Training - Learning`" Description=`"Start Kids Training learning mode`" Target=`"[INSTALLFOLDER]KidsTraining.App.exe`" Arguments=`"--training`" WorkingDirectory=`"INSTALLFOLDER`" Icon=`"AppIcon.ico`" />")
$wxsLines.Add("        <RemoveFolder Id=`"RemoveApplicationProgramsFolder`" On=`"uninstall`" />")
$wxsLines.Add("        <RegistryValue Root=`"HKCU`" Key=`"Software\KidsTraining`" Name=`"StartMenuShortcut`" Type=`"integer`" Value=`"1`" KeyPath=`"yes`" />")
$wxsLines.Add("      </Component>")
$wxsLines.Add("    </DirectoryRef>")
$wxsLines.Add("    <DirectoryRef Id=`"INSTALLFOLDER`">")
$wxsLines.Add("      <Component Id=`"StartupComponent`" Guid=`"$startupComponentGuid`">")
$wxsLines.Add("        <RegistryValue Root=`"HKCU`" Key=`"Software\Microsoft\Windows\CurrentVersion\Run`" Name=`"KidsTraining`" Type=`"string`" Value=`"&quot;[INSTALLFOLDER]KidsTraining.App.exe&quot; --auto-training`" KeyPath=`"yes`" />")
$wxsLines.Add("      </Component>")
$wxsLines.Add("      <Component Id=`"ProfileCleanupComponent`" Guid=`"$cleanupComponentGuid`">")
$wxsLines.AddRange($cleanupRemoveFolders)
$wxsLines.Add("        <RegistryValue Root=`"HKCU`" Key=`"Software\KidsTraining`" Name=`"ProfileCleanup`" Type=`"integer`" Value=`"1`" KeyPath=`"yes`" />")
$wxsLines.Add("      </Component>")
$wxsLines.Add("    </DirectoryRef>")
$wxsLines.Add("    <Feature Id=`"MainFeature`" Title=`"Kids Training`" Level=`"1`">")
$wxsLines.AddRange($componentRefs)
$wxsLines.Add("    </Feature>")
$wxsLines.Add("  </Package>")
$wxsLines.Add("</Wix>")

[System.IO.File]::WriteAllLines($wxsPath, $wxsLines, [System.Text.UTF8Encoding]::new($false))

& wix build $wxsPath -arch x64 -out $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "wix build failed with exit code $LASTEXITCODE"
}

Write-Host "MSI created: $msiPath"
