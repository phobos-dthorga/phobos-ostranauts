"""Read-only, aggregate saved-item handling/container audit; never extracts or edits a save."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import zipfile


def audit(save, evidence):
    definitions = {i['id']: i for m in evidence['mods'] for i in m['items']}
    aliases = {a: i for i in definitions.values() for a in i['aliases']}
    counts, missing, inaccessible = Counter(), Counter(), []
    with zipfile.ZipFile(save) as archive:
        for name in archive.namelist():
            if not name.startswith('ships/') or not name.endswith('.json'):
                continue
            ships = json.loads(archive.read(name))
            for ship in ships:
                owners = {c['strID']: c for c in ship.get('aCOs', [])}
                items = ship.get('aItems', [])
                for co in owners.values():
                    definition = definitions.get(co.get('strCODef')) or aliases.get(co.get('strCODef'))
                    if definition is None:
                        continue
                    identity = definition['id']
                    counts[identity] += 1
                    values = co.get('aConds', [])
                    flags = set(definition['handlingFlags']) if 'DEFAULT' in values else set()
                    for term in values:
                        if '=' not in term:
                            continue
                        flag, amount = term.split('=', 1)
                        if float(amount.rsplit('x', 1)[-1]) > 0:
                            flags.add(flag)
                        else:
                            flags.discard(flag)
                    flags.difference_update(co.get('aCondZeroes', []))
                    for flag in ('IsInstalled', 'IsCumbersome', 'IsSystem'):
                        if flag in definition['handlingFlags'] and flag not in flags:
                            missing[identity + ':' + flag] += 1
                    container = definition['container']
                    children = [i['strID'] for i in items if i.get('strParentID') == co['strID']]
                    if children and container['trigger'] is not None and not container['inventoryAction'] and 'IsSystem' not in flags:
                        # Count individual saved stack members without counting a member twice.
                        seen, pending, cargo = set(), list(children), Counter()
                        while pending:
                            key = pending.pop()
                            if key in seen or key not in owners:
                                continue
                            seen.add(key)
                            child = owners[key]
                            cargo[child['strCODef']] += 1
                            pending.extend(child.get('aStack', []))
                        inaccessible.append(dict(definition=identity, directCargoObjects=len(children),
                                                 unitsIncludingStacks=sum(cargo.values()), cargo=dict(sorted(cargo.items()))))
    return dict(schemaVersion=1, saveSha256=hashlib.sha256(Path(save).read_bytes()).hexdigest(),
                savedObjects=sum(counts.values()), representedDefinitions=len(counts),
                counts=dict(sorted(counts.items())), missingHandlingFlags=dict(sorted(missing.items())),
                inaccessibleCargo=inaccessible,
                limits='Serialized ship records only; no live menu, tool, path, lock or Harmony execution. No save changes.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--save', type=Path, required=True)
    parser.add_argument('--snapshot', type=Path, default=Path(__file__).resolve().parents[1] / 'docs/item-reference-data.json')
    parser.add_argument('--output', type=Path, required=True, help='Use an ignored local output path for owner evidence.')
    args = parser.parse_args()
    if args.output.resolve() in (args.save.resolve(), args.snapshot.resolve()):
        parser.error('Output must not replace the save or definition snapshot.')
    result = audit(args.save, json.loads(args.snapshot.read_text(encoding='utf-8')))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: result[k] for k in ('savedObjects', 'representedDefinitions', 'missingHandlingFlags', 'inaccessibleCargo')}, indent=2))


if __name__ == '__main__':
    main()
