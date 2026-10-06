#requires -Version 7.0
<#
.SYNOPSIS
Owner-run Steam Workshop upload of a prepared, verified Phobos candidate through SteamCMD.
.DESCRIPTION
Never run by builds, CI or preparation. You type your Steam password and Steam Guard
code into SteamCMD's own prompt; this script never sees, stores or passes them.
Each run keeps a receipt under .local/workshop-receipts/ before contacting Steam.
New items are always created Private; test a subscription before choosing Public.
See docs/development/workshop-upload-preparation.md.
.EXAMPLE
./scripts/upload-workshop.ps1 -Mod Framework -Operation Create -SteamUser myaccount -WhatIf
.EXAMPLE
./scripts/upload-workshop.ps1 -Mod Framework -Operation Update -Visibility Public -ConfirmPublic
.EXAMPLE
./scripts/upload-workshop.ps1 -Reconcile .local/workshop-receipts/PhobosFramework/<receipt> -ItemId 1234567890
#>
[CmdletBinding(DefaultParameterSetName = 'Upload')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Upload')]
    [ValidateSet('Framework', 'AutoNav', 'Shipbreaker', 'Agriculture', 'Manufacturing', 'WarDeclared', 'Medical', 'SpacerStories', 'Bank')]
    [string]$Mod,
    # Must match the candidate: Create for a mod without an item ID, Update afterwards.
    [Parameter(Mandatory, ParameterSetName = 'Upload')]
    [ValidateSet('Create', 'Update')]
    [string]$Operation,
    [Parameter(ParameterSetName = 'Upload')]
    [ValidateSet('Private', 'FriendsOnly', 'Unlisted', 'Public')]
    [string]$Visibility = 'Private',
    # A staging directory from prepare-workshop; default is the newest one for this mod.
    [Parameter(ParameterSetName = 'Upload')]
    [string]$Candidate,
    # Account name only (remembered locally); never a password.
    [Parameter(ParameterSetName = 'Upload')]
    [string]$SteamUser,
    [Parameter(ParameterSetName = 'Upload')]
    [string]$SteamCmdPath,
    # Private uploads only: proceed despite a publication hold or unpublished dependency.
    [Parameter(ParameterSetName = 'Upload')]
    [switch]$AcknowledgeHold,
    [Parameter(ParameterSetName = 'Upload')]
    [switch]$ConfirmPublic,
    [Parameter(ParameterSetName = 'Upload')]
    [switch]$WhatIf,
    # After an uncertain create: record the item Steam shows, or confirm none exists.
    [Parameter(Mandatory, ParameterSetName = 'Reconcile')]
    [string]$Reconcile,
    [Parameter(ParameterSetName = 'Reconcile')]
    [string]$ItemId,
    [Parameter(ParameterSetName = 'Reconcile')]
    [switch]$NotCreated,
    # Test override only.
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$preparer = Join-Path $root 'scripts/prepare-workshop.py'
$settingsFile = Join-Path $root '.local/workshop-settings.json'
$receiptRoot = Join-Path $root '.local/workshop-receipts'
$visibilityKey = @{ Private = 'private'; FriendsOnly = 'friends'; Unlisted = 'unlisted'; Public = 'public' }

function Invoke-Preparer([string[]]$Arguments) {
    $output = & python $preparer @Arguments
    $report = ($output -join "`n") | ConvertFrom-Json -AsHashtable
    if ($LASTEXITCODE -ne 0 -or $report['status'] -eq 'error') { throw "Workshop preparation check failed: $($report['error'])" }
    return $report
}
function Write-Json($Value, [string]$Path) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 10) + "`n", [Text.UTF8Encoding]::new($false))
}
function Read-Receipt([string]$Directory) {
    $file = Join-Path $Directory 'receipt.json'
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "No receipt.json in $Directory" }
    return Get-Content -LiteralPath $file -Raw | ConvertFrom-Json -AsHashtable
}
function Get-VdfItemId([string]$Path) {
    $match = [regex]::Match((Get-Content -LiteralPath $Path -Raw), '"publishedfileid"\s+"(\d+)"')
    if ($match.Success) { return $match.Groups[1].Value }
    return '0'
}

