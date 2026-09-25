# F6 vanilla UI reuse, layouts and graphics brief

**Implementation update (25 September 2026):** Shipbreaker 0.12.0 / Framework 0.16.0
now prepare the electrical casting candidate described in the [F6 operating guide](furnace-player-guide.md).
Read that guide for current dimensions, acquisition, controls and owner checks.
The research and direct-fusion installation diagrams below retain their historical scope;
they are not a record of an in-game test.

**Energy-source update:** [electrical heating is now selected](furnace-electrical-direction.md).
Retain the panel design and vanilla reuse priorities. The original coupler and
direct-fusion installation below are historical candidates, excluded from the
first electrical build; its coupler art is no longer required.

**25 September 2026.** Companion to the [first-cycle specification](furnace-first-cycle.md).
This round inspects native resources and produces a
[browser layout study](../assets/phobos-furnace/research/layouts.html).
There is no Unity furnace panel, no production artwork and no in-game UI test yet.
The browser study uses original geometry placeholders, not extracted game pixels.

## Reuse decision

**Use vanilla assets at runtime wherever the individual element fits.** First
reuse a self-contained native widget, next use its artwork with a small Phobos
adapter, and create original UI art only for a demonstrated gap. This supersedes
the earlier assumption that every furnace knob, lamp and LED needed new artwork.

`ReactorIC` in native `guipropmaps.json` specifies `GUIReactor`.
`CrewSim` loads that via `Resources.Load<GameObject>("GUIShip/" + prefab)`.
The inspected `GUIReactor` prefab in `resources.assets` contains **2 knobs,
14 LED meters, 43 lamps, 4 seven-segment displays, 1 guarded switch, 9 toggles
and 2 sliders**. These counts describe the prefab, not a proposed panel inventory.
Game resource version is Unity **6000.3.23f1**, assembly fingerprint as recorded
in the first-cycle report. Sprite names below are serialized references, not
assumed independently loadable Resources paths.

Run `scripts/inspect-furnace-ui.py --game <local-game-directory> --output
.local/research/furnace/ui-metadata.json` with the existing local UnityPy research
dependency. It reads six selected prefabs, geometry, component types and targeted
sprite/guard references. The small custom-field decoder refuses a different
assembly hash or Unity serialization version; native path IDs are **research
metadata only**, never stable runtime lookup keys. No textures are exported.

## Element-by-element audit

All reactor paths below are relative to **`GUIShip/GUIReactor`**.

| Native path / component | Verified native artwork or dependencies | Furnace choice and adaptation |
| --- | --- | --- |
| `pnlPower/knobBus` / `GUIKnob` | `aStates`: `GUIKnob02_0`, `_1`, `_2`; each sprite 64 x 64; same family at `pnlCoilPump/knobPump` | Reuse for OFF/STANDBY/RUN and cooling LOW/AUTO/HIGH. Keep three real detents, suppress callbacks during display refresh. |
| `pnlPower/pnlLedsTotal` / `GUILedMeter` | `GrnOff/On`, `YelOff/On`, `RedOff/On`, 20 x 20; child rows each contain an Image; `aOff`, `bReverse`, initialization curve | Reuse complete isolated bank for requested/received/rejected heat. Validate row/array counts and use finite clamped values. |
| `pnlCoreTemp/pnlLeds` / `GUILedMeter` | Wide LED family `GrnOffWide/OnWide`, `YelOffWide/OnWide`, `RedOffWide/OnWide`, 29 x 18 | Use for process/sink temperature or margin; distinguish a temperature gauge from an energy reserve. |
| `pnlInit/pnlStepBus/bmpFound`, `bmpGreen`, `bmpOff` / `GUILamp` | White, green and red off/on sprites; two initialization/wait curves | Reuse lamps for sequence evidence; build our own localized row labels. No inherited reactor readiness calculation. |
| `pnlPower/bmpThrustWarn` and `pnlLamps/bmpCap` / `GUILamp` | `GUIBacklitRedOff/On`, `GUIBacklitGrnOff/On`; white label wells also exist | Reuse blank backlit wells for process alarms/status, with furnace wording. Do not copy reactor labels such as XRAY. |
| `pnlFuel/pnlHe3` / `GUI7Seg` | `GUI7SegDigits_0` through `_9` plus `_blank`, each 77 x 111; named `bmpDigitN` / `bmpDotN` children | Prefer native digit artwork with a Phobos formatter; use a normal localized text field for signs, Unknown, units and overflow. |
| `pnlPower/chkThrustSafety` / `GUISafetyToggle` | Serialized `btnSafetyOpen`, `btnSafetyClosed`, their CanvasGroups, `GUIToggleSwitch`; its `GUIToggleSwap` targets the same Toggle and uses `GUIToggleSwitchOn` (32 x 64) | Keep guard and sprite-swap mechanics. Bind furnace heat/isolation actions after neutral initialization and listener audit. |
| `pnlPower/chkMHDOn` / `Toggle` | `Background` and `Background/Checkmark` Images | Reuse visuals and native toggle mechanics for AUTO/STEP or pump; replace events on the clone, retain no MHD action. |
| `pnlPower/SliderFlow` / `Slider` | `Background`, `Fill Area/Fill`, `Handle Slide Area/Handle` Images | Reuse complete visual child hierarchy for power cap; give it furnace limits and no reactor FLOW binding. |

