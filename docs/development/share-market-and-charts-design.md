# A share market for Phobos Banking, and Framework charts: research and design

Research record, 7 October 2026. The owner asked whether a third-party stock-market library
could give Phobos Banking a believable share market without writing one from scratch, and
for a charting library that works in the game, for the market and for other uses. This
record collects what was checked, what the game already offers, a proposed design, the
pitfalls found so far and the decisions left to the owner.

**A planning round the same day settled the decisions and revised the design.** The market
becomes its own mod, Phobos Exchange. See
[Planning round: owner decisions and the first build](#planning-round-owner-decisions-and-the-first-build-7-october-2026);
where it differs from the original proposal below, the planning round wins, and the
superseded sections say so.

Labels used below: **Observed** is seen in the installed game's data files or in the local
decompile used by the [banking research](pda-apps-and-banking-research.md) (kept in ignored
`.local`, never redistributed); **Read from source** is a third-party library's published
code, read but not run; **Agent proposal** is Claude's design for the owner to revise;
**Unverified** is stated as such. No part of the proposed market has been tested in play.

## Owner direction (7 October 2026)

- Add share-market features to Phobos Banking. A realistic market is wanted, which is why
  a library was asked about first.
- Give the person at the keyboard more to do, especially on long-haul trading routes, where
  the crew mostly runs itself.
- Make the world feel believable and alive, leaning on the setting's cyberpunk ubercorps.
- A charting library (or control) is needed for the market and will see other uses.

## Summary of recommendations

1. **No third-party market library fits** (see [What was checked](#third-party-libraries-what-was-checked)).
   The candidates price real financial instruments, connect to real exchanges, run as
   servers, or are written in Python or C++. Write a small market model in Banking instead:
   a few well-studied pieces of finance research reproduce how real prices behave, at a cost
   of a few hundred lines.
2. **Drive the market from the game's own economy.** The game already runs a cargo market
   with supply, demand, AI haulers and blockades at every station (see
   [The game's own cargo market](#the-games-own-cargo-market-observed)). Companies whose
   share price follows those real signals make the world feel alive in a way no library
   could, and they reward a player who travels and looks.
3. **Build a chart control in Framework** rather than adopting XCharts. Framework already
   draws custom lines on the game's UI (`Controls/PickerGraphic.cs`); XCharts cannot load its
   settings inside a mod without a fork (see [Charts](#charts)).
4. **Treat the market as a business with a house edge, not a money tap.** The project's
   economy rules forbid loops that create value from nothing; a market whose shares simply
   rise over time is one. See [Economy rules](#economy-rules-and-the-house-edge).
   *Superseded in part:* the owner chose a real-world upward drift under a guard (below).

## Planning round: owner decisions and the first build (7 October 2026)

The owner asked for every idea, suggestion and disagreement before a first build, answered
the open decisions and corrected two of the agent's assumptions. Owner choices are marked
**owner**; the rest are agent choices for the owner to revise.

### Owner decisions

- **A separate mod, Phobos Exchange** (owner). Its own PDA app, save record, Workshop page
  and version; it needs only Framework. When Phobos Banking is also installed, the Banking
  overview shows the player's holdings; neither mod requires the other.
- **A mix of companies** (owner): the game's ubercorps, a few invented ones and our own
  makers. News about the game's companies is a wire report, never their own statements.
- **Visible trend phases with a real-world upward drift** (owner). Prices rise and fall in
  phases lasting weeks, plainly visible on a chart; rises outlast falls because the long-run
  drift is upward. This is the owner's one exception to the "no gaining loop" rule, held by
  a guard: the expected yearly return stays at or below half the cheapest Banking loan's
  yearly cost (Halcyon Bond, 0.02% a shift, 28.8% a game year), so borrowing to buy and hold
  does not pay on average. A player who reads a phase right does profit.
- **The game moves in weeks** (owner). Weeks are the normal pacing, so the market is tuned to
  be watched over weeks, not hours. Years can pass in an instant (an arrest, plot or
  character advancement): the market must handle that correctly, quickly and at a bounded
  cost. Earlier work had trouble with exactly this kind of time jump.
- **Test commands need the game's `unlockdebug`** (owner, every Phobos mod): any F3 test
  command that would damage saved data, or change how play goes on if the player carries on,
  works only after the game's own debug switch, and even then warns plainly that the save
  then lies outside what the mod was built for. Read-only readouts and ordinary player
  commands stay open. Framework gains a shared gate (`DebugCommands`); a later round audits
  the existing mods' commands.
- **Reloading to peek is accepted** (owner): stable noise means a reload never rerolls prices,
  but a player can play ahead and reload; that cannot be avoided without changing the
  foundation.
- **First build** (owner): market, chart, alerts and the debug gate. Not yet: standing orders,
  dividends, the docked-station edge, TV wire news, short selling, trading on credit.

### Corrections to the original proposal

- **Hauls are days, play is weeks.** The original "two-month haul" was wrong. At speed 1 a
  game second is a real second (the game allows 0.25x to 16x, and skips of up to 24 hours);
  0.7 AU at 1 g takes about 57 hours, so a haul is 2 to 5 game days within weeks of play.
- **No GARCH(1,1).** Its parameters depend on the step size. The market must give the same
  price at a given moment whether the player watched at 1x or skipped a day, so every random
  process is an Ornstein–Uhlenbeck process stepped by its exact transition on a fixed grid of
  60 game seconds. Volatility clustering (one of Cont's stylized facts) comes from a slowly
  wandering log-volatility instead.
- **Readable over realistic.** Day-to-day noise is calm (about 1 to 2% a game day); week-scale
  trend phases carry the story; every big move and every turn of a phase is explained in the
  crew log with its largest cause.
- **The native market moves only when a station's stock moves.** Its price factor at a station
  is fixed demand scaled by how full that station's store is. The trend phases therefore carry
  the base movement, native drivers add to it, a driver whose station neither makes nor uses
  its category is rejected by the native checks, and a read-only F3 readout shows the driver
  values for a playtest.

### The model (agent design)

Every random term is an Ornstein–Uhlenbeck process X stepped exactly:
X ← aX + s·√(1−a²)·z, with a = 2^(−Δ/half-life) and stationary spread s. The step index is
n = floor(game time / 60 s); z comes from a stable hash of the save, the company, the step
and a stream number, so it is never rolled.

- **Trend phases (as built).** The plan was an OU trend rate whose time integral the price
  carries. Tuning showed its integral's variance grows without bound: about 0.6 in a game
  year, far over the return guard, and it would make typical prices fall. As built, each
  phase (the market's, each sector's and each company's) is the difference of a slow and a
  fast OU process driven by the same noise. The noise cancels over short steps, so the level
  moves smoothly with momentum (a rise carries on, then turns), and its variance stays
  bounded however long a save runs. Both processes step exactly together (a 2 x 2 Gaussian
  transition), so one draw still covers a minute, a day or fifty years; the short-step
  covariance uses series expansions so a one-minute step never loses precision. Shipped
  half-lives: market 10 and 2.5 weeks, sectors 8 or 9 and 2 weeks, companies 6 and 1.5
  weeks; phase sizes 0.06 to 0.15 of the log price.
- **Noise.** About 0.7 to 1.1% a game day as shipped (calmer than planned, so phases show over weeks), scaled by a log-volatility that wanders with a 3-day
  half-life (calm and stormy stretches), with slow reversion over years.
- **Jumps.** Rare, decided by hash.
- **Drivers.** The native price factors at named stations for named categories, read every game
  hour, smoothed over a day, each term clamped.
- **The player's own trading.** Square-root price impact (Tóth et al., 2011), fading with a
  one-day half-life.
- **Price.** ln P = ln B + μ·t + β·T(market) + γ·T(sector) + T(company) + D + w + I. Every term
  adds in log space, so a move is attributed to its largest change.
- **Drift guard.** The expected yearly return combines the drift, half the one-year variance of
  every term and the jump term; the pack validator refuses more than 0.14, and a test checks
  that this cap is at most half the cheapest yearly loan cost in Banking's shipped lenders file.
- **Economy simulation.** A test runs borrow-and-hold, buy-the-dip and follow-the-trend players
  over several seeds and game years: borrow-and-hold must lose on average; trend-following
  profit per game month is reported as a balance figure for the owner.

### Time jumps of any size (agent design, owner requirement)

The gap from the last step to now is split into segments, each stepped exactly: one jump to
two years before now when the gap is longer than that, weekly steps from two years to 120
days, daily steps from 120 days to 3 days, and 60-second steps for the last 3 days. At most
about 4,545 steps a company, whether the gap is ten minutes or fifty years, and the same save
always gives the same result. Coarse steps are exact for levels, phases and drift; noise over
a coarse step uses the mean volatility (a labelled approximation), and jumps use a hashed
count. The step loop allocates nothing, writes no ledger line, posts no notice and builds no
text; alerts crossed during a catch-up become one summary, and a gap of more than 3 days ends
with one "while you were away" summary. The clock never steps backwards.

### Save record (agent design)

`PhobosState.PhobosExchange` through Framework's object state store, version 1: the seed and
clock, trend states, each company's model state, holdings, alerts, a test-change marker, and
history as 73 hourly, 120 daily and 104 weekly closes per company, stored compactly relative to
each chunk's base. About 1.5 KB a company and fixed caps, so the record never grows without
bound. Fields this version does not understand are kept exactly as they were. (0.3.0 adds
at most 240 monthly lifetime points a company; see Company histories below.)

### As built (Phobos Exchange 0.1.0 and Framework 0.128.0, 7 October 2026, held draft)

- **Measured offline** (.NET 10 on the owner's PC; the game's Mono is slower, and the in-game
  operation `exchange.catch_up` records the real cost): a one-day skip about 3 ms for three
  companies; three days or more 8 to 12 ms; fifty years at once 4,589 steps, within the fixed
  bound of 4,610. Minute-by-minute stepping, one catch-up and any chunking give bit-identical
  prices, also across a save and load.
- **The guard as shipped:** expected yearly returns from 9.2% (Verdemorrow) to 12.7%
  (Brightvein Mining), all under 14%. Over ten seeds and three game years, borrowing at the
  cheapest Banking rate (29.2% a game year) to buy and hold lost about 71% after interest.
  Following the hidden phase earned about 2.3% a month per company before commission;
  following what the weekly chart shows (the price above where it stood a week ago) about
  1.3%. That is the reward for reading the board, and it stays below the cost of a loan.
- **The record** for three companies after 400 game days: 29 fields, about 4.6 KB; eight
  companies come to about 12 KB, fixed.
- **Every driver is live:** each of the 19 shipped drivers names a station whose cargo
  market prices the category and makes or uses it (checked against the installed game's
  market data by the native checks).
- **Not yet seen in play.** Owner checks: the panel and chart at 16x, a 24-hour skip with
  alerts, a save and reload, a trade against the Finances ledger, `phobosexchange drivers`
  at several stations, the test commands before and after `unlockdebug`, and a long time
  jump for the away summary.

### Stories as data (Phobos Exchange 0.2.0 and Framework 0.129.0, 7 October 2026, held draft)

Owner direction, 7 October 2026: creative and world content is schema-checked data that
ChatGPT writes and players add to; Claude builds the logic, schemas, validators and hooks,
with a handoff for writers (now a rule in `AGENTS.md`). For the exchange:

- **Story into price.** A company's `news` entries in the `exchange` pack name a story flag,
  a one-off move (a share of the price, at most 30% either way) and an optional wire line.
  When the flag is set, by an arc's `setFlags` or another mod, the price moves once and stays
  moved: news is its own part of the log price (`Cause.News`), so the wire and later reports
  name it, and the day's move report is held back so the news is not reported twice. Applied
  news is saved under its own key (`news.<company>`), so a 0.1.0 record reads unchanged and an
  older version keeps it untouched. One-off news is authored and bounded and is not part of
  the return guard, which covers what a holder can expect from the market itself (agent
  choice).
- **Price into story.** The exchange sets flags and starts arcs named
  `exchange-<company>-<event>` for `surge` and `slump` (each day's reported big move),
  `bought`, `major-holder` (half the holding cap) and `sold-out`, as Banking does for loans.
- **A story pack** registered by the exchange, holding one place-less thread for the exchange
  and one per company as the writers' canvas; the content is ChatGPT's, from
  [the handoff](exchange-stories-handoff.md).
- **Players' own companies with their own stories.** Add-ons could add a company but not the
  arcs for its events, because those ids start with `exchange-`, not the add-on's prefix.
  Framework 0.129.0 adds event namespaces: a mod registers `exchange`, and an add-on with
  prefix `p` may then add ids starting `exchange-p`. The worked example
  `examples/addons/PhobosExampleKeelhaulListing` lists Keelhaul Freight, tells a story whose
  flag lifts its price 8% and answers its `bought` event with a letter; the C# and Python
  checks load it.

### The first fix in play (Phobos Exchange 0.2.1, 7 October 2026)

The owner's `Player-prev.log` showed 0.1.0 failing on every attach and then every poll (a
NullReferenceException in `MarketModel.SetDriverFactor`): the service read the drivers
before the model had started, so prices never moved in play. `MarketModel.Open` now starts
the market and then reads the drivers, and the service keeps a model only once it opened.
A new market also settles its drivers at their first readings, moving the base the other
way, so it opens at the pack's prices without a first-day drift. A trade moves the market
before it touches shares or cash, and the chart's fill moved into the game-free
`HistoryView` (the 120-day chart had drawn only its oldest 105 closes).

### Company histories (Phobos Exchange 0.3.0 and Framework 0.130.0, 7 October 2026, held draft)

**Owner decisions** (7 October 2026): price history and market data must exist from before
the day a player installs the exchange, reaching back as far as each company's age, which
the `exchange` pack decides within a fixed range; the writers' handoff covers it.
- The range is 200 years: founding, listing and history years from 1879 to 2078 (a new game
  always starts in 2079, the game's `NewGame` epoch, checked natively).
- Two years per company: `founded` (its age, for the story) and `listed` (where its chart
  starts, defaulting to `founded`), with an optional exchange `opened` year no listing
  predates.
- Authored history in this round: dated entries on the exchange, each sector and each
  company, and an optional listing price.
- The history before a save's own is redrawn from the pack and the save's seed at every
  load, never stored, so later lore reaches saves already started; what the save played is
  stored and never changes.

**Agent choices, for the owner to revise:**
- Entries that move a price end in 2076, and listing prices need a listing by 2076: the two
  years before a new game are the backfill the save stores, so a move dated there could not
  show without changing stored data.
- An entry without a move is lore only and may date from the founding.
- A month left out is picked by a stable FNV hash of the entry, the same for every player.
- An authored listing price must imply a yearly growth, net of the entries' moves, from
  -0.05 to 0.25, so a short history is never a cliff.
- The former placeholder founding, listing and opening years have been replaced by first-draft authored values. They remain proposals for owner review (see [the handoff](exchange-stories-handoff.md)).

```mermaid
flowchart LR
    A["Listing year<br/>(pack: listed or founded)"] -->|"drawn at each load:<br/>PastPrices, monthly"| B["The save's anchor<br/>(lifetime point 0)"]
    B -->|"stored: backfill<br/>two years"| C["First day with<br/>the exchange"]
    C -->|"stored: played,<br/>a point a month"| D["Now"]
```

**How it works.**
- **The save's own lifetime points** (`LifetimeSeries`, keys `hist.m.<company>.<part>`): the
  price at the start of each calendar month, from the anchor where the save's own history
  begins (the backfill's first step for a new save, the first weekly close for a 0.1 or 0.2
  record, the listing moment for a company added mid-save). At most 240 points; when full
  they thin to every 2, 4, 8 … months on the calendar, always keeping the anchor and the
  newest. A series this version cannot read is kept exactly and never sampled or rewritten.
  0.2.x keeps the new keys untouched, because its history key rule reads only h, d and w.
- **Long jumps** of more than two years are crossed in month-aligned steps every 1, 2, 4 …
  months (at most 240), feeding only the lifetime points; the weekly, daily and minute
  steps after them refill the rest. The bound is now 4,850 steps for any gap (4,777 for a
  thousand years). Such a jump gives different, equally exact prices than 0.2 did, and an
  alert can fire partway through it. A correction to the record above: prices are
  bit-identical whatever the chunking only within the last three days (minute steps);
  coarse steps depend on where the segments fall, and a given gap always gives the same
  result.
- **The drawn past** (`PastPrices`, game-free): the market's and each sector's phases run
  monthly from a fixed origin (January 1879) with stationary starts and stream offsets of
  their own, so they are the same whichever companies are present; each company's own phase
  and noise start at its listing. The price at month t is the anchor's, moved by the phases
  and noise since t, by the entries' moves between t and the anchor (the exchange's times
  `followsMarket`, the sector's times `followsSector`), and by the drift run backwards. An
  authored listing price adds a straight-line correction that meets it at the listing and
  vanishes at the anchor. The ordinary process statistics are unchanged by a straight line,
  so it only re-solves the long-run growth from the two fixed ends. Each path takes at most
  4,800 steps; a span past 2279 would step every two months.
- **Views** (`HistoryView`): each range draws its finest source first and fills in from
  coarser ones only before the finer one begins (hourly, daily, weekly, lifetime points,
  then the drawn past), clipped at the pack's listing month, so a company added mid-save
  shows its drawn past on the 2-year chart. **All** uses Framework 0.130.0's log scale, the
  calendar years along the bottom, unclamped prices and marks at the entries that moved the
  price.
- **Known limit:** a company added to a running save gets a drawn past for the years since
  2079 too, which does not follow the market's own record of those years.

**Measured offline** (.NET 10 on the owner's PC): drawing the past for all eight shipped
companies takes about 4.5 ms (3,056 points), once per load at the first chart that needs
it, measured in play as `exchange.past`. The shipped record is about 14 KB at first and
about 21 KB after fifty years, where the lifetime points reach their cap. Not yet seen in
play.

### First authored content draft (Phobos Exchange 0.3.0, 7 October 2026, held draft)

The content pass fills `exchange.json` with 41 company-history entries, 16 sector entries and five market entries. Every company now has three to six history lines, a founding and listing year, and a listing price calibrated to about 3.5% yearly growth after its dated moves. The 2034 opening, unsupported founding and listing years, listing prices, invented-company histories and all fictional market anecdotes are author choices for owner review. The story pack contains two price-moving news arcs per company, four event responses per company, eight named company correspondents, an Exchange desk correspondent, five adverts, sixteen local surge/slump lines and an encyclopedia article. The Keelhaul add-on now demonstrates the same extension surfaces.

**Canon boundary and sources.** Dated setting anchors were checked against Blue Bottle Games' installed Ostranauts 1.0.1.5 primary data (`Ostranauts_Data/StreamingAssets/data/tips/tips.json`, `headlines`, and the relevant company and ship definitions, retained locally and not redistributed). Blue Bottle Games' [Ostranauts](https://store.steampowered.com/app/1022980/Ostranauts/) is the game source; Joshu's [Official Ostranauts Modding Guide, 22 June 2026](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946) documents the modding context. This version uses only the dated anchors listed in [the story handoff](exchange-stories-handoff.md): the Kronos missions reaching Titan, Ceres Resource Extraction's closure, Ayotimiwa's K-Leg work, the Green Energy Company name change, Testudo's Mesa production, the Ganymede Coup, the Kessler collapse and the new-game year. Smartlink's founding year and every other unestablished date or incident are fiction written for this draft. When fictional histories meet a canon date, only the date is anchored: the company-specific causes, market reactions and price moves are authored story choices, not claims made by the game. Neither source is said to endorse or validate this mod.

**Offline check.** `python scripts/validate-data-packs.py` accepts all 29 shipped packs, including both Phobos Exchange packs. The separate merged add-on check also accepts the Keelhaul example. These are schema and cross-pack checks only; no gameplay check has been done.

### Recurring news (Phobos Exchange 0.4.0 and Framework 0.131.0, 8 October 2026, held draft)

**Owner direction** (8 October 2026): news pieces have marked effects over time, repeatedly, as
the game's own headlines recur. Owner choices: the effect is a jump that a trend phase carries
on (not a fixed fade, not a build-up), and news recurs weekly.

**Why it needed Framework.** A story flag kept the time it was first set, so an arc running
again changed nothing, and TV news was shown once a save. Framework 0.131.0 adds:
- an arc that sets a flag already set renews its time (`StoryFlags.Renew` from code);
- `cooldownDays` on a repeatable arc;
- `onceEach` news and adverts, shown once each time a required flag is set again;
- a news item's seen time is now its latest showing, so talk follows the latest.

**How a piece of news plays out** (`MarketModel.BreakNews`, `NewsPart`):
- **The jump.** The price jumps by `move` at once. No build-up: an announced rise to come
  would be free money.
- **The lasting share.** The first time a piece breaks in a save, its `keeps` share joins the
  lasting news part.
- **The unwind.** The rest, and the whole jump every later time, joins one unwinding part per
  company. It fades at the company's `newsFadeDays` half-life, exactly for any step (closed
  form), and is saved as two numbers (`newsfx.<company>`), so weekly news over years costs a
  fixed few bytes.
- **The carry.** `carry` pushes the company's own trend phase: the same amount added to its
  slow and fast processes leaves the level where it is and sets it moving. On average the
  phase rises to `carry` after the kernel's peak time (about four weeks for the shipped
  phases) and fades, with the phase's own noise, so it is usually but not always a sure
  thing.
- **Detecting each breaking.** The service keeps a watermark per company: a news flag whose
  set time is later breaks again. A 0.2 or 0.3 record, or a company new to the save, starts
  its watermark at the latest of its news flags already set, so old news never breaks twice.

**What stops this becoming a gaining loop.**
- Repeats keep nothing for good, so the lasting part is bounded by the news entries' first
  breakings.
- The unwind and the carry both fade to nothing.
- The validator holds a jump's first-week unwind (the part a player can count on after bad
  news) to 0.02 in log units. Agent default, with `|carry|` capped at 0.15.
- The return guard is unchanged: it covers the market, and news is authored and bounded.

**Shipped wiring** (agent choices): each shipped news item carries half its move and keeps
0.3 of its first jump; the news arcs repeat with a 7-day cooldown, and the news, surge and
slump TV items are `onceEach`. The wire lines are ChatGPT's as written; they now repeat,
and new lines written to vary them would serve recurring news better.

## What we can build on

### Our technical constraints (observed)

| Fact | Where seen | Consequence |
| --- | --- | --- |
| The game runs Unity 6 (UnityPlayer 6000.3.23) on Mono | `UnityPlayer.dll` file version | Libraries must load on Unity's Mono, not modern .NET |
| Our plugins target `netstandard2.1` | every mod `.csproj` | A library must offer netstandard2.0 or 2.1 builds, or be source we compile |
| The game ships `UnityEngine.UI`, TextMeshPro and `Newtonsoft.Json` | `Ostranauts_Data/Managed` | uGUI drawing and TMP text are available to us at no cost |
| This repository's own work is MIT licensed | `LICENSE` | MIT, BSD and Apache code is usable with a notice in `THIRD_PARTY_NOTICES.md`; GPL or LGPL code is not; Unity Asset Store packages cannot be redistributed in a public repository or Workshop item |

### The game's own cargo market (observed)

Blue Bottle Games' Ostranauts already keeps a living market behind its cargo kiosks. The
[solar-system economy record](../solar-system-economy.md) documents the pricing side; what
matters for a share market is:

- **Every station market is a `ShipMarket`** owned by `MarketManager`. Data lives in
  `data/market`: 19 named market profiles (`Markets/market_actor_configs.json`), production
  maps that turn inputs into outputs or consume goods per hour
  (`Production/production_maps.json`, for example Ceres's volatiles-to-water converter), and
  categories of goods (`CoCollections`, such as `AnyOres`, `AnyFood`, `AnyWeapons`).
- **Prices move with supply and demand.** `ShipMarket.CalculatePriceModifier` sums each
  category's net hourly demand from the production maps, clamps it to -0.8..+0.8
  (-0.8..+1.7 under a blockade, via the station's `DiscountBlockade` condition) and scales it
  by how full or empty the station's virtual inventory is. The result is a per-category
  price factor of about 0.2 to 1.8.
- **It runs on its own.** `MarketManager.UpdateMarket` runs production every 10 seconds of
  game time (`StarSystem.fEpoch`); AI cargo haulers are registered on trade routes between
  surplus and demand (`RegisterAICargoHauler`, `FindSupplyDemandMarketPairs`, which picks
  routes with `UnityEngine.Random`). Player sales and purchases are reported to it
  (`ReportTransaction`).
- **It is saved** (`MarketManager.GetJSONSave`, `JsonMarketSave`), so its state carries over
  between sessions without any work from us.
- **It can be read.** `MarketManager.GetSystemMarket()` returns every station's category
  price factors; `GetStationMarket(regId)` and `GetStatusForShip(regId)` return a station's
  report and a text description of what it produces and how full its stores are.

Not yet verified: whether production catches up correctly across a long native time jump
(`MarketActorConfig.RunProduction` is not in the local decompile), and which of the game's
own screens shows `GetStationMarket`.

**What this means:** the game already produces the signals a share price needs: a
shortage of ore at Mars, a blockade at a station, a glut of intoxicants. A mining company
whose price follows ore demand at its home stations is believable because it reacts to
things the player can see and even cause.

### The game's corporations (observed in the game's text)

The game's own writing calls Smartlink an "ubercorp". Companies named in its data, with
where each was seen:

| Company | Seen in | What it does, as written |
| --- | --- | --- |
| Smartlink | point-defence item descriptions | Weapons maker; "the ubercorp's rivals" dispute its claims |
| Ayotimiwa Corp. (Ayotimiwa Corporation) | K-Leg kiosk and refuel station descriptions | Runs K-Leg's shipbreaking, kiosks and refuelling with the OKLG Port Authority |
| Testudo | ship registry (24 entries), hardsuits, flooring, furniture | Shipbuilder (Dream, Rouncy, Mesa, Bulk Lifter) and fittings maker |
| Ryokka | ship registry (16 entries) | Shipbuilder |
| Mobile Space Systems | ship registry (8 entries) | Shipbuilder |
| Renske International | ship registry (4 entries) | Shipbuilder |
| The Green Energy Company | intoxicant descriptions, `IsGEC` brand condition | Intoxicants maker based in the Rosebud on Tharsis Landing |
| Brave New World, LLC | meat-product packaging | Food maker |
| Huoxing Mining Company, Troy Jupiter Group | game text, context not yet read | Unknown |
| Penumbra Group | an advert | Contract killers; probably not something to list |
| Ogiso's Register, Ogiso's Bank | ship registry and starting mortgages | Already referred to by Banking, never spoken for |

The banking research set the rule that we **refer to the game's institutions but never
speak for them**. Listing their shares and reporting news about them in a wire-service
voice is referring; writing their press releases would be speaking for them. Which of them
may be listed is an owner decision (below).

The game's own TV headlines (`data/headlines`, 43 static regional stories) and adverts are
unsaved flavour text; Framework's story system already mixes our news into that feed.

### Our own systems to reuse

- **Banking.** Money is the player's `StatUSD`; the ledger records transactions
  (`Ledger.RecordTransaction` writes a line but moves no money, so callers change `StatUSD`
  themselves). Banking saves its book as one record on the player (`PhobosState.PhobosBank`),
  hosts a full panel from its CREDIT PDA app (Framework `PdaApps`), and already fires story
  events and flags. Orrery Credit (0.6.0, held draft) set the precedent of a service usable
  from anywhere at a higher price.
- **Story system.** News and adverts share the TV feed, bulletins, places, people, threads,
  flags (`StoryFlags`), where the player is (`StoryLocation.DockedPlace`, `Near`) and the
  game's own day length (`GameClock`, 87,658.125 seconds). See the
  [story system design](story-system-design.md).
- **Time.** Time-skips step running work (`CrewSkip.Advance`) rather than handing it hours at
  once; `Cadence.RealTime` counts skipped seconds.
- **Data packs** for authored tables (player and add-on overridable), **stable-hash
  outcomes** (`Outcomes.Pick`: never rolled at runtime or rerolled), the **economy audit**
  (`scripts/audit-economy.py`) and **performance footprints** (`Performance.RegisterFootprint`).

## Third-party libraries: what was checked

### Market simulation

| Candidate | Licence | Verdict |
| --- | --- | --- |
| [QLNet](https://github.com/amaggiulli/qlnet), C# port of [QuantLib](https://www.quantlib.org) | BSD | Prices bonds, options and other real instruments from given inputs. It does not make a market move. |
| QuantLib with its C# bindings | BSD-style | Native C++ library behind a generated wrapper; a native DLL in a mod, for the same unsuitable job. |
| [StockSharp](https://feed.nuget.org/packages/StockSharp.Algo/5.0.231), QuantConnect Lean | not rechecked | Trading platforms for real exchanges; very large and built for modern .NET. Excluded on purpose and size, licences not rechecked. |
| [MemExchange](https://github.com/chironn/MemExchange) | MIT | A C# order-matching exchange, but it runs only as a server and client over NetMQ with Castle Windsor, Disruptor, Topshelf and others. |
| [ABIDES](https://arxiv.org/abs/1904.12066) (Byrd, Hybinette and Balch, 2019) | not checked | A high-fidelity agent-based market simulator for AI research, in Python. A reference, not a dependency. |
| [MAXE](https://arxiv.org/abs/2008.07871v2), [StockSim](https://www.emergentmind.com/topics/stocksim) | various | Agent-based order-book simulators in C++ or Python. |
| [Math.NET Numerics](https://numerics.mathdotnet.com/) | MIT | Usable (netstandard2.0) if we want proper probability distributions; not needed, since the model below needs only a uniform random source. |

**Conclusion.** A market in a game feels real when its prices behave as real prices do and
react to the world, not when it runs a real exchange. That is a small, well-researched
model, and only we can wire it to the game's economy and stories.

### Charts

| Candidate | Licence | Verdict |
| --- | --- | --- |
| [XCharts](https://github.com/XCharts-Team/XCharts) (XCharts-Team) | MIT | The only real candidate: uGUI, about 3,800 stars, last pushed 17 July 2026, line, bar, area, candlestick, heatmap and more. Blocked as shipped; see below. |
| ScottPlot, OxyPlot, LiveCharts2 | MIT | Draw through SkiaSharp (native libraries), System.Drawing or desktop UI frameworks. The game ships a `System.Drawing.dll`, but whether Unity's Mono can render with it was not checked, and a rendered bitmap would need copying into a texture on every change and would not match the game's look. OxyPlot's renderer is an interface we could implement on uGUI, but by then we have written most of a chart control. |

**XCharts pitfall (read from source).** In a built game, `XCSettings.Instance` is only
`Resources.Load<XCSettings>("XCSettings")` (`Runtime/Internal/XCSettings.cs`); every fallback
(default language, Arial, the TMP font) sits inside `#if UNITY_EDITOR`. Themes also come only
from `Resources.Load<Theme>` (`Runtime/Internal/XCThemeMgr.cs`). A BepInEx mod has no such
assets in the game's Resources, so `Instance` is null and the first font or theme lookup
fails. Using XCharts would mean forking it to build its settings and themes in code (or
setting its private static instance by reflection), compiling its runtime sources against
the game's `UnityEngine.UI` with `dUI_TextMeshPro` defined, and restyling it to the game's
palette. None of that has been tried.

## Design proposal: the exchange (agent proposal)

*The original proposal, kept as written. The [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026) moved the market into its own mod and revised the model, time handling and save format; where they differ, the planning round wins.*

### What the player does

The aim is play for the long haul, when the crew runs the ship and the player has time:

- **Watch and trade.** A dozen or so listed companies, each with a chart, a short profile,
  its home stations and the goods it depends on. Buy and sell from the PDA wherever the
  ship is, as Orrery Credit can be used anywhere.
- **Leave orders.** "Buy if it falls to X", "sell if it rises to Y" or "sell if it falls
  below Z" orders that fill while the player sleeps or skips time, and say so when they do.
- **Read the world.** Company news in the TV feed and the PDA, quarterly reports as data
  files, letters from a broker or a contact with a tip that may be wrong.
- **Be there.** Docked at a station, the player can read its market report: the shortage
  or glut that will move a company's price before the news reaches the wire. Travel and
  looking around become an edge, which suits long routes.
- **Collect dividends.** Some companies pay a share of profit each quarter, recorded in the
  ledger.

Later possibilities, not for the first version: short selling and buying on credit through
Banking loans, and contracts on the cargo categories the player hauls (a hauler fixing the
price of a load of volatiles before a long trip). The second ties the market to the game's
own trade directly, but settling it against the native price factor needs its own research.

### Listed companies: an `exchange` data pack

One Banking-owned schema, validated like the lenders pack, overridable by players and
add-ons, adding by key and never renaming a shipped id:

| Field | Meaning |
| --- | --- |
| `id`, `ticker`, `name` | Stable id, a short ticker for the screen, the company's name |
| `sector`, `profile` | What it does, for the company page |
| `home` | Story places (stations) where it trades and whose news concerns it |
| `drivers` | Native cargo categories and markets whose price factors move its fair value, each with a sign and weight (a miner rises with ore demand; a refiner falls when ore is dear) |
| `price`, `shares` | Starting price and shares in issue (for market size and the player's impact) |
| `volatility`, `reversion` | Calm or wild; how quickly price returns to fair value |
| `dividend` | Share of value paid each quarter, if any |
| `thread`, `events` | The story thread its news belongs to, and story flags that move it (below) |

Candidates: the game's companies the owner allows, invented ubercorps for gaps (names
through the [branding record](equipment-branding.md)'s collision search), and **our own
equipment makers** (Verdemorrow, Halewright and the rest), which exist in the fiction already
and would make the player's purchases feel part of a wider economy.

### The price model

*Superseded in part by the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026): hourly steps and GARCH(1,1) gave way to exact Ornstein–Uhlenbeck steps on a 60-second grid and trend phases lasting weeks. The research citations below still support the stylized facts the model reproduces.*

Each listed company takes one step per game hour (3,600 seconds of game time). The change
in the logarithm of its price over a step is the sum of four parts:

```text
r = κ · (ln F − ln P)          pull back towards fair value F
  + σ · ε                      noise: fat-tailed, volatility σ that clusters
  + J                          jumps: news and rare surprises
  − I                          the player's own trading, fading over time
σ² = ω + α · r²(previous) + β · σ²(previous)
```

What each part is for, and the research it rests on:

- **Fat tails.** Real price changes include far more large moves than a normal (bell-curve)
  distribution allows. Benoit Mandelbrot showed this in cotton prices
  ([Mandelbrot, "The Variation of Certain Speculative Prices", Journal of Business 36(4):394–419, October 1963](http://www.jstor.org/stable/2350970)),
  and Rama Cont's survey lists it among the "stylized facts" found across markets
  ([Cont, "Empirical properties of asset returns: stylized facts and statistical issues", Quantitative Finance 1(2):223–236, 2001, doi:10.1080/713665670](https://ideas.repec.org/a/taf/quantf/v1y2001i2p223-236.html)).
  Proposal: draw ε from a Student-t distribution with about four degrees of freedom
  (authored), made from uniform random numbers.
- **Volatility clustering.** Calm and turbulent stretches follow each other, also one of
  Cont's stylized facts. The σ² line is Tim Bollerslev's GARCH(1,1) model
  ([Bollerslev, "Generalized autoregressive conditional heteroskedasticity", Journal of Econometrics 31(3):307–327, 1986, doi:10.1016/0304-4076(86)90063-1](https://ideas.repec.org/a/eee/econom/v31y1986i3p307-327.html)).
  ω, α and β are authored per company with α + β below 1 so volatility settles back.
- **Jumps.** Robert C. Merton added sudden jumps to continuous price movement to describe
  discontinuous returns
  ([Merton, "Option pricing when underlying stock returns are discontinuous", Journal of Financial Economics 3:125–144, 1976; MIT working paper](https://dspace.mit.edu/handle/1721.1/1899)).
  Here jumps come mostly from story events (a scandal, a contract, a blockade), plus rare
  unexplained ones.
- **Fair value from the game.** F is the company's base value times a weighted product of
  its `drivers`: the native price factors at its home markets for the categories it buys and
  sells, read through `MarketManager.GetSystemMarket()` at each step. A blockade or shortage
  therefore moves F, and the price follows at a speed set by κ. This is our simplified,
  authored model, not a claim about how real firms are valued.
- **The player's impact.** Large orders move prices against the trader. Studies of real
  markets find impact grows roughly with the square root of order size
  ([Tóth, Lempérière, Deremble, de Lataillade, Kockelkoren and Bouchaud, "Anomalous price impact and the critical nature of liquidity in financial markets", Physical Review X 1:021006, 2011](https://doi.org/10.1103/PhysRevX.1.021006)).
  Proposal: impact proportional to the square root of the order's share of daily volume,
  fading over a few game days. It stops the player cornering a company and makes a quick
  round trip lose money.

An agent-based market, where simulated traders interact, produces these facts on its own
([Lux and Marchesi, "Scaling and criticality in a stochastic multi-agent model of a financial market", Nature 397:498–500, 1999](https://www.nature.com/articles/17290)),
but costs far more processing and tuning for little visible gain. Not recommended for a
first version.

All parameters are authored balance, not measured from any real market; the record and the
company pages must not imply otherwise.

### How the pieces connect

```mermaid
flowchart LR
    Native["Game's cargo market: demand, stock, blockades"] -->|"price factors each hour"| Fair["Fair value per company"]
    Story["Story packs: news, flags, letters"] -->|"jumps, rumours"| Model["Price step per game hour"]
    Fair --> Model
    Player["Player's orders"] -->|"impact, fills"| Model
    Model --> Book["Exchange section of the Banking record"]
    Book --> Panel["PDA exchange page and charts"]
    Book -->|"StatUSD and ledger lines"| Ledger["Game's money and ledger"]
    Model -->|"big moves"| News["TV news and notices"]
```

### Time and long hauls

*Superseded by the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026): hauls are 2 to 5 game days within weeks of play, not months, and any time jump is caught up at a bounded cost.*

- Steps are game hours. A time-skip steps the market hour by hour and checks standing orders
  at each step, as `CrewSkip` does for machines; it never applies a week at once.
- A native time jump (the clock moving without our skip) is caught up by stepping through the
  missed hours. A two-month haul is about 1,400 steps per company, a negligible amount of
  arithmetic, but it needs a measured cap and a recorded performance finding.
- Native production's own catch-up across a jump is unverified (above), so fair values read
  straight after a jump may lag the native market by one update.

### Information and place

- Quotes and trading everywhere through the PDA, with a broker's commission, following the
  Orrery Credit precedent. Neither instant interplanetary communication nor its absence has
  been established as canon; present this as a game abstraction.
- **The docked edge.** When docked, the exchange page shows the station's own market report
  (native `GetStatusForShip`) next to the companies it drives. Wire news of the same change
  reaches the TV feed a few game hours later (a bulletin delay). Seeing it first is the
  player's reward for being there.
- **Tips.** A contact's letter may claim a company will jump. Whether the tip is right is
  picked once by stable hash when the letter is written (the `Outcomes.Pick` rule), never
  rerolled.

### Story integration

- Company news goes in story packs, tied to the company's `thread` and `home` places.
- Shocks need no new Framework vocabulary at first: a company's `events` list names a story
  flag and a jump size, and Banking checks `StoryFlags.Has` at each step, applying each jump
  once. Arcs already set flags, so writers can move markets without code.
- Banking fires its own story events (for example on a large gain or loss, or an order
  filling), as it already does for loans.

### Screens and commands

- An Exchange page in the Banking panel: watch list, company page with chart (line and
  candlestick, one day, seven days and ninety days), holdings with gain or loss, open orders
  and recent news.
- Every action says what it did and every refusal why. An action that needs another step
  first (for example replacing an open order on the same company) follows the press-twice
  rule and is classified in `config/panel-override-audit.json`.
- F3 commands through the same service: quote, buy, sell, orders and cancel.
- Player language: explain share, dividend, standing order and spread in the shared
  glossary; keep the voice of a working spacer reading the markets, not a finance textbook.

## Determinism, saves and save reloading

*Superseded in part by the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026): the record is Phobos Exchange's own (`PhobosState.PhobosExchange`), not a section of Banking's, with hourly, daily and weekly closes; reloading to peek is accepted by the owner.*

**Saved structures (proposal).** A new `exchange` section in the existing Banking record
(`PhobosState.PhobosBank`): per-company state at the last step (price, volatility, fair
value, fading impact, last hour stepped), the player's holdings and open orders, jumps
already applied, and a compact price history for the charts. Nothing new enters the game's
own save except `StatUSD` changes and ordinary ledger lines. Unknown companies and fields
are kept as read, so an older Banking never destroys what a newer one wrote, and tests need
old-record fixtures as for the loan book.

**Pitfalls found so far:**

- **History size.** Fair value comes from the live native market, which is not recorded, so
  past prices cannot be regenerated from a seed; history has to be saved. Hourly history for
  every company over a long game is too large for a property on the player. Proposal: hourly
  for the last seven days, daily open, high, low and close for a year, packed compactly; to
  be measured before choosing.
- **Reload to reroll, or reload to peek.** If noise were rolled at runtime, a player could
  reload until prices go their way; the project rule forbids rerolls. If noise comes from a
  stable hash of the save's seed, company and hour, reloading does not change the noise, but
  a player can play ahead, note prices and reload to trade on them. Fundamentals still
  depend on live play, so the future is not fully fixed. Proposal: the stable hash, as the
  rules require; peeking needs deliberate effort in a single-player game. Owner decision.
- **Removing the mod.** Shares are stored value. Removing Banking strands them; an F3 or
  panel "sell everything" before removal would be the honest answer. An engineering item to
  report, not a reason to shrink the design.
- **The native market uses `UnityEngine.Random`** for routes, so the fair-value input is not
  reproducible between two runs of the same save. That is acceptable for play but means
  offline tests must feed the model recorded native factors, not the live game.

## Economy rules and the house edge

*Superseded in part by the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026): the owner chose a real-world upward drift, held to an expected yearly return at most half the cheapest Banking loan's yearly cost.*

- **No gaining loop.** Real shares rise over time on average; in the game that would be
  money from nothing for any player who buys and waits. Proposal: authored drift so that,
  after commission, spread and dividends, holding a broad spread of companies earns about
  nothing over a year. The player earns by information (being docked, reading news, good
  tips) and timing, and can lose by the same means.
- **Manipulation by cargo.** The player's own cargo sales move native price factors, and
  those move fair values. Dumping a hold of ore to sink a miner's price is very cyberpunk and
  should be allowed, but must not pay: the cargo's lost value and the trading impact have to
  exceed the share gain. This needs a simulated test.
- **An audit.** Extend `scripts/audit-economy.py` (or a test beside it) to simulate a
  buy-and-hold player, a random trader and a cargo-dumping manipulator over several game
  years with recorded native factors, and fail if any beats an authored threshold.
- **Settlement.** Buying and selling change `StatUSD` and write ledger lines through
  `Ledger.RecordTransaction`; dividends are ledger lines. Payment of debts stays in the
  game's Finances window.

## Charts: a Framework control (agent proposal)

A shared chart control in Framework, since many mods would use it: loan balances over time,
share prices, crop growth, tank levels, power draw, Auto Nav range and speed, and in-game
views of performance captures.

- **Rendering.** A `MaskableGraphic` that builds its mesh in `OnPopulateMesh`, as
  `PickerGraphic` does: line, filled area, bar, step and candlestick series, gridlines and a
  cursor line. Axis labels and the hover readout are ordinary live panel text (TMP), not
  drawn into the mesh, so they follow localisation and the panel's font.
- **Redraw only on change.** `PickerGraphic` animates every frame; a chart marks itself dirty
  only when its data or size changes.
- **Bound the mesh.** Thin a series to at most one minimum and maximum per pixel column
  before drawing, so long histories stay fast and keep well under the per-graphic vertex limit
  of Unity's UI meshes (16-bit indices).
- **Game-free core.** Axis ranges, 1-2-5 tick steps, thinning and value formatting live in a
  class with no game types, tested in the ordinary test projects (Chainloader-gated code
  cannot run in the native tests).
- **Look.** The game's palette (`ConsoleWidgets.Amber`, `Slate` and the rest), series told
  apart by line style as well as colour, no gradients. Charts live on panels only; the
  artwork rule against painted live readouts on world sprites still applies.
- **API sketch.** A consumer supplies named series of (time, value) or (time, open, high,
  low, close) points and a value format; the control owns layout, ticks, thinning and hover.

## Decisions for the owner

*Answered by the owner on 7 October 2026; see the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026). Kept as asked.*

1. **Which game companies may be listed?** All named above, a few (the shipbuilders,
   Smartlink, Ayotimiwa, the Green Energy Company), or none, with invented ubercorps only?
   Penumbra Group probably not. Should our own equipment makers be listed?
2. **Trading anywhere, or only near a broker?** The proposal is anywhere with a commission,
   with being docked as an information edge.
3. **Stable-hash noise** (no rerolls, peeking possible) as proposed, or another approach?
4. **Expected return.** Roughly zero after costs for a passive holder, as proposed, or a
   small positive return as a reward for patience?
5. **Short selling and buying on credit** through Banking loans: wanted later, or never?
6. **Cargo contracts** on native categories for haulers: worth a research round later?
7. **Charts:** a Framework control as recommended, or a trial of a forked XCharts first?

## Proposed rounds

*Replaced by the [planning round](#planning-round-owner-decisions-and-the-first-build-7-october-2026): Framework charts arrive together with Phobos Exchange 0.1.0.*

1. **Framework charts.** The control and its game-free core, first used for a loan or credit
   line balance history in Banking. Proves drawing in play before the market exists.
2. **Exchange core.** The `exchange` pack and validator, the price model reading native
   factors, the saved section with old-record fixtures, settlement through `StatUSD` and the
   ledger, F3 commands, time-skip stepping, the economy simulation and a performance finding.
3. **Exchange page.** Watch list, company pages with charts, holdings, standing orders.
4. **Story and place.** Company news and threads, flag-driven jumps, tips, the docked market
   report and delayed wire news.
5. **Later.** Dividends if not earlier, short selling and credit, cargo contracts.

## Sources

**Game.** Blue Bottle Games' installed Ostranauts: `data/market` (market profiles, production
maps, categories, cargo pods), `data/headlines`, item, ship registry and advert text; local
inspection of `MarketManager`, `ShipMarket` and `StarSystem` (decompile kept in `.local`, not
redistributed). Game-documentation context: Joshu,
[Official Ostranauts Modding Guide, 22 June 2026](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946).

**Finance research** (authors as listed; our parameters and simplifications are our own and
imply no endorsement):

- Benoit Mandelbrot, [The Variation of Certain Speculative Prices](http://www.jstor.org/stable/2350970), Journal of Business 36(4), 1963.
- Robert C. Merton, [Option pricing when underlying stock returns are discontinuous](https://dspace.mit.edu/handle/1721.1/1899), Journal of Financial Economics 3, 1976 (MIT working paper).
- Tim Bollerslev, [Generalized autoregressive conditional heteroskedasticity](https://ideas.repec.org/a/eee/econom/v31y1986i3p307-327.html), Journal of Econometrics 31(3), 1986.
- Thomas Lux and Michele Marchesi, [Scaling and criticality in a stochastic multi-agent model of a financial market](https://www.nature.com/articles/17290), Nature 397, 1999.
- Rama Cont, [Empirical properties of asset returns: stylized facts and statistical issues](https://ideas.repec.org/a/taf/quantf/v1y2001i2p223-236.html), Quantitative Finance 1(2), 2001.
- Bence Tóth, Yves Lempérière, Cyril Deremble, Joachim de Lataillade, Julien Kockelkoren and Jean-Philippe Bouchaud, [Anomalous price impact and the critical nature of liquidity in financial markets](https://doi.org/10.1103/PhysRevX.1.021006), Physical Review X 1, 2011.
- David Byrd, Maria Hybinette and Tucker Hybinette Balch, [ABIDES: Towards High-Fidelity Market Simulation for AI Research](https://arxiv.org/abs/1904.12066), 2019.

**Libraries** (checked 7 October 2026): [XCharts](https://github.com/XCharts-Team/XCharts)
(MIT; source read through the GitHub API), [QLNet](https://github.com/amaggiulli/qlnet),
[QuantLib](https://www.quantlib.org), [MemExchange](https://github.com/chironn/MemExchange),
[StockSharp.Algo](https://feed.nuget.org/packages/StockSharp.Algo/5.0.231),
[Math.NET Numerics](https://numerics.mathdotnet.com/), [MAXE](https://arxiv.org/abs/2008.07871v2).
