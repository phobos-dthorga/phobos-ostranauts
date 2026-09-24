# Polaris docking — Auto Nav 0.8.0

Prepared on 2026-09-24 against Ostranauts **1.0.1.5**, BepInEx **5.4.23.5**
and Phobos Framework **0.11.0**. Built and checked offline; **not yet tested in
game or installed**. No new item, dependency, price change or raster artwork.

## Operating sequence

1. Use **Fly** for travel and a normal approach. Dock is a separate, deliberate
   maneuver; Fly does not start docking automatically.
2. Obtain **DOCK clearance through native Comms** and select the same ship or
   station. End any active/suspended Fly command with Stop before choosing Dock.
3. Within **10 km of the hull**, with room to brake, choose **Details → Dock with
   selected target**, or enter `phobosnav dock`. The display's ordinary Range
   remains centre-to-centre; the docking progress message reports **hull gap**.
4. Polaris resets time acceleration, uses RCS to approach at up to **20 m/s**,
   removes relative motion and turns toward the target. The ship's throttle
   limits the combined translation/rotation input. Working RCS and fuel are
   required even with a running reactor.
5. Keep that navigation console's **Comms/docking controls open** for final
   attachment. If closed, guidance continues and reports that the console must
   be reopened. Native attachment is requested once, after fresh checks.

The stopping-distance dial displays **CLAMPS** during docking; its ordinary
distance and torch settings do not control this maneuver. Normal Fly retains
torch preference, including approach braking where permitted. Tight terminal
docking uses RCS only. No automatic clearance request or fee avoidance.

`phobosnav status`, Stop and Resume use the same service as the panel. **Stop
removes commanded thrust, not velocity**. Replacing an active/suspended mission
requires a deliberate Stop. Changing the crosshair does not retarget it.

## Rules and limits

- Capture the clearance's exact target port. Prefer the ship's primary port
  when it fits; otherwise choose a compatible open ship port deterministically.
  Preserve both IDs; never silently substitute another port during flight/reload.
- Check clearance type, destination, assigned port and open-port lists each
  step. Recheck native hull fit every two simulated seconds and immediately
  before attaching. Stop on changed clearance, lost target, occupied ports,
  power/control loss, another controller, invalid data or excessive elapsed time.
- Use a conservative braking envelope and retain margin for sampled control
  and sideways correction. Refuse a starting intercept without braking room.
  An abort leaves the ship coasting; this is not emergency collision recovery.
- Maximum native simulation step **one second**; docking timeout **30 simulated
  minutes**, retained across saves. These are named docking policy constants.
  Existing throttle, Enabled and FuelCheck settings apply. Fuel budgeting includes
  current relative motion, acceleration/braking and a small correction reserve;
  it remains an estimate, not a guarantee against damage or changed conditions.
- Native clamp distance is **1.1 × collision distance**. Guidance aims for
  **1.05 ×** and refuses to guide inside **1.005 ×**. Clamp request additionally
  requires total relative speed ≤ **0.2 m/s**, heading error ≤ **0.004 radians**
  (about 0.23°) and spin ≤ **0.002 radians/s**. This tighter accuracy applies to
  final docking, not ordinary coasting flights.
- Spaceborne ships and stations only. No undocking, mooring, asteroid tethering,
  ground landing, arbitrary obstacle avoidance or route planning around hulls.
  Use a clear approach. Existing dock/mooring and a secured brace to the target
  block engagement. A manual maneuver takes control back from Polaris.

## Persistence and Framework

Reuse Framework's `Persistence.ObjectStateStore` and Auto Nav's console/module/
ship/player/target binding. The schema-1 record gains `ownPort` and `targetPort`
only for docking states, plus `Docking`, `DockingSuspended` and `Docked` lifecycle
names. Earlier plugins reject those unknown states and retain the record;
they cannot reinterpret docking as an ordinary approach on downgrade.

