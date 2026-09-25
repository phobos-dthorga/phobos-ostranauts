# Polaris N3 Fire Control System — Auto Nav 0.13.0

Prepared 25 September 2026 against Blue Bottle Games' Ostranauts 1.0.1.5.
This is an implemented, offline-checked candidate. Owner-run gameplay evaluation
is pending. Preparing it does not install, commit or publish it.

## Equipment and migration

**Phobos' Asterel N3 Polaris Fire Control System** is separate equipment in the
Auto Nav package. It supplies Fire and Systems in the existing tall flight hub,
including during manual flight or coasting without N1/N2. All installed boards
share one hub per console.

| Working module | Capabilities |
| --- | --- |
| N1 | Navigation and docking |
| N2 | Navigation, docking, Rendezvous and Follow |
| N3 | Weapon observations, limited offensive volleys and optional RCS aiming |
| N2 + N3 | Follow with coordinated aiming and firing |

**N2 no longer authorizes firing.** Existing N2 owners must acquire N3; a once-only
console message explains this change. N1/N2 item IDs, recipes and exact saved
flight bindings are preserved. A substitute or repaired/replaced N3 never inherits
an existing engagement's permission.

Native spawn command:

```text
spawn PhobosNavModFireControl
```

Intact/damaged overlays are `PhobosNavModFireControl` and
`PhobosNavModFireControlDmg`; underlying definitions are `PhobosFireControlBoard`
and `PhobosFireControlBoardDmg`. Construction is `PhobosBuildFireControl`.
The authored balance matches N2: 0.4 kg, $5,400 intact/$1,350 damaged base value,
two 0.5 kg electronics parts, 30 minutes and the existing 0.6 kg assembly offcuts.
Native repair/Restore and mass-balanced 0.4 kg board residue are retained. The
Polaris merchant has a 60% pristine offer chance. These are gameplay choices,
not real equipment performance or guaranteed merchant quotes. See the
[economy guide](auto-nav-economy.md).

## Using Fire

1. Select a qualified contact with the native crosshair, then press **Fire target**
   on **Fire**. The offensive target is captured separately from the navigation
   target; moving the crosshair does not retarget an engagement.
2. Select one native group and **1–9 volleys** (default 1). Browse the weapon card
   to inspect mode, combined arc/range status, loaded status, reload/aim delay and
   the primary blocking reason. Ready/selected counts are visible before Engage.
3. **Take FCS control** establishes offensive hold without aiming or shooting.
   Engage or Auto Aim also takes control when needed. Release the previous group
   with **Return to Native** before changing groups.
4. For optional aiming, browse to a weapon and use **Use for aim**, then enable
   **Auto Aim**. With no selected reference, the first eligible loaded weapon with
   a valid solution is selected in stable ID order. Its displayed index remains
   fixed; conflicting mounts do not make it alternate. Auto Aim grants no shot.
5. Lift the native cover and **Engage** to capture the volley budget. With Auto
   Aim off, steer and thrust manually; that does not cancel weapons-only firing.
   Kinetic fire can continue with the console closed or another page displayed.
6. **Cease Fire** immediately ends firing and aiming, retains Follow if active,
   and leaves **FCS Hold**. Budget completion does the same. Only **Return to
   Native** releases the hold; native offensive autofire may then resume.

A volley is **one successful native batch of the authorized weapons ready at that
instant**. It is not one projectile or ammunition unit per weapon. A reloading or
still-aiming weapon can miss a volley. Native salvo costs, consumption, jams,
reloads and projectile creation remain authoritative. No catch-up volleys are
issued; invalid/over-one-second simulation intervals revoke permission. Exceptions
terminate engagement without retrying an uncertain shot. Native faction
consequences are applied once per successful batch.

| State | Meaning |
| --- | --- |
| Native | FCS has released offensive automation |
| FCS Hold | Group owned by FCS; no firing permission |
| Armed | Captured volley permission; each shot still needs eligibility |
| Held | Armed, but guidance currently vetoes shots |
| Fault | Permission revoked; group stays held until explicit handoff |

