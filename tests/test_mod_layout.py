import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('layout', Path(__file__).resolve().parents[1] / 'scripts/check-mod-layout.py')
layout = importlib.util.module_from_spec(spec)
spec.loader.exec_module(layout)

class LayoutTests(unittest.TestCase):
    def test_source_and_download_regressions(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / 'mods/PhobosExample'
            source.mkdir(parents=True)
            (source / 'mod_info.json').write_text('[]')
            with patch.object(layout.subprocess, 'check_output', return_value=b''):
                self.assertEqual(layout.check(root)['status'], 'invalid')
            (source / 'data').mkdir()
            marker = source / 'data/README.md'
            marker.write_text('Keep data')
            with patch.object(layout.subprocess, 'check_output', return_value=b'mods/PhobosExample/data/README.md\0'):
                self.assertEqual(layout.check(root)['status'], 'valid')
                self.assertEqual(layout.check(root, True)['status'], 'invalid')
                package = root / 'dist/PhobosExample-P0'
                target = package / 'Mods/PhobosExample/data/README.md'
                target.parent.mkdir(parents=True)
                target.write_bytes(marker.read_bytes())
                with zipfile.ZipFile(package.with_suffix('.zip'), 'w') as archive:
                    archive.writestr('Mods/PhobosExample/data/README.md', marker.read_bytes())
                self.assertEqual(layout.check(root, True)['status'], 'valid')
                target.unlink()
                self.assertEqual(layout.check(root, True)['status'], 'invalid')

if __name__ == '__main__':
    unittest.main()
