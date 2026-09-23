# Helpers shared by the command-line installer and its legacy entry point.
function Test-Within([string]$Path, [string]$Root) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $parent = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    return $full.Equals($parent, [StringComparison]::OrdinalIgnoreCase) -or
        $full.StartsWith($parent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoLinks([string]$Path, [switch]$Tree) {
    $cursor = [IO.Path]::GetFullPath($Path)
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Filesystem link is not supported; use a direct installation path: $cursor"
            }
        }
        $cursor = Split-Path -Parent $cursor
    }
    if ($Tree -and (Test-Path -LiteralPath $Path -PathType Container)) {
        if (Get-ChildItem -LiteralPath $Path -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
            throw "Filesystem link inside mod directory: $Path"
        }
    }
}

function Resolve-ModEntry([string]$Entry, [string]$ModRoot) {
    $path = if ([IO.Path]::IsPathRooted($Entry)) { $Entry } else { Join-Path $ModRoot $Entry }
    return [IO.Path]::GetFullPath($path).TrimEnd('\', '/')
}

function Save-InstallLocations($Locations, [string]$SettingsFile) {
    try {
        Assert-NoLinks $SettingsFile
        $json = $Locations | ConvertTo-Json
        if ((Test-Path -LiteralPath $SettingsFile) -and (Get-Content -LiteralPath $SettingsFile -Raw).Trim() -eq $json.Trim()) { return }
        New-Item -ItemType Directory -Path (Split-Path -Parent $SettingsFile) -Force | Out-Null
        $json | Set-Content -LiteralPath $SettingsFile -Encoding utf8
    } catch { Write-Warning "Installation verified, but paths could not be remembered: $($_.Exception.Message)" }
}

function Resolve-InstallLocations([string]$Game, [string]$Order, [string]$SettingsFile) {
    # Explicit game paths never inherit a load-order path saved for a different game.
    $saved = if ((-not $Game -or -not $Order) -and (Test-Path -LiteralPath $SettingsFile)) {
        Get-Content -LiteralPath $SettingsFile -Raw | ConvertFrom-Json -AsHashtable
    } else { @{} }
    if (-not $Game -and $saved['OstranautsPath']) { $Game = $saved['OstranautsPath'] }
    if (-not $Game) {
        $steamRoots = @()
        if ($IsWindows) {
            $steam = Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam' -ErrorAction SilentlyContinue
            if ($steam -and $steam.PSObject.Properties['SteamPath']) { $steamRoots += $steam.SteamPath }
        }
        if (${env:ProgramFiles(x86)}) { $steamRoots += Join-Path ${env:ProgramFiles(x86)} 'Steam' }
        $libraries = @($steamRoots)
        foreach ($root in ($steamRoots | Select-Object -Unique)) {
            $vdf = Join-Path $root 'steamapps/libraryfolders.vdf'
            if (Test-Path -LiteralPath $vdf) {
                foreach ($match in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s*"([^"]+)"')) {
                    $libraries += $match.Groups[1].Value.Replace('\\', '\')
                }
            }
        }
        $candidates = @(foreach ($root in ($libraries | Select-Object -Unique)) {
            $manifest = Join-Path $root 'steamapps/appmanifest_1022980.acf'
            if (-not (Test-Path -LiteralPath $manifest)) { continue }
            $match = [regex]::Match((Get-Content -LiteralPath $manifest -Raw), '"installdir"\s*"([^"]+)"')
            if ($match.Success) {
                $candidate = Join-Path $root ('steamapps/common/' + $match.Groups[1].Value)
                if (Test-Path -LiteralPath (Join-Path $candidate 'Ostranauts.exe')) { [IO.Path]::GetFullPath($candidate) }
            }
        })
        $candidates = @($candidates | Sort-Object -Unique)
        if ($candidates.Count -ne 1) { throw 'Could not identify one Steam installation. Supply -OstranautsPath and -LoadOrderPath explicitly.' }
        $Game = $candidates[0]
    }
    $Game = (Resolve-Path -LiteralPath $Game).Path
    if (-not (Test-Path -LiteralPath (Join-Path $Game 'Ostranauts.exe') -PathType Leaf)) { throw 'OstranautsPath must contain Ostranauts.exe.' }
    if (-not $Order -and $saved['OstranautsPath'] -and $saved['LoadOrderPath'] -and
        [IO.Path]::GetFullPath($saved['OstranautsPath']).TrimEnd('\', '/') -eq $Game.TrimEnd('\', '/')) { $Order = $saved['LoadOrderPath'] }
    if (-not $Order) { $Order = Join-Path $Game 'Ostranauts_Data/Mods/loading_order.json' }
    if (-not (Test-Path -LiteralPath $Order -PathType Leaf)) { throw 'Load order not found. Supply -LoadOrderPath for your configured Mods folder.' }
    $Order = (Resolve-Path -LiteralPath $Order).Path
    if ((Split-Path -Leaf $Order) -ne 'loading_order.json') { throw 'LoadOrderPath must name loading_order.json.' }
    return [ordered]@{ OstranautsPath = $Game; LoadOrderPath = $Order }
}

function Assert-ShipbreakerDependencies([string[]]$Entries, [string]$ModRoot, [string]$GameRoot, [int]$CoreIndex) {
    $providers = @{}
    for ($i = 0; $i -lt $Entries.Count; $i++) {
        $parts = $Entries[$i].Split('|')
        if ($parts[0] -eq 'core' -or 'disabled' -in $parts) { continue }
        $folder = Resolve-ModEntry $parts[0] $ModRoot
        $infoFile = Join-Path $folder 'mod_info.json'
        if (-not (Test-Path -LiteralPath $infoFile -PathType Leaf)) { continue }
        try { $metadata = @(Get-Content -LiteralPath $infoFile -Raw | ConvertFrom-Json -AsHashtable) } catch { continue }
        foreach ($info in $metadata) {
            $key = switch ($info['strWorkshopID']) { '3798573443' { 'Framework' } '3798573453' { 'Workshop' } }
            if (-not $key) {
                $key = switch ($info['strName']) { 'Ostranauts Crafting Framework' { 'Framework' } 'Salvage Workshop' { 'Workshop' } }
            }
            if ($key) {
                if ($providers.ContainsKey($key)) { throw "Duplicate enabled Shipbreaker dependency: $key" }
                $providers[$key] = @{ Index = $i; Folder = $folder; Version = $info['strModVersion'] }
            }
        }
    }
    if (-not $providers.ContainsKey('Framework') -or -not $providers.ContainsKey('Workshop')) {
        throw 'Shipbreaker needs enabled Crafting Framework and Salvage Workshop. Install/enable them through Steam and the game first; this installer leaves other mods unchanged.'
    }
    $shipIndex = -1
    for ($i = 0; $i -lt $Entries.Count; $i++) {
        if ((Resolve-ModEntry ($Entries[$i].Split('|')[0]) $ModRoot) -eq (Join-Path $ModRoot 'PhobosShipbreaker')) { $shipIndex = $i }
    }
    if ($providers.Framework.Index -le $CoreIndex -or $providers.Workshop.Index -le $providers.Framework.Index -or $shipIndex -le $providers.Workshop.Index) {
        throw 'Shipbreaker load order must be core, Crafting Framework, Salvage Workshop, then PhobosShipbreaker. Correct it in the game before retrying.'
    }
    if ([version]$providers.Framework.Version -lt [version]'0.8.71') { throw 'Shipbreaker requires Crafting Framework 0.8.71 or later.' }
    # Check the installed plugin or a Workshop source with its bridge. Presence is
    # not a runtime loading/compatibility guarantee; the mod reports that in-game.
    $pluginRoot = Join-Path $GameRoot 'BepInEx/plugins'
    $dlls = @(if (Test-Path -LiteralPath $pluginRoot) { Get-ChildItem -LiteralPath $pluginRoot -Recurse -File -Filter 'CraftingFramework.dll' })
    if ($dlls.Count -eq 0 -and (Test-Path -LiteralPath (Join-Path $GameRoot 'BepInEx/patchers/OstranautsWorkshopBepInExBridge.Preloader.dll'))) {
        $dlls = @(Get-ChildItem -LiteralPath $providers.Framework.Folder -Recurse -File -Filter 'CraftingFramework.dll')
    }
    if ($dlls.Count -ne 1) { throw 'Shipbreaker needs one CraftingFramework.dll installed (or available to the Workshop BepInEx Bridge). Check the framework installation.' }
    $assembly = [Reflection.AssemblyName]::GetAssemblyName($dlls[0].FullName)
    if ($assembly.Name -ne 'CraftingFramework') { throw 'CraftingFramework.dll contains an unexpected assembly.' }
    # Upstream 0.8.71 uses assembly version 0.0.0.0. BepInEx's plugin version
    # comes from its attribute, not AssemblyName. Leave that check to the loader.
    Write-Output "Dependency preflight: Crafting Framework data $($providers.Framework.Version), plugin present; Salvage Workshop $($providers.Workshop.Version). Runtime plugin version/compatibility checks remain in-game."
}
