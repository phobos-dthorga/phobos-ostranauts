# Alternative PDA cartridge ideas

Shortlist, 2026-09-20. The owner set aside the locator because the existing
manifest meets the immediate need and is already being used, and requested other cartridge ideas, including
connections to medical devices and portable power. These are design proposals,
not approved features or completed feasibility studies.

Selection should weigh the additional benefit of our work against an available
solution and the time before our version becomes useful. Novelty alone is not
the goal, and overlap alone does not rule out a project. The owner's report of
using the manifest does not establish its exact installed package/version or a
project compatibility test.

## What the cartridge contributes

Treat a cartridge as software for a specific instrument or workflow: measurement
procedures, analysis, records and supported alerts. It does not automatically give
the PDA a blood analyser, radio receiver or unrestricted access to every object.
The hardware produces observations; the cartridge helps the player use them.

Local data confirms `ItmPDACart00` has `IsPDACartridge`, `ItmWristPDA01` has
`data` and `pdaCards` slots, and `PocketPDACarts01` filters for cartridges.
`PDAVisualisers._Assemble` checks wrist equipment, finds cartridges and enables
features from their conditions. This establishes a native pattern for physical
software capabilities, not automatic support for an arbitrary new application.
New app logic still requires the extension work described in the
[PDA integration research](locator-research.md#app-integration).

For a chosen feature, explicitly define removal behaviour: removing the cartridge
stops its active functionality but should not silently destroy records. Record
ownership, transfer between PDAs and save/reload require a separate decision.
Do not build a cartridge framework or shared history system before a first feature
establishes those requirements.

## Candidates

| Cartridge proposal | Concrete player use | Equipment relationship | Principal uncertainty |
| --- | --- | --- | --- |
| Clinical course recorder | Compare observations before and after wound care or medication. See whether symptoms improved while injury or blood-loss indicators continued worsening. | Initially one examination/treatment record; later a handheld probe and bedside monitor can contribute measurements. | Treatment-event capture, supported observations, remaining medicine duration and persistent patient identity need validation. A temporal association must not be labelled a proven drug effect. |
| Battery test and endurance analyst | Test a selected pack/device combination, compare energy delivered during a controlled run and plan operating time with an explicit reserve. | Portable tester or adapter first; an installed test/charging bench later. Native damage-reduced capacity and stored energy are foundations. | A meaningful test must add more than a renamed charge/condition display. It consumes actual energy and game time; do not invent voltage sag, resistance or chemistry ageing absent a chosen simulation. |
| Compartment commissioning kit | After rebuilding a room, run a controlled observation period and record whether it held atmosphere under stated conditions. Compare before and after a repair. | A portable atmospheric instrument, with an installed sampling point later if useful. | Pressure change alone does not establish a leak. Temperature, pumps, breathing and open connections must be accounted for; inconclusive is a valid result. Gas APIs and room identity need a focused investigation. |
| Machinery event recorder | Attach to one system and record observed loss of power, stoppages and other supported state changes, making intermittent faults easier to investigate. | Portable logging adapter; later an installed recorder for a chosen machine. | Current-state APIs do not provide past events or prove their cause. Sampling can miss short transitions; event hooks and compatibility need investigation. Avoid another generic power overlay. |
| Environmental exposure journal | Record conditions encountered while working, identify repeated periods of heat, cold or low oxygen and compare work routes or procedures. | Carried environmental logger; a later medical instrument may link measured patient observations to the record. | Record actual samples with gaps. Do not invent exposure history, diagnoses, cumulative injury or radiation biology. Added value over existing alerts must be demonstrated. |

## Existing coverage checked

[First Aid for NPCs](https://steamcommunity.com/sharedfiles/filedetails/?id=3786464764)
advertises NPC self-care and player treatment with native medicines, dressings and
splints. Rebuilding basic treatment access would overlap it. A longitudinal record
has a different proposed purpose; no integration or compatibility is established.

The [official starting guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3347080066)
documents Power Viz and existing electrical/atmospheric information. Native code
also exposes Vizor presets plus Notes and Timer. A static overlay, generic notebook
or manual reminder alone would therefore be a weak cartridge proposal.

The [Common Sense collection](https://steamcommunity.com/sharedfiles/filedetails/?id=3789049955)
advertises pre-EVA equipment checks and retrieval, as well as its manifest. Avoid
proposing a basic suit-readiness checklist as new functionality. Battery testing
would need a measured endurance/bench-test purpose beyond checking a pack is present.

This is an initial overlap screen. No exact duplicate of the five proposed
workflows was verified in the inspected sources, but search absence is not proof
of novelty. Check the chosen candidate more deeply before implementation. No
additional game mod was installed or tested.

## Recommendation

**Clinical course recorder** is the strongest connection to the medical ambition.
Its first useful question is whether one observed patient is improving following
one intervention. It can support future equipment without first inventing new
physiology. It is worthwhile only if automatic observations and linkage to actual
treatment provide more value than the native log, Notes and Timer.

**Battery test and endurance analyst** is the best alternative for a concrete
hardware/software experiment. First check whether a controlled test reveals a
useful distinction between packs or device modes that existing readouts do not
already make obvious. Use a known load and measured energy; runtime projections
are conditional estimates, not guarantees about changing loads.

Compartment commissioning is another promising direction for someone who enjoys
building and diagnosing machinery, but has more untraced gas/room behaviour than
the medical and power candidates. Event and exposure recording are later options
if a particular recurring player problem makes their records useful.

Choose one workflow, establish the missing player decision and audit its exact
engine path. A large suite, shared services, cross-device integrations and a fixed
implementation schedule are not prerequisites.
