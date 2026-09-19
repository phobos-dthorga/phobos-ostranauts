# Phobos Ostranauts contributor instructions

## Working style

- Keep this a practical, small-team project. Prefer a working slice over a
  speculative framework or extensive process.
- Use observed progress to plan rounds; do not invent hour estimates.
- Explain what works, what was checked and what remains uncertain.
- Repository visibility stays private until the owner explicitly requests a change.

## Architecture

- Use native JSON definitions for suitable content and existing behaviours.
- Use C# extensions for behaviour the native data system cannot express cleanly.
- UI code presents state and delegates actions; gameplay services own mutations.
- Extract shared code when concrete features establish a shared need. Avoid
  duplicated business logic and premature generalisation.
- Prefix new game identifiers with `Phobos` and keep them stable once saved games
  can contain them. Document migrations for incompatible changes.
- Distinguish observed engine behaviour from proposed designs and untested assumptions.

## Game and repository boundaries

- Develop in mod folders; do not overwrite the game's original files.
- Use a separate test save for gameplay experiments. Do not directly edit, replace
  or delete the player's real saves without explicit authorisation.
- Do not commit saves, decompiled game source, game assemblies, extracted game
  assets, credentials or personal machine paths.
- Resolve game references from a local path or configuration, not a committed
  machine-specific location. Document third-party licences and asset provenance.
- For private-key generation, conversion or credential entry, provide manual
  instructions to the owner; do not automate it or inspect populated secret fields.

## Verification

- Scale checks to the change. Add useful tests for gameplay rules and persistence;
  do not create tests that merely repeat static documentation.
- For new machinery, check relevant power, interruption, save/reload and
  fast-forward behaviour. Record the game and plugin versions tested.
- Never call a successful build an in-game test. Do not claim untested compatibility.
