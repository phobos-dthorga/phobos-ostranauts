"""Worksheets for tidying each Workshop item's Change Notes tab by hand.

Steam offers no way to edit or delete an existing change note from a tool
(ISteamUGC and SteamCMD only add one per upload); the item's owner can edit each
entry's text in the Change Notes tab. This script rebuilds every item's entries
from the upload receipts in .local/workshop-receipts/, which keep the exact text
and time of each upload, and suggests replacement text where an entry is
untidy:

- an upload that sent a version the item already had (a re-send or a visibility
  change) becomes a one-line pointer to that version's entry;
- a heading reading Draft - not published, on an item now public, says how that
  version was really uploaded: Released for a public upload, Uploaded privately
  otherwise.

It writes one worksheet per item to .local/workshop-change-notes/<ModId>.md,
entries newest first as Steam lists them, and never contacts Steam.
See docs/development/workshop-upload-preparation.md.
"""
import argparse
from datetime import datetime
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CATALOGUE = 'config/workshop-publishing.json'
RECEIPTS = '.local/workshop-receipts'
WORKSHEETS = '.local/workshop-change-notes'
# Receipts whose upload reached Steam: SteamCMD exited 0, or the owner reconciled it as submitted.
REACHED = {'created-unverified', 'submitted-unverified', 'reconciled-submitted'}
DRAFT = re.compile(r'(?m)^\[b\]Draft - not published \| \d{4}-\d{2}-\d{2}\[/b\]$')


def change_note(vdf_text):
    """The changenote value of an upload VDF: one quoted string, literal newlines, never an inner quote.
    SteamCMD rewrites a create's VDF with tabs between key and value when it adds the new item ID."""
    match = re.search(r'(?m)^\s*"changenote"\s+"([^"]*)"', vdf_text)
    return match[1] if match else None


def entries(root, name):
    """Every upload of a mod that reached Steam, oldest first."""
    found = []
    folder = root / RECEIPTS / name
    for directory in sorted(folder.iterdir()) if folder.is_dir() else []:
        receipt_file, vdf_file = directory / 'receipt.json', directory / 'upload.vdf'
        if not receipt_file.is_file() or not vdf_file.is_file():
            continue
        receipt = json.loads(receipt_file.read_text(encoding='utf-8-sig'))
        if receipt.get('status') not in REACHED or (receipt.get('status') != 'reconciled-submitted' and receipt.get('steamCmdExitCode') not in (0, None)):
            continue
        note = change_note(vdf_file.read_text(encoding='utf-8-sig'))
        if note is None:
            continue
        found.append({'time': datetime.fromisoformat(receipt['startedAt']), 'version': receipt['version'],
                      'visibility': receipt.get('visibility', 'Private'), 'operation': receipt.get('operation'),
                      'receipt': directory.name, 'sent': note})
    return sorted(found, key=lambda e: e['time'])


def suggest(items, now_public):
    """Decide keep or replace for each entry (oldest first) and attach any replacement text."""
    seen = {}
    for entry in items:
        version, when = entry['version'], entry['time'].strftime('%d %B %Y').lstrip('0')
        if version in seen:
            reason = (f'to make the item {entry["visibility"].lower()}' if entry['visibility'] != seen[version]['visibility']
                      else 'again with no changes')
            entry['replacement'] = f'Version {version} sent {reason} on {when}. Its changes are in the {version} entry below.'
            entry['why'] = 'repeats a version the item already had'
        elif now_public and DRAFT.search(entry['sent']):
            label = 'Released' if entry['visibility'] == 'Public' else 'Uploaded privately'
            entry['replacement'] = DRAFT.sub(lambda _: f'[b]{label} | {entry["time"].date().isoformat()}[/b]', entry['sent'])
            entry['why'] = 'says Draft - not published on an item that is now public'
        seen.setdefault(version, entry)
    return items


def worksheet(name, item, items):
    lines = [f'# {name} change notes', '',
             f'Item: https://steamcommunity.com/sharedfiles/filedetails/changelog/{item["itemId"]}', '',
             'Steam lists change notes newest first, each headed by its update time. Sign in as the',
             'item owner, open the Change Notes tab and edit each entry marked Replace: paste the text',
             'under it in place of the entry text. Entries marked Keep need nothing.', '']
    for number, entry in enumerate(reversed(items), 1):
        stamp = entry['time'].strftime('%d %b %Y %H:%M').lstrip('0')
        action = 'Replace' if 'replacement' in entry else 'Keep'
        lines.append(f'## {number}. {stamp}: {entry["version"]}, {entry["visibility"]} {entry["operation"]} - {action}')
        lines.append('')
        if 'replacement' in entry:
            lines += [f'Why: it {entry["why"]}.', '', '```text', entry['replacement'].rstrip('\n'), '```', '']
    if not items:
        lines.append('No uploads recorded in this checkout.')
    return '\n'.join(lines).rstrip('\n') + '\n'


def run(root, mods, write):
    catalogue = json.loads((root / CATALOGUE).read_text(encoding='utf-8-sig'))['mods']
    selected = [m if m.startswith('Phobos') else 'Phobos' + m for m in (mods or [n for n, i in catalogue.items() if i['itemId']])]
    rows = []
    for name in selected:
        if name not in catalogue:
            raise ValueError(f'Unknown mod: {name}')
        item = catalogue[name]
        if not item['itemId']:
            continue
        items = suggest(entries(root, name), item.get('uploadedVisibility') == 'public')
        path = root / WORKSHEETS / f'{name}.md'
        if write:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(worksheet(name, item, items), encoding='utf-8', newline='\n')
        rows.append({'mod': name, 'entries': len(items), 'toReplace': sum('replacement' in e for e in items),
                     'worksheet': str(path.relative_to(root)).replace('\\', '/')})
    return rows


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mod', action='append', help='Repeatable; default every mod with an item ID')
    parser.add_argument('--no-write', action='store_true', help='Only count entries; write no worksheets')
    args = parser.parse_args(argv)
    try:
        rows = run(ROOT, args.mod, not args.no_write)
    except (ValueError, OSError, KeyError) as error:
        print('ERROR: ' + str(error))
        return 1
    for row in rows:
        todo = f'{row["toReplace"]} to replace' if row['toReplace'] else 'nothing to tidy'
        print(f'{row["mod"]}: {row["entries"]} change notes, {todo}' + ('' if args.no_write else f' ({row["worksheet"]})'))
    return 0


if __name__ == '__main__':
    sys.exit(main())
