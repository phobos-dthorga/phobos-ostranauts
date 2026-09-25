"""Registered mechanical exports of separate generated layers; no generation."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
OUT.mkdir(parents=True, exist_ok=True)
LAYOUT = json.loads((ASSETS / 'layers.json').read_text(encoding='utf-8-sig'))
NEAREST = Image.Resampling.NEAREST
records = []


def source(filename, native_size):
    path = ASSETS / 'source' / filename
    master = Image.open(path).convert('RGBA')
    factor = 4 if native_size <= 32 else 2
    assert min(master.size) >= native_size * factor, f'{filename}: insufficient master'
    assert master.width == master.height, f'{filename}: unexpected canvas'
    assert master.getextrema()[3][0] == 0, f'{filename}: no transparent pixels'
    return master, {'source': path.relative_to(ROOT).as_posix(),
                    'sourceSHA256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'canvas': list(master.size)}


def export(name, pixels, inputs, normal=False):
    path = OUT / (name + '.png')
    pixels.save(path)
    record = {'name': name, 'inputs': inputs, 'export': list(pixels.size),
              'sampling': 'nearest; alpha composite registered layers',
              'layout': 'assets/phobos-agriculture/layers.json',
              'exportSHA256': hashlib.sha256(path.read_bytes()).hexdigest()}
    if normal:
        # Neutral tangent normal, not inferred physical relief. Alpha is registered.
        normals = Image.new('RGBA', pixels.size, (128, 128, 255, 255))
        normals.putalpha(pixels.getchannel('A'))
        normal_path = OUT / (name + 'Normal.png')
        normals.save(normal_path)
        record['normalSHA256'] = hashlib.sha256(normal_path.read_bytes()).hexdigest()
    records.append(record)


rack = LAYOUT['rack']
master, rack_input = source(rack['source'], rack['nativeSize'])
empty = master.resize((rack['nativeSize'],) * 2, NEAREST)
inlet = rack['waterInlet']
inlet_master, inlet_input = source(inlet['source'], 16)
inlet_sprite = inlet_master.crop(tuple(inlet['sourceCrop'])).resize((16, 16), NEAREST).crop(tuple(inlet['part']))
empty.alpha_composite(inlet_sprite, tuple(inlet['origin']))
export('Rack', empty, [rack_input, inlet_input], normal=True)
for key, filename in LAYOUT['plants'].items():
    plant_master, plant_input = source(filename, rack['plantSize'])
    plant = plant_master.resize((rack['plantSize'],) * 2, NEAREST)
    export(key, plant, [plant_input])
    composed = empty.copy()
    for x, y in rack['plantCenters']:
        origin = (x - plant.width // 2, y - plant.height // 2)
        assert min(origin) >= 0 and origin[0] + plant.width <= empty.width and origin[1] + plant.height <= empty.height
        composed.alpha_composite(plant, origin)
    export('Rack-' + key, composed, [rack_input, inlet_input, plant_input], normal=True)

cooker = LAYOUT['cooker']
counter_master, counter_input = source(cooker['source'], cooker['nativeSize'])
counter = counter_master.resize((cooker['nativeSize'],) * 2, NEAREST)
export('Counter', counter, [counter_input])
stove_master, stove_input = source(cooker['applianceSource'], cooker['applianceSize'])
stove = stove_master.resize((cooker['applianceSize'],) * 2, NEAREST)
export('Stove', stove, [stove_input])
counter.alpha_composite(stove, tuple(cooker['applianceOrigin']))
export('Cooker', counter, [counter_input, stove_input], normal=True)
(ASSETS / 'exports.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')

# Review all stages at the same integer enlargement; never smooth native exports.
stages = ['sprout', 'young', 'mature', 'harvest', 'wilted', 'dead']
sheet = Image.new('RGB', (1200, 610), '#252b2e')
draw = ImageDraw.Draw(sheet)
for row, crop in enumerate(['Potato', 'Lettuce']):
    for col, stage in enumerate(stages):
        name = crop + '-' + stage
        pixels = Image.open(OUT / ('Rack-' + name + '.png')).convert('RGBA').resize((192, 192), NEAREST)
        sheet.paste(pixels, (col * 200 + 4, row * 220 + 8), pixels)
        draw.text((col * 200 + 12, row * 220 + 202), name, fill='white')
for col, name in enumerate(['Rack', 'Counter', 'Stove', 'Cooker']):
    pixels = Image.open(OUT / (name + '.png')).convert('RGBA')
    scale = 2 if pixels.width == 64 else 4 if pixels.width == 32 else 8
    pixels = pixels.resize((pixels.width * scale, pixels.height * scale), NEAREST)
    sheet.paste(pixels, (col * 200 + 30, 450), pixels)
    draw.text((col * 200 + 25, 585), name, fill='white')
draw.text((830, 490), 'Verdemorrow living-ship candidates', fill='white')
draw.text((830, 512), '4 x 4 rack | 2 x 2 galley', fill='white')
draw.text((830, 534), 'Separate sources; fixed registration', fill='white')
sheet.save(ASSETS / 'living-visuals-preview.png')
print(f'Exported {len(records)} registered Agriculture images plus 14 flat normals; footprints unchanged.')
