# Agriculture nutrient recovery and workup direction

25 September 2026. Owner-authorized direction: nutrient production, crop-residue
recovery and further economic balancing, including optional Ship's Water waste,
reuse of the Residue Collector and gradually consumed W2 mixtures.

**Implemented follow-up, 26 September 2026:** Agriculture 0.9.0 now provides
recorded crop residue, the B2 bench, equal-mass makeup formulation, selected W2
charge dosing and optional measured Recycler wet-reject capture. See the
[current operating guide](agriculture-nutrient-production.md) and generated
[economic evidence](agriculture-economy-evidence.md). Wastewater nutrient
conversion remains unavailable without provenance. Gameplay testing is pending.

The remainder of this document preserves the **25 September research and
proposal**, including options superseded by the delivered guide. Its research
attribution and provider observations still support the implemented boundaries.

## Recommended chain

1. Newly recorded crop residue becomes the first independent feedstock.
2. A sealed Groundwork workup bench extracts/treats a bounded batch into an
   incomplete recovered concentrate and retained spent biomass/process liquid.
3. Formulation combines characterized fractions and finite make-up ingredients
   into a usable Groundwork mixture. Raw wastewater and raw crop leachate are
   never directly interchangeable with the existing formulated nutrient pack.
4. A selected mixture in the W2 inventory dissolves only as finite water, tank
   headroom, electrical work and the selected feed profile permit.
5. An optional Ship's Water Recycler connection captures measured wet rejects.
   Nutrient recovery from those rejects requires separately established origin
   and composition; tank volume alone is not sufficient.

Potatoes, food lettuce and seed lettuce remain the concrete consumers. Avoid a
large chemistry catalogue, species-by-species deficiency simulation or extra
industrial hardware until one usable production route is complete.

## Scientific support, distinguished from gameplay

