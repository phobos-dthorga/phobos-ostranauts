#!/usr/bin/env python3
"""Write the JSON Schema files in schemas/ for the Phobos data packs.

The schemas give editors completion and inline errors for the shipped packs and for
player override files. They mirror the field sets and ranges of
scripts/validate-data-packs.py, which mirrors the Framework validators; the C#
loader remains authoritative at runtime. Cross-entry rules (mass conservation,
salvage weight, known ids, frozen revisions) are not expressible here and stay in
the validators. Run with --check to verify the files on disk are current.

Usage: write-json-schemas.py [--check]
"""
import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'schemas'
DRAFT = 'http://json-schema.org/draft-07/schema#'
REGIONS = ['BCER', 'BCRS', 'EJDR', 'HQCH', 'JATL', 'JFTS', 'JPTN', 'MHNG', 'MLAB', 'MSUZ', 'MTRS', 'MVOL', 'OFLT', 'OKLG', 'SVIR', 'VCBR', 'VENC', 'VNCA', 'VORB']
CONDITIONS = ['Pristine', 'Refurbished', 'Worn', 'Broken']
SPECIES = ['CH4', 'CO', 'CO2', 'H2SO4', 'N2', 'NH3', 'O2', 'Smoke']


def num(minimum=None, maximum=None, exclusive_minimum=None, description=None, integer=False):
    s = {'type': 'integer' if integer else 'number'}
    if minimum is not None: s['minimum'] = minimum
    if exclusive_minimum is not None: s['exclusiveMinimum'] = exclusive_minimum
    if maximum is not None: s['maximum'] = maximum
    if description: s['description'] = description
    return s


def obj(properties, required=(), description=None, extra=None):
    s = {'type': 'object', 'additionalProperties': False, 'properties': properties}
    if required: s['required'] = list(required)
    if description: s['description'] = description
    if extra: s.update(extra)
    return s


def string(description=None, enum=None, pattern=None):
    s = {'type': 'string'}
    if description: s['description'] = description
    if enum: s['enum'] = enum
    if pattern: s['pattern'] = pattern
    return s


def named(value_schema, description=None, key_pattern=None):
    s = {'type': 'object', 'additionalProperties': False, 'patternProperties': {key_pattern or '^.+$': value_schema}}
    if description: s['description'] = description
    return s


NOTES = string('Free text for maintainers; ignored by the game.')
BILL = named(num(0, 10000, integer=True), 'Material counts by item id.')
HEADER = {
    'priority': {'type': 'integer', 'minimum': -100, 'maximum': 100, 'description': 'Override files only: files apply lowest first (default 0), so a higher number has a later word. Leave it out unless you need it.'},
    'schemaVersion': {'type': 'integer', 'const': 1},
    'schema': string('The schema name; a player file may repeat it.'),
    'notes': NOTES,
}


def header(name):
    h = dict(HEADER); h['schema'] = {'type': 'string', 'const': name}; return h


def economy():
    work = obj({
        'install': num(0, integer=True, description='Zero keeps the definition\'s own value.'),
        'uninstall': num(0, integer=True, description='Zero keeps the definition\'s own value.'),
        'repair': num(0, integer=True, description='Positive unless the entry is a section.'),
        'dismantle': num(0, integer=True, exclusive_minimum=0),
    }, description='Native work-progress targets, not seconds.')
    equipment = obj({
        'notes': NOTES,
        'kind': string('equipment: a machine with the four forms; section: an assembly part sold whole.', ['equipment', 'section']),
        'forms': string('machine: Installed, Loose, InstalledDmg, LooseDmg under the key; item: the key and the key plus Dmg; single: the key alone.', ['machine', 'item', 'single']),
        'price': num(0, exclusive_minimum=0),
        'brokenPrice': num(0, exclusive_minimum=0, description='Below price; a quarter of price when omitted.'),
        'work': work,
        'repairBill': BILL, 'salvage': BILL, 'brokenSalvage': BILL,
        'restoreMinutes': num(0, integer=True, description='Zero keeps the native Restore pace.'),
        'internalBin': string('Internal compartment emptied before dismantling.'),
        'loot': {'type': 'boolean'}, 'salvageValueHigh': {'type': 'boolean'}, 'offers': {'type': 'boolean'},
        'offerScale': num(0, 4, exclusive_minimum=0, description='Multiplies template and regional offer chances.'),
        'regionalChance': num(0, 1, description='Regional offer chance before the region factor; the regional base chance when omitted.'),
        'salvageRemainder': {'type': 'boolean', 'description': 'The mod adds a remainder carrying what the salvage leaves of the mass.'},
        'lot': string('A name in the lots table; the kind when omitted.'),
        'floor': string('A name in the chanceFloors table; equipment when omitted.'),
    }, ['price', 'work'])
    supply = obj({
        'notes': NOTES, 'kind': string(enum=['supplies']),
        'price': num(0, exclusive_minimum=0),
        'repairWork': num(0, integer=True, exclusive_minimum=0), 'dismantleWork': num(0, integer=True, exclusive_minimum=0),
        'repairBill': BILL, 'remainder': string('Retained remainder returned by dismantling.'),
        'merchants': {'type': 'array', 'minItems': 1, 'items': string()},
        'chance': num(0, 1, description='Offer chance before the floor; 0 leaves the floor to decide.'),
        'lot': string(), 'floor': string(), 'expanded': {'type': 'boolean'},
        'regionalChance': num(0, 1), 'regionalCondition': string(enum=['regional'] + CONDITIONS),
    }, ['price', 'repairWork', 'dismantleWork', 'repairBill', 'merchants'])
    template = obj({'notes': NOTES, 'merchant': string(), 'tag': string(pattern='^[A-Za-z0-9]+$'), 'form': string(enum=['Loose', 'LooseDmg']),
                    'condition': string(enum=CONDITIONS), 'chance': num(0, 1, exclusive_minimum=0)}, ['merchant', 'tag', 'chance'])
    offer = obj({'notes': NOTES, 'merchant': string(), 'item': string(), 'condition': string(enum=CONDITIONS), 'chance': num(0, 1, exclusive_minimum=0),
                 'quantity': num(1, 256, integer=True), 'lot': string(), 'floor': string()}, ['merchant', 'item', 'chance'])
    regional_item = obj({'notes': NOTES, 'chance': num(0, 1, exclusive_minimum=0), 'condition': string(enum=CONDITIONS), 'lot': string(), 'floor': string(), 'expanded': {'type': 'boolean'}}, ['chance'])
    regional = obj({'notes': NOTES, 'baseChance': num(0, 1, exclusive_minimum=0), 'refurbished': {'type': 'array', 'items': string(enum=REGIONS)},
                    'expandedMerchants': {'type': 'array', 'items': string()}, 'items': named(regional_item)}, ['baseChance'])
    loot = obj({'notes': NOTES, 'table': string(), 'tables': {'type': 'array', 'items': string()}, 'branch': string(pattern='^Phobos'),
                'chance': num(0, 1, exclusive_minimum=0), 'brokenShare': num(0, 1), 'items': named(num(0, 1, exclusive_minimum=0))}, ['branch'])
    kiosks = obj({'notes': NOTES, 'merchants': {'type': 'array', 'minItems': 1, 'items': string()},
                  'chance': num(0, 1, exclusive_minimum=0, description="Offer chance before the availability floor; 1 when omitted, like the game's own kiosk stock."),
                  'tiers': named(string(enum=['Neutral', 'Warm', 'Friendly', 'Trusted', 'Honored']),
                                 'Reputation tier by equipment key (every saleable size), supply key or item id. Only listed items are sold.')},
                 ['merchants', 'tiers'], "What the game's faction kiosks sell for scrip, and the reputation each item asks.")
    return obj({
        **header('economy'),
        'equipment': named(equipment, 'Machines and sections by definition prefix or item id.'),
        'supplies': named(supply, 'Ordinary supplies by definition prefix.'),
        'offerTemplates': {'type': 'array', 'items': template},
        'offers': named(offer, 'Explicit offers keyed by loot id.', '^Phobos'),
        'regions': named(num(0, 4), 'Availability factor by region code.', '^(' + '|'.join(REGIONS) + ')$'),
        'regional': regional,
        'lots': named(num(1, 256, integer=True), 'Finite lot per successful offer, by lot name.'),
        'chanceFloors': named(num(0, 1), 'Minimum offer probability, by floor name.'),
        'worldLoot': {'type': 'array', 'items': loot},
        'factionKiosks': kiosks,
    }, ['schemaVersion', 'schema', 'equipment'], 'Prices, work, bills, salvage, offers, regional stock, lots, world finds and faction-kiosk tiers of a Phobos mod.')


