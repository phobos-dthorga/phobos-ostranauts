# Phobos Exchange changelog

Maintained from 7 October 2026. Dates on Draft entries record preparation,
not Steam publication.

## [Unreleased]

## [0.3.0] - 2026-10-07 - Draft

### Added

- First-draft histories for the Lodestar Exchange, its four sectors and all eight companies. Every company has a founding year, a listing year and a chart that reaches back to that listing, however recently you installed the exchange.
- Each market, sector and company history records dated events; an optional listing price sets where a company's chart begins. The history entries shape the past drawn before a save's first day with the exchange.
- An All chart range: the whole history from the listing to now, on a scale that keeps a rise from small beginnings readable, with the years along the bottom and the history's big events marked. Point at a mark to read it.
- The company page and the F3 quote say when the company was founded and when it listed. F3 phobosexchange history, with or without a ticker, prints the exchange's or a company's history.
- Two price-moving story news entries per company can move its price once when their flags are set. The Exchange also sends letters when you buy shares or become a major holder, and TV reports when a company's price surges or slumps.
- Named correspondents, local chatter, five adverts and a share-board article give the market a voice. The Keelhaul Listing add-on demonstrates company news, event responses, adverts, chatter and encyclopedia entries.
- Founding dates and events that do not come from Ostranauts are author-written proposals for owner review, not claims about established game canon.
- A time skip of more than two years now leaves a record of those years on the All chart instead of a straight line.

### Compatibility and limits

- Requires Phobos Framework 0.130.0, for its charts' log scale and marks and its calendar months.
- Saves: the exchange record gains the save's own monthly prices, kept to at most 240 points per company however long you play. The history before your first day with the exchange is never saved: it is drawn again at each load from the company list and your save, so later histories from writers or add-ons reach saves already started. What your save has actually seen never changes.
- Saves from 0.1 and 0.2 load and start their monthly record from their two years of weekly prices. In a save played more than two years under an older version, the years before that are drawn rather than remembered.
- A time skip of more than two years gives different, equally exact prices than 0.2 did, and a price alert can now go off partway through it.
- History never moves a price in play: story news does that. History entries that move a price end in 2076, because the two years before a new game are the market's own record. A company added to a running save gets a drawn history for the years since 2079 too, which does not follow the market's own record of those years.
- Checked offline: the year and month rules, the drawn history meeting its listing price and the save's own first price exactly, events showing as steps, the same history after a reload, the monthly record's thinning and saved form, older records, time skips of up to a thousand years within the step bound, and the calendar against the game's. Not yet seen in the game.

## [0.2.1] - 2026-10-07 - Draft

### Fixed

- The exchange failed to open in play: 0.1.0 read the stations' markets before the market had started, so every load failed and prices never moved, with an error logged every second. It now starts the market first. If opening ever fails again, trading closes until the next load with one plain message, and nothing is saved over your record.
- A new market no longer drifts on its first day while the stations' markets settle in, which the wire reported as shortages or gluts. It opens at the listed prices with today's station markets already counted.
- A trade now moves the market before it touches your shares or cash, so a fault can never leave shares bought but unpaid.
- The 120-day chart showed only its oldest 105 days and drew a straight line to now. It now shows every day, and the latest point sits where it belongs, just before now.

### Compatibility and limits

- Saves: no change to the record. A save in which 0.1.0 failed to open has no exchange record, so the market opens fresh with its two years of history.
- Checked offline: opening a new and a saved market in the order the game uses, the first day with settled drivers, and the chart's points. Not yet seen in the game.

## [0.2.0] - 2026-10-07 - Draft

### Added

- Story news that moves prices: each company can list news in the exchange file, a story flag, how far the price moves and the line the market wire prints. When a story sets the flag, the price moves once and stays moved, and the wire names the news as the cause.
- The exchange tells stories what you do: each company's biggest moves of the day, your first purchase, a large stake and selling out set story flags and start the matching story arcs when a story pack has them, so letters and news can follow your trading.
- A story pack with a thread for the exchange and one for each company, ready for the stories to come.
- A worked example add-on, Keelhaul Listing, that lists a company of its own, tells a story that lifts its price, and answers your first purchase with a letter. Add-ons can now write the story events of their own companies.

