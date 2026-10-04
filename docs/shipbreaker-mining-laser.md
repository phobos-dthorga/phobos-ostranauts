# Mining laser

Shipbreaker 0.61.0 (the laser arrived in 0.59.0, its radiator link in 0.61.0).
Implemented and checked offline; owner
gameplay checks are pending, including how the firing animation and the beam
look in play. Use the [current dependency requirements](installing-mods.md).

## Equipment

| Item | Size and mass | Power | Base price | Where |
| --- | --- | --- | --- | --- |
| Phobos' Ablatine ML-2 Mining Laser | 2 x 2 tiles on the outside of the hull; 120 kg | 0.1 kW idle, 24 kW while cutting (48 kW at the high setting) | 9,600 cr, broken 2,400 cr | K-Leg supply kiosk and fixer, San Diego Halvorson, the Venus scrap kiosk and regional markets, in lots of eight; CCRE and GalCon faction kiosks at Friendly standing; INSTALL > APPS. Also rare engineering salvage. |

No fabrication recipe. The ML-2 is a laser head that bolts to the outside of
your hull and works whatever ship is moored to yours: it breaks asteroid rock
the way a drill does, and it cuts wall panels loose from a hull your G4 has hold
of. It does not collect anything. What falls stays where it fell, and the crew
fetch it.

## Use

1. **Install it.** From the INSTALL menu's **APPS** tab, place it outside the
   hull against two sound hull walls. The emitter faces away from those walls.
   Run power conduit to either power point, which sit in the wall row behind it.
2. **Give the room behind it air, or link a radiator.** The laser sheds its heat
   into the room on the inside of those two walls, which needs at least 10 kPa,
   unless a linked F6-R or F6-P radiator takes it.
3. **Moor a target.** Either tether an asteroid with the nav station's mooring
   clamp (the game's own tether: within 2.5 km of the surface and under
   50 m/s), or capture a hull you own with the
   [G4 grabber](shipbreaker-reclamation.md). The laser needs exactly one moored
   ship. A station dock does not count.
4. **Choose what it cuts.** Right-click the laser and open its **Control Panel**
   with a crew member standing by the wall behind it, then **Settings**; or use the
   [C1 console](industrial-console-player-guide.md). **Set to cut** offers Rock
   only, Wall panels only, or Rock and wall panels. An asteroid is always rock.
5. **Start / resume cutting.** The laser sweeps its arc from one side to the
   other, nearest things first, one cut at a time. **Pause cutting** keeps the
   work so far. **Stop** drops the cut in hand and keeps the sweep and the
   counts.
