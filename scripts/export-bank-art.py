"""Draw Phobos Banking's artwork deterministically: the CREDIT PDA icon and the Workshop cover's scene layer.

No image generation and no game asset: every shape is drawn here from fixed coordinates, so the script is the
master's source. The icon follows the game's own PDA icons (a white disc with a black glyph, 256 pixels square,
tinted by the game); its master is drawn at 1024 pixels and reduced with Lanczos filtering. The cover scene is a
244 x 170 pixel layer for scripts/compose-workshop-cover.py, drawn directly at that size in coarse pixel clusters.
Use --check to compare committed outputs without writing.
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
SCENE = 'assets/workshop/sources/PhobosBank-scene.png'
RECORD = 'assets/phobos-bank/exports.json'
MASTER_SIZE, ICON_SIZE, SCENE_SIZE = 1024, 256, (244, 170)
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


def scene():
    """A station finance kiosk on a worn deck: a terminal showing ledger rows, one of them late, and a credit chit."""
    w, h = SCENE_SIZE
    image = Image.new('RGB', SCENE_SIZE, (52, 58, 62))
    draw = ImageDraw.Draw(image)
    # Deck plates with darker seams and a few worn patches.
    for y in range(0, h, 20):
        for x in range(0, w, 20):
            shade = 62 + ((x * 7 + y * 13) // 20) % 3 * 4
            draw.rectangle((x, y, x + 19, y + 19), fill=(shade, shade + 5, shade + 8))
            draw.rectangle((x, y, x + 19, y), fill=(40, 45, 48))
            draw.rectangle((x, y, x, y + 19), fill=(40, 45, 48))
            draw.rectangle((x + 3, y + 3, x + 4, y + 4), fill=(84, 90, 94))
    # The bulkhead and the kiosk housing.
    draw.rectangle((0, 0, w - 1, 37), fill=(34, 38, 41))
    draw.rectangle((0, 36, w - 1, 39), fill=(96, 86, 62))
    draw.rectangle((46, 10, 198, 132), fill=(28, 31, 33))
    draw.rectangle((50, 14, 194, 128), fill=(78, 84, 86))
    draw.rectangle((50, 14, 194, 16), fill=(118, 124, 124))
    # The screen: dark glass, a heading bar and ledger rows (pale rows paid, an amber row late).
    draw.rectangle((60, 22, 184, 96), fill=(14, 30, 34))
    draw.rectangle((64, 26, 180, 31), fill=(82, 124, 150))
    rows = [(36, (196, 214, 206)), (44, (196, 214, 206)), (52, (212, 153, 72)), (60, (196, 214, 206)), (68, (120, 158, 118))]
    for y, colour in rows:
        draw.rectangle((66, y, 120, y + 3), fill=colour)
        draw.rectangle((150, y, 176, y + 3), fill=colour)
    draw.rectangle((66, 78, 176, 79), fill=(82, 124, 150))
    draw.rectangle((150, 84, 176, 88), fill=(236, 226, 204))
    # The credit chit in its reader beneath the screen.
    draw.rectangle((92, 104, 152, 122), fill=(36, 40, 42))
    draw.rectangle((98, 100, 146, 118), fill=(226, 218, 196))
    draw.rectangle((98, 104, 146, 106), fill=(42, 44, 46))
    draw.rectangle((102, 110, 110, 115), fill=(178, 146, 72))
    # A crate of goods waiting on payment and a stool, for scale.
    draw.rectangle((16, 118, 40, 150), fill=(98, 82, 58))
    draw.rectangle((16, 118, 40, 121), fill=(132, 112, 80))
    draw.rectangle((26, 126, 30, 142), fill=(70, 58, 42))
    draw.rectangle((206, 132, 226, 150), fill=(44, 48, 50))
    draw.rectangle((210, 150, 222, 160), fill=(36, 40, 42))
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
    files = {MASTER: png(master), ICON: png(icon), SCENE: png(scene())}
    record = {
        'schemaVersion': 1,
        'tool': 'scripts/export-bank-art.py',
        'generation': 'none: drawn from fixed shapes by the script, which is the source of record',
        'files': {path: {'sha256': sha256(data), 'bytes': len(data)} for path, data in files.items()},
        'notes': {
            ICON: 'CREDIT PDA app icon, 256 px like the game\'s own; reduced from the 1024 px master with Lanczos filtering.',
            SCENE: 'Placeholder scene layer for the held Workshop cover, composed by compose-workshop-cover.py.'
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
