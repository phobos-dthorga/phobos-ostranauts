# Changelogs and Steam Workshop publication records

For offline upload staging, private VDF drafts, saved item IDs and verification,
see [Workshop upload preparation](workshop-upload-preparation.md).

Owner memorandum, **25 September 2026**: every Ostranauts mod maintains its own
changelog, current Workshop page draft and generated Steam-formatted notes for
each dated release entry. This is required alongside feature and maintenance
work, not deferred until upload. It does not publish anything automatically.

## Files for each mod

| File | Purpose | Edit directly? |
| --- | --- | --- |
| `mods/<ModId>/CHANGELOG.md` | Authoritative Markdown history and pending changes | Yes |
| `workshop/<ModId>/page.bbcode` | Current copy-ready Steam description, including title, version, publication status, requirements and limits | Yes; version field also follows the constants updater |
| `workshop/<ModId>/releases/<version>.bbcode` | One Steam-formatted changelog document per dated version | No; regenerate from the main changelog |

See the [Workshop draft index](../workshop/README.md). All six existing mods have
initial **Draft** baseline entries. Their dates record preparation, not release
dates, and do not reconstruct an unsupported history of earlier releases.
Manufacturing is held as a
scaffold. Auto Nav is held for unresolved upstream provenance before distribution.
Phobos Scope is a separate toolkit and is outside this Workshop inventory.

## Routine maintenance

1. Update the owning mod's changelog as behaviour, fixes, balance, requirements,
   controls, saved-state effects or known limits change. Use Unreleased while
   changes are not assigned to a candidate version.
2. Update the page draft to describe the actual current scope, dependencies,
   installation, save behaviour and support. Preserve direct author/research
   attribution and distinguish gameplay choices from scientific findings.
3. Use the [constants updater](updating-constants.md) for a version change. Its
   page-version target does not author the changelog for you: add a dated Draft
   entry for that new version, moving the relevant Unreleased notes into it.
4. Preview and generate the corresponding release document. Commit source and
   generated notes together with the related change, following the current Git policy.
5. Run the all-mod check before a checkpoint or publication. CI enforces the
   presence of each mod's records, matching metadata/page versions, valid headings
   and the exact generated contents of all dated entries.

```powershell
# Preview one candidate; no writes. Omit --version for all dated entries.
python scripts/workshop-release-notes.py --mod Agriculture --version 0.5.0

# Generate one candidate document, or refresh every dated version for all mods.
python scripts/workshop-release-notes.py --mod Agriculture --version 0.5.0 --write
python scripts/workshop-release-notes.py --write

# Read-only verification suitable for agents and CI.
python scripts/workshop-release-notes.py --check --format json

# Also require changed mod source/data/art/translations to include both records.
python scripts/workshop-release-notes.py --check --base HEAD --format json
```

The script requires Python 3.10+ and its standard library only. `--mod` accepts
the full native ID or short name and can be repeated. No selection means all
mod folders with native metadata, including future additions. `--version` requires
one selected mod. Unreleased is never exported as a release.

Default mode previews missing/changed outputs. `--write` regenerates them and
checks the result; an identical repeat changes nothing. `--check` fails for
missing or stale outputs and never writes. JSON schema version 1 reports
`status`, `checkedReleases`, and `changedFiles` with relative paths and SHA-256
hashes; previews include the proposed BBCode. Errors report `status: error` and
`error`. Exit codes: 0 success, 1 validation/write failure, 2 CLI syntax error.
CLI syntax errors use the normal stderr usage message.

CI also compares each push/PR with its base commit: changes under a mod's
`src/`, `mods/` or `translations/` directory must include edits to both its
changelog and page draft. Local `--base HEAD` includes untracked new records.
This is a presence check, not an assessment of prose accuracy; write a meaningful
summary/revision rather than changing whitespace to satisfy it. Docs-only or
shared tooling changes do not automatically require every mod's page to change.

All selected mods are preflighted before writing. Writes replace individual
generated files, not source changelogs. This is not a crash-atomic batch: after
interruption, rerun the generator and check. Do not concurrently edit the source
or generated files. Orphan version files are reported, not silently deleted.

## Authoring format

Use these exact second-level headings in the main changelog:

```markdown
# Phobos Example changelog

## [Unreleased]

No additional changes recorded.

## [0.2.0] - 2026-09-25 - Draft

### Added

- Describe a real player-visible change.

### Compatibility and limits

- Explain changed dependencies or saved-state behaviour.
```

Supported release content: paragraphs, `###` headings, flat `- ` bullet lists,
plain **bold** spans and Markdown links to absolute HTTP(S) URLs. Keep each bullet
on one source line. Tables, images, nested lists, code, raw HTML/BBCode and other
unsupported markup fail validation rather than being silently flattened. Use
plain words for commands or link to their detailed guide.

Use `Released` only for an actually published release. Replace its Draft date
with the actual publication date and regenerate. Earlier released entries remain
in the authoritative changelog; subsequent versions get separate files. Corrections
to earlier notes are explicit reviewed edits to their source, never silent deletion
or hand-editing the generated file.

## Steam formatting and publication

Valve documents `[h1]`, `[h2]`, `[b]`, `[list]`, `[*]` and `[url=...]` in
[Steam Community Text Formatting](https://steamcommunity.com/comment/Recommendation/formattinghelp).
The generator uses that conservative subset; the page validator also permits
headings through h3, italic and underline. These are text drafts, not screenshots
or a claim that the live Workshop editor has been tested. Preview the exact text
in Steam before submission; render/layout differences may need adjustment.

Copy a mod's page document into its description editor and its version document
into the corresponding change-note editor. The page title, required-item links,
tags, visibility and preview image also need review in Steam's separate fields.
Do not invent item IDs or dependency links while the items do not exist. Once an
item is published, record its actual URL and current status in the page document,
and replace the pre-publication/download wording with verified instructions.
Artwork records remain in [Workshop artwork provenance](../assets/workshop/README.md).

Before uploading, resolve distribution terms, check exact package versions,
and run `python scripts/check-mod-layout.py --packages` after building all mods.
This checks that every mod retains Git-tracked native `data/` content in its
source, prepared folder and ZIP. Empty folders do not survive Git downloads;
plugin-only definitions still need a tracked file such as Agriculture's
`data/README.md`. Preserve that directory in the final Workshop upload content.
GitHub source ZIPs are source code, not installable release packages.
Also review
required items, save compatibility and known gameplay limitations. Generated
notes do not confer reuse permission or certify the mod. See
[public-release readiness](public-release-readiness.md) and
[third-party notices](../THIRD_PARTY_NOTICES.md).

The checks cannot judge whether prose accurately explains every code change.
Maintainers must review content whenever features, dependencies or limitations
change. Extend the parser, tests and this guide together if another formatting
construct becomes necessary.
