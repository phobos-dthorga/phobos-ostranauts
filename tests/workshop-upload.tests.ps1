#requires -Version 7.0
# Synthetic tests for the owner-run Workshop uploader and local-copy removal.
# A fake SteamCMD stands in for Valve's tool: no network, Steam login or game files.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$uploader = Join-Path $repoRoot 'scripts/upload-workshop.ps1'
$remover = Join-Path $repoRoot 'scripts/remove-local-mods.ps1'
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
function Put([string]$Root, [string]$Path, [string]$Text) {
    $target = Join-Path $Root $Path
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    [IO.File]::WriteAllText($target, $Text, [Text.UTF8Encoding]::new($false))
}
function Catalogue([string]$Root) { Get-Content -LiteralPath (Join-Path $Root 'config/workshop-publishing.json') -Raw | ConvertFrom-Json -AsHashtable }

# --- A minimal repository with one prepared Framework candidate --------------------
$repo = Join-Path $fixtures 'repo'
Put $repo 'config/workshop-publishing.json' (@{ appId = '1022980'; mods = @{ PhobosFramework = @{ itemId = $null; requires = @(); hold = $null } }
    externalRequiredItems = @('3741030124'); externalRequiredItemNames = @{ '3741030124' = 'BepInEx Mod Loader' } } | ConvertTo-Json -Depth 5)
Put $repo 'mods/PhobosFramework/mod_info.json' '[{"strName":"Phobos Framework","strModVersion":"1.0.0"}]'
Put $repo 'mods/PhobosFramework/data/README.md' 'Keep data'
Put $repo 'mods/PhobosFramework/preview.png' 'fixture cover'
Put $repo 'mods/PhobosFramework/CHANGELOG.md' "# Changelog`n`n## [Unreleased]`n`nNone.`n`n## [1.0.0] - 2026-09-29 - Draft`n`n### Added`n`n- Example.`n"
Put $repo 'workshop/PhobosFramework/page.bbcode' "[h1]Phobos Framework[/h1]`n[b]Version:[/b] 1.0.0`n[b]Publication status:[/b] Draft`n"
foreach ($script in 'prepare-workshop.py', 'workshop-release-notes.py', 'read-only-data-headers.py') {
    New-Item -ItemType Directory -Force -Path (Join-Path $repo 'scripts') | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot "scripts/$script") -Destination (Join-Path $repo "scripts/$script")
}
Copy-Item -LiteralPath $uploader -Destination (Join-Path $repo 'scripts/upload-workshop.ps1')
& python (Join-Path $repo 'scripts/workshop-release-notes.py') --write | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'mods/PhobosFramework') -Destination (Join-Path $repo 'dist/PhobosFramework-P0/Mods/PhobosFramework') -Recurse
foreach ($dll in 'PhobosFramework.dll', 'Phobos.Scope.Recording.dll') {
    Put $repo "dist/PhobosFramework-P0/BepInEx/plugins/PhobosFramework/$dll" "fixture $dll"
    Put $repo "src/PhobosFramework/bin/Release/netstandard2.1/$dll" "fixture $dll"
}
Put $repo '.gitignore' ".local/`n"
git -C $repo init -q
git -C $repo -c user.name=Test -c user.email=test@example.invalid add -A
git -C $repo -c user.name=Test -c user.email=test@example.invalid commit -q -m fixture
$prepared = (& python (Join-Path $repo 'scripts/prepare-workshop.py') --mod Framework --prepare) -join "`n" | ConvertFrom-Json
Check ($prepared.status -eq 'prepared-offline' -and $prepared.operation -eq 'create') 'Fixture candidate was not prepared'

# The fake SteamCMD records its arguments and, like Valve's tool, writes a new ID into the VDF.
$steam = Join-Path $fixtures 'steamcmd'
Put $fixtures 'steamcmd/steamcmd.ps1' @'
param([Parameter(ValueFromRemainingArguments)] $Rest)
$vdf = $Rest[[array]::IndexOf($Rest, '+workshop_build_item') + 1]
New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot 'logs') | Out-Null
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'logs/workshop_log.txt') -Value ($Rest -join ' ')
if ($env:PHOBOS_FAKE_STEAM_ID) {
    $text = (Get-Content -LiteralPath $vdf -Raw) -replace '"publishedfileid" "0"', ('"publishedfileid" "' + $env:PHOBOS_FAKE_STEAM_ID + '"')
    [IO.File]::WriteAllText($vdf, $text)
}
exit 0
'@
$fake = Join-Path $steam 'steamcmd.ps1'
$common = @{ RepositoryRoot = $repo; SteamCmdPath = $fake; SteamUser = 'fixture-account' }

