param(
    [Parameter(Mandatory = $true)][string]$OstranautsPath,
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
$project = Join-Path $repoRoot 'src/PhobosApproachAssist/PhobosApproachAssist.csproj'
& dotnet build $project -c Release "-p:OstranautsPath=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
if (-not $SkipTests) {
    & dotnet run --project (Join-Path $repoRoot 'tests/PhobosApproachAssist.Tests') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Controller tests failed.' }
}
$package = Join-Path $repoRoot 'dist/PhobosApproachAssist-P0'
$pluginTarget = Join-Path $package 'BepInEx/plugins/PhobosApproachAssist'
$nativeTarget = Join-Path $package 'Mods'
New-Item -ItemType Directory -Force -Path $pluginTarget, $nativeTarget | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'src/PhobosApproachAssist/bin/Release/netstandard2.1/PhobosApproachAssist.dll') -Destination $pluginTarget
Copy-Item -LiteralPath (Join-Path $repoRoot 'mods/PhobosApproachAssist') -Destination $nativeTarget -Recurse -Force
$guide = Get-Content -LiteralPath (Join-Path $repoRoot 'docs/approach-assist-prototype.md') -Raw
$guide = $guide.Replace('(limited-autopilot.md)', '(https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/limited-autopilot.md)')
Set-Content -LiteralPath (Join-Path $package 'README.md') -Value $guide -Encoding utf8
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $package 'LICENSE')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip" -Force
Write-Output "Prototype package: $package"
Write-Output 'No game files, load order or saves were changed.'
