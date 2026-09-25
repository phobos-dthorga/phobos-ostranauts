import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('item_reference', ROOT / 'scripts/update-item-reference.py')
reference = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reference)


class ItemReferenceTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads((ROOT / reference.SNAPSHOT).read_text(encoding='utf-8'))
        self.config = json.loads((ROOT / reference.CONFIG).read_text(encoding='utf-8'))

    def test_new_item_requires_reviewed_explanation(self):
        self.data['mods'][0]['items'].append(dict(self.data['mods'][0]['items'][0], id='NewUnexplainedItem'))
        with self.assertRaisesRegex(ValueError, 'explain new items'):
            reference.validate(self.data, self.config)

    def test_removed_item_and_duplicate_coverage_fail(self):
        self.data['mods'][0]['items'].pop()
        with self.assertRaisesRegex(ValueError, 'stale entries'):
            reference.validate(self.data, self.config)
        self.setUp()
        groups = self.config['mods']['PhobosAutoNav']['groups']
        groups[1]['ids'].append(groups[0]['ids'][0])
        with self.assertRaisesRegex(ValueError, 'duplicate item coverage'):
            reference.validate(self.data, self.config)

    def test_unknown_acquisition_route_requires_readable_label(self):
        self.data['mods'][0]['sources'][0]['table'] = 'NewMerchant'
        with self.assertRaisesRegex(ValueError, 'Describe acquisition table'):
            reference.generate(ROOT, self.data, self.config)

    def test_installed_price_difference_cannot_be_hidden(self):
        for item in self.data['mods'][0]['items']:
            if item['installed']:
                item['baseValue'] += 1
                break
        with self.assertRaisesRegex(ValueError, 'valuation differs'):
            reference.generate(ROOT, self.data, self.config)

    def test_snapshot_source_changes_fail_without_writes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'config').mkdir(); (root / 'docs').mkdir()
            (root / reference.CONFIG).write_text(json.dumps(self.config), encoding='utf-8')
            (root / reference.SNAPSHOT).write_text(json.dumps(self.data), encoding='utf-8')
            before = (root / reference.SNAPSHOT).read_bytes()
            with patch.object(reference, 'ROOT', root), patch.object(reference, 'source_hashes', return_value={'changed': 'hash'}), patch.object(sys, 'argv', ['update', '--check']), contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(reference.main(), 1)
            self.assertEqual(before, (root / reference.SNAPSHOT).read_bytes())
            self.assertEqual(list((root / 'docs').glob('*.md')), [])

    def test_check_detects_stale_guide_without_repairing_it(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); (root / 'config').mkdir(); (root / 'docs').mkdir()
            (root / reference.CONFIG).write_text(json.dumps(self.config), encoding='utf-8')
            (root / reference.SNAPSHOT).write_text(json.dumps(self.data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            for path, text in reference.generate(root, self.data, self.config).items():
                (root / path).write_text(text, encoding='utf-8')
            guide = root / 'docs/auto-nav-item-reference.md'
            guide.write_text('outdated price', encoding='utf-8')
            with patch.object(reference, 'ROOT', root), patch.object(reference, 'source_hashes', return_value=self.data['sourceHashes']), patch.object(sys, 'argv', ['update', '--check']), contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(reference.main(), 1)
            self.assertEqual(guide.read_text(), 'outdated price')

    def test_source_fingerprints_ignore_platform_newlines(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name in ('ItemReferenceExport.cs', 'EquipmentValueAudit.cs', 'Program.cs'):
                p = root / 'tests/PhobosNative.Tests' / name
                p.parent.mkdir(parents=True, exist_ok=True); p.write_bytes(b'line one\r\nline two\r\n')
            before = reference.source_hashes(root)
            for p in root.rglob('*.cs'): p.write_bytes(b'line one\nline two\n')
            self.assertEqual(before, reference.source_hashes(root))


if __name__ == '__main__':
    unittest.main()
