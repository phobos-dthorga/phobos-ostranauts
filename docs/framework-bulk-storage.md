# Shared bulk custody and station transactions

Framework 0.27.0 adds concrete mechanisms used by Agriculture's
[R3 slice](agriculture-bulk-storage.md). It does not register chemicals or
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
power accounting and equipment/crew interfaces remain compatible. R3 adapts its
store to `ILiquidReservoir` and `LiquidTransferGuard`; it does not need another
general reservoir-provider registry or an additional pipe network. Its kg model
must not be reused as a litres conversion for a different substance.

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
