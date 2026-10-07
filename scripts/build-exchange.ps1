#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& dotnet build (Join-Path $repoRoot 'src/PhobosExchange') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Phobos Exchange build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosExchange.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Market model, record and trading checks failed.' }
# The native suite checks the game's market members the exchange reads and every driver's station and category.
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosExchange -Readme 'docs/exchange-player-guide.md' -ExtraDocs @(
    'docs/exchange-player-guide.md', 'docs/development/share-market-and-charts-design.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed; offline checks are not gameplay validation.'
