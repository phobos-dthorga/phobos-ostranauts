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
import re
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
    if not isinstance(equipment, dict) or (not equipment and not pack.get('supplies')):
        raise Problem(f'{where}/equipment: needs at least one family or supply')
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
        fields(r, {'notes', 'machine', 'revision', 'inputs', 'products', 'offGas', 'seconds', 'legacySeconds', 'melt', 'requires', 'thermal', 'circulates', 'reactionKWh', 'supersedes'}, w)
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
    # Supersession (Framework 0.68.0): an earlier revision of the same machine, replaced by one recipe only.
    replaced = set()
    for key, r in recipes.items():
        w = f'{where}/recipes/{key}'
        earlier = r.get('supersedes', [])
        if not isinstance(earlier, list):
            raise Problem(f'{w}/supersedes: expected a list of revisions')
        for e in earlier:
            if not isinstance(e, int) or isinstance(e, bool) or e < 1 or e >= r['revision'] or (r['machine'], e) not in seen:
                raise Problem(f'{w}/supersedes: {e!r} is not an earlier revision of {r["machine"]}')
            if (r['machine'], e) in replaced:
                raise Problem(f'{w}/supersedes: {r["machine"]} revision {e} is already superseded')
            replaced.add((r['machine'], e))


# Mods whose materials pack takes added materials (Framework 0.92.0).
# Each with the kinds an added material may be: Shipbreaker's are always plain stock (0.75.0).
MATERIAL_ADDITIONS = {'PhobosManufacturing': ('stock', 'mined'), 'PhobosShipbreaker': ('stock',), 'PhobosAgriculture': ('stock', 'food', 'waste')}
MATERIAL_RESERVED = ('phobos', 'itm', 'sys', 'stat', 'is')


def materials(pack, where, shipped=None):
    """shipped: the shipped pack's ids when checking an override over a pack that takes additions; None otherwise."""
    fields(pack, {'schemaVersion', 'schema', 'notes', 'materials'}, where)
    entries = pack.get('materials', {})
    if not isinstance(entries, dict) or not entries:
        raise Problem(f'{where}/materials: needs at least one material')
    for key, m in entries.items():
        w = f'{where}/materials/{key}'
        fields(m, {'notes', 'kind', 'kg', 'price', 'stack', 'side', 'category', 'terminal', 'art', 'name', 'description', 'image'}, w)
        added = shipped is not None and key not in shipped
        if added:
            if not 3 <= len(key) <= 64 or not key.isascii() or not key.isalnum() or not key[0].isalpha() or any(key.lower().startswith(r) for r in MATERIAL_RESERVED):
                raise Problem(f'{w}: an added material id is 3 to 64 letters and digits, starts with a letter, and does not start with Phobos, Itm, Sys, Stat or Is')
            image = m.get('image', '')
            if (not isinstance(m.get('name'), str) or not m['name'].strip() or not isinstance(m.get('category'), str) or not m['category'].strip() or not isinstance(image, str) or not image
                    or image.startswith('/') or '..' in image or any(not (c.isascii() and c.isalnum() or c in '/_-') for c in image)):
                raise Problem(f'{w}: an added material needs a name, a category and an image (a path under an images folder, without .png)')
            if m['category'] == 'IsCategoryTrash' and m.get('terminal') is not True:
                raise Problem(f'{w}: an added item in the trash category must say "terminal": true, so the reaction mass feeder takes it')
        elif any(k in m for k in ('name', 'description', 'image')):
            raise Problem(f'{w}: name, description and image are for added materials only')
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

CROP_TOLERANCE = 1e-9
# Carbon dioxide taken in less oxygen given off, per kilogram of carbon fixed as CH2O: (44 - 32) / 30.
NET_GAS_PER_CARBON = 12 / 30


GROWTH_DEFAULT_ROOM = {'minC': 18, 'maxC': 31, 'minKPa': 70, 'maxKPa': 110}


