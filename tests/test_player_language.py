import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('language_audit', Path(__file__).resolve().parents[1] / 'scripts/audit-player-language.py')
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

class LanguageAuditTests(unittest.TestCase):
    def fixture(self, root):
        for name in ('config', 'docs', 'workshop', 'translations/Example'):
            (root / name).mkdir(parents=True, exist_ok=True)
        documents = []
        for name in ('README.md', 'SUPPORT.md'):
            (root / name).write_text('Reviewed', encoding='utf-8')
            documents.append({'path': name, 'sha256': audit.digest('Reviewed')})
        ledger = {'entries': [{'mod': 'Example', 'key': 'status', 'before': 'Paid {0:F2} kg [us]',
                   'after': 'Loaded {0:F2} kg [us]', 'reason': 'Use player terminology', 'references': ['source.cs:1']}],
                  'documents': documents, 'surfaces': []}
        (root / 'config/english-language-audit.json').write_text(json.dumps(ledger), encoding='utf-8')
        (root / 'translations/Example/en.json').write_text(json.dumps({'status': 'Loaded {0:F2} kg [us]'}), encoding='utf-8')

    def test_reviewed_text_passes(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); self.fixture(root)
            self.assertEqual(audit.verify(root)['errors'], [])

    def test_changed_contract_and_new_key_fail(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); self.fixture(root)
            (root / 'translations/Example/en.json').write_text(json.dumps({'status': 'Loaded {0:F1}', 'new': 'New text'}))
            errors = audit.verify(root)['errors']
            for word in ('Coverage changed', 'Unreviewed wording', 'Placeholder contract changed', 'Native grammar changed'):
                self.assertTrue(any(word in x for x in errors), word)

    def test_changed_and_added_documents_fail(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); self.fixture(root)
            (root / 'README.md').write_text('Changed')
            (root / 'docs/new-guide.md').write_text('New guide')
            errors = audit.verify(root)['errors']
            self.assertTrue(any('Review is stale' in x for x in errors))
            self.assertTrue(any('Document inventory changed' in x for x in errors))

    def test_newlines_do_not_invalidate_review(self):
        self.assertEqual(audit.digest('one\r\ntwo'), audit.digest('one\ntwo'))

if __name__ == '__main__':
    unittest.main()
