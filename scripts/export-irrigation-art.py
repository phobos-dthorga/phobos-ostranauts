"""Mechanical registration/export of original irrigation masters; no generation.
Run with Python/Pillow. Pipe variants reuse one generated fitting, never game textures.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
LAYOUT = json.loads((ASSETS / 'irrigation-layers.json').read_text())
NEAREST = Image.Resampling.NEAREST
records = []

def master(filename, minimum):
    path = ASSETS / 'source' / filename
    image = Image.open(path).convert('RGBA')
    assert min(image.size) >= minimum
    assert image.getextrema()[3][0] == 0, 'Transparency required'
    records.append({'source': path.relative_to(ROOT).as_posix(), 'size': image.size,
                    'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    return image

def export(name, pixels):
    path = OUT / (name + '.png')
    pixels.save(path)
    normal = Image.new('RGBA', pixels.size, (128, 128, 255, 255))
    normal.putalpha(pixels.getchannel('A'))
    normal.save(OUT / (name + 'Normal.png'))
    records.append({'export': path.relative_to(ROOT).as_posix(), 'size': pixels.size,
                    'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})

supply = master(LAYOUT['supply']['source'], 128)
native = Image.new('RGBA', (32, 32))
# Retain full silhouette; registration puts the right outlet at native y=8.
crop = supply.crop(tuple(LAYOUT['supply']['crop']))
native.alpha_composite(crop.resize(tuple(LAYOUT['supply']['composeSize']), NEAREST), tuple(LAYOUT['supply']['origin']))
export('WaterSupply', native)
cross = master(LAYOUT['conduit']['source'], 64)
cross = cross.crop(tuple(LAYOUT['conduit']['crop'])).resize((16, 16), NEAREST)
# Native Item.SetSpriteSheetIndex bitmask: N=8 W=4 E=2 S=1.
# Native UV indexing starts at bottom-left; PNG indexing starts at top-left.
indices = {3:12, 7:13, 5:14, 8:15, 11:8, 15:9, 13:10, 2:11,
           10:4, 14:5, 12:6, 4:7, 6:0, 0:1, 9:2, 1:3}
sheet = Image.new('RGBA', (64, 64))
centre = (5, 5, 11, 11)
arms = {8: (0, 0, 16, 5), 4: (0, 5, 5, 11), 2: (11, 5, 16, 11), 1: (0, 11, 16, 16)}
for mask, index in indices.items():
    tile = Image.new('RGBA', (16, 16))
    tile.paste(cross.crop(centre), centre[:2])
    for bit, box in arms.items():
        if mask & bit:
            tile.paste(cross.crop(box), box[:2])
    sheet.paste(tile, ((index % 4) * 16, (3 - index // 4) * 16))
export('WaterPipeSheet', sheet)
export('WaterPipe', cross)
(ASSETS / 'irrigation-exports.json').write_text(json.dumps(records, indent=2) + '\n')
preview = Image.new('RGB', (720, 380), '#252b2e')
preview.paste(native.resize((256, 256), NEAREST), (24, 45), native.resize((256, 256), NEAREST))
preview.paste(sheet.resize((256, 256), NEAREST), (360, 45), sheet.resize((256, 256), NEAREST))
preview.paste(native, (24, 326), native)
preview.paste(cross, (85, 333), cross)
draw = ImageDraw.Draw(preview)
draw.text((24, 18), 'Groundwork W2 | 32 x 32 native', fill='white')
draw.text((360, 18), '16 conduit masks | 16 x 16 per tile', fill='white')
draw.text((135, 333), 'Actual native sizes at left; untested in-game candidates', fill='white')
preview.save(ASSETS / 'irrigation-preview.png')
print('Exported supply and 16 registered pipe masks, with flat normals and preview.')
