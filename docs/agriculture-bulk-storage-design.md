# R3 first-slice implementation blueprint

27 September 2026. **Proposed, not implemented.** Read the
[findings and calculations](agriculture-bulk-storage-research.md) and
[artwork brief](agriculture-bulk-storage-art.md) first. This document chooses the
implementation defaults; changing them requires updating the study, not quietly
registering different equipment. It is not a claim of gameplay readiness.

## Selected equipment and supplies

| Property | Proposed first-slice value |
|---|---|
| Name | Phobos' Verdemorrow Groundwork R3 Agricultural Water Reservoir |
| Family ID | `PhobosVerdemorrowGroundworkR3`, with existing Installed/Loose/InstalledDmg/LooseDmg conventions; not currently registered |
| Footprint / art | 3 x 3 structural-floor fixture, 48 x 48 native sprite, no wall requirement; service side local -Y |
| Capacity | 120 kg clean agricultural water total across service chamber and isolated catch chamber; starts empty |
| Empty/full mass | 25 / 145 kg; contents contribute exactly once in every form |
| Base value | 450 cr empty; 90 cr damaged empty; contents retain their separate measured value |
| Hardware / power | Sealed positive-feed service reservoir; passive storage, no separate pump or idle electrical charge; W2 pays transfer work |
| Connection | One explicitly paired adjacent W2; neither a rack connection nor a new pump on the output circuit |
| Acquisition | Empty loose machine at the same three Agriculture merchants; finite lots of 4, chance 1; native APPS install, including damaged form |
| Construction | Install purchased/salvaged loose machinery; no invented fabrication bill for this first slice |
| Maintenance | 1,800 native repair-progress rating; 1 mechanical small part + 1 electrical small part + 1 aluminium scrap; ordinary wear restoration preserves contents |
| Dismantling | Empty/unpaired only, 600-second work; intact returns 4 kg steel plus 21 kg terminal housing remainder, damaged 1 kg steel plus 24 kg remainder |
| Damage | Immediately isolates and moves service water into the catch chamber once; no atmosphere discharge or invented gas |

These are authored gameplay choices. Reuse Framework's non-sortable housing
remainder contract at 0.01 cr per remainder, with new R3 intact/broken remainder
IDs and exact 21/24 kg masses; never classify housing as crop residue. Using the
inspected native steel value of 3.60 cr/kg, dismantling yields **14.41/3.61 cr**
against **450/90 cr** empty whole values, or **112.50/22.50 cr** at the native
lowest wear tier. Repair inputs have **20.60 cr** base value and the selected
progress rating corresponds to **21.6 unmodified minutes**, matching W2's bill.
See Blue Bottle Games' native values in the existing
[Agriculture economy evidence](agriculture-economy-evidence.md) and
[maintenance explanation](agriculture-treatment-economy.md). Actual merchant,
condition and crew/tool modifiers still apply. Retain actual replaced parts as
spent material through the shared maintenance service.

A retained catch is unusable until repair and an explicit local Recover action
(60 seconds crew work) returns it to service. Repair alone never refills,
recovers or resumes. This is a simple authored containment abstraction, not a
damage-frequency/leak-flow model. Guard the movement between chambers; total
capacity and mass include both. Broken or locked neighbours cannot consume it.
Block uninstall, dismantle and inventory conversion while either chamber contains
fluid or any journal is unresolved. Destruction must first preserve contents as
an exact-mass recorded agricultural drainage item through the existing guarded
delivery path; reserve its destination before a Phobos-controlled conversion.
If a native destruction hook cannot preserve custody, ship no destructive-fluid
extension until that path is resolved; do not claim arbitrary native deletion is
crash-atomic or conservation-safe.

Add **Phobos' Verdemorrow Groundwork Bulk Nutrient Charge**, 0.5 kg / 750 cr,
one inventory slot, using the current 1,500 cr/kg formulation value. Retail lot
8, chance 1, same Agriculture merchants. A new item ID
`PhobosVerdemorrowGroundworkBulkNutrients` is admitted to W2's selected-charge
adapter; old 40 g packets and B2 products retain IDs and records. Physical
remaining mass/value decline together. Empty charges vanish under the existing
zero-packaging contract. No Repair/Restore and no extra water is implied.

## Connections, operation and crew

