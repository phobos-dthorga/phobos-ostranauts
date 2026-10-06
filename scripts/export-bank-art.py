"""Draw Phobos Banking's CREDIT PDA icon deterministically.

No image generation and no game asset: every icon shape is drawn here from fixed coordinates, so the script is
the icon's source. The icon follows the game's own PDA icons (a white disc with a black glyph, 256 pixels square,
tinted by the game); its master is drawn at 1024 pixels and reduced with Lanczos filtering. The Workshop cover's
scene is generated artwork and is recorded in assets/workshop/composed.json.
Use --check to compare committed icon outputs without writing.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import sys

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MASTER = 'assets/phobos-bank/Credit-1024.png'
ICON = 'mods/PhobosBank/images/phobos/bank/Credit.png'
RECORD = 'assets/phobos-bank/exports.json'
MASTER_SIZE, ICON_SIZE = 1024, 256
WHITE, BLACK, CLEAR = (255, 255, 255, 255), (0, 0, 0, 255), (0, 0, 0, 0)


def icon_master():
    """A credit chit: a rounded card with a stripe, a contact chip and an account line, on the game's white disc."""
    image = Image.new('RGBA', (MASTER_SIZE, MASTER_SIZE), CLEAR)
    draw = ImageDraw.Draw(image)
    draw.ellipse((16, 16, 1007, 1007), fill=WHITE)
    draw.rounded_rectangle((212, 312, 812, 712), radius=56, fill=BLACK)
    draw.rectangle((212, 392, 812, 456), fill=WHITE)  # the stripe runs edge to edge
    draw.rounded_rectangle((282, 520, 402, 616), radius=14, fill=WHITE)
    for y in (544, 576):  # contact lines across the chip
        draw.rectangle((282, y, 402, y + 12), fill=BLACK)
    draw.rectangle((338, 520, 346, 616), fill=BLACK)
    for x in (456, 556, 656):  # the account line, in three groups
        draw.rectangle((x, 584, x + 76, 616), fill=WHITE)
    return image


def png(image):
    buffer = io.BytesIO()
    image.save(buffer, format='PNG', optimize=True)
    return buffer.getvalue()


def sha256(data):
    return hashlib.sha256(data).hexdigest().upper()


def outputs():
    master = icon_master()
    icon = master.resize((ICON_SIZE, ICON_SIZE), Image.LANCZOS)
    files = {MASTER: png(master), ICON: png(icon)}
    record = {
        'schemaVersion': 1,
        'tool': 'scripts/export-bank-art.py',
        'generation': 'none: drawn from fixed shapes by the script, which is the source of record',
        'files': {path: {'sha256': sha256(data), 'bytes': len(data)} for path, data in files.items()},
        'notes': {
            ICON: 'CREDIT PDA app icon, 256 px like the game\'s own; reduced from the 1024 px master with Lanczos filtering.'
        }
    }
    files[RECORD] = (json.dumps(record, indent=2) + '\n').encode('utf-8')
    return files


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify committed outputs without writing')
    args = parser.parse_args()
    report = []
    for path, data in outputs().items():
        target = ROOT / path
        if args.check:
            if not target.is_file() or target.read_bytes() != data:
                print(json.dumps({'status': 'error', 'error': f'Differs from its drawing: {path}'}))
                return 1
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        report.append({'path': path, 'bytes': len(data), 'sha256': sha256(data)})
    print(json.dumps({'status': 'verified' if args.check else 'written', 'files': report}, indent=2))
    return 0


if __name__ == '__main__':
    sys.exit(main())
