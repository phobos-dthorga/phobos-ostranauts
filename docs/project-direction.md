# Project direction

## Intent

Build useful portable equipment, furniture and machinery for Ostranauts. The owner clarified on
2026-09-20 that the medical ambition is a broad collection of health devices,
not a single diagnostic appliance. Treat an integrated shipboard medical system
as the leading design direction: equipment for assessment, stabilisation,
treatment, medication, clinical support and recovery.

The owner further clarified that handheld and portable equipment should receive
slightly greater emphasis, with installed counterparts where they provide useful
advantages. Portable devices favour rapid assessment at the patient's location;
installed equipment can offer deeper diagnosis, sustained monitoring and more
capable treatment at the cost of space, infrastructure, setup and mobility.
Battery-powered equipment and realistic battery management are explicit interests.
Research native batteries, consumption and charging alongside the medical systems.

The owner's interests include interacting physiological systems, realistic
medication outcomes, lasting consequences and meaningful ship equipment.
Industrial and recreational ideas remain in scope, including their connections
to medical supplies, life support and rehabilitation.

The [medical-system vision](medical-system-vision.md) develops this direction.
Its individual devices, new physiological mechanics and first connected group
are proposals, not selected features or verified engine capabilities.

## First milestone

The immediate work is research: map existing health and portable-power behaviour,
identify extension points and unknowns, and establish what needs in-game testing.
The [research evidence map](medical-research.md) records native definitions;
[runtime findings](medical-runtime-findings.md) trace selected implementation
paths, and [next steps](medical-next-steps.md) define the remaining experiments.
Research rounds should resolve concrete design or implementation
questions; they do not need to produce a new device every time.

One object with one working interaction in a test save remains a useful
technical checkpoint. A handheld is equally valid and reflects the owner's
portable-equipment priority. This does not define the scope of the intended product.
Choose that checkpoint for its role in a coherent medical care loop, then prove
the connected devices needed to make that loop useful.

The first object and care loop are not yet selected. Use placeholder artwork
until behaviour works. Expand power, resources, persistence, crew use and final
artwork as the chosen design requires. Design the relationships between devices
before implementing them, but introduce shared services only as working features
establish the need.

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
- Candidly reassess every Ostranauts mod idea, across all categories. If evidence
  shows weak gameplay value, unsuitable engine boundaries or disproportionate
  maintenance, say so and recommend a narrower approach or another direction.
  Previous effort is not a reason to continue an unconvincing idea.
