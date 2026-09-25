# Original quiet completion cue

`completion.wav` is the first optional watched D4/R4 batch cue, authored with
ChatGPT-assisted procedural synthesis on 25 September 2026 for phobosgekko.
It contains no recording, game sound, PixelLab audio or audio-model output.
Source and export are original project work under the [MIT licence](../../../LICENSE).

Reproduce with `python scripts/synthesize-completion-cue.py`; verify without
writing with `--check`. Python's standard library is sufficient. No network,
API credentials, random seed or paid generation is involved. `completion.json`
records the recipe, format, duration, measured peak/RMS and source/export hashes.
The sample is embedded once into the Framework plugin, so runtime playback requires
no external file loader or network access.

The raw preview is louder than the default in-game level (35% before the native
effects mixer). Do not normalize it to full scale. The source amplitude ceiling
and envelope are deliberate design choices, not hearing-safety certification.
See [behaviour and owner checks](../../../docs/shipbreaker-completion-cue.md).
