# Project direction

## Intent

Build useful portable equipment, furniture and machinery for Ostranauts. The owner clarified on
2026-09-20 that the medical ambition is a broad collection of health devices,
not a single diagnostic appliance. The medical ambition is an integrated shipboard
system: equipment for assessment, stabilisation,
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

The owner selected **[Phobos Approach Assist](limited-autopilot.md)** as the first
mod on **2026-09-20**. The name is provisional; the chosen direction is a limited
autopilot nav module using existing ship sensors, with later capabilities
supported by additional shipboard hardware.

The first playable loop is one installed module, one firmly detected nearby
contact, a capped RCS approach and braking outside docking clearance, followed
by manual docking. It must consume actual fuel and relinquish control on manual
input, lost tracking or invalid equipment/power state. Treat inadequate braking
fuel explicitly. Exact speeds, distances and acceleration limits require testing.

Begin with a test-console integration and a short controlled native RCS burn.
Prove sensing, control ownership, fuel use and interruption before adding the
full approach sequence. Verify the connected loop in a separate test save,
including pause, fast-forward, UI closure and save/reload. The
[P0 integration prototype](approach-assist-prototype.md) now builds and passes
controller checks; the owner will perform its in-game tests. Full approach and
braking remain outstanding.

Use placeholder artwork until behaviour works. Introduce extra hardware and
shared services as working features establish the need. No custom sensor family
or shipwide network framework is required for this milestone.

## Other research and later ideas

Medical and portable-power research remains available for later features.
The [research evidence map](medical-research.md) records native definitions;
[runtime findings](medical-runtime-findings.md) trace selected implementation
paths, and [next steps](medical-next-steps.md) preserve outstanding experiments.
Handheld and installed equipment remain part of the broader project direction.

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
