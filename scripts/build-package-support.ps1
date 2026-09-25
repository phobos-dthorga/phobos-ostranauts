# Shared packaging for our own assemblies and native mod folders only.
function Copy-PhobosPlayerGuides {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Package
    )
    # Every suite package has the same entry point and its directly linked guides.
    # Keep their filenames as well as the mod-specific README so links remain usable.
    foreach ($name in @(
        'agriculture-player-guide', 'agriculture-implementation', 'agriculture-research', 'agriculture-first-slice', 'agriculture-roadmap', 'agriculture-living-visuals', 'asset-generation-policy',
        'performance-captures', 'furnace-player-guide', 'furnace-connections-and-instruments', 'furnace-first-cycle', 'furnace-repair-castings', 'furnace-material-routing', 'manufacturing-handover',
        'player-guide', 'equipment-branding', 'installing-mods', 'equipment-economy', 'equipment-value-audit', 'auto-nav-instruments', 'auto-nav-docking', 'auto-nav-sensors', 'auto-nav-flight-profiles', 'artwork-resolution-policy',
        'vanilla-economy-audit', 'shipbreaker-first-build', 'shipbreaker-hull-intake',
        'residue-collector', 'auto-navigate-adaptation', 'auto-nav-economy', 'auto-nav-panel-layout-audit', 'auto-nav-persistence', 'auto-nav-torch', 'residue-material-contract',
        'shipbreaking-material-processing-research', 'material-disposal-port-research',
        'processing-job-compatibility', 'localization', 'scrap-reclaimer', 'automatic-material-routing', 'material-port-pairing',
        'industrial-console-player-guide', 'industrial-control-console', 'industrial-control-mockups', 'shared-console-observations', 'sensor-integration-research', 'fusion-smelter-research', 'framework-author-guide'
    )) {
        Copy-Item -LiteralPath (Join-Path $RepoRoot "docs/$name.md") -Destination $Package
    }
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'assets/phobos-furnace/coupling-provenance.json') -Destination $Package
    $furnaceGuidePath = Join-Path $Package 'furnace-connections-and-instruments.md'
    $furnaceGuide = Get-Content -LiteralPath $furnaceGuidePath -Raw
    $furnaceGuide = $furnaceGuide.Replace('../assets/phobos-furnace/coupling-provenance.json', 'coupling-provenance.json')
    Set-Content -LiteralPath $furnaceGuidePath -Value $furnaceGuide -Encoding utf8
    # The shared instrument guide links a preview; keep it usable offline in every suite package.
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'assets/phobos-autonav/instruments-prompt.md') -Destination (Join-Path $Package 'INSTRUMENTS-PROMPT.md')
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'mods/PhobosAutoNav/images/phobos/autonav/PhobosAutoNavInstruments.png') -Destination $Package
    $preview = Get-Content -LiteralPath (Join-Path $RepoRoot 'assets/phobos-autonav/previews/instruments.html') -Raw
    $preview = $preview.Replace('../../../mods/PhobosAutoNav/images/phobos/autonav/PhobosAutoNavInstruments.png', 'PhobosAutoNavInstruments.png')
    Set-Content -LiteralPath (Join-Path $Package 'polaris-instruments-preview.html') -Value $preview -Encoding utf8
    $instrumentGuidePath = Join-Path $Package 'auto-nav-instruments.md'
    $instrumentGuide = Get-Content -LiteralPath $instrumentGuidePath -Raw
    $instrumentGuide = $instrumentGuide.Replace('../assets/phobos-autonav/instruments-prompt.md', 'INSTRUMENTS-PROMPT.md').Replace('../assets/phobos-autonav/previews/instruments.html', 'polaris-instruments-preview.html')
    Set-Content -LiteralPath $instrumentGuidePath -Value $instrumentGuide -Encoding utf8
    # Covers are native preview.png files, copied with the native mod directory.
    # Validate against the committed derivative; ordinary builds need no art branch.
    $covers = Get-Content -LiteralPath (Join-Path $RepoRoot 'assets/workshop/exports.json') -Raw | ConvertFrom-Json
    foreach ($cover in $covers.assets) {
        $native = Join-Path $Package "Mods/$($cover.id)"
        if (-not (Test-Path -LiteralPath $native -PathType Container)) { continue }
        $expected = Join-Path $RepoRoot "assets/workshop/previews/$($cover.id)-512.png"
        $actual = Join-Path $native 'preview.png'
        if (-not (Test-Path -LiteralPath $actual -PathType Leaf) -or (Get-FileHash -LiteralPath $actual).Hash -ne (Get-FileHash -LiteralPath $expected).Hash) {
            throw "Missing or stale mod-menu preview for $($cover.id). Run export-workshop-art.ps1."
        }
        Copy-Item -LiteralPath (Join-Path $RepoRoot 'assets/workshop/PACKAGE-ARTWORK.md') -Destination (Join-Path $Package 'WORKSHOP-ARTWORK.md')
        Copy-Item -LiteralPath (Join-Path $RepoRoot 'assets/workshop/prompts.json') -Destination (Join-Path $Package 'WORKSHOP-ARTWORK-PROMPTS.json')
    }
}

