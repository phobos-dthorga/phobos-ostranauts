# Public source and release status

The owner authorized making Phobos Ostranauts and its Phobos Scope dependency
public on **25 September 2026**, and selected MIT for original Scope work.
This is a **source publication**, not a stable gameplay release or Workshop launch.

## What is available

Source, original artwork with provenance, research, player guides and build/install
scripts are available for inspection and contribution. There are no installable
GitHub release assets yet. [Getting started](getting-started.md) explains the
player route; [building](building.md) explains the developer route.

Scope is separately maintained and pinned as a Git submodule. Clone recursively.
Its recorder is required by Framework even though recording is disabled by default.
The standalone Rust analyser is not needed during gameplay.

## Provenance that remains unresolved

**Gravy / mrkmg's Auto Navigate 1.2.0** supplies the upstream-derived guidance
adaptation. The [original Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
was rechecked on 25 September 2026; it did not establish an explicit reuse grant.
The package investigation and exact provenance remain in
[the reuse review](auto-navigate-reuse-review.md) and
[third-party notices](../THIRD_PARTY_NOTICES.md). The owner's permissive working
assumption is not verified upstream permission. These portions remain excluded
from our MIT grant. No author contact or approval is claimed.

Public visibility does not resolve this issue. Before binary distribution or
claiming the entire suite is MIT, obtain and record applicable upstream terms or
replace the affected implementation with independently authored work.

## Publication checks and limits

- Review tracked files and all locally reachable branch/tag history, not just
  the latest checkout. `scripts/audit-public-history.py` checks selected credential
  patterns, personal machine paths and prohibited file extensions without printing
  matching contents. Run it separately for Scope. It is not an exhaustive secret
  detector, asset-authorship check or legal clearance.
- Review existing Actions logs/artifacts before making a private repository public;
  GitHub documents the consequences in [its visibility guide](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/setting-repository-visibility).
- Check newcomer links with `python scripts/check-doc-links.py`; this checks
  relative file targets, not remote availability or heading fragments.
- Preserve original and third-party notices, artwork provenance and research
  attribution. Keep saves, game assemblies, extracted assets and local configuration
  outside tracked content. Do not rewrite history or force-push as routine cleanup.

## Before a playable release

Record the exact mod/game/loader versions, relevant gameplay results, remaining
limits and known incompatibilities. Rebuild selected packages using the existing
scripts, verify contents/notices and dependency versions, and supply explicit
installation/update instructions with the release. Keep experimental releases
labelled accordingly. A successful build or source publication is not gameplay
validation, and a local prepared package is not a published release.

## Recorded checks — 25 September 2026

Before this publication checkpoint, the history screen covered 1,453 Ostranauts
and 74 Scope file objects, with no configured-pattern/file-type findings. Scope's
five existing Actions runs (20 log files) had no checked credential/personal-path
matches and no stored artifacts. Ostranauts had no prior Actions runs. This is
limited screening, not a guarantee of absence.

Relative documentation file links passed. Scope's verification passed 21 Rust
tests, 27 recorder checks and 13 cross-language captures, plus HTML/comparison and
CLI failure checks. The Framework build passed 1,954 provider/recovery checks
and 35 performance-adapter checks. Its prepared package includes the full Scope
MIT notice. No installation, save changes or gameplay tests were performed.
