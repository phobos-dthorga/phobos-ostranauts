"""The press-twice audit (Framework 0.125.0): every do-first refusal is classified, and the record cannot go stale."""
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
_spec = importlib.util.spec_from_file_location("audit_panel_overrides", ROOT / "scripts" / "audit-panel-overrides.py")
audit = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(audit)


class PanelOverrideAuditTests(unittest.TestCase):
    def test_repository_is_classified(self):
        result = audit.check(ROOT)
        self.assertEqual(result["errors"], [])
        self.assertGreater(result["scanned"], 0)

    def _root(self, catalog, record):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        root = Path(tmp.name)
        (root / "translations" / "PhobosDemo").mkdir(parents=True)
        (root / "translations" / "PhobosDemo" / "en.json").write_text(json.dumps(catalog), encoding="utf-8")
        (root / "config").mkdir()
        (root / "config" / "panel-override-audit.json").write_text(json.dumps({"schemaVersion": 1, "mods": ["PhobosDemo"], **record}), encoding="utf-8")
        return root

    def test_unclassified_refusal_fails(self):
        root = self._root({"Pump.link_busy": "Pause the pump before changing its link.", "Pump.title": "Pump"}, {"rules": [], "entries": []})
        errors = audit.check(root)["errors"]
        self.assertEqual(len(errors), 1)
        self.assertIn("Pump.link_busy", errors[0])

    def test_rule_and_entry_classify(self):
        root = self._root({"Pump.link_busy": "Pause the pump before changing its link.", "Pump.repair_first": "Repair the pump first."},
                          {"rules": [{"keys": r"repair_first$", "class": "refused", "note": "Repair is crew work."}],
                           "entries": [{"mod": "PhobosDemo", "key": "Pump.link_busy", "class": "pending", "round": "Demo", "note": "A: pause, change, resume."}]})
        result = audit.check(root)
        self.assertEqual(result["errors"], [])
        self.assertEqual(result["pending"][0]["key"], "Pump.link_busy")

    def test_stale_and_incomplete_entries_fail(self):
        root = self._root({"Pump.title": "Pump"},
                          {"rules": [], "entries": [{"mod": "PhobosDemo", "key": "Pump.gone_first", "class": "pending", "note": "x"},
                                                    {"mod": "PhobosOther", "key": "x", "class": "maybe"}]})
        errors = " ".join(audit.check(root)["errors"])
        self.assertIn("no longer exists", errors)
        self.assertIn("need a round", errors)
        self.assertIn("unknown class", errors)
        self.assertIn("not in the audit's mods list", errors)


if __name__ == "__main__":
    unittest.main()
