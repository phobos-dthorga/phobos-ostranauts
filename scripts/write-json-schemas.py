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
    }, ['machine', 'revision', 'inputs', 'products'])
    return obj({**header('process-recipes'), 'recipes': named(recipe, 'Recipes by id; published revisions are frozen by hash.', '^[a-z0-9-]+$')},
               ['schemaVersion', 'schema', 'recipes'], 'Fixed process recipes: mass-conserving inputs, products and native off-gas.')


def materials():
    material = obj({'notes': NOTES, 'kind': string('How the mod builds it (stock, mined, packet, food, waste, ...).'), 'kg': num(0, exclusive_minimum=0),
                    'price': num(0, exclusive_minimum=0), 'stack': num(1, 1000, integer=True), 'side': num(1, 8, integer=True),
                    'category': string('Native market category condition, e.g. IsCategoryMetals.'), 'terminal': {'type': 'boolean'}, 'art': string()}, ['kg', 'price'])
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

SCHEMAS = {'economy': economy, 'process-recipes': recipes, 'materials': materials, 'vessels': vessels, 'equipment': equipment}


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
