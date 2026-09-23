"""Inspect vanilla ship equipment without changing or redistributing game assets.

Creates local-only reference sheets (unaltered colours, nearest-neighbour scale),
an inventory of selected definitions and source fingerprints. These are analytical
figures, not mod artwork or rendered in-game scenes. Requires Pillow.
"""

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from ostranauts_json import read_json


SAMPLES = [
    ("ItmBattery02", "Ship battery"),
    ("ItmBattery02b", "Compact ship battery"),
    ("ItmAirPump02Off", "Air pump"),
    ("ItmAtmoScrubber01", "CO2 scrubber"),
    ("ItmAtmoScrubber02", "Contaminant scrubber"),
    ("ItmCooler01On", "Cooler"),
    ("ItmHeater01", "Heater"),
    ("ItmRCSDistro01", "Miura Hydra RCS regulator"),
    ("ItmRCSDistro02", "Kang 2202 RCS regulator"),
    ("ItmRCSCluster01", "RCS thruster assembly"),
    ("ItmStationNav", "Navigation console (world)"),
    ("ItmFloorGrate4x401", "Turbine lifter / current placeholder"),
    ("ItmReactorIC03Ignition", "Compact IC fusion reactor"),
    ("ItmFusionReactorCore01Ignition", "Sulaiman fusion reactor core"),
    ("ItmFusionCorePump01", "Fusion core pump"),
    ("ItmFusionCryoPump01", "Cryo distribution pump"),
    ("ItmFusionMHDGenerator01", "MHD generator"),
    ("ItmFusionFieldCoils01", "Fusion field coils"),
    ("ItmFusionFuelRegulator01", "Reactor fuel regulator"),
    ("ItmFusionLaserArray01", "Fusion laser array"),
    ("ItmFusionPelletFeeder01", "Pellet feeder"),
    ("ItmCargoPod01", "Cargo pod"),
]

# The declared pump base is a magenta Debug image in this build. Display the
# separately shipped Off texture as an explicitly unverified candidate, not as
# evidence that the default strImg is what players see at runtime.
DISPLAY_CANDIDATES = {"ItmAirPump02Off": "ItmAirPump02Off"}


