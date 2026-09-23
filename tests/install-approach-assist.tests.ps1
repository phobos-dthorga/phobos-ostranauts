#requires -Version 7.0
param([string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist/PhobosApproachAssist-P0'))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $repoRoot 'scripts/install-approach-assist.ps1'
$fixtures = Join-Path $repoRoot ('.local/script-tests/' + [guid]::NewGuid().ToString('N'))
$script:passed = 0
$global:PhobosInstallerTestGameRunning = $false

# Exercise only synthetic installations. Never inspect or stop the real game process.
function Get-Process {
    [CmdletBinding()]
    param([string]$Name)
    if ($global:PhobosInstallerTestGameRunning) { [pscustomobject]@{ ProcessName = $Name } }
}
function Check($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:passed++
}
function Fails([scriptblock]$Action, [string]$Message) {
    try { & $Action; throw 'Expected failure did not occur' }
    catch { Check ($_.Exception.Message.Contains($Message)) "Wrong failure: $($_.Exception.Message)" }
}
function Fixture([string]$Name, [string[]]$Entries = @('core', 'ExistingMod|disabled')) {
    $root = Join-Path $fixtures $Name
    $game = Join-Path $root 'game'
    $mods = Join-Path $root 'configured-mods'
    New-Item -ItemType Directory -Path (Join-Path $game 'BepInEx/core'), $mods -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'BepInEx/core/BepInEx.dll') -Value 'fixture prerequisite only'
    Set-Content -LiteralPath (Join-Path $game 'Ostranauts.exe') -Value 'fixture executable marker'
    $order = Join-Path $mods 'loading_order.json'
    $data = @(@{ strName = 'Mod Loading Order'; aLoadOrder = $Entries; CORE_MOD_NAME = 'core'; aIgnorePatterns = @('KeepMe') })
    ConvertTo-Json -InputObject $data -Depth 10 | Set-Content -LiteralPath $order
    return @{ OstranautsPath = $game; LoadOrderPath = $order; PackagePath = $PackagePath }
}
function ReadOrder($Fixture) { return @(Get-Content -LiteralPath $Fixture.LoadOrderPath -Raw | ConvertFrom-Json)[0] }

$fresh = Fixture 'fresh'
$before = (Get-FileHash -LiteralPath $fresh.LoadOrderPath).Hash
& $installer @fresh -WhatIf | Out-Null
Check ((Get-FileHash -LiteralPath $fresh.LoadOrderPath).Hash -eq $before) 'Preview changed load order'
Check (-not (Test-Path -LiteralPath (Join-Path $fresh.OstranautsPath 'BepInEx/plugins'))) 'Preview copied files'
Fails { & $installer @fresh -VerifyOnly | Out-Null } 'Installation differs'
& $installer @fresh | Out-Null
$installed = ReadOrder $fresh
Check (($installed.aLoadOrder -join ',') -eq 'core,ExistingMod|disabled,PhobosApproachAssist') 'Existing order was not preserved'
Check ($installed.aIgnorePatterns[0] -eq 'KeepMe') 'Other load-order settings were lost'
& $installer @fresh -VerifyOnly | Out-Null
Check $true 'Fresh installation verified'
$dll = Join-Path $fresh.OstranautsPath 'BepInEx/plugins/PhobosApproachAssist/PhobosApproachAssist.dll'
$dllStamp = (Get-Item -LiteralPath $dll).LastWriteTimeUtc
$orderStamp = (Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc
& $installer @fresh | Out-Null
Check ((Get-Item -LiteralPath $dll).LastWriteTimeUtc -eq $dllStamp) 'Repeat installation rewrote DLL'
Check ((Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc -eq $orderStamp) 'Repeat installation rewrote load order'

$nativeFile = Join-Path (Split-Path -Parent $fresh.LoadOrderPath) 'PhobosApproachAssist/mod_info.json'
Set-Content -LiteralPath $nativeFile -Value 'previous installed contents'
Fails { & $installer @fresh -VerifyOnly | Out-Null } 'Installation differs'
$output = & $installer @fresh
$backupLine = $output | Where-Object { $_ -like '*Backups and receipt:*' }
$backupPath = ($backupLine -split 'Backups and receipt: ', 2)[1]
Check ((Get-Content -LiteralPath (Join-Path $backupPath 'PhobosApproachAssist/native/mod_info.json') -Raw).Trim() -eq 'previous installed contents') 'Update did not back up overwritten file'
& $installer @fresh -VerifyOnly | Out-Null
Check $true 'Updated file verified'

$disabled = Fixture 'disabled' @('core', 'PhobosApproachAssist|disabled', 'ExistingMod|edit')
Fails { & $installer @disabled -VerifyOnly | Out-Null } 'load-order status: disabled'
Check (((ReadOrder $disabled).aLoadOrder -join ',') -eq 'core,PhobosApproachAssist|disabled,ExistingMod|edit') 'Verification changed disabled entry'
& $installer @disabled | Out-Null
Check (((ReadOrder $disabled).aLoadOrder -join ',') -eq 'core,PhobosApproachAssist,ExistingMod|edit') 'Enabling moved or changed unrelated entries'
$absolute = Fixture 'absolute'
$absoluteData = @(Get-Content -LiteralPath $absolute.LoadOrderPath -Raw | ConvertFrom-Json)
$absoluteData[0].aLoadOrder += (Join-Path (Split-Path -Parent $absolute.LoadOrderPath) 'PhobosApproachAssist')
ConvertTo-Json -InputObject $absoluteData -Depth 10 | Set-Content -LiteralPath $absolute.LoadOrderPath
& $installer @absolute | Out-Null
Check ((ReadOrder $absolute).aLoadOrder.Count -eq 3) 'Absolute entry caused duplicate registration'

$duplicate = Fixture 'duplicate' @('core', 'PhobosApproachAssist', 'PhobosApproachAssist|disabled')
Fails { & $installer @duplicate | Out-Null } 'Duplicate Approach Assist'
Check (-not (Test-Path -LiteralPath (Join-Path $duplicate.OstranautsPath 'BepInEx/plugins'))) 'Rejected load order copied files'
$wrongOrder = Fixture 'wrong-order' @('PhobosApproachAssist', 'core')
Fails { & $installer @wrongOrder | Out-Null } 'must load after core'
$noCore = Fixture 'missing-core' @('ExistingMod')
Fails { & $installer @noCore | Out-Null } 'must include enabled core'

$running = Fixture 'running'
$global:PhobosInstallerTestGameRunning = $true
try {
    Fails { & $installer @running | Out-Null } 'Exit Ostranauts'
    Check (-not (Test-Path -LiteralPath (Join-Path $running.OstranautsPath 'BepInEx/plugins'))) 'Running-game refusal copied files'
    & $installer @fresh -VerifyOnly | Out-Null
    Check $true 'Read-only verification remains available while running'
} finally { $global:PhobosInstallerTestGameRunning = $false }

Set-Content -LiteralPath (Join-Path (Split-Path -Parent $dll) 'unexpected.json') -Value '{}'
Fails { & $installer @fresh | Out-Null } 'Unmanaged installed file'
Write-Output "$script:passed installer checks passed using synthetic installations. No gameplay tests performed."
Remove-Variable -Name PhobosInstallerTestGameRunning -Scope Global
