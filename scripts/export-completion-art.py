"""Mechanical exports of retained artwork-completion masters; never calls a provider.

Use --check for byte-for-byte verification without writing files.
"""
from pathlib import Path
import argparse
import hashlib
import io
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/artwork-completion'


def png_bytes(image):
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    return stream.getvalue()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    manifest = json.loads((ASSETS / 'manifest.json').read_text(encoding='utf-8'))
    outputs, records, previews, selected, sheets = {}, [], [], {}, []
    for entry in manifest['assets']:
        if entry['status'] != 'selected':
            continue
        source = ASSETS / entry['source']
        data = source.read_bytes()
        sha = hashlib.sha256(data).hexdigest()
        if sha != entry['sourceSHA256']:
            raise ValueError(f'Changed retained master: {source}')
        with Image.open(source) as image:
            master = image.convert('RGBA')
        native = tuple(entry['nativeSize'])
        factor = 4 if min(native) <= 32 else 2
        # A derived placeholder (the S2 tank, Framework 0.58.0) reuses another entry's retained master through a
        # recorded reduction of that entry's native export; it adds no detail and is replaced when a pilot is made.
        derived = entry.get('reduction')
        if master.size != tuple(entry['masterSize']) or (not derived and any(a < b * factor for a, b in zip(master.size, native))):
            raise ValueError(f'Insufficient/unexpected master dimensions: {source}')
        if entry.get('productionSource'):
            original = ASSETS / entry['productionSource']
            if hashlib.sha256(original.read_bytes()).hexdigest() != entry['productionSHA256']:
                raise ValueError(f'Changed retained production original: {original}')
            with Image.open(original) as image:
                if image.size != tuple(entry['productionSize']) or any(a < b * factor for a, b in zip(image.size, native)):
                    raise ValueError(f'Insufficient/unexpected production original dimensions: {original}')
        bounds = master.getchannel('A').getbbox()
        # A full-footprint fixture (a deck-mounted tank or cabinet, like the game's own square-deck machinery)
        # deliberately covers its whole tile square: it must be opaque edge to edge rather than padded.
        full = entry.get('fullFootprint', False)
        if full:
            if bounds != (0, 0, master.width, master.height) or master.getchannel('A').getextrema() != (255, 255):
                raise ValueError(f'Full-footprint master must be opaque across its whole canvas: {source}')
        elif not bounds or master.getchannel('A').getextrema()[0] != 0:
            raise ValueError(f'Missing object/transparency: {source}')
        if not full and not entry.get('registrationReference') and (min(bounds[:2]) <= 0 or bounds[2] >= master.width or bounds[3] >= master.height):
            raise ValueError(f'Clipped silhouette: {source}')
        if derived:
            # Area-average the parent's native sprite down to this size, then hold every pixel to the parent's own
            # palette (no blended colours), opaque edge to edge like its parent.
            parent = master.resize(tuple(derived['parentNativeSize']), Image.Resampling.NEAREST).convert('RGB')
            palette = parent.quantize(colors=derived['colours'], method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
            reduced = parent.resize(native, Image.Resampling.BOX).quantize(palette=palette, dither=Image.Dither.NONE)
            pixels = reduced.convert('RGBA')
        elif entry.get('sampleOffset'):
            # A recorded nearest-neighbour phase (the D20 drain canister, Framework 0.65.0): each native pixel takes the
            # master pixel at factor * i + offset instead of the block centre, chosen by inspecting every phase at native
            # size. It selects existing master pixels only; nothing is redrawn or blended.
            step = master.width // native[0], master.height // native[1]
            ox, oy = entry['sampleOffset']
            if not (0 <= ox < step[0] and 0 <= oy < step[1]):
                raise ValueError(f'Invalid sample offset: {source}')
            pixels = Image.new('RGBA', native)
            for y in range(native[1]):
                for x in range(native[0]):
                    pixels.putpixel((x, y), master.getpixel((step[0] * x + ox, step[1] * y + oy)))
        else:
            pixels = master.resize(native, Image.Resampling.NEAREST)
        if entry.get('registrationReference'):
            reference_path = ASSETS / entry['registrationReference']
            if hashlib.sha256(reference_path.read_bytes()).hexdigest() != entry['registrationSHA256']:
                raise ValueError(f'Changed registration reference: {reference_path}')
            # Extract authored state layers onto the unchanged registered chassis.
            # Do not let a generator move sockets, crop a mounting foot or replace
            # an approved silhouette. Coordinates are reviewed native pixels.
            with Image.open(reference_path) as image:
                reference = image.convert('RGBA').resize(native, Image.Resampling.NEAREST)
            composed = reference.copy()
            regions = entry.get('patches', [[0, 0, native[0], native[1]]])
            for region in regions:
                if len(region) != 4 or not (0 <= region[0] < region[2] <= native[0] and 0 <= region[1] < region[3] <= native[1]):
                    raise ValueError(f'Invalid registered patch: {entry["key"]}')
                composed.alpha_composite(pixels.crop(tuple(region)), (region[0], region[1]))
            composed.putalpha(reference.getchannel('A'))
            pixels = composed
        if not pixels.getchannel('A').getbbox():
            raise ValueError(f'Empty native export: {source}')
        normal = Image.new('RGBA', native, (128, 128, 255, 255))
        normal.putalpha(pixels.getchannel('A'))
        target = ROOT / 'mods' / entry['mod'] / 'images' / entry['runtime']
        hashes = {}
        for suffix, image in [('', pixels), ('Normal', normal)]:
            path = target.with_name(target.name + suffix + '.png')
            payload = png_bytes(image)
            outputs[path] = payload
            hashes[path.relative_to(ROOT).as_posix()] = hashlib.sha256(payload).hexdigest()
        records.append({'key': entry['key'], 'sourceSHA256': sha, 'nativeSize': native,
                        'pivot': entry['pivot'], 'sourceBounds': bounds, 'exports': hashes,
                        'normal': 'Neutral tangent normal with matching alpha; no authored relief',
                        'registrationReference': entry.get('registrationReference'),
                        'patches': entry.get('patches'), **({'fullFootprint': True} if full else {})})
        previews.append((entry['key'], pixels))
        selected[entry['key']] = pixels
        if entry.get('sheet'):
            sheets.append(entry)
    # A frame sheet for the game's own item animation (the ML-2 firing sheet, Shipbreaker 0.59.0). The game sizes
    # an animated item from one cell, so each cell must be exactly the item's native image; and so the fixture
    # never shifts between frames, every frame must equal the still image outside the declared animated regions.
    for entry in sheets:
        sheet, pixels = entry['sheet'], selected[entry['key']]
        cell_width, cell_height = sheet['cell']
        if pixels.size != (cell_width * sheet['columns'], cell_height * sheet['rows']) or not 0 < sheet['frames'] <= sheet['columns'] * sheet['rows']:
            raise ValueError(f'Frame sheet does not match its cell grid: {entry["key"]}')
        still = selected.get(sheet['still'])
        if still is None or still.size != (cell_width, cell_height):
            raise ValueError(f'Frame sheet has no matching still image: {entry["key"]}')
        regions = sheet.get('animated', [])
        for index in range(sheet['frames']):
            x0, y0 = (index % sheet['columns']) * cell_width, (index // sheet['columns']) * cell_height
            frame = pixels.crop((x0, y0, x0 + cell_width, y0 + cell_height))
            for y in range(cell_height):
                for x in range(cell_width):
                    if any(r[0] <= x <= r[2] and r[1] <= y <= r[3] for r in regions):
                        continue
                    if frame.getpixel((x, y)) != still.getpixel((x, y)):
                        raise ValueError(f'Frame {index} of {entry["key"]} moves the fixture at ({x}, {y})')
    # Keep saved crop-stage imagery when the installed chassis is damaged.
    if 'rack-damaged' in selected:
        layout = json.loads((ROOT / 'assets/phobos-agriculture/layers.json').read_text())
        folder = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
        for plant in layout['plants']:
            composed = selected['rack-damaged'].copy()
            with Image.open(folder / (plant + '.png')) as image:
                layer = image.convert('RGBA')
            for x, y in layout['rack']['plantCenters']:
                composed.alpha_composite(layer, (x - layer.width // 2, y - layer.height // 2))
            normal = Image.new('RGBA', composed.size, (128, 128, 255, 255))
            normal.putalpha(composed.getchannel('A'))
            name = 'Rack-' + plant + 'Damaged'
            outputs[folder / (name + '.png')] = png_bytes(composed)
            outputs[folder / (name + 'Normal.png')] = png_bytes(normal)
    # Preserve the established independent left/right coupling insert exactly.
    if 'thermal-port-damaged' in selected:
        folder = ROOT / 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
        for side, x in [('Left', 0), ('Right', 8)]:
            composed = selected['thermal-port-damaged'].copy()
            name = 'PhobosFurnaceThermalPortConnect' + side
            with Image.open(folder / (name + '.png')) as image:
                connection = image.convert('RGBA').crop((x, 4, x + 8, 12))
            composed.paste(connection, (x, 4))
            normal = Image.new('RGBA', composed.size, (128, 128, 255, 255))
            normal.putalpha(composed.getchannel('A'))
            outputs[folder / (name + 'Damaged.png')] = png_bytes(composed)
            outputs[folder / (name + 'DamagedNormal.png')] = png_bytes(normal)
    if previews:
        columns, cell_width = 4, 304
        sheet = Image.new('RGB', (columns * cell_width, ((len(previews) + columns - 1) // columns) * 240), '#252b2e')
        draw = ImageDraw.Draw(sheet)
        for i, (name, pixels) in enumerate(previews):
            x, y = (i % columns) * cell_width, (i // columns) * 240
            scale = max(1, min(176 // pixels.width, 176 // pixels.height))
            zoom = pixels.resize((pixels.width * scale, pixels.height * scale), Image.Resampling.NEAREST)
            sheet.paste(zoom, (x + 8, y + 8), zoom)
            sheet.paste(pixels, (x + 192, y + 8), pixels)
            draw.text((x + 8, y + 200), name, fill='white')
        outputs[ASSETS / 'preview.png'] = png_bytes(sheet)
        for start in range(0, len(previews), 16):
            end_row = min(sheet.height, ((start + 16) // columns) * 240)
            outputs[ASSETS / f'preview-{start // 16 + 1}.png'] = png_bytes(sheet.crop((0, start // columns * 240, sheet.width, end_row)))
    # Cover every composed crop/socket derivative as well as the direct exports.
    outputs[ASSETS / 'runtime-hashes.json'] = (json.dumps({
        path.relative_to(ROOT).as_posix(): hashlib.sha256(payload).hexdigest()
        for path, payload in outputs.items() if path.is_relative_to(ROOT / 'mods')
    }, indent=2) + '\n').encode()
    outputs[ASSETS / 'exports.json'] = (json.dumps(records, indent=2) + '\n').encode()
    stale = []
    for path, payload in outputs.items():
        if args.check:
            if not path.exists() or path.read_bytes() != payload:
                stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(payload)
    if stale:
        raise SystemExit('Missing/stale exports: ' + ', '.join(stale))
    print(f'{"Verified" if args.check else "Exported"} {len(records)} selected assets and matching normals.')


if __name__ == '__main__':
    main()
