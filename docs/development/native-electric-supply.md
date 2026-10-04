# How the game supplies electricity: machines, the MHD and fuel

Recorded 5 October 2026 at the owner's request, after the V4 refinery's 24 kW draw raised
two questions: how much one MHD generator can supply, and whether drawing from it costs
fuel. The player version is in the [player guide](../player-guide.md#how-much-power-machines-draw).

**Source.** Everything below was read from the code of Ostranauts 1.0.1.5 (Blue Bottle
Games), in a local research copy kept out of the repository, and from the game's own data
files (`powerinfos`, `condowners_reactor.json`, `tickers`, `loot`). None of it was measured in
play. Figures are worked out from the constants named; a later game version may change them.

## How an appliance draws

- Each powered object runs its power step about once per game second (`Powered.Update`
  runs `Run` when at least one second has passed since its last step; the first step is
  offset by up to one second). The request is the object's rate times the time since the
  last step. A rate in the data is kilowatt-hours per second, so a 24 kW machine's rate is
  24 / 3600.
- The step gathers from what is wired to the object's power points (conduit, batteries,
  generators), then from storage it holds, then from its own charge (`UsePower`,
  `GatherPower`). Sources marked `IsPowerGen` are asked first; other sources are taken
  highest charge first. Passing power along conduit (`TransmitPower`) recurses through
  connected objects; no carrying capacity was found on conduit.
- The game's `Power` ticker adds one to `IsReadyUsePower` on every powered object each
  second (`CONDTickPower`); the power trigger only asks whether it is present. The large
  numbers this leaves in saves are harmless.

**What Phobos adds.** Before the game's step, a Phobos machine sets its request (idle
or working draw) and checks its room's air; if the room cannot take the step's heat, it
asks for nothing. After the step it measures what actually arrived (Framework
`NativeEnergyReceipts`, which keeps the request from before the game's call, Framework
0.93.0) and credits work for that only. Partial supply gives proportionally less progress.

## One MHD generator's limit

- While the reactor's bus is on and its MHD switch is checked, each reactor update sets
  every installed MHD's charge to **2 kWh** (`FusionIC.Run`, `StatPower = 2.0` on each MHD).
- The reactor updates every **0.27 game seconds** (`fTimeNextRun = fEpoch + 0.27`),
  scheduled from Unity's `Update`, so at most once per rendered frame.
- The MHD is not marked `IsPowerGen`, so the game takes it like a battery: whatever is
  wired to it draws down that 2 kWh store until the next refill.
- **Ceiling at normal speed:** 2 kWh per 0.27 s, about 7.4 kWh each second, or roughly
  **26.7 MW** per switched-on MHD. Each installed MHD is refilled separately; how many a
  load reaches depends on the wiring.
- **At high fast-forward** a frame can span more than 0.27 game seconds, and the refill
  still happens once per frame, so the ceiling becomes 2 kWh per frame.
- The refill does not depend on the reactor's power level, its MHD/thrust ratio knob, or
  how much was drawn. The MHD wears (`WearModule`) at the same rate whatever the load.
- **Battery charging:** while the MHD switch is on, the reactor is also marked ready to
  recharge (`IsReadyRecharge`) and pushes into batteries wired to its power point at each
  battery's own charge rate (`Powered.Recharge`). That is a second supply path, through
  batteries, separate from the 2 kWh store.

## Fuel

- The reactor burns deuterium and helium-3 in `FusionIC.Fusion` in proportion to its own
  power figure (which follows core temperature), the pellet rate set by the flow slider,
  the exhaust-velocity setting and time.
- Nothing in that calculation reads the electricity taken from the MHD, the load on the
  ship, or the MHD/thrust ratio knob, which only changes how the reactor's readouts split
  its power. **Drawing more electricity costs no more fuel** while the reactor is lit.
- What a Phobos machine's draw does cost is modelled by the machines themselves: heat
  into the room (a quarter of its waste heat by default, Framework 0.94.0) and time.

## Limits of this record

- Code reading, not measurement. A play test that loads one MHD well past a few
  megawatts would confirm the ceiling; the owner has not asked for one.
- The game's own consumers (thrusters, weapons, the reactor's modules) are outside this
  record except where named.
- Earlier research on raw fusion heat as a direct furnace source is in the
  [fusion industry roadmap](fusion-industry-roadmap.md); the reactor's displayed power
  split is still not an energy receipt.
