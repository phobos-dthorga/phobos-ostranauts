"""Bring the repository's Workshop records up to date with what Steam shows.

The reverse of an upload: reads each item's live state from the Steam Web API
(ISteamRemoteStorage/GetPublishedFileDetails, no key or login) and, for the
selected areas, updates the repository to match:

  --visibility   the item's visibility in config/workshop-publishing.json
  --description  workshop/<ModId>/page.bbcode from the item's live description
  --changelog    the uploaded version's changelog entry marked Released, with
                 its release notes regenerated

Without --write it only reports what would change. Steam answers only for items
anyone can see: a private or friends-only item reads as not found, and its
records are left alone. Never uploads, logs in or handles credentials.
See docs/development/workshop-upload-preparation.md.
"""
import argparse
from datetime import datetime
import importlib.util
import json
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
CATALOGUE = 'config/workshop-publishing.json'
DETAILS_URL = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/'
# EWorkshopFileVisibility values, named as the catalogue records them.
VISIBILITY = {0: 'public', 1: 'friends', 2: 'private', 3: 'unlisted'}
FOUND = 1  # EResult OK; anything else (usually 9, file not found) means Steam would not show it


def module(root, name, path):
    spec = importlib.util.spec_from_file_location(name, root / path)
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


def fetch_details(item_ids):
    """Live details for each item ID, as Steam returns them to anyone."""
    form = {'itemcount': len(item_ids)}
    form.update({f'publishedfileids[{i}]': item for i, item in enumerate(item_ids)})
    request = urllib.request.Request(DETAILS_URL, urllib.parse.urlencode(form).encode('ascii'))
    with urllib.request.urlopen(request, timeout=30) as response:
        details = json.load(response)['response']['publishedfiledetails']
    return {d['publishedfileid']: d for d in details}


def steam_page(description):
    """Steam's description in the repository's form: LF line endings and one closing newline."""
    return description.replace('\r\n', '\n').rstrip() + '\n'


def plan(root, mods, areas, fetch=fetch_details):
    """Each selected mod's changes, as (path, before, after) triples plus notes; nothing is written."""
    catalogue = json.loads((root / CATALOGUE).read_text(encoding='utf-8-sig'))
    selected = mods or [name for name, item in catalogue['mods'].items() if item['itemId']]
    selected = [m if m.startswith('Phobos') else 'Phobos' + m for m in selected]
    unknown = [m for m in selected if m not in catalogue['mods']]
    if unknown:
        raise ValueError(f'Unknown mod: {", ".join(unknown)}')
    items = {m: catalogue['mods'][m]['itemId'] for m in selected if catalogue['mods'][m]['itemId']}
    details = fetch(list(items.values())) if items else {}
    notes = module(root, 'release_notes', 'scripts/workshop-release-notes.py')
    rows, changes, new_catalogue = [], {}, json.loads(json.dumps(catalogue))
    for name in selected:
        item = catalogue['mods'][name]
        row = {'mod': name, 'itemId': item['itemId'], 'changes': [], 'notes': []}
        rows.append(row)
        if not item['itemId']:
            row['notes'].append('Not on the Workshop yet; nothing to pull.')
            continue
        live = details.get(item['itemId'], {})
        if live.get('result') != FOUND:
            row['notes'].append('Steam does not show this item to the public (private, friends-only or removed); its records are left as they are.')
            continue
        visibility = VISIBILITY.get(live.get('visibility'))
        version = json.loads((root / 'mods' / name / 'mod_info.json').read_text(encoding='utf-8-sig'))[0]['strModVersion']
        row.update(visibility=visibility, updated=datetime.fromtimestamp(live['time_updated']).date().isoformat())
        if visibility == 'public' and item['hold']:
            row['notes'].append(f'Public on Steam but held in the catalogue ({item["hold"]}). Lift the hold if that is intended.')
        if 'visibility' in areas and visibility and item.get('uploadedVisibility') != visibility:
            new_catalogue['mods'][name]['uploadedVisibility'] = visibility
            row['changes'].append(f'Visibility: {item.get("uploadedVisibility")} -> {visibility}')
        if 'description' in areas:
            path = root / 'workshop' / name / 'page.bbcode'
            before = path.read_text(encoding='utf-8-sig')
            after = steam_page(live.get('description', ''))
            if after != before:
                shown = re.search(r'(?m)^\[b\]Version:\[/b\] (\S+)$', after)
                if not shown or shown[1] != version:
                    row['notes'].append(f'Steam describes version {shown[1] if shown else "(none)"} but the source is {version}; the page was not pulled, so a newer page is not overwritten.')
                else:
                    changes[path] = (before, after)
                    row['changes'].append('Page description: replaced by the live Steam text')
        if 'changelog' in areas and visibility == 'public':
            uploaded = item.get('uploadedVersion')
            path = root / 'mods' / name / 'CHANGELOG.md'
            before = changes.get(path, (None, path.read_text(encoding='utf-8-sig')))[1]
            heading = re.compile(r'(?m)^## \[' + re.escape(uploaded or '') + r'\] - \d{4}-\d{2}-\d{2} - Draft$')
            if uploaded and heading.search(before):
                after = heading.sub(f'## [{uploaded}] - {row["updated"]} - Released', before)
                changes[path] = (before, after)
                row['changes'].append(f'Changelog: {uploaded} marked Released on {row["updated"]} (Steam's last update of the item)')
    if new_catalogue != catalogue:
        text = json.dumps(new_catalogue, indent=2, ensure_ascii=False) + '\n'
        changes[root / CATALOGUE] = ((root / CATALOGUE).read_text(encoding='utf-8-sig'), text)
    return rows, changes, notes


