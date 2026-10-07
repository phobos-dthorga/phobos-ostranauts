# Phobos Exchange stories: handoff for ChatGPT

Handoff, 7 October 2026 (Phobos Exchange 0.2.0, histories from 0.3.0, held draft). First content draft, 7 October 2026: histories and story content now fill the exchange and story packs; all newly invented dates and events remain author-written proposals for owner review. Offline pack validation passes. Nothing here has been checked in play.

The owner's direction: creative content (companies, histories, news, adverts, letters and small talk) is schema-checked data that ChatGPT writes and players can add to; Claude builds the logic, schemas, validators and hooks. This page records the voice, limits, canon anchors and structure for the Lodestar Exchange.

Two files carry the exchange's world:

- **`mods/PhobosExchange/framework/exchange.json`** (the `exchange` pack): the companies,
  their profiles and, since 0.2.0, their **news**: story flags that move a company's price;
  since 0.3.0 also their founding and listing years and their **history** before the game.
  Its fields are in [Listing a company on the exchange](../editing-data-files.md#listing-a-company-on-the-exchange).
- **`mods/PhobosExchange/framework/story.json`** (a `story` pack): named correspondents, one thread for the Exchange desk and one per company, news, adverts, small talk, an encyclopedia article and event arcs, following [writing story content](../writing-story-content.md). The current pack is the first authored draft; new content remains held for owner review.

## What exists

The exchange itself is **the Lodestar Exchange** (agent-chosen name: the star a navigator
steers by; plain, brisk, a little proud of itself). Its thread is `exchange-lodestar`.

| Company id | Ticker | Name | Sector (id) | Whose | Thread | What moves it (the game's own station markets) |
| --- | --- | --- | --- | --- | --- | --- |
| `smartlink` | SMLK | Smartlink | Heavy industry (`industry`) | the game's | `exchange-smartlink` | Weapons at Port Yangshan (Mars) and Cassini Spaceport (Titan) |
| `testudo` | TSTD | Testudo | Heavy industry (`industry`) | the game's | `exchange-testudo` | Hull at Cassini Spaceport and Cloudbreak (Venus); metal at Port Yangshan, an input |
| `ayotimiwa` | AYO | Ayotimiwa Corp. | Heavy industry (`industry`) | the game's | `exchange-ayotimiwa` | Hull and ore at K-Leg |
| `green-energy` | GEC | The Green Energy Company | Food and comforts (`consumer`) | the game's | `exchange-green-energy` | Intoxicants at Zhonghuamen Terminal and Port Shajiang (Luna) |
| `brightvein` | BVM | Brightvein Mining | Mining and volatiles (`resources`) | invented | `exchange-brightvein` | Ore at Zhonghuamen Terminal; metal at Port Yangshan |
| `coldwell` | CWV | Coldwell Volatiles | Mining and volatiles (`resources`) | invented | `exchange-coldwell` | Volatiles at Port Yangshan and Upsilon Docking (Deimos); helium-3 at Venus Orbital |
| `verdemorrow` | VERD | Verdemorrow | Food and comforts (`consumer`) | ours (Phobos Agriculture's maker) | `exchange-verdemorrow` | Food at Zhonghuamen Terminal and Qincheng Station (Mercury) |
| `halewright` | HALE | Halewright | Medicine (`health`) | ours (Phobos Medical's maker) | `exchange-halewright` | Medical supplies at Zhonghuamen Terminal, Qiantangmen and Cassini Spaceport |

The four sectors are keyed `industry`, `resources`, `consumer` and `health` in
`exchange.json`; a sector's history sits under its key.

Canon anchors below come from Blue Bottle Games' installed Ostranauts 1.0.1.5 data: the local primary source at `Ostranauts_Data/StreamingAssets/data`, especially `tips/tips.json`, `headlines` and the relevant company and ship definitions. The files stay local and are not redistributed. See Blue Bottle Games' [Ostranauts](https://store.steampowered.com/app/1022980/Ostranauts/) and Joshu's [Official Ostranauts Modding Guide, 22 June 2026](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946).

What the game itself says about its four companies, so the lore stays consistent:
Smartlink is a weapons maker the game calls an "ubercorp", proud of its point-defence
cannon suites, which its rivals call grandstanding. Testudo builds the Dream, Rouncy, Mesa
and Bulk Lifter ships and makes hardsuits, flooring and station furniture (a lobby seat
designed for CCRE's second Ring Station). Ayotimiwa Corp. runs K-Leg's shipbreaking, salvage
buying, kiosks and refuelling with the OKLG Port Authority. The Green Energy Company makes
intoxicants in the Rosebud on Tharsis Landing, Damask Rose cigarettes best known. Verdemorrow
and Halewright are ours: see the equipment and their sales voices in the Agriculture and
Medical item references and `docs/development/equipment-branding.md`. Brightvein Mining and
Coldwell Volatiles are invented for this mod; their first-draft histories, people and voices
are proposals, not Ostranauts canon.

## Hooks: how stories and prices meet

**Story into price: news.** Each company's entry in `exchange.json` may list up to twelve
`news` items. News recurs (Phobos Exchange 0.4.0, owner direction 8 October 2026: marked
effects over time, again and again, as the game's own headlines do). Each item has:

- `flag`: the story flag that brings it. It breaks each time the flag is set, or set again;
  an arc that sets a flag already set renews it (Framework 0.131.0).
- `move`: how far the price jumps at once, a share from -0.3 to 0.3, at least 0.005 either way.
- `wire`: the line the market wire prints, up to 300 characters, no placeholders. Since
  Phobos Exchange 0.5.0 it may be a list of up to eight variants; the wire prints one each
  time the news breaks, never the same one twice running (see Variant lines below).
- `carry`: how far the company's trend carries the price on afterwards, at its height a
  few weeks later, from -0.15 to 0.15, the same way as the move. Usually, not always: the
  market's own ups and downs still apply.
- `keeps`: the share of the jump that stays for good the first time it breaks in a save
  (0 to 1). The rest, and the whole jump every later time, unwinds over the company's
  `newsFadeDays` (a half-life in game days, default 30).

A jump may unwind by no more than about 2% in its first week, so a bigger move needs a longer
`newsFadeDays` (the validator says so). You write the story in `story.json` and say what it
does to the price in `exchange.json`:

```json
"news": [ { "flag": "exchange-testudo-titan-yard-contract", "move": 0.08, "carry": 0.04, "keeps": 0.3,
            "wire": "Testudo wins the Cassini Spaceport yard contract for the coming season." } ]
```

**How news comes round again.** Make the arc that sets the flag `"repeatable": true` with a
`"cooldownDays"` (the fewest game days before it may start again; the shipped news uses 7,
the owner's choice of weekly news) and a `chance`. Give its TV item `"onceEach": true` with
the flag in `requires.flags`, so it shows once each time the news breaks, not once a save.
Write lines that bear repeating: the second contract of a kind reads as well as the first.

**Price into story: events.** Phobos Exchange starts the arc `exchange-<company>-<event>`
when a story pack has one (its `requires` still apply), and keeps a flag of the same name:

| Event | When | Flag kept |
| --- | --- | --- |
| `surge` | A company's price rose by the wire's threshold (5%) over a game day | `exchange-<company>-surge` while the latest big move was up (cleared by a slump); renewed at each surge, so `onceEach` news follows every one |
| `slump` | It fell that far | `exchange-<company>-slump` while the latest big move was down |
| `bought` | The player buys the company's shares, from none | `exchange-<company>-bought` (cleared when they sell out) |
| `major-holder` | The player's holding reaches half of what one trader may hold (125,000 credits by default) | `exchange-<company>-major-holder` while it stays there |
| `sold-out` | The player sells their last share of the company | `exchange-<company>-sold-out` until they buy again |

Event arcs default to no chance (only the event starts them) and can be repeatable, so later surges can bring later reports. They belong to the company's thread. News, adverts and small talk may require these flags, so a slump can bring gloomy talk at the docks.

## Company histories (Phobos Exchange 0.3.0)

Every company now has a past before the game begins, and you write it. The exchange draws
each company's price chart back to the day it listed, shaped by what you put in
`exchange.json`, so the history you give a company is something the player can see on its
**All** chart and read on its page under **Through the years**. Use it to decide who these
companies are: how old they are, where they came from, what they survived and what they
want now.

**The fields** (all in `exchange.json`; the [editing guide](../editing-data-files.md#listing-a-company-on-the-exchange)
has the exact limits):

- `founded`: the year the company was founded. Its age, for the story; shown on its page.
- `listed`: the year its shares first traded on the Lodestar Exchange. Its chart starts
  here. Defaults to `founded`.
- `listingPrice`: the share price when it listed, in credits. It says how much the company
  has grown since. Optional; leave it out and the price simply grows at the company's usual
  pace back from the present.
- `history`: dated entries by id, each with a `year`, an optional `month` (1 to 12), a
  `line` for the company page and, if the event moved the price, a `move` (0.4 is up 40%,
  -0.3 down 30%). An entry without a `move` is lore only.
- The exchange itself has an `opened` year and a `history`, and so does each sector. An
  exchange event moves every company listed at the time (each by how closely it follows
  the market); a sector event moves that sector's companies.

**Limits** (the validators refuse anything outside them):

| What | Limit |
| --- | --- |
| Entries in one history | 12 for a company, 8 for a sector, 16 for the exchange |
| Entry id | lower case letters and digits joined by dashes, at most 24 characters, unique within its history |
| `year` (and `founded`, `listed`, `opened`) | a whole year from 1879 to 2078 |
| `month` | 1 to 12 |
| `move` | from -0.6 to 1.0 (a fall of at most 60%, a rise of at most 100%), at least 0.01 either way; only by 2076 |
| `line` | up to 200 characters, no placeholders in square brackets |
| `listingPrice` | 0.01 to 100,000 credits, only for a company listed by 2076 |

**What it looks like.** An illustration of the format only, not lore: the company and its
events are made up, so do not copy them. Inside a company's entry:

```json
"founded": 2019,
"listed": 2038,
"listingPrice": 12,
"history": {
  "first-yard": { "year": 2021, "line": "Opens its first orbital yard over Lagos with two borrowed cranes." },
  "colony-haulers": { "year": 2041, "month": 5, "move": 0.35, "line": "Wins the colony haulage contracts and doubles its yard crews." },
  "yard-fire": { "year": 2044, "month": 9, "move": -0.2, "line": "A fire guts the main assembly hall; two hulls are lost on the slips." }
}
```

And the exchange's own history, inside `market`:

```json
"opened": 2034,
"history": {
  "first-bell": { "year": 2034, "month": 3, "line": "The first trading session runs from a rented office on Luna." },
  "the-long-silence": { "year": 2059, "month": 11, "move": -0.4, "line": "Earth goes dark behind the debris; the board stays shut for nine days and reopens far lower." }
}
```

**The range.** A new game always begins in 2079 (the game's own start date). Every year you
give runs from **1879 to 2078**, at most 200 years back. A company cannot list before the
exchange opened, or before it was founded. An event that moved a price must fall on or
after the listing and **by 2076**: the two years before the game begins are the market's own
recent record, drawn by the market itself. Lore-only entries may go back to the founding,
so a company founded in 1952 and listed in 2036 can have its 1950s told on its page.

**How the chart is drawn.** The price at listing (yours, or one worked back from today's)
and today's price are fixed; your events decide when the rises and falls in between
happened, and the market's ordinary ups and downs fill the rest. A 40% rise in 2066 is a
step up in 2066 on the chart. A listing price must not make the company grow faster than
25% a year, or shrink more than 5% a year, once your events are counted, so the chart
never shows a cliff. History never moves a price in play: that is what `news` is for. The
drawn past is redrawn whenever the game loads, so when you revise a history, saves already
started show the new one.

**Rules for histories:**

- No prices, percentages or share figures in a `line`: the chart shows how far it went.
  Write "Wins the Cassini yard contract", not "Shares jump 40% on the Cassini contract".
  Ordinary counts are fine ("two hulls are lost"). The page prints the year before each
  line ("2041: Wins the colony haulage contracts"), so do not repeat it.
- Give a `month` to any event whose timing matters, such as two events in the same year
  that must come in order. Without one, the month is picked by a fixed calculation from
  the entry's id: the same for every player, but not one you chose.
- For a company listed long ago, set a `listingPrice`. Without one, the price is worked
  back from today at the company's usual growth, which over many decades can leave it at a
  fraction of a credit a share.
- The game's own companies are described from outside, as for news: a wire report or a
  historian's line, never their own press release.
- Check the game's own text before dating anything to do with its companies or places
  (below), and never contradict it. Invent freely where the game says nothing.
- Space came late: offworld settlement began in the 2020s and 2030s, so a company founded
  earlier did its early work on Earth. The Kessler collapse of 2059 ended the old world;
  something that happened to every company at once belongs in the exchange's own history.
- Ids are lower case with dashes, unique within their table. Lines are translatable as
  `Companies.<company>.history.<id>`, `Sectors.<sector>.history.<id>` and
  `Market.history.<id>`.

**What the game itself dates** (from its loading tips, its data files and its ship files):

| Year | Event |
| --- | --- |
| 2021 | PASS founded; Jade Rabbit on Luna is the oldest offworld colony |
| 2025 to 2029 | The Kronos missions reach Titan |
| 2027 | RST founded |
| 2029 | Tharsis Landing incorporated; Ceres Resource Extraction builds the ring station (forced out of business in 2037) |
| 2032 | Ayotimiwa, then a Nigerian shipping company, establishes K-Leg with the OKLG Port Authority; shipbreaking from 2036 |
| 2034 | The Growers Alliance of California becomes the Green Energy Company |
| 2035 | Europa and Ganymede settled |
| 2041 | Testudo's Mesa in production |
| 2051 | Xinhua overthrows Old China |
| 2054 | Fort Simpson founded after the Ganymede Coup |
| 2059 | The Kessler collapse: "the old world died" |
| 2079 | The game begins |

Smartlink was a US military contractor before the colonies and made Titan its offworld
headquarters; the game gives no founding year. The game is inconsistent on Newcal's
independence (2059 or 2062), so leave that date alone.

**First-draft dates (author choices, owner review pending).** The opening, founding, listing and listing-price values are creative choices where the canon list below does not establish them. The Green Energy Company's transformation year, Ayotimiwa's K-Leg work, Testudo's Mesa production and the wider disasters remain anchored to the game's own text.

| Company | Founded | Listed |
| --- | ---: | ---: |
| The Lodestar Exchange | — | opened 2034 |
| Smartlink | 1952 | 2036 |
| Testudo | 2019 | 2038 |
| Ayotimiwa Corp. | 1987 | 2034 |
| The Green Energy Company | 1982 | 2040 |
| Brightvein Mining | 2044 | 2051 |
| Coldwell Volatiles | 2038 | 2047 |
| Verdemorrow | 2049 | 2062 |
| Halewright | 2045 | 2058 |

## Variant lines (Phobos Exchange 0.5.0, Framework 0.132.0)

Owner direction, 8 October 2026: text that comes round again may be written several ways.
Wherever one string was allowed in these places, a list of up to eight different strings
now is too, and the game picks one each time:

- in `exchange.json`, a news entry's `wire`;
- in `story.json`, a news item's `text` and `mention`, an advert's `text`, a small-talk
  `line`, and the `text` of every letter (delivery, outcome, branch and reply).

News, adverts, mentions, small talk and the wire pick at random, never the one just shown;
a letter takes its variants in turn, one per time its arc comes round. Each variant keeps
the field's own limits and placeholders, and no two may be the same. A single string still
works everywhere, so nothing has to change at once: add variants entry by entry, at your
own pace. Write variants as the same news in other words, not different events, and keep a
correspondent's voice the same across a letter's variants. Goal titles and descriptions,
reply labels, names, profiles and history lines stay one string.

**Where variants are wanted most**, in order:

1. The wire lines of all sixteen shipped news entries: each breaks again and again
   (weekly by the shipped cooldowns), so the same line every week reads like a stuck
   record. Two or three each.
2. The 32 `onceEach` news items and the letters of the repeatable event arcs (`bought`,
   `major-holder`), which also come round.
3. The sixteen small-talk lines, which the docks repeat most of all.

**The proof already in place:** Smartlink's spares-contract news
(`exchange-smartlink-spares-contract`) carries a second wire variant written by Claude only
to show the wiring works; its `notes` say so. Replace it, or keep it if it reads well.
The Keelhaul example add-on shows variants on its wire line, its surge news and its
large-holder letter. This is step 2 of the [variant lines handoff](variant-lines-handoff.md),
which has the format, the rules for every variant and the order of the other steps.

## Rules (the code holds the text to these)

These follow the Banking precedent
([Phobos Banking stories](banking-stories-handoff.md)), adapted to shares:

- **The game's own companies never speak for themselves.** For Smartlink, Testudo,
  Ayotimiwa and the Green Energy Company, write news as a wire service or station press would
  report it, and letters only from people outside them (a broker, a dockworker, a rival, the
  exchange's own desk). No press releases, adverts, spokespeople or internal memos in their
  voice. Our makers (Verdemorrow, Halewright) and the invented companies may advertise and
  write letters in their own voice, in the maker's salesperson voice for adverts.
- **Promise only what the code does.** A letter or news item may move a price only through
  a `news` entry; it cannot pay dividends, cancel a holding, buy or sell for the player, or
  change a commission. Do not imply insider trading is punished or rewarded beyond what an
  arc's own steps do. `credits` rewards stay small and never stand in for share value.
- **Never quote prices, percentages, limits or fees** in story text: the exchange file and
  the market move them. Say "climbed", "slid", "a strong week"; the wire line of a `news`
  entry is the one place to say what happened, and still without figures (the wire adds
  the percentage itself).
- **News comes round.** Each piece of news can break again and again; make its arc
  repeatable with a cooldown, its TV item `onceEach`, and its lines fit to read more than
  once, with variants where the same words would grate.
- **Weeks, not hours.** Prices run in phases that last weeks; stories pace the same way. A
  `wait` test counts game hours, up to 720 (about thirty game days); waits of a day (24) or
  more suit the market better than a few hours.
- **Standing** changes stay small (up to 10 points, two factions an outcome) and go to the
  game's own factions.
- **Ids**: lower case with dashes. Event arcs must be exactly `exchange-<company>-<event>`; that prefix is reserved for the Exchange's five runtime events. A story arc that sets a company-news flag uses `lodestar-<company>-<news>`; the flag itself is `exchange-<company>-<news>`. Keep the shipped thread keys; you may rename their titles.
- **Everything belongs somewhere**: every arc, news item, advert and small-talk line names a
  thread (a company's or `exchange-lodestar`); company threads have no place, so letters
  reach the player anywhere, but news and adverts may take a `place` (the station a company
  trades from) so they show more often there.
- Facts from finance stay honest: a market can be talked up or down, but the code's prices
  come from the station markets, the phases and the news entries; stories never claim to
  control them by any other means.

## First draft content delivered

- Histories for the Exchange, all four sectors and all eight companies: 62 dated entries in all, with three to six company entries each.
- A founding and listing year for every company, an opening year and a calibrated listing price. All unsupported years and events are labeled as author choices for owner review.
- One named correspondent per company and a named Exchange desk contact. The four game companies use outside voices; invented companies and Phobos makers may speak for themselves.
- Two story-driven price-news entries per company, each with a wire line and a delayed flag from its arc.
- Event arcs for `bought`, `major-holder`, `surge` and `slump`; letters for the first two and a TV report for the latter two.
- Five adverts, local small talk tied to the latest surge or slump, and an encyclopedia article on reading the board.
- The Keelhaul example add-on demonstrates company history, news, price flags, event responses, adverts, chatter and an encyclopedia article.

## How it is wired and checked

- The C# checks require every shipped arc named `exchange-…` to answer a known company's
  event, every event id to be a valid story id, and the story pack to keep its threads.
  Since 0.3.0 they also require every shipped company to have a founding and a listing year,
  and check the history rules above in the game's own code and the Python mirror.
- The `exchange` validator checks news flags, moves and wire lines, and the history rules;
  the story validator checks the story pack. Both have Python mirrors and JSON Schemas for
  your editor.
- To check your work: run `python scripts/validate-data-packs.py` in the repository, which
  names any field outside its limits. In the game, a company's **All** chart and its page
  show the history, and the F3 command `phobosexchange history SMLK` (or `history` alone
  for the exchange) prints it.
- Players can do all of this too: [the Keelhaul example add-on](../../examples/addons/PhobosExampleKeelhaulListing/README.md)
  lists a company, tells a story that moves its price, answers its `bought` event and gives
  the company a history before the game.
  Add-ons may name `exchange-<their prefix>…` ids because Phobos Exchange registers the
  `exchange` namespace (Framework 0.129.0).
- These are first-draft creative choices, not established canon. Keep the release held until owner review and gameplay checks; update the changelog, language ledger, Workshop page and generated release notes together.
