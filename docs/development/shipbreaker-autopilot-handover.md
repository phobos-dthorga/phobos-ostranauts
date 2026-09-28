# Autonomous reclamation implementation handover

**26 September update:** the owner selected temporary native capture/mooring,
release and repositioning first, with a finite deployed head as a possible later
enhancement. [The capture candidate](shipbreaker-capture.md) implements exact G4
binding, mandatory Auto Nav integration, checked native attachment and explicit
release. Cutting, automatic repositioning and repeated processing remain later
stages. The [geometry evidence](shipbreaker-close-work-geometry.md) explains why
native attachment is needed for this arrangement. The original research below
is historical proposal text; the capture guide defines current delivered scope.

25 September 2026. **Approved direction; implementation specification, not a
delivered feature.** Read the [research and native evidence](shipbreaker-autopilot-research.md)
first. This round changes documentation only. No new version, native definition,
loader dependency, save record, installation or publication is created.

## Ownership and compatibility

- Shipbreaker owns mission selection, cut order, material eligibility, acquisition,
  processing coordination and completion accounting.
- Auto Nav remains the sole owner of propulsion, flight admission, docking,
  working-pose control, repositioning and bounded retreat. N1 or N2 qualifies;
  N3 alone does not. There is no new navigation module or required artwork.
- Framework owns existing versioned stores, observations, physical commit and
  reusable finite storage/accounting support where a concrete consumer needs it.
  It must not depend on either content plugin.
- Dependency direction is `Shipbreaker -> Auto Nav -> Framework`, with
  Shipbreaker's existing direct Framework requirement retained. Auto Nav remains
  independently usable. Agriculture and Manufacturing integration stays optional.
- Preserve G4/native item IDs, all historic processing revisions, material port
  addresses, furnace hot saves, cooling pairs and ordinary flight restore policy.
  Industrial missions always suspend after loading.

## Service boundary and persistent intent

Add a small public `IndustrialNavigation` facade to the Auto Nav assembly. The
following is the proposed interface contract, not an existing callable API:

| Operation/type | Contract |
| --- | --- |
| `TryAcquire(IndustrialFlightBinding, out IndustrialFlightLease, out reason)` | Bind mission ID, player/ship, exact console/module, G4 and target. Validate N1/N2 and exclusive authority. Return a session-only opaque lease; reject an existing Fly/Dock/Follow/industrial controller rather than stealing it. |
| `RequestPose(lease, IndustrialPoseRequest)` | Request the selected G4's pose against an observed target surface: reference/observation epoch, approach normal, bounded stand-off and allowed work envelope. Auto Nav owns approach, hold, advance and bounded retreat. Optional attached mode additionally binds an exact permitted port pair; unsupported modes are rejected. No raw thrust, position setter or clearance fabrication is exposed. |
| `ReadSnapshot(lease)` | Immutable phase, binding/observation validity, tool-relative position/attitude/motion errors, clearance and reason code. `WorkReady` requires fresh measurements inside the admitted work envelope, not native attachment. Shipbreaker separately verifies part eligibility and process capacity before mutation; reads have no side effects. |
| `Release(lease, reason)` | Revoke work readiness and clear only owned commands. Idempotent; it does not undock, zero velocity or promise emergency braking. |

Requests and snapshots carry a session generation and observation epoch. Service
ticks expire readiness before the next native mutation; a cached UI snapshot can
never authorize a cut. Avoid a generic third-party autopilot registry for this
single new consumer. Extract reusable numerical primitives only where Auto Nav
actually shares them with docking/pursuit.

Store the authoritative mission in a namespaced schema-1 Framework
`ObjectStateStore` on the selected G4. Retain mission ID, full player/ship/target
IDs, exact nav console/module, working mode and optional docking pair, G4/chute/D4 binding, all
participating processor/storage/cooling IDs, enabled feed families, captured work
policy/revision, progress/commit journal and intent phase. Auto Nav's retained
flight intent references that mission; it does not own a second cut ledger.
Enforce one mission per player ship. Do not regenerate existing object IDs.

Never persist the lease, live thrust, sensor history or permission to heat/cut.
Future/corrupt records remain protected until explicitly forgotten; forgetting
intent does not delete cargo or clear a machine's physical hot-state record.
Changing the G4, layout, module, participant, ownership or target suspends and
requires explicit rebind/revalidation. No replacement or nearest-machine fallback.

