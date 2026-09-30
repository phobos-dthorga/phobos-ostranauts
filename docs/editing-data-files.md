# Editing the Phobos data files

Since Phobos Framework 0.49.0, some of the numbers behind the Phobos mods live in
plain text files you can read and change: prices, how long a job takes, what a
repair needs, what dismantling returns, which merchants stock what and how often.
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

## Seeing what happened

A skipped file is written to the BepInEx log with the reason, and `phobosframework
status` on the F3 console lists every data pack with how many of your files were
applied and which were skipped. Files that passed still apply when a later one is
skipped. Changes take effect on the next game load; nothing reloads live.

## What it does to saved games

Prices, work and merchant odds change for future work and future restocks; an
existing job keeps the work it started with, and a shop keeps what it already
holds. Changing a store's capacity or a machine's weight is not offered in these
files yet; where a later pack allows it, an existing machine will show as needing
attention with an Accept button rather than silently changing its contents. A
charge already running keeps the recipe revision it started with.

## Which packs exist

| Mod | Schema | What it holds |
| --- | --- | --- |
| Phobos Manufacturing 0.11.0 | `economy` | Machine and store prices, work, repair bills, salvage, offers, lots, regional factors, world loot |
| Phobos Manufacturing 0.12.0 | `process-recipes` | The six V4 charges: inputs, products, off-gas, seconds |
| Phobos Manufacturing 0.12.0 | `materials` | Ingots, carbon stock, remainders and mined chunks: mass, price, stack, category |
| Phobos Shipbreaker 0.46.0 | `process-recipes` | The F6 furnace recipes and thermal profiles, the T2 thaw recipes, the R4 budget |
| Phobos Shipbreaker 0.46.0 | `materials` | Housing stock, ingots, remainders and the reject packets |

More packs (recipes, materials, vessels, loot) follow as the other mods move
their tables over; this page lists them as they land.