def recipes():
    unit = obj({'id': string(), 'count': num(1, integer=True), 'kg': num(0, exclusive_minimum=0)}, ['id', 'kg'])
    thermal = obj({'meltK': num(0, exclusive_minimum=0), 'targetK': num(0, exclusive_minimum=0), 'solidCp': num(0, exclusive_minimum=0), 'liquidCp': num(0, exclusive_minimum=0),
                   'latentKJ': num(0), 'holdSeconds': num(0)}, description='Furnace heat profile: melting point and target in kelvin, heat capacities in kJ/(kg K), latent heat in kJ/kg, hold in seconds.')
    recipe = obj({
        'notes': NOTES, 'machine': string(), 'revision': num(1, integer=True),
        'inputs': {'type': 'array', 'minItems': 1, 'items': unit}, 'products': {'type': 'array', 'minItems': 1, 'items': unit},
        'offGas': named(num(0, exclusive_minimum=0), 'Kilograms of a native gas breathed into the room.', '^(' + '|'.join(SPECIES) + ')$'),
        'seconds': num(0, exclusive_minimum=0), 'legacySeconds': num(0, exclusive_minimum=0), 'melt': {'type': 'boolean'},
        'requires': {'type': 'array', 'items': string()}, 'thermal': thermal,
        'circulates': named(num(0, exclusive_minimum=0), 'Working volume of a commodity present during the charge and returned; not in the mass balance.'),
        'reactionKWh': num(-1000, 1000, description='Reaction heat released into the room over the charge (negative when absorbed).'),
        'supersedes': {'type': 'array', 'items': num(1, integer=True), 'description': 'Earlier revisions of the same machine this recipe replaces for new charges; bound jobs still settle by them.'},
    }, ['machine', 'revision', 'inputs', 'products'])
    return obj({**header('process-recipes'), 'recipes': named(recipe, 'Recipes by id; published revisions are frozen by hash.', '^[a-z0-9-]+$')},
               ['schemaVersion', 'schema', 'recipes'], 'Fixed process recipes: mass-conserving inputs, products and native off-gas.')


def materials():
    material = obj({'notes': NOTES, 'kind': string('How the mod builds it (stock, mined, packet, food, waste, ...).'), 'kg': num(0, exclusive_minimum=0),
                    'price': num(0, exclusive_minimum=0), 'stack': num(1, 1000, integer=True), 'side': num(1, 8, integer=True),
                    'category': string('Native market category condition, e.g. IsCategoryMetals.'), 'terminal': {'type': 'boolean'}, 'art': string(),
                    'name': string('Added materials only: the name shown when no translation names it.'), 'description': string('Added materials only.'),
                    'image': {'type': 'string', 'pattern': '^[A-Za-z0-9_-][A-Za-z0-9_/-]*$', 'description': 'Added materials only: the picture, a path under an images folder without .png, such as myaddon/SteelNugget.'}}, ['kg', 'price'])
    return obj({**header('materials'), 'materials': named(material, 'Loose items by definition id.')}, ['schemaVersion', 'schema', 'materials'],
               'Loose items a Phobos mod adds: mass, price, stack, footprint, category and art.')


def vessels():
    family = obj({'notes': NOTES, 'kind': string('gas-store, silo, reservoir, bin or another kind the mod names.'), 'commodity': string(),
                  'capacityKg': num(0, 100000, exclusive_minimum=0), 'dryKg': num(0, 10000, exclusive_minimum=0), 'leakKgPerHour': num(0, 1000),
                  'cellsPerTileSide': num(1, 8, integer=True, description='Bins only.')}, ['dryKg'])
    return obj({**header('vessels'), 'families': named(family, 'Small size of each vessel or bin family by definition prefix.')}, ['schemaVersion', 'schema', 'families'],
               'Capacity, dry mass and leak rate of each bulk vessel family; larger sizes follow the Framework ladder.')



def equipment():
    entry = obj({'notes': NOTES, 'kind': string('How the mod builds it (for example charge-machine).'), 'footprint': num(1, 12, integer=True),
                 'massKg': num(0, 100000, exclusive_minimum=0), 'idleKW': num(0), 'workingKW': num(0, 10000, exclusive_minimum=0),
                 'roomHeatFraction': num(0, 1), 'feedCells': num(0, 64, integer=True), 'art': string(), 'installTab': string('A native INSTALL tab, for example APPS.'),
                 'points': named({'type': 'array', 'minItems': 2, 'items': num(-512, 512, integer=True)}, 'Named map points in pixels from the centre.')},
                ['footprint', 'massKg', 'workingKW'])
    return obj({**header('equipment'), 'equipment': named(entry, 'Machine shapes by definition prefix; a read-only reference for now.')},
               ['schemaVersion', 'schema', 'equipment'], 'Footprint, mass, power, heat share, feed cells, art and use points of each machine.')