def apply(root, changes, notes):
    """Write the planned files, then regenerate the release notes of every changed changelog."""
    for path, (_, after) in changes.items():
        path.write_bytes(after.encode('utf-8'))
    changed = [p.parent.name for p in changes if p.name == 'CHANGELOG.md']
    if changed:
        _, outputs = notes.plan(root, changed, None)
        for path, (before, data) in outputs.items():
            if before != data:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
        if any(b != a for b, a in notes.plan(root, changed, None)[1].values()):
            raise ValueError('Release notes did not regenerate cleanly; run workshop-release-notes.py --check')
    module(root, 'workshop', 'scripts/prepare-workshop.py').catalogue(root)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mod', action='append', help='Repeatable; default every mod with an item ID')
    parser.add_argument('--visibility', action='store_true', help='Record the visibility Steam shows')
    parser.add_argument('--description', action='store_true', help='Replace page.bbcode with the live description')
    parser.add_argument('--changelog', action='store_true', help='Mark the uploaded version Released on public items')
    parser.add_argument('--write', action='store_true', help='Apply the changes; default only reports them')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    args = parser.parse_args(argv)
    areas = {a for a in ('visibility', 'description', 'changelog') if getattr(args, a)}
    try:
        rows, changes, notes = plan(ROOT, args.mod, areas)
        if args.write and changes:
            apply(ROOT, changes, notes)
        status = 'written' if args.write and changes else 'preview' if changes else 'unchanged'
        report = {'schemaVersion': 1, 'status': status, 'areas': sorted(areas), 'mods': rows,
                  'changedFiles': sorted(str(p.relative_to(ROOT)).replace('\\', '/') for p in changes)}
        code = 0
    except (ValueError, OSError, KeyError) as error:
        report, code = {'schemaVersion': 1, 'status': 'error', 'error': str(error)}, 1
    if args.format == 'json':
        print(json.dumps(report, indent=2, ensure_ascii=False))
        return code
    if code:
        print('ERROR: ' + report['error'])
        return code
    for row in report['mods']:
        state = f" {row['visibility']} on Steam, last updated {row['updated']}" if row.get('visibility') else ''
        print(f"{row['mod']}{state}")
        for line in row['changes'] + row['notes']:
            print('  ' + line)
    if not areas:
        print('Report only: add --visibility, --description and/or --changelog to choose what to pull.')
    elif status == 'preview':
        print('No files written. Add --write to apply; then review the pages, refresh their language-ledger rows and commit.')
    elif status == 'written':
        print('Written: ' + ', '.join(report['changedFiles']) + '. Review them, refresh their language-ledger rows and commit.')
    return code


if __name__ == '__main__':
    sys.exit(main())
