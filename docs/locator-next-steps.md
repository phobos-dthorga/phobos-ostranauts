# Locator experiment and decisions

**Status: historical proposal, set aside by the owner on 2026-09-20 because the
existing manifest meets the immediate need and is already being used.** Its
immediate availability outweighs developing our alternative. These steps are not
authorised implementation work.
The current exploration is [alternative PDA cartridges](pda-cartridge-ideas.md).

Proposal, 2026-09-20. Read the [research and existing-mod comparison](locator-research.md)
first. The owner requested research into a PDA app and installed sensors; no
implementation or final detection mechanism has been selected. Medical work remains
an independent project direction.

## The experience to justify

Install powered sensors to cover useful parts of a ship. Open Locator in the
existing PDA, search for eligible equipment and see which observations came from
working coverage. Cargo rearrangement, a closed door or lost power can change the
information available. The player chooses sensor positions for a reason.

This must earn its place alongside Common Sense Cargo Manifest. If a searchable
ship inventory is the whole desired outcome, evaluate that existing mod's current
compatibility instead. Adding constraints to an otherwise solved convenience
problem is only worthwhile when maintaining coverage is enjoyable in its own right.

## Decisions worth making before implementation

| Choice | Recommended first proof | Alternatives worth retaining |
| --- | --- | --- |
| What is detected? | One type of exposed equipment, using an optical interpretation. | Registered/tagged items or compatible telemetry equipment; each needs additional mechanics. |
| Where does it work? | One selected test ship; deployed sensor radius plus LOS. | Later owned-ship network, remote access or portable deployment. Define permissions and connections first. |
| What does the app reveal? | Item identity/type, sensor, supported location, observation time and coverage state. | Charge/condition only when there is an explicit means to measure or transmit it. |
| What survives lost coverage? | Session-only last observation, visibly stale. | Cross-reload history after a storage design is validated. |
| How does the PDA connect? | Explicit same-ship access during the proof, with the test limitation recorded. | Pairing, a network terminal or a cartridge could make access physical. Power wiring alone does not imply a data network. |

The first proof should not implement multiple sensor technologies, tag fabrication,
automated pickup, a hauling system, navigation, every item category or batteries
for the existing PDA. Native electrical load belongs on the installed sensor.
Portable sensor packs can follow if the installed version proves useful.

## Radius and placement

Use a sensor origin point and a radius in interior tiles. For a candidate item,
test squared distance against radius squared, then apply the chosen LOS and item
eligibility rules. Use a consistent point for large objects in the first test;
footprint intersection is a different rule. The coverage display must use the same
rules as detection, not show an unobstructed circle while walls block observations.

Coverage is the union of active sensors. Overlap must not duplicate items or grant
extra precision. Sensor density should provide practical redundancy and cover
occluded spaces, not arbitrary stacking bonuses.

Do not interpret radius R as permission to place sensors 2R apart everywhere.
Two unobstructed circles meet along their centre line at that spacing, but a
two-dimensional square layout leaves gaps. For ideal circles on a square lattice,
full coverage requires spacing at most sqrt(2) times R; walls make layout more
restrictive. Use a coverage preview and ship layouts to choose R, rather than
declaring an arbitrary placement rule now. No exact value has been selected.

## First technical experiment

In a separate test save, demonstrate one original placeholder sensor and one
`Phobos` PDA app entry. If a temporary debug button is needed to isolate panel
behaviour, remove it before claiming a functioning PDA integration.

1. Prove the home icon opens and closes a panel without breaking existing apps,
   input, pause controls or crew switching.
2. Place one sensor and eligible exposed items inside/outside its provisional
   radius. Require native installation and power. Record actual scan time.
3. Add a wall/door test to prove the selected optical interpretation. A mere
   distance-only debug query is not a completed detection model.
4. Return searchable observations and locate one valid result. Keep detection
   and cached observations in a service; UI filters/displays those results.
5. Remove power or move the target out of coverage. Invalidate the live status;
   any retained observation keeps its old position and timestamp.
6. Add a second sensor only to prove overlap, coverage extension and deduplication.

Use clearly temporary balance values in local configuration, not a promise about
final range or power. This proves a coherent device/app relationship without
building a general sensor framework.

## Observation states

- **Observed:** sampled at a stated time by an operational sensor. Even a recent
  observation is not a continuous guarantee between scans.
- **Last known:** the recorded observation is no longer current; its location does
  not follow the item's hidden current transform.
- **Not detected:** the eligible target is absent from the area actually scanned.
- **No coverage / offline:** no working sensor observation exists for the area.

Never turn lack of coverage into a claim that an item does not exist. If an item
is destroyed or transferred outside observable coverage, do not reveal that event
from global game knowledge. A subsequent scan can report that the old area no
longer contains it. The app must explain excluded classes such as closed storage.

## Acceptance checks

These are pending checks, not test results.

| Test | Required outcome |
| --- | --- |
| App lifecycle | Home, Close/Escape, another app, repeated opening, crew switching and loading a different save leave no orphan panel, duplicate icon, input capture or marker. |
| Radius | Test just inside, on and outside the chosen boundary, including diagonals and large items. Detection and the coverage display agree. |
| Occlusion | Test walls, open/closed doors, glass, corners and the sensor's own mount/collider. Closed storage and carried contents follow the explicit exclusion policy. |
| Power | Installed/on/supplied works; loose, off, broken and disconnected sensors do not refresh. Reconnection resumes without extra charging or duplicate scans. Record the native transition delay. |
| Multiple sensors | One result per object ID; losing one sensor preserves observations supported by another. Display their combined covered area correctly. |
| Location | A current result can be located without moving the object or issuing work. Markers/camera leave existing Vizor, selections and highlights usable. |
| Stale data | Move/remove a target beyond observation, then select its old result. The app uses the old recorded location; it does not expose present position or invent destruction knowledge. |
| Containers and stacks | Excluded nested objects do not leak into results. Test identical objects, stacked quantities and split/merged stacks before displaying aggregate counts. |
| Ship/access boundary | Dock beside another ship, switch crew/ships, undock and return. No cross-ship query occurs merely because ships are loaded or docked. Product ownership/pairing rules require their own proof. |
| Time | Normal speed, pause and acceleration respect game-time sampling. UI typing/refresh is not scanning; large time jumps do not generate fictitious historical samples. |
| Reload/unload | Physical sensors retain native state; transient observations clear and await a scan. No cross-save cache leakage or use of destroyed object references. |
| Cost | Record candidate count, sensor count, LOS checks, sample duration and allocations on a representative ship. Increase density once; optimise measured problems, not hypothetical scale. |
| Other UI mods | Once relevant mods are deliberately selected for testing, verify app dispatch and close behaviour with them. No current compatibility claim follows from separate successful builds. |

For each run record game and plugin versions, loaded configuration, starting
state, actions, elapsed game time and actual results. Keep saves and raw game
material ignored; retain concise observation summaries. Add meaningful unit tests
for coverage boundaries, deduplication and stale-snapshot handling when code exists.

## When to stop or redirect

First establish that deploying coverage is more interesting than simply using the
existing manifest. Stop if that distinction does not matter to the owner. Simplify
to reader/compartment-level detection if exact localisation is unjustified or
costly. Defer persistent history, radio propagation and remote networks until a
working local sensor establishes a need. Reassess medical or other machinery
ideas if this feature demands disproportionate UI or engine maintenance.
