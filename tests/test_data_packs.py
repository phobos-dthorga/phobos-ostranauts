"""Offline data-pack checks: the shipped packs validate, the recipe freeze is current, and the Python hash
agrees with the C# RecipeFreeze canonical form on a fixed sample (the same sample and digest are asserted in
tests/PhobosFramework.Tests/DataPackChecks.cs)."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load(name):
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), ROOT / 'scripts' / f'{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


validate = load('validate-data-packs')
freeze = load('freeze-recipes')
schemas = load('check-json-schemas')
writer = load('write-json-schemas')

SAMPLE = {"notes": "x", "machine": "test", "revision": 1, "inputs": [{"id": "A", "count": 2, "kg": 1.5}],
          "products": [{"id": "B", "count": 1, "kg": 3}], "melt": False}
SAMPLE_CANONICAL = '{"inputs":[{"count":2,"id":"A","kg":1.5}],"machine":"test","melt":false,"products":[{"count":1,"id":"B","kg":3}],"revision":1}'
SAMPLE_HASH = '49f8679fceadf55956c05d1013dad41f13c41b907a56bf53add8faee176b5381'


class DataPackTests(unittest.TestCase):
    def test_shipped_packs_validate(self):
        for path in sorted((ROOT / 'mods').glob('*/framework/*.json')):
            with self.subTest(path=path.name):
                validate.check_file(path)

    def test_shipped_packs_match_json_schemas(self):
        for path in sorted((ROOT / 'mods').glob('*/framework/*.json')):
            found = schemas.check_file(path)
            if found is None:
                continue
            with self.subTest(path=path.name):
                self.assertEqual(found, [])

    def test_json_schemas_are_current_and_refuse_drift(self):
        for name in writer.SCHEMAS:
            with self.subTest(schema=name):
                self.assertEqual((ROOT / 'schemas' / f'{name}.schema.json').read_text(encoding='utf-8'), writer.render(name))
        bad = {'schemaVersion': 1, 'schema': 'materials', 'materials': {'m': {'kg': 1, 'price': 1, 'colour': 'red'}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('materials')), bad))
        bad_economy = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {'PhobosX': {'price': 1, 'work': {'dismantle': 1}, 'kind': 'furniture'}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('economy')), bad_economy))

    def test_freeze_is_current(self):
        result = subprocess.run([sys.executable, str(ROOT / 'scripts/freeze-recipes.py'), '--check', '--format', 'json'], capture_output=True, text=True, cwd=ROOT)
        report = json.loads(result.stdout)
        self.assertEqual(report['status'], 'valid', report)
        self.assertTrue(report['packs'], 'no process-recipe packs found')

    def test_canonical_hash_matches_csharp_sample(self):
        self.assertEqual(freeze.canonical(SAMPLE), SAMPLE_CANONICAL)
        self.assertEqual(freeze.entry_hash(SAMPLE), SAMPLE_HASH)

    def test_validator_refuses_mass_creation(self):
        pack = {'schemaVersion': 1, 'schema': 'process-recipes', 'recipes': {'x': {'machine': 'm', 'revision': 1,
                'inputs': [{'id': 'a', 'count': 1, 'kg': 1}], 'products': [{'id': 'b', 'count': 1, 'kg': 2}]}}}
        with self.assertRaises(validate.Problem):
            validate.process_recipes(pack, 'test')
        pack['recipes']['x']['products'][0]['kg'] = 1
        validate.process_recipes(pack, 'test')
        pack['recipes']['x']['offGas'] = {'H2': 0.1}
        with self.assertRaises(validate.Problem):
            validate.process_recipes(pack, 'test')

    def test_validator_checks_supersession(self):
        # Framework 0.68.0: a later revision may replace one earlier revision of its own machine, and only one recipe may.
        def pack(*supersedes):
            unit = {'inputs': [{'id': 'a', 'count': 1, 'kg': 1}], 'products': [{'id': 'b', 'count': 1, 'kg': 1}]}
            recipes = {'old': {'machine': 'm', 'revision': 1, **unit}}
            for i, earlier in enumerate(supersedes):
                recipes[f'new{i}'] = {'machine': 'm', 'revision': 2 + i, 'supersedes': earlier, **unit}
            return {'schemaVersion': 1, 'schema': 'process-recipes', 'recipes': recipes}
        validate.process_recipes(pack([1]), 'test')
        for bad in (pack([2]), pack([3]), pack([0]), pack([1], [1]), pack('1')):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.process_recipes(bad, 'test')

    def test_faction_kiosks_sell_everything_below_honored(self):
        # Owner direction (30 September 2026): every sold Phobos item is also at the faction kiosks, never at Honored.
        for path in sorted((ROOT / 'mods').glob('*/framework/economy.json')):
            pack = json.loads(path.read_text(encoding='utf-8'))
            with self.subTest(pack=path.parent.parent.name):
                kiosks = pack.get('factionKiosks')
                self.assertIsNotNone(kiosks, 'no faction-kiosk section')
                tiers = kiosks['tiers']
                self.assertNotIn('Honored', tiers.values())
                equipment = pack.get('equipment', {})

                def family(item):
                    # An exact key first, then the longest machine prefix (PhobosShipbreaker must not claim its section).
                    if item in equipment:
                        return item
                    found = [k for k, e in equipment.items() if (e.get('forms', 'machine') == 'machine' and item.startswith(k))
                             or (e.get('forms') == 'item' and item == k + 'Dmg')]
                    return max(found, key=len) if found else item
                sold = {k for k, e in equipment.items() if e.get('offers', True)} | set(pack.get('supplies', {}))
                sold |= set((pack.get('regional') or {}).get('items', {}))
                sold |= {family(o['item']) for o in pack.get('offers', {}).values()}
                self.assertEqual(sorted(sold - set(tiers)), [], 'sold elsewhere but not at the faction kiosks')

    def test_validator_refuses_bad_faction_tiers(self):
        pack = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {}, 'factionKiosks': {'merchants': ['ItmFactionKioskBCRSInv'], 'tiers': {'PhobosX': 'Revered'}}}
        with self.assertRaises(validate.Problem):
            validate.economy(pack, 'test')
        bad = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {}, 'factionKiosks': {'merchants': [], 'tiers': {}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('economy')), bad))

    def test_crops_must_conserve_mass_and_stay_frozen(self):
        # Agriculture 0.40.0: crops are a data pack. Every file balances, and a published crop never changes.
        path = ROOT / 'mods/PhobosAgriculture/framework/crops.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.crops(pack, 'test')
        frozen = json.loads((path.with_name('frozen-crops.json')).read_text(encoding='utf-8'))['revisions']
        self.assertEqual(sorted(frozen), sorted(pack['crops']))
        for crop_id, entry in pack['crops'].items():
            self.assertEqual(freeze.entry_hash(entry), frozen[crop_id], crop_id)
        for field, value in (('waterKg', 9), ('edibleKg', 99), ('feed', 'lettuce-v1'), ('produce', 'ItmScrapSteel'), ('keptStockKg', 0.1)):
            bad = json.loads(json.dumps(pack))
            bad['crops']['potato'][field] = value
            with self.subTest(field=field), self.assertRaises(validate.Problem):
                validate.crops(bad, 'test')
        bad = json.loads(json.dumps(pack))
        bad['crops']['potato']['yield'] = 1
        with self.assertRaises(validate.Problem):
            validate.crops(bad, 'test')
        self.assertTrue(schemas.problems(json.loads(writer.render('crops')), bad))

    def test_care_pack_keeps_thresholds_below_the_game_limits(self):
        # Phobos Medical 0.1.0: the care pack holds station power and admission thresholds; the healing stays the game's.
        path = ROOT / 'mods/PhobosMedical/framework/care.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.care(pack, 'test')
        self.assertEqual(schemas.problems(json.loads(writer.render('care')), pack), [])
        for section, field, value in (('admission', 'bloodLost', 40), ('admission', 'pain', 0), ('admission', 'dischargeShare', 1),
                                      ('admission', 'wound', 1.5), ('stations', 'bed', {'idleKW': 0.5, 'workingKW': 0.1}),
                                      ('stations', 'bed', {'idleKW': 0, 'workingKW': 3}), ('stations', 'autodoc', {'idleKW': 0, 'workingKW': 0.1})):
            bad = json.loads(json.dumps(pack))
            bad[section][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(validate.Problem):
                validate.care(bad, 'test')
        for value in (0.01, 1.5):
            bad = json.loads(json.dumps(pack))
            bad['levels']['bed']['weightlessHealing'] = value
            with self.subTest(weightless=value), self.assertRaises(validate.Problem):
                validate.care(bad, 'test')
        bad = json.loads(json.dumps(pack)); bad['alerts']['pain'] = 75
        with self.assertRaises(validate.Problem):
            validate.care(bad, 'test')
        legacy = json.loads(json.dumps(pack)); del legacy['levels']; del legacy['alerts']
        validate.care(legacy, 'test')
        bad = json.loads(json.dumps(pack))
        bad['admission']['heal'] = 1
        with self.assertRaises(validate.Problem):
            validate.care(bad, 'test')
        self.assertTrue(schemas.problems(json.loads(writer.render('care')), bad))

    def test_validator_refuses_unknown_fields(self):
        pack = {'schemaVersion': 1, 'schema': 'materials', 'materials': {'m': {'kg': 1, 'price': 1, 'colour': 'red'}}}
        with self.assertRaises(validate.Problem):
            validate.materials(pack, 'test')


if __name__ == '__main__':
    unittest.main()
