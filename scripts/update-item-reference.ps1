#requires -Version 7.0
param(
    [string]$OstranautsPath,
    [string]$PythonPath = 'python',
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OstranautsPath) {
    $settingsPath = Join-Path $repoRoot '.local/install-settings.json'
    if (Test-Path -LiteralPath $settingsPath) {
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        if ($settings.PSObject.Properties.Name -contains 'OstranautsPath') {
            $OstranautsPath = $settings.OstranautsPath
        }
    }
}
if (-not $OstranautsPath) { $OstranautsPath = Read-Host 'Ostranauts installation folder (Steam: Manage > Browse local files)' }
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'Ostranauts_Data/Managed/Assembly-CSharp.dll'))) {
    throw 'This folder does not contain the required Ostranauts game assembly.'
}

# Reuse the existing economic audits and real native-definition checks.
if ($Check) {
    & dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot
    if ($LASTEXITCODE -ne 0) { throw 'Native definition checks failed.' }
} else {
    & (Join-Path $PSScriptRoot 'audit-economy.ps1') -OstranautsPath $gameRoot -PythonPath $PythonPath
}
$export = Join-Path $repoRoot '.local/item-reference-export.json'
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot --export-item-reference $export
if ($LASTEXITCODE -ne 0) { throw 'Native item-reference export failed.' }
$renderArgs = @((Join-Path $PSScriptRoot 'update-item-reference.py'), '--snapshot', $export)
if ($Check) { $renderArgs += '--check' }
& $PythonPath @renderArgs
if ($LASTEXITCODE -ne 0) { throw 'Item reference coverage or freshness check failed. Review the reported catalogue entries.' }
& $PythonPath (Join-Path $PSScriptRoot 'update-constants.py') --check --format json
if ($LASTEXITCODE -ne 0) { throw 'Maintained constants are inconsistent.' }
& $PythonPath (Join-Path $PSScriptRoot 'workshop-release-notes.py') --check --format json
if ($LASTEXITCODE -ne 0) { throw 'Workshop publication records are inconsistent.' }
& $PythonPath (Join-Path $PSScriptRoot 'check-doc-links.py')
if ($LASTEXITCODE -ne 0) { throw 'Documentation links failed.' }
Write-Output 'Item references and economic evidence checked. Game files, saves and installed packages were not changed.'
