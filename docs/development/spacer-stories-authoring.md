# Phobos Spacer Stories: authoring record

Initial collection prepared 6 October 2026 at the owner's request: **220 original entries**, about
**13,700 words**, in nine story-schema files. The owner asked for the bulk of company,
product and world writing overnight, with content that would make sense alongside
vanilla Ostranauts. All new storytelling behaviour is data in the existing schema.

The initial inventory below is historical. The place-based 0.2.0 rewrite added
nine outcome headlines; the [0.3.0 vanilla-world expansion](spacer-stories-vanilla-expansion.md)
adds 164 entries in four more overlays. The current total is 393 content entries,
22 chains, 50 steps and 23 readable files. That expansion record contains the new
setting evidence, literary influences, rewards, travel choices and code handoff.

## Read the collection

The data-only add-on is [Phobos Spacer Stories](../../mods/PhobosSpacerStories), a
first-party Phobos mod that players subscribe to separately. Its
[manifest](../../mods/PhobosSpacerStories/phobos-addon.json) carries the current
Framework minimum. It adds no plugin, equipment definition,
native plot, world location, interaction or schematic. It is held from Workshop
publication until the owner has checked it in the game.

| Source file | Contents |
| --- | --- |
| [Yard and lines](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/01-yard-and-lines.json) | Rivetline and Ablatine; recovery yards, stores, drain cans and mining tools |
| [Flight and rebuilding](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/02-flight-and-rebuild.json) | Asterel, repair records, navigation assistance and the drawings crews keep |
| [Growing and the galley](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/03-growing-and-galley.json) | Verdemorrow, the first invoice, planting-stock reserves and meals after shift |
| [Process makers](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/04-process-makers.json) | Fennmark, Tolvane, Lixivar and Oxsmith; different jobs in a mixed plant |
| [The still and remainders](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/05-still-and-remainders.json) | Alembrine, Slingwright, a borrowed pump, disputed grain and the work of loading |
| [Sickbay](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/06-sickbay.json) | Halewright, fitting letters, access, observation and people doing care work |
| [Letters and local history](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/07-letters-and-local-history.json) | The second-shift ledger, Sable Margin and Short Return accounts, and outcome news |
| [Goal chains](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/08-goal-chains.json) | Nine correspondence chains, 22 steps, through existing checked story actions |
| [Data-card files](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/09-data-card-files.json) | Nine fuller archive documents, given on native data cards at chain completion |

| Channel | Entries |
| --- | ---: |
| TV news, each with a small-talk mention | 42 |
| Adverts | 26 |
| Additional small-talk lines | 84 |
| Loading lore | 26 |
| Encyclopedia articles | 24 |
| Goal chains | 9 |
| Readable archive files | 9 |

The company histories cover eleven existing makers. The collection uses Framework's
existing Makers and brands and Life between stations encyclopedia sections. It
leaves the game's and Framework's selection shares unchanged.

## Setting evidence and invention

