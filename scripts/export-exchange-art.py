"""Draw Phobos Exchange's EXCHANGE PDA icon deterministically.

No game asset is used: the icon shapes are drawn from fixed coordinates, so this
script is their source. The icon follows the game's own PDA icons (a white disc
with a black glyph, 256 pixels square, tinted by the game); its master is drawn
at 1024 pixels and reduced with Lanczos filtering. The Workshop cover scene is
separate generated artwork retained at assets/workshop/sources/PhobosExchange-scene.png
and composed by scripts/compose-workshop-cover.py. Use --check to compare the
icon outputs without writing.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import sys

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MASTER = 'assets/phobos-exchange/Shares-1024.png'
ICON = 'mods/PhobosExchange/images/phobos/exchange/Shares.png'
RECORD = 'assets/phobos-exchange/exports.json'
MASTER_SIZE, ICON_SIZE = 1024, 256
WHITE, BLACK, CLEAR = (255, 255, 255, 255), (0, 0, 0, 255), (0, 0, 0, 0)
# A price line that dips, then climbs through two runs to the top right.
LINE = [(262, 640), (372, 560), (452, 612), (562, 452), (642, 500), (762, 312)]


def icon_master():
    """A chart: two axes and a rising price line with an arrowhead, on the game's white disc."""
    image = Image.new('RGBA', (MASTER_SIZE, MASTER_SIZE), CLEAR)
    draw = ImageDraw.Draw(image)
    draw.ellipse((16, 16, 1007, 1007), fill=WHITE)
    draw.rectangle((212, 262, 252, 762), fill=BLACK)   # the price axis
    draw.rectangle((212, 722, 812, 762), fill=BLACK)   # the time axis
    draw.line(LINE, fill=BLACK, width=44, joint='curve')
    for x, y in LINE[1:-1]:
        draw.ellipse((x - 22, y - 22, x + 22, y + 22), fill=BLACK)
    draw.polygon([(812, 262), (812, 392), (682, 262)], fill=BLACK)  # the arrowhead at the newest price
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
        'tool': 'scripts/export-exchange-art.py',
        'generation': 'the EXCHANGE PDA icon is drawn from fixed shapes by this script; the Workshop cover scene is separately generated and recorded in assets/workshop/composed.json',
        'files': {path: {'sha256': sha256(data), 'bytes': len(data)} for path, data in files.items()},
        'notes': {
            ICON: 'EXCHANGE PDA app icon, 256 px like the game\'s own; reduced from the 1024 px master with Lanczos filtering.'
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
