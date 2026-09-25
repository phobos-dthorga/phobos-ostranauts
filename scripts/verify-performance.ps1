#requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$scope = Join-Path $root 'external/phobos-scope'
if (-not (Test-Path -LiteralPath (Join-Path $scope 'Cargo.toml'))) { throw 'Run git submodule update --init --recursive first.' }
$output = Join-Path $root ('.local/performance-checks/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
& dotnet run --project (Join-Path $root 'tests/PhobosPerformance.Tests') -c Release -- $output
if ($LASTEXITCODE -ne 0) { throw 'Performance adapter checks failed.' }
Push-Location $scope
try {
    & cargo build --workspace --release --locked
    if ($LASTEXITCODE -ne 0) { throw 'Pinned Scope analyser build failed.' }
} finally { Pop-Location }
$executable = Join-Path $scope ('target/release/phobos-scope' + $(if ($IsWindows) { '.exe' } else { '' }))
foreach ($capture in Get-ChildItem -LiteralPath $output -Filter '*.json' -File) {
    & $executable analyse $capture.FullName (Join-Path $output $capture.BaseName)
    if ($LASTEXITCODE -ne 0) { throw "Adapter capture is incompatible with Scope: $($capture.Name)" }
}
$detailed = Get-Content -LiteralPath (Join-Path $output 'adapter-detailed-world-change/report.json') -Raw | ConvertFrom-Json
if ($detailed.stop_reason -ne 'world_change' -or $detailed.statistics[1].calls -ne 1 -or $detailed.statistics[1].incomplete -ne 1) {
    throw 'Rust reports do not preserve adapter lifecycle evidence.'
}
if (Test-Path -LiteralPath (Join-Path $output 'adapter-summary-world-change/trace.json')) { throw 'Summary capture fabricated a timeline.' }
Write-Output "Adapter captures accepted by pinned Rust analyser: $output"
Write-Output 'Synthetic checks only; no game launched or save accessed.'