Fails { & $uploader @common -Mod Framework -Operation Update | Out-Null } 'prepared for create'
Fails { & $uploader @common -Mod Framework -Operation Create -Visibility Public -ConfirmPublic | Out-Null } 'New items are created Private'
$plan = & $uploader @common -Mod Framework -Operation Create -WhatIf
Check (($plan -join "`n") -match 'BepInEx Mod Loader \(3741030124\)') 'Plan must list required items'
Check (-not (Test-Path -LiteralPath (Join-Path $repo '.local/workshop-receipts'))) 'WhatIf must not write a receipt'

# An uncertain create (no ID back) blocks a second create until reconciled.
$env:PHOBOS_FAKE_STEAM_ID = ''
& $uploader @common -Mod Framework -Operation Create | Out-Null
$receiptDir = @(Get-ChildItem -LiteralPath (Join-Path $repo '.local/workshop-receipts/PhobosFramework') -Directory)[0].FullName
$receipt = Get-Content -LiteralPath (Join-Path $receiptDir 'receipt.json') -Raw | ConvertFrom-Json
Check ($receipt.status -eq 'submitted-unverified' -and $null -eq (Catalogue $repo).mods.PhobosFramework.itemId) 'Missing ID must stay unresolved'
Check ((Get-Content -LiteralPath (Join-Path $receiptDir 'upload.vdf') -Raw) -match '"visibility" "2"') 'Upload VDF must be private'
Check (Test-Path -LiteralPath (Join-Path $receiptDir 'steamcmd-logs/workshop_log.txt')) 'SteamCMD logs must be kept with the receipt'
Check ((Get-Content -LiteralPath (Join-Path $receiptDir 'steamcmd-logs/workshop_log.txt') -Raw) -notmatch 'password') 'No password is passed to SteamCMD'
Fails { & $uploader @common -Mod Framework -Operation Create | Out-Null } 'unresolved'
& $uploader -RepositoryRoot $repo -Reconcile $receiptDir -NotCreated | Out-Null
Check ((Get-Content -LiteralPath (Join-Path $receiptDir 'receipt.json') -Raw | ConvertFrom-Json).status -eq 'reconciled-not-created') 'Reconcile must close the receipt'

# A successful create records the returned ID; the old candidate can no longer create.
$env:PHOBOS_FAKE_STEAM_ID = '4242'
Start-Sleep -Seconds 1
& $uploader @common -Mod Framework -Operation Create | Out-Null
Check ((Catalogue $repo).mods.PhobosFramework.itemId -eq '4242') 'Returned item ID must be recorded'
Fails { & $uploader @common -Mod Framework -Operation Create | Out-Null } 'already has item 4242'
Check (& python (Join-Path $repo 'scripts/prepare-workshop.py') --verify $prepared.directory | ConvertFrom-Json).status -eq 'verified-offline' 'Candidate must stay unchanged'
Check ((Catalogue $repo).mods.PhobosFramework.uploadedVersion -eq '1.0.0') 'A create must record the uploaded version'
Remove-Item Env:PHOBOS_FAKE_STEAM_ID

