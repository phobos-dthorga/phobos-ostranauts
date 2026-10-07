from datetime import datetime, timezone
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('pull', ROOT / 'scripts/pull-workshop.py')
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)
# Midday UTC is the same calendar day in every time zone from UTC-11 to UTC+11.
NOON = int(datetime(2026, 10, 7, 12, tzinfo=timezone.utc).timestamp())


class PullTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'scripts').mkdir()
        for script in ('workshop-release-notes.py', 'prepare-workshop.py'):
            shutil.copy2(ROOT / 'scripts' / script, self.root / 'scripts' / script)
        self.catalogue({'itemId': '4242', 'requires': [], 'hold': None, 'uploadedVersion': '1.0.1', 'uploadedVisibility': 'private'})
        self.put('mods/PhobosExample/mod_info.json', '[{"strName":"Example","strModVersion":"1.0.1"}]')
        self.put('mods/PhobosExample/CHANGELOG.md', '# Changelog\n\n## [Unreleased]\n\n## [1.0.1] - 2026-10-01 - Draft\n\n### Fixed\n\n- A fix.\n\n## [1.0.0] - 2026-09-01 - Draft\n\n### Added\n\n- Example.\n')
        self.page = '[h1]Example[/h1]\n[b]Version:[/b] 1.0.1\n[b]Publication status:[/b] Not published - development candidate\n'
        self.put('workshop/PhobosExample/page.bbcode', self.page)
        notes = p.module(self.root, 'notes', 'scripts/workshop-release-notes.py')
        for path, (_, data) in notes.plan(self.root, ['PhobosExample'], None)[1].items():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        self.live = {'result': 1, 'visibility': 0, 'time_updated': NOON,
                     'description': self.page.replace('Not published - development candidate', 'Public since 7 October 2026').replace('\n', '\r\n') + '\r\n'}

    def put(self, path, text):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding='utf-8', newline='')

    def catalogue(self, item):
        self.put('config/workshop-publishing.json', json.dumps({'appId': '1022980', 'mods': {'PhobosExample': item},
                 'externalRequiredItems': ['3741030124'], 'externalRequiredItemNames': {'3741030124': 'BepInEx Mod Loader'}}, indent=2) + '\n')

    def pull(self, areas, write=False):
        rows, changes, notes = p.plan(self.root, None, set(areas), fetch=lambda ids: {'4242': dict(self.live, publishedfileid='4242')})
        if write:
            p.apply(self.root, changes, notes)
        return rows[0], changes

    def test_preview_writes_nothing(self):
        row, changes = self.pull({'visibility', 'description', 'changelog'})
        self.assertEqual(len(changes), 3)
        self.assertEqual(json.loads((self.root / 'config/workshop-publishing.json').read_text())['mods']['PhobosExample']['uploadedVisibility'], 'private')
        self.assertEqual((self.root / 'workshop/PhobosExample/page.bbcode').read_text(encoding='utf-8'), self.page)

    def test_pull_every_area(self):
        self.pull({'visibility', 'description', 'changelog'}, write=True)
        self.assertEqual(json.loads((self.root / 'config/workshop-publishing.json').read_text())['mods']['PhobosExample']['uploadedVisibility'], 'public')
        page = (self.root / 'workshop/PhobosExample/page.bbcode').read_bytes()
        self.assertIn(b'Public since 7 October 2026\n', page)
        self.assertNotIn(b'\r', page)
        self.assertTrue(page.endswith(b'\n') and not page.endswith(b'\n\n'))
        log = (self.root / 'mods/PhobosExample/CHANGELOG.md').read_text(encoding='utf-8')
        self.assertIn('## [1.0.1] - 2026-10-07 - Released', log)
        self.assertIn('## [1.0.0] - 2026-09-01 - Draft', log)  # only the version on the item
        self.assertIn('[b]Released | 2026-10-07[/b]', (self.root / 'workshop/PhobosExample/releases/1.0.1.bbcode').read_text(encoding='utf-8'))
        self.assertEqual(self.pull({'visibility', 'description', 'changelog'})[1], {})  # a repeat finds nothing

    def test_only_the_chosen_areas(self):
        _, changes = self.pull({'visibility'})
        self.assertEqual([path.name for path in changes], ['workshop-publishing.json'])
        self.assertEqual(self.pull(set())[1], {})

    def test_hidden_items_are_left_alone(self):
        self.live = {'result': 9}
        row, changes = self.pull({'visibility', 'description', 'changelog'})
        self.assertEqual(changes, {})
        self.assertIn('does not show this item', row['notes'][0])

    def test_older_steam_page_is_not_pulled(self):
        self.put('mods/PhobosExample/mod_info.json', '[{"strName":"Example","strModVersion":"1.0.2"}]')
        row, changes = self.pull({'description'})
        self.assertEqual(changes, {})
        self.assertIn('describes version 1.0.1 but the source is 1.0.2', row['notes'][0])

    def test_private_items_keep_draft_and_held_public_items_are_flagged(self):
        self.live['visibility'] = 3
        _, changes = self.pull({'changelog'})
        self.assertEqual(changes, {})  # unlisted is not a public release
        self.live['visibility'] = 0
        self.catalogue({'itemId': '4242', 'requires': [], 'hold': 'Not ready', 'uploadedVersion': '1.0.1', 'uploadedVisibility': 'private'})
        row, _ = self.pull(set())
        self.assertIn('held in the catalogue (Not ready)', row['notes'][0])


if __name__ == '__main__':
    unittest.main()
