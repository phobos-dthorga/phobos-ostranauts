# Example add-on: Keelhaul Listing

A small, real add-on for Phobos Exchange, kept here as the worked example of a listed
company with its own story in [publishing an add-on](../../../docs/publishing-an-add-on.md).
The repository's checks load it over the shipped packs, so the format it shows is the
format that works. Start from it to list your own corporation.

What it does:

- Lists **Keelhaul Freight** (ticker KHF) in the Mining and volatiles sector. Its price
  follows ore at Zhonghuamen Terminal, and it has one piece of **news**: when the story flag
  `keelhaul-titan-contract` is set, the price rises 8% once and the market wire carries the
  line in the file (`phobos/PhobosExchange/exchange/keelhaul.json`).
- Tells that story (`phobos/PhobosFramework/story/keelhaul.json`): an arc that, some while
  after it starts, puts the contract on the TV news and then sets the flag.
- Answers one of the exchange's own events: Phobos Exchange starts the arc
  `exchange-keelhaul-freight-bought` the first time the player buys the company, so a
  letter arrives from Keelhaul's shareholder desk. An add-on may name that arc because
  Phobos Exchange registers its `exchange` namespace: ids that start `exchange-` and then
  the add-on's own prefix belong to the add-on.

The other events a company can answer are `surge` and `slump` (its biggest moves in a day),
`major-holder` and `sold-out`; see the
[exchange stories handoff](../../../docs/development/exchange-stories-handoff.md).

To try it, copy this folder into the game's `Ostranauts_Data/Mods` folder, enable it in
the MODS screen after the Phobos mods, restart, and type `phobosframework addons` in the
F3 console. KHF then shows on the exchange. To see the news without waiting, type the
game's `unlockdebug`, then `phobosframework story flag keelhaul-titan-contract confirm`,
on a copy of a save.
