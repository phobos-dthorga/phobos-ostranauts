# Phobos Exchange stories: handoff for ChatGPT

Handoff, 7 October 2026 (Phobos Exchange 0.2.0, histories from 0.3.0, held drafts). The owner's direction: the
creative side of the Phobos mods (companies, their histories, news, adverts, letters and
small talk) is data that ChatGPT writes and players can add to; Claude builds the logic,
the schemas and the checks. This page is what you need to write the stories behind the
Lodestar Exchange. Nothing here is tested in play yet.

Two files carry the exchange's world:

- **`mods/PhobosExchange/framework/exchange.json`** (the `exchange` pack): the companies,
  their profiles and, since 0.2.0, their **news**: story flags that move a company's price;
  since 0.3.0 also their founding and listing years and their **history** before the game.
  Its fields are in [Listing a company on the exchange](../editing-data-files.md#listing-a-company-on-the-exchange).
- **`mods/PhobosExchange/framework/story.json`** (a `story` pack): people, threads, news,
  adverts, small talk, articles and arcs, written exactly as
  [writing story content](../writing-story-content.md) describes. It ships with one thread
  for the exchange and one per company and nothing else: that is your canvas.

## What exists

The exchange itself is **the Lodestar Exchange** (agent-chosen name: the star a navigator
steers by; plain, brisk, a little proud of itself). Its thread is `exchange-lodestar`.

| Company id | Ticker | Name | Sector | Whose | Thread | What moves it (the game's own station markets) |
| --- | --- | --- | --- | --- | --- | --- |
| `smartlink` | SMLK | Smartlink | Heavy industry | the game's | `exchange-smartlink` | Weapons at Port Yangshan (Mars) and Cassini Spaceport (Titan) |
| `testudo` | TSTD | Testudo | Heavy industry | the game's | `exchange-testudo` | Hull at Cassini Spaceport and Cloudbreak (Venus); metal at Port Yangshan, an input |
| `ayotimiwa` | AYO | Ayotimiwa Corp. | Heavy industry | the game's | `exchange-ayotimiwa` | Hull and ore at K-Leg |
| `green-energy` | GEC | The Green Energy Company | Food and comforts | the game's | `exchange-green-energy` | Intoxicants at Zhonghuamen Terminal and Port Shajiang (Luna) |
| `brightvein` | BVM | Brightvein Mining | Mining and volatiles | invented | `exchange-brightvein` | Ore at Zhonghuamen Terminal; metal at Port Yangshan |
| `coldwell` | CWV | Coldwell Volatiles | Mining and volatiles | invented | `exchange-coldwell` | Volatiles at Port Yangshan and Upsilon Docking (Deimos); helium-3 at Venus Orbital |
| `verdemorrow` | VERD | Verdemorrow | Food and comforts | ours (Phobos Agriculture's maker) | `exchange-verdemorrow` | Food at Zhonghuamen Terminal and Qincheng Station (Mercury) |
| `halewright` | HALE | Halewright | Medicine | ours (Phobos Medical's maker) | `exchange-halewright` | Medical supplies at Zhonghuamen Terminal, Qiantangmen and Cassini Spaceport |

What the game itself says about its four companies, so the lore stays consistent:
Smartlink is a weapons maker the game calls an "ubercorp", proud of its point-defence
cannon suites, which its rivals call grandstanding. Testudo builds the Dream, Rouncy, Mesa
and Bulk Lifter ships and makes hardsuits, flooring and station furniture (a lobby seat
designed for CCRE's second Ring Station). Ayotimiwa Corp. runs K-Leg's shipbreaking, salvage
buying, kiosks and refuelling with the OKLG Port Authority. The Green Energy Company makes
intoxicants in the Rosebud on Tharsis Landing, Damask Rose cigarettes best known. Verdemorrow
and Halewright are ours: see the equipment and their sales voices in the Agriculture and
Medical item references and `docs/development/equipment-branding.md`. Brightvein Mining and
Coldwell Volatiles have only their profiles so far: their histories, people and voices are
yours to invent.

## Hooks: how stories and prices meet

**Story into price: news.** Each company's entry in `exchange.json` may list up to twelve
`news` items: a story flag, how far the price moves when the flag is set (`move`, a share of
the price from -0.3 to 0.3, at least 0.005 either way) and the line the market wire prints
(`wire`, up to 300 characters, no placeholders). The move happens once per save, the moment
the flag is set, and it stays: a contract won or a yard lost changes what the company is
worth. Any story arc can set the flag (`onComplete.setFlags`), so you write the story in
`story.json` and say what it does to the price in `exchange.json`:

```json
"news": [ { "flag": "exchange-testudo-titan-yard-contract", "move": 0.08,
            "wire": "Testudo wins the Cassini Spaceport yard contract for the coming season." } ]
```

**Price into story: events.** Phobos Exchange starts the arc `exchange-<company>-<event>`
when a story pack has one (its `requires` still apply), and keeps a flag of the same name:

| Event | When | Flag kept |
| --- | --- | --- |
| `surge` | A company's price rose by the wire's threshold (5%) over a game day | `exchange-<company>-surge` while the latest big move was up (cleared by a slump) |
| `slump` | It fell that far | `exchange-<company>-slump` while the latest big move was down |
| `bought` | The player buys the company's shares, from none | `exchange-<company>-bought` (cleared when they sell out) |
| `major-holder` | The player's holding reaches half of what one trader may hold (125,000 credits by default) | `exchange-<company>-major-holder` while it stays there |
| `sold-out` | The player sells their last share of the company | `exchange-<company>-sold-out` until they buy again |

Event arcs take `"chance": 0` (only the event starts them) and usually `"repeatable": true`
(a second surge brings a second letter), and belong to the company's thread. News, adverts
and small talk may `require` these flags, so a slump can bring gloomy talk at the docks.

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

- No figures in a `line`: the chart shows how far it went. Write "Wins the Cassini yard
  contract", not "Shares jump 40% on the Cassini contract".
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

**What ships now (placeholders, agent choice).** These years were set only so the feature
can be seen working. Replace any of them; nothing else depends on them.

| Company | Founded | Listed |
| --- | --- | --- |
| The Lodestar Exchange | | opened 2034 |
| Smartlink | 1952 | 2036 |
| Testudo | 2019 | 2038 |
| Ayotimiwa Corp. | 1987 | 2034 |
| The Green Energy Company | 2034 | 2040 |
| Brightvein Mining | 2044 | 2051 |
| Coldwell Volatiles | 2038 | 2047 |
| Verdemorrow | 2049 | 2062 |
| Halewright | 2045 | 2058 |

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
- **Weeks, not hours.** Prices run in phases that last weeks; stories pace the same way.
  Wait tests of days fit better than hours.
- **Standing** changes stay small (up to 10 points, two factions an outcome) and go to the
  game's own factions.
- **Ids**: lower case with dashes. Exchange content uses `exchange-`; event arcs must be
  exactly `exchange-<company>-<event>`; give news flags `exchange-<company>-` and a short
  name. Keep the shipped thread keys; you may rename their titles.
- **Everything belongs somewhere**: every arc, news item, advert and small-talk line names a
  thread (a company's or `exchange-lodestar`); company threads have no place, so letters
  reach the player anywhere, but news and adverts may take a `place` (the station a company
  trades from) so they show more often there.
- Facts from finance stay honest: a market can be talked up or down, but the code's prices
  come from the station markets, the phases and the news entries; stories never claim to
  control them by any other means.

## What would bring it to life (wanted)

- A founding year, a listing year and, where it helps, a listing price for each company,
  with three to six history entries each: how it began, its best and worst years, and what
  it is chasing now. An opening year and a history for the Lodestar Exchange itself, with
  the events that shook every company (the Kessler collapse of 2059 above all).
- A short history, a voice and one or two named people for each company (and for the
  exchange's desk), people being outsiders for the game's four.
- Two or three `news` entries per company, each told by an arc: contracts, accidents,
  shortages, a new ship class, a recall, a strike, with moves from 3% to 15%.
- Event letters for `bought` (a welcome from a broker or the company's shareholder desk,
  for ours and the invented ones) and `major-holder`, and wire-style news for `surge` and
  `slump`.
- Adverts for Verdemorrow, Halewright, Brightvein and Coldwell, and for the exchange itself.
- Small talk about the markets for dockside crowds, tied to the surge and slump flags.
- An encyclopedia article on the Lodestar Exchange: what it is, who trades, how a spacer
  reads the board.

## How it is wired and checked

- The C# checks require every shipped arc named `exchange-…` to answer a known company's
  event, every event id to be a valid story id, and the story pack to keep its threads.
  Since 0.3.0 they also require every shipped company to have a founding and a listing year,
  and check the history rules above in the game's own code and the Python mirror.
- The `exchange` validator checks news flags, moves and wire lines; the story validator
  checks the story pack. Both have Python mirrors and JSON Schemas for your editor.
- Players can do all of this too: [the Keelhaul example add-on](../../examples/addons/PhobosExampleKeelhaulListing/README.md)
  lists a company, tells a story that moves its price, and answers its `bought` event.
  Add-ons may name `exchange-<their prefix>…` ids because Phobos Exchange registers the
  `exchange` namespace (Framework 0.129.0).
- Changes go in Draft with the changelog, the language ledger and the release notes, as for
  Banking's stories, until the owner confirms publication.