if ($PSCmdlet.ParameterSetName -eq 'Reconcile') {
    $directory = (Resolve-Path -LiteralPath $Reconcile).Path
    $receipt = Read-Receipt $directory
    if ($receipt.status -notin @('pending', 'submitted-unverified')) { throw "Receipt is already $($receipt.status)." }
    if ($NotCreated.IsPresent -eq [bool]$ItemId) { throw 'Give exactly one of -ItemId (the item Steam shows) or -NotCreated.' }
    if ($ItemId) {
        if ($receipt.operation -eq 'update' -and $ItemId -ne $receipt.itemId) { throw "This update targeted item $($receipt.itemId)." }
        if ($receipt.operation -eq 'create') { Invoke-Preparer @('--mod', $receipt.mod, '--record-item-id', $ItemId) | Out-Null }
        Invoke-Preparer @('--mod', $receipt.mod, '--record-uploaded-version', $receipt.version) | Out-Null
        $receipt.itemId = $ItemId
        $receipt.status = 'reconciled-submitted'
    } else {
        $receipt.status = 'reconciled-not-created'
    }
    $receipt.reconciledAt = (Get-Date).ToString('o')
    Write-Json $receipt (Join-Path $directory 'receipt.json')
    Write-Output "Receipt marked $($receipt.status)."
    return
}

$id = "Phobos$Mod"
$settings = @{}
if (Test-Path -LiteralPath $settingsFile) { $settings = Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json -AsHashtable }
if (-not $SteamUser) { $SteamUser = $settings['SteamUser'] }
if (-not $SteamCmdPath) { $SteamCmdPath = $settings['SteamCmdPath'] }
if (-not $SteamCmdPath) {
    $found = Get-Command steamcmd -ErrorAction SilentlyContinue
    $SteamCmdPath = if ($found) { $found.Source } elseif (Test-Path -LiteralPath 'C:/steamcmd/steamcmd.exe') { 'C:/steamcmd/steamcmd.exe' }
}

# 1. The candidate: verified, current and matching the requested operation.
if (-not $Candidate) {
    $staging = Join-Path $root ".local/workshop-staging/$id"
    $newest = @(if (Test-Path -LiteralPath $staging) { Get-ChildItem -LiteralPath $staging -Directory | Sort-Object LastWriteTimeUtc -Descending })
    if ($newest.Count -eq 0) { throw "No prepared candidate for $id. Run ./scripts/prepare-workshop.ps1 -Mod $Mod -Build -Prepare first." }
    $Candidate = $newest[0].FullName
}
$Candidate = (Resolve-Path -LiteralPath $Candidate).Path
$verified = Invoke-Preparer @('--verify', $Candidate)
if ($verified.mod -ne $id) { throw "Candidate is for $($verified.mod), not $id." }
$manifest = Get-Content -LiteralPath (Join-Path $Candidate 'manifest.json') -Raw | ConvertFrom-Json -AsHashtable
$entry = (Get-Content -LiteralPath (Join-Path $root 'config/workshop-publishing.json') -Raw | ConvertFrom-Json -AsHashtable).mods[$id]
$current = $entry.itemId
if ($manifest.operation -ne $Operation.ToLowerInvariant()) { throw "Candidate is prepared for $($manifest.operation), not $Operation." }
if ($current -and -not $manifest.itemId) { throw "$id already has item $current. Prepare a fresh candidate, which will update it instead of creating a duplicate." }
if ($manifest.itemId -and $manifest.itemId -ne $current) { throw 'Catalogue item ID changed since this candidate was prepared. Prepare a fresh candidate.' }
# The change note lists every version since the last upload; a newer upload since preparing makes it wrong.
if ($manifest.ContainsKey('changeNoteVersions') -and $current -and $manifest.uploadedVersion -ne $entry['uploadedVersion']) {
    throw 'Another upload was recorded since this candidate was prepared, so its change note is out of date. Prepare a fresh candidate.'
}
$version = (@(Get-Content -LiteralPath (Join-Path $root "mods/$id/mod_info.json") -Raw | ConvertFrom-Json))[0].strModVersion
if ($manifest.version -ne $version) { throw "Candidate is $($manifest.version) but the source is now $version. Prepare a fresh candidate." }

