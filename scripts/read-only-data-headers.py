"""First-line notes on the read-only data copies a package ships (owner request, 6 October 2026).

A file under mods/<Mod>/framework/ that the mod's project builds into its DLL is a reference copy: the game reads
the built-in copy, so editing the installed file changes nothing. The package's copy gets one comment line saying so,
above the opening brace; the repository's own file stays plain JSON for the tools that read it. Our loaders skip
comments (Newtonsoft's default), and so does PowerShell 7's ConvertFrom-Json.

    python scripts/read-only-data-headers.py --mod PhobosFramework --package dist/PhobosFramework-P0
    python scripts/read-only-data-headers.py --list
"""
import argparse
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
BOM = b'\xef\xbb\xbf'
MARK = '// Read-only copy.'
GUIDE = 'see the Editing data files guide (editing-data-files.md)'


def read_only_files(root, mod):
    """The native files under framework/ that the mod's project embeds, as paths relative to its mod folder."""
    project = root / 'src' / mod / f'{mod}.csproj'
    if not project.is_file():
        return []
    text = project.read_text(encoding='utf-8-sig')
    found = re.findall(r'<EmbeddedResource\s+Include="\.\./\.\./mods/' + re.escape(mod) + r'/(framework/[^"]+\.json)"', text)
    return sorted(set(found))


def header(mod, relative, data):
    """The comment line for one read-only file: what to do instead of editing it."""
    body = json.loads(data.removeprefix(BOM).decode('utf-8'))
    schema = body.get('schema') if isinstance(body, dict) else None
    lead = f'{MARK} The game uses the copy built into {mod}.dll, so editing this file changes nothing.'
    if isinstance(schema, str) and schema:
        return f'{lead} To change or add values, put a .json file with any name in BepInEx/config/{mod}/{schema}/ holding only what you change; {GUIDE}.'
    name = Path(relative).name
    if name == 'equipment-names.json':
        return f'{lead} Brand and model names cannot be overridden; a translation changes only the rest of each name.'
    if name.startswith('frozen-'):
        kind = 'crop' if 'crops' in name else 'recipe'
        return f'{lead} It holds the fingerprint of every published {kind}, which never changes, so it cannot be overridden.'
    return f'{lead} It cannot be overridden.'


def stamped(mod, relative, data):
    """The packaged bytes: the BOM (if any), the note, the file's own newline, then the file unchanged."""
    bom = BOM if data.startswith(BOM) else b''
    rest = data[len(bom):]
    if rest.startswith(MARK.encode('utf-8')):
        raise ValueError(f'{mod}/{relative} already carries a read-only note')
    newline = b'\r\n' if b'\r\n' in rest else b'\n'
    return bom + header(mod, relative, data).encode('utf-8') + newline + rest


def stamp(root, mod, package):
    native = Path(package) / 'Mods' / mod
    done = []
    for relative in read_only_files(root, mod):
        target = native / relative
        if not target.is_file():
            raise ValueError(f'Package lacks {mod}/{relative}')
        source = (root / 'mods' / mod / relative).read_bytes()
        if target.read_bytes() != source:
            raise ValueError(f'Packaged {mod}/{relative} differs from the repository copy; rebuild the package')
        target.write_bytes(stamped(mod, relative, source))
        done.append(relative)
    return done


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mod')
    parser.add_argument('--package')
    parser.add_argument('--list', action='store_true', help='print each mod\'s read-only files and their notes')
    args = parser.parse_args()
    if args.list:
        for info in sorted((ROOT / 'mods').glob('*/mod_info.json')):
            mod = info.parent.name
            for relative in read_only_files(ROOT, mod):
                print(f'{mod}/{relative}: {header(mod, relative, (ROOT / "mods" / mod / relative).read_bytes())}')
        return 0
    if not args.mod or not args.package:
        parser.error('--mod and --package are required unless --list')
    for relative in stamp(ROOT, args.mod, args.package):
        print(f'Read-only note: {args.mod}/{relative}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
