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
        put('config/workshop-publishing.json', json.dumps({'appId':'1022980', 'mods':{self.name:{'itemId':None,'requires':[],'hold':None}}, 'externalRequiredItems':['3741030124'], 'externalRequiredItemNames':{'3741030124':'BepInEx Mod Loader'}}))
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

    def test_read_only_copies_carry_their_note(self):
        self.put('src/PhobosExample/PhobosExample.csproj', '<Project><ItemGroup><EmbeddedResource Include="../../mods/PhobosExample/framework/economy.json" LogicalName="x" /></ItemGroup></Project>')
        self.put('mods/PhobosExample/framework/economy.json', '{"schemaVersion": 1, "schema": "economy"}\n')
        self.put('dist/PhobosExample-P0/Mods/PhobosExample/framework/economy.json', '{"schemaVersion": 1, "schema": "economy"}\n')
        with self.assertRaisesRegex(ValueError, 'Stale package: PhobosExample/framework/economy.json'):
            w.plan(self.root, self.name)
        headers = w.module(ROOT, 'headers', 'scripts/read-only-data-headers.py')
        headers.stamp(self.root, self.name, self.root / 'dist/PhobosExample-P0')
        report, content, _ = w.plan(self.root, self.name)
        self.assertTrue(content['framework/economy.json'].startswith(b'// Read-only copy.'))

    def test_framework_sounds(self):
        self.put('config/workshop-publishing.json', json.dumps({'appId':'1022980', 'mods':{'PhobosFramework':{'itemId':None,'requires':[],'hold':None}}, 'externalRequiredItems':['3741030124'], 'externalRequiredItemNames':{'3741030124':'BepInEx Mod Loader'}}))
        shutil.move(self.root / 'mods/PhobosExample', self.root / 'mods/PhobosFramework')
        shutil.move(self.root / 'workshop/PhobosExample', self.root / 'workshop/PhobosFramework')
        for path in (self.root / 'workshop/PhobosFramework/releases').glob('*'):
            path.unlink()
        note_spec = importlib.util.spec_from_file_location('notes', self.root / 'scripts/workshop-release-notes.py')
        notes = importlib.util.module_from_spec(note_spec)
        note_spec.loader.exec_module(notes)
        for path, (_, data) in notes.plan(self.root, ['PhobosFramework'], None)[1].items():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        shutil.rmtree(self.root / 'dist/PhobosExample-P0')
        shutil.copytree(self.root / 'mods/PhobosFramework', self.root / 'dist/PhobosFramework-P0/Mods/PhobosFramework')
        for dll in ('PhobosFramework.dll', 'Phobos.Scope.Recording.dll'):
            self.put(f'dist/PhobosFramework-P0/BepInEx/plugins/PhobosFramework/{dll}', 'fixture ' + dll)
            self.put(f'src/PhobosFramework/bin/Release/netstandard2.1/{dll}', 'fixture ' + dll)
        self.put('assets/loop.wav', 'fixture loop')
        self.put('config/framework-sounds.json', json.dumps({'files': {'sounds/machine-loop-a.wav': 'assets/loop.wav'}}))
        with self.assertRaisesRegex(ValueError, 'Packaged sound sounds/machine-loop-a.wav'):
            w.plan(self.root, 'PhobosFramework')
        self.put('dist/PhobosFramework-P0/BepInEx/plugins/PhobosFramework/sounds/machine-loop-a.wav', 'fixture loop')
        _, content, _ = w.plan(self.root, 'PhobosFramework')
        self.assertIn('BepInEx/plugins/PhobosFramework/sounds/machine-loop-a.wav', content)
        self.put('dist/PhobosFramework-P0/BepInEx/plugins/PhobosFramework/sounds/extra.wav', 'not mapped')
        with self.assertRaisesRegex(ValueError, 'Unexpected plugin'):
            w.plan(self.root, 'PhobosFramework')

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
        self.assertNotIn('\\', draft)
        self.assertIn('[h1]Example[/h1]\n[b]Version', draft)
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

    def test_vdf_values(self):
        # Literal newlines; quotes and backslashes refused rather than escaped for SteamCMD.
        self.assertEqual(w.quote('line one\r\nline two'), '"line one\nline two"')
        for bad in ('say "hi"', 'C:\\path', 'bad\x00'):
            with self.assertRaises(ValueError):
                w.quote(bad)

    def test_steam_text_limits(self):
        self.put('workshop/PhobosExample/page.bbcode', '[h1]Example[/h1]\n[b]Version:[/b] 1.0.0\n[b]Publication status:[/b] Draft\n' + 'x' * 8000 + '\n')
        with self.assertRaisesRegex(ValueError, 'page'):
            w.plan(self.root, self.name)

    def test_upload_vdf_status_and_item_record(self):
        with patch.object(w.subprocess, 'check_output', side_effect=['abc\n', b'']):
            target = Path(w.prepare(self.root, self.name)['directory'])
        output = self.root / 'upload.vdf'
        result = w.upload_vdf(target, 'unlisted', output)
        text = output.read_text(encoding='utf-8')
        self.assertEqual(result['operation'], 'create')
        self.assertIn('"visibility" "3"', text)
        self.assertIn('"contentfolder" "' + (target / 'content').resolve().as_posix() + '"', text)
        with self.assertRaisesRegex(ValueError, 'already exists'):
            w.upload_vdf(target, 'private', output)
        self.assertEqual(w.verify(target)['status'], 'verified-offline')
        overview = w.status(self.root)
        self.assertEqual(overview['publicationOrder'], [self.name])
        self.assertEqual(overview['mods'][0]['package'], 'ready')
        with self.assertRaisesRegex(ValueError, 'decimal'):
            w.record_item_id(self.root, self.name, '12a')
        with self.assertRaisesRegex(ValueError, 'another entry'):
            w.record_item_id(self.root, self.name, '3741030124')
        self.assertEqual(w.record_item_id(self.root, self.name, '4242')['status'], 'recorded')
        self.assertEqual(w.record_item_id(self.root, self.name, '4242')['status'], 'unchanged')
        with self.assertRaisesRegex(ValueError, 'already has'):
            w.record_item_id(self.root, self.name, '5555')
        self.assertEqual(w.plan(self.root, self.name)[0]['operation'], 'update')
        self.assertNotIn(b'\r\n', (self.root / 'config/workshop-publishing.json').read_bytes())

    def release(self, entries, uploaded=None):
        """Rewrite the fixture at the first entry's version with the given (version, body) changelog entries."""
        current = entries[0][0]
        self.put('mods/PhobosExample/mod_info.json', '[{"strName":"Example","strModVersion":"%s"}]' % current)
        self.put('mods/PhobosExample/CHANGELOG.md', '# Changelog\n\n## [Unreleased]\n\nNone.\n\n' + ''.join(
            f'## [{v}] - 2026-09-26 - Draft\n\n### Fixed\n\n- {body}\n\n' for v, body in entries))
        self.put('workshop/PhobosExample/page.bbcode', f'[h1]Example[/h1]\n[b]Version:[/b] {current}\n[b]Publication status:[/b] Draft\n')
        notes = w.module(self.root, 'notes', 'scripts/workshop-release-notes.py')
        for path, (_, data) in notes.plan(self.root, [self.name], None)[1].items():
            path.write_bytes(data)
        shutil.rmtree(self.root / 'dist/PhobosExample-P0/Mods/PhobosExample')
        shutil.copytree(self.root / 'mods/PhobosExample', self.root / 'dist/PhobosExample-P0/Mods/PhobosExample')
        path = self.root / 'config/workshop-publishing.json'
        config = json.loads(path.read_text())
        config['mods'][self.name].update(itemId='4242', uploadedVersion=uploaded)
        path.write_text(json.dumps(config))

    def test_change_note_covers_every_version_since_the_last_upload(self):
        self.release([('1.0.2', 'Second fix.'), ('1.0.1', 'First fix.'), ('1.0.0', 'Example.')], uploaded='1.0.0')
        report, content, _ = w.plan(self.root, self.name)
        note = content['documentation/Release-notes.bbcode'].decode('utf-8')
        self.assertEqual(report['changeNoteVersions'], ['1.0.2', '1.0.1'])
        self.assertLess(note.index('Example 1.0.2'), note.index('Example 1.0.1'))
        self.assertNotIn('Example 1.0.0', note)
        # Without a recorded upload, or repeating the same version, only the current version is described.
        for uploaded in (None, '1.0.2'):
            self.release([('1.0.2', 'Second fix.'), ('1.0.1', 'First fix.'), ('1.0.0', 'Example.')], uploaded=uploaded)
            self.assertEqual(w.plan(self.root, self.name)[0]['changeNoteVersions'], ['1.0.2'])

    def test_change_note_names_versions_that_do_not_fit(self):
        long = 'Long fix. ' * 800
        self.release([('1.0.3', 'Third fix.'), ('1.0.2', long), ('1.0.1', 'First fix.'), ('1.0.0', 'Example.')], uploaded='1.0.0')
        report, content, _ = w.plan(self.root, self.name)
        note = content['documentation/Release-notes.bbcode'].decode('utf-8')
        self.assertEqual((report['changeNoteVersions'], report['changeNoteOmitted']), (['1.0.3'], ['1.0.2', '1.0.1']))
        self.assertTrue(note.endswith('Earlier changes in versions 1.0.1 to 1.0.2 are listed in CHANGELOG.md, which comes with the mod.\n'))
        self.assertLessEqual(len(note.encode('utf-8')), w.TEXT_MAX_BYTES)

    def test_record_uploaded_version(self):
        with self.assertRaisesRegex(ValueError, 'no item ID'):
            w.record_uploaded_version(self.root, self.name, '1.0.0')
        w.record_item_id(self.root, self.name, '4242')
        with self.assertRaisesRegex(ValueError, 'numbers like'):
            w.record_uploaded_version(self.root, self.name, '1.0')
        self.assertEqual(w.record_uploaded_version(self.root, self.name, '1.0.0')['status'], 'recorded')
        self.assertEqual(w.record_uploaded_version(self.root, self.name, '1.0.0')['status'], 'unchanged')
        self.assertEqual(w.plan(self.root, self.name)[0]['uploadedVersion'], '1.0.0')
        path = self.root / 'config/workshop-publishing.json'
        config = json.loads(path.read_text())
        config['mods'][self.name]['itemId'] = None
        path.write_text(json.dumps(config))
        with self.assertRaisesRegex(ValueError, 'uploadedVersion'):
            w.catalogue(self.root)

    def test_publication_order(self):
        config = {'mods': {'PhobosB': {'requires': ['PhobosA']}, 'PhobosA': {'requires': []}, 'PhobosC': {'requires': ['PhobosB', 'PhobosA']}}}
        self.assertEqual(w.publication_order(config), ['PhobosA', 'PhobosB', 'PhobosC'])
        config['mods']['PhobosA']['requires'] = ['PhobosC']
        with self.assertRaisesRegex(ValueError, 'Circular'):
            w.publication_order(config)

if __name__ == '__main__':
    unittest.main()
