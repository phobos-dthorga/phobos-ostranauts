"""Export/verify saved ElevenLabs washer candidates and loops; no synthesis or API calls."""
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
RATE = 44100
SOURCE_FRAMES = 6 * RATE
OVERLAP = RATE // 4
LOOP_FRAMES = SOURCE_FRAMES - OVERLAP


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def properties(path, looping=False):
    with wave.open(str(path), "rb") as clip:
        info = (clip.getnchannels(), clip.getsampwidth(), clip.getframerate(), clip.getnframes())
        if info != (1, 2, RATE, LOOP_FRAMES if looping else SOURCE_FRAMES):
            raise ValueError(f"Unexpected WAV format or length: {path.name}: {info}")
        samples = array.array("h", clip.readframes(clip.getnframes()))
    if sys.byteorder != "little":
        samples.byteswap()
    peak = max(abs(v) for v in samples) / 32768
    rms = math.sqrt(sum((v / 32768) ** 2 for v in samples) / len(samples))
    clipped = sum(v in (-32768, 32767) for v in samples)
    edges = [samples[0], samples[-1]]
    if clipped or (not looping and edges != [0, 0]):
        raise ValueError(f"Clipping or unfaded one-shot edges: {path.name}")
    result = {"channels": 1, "sample_width_bytes": 2, "sample_rate_hz": RATE,
            "frames": len(samples), "duration_seconds": len(samples) / RATE,
            "peak_dbfs": round(20 * math.log10(max(peak, 1e-15)), 3),
            "rms_dbfs": round(20 * math.log10(max(rms, 1e-15)), 3),
            "clipped_samples": clipped, "first_last_pcm16": edges,
            "bytes": path.stat().st_size, "sha256": digest(path)}
    if looping:
        steps = sorted(abs(b - a) for a, b in zip(samples, samples[1:]))
        p99 = steps[int(0.99 * (len(steps) - 1))]
        seam = abs(samples[0] - samples[-1])
        join = abs(samples[-OVERLAP] - samples[-OVERLAP - 1])
        # A loop need not end at zero. Its two joins must behave like ordinary
        # adjacent samples, with no artificial edge fade or silent seam.
        window = RATE // 50
        boundary = samples[-window:] + samples[:window]
        boundary_rms = math.sqrt(sum((v / 32768) ** 2 for v in boundary) / len(boundary))
        zero_gap = 0
        for sequence in (reversed(samples), iter(samples)):
            for value in sequence:
                if value:
                    break
                zero_gap += 1
        if seam > p99 or join > p99 or boundary_rms < rms * 0.1 or zero_gap > 8:
            raise ValueError(f"Discontinuous or silent loop boundary: {path.name}")
        result.update({"seam_step_pcm16": seam, "crossfade_join_step_pcm16": join,
                       "ordinary_step_max_pcm16": steps[-1],
                       "ordinary_step_p99_pcm16": p99,
                       "boundary_40ms_rms_dbfs": round(20 * math.log10(max(boundary_rms, 1e-15)), 3),
                       "boundary_zero_run_samples": zero_gap})
    return result


def make_loop(ffmpeg, original, output):
    decoded = subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-nostdin",
                              "-i", str(original), "-map", "0:a:0", "-ac", "2", "-ar", str(RATE),
                              "-f", "f32le", "-c:a", "pcm_f32le", "pipe:1"],
                             check=True, capture_output=True).stdout
    stereo = array.array("f", decoded)
    if sys.byteorder != "little":
        stereo.byteswap()
    if len(stereo) != SOURCE_FRAMES * 2 or not all(math.isfinite(v) for v in stereo):
        raise ValueError(f"Unexpected decoded source: {original.name}")
    # Explicit channel averaging avoids FFmpeg's different default mono gain
    # for floating-point versus integer outputs. These saved MP3s are stereo.
    source = [(stereo[i] + stereo[i + 1]) * 0.5 for i in range(0, len(stereo), 2)]
    # Move the wrap into a circular overlap. Both external joins land on
    # originally adjacent source samples. Raised-cosine complementary weights
    # avoid boosting correlated motor tones. Never add silence or per-cycle fades.
    loop = list(source[OVERLAP:-OVERLAP])
    for i in range(OVERLAP):
        weight = 0.5 - 0.5 * math.cos(math.pi * i / (OVERLAP - 1))
        loop.append((1 - weight) * source[-OVERLAP + i] + weight * source[i])
    dc = sum(loop) / len(loop)
    quantized = [round((v - dc) * 32768) for v in loop]
    if min(quantized) <= -32768 or max(quantized) >= 32767:
        raise ValueError(f"Loop would clip: {original.name}; review instead of changing gain")
    samples = array.array("h", quantized)
    if sys.byteorder != "little":
        samples.byteswap()
    output.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output), "wb") as clip:
        clip.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        clip.writeframes(samples.tobytes())
    return round(dc, 12)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--write", action="store_true")
    action.add_argument("--check", action="store_true")
    parser.add_argument("--ffmpeg", help="FFmpeg executable; required for export if absent from PATH")
    args = parser.parse_args()
    manifest_path = PACK / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if [row["variant"] for row in manifest["clips"]] != list("ABCDEFGH"):
        raise ValueError("Expected the eight washer candidates A-H in audition order")
    # Validate every input before overwriting any export.
    for row in manifest["clips"]:
        for key, folder, suffix in (("original", "originals", ".mp3"), ("wav", "wav", ".wav"),
                                    ("loop", "loops", ".wav")):
            if key == "wav" and key not in row:
                continue
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
            original = PACK / row["original"]["path"]
            if "wav" in row:
                output = PACK / row["wav"]["path"]
                output.parent.mkdir(parents=True, exist_ok=True)
                # Preserve the original A-D one-shot export recipe byte-for-byte.
                command = [ffmpeg, "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
                           "-i", str(original), "-map", "0:a:0", "-ac", "1", "-ar", str(RATE),
                           "-af", manifest["export"]["filter"], "-c:a", "pcm_s16le",
                           "-map_metadata", "-1", "-bitexact", str(output)]
                subprocess.run(command, check=True)
                row["wav"] = {"path": row["wav"]["path"], **properties(output)}
            output = PACK / row["loop"]["path"]
            removed_dc = make_loop(ffmpeg, original, output)
            row["loop"] = {"path": row["loop"]["path"], "removed_dc": removed_dc,
                           **properties(output, looping=True)}
        manifest["export"]["script_sha256"] = digest(Path(__file__))
        manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    else:
        if digest(Path(__file__)) != manifest["export"]["script_sha256"]:
            raise ValueError("Exporter changed; review and export again")
        for row in manifest["clips"]:
            for key in ("wav", "loop"):
                if key not in row:
                    continue
                actual = {"path": row[key]["path"], **properties(PACK / row[key]["path"], key == "loop")}
                if key == "loop":
                    actual["removed_dc"] = row[key]["removed_dc"]
                if actual != row[key]:
                    raise ValueError(f"WAV hash or measured properties changed: {row['variant']}/{key}")
    print(json.dumps({"status": "exported" if args.write else "verified", "clips": 8,
                      "loop_wavs": 8, "retained_one_shot_wavs": 4,
                      "originals_preserved": True, "new_generation": False,
                      "channels": 1, "sample_rate_hz": RATE, "loop_duration_seconds": LOOP_FRAMES / RATE}, indent=2))


if __name__ == "__main__":
    main()
