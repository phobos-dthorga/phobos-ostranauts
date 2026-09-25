# Project direction

## Intent

The owner clarified on **2026-09-23** that the overarching ambition is equipment
and systems that let a crew **live in the nastiness of space for longer,
potentially indefinitely**. Extended habitation and the ability to maintain a
ship away from stations give the project's medical, industrial, life-support
and navigation interests a shared purpose.

Build useful equipment and meaningful interactions for Ostranauts. The owner clarified on
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

## Long-term habitation and endurance

Use this question when choosing work: **what currently forces the crew to return
to port, abandon a ship or die, and how would this feature extend their options?**
The answer should identify a practical need and a useful improvement the owner
can encounter and assess during play. Longer independent voyages, recoverable
failures and a habitable ship are the intended benefits.

The following are design areas to investigate as those needs arise, not verified
engine capabilities or a commitment to implement every system:

| Area | Contribution to continued habitation |
| --- | --- |
| Air and water | Recover usable supplies, service treatment equipment and replenish losses. |
| Materials and maintenance | Recover useful stock from salvage, repair equipment and make appropriate replacement parts or consumables. |
| Energy and heat | Supply useful work while accounting for fuel, storage and a path for waste heat. |
| Crew health and daily life | Support food supply, treatment, recovery, rest and the social needs of a long voyage. |
| Damage and safe operation | Detect problems, manage leaks or failures, and reduce avoidable damage through useful controls and assistance. |

For design purposes, indefinite operation means the possibility of continuing
through maintenance and access to replenishment, including scavenged or acquired
feedstocks. Recovery processes must retain material accounting; energy cannot
replace missing material. Losses, worn components and unavailable ingredients
can remain meaningful constraints. The ambition does not establish that the
current game or these prototypes support indefinite survival.

Shipbreaking contributes by supplying the maintenance and fabrication chain.
Its next useful outputs should address actual repair or life-support needs in
the owner's installed game. The first panel-processing recipe is an initial
material-recovery step; it does not yet provide a complete self-maintenance loop.
Existing mods may already supply much of the downstream use, so inspect and
extend them before adding parallel systems.

Keep features independently useful and build connections where they solve a
concrete need. The current shipbreaker work remains the immediate priority;
the wider endurance direction guides later choices as the owner encounters them.

On **2026-09-24**, the owner also requested asteroid resource types that can
replenish life support. The [asteroid endurance research](asteroid-life-support-research.md)
prioritises existing water ice/hydrates, then oxygen recovery, with new
nitrogen-bearing and phosphate/salt-bearing feeds where useful. Acquisition stays
with native tethered asteroid mining. These are proposed replenishment routes,
not a claim that food, atmosphere or all maintenance supplies are already closed
loops. The [processing study](shipbreaking-material-processing-research.md) connects
them to shredding, separation, finite transport and retained waste.

On **2026-09-25**, the owner selected **Phobos Agriculture** for research and
design: a separate Framework-dependent content mod beginning with potatoes and
lettuce, crew tending and managed growing equipment. The
[research report](agriculture-research.md), [first-slice specification](agriculture-first-slice.md)
and [endurance roadmap](agriculture-roadmap.md) connect food production to finite
water, nutrients, electrical supply and heat. NASA and ESA references are explicit;
accelerated growth and candidate yields remain authored gameplay balance.
Ship's Water, Shipbreaker and future asteroid processing have optional proposed
links. PixelLab is the preferred plant-sprite candidate, with a small pilot before
production. This research does not implement a growing mod or authorize paid
generation, and does not establish indefinite survival.

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

On **2026-09-23**, the owner selected **powered shipbreaking for immediate
research**, while retaining all five [fusion-industry ideas](fusion-industry-roadmap.md).
Research should follow relevant encounters during play so the owner can test
what is proposed. The selected sequence is onboard processing first, external
cutting later, including research into the latter's autopilot/positioning needs.
This does not mark Approach Assist's unfinished guidance as complete or authorise
changes to the running game. See the [shipbreaking findings](powered-shipbreaking-research.md).
The owner subsequently authorised the [first shipbreaker build](shipbreaker-first-build.md),
using its intended physical footprint and capacity from the outset, with reasonable
end-user settings and console commands. Its build and offline checks pass;
gameplay verification remains outstanding.

