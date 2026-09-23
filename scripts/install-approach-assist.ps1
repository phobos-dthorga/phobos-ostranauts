#requires -Version 7.0
# Compatibility entry point. All installation rules live in install-mods.ps1.
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)][string]$OstranautsPath,
    [Parameter(Mandatory = $true)][string]$LoadOrderPath,
    [string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist/PhobosApproachAssist-P0'),
    [switch]$VerifyOnly
)
& (Join-Path $PSScriptRoot 'install-mods.ps1') @PSBoundParameters -Mods ApproachAssist -NoRememberPaths
