# Phobos Ostranauts

Experimental furniture, machinery and gameplay extensions for **Ostranauts**.

This is a private development workspace. It may become public later, once there
is something worthwhile to share. No playable mod has been implemented yet.

## Scope

- Medical equipment, diagnostics and treatments.
- Production, recycling and life-support machinery.
- Comfort, recreation and crew behaviour.

Start with one useful, playable experiment and build from there. Use native
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
- [Modding findings](docs/modding-notes.md)
- [Contributor guide](CONTRIBUTING.md)
- [Agent instructions](AGENTS.md)

Next: choose one furniture concept, prove its basic interaction in-game, and
record what works before expanding it. Add build tools and automated checks
alongside the first implementation that needs them.

## Licence and attribution

Original project code and documentation are MIT licensed; see [LICENSE](LICENSE).
Keep third-party asset licences and attribution with the relevant assets.
Game files, saves and extracted assets are not distributed in this repository.

Independent community project by Phobos A. D'thorga (phobos-dthorga).
Not affiliated with or endorsed by Blue Bottle Games or Kitfox Games.
