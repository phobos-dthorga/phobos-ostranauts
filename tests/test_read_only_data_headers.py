import importlib.util
import json
from pathlib import Path
import re
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('headers', ROOT / 'scripts/read-only-data-headers.py')
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)


class ReadOnlyHeaderTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.put('src/PhobosExample/PhobosExample.csproj', '<Project>\n'
                 '  <ItemGroup><EmbeddedResource Include="../../mods/PhobosExample/framework/economy.json" LogicalName="PhobosExample.economy.json" /></ItemGroup>\n'
                 '  <ItemGroup><EmbeddedResource Include="../../mods/PhobosExample/framework/equipment-names.json" LogicalName="PhobosExample.equipment-names.json" /></ItemGroup>\n'
                 '  <ItemGroup><EmbeddedResource Include="../../translations/PhobosExample/en.json" LogicalName="PhobosExample.en.json" /></ItemGroup>\n'
                 '</Project>\n')
        self.economy = '{\r\n  "schemaVersion": 1,\r\n  "schema": "economy",\r\n  "notes": "Prices."\r\n}\r\n'.encode('utf-8')
        self.names = b'\xef\xbb\xbf{\n  "Example.name": { "brand": "Rivetline", "model": "Q1" }\n}\n'
        self.put_bytes('mods/PhobosExample/framework/economy.json', self.economy)
        self.put_bytes('mods/PhobosExample/framework/equipment-names.json', self.names)
        self.put('mods/PhobosExample/framework/recipes.json', '{"schemaVersion": 1, "recipes": []}')

    def put(self, path, text):
        self.put_bytes(path, text.encode('utf-8'))

    def put_bytes(self, path, data):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def test_only_embedded_files_are_read_only(self):
        # recipes.json is read live from the mod folder, so it is not a read-only copy.
        self.assertEqual(h.read_only_files(self.root, 'PhobosExample'), ['framework/economy.json', 'framework/equipment-names.json'])
        self.assertEqual(h.read_only_files(self.root, 'PhobosMissing'), [])

    def test_note_is_the_first_line_and_the_rest_is_unchanged(self):
        stamped = h.stamped('PhobosExample', 'framework/economy.json', self.economy)
        first, rest = stamped.split(b'\r\n', 1)
        self.assertTrue(first.startswith(b'// Read-only copy.'))
        self.assertIn(b'PhobosExample.dll', first)
        self.assertIn(b'BepInEx/config/PhobosExample/economy/', first)
        self.assertEqual(rest, self.economy)
        self.assertEqual(json.loads(rest)['schema'], 'economy')
        with self.assertRaisesRegex(ValueError, 'already carries'):
            h.stamped('PhobosExample', 'framework/economy.json', stamped)

    def test_byte_order_mark_stays_first(self):
        stamped = h.stamped('PhobosExample', 'framework/equipment-names.json', self.names)
        self.assertTrue(stamped.startswith(b'\xef\xbb\xbf// Read-only copy.'))
        self.assertIn(b'cannot be overridden', stamped.split(b'\n', 1)[0])
        self.assertEqual(stamped.split(b'\n', 1)[1], self.names[3:])

    def test_stamp_package(self):
        package = self.root / 'dist/PhobosExample-P0'
        for name in ('economy.json', 'equipment-names.json', 'recipes.json'):
            self.put_bytes(f'dist/PhobosExample-P0/Mods/PhobosExample/framework/{name}', (self.root / 'mods/PhobosExample/framework' / name).read_bytes())
        self.assertEqual(h.stamp(self.root, 'PhobosExample', package), ['framework/economy.json', 'framework/equipment-names.json'])
        native = package / 'Mods/PhobosExample/framework'
        self.assertTrue((native / 'economy.json').read_bytes().startswith(b'// Read-only copy.'))
        self.assertEqual((native / 'recipes.json').read_bytes(), (self.root / 'mods/PhobosExample/framework/recipes.json').read_bytes())
        with self.assertRaisesRegex(ValueError, 'differs'):
            h.stamp(self.root, 'PhobosExample', package)

    def test_every_shipped_read_only_file_gets_a_note(self):
        for info in sorted((ROOT / 'mods').glob('*/mod_info.json')):
            mod = info.parent.name
            for relative in h.read_only_files(ROOT, mod):
                with self.subTest(mod=mod, file=relative):
                    note = h.header(mod, relative, (ROOT / 'mods' / mod / relative).read_bytes())
                    self.assertTrue(note.startswith(h.MARK) and '"' not in note and '\n' not in note)

    def test_every_build_script_stamps_its_package(self):
        # Workshop preparation expects the notes, so a build that packages by hand must add them itself.
        for script in sorted((ROOT / 'scripts').glob('build-*.ps1')):
            if script.name == 'build-package-support.ps1':
                continue
            with self.subTest(script=script.name):
                text = script.read_text(encoding='utf-8-sig')
                self.assertTrue(re.search(r'New-PhobosPackage\s+-RepoRoot', text) or 'read-only-data-headers.py' in text)


if __name__ == '__main__':
    unittest.main()