Numbers in the sprite column are source rectangles, **not mandatory screen
dimensions**. Preserve their aspect, borders and nearest-neighbour suitability;
measure the available panel before choosing rendered sizes. Do not re-export or
upscale vanilla textures to satisfy the policy for newly authored artwork.

### Wider vanilla investigation

Native `GUIShip/GUIAirPump` is another confirmed GUI-map resource. Its
`MainPanel/lblTitle` is a TextMeshPro label; use its `font`, shared material and
style references for the industrial panel where glyph coverage permits.
`MainPanel/btnScrew01` through `04` contain frame/screw Images and Buttons.
Copy Image visuals only for inert framing, so decorative screws do not open
the air pump's configuration panel. The SidePanel's `GUIPressurePanel Top` and
`GUIGaugeInstrument Top` contain `Container/imgCorners` and live TMP labels,
useful for inset frame geometry and value/units layout. These are visual donors,
not furnace sensor controllers. `pnlInside/btnDone` supplies a generic Button/Image
and legacy Text child; use the native appearance with a new localized TMP label.

The navigation `NavModTimeZoom` prefab has TMP labels and Button/Image/GUIAudioBtn
controls, including `Container/Zoom/btnZoomStn` and its `txt` child. Its full resource
loading route was not established in this focused audit, so it is an alternative,
not a mandatory furnace dependency. `GUIMeter` uses older Text labels and a
whole-machine input/property-map controller; its behaviour is unnecessary here.
None of the six inspected prefab subtrees supplies a suitable general-purpose
input field or ScrollRect. Retain existing Framework clipping, scrolling and
search/numeric-entry foundations, styling them with the selected native font/frame
where possible. This is a scoped finding, not a claim that vanilla has no such UI.

The audit resolves custom-widget sprite arrays, guard references and the switch
swap helper. `GUIToggleSwap.Awake` disables Toggle fading and subscribes a sprite
change to `targetToggle.onValueChanged`; it has no reactor command. Its target
must resolve inside the cloned subtree. Preserve this visual listener while
removing external actions; a no-notify refresh also needs an explicit visual
refresh because it will not invoke the swap listener.

`GUIReactor.Awake` supplies the reactor-specific bindings: `knobBus.Callback`
calls `SetPowerBus`, `knobPump.Callback` calls `SetPump`, MHD's `onValueChanged`
writes `chkMHDOn`, `SliderFlow` writes `slidFlow`, and the guarded switch calls
`ToggleThrustSafety`. These bindings are established by the full panel controller,
which must never be cloned. Furnace adapters instead bind mode/cooling requests,
power-limit requests and heat-permission/isolation to their checked service.
`GUILedMeter.Awake` requires **each row's first child Image**, indexed by `aOff`,
and initializes OFF; keep both hierarchy levels. `SetState(2)` selects an active
reading without a startup sweep, after validating and setting the value.

Ordinary
Unity `Image.sprite`, `Slider` fill/handle and Toggle graphics are accessed by
their exact paths above at runtime; their full serialized event/material fields
were not decoded by the small research parser. Inspect those references on a
clone before enabling it; their suitability is not already a runtime test pass.

## Safe runtime composition contract

1. Load the immutable resource prefab. Resolve only approved child donors;
   never instantiate the whole `GUIReactor` and then hope to remove its controller.
   Its Awake wires reactor commands and Update requires an actual reactor.
2. Instantiate each donor under an **inactive** Phobos-owned root. Before activation,
   verify required components, children, arrays and referenced objects. Read shared
   sprites/fonts/materials; never modify or destroy those assets.
