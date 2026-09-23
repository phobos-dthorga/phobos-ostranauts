#requires -Version 7.0
param(
    [string]$PackageRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'),
    # Only read/copied into synthetic fixtures; never loaded or changed.
    [string]$FrameworkDllPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $repoRoot 'scripts/install-mods.ps1'
. (Join-Path $repoRoot 'scripts/installer-support.ps1')
if (-not $FrameworkDllPath) {
    $location = Resolve-InstallLocations '' '' (Join-Path $repoRoot '.local/install-settings.json')
    $frameworkDlls = @(Get-ChildItem -LiteralPath (Join-Path $location.OstranautsPath 'BepInEx/plugins') -Recurse -Filter CraftingFramework.dll -File)
    if ($frameworkDlls.Count -ne 1) { throw 'Supply -FrameworkDllPath with a local Crafting Framework assembly for the test fixture.' }
    $FrameworkDllPath = $frameworkDlls[0].FullName
}
$fixtures = Join-Path $repoRoot ('.local/script-tests/' + [guid]::NewGuid().ToString('N'))
$script:passed = 0
$global:PhobosInstallerTestGameRunning = $false
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
function Fixture([string]$Name, [string[]]$Entries = @('core', 'OCF', 'SWB', 'AutoNavigate|disabled', 'PhobosApproachAssist|disabled')) {
    $root = Join-Path $fixtures $Name
    $game = Join-Path $root 'game'
    $mods = Join-Path $root 'custom native mods'
    New-Item -ItemType Directory -Path (Join-Path $game 'BepInEx/core'), (Join-Path $game 'BepInEx/plugins/Framework'),
        (Join-Path $mods 'OCF'), (Join-Path $mods 'SWB') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'Ostranauts.exe') -Value 'fixture marker'
    Set-Content -LiteralPath (Join-Path $game 'BepInEx/core/BepInEx.dll') -Value 'fixture loader marker'
    Copy-Item -LiteralPath $FrameworkDllPath -Destination (Join-Path $game 'BepInEx/plugins/Framework/CraftingFramework.dll')
    foreach ($provider in @(@{ Folder = 'OCF'; Name = 'Ostranauts Crafting Framework'; Id = '3798573443' }, @{ Folder = 'SWB'; Name = 'Salvage Workshop'; Id = '3798573453' })) {
        ConvertTo-Json -InputObject @(@{ strName = $provider.Name; strWorkshopID = $provider.Id; strModVersion = '0.8.71' }) |
            Set-Content -LiteralPath (Join-Path $mods ($provider.Folder + '/mod_info.json'))
    }
    $order = Join-Path $mods 'loading_order.json'
    ConvertTo-Json -InputObject @(@{ strName = 'Mod Loading Order'; aLoadOrder = $Entries; aIgnorePatterns = @('KeepMe') }) -Depth 10 |
        Set-Content -LiteralPath $order
    return @{ OstranautsPath = $game; LoadOrderPath = $order; PackageRoot = $PackageRoot; NoRememberPaths = $true }
}
function ReadOrder($Fixture) { return @(Get-Content -LiteralPath $Fixture.LoadOrderPath -Raw | ConvertFrom-Json)[0] }
function BackupPath($Output) { return (($Output | Where-Object { $_ -like '*Backups and receipt:*' }) -split 'Backups and receipt: ', 2)[1] }
function InstalledFiles($Fixture) {
    return @(Get-ChildItem -LiteralPath (Join-Path $Fixture.OstranautsPath 'BepInEx/plugins'), (Split-Path -Parent $Fixture.LoadOrderPath) -Recurse -File |
        Sort-Object FullName | ForEach-Object { "$($_.FullName):$((Get-FileHash -LiteralPath $_.FullName).Hash)" }) -join "`n"
}

