import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('performance_audit', Path(__file__).resolve().parents[1] / 'scripts/audit-performance.py')
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class CoverageTests(unittest.TestCase):
    def test_coverage_freshness_and_disposition(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); (root/'src').mkdir(); (root/'config').mkdir()
            source = root/'src/Example.cs'; source.write_text('class Example {}\n')
            row = dict(path='src/Example.cs', sha256=audit.digest(source), decision='Retain', findings=['R4'])
            ledger = root/'config/performance-audit.json'
            def save(rows): ledger.write_text(json.dumps(dict(files=rows)))
            save([row]); self.assertEqual(audit.verify(root)['status'], 'valid')
            (root/'src/obj').mkdir(); (root/'src/obj/Generated.cs').write_text('generated')
            self.assertEqual(audit.verify(root)['sourceFiles'], 1)
            save([row, row]); self.assertIn('Duplicate source entries', audit.verify(root)['errors'])
            save([dict(row, decision='')]); self.assertTrue(audit.verify(root)['errors'])
            save([row]); source.write_text('class Changed {}')
            self.assertTrue(audit.verify(root)['errors'][0].startswith('Source review stale'))
            source.unlink(); self.assertTrue(audit.verify(root)['errors'][0].startswith('Coverage changed'))
