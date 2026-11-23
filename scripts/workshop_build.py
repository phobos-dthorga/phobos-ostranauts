"""Builds one mod's package for a Workshop upload: the catalogue's build command (config/workshop-publishing.json).

phobos-modkit's steam-publish runs it before preparing when asked (`prepare --build`, `update`), as do D'thorga Companion's
**Build first** and **Update all waiting**. Each mod is built by its own script, scripts/build-<mod>.ps1, which needs the game's
folder: it comes from --ostranauts, then OSTRANAUTS_PATH, then OstranautsPath in .local/install-settings.json, never from a
committed file. Builds write only dist/ (and each project's own build output); no game files, load order or saves change.

    python scripts/workshop_build.py PhobosExchange
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SETTINGS = ROOT / '.local' / 'install-settings.json'
# Each mod and its build script. A new mod adds its line here; test_workshop_build.py checks every catalogue mod has one.
SCRIPTS = {
    'PhobosAgriculture': 'build-agriculture.ps1',
    'PhobosAutoNav': 'build-autonav.ps1',
    'PhobosBank': 'build-bank.ps1',
    'PhobosExchange': 'build-exchange.ps1',
    'PhobosFramework': 'build-framework.ps1',
    'PhobosManufacturing': 'build-manufacturing.ps1',
    'PhobosMedical': 'build-medical.ps1',
    'PhobosShipbreaker': 'build-shipbreaker.ps1',
    'PhobosSpacerStories': 'build-spacer-stories.ps1',
    'PhobosWarDeclared': 'build-war-declared.ps1',
}


def game_folder(explicit=None, environ=None):
    environ = os.environ if environ is None else environ
    if explicit:
        return explicit
    if environ.get('OSTRANAUTS_PATH'):
        return environ['OSTRANAUTS_PATH']
    if SETTINGS.is_file():
        return json.loads(SETTINGS.read_text(encoding='utf-8-sig')).get('OstranautsPath')
    return None


def command(mod, game, shell=None):
    """The PowerShell 7 command that builds mod."""
    if mod not in SCRIPTS:
        raise SystemExit(f'No build script is known for {mod}: add it to SCRIPTS in scripts/workshop_build.py')
    shell = shell or shutil.which('pwsh') or 'pwsh'
    return [shell, '-NoProfile', '-File', str(ROOT / 'scripts' / SCRIPTS[mod]), '-OstranautsPath', str(game)]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('mod')
    parser.add_argument('--ostranauts', help="the game's folder (else OSTRANAUTS_PATH, else .local/install-settings.json)")
    args = parser.parse_args(argv)
    game = game_folder(args.ostranauts)
    if not game or not Path(game).is_dir():
        print("The game's folder was not found: set OstranautsPath in .local/install-settings.json, or pass --ostranauts.",
              file=sys.stderr)
        return 2
    return subprocess.run(command(args.mod, game), cwd=ROOT).returncode


if __name__ == '__main__':
    sys.exit(main())
