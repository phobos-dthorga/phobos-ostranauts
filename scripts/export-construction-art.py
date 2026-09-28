"""Export four registered construction layers without calling an image provider.

Outputs include registered runtime copies. --check writes nothing.
"""
import argparse
import hashlib
import io
import json
import sys
from pathlib import Path

# Build-time hash verification does not need an image-editing dependency.
if '--verify-runtime' not in sys.argv:
    from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/construction-stages'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def png(image):
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    return stream.getvalue()


def read_image(path, expected_hash):
    data = (ASSETS / path).read_bytes()
    if digest(data) != expected_hash:
        raise ValueError('Retained input changed: ' + path)
    return Image.open(io.BytesIO(data)).convert('RGBA')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--verify-runtime', action='store_true', help='Verify packaged copies using retained export hashes; no Pillow required')
    args = parser.parse_args()
    manifest = json.loads((ASSETS / 'manifest.json').read_text(encoding='utf-8'))
    references = json.loads((ASSETS / 'references.json').read_text(encoding='utf-8'))
    if args.verify_runtime:
        records = {r['key']: r for r in json.loads((ASSETS / 'exports.json').read_text(encoding='utf-8'))}
        for entry in manifest['assets']:
            if digest((ASSETS / entry['source']).read_bytes()) != entry['sourceSHA256']:
                raise SystemExit('Retained master changed: ' + entry['key'])
            for suffix in ['', 'Normal']:
                path = 'exports/' + entry['key'] + suffix + '.png'
                expected = records[entry['key']]['exports'][path]
                for target in [ASSETS / path, ROOT / (entry['runtime'] + suffix + '.png')]:
                    if digest(target.read_bytes()) != expected:
                        raise SystemExit('Construction runtime copy changed: ' + str(target))
        print('Verified eight construction runtime images against retained exports.')
        return
    for entry in references:
        read_image(entry['copy'], entry['sha256'])
    outputs, records, selected = {}, [], {}
    for entry in manifest['assets']:
        size = tuple(entry['nativeSize'])
        master = read_image(entry['source'], entry['sourceSHA256'])
        reference = read_image(entry['reference'], entry['referenceSHA256'])
        normal = read_image(entry['normalReference'], entry['normalReferenceSHA256'])
        if master.size != tuple(entry['masterSize']) or any(a < b * 2 for a, b in zip(master.size, size)):
            raise ValueError('Master below required resolution: ' + entry['key'])
        if reference.size != size or normal.size != size:
            raise ValueError('Registration size mismatch: ' + entry['key'])
        layer = master.resize(size, Image.Resampling.NEAREST)
        mask = Image.new('L', size)
        draw = ImageDraw.Draw(mask)
        for patch in entry['patches']:
            x0, y0, x1, y1 = patch['box']
            if not (0 <= x0 < x1 <= size[0] and 0 <= y0 < y1 <= size[1]):
                raise ValueError('Patch outside canvas: ' + entry['key'])
            # Manifest rectangles are half-open, like image crops.
            box = (x0, y0, x1 - 1, y1 - 1)
            if patch['shape'] == 'ellipse':
                draw.ellipse(box, fill=255)
            elif patch['shape'] == 'rectangle':
                draw.rectangle(box, fill=255)
            else:
                raise ValueError('Unknown patch shape')
        # Never extend the original silhouette or move its sockets/feet.
        mask = ImageChops.multiply(mask, reference.getchannel('A'))
        colour = Image.composite(layer, reference, mask)
        original_normal = normal.copy()
        normal = Image.composite(Image.new('RGBA', size, (128, 128, 255, 255)), normal, mask)
        normal.putalpha(colour.getchannel('A'))
        outside = ImageChops.invert(mask)
        for before, after in [(reference, colour), (original_normal, normal)]:
            difference = ImageChops.difference(before, after)
            if any(ImageChops.multiply(channel, outside).getbbox() for channel in difference.split()[:3]):
                raise ValueError('Frame changed outside approved patches')
        if colour.getchannel('A').getbbox() != reference.getchannel('A').getbbox():
            raise ValueError('Outer registration changed')
        record = {'key': entry['key'], 'exports': {}}
        for suffix, im in [('', colour), ('Normal', normal)]:
            path = ASSETS / 'exports' / (entry['key'] + suffix + '.png')
            payload = png(im)
            outputs[path] = payload
            outputs[ROOT / (entry['runtime'] + suffix + '.png')] = payload
            record['exports'][path.relative_to(ASSETS).as_posix()] = digest(payload)
        records.append(record)
        selected[entry['key']] = colour

    # Three-stage comparisons at native size and integer 3x zoom.
    rows = [
        ('D4 dismantling fixture', 'references/PhobosShipbreakerSection.png', 'd4-mid', 'references/PhobosShipbreakerInstalled.png'),
        ('R4 scrap reclaimer', 'references/PhobosReclaimerSectionDedicated.png', 'r4-mid', 'references/PhobosScrapReclaimer.png'),
        ('F6 electric furnace', 'f6-early', 'f6-mid', 'references/PhobosFurnaceSockets.png'),
    ]
    sheet = Image.new('RGB', (1050, 1440), '#252b2e')
    draw = ImageDraw.Draw(sheet)
    for row, (title, *keys) in enumerate(rows):
        top = row * 480
        draw.text((12, top + 8), title, fill='white')
        for column, (key, label) in enumerate(zip(keys, ['Early', 'Intermediate', 'Finished (existing)'])):
            im = selected[key] if key in selected else Image.open(ASSETS / key).convert('RGBA')
            left = column * 350 + 12
            draw.text((left, top + 30), label + (' (existing)' if column == 0 and row < 2 else ''), fill='#dbba69')
            zoom = im.resize((im.width * 3, im.height * 3), Image.Resampling.NEAREST)
            sheet.paste(zoom, (left, top + 52), zoom)
            sheet.paste(im, (left + 238, top + 355), im)
            draw.text((left, top + 462), f'3x above / {im.width} x {im.height} native at right', fill='white')
    outputs[ASSETS / 'previews/stages.png'] = png(sheet)
    sheet = Image.new('RGB', (1000, 360), '#252b2e')
    draw = ImageDraw.Draw(sheet)
    for column, (key, im) in enumerate(selected.items()):
        left = column * 250 + 16
        draw.text((left, 12), key + ' / 2x', fill='white')
        zoom = im.resize((im.width * 2, im.height * 2), Image.Resampling.NEAREST)
        sheet.paste(zoom, (left, 36), zoom)
        draw.text((left, 244), 'Native size', fill='#dbba69')
        sheet.paste(im, (left + 114, 244), im)
    outputs[ASSETS / 'previews/new-assets.png'] = png(sheet)
    outputs[ASSETS / 'exports.json'] = (json.dumps(records, indent=2) + '\n').encode('utf-8')
    stale = []
    for path, payload in outputs.items():
        if args.check:
            if not path.exists() or path.read_bytes() != payload:
                stale.append(path.relative_to(ROOT).as_posix())
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(payload)
    if stale:
        raise SystemExit('Missing/stale exports: ' + ', '.join(stale))
    print(f'{"Verified" if args.check else "Exported"} four construction colours, matching normals, runtime copies and stage previews.')


if __name__ == '__main__':
    main()
