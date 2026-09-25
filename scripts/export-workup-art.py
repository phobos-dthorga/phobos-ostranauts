"""Export the retained original B2 master into a registered native footprint."""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/phobos-agriculture'
OUT = ROOT / 'mods/PhobosAgriculture/images/phobos/agriculture'
source = ASSETS / 'source/groundwork-b2-master-v1.png'
master = Image.open(source).convert('RGBA')
assert min(master.size) >= 128 and master.getextrema()[3][0] == 0
bounds = master.getbbox()
native = Image.new('RGBA', (32, 32))
native.alpha_composite(master.crop(bounds).resize((30, 30), Image.Resampling.NEAREST), (1, 1))
native.save(OUT / 'Workup.png')
normal = Image.new('RGBA', native.size, (128, 128, 255, 255))
normal.putalpha(native.getchannel('A'))
normal.save(OUT / 'WorkupNormal.png')
preview = Image.new('RGBA', (352, 320), '#252b2e')
preview.alpha_composite(native.resize((256, 256), Image.Resampling.NEAREST), (16, 16))
preview.alpha_composite(native, (296, 16))
preview.save(ASSETS / 'workup-preview.png')
record = {'source': source.relative_to(ROOT).as_posix(), 'sourceSize': master.size, 'crop': bounds,
          'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'outputSize': [32, 32],
          'origin': [1, 1], 'composeSize': [30, 30], 'pivot': [16, 16], 'projection': 'orthographic overhead',
          'layers': ['permanent sealed equipment chassis'],
          'damage': 'native appliance damage presentation; registered shared master, no generated damaged variant',
          'supplies': 'reuse original Stock-residue, Stock-nutrients and Stock-recovery_reject artwork; identities and localized descriptions differ',
          'exports': {name: hashlib.sha256((OUT / name).read_bytes()).hexdigest() for name in ('Workup.png','WorkupNormal.png')}}
(ASSETS / 'workup-exports.json').write_text(json.dumps(record, indent=2) + '\n')
print('Exported B2 32 x 32 chassis, matching flat normal and native-size preview.')