Use one R3 port and a new W2 supply-side port, distinct from W2's current rack
outlet. In R3-local tile coordinates, W2 centre is `(2.5, 0.5)` or `(2.5, -0.5)`
on its +X edge; rotate the arrangement by quarter turns. W2's added inlet faces
local -X and its rotation must match R3. Keep both -Y service approaches walkable.
The full two-tile W2 edge must touch the R3 edge; diagonal, gap or corner pairing
is invalid. This direct coupling introduces no new pipe network or line hold-up.
Reuse shared adjacency geometry where suitable, keeping these offsets content-owned.

Configuration: selected R3, water reserve (default 0 kg, range 0..120), refill
target (default 19.5 kg within live W2 headroom), and optional authorised charge
replacement from an approved store. Apply uses fresh state and pauses active
operation; Resume is explicit. Existing Stop semantics survive all edits/reloads.
Local and C1 entry points delegate to the same actor-aware checked service.

R3 intake replaces W2's optional Ship's Water intake only while explicitly
selected; it is not a fallback source switch. A missing/empty/blocked R3 waits.
Clearing that selection while paused restores the pre-existing intake option;
the crew-water reserve still applies to Ship's Water. Do not register R3 as a
foreign potable vessel or use provider-owned water fields for its contents.

Distribution, blending and new R3 intake share W2's existing measured pump
budget, capped by its 0.05 kg/s and 0.001 kWh/kg rules. Existing output-first
order remains. R3 intake is permitted only when W2 receiving is enabled, the
pair and service paths remain valid and both journals are clear. A full 120 kg
transfer therefore consumes 0.12 kWh of W2 work; this is shared capacity, not
another 0.18 kW allowance. Account all received energy once as existing room heat.

Crew can haul the existing 5 kg irrigation charges to R3 and load each through
a ten-second checked action. Station fill avoids those onboard loads. Approved
charge replacement queues one physical 500 g/40 g/finished B2 charge from the
selected source, and explicitly rebinds W2 after completion. Default off; never
silently chooses a replacement. Agriculture permissions, roster, AutoTask,
needs, access and reservations remain authoritative. Machines still do the
pumping; skill reduces hands-on work only.

Skipped work reuses the same bounded W2 intake and shared resource budget. No
separate reservoir elapsed-time ticker. Native walking/carrying applies normally;
skip handling/travel consumes the existing crew budget. If an adapter cannot
account for new loading/charge replacement, its skip offer stays pending with
a reason. No station purchase or damage-recovery action runs during a skip.

## Minimum additive Framework contracts

These are proposed API responsibilities and names, not shipped signatures.
Keep `ILiquidReservoir`, `IMixtureReservoir`, `LiquidTransferGuard` and foreign
adapters source/binary-compatible. Do not replace Agriculture's current maps.

The audit supports extending these existing mechanisms rather than replacing them:

| Existing source | Verified responsibility and boundary |
|---|---|
| [FiniteLiquidTransfer](../src/PhobosFramework/Liquids/FiniteLiquidTransfer.cs) | Scalar kg capacity, same-ship/commodity admission, measured debit/receipt and known partial rollback; not a station payment service. |
| [MixtureTransfer](../src/PhobosFramework/Liquids/MixtureTransfer.cs) | Carrier/solute receipts and compatible profiles; preserves both components rather than treating every kilogram as water. |
| [LiquidTransferGuard](../src/PhobosFramework/Liquids/LiquidTransferGuard.cs) | Journals both endpoints before mutation; unresolved evidence protects them against retries. No crash-atomic guarantee. |
| [FluidLine](../src/PhobosFramework/Liquids/FluidLine.cs) / [NativeFluidRoute](../src/PhobosFramework/Liquids/NativeFluidRoute.cs) | Endpoint-owned parcels, route/profile locking while occupied and bounded transit; retain these on existing W2 branches. Direct R3 coupling needs no extra parcel. |
| [PortPairing](../src/PhobosFramework/Inventory/PortPairing.cs) | Reciprocal saved pairs; consumers still own geometry, authority and movement. A pair alone grants no access. |
| [LiquidDeliveryBudget](../src/PhobosFramework/Liquids/LiquidDeliveryBudget.cs) / [IrrigationService](../src/PhobosAgriculture/IrrigationService.cs) | Received-energy and rate-limited work; W2 shares one output/blending/intake budget. R3 must join that budget. |
| [ObjectStateStore](../src/PhobosFramework/Persistence/ObjectStateStore.cs) | Versioned native property maps; unknown schema/owner remains protected, no automatic resume or file rewriting. |
| [EquipmentProviders](../src/PhobosFramework/Controls/EquipmentProviders.cs), [ConfigurationSheet](../src/PhobosFramework/Controls/ConfigurationSheet.cs), [ObjectPicker](../src/PhobosFramework/Controls/ObjectPicker.cs) | Extend content-owned descriptions and checked Apply/actions through the current shell, draft and isolated ship selection paths. |

