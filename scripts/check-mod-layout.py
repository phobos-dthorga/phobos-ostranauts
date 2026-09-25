"""Validate tracked native data and optionally all prepared folders and ZIPs."""
import argparse
import json
from pathlib import Path
import subprocess
import zipfile


def check(root, packages=False):
    tracked = subprocess.check_output(['git', 'ls-files', '-z', '--', 'mods'], cwd=root).decode().split('\0')
    errors, mods = [], []
    for metadata in sorted((root / 'mods').glob('*/mod_info.json')):
        mod = metadata.parent.name
        mods.append(mod)
        files = [p for p in tracked if p.startswith(f'mods/{mod}/data/') and (root / p).is_file()]
        if not files:
            errors.append(f'{mod}: no tracked data file; native loader requires data/')
        if not packages:
            continue
        package = root / 'dist' / f'{mod}-P0'
        try:
            with zipfile.ZipFile(package.with_suffix('.zip')) as archive:
                for source in files:
                    relative = 'Mods/' + source.removeprefix('mods/')
                    expected = (root / source).read_bytes()
                    target = package / relative
                    if not target.is_file() or target.read_bytes() != expected:
                        errors.append(f'{mod}: missing/stale prepared {relative}')
                    if relative not in archive.namelist() or archive.read(relative) != expected:
                        errors.append(f'{mod}: missing/stale ZIP {relative}')
        except (OSError, zipfile.BadZipFile) as exc:
            errors.append(f'{mod}: cannot inspect archive: {exc}')
    if not mods:
        errors.append('No mod metadata found')
    return {'status': 'invalid' if errors else 'valid', 'mods': mods,
            'packagesChecked': packages, 'errors': errors}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--packages', action='store_true')
    args = parser.parse_args()
    result = check(Path(__file__).resolve().parents[1], args.packages)
    print(json.dumps(result, indent=2))
    raise SystemExit(bool(result['errors']))