**Vanilla setting evidence.** Blue Bottle Games describes a scavenger's life of ship
ownership, debt, salvage, fuel, air and food in a System separated from a damaged
Earth in its [official Ostranauts Steam description](https://store.steampowered.com/app/1022980/Ostranauts/).
That supports the scale and concerns of these stories; it does not establish any of
our companies or incidents as canon.

**Read-only primary game evidence.** Blue Bottle Games' installed Ostranauts 1.0.1.5
data were read directly: `tips/tips.json`, `headlines/headlines.json`, `ads/ads.json`
and the opening encyclopedia material in `info/infoNodes.json`. The lore entries
TipLoreStartDate, TipLoreOKLGGeneral, TipLoreLunaGeneral and TipLoreVoltaire establish
the existing chronology, shipping origins, abandoned infrastructure and corporate
research context. The news and adverts establish that labour disputes, commercial
pressure, local mishaps and unsettling rumours belong in the world's everyday media.
These are observations of Blue Bottle Games' own text, not paraphrases of an
unverified community lore site. No extracted text or assets are shipped here.

**Mod evidence.** The [branding record](equipment-branding.md),
[item references](../item-references.md), [player-language rules](player-language.md)
and [story authoring guide](../writing-story-content.md) supply maker identities,
product names, implemented functions and the available channels. Original fictional
history is kept separate from the cited research behind recipes and equipment.
Nothing here establishes a new research result, yield, performance figure,
clinical outcome, chemical reaction or institutional endorsement.

**Agent-authored fiction, open to owner revision.** Company origins, the second-shift
ledger, Neri Vale, Orra Pell, Sable Margin, Short Return, the borrowed-pump accounts
and all new incidents are inventions. The correspondence uses named senders without
creating NPCs. The named ships are offscreen history, with no reachable ship or
quest location promised. No company receives control over a vanilla faction,
station, historical catastrophe or major timeline event. Founding stories often
remain contested crew accounts rather than an omniscient explanation.

**Delivery.** The collection uses the data-only add-on route, so every maker can be
covered without adding story registrations to content-mod code. Framework reads the
nine files as its story overlays. Existing shipped seeds remain intact. It was
written under `examples/addons/` and moved to `mods/PhobosSpacerStories` on
6 October 2026 (owner choice: its own add-on mod, rather than splitting it into each
content mod's pack, which keeps the cross-file chains together and lets players opt
in). It has its own changelog and Workshop page draft; existing mods and their
Workshop releases are not changed by it.

## The linked stories

The **second-shift ledger** preserves work omitted from cleaner accounts of a
refit. The Sable Margin's two contractors claim the same shipment; crew letters
describe parts bought and repaired from wages. Correcting a drain-can description
does not settle ownership. An ingot comparison does not prove where an old batch
was made. An Asterel letter preserves a watchkeeper's repair. Verdemorrow's galley
photograph acquires the names of the people who actually provided supper.

The **Short Return** letters connect Halewright's fitting concerns with crew care:
space around a bed, a clear approach, supplies and a watch maintained by people.
The surviving account does not diagnose a patient or assign a medical success rate.

The other correspondence covers **four accurate leaflets that do not describe one
complete plant**, **two irreconcilable accounts of a missing pump**, **an old ship
drawing confused with a current one**, and **a worker left out of a feeder's
publicity photograph and its fee**. Some disputes stay unresolved. Completion
means the correspondence closed, not that the world awarded a convenient verdict.

## Rewrite onto places, people and threads (0.2.0)

The owner disabled the collection on 6 October 2026 because its stories came "out of
nowhere with absolutely no relation to anything or anywhere". Framework 0.114.0 added
places, people, threads and progress gates; ChatGPT rewrote the collection under the
[rewrite handoff](spacer-stories-rewrite-handoff.md) (owner commit `0f49e77a`), and
Claude reviewed it against the handoff's rules.

| Thread (after `spacertales-`) | Home place | Cast |
| --- | --- | --- |
| second-shift-ledger | `oklg` | Neri Vale, Mera Dain, Kes Arven, Sen, Alin |
| flight-service | `oklg` | Kes Arven, Neri Vale |
| yard-trade | `oklg` | open |
| beam-contractors | `bwvn` (Weaver's Needle) | open |
| refit-drawings | `oklg-flot` (the Flotilla) | Neri Vale |
| galley-trade | `oklg-flot` | Mera Dain, Neri Vale, Sen, Alin |
| mixed-plant-trade | `bcer` (Port Mojave) | Orra Pell |
| still-correspondence | `coho` (Corsair's Hollow) | Edda Rusk |
| sickbay-correspondence | `mvol-med` (Central Medical Facility) | Ilen Marr |
| loading-work | `svir-shipyard` (Titan Shipyards) | Tavi Sen |

Homes were ChatGPT's choices under the handoff (it took the suggested OKLG, Ceres and
Deimos homes); the owner may revise any of them.

**Review (6 October 2026).** Every handoff rule holds: all 220 earlier ids and every
step id kept (now 229 story entries, 9 of them new outcome-news items, plus 9 people
and 10 threads); every arc, news item, advert, small-talk line and file in a thread; every
thread and home a Framework place; every letter from a cast member with no free-text
sender; all fifteen known spoilers gated (news and talk on `arcsAtStep`, `arcsDone`,
`filesRead`) or reworded as background (tips and articles); no `dock-at` to any
station; no Framework mod gate; no standing change. All 181 new or changed strings were
read: in voice, no claimed mechanic the code lacks, item names matching the game's.
Small talk is now 26 crew, 30 locals and 28 anyone. Flags were not used; chains connect
through `arcsDone`, `filesRead` and `newsSeen`. Standalone chains start only after
their own introductory news has been shown, and only at their home place.

| Chain id after the spacertales- prefix | Home | Starts when (besides being at home) |
| --- | --- | --- |
| misfiled-can | `oklg` | The receiving-desk notice has been shown |
| yard-signatures | `oklg` | First chain done, SABLE_FREIGHT.TXT opened; Shipbreaker present |
| meal-in-the-margin | `oklg` | First chain done, file opened; Agriculture and a Firstlight-4 aboard |
| asterel-old-number | `oklg` | First chain done, file opened; Auto Nav and an N1 module aboard |
| drawing-after-damage | `oklg-flot` | First chain done, file opened; War Has Been Declared present |
| four-leaflets | `bcer` | The mixed-plant news shown; Manufacturing and a V4 aboard |
| borrowed-pump | `coho` | The Alembrine letters notice shown; Manufacturing and a Copperhead-3 aboard |
| room-for-one-more | `mvol-med` | The Halewright archive notice shown; Medical and a Ward-3 aboard |
| last-vessel | `svir-shipyard` | The Slingwright letters notice shown; Manufacturing and an RM-1 aboard |

Steps that finish at a station now finish at the chain's home (`dock-at` with no
station), and "being here already counts" in their text is true of that test.

## Goals, pacing and saved structures

The table below describes 0.1.x; the 0.2.0 start conditions are in the section above.

Agent defaults: independent starts use chances of 0.003 or 0.005 at Framework's
ordinary check; the three ledger follow-ups use 0.01 only after their opening
conditions hold. Framework's existing active-chain limit remains two. None is
repeatable. Dismissing a goal sets its chain aside through the existing system.
In-game pacing is untested and can be revised without changing the premise.

| Chain id after the spacertales- prefix | Starts when | Actions |
| --- | --- | --- |
| misfiled-can | D20 aboard, at a station | Wait for a note, carry the can at a station, wait for the correction |
| yard-signatures | First chain done and SABLE_FREIGHT.TXT opened; Shipbreaker present | Carry an aluminium ingot at a station, wait for repair letters |
| meal-in-the-margin | First chain done and file opened; Agriculture and a Firstlight-4 aboard | Wait, carry lettuce at a station, wait for the caption |
| asterel-old-number | First chain done and file opened; Auto Nav and an N1 module aboard | Wait for a service letter, acknowledge at a station |
| four-leaflets | Manufacturing and a V4 aboard, at a station | Wait, return the quotation at a station, wait for the closing note |
| borrowed-pump | Manufacturing and a Copperhead-3 aboard, at a station | Carry spirit at a station, wait for conflicting accounts |
| room-for-one-more | Medical and a Ward-3 aboard, at a station | Wait for fitting and watch letters, acknowledge at a station |
| drawing-after-damage | War Has Been Declared present, at a station | Wait for a drawing account, acknowledge at a station |
| last-vessel | Manufacturing and an RM-1 aboard, at a station | Wait for the photograph and fee correspondence |

All carried examples are retained. Bought or found examples are explicitly accepted;
no goal claims to prove that a crop, furnace or still ran. Being at a station already
counts; no travel is falsely certified. No credits, materials, reputation, treatment,
thrust or machine progress are granted. Each completion gives **one native data
card holding one story file**, which is the document described by its closing letter.
Opening SABLE_FREIGHT.TXT on a computer or PDA makes the related follow-ups eligible;
it never has to be retained to close its own issuing chain.

Saved structures used: Framework's existing player story record (arc/step progress,
once-only news and file-read marks), ordinary objectives with stable PhobosStory
test names, the native card's DataStore, and Framework's existing story-file record.
No migration or save edits are introduced. On pack removal, existing Framework
handling closes missing story goals; cards remain, and their files read as corrupted
until the pack returns. Dismissed arcs stay dismissed. Future or unknown record
fields are handled by the existing Framework contract, not rewritten by the pack.

