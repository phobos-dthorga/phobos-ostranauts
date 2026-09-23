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

Our first selected mod is **[Phobos Approach Assist](docs/limited-autopilot.md)**
(working name): a physical nav module for cautious RCS approaches using native
ship sensors. It accelerates, coasts and brakes, leaving final docking to the
pilot. Later capabilities can depend on additional equipment aboard the ship.

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

Work in small, practical rounds: implement, try it in a test game, then refine.
Keep UI and artwork separate from gameplay logic. Extract shared functionality
when real features need it, and keep unrelated experiments independently usable.

Repository conventions are adapted from
[Republic Observatory](https://github.com/phobos-dthorga/soviet-republic-observatory),
with a smaller setup appropriate to this project's current scope.

## Start here

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
- [Shipbreaker outputs: existing repair and construction uses](docs/shipbreaker-material-uses.md)
- [Shipbreaker expansion: shredding, recycling and asteroid feedstocks](docs/shipbreaking-material-processing-research.md)
- [Hull disposal port: filters, external collection and persistent ejecta](docs/material-disposal-port-research.md)
- [Residue Collector: placement, controls and testing](docs/residue-collector.md)
- [Asteroid resources for water, oxygen, nitrogen and nutrients](docs/asteroid-life-support-research.md)
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
that replaces our OCF/Salvage Workshop requirements. Framework and Shipbreaker
**0.6.0** include physical-item transfers and the approved exterior grabber / wall
chute / indoor processor layout, alongside independent construction at native
tables. The [connected intake](docs/shipbreaker-hull-intake.md) is packaged for
owner gameplay testing. The new [residue collector](docs/residue-collector.md)
receives four residue packets through a checked structural-floor route, with its
own Control Panel and console commands. [Saved endpoint pairing](docs/material-port-pairing.md)
lets players choose one sender and receiver from either end; pairs survive reload
and collection resumes manually. Cargo remains aboard; persistent ejection
and a general conveyor network remain future work.
The economy update adds merchant offers, mass-balanced maintenance and ordinary-save
Auto Nav 0.2.0 using Framework 0.6.0. Installation awaits the game closing. Existing 0.2.1 files remain installed.
Framework 0.2.1 subsequently fixes its false Missing status in the native mod
menu; the owner confirmed the correction after restart on 2026-09-24.
The first 4 x 4 fixture build includes a mass-balanced panel recipe, user settings
and F3 console controls. Build and offline checks pass; owner gameplay checks remain.
External cutting and its positioning/autopilot needs follow later. Other industrial
ideas advance as the owner encounters relevant gameplay and can test them.
Approach Assist prototype testing remains with the owner in a separate test world,
checking module integration, native sensing and a short controlled RCS burn.
Medical experiments, terminal social interactions and alternative PDA cartridges
remain documented as other directions.

## Licence and attribution

Original project code and documentation are MIT licensed; see [LICENSE](LICENSE).
Keep third-party asset licences and attribution with the relevant assets.
Game files, saves and extracted assets are not distributed in this repository.

Independent community project by Phobos A. D'thorga (phobos-dthorga).
Not affiliated with or endorsed by Blue Bottle Games or Kitfox Games.
