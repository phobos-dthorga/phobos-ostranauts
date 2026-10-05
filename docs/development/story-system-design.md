# Story and worldview content: design record

Phase 1: Framework 0.107.0, Agriculture 0.60.0. Phase 2 (small talk, loading tips and
encyclopedia articles): Framework 0.108.0, Agriculture 0.61.0. Owner request, 6 October 2026: Framework code
and a schema so story and worldview content can enter the game through TV broadcasts,
people talking about topics, and goals the player picks up; written easily by ChatGPT
(the creative work) while Claude writes the code. The player guide is
[Writing story content](../writing-story-content.md).

## Owner decisions (6 October 2026)

| Question | Decision |
| --- | --- |
| How goals work | Framework story arcs: ordinary game goals run by Framework, not the game's plots |
| First release | News, adverts and arcs. Crew chatter follows after a code spike |
| Who ships content | Each mod ships its own story pack; Framework holds the code and schema |
| Text | Inline English in the pack, replaceable through a translation key |
| Diagrams | GitHub Mermaid charts |

## What the game offers

Read-only inspection of the game's assembly (Ostranauts 1.0.1.5) and its data folder;
no decompiled source was saved. **Observed** means seen in that inspection.

- **TV news (observed).** `CCTV.Update` builds the TV text by asking
  `DataHandler.GetHeadline()` for one headline at a time (a header "Region News:" when
  the region changes) and `DataHandler.GetAd()` for one advert at a time. Each picks
  uniformly from `dictHeadlines` (43 headlines; regions Shipping & Inner System,
  Tharsis, Outer System and a few topics; up to 652 characters) or `dictAds` (27 adverts,
  up to 374 characters). Nothing about either is saved. `CCTV.Update` is the only caller
  of `GetHeadline`.
- **Plots are saved by name (observed).** Saves keep plot names (`aPlots`), objectives
  that name their plot, player conditions and pledges. `ObjectiveTracker.LoadObjectives`
  looks a saved plot objective up by name, and the GOALS panel
  (`ObjectivePlotPanel.SetData`) reads the plot without a null check, so a removed plot
  throws whenever the panel draws. Added content must never use native plots.
- **Ordinary goals (observed).** `new Objective(co, title, ctName)` and
  `ObjectiveTracker.AddObjective` add a goal; a goal is complete when its completion
  test (`CondTrigger`) passes on its target. A save keeps the goal's title, description
  and test name (`Objective.GetJSON`). On load the test is looked up by name through
  `DataHandler.GetCondTrigger`, which logs "No such CT" for an unknown name and returns a
  copy of the game's always-true `Blank`. `ObjectiveTracker.CheckObjective` then finishes
  the goal through `RemoveObjective(..., REASON_COMPLETED)`.
- **Finished goals stay listed (observed).** `RemoveObjective` marks a goal finished
  and logs "OBJECTIVE_COMPLETE"; nothing removes it from `_allObjectives`, and saves keep
  it with `bFinished`. `AddObjective` refuses a goal equal to one already listed (same
  target, test, plot and title), and `CanShow` refuses one titled like a goal shown in
  the last ten seconds.
- **Dismissal (observed).** The GOALS panel's dismiss button calls `RemoveObjective`
  with `REASON_DISMISSED`, which for a plot goal also cancels its plot.
- **No topic system (observed).** Each small-talk line is a fixed social interaction,
  and character history stores interaction names. A line such as `SOCMentionHeadline`
  mentions "a headline" without saying which.

## How it works

```mermaid
flowchart LR
    Packs["Story packs: Framework settings, each mod, add-ons, player files"] --> Library["StoryLibrary: merged, names checked"]
    Library -->|"share of picks"| News["GetHeadline and GetAd postfixes"]
    Library --> Runner["StoryArcs: check every 30 s"]
    Runner -->|"message, bulletin"| Player["Crew log and next TV news"]
    Runner -->|"Objective with PhobosStory test"| Goals["GOALS list"]
    Runner -->|"tests pass: take items, close goal, reward, next step"| Goals
    Record["PhobosState.PhobosStory on the player"] <--> Runner
    Goals -->|"dismissed"| Runner
```

