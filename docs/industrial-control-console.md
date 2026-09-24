# Industrial control console and equipment panels

24 September 2026. Research baseline: Framework/Shipbreaker 0.9.0,
Auto Nav 0.3.0, Ostranauts 1.0.1.5. The owner approved the
[text mockups](industrial-control-mockups.md), then implementation of overflow,
ship isolation and automatic grouping, with original artwork.

**0.10.0 implementation:** the [player guide](industrial-console-player-guide.md)
describes the prepared console, local panels and limits. Native definition,
economy, mass, access-policy and package checks run offline. Native UI/seating
and final gameplay remain owner checks. The design rationale below is preserved;
the guide identifies what actually shipped. PDA/visor connections remain ideas
only, documented at the end of that guide.

## Owner direction and recommendation

The owner requested proper equipment control panels and original graphics,
plus a central console so crew do not have to visit every appliance to operate
it. Research and text mockups come before finished artwork. This supersedes the
earlier instruction to wait until gameplay evaluation concludes before planning
the fixture's Control Panel. It does not make any outstanding gameplay checks pass.

Build a **Phobos Industrial Console**, a dedicated seated 3 x 3 workstation,
with a shared equipment screen also reachable through each machine's right-click
**Control Panel** action. Use the navigation station as an ergonomic and visual
reference. Give the industrial console its own definition and interaction;
inheriting navigation duties, flight hardware or pilot behaviour is unnecessary.

The implemented footprint is 3 x 3 and must remain stable in saves. A 48 x 48 world
sprite follows the inspected 16 pixels/tile convention. Include the chair within
those nine tiles, an inboard operator position and an unobstructed approach.
Do not make a miniature prototype and change its footprint after saves use it.
Placement sockets and seating cleanup pass native-definition checks; final use/sit placement and rotation await gameplay.

The console issues commands and displays status. Machines keep their own power,
cargo, progress, safeguards and autonomous queues. Local controls remain useful
for a small installation or a broken console. A console is not a new dependency
required to operate already-owned machinery.

## Evidence inspected