**Docking always suspends after load**, even when ordinary Fly is configured to
resume automatically. Explicit Resume rechecks hardware, current clearance,
the original ports, hull fit, motion, throttle, fuel and elapsed budget. It does
not request fresh clearance on the player's behalf. Stopped and docked missions
stay completed. Corrupt/future records remain protected until explicitly forgotten.
Saved physics still omits our actuator commands and retains actual motion.

Flight/docking policy belongs in Auto Nav. Framework already supplies the needed
versioned storage, localization and scrollable controls; this single consumer
does not justify introducing a generic docking framework or another dependency.

## Engine and community evidence

Local assembly inspected: `Assembly-CSharp.dll`, SHA-256
`91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
Private inspection files stay under ignored `.local/research/engine`; no game
assemblies, decompiled bodies or extracted assets are packaged.

- `GUIDockSys.CanDock` uses clearance, an available target port, collision
  distance ×1.1 and docking-ring alignment. `ConvertShipToRing` derives the
  ring's horizontal displacement from heading toward the target centre. This
  supports a centre-facing terminal controller rather than a second simulation
  of physical airlock approach geometry.
- `Comms.ApplyLoot` supplies a real `Clearance` with target, `DOCK` type and port
  ID. `Ship.GetAvailableDockingPorts(..., earlyOut: false)` checks open port pairs
  and structural grid fit. Checking just the first pair can miss the assigned one.
- `StarSystem.Update(double)` advances bodies, individual ships and docking groups
  in sequence. Our docking prefix samples both endpoints before those advances;
  its postfix checks completed motion and requests attachment outside the ship
  iteration. Normal Fly retains its existing per-ship hook, which skips docking.
- `CrewSim.DockShip` performs native port attachment, room positioning and dock
  group changes; `Ship.Dock` applies native fees. Polaris does not write position,
  velocity or heading to force a rendezvous. The final native join can snap hulls
  into the game's normal connected layout.
- The adapter retains the bound docking UI's salvage-law check and, at stations,
  its native `BreakAllLocks` cleanup. A missing cleanup method refuses attachment.
  Tutorial updates, docking event, encounter autosave and dock-info notification
  follow successful attachment **after** persisting completion. Cosmetic pre-dock
  ship screenshot refresh from the manual UI coroutine is not reproduced.

[Auto Dock by Gravy](https://steamcommunity.com/sharedfiles/filedetails/?id=3745448384)
provides a relevant precedent: real RCS travel, player-obtained clearance,
throttle limits and automatic final docking. Its Workshop description was reviewed;
no Auto Dock assembly was installed or copied for this round, and no licence grant
is inferred. The new docking code is original Phobos code using native APIs and
our existing service/persistence. Existing Auto Navigate adaptation attribution
continues to apply to the already adapted flight core.

## Verification and owner involvement

The canonical Auto Nav build includes a dedicated docking harness. It integrates
translation and the native two-stage spin update over several timesteps, headings
and thrust levels, checks braking/heading/lateral-motion limits and executes the
production docking adapter, service and persistence against native-boundary doubles.
Cases cover clearance changes, occupied/unfit ports, control loss, timeout,
closed console, once-only attachment, original port IDs and explicit resume.
These checks do not establish Unity integration or live docking compatibility.

After installation, the useful first checks are:

1. A cleared, slow station approach from approximately 1–2 km hull gap: Dock,
   displayed progress, physical RCS movement and a single successful attachment.
2. A cleared derelict approach; verify its chosen port and normal access/fees.
3. Stop during approach and save/reload during another: no unrequested thrust on
   reload; Resume keeps the same target and ports. Lost/reassigned clearance must
   block it rather than choosing another destination.
4. Reopen the controls after closing them near arrival; confirm the waiting message
   and final attachment. Report any native docking display or control ownership
   discrepancy, including the F3 status text.

The owner runs these checks. No mouse/keyboard automation or save editing is used.
