# F6 electric furnace: first implementation

**25 September 2026 — Shipbreaker 0.12.0, Framework 0.16.0.** This is a prepared
implementation candidate. Automated physics and native-definition checks are
separate from in-game evaluation; installation and gameplay review remain with
the owner. Ordinary saves are supported. No game or save was modified to test it.

## Installation

**Phobos' Rivetline F6 Electric Furnace** occupies **6 x 6 tiles**, weighs 240 kg
empty and is rated for a 50 kg charge. The first supported recipe uses exactly
20 kg; the rating does not enable arbitrary alloys or larger recipes. Its three
80 kg construction sections keep each craft within Framework's 100-unit limit.

**Phobos' Rivetline F6-R Exterior Radiator** is separate **6 x 4**, 100 kg
equipment. Both can appear in the existing industrial/fixer/scrap stock routes,
and both have table construction, installation, repair, Restore and mass-balanced
dismantling. Existing D4/R4 equipment and construction routes remain unchanged.

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
footprint. Furnace placement requires flooring. Pair their full object IDs using
the Control Panel; pairing requires a player-owned ship. The pair is reciprocal
and saved. Moving hardware or breaking the geometry removes usable cooling.
An intact disconnected radiator continues radiating its own stored heat.

Connect the F6's two front power points to ordinary native electrical supply.
There is no reactor-side coupler, fuel debit or free reactor-running heat.
Peak demand is approximately **279.8 kW**: 250 kW useful heat at 90% efficiency,
plus 2 kW auxiliaries. At rest, a connected unit requests **50 W** for instruments;
this energy also enters its finite cooling store. Active cooling auxiliaries use
up to 1 kW. The one-way passive thermal path remains available without power.

## First casting

1. Open **Control Panel**, choose the radiator and pair it. Allow a powered
   instrument update. Native donor failures fall back to the existing controls.
2. Open **Feed** locally. Insert **twenty separate, unstacked native Scrap
   Aluminum items**, each 1 kg and carrying nothing. Other identities, including
   finished housings and historic residue, are not accepted. Native auto-stacking
   is disabled only for items entering or leaving the F6 charge bin.
3. **Seal and verify charge**. The cool lining, exact feed, room atmosphere and
   finite receiver are checked. This closes the native feed container and captures
   80 litres of the room's actual gas species. No mass is replaced by virtual cargo.
4. **Enable / resume sequence**. AUTO evacuates, preheats, melts and holds at
   700 C for 60 continuous qualified seconds, then solidifies in its captive mould
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
| Cool | Connected finite sink and accepting room leakage | No power is required for the passive path |
| Gas return | ≤50 C, same ship, authorized adjacent room, accepting gas | Receiver stays full until return succeeds |
| Release | All outputs fit; residual charge heat fits radiator | Blocked output leaves inputs intact |

The 30 kJ/K lining and 80 kJ/K cooling assembly are separate stores. The radiator
uses a 12 m² effective area, emissivity 0.85 and a 200 K background; its 250 C
operating limit is checked before demand. Passive transfer is limited to 100 kW
and 0.30 kW/K. Room leakage uses 1 W/K with actual native gas heat capacity and
a 60 C accepting-room ceiling. Vacuum supplies no convective cooling. These are
authored lumped thermal parameters, not measurements of vanilla equipment.

Charge sensible heat remaining at release moves into the finite radiator store;
released native stock has no separate invisible hot-item state. Chamber and
receiver sensible energy moves back into native room gas. Every native electrical
receipt is consumed once, including partial supply and appliance storage changes.

Power loss, failed instruments, missing cooling and native flight commands pause
heating. Torch demand and RCS manoeuvre commands pre-empt the furnace; the furnace
does not alter flight controls or upstream fuel accounting. Resume is explicit.
The permanent **STOP / ISOLATE HEAT** button remains below the scrolling controls.

