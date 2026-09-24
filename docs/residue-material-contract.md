# Residue composition, destinations and the next processing stage

Decision record, **2026-09-24**. Current prepared versions are Shipbreaker **0.6.1**
and Framework **0.6.0**, panel recipe **revision 1**. The future composition and reclaimer below
are a selected implementation design, **not registered items or playable machinery**.
No existing item or pending job acquires a new assay through this document.

## Existing residue stays unclassified

`PhobosShipbreakerResidue` is one separate **13 kg** packet. Its known property is
retained mass. Neither the native wall definition nor our saved packet establishes
how much steel, aluminium, polymer or contamination remains in it. The native
dismantling loot lists material families and random yields, not a manufacturer's
bill of materials or elemental assay.

Keep this definition, mass, collector eligibility and existing jobs unchanged.
Do not award metals by inspecting its age, displayed name or current settings.
Do not relabel it as the new characterised stream. New ordinary-wall jobs also
continue producing this legacy packet until a versioned producer is implemented.

Its current destination is **finite storage**: processor tray → paired collector
→ crew-hauled storage. The collector is a receiving port, not a trash deletion
action. Full storage stops transfer; full product space eventually stops processing.
Manual hauling to a legitimate accepting recipient uses normal game handling;
there is no promised station buyback or disposal service for this packet.

This is an honest limitation. Old packets can remain cargo or later use a separately
verified recoverable-release path. They cannot enter the new recovery recipe merely
because they have the same mass.

## Chosen composition for future production

Reserve **`PhobosPanelResidueR2`** for a future revision-2 producer. This is an
authored, conservative gameplay material budget; it is not measured native data.
The initial dismantling products remain 11 kg, with this new 13 kg remainder.
Keeping that split avoids changing the processor's footprint or feed capacity.

| Fraction | In the new 13 kg packet | Reclaimer output | Remains in rejects |
| --- | ---: | ---: | ---: |
| Steel-bearing pieces, represented as native generic steel scrap when sorted | 4 kg | 3 kg steel scrap | 1 kg |
| Aluminium-bearing pieces, represented as native generic aluminium scrap when sorted | 1 kg | 1 kg aluminium scrap | 0 kg |
| Unclassified bonded matrix, filler, coatings and mixed fines | 8 kg | 0 kg | 8 kg |
| **Total** | **13 kg** | **4 kg useful stock** | **9 kg rejects** |

These are mechanical material classes, not a chemical assay or certification of
pure metals. The 8 kg mixed fraction is deliberately not a source of assumed
oxygen, nitrogen, drinkable water, fresh carbon fibre or replacement electronics.
Assigning it constituent chemistry later requires another identity and recipe.

The corresponding **whole 24 kg wall budget** is 10 kg steel class, 3 kg aluminium
class, 2 kg recovered carbon-fibre scrap, 1 kg recovered mechanical parts and 8 kg
unclassified matrix. The original dismantling removes 6, 2, 2 and 1 kg respectively;
the remainder matches the table above. Parts and composite scrap retain their own
material classes rather than being counted again as elemental steel/carbon.

One new packet would produce exactly:

- `ItmScrapSteel` × 3 (3 kg).
- `ItmScrapAluminum` × 1 (1 kg).
- Reserved `PhobosPanelRejectR2` × 1 (9 kg: 1 kg unrecovered steel class plus 8 kg matrix).

The reject has no repeat-processing recipe. Refeeding rejects, renaming a packet,
reloading, changing settings or switching machines must never reroll a yield.
One panel through both stages gives **15 kg usable stock + 9 kg retained rejects**.
Sorting does not melt metal, make certified alloys or restore intact components.

## Destination decisions

| Material | Selected destination | Availability |
| --- | --- | --- |
| Existing unclassified 13 kg packet | Existing collector or ordinary storage | Implemented |
| Future characterised 13 kg packet | Reclaimer input; ordinary storage while waiting | Design only |
| Recovered native steel/aluminium | Existing native repair/construction inputs, or trade where accepted | Native consumers exist; this new production route does not |
| Future 9 kg terminal reject | Reclaimer output storage, then a compatible collection route | Design only; current collector rejects this new ID |
| Material deliberately released from the ship | One recoverable cargo carrier holding a finite batch | Deferred integration; never substituted by deletion |