| Proposed contract | Required data and behaviour |
|---|---|
| `StoredCommoditySnapshot` | Immutable owner/object/ship identity, commodity or profile, service/catch quantities, capacity, revision and protected/manual-stop state; kg internally, presentation units owned by content |
| `IStoredCommodityProvider` | Enumerate owned endpoints and read snapshots; validate actor, compatibility, available quantity/headroom; adapt scalar/mixture transfers through existing guards; no translated-text decisions |
| `CommodityReservation` | Exact source/destination, amount and configuration revision; owned by one operation; release only its own reservation; no persistent virtual inventory |
| `BulkSupplyOffer` / `IBulkSupplyProvider` | Stable offer/provider/commodity IDs, station eligibility, unit price, maximum quantity, selected eligible destination; checked quote and settlement, not direct UI mutations |
| `BulkPurchaseQuote` / receipt | Actor, station, docked ship, destination, offer revision, mass, total price and operation ID; results distinguish delivered, rejected and protected/uncertain |

Physical quantity is mass in this slice. Future litre/unit displays require a
commodity-owned conversion and stated conditions; Framework must never assume
that all liquids have water's density. A future dissolved concentrate carries
explicit carrier water and solute mass. Dry nutrient stock remains dry stock.

Use `ObjectStateStore` for a separate versioned R3 contents record, paired-port
records and a namespaced station-purchase journal. Missing records initialize
only newly created empty equipment. Unknown/corrupt/newer data remains protected.
Store kg, profile/commodity, exact IDs and revision, never translated labels.
Maintain current two-component profiles for later prepared-feed/recorded-return
adapters; don't promise arbitrary multi-species chemistry.
Loading retains contents and selection but pauses the new intake for explicit
Resume, matching W2's existing receiving policy. Routine crew loading can only
revalidate under its selected standing-order policy; it cannot clear a manual stop
or protected transaction. Existing rack and W2 records are not migrated into R3.

R3 must have its own Agriculture storage service/state adapter, not masquerade
as a crop rack or reuse `CropState`'s fixed 20 kg capacity. Reuse scalar commodity
`water` for W2 transfer compatibility, while content-owned admission keeps this
nonpotable source out of foreign potable-vessel triggers and return plumbing.

## Station service: verified boundary and selected design

Native `GUIStationRefuel.Awake` instantiates rows under
`pnlScrollingList/Viewport/pnlListContent`, using
`Resources.Load("GUIShip/GUIStationRow")`. Thus scrolling space is not the main
limit. `UpdateFields`, `GetTotal` and `OnSubmit` name individual rows explicitly.
The latter adds ledger entries, pays them via the interaction actor's `StatUSD`,
then distributes specific resources. An extra visible row is not automatically
part of that transaction.

**Valtora's Ship's Water 0.16.1** patches `SetupFields` and `OnSubmit`, uses
`rowLifeBlack` for potable water, and checks the payer's balance change before
deposit. Preserve that row and submission lifecycle; don't copy this heuristic
as a generic purchase API. See its [author page](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189).

Add one **Bulk supplies** entry to the scrolling station list. It opens the
Framework shell with commodity list, quantity field, exact destination picker,
price summary and its own Buy button. Returning preserves unsubmitted native
settings. Capture the actual terminal interaction actor, station and selected
docked ship; revalidate ownership, docking, terminal access and live destination
on Buy. No access from C1 grants station service remotely.

Initial offers: nonpotable agricultural water at **10 cr/kg**, in **0.25 kg**
increments, into one R3; and the new **500 g nutrient charge at 750 cr**, in whole
charges, into a selected accessible W2 supply inventory with sufficient physical
space. This prevents delivery of unselected invisible powder. No mixing offers,
buyback or waste disposal. Water availability ceiling per quote is 120 kg;
nutrient ceiling is one charge per purchase. These are transaction caps, not
a claimed finite station stock simulation. Use native serviced docking eligibility
and no Endgame/empty-universe station supply; unknown eligibility disables offers.
Baseline prices equal present packaged goods per kg, with no bulk discount or
double connection fee. Future sellers/prices belong to content.

