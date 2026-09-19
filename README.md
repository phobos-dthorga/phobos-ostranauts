# Phobos Ostranauts

Experimental portable equipment, furniture, machinery and gameplay extensions
for **Ostranauts**.

This is a private development workspace. It may become public later, once there
is something worthwhile to share. No playable mod has been implemented yet.

Our first selected mod is **[Phobos Approach Assist](docs/limited-autopilot.md)**
(working name): a physical nav module for cautious RCS approaches using native
ship sensors. It accelerates, coasts and brakes, leaving final docking to the
pilot. Later capabilities can depend on additional equipment aboard the ship.

## Scope

- Medical equipment, diagnostics and treatments.
- Production, recycling and life-support machinery.
- Comfort, recreation and crew behaviour.
- Navigation assistance and supporting shipboard equipment.

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

- [Project direction](docs/project-direction.md)
- [Medical-system vision](docs/medical-system-vision.md)
- [Health, injury and drug reference](docs/health-reference.md)
- [Limited autopilot module: feasibility and proposed behaviour](docs/limited-autopilot.md)
- [Medical and portable-power research](docs/medical-research.md)
- [Medical and power runtime findings](docs/medical-runtime-findings.md)
- [Next experiments and decisions](docs/medical-next-steps.md)
- [Modding findings](docs/modding-notes.md)
- [Earlier first-object investigation](docs/first-furniture-experiment.md)
- [Contributor guide](CONTRIBUTING.md)
- [Agent instructions](AGENTS.md)

Next: implement and verify the first Approach Assist loop in a separate test save,
starting with module integration, native sensing and a short controlled RCS burn.
Medical research remains documented separately. Add build tools and useful checks
with the first implementation.

## Licence and attribution

Original project code and documentation are MIT licensed; see [LICENSE](LICENSE).
Keep third-party asset licences and attribution with the relevant assets.
Game files, saves and extracted assets are not distributed in this repository.

Independent community project by Phobos A. D'thorga (phobos-dthorga).
Not affiliated with or endorsed by Blue Bottle Games or Kitfox Games.
