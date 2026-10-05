# Phobos Spacer Stories

A complete, original story collection for the existing Phobos equipment makers:
company histories, crew life, TV news and adverts, small talk, loading lore, nine
correspondence chains and nine archive documents on data cards.

All stories live in the JSON files under `phobos/PhobosFramework/story/`. The
collection uses the existing add-on loader and requires Phobos Framework 0.110.0.
Entries about other Phobos equipment require the relevant mods individually.
It adds no plugin or equipment definition.

The [authoring record](../../docs/development/spacer-stories-authoring.md) links
every file and explains the story connections, setting evidence, goal actions,
saved records and offline checks. The [draft changelog](CHANGELOG.md) records this
first collection; the repository's licence covers the original Phobos writing, and every package carries a copy as LICENSE.

It is a first-party, data-only Phobos mod: players subscribe to it separately, and
it changes nothing unless installed. Build it with `scripts/build-spacer-stories.ps1`
and install it with `scripts/install-mods.ps1 -Mods SpacerStories`. It is held from
Workshop publication until the owner has reviewed its appearance and pacing in the
game.

For the supported data-file format and review commands, see
[Writing story content](../../docs/writing-story-content.md). Packaging guidance
is in [Publishing a Phobos add-on](../../docs/publishing-an-add-on.md).
