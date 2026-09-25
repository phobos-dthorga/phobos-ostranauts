"""Repeatable registered PNG exports; no generation and no game-derived inputs."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
OUT.mkdir(parents=True, exist_ok=True)
plan = {'Rack': ('rackEdit', 64), 'Cooker': ('cookerRetry', 32),
        'Potato-mature': ('plantPilot', 16), 'Potato-dead': ('deadEdit', 16)}
plan.update({'Potato-' + state: ('edit_' + state, 16) for state in ['sprout', 'young', 'harvest', 'wilted']})
records = []
for name, (source, size) in plan.items():
    path = ASSETS / 'source' / (source + '.png')
    master = Image.open(path).convert('RGBA')
    factor = 4 if size <= 32 else 2
    assert master.width >= size * factor and master.height >= size * factor
    assert master.getextrema()[3][0] == 0, f'{name}: no transparent pixels'
    exported = master.resize((size, size), Image.Resampling.NEAREST)
    exported.save(OUT / (name + '.png'))
    # Flat registered normal: neutral tangent normal and identical alpha.
    if name in ('Rack', 'Cooker'):
        normal = Image.new('RGBA', exported.size, (128, 128, 255, 255))
        normal.putalpha(exported.getchannel('A')); normal.save(OUT / (name + 'Normal.png'))
    records.append({'name': name, 'source': str(path.relative_to(ROOT)).replace('\\', '/'),
                    'sourceSHA256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'canvas': list(master.size), 'export': [size, size], 'sampling': 'nearest',
                    'registration': 'whole canvas; centered pivot; no independent crop',
                    'exportSHA256': hashlib.sha256((OUT / (name + '.png')).read_bytes()).hexdigest()})
(ASSETS / 'exports.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
# Evaluation sheet: native sprites at 4x integer scale, never smoothed.
sheet = Image.new('RGB', (640, 270), '#252b2e'); draw = ImageDraw.Draw(sheet)
for index, name in enumerate(plan):
    x, y = (index % 4) * 160, (index // 4) * 135
    sprite = Image.open(OUT / (name + '.png')).convert('RGBA')
    scale = min(4, 112 // sprite.width)
    sprite = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)
    sheet.paste(sprite, (x + (160 - sprite.width) // 2, y + 4), sprite)
    draw.text((x + 8, y + 116), name, fill='white')
sheet.save(ASSETS / 'pilot-preview.png')
print('Exported 8 Agriculture sprites and 2 registered flat normals; physical footprints unchanged.')