def fingerprint(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_definitions(directory):
    definitions = {}
    for path in sorted(directory.glob("*.json")):
        for entry in read_json(path):
            definitions[entry["strName"]] = entry
    return definitions


def font(size):
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default(size=size)


def sheet(cells, title, output, columns=4):
    width, height, header = 370, 480, 92
    rows = (len(cells) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * width, header + rows * height), "#14191d")
    draw = ImageDraw.Draw(canvas)
    draw.text((20, 14), title, font=font(24), fill="#eff1eb")
    draw.text((20, 47), "LOCAL STUDY ONLY | Vanilla source textures, fixed 4x nearest-neighbour. No game lighting or wear shader.",
              font=font(17), fill="#abb9c1")
    for i, (label, path, subtitle) in enumerate(cells):
        x, y = (i % columns) * width, header + (i // columns) * height
        draw.rectangle((x + 7, y + 7, x + width - 7, y + height - 7), fill="#262c30")
        draw.text((x + 16, y + 16), label, font=font(15), fill="#f1e7cc")
        draw.text((x + 16, y + 40), subtitle, font=font(12), fill="#bfcbd1")
        if path and path.exists():
            with Image.open(path) as source:
                rgba = source.convert("RGBA")
            # Uncropped, unretouched reference pixels. Fixed scale preserves comparison.
            enlarged = rgba.resize((rgba.width * 4, rgba.height * 4), Image.Resampling.NEAREST)
            canvas.paste(enlarged, (x + (width - enlarged.width) // 2, y + 68 + (400 - enlarged.height) // 2), enlarged)
        else:
            draw.text((x + 20, y + 180), "No separate texture referenced", font=font(18), fill="#a7b0b5")
    canvas.save(output)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-path", required=True, type=Path)
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1] / ".local/research/equipment-art")
    args = parser.parse_args()
    output = args.output.resolve()
    local = Path(__file__).resolve().parents[1] / ".local"
    if not output.is_relative_to(local.resolve()):
        parser.error("Reference images must stay under the repository's ignored .local directory.")
    output.mkdir(parents=True, exist_ok=True)
    base = args.game_path.resolve() / "Ostranauts_Data/StreamingAssets"
    items = load_definitions(base / "data/items")
    owners = load_definitions(base / "data/condowners")
    images = base / "images"
    records, cells = [], []
    for identifier, title in SAMPLES:
        owner = owners[identifier]
        item_id = owner.get("strItemDef", identifier)
        item = items[item_id]
        display_name = DISPLAY_CANDIDATES.get(identifier, item["strImg"])
        path = images / (display_name + ".png")
        with Image.open(path) as source:
            rgba = source.convert("RGBA")
        cols = item.get("nCols", 1)
        rows = len(item.get("aSocketAdds", [])) // cols
        maps = {}
        for key in ("strImg", "strImgNorm", "strImgDamaged"):
            name = item.get(key)
            p = images / (name + ".png") if name else None
            maps[key] = {"name": name, "sha256": fingerprint(p) if p and p.exists() else None}
        record = {
            "object_id": identifier, "item_id": item_id, "title": title,
            "game_name": owner.get("strNameFriendly"), "texture_size": list(rgba.size),
            "alpha_bbox": rgba.getchannel("A").getbbox(), "grid_bounds": [cols, rows],
            "socket_cells": item.get("aSocketAdds", []), "normal_and_damage": maps,
            "height_parameter": item.get("fZScale"), "lights": item.get("aLights", []),
            "damage_mode": item.get("nDmgMode"), "portrait": owner.get("strPortraitImg"),
            "displayed_texture": display_name, "displayed_sha256": fingerprint(path),
            "display_candidate_only": identifier in DISPLAY_CANDIDATES,
        }
        records.append(record)
        note = " | candidate; runtime unresolved" if identifier in DISPLAY_CANDIDATES else f" | {cols} x {rows} grid bounds"
        cells.append((title, path, f"{rgba.width} x {rgba.height} px" + note))
    for page, start in enumerate(range(0, len(cells), 12), 1):
        sheet(cells[start:start + 12], f"Ostranauts ship equipment / native textures / {page}", output / f"equipment-{page}.png")
    states = []
    for identifier in ("ItmBattery02", "ItmAtmoScrubber01", "ItmFusionMHDGenerator01", "ItmFloorGrate4x401"):
        record = next(r for r in records if r["object_id"] == identifier)
        item = items[record["item_id"]]
        for label, key in (("Base colour", "strImg"), ("Normal map", "strImgNorm"), ("Damage texture", "strImgDamaged")):
            name = item.get(key)
            states.append((record["title"].split(" / ")[0] + " / " + label,
                           images / (name + ".png") if name else None, name or "none"))
        loose_id = identifier + ("OffLoose" if identifier == "ItmAtmoScrubber01" else "Loose")
        loose = owners.get(loose_id, {})
        loose_item = items.get(loose.get("strItemDef", loose_id), {})
        name = loose_item.get("strImg")
        states.append((record["title"].split(" / ")[0] + " / Loose", images / (name + ".png") if name else None, name or "none"))
    sheet(states, "Colour, normal, damage and loose forms / source textures", output / "equipment-states.png")
    manifest = {"scope": "Selected vanilla definitions, not Workshop overrides or runtime rendering", "records": records,
                "game_assembly_sha256": fingerprint(args.game_path.resolve() / "Ostranauts_Data/Managed/Assembly-CSharp.dll"),
                "definitions": {str(p.relative_to(base)): fingerprint(p) for folder in ("items", "condowners")
                                for p in sorted((base / "data" / folder).glob("*.json"))}}
    (output / "inventory.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    for record in records:
        print(f"{record['title']}: {record['texture_size']} pixels, {record['grid_bounds']} grid bounds, {record['normal_and_damage']['strImg']['name']}")
    print(f"Local reference sheets and inventory: {output}")


if __name__ == "__main__":
    main()