3. For commands, replace cloned serialized UnityEvents (removing runtime listeners
   alone does not remove persistent ones). Audit UnityEvent targets, Toggle groups,
   navigation links, animation bindings and scripts for references outside the clone.
   Remove gameplay controllers; preserve audited internal guard/layout behaviour.
4. Guard initialization: GUISafetyToggle.Awake closes its cover and sets its switch
   false. Establish a neutral state before binding furnace commands. GUIDisplay
   changes must run under a scoped `refreshing` guard, including any nested callback.
   GUIKnob.SetStateSilent still invokes Callback; temporarily detach or guard it
   and restore in a finally block. Unity no-notify setters apply where available.
5. Bind one small adapter to the same furnace service used by C1/F3. Reads cannot
   advance physics or edit saved/configured state. Separate requested from actual
   latch, temperature, heat and sequence status.
6. On close, detach callbacks and destroy only owned instances. Reopening must not
   multiply listeners or retain a former machine ID. Losing authority clears the
   view's actionable binding immediately; it does not erase batch state.

Use a small Framework widget factory/adaptation helper only as the furnace creates
that concrete shared need. This research adds no new Framework API or wholesale
UI-theme replacement. Existing Auto Nav artwork stays unchanged.

### Specific behavioural limits

- **Knob:** native left click decreases, right click increases; drag advances
  detents. There is no native keyboard/wheel handler in the inspected class. Keep
  native pointer behaviour; add focused keyboard access and precise numeric input
  where meaningful. Native three-state sprites must not pretend to offer continuous
  temperature adjustment: use a slider/numeric pair for temperature/ramp/hold.
- **LED meter:** the float setter turns NaN into zero and does not clamp all invalid
  inputs. Filter values before calling it. Unavailable readings get an unlit bank
  plus visible Unknown/Fault/Stale text. A startup sweep is a labelled lamp test,
  never a valid measurement. Native animation uses real frame time, independent
  of process simulation time; no automatic sweep on every refresh.
- **Digits:** numeric mode clamps negatives, formats with current culture but
  searches for '.', and truncates to fit. Text mode does not provide a general
  minus/letter/decimal formatter. Use the sprite set with a small invariant-digit
  formatter, explicit decimals and overflow indication, or use native-styled TMP.
  Display negative Celsius truthfully; never clamp it to zero for appearance.
- **Safety cover:** the cover prevents casual pointer access, not service-level
  bypass. Keyboard/F3/remote actions retain the same actual interlocks.
- **Sliders and scrolling:** wheel over an unfocused control scrolls the page.
  A grabbed slider retains pointer capture; numeric-entry focus suppresses movement
  input. Preserve input while game simulation is paused without advancing the batch.
- **Audio:** retain only audited generic click/knob sounds by existing emitter
  reference. Reactor ignition/coil sounds are inappropriate for a furnace toggle.

If a donor is unavailable after a game update, diagnose the missing path/type once
and use the existing Framework control with the same service action. Unknown
telemetry stays unknown; emergency isolation remains available through the fallback
and F3. Donor compatibility must not disable physical cooling.

## Mockup contract

The linked study contains **Installation**, **Full panel** and **Compact panel**.
Its reactor is a labelled edge reference, not a claim about the complete native
reactor footprint. The tile grid fixes the proposed F6 6 x 6, coupler 2 x 2 and
radiator 6 x 4, with intact hull and an operator aisle. Clearances outside the
shown reactor edge still require native placement checks.

Panel groups are sequence, heat delivery, chamber/charge, and cooling/condition.
Fixed identity/phase and stop/isolate controls survive scrolling. Local and C1
open the same expanded instrument view; returning to C1 restores its roster.
At small widths the same sections stack; they are not reduced to illegible gauges.
Mockup scenarios include cold idle, melting, cooling, blocked output and unknown
probe. All values are clearly labelled illustrative; the browser does not simulate
the furnace and contains no native game art. Stop/Isolate affect the example view only.

## Original artwork remaining

### Minimize the work needed for later changes

Owner direction, 25 September: design the graphics so routine changes affect the
smallest relevant part. Keep the approved instrument grouping and visual language.
This is the production approach for the future furnace, not a claim that its
runtime panel or layered source art already exists.

