# Crew panel and Maintenance sheet clarity

Framework 0.117.0, prepared 6 October 2026. Not yet seen in the game; the owner checks
are at the end.

## What prompted it

The owner's screenshots of 6 October 2026 showed the Crew operations panel and the
right-click Maintenance sheet "spewing" information on the player:

- **Orders.** Opened from an X2 (which takes no crew orders), the page showed the generic
  "Select equipment" text and every machine on the ship, whatever its state.
- **Upkeep.** Four permanent help paragraphs on the left, and on the right three header
  lines repeating the switches, then one line per machine with no grouping.
- **Time-skip.** An hour-by-hour shift string for each crew member, and one paragraph per
  machine.
- **Maintenance sheet.** The same facts said three ways (a heading, a sentence, and the
  switches again), then two paragraphs on removal.

On narrow windows (under 1,000 px) the Upkeep switches and the Time-skip text were not
shown at all: those pages drew them in the list column and then asked for the detail
column only.

## Owner choices (6 October 2026)

1. **Focus.** Opened from a machine, Orders and Upkeep show that machine alone, with a
   Show all ship button. Opened from the roster, the whole ship.
2. **Lists.** Collapsible groups by state with counts, as the C1 console does. What needs
   the player is open; the rest is folded. One row per machine: name, state, and the one
   thing it waits for.
3. **Help.** Generic explanations go to the game's encyclopedia as a Phobos operations
   section. Each page keeps one short line and an About button that opens the article.
   Per-machine facts stay on the panel.

The owner asked whether a PDA app could hold the help instead. It can: the app icon is
data (`data/pda_apps`), and LOGUSS's Common Sense Cargo Manifest adds one. But the game
opens apps through a fixed list in `GUIPDA.OpenApp`, so a Phobos app needs Harmony patches
to open and close it and a screen we build ourselves, and it would break when that list
changes. The explanations are the same for every machine, so the encyclopedia holds them
as well. Framework already writes articles there from story packs. The owner chose the
encyclopedia; a PDA app stays a later option.

## What was built

- **`Controls/GroupedList`.** Collapsible groups lifted from the C1 console's equipment
  list: header buttons with "+" or "-" and a count, folds kept by the caller, and row text
  refreshed in place. The list is redrawn only when a machine changes group. The C1
  console itself is not retrofitted yet.
- **Orders.** `Crew/OrderGroups` gives three groups:
  - **Needs you** (open): Blocked and Stopped. A suspended order reads as Stopped with its
    reason; there is no separate state.
  - **Working**: Running and Waiting.
  - **Not set up or off**: Disabled and NeedsSetup.

  A machine with no provider gets a card: it takes no crew orders, how it is loaded (by
  hand, plus any feed or product store), and its upkeep.
- **Upkeep.** Each switch has its own row with a Turn on or Turn off button, and its answer
  goes to the footer. `Upkeep.Report` is one walk of the machine family, shared with the F3
  report. The list has three groups: Needs attention, Looked after and Inspection only. A
  chosen machine shows its `MachineReport`. The page now draws the list column by default,
  so a narrow window keeps the switches.
- **Time-skip.** `Crew/ShiftRuns` turns coming hours into runs. `CrewSkip.PreviewRows`
  groups the orders as Will run, Waits (with the reason) and Paused for the skip. The old
  `CrewSkip.Preview`, which nothing called, became the F3 `phobosframework skip` text.
- **Maintenance sheet.** One unheaded line for a machine without orders, a sentence for its
  upkeep (`Upkeep.Summary`), then the removal blocker or "Nothing blocks taking it up or
  dismantling it." An About link replaces the general paragraph.
- **Encyclopedia.** Framework's `story.json` gains the `phobos-operations` section and five
  articles: standing orders, upkeep, time-skips, store links and maintenance. Their text
  replaces the removed panel paragraphs (`Console.upkeep_*_help`, `skip_estimate`,
  `skip_native`, `orders_intro`, `MaintenanceInfo.basics`).
  - `StoryLore.Open` refuses with a reason when there is no game, the article is unknown, or
    the tree was built before the pack loaded.
  - Otherwise it calls `Info.instance.OpenToNode(name)`.
  - `Controls.Help.About` draws the button and writes the outcome to the footer.
  - F3: `phobosframework articles` and `phobosframework help <name>`.

### How the encyclopedia opens an article (evidence)