def growth(section, crop_entries, where):
    """The crops pack's growth section (Agriculture 0.55.0): the growing room, stress rules and W2 feeding figures.
    Every figure is optional; a crop's own room replaces only what it gives."""
    if section is None:
        return
    fields(section, {'notes', 'room', 'stress', 'nutrientTargetKg', 'feedStrengthKgPerKg', 'crops'}, where)

    def room(entry, w, base):
        resolved = dict(base)
        if entry is not None:
            fields(entry, {'notes', 'minC', 'maxC', 'minKPa', 'maxKPa'}, w)
            for name in ('minC', 'maxC', 'minKPa', 'maxKPa'):
                if name in entry:
                    number(entry[name], f'{w}/{name}', None, None)
                    resolved[name] = entry[name]
        if not (-50 <= resolved['minC'] < resolved['maxC'] <= 100 and 0 < resolved['minKPa'] < resolved['maxKPa'] <= 500):
            raise Problem(f'{w}: the lower limit must be below the upper one, between -50 and 100 C and up to 500 kPa')
        return resolved

    shared = room(section.get('room'), f'{where}/room', GROWTH_DEFAULT_ROOM)
    stress = section.get('stress')
    if stress is not None:
        fields(stress, {'notes', 'graceHours', 'healthLossPerHour', 'healthLossPerHourOutside', 'healthLossPerHourNoAir', 'plantWaterKg'}, f'{where}/stress')
        if 'graceHours' in stress:
            number(stress['graceHours'], f'{where}/stress/graceHours', 0, 1000)
        for name in ('healthLossPerHour', 'healthLossPerHourOutside', 'healthLossPerHourNoAir'):
            if name in stress:
                number(stress[name], f'{where}/stress/{name}', 0, 1)
        if 'plantWaterKg' in stress:
            number(stress['plantWaterKg'], f'{where}/stress/plantWaterKg', 0, 20)
    if 'nutrientTargetKg' in section:
        number(section['nutrientTargetKg'], f'{where}/nutrientTargetKg', 0, 0.5, exclusive_low=True)
    if 'feedStrengthKgPerKg' in section:
        number(section['feedStrengthKgPerKg'], f'{where}/feedStrengthKgPerKg', 0, 1, exclusive_low=True)
    per_crop = section.get('crops', {})
    if not isinstance(per_crop, dict):
        raise Problem(f'{where}/crops: expected crop name to entry')
    for key, entry in per_crop.items():
        if key not in crop_entries:
            raise Problem(f'{where}/crops/{key}: names a crop that is not in this file')
        fields(entry, {'notes', 'room'}, f'{where}/crops/{key}')
        room(entry.get('room'), f'{where}/crops/{key}/room', shared)


