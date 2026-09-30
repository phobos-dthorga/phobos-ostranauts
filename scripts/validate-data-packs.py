#!/usr/bin/env python3
"""Offline mirror of the Framework data-pack checks, for CI without game files.

Reads every mods/<Mod>/framework/<schema>.json whose header names a known schema,
refuses unknown fields and out-of-range numbers, and mirrors the schema rules that
need no native data. The C# loader (PhobosFramework.Data) is authoritative; this
script catches mistakes before a build. Standard library only.

Usage: validate-data-packs.py [--format json] [paths...]
"""
import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REGIONS = {'BCER', 'BCRS', 'EJDR', 'HQCH', 'JATL', 'JFTS', 'JPTN', 'MHNG', 'MLAB', 'MSUZ', 'MTRS', 'MVOL', 'OFLT', 'OKLG', 'SVIR', 'VCBR', 'VENC', 'VNCA', 'VORB'}
CONDITIONS = {'Pristine', 'Refurbished', 'Worn', 'Broken'}
FACTION_TIERS = ('Neutral', 'Warm', 'Friendly', 'Trusted', 'Honored')
KINDS = {'equipment', 'supplies', 'section'}
FORMS = {'machine', 'item', 'single'}
MAX_QUANTITY = 256


class Problem(ValueError):
    pass


def fields(obj, allowed, where):
    if not isinstance(obj, dict):
        raise Problem(f'{where}: expected an object')
    extra = [k for k in obj if k not in allowed]
    if extra:
        raise Problem(f'{where}: unknown field {extra[0]!r}')


def number(value, where, low=None, high=None, integer=False, exclusive_low=False):
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise Problem(f'{where}: expected a number')
    if integer and int(value) != value:
        raise Problem(f'{where}: expected a whole number')
    if low is not None and (value < low or (exclusive_low and value == low)):
        raise Problem(f'{where}: below {low}')
    if high is not None and value > high:
        raise Problem(f'{where}: above {high}')
    return value


def bill(obj, where, allow_empty=False):
    fields_ok = isinstance(obj, dict) and all(isinstance(k, str) and k for k in obj)
    if not fields_ok:
        raise Problem(f'{where}: expected material counts by id')
    if not obj and not allow_empty:
        raise Problem(f'{where}: needs at least one material')
    for item, count in obj.items():
        number(count, f'{where}/{item}', 0, 10000, integer=True)


def work(obj, where, repair_optional=False):
    fields(obj, {'install', 'uninstall', 'repair', 'dismantle'}, where)
    # Zero install or uninstall keeps the definition's own value; dismantle is always set, repair unless a section.
    for k in ('install', 'uninstall'):
        number(obj.get(k, 0), f'{where}/{k}', 0, None, integer=True)
    number(obj.get('repair', 0), f'{where}/repair', 0, None, integer=True, exclusive_low=not repair_optional)
    number(obj.get('dismantle'), f'{where}/dismantle', 0, None, integer=True, exclusive_low=True)


