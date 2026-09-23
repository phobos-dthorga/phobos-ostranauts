# Installing and updating our mods

Close Ostranauts, then double-click **`Install or Update Mods.cmd`** in the
repository root. It installs or updates the latest **prepared packages** for
Phobos Auto Nav and Phobos Shipbreaker. Use the same launcher for subsequent
updates. PowerShell 7 is required; the launcher leaves its result visible.

The installer finds Ostranauts through Steam's library records and remembers
successful installation paths in `.local/install-settings.json` (ignored by Git).
It uses `Ostranauts_Data/Mods/loading_order.json` by default. For a customised
native Mods location, supply the actual load-order file on the first run:

```powershell
./scripts/install-mods.ps1 -OstranautsPath '<game folder>' -LoadOrderPath '<configured Mods folder>/loading_order.json'
```

Explicit paths override saved paths. If Steam has more than one Ostranauts
installation, select one with these arguments. Filesystem links/junctions in our
package or installation destinations are rejected; supply direct paths.

## Using the same script from a terminal or Codex

Run these from the repository root in PowerShell 7. Codex can use these commands
directly without interacting with your mouse or opening a launcher window.

```powershell
# Install/update both current mods.
./scripts/install-mods.ps1

# Install/update just one.
./scripts/install-mods.ps1 -Mods AutoNav
./scripts/install-mods.ps1 -Mods Shipbreaker

# Preview without changing anything, even while the game is running.
./scripts/install-mods.ps1 -WhatIf

# Compare installed files and enabled load-order entries with the packages.
./scripts/install-mods.ps1 -VerifyOnly
```

The old pulse-only Approach Assist prototype is **not included by default**.
Explicitly select `-Mods ApproachAssist` if wanted. Its previous
`install-approach-assist.ps1` command remains supported through the same installer.
Installing AutoNav does not remove that old plugin or enable original Auto
Navigate. AutoNav still enforces its runtime navigation-conflict checks.

## What an update does

- Checks **all selected packages** before copying any of them: required data and
  artwork, readable JSON, matching native/plugin versions and assembly identity.
- Places each own DLL in `BepInEx/plugins/Phobos…/` and its native definitions,
  artwork and included native notices in the configured Mods folder.
- Enables selected native entries in place or appends new entries. Existing
  unrelated entries, disabled mods and other load-order settings stay intact.
- For Shipbreaker, requires enabled Crafting Framework data version 0.8.71+,
  Salvage Workshop, the framework DLL (installed, or available through the
  Workshop bridge), and the correct native order. Missing requirements stop the
  entire selected update; it does not download or enable other authors' mods.
- Skips matching files, checks copied files with SHA-256 and reads back the
  load order. A second identical run changes no game files.
- Before replacement, backs up changed existing files and the original load
  order under `.local/installations/`. Writes a recovery receipt before copying,
  including which destination files are new, then marks it verified on success.

The framework's assembly version is not its BepInEx plugin version: the inspected
0.8.71 release uses assembly version 0.0.0.0. The installer checks native metadata
and DLL identity; BepInEx and Shipbreaker's runtime safeguards check the loaded
plugin version and usable definitions at startup. A successful install is **not
an in-game compatibility test**.

The installer refuses actual updates while Ostranauts is running. Close it
normally and run again. It never stops the game, launches it, accesses saves,
changes player settings, removes files, or overwrites original game data.
Unexpected extra files in our destination folders stop an update for inspection,
so obsolete code and user additions are not silently retained or deleted.

If copying fails, **keep the game closed** and retain the reported backup folder.
This is not a transactional installer and does not automatically roll back.
Its receipt records intended targets and prior existence; previous contents and
`loading_order.before.json` support inspection and recovery. Ask Codex to resolve
the failure before launching. Do not treat these snapshots as verified gameplay
rollback versions; see [dependency contingencies](dependency-contingencies.md).

## Where updates come from

Packages are read from `dist/PhobosAutoNav-P0` and `dist/PhobosShipbreaker-P0`.
The installer does not rebuild source code, download releases or alter Workshop
subscriptions. After code or artwork changes, Codex should run the corresponding
existing build script, then this installer. A missing package reports that step.
`-PackageRoot` selects another prepared-package directory; `-PackagePath` selects
one unpacked package when exactly one mod is selected. These overrides are not
saved. `-NoRememberPaths` suppresses saving machine paths, useful for fixtures.

## Installer checks

```powershell
./tests/install-mods.tests.ps1
./tests/install-approach-assist.tests.ps1
```

The checks use temporary installations under `.local/script-tests/`, prepared
packages, and a read-only copy of the installed Crafting Framework DLL. Supply
`-FrameworkDllPath` to the multi-mod tests if it cannot be found automatically.
They exercise fresh and repeated installs, file repair/backups, complete preflight
before a multi-mod update, artwork preservation, dependency/order failures,
running-game refusal, custom paths and junction refusal. They never write into
the real game installation or perform gameplay tests.
