"""Offline Workshop staging. No network, login, or upload operations exist here.

The separate owner-run scripts/upload-workshop.ps1 consumes these candidates.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import stat
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[1]
CATALOGUE = 'config/workshop-publishing.json'
# Steamworks ISteamUGC buffers include the terminating NUL: k_cchPublishedDocumentTitleMax
# is 129, k_cchPublishedDocumentDescriptionMax and ...ChangeDescriptionMax are 8000 (UTF-8 bytes).
TITLE_MAX_BYTES = 128
TEXT_MAX_BYTES = 7999
# Our own conservative cover budget (assets/workshop/README.md), not a Valve figure.
PREVIEW_MAX_BYTES = 1_000_000
VISIBILITY = {'public': 0, 'friends': 1, 'private': 2, 'unlisted': 3}
VERSION = re.compile(r'\d+\.\d+\.\d+')


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
    """VDF value with literal newlines. Quotes and backslashes are refused, not escaped:
    SteamCMD's escape handling is undocumented, so text that needs neither is unambiguous."""
    value = value.replace('\r', '')
    if '"' in value or '\\' in value:
        raise ValueError('Workshop text and paths must not contain " or \\ (use typographic quotes and forward slashes)')
    if any(ord(c) < 32 and c not in '\n\t' for c in value):
        raise ValueError('Unsupported control character in VDF')
    return '"' + value + '"'


def vdf(values):
    return '"workshopitem"\n{\n' + ''.join(f'  {quote(k)} {quote(str(v))}\n' for k, v in values.items()) + '}\n'


def catalogue(root):
    value = json.loads((root / CATALOGUE).read_text(encoding='utf-8-sig'))
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
        # The last version sent to this item; its change notes begin after it (optional, written by the uploader).
        uploaded = item.get('uploadedVersion')
        if uploaded is not None and (item_id is None or not isinstance(uploaded, str) or not VERSION.fullmatch(uploaded)):
            raise ValueError('uploadedVersion must be a version number, and only on a mod with an item ID')
        if item.get('uploadedVisibility') is not None and (uploaded is None or item['uploadedVisibility'] not in VISIBILITY):
            raise ValueError('uploadedVisibility must be private, friends, unlisted or public, beside an uploadedVersion')
    for item_id in value['externalRequiredItems']:
        if not re.fullmatch(r'[1-9][0-9]*', item_id) or item_id not in value.get('externalRequiredItemNames', {}):
            raise ValueError('External required items need a decimal ID and a name')
    if len(ids) != len(set(ids)):
        raise ValueError('Duplicate Workshop item IDs')
    publication_order(value)
    return value


def publication_order(config):
    """Dependencies first; a cycle cannot be published."""
    order, pending = [], dict(config['mods'])
    while pending:
        ready = sorted(n for n, item in pending.items() if set(item['requires']) <= set(order))
        if not ready:
            raise ValueError('Circular Phobos dependencies')
        order += ready
        for name in ready:
            del pending[name]
    return order


def module(root, name, path):
    spec = importlib.util.spec_from_file_location(name, root / path)
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


def limit(label, data, maximum):
    if len(data) > maximum:
        raise ValueError(f'{label} is {len(data)} UTF-8 bytes; Steam accepts at most {maximum}. Shorten it before upload')


def version_key(version):
    return tuple(int(part) for part in version.split('.'))