# 2. Publication rules.
$blockers = @($manifest.blockers)
# Holds and unpublished dependencies may be waived for a private test; a missing cover may not.
$waivable = @($blockers | Where-Object { ($entry.hold -and $_ -eq $entry.hold) -or $_ -like '*has no published item ID yet*' })
if ($blockers.Count -gt 0) {
    if ($Visibility -ne 'Private' -or -not $AcknowledgeHold -or $waivable.Count -ne $blockers.Count) {
        throw "Blocked: $($blockers -join '; '). Only a Private upload with -AcknowledgeHold may proceed past holds or unpublished dependencies."
    }
}
if ($Visibility -ne 'Private') {
    if ($Operation -eq 'Create') { throw 'New items are created Private. Test a subscription, then run an Update with the wider visibility.' }
    if ($manifest.workingTreeDirty) { throw 'Candidate was prepared from uncommitted changes. Commit, then prepare a fresh candidate.' }
    if ($Visibility -eq 'Public' -and -not $ConfirmPublic) { throw 'Public visibility needs -ConfirmPublic after a successful private subscription test.' }
}
$open = @(if (Test-Path -LiteralPath (Join-Path $receiptRoot $id)) {
    Get-ChildItem -LiteralPath (Join-Path $receiptRoot $id) -Directory | Where-Object {
        $r = Read-Receipt $_.FullName; $r.operation -eq 'create' -and $r.status -in @('pending', 'submitted-unverified') } })
if ($Operation -eq 'Create' -and $open.Count -gt 0) {
    throw "An earlier create for $id is unresolved: $($open[0].FullName). Check your Workshop items on Steam, then run -Reconcile with -ItemId or -NotCreated. Retrying could create a duplicate."
}
if ($manifest.workingTreeDirty) { Write-Warning 'Candidate was prepared from uncommitted changes; acceptable only for a private test.' }

$required = @($manifest.requiredItemDetails | ForEach-Object { if ($_.itemId) { "$($_.name) ($($_.itemId))" } else { "$($_.name) (not published yet)" } })
Write-Output "Upload plan: $($Operation.ToLowerInvariant()) $id $version as $Visibility"
Write-Output "Candidate: $Candidate"
if ($manifest.itemId) { Write-Output "Item: https://steamcommunity.com/sharedfiles/filedetails/?id=$($manifest.itemId)" }
Write-Output "Required items to set on Steam afterwards: $($required -join '; ')"
if ($manifest.ContainsKey('changeNoteVersions')) {
    $covers = "Change note covers: $($manifest.changeNoteVersions -join ', ')"
    if (@($manifest.changeNoteOmitted).Count -gt 0) { $covers += " (too long for Steam, so it points to CHANGELOG.md for $($manifest.changeNoteOmitted -join ', '))" }
    Write-Output $covers
}
if ($blockers.Count -gt 0) { Write-Output "Proceeding past (private, acknowledged): $($blockers -join '; ')" }

if (-not $SteamCmdPath -or -not (Test-Path -LiteralPath $SteamCmdPath -PathType Leaf)) {
    throw 'SteamCMD not found. Install it from https://developer.valvesoftware.com/wiki/SteamCMD and pass -SteamCmdPath once; it is remembered locally.'
}
if (-not $SteamUser) { throw 'Pass -SteamUser <account name> once; it is remembered locally. Never pass a password.' }
if ($WhatIf) { Write-Output 'WhatIf: no receipt written and SteamCMD not started.'; return }

