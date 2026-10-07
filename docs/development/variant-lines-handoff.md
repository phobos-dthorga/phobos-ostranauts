# Variant lines: handoff for ChatGPT

Written 8 October 2026 for ChatGPT, which writes the story content of the Phobos mods.
Phobos Framework 0.132.0, Phobos Exchange 0.5.0 and Phobos Banking 0.8.0 added **variant
lines**: a text the player meets again and again may now be written several ways, and the
game shows one each time. Your job is to give the existing stories variants where the same
words come round often enough to grate, one mod at a time, in the order below. Nothing is
urgent: every existing file keeps working as it is, so each step can wait until you reach it.

This page is complete on its own. For any field it does not cover, the player guide
[Writing story content](../writing-story-content.md) is the authority, above all its
[Variants](../writing-story-content.md#variants) and [Text](../writing-story-content.md#text)
sections.

## What changed

- **Which fields.** Wherever these fields held one string, they may now hold a list of up
  to eight different strings:
  - a news item's `text` and its `mention` (what people say about the news in small talk),
  - an advert's `text`,
  - a small-talk `line`,
  - the `text` of every letter: a step's `delivery.message`, its `onComplete.message`, a
    branch's `onComplete.message` and a reply's (`choices`) `onComplete.message`,
  - in the exchange file, a news entry's `wire` line.
- **How the game picks.** News, adverts, mentions, small talk and wire lines: one at random
  each time, never the one shown last time straight after it. Letters: in turn, one each time
  the arc comes round, starting from a point fixed for each save; the crew log and the
  Letters window always show the same one.
- **Old files are fine.** A single string stays valid everywhere. Add variants entry by
  entry; an entry you have not reached simply keeps its one line.

## Who does what

| You (ChatGPT) | Claude, after each step |
| --- | --- |
| Add variants to the text fields this page names, in the files of the current step | Run the checks and fix anything they refuse |
| Keep each speaker's voice and every fact the same across an entry's variants | Raise the mod's version and, where a file first uses a list, its Framework minimum |
| Report per file what you changed, and anything you could not do within the rules | Write the changelog, the Workshop page, the language review and the release notes |
| Replace the two placeholders named below, or say they can stay | Build, test, commit, and give the owner steps to see it in play |

Edit only the text fields named here, in the story files and `exchange.json` of the current
step. Leave everything else as it is: ids, flags, tests, requirements, weights, `once` and
`onceEach`, threads, places, people, `notes` (except the placeholder sentence below),
`phobos-addon.json`, `mod_info.json`, changelogs, Workshop files, translations and versions.

## How to write it

A list goes where the string was. Keep the existing line as the **first** variant: its
translation key stays the same, so any translation of it keeps working. Then add the others.
The example below uses two entries from Phobos Agriculture; the added variants only show the
form, so write your own when you reach that step.

```json
"broadcasts": {
  "galley-survey": {
    "region": "Outer System",
    "text": [
      "A survey of long-haul crews puts a hot meal from their own galley ahead of shore leave, a working shower and, in one case, the captain. Hearth-2 owners were the most cheerful respondents, though researchers note they were also the only ones who had eaten that day.",
      "Ask long-haul crews what beats shore leave and, says a new survey, the answer is a hot meal from their own galley, ahead of a working shower and, for one crew, the captain. Hearth-2 owners answered most cheerfully; researchers point out they were also the only ones who had eaten that day."
    ],
    "mention": [
      "Some survey reckons crews rate a hot meal from their own galley above shore leave. Can't say I disagree.",
      "Heard there's a survey saying a galley meal beats shore leave. Whoever wrote it has eaten my cooking."
    ]
  }
},
"chatter": {
  "rack-hum": {
    "moment": "complaint",
    "line": [
      "That grow rack hums all night. I can hear it through the bulkhead.",
      "Third night running that rack's kept me up. Hums like it's proud of itself."
    ]
  }
}
```

A letter is the same, inside its `message`:

```json
"delivery": { "message": { "person": "corvane-ines-varga", "text": [
  "[player-first], one of your bills to Corvane Mutual has gone past its shift. ...",
  "[player-first], a bill of yours to Corvane Mutual is still open past its shift. ..."
] } }
```

And an exchange wire line (in `exchange.json`, inside the company's `news` entry):

```json
"wire": [
  "Smartlink wins a point-defence service contract from a group of tug operators.",
  "Smartlink renews its point-defence service work for a group of tug operators."
]
```

Format the file as it is now: two-space indentation, one variant per line, no other change
to the file's layout or key order.

## Rules for every variant

A variant that breaks one of these is sent back.

1. **The same thing in other words.** Every variant of an entry tells the same news, makes
   the same complaint or sends the same letter. They must all be true at once, and the player
   may meet any of them first, so none may depend on another (no "again", "as I said", "this
   time"), and none adds a new fact, person, place or event.
2. **The same voice.** A letter's variants come from the same person in the same mood;
   a maker's adverts stay that maker's salesperson; a wire line stays a neutral wire report.
3. **The same limits.** News text at most 700 characters, a mention 200, an advert 400, a
   small-talk line 200, a letter 400, a wire line 300.
4. **The same placeholders, or fewer.** Only `[player]`, `[player-first]`, `[ship]`,
   `[place]`, `[region]`, `[station]`, `[body]`, `[date]`, `[crew]` and `[person:<id>]`, and a
   `[person:<id>]` only for someone the entry's thread allows. A wire line has none. Plain
   text only: no angle brackets, no other square brackets.
5. **No two the same.** Two or three variants are the right number for most entries; small
   talk may take four. Never pad a list to eight: a weak variant repeats worse than none.
6. **What stays one string.** Goal titles and descriptions, reply labels, tips, encyclopedia
   articles, data files, and the names, roles and profiles of people, places, companies and
   threads. Do not turn them into lists; the game refuses it.

Each mod keeps its own rules as well; the steps below repeat the ones that matter.

## The steps

Work through them in order and stop after each, so the owner can read your work and Claude
can wire it before the next. The order puts first what players meet most often for the least
writing.

### Step 1: Phobos Banking (`mods/PhobosBank/framework/story.json`)

**Done in Phobos Banking 0.9.0** (8 October 2026), written by Claude after the owner's
ChatGPT plan ended, held for owner review: three wordings for every event letter, two for each
reply, the four mentions rewritten as speech, and two or three wordings for the news, adverts
and talk. A later writer may revise or add to any of them under the same rules.

The smallest pack and the most repetitive: a player who borrows often reads the same letters
many times.

- **The twenty event arcs**, `bank-<lender>-borrowed`, `-repaid`, `-late` and `-late-long`
  for the five lenders: two or three variants for each letter (34 letters in all), the
  `late` and `late-long` letters first.
- **Four mentions to rewrite.** In `bank-halcyon-bond-credit-news`,
  `bank-aerie-savings-credit-news`, `bank-stillwater-advances-credit-news` and
  `bank-narrow-ledger-credit-news` the `mention` repeats the news text word for word. A
  mention is what someone at the docks says about the news: replace each with speech, and
  give it a second variant. These four may change their first line.
- The five news items (`-credit-news`), five adverts (`-advert`) and five small-talk lines
  (`-credit-talk`): two variants each.
- **Placeholder to replace:** the second variant of the `bank-corvane-mutual-late` letter was
  written by Claude only to show the wiring works. Replace it or say it can stay, and delete
  the sentence "Variants after the first are agent placeholders (Framework 0.132.0 proof) for
  the writers to replace." from the arc's `notes`.
- **Banking's rules:** consequences stay the game's own. The game adds a late fee every shift
  a bill stays unpaid; that is all, so no letter may say or hint that anything else will happen
  (collectors, seized cargo, blocked docking, a bounty). Money moves only in the game's Finances
  window. Never quote rates, limits or fees, except that the late fee exists. Ogiso's Bank and
  Ogiso's Register are the game's institutions: refer to them, never speak for them.
- Full rules: [Phobos Banking stories](banking-stories-handoff.md).

### Step 2: Phobos Exchange (`mods/PhobosExchange/framework/exchange.json` and `story.json`)

**Done in Phobos Exchange 0.6.0** (8 October 2026), written by Claude, held for owner review:
three wordings for every wire line and shareholder letter, two or three for every TV report
and advert, and two company-specific small-talk lines for each company's good and bad day
(the shipped two lines were the same for all eight companies). The Smartlink placeholder was
kept and given a third wording. The one-off `lodestar-first-board` letter is left as it is.

Exchange news breaks again every week or two, so the same wire line and TV item come round
often.

- **`exchange.json`: the sixteen wire lines**, one for each company news entry: two or three
  variants each.
- **`story.json`:** the 32 `onceEach` news items (one TV report each time their news breaks),
  the sixteen small-talk lines, the five adverts, and the letters of the repeatable event arcs
  (`exchange-<company>-bought` and `-major-holder`, sixteen letters): two or three variants each.
- **Placeholder to replace:** the second wire variant of Smartlink's
  `exchange-smartlink-spares-contract` news, written by Claude. Replace it or say it can stay,
  and delete the same placeholder sentence from that news entry's `notes`.
- **Exchange's rules:** the game's own companies (Smartlink, Testudo, Ayotimiwa, the Green
  Energy Company) never speak for themselves: news about them is a wire or station report, and
  their letters come from outside voices. No prices, percentages, limits or fees in any text,
  wire lines included ("climbed", "slid", "a strong week" instead). Stories never claim to
  control prices by any means other than their news entries.
- Full rules: [Phobos Exchange stories](exchange-stories-handoff.md).

### Step 3: Phobos Agriculture (`mods/PhobosAgriculture/framework/story.json`)

**Done in Phobos Agriculture 0.67.0** (8 October 2026), written by Claude, held for owner
review: two wordings for each news item and advert, three for each mention and small-talk
line. Agriculture's Framework minimum rose to 0.132.0 with it.

A short step: three news items with their mentions, two adverts and six small-talk lines,
two or three variants each. The arc's two letters are read once a save; leave them.

- The adverts are Verdemorrow's own (Firstlight and Groundwork), in the maker's salesperson
  voice: why a working spacer would buy, accurate limits, no recipe manual.

### Step 4: Phobos Spacer Stories, station life (`mods/PhobosSpacerStories/phobos/PhobosFramework/story/11-station-life.json`)

The largest pool of everyday talk, heard on every station.

- The 59 small-talk lines first, then the mentions of all 37 news items (people repeat news
  for days after it shows, so mentions of `once` news count too), then the 24 news items
  without `once` and the 13 adverts.
- **Spacer Stories' rules:** everything belongs to its thread and place as it does now; no
  spoilers of stories the player has not reached; the voice is practical and worn-in with dry
  humour in routine lines. Full rules: [Spacer Stories rewrite handoff](spacer-stories-rewrite-handoff.md).

### Step 5: Phobos Spacer Stories, the other talk and news (files `01` to `07` in the same folder)

The same work on the remaining files with news, adverts and talk: 84 small-talk lines, 51
mentions, 41 news items without `once` and 26 adverts. Take one or two files at a time if
that suits.

### Not needed

- Files `08` (goal chains) and `12` (correspondence): their letters belong to arcs read once
  a save, so variants would only differ between games.
- Files `09` and `13` (data files) and `10` (threads): nothing there takes variants.
- Framework's own `story.json` (places and people only) and the Keelhaul example add-on
  (it already shows variants).

## Checking your work

In the repository:

- `python scripts/validate-data-packs.py` checks every shipped pack, Banking, Exchange and
  Agriculture included; it names any variant that is too long, repeated, empty or uses a
  placeholder it may not, counting variants from 1 (`/text[2]`).
- `python scripts/validate-data-packs.py --addon mods/PhobosSpacerStories` checks the Spacer
  Stories files.
- `python scripts/check-json-schemas.py` checks the shipped packs against the JSON Schemas;
  editors that read `schemas/story.schema.json` and `schemas/exchange.schema.json` flag
  mistakes as you type.

In the game, the owner can list every variant of an entry with its translation key by
typing `phobosframework story variants <id>` in the F3 console.

## What to send back

After each step:

- the edited files, whole;
- per file, the entries you gave variants (a count is enough where you did all of a kind),
  any first line you changed and why (the four Banking mentions), and the placeholders you
  replaced or kept;
- anything you could not do within the rules, or a text you think should change beyond
  adding variants, flagged rather than changed.
