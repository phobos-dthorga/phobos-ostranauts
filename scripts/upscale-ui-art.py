#!/usr/bin/env python3
"""Explicit local Real-ESRGAN export. Requires Pillow and an owner-approved local executable/models.

Never downloads or installs software. Keeps source, AI RGB output and final alpha registration distinct.
AI cannot recover lossless detail. Inspect output before selecting it as production artwork.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

from PIL import Image, ImageOps


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--models", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--gpu", default="auto")
    args = parser.parse_args()
    source, exe, models, output = (p.resolve() for p in (args.source, args.executable, args.models, args.output_dir))
    if source.parent == output:
        raise ValueError("Keep the source outside the derived output directory")
    output.mkdir(parents=True, exist_ok=True)
    model = "realesrgan-x4plus"
    parameters, weights = models / (model + ".param"), models / (model + ".bin")
    for file in (source, exe, parameters, weights):
        if not file.is_file():
            raise FileNotFoundError(file)
    with Image.open(source) as opened:
        original = opened.convert("RGBA")
    rgb = output / "input-rgb.png"
    original.convert("RGB").save(rgb)
    enhanced = output / "ai-rgb-4x.png"
    command = [str(exe), "-i", str(rgb), "-o", str(enhanced), "-m", str(models), "-n", model, "-s", "4", "-t", "256", "-j", "1:2:2"]
    if args.gpu != "auto":
        command += ["-g", args.gpu]
    with (output / "inference.log").open("w", encoding="utf-8") as log:
        subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=True)
    with Image.open(enhanced) as opened:
        ai = opened.convert("RGB")
    if ai.size != (original.width * 4, original.height * 4):
        raise ValueError(f"Unexpected model output dimensions: {ai.size}")
    # Keep AI away from alpha: silhouette ownership remains with the original master.
    master = ai.resize((original.width * 2, original.height * 2), Image.Resampling.LANCZOS).convert("RGBA")
    master.putalpha(original.getchannel("A").resize(master.size, Image.Resampling.LANCZOS))
    master_path = output / "flight-hub-ai-master.png"
    master.save(master_path)
    # Exact registration with uniform scaling and a centred subpixel crop (<0.1% for this source).
    production = ImageOps.fit(master, (1200, 1920), Image.Resampling.LANCZOS)
    production_path = output / "flight-hub-production.png"
    production.save(production_path)
    baseline = ImageOps.fit(original, production.size, Image.Resampling.LANCZOS)
    baseline.save(output / "flight-hub-lanczos-comparison.png")
    # A useful 100% crop for inspecting fastener geometry and thin borders.
    comparison = Image.new("RGB", (600, 600), "#28323a")
    crop = (0, 0, 300, 600)
    comparison.paste(baseline.crop(crop), (0, 0), baseline.crop(crop))
    comparison.paste(production.crop(crop), (300, 0), production.crop(crop))
    comparison.save(output / "fastener-comparison.png")
    report = {"source_size": original.size, "model_output_size": ai.size, "master_size": master.size,
              "production_size": production.size, "model": model, "source_sha256": digest(source),
              "executable_sha256": digest(exe), "model_parameters_sha256": digest(parameters),
              "model_weights_sha256": digest(weights), "master_sha256": digest(master_path),
              "production_sha256": digest(production_path),
              "alpha": "Original alpha, Lanczos resampled; never AI reconstructed",
              "processing": "Real-ESRGAN 4x RGB; Lanczos 2x retained master; uniform fit to 1200x1920 production",
              "limitations": "AI inferred texture. Not a native high-resolution generation or lossless recovery."}
    (output / "provenance.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