**Jay Garland (Bionetics Corporation), NASA Technical Memorandum 107557 (1992)**,
[Characterization of the Water Soluble Component of Inedible Residue from Candidate CELSS Crops](https://ntrs.nasa.gov/citations/19930008922),
studied leaching wheat, potato and soybean residues. Recovery differed by crop
and element; calcium and iron were exceptions to the generally soluble inorganic
fractions. This supports extraction followed by formulation, not assigning every
gram of wet residue a complete-fertilizer value. NASA's record lists Garland as
the author; NASA hosts/publishes the report. The abstract was accessible through
NTRS search; direct page/PDF retrieval was unavailable in this session.

**ESA MELiSSA**, [Compartment III: the nitrifying compartment](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Compartment_III_The_nitrifying_compartment),
uses a biological step to convert waste-derived ammonium toward nitrate.
This supports keeping waste conversion separate from a fresh-feed mixer.

**Stephanie Engeli, Eawag (28 June 2018)**,
[Fertiliser from urine set to flourish](https://www.eawag.ch/en/info/portal/news/news-detail/fertiliser-from-urine-set-to-flourish/),
describes VUNA's combination of nitrification, activated-carbon adsorption and
evaporation. Eawag's [VUNA project](https://www.eawag.ch/de/abteilung/eng/projekte/vuna-naehrstoffrueckgewinnung-aus-urin/)
also identifies salt concentration as a concern. Source-separated urine research
does not establish the chemistry of a game's combined shower/toilet wastewater.

Our simplified fractions, finite consumables, accelerated work, space, energy
and prices will be authored gameplay. These sources do not establish recipe
yields, prove this mod's microbial safety or endorse our equipment.

## Observed Ship's Water boundary

Original mod: **Valtora's [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)**.
The public description documents finite waste tanks, powered water reclamation,
filters and adjustable throughput/recovery. Installed plugin inspected:
**0.16.1**, SHA-256
`F236324A1C7732A2CEFB81DAAC9DE9167B31B3FA6FE646E32F88AC8702F4FC04`.
These are local code/data observations, not a running-game test. Decompiled
third-party material remains local and is not redistributed.

- Waste vessels use `TIsWasteVesselInstalled` and `StatLiqH2OWaste`;
  potable vessels use `TIsWaterVesselInstalled` and `StatLiqH2O`.
  Neither quantity carries a nutrient assay or separates greywater and blackwater.
- `Plumbing.TopUpFixture` can deposit waste while refilling a fixture buffer
  from potable supply. This does not prove an excretion/food-derived nutrient
  input. Refills and initial priming must not manufacture fertilizer.
- `Recycler.ProcessOne` is a private per-recycler settlement method, called
  from the provider's `Run`. It bounds processed quantity by time, filter life,
  available waste and potable headroom; it debits waste, deposits recovered water
  and reduces `StatWaterFilterLife`. There is no public reject-item event.
- The default configured throughput is 120 L/day and recovery 0.60; the latter
  can reach 1.00. Actual values must be read at settlement, not hard-coded.
  No rejects or nutrients may be awarded merely because a recycler is powered.
- The lost fraction is currently not a physical output. At the default ratio,
  10 L processed nominally returns 6 L and leaves 4 L unrecovered. Those 4 L
  are mostly reject liquid in our proposed capture abstraction, not 4 kg of
  dry fertilizer. A 100% recovery setting leaves no mass for a new reject output.
- Fresh filter data contains a distinct `StatWaterFilterLife` (6,000 L),
  separate from ordinary mechanical damage. This is a useful precedent for
  separating remaining consumable quantity from repairable machine condition.
  Do not alter Valtora's filters or maintenance definitions.

An adapter cannot reconstruct historical nutrient content from an old tank.
Existing tanks/derelict contents remain uncharacterized. Future source accounting
must distinguish genuine waste generation from fresh-water priming, provider
debug fills, refuelling and recycle loops. Until that is established, capture can
retain wet rejects but cannot credit them as nutrient concentrate. A proposed
average sewage assay is an explicit gameplay assumption, not a provider fact.

## Reuse the collector, preserve the material

Reuse the original Shipbreaker Residue Collector family, footprint, artwork,
native inventory and physical paired-transfer services. Do not create another
nearly identical collector or move its ownership into Agriculture.

The collector currently has four slots and a 52 kg maximum, with exact-ID/mass
filters for industrial residue and terminal rejects. It is **not yet a general
fluid collector or a Ship's Water attachment**. Its current source discovery
also only recognizes Phobos industrial equipment.

Extend shared registration/endpoint services in Framework for this concrete
external producer; keep biological eligibility in Agriculture and collector
hardware in Shipbreaker. A collector must have one explicit source/mode, bound
by full object IDs. New bio capture is opt-in, requires both providers, and must
not change existing industrial filters, pair slot meaning or route contracts.
Agriculture without Shipbreaker or Ship's Water must still support the crop route.

Reuse residue *artwork/container conventions*, but preserve these distinctions:

| Existing or proposed material | Nutrient eligibility |
| --- | --- |
| Legacy 13 kg industrial residue | None; never reassay |
| Identified revision-2 panel residue | Existing metal contract only |
| Terminal industrial/treatment rejects | None; no repeat yield |
| Historical unrecorded crop residue | No retrospectively assigned nutrient budget |
| Newly recorded crop residue | Only its explicitly retained, bounded nutrient allocation |
| Captured Recycler reject liquid | Only after origin/composition accounting; volume is not an assay |

Fresh biological payloads need distinct definitions/versioned records. Keep all
water, nutrient and remainder mass, even when generation is too small for a full
packet. Finite endpoint storage may accumulate fractions; it is not unlimited
virtual cargo. Packet size must respect both slot and mass limits.

For scale, 120 L/day at 60% recovery leaves 48 L/day unrecovered. At an explicitly
authored 1 kg/L wet-mixture conversion, a 52 kg collector could fill in about
26 hours; four 1 kg packets would instead exhaust its slots much sooner. Packing
and capacity therefore matter. These are capacity illustrations, not measured
sludge density or a promise of native liquid flight-mass accounting.

For opt-in capture, preflight destination space before provider settlement, cap
processed quantity to actual reject headroom and keep rejected throughput in
the original waste tank. Report a full collector; never silently discard captured
material. With capture disabled, retain the provider's original behavior.
Read fresh recovery settings and exact same-ship scope on every settlement.
Use a narrow inspected-version/signature gate, measured debit/deposit receipts
and an interruption journal. A postfix that simply spawns loot is insufficient.
Future/unknown providers disable this optional adapter without disabling farming.

## Workup bench

Suggested original name: **Phobos' Verdemorrow Groundwork B2 Workup Bench**.
The model, footprint, dry mass, price and construction bill are proposals, not
registered identifiers or finished equipment.

Use a compact bench with a sealed powered batch vessel for extraction/treatment,
and a clean preparation area for blending and packing. Crew setup and formulation
are suitable table work; active conversion must pay for time, received power,
heat rejection and any actual oxygen/reagents it uses. A bare table recipe must
not stand in for an unaccounted digester, sterilizer or nitrifier.

Start with one selectable batch and retained inputs/outputs. Two UI operations
are enough: **recover concentrate** and **formulate feed**. Batch saved jobs need
captured input IDs, profile revision, actual mass/quantity and a fixed output
budget. Pause/reload retains physical work and needs explicit resume. Full
outputs retain inputs; failed/unknown records protect the batch. Future hardware
must include native INSTALL, acquisition, repair, dismantling and the item guide.

Before choosing the first real recipe, record: water and nutrient-bearing input
fractions; treatment reagents/media and their finite costs; usable incomplete
concentrate; required make-up minerals; wet residue/terminal outputs; energy and
heat. Do not invent nitrogen/phosphorus from a generic carbon-bearing solid.
The existing crop model has aggregate nutrient stock, not full N/P/K chemistry:
keep a clearly named aggregate bookkeeping model until element-specific recipes
actually need a more detailed contract.

## W2 inventory and progressive mixture consumption

W2 already has a native 8 x 8 inventory, a 20 kg liquid budget and 0.5 kg nutrient
budget. Reuse it. Current Load nutrients consumes a whole 40 g pack into numeric
stock; the requested progressive physical-pack mode is new behavior.

Add an explicit **use inventory mixture** choice, off after reload until resumed.
Select a compatible item by full ID; do not silently switch profiles or use a
different item just because its name matches. Keep manual loading as a fallback,
and keep previous numeric stock usable without awarding a second pack.

- Track remaining usable material in a versioned item quantity record.
- Display remaining charge/percent, optionally through a native condition meter.
  Ordinary damage and consumable depletion remain separate.
- Decrement only material actually moved into a prepared solution or finite dry
  buffer. Pause, no water, full capacity, no received electricity, incompatible
  formulation and a missing source must consume nothing.
- Debit the item once and credit the W2 once through a guarded transfer. Update
  physical mass and value with remaining content; if packaging is material, retain
  an empty cartridge. Existing 40 g stock abstracts packaging, so do not invent
  additional empty-container mass for it.
- No Repair, Restore, recharge or reconstruction path may replenish mixtures.
  Block native generic maintenance eligibility too, not just visible buttons.
  Only the W2 machine itself is repairable.
- Consumed/sold/reloaded/removed packs cannot regenerate their quantity.
  Corrupt, unknown or mismatched records stay unusable rather than resetting full.

Use the existing nutrient formulations and finite two-component pipes. Do not
make biological mixtures soluble merely because they are held in the W2, and
do not consume packs on a wall-clock timer unrelated to actual application.

## Economic findings and proposed balance policy

The [expanded native economic evidence](agriculture-economy-evidence.md#cultivation-cost-sensitivity-and-propagation)
is generated from current source crop rules and native definition prices by
`scripts/audit-economy.ps1 -AgricultureOnly -OstranautsPath <game>`.
It now compares electricity sensitivities, repeating lettuce propagation, and
a clearly labelled crop-residue recovery ceiling.

At current base prices, a seed-sustaining rotation costs **29.678 cr per
food-lettuce cohort** in consumed Groundwork water/nutrients, versus **25.158 cr**
with purchased seed. The rotation needs **32 kWh per food cohort** rather than
19.2 kWh, before setup, pumps, cooking, cooling, labour and equipment.
Self-sufficiency already has a real resource and rack-time cost.

For an explicitly *proposed* 60% recovery of nutrients allocated to residue in
proportion to biomass, an ideal potato harvest would return at most **3.84 g
nutrient equivalent**, worth **5.76 cr** of avoided fresh stock before all workup
costs. Lettuce food residue yields only 0.5 g in that same illustrative model.
These are conservative accounting scenarios, not extraction recipes or assays.
Do not balance by multiplying wet residue kilograms by a fertilizer price.

Recommendations:

- Reward lower resupply burden and avoided purchases, rather than forcing a
  profitable seed/fertilizer export business.
- Amortize crew setup across batches; do not require separate work per tiny
  residue packet. Keep hands-on labour distinct from powered processing time.
- Compare recovery with fresh stock after make-up ingredients, consumables,
  measured electricity, cooling burden, capital and losses. Add a treatment route
  only if it offers a useful endurance choice at ordinary operating volumes.
- Leave commodity price changes until that first physical recipe has a complete
  budget. Do not inflate food resale values to hide expensive recovery.
- Model crew wage/opportunity cost and marginal power price as adjustable
  analysis scenarios, not as fictional guaranteed in-game tariffs.
- Keep crop-residue recovery, recorded drainage treatment and later human-waste
  recovery from claiming the same nutrient mass more than once.

## Implementation sequence and review checks

1. Add the fresh crop-residue record/partition contract and a finite crop-only
   extraction/formulation batch. Existing crops/waste retain their saved meaning.
2. Add the workup bench only with that useful job, native economy/catalogue,
   original layered art and standalone stock acquisition.
3. Add W2 progressive consumables, no-refill maintenance exclusions and shared
   measured quantity transfers. Test partial items across save, removal and trade.
4. Extend collector/provider registration and exact-ID routing for a source-bound
   optional Recycler attachment; establish a defensible new-waste nutrient ledger
   before enabling its fertilizer outputs.
5. Re-run full-cycle cost/space/energy comparisons and update authored balance
   from the completed chain. Regenerate item references and release records.

Meaningful checks include clear-at-zero-growth (no free nutrients), stressed and
delayed crops, split/combined batch invariance, bounded recovery over generations,
spent rejects, full destinations, two recyclers/one collector, changed recovery
settings including 100%, docked neighbours, provider absence/version mismatch,
interrupted settlement and consumable Repair/Restore attempts.

No paid artwork generation, game installation, foreign-provider edits or save
changes were performed for this research checkpoint. The roadmap is authorized
work, while recipe yields, the bench specification and the source-tracked
wastewater nutrient adapter still need implementation.
