# Processing jobs across recipe updates

Implemented in **Shipbreaker 0.6.1**, using **Phobos Framework 0.6.0**.
New wall jobs still use **revision 1** and produce the same 11 kg of recovered
stock plus the existing 13 kg mixed residue. No new feedstock or reclaimer is
enabled by this update.

## Saved contract

The actual input panel retains the existing native numeric conditions:

| Saved condition | Meaning |
| --- | --- |
| `PhobosShipbreakerRecipeRevision` | Exact integer revision identifying immutable output definitions, counts and unit masses |
| `PhobosShipbreakerProgress` | Powered seconds already credited to this input |
| `PhobosShipbreakerJobSeconds` | Total duration selected when this job started |

Its native object ID binds that work to that panel. No external save file, item
renaming, automatic cancellation or rewrite of old residue is needed.

Each registered revision has its own immutable product list. New panels select
the current revision; started panels resolve their saved revision independently.
Space planning, product creation, material checks and completion logging use the
job's selected recipe. A change to the default cannot redirect old jobs to new
outputs. Published revisions must stay in the catalog unchanged.

All three fields must be zero for a panel to count as fresh. A revision with zero
progress still means a started job. Revision 1 alone supports the historical
missing-duration case: it uses the original **60 seconds**, regardless of current
settings. Future revisions must not inherit that exception accidentally.

Unknown, fractional or otherwise invalid revisions stop processing and retain
the input and fields. Invalid progress/duration and incomplete records also stop.
Status explains whether to restore a supporting version or explicitly cancel
work. Existing **Cancel work** clears jobs on wall panels inside the processor's
feed, retains the actual panels and refunds no energy. A subsequent Start chooses
the current recipe from zero; it does not convert finished residue.

Before advancing or consuming the input, the service checks that its saved
identity, revision, duration and progress still match the active job. Changes
pause the queue instead of being overwritten by an old session. Reload remains
paused; reading F9/F3 status neither writes job fields nor starts work. Status now
shows the selected revision and uses the same legacy-duration interpretation as
Start.

## Scope and ownership

Recipe selection is currently a wall-processing concern in Shipbreaker. The
existing Framework batch placement and staged delivery services remain shared;
products are placed before the original panel is retired, with rollback while
the input survives. This is not crash-atomic game saving.

Extract the common job mechanics into Framework when the reclaimer supplies the
second actual consumer. Keep revision identities, input rules and material
budgets in the content mod. This round adds no generic process registry, new
Framework dependency version, chemical system or heat simulation.

Duration is job-specific. Electrical demand still follows the documented
startup settings for all panels; this update does not promise that every balance
setting is saved with each job.

## Verification and next work

The logic checks simulate a newer default alongside revision 1, then exercise
fresh/started selection, zero-progress jobs, old missing durations, corrupt and
unknown records, interruption, blocked output, original residue delivery and
duplicate-completion prevention. The second recipe exists only in test fixtures.

Native integration checks round-trip the game's `JsonItem` condition overrides
and use real output definitions for recipe-specific footprint planning. These
are offline checks, not a Unity session or owner gameplay result. Inspected local
game assembly SHA-256 on 2026-09-24:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
The owner's earlier screenshots report game 1.0.1.4; that is not a fresh runtime
version check.

Next, settle the combined reclaimer's machine mass, construction/service bills,
price, duration, power and heat destination. Follow the
[residue material contract](residue-material-contract.md): add the characterised
feed and its useful consumer together, preserve revision-1 jobs and legacy
residue, and enable a new producer only when its consumer is usable.
