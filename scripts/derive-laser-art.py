"""Derives the Ablatine ML-2 firing frames, their sheet and the beam texture; never calls a provider.

The eight frames are the selected overhead master with three small regions repainted in whole native pixels
(4 x 4 master pixels each): the lens port pulses from ember to white-hot and back, the collar behind it warms at
the peak, and two pixels on each radiator fin alternate. Nothing else moves, so the head never shifts between
frames. The sheet is the frames tiled four across and two down, top row first, which is the order the game's
frame animation reads. The beam is a drawn glow: a pale core with an orange falloff, fading out at both ends.

Use --check for byte-for-byte verification without writing files. Run scripts/export-completion-art.py afterwards
for the native exports.
"""
from pathlib import Path
import argparse
import io
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/artwork-completion/source'
MASTER = SOURCE / 'ml2-mining-laser.png'
FRAMES = SOURCE / 'ml2-mining-laser-frames'
SHEET = SOURCE / 'ml2-mining-laser-sheet.png'
BEAM = SOURCE / 'laser-beam.png'
SCALE, NATIVE, COLUMNS, ROWS = 4, 32, 4, 2

# Native-pixel regions, inclusive, on the 32 x 32 head. The exporter verifies that nothing outside them changes.
LENS = (14, 1, 17, 3)
LENS_CORE = (15, 2, 16, 2)
COLLAR = (13, 5, 18, 5)
FIN_ROW, FIN_COLUMNS = 24, ((5, 6, 7, 8), (23, 24, 25, 26))
ANIMATED = [list(LENS), list(COLLAR), [5, FIN_ROW, 8, FIN_ROW], [23, FIN_ROW, 26, FIN_ROW]]

LENS_COLOURS = [(40, 10, 12), (120, 24, 20), (210, 60, 30), (255, 150, 60), (255, 236, 200), (255, 150, 60), (210, 60, 30), (120, 24, 20)]
CORE_LIFT = 30
COLLAR_COLOURS = {3: (230, 80, 60), 4: (255, 120, 90), 5: (230, 80, 60)}
FIN_WARM = (150, 110, 100)


def png_bytes(image):
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    return stream.getvalue()


def block(image, x0, y0, x1, y1, colour):
    image.paste(tuple(colour) + (255,), (x0 * SCALE, y0 * SCALE, (x1 + 1) * SCALE, (y1 + 1) * SCALE))


def frame(master, index):
    image = master.copy()
    lens = LENS_COLOURS[index]
    block(image, *LENS, lens)
    block(image, *LENS_CORE, tuple(min(255, channel + CORE_LIFT) for channel in lens))
    if index in COLLAR_COLOURS:
        block(image, *COLLAR, COLLAR_COLOURS[index])
    for fin in FIN_COLUMNS:
        for x in fin[index % 2::2]:
            block(image, x, FIN_ROW, x, FIN_ROW, FIN_WARM)
    return image


def beam():
    # Native 32 x 8: brightness across the beam, fading over three pixels at each end; the colour carries the same
    # fade as the alpha, so the glow reads the same whether the game adds it or blends it.
    across = [0, .25, .7, 1, 1, .7, .25, 0]
    colours = [(0, 0, 0), (255, 96, 40), (255, 170, 70), (255, 244, 220), (255, 244, 220), (255, 170, 70), (255, 96, 40), (0, 0, 0)]
    image = Image.new('RGBA', (32 * SCALE, 8 * SCALE), (0, 0, 0, 0))
    for y in range(8):
        for x in range(32):
            level = across[y] * min(1, x / 3, (31 - x) / 3)
            pixel = tuple(round(channel * level) for channel in colours[y]) + (round(255 * level),)
            image.paste(pixel, (x * SCALE, y * SCALE, (x + 1) * SCALE, (y + 1) * SCALE))
    return image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    with Image.open(MASTER) as image:
        master = image.convert('RGBA')
    if master.size != (NATIVE * SCALE, NATIVE * SCALE) or master.getchannel('A').getextrema() != (255, 255):
        raise ValueError('The ML-2 master must be an opaque 128 x 128 image.')
    frames = [frame(master, index) for index in range(COLUMNS * ROWS)]
    sheet = Image.new('RGBA', (NATIVE * SCALE * COLUMNS, NATIVE * SCALE * ROWS))
    for index, image in enumerate(frames):
        sheet.paste(image, ((index % COLUMNS) * NATIVE * SCALE, (index // COLUMNS) * NATIVE * SCALE))
    outputs = {FRAMES / f'{index}.png': png_bytes(image) for index, image in enumerate(frames)}
    outputs[SHEET] = png_bytes(sheet)
    outputs[BEAM] = png_bytes(beam())
    stale = []
    for path, payload in outputs.items():
        if args.check:
            if not path.exists() or path.read_bytes() != payload:
                stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(payload)
    if stale:
        raise SystemExit('Missing/stale laser art: ' + ', '.join(stale))
    print(f'{"Verified" if args.check else "Derived"} {len(frames)} firing frames, the sheet and the beam texture.')


if __name__ == '__main__':
    main()
