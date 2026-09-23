# Installed mods and extension opportunities

Snapshot: **2026-09-23, Australia/Sydney**. This supersedes the 2026-09-20
five-package inventory for current planning. Refresh before implementation if
the owner's subscriptions or enabled mods change.

## Owner preferences

- Extend worthwhile community work instead of repeatedly implementing equivalent
  crafting, hauling, navigation, medical or resource systems.
- Steam Workshop required items are welcome as dependencies of our own mods.
  A cohesive dependent add-on is preferable to an unnecessary standalone clone.
- Where no restriction is explicitly stated, use the owner's **working assumption
  of permissive mod licensing**. Record assumptions separately from verified
  licences and follow explicit terms where present. Lack of a licence file is not
  labelled a verified MIT licence. Preserve credit and any required notices.
- Runtime dependencies are the default reuse route. No upstream source, binary or
  artwork was copied into distributable project files during this research.

## What was checked

Read-only inspection of Workshop `mod_info.json` files, the game's native
`Mods/loading_order.json`, packaged documentation/licences, selected definitions,
Crafting Framework's assembly and current startup logs. No subscriptions, package
files, configuration or saves were changed; no gameplay compatibility test ran.

The observed game process started at **07:32:53** on September 23. The inspected
logs were updated around **07:39-07:41** and report **Ostranauts 1.0.1.4**,
**BepInEx 5.4.23.5**, and the Workshop bridge preloader **1.0.0.0**.

The configuration contains **29 enabled Workshop packages**, plus the local
**Phobos Approach Assist 0.1.1 native package marked disabled**. The BepInEx log
reports **22 plugins loading**, including Phobos Approach Assist. Native-package
enablement and DLL loading are therefore demonstrably different here. This
research did not change either setting. Do not call the prototype fully disabled
based solely on its native load-order suffix.

The inventoried Workshop metadata all appears in the enabled configuration; none
of these 29 packages is classified merely from a surviving download. Nevertheless,
configuration alone does not prove every native definition loaded successfully.

## Configured Workshop packages

Versions are installed metadata versions, not a compatibility certification.
Workshop IDs link directly to the corresponding item; some pages could not be
retrieved by the web reader, so package files are the main evidence here.

