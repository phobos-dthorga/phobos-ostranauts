"""What a Phobos Ostranauts Workshop item holds: the built package, checked against its sources (phobos-modkit's payload hook).

config/workshop-publishing.json names this file as its layout's payloadHook, so every runner of the shared steam-publish
(scripts/workshop.py, D'thorga Companion's Publish page) builds the same item. The checks are the ones scripts/prepare-workshop.py
made before it (moved 23 Nov 2026; that script and the PowerShell uploader stay until the shared uploader has made a real
upload, the owner's choice): the package must be current with its sources, carry only the files a mod may carry, and its plugin
must match the compiled output. The shared tool adds the page and change note under documentation/ (layout textFolder).
"""
from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import re
import stat

ROOT = Path(__file__).resolve().parents[1]


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


def _module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def payload(project, name):
    """{item path: bytes} for mod name, from dist/<name>-P0; raises when the package is stale or carries the wrong files."""
    root = Path(project.root)
    source = files(root / 'mods' / name)
    package = root / 'dist' / f'{name}-P0'
    native = files(package / 'Mods' / name)
    # The data copies built into the DLL ship with a first-line read-only note (scripts/read-only-data-headers.py).
    headers = _module('read_only_headers', ROOT / 'scripts/read-only-data-headers.py')
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
    result = dict(native)
    result.update({f'BepInEx/plugins/{name}/{p}': data for p, data in plugins.items()})
    for relative, data in files(package).items():
        if '/' not in relative and Path(relative).suffix.lower() in ('.md', '.json', '.html', '.png') or relative == 'LICENSE' \
                or relative.startswith('licenses/'):
            result['documentation/' + relative] = data
    return result
