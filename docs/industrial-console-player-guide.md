# Industrial controls

Shipbreaker 0.19.0 requires Framework 0.21.0. The
[shared cue controls](shared-completion-cues.md) cover D4/R4 and optional Agriculture
equipment through C1, with one suite-wide volume/mute setting.

D4/R4 panels now offer **Notify on next batch completion**, **Cancel completion
notification** and cue volume/mute. Watching is optional, never starts a job and
clears on processing pause, fault or reload. See the
[completion cue guide](shipbreaker-completion-cue.md) for scope and listening checks.

Current packages: Shipbreaker **0.20.0**, requiring Phobos Framework **0.21.0**. Auto Nav remains optional.
Built against Ostranauts **1.0.1.5** / BepInEx **5.4.23.5**. Automated checks
passed; the new native panel/seating integration awaits the owner's game test.

## Use

Install a **Phobos' Asterel C1 Industrial Control Console** on a clear **3 x 3** area inside
a player-owned ship and connect separate electrical conduit. It weighs **40 kg**
and draws **80 W**. Right-click **Control Panel** to sit at it. The operator must
remain awake at that console. Multiple consoles may control the same ship; each
rechecks its own operator, power and ownership on every command.

The console lists installed dismantling fixtures, reclaimers, residue collectors,
chutes, grabbers, furnaces and cooling assemblies on its own host ship. Docking, mooring or towing never adds the
other ship's equipment, even when both ships belong to the player. Unknown,
leased and foreign ownership does not grant remote control. Moving/uninstalling
the console or changing selected crew ends the open session. Losing power or
ownership disables commands. Closing a screen or losing console power does not
cancel autonomous machinery work. Work and receiving still pause after reload.

Use **Overview**, **Equipment**, **Routing**, **Observations** and **Attention**. Equipment groups
collapse by type; search accepts a name or full object ID. **Change status filter**
cycles statuses. Both columns scroll independently, with visible scrollbars.
Narrow layouts show the list and details separately; **Equipment list** returns
to the list. Text wraps in scrolling content. The ship/access header and
**Pause ship industry** remain outside the scrolling body. There is no Start All.

Select one machine, then start/pause processing or receiving separately. The
Routing screen offers only supported pairs on this ship. Changing links and
filters uses the existing saved-pair services; it does not bypass floor routes,
locks, receiving capacity, input eligibility or reclaimer cooling checks.
Receiving filters cover identified feedstock, reclaimer rejects, legacy residue
and explicit released furnace products. Shipbreaker 0.17.0 adds a separate R4
aluminium outlet and F6 receiving controls; see [furnace material routing](furnace-material-routing.md).
These do not start or release a casting batch. Arbitrary item routing remains unsupported.

Every installed equipment family also has a local **Control Panel** action using
the same shared faceplate and command service. Inventory buttons on those local
panels close the controls and open the real inventory. Loading, collecting and
maintenance still need local crew access. A chute is passive; its panel explains
the connected intake. The grabber panel can lead to its linked fixture. Existing
F9 and older console controls remain diagnostic fallbacks.

Readouts show configured working demand, not measured reactor telemetry. Reclaimer
temperature/pressure is room atmosphere, not the machine's core. Attention is
based on typed status and interlocks, never on matching English error text. A
deliberately unused unlinked receiving port is not an alarm. Faults and invalidated
active receiving routes are attention states until acknowledged/restarted.

## Acquire and maintain

The console follows the existing additive equipment-stock service:

- OKLG scrap supply: broken, base chance 20%.
- OKLG fixer: usable with 15% wear, base chance 10%.
- San Diego / Halvorson: pristine, base chance 40%.
- VORB scrap trader: broken, base chance 15%.

These are independent chances when stock is generated, adjusted by Framework's
availability multiplier. Existing merchant stock does not necessarily refresh
immediately. Native base value is **$5,200** intact
and **$1,300** broken; actual quotes depend on the game market and condition.

Build on native tables (or a detected supported workbench) using **20 steel,
12 aluminium, 8 mechanical parts and 8 electronic parts**: 40 kg in, 40 kg out.
Base construction work is **2,700 seconds**, with the existing native work/tool
system. Requires a mortorq tool and soldering tool. Installation/uninstallation
progress targets are 1,000/800; repair 2,400; dismantling 500. These are native
work-progress values, not guaranteed wall-clock seconds. Restore uses the shared
native maintenance definition.

Repair consumes 1 steel, 1 aluminium, 2 mechanical parts and 4 electronics;
replaced mass returns as spent material through Framework maintenance.
Dismantling intact returns 16 steel, 8 aluminium, 8 mechanical parts, 4 electronics
and 10 trash; broken returns 12 steel, 6 aluminium, 4 mechanical parts and 20 trash.
Both retain **40 kg**. Output value is audited against the native whole-item value
and wear tiers, not assumed equal to mass. See [economy policy](equipment-value-audit.md).

## Observations

**Observations** shows native room-alarm outputs and the R4's built-in cooling-air
probes, with instrument ID, monitored compartment and validity. Missing readings
are unknown; stale values are explicitly historical. Attention includes instrument
problems. Equipment details retain the last recorded processing stop or collector
fault/block and available probe evidence for this session. See
[shared observations](shared-console-observations.md) for scope, limitations and
the focused test sequence. Use `phobosindustry observations <console-ID>` for
the same readings through F3. Losing console access also stops observation reads.

## Console commands

`phobosindustry help` lists the new commands. Start with `phobosindustry consoles`
to get the full console ID, then `phobosindustry controls <console-ID>` or
`phobosindustry status <console-ID>`. Status reports full equipment IDs.

Actions use `phobosindustry <action> <console-ID> <equipment-ID> [value]`.
Supported actions are `start`, `pause`, `cancel`, `receive`, `pause-receive`,
`link-input`, `link-output`, `unlink-input`, `unlink-output` and `filter`.
Link value is a full peer ID; filter value is `all`, `feed`, `rejects` or `legacy`
(reclaimers accept only `feed`). `pause-all` needs only the console ID and reports
each attempted processing/receiving pause. These commands enforce the panel's
same access and machinery rules.

## Focused owner check

1. Open a local equipment panel, then use a powered console to operate distant
   equipment. Confirm processing/receiving independence and local inventory access.
2. Dock to another ship/station: its equipment must stay absent. Change crew or
   uninstall/move the console: reopen before commanding. A console on a ship the
   player does not own must refuse access.
3. Browse multiple machines; try search, status, Attention and routing, scrolling
   at your usual UI scale. Close/Escape and resume movement without stuck input.
4. Save/reload: existing cargo/jobs/pairs/filters survive, while work and receiving
   remain paused. Console power loss must block commands without erasing jobs.

These are checks of the new UI/access integration, not another basic power test.

## Later: PDA / visor

Documented only. A future view-only cartridge could overlay logical saved
conveyor pairs, full IDs on selection, directional arrows and blocked/missing
endpoints, scoped to the subscribed player-owned host ship. Show all connections
or only the selected machine's links to avoid clutter. Reuse Framework pair
records and snapshots. Do not imply those lines are physical belts or silently
include docked ships. Access, native visor drawing hooks, visibility, refresh cost
and save behaviour need implementation research. This release adds no cartridge,
overlay, remote PDA commands, cargo locator or new conveyor simulation.