**Cost constraint:** the owner only wants this approach if it does not drastically
increase ChatGPT charges. Keep the additional effort small and part of ordinary
asset production. Reuse existing components and exporters; retain useful source
layers as the artwork is made. Do not commission extra image generations, split
every decorative detail into a separate asset, or undertake broad refactors solely
for hypothetical future changes. Defer optional infrastructure until repeated real
work justifies it. No exact billing saving is established by this design.

**Panel composition:** assemble separate reusable controls on a resizable frame.
Do not paint a complete panel with labels, gauges and values into one image.
`IndustrialPanel.Build` already uses a sliced frame with separate live controls;
Framework's `PanelWidgets` already supplies labels, input fields and scrolling.
Build on those patterns and the native donors audited above.

- Keep five named presentation sections: sequence, heat delivery, chamber,
  charge/casting and cooling. Full and compact views arrange the same sections
  and adapters; they do not own duplicate control implementations or artwork.
  Keep identity, alarms and Stop/Isolate outside the scrolling area.
- Keep captions, names, units, limits and status messages in live localized text.
  Numeric scales derive from the service/recipe. A balance change must not require
  a new dial texture or handwritten maximum in another file.
- Centralize native resource paths and compatibility checks in a small donor
  catalog. Panel code requests a role such as three-detent selector or guarded
  switch; it does not repeat prefab child paths. If an update moves a native
  donor, repair that mapping/adapter once and retain the Framework fallback.
  This is a proposed implementation boundary, not an existing Framework API.
- Keep frame borders, spacing, font choices and status colours in a small shared
  style definition. Stretch/tile only suitable frame interiors and edges; retain
  corner dimensions and control aspect ratios. Do not stretch knobs or digit art
  to fill arbitrary spaces. Do not create a general-purpose theme engine.
- Layout and styling remain presentation only. Replacing an Image or moving a
  section must not change commands, saved state, physical footprint or ownership
  checks. Keep native callback suppression and isolation rules from this audit.

**Machine art:** retain one editable, registered source composition for each
equipment family. Use practical layers: chassis/vessel, hatch and fittings,
connections, surface finish/markings, and local damage. Retain a layered authoring
file or aligned transparent source PNGs with a documented composition order.
Unchanged portions stay fixed when revising a component. Prefer targeted edits
of retained artwork over generating a whole replacement machine.

These are **authoring layers**, flattened into the ordinary native colour/normal
files at export. They do not require a new in-game layered-sprite renderer.
Installed and loose forms can share suitable source parts, while retaining their
own mounting/removal differences. Damage can use local masks for surface wear;
dents, missing geometry and changed silhouettes need deliberate colour/normal
edits, not a generic scorch overlay. Normals describe surface direction and must
not be alpha-blended like colour layers without correct vector handling.

Freeze the approved canvas, crop, pivot, scale and connection anchors for each
form. For F6 retain the 96 x 96 world export and at least 192 x 192 master. Keep
larger masters and the separate portrait requirements. A connector revision
updates its layer and affected maps; unrelated controls or radiator art stay fixed.
Reserve modest breathing room in layouts without drawing speculative hardware.

**Exports and asset identity:** when the first real masters exist, add a small
furnace manifest and exporter using the existing industrial-console/Shipbreaker
patterns. The manifest owns source/layer paths, dimensions, crop/pivot, output
paths, hashes and provenance; the exporter owns composition and derived files.
Retain stable runtime filenames, with revisions/hashes in the manifest rather
than changing every consumer's asset path. Never overwrite an approved master
silently or distribute native donor images.

An explicit family/form selection should rebuild the affected colour, normal,
portrait and previews together. Rebuilding unchanged exports is harmless; do
not build a complex dependency cache merely to avoid inexpensive image exports.
Keep derived files out of the manual editing workflow. Share small export helpers
where a real repeated operation justifies them; no new asset framework or atlas
packing is required for this equipment set.

| Future change | Expected work |
| --- | --- |
| Recipe yield, power limit, name or translation | Service/catalogue value and live text; normally no artwork |
| One extra instrument | Add its binding/control within the relevant section; reflow layout using the existing donor |
| Wider or compact panel | Layout measurements; reuse frame, sections and controls |
| Vanilla widget path changes | Donor mapping or its adapter, plus compatibility checks |
| Different hatch, connection or local damage | Edit the relevant source layer and affected normal detail; regenerate that form's exports |
| Electrical instead of direct-fusion supply | Change source labels/bindings and omit coupler equipment; preserve the panel family |
| Major footprint or vessel shape change | Revise the affected machine geometry/maps and placement checks; panel controls remain reusable |

