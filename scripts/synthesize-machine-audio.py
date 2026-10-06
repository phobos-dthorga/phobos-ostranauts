"""Author/verify original intermittent machine SFX candidates; never changes a mod.

Python standard library only. Recipes, timings, seeds and gains are in the pack.
No recording, game asset, audio model, network access or paid generation is used.
"""
import argparse
import array
import hashlib
import html
import io
import json
import math
from pathlib import Path
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/phobos-audio/work-sounds-v1"
RATE = 44100
TAU = 2 * math.pi


def sha(data):
    return hashlib.sha256(data).hexdigest()


def db(value):
    return round(20 * math.log10(max(value, 1e-15)), 3)


def noise(seed):
    """Specified xorshift32 stream, independent of Python's random module."""
    state = seed & 0xffffffff or 1
    while True:
        state ^= (state << 13) & 0xffffffff
        state ^= state >> 17
        state ^= (state << 5) & 0xffffffff
        state &= 0xffffffff
        yield state / 2147483648 - 1


def envelope(t, duration, attack, release):
    if t < 0 or t >= duration:
        return 0
    rise = 0.5 - 0.5 * math.cos(math.pi * min(1, t / attack))
    fall = 0.5 - 0.5 * math.cos(math.pi * min(1, (duration - t) / release))
    return rise * fall


def render(recipe, variant):
    n = round(recipe["seconds"] * RATE)
    signal = [0.0] * n
    seed = recipe["seed"] + variant
    # Variant B changes resonances, motor load and stroke spacing slightly. No
    # pitch-shifting at playback is needed and the two exports have equal duration.
    pitch = 1.0 if variant == 0 else 0.965
    offset = 0.0 if variant == 0 else 0.037
    for index, layer in enumerate(recipe["layers"]):
        kind = layer[0]
        start, duration, gain = layer[1:4]
        if kind == "tap":
            start += offset
        begin = round(start * RATE)
        length = min(round(duration * RATE), n - begin)
        if begin < 0 or length <= 0:
            raise ValueError("Layer outside clip: " + recipe["id"])
        stream = noise(seed + index * 7919)
        phase, lo, slow, lo2 = 0.0, 0.0, 0.0, 0.0
        if kind in ("noise", "motor"):
            # Two cascaded one-pole low passes soften the high-frequency edge;
            # subtracting a slow low pass removes the low/DC part of the noise.
            low, high = layer[4:6] if kind == "noise" else (85, 1150)
            alpha = 1 - math.exp(-TAU * high / RATE)
            beta = 1 - math.exp(-TAU * low / RATE)
        for i in range(length):
            t = i / RATE
            if kind in ("noise", "motor"):
                white = next(stream)
                lo += alpha * (white - lo)
                lo2 += alpha * (lo - lo2)
                slow += beta * (lo2 - slow)
                filtered = lo2 - slow
            if kind == "noise":
                env = envelope(t, duration, 0.055, 0.12)
                modulation = 0.72 + 0.16 * math.sin(TAU * 2.3 * t) + 0.12 * math.sin(TAU * 4.7 * t + 1.2)
                value = filtered * modulation * 3.8
            elif kind == "motor":
                hz = layer[4] * pitch
                phase += TAU * hz * (1 + 0.015 * math.sin(TAU * 1.7 * t + variant)) / RATE
                env = envelope(t, duration, 0.09, 0.14)
                value = (0.58 * math.sin(phase) + 0.21 * math.sin(phase * 2.01 + 0.4)
                         + 0.11 * math.sin(phase * 3.97 + 0.7) + 0.10 * filtered * 3)
                value *= 0.85 + 0.15 * math.sin(TAU * 3.1 * t + 0.5)
            elif kind == "tap":
                hz, decay = layer[4:6]
                hz *= pitch
                env = envelope(t, duration, 0.009, 0.045) * math.exp(-t / decay)
                value = (0.64 * math.sin(TAU * hz * t) + 0.24 * math.sin(TAU * hz * 2.37 * t)
                         + 0.12 * math.sin(TAU * hz * 4.11 * t))
            else:
                raise ValueError("Unknown layer " + kind)
            signal[begin + i] += gain * env * value
    # A source-wide fade and a mean correction keep exports centred and their
    # first/last samples silent. These are one-shots, deliberately not loops.
    mean = sum(signal) / n
    for i in range(n):
        signal[i] = (signal[i] - mean) * envelope(i / RATE, n / RATE, 0.035, 0.08)
    rms = math.sqrt(sum(v * v for v in signal) / n)
    peak = max(abs(v) for v in signal)
    if rms <= 0:
        raise ValueError("Silent recipe " + recipe["id"])
    scale = min(10 ** (recipe["target_rms_dbfs"] / 20) / rms,
                10 ** (recipe["peak_ceiling_dbfs"] / 20) / peak)
    signal = [v * scale for v in signal]
    pcm = [round(v * 32767) for v in signal]
    stats = {
        "frames": n, "duration_seconds": n / RATE,
        "peak_dbfs": db(max(abs(v) for v in pcm) / 32768),
        "rms_dbfs": db(math.sqrt(sum(v * v for v in pcm) / n) / 32768),
        "dc_mean": round(sum(pcm) / n / 32768, 9),
        "maximum_sample_step_dbfs": db(max(abs(a - b) for a, b in zip(pcm, pcm[1:])) / 32768),
        "first_last_pcm16": [pcm[0], pcm[-1]],
        "clipped_samples": sum(abs(v) >= 32767 for v in pcm),
        "seed": seed, "applied_gain": round(scale, 9),
    }
    if stats["clipped_samples"] or stats["first_last_pcm16"] != [0, 0]:
        raise ValueError("Export edge/clipping failure " + recipe["id"])
    if stats["peak_dbfs"] > recipe["peak_ceiling_dbfs"] + 0.02:
        raise ValueError("Peak ceiling exceeded " + recipe["id"])
    if abs(stats["dc_mean"]) > 0.0001:
        raise ValueError("DC offset " + recipe["id"])
    return signal, stats


