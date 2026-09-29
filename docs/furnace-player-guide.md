# F6 electric furnace: operating guide

The F6 turns twenty 1 kg pieces of scrap into castings: a rough aluminium
machinery housing, four aluminium ingots, or four steel ingots (since 0.38.0),
plus a little waste. It needs ship power, room air and somewhere to dump the
heat. Leave space to load it and collect the finished casting.

For the current packages and requirements, use the [player guide](player-guide.md)
and [installation guide](installing-mods.md). The [item reference](shipbreaker-item-reference.md)
lists prices, suppliers, construction bills and maintenance materials. These are
development builds; the full casting cycle still needs owner playtesting.

## Get the furnace aboard

- **Starting with sections:** obtain three F6-S sections, 80 kg each. Choose
  **Assembly information** for the bill, then **Install** on a section or the
  furnace in **INSTALL > APPS**. Place the outline on suitable flooring. Crew
  bring the sections separately; final assembly needs Mortorq and soldering tools.
  Tables still make individual sections. They no longer assemble the whole F6.
- **Starting with a complete loose furnace:** use its own **Install** action.
  You do not need to break it into sections first.
- Choose one cooling assembly below. Buy it or build it at a supported table,
  install it, then pair it with the furnace. A powered furnace with no usable
  cooling will not heat a batch.

The site keeps delivered parts and progress after saving. Cancel the site's
construction order to release its delivered parts. For hauling, old table orders
and the D4/R4 equivalents, see [assembly and maintenance](section-assembly-and-maintenance.md).

## Installation: choose one cooling assembly

**Phobos' Rivetline F6 Electric Furnace** occupies **6 x 6 tiles**, weighs 240 kg
empty and is rated for a 50 kg charge. The first supported recipe uses exactly
20 kg; the rating does not enable arbitrary alloys or larger recipes. Three 80 kg sections make one complete furnace.

**Phobos' Rivetline F6-R Exterior Radiator** is separate **6 x 4**, 100 kg
equipment. Both can appear in the existing industrial/fixer/scrap stock routes,
and both can be installed, repaired, restored and dismantled. Check the
[item reference](shipbreaker-item-reference.md) for their material bills.

### Exterior radiator

Use this local layout, rotated together as necessary:

```text
           SPACE / local +Y
        R R R R R R     radiator: 6 wide x 4 deep
        R R R R R R
        R R R R R R
        R R R R R R
        # # # # # #     six intact hull walls; retain these
        F F F F F F     furnace: 6 wide x 6 deep
        F F F F F F
        F F F F F F
        F F F F F F
        F F F F F F
        F F F F F F
         operator aisle / local -Y
```

The machines face the same direction; their centres are **six tiles apart**.
The radiator needs all six rear wall supports and no floor/wall through its fin
footprint. Furnace placement requires flooring. Choose the cooling assembly in **Control Panel** and apply the selection;
pairing requires a player-owned ship. F3 commands use full object IDs. The pair is reciprocal
and saved. Moving hardware or breaking the geometry removes usable cooling.
An intact disconnected radiator continues radiating its own stored heat.

### Optional F6-P thermal exhaust port

**Phobos' Rivetline F6-P Thermal Exhaust Port** is a **1 x 1 mounting head beside
the furnace**, supplied with a complete **100 kg underside radiator assembly**.
Its base value, construction, installation and maintenance costs equal the F6-R.
The visible head represents a sealed through-deck connection; its **12 m² effective
underside radiating area is a simplified equipment model**, not a second deck
or a new 3D clearance simulation. It consumes no vented cabin air or coolant.

Keep an **intact installed sealed floor** beneath the port. Its floor remains the
pressure barrier; never remove it to make an exhaust hole. Damaged/EVA
flooring, missing floors and wall tiles are rejected. Place the port at one of the
two side sockets, facing the same direction as the furnace. Furnace-local centre
offsets are **(-3.5, +0.5)** or **(+3.5, +0.5)** tiles; rotate the layout together.

