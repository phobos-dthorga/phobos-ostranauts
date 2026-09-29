"""Compose Workshop covers from a retained scene layer and an existing cover frame.

Deterministic and offline: the scene is an unchanged generated source, the frame is
a committed cover export, and the title is drawn from the pixel glyphs below. No
image generation happens here. Use --check to compare committed outputs.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import sys

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = 'assets/workshop/composed.json'

# Bold title glyphs, 10 rows; widths vary (M is wider, I is a bar).
TITLE = {
    'A': ['.XXXX.', 'XXXXXX', 'XX..XX', 'XX..XX', 'XX..XX', 'XXXXXX', 'XXXXXX', 'XX..XX', 'XX..XX', 'XX..XX'],
    'C': ['.XXXXX', 'XXXXXX', 'XX....', 'XX....', 'XX....', 'XX....', 'XX....', 'XX....', 'XXXXXX', '.XXXXX'],
    'D': ['XXXXX.', 'XXXXXX', 'XX..XX', 'XX..XX', 'XX..XX', 'XX..XX', 'XX..XX', 'XX..XX', 'XXXXXX', 'XXXXX.'],
    'E': ['XXXXXX', 'XXXXXX', 'XX....', 'XX....', 'XXXXX.', 'XXXXX.', 'XX....', 'XX....', 'XXXXXX', 'XXXXXX'],
    'F': ['XXXXXX', 'XXXXXX', 'XX....', 'XX....', 'XXXXX.', 'XXXXX.', 'XX....', 'XX....', 'XX....', 'XX....'],
    'G': ['.XXXXX', 'XXXXXX', 'XX....', 'XX....', 'XX.XXX', 'XX.XXX', 'XX..XX', 'XX..XX', 'XXXXXX', '.XXXXX'],
    'I': ['XX'] * 10,
    'L': ['XX....'] * 8 + ['XXXXXX', 'XXXXXX'],
    'M': ['XX....XX', 'XXX..XXX', 'XXXXXXXX', 'XX.XX.XX', 'XX.XX.XX', 'XX....XX', 'XX....XX', 'XX....XX', 'XX....XX', 'XX....XX'],
    'N': ['XX..XX', 'XXX.XX', 'XXX.XX', 'XXXXXX', 'XXXXXX', 'XX.XXX', 'XX.XXX', 'XX..XX', 'XX..XX', 'XX..XX'],
    'R': ['XXXXX.', 'XXXXXX', 'XX..XX', 'XX..XX', 'XXXXXX', 'XXXXX.', 'XX.XX.', 'XX..XX', 'XX..XX', 'XX..XX'],
    'T': ['XXXXXX', 'XXXXXX', '..XX..', '..XX..', '..XX..', '..XX..', '..XX..', '..XX..', '..XX..', '..XX..'],
    'U': ['XX..XX'] * 8 + ['XXXXXX', '.XXXX.'],
    'W': ['XX....XX'] * 3 + ['XX.XX.XX'] * 3 + ['XXXXXXXX', 'XXXXXXXX', 'XXX..XXX', 'XX....XX'],
    ' ': ['...'] * 10,
}
# Thin 5 x 7 subtitle glyphs, emboldened and stretched to 6 x 8 when drawn.
SUBTITLE = {
    'A': ['.XXX.', 'X...X', 'X...X', 'XXXXX', 'X...X', 'X...X', 'X...X'],
    'B': ['XXXX.', 'X...X', 'X...X', 'XXXX.', 'X...X', 'X...X', 'XXXX.'],
    'D': ['XXXX.', 'X...X', 'X...X', 'X...X', 'X...X', 'X...X', 'XXXX.'],
    'E': ['XXXXX', 'X....', 'X....', 'XXXX.', 'X....', 'X....', 'XXXXX'],
    'F': ['XXXXX', 'X....', 'X....', 'XXXX.', 'X....', 'X....', 'X....'],
    'I': ['XXX', '.X.', '.X.', '.X.', '.X.', '.X.', 'XXX'],
    'L': ['X....', 'X....', 'X....', 'X....', 'X....', 'X....', 'XXXXX'],
    'N': ['X...X', 'XX..X', 'X.X.X', 'X.X.X', 'X..XX', 'X...X', 'X...X'],
    'O': ['.XXX.', 'X...X', 'X...X', 'X...X', 'X...X', 'X...X', '.XXX.'],
    'P': ['XXXX.', 'X...X', 'X...X', 'XXXX.', 'X....', 'X....', 'X....'],
    'R': ['XXXX.', 'X...X', 'X...X', 'XXXX.', 'X.X..', 'X..X.', 'X...X'],
    'S': ['.XXXX', 'X....', 'X....', '.XXX.', '....X', '....X', 'XXXX.'],
    'T': ['XXXXX', '..X..', '..X..', '..X..', '..X..', '..X..', '..X..'],
    'U': ['X...X'] * 6 + ['.XXX.'],
    'W': ['X...X', 'X...X', 'X...X', 'X.X.X', 'X.X.X', 'XX.XX', 'X...X'],
    '/': ['....X', '...X.', '...X.', '..X..', '.X...', '.X...', 'X....'],
    ' ': ['.....'] * 7,
}


def sha256(data):
    return hashlib.sha256(data).hexdigest().upper()


def load(path, expected):
    data = (ROOT / path).read_bytes()
    if sha256(data) != expected.upper():
        raise ValueError(f'Source hash changed: {path}')
    return Image.open(io.BytesIO(data)).convert('RGB')


def draw_title(image, text, x, y, cell, body, highlight):
    for letter in text:
        rows = TITLE[letter]
        for r, row in enumerate(rows):
            for c, mark in enumerate(row):
                if mark == 'X':
                    for dy in range(cell):
                        colour = highlight if dy == 0 and (r == 0 or rows[r - 1][c] != 'X') else body
                        for dx in range(cell):
                            image.putpixel((x + c * cell + dx, y + r * cell + dy), colour)
        x += (len(rows[0]) + 1) * cell


def subtitle_mask(letter):
    rows = SUBTITLE[letter]
    # Embolden horizontally, then repeat the middle row: 5 x 7 becomes 6 x 8.
    bold = [''.join('X' if row[i:i + 1] == 'X' or row[i - 1:i] == 'X' else '.' for i in range(len(row) + 1)) for row in rows]
    return bold[:4] + bold[3:]


def draw_subtitle(image, text, centre, y, advance, colour):
    x = centre - (len(text) * advance - (advance - 6)) // 2
    for letter in text:
        for r, row in enumerate(subtitle_mask(letter)):
            for c, mark in enumerate(row):
                if mark == 'X':
                    image.putpixel((x + c + (6 - len(row)) // 2, y + r), colour)
        x += advance


def compose(cover):
    image = load(cover['frame']['path'], cover['frame']['sha256'])
    scene = load(cover['scene']['path'], cover['scene']['sha256'])
    left, top, right, bottom = cover['scene']['crop']
    scale = cover['scene']['scale']
    part = scene.crop((left, top, right, bottom))
    part = part.resize((part.width * scale, part.height * scale), Image.NEAREST)
    window = cover['scene']['window']
    image.paste(part.crop((0, 0, window[2] - window[0], window[3] - window[1])), (window[0], window[1]))
    background = tuple(cover['header']['background'])
    for box in cover['header']['clear']:
        image.paste(background, tuple(box))
    title = cover['header']['title']
    draw_title(image, title['text'], title['x'], title['y'], title['cell'], tuple(title['body']), tuple(title['highlight']))
    subtitle = cover['header']['subtitle']
    draw_subtitle(image, subtitle['text'], subtitle['centre'], subtitle['y'], subtitle['advance'], tuple(subtitle['colour']))
    return image


def png(image):
    buffer = io.BytesIO()
    image.save(buffer, format='PNG', optimize=True)
    return buffer.getvalue()


def run(check):
    manifest = json.loads((ROOT / MANIFEST).read_text(encoding='utf-8'))
    report = []
    for cover in manifest['covers']:
        full = compose(cover)
        outputs = {}
        for size in manifest['exportSizes']:
            data = png(full if size == full.width else full.resize((size, size), Image.NEAREST))
            if len(data) >= manifest['maxPreviewBytes']:
                raise ValueError(f"{cover['id']} {size}px export exceeds the byte budget")
            outputs[f"assets/workshop/previews/{cover['id']}-{size}.png"] = data
            if size == 512:
                outputs[f"mods/{cover['id']}/preview.png"] = data
        for path, data in outputs.items():
            target = ROOT / path
            if check:
                if not target.is_file() or target.read_bytes() != data:
                    raise ValueError(f'Cover differs from its recorded composition: {path}')
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
            report.append({'path': path, 'bytes': len(data), 'sha256': sha256(data)})
    return {'status': 'verified' if check else 'written', 'files': report}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify committed covers without writing')
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.check), indent=2))
        return 0
    except (ValueError, OSError, KeyError) as error:
        print(json.dumps({'status': 'error', 'error': str(error)}))
        return 1


if __name__ == '__main__':
    sys.exit(main())
