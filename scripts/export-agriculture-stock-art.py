"""Export registered commodity sprites from untouched PixelLab masters.

Run with Python/Pillow. Mechanical nearest-neighbour exports only; no generation.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
LAYOUT = json.loads((ASSETS / 'stock-layers.json').read_text(encoding='utf-8'))
NEAREST = Image.Resampling.NEAREST


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    sheet = Image.new('RGB', (960, 700), '#252b2e')
    draw = ImageDraw.Draw(sheet)
    draw.text((16, 12), 'Verdemorrow supplies - native 16px and 8x inspection', fill='white')
    for index, entry in enumerate(LAYOUT['items']):
        path = ASSETS / 'source' / entry['source']
        master = Image.open(path).convert('RGBA')
        size = LAYOUT['nativeSize']
        assert master.size == tuple(LAYOUT['masterSize']), f'{path}: unexpected master dimensions'
        assert min(master.size) >= size * 4, f'{path}: insufficient production resolution'
        alpha = master.getchannel('A')
        bounds = alpha.getbbox()
        assert bounds and alpha.getextrema()[0] == 0, f'{path}: missing object/transparency'
        assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < master.width and bounds[3] < master.height, f'{path}: clipped silhouette'
        pixels = master.resize((size, size), NEAREST)
        assert pixels.getchannel('A').getbbox(), f'{path}: vanished at native size'
        name = 'Stock-' + entry['key']
        target = OUT / (name + '.png')
        pixels.save(target)
        # Neutral tangent normals, with exactly matching alpha; no invented relief.
        normals = Image.new('RGBA', pixels.size, (128, 128, 255, 255))
        normals.putalpha(pixels.getchannel('A'))
        normal_path = OUT / (name + 'Normal.png')
        normals.save(normal_path)
        records.append({'key': entry['key'], 'source': path.relative_to(ROOT).as_posix(),
                        'sourceSHA256': digest(path), 'canvas': list(master.size),
                        'export': list(pixels.size), 'sourceBounds': list(bounds),
                        'sampling': 'nearest; whole canvas; no independent crop',
                        'exportSHA256': digest(target), 'normalSHA256': digest(normal_path)})
        x, y = (index % 4) * 240, (index // 4) * 220 + 36
        # Both backgrounds expose silhouette/alpha problems. Small swatch is 1:1.
        draw.rectangle((x + 12, y + 4, x + 155, y + 148), fill='#36434a')
        draw.rectangle((x + 172, y + 60, x + 211, y + 99), fill='#b9b9a4')
        sheet.paste(pixels.resize((128, 128), NEAREST), (x + 20, y + 12), pixels.resize((128, 128), NEAREST))
        sheet.paste(pixels, (x + 184, y + 72), pixels)
        draw.text((x + 12, y + 160), entry['label'], fill='white')
    (ASSETS / 'stock-exports.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
    sheet.save(ASSETS / 'stock-preview.png')
    print(f'Exported {len(records)} stock sprites and matching flat normals; native footprint 16 x 16.')


if __name__ == '__main__':
    main()
