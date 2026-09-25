"""Offline Workshop staging. No network, login, or upload operations exist here."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[1]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def checked(path):
    for part in (path, *path.parents):
        if part.exists() and (part.is_symlink() or getattr(part.stat(), 'st_file_attributes', 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT):
            raise ValueError(f'Filesystem link refused: {part}')
    return path


def files(folder):
    checked(folder)
    if not folder.is_dir():
        raise ValueError(f'Missing folder: {folder}')
    result = {}
    for path in sorted(folder.rglob('*')):
        checked(path)
        if path.is_file():
            result[path.relative_to(folder).as_posix()] = path.read_bytes()
    return result


def quote(value):
    if any(ord(c) < 32 and c not in '\n\r\t' for c in value):
        raise ValueError('Unsupported control character in VDF')
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '').replace('\n', '\\n').replace('\t', '\\t') + '"'


def vdf(values):
    return '"workshopitem"\n{\n' + ''.join(f'  {quote(k)} {quote(str(v))}\n' for k, v in values.items()) + '}\n'


def catalogue(root):
    value = json.loads((root / 'config/workshop-publishing.json').read_text(encoding='utf-8-sig'))
    if value['appId'] != '1022980':
        raise ValueError('Unexpected target game')
    known = {p.parent.name for p in (root / 'mods').glob('*/mod_info.json')}
    if set(value['mods']) != known:
        raise ValueError('Publishing catalogue must cover every native mod')
    ids = []
    for name, item in value['mods'].items():
        if not re.fullmatch(r'Phobos[A-Za-z]+', name):
            raise ValueError('Invalid mod identifier')
        item_id = item['itemId']
        if item_id is not None:
            if not isinstance(item_id, str) or not re.fullmatch(r'[1-9][0-9]*', item_id):
                raise ValueError('Item IDs must be positive decimal strings or null')
            ids.append(item_id)
        if set(item['requires']) - known or name in item['requires']:
            raise ValueError('Invalid dependency')
    if len(ids) != len(set(ids)):
        raise ValueError('Duplicate Workshop item IDs')
    return value


def plan(root, name):
    config = catalogue(root)
    if name not in config['mods']:
        raise ValueError('Unknown mod')
    item = config['mods'][name]
    source = files(root / 'mods' / name)
    info = json.loads(source['mod_info.json'].decode('utf-8-sig'))[0]
    version = info['strModVersion']
    if not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Invalid version')
    spec = importlib.util.spec_from_file_location('release_notes', root / 'scripts/workshop-release-notes.py')
    notes = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(notes)
    _, outputs = notes.plan(root, [name], version)
    if any(before != after for before, after in outputs.values()):
        raise ValueError('Generated release notes are stale; regenerate them')
    package = root / 'dist' / f'{name}-P0'
    native = files(package / 'Mods' / name)
    for relative, data in source.items():
        if native.get(relative) != data:
            raise ValueError(f'Stale package: {name}/{relative}; rebuild first')
    if not any(p.startswith('data/') for p in native):
        raise ValueError('Native data directory is missing')
    if any(Path(p).suffix.lower() not in ('.json', '.png', '.md') for p in native):
        raise ValueError('Unexpected native package file')
    plugins = files(package / 'BepInEx/plugins' / name)
    allowed_dlls = {f'{name}.dll'} | ({'Phobos.Scope.Recording.dll'} if name == 'PhobosFramework' else set())
    if not allowed_dlls <= plugins.keys():
        raise ValueError('Required plugin assembly missing')
    for relative in plugins:
        if relative not in allowed_dlls and not re.fullmatch(r'translations/[A-Za-z0-9-]+\.json', relative):
            raise ValueError(f'Unexpected plugin payload: {relative}')
    # A matching compiled output prevents a package from silently keeping an older DLL.
    for dll in allowed_dlls:
        compiled = checked(root / 'src' / name / 'bin/Release/netstandard2.1' / dll)
        if compiled.read_bytes() != plugins[dll]:
            raise ValueError('Plugin package differs from compiled output; rebuild first')
    translations = root / 'translations' / name
    if translations.is_dir():
        for relative, data in files(translations).items():
            if plugins.get('translations/' + relative) != data:
                raise ValueError('Packaged translations are stale')
    payload = dict(native)
    payload.update({f'BepInEx/plugins/{name}/{p}': data for p, data in plugins.items()})
    for relative, data in files(package).items():
        if '/' not in relative and Path(relative).suffix.lower() in ('.md', '.json', '.html', '.png') or relative == 'LICENSE' or relative.startswith('licenses/'):
            target = 'documentation/' + relative
            payload[target] = data
    payload['documentation/Workshop-page.bbcode'] = (root / 'workshop' / name / 'page.bbcode').read_bytes()
    payload['documentation/Release-notes.bbcode'] = next(iter(outputs.values()))[1]
    blockers = [item['hold']] if item['hold'] else []
    if 'preview.png' not in payload:
        blockers.append('No preview.png; prepare a cover before upload')
    dependencies = list(config['externalRequiredItems'])
    for dependency in item['requires']:
        dependency_id = config['mods'][dependency]['itemId']
        if dependency_id:
            dependencies.append(dependency_id)
        else:
            blockers.append(f'{dependency} has no published item ID yet; publish dependency first')
    report = {'schemaVersion': 1, 'mod': name, 'version': version, 'appId': config['appId'],
              'itemId': item['itemId'], 'operation': 'update' if item['itemId'] else 'create',
              'visibility': 'private', 'uploadEnabled': False, 'blockers': blockers,
              'requiredItems': dependencies, 'subscriptionTested': False,
              'files': {p: digest(data) for p, data in sorted(payload.items())}}
    return report, payload, info['strName']


def prepare(root, name):
    report, payload, title = plan(root, name)
    # New immutable directory each time: no recursive cleanup or stale files.
    target = checked(root / '.local/workshop-staging' / name / (report['version'] + '-' + uuid.uuid4().hex[:12]))
    target.mkdir(parents=True, exist_ok=False)
    for relative, data in payload.items():
        output = target / 'content' / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(data)
    values = {'appid': report['appId'], 'publishedfileid': report['itemId'] or '0',
              'contentfolder': str(target / 'content'), 'previewfile': str(target / 'content/preview.png'),
              'visibility': '2', 'title': title,
              'description': payload['documentation/Workshop-page.bbcode'].decode('utf-8-sig'),
              'changenote': payload['documentation/Release-notes.bbcode'].decode('utf-8-sig')}
    draft = vdf(values).encode('utf-8')
    (target / 'workshop.vdf.draft').write_bytes(draft)
    report['vdfSha256'] = digest(draft)
    report['sourceCommit'] = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    report['workingTreeDirty'] = bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=root))
    (target / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    verify(target)
    return {'status': 'prepared-offline', 'directory': str(target), **report}


def verify(target):
    report = json.loads(checked(target / 'manifest.json').read_text(encoding='utf-8'))
    actual = {p: digest(data) for p, data in files(target / 'content').items()}
    if actual != report['files'] or digest(checked(target / 'workshop.vdf.draft').read_bytes()) != report['vdfSha256']:
        raise ValueError('Staging contents changed; prepare a fresh candidate')
    return {'status': 'verified-offline', 'mod': report['mod'], 'uploadEnabled': False,
            'blockers': report['blockers'], 'fileCount': len(actual)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mod')
    parser.add_argument('--prepare', action='store_true')
    parser.add_argument('--verify', type=Path)
    args = parser.parse_args()
    try:
        if args.verify:
            if args.mod or args.prepare:
                raise ValueError('--verify is a separate operation')
            report = verify(args.verify.resolve())
        else:
            name = args.mod or 'Framework'
            name = name if name.startswith('Phobos') else 'Phobos' + name
            report = prepare(ROOT, name) if args.prepare else plan(ROOT, name)[0]
        print(json.dumps(report, indent=2))
        return 0
    except (ValueError, OSError, KeyError, IndexError) as exc:
        print(json.dumps({'status': 'error', 'error': str(exc), 'uploadEnabled': False}))
        return 1


if __name__ == '__main__':
    sys.exit(main())
