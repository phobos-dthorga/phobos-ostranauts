# Claude Code handoff: Phobos Ostranauts

Prepared **28 September 2026** for the owner, **phobosgekko**. This is an
onboarding handoff, not a new feature specification or permission to publish.
Open Claude Code in the repository root and give it this document. Paths below
are relative to that root unless they are Markdown links.

## Instructions to the receiving agent

Help maintain and develop this existing suite of Ostranauts mods. Read the actual
repository before making changes. Follow [AGENTS.md](../../AGENTS.md), including
its later owner clarifications, and any applicable instructions in subdirectories.
Do not assume that your tool automatically loads AGENTS.md: read it explicitly.
This handoff is a navigation aid; current owner instructions and inspected source
take precedence over its dated snapshot.

The aim is longer-term habitation and survival in hostile space: useful salvage,
maintenance, navigation, food and replenishment of real losses. Prefer small,
working features and candidly recommend simplification when an idea is not worth
its gameplay or maintenance cost. Indefinite endurance is an ambition, not perfect
recycling or unlimited matter.

This is a **C# / Unity / BepInEx Ostranauts project**. Do not import Project
Zomboid's Lua APIs, PhobosLib dependency or Build 42 rules into it. Shared services
here belong in **Phobos Framework**.

## First session

1. Read [AGENTS.md](../../AGENTS.md), [README](../../README.md),
   [contributing](../../CONTRIBUTING.md), [building](building.md) and
   [project direction](project-direction.md).
2. Inspect `git status --short --branch`, recent commits, local diffs and
   `git submodule status`. Preserve unrelated work. Check submodule changes before
   updating anything; do not reset, clean or overwrite another session's work.
3. Read the relevant current player guide, owning mod changelog and source for
   the owner's task. Use the [player index](../README.md) and
   [development index](README.md) to find them. Historical proposals are evidence
   of earlier decisions, not proof of today's runtime behavior.
4. Check available PowerShell, .NET and Python tooling. Locate the game through
   existing local installer settings or an explicitly supplied path. Never commit
   that machine path. Do not download or commit proprietary game references.
5. If the owner supplied a concrete task with this handoff, proceed with that
   task and its relevant checks. Otherwise, give a brief readiness report and ask
   which feature or issue to tackle. Do not choose a speculative feature merely
   because a roadmap mentions it.

Do useful read-only onboarding without an approval round. Continue ordinary
authorized implementation autonomously, reporting what works, what was checked
and what remains uncertain. Do not invent time estimates or stop at a plan when
the owner has asked for implementation.

## Repository snapshot, not installed-state evidence

At inspection the checkout was clean on `main`, at commit `697837f`
(`Show staged artwork during D4 R4 and F6 construction`). The local tracking
status showed no ahead/behind count; this was not a fresh remote fetch.
The recorded `external/phobos-scope` submodule was
`345becd49d1408ad179f78f0130f1c02a8e830c5`.