def economy(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'equipment', 'supplies', 'offerTemplates', 'offers', 'regions', 'regional', 'lots', 'chanceFloors', 'worldLoot', 'factionKiosks'}, where)
    lots = pack.get('lots', {})
    floors = pack.get('chanceFloors', {})
    for name, lot in lots.items():
        if not name:
            raise Problem(f'{where}/lots: lot names cannot be empty')
        number(lot, f'{where}/lots/{name}', 1, MAX_QUANTITY, integer=True)
    for name, floor in floors.items():
        if not name:
            raise Problem(f'{where}/chanceFloors: floor names cannot be empty')
        number(floor, f'{where}/chanceFloors/{name}', 0, 1)

    def lot_and_floor(lot, floor, w):
        if lot not in lots:
            raise Problem(f'{w}: lot {lot!r} is not in the lots table')
        if floor not in floors:
            raise Problem(f'{w}: floor {floor!r} is not in the chanceFloors table')

    equipment = pack.get('equipment', {})
    if not isinstance(equipment, dict) or not equipment:
        raise Problem(f'{where}/equipment: needs at least one family')
    for key, e in equipment.items():
        w = f'{where}/equipment/{key}'
        fields(e, {'notes', 'kind', 'forms', 'price', 'brokenPrice', 'work', 'repairBill', 'salvage', 'brokenSalvage', 'restoreMinutes', 'internalBin', 'loot',
                   'salvageValueHigh', 'offers', 'offerScale', 'regionalChance', 'salvageRemainder', 'lot', 'floor'}, w)
        kind = e.get('kind', 'equipment')
        if kind not in ('equipment', 'section'):
            raise Problem(f'{w}/kind: not equipment or section')
        forms = e.get('forms', 'machine')
        if forms not in FORMS:
            raise Problem(f'{w}/forms: {sorted(FORMS)}')
        section = kind == 'section'
        price = number(e.get('price'), f'{w}/price', 0, None, exclusive_low=True)
        if 'brokenPrice' in e:
            broken = number(e['brokenPrice'], f'{w}/brokenPrice', 0, None, exclusive_low=True)
            if broken >= price:
                raise Problem(f'{w}/brokenPrice: must be below price')
        work(e.get('work', {}), f'{w}/work', repair_optional=section)
        number(e.get('restoreMinutes', 0), f'{w}/restoreMinutes', 0, None, integer=True)
        remainder = e.get('salvageRemainder', False)
        bill(e.get('repairBill', {}), f'{w}/repairBill', allow_empty=section)
        bill(e.get('salvage', {}), f'{w}/salvage', allow_empty=remainder)
        bill(e.get('brokenSalvage', {}), f'{w}/brokenSalvage', allow_empty=remainder or forms == 'single')
        number(e.get('offerScale', 1), f'{w}/offerScale', 0, 4, exclusive_low=True)
        if 'regionalChance' in e:
            number(e['regionalChance'], f'{w}/regionalChance', 0, 1)
        for flag in ('loot', 'salvageValueHigh', 'offers', 'salvageRemainder'):
            if flag in e and not isinstance(e[flag], bool):
                raise Problem(f'{w}/{flag}: expected true or false')
        lot_and_floor(e.get('lot', kind), e.get('floor', 'equipment'), w)
    for key, s in pack.get('supplies', {}).items():
        w = f'{where}/supplies/{key}'
        fields(s, {'notes', 'kind', 'price', 'repairWork', 'dismantleWork', 'repairBill', 'remainder', 'merchants', 'chance', 'lot', 'floor', 'expanded', 'regionalChance', 'regionalCondition'}, w)
        if s.get('kind', 'supplies') != 'supplies':
            raise Problem(f'{w}/kind: supplies')
        number(s.get('price'), f'{w}/price', 0, None, exclusive_low=True)
        number(s.get('repairWork'), f'{w}/repairWork', 0, None, integer=True, exclusive_low=True)
        number(s.get('dismantleWork'), f'{w}/dismantleWork', 0, None, integer=True, exclusive_low=True)
        bill(s.get('repairBill', {}), f'{w}/repairBill')
        number(s.get('chance', 1), f'{w}/chance', 0, 1)
        merchants = s.get('merchants', [])
        if not isinstance(merchants, list) or not merchants or not all(isinstance(m, str) and m for m in merchants):
            raise Problem(f'{w}/merchants: needs at least one merchant id')
        if 'regionalChance' in s:
            number(s['regionalChance'], f'{w}/regionalChance', 0, 1)
        condition = s.get('regionalCondition', 'regional')
        if condition != 'regional' and condition not in CONDITIONS:
            raise Problem(f'{w}/regionalCondition: regional or {sorted(CONDITIONS)}')
        lot_and_floor(s.get('lot', 'supplies'), s.get('floor', 'supplies'), w)
    for i, o in enumerate(pack.get('offerTemplates', [])):
        w = f'{where}/offerTemplates/{i}'
        fields(o, {'notes', 'merchant', 'tag', 'form', 'condition', 'chance'}, w)
        if not str(o.get('tag', '')).isalnum():
            raise Problem(f'{w}/tag: letters and digits only')
        if o.get('form', 'Loose') not in ('Loose', 'LooseDmg'):
            raise Problem(f'{w}/form: Loose or LooseDmg')
        if o.get('condition', 'Pristine') not in CONDITIONS:
            raise Problem(f'{w}/condition: {sorted(CONDITIONS)}')
        number(o.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
        if not o.get('merchant'):
            raise Problem(f'{w}/merchant: needed')
    for key, o in pack.get('offers', {}).items():
        w = f'{where}/offers/{key}'
        if not key.startswith('Phobos'):
            raise Problem(f'{w}: offer ids start with Phobos')
        fields(o, {'notes', 'merchant', 'item', 'condition', 'chance', 'quantity', 'lot', 'floor'}, w)
        if not o.get('merchant') or not o.get('item'):
            raise Problem(f'{w}: merchant and item are needed')
        if o.get('condition', 'Pristine') not in CONDITIONS:
            raise Problem(f'{w}/condition: {sorted(CONDITIONS)}')
        number(o.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
        if 'quantity' in o:
            number(o['quantity'], f'{w}/quantity', 1, MAX_QUANTITY, integer=True)
        if 'lot' in o or 'floor' in o:
            lot_and_floor(o.get('lot', 'equipment'), o.get('floor', 'equipment'), w)
    for code, factor in pack.get('regions', {}).items():
        if code not in REGIONS:
            raise Problem(f'{where}/regions/{code}: unknown region')
        number(factor, f'{where}/regions/{code}', 0, 4)
    regional = pack.get('regional')
    if regional is not None:
        fields(regional, {'notes', 'baseChance', 'refurbished', 'expandedMerchants', 'items'}, f'{where}/regional')
        number(regional.get('baseChance'), f'{where}/regional/baseChance', 0, 1, exclusive_low=True)
        for code in regional.get('refurbished', []):
            if code not in REGIONS:
                raise Problem(f'{where}/regional/refurbished: unknown region {code}')
        for item, r in regional.get('items', {}).items():
            w = f'{where}/regional/items/{item}'
            if not item:
                raise Problem(f'{w}: item id needed')
            fields(r, {'notes', 'chance', 'condition', 'lot', 'floor', 'expanded'}, w)
            number(r.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
            if r.get('condition', 'Pristine') not in CONDITIONS:
                raise Problem(f'{w}/condition: {sorted(CONDITIONS)}')
            lot_and_floor(r.get('lot', 'supplies'), r.get('floor', 'supplies'), w)
    for i, loot in enumerate(pack.get('worldLoot', [])):
        w = f'{where}/worldLoot/{i}'
        fields(loot, {'notes', 'table', 'tables', 'branch', 'chance', 'brokenShare', 'items'}, w)
        tables = loot.get('tables') or [loot.get('table', '')]
        if not all(isinstance(x, str) and x for x in tables):
            raise Problem(f'{w}/table: needed')
        if not str(loot.get('branch', '')).startswith('Phobos'):
            raise Problem(f'{w}/branch: starts with Phobos')
        if 'items' in loot:
            items = loot['items']
            if not isinstance(items, dict) or not items:
                raise Problem(f'{w}/items: needs at least one item')
            total = 0
            for item, chance in items.items():
                total += number(chance, f'{w}/items/{item}', 0, 1, exclusive_low=True)
            if total > 1 + 1e-9:
                raise Problem(f'{w}/items: chances add up to more than 1')
        else:
            number(loot.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
        number(loot.get('brokenShare', 0), f'{w}/brokenShare', 0, 1)
    kiosks = pack.get('factionKiosks')
    if kiosks is not None:
        w = f'{where}/factionKiosks'
        fields(kiosks, {'notes', 'merchants', 'chance', 'tiers'}, w)
        merchants = kiosks.get('merchants', [])
        if not isinstance(merchants, list) or not merchants or not all(isinstance(m, str) and m.strip() for m in merchants):
            raise Problem(f'{w}/merchants: at least one merchant is needed')
        number(kiosks.get('chance', 1), f'{w}/chance', 0, 1, exclusive_low=True)
        tiers = kiosks.get('tiers', {})
        if not isinstance(tiers, dict):
            raise Problem(f'{w}/tiers: expected tiers by item')
        for item, tier in tiers.items():
            if not item.strip():
                raise Problem(f'{w}/tiers: item id needed')
            if tier not in FACTION_TIERS:
                raise Problem(f'{w}/tiers/{item}: {tier!r} is not one of {list(FACTION_TIERS)}')

SPECIES = {'CH4', 'CO', 'CO2', 'H2SO4', 'N2', 'NH3', 'O2', 'Smoke'}
MASS_TOLERANCE = 1e-6


def units(items, where):
    if not isinstance(items, list) or not items:
        raise Problem(f'{where}: needs at least one unit')
    ids = []
    for i, u in enumerate(items):
        fields(u, {'id', 'count', 'kg'}, f'{where}/{i}')
        if not u.get('id'):
            raise Problem(f'{where}/{i}: id needed')
        number(u.get('count', 1), f'{where}/{i}/count', 1, None, integer=True)
        number(u.get('kg'), f'{where}/{i}/kg', 0, None, exclusive_low=True)
        ids.append(u['id'])
    if len(set(ids)) != len(ids):
        raise Problem(f'{where}: the same id appears twice')
    return sum(u.get('count', 1) * u['kg'] for u in items)


def process_recipes(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'recipes'}, where)
    recipes = pack.get('recipes', {})
    if not isinstance(recipes, dict) or not recipes:
        raise Problem(f'{where}/recipes: needs at least one recipe')
    seen = set()
    for key, r in recipes.items():
        w = f'{where}/recipes/{key}'
        if not key or any(not (c.isalnum() or c == '-') for c in key):
            raise Problem(f'{w}: ids are letters, digits and hyphens')
        fields(r, {'notes', 'machine', 'revision', 'inputs', 'products', 'offGas', 'seconds', 'legacySeconds', 'melt', 'requires', 'thermal', 'circulates', 'reactionKWh'}, w)
        for commodity, kg in r.get('circulates', {}).items():
            if not commodity:
                raise Problem(f'{w}/circulates: commodity needed')
            number(kg, f'{w}/circulates/{commodity}', 0, None, exclusive_low=True)
        if 'reactionKWh' in r:
            number(r['reactionKWh'], f'{w}/reactionKWh', -1000, 1000)
        if not r.get('machine'):
            raise Problem(f'{w}/machine: needed')
        number(r.get('revision'), f'{w}/revision', 1, None, integer=True)
        pair = (r['machine'], r['revision'])
        if pair in seen:
            raise Problem(f'{w}: {pair[0]} already has revision {pair[1]}')
        seen.add(pair)
        in_kg = units(r.get('inputs'), f'{w}/inputs')
        out_kg = units(r.get('products'), f'{w}/products')
        gas = r.get('offGas', {})
        if not isinstance(gas, dict):
            raise Problem(f'{w}/offGas: expected species to kg')
        for species, kg in gas.items():
            if species not in SPECIES:
                raise Problem(f'{w}/offGas/{species}: not one of the game gases {sorted(SPECIES)}')
            number(kg, f'{w}/offGas/{species}', 0, None, exclusive_low=True)
        out_kg += sum(gas.values())
        if abs(in_kg - out_kg) > MASS_TOLERANCE:
            raise Problem(f'{w}: does not conserve mass ({in_kg:.6f} in, {out_kg:.6f} out)')
        for name in ('seconds', 'legacySeconds'):
            if name in r:
                number(r[name], f'{w}/{name}', 1, 3600)
        if 'melt' in r and not isinstance(r['melt'], bool):
            raise Problem(f'{w}/melt: expected true or false')
        requires = r.get('requires', [])
        if not isinstance(requires, list) or not all(isinstance(x, str) and x for x in requires):
            raise Problem(f'{w}/requires: expected a list of keys')
        thermal = r.get('thermal')
        if thermal is not None:
            fields(thermal, {'meltK', 'targetK', 'solidCp', 'liquidCp', 'latentKJ', 'holdSeconds'}, f'{w}/thermal')
            for name in ('meltK', 'targetK', 'solidCp', 'liquidCp', 'latentKJ', 'holdSeconds'):
                number(thermal.get(name), f'{w}/thermal/{name}', 0, None, exclusive_low=True)
            if thermal['targetK'] <= thermal['meltK']:
                raise Problem(f'{w}/thermal: targetK must be above meltK')


def materials(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'materials'}, where)
    entries = pack.get('materials', {})
    if not isinstance(entries, dict) or not entries:
        raise Problem(f'{where}/materials: needs at least one material')
    for key, m in entries.items():
        w = f'{where}/materials/{key}'
        fields(m, {'notes', 'kind', 'kg', 'price', 'stack', 'side', 'category', 'terminal', 'art'}, w)
        number(m.get('kg'), f'{w}/kg', 0, None, exclusive_low=True)
        number(m.get('price'), f'{w}/price', 0, None, exclusive_low=True)
        number(m.get('stack', 1), f'{w}/stack', 1, 1000, integer=True)
        number(m.get('side', 1), f'{w}/side', 1, 8, integer=True)
        if 'terminal' in m and not isinstance(m['terminal'], bool):
            raise Problem(f'{w}/terminal: expected true or false')



def vessels(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'families'}, where)
    entries = pack.get('families', {})
    if not isinstance(entries, dict) or not entries:
        raise Problem(f'{where}/families: needs at least one family')
    for key, v in entries.items():
        w = f'{where}/families/{key}'
        fields(v, {'notes', 'kind', 'commodity', 'capacityKg', 'dryKg', 'leakKgPerHour', 'cellsPerTileSide'}, w)
        if v.get('kind') == 'bin':
            if 'capacityKg' in v or 'commodity' in v:
                raise Problem(f'{w}: a bin has no commodity or capacityKg')
            number(v.get('cellsPerTileSide'), f'{w}/cellsPerTileSide', 1, 8, integer=True)
        else:
            if not v.get('commodity'):
                raise Problem(f'{w}/commodity: needed')
            number(v.get('capacityKg'), f'{w}/capacityKg', 0, 100000, exclusive_low=True)
            if 'cellsPerTileSide' in v:
                raise Problem(f'{w}: cellsPerTileSide belongs to bins only')
        number(v.get('dryKg'), f'{w}/dryKg', 0, 10000, exclusive_low=True)
        number(v.get('leakKgPerHour', 0), f'{w}/leakKgPerHour', 0, 1000)



def equipment(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'equipment'}, where)
    entries = pack.get('equipment', {})
    if not isinstance(entries, dict) or not entries:
        raise Problem(f'{where}/equipment: needs at least one machine')
    for key, e in entries.items():
        w = f'{where}/equipment/{key}'
        fields(e, {'notes', 'kind', 'footprint', 'massKg', 'idleKW', 'workingKW', 'roomHeatFraction', 'feedCells', 'art', 'installTab', 'points'}, w)
        number(e.get('footprint'), f'{w}/footprint', 1, 12, integer=True)
        number(e.get('massKg'), f'{w}/massKg', 0, 100000, exclusive_low=True)
        working = number(e.get('workingKW'), f'{w}/workingKW', 0, 10000, exclusive_low=True)
        number(e.get('idleKW', 0), f'{w}/idleKW', 0, working)
        number(e.get('roomHeatFraction', 0), f'{w}/roomHeatFraction', 0, 1)
        number(e.get('feedCells', 0), f'{w}/feedCells', 0, 64, integer=True)
        points = e.get('points', {})
        if not isinstance(points, dict):
            raise Problem(f'{w}/points: expected named offsets')
        for name, xy in points.items():
            if not isinstance(xy, list) or len(xy) != 2:
                raise Problem(f'{w}/points/{name}: two pixel offsets')
            for i, v in enumerate(xy):
                number(v, f'{w}/points/{name}/{i}', -512, 512, integer=True)