def crops():
    kg = lambda text: num(0, exclusive_minimum=0, description=text)
    crop = obj({'notes': NOTES, 'name': string('Plain name for a crop you add; shipped crops use the translation files.'), 'hours': num(None, 10000, exclusive_minimum=0, description='Lit hours to harvest at ordinary pace.'),
                'kw': num(None, 1.5, exclusive_minimum=0, description='Rack electrical demand while growing.'),
                'seedKg': kg('Planted mass; one unit of the stock item.'), 'finalKg': kg('Harvest-ready mass of the cohort.'),
                'carbonKg': kg('Carbon fixed over the cycle, as CH2O.'), 'nutrientKg': kg('Nutrient taken up.'), 'waterKg': kg('Water taken up.'),
                'vapourKg': num(0, description='Water transpired.'), 'seedCarbonKg': num(0, description='Carbon the planting stock already holds.'),
                'edibleKg': kg('Edible mass of a full healthy cohort.'), 'keptStockKg': num(0, description='Planting stock kept back at harvest: zero or one planting.'),
                'portionKg': kg('One portion; one unit of the produce item.'),
                'picks': num(0, 10, integer=True, description='Picks a ripe plant allows before its final harvest; absent for a crop harvested once.'),
                'pickKg': num(0, description='Most fruit one pick takes, in whole portions.'), 'stock': string('The item planted.'), 'produce': string('The item each portion becomes.'),
                'feed': string('Old name, kept to read old saves: the feed a W2 mixed for this crop until Agriculture 0.54.0. Leave out for a new crop.'),
                'feedCommodity': string('Old name, kept to read old saves: what that feed was held under in conduit. Leave out for a new crop.'),
                'art': string('Artwork family of the growth stages; a crop that ships with the mod.', pattern='^[A-Za-z0-9]+$')},
               ['hours', 'kw', 'seedKg', 'finalKg', 'carbonKg', 'nutrientKg', 'waterKg', 'vapourKg', 'seedCarbonKg', 'edibleKg', 'keptStockKg', 'portionKg',
                'stock', 'produce', 'art'],
               'Mass must close: waterKg + nutrientKg + 0.4 x carbonKg - vapourKg = finalKg - seedKg.')
    item = obj({'notes': NOTES, 'text': string('Translation key of the item name; its description is the key plus _desc. Left out for an item a file adds to the materials pack, which is named there.'),
                'hunger': num(1, 20, integer=True), 'satiety': num(1, 20, integer=True)}, [])
    response = obj({'notes': NOTES, 'points': {'type': 'array', 'minItems': 1, 'maxItems': 16, 'items': {'type': 'array', 'minItems': 2, 'maxItems': 2,
                    'prefixItems': [num(0, 10, description='CO2 partial pressure, kPa'), num(0.5, 2, description='Growth factor')]},
                    'description': 'Points of [kPa, factor] in rising pressure; interpolated, ends held.'}}, ['points'],
                   'How room carbon dioxide speeds growth per hour and per kWh; budgets per kilogram are unchanged.')
    room = obj({'notes': NOTES, 'minC': num(-50, 100, description='Coldest room the crop grows in, C.'), 'maxC': num(-50, 100, description='Warmest room the crop grows in, C.'),
                'minKPa': num(0, 500, exclusive_minimum=0, description='Lowest pressure, kPa.'), 'maxKPa': num(0, 500, exclusive_minimum=0, description='Highest pressure, kPa.')}, [],
               'A growing room. Each limit may be left out; a lower limit must stay below its upper one.')
    stress = obj({'notes': NOTES, 'graceHours': num(0, 1000, description='Hours of stress a crop shrugs off.'),
                  'healthLossPerHour': num(0, 1, description='Health lost per further stressed hour in a room that suits the crop.'),
                  'healthLossPerHourOutside': num(0, 1, description='Health lost per further hour in a room outside its limits.'),
                  'healthLossPerHourNoAir': num(0, 1, description='Health lost per hour in a room with no air.'),
                  'plantWaterKg': num(0, 20, description='Water a rack needs before a crew order plants in it, kg.')}, [])
    misting = obj({'notes': NOTES, 'maxCoolingC': num(0, 15, description='How far misting can cool a crop below a room too hot for it, C.'),
                   'waterKgPerHourPerC': num(0, 5, exclusive_minimum=0, description='Water misting takes per hour for each degree it covers, kg.'),
                   'damageShareBeyond': num(0, 1, description='Share of heat damage left while misting a room hotter than it can cover.'),
                   'reserveKg': num(0, 20, description='Reservoir water misting never uses, kg.')}, [],
                  'Misting a crop in a room too hot for it, switched on per rack. Every figure is optional.')
    crop_misting = obj({'notes': NOTES, 'maxCoolingC': num(0, 15, description='This crop\'s own misting limit, C.')}, [])
    growth = obj({'notes': NOTES, 'room': room, 'stress': stress, 'misting': misting,
                  'nutrientTargetKg': num(0, 0.5, exclusive_minimum=0, description='Nutrient a W2 keeps in each rack it feeds, kg.'),
                  'feedStrengthKgPerKg': num(0, 1, exclusive_minimum=0, description='Nutrient each kilogram of water the pump moves can carry, kg.'),
                  'crops': named(obj({'notes': NOTES, 'room': room, 'misting': crop_misting}, []), 'One crop\'s own room and misting limit, by crop name; only what is given replaces the shared figures.', '^[a-z0-9-]+$')}, [],
                 'Where and how crops grow, and how a W2 feeds them. Every figure is optional.')
    return obj({**header('crops'), 'co2Response': response, 'growth': growth, 'crops': named(crop, 'Crops by the name a saved planting stores. A published crop is frozen: add a crop beside it.', '^[a-z0-9-]+$'),
                'items': named(item, 'The crop items the mod builds, by definition id.')}, ['schemaVersion', 'schema', 'crops', 'items'],
               'What a Firstlight rack grows: budgets, harvest, items and artwork of each crop, and the room they grow in.')


def care():
    station = obj({'notes': NOTES, 'idleKW': num(0, 2, description='Demand with nobody under care, kW; no more than workingKW.'),
                   'workingKW': num(None, 2, exclusive_minimum=0, description='Demand while giving care, kW.')})
    admission = obj({'notes': NOTES,
                     'bloodLost': num(None, 40, exclusive_minimum=0, description='Blood lost at which a person counts as injured (the game: 40 is fatal); below 40.'),
                     'infection': num(None, 95, exclusive_minimum=0, description='Infection at which a person counts as injured (the game: 95 is fatal); below 95.'),
                     'pain': num(None, 75, exclusive_minimum=0, description='Pain at which a person counts as injured (the game: 75 knocks out); below 75.'),
                     'wound': num(None, 1, exclusive_minimum=0, description='Worst wound cut or blunt damage, 0 to 1, at which a person counts as injured; below 1.'),
                     'dischargeShare': num(0, 1, description='A resting patient gets up below every threshold times this share; below 1.')},
                    description='Who counts as injured, and when a resting patient has recovered.')
    treatment = obj({'notes': NOTES, 'test': string('Which wounds it is for.', enum=['bleeding', 'fracture', 'spent-dressing']),
                     'effect': string('slot-item: the item goes onto the wound the game\'s own way.', enum=['slot-item']),
                     'item': string('The game item definition used up, one per treatment.', pattern='^[A-Za-z0-9_]{1,48}$'),
                     'medicSeconds': num(5, 1800, description='How long a medic takes, in seconds; crew with the skill are quicker.'),
                     'skill': string('A skill condition, such as SkillMedicalTrauma; crew with it are asked first. Leave out for anyone.', pattern='^[A-Za-z0-9_]{0,48}$'),
                     'order': num(0, 1000, integer=True, description='Lower comes first; ties go by name.')},
                    ['test', 'item', 'medicSeconds'])
    level = obj({'notes': NOTES, 'weightlessHealing': num(0.05, 1, description="Share of normal wound healing a weightless patient keeps under care: 0.05 is the game's own, 1 removes the penalty.")})
    return obj({**header('care'), 'stations': named(station, 'Electrical demand by station: bed, monitor.', '^(bed|monitor)$'), 'admission': admission,
                'alerts': obj({'notes': NOTES, 'bloodLost': num(None, 40, exclusive_minimum=0), 'infection': num(None, 95, exclusive_minimum=0),
                               'pain': num(None, 75, exclusive_minimum=0)}, description='When a Vigil-2 posts a caution (Medical 0.3.0); a bleeding wound always alerts.'),
                'levels': named(level, "What a station adds to the game's own care (Medical 0.2.0).", '^(bed)$'),
                'treatments': named(treatment, 'What a medic does for a patient in a Ward-3, by name (Medical 0.4.0).', '^[A-Za-z0-9_-]{1,48}$')},
               ['schemaVersion', 'schema', 'stations', 'admission'],
               'Phobos Medical care pack: station power and admission thresholds. The healing is the game\'s own Recuperating.')