def change_note(root, notes, name, version, uploaded):
    """Steam's change note: every changelog version after the last one uploaded to the item, newest first.
    A first upload (or a repeat of the same version) carries the current version alone. Versions that would
    push the note past Steam's limit are named in a closing line instead, oldest dropped first."""
    releases = notes.parse_changelog((root / 'mods' / name / 'CHANGELOG.md').read_text(encoding='utf-8-sig'))
    since = version_key(uploaded or version)
    versions = sorted((v for v in releases if since < version_key(v) <= version_key(version)), key=version_key, reverse=True) or [version]
    texts = []
    for release in versions:
        _, outputs = notes.plan(root, [name], release)
        before, after = next(iter(outputs.values()))
        if before != after:
            raise ValueError('Generated release notes are stale; regenerate them')
        texts.append(after)

    def closing(left):
        span = left[0] if len(left) == 1 else f'{left[-1]} to {left[0]}'
        word = 'version' if len(left) == 1 else 'versions'
        return f'Earlier changes in {word} {span} are listed in CHANGELOG.md, which comes with the mod.\n'.encode('utf-8')
    for count in range(len(texts), 0, -1):
        note = b'\n'.join(texts[:count]) + (b'\n' + closing(versions[count:]) if count < len(texts) else b'')
        if len(note) <= TEXT_MAX_BYTES or count == 1:
            return note, versions[:count], versions[count:]


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
    notes = module(root, 'release_notes', 'scripts/workshop-release-notes.py')
    uploaded = item.get('uploadedVersion') if item['itemId'] else None
    note, covered, omitted = change_note(root, notes, name, version, uploaded)
    page = (root / 'workshop' / name / 'page.bbcode').read_bytes()
    limit('Workshop title', info['strName'].encode('utf-8'), TITLE_MAX_BYTES)
    limit('Workshop page description', page.decode('utf-8-sig').encode('utf-8'), TEXT_MAX_BYTES)
    limit('Release change note', note, TEXT_MAX_BYTES)
    quote(page.decode('utf-8-sig'))
    quote(note.decode('utf-8'))
    package = root / 'dist' / f'{name}-P0'
    native = files(package / 'Mods' / name)
    # The data copies built into the DLL ship with a first-line read-only note (scripts/read-only-data-headers.py).
    headers = module(ROOT, 'read_only_headers', 'scripts/read-only-data-headers.py')
    read_only = set(headers.read_only_files(root, name))
    for relative, data in source.items():
        expected = headers.stamped(name, relative, data) if relative in read_only else data
        if native.get(relative) != expected:
            raise ValueError(f'Stale package: {name}/{relative}; rebuild first')
    # A data-only add-on (a story collection) has no plugin source: its manifest and phobos/ files are the mod.
    data_only = not (root / 'src' / name).is_dir() and 'phobos-addon.json' in source
    if data_only and not any(p.startswith('phobos/') for p in native):
        raise ValueError('Add-on data directory is missing')
    if not any(p.startswith('data/') for p in native):
        raise ValueError('Native data directory is missing')
    if any(Path(p).suffix.lower() not in ('.json', '.png', '.md') for p in native):
        raise ValueError('Unexpected native package file')
    plugins = files(package / 'BepInEx/plugins' / name) if (package / 'BepInEx/plugins' / name).is_dir() else {}
    allowed_dlls = set() if data_only else {f'{name}.dll'} | ({'Phobos.Scope.Recording.dll'} if name == 'PhobosFramework' else set())
    if data_only and plugins:
        raise ValueError('A data-only add-on carries no plugin payload')
    if not allowed_dlls <= plugins.keys():
        raise ValueError('Required plugin assembly missing')
    # Framework's loose sounds (0.120.0): exactly the mapped files, each identical to its repository source.
    sounds = {}
    if name == 'PhobosFramework' and (root / 'config/framework-sounds.json').is_file():
        sounds = json.loads((root / 'config/framework-sounds.json').read_text(encoding='utf-8-sig'))['files']
    for relative in plugins:
        if relative not in allowed_dlls and relative not in sounds and not re.fullmatch(r'translations/[A-Za-z0-9-]+\.json', relative):
            raise ValueError(f'Unexpected plugin payload: {relative}')
    for relative, origin in sounds.items():
        if plugins.get(relative) != checked(root / origin).read_bytes():
            raise ValueError(f'Packaged sound {relative} is missing or stale; rebuild first')
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
    payload['documentation/Workshop-page.bbcode'] = page
    payload['documentation/Release-notes.bbcode'] = note
    blockers = [item['hold']] if item['hold'] else []
    if 'preview.png' not in payload:
        blockers.append('No preview.png; prepare a cover before upload')
    elif len(payload['preview.png']) >= PREVIEW_MAX_BYTES:
        blockers.append('preview.png exceeds the 1,000,000-byte cover budget')
    dependencies = list(config['externalRequiredItems'])
    required = [{'name': config['externalRequiredItemNames'][i], 'itemId': i} for i in config['externalRequiredItems']]
    for dependency in item['requires']:
        dependency_id = config['mods'][dependency]['itemId']
        required.append({'name': dependency, 'itemId': dependency_id})
        if dependency_id:
            dependencies.append(dependency_id)
        else:
            blockers.append(f'{dependency} has no published item ID yet; publish dependency first')
    report = {'schemaVersion': 2, 'mod': name, 'title': info['strName'], 'version': version, 'appId': config['appId'],
              'itemId': item['itemId'], 'operation': 'update' if item['itemId'] else 'create',
              'visibility': 'private', 'uploadEnabled': False, 'blockers': blockers,
              'requiredItems': dependencies, 'requiredItemDetails': required, 'subscriptionTested': False,
              'uploadedVersion': uploaded, 'changeNoteVersions': covered, 'changeNoteOmitted': omitted,
              'sizes': {'titleBytes': len(info['strName'].encode('utf-8')), 'descriptionBytes': len(page),
                        'changeNoteBytes': len(note)},
              'files': {p: digest(data) for p, data in sorted(payload.items())}}
    return report, payload, info['strName']


