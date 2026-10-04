# Editing the Phobos data files

Since Phobos Framework 0.49.0, some of the numbers behind the Phobos mods live in
plain text files you can read and change: prices, how long a job takes, what a
repair needs, what dismantling returns, which merchants stock what and how often,
what a charge yields and how much a store holds.
You never edit the mod's own files. You write a small file of your own next to
your game settings, and the mod reads it on top of the shipped one.

## Where the files are

- The shipped tables: `mods/<Mod>/framework/<schema>.json` inside each Phobos mod
  folder (for example `PhobosManufacturing/framework/economy.json`). Read them;
  do not edit them, an update would overwrite your changes.
- Your files: `BepInEx/config/<Mod>/<schema>/`, for example
  `BepInEx/config/PhobosManufacturing/economy/my-prices.json`. The folder is
  created the first time the game loads with the mod installed. Every `.json`
  file in it is applied, in name order.

## What a file looks like

Copy only what you want to change, keeping the same nesting as the shipped file.
This file makes the V4 refinery cheaper and sells it more often at Halvorson's:

```json
{
  "equipment": {
    "PhobosVolatilesRefinery": { "price": 48000 }
  },
  "offerTemplates": [
    { "merchant": "ItmOKLGSupplyKioskInv", "tag": "Scrap", "form": "LooseDmg", "condition": "Broken", "chance": 0.20 },
    { "merchant": "ItmOKLGFixer", "tag": "Fixer", "form": "Loose", "condition": "Worn", "chance": 0.10 },
    { "merchant": "ItmTraderSanDiegoHalvorsonInv", "tag": "Industrial", "form": "Loose", "condition": "Pristine", "chance": 0.60 },
    { "merchant": "ItmVORBScrapKioskInv", "tag": "VenusScrap", "form": "LooseDmg", "condition": "Broken", "chance": 0.15 },
    { "merchant": "ItmVORBScrapKioskInv", "tag": "VenusRefurb", "form": "Loose", "condition": "Refurbished", "chance": 0.20 }
  ]
}
```

The rules:

- **Named entries merge.** Give an entry's name and only the fields you change;
  everything else keeps the shipped value. Here only the refinery's price moves.
- **Lists replace.** A list in your file replaces the whole shipped list, so copy
  the entries you want to keep (as the offer templates above).
- **You can add, never remove.** A new offer works; writing `null` to delete an
  entry is refused. Equipment cannot be added by a file: a machine needs its code.
- **Unknown fields are refused.** A misspelt field name rejects the whole file, so
  the mod never guesses what you meant.
- **Some rules always apply.** Salvage must weigh what the machine weighs, prices
  and work must be above zero, chances are 0 to 1, and a recipe must conserve mass
  and use only the game's own gases. A file that breaks one is skipped.
- **A published recipe never changes; add a new one.** Every shipped recipe
  revision is frozen, because a machine mid-charge in your save remembers only its
  revision number. To change what a charge yields, copy the recipe under a new id
  with a higher `revision` (the machine offers the highest); a file that edits a
  frozen revision is skipped with that reason. Material masses are tied to the
  recipes that use them in the same way.
