#requires -Version 7.0
# Phobos Spacer Stories is a data-only add-on: no plugin. The build checks every story file against the shipped
# packs and, through the native suite, every name it uses against the installed game, then packages the folder.
param([Parameter(Mandatory)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& python (Join-Path $repoRoot 'scripts/validate-data-packs.py') --addon (Join-Path $repoRoot 'mods/PhobosSpacerStories')
if ($LASTEXITCODE -ne 0) { throw 'Spacer Stories files failed the offline story checks.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosSpacerStories -Readme 'mods/PhobosSpacerStories/README.md' -DataOnly -ExtraDocs @(
    'docs/writing-story-content.md', 'docs/development/spacer-stories-authoring.md', 'docs/development/spacer-stories-vanilla-expansion.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'Prepared only. No game files, load order or saves were changed; offline checks are not gameplay validation.'