def draft_values(report, target, title, description, change_note, visibility='private'):
    content = target / 'content'
    return {'appid': report['appId'], 'publishedfileid': report['itemId'] or '0',
            'contentfolder': content.resolve().as_posix(), 'previewfile': (content / 'preview.png').resolve().as_posix(),
            'visibility': str(VISIBILITY[visibility]), 'title': title,
            'description': description, 'changenote': change_note}


def prepare(root, name):
    report, payload, title = plan(root, name)
    # New immutable directory each time: no recursive cleanup or stale files.
    target = checked(root / '.local/workshop-staging' / name / (report['version'] + '-' + uuid.uuid4().hex[:12]))
    target.mkdir(parents=True, exist_ok=False)
    for relative, data in payload.items():
        output = target / 'content' / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(data)
    values = draft_values(report, target, title, payload['documentation/Workshop-page.bbcode'].decode('utf-8-sig'),
                          payload['documentation/Release-notes.bbcode'].decode('utf-8-sig'))
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
    return {'status': 'verified-offline', 'mod': report['mod'], 'version': report['version'],
            'operation': report['operation'], 'itemId': report['itemId'], 'uploadEnabled': False,
            'blockers': report['blockers'], 'requiredItemDetails': report.get('requiredItemDetails', []),
            'sourceCommit': report.get('sourceCommit'), 'workingTreeDirty': report.get('workingTreeDirty'),
            'fileCount': len(actual)}


def upload_vdf(target, visibility, output):
    """Write the upload VDF for an owner-run SteamCMD session; the candidate stays unchanged."""
    if visibility not in VISIBILITY:
        raise ValueError('Visibility must be private, friends, unlisted or public')
    verify(target)
    report = json.loads((target / 'manifest.json').read_text(encoding='utf-8'))
    content = target / 'content'
    values = draft_values(report, target, report.get('title') or json.loads((content / 'mod_info.json').read_text(encoding='utf-8-sig'))[0]['strName'],
                          (content / 'documentation/Workshop-page.bbcode').read_text(encoding='utf-8-sig'),
                          (content / 'documentation/Release-notes.bbcode').read_text(encoding='utf-8-sig'), visibility)
    data = vdf(values).encode('utf-8')
    output = Path(output)
    if output.exists():
        raise ValueError('Upload VDF already exists; use a fresh receipt directory')
    output.write_bytes(data)
    return {'status': 'upload-vdf-written', 'path': str(output), 'visibility': visibility,
            'operation': report['operation'], 'itemId': report['itemId'], 'sha256': digest(data)}


def status(root):
    """Publishing overview for every mod, dependencies first; never writes."""
    config = catalogue(root)
    rows = []
    for name in publication_order(config):
        item = config['mods'][name]
        try:
            report = plan(root, name)[0]
            row = {'mod': name, 'version': report['version'], 'itemId': item['itemId'], 'operation': report['operation'],
                   'package': 'ready', 'blockers': report['blockers'], 'sizes': report['sizes']}
        except (ValueError, OSError, KeyError, IndexError) as error:
            row = {'mod': name, 'itemId': item['itemId'], 'package': 'not ready', 'error': str(error),
                   'blockers': [item['hold']] if item['hold'] else []}
        row['requires'] = item['requires']
        rows.append(row)
    return {'schemaVersion': 1, 'status': 'overview', 'uploadEnabled': False, 'publicationOrder': [r['mod'] for r in rows], 'mods': rows}


def record_item_id(root, name, item_id):
    """Save a Steam-assigned item ID after the owner has seen the item exist."""
    if not re.fullmatch(r'[1-9][0-9]*', item_id or ''):
        raise ValueError('Item IDs are positive decimal numbers')
    path = root / CATALOGUE
    config = json.loads(path.read_text(encoding='utf-8-sig'))
    if name not in config['mods']:
        raise ValueError('Unknown mod')
    current = config['mods'][name]['itemId']
    if current == item_id:
        return {'status': 'unchanged', 'mod': name, 'itemId': item_id}
    if current is not None:
        raise ValueError(f'{name} already has item ID {current}; correct the catalogue by hand if Steam disagrees')
    if any(m['itemId'] == item_id for m in config['mods'].values()) or item_id in config['externalRequiredItems']:
        raise ValueError('That item ID already belongs to another entry')
    config['mods'][name]['itemId'] = item_id
    path.write_bytes((json.dumps(config, indent=2, ensure_ascii=False) + '\n').encode('utf-8'))
    catalogue(root)
    return {'status': 'recorded', 'mod': name, 'itemId': item_id, 'catalogue': CATALOGUE}