def outcomes():
    table = obj({'notes': NOTES, 'outcomes': named(num(0, 10000, integer=True, description='Weight: the outcome is picked this often out of the table total. 0 switches it off.'),
                                                   'Weight by outcome recipe id. The base recipe is one of its own outcomes.', '^[A-Za-z0-9-]+$')}, ['outcomes'])
    return obj({**header('outcomes'), 'tables': named(table, 'Tables by base recipe id: the recipe a player chooses or a machine matches.', '^[A-Za-z0-9-]+$')},
               ['schemaVersion', 'schema', 'tables'],
               'Chance tables: the recipes a charge may turn out to be, and the odds. Every outcome is an ordinary recipe of the same mod with the same charge and duration as its base.')


def lines():
    name = {'type': 'string', 'pattern': '^[A-Za-z][A-Za-z0-9]{0,63}$'}
    support = obj({'notes': NOTES, 'tile': {**name, 'description': 'The condition the tile must carry, such as IsFloor or IsWall.'},
                   'object': {**name, 'description': 'What must stand there, installed and intact: the word floor for any of the game\'s floors, or a condition the object carries, such as IsWall.'}},
                  ['tile', 'object'])
    rule = obj({'notes': NOTES,
                'forbiddenTiles': {'type': 'array', 'items': name, 'maxItems': 16, 'uniqueItems': True, 'description': 'Tile conditions on which a segment never counts.'},
                'supports': {'type': 'array', 'items': support, 'minItems': 1, 'maxItems': 8, 'description': 'What holds a segment up. One match is enough.'}},
               ['supports'])
    return obj({**header('lines'), 'placement': named(rule, 'Rules by name: default, or a line family id such as PhobosFramework.ProcessWater.')},
               ['schemaVersion', 'schema', 'placement'],
               'Where a pipe or conduit segment counts as laid: the tiles that carry nothing, and what must stand on a tile for a segment there to join its line.')


def stores():
    name = {'type': 'string', 'pattern': '^[A-Za-z][A-Za-z0-9]{0,63}$'}
    container = {'type': 'string', 'pattern': '^[A-Za-z][A-Za-z0-9]{0,63}$|^[A-Za-z][A-Za-z0-9]{2,63}[*]$'}
    return obj({**header('stores'),
                'requireInteraction': {'type': 'string', 'pattern': '^([A-Za-z][A-Za-z0-9]{0,63})?$', 'description': 'The interaction a container must offer to count as a store, such as the game\'s Inventory; empty for any.'},
                'excludeConditions': {'type': 'array', 'items': name, 'maxItems': 48, 'uniqueItems': True, 'description': 'Conditions that mark an object as not a store, such as IsShipWeapon or IsToilet01.'},
                'excludeContainers': {'type': 'array', 'items': container, 'maxItems': 48, 'uniqueItems': True, 'description': 'Container rules that hold only one kind of thing, such as TIsFitAmmo20mm; a name ending in * covers every rule that starts with the rest.'}},
               ['schemaVersion', 'schema'],
               'Which of the game\'s containers are not stores: weapons, chargers, filter holders and toilets are left out of store pickers, crew orders and housekeeping, and crew never take from them.')


