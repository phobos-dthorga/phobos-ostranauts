# Help and troubleshooting

New here? Start with [getting started](docs/getting-started.md). Questions, bugs
and suggestions belong in [GitHub Issues](https://github.com/phobos-dthorga/phobos-ostranauts/issues/new/choose).
Search existing issues first; a short question is welcome when you are unsure.
Support is best-effort, without a promised response time.

## Common problems

| Symptom | What to check |
| --- | --- |
| Source ZIP has no DLLs / package missing | Source archives are not installable releases. See [download status](docs/getting-started.md#can-i-download-and-play-today) and [builds](docs/building.md). |
| Launcher cannot find PowerShell | Install PowerShell 7; Windows PowerShell 5.1 is insufficient. |
| Build cannot find Phobos Scope | Run `git submodule update --init --recursive` in a Git clone. ZIPs omit submodules. |
| No Phobos commands or equipment | Check loader setup, plugin/native files, enabled native entries and startup log. Run installer `-VerifyOnly` for selected mods. |
| Framework version mismatch or duplicate | Keep one compatible Framework installation. Inspect reported paths; do not delete unrelated plugins. |
| Update refused while game runs | Close Ostranauts normally, then repeat. |
| Merchant has no new items | Stock is additive and probabilistic; existing inventories may not refresh immediately. See [acquisition](docs/equipment-economy.md). |
| Auto Nav refuses Fly / Resume | Read Details and `phobosnav status`; check live contact, braking room and competing providers. Original Auto Navigate must be disabled for ours to engage. |
| Machines paused after reload | Resume processing and receiving separately. Opening a panel never restarts work. |
| C1 cannot see a docked neighbour | The console controls only its own authorized ship. |
| Reclaimer/furnace refuses work | Read its reason: inputs, output room, power, atmosphere, heat and cooling are interlocks. Follow its equipment guide. |

For interrupted installation, keep the game closed and retain the
`.local/installations/` receipt and backups. There is no automatic rollback;
see [recovery guidance](docs/installing-mods.md#what-an-update-does). Do not delete
saves or required content providers as a troubleshooting shortcut.

## A useful bug report

Include game/BepInEx/mod versions, expected and actual behaviour, reproduction
steps, and whether it began after an update/reload. Name relevant mods and
equipment. Screenshots help; a complete save is not needed initially.

F3 diagnostics for installed components:

```text
phobosframework status
phobosshipbreaker dependencies
phobosnav status
```

For startup failures, inspect `BepInEx/LogOutput.log` in the game directory.
Share relevant errors and nearby context, after redacting private details such
as personal paths and usernames. Never upload credentials, full game assemblies,
extracted assets or other authors' packages. Sensitive reports: [SECURITY](SECURITY.md).
