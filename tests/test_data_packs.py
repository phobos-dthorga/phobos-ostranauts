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

SAMPLE = {"notes": "x", "machine": "test", "revision": 1, "inputs": [{"id": "A", "count": 2, "kg": 1.5}],
          "products": [{"id": "B", "count": 1, "kg": 3}], "melt": False}
SAMPLE_CANONICAL = '{"inputs":[{"count":2,"id":"A","kg":1.5}],"machine":"test","melt":false,"products":[{"count":1,"id":"B","kg":3}],"revision":1}'
SAMPLE_HASH = '49f8679fceadf55956c05d1013dad41f13c41b907a56bf53add8faee176b5381'


class DataPackTests(unittest.TestCase):
    def test_shipped_packs_validate(self):
        for path in sorted((ROOT / 'mods').glob('*/framework/*.json')):
            with self.subTest(path=path.name):
                validate.check_file(path)

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

    def test_validator_refuses_unknown_fields(self):
        pack = {'schemaVersion': 1, 'schema': 'materials', 'materials': {'m': {'kg': 1, 'price': 1, 'colour': 'red'}}}
        with self.assertRaises(validate.Problem):
            validate.materials(pack, 'test')


if __name__ == '__main__':
    unittest.main()
