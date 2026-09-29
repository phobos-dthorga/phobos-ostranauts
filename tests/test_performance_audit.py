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


class RefreshTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory(); self.root = Path(self.folder.name)
        (self.root/'src/PhobosExample/Core').mkdir(parents=True); (self.root/'config').mkdir(); (self.root/'docs/development').mkdir(parents=True)
        self.kept = self.root/'src/PhobosExample/Kept.cs'; self.kept.write_text('class Kept {}\n')
        self.changed = self.root/'src/PhobosExample/Changed.cs'; self.changed.write_text('class Old {}\n')
        self.removed = self.root/'src/PhobosExample/Gone.cs'; self.removed.write_text('class Gone {}\n')
        rows = [dict(path='src/PhobosExample/Kept.cs', sha256=audit.digest(self.kept), role='r', review='v', decision='Keep as is', findings=['R4'], indicators={}),
                dict(path='src/PhobosExample/Changed.cs', sha256=audit.digest(self.changed), role='r', review='v', decision='Old decision', findings=['R3'], indicators={}),
                dict(path='src/PhobosExample/Gone.cs', sha256=audit.digest(self.removed), role='r', review='v', decision='Gone', findings=['R3'], indicators={})]
        (self.root/audit.LEDGER).write_text(json.dumps(dict(schemaVersion=1, date='2026-09-28', baselineCommit='old', files=rows)))
        (self.root/audit.REPORT).write_text('# Report\n\nFF1 is described here.\n')
        self.changed.write_text('[HarmonyPatch]\nclass New { void Run() { foreach (var c in DataHandler.mapCOs.Values.Where(x => x != null).ToArray()) Log(c); } }\n')
        self.removed.unlink()
        self.added = self.root/'src/PhobosExample/Core/Added.cs'; self.added.write_text('class Added {}\n')

    def tearDown(self): self.folder.cleanup()

    def test_changed_file_without_disposition_is_refused(self):
        with self.assertRaises(ValueError) as refused: audit.refresh(self.root, date='2026-09-29')
        self.assertIn('Changed.cs', str(refused.exception)); self.assertIn('Added.cs', str(refused.exception))
        self.assertEqual(json.loads((self.root/audit.LEDGER).read_text())['date'], '2026-09-28', 'a refused refresh writes nothing')

    def test_stamp_carry_add_and_remove(self):
        ledger, report = audit.refresh(self.root, findings=['FF1'], commit='abc', date='2026-09-29')
        rows = {r['path']: r for r in ledger['files']}
        self.assertEqual(report['unchanged'], ['src/PhobosExample/Kept.cs']); self.assertEqual(rows['src/PhobosExample/Kept.cs']['decision'], 'Keep as is')
        changed = rows['src/PhobosExample/Changed.cs']
        self.assertEqual(changed['findings'], ['R3', 'FF1'], 'earlier findings are retained and the new code appended once')
        self.assertEqual(changed['sha256'], audit.digest(self.changed)); self.assertEqual(changed['reviewed'], '2026-09-29')
        self.assertEqual(sorted(changed['indicators']), ['collection', 'discovery', 'persistence_or_io', 'runtime_hook'])
        self.assertEqual(changed['indicators']['runtime_hook'], [1])
        added = rows['src/PhobosExample/Core/Added.cs']
        self.assertEqual(added['findings'], ['FF1']); self.assertEqual(added['role'], 'registration, metadata or event-driven content')
        self.assertIn('2026-09-29', added['decision'])
        self.assertEqual(report['removed'], ['src/PhobosExample/Gone.cs']); self.assertNotIn('src/PhobosExample/Gone.cs', rows)
        self.assertEqual((ledger['date'], ledger['baselineCommit']), ('2026-09-29', 'abc'))
        audit.write_ledger(self.root, ledger)
        self.assertEqual(audit.verify(self.root)['status'], 'valid', 'a refreshed ledger verifies')
        self.assertEqual(audit.missing_findings(self.root, ledger), ['R3', 'R4'], 'codes absent from the report are listed')

    def test_touched_set_limits_the_stamp_and_carry_keeps_the_rest(self):
        with self.assertRaises(ValueError):
            audit.refresh(self.root, findings=['FF2'], touched={'src/PhobosExample/Core/Added.cs'}, date='2026-09-29')
        ledger, report = audit.refresh(self.root, findings=['FF2'], touched={'src/PhobosExample/Core/Added.cs'}, carry=True, date='2026-09-29')
        rows = {r['path']: r for r in ledger['files']}
        self.assertEqual(report['stamped'], []); self.assertEqual(report['added'], ['src/PhobosExample/Core/Added.cs'])
        self.assertEqual(report['carried'], ['src/PhobosExample/Changed.cs'])
        self.assertEqual(rows['src/PhobosExample/Changed.cs']['decision'], 'Old decision'); self.assertEqual(rows['src/PhobosExample/Changed.cs']['carried'], '2026-09-29')
        self.assertEqual(rows['src/PhobosExample/Changed.cs']['sha256'], audit.digest(self.changed))
        self.assertEqual(rows['src/PhobosExample/Core/Added.cs']['findings'], ['FF2'])

    def test_role_heuristic_and_override(self):
        self.assertEqual(audit.role_for('src/PhobosAutoNav/NavigationService.cs'), 'navigation, native boundary or flight policy')
        self.assertEqual(audit.role_for('src/PhobosShipbreaker/IndustrialPanel.cs'), 'shared presentation and input')
        self.assertEqual(audit.role_for('src/PhobosShipbreaker/FurnaceService.cs'), 'industrial service, native boundary or process policy')
        self.assertEqual(audit.role_for('src/PhobosFramework/Crew/CrewWork.cs'), 'shared accounting, inventory, crew or native boundary')
        self.assertEqual(audit.role_for('src/PhobosFramework/Crew/CrewWork.cs', 'custom'), 'custom')


if __name__ == '__main__':
    unittest.main()