def story():
    story_id = '^[a-z0-9]+(-[a-z0-9]+)*$'
    game = {'type': 'string', 'pattern': '^[A-Za-z0-9_]+$'}
    station = {'type': 'string', 'pattern': '^(any|[A-Za-z0-9_|-]{1,32})$', 'description': 'A station registration id such as OKLG (its parts, such as VORB_HAB, count too), or any for any station.'}
    plain = ('Plain text: no angle brackets, and no square brackets except the placeholders [player], [player-first], [ship], [place], [region], '
             '[station], [body], [date], [crew] and [person:key].')
    tier = string('One of the game\'s standing tiers.', ['dislikes', 'neutral', 'warm', 'friendly', 'trusted', 'honored'])
    standing = obj({'faction': {**game, 'description': 'The game\'s faction name, such as OKLGCorp.'}, 'atLeast': tier, 'atMost': tier}, ['faction'],
                   'How a faction regards the player: at least and/or at most a tier.', extra={'anyOf': [{'required': ['atLeast']}, {'required': ['atMost']}]})
    key = {'type': 'string', 'pattern': story_id, 'maxLength': 48}
    flags = {'type': 'array', 'maxItems': 4, 'uniqueItems': True, 'items': key}
    def text(limit, description):
        return {'type': 'string', 'minLength': 1, 'maxLength': limit, 'description': description + ' ' + plain}
    def names(item, description):
        return {'type': 'array', 'items': item, 'maxItems': 16, 'description': description}
    requires = obj({
        'mods': names({'type': 'string', 'pattern': '^(Phobos[A-Za-z]+|[A-Za-z0-9_-]+(\\.[A-Za-z0-9_-]+)+)$'}, 'Mods that must be installed: a Phobos mod folder name such as PhobosManufacturing, or a BepInEx plugin id.'),
        'playerConditions': names(game, 'Game conditions the player must have.'),
        'forbidConditions': names(game, 'Game conditions the player must not have.'),
        'owns': names(game, "Item definition ids that must be on one of the player's ships, such as PhobosVerdemorrowFirstlight4Installed."),
        'dockedAt': names(station, 'Docked at or aboard any one of these stations.'),
        'arcsDone': names({'type': 'string', 'pattern': story_id}, 'Arcs the player must have finished.'),
        'arcsNotStarted': names({'type': 'string', 'pattern': story_id}, 'Arcs the player must never have started.'),
        'afterDays': num(0, 3650, description="Only once this many game days have passed since the player's story record began."),
        'beforeDays': num(0, 3650, description="Only until this many game days have passed since the player's story record began."),
        'filesRead': names({'type': 'string', 'pattern': story_id}, 'Story data files the player must have opened.'),
        'flags': names(key, 'Story flags that must all be set (an arc outcome sets them).'),
        'notFlags': names(key, 'Story flags none of which may be set.'),
        'arcsActive': names(key, 'Arcs that must be under way.'),
        'arcsAtStep': names({'type': 'string', 'pattern': '^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*$'}, 'Arcs under way at a step, as arc.step.'),
        'places': names(key, 'Places the player must be at (any one): docked at it, or anywhere in its region for a regional place.'),
        'regions': names(key, 'Regional places the player must be in the region of (any one).'),
        'newsSeen': names(key, 'News items that must have been shown on a TV.'),
        'standing': {'type': 'array', 'maxItems': 16, 'items': standing, 'description': 'How the game\'s factions must regard the player; each must hold.'},
        'crewWith': names(game, 'Game conditions (skills among them) that someone aboard other than the player must have.'),
        'crewCount': obj({'atLeast': num(0, 50, integer=True), 'atMost': num(0, 50, integer=True)}, description='How many crew the player has, the player not counted.'),
        'running': names(game, 'Phobos machines, by installed definition id, that must be running on one of the player\'s ships.'),
        'months': {'type': 'array', 'maxItems': 12, 'uniqueItems': True, 'items': num(1, 12, integer=True), 'description': 'Calendar months the entry is for.'},
        'hours': obj({'from': num(0, 23, integer=True), 'to': num(0, 23, integer=True)}, ['from', 'to'], 'A window of the day in UTC hours; from after to wraps midnight.')},
        description='When the entry may appear. Every part is optional and every part given must hold.')
    thread_ref = {**key, 'description': 'The thread this entry belongs to: it inherits the thread\'s place and requirements.'}
    place_ref = {**key, 'description': 'The place this entry belongs to, by key (phobosframework story places lists them).'}
    message = obj({'from': {'type': 'string', 'minLength': 1, 'maxLength': 40, 'description': 'Who it is from, shown before the text in the crew log; leave out when person names them.'},
                   'person': {**key, 'description': 'The person it is from, shown as Name, role.'},
                   'text': text(400, 'The message, shown in the crew log.')}, ['text'], extra={'anyOf': [{'required': ['from']}, {'required': ['person']}]})
    test = obj({'kind': string('What the step waits for.', ['dock-at', 'have-item', 'install', 'wait', 'credits', 'condition']),
                'station': {**station, 'description': 'dock-at only. ' + station['description'] + ' Left out, the arc\'s own place.'},
                'item': {**game, 'description': 'have-item and install only: an item definition id.'},
                'count': num(1, 100, integer=True, description='have-item and install only: how many (default 1).'),
                'consume': {'type': 'boolean', 'description': 'have-item only: the items are taken from the player when the step finishes.'},
                'hours': num(exclusive_minimum=0, maximum=720, description='wait only: game hours since the step began.'),
                'amount': num(exclusive_minimum=0, maximum=1000000, description='credits only: how many credits the player holds (taken when the step finishes, with consume).'),
                'condition': {**game, 'description': 'condition only: a game condition the player has, such as a skill (SkillHacking).'}}, ['kind'])
    outcome = obj({'message': message,
                   'items': {'type': 'array', 'maxItems': 5, 'items': obj({'item': game, 'count': num(1, 20, integer=True)}, ['item']),
                             'description': 'Items given to the player, or put at their feet when they cannot carry them.'},
                   'credits': num(0, 50000, integer=True, description="Credits paid to the player, entered in the game's ledger."),
                   'files': {'type': 'array', 'maxItems': 5, 'uniqueItems': True, 'items': {'type': 'string', 'pattern': story_id},
                             'description': 'Story data files given to the player on one data card.'},
                   'setFlags': {**flags, 'description': 'Story flags set on the player\'s record, for other entries\' requirements.'},
                   'clearFlags': {**flags, 'description': 'Story flags cleared.'},
                   'standing': {'type': 'array', 'maxItems': 2, 'items': obj({'faction': game, 'change': num(-10, 10, description='Points added to the faction\'s view of the player, not 0; a tier is 25.')}, ['faction', 'change']),
                                'description': 'Small changes to how factions regard the player, through the game\'s own scores.'}},
                  description='What happens when the step finishes.')
    tests = {'type': 'array', 'items': test, 'minItems': 1, 'maxItems': 4, 'description': 'All must pass for the step to finish.'}
    next_step = {'type': 'string', 'pattern': '^(end|[a-z0-9]+(-[a-z0-9]+)*)$', 'description': 'A step id of the same arc, or end.'}
    branch = obj({'notes': NOTES, 'tests': tests, 'onComplete': outcome, 'next': next_step}, ['tests', 'next'],
                 'Another way the step can finish: the first branch whose tests all pass decides, after the step\'s own tests.')
    choice = obj({'id': {'type': 'string', 'pattern': story_id, 'maxLength': 32, 'description': 'Unique within the step; saved with the player\'s answer, so keep it once published.'},
                  'label': text(60, 'The reply as the player sees it on its button.'), 'notes': NOTES,
                  'tests': {'type': 'array', 'items': test, 'maxItems': 4, 'description': 'What the reply needs before it can be sent; shown locked, with the reason, until all pass.'},
                  'onComplete': outcome, 'next': next_step}, ['id', 'label', 'next'],
                 'A reply the player may send from the Letters window (Framework 0.122.0).')
    step = obj({
        'id': {'type': 'string', 'pattern': story_id, 'maxLength': 32, 'description': 'Unique within the arc; it names the goal in saves, so keep it once published.'},
        'delivery': obj({'message': message, 'bulletin': {'type': 'string', 'pattern': story_id, 'description': 'A broadcast id that the next TV news item shows.'}},
                        description='What the player is told when the step begins.'),
        'objective': obj({'title': text(60, 'The goal title in the GOALS list.'), 'description': {**text(300, 'The goal description.'), 'minLength': 0},
                          'person': {**key, 'description': 'Whose face the goal shows and who it says it is from; left out, the sender of the step\'s letter, else the arc\'s last sender.'}}, ['title'],
                         'A goal in the GOALS list. A step without one waits on its tests unseen.'),
        'tests': tests,
        'onComplete': outcome,
        'next': {**next_step, 'description': 'The step that follows: a step id of the same arc, or end. By default the next in order.'},
        'branches': {'type': 'array', 'items': branch, 'minItems': 1, 'maxItems': 4},
        'choices': {'type': 'array', 'items': choice, 'minItems': 2, 'maxItems': 4,
                    'description': 'Replies the player chooses between in the Letters window; a step with replies has no tests or branches.'}}, ['id'],
        extra={'oneOf': [{'required': ['tests'], 'not': {'required': ['choices']}}, {'required': ['choices'], 'not': {'anyOf': [{'required': ['tests']}, {'required': ['branches']}]}}]})
    weight = num(1, 100, integer=True, description='How often it is picked against other story entries (default 1).')
    once = {'type': 'boolean', 'description': 'Shown once in a save, then never again.'}
    author = {'title': {'type': 'string', 'maxLength': 80, 'description': 'For authors; the game does not show it.'}, 'notes': NOTES}
    broadcast = obj({**author, 'region': {'type': 'string', 'minLength': 1, 'maxLength': 40, 'description': 'Shown above the item as Region News, such as Outer System, Tharsis or Shipping & Inner System; left out, the place\'s own.'},
                     'text': text(700, 'The news item.'), 'weight': weight, 'once': once, 'requires': requires,
                     'mention': text(200, 'What people say when they bring this news up in small talk, for a while after it was shown.'),
                     'thread': thread_ref, 'place': place_ref}, ['text'],
                    extra={'anyOf': [{'required': ['region']}, {'required': ['place']}, {'required': ['thread']}]})
    advert = obj({**author, 'text': text(400, 'The advert; a line break may separate a heading.'), 'weight': weight, 'once': once, 'requires': requires,
                  'thread': thread_ref, 'place': place_ref}, ['text'])
    arc = obj({'title': {'type': 'string', 'minLength': 1, 'maxLength': 80, 'description': 'For authors and the F3 list.'}, 'notes': NOTES, 'requires': requires,
               'chance': num(0, 1, description='Chance per story check (every 30 s) that the eligible arc starts by itself, while the player is at its place if it has one; 0 starts it only from F3.'),
               'repeatable': {'type': 'boolean', 'description': 'May start again after it is finished.'},
               'thread': thread_ref, 'place': {**place_ref, 'description': place_ref['description'] + ' A dock-at test without a station means this place.'},
               'steps': {'type': 'array', 'items': step, 'minItems': 1, 'maxItems': 12}}, ['title', 'steps'])
    lore_note = ' Shown with no player at hand: plain text without placeholders.'
    lore = lambda limit, description: {'type': 'string', 'minLength': 1, 'maxLength': limit, 'description': description + lore_note}
    mods_only = obj({'mods': requires['properties']['mods']}, description='Only installed mods can be required: no player exists when this is shown.')
    chatter = obj({**author,
                   'moment': string('Which kind of the game\'s own small talk carries the line.', ['headline', 'joke', 'complaint', 'story', 'jargon', 'superstition', 'worry', 'question', 'small-talk']),
                   'line': text(200, 'What the speaker says, after the moment\'s lead-in.'),
                   'speakers': string('Who may say it: anyone (default), crew (someone aboard one of the player\'s ships), others (anyone else) or locals (others, at the line\'s place).', ['anyone', 'crew', 'others', 'locals']),
                   'weight': weight, 'requires': requires,
                   'thread': thread_ref, 'place': {**place_ref, 'description': place_ref['description'] + ' Crew say the line while the player is there; others only when they are there.'},
                   'speakerFactions': {'type': 'array', 'maxItems': 4, 'items': game, 'description': 'Game faction names the speaker must belong to one of, such as OKLGLEO.'}}, ['moment', 'line'])
    grouping = {**key, 'description': 'The thread this entry belongs to, for authors and the F3 thread report.'}
    tip = obj({**author, 'text': lore(450, 'The lore tip shown while the game loads.'), 'weight': weight, 'requires': mods_only, 'thread': grouping}, ['text'])
    image = {'type': 'string', 'maxLength': 100, 'pattern': '^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$', 'description': "A picture beside the page: a path under a mod's images folder, without .png."}
    section = obj({'notes': NOTES, 'label': lore(40, 'The name in the encyclopedia\'s list.'), 'title': lore(60, 'The heading of its page.'),
                   'body': {**lore(4000, 'Its page text.'), 'minLength': 0}, 'image': image, 'requires': mods_only, 'thread': grouping}, ['label', 'title'])
    article = obj({'notes': NOTES, 'section': {'type': 'string', 'pattern': story_id, 'description': 'A section id from any loaded pack, such as phobos-makers.'},
                   'label': lore(40, 'The name in the encyclopedia\'s list.'), 'title': lore(60, 'The heading of its page.'),
                   'body': lore(4000, 'The article; line breaks separate paragraphs.'), 'image': image, 'requires': mods_only, 'thread': grouping}, ['section', 'label', 'title', 'body'])
    data_file = obj({'notes': NOTES, 'name': {'type': 'string', 'maxLength': 32, 'pattern': '^[A-Za-z0-9_.-]+$', 'description': 'The file name a computer lists, such as TRIAL_NOTES.TXT.'},
                     'text': text(3000, 'What the file says when opened on a computer or PDA.'),
                     'startsArc': {'type': 'string', 'pattern': story_id, 'description': 'An arc that starts when the file is first opened, if it has not started and its requirements hold.'},
                     'thread': thread_ref, 'place': place_ref, 'person': {**key, 'description': 'The person who wrote it.'}},
                    ['name', 'text'])
    place = obj({'notes': NOTES,
                 'station': {'type': 'string', 'pattern': '^[A-Za-z0-9_|-]{1,32}$', 'description': 'The station registration id or prefix (OKLG, VORB_HAB); its parts count as it does.'},
                 'within': {**key, 'description': 'The regional place this one lies within, one level; a place without one is a region of its own.'},
                 'region': {'type': 'string', 'minLength': 1, 'maxLength': 40, 'description': 'The Region News label for news from here; required on a regional place, inherited by its parts.'},
                 'body': {'type': 'string', 'minLength': 1, 'maxLength': 40, 'description': 'The body it orbits or stands on, for the [body] placeholder.'},
                 'factions': {'type': 'array', 'maxItems': 8, 'items': game, 'description': 'The game\'s faction names at home here, for authors.'},
                 'name': {'type': 'string', 'minLength': 1, 'maxLength': 40, 'description': 'What people call it: the [place] placeholder.'}},
                ['station', 'name'], 'A station, or a part of one, that content can belong to. Framework ships the game\'s regional stations.')
    person = obj({'notes': NOTES, 'name': {'type': 'string', 'minLength': 1, 'maxLength': 40}, 'role': {'type': 'string', 'minLength': 1, 'maxLength': 40},
                  'home': {**key, 'description': 'The place they belong to.'}, 'faction': {**game, 'description': 'The game\'s faction name they belong to, for authors.'},
                  'face': string('The look of the face the game makes for them, rolled once per save: masculine, feminine or any (default).', ['any', 'masculine', 'feminine'])},
                 ['name', 'home'], 'A named recurring person, shown as Name, role where a letter names its sender.')
    thread = obj({'title': {'type': 'string', 'minLength': 1, 'maxLength': 80}, 'notes': NOTES,
                  'place': {**key, 'description': 'The place its members belong to unless they name their own.'},
                  'people': {'type': 'array', 'maxItems': 8, 'uniqueItems': True, 'items': key, 'description': 'Its cast: the people its letters may come from. Empty leaves the cast open.'},
                  'requires': {**requires, 'description': 'Requirements every member must also meet.'}},
                 ['title'], 'A story thread: its members declare it and share its place, cast and requirements.')
    settings = obj({'broadcastShare': num(0, 1, description='Share of TV news picks given to story broadcasts.'),
                    'advertShare': num(0, 1, description='Share of TV advert picks given to story adverts.'),
                    'checkSeconds': num(5, 600, description='Real seconds between story checks.'),
                    'maxActiveArcs': num(0, 10, integer=True, description='How many arcs may start by themselves at once.'),
                    'chatterShare': num(0, 1, description='Share of matching small talk that uses a story line when one fits.'),
                    'tipShare': num(0, 1, description='Share of loading-screen tips taken from story tips.'),
                    'localWeight': num(0, 100, description='How much more often news and adverts of the place the player is at are picked (default 4; unplaced entries count 2).'),
                    'farWeight': num(0, 100, description='How much news and adverts of other places weigh (default 1; 0 hides them).'),
                    'mentionDays': num(0, 365, description='Game days after a news item was shown during which people still mention it (default 10).')},
                   description="Framework's own story pack only.")
    return obj({**header('story'), 'settings': settings,
                'broadcasts': named(broadcast, 'TV news items by id.', story_id), 'adverts': named(advert, 'TV adverts by id.', story_id),
                'arcs': named(arc, 'Story arcs by id.', story_id),
                'chatter': named(chatter, 'Lines people may say in the game\'s own small talk, by id.', story_id),
                'tips': named(tip, 'Lore tips for loading screens, by id.', story_id),
                'sections': named(section, 'Top-level encyclopedia entries, by id; shown while one of their articles is.', story_id),
                'articles': named(article, 'Encyclopedia articles, by id.', story_id),
                'files': named(data_file, 'Data files, by id: given on a data card and read on any computer or PDA.', story_id),
                'places': named(place, 'Places content belongs to, by key.', story_id),
                'people': named(person, 'Named recurring people, by key.', story_id),
                'threads': named(thread, 'Story threads, by key.', story_id)},
               ['schemaVersion', 'schema'],
               'Story content: TV news, adverts, arcs whose goals appear in the GOALS list, small talk, loading tips, encyclopedia articles and data files, grounded in places, people and threads. See docs/writing-story-content.md.')


