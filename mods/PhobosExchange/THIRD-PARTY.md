# Phobos Exchange provenance

Phobos Exchange is original Phobos code under the repository licence. It uses
Phobos Framework as a separately supplied dependency; Framework retains its own
upstream adaptation notices.

The game's cargo market (`MarketManager`, `ShipMarket`, its station market and
production data), ledger and PDA behaviour were inspected locally in
[Blue Bottle Games' Ostranauts](https://store.steampowered.com/developer/bluebottlegames/)
1.0.1.5. The mod reads the game's own station price factors and writes trades to the
game's own ledger at runtime by reference; no game asset, definition or code is copied
into the package. Game assemblies and decompiled source are excluded from
distribution. BepInEx and Unity references are resolved from the owner's
installation and are not bundled.

Smartlink, Testudo, Ayotimiwa Corp. and the Green Energy Company are Blue Bottle Games'
fictional companies, named as the game names them; the exchange reports on them in a
neutral wire-service voice and never speaks for them. No endorsement by Blue Bottle Games
or Kitfox Games is implied.

The market's random numbers use Sebastiano Vigna's SplitMix64 finaliser (public domain)
and Peter J. Acklam's rational approximation of the inverse normal distribution,
implemented independently. The model reproduces findings from published finance research
(Rama Cont, 2001; Robert C. Merton, 1976; Bence Tóth and colleagues, 2011); the design
record `docs/development/share-market-and-charts-design.md` gives every source with a
link. No research figure is presented as a measurement of this game's market; the
parameters are authored balance.
