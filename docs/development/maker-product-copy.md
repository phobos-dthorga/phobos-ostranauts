# Maker voices in product descriptions

6 October 2026. Owner choice: product descriptions should be written as a
fictional manufacturer's salesperson or spokesperson making a sale inside the
vanilla world. The owner subsequently asked for the pass across all mods.
Agent choices: the voices and phrasing below, open to revision by the owner.

## Setting evidence and interpretation

Blue Bottle Games' [official Ostranauts description on Steam](https://store.steampowered.com/app/1022980/Ostranauts/)
places independent captains in a System cut off from a ravaged Earth, managing
salvage, debt, fuel, air and food. It describes functional ship systems, worn
parts, crew needs and in-world flight manuals. Those are the setting anchors
for this pass. Our inference is that manufacturers sell time between ports,
useful work from carried material and a tolerable life aboard, as well as hardware.
The new slogans and maker language are Phobos fiction, not additional vanilla
canon or Blue Bottle Games' text.

The established makers, models and product families come from the project's
[equipment-branding record](equipment-branding.md). Their voices also sit beside
the Phobos fiction in Spacer Stories; neither reference establishes that the
base game contains these companies. No new company history, canonical events,
institutional endorsement or legal warranty was invented for this pass. No new
literary imitation is introduced: the broader story influences remain credited
in the [Spacer Stories expansion record](spacer-stories-vanilla-expansion.md).

## Editorial choices

| Maker | Sales position | Voice |
| --- | --- | --- |
| Fennmark | Supplies already aboard can do another useful job; reserves buy choices between ports. | Practical process supplier, attentive to the captain's bills. |
| Oxsmith | Recover oxygen from the rock you encounter, with the plant and upkeep it takes. | Confident industrial oxygen maker, frank about the whole installation's power and safety. |
| Tolvane | Ammonia can supply nitrogen and hydrogen where the ship needs them. | Compact, purposeful chemical-plant pitch. |
| Lixivar | Recover useful salts and carry more of the refining trade aboard. | Working chemical supplier; containment and cleanup are part of the bargain. |
| Alembrine | A working galley can have something to offer after the watch. | Restrained warmth from a distillery maker; flammability remains explicit. |
| Slingwright | Supported process remainders can do one last job as reaction mass. | Dry, economical, honest about material consumption. |
| Rivetline | Put salvage, storage and handling to work without pretending the rejects vanished. | Yard-equipment supplier selling the next productive shift. |
| Ablatine | Put the cutting tool on the hull and keep collection as crew work. | Direct tooling supplier; no promise of effortless mining. |
| Asterel | Bring instruments and control within reach while the captain keeps the decisions. | Reassuring electronics supplier, never promising perfect tracking or guaranteed interception. |
| Verdemorrow | Make room aboard for food, useful crops and a life that continues. | Hopeful and practical; its existing line, Where we go, life grows, remains. |
| Halewright | Give a patient a berth and the watch useful information. | Quietly humane medical-equipment supplier; observation and treatment stay distinct. |

The standing [player-language policy](player-language.md) and AGENTS.md now
require this voice for future and existing product descriptions. Controls,
refusals, warnings and operating guides retain direct instructions. Raw stock,
internal feeds, rejects, legacy conversion notices and construction recipes
retain their appropriate factual roles. Existing meal and produce descriptions
already provide an in-world galley voice and remain as written.

## Delivery and data boundaries

This pass rewrites 71 English entries: Manufacturing 25, Framework 5,
Shipbreaker 14, Agriculture 19, Auto Nav 6 and Medical 2. Shared templates cover
all sizes and the ordinary installed, loose and damaged equipment forms; Auto
Nav's separate damaged-board descriptions receive their own copy. Framework's
five shared descriptions include both lines, the belt, water silos and drain can.
War Has Been Declared and Spacer Stories have no equipment product descriptions
in their English catalogues needing this treatment.

Authoring stays in English JSON catalogues and Auto Nav's existing native JSON
overlay fallbacks. No recipe, yield, price, machinery behaviour, story state,
saved identity, translation key or format argument is changed. Registered
version copies are updated only through update-constants.py. The six owning mods
receive patch versions because this adds wording, not player-facing capability.
Workshop pages remain mod descriptions and operating guidance, rather than being
presented as fictional manufacturers' brochures.

The CR-4 copy keeps the distinction between rock oxygen carried in carbon
monoxide and breathing oxygen after K2/X2 processing. Its half-power comparison
applies to CR-4 versus EC-4 alone; partner machines have their own power demands.
Store leaks, acid mist, ethanol ignition, heat, filled-line contents, process
water restrictions and the monitor's lack of healing remain explicit. Detailed
chemistry and recipe evidence remain in [Manufacturing's research](manufacturing-refinery-and-chemistry.md)
and the [regolith programme](regolith-programme.md). Agriculture's accelerated
growth, simplified recovery and nutrient models remain gameplay choices in its
[research](agriculture-research.md) and [operating guide](../agriculture-player-guide.md),
rather than manufacturer's claims about real cultivation.

The E-series hopper's actual shared display template is reviewed as well as the
older short description. Retired R-series reservoir ratings and conversion
notices remain factual legacy text; saved reservoirs convert to the shared
Rivetline silos. Furnace coolant loses the out-of-world fictional label in its
item description. It remains an authored fictional supply, with its existing
mass, thermal and servicing rules; the new copy makes no research claim about
real coolant chemistry or performance.

The English audit ledger retains the previous wording and review rationale for
each changed entry. Native item references are regenerated from the embedded
catalogues through the maintained export and renderer. No game files, installed
packages or saves are edited, and no Workshop publication is performed.

## Verification

The public-assembly checks passed 25,488 checks, including catalogue keys,
format signatures, native grammar and agreement between Auto Nav overlays and
their English fallbacks. Native integration passed 34,049 definition and
registration checks. Both ran again against the final copy. The maintained
item-reference workflow exported 447 item definitions across nine guides and
checked the existing economic reports. After the final hopper and coolant
wording changes, a fresh native export updated the snapshot.

The final English audit passed for 4,489 catalogue entries, 209 documents and
45 other surfaces. Maintained constants, generated Workshop notes and all
2,291 relative documentation links passed; the nine item guides match the
447-definition snapshot. These are offline checks; they establish neither
installation nor the descriptions' appearance in the game. The owner retains
that review.
