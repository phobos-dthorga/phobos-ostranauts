#!/usr/bin/env python3
"""Economy audit across every Phobos Ostranauts mod.

Reads the generated item evidence (docs/item-reference-data.json: values, merchant offers, kiosk tiers, loot,
repair, dismantle and restore jobs), every mod's data packs (process recipes, crops, construction recipes,
economy) and the game's own definitions (native prices, merchant inventories, gas prices), and writes one
Markdown file of tables: coverage, kiosk tiers, world finds, dismantling, repair margins beside the game's own,
construction recipes, process recipes, crops, machine payback and price rows the documents lack.

The tables are evidence for a human review, not verdicts: a ratio above 1 can be an owner-approved exception.
Run it after scripts/update-item-reference.ps1, which refreshes the evidence. Read-only: it changes no source.

Usage: audit-economy.py [--game <Ostranauts folder>] [--out <file>]
The game folder defaults to OstranautsPath in .local/install-settings.json.
"""
import argparse
import glob
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# The game's own trade factors (docs/development/vanilla-economy-audit.md, "Merchant multipliers").
KIOSK_BUY = (0.4, 0.5)      # a kiosk buying from the player
FIXER_BUY = (0.5, 0.9)      # the K-Leg fixer buying intact high-salvage equipment
SCRAP_SELL = (1.2, 1.5)     # scrap kiosks selling to the player (broken machines are offered there)
BUYBACK = 0.45              # Framework BulkSupplies.BuybackShare: bulk sold back from installed stores
# Bulk commodities priced per kilogram, as the code prices them.
FIXED = {'water': 10.0, 'ethanol': 20.0, 'crop nutrients': 1500.0}
GAS = {'hydrogen': 'H2', 'methane': 'CH4', 'oxygen': 'O2', 'nitrogen': 'N2', 'carbon dioxide': 'CO2',
       'ammonia': 'NH3', 'carbon monoxide': 'CO', 'sulfuric acid': 'H2SO4'}
# Game items whose price a Phobos mod amends at load (the evidence holds only Phobos items).
NATIVE_OVERRIDES = {'ItmIce02': ('src/PhobosShipbreaker/Core/ThawRules.cs', r'MethaneIcePrice\s*=\s*([0-9.]+)')}
# Repair input triggers and the native item each stands for.
TRIGGERS = {'TIsScrapSteel': 'ItmScrapSteel', 'TIsScrapAluminum': 'ItmScrapAluminum', 'TIsPartsMechSmall': 'ItmPartsMechSmall01',
            'TIsPartsElecSmall': 'ItmPartsElecSmall01', 'TIsMotor': 'ItmComponentMotor01', 'TIsMobo': 'ItmComponentMobo01',
            'TIsHeatSink': 'ItmHeatSink01', 'TIsScreen': 'ItmPartsScreen01', 'TIsScrapCarbonFiber': 'ItmScrapCarbonFiber',
            'TIsScrapClothClean': 'ItmScrapClothClean'}
VANILLA_REPAIRS = ['ItmAICargo01LooseDmg', 'ItmFusionMHDGenerator01DmgLoose', 'ItmBedMedical01DmgLoose', 'ItmRCSCluster01DmgLoose',
                   'ItmAirPump02DmgLoose', 'ItmTowingBrace01DmgLoose']