- **A published crop never changes either; add one beside it.** A rack that is
  growing a crop remembers only the crop's name, so the shipped crops are frozen
  like recipes. See [Adding a crop](#adding-a-crop).

## Editor help

The repository ships a JSON Schema for each pack in `schemas/` (`economy`,
`process-recipes`, `materials`, `vessels`, `equipment`, `crops`). Point your editor at them and it will
complete field names and flag a wrong type or range as you type. In VS Code, add
to your settings (adjust the path to where you cloned or downloaded the schemas):

```json
"json.schemas": [
  { "fileMatch": ["**/BepInEx/config/Phobos*/economy/*.json", "**/framework/economy.json"], "url": "./schemas/economy.schema.json" },
  { "fileMatch": ["**/BepInEx/config/Phobos*/materials/*.json", "**/framework/materials.json"], "url": "./schemas/materials.schema.json" }
]
```

Do not write a `$schema` line into a pack or an override file: unknown fields are
refused, and that is one. The schemas check shape and ranges only; the rules that
need other entries (mass conservation, salvage weight, known ids, frozen
revisions) are checked when the game loads the file.

## Seeing what happened

A skipped file is written to the BepInEx log with the reason, and `phobosframework
status` on the F3 console lists every data pack with how many of your files were
applied and which were skipped. Files that passed still apply when a later one is
skipped. Changes take effect on the next game load; nothing reloads live.

## What it does to saved games

Prices, work and merchant odds change for future work and future restocks; an
existing job keeps the work it started with, and a shop keeps what it already
holds. Changing a store's capacity or empty weight in a vessels file does not
silently change a store you already own: it shows as needing attention with an
Accept button, and its contents wait until you accept. A charge already running
keeps the recipe revision it started with. A rack keeps growing the crop it was
planted with; if you remove the file that added that crop, the rack waits, marked
as needing attention, until the file is back.

## Which packs exist

| Mod | Schema | What it holds |
| --- | --- | --- |
| Phobos Manufacturing 0.11.0 | `economy` | Machine and store prices, work, repair bills, salvage, offers, lots, regional factors, world loot |
| Phobos Manufacturing 0.12.0 | `process-recipes` | The six V4 charges: inputs, products, off-gas, seconds |
| Phobos Manufacturing 0.12.0 | `materials` | Ingots, carbon stock, remainders and mined chunks: mass, price, stack, category |
| Phobos Shipbreaker 0.46.0 | `process-recipes` | The F6 furnace recipes and thermal profiles, the T2 thaw recipes, the R4 budget |
| Phobos Shipbreaker 0.46.0 | `materials` | Housing stock, ingots, remainders and the reject packets |
| Phobos Manufacturing 0.13.0 | `vessels` | The six gas store families: capacity, empty weight, leak rate when damaged |
| Phobos Shipbreaker 0.47.0 | `vessels` | The Y2 bin's weight and cells per tile (the S3 silo's entry moved to Framework in 0.58.0) |
| Phobos Agriculture 0.23.0 | `vessels` | The E2 nutrient hopper's capacity and weight; the retired R3 reservoir's ratings, read when an old reservoir converts |
| Phobos Shipbreaker 0.48.0 | `economy` | Every Rivetline machine and section: price, work, repair bill, salvage; the coolant conduit; offers, regions, lots, world finds |
| Phobos Agriculture 0.24.0 | `economy` | Every Verdemorrow machine: price, repair and dismantle work, bills, salvage; the irrigation pipe; offers, regions, lots, loot |
| Phobos Auto Nav 0.30.0 | `economy` | The three navigation boards: price, repair, dismantle, offers, regions, the lot of sixteen, derelict salvage |
| Phobos Agriculture 0.25.0 | `materials` | Seeds, nutrient and irrigation charges, produce, meals, recovery supplies and wastes: mass, price, stack, category |
| Phobos Framework 0.57.0 | `economy` | The shared gas and process-water lines, and since 0.58.0 the Rivetline S2 to S5 water silos: price, work, bills, salvage, offers, lots, world finds |
| Phobos Framework 0.58.0 | `vessels` | The S3 water silo's capacity and weight |
| Phobos Manufacturing 0.17.0 | `equipment` | The V4 refinery's size, weight, power, heat into the room, feed cells and connection points (read only for now) |
| Phobos Agriculture 0.40.0 | `crops` | What a Firstlight rack grows: each crop's growth time, power, water, nutrient and carbon budgets, harvest, items, feed and artwork |
| Phobos Agriculture 0.40.0 | `process-recipes` | What the Hearth-2 cooks: one portion in, one portion out, and the seconds it takes |