Do not silently add new packets to the current four-by-13 kg collector contract.
When the reclaimer is implemented, add explicit accepted IDs and actual mass
checks, plus clear capacity text, without changing existing saved pairs. New routes
must identify a source port and destination port; no nearest-machine fallback.
Manual hauling is sufficient for the first reclaimer. Automatic collector-to-
reclaimer delivery is a separate concrete extension of Framework's port service.

Persistent release remains a separate feature because native jettison can destroy
the payload. A carrier must preserve original items, account for carrier hardware,
own its payload after separation and persist across departure and save/load.
See [release research](material-disposal-port-research.md). A counter labelled
"exported mass" is not a physical destination.

## Next machinery: one combined scrap reclaimer

Combine captive shredding and mechanical separation in **one appliance**. A separate
mandatory shredder currently offers no alternate output or meaningful choice.
Native useful scrap can already go directly into repairs; do not make the player
process it again. This choice follows the bounded process study in
[machinery research](shipbreaking-material-processing-research.md).

Selected design envelope for implementation:

- **4 x 4 tiles**, with one accessible service edge; no smaller temporary model.
- One active characterised packet, up to four waiting packets (**52 kg feed**),
  separate **8 x 8 product inventory**. Capacity is inventory geometry plus mass,
  not an eight-tile-wide physical extension.
- Only the exact new residue definition, empty and unstacked, with the expected
  composition/revision and actual mass. Legacy residue, ordinary trash, live
  machinery, batteries, pressure vessels and unknown mod cargo are not accepted.
- A closed chamber, captive feed and retained fines; static artwork needs a clear
  feed mouth, service cover and product/reject access, not another exposed hopper.
- Start/pause/cancel, recipe and full-output explanation through an ordinary
  right-click Control Panel and F3, delegating to a gameplay service.
- The current processor's original Control Panel work still waits for the owner's
  active gameplay evaluation; this design does not replace that test candidate.

Final machine mass, construction/service bills, price, duration, power and heat
budget must be settled together before creating a usable saved definition. They
are not derived from the fractional yield alone. A reasonable next implementation
step is to choose them against the existing 4 x 4 fixture and native maintenance
comparisons, with a bounded heat destination instead of assuming space removes it.
No additional standalone machine or artwork is necessary to settle that model.

## Monetary value is a separate budget

At the audited vanilla base prices, the four useful reclaimer outputs total
**$11.90** (3 × $3.60 steel + 1 × $1.10 aluminium). Together with the first stage,
future useful products would total **$45.94**, plus the deliberately minimal
terminal-reject value. Use an explicit **$0.01 per packet** for both reserved
residue/reject definitions when they are registered, following the current residue
convention: zero invokes the native mass-as-price fallback. This is a technical
minimum value, not a guaranteed merchant buying price. The input wall's base
value is **$21**. These are neither
merchant quotes nor net profit after acquisition, energy, service and labour.

This proposal adds value by processing; it must not be described as a money-losing
wall salvage recipe. Decide its operating economics before enabling the new
producer/reclaimer together. Keep the owner's equipment rule separate:
**dismantling a machine must return less value than selling that machine whole**.
The existing machine audit already meets that rule; do not nerf its yields again.
Retaining material does not require giving waste a lucrative selling price.

## Save-compatible implementation sequence

1. **Implemented in 0.6.1:** keep revision 1 readable with its exact original products and saved duration.
   Store/select output definitions by the job's revision, not a mutable global
   product list. Unknown revisions stop with an actionable message; do not reset them.
2. Register the new residue and terminal reject alongside the legacy definition.
   They need distinct player-facing names even if initial packet artwork is reused.
3. Add the reclaimer's useful consumer and finite output path before enabling new
   wall jobs to generate its feed. Do not ship a new unusable inventory material.
4. Enable revision 2 for **new** eligible jobs only. A started revision-1 panel
   finishes revision 1. Explicit cancellation refunds no work but keeps the actual
   panel; starting it again selects the then-current recipe and starts from zero.
5. Reuse Framework's actual-object transfer, pairing, reservations and staged
   delivery. Extract only the batch/job rules shared by these two real machines;
   no generic chemistry engine or global conveyor scheduler is required.
6. Before delivery, validate gross and class-level mass, reject loops, revision-1
   continuation, interruption, full output and duplicate completion. Follow with
   owner gameplay use when the implementation is ready, not another basic-power test.

Step 1 now has [version-aware job handling](processing-job-compatibility.md),
including recipe-specific output planning and delivery. Only revision 1 is
registered in production; steps 2–6 remain future work. No new material,
reclaimer, dependency or save edit accompanies that compatibility update.
