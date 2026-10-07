# Writing story content

Phobos Framework lets anyone add to the world's story with a data file:

- **News** that plays on the game's TVs, among the game's own headlines.
- **Adverts** that play in the TVs' commercial breaks.
- **Arcs**: a short chain of goals in the GOALS list, with messages from someone in
  the world, items to bring or install, places to dock, credits to pay or hold, and
  rewards in items or credits. A step can branch: what the player does decides what
  comes next. Since Framework 0.122.0 a step can also wait for the player's reply,
  chosen in the Letters window.
- **Small talk** (since Framework 0.108.0): lines people say in the game's own chatter,
  so crews and station folk talk about your news and topics.
- **Loading tips**: lore shown on loading screens among the game's own.
- **Encyclopedia articles**: pages in the game's encyclopedia, under shared sections,
  with an optional picture.
- **Data files** (since Framework 0.110.0): files on a data card, read on any computer
  or PDA like the game's own; opening one can start an arc.

You need no programming. One JSON file holds the lot, and the game checks it as it
loads. This page explains the file, then gives a [prompt for ChatGPT](#writing-with-chatgpt)
that writes one for you. For a complete collection to learn from, see Phobos Spacer
Stories in `mods/PhobosSpacerStories`: nine files covering every kind of entry, packaged
as its own data-only add-on.

**Not available yet:** new kinds of conversation (lines ride on the game's existing
small talk), characters approaching the player, data files found as loot in the
world (for now they come as arc rewards) and full-screen encounters. Replies the player
picks are available since Framework 0.122.0 (see [Replies](#replies-choices)). Do not write content that needs
them. The [design record](development/story-system-design.md#later-work) says where
each stands.

## How it fits together

Story content needs nothing from you once the file is in place. Every story file is
read each time the game loads its content: the mods' own packs, add-ons and your
files. Change a file during a session and the change applies from the next load.
From then on:

- news and adverts appear on the TVs among the game's own, whenever the player meets
  their requirements;
- people use your small-talk lines in some of the game's own chatter, whenever the
  player meets the line's requirements;
- loading screens sometimes show your tips, and the encyclopedia lists your articles;
- an arc starts by itself: every 30 seconds, each arc whose requirements hold has its
  `chance` of starting (an arc with no chance starts only from the console);
- a goal finishes by itself within 30 seconds of its tests passing.

The F3 console commands in [Checking and testing](#checking-and-testing) are an
optional shortcut for writers who want to see their content at once instead of
waiting for it.

```mermaid
flowchart LR
    Write["Write or ask ChatGPT for a story file"] --> Check["Check it: game log, or validate-data-packs.py"]
    Check --> Place["Put it in a story folder"]
    Place --> Load["Load a game"]
    Load --> Play["Play: news, talk, tips, articles and arcs turn up by themselves"]
    Load -.->|"optional"| Try["F3: phobosframework story, then start, news or chatter"]
    Try -.-> Play
    Play --> Read["Watch the TV, the social log, the encyclopedia and the GOALS list"]
    Read --> Write
```

## Where the file goes

| Who you are | Folder |
| --- | --- |
| A player adding your own stories | `BepInEx/config/PhobosFramework/story/` (any name ending `.json`) |
| An add-on author | `phobos/PhobosFramework/story/` inside your add-on; see [publishing an add-on](publishing-an-add-on.md) |
| A Phobos mod | the mod's own `framework/story.json`, registered by the mod |

Files in a mod's own folder (for example `BepInEx/config/PhobosAgriculture/story/`)
work too, and can change that mod's shipped entries. An add-on may add only entries
whose ids start with its id prefix, written in lower case (prefix `Kestrel` gives
`kestrel-first-news`).

Every id is shared by all packs and all tables: if two files use the same id, the
first one loaded keeps it and the other is left out with a message in the log. Pick ids
that say who wrote them.

## The file

```json
{
  "schemaVersion": 1,
  "schema": "story",
  "broadcasts": {
    "kestrel-ice-prices": {
      "region": "Outer System",
      "text": "Water ice prices fell again at the outer stations, where haulers report the best season in years.",
      "weight": 2,
      "mention": "Ice is cheap at the outer stations again. Best season in years, they reckon."
    }
  },
  "adverts": {
    "kestrel-tug-advert": {
      "text": "Kestrel Towing\nStuck? We have been, too. Call Kestrel."
    }
  },
  "chatter": {
    "kestrel-tow-joke": {
      "moment": "joke",
      "line": "How many Kestrel tugs does it take to move a station? Just the one, if you're patient."
    }
  },
  "tips": {
    "kestrel-founding": {
      "text": "Kestrel Towing began with one tug and a debt the size of a moon."
    }
  },
  "articles": {
    "kestrel-towing": {
      "section": "phobos-makers",
      "label": "Kestrel Towing",
      "title": "Kestrel Towing",
      "body": "A towing outfit working the outer stations.\n\nIts tugs are old, slow and very hard to break."
    }
  },
  "arcs": {
    "kestrel-first-job": {
      "title": "Kestrel's first job",
      "chance": 0.05,
      "requires": { "dockedAt": [ "any" ] },
      "steps": [
        {
          "id": "ask",
          "delivery": { "message": { "from": "Kestrel Towing", "text": "Hello, [player-first]. Fancy some easy work?" } },
          "objective": { "title": "Wait for a call from Kestrel", "description": "Kestrel will be in touch within a few hours." },
          "tests": [ { "kind": "wait", "hours": 4 } ]
        }
      ]
    }
  }
}
```

Keep `schemaVersion` and `schema` as shown. Any table may be left out.

### Where things happen: places, people and threads

Since Framework 0.114.0 content can belong somewhere and to someone, so news comes
from where you are, strangers talk about their own station, and letters arrive from
people with a home. Three tables carry this; every field is optional on older files,
which load as before.

**Places** (`places`) are stations by the game's registration id. Framework ships
the game's twelve regional stations and the parts and neighbours that lie within
them; `phobosframework story places` lists them with their keys (`oklg`, `vnca`,
`bcer`, `oklg-flot`, `vorb` and so on). Add your own only for a station Framework
does not name:

| Field | Needed | What it does |
| --- | --- | --- |
| `station` | yes | The registration id or prefix (`OKLG`, `VORB_HAB`); its parts (`OKLG_RES`, `VORB\|Aux`) count as it does. |
| `name` | yes | What people call it: the `[place]` placeholder (up to 40 characters). |
| `within` | no | The regional place it lies within, by key. A place without one is a region of its own and needs a `region`. |
| `region` | sometimes | The "Region News:" label for news from here: `Shipping & Inner System`, `Tharsis` or `Outer System` as the game uses them. A part inherits its region's. |
| `body` | no | The body it orbits or stands on, for `[body]`. |
| `factions` | no | The game's faction names at home there, for your reference. |

**People** (`people`) are named recurring characters. A letter from a person shows
as "Name, role" and the person can be named in text with `[person:key]`:

| Field | Needed | What it does |
| --- | --- | --- |
| `name` | yes | Up to 40 characters. |
| `role` | no | Up to 40 characters, shown after the name. |
| `home` | yes | A place key. Everyone lives somewhere. |
| `faction` | no | The game's faction name they belong to, for your reference. |
| `face` | no | The look of the face the game makes for them (Framework 0.121.0): `masculine`, `feminine` or `any` (the default). |

A goal from a person shows their face in the GOALS list. The face is built from the
game's own portrait parts, rolled once the first time it is needed and kept in the
player's save, so a correspondent looks the same for that whole game. A goal from no one
in particular shows the game's wrist PDA picture. A goal also ends with a line naming who
it is from, such as "From Orra Pell, equipment broker (Port Mojave).", so you need not say so in its
description.

**Threads** (`threads`) tie a story together. An entry that names a `thread` inherits
the thread's `place` unless it names its own, and must meet the thread's `requires` as
well as its own. A thread with a cast (`people`) keeps its letters in the family: a
member's `person` must be in the cast.

| Field | Needed | What it does |
| --- | --- | --- |
| `title` | yes | For you and the F3 thread report. |
| `place` | no | Where the thread lives: the default place of its members. |
| `people` | no | Its cast, up to eight person keys. Leave it out to allow anyone. |
| `requires` | no | Requirements every member must also meet. |

News items, adverts, small talk, arcs and data files take `thread` and `place`; tips,
sections and articles take `thread` for grouping only (they have no player to check).
What a place does for each kind:

- **News and adverts** of the place you are at or in are picked about four times as
  often as news of elsewhere; news with no place counts twice. A news item takes its
  "Region News:" label from its place when it has no `region` of its own.
- **Small talk** with a place is said there: by your crew while you are at it, by
  others only when they are there themselves. `speakers: locals` means others, at
  the line's place.
- **An arc** with a place starts by itself only while you are at it, and a `dock-at`
  test may leave out its `station` to mean that place.
- **A regional place** (one with no `within`) counts as "at" from anywhere in its
  region, which is the game's own idea of where you are: the nearest regional station.

```mermaid
flowchart LR
    Thread["Thread: place, cast, requires"] --> News["News and adverts: local ones weigh more"]
    Thread --> Talk["Small talk: said at the place"]
    Thread --> Arc["Arc: starts there; letters from the cast"]
    Arc -->|setFlags| Flags["Story flags on the player"]
    Flags -->|requires.flags| News
    Flags -->|requires.flags| Talk
    Arc -->|"requires.arcsAtStep"| Talk
```

### News items (`broadcasts`)

| Field | Needed | What it does |
| --- | --- | --- |
| `region` | yes | Shown above the item as "Region News:". The game's own regions are Shipping & Inner System, Tharsis and Outer System. Up to 40 characters. |
| `text` | yes | The news item, up to 700 characters. The game's own run to about 650. |
| `weight` | no | 1 to 100 (default 1): how often it is picked against other story news. |
| `once` | no | `true` shows it once in a save, then never again. |
| `mention` | no | What people say when they bring the news up in small talk (up to 200 characters), while the news item's requirements hold. |
| `requires` | no | When it may show; see [requirements](#requirements). |
| `title`, `notes` | no | For you; the game never shows them. |

About a third of TV news picks come from story news when any is available; the rest
stay the game's own (this share is a setting, below).

### Adverts (`adverts`)

`text` (up to 400 characters; a line break can separate a heading), and `weight`,
`once`, `requires`, `title` and `notes` as for news.

### Small talk (`chatter`)

The game's characters already chat: they mention headlines, crack jokes, complain
about the authorities. A chatter line rides on one of those moments. When a moment
comes up and one of your lines fits, the game sometimes (40% of the time by default)
says your line instead, after a short lead-in:

| `moment` | The game's own small talk | How your line is introduced |
| --- | --- | --- |
| `headline` | Mention a headline | Sam mentions a headline to Ada: "…" |
| `joke` | A mild or dark joke | Sam tells a joke to Ada: "…" |
| `complaint` | Complain about the authorities | Sam complains to Ada: "…" |
| `story` | Reminisce, share a story | Sam tells Ada a story: "…" |
| `jargon` | Recite technical jargon | Sam recites some jargon to Ada: "…" |
| `superstition` | Warn about a spacer superstition | Sam cautions Ada about a spacer superstition: "…" |
| `worry` | Admit worries | Sam opens up to Ada: "…" |
| `question` | A metaphysics question | Sam asks Ada: "…" |
| `small-talk` | Shoot the breeze | Sam shoots the breeze with Ada: "…" |

| Field | Needed | What it does |
| --- | --- | --- |
| `moment` | yes | One of the moments above. |
| `line` | yes | What the speaker says, up to 200 characters. Write it as speech. |
| `speakers` | no | `anyone` (default); `crew`, only someone aboard one of the player's ships (the player too); `others`, only someone who is not, such as station folk; or `locals`, others at the line's place (Framework 0.114.0). |
| `speakerFactions` | no | Up to four of the game's faction names; only a speaker who belongs to one says the line (Framework 0.115.0). `OKLGLEO` is AyoSec, `OKLGCorp` the Ayotimiwa Ship Breaking Co., `OKLGCiv` OKLG's civilians; a place's `factions` list the rest. |
| `weight` | no | 1 to 100 (default 1): how often it is picked against other lines for the same moment. |
| `requires` | no | When it may be said; checked against the player, as for news. |
| `title`, `notes` | no | For you. |

A news item's `mention` is a `headline` line for anyone, with the news item's weight
and requirements. Lines are heard in the social log when the player is near the
people talking, and in conversations the player takes part in.

### Loading tips (`tips`)

`text` (up to 450 characters), `weight`, and `requires`, which may list only `mods`:
no player exists while the game loads. About a third of tips come from story tips when
any is available (a setting, below). Tips cannot use placeholders.

### Encyclopedia (`sections` and `articles`)

Articles appear in the game's encyclopedia, each under a section. Framework provides
three shared sections, and any pack can add more:

| Section id | Shown as |
| --- | --- |
| `phobos-makers` | Makers and brands |
| `phobos-spacer-life` | Life between stations |
| `phobos-operations` | Phobos operations (Framework 0.117.0) |

Phobos operations holds Framework's own help: the articles the **About** buttons on
the Crew panel and the Maintenance sheet open (`operations-standing-orders`,
`operations-upkeep`, `operations-time-skips`, `operations-store-links`,
`operations-maintenance`). A content mod may add articles on running its own
equipment there; keep them practical and leave those five ids alone.

An article has `section` (a section id from any loaded pack), `label` (its name in the
list, up to 40 characters), `title` (up to 60), `body` (up to 4,000; separate
paragraphs with a blank line, `\n\n` in JSON), an optional `image` and `requires`,
which may list only `mods`. A new section has `label`, `title`, and an optional `body`
and `image`. `image` is the path of a picture under a mod's `images` folder, without
`.png`, such as `phobos/agriculture/Counter`; the encyclopedia shows it at its own size,
so a small item sprite stays small. A section shows
only while at least one of its articles does. Articles cannot use placeholders.

### Data files (`files`)

A data file sits on a data card (the game's own Renbao R014 Data Card) and is opened on
any computer or PDA, as the game's own files are. An arc gives one by listing its id in
an outcome's `files`; the player receives a data card holding those files.

| Field | Needed | What it does |
| --- | --- | --- |
| `name` | yes | The file name the computer lists, such as `TRIAL_NOTES.TXT`: letters, digits, dots, hyphens and underscores, up to 32 characters. |
| `text` | yes | What the file says when opened, up to 3,000 characters. Placeholders work here. |
| `startsArc` | no | An arc that starts the first time the file is opened, if it has not started and its requirements hold. |
| `notes` | no | For you. |

An arc can wait for a file with the requirement `filesRead`. If a file's story pack is
removed, the file stays on its card and reads as corrupted.

### Arcs (`arcs`)

| Field | Needed | What it does |
| --- | --- | --- |
| `title` | yes | For you and the F3 list; the player sees each step's goal title instead. |
| `chance` | no | 0 to 1 (default 0): the chance, at each story check (every 30 seconds), that an available arc starts by itself. 0.05 starts it after about ten minutes of play. 0 means it starts only from F3. |
| `requires` | no | When it may start. Once started, it carries on whatever happens. |
| `repeatable` | no | `true` lets it start again after it is finished. |
| `steps` | yes | 1 to 12 steps, done in order. |

Each step:

| Field | Needed | What it does |
| --- | --- | --- |
| `id` | yes | Lower case and hyphens, unique in the arc. It is saved with the player's goal, so do not rename it after people play it. |
| `delivery` | no | What the player is told as the step begins: a `message` (`from` and `text`, shown in the crew log) and/or a `bulletin`, the id of a news item the next TV news shows. |
| `objective` | no | A goal in the GOALS list: `title` (up to 60 characters), `description` (up to 300) and, since Framework 0.121.0, `person`: whose face it shows and who it is from. Left out, that is the sender of the step's letter, else the last sender before it in the arc. A step without an objective waits unseen. |

Write a goal as the player's next move and what it is for, in the story's own terms:
"Give the broker two hours to write back", "Be at [place] with one bottle of Alembrine
Spirit on you (a bag counts). You keep it." Say what a player could wrongly fear, such as
losing an item or a deadline, but do not list everything the goal does not do.
| `tests` | yes | 1 to 4 tests; all must pass to finish the step. |
| `onComplete` | no | A `message`; `items` (up to five kinds, 1 to 20 of each) given to the player, or put at their feet when they cannot carry them; `credits` (up to 50,000) paid to the player with a line in the game's ledger; and `files` (up to five data file ids) on one data card. |
| `next` | no | The step that follows: another step's `id`, or `end`. By default the next step in order, or the end after the last. |
| `branches` | no | Up to four other ways the step can finish; see [branches](#branches). |

Two steps in a row should not have the same goal title: the game does not show a goal
titled like one it showed in the last ten seconds (the story check offers it again).

### Branches

A branch is another way out of a step: its own `tests` (1 to 4), its own `onComplete`
and a `next` step (another step's `id`, or `end`), which every branch must name. At each
story check the step's own tests are tried first; if they do not all pass, the first
branch whose tests all pass decides. The goal closes as completed either way. For
example, a step can pay off a debt with credits, finish early for a crew member with a
skill, or give up after 48 hours:

```json
{ "id": "offer",
  "objective": { "title": "Settle the dock fees" },
  "tests": [ { "kind": "credits", "amount": 500, "consume": true } ],
  "next": "thanks",
  "branches": [
    { "tests": [ { "kind": "condition", "condition": "SkillHacking" } ], "next": "hacked" },
    { "tests": [ { "kind": "wait", "hours": 48 } ], "next": "end" } ] }
```

Steps may also jump back to an earlier step; each check moves an arc at most one step.

### Replies (`choices`)

Since Framework 0.122.0 a step can wait for the player to answer. Clicking a story goal
in the GOALS list opens the **Letters** window: every correspondence the player has
begun, its letters in order with the correspondent's face, and the replies. A step with
`choices` offers two to four of them, has no `tests` or `branches` of its own, and
finishes when the player sends one. Each reply has:

| Field | Needed | What it does |
| --- | --- | --- |
| `id` | yes | Lower case and hyphens, unique in the step. It is saved with the player's answer, so do not rename it after people play it. |
| `label` | yes | The reply as the player sees it on its button, up to 60 characters. |
| `tests` | no | Up to four tests the reply needs before it can be sent. Until they pass it shows locked, with what it needs ("needs you to carry 1 bottle of Alembrine Spirit"). |
| `onComplete` | no | As a step's: the answer's letter, items, credits, files, flags and standing. |
| `next` | yes | The step the reply leads to, or `end`. |

```json
{ "id": "offer",
  "delivery": { "message": { "person": "dara-osei", "text": "Will you carry the sample to [place]?" } },
  "objective": { "title": "Answer Dara about the sample" },
  "choices": [
    { "id": "accept", "label": "I'll carry it.", "next": "carry",
      "onComplete": { "message": { "person": "dara-osei", "text": "Thank you." }, "setFlags": [ "myprefix-sample-taken" ] } },
    { "id": "insured", "label": "I'll carry it, insured.", "tests": [ { "kind": "credits", "amount": 200, "consume": true } ], "next": "carry" },
    { "id": "refuse", "label": "Not this time.", "next": "end" } ] }
```

The player confirms a reply before it is sent, and cannot take it back. Use `setFlags` to
let later news, small talk and arcs follow the answer. Dismissing the goal still sets the
whole correspondence aside, as it does for any goal. The console's
`phobosframework story answer <arc> <reply>` sends a reply too, through the same checks.

### Tests

| `kind` | Fields | Passes when |
| --- | --- | --- |
| `dock-at` | `station` | The player is aboard, or docked at, that station or one of its parts (`VORB` covers `VORB_HAB`). `any` means any station. |
| `have-item` | `item`, `count` (default 1), `consume` | The player carries that many, bags included. With `"consume": true` they are taken when the step finishes. |
| `install` | `item`, `count` (default 1) | That many are on the player's ships nearby (installed, or lying aboard). |
| `wait` | `hours` | That many game hours have passed since the step began (up to 720). |
| `credits` | `amount`, `consume` | The player holds at least that many credits. With `"consume": true` they are paid when the step finishes, with a line in the game's ledger. |
| `condition` | `condition` | The player has that game condition, such as a skill (`SkillHacking`, `SkillEngMechanical`). |

`item` is an item definition id, such as `PhobosVerdemorrowWheatGrain`. To find one,
type `phobosframework story items wheat` in the F3 console: it lists every id whose
name contains the words. Installed machines usually end in `Installed`. A station id is
the one the game uses, such as `OKLG`; type `phobosframework story` in the F3 console
while docked to see the ids where you are.

### Requirements

Every part is optional, and every part given must hold. Tips and the encyclopedia
take only `mods`.

| Field | Holds when |
| --- | --- |
| `mods` | Every mod listed is installed: a Phobos mod by folder name (`PhobosManufacturing`) or any BepInEx plugin id. An entry for a mod that is not installed never shows. |
| `playerConditions` | The player has every game condition listed. |
| `forbidConditions` | The player has none of them. |
| `owns` | Each item listed is on one of the player's ships. |
| `dockedAt` | The player is docked at any one of these stations (`any` for any). |
| `arcsDone` | Each arc listed has been finished. |
| `arcsNotStarted` | No arc listed has ever been started. |
| `filesRead` | Each story data file listed has been opened. |
| `afterDays`, `beforeDays` | Story time is at least `afterDays`, and less than `beforeDays`, game days. A game day is the game's own, 87,658 seconds (about 24 hours 21 minutes; Framework 0.127.0 and later). Story time starts when the player's story record begins: at the start of a new game, or for a game started before Framework 0.109.0 the first time it is loaded with it. |
| `flags` | Every story flag listed is set. An arc's `onComplete` sets and clears flags with `setFlags` and `clearFlags` (up to four each). Other mods set flags too (Framework 0.127.0): Phobos Banking marks loans taken, repaid and late, so a story can follow what the player did with money; Phobos Exchange (0.2.0) marks each company's latest big move (`exchange-<company>-surge` or `-slump`), the player buying in (`-bought`), holding a large stake (`-major-holder`) and selling out (`-sold-out`), and starts the arc of the same name when one exists. A flag can also move a company's share price, through the exchange's `news` entries. Flag ids are yours to choose; give them your prefix. |
| `notFlags` | None of the flags listed is set. |
| `arcsActive` | Each arc listed is under way. |
| `arcsAtStep` | Each `arc.step` listed is under way at that step, so news and talk can follow a story as it happens. |
| `places` | The player is at any one of these places: docked at it, or anywhere in its region for a regional place. |
| `regions` | The player is in the region of any one of these regional places. |
| `newsSeen` | Each news item listed has been shown on a TV. |
| `standing` | How the game's factions regard the player (Framework 0.115.0): a list of `{ "faction": "OKLGCorp", "atLeast": "warm" }` and/or `"atMost"`, by the game's own tiers `dislikes`, `neutral`, `warm`, `friendly`, `trusted`, `honored`. Faction names are the game's (the FACTIONS app on the PDA shows them; `phobosframework story where` shows your standing with each faction a loaded place or person names). |
| `crewWith` | Someone aboard other than the player has each game condition listed (a skill such as `SkillBotany`). |
| `crewCount` | `{ "atLeast": 1, "atMost": 4 }`: how many crew the player has, the player not counted. |
| `running` | Each Phobos machine listed (its installed definition id, such as `PhobosVerdemorrowFirstlight4Installed`) is running on one of the player's ships. |
| `months` | The calendar month is one of those listed (1 to 12). |
| `hours` | `{ "from": 22, "to": 5 }`: the UTC hour is in the window; `from` after `to` wraps midnight. |

Since Framework 0.114.0 a thread's `requires` also apply to every entry in it.

An arc's `onComplete` may also change standing (Framework 0.115.0): `"standing":
[ { "faction": "OKLGCorp", "change": 5 } ]`, up to two factions and up to 10 points
either way (a tier is 25 points), through the game's own faction scores, with a line
in the crew log. The owner's choice is small changes only.

### Text

- Plain text only. A line break (`\n` in JSON) is fine; angle brackets are not.
- Square brackets are kept for placeholders: `[player]` (full name), `[player-first]`,
  `[ship]` (the ship the player is aboard), and since Framework 0.114.0 `[place]` (the
  entry's place, else where the player is), `[region]` (its region label), `[station]`
  (the station the player is docked at), `[body]` (the place's body), `[date]` (the
  game's date, year-month-day), `[person:key]` (a person's name) and `[crew]` (the
  name of one of the player's crew, chosen at random; Framework 0.115.0). Any other
  bracketed word is refused. Tips and encyclopedia text cannot use them.
- Text is English in the file. A translation can replace it by the key
  `Story.<id>.<field>` in the owning mod's translation file: for news `text`, `region`
  and `mention`; for small talk `line`; for tips `text`; for sections and articles
  `label`, `title` and `body`; for data files `name` and `text`; for arc steps `<arc>.<step>.title`, `.description`,
  `.from`, `.message`, `.doneFrom` and `.done`; for a branch's message
  `<arc>.<step>.b<n>.doneFrom` and `.done`, counting branches from 0.

### Settings

Framework's own file (`mods/PhobosFramework/framework/story.json`) holds the settings;
override them in `BepInEx/config/PhobosFramework/story/`:

| Setting | Shipped | What it does |
| --- | ---: | --- |
| `broadcastShare` | 0.3 | Share of TV news picks given to story news. |
| `advertShare` | 0.3 | Share of advert picks given to story adverts. |
| `chatterShare` | 0.4 | Share of matching small talk that uses a story line when one fits. |
| `tipShare` | 0.3 | Share of loading-screen tips taken from story tips. |
| `checkSeconds` | 30 | Real seconds between story checks. Arriving in a new region runs a check at once. |
| `maxActiveArcs` | 2 | How many arcs may start by themselves at once. |
| `localWeight` | 4 | How much more often news and adverts of the place you are at are picked. News with no place counts 2. |
| `farWeight` | 1 | How much news and adverts of other places weigh. 0 hides them until you visit. |
| `mentionDays` | 10 | Game days after a news item was shown during which people still mention it. |

## Checking and testing

- **Before the game:** with the repository, `python scripts/validate-data-packs.py yourfile.json`
  checks a file that carries the `schema` header. Editors that read JSON Schema can use
  `schemas/story.schema.json` for completion and inline errors.
- **In the game:** a file with a mistake is skipped and the reason goes to
  `BepInEx/LogOutput.log`. An entry naming an item, condition, arc or section the game
  does not have is left out on its own, with a message.
- **F3 console (optional):** nothing here is needed for story content to work. These
  commands only save waiting while you write and test. Since Framework 0.128.1, the ones
  that change your save (`story start`, `try`, `reset`, `news`, `file`, `flag`, `standing`
  and `check`) are test commands: type the game's own `unlockdebug` first, read the warning,
  and repeat the command with `confirm` at the end, for example
  `phobosframework story start my-arc confirm`. The save is then marked as test-changed
  (`phobosframework status` says so). Test on a copy of your save. The readouts below stay open:
  - `phobosframework story` lists the packs, anything left out and why, where you are
    docked, each arc (under way with each test's progress, finished, set aside, or why
    it cannot start yet), and how much small talk, tips and articles are in play.
  - `phobosframework story news <id>` shows a news item on the next TV news.
  - `phobosframework story chatter` lists the small-talk lines each moment can use now;
    `phobosframework story chatter <id>` makes the next small talk of that line's moment
    say it.
  - `phobosframework story start <arc>` starts an arc now, whatever its chance and requirements.
  - `phobosframework story try <arc>` starts an arc only if its requirements and place hold, and says what blocks it otherwise, as another mod starting it would. (Another mod starting it from code, through `StoryArcs.TryBegin`, needs no `unlockdebug`.)
  - `phobosframework story check` runs the story check at once.
  - `phobosframework story reset <arc>` forgets an arc in this game so it can start again.
  - `phobosframework story items <words>` lists the item ids whose names contain the words.
  - `phobosframework story file <id>` gives you a data card with that story file.
  - `phobosframework story where` says which region and place you are in, where you
    are docked, the date, the flags set and whether each thread is open.
  - `phobosframework story thread <id>` lists a thread's members and what blocks each.
  - `phobosframework story flag <id>` sets a story flag; add `clear` to clear it.
  - `phobosframework story places` and `story people` list what the loaded packs know.
  - `phobosframework story standing <faction> <change>` changes a faction's view of you
    by up to 10 points, for testing content; check the FACTIONS app afterwards.

## What stays in a save

- The player carries one Phobos record: where each arc is, which news has been shown
  and when, news waiting for a TV, the story flags set, and when story time began.
- Credits paid or taken by an arc are ordinary credits, with a line in the game's ledger.
- A data file is a Framework data object on an ordinary data card; it keeps its file
  name and the id of its story file, and the record remembers which files were opened.
- Each goal keeps its title, description and the name `PhobosStory.<arc>.<step>`.
- Nothing else. TV news, small talk, tips and articles leave nothing in the save.
- **Removing a story file is safe.** Its goals finish and disappear on the next load,
  its talk, tips and articles simply stop appearing, and its record entries are
  ignored; put the file back and they are picked up again.
- **Dismissing a story goal** in the GOALS list sets that arc aside for good in that game.

## Writing well

**The setting, in our own words.** Ostranauts is set in the Solar System of the
near future; the game's own loading tips give its year. People live and work on
stations and ships from the inner system to the outer moons, and most crews scrape a
living from salvage, hauling and odd jobs, with fuel, air, food and debt never far from
mind. Companies and governments own the stations and the rules. The tone is
blue-collar and lived-in: worn ships, small victories, dry humour. Read the game's own
news, tips and encyclopedia before naming its places, factions or history, and do not
contradict them.

**Our companies** are invented for these mods and can appear in news, adverts, talk
and articles. Use them as companies in the world, never as claims about real firms:

| Company | Makes |
| --- | --- |
| Verdemorrow Agronomics | Grow racks (Firstlight), galley cookers (Hearth), seed (Continuance), nutrients (Groundwork). Hopeful and practical; its line is "Where we go, life grows." |
| Rivetline | Salvage machinery, furnaces, silos, bins and belts |
| Asterel | Navigation and control electronics (the Polaris modules) |
| Fennmark | Refineries, chemical processors and gas stores |
| Tolvane, Lixivar, Oxsmith | Ammonia, leaching and acid, and oxygen from rock |
| Ablatine | Mining lasers |
| Alembrine | Ethanol tanks and spirits |
| Slingwright | The reaction mass feeder |
| Halewright | Medical beds and patient monitors |

**Voice.** Practical, worn-in and occasionally dry, as in the
[player language guide](development/player-language.md). Goals say plainly what to do;
messages and small talk may have character. No forced slang, no gratuitous swearing.

**Do not:**

- name or imitate real people, real companies or real-world politics;
- copy the game's own text, or anyone else's;
- promise something the game does not do (a goal must be possible with the tests above,
  and a news item or line should not describe a mechanic that does not exist, such as
  a crop disease);
- ask for items players cannot get. Check the item reference for where each is bought
  or found;
- shorten a full equipment name: it always starts with `Phobos'` (for example
  Phobos' Verdemorrow Firstlight-4 Cultivation Rack); a short name such as
  "the Firstlight-4" is fine in passing.

## Writing with ChatGPT

Paste the prompt below into ChatGPT, then describe what you want at the end. Check
what comes back with the steps above before you use it.

```text
You write story content for the Ostranauts mod suite "Phobos". Reply with one JSON
file only, no commentary, following these rules exactly.

Format:
{ "schemaVersion": 1, "schema": "story",
  "people":     { "<id>": { "name": "...", "role": "...", "home": "<place key>", "face": "masculine, feminine or any" } },
  "threads":    { "<id>": { "title": "...", "place": "<place key>", "people": [ "<person id>" ], "requires": { ... } } },
  "broadcasts": { "<id>": { "thread": "<thread id>", "text": "...", "weight": 1, "once": false, "mention": "...", "requires": { ... } } },
  "adverts":    { "<id>": { "thread": "<thread id>", "text": "..." } },
  "chatter":    { "<id>": { "thread": "<thread id>", "moment": "...", "line": "...", "speakers": "locals" } },
  "tips":       { "<id>": { "text": "..." } },
  "articles":   { "<id>": { "section": "phobos-makers", "label": "...", "title": "...", "body": "..." } },
  "files":      { "<id>": { "name": "FILE_NAME.TXT", "text": "...", "thread": "<thread id>", "person": "<person id>", "startsArc": "<arc id, optional>" } },
  "arcs":       { "<id>": { "title": "...", "thread": "<thread id>", "chance": 0.05, "requires": { ... },
                  "steps": [ { "id": "...",
                    "delivery": { "message": { "person": "<person id>", "text": "..." }, "bulletin": "<broadcast id>" },
                    "objective": { "title": "...", "description": "..." },
                    "tests": [ { "kind": "...", ... } ],
                    "onComplete": { "message": { "person": "<person id>", "text": "..." },
                                    "items": [ { "item": "...", "count": 1 } ], "credits": 0, "files": [ "<file id>" ],
                                    "setFlags": [ "MYPREFIX-<flag>" ], "clearFlags": [ ] },
                    "next": "<step id or end>",
                    "branches": [ { "tests": [ ... ], "next": "<step id or end>", "onComplete": { ... } } ] },
                  { "id": "...", "delivery": { ... }, "objective": { ... },
                    "choices": [ { "id": "...", "label": "...", "tests": [ ... ], "onComplete": { ... }, "next": "<step id or end>" } ] } ] } } }

Rules:
- Ids: lower-case letters and digits joined by single hyphens, starting with MYPREFIX-.
  Step ids likewise, unique within their arc. Flag ids likewise.
- Everything belongs somewhere. Every arc, news item, advert, small-talk line and data
  file names a thread, and every thread names a place from the list I give you below
  (a station key such as oklg, bcer, vnca, vorb). Every letter names a person from
  the thread's cast, and every person has a home place. Do not invent places; write
  "[place]" in text where the place's name belongs, "[region]" for its region and
  "[person:<id>]" for a person's name.
- Connect the threads: news that follows an arc uses "requires": {"arcsAtStep": ["<arc>.<step>"]}
  or {"flags": [...]} set by an earlier step's "setFlags"; talk about something that
  happened uses "newsSeen" or "arcsDone". Nothing should refer to events the player
  has not seen. Give each thread one news item with no requirements, as a rumour.
- Broadcast text at most 700 characters; mention at most 200; advert text at most 400;
  message text at most 400; goal title at most 60 and description at most 300. A news
  item in a thread needs no region (its place gives one); one without a thread needs
  "region": Shipping & Inner System, Tharsis or Outer System.
- Goals: write the title and description as the player's next move and its purpose
  in the story ("Give the broker two hours to write back"). The game adds who the goal
  is from and shows their face. Mention only what a player could wrongly fear (losing
  an item, a deadline); never list what the goal does not do, check or change.
- Small talk: moment is one of headline, joke, complaint, story, jargon, superstition,
  worry, question, small-talk. The line is what the speaker says, at most 200
  characters, written as speech. speakers is anyone, crew (aboard the player's ships),
  others (anyone else) or locals (others, at the line's place). A line in a thread
  is said only at the thread's place.
- Tips: at most 450 characters of lore. Articles: section phobos-makers (companies)
  or phobos-spacer-life (how crews live), label at most 40, title at most 60, body at
  most 4000 with paragraphs separated by \n\n. Tips and articles may require only mods
  and use no placeholders.
- Data files: a file name of letters, digits, dots, hyphens and underscores (at most 32,
  such as LOG_0412.TXT) and text of at most 3000 characters, written as the document
  itself (a log, a memo, a letter). An arc outcome gives them with "files".
- Plain text only: no angle brackets, no square brackets except [player],
  [player-first], [ship], [place], [region], [station], [body], [date] and
  [person:<id>]. Use \n for a line break.
- Tests, all of which must pass to finish a step:
  {"kind":"dock-at"} (the arc's own place), {"kind":"dock-at","station":"any"} or a
  station id I give you;
  {"kind":"have-item","item":"<item id>","count":N,"consume":true or false};
  {"kind":"install","item":"<item id>","count":N};
  {"kind":"wait","hours":H};
  {"kind":"credits","amount":N,"consume":true or false};
  {"kind":"condition","condition":"<game condition, such as SkillHacking>"}.
- A step may name its next step ("next": a step id or "end") and have up to four
  branches, each with its own tests, onComplete and next; the first branch whose
  tests pass decides when the step's own tests do not.
- Requirements (all optional): mods, playerConditions, forbidConditions, owns,
  dockedAt, arcsDone, arcsNotStarted, arcsActive, arcsAtStep, filesRead, flags,
  notFlags, places, regions, newsSeen, standing (a list of {"faction": "...",
  "atLeast": "warm"} with tiers dislikes, neutral, warm, friendly, trusted, honored),
  crewWith (skills someone aboard has), crewCount ({"atLeast": 1, "atMost": 4}),
  running (installed machine ids that must be running), months (1 to 12), hours
  ({"from": 22, "to": 5}, UTC), afterDays, beforeDays (game days of story time).
  A thread's requires apply to all its members. Small talk may name speakerFactions.
- An arc outcome may change standing: "standing": [{"faction": "OKLGCorp",
  "change": 5}], at most two factions, at most 10 points either way. Use it rarely.
- Faction names you may use: OKLGCorp (Ayotimiwa Ship Breaking Co.), OKLGLEO (AyoSec),
  OKLGCiv, OKLGFlotilla, OKLGProspector, GalileanConfederacy (GalCon Peacekeepers),
  GalileanConfederacyCiv, CCRE (CCRE Enforcers), CCRECiv, Titan (Titan Navy), TitanCiv,
  Atlantis, AtlantisCiv, Xinhua, XinhuaCiv, VNCALEO (Newcal PD), VNCACiv, VENCLEO,
  VENCCiv, VCBRLEO, VCBRCiv, EJDR, EJDRCiv, HQCH, HQCHCiv, MHNG, MHNGCiv, MSUZ,
  MSUZCiv, VenusCrim, BeltPirates, OKLGCrim.
- Use only item ids I list below. Rewards: at most five kinds of item, at most 20 of
  each, and at most 50000 credits, modest in value.
- A step may instead offer the player 2 to 4 replies ("choices"), each with an id, a
  label of at most 60 characters, optional tests that must pass before it can be sent,
  an optional onComplete and a required next. Such a step has no tests or branches of
  its own and waits for the player's answer. Use replies where the captain would
  really decide something (accept, refuse, haggle), not as a way to acknowledge a
  letter; follow the answer with setFlags.
- Nothing else is available: no new kinds of conversation, no new items or places.
- Setting: Ostranauts, the Solar System of the near future; blue-collar spacers living
  by salvage, hauling and odd jobs. Practical, worn-in voice with occasional dry
  humour. No real people, companies or politics; do not copy the game's text; never
  describe a game mechanic that does not exist.
- Companies you may use: Verdemorrow Agronomics (grow racks, cookers, seed,
  nutrients; "Where we go, life grows."), Rivetline (salvage machinery), Asterel
  (navigation electronics), Fennmark (refineries and gas stores), Halewright
  (medical equipment). Full equipment names start with Phobos', for example
  Phobos' Verdemorrow Firstlight-4 Cultivation Rack.

Place keys you may use: <paste from phobosframework story places, with their names and bodies>
Item ids you may use: <paste ids from the item reference>
What I want: <describe the threads: where each lives, who is in it, and what happens>
```

Replace `MYPREFIX` with your add-on's prefix (or a word of your own for a personal
file), paste the place keys from `phobosframework story places` (or Framework's
`story.json`), and paste the item ids you want used, found with
`phobosframework story items` in the F3 console. The [item references](item-references.md)
say where each item is bought or found. Check the result with
`phobosframework story thread <id>` in play: it lists each member and what blocks it.