| File | Role |
| --- | --- |
| `Story/StoryPack.cs` | Entry types and `StorySchema.Validate`: ids, lengths, placeholders, test fields |
| `Story/StoryLibrary.cs` | Merges packs in load order; refuses a reused id, unknown items, conditions, arcs and bulletins |
| `Story/StoryRecord.cs` | The player's record, and `StoryRules`: eligibility, tests, weighted picks, step lookup, goal names |
| `Story/StoryContent.cs` | Pack registration, load on `ContentLoaded`, mods by name, translations, F3 commands |
| `Story/StoryArcs.cs` | The runner, game facts, goal tests, rewards, dismissal postfix |
| `Story/StoryNews.cs` | The TV postfixes |

- **Packs.** Framework's own pack holds the settings and no stories. A mod registers
  its pack once in Awake (`StoryContent.Register`); all packs are reread with player
  and add-on files on every game load, after every mod has published its items, so the
  names a pack uses can be checked against the game's tables.
- **News.** A queued bulletin goes out first. Otherwise, with probability
  `broadcastShare` (0.3), a pick comes from the story news eligible at the last check,
  weighted; the rest stay the game's own. Adverts likewise with `advertShare`.
- **Arcs.** Each check finishes the active steps whose tests pass, may start one
  available arc (by its `chance`, while fewer than `maxActiveArcs` are active), and
  rebuilds the news pools. A step's goal is re-offered if the game did not take it.
- **Goal tests.** Each step with a goal gets the test `PhobosStory.<arc>.<step>`,
  registered on every load and requiring the hidden condition `IsPhobosStoryGoalOpen`,
  which nothing ever sets. The goal therefore never finishes by itself; Framework
  finishes it through the game's own `RemoveObjective`, so the game logs and sounds the
  completion as for its own goals. Before showing a goal again (a repeated arc), our
  finished copy is removed from the game's list so the game does not refuse the new one.
- **Rewards.** Items are created with `DataHandler.GetCondOwner`, added to the player's
  inventory and dropped at their feet when they do not fit. Consumed items are taken
  unit by unit, stack members first; if fewer than needed remain at that moment, the
  step waits.

## Save footprint

| Saved | Where | If the pack is removed | If Framework is removed |
| --- | --- | --- | --- |
| Arc progress, once-only news shown, queued bulletins | `PhobosState.PhobosStory`, a property map on the player | Entries are ignored and kept; restored with the pack | The map stays unread on the player |
| A goal's title, description and test name | The game's own goal list | The unknown test reads as `Blank`, so the goal finishes and disappears at the next goal check | The same |
| The hidden condition | Nowhere: it is never set | — | — |

No plots, pledges, social interactions, player conditions or new items enter a save.
The record keeps unknown fields and arcs exactly as read, so an older Framework never
destroys what a newer one wrote.

**Removal sequence (inferred from the observed lookups, not yet seen in play).** With
the pack gone, the game logs "No such CT: PhobosStory..." once per goal on load, and the
goal completes the next time the game checks the player's goals (`CheckObjective` runs
on many events, such as pausing). The player sees an ordinary "complete" line.

## Phase 2: small talk, loading tips and encyclopedia (Framework 0.108.0)

Owner request, 6 October 2026: proceed with the next set. Owner choices:

| Question | Decision |
| --- | --- |
| Scope | Small talk, loading-screen lore tips and encyclopedia articles, in one release |
| Who may voice a line | Anyone the game has chatting; each line may narrow it to the player's crew or to people elsewhere |

Agent choices, open to revision: the shares (40% of matching small talk, 30% of tips),
the nine moments and their lead-ins, and Framework's two shared encyclopedia sections
(Makers and brands; Life between stations).

**What the game offers (observed, same inspection method as above).**

