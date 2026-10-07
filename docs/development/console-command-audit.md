# F3 test commands: the unlockdebug audit

Audit record, 7 October 2026 (Framework 0.128.1, Auto Nav 0.35.1). The owner's rule, now in
`AGENTS.md`: an F3 **test command** that changes saved data, or how play goes on if the
player carries on, works only after the game's own `unlockdebug`, warns on every use that the
save then lies outside what the mod was built for and that no later version will put it
back, needs `confirm` at the end of the command, and marks the save as test-changed.
Read-only readouts and ordinary player commands stay open. Framework's
`Diagnostics.DebugCommands` is the shared gate and the shared mark (see the
[author guide](framework-author-guide.md)).

## How the commands were classified

- **Readout:** prints what is there and changes nothing.
- **Player command:** does what a panel button or ordinary play does, through the same
  service and the same checks. A save after it is one the mod was built for.
- **Test command:** creates items, progress, standing or story state from nothing, forces an
  outcome, or erases saved progress, which ordinary play cannot do. Gated.

Agent survey of every console handler (`ConsoleResolver.ResolveString` prefixes and the
services they call); the owner rule decides, and the owner may revise any classification.

## Gated (test commands)

| Mod | Command | Why |
| --- | --- | --- |
| Framework | `phobosframework story start <arc>` | Starts an arc whatever its requirements, place and chance |
| Framework | `story try <arc>` | Starts an arc at will, skipping its chance and normal trigger (the code form, `StoryArcs.TryBegin`, which Phobos Banking calls, is not gated) |
| Framework | `story reset <arc>` | Erases an arc's record so a one-off arc and its rewards can run again |
| Framework | `story news <broadcast>` | Queues a bulletin in the saved queue and marks once-only items seen |
| Framework | `story file <file>` | Puts a story data card in the inventory from nothing |
| Framework | `story flag <flag> [clear]` | Sets or clears a saved story flag |
| Framework | `story standing <faction> <change>` | Changes faction standing |
| Framework | `story check` | Runs the story check now: each use is an extra chance for waiting arcs to start, beyond the normal pace (agent choice; the effect is small) |
| Auto Nav | `phobosnav spawn`, `spawnpursuit` | Create a navigation module in the console from nothing |
| Exchange | `phobosexchange test shock`, `test reset` | Jump a price; reset the market (built gated in 0.1.0) |

## Left open

- **Framework:** `status`, `recipes`, `help`, `addons`, `perf`, `loot`, `crew`, `skip`,
  `articles`, `story` (the report), `story items`, `story chatter`, `story where`,
  `story thread`, `story places`, `story people` are readouts. `upkeep … on|off` is the Crew
  panel's switch; `help <article>` opens the encyclopedia as the About button does;
  `story answer` sends a reply as the Letters window does; `story letters` opens it.
  `story chatter <line>` only changes the text of the next small talk for the session and is
  never saved (agent choice: not gated).
- **Auto Nav:** flight, docking, departure, pursuit, fire control, sensors, cue and preference
  commands match the panel; `defaults` and `forget` only discard the player's own console
  preferences or saved flight.
- **Agriculture, Manufacturing, Medical, Shipbreaker, War Has Been Declared:** every command is
  a readout or the same action as a panel control through the same service.
- **Banking:** `debts`, `lenders`, `loans`, `line` are readouts; `open` and `finances` open
  windows; `borrow`, `approve`, `withdraw`, `openline` and `draw` call the same services and
  checks as the panel's buttons (agreed with the Banking session, 7 October 2026).

## Notes found on the way (not changed here)

- Agriculture's console help still lists `mix-potato`, `mix-lettuce` and `water-only`, which no
  code handles any more.
- War Has Been Declared's `battle`, `standdown` and `lay` need only the player's own ship from
  F3, while the nav station's orders also need an intact, installed station: a small parity
  gap, not a test command.
- Banking's panel confirms borrow, open line and draw on a card; F3 does them at once.

## Checks

The gate's rule and the shared mark are covered by Framework's offline checks
(`ChartChecks`); a native check confirms the game's `CrewSim.bEnableDebugCommands` exists. The
gate in play (locked, warned, confirmed) is an owner check, not yet seen.
