# Auto Navigate comparison and current Auto Nav audit

Assessed **25 September 2026** against committed revision
`2a77e74df8762b36f005e0da53808ebc4db0e6c7`: **Phobos Auto Nav 0.9.0** and
**Phobos Framework 0.12.0**. The installed game reference is Ostranauts **1.0.1.5**;
BepInEx baseline **5.4.23.5**. This is an implementation audit with offline
checks, not an installation or gameplay compatibility result.

**Recommendation:** two upstream features merit MEDIUM priority, and two
separate findings in our existing flight controller merit HIGH priority before
more features. There is no missing HIGH-priority upstream feature that requires
a broad Framework expansion.

Here, HIGH means a concrete control or approach-safety problem relevant to
ordinary short-range use. MEDIUM means useful current gameplay or usability
without being a prerequisite for flight. These are usefulness priorities, not
implementation estimates.

## Findings and order

| Order | Index | Outstanding work | Relationship to Auto Navigate |
|---|---|---|---|
| 1 | HIGH | Apply the selected throttle to the complete RCS command, including turning | Hardening of inherited guidance; upstream is not a ready-made fix |
| 2 | HIGH | Check available braking room before ordinary Fly and Resume | Current short-range safety gap; not an upstream feature to copy |
| 3 | MEDIUM | In-game cruise/arrival-speed controls and console-specific defaults | Verified upstream feature absent from our current interface/default storage |
| 4 | MEDIUM | Rare natural module salvage, preferably navigation-console loot | Verified upstream acquisition path absent from our current content |

### 1. HIGH: consistent RCS throttle budget

The ordinary guidance path scales translational acceleration by the console
throttle but subsequently adds an independently calculated rotation command.
Its translational vector also uses a Euclidean bound rather than the sum of
absolute native axis commands. There is no final combined clamp before
`Ship.Maneuver`.

An isolated probe executed the production `AutoNavCore.SteerFlight` with a
stationary ship, a diagonal target 85 km away on each axis, RCS-only preference,
100 m/s cruise and a 0.25-second step. Only the test boundary was extended to
allow throttle selection and record commands:

| Selected throttle | X | Y | Rotation | Sum of absolute commands |
|---:|---:|---:|---:|---:|
| 0.10 | 0.070711 | 0.070711 | -0.500000 | 0.641421 |
| 0.25 | 0.176777 | 0.176777 | -0.500000 | 0.853553 |
| 1.00 | 0.707107 | 0.707107 | -0.500000 | 1.914214 |

Native `Ship.Maneuver` uses the sum of absolute translation and rotation inputs
in its RCS gas request. This demonstrates a command-budget defect, **not** a
measurement of game fuel usage, crew injury or a multiplier of linear G-force.
The pure rotation path used while aligning for torch also lacks a throttle cap.

Our final arrival brake and docking controller already account for aggregate
commands. Consolidate the concrete shared RCS budgeting calculation inside
Auto Nav, preserve braking priority, and use it for ordinary translation,
alignment and coasting spin correction. Verify combined axes at low throttle
and update braking assumptions wherever rotation reduces available translation.
There is no present need for a public Framework navigation API.

Evidence: [ordinary guidance](../src/PhobosAutoNav/Adapted/AutoNavCore.cs),
`SteerFlight` / `ComputeRotInput`;
[arrival brake](../src/PhobosAutoNav/Core/ArrivalBrake.cs), `TryCommand`;
[docking rules](../src/PhobosAutoNav/Core/DockingRules.cs), `TryGuide`.

### 2. HIGH: braking-room admission for Fly and Resume

`Engage` checks hardware, contact, settings, arrival geometry and estimated fuel.
`ApproachRules.TryPlan` validates distances and enlarges the requested stopping
distance for hull clearance; it takes no speed, throttle or acceleration input.
Ordinary `ResumeSaved` likewise lacks a braking-room check. Guidance does reduce
its desired speed and brakes inside the arrival band, but that cannot make an
already impossible intercept safe.

The audit probe supplied a target at 2 km, 100 m/s closing speed, 0.5 m/s² full
RCS acceleration, a requested 1 km stop and ample fuel. Current approach and
fuel checks both returned true. Even ideal straight-line stopping takes **10 km**,
before reaction, orientation or other margins. The docking rule rejected the
same motion/geometry inputs. The probe checked production approach/fuel rules;
the absence of a further admission guard was established by reading Engage and
Resume, not by running a live collision trial.

Add one Auto Nav approach-admission calculation shared by engagement, resume
and readiness display. Use sensor-qualified motion, actual throttle and
conservative RCS braking capability; do not count on torch permission remaining
available. Reject an unsafe new request with an actionable explanation. Define
in-flight deterioration separately: dropping an existing braking command simply
because the envelope became unsafe could make the situation worse.

This must not introduce an arbitrary minimum engagement distance or reject every
flight starting inside its arrival ring. Already-slow close approaches remain
valid. It is not obstacle avoidance or guaranteed emergency recovery.

Evidence: [Engage](../src/PhobosAutoNav/NavigationService.cs),
[ResumeSaved](../src/PhobosAutoNav/NavigationPersistence.cs),
[approach rules](../src/PhobosAutoNav/Core/ApproachRules.cs),
[docking admission](../src/PhobosAutoNav/NavigationDocking.cs).

### 3. MEDIUM: flight settings from the console

