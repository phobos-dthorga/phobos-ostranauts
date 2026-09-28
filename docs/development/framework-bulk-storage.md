# Shared bulk custody and station transactions

Framework 0.27.0 adds concrete mechanisms used by Agriculture's
[R3 slice](../agriculture-bulk-storage.md). It does not register chemicals or
invent a universal fluid density. The earlier
[blueprint](agriculture-bulk-storage-design.md) is design history, not the API.

| Public mechanism | Boundary |
|---|---|
| `Liquids.StoredCommodity` | Validated commodity identity, kg capacity, service/catch/reserve quantities, monotonically updated revision and strict protected-state serialization. Content owns native mass, identity, chemistry and damage policy. |
| `Liquids.CommodityReservations` | All-or-nothing, exact-endpoint exclusive reservations owned by an operation; releasing another ID cannot cancel them. Synchronous first slice conservatively reserves the whole endpoint, rather than speculative fractional capacity. |
| `Trading.BulkSupplyOffer` / `IBulkSupplyProvider` | Additive offer enumeration, eligible destinations, read-only quantity/revision, checked acceptance and measured delivery. Content owns prices, units and physical mutation. |
| `Trading.BulkPurchaseQuote` / `BulkSettlement` | Captured actor, station, ship, destination, offer, quantity/price, revision and unique operation; validation, reservation, write-ahead evidence, debit, measured delivery, known-partial refund and paid receipt. |
| `Controls.IEquipmentPanelFields` | Optional structured labelled choices alongside the unchanged equipment-provider API; C1 renders checked draft Apply through content-owned validation. |

Existing `ILiquidReservoir`, mixture transfers, retained parcels, pairing,
power accounting and equipment/crew interfaces remain compatible. R3 adapted its
store to `ILiquidReservoir` and `LiquidTransferGuard` in 0.27.0; since Framework
0.39.0 that custody is the shared bulk vessel service below, with no pipe network.
A kg record must not be reused as a litres conversion for a different substance.

## Framework 0.39.0: registered bulk vessels

The R3 model became a shared service on 29 September 2026 so Shipbreaker's S3
silo and T2 thaw unit and Agriculture's R3 use one contract.

| Public mechanism | Boundary |
|---|---|
| `Liquids.BulkVesselSpec` | A content declaration: definition family prefix, one commodity id, capacity and dry mass in kg, owner id, and the names of the record, journal and guard. `CapacityFromVolume(m3, density)` needs the content's density; Framework never assumes one. |
| `Liquids.BulkVessels` | The registry: `Register` (the same owner re-registering a family replaces its declaration; another owner cannot claim it), `SpecFor(definition)`, `Of(object)`, `IsVessel`, `Aboard(ship, commodity)`. |
| `Liquids.BulkVessel` | Custody of one object: `Read`/`Save` keep native mass equal to dry mass plus contents plus physical cargo; `Protected` (unreadable record, mass mismatch, open transfer or conversion journal); owner-confirmed `Accept`; `BeginConversion`/`EndConversion` around an item becoming contents or contents becoming an item; `Snapshot`; `Endpoint` as an `ILiquidReservoir` whose capacity excludes the catch chamber. |
| Native patches | Contents follow a mode switch into a successor of the same family and are isolated in the catch when the successor is damaged; contents lost with a destroyed vessel are logged, never blocked (vanilla destructibility). Agriculture's own copies of these patches were retired. |
| `Trading.VesselSupplyProvider` | A station Bulk supplies provider over registered families with content-declared offers; delivery is the measured change of the vessel's record. |
| `Liquids.ShipsWaterSupply.DepositWaste` / `WasteCapacityKg` | Optional 0.16.1-pinned deposit into installed Ship's Water waste tanks through guarded transfers, up to the capacity their own public configuration entries declare (read by reflection, size tag by size tag), so their plumbing clamp can never delete deposited water. Litres equal kg for water only. The potable tanks are never written to. |

Records, journals and guards keep their existing names, so a saved R3 reads
unchanged after the re-point. A vessel commodity is an id in kilograms; nothing
about it is a native gas or a native fuel stat.

## Station adapter

An audited postfix adds one entry to native `GUIStationRefuel.SetupFields`'s
scrolling list. The new view retains the actual terminal actor and selected
docked ship. Buy revalidates actor proximity, terminal identity, ownership,
docking, universe state and content-specific destination eligibility. Unknown
native assemblies receive no entry. Returning leaves native fuel fields intact.

The adapter never calls or patches native `OnSubmit`; it does not settle other
ledger lines or infer delivery from an unrelated wallet delta. It constructs
one dedicated `LedgerLI` with the operation ID and an already-paid epoch after
known delivery/refund. Blue Bottle Games' local 1.0.1.5 `Ledger`/`LedgerLI` audit
confirmed that `AddLI` records a line and `PayLI` marks payment rather than
performing the wallet debit. Proprietary audit files remain local. Product
attribution: [Blue Bottle Games — Ostranauts](https://bluebottlegames.com/ostranauts).
The product page is not documentation of these locally inspected methods.

[Valtora's Ship's Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
retains its own potable row and submission patch. No third-party code/artwork
is copied into this adapter. Optional services and unknown future versions need
owner-run coexistence checks; packaged stock remains the fallback.

## Persistence and interrupted operations

The payer's `ObjectStateStore` journal records operation/identities, quote,
revision, initial balance and completed phases. A synchronous partial receipt
is refunded exactly. A completed operation cannot replay. Any exception or
unknown receipt leaves a protected journal, releases transient reservations,
and refuses another bulk purchase. Later evidence-based reconciliation is a
maintenance operation, not an automatic reset or a player retry button.

No atomic-save guarantee spans the payer, destination and native ledger. Do not
add automatic recovery based only on a changed wallet, current headroom or a
missing cargo item. Unknown owner/schema records remain untouched. Closing an
unsubmitted view never reserves or debits; settlement itself is synchronous.

Tests inject failures before/after debit, delivery, refund and ledger recording,
and check replay, future schemas, capacity contention and invalid receipts.
Storage tests cover reserve/catch conservation, repeated damage/recovery and
shrinking capacity. Native tests verify definitions, forms and method signatures;
they do not establish Unity callback order, gameplay access or visual quality.