- **Small talk.** Every social interaction becomes text through
  `GrammarUtils.GenerateDescription(Interaction)` or `(Interaction, bool)`, which return
  `GetInflectedString(strDesc, interaction)`; the tokens such as `[us]` and `[mentions]`
  are inflected there. Callers include the social log (`Interaction.ApplyLogging`, which
  logs to people in the room or aboard), the conversation screen
  (`GUISocialCombat2.SetData`), ship comms and tooltips. Character history stores the
  interaction's name, never its text. The social openers suited to carrying a topic
  are SOCMentionHeadline ("mentions a headline ... that [us-subj] recently read"),
  the jokes, complaints, stories, jargon, superstition, worries, a metaphysics
  question and shooting the breeze.
- **Tips.** `DataHandler.GetTip()` picks uniformly from `dictTips` (23 lore tips, up to
  444 characters, each starting with a line break) during the loading-screen fade.
  Nothing about it is saved, and no player exists then.
- **Encyclopedia.** `Info.Init` runs on `DataHandler.LoadComplete` and builds its tree
  from `dictInfoNodes` (`BuildHierarchyFromJSON`). A node with an empty parent hangs
  under the index; any other parent is looked up with the dictionary indexer, which
  throws for an unknown name. An empty `strImage` is drawn with the encyclopedia's logo
  and a blank image colour (`DrawMainWindow`). The game ships 68 nodes in one section.
- **Saves.** The save's `aCustomInfos` holds PDA notes, overlays, timers, presets,
  filters and the quick bar (`CrewSim.CustomInfosString`); nothing records which
  articles were read.

**How it works.**

| Channel | Mechanism | Saved |
| --- | --- | --- |
| Small talk | Postfixes on both `GenerateDescription` overloads (`StoryChatter`). For an interaction in a moment, a share of uses picks an eligible line the speaker may voice and returns `GetInflectedString(lead-in + line, interaction)`. The choice is remembered per interaction instance, checked against its name and speakers, so the log and the conversation screen agree. Lines are eligible by the player's facts at the last 30-second check; the speaker test (aboard one of the player's ships or not) is made at the moment of speech. | Nothing: only text changes |
| News mentions | A broadcast's `mention` is a `headline` line with the broadcast's weight and requirements | Nothing |
| Tips | `GetTip` postfix (`StoryLore.Tip`); entries may require only mods, which are checked against loaded plugins | Nothing |
| Encyclopedia | On each content load our `PhobosStory.` nodes in `dictInfoNodes` are replaced by the current sections and articles; a prefix on `BuildHierarchyFromJSON` applies them again in case the tree is built first. A section is published only with an article to show, and an article's parent is always its section's node | Nothing |

