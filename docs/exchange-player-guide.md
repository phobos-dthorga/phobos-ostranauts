# Phobos Exchange: player guide

Phobos Exchange adds an **EXCHANGE** app to your PDA: shares in the system's companies,
from the game's own ubercorps to the makers of Phobos equipment, priced around the
clock on the Lodestar Exchange. Watch their charts, buy and sell for cash, and set price
alerts that keep working while you skip time. Version 0.1.0 is a held draft: it has been
built and checked offline, and nothing about it has been seen in play yet.

## Opening it

1. Open the PDA's home screen.
2. Tap **EXCHANGE**. The PDA closes and the Exchange panel opens.
3. **Market** lists the companies by sector; **Your shares** shows what you hold.
   **Close** returns you to the game; **Back** returns to the list on a narrow screen.

The first time you open it in a save, the charts already reach back: two years of the
market's own recent record, and before that each company's history since it listed, so
there is something to read straight away however late in a game you install it.

## What moves a price

Every company's price is made of a few parts, and the wire tells you which one did the
moving:

- **The cargo markets it trades with.** Each company follows the game's own cargo market
  at a few named stations. When weapons run short at Port Yangshan, Smartlink climbs;
  when ore piles up at Zhonghuamen Terminal, Brightvein Mining slides. These are the same
  station markets the game's traders and haulers work, so what you see on your travels
  is what moves the board.
- **Trend phases.** Prices run in rising and falling stretches that last weeks: the whole
  market, each sector and each company have their own. A phase that has turned tends to
  keep going for a while, which is where the money is.
- **Steady growth.** Over the years, shares creep upward, as they do in real markets.
  It is slow: a few percent a game year, less than the cost of the trading itself over a
  short stay. A loan from the Phobos Banking lenders costs more than shares earn on
  average, so borrowing to sit on shares loses money.
- **Day-to-day noise and the odd jolt.** Small daily wobbles, calm and stormy stretches,
  and now and then a sudden jump nobody saw coming.
- **Your own orders.** A big order nudges the price against you, and the push fades over
  about a day.

Big moves over a day and the turn of a phase go out on the **market wire**: a line in the
crew log and on the Market page, naming the company and the cause, for example "Market
wire: Smartlink (SMLK) rose 6.2% over the day, as Weapons ran short at Port Yangshan."

## News and stories

From version 0.2.0 the exchange has a story side. Story news can move a company's price
for good: when a contract is won or a yard burns, the wire carries the line and the price
jumps once. Stories also notice what you do: your first purchase of a company, a large
stake, selling out, and a company's biggest moves can each bring a letter or news, when a
story pack has one. Version 0.2.0 ships the hooks and a thread for each company; the stories
themselves come in a later release, and add-ons can bring their own (see
[Publishing an add-on](publishing-an-add-on.md)).

## Reading a company

Pick a company in the list. Its page shows:

- the price, what you would get selling (the lower figure) and what you would pay buying
  (the higher one); the gap between them is the **spread**;
- the change over the last game day and week;
- when it was founded and when it listed on the exchange;
- a chart, over **3 days** (hourly), **120 days** (daily), **2 years** (weekly) or **All**,
  with your alert levels and what you paid marked as dashed lines; point at the chart to
  read a value;
- what the company does, and what you hold;
- **Through the years**: the events in its past, and the exchange's and its sector's since
  it was founded, each with how far it moved this company's price.

## A company's history

**All** shows a company's whole price history, from the day it listed to now, with the
years along the bottom. It is drawn on a scale where each step up the side is ten times
the last, so a company that grew from a few credits a share still reads clearly, and a fall
of half looks the same in any decade. Faint dashed lines mark the big events of its past;
point at one to read what happened.

The history from before your first day with the exchange is drawn from the company list,
the same for you every time you load: it starts at the company's listing price (when the
list gives one), passes through the events of its past, and meets the market's own record
two years before you started. It is history, not a forecast: it never moves a price in
play, and it can change when a writer or an add-on adds to a company's story. What your
own save has seen stays as it was.

The founding and listing years shipped with this version are placeholders, to be replaced
by the companies' real histories in a later release. The exchange's own opening year and
history are on the Market page.

## Buying and selling

Choose how many lots to trade with the stepper; the page works out what buying or selling
that many would cost or fetch, commission included. Press **Buy** or **Sell**, check the
figures on the card, and confirm. The order fills at the price at that moment, which may
have moved a little since.

What every order costs:

- the **spread**, between half and three quarters of a percent;
- a **commission** of 0.4% of the order, at least 25 credits;
- for big orders, the push on the price.

So buying and selling straight back always loses money. Orders are for cash only: no
credit, no selling shares you do not hold.

Limits, which the panel and F3 explain when you reach them:

- one order is at most a quarter of a day's trading in that company;
- you may hold at most 250,000 credits' worth of any one company.

Every trade goes in the game's own ledger, so the Finances window shows it, and your cash
changes at once.

## Price alerts

On a company's page, **Alert at +10%** and **Alert at -10%** set an alert ten percent above
or below the price now; **Clear alerts** removes them. F3 sets any level you like. When the
price reaches a level, you get a crew log line and, with the navigation map open, the
banner. Each alert goes off once and clears itself. Alerts are checked at every minute of
game time, also through a time skip, and the ones that went off during a skip are listed
together afterwards.

## While you are away

