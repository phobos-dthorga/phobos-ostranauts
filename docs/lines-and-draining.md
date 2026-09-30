# Lines hold what they carry: draining and venting

Phobos Framework 0.63.0 and Manufacturing 0.25.0, with Shipbreaker 0.58.0 for the
furnace coolant conduit and Agriculture 0.33.0 for irrigation. Every Phobos pipe keeps
what flows through it, like real pipe: the process-water line, the gas line, the
Lixivar acid line, the F6-C coolant conduit and the irrigation conduit.

## What a line holds

| Line | Each tile holds | When you take it up |
| --- | --- | --- |
| Process-water line | about 0.49 kg of water | drain it into a canister |
| Gas line | a few grams of gas: 0.4 g hydrogen, 3.2 g methane, 3.4 g ammonia, 5.6 g nitrogen, 6.4 g oxygen or 8.8 g CO2 | vent it |
| Lixivar acid line | about 0.90 kg of sulfuric acid | drain it into a canister |
| F6-C coolant conduit, serviced coolant | about 0.33 kg of service fluid | drain it into a canister |
| Irrigation conduit | about 0.20 kg of water or feed | drain it into a canister |

An open water, gas or acid line fills itself from the tanks and stores on it, a
couple of seconds after they have something above their reserve. The coolant conduit
is filled by the F6 furnace's pump instead, from the coolant charges loaded into the
furnace; see the [furnace guide](furnace-player-guide.md). A conduit of the legacy
sealed assembly holds nothing. The irrigation conduit is filled by a running W2 from
its own reservoir before feed reaches the racks; see
[fluid networks](fluid-network-operations.md). The water, gas or acid comes out of those
tanks, and each line tile weighs that much more. A gas line with several stores on it
holds a mix of their gases; nothing stops you sharing one line between gases.

Machines still draw from and deliver to their linked stores exactly as before. The
line's contents are the pipe being full, not a second store.

## Draining a water or acid line

1. Have a **Phobos' Rivetline D20 Drain Canister** on hand: carried by the crew member,
   or lying on the deck within two tiles of the line. An empty canister works, and so
   does one already holding the same liquid with room left.
2. Right-click any installed tile of the line and choose **Drain line into canister**.
3. The crew member drains the whole connected run, nearest tiles first, for two minutes.
   One canister holds 20 litres: about 20 kg of water or 36.7 kg of acid. A long line
   takes several canisters. The crew log says how much went in and how much is left.

Draining **closes** the run. Its tank no longer refills it, and no machine reaches a
store through it. When you are done, right-click it and choose **Return line to
service**; it opens again and refills from its tanks.

## Venting a gas line

Right-click an installed gas line and choose **Vent gas line**. Its few grams go into
the room air, except hydrogen, which the game has no room gas for, so it goes overboard.
The run closes in the same way; **Return line to service** opens it again.

## Pouring a canister back

A filled canister is ordinary cargo. Move it with the game's own Haul orders or by
dragging it, or let a hauling mod such as LOGUSS's Common Sense move it. Put it in the
inventory of an installed, undamaged tank that holds the same liquid: a Rivetline water
silo for water, the canister rack on an AT acid tank for acid, or the Products of a cool,
idle F6 furnace with serviced coolant for coolant, or a W2's inventory for water or that
W2's own feed. Within a couple of seconds it pours in, as far as there is room, and the
empty canister stays there for next time.

## Taking a line up, damage and loss

- A line tile that holds anything cannot be uninstalled or dismantled; the refusal says to
  drain it first. Drain or vent the run, then take it up.
- A damaged gas tile lets its gas out into the room (hydrogen overboard). A damaged water
  tile keeps its water until drained.
- A damaged acid tile lets a ten-thousandth of its acid into the room as mist, which the
  game treats as H2SO4, so keep the crew clear. The rest stays in the tile until drained.
- A destroyed tile loses what it held; the crew are warned and the log records how much.

## Buying canisters

The D20 costs 40 cr and is sold with the lines at every general market in lots of 16,
and for scrip at the CCRE and GalCon faction kiosks at any standing. Each one keeps
its own contents, so canisters never stack.

## Limits

The hold-up is authored: a 25 mm bore, one metre of pipe per tile, and gas at 10 bar.
It is not a flow or pressure simulation. Ship's Water tanks do not fill lines.
Existing saves need nothing from you: lines already laid start empty and fill within
seconds, taking their hold-up from their tanks. None of this has been checked in play
yet. Design details are in the [line contents record](development/line-contents-design.md).
