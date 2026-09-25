#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& dotnet build (Join-Path $repoRoot 'src/PhobosManufacturing') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Manufacturing scaffold build failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosManufacturing -Readme 'docs/manufacturing-implementation.md' -ExtraDocs @('docs/manufacturing-implementation.md', 'docs/manufacturing-research.md', 'docs/manufacturing-handover.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-manufacturing/README.md') -Destination (Join-Path $package 'manufacturing-art-brief.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Research scaffold only. No game files, load order or saves were changed.'
