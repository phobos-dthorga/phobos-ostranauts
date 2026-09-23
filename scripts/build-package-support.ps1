# Shared packaging for our own assemblies and native mod folders only.
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
    Copy-Item -LiteralPath $source -Destination $nativeTarget -Recurse
    Copy-Item -LiteralPath (Join-Path $RepoRoot $Readme) -Destination (Join-Path $package 'README.md')
    foreach ($document in $ExtraDocs) { Copy-Item -LiteralPath (Join-Path $RepoRoot $document) -Destination $package }
    Copy-Item -LiteralPath (Join-Path $source 'THIRD-PARTY.md') -Destination $package
    if (Test-Path -LiteralPath (Join-Path $source 'licenses') -PathType Container) {
        Copy-Item -LiteralPath (Join-Path $source 'licenses') -Destination $package -Recurse
    }
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'LICENSE') -Destination $package
    return $package
}
