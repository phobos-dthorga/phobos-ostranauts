"""Offline research-tool checks. No game installation or saves are used."""

import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parents[1] / "scripts"
sys.path.insert(0, str(SCRIPTS))
spec = importlib.util.spec_from_file_location("salvage", SCRIPTS / "inspect-salvage.py")
salvage = importlib.util.module_from_spec(spec)
spec.loader.exec_module(salvage)


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value), encoding="utf-8")


class ResearchTests(unittest.TestCase):
    def test_comments_and_trailing_commas_preserve_string_contents(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "example.json"
            path.write_text('\ufeff{/* comment */ "url":"https://example.invalid/a,}",\n'
                            '"notes":"line one\nline two /* literal */", // comment\n'
                            '"values":[1,2,],}', encoding="utf-8")
            before = path.read_bytes()
            parsed = salvage.read_json(path)
            self.assertEqual(parsed["url"], "https://example.invalid/a,}")
            self.assertIn("\nline two /* literal */", parsed["notes"])
            self.assertEqual(parsed["values"], [1, 2])
            self.assertEqual(before, path.read_bytes())

    def test_load_order_excludes_disabled_packages_and_records_origin(self):
        with tempfile.TemporaryDirectory() as folder:
            game = Path(folder)
            mods = game / "Ostranauts_Data/Mods"
            write_json(mods / "loading_order.json", [{"aLoadOrder": [
                "core", "first", "disabled|disabled", "last"]}])
            core = game / "Ostranauts_Data/StreamingAssets/data"
            for root, mass in ((core, 1), (mods / "first/data", 2),
                               (mods / "last/data", 3), (mods / "disabled/data", 99)):
                write_json(root / "condowners/test.json", [{"strName": "Panel",
                           "aStartingConds": [f"StatMass=1.0x{mass}"]}])
            db, origins, warnings = salvage.load_definitions(game)
            self.assertEqual(salvage.static_stats(db["condowners"]["Panel"])["StatMass"], 3)
            self.assertEqual(origins[("condowners", "Panel")], "last/data/condowners/test.json")
            self.assertEqual(warnings, [])

    def test_mass_bounds_use_counts_and_refuse_partial_totals(self):
        db = {"condowners": {"Metal": {"aStartingConds": [
            "StatMass=1.0x0.5", "StatBasePrice=1.0x2"]}}}
        result = salvage.output_bounds(["Metal=1.0x2-4"], [], db)
        self.assertEqual(result["mass_bounds"], [1.0, 2.0])
        self.assertEqual(result["base_price_bounds"], [4.0, 8.0])
        for entries, nested in ((["Metal=1.0x2", "Missing=1.0x1"], []),
                                (["Metal=0.5x2"], []),
                                (["Metal=1.0x4-2"], []),
                                (["Metal=1.0x2"], ["Nested=1.0x1"])):
            with self.subTest(entries=entries, nested=nested):
                result = salvage.output_bounds(entries, nested, db)
                self.assertFalse(result["complete"])
                self.assertIsNone(result["mass_bounds"])

    def test_named_loot_precedes_direct_ids_and_absence_is_unknown(self):
        db = {kind: {} for kind in salvage.KINDS}
        db["condowners"] = {"Panel": {}, "Metal": {"aStartingConds": [
            "StatMass=1.0x2", "StatBasePrice=1.0x1"]}}
        db["installables"]["Dismantle"] = {"strName": "Dismantle", "strJobType": "dismantle",
            "strActionCO": "Panel", "strLootOut": "Yield", "aLootCOs": ["Missing"]}
        db["loot"]["Yield"] = {"aCOs": ["Metal=1.0x3"]}
        origins = {("condowners", "Panel"): "fixture", ("installables", "Dismantle"): "fixture"}

        def audit():
            return salvage.inspect_item("Panel", db, origins)["dismantle_actions"][0]["output_audit"]

        self.assertEqual(audit()["mass_bounds"], [6, 6])
        db["installables"]["Dismantle"].update(strLootOut=None, aLootCOs=["Metal", "Metal"])
        self.assertEqual(audit()["mass_bounds"], [4, 4])
        db["installables"]["Dismantle"]["aLootCOs"] = None
        self.assertFalse(audit()["complete"])
        self.assertIsNone(audit()["mass_bounds"])


if __name__ == "__main__":
    unittest.main()