def crops(pack, where, added=()):
    """The crops schema (Agriculture 0.40.0): structure and mass balance. Item masses, text and the freeze are
    checked by the game-side loader and scripts/freeze-recipes.py. added: the ids of materials an add-on adds
    (Agriculture 0.48.0), whose item entries carry no text key."""
    fields(pack, {'schemaVersion', 'schema', 'notes', 'crops', 'items', 'co2Response', 'growth'}, where)
    response = pack.get('co2Response')
    if response is not None:
        fields(response, {'notes', 'points'}, f'{where}/co2Response')
        points, last = response.get('points'), -1
        if not isinstance(points, list) or not 1 <= len(points) <= 16:
            raise Problem(f'{where}/co2Response/points: 1 to 16 points of [kPa, factor]')
        for i, point in enumerate(points):
            if not isinstance(point, list) or len(point) != 2:
                raise Problem(f'{where}/co2Response/points/{i}: expected [kPa, factor]')
            number(point[0], f'{where}/co2Response/points/{i}/0', 0, 10)
            number(point[1], f'{where}/co2Response/points/{i}/1', 0.5, 2)
            if point[0] <= last:
                raise Problem(f'{where}/co2Response/points/{i}: pressures must rise')
            last = point[0]
    entries = pack.get('crops', {})
    if not isinstance(entries, dict) or not entries:
        raise Problem(f'{where}/crops: needs at least one crop')
    items = pack.get('items', {})
    if not isinstance(items, dict):
        raise Problem(f'{where}/items: expected item id to entry')
    growth(pack.get('growth'), entries, f'{where}/growth')
    feeds, commodities = set(), set()
    for key, c in entries.items():
        w = f'{where}/crops/{key}'
        if not key or key == 'empty' or any(not (ch.islower() or ch.isdigit() or ch == '-') for ch in key):
            raise Problem(f'{w}: names are lower-case letters, digits and hyphens')
        fields(c, {'notes', 'name', 'hours', 'kw', 'seedKg', 'finalKg', 'carbonKg', 'nutrientKg', 'waterKg', 'vapourKg', 'seedCarbonKg',
                   'edibleKg', 'keptStockKg', 'portionKg', 'picks', 'pickKg', 'stock', 'produce', 'feed', 'feedCommodity', 'art'}, w)
        if 'name' in c and (not isinstance(c['name'], str) or not c['name'].strip() or len(c['name']) > 40):
            raise Problem(f'{w}/name: a plain name is at most 40 characters')
        number(c.get('hours'), f'{w}/hours', 0, 10000, exclusive_low=True)
        number(c.get('kw'), f'{w}/kw', 0, 1.5, exclusive_low=True)
        for name in ('seedKg', 'finalKg', 'carbonKg', 'nutrientKg', 'waterKg', 'edibleKg', 'portionKg'):
            number(c.get(name), f'{w}/{name}', 0, None, exclusive_low=True)
        for name in ('vapourKg', 'seedCarbonKg', 'keptStockKg'):
            number(c.get(name), f'{w}/{name}', 0, None)
        if c['seedKg'] >= c['finalKg'] or c['seedCarbonKg'] > c['seedKg'] or c['seedCarbonKg'] + c['carbonKg'] > c['finalKg'] + CROP_TOLERANCE:
            raise Problem(f'{w}: holds more carbon than it weighs, or its seed weighs as much as its harvest')
        gained = c['waterKg'] + c['nutrientKg'] + c['carbonKg'] * NET_GAS_PER_CARBON - c['vapourKg']
        grown = c['finalKg'] - c['seedKg']
        if abs(gained - grown) > CROP_TOLERANCE:
            raise Problem(f'{w}: does not conserve mass (takes in {gained:.6f} kg, grows by {grown:.6f} kg)')
        if c['edibleKg'] > c['finalKg'] + CROP_TOLERANCE or c['keptStockKg'] > c['edibleKg'] or c['portionKg'] > c['edibleKg']:
            raise Problem(f'{w}: the edible share, kept stock and one portion must fit inside the harvest')
        if c['keptStockKg'] > 0 and abs(c['keptStockKg'] - c['seedKg']) > CROP_TOLERANCE:
            raise Problem(f'{w}/keptStockKg: stock kept back must be exactly one planting')
        # Repeat picking (Agriculture 0.42.0): whole portions, within the edible share, less than one cycle's growth.
        picks, pick_kg = c.get('picks', 0), c.get('pickKg', 0)
        number(picks, f'{w}/picks', 0, 10, integer=True)
        number(pick_kg, f'{w}/pickKg', 0, None)
        if (picks == 0) != (pick_kg == 0) or picks > 0 and not (c['portionKg'] <= pick_kg <= c['edibleKg'] and pick_kg < c['finalKg'] - c['seedKg']):
            raise Problem(f'{w}: a picked crop needs a pick mass of at least one portion, within its edible share and less than one cycle of growth')
        for name in ('stock', 'produce'):
            if not c.get(name) or c[name] not in items:
                raise Problem(f'{w}/{name}: must name an entry of the items section')
        # The old feed names (until Agriculture 0.54.0) are optional since 0.55.0; one that is given stays its crop's alone.
        for name, seen in (('feed', feeds), ('feedCommodity', commodities)):
            if name in c:
                if not isinstance(c[name], str) or c[name] and (not c[name].strip() or c[name] == 'water' or c[name] in seen):
                    raise Problem(f'{w}/{name}: an old feed name must be its crop\'s own; a new crop can leave it out')
                if c[name]:
                    seen.add(c[name])
        if not c.get('art') or not c['art'].isalnum():
            raise Problem(f'{w}/art: needs an artwork name made of letters and digits')
    for key, item in items.items():
        w = f'{where}/items/{key}'
        fields(item, {'notes', 'text', 'hunger', 'satiety'}, w)
        if key in added:
            if 'text' in item:
                raise Problem(f'{w}/text: an added item is named in the materials file, not here')
        elif not item.get('text'):
            raise Problem(f'{w}/text: needed')
        if ('hunger' in item) != ('satiety' in item):
            raise Problem(f'{w}: hunger and satiety go together')
        for name in ('hunger', 'satiety'):
            if name in item:
                number(item[name], f'{w}/{name}', 1, 20, integer=True)


