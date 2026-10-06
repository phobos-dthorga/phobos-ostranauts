#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Framework','AutoNav','Shipbreaker','Agriculture','Manufacturing','WarDeclared','Medical','SpacerStories','Bank')]
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
    $builder = @{Framework='framework';AutoNav='autonav';Shipbreaker='shipbreaker';Agriculture='agriculture';Manufacturing='manufacturing';WarDeclared='war-declared';Medical='medical';SpacerStories='spacer-stories';Bank='bank'}[$Mod]
    & (Join-Path $PSScriptRoot "build-$builder.ps1") -OstranautsPath $OstranautsPath
    if (-not $?) { throw 'Build failed; no Workshop candidate prepared.' }
    $id = "Phobos$Mod"
    $info = @(Get-Content -LiteralPath (Join-Path $repoRoot "mods/$id/mod_info.json") -Raw | ConvertFrom-Json)[0]
    # A data-only add-on has no assembly; its manifest carries the version instead.
    if (Test-Path -LiteralPath (Join-Path $repoRoot "src/$id")) {
        $assembly = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $repoRoot "dist/$id-P0/BepInEx/plugins/$id/$id.dll"))
        if ($assembly.Name -ne $id -or $assembly.Version.ToString(3) -ne $info.strModVersion) { throw 'Assembly/native version mismatch.' }
    } elseif ((Get-Content -LiteralPath (Join-Path $repoRoot "mods/$id/phobos-addon.json") -Raw | ConvertFrom-Json).version -ne $info.strModVersion) {
        throw 'Add-on manifest/native version mismatch.'
    }
}
$arguments = @((Join-Path $PSScriptRoot 'prepare-workshop.py'), '--mod', $Mod)
if ($Prepare) { $arguments += '--prepare' }
& python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Workshop preparation failed. See the JSON report.' }
# Deliberately no Steam process, network upload or game installation.