Fresh validation may reject a stale quote, but may not silently raise its price
or redirect its destination. Reserve exact headroom/slot and offered quantity,
journal before mutation, debit the captured payer, deliver and measure, then
finalize a dedicated paid ledger line through an audited adapter. Never run
native `OnSubmit` for this purchase, settle unrelated fees or use a broad
"balance decreased" test. For a synchronous known partial receipt, refund only
the undelivered portion and record actual quantity/cost; release remaining
reservation. On exception/unknown receipt retain before/after evidence and block
retry. On reload, complete/refund only when saved evidence unambiguously proves
the result; otherwise require reconciliation, with no automatic second debit or
delivery. Native save writes are not a database transaction; don't promise
crash atomicity across separate money/contents records.

Cancel before Buy releases only this quote's reservations and spends nothing.
Once settlement starts, closing the view cannot cancel or replay its financial
steps: complete known settlement or retain a protected journal. Reopening shows
the outcome or reconciliation blocker, not another enabled Buy for that operation.

Implementation gates: audit `LedgerLI` creation/payment identity and native
station eligibility including other mods before enabling Buy. If payment or UI
isolation cannot be proven, disable the optional station adapter with a clear
reason; R3 still accepts ordinary purchased 5 kg charges. Ship's Water remains
optional and its potable service separate. No native/foreign assets are packaged.

## Recovery and deferred machinery

R3 stores clean agricultural water only. Explicit local Drain creates the existing
recorded process-solution item with exact water mass and zero nutrient mass;
reuse W2 treatment rules and retain rejects. Confirm that this is a lossy drain
before work, and allow no crew drain without specific permission. A return tank
would instead require `IMixtureReservoir`, component-preserving receipt, separate
contamination/profile identity and a bounded physical batch withdrawal for W2.
Do not quietly make it another mode of the first R3.

Keep B2's one-packet jobs, finite makeup, exact outputs and saved receipts.
Prepared-feed storage would require a separately authorised compatible supply
route and meaningful independent pump authority to bridge W2 downtime; merely
placing a tank behind the busy W2 cannot do so. Both are later slices only if
owner usage establishes the need. Future Shipbreaker coolant/chemicals can
implement the same provider contracts without becoming Agriculture dependencies.

## Implementation order and acceptance matrix

1. Add only required snapshot/reservation/quote support with fake-provider tests;
   audit ledger/eligibility and fail-closed fallback before hooking any station UI.
2. Implement R3 water custody, direct coupling, physical mass and manual loading;
   retain old W2 routes/records. Register the 500 g charge and opt-in crew replacement.
3. Review one original R3 art pilot beside native tanks and W2; complete normal,
   loose and damaged variants only after silhouette/placement checks pass.
4. Integrate selected-source W2 intake, panels, crew and bounded skips; then enable
   the independent station purchase service with proven payment reconciliation.
5. Register new tuning values through the constants updater, translations, native
   INSTALL coverage, item/economy references, changelogs and Workshop drafts.
   Run affected native/Framework/Agriculture and cross-mod regression checks.
   Installation and owner gameplay review belong to a later implementation task.

| Scenario | Required result |
|---|---|
| Two W2s claim R3; source changes while quoted | Only one valid pair/reservation; stale requests fail without spending |
| Full W2/R3, wrong commodity, locked/moved/foreign endpoint | Specific blocker; retained cargo; no source substitution |
| Partial power or eight active branches | One shared W2 budget; no new pump energy or accelerated feed |
| Repeated damage/repair/reload, catch recovery | Same total mass; no repeated leak, refill, price duplication or implicit Resume |
| Native destruction, uninstall or dismantle with contents | Physical custody retained or conversion blocked; unresolved native path is a release blocker |
| Load/charge worker sleeps, cancels, loses route or manual takeover | Exact cargo remains; only that job's actions/reservations released |
| One/six-hour skips versus bounded ordinary updates | Same accounted resources; one crew budget; no subsequent double completion |
| Lost docking, insufficient funds, filled destination on Buy | Fresh rejection without charge; original selection retained |
| Partial delivery, exception before/after debit, save interruption | Measured refund or protected journal, never repeated payment/material |
| Native refuel and Ship's Water present/absent/disabled | Existing rows, totals, fees and delivery unaffected; unsupported optional hook falls back |
| UI refresh, stale Apply, stops, destroyed selections | Pending drafts preserved; stopped orders stay stopped; IDs stay diagnostic only |
| Art rotations, lighting, damage, 1080p/1440p/ultrawide and larger UI scale | Correct bounds, readable connections/live labels, no clipping or baked fluid identity |

Automated tests must include forced failures at each settlement step, not only
happy-path mocks. Source and offline image checks do not verify Unity hooks,
lighting, actual walking or compatibility with an uninspected provider version.
