#!/usr/bin/env python3
"""Check the shipped data packs against the JSON Schema files in schemas/.

A small validator for the subset of draft-07 the schemas use (type, const, enum,
pattern, minimum/maximum/exclusiveMinimum, properties/additionalProperties,
patternProperties, required, items, minItems); no third-party package is needed.
The Framework loader remains authoritative; this catches a schema that drifted
from the validators or a pack an editor would flag.

Usage: check-json-schemas.py [--format json] [paths...]
"""
import argparse
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]


def problems(schema, value, where='', out=None):
    out = out if out is not None else []
    kind = schema.get('type')
    if kind and not _is(kind, value):
        out.append(f'{where or "/"}: expected {kind}'); return out
    if 'const' in schema and value != schema['const']: out.append(f'{where}: expected {schema["const"]!r}')
    if 'enum' in schema and value not in schema['enum']: out.append(f'{where}: not one of {schema["enum"]}')
    if isinstance(value, str) and 'pattern' in schema and not re.search(schema['pattern'], value): out.append(f'{where}: does not match {schema["pattern"]}')
    if isinstance(value, (int, float)) and not isinstance(value, bool):
        if 'minimum' in schema and value < schema['minimum']: out.append(f'{where}: below {schema["minimum"]}')
        if 'exclusiveMinimum' in schema and value <= schema['exclusiveMinimum']: out.append(f'{where}: must be above {schema["exclusiveMinimum"]}')
        if 'maximum' in schema and value > schema['maximum']: out.append(f'{where}: above {schema["maximum"]}')
    if isinstance(value, dict):
        for key in schema.get('required', []):
            if key not in value: out.append(f'{where}: missing {key}')
        props = schema.get('properties', {}); patterns = schema.get('patternProperties', {})
        for key, item in value.items():
            here = f'{where}/{key}'
            if key in props: problems(props[key], item, here, out); continue
            matched = [s for p, s in patterns.items() if re.search(p, key)]
            if matched:
                for s in matched: problems(s, item, here, out)
                continue
            if schema.get('additionalProperties') is False: out.append(f'{here}: unknown field')
    if isinstance(value, list):
        if 'minItems' in schema and len(value) < schema['minItems']: out.append(f'{where}: needs at least {schema["minItems"]} item(s)')
        if 'items' in schema:
            for i, item in enumerate(value): problems(schema['items'], item, f'{where}/{i}', out)
    return out


def _is(kind, value):
    return {'object': isinstance(value, dict), 'array': isinstance(value, list), 'string': isinstance(value, str),
            'boolean': isinstance(value, bool), 'integer': isinstance(value, int) and not isinstance(value, bool),
            'number': isinstance(value, (int, float)) and not isinstance(value, bool)}[kind]


def schema_for(pack):
    name = pack.get('schema')
    path = ROOT / 'schemas' / f'{name}.schema.json'
    return json.loads(path.read_text(encoding='utf-8')) if path.exists() else None


def check_file(path):
    pack = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    schema = schema_for(pack)
    if schema is None: return None
    return problems(schema, pack)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('paths', nargs='*')
    parser.add_argument('--format', choices=['text', 'json'], default='text')
    args = parser.parse_args()
    paths = [Path(p) for p in args.paths] or sorted((ROOT / 'mods').glob('*/framework/*.json'))
    report = []
    for path in paths:
        found = check_file(path)
        if found is None: continue
        report.append({'path': str(path.relative_to(ROOT)) if path.is_absolute() and ROOT in path.parents else str(path), 'problems': found})
    errors = sum(len(r['problems']) for r in report)
    if args.format == 'json':
        print(json.dumps({'schemaVersion': 1, 'status': 'valid' if errors == 0 else 'failed', 'packs': report}, indent=2))
    else:
        for r in report:
            print(('ok  ' if not r['problems'] else 'FAIL') + ' ' + r['path'])
            for p in r['problems']: print('     ' + p)
        print(f'{len(report)} pack(s) checked against schemas/, {errors} problem(s).')
    return 0 if errors == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
