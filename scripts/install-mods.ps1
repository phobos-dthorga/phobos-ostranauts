#requires -Version 7.0
# Prepared packages are installed locally; this script never builds, downloads or launches anything.
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('AutoNav', 'Shipbreaker', 'Framework', 'Agriculture', 'Manufacturing', 'WarDeclared')]
    [string[]]$Mods = @('AutoNav', 'Shipbreaker'),
    [string]$OstranautsPath,
    [string]$LoadOrderPath,
    [string]$PackageRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'),
    # Override an unpacked package only when installing one mod.
    [string]$PackagePath,
    [switch]$VerifyOnly,
    # Update existing mods' menu/Workshop covers without changing gameplay or enabling mods.
    [switch]$PreviewsOnly,
    # Validate the existing dependency without replacing any of its files.
    [switch]$KeepInstalledFramework,
    # Remove only the known, non-operational Manufacturing 0.0.1 scaffold DLL from loading.
    [switch]$HoldManufacturing,
    # Test override for the catalogue of files earlier packages installed and later ones no longer ship.
    [string]$RetiredCataloguePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'config/retired-installed-files.json'),
    [switch]$NoRememberPaths
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$settingsFile = Join-Path $repoRoot '.local/install-settings.json'
. (Join-Path $PSScriptRoot 'installer-support.ps1')