Create a checked Shipbreaker mission-command context distinct from a seated UI
`ConsoleBinding`. It is issued only by deliberate Start/Resume with valid local
or C1 access, grants specific actions on exact participants, and is revalidated
by services on every action. Retain player aboard, ownership, hardware and native
interlocks; allow panels to close and the crew operator to leave the console.
Do not pass null to impersonate local access, fabricate a crew operator, or use
the local-room exception when automatically equalizing a furnace.

## Mission behaviour and player controls

Use the G4 local panel and C1 to select the target, navigation console, G4 and
processing chain. Show all G4s on the authorized host ship, with extended unique
short labels, location/facing, connected intake and reasons for ineligibility.
The selector persists a full object ID only after an explicit selection action.
The Auto Nav hub shows mission/target/G4 and flight status through the same service.
Start, Pause, Resume and Stop have F3 equivalents and the same access rules.
Do not add a new control-panel family or bake text into equipment artwork.

| State | Action and exit |
| --- | --- |
| Assess | Verify selected hardware, native target eligibility, explicit routes and real capacity. Obtain qualified surface observations and a collision-compatible G4 approach/retreat. Never infer cutting readiness or interior contents from hidden grid data. Docking clearance is needed only if optional docking is selected. |
| Approach | Auto Nav performs qualified approach to the work area, then tool-relative positioning. Cutting stays disabled until readiness. Downstream work obeys checked motion/power limits; no unauthorized rearming. Optional capture follows its own admission. |
| Hold and acquire | Track target drift/spin and maintain the selected G4 window. Check occupants, process hazards and the exact next part. Cut one eligible object with paid work, then commit one physical transfer into G4. Authorized processing continues where the process envelope allows it. |
| Capacity wait | Stop new cuts; retain cargo and completed work. Continue authorized downstream processing and bounded pose control. Fuel/power reserves can require an Auto Nav retreat or suspension; never hold indefinitely on exhausted reserves. Resume acquisition automatically only after fresh capacity and pose checks. |
| Reposition | Reassess geometry after each removal and request the next exposed reachable wall pose. Stop cutting during transit; maintain permitted downstream work. If no admitted pose remains, report remnants. Optional attached modes need checked release/reattachment, never an assumed ability to slide along a hull. |
| Suspended/faulted | Manual takeover, lost tracking, reload, changed bindings, occupants, invalid time step or uncertain mutation revoke the mission context and flight lease. Resume must recheck original intent and physical ownership. |
| Finished | Distinguish completed supported work with remnants from a fully accounted target. Stop acquisition authority; allow the explicitly authorized finite in-flight processing queue to drain, with clear remaining-product/partial-charge status. |

Pause disables acquisition and new batch starts, isolates active heat through its
existing service, and retains material/thermal state. Stop additionally ends the
mission's retained flight intent. Both release the owned flight lease and clear
owned thrust; neither zeros velocity, silently brakes or implicitly detaches ships.
Show resulting coasting/attachment state. A requested bounded retreat is a separate
Auto Nav operation requiring valid tracking and authority. Manual
controls and unrelated machinery permissions remain authoritative; the mission
does not commandeer another started/hot job or rearm work the player paused.
After a fault/reload, downstream receiving also follows its existing explicit
resume policy. Passive cooling continues independently of automation permission.

Use service reason codes for decisions and localized complete messages for
display. Show current part, selected G4, limiting machine, physical buffer
occupancy, retained rejects and categorized remaining objects. Progress is based
on supported observed work, never a fabricated percentage of the entire wreck.
No exact hidden interiors or unobserved material composition appear in the UI.

## Delivery sequence and gates

### 1. Identity, dependency and active working geometry

First resolve the concrete integration questions: deck/tool-to-navigation transform,
target loading/observations, collision-compatible finite G4 reach, and geometry
refresh after removal. Record inspected native methods and the limits actually
established. Test all mounting orientations, target drift/spin and shrinking hulls.
If present hardware cannot reach, specify the measured obstruction and finite
equipment alternative; compulsory docking is not the default fallback.