def load(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def read_all(folder):
    out = {}
    for f in glob.glob(str(folder / '*.json')):
        try:
            arr = load(f)
        except Exception:
            continue
        for o in arr if isinstance(arr, list) else []:
            if isinstance(o, dict) and 'strName' in o:
                out[o['strName']] = o
    return out


class Audit:
    def __init__(self, game):
        data = game / 'Ostranauts_Data/StreamingAssets/data'
        self.native = read_all(data / 'condowners')
        self.loot = read_all(data / 'loot')
        self.installables = read_all(data / 'installables')
        self.gas = {e.split('=')[0]: float(e.split('x')[1]) for e in self.loot['GasPrices']['aCOs']}
        ev = load(ROOT / 'docs/item-reference-data.json')
        self.items, self.alias, self.sources = {}, {}, defaultdict(list)
        for m in ev['mods']:
            for it in m['items']:
                it['_mod'] = m['id'].replace('Phobos', '')
                self.items[it['id']] = it
                for a in it.get('aliases') or []:
                    self.alias[a] = it['id']
            for s in m['sources']:
                self.sources[self.alias.get(s['item'], s['item'])].append(s)
        self.overrides = {}
        for item, (path, pattern) in NATIVE_OVERRIDES.items():
            found = re.search(pattern, (ROOT / path).read_text(encoding='utf-8'))
            if found:
                self.overrides[item] = float(found.group(1))
        self.packs = {}
        for p in glob.glob(str(ROOT / 'mods/*/framework/*.json')):
            self.packs[(Path(p).parts[-3], Path(p).stem)] = load(p)
        self.materials = {}
        for (mod, name), d in self.packs.items():
            if name == 'materials':
                for k, v in d.get('materials', {}).items():
                    self.materials[k] = v
        self.native_sold = self._merchant_stock()
        self.retired = self._retired_recipes()
        self.lines = []

    # ---- prices
    def native_price(self, item):
        if item in self.overrides:
            return self.overrides[item]
        o = self.native.get(item)
        for c in (o or {}).get('aStartingConds', []) or []:
            if c.startswith('StatBasePrice='):
                return float(c.split('x')[-1])
        return None

    def price(self, item):
        item = self.alias.get(item, item)
        if item in self.items:
            return self.items[item].get('baseValue')
        return self.native_price(item)

    def commodity(self, item):
        if item in FIXED:
            return FIXED[item]
        if item in GAS:
            return self.gas[GAS[item]]
        return None

    def value(self, item, count, kg):
        per_kg = self.commodity(item)
        if per_kg is not None:
            return per_kg * count * kg
        p = self.price(item)
        return None if p is None else p * count

    # ---- availability
    def _merchant_stock(self):
        memo = {}

        def reach(name, depth=0):
            if name in memo:
                return memo[name]
            memo[name] = set()
            entry = self.loot.get(name)
            if entry is None or depth > 10:
                return memo[name]
            found = set()
            for e in (entry.get('aCOs') or []) + (entry.get('aLoots') or []):
                key = e.split('=')[0].lstrip('-')
                found |= reach(key, depth + 1) if key in self.loot and key != name else {key}
            memo[name] = found
            return found
        roots = [n for n in self.loot if re.search(r'(Kiosk|Trader|Fixer|Vendor|Shop|Cart|Prospector).*Inv$|Fixer$', n)]
        stock = set()
        for r in roots:
            stock |= reach(r)
        return stock

    def bought(self, item):
        item = self.alias.get(item, item)
        if item in self.items:
            return any(s.get('condition') != 'Loot' for s in self.sources.get(item, []))
        return item in self.native_sold

    def _retired_recipes(self):
        text = (ROOT / 'src/PhobosShipbreaker/AssemblyDefinitions.cs').read_text(encoding='utf-8')
        found = set()
        for name in ('Legacy', 'RetiredSectionRecipes'):
            block = re.search(name + r'\s*=\s*\{([^}]*)\}', text)
            if block:
                found |= set(re.findall(r'"([A-Za-z0-9]+)"', block.group(1)))
        return found

    # ---- output
    def h(self, title, note=None):
        self.lines += ['', '## ' + title, '']
        if note:
            self.lines += [note, '']

    def table(self, head, rows):
        self.lines.append('| ' + ' | '.join(head) + ' |')
        self.lines.append('|' + '---|' * len(head))
        for r in rows:
            self.lines.append('| ' + ' | '.join(fmt(x) for x in r) + ' |')
        if not rows:
            self.lines.append('| ' + ' | '.join(['none'] + [''] * (len(head) - 1)) + ' |')

    def kind(self, item):
        it = self.items[item]
        mat = self.materials.get(item, {})
        if it.get('installed'):
            return 'installed'
        if item.endswith('InputBin') or item.startswith('Sys'):
            return 'internal'
        if mat.get('terminal') or 'Waste' in item or 'Trash' in (mat.get('category') or '') or (it.get('baseValue') or 0) <= 0.011:
            return 'remainder'
        return 'loose'

    # ---- sections
    def coverage(self):
        self.h('1. Coverage', 'Loose identities only (installed forms are never sold; internal compartments and system objects are left out). '
               '"Sold" counts merchant offers outside the faction kiosks, "found" world loot.')
        agg = defaultdict(lambda: [0, 0, 0, 0])
        never = []
        for item in sorted(self.items):
            k = self.kind(item)
            if k != 'loose':
                continue
            src = self.sources.get(item, [])
            sold = any(s.get('condition') != 'Loot' and 'FactionKiosk' not in s['table'] for s in src)
            found = any(s.get('condition') == 'Loot' for s in src)
            a = agg[self.items[item]['_mod']]
            a[0] += 1; a[1] += sold; a[2] += found; a[3] += not sold and not found
            if not sold and not found:
                never.append((self.items[item]['_mod'], item, self.items[item].get('baseValue')))
        self.table(['Mod', 'Loose identities', 'Sold', 'Found', 'Neither'], [(m, *v) for m, v in sorted(agg.items())])
        self.lines += ['', 'Loose identities neither sold nor found (made aboard, legacy, or a gap to review):', '']
        self.table(['Mod', 'Item', 'Value'], never)

    def tiers(self):
        self.h('2. Faction kiosk tiers', 'Every family offered at retail should have a tier, and none may need Honored.')
        fam = lambda i: re.sub(r'(Installed|Loose)?(Dmg)?$', '', i)
        retail, tiers = set(), defaultdict(set)
        for item, src in self.sources.items():
            for s in src:
                if 'FactionKiosk' in s['table']:
                    tiers[fam(item)].add(s['condition'].split(', ')[-1])
                elif s.get('condition') != 'Loot':
                    retail.add(fam(item))
        missing = sorted(f for f in retail if f not in tiers)
        honored = sorted(f for f, t in tiers.items() if any('Honored' in x for x in t))
        self.table(['Check', 'Result'], [('Families sold at retail without a tier', ', '.join(missing) or 'none'),
                                          ('Families needing Honored', ', '.join(honored) or 'none'),
                                          ('Tiers in use', ', '.join(f'{k} {v}' for k, v in Counter(t for s in tiers.values() for t in s).most_common()))])

    def finds(self):
        self.h('3. World finds', 'Every loot table that can yield a Phobos identity, with the chance the evidence records for it.')
        rows = defaultdict(list)
        for item, src in self.sources.items():
            for s in src:
                if s.get('condition') == 'Loot':
                    rows[s['table']].append(f"{item} {s.get('chance', 0):.3g}")
        self.table(['Loot table', 'Identities', 'Entries'], [(t, len(v), '; '.join(sorted(v))) for t, v in sorted(rows.items())])
        # Each mod's branch is its own extra link on the game's table, so the odds stack rather than share one roll.
        ev = defaultdict(lambda: [0.0, 0.0])
        for item, src in self.sources.items():
            for s in src:
                if s.get('condition') == 'Loot' and s['table'] == 'ItmLootSpawnEngineering':
                    e = ev[self.items[item]['_mod'] if item in self.items else '?']
                    e[0] += s.get('chance', 0)
                    e[1] += s.get('chance', 0) * (self.price(item) or 0)
        none = 1.0
        for chance, _ in ev.values():
            none *= max(0.0, 1 - chance)
        self.lines += ['', "What each engineering-salvage roll adds, per mod. Each mod's branch is an extra link on the game's table, so "
                       "the chances stack. The game's own roll is 0.8 of its random engineering loot or 0.1 of trash.", '']
        self.table(['Mod', 'Chance of a Phobos item', 'Expected base value added'], [(m, v[0], v[1]) for m, v in sorted(ev.items())] +
                   [('All mods', 1 - none, sum(v[1] for v in ev.values()))])

    def dismantling(self):
        self.h('4. Dismantling', 'Breaking an item into materials must lose value against selling it whole (owner rule, 24 September 2026).')
        rows = []
        for item, it in sorted(self.items.items()):
            v = it.get('baseValue') or 0
            for j in it['jobs']:
                if j['kind'] == 'dismantle' and j['outputs'] and v > 0.011:
                    out = sum(o.get('baseValue') or 0 for o in j['outputs'])
                    if out >= v:
                        rows.append((it['_mod'], item, v, out, out / v))
        self.table(['Mod', 'Item', 'Value', 'Outputs', 'Ratio'], rows)

    def bill(self, inputs):
        total, unknown = 0.0, []
        for inp in inputs:
            trig, qty = inp.split('=')
            n = int(float(qty.split('x')[1]))
            native = TRIGGERS.get(trig)
            if native is None:
                unknown.append(trig)
                continue
            total += n * (self.price(native) or 0)
        return total, unknown

    def repairs(self):
        self.h('5. Repair margins',
               'Buy a broken machine from a scrap kiosk (1.2 to 1.5 x its broken value), buy its repair bill at the same factor, and sell it '
               'whole to the K-Leg fixer (0.5 to 0.9 x). The range is worst to best. The game\'s own equipment is listed below ours for comparison.')
        rows, unknown = [], Counter()
        for item, it in sorted(self.items.items()):
            if not (item.endswith('Dmg') and 'Loose' in item):
                continue
            whole = self.price(item[:-3])
            for j in it['jobs']:
                if j['kind'] != 'repair' or not whole:
                    continue
                cost, missing = self.bill(j['inputs'])
                unknown.update(missing)
                broken = it.get('baseValue') or 0
                worst = whole * FIXER_BUY[0] - (broken + cost) * SCRAP_SELL[1]
                best = whole * FIXER_BUY[1] - (broken + cost) * SCRAP_SELL[0]
                sold_broken = any(s.get('condition') == 'Broken' for s in self.sources.get(item, []))
                rows.append((it['_mod'], item[:-8], whole, broken, cost, worst, best, 'yes' if sold_broken else ''))
        rows.sort(key=lambda r: -r[6])
        self.table(['Mod', 'Family', 'Whole', 'Broken', 'Repair bill', 'Worst margin', 'Best margin', 'Broken form sold'], rows)
        vanilla = []
        for dmg in VANILLA_REPAIRS:
            job = next((o for o in self.installables.values() if o.get('strActionCO') == dmg and 'Repair' in o['strName']), None)
            whole = self.native_price(dmg.replace('DmgLoose', 'Loose').replace('LooseDmg', 'Loose'))
            broken = self.native_price(dmg)
            if job and whole and broken is not None:
                cost, _ = self.bill(job.get('aInputs') or [])
                vanilla.append(('game', dmg, whole, broken, cost, whole * FIXER_BUY[0] - (broken + cost) * SCRAP_SELL[1],
                                whole * FIXER_BUY[1] - (broken + cost) * SCRAP_SELL[0], ''))
        self.lines += ['', 'The game\'s own equipment, the same way:', '']
        self.table(['Source', 'Item', 'Whole', 'Broken', 'Repair bill', 'Worst margin', 'Best margin', ''], vanilla)
        if unknown:
            self.lines += ['', 'Repair inputs not valued: ' + ', '.join(f'{k} x{v}' for k, v in unknown.items())]

    def construction(self):
        self.h('6. Construction recipes',
               'Table and bench builds still offered (retired section recipes are left out). "Build and sell" buys the parts at 1.3 x '
               'and sells the product to a kiosk at 0.5 x.')
        rows = []
        for (mod, name), d in sorted(self.packs.items()):
            if name != 'recipes':
                continue
            for r in d.get('recipes', []):
                if r['id'] in self.retired:
                    continue
                parts = sum((self.price(g['item']) or 0) * g['count'] for g in r.get('ingredients', []))
                product = sum((self.price(o['item']) or 0) * o['count'] for o in r.get('outputs', []))
                rows.append((mod.replace('Phobos', ''), r['id'], parts, product, product / parts if parts else 0,
                             product * KIOSK_BUY[1] - parts * 1.3, (r.get('workSeconds') or 0) / 60))
        rows.sort(key=lambda r: -r[5])
        self.table(['Mod', 'Recipe', 'Parts', 'Product', 'Ratio', 'Build and sell', 'Minutes'], rows)

    def processes(self):
        self.h('7. Process recipes',
               'At base value. Bulk is valued at the station price per kilogram. "Bought loop" buys every item input at 1.2 x and sells '
               'every product back at 0.45 x (bulk) or 0.5 x (items); positive means a repeatable trade gains. Outcome-table recipes are '
               'judged over their table by the native value check, not row by row.')
        rows, self.best = [], {}
        for (mod, name), d in sorted(self.packs.items()):
            if name != 'process-recipes':
                continue
            for rid, r in d.get('recipes', {}).items():
                ins = [(u['id'], u['count'], u['kg']) for u in r.get('inputs', [])]
                outs = [(u['id'], u['count'], u['kg']) for u in r.get('products', [])]
                iv = [self.value(*u) for u in ins]
                ov = [self.value(*u) for u in outs]
                hours = (r.get('seconds') or 0) / 3600
                if None in iv or None in ov:
                    missing = [u[0] for u, v in zip(ins + outs, iv + ov) if v is None]
                    rows.append((mod.replace('Phobos', ''), r.get('machine'), rid, 'unpriced: ' + ', '.join(missing), '', '', '', '', hours))
                    continue
                I, O = sum(iv), sum(ov)
                item_inputs = [u[0] for u in ins if self.commodity(u[0]) is None]
                all_bought = bool(item_inputs) and all(self.bought(x) for x in item_inputs)
                sale = sum(v * (BUYBACK if self.commodity(u[0]) is not None else KIOSK_BUY[1]) for u, v in zip(outs, ov))
                raw = sum(v * (BUYBACK if self.commodity(u[0]) is not None else KIOSK_BUY[1]) for u, v in zip(ins, iv))
                loop = sale - I * 1.2 if all_bought else None
                rows.append((mod.replace('Phobos', ''), r.get('machine'), rid, I, O, O / I if I else 0, 'yes' if all_bought else '',
                             '' if loop is None else loop, hours))
                if hours > 0:
                    key = (mod.replace('Phobos', ''), r.get('machine'))
                    gain = (O - I) / hours
                    if key not in self.best or gain > self.best[key][1]:
                        self.best[key] = (rid, gain, (sale - raw) / hours)
        self.table(['Mod', 'Machine', 'Recipe', 'In', 'Out', 'Out/in', 'All item inputs bought', 'Bought loop', 'Hours'], rows)

    def crops(self):
        crops = self.packs.get(('PhobosAgriculture', 'crops'))
        if not crops:
            return
        self.h('8. Crops', 'One harvest, at base value: produce against the water and crop nutrients it took at station prices. '
               'Seed stock kept back is not counted either way. The rack itself is the cost of space and power.')
        rows = []
        for cid, c in crops['crops'].items():
            produce = c.get('produce')
            unit = (self.materials.get(produce) or {}).get('kg') or c.get('portionKg') or 1
            units = (c.get('edibleKg') or 0) / unit
            out = units * (self.price(produce) or 0)
            water = (c.get('waterKg') or 0) * FIXED['water']
            feed = (c.get('nutrientKg') or 0) * FIXED['crop nutrients']
            hours = c.get('hours') or 0
            rows.append((cid, produce, units, out, water, feed, out - water - feed, hours, (out - water - feed) / hours if hours else 0))
        self.table(['Crop', 'Produce', 'Units', 'Harvest', 'Water', 'Nutrients', 'Net', 'Hours', 'Net per hour'], rows)

    def payback(self):
        self.h('9. Machine payback', "Each machine's best recipe by base gain per working hour, and what processing adds per hour over "
               'selling the same inputs raw, both at kiosk factors (0.5 x items, 0.45 x bulk). Compare with the machine prices below.')
        self.table(['Mod', 'Machine', 'Best recipe', 'Base gain per hour', 'Gain over selling raw, per hour'],
                   [(k[0], k[1], v[0], v[1], v[2]) for k, v in sorted(self.best.items())])
        prices = []
        for (mod, name), d in sorted(self.packs.items()):
            if name == 'economy':
                for k, v in d.get('equipment', {}).items():
                    if v.get('price'):
                        prices.append((mod.replace('Phobos', ''), k, v['price']))
        self.lines += ['']
        self.table(['Mod', 'Equipment (small size)', 'Price'], sorted(prices, key=lambda r: -r[2]))

    def documents(self):
        self.h('10. Price rows the documents lack', 'Each priced machine model named in an equipment-names pack should appear in '
               'docs/equipment-economy.md.')
        doc = (ROOT / 'docs/equipment-economy.md').read_text(encoding='utf-8')
        rows = []
        for (mod, name), d in sorted(self.packs.items()):
            if name != 'equipment-names':
                continue
            for key, v in d.items():
                if not isinstance(v, dict) or not v.get('model') or not key.endswith('.name'):
                    continue
                if v['model'] not in doc:
                    rows.append((mod.replace('Phobos', ''), key, f"{v.get('brand')} {v['model']}"))
        self.table(['Mod', 'Name key', 'Model missing from docs/equipment-economy.md'], rows)

    def run(self, out):
        self.lines = ['# Economy audit tables', '',
                      'Generated by `scripts/audit-economy.py` from docs/item-reference-data.json, the mods\' data packs and the game\'s '
                      'own definitions. Evidence for review, not verdicts; see the dated audit record for findings.']
        for section in (self.coverage, self.tiers, self.finds, self.dismantling, self.repairs, self.construction, self.processes,
                        self.crops, self.payback, self.documents):
            section()
        out.write_text('\n'.join(self.lines) + '\n', encoding='utf-8')
        return out


def fmt(x):
    if isinstance(x, float):
        return f'{x:,.2f}'
    return str(x)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--game')
    parser.add_argument('--out', default=str(ROOT / 'docs/development/economy-audit-tables.md'))
    args = parser.parse_args()
    game = args.game or load(ROOT / '.local/install-settings.json')['OstranautsPath']
    out = Audit(Path(game)).run(Path(args.out))
    print(f'Wrote {out.relative_to(ROOT) if out.is_relative_to(ROOT) else out}. Read-only: no source or game file changed.')


if __name__ == '__main__':
    main()
