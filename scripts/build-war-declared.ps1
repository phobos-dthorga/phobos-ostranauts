#requires -Version 7.0
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& dotnet build (Join-Path $repoRoot 'src/PhobosWarDeclared') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'War Has Been Declared build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosWarDeclared.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Battle-window, schematic and ledger checks failed.' }
# The native suite resolves build sites against the installed game's own definitions and install jobs.
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosWarDeclared -Readme 'docs/war-declared-player-guide.md' -ExtraDocs @(
    'docs/war-declared-player-guide.md', 'docs/development/war-declared-design.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed; offline checks are not gameplay validation.'
