# Auto Nav saved flights (introduced 0.5.0; torch addition 0.6.0)

Auto Nav 0.9.0 adds [fresh sensor validation](auto-nav-sensors.md) before any
resume. Contact loss suspends without discarding the destination or elapsed
budget, and requires explicit Resume after recovery. No saved reading is trusted
as current; the existing schema and Framework envelope are unchanged.

Auto Nav 0.8.0 additionally saves [docking intent and exact port pairs](auto-nav-docking.md).
Docking always suspends after reload for explicit Resume, with current clearance
and port checks. Its new lifecycle names prevent an earlier plugin from treating
a docking record as an ordinary approach. The automatic restoration described
below continues to apply to ordinary Fly missions.

Prepared against Ostranauts 1.0.1.5 and BepInEx 5.4.23.5, with Phobos Framework
0.11.0. Builds and automated checks are not in-game validation.

## Player behaviour

Save normally during an Auto Nav flight. The console records the target's exact
ship registration, the console/module/ship/player identities, cruise and arrival
settings, captured coasting settings, elapsed simulation time and coasting latch.
Stopped and arrived states also persist. Each save carries its own copy; no
external sidecar or global last-destination file is used.

After loading completes, an active flight resumes by default if its original
hardware, player ship, target, power, fuel, throttle, timeout and competing-control
checks pass. Guidance is recomputed from the game's restored position and
velocity on the next unpaused simulation step. It does not restore an old thrust
command, select the current crosshair as a replacement, or restart the timeout.
The transient steering phase is recalculated; the cruise hysteresis latch persists.

Version 0.6.0 adds optional preferTorch to this same compatible payload.
Missing means false, preserving old RCS-only flights. A new flight captures the
current torch preference; invalid values are rejected. Torch burn permissions,
alignment and legality are recomputed after loading. Reactor save DTOs contain
idle flight controls and controlled-ship physics omits vAccIn; live state is
untouched. See [torch persistence and operation](auto-nav-torch.md).

Set `Persistence.ResumeAfterLoad = false` to restore active flights suspended.
A blocked flight also becomes suspended, with a reason in the panel/status.
Resolve the reason and choose **Resume** or use `phobosnav resume`. Failed checks
do not periodically retry and take control later. Multiple active records on one
ship suspend all of them; open the intended console to select one explicitly.

- **Resume** continues the saved destination and profile without selecting a target again.
- **Disengage** / `phobosnav stop` cancels the active or selected suspended flight.
- `phobosnav fly [arrival-km]` deliberately starts a fresh flight to the current crosshair.
- `phobosnav forget` stops and explicitly discards the open console's saved flight,
  including unreadable/future-version records. It does not reset native panel layout,
  throttle, inventories or BepInEx settings.
- `phobosnav settings` displays the active/suspended profile and reload preference.

Global BepInEx defaults remain global. Captured cruise/arrival/coast settings stay
with a flight across config changes. Live safety limits (including maximum flight
duration, simulation-step ceiling and arrival tolerance), rotation settings and
fuel-check preference still use current configuration. Shortening the flight
timeout does not give the restored flight a fresh budget.

Old saves without this record remain idle. A pre-0.5.0 flight cannot be recovered
retroactively. Removing the original module, moving the console to another ship,
missing targets and other ownership mismatches do not silently rebind a flight.
Docked neighbours are excluded from discovery. Hardware can be repaired and a
suspended flight retried; replacing hardware requires a fresh flight.

## Storage and engine evidence

Framework's `Persistence.ObjectStateStore` owns the namespaced, versioned envelope
in native `CondOwner.mapGUIPropMaps`. Auto Nav owns the schema-1 payload under
`PhobosState.AutoNav.Flight` on the controlling navigation console. Definitions,
item IDs, native nav layout and material-port formats are unchanged. The new
shared service does not migrate existing Framework links or change their pause
on reload policy.

The inspected 1.0.1.5 engine serializes each object's property maps into
`JsonItem.aGPMSettings` through `Ship`'s item serialization and restores them
during item loading. Framework already uses this native mechanism for port pairs.
The engine's `OnGameFinishedLoading` event precedes the coroutine's final
`FinishedLoading = true`; restoration waits for both. Load/NewGame entry clears
session references without modifying the departing world's snapshot or ship.

Native `ShipSitu.GetJSON` also saves `vAccIn`, `vAccRCS` and angular acceleration. For our
currently active ship only, a postfix zeros those actuator fields in the newly
created save DTO. It does not alter live physics, velocity, spin, gravity or other
ships. This prevents a saved burn from continuing if restoration is suspended or
the plugin is absent. The native physics step supplies fresh commands after a
successful restore. No save files are edited by our tools.

Framework detaches read/write dictionaries, preserves other native property maps,
and refuses automatic overwrite of malformed envelopes, different owners or
unsupported schemas. Auto Nav additionally rejects missing fields, non-finite
numbers, invalid ranges/states and self-targets. Unreadable records remain intact
until an explicit forget or a compatible implementation can read them.

These are original Phobos persistence additions around the adapted Auto Navigate
guidance; upstream attribution and the third-party notice are linked from
[the adaptation guide](auto-navigate-adaptation.md). No decompiled engine source
or game assets are redistributed.

## Verification and owner checks

Automated checks cover detached round trips for active/suspended/stopped/arrived
states, both coasting states, non-English numeric culture, missing/corrupt fields,
identity mismatches, schema protection, isolated saves and preserved timeout
budgets. Installer checks require Framework 0.11.0 for Auto Nav 0.5.0.
The production persistence service also runs against small native-world doubles:
early load events, world switches, explicit stop/resume, restored power, multiple
controllers, missing targets, fuel/timeouts and scoped saved-physics sanitization.
These doubles do not validate Unity loading or the game's actual hardware checks.

When ready to install, close the game and use the normal installer. In-game checks:

1. Save during a flight, reload, and confirm the same target/range/profile resumes
   after unpausing, without a burst of stale thrust or a reset timeout.
2. Repeat while coasting; reopen/close the panel and verify flight continuity.
3. Save after Disengage or arrival; reload and confirm it stays stopped.
4. With automatic resumption disabled, reload and use Resume without reselecting
   the target. A hardware/power/target failure should report a suspended reason.
5. Load an unrelated save or start a new game; no previous flight should appear.

Prepared packages are separate from the installed build. The owner is testing
0.4.3 while this update is developed; installation and these checks remain pending.
