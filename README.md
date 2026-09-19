# Phobos Ostranauts

Experimental portable equipment, furniture, machinery and gameplay extensions
for **Ostranauts**.

This is a private development workspace. It may become public later, once there
is something worthwhile to share. No playable mod has been implemented yet.

## Scope

- Medical equipment, diagnostics and treatments.
- Production, recycling and life-support machinery.
- Comfort, recreation and crew behaviour.

The leading direction is a broad medical system spanning portable field equipment
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
- [Medical and portable-power research](docs/medical-research.md)
- [Medical and power runtime findings](docs/medical-runtime-findings.md)
- [Next experiments and decisions](docs/medical-next-steps.md)
- [Modding findings](docs/modding-notes.md)
- [Earlier first-object investigation](docs/first-furniture-experiment.md)
- [Contributor guide](CONTRIBUTING.md)
- [Agent instructions](AGENTS.md)

Next: compare the existing health interface with a useful field-care decision and
measure the native battery loop in a separate test save. Then choose the first
device using the documented shortlist and acceptance checks. Add build tools and
automated checks alongside the first implementation that needs them.

## Licence and attribution

Original project code and documentation are MIT licensed; see [LICENSE](LICENSE).
Keep third-party asset licences and attribution with the relevant assets.
Game files, saves and extracted assets are not distributed in this repository.

Independent community project by Phobos A. D'thorga (phobos-dthorga).
Not affiliated with or endorsed by Blue Bottle Games or Kitfox Games.
