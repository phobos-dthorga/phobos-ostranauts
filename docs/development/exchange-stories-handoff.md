# Phobos Exchange stories: handoff for ChatGPT

Handoff, 7 October 2026 (Phobos Exchange 0.2.0, held draft). The owner's direction: the
creative side of the Phobos mods (companies, their histories, news, adverts, letters and
small talk) is data that ChatGPT writes and players can add to; Claude builds the logic,
the schemas and the checks. This page is what you need to write the stories behind the
Lodestar Exchange. Nothing here is tested in play yet.

Two files carry the exchange's world:

- **`mods/PhobosExchange/framework/exchange.json`** (the `exchange` pack): the companies,
  their profiles and, since 0.2.0, their **news**: story flags that move a company's price.
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
- The `exchange` validator checks news flags, moves and wire lines; the story validator
  checks the story pack. Both have Python mirrors and JSON Schemas for your editor.
- Players can do all of this too: [the Keelhaul example add-on](../../examples/addons/PhobosExampleKeelhaulListing/README.md)
  lists a company, tells a story that moves its price, and answers its `bought` event.
  Add-ons may name `exchange-<their prefix>…` ids because Phobos Exchange registers the
  `exchange` namespace (Framework 0.129.0).
- Changes go in Draft with the changelog, the language ledger and the release notes, as for
  Banking's stories, until the owner confirms publication.