Outcome headlines require their completed arc and are once-only. They are **not**
queued in a step's opening delivery: that queue bypasses requirements in the current
runner, which would permit a conclusion before the correspondence finished.

## Offline verification and owner review

Checked before committing:

- All nine files pass the existing add-on validator and generated JSON Schema.
- The actual C# DataPacks add-on loader accepts the nine overlays without rejection;
  the merged StoryLibrary resolves all references across all 64 combinations of the
  six optional content mods. Items are checked against the maintained, native-derived
  item reference; no in-game lookup or new condition is needed by the authored tests.
- The original story settings and encyclopedia sections remain unchanged after merge.
- An older grain-sample arc record, an unknown file-read mark and a future record
  field survive an existing StoryRecord decode/encode fixture unchanged.
- Editorial and content checks cover global ids, mod gates, parent arcs, card-file
  references, preserved examples, no repeatable card grant, no economic reward and
  no outcome queued before completion. The four item tests name current definitions.
- English wording and the source files are recorded in the language ledger. The
  repository's language and documentation-link checks pass, as do all 115 tests in
  the Python suite. Maintained constants and generated Workshop notes also check
  cleanly; none is changed by the collection.

These are offline checks. No claim is made about Unity layout, TV distribution,
card opening, card delivery, notices or pacing observed in play. No game files,
load order, installation, saves or Workshop items were changed.

