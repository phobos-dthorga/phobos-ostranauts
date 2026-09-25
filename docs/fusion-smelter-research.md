# Fusion furnace and instrument panel: feasibility and design

**Superseding owner decision, 25 September:** [use electrical heating for the
first F6](furnace-electrical-direction.md). The earlier direct-fusion-first language
below records the original research direction and no longer constrains delivery.

Research date: **24 September 2026**. Status: **research and proposed design only**.
No furnace, heat connection, recipe, hazard, artwork or installation is added by
this document. The owner's reactor screenshot supplies the visual direction;
local code inspection establishes game capabilities, not an in-game test.

**25 September first-cycle follow-up:** the [complete cycle specification](furnace-first-cycle.md)
compares current construction and repair contracts and recommends a 20 kg aluminium
batch for an optional D4/R4 housing. It includes finite gas recovery, thermal
calculations, installation geometry and failure/save rules. The [native UI reuse
audit and graphics brief](furnace-ui-and-art.md) and [interactive layouts](../assets/phobos-furnace/research/layouts.html)
supersede the original assumption of newly drawn controls below. The 6 x 6,
50 kg and 250 kW hardware baselines remain provisional; accounted native fusion
delivery is still an explicitly unresolved implementation connection.

**25 September instrumentation direction:** the [suite sensor report](sensor-integration-research.md)
distinguishes native ship sensing, room alarms and proposed process probes. Use
built-in basic furnace instruments and modular specialists; native ship IR does
not measure this chamber. Missing readings remain unknown while stored heat and
pressure continue to evolve. This follow-up adds no furnace or sensor behaviour.

The subsequent [shared console observations](shared-console-observations.md)
implementation establishes native alarm-output validity and the existing R4's
cooling probes. It supplies reusable evidence/diagnostic patterns for a future
furnace; it does not yet supply its chamber or cooling-loop instrumentation.

## Recommendation

Build a **sealed, batch-operated fusion furnace** as Shipbreaker's industrial
centrepiece, downstream of careful dismantling and material separation. Give it
a dedicated instrument panel reachable locally and through the existing Asterel
C1 console. The owner wants direct fusion energy first, with an electromagnetic
alternative potentially later. Do not silently substitute an ordinary electrical
appliance for that request.

We can closely match the reference's **visual density, tactile controls and
readable operating sequence**. The game already implements its reactor screen
with Unity switches, sliders, rotary selectors, lamps and segmented meters. Our
own panels establish the integration route. Achieving the same useful depth
requires new furnace behaviour behind those instruments; copying a reactor
prefab would not provide it.

The largest unresolved engineering task is **accounted direct-energy extraction**.
The inspected reactor has no general process-heat outlet. Its impressive power
readouts are not an API granting that much usable industrial heat. Resolve the
source debit and cooling path before promising a functioning fusion furnace.
The panel and bounded furnace model are feasible; that particular native adapter
is a candidate requiring focused implementation and verification.

## Owner requirements and proposed choices

**Requested:** a fusion-driven smelter, an impressive reactor-like control panel,
meaningful environmental adjustments, multiple physical-looking controls and
LED feedback. Preserve the future electromagnetic alternative, Framework use,
localization, ordinary-save persistence, central control and original branding.

**Proposed, not approved dimensions or balance:** reserve a **6 x 6 tile furnace**
with a sealed loading cassette, central hot chamber and captive casting/cooling
cassette. Start sizing around **one charge of up to 50 kg**, rather than a
continuous industrial river of molten metal. A **250 kW delivered-heat ceiling**
is a candidate for balancing, not a measured requirement. Source input must be
higher when coupling loses energy; pumps and controls also consume electricity.
Cold lining, batch size, temperature and cooling determine cycle length, rather
than a universal countdown. Finalize intended dimensions and capacities before
creating world sprites or introducing saved definitions.

The provisional name is **Phobos' Rivetline F6 Fusion Furnace**. F6 follows our
width-based industrial model convention; it is not yet in the equipment naming
map. A future electrical variant could share the chassis and panel. Do not
assign separate model IDs until its actual hardware distinction is settled.

