# Press twice to go ahead: panel override audit

Design record for the rule that a control panel never refuses a change only because
other steps must come first. Framework 0.125.0 delivers the shared mechanism;
Agriculture 0.66.0 is its first consumer. Manufacturing, Shipbreaker and Auto Nav
follow in their own rounds. Classification lives in
[`config/panel-override-audit.json`](../../config/panel-override-audit.json), and
`python scripts/audit-panel-overrides.py --check` enforces it.

## The rule (owner, 6 October 2026)

The owner reported two refusals on a Firstlight-4 rack's Water connection page:

- "One of these machines is already linked. Unlink it before choosing a new pair."
- "Pause both machines and their water intake first. Both must be working, unlocked
  and on your ship, with no saved-settings fault."

Owner direction: "can overrides be done just automatically on the player's part, no
questions needed? Even have them click the apply button twice if needed, with a
warning given on the first (that should actually be the expected modal for this
kind of thing henceforth, retroactively at that)." On reviewing the plan, the owner
set the scope: every control panel screen across all Phobos mods.

So, on every panel screen and F3 command, a change or button that needs other steps
first is never refused for that reason. Such steps include pausing machines or
intakes, removing an old link, applying a draft, stopping work and cancelling a
batch.

1. The first press warns in plain words what will be done, including any progress
   lost.
2. A second press on the same choice does it through the service, then lets the
   paused machines carry on.
3. Conditions the service cannot clear are still refused, with a reason that names
   the machine and what to do.

## How it works

```mermaid
sequenceDiagram
    participant P as Panel (PressGuard)
    participant C as Confirmations
    participant S as Service
    P->>C: Press(seen = null)
    C->>S: command
    S->>C: Ask(warning)
    C-->>S: not confirmed, offer recorded
    S-->>P: refused with the warning
    Note over P: arms the button, which reads "... again"
    P->>C: Press(seen = warning)
    C->>S: same command
    S->>C: Ask(warning)
    C-->>S: confirmed (same warning)
    S->>S: PausedChange.Hold, the change, Release
    S-->>P: done, with any machine that could not carry on
```

- **One channel.** A service decides once. It works out the steps, then calls
  `Confirmations.Ask(warning, confirmed, out message)`. A panel runs each press
  through `Confirmations.Press` or a `PressGuard`, passing the warning the player saw
  on the first press.
- **No blind go-ahead.** A second press whose warning has changed, because something
  changed in between, is a new offer rather than a go-ahead.
- **F3.** A trailing `confirm` word confirms (`Confirmations.TakeWord`). The service
  receives the action with the `confirm:` prefix and strips it (`Confirmations.Split`).
- **Pausing for a change.** `PausedChange` holds only machines that were working. Its
  pause never ends a standing crew order. It resumes them last first, through each
  machine's own checks, and returns the reason for any that could not carry on.
  Applying a setting from a panel still makes an enabled order on that machine wait
  for Resume (`ConfigurationStamp.SuspendChangedOrder`, from the 27 September panel
  redesign); that rule is unchanged and is an open question for the owner.
- **Knobs and guarded switches.** These cannot take a second press, so they confirm
  through a `ChoiceCard` (`Confirmations.PressWithCard`) with the same warning.
- **Nothing is saved.** An offer lapses when another press, a changed selection,
  Discard, Back or closing the panel disarms it.

## Classes

| Class | Meaning |
| --- | --- |
| offered | The second press is in place |
| pending | A bookkeeping case (A) or one that loses progress (L), to be converted in the named round |
| refused | Stays a refusal: heat, contents, a fault, damage, another console, or a choice only the player makes |
| status | Matched the wording but is not a do-first refusal (a status line, a label, a requirement) |
| unused | No code uses the text; remove it in the named round |

The check scans the English catalogs of the mods listed in the record for texts and
keys that read like "do this first". Descriptions, help, settings, removal checks
and story text are skipped. Every match needs an entry or a key-pattern rule, and an
entry whose key no longer exists fails the check.

## Delivered in Framework 0.125.0 and Agriculture 0.66.0

