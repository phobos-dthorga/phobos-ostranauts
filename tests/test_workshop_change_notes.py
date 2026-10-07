import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('notes', ROOT / 'scripts/workshop-change-notes.py')
n = importlib.util.module_from_spec(spec)
spec.loader.exec_module(n)


def note(version, status='Draft - not published | 2026-10-01'):
    return f'[h1]Example {version}[/h1]\n[b]{status}[/b]\n\nA change.\n'


class ChangeNoteTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.visibility('public')

    def visibility(self, value):
        path = self.root / 'config/workshop-publishing.json'
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps({'mods': {'PhobosExample': {'itemId': '4242', 'uploadedVisibility': value}}}), encoding='utf-8')

    def upload(self, stamp, version, visibility, operation='update', status='submitted-unverified', exit_code=0, text=None, tabs=False):
        folder = self.root / '.local/workshop-receipts/PhobosExample' / f'2026100{stamp}-{version}-{operation}'
        folder.mkdir(parents=True)
        receipt = {'version': version, 'visibility': visibility, 'operation': operation, 'status': status,
                   'steamCmdExitCode': exit_code, 'startedAt': f'2026-10-07T0{stamp}:00:00+11:00'}
        (folder / 'receipt.json').write_text(json.dumps(receipt), encoding='utf-8')
        gap = '\t\t' if tabs else ' '
        (folder / 'upload.vdf').write_text(f'"workshopitem"\n{{\n  "title"{gap}"Example"\n  "changenote"{gap}"{text or note(version)}"\n}}\n', encoding='utf-8')

    def run_items(self):
        n.run(self.root, None, True)
        return (self.root / '.local/workshop-change-notes/PhobosExample.md').read_text(encoding='utf-8')

    def test_entries_and_suggestions(self):
        self.upload(1, '1.0.0', 'Private', operation='create', status='created-unverified', tabs=True)
        self.upload(2, '1.0.0', 'Private')                     # re-sent with no changes
        self.upload(3, '1.0.1', 'Public', text=note('1.0.1', 'Released | 2026-10-07'))
        self.upload(4, '1.0.1', 'Public')                      # same version, same visibility
        self.upload(5, '1.0.2', 'Private', status='submitted-unverified', exit_code=5)  # failed: never shown
        sheet = self.run_items()
        headings = [line for line in sheet.splitlines() if line.startswith('## ')]
        self.assertEqual(len(headings), 4)
        self.assertTrue(headings[0].startswith('## 1. 7 Oct 2026 04:00: 1.0.1') and headings[-1].endswith('Private create - Replace'))
        self.assertIn('Version 1.0.1 sent again with no changes on 7 October 2026. Its changes are in the 1.0.1 entry below.', sheet)
        self.assertIn('Version 1.0.0 sent again with no changes', sheet)
        self.assertIn('[b]Uploaded privately | 2026-10-07[/b]', sheet)   # the create, now on a public item
        self.assertIn('## 2. 7 Oct 2026 03:00: 1.0.1, Public update - Keep', sheet)  # already says Released
        self.assertNotIn('1.0.2', sheet)

    def test_visibility_change_and_private_items(self):
        self.upload(1, '1.0.0', 'Private', operation='create', status='created-unverified')
        self.upload(2, '1.0.0', 'Public')
        self.assertIn('Version 1.0.0 sent to make the item public', self.run_items())
        self.visibility('private')
        sheet = self.run_items()
        self.assertNotIn('Uploaded privately', sheet)  # drafts on a private item are accurate
        self.assertIn('## 2. 7 Oct 2026 01:00: 1.0.0, Private create - Keep', sheet)

    def test_no_write(self):
        self.upload(1, '1.0.0', 'Private', operation='create', status='created-unverified')
        rows = n.run(self.root, None, False)
        self.assertEqual((rows[0]['entries'], rows[0]['toReplace']), (1, 1))
        self.assertFalse((self.root / '.local/workshop-change-notes').exists())


if __name__ == '__main__':
    unittest.main()
