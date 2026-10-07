# Example add-on: Keelhaul Listing

A small, real add-on for Phobos Exchange, kept here as the worked example of a listed
company and its story content in [publishing an add-on](../../../docs/publishing-an-add-on.md).
The repository's checks load it over the shipped packs, so the format it shows is the
format that works. Start from it to list your own corporation.

What it does:

- Lists **Keelhaul Freight** (ticker KHF) in the Mining and volatiles sector. Its price
  follows ore at Zhonghuamen Terminal, and it has one piece of **news**: when the story flag
  `keelhaul-titan-contract` is set, the price jumps 8% and the market wire carries the line
  in the file (`phobos/PhobosExchange/exchange/keelhaul.json`). The contract comes round again
  (Phobos Exchange 0.4.0): its arc repeats with a 14-day cooldown, each time the price jumps
  again, the company's trend carries a further 4% on average, and the jump unwinds over the
  following weeks. Half of the first jump stays for good.
- Gives the company a **history before the game** (Phobos Exchange 0.3.0): founded in 2052,
  listed in 2058 at 4 credits a share, with a lore entry for its founding and one event, the
  Ceres ore route won in 2063, that lifted its price 30%. The exchange draws its price chart
  back to 2058 from these, and the company page lists both entries. The ids carry the
  add-on's `keelhaul` prefix.
- Tells that story (`phobos/PhobosFramework/story/keelhaul.json`): an arc that puts the contract on the TV news when it starts, then sets the price-moving
  flag after five game days. It also shows how to
  add a company advert, local chatter tied to story flags, and an encyclopedia article.
- Answers the exchange's events: buying shares brings a letter from Keelhaul's shareholder
  desk, a large holding brings another note, and sharp rises or falls can put wire reports
  on the TV. Each event has its own arc in the story file. An add-on may name those arcs because
  Phobos Exchange registers its `exchange` namespace: ids that start `exchange-` and then
  the add-on's own prefix belong to the add-on.

The remaining event is `sold-out`, when the player sells their last share. See the
[exchange stories handoff](../../../docs/development/exchange-stories-handoff.md) for the
flags and their timing.

To try it, copy this folder into the game's `Ostranauts_Data/Mods` folder, enable it in
the MODS screen after the Phobos mods, restart, and type `phobosframework addons` in the
F3 console. KHF then shows on the exchange. To see the news without waiting, type the
game's `unlockdebug`, then `phobosframework story flag keelhaul-titan-contract confirm`,
on a copy of a save.