def addon():
    return obj({'schemaVersion': {'type': 'integer', 'const': 1},
                'id': {'type': 'string', 'pattern': '^[a-z0-9-]{3,48}$', 'description': "The add-on's own short id."},
                'name': string('The name players see in the add-on list.'), 'author': string('Who made it.'),
                'version': {'type': 'string', 'pattern': '^[0-9]+(\\.[0-9]+){1,3}$', 'description': 'Such as 1.0.0.'},
                'idPrefix': {'type': 'string', 'pattern': '^[A-Za-z][A-Za-z0-9]{2,23}$', 'description': 'The start of every id the add-on adds. Not Phobos, Itm, Sys, Stat or Is.'},
                'requires': named({'type': 'string', 'pattern': '^[0-9]+(\\.[0-9]+){1,3}$'}, 'Phobos mod folder names and the lowest version of each the files need, such as PhobosManufacturing: 0.44.0.'),
                'notes': NOTES},
               ['schemaVersion', 'id', 'name', 'author', 'version', 'idPrefix'],
               'The manifest of a Phobos add-on (phobos-addon.json), beside the mod_info.json the game itself reads.')


def upkeep():
    family = obj({'notes': NOTES, 'gainShare': num(0, 1, description='This family\'s share of the player\'s MaxTuningGain setting (default 1); 0 leaves it untuned.')})
    return obj({**header('upkeep'),
                'tuneStep': num(0.01, 1, description='How much of a full tune one session adds (default 0.2).'),
                'tuneStepSkilled': num(0.01, 1, description='The same for a crew member skilled at the machine (default 0.3); never less than tuneStep.'),
                'inspectionValidHours': num(1, 240, description='Game hours an inspection stays good for (default 24).'),
                'inspectedFadeShare': num(0, 1, description='Share of the ordinary fade an inspected machine\'s tune takes (default 0.5).'),
                'practiceMinutes': num(2, 60, description='Game minutes of one practice session at a machine (default 10).'),
                'families': named(family, 'Machine families by key, such as manufacturing.charge; phobosframework upkeep lists them.', '^.{1,64}$')},
               ['schemaVersion', 'schema'],
               'Crew upkeep: what a tuning session adds, how long an inspection is good, and each machine family\'s share of the gain.')


