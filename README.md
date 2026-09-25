# Phobos Ostranauts

Experimental portable equipment, furniture, machinery and gameplay extensions
for **Ostranauts**.

The guiding ambition is **living in hostile space for longer, potentially
indefinitely**: a ship that can sustain its crew, recover useful resources and
maintain its equipment through extended operation away from stations. Medicine,
recycling, life support and navigation contribute to that shared purpose.
See the [endurance direction](docs/project-direction.md#long-term-habitation-and-endurance).

This is a private development workspace with public mod releases intended later.
The original Approach Assist prototype provides a short test pulse. A separate
[Phobos Auto Nav standalone adaptation](docs/auto-navigate-adaptation.md) now
implements approach and braking for ordinary saves and owner-run evaluation; it is not yet
tested in-game. Attribution and upstream licensing uncertainty are recorded in
[third-party notices](THIRD_PARTY_NOTICES.md).

The current equipment suite is **Phobos Framework, Shipbreaker and Auto Nav**.
Approach Assist was the earlier pulse-only experiment and is not installed by
default. Its longer navigation proposal remains historical design material.

## Scope

- Medical equipment, diagnostics and treatments.
- Production, recycling and life-support machinery.
- Comfort, recreation and crew behaviour.
- Navigation assistance and supporting shipboard equipment.
- PDA utilities and installed sensor equipment.
- Computer-based social interactions and their consequences.

The medical ambition is a broad system spanning portable field equipment
and installed shipboard facilities. Handheld devices, realistic battery management
and their relationship with more capable medical facilities are central interests.
Build it through focused research and useful, playable experiments. Use native
game data where it fits and C# plugins where new behaviour needs them.

## Working approach

Work in small, practical rounds: implement, evaluate during owner-run play, then refine.
Keep UI and artwork separate from gameplay logic. Extract shared functionality
when real features need it, and keep unrelated experiments independently usable.

Repository conventions are adapted from
[Republic Observatory](https://github.com/phobos-dthorga/soviet-republic-observatory),
with a smaller setup appropriate to this project's current scope.

## Start here

- [Opt-in performance captures: Phobos Scope commands and analysis](docs/performance-captures.md)

- [Current player guide: acquire, install, load, run and collect](docs/player-guide.md)
- [Automatic material routing: machine inputs, buffers and reject destinations](docs/automatic-material-routing.md)
- [Industrial console and equipment panels: research and text mockups](docs/industrial-control-console.md) — proposed next interface, not yet implemented.
- [Residue composition, destinations and the next processing stage](docs/residue-material-contract.md)
- [Equipment prices, merchants, maintenance and salvage](docs/equipment-economy.md)
- [One-command mod installation and updates](docs/installing-mods.md) — or double-click
  `Install or Update Mods.cmd` after closing the game.
- [Project direction](docs/project-direction.md)
- [Phobos Framework: shared services and OCF independence plan](docs/phobos-framework.md)
- [Phobos Framework: author API guide](docs/framework-author-guide.md)
- [Fusion-powered industry: five ideas and research triggers](docs/fusion-industry-roadmap.md)
- [Powered shipbreaking: feasibility and first observations](docs/powered-shipbreaking-research.md)
- [Shipbreaking follow-up: power, mass balance and the first experiment](docs/powered-shipbreaking-design-findings.md)
- [Shipbreaker first build: installation, settings and console commands](docs/shipbreaker-first-build.md)
- [Ship equipment art study and Shipbreaker visual direction](docs/ship-equipment-art-study.md)
- [Artwork resolution policy: 2x production assets, 4x for small graphics](docs/artwork-resolution-policy.md)
- [Shipbreaker outputs: existing repair and construction uses](docs/shipbreaker-material-uses.md)
- [Shipbreaker expansion: shredding, recycling and asteroid feedstocks](docs/shipbreaking-material-processing-research.md)
- [Hull disposal port: filters, external collection and persistent ejecta](docs/material-disposal-port-research.md)
- [Residue Collector: placement, controls and testing](docs/residue-collector.md)
- [Asteroid resources for water, oxygen, nitrogen and nutrients](docs/asteroid-life-support-research.md)
- [Future chemical tanks, station replenishment and industrial hazards](docs/chemical-storage-and-process-fluids.md)
- [Current mod inventory and extension opportunities](docs/mod-extension-survey.md)
- [Dependency maintenance and fallback plan](docs/dependency-contingencies.md)
- [Medical-system vision](docs/medical-system-vision.md)
- [Health, injury and drug reference](docs/health-reference.md)
- [Limited autopilot module: feasibility and proposed behaviour](docs/limited-autopilot.md)
- [Auto Navigate: reuse, licensing and community practice](docs/auto-navigate-reuse-review.md)
- [Phobos Auto Nav: standalone adaptation, settings and owner testing](docs/auto-navigate-adaptation.md)
- [Approach Assist prototype: build, installation and testing](docs/approach-assist-prototype.md)
- [Medical and portable-power research](docs/medical-research.md)
- [Medical and power runtime findings](docs/medical-runtime-findings.md)
- [Next experiments and decisions](docs/medical-next-steps.md)
- [PDA locator and sensor research](docs/locator-research.md)
- [Locator experiment and design choices](docs/locator-next-steps.md)
- [Alternative PDA cartridge ideas](docs/pda-cartridge-ideas.md)
- [Terminal social network concept](docs/terminal-social-network.md)
- [Modding findings](docs/modding-notes.md)
- [Earlier first-object investigation](docs/first-furniture-experiment.md)
- [Contributor guide](CONTRIBUTING.md)
- [Agent instructions](AGENTS.md)

The locator proposal was set aside because an existing mod already meets the
owner's immediate need and is being used now.
Current development priority: powered shipbreaking and a reusable **Phobos Framework**
that replaces our OCF/Salvage Workshop requirements. Framework **0.9.0** and
Shipbreaker **0.9.0** include physical-item transfers and the approved exterior grabber / wall
chute / indoor processor layout, alongside independent construction at native
tables. The [connected intake](docs/shipbreaker-hull-intake.md) is packaged for
owner gameplay testing. The new [residue collector](docs/residue-collector.md)
receives four residue packets through a checked structural-floor route, with its
own Control Panel and console commands. [Saved endpoint pairing](docs/material-port-pairing.md)
lets players choose one sender and receiver from either end; pairs survive reload
and collection resumes manually. Cargo remains aboard; persistent ejection
and a general conveyor network remain future work.
Shipbreaker 0.6.1 adds [version-aware processing jobs](docs/processing-job-compatibility.md)
so started panels retain their recipe outputs and duration through later updates.
Version 0.8.0 starts new panels on revision 2, supplying the new scrap reclaimer;
already-started revision-1 jobs retain their original outputs.
The economy update adds merchant offers, mass-balanced maintenance and ordinary-save
operation. Auto Nav 0.8.0 adds [RCS docking](docs/auto-nav-docking.md).
Version 0.7.0 added a [rotary instrument panel](docs/auto-nav-instruments.md)
and includes short-range approaches below 5,000 km with closer
arrival settings, improved coasting and corrected panel dragging/sizing. The current
suite uses Framework 0.14.0. Auto Nav 0.10.0 implements the four audit
recommendations: combined RCS throttle budgeting, braking-room admission,
console-specific speed/distance defaults and rare native module salvage. See
[flight profiles and safety](docs/auto-nav-flight-profiles.md). Auto Nav supports
[validated saved-flight restoration](docs/auto-nav-persistence.md) and
[torch-preferred travel and braking](docs/auto-nav-torch.md) with native no-wake
protection and RCS fallback. These are prepared versions; use installer
verification and in-game status commands to determine what is actually installed.
Framework 0.2.1 subsequently fixes its false Missing status in the native mod
menu; the owner confirmed the correction after restart on 2026-09-24.
The first 4 x 4 fixture build includes a mass-balanced panel recipe, user settings
and F3 console controls. Build and offline checks pass; owner gameplay checks remain.
External cutting and its positioning/autopilot needs follow later. Other industrial
ideas advance as the owner encounters relevant gameplay and can test them.
Historical Approach Assist instructions describe its original isolated pulse
experiment; they are not prerequisites or save restrictions for the current suite.
Medical experiments, terminal social interactions and alternative PDA cartridges
remain documented as other directions.

## Licence and attribution

Original project code and documentation are MIT licensed; see [LICENSE](LICENSE).
Keep third-party asset licences and attribution with the relevant assets.
Game files, saves and extracted assets are not distributed in this repository.

Independent community project by Phobos A. D'thorga (phobos-dthorga).
Not affiliated with or endorsed by Blue Bottle Games or Kitfox Games.

Original equipment brands and current model names: [Equipment branding](docs/equipment-branding.md).

Translation catalogs, language settings and contributor guidance: [Localization](docs/localization.md).

Framework/Shipbreaker 0.8.0 adds the [scrap reclaimer](docs/scrap-reclaimer.md):
new identified residue, useful recovered metals, retained rejects, room heat,
normal merchant acquisition and shared saved-job handling. Prepared, not yet gameplay verified.

## Industrial controls (0.10.0)

[Console and equipment panel guide](docs/industrial-console-player-guide.md): a 3 x 3 ship-bound workstation, local Control Panels, automatic grouping, search, Attention and routing. Shipbreaker now requires Framework 0.13.0 and adds [shared observations](docs/shared-console-observations.md). Prepared for owner testing; no in-game validation claimed.
