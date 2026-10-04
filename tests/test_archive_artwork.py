"""Safety checks for exact archival, selected-source guards and isolated Git indexes."""
import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("archive_artwork", ROOT / "scripts/archive-artwork.py")
archive = importlib.util.module_from_spec(spec)
spec.loader.exec_module(archive)


class ArchiveSafetyTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="phobos-artwork-archive-test-")
        self.root = Path(self.directory.name).resolve()
        self.assets = self.root / "assets"
        folder = self.assets / "artwork-completion/source"
        folder.mkdir(parents=True)
        self.old = folder / "old.png"
        self.current = folder / "current.png"
        self.original = b"retained original binary\x00\xff"
        self.old.write_bytes(self.original)
        self.current.write_bytes(b"selected image")
        (folder.parent / "manifest.json").write_text(json.dumps({"assets": [{"status": "selected", "source": "source/current.png"}]}))
        (self.assets / "rejected-artwork-archive.json").write_text(json.dumps({"assets": []}))
        (self.root / "unrelated.txt").write_text("before")
        self.git("init", "-q", "-b", "main")
        self.git("config", "user.name", "Archive fixture")
        self.git("config", "user.email", "archive-fixture@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("add", ".")
        self.git("commit", "-qm", "Fixture")
        self.git("branch", "codex/rejected-artwork")
        self.head = self.git("rev-parse", "HEAD")

    def tearDown(self):
        self.directory.cleanup()

    def git(self, *args):
        return subprocess.check_output(["git", *args], cwd=self.root, stderr=subprocess.DEVNULL)

    def run_archive(self, path):
        argv = ["archive-artwork", str(path), "--date", "2026-10-05", "--reason", "Superseded fixture", "--remove"]
        with patch.object(archive, "ROOT", self.root), patch.object(archive, "ASSETS", self.assets), patch.object(sys, "argv", argv), contextlib.redirect_stdout(io.StringIO()) as output:
            archive.main()
        return json.loads(output.getvalue())

    def test_exact_archive_keeps_main_head_and_staged_work(self):
        (self.root / "unrelated.txt").write_text("staged work")
        self.git("add", "unrelated.txt")
        staged = self.git("diff", "--cached", "--binary")
        result = self.run_archive(self.old)
        self.assertEqual(self.git("show", result["commit"] + ":assets/artwork-completion/source/old.png"), self.original)
        self.assertEqual(self.git("rev-parse", "HEAD"), self.head)
        self.assertEqual(self.git("diff", "--cached", "--binary"), staged)
        self.assertFalse(self.old.exists())
        self.assertTrue(self.current.exists())
        with self.assertRaises(ValueError):
            self.run_archive(self.old)
        inventory = json.loads((self.assets / "rejected-artwork-archive.json").read_text())
        self.assertEqual(len(inventory["assets"]), 1)

    def test_selected_source_cannot_be_removed(self):
        with self.assertRaisesRegex(ValueError, "Selected artwork"):
            self.run_archive(self.current)
        self.assertTrue(self.current.exists())
        self.assertEqual(self.git("rev-parse", "codex/rejected-artwork"), self.head)

    def test_resolved_path_outside_assets_is_rejected(self):
        outside = self.root / "outside.png"
        outside.write_bytes(self.original)
        with self.assertRaisesRegex(ValueError, "within assets"):
            self.run_archive(self.assets / "../outside.png")
        self.assertEqual(outside.read_bytes(), self.original)
        self.assertEqual(self.git("rev-parse", "codex/rejected-artwork"), self.head)


if __name__ == "__main__":
    unittest.main()