SCHEMAS = {'economy': economy, 'process-recipes': process_recipes, 'materials': materials, 'vessels': vessels, 'equipment': equipment}


def check_file(path):
    text = path.read_text(encoding='utf-8-sig')
    if len(text.encode('utf-8')) > 1048576:
        raise Problem('larger than 1 MiB')
    def no_duplicates(pairs):
        seen = {}
        for k, v in pairs:
            if k in seen:
                raise Problem(f'duplicate field {k!r}')
            seen[k] = v
        return seen
    pack = json.loads(text, object_pairs_hook=no_duplicates)
    if not isinstance(pack, dict):
        raise Problem('expected an object')
    schema = pack.get('schema')
    if schema not in SCHEMAS:
        return None
    if pack.get('schemaVersion') != 1:
        raise Problem('schemaVersion must be 1')
    SCHEMAS[schema](pack, schema)
    return schema


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('paths', nargs='*')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    args = parser.parse_args()
    paths = [Path(p) for p in args.paths] or sorted((ROOT / 'mods').glob('*/framework/*.json'))
    results, errors = [], []
    for path in paths:
        rel = path.resolve().relative_to(ROOT.resolve()).as_posix() if path.resolve().is_relative_to(ROOT.resolve()) else str(path)
        try:
            schema = check_file(path)
        except (Problem, ValueError, OSError) as error:
            errors.append({'path': rel, 'error': str(error)})
            continue
        if schema:
            results.append({'path': rel, 'schema': schema})
    report = {'schemaVersion': 1, 'status': 'valid' if not errors else 'invalid', 'packs': results, 'errors': errors}
    if args.format == 'json':
        print(json.dumps(report, indent=2))
    else:
        for r in results:
            print(f"ok  {r['path']} ({r['schema']})")
        for e in errors:
            print(f"ERR {e['path']}: {e['error']}")
        print(f"{len(results)} pack(s) valid, {len(errors)} error(s). The game's own loader remains authoritative.")
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
