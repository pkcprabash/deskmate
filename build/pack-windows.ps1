# Builds, signs and packs the Windows installer with Velopack.
#
# Usage: build/pack-windows.ps1 [-Version 1.0.0] [-Rid win-x64]
#
# Code signing runs only when SIGN_PARAMS is set; it is passed to signtool, e.g.
#   /fd sha256 /tr http://timestamp.digicert.com /td sha256 /f cert.pfx /p <password>
# (or an Azure Trusted Signing / azuresigntool command line via VPK's --signParams / --signTemplate).
# Otherwise you get an unsigned local build.
#
# Requires: dotnet tool install -g vpk
param(
    [string]$Version = "1.0.0",
    [string]$Rid = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts/publish/$Rid"
$releaseDir = Join-Path $root "artifacts/releases/$Rid"

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
dotnet publish (Join-Path $root "src/Deskmate.App/Deskmate.App.csproj") `
    -c Release -r $Rid --self-contained "-p:Version=$Version" -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$packArgs = @(
    "pack",
    "--packId", "Deskmate",
    "--packTitle", "Deskmate",
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--mainExe", "Deskmate.App.exe",
    "--outputDir", $releaseDir,
    "--icon", (Join-Path $root "src/Deskmate.App/Assets/avalonia-logo.ico")
)

if ($env:SIGN_PARAMS) {
    $packArgs += @("--signParams", $env:SIGN_PARAMS)
} else {
    Write-Host "SIGN_PARAMS not set: building an UNSIGNED package."
}

vpk @packArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }
Write-Host "Done. Output in $releaseDir"
