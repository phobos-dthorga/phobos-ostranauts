"""Mechanical registration/export of original irrigation masters; no generation.
Run with Python/Pillow. Since Framework 0.56.0 the conduit's pipe art is drawn in its own lane by
scripts/export-line-art.py (from this master's palette); this script exports the W2 supply only and keeps the
conduit master's registration record.
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
# The pipe tiles and sheet are exported in lanes by scripts/export-line-art.py; the master stays registered here.
sheet = Image.open(OUT / 'WaterPipeSheet.png').convert('RGBA')
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
print('Exported the W2 supply with a flat normal and the preview; pipe masks come from export-line-art.py.')
