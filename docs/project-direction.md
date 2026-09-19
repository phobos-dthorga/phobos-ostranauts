# Project direction

## Intent

Build useful new furniture and machinery for Ostranauts. Medical, industrial and
recreational ideas are all in scope; the first concrete feature is not selected.
The owner's interests include interacting physiological systems, medication,
lasting consequences and meaningful ship equipment.

## First milestone

One placeable object with one working interaction, demonstrated in a test save.
Use placeholder artwork until the interaction works. Expand power, resources,
state handling, crew use and final artwork as the chosen design requires.

## Structure as implementation arrives

- `mods/`: independently usable native data packages and distributable sprites.
- `src/`: C# plugins and shared services where required.
- `assets/`: original artwork sources and provenance.
- `scripts/` and `tests/`: tooling and checks justified by actual implementation.
- `.local/`: ignored game references, extracted research and test material.

These directories are created when needed. No runtime stack, framework dependency,
release date or implementation time budget is fixed by this initial repository.

## Decisions already made

- Private GitHub repository initially; publication requires an explicit decision.
- MIT licence for original code and documentation, following Republic Observatory.
- Data and code can coexist; choose the smallest maintainable implementation.
- AI-assisted artwork is welcome, with in-game visual checks and recorded provenance.