CARE_STATIONS = ('bed', 'monitor')
CARE_LEVELS = ('bed',)
CARE_TESTS = ('bleeding', 'fracture', 'spent-dressing')
CARE_EFFECTS = ('slot-item',)
CARE_NAME = re.compile(r'^[A-Za-z0-9_-]{1,48}$')
CARE_IDENT = re.compile(r'^[A-Za-z0-9_]{1,48}$')
CARE_MAX_KW = 2
# The game's fatal or knock-out levels: an admission threshold must sit below them (Phobos Medical 0.1.0).
CARE_LIMITS = {'bloodLost': 40, 'infection': 95, 'pain': 75, 'wound': 1}


def care(pack, where):
    """The care schema (Phobos Medical 0.1.0): station power and the injured and discharge thresholds."""
    fields(pack, {'schemaVersion', 'schema', 'notes', 'stations', 'admission', 'levels', 'alerts', 'treatments'}, where)
    stations = pack.get('stations')
    if not isinstance(stations, dict):
        raise Problem(f'{where}/stations: expected station to entry')
    for key in CARE_STATIONS:
        if key not in stations:
            raise Problem(f'{where}/stations: missing {key}')
    for key, s in stations.items():
        w = f'{where}/stations/{key}'
        if key not in CARE_STATIONS:
            raise Problem(f'{w}: unknown station; the stations are {", ".join(CARE_STATIONS)}')
        fields(s, {'notes', 'idleKW', 'workingKW'}, w)
        number(s.get('idleKW'), f'{w}/idleKW', 0, CARE_MAX_KW)
        number(s.get('workingKW'), f'{w}/workingKW', 0, CARE_MAX_KW, exclusive_low=True)
        if s['idleKW'] > s['workingKW']:
            raise Problem(f'{w}: idle power above working power')
    a = pack.get('admission')
    if not isinstance(a, dict):
        raise Problem(f'{where}/admission: expected an object')
    fields(a, {'notes', 'dischargeShare', *CARE_LIMITS}, f'{where}/admission')
    for name, below in CARE_LIMITS.items():
        number(a.get(name), f'{where}/admission/{name}', 0, below, exclusive_low=True)
        if a[name] >= below:
            raise Problem(f'{where}/admission/{name}: must be below {below}')
    number(a.get('dischargeShare'), f'{where}/admission/dischargeShare', 0, 1)
    if a['dischargeShare'] >= 1:
        raise Problem(f'{where}/admission/dischargeShare: must be below 1')
    # Medical 0.2.0: what a station adds to the game's care; optional.
    levels = pack.get('levels', {})
    if not isinstance(levels, dict):
        raise Problem(f'{where}/levels: expected station to entry')
    for key, level in levels.items():
        w = f'{where}/levels/{key}'
        if key not in CARE_LEVELS:
            raise Problem(f'{w}: unknown level; the levels are {", ".join(CARE_LEVELS)}')
        fields(level, {'notes', 'weightlessHealing'}, w)
        number(level.get('weightlessHealing'), f'{w}/weightlessHealing', 0.05, 1)
    # Medical 0.3.0: when a Vigil-2 warns; optional, every figure below the game's fatal or knock-out level.
    alerts = pack.get('alerts')
    if alerts is not None:
        fields(alerts, {'notes', 'bloodLost', 'infection', 'pain'}, f'{where}/alerts')
        for name in ('bloodLost', 'infection', 'pain'):
            number(alerts.get(name), f'{where}/alerts/{name}', 0, CARE_LIMITS[name], exclusive_low=True)
            if alerts[name] >= CARE_LIMITS[name]:
                raise Problem(f'{where}/alerts/{name}: must be below {CARE_LIMITS[name]}')
    # Medical 0.4.0: what a medic does; optional. Whether the item fits a wound is checked against the game's own
    # definitions when the game loads the pack, not here.
    treatments = pack.get('treatments', {})
    if not isinstance(treatments, dict):
        raise Problem(f'{where}/treatments: expected name to entry')
    for key, t in treatments.items():
        w = f'{where}/treatments/{key}'
        if not CARE_NAME.match(key):
            raise Problem(f'{w}: a name is letters, digits, dashes or underscores, at most 48')
        fields(t, {'notes', 'test', 'effect', 'item', 'medicSeconds', 'skill', 'order'}, w)
        if t.get('test') not in CARE_TESTS:
            raise Problem(f'{w}/test: one of {", ".join(CARE_TESTS)}')
        if t.get('effect', 'slot-item') not in CARE_EFFECTS:
            raise Problem(f'{w}/effect: one of {", ".join(CARE_EFFECTS)}')
        if not isinstance(t.get('item'), str) or not CARE_IDENT.match(t['item']):
            raise Problem(f'{w}/item: a game item definition name')
        number(t.get('medicSeconds'), f'{w}/medicSeconds', 5, 1800)
        if 'skill' in t and (not isinstance(t['skill'], str) or t['skill'] and not CARE_IDENT.match(t['skill'])):
            raise Problem(f'{w}/skill: a skill condition name')
        number(t.get('order', 0), f'{w}/order', 0, 1000, integer=True)