| Package | Version | Workshop ID | Relevance |
| --- | --- | --- | --- |
| Ithalan's Additional Ships | 1.5.1 | [3739343366](https://steamcommunity.com/sharedfiles/filedetails/?id=3739343366) | Additional salvage/ship test layouts |
| Very Ship Shaped Shippy Ships for Ship Enjoyers (Absolutely No Boats Allowed) | 2.0.1 | [3774931640](https://steamcommunity.com/sharedfiles/filedetails/?id=3774931640) | Additional ship layouts |
| Good AAAS Ships | 0.2 | [3777373208](https://steamcommunity.com/sharedfiles/filedetails/?id=3777373208) | Additional ship layouts |
| BepInEx Configuration Manager | 1.0.0 | [3745492729](https://steamcommunity.com/sharedfiles/filedetails/?id=3745492729) | Existing settings UI; runtime manager is 18.4.1 plus a 1.0.0 button plugin |
| BepInEx Mod Loader | 0.1.2 | [3741030124](https://steamcommunity.com/sharedfiles/filedetails/?id=3741030124) | Runtime/Workshop loading dependency |
| Ostranauts Crafting Framework | 0.8.71 | [3798573443](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573443) | Preferred crafting and machine extension base |
| Salvage Workshop | 0.8.71 | [3798573453](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573453) | Existing workshop, recovery recipes and equipment |
| OrbitalTrajectoryFixes | 0.6.3 | [3801852103](https://steamcommunity.com/sharedfiles/filedetails/?id=3801852103) | Navigation display compatibility; not a flight controller |
| EVArything Suit Unlocked | 1.1.1 | [3800275558](https://steamcommunity.com/sharedfiles/filedetails/?id=3800275558) | EVA carrying/equipment compatibility |
| Common Sense Behavior | 0.12.6 | [3790481737](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481737) | Crew behaviour/lighting; avoid duplicate behaviour patches |
| Common Sense Salvage and Storage | 0.12.14 | [3790481217](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481217) | Preferred physical hauling/sorting companion |
| Common Sense Cargo Manifest | 0.12.10 | [3790481501](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481501) | Existing cargo visibility; do not revive the abandoned locator as a duplicate |
| Ship's Water | 0.16.1 | [3757331189](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189) | Water tanks, plumbing, recovery and hygiene integration |
| Common Sense Firefighting | 0.12.6 | [3790484261](https://steamcommunity.com/sharedfiles/filedetails/?id=3790484261) | Existing crew response to small fires |
| Common Sense Finances | 0.12.6 | [3790482485](https://steamcommunity.com/sharedfiles/filedetails/?id=3790482485) | Existing bill-payment automation |
| Station Market Relay | 0.3.8.11 | [3800239569](https://steamcommunity.com/sharedfiles/filedetails/?id=3800239569) | Existing shipboard station-market interface |
| G-Immersion Pods | 0.8.3 | [3783626623](https://steamcommunity.com/sharedfiles/filedetails/?id=3783626623) | Powered occupant equipment and alternative nav-console compatibility |
| Study at Terminals | 0.0.1 | [3788237703](https://steamcommunity.com/sharedfiles/filedetails/?id=3788237703) | Existing terminal actions to preserve in the social-terminal proposal |
| Take Their Ships | 0.9.20 | [3782640473](https://steamcommunity.com/sharedfiles/filedetails/?id=3782640473) | Captured-ship ownership/provenance; external salvage must respect it |
| Testudo Safe Pump | 0.3.3 | [3768554122](https://steamcommunity.com/sharedfiles/filedetails/?id=3768554122) | Existing powered O2-bottle filling cabinet |
| Civic Volunteer | 1.1.0 | [3787715657](https://steamcommunity.com/sharedfiles/filedetails/?id=3787715657) | Crew task/shift compatibility |
| Under-Cover | 1.3.1 | [3738764766](https://steamcommunity.com/sharedfiles/filedetails/?id=3738764766) | Clothing/EVA compatibility |
| The little patch for EVArythings V1.1 | 1.1.0 | [3801089823](https://steamcommunity.com/sharedfiles/filedetails/?id=3801089823) | Already patches Under-Cover/EVArything overlap; runtime recovery plugin reports 0.2.0 |
| Seek Ship - No Age | 1.0.1 | [3781665308](https://steamcommunity.com/sharedfiles/filedetails/?id=3781665308) | Character-generation change; no machinery extension role identified |
| Solar Panels | 0.1.8 | [3738765255](https://steamcommunity.com/sharedfiles/filedetails/?id=3738765255) | Alternative electrical supply; logged plugin version is 0.1.7 |
| Orbit Markers | 1.3.1 | [3768492887](https://steamcommunity.com/sharedfiles/filedetails/?id=3768492887) | Navigation UI coexistence |
| First Aid for NPCs | 0.5.0 | [3786464764](https://steamcommunity.com/sharedfiles/filedetails/?id=3786464764) | Existing NPC treatment; runtime name is Aid NPC |
| Aerofects | 1.0.0 | [3757708925](https://steamcommunity.com/sharedfiles/filedetails/?id=3757708925) | Atmospheric/nozzle content; no machinery API verified |
| Target Lead Computer | 0.4.16 | [3785752174](https://steamcommunity.com/sharedfiles/filedetails/?id=3785752174) | Installed nav module compatibility; ballistic lead is not a demonstrated salvage-targeting API |

Package/plugin version differences above are observations, not diagnoses of a
broken installation. Exact binary fingerprints are preserved in the ignored
inventory report if a later compatibility problem needs investigation.

## Extension decisions

| Existing work | Reuse direction | Dependency decision / remaining evidence |
| --- | --- | --- |
| Crafting Framework | JSON recipes, stations, blueprint access, ingredient collection and suitable automatic processing | Proposed required item for the onboard add-on. Installed code supports discovery of other mods' recipe packs; advanced process hooks remain limited. |
| Salvage Workshop | Existing bench, material recipes, sorter, charging and supply distribution | Proposed required item for the first dependent expansion. Add a distinct process; do not replace its broken-item recovery. Drop the hard dependency only if the final feature genuinely uses none of its content. |
| Common Sense Salvage and Storage | Let crew move real workpieces and outputs using current orders and bins | Recommended optional companion. Machine input/output compatibility must be tested; no public API was verified. Its README explicitly says not to enable Smart Orders alongside it. |
| Common Sense Cargo Manifest | Allow new physical items to appear through existing inventory visibility | Optional; test recognition/category handling. No second shipwide manifest. |
| Ship's Water | Use its water state for appropriate washing/coolant-consumption recipes | Optional. OCF already has an enabled adapter. Drinking water must not silently become an infinite industrial coolant or a new closed loop. |
| Testudo Safe Pump | Retain its bottle-filling cabinet when gas production becomes relevant | Optional companion; produce compatible feedstock/storage where possible. No general gas-filling API verified. |
| First Aid for NPCs / G-Immersion Pods | Reassess medical tasks and installed patient equipment against what the owner already has | Research when returning to medicine. Current metadata does not establish reusable public clinical APIs. |
| Study at Terminals | Add social actions without wiping out the installed study actions | Compatibility requirement for terminal research; metadata URL is a placeholder, not a usable source repository. |
| Station Market Relay | Let normal vendor stock expose machinery/materials through an existing trading interface | Optional compatibility; no remote-market system required for our add-on. |
| Take Their Ships | Respect transferred ownership and wreck-title restrictions | Optional integration for external shipbreaking. Do not invent a parallel ownership/claim system. |
| Native/third-party navigation | Reuse existing guidance and preserve provenance | Owner selected a [standalone Auto Navigate adaptation](auto-navigate-adaptation.md), with no original-mod dependency. Built, not tested in-game; external cutting still needs separate work-position control. |

Shipbreaker 0.1.2 now isolates its template adaptation in `WorkshopAdapter`, checks
the specific dependency contract before publishing definitions, rolls back failed
publication, and verifies both construction recipes before enabling processing.
`phobosshipbreaker dependencies` exposes these checks. This is early compatibility
hardening, not a replacement for OCF/Workshop or missing-dependency save recovery.

## Licences actually found

Auto Navigate was separately reviewed on 2026-09-23; see the
[source, licence and community-policy findings](auto-navigate-reuse-review.md).
The owner downloaded it and kept it disabled. No bundled licence was found.
The selected standalone adaptation preserves the original author and binary
provenance in [third-party notices](../THIRD_PARTY_NOTICES.md); public release is
intended, but that intention is not recorded as an upstream licence grant.
No public blanket permission was found for that mod or for unlicensed Ostranauts
mods generally. Keep verified grants separate from the owner's working assumption.

Crafting Framework and Salvage Workshop each include `LICENSE.txt`, granting MIT
terms for **newly authored framework code and documentation**. They expressly
exclude game artwork, game-derived definitions, game binaries and other
third-party components from that grant. Preserve this scope if copying anything;
a Workshop dependency avoids repackaging their content.

All five installed Common Sense modules include a `LICENSE` file identifying
MIT and LOGUSS. The salvage/storage README also states MIT. Keep attribution and
the copyright/permission notices with any substantial copied code.

The other package trees did not contain a licence-named file in the inventory
scan. Treat them under the owner's permissive working assumption unless explicit
restrictions are encountered. This is not a comprehensive legal audit of linked
sites. None of the candidate integrations is blocked on an unsolicited permission
request, and no author was contacted during this task.

## Workshop and runtime dependencies

For prolonged incompatibility or unavailable upstream packages, use the
[dependency contingency plan](dependency-contingencies.md). It records specific
options for Crafting Framework, Salvage Workshop, optional helpers and the
standalone Auto Nav adaptation, including current save-recovery limitations.

Planned first add-on dependency chain:

`BepInEx Mod Loader -> Crafting Framework -> Salvage Workshop -> Phobos add-on`

List required Workshop items in the eventual release and explain required native
load order. Also verify the framework DLL/version at runtime and disable only
unsupported Phobos features with a clear message. Do not ship private copies of
other mods' DLLs to force a preferred version.

Valve documents Workshop item dependencies as **soft relationships**. Required
items alone do not prove correct game load order, completed BepInEx setup,
compatible versions or successful plugin loading.
[Steamworks dependency documentation](https://partner.steamgames.com/doc/api/ISteamUGC#AddDependency).
Ostranauts also replaces same-named native definitions according to load order,
so broad whole-file overrides are a poor integration strategy.
[Official modding guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946).

Optional mods should be detected by their actual loaded data/plugin identity,
not folder existence alone. Avoid references to absent optional definitions and
report unsupported combinations without changing the player's configuration.

## Observed framework startup and open questions

- Framework and Salvage Workshop are both **0.8.71**. The framework registers
  **21 recipes and one powered machine**, with its Ship's Water adapter enabled.
- The two excluded recipes are `SWB_RustysTwinO2` and
  `SWB_RustysTwinEVABattery`; their output objects are missing. Installed metadata
  explicitly calls for the original Rusty item mods for those recipes. This is
  not evidence that the whole workshop failed. No missing mod was installed.
- OCF and Common Sense both touch work/ingredient movement. Their successful
  startup is not proof that simultaneous hauling and crafting are conflict-free.
- The framework's author-supplied guide link and the three principal Workshop
  pages could not be retrieved through the web reader. Their installed metadata,
  licences, recipes and code were inspected directly; no unseen guide was used.
- Refresh after upstream updates. The framework advertises active development;
  the inspected automation limitations may change.
- The [second shipbreaking round](powered-shipbreaking-design-findings.md) examines
  native active/idle power, machine-level progress without input binding and
  multi-output placement. These refine the Phobos extension boundary; none was
  exercised in the running game.

## Repeating the inventory

The later [material-use review](shipbreaker-material-uses.md) refreshed the
inventory to 31 known packages (29 enabled, two disabled, including the downloaded
Auto Navigate). Framework/Workshop versions remain 0.8.71. It maps the panel
outputs to existing repairs/fabrication and records the upstream mass-accounting
limits separately from compatibility.

Run `scripts/inventory-mods.py` with `--game-path`, optionally `--workshop-path`
and `--player-log`, and `--output .local/research/mod-inventory-YYYY-MM-DD.json`.
All machine paths are supplied at runtime. The script reads metadata, licence
filenames, assembly hashes and selected startup records; it never reads saves.
Reports are local research, not a file to commit.

The 2026-09-23 run read all **30 packages** (29 Workshop plus local Phobos), with
**zero metadata errors**, and found 22 startup plugin entries. Its parser accepts
the literal description newlines present in one installed ship pack. A local
fixture smoke check also covers comments, trailing commas, disabled entries,
unconfigured downloads and output-path protection.
