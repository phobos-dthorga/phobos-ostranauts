"""Fennmark propellant line art: a recorded, deterministic recolour of the shared conduit artwork; never calls a provider.

The Agriculture irrigation pipe art (itself reused byte-for-byte by Shipbreaker's F6-C coolant line) is recoloured
per pixel: luminance maps onto a warm amber ramp, the yellow-ochre convention for gas lines, so propellant lines
read differently from water and coolant. Alpha and pixel placement are unchanged; normals are copied unchanged.
Use --check for byte-for-byte verification without writing files.
"""
from pathlib import Path
import argparse
import io
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
TARGET = ROOT / 'mods/PhobosManufacturing/images/phobos/manufacturing'
# channel = a * luminance + b (rounded, clamped): dark graphite shadows to a burnt-amber jacket.
RAMP = {'r': (1.25, 22), 'g': (0.86, 12), 'b': (0.34, 8)}
PAIRS = [('WaterPipe', 'PropellantPipe'), ('WaterPipeSheet', 'PropellantPipeSheet')]


def png_bytes(image):
    stream = io.BytesIO(); image.save(stream, format='PNG'); return stream.getvalue()


def recolour(image):
    out = []
    for p in image.convert('RGBA').get_flattened_data():
        if p[3] == 0:
            out.append(p); continue
        lum = 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]
        out.append(tuple(max(0, min(255, int(round(RAMP[c][0] * lum + RAMP[c][1])))) for c in 'rgb') + (p[3],))
    result = Image.new('RGBA', image.size); result.putdata(out)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    outputs = {}
    for source, target in PAIRS:
        with Image.open(SOURCE / (source + '.png')) as image:
            outputs[TARGET / (target + '.png')] = png_bytes(recolour(image))
        outputs[TARGET / (target + 'Normal.png')] = (SOURCE / (source + 'Normal.png')).read_bytes()
    stale = []
    for path, payload in outputs.items():
        if args.check:
            if not path.exists() or path.read_bytes() != payload: stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(payload)
    if stale:
        raise SystemExit('Missing/stale propellant line art: ' + ', '.join(stale))
    print(f'{"Verified" if args.check else "Exported"} {len(outputs)} propellant line images.')


if __name__ == '__main__':
    main()
