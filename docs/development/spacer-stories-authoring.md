# Phobos Spacer Stories: authoring record

Prepared 6 October 2026 at the owner's request. **220 original entries**, about
**13,700 words**, in nine story-schema files. The owner asked for the bulk of company,
product and world writing overnight, with content that would make sense alongside
vanilla Ostranauts. All new storytelling behaviour is data in the existing schema.

## Read the collection

The data-only add-on is [Phobos Spacer Stories](../../mods/PhobosSpacerStories), a
first-party Phobos mod that players subscribe to separately. Its
[manifest](../../mods/PhobosSpacerStories/phobos-addon.json) requires
Framework 0.110.0 for native data-card files. It adds no plugin, equipment definition,
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

## Goals, pacing and saved structures

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