The refreshed [mod inventory](mod-extension-survey.md) finds installed Crafting
Framework, Salvage Workshop and Common Sense logistics, among other useful
foundations. Prefer dependent add-ons and optional integrations over repeated
implementations. The owner explicitly welcomes Steam Workshop dependencies and
sets permissive licensing as the working assumption unless restrictions are
directly noted. Follow explicit terms and distinguish an assumption from a
verified licence; preserve upstream attribution.

The owner set aside the **locator app and sensor network** on 2026-09-20 because
the existing cargo manifest solves the immediate need and is already being used.
Its immediate availability is the main benefit; our realism additions do not
currently justify developing another solution. This is not a blanket rejection
of overlapping mods. Preserve the [research](locator-research.md) and
[historical experiment brief](locator-next-steps.md); do not resume that proposal
without a new decision. [Alternative PDA cartridges](pda-cartridge-ideas.md)
remain possible connections to medical, portable-power and machinery interests.

A later possibility is a [terminal social network](terminal-social-network.md).
The owner means the installed computer used to study Software Engineering and
wants social interactions, new contacts, friends/enemies and quests. Investigate
how online exchanges could affect persistent people and encounters in the game
world. No first conversation, quest or implementation has been selected.

Before implementing that social idea, establish terminal entry points and safe
contact and relationship integration, then select a playable social experiment.
The earlier medical work maps existing health and portable-power behaviour,
extension points and unknowns, and what needs in-game testing.
The [research evidence map](medical-research.md) records native definitions;
[runtime findings](medical-runtime-findings.md) trace selected implementation
paths, and [next steps](medical-next-steps.md) define the remaining experiments.
Research rounds should resolve concrete design or implementation
questions; they do not need to produce a new device every time.

For these later ideas, one working interaction in a test save is a useful checkpoint;
it may extend an existing object such as the terminal. A handheld is equally
valid for equipment experiments and reflects the owner's
portable-equipment priority. This does not define the scope of the intended product.
Choose that checkpoint for its role in a coherent gameplay loop, then prove
the connected devices needed to make that loop useful. For medical equipment this
means a care loop; for instrument software it means useful tests or observations;
for social software it means an exchange with a lasting consequence.

These concepts remain available for later work; they are not the first mod.
Design the relationships between devices before implementing them and expand
power, resources, persistence, crew use and artwork as each selected idea requires.

## Structure as implementation arrives

On **2026-09-24**, the owner selected **Phobos Framework as our own replacement
foundation for Ostranauts Crafting Framework**, with common services reusable by
other authors, especially material transport. Framework/Shipbreaker 0.2.0 now
implement independent construction/machinery; migration limits are recorded in the
[framework decision](phobos-framework.md). This supersedes retaining OCF as our
long-term foundation; optional integrations and reuse of suitably licensed code
remain welcome. It does not expand the project into an entire factory simulation.

- `mods/`: independently usable native data packages and distributable sprites.
- `src/`: C# plugins and shared services where required.
- `assets/`: original artwork sources and provenance.
- `scripts/` and `tests/`: tooling and checks justified by actual implementation.
- `.local/`: ignored game references, extracted research and test material.

These directories are created when needed. Current framework decisions are recorded
above and in the implementation guides; no release date or time budget is fixed.

## Decisions already made

- Private GitHub repository initially; public mod releases are the intended
  destination (clarified 2026-09-23). Changing visibility still requires an
  explicit decision. Preserve third-party provenance and distinguish verified
  terms from working assumptions throughout development.
- MIT licence for original code and documentation, following Republic Observatory.
- Data and code can coexist; choose the smallest maintainable implementation.
- AI-assisted artwork is welcome, with in-game visual checks and recorded provenance.
- Candidly reassess every Ostranauts mod idea, across all categories. If evidence
  shows weak gameplay value, unsuitable engine boundaries or disproportionate
  maintenance, say so and recommend a narrower approach or another direction.
  Previous effort is not a reason to continue an unconvincing idea.
