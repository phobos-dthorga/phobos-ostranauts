"""Export/verify saved ElevenLabs washer candidates; no synthesis or API calls."""
import argparse
import array
import hashlib
import json
import math
from pathlib import Path
import shutil
import subprocess
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/phobos-audio/washer-motor-pump-v1"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def properties(path):
    with wave.open(str(path), "rb") as clip:
        info = (clip.getnchannels(), clip.getsampwidth(), clip.getframerate(), clip.getnframes())
        if info != (1, 2, 44100, 264600):
            raise ValueError(f"Unexpected WAV format or length: {path.name}: {info}")
        samples = array.array("h", clip.readframes(clip.getnframes()))
    if sys.byteorder != "little":
        samples.byteswap()
    peak = max(abs(v) for v in samples) / 32768
    rms = math.sqrt(sum((v / 32768) ** 2 for v in samples) / len(samples))
    clipped = sum(v in (-32768, 32767) for v in samples)
    edges = [samples[0], samples[-1]]
    if clipped or edges != [0, 0]:
        raise ValueError(f"Clipping or unfaded edges: {path.name}")
    return {"channels": 1, "sample_width_bytes": 2, "sample_rate_hz": 44100,
            "frames": len(samples), "duration_seconds": 6.0,
            "peak_dbfs": round(20 * math.log10(max(peak, 1e-15)), 3),
            "rms_dbfs": round(20 * math.log10(max(rms, 1e-15)), 3),
            "clipped_samples": clipped, "first_last_pcm16": edges,
            "bytes": path.stat().st_size, "sha256": digest(path)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--write", action="store_true")
    action.add_argument("--check", action="store_true")
    parser.add_argument("--ffmpeg", help="FFmpeg executable; required for export if absent from PATH")
    args = parser.parse_args()
    manifest_path = PACK / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if len(manifest["clips"]) != 4:
        raise ValueError("Expected the four owner-selected candidates")
    # Validate every input before overwriting any export.
    for row in manifest["clips"]:
        for key, folder, suffix in (("original", "originals", ".mp3"), ("wav", "wav", ".wav")):
            path = PACK / row[key]["path"]
            if path.parent != PACK / folder or path.suffix != suffix:
                raise ValueError("Unexpected audio path in manifest")
        original = PACK / row["original"]["path"]
        if digest(original) != row["original"]["sha256"]:
            raise ValueError(f"Original hash changed: {original.name}")
    if args.write:
        ffmpeg = args.ffmpeg or shutil.which("ffmpeg")
        if not ffmpeg:
            parser.error("Pass --ffmpeg with an existing FFmpeg executable")
        version = subprocess.run([ffmpeg, "-version"], check=True, capture_output=True,
                                 text=True).stdout.splitlines()[0]
        manifest["export"]["tool_version"] = version
        for row in manifest["clips"]:
            output = PACK / row["wav"]["path"]
            output.parent.mkdir(parents=True, exist_ok=True)
            # Only decoding/downmixing and edge fades. Never replace originals.
            command = [ffmpeg, "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
                       "-i", str(PACK / row["original"]["path"]), "-map", "0:a:0",
                       "-ac", "1", "-ar", "44100", "-af", manifest["export"]["filter"],
                       "-c:a", "pcm_s16le", "-map_metadata", "-1", "-bitexact", str(output)]
            subprocess.run(command, check=True)
            row["wav"] = {"path": row["wav"]["path"], **properties(output)}
        manifest["export"]["script_sha256"] = digest(Path(__file__))
        manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    else:
        if digest(Path(__file__)) != manifest["export"]["script_sha256"]:
            raise ValueError("Exporter changed; review and export again")
        for row in manifest["clips"]:
            actual = {"path": row["wav"]["path"], **properties(PACK / row["wav"]["path"])}
            if actual != row["wav"]:
                raise ValueError(f"WAV hash or measured properties changed: {row['variant']}")
    print(json.dumps({"status": "exported" if args.write else "verified", "clips": 4,
                      "originals_preserved": True, "new_generation": False,
                      "channels": 1, "sample_rate_hz": 44100, "duration_seconds": 6.0}, indent=2))


if __name__ == "__main__":
    main()
