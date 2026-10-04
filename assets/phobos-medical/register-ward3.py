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
from registration import registered, png

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "assets/phobos-medical/source/ward3-nanomedical-chatgpt.png"
SOURCE_SHA256 = "1e38c5c746c7bb847291cf99fdfe7218e731e3ecd73f98229d6e78724f63c768"
MASTER = ROOT / "assets/artwork-completion/source/ward3-medical-bed.png"
SIZE, BODY, TOP = (96, 160), (88, 156), 2
TOLERANCE, COLOURS = 28, 96


def build():
    data = SOURCE.read_bytes()
    if hashlib.sha256(data).hexdigest() != SOURCE_SHA256:
        raise SystemExit(f"Changed retained concept: {SOURCE}")
    return png(registered(Image.open(io.BytesIO(data)), SIZE, BODY,
                          ((SIZE[0] - BODY[0]) // 2, TOP), COLOURS, TOLERANCE))


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
