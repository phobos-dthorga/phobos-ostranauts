"""Repeatable registration of original R3 masters; no native artwork inputs.
Retains whole silhouette at 48 px (3 tiles); all forms share bounds/pivot.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture/bulk'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
base = Image.open(ASSETS / 'source/reservoir-master.png').convert('RGBA')
crop = base.getchannel('A').point(lambda p: 255 if p > 128 else 0).getbbox()
records = []
preview = Image.new('RGB', (880, 360), '#252b2e')
draw = ImageDraw.Draw(preview)
for index, (form, source) in enumerate([
    ('Reservoir', 'reservoir-master.png'),
    ('ReservoirLoose', 'reservoir-master.png'),
    ('ReservoirDamaged', 'reservoir-damaged-master.png'),
    ('ReservoirLooseDamaged', 'reservoir-damaged-master.png'),
]):
    path = ASSETS / 'source' / source
    if not path.exists():
        continue  # Allows review of the pilot before producing the full family.
    master = Image.open(path).convert('RGBA')
    assert min(master.size) >= 96
    assert master.getextrema()[3][0] == 0
    # Register the generated edit by its opaque chassis bounds, not the diffuse
    # nearly-transparent export fringe. Neither crop changes placement geometry.
    bounds = master.getchannel('A').point(lambda p: 255 if p > 128 else 0).getbbox()
    native = Image.new('RGBA', (48, 48))
    native.alpha_composite(master.crop(bounds).resize((46, 44), Image.Resampling.NEAREST), (1, 2))
    native.save(OUT / (form + '.png'))
    normal = Image.new('RGBA', native.size, (128, 128, 255, 255))
    normal.putalpha(native.getchannel('A'))
    normal.save(OUT / (form + 'Normal.png'))
    records.append({'source': path.relative_to(ROOT).as_posix(), 'size':master.size, 'crop':bounds,
                    'sourceSha256':hashlib.sha256(path.read_bytes()).hexdigest(),
                    'form':form, 'pivot':[24,24], 'origin':[1,2], 'composeSize':[46,44],
                    'exports':{name:hashlib.sha256((OUT / name).read_bytes()).hexdigest()
                               for name in (form+'.png',form+'Normal.png')}})
    x=12+index*218
    draw.text((x,12),form,fill='white')
    enlarged=native.resize((192,192),Image.Resampling.NEAREST)
    preview.paste(enlarged,(x,36),enlarged)
    preview.paste(native,(x,245),native)
    for rotation in range(3):
        turned=native.rotate((rotation+1)*90)
        preview.paste(turned,(x+54*(rotation+1),245),turned)
draw.text((12,325),'48 px native, 4x enlargement and quarter turns. Static check; Unity lighting awaits owner review.',fill='white')
preview.save(ASSETS / 'preview.png')
(ASSETS / 'exports.json').write_text(json.dumps(records,indent=2)+'\n')
print(f'Exported {len(records)} registered R3 forms and matching flat normals.')
