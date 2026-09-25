# Auto Nav 0.10.0: flight controls, braking room and salvage

Prepared on 25 September 2026 for Ostranauts **1.0.1.5** / BepInEx **5.4.23.5**,
requiring **Phobos Framework 0.14.0**. This implements the four recommendations
from the earlier Auto Navigate comparison. Offline checks are complete; installation
and owner gameplay evaluation remain separate. No new graphics or saved item IDs.

## Throttle and approach safety

All Auto Nav RCS commands now share the selected console throttle across both
translation axes and turning. During combined movement, turning can use up to
one quarter of the budget; translation gets the remainder. Pure turning can use
the selected budget. This applies to approach guidance, arrival braking,
coasting spin correction, torch alignment and docking. It fixes an inherited
command-budget error; it is not a measured claim about in-game fuel savings.

Ordinary **Fly** and **Resume**, including automatic restoration after loading,
check braking room using current qualified contact, relative motion, hull size,
arrival settings, RCS acceleration and console throttle. The calculation reserves
turning capacity, a braking margin and one maximum permitted simulation step.
It does not depend on the torch being available. Details reports required and
available room when a start is rejected.

There is no arbitrary minimum engagement range. Slow starts inside the arrival
band remain possible with hull clearance; an already fast inbound approach can
be refused. Reduce relative speed manually or arrange more room, then try again.
The check does not interrupt an already running brake. Dock retains its separate
capture admission rules. This is not obstacle avoidance or a collision guarantee.
**Stop / Coast still clears thrust rather than braking.**

## Settings belong to the console

Each navigation console saves its own cruise speed, arrival relative speed and
stopping distance. Use the existing distance dial and the new minus/plus controls
in **Details**. Exact values remain available through F3:

```text
phobosnav cruise 100
phobosnav arrivalspeed 0
phobosnav arrival 1
phobosnav settings
```

Cruise accepts **10–5,000 m/s**; arrival speed accepts **0–1,000 m/s**, no higher
than cruise; stopping distance accepts **0.1–100 km** with native hull clearance.
Lowering cruise also lowers an excessive arrival-speed preference. Custom values
stay exact until deliberately stepped. The controls stop at their limits.

New console preferences start from the existing configuration. Merely opening a
panel does not write them. The first successful setting change or ordinary Fly
stores that console's numeric defaults. Editing global defaults later affects
only consoles without stored preferences. Separate ships, consoles and saves do
not share these saved records. Propulsion preference and the other general
configuration settings retain their existing behaviour.

Active and suspended flights retain their captured profile; stop before changing
numeric settings. `phobosnav fly 0.5` requests 500 m for that flight only, without
changing the saved distance default. Resume uses the saved destination and profile.
Missing old preference records require no migration. Unknown or invalid records
are retained and block new settings/flights until explicitly reset with
`phobosnav defaults`; this clears only numeric preferences. `phobosnav forget`
remains the separate saved-flight action. Neither operation is performed by a
display read.

## Rare module salvage

New native navigation-module loot rolls can include an intact or damaged
**Phobos' Asterel N1 Polaris Auto Nav Module**. The default chance is **3% per
eligible leaf-table roll**: approximately **1% functional and 2% damaged** in
mixed pools, or **3% damaged** in damaged-only pools. One added choice can produce
at most one module. These are authored balance values, not native drop rates.

The registration covers the native navigation-module leaf tables used by ship
generation and random module pools. Some pools also feed merchants: this is not
an exclusively derelict or navigation-console-only drop. A generated ship can
roll more than one eligible table. Existing inventories and already generated
ships are not rewritten; finding a module is not guaranteed by reloading.

In the Auto Nav configuration, `[Salvage] Enabled` disables these future-roll
additions; `NavModuleChance` sets the total probability from 0 to 1. Change these
with the game closed and restart. Framework's merchant availability multiplier
continues to govern explicit merchant offers; it does not scale this separate
salvage chance. Existing acquisition, repair, Restore and dismantling remain in
the [economy guide](auto-nav-economy.md).

Framework 0.14.0 supplies the small additive loot helper also used by merchant
stock. Auto Nav owns the table selection, item identities and probabilities.
Repeated registration keeps one owned branch and preserves native/foreign
entries. All changes publish through the existing definition transaction.

## Verification and owner checks

Offline checks cover combined commands at low throttle, integrated RCS/torch
approaches, unsafe Fly/Resume rejection, near slow starts, read-only displays,
two-console preference isolation, captured profiles, unsupported saved records,
and the actual native loot parser and repeated/disabled registration. Builds
also verify the existing artwork hashes and native panel placement references.
Native definition checks include existing Shipbreaker economy behaviour.

After installing the prepared packages, check Details legibility and speed
controls at your display scale; set different values on two consoles and reload;
try an ordinary approach at reduced throttle and a rejected fast inbound start;
confirm a suspended flight retains its profile. Evaluate salvage through normal
new generation/restocking over time. No specific drop or in-game compatibility
is asserted by the offline results.
