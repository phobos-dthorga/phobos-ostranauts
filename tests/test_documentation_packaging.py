import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('package_docs', ROOT / 'scripts/package-documentation.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class DocumentationPackagingTests(unittest.TestCase):
    def test_separation_links_readme_and_obsolete_flat_copy(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            docs = root / 'docs'
            dev = docs / 'development'
            dev.mkdir(parents=True)
            (docs / 'README.md').write_text('[Player](player.md) [Research](development/research.md)', encoding='utf-8')
            (docs / 'player.md').write_text('[Research](development/research.md#limits)', encoding='utf-8')
            (dev / 'research.md').write_text('[Player](../player.md) [Source](../../src/a.cs) [Old](https://github.com/example/blob/abc/docs/old.md)', encoding='utf-8')
            package = root / 'package'
            package.mkdir()
            (package / 'research.md').write_text('old flat record')
            (package / 'unrelated.md').write_text('keep')
            module.package_documents(package, 'docs/development/research.md', root)
            self.assertFalse((package / 'research.md').exists())
            self.assertEqual((package / 'unrelated.md').read_text(), 'keep')
            self.assertIn('(development/research.md#limits)', (package / 'player.md').read_text())
            self.assertIn('(../player.md)', (package / 'development/research.md').read_text())
            self.assertIn('(player.md)', (package / 'README.md').read_text())
            self.assertIn(module.REMOTE + 'src/a.cs', (package / 'README.md').read_text())
            self.assertIn('blob/abc/docs/old.md', (package / 'README.md').read_text())
            self.assertTrue((package / 'GUIDES.md').exists())
            module.package_documents(package, 'docs/development/research.md', root)
            self.assertTrue((package / 'development/research.md').exists())

    def test_guide_illustration_is_copied_and_linked_offline(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'docs').mkdir()
            previews = root / 'assets/workshop/previews'
            previews.mkdir(parents=True)
            (previews / 'PhobosWarDeclared-512.png').write_bytes(b'png')
            (root / 'docs/guide.md').write_text('![Cover](../assets/workshop/previews/PhobosWarDeclared-512.png)', encoding='utf-8')
            package = root / 'package'
            package.mkdir()
            module.package_documents(package, '', root)
            self.assertEqual((package / 'images/war-declared-cover.png').read_bytes(), b'png')
            self.assertIn('(images/war-declared-cover.png)', (package / 'guide.md').read_text())