Time skips and fast play are fine. Up to three game days, the market moves minute by minute
through them, so the same moment has the same price however you got there. A longer jump is
crossed in exact steps of days, weeks or months (at a fixed cost, however many years pass),
so the path differs from watching it all, but the market behaves the same. After a skip, the wire lists
the biggest moves and any alerts. After anything longer than three game days, such as years
passing at once, you get one summary instead: how long you were away, the biggest movers
and what your shares are worth now against before.

## Your shares

**Your shares** lists each holding with what it is worth at the selling price, what it cost
you and the gain or loss. **Sell everything** sells every holding in orders no larger than
allowed: the first press says what it will do, the second does it.

From Phobos Banking 0.7.0, if you also run it, the Credit panel's overview shows what your shares are
worth, with a button into the exchange. Neither mod needs the other.

## The listed companies

| Ticker | Company | Sector | Follows |
| --- | --- | --- | --- |
| SMLK | Smartlink | Heavy industry | Weapons at Port Yangshan and Cassini Spaceport |
| TSTD | Testudo | Heavy industry | Hull at Cassini Spaceport and Cloudbreak; metal at Port Yangshan (an input) |
| AYO | Ayotimiwa Corp. | Heavy industry | Hull and ore at K-Leg |
| GEC | The Green Energy Company | Food and comforts | Intoxicants at Zhonghuamen Terminal and Port Shajiang |
| BVM | Brightvein Mining | Mining and volatiles | Ore at Zhonghuamen Terminal; metal at Port Yangshan |
| CWV | Coldwell Volatiles | Mining and volatiles | Volatiles at Port Yangshan and Upsilon Docking; helium-3 at Venus Orbital |
| VERD | Verdemorrow | Food and comforts | Food at Zhonghuamen Terminal and Qincheng Station |
| HALE | Halewright | Medicine | Medical supplies at Zhonghuamen Terminal, Qiantangmen and Cassini Spaceport |

Smartlink, Testudo, Ayotimiwa and the Green Energy Company are the game's own companies; the
exchange reports on them as a wire service would and never speaks for them. Brightvein
Mining and Coldwell Volatiles are new; Verdemorrow and Halewright are the makers of Phobos
Agriculture's and Phobos Medical's equipment. The company list is a data file you can
change or add to (see [Editing data files](editing-data-files.md)).

## The F3 console

Everything on the panel is also on the F3 console through `phobosexchange`.

| Command | What it does |
| --- | --- |
| `phobosexchange quotes` | Every company: price, change over a day and a week, shares held. |
| `phobosexchange quote SMLK` | One company in full: selling and buying price, your holding, alerts and profile. |
| `phobosexchange buy SMLK 100` | Buys 100 shares, as the panel's Buy button does. |
| `phobosexchange sell SMLK 100` or `sell SMLK all` | Sells shares. |
| `phobosexchange holdings` | Your shares, their value, cost and the gains already taken. |
| `phobosexchange alert SMLK above 200` or `below 150` | Sets an alert at any level; `alert SMLK clear` removes both. |
| `phobosexchange alerts` | Every alert you have set. |
| `phobosexchange sellall` | Sells everything; repeat it with `confirm` at the end to go ahead. |
| `phobosexchange open` | Opens the panel. |
| `phobosexchange history` or `history SMLK` | Read only: the exchange's history, or a company's founding, listing and history. |
| `phobosexchange drivers` or `drivers SMLK` | Read only: each station signal a company follows, the game's price factor there now, and where the signal stands. |
| `phobosexchange state` or `state SMLK` | Read only: the parts of each price and the pace of its phase. |

Two **test commands** change your save outside what the exchange was built to do, so they
are locked until you type the game's own `unlockdebug`, warn you every time, and need
`confirm` at the end:

| Command | What it does |
| --- | --- |
| `phobosexchange test shock SMLK 20 confirm` | Jumps a company's price by a percentage, to try alerts and the wire. |
| `phobosexchange test reset confirm` | Starts the exchange's prices, history and alerts afresh; your shares stay. |

A save a test command has touched is marked as test-changed, and the log says so, so a
bug report shows it. Keep a copy of the save first if you might want to go back.

## Saves and removal

- The exchange keeps one record on your character: the market's state, your shares and
  alerts, and the chart history: about 14 KB with the shipped companies at first, and never
  more than about 22 KB however long you play, because the point a month it keeps from
  0.3.0 is thinned to at most 240 a company. The history from before your first day is
  never saved; it is drawn again at each load. Nothing else in the save changes, apart from your cash and the ledger lines your
  trades write.
- Reloading a save never rerolls prices: the same moment always has the same price. That
  also means you could play ahead and reload to peek; that's yours to resist.
- **Before removing Phobos Exchange, sell everything.** Shares are only worth credits while
  the exchange is there to buy them back. If you remove it with shares held, the record
  stays on your character untouched, and the shares come back if you reinstall it.
- A record from a newer version of the exchange is left untouched, and trading stays closed
  until you update.

## Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.130.0 or newer. Install it with
`./scripts/install-mods.ps1 -Mods Exchange` (see [Installing and updating our mods](installing-mods.md)).

## Where the ideas come from

The market's behaviour follows what finance research finds in real prices: fat tails and
calm and stormy stretches (Rama Cont, Quantitative Finance, 2001), sudden jumps (Robert C.
Merton, 1976) and an order's push growing with the square root of its size (Tóth et al.,
Physical Review X, 2011). The figures themselves are invented for the game, tuned so a
trend shows over weeks of play; they are not measured from any real market. The design
record, with links to every source, is
[A share market for Phobos Banking, and Framework charts](development/share-market-and-charts-design.md).