def wav_bytes(samples, width):
    maximum = (1 << (8 * width - 1)) - 1
    if width == 2:
        data = array.array("h", (round(v * maximum) for v in samples))
        if sys.byteorder != "little":
            data.byteswap()
        raw = data.tobytes()
    else:
        raw = b"".join(int(round(v * maximum)).to_bytes(width, "little", signed=True) for v in samples)
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as output:
        output.setparams((1, width, RATE, len(samples), "NONE", "not compressed"))
        output.writeframes(raw)
    return buffer.getvalue()


def preview(rows):
    cards = []
    for row in rows:
        clips = "".join(f'<div><label>Variation {v["variant"]}</label><audio controls preload="none" src="wav/{html.escape(v["file"])}"></audio></div>' for v in row["clips"])
        cards.append(f'<section><h2>{html.escape(row["title"])}</h2><p>{html.escape(row["description"])}</p>{clips}</section>')
    return ("""<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Phobos machinery: sound candidates</title>
<style>body{font:17px/1.6 system-ui,sans-serif;max-width:850px;margin:36px auto;padding:0 24px;background:#152025;color:#e6ebe6}h1,h2{line-height:1.3}h2{font-size:21px}section{border-top:1px solid #53615f;padding:16px 0}label{display:block}audio{width:min(100%,550px)}a{color:#b3d9bf}button{font:inherit;padding:8px 16px}p{max-width:70ch}</style>
<h1>A little life in the machinery</h1>
<p>18 original sound candidates. Short work sounds, with two variations per family. No operating loops. Nothing here is installed or wired into the game.</p>
<p>Start at your usual listening volume. The files retain headroom; this page starts at 35% playback volume. Actual game loudness still needs a check alongside its normal effects.</p>
<button id="stop">Stop all</button>
<section><h2>Quick comparison</h2><p>All families in the order below, A then B, with a pause between samples.</p><audio controls preload="none" src="review-reel.wav"></audio>
<h2>Sparse workshop sketch</h2><p>A 60-second illustration of occasional work sounds. This is an authored timing example without the game's background audio.</p><audio controls preload="none" src="sparse-workshop.wav"></audio></section>
""" + "\n".join(cards) + """
<script>
const players=[...document.querySelectorAll('audio')];
players.forEach(a=>{a.volume=.35;a.addEventListener('play',()=>players.forEach(b=>{if(a!==b)b.pause()}))});
document.getElementById('stop').onclick=()=>players.forEach(a=>{a.pause();a.currentTime=0});
</script></html>
""").encode("utf-8")