Other sizes (S2, S4, S5, E3, E4, Y3, Y4 and the medium and large gas stores) follow
from the listed entry: one tile wider per step (the S2 one tile narrower than the S3), more capacity and less weight per
kilogram, so you edit the listed size and the rest follow. Assembly sections and
navigation boards are entries too: a section has a price, dismantle work and salvage
(the three Shipbreaker sections are retired since 0.60.0 and are never sold, but the
entries stay for copies still held);
a board has a price, a broken price, repair and dismantle work. Every entry names
the `lot` it ships in and the `floor` its offers never fall below, from the pack's
own `lots` and `chanceFloors` tables.

The equipment pack is for reading only, for now: a file that changes a shipped
machine's size, weight, power or connection points is skipped with a message,
because a new footprint or connection would move a machine already placed in
your save. Recipes may also say how much of a commodity a machine needs on hand
but gives back (`circulates`, such as wash water) and how much heat the reaction
itself gives off into the room over the charge (`reactionKWh`, negative when it
draws heat in). A recipe can also name earlier revisions of its own machine that
it replaces (`supersedes`, a list of revision numbers): the machine then offers
the new recipe for new charges, while a charge already bound to the old revision
still finishes by it. Only one recipe may supersede a given revision. More packs (loot) follow as the tables move over; this page lists
them as they land.

## Adding a crop

Put a file in `BepInEx/config/PhobosAgriculture/crops/` (Agriculture 0.40.0). The
shipped `crops.json` in the mod's `framework` folder is the reference. A new crop
gets its own planting job on the rack, its own feed on the W2 and its own crew
order, with no code.

```json
{
  "crops": {
    "quick-lettuce": {
      "name": "Quick lettuce",
      "hours": 36, "kw": 0.4,
      "seedKg": 0.005, "finalKg": 1.2,
      "carbonKg": 0.0605, "nutrientKg": 0.005, "waterKg": 1.2658, "vapourKg": 0.1,
      "seedCarbonKg": 0.0045,
      "edibleKg": 1, "keptStockKg": 0, "portionKg": 0.25,
      "stock": "PhobosVerdemorrowContinuanceLettuce",
      "produce": "PhobosVerdemorrowLettuce",
      "feed": "quick-lettuce-v1", "feedCommodity": "quick lettuce feed",
      "art": "Lettuce"
    }
  }
}
```

The rules a crop is held to:

- **Mass must close.** Water plus nutrient plus 0.4 times the carbon, less the
  vapour, must equal the final mass less the seed. The 0.4 is the carbon dioxide a
  plant takes in less the oxygen it gives off, per kilogram of carbon it fixes.
- **It uses the mod's own items.** `stock` (what is planted) and `produce` (what
  each portion becomes) must be Agriculture items, `seedKg` must be one unit of the
  stock and `portionKg` one unit of the produce. A file cannot add an item.
- **It borrows artwork.** `art` names a shipped crop's growth stages: `Potato`,
  `Lettuce`, `LettuceSeed`, `Wheat`, `Tomato` or `Soybean`.
- **It needs its own names.** The crop's name, `feed` and `feedCommodity` must not
  be used by another crop. They are saved with your racks and pipes, so do not
  rename them later. `name` is the plain name shown in the game.
- **A rack's limits apply.** At most 1.5 kW, 20 kg of water and 0.5 kg of nutrient.
- **Picking is optional.** `picks` (up to 10) and `pickKg` let a ripe plant be picked
  that many times before its harvest, taking up to `pickKg` of whole portions each
  time; the shipped tomato uses them. A pick must take at least one portion and less
  than one cycle's growth.
- **The CO2 curve is yours to tune.** `co2Response.points` lists `[kPa, factor]`
  pairs in rising pressure (factors 0.5 to 2); it changes how fast crops grow, never
  what they take or give, so it is not frozen.
- **Shipped crops cannot be edited.** A file that changes `potato`, `lettuce` or
  `lettuce-seed` is skipped. Copy one under a new name instead.

The Hearth-2's recipes work the same way in
`BepInEx/config/PhobosAgriculture/process-recipes/`: one item in, one item of the
same mass out, a number of seconds, and no two recipes for the same item.