# Two releases later, the update's change note covers both, newest first, and records the new version.
Put $repo 'mods/PhobosFramework/mod_info.json' '[{"strName":"Phobos Framework","strModVersion":"1.0.2"}]'
Put $repo 'mods/PhobosFramework/CHANGELOG.md' "# Changelog`n`n## [Unreleased]`n`nNone.`n`n## [1.0.2] - 2026-09-30 - Draft`n`n### Fixed`n`n- Second fix.`n`n## [1.0.1] - 2026-09-30 - Draft`n`n### Fixed`n`n- First fix.`n`n## [1.0.0] - 2026-09-29 - Draft`n`n### Added`n`n- Example.`n"
Put $repo 'workshop/PhobosFramework/page.bbcode' "[h1]Phobos Framework[/h1]`n[b]Version:[/b] 1.0.2`n[b]Publication status:[/b] Draft`n"
& python (Join-Path $repo 'scripts/workshop-release-notes.py') --write | Out-Null
Remove-Item -LiteralPath (Join-Path $repo 'dist/PhobosFramework-P0/Mods/PhobosFramework') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repo 'mods/PhobosFramework') -Destination (Join-Path $repo 'dist/PhobosFramework-P0/Mods/PhobosFramework') -Recurse
$update = (& python (Join-Path $repo 'scripts/prepare-workshop.py') --mod Framework --prepare) -join "`n" | ConvertFrom-Json
Check ($update.operation -eq 'update' -and ($update.changeNoteVersions -join ',') -eq '1.0.2,1.0.1') "Change note must cover 1.0.2 and 1.0.1: $($update.changeNoteVersions -join ',')"
$note = Get-Content -LiteralPath (Join-Path $update.directory 'content/documentation/Release-notes.bbcode') -Raw
Check ($note.IndexOf('Framework 1.0.2') -lt $note.IndexOf('Framework 1.0.1') -and $note -notmatch 'Framework 1\.0\.0') 'Change note must run newest first from after the last upload'
Check ((& $uploader @common -Mod Framework -Operation Update -WhatIf) -contains 'Change note covers: 1.0.2, 1.0.1') 'Plan must name the versions in the change note'
# A newer upload recorded after preparation makes the candidate's change note wrong.
& python (Join-Path $repo 'scripts/prepare-workshop.py') --mod Framework --record-uploaded-version 1.0.1 | Out-Null
Fails { & $uploader @common -Mod Framework -Operation Update | Out-Null } 'change note is out of date'
Start-Sleep -Seconds 1
$fresh = (& python (Join-Path $repo 'scripts/prepare-workshop.py') --mod Framework --prepare) -join "`n" | ConvertFrom-Json
Check (($fresh.changeNoteVersions -join ',') -eq '1.0.2') 'A fresh candidate starts after the newly recorded version'
& $uploader @common -Mod Framework -Operation Update | Out-Null
Check ((Catalogue $repo).mods.PhobosFramework.uploadedVersion -eq '1.0.2') 'An update must record the uploaded version'

# --- Local-copy removal ----------------------------------------------------------------
$game = Join-Path $fixtures 'game'
Put $game 'Ostranauts.exe' 'fixture'
Put $game 'Ostranauts_Data/Mods/PhobosFramework/mod_info.json' '[{"strName":"Phobos Framework"}]'
Put $game 'Ostranauts_Data/Mods/PhobosFramework/data/README.md' 'data'
Put $game 'Ostranauts_Data/Mods/OtherMod/mod_info.json' '[{"strName":"Other"}]'
Put $game 'BepInEx/plugins/PhobosFramework/PhobosFramework.dll' 'plugin'
Put $game 'BepInEx/plugins/PhobosFramework/translations/en.json' '{}'
$order = Join-Path $game 'Ostranauts_Data/Mods/loading_order.json'
ConvertTo-Json -InputObject @(@{ strName = 'Mod Loading Order'; aLoadOrder = @('core', 'C:/Workshop/4242', 'PhobosFramework', 'OtherMod|disabled') }) -Depth 5 | Set-Content -LiteralPath $order
$paths = @{ OstranautsPath = $game; LoadOrderPath = $order; RepositoryRoot = $repo }
& $remover @paths -Mods Framework -WhatIf | Out-Null
Check (Test-Path -LiteralPath (Join-Path $game 'BepInEx/plugins/PhobosFramework/PhobosFramework.dll')) 'WhatIf must not remove files'
$global:PhobosInstallerTestGameRunning = $true
Fails { & $remover @paths -Mods Framework | Out-Null } 'Exit Ostranauts'
$global:PhobosInstallerTestGameRunning = $false
& $remover @paths -Mods Framework, Agriculture | Out-Null
$after = (Get-Content -LiteralPath $order -Raw | ConvertFrom-Json)[0].aLoadOrder -join ','
Check ($after -eq 'core,C:/Workshop/4242,OtherMod|disabled') "Only the local entry may leave the load order: $after"
Check (-not (Test-Path -LiteralPath (Join-Path $game 'Ostranauts_Data/Mods/PhobosFramework')) -and -not (Test-Path -LiteralPath (Join-Path $game 'BepInEx/plugins/PhobosFramework'))) 'Local folders must be removed'
Check (Test-Path -LiteralPath (Join-Path $game 'Ostranauts_Data/Mods/OtherMod/mod_info.json')) 'Other mods stay untouched'
$backup = @(Get-ChildItem -LiteralPath (Join-Path $repo '.local/installations') -Directory)[0].FullName
Check (Test-Path -LiteralPath (Join-Path $backup 'PhobosFramework/plugin/translations/en.json')) 'Plugin backup must keep subfolders'
Check ((Get-Content -LiteralPath (Join-Path $backup 'receipt.json') -Raw | ConvertFrom-Json).Status -eq 'Removed and verified backups') 'Removal receipt must finish'
Check ((& $remover @paths -Mods Framework) -match 'Nothing to remove') 'Repeat run must be harmless'

Remove-Item -LiteralPath $fixtures -Recurse -Force
Write-Output "Workshop upload and removal tests passed: $script:passed checks."