def lenders():
    lender = obj({
        'notes': NOTES,
        'name': string('The name shown in the app and on the ledger (the creditor): 1 to 40 characters, without | , = # [ ] < >.'),
        'pitch': string("The lender's own words to a customer, in their sales voice; at most 400 characters, no placeholders."),
        'accredited': {'type': 'boolean', 'description': 'A registered lender (lower rates, asks for standing) or a quick-money one.'},
        'home': string('The story place the lender trades from (a key of Framework places): a regional place lends in its region, a part of one only to a player docked there.', pattern='^[a-z0-9]+(-[a-z0-9]+)*$'),
        'person': string('The story person who speaks for the lender; optional.', pattern='^[a-z0-9]+(-[a-z0-9]+)*$'),
        'requires': {'type': 'object', 'description': 'Who may borrow: a story requires block, checked as story content is (see the story schema).'},
        'ratePerShift': num(None, 0.01, exclusive_minimum=0, description='Interest per shift on the balance still owed, as a share (0.0003 is 0.03%).'),
        'minPrincipal': num(1000, 5000000, description='The smallest loan, in credits.'),
        'maxPrincipal': num(1000, 5000000, description='The most the player may owe this lender at once, in credits; above minPrincipal.'),
        'maxLoans': num(1, 5, integer=True, description='How many loans from this lender may run at once (default 1).'),
        'offers': {'type': 'array', 'minItems': 1, 'uniqueItems': True, 'items': string(enum=['cash', 'ship', 'home']),
                   'description': 'What it lends for: cash, ship (a ship broker purchase) or home (an apartment from a real-estate broker).'},
        'minDownShare': num(0.1, 1, description='For ship and home loans: the least paid down, as a share of the price (default 0.5).'),
    }, required=('name', 'pitch', 'home', 'ratePerShift', 'minPrincipal', 'maxPrincipal', 'offers'))
    line = obj({
        'notes': NOTES,
        'name': string('The service name shown in the app and on the ledger: 1 to 40 characters, without | , = # [ ] < >.'),
        'pitch': string("The service's own words to a customer; at most 400 characters, no placeholders."),
        'person': string('The story person who speaks for the service; optional.', pattern='^[a-z0-9]+(-[a-z0-9]+)*$'),
        'requires': {'type': 'object', 'description': 'Who may open the line: a story requires block; none means anyone.'},
        'limit': num(1000, 1000000, description='The most that may be owed on the line, fee included, in credits.'),
        'ratePerShift': num(None, 0.01, exclusive_minimum=0, description='Interest per shift on the balance, as a share.'),
        'drawFee': num(0, 0.2, description='The fee on each draw, as a share of the amount drawn, added to the balance.'),
        'minDraw': num(100, 1000000, description='The smallest draw, in credits (default 500); with its fee, within the limit.'),
    }, required=('name', 'pitch', 'limit', 'ratePerShift', 'drawFee'))
    return obj({**header('lenders'), 'lenders': named(lender, 'Lenders by id (lowercase words joined by dashes, at most 32 characters).', '^[a-z0-9]+(-[a-z0-9]+)*$'),
                'creditLines': named(line, 'System-wide credit lines by id (Phobos Banking 0.6.0): opened and drawn on from anywhere; ids must differ from every lender id, at most 31 characters.', '^[a-z0-9]+(-[a-z0-9]+)*$')},
               required=('schemaVersion', 'schema', 'lenders'),
               description="Phobos Banking lenders pack: who lends where, to whom and on what terms. Terms are copied into a loan when it is taken.")


