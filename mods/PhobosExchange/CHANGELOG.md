# Phobos Exchange changelog

Maintained from 7 October 2026. Dates on Draft entries record preparation,
not Steam publication.

## [Unreleased]

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
