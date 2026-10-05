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


SCHEMAS = {'addon': addon, 'economy': economy, 'process-recipes': recipes, 'materials': materials, 'vessels': vessels, 'equipment': equipment, 'crops': crops, 'care': care, 'outcomes': outcomes, 'lines': lines}


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
