#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)][string]$OstranautsPath,
    [Parameter(Mandatory = $true)][string]$LoadOrderPath,
    [string]$PackagePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist/PhobosApproachAssist-P0'),
    [switch]$VerifyOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $OstranautsPath).Path
$packageRoot = (Resolve-Path -LiteralPath $PackagePath).Path
$orderFile = (Resolve-Path -LiteralPath $LoadOrderPath).Path
$modRoot = Split-Path -Parent $orderFile
$pluginTarget = Join-Path $gameRoot 'BepInEx/plugins/PhobosApproachAssist'
$nativeTarget = Join-Path $modRoot 'PhobosApproachAssist'

if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx/core/BepInEx.dll') -PathType Leaf)) {
    throw 'BepInEx 5 must already be installed in OstranautsPath.'
}
if (-not $VerifyOnly -and (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue)) {
    throw 'Exit Ostranauts normally before installing. No files were changed.'
}

# Derive destinations from two explicit installation locations, never the user's saves.
$nativeSource = Join-Path $packageRoot 'Mods/PhobosApproachAssist'
$dllSource = Join-Path $packageRoot 'BepInEx/plugins/PhobosApproachAssist/PhobosApproachAssist.dll'
$metadata = @(Get-Content -LiteralPath (Join-Path $nativeSource 'mod_info.json') -Raw | ConvertFrom-Json)
if ($metadata.Count -ne 1) { throw 'Expected exactly one native mod metadata entry.' }
$version = [version]$metadata[0].strModVersion
$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllSource).Version
if ($assemblyVersion.Major -ne $version.Major -or $assemblyVersion.Minor -ne $version.Minor -or $assemblyVersion.Build -ne $version.Build) {
    throw 'Plugin and native package versions differ. Rebuild the package first.'
}
$files = @([pscustomobject]@{ Source = $dllSource; Target = (Join-Path $pluginTarget 'PhobosApproachAssist.dll'); Backup = 'plugin/PhobosApproachAssist.dll' })
foreach ($file in Get-ChildItem -LiteralPath $nativeSource -Recurse -File) {
    if ($file.Extension -ne '.json') { throw "Unexpected native package file: $($file.Name)" }
    $null = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $relative = [System.IO.Path]::GetRelativePath($nativeSource, $file.FullName)
    $files += [pscustomobject]@{ Source = $file.FullName; Target = (Join-Path $nativeTarget $relative); Backup = "native/$relative" }
}
foreach ($required in @('data/cooverlays/phobos_approach_assist.json', 'data/guipropmaps/phobos_approach_assist.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $nativeSource $required) -PathType Leaf)) { throw "Package is incomplete: $required" }
}
foreach ($file in $files) {
    if ([System.IO.Path]::GetFullPath($file.Source) -eq [System.IO.Path]::GetFullPath($file.Target)) {
        throw 'PackagePath must be separate from the installed files.'
    }
    Add-Member -InputObject $file -NotePropertyName Hash -NotePropertyValue (Get-FileHash -LiteralPath $file.Source -Algorithm SHA256).Hash
}
# Do not silently keep obsolete files or delete user additions during an update.
foreach ($folder in @($pluginTarget, $nativeTarget)) {
    if (Test-Path -LiteralPath $folder) {
        foreach ($existing in Get-ChildItem -LiteralPath $folder -Recurse -File) {
            if ($existing.FullName -notin $files.Target) { throw "Unmanaged installed file; inspect before updating: $($existing.FullName)" }
        }
    }
}

$orderHash = (Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash
$order = Get-Content -LiteralPath $orderFile -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
$records = @($order | Where-Object { $_.strName -eq 'Mod Loading Order' })
if ($records.Count -ne 1 -or -not $records[0].ContainsKey('aLoadOrder')) { throw 'Invalid loading_order.json structure.' }
$record = $records[0]
$entries = @($record.aLoadOrder)
$coreIndex = [Array]::IndexOf($entries, 'core')
if ($coreIndex -lt 0) { throw 'Load order must include enabled core.' }
$moduleIndices = @()
for ($i = 0; $i -lt $entries.Count; $i++) {
    $parts = $entries[$i].Split('|')
    if ($parts[0] -eq 'core') { continue }
    $path = if ([System.IO.Path]::IsPathRooted($parts[0])) { $parts[0] } else { Join-Path $modRoot $parts[0] }
    if ([System.IO.Path]::GetFullPath($path).TrimEnd('\', '/') -eq $nativeTarget.TrimEnd('\', '/')) {
        if ($parts.Count -gt 2 -or ($parts.Count -eq 2 -and $parts[1] -notin @('disabled', 'edit'))) { throw 'Unrecognised Approach Assist load-order flags.' }
        $moduleIndices += $i
    }
}
if ($moduleIndices.Count -gt 1) { throw 'Duplicate Approach Assist load-order entries; resolve them before installing.' }
$changeOrder = $moduleIndices.Count -eq 0
$loadOrderStatus = 'missing'
if ($moduleIndices.Count -eq 1) {
    $loadOrderStatus = 'enabled'
    $index = $moduleIndices[0]
    if ($index -lt $coreIndex) { throw 'Approach Assist must load after core.' }
    if ($entries[$index].EndsWith('|disabled', [StringComparison]::Ordinal)) {
        $loadOrderStatus = 'disabled'
        $entries[$index] = $entries[$index].Split('|')[0]
        $changeOrder = $true
    }
} else { $entries += 'PhobosApproachAssist' }
$record.aLoadOrder = $entries

$changedFiles = @($files | Where-Object {
    -not (Test-Path -LiteralPath $_.Target -PathType Leaf) -or (Get-FileHash -LiteralPath $_.Target -Algorithm SHA256).Hash -ne $_.Hash
})
if ($VerifyOnly) {
    if ($changedFiles.Count -gt 0 -or $changeOrder) { throw "Installation differs: $($changedFiles.Count) missing/changed file(s); load-order status: $loadOrderStatus" }
    Write-Output "Verified Approach Assist $version`: $($files.Count) matching files and one enabled load-order entry. In-game startup is not tested."
    return
}
if ($changedFiles.Count -eq 0 -and -not $changeOrder) {
    Write-Output "Approach Assist $version is already installed and verified. No files changed."
    return
}
if (-not $PSCmdlet.ShouldProcess("$pluginTarget and $nativeTarget", "Install Approach Assist $version; update load order: $changeOrder")) { return }
if (Get-Process -Name Ostranauts -ErrorAction SilentlyContinue) { throw 'Ostranauts started during preflight; installation stopped.' }
if ((Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash -ne $orderHash) { throw 'Load order changed during preflight; retry.' }

$backupRoot = Join-Path $repoRoot ('.local/installations/' + (Get-Date -Format 'yyyyMMdd-HHmmss-ffff') + '-approach-assist-' + $version)
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
Copy-Item -LiteralPath $orderFile -Destination (Join-Path $backupRoot 'loading_order.before.json')
foreach ($file in $changedFiles) {
    if (Test-Path -LiteralPath $file.Target -PathType Leaf) {
        $backupFile = Join-Path $backupRoot $file.Backup
        New-Item -ItemType Directory -Path (Split-Path -Parent $backupFile) -Force | Out-Null
        Copy-Item -LiteralPath $file.Target -Destination $backupFile
    }
}
try {
    foreach ($file in $changedFiles) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $file.Target) -Force | Out-Null
        Copy-Item -LiteralPath $file.Source -Destination $file.Target -Force
    }
    foreach ($file in $files) {
        if ((Get-FileHash -LiteralPath $file.Target -Algorithm SHA256).Hash -ne $file.Hash) { throw "Installed file mismatch: $($file.Target)" }
    }
    if ((Get-FileHash -LiteralPath $orderFile -Algorithm SHA256).Hash -ne $orderHash) { throw 'Load order changed during copying; it was not overwritten.' }
    if ($changeOrder) {
        ConvertTo-Json -InputObject $order -Depth 100 | Set-Content -LiteralPath $orderFile -Encoding utf8
    }
    $writtenOrder = Get-Content -LiteralPath $orderFile -Raw | ConvertFrom-Json -AsHashtable -NoEnumerate
    if ((ConvertTo-Json -InputObject $writtenOrder -Depth 100 -Compress) -ne (ConvertTo-Json -InputObject $order -Depth 100 -Compress)) {
        throw 'Load-order readback does not match the intended configuration.'
    }
    $receipt = [ordered]@{ Version = "$version"; InstalledAt = (Get-Date).ToString('o'); LoadOrderPath = $orderFile; Files = $files; InGameVerification = 'Pending owner testing' }
    $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backupRoot 'receipt.json') -Encoding utf8
} catch {
    throw "Installation incomplete. Keep the game closed and inspect backups in $backupRoot. $($_.Exception.Message)"
}
Write-Output "Installed and verified Approach Assist $version. Backups and receipt: $backupRoot"
Write-Output 'Game not launched. Saves not accessed. In-game testing remains with the owner.'
