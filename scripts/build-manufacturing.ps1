#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& dotnet build (Join-Path $repoRoot 'src/PhobosManufacturing') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Manufacturing build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosManufacturing.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Manufacturing chemistry and record checks failed.' }
# The native suite prepares every content mod; Shipbreaker's projects must build first for the steel charge.
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
& python (Join-Path $PSScriptRoot 'export-completion-art.py') --check
if ($LASTEXITCODE -ne 0) { throw 'Artwork export check failed.' }
& python (Join-Path $PSScriptRoot 'export-line-art.py') --check
if ($LASTEXITCODE -ne 0) { throw 'Line artwork check failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosManufacturing -Readme 'docs/manufacturing-player-guide.md' -ExtraDocs @(
    'docs/manufacturing-player-guide.md', 'docs/development/manufacturing-refinery-and-chemistry.md', 'docs/development/manufacturing-implementation.md',
    'docs/development/manufacturing-research.md', 'docs/development/manufacturing-handover.md', 'docs/development/manufacturing-art-handoff.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-manufacturing/README.md') -Destination (Join-Path $package 'manufacturing-art-brief.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed; offline checks are not gameplay validation.'