6. **Send the crew to fetch.** Ore and gangue lie on the asteroid where the
   rock stood. Freed panels lie on the hull where they hung. Haul them home by
   hand, into [material bins](shipbreaker-material-bins.md) or to the D4, or let
   the laser queue the jobs for you: see [crew jobs](#crew-jobs).

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
- With vacuum behind it and no linked radiator, the laser does not run, like
  every Phobos machine in vacuum; the panel says so.

These figures are authored for play. The game's damage points are not a unit of
energy, and no real laser was measured for them.

## Radiator link and the high setting

The laser can shed its heat into one of the F6 furnace's cooling assemblies in
place of the cabin: an F6-R exterior radiator or an F6-P thermal exhaust port.

1. **Fit the assembly where it touches the laser**, or stands one tile from it.
   An F6-R goes outside on the hull beside the head. An F6-P goes on sealed deck
   just inside the wall behind it. Each has its own mounting rules; see the
   [furnace guide](furnace-player-guide.md).
2. **Pause the laser**, open its Control Panel, then **Settings**, and choose **Cooling**. Pick the
   assembly. It must be at 50 C or below, undamaged, properly mounted and not
   paired with a furnace or another laser.
3. If you want it, choose **Power**: **High, 48 kW**. It needs the linked
   assembly to be ready, and it applies from the next cut.

While the assembly is ready and has room, all of the laser's heat goes there and
the room behind the mount is left alone, so the laser no longer stops for the
cabin. The panel shows where the heat is going, how hot the assembly is and how
much more it can take.

| Setting | Draw | Heat to shed | Rock wall | Wall panel | One assembly settles near |
| --- | --- | --- | --- | --- | --- |
| Standard | 24 kW | 14.4 kW | 45 s | 60 s | 130 C |
| High | 48 kW | 28.8 kW | 22.5 s | 30 s | 203 C |

The electricity for each cut is the same at either setting; High only does it
sooner. An assembly's limit is 250 C.

- One assembly serves one machine: a furnace, or one laser.
- If the assembly is damaged, moved away, uninstalled or at its limit, the heat
  goes to the room behind the mount instead, under the room rule above. A cut
  that started at 48 kW keeps that draw, so expect it to wait on the room.
- To unpair, pause the laser, let the assembly cool to 50 C or below and choose
  **Cooling: Room behind the mount**.
- The link is by touching only. The laser cannot be cooled through F6-C conduit
  in this version.

## Crew jobs

Two settings on the laser's **Settings** page, both off until you choose them
(Shipbreaker 0.71.0). They queue the game's own jobs, the same ones you paint with
the PDA, so the crew follow the game's rules and you can cancel a job from the PDA.

- **Haul jobs for what it frees.** Each freed panel, and the ore and gangue from
  each broken rock, gets a Haul job. Crew with the **Haul** duty bring it to a
  **haul zone** on your ship: mark one with the PDA's zone tool and tick Haul.
  With no haul zone the crew have nowhere to bring it, and the laser tells you
  once. What the zone accepts is the zone's own category setting. Once panels are
  aboard, the D4's Load feed order takes them from the deck as usual.
- **Mine jobs for deposits it opens.** When a cut opens an ore deposit, the deposit
  gets the game's Mine job. Crew need the **Demolish** duty and a mining drill.
  The ore goes to whoever drills, as the game always does it; the laser still
  never touches a deposit itself.

The panel shows both settings and how many jobs were queued since Start. Only
cuts finished while a setting is on get a job; things already lying about are
yours to paint. Turning a setting off leaves queued jobs in place.

## Safety

- The laser holds fire while anyone, yours or not, stands within a tile of the
  beam between the head and the thing it is cutting. The panel says so, the
  crew log says who, and the nav station shows a banner if it is open. It
  carries on when they move.
- The beam you see is drawn for you. It lights nothing, starts no fires and
  hurts nobody; the rule above is what keeps people out of it.

## Saves and changes

- After loading a save, a laser that was cutting when you saved starts again by
  itself once its usual Start checks pass (its mount, what is moored and which
  way it faces). It carries on with the sweep, the counts and any work already
  paid into the cut in hand. If a check fails it stays paused and says why; a
  laser that was paused when you saved stays paused until **Start / resume
  cutting**.
- If the game was saved in the moment between a cut being made and the rock or
  panel giving way, the laser looks at what actually happened. If the cut did
  not take, it is paid for again; it is never repeated for nothing.
- Moving or turning the laser, or mooring something else, starts a fresh sweep.
  The totals are kept.
- New save records on the laser only: its sweep, what it is set to cut, its
  cooling link and its power setting. The cooling assembly's own record is
  unchanged.

## Settings

`BepInEx/config/phobosgekko.ostranauts.shipbreaker.cfg`, section `Laser`:
`ShowBeam` (on by default) shows the firing animation and the beam. Turning it
off changes nothing about the work.

## Limits

- The laser only reaches a ship moored to yours. It cannot work a rock or a
  wreck you are merely flying beside: deck tiles are 0.32 m across and the
  game keeps free-flying ships hundreds of metres apart.
- One moored ship at a time, and no station docks.
- It does not gather, haul or feed. The crew do, by hand or through the
  [crew jobs](#crew-jobs) it can queue.
- Crew jobs are the game's own. Whether crew can reach the moored rock or hull
  (suits, airlocks, a walkable way across) is the game's pathing, not the laser's.
- The radiator link is by touching only; there is no piped cooling for the laser.
- Offline checks are not gameplay validation. How the beam and animation look,
  how long the bursts feel and how the sweep order reads still need checking in
  play.

Design evidence and the reasons behind these choices are in the
[mining laser design record](development/mining-laser-design.md).
