# Phobos intermittent machinery sounds: first candidate pack

Created 6 October 2026 for phobosgekko, with Codex/ChatGPT-assisted procedural
synthesis. **18 original work sounds in nine families, two variations each.**
No recordings, native game samples, third-party samples, PixelLab audio or audio
model outputs. No API or paid generation was used. Original project work under
the [MIT licence](../../../LICENSE), copyright 2026 Phobos A. D'thorga.

These are **unintegrated candidates**, not selected artwork, a mod release or
in-game listening-validated audio. None loops. The owner chose occasional
mechanical sounds while equipment works; Claude is to handle wiring.

Open [listen.html](listen.html) in a browser to compare each family, or play the
[44.5-second comparison](review-reel.wav) and [60-second sparse workshop sketch](sparse-workshop.wav).
The comparison runs through the families below, A then B. The sketch places six
sounds at 3, 11, 21, 33, 44 and 55 seconds; it has no game background track.
The browser starts at 35% playback volume. WAVs retain the authored amplitude.

| Family | Variation A | Variation B |
| --- | --- | --- |
| D4 dismantler | [A](wav/dismantler-work-a.wav) | [B](wav/dismantler-work-b.wav) |
| R4 reclaimer | [A](wav/reclaimer-work-a.wav) | [B](wav/reclaimer-work-b.wav) |
| F6 furnace | [A](wav/furnace-work-a.wav) | [B](wav/furnace-work-b.wav) |
| Powered coolant pump | [A](wav/coolant-pump-work-a.wav) | [B](wav/coolant-pump-work-b.wav) |
| Wet processor | [A](wav/wet-processor-work-a.wav) | [B](wav/wet-processor-work-b.wav) |
| W2 crop water supply | [A](wav/crop-pump-work-a.wav) | [B](wav/crop-pump-work-b.wav) |
| Corker-2 bottler | [A](wav/bottler-work-a.wav) | [B](wav/bottler-work-b.wav) |
| G4 interior actuator | [A](wav/grabber-work-a.wav) | [B](wav/grabber-work-b.wav) |
| ML-2 interior cabinet | [A](wav/laser-cabinet-work-a.wav) | [B](wav/laser-cabinet-work-b.wav) |

Use `wav/` PCM16 files for possible runtime integration and retain `masters/`
PCM24 exports untouched. Both are mono, 44,100 Hz; individual clips are 0.9 to
1.6 seconds. Source peaks stay at approximately -20 dBFS or lower; these measurements do not
establish perceived loudness or fit with native alerts. No perceptual listening
or Unity playback test was performed by Codex. G4/ML-2 interior acoustic placement
and powered-cooling eligibility specifically need verification before wiring.

[Claude handoff](../../../docs/development/machine-work-sounds-handoff.md) contains
the priorities, candidate meanings, current source boundaries, proposed spacing,
mixing constraints, optional later ideas and owner listening checks.

[recipes.json](recipes.json) retains the authored layers/seeds; [manifest.json](manifest.json)
retains source/recipe/export/master hashes, durations, peaks, RMS and validation
limits. Regenerate with `python scripts/synthesize-machine-audio.py`; verify
without writing with `--check`. Python standard library only. The original
[completion cue](../../phobos-shipbreaker/audio/README.md) remains unchanged.
