"""Package the documentation tree with player guides separate from development records."""
from pathlib import Path
import argparse
import os
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
REMOTE = 'https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/'
LINK = re.compile(r'\]\(([^\s)]+)\)')


def render(text, source, destination, mapping, root=ROOT):
    def replace(match):
        url = match.group(1)
        if re.match(r'[a-zA-Z]+:', url) or url.startswith('#'):
            return match.group(0)
        path, mark, fragment = url.partition('#')
        target = (source.parent / path).resolve()
        if target in mapping:
            link = os.path.relpath(mapping[target], destination.parent).replace('\\', '/')
        else:
            try:
                link = REMOTE + target.relative_to(root).as_posix()
            except ValueError:
                return match.group(0)
        return '](' + link + (mark + fragment if mark else '') + ')'
    return LINK.sub(replace, text)


def package_documents(package, readme='', root=ROOT):
    package = package.resolve()
    docs = root / 'docs'
    sources = sorted(docs.rglob('*.md'))
    mapping = {p.resolve(): package / ('GUIDES.md' if p == docs / 'README.md' else p.relative_to(docs)) for p in sources}
    # Existing package assets keep previews and attribution available offline.
    assets = {
        'assets/artwork-completion': 'assets/artwork-completion',
        'assets/phobos-shipbreaker/audio': 'assets/phobos-shipbreaker/audio',
    }
    for folder, output in assets.items():
        for source in (root / folder).glob('*'):
            if source.is_file() and (package / output / source.name).exists():
                mapping[source.resolve()] = package / output / source.name
    for source, output in {
        'assets/phobos-autonav/previews/flight-hub.html': 'polaris-flight-hub-preview.html',
        'assets/phobos-autonav/previews/instruments.html': 'polaris-instruments-preview.html',
        'assets/phobos-autonav/hub-layout.json': 'hub-layout.json',
        'assets/phobos-autonav/hub-prompt.md': 'HUB-ART-PROMPT.md',
        'assets/phobos-autonav/instruments-prompt.md': 'INSTRUMENTS-PROMPT.md',
        'assets/phobos-autonav/pursuit-prompt.md': 'PURSUIT-ART-PROMPT.md',
        'scripts/synthesize-completion-cue.py': 'scripts/synthesize-completion-cue.py',
        'assets/phobos-furnace/coupling-provenance.json': 'coupling-provenance.json',
        'assets/phobos-furnace/coolant-conduit-reuse.md': 'coolant-conduit-reuse.md',
    }.items():
        if (package / output).exists():
            mapping[(root / source).resolve()] = package / output
    # Guide illustrations are copied so the flattened guides show them offline.
    for source, output in {
        'assets/workshop/previews/PhobosWarDeclared-512.png': 'images/war-declared-cover.png',
        'assets/workshop/previews/PhobosSpacerStories-512.png': 'images/spacer-stories-cover.png',
    }.items():
        if (root / source).is_file():
            (package / output).parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(root / source, package / output)
            mapping[(root / source).resolve()] = package / output
    for source in sources:
        destination = mapping[source.resolve()]
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(render(source.read_text(encoding='utf-8-sig'), source, destination, mapping, root), encoding='utf-8', newline='\n')
        # Remove only the known obsolete flat copy of a moved development guide.
        if source.parent == docs / 'development' and source.name != 'README.md':
            old = package / source.name
            if old.is_file() and old.resolve().parent == package and old.resolve() != destination.resolve():
                old.unlink()
    for source, destination in mapping.items():
        if source.suffix == '.md' and not source.is_relative_to(docs):
            destination.write_text(render(source.read_text(encoding='utf-8-sig'), source, destination, mapping, root), encoding='utf-8', newline='\n')
    if readme:
        source = root / readme
        destination = package / 'README.md'
        destination.write_text(render(source.read_text(encoding='utf-8-sig'), source, destination, mapping, root), encoding='utf-8', newline='\n')
    print(f'Packaged {len(sources)} guides; development records are in development/.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, required=True)
    parser.add_argument('--readme', default='')
    args = parser.parse_args()
    package_documents(args.package, args.readme)