Implement the facade, mission store, G4 selector and an RCS working-pose controller
under Auto Nav's existing authority. Reuse established guidance primitives; G4
position/normal and surface-point motion replace centre-arrival readiness. Preserve
the original intact G4/chute/D4 geometry and proposed finite leading-edge reach.
Check both complete hulls, supports, unrelated neighbours, braking reserve and
bounded retreat. Keep the cutter disabled until these integration checks pass.

Review the furnace's Phobos-owned any-thrust interruption alongside this gate.
Specify which process stages can coexist with gentle positioning, their motion,
power and cooling limits, and the checked hold behaviour outside them. No blanket
interlock removal, guessed tolerances or automatic per-pulse restart.

This is an internal gate toward milestone 2, not a separate diagnostic-only
deliverable or an owner gameplay-test prerequisite. It passes when an admitted
surface pose can be approached, held and advanced under the selected G4, while
unreachable layouts and invalid observations receive actionable reasons. If the
engine gate fails, report that limitation before implementing unreachable cutting.

### 2. Useful ordinary-wall acquisition slice

Add a Shipbreaker cutting service and a narrow cross-ship `IPhysicalTransfer`
adapter. Use the inspected ordinary-wall native uninstall transition, exact-ID
re-resolution and a persisted no-blind-retry commit journal. Do not broaden
Framework's same-ship container adapter. Reuse D4/R4 recipes and routing unchanged.

Before enabling work, settle cutter power, duration and heat deposition in a
short content-owned design record, then name/test those values. Native 24 kg wall
identity/output and the existing G4 footprint are fixed constraints; no new laser
balance is invented by this research. Use an explicit empty-G4-space admission
and post-uninstall reach check. Do not auto-cut occupied, pressure-unsafe,
unsupported or attachment-supporting structure.

The useful result is an autonomous short section of exposed ordinary walls:
approach -> align/hold -> cut/feed -> advance -> repeat, with no required docking.
Integrate the checked motion/process policy for gentle holding; keep stronger
manoeuvres subject to process holds. Retain D4/R4 output, automatic capacity waits
and reliable reload/manual interruption. Exhausted reach stops with remnants;
final structure/support remains. Continuous translation through a cut is later
work unless separately justified by the demonstrated cutting envelope.
Owner gameplay evaluation follows preparation, not as a prerequisite for coding.

### 3. Processing endurance, new structure and broader traversal

Deliver finite storage endpoints and explicit ordinary-product routes before
calling the chain unattended. Keep one destination per address; register new
ports for D4 products/R4 steel without changing historic port meanings. Existing
R4-only aluminium-to-F6 feed remains the first furnace supply. Native stacks need
an explicitly accounted separation path before they become automatically usable.

Add opt-in repeated F6 operation through checked services and a mission-specific
permission. Bind recipe, room and cooling endpoint. Apply the checked process
envelope established in milestones 1-2 so suitable batches can run alongside
gentle positioning. Retain flight power priority, stronger-manoeuvre interruption,
finite cooling, safe gas equalization and output capacity. Never restart the
furnace on every guidance correction. A final partial charge stays physical,
cold and unprocessed.

Add ordinary floors next, only with an explicit balanced recipe and support/
pressure tests. Add other wall and equipment families individually, including
contents handling and value/mass audits. Never use native dismantle output tables
as a universal physical composition model.

Extend the first wall-section traversal to other observed faces with newly checked
approach corridors and bounded fuel/thermal reserves. Do not treat a local working
pose as general obstacle avoidance. Reassess exposure after every committed cut.
Optional docking/mooring requires its own eligibility, exact or generated endpoints,
load/unload, legal consequences, reach and recovery adapter. Existing Auto Nav does
not automate undocking. A fixed attachment cannot advance the G4 without checked
release/repositioning or separately designed finite handling equipment.

### 4. Whole-supported-wreck completion

Keep a categorized remaining-object ledger, refreshing after native changes and
target reload. Reserve attachment/support structure for last; inspect every
container, slot, stack, actor and disconnected component before retiring it.
Implement final detachment and empty-target cleanup only through a proven native
contract with retained recovery evidence. Never derive permission for `Destroy`
from an empty work list. Unsupported content, unreachable components, leftover
cargo or unverified lifecycle cleanup end as a remnant report, not whole completion.

