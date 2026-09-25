# F6 attachment and instrument completion

**25 September 2026 — Shipbreaker 0.14.0 / Framework 0.17.0 candidate.**
This records inspected game behaviour and the implementation; in-game appearance,
placement interaction and complete-cycle evaluation remain owner checks.

## Vanilla evidence and the adaptation

The reference is **Blue Bottle Games' Ostranauts inertial-confinement reactor**,
not a scientific claim about a real fusion device. [Blue Bottle Games](https://bluebottlegames.com/)
owns the inspected game code, definitions and native UI artwork. No game assets
or decompiled source are distributed with Phobos.

Local primary evidence: the installed `Assembly-CSharp.dll`, SHA-256
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`, and
the game's `StreamingAssets/data/condowners/condowners_reactor.json` and
`StreamingAssets/data/items/items.json`, inspected on the date above:

- `ItmFusionReactorCore01Off` defines twelve named `Module01`–`Module12` points.
  `FusionIC` discovers modules at those transformed points on the same ship and
  classifies each by native component conditions. This inspected discovery path
  does not itself impose our furnace's equal-heading rule.
- `ItmFusionCorePump01` requires two floor cells and one
  `TILFusionReactorCoreFixtureAdds` socket. It adds a parts-fixture condition at
  its connecting cell and forbids an existing parts fixture there. Its named
  `ReactorPlug` is at local pixel coordinates `(0,-16)`. The cryo pump has its own
  offset plug and socket pattern. These are deliberate physical attachments,
  not connectivity inferred from coloured pixels or generic nearby objects.
- `CondOwner.GetPos(name)` rotates local point coordinates using the equipment
  transform; sixteen local pixels equal one world tile.

**Phobos adaptation:** the F6 now declares `CoolingLeft`, `CoolingRight` and
`CoolingRear` points. Their positions retain the existing side offsets
`(-3.5,+0.5)` / `(+3.5,+0.5)` and rear offset `(0,+6)` tiles. Older objects without
the new point names use exactly the same historical geometry. Equal orientation,
support, ship access and reciprocal saved pairing are still required.

We retain the existing native floor/wall placement requirements. We do **not**
copy the reactor's overlapping socket cells onto the F6, introduce an open floor,
auto-pair nearby equipment or move existing saved machinery. Wrong-position
equipment can exist, but its checked cooling connection is unavailable and its
panel explains why. This preserves both port-side choices and old radiator saves.

## Connection artwork and placement key

One original PixelLab coupling insert is composed into separate native-size
derivatives. Furnace side sockets and the rear socket are visible. The paired
side port shows its coupling toward the furnace; the same equipment heading works
on either side. Disconnected/invalid ports return to their original capped art.
The rear radiator has a matching connection mark across the retained hull wall;
the sealed wall crossing is an equipment abstraction, not a simulated pipe tile.

Native `Item.SetAlt` changes only the port's artwork. It retains the ordinary
item renderer, visibility, damage tint and lighting. A visual failure logs a
fallback and cannot modify heating, stored heat, mass or pairing. Original masters,
portraits, normal maps, collision bounds and saved identities remain unchanged.

The panel includes a live rotating installation key: F6 body, optional P side
sockets, R radiator position, rear hull and front access. Only the valid selected
connection is highlighted. It is an attachment diagram, not a temperature/probe
indicator or a construction ghost. World placement and UI scaling need owner review.

## Native controls

Framework extends the isolated native donor adapter with these paths relative to
`GUIShip/GUIReactor`:

| Donor | Implemented handling |
|---|---|
| `pnlPower/chkThrustSafety` | Guard buttons and canvas groups retained; reference ownership audited. Fresh cloned events remove serialized reactor actions before Awake adds the native guard/sprite behaviour. Switch delegates resume/stop to the checked service. Silent display refresh explicitly refreshes the swap sprite. |
| `pnlFuel/pnlHe3` | Native seven-digit artwork retained; native formatting disabled. Phobos rounds and positions decimal points independently of locale. Missing/nonfinite values and overflow blank the digits. The adjacent localized full reading supplies the sign, units, Unknown and overflow explanation. |
| `pnlPower/SliderFlow` | Isolated native vertical slider visuals and interaction retained; fresh events and bounded heat/ramp/cooling ranges. Dragging changes an editable draft; Apply invokes the service once. Display refresh does not issue commands or overwrite drafts/focused input. |

Existing knob callback suppression, finite-value LED checks, lamps and native
font reuse continue. Guard/slider/selectable targets must remain inside the clone;
navigation references and toggle groups are cleared. Native-resource changes
fall back with diagnostics to ordinary buttons, numeric fields and signed text.
The permanently accessible footer Stop remains independent of the guard.

## Scope and validation

Thermal balance, damage penalties, electricity, chamber gas, recipes, cooling
capacity, flight preemption and saved thermal schemas are unchanged. A new public
Framework widget adapter is the concrete reason for the newer Framework dependency.

Tests cover rotated socket identities, the named-point/legacy-coordinate match,
normal/colour dimensions and numeric rounding, signs, overflow, unknown values and
decimal-comma cultures. The existing conservation, partial-power, interruption,
support-loss and serialization checks still apply. These checks do not execute a
Unity scene: native widget lifecycle, focus, paused control use, both installation
types in all rotations and the first full batch remain owner gameplay evaluation.

Research underpinning the existing radiation model is attributed to **NASA's
Small Spacecraft Technology State of the Art, Thermal Control** in the
[first-cycle specification](furnace-first-cycle.md#thermal-and-physical-design),
with a [direct NASA source](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/).
The F6's chosen area, storage, temperature limits and transfer coefficient are
authored gameplay parameters; NASA has not validated or endorsed this mod.

PixelLab provenance, exact prompt, job, seed, dimensions and cost are recorded
separately in [coupling-provenance.json](../assets/phobos-furnace/coupling-provenance.json).
One included generation was used; no paid credits or game-derived image inputs.
