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
$overrideDirectory = Join-Path $fresh.OstranautsPath 'BepInEx/config/PhobosTranslations/phobosgekko.ostranauts.shipbreaker'
New-Item -ItemType Directory -Path $overrideDirectory -Force | Out-Null
$overrideFile = Join-Path $overrideDirectory 'fr.json'
Set-Content -LiteralPath $overrideFile -Value '{"community":"Preserve my translation"}' -Encoding utf8
$overrideHash = (Get-FileHash -LiteralPath $overrideFile).Hash
$before = InstalledFiles $fresh
& $installer @fresh -WhatIf | Out-Null
Check ((InstalledFiles $fresh) -eq $before) 'Preview mutated installation'
Fails { & $installer @fresh -VerifyOnly | Out-Null } 'Installation differs'
$backup = BackupPath (& $installer @fresh)
Check (((ReadOrder $fresh).aLoadOrder -join ',') -eq 'core,OCF,SWB,AutoNavigate|disabled,PhobosApproachAssist|disabled,PhobosFramework,PhobosAutoNav,PhobosShipbreaker') 'Unrelated load order changed'
Check ((ReadOrder $fresh).aIgnorePatterns[0] -eq 'KeepMe') 'Other configuration lost'
& $installer @fresh -VerifyOnly | Out-Null
Check $true 'Combined install verification'
foreach ($mod in @('PhobosFramework', 'PhobosShipbreaker', 'PhobosAutoNav')) {
    $catalog = Join-Path $fresh.OstranautsPath "BepInEx/plugins/$mod/translations/en.json"
    $sourceCatalog = Join-Path $PackageRoot "$mod-P0/BepInEx/plugins/$mod/translations/en.json"
    Check ((Get-FileHash -LiteralPath $catalog).Hash -eq (Get-FileHash -LiteralPath $sourceCatalog).Hash) 'Translation catalog was not delivered unchanged'
}
Check ((Get-FileHash -LiteralPath $overrideFile).Hash -eq $overrideHash) 'Installation changed a community translation override'
$nativeRoot = Split-Path -Parent $fresh.LoadOrderPath
$artwork = @(Get-ChildItem -LiteralPath (Join-Path $nativeRoot 'PhobosAutoNav/images') -Recurse -File)
Check ($artwork.Count -eq 6) 'Approved artwork missing'
foreach ($image in $artwork) {
    $relative = [IO.Path]::GetRelativePath((Join-Path $nativeRoot 'PhobosAutoNav'), $image.FullName)
    Check ((Get-FileHash -LiteralPath $image.FullName).Hash -eq (Get-FileHash -LiteralPath (Join-Path $PackageRoot "PhobosAutoNav-P0/Mods/PhobosAutoNav/$relative")).Hash) 'Artwork changed during installation'
}
$receipt = Get-Content -LiteralPath (Join-Path $backup 'receipt.json') -Raw | ConvertFrom-Json
$expectedRecoveryFiles = @($receipt.Files | ForEach-Object { "$($_.Backup):$($_.Hash)" } | Sort-Object) -join "`n"
Check (@(Get-ChildItem -LiteralPath (Join-Path $nativeRoot 'PhobosShipbreaker/images') -Recurse -Filter '*.png').Count -eq 27) 'Shipbreaker artwork missing from installation'
Check ($receipt.Status -eq 'Verified files and load order' -and $receipt.Mods.Count -eq 3) 'Receipt incomplete'
Check (@($receipt.ChangedFiles | Where-Object ExistedBefore).Count -eq 0) 'Fresh files not marked for recovery'
$stamp = (Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc
$dll = Join-Path $fresh.OstranautsPath 'BepInEx/plugins/PhobosAutoNav/PhobosAutoNav.dll'
$dllStamp = (Get-Item -LiteralPath $dll).LastWriteTimeUtc
& $installer @fresh | Out-Null
Check ((Get-Item -LiteralPath $fresh.LoadOrderPath).LastWriteTimeUtc -eq $stamp -and (Get-Item -LiteralPath $dll).LastWriteTimeUtc -eq $dllStamp) 'Repeat rewrote unchanged files'
Check ((Get-FileHash -LiteralPath $overrideFile).Hash -eq $overrideHash) 'Repeat installation changed a community translation override'

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
Check (((ReadOrder $autoOnly).aLoadOrder -join ',') -eq 'core,OCF|disabled,SWB|disabled,PhobosFramework,PhobosAutoNav') 'AutoNav selected only its own framework dependency'
Check (Test-Path -LiteralPath (Join-Path $autoOnly.OstranautsPath 'BepInEx/plugins/PhobosFramework')) 'AutoNav shared economy provider missing'

$libraryOnly = Fixture 'framework-only' @('core', 'OCF|disabled', 'SWB|disabled')
& $installer @libraryOnly -Mods Framework | Out-Null
& $installer @libraryOnly -Mods Framework -VerifyOnly | Out-Null
Check (((ReadOrder $libraryOnly).aLoadOrder -join ',') -eq 'core,OCF|disabled,SWB|disabled,PhobosFramework') 'Framework requires or enables upstream crafting content'
Check (@(Get-ChildItem -LiteralPath (Join-Path $libraryOnly.OstranautsPath 'BepInEx/plugins') -Recurse -Filter PhobosFramework.dll).Count -eq 1) 'Framework installs one shared provider'

$duplicate = Fixture 'duplicate-phobos-provider'
$extraProvider = Join-Path $duplicate.OstranautsPath 'BepInEx/plugins/ForeignCopy'
New-Item -ItemType Directory -Path $extraProvider -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosFramework-P0/BepInEx/plugins/PhobosFramework/PhobosFramework.dll') -Destination $extraProvider
$before = InstalledFiles $duplicate
Fails { & $installer @duplicate | Out-Null } 'Duplicate Phobos Framework provider'
Check ((InstalledFiles $duplicate) -eq $before) 'Duplicate-provider preflight partially installed content'

# Represent a newer provider used by another mod: never replace it with our older
# prepared package. This inert fixture assembly is inspected, not loaded as a mod.
$newerLibrary = Fixture 'newer-phobos-provider'
$newerDirectory = Join-Path $newerLibrary.OstranautsPath 'BepInEx/plugins/PhobosFramework'
New-Item -ItemType Directory -Path $newerDirectory -Force | Out-Null
$fixtureSource = Join-Path $fixtures 'newer-provider-source'
New-Item -ItemType Directory -Path $fixtureSource -Force | Out-Null
Set-Content -LiteralPath (Join-Path $fixtureSource 'Fixture.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>PhobosFramework</AssemblyName><Version>9.0.0</Version></PropertyGroup></Project>'
Set-Content -LiteralPath (Join-Path $fixtureSource 'Fixture.cs') -Value 'public sealed class NewerFrameworkFixture { }'
& dotnet build (Join-Path $fixtureSource 'Fixture.csproj') -c Release -o $newerDirectory --nologo -v quiet | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not build the inert newer-provider fixture.' }
$before = InstalledFiles $newerLibrary
Fails { & $installer @newerLibrary | Out-Null } 'newer Phobos Framework is installed'
Check ((InstalledFiles $newerLibrary) -eq $before) 'Shared-provider downgrade guard partially changed an installation'

$missingLibraryPackages = Join-Path $fixtures 'without-framework-package'
New-Item -ItemType Directory -Path $missingLibraryPackages -Force | Out-Null
foreach ($name in @('PhobosAutoNav-P0', 'PhobosShipbreaker-P0')) {
    Copy-Item -LiteralPath (Join-Path $PackageRoot $name) -Destination $missingLibraryPackages -Recurse
}
$missingLibrary = Fixture 'missing-phobos-package'
$missingLibrary.PackageRoot = $missingLibraryPackages
$before = InstalledFiles $missingLibrary
Fails { & $installer @missingLibrary | Out-Null } 'Prepared package missing'
Check ((InstalledFiles $missingLibrary) -eq $before) 'Missing framework package partially installed a consumer'

$disabled = Fixture 'disabled' @('core', 'OCF', 'SWB', 'PhobosAutoNav|disabled', 'PhobosShipbreaker|disabled', 'Keep|edit')
& $installer @disabled | Out-Null
Check (((ReadOrder $disabled).aLoadOrder -join ',') -eq 'core,OCF,SWB,PhobosAutoNav,PhobosShipbreaker,Keep|edit,PhobosFramework') 'Selected mods not enabled in place'

$independent = Fixture 'independent' @('core')
Remove-Item -LiteralPath (Join-Path $independent.OstranautsPath 'BepInEx/plugins/Framework/CraftingFramework.dll')
& $installer @independent -Mods Shipbreaker | Out-Null
& $installer @independent -Mods Shipbreaker -VerifyOnly | Out-Null
Check (((ReadOrder $independent).aLoadOrder -join ',') -eq 'core,PhobosFramework,PhobosShipbreaker') 'Independent Shipbreaker acquired Workshop requirements'
$optionalDisabled = Fixture 'optional-disabled' @('core', 'OCF|disabled', 'SWB|disabled')
& $installer @optionalDisabled -Mods Shipbreaker | Out-Null
Check (((ReadOrder $optionalDisabled).aLoadOrder -join ',') -eq 'core,OCF|disabled,SWB|disabled,PhobosFramework,PhobosShipbreaker') 'Optional mods were enabled'
$wrong = Fixture 'legacy-wrong-order' @('core', 'OCF', 'PhobosShipbreaker', 'SWB')
# Retain the legacy preflight for users explicitly installing a pre-0.2 package.
Fails { Assert-ShipbreakerDependencies (ReadOrder $wrong).aLoadOrder (Split-Path -Parent $wrong.LoadOrderPath) $wrong.OstranautsPath 0 } 'Shipbreaker load order'
& $installer @wrong -Mods Shipbreaker | Out-Null
Check $true 'Independent version does not depend on optional Workshop load order'
$old = Fixture 'old-optional-framework' @('core', 'OCF', 'SWB', 'PhobosShipbreaker')
$metadataFile = Join-Path (Split-Path -Parent $old.LoadOrderPath) 'OCF/mod_info.json'
(Get-Content -LiteralPath $metadataFile -Raw).Replace('0.8.71', '0.8.70') | Set-Content -LiteralPath $metadataFile
Fails { Assert-ShipbreakerDependencies (ReadOrder $old).aLoadOrder (Split-Path -Parent $old.LoadOrderPath) $old.OstranautsPath 0 } 'requires Crafting Framework 0.8.71'
& $installer @old -Mods Shipbreaker | Out-Null
Check $true 'Independent Shipbreaker tolerates unrelated older OCF metadata'

# Retire our old OCF recipe pack using normal backed-up replacement, not deletion.
$oldPackPath = Join-Path (Split-Path -Parent $independent.LoadOrderPath) 'PhobosShipbreaker/crafting/recipes.json'
$legacyPack = '{"schemaVersion":1,"recipes":[{"id":"PhobosBuildShipbreaker"}],"stockAdditions":[]}'
Set-Content -LiteralPath $oldPackPath -Value $legacyPack
$backup = BackupPath (& $installer @independent -Mods Shipbreaker)
Check ((Get-Content -LiteralPath (Join-Path $backup 'PhobosShipbreaker/native/crafting/recipes.json') -Raw).Trim() -eq $legacyPack) 'Retired recipe pack was not backed up'
$retired = Get-Content -LiteralPath $oldPackPath -Raw | ConvertFrom-Json
Check (@($retired.recipes).Count -eq 0 -and @($retired.stockAdditions).Count -eq 0) 'Old OCF recipe ownership was not retired'
Check (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $independent.LoadOrderPath) 'PhobosShipbreaker/framework/recipes.json')) 'New provider recipe pack missing'
# A second broken selected package must prevent the first from being installed.
$badPackages = Join-Path $fixtures 'packages'
New-Item -ItemType Directory -Path $badPackages -Force | Out-Null
foreach ($name in @('PhobosAutoNav-P0', 'PhobosShipbreaker-P0', 'PhobosFramework-P0')) { Copy-Item -LiteralPath (Join-Path $PackageRoot $name) -Destination $badPackages -Recurse }
$badVersion = Join-Path $badPackages 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/mod_info.json'
$json = @(Get-Content -LiteralPath $badVersion -Raw | ConvertFrom-Json)
$json[0].strModVersion = '9.9.9'
ConvertTo-Json -InputObject $json | Set-Content -LiteralPath $badVersion
$incomplete = Fixture 'bad-second-package'
$incomplete.PackageRoot = $badPackages
$before = InstalledFiles $incomplete
Fails { & $installer @incomplete | Out-Null } 'versions differ'
Check ((InstalledFiles $incomplete) -eq $before) 'Bad second package partially installed first'

