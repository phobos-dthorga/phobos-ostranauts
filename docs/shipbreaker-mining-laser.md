# Mining laser

Shipbreaker 0.59.0 with Framework 0.66.0. Implemented and checked offline; owner
gameplay checks are pending, including how the firing animation and the beam
look in play. Use the [current dependency requirements](installing-mods.md).

## Equipment

| Item | Size and mass | Power | Base price | Where |
| --- | --- | --- | --- | --- |
| Phobos' Ablatine ML-2 Mining Laser | 2 x 2 tiles on the outside of the hull; 120 kg | 0.1 kW idle, 24 kW while cutting | 9,600 cr, broken 2,400 cr | K-Leg supply kiosk and fixer, San Diego Halvorson, the Venus scrap kiosk and regional markets, in lots of eight; CCRE and GalCon faction kiosks at Friendly standing; INSTALL > APPS. Also rare engineering salvage. |

No fabrication recipe. The ML-2 is a laser head that bolts to the outside of
your hull and works whatever ship is moored to yours: it breaks asteroid rock
the way a drill does, and it cuts wall panels loose from a hull your G4 has hold
of. It does not collect anything. What falls stays where it fell, and the crew
fetch it.

## Use

1. **Install it.** From the INSTALL menu's **APPS** tab, place it outside the
   hull against two sound hull walls. The emitter faces away from those walls.
   Run power conduit to either power point, which sit in the wall row behind it.
2. **Give the room behind it air.** The laser sheds its heat into the room on
   the inside of those two walls. That room needs at least 10 kPa.
3. **Moor a target.** Either tether an asteroid with the nav station's mooring
   clamp (the game's own tether: within 2.5 km of the surface and under
   50 m/s), or capture a hull you own with the
   [G4 grabber](shipbreaker-reclamation.md). The laser needs exactly one moored
   ship. A station dock does not count.
4. **Choose what it cuts.** Right-click the laser and open its **Control Panel**
   with a crew member standing by the wall behind it, or use the
   [C1 console](industrial-console-player-guide.md). **Set to cut** offers Rock
   only, Wall panels only, or Rock and wall panels. An asteroid is always rock.
5. **Start / resume cutting.** The laser sweeps its arc from one side to the
   other, nearest things first, one cut at a time. **Pause cutting** keeps the
   work so far. **Stop** drops the cut in hand and keeps the sweep and the
   counts.
6. **Send the crew to fetch.** Ore and gangue lie on the asteroid where the
   rock stood. Freed panels lie on the hull where they hung. Haul them home by
   hand, into [material bins](shipbreaker-material-bins.md) or to the D4.

The panel shows the cut in hand, how much rock has been broken, how many panels
are free and how much is left in the arc.

## What it reaches

- A 60 degree arc centred on the way the head faces, out to 24 deck tiles from
  the emitter (about 7.7 m).
- Only the first solid thing along each line. To get at rock behind rock, it
  breaks the near rock first.
- Never anything behind your own hull or equipment. If that is all that is
  left, the panel says so; move the ship or the laser.
- Never the rock or panels within a tile and a half of the mooring anchors, and
  never the wall, floor and port a G4 is holding the hull by.

## What it cuts

**Rock.** Asteroid rock walls, ice walls and unbroken cores. The laser asks the
game's own mining rule what counts, and does the game's own damage to it, so
what falls out is exactly what a crew member with a drill would get: ore,
gangue, ice, and the Phobos mined chunks where the game rolls them. A rock wall
takes two cuts, one to crack it and one to break it: 45 seconds and 0.3 kWh at
full power. The laser leaves three things alone:

- **Opened ore deposits.** The game hands deposit ore to the person drilling,
  so a deposit the laser has opened is still a job for the crew.
- **Bare rock floor, ice floor and rubble.** Breaking them only ever gives
  gangue, which is not worth the power.
- **Anything already at its limit.** If someone else is finishing it, the laser
  moves on.

**Wall panels.** Ordinary wall panels on a moored hull that you own, with nobody
aboard and no air left in it; the same hulls the G4 will work. A panel takes 60
seconds and 0.4 kWh at full power. The laser frees it the way the game's own
uninstall does, so the whole panel comes away intact for the D4.

## Power and heat

- It draws 24 kW only while it is actually cutting, and 0.1 kW otherwise. On
  short power it cuts more slowly and the energy for each cut is the same.
- About 14.4 kW of that warms the room behind the mount. The laser cuts while
  that room is under 40 C and holds at least 10 kPa. When the room cannot take
  the next second of heat the laser waits, draws nothing and carries on by
  itself once the room has cooled. A small room warms by a little under 1 C for
  every second of cutting, so expect bursts of cutting with pauses between
  them. A bigger room, or better cooling in it, gives longer bursts.
- A mount with vacuum behind it will not run. Vacuum is not cooling.

These figures are authored for play. The game's damage points are not a unit of
energy, and no real laser was measured for them.

## Safety

- The laser holds fire while anyone, yours or not, stands within a tile of the
  beam between the head and the thing it is cutting. The panel says so, the
  crew log says who, and the nav station shows a banner if it is open. It
  carries on when they move.
- The beam you see is drawn for you. It lights nothing, starts no fires and
  hurts nobody; the rule above is what keeps people out of it.

## Saves and changes

- After loading a save the laser is paused. **Start / resume cutting** carries
  on with the sweep, the counts and any work already paid into the cut in hand.
- If the game was saved in the moment between a cut being made and the rock or
  panel giving way, the laser looks at what actually happened. If the cut did
  not take, it is paid for again; it is never repeated for nothing.
- Moving or turning the laser, or mooring something else, starts a fresh sweep.
  The totals are kept.
- A new save record on the laser only. Nothing else aboard changes.

## Settings

`BepInEx/config/phobosgekko.ostranauts.shipbreaker.cfg`, section `Laser`:
`ShowBeam` (on by default) shows the firing animation and the beam. Turning it
off changes nothing about the work.

## Limits

- The laser only reaches a ship moored to yours. It cannot work a rock or a
  wreck you are merely flying beside: deck tiles are 0.32 m across and the
  game keeps free-flying ships hundreds of metres apart.
- One moored ship at a time, and no station docks.
- It does not gather, haul or feed. The crew do.
- A radiator link, for cutting harder without warming the cabin, is planned and
  not in this version.
- Offline checks are not gameplay validation. How the beam and animation look,
  how long the bursts feel and how the sweep order reads still need checking in
  play.

Design evidence and the reasons behind these choices are in the
[mining laser design record](development/mining-laser-design.md).
