#requires -Version 7.0
<#
.SYNOPSIS
Backs up and removes locally installed Phobos mods, for example before switching to Workshop copies.
.DESCRIPTION
Removes only the folders install-mods.ps1 creates (Ostranauts_Data/Mods/<ModId> and
BepInEx/plugins/<ModId>) and their load-order entries. Workshop subscriptions, other
mods, saves and BepInEx configuration are untouched. Every removed file is copied to
.local/installations/<time>-removed/ and verified first. To go back to local copies,
run install-mods.ps1 again. Keep the game closed.
.EXAMPLE
./scripts/remove-local-mods.ps1 -Mods Framework,AutoNav,Shipbreaker -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Framework', 'AutoNav', 'Shipbreaker', 'Agriculture', 'Manufacturing')]
    [string[]]$Mods,
    [string]$OstranautsPath,
    [string]$LoadOrderPath,
    # Test override only.
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'installer-support.ps1')
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$locations = Resolve-InstallLocations $OstranautsPath $LoadOrderPath (Join-Path $root '.local/install-settings.json')
$gameRoot = $locations.OstranautsPath
$orderFile = $locations.LoadOrderPath
$modRoot = Split-Path -Parent $orderFile
Assert-NoLinks $orderFile
if (-not $WhatIfPreference -and (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue)) {
    throw 'Exit Ostranauts normally before removing mods. No files were changed.'
}

$orderHash = (Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash
$order = Get-Content -LiteralPath $orderFile -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
$records = @($order | Where-Object { $_.strName -eq 'Mod Loading Order' })
if ($records.Count -ne 1 -or -not $records[0].ContainsKey('aLoadOrder')) { throw 'Invalid loading_order.json structure.' }
$entries = [Collections.Generic.List[string]]::new([string[]]@($records[0].aLoadOrder))

# Preflight: identify exactly our folders; refuse anything unexpected before changing files.
$plans = @()
foreach ($mod in ($Mods | Select-Object -Unique)) {
    $id = "Phobos$mod"
    $native = Join-Path $modRoot $id
    $plugin = Join-Path $gameRoot "BepInEx/plugins/$id"
    $folders = @()
    foreach ($folder in @($native, $plugin)) {
        if (-not (Test-Path -LiteralPath $folder)) { continue }
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) { throw "Expected a folder: $folder" }
        Assert-NoLinks $folder -Tree
        $folders += $folder
    }
    if ($folders -contains $native) {
        $info = Join-Path $native 'mod_info.json'
        if (Test-Path -LiteralPath $info) {
            $name = (@(Get-Content -LiteralPath $info -Raw | ConvertFrom-Json))[0].strName
            if ($name -notlike 'Phobos*') { throw "$native does not contain a Phobos mod ($name)." }
        }
    }
    if ($folders -contains $plugin -and -not (Test-Path -LiteralPath (Join-Path $plugin "$id.dll"))) {
        throw "$plugin does not contain $id.dll; inspect it by hand."
    }
    $indices = @(for ($i = 0; $i -lt $entries.Count; $i++) {
        $path = $entries[$i].Split('|')[0]
        if ($path -ne 'core' -and (Resolve-ModEntry $path $modRoot) -eq [IO.Path]::GetFullPath($native).TrimEnd('\', '/')) { $i }
    })
    $plans += [pscustomobject]@{ Id = $id; Folders = $folders; Entries = @($indices | ForEach-Object { $entries[$_] }) }
}
$work = @($plans | Where-Object { $_.Folders.Count -gt 0 -or $_.Entries.Count -gt 0 })
if ($work.Count -eq 0) { Write-Output 'No local copies of the selected mods were found. Nothing to remove.'; return }
foreach ($plan in $work) {
    $what = @($plan.Folders) + @($plan.Entries | ForEach-Object { "load-order entry '$_'" })
    Write-Output "$($plan.Id): remove $($what -join ', ')"
}
if (-not $PSCmdlet.ShouldProcess(($work.Id -join ', '), 'Back up and remove local installation')) { return }

$backupRoot = Join-Path $root ('.local/installations/' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-removed')
New-Item -ItemType Directory -Path $backupRoot | Out-Null
Copy-Item -LiteralPath $orderFile -Destination (Join-Path $backupRoot 'loading_order.before.json')
$receipt = [ordered]@{ Operation = 'Remove local mods'; StartedAt = (Get-Date).ToString('o'); LoadOrderPath = $orderFile; Status = 'Backing up'; Removed = @() }
$receiptFile = Join-Path $backupRoot 'receipt.json'
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptFile -Encoding utf8
try {
    foreach ($plan in $work) {
        foreach ($folder in $plan.Folders) {
            $area = if ($folder -like '*BepInEx*') { 'plugin' } else { 'native' }
            $copy = Join-Path $backupRoot "$($plan.Id)/$area"
            New-Item -ItemType Directory -Path $copy -Force | Out-Null
            Get-ChildItem -LiteralPath $folder -Force | Copy-Item -Destination $copy -Recurse -Force
            foreach ($file in Get-ChildItem -LiteralPath $folder -Recurse -File) {
                $twin = Join-Path $copy ([IO.Path]::GetRelativePath($folder, $file.FullName))
                if ((Get-FileHash -LiteralPath $twin).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) { throw "Backup verification failed: $($file.FullName)" }
            }
            $receipt.Removed += [ordered]@{ Mod = $plan.Id; Folder = $folder; Backup = $copy }
        }
    }
    $receipt.Status = 'Removing'
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptFile -Encoding utf8
    if (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue) { throw 'Ostranauts started; nothing was removed.' }
    if ((Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash -ne $orderHash) { throw 'Load order changed during backup; nothing was removed.' }
    foreach ($plan in $work) { foreach ($entry in $plan.Entries) { [void]$entries.Remove($entry) } }
    $records[0].aLoadOrder = $entries.ToArray()
    ConvertTo-Json -InputObject $order -Depth 100 | Set-Content -LiteralPath $orderFile -Encoding utf8
    foreach ($plan in $work) {
        foreach ($folder in $plan.Folders) {
            Remove-Item -LiteralPath $folder -Recurse -Force
            if (Test-Path -LiteralPath $folder) { throw "Folder remains: $folder" }
        }
    }
    $receipt.Status = 'Removed and verified backups'
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptFile -Encoding utf8
} catch {
    throw "Removal incomplete. Keep the game closed and inspect $backupRoot. $($_.Exception.Message)"
}
Write-Output "Removed local copies of $($work.Id -join ', '). Backups and receipt: $backupRoot"
Write-Output 'To return to local copies later, run install-mods.ps1 for the same mods.'
