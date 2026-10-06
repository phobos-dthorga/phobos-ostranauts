# Phobos Spacer Stories: rewrite handoff for ChatGPT

Written 6 October 2026 for ChatGPT, which drafted the collection and now rewrites it.
The owner disabled the mod because its stories "come and go out of nowhere with
absolutely no relation to anything or anywhere". Phobos Framework 0.114.0 and 0.115.0
added the means to fix that: places, people, threads, story flags and new
requirements. Your job is to move the collection onto them.

Read these first:

- The player guide [Writing story content](../writing-story-content.md), above all
  [Where things happen](../writing-story-content.md#where-things-happen-places-people-and-threads),
  [Requirements](../writing-story-content.md#requirements) and
  [Text](../writing-story-content.md#text). It is the authority on every field.
- The collection's [authoring record](spacer-stories-authoring.md), for what each file
  and chain is about.
- The nine files in
  [`mods/PhobosSpacerStories/phobos/PhobosFramework/story/`](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story).

## Who does what

| You (ChatGPT) | Claude, after you |
| --- | --- |
| Edit the nine story files, and add new story files beside them if a new one is clearer | Run the checks below and fix anything they refuse |
| Keep the writing voice: practical, worn-in, dry humour in routine lines | Raise the mod to 0.2.0 and its Framework minimum to 0.115.0 |
| List what you changed and why, per file, in your reply | Write the changelog, update the authoring record, fix the authorship labels |
| Flag anything you could not do within the rules | Re-enable the mod for the owner's play test |

Edit nothing outside the story folder: not `phobos-addon.json`, `mod_info.json`,
`CHANGELOG.md`, `THIRD-PARTY.md`, the README, the Workshop files or any version number.
Those go through the project's own tools.

## Strictly required

A file that breaks one of these is sent back.

1. **Keep every id.** Never rename or delete an existing entry id or an arc's step id.
   Saves hold them. You may reword any text, change any gate, and add new entries.
2. **Everything belongs to a thread.** Every arc, news item (`broadcasts`), advert,
   small-talk line (`chatter`) and data file (`files`) names a `thread`. Every thread
   names a `place` (its home). An entry may name its own `place` to override the
   thread's.
3. **Use only Framework's places.** Thread and entry places come from the
   [place keys](#place-keys) below. Do not invent places or write station names into
   text: use `[place]`, `[region]`, `[station]` and `[body]`, which fill from the place.
4. **Letters come from people.** Create a `people` table. Each of the seven current
   senders becomes a person with `name`, `role` and a `home` place. Every arc message
   (`delivery.message` and `onComplete.message`) uses `"person": "<id>"` and drops
   `from`. Each thread with letters lists its senders in its `people` cast. Name
   people in text with `[person:<id>]`.
5. **Nothing tells a story before it happens.** No entry may mention an arc's people
   or events before the player could know them. Gate it with `arcsActive`,
   `arcsAtStep`, `arcsDone`, `filesRead`, `newsSeen` or a story flag. The
   [known spoilers](#known-spoilers) below must all be fixed.
6. **Tips and encyclopedia articles stay general.** They show at loading screens and in
   the encyclopedia, where there is no player to check, so they can require only
   `mods` and take no placeholders. Reword them as background (the company, the trade,
   the setting); do not name an arc's people or what happens in an arc.
7. **Docking means somewhere.** Replace `"station": "any"` in `dock-at` tests with
   `{ "kind": "dock-at" }`, which means the arc's own place, unless a step really is
   meant to finish at any station; say so in the step's notes when you keep `any`.
   Drop `"dockedAt": ["any"]` from arc requirements: an arc with a place already
   starts only there.
8. **Regions come from places.** Remove `"region": "Outer System"` from news items in a
   thread; the place gives the label. Keep `region` only on news with no thread, using
   `Shipping & Inner System`, `Tharsis` or `Outer System`.
9. **Remove meaningless gates.** Delete every `"mods": ["PhobosFramework"]`
   (Framework is always installed). Keep the other mod gates.
10. **Prefix and plain text.** New ids, including flag ids, start with `spacertales-`.
    Text is plain: no angle brackets, and square brackets only for the placeholders.
    Respect every length limit in the guide.
11. **Valid against the checker.** Every file passes
    `python scripts/validate-data-packs.py <file>`. If you cannot run it, check by hand
    against [the requirements table](../writing-story-content.md#requirements) and the
    field tables, and say so.

## Optional, and welcome

Use these where they make a story better. None is required.

- **Connect the chains with flags.** An arc step's `onComplete` can `setFlags` and
  `clearFlags`; news, talk and later arcs can require `flags` or `notFlags`. Prefer
  this to `arcsDone` when only part of a chain matters.
- **Follow a story as it happens.** News or talk gated on `"arcsAtStep":
  ["<arc>.<step>"]` appears only while that step is under way.
- **Outcome news for the other five chains.** Only four of the nine chains have news of
  how they ended. Add `once` news gated on `arcsDone` (or a flag) for the rest.
- **A rumour per thread.** Each thread can have one news item with no requirements
  beyond its mods, so a player who never visits still hears of it. Local news weighs
  four times as much at its place.
- **Local voices.** `"speakers": "locals"` for lines only strangers at the place say;
  `"speakers": "crew"` for lines only your crew say. Many current lines are first-person
  about "this ship" and should be `crew`.
- **Speaker factions.** `speakerFactions` (up to four of the game's
  [faction names](#faction-names)) for lines only certain people say, such as AyoSec
  officers at OKLG.
- **Crew, machines and clock.** `crewWith` (a skill someone aboard has, such as
  `SkillBotany`), `crewCount`, `running` (an installed machine id that must be
  running), `months`, `hours`, and `[crew]` for a crew member's name.
- **Standing gates.** `"standing": [{ "faction": "OKLGCorp", "atLeast": "warm" }]` for
  offers that need a faction's good opinion.
- **New threads, people and chains,** within the rules above.

## Held: do not use yet

- **Standing changes** (`onComplete.standing`). The owner has not yet confirmed in play
  that they reach the game's FACTIONS app. Write them in the step's `notes` as a
  suggestion ("suggested standing: OKLGCorp +5") instead; Claude adds them once
  confirmed.
- **New places.** Use Framework's. If a story truly needs a station Framework lacks,
  say which and why in your reply.

## Not available

No menus of choices, no new kinds of conversation, no new items, no world loot, no
characters walking up to the player, no encyclopedia articles unlocked by a flag. Do
not describe a game mechanic that does not exist.

## Reference

### Place keys

Regional places (one with no `within`) count as "here" anywhere in their region. Parts
lie within one.

| Key | Station | Name | Region or within | Body |
| --- | --- | --- | --- | --- |
| `hqch` | HQCH | Qincheng Station | Shipping & Inner System | Mercury |
| `vnca` | VNCA | Long Beach Terminal | Shipping & Inner System | Venus |
| `ejdr` | EJDR | Port Shajiang | Shipping & Inner System | Luna |
| `mhng` | MHNG | Qiantangmen | Shipping & Inner System | Sol |
| `msuz` | MSUZ | Panmen | Shipping & Inner System | Sol |
| `mtrs` | MTRS | Port Yangshan | Tharsis | Mars |
| `mvol` | MVOL | Upsilon Docking | Tharsis | Deimos |
| `bcer` | BCER | Port Mojave | Outer System | Ceres |
| `oklg` | OKLG | OKLG | Outer System | 1036 Ganymed |
| `jfts` | JFTS | Port Independence | Outer System | Ganymede |
| `jptn` | JPTN | Porto Nuevo | Outer System | Europa |
| `svir` | SVIR | Cassini Spaceport | Outer System | Titan |
| `hqch-det` | HQCH_DET | Temporary Detention Level | within `hqch` | Mercury |
| `vnca-sd` | VNCA_SD | San Diego Mall | within `vnca` | Venus |
| `vcbr` | VCBR | Cloudbreak | within `vnca` | Venus |
| `vcbr-sci` | VCBR_SCI | Cloudbreak Habitation Level | within `vnca` | Venus |
| `venc` | VENC | Porto do Encantado | within `vnca` | Venus |
| `vorb` | VORB | Venus Orbital | within `vnca` | Venus |
| `vorb-hab` | VORB_HAB | Venus Orbital: Habitation | within `vnca` | Venus |
| `ejdr-geo` | EJDR_GEO | Mare Imbrium Space Elevator | within `ejdr` | Luna |
| `mtrs-sub` | MTRS_SUB | Heifei District | within `mtrs` | Mars |
| `mlab` | MLAB | Royal Carriers Launch Facility | within `mtrs` | Mars |
| `mtam` | MTAM | Arsia Mons Space Elevator | within `mtrs` | Mars |
| `mhng-bbl` | MHNG_BBL | Duanqiao Lounge | within `mhng` | Sol |
| `msuz-rb` | MSUZ_RB | Renbao Pavilion Seven | within `msuz` | Sol |
| `mvol-med` | MVOL_Med | Central Medical Facility | within `mvol` | Deimos |
| `mvol-lab` | MVOL_Lab | Research & Development | within `mvol` | Deimos |
| `coho` | COHO | Corsair's Hollow | within `bcer` | Ceres |
| `bcer-roof` | BCER_ROOF | The Mouth | within `bcer` | Ceres |
| `bcer-sarc` | BCER_SARC | The Sarcophagus | within `bcer` | Ceres |
| `bcrs` | BCRS | Zhonghuamen Terminal | within `bcer` | Ceres |
| `bcrs-off` | BCRS_OFF | CCRE Government Offices | within `bcer` | Ceres |
| `bcrs-res` | BCRS_RES | Residential Block 01 | within `bcer` | Ceres |
| `bcrs-old` | BCRS_OLD | Old Ring: HAB 01 | within `bcer` | Ceres |
| `oklg-flot` | OKLG_FLOT | the Flotilla | within `oklg` | 1036 Ganymed |
| `bwvn` | BWVN | Weaver's Needle | within `oklg` | 1036 Ganymed |
| `jfts-ctr` | JFTS_CTR | Capitol District | within `jfts` | Ganymede |
| `jatl` | JATL | Atlantis Landing Zone | within `jptn` | Europa |
| `jatl-sub` | JATL_SUB | Lemuria Terminal | within `jptn` | Europa |
| `svir-shipyard` | SVIR_Shipyard | Titan Shipyards | within `svir` | Titan |

Framework's [`story.json`](../../mods/PhobosFramework/framework/story.json) holds the
full entries, including each place's factions.

### Faction names

`OKLGCorp` (Ayotimiwa Ship Breaking Co.), `OKLGLEO` (AyoSec), `OKLGCiv`, `OKLGFlotilla`,
`OKLGProspector` (Peralta Prospecting Concern), `OKLGScav`, `OKLGCrim`,
`GalileanConfederacy` (GalCon Peacekeepers), `GalileanConfederacyCiv`, `CCRE` (CCRE
Enforcers), `CCRECiv`, `Titan` (Titan Navy), `TitanCiv`, `Atlantis` (Atlantis Marines),
`AtlantisCiv`, `Xinhua`, `XinhuaCiv`, `EJDR`, `EJDRCiv`, `HQCH`, `HQCHCiv`, `MHNG`,
`MHNGCiv`, `MSUZ`, `MSUZCiv`, `VNCALEO` (Newcal PD), `VNCACiv`, `VENCLEO`, `VENCCiv`,
`VCBRLEO`, `VCBRCiv`, `VenusCrim`, `VenusPirates`, `BeltPirates`. Tiers, lowest
first: `dislikes`, `neutral`, `warm`, `friendly`, `trusted`, `honored`.

### The seven senders

Make each a person. Homes are your choice; the suggestions fit the stories as written.

| Current `from` | Letters | Suggested home | Why |
| --- | ---: | --- | --- |
| Neri Vale, receiving clerk | 14 | `oklg` or a part of it | OKLG is the shipbreaking hub where freight is received and recovered |
| Orra Pell, equipment broker | 6 | `bcer` | Ceres trade |
| Verdemorrow archive | 6 | your choice | A company archive; a person keeping it reads better than "archive" |
| Halewright service archive | 6 | `mvol-med` | Central Medical Facility, Deimos |
| Asterel service desk | 4 | your choice | Navigation electronics service |
| Alembrine correspondence | 4 | your choice | The still maker |
| Slingwright customer letters | 4 | your choice | Loading equipment |

A company desk may stay an organisation as a person entry (name "Asterel service
desk", no role), but a named clerk with a role is better.

### Item ids already used

Keep using these where the stories need items; add others only from
`phobosframework story items <words>` or the [item references](../item-references.md):
`PhobosLineDrainCanister`, `PhobosAluminiumIngot`, `PhobosAlembrineSpirit`,
`PhobosVerdemorrowLettuce`, `PhobosAutoNavBoard`, `PhobosVerdemorrowFirstlight4Installed`,
`PhobosVolatilesRefineryInstalled`, `PhobosFermenterStillInstalled`,
`PhobosMedicalBedInstalled`, `PhobosReactionMassFeederInstalled`.

## Known problems to fix

### Known spoilers

These mention an arc's people or events with no gate that ensures the player has met
them. News and talk get a gate; tips and articles get reworded (rule 6).

| File | Entry | Mentions |
| --- | --- | --- |
| 07 | `broadcasts.spacertales-double-freight-claim` | Neri Vale |
| 07 | `chatter.spacertales-ledger-names` | the second-shift ledger |
| 07 | `chatter.spacertales-ledger-not-verdict` | Neri Vale |
| 07 | `chatter.spacertales-watch-people` | the Short Return letter (needs `filesRead: spacertales-short-return-file`) |
| 07 | `tips.spacertales-ledger-history` | the second-shift ledger |
| 07 | `tips.spacertales-sable-margin-history-tip` | the Sable Margin |
| 07 | `tips.spacertales-short-return-ending` | Short Return |
| 07 | `articles.spacertales-second-shift-record` | the second-shift ledger |
| 07 | `articles.spacertales-sable-margin-history` | the Sable Margin |
| 07 | `articles.spacertales-short-return-history` | Short Return |
| 05 | `broadcasts.spacertales-alembrine-pump-story` | the borrowed pump |
| 05 | `chatter.spacertales-alembrine-pump-gossip` | the borrowed pump |
| 05 | `tips.spacertales-alembrine-pump-lore` | the borrowed pump |
| 05 | `articles.spacertales-alembrine-history` | the borrowed pump |
| 04 | `chatter.spacertales-process-last-bucket` | the last vessel |

If a tip or article cannot be told without the event, keep the background and leave
the event to the arc and its news.

### Other problems

- **Ownership gates on carried items.** `spacertales-misfiled-can` requires
  `owns: PhobosLineDrainCanister` to start but its step tests `have-item` (carried).
  Decide which is meant and make them agree.
- **First-person lines said by strangers.** All 84 small-talk lines are
  `speakers: anyone`, including lines about "this ship" and "aboard". Set those to
  `crew`.
- **Outcome news.** Only 4 of 9 chains have news of how they ended, though the
  changelog reads as if all do. Add it for the rest, or tell Claude to correct the
  changelog.

## A converted example

Before:

```json
"spacertales-ledger-not-verdict": {
  "moment": "small-talk", "speakers": "anyone",
  "line": "Neri's letters don't decide who owns the Sable Margin's parts. They make it harder to forget who repaired them."
}
```

After (with a thread and a person defined once in the file):

```json
"people": {
  "spacertales-neri-vale": { "name": "Neri Vale", "role": "receiving clerk", "home": "oklg" }
},
"threads": {
  "spacertales-second-shift-ledger": {
    "title": "The second-shift ledger", "place": "oklg",
    "people": [ "spacertales-neri-vale" ]
  }
},
"chatter": {
  "spacertales-ledger-not-verdict": {
    "thread": "spacertales-second-shift-ledger", "moment": "small-talk", "speakers": "locals",
    "line": "[person:spacertales-neri-vale]'s letters don't decide who owns the Sable Margin's parts. They make it harder to forget who repaired them.",
    "requires": { "arcsDone": [ "spacertales-misfiled-can" ] }
  }
}
```

The line is now said only at OKLG, only by people who live there, and only after the
player has finished the chain that introduces Neri.

## What to send back

1. The edited story files, whole, and any new ones.
2. A short list per file: what changed, which spoilers were fixed and how, and any rule
   you could not meet.
3. Your choices: each person's home, each thread's place, and any suggested standing
   changes left in notes.

Claude then runs the checker, the game-data checks (every item, place and person must
exist) and the language audit, and returns anything refused with the reason.
