# Phobos washer motor and liquid-pump sounds — elevenlabs.io

**Eight distinct variations, A–H**, saved on 6 October 2026. The owner selected
the A–D ElevenLabs previews: a washing machine in good working order, blending
its electric motor with a liquid pump. E–H are four additional variations of the
same prompt. The owner requires loopable audio; new variations, exact gain and
equipment assignments remain for owner listening.

Open [listen.html](listen.html) to hear each prepared WAV repeat. All eight
**loop WAVs** are 5.75 seconds, mono, 44,100 Hz, signed PCM16 little-endian RIFF.
Nothing is wired into a mod, installed or tested in Ostranauts.

| Variation | Untouched ElevenLabs download | Loop WAV for Claude |
| --- | --- | --- |
| A | [MP3](originals/washer-motor-pump-a.mp3) | [Loop](loops/washer-motor-pump-a.wav) |
| B | [MP3](originals/washer-motor-pump-b.mp3) | [Loop](loops/washer-motor-pump-b.wav) |
| C | [MP3](originals/washer-motor-pump-c.mp3) | [Loop](loops/washer-motor-pump-c.wav) |
| D | [MP3](originals/washer-motor-pump-d.mp3) | [Loop](loops/washer-motor-pump-d.wav) |
| E | [MP3](originals/washer-motor-pump-e.mp3) | [Loop](loops/washer-motor-pump-e.wav) |
| F | [MP3](originals/washer-motor-pump-f.mp3) | [Loop](loops/washer-motor-pump-f.wav) |
| G | [MP3](originals/washer-motor-pump-g.mp3) | [Loop](loops/washer-motor-pump-g.wav) |
| H | [MP3](originals/washer-motor-pump-h.mp3) | [Loop](loops/washer-motor-pump-h.wav) |

The original six-second provider MP3s are preserved byte-for-byte; they are
**not lossless masters**. A–D were generated with looping disabled, E–H with
looping enabled. All eight loop WAVs were prepared from those originals with
channel averaging, a 250 ms circular crossfade and uniform DC removal. The
overlap shortens each repeat to 5.75 seconds. No silence, per-cycle edge fades,
loudness normalisation, pitch change or synthesized sound is added.

Both joins use originally adjacent source samples. Checks find no clipped
samples, no silent boundary and wrap steps below each file's ordinary 99th
percentile sample step. This is offline signal evidence; listen over several
repeats for a noticeable swell or repeating motif, then review in the game mix.
B, F and G are measurably louder than some other variations.

At the owner's request, the earlier six-second, faded A–D one-shot WAVs were
removed from the `wav/` folder. Their former paths and hashes remain in the
[manifest](manifest.json) for provenance; the exporter will not recreate them.
Claude should use **loops/** and fade playback gain only at machine start/stop,
never each repeat.
See the [Claude handoff](../../../docs/development/machine-work-sounds-handoff.md).

The [manifest](manifest.json) keeps prompts, settings, provider IDs, hashes,
measured levels/boundaries and the reproducible export recipe. The new E–H
round cost **240 credits**; both washer rounds cost **480 credits**. Including
the earlier ratchet (80) and pink-noise (120) previews, this chat used **680
credits**. Local preparation adds no generation charge.

Generated with **ElevenLabs — elevenlabs.io**. See [LICENSING.md](LICENSING.md):
these generated audio files do not inherit the repository's MIT licence.
This is fictional machine sound design, not a scientific acoustic model or
a recording of a particular washing machine.

Rebuild from the saved MP3s with
`python scripts/export-audio-candidates.py --write --ffmpeg <executable>`;
verify hashes, format, levels and joins without changing files with
`python scripts/export-audio-candidates.py --check`.
The exporter prepares provider output; it does not synthesize new sounds.

The owner asked to remove only the procedural audio made earlier in this chat.
[retired-session-audio.json](retired-session-audio.json) records the 38 removed
WAVs from commit b8331d5e. That earlier removal is separate from today's four
deleted one-shot exports. The older Shipbreaker completion cue is preserved.