The local decompiled research copy (ignored `.local/decompiled-sensors/_all.cs`) shows
`Info.OpenToNode(string s)` looking `s` up in the public `mapNodes`. `BuildHierarchyFromJSON`
fills `mapNodes` by each node's `strName`, so our nodes open by
`StoryLore.NodePrefix + articleId`.

We do not set `strLookup`: the encyclopedia prints it in the article header. A native check
fails if `Info` loses `OpenToNode(string)`, `mapNodes` or `instance`. Whether the window
shows above the raised Crew panel has not been seen in the game.

## Agent defaults (owner may revise)

- **Group names and membership.** Needs you, Working, Not set up or off; Needs attention,
  Looked after, Inspection only; Will run, Waits, Paused for the skip. Needs attention and
  Waits start open; the rest start folded.
- **Folds.** Remembered per page while the game runs; not saved.
- **No upkeep asked for, no upkeep listed.** With both upkeep switches off, only an
  unreadable record needs attention. Right after Inspection rounds is switched on, every
  machine is due until the first round. That is the honest to-do list, and it matches the
  upkeep design's rule that inspection lines appear only where due.
- **The order editor stays as it was:** fields, Apply, Discard, Resume, Stop, and the
  Details & diagnostics toggle.

## Colour as a signal (Framework 0.118.0)

The owner asked (6 October 2026) whether buttons, tabs and cards should be coloured by
priority where that helps, and not where it would mislead. Before this, colour meant almost
nothing: green marked a selection or Apply, amber Stop, slate structure, and a blocked
order's card looked like a running one's.

**Owner choices.** Scope: the Crew panel and the Maintenance sheet (the shared Control Panel
and the C1 console are untouched). Apply and Resume light only when they are the next step.

**The rule.** The owner's standing rule is that states are distinct text and colour is
supplementary (`industrial-control-mockups.md:226`), so every tint sits beside a word or a
count saying the same thing. A player who cannot tell amber from green loses nothing. Three
meanings, using the panel's existing three colours (`Controls/Tone.cs`):

| Tone | Colour | Where |
| --- | --- | --- |
| Attention | Amber | Needs you and Needs attention headers, Waits; stopped or unreadable order cards; upkeep cards that are due; a tab with a count; the sheet's buttons when what they open needs you |
| Good | Green | Running order cards; Will run; Apply with changes pending; Resume when ready |
| Neutral | Slate or plain | everything else |

**Agent defaults (owner may revise):**
- **No red.** Nothing on these panels is an alarm. Red would compete with the game's own
  danger colours and would cry wolf over a stopped order.
- **No priority ranking beyond "needs you".** A finer ranking (urgent, soon, later) would
  invent judgements the data does not support, which is the misinformation the owner was
  wary of.
- **Waiting stays plain.** An order waiting for feed or crew is the crew's business, not the
  player's.
- **Stop stays fixed amber.** It is a safety control: one place, one colour.
- **Status cards lean 30% toward their tone.** The text stays the panel's usual ink for
  contrast.
- **Tabs count, then tint.** "Orders (2)" carries the signal; the tint only repeats it. The
  current tab keeps the slate selection.

## Saved structures

None. Nothing new is saved, the folds are not saved, and the removed text keys were panel
text only.

## Risks and limits

- **Encyclopedia layering.** The window may sit under the Crew panel; if so, the About
  service should lower the panel first.
- **Late-loaded packs.** A pack loaded after the encyclopedia tree is built is not in it;
  About says so and asks for a restart.
- **Player translation files** that still carry the removed keys are harmless: unknown
  keys are ignored.
- **Not yet seen in game.** The owner has not seen 0.116.0's sheet in the game either; this
  release changes it again.

## Owner checks

1. **Maintenance → Open standing orders on an X2:** a card saying it takes no orders and how
   it is loaded, its upkeep, and Show all ship. On a V4: its order card.
2. **Show all ship:** groups with counts; Needs you open; folding works; search still
   filters.
3. **Upkeep:** the switch rows; turning Inspection rounds on moves machines into Needs
   attention, and the footer reports the switch.
4. **Time-skip estimate from the game's time-skip screen:** each crew member's hours read as
   one line; orders sit in Will run or Waits.
5. **About** on each page and on the Maintenance sheet opens the right encyclopedia article,
   and the footer says so.
6. **Narrow window (under 1,000 px):** the Upkeep switches and list are still reachable.
