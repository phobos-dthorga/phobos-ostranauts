#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)][string]$OstranautsPath,
    [string]$PythonPath = 'python'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
# Read definitions only. The game may remain open; no installation or save access.
& $PythonPath (Join-Path $PSScriptRoot 'audit-vanilla-economy.py') --game $gameRoot --output (Join-Path $repoRoot 'docs/vanilla-economy-audit.md')
if ($LASTEXITCODE -ne 0) { throw 'Vanilla economy audit failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosNative.Tests') -c Release "-p:OstranautsPath=$gameRoot" -- $gameRoot $repoRoot (Join-Path $repoRoot 'docs/equipment-value-audit.md')
if ($LASTEXITCODE -ne 0) { throw 'Equipment economy checks failed.' }
Write-Output 'Both economy reports refreshed. Game files and saves were not changed.'
