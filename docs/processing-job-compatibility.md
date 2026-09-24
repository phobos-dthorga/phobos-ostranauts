# Processing jobs across recipe updates

Implemented in **Shipbreaker 0.6.1**, using **Phobos Framework 0.6.0**.
This document records the original compatibility mechanism. **Current 0.8.0**
adds the [reclaimer](scrap-reclaimer.md) and revision 2 for new wall jobs. Started
revision-1 jobs still produce their original 11 kg recovered stock and 13 kg
unclassified residue. Reclaimer jobs use their own revision catalog.

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

Recipe identities remain a Shipbreaker concern. Framework 0.8.0 now owns the
shared immutable catalog, job recovery and material checks used by both processors. The
existing Framework batch placement and staged delivery services remain shared;
products are placed before the original panel is retired, with rollback while
the input survives. This is not crash-atomic game saving.

The second actual consumer now shares those mechanics through Framework. Keep
revision identities, input rules and material budgets in the content mod.
Version 0.8.0 requires Framework 0.8.0 and adds the reclaimer's native room-heat
adapter. It adds no generic process registry or chemical system.

Duration is job-specific. Electrical demand still follows the documented
startup settings for all panels; this update does not promise that every balance
setting is saved with each job.

## Verification and next work

The logic checks simulate a newer default alongside revision 1, then exercise
fresh/started selection, zero-progress jobs, old missing durations, corrupt and
unknown records, interruption, blocked output, original residue delivery and
duplicate-completion prevention. Version 0.8.0 also checks the actual revision-2
panel recipe and the reclaimer's separate recipe catalog.

Native integration checks round-trip the game's `JsonItem` condition overrides
and use real output definitions for recipe-specific footprint planning. These
are offline checks, not a Unity session or owner gameplay result. Inspected local
game assembly SHA-256 on 2026-09-24:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
The owner's earlier screenshots report game 1.0.1.4; that is not a fresh runtime
version check.

The operating budget and implementation are now in the [reclaimer guide](scrap-reclaimer.md).
Live connected-workflow and thermal checks remain owner-run work.
