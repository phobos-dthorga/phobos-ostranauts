"""Shared opaque Manufacturing machinery registration and preview exports."""
import argparse
import hashlib
import io
import json
from pathlib import Path
from PIL import Image, ImageDraw

ASSETS = Path(__file__).resolve().parent
ROOT = ASSETS.parents[1]


def png(image):
    stream = io.BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def original(path, expected_hash, expected_size):
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected_hash:
        raise ValueError("Changed retained Oxsmith input: " + str(path))
    with Image.open(io.BytesIO(data)) as image:
        result = image.convert("RGBA")
    if result.size != tuple(expected_size) or result.getchannel("A").getextrema() != (255, 255):
        raise ValueError("Unexpected input dimensions or nonopaque footprint: " + str(path))
    return result


def build(record_name):
    record = json.loads((ASSETS / record_name).read_text(encoding="utf-8"))
    registration = record["registration"]
    outputs, natives, hashes = {}, [], {}
    for request in record["requests"]:
        master_size = tuple(request.get("masterSize", registration.get("masterSize", ())))
        native_size = tuple(request.get("nativeSize", registration.get("nativeSize", ())))
        footprint = request.get("footprintTiles", registration.get("footprintTiles", ()))
        minimum = 4 if min(native_size) <= 32 else 2
        if (tuple(size * 16 for size in footprint) != native_size
                or any(a < b * minimum or a % b for a, b in zip(master_size, native_size))):
            raise ValueError("Machine footprint, multiplier and native dimensions disagree: " + request["key"])
        image = original(ASSETS / request["source"], request["sha256"], request["returnedSize"])
        if image.width != image.height or min(image.size) < 1024:
            raise ValueError("Retained original must be a square of at least 1024 pixels")
        master = image.convert("RGB").resize(master_size, Image.Resampling.BOX).quantize(
            colors=registration["paletteColours"], method=Image.Quantize.MEDIANCUT,
            dither=Image.Dither.NONE).convert("RGBA")
        native = master.resize(native_size, Image.Resampling.NEAREST)
        normal = Image.new("RGBA", native_size, (128, 128, 255, 255))
        normal.putalpha(native.getchannel("A"))
        for field, image in (("master", master), ("native", native), ("normal", normal)):
            name = request[field]
            outputs[ASSETS / name] = png(image)
            hashes[name] = hashlib.sha256(outputs[ASSETS / name]).hexdigest()
        natives.append(native)
    preview = oxsmith_preview(record, natives) if "comparisons" in record else reactor_preview(record, natives)
    name = registration["familyPreview"]
    outputs[ASSETS / name] = png(preview)
    hashes[name] = hashlib.sha256(outputs[ASSETS / name]).hexdigest()
    outputs[ASSETS / registration.get("hashes", "oxsmith-export-hashes.json")] = (json.dumps(hashes, indent=2) + "\n").encode()
    return outputs


def oxsmith_preview(record, natives):
    # Keep the initial Oxsmith review exact; comparisons are pinned pre-restyle baselines.
    preview = Image.new("RGB", (700, 420), "#252b2e")
    draw = ImageDraw.Draw(preview)
    draw.text((12, 8), "Native 64 x 64", fill="white")
    for index, (request, native) in enumerate(zip(record["requests"], natives)):
        y, x = 34 + index * 130, 124 + index * 276
        preview.paste(native, (12, y), native)
        draw.text((12, y + 70), request["key"].upper(), fill="white")
        draw.text((x, 8), request["key"].upper() + " / 4x nearest-neighbour", fill="white")
        zoom = native.resize((native.width * 4, native.height * 4), Image.Resampling.NEAREST)
        preview.paste(zoom, (x, 34), zoom)
    draw.text((12, 308), "Existing Phobos machinery / native scale", fill="white")
    for index, reference in enumerate(record["comparisons"]):
        image = original(ROOT / reference["path"], reference["sha256"], reference["nativeSize"])
        x = 12 + index * 140
        preview.paste(image, (x, 328), image)
        draw.text((x, 397), reference["name"], fill="white")
    return preview


def reactor_preview(record, natives):
    columns, width, height = 3, 360, 332
    preview = Image.new("RGB", (columns * width, ((len(natives) + columns - 1) // columns) * height), "#252b2e")
    draw = ImageDraw.Draw(preview)
    for index, (request, native) in enumerate(zip(record["requests"], natives)):
        x, y = index % columns * width, index // columns * height
        draw.text((x + 12, y + 8), request["model"].removeprefix("Phobos' "), fill="white")
        draw.text((x + 12, y + 24), "Native", fill="white")
        preview.paste(native, (x + 12, y + 42), native)
        zoom = native.resize((native.width * 4, native.height * 4), Image.Resampling.NEAREST)
        preview.paste(zoom, (x + 92, y + 42), zoom)
        previous = request["previousReference"]
        before = original(ROOT / previous["path"], previous["sha256"], request["nativeSize"])
        draw.text((x + 12, y + 244), "Before", fill="#aab2b5")
        preview.paste(before, (x + 12, y + 262), before)
        draw.text((x + 92, y + 310), "4x nearest-neighbour / " + str(native.width) + " x " + str(native.height), fill="white")
    return preview


def main(record_name):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    check = parser.parse_args().check
    for path, data in build(record_name).items():
        if check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit("Stale Manufacturing derivative: " + str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(("Verified " if check else "Prepared ") + record_name.removesuffix("-requests.json") + " artwork.")