Local source observations below are from the current repository and the installed
game's definitions plus the project's local engine inspection. The assembly
fingerprint was rechecked in this round:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
It matches the [1.0.1.5 baseline](automatic-material-routing.md#game-update-observed).
No game was launched or controlled. Decompiled source and game artwork stay in
ignored local research; they are not part of this deliverable.

| Evidence | Consequence for this feature |
| --- | --- |
| Native `ItmStationNav` has a 3 x 3 item footprint, separate use/sit/power points, navigation conditions and several console interactions. | A seated workstation fits the game. Copying the whole nav definition would also inherit unrelated flight behaviour. |
| Native `GUINavStation` is a use-point interaction. Its panel map names `GUIOrbitDraw/GUIOrbitDraw`. | Reach the console through an ordinary world interaction; a UI texture does not provide the interaction by itself. |
| `CrewSim.RaiseUI` obtains a panel map's `strGUIPrefab`, loads a Unity resource under `GUIShip/`, initializes a `GUIData` harness and changes the UI state. | A new JSON panel name plus a PNG is insufficient to create a functioning native console. Our C# code needs to create/host the UI and handle its lifecycle. |
| `GUIPDA.ToggleNAV` checks a subscribed ship and uses its first nav station. | This is a remote-control precedent, not an existing general equipment-control API or precise machine-pairing service. |
| Auto Nav creates Unity UI/TextMeshPro children at runtime on the native nav canvas and overlays live controls on the approved faceplate. | Reuse this rendering technique and approved style. Its `NavModBase` integration itself depends on the flight panel and is not a standalone industrial window. |
| Reclaimer and collector definitions already register a right-click Control Panel interaction; their panels use temporary `GUI.Window` layouts. Fixture controls are still F9. | We can preserve the established interaction pattern and improve presentation. The fixture still needs a target-specific right-click entry. |
| `ProcessingService.AccessProblem` and `CollectorService.EndpointAccess` enforce local selected-crew range. Commands call those services. | A central screen alone would leave remote buttons failing. Access must become an explicit service-level policy for both UI and F3 commands. |
| Machine checks separately cover installation, damage, locks, loaded ship, switch/signal state, feed and output. Reclaimer heat checks use native room gas. | Remote commands must retain those checks. They replace the operator's location requirement only. |
| Processing/collection status is largely assembled into localized strings. | Add typed status snapshots for meters, reasons and action availability. Do not parse translated prose or invent telemetry. |

The developer-made [Official Ostranauts Starting Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3347080066)
also describes operating flight from an installed, powered navigation console.
It supports the workstation precedent; the current local code is the evidence
for integration details. The guide predates 1.0.1.5.

## What each panel actually needs

All five existing industrial equipment families appear in the overview. Opening
a local panel selects the clicked full object ID, not the nearest machine.

| Equipment | Status to present | Useful controls and limits |
| --- | --- | --- |
| Dismantling fixture | Intake assembly, feed count including active input, current recipe/progress, product capacity, power state, residue destination and block reason | Start/resume pipeline, pause pipeline, output routing. Manual feed and collect products stay local inventory actions. Starting the pipeline retains today's coupled grabber behaviour. |
| Exterior grabber | Linked chute/fixture, detached input eligibility, intake progress and power | Open the linked pipeline's controls. Start/pause is explicitly labelled as a pipeline action, because independent grabber control is not implemented. No hull-cutting button. |
| Hull chute | Supporting walls, alignment, adjacent grabber/fixture, installation and damage | Inspect connected pipeline. Passive hardware: no fabricated power switch, progress meter or airlock cycle. |
| Scrap reclaimer | Processing permission, receiving permission, four-packet feed, job progress, output space, input/output pairs, ambient room temperature/pressure, cooling block reason | Separate process and receive start/pause; input/output routing and filter. Feed/products are local inventory actions. |
| Residue collector/buffer | Incoming transfer state, exact filter, stored count/mass, source and outgoing destination, floor-route problem | Start/pause receiving, choose source/destination, filter, unlink the selected direction. No eject button: recoverable disposal remains unimplemented. |

Do not display future solvent tanks, ore plants, shredders or cutting tools as
available appliances. Auto Nav retains its flight console and safety checks.
An optional status/shortcut integration can follow a concrete provider adapter;
the industrial console must not require Auto Nav or silently take flight control.
Native pumps, reactors and third-party machines likewise need explicit adapters
before we claim they are controllable here.

## Operator workflow

1. Install and power the console using ordinary, separately placed conduit.
2. Right-click **Control Panel**; the operator uses the workstation. Show current
   ship and console identity persistently, with an obvious return/close control.
3. Overview lists supported equipment on that authorized ship. Select an item to
   show its detailed panel without opening a pile of overlapping windows.
4. Routing selects named source/output and destination/input. Names and positions
   are display aids; actual selection always carries the full persistent IDs.
5. Start processing and receiving explicitly. Already-armed local jobs continue
   after the operator closes the display; no crew has to stand at every appliance.

The first implementation uses friendly names with short IDs in rows, full IDs
in details, automatic type groups and case-insensitive name/ID search. Position
labels, aliases and copy buttons remain future refinements; aliases must never
change the persistent identity.
The first layout uses fixed regions and tabs. A user-configurable nav-style panel
editor would add complexity without helping this small equipment roster yet.

The overview's **Pause industry** means pause processing, coupled intake and
receiving transfers for the supported equipment on this ship. It is not a reactor
shutdown or a guaranteed electrical emergency stop. No blanket Start All in the
first version: independently armed processing and transfers should stay legible.

## Control access and failure behaviour

Introduce a checked command context, not a `remote=true` bypass flag. It identifies
the actor, console, ship and target; the service validates it at command time.
Revalidate after crew selection, object replacement, docking or ship changes.

| Situation | Proposed behaviour |
| --- | --- |
| Local panel/F3 without a console context | Preserve current local access requirements and gameplay checks. |
| Operator using an installed, healthy, unlocked and powered console | Permit supported commands to eligible equipment on the same authorized loaded ship. Do not require crew beside each target. |
| Console power/state or operator access is lost | Disable further commands and show the reason. Existing autonomous jobs retain their ordinary behaviour and local safeguards; monitor failure must not erase work. |
| Machine is switched off, damaged, locked, cooling-blocked or missing | Show the actual reason; remote control does not override it. Pausing remains possible where the existing service permits it. |
| Another ship is docked or is physically nearby | Do not discover or command its equipment. Same screen view, floor proximity or docking is not authority. |
| Inventory loading, product collection, repairs, restore, dismantling | Still require ordinary crew/item access. Central telemetry can show contents, but must not expose a remote draggable inventory. |
| Session reload | Restore identities, filters, cargo and saved job meaning. Processing/receiving remain paused as now; opening the screen never resumes them. |
| Pair/filter edit | Preserve current pause/reset and reciprocal-link rules. Validate both targets for console access; never bypass the physical material route. |
| Pause industry meets an inaccessible/missing target | Report which machines paused and which could not. No silent claim of atomic success; keep the operation limited to our services. |

Commands are same-ship software communication in this initial design. They do
not require a new simulated data cable or a material-floor route to the console.
The machines and console still need native electrical supply, and material
transfers retain their current floor and port checks. Native signal-off remains
an independent machine interlock. Do not use native electrical signal maps to
store our UI state or claim we have implemented signal-network data traffic.

**Implemented ownership policy:** native `CrewSim.system.GetShipOwner(ship.strRegID)`
must equal `CrewSim.coPlayer.strID`. Unknown, leased and foreign ownership fails
closed. Discovery uses `bAllowDocked:false` and exact `co.ship == host` filtering.
`ConsoleBinding` captures console, ship and operator IDs; every command resolves
fresh native facts. Moving the console/changing crew ends that binding. Power or
ownership loss disables actions without erasing autonomous work. No derelict
console population or authority over neighbouring owned ships is added.

## Framework and content boundaries

Extend Framework where the central and local panels create a concrete shared need:

- Framework supplies checked session identity (`ConsoleBinding`), typed activity
  and small Unity UI primitives (`PanelWidgets`). Shipbreaker owns discovery,
  snapshots, command dispatch and the native GUIData host for its five equipment
  families. A generic provider registry is deferred until another consumer needs
  it. Resolve targets by full ID on every action.
- Shared command-context/access plumbing and action results with stable reason
  codes plus localizable messages. Providers keep their machine-specific rules.
- A reusable panel shell, live labels, status indicators and routing selectors
  where actual panels share them. Keep machinery-specific layouts and artwork
  content-owned. No generic workflow editor or general automation scheduler.
- Existing definition registration, maintenance, construction, stock, pairing,
  saved filters and physical transfer remain authoritative.

Shipbreaker owns the console item and artwork initially, along with its price,
materials and service interactions. An independently useful console content mod
can be split out later with an explicit saved-ID migration, rather than moving
definitions between owners casually. Framework must remain usable without it.
Adapters for other authors are opt-in and versioned; a missing provider reports
unavailable equipment and must not throw or manipulate unknown saved objects.

UI callbacks, right-click panels and F3 all delegate to the same command service.
Add typed snapshots rather than a parallel simulation: state/reason, permission,
current/maximum feed, stored mass, recipe progress/duration, capacity result,
port partner, filter and available commands. An observation must not arm a job.

Power should initially display **configured demand** and native powered/block
state. Do not label demand as measured consumption or sum a whole-ship meter
without the corresponding telemetry. Temperature is **room temperature at the
reclaimer's use point**, not a fictitious measured machine-core temperature.
Unknown, unavailable and stale values get explicit states, never a healthy zero.

The implemented visible panel refreshes snapshots/discovery every 0.5 s;
closed panels do no polling. Stable rows update their text without rebuilding
buttons unless membership changes. Topology search stays in existing command/
transfer services, not the rendering loop.
Gameplay services still decide at action/transfer time whether a route is valid.

## UI implementation constraints

- Native context-menu actions are established in our code. Reuse them for all
  machines, plus the console use interaction. F9 remains a useful fallback.
- Prefer runtime Unity UI/TextMeshPro for the finished display, following Auto
  Nav's rendering precedent. A shared skin replaces the temporary IMGUI look.
- Prove the custom window's native canvas hosting, close/escape behaviour, input
  blocking and crew/use interaction lifecycle before treating it as production.
  `Resources.Load` cannot discover an arbitrary newly named PNG as a UI prefab.
  Scope any interception to our IDs; do not replace the game's general RaiseUI.
- Show disabled actions with reasons, progress plus numbers, text beside colour
  lamps, and scroll/wrap support for long translations. Keep every label live.
- Suggested design area: 1120 x 680 logical units for central control and
  760 x 560 for a local detail panel. Fit to available screen space/UI scale,
  preserving useful text size and scrollable content. These are design targets,
  not claimed verified display support.

## Graphics brief

The existing owner screenshots and approved Auto Nav faceplate are sufficient
for initial industrial-console art. No more screenshots are needed now. If the
in-game UI hosting/scale differs, request one ordinary open native equipment
panel at the owner's usual UI scale; there is no need to rearrange their ship.

| Asset | Need and production constraint |
| --- | --- |
| Shared industrial panel frame | Original neutral slate faceplate, subtle bevel, corner fasteners, dark display backing. Produce one text-free master with safe slicing margins; reuse it at central and local sizes. Do not stretch the approved Auto Nav plate's fixed wells into a new aspect ratio. |
| Controls and lamps | Live Unity controls with consistent restrained bezels, on/off/disabled feedback, green/amber/red states plus words. Generate only any textured bezel artwork the shell genuinely needs. No separate full-screen image per status. |
| Machine thumbnails | Reuse our existing original machinery portraits for fixture, grabber, chute, collector and reclaimer. No new machine artwork needed for those pages. |
| Console installed/intact | Original overhead 3 x 3 workstation, 48 x 48 export, chair in the footprint, broad dark screen, pale/slate casing and a few purposeful accents. Clear use position and power connection. Coarse pixel clusters; no tiny painted labels or baked conduit loop. |
| Console installed/damaged | Registered matching shape; localized broken screen/casing damage, matching normal and portrait exports. Damage must remain distinct from runtime obscuration or lack of light. |
| Console loose/intact and loose/damaged | Braced/packed versions at the agreed item footprint, with corresponding colour, normal and portrait exports. Native condition rendering may share inputs only where intentional and documented. |

Expected initial scope is **one shared interface frame plus four console forms**,
with their required normal/portrait exports. Existing machinery remains unchanged.
No animated machinery or artwork with baked-in labels, bars, amounts or alerts.
Keep the UI cleaner/flatter than the deliberately pixelated world sprite.
Use Imagegen for the original raster art when production begins; retain prompts,
references, source hashes and provenance alongside the existing asset records.

## Implementation sequence and checks

1. Agree the text layouts and 3 x 3 workstation direction; extract the real
   snapshot/action contract and settle owned/leased-ship access.
2. Build a useful text-backed local and central control slice with checked
   console access. Verify normal use/close behaviour and missing/powerless console
   handling. This directly becomes the final UI, not a disposable power probe.
3. Generate the shared frame and console art against those measured layouts;
   export at game scale, integrate dynamic controls and all item states.
4. Give the console ordinary merchant/construction/repair/restore/dismantling
   support using existing Framework/economy services. Audit actual bill mass and
   returned value; buying and dismantling it must not become a profit source.
5. Build and package, then install when the game is closed. Owner checks the
   connected setup from one console and compares a local panel on the same target.

Useful automated checks: wrong ship/crew/console rejection; local access retained;
remote access preserving machine interlocks; selected target identity after mode
switch/reload; no remote inventory bypass; process/receive independence; partial
bulk-pause results; missing-provider handling; saved jobs/pairs/filters unchanged;
snapshot reads granting no run permission; translation coverage.

Owner checks: operate distant equipment without walking to it; control both
reclaimer permissions; backpressure and cooling reasons remain visible; lose
console power then use local controls; close/reopen/switch crew without stuck
input or unintended commands; inspect at normal UI scale and after reload.
These test our new access/UI behaviour, not already-established basic conduit use.

## Implemented slice and remaining limits

0.10.0 adds the 40 kg / 80 W console, four 48 x 48 sprite states, shared frame,
construction, maintenance and additive station stock. See the player guide for
exact bills, prices and checks. Local and central panels are generated with
Unity UI/TMP under the native control-panel canvas, using GUIData close/escape
lifecycle and a narrowly scoped restore hook for our exact key. F9 and earlier
console controls remain fallback diagnostics. Broad provider registration,
aliases, PDA overlays, automatic derelict population and remote inventories are
not implemented. Final game rendering and seating require owner validation.