Manual native fire and defensive PDC fire remain available. Native offensive
queues are filtered by controlled group, including after Cease Fire; unrelated
groups and native panels are not disabled wholesale. A single service arbitrates
flight and fire, with one active engagement. A second console cannot silently
take an already held group. Return to Native is available without a working N3
so failed hardware does not make ownership impossible to release.

## Native restrictions and limits

The checked adapter verifies power, damage, manual/defensive modes, ammunition,
jam/reload state, native range/arc and targeting time again before dispatch.
Unknown weapon types and decoys, including decoy missile ammunition, are excluded.
Unqualified or stale contact never becomes a firing solution, and native signal
strength is not converted into a hit probability.

Missiles require every applicable native automatic condition and a full native
lock from the open native navigation display. The inspected 1.0.1.5 missile
definitions lack the automatic range envelope expected by that path: N3 reports
the unavailable envelope rather than inventing a range or overriding manual mode.
Tests of a synthetic eligible missile establish the lock interlock, **not** that
stock missile launchers can automatically fire. Native manual missiles remain
available. Native projectile lead is retained; unreachable, nonfinite or singular
native quadratic solutions hold fire.

Auto Aim during coasting commands bounded **RCS rotation only**, never translation,
torch startup or velocity matching. During N2 Follow it requests attitude through
the existing guidance arbiter; braking, clearance, control limits and torch
transitions take priority. Pilot thrust/yaw or manual reactor control while Auto
Aim is active cancels both aiming and firing. Starting another navigation or
docking operation ends the engagement. Shooting never requests docking.

No firing solution guarantees a hit, intact boarding, functioning life support or
surviving crew. General obstacle avoidance is outside this system's scope.

## Persistence and commands

Preferences and the console's group ownership are saved in Framework's protected
object store. Reload restores ownership as **Hold**, including locked/damaged
consoles. Offensive targets, target locks, prediction/aim progress, remaining
volley permission and live RCS aiming are not restored. N3-owned aiming commands
are removed from the saved physics copy without changing velocity or spin.
Contact/power/hardware/player binding loss revokes permission; reacquisition,
Resume and rendering cannot rearm. Unknown future/corrupt records are not rewritten.

F3 actions use the same checked service, with the local Polaris console open:

```text
phobosnav firetarget
phobosnav weapons <1–9>
phobosnav volley
phobosnav fireweapon
phobosnav aimweapon
phobosnav autoaim
phobosnav engage
phobosnav ceasefire
phobosnav nativefire
```

`volley` cycles the budget; `fireweapon` browses the card; `aimweapon` selects that
card as the reference. Setting changes revoke permission. All routine controls
remain on Fire, with Cease Fire in the persistent action strip. Short tabs are
Nav, Track, Fire, Sys and Info; Details/Info contains explanations only.

## Evidence, provenance and validation

The implementation baseline is local inspection of **Blue Bottle Games**'
Ostranauts 1.0.1.5 native weapon code and definitions. Inspection files, assemblies
and game assets remain local. **Daniel Fedor of Blue Bottle Games**, in the
[8 July 2025 combat preview](https://store.steampowered.com/news/app/1022980/view/503954886346412815),
described separate missile, direct-fire and defensive roles, targeting time and
damaging consequences. This historical account supports keeping those roles
distinct; the inspected current implementation determines the adapter's checks.
It does not validate or endorse this mod. The volley/ownership model is our
engineering proposal implemented here, not a claim about real fire control.

N3 references native board artwork at runtime and reuses the approved tall
faceplate. No new raster, game-asset redistribution or separate package is needed.
Existing [artwork provenance](auto-nav-instruments.md#layout-artwork-and-verification) and upstream
guidance licence exclusions remain in force. Framework's minimum dependency
remains 0.17.0; no speculative public navigation API was added.

Offline verification covers capability/damage combinations, independent lifecycle,
group ownership, finite volleys, native eligibility, save/reload, control loss and
RCS limits. The compiled hub parser and installed native method/field contracts
are also checked. See the [validation record](auto-nav-hub-validation.md) for
counts, rendered sizes and owner-run scenarios. Doubles and browser renders are
not a Unity flight/combat session; live weapon consequences, panel dragging,
missile behavior and mixed-mount pursuit remain owner evaluation items.
