# Auto Nav 0.22.0: docking repairs and Combat

Prepared 27 September 2026 for Blue Bottle Games' Ostranauts 1.0.1.5.
This records source/native inspection and offline tests, not Unity approval.

## Use Combat

1. Fit working **N2 and N3 in the same Polaris console**. Choose navigation cruise
   speed, separation and RCS/torch preference before starting. Separation is
   centre-to-centre; native hull clearance can raise it. Track shows both values.
2. On **Fire**, select a tracked fire target, select its weapon group, browse the
   desired ready weapon and choose **Use for aim**. Combat requires an explicit
   reference; it does not invent a weapon range or choose a different target.
3. Choose **Enter Combat** on **Track**. It validates guidance, hardware, sensing,
   fuel and ownership, records suspension of the previous mission, then transfers
   movement control. It matches range and motion and requests weapon-facing
   attitude. It can close or retreat, but does not circle automatically.
4. Choose **Engage** separately to authorize the selected volley budget. Entry
   grants no firing permission. Braking, traffic avoidance and permitted torch
   alignment/burns take priority over weapon facing and hold offensive dispatch.
5. **Cease Fire** (or an exhausted volley budget) ends firing and aiming while
   range matching continues. **Leave Combat** or **Disengage** ends Combat.
   **Return to Native** also ends Combat and releases the offensive hold; native
   automatic fire may resume. Use **Resume** explicitly for the previous mission.

Leave Combat before editing movement settings. Changing fire target, aim reference
or group ends combat movement and cancels the old permission. Changing volleys
cancels firing and aiming while retaining range matching. Contact, operator,
module/weapon or ownership loss stops Combat without automatic restart.
Changing the permitted drive preference also cancels firing and aiming.

Combat intent is session-only. Reload preserves the previous suspended mission
and FCS Hold; it restores no combat movement, target, aiming or firing permission.
No prior mission means there is nothing to Resume. Unknown saved formats remain
protected. Existing flight/fire schemas, assignments and gameplay values are
unchanged. Framework minimum remains 0.30.1; other mod versions are unchanged.

Outside Combat, Approach, Rendezvous and Follow can accept N3 attitude requests
while keeping their own navigation destination. Terminal docking, departure and
industrial movement retain exclusive movement authority. Current native shot
eligibility is always checked again. Automatic Artemis launch remains outside
this change; see [native missile limitations](auto-nav-fire-control.md).

## Findings and evidence

| Finding | Evidence and decision |
| --- | --- |
| High-cruise approach can stall | The production core with combined approach/docking services failed to reach staging at 1,500 m/s requested cruise. Its percentage tolerance was based on the requested cruise even when guidance demanded far less. The coast gate now uses attainable speed and requires positive closing progress. The same cases pass after the change. |
| Moving docking can divert during avoidance | With 20 km/s shared horizontal motion and -15 km/s vertical motion, a mirrored 0.5-second terminal case timed out near the hull. The cached avoidance waypoint was inertial while obstacle velocities were target-relative. Retaining the waypoint relative to the tracked target fixes this frame mismatch; the moving cases now converge. This is a plausible explanation for the reported diversion, not proof of the exact owner incident. |
| Failed fuel admission could leave RCS behind | Terminal entry restored controller state then dropped references on insufficient fuel without ending the acquired control. A regression seeds existing RCS and requires zero command on rejection. Entry now ends that control before resetting references. |
| Follow-only FCS gate | Production service and UI explicitly restricted coordinated FCS to Follow. The shared admission policy now permits Approach, Rendezvous, Follow and explicit Combat. Fire never directly adds a competing movement command during navigation. |
| Saved incident unavailable | Latest relevant save and its adjacent copies were inspected read-only. The ship was already docked; its latest relevant saved terminal operation was Stopped. No changes were made to saves or required providers. |
| Legacy prototype present | The actual installed Approach Assist 0.1.2 assembly was inspected, not assumed identical to archived source. Its pre-physics hook returns when its test pulse is inactive; its post-physics clear is guarded by its own thrust ownership. Test-save admission remains enforced. No evidence established it caused the reported incident; it remains installed. |

Primary native evidence is **Blue Bottle Games' installed game code**:
`Ship.Maneuver` transforms local translation into world axes;
`ShipSitu.TimeAdvance` applies native angular integration;
`StarSystem.Update` advances the epoch and ships before the post-physics boundary;
`GUIDockSys`/`CrewSim.DockShip` own legal checks and attachment. These were inspected
locally and are not redistributed. Product attribution:
[Blue Bottle Games' official Ostranauts description](https://store.steampowered.com/app/1022980/Ostranauts/).
This is observed implementation evidence, not an institutional or scientific
validation of the authored controller. Existing research attribution in the
[docking guide](auto-nav-docking.md) and upstream Gravy/mrkmg notices remain intact.

## Regression coverage and limits

The header shows a current docking admission blocker separately from the earlier
event retained in Info. Existing opt-in five-second guidance diagnostics now also
include the controller, console, phase and reason. Recording stays disabled by
default; no per-frame log or file writer was added.

The coupled suite links the production core, approach/handoff, terminal docking,
avoidance, persistence and shared pre/post update ordering. Native objects,
hardware/contact admission and actuator integration use explicitly labelled
boundary doubles. The numerical fixture preserves native translation and double
rotation updates; it is not a full Unity, orbital gravity or rendering simulation.

Its 128 cases combine direct Dock and Approach & Dock, inside/outside staging,
100/1,500 m/s requested cruise, mirrored/rotated approaches, common moving frames,
and 0.02/0.1/0.5/1-second timesteps. Existing docking tests cover clearance/fit,
interface unavailability, suspended handoff, reload, fuel, obstacles and protected
records. Sensor/service tests add Combat entry/exit, protected persistence,
permission separation, manual takeover and contact/operator/hardware/ownership
loss. Fire tests retain native shot/lock/volley restrictions. Shipbreaker capture
and reclamation regression checks remain required.

No clearance or port assignment, hull-fit test, braking admission, docking speed,
rotation limit, native fee/crime check or attachment rule was weakened. The UI
retains bounded refreshes and hidden-page inactivity. Track help scrolls inside
its allocated region; controls remain in the compact footprint. Browser previews
are layout evidence only. No new recorder mode, tooling installation, performance
campaign, artwork generation or measured FPS claim is part of this update.

## Optional owner playtest

- On the reported destination, obtain native docking clearance, open the native
  docking interface and try Approach & Dock from outside staging, then Dock from
  a closer position. Repeat from the opposite side if convenient. Note the exact
  current blocker and Info event if it stops; no new capture campaign is needed.
- With N2/N3, select a different fire target from the navigation destination.
  Confirm ordinary navigation keeps its destination while FCS can aim. Then enter
  Combat: check range matching and weapon facing, and that entering never fires.
- Check RCS/torch priority, Cease Fire, Leave Combat and explicit Resume. Save/reload
  during Combat and confirm no movement/aiming/firing restarts. Check long Track
  text scrolls while Disengage and Cease Fire stay accessible.

Live handling, the owner's exact docking incident, Unity interaction and subjective
performance remain unverified until owner feedback. Existing captures and installer
rollback packages are retained.

A reciprocal, securely braced tow is supported from Auto Nav 0.22.2. Use a separate fire target; only the piloted ship’s weapons are controlled. Brace faults cancel aiming and firing, and recovery never re-arms automatically. See [towing controls and limits](auto-nav-towing.md).