Upstream offers live controls for cruise speed, arrival speed and stopping
distance, with preferences saved per console. Our panel offers propulsion and
stopping-distance dials. Cruise and arrival speed come from shared configuration;
F3 can report them but has no setter for either speed. The stopping-distance dial
also changes shared configuration. A saved flight captures its own values, which
is different from retaining the console's defaults for later flights.

Add bounded cruise and arrival-speed controls in the existing Details area,
including a clearly labelled zero-speed arrival. Save defaults against that
console, seed missing preferences from current configuration, and capture a
separate immutable profile when Fly begins. Preserve active/suspended flight
settings and existing saved flight identities. Keep ordinary Fly's arrival speed
distinct from docking's much tighter fixed terminal limits.

Framework's existing `ObjectStateStore`, localization and `PanelWidgets` supply
the required foundations. Use a distinct namespaced preference record so changing
defaults cannot overwrite flight intent. UI callbacks should delegate to the
same Auto Nav service as F3. No new artwork or generic preferences framework is
needed for this slice.

Evidence: [current instruments](../src/PhobosAutoNav/NavigationInstruments.cs),
[panel](../src/PhobosAutoNav/AutoNavPanel.cs),
[configuration](../src/PhobosAutoNav/Plugin.cs),
[instrument guide](auto-nav-instruments.md). Upstream implementation inspected:
`AutoNavNavMod.LoadSettings`, `SaveSettings` and its three adjustment pairs.

### 4. MEDIUM: natural salvage acquisition

Upstream injects rare modules during navigation-console and other loot rolls.
Our registration supplies four merchant offers, construction and maintenance,
but no natural derelict/locker loot injection. Recovering an already existing
Polaris module is possible; that does not make it spawn on newly generated wrecks.

Prefer a small, optional navigation-console salvage rule, with content-owned
rarity and intact/damaged choices. General locker drops have less thematic value
and need not be copied. Normal merchants and construction keep this below HIGH
priority. Apply additions only to future native generation, preserving existing
inventories and other providers' entries; do not reroll explored wrecks.

Framework's `MarketStock` is a merchant registration/condition helper, not a
complete world-loot placement API. `NativeDefinitions` and its transaction can
already publish additive native definition changes. Inspect the precise native
loot-table route before implementation. Extract a small additive table helper
from the existing stock path if both consumers need it; do not copy upstream's
broad spawner postfix or build a general world-spawning subsystem by default.

Evidence: [equipment registration](../src/PhobosAutoNav/EquipmentContent.cs),
[economy guide](auto-nav-economy.md),
[Framework stock helper](../src/PhobosFramework/Trading/MarketStock.cs).
Upstream implementation inspected: `LootSpawner_DoLoot_Patch`.

## Already covered or not worth importing now

Current Auto Nav includes moving-target prediction, real RCS travel,
acceleration/coasting/braking, configurable arrival speed in its guidance,
fuel estimates, off-console guidance, manual takeover, realistic turning,
timeouts and return to normal time when a flight ends. Merchant acquisition is
also covered. These are not outstanding feature imports.

Polaris additionally supplies validated saved-flight restoration, sensor-qualified
contact, torch preference, deliberate docking, construction/repair and diagnostics.
The inspected upstream 1.2.0 assembly did not implement its advertised saved-flight
restoration; our existing persistence must not be replaced with that path. See
the [earlier binary review](auto-navigate-reuse-review.md).

Planet/moon navigation remains outside the immediate ship/station approach goal
and would require a separate body-target sensing/persistence policy. Do not rank
it MEDIUM/HIGH simply because its upstream code exists. Obstacle avoidance and
continuous work-position holding are separate projects, not missing capabilities
supplied by this Workshop item. Removing throttle/manual-takeover protections or
restoring permissive safety bypasses has no current benefit.

## Framework boundary and verification

This was a focused audit of the Framework services relevant to these gaps:
versioned object storage, shared controls/localization, additive stock, native
definition transactions and content lifecycle. The committed provider is adequate
for flight preferences and existing flight persistence. Its industrial console
binding is not a navigation ownership/arbitration service. Flight authority,
sensor interpretation and braking policy remain Auto Nav responsibilities.

Uncommitted `Observations` and associated Shipbreaker work were evolving during
the audit. They are not part of committed Framework 0.12.0 and are not prerequisites
for these findings. An initial working-tree test build stopped on an unresolved
`CondOwner.bLocked` reference in that pending work. No changes were made to it.
Verification therefore used an ignored, isolated export of the exact revision
above, preserving concurrent work.

All five unmodified baseline suites passed:

| Suite | Reported assertions/checks |
|---|---:|
| Framework | 1,573 |
| Auto Nav rules, persistence and instruments | 844,422 |
| Auto Nav torch and production guidance | 11,381 |
| Auto Nav docking | 437,143 |
| Auto Nav sensors | 75 |

The additional throttle/admission probes then ran in that isolated test copy.
Their source and output remain under ignored `.local/autonav-audit-20260925-091458/`.
Passing baseline checks do not cover the newly identified cases or prove native
Unity behavior. No game files, saves, installation or production source were changed.

Native assembly SHA-256:
`91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
Native research remains outside Git. Upstream binary provenance and attribution
remain as recorded in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

Public sources refreshed for this audit:
[Auto Navigate Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
and [1.2.0 change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3745533691).
The comparison distinguishes advertised behavior from the locally inspected
implementation; it does not infer incompatibility from Steam's generic page notices.