## Mandatory dependency delivery

The owner requirement is **mandatory Auto Nav for Shipbreaker**, not an optional
feature adapter. Implement it alongside milestone 1, not by rewriting the
requirements of older packages in this research round.

| Surface | Required implementation change |
| --- | --- |
| Loader and native startup | Hard BepInEx dependency on `phobosgekko.ostranauts.autonav` at the first released facade version; runtime check for the compatible API and enabled native content. Keep Framework minimums and saved definitions protected. |
| Build/package | Reference Auto Nav without privately bundling duplicate Auto Nav/Framework assemblies. Build/validate dependency order Framework -> Auto Nav -> Shipbreaker; package the new guide links through the existing support helper. |
| Installer | Selecting the new Shipbreaker release resolves compatible Auto Nav and Framework packages, checks plugin/native enablement, and orders all three. Preserve historical-version requirements, unrelated mods, rollback and the game-closed guard. Exercise Shipbreaker-only selection, not just today's default pair. |
| Version catalogue | Use the constants updater for release versions; register the new authoritative minimum and recurring summaries where appropriate. Do not assume current Auto Nav 0.13.0 exposes the new API. |
| Player/publication records | Update metadata, getting-started/player/install guides, dependency contingencies, both changelogs and Workshop drafts in the same feature checkpoint. Generate dated release notes with the existing script; remain unpublished until actual publication. |

Do not remove required providers from existing saves. Preserve the existing
[Auto Nav provenance/publication hold](auto-navigate-reuse-review.md): mandatory
dependency does not resolve upstream licensing or authorize redistributing it.

## Acceptance matrix

| Area | Required scenarios and observable result |
| --- | --- |
| Identity | Multiple G4s, identical short prefixes, rename, damage/repair and replacement: selection remains exact; unavailable equipment suspends; replacement never inherits authorization. |
| Geometry | Four orientations, asymmetric hulls, deck/navigation transform, circle clearance versus real tool reach, neighbouring docked ship, obstruction, shrinking radius/cache and unreachable interior: only the actual window qualifies. Optional attachment also checks offset ports/supports. |
| Flight | Drift/spin/accelerating target, sensor loss, inadequate braking/fuel, another controller, N3-only console and manual takeover: cutting never outlives current flight/attachment permission. |
| Native acquisition | Successful uninstall, replaced native reference, native loose-item relocation, full destination, interleaved manual removal and missing/future record: one retained physical object or a protected fault, never duplicate output. |
| Occupancy/law | Human/droid aboard, uncertain occupancy, pressurised work area, station, foreign attached vessel, expired license and unknown jurisdiction: reject before mutation without hidden-state disclosure. |
| Processing | D4 tray full, slower R4, steel accumulation, reject storage full, F6 blank blocking collector, partial final charge, room change and lost cooling/probe: stop upstream and retain exact mass/heat. |
| Authority | Closed panel/operator leaves seat, selected crew changes, ownership/module changes, explicit machine pause, foreign hot job, gentle RCS versus excessive motion/torch and competing flight demand: mission permissions stay distinct from UI, cannot override local intervention and never rearm on each thrust pulse. |
| Time/persistence | Ordinary and accelerated steps, excessive/negative intervals, reload during each cut/transfer/furnace commit and save before/after attachment: no replay, elapsed catch-up credit or automatic industrial resume. |
| Completion | Exposed window consumed, unsupported machinery/contents, disconnected island, last floor/dock support and empty registration: report the actual remaining state; never delete surviving matter to finish. |
| Dependency | Missing/old/disabled Auto Nav, valid N1/N2, installer Shipbreaker-only selection, historical package and rollback: actionable preflight and no unintended provider changes. |

Use existing pure-rule and native-boundary harnesses, extending them for new
behaviour rather than retesting unchanged patterns. Validate ordinary wall
conservation, transfer ownership and furnace interruption with production paths.
Owner checks then cover an eligible free-flight wall section with advance,
concurrent permitted processing, an ineligible G4 layout, a capacity stop and
save/reload in an ordinary save. No agent-run save
editing or mouse/keyboard control. Build/offline evidence remains separate from
Unity attachment, material handling and gameplay validation.
