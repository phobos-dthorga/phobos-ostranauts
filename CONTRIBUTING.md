# Contributing

Read [AGENTS.md](AGENTS.md) and the [project direction](docs/project-direction.md).

Keep changes focused on a useful, demonstrable result. For a new feature, describe
the player action, required resources, resulting behaviour and any unresolved
limits. Record actual in-game checks separately from builds or data validation.

Reuse established mod patterns without isolated tests to prove them again.
Concentrate checks on our new behaviour and concrete integration risks; see the
[verification preferences](AGENTS.md#verification).

For dependency breakage or prolonged lack of compatible upstream releases, follow
the [dependency contingency plan](docs/dependency-contingencies.md). Prefer a
narrow fix or maintained successor and plan saved-content transitions before
removing a required provider.

While this repository is private, requested checkpoints use ordinary commits and
direct pushes to `main`. Pull requests and their review/merge workflows are
deferred until the project is public, unless the owner explicitly requests one.
Keep relevant checks and normal Git protections. Public releases and a change of
repository visibility remain separate decisions for the owner.

Keep game data and local research outside tracked source. Use small synthetic
fixtures where tests need representative state. Include licences and provenance
for third-party artwork or code, and note AI assistance when assets use it.

For every mod and research document, name NASA, ESA, original researchers, game
documentation and mod authors beside relevant claims, with direct primary-source
links. Separate scientific findings, observed behaviour, proposals and untested
assumptions; identify accelerated growth, authored yields and simplified chemistry
as gameplay choices. Preserve relevant references in design and release documents
alongside separate artwork/licensing records, and repair citations when revising
related research. See the [standing attribution guidance](AGENTS.md#supporting-research-and-institutional-attribution).

As with Republic Observatory, independent forks and community maintenance are
welcome if the project is shared. Preserve the MIT notice and credit contributors.
Suggested attribution: "Based on Phobos Ostranauts by Phobos A. D'thorga."
That wording is a courtesy request, not an additional licence restriction.
