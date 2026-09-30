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
KINDS = {'equipment', 'supplies'}
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


def work(obj, where):
    fields(obj, {'install', 'uninstall', 'repair', 'dismantle'}, where)
    for k in ('install', 'uninstall', 'repair', 'dismantle'):
        number(obj.get(k, 0), f'{where}/{k}', 0, None, integer=True, exclusive_low=True)


def economy(pack, where):
    fields(pack, {'schemaVersion', 'schema', 'notes', 'equipment', 'supplies', 'offerTemplates', 'offers', 'regions', 'regional', 'lots', 'chanceFloors', 'worldLoot'}, where)
    equipment = pack.get('equipment', {})
    if not isinstance(equipment, dict) or not equipment:
        raise Problem(f'{where}/equipment: needs at least one family')
    for key, e in equipment.items():
        w = f'{where}/equipment/{key}'
        fields(e, {'notes', 'kind', 'price', 'brokenPrice', 'work', 'repairBill', 'salvage', 'brokenSalvage', 'restoreMinutes', 'internalBin', 'loot', 'salvageValueHigh', 'offers'}, w)
        if e.get('kind', 'equipment') not in KINDS:
            raise Problem(f'{w}/kind: not equipment or supplies')
        price = number(e.get('price'), f'{w}/price', 0, None, exclusive_low=True)
        if 'brokenPrice' in e:
            broken = number(e['brokenPrice'], f'{w}/brokenPrice', 0, None, exclusive_low=True)
            if broken >= price:
                raise Problem(f'{w}/brokenPrice: must be below price')
        work(e.get('work', {}), f'{w}/work')
        number(e.get('restoreMinutes'), f'{w}/restoreMinutes', 0, None, integer=True, exclusive_low=True)
        for name in ('repairBill', 'salvage', 'brokenSalvage'):
            bill(e.get(name, {}), f'{w}/{name}')
        for flag in ('loot', 'salvageValueHigh', 'offers'):
            if flag in e and not isinstance(e[flag], bool):
                raise Problem(f'{w}/{flag}: expected true or false')
    for key, s in pack.get('supplies', {}).items():
        w = f'{where}/supplies/{key}'
        fields(s, {'notes', 'kind', 'price', 'repairWork', 'dismantleWork', 'repairBill', 'remainder', 'merchants', 'chance'}, w)
        number(s.get('price'), f'{w}/price', 0, None, exclusive_low=True)
        number(s.get('repairWork'), f'{w}/repairWork', 0, None, integer=True, exclusive_low=True)
        number(s.get('dismantleWork'), f'{w}/dismantleWork', 0, None, integer=True, exclusive_low=True)
        bill(s.get('repairBill', {}), f'{w}/repairBill')
        number(s.get('chance', 1), f'{w}/chance', 0, 1)
        merchants = s.get('merchants', [])
        if not isinstance(merchants, list) or not merchants or not all(isinstance(m, str) and m for m in merchants):
            raise Problem(f'{w}/merchants: needs at least one merchant id')
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
        fields(o, {'notes', 'merchant', 'item', 'condition', 'chance', 'quantity'}, w)
        if not o.get('merchant') or not o.get('item'):
            raise Problem(f'{w}: merchant and item are needed')
        if o.get('condition', 'Pristine') not in CONDITIONS:
            raise Problem(f'{w}/condition: {sorted(CONDITIONS)}')
        number(o.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
        if 'quantity' in o:
            number(o['quantity'], f'{w}/quantity', 1, MAX_QUANTITY, integer=True)
    for code, factor in pack.get('regions', {}).items():
        if code not in REGIONS:
            raise Problem(f'{where}/regions/{code}: unknown region')
        number(factor, f'{where}/regions/{code}', 0, 4)
    regional = pack.get('regional')
    if regional is not None:
        fields(regional, {'notes', 'baseChance', 'refurbished', 'expandedMerchants'}, f'{where}/regional')
        number(regional.get('baseChance'), f'{where}/regional/baseChance', 0, 1, exclusive_low=True)
        for code in regional.get('refurbished', []):
            if code not in REGIONS:
                raise Problem(f'{where}/regional/refurbished: unknown region {code}')
    for kind, lot in pack.get('lots', {}).items():
        if kind not in KINDS:
            raise Problem(f'{where}/lots/{kind}: unknown kind')
        number(lot, f'{where}/lots/{kind}', 1, MAX_QUANTITY, integer=True)
    for kind, floor in pack.get('chanceFloors', {}).items():
        if kind not in KINDS:
            raise Problem(f'{where}/chanceFloors/{kind}: unknown kind')
        number(floor, f'{where}/chanceFloors/{kind}', 0, 1)
    for i, loot in enumerate(pack.get('worldLoot', [])):
        w = f'{where}/worldLoot/{i}'
        fields(loot, {'notes', 'table', 'branch', 'chance', 'brokenShare'}, w)
        if not loot.get('table'):
            raise Problem(f'{w}/table: needed')
        if not str(loot.get('branch', '')).startswith('Phobos'):
            raise Problem(f'{w}/branch: starts with Phobos')
        number(loot.get('chance'), f'{w}/chance', 0, 1, exclusive_low=True)
        number(loot.get('brokenShare'), f'{w}/brokenShare', 0, 1)


SCHEMAS = {'economy': economy}


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
