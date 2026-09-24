#requires -Version 7.0
param([Parameter(Mandatory = $true)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& (Join-Path $PSScriptRoot 'build-framework.ps1') -OstranautsPath $gameRoot
& (Join-Path $PSScriptRoot 'export-autonav-art.ps1')
& dotnet build (Join-Path $repoRoot 'src/PhobosAutoNav/PhobosAutoNav.csproj') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Auto Nav build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosAutoNav.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Auto Nav flight and layout checks failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosAutoNav.Torch.Tests') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Auto Nav torch control checks failed.' }
$source = Join-Path $repoRoot 'mods/PhobosAutoNav'
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -Filter '*.json' -File) {
    $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
}
# Texture dimensions affect native item scale; resolve every exported art path.
$overlays = Get-Content -LiteralPath (Join-Path $source 'data/cooverlays/phobos_approach_assist.json') -Raw | ConvertFrom-Json
foreach ($overlay in $overlays) {
    foreach ($property in @('strImg', 'strImgNorm', 'strPortraitImg')) {
        $path = Join-Path $source ('images/' + $overlay.$property + '.png')
        $bitmap = [Drawing.Bitmap]::new($path)
        try {
            $expectedSize = if ($property -eq 'strPortraitImg') { 256 } else { 16 }
            if ($bitmap.Width -ne $expectedSize -or $bitmap.Height -ne $expectedSize) { throw "Wrong image size: $path" }
            if ($property -ne 'strImgNorm' -and $bitmap.GetPixel(0, 0).A -ne 0) { throw "Image margin is not transparent: $path" }
        } finally { $bitmap.Dispose() }
    }
}
$faceplate = Join-Path $source 'images/phobos/autonav/PhobosAutoNavPanel.png'
if ((Get-FileHash -LiteralPath $faceplate -Algorithm SHA256).Hash -ne 'A5D5A01FA3460A255D7E7A8830495141E8EBC02168683749D23B7E882252185E') { throw 'Approved faceplate changed.' }
$plugin = Join-Path $repoRoot 'src/PhobosAutoNav/bin/Release/netstandard2.1/PhobosAutoNav.dll'
$instruments = Join-Path $source 'images/phobos/autonav/PhobosAutoNavInstruments.png'
if ((Get-FileHash -LiteralPath $instruments -Algorithm SHA256).Hash -ne '0143EDB294795C466FC397DE981F50859CC54FF51A04E03D181B5E0AABECE674') { throw 'Instrument artwork changed; review dimensions and control registration.' }
$bitmap = [Drawing.Bitmap]::new($instruments)
try {
    if ($bitmap.Width -lt 1200 -or $bitmap.Height -lt 500) { throw 'Instrument artwork must meet 2x the 600 x 250 reference display size.' }
} finally { $bitmap.Dispose() }
# Read metadata without loading or executing the plugin.
Add-Type -Path (Join-Path $gameRoot 'BepInEx/core/Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($plugin)
try {
    if ($module.AssemblyReferences.Name -contains 'AutoNavigate') { throw 'Standalone build unexpectedly references AutoNavigate.' }
    & (Join-Path $PSScriptRoot 'assert-autonav-panel.ps1') -Module $module
} finally { $module.Dispose() }

$distRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'dist'))
$package = [IO.Path]::GetFullPath((Join-Path $distRoot 'PhobosAutoNav-P0'))
if ((Split-Path -Parent $package) -ne $distRoot -or (Split-Path -Leaf $package) -ne 'PhobosAutoNav-P0') { throw 'Unexpected output path.' }
foreach ($path in @($distRoot, $package)) {
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Build output must not be a filesystem link.'
    }
}
if (Test-Path -LiteralPath $package) {
    if (Get-ChildItem -LiteralPath $package -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw 'Output contains a filesystem link.' }
    $resolvedPackage = (Resolve-Path -LiteralPath $package).Path
    if ($resolvedPackage -ne $package) { throw 'Unexpected resolved output path.' }
    Remove-Item -LiteralPath $resolvedPackage -Recurse -Force
}
$pluginTarget = Join-Path $package 'BepInEx/plugins/PhobosAutoNav'
$nativeTarget = Join-Path $package 'Mods'
New-Item -ItemType Directory -Force -Path $pluginTarget, $nativeTarget | Out-Null
Copy-Item -LiteralPath $plugin -Destination $pluginTarget
Copy-Item -LiteralPath (Join-Path $repoRoot 'translations/PhobosAutoNav') -Destination (Join-Path $pluginTarget 'translations') -Recurse
Copy-Item -LiteralPath $source -Destination $nativeTarget -Recurse
$guide = Get-Content -LiteralPath (Join-Path $repoRoot 'docs/auto-navigate-adaptation.md') -Raw
$guide = $guide.Replace('(../THIRD_PARTY_NOTICES.md)', '(THIRD_PARTY_NOTICES.md)')
$guide = $guide.Replace('(../assets/phobos-autonav/README.md)', '(ARTWORK.md)')
Set-Content -LiteralPath (Join-Path $package 'README.md') -Value $guide -Encoding utf8
$notices = Get-Content -LiteralPath (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Raw
$notices = $notices.Replace('(docs/auto-navigate-adaptation.md)', '(README.md)')
$notices = $notices.Replace('(assets/phobos-autonav/README.md)', '(ARTWORK.md)')
Set-Content -LiteralPath (Join-Path $package 'THIRD_PARTY_NOTICES.md') -Value $notices -Encoding utf8
$artwork = Get-Content -LiteralPath (Join-Path $repoRoot 'assets/phobos-autonav/README.md') -Raw
$artwork = $artwork.Replace('(prompts.md)', '(ARTWORK-PROMPTS.md)')
$artwork = $artwork.Replace('(instruments-prompt.md)', '(INSTRUMENTS-PROMPT.md)').Replace('(../../docs/artwork-resolution-policy.md)', '(artwork-resolution-policy.md)').Replace('(../../docs/auto-nav-instruments.md)', '(auto-nav-instruments.md)')
$artwork = $artwork.Replace('(previews/instruments.html)', '(polaris-instruments-preview.html)')
Set-Content -LiteralPath (Join-Path $package 'ARTWORK.md') -Value $artwork -Encoding utf8
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-autonav/prompts.md') -Destination (Join-Path $package 'ARTWORK-PROMPTS.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/phobos-autonav/instruments-prompt.md') -Destination (Join-Path $package 'INSTRUMENTS-PROMPT.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $package
. (Join-Path $PSScriptRoot 'build-package-support.ps1')
Copy-PhobosPlayerGuides -RepoRoot $repoRoot -Package $package
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Standalone prototype: $package.zip"
Write-Output 'No game files, load order or saves were changed. No in-game tests performed.'