def build():
    source = Path(__file__).read_bytes()
    recipe_data = (PACK / "recipes.json").read_bytes()
    recipes = json.loads(recipe_data)
    exports, rows, signals, chapters = {}, [], {}, []
    reel = [0.0] * round(0.5 * RATE)
    for recipe in recipes["families"]:
        row = {key: recipe[key] for key in ("id", "title", "description", "suggested_consumers", "eligibility", "spacing_seconds")}
        row["clips"] = []
        for variant, label in enumerate(("A", "B")):
            signal, stats = render(recipe, variant)
            filename = recipe["id"] + "-" + label.lower() + ".wav"
            final = wav_bytes(signal, 2)
            master = wav_bytes(signal, 3)
            exports["wav/" + filename] = final
            exports["masters/" + filename] = master
            # Explicitly re-open both encodings and prove frame/rate/channel/size.
            for content, width in ((final, 2), (master, 3)):
                with wave.open(io.BytesIO(content), "rb") as clip:
                    if (clip.getnchannels(), clip.getsampwidth(), clip.getframerate(), clip.getnframes()) != (1, width, RATE, len(signal)):
                        raise ValueError("Invalid WAV " + filename)
            row["clips"].append({"variant": label, "file": filename, **stats,
                                 "wav_sha256": sha(final), "master_sha256": sha(master),
                                 "loop": False, "status": "candidate; owner listening and game mix unverified"})
            signals[filename] = signal
            chapters.append({"file": filename, "start_seconds": round(len(reel) / RATE, 3),
                             "duration_seconds": stats["duration_seconds"]})
            reel.extend(signal)
            reel.extend([0.0] * round(1.2 * RATE))
        rows.append(row)
    exports["review-reel.wav"] = wav_bytes(reel, 2)
    scene = [0.0] * (60 * RATE)
    sketch = [(3, "dismantler-work-a.wav"), (11, "crop-pump-work-a.wav"),
              (21, "reclaimer-work-b.wav"), (33, "wet-processor-work-a.wav"),
              (44, "furnace-work-a.wav"), (55, "bottler-work-b.wav")]
    for when, name in sketch:
        for i, value in enumerate(signals[name]):
            scene[when * RATE + i] += value
    exports["sparse-workshop.wav"] = wav_bytes(scene, 2)
    exports["listen.html"] = preview(rows)
    manifest = {
        "schema_version": 1, "date": "2026-10-06", "status": "unintegrated audio candidates",
        "owner_direction": "Occasional mechanical sounds while equipment works; Codex creates audio and Claude handoff only",
        "provenance": "Codex/ChatGPT-assisted procedural synthesis; no audio model, recordings, native samples or third-party sample assets",
        "copyright": "2026 Phobos A. D'thorga", "license": "MIT; repository LICENSE",
        "provider": "local Python standard library", "provider_job_id": None, "generation_cost": "No paid generation or API calls",
        "sample_rate": RATE, "channels": 1, "runtime_encoding": "PCM16 little-endian RIFF WAV",
        "master_encoding": "PCM24 little-endian RIFF WAV", "audio_model": None,
        "source": "scripts/synthesize-machine-audio.py", "source_sha256": sha(source),
        "recipes_sha256": sha(recipe_data), "clip_count": sum(len(r["clips"]) for r in rows),
        "families": rows, "review_reel": {"duration_seconds": len(reel) / RATE, "chapters": chapters},
        "sparse_workshop": {"duration_seconds": 60, "events": [{"start_seconds": t, "file": n} for t, n in sketch]},
        "exports": [{"path": name, "bytes": len(content), "sha256": sha(content)} for name, content in exports.items()],
        "verification_limits": "Format, peaks, DC, silent edges, hashes and regeneration checked offline. No perceptual listening or Unity/Ostranauts playback test.",
    }
    exports["manifest.json"] = (json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    return exports, manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Regenerate in memory and compare every export without writing")
    parser.add_argument("--restore-retired", action="store_true", help="Explicitly restore the owner-discarded session pack")
    args = parser.parse_args()
    if not args.restore_retired:
        parser.error("This chat's Python audio pack was retired by the owner on 6 October 2026. Use scripts/export-audio-candidates.py for the selected ElevenLabs sounds. Restoration needs a new owner request and --restore-retired.")
    exports, manifest = build()
    for name, data in exports.items():
        path = PACK / name
        if args.check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit("Stale/missing audio candidate: " + path.relative_to(ROOT).as_posix())
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    # Obsolete named exports are reported rather than removed automatically.
    unexpected = {p.relative_to(PACK).as_posix() for folder in ("wav", "masters") for p in (PACK / folder).glob("*.wav")} - set(exports)
    if unexpected:
        raise SystemExit("Unexpected candidates retained; review before archiving: " + ", ".join(sorted(unexpected)))
    print(json.dumps({"status": "verified" if args.check else "generated", "clips": manifest["clip_count"],
                      "sample_rate": RATE, "channels": 1, "export_files": len(exports),
                      "reel_seconds": manifest["review_reel"]["duration_seconds"],
                      "playback_tested": False}, indent=2))


if __name__ == "__main__":
    main()
