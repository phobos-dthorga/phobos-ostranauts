import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('workshop', ROOT / 'scripts/prepare-workshop.py')
w = importlib.util.module_from_spec(spec)
spec.loader.exec_module(w)


class PreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        def put(path, text):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(text, encoding='utf-8')
        self.put = put
        self.name = 'PhobosExample'
        put('config/workshop-publishing.json', json.dumps({'appId':'1022980', 'mods':{self.name:{'itemId':None,'requires':[],'hold':None}}, 'externalRequiredItems':['3741030124']}))
        put('mods/PhobosExample/mod_info.json', '[{"strName":"Example","strModVersion":"1.0.0"}]')
        put('mods/PhobosExample/data/README.md', 'Keep data')
        put('mods/PhobosExample/preview.png', 'fixture cover')
        put('mods/PhobosExample/CHANGELOG.md', '# Changelog\n\n## [Unreleased]\n\nNone.\n\n## [1.0.0] - 2026-09-26 - Draft\n\n### Added\n\n- Example.\n')
        put('workshop/PhobosExample/page.bbcode', '[h1]Example[/h1]\n[b]Version:[/b] 1.0.0\n[b]Publication status:[/b] Draft\n')
        (self.root / 'scripts').mkdir()
        shutil.copy2(ROOT / 'scripts/workshop-release-notes.py', self.root / 'scripts/workshop-release-notes.py')
        note_spec = importlib.util.spec_from_file_location('notes', self.root / 'scripts/workshop-release-notes.py')
        notes = importlib.util.module_from_spec(note_spec)
        note_spec.loader.exec_module(notes)
        _, outputs = notes.plan(self.root, [self.name], None)
        for path, (_, data) in outputs.items():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        shutil.copytree(self.root / 'mods/PhobosExample', self.root / 'dist/PhobosExample-P0/Mods/PhobosExample')
        put('dist/PhobosExample-P0/BepInEx/plugins/PhobosExample/PhobosExample.dll', 'fixture assembly')
        put('src/PhobosExample/bin/Release/netstandard2.1/PhobosExample.dll', 'fixture assembly')

    def test_preview_and_layout(self):
        report, content, _ = w.plan(self.root, self.name)
        self.assertFalse((self.root / '.local').exists())
        self.assertFalse(report['uploadEnabled'])
        self.assertIn('mod_info.json', content)
        self.assertIn('BepInEx/plugins/PhobosExample/PhobosExample.dll', content)
        self.assertFalse(any(p.startswith('Mods/') for p in content))

    def test_stale_package(self):
        self.put('mods/PhobosExample/data/README.md', 'changed')
        with self.assertRaisesRegex(ValueError, 'Stale package'):
            w.plan(self.root, self.name)

    def test_missing_data(self):
        (self.root / 'mods/PhobosExample/data/README.md').unlink()
        (self.root / 'dist/PhobosExample-P0/Mods/PhobosExample/data/README.md').unlink()
        with self.assertRaisesRegex(ValueError, 'data directory'):
            w.plan(self.root, self.name)

    def test_unexpected_plugin(self):
        self.put('dist/PhobosExample-P0/BepInEx/plugins/PhobosExample/foreign.dll', 'not ours')
        with self.assertRaisesRegex(ValueError, 'Unexpected plugin'):
            w.plan(self.root, self.name)

    def test_stale_notes(self):
        self.put('workshop/PhobosExample/releases/1.0.0.bbcode', 'stale')
        with self.assertRaisesRegex(ValueError, 'notes are stale'):
            w.plan(self.root, self.name)

    def test_prepare_verify_tampering_and_repeat(self):
        with patch.object(w.subprocess, 'check_output', side_effect=['abc\n', b''] * 2):
            first = w.prepare(self.root, self.name)
            second = w.prepare(self.root, self.name)
        self.assertNotEqual(first['directory'], second['directory'])
        target = Path(first['directory'])
        self.assertEqual(w.verify(target)['status'], 'verified-offline')
        draft = (target / 'workshop.vdf.draft').read_text()
        self.assertIn('"visibility" "2"', draft)
        self.assertIn('"publishedfileid" "0"', draft)
        (target / 'content/data/README.md').write_text('tampered')
        with self.assertRaisesRegex(ValueError, 'changed'):
            w.verify(target)

    def test_holds_ids_and_cover(self):
        path = self.root / 'config/workshop-publishing.json'
        config = json.loads(path.read_text())
        config['mods'][self.name].update(itemId='12345', hold='Not ready')
        path.write_text(json.dumps(config))
        report, _, _ = w.plan(self.root, self.name)
        self.assertEqual(report['operation'], 'update')
        self.assertIn('Not ready', report['blockers'])
        config['mods'][self.name]['itemId'] = '../bad'
        path.write_text(json.dumps(config))
        with self.assertRaisesRegex(ValueError, 'Item IDs'):
            w.plan(self.root, self.name)

    def test_vdf_escaping(self):
        self.assertEqual(w.quote('a"b\\c\nd'), '"a\\"b\\\\c\\nd"')
        with self.assertRaises(ValueError):
            w.quote('bad\x00')

if __name__ == '__main__':
    unittest.main()
