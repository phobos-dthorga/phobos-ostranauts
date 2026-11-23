"""The Workshop build command: every catalogue mod has a build script, and the command names it with the game's folder."""
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('workshop_build', ROOT / 'scripts' / 'workshop_build.py')
build = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(build)


class WorkshopBuild(unittest.TestCase):
    def test_every_catalogue_mod_has_a_build_script(self):
        catalogue = json.loads((ROOT / 'config/workshop-publishing.json').read_text(encoding='utf-8-sig'))
        self.assertEqual(set(catalogue['mods']), set(build.SCRIPTS))
        for script in build.SCRIPTS.values():
            self.assertTrue((ROOT / 'scripts' / script).is_file(), script)

    def test_the_catalogue_names_this_command(self):
        catalogue = json.loads((ROOT / 'config/workshop-publishing.json').read_text(encoding='utf-8-sig'))
        self.assertEqual(catalogue['layout']['build'], ['python', 'scripts/workshop_build.py', '{mod}'])

    def test_command_and_game_folder(self):
        command = build.command('PhobosExchange', 'C:/Games/Ostranauts', shell='pwsh')
        self.assertEqual(command[:3], ['pwsh', '-NoProfile', '-File'])
        self.assertTrue(command[3].endswith('build-exchange.ps1'))
        self.assertEqual(command[4:], ['-OstranautsPath', 'C:/Games/Ostranauts'])
        self.assertEqual(build.game_folder('X', {}), 'X')
        self.assertEqual(build.game_folder(None, {'OSTRANAUTS_PATH': 'Y'}), 'Y')
        with self.assertRaises(SystemExit):
            build.command('PhobosNothing', 'C:/Games/Ostranauts')


if __name__ == '__main__':
    unittest.main()