```text
        F F F F F F
        F F F F F F
      P F F F F F F P     choose ONE side port
        F F F F F F
        F F F F F F
        F F F F F F
         front operator aisle
```

The port occupies its own adjacent floor tile, outside the unchanged 6 x 6
furnace footprint. Its local panel and C1 show the selected assembly's full name,
ID and mounting status. The existing pair command accepts either endpoint type.

A furnace has exactly **one** selected cooling assembly. To change installation,
cool both devices to **50 C or less**, return gas, release the charge, empty both
feed and product inventories, drain any serviceable coolant into waste, then
choose **Disconnect cooling pair** and pair the alternative. Remove the drained
waste from Products before removal or dismantling. A second
assembly cannot be added to combine capacities, nor shared with another furnace.
An unavailable connection retains its saved identity and physical heat. Old F6-R
links and hot saves need no conversion; adding this update does not replace them.

### Optional pipes and serviceable coolant

For an F6-R farther away, lay [F6-C coolant conduits](furnace-coolant-conduits.md)
and choose the left or right piped-cooling fitting before pairing. F6-P stays
beside its furnace; it does not accept a remote pipe connection.

The original sealed cooling mode needs no coolant items. **Serviceable coolant
is optional and only works with piped F6-R cooling.** To use it, stand beside a
cool, idle, paused furnace, enable finite coolant servicing and place separate
1 kg Thermal Service Fluid Charges in **Products**. Load them one at a time;
six charges fill the 6 kg circuit. Do not put them in the aluminium Feed chamber
or inside the cooling unit. See [coolant filling, leaks and draining](fluid-network-operations.md#optional-finite-furnace-coolant)
for route requirements and recovery of caught leakage.

Fill, drain and coolant-mode changes require local access. C1 cannot perform
those physical service steps. Drain retained fluid before unpairing, changing
cooling mode or removing equipment; leave room in Products for the waste.

### Power and heat

Connect the F6's two front power points to ordinary ship electrical supply.
There is no reactor-side coupler, fuel debit or free reactor-running heat.
At the full heating rating, direct coupling requests approximately **279.8 kW**:
250 kW useful heat at 90% efficiency plus 2 kW process auxiliaries. Piped cooling
adds up to **1 kW** for circulation, giving approximately **280.8 kW** during
full-rate heating. Actual demand is bounded by the selected process settings,
thermal need and cooling-store headroom; these are ratings, not continuous loads.

At rest, a connected direct installation requests **50 W** for instruments.
An eligible piped installation additionally requests up to **1 kW** for its pump,
including while heating is paused. Active process cooling auxiliaries can request
up to **1 kW**, separately from that circulation pump. Cold automatic receiving
adds the configured feed motor demand (**2 kW** by default); it cannot run during
a sealed casting cycle. All received auxiliary, pump and motor electricity is
accounted as heat in the finite cooling store. See
[material-routing power accounting](furnace-material-routing.md#power-heat-and-backpressure).

Direct F6-R and F6-P coupling retains a passive furnace-to-sink path without
power. **Piped circulation requires measured pump electricity**; it has no
unpowered thermosiphon. On power loss, the radiator can still reject heat already
in its own store, and the furnace can still leak bounded heat to an accepting
room, but new heat does not circulate from the furnace through the pipes.
At 700 C with a 25 C cabin, insulation leakage is approximately **675 W**; the
250 kW process load is stored in the charge/lining and later rejected through the
selected cooling assembly. Conversion losses also enter that finite store.

## First casting

1. Open **Control Panel**, choose one cooling assembly and pair it. Allow a powered
   instrument update. If the instrument artwork is unavailable, use the fallback controls.
2. Open **Feed** locally. Insert **twenty separate, unstacked native Scrap
   Aluminum items**, each 1 kg and carrying nothing. Other identities, including
   finished housings and historic residue, are not accepted. Native auto-stacking
   is disabled only for items entering or leaving the F6 charge bin. Pick a
   stack up in the inventory window and right-click on the bin to place one
   piece at a time, or switch on **Load feed by crew** on the furnace so crew
   bring single pieces from anywhere aboard; the hot steps below still need
   the hazardous permission or a repeat run.
3. **Seal and verify charge**. The cool lining, exact feed, room atmosphere and
   finite receiver are checked. The feed chamber closes and holds 80 litres of cabin gas.
4. **Enable / resume sequence**. AUTO evacuates, preheats, melts and holds at
   700 C for 60 continuous seconds with the required pressure and working probes, then solidifies in its captive mould
   and cools. STEP waits at the operating transitions; use **Advance next step**.
5. At or below **50 C**, return chamber and receiver gas to the adjacent room.
   An absent accepting gas volume or excessive resulting room temperature blocks
   this operation. After compartment reconstruction, return gas locally beside the
   furnace on the same ship; C1 retains the original room scope. The 50 litre
   receiver is emptied for reuse.
6. **Release cool charge** with room for both products. A successful cycle yields
   one **19 kg rough housing** and **1 kg terminal melt remainder**. An incomplete
   cycle returns the original charge after the same cooling/gas-return requirements.

Finish the rough housing at a Bar/Dining Table, using Mortorq and welding tools:
**19 kg -> 18 kg finished housing + 1 kg native aluminium offcut**, 600 native
work-progress seconds. The optional D4 and R4 section recipes each consume one
housing, retaining their mechanisms, electronics and other material inputs.
The remainder has no recycling route. Nothing reclassifies historic residue.

| Optional section | Other ingredients | Output | Native work target |
|---|---|---|---|
| D4-S | 50 steel, 6 aluminium, 10 small mechanisms, 2 small electronics | 80 kg section | 2,400 s |
| R4-S | 56 steel, 8 aluminium, 12 small mechanisms, 4 small electronics | 90 kg section | 3,000 s |

Steel/aluminium units weigh 1 kg; small mechanisms/electronics weigh 0.5 kg.
These are labour-saving alternative construction routes, not precision parts or
native repair-recipe replacements. The 240 kg furnace and 100 kg radiator remain
substantial infrastructure for sustained independent maintenance.

## Recipes: housings and ingots

Since 0.38.0 the furnace has three recipes. Choose the **next charge** on the
panel, the C1 or with F3 `phobosfurnace recipe <furnace-id> <recipe>` while
the furnace is idle and cool, its chamber is empty and no repeat run is on. A
batch already sealed stays bound to the recipe it was sealed with, and a saved
batch from before 0.38.0 finishes as a housing.

| Recipe | Charge | Released | Cycle |
|---|---|---|---|
| `housing` | twenty 1 kg aluminium scraps | one 19 kg rough housing, one 1 kg aluminium melt remainder | the original 700 C aluminium cycle |
| `aluminium-ingots` | twenty 1 kg aluminium scraps | four 4 kg aluminium ingots, three 1 kg aluminium scrap gates, one 1 kg aluminium melt remainder | the same aluminium cycle |
| `steel-ingots` | twenty 1 kg steel scraps | four 4 kg steel ingots, three 1 kg steel scrap gates, one 1 kg steel melt remainder | hotter and longer: melts at 1538 C, holds at 1578 C |

The chamber takes only the metal the selected recipe names; the game-level rule
admits aluminium and steel, and the exact rule refuses the other metal with the
reason. Every recipe conserves the 20 kg charge. Ingots are one-cell stock that
stacks to ten, trade as metals, and are the raw stock the coming Phobos
Manufacturing machining will consume; a table can cut one back into four scraps
(`Recover aluminium from ingot`, `Recover steel from ingot`, 300 work seconds),
which loses value. Single ingots also turn up now and then in engineering salvage. The steel remainder, like the aluminium one, has no recipe.

A steel charge runs through the same lining, sink, radiator and 250 kW rating.
It needs well over half again the energy of an aluminium charge, so it takes
longer to reach temperature and longer to cool; the pressure interlock scales
with the hotter target so the gas the pump already left behind does not refuse
heating. Offline checks run a full steel cycle to completion and cooling through
the existing hardware; the real duration on your ship depends on the power you
supply and on the radiator. The steel numbers (melting point 1811 K, enthalpy of
fusion 13.81 kJ/mol) are iron data from the
[NIST Chemistry WebBook, iron](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7439896&Mask=4)
(National Institute of Standards and Technology); the superheat, the averaged heat
capacities and the treatment of the game's steel scrap as iron are our
simplification, not measured properties of an alloy. No endorsement is implied.

## Repeat batches

**Repeat batches** keeps the F6 casting without a crew trip for every step. Start
it from the local panel, C1 or F3 once the furnace is paired with a working cooling
assembly on your own ship. For each full charge of twenty pieces of the selected
metal, the run
seals, runs the AUTO sequence, returns chamber and receiver gas to the recorded room
at or below 50 C, and releases the products. It then turns receiving back on for a
paired R4 aluminium output, so the next charge can arrive. Without a paired R4, load
aluminium by hand or with a crew order; the run seals once twenty pieces are in.

The run records the furnace's ship, room and cooling assembly when you start it.
It stops acting, and waits for **Repeat batches** again, when:

- the ship manoeuvres or demands torch thrust, including autonomous G4 transits;
- power, instruments or cooling fail, or heating stops before the batch finishes;
- you or C1 use a manual furnace control, such as Stop, Seal, Resume, Equalize or Release;
- the room, cooling assembly or ship ownership changes, or a fault occurs;
- the game is loaded.

A pause only withdraws heating permission. Heat, gas, charge and progress stay where
they are, and passive cooling continues. Choosing **Repeat batches** on an interrupted
batch is your explicit Resume of that batch. The run never re-arms heating by itself
and never melts a partial charge: a final charge of fewer than twenty pieces stays
cold in the bin. **End repeat run** stops the automation and leaves the current batch
for the normal controls. The status line counts batches released during the run.

Products still need somewhere to go. Release waits while the product tray is full;
pair the F6 output to a collector with **Released furnace products only**, or let crew
clear it. Crew F6 standing orders keep bringing aluminium and clearing products during
a repeat run, and leave sealing, heating, gas return and release to it. Seal, gas return
and release happen during ordinary play, not inside a managed time-skip; heating already
under way still follows the power actually supplied.

## Process and interruptions

| State | Requirement / transition | Retained when interrupted |
|---|---|---|
| Load / verify | Cool idle machine, twenty exact physical inputs | Lining/radiator energy from earlier work |
| Sealed | Captured gas fits receiver; explicit Resume | Locked charge, gas species and energy |
| Evacuating | 0.05 mol/s maximum; target 0.1 kPa; 200 kPa receiver limit; 180 s timeout | Both finite gas parcels; no room/vacuum vent shortcut |
| Preheat | Ramp 0.1–5 K/s, delivered heat 1–250 kW, finite cooling headroom | Accounted sensible energy |
| Melt | 660.3 C latent plateau | Melt fraction through enthalpy |
| Hold | 700 C ±0.5 C, chamber ≤0.5 kPa, valid probes, 60 continuous seconds | Heat remains; interruption resets uncompleted hold |
| Solidify | Qualified product or aborted charge stays captive | Latent heat must still leave |
| Cool | Connected finite sink and accepting room leakage | Direct coupling can transfer heat passively; piped transfer requires powered circulation. Sink radiation and bounded room leakage remain independent. |
| Gas return | ≤50 C, same ship, authorized adjacent room, accepting gas | Receiver stays full until return succeeds |
| Release | All outputs fit; residual charge heat fits radiator | Blocked output leaves inputs intact |

The 30 kJ/K lining and 80 kJ/K cooling assembly are separate stores. The radiator
uses a 12 m² effective area, emissivity 0.85 and a 200 K background; its 250 C
operating limit is checked before demand. Furnace-to-sink transfer is bounded by
100 kW and 0.30 kW/K; direct coupling is passive, while piped transfer is also
bounded by actual pump power and, if enabled, serviced-coolant flow. Room leakage uses 1 W/K with actual native gas heat capacity and
a 60 C accepting-room ceiling. Vacuum supplies no convective cooling. These are
authored lumped thermal parameters, not measurements of vanilla equipment.

Charge sensible heat remaining at release moves into the finite radiator store;
released shop stock has no separate invisible hot-item state. Chamber and
receiver sensible energy moves back into native room gas. Heat follows the electricity actually supplied, including partial power.

Power loss, failed instruments, missing cooling and native flight commands pause
heating. Torch demand and RCS manoeuvre commands pre-empt the furnace; the furnace
does not alter flight controls or upstream fuel accounting. Resume is explicit.
The permanent **STOP / ISOLATE HEAT** button remains below the scrolling controls.

Loading keeps the heat, gas, selected input pieces, casting progress and settings. Reload always pauses heating and resets
an unfinished continuous hold. Passive simulation advances observed intervals up
to 60 seconds, substepped at 0.25 seconds. Longer or unloaded intervals retain
heat conservatively, reset the clock and require Resume; wall-clock time away
does not complete a batch. Very large fast-forward steps can therefore pause it.

Uninstallation, dismantling and disconnection require a cool, empty furnace.
Repair/Restore requires cool hardware, but may service a still-sealed cold charge;
this allows a failed probe to be repaired before gas return. Native damage mode
switches retain persistent maps and cargo; damaged probes display **Unknown**.
The cooling assembly's local thermometer is passive. Probe failure alone does not erase heat or disable otherwise available cooling.
Either damaged cooling assembly retains 25% of its nominal radiating area while
its rejection path remains available; heating is blocked until repaired. An
actually damaged furnace cannot operate its circulation pump. Piped installations
then retain heat until the remaining radiation/room paths or restored circulation
can remove it. Removing or damaging the port's supporting floor stops its
modeled heat rejection and disconnects the furnace; both thermal stores remain. Losing its supporting wall disconnects
the furnace while an exposed fin bank still rejects its own heat. Absolute destruction remains the
game's destructive machinery path; this release does not add explosions, rupture
recovery or a new atmosphere/hazard simulation.

If gas return or product release is interrupted and cannot be confirmed, the
furnace locks for inspection instead of retrying. Keep the log, save and cargo;
do not erase the fault record to force another attempt. Recovery after a crash
is not guaranteed.

## Missing actions and stopped work

Choose **Maintenance information** on the equipment to see what is blocking
removal. Check both paired machines: a cool furnace cannot be removed while its
cooling assembly is still unsafe. Empty Feed and Products, finish or release the
batch, let both machines cool and drain serviceable coolant as required.

Older F6-P and F6-R units may contain cargo that their old menus hid. Stand beside
the unit and choose **Recover stored cargo**, then move the items out through the
inventory window. New deposits are blocked. Real work reservations, unsafe heat
or a saved-operation fault can still block recovery; finish or cancel the
relevant work first. Recovery is also available on loose or damaged cooling units
when safe. See [cargo recovery](section-assembly-and-maintenance.md#recover-cargo-from-older-cooling-units).

**Repair** fixes a broken form; **Restore** treats ordinary wear. Restore is absent
when there is no wear to repair. Missing tools, materials or access can also hide
work. A missing action is not permission to use Bash as an uninstall shortcut.

## Controls and command reference

Local panels and C1 offer the same operating controls. Stand beside the furnace
to open Feed or Products or service its coolant. Use the scroll area to reach
more controls; **STOP / ISOLATE HEAT** stays below it. Enter decimal values with
a point. Sliders and fields are drafts until you choose **Apply**; **Discard**
keeps the existing settings. Opening the panel does not start a job.

If a gauge is blank or out of range, read the adjacent text and stop reason.
Buttons and number fields remain available when instrument artwork cannot load.
See [panel controls](control-panel-guide.md) for the shared layout and
[the instrument implementation record](development/furnace-connections-and-instruments.md)
for developer details about reused game widgets and their checks.

F3 entry points:

```text
phobosfurnace help
phobosfurnace list
phobosfurnace controls <full-furnace-id>
phobosfurnace pair <full-furnace-id> <full-cooling-id>
phobosfurnace status <full-furnace-id>
phobosfurnace stop <full-furnace-id>
phobosfurnace repeat <full-furnace-id>
phobosfurnace repeat-stop <full-furnace-id>
phobosfurnace recipe <full-furnace-id> <housing|aluminium-ingots|steel-ingots>
```

C1 also accepts furnace actions through `phobosindustry <action> <console-id>
<furnace-id> [value]`, using the same access checks.

The installation key rotates with the F6 and highlights its valid selected
attachment. Named attachment points follow the vanilla reactor pattern without
changing existing offsets. The side-port coupling faces inward automatically
when the saved pair is physically valid. Wrong rotation, wrong socket and missing
support have separate messages. Painted pipe details are not a routable network.

## Owner review on return

- After closing the game, install the intended prepared packages using the shared
  installer. Verify their files/load order, then compare the loaded versions with
  the [current player guide](player-guide.md); do not use the historical feature
  versions above as installation targets.
- Check both cooling installations in all rotations, including both port side
  sockets, visible alignment and collision bounds. Review the port at normal game
  scale, including damage tint. Verify cabin pressure is unaffected by installation.
- Pair, load and seal one batch. Verify a previously queued haul/construction
  action cannot take captive feed; inspect normal inventory access while sealed.
- Check real grid demand, partial supply, flight interruption and loss of
  radiator or port-floor support. Test blocked hot/loaded
  Unpair and removal from both ends; confirm old F6-R links still work. Do the displayed readings and stop reason explain each stop?
- Save hot, reload, verify retained charge/heat/gas and explicit Resume. Block the
  output tray, then free it and release once. Compare all product masses.
- With R4 aluminium paired to the F6, start **Repeat batches** and let two charges
  run. Check that receiving turns back on after each release, that a manoeuvre and a
  reload each pause the run, and that **End repeat run** leaves the batch unchanged.
- Open/close local and C1 panels repeatedly; check paused controls, numeric focus,
  smaller UI scales, native donor appearance and always-accessible Stop. Open the
  guard, enable/stop, drag each slider and Apply, then change a value through F3.
  Verify unsubmitted edits are retained and refresh causes no new commands.

These gameplay checks have **not** been run by the agent. File/load-order
verification does not establish a successful in-game cycle. Historical research,
installation records and mockups are background, not runtime proof.

## Crew standing orders

See [crew automation, specialities and time-skips](crew-automation.md) for default-disabled orders, native duty/AutoTask rules, approved stores, training, saved stops and supported onboard work. Industrial batches, exterior missions and crew-launched flight require explicit Resume. Gameplay and UI checks remain owner-run.

## Feature history and further reading

The dated [Shipbreaker changelog](../mods/PhobosShipbreaker/CHANGELOG.md) records
when features arrived. Earlier furnace studies describe the design at that time;
they are not the instructions for installing today's packages.

- [Material routing](furnace-material-routing.md): feed aluminium from R4 and
  send released products to a collector. Receiving, Start and Release are
  separate controls.
- [Repair-casting study](development/furnace-repair-castings.md): proposed heat sinks for
  future Manufacturing equipment. The implemented furnace still casts housings;
  the proposed heat sinks and machining are not available.
- [Electrical heating decision](development/furnace-electrical-direction.md) and
  [original thermal research](development/fusion-smelter-research.md): evidence and research
  attribution behind the design, with the earlier fusion-first proposal retained
  as history. Operating limits and yields here are gameplay choices.
