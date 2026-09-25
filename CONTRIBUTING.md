# Contributing

Questions, documentation fixes, translations, reproducible bug reports and code
are welcome. Start with [the player introduction](docs/getting-started.md) or
[developer setup](docs/building.md), depending on what you want to do.

## A small, useful contribution

1. Search [existing issues](https://github.com/phobos-dthorga/phobos-ostranauts/issues).
   Discuss substantial features before a large build; small fixes can go straight
   to a pull request.
2. Fork and create a focused branch. Keep unrelated edits out.
3. Describe the problem, resulting behaviour, checks and remaining limits.
   Screenshots help with UI changes; prefer GitHub-native Mermaid for flowcharts.
4. Run the relevant [checks](docs/building.md#contributing-and-checks).
   Documentation-only contributions do not require the game or compilation.
5. Open a pull request. Expect review and possible revisions; there is no fixed
   review timetable.

Follow [community expectations](CODE_OF_CONDUCT.md). See [SUPPORT](SUPPORT.md)
for gameplay reports and [SECURITY](SECURITY.md) for sensitive vulnerabilities.

## Design and testing

Read [AGENTS.md](AGENTS.md) and [project direction](docs/project-direction.md)
before changing behaviour. Prefer useful working slices. UI presents state and
delegates to checked services. Shared services belong in Framework when concrete
consumers need them; content owns balance. Preserve saved identities, inventories,
in-progress jobs and optional integrations.

Exercise the changed behaviour. Record game/plugin versions for gameplay
observations; builds and synthetic tests are not gameplay tests. Never commit
game files, decompiled source, saves, real captures, credentials or personal
machine paths. Use synthetic fixtures.

## Writing and translations

Player guides describe implemented behaviour; research distinguishes proposals,
observations and assumptions. Use complete messages and stable keys in
[translation catalogs](docs/localization.md), and follow the [brand register](docs/equipment-branding.md).

Name NASA, ESA, original researchers, game documentation and mod authors beside
the claims their work supports, with primary-source links. Separate findings
from inference and authored gameplay balance. Preserve citations and separate
artwork provenance; never imply endorsement.

## Authorship and licensing

Contribute only material you have the right to contribute. Preserve notices and
record third-party licences, adaptations and AI-assisted artwork. Original
contributions follow [LICENSE](LICENSE); excluded work retains its own terms.
Read [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md), especially Auto Nav's unresolved
upstream terms. Do not label those portions MIT.

Forks and community maintenance are welcome within applicable terms. Suggested
credit: “Based on Phobos Ostranauts by Phobos A. D'thorga.” This is a courtesy
request, not an additional licence restriction.