- **Framework:**
  - `Confirmations`, `PressGuard` and `PausedChange`.
  - The second press in `ConfigurationSheet` (every settings Apply) and in
    `ProviderPanel` command buttons.
  - Crew panel **Resume** with unsaved changes now applies them on the second press;
    `Console.apply_first` used to refuse.
  - The silo's mixed refusal is split: `WaterTanks.not_ready` (the silo itself) and
    `WaterTanks.transfer_busy` (a transfer still running).
- **Agriculture, each with a second press:**
  - **Water links:**
    - linking a rack to a W2, including a rack already linked to another W2;
    - unlinking at a rack or at the W2;
    - choosing pipe-fed or manual water.
    The rack, the old W2 and the new W2 pause for the change and carry on; the old W2
    keeps feeding its other racks.
  - **W2 settings:** the W2's nutrient source, its water target and its silo link.
  - **Recycler capture link:** an old link on either end is replaced, and capture
    that was on carries on.
  - **B2 bench:** cancelling a workup or emptying the straw press while it works. The
    cancel used to fall through to the F3 help text.
  - **W2 recovery:** cancelling drainage recovery while it works.
  - **Hearth-2 cooker:** when a batch's portion has gone missing, the second press
    cancels the batch (the warning gives the share of cooking lost) and starts a new
    one. Putting the portion back instead keeps the batch.
- **Agriculture refusals reworded:** `water_pause` split into `water_not_ready` and
  `water_wrong_kind`, with the pausing made automatic. A full W2 (`water_bank_full`)
  and old-model line water (`line_drain`, `line_drain_named`) still refuse.
- **Agriculture keys removed:** `water_pause`, `press_pause`, `cancel_missing` and
  the unused `water_pair_help`.

## Agent choices (revisable by the owner)

- **Silent steps stay one press.** Steps that already happened without a refusal
  keep happening that way: a vessel link replacing the old one, a laser cooling
  unpair, a reserve pausing its W2s. A second press there would add friction the
  owner asked to remove. Their messages name the extra step; setting a silo reserve
  now names the W2s it paused.
- **Active Auto Nav flights.** Stopping an active flight to change it will be a
  second press whose warning says the ship coasts on its present course with thrust
  cut. This follows the owner's preference for offering risky actions with warnings
  rather than refusing them.
- **G4 moorings.** Releasing a G4 capture mooring to choose another target will be a
  second press with an unmoored warning, on the same grounds.
- **Collectors held by Shipbreaker.** A collector held by a Phobos Shipbreaker route
  stays refused from Agriculture's recycler, naming what holds it. Agriculture does
  not manage Shipbreaker's pairs.

## Delivered in Manufacturing 0.58.0

- **Links and modes on a working machine.** Changing a link on any of the six charge
  machines, the X2 processor, the K2 Sabatier reactor, the AX-2 cracker or the
  bottler, or the L2 station's mode, pauses the machine on the second press. The
  bound batch is kept, and the machine carries on through its own Start.
- **Recipes and optional charges with a batch bound.** The second press cancels the
  batch. The warning gives the share of its work lost; its supplies go back to the
  inventory.
- **Start with a cluttered feed.** Start offers to put the inner feed's other items
  back in the inventory, then takes the charge in.
- **F3.** A trailing `confirm` reaches every machine through the provider (the
  Manufacturing-local `Overrides.Confirmed`).
- **Removed:** 18 refusal keys (`*.link_busy`, `*.prefer_busy`, `Leach.select_busy`).
  `Content.feed_blocked` stays as the status of an automatic repeat.

## Delivered in Shipbreaker 0.85.0

- **Collector and processor pairs.** An endpoint paired elsewhere is unlinked from its
  old partner on the second press, and collection that was on carries on.
- **T2 and ML-2.** A working T2 pauses for a link change. A cutting ML-2 pauses for a
  filter or cooling change and carries on. The laser now checks a hot assembly, old
  or new, before changing anything.
- **G4 capture.** An approach under way is stopped, or a ship moored for capture
  released, on the second press. Captures still settling are refused with their own
  text.