Hot state, gas species, exact input IDs, qualified casting state and bounded
settings persist in native object maps. Reload always pauses heating and resets
an unfinished continuous hold. Passive simulation advances observed intervals up
to 60 seconds, substepped at 0.25 seconds. Longer or unloaded intervals retain
heat conservatively, reset the clock and require Resume; wall-clock time away
does not complete a batch. Very large fast-forward steps can therefore pause it.

Uninstallation, dismantling and disconnection require a cool, empty furnace.
Repair/Restore requires cool hardware, but may service a still-sealed cold charge;
this allows a failed probe to be repaired before gas return. Native damage mode
switches retain persistent maps and cargo; damaged probes display **Unknown**.
The radiator's local thermometer is passive. Damaged probes do not disable physical
heat transfer: a damaged exterior radiator retains 25% of its nominal radiating
area, but heating is blocked until repaired. Losing its supporting wall disconnects
the furnace while an exposed fin bank still rejects its own heat. Absolute destruction remains the
game's destructive machinery path; this release does not add explosions, rupture
recovery or a new atmosphere/hazard simulation.

Output delivery stages both products before retiring the twenty inputs in one
synchronous native operation. An interrupted gas transfer or output commit retains a protected marker,
locks the equipment and cannot replay after reload. This is **not** a promise of
crash-atomic native inventory operations. Keep the log and retained cargo for
recovery; do not delete or reset a protected record.

## Controls and native artwork

Local panels, C1 and F3 use one checked service. Remote physical inventory access
remains local. Numeric limits use decimal-point input; measurement formatting
and complete UI messages use localization catalogs. Reading a panel does not
advance the process or rewrite saved settings.

The implementation loads only isolated `GUIShip/GUIReactor` child donors:
`pnlPower/knobBus`, `pnlCoreTemp/pnlLeds`, `pnlPower/pnlLedsTotal` and
`pnlInit/pnlStepBus/bmpGreen`. It checks the locally audited game assembly fingerprint, audits the component
hierarchy, initializes under
an inactive owned root and explicitly detaches knob callbacks during refresh.
It also reuses the air-pump title font. No full reactor controller is cloned.

The guarded toggle, seven-segment formatter and sliders remain follow-up work.
This candidate uses ordinary signed TMP readings and Framework numeric fields,
buttons and scrolling for those controls. Native layout/scale, click feedback,
focus and repeated opening need in-game review. Diagnostics identify rejected
donor paths once; original game assets are never modified or distributed.

F3 entry points:

```text
phobosfurnace help
phobosfurnace list
phobosfurnace controls <full-furnace-id>
phobosfurnace pair <full-furnace-id> <full-radiator-id>
phobosfurnace status <full-furnace-id>
phobosfurnace stop <full-furnace-id>
```

C1 also accepts furnace actions through `phobosindustry <action> <console-id>
<furnace-id> [value]`, using the same access checks.

## Owner review on return

- Install the prepared packages with the existing installer after closing the
  game; confirm Framework 0.16.0 and Shipbreaker 0.12.0 in the loaded status/log.
- Check the furnace/radiator placement in all rotations, visible alignment and
  collision bounds. Review the three candidate sprites at normal game scale.
- Pair, load and seal one batch. Verify a previously queued haul/construction
  action cannot take captive feed; inspect normal inventory access while sealed.
- Check real grid demand, partial supply, flight interruption and loss of
  radiator support. Do the displayed readings and stop reason explain each stop?
- Save hot, reload, verify retained charge/heat/gas and explicit Resume. Block the
  output tray, then free it and release once. Compare all product masses.
- Open/close local and C1 panels repeatedly; check paused controls, numeric focus,
  smaller UI scales, native donor appearance and always-accessible Stop.

These gameplay checks have **not** been run by the agent. The owner's absence is
the reason installation, visual approval and interactive validation were left
for return. The historical research and mockup are background, not runtime proof.
