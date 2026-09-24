#requires -Version 7.0
param([Parameter(Mandatory = $true)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& (Join-Path $PSScriptRoot 'export-shipbreaker-art.ps1')
& (Join-Path $PSScriptRoot 'export-hull-intake-concepts.ps1') -Runtime
& (Join-Path $PSScriptRoot 'export-collector-art.ps1')
& (Join-Path $PSScriptRoot 'export-reclaimer-art.ps1')
& (Join-Path $PSScriptRoot 'export-industrial-console-art.ps1')
& dotnet build (Join-Path $repoRoot 'src/PhobosShipbreaker/PhobosShipbreaker.csproj') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shipbreaker build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosShipbreaker.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shipbreaker processing checks failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Independent construction/native-definition checks failed.' }

. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosShipbreaker -Readme 'docs/shipbreaker-first-build.md' -ExtraDocs @(
    'docs/dependency-contingencies.md', 'docs/shipbreaker-material-uses.md', 'docs/shipbreaker-hull-mounting.md',
    'docs/underfloor-material-transport.md', 'docs/installing-mods.md', 'docs/phobos-framework.md', 'docs/framework-author-guide.md',
    'docs/shipbreaker-hull-intake.md', 'docs/residue-collector.md', 'docs/material-disposal-port-research.md', 'docs/material-port-pairing.md', 'docs/equipment-economy.md',
    'docs/equipment-value-audit.md', 'docs/vanilla-economy-audit.md', 'docs/industrial-control-console.md', 'docs/industrial-control-mockups.md', 'docs/industrial-console-player-guide.md'
)
$materialGuide = Get-Content -LiteralPath (Join-Path $package 'shipbreaker-material-uses.md') -Raw
$materialGuide = $materialGuide.Replace('(shipbreaker-first-build.md)', '(README.md)')
Set-Content -LiteralPath (Join-Path $package 'shipbreaker-material-uses.md') -Value $materialGuide -Encoding utf8
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'No game files, load order or saves were changed.'