**Lead-ins** (`Story.moment.<moment>` in Framework's catalogue) use only grammar tokens
found in the game's own social lines, so the game inflects them as it does its own: the
native checks compare every token against the loaded interactions.

**Limits.**

- Story lines take the place of the game's line for that use; the game's own topics
  still come up the rest of the time.
- The speaker test reads ship ownership only: a guest aboard the player's ship counts
  as crew, and the player's crew visiting a station count as crew while aboard the
  player's ship.
- A line chosen for an interaction stays with that interaction object; if the game
  reuses one between the same two people with the same opener, the line repeats.
- The encyclopedia tree is built once a session; an article added by a file changed
  mid-session appears after the next game start (unobserved: whether the game rebuilds
  the tree on a later content load).

## Limits of phase 1

- Goals and news concern the player character. A player who switches to another
  character starts with that character's record.
- `install` and `owns` count objects on the player's loaded ships, installed or lying
  aboard; a ship out of range is not counted.
- Story steps advance at the 30-second check, so a goal can finish up to half a minute
  after its test is met. A time-skip advances `wait` tests by game hours as usual.
- A step whose saved id and position both vanish from its pack is set aside, with a log
  line; `story reset` starts it again.

## Later work

What the owner's request covered but phase 2 does not (small talk, tips and encyclopedia entries were delivered in phase 2). The save-risk column uses
the same test as above: does a name of ours enter saves, and what happens if it goes?

| Item | What it would add | What we know / what it still needs | Save risk |
| --- | --- | --- | --- |
| **A Framework talk opener** | Topics as their own kind of conversation | Phase 2 borrows the game's own small talk. A new opener only if that proves too limited. The crew pick openers by learned weights (`ai_training`), so a new opener may rarely fire. | One Framework-owned name in conversation history (acceptable, like Framework items) |
| **Characters approaching the player** | A contact walks up and hands over a goal | The game does this with pledges, saved by name on characters. A Framework version needs its own approach behaviour. | Pledges refused; a Framework version needs its own design |
| **Found data files** | Goals and lore found in the world | The game's data files are items saved by definition name. A safe version is one Framework-owned data file item whose text comes from a story pack by an id property; an unknown id reads as a corrupted file. | One Framework item definition (acceptable) |
| **Money and reputation rewards** | Paying out or changing faction standing | Money goes through the game's ledger, and faction scores are saved state of the game's own kinds. Needs a check that an entry left by a removed pack is harmless. | To assess; items only for now |
| **More tests** | Goals beyond the four kinds | Candidates: visit a ship type, reach a skill level, hold credits, own a machine family, talk to a kind of person. Each is code in the fixed vocabulary, added when content needs it. | None (code only) |
| **Time windows** | Content tied to dates or elapsed game time | The game's clock is `StarSystem.fEpoch`, and start dates vary by save; a "days since this game began" window needs the start date in our record. | None beyond our record |
| **Branching arcs** | Choices that lead to different next steps | The runner is linear; branching needs a choice screen or tests that pick the branch. | None beyond our record |
| **Encounter scenes** | Full-screen story scenes with pictures and choices | The game's encounters are interactions saved in history; ours would need Framework-owned ones and original art. | Names in history; needs design |
| **Translations of story text** | Other languages | The keys exist (`Story.<id>.<field>`); no translation work has started. | None |
| **Other mods' story packs** | Manufacturing, Shipbreaker, Medical, Auto Nav and War Has Been Declared content | Each mod registers its own pack as Agriculture does; only Agriculture ships a seed. | None beyond this release's rules |
| **Encyclopedia pictures** | A picture beside an article | The encyclopedia loads `strImage` as a PNG path; articles have none for now, and a picture would need original art and a field. | None |

## Verification

- `tests/PhobosFramework.Tests/StoryChecks.cs`: schema refusals, the merged library,
  the record round trip (unknown fields kept), eligibility, every test kind, picks,
  step lookup and goal names.
- `tests/PhobosNative.Tests/StoryNativeChecks.cs`: the shipped packs over the game's
  real data (every name exists), goal tests that never pass by themselves, and the game
  members the code relies on. `PatchResolutionChecks` covers the three postfixes.
- `tests/test_data_packs.py`: the Python mirror and the JSON Schema on the shipped
  packs and on broken copies.
- Phase 2: `StoryChecks.Phase2` (refusals, mention lines, moments, pools, speakers,
  encyclopedia nodes) and `StoryNativeChecks` (every mapped interaction exists, every
  lead-in token appears in the game's own lines, the seed's nodes have known parents,
  the tip and encyclopedia methods are where expected).
- **In play (owner, pending), phase 2:** story lines turn up in the social log among
  small talk, aboard and on stations; `phobosframework story chatter <id>` forces one;
  loading screens sometimes show a Verdemorrow tip; the encyclopedia lists Makers and
  brands and Life between stations with Agriculture's articles, and without
  Agriculture neither section shows.
- **In play (owner, pending), phase 1:** the TV shows the seed news among the game's own;
  `phobosframework story start verdemorrow-grain-sample` puts a goal in the GOALS list;
  carrying a portion of wheat grain finishes the first step, docking with it the second,
  with the reward; dismissing a goal sets the arc aside; with Agriculture's pack removed
  the goal finishes on load with only a "No such CT" line in `Player.log`.