OUTCOME_MAX_WEIGHT = 10000


def outcomes(pack, where, recipes=None):
    """The outcomes schema (Framework 0.88.0): chance tables over ordinary recipes of the same mod.

    recipes is the sibling process-recipes pack's recipes (id to entry); without it only the shape is checked."""
    fields(pack, {'schemaVersion', 'schema', 'notes', 'tables'}, where)
    tables = pack.get('tables')
    if not isinstance(tables, dict):
        raise Problem(f'{where}/tables: expected base recipe to table')
    def signature(r):
        ins = sorted((u['id'], u['count'], u['kg']) for u in r.get('inputs', []))
        return (r.get('machine'), tuple(ins), tuple(sorted(r.get('circulates', {}).items())), r.get('seconds'))
    seen = {}
    for key, table in tables.items():
        w = f'{where}/tables/{key}'
        fields(table, {'notes', 'outcomes'}, w)
        entries = table.get('outcomes')
        if not isinstance(entries, dict) or not entries:
            raise Problem(f'{w}/outcomes: expected outcome recipe to weight')
        if key not in entries:
            raise Problem(f'{w}: a table lists its own base recipe among its outcomes')
        total = 0
        for outcome, weight in entries.items():
            number(weight, f'{w}/outcomes/{outcome}', 0, OUTCOME_MAX_WEIGHT, integer=True)
            total += weight
            if outcome in seen:
                raise Problem(f'{w}: {outcome} is already an outcome of {seen[outcome]}')
            seen[outcome] = key
            if recipes is not None:
                if key not in recipes or outcome not in recipes:
                    raise Problem(f'{w}: {outcome if key in recipes else key} is not a recipe')
                if signature(recipes[outcome]) != signature(recipes[key]):
                    raise Problem(f'{w}: {outcome} must have the same machine, inputs, circulating volumes and duration as {key}')
        if total < 1:
            raise Problem(f'{w}: needs at least one outcome with a weight above 0')


LINE_MAX_FORBIDDEN, LINE_MAX_SUPPORTS = 16, 8
CONDITION_NAME = re.compile(r'^[A-Za-z][A-Za-z0-9]{0,63}$')


