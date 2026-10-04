"""Prepare the Vigil-2 master at 128 x 128 and its 32 x 32 native derivatives (Medical 0.3.0 binds them).

Reuse the Ward-3's edge-only white removal, box reduction and thresholded alpha.
The 112 x 112 body sits on a transparent 128 x 128 canvas with an eight-pixel margin;
The Ward-3's 96-colour palette reduction retains the enamel edges and teal accents.
Native output samples the master at phase (1, 1) of each 4 x 4 block (Medical 0.3.0: every phase was compared at
native size; this one keeps the vent dark and the handle, arm and caster locks distinct), with a matching neutral
normal. The completion manifest records the same sampleOffset, so the runtime export equals the native pilot.
Run with --check to verify every derivative without writing or generating art.
"""
import argparse
import hashlib
from pathlib import Path
from PIL import Image, ImageDraw
from registration import registered, png

ASSETS = Path(__file__).resolve().parent
SOURCE = ASSETS / "source/vigil1-monitor-chatgpt.png"
SOURCE_SHA256 = "06e109cd1f4b717bb751ce5fe827ff08fc874a16dcde4df87efc5d3260e60ebd"
MASTER_SIZE, BODY_SIZE, ORIGIN = (128, 128), (112, 112), (8, 8)
NATIVE_SIZE, COLOURS = (32, 32), 96
SAMPLE_OFFSET = (1, 1)


def build():
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise ValueError("Changed retained Vigil-1 original")
    with Image.open(SOURCE) as source:
        master = registered(source, MASTER_SIZE, BODY_SIZE, ORIGIN, COLOURS)
    step = MASTER_SIZE[0] // NATIVE_SIZE[0]
    native = Image.new("RGBA", NATIVE_SIZE)
    for y in range(NATIVE_SIZE[1]):
        for x in range(NATIVE_SIZE[0]):
            native.putpixel((x, y), master.getpixel((step * x + SAMPLE_OFFSET[0], step * y + SAMPLE_OFFSET[1])))
    normal = Image.new("RGBA", NATIVE_SIZE, (128, 128, 255, 255))
    normal.putalpha(native.getchannel("A"))
    preview = Image.new("RGB", (344, 300), "#252b2e")
    preview.paste(native, (12, 32), native)
    enlarged = native.resize((256, 256), Image.Resampling.NEAREST)
    preview.paste(enlarged, (72, 24), enlarged)
    draw = ImageDraw.Draw(preview)
    draw.text((12, 8), "32 x 32", fill="white")
    draw.text((72, 8), "8x nearest-neighbour", fill="white")
    return {"vigil2-monitor-master.png": png(master), "vigil2-monitor-native.png": png(native),
            "vigil2-monitor-nativeNormal.png": png(normal), "vigil2-monitor-preview.png": png(preview)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true")
    check = parser.parse_args().check
    for name, data in build().items():
        target = ASSETS / name
        if check:
            if not target.exists() or target.read_bytes() != data:
                raise SystemExit("Stale Vigil-2 derivative: " + name)
        else:
            target.write_bytes(data)
    print("Verified Vigil-2 derivatives." if check else "Prepared the Vigil-2 master and native derivatives.")