if ($Mods.Count -eq 0 -or @($Mods | Select-Object -Unique).Count -ne $Mods.Count) {
    throw 'Choose at least one mod, without duplicates.'
}
if ($PackagePath -and $Mods.Count -ne 1) { throw 'PackagePath requires exactly one selected mod.' }
if ($HoldManufacturing -and ('Manufacturing' -in $Mods -or $PreviewsOnly)) { throw 'Holding Manufacturing cannot also select it or use preview-only installation.' }
$overrideMod = if ($PackagePath) { $Mods[0] } else { $null }
# Shipbreaker 0.1.5+ uses one separately installed Phobos Framework provider.
# A package override selects the requested mod only; dependencies use PackageRoot.
$needsPhobosFramework = $false
$independentShipbreaker = $false
$minimumPhobosFramework = [version]'0.1.0'
$minimumAutoNav = $null
if ('Shipbreaker' -in $Mods) {
    $shipPackage = if ($overrideMod -eq 'Shipbreaker') { $PackagePath } else { Join-Path $PackageRoot 'PhobosShipbreaker-P0' }
    $shipMetadata = Join-Path $shipPackage 'Mods/PhobosShipbreaker/mod_info.json'
    if (Test-Path -LiteralPath $shipMetadata -PathType Leaf) {
        $shipInfo = @(Get-Content -LiteralPath $shipMetadata -Raw | ConvertFrom-Json)
        if ($shipInfo.Count -ne 1) { throw 'Expected exactly one native mod metadata entry for PhobosShipbreaker.' }
        $needsPhobosFramework = [version]$shipInfo[0].strModVersion -ge [version]'0.1.5'
        $independentShipbreaker = [version]$shipInfo[0].strModVersion -ge [version]'0.2.0'
        if ($independentShipbreaker) { $minimumPhobosFramework = [version]'0.2.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.3.0') { $minimumPhobosFramework = [version]'0.3.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.4.0') { $minimumPhobosFramework = [version]'0.4.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.5.0') { $minimumPhobosFramework = [version]'0.5.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.6.0') { $minimumPhobosFramework = [version]'0.6.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.7.0') { $minimumPhobosFramework = [version]'0.7.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.8.0') { $minimumPhobosFramework = [version]'0.8.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.9.0') { $minimumPhobosFramework = [version]'0.9.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.10.0') { $minimumPhobosFramework = [version]'0.10.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.10.1') { $minimumPhobosFramework = [version]'0.12.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.11.0') { $minimumPhobosFramework = [version]'0.13.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.11.1') { $minimumPhobosFramework = [version]'0.15.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.12.0') { $minimumPhobosFramework = [version]'0.16.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.14.0') { $minimumPhobosFramework = [version]'0.17.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.16.0') { $minimumPhobosFramework = [version]'0.19.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.17.0') { $minimumPhobosFramework = [version]'0.20.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.19.0') { $minimumPhobosFramework = [version]'0.21.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.19.1') { $minimumPhobosFramework = [version]'0.21.1' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.20.0') { $minimumPhobosFramework = [version]'0.22.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.21.0') { $minimumPhobosFramework = [version]'0.23.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.22.0' -and -not $PreviewsOnly) {
            $minimumAutoNav = [version]'0.16.0'
            $Mods = @('AutoNav') + @($Mods | Where-Object { $_ -ne 'AutoNav' })
        }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.23.0' -and $minimumPhobosFramework -lt [version]'0.28.0') { $minimumPhobosFramework = [version]'0.28.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.28.2' -and $minimumPhobosFramework -lt [version]'0.29.0') { $minimumPhobosFramework = [version]'0.29.0' }
        if ([version]$shipInfo[0].strModVersion -ge [version]'0.29.0' -and $minimumPhobosFramework -lt [version]'0.30.0') { $minimumPhobosFramework = [version]'0.30.0' }
        $minimumPhobosFramework = Get-MaintainedDependencyMinimum 'Shipbreaker.Framework' ([version]$shipInfo[0].strModVersion) $minimumPhobosFramework
        if (-not $PreviewsOnly) { $minimumAutoNav = Get-MaintainedDependencyMinimum 'Shipbreaker.AutoNav' ([version]$shipInfo[0].strModVersion) $minimumAutoNav }
        if ($needsPhobosFramework) { $Mods = @('Framework') + @($Mods | Where-Object { $_ -ne 'Framework' }) }
    }
}
if ('AutoNav' -in $Mods) {
    $navPackage = if ($overrideMod -eq 'AutoNav') { $PackagePath } else { Join-Path $PackageRoot 'PhobosAutoNav-P0' }
    $navMetadata = Join-Path $navPackage 'Mods/PhobosAutoNav/mod_info.json'
    if (Test-Path -LiteralPath $navMetadata -PathType Leaf) {
        $navInfo = @(Get-Content -LiteralPath $navMetadata -Raw | ConvertFrom-Json)
        if ($navInfo.Count -ne 1) { throw 'Expected exactly one native mod metadata entry for PhobosAutoNav.' }
        if ([version]$navInfo[0].strModVersion -ge [version]'0.2.0') {
            $needsPhobosFramework = $true
            if ($minimumPhobosFramework -lt [version]'0.6.0') { $minimumPhobosFramework = [version]'0.6.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.3.0' -and $minimumPhobosFramework -lt [version]'0.7.0') { $minimumPhobosFramework = [version]'0.7.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.5.0' -and $minimumPhobosFramework -lt [version]'0.11.0') { $minimumPhobosFramework = [version]'0.11.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.8.1' -and $minimumPhobosFramework -lt [version]'0.12.0') { $minimumPhobosFramework = [version]'0.12.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.10.0' -and $minimumPhobosFramework -lt [version]'0.14.0') { $minimumPhobosFramework = [version]'0.14.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.10.1' -and $minimumPhobosFramework -lt [version]'0.15.0') { $minimumPhobosFramework = [version]'0.15.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.11.1' -and $minimumPhobosFramework -lt [version]'0.17.0') { $minimumPhobosFramework = [version]'0.17.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.14.0' -and $minimumPhobosFramework -lt [version]'0.21.0') { $minimumPhobosFramework = [version]'0.21.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.14.1' -and $minimumPhobosFramework -lt [version]'0.21.2') { $minimumPhobosFramework = [version]'0.21.2' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.15.0' -and $minimumPhobosFramework -lt [version]'0.23.0') { $minimumPhobosFramework = [version]'0.23.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.17.0' -and $minimumPhobosFramework -lt [version]'0.26.0') { $minimumPhobosFramework = [version]'0.26.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.20.3' -and $minimumPhobosFramework -lt [version]'0.29.0') { $minimumPhobosFramework = [version]'0.29.0' }
            if ([version]$navInfo[0].strModVersion -ge [version]'0.21.0' -and $minimumPhobosFramework -lt [version]'0.30.0') { $minimumPhobosFramework = [version]'0.30.0' }
            $minimumPhobosFramework = Get-MaintainedDependencyMinimum 'AutoNav.Framework' ([version]$navInfo[0].strModVersion) $minimumPhobosFramework
            $Mods = @('Framework') + @($Mods | Where-Object { $_ -ne 'Framework' })
        }
    }
}
if ('Agriculture' -in $Mods) {
    $needsPhobosFramework = $true
    if ($minimumPhobosFramework -lt [version]'0.17.0') { $minimumPhobosFramework = [version]'0.17.0' }
    $farmPackage = if ($overrideMod -eq 'Agriculture') { $PackagePath } else { Join-Path $PackageRoot 'PhobosAgriculture-P0' }
    $farmMetadata = Join-Path $farmPackage 'Mods/PhobosAgriculture/mod_info.json'
    if (Test-Path -LiteralPath $farmMetadata -PathType Leaf) {
        $farmInfo = @(Get-Content -LiteralPath $farmMetadata -Raw | ConvertFrom-Json)
        if ($farmInfo.Count -ne 1) { throw 'Expected exactly one native mod metadata entry for PhobosAgriculture.' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.4.0' -and $minimumPhobosFramework -lt [version]'0.18.0') { $minimumPhobosFramework = [version]'0.18.0' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.5.0' -and $minimumPhobosFramework -lt [version]'0.19.0') { $minimumPhobosFramework = [version]'0.19.0' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.6.0' -and $minimumPhobosFramework -lt [version]'0.20.0') { $minimumPhobosFramework = [version]'0.20.0' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.8.0' -and $minimumPhobosFramework -lt [version]'0.21.0') { $minimumPhobosFramework = [version]'0.21.0' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.9.0' -and $minimumPhobosFramework -lt [version]'0.22.0') { $minimumPhobosFramework = [version]'0.22.0' }
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.11.0' -and $minimumPhobosFramework -lt [version]'0.28.0') { $minimumPhobosFramework = [version]'0.28.0' }
        $minimumPhobosFramework = Get-MaintainedDependencyMinimum 'Agriculture.Framework' ([version]$farmInfo[0].strModVersion) $minimumPhobosFramework
        if ([version]$farmInfo[0].strModVersion -ge [version]'0.10.0' -and $minimumPhobosFramework -lt [version]'0.23.0') { $minimumPhobosFramework = [version]'0.23.0' }
    }
    $Mods = @('Framework') + @($Mods | Where-Object { $_ -ne 'Framework' })
}
if ('Manufacturing' -in $Mods) {
    $needsPhobosFramework = $true
    if ($minimumPhobosFramework -lt [version]'0.17.0') { $minimumPhobosFramework = [version]'0.17.0' }
    $shopPackage = if ($overrideMod -eq 'Manufacturing') { $PackagePath } else { Join-Path $PackageRoot 'PhobosManufacturing-P0' }
    $shopMetadata = Join-Path $shopPackage 'Mods/PhobosManufacturing/mod_info.json'
    if (Test-Path -LiteralPath $shopMetadata -PathType Leaf) {
        $shopInfo = @(Get-Content -LiteralPath $shopMetadata -Raw | ConvertFrom-Json)
        if ($shopInfo.Count -ne 1) { throw 'Expected exactly one native mod metadata entry for PhobosManufacturing.' }
        # The 0.0.1 scaffold kept the 0.17.0 floor; operational packages follow the maintained catalogue.
        $minimumPhobosFramework = Get-MaintainedDependencyMinimum 'Manufacturing.Framework' ([version]$shopInfo[0].strModVersion) $minimumPhobosFramework
    }
    $Mods = @('Framework') + @($Mods | Where-Object { $_ -ne 'Framework' })
}
if ('WarDeclared' -in $Mods) {
    $needsPhobosFramework = $true
    $warPackage = if ($overrideMod -eq 'WarDeclared') { $PackagePath } else { Join-Path $PackageRoot 'PhobosWarDeclared-P0' }
    $warMetadata = Join-Path $warPackage 'Mods/PhobosWarDeclared/mod_info.json'
    if (Test-Path -LiteralPath $warMetadata -PathType Leaf) {
        $warInfo = @(Get-Content -LiteralPath $warMetadata -Raw | ConvertFrom-Json)
        if ($warInfo.Count -ne 1) { throw 'Expected exactly one native mod metadata entry for PhobosWarDeclared.' }
        $minimumPhobosFramework = Get-MaintainedDependencyMinimum 'WarDeclared.Framework' ([version]$warInfo[0].strModVersion) $minimumPhobosFramework
    }
    # Every War Has Been Declared package needs the build-site services first shipped in Framework 0.43.0.
    if ($minimumPhobosFramework -lt [version]'0.43.0') { $minimumPhobosFramework = [version]'0.43.0' }
    $Mods = @('Framework') + @($Mods | Where-Object { $_ -ne 'Framework' })
}
$locations = Resolve-InstallLocations $OstranautsPath $LoadOrderPath $settingsFile
$gameRoot = $locations.OstranautsPath
$orderFile = $locations.LoadOrderPath
$modRoot = Split-Path -Parent $orderFile
Assert-NoLinks $orderFile
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx/core/BepInEx.dll') -PathType Leaf)) {
    throw 'BepInEx 5 must already be installed in OstranautsPath.'
}
if (-not $VerifyOnly -and -not $WhatIfPreference -and (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue)) {
    throw 'Exit Ostranauts normally before installing. No files were changed.'
}

$orderHash = (Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash
$order = Get-Content -LiteralPath $orderFile -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
$records = @($order | Where-Object { $_.strName -eq 'Mod Loading Order' })
if ($records.Count -ne 1 -or -not $records[0].ContainsKey('aLoadOrder')) { throw 'Invalid loading_order.json structure.' }
$record = $records[0]
$entries = @($record.aLoadOrder)
$coreIndex = [Array]::IndexOf($entries, 'core')
if ($coreIndex -lt 0) { throw 'Load order must include enabled core.' }
if (@($entries | Where-Object { $_.Split('|')[0] -eq 'core' }).Count -ne 1) { throw 'Duplicate core entries.' }

# Preflight every selected package before making any installation changes.
$files = @()
$plans = @()
$changeOrder = $false
$heldScaffold = $null
# Files earlier packages installed that this package no longer ships, matched by exact hash only.
$retiredFiles = @()
if ($HoldManufacturing) {
    $scaffoldDll = Join-Path $gameRoot 'BepInEx/plugins/PhobosManufacturing/PhobosManufacturing.dll'
    Assert-NoLinks $scaffoldDll
    if (@($entries | Where-Object { $_.Split('|')[0] -eq 'PhobosManufacturing' -and $_ -ne 'PhobosManufacturing|disabled' }).Count -gt 0) {
        throw 'HoldManufacturing requires the native Manufacturing entry already disabled or absent.'
    }
    if (Test-Path -LiteralPath $scaffoldDll -PathType Leaf) {
        $identity = [Reflection.AssemblyName]::GetAssemblyName($scaffoldDll)
        if ($identity.Name -ne 'PhobosManufacturing' -or $identity.Version -ne [version]'0.0.1.0') {
            throw 'HoldManufacturing only handles the known non-operational 0.0.1 scaffold; other versions require review.'
        }
        $heldScaffold = [pscustomobject]@{ Target = $scaffoldDll; Backup = 'held/PhobosManufacturing.dll'; Hash = (Get-FileHash -LiteralPath $scaffoldDll -Algorithm SHA256).Hash }
    }
}
foreach ($mod in $Mods) {
    $id = 'Phobos' + $mod
    $label = switch ($mod) { 'AutoNav' { 'Auto Nav' } 'Shipbreaker' { 'Shipbreaker' } 'Framework' { 'Framework' } 'Agriculture' { 'Agriculture' } 'Manufacturing' { 'Manufacturing' } 'WarDeclared' { 'War Has Been Declared' } }
    $package = if ($overrideMod -eq $mod) { $PackagePath } else { Join-Path $PackageRoot ($id + '-P0') }
    if (-not (Test-Path -LiteralPath $package -PathType Container)) {
        throw "Prepared package missing: $package. Run the corresponding build script first."
    }
    $package = (Resolve-Path -LiteralPath $package).Path
    $nativeSource = Join-Path $package "Mods/$id"
    $pluginSource = Join-Path $package "BepInEx/plugins/$id"
    $nativeTarget = Join-Path $modRoot $id
    $pluginTarget = Join-Path $gameRoot "BepInEx/plugins/$id"
    $retainFramework = $KeepInstalledFramework -and $mod -eq 'Framework'
    if ($retainFramework) {
        $nativeSource = $nativeTarget
        $pluginSource = $pluginTarget
    }
    if ((Test-Within $nativeTarget $pluginTarget) -or (Test-Within $pluginTarget $nativeTarget)) {
        throw 'Native and plugin destinations must be separate folders. Check LoadOrderPath.'
    }
    foreach ($folder in @($nativeSource, $pluginSource, $nativeTarget, $pluginTarget)) { Assert-NoLinks $folder -Tree }
    foreach ($source in @($nativeSource, $pluginSource)) {
        foreach ($target in @($nativeTarget, $pluginTarget)) {
            if (-not $retainFramework -and ((Test-Within $source $target) -or (Test-Within $target $source))) { throw 'PackagePath must be separate from the installed files.' }
        }
    }
    $metadata = @(Get-Content -LiteralPath (Join-Path $nativeSource 'mod_info.json') -Raw | ConvertFrom-Json)
    if ($metadata.Count -ne 1) { throw "Expected exactly one native mod metadata entry for $id." }
    if ($PreviewsOnly) {
        $installedInfo = Join-Path $nativeTarget 'mod_info.json'
        if (-not (Test-Path -LiteralPath $installedInfo -PathType Leaf)) {
            throw "Preview-only updates require an already installed mod: $id. Install its complete package separately."
        }
        $installedMetadata = @(Get-Content -LiteralPath $installedInfo -Raw | ConvertFrom-Json)
        if ($installedMetadata.Count -ne 1 -or $installedMetadata[0].strName -ne $metadata[0].strName) {
            throw "Installed mod identity differs for $id."
        }
        $previewSource = Join-Path $nativeSource 'preview.png'
        $previewTarget = Join-Path $nativeTarget 'preview.png'
        if (-not (Test-Path -LiteralPath $previewSource -PathType Leaf)) { throw "Package is missing $id/preview.png. Rebuild it first." }
        if (Test-Path -LiteralPath $previewTarget -PathType Container) { throw "A directory occupies an intended file destination: $previewTarget" }
        Add-Type -AssemblyName System.Drawing
        $image = [Drawing.Bitmap]::new($previewSource)
        try {
            if ($image.Width -ne 512 -or $image.Height -ne 512 -or (Get-Item -LiteralPath $previewSource).Length -ge 1000000) {
                throw "Expected a 512px PNG preview below 1,000,000 bytes for $id."
            }
        } finally { $image.Dispose() }
        $files += [pscustomobject]@{ Source = $previewSource; Target = $previewTarget; Backup = "$id/native/preview.png"; Hash = (Get-FileHash -LiteralPath $previewSource -Algorithm SHA256).Hash }
        $plans += [pscustomobject]@{ Id = $id; Version = $installedMetadata[0].strModVersion; Index = -1; LoadOrderStatus = 'preserved (preview only)' }
        continue
    }
    $version = [version]$metadata[0].strModVersion
    if ($mod -eq 'AutoNav' -and $null -ne $minimumAutoNav -and $version -lt $minimumAutoNav) {
        throw "Shipbreaker requires Phobos Auto Nav $minimumAutoNav or later."
    }
    $dllSource = Join-Path $pluginSource "$id.dll"
    $assembly = [System.Reflection.AssemblyName]::GetAssemblyName($dllSource)
    if ($assembly.Name -ne $id -or $assembly.Version.ToString(3) -ne $version.ToString(3)) {
        throw "Plugin and native package versions differ or wrong assembly for $id. Rebuild the package first."
    }
    if ($mod -eq 'AutoNav' -and $null -ne $minimumAutoNav) {
        $installedNavDll = Join-Path $pluginTarget 'PhobosAutoNav.dll'
        if (Test-Path -LiteralPath $installedNavDll -PathType Leaf) {
            $installedNav = [Reflection.AssemblyName]::GetAssemblyName($installedNavDll)
            if ($installedNav.Version -gt $assembly.Version) {
                throw 'A newer Phobos Auto Nav is installed. Supply an equal or newer prepared dependency; Shipbreaker does not downgrade it automatically.'
            }
        }
    }
    if ($mod -eq 'Framework' -and $needsPhobosFramework -and $version -lt $minimumPhobosFramework) {
        throw "Selected equipment requires Phobos Framework $minimumPhobosFramework or later."
    }
    $needsScope = $mod -eq 'Framework' -and $version -ge [version]'0.15.0'
    foreach ($pluginFile in Get-ChildItem -LiteralPath $pluginSource -Recurse -File -Force) {
        $relativePluginFile = [IO.Path]::GetRelativePath($pluginSource, $pluginFile.FullName).Replace('\', '/')
        if ($relativePluginFile -ne "$id.dll" -and -not ($needsScope -and $relativePluginFile -eq 'Phobos.Scope.Recording.dll') -and $relativePluginFile -notmatch '^translations/[a-zA-Z]{2,8}(-[a-zA-Z0-9]{2,8})*\.json$') {
            throw "Unexpected plugin package files for $id."
        }
    }
    if ($mod -eq 'Framework') {
        if ($needsScope) {
            $scopeSource = Join-Path $pluginSource 'Phobos.Scope.Recording.dll'
            $scopeIdentity = [Reflection.AssemblyName]::GetAssemblyName($scopeSource)
            if ($scopeIdentity.Name -ne 'Phobos.Scope.Recording' -or $scopeIdentity.Version -lt [version]'0.1.1') {
                throw 'Framework requires the packaged Phobos Scope recorder 0.1.1 or later.'
            }
            $scopeTarget = Join-Path $pluginTarget 'Phobos.Scope.Recording.dll'
            $allPlugins = Join-Path $gameRoot 'BepInEx/plugins'
            if (Test-Path -LiteralPath $allPlugins) {
                $scopeCopies = @(Get-ChildItem -LiteralPath $allPlugins -Recurse -Filter 'Phobos.Scope.Recording.dll' -File)
                if (@($scopeCopies | Where-Object { $_.FullName -ne $scopeTarget }).Count -gt 0) {
                    throw 'Duplicate Phobos Scope recorder outside Framework; inspect before updating.'
                }
            }
            if (Test-Path -LiteralPath $scopeTarget) {
                $installedScope = [Reflection.AssemblyName]::GetAssemblyName($scopeTarget)
                if ($installedScope.Name -ne $scopeIdentity.Name -or $installedScope.Version -gt $scopeIdentity.Version) {
                    throw 'Installed recorder identity/version conflicts with the prepared package; automatic downgrade is refused.'
                }
            }
            if (-not (Test-Path -LiteralPath (Join-Path $nativeSource 'licenses/PhobosScope-LICENSING.md'))) {
                throw 'Framework package is missing its Phobos Scope licensing notice.'
            }
        }
        $intendedDll = Join-Path $pluginTarget 'PhobosFramework.dll'
        $plugins = Join-Path $gameRoot 'BepInEx/plugins'
        if (Test-Path -LiteralPath $plugins) {
            $copies = @(Get-ChildItem -LiteralPath $plugins -Recurse -Filter 'PhobosFramework.dll' -File)
            if (@($copies | Where-Object { $_.FullName -ne $intendedDll }).Count -gt 0) {
                throw 'Duplicate Phobos Framework provider outside its shared folder. Inspect it before updating.'
            }
        }
        if (Test-Path -LiteralPath $intendedDll -PathType Leaf) {
            $installedFramework = [Reflection.AssemblyName]::GetAssemblyName($intendedDll)
            if ($installedFramework.Name -ne $id) { throw 'Installed Phobos Framework has an unexpected assembly identity.' }
            if ($installedFramework.Version -gt $assembly.Version) {
                throw 'A newer Phobos Framework is installed. Supply an equal or newer prepared framework package; shared dependencies are not downgraded automatically.'
            }
        }
    }
    $required = switch ($mod) {
        'Agriculture' {
            'data/README.md'
            'framework/recipes.json'; 'framework/equipment-names.json'
            foreach ($image in @('Rack', 'RackNormal', 'Cooker', 'CookerNormal', 'Potato-sprout', 'Potato-young', 'Potato-mature', 'Potato-harvest', 'Potato-wilted', 'Potato-dead')) {
                "images/phobos/agriculture/$image.png"
            }
        }
        'Framework' { 'data/conditions/phobos_framework.json' }
        'WarDeclared' {
            # The shipped schematics are embedded in the plugin; the folder copies are the players' examples.
            'data/conditions/phobos_war_declared.json'; 'schematics/safe.json'; 'schematics/everything.json'; 'schematics/hull-only.json'
        }
        'Manufacturing' {
            # The 0.0.1 scaffold shipped no content; from 0.1.0 the package carries its marker, explosions, names and sprites.
            if ($version -ge [version]'0.1.0') {
                'data/conditions/phobos_manufacturing.json'; 'data/explosions/phobos_manufacturing.json'; 'framework/equipment-names.json'
                foreach ($image in @('PhobosVolatilesRefinery', 'PhobosChemicalProcessor', 'PhobosHydrogenStore', 'StockNickelIronIngot',
                    'StockCarbon', 'StockRefinerySlag', 'StockAnhydrousResidue')) {
                    "images/phobos/manufacturing/$image.png"; "images/phobos/manufacturing/${image}Normal.png"
                }
            }
            # 0.2.0 adds the Sabatier reactor and methane store; 0.3.0 the propellant manifold and line.
            if ($version -ge [version]'0.2.0') {
                foreach ($image in @('PhobosSabatierReactor', 'PhobosMethaneStore')) {
                    "images/phobos/manufacturing/$image.png"; "images/phobos/manufacturing/${image}Normal.png"
                }
            }
            if ($version -ge [version]'0.3.0') {
                foreach ($image in @('PhobosPropellantManifold', 'PropellantPipe', 'PropellantPipeSheet')) {
                    "images/phobos/manufacturing/$image.png"; "images/phobos/manufacturing/${image}Normal.png"
                }
            }
        }
        'AutoNav' {
            if ($version -ge [version]'0.2.0') { 'framework/recipes.json' }
            if ($version -ge [version]'0.7.0') { 'images/phobos/autonav/PhobosAutoNavInstruments.png' }
            'data/cooverlays/phobos_approach_assist.json'; 'data/guipropmaps/phobos_approach_assist.json'
            foreach ($image in @('Panel', 'Module', 'ModuleDmg', 'ModulePortrait', 'ModuleDmgPortrait', 'ModuleNormal')) {
                "images/phobos/autonav/PhobosAutoNav$image.png"
            }
        }
        'Shipbreaker' {
            if ($version -ge [version]'0.16.0') {
                foreach ($suffix in @('', 'Normal', 'Sheet', 'SheetNormal')) {
                    "images/phobos/shipbreaker/FurnaceCoolantPipe$suffix.png"
                }
            }
            if ($version -ge [version]'0.10.0') {
                'images/phobos/shipbreaker/PhobosIndustrialPanel.png'
                foreach ($state in @('Installed', 'InstalledDmg', 'Loose', 'LooseDmg')) {
                    foreach ($suffix in @('', 'Normal', 'Portrait')) { "images/phobos/shipbreaker/PhobosIndustrialConsole$state$suffix.png" }
                }
            }
            if ($version -ge [version]'0.8.0') {
                foreach ($suffix in @('', 'Normal', 'Portrait')) { "images/phobos/shipbreaker/PhobosScrapReclaimer$suffix.png" }
            }
            if ($version -ge [version]'0.4.0') {
                foreach ($suffix in @('', 'Normal', 'Portrait')) { "images/phobos/shipbreaker/PhobosResidueCollector$suffix.png" }
            }
            if ($version -ge [version]'0.3.0') {
                foreach ($hardware in @('PhobosHullChute', 'PhobosExteriorGrabber')) {
                    foreach ($suffix in @('', 'Normal', 'Portrait')) { "images/phobos/shipbreaker/$hardware$suffix.png" }
                }
            }
            'crafting/recipes.json'; 'data/conditions/phobos_shipbreaker.json'; 'data/condtrigs/phobos_shipbreaker.json'
            if ($version -ge [version]'0.2.0') { 'framework/recipes.json' }
            if ($version -ge [version]'0.1.4') {
                foreach ($state in @('Installed', 'InstalledDmg', 'Loose', 'LooseDmg', 'Section', 'Residue')) {
                    foreach ($suffix in @('', 'Normal', 'Portrait')) {
                        "images/phobos/shipbreaker/PhobosShipbreaker$state$suffix.png"
                    }
                }
            }
        }
    }
    foreach ($relative in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $nativeSource $relative) -PathType Leaf)) { throw "Package is incomplete: $id/$relative" }
    }
    $needsEquipmentNames = ($mod -eq 'Framework' -and $version -ge [version]'0.12.0') -or
        ($mod -eq 'Shipbreaker' -and $version -ge [version]'0.10.1') -or
        ($mod -eq 'Manufacturing' -and $version -ge [version]'0.1.0') -or
        ($mod -eq 'AutoNav' -and $version -ge [version]'0.8.1')
    if ($needsEquipmentNames -and -not (Test-Path -LiteralPath (Join-Path $nativeSource 'framework/equipment-names.json') -PathType Leaf)) {
        throw "Package is incomplete: $id/framework/equipment-names.json"
    }
    if ($mod -eq 'Shipbreaker' -and $version -ge [version]'0.2.0') {
        # Overwrite our former recipe file with an inert pack, using the normal
        # backup/receipt path. This safely retires old OCF ownership on upgrades.
        $retired = Get-Content -LiteralPath (Join-Path $nativeSource 'crafting/recipes.json') -Raw | ConvertFrom-Json
        if ($retired.schemaVersion -ne 1 -or $null -eq $retired.recipes -or @($retired.recipes).Count -ne 0 -or
            $null -eq $retired.stockAdditions -or @($retired.stockAdditions).Count -ne 0) {
            throw 'Independent Shipbreaker must retire its old Crafting Framework recipe pack.'
        }
    }
    $modFiles = @([pscustomobject]@{ Source = $dllSource; Target = (Join-Path $pluginTarget "$id.dll"); Backup = "$id/plugin/$id.dll" })
    if ($needsScope) {
        $modFiles += [pscustomobject]@{ Source = $scopeSource; Target = $scopeTarget; Backup = "$id/plugin/Phobos.Scope.Recording.dll" }
    }
    $translationSource = Join-Path (Split-Path -Parent $dllSource) 'translations'
    $needsTranslations = ($mod -in @('Agriculture', 'Manufacturing', 'WarDeclared')) -or ($mod -eq 'AutoNav' -and $version -ge [version]'0.3.0') -or
        ($mod -in @('Framework', 'Shipbreaker') -and $version -ge [version]'0.7.0')
    if ($needsTranslations -and -not (Test-Path -LiteralPath (Join-Path $translationSource 'en.json') -PathType Leaf)) {
        throw "Package is incomplete: $id/translations/en.json"
    }
    if (Test-Path -LiteralPath $translationSource) {
        foreach ($file in Get-ChildItem -LiteralPath $translationSource -Recurse -File -Force) {
            if ($file.DirectoryName -ne $translationSource -or $file.Name -notmatch '^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{2,8})*\.json$') {
                throw "Unexpected translation package file: $($file.FullName)"
            }
            $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            $modFiles += [pscustomobject]@{ Source = $file.FullName; Target = (Join-Path $pluginTarget "translations/$($file.Name)"); Backup = "$id/plugin/translations/$($file.Name)" }
        }
    }
    foreach ($file in Get-ChildItem -LiteralPath $nativeSource -Recurse -File -Force) {
        if ($file.Extension -notin @('.json', '.png', '.md')) { throw "Unexpected native package file: $($file.FullName)" }
        if ($file.Extension -eq '.json') { $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json }
        $relative = [IO.Path]::GetRelativePath($nativeSource, $file.FullName)
        $modFiles += [pscustomobject]@{ Source = $file.FullName; Target = (Join-Path $nativeTarget $relative); Backup = "$id/native/$relative" }
    }
    foreach ($file in $modFiles) {
        if (Test-Path -LiteralPath $file.Target -PathType Container) { throw "A directory occupies an intended file destination: $($file.Target)" }
        Add-Member -InputObject $file -NotePropertyName Hash -NotePropertyValue (Get-FileHash -LiteralPath $file.Source -Algorithm SHA256).Hash
    }
    $retiredCatalogue = @(Get-RetiredInstalledFiles $mod $RetiredCataloguePath)
    foreach ($folder in @($pluginTarget, $nativeTarget)) {
        if (Test-Path -LiteralPath $folder) {
            foreach ($existing in Get-ChildItem -LiteralPath $folder -Recurse -File -Force) {
                if ($existing.FullName -in $modFiles.Target) { continue }
                # A file this mod used to ship is retired only on an exact hash match; a modified copy or
                # any other extra file is still the owner's to inspect.
                $relativeExisting = [IO.Path]::GetRelativePath($folder, $existing.FullName)
                $area = if ($folder -eq $nativeTarget) { 'native' } else { 'plugin' }
                $retired = @($retiredCatalogue | Where-Object { $_.Area -eq $area -and $_.Relative -eq $relativeExisting })
                if ($retired.Count -eq 1 -and (Get-FileHash -LiteralPath $existing.FullName -Algorithm SHA256).Hash -eq $retired[0].Hash) {
                    $retiredFiles += [pscustomobject]@{ Target = $existing.FullName; Backup = "$id/retired/$area/$relativeExisting"; Hash = $retired[0].Hash }
                    continue
                }
                throw "Unmanaged installed file; inspect before updating: $($existing.FullName)"
            }
        }
    }
    $moduleIndices = @()
    for ($i = 0; $i -lt $entries.Count; $i++) {
        $parts = $entries[$i].Split('|')
        if ($parts[0] -eq 'core') { continue }
        if ((Resolve-ModEntry $parts[0] $modRoot) -eq $nativeTarget) {
            if ($parts.Count -gt 2 -or ($parts.Count -eq 2 -and $parts[1] -notin @('disabled', 'edit'))) { throw "Unrecognised $label load-order flags." }
            $moduleIndices += $i
        }
    }
    if ($moduleIndices.Count -gt 1) { throw "Duplicate $label load-order entries; resolve them before installing." }
    $loadOrderStatus = 'missing'
    if ($retainFramework -and ($moduleIndices.Count -ne 1 -or $entries[$moduleIndices[0]].EndsWith('|disabled', [StringComparison]::Ordinal))) {
        throw 'KeepInstalledFramework requires an already enabled, compatible Framework installation.'
    }
    if ($moduleIndices.Count -eq 1) {
        $index = $moduleIndices[0]
        if ($index -lt $coreIndex) { throw "$label must load after core." }
        $loadOrderStatus = 'enabled'
        if ($entries[$index].EndsWith('|disabled', [StringComparison]::Ordinal)) {
            $loadOrderStatus = 'disabled'
            $entries[$index] = $entries[$index].Split('|')[0]
            $changeOrder = $true
        }
    } else {
        $index = $entries.Count
        $entries += $id
        $changeOrder = $true
    }
    $files += $modFiles
    $plans += [pscustomobject]@{ Id = $id; Version = "$version"; Index = $index; LoadOrderStatus = $loadOrderStatus }
}
if (-not $PreviewsOnly -and 'Shipbreaker' -in $Mods -and -not $independentShipbreaker) { Assert-ShipbreakerDependencies $entries $modRoot $gameRoot $coreIndex }
$record.aLoadOrder = $entries
$changedFiles = @($files | Where-Object {
    -not (Test-Path -LiteralPath $_.Target -PathType Leaf) -or (Get-FileHash -LiteralPath $_.Target -Algorithm SHA256).Hash -ne $_.Hash
})
$description = ($plans | ForEach-Object { "$($_.Id) $($_.Version)" }) -join ', '
Write-Output "Selected: $description"
if ('Manufacturing' -in $Mods -and 'Shipbreaker' -notin $Mods -and -not $PreviewsOnly) {
    Write-Output 'Note: Manufacturing works alone; Shipbreaker 0.38.0 or newer (select or install it separately) adds the refinery''s steel charge and its S3 water silo.'
}
Write-Output "Game: $gameRoot"
Write-Output "Native mods: $modRoot"
if ($VerifyOnly) {
    if ($changedFiles.Count -gt 0 -or $retiredFiles.Count -gt 0 -or $changeOrder -or $null -ne $heldScaffold) {
        $states = ($plans | ForEach-Object { "$($_.Id) load-order status: $($_.LoadOrderStatus)" }) -join '; '
        $verifyListLimit = 20
        $names = @($changedFiles | Select-Object -First $verifyListLimit | ForEach-Object { [IO.Path]::GetRelativePath($gameRoot, $_.Target) })
        if ($changedFiles.Count -gt $verifyListLimit) { $names += "and $($changedFiles.Count - $verifyListLimit) more" }
        $listed = if ($names.Count -gt 0) { ': ' + ($names -join ', ') } else { '' }
        $retiredListed = if ($retiredFiles.Count -gt 0) { "; $($retiredFiles.Count) retired file(s) still installed: " + (($retiredFiles | ForEach-Object { [IO.Path]::GetRelativePath($gameRoot, $_.Target) }) -join ', ') } else { '' }
        throw "Installation differs: $($changedFiles.Count) missing/changed file(s)$listed$retiredListed; held scaffold still present: $($null -ne $heldScaffold); $states"
    }
    Write-Output "Verified $($files.Count) matching files and load-order configuration. In-game startup is not tested."
    return
}
if ($changedFiles.Count -eq 0 -and $retiredFiles.Count -eq 0 -and -not $changeOrder -and $null -eq $heldScaffold) {
    if (-not $NoRememberPaths -and $PSCmdlet.ShouldProcess($settingsFile, 'Remember verified installation paths')) {
        Save-InstallLocations $locations $settingsFile
    }
    Write-Output 'Already installed and verified. No game files changed.'
    return
}
if (-not $PSCmdlet.ShouldProcess($gameRoot, "Install/update $description; $($changedFiles.Count) files; retire $($retiredFiles.Count) obsolete file(s); update load order: $changeOrder; archive held scaffold: $($null -ne $heldScaffold)")) { return }
if (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue) { throw 'Ostranauts started during preflight; installation stopped.' }
if ((Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash -ne $orderHash) { throw 'Load order changed during preflight; retry.' }
# Recheck sources before touching destinations (e.g. a build in another terminal).
foreach ($file in $files) {
    if ((Get-FileHash -LiteralPath $file.Source -Algorithm SHA256).Hash -ne $file.Hash) { throw 'Prepared package changed during preflight; retry.' }
}
$backupRoot = Join-Path $repoRoot ('.local/installations/' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N'))
Assert-NoLinks $backupRoot
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
Copy-Item -LiteralPath $orderFile -Destination (Join-Path $backupRoot 'loading_order.before.json')
foreach ($file in $changedFiles) {
    $existed = Test-Path -LiteralPath $file.Target -PathType Leaf
    Add-Member -InputObject $file -NotePropertyName ExistedBefore -NotePropertyValue $existed
    if ($existed) {
        $backupFile = Join-Path $backupRoot $file.Backup
        New-Item -ItemType Directory -Path (Split-Path -Parent $backupFile) -Force | Out-Null
        Copy-Item -LiteralPath $file.Target -Destination $backupFile
    }
}
# Write the recovery manifest BEFORE copying, so even an interrupted run is inspectable.
$receipt = [ordered]@{ Mods = $plans; StartedAt = (Get-Date).ToString('o'); Status = 'Copying'; LoadOrderPath = $orderFile; ChangedFiles = $changedFiles; Files = $files; InGameVerification = 'Pending owner testing' }
$receiptFile = Join-Path $backupRoot 'receipt.json'
if ($null -ne $heldScaffold) {
    if ((Get-FileHash -LiteralPath $heldScaffold.Target -Algorithm SHA256).Hash -ne $heldScaffold.Hash) { throw 'Held scaffold changed during preflight.' }
    $heldBackup = Join-Path $backupRoot $heldScaffold.Backup
    New-Item -ItemType Directory -Path (Split-Path -Parent $heldBackup) -Force | Out-Null
    Copy-Item -LiteralPath $heldScaffold.Target -Destination $heldBackup
    if ((Get-FileHash -LiteralPath $heldBackup -Algorithm SHA256).Hash -ne $heldScaffold.Hash) { throw 'Held scaffold backup failed verification.' }
    $receipt.HeldScaffold = $heldScaffold
}
foreach ($retiredFile in $retiredFiles) {
    if ((Get-FileHash -LiteralPath $retiredFile.Target -Algorithm SHA256).Hash -ne $retiredFile.Hash) { throw "Retired file changed during preflight: $($retiredFile.Target)" }
    $retiredBackup = Join-Path $backupRoot $retiredFile.Backup
    New-Item -ItemType Directory -Path (Split-Path -Parent $retiredBackup) -Force | Out-Null
    Copy-Item -LiteralPath $retiredFile.Target -Destination $retiredBackup
    if ((Get-FileHash -LiteralPath $retiredBackup -Algorithm SHA256).Hash -ne $retiredFile.Hash) { throw "Retired file backup failed verification: $($retiredFile.Target)" }
}
if ($retiredFiles.Count -gt 0) { $receipt.RetiredFiles = $retiredFiles }
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptFile -Encoding utf8
try {
    foreach ($file in $changedFiles) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $file.Target) -Force | Out-Null
        Copy-Item -LiteralPath $file.Source -Destination $file.Target -Force
    }
    foreach ($file in $files) {
        if ((Get-FileHash -LiteralPath $file.Target -Algorithm SHA256).Hash -ne $file.Hash) { throw "Installed file mismatch: $($file.Target)" }
    }
    foreach ($retiredFile in $retiredFiles) {
        if (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue) { throw 'Ostranauts started; retired files were not removed.' }
        if ((Get-FileHash -LiteralPath $retiredFile.Target -Algorithm SHA256).Hash -ne $retiredFile.Hash) { throw "Retired file changed during copying: $($retiredFile.Target)" }
        Remove-Item -LiteralPath $retiredFile.Target
        if (Test-Path -LiteralPath $retiredFile.Target) { throw "Retired file remains in the loader directory: $($retiredFile.Target)" }
    }
    if ($null -ne $heldScaffold) {
        if (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue) { throw 'Ostranauts started; held scaffold was not removed.' }
        if ((Get-FileHash -LiteralPath $heldScaffold.Target -Algorithm SHA256).Hash -ne $heldScaffold.Hash) { throw 'Held scaffold changed during copying.' }
        Remove-Item -LiteralPath $heldScaffold.Target
        if (Test-Path -LiteralPath $heldScaffold.Target) { throw 'Held scaffold remains in the loader directory.' }
    }
    if ((Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash -ne $orderHash) { throw 'Load order changed during copying; it was not overwritten.' }
    if ($changeOrder) { ConvertTo-Json -InputObject $order -Depth 100 | Set-Content -LiteralPath $orderFile -Encoding utf8 }
    $writtenOrder = Get-Content -LiteralPath $orderFile -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
    if ((ConvertTo-Json -InputObject $writtenOrder -Depth 100 -Compress) -ne (ConvertTo-Json -InputObject $order -Depth 100 -Compress)) {
        throw 'Load-order readback does not match the intended configuration.'
    }
    $receipt.Status = 'Verified files and load order'
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptFile -Encoding utf8
} catch {
    throw "Installation incomplete. Keep the game closed and inspect backups in $backupRoot. $($_.Exception.Message)"
}
if (-not $NoRememberPaths) {
    Save-InstallLocations $locations $settingsFile
}
Write-Output "Installed and verified $description. Backups and receipt: $backupRoot"
Write-Output 'Game not launched. Saves and player settings not accessed. In-game testing remains with the owner.'
