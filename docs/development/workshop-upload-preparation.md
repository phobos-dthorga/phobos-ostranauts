# Preparing Steam Workshop uploads

This workflow prepares local candidates only. Neither script logs in to Steam,
starts SteamCMD, uploads files, changes visibility, or marks a release published.
Steam credentials and API keys are not inputs. Source/package checks cannot prove
that a future subscriber can load the mod: that requires a private subscription test.

## Commands

```powershell
# Inspect an existing prepared package without writing anything; JSON output.
python scripts/prepare-workshop.py --mod Framework

# Build with the existing local game settings, then stage a candidate.
./scripts/prepare-workshop.ps1 -Mod Framework -Build -Prepare

# Stage an already rebuilt package; does not compile or install it.
python scripts/prepare-workshop.py --mod Agriculture --prepare

# Recheck every staged file and the upload draft against its saved manifest.
python scripts/prepare-workshop.py --verify '<candidate directory>'

# Regression tests (no game installation or Steam account needed).
python -m unittest discover -s tests -p test_workshop_preparation.py
```

Use `-OstranautsPath` with the PowerShell wrapper to override the saved local game
path. Python 3.10+ and PowerShell 7 are required; builds also need the prerequisites
in [building](building.md). Use `-Build` after code changes: the offline checker
compares packaged DLLs to compiled outputs, not C# source to machine code.

Each preparation creates a fresh directory under ignored `.local/workshop-staging/`.
It contains `content/`, `manifest.json` and `workshop.vdf.draft`. Older candidates
are retained; no automatic deletion occurs. The manifest records version, source
commit, dirty-tree status, operation, required-item IDs, blockers and SHA-256 hashes.
A successful preparation means **offline staging succeeded**, even when publication
blockers remain. `uploadEnabled` is always false. Exit 1 means validation failed;
exit 2 is invalid arguments. Default operation is a read-only preview.

## Content layout and checks

The Workshop content root contains native `mod_info.json`, `data/`, artwork and
framework definitions. The mod's own plugin folder is at `BepInEx/plugins/<ModId>/`;
Framework additionally carries its pinned Phobos Scope recorder. Supporting package
documentation is under `documentation/`. The installer ZIP's outer `Mods/<ModId>/`
wrapper must not become the Workshop root.

This follows **EddieSM / EsMM27's BepInEx Workshop bridge**
[documented layout and synchronization behaviour](https://github.com/EsMM27/BepInEx_Loader_Ostranauts#readme).
The bridge copies enabled Workshop plugin payloads to its managed installation
area. BepInEx itself still needs its normal installation; see the
[author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3741030124).
This is source-backed packaging preparation, not a tested subscription install.
Do not leave duplicate local and Workshop copies of the same Phobos plugin active
when eventually testing. Plan migration and backup before changing an existing save.

Preparation checks current native source files against the package, generated notes,
page version/title, required data content, DLL presence and compiled-byte agreement,
translation freshness and permitted payload types. It rejects filesystem links.
Hashes detect accidental staging changes, not malicious manifest replacement or
proof of remote installation. Rebuild and regenerate instead of editing staged files.

## Item IDs, holds and prerequisites

`config/workshop-publishing.json` holds the game App ID, per-mod item IDs and
required Phobos mods. Item IDs remain null until Steam actually creates them.
Never invent IDs. Copy verified real IDs into the catalogue in a normal commit;
subsequent drafts then select an update rather than creation. Keep dependencies in
sync with the actual plugin requirements as the mods evolve.

The BepInEx Workshop bridge is listed as an external required item. Framework must
receive its real item ID before its consumers can have complete dependency lists.
SteamCMD's basic VDF does not configure these required-item relationships: set and
verify them separately through the Workshop editor or a future UGC integration.
Optional providers must not become mandatory required items.

Auto Nav remains held for unresolved upstream distribution terms. Manufacturing
retains its scaffold hold. Missing covers and
unpublished dependency IDs are reported as blockers. Do not treat a generated draft
as permission to bypass these holds. Resolve them in the source catalogue/docs.

## Later: an explicitly authorized private upload

**No upload has been performed by preparing these tools.** Before enabling one:

1. Review the exact candidate, manifest, distribution rights and blockers; run
   `--verify`. Build a fresh candidate if the source or description changed.
2. Validate the SteamCMD installation and log in interactively with the publishing
   account. Enter passwords and Steam Guard in Steam's prompt, never in a script,
   argument, repository or report. A Web API key is not this account session.
3. Following a separate authorization, supply the reviewed VDF to SteamCMD's
   `workshop_build_item` command. Drafts specify private visibility (`2`). Valve
   documents this route for testing. The `.draft` suffix is a review convention,
   not a technical lock; SteamCMD must never be invoked by preparation tooling.
4. A zero item ID creates a new item. Valve says SteamCMD writes the resulting ID
   back into the VDF. After an error or timeout, inspect the account and upload logs
   before retrying creation: a retry might create a duplicate. Preserve the original
   candidate, returned ID and logs as a local receipt. Record the verified ID in the
   catalogue before any later update. A Steam-mutated draft will fail `--verify`;
   retain it as evidence and generate a new candidate for subsequent work.
5. Check the actual item's app, owner, private visibility, title, description,
   changelog, cover and required items. Accept Workshop legal terms manually if
   prompted. A successful upload response alone is insufficient verification.
6. Test a clean subscription installation with BepInEx and the bridge, check startup
   logs and native recognition, and test gameplay. Only then consider public release.
   Public visibility and Released changelog status require actual publication.

Primary reference: **Valve**, [Steam Workshop implementation guide, SteamCMD
integration](https://partner.steamgames.com/doc/features/workshop/implementation#SteamCmd).
Valve also documents the Steamworks UGC creation/update path there. An independently
registered publishing application may require game-developer configuration; it is
not assumed to be available to our account. This task has not validated SteamCMD
account permissions, real uploads, BBCode rendering or remote metadata readback.

A future uploader should consume this manifest, require explicit create/update and
visibility selection, refuse blockers, save a pending receipt before contacting
Steam, preserve failures for reconciliation, and verify the remote result. It must
not upload automatically on Git commits or be added to the documentation CI job.