Once the owner enables the add-on through an approved installation path, useful
review commands from the [story guide](../writing-story-content.md) are:

1. `phobosframework story` to inspect the loaded entries and refusals.
2. `phobosframework story start spacertales-misfiled-can` to inspect the opening
   ledger chain without waiting for its random start.
3. Carry a D20 at a station and advance the requested game time; open the received
   SABLE_FREIGHT.TXT on a computer or PDA, then check the three eligible follow-ups.
4. `phobosframework story news spacertales-double-freight-claim` for a TV sample,
   and `phobosframework story chatter spacertales-ledger-names` for a talk sample.
5. Read Makers and brands and Life between stations in the encyclopedia, then try
   a Medical or Manufacturing letter chain with its matching equipment present.

Use a test session: starting an arc writes progress and completing it gives a card.
Build the package with `scripts/build-spacer-stories.ps1` and install it with the game
closed through `scripts/install-mods.ps1 -Mods SpacerStories`, which also installs or
checks Framework.

Publication and distribution remain owner decisions. The fictional names, drafts,
data files and source package do not establish a Workshop listing. Original Phobos
writing falls under the repository's Phobos-authored license; no copied game prose
or artwork is included.

## Workshop preview (6 October 2026)

**Owner request:** a Workshop preview in the Phobos series, with a visual style
suited to the vanilla game. **Agent choice:** two working spacers sharing a story
at a mess table, with a terminal, correspondence and an archive data card. The
subtitle is READ / REMEMBER / RETELL. The owner can revise this selection.

OpenAI's built-in Imagegen produced one complete square cover, using only the
original Phobos Shipbreaker, Agriculture and Medical covers as style references.
[Blue Bottle Games' Ostranauts](https://store.steampowered.com/app/1022980/Ostranauts/)
informs the utilitarian ship interior and restrained industrial pixel-art style;
no game screenshot, extracted texture or official logo was an input.

The untouched master is 1254 x 1254. The existing exporter makes 512px and 256px
nearest-neighbour previews, with the 512px copy in the native mod folder. The
[artwork record](../../assets/workshop/README.md#spacer-stories-a-complete-cover),
[exact prompt and provenance](../../assets/workshop/prompts.json) and
[export manifest](../../assets/workshop/exports.json) preserve the source and
reproduction details on the existing artwork branch. Model, seed and cost were
not disclosed; no provider switch or credit purchase was made.

The artwork-only update is classified as a **patch**, 0.1.2. Story data and saved
structures are unchanged. The selected image and reduced thumbnail were visually
reviewed. Offline export and package checks verify the files; they do not establish
how the cover appears in the game's MODS screen or constitute Workshop publication.
