"""Retain a visually reviewed PixelLab machinery result and register its export.

No generation or account credentials. Call only after get_image confirms completion
and the operator has inspected the result. Request JSON retains original provenance.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/artwork-completion'
PREFIXES = {
    'rack': 'PhobosVerdemorrowFirstlight4', 'cooker': 'PhobosVerdemorrowHearth2',
    'water-supply': 'PhobosVerdemorrowGroundworkW2', 'workup': 'PhobosVerdemorrowGroundworkB2',
    'chute': 'PhobosHullChute', 'grabber': 'PhobosExteriorGrabber',
    'collector': 'PhobosResidueCollector', 'reclaimer': 'PhobosScrapReclaimer',
    'furnace': 'PhobosFurnace', 'radiator': 'PhobosFurnaceRadiator',
    'thermal-port': 'PhobosFurnaceThermalPort',
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('request_file', help='JSON filename inside artwork-completion')
    parser.add_argument('--select', nargs='+', required=True, help='Reviewed candidate keys only')
    args = parser.parse_args()
    request_path = (ASSETS / args.request_file).resolve()
    if request_path.parent != ASSETS.resolve():
        raise ValueError('Request must be an artwork-completion record')
    requests = json.loads(request_path.read_text())
    if isinstance(requests, dict):
        requests = [requests]
    plan = {r['key']: r for r in json.loads((ASSETS / 'machinery-plan.json').read_text())}
    manifest_path = ASSETS / 'manifest.json'
    manifest = json.loads(manifest_path.read_text())
    found = set()
    for request in requests:
        key = request.get('key', 'rack-damaged' if args.request_file == 'rack-damaged-request.json' else '')
        if key not in args.select:
            continue
        if request['response'].get('isError'):
            raise ValueError(f'Failed submission: {key}')
        family = request.get('family', 'rack')
        row = plan[family]
        text = request['response']['content'][0]['text']
        job = re.search(r'job_id: ([0-9a-f-]+)', text).group(1)
        state = key.removeprefix(family + '-')
        suffix, form = {'damaged': ('Damaged', 'InstalledDmg'), 'loose': ('Loose', 'Loose'),
                        'loose-damaged': ('LooseDamaged', 'LooseDmg')}[state]
        source = ASSETS / 'source' / (key + '.png')
        if not source.exists():
            urllib.request.urlretrieve(f'https://api.pixellab.ai/mcp/images/{job}/download', source)
        entry = {'key': key, 'mod': row['mod'], 'definition': PREFIXES[family] + form,
                 'nativeSize': row['nativeSize'], 'masterSize': row['masterSize'],
                 'pivot': [n // 2 for n in row['nativeSize']],
                 'runtime': 'phobos/' + ('agriculture/' if row['mod'] == 'PhobosAgriculture' else 'shipbreaker/') + row['runtimeBase'] + suffix,
                 'source': source.relative_to(ASSETS).as_posix(),
                 'sourceSHA256': hashlib.sha256(source.read_bytes()).hexdigest(),
                 'status': 'selected', 'registrationReference': request['request']['inputReference'] if state == 'loose-damaged' else row['reference'],
                 'requestRecord': args.request_file, 'jobId': job,
                 'review': 'Overhead projection inspected; native-scale export review required before delivery.'}
        manifest['assets'] = [a for a in manifest['assets'] if a['key'] != key] + [entry]
        entry['registrationSHA256'] = hashlib.sha256((ASSETS / entry['registrationReference']).read_bytes()).hexdigest()
        found.add(key)
    if found != set(args.select):
        raise ValueError(f'Unknown selections: {set(args.select) - found}')
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    print(f'Retained and registered {len(found)} reviewed machinery candidates.')


if __name__ == '__main__':
    main()