def lines(pack, where):
    """The lines schema (Framework 0.100.0): where a pipe or conduit segment counts as laid."""
    fields(pack, {'schemaVersion', 'schema', 'notes', 'placement'}, where)
    rules = pack.get('placement')
    if not isinstance(rules, dict) or 'default' not in rules:
        raise Problem(f'{where}/placement: needs a rule named default')
    for key, rule in rules.items():
        w = f'{where}/placement/{key}'
        fields(rule, {'notes', 'forbiddenTiles', 'supports'}, w)
        forbidden = rule.get('forbiddenTiles', [])
        if (not isinstance(forbidden, list) or len(forbidden) > LINE_MAX_FORBIDDEN or len(set(forbidden)) != len(forbidden)
                or any(not isinstance(c, str) or not CONDITION_NAME.match(c) for c in forbidden)):
            raise Problem(f'{w}/forbiddenTiles: up to {LINE_MAX_FORBIDDEN} condition names, each once')
        supports = rule.get('supports')
        if not isinstance(supports, list) or not 1 <= len(supports) <= LINE_MAX_SUPPORTS:
            raise Problem(f'{w}/supports: between 1 and {LINE_MAX_SUPPORTS} supports')
        for n, support in enumerate(supports):
            sw = f'{w}/supports/{n}'
            fields(support, {'notes', 'tile', 'object'}, sw)
            for name in ('tile', 'object'):
                if not isinstance(support.get(name), str) or not CONDITION_NAME.match(support[name]):
                    raise Problem(f'{sw}/{name}: a condition name (object may also be the word floor)')
            if support['tile'] in forbidden:
                raise Problem(f'{w}: forbids {support["tile"]} tiles and also names them as a support')


SCHEMAS = {'economy': economy, 'process-recipes': process_recipes, 'materials': materials, 'vessels': vessels, 'equipment': equipment, 'crops': crops, 'care': care, 'outcomes': outcomes, 'lines': lines}


# ---- Add-ons (Framework 0.90.0): a mod folder with phobos-addon.json and phobos/<Mod>/<schema>/*.json ----
ADDON_RESERVED = ('phobos', 'itm', 'sys', 'stat', 'is')
MIN_PRIORITY, MAX_PRIORITY = -100, 100
DERIVED_REVISION_FLOOR, DERIVED_REVISION_SPAN = 1000000, 1000000000


def stable_hash(ids):
    """Framework's Outcomes.Hash: FNV-1a over the sorted, newline-joined ids, then a final avalanche."""
    h = 2166136261
    for b in '\n'.join(sorted(ids)).encode('utf-8'):
        h ^= b
        h = (h * 16777619) & 0xffffffff
    h ^= h >> 16
    h = (h * 0x85ebca6b) & 0xffffffff
    h ^= h >> 13
    h = (h * 0xc2b2ae35) & 0xffffffff
    h ^= h >> 16
    return h


def derived_revision(recipe_id):
    return DERIVED_REVISION_FLOOR + stable_hash([recipe_id]) % DERIVED_REVISION_SPAN


def version_tuple(text, where):
    parts = str(text).split('.')
    if not 2 <= len(parts) <= 4 or not all(p.isdigit() for p in parts):
        raise Problem(f'{where}: expected a version such as 1.0.0')
    return tuple(int(p) for p in parts)


def addon_manifest(manifest, where='phobos-addon.json'):
    fields(manifest, {'schemaVersion', 'id', 'name', 'author', 'version', 'idPrefix', 'requires', 'notes'}, where)
    if manifest.get('schemaVersion') != 1:
        raise Problem(f'{where}: schemaVersion must be 1')
    ident = manifest.get('id', '')
    if not isinstance(ident, str) or not 3 <= len(ident) <= 48 or any(not (c.islower() and c.isascii() or c.isdigit() or c == '-') for c in ident):
        raise Problem(f'{where}/id: 3 to 48 lower-case letters, digits or hyphens')
    for key in ('name', 'author'):
        if not isinstance(manifest.get(key), str) or not manifest[key].strip():
            raise Problem(f'{where}/{key}: needed')
    version_tuple(manifest.get('version', ''), f'{where}/version')
    prefix = manifest.get('idPrefix', '')
    if (not isinstance(prefix, str) or not 3 <= len(prefix) <= 24 or not prefix.isascii() or not prefix.isalnum() or not prefix[0].isalpha()
            or any(prefix.lower().startswith(r) for r in ADDON_RESERVED)):
        raise Problem(f'{where}/idPrefix: 3 to 24 letters and digits, starting with a letter, and not starting with Phobos, Itm, Sys, Stat or Is')
    requires = manifest.get('requires', {})
    if not isinstance(requires, dict):
        raise Problem(f'{where}/requires: expected mod folder to version')
    for mod, need in requires.items():
        version_tuple(need, f'{where}/requires/{mod}')
    return manifest