# Missing shader input is also an incomplete package, before any mod is copied.
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/mod_info.json') -Destination $badVersion -Force
$missingCatalog = Join-Path $badPackages 'PhobosShipbreaker-P0/BepInEx/plugins/PhobosShipbreaker/translations/en.json'
$catalogContents = [IO.File]::ReadAllBytes($missingCatalog)
Remove-Item -LiteralPath $missingCatalog
Fails { & $installer @incomplete | Out-Null } 'PhobosShipbreaker/translations/en.json'
Check ((InstalledFiles $incomplete) -eq $before) 'Missing English catalog partially installed a package'
[IO.File]::WriteAllBytes($missingCatalog, $catalogContents)
# A coherent older provider package must still be rejected before any copying.
# Only inert synthetic assemblies are built here; the installed game is untouched.
$olderOutput = Join-Path $fixtures 'older-provider-output'
& dotnet build (Join-Path $fixtureSource 'Fixture.csproj') -c Release -p:Version=0.4.99 -o $olderOutput --nologo -v quiet | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not build the inert older-provider fixture.' }
$frameworkMetadataRelative = 'PhobosFramework-P0/Mods/PhobosFramework/mod_info.json'
$frameworkDllRelative = 'PhobosFramework-P0/BepInEx/plugins/PhobosFramework/PhobosFramework.dll'
$olderMetadata = Join-Path $badPackages $frameworkMetadataRelative
$olderInfo = @(Get-Content -LiteralPath $olderMetadata -Raw | ConvertFrom-Json)
$olderInfo[0].strModVersion = '0.4.99'
ConvertTo-Json -InputObject $olderInfo | Set-Content -LiteralPath $olderMetadata
Copy-Item -LiteralPath (Join-Path $olderOutput 'PhobosFramework.dll') -Destination (Join-Path $badPackages $frameworkDllRelative) -Force
Fails { & $installer @incomplete | Out-Null } 'Selected equipment requires Phobos Framework 0.7.0'
Check ((InstalledFiles $incomplete) -eq $before) 'Old pairing provider partially installed packages'
foreach ($relative in @($frameworkMetadataRelative, $frameworkDllRelative)) {
    Copy-Item -LiteralPath (Join-Path $PackageRoot $relative) -Destination (Join-Path $badPackages $relative) -Force
}
# Regression: a DLL-only framework can load while the native menu reports Missing.
# Keep a tracked empty definitions file so packaging and file-only installation
# actually deliver the data directory required by the native loader.
$frameworkData = Join-Path $badPackages 'PhobosFramework-P0/Mods/PhobosFramework/data/conditions/phobos_framework.json'
Remove-Item -LiteralPath $frameworkData
$before = InstalledFiles $incomplete
Fails { & $installer @incomplete | Out-Null } 'PhobosFramework/data/conditions/phobos_framework.json'
Check ((InstalledFiles $incomplete) -eq $before) 'Missing Framework data partially installed packages'
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosFramework-P0/Mods/PhobosFramework/data/conditions/phobos_framework.json') -Destination $frameworkData
Check (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $libraryOnly.LoadOrderPath) 'PhobosFramework/data/conditions/phobos_framework.json')) 'Framework native data missing after installation'
$badLegacyPack = Join-Path $badPackages 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/crafting/recipes.json'
Set-Content -LiteralPath $badLegacyPack -Value $legacyPack
Fails { & $installer @incomplete | Out-Null } 'must retire its old Crafting Framework recipe pack'
Check ((InstalledFiles $incomplete) -eq $before) 'Duplicate recipe ownership preflight partially installed content'
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/crafting/recipes.json') -Destination $badLegacyPack -Force
$actualPack = Join-Path $badPackages 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/framework/recipes.json'
Remove-Item -LiteralPath $actualPack
Fails { & $installer @incomplete | Out-Null } 'framework/recipes.json'
Check ((InstalledFiles $incomplete) -eq $before) 'Missing active construction pack partially installed content'
Copy-Item -LiteralPath (Join-Path $PackageRoot 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/framework/recipes.json') -Destination $actualPack
$missingNormal = Join-Path $badPackages 'PhobosShipbreaker-P0/Mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosShipbreakerInstalledNormal.png'
foreach ($component in @('PhobosHullChute', 'PhobosExteriorGrabber', 'PhobosResidueCollector')) {
    $relative = "PhobosShipbreaker-P0/Mods/PhobosShipbreaker/images/phobos/shipbreaker/$($component)Normal.png"
    $missingHullNormal = Join-Path $badPackages $relative
    Remove-Item -LiteralPath $missingHullNormal
    Fails { & $installer @incomplete | Out-Null } "$($component)Normal.png"
    Check ((InstalledFiles $incomplete) -eq $before) 'Missing machinery artwork partially installed packages'
    Copy-Item -LiteralPath (Join-Path $PackageRoot $relative) -Destination $missingHullNormal
}
Remove-Item -LiteralPath $missingNormal
Fails { & $installer @incomplete | Out-Null } 'PhobosShipbreakerInstalledNormal.png'
Check ((InstalledFiles $incomplete) -eq $before) 'Missing artwork partially installed first mod'

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
$actualRecoveryFiles = @($receipt.ChangedFiles | ForEach-Object { "$($_.Backup):$($_.Hash)" } | Sort-Object) -join "`n"
Check ($receipt.Status -eq 'Copying' -and $actualRecoveryFiles -eq $expectedRecoveryFiles) 'Interrupted installation lacks complete recovery manifest'
Check ((Get-FileHash -LiteralPath $interrupted.LoadOrderPath).Hash -eq $beforeOrderHash) 'Incomplete packages were enabled'
Check ((Get-FileHash -LiteralPath (Join-Path $recovery 'loading_order.before.json')).Hash -eq $beforeOrderHash) 'Recovery load-order backup missing'
& $installer @interrupted | Out-Null
& $installer @interrupted -VerifyOnly | Out-Null
Check $true 'Interrupted installation can be completed by rerunning'

Write-Output "$script:passed multi-mod installer checks passed using synthetic installations. No gameplay tests performed."
Remove-Variable -Name PhobosInstallerTestGameRunning -Scope Global
