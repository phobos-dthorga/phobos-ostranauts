#requires -Version 7.0
param([Parameter(Mandatory = $true)][string]$OstranautsPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
& dotnet build (Join-Path $repoRoot 'src/PhobosShipbreaker/PhobosShipbreaker.csproj') -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shipbreaker build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/PhobosShipbreaker.Tests') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Shipbreaker processing checks failed.' }

$source = Join-Path $repoRoot 'mods/PhobosShipbreaker'
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -Filter '*.json' -File) {
    $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
}
$distRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'dist'))
$package = [IO.Path]::GetFullPath((Join-Path $distRoot 'PhobosShipbreaker-P0'))
# Only this named build output may be replaced, and never through a filesystem link.
if ((Split-Path -Parent $package) -ne $distRoot -or (Split-Path -Leaf $package) -ne 'PhobosShipbreaker-P0') {
    throw 'Unexpected package output path.'
}
foreach ($path in @($distRoot, $package)) {
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
$pluginTarget = Join-Path $package 'BepInEx/plugins/PhobosShipbreaker'
$nativeTarget = Join-Path $package 'Mods'
New-Item -ItemType Directory -Force -Path $pluginTarget, $nativeTarget | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'src/PhobosShipbreaker/bin/Release/netstandard2.1/PhobosShipbreaker.dll') -Destination $pluginTarget
Copy-Item -LiteralPath $source -Destination $nativeTarget -Recurse
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/shipbreaker-first-build.md') -Destination (Join-Path $package 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/dependency-contingencies.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/shipbreaker-material-uses.md') -Destination $package
$materialGuide = Get-Content -LiteralPath (Join-Path $package 'shipbreaker-material-uses.md') -Raw
$materialGuide = $materialGuide.Replace('(shipbreaker-first-build.md)', '(README.md)')
Set-Content -LiteralPath (Join-Path $package 'shipbreaker-material-uses.md') -Value $materialGuide -Encoding utf8
Copy-Item -LiteralPath (Join-Path $source 'THIRD-PARTY.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $package
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Package: $package.zip"
Write-Output 'No game files, load order or saves were changed.'