Validate only the affected boundary: export dimensions/alpha/registration and a
native-size preview for art edits; long labels, narrow/full layouts and input
behaviour for panel changes; isolated donor compatibility after native changes.
Use source hashes and visual comparison to catch accidental changes elsewhere.
No approach makes a major silhouette change free, but it should not force a
redesign of unrelated equipment or the entire control panel.

### Current electrical-build asset list

Use the equipment study's broad painted masses, coarse clusters and restrained
functional detail. The vessel has a sealed cassette and no open molten pour.
The radiator remains separately installed hardware; ordinary conduit
must never be painted as the furnace's border. No new production images this round.

| Family / ownership | Proposed runtime colour size | Minimum new master | Required forms |
| --- | --- | --- | --- |
| New F6 furnace, 6 x 6 | 96 x 96 | 192 x 192 | Installed intact/damaged; loose intact/damaged; unfinished assembly section |
| New radiator, 6 x 4 | 96 x 64 | 192 x 128 | Installed intact/damaged; loose intact/damaged; fixed radiator, no deployment animation |
| New rough/finished housing, 2 x 2 | 32 x 32 | 128 x 128 | One rough casting and one recognizably finished version |
| New terminal melt remainder, 1 x 1 | 16 x 16 | 64 x 64 | One sealed residue packet; not ordinary trash |
| Vanilla UI reused | Native asset rectangles | Preserve native originals | Knobs, lamps, LED banks, digit sprites, switches, sliders, suitable framing and fonts |
| Existing Phobos reused | Existing approved sizes | Preserve originals | C1 workstation/portrait and Framework fallback frame; no redesign |

The selected electrical build has **12 new colour forms**, each with an aligned
normal and a portrait: up to 36 standard runtime exports. The former direct-fusion
coupler's four forms are excluded. These exports are derived deliverables, not
36 separately painted originals, with damage shaders reusing the relevant damaged
form rather than inventing an extra state. Portrait reference is 256 x 256 with
at least a 512 x 512 authored master; retain larger original generation masters.
Separate cassette art is unnecessary because the first mould/cassette is captive
and not a traded item. Native aluminium offcuts use existing native item artwork.

Prefer two unfinished sections for the large furnace, following existing equipment
construction practice; their common unfinished appearance is the fifth F6 form.
Exact full-machine construction/economy awaits the electrical implementation.
The radiator uses a direct construction recipe and needs no separate unfinished item.
Assign their full branded model names when dimensions are accepted; no new saved
identity or naming-map entry is introduced by these research names.

Record final crop, pivot, alpha, rotations, connection locations, source hash and
prompt in the new asset manifest. Generate native-size derivatives reproducibly
with existing export patterns. Keep damage/normal registration and sprite bounds;
do not ship a larger world PNG at unchanged native scale. Runtime native UI reuse
requires no copied game texture in the mod, mockup or public asset directory.

## Validation record and remaining checks

**Performed:** assembly/version check; selected prefab hierarchy and component
inspection; sprite references and dimensions; native knob/LED/lamp/digit/guard
method inspection; browser scenarios, heat-limit control, Unknown handling, guard
and stop actions; all three layouts at 360, 800 and 1440 pixel widths; scrolling
without overlapping the fixed alarm/stop area; visual inspection of rendered views.
These establish candidates and layout feasibility, not Unity widget isolation.

**Unity checks still required when the panel exists:**

| Scenario | Required result |
| --- | --- |
| Open cold, hot, stopped and reload-paused batch | No actions or saved-state writes from refresh/initialization |
| Reopen and switch selected machine repeatedly | Exactly one action per gesture; no stale machine callback |
| Set knob programmatically; refresh slider/toggle | Zero furnace command dispatches |
| Clone guarded switch | Internal cover works; no reactor event or external object references |
| Negative/missing/NaN/stale/large readings; comma-decimal culture | Truthful value or explicit unavailable/overflow; no false zero or formatter exception |
| Paused game; focused keyboard; wheel over scrolling content | Controls work as permitted; physics does not advance; no accidental setpoint change |
| Narrow UI and long translations | Reachable controls, clear units/alarms, fixed stop/isolate actions |
| Missing donor, close/reopen, disabled/damaged machine | Diagnosed usable fallback; native assets and cooling state preserved |

The owner runs native gameplay/rendering checks. No diagnostic-only installation
or new screenshots are prerequisites for completing this design work.
