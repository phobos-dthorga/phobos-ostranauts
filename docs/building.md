# Building from source

This is the developer/experimental route. Start with [getting started](getting-started.md)
for download availability and game prerequisites.

## Prerequisites

- Windows, Git and [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows).
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Scope also
  records its SDK version in `external/phobos-scope/global.json`.
- Local Ostranauts **1.0.1.5** with **BepInEx 5.4.23.5**. Game and loader assemblies
  are resolved locally, never downloaded by our builds.

Rust is needed only for Scope's standalone analyser, not Framework's C# recorder.
Python/Pillow are needed for relevant artwork regeneration, not ordinary committed
Agriculture exports.

## Clone and prepare packages

```powershell
git clone --recurse-submodules https://github.com/phobos-dthorga/phobos-ostranauts.git
cd phobos-ostranauts
# For an existing clone, fetch the recorded dependency revision:
git submodule update --init --recursive

# Your game directory (Steam: Manage → Browse local files).
$gamePath = 'D:/SteamLibrary/steamapps/common/Ostranauts'

# Choose content. Each content build also builds Framework.
./scripts/build-shipbreaker.ps1 -OstranautsPath $gamePath
./scripts/build-autonav.ps1 -OstranautsPath $gamePath
./scripts/build-agriculture.ps1 -OstranautsPath $gamePath
```

Run from the repository root in PowerShell 7. Replace the example game path.
Build scripts run associated checks and stop on failure. They create unpacked
`dist/Phobos…-P0/` folders and ZIPs; `P0` is a directory convention, not the mod
version. Native `mod_info.json` records the version.

Framework alone uses `scripts/build-framework.ps1`. Manufacturing has
`scripts/build-manufacturing.ps1`, but no operational machine and no supported
installer selection. Approach Assist is retired and no longer built.

## Preview, install and verify

Close Ostranauts. Select only mods whose packages you built:

```powershell
./scripts/install-mods.ps1 -Mods Shipbreaker -OstranautsPath $gamePath -WhatIf
./scripts/install-mods.ps1 -Mods Shipbreaker -OstranautsPath $gamePath
./scripts/install-mods.ps1 -Mods Shipbreaker -OstranautsPath $gamePath -VerifyOnly
```

Framework is selected automatically. Use `-Mods Agriculture` for Agriculture,
or `-Mods AutoNav,Shipbreaker,Agriculture` for multiple content mods. The launcher
defaults to Auto Nav + Shipbreaker, so both packages must exist before using it.
Building never installs or launches the game.

See [installation and recovery](installing-mods.md), then [the player guide](player-guide.md).

## Contributing and checks

For routine version/default updates, use the [maintained-constants updater](updating-constants.md).
It previews exact changes and can emit JSON verification; builds remain separate.

Follow [CONTRIBUTING](../CONTRIBUTING.md). Run the affected build script when code,
native data or packaged artwork changes. For documentation, run
`python scripts/check-doc-links.py`. Public documentation checks do not build
against proprietary game assemblies. Record gameplay checks separately.

Before redistributing, read [third-party notices](../THIRD_PARTY_NOTICES.md).
Auto Nav's upstream-derived portions retain unverified reuse terms. Public
source and successful builds do not resolve that provenance issue.
