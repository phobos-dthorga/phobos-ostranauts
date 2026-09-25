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
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosAgriculture -Readme 'docs/agriculture-player-guide.md' -ExtraDocs @('docs/agriculture-player-guide.md', 'docs/agriculture-first-slice.md', 'docs/agriculture-research.md', 'docs/agriculture-roadmap.md', 'docs/agriculture-implementation.md', 'docs/agriculture-living-visuals.md', 'docs/agriculture-loot.md', 'docs/asset-generation-policy.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/generation-records.json') -Destination (Join-Path $package 'agriculture-art-provenance.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/README.md') -Destination (Join-Path $package 'agriculture-art-notes.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/exports.json') -Destination (Join-Path $package 'agriculture-art-exports.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/living-visuals-generation-records.json') -Destination (Join-Path $package 'agriculture-living-art-provenance.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-agriculture/layers.json') -Destination (Join-Path $package 'agriculture-art-layers.json')
foreach ($record in @('irrigation-generation-records', 'irrigation-exports', 'irrigation-layers', 'stock-generation-records', 'stock-exports', 'stock-layers')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "assets/phobos-agriculture/$record.json") -Destination (Join-Path $package "agriculture-$record.json")
}
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed.'
