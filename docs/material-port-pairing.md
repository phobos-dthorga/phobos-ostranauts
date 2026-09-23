# Saved material-port pairing

24 September 2026. Framework and Shipbreaker **0.5.0**. Requested by the owner:
explicit sender/receiver selection by unique ID, borrowing the native PDA/nav or
signal-linking approach. Current consumer: processor residue output → collector.

## Native precedent inspected

Read-only inspection of the locally installed Ostranauts **1.0.1.4** assembly:

- `Electrical` addresses connections by object ID and saves its input/output
  connection data in `CondOwner.mapGUIPropMaps` under `Electrical`.
- `Ship.GetJsonItem` writes each property map into `JsonItem.aGPMSettings`;
  `Ship.CreatePart` restores those saved maps. `CondOwner.ModeSwitch`
  copies the object's persistent ID and property maps to the replacement form.
- The native ship ID-remapping pass updates entire property values that match
  remapped object IDs. Our owner/peer IDs are separate whole values, allowing that
  established mechanism to work; no encoded cross-object registry is needed.
- The inspected `GUIPDA.ToggleNAV` path checks ship subscription and selects the
  ship's first nav station. That particular path is not an exact two-port pairing
  model. The electrical connection model is the closer precedent.

This records observed local code behaviour, not a claim that our integration has
been tested in-game. No decompiled source or game data is distributed. Our helper
is independently authored code using the native persistence container. It does
not patch signal routing or write into the native `Electrical` property map.

## Pair contract

- Identity is **full object ID + stable logical port ID**. A pair records a fresh
  token, owner, role, peer object and peer port on both endpoints. Short display
  IDs are never accepted as substitute routing identities.
- One sender per receiver and one receiver per sending port. Separate logical
  ports on future equipment may have independent pairs. No broadcasting,
  priorities, fan-in, fan-out or cycle scheduler in this slice.
- Both endpoints must reciprocate the same addresses, opposite roles and token
  before work is allowed. Matching only a human-entered channel label is insufficient.
- Linking an occupied endpoint is refused; relinking the same existing pair is
  idempotent. Unlink clears the peer only if it still reciprocates that exact pair.
  This lets the player clear a missing endpoint without breaking another route.
- Missing, invalid or newer-schema records are retained and block operation until
  explicitly unlinked. No automatic reassignment, repair or nearest-machine fallback.
- Native save/reload preserves configuration, not permission to resume work.
  Shipbreaker resets collection time and remains paused; cargo remains in its
  actual inventory. Resuming resolves full IDs and validates placement, ship,
  floors, locks, filter, capacity and power through the existing service.

The namespaced maps are `PhobosMaterialPort.<logical-port-ID>` with schema `1`.
Shipbreaker uses `PhobosShipbreaker.ResidueOut` and `PhobosShipbreaker.ResidueIn`.
Do not rename these keys after saves contain them. Pre-0.5.0 collectors start
unlinked. No migration modifies game save files outside the running game.

## Controls and scope

The collector's **Control Panel** offers Link/Unlink; the processor's F9 panel
opens **Residue destination / unlink** for the opposite perspective. Names and
short IDs are shown together, with full IDs in button tooltips and console status.
Linking is permitted beside either endpoint; starting collection is performed
beside the collector. Console operations use the same service and gameplay checks.

The grabber/chute/processor's direct mechanical intake still requires its physical
alignment. Pairing selects a material destination and does not replace that mount
or the structural-floor route. Electrical signals may enable/disable machinery
through the existing native conditions; they do not carry items. Persistent
space ejection, arbitrary filters and standalone conveyor endpoint equipment
remain later work.

## Verification

Framework consumer tests cover one-to-one exclusivity, direction, idempotency,
full-ID collisions, save round trips, malformed/future records, multiple logical
ports, orphan cleanup and protecting newer pairs. Native-data checks round-trip
through the installed game's actual `JsonItem`, `JsonGUIPropMap` and conversion
methods, plus whole-ID remapping and post-unlink saves. These run offline.

The owner should test saved selection and explicit reassignment alongside ordinary
collector use. Actual in-game save/load, damage/reinstall and collection remain
unverified until those sessions occur; established native persistence is sufficient
to implement the useful feature without a separate diagnostic prototype.