The scale recommendation is game design. Real equipment spans much wider scales:
Royce operates 10 kg and 25 kg vacuum induction machines, while Consarc documents
far larger installations. Those precedents support batch processing, not this
fictional machine's footprint, price or thermal rating.
[Royce facility](https://www.royce.ac.uk/equipment-and-facilities/vacuum-induction-melting/),
[Consarc VIM](https://consarc.com/wp-content/uploads/sites/9/2019/05/Vacuum-Induction-Melting-Furnace-guide-en.pdf).

## What the native implementation actually supports

Inspected **Ostranauts 1.0.1.5**, repository commit `d04878f`, Framework **0.12.0**,
Shipbreaker **0.10.1**, Auto Nav **0.8.1**. The supplied reference screenshot shows
1.0.1.4; the implementation evidence below comes from the installed assembly.

| Inspected evidence | Consequence |
| --- | --- |
| `GUIReactor : GUIData` uses `GUIKnob`, `GUILedMeter`, `GUILamp`, `GUI7Seg`, `GUISafetyToggle`, Unity `Toggle` and `Slider`. | All the requested kinds of instrument have a native precedent. Runtime UI changes do not require animated furniture sprites. |
| `GUIReactor.GetUIRefs` expects many fixed child paths; `Update` requires `IsReactorIC`, and callbacks write reactor properties. | Do not attach it to our furnace or disguise the furnace as a reactor. Use a dedicated UI harness and checked furnace service. |
| `GUIKnob` supports bounded/wrapping states, dragging and a callback. `GUILedMeter` drives an array of on/off sprites and includes a startup sweep. | Similar behaviour is practical. Reuse small native components only if their hierarchy and initialization contract fit; otherwise use our own simple widgets. No need to copy decompiled code. |
| Native `ReactorIC` GUI map names `GUIReactor` and stores `knobBus`, `knobPump`, `knobRatio`, switches, `slidFlow` and `slidCycle`. | A JSON GUI map stores controls but does not construct a new furnace interface or simulation by itself. |
| `FusionIC.Run/Fusion` reads native readiness, fuel and reactor modules, updates wear, and sets ship thrust. It schedules around 0.27 game seconds. `CatchUp()` invokes its ordinary update checks. | Let native code own fuel burn and reactor lifecycle. A furnace must not call extra fusion ticks to obtain extra energy. |
| `StatICPwrTotal/Fus/MHD/Thrust/Load` feed the native LED banks. `Fusion` multiplies its power value by 10^12 in its calculation. | These are reactor-model power quantities, not kWh balances. The adapter needs explicit units and confirmed delivery, not `readout * elapsed = free heat`. |
| `Run` assigns `StatICPressureA` from the core-temperature state in the inspected path. Native GUI temperature and pressure bars use scaled values. | Do not reinterpret the reactor's pressure display as a physical furnace pressure sensor, or its MeV scale as metal temperature. Our chamber needs its own bounded state and units. |
| `Fusion` replenishes `StatPower` from its current power definition; `Powered.GatherPower/TransmitPower` handles native deliveries. `UserPowerExt` delegates to `UsePower` without returning a delivery receipt. | Native energy storage/generation is abstracted. Merely reducing a display condition, subtracting a buffer once, or calling `UserPowerExt` does not prove a persistent raw-energy allocation. |
| `FusionIC.CheckNWZ` sets station-proximity state and inhibits thrust controls, with a one-game-second recheck. | Retain native zone rules. A sealed industrial heat draw is distinct from an open torch burn, but permission for the proposed coupling is not established by this inspection. |
| Auto Nav's `TorchDriveController` owns temporary native flight-control commands. | Furnace code must not fight Auto Nav over FLOW, CYCLE or RATIO, ignite a reactor implicitly, or steal emergency propulsion authority. |
| `IndustrialPanel`, `IndustryService`, `ControlAuthority` and Framework `ConsoleBinding` already support local/central controls and ship scope. | Add a furnace detail view and shared action dispatch, retaining search, groups, Attention and per-command authorization. Docked ships do not become one industrial authority. |
| `ReclaimerHeat` accounts for received electrical energy and adds heat to native room gas through `fDGasTemp`. | Reuse receipt/accounting lessons. Dumping a furnace's entire heat load into the room is not a suitable default cooling design. The current reclaimer is not a heat exchanger or radiator service. |

## A credible source of heat

### Direct fusion route: the requested primary design

Propose a **shielded reactor-side energy coupler**, a separate installed attachment
paired by full object ID to one eligible reactor and one furnace. It diverts a
limited share of the reactor's energy before electrical conversion into the
furnace's contained heating assembly. Keep the transfer short and physically
constrained initially; paired IDs select endpoints, they do not establish a
magical ship-wide heat pipe. Ordinary yellow electrical conduit supplies controls
and pumps, not the high-grade heat connection.

This is an **authored extension of Ostranauts' fictional reactor technology**,
not a demonstrated off-the-shelf fusion foundry. The precise transfer mechanism
and attachment geometry remain to be selected. Never run raw core plasma through
an ordinary water hose, feed scrap into the fusion core, or equate warm reactor
coolant with a source capable of melting steel.

ITER provides a useful boundary: its blanket captures fusion energy in coolant,
and its wall-facing structures have demanding heat-removal requirements. It is
a different reactor/fuel system from the game's model; it does not validate this
coupler or imply coolant reaches the plasma's temperature.
[ITER blanket](https://www.iter.org/machine/blanket).

The native adapter must establish all of the following together:

1. A completed, eligible native reactor interval with fuel actually available,
   explicit source identity and validated installed coupling.
2. A bounded industrial allocation, including coupling losses, derived from a
   source budget whose units and time basis have been audited.
3. An effective debit or reservation in the corresponding native output path so
   the same share cannot simultaneously feed MHD generation or propulsion.
   Patching `StatICPwrMHD` alone is insufficient: prove actual delivery changes.
4. One receipt per source interval shared by all consumers; total grants cannot
   exceed the reserved allocation. No separate full budget for every furnace.
5. No replay across reload, no accrual while the reactor is unavailable, and no
   authority inherited merely from selecting the reactor on a panel.

**Recommended initial policy:** industrial heat is interruptible and lower
priority than flight. Yield on torch demand, manual reactor changes, shutdown,
loss of access or uncertain native compatibility. Preserve the batch and stored
heat. Start with deliberate processing while coasting; simultaneous torch and
industrial draw should wait until the shared allocation is verified. Never stop
the ship or disable the torch just to finish a casting.

The native model's large replenished electrical store and separate thrust path
make this a genuine adapter problem. A narrow hook might be sufficient, but its
exact patch point is **not yet verified**. If proving an effective native debit
requires an invasive reactor rewrite, recommend revising the connection design
with the owner. Do not ship an `IsReactorRunning => unlimited heat` shortcut or
quietly call electricity "raw fusion".

### Electromagnetic route: viable later alternative

An induction heater uses AC electricity and a changing magnetic field to heat
conductive material. A static magnet alone is not a furnace. Heating, stirring
and levitation are related applications with different requirements. Consarc's
VIM equipment establishes electrical induction inside a vacuum chamber as a
real industrial process; ESA's electromagnetic levitator establishes heating and
positioning small conductive samples in microgravity. The latter is research
equipment, not evidence for unattended tonnes of mixed scrap in zero gravity.
[Consarc process](https://consarc.com/wp-content/uploads/sites/9/2019/05/Vacuum-Induction-Melting-Furnace-guide-en.pdf),
[ESA MSL-EML](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/International_Space_Station/Material_Science_Laboratory_Electromagnetic_Levitator_MSL-EML).

Use the same process model with a different energy provider and suitable hardware.
Draw real native electricity, capture partial delivery during brownouts, and
apply conversion losses once. This would be easier to connect than raw fusion,
but remains the owner's **later option**, not the selected first implementation.
Avoid a dependency on Auto Nav merely for industrial controls.

## Heat, containment and material accounting

Model a small number of thermal stores: **charge, hearth/lining and cooling
assembly**. Model phase change as energy, not simply "temperature exceeded a
threshold, therefore finished". A suitable minimum accounting identity is:

```text
source energy = delivered process energy + coupling losses
delivered process energy + auxiliary heat
  = change in stored thermal energy + rejected/transferred heat
    + energy carried out in explicitly transferred material

input material + consumed additions
  = cast product + retained heel + captured contamination/waste + released mass
```

Implementation may use enthalpy tables or a documented piecewise approximation:
solid heat capacity, latent heat, then liquid heat capacity. Recipes own the
material parameters and operating envelope. Use kW for power, kWh or MJ for
energy, kg for charge, kelvin internally and Celsius for the operator; pressure
must carry an explicit unit. Never turn native MeV readings into Celsius by
renaming the scale. Freeze recipe parameters for an active batch when settings
change, following existing versioned-job policy.

**Cooling is the practical bottleneck.** Insulation limits room leakage; a finite
cooling store and connected external radiator reject the remaining heat. In
vacuum, exposure to space is not automatic rapid cooling. Radiation depends on
area, temperature, surface properties and what the surface faces. NASA documents
this energy balance and both heat transport and radiator mechanisms.
[NASA thermal control](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/).

For an initial implementation, choose a declared, bounded radiator abstraction
with a real installed exterior sink, capacity and temperature limits. Do not
require a complete planetary illumination simulator before a useful furnace,
but document any simplified external heat load. Return enclosure leakage to
native room heat only when that transfer is supported; retain untransferred heat
instead of dividing by zero gas mass. Missing or obstructed cooling must reduce
throughput or stop heating. A cold reservoir only buys time; it is not disposal.

Keep hot contents captive in a sealed cartridge/mould assembly. Earth-style
tilting and gravity pouring do not automatically work in microgravity. For the
first machine, melting and solidification within a captive casting cassette
avoid an open molten stream; active ejection occurs only after cooling. A later
pressure-fed or electromagnetic transfer mechanism needs its own design.

The first useful recipe should treat **identified, compatible metal feed**.
Generic steel scrap does not prove an exact alloy assay. Preserve ordinary scrap
as ordinary scrap unless a recipe explicitly accepts that game's abstraction;
never advertise aerospace purity from unknown junk. Melting mixed metals does
not sort them, and ore reduction needs more than heating. Keep asteroid mining,
ore chemistry and chemical-fluid extensions on their separate research paths.

**Product requirement before coding:** pair the first casting with an actual
consumer, such as a new cast structural housing or repair blank for our equipment,
including any finishing/tools it requires. Existing recipes already accept
scrap; do not impose a new furnace tax on them. A casting blank is not a completed
bearing, motor or circuit board. Final output IDs, bills, yields, sell values and
repair use need a concrete recipe audit. Reject a first release that merely makes
decorative ingots. Retain recoverable waste; repeated remelting cannot create
material or turn rejects back into fresh high-yield feed.

## Process state and meaningful controls

Proposed sequence:

```text
IDLE -> LOAD/VERIFY -> SEAL -> EVACUATE -> PREHEAT -> MELT
     -> HOLD/CONDITION -> SOLIDIFY IN MOULD -> COOL -> RELEASE
                         | interruption |
                         v              v
                   HOLD IF SUPPLIED / CONTROLLED COOLDOWN / FAULT
```

Loading, seals, heat source, cooling and output readiness form the visible startup
checklist. AUTO advances a chosen recipe through those same states. STEP lets the
operator command eligible transitions and adjust bounded settings. Neither mode
bypasses interlocks. Saved presets provide routine convenience after the player
has enjoyed learning the machine. Real furnace controls also use interlocked
automatic sequences and editable melt recipes; manual involvement is not needed
just to make the equipment feel industrial.
[Consarc controls](https://consarc.com/wp-content/uploads/sites/9/2019/05/Vacuum-Precision-Investment-Casting-Furnace-en.pdf).

The following is a **target control contract**, not existing UI functionality:

| Instrument / control | Real effect and feedback | Delivery scope |
| --- | --- | --- |
| OFF / STANDBY / RUN rotary | Requests operating mode; shows actual state separately. OFF isolates heating; hot contents still cool. | First furnace |
| AUTO / STEP selector | Chooses sequencing authority, not a different physics model. Current step and next prerequisite remain visible. | First furnace |
| Batch selector and mass display | Selects compatible feed/recipe; displays admitted mass, retained material and free capacity. Captured recipe cannot be swapped halfway through a melt. | First furnace |
| SEAL / RELEASE controls | Lock the captive cassette; release only below temperature and pressure limits. Show requested versus actual latch state. | First furnace |
| Vacuum pump switch and pressure target | Changes evacuation demand and achievable chamber pressure over time, including leaks/outgassing and destination backpressure. Chamber kPa/mbar is separate from room pressure. | First vacuum-only recipe, with a finite gas destination |
| Heat-enable guarded switch | Opens the furnace's energy request only after readiness checks; does not ignite the reactor. | First furnace |
| Power-limit slider | Caps requested heat; LED columns show REQUEST / RECEIVED / COOLING rather than three copies of a percentage. | First furnace |
| Temperature and ramp-rate dials | Set recipe-bounded temperature and warm-up rate; affect time, required energy and lining stress. Actual and target are both displayed. | First furnace |
| Hold-time dial and countdown | Runs only while actual conditions meet the recipe's band. Interrupted holding has explicit rules, not free elapsed progress. | First furnace |
| Cooling AUTO / LOW / HIGH | Controls bounded heat transfer and pump demand; shows sink temperature and remaining cooling margin. Protective minimum cooling cannot be switched off through the normal panel while hot. | First furnace |
| CAST / COOL / RELEASE progression | Verifies mould fit/space, solidifies in the captive cassette and only then releases physical output. An unavailable destination retains the product. | First furnace |
| Controlled stop and guarded emergency isolate | Stop requests cooldown; emergency isolate cuts furnace energy/feed while preserving available protective cooling. Neither deletes a hot batch or commands reactor shutdown. | First furnace |
| Alarm acknowledge, lamp test, service view | Acknowledge silences a latched warning, not its cause. Lamp test explicitly shows TEST without reporting false process readiness. Service shows wear and replacement requirements. | First furnace |
| Gas species / purge / backfill controls | Consume a compatible supply and send displaced gas to an explicit receiver; contamination and pressure have consequences. Nitrogen is not universally inert for every alloy. | Later, with actual fluid/gas services |
| Electromagnetic stirring / coil diagnostics | Alters mixing within supported recipes and electrical/thermal demand. Coil current/frequency display requires an actual model. | Electromagnetic hardware or a separately justified stirrer |
| Flux/reagent dosing and return selection | Changes only chemical recipes that specify additions; tracks spent material and emissions. | Later chemical round |

Detailed future controls should be absent or clearly marked unavailable until
their hardware and behaviour exist. Do not show simulated oxygen ppm, purity,
coolant litres/minute, coil current or leak rate unless the implementation owns
a defensible quantity behind that reading. A target is a command, not a direct
edit to the chamber's actual environment.

## Panel composition and text mockup

The reference succeeds through **grouping and repetition**: flat modular plates,
narrow seams and fasteners, black instrument wells, matching LED segments,
restrained highlights, consistent red switches and sparse hazard stripes.
Prefer isolated native widgets or their runtime-referenced artwork, with original
art only for documented gaps. Do not distribute a cropped
reactor screenshot or reproduce its labels blindly. The furnace has no reason
to display laser pellet alignment or a fusion ignition sequence of its own.

Use an expanded **Furnace** detail page on C1, also opened by the local Control
Panel action. Give the instrument view the available main panel area; do not
compress it into the small navigation-module slot. Keep the selected machine,
host ship, operating phase and stop control fixed. Preserve C1's machine list
and Attention view on return; larger fleets do not need a wall of tiny gauges.

```text
PHOBOS' RIVETLINE F6 FUSION FURNACE        <host / machine>   [Overview] [Close]
+------------------+--------------------------+----------------------------+
| SEQUENCE         | HEAT DELIVERY            | CHAMBER                    |
| [ ] Load checked | REQUEST  RECEIVED  REJECT | ACTUAL ---- C  TARGET ---- |
| [ ] Cassette shut|   ||        ||       ||  | TEMP  [||||||||.......]    |
| [ ] Cooling ready|   ||        ||       ||  | PRESS ---- kPa [||||....]  |
| [ ] Vacuum ready | ---- kW  ---- kW ---- kW | Ramp (dial) Temp (dial)    |
| [ ] Source ready | Power limit [slider]     | [Pump] [Seal]              |
+------------------+--------------------------+----------------------------+
| OPERATION        | CHARGE / CASTING         | COOLING / CONDITION        |
| Mode (rotary)    | Recipe: <selected>       | Sink ---- C  Margin ----   |
| AUTO / STEP      | Charge -- kg / -- kg     | [||||||||.............]    |
| [HEAT ENABLE]    | Phase: <current step>    | Cooling (rotary)           |
| [Next step]      | Hold --:-- / --:--       | Lining [|||||||.......]    |
| [Start cycle]    | [Hold dial] [Cool]       | [Alarms] [Service]         |
+------------------+--------------------------+----------------------------+
| <highest-priority reason + next useful action> [Stop & cool] [ISOLATE]     |
+------------------------------------------------------------------------+
[Process] [Connections] [History]   Later hardware adds applicable sections
```

All dashes above mean **no sample telemetry asserted**. REJECT means heat
rejected by the cooling system; label it in full or explain it on focus.
At smaller sizes, reflow into the same three functional pages with independently
scrollable details, not shrunken unreadable text. Keep critical alarms and stop
actions outside scrolling areas. Use numbers/units and state words alongside
colour; include explicit missing/stale-sensor states rather than a reassuring zero.

Let dials support bounded detents, drag, wheel and focused keys, with a precise
numeric entry alternative for setpoints. A wheel over a scrolling page must not
silently change an unfocused knob. Programmatic widget refresh must suppress
command callbacks. Viewing the panel never advances the process or writes a save.
Short explanations should connect a block to its cause, e.g. **Cooling reserve
full — heating isolated; batch retained**, with details available separately.

### Artwork brief after the contracts are settled

The [25 September asset matrix](furnace-ui-and-art.md#original-artwork-remaining)
now governs production. The earlier list below identifies visual needs, not a
requirement to redraw vanilla controls.

- Suitable native modular frame/fastener artwork referenced at runtime; a small
  original frame only if native parts cannot fill a documented layout need.
- Reuse vanilla LED cells, lamps, guarded switches, rotary states, slider parts
  and digit artwork where suitable. Keep values, localized labels and state live;
  the audit records initialization hazards and required adapters.
- Optional startup lamp sweep and restrained audio clicks. Identify the sweep
  as a test; it cannot report readiness. Respect reduced flashing preferences.
- A world sprite with a strong central sealed vessel silhouette, loading and
  cooling interfaces, limited hazard accents and distinct damage states. Keep
  separately installed conduit and heat/cooling attachments visually separate.
- For the proposed 6 x 6 footprint: a 96 x 96 native world derivative at the
  inspected 16 pixels/tile scale, with a master at least 192 x 192. A 1440 x 720
  panel design reference would need at least a 2880 x 1440 master; actual viewport
  fit must be checked before choosing that reference. A 16 x 8 LED cell needs at
  least 64 x 32 source artwork under the 4x small-asset rule. Preserve crisp pixel
  clusters, matching normals/damage alignment and runtime layout bounds.
- Localize complete labels/messages through catalogs. Keep original brand/model
  handling in Framework; content owns the proposed F6 designation. No hard-coded
  English baked into gauges or needlessly duplicated decorative text.

## Framework and Shipbreaker responsibilities

| Reuse now | Add only for this concrete implementation | Keep in Shipbreaker |
| --- | --- | --- |
| Definition registration, construction, maintenance, merchant stock, translations and branded names | Thermal energy receipts/units and shared source allocation when the reactor adapter is implemented | Furnace/coupler/radiator definitions, dimensions, graphics and economic balance |
| Versioned `ObjectStateStore` and protected unknown/corrupt records | A bounded thermal-state helper if furnace and sink share that need; explicit resource identity and conservation | Material properties, allowed recipes, phase rules, lining wear and hazard thresholds |
| Solid-item `PortPairing`, checked transfers and immutable processing contracts | Heat/gas endpoint compatibility and actual transport checks; existing solid transfers are not fluid or energy pipes | Reactor-specific eligibility and native hook details until a second provider justifies extraction |
| `ConsoleBinding`, typed views, service dispatch and clipped panel widgets | Small isolated-native-widget adapters; existing Phobos controls as a diagnosed fallback, with no gameplay logic in either | Furnace layout, control ranges, recipe presets and furnace-specific messages |

If a shared reactor-control lease is needed, Framework can hold the small
arbitration primitive used by both consumers; flight policy stays in Auto Nav
and process policy in Shipbreaker. Do not make Shipbreaker require Auto Nav, or
add a generic energy-network plugin with no working machinery consumer.

The proposed chamber is a **bounded internal process volume**, not a second
simulation of every ship room. Use native gas amounts/species and damage where
appropriate after checking their mass and persistence behaviour. Pumping needs
an actual finite receiver and measured transfer; a hard-coded pressure decrease
with disappearing gas is not sufficient. Prefer a vacuum-only first recipe and
integrated receiver over implementing the whole chemical silo roadmap at once.
Custom gases, chemical service terminals and solvent recovery remain separate
[future work](chemical-storage-and-process-fluids.md).

## Interruption, hazards and ordinary saves

Persist source/sink IDs, batch identity and captured recipe, phase, actual
enthalpies/phase fraction, chamber contents, waste, wear and control requests.
Cross-ship control checks must refresh at each command and transfer; docking,
towing or ownership changes cannot silently bind a neighbour's reactor or sink.

On reload, retain physical contents and temperatures, invalidate old energy
receipts, and pause permission for further industrial heating/feed. Passive
cooling and locally available protective functions must follow an explicitly
defined simulation-time policy; **pause-on-reload must not freeze a dangerously
hot object forever or magically make it cold**. Decide unloaded-ship catch-up
before implementation. No wall-clock heat accrual while the game is closed, no
unbounded catch-up loops and no replayed pump transfers.

A blocked product destination retains the cooled casting. A power cut or missing
source stops energy input but leaves stored heat. Unknown recipe/state versions
remain protected; removing a required provider does not become supported save
recovery. Preserve the current D4/R4 recipes, residue contracts and independent
pause-on-reload behaviour.

Make faults conditional on actual heat, contents and failed containment:
overtemperature damages lining, loss of cooling can force a stop, a leak changes
chamber contents, and a breach releases only material actually present. Native
fire/exposure integration needs a verified pathway. Do not implement a generic
"furnace explodes and creates toxic gases" dice roll. A cooling fault need not
explode a properly isolated machine. More elaborate reactive-charge hazards
belong with the chemical research, not the first clean-metal recipe.

## Practical implementation sequence

1. **Select the first useful casting and intended installation.** Confirm its
   feed/output contract, downstream consumer, 6 x 6/50 kg proposal or replacement,
   coupler attachment and exterior cooling footprint. These choices shape art.
2. **Implement one complete fusion-fed batch path**, resolving native energy
   allocation alongside charge/lining heat, finite cooling, contained gas and
   saved phases. Preserve the real output debit and flight handover. If the
   reactor coupling cannot meet that contract with a maintainable patch, stop
   and revise that part explicitly rather than layering on decorative controls.
3. **Build the instrument view on that service**, using a text/geometry layout
   first, then suitable native widgets and original machine artwork. Local, C1 and F3 controls all use
   the same checked actions. Include diagnosis of source, heat receipt, chamber,
   cooling and output blocks; console commands do not bypass them.
4. **Package the complete equipment** with construction, merchants, maintenance,
   material-balanced dismantling that loses value against selling whole hardware,
   translation catalogs and the usual installer. No runtime version bump or
   installation is needed for this research-only round.
5. Add protective gas, electromagnetic heating/stirring or chemical refining as
   separate useful expansions. Do not expose a forest of inactive switches just
   to resemble the reference on day one.

Focused checks should cover partial energy delivery, multiple consumers, exact
unit conversion, source debits, flight preemption, time acceleration, sensible
and latent heat, finite cooling, pump gas conservation, blocked output, emergency
isolation, hot save/reload, unavailable/unknown providers and ship-bound access.
UI checks should cover readable gauges, translation expansion, input while
paused, wheel/scroll conflicts and panel dismissal. The owner handles in-game
evaluation; established basic native power behaviour needs no separate proof test.

**No further screenshot is required to proceed with design.** This reference is
enough for the panel language. Later, a furnace placement view and one panel view
at the owner's normal UI scale will be useful for actual fit/readability checks.

## Evidence, attribution and limits

- Native assembly SHA-256 rechecked in this round:
  `91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
  Inspection covered `GUIReactor`, `GUIKnob`, `GUILedMeter`, `FusionIC`,
  `Powered`, `JsonPowerInfo` and native `guipropmaps/guipropmaps.json` and
  `powerinfos/powerinfos.json`. Decompiled material remains in ignored local
  research. The game/reactor design belongs to Blue Bottle Games; no native art
  or source is redistributed here.
- Repository precedents: [industrial console](industrial-control-console.md),
  [Polaris instruments](auto-nav-instruments.md), [torch integration](auto-nav-torch.md),
  [reclaimer](scrap-reclaimer.md), [saved jobs](processing-job-compatibility.md),
  [material pairing](material-port-pairing.md), [dependency policy](dependency-contingencies.md),
  [equipment names](equipment-branding.md), [art resolution](artwork-resolution-policy.md).
- External sources linked at the relevant claims were accessed for this round.
  ITER, NASA, ESA, Royce and Consarc provide engineering precedents, not evidence
  of game integration or permission to reproduce their artwork. No upstream mod
  source is needed or copied for this proposed panel.
- Native GUI component contracts and the existing Phobos integration are
  inspected facts. Furnace dimensions, coupling physics, numerical thermal
  model, hazards and gameplay balance are proposals. Raw-energy accounting,
  radiator integration, chamber gas delivery and in-game presentation remain
  implementation work, not verified capabilities.
