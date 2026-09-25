#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)][string]$OstranautsPath,
    [string]$PythonPath = 'python',
    [switch]$AgricultureOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
# Read definitions only. The game may remain open; no installation or save access.
if ($AgricultureOnly) {
    & dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot --agriculture-only (Join-Path $repoRoot 'docs/agriculture-economy-evidence.md')
    if ($LASTEXITCODE -ne 0) { throw 'Agriculture economy audit failed.' }
    Write-Output 'Agriculture economy evidence refreshed; unrelated content was not prepared. Game files and saves were not changed.'
    return
}
& $PythonPath (Join-Path $PSScriptRoot 'audit-regional-economy.py') --game $gameRoot
if ($LASTEXITCODE -ne 0) { throw 'Regional economy audit failed.' }
& $PythonPath (Join-Path $PSScriptRoot 'audit-vanilla-economy.py') --game $gameRoot --output (Join-Path $repoRoot 'docs/vanilla-economy-audit.md')
if ($LASTEXITCODE -ne 0) { throw 'Vanilla economy audit failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot (Join-Path $repoRoot 'docs/equipment-value-audit.md') (Join-Path $repoRoot 'docs/agriculture-economy-evidence.md')
if ($LASTEXITCODE -ne 0) { throw 'Equipment economy checks failed.' }
Write-Output 'Vanilla, equipment and Agriculture economy reports refreshed. Game files and saves were not changed.'