function New-PhobosPackage {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][ValidatePattern('^Phobos[A-Za-z]+$')][string]$Id,
        [Parameter(Mandatory)][string]$Readme,
        [string[]]$ExtraDocs = @()
    )
    $source = Join-Path $RepoRoot "mods/$Id"
    foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -Filter '*.json' -File) {
        $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    }
    $distRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot 'dist'))
    $package = [IO.Path]::GetFullPath((Join-Path $distRoot "$Id-P0"))
    if ((Split-Path -Parent $package) -ne $distRoot -or (Split-Path -Leaf $package) -ne "$Id-P0") {
        throw 'Unexpected package output path.'
    }
    foreach ($path in @($distRoot, $package, "$package.zip")) {
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Build output must not be a filesystem link: $path"
        }
    }
    if (Test-Path -LiteralPath $package) {
        if (Get-ChildItem -LiteralPath $package -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
            throw 'Build output contains a filesystem link; inspect it before rebuilding.'
        }
        $resolvedPackage = (Resolve-Path -LiteralPath $package).Path
        if ($resolvedPackage -ne $package) { throw 'Build output resolves outside its expected location.' }
        Remove-Item -LiteralPath $resolvedPackage -Recurse -Force
    }
    $pluginTarget = Join-Path $package "BepInEx/plugins/$Id"
    $nativeTarget = Join-Path $package 'Mods'
    New-Item -ItemType Directory -Force -Path $pluginTarget, $nativeTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $RepoRoot "src/$Id/bin/Release/netstandard2.1/$Id.dll") -Destination $pluginTarget
    Copy-Item -LiteralPath (Join-Path $RepoRoot "translations/$Id") -Destination (Join-Path $pluginTarget 'translations') -Recurse
    Copy-Item -LiteralPath $source -Destination $nativeTarget -Recurse
    if ($Id -eq 'PhobosFramework') {
        $recorder = Join-Path $RepoRoot 'src/PhobosFramework/bin/Release/netstandard2.1/Phobos.Scope.Recording.dll'
        $identity = [Reflection.AssemblyName]::GetAssemblyName($recorder)
        if ($identity.Name -ne 'Phobos.Scope.Recording' -or $identity.Version -lt [version]'0.1.1') {
            throw 'Build Framework with its pinned Phobos Scope recorder dependency first.'
        }
        Copy-Item -LiteralPath $recorder -Destination $pluginTarget
        Copy-Item -LiteralPath (Join-Path $RepoRoot 'external/phobos-scope/docs/licensing.md') -Destination (Join-Path $nativeTarget 'PhobosFramework/licenses/PhobosScope-LICENSING.md')
    }
    Copy-Item -LiteralPath (Join-Path $RepoRoot $Readme) -Destination (Join-Path $package 'README.md')
    foreach ($document in $ExtraDocs) { Copy-Item -LiteralPath (Join-Path $RepoRoot $document) -Destination $package }
    Copy-PhobosPlayerGuides -RepoRoot $RepoRoot -Package $package
    Copy-Item -LiteralPath (Join-Path $source 'THIRD-PARTY.md') -Destination $package
    if (Test-Path -LiteralPath (Join-Path $source 'licenses') -PathType Container) {
        Copy-Item -LiteralPath (Join-Path $source 'licenses') -Destination $package -Recurse
    }
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'LICENSE') -Destination $package
    return $package
}
