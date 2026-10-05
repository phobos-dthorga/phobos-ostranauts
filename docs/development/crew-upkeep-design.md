# Crew upkeep on long hauls

Design record for Framework 0.111.0 and the matching Shipbreaker 0.82.0,
Manufacturing 0.55.0, Agriculture 0.63.0 and Medical 0.5.0 releases. The player
guide is [Upkeep on long hauls](../crew-automation.md#upkeep-on-long-hauls).

Status on 6 October 2026: built and checked offline. Nothing here has been seen
in the game yet.

## The request

Owner, 6 October 2026: give crew useful work on long-haul flights, when there is
little to do. Crew tune Phobos machines for a small efficiency gain each time,
coded to be cheap to run. Further ideas were invited.

## Decisions

| Decision | Whose | Value |
| --- | --- | --- |
| Benefit of a tune | Owner | Faster work, up to about 10%; yields and mass balance never change |
| How a tune lasts | Owner | Fades with the machine's running hours; idle machines keep it |
| Further ideas taken up | Owner | Inspection rounds, practice at machines, housekeeping |
| Inspection length | Owner | 5 game minutes, a player setting |
| "Make a few settings modifiable" | Owner direction, agent choice of which | `InspectionMinutes`, `TuningMinutes`, `MaxTuningGain`, `TuneFadeHours` |
| Setting defaults and ranges other than the inspection length | Agent default | See the guide's settings table |
| Session step 0.2, skilled 0.3 | Agent default | `upkeep` pack |
| Inspection good for 24 game hours; inspected fade share 0.5 | Agent default | `upkeep` pack |
| Planner every 10 real seconds, two tasks a pass, unclaimed tasks withdrawn after 60 s | Agent default | Code constants in `Upkeep` |
| Skill for Manufacturing machines: the game's `SkillEngMechanical` | Agent default | Manufacturing has no speciality of its own |
| Skill for Medical inspections: the game's `SkillMedicalTrauma` | Agent default | |

All figures are authored balance, not measurements.

## Three things that differ from the approved plan

**1. A tuned machine draws more power.** The plan said the rate "multiplies
progress only" and that energy per job stays the same. Those two cannot both
hold: most Phobos machines count progress by the electricity they receive, so
more progress for the same draw would make each job cheaper in electricity. To
keep the owner's "same electricity per job", a tuned machine asks the game for
more power while it works (`Upkeep.Draw`), gives off that much more heat, and
its seconds count for that much more. Energy per job is unchanged; power while
working rises by up to 10%. The guide, panel line and changelogs say so. The
owner may prefer the other reading (same power, cheaper jobs); it is a one-line
change per service.

**2. Inspection does not slow wear.** The offer to the owner said an inspected
machine "wears more slowly". Phobos machines take no wear from running: damage
comes only from the game's own fire, combat and accidents. Inspection instead
halves the fade of the tune while it is good, and reports in the crew log what
the machine is waiting for or that it is more than half worn.

**3. Two machines are inspected only.**

- The F6 furnace: the plan tuned its melt hold time. The hold is 60 seconds
  (`FurnaceRules.HoldSeconds`), so a full tune would gain six seconds a melt, not
  worth touching the hot-batch physics for.
- The A2 regulator: it holds a set point and has no work rate to raise.

## How it works

```mermaid
flowchart LR
    Switch["Player record PhobosUpkeep: tune, inspect"] --> Plan["Upkeep.Plan, every 10 s, from CrewWork.Poll"]
    Plan --> Job["CrewWork.Job with Upkeep kind: a native task"]
    Job --> Complete["Upkeep.Complete: raise the level or stamp the inspection"]
    Complete --> Record["Machine record upkeep: level, inspected"]
    Record --> Draw["Upkeep.Draw at the machine's power request"]
    Draw --> Fade["Level fades by the seconds worked"]
```

- **One ship-wide order.** A machine has one crew order and one provider, so a
  tune order per machine would clash with existing orders. Upkeep jobs are
  `CrewWork.Job` records with no provider; `CrewWork.Allowed` checks the upkeep
  switch instead of a machine's order.
- **Lowest priority.** The planner runs only when a switch is on, after the
  standing-order steps, and offers at most one task per idle on-shift crew
  member.
- **The effect.** Each service calls `Upkeep.Draw(co, ref amountKWh, seconds)`
  where it asks for power, only while working. It returns the rate
  (`1 + MaxTuningGain x family share x level`), scales the request and fades the
  level by `seconds / TuneFadeHours`, at the inspected share while an inspection
  is good. The service scales its heat check and any progress counted in seconds
  by the same rate.
- **Records.** The level is cached and written only when it moves a whole
  percent or reaches none, so a running machine does not save every step.
- **Loss.** A mode switch to a damaged or loose form clears the tune.
- **Time-skip.** `Upkeep.SkipStep` banks on-shift crew time not held by an order
  and spends it on sessions, charging the walk by tile distance. It does not
  change the skip's stepping or `RepairShare`.

## Where each mod hooks in

| Mod | Site | Note |
| --- | --- | --- |
| Manufacturing | `BeginPower` of the processor, Sabatier, cracker, filler, bottler and feeder; `ChargeMachine` | Charge progress uses `elapsed x tune` |
| Shipbreaker | `PowerPatch.Prefix` for the D4 and R4; `ThawService.BeginPower`; `LaserService.BeginPower` | D4 progress is `elapsed x tune`; the laser counts work by energy received |
| Agriculture | `Service.Requested`; `CropState.Step(..., tune)`; the W2's delivery budget | Cooker and bench count work by energy received |
| Medical | Registration only | The bed excludes its patient |

## Saved structures

| Structure | Where | Migration |
| --- | --- | --- |
| Upkeep switches | New Phobos record `PhobosUpkeep` on the player | None: missing means off |
| Machine upkeep | New Phobos record `upkeep` on a tuned or inspected machine | None: missing means untuned |

## Checks

- Framework rule checks (`UpkeepChecks`): rate, fade, inspected share, session
  steps, planning order, record round trip and old records, skip sessions,
  settings clamps and pack refusals.
- Agriculture: a rate of 1 grows exactly as before; 1.1 ripens wheat in 1/1.1 of
  the time on the same energy with the same biomass, water and nutrients; a tune
  without the extra power grows nothing extra.
- The `upkeep` pack has the game loader's validator, the offline checker, a JSON
  Schema and Python tests.
- Not covered by a test: the Manufacturing and Shipbreaker services at a rate
  above 1 (their power steps need the game's `Powered`), the planner against a
  live crew, and the panel tab. These need play.

## For the owner to try

1. Switch on Tune machinery; idle on-shift crew walk to machines and tune them.
2. A tuned machine's panel shows the gain, and its power draw rises with it.
3. A time-skip on a long haul keeps machines tuned without slowing the skip.
4. Inspection lines appear only for machines that need attention.
5. Switching both off stops new upkeep tasks at once.

## Still to do (Set B)

Practice at machines and housekeeping, as approved, after this set has been seen
in play. Performance at 16x with upkeep on is the case to capture (finding L71).
