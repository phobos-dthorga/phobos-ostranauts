"""Draw Phobos Exchange's EXCHANGE PDA icon deterministically.

No image generation and no game asset: every icon shape is drawn here from fixed coordinates, so the script is
the icon's source. The icon follows the game's own PDA icons (a white disc with a black glyph, 256 pixels square,
tinted by the game); its master is drawn at 1024 pixels and reduced with Lanczos filtering. It also draws the held Workshop
cover's scene layer (244 x 170, recorded in assets/workshop/composed.json), a placeholder until the owner chooses
otherwise. Use --check to compare committed outputs without writing.
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
SCENE = 'assets/workshop/sources/PhobosExchange-scene.png'
SCENE_SIZE = (244, 170)
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


def scene():
    """The cover's scene layer, overhead like the game's rooms: a cabin nook where a spacer checks the exchange board on
    the bulkhead, a desk terminal and two crates. Promotional placeholder, no lettering; drawn from fixed shapes."""
    deck, seam, wall, strip = (44, 48, 52), (36, 40, 44), (28, 31, 34), (60, 66, 72)
    steel, screen, cream, edge = (96, 104, 110), (18, 26, 32), (196, 186, 160), (150, 140, 116)
    green, amber, blue, rust, skin, crate = (130, 170, 120), (210, 150, 70), (92, 128, 150), (150, 84, 48), (196, 150, 120), (120, 100, 70)
    image = Image.new('RGB', SCENE_SIZE, deck)
    draw = ImageDraw.Draw(image)
    for x in range(0, SCENE_SIZE[0], 20):
        draw.line([(x, 34), (x, SCENE_SIZE[1])], fill=seam)
    for y in range(54, SCENE_SIZE[1], 20):
        draw.line([(0, y), (SCENE_SIZE[0], y)], fill=seam)
    draw.rectangle((0, 0, SCENE_SIZE[0], 33), fill=wall)
    draw.rectangle((0, 32, SCENE_SIZE[0], 35), fill=strip)
    # The board on the bulkhead: three price lines, the market's in cream, a rising one in green, a falling one in amber.
    draw.rectangle((30, 4, 214, 62), fill=steel)
    draw.rectangle((34, 8, 210, 58), fill=screen)
    for x in range(42, 206, 24):
        draw.line([(x, 12), (x, 54)], fill=(30, 40, 46))
    lines = [
        (cream, [(38, 40), (60, 36), (82, 38), (104, 32), (126, 34), (148, 28), (170, 30), (192, 24), (206, 26)]),
        (green, [(38, 50), (60, 46), (82, 48), (104, 40), (126, 36), (148, 30), (170, 22), (192, 18), (206, 14)]),
        (amber, [(38, 20), (60, 24), (82, 22), (104, 30), (126, 34), (148, 40), (170, 38), (192, 46), (206, 48)]),
    ]
    for colour, points in lines:
        draw.line(points, fill=colour, width=2)
    for x, height in ((40, 3), (46, 5), (52, 2), (58, 6), (64, 4)):
        draw.rectangle((x, 56 - height, x + 3, 56), fill=blue)
    # The desk below the board, with a terminal and a mug.
    draw.rectangle((70, 94, 174, 118), fill=edge)
    draw.rectangle((72, 96, 172, 116), fill=cream)
    draw.rectangle((104, 98, 138, 110), fill=(30, 34, 38))
    draw.line([(107, 107), (113, 104), (119, 106), (127, 101), (135, 100)], fill=green, width=1)
    draw.ellipse((152, 100, 160, 108), fill=(80, 90, 96))
    draw.ellipse((154, 102, 158, 106), fill=(40, 30, 24))
    # The spacer, seen from above, facing the board.
    draw.ellipse((106, 122, 138, 146), fill=rust)
    draw.ellipse((113, 118, 131, 136), fill=(60, 44, 36))
    draw.ellipse((115, 121, 129, 134), fill=skin)
    # Two crates against the edges.
    for box in ((14, 120, 44, 150), (200, 126, 230, 156)):
        draw.rectangle(box, fill=crate)
        draw.rectangle((box[0] + 2, box[1] + 2, box[2] - 2, box[3] - 2), outline=(90, 74, 52))
        draw.line([(box[0] + 2, box[1] + 2), (box[2] - 2, box[3] - 2)], fill=(90, 74, 52))
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
        'tool': 'scripts/export-exchange-art.py',
        'generation': 'none: drawn from fixed shapes by the script, which is the source of record',
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
