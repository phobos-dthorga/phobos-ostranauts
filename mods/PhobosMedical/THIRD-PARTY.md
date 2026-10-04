# Phobos Medical provenance

Phobos Medical is original Phobos code under the repository licence. It uses
Phobos Framework as a separately supplied dependency; Framework retains its own
upstream adaptation notices.

Native bed, sleep, wound, drag and time-skip behaviour was inspected locally in
[Blue Bottle Games' Ostranauts](https://store.steampowered.com/developer/bluebottlegames/)
1.0.1.5. The mod reuses the game's own sleep chain, Recuperating condition,
chair and sleep actions (cloned at load) and wound and drag systems at runtime by
reference; no game asset, definition or code is copied into the package. Game assemblies and decompiled source are excluded
from distribution. BepInEx and Unity references are resolved from the owner's
installation and are not bundled.

The shipped data packs in `framework/` are original data under the repository
licence; players may copy and adapt them freely. Admission thresholds are authored
gameplay balance on the game's own scales, not clinical values.

## Artwork

The Ward-3 image (`images/phobos/medical/PhobosMedicalBed.png` and its neutral normal map) was
generated on 4 October 2026 by OpenAI's built-in image tool from an original text prompt, selected
by the owner, and registered to the 3 x 5 footprint by `assets/phobos-medical/register-ward3.py`.
No game image was supplied to the generator. The prompt, provider usage and hashes are in
`assets/phobos-medical/requests.json`; provider terms:
[OpenAI Terms of Use](https://openai.com/policies/terms-of-use/).
