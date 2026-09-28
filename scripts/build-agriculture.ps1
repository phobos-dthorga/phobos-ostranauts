#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
# Native exports are committed. Regenerate with export-agriculture-art.py and Python/Pillow after artwork changes.
& dotnet build (Join-Path $repoRoot 'src/PhobosAgriculture') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Agriculture build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosAgriculture.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Agriculture balance/persistence checks failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosAgriculture -Readme 'docs/agriculture-player-guide.md' -ExtraDocs @('docs/agriculture-player-guide.md', 'docs/agriculture-bulk-storage.md', 'docs/development/framework-bulk-storage.md', 'docs/development/agriculture-bulk-storage-research.md', 'docs/development/agriculture-first-slice.md', 'docs/development/agriculture-research.md', 'docs/development/agriculture-roadmap.md', 'docs/development/agriculture-implementation.md', 'docs/development/agriculture-living-visuals.md', 'docs/development/agriculture-loot.md', 'docs/agriculture-seed-production.md', 'docs/development/agriculture-treatment-economy.md', 'docs/agriculture-nutrient-production.md', 'docs/development/agriculture-nutrient-recovery.md', 'docs/development/asset-generation-policy.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/generation-records.json') -Destination (Join-Path $package 'agriculture-art-provenance.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/README.md') -Destination (Join-Path $package 'agriculture-art-notes.md')
$artNotes = Join-Path $package 'agriculture-art-notes.md'
Set-Content -LiteralPath $artNotes -Value ((Get-Content -LiteralPath $artNotes -Raw).Replace('../artwork-completion/', 'assets/artwork-completion/')) -Encoding utf8
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/exports.json') -Destination (Join-Path $package 'agriculture-art-exports.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/living-visuals-generation-records.json') -Destination (Join-Path $package 'agriculture-living-art-provenance.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/layers.json') -Destination (Join-Path $package 'agriculture-art-layers.json')
foreach ($record in @('irrigation-generation-records', 'irrigation-exports', 'irrigation-layers', 'stock-generation-records', 'stock-exports', 'stock-layers', 'seed-generation-records', 'workup-generation-records', 'workup-exports')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "assets/phobos-agriculture/$record.json") -Destination (Join-Path $package "agriculture-$record.json")
}
foreach ($record in @('generation-records', 'exports', 'pixellab-requests')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "assets/phobos-agriculture/bulk/$record.json") -Destination (Join-Path $package "agriculture-bulk-$record.json")
}
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed.'
