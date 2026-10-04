"""Line lane art: every pipe family drawn in its own lane of the 16 x 16 tile, so different families can share a tile
(owner decision, 30 September 2026; Framework 0.56.0 layers). Deterministic and procedural: no provider is called.

Each family is a 3-pixel pipe in lane n (rows or columns 1+3n to 3+3n; process water 0, gas 1, acid 2, irrigation 3,
coolant 4; belts lie under all of them), lit from the upper left, with a 3 x 3 fitting where arms meet and a darker
collar every four pixels. Colours come from the original irrigation conduit's own palette, recoloured per family by
a recorded luminance ramp (the propellant line's amber ramp is kept). Sheets follow the native joint mask order
(N=8 W=4 E=2 S=1, bottom-left first), with flat normals. The loose icon is the full cross in the middle lane.
Use --check for byte-for-byte verification without writing files; --preview writes a native-scale test scene.
"""
from pathlib import Path
import argparse
import hashlib
import io
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
NEAREST = Image.Resampling.NEAREST
# The original conduit palette (irrigation WaterPipe, 2026-09-25 master): highlight, body, shadow, fitting and jewel.
BASE = {'light': (127, 138, 154), 'mid': (84, 93, 103), 'dark': (27, 38, 48), 'collar': (39, 53, 67), 'fitting': (79, 89, 100), 'jewel': (71, 235, 242)}
# channel = a * luminance + b (rounded, clamped); None keeps the original colours.
FAMILIES = [
    {'name': 'process-water', 'lane': 0, 'ramp': {'r': (0.35, 10), 'g': (0.70, 20), 'b': (1.25, 30)},
     'targets': ['mods/PhobosFramework/images/phobos/framework/ProcessWaterPipe']},
    {'name': 'irrigation', 'lane': 3, 'ramp': None,
     'targets': ['mods/PhobosAgriculture/images/phobos/agriculture/WaterPipe']},
    {'name': 'gas', 'lane': 1, 'ramp': {'r': (1.25, 22), 'g': (0.86, 12), 'b': (0.34, 8)},
     'targets': ['mods/PhobosFramework/images/phobos/framework/PropellantPipe']},
    {'name': 'coolant', 'lane': 4, 'ramp': {'r': (0.45, 8), 'g': (1.05, 22), 'b': (0.62, 12)},
     'targets': ['mods/PhobosShipbreaker/images/phobos/shipbreaker/FurnaceCoolantPipe']},
    # Violet, the pipeline identification colour for acids and alkalis (BS 1710), for the Lixivar acid line.
    {'name': 'acid', 'lane': 2, 'ramp': {'r': (1.0, 22), 'g': (0.55, 8), 'b': (1.3, 30)},
     'targets': ['mods/PhobosManufacturing/images/phobos/manufacturing/AcidPipe']},
    # Brown, the pipeline identification colour for oils and combustible liquids (BS 1710), for the Alembrine ethanol line
    # (Manufacturing 0.38.0). The tile has room for five lanes, all taken, so it shares the coolant conduit's lane.
    {'name': 'ethanol', 'lane': 4, 'ramp': {'r': (1.1, 30), 'g': (0.7, 10), 'b': (0.35, 4)},
     'targets': ['mods/PhobosManufacturing/images/phobos/manufacturing/EthanolPipe']},
]
# The Rivetline conveyor belt (Framework 0.61.0) lies under every pipe lane: a 12-pixel band of dark belting with raised
# cross-cleats every third pixel between steel side rails, joined by the same native joint mask as the pipes.
BELT = {'rail': (118, 124, 132), 'rail_dark': (70, 75, 82), 'belt': (44, 46, 50), 'cleat': (76, 79, 85)}
BELT_TARGETS = ['mods/PhobosFramework/images/phobos/framework/ConveyorBelt']
# Native Item.SetSpriteSheetIndex bitmask to sheet index; UV rows count from the bottom.
INDICES = {3: 12, 7: 13, 5: 14, 8: 15, 11: 8, 15: 9, 13: 10, 2: 11, 10: 4, 14: 5, 12: 6, 4: 7, 6: 0, 0: 1, 9: 2, 1: 3}
N, W, E, S = 8, 4, 2, 1


def colour(name, ramp):
    rgb = BASE[name]
    if ramp is None: return rgb + (255,)
    lum = 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2]
    return tuple(max(0, min(255, int(round(ramp[c][0] * lum + ramp[c][1])))) for c in 'rgb') + (255,)


def tile(mask, lane, ramp):
    img = Image.new('RGBA', (16, 16))
    px = img.load()
    a = 1 + 3 * lane  # first row/column of the lane
    c = {k: colour(k, ramp) for k in BASE}
    def horizontal(x0, x1):
        for x in range(x0, x1):
            band = x % 4 == 3
            px[x, a] = c['collar'] if band else c['light']
            px[x, a + 1] = c['collar'] if band else c['mid']
            px[x, a + 2] = c['dark']
    def vertical(y0, y1):
        for y in range(y0, y1):
            band = y % 4 == 3
            px[a, y] = c['collar'] if band else c['light']
            px[a + 1, y] = c['collar'] if band else c['mid']
            px[a + 2, y] = c['dark']
    if mask & W: horizontal(0, a)
    if mask & E: horizontal(a + 3, 16)
    if mask & N: vertical(0, a)
    if mask & S: vertical(a + 3, 16)
    for dx in range(3):
        for dy in range(3):
            edge = dx == 2 or dy == 2
            px[a + dx, a + dy] = c['dark'] if edge else c['fitting']
    px[a + 1, a + 1] = c['jewel']
    return img


