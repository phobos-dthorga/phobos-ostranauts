#requires -Version 7.0
param([Parameter(Mandatory = $true)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'external/phobos-scope/recording/Phobos.Scope.Recording/Phobos.Scope.Recording.csproj'))) {
    throw 'Initialize the pinned source dependency first: git submodule update --init --recursive'
}
& dotnet build (Join-Path $repoRoot 'src/PhobosFramework/PhobosFramework.csproj') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Phobos Framework build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosFramework.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Phobos Framework checks failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosPerformance.Tests') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Performance capture adapter checks failed.' }
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
$package = New-PhobosPackage -RepoRoot $repoRoot -Id PhobosFramework -Readme 'docs/framework-author-guide.md' -ExtraDocs @('docs/phobos-framework.md', 'docs/material-port-pairing.md', 'docs/equipment-economy.md', 'docs/equipment-value-audit.md', 'docs/vanilla-economy-audit.md')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'No game files, load order or saves were changed.'
