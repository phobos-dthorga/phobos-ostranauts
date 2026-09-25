"""Synthetic release records only; no Steam interaction or real mod edits."""
from argparse import Namespace
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("notes", Path(__file__).resolve().parents[1] / "scripts/workshop-release-notes.py")
notes = importlib.util.module_from_spec(spec)
spec.loader.exec_module(notes)


class WorkshopNotesTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.mod = self.root / "mods/PhobosTest"
        self.mod.mkdir(parents=True)
        (self.mod / "mod_info.json").write_text(json.dumps([{"strName": "Phobos Test", "strModVersion": "1.2.3"}]))
        self.changelog = self.mod / "CHANGELOG.md"
        self.changelog.write_text("# Phobos Test changelog\n\n## [Unreleased]\n\nPending.\n\n## [1.2.3] - 2026-09-25 - Draft\n\n### Added\n\n- **Readable** notes.\n- [Source](https://example.com/reference)\n")
        self.page = self.root / "workshop/PhobosTest/page.bbcode"
        self.page.parent.mkdir(parents=True)
        self.page.write_text("[h1]Phobos Test[/h1]\n[b]Version:[/b] 1.2.3\n[b]Publication status:[/b] Not published\n")
        self.output = self.page.parent / "releases/1.2.3.bbcode"

    def run_tool(self, **options):
        values = {"mod": None, "version": None, "write": False, "check": False}
        values.update(options)
        return notes.execute(self.root, Namespace(**values))

    def test_preview_and_missing_check_never_write(self):
        report, code = self.run_tool()
        self.assertEqual(("preview", 0), (report["status"], code))
        self.assertFalse(self.output.exists())
        self.assertEqual(1, self.run_tool(check=True)[1])
        self.assertFalse(self.output.exists())

    def test_write_render_and_idempotence(self):
        before = self.changelog.read_bytes()
        report, code = self.run_tool(write=True)
        self.assertEqual(("written", 0), (report["status"], code))
        text = self.output.read_text()
        self.assertIn("Draft - not published", text)
        self.assertIn("[list]\n[*][b]Readable[/b] notes.", text)
        self.assertIn("[url=https://example.com/reference]Source[/url]", text)
        self.assertNotIn("Pending.", text)
        timestamp = self.output.stat().st_mtime_ns
        self.assertEqual("valid", self.run_tool(write=True)[0]["status"])
        self.assertEqual(timestamp, self.output.stat().st_mtime_ns)
        self.assertEqual(before, self.changelog.read_bytes())

    def test_source_change_detected_as_stale(self):
        self.run_tool(write=True)
        self.changelog.write_text(self.changelog.read_text().replace("Readable", "Revised"))
        self.assertEqual("stale", self.run_tool(check=True)[0]["status"])
        self.run_tool(write=True)
        self.assertEqual(0, self.run_tool(check=True)[1])

    def test_multiple_releases_and_targeted_export(self):
        self.changelog.write_text(self.changelog.read_text() + "\n## [1.2.2] - 2026-09-24 - Released\n\n- Earlier fix.\n")
        report, _ = self.run_tool(mod=["Test"], version="1.2.2", write=True)
        self.assertEqual(1, report["checkedReleases"])
        self.assertFalse(self.output.exists())
        earlier = self.output.with_name("1.2.2.bbcode")
        self.assertIn("[b]Released | 2026-09-24[/b]", earlier.read_text())
        self.run_tool(write=True)
        self.assertTrue(self.output.exists())
        self.assertEqual(2, self.run_tool(check=True)[0]["checkedReleases"])

    def test_unknown_mod_and_version_rejected(self):
        for arguments in [{"mod": ["../escape"]}, {"mod": ["Test", "Test"]}, {"mod": ["Test"], "version": "9.9.9"}]:
            with self.subTest(arguments=arguments), self.assertRaises(notes.NotesError):
                self.run_tool(**arguments)

    def test_duplicate_release_invalid_date_and_missing_unreleased(self):
        text = self.changelog.read_text()
        for bad in [text + "\n## [1.2.3] - 2026-09-25 - Draft\n- Duplicate\n", text.replace("2026-09-25", "2026-02-30"), text.replace("## [Unreleased]", "Pending")]:
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                notes.parse_changelog(bad)

    def test_raw_bbcode_and_unsupported_markdown_rejected(self):
        for body in ["[b]injection[/b]", "- [bad](../local.md)", "| Table |", "  - Nested", "![Image](https://example.com/a.png)", "`code`", "#### Heading", "- **[b]bad[/b]**"]:
            with self.subTest(body=body), self.assertRaises(notes.NotesError):
                notes.render_body(body)

    def test_bad_page_and_stale_version_rejected(self):
        original = self.page.read_text()
        for bad in [original.replace("1.2.3", "1.2.2"), original + "[b]Unclosed", original + "## Markdown", original.replace("[h1]Phobos Test", "[h1]Wrong")]:
            self.page.write_text(bad)
            with self.assertRaises(notes.NotesError):
                self.run_tool(write=True)
            self.assertFalse(self.output.exists())

    def test_new_mod_without_records_blocks_batch(self):
        other = self.root / "mods/PhobosNew"
        other.mkdir()
        (other / "mod_info.json").write_text('[{"strName":"Phobos New","strModVersion":"0.1.0"}]')
        with self.assertRaises(OSError):
            self.run_tool(write=True)
        self.assertFalse(self.output.exists())

    def test_orphan_output_is_retained_and_reported(self):
        self.run_tool(write=True)
        orphan = self.output.with_name("0.0.1.bbcode")
        orphan.write_text("Do not silently delete historical notes")
        with self.assertRaisesRegex(notes.NotesError, "orphan"):
            self.run_tool(write=True)
        self.assertTrue(orphan.exists())

    def test_current_version_needs_entry_even_for_older_export(self):
        path = self.mod / "mod_info.json"
        path.write_text(path.read_text().replace("1.2.3", "1.2.4"))
        with self.assertRaisesRegex(notes.NotesError, "needs a dated"):
            self.run_tool(mod=["Test"], version="1.2.3")

    def test_page_tag_balance_and_attributes(self):
        for text in ["[*]outside", "[b][i]wrong[/b][/i]", "[url=javascript:bad]x[/url]", "[b=oops]x[/b]", "unmatched ["]:
            with self.subTest(text=text), self.assertRaises(notes.NotesError):
                notes.validate_bbcode(text)

    def test_interrupted_write_can_be_regenerated(self):
        before = self.changelog.read_bytes()
        with patch.object(notes, "write_file", side_effect=OSError("Disk failure")):
            with self.assertRaises(OSError):
                self.run_tool(write=True)
        self.assertEqual(before, self.changelog.read_bytes())
        self.run_tool(write=True)
        self.assertEqual(0, self.run_tool(check=True)[1])

    def test_content_change_requires_both_records(self):
        content = ["src/PhobosTest/Plugin.cs"]
        with self.assertRaises(notes.NotesError):
            notes.require_changed_records(content)
        with self.assertRaises(notes.NotesError):
            notes.require_changed_records(content + ["mods/PhobosTest/CHANGELOG.md"])
        notes.require_changed_records(content + ["mods/PhobosTest/CHANGELOG.md", "workshop/PhobosTest/page.bbcode"])

    def test_translation_change_and_docs_only_scope(self):
        with self.assertRaises(notes.NotesError):
            notes.require_changed_records(["translations/PhobosTest/en.json"])
        notes.require_changed_records(["docs/a.md", "mods/PhobosTest/CHANGELOG.md", "scripts/a.py"])


if __name__ == "__main__":
    unittest.main()
