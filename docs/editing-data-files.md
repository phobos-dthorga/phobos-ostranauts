# Editing the Phobos data files

Since Phobos Framework 0.49.0, some of the numbers behind the Phobos mods live in
plain text files you can read and change: prices, how long a job takes, what a
repair needs, what dismantling returns, which merchants stock what and how often,
what a charge yields and how much a store holds.
You never edit the mod's own files. You write a small file of your own next to
your game settings, and the mod reads it on top of the shipped one.

## Where the files are

- The shipped tables: `mods/<Mod>/framework/<schema>.json` inside each Phobos mod
  folder (for example `PhobosManufacturing/framework/economy.json`). **These are
  read-only copies for you to look at.** The game reads the same tables from inside
  the mod's own DLL, so editing a copy changes nothing in the game, and an update
  overwrites it anyway. Since Phobos Framework 0.120.0 each copy says so on its first
  line, a `//` note above the opening brace.
- Your files: `BepInEx/config/<Mod>/<schema>/`, for example
  `BepInEx/config/PhobosManufacturing/economy/my-prices.json`. The folder is
  created the first time the game loads with the mod installed. **Every `.json`
  file in it is applied, whatever you name it**, in name order (`a-prices.json`
  before `b-prices.json`), each on top of the ones before. So you can keep your
  changes in several small files, and each still becomes part of the live tables
  when the game loads. A file that breaks a rule is skipped with its reason, and the
  others still apply (see [Seeing what happened](#seeing-what-happened)).

Some files in a mod's `framework` folder are not tables you can change at all, and
their first line says so too: `equipment-names.json` (brand and model names) and the
`frozen-*.json` files (fingerprints of published recipes and crops). The one
exception the other way is `recipes.json` in the Shipbreaker, Agriculture and Auto
Nav folders: those construction recipes are read from the mod folder itself, carry
no note, and are overwritten by every update, so do not edit them either.

To share your changes with other players, see
[publishing an add-on](publishing-an-add-on.md): the same files, in a mod folder of
your own. Add-ons apply before your local files, so yours have the last word unless
a file sets a `priority`.

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
`process-recipes`, `materials`, `vessels`, `equipment`, `crops`, `care`, `lenders` and the others). Point your editor at them and it will
complete field names and flag a wrong type or range as you type. In VS Code, add
to your settings (adjust the path to where you cloned or downloaded the schemas):

```json
"json.schemas": [
  { "fileMatch": ["**/BepInEx/config/Phobos*/economy/*.json", "**/framework/economy.json"], "url": "./schemas/economy.schema.json" },
  { "fileMatch": ["**/BepInEx/config/Phobos*/materials/*.json", "**/framework/materials.json"], "url": "./schemas/materials.schema.json" }
]
```

Strict JSON has no comments, so an editor may mark the first-line note in a shipped
copy as an error. The note is meant for you to read, and the mod's loaders skip it.
If you copy a shipped file as the start of your own, you can keep or delete it.

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
| Phobos Agriculture 0.40.0 | `crops` | What a Firstlight rack grows: each crop's growth time, power, water, nutrient and carbon budgets, harvest, items and artwork; since 0.55.0 the room crops grow in, how stress wears them down and how a W2 feeds them |
| Phobos Agriculture 0.40.0 | `process-recipes` | What the Hearth-2 cooks: one portion in, one portion out, and the seconds it takes |
| Phobos Medical 0.1.0 | `care` | The Ward-3 bed's idle and working power, who counts as injured, and when a resting patient gets up |
| Phobos Manufacturing 0.44.0 | `outcomes` | Charges with more than one possible result (the gangue wash), and the odds of each |
| Phobos Framework 0.100.0 | `lines` | Where a pipe or conduit segment counts as laid: floor, inside a wall, and the tiles that carry nothing |
| Phobos Medical 0.1.0 | `economy` | The Ward-3 bed: price, work, repair bill, salvage, offers, regions, world finds and kiosk tier |
| Phobos Framework 0.107.0 | `story` | TV news, adverts and story arcs, and how much of the TV they take; since 0.114.0 the places, people and threads content belongs to; since 0.108.0 also small talk, loading tips and encyclopedia articles; since 0.109.0 branches and credits; since 0.110.0 data files and pictures. Agriculture ships Verdemorrow's. See [writing story content](writing-story-content.md) |
| Phobos Framework 0.111.0 | `upkeep` | Crew upkeep: what a tuning session adds, how long an inspection is good, each machine family's share of the gain |
| Phobos Framework 0.116.0 | `stores` | Which of the game's containers are not stores: weapons, chargers, filter holders, toilets |
| Phobos Banking 0.2.0 | `lenders` | Who lends, where, to whom and on what terms: the CREDIT app's lenders. See [Adding a lender](#adding-a-lender) |

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

## Adding an item

Phobos Manufacturing 0.45.0, Phobos Shipbreaker 0.75.0 and Phobos Agriculture 0.48.0
let a file add items of its own to the `materials` pack, with their own pictures, for
recipes to make and use. Because an item needs
a picture in a mod folder, this is really for add-ons:
[publishing an add-on](publishing-an-add-on.md#6-new-items-with-your-own-pictures)
has the fields and rules. The shipped materials can still only be retuned.

## Tuning crew upkeep

The `upkeep` pack (Framework 0.111.0) holds the figures behind
[crew upkeep](crew-automation.md#upkeep-on-long-hauls) that are not ordinary
settings. The shipped file:

```json
{
  "tuneStep": 0.2,
  "tuneStepSkilled": 0.3,
  "inspectionValidHours": 24,
  "inspectedFadeShare": 0.5,
  "practiceMinutes": 10,
  "families": {}
}
```

- **`tuneStep`, `tuneStepSkilled`:** how much of a full tune one session adds,
  for an unskilled and a skilled crew member. Five unskilled sessions make a full
  tune as shipped.
- **`inspectionValidHours`:** game hours an inspection stays good for.
- **`inspectedFadeShare`:** how fast an inspected machine's tune fades, as a share
  of the ordinary rate. 0.5 means it lasts twice as long.
- **`practiceMinutes`:** game minutes of one practice session (Framework
  0.113.0), from 2 to 60. A session teaches as much as the same time studying at
  a terminal.
- **`families`:** a machine family's own share of the gain, by its key. The F3
  command `phobosframework upkeep` lists the families aboard.

To change one, put a file in `BepInEx/config/PhobosFramework/upkeep/`:

```json
{ "families": { "manufacturing.charge": { "gainShare": 0.5 } } }
```

- **The session lengths, the largest gain and the fade time** are not here: they
  are settings in Framework's config file, section `Upkeep`
  (`InspectionMinutes`, `TuningMinutes`, `MaxTuningGain`, `TuneFadeHours`).
- **What is refused:** a step of nothing, a skilled step smaller than the
  unskilled one, a share outside 0 to 1, a practice session outside 2 to 60
  minutes, and unknown fields. The file is skipped
  with a message and the shipped figures stand.
- **Saved games:** a machine saves how tuned it is as a share of a full tune, so
  changed figures apply at once and nothing needs converting.

All of these are authored balance, not measurements.

## What counts as a store

The `stores` pack (Framework 0.116.0) keeps things that have a container but are
not stores out of the **Take feed from** and **Send products to** pickers, crew
order stores and housekeeping, and stops crew taking anything out of them. A
ship weapon's magazine, a battery charger, a scrubber's filter holder or a
toilet each has a container, but nobody keeps ore in one. The shipped file:

```json
{
  "requireInteraction": "Inventory",
  "excludeConditions": [ "IsShipWeapon", "IsRechargingContainer", "IsAtmoScrubber", "IsNavStation",
    "IsToilet01", "IsSink", "IsWaterRecycler", "IsSafePumpTestudo" ],
  "excludeContainers": [ "TIsFitAmmo*", "TIsFitContainerFilterCO2", "TIsFitContainerWaterFilter",
    "TIsFitContainerNavMod", "TIsFitContainerLiquid", "TIsFitContainerSafePumpO2Bottle" ]
}
```

- **`requireInteraction`:** a store must offer this entry on its right-click
  menu. The game's `Inventory` is its "open" entry, so a container you cannot
  open by hand (the toilet, the coffee machine, a cargo lift) is not a store.
  Leave it empty to drop the rule.
- **`excludeConditions`:** anything carrying one of these game conditions is not
  a store. `IsShipWeapon` covers every ship weapon in the game, and other mods'
  weapons when they use it. A name from a mod you do not have does nothing.
- **`excludeContainers`:** the game's container rules (`strContainerCT` in its
  data) that hold one kind of thing only. A name ending in `*` covers every rule
  that starts with the rest, so `TIsFitAmmo*` covers every magazine.

The game's racks, bins, floor bins, fridge and secret compartment stay stores,
and so does every installed Phobos container. Since the names are the game's
own, equipment from another mod can be added by the condition or container rule
its data gives it. To change the lists, put a file in
`BepInEx/config/PhobosFramework/stores/`. A list you set replaces the shipped
list, so write the whole list:

```json
{ "excludeConditions": [ "IsShipWeapon", "IsRechargingContainer", "IsAtmoScrubber", "IsNavStation",
    "IsToilet01", "IsSink", "IsWaterRecycler", "IsSafePumpTestudo", "IsSomeModTurret" ] }
```

- **What is refused:** names with spaces or symbols, a name listed twice, more
  than 48 names in a list, a `*` anywhere but at the end, a prefix shorter than
  three letters, and unknown fields. The file is skipped with a message and the
  shipped rules stand.
- **Saved games:** nothing is saved. A store you already chose stays recorded,
  but once a rule excludes it the machine stops using it and its status says the
  store is "not a store any more"; choose another under Take feed from or Send
  products to.

## Where a pipe counts as laid

The `lines` pack (Framework 0.100.0) says where a segment of water, gas, acid,
ethanol, irrigation or coolant line joins its line. The shipped rule:

```json
{
  "placement": {
    "default": {
      "forbiddenTiles": [ "IsFloorFlex", "IsEVATile" ],
      "supports": [
        { "tile": "IsFloor", "object": "floor" },
        { "tile": "IsWall", "object": "IsWall" }
      ]
    }
  }
}
```

A segment counts when its tile carries none of the forbidden conditions, and one
support matches: the tile carries the support's `tile` condition, and something
installed and undamaged stands on it that is the `object`. The word `floor` means
any of the game's floors; anything else is a condition the object carries. So a
pipe works on an intact floor, or inside an intact wall beside the ship's own
conduit. The names are the game's own condition names.

To change it, put a file in `BepInEx/config/PhobosFramework/lines/`. A list you
set replaces the shipped list, so write the whole list:

```json
{ "placement": { "default": { "forbiddenTiles": [ "IsFloorFlex" ] } } }
```

- **A rule for one kind of line:** add an entry named after the line family id
  beside `default`, such as `PhobosFramework.ProcessWater` or
  `PhobosFramework.GasLine`. That line uses its own rule and the rest keep `default`.
- **What is refused:** a file with no `default` rule, a rule with no supports,
  names with spaces or symbols, a rule that forbids the tile one of its supports
  needs, and unknown fields. The file is skipped with a message and the shipped
  rule stands.
- **Saved games:** nothing is saved. A changed rule applies to every line the
  next time the game loads.

## Retuning the gangue wash

The `outcomes` pack (Manufacturing 0.44.0, Framework 0.88.0) holds the tables for
charges that can turn out more than one way. A table belongs to one recipe and
lists the recipes a charge of it may become, each with a whole-number weight:

```json
{
  "tables": {
    "gangue-wash": {
      "outcomes": {
        "gangue-wash": 50,
        "gangue-wash-steel": 30,
        "gangue-wash-aluminium": 15,
        "gangue-wash-nickel-iron": 5
      }
    }
  }
}
```

An outcome is picked as often as its weight out of the table's total, so these
are 50, 30, 15 and 5 in a hundred. To change the odds, put a file in
`BepInEx/config/PhobosManufacturing/outcomes/` naming only what you change:

```json
{ "tables": { "gangue-wash": { "outcomes": { "gangue-wash": 20, "gangue-wash-steel": 60 } } } }
```

- **Switch an outcome off** with a weight of 0. A table needs some weight left.
- **Add an outcome** by writing a recipe of your own in a `process-recipes` file
  (same machine, same inputs, same circulating volumes and same seconds as the
  base recipe, and it must balance), then naming it in the table.
- **Add a table** for a recipe of your own the same way; the table lists the
  base recipe itself as one of its outcomes.
- **What is refused:** an outcome whose charge differs from its base, a recipe
  in two tables, a weight below 0 or above 10,000, and unknown fields. The file
  is skipped with a message and the shipped table stands.
- **Saved games:** a wash already bound has saved its result; new odds apply to
  washes started after the change. A charge's result comes from the items in it,
  so the same four lumps give the same result under the same table.

## Adding a crop

Put a file in `BepInEx/config/PhobosAgriculture/crops/` (Agriculture 0.40.0). The
shipped `crops.json` in the mod's `framework` folder is the reference. A new crop
gets its own planting job on the rack and its own crew order, with no code. Every
crop uses the same nutrient, so there is nothing to add on the W2.

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
- **It needs its own name.** The crop's name is saved with your racks, so do not
  rename it later. `name` is the plain name shown in the game. The shipped crops
  also carry `feed` and `feedCommodity`: old names from Agriculture 0.54.0 and
  earlier, kept only so old saves can be read. Leave both out of a new crop.
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

## Growing room and stress

The `growth` section of the crops file (Agriculture 0.55.0) holds where crops grow,
how stress wears them down and how a W2 feeds them. It sits beside the crops, not
inside them, so it is not frozen: change any figure in a file of your own in
`BepInEx/config/PhobosAgriculture/crops/`, or in an add-on. Every figure may be left
out and then keeps the shipped value. These are authored gameplay limits, not plant
physiology.

```json
{
  "growth": {
    "room": { "maxC": 33 },
    "stress": { "graceHours": 4 },
    "crops": {
      "potato": { "room": { "minC": 12, "maxC": 26 } },
      "quick-lettuce": { "room": { "maxKPa": 120 } }
    }
  }
}
```

| Field | Shipped | What it does |
| --- | ---: | --- |
| `room.minC`, `room.maxC` | 18, 31 | The room temperature every crop grows in, in degrees C. |
| `room.minKPa`, `room.maxKPa` | 70, 110 | The room pressure every crop grows in. |
| `crops.<crop>.room` | none | One crop's own room. Only the limits you give replace the shared ones. |
| `stress.graceHours` | 2 | Hours without light or water, or in a room outside its limits, that a crop shrugs off. |
| `stress.healthLossPerHour` | 0.01 | Health lost per further hour short of light or water in a room that suits it. Health runs from 1 to 0. |
| `stress.healthLossPerHourOutside` | 0.1 | The same in a room outside its limits. |
| `stress.healthLossPerHourNoAir` | 0.1 | Health lost per hour in a room with no air. |
| `stress.plantWaterKg` | 0.25 | Water a rack needs before a crew order plants in it. |
| `nutrientTargetKg` | 0.1 | Nutrient a W2 keeps in each rack it feeds. A rack holds at most 0.5 kg. |
| `feedStrengthKgPerKg` | 0.01 | Nutrient each kilogram of water the pump moves can carry, fresh or recirculated. |
| `misting.maxCoolingC` | 4 | How far misting can hold a crop below a room that is too hot for it, in degrees C (Agriculture 0.59.0). |
| `misting.waterKgPerHourPerC` | 0.15 | Water misting takes per hour for each degree it covers. |
| `misting.damageShareBeyond` | 0.5 | Share of heat damage left while misting a room hotter than it can cover. |
| `misting.reserveKg` | 2 | Reservoir water misting never uses. |
| `crops.<crop>.misting.maxCoolingC` | none | One crop's own misting limit. |

The rules:

- A lower limit stays below its upper one, between -50 and 100 C and up to 500 kPa.
  This is checked for the shared room and for each crop's room after your changes.
- An entry under `crops` must name a crop in the file, shipped or added. An add-on
  may tune any crop's room and add entries for its own crops.
- Rates are from 0 to 1 an hour; the grace is at most 1,000 hours.
- The nutrient target is above 0 and at most 0.5 kg; the feed strength above 0 and at
  most 1.
- A crop already growing follows the new figures from the next load. Nothing saved
  changes.

The Hearth-2's recipes work the same way in
`BepInEx/config/PhobosAgriculture/process-recipes/`: one item in, one item of the
same mass out, a number of seconds, and no two recipes for the same item.

## Tuning the medical bed

Put a file in `BepInEx/config/PhobosMedical/care/` (Phobos Medical 0.1.0). The
shipped `care.json` in the mod's `framework` folder is the reference. This file
lowers the bar for who may rest and lets patients stay in bed longer:

```json
{
  "admission": { "pain": 10, "wound": 0.05, "dischargeShare": 0.25 }
}
```

What the file holds:

- **`stations.bed`**: `idleKW` with nobody under care and `workingKW` while caring,
  at most 2 kW, idle no more than working.
- **`admission`**: a person counts as injured, and may **Rest and recover**, when
  any figure reaches its threshold, a wound is bleeding or a fracture is unsplinted.
  The figures are the game's own scales: `bloodLost` (blood lost; the game's shock
  bands start at 15 and 30, and 40 is fatal), `infection` (35, 65, 95 fatal),
  `pain` (25, 50, 75 knocks out) and `wound` (the worst wound's cut or blunt damage,
  0 to 1). Each threshold must sit below the game's fatal or knock-out level.
- **`dischargeShare`**: a resting patient gets up once every figure is below its
  threshold times this share and nothing bleeds (0 to just under 1). The gap stops
  a patient getting up and lying down again.

- **`stations.monitor`** (Medical 0.3.0): the Vigil-2's power, as for the bed.
- **`alerts`** (Medical 0.3.0): `bloodLost`, `infection` and `pain` at which a Vigil-2
  posts a caution, each below the game's fatal or knock-out level. A bleeding wound
  always alerts.
- **`levels.bed.weightlessHealing`** (Medical 0.2.0): the share of normal wound
  healing a weightless patient keeps under care, from 0.05 (the game's own, no help)
  to 1 (no weightless penalty, the shipped value).
- **`treatments`** (Medical 0.4.0): what the crew do under **Keep patient treated**,
  by name. Each has a `test` (`bleeding`: a bleeding wound with nothing on it;
  `fracture`: an unsplinted broken arm or leg; `spent-dressing`: a dressing gone
  dirty, which comes off first), an `effect` (`slot-item`, the only one so far: the
  item goes on the wound as if you had dropped it there), the `item` used up (a game
  item name), `medicSeconds` (5 to 1800), an optional `skill` (a game skill condition
  such as `SkillMedicalTrauma`; crew who have it go first and work faster) and an
  `order` (lower first). The item must go on a suitable wound by the game's own slot
  rules, or the file is refused when the game loads it. The shipped entries are
  `dress-bleeding`, `splint-fracture` and `change-dressing`.

A wound may match more than one treatment; the crew use the first whose item they
can get, so a later entry is a fallback. This file lets them dress a bleeding wound
with a dirty cloth when no clean one can be had (the game's own dirty-dressing
effect: it stops the bleeding but adds infection, and the change-dressing treatment
swaps it once clean cloth arrives):

```json
{
  "treatments": {
    "dress-dirty": { "test": "bleeding", "item": "ItmScrapClothDirty", "medicSeconds": 45, "order": 15 }
  }
}
```

A treatment stays due while its test holds, so pick an item that changes the wound:
a dressing covers it, a splint splints it. Water or spirits on a bleeding wound clean
it but leave it bleeding, so the crew would keep pouring.

The healing is otherwise the game's own Recuperating and cannot be changed here; a
field such as `heal` is refused. Nothing in this file is saved with your beds, so a change
applies to every bed from the next game load.

## Adding a lender

Lenders are a data pack (Phobos Banking 0.2.0), so you can add your own or change the
shipped ones and shape who lends money where in your game. Put a file in
`BepInEx/config/PhobosBank/lenders/`; the shipped `lenders.json` in the mod's
`framework` folder is the reference. This file adds a small lender on Ceres for anyone
the Galilean Confederacy does not dislike:

```json
{
  "lenders": {
    "mojave-credit": {
      "name": "Mojave Credit Co-op",
      "pitch": "We lend to miners, haulers and anyone else Port Mojave has room for. Small loans, plain terms, no lectures.",
      "accredited": true,
      "home": "bcer",
      "requires": { "standing": [ { "faction": "GalileanConfederacy", "atLeast": "neutral" } ] },
      "ratePerShift": 0.0004,
      "minPrincipal": 2000,
      "maxPrincipal": 60000,
      "offers": [ "cash" ]
    }
  }
}
```

What a lender holds:

- **`name`**: shown in the app and on your ledger as the creditor, 1 to 40 characters,
  without `| , = # [ ] < >`. **`pitch`**: the lender's own words to a customer, up to
  400 characters, no placeholders.
- **`accredited`**: a registered lender (`true`) or a quick-money one (`false`); the app
  says which.
- **`home`**: where it trades, a place key from Framework's story places (`oklg`, `mtrs`,
  `bcer`, `vnca` and the rest; `phobosframework story places` lists them). A regional place
  lends anywhere in its region; a part of one (`venc`, `mtrs-sub`) only to a player docked
  there. **`person`** (optional): a story person who speaks for the lender.
- **`requires`** (optional): who may borrow, the same block story content uses: standing
  with the game's factions, flags, places, crew and the rest
  ([writing story content](writing-story-content.md)). The app says which part is not met.
- **`ratePerShift`**: interest on what you still owe, per shift change, as a share
  (0.0003 is 0.03%), above 0 and at most 0.01.
- **`minPrincipal`**, **`maxPrincipal`**: the smallest loan, and the most you may owe this
  lender at once, 1,000 to 5,000,000 credits.
- **`maxLoans`** (default 1, up to 5): loans from this lender running at once.
- **`offers`**: `cash` (paid into your account), `ship` (a ship broker purchase, Phobos
  Banking 0.3.0) and `home` (an apartment from a real-estate broker, 0.3.0).
  **`minDownShare`** (default 0.5, from 0.1): for ship and home loans, the least you pay
  down.

Repayment always follows the game's own mortgage schedule, paid in the Finances window
with the game's late fee; only the interest is the lender's. A lender's terms are copied
into each loan when you take it, so changing or removing a lender never changes a loan
already running. A lender whose `home`, `person` or `requires` names a place, person or
story entry the game does not have is left out, with the reason in the log and in
`phobosbank lenders`. Add-ons may add lenders under their own id prefix.

Each lender also leaves story flags a story pack can react to: `bank-<id>-borrowed` once
you borrow, `bank-<id>-repaid` once a loan from it is repaid, and `bank-<id>-late` while
any of its bills is late.
