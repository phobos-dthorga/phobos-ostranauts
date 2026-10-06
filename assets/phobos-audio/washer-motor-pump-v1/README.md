# Phobos washer motor and liquid-pump sounds — elevenlabs.io

Selected by phobosgekko on **6 October 2026** after auditioning the four
ElevenLabs previews: a washing machine in good working order, blending its
electric motor with a liquid pump. The owner retained **all four variations**;
no individual winner or in-game mix has been selected.

These are six-second sound files for Claude's integration work. Nothing is
wired into a mod, installed or tested in Ostranauts.

Open [listen.html](listen.html) to compare the saved originals and prepared WAVs.

| Variation | Untouched ElevenLabs download | Prepared WAV |
| --- | --- | --- |
| A | [MP3](originals/washer-motor-pump-a.mp3) | [WAV](wav/washer-motor-pump-a.wav) |
| B | [MP3](originals/washer-motor-pump-b.mp3) | [WAV](wav/washer-motor-pump-b.wav) |
| C | [MP3](originals/washer-motor-pump-c.mp3) | [WAV](wav/washer-motor-pump-c.wav) |
| D | [MP3](originals/washer-motor-pump-d.mp3) | [WAV](wav/washer-motor-pump-d.wav) |

The original provider files are MP3, **not lossless masters**. WAV copies are
mono, 44,100 Hz, signed PCM16 little-endian RIFF. Export adds a 25 ms fade-in and
80 ms fade-out to prevent abrupt sample edges. It applies no loudness
normalisation, pitch change, synthesis or additional model generation.
All WAVs have zero first/last samples and no clipped samples. Variation B has
higher measured RMS than the others; Claude should review the playback gain.

The [manifest](manifest.json) keeps the exact prompt, model settings, provider
flow/node/session/generation IDs, original and WAV hashes, measured properties,
credit costs and export recipe. Generating the selected four cost **240 credits**.
The ratchet and pink-noise preview rounds cost 80 and 120 credits respectively:
**440 credits total for this chat**. Saving and converting these files caused
no further model generation or credit spend.

Generated with **ElevenLabs — elevenlabs.io**. See [LICENSING.md](LICENSING.md):
these generated audio files do not inherit the repository's MIT licence.
The request is a fictional machine sound design, not a scientific acoustic
model or a field recording of a particular washing machine.

The [Claude handoff](../../../docs/development/machine-work-sounds-handoff.md)
describes eligible work, quiet local playback, decoding bounds, optional
consumers and owner listening checks. These files were generated with looping
disabled and exported with fades; they are **not seamless loops**.

To rebuild WAVs from the retained MP3s, run
`python scripts/export-audio-candidates.py --write --ffmpeg <executable>`.
Verify without changing files with
`python scripts/export-audio-candidates.py --check`.
This exporter does not create sounds; it decodes the saved provider output.

The owner explicitly asked to delete the Python audio samples made earlier in
this chat. [retired-session-audio.json](retired-session-audio.json) lists the 38
removed WAVs and their hashes from commit b8331d5e. The older Shipbreaker
completion cue was preserved.
