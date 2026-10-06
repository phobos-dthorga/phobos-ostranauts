# Original quiet completion cue

`completion.wav` is the first optional watched D4/R4 batch cue, authored with
ChatGPT-assisted procedural synthesis on 25 September 2026 for phobosgekko.
It contains no recording, game sound, PixelLab audio or audio-model output.
Source and export are original project work under the [MIT licence](../../../LICENSE).

Reproduce with `python scripts/synthesize-completion-cue.py`; verify without
writing with `--check`. Python's standard library is sufficient. No network,
API credentials, random seed or paid generation is involved. `completion.json`
records the recipe, format, duration, measured peak/RMS and source/export hashes.
Until Framework 0.119.0 the sample was embedded in the Framework plugin. Since
0.120.0 it ships unchanged as `sounds/completion.wav` beside the plugin, where a
player may replace it (see the [shared completion cues](../../../docs/shared-completion-cues.md)
guide). Playback reads only that local file, with no network access.

The raw preview is louder than the default in-game level (35% before the native
effects mixer). Do not normalize it to full scale. The source amplitude ceiling
and envelope are deliberate design choices, not hearing-safety certification.
See [behaviour and owner checks](../../../docs/development/shipbreaker-completion-cue.md).
