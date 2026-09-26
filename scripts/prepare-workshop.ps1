#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Framework','AutoNav','Shipbreaker','Agriculture','Manufacturing')]
    [string]$Mod = 'Framework',
    [switch]$Build,
    [switch]$Prepare,
    [string]$OstranautsPath
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ($Build) {
    if (-not $OstranautsPath) {
        $OstranautsPath = (Get-Content -LiteralPath (Join-Path $repoRoot '.local/install-settings.json') -Raw | ConvertFrom-Json).OstranautsPath
    }
    $builder = @{Framework='framework';AutoNav='autonav';Shipbreaker='shipbreaker';Agriculture='agriculture';Manufacturing='manufacturing'}[$Mod]
    & (Join-Path $PSScriptRoot "build-$builder.ps1") -OstranautsPath $OstranautsPath
    if (-not $?) { throw 'Build failed; no Workshop candidate prepared.' }
    $id = "Phobos$Mod"
    $assembly = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $repoRoot "dist/$id-P0/BepInEx/plugins/$id/$id.dll"))
    $info = @(Get-Content -LiteralPath (Join-Path $repoRoot "mods/$id/mod_info.json") -Raw | ConvertFrom-Json)[0]
    if ($assembly.Name -ne $id -or $assembly.Version.ToString(3) -ne $info.strModVersion) { throw 'Assembly/native version mismatch.' }
}
$arguments = @((Join-Path $PSScriptRoot 'prepare-workshop.py'), '--mod', $Mod)
if ($Prepare) { $arguments += '--prepare' }
& python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Workshop preparation failed. See the JSON report.' }
# Deliberately no Steam process, network upload or game installation.
