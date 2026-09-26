# Updating versions and maintained constants

The catalogue also maintains `Shipbreaker.cuttingSeconds` and
`Shipbreaker.cuttingKilowatts` for new G4 jobs (started jobs retain their captured values),
and current runtime/installer dependency minima through
`config/mod-dependency-minimums.json`. Use `--list` for exact keys. Historical
compatibility version gates are not tuning knobs. Regenerate item references and
build affected packages after changing cutter balance or dependencies.

Use **`python scripts/update-constants.py`** from any working directory (give the
script's path if outside the repository). Python **3.10+** is required; there are
no third-party packages. The script locates its own repository and uses the
explicit [constant catalogue](../config/maintained-constants.json).

The default is a **preview**, with no writes. `--apply` performs the update and
reads every registered field back. It does not build, install, commit, push or
publish anything, and never edits saves, local settings or the Scope submodule.

## Common commands

```powershell
# Discover all keys, current values, types and numeric bounds.
python scripts/update-constants.py --list

# Verify that every registered copy agrees with the catalogue.
python scripts/update-constants.py --check

# Preview a version bump across code, project, native metadata and summaries.
python scripts/update-constants.py --set AutoNav.version=0.10.2

# Apply with an optional old-value guard against stale instructions.
python scripts/update-constants.py --set AutoNav.version=0.10.2 --expect AutoNav.version=0.10.1 --apply

# Several independent changes in one preflight/update.
python scripts/update-constants.py --set Shipbreaker.version=0.14.1 --set Shipbreaker.feedSeconds=3 --apply

# Structured output for agents, scripts and CI.
python scripts/update-constants.py --check --format json
python scripts/update-constants.py --set Agriculture.version=0.2.1 --format json
```

These values are examples, not instructions to advance a release. Key names are
case-sensitive; use one `--set` per key. `--list`, `--check` and `--apply` are
mutually exclusive. `--expect` is optional and must name a key also being set.
After a successful apply, an old `--expect` value intentionally fails: refresh
the expectation or omit it for an idempotent repeat.

## What is managed

| Keys | Targets / meaning |
| --- | --- |
| `Framework.version`, `Shipbreaker.version`, `AutoNav.version`, `Agriculture.version`, `Manufacturing.version` | Plugin version, project version, native `strModVersion`; current README table where present, selected explicitly current player-guide summaries, and the Workshop page version |
| The same six mod names with `.gameVersion` | Native `strGameVersion` (four numeric components) |
| `AutoNav.salvageChance` | Default eligible-roll chance, 0–1 |
| `AutoNav.coastTolerancePercent` | Default cruise speed-error tolerance, 0–25 percent |
| `AutoNav.coastEnterFraction` | Default coasting entry fraction, 0.2–0.9 |
| `AutoNav.burnHeadingDegrees` | Default burn heading tolerance, 0.1–10 degrees |
| `Agriculture.stock*`, `Shipbreaker.stock*`, `AutoNav.stockBoards` | Per-offer merchant lot sizes and the current [stock table](merchant-stock.md); integers 1–256 |
| `Shipbreaker.feedSeconds` | Default material feed time, 1–60 seconds |
| `Shipbreaker.feedKilowatts` | Default feed electrical demand, 0.1–100 kW |

The catalogue gives **every exact target**, its expected match count and review
instructions. Numeric limits follow the existing configurable ranges. Numbers
use decimal points, without exponent notation or C# suffixes; the target's `f`
or `d` suffix stays intact. Versions use three numeric components, game targets
four; prerelease suffixes are not supported by the current native version scheme.

Changing a default does not overwrite existing player configuration or captured
jobs/flight preferences. Changing a game target is not evidence of compatibility.
There is no automatic relationship between a mod's own version and its minimum
Framework dependency: inspect requirements when introducing new API use.

## Deliberate boundaries

This is **not global search-and-replace**. Historical reports, release notes,
research, installation compatibility thresholds, dependency minimums and prose
outside the registered summaries are not rewritten. Read the report's `review`
items and update current explanatory text as appropriate. Older package thresholds
must keep their old meaning; do not replace them with the newest dependency floor.

Saved IDs, recipe revisions, material yields, storage capacities, unit conversions,
safety interlocks and other cross-field contracts are not registered as routine
tuning knobs. Changes to those need their own design and compatibility checks.
The script validates types, ranges and registered-copy agreement, not gameplay
semantics or dependency compatibility. It intentionally does not choose versions
or infer which downstream mods need a bump.

## Verification and recovery

Every command checks **all registered targets**. Missing/duplicate matches,
overlaps, unknown keys, out-of-range values, malformed files and mismatched copies
stop the update before writing. Fix genuine source drift and its catalogue value
together after reviewing the diff; there is no force/ignore-drift switch.

Changes are composed before writes. The script preserves surrounding source,
UTF-8 BOMs and line endings, verifies input bytes have not changed, replaces each
file individually, and checks the resulting bytes and field values. On a caught
write/read-back failure it restores files it wrote; it refuses to overwrite a
subsequent concurrent edit. Avoid simultaneous writers. This is not a database
transaction: process termination, disk failure or a rollback failure can require
manual recovery. Review `git diff` and `--check`; never discard unrelated edits.

After applying, inspect the diff and run the affected [build and checks](building.md).
Version-only source changes still require rebuilding before installation. Keep
gameplay results separate from successful builds. This utility's own tests use
temporary fixtures and never update real mod values:

```powershell
python -m unittest discover -s tests -p test_update_constants.py -v
```

## Machine-readable contract

`--format json` prints one JSON object to stdout, with no progress text. Schema
version **1** includes:

- `status`: `listed`, `valid`, `preview`, `applied`, `unchanged` or `error`.
- `verified`: whether preflight or read-back succeeded; `verification` identifies
  `preflight` versus `read-back`. A preview does not claim files were written.
- `checkedConstants`: number checked; `changes`: key, relative path, one-based
  line and old/new value for each changed target. `replacement` is the exact
  inserted text, including a required C# float suffix where applicable.
- `files`: changed relative paths and SHA-256 hashes before/after, including the
  catalogue. In a preview the after hash describes the planned content.
- `review`: follow-up guidance by changed key. `--list` also returns `constants`
  with types, bounds, descriptions, values and target selectors.
- On operational failure: `status: error`, `verified: false`, and `error` text.

Exit **0** means success (including preview/no-op), **1** means validation or
update failure, **2** means command-line syntax error. Argument-parser errors use
the normal usage message on stderr rather than the JSON result schema. Run
`--check --format json` to gate automation, then inspect `status`, `verification`
and exit code after apply. Do not interpret `preview` as deployment success.

## Register another appropriate constant

Edit `config/maintained-constants.json` in a normal reviewed change. Add a stable
`ModName.settingName` key, string `value`, `description`, `review` list and a type:
`version` with `parts`, `number`/`integer` with `min`/`max`, or `boolean`.
Add explicit source targets, each with a repository-relative `path`, regex
`pattern`, named capture `(?P<value>...)` and positive expected `count` (normally 1).
Only the captured value is replaced; include precise surrounding syntax to avoid
matching historical examples or a different field. Targets are confined to
`src/`, `mods/`, `docs/`, `config/` and the root README; filesystem links are refused.
For a C# float target add `"literal": "csharp-float"`: this preserves an existing
`f` suffix or inserts one when changing an integer-looking float to a fraction.

Do not add arbitrary IDs or dependent quantities to avoid doing a migration.
Keep content rules in their owning mod. Add all real authoritative copies and
current summaries that should change, plus a meaningful fixture test where the
new target shape or constraint differs. Run `--check`, preview the new key, and
run the updater tests before relying on it. Existing mods keep their normal
compile/runtime files; this catalogue is a maintenance tool, not a new runtime
configuration dependency.

Workshop page version fields are maintained targets. Add a new changelog entry after a version bump, review the page content and regenerate Steam notes; see [Workshop publication](workshop-publication.md). The updater does not rewrite historical release entries. Only `workshop/<ModId>/page.bbcode` is allowed as a Workshop target, not generated release files.