- **D4 and R4 product store.** An unreadable saved store choice is cleared on the
  second press, after a warning.
- **F6 furnace.**
  - Cooling, recipe, coolant-service and continue refusals name the one blocking
    condition instead of a list.
  - Heating permission alone is withdrawn on a second press for a recipe or coolant
    service. The furnace keeps its heat, and its resume stays explicit (AGENTS
    Shipbreaker rule).
  - No furnace knob or guarded switch offers a second press today, so none needed a
    card.
- **Hosts.** The IndustrialPanel's buttons (local and C1) use a `PressGuard`; an offer
  shows as a warning, not "rejected".
- **F3.** Shipbreaker's single F3 entry strips a trailing `confirm` for its own
  commands only. `IndustryService.Run` strips the prefix and passes it on to the
  providers it reaches.
- **Removed:** nine keys (`Furnace.hot_maintenance`, `Furnace.resume_block`,
  `Furnace.recipe_block`, `Furnace.charge_service`, `Capture.stop_rebind`,
  `Thaw.link_busy`, `Laser.filter_busy`, `Laser.cooling_busy` and the unused
  `CollectorPanel.one_sender_per_receiver_unlink_before_changing`).
- **Still refused:** the shared pairing refusal for furnace and laser cooling, where
  the other end serves other equipment and unpairing it is limited by temperature.

## Delivered in Auto Nav 0.35.0

- **Flights and docking.** Approach, Dock, Approach & Dock, Rendezvous, Follow and
  departure no longer refuse because a flight, a suspended flight, a departure or a
  positioning job holds the console.
  - The new target is checked first.
  - The second press stops whatever holds the console through `Stop`, exactly as the
    Stop button does, so thrust is cut and the ship coasts. The new flight then starts.
  - This is the agent choice listed above. `ClearFlightFor` in
    `NavigationPersistence.cs` is the one place it happens.
- **Departure.** An unfinished saved departure is cancelled on the same second press.
  One still detaching, or an unreadable record, still refuses.
- **Hub drafts.** Unsaved settings no longer disable the flight buttons. The first
  press says they will be applied (`Hub.apply_first`), and the second applies them
  and acts.
- **Weapons.** A weapon group this console holds is returned to ship controls on the
  second press of another group.
- **Settings during a flight.** A setting changed while a flight runs or waits is
  saved for the next flight instead of refused, and the message says so. The running
  or waiting flight keeps its captured profile. This is an agent choice: it removes
  the refusal without stopping anything. Two Auto Nav checks that asserted the old
  refusal now assert the new rule.
- **Refusals that name one cause.** Departure, combat and industrial positioning now
  name the one blocking condition. Industrial positioning is requested by Shipbreaker,
  so a flight it would interrupt stays the player's to stop at the navigation console.
- **F3.** `NavigationService.Command` takes a trailing `confirm`. A check confirms
  that `fly confirm` stops a suspended flight.
- **Removed:** ten refusal keys, the unused `Instruments.arrival_locked` and
  `NavigationService.stop_approach_assist_s_test_pulse_first`, and the uncalled
  `StepPanelSpeed`. `SetPanelTorch` and `StepPanelArrival` stay: the instrument
  checks call them.
- **Tests.** The Sensors, Docking and Coupled check projects now reference
  Newtonsoft.Json and link `Overrides.cs`, as Framework's text catalog needs.

With this round no do-first refusal is pending.

## Verification and limits

- **Offline checks:**
  - Framework: `ConfirmationChecks` covers the parser, the decision rule, offer and
    re-offer, `PressGuard` arming and disarming, and `PausedChange`.
  - `tests/test_panel_overrides.py` covers the audit check.
- **Not unit-tested.** Agriculture's override paths act on game objects, so offline
  tests cannot construct them. They compile and are not otherwise exercised.
- **Owner checks in play:**
  - relink a rack to another W2: the rack keeps growing and the old W2 keeps feeding
    its other racks (an enabled crew order on the rack waits for Resume, as with any
    applied setting);
  - change a W2's nutrient source while it pumps;
  - press Resume on the Crew panel with unsaved changes.
