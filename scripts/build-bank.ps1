#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& dotnet build (Join-Path $repoRoot 'src/PhobosBank') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Phobos Banking build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosBank.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Mortgage-figure and debt-summary checks failed.' }
# The native suite compares the mirrored mortgage figures and the PDA hooks with the installed game.
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosBank -Readme 'docs/bank-player-guide.md' -ExtraDocs @(
    'docs/bank-player-guide.md', 'docs/development/pda-apps-and-banking-research.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed; offline checks are not gameplay validation.'
