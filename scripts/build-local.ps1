param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$RimWorldManagedDir = $env:RIMWORLD_MANAGED_DIR,
    [string]$HarmonyDir = $env:HARMONY_DIR,
    [string]$NetStandardFacade = $env:NETSTANDARD_FACADE,
    [string]$BuildTool = $env:MSBUILD_PATH
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $PSCommandPath
$RootDir = Split-Path -Parent $ScriptDir
$ProjectFile = Join-Path $RootDir "AntiAirWeapon.csproj"

if ([string]::IsNullOrWhiteSpace($env:DOTNET_CLI_HOME)) {
    $env:DOTNET_CLI_HOME = Join-Path $RootDir ".dotnet-home"
}
if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    $env:NUGET_PACKAGES = Join-Path $RootDir ".nuget/packages"
}
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME -Force | Out-Null
New-Item -ItemType Directory -Path $env:NUGET_PACKAGES -Force | Out-Null

function Find-DirectoryWithFile {
    param(
        [string[]]$Candidates,
        [string]$RequiredFile
    )

    foreach ($Candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($Candidate)) {
            continue
        }

        $Resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Candidate)
        if ((Test-Path -LiteralPath $Resolved -PathType Container) -and (Test-Path -LiteralPath (Join-Path $Resolved $RequiredFile) -PathType Leaf)) {
            return $Resolved
        }
    }

    return $null
}

function Find-File {
    param([string[]]$Candidates)

    foreach ($Candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($Candidate)) {
            continue
        }

        $Resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Candidate)
        if (Test-Path -LiteralPath $Resolved -PathType Leaf) {
            return $Resolved
        }
    }

    return $null
}

function Find-CommandPath {
    param([string[]]$Names)

    foreach ($Name in $Names) {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            continue
        }

        if (Test-Path -LiteralPath $Name -PathType Leaf) {
            return $Name
        }

        $Command = Get-Command $Name -ErrorAction SilentlyContinue
        if ($Command) {
            return $Command.Source
        }
    }

    return $null
}

if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir)) {
    $RimWorldManagedDir = Find-DirectoryWithFile @(
        "F:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin64_Data/Managed",
        "C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed",
        "C:/Program Files/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed",
        (Join-Path $RootDir "References/RimWorld/Managed")
    ) "Assembly-CSharp.dll"
}

if ([string]::IsNullOrWhiteSpace($HarmonyDir)) {
    $HarmonyDir = Find-DirectoryWithFile @(
        "F:/SteamLibrary/steamapps/workshop/content/294100/2009463077/Current/Assemblies",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies",
        "C:/Program Files/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies",
        "F:/SteamLibrary/steamapps/workshop/content/294100/2009463077/1.6/Assemblies",
        (Join-Path $RootDir "References/Harmony")
    ) "0Harmony.dll"
}

if ([string]::IsNullOrWhiteSpace($NetStandardFacade)) {
    $NetStandardFacade = Find-File @(
        (Join-Path $RimWorldManagedDir "netstandard.dll"),
        (Join-Path $RootDir ".dotnet/sdk/*/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll"),
        "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll",
        "C:/Program Files/Microsoft Visual Studio/2022/BuildTools/MSBuild/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll",
        "C:/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll"
    )

    if ([string]::IsNullOrWhiteSpace($NetStandardFacade)) {
        $FacadeMatches = Get-ChildItem -Path @(
            (Join-Path $RootDir ".dotnet"),
            "C:/Program Files/Microsoft Visual Studio",
            "C:/Program Files (x86)/Microsoft Visual Studio"
        ) -Recurse -Filter "netstandard.dll" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "net461|4\.7\.2|Facades" } |
            Select-Object -First 1
        if ($FacadeMatches) {
            $NetStandardFacade = $FacadeMatches.FullName
        }
    }
}

if ([string]::IsNullOrWhiteSpace($BuildTool)) {
    $BuildTool = Find-CommandPath @("dotnet", "msbuild")
}

if ([string]::IsNullOrWhiteSpace($BuildTool)) {
    throw "Missing build tool. Install .NET SDK/MSBuild, or set MSBUILD_PATH."
}
if ([string]::IsNullOrWhiteSpace($RimWorldManagedDir)) {
    throw "Missing RimWorld managed assemblies. Set RIMWORLD_MANAGED_DIR or copy files into References/RimWorld/Managed."
}
if ([string]::IsNullOrWhiteSpace($HarmonyDir)) {
    throw "Missing Harmony assemblies. Set HARMONY_DIR or copy 0Harmony.dll into References/Harmony."
}

Write-Host "Using build tool: $BuildTool"
Write-Host "Using RimWorld assemblies: $RimWorldManagedDir"
Write-Host "Using Harmony assemblies: $HarmonyDir"
if (-not [string]::IsNullOrWhiteSpace($NetStandardFacade)) {
    Write-Host "Using netstandard facade: $NetStandardFacade"
}

if ((Split-Path -Leaf $BuildTool) -ieq "dotnet.exe" -or (Split-Path -Leaf $BuildTool) -ieq "dotnet") {
    & $BuildTool msbuild $ProjectFile `
        /p:Configuration=$Configuration `
        /p:RimWorldManagedDir="$RimWorldManagedDir" `
        /p:HarmonyDir="$HarmonyDir" `
        /p:NetStandardFacade="$NetStandardFacade"
} else {
    & $BuildTool $ProjectFile `
        /p:Configuration=$Configuration `
        /p:RimWorldManagedDir="$RimWorldManagedDir" `
        /p:HarmonyDir="$HarmonyDir" `
        /p:NetStandardFacade="$NetStandardFacade"
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
