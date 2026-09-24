# Residue composition and saved material contracts

Decision record: 2026-09-24. Implemented by **Framework / Shipbreaker 0.8.0**.
The complete operating budget, acquisition and controls are in the
[scrap reclaimer guide](scrap-reclaimer.md). These are authored gameplay material
budgets, not a native assay or certification of pure metals.

## Preserve the original material

`PhobosShipbreakerResidue` stays a separate **13 kg unclassified packet**.
Existing objects are never relabelled or granted a new composition. Started
revision-1 wall jobs finish with the exact original products, including that
packet. Unclassified residue can be stored or collected, but cannot enter the
new reclaimer. Its age, name and settings do not establish recoverable content.

Only fresh or explicitly cancelled/restarted wall jobs select revision 2.
The actual input remains in place during cancellation; no energy is refunded.
The distinct `PhobosPanelResidueR2` identity carries the new authored composition.

## Revision-2 budget

| Fraction | In the 13 kg packet | Reclaimer output | In rejects |
| --- | ---: | ---: | ---: |
| Steel-bearing fraction | 4 kg | 3 kg native steel scrap | 1 kg |
| Aluminium-bearing fraction | 1 kg | 1 kg native aluminium scrap | 0 kg |
| Unclassified matrix, coatings, filler and fines | 8 kg | 0 kg | 8 kg |
| **Total** | **13 kg** | **4 kg useful stock** | **9 kg** |

The full 24 kg wall budget is 10 kg steel class, 3 kg aluminium class, 2 kg
carbon-fibre scrap, 1 kg mechanical parts and 8 kg unclassified matrix. Initial
wall processing removes 6 kg steel, 2 kg aluminium, 2 kg carbon-fibre scrap and
1 kg mechanical parts, leaving the identified packet. Processing both stages
therefore produces **15 kg useful stock + 9 kg retained terminal rejects**.
Components/composites are not counted again as elemental steel or carbon.
No oxygen, water, nitrogen, electronics or other chemistry is inferred from matrix.

The reclaimer outputs `ItmScrapSteel` x3, `ItmScrapAluminum` x1 and
`PhobosPanelRejectR2` x1 (9 kg). Rejects have no repeat-processing recipe. Renaming,
reloading, changing settings or switching machines cannot reroll these yields.
Useful native scrap goes directly to existing repair/construction or trade; it
needs no mandatory extra shredding stage.

## Finite destinations

- Legacy residue: processor → collector or ordinary storage; no recovery recipe.
- Identified residue: manually haul from processor/collector to reclaimer feed.
- Useful metal: native repair/construction or trade where accepted.
- Terminal rejects: reclaimer output → paired collector or ordinary storage.

Collectors explicitly accept these three IDs with their actual expected masses,
retain four slots / 52 kg maximum and preserve saved pair identities. They do not
create cargo space, cross to docked ships or remove ship mass. Automatic reclaimer
feeding is a later concrete endpoint extension; manual hauling is implemented.

Recoverable external release remains separate future work. Native jettison can
destroy payload, so it is not substituted for physical cargo. A future carrier
must retain items, account for its own hardware and persist after separation and
save/load. See [release research](material-disposal-port-research.md).

## Monetary value and compatibility

The extra metal is $11.90 at audited native base prices; complete-chain useful
products are $45.94 versus a $21 input wall. Processing adds value and must not be
advertised as money-losing wall salvage. Capital, energy, cooling, labour and
acquisition still matter; no guaranteed net trading profit is claimed. Identified
residue and terminal rejects each use the technical minimum $0.01, avoiding the
native zero-price mass fallback. There is no promised waste buyer.

Keep this separate from the owner's rule that **dismantling machinery must return
less monetary value than selling it whole**. The native equipment audit includes
the reclaimer and its sections and checks that rule and construction/salvage loops.

Framework 0.8.0 owns shared immutable jobs and mass checks now that two actual
processors need them. Content owns the recipe identities and native save keys.
The shared batch planner and staged delivery retain inputs on blocked placement
and protect against duplicate completion. This is not crash-atomic game saving.
See [job compatibility](processing-job-compatibility.md). Unknown revisions halt
without resetting the input or saved fields. Gameplay checks remain outstanding.