$fresh = Fixture 'both'
$before = InstalledFiles $fresh
& $installer @fresh -WhatIf | Out-Null
Check ((InstalledFiles $fresh) -eq $before) 'Preview mutated installation'
Fails { & $installer @fresh -VerifyOnly | Out-Null } 'Installation differs'
$backup = BackupPath (& $installer @fresh)
Check (((ReadOrder $fresh).aLoadOrder -join ',') -eq 'core,OCF,SWB,AutoNavigate|disabled,PhobosApproachAssist|disabled,PhobosAutoNav,PhobosShipbreaker') 'Unrelated load order changed'
Check ((ReadOrder $fresh).aIgnorePatterns[0] -eq 'KeepMe') 'Other configuration lost'
& $installer @fresh -VerifyOnly | Out-Null
Check $true 'Combined install verification'
$nativeRoot = Split-Path -Parent $fresh.LoadOrderPath
$artwork = @(Get-ChildItem -LiteralPath (Join-Path $nativeRoot 'PhobosAutoNav/images') -Recurse -File)
Check ($artwork.Count -eq 6) 'Approved artwork missing'
foreach ($image in $artwork) {
    $relative = [IO.Path]::GetRelativePath((Join-Path $nativeRoot 'PhobosAutoNav'), $image.FullName)
    Check ((Get-FileHash -LiteralPath $image.FullName).Hash -eq (Get-FileHash -LiteralPath (Join-Path $PackageRoot "PhobosAutoNav-P0/Mods/PhobosAutoNav/$relative")).Hash) 'Artwork changed during installation'
}
$receipt = Get-Content -LiteralPath (Join-Path $backup 'receipt.json') -Raw | ConvertFrom-Json
Check ($receipt.Status -eq 'Verified files and load order' -and $receipt.Mods.Count -eq 2) 'Receipt incomplete'
Check (@($receipt.ChangedFiles | Where-Object ExistedBefore).Count -eq 0) 'Fresh files not marked for recovery'
$stamp = (Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc
$dll = Join-Path $fresh.OstranautsPath 'BepInEx/plugins/PhobosAutoNav/PhobosAutoNav.dll'
$dllStamp = (Get-Item -LiteralPath $dll).LastWriteTimeUtc
& $installer @fresh | Out-Null
Check ((Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc -eq $stamp -and (Get-Item -LiteralPath $dll).LastWriteTimeUtc -eq $dllStamp) 'Repeat rewrote unchanged files'

$panel = Join-Path $nativeRoot 'PhobosAutoNav/images/phobos/autonav/PhobosAutoNavPanel.png'
Set-Content -LiteralPath $panel -Value 'previous image'
Fails { & $installer @fresh -VerifyOnly | Out-Null } 'Installation differs'
$backup = BackupPath (& $installer @fresh)
Check ((Get-Content -LiteralPath (Join-Path $backup 'PhobosAutoNav/native/images/phobos/autonav/PhobosAutoNavPanel.png') -Raw).Trim() -eq 'previous image') 'Changed artwork not backed up'
& $installer @fresh -VerifyOnly | Out-Null
Check $true 'Repaired installation verified'

$autoOnly = Fixture 'autonav-only' @('core', 'OCF|disabled', 'SWB|disabled')
& $installer @autoOnly -Mods AutoNav | Out-Null
Check (-not (Test-Path -LiteralPath (Join-Path $autoOnly.OstranautsPath 'BepInEx/plugins/PhobosShipbreaker'))) 'Single selection installed Shipbreaker'
Check (((ReadOrder $autoOnly).aLoadOrder -join ',') -eq 'core,OCF|disabled,SWB|disabled,PhobosAutoNav') 'AutoNav enabled other mods'

$disabled = Fixture 'disabled' @('core', 'OCF', 'SWB', 'PhobosAutoNav|disabled', 'PhobosShipbreaker|disabled', 'Keep|edit')
& $installer @disabled | Out-Null
Check (((ReadOrder $disabled).aLoadOrder -join ',') -eq 'core,OCF,SWB,PhobosAutoNav,PhobosShipbreaker,Keep|edit') 'Selected mods not enabled in place'

$missing = Fixture 'missing-dependency' @('core', 'OCF|disabled', 'SWB')
$before = InstalledFiles $missing
Fails { & $installer @missing | Out-Null } 'enabled Crafting Framework and Salvage Workshop'
Check ((InstalledFiles $missing) -eq $before) 'Multi-mod preflight failure partially installed AutoNav'
$wrong = Fixture 'wrong-order' @('core', 'OCF', 'PhobosShipbreaker', 'SWB')
Fails { & $installer @wrong | Out-Null } 'Shipbreaker load order'
$old = Fixture 'old-framework'
$metadataFile = Join-Path (Split-Path -Parent $old.LoadOrderPath) 'OCF/mod_info.json'
(Get-Content -LiteralPath $metadataFile -Raw).Replace('0.8.71', '0.8.70') | Set-Content -LiteralPath $metadataFile
Fails { & $installer @old | Out-Null } 'requires Crafting Framework 0.8.71'
$badDll = Fixture 'wrong-framework-dll'
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosAutoNav-P0/BepInEx/plugins/PhobosAutoNav/PhobosAutoNav.dll') -Destination (Join-Path $badDll.OstranautsPath 'BepInEx/plugins/Framework/CraftingFramework.dll') -Force
Fails { & $installer @badDll | Out-Null } 'unexpected assembly'

# A second broken selected package must prevent the first from being installed.
$badPackages = Join-Path $fixtures 'packages'
New-Item -ItemType Directory -Path $badPackages -Force | Out-Null
foreach ($name in @('PhobosAutoNav-P0', 'PhobosShipbreaker-P0')) { Copy-Item -LiteralPath (Join-Path $PackageRoot $name) -Destination $badPackages -Recurse }
$badVersion = Join-Path $badPackages 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/mod_info.json'
$json = @(Get-Content -LiteralPath $badVersion -Raw | ConvertFrom-Json)
$json[0].strModVersion = '9.9.9'
ConvertTo-Json -InputObject $json | Set-Content -LiteralPath $badVersion
$incomplete = Fixture 'bad-second-package'
$incomplete.PackageRoot = $badPackages
$before = InstalledFiles $incomplete
Fails { & $installer @incomplete | Out-Null } 'versions differ'
Check ((InstalledFiles $incomplete) -eq $before) 'Bad second package partially installed first'

$running = Fixture 'running'
$global:PhobosInstallerTestGameRunning = $true
try {
    $before = InstalledFiles $running
    Fails { & $installer @running | Out-Null } 'Exit Ostranauts'
    Check ((InstalledFiles $running) -eq $before) 'Running game guard wrote files'
    & $installer @running -WhatIf | Out-Null
    & $installer @fresh -VerifyOnly | Out-Null
    Check $true 'Preview and verification available while game running'
} finally { $global:PhobosInstallerTestGameRunning = $false }

$extra = Join-Path $nativeRoot 'PhobosAutoNav/user-notes.md'
Set-Content -LiteralPath $extra -Value 'retain me'
Fails { & $installer @fresh | Out-Null } 'Unmanaged installed file'
Check ((Get-Content -LiteralPath $extra -Raw).Trim() -eq 'retain me') 'User file removed'

$linked = Fixture 'junction'
$outside = Join-Path $fixtures 'outside'
New-Item -ItemType Directory -Path $outside -Force | Out-Null
New-Item -ItemType Junction -Path (Join-Path (Split-Path -Parent $linked.LoadOrderPath) 'PhobosAutoNav') -Target $outside | Out-Null
Fails { & $installer @linked | Out-Null } 'Filesystem link'
Check (@(Get-ChildItem -LiteralPath $outside).Count -eq 0) 'Installation followed a junction'

# Saved paths are tested in a separate file; no real machine settings are changed.
$settings = Join-Path $fixtures 'settings.json'
$savedLocations = [ordered]@{ OstranautsPath = $fresh.OstranautsPath; LoadOrderPath = $fresh.LoadOrderPath }
Save-InstallLocations $savedLocations $settings
$settingsStamp = (Get-Item -LiteralPath $settings).LastWriteTimeUtc
Save-InstallLocations $savedLocations $settings
Check ((Get-Item -LiteralPath $settings).LastWriteTimeUtc -eq $settingsStamp) 'Unchanged saved paths were rewritten'
$resolved = Resolve-InstallLocations '' '' $settings
Check ($resolved.LoadOrderPath -eq $fresh.LoadOrderPath) 'Saved custom Mods path was lost'
$resolved = Resolve-InstallLocations $autoOnly.OstranautsPath $autoOnly.LoadOrderPath $settings
Check ($resolved.LoadOrderPath -eq $autoOnly.LoadOrderPath) 'Explicit paths did not override saved configuration'
Fails { & $installer @autoOnly -Mods AutoNav,AutoNav | Out-Null } 'without duplicates'

# Simulate a disk/copy failure after the first mod was copied. Recovery information
# must already exist and the game must not be told to enable incomplete packages.
$interrupted = Fixture 'interrupted'
$beforeOrderHash = (Get-FileHash -LiteralPath $interrupted.LoadOrderPath).Hash
function Copy-Item {
    [CmdletBinding()]
    param([string]$LiteralPath, [string]$Destination, [switch]$Force)
    if ($Destination -like '*PhobosShipbreaker.dll') { throw 'Simulated copy failure' }
    Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters
}
$failure = ''
try { & $installer @interrupted | Out-Null }
catch { $failure = $_.Exception.Message }
finally { Remove-Item Function:/Copy-Item }
Check ($failure -match 'backups in (.+?)\. Simulated copy failure') 'Interrupted copy did not report recovery directory'
$recovery = $Matches[1]
$receipt = Get-Content -LiteralPath (Join-Path $recovery 'receipt.json') -Raw | ConvertFrom-Json
Check ($receipt.Status -eq 'Copying' -and $receipt.ChangedFiles.Count -eq 16) 'Interrupted installation lacks recovery manifest'
Check ((Get-FileHash -LiteralPath $interrupted.LoadOrderPath).Hash -eq $beforeOrderHash) 'Incomplete packages were enabled'
Check ((Get-FileHash -LiteralPath (Join-Path $recovery 'loading_order.before.json')).Hash -eq $beforeOrderHash) 'Recovery load-order backup missing'
& $installer @interrupted | Out-Null
& $installer @interrupted -VerifyOnly | Out-Null
Check $true 'Interrupted installation can be completed by rerunning'

Write-Output "$script:passed multi-mod installer checks passed using synthetic installations. No gameplay tests performed."
Remove-Variable -Name PhobosInstallerTestGameRunning -Scope Global