def exchange():
    name_text = 'without | , ; = # [ ] < > or line breaks'
    trend = obj({
        'slowWeeks': num(0.5, 104, description='How long a phase lasts, roughly: the slow half-life in game weeks; at least 1.5 times fastWeeks.'),
        'fastWeeks': num(0.25, 26, description='How quickly a phase turns: the fast half-life in game weeks.'),
        'sd': num(0, 0.5, description='How far phases carry the price: the standard deviation of the log price (0.15 is about 15%).'),
    }, required=('slowWeeks', 'fastWeeks', 'sd'))
    market = obj({
        'notes': NOTES,
        'name': string('The exchange name in the world: 1 to 40 characters, ' + name_text + '.'),
        'commission': num(0, 0.05, description='The broker commission, as a share of an order value.'),
        'minCommission': num(0, 10000, description='The least commission on an order, in credits.'),
        'impact': num(0, 5, description='How hard an order pushes the price: impact = this x daily volatility x the square root of (shares / daily volume).'),
        'impactHalfLifeDays': num(0.05, 30, description='How quickly the push fades: a half-life in game days.'),
        'maxOrderShare': num(0.01, 5, description='The largest order, as a share of one company daily volume.'),
        'maxHolding': num(1000, 100000000, description='The most one player may hold in one company, valued at the asking price, in credits.'),
        'trend': {**trend, 'description': 'The market-wide trend phase every company follows to its own degree.'},
        'moveShare': num(0.01, 0.5, description='A change over one game day this large (a share of the price) is reported with its cause.'),
        'turnShare': num(0.001, 0.5, description='A phase moving this fast (a share of the price per game week) counts as a trend when it turns.'),
        'reportCooldownDays': num(0, 30, description='The fewest game days between two reports on one company.'),
    }, required=('name', 'commission', 'minCommission', 'impact', 'impactHalfLifeDays', 'maxOrderShare', 'maxHolding', 'trend', 'moveShare', 'turnShare', 'reportCooldownDays'))
    sector = obj({'notes': NOTES, 'name': string('The sector name shown on the panel: 1 to 40 characters, ' + name_text + '.'), 'trend': trend}, required=('name', 'trend'))
    driver = obj({
        'station': string('The station registration whose cargo market the company follows, such as MTRS.', pattern='^[A-Z0-9]{3,8}$'),
        'category': string('The game category of goods, such as AnyWeapons.', pattern='^Any[A-Za-z0-9]{1,37}$'),
        'weight': num(-1, 1, description='How strongly the company follows it: positive when dear goods there help it, negative for an input; not 0.'),
        'limit': num(0.01, 0.3, description='The most this driver may move the log price either way (default 0.12).'),
    }, required=('station', 'category', 'weight'))
    news = obj({
        'notes': NOTES,
        'flag': string('The story flag that brings the news, set by a story arc or another mod.', pattern='^[a-z0-9]+(-[a-z0-9]+)*$'),
        'move': num(-0.3, 0.3, description='How far the price moves when the news breaks, as a share (0.08 is up 8 percent); at least 0.005 either way. Once per save, and it stays.'),
        'wire': string('The wire line the player reads, in a neutral wire-service voice; at most 300 characters, no placeholders.'),
    }, required=('flag', 'move'))
    company = obj({
        'notes': NOTES,
        'ticker': string('The trading symbol: two to five capital letters, unique.', pattern='^[A-Z]{2,5}$'),
        'name': string('The company name: 1 to 40 characters, ' + name_text + '.'),
        'profile': string('What the company does, in a wire service neutral voice; at most 400 characters, no placeholders.'),
        'sector': string('One of the sectors.', pattern='^[a-z0-9]+(-[a-z0-9]+)*$'),
        'price': num(0.5, 100000, description='The price when first listed in a save, in credits per share.'),
        'dailyVolume': num(100, 1e9, description='Shares that change hands in a game day: the scale of the player price impact.'),
        'volatility': num(0.001, 0.05, description='Day-to-day noise: the standard deviation of the log price over a game day.'),
        'volOfVol': num(0, 1, description='How much the noise wanders between calm and stormy stretches.'),
        'noiseHalfLifeYears': num(0.25, 50, description='How slowly accumulated noise fades back: a half-life in game years.'),
        'drift': num(0, 0.1, description='The steady upward drift of the log price per game year (owner choice: real-world drift).'),
        'jumpsPerYear': num(0, 12, description='Sudden jumps a game year.'),
        'jumpSize': num(0, 0.3, description='The typical size of a jump: a standard deviation of the log price.'),
        'spread': num(0, 0.05, description='The gap between buying and selling prices, as a share of the price.'),
        'followsMarket': num(0, 2, description='How closely the company follows the market phases (default 1).'),
        'followsSector': num(0, 2, description='How closely it follows its sector phases (default 1).'),
        'trend': trend,
        'drivers': {'type': 'array', 'maxItems': 6, 'items': driver, 'description': 'The game market signals the company follows.'},
        'news': {'type': 'array', 'maxItems': 12, 'items': news, 'description': 'Story news that moves the price once when its story flag is set (Phobos Exchange 0.2.0).'},
    }, required=('ticker', 'name', 'profile', 'sector', 'price', 'dailyVolume', 'volatility', 'volOfVol', 'noiseHalfLifeYears', 'drift', 'jumpsPerYear', 'jumpSize', 'spread', 'trend'))
    return obj({**header('exchange'), 'market': market,
                'sectors': named(sector, 'Sectors by id (lowercase words joined by dashes, at most 24 characters); companies in one sector share its phases.', '^[a-z0-9]+(-[a-z0-9]+)*$'),
                'companies': named(company, 'Listed companies by id (lowercase words joined by dashes, at most 24 characters); the id names the company saved state.', '^[a-z0-9]+(-[a-z0-9]+)*$')},
               required=('schemaVersion', 'schema', 'market', 'sectors', 'companies'),
               description='Phobos Exchange pack: the exchange terms, its sectors and the listed companies. Each company expected yearly return must stay at or below 0.14 (checked by the validators).')


SCHEMAS = {'addon': addon, 'economy': economy, 'process-recipes': recipes, 'materials': materials, 'vessels': vessels, 'equipment': equipment, 'crops': crops, 'care': care, 'outcomes': outcomes, 'lines': lines, 'story': story, 'upkeep': upkeep, 'stores': stores, 'lenders': lenders, 'exchange': exchange}


def render(name):
    schema = {'$schema': DRAFT, '$id': f'https://github.com/phobos-dthorga/phobos-ostranauts/schemas/{name}.schema.json', 'title': f'Phobos {name} data pack'}
    schema.update(SCHEMAS[name]())
    return json.dumps(schema, indent=2, ensure_ascii=False) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--check', action='store_true', help='fail when a schema on disk is stale')
    args = parser.parse_args()
    stale = []
    OUT.mkdir(exist_ok=True)
    for name in SCHEMAS:
        path = OUT / f'{name}.schema.json'
        text = render(name)
        if args.check:
            if not path.exists() or path.read_text(encoding='utf-8') != text: stale.append(path.name)
        else:
            path.write_text(text, encoding='utf-8', newline='\n')
            print('wrote', path.relative_to(ROOT))
    if stale:
        print('stale schema files:', ', '.join(stale)); return 1
    if args.check: print(f'{len(SCHEMAS)} schema files current')
    return 0


if __name__ == '__main__':
    sys.exit(main())