Read current versions from the maintained [mod table](../../README.md#the-mods),
native metadata and [constants catalogue](../../config/maintained-constants.json).
Read required minima from [dependency minimums](../../config/mod-dependency-minimums.json)
and runtime checks. Do not treat old version numbers in research as today's
requirements, or copy another unmaintained current-version table into this handoff.

| Component | Responsibility and present scope | Starting points |
| --- | --- | --- |
| Phobos Framework | Concrete shared inventory, construction, persistence, power/thermal accounting, controls and integration services | [Author guide](framework-author-guide.md), [source](../../src/PhobosFramework), [changelog](../../mods/PhobosFramework/CHANGELOG.md) |
| Phobos Shipbreaker | Wall processing, bounded G4 reclamation, R4 metal recovery, finite material routing, C1 console and electrical F6 furnace | [Player guide](../player-guide.md), [reclamation](../shipbreaker-reclamation.md), [furnace](../furnace-player-guide.md), [source](../../src/PhobosShipbreaker) |
| Phobos Auto Nav | Polaris navigation, docking/departure, pursuit and limited weapon-control functions; owns flight authority | [Departure](../auto-nav-departure.md), [towing](../auto-nav-towing.md), [changelog](../../mods/PhobosAutoNav/CHANGELOG.md), [source](../../src/PhobosAutoNav) |
| Phobos Agriculture | Potato/lettuce cultivation, cooking, irrigation/nutrient solutions, recovery/workup and R3 agricultural bulk storage | [Player guide](../agriculture-player-guide.md), [changelog](../../mods/PhobosAgriculture/CHANGELOG.md), [source](../../src/PhobosAgriculture) |
| Phobos Manufacturing | Buildable research scaffold; no operational machining equipment | [Current status](manufacturing-implementation.md), [research](manufacturing-research.md), [source](../../src/PhobosManufacturing) |
| Phobos Scope | Separate toolkit and pinned source submodule used for optional recording; not a Workshop mod | [Upstream repository](https://github.com/phobos-dthorga/phobos-scope), local `external/phobos-scope/README.md`, [performance guidance](performance-audit.md) |

Framework is required by the content mods. **Shipbreaker also requires Auto Nav**;
do not infer optionality from older design notes or an incomplete publication
manifest. Other integrations remain optional as specified by their owning code.
Manufacturing's only required Phobos dependency is Framework. Approach Assist is
retired; do not restore its package or installer entries.

### Latest work and remaining evaluation

The latest commit binds unfinished construction artwork to D4, R4 and F6 section
assembly. Stages derive from actual delivered parts and work; native completion
creates the finished machine. No saved appearance fields or new material bills
were added. Start with [the construction audit and its subsequent code follow-up](construction-artwork-audit.md)
and [section assembly and maintenance](../section-assembly-and-maintenance.md).
Earlier text saying the artwork is unbound describes the prior audit/preparation
stage. Read the later follow-up and source before proposing to implement it again.

The existing records report offline checks; live lighting, rotation, highlights
and stage transitions still need the owner's evaluation. This handoff does not
verify installed packages or run gameplay. Do not mislabel those pending checks
as a requirement to stop all useful feature development.

Other important stale-plan traps: G4 already has bounded cut/feed/release/traverse/
recapture; R3 bulk storage and station agricultural purchases are implemented;
F6 heating is electrical. Manufacturing machining, broader structural processing
and repeated automatic furnace operation must not be inferred as implemented.

## Engineering rules that must survive the handoff

- **Keep ownership clear.** Shared concrete services belong in Framework; biology,
  recipes, balance and equipment belong in content mods. UI reads/formats state
  and delegates actions to checked services. Auto Nav alone owns flight.
  Reduce duplication and name meaningful constants while doing relevant work;
  avoid speculative abstractions.
- **Preserve saves and physical accounting.** Keep definition IDs, recipe IDs,
  saved keys, existing cargo, captured job contracts and hot state. Revalidate
  identity, scope and resources at mutations. Use measured power and transfers;
  full destinations retain cargo. Do not silently delete, duplicate or reinterpret
  material, unknown records or uncertain native commits.
- **Keep resume policies distinct.** Industrial receiving/processing and docking
  require explicit resume after reload where their contracts specify it.
  Ordinary Auto Nav Fly has its own validated resume policy. Viewing a panel
  grants no operating authority; manual takeover revokes mission authority.
- **Respect native boundaries.** Use existing native installation, hauling,
  carrying, maintenance, restocking and save mechanisms. Every implemented
  placeable family and supported damaged form needs appropriate INSTALL coverage.
  Keep other providers intact and exact ship/endpoint scope enforced.
- **Player language is part of the change.** Follow [the language rule and glossary](player-language.md)
  and [localization](localization.md). Keep complete messages in translation
  catalogs with embedded English fallbacks. Preserve placeholders and stable
  keys. Use practical spacer wording, direct warnings and the literal `Phobos'`
  equipment-name prefix with established brands/models.
- **Evidence and credit travel with claims.** Name and directly link NASA, ESA,
  actual researchers, Blue Bottle Games or original mod authors beside the
  relevant claim. Distinguish observed game behavior, scientific findings,
  proposals and authored gameplay simplifications. A host is not necessarily
  a paper's author; attribution implies no institutional endorsement.
- **Reuse approved artwork.** Follow [asset generation](asset-generation-policy.md),
  [resolution policy](artwork-resolution-policy.md) and the owning asset records.
  Preserve masters, registration, prompts, provenance and repeatable exports.
  Planning does not authorize paid generation. Keep game-derived assets local;
  prefer native runtime references where appropriate.

## Source and maintenance map

`src/` holds runtime C#; `mods/` holds native mod content and changelogs;
`translations/` holds catalogs; `assets/` holds artwork/provenance;
`tests/` and `scripts/` hold checks and repeatable workflows. `config/` holds
maintenance catalogues. Player operations live in `docs/`; contributor,
implementation, research and audit material lives in `docs/development/`.

For each relevant change:

1. Use [the constants updater](updating-constants.md) for registered versions and
   tunables: discover with `--list`, preview `--set Key=value`, apply with
   `--apply`, then verify `--check --format json`. Do not independently edit
   registered copies. Preserve historical contracts and compatibility gates.
2. Maintain the owning `mods/<ModId>/CHANGELOG.md` and
   `workshop/<ModId>/page.bbcode` together. Dated entries remain Draft until real
   publication. Generate release notes with `scripts/workshop-release-notes.py`;
   never hand-edit generated exports. See [publication records](workshop-publication.md).
3. Update all affected linked player guides, translation/language review records,
   INSTALL coverage and [handling audit](item-handling-audit.md).
4. Maintain reviewed item explanations in `config/item-reference.json`, then use
   `scripts/update-item-reference.ps1` for fresh native exports and references.
   `-Check` compares without rewriting tracked outputs. Do not hand-edit generated
   reference tables or `docs/item-reference-data.json`. See [maintenance](item-reference-maintenance.md).
5. Review the final diff, run affected checks and report any remaining limitations.
   Do not manufacture release history or claim untested compatibility.

## Build and verification workflow

Follow [building](building.md) for the supported tool/game baseline. It currently
specifies PowerShell 7 and .NET 10; Python and artwork dependencies are required
by the workflows that use them. Preserve the pinned Scope revision. If the
submodule is missing, initialize the recorded revision only after checking local
state; do not update it to an arbitrary latest revision.

Run commands from the repository root. Set `$gamePath` locally to the actual game
folder. Choose the affected build, rather than blindly running every example:

```powershell
./scripts/build-framework.ps1 -OstranautsPath $gamePath
./scripts/build-autonav.ps1 -OstranautsPath $gamePath
./scripts/build-shipbreaker.ps1 -OstranautsPath $gamePath
./scripts/build-agriculture.ps1 -OstranautsPath $gamePath
./scripts/build-manufacturing.ps1 -OstranautsPath $gamePath
```

Content builds include Framework; Shipbreaker's build also builds Auto Nav.
The scripts run associated checks and prepare `dist/` packages. Many C# checks
are executable projects invoked with `dotnet run`, not ordinary `dotnet test`
suites. Use existing scripts as the authority. Building does not install.

Useful repository checks, selected according to the change and current CI:

```powershell
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python scripts/audit-player-language.py
python scripts/update-constants.py --check --format json
python scripts/workshop-release-notes.py --check --format json
python scripts/update-item-reference.py --check
python scripts/audit-item-handling.py --check
```

When their mechanisms change, also run the relevant unittest discovery, including
`python -m unittest discover -s tests -p test_update_constants.py` and
`python -m unittest discover -s tests -p test_item_reference.py` as applicable.
See [.github/workflows](../../.github/workflows) and build scripts for additional
required checks. Add meaningful regression coverage for changed rules and
persistence; do not add tests that merely restate static documentation.

## Delivery, permissions and licensing

- Owner-directed maintainer work uses ordinary checked commits and direct pushes
  to `main`. **Do not create PRs or start a review/merge workflow unless the owner
  requests it.** Never force-push. Check for concurrent changes before committing;
  keep task scope clear and do not include unrelated work.
- For authorized installation, build selected packages first and use only
  `scripts/install-mods.ps1`, with `-WhatIf` for preview and `-VerifyOnly` for
  installed-file checks. Preserve its game-closed guard and dependency selection.
  Do not manually copy files or edit load order. Manufacturing is a held scaffold:
  the installer accepts an explicit `-Mods Manufacturing` selection but never
  includes it by default. See [installation](../installing-mods.md).
- The owner runs gameplay checks. Do not control their mouse/keyboard, launch an
  interactive test or edit/replace/delete saves without explicit direction.
  Ordinary saves are the baseline; do not invent named-test-save restrictions.
- Keep credentials, personal paths, saves, game assemblies, extracted game art
  and decompiled source out of Git. Private-key generation, conversion and
  credential entry are manual owner steps; never inspect populated secret fields.
- No Steam upload, login, visibility change, public binary release or announcement
  is authorized by this handoff. [Workshop preparation](workshop-upload-preparation.md)
  is offline only. Honor [publication holds](../../config/workshop-publishing.json)
  and retain unknown Workshop IDs as null.
- Read [LICENSE](../../LICENSE) and [third-party notices](../../THIRD_PARTY_NOTICES.md).
  Auto Nav adapts **Gravy / mrkmg's [Auto Navigate](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)**;
  upstream reuse terms remain unresolved and those portions are excluded from the
  project's MIT grant. Preserve attribution to Ostranauts Crafting Framework
  contributors and their retained licence. Public source is not proof of reuse
  permission, installation, publication or gameplay readiness.

Finish each implementation report with the concrete result, relevant checks,
save/dependency implications, delivery state and any short owner gameplay steps.
Keep compiled, tested offline, installed and tested in-game as separate claims.
