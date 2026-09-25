"""Reproduce the original, quiet Shipbreaker completion cue (no model/API audio)."""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import wave

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "assets/phobos-shipbreaker/audio"
RATE = 44100
DURATION = 0.28
PEAK = 0.10


def render():
    count = round(RATE * DURATION)
    samples = []
    for i in range(count):
        t = i / RATE
        # One rounded, warm tone: no pitch sweep, impact, noise or second ping.
        envelope = math.sin(math.pi * i / (count - 1)) ** 2
        value = PEAK * envelope * (0.82 * math.sin(2 * math.pi * 440 * t)
                                  + 0.18 * math.sin(2 * math.pi * 660 * t))
        samples.append(round(32767 * value))
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as wav:
        wav.setparams((1, 2, RATE, count, "NONE", "not compressed"))
        wav.writeframes(struct.pack("<" + "h" * count, *samples))
    data = buffer.getvalue()
    metadata = {
        "file": "completion.wav", "provenance": "ChatGPT-authored procedural synthesis; no audio model, recordings or native samples",
        "license": "MIT (original project work)", "sample_rate": RATE, "channels": 1,
        "encoding": "PCM signed 16-bit little-endian WAV", "frames": count,
        "duration_seconds": count / RATE, "frequencies_hz": [440, 660],
        "weights": [0.82, 0.18], "envelope": "sin(pi*i/(frames-1))^2", "amplitude_ceiling": PEAK,
        "peak_dbfs": round(20 * math.log10(max(abs(v) for v in samples) / 32768), 3),
        "rms_dbfs": round(20 * math.log10(math.sqrt(sum(v*v for v in samples) / count) / 32768), 3),
        "wav_sha256": hashlib.sha256(data).hexdigest(),
        "source_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    }
    return data, (json.dumps(metadata, indent=2) + "\n").encode()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check exports without writing")
    args = parser.parse_args()
    data, metadata = render()
    for name, content in (("completion.wav", data), ("completion.json", metadata)):
        path = OUTPUT / name
        if args.check:
            if not path.exists() or path.read_bytes() != content:
                raise SystemExit(f"Stale audio export: {path}")
        else:
            OUTPUT.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
    print("Completion cue verified: 280 ms, mono PCM16, quiet amplitude ceiling 0.10.")
