"""Exercise source updates in temporary fixtures, never real mod sources."""
import contextlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("constants_updater", ROOT / "scripts/update-constants.py")
updater = importlib.util.module_from_spec(spec)
spec.loader.exec_module(updater)


class UpdateTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        catalog = json.loads((ROOT / updater.CATALOG).read_text())
        paths = {updater.CATALOG}
        for entry in catalog["constants"].values():
            paths.update(target["path"] for target in entry["targets"])
        for name in paths:
            target = self.root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)
        # Stable fixture baselines: a real release bump must not invalidate the
        # updater tests themselves. Only temporary copies are normalized.
        for key, value in {"AutoNav.version": "0.10.1", "Framework.version": "0.17.0",
                           "AutoNav.salvageChance": "0.03", "AutoNav.coastTolerancePercent": "10",
                           "AutoNav.burnHeadingDegrees": "2", "Shipbreaker.feedSeconds": "2"}.items():
            entry = catalog["constants"][key]
            for target in entry["targets"]:
                path = self.root / target["path"]
                data = path.read_bytes().decode("utf-8-sig")
                for match in reversed(list(updater.re.finditer(target["pattern"], data, updater.re.MULTILINE))):
                    start, end = match.span("value")
                    data = data[:start] + value + data[end:]
                path.write_bytes(data.encode("utf-8"))
            entry["value"] = value
        (self.root / updater.CATALOG).write_text(json.dumps(catalog), encoding="utf-8")

    def snapshot(self):
        return {str(p.relative_to(self.root)): p.read_bytes() for p in self.root.rglob("*") if p.is_file()}

    def edit_catalog(self, change):
        path = self.root / updater.CATALOG
        data = json.loads(path.read_text())
        change(data)
        path.write_text(json.dumps(data))

    def cli(self, *args):
        output = io.StringIO()
        with patch.object(updater, "ROOT", self.root), contextlib.redirect_stdout(output):
            code = updater.main([*args, "--format", "json"])
        return code, json.loads(output.getvalue())

    def test_preview_checks_without_writes(self):
        before = self.snapshot()
        code, result = self.cli("--set", "AutoNav.version=0.10.2")
        self.assertEqual(0, code)
        self.assertEqual("preview", result["status"])
        self.assertEqual(before, self.snapshot())
        self.assertGreaterEqual(len(result["changes"]), 3)
        self.assertTrue(all(len(f["afterSha256"]) == 64 for f in result["files"]))

    def test_multi_update_apply_and_idempotent_repeat(self):
        args = ("--set", "AutoNav.version=0.10.2", "--set", "Shipbreaker.feedSeconds=3", "--apply")
        code, result = self.cli(*args)
        self.assertEqual(0, code)
        self.assertEqual("read-back", result["verification"])
        for item in result["files"]:
            self.assertEqual(item["afterSha256"], updater.digest((self.root / item["path"]).read_bytes()))
        before = self.snapshot()
        code, repeat = self.cli(*args)
        self.assertEqual((0, "unchanged"), (code, repeat["status"]))
        self.assertEqual(before, self.snapshot())
        self.assertEqual(0, self.cli("--check")[0])

    def test_all_mod_versions_and_game_targets(self):
        catalog, _ = updater.load_catalog(self.root)
        assignments = []
        for key, entry in catalog["constants"].items():
            if entry["type"] == "version":
                parts = entry["value"].split(".")
                parts[-1] = str(int(parts[-1]) + 1)
                assignments.extend(["--set", key + "=" + ".".join(parts)])
        self.assertEqual(0, self.cli(*assignments, "--apply")[0])
        self.assertEqual(0, self.cli("--check")[0])

    def test_invalid_values_leave_everything_intact(self):
        before = self.snapshot()
        for assignment in ["NoSuch.key=1", "AutoNav.version=1.2", "AutoNav.version=01.2.3",
                           "AutoNav.version=1.2.65535", "AutoNav.version=1.2.3-preview",
                           "Shipbreaker.feedSeconds=0", "AutoNav.salvageChance=NaN",
                           "AutoNav.salvageChance=1e-3", "AutoNav.salvageChance=1.1"]:
            with self.subTest(assignment=assignment):
                self.assertEqual(1, self.cli("--set", assignment, "--apply")[0])
                self.assertEqual(before, self.snapshot())

    def test_expected_value_conflict(self):
        before = self.snapshot()
        self.assertEqual(1, self.cli("--set", "AutoNav.version=0.10.2", "--expect", "AutoNav.version=0.1.0", "--apply")[0])
        self.assertEqual(before, self.snapshot())
        self.assertEqual(0, self.cli("--set", "AutoNav.version=0.10.2", "--expect", "AutoNav.version=0.10.1", "--apply")[0])

    def test_drift_in_unselected_mod_blocks_whole_batch(self):
        path = self.root / "src/PhobosFramework/Plugin.cs"
        path.write_bytes(path.read_bytes().replace(b'Version = "0.17.0"', b'Version = "0.17.1"'))
        before = self.snapshot()
        code, result = self.cli("--set", "AutoNav.version=0.10.2", "--apply")
        self.assertEqual(1, code)
        self.assertIn("Drift", result["error"])
        self.assertEqual(before, self.snapshot())

    def test_missing_and_ambiguous_targets_are_errors(self):
        path = self.root / "src/PhobosAutoNav/Plugin.cs"
        data = path.read_bytes()
        for changed in [data.replace(b'Version = "0.10.1"', b'Renamed = "0.10.1"'), data + b'\nVersion = "0.10.1";']:
            path.write_bytes(changed)
            self.assertEqual(1, self.cli("--check")[0])

    def test_bom_newlines_unicode_and_unrelated_edits_preserved(self):
        path = self.root / "src/PhobosAutoNav/Plugin.cs"
        original = path.read_bytes().decode("utf-8-sig").replace("\r\n", "\n").replace("\n", "\r\n")
        before = b"\xef\xbb\xbf" + (original + "// café — unrelated edit\r\n").encode()
        path.write_bytes(before)
        self.assertEqual(0, self.cli("--set", "AutoNav.version=0.10.2", "--apply")[0])
        self.assertEqual(before.replace(b'Version = "0.10.1"', b'Version = "0.10.2"'), path.read_bytes())

    def test_numeric_equivalence_does_not_reformat(self):
        before = self.snapshot()
        self.assertEqual("unchanged", self.cli("--set", "AutoNav.salvageChance=0.030", "--apply")[1]["status"])
        self.assertEqual(before, self.snapshot())

    def test_fractional_float_gets_required_suffix_once(self):
        args = ("--set", "AutoNav.coastTolerancePercent=10.5", "--set", "AutoNav.burnHeadingDegrees=2.5", "--apply")
        self.assertEqual(0, self.cli(*args)[0])
        data = (self.root / "src/PhobosAutoNav/Core/CoastRules.cs").read_text()
        self.assertIn("DefaultSpeedTolerancePercent = 10.5f;", data)
        self.assertIn("DefaultBurnHeadingToleranceDegrees = 2.5f;", data)
        self.assertEqual("unchanged", self.cli(*args)[1]["status"])

    def test_integer_boolean_and_zero_count_validation(self):
        self.assertEqual("2", updater.validate({"type": "integer", "min": 0, "max": 10}, "2.0"))
        self.assertEqual("false", updater.validate({"type": "boolean"}, "false"))
        with self.assertRaises(updater.UpdateError):
            updater.validate({"type": "integer", "min": 0, "max": 10}, "2.5")
        self.edit_catalog(lambda d: d["constants"]["AutoNav.version"]["targets"][0].update(count=0))
        self.assertEqual(1, self.cli("--check")[0])

    def test_concurrent_edit_refused(self):
        original, proposed, _ = updater.plan(self.root, {"AutoNav.version": "0.10.2"}, {})
        path = self.root / "README.md"
        path.write_bytes(path.read_bytes() + b"\nConcurrent work\n")
        before = self.snapshot()
        with self.assertRaisesRegex(updater.UpdateError, "Concurrent edit"):
            updater.apply(self.root, original, proposed)
        self.assertEqual(before, self.snapshot())

    def test_write_failure_rolls_back(self):
        before = self.snapshot()
        original, proposed, _ = updater.plan(self.root, {"AutoNav.version": "0.10.2"}, {})
        real = updater.replace_file
        calls = 0
        def fail_once(path, data):
            nonlocal calls
            calls += 1
            if calls == 2:
                raise OSError("Simulated write failure")
            real(path, data)
        with patch.object(updater, "replace_file", side_effect=fail_once):
            with self.assertRaisesRegex(updater.UpdateError, "written files restored"):
                updater.apply(self.root, original, proposed)
        self.assertEqual(before, self.snapshot())

    def test_unsafe_catalogue_path_refused(self):
        self.edit_catalog(lambda d: d["constants"]["AutoNav.version"]["targets"][0].update(path="../outside.cs"))
        self.assertEqual(1, self.cli("--check")[0])

    def test_overlap_refused(self):
        self.edit_catalog(lambda d: d["constants"]["AutoNav.version"]["targets"].append(d["constants"]["AutoNav.version"]["targets"][0]))
        self.assertEqual(1, self.cli("--check")[0])

    def test_malformed_pattern_is_machine_readable(self):
        self.edit_catalog(lambda d: d["constants"]["AutoNav.version"]["targets"][0].update(pattern="["))
        self.assertEqual("error", self.cli("--check")[1]["status"])

    def test_catalogue_list_and_duplicate_assignment(self):
        code, result = self.cli("--list")
        self.assertEqual(0, code)
        self.assertIn("AutoNav.version", result["constants"])
        self.assertEqual(1, self.cli("--set", "AutoNav.version=0.10.2", "--set", "AutoNav.version=0.10.3")[0])


if __name__ == "__main__":
    unittest.main()