# 3. Receipt before contacting Steam; the candidate itself stays untouched.
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $settingsFile) | Out-Null
Write-Json @{ SteamUser = $SteamUser; SteamCmdPath = $SteamCmdPath } $settingsFile
$receiptDir = Join-Path $receiptRoot "$id/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$version-$($Operation.ToLowerInvariant())"
New-Item -ItemType Directory -Path $receiptDir | Out-Null
$vdf = Join-Path $receiptDir 'upload.vdf'
Invoke-Preparer @('--upload-vdf', $Candidate, '--visibility', $visibilityKey[$Visibility], '--output', $vdf) | Out-Null
$receipt = [ordered]@{
    schemaVersion = 1; mod = $id; version = $version; operation = $Operation.ToLowerInvariant(); visibility = $Visibility
    itemId = $manifest.itemId; candidate = $Candidate; sourceCommit = $manifest.sourceCommit
    manifestSha256 = (Get-FileHash -LiteralPath (Join-Path $Candidate 'manifest.json') -Algorithm SHA256).Hash
    acknowledgedBlockers = $blockers; startedAt = (Get-Date).ToString('o'); status = 'pending'
}
Write-Json $receipt (Join-Path $receiptDir 'receipt.json')

# 4. SteamCMD in this console, so its password and Steam Guard prompts work normally.
Write-Output 'Starting SteamCMD. Enter your password and Steam Guard code in its prompt if asked.'
$started = Get-Date
& $SteamCmdPath '+login' $SteamUser '+workshop_build_item' $vdf '+quit'
$receipt.steamCmdExitCode = $LASTEXITCODE
$receipt.finishedAt = (Get-Date).ToString('o')
$logs = Join-Path (Split-Path -Parent $SteamCmdPath) 'logs'
if (Test-Path -LiteralPath $logs) {
    New-Item -ItemType Directory -Force -Path (Join-Path $receiptDir 'steamcmd-logs') | Out-Null
    Get-ChildItem -LiteralPath $logs -File | Where-Object LastWriteTime -ge $started.AddSeconds(-5) |
        Copy-Item -Destination (Join-Path $receiptDir 'steamcmd-logs')
}

# 5. SteamCMD writes a new item's ID back into the VDF. Its exit code alone is not proof.
$returned = Get-VdfItemId $vdf
$receipt.status = 'submitted-unverified'
if ($receipt.operation -eq 'create' -and $returned -ne '0') {
    $receipt.itemId = $returned
    Write-Json $receipt (Join-Path $receiptDir 'receipt.json')
    Invoke-Preparer @('--mod', $id, '--record-item-id', $returned) | Out-Null
    Invoke-Preparer @('--mod', $id, '--record-uploaded-version', $version) | Out-Null
    $receipt.status = 'created-unverified'
    Write-Output "Steam returned item $returned; recorded with version $version in config/workshop-publishing.json (commit it)."
} elseif ($receipt.operation -eq 'create') {
    Write-Warning "No item ID came back. Check your Workshop items on Steam before anything else, then run: ./scripts/upload-workshop.ps1 -Reconcile '$receiptDir' -ItemId <id> (or -NotCreated)."
} elseif ($receipt.steamCmdExitCode -eq 0) {
    # The next change note starts after this version. A failed run records nothing, so its versions are described again next time.
    Invoke-Preparer @('--mod', $id, '--record-uploaded-version', $version) | Out-Null
    Write-Output "Recorded version $version as uploaded in config/workshop-publishing.json (commit it)."
} else {
    Write-Warning "SteamCMD exited with code $($receipt.steamCmdExitCode); version $version was not recorded as uploaded. Check the item on Steam; if the update did arrive, run: ./scripts/upload-workshop.ps1 -Reconcile '$receiptDir' -ItemId $($receipt.itemId)"
}
Write-Json $receipt (Join-Path $receiptDir 'receipt.json')
Write-Output "Receipt: $receiptDir"
if ($receipt.itemId) {
    Write-Output "Next: open https://steamcommunity.com/sharedfiles/filedetails/?id=$($receipt.itemId) and check title, description, change note, cover and visibility."
    Write-Output "Set Required items (Owner controls > Add/Remove Required Items): $($required -join '; ')"
    Write-Output 'Accept the Workshop legal agreement if Steam asks. Then run the subscription test in docs/development/workshop-upload-preparation.md.'
}
if ($Visibility -eq 'Public') {
    Write-Output "After confirming the public item, mark $version Released with today's date in mods/$id/CHANGELOG.md, update the page's Publication status and regenerate release notes."
}
