param(
    [switch]$IncludeSymbols
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $PSCommandPath
$RootDir = Split-Path -Parent $ScriptDir
$ModName = "AntiAirWeapon[forked]"
$PackageRoot = Join-Path $RootDir "artifacts/release/$ModName"
$VersionedRoot = Join-Path $PackageRoot "1.6"
$AssemblySource = Join-Path $RootDir "dist/Assemblies/AntiAirWeapon.dll"
$PdbSource = Join-Path $RootDir "dist/Assemblies/AntiAirWeapon.pdb"
$ZipPath = Join-Path $RootDir "artifacts/release/$ModName.zip"
$LegacyPackageRoot = Join-Path $RootDir "artifacts/release/AntiAirWeaponForked"
$LegacyZipPath = Join-Path $RootDir "artifacts/release/AntiAirWeaponForked.zip"
$SteamTestRoot = Join-Path $RootDir "3715925883"

& (Join-Path $ScriptDir "build-local.ps1") -Configuration Release
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $AssemblySource -PathType Leaf)) {
    throw "Missing build output: $AssemblySource"
}

Remove-Item -LiteralPath $PackageRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $LegacyPackageRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $LegacyZipPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $PackageRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $VersionedRoot "Assemblies") -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $RootDir "About") -Destination (Join-Path $PackageRoot "About") -Recurse
Copy-Item -LiteralPath (Join-Path $RootDir "Languages") -Destination (Join-Path $PackageRoot "Languages") -Recurse
Copy-Item -LiteralPath (Join-Path $RootDir "Sounds") -Destination (Join-Path $PackageRoot "Sounds") -Recurse
Copy-Item -LiteralPath (Join-Path $RootDir "Textures") -Destination (Join-Path $PackageRoot "Textures") -Recurse
Copy-Item -LiteralPath (Join-Path $RootDir "1.6/Defs") -Destination (Join-Path $VersionedRoot "Defs") -Recurse
Copy-Item -LiteralPath $AssemblySource -Destination (Join-Path $VersionedRoot "Assemblies/AntiAirWeapon.dll")

if ($IncludeSymbols -and (Test-Path -LiteralPath $PdbSource -PathType Leaf)) {
    Copy-Item -LiteralPath $PdbSource -Destination (Join-Path $VersionedRoot "Assemblies/AntiAirWeapon.pdb")
}

Copy-Item -LiteralPath (Join-Path $RootDir "README.md") -Destination (Join-Path $PackageRoot "README.md")
Copy-Item -LiteralPath (Join-Path $RootDir "LICENSE") -Destination (Join-Path $PackageRoot "LICENSE")
Copy-Item -LiteralPath (Join-Path $RootDir "LICENSE.zh-CN.md") -Destination (Join-Path $PackageRoot "LICENSE.zh-CN.md")
Copy-Item -LiteralPath (Join-Path $RootDir "WORKSHOP_DESCRIPTION.md") -Destination (Join-Path $PackageRoot "WORKSHOP_DESCRIPTION.md")

$PublishedFileIds = Get-ChildItem -LiteralPath $PackageRoot -Recurse -Filter "PublishedFileId.txt" -ErrorAction SilentlyContinue
foreach ($File in $PublishedFileIds) {
    Remove-Item -LiteralPath $File.FullName -Force
}

Remove-Item -LiteralPath $SteamTestRoot -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath $PackageRoot -Destination $SteamTestRoot -Recurse

Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -LiteralPath $PackageRoot -DestinationPath $ZipPath -Force

Write-Host "Release prepared:"
Write-Host "  Folder: $PackageRoot"
Write-Host "  Archive: $ZipPath"
Write-Host "  Steam test folder: $SteamTestRoot"