def merge(base, overlay, where=''):
    """The loader's merge: objects merge by name, everything else replaces; a null is refused."""
    for key, value in overlay.items():
        if value is None:
            raise Problem(f'{where}/{key}: null; a file can tune or add entries, never remove one')
        if isinstance(value, dict) and isinstance(base.get(key), dict):
            merge(base[key], value, f'{where}/{key}')
        else:
            base[key] = value
    return base


def no_duplicates(pairs):
    seen = {}
    for k, v in pairs:
        if k in seen:
            raise Problem(f'duplicate field {k!r}')
        seen[k] = v
    return seen


def check_addon(folder):
    """Validates an add-on folder against the shipped packs. Returns the list of files that were checked."""
    folder = Path(folder)
    manifest_path = folder / 'phobos-addon.json'
    if not manifest_path.exists():
        raise Problem('phobos-addon.json is missing')
    manifest = addon_manifest(json.loads(manifest_path.read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates))
    prefix = manifest['idPrefix'].lower()
    if not (folder / 'mod_info.json').exists():
        raise Problem('mod_info.json is missing: the game needs it to list the add-on')
    checked = []
    root = folder / 'phobos'
    for mod_dir in sorted(p for p in root.iterdir() if p.is_dir()) if root.exists() else []:
        if mod_dir.name == 'translations':
            for path in sorted(mod_dir.rglob('*.json')):
                rel = path.relative_to(folder).as_posix()
                catalog = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
                if not isinstance(catalog, dict) or any(not isinstance(v, str) or not v.strip() for v in catalog.values()):
                    raise Problem(f'{rel}: a translation file is one object of text by key')
                # A key is one of the mod's own (a translation) or names something of the add-on's (its prefix).
                english = ROOT / 'translations' / path.parent.name / 'en.json'
                if not english.exists():
                    raise Problem(f'{rel}: there is no Phobos mod folder called {path.parent.name}')
                known = json.loads(english.read_text(encoding='utf-8-sig'))
                for key in catalog:
                    if key not in known and not any(part.lower().startswith(prefix) for part in key.split('.')):
                        raise Problem(f'{rel}: {key} is not one of the mod\'s keys and does not carry the id prefix {manifest["idPrefix"]}')
                checked.append(path.relative_to(folder).as_posix())
            continue
        shipped_dir = ROOT / 'mods' / mod_dir.name / 'framework'
        merged_recipes = None
        added_items = set()
        # Materials and recipes first: a crop item or an outcome table may name what the same add-on adds.
        order = {'materials': 0, 'process-recipes': 1, 'outcomes': 3}
        for schema_dir in sorted((p for p in mod_dir.iterdir() if p.is_dir()), key=lambda p: (order.get(p.name, 2), p.name)):
            schema = schema_dir.name
            files = sorted(schema_dir.glob('*.json'), key=lambda p: p.name.lower())
            if schema == 'schematics':
                for path in files:
                    json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
                    checked.append(path.relative_to(folder).as_posix())
                continue
            shipped_path = shipped_dir / f'{schema}.json'
            if schema not in SCHEMAS or not shipped_path.exists():
                raise Problem(f'phobos/{mod_dir.name}/{schema}: {mod_dir.name} has no {schema} pack')
            merged = json.loads(shipped_path.read_text(encoding='utf-8-sig'))
            overlays = []
            for path in files:
                rel = path.relative_to(folder).as_posix()
                if path.stat().st_size > 1048576:
                    raise Problem(f'{rel}: larger than 1 MiB')
                overlay = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
                if not isinstance(overlay, dict):
                    raise Problem(f'{rel}: expected an object')
                if overlay.get('schema', schema) != schema or overlay.get('schemaVersion', 1) != 1:
                    raise Problem(f'{rel}: the folder is for schema {schema}, version 1')
                priority = overlay.pop('priority', 0)
                if isinstance(priority, bool) or not isinstance(priority, int) or not MIN_PRIORITY <= priority <= MAX_PRIORITY:
                    raise Problem(f'{rel}/priority: a whole number from {MIN_PRIORITY} to {MAX_PRIORITY}')
                overlays.append((priority, path.name.lower(), rel, overlay))
            for _, _, rel, overlay in sorted(overlays, key=lambda o: (o[0], o[1])):
                for table, entries in overlay.items():
                    if not isinstance(entries, dict):
                        continue
                    for key in entries:
                        if key not in merged.get(table, {}) and not key.lower().startswith(prefix):
                            raise Problem(f'{rel}: an add-on may add only entries that start with its id prefix; {key} does not start with {manifest["idPrefix"]}')
                if schema == 'process-recipes':
                    for key, recipe in overlay.get('recipes', {}).items():
                        if isinstance(recipe, dict) and 'revision' not in recipe and key not in merged.get('recipes', {}):
                            recipe['revision'] = derived_revision(key)
                merge(merged, overlay, rel)
                try:
                    if schema == 'materials':
                        if mod_dir.name in MATERIAL_ADDITIONS:
                            shipped_ids = set(json.loads(shipped_path.read_text(encoding='utf-8-sig')).get('materials', {}))
                            materials(merged, rel, shipped_ids)
                            for key, m in merged['materials'].items():
                                if key in shipped_ids:
                                    continue
                                added_items.add(key)
                                if m.get('kind', 'stock') not in MATERIAL_ADDITIONS[mod_dir.name]:
                                    raise Problem(f'{rel}: {key} must be of kind {" or ".join(MATERIAL_ADDITIONS[mod_dir.name])}')
                                for suffix in ('.png', 'Normal.png'):
                                    if not (folder / 'images' / (m['image'] + suffix)).exists():
                                        raise Problem(f'{rel}: images/{m["image"]}{suffix} is missing from the add-on (run with --write-normals to make the Normal picture)')
                        else:
                            materials(merged, rel)
                            if any(key not in json.loads(shipped_path.read_text(encoding='utf-8-sig')).get('materials', {}) for key in merged['materials']):
                                raise Problem(f'{rel}: {mod_dir.name} does not take added materials yet')
                    elif schema == 'outcomes':
                        if merged_recipes is None:
                            sibling = shipped_dir / 'process-recipes.json'
                            merged_recipes = json.loads(sibling.read_text(encoding='utf-8-sig')).get('recipes') if sibling.exists() else None
                        outcomes(merged, rel, merged_recipes)
                    elif schema == 'crops':
                        crops(merged, rel, added_items)
                    else:
                        SCHEMAS[schema](merged, rel)
                except Problem as error:
                    raise Problem(str(error)) from None
                checked.append(rel)
            if schema == 'process-recipes':
                merged_recipes = merged.get('recipes')
    return manifest, checked


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
    if schema == 'outcomes':
        # Cross-checked against the same mod's recipes, as the game's loader does.
        sibling = path.with_name('process-recipes.json')
        recipes = json.loads(sibling.read_text(encoding='utf-8-sig')).get('recipes') if sibling.exists() else None
        outcomes(pack, schema, recipes)
    else:
        SCHEMAS[schema](pack, schema)
    return schema


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('paths', nargs='*')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    parser.add_argument('--addon', metavar='FOLDER', help='check an add-on folder against the shipped packs instead')
    parser.add_argument('--write-normals', action='store_true', help="with --addon: write a flat <name>Normal.png beside every picture in the add-on's images folder that lacks one (needs Pillow)")
    args = parser.parse_args()
    if args.addon:
        if args.write_normals:
            from PIL import Image
            for picture in sorted((Path(args.addon) / 'images').rglob('*.png')):
                target = picture.with_name(picture.stem + 'Normal.png')
                if picture.stem.endswith('Normal') or target.exists():
                    continue
                source = Image.open(picture).convert('RGBA')
                flat = Image.new('RGBA', source.size, (128, 128, 255, 0))
                flat.putalpha(source.getchannel('A'))
                flat.save(target)
                print('wrote', target)
        try:
            manifest, checked = check_addon(args.addon)
        except (Problem, ValueError, OSError) as error:
            print(json.dumps({'status': 'invalid', 'error': str(error)}, indent=2) if args.format == 'json' else f'ERR {error}')
            return 1
        if args.format == 'json':
            print(json.dumps({'status': 'valid', 'id': manifest['id'], 'files': checked}, indent=2))
        else:
            for name in checked:
                print('ok ', name)
            print(f"Add-on {manifest['id']}: {len(checked)} file(s) valid against the shipped packs. The game's own loader remains authoritative.")
        return 0
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