### Compatibility and limits

- Requires Phobos Framework 0.129.0, which lets add-ons name the exchange's events for their own companies.
- Saves: news that has moved a price is kept in the exchange record under its own entry. Saves from 0.1.0 load unchanged.
- The stories themselves are not written yet: this version ships the hooks and the threads. The figures are still invented for the game.
- Checked offline: the news validation, a price moving once and staying moved through a save and load, the wire not repeating news, the story event names, and the example add-on loading through the game's own loader. Not yet seen in the game.

## [0.1.0] - 2026-10-07 - Draft

### Added

- An EXCHANGE app on the PDA home screen: shares in the system's companies on the Lodestar Exchange. Tap it and the PDA closes and the Exchange panel opens.
- Eight listed companies in four sectors: the game's own Smartlink, Testudo, Ayotimiwa Corp. and the Green Energy Company, two new ones (Brightvein Mining and Coldwell Volatiles), and Verdemorrow and Halewright, the makers of Phobos Agriculture's and Phobos Medical's equipment.
- Prices that follow the game's own cargo markets: each company follows a few named stations, and a shortage or a glut there moves its price. Prices also run in rising and falling phases lasting weeks (for the whole market, each sector and each company), creep upward over the years, and carry day-to-day noise with the odd sudden jump.
- A chart on each company's page over 3 days, 120 days or 2 years, with your alerts and what you paid marked; point at it to read a value.
- Buying and selling for cash: the page works out the cost or the proceeds before you confirm. Each order pays the spread and a 0.4% commission (at least 25 credits), and a big order pushes the price against you, so quick round trips lose. Trades go in the game's own ledger.
- Price alerts at ten percent either side from the panel, or any level from F3. Each goes off once, also through a time skip.
- The market wire: big moves over a day and the turn of a phase, each with its cause, in the crew log and on the Market page. After a long absence, one summary of the biggest movers and your shares.
- Your shares: each holding's value, cost and gain, and Sell everything (press twice).
- A first two years of price history in every save, so the charts have something to show from the start.
- The company list is a data file (exchange), for players and add-ons to change or add to.
- F3 console: phobosexchange quotes, quote, buy, sell, holdings, alert, alerts, sellall and open; drivers and state are read-only readouts.
- Test commands for trying the exchange out, test shock and test reset, locked until the game's own unlockdebug, warning every time and needing confirm. A save they touch is marked as test-changed.

### Compatibility and limits

- Requires Phobos Framework 0.128.0, which adds the chart, the shared holdings list and the test-command gate.
- Saves: one record on your character holds the market, your shares, your alerts and the chart history, about 12 KB in all and fixed in size. Nothing else changes except your cash and the ledger lines your trades write.
- Reloading never rerolls prices: a moment always has the same price however you got there, through watching, fast play or a skip. Any time jump, up to years at once, is caught up at a fixed cost.
- Before removing the mod, sell everything: shares are only worth credits while the exchange can buy them back. A removed exchange leaves its record untouched, and your shares return if you reinstall it.
- Shares earn a few percent a game year on average, less than a Phobos Banking loan costs, so borrowing to hold shares loses money on average.
- The figures are invented for the game and tuned for weeks of play; they are not measured from any real market.
- An original pixel-art Workshop cover showing a spacer following market prices aboard ship; it is promotional artwork, not a gameplay screenshot. The EXCHANGE PDA app icon remains script-drawn.
- Checked offline: the market model, prices matching however time is stepped, jumps of up to fifty years, the save record, trades, alerts, the return guard and an economy simulation; the game's market members and every company's stations against the installed game. Not yet seen in the game; owner gameplay checks are pending.
