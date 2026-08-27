[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$ProjectPath = Join-Path $Root "Livesplit.SubnauticaTracker\Livesplit.SubnauticaTracker.csproj"
$BuiltDllPath = Join-Path $Root "Livesplit.SubnauticaTracker\bin\Release\LiveSplit.SubnauticaTracker.dll"
$RepositoryDllPath = Join-Path $Root "Components\LiveSplit.SubnauticaTracker.dll"
$LiveSplitRoot = "C:\Users\mxz\Desktop\LiveSplit\LiveSplit_1.8.37"
$LiveSplitDllPath = Join-Path $LiveSplitRoot "Components\LiveSplit.SubnauticaTracker.dll"

function Assert-File {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not found:`n$Path"
    }
}

function Find-MSBuild {
    $command = Get-Command "MSBuild.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswherePath = Join-Path ${env:ProgramFiles(x86)} `
        "Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path -LiteralPath $vswherePath -PathType Leaf) {
        $foundPath = & $vswherePath `
            -latest `
            -products * `
            -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1

        if ($foundPath) {
            return $foundPath
        }
    }

    return $null
}

function Copy-And-Verify {
    param(
        [Parameter(Mandatory)]
        [string]$Source,

        [Parameter(Mandatory)]
        [string]$Destination
    )

    $destinationDirectory = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force

    $sourceHash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) {
        throw "The copied file failed verification:`n$Destination"
    }

    Write-Host "Updated: $Destination" -ForegroundColor Green
}

try {
    Write-Host ""
    Write-Host "LiveSplit.SubnauticaTracker Release Builder" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    Write-Host ""

    Assert-File -Path $ProjectPath -Description "Project"

    $RequiredReferences = @(
        (Join-Path $LiveSplitRoot "LiveSplit.Core.dll"),
        (Join-Path $LiveSplitRoot "UpdateManager.dll")
    )
    foreach ($ReferencePath in $RequiredReferences) {
        Assert-File -Path $ReferencePath -Description "Required project reference"
    }

    if (Get-Process -Name "LiveSplit" -ErrorAction SilentlyContinue) {
        throw "LiveSplit is currently running. Close LiveSplit and run the script again."
    }

    $MSBuildPath = Find-MSBuild
    if (-not $MSBuildPath) {
        throw @"
MSBuild could not be found.

Install Visual Studio 2022 or Visual Studio Build Tools with:
- .NET desktop build tools
- .NET Framework 4.8.1 targeting pack
"@
    }

    Write-Host "Using MSBuild:" -ForegroundColor DarkGray
    Write-Host $MSBuildPath -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Building Release | Any CPU..." -ForegroundColor Yellow
    Write-Host ""

    & $MSBuildPath `
        $ProjectPath `
        /t:Rebuild `
        /p:Configuration=Release `
        /p:Platform=AnyCPU `
        "/p:ReferencePath=$LiveSplitRoot" `
        /m `
        /nologo `
        /verbosity:minimal

    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE."
    }

    Assert-File -Path $BuiltDllPath -Description "Release build output"

    Write-Host ""
    Write-Host "Build completed:" -ForegroundColor Green
    Write-Host $BuiltDllPath
    Write-Host ""

    Copy-And-Verify -Source $BuiltDllPath -Destination $RepositoryDllPath
    Copy-And-Verify -Source $BuiltDllPath -Destination $LiveSplitDllPath

    $BuiltFile = Get-Item -LiteralPath $BuiltDllPath
    Write-Host ""
    Write-Host "Release build and deployment completed successfully." -ForegroundColor Cyan
    Write-Host "File version: $($BuiltFile.VersionInfo.FileVersion)" -ForegroundColor DarkGray
    Write-Host "Built at: $($BuiltFile.LastWriteTime)" -ForegroundColor DarkGray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "Build/deployment failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
