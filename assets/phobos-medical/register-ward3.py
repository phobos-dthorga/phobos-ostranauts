"""Register the untouched Ward-3 Imagegen concept as the 96 x 160 working master (Phobos Medical 0.1.1).

Mechanical and repeatable; never calls a provider. Steps, each chosen by inspecting trials at the 48 x 80 native size:

1. Remove the generator's white margin by flood fill from the canvas edges only (tolerance 28 of 255), so whites
   inside the bed are kept.
2. Crop to the silhouette (747 x 1590, width to height 0.47) and box-reduce it to 88 x 156 px. That widens it by
   about a fifth to suit the 3 x 5 footprint: 44 native px across, the width of the game's own medical bed (its
   48 x 80 image is opaque over 44 x 80). Narrower trials left a bed visibly thinner than its tiles; wider ones
   flattened the round cartridges.
3. Hold the colours to a 96-colour median-cut palette with no dithering (32, 48 and 64 colours lost the teal
   cartridge caps and side plates at native size) and threshold alpha at half.
4. Centre it on a transparent 96 x 160 canvas with 4 px at each side and 2 px top and bottom, so the exporter's
   nearest-neighbour native sample is not clipped.

Usage: python assets/phobos-medical/register-ward3.py [--check]
"""
import argparse
import hashlib
import io
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "assets/phobos-medical/source/ward3-nanomedical-chatgpt.png"
SOURCE_SHA256 = "1e38c5c746c7bb847291cf99fdfe7218e731e3ecd73f98229d6e78724f63c768"
MASTER = ROOT / "assets/artwork-completion/source/ward3-medical-bed.png"
SIZE, BODY, TOP = (96, 160), (88, 156), 2
TOLERANCE, COLOURS = 28, 96


def cutout(image):
    rgb = image.convert("RGB")
    w, h = rgb.size
    px = rgb.load()
    outside = bytearray(w * h)
    stack = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    while stack:
        x, y = stack.pop()
        i = y * w + x
        if outside[i] or min(px[x, y]) < 255 - TOLERANCE:
            continue
        outside[i] = 1
        stack.extend(p for p in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)) if 0 <= p[0] < w and 0 <= p[1] < h)
    alpha = Image.frombytes("L", (w, h), bytes(0 if v else 255 for v in outside))
    rgba = rgb.convert("RGBA")
    rgba.putalpha(alpha)
    return rgba.crop(alpha.getbbox())


def build():
    data = SOURCE.read_bytes()
    if hashlib.sha256(data).hexdigest() != SOURCE_SHA256:
        raise SystemExit(f"Changed retained concept: {SOURCE}")
    body = cutout(Image.open(io.BytesIO(data))).resize(BODY, Image.Resampling.BOX)
    alpha = body.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    colours = body.convert("RGB").quantize(colors=COLOURS, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    colours.putalpha(alpha)
    canvas = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    canvas.alpha_composite(colours, ((SIZE[0] - BODY[0]) // 2, TOP))
    stream = io.BytesIO()
    canvas.save(stream, format="PNG")
    return stream.getvalue()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true")
    payload = build()
    if parser.parse_args().check:
        if not MASTER.exists() or MASTER.read_bytes() != payload:
            raise SystemExit(f"Stale registered master: {MASTER}")
        print("Registered master current:", hashlib.sha256(payload).hexdigest())
    else:
        MASTER.parent.mkdir(parents=True, exist_ok=True)
        MASTER.write_bytes(payload)
        print("Wrote", MASTER.relative_to(ROOT).as_posix(), hashlib.sha256(payload).hexdigest())