def pending(root):
    """Which items need an upload, from the catalogue and mod versions alone: no package checks, never writes.
    An item needs one when its mod's version differs from the version last uploaded to it."""
    config = catalogue(root)
    rows = []
    for name in publication_order(config):
        item = config['mods'][name]
        version = json.loads((root / 'mods' / name / 'mod_info.json').read_text(encoding='utf-8-sig'))[0]['strModVersion']
        rows.append({'mod': name, 'version': version, 'itemId': item['itemId'],
                     'uploadedVersion': item.get('uploadedVersion'), 'uploadedVisibility': item.get('uploadedVisibility'),
                     'needsUpload': bool(item['itemId']) and item.get('uploadedVersion') != version})
    return {'schemaVersion': 1, 'status': 'pending', 'uploadEnabled': False, 'mods': rows}


def record_uploaded_version(root, name, version, visibility=None):
    """Save the version (and visibility) just sent to a mod's item, so the next change note starts after it
    and an update with nothing new is skipped."""
    if not VERSION.fullmatch(version or ''):
        raise ValueError('Uploaded versions are numbers like 1.2.3')
    if visibility is not None and visibility not in VISIBILITY:
        raise ValueError('Visibility must be private, friends, unlisted or public')
    path = root / CATALOGUE
    config = json.loads(path.read_text(encoding='utf-8-sig'))
    if name not in config['mods']:
        raise ValueError('Unknown mod')
    if not config['mods'][name]['itemId']:
        raise ValueError(f'{name} has no item ID; record the item before its uploaded version')
    item = config['mods'][name]
    visibility = visibility or item.get('uploadedVisibility')
    if item.get('uploadedVersion') == version and item.get('uploadedVisibility') == visibility:
        return {'status': 'unchanged', 'mod': name, 'uploadedVersion': version, 'uploadedVisibility': visibility}
    item['uploadedVersion'] = version
    if visibility:
        item['uploadedVisibility'] = visibility
    path.write_bytes((json.dumps(config, indent=2, ensure_ascii=False) + '\n').encode('utf-8'))
    catalogue(root)
    return {'status': 'recorded', 'mod': name, 'uploadedVersion': version, 'uploadedVisibility': visibility, 'catalogue': CATALOGUE}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mod')
    parser.add_argument('--prepare', action='store_true')
    parser.add_argument('--verify', type=Path)
    parser.add_argument('--status', action='store_true', help='Publishing overview of every mod; read-only')
    parser.add_argument('--pending', action='store_true', help='Which items have a newer mod version than the one uploaded; read-only')
    parser.add_argument('--record-item-id', metavar='ID', help='Save a real Steam item ID for --mod')
    parser.add_argument('--record-uploaded-version', metavar='VERSION', help='Save the version just uploaded to the item of --mod')
    parser.add_argument('--upload-vdf', type=Path, metavar='CANDIDATE', help='Write an upload VDF for a verified candidate')
    parser.add_argument('--visibility', choices=sorted(VISIBILITY), help='For --upload-vdf (default private) or --record-uploaded-version')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        name = args.mod or 'Framework'
        name = name if name.startswith('Phobos') else 'Phobos' + name
        if args.verify:
            if args.mod or args.prepare:
                raise ValueError('--verify is a separate operation')
            report = verify(args.verify.resolve())
        elif args.status:
            report = status(ROOT)
        elif args.pending:
            report = pending(ROOT)
        elif args.record_item_id:
            if not args.mod:
                raise ValueError('--record-item-id needs --mod')
            report = record_item_id(ROOT, name, args.record_item_id)
        elif args.record_uploaded_version:
            if not args.mod:
                raise ValueError('--record-uploaded-version needs --mod')
            report = record_uploaded_version(ROOT, name, args.record_uploaded_version, args.visibility)
        elif args.upload_vdf:
            if not args.output:
                raise ValueError('--upload-vdf needs --output')
            report = upload_vdf(args.upload_vdf.resolve(), args.visibility or 'private', args.output)
        else:
            report = prepare(ROOT, name) if args.prepare else plan(ROOT, name)[0]
        print(json.dumps(report, indent=2))
        return 0
    except (ValueError, OSError, KeyError, IndexError) as exc:
        print(json.dumps({'status': 'error', 'error': str(exc), 'uploadEnabled': False}))
        return 1


if __name__ == '__main__':
    sys.exit(main())
