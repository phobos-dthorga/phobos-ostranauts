#requires -Version 7.0
<#
.SYNOPSIS
Owner-run: update only the Workshop items whose mod has a newer version than the one last uploaded.
.DESCRIPTION
Reads config/workshop-publishing.json and each mod's version first, without building anything.
Mods already on Steam at their current version are skipped, so their change notes are never
repeated; mods with no item yet are listed for a separate Create. Each remaining mod, dependencies
first, is built, prepared and sent through upload-workshop.ps1 as an Update that keeps its item's
recorded visibility. Stops at the first failure. Every relevant change raises a mod's version
(AGENTS.md), so a guide-only change that leaves the version alone is not re-uploaded.
See docs/development/workshop-upload-preparation.md.
.EXAMPLE
./scripts/update-workshop.ps1 -WhatIf
.EXAMPLE
./scripts/update-workshop.ps1 -AcknowledgeHold
#>
[CmdletBinding()]
param(
    # Limit the check to these mods; default is every mod.
    [ValidateSet('Framework', 'AutoNav', 'Shipbreaker', 'Agriculture', 'Manufacturing', 'WarDeclared', 'Medical', 'SpacerStories', 'Bank', 'Exchange')]
    [string[]]$Mod,
    # Private items only: update held mods too (passed to upload-workshop.ps1).
    [switch]$AcknowledgeHold,
    # List what would be updated; builds nothing and does not contact Steam.
    [switch]$WhatIf,
    # Stage the packages already in dist/ instead of building them first.
    [switch]$NoBuild,
    [string]$OstranautsPath,
    [string]$SteamUser,
    [string]$SteamCmdPath,
    # Test override only.
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$preparer = Join-Path $root 'scripts/prepare-workshop.py'

function Invoke-Preparer([string[]]$Arguments) {
    $output = & python $preparer @Arguments
    $report = ($output -join "`n") | ConvertFrom-Json -AsHashtable
    if ($LASTEXITCODE -ne 0 -or $report['status'] -eq 'error') { throw "Workshop preparation check failed: $($report['error'])" }
    return $report
}

$rows = @((Invoke-Preparer @('--pending')).mods | Where-Object { -not $Mod -or $Mod -contains $_.mod.Substring(6) })
$todo = @($rows | Where-Object { $_.needsUpload })
$upToDate = @($rows | Where-Object { $_.itemId -and -not $_.needsUpload })
$notCreated = @($rows | Where-Object { -not $_.itemId })
foreach ($row in $upToDate) { Write-Output "Up to date: $($row.mod) $($row.version) ($($row.uploadedVisibility))" }
foreach ($row in $notCreated) { Write-Output "Not on the Workshop yet: $($row.mod) $($row.version). Create it with ./scripts/upload-workshop.ps1 -Mod $($row.mod.Substring(6)) -Operation Create" }
foreach ($row in $todo) { Write-Output "To update: $($row.mod) $(if ($row.uploadedVersion) { $row.uploadedVersion } else { '(no version recorded)' }) -> $($row.version)" }
if ($todo.Count -eq 0) { Write-Output 'Nothing to update.'; return }
if ($WhatIf) { Write-Output 'WhatIf: nothing built, prepared or uploaded.'; return }

$uploader = Join-Path $PSScriptRoot 'upload-workshop.ps1'
$done = @()
foreach ($row in $todo) {
    $short = $row.mod.Substring(6)
    Write-Output "--- $($row.mod) $($row.version)"
    if ($NoBuild) {
        Invoke-Preparer @('--mod', $short, '--prepare') | Out-Null
    } else {
        $prepare = @{ Mod = $short; Build = $true; Prepare = $true }
        if ($OstranautsPath) { $prepare.OstranautsPath = $OstranautsPath }
        & (Join-Path $PSScriptRoot 'prepare-workshop.ps1') @prepare
    }
    $upload = @{ Mod = $short; Operation = 'Update'; RepositoryRoot = $root }
    if ($AcknowledgeHold) { $upload.AcknowledgeHold = $true }
    if ($SteamUser) { $upload.SteamUser = $SteamUser }
    if ($SteamCmdPath) { $upload.SteamCmdPath = $SteamCmdPath }
    & $uploader @upload
    # A failed SteamCMD run records nothing; stop rather than carry on past an item in an unknown state.
    $after = @((Invoke-Preparer @('--pending')).mods | Where-Object { $_.mod -eq $row.mod })[0]
    if ($after.uploadedVersion -ne $row.version) { throw "$($row.mod) $($row.version) was not recorded as uploaded; check the item on Steam before running this again." }
    $done += "$($row.mod) $($row.version)"
}
Write-Output "Updated: $($done -join ', '). Commit config/workshop-publishing.json."