def belt_tile(mask):
    img = Image.new('RGBA', (16, 16))
    px = img.load()
    lo, hi = 2, 13  # the band's outer rows or columns
    c = {k: v + (255,) for k, v in BELT.items()}
    horizontal_run = bool(mask & (W | E)) or not mask & (N | S)
    def band(horizontal, start, end):
        for t in range(start, end):
            for s in range(lo, hi + 1):
                colour = c['rail'] if s == lo else c['rail_dark'] if s == hi else c['cleat'] if t % 3 == 0 else c['belt']
                px[(t, s) if horizontal else (s, t)] = colour
    if mask & W: band(True, 0, lo)
    if mask & E: band(True, hi + 1, 16)
    if mask & N: band(False, 0, lo)
    if mask & S: band(False, hi + 1, 16)
    # The centre carries the belt on in its main direction, with rails on every side that has no arm.
    for x in range(lo, hi + 1):
        for y in range(lo, hi + 1):
            t = x if horizontal_run else y
            px[x, y] = c['cleat'] if t % 3 == 0 else c['belt']
    for s in range(lo, hi + 1):
        if not mask & N: px[s, lo] = c['rail']
        if not mask & S: px[s, hi] = c['rail_dark']
        if not mask & W: px[lo, s] = c['rail']
        if not mask & E: px[hi, s] = c['rail_dark']
    return img


def belt_sheet():
    out = Image.new('RGBA', (64, 64))
    for mask, index in INDICES.items():
        out.paste(belt_tile(mask), ((index % 4) * 16, (3 - index // 4) * 16))
    return out


def sheet(lane, ramp):
    out = Image.new('RGBA', (64, 64))
    for mask, index in INDICES.items():
        out.paste(tile(mask, lane, ramp), ((index % 4) * 16, (3 - index // 4) * 16))
    return out


def normal(image):
    n = Image.new('RGBA', image.size, (128, 128, 255, 255)); n.putalpha(image.getchannel('A')); return n


def png(image):
    stream = io.BytesIO(); image.save(stream, format='PNG'); return stream.getvalue()


def outputs():
    result = {}
    for family in FAMILIES:
        icon = tile(15, 2, family['ramp']); full = sheet(family['lane'], family['ramp'])
        for target in family['targets']:
            base = ROOT / target
            result[base.with_name(base.name + '.png')] = png(icon)
            result[base.with_name(base.name + 'Normal.png')] = png(normal(icon))
            result[base.with_name(base.name + 'Sheet.png')] = png(full)
            result[base.with_name(base.name + 'SheetNormal.png')] = png(normal(full))
    icon, full = belt_tile(W | E), belt_sheet()
    for target in BELT_TARGETS:
        base = ROOT / target
        result[base.with_name(base.name + '.png')] = png(icon)
        result[base.with_name(base.name + 'Normal.png')] = png(normal(icon))
        result[base.with_name(base.name + 'Sheet.png')] = png(full)
        result[base.with_name(base.name + 'SheetNormal.png')] = png(normal(full))
    return result


def preview(path):
    """A 6 x 4 test scene at native scale x4: each family a straight run, crossings, a shared corridor."""
    scene = Image.new('RGBA', (16 * 6, 16 * 4), (58, 64, 70, 255))
    # A belt run along the top row and down the last column, under the pipes that cross it.
    for x in range(6): scene.alpha_composite(belt_tile(W | E if x < 5 else W | S), (16 * x, 0))
    for y in range(1, 4): scene.alpha_composite(belt_tile(N | S if y < 3 else N), (16 * 5, 16 * y))
    for family in FAMILIES:
        l, r = family['lane'], family['ramp']
        for x in range(6): scene.alpha_composite(tile(W | E if 0 < x < 5 else (E if x == 0 else W), l, r), (16 * x, 16))
        for y in range(4): scene.alpha_composite(tile(N | S if 0 < y < 3 else (S if y == 0 else N), l, r), (16 * (1 + FAMILIES.index(family)), 16 * y))
    big = scene.resize((scene.width * 4, scene.height * 4), NEAREST)
    canvas = Image.new('RGBA', (big.width + 20, big.height + 40), (37, 43, 46, 255)); canvas.alpha_composite(big, (10, 30))
    ImageDraw.Draw(canvas).text((10, 8), 'Line lanes at native scale x4: ' + ', '.join(f['name'] + ' lane ' + str(f['lane']) for f in FAMILIES), fill='white')
    canvas.save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--preview')
    args = parser.parse_args()
    files = outputs()
    if args.preview: preview(args.preview)
    stale = []
    for path, payload in files.items():
        if args.check:
            if not path.exists() or path.read_bytes() != payload: stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(payload)
    if stale: raise SystemExit('Missing/stale line art: ' + ', '.join(stale))
    if not args.check:
        record = {'families': [{k: f[k] for k in ('name', 'lane', 'ramp', 'targets')} for f in FAMILIES], 'base': BASE,
                  'belt': {'colours': BELT, 'targets': BELT_TARGETS},
                  'exports': {str(p.relative_to(ROOT).as_posix()): hashlib.sha256(b).hexdigest() for p, b in sorted(files.items())}}
        (ROOT / 'assets/line-art/line-art-exports.json').parent.mkdir(parents=True, exist_ok=True)
        (ROOT / 'assets/line-art/line-art-exports.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(f'{"Verified" if args.check else "Exported"} {len(files)} line images.')


if __name__ == '__main__':
    main()
