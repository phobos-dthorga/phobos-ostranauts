# Pending Shipbreaker construction and saved rooms

Use **Framework 0.24.1** with Shipbreaker **0.19.1 or newer** for the three
identified failures. Framework preserves living saved construction markers,
validates stale grid headers and records dimensions after native save trimming;
Shipbreaker preserves checked loading bounds for its pending equipment.
The owner confirmed the first two fixes on their reported saves. The third,
described below, passes offline checks and awaits owner gameplay confirmation.

Shipbreaker **0.19.1** with Framework **0.21.1** introduced a narrowly scoped
mitigation for atmosphere loss while loading saves with pending Shipbreaker
equipment installation. It preserves the saved grid before the game assigns
rooms and zones. On 26 September 2026, the owner reported that the patched
reload worked well. This confirms the reported case by owner observation;
broader save/reload coverage remains unverified.

## Third incident: dimensions captured before save trimming

The owner's later 26 September report supplied `pg17` (05:33:29 local save
metadata) and `autosave_293_pg17` (05:39:33), and associated the recurrence with
torch use and skipping six hours. The running log records Framework 0.24.0 and
Shipbreaker 0.23.0. Both earlier protections **ran**: the loader retained 58 worn
markers and expanded its grid from 63 by 44 to the header's 64 by 44. Nevertheless,
four duplicate-room lookups and five ghost-room removals followed.

**Observed save/log evidence:** `pg17` consistently uses 64 columns, 44 rows and
origin (-33,-14). In the autosave, all eight room positions, the complete exterior
border and the zone indices instead agree on **63 columns by 44 rows**, with
origin (-32,-14). Its header alone still says 64 columns. The log explicitly
records B-14EA changing from **2816 to 2772 tiles during saving**, then regenerating
the eight room IDs found in that autosave. The seven interior rooms still retain
approximately **101.28–101.39 kPa**. Their gas records have not yet been lost in
the supplied archive.

| Actual saved room lookup, width 63 | Incorrect lookup using header width 64 |
| --- | --- |
| 1111 | 1128 |
| 1120 | 1137 |
| 1410 | 1432 |
| 2297 | 2333 |

All four incorrect indices match the recorded load errors. The earlier Phobos
grid guard contributed to this recurrence by trusting the stale header and
expanding an already-correct 63-column loading grid. The new correction must run
before that guard; simply retaining the worn markers cannot solve this case.

**Locally inspected native cause:** [Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts)
1.0.1.5 `Ship.GetJSON` copies dimensions before `TileUtils.TrimAllSides`, but saves
the origin, item positions, rooms and zones afterwards. If native trimming removes
an empty edge during that save, the archive combines dimensions from one grid
with indices from another. This is a source/log/save diagnosis, not a developer
confirmation or an independently reproduced unmodded gameplay test.

**Torch and accelerated-time evidence:** six loose trash objects along the left
edge disappear between the two saves, including the objects at x=-32 that kept
the extra column in use. The enclosed room tile counts stay the same. Their
removal permits the native trim. The owner reports torch use and a six-hour time
skip; these are relevant conditions, but the available evidence does not establish
which activity removed the trash. Neither torch controls nor time progression
is disabled or changed by this fix.

Framework 0.24.1 has two narrow corrections:

1. Before a full saved load, resolve a smaller grid only when exactly one candidate
   matches the complete single exterior border and every same-ID saved Compartment
   item's world position. Require unique room/anchor IDs, consistent room ownership,
   integral coordinates and in-bounds zones. Repeated native tile entries within
   the same room are preserved. Missing/conflicting evidence or ambiguous grids
   are left unchanged. Correct only the in-memory dimensions, keeping the saved
   origin and all room, gas, zone, item, damage and progress records.
2. After native full-ship save serialization, synchronize the outgoing dimensions
   with the post-trim live grid when its tile count and origin agree. Do not pad,
   trim or move live geometry. Templates, shallow snapshots and inconsistent
   native output keep their original behavior. This prevents the inconsistent
   header from being written by subsequent ordinary saves.

Read-only native DTO audits resolve **63 by 44** for `autosave_293_pg17`, matching
all eight room anchors; `pg17` remains unchanged at **64 by 44**. Tests cover
width/height trimming, repeated application, room/zone preservation, missing or
duplicate anchors, ambiguous factorization, malformed bounds, native hook
signatures and template/shallow exclusions. Repeat the archive audit with:

```powershell
dotnet run --project tests/PhobosNative.Tests -c Release "-p:OstranautsPath=$gamePath" -- $gamePath $repoPath --audit-room-grid $archivePath B-14EA
```

The delivery for this incident updates **Framework only**. The failure log used
Shipbreaker 0.23.0; the local installation had advanced to 0.24.0 by delivery, and
this installer run left that package intact. Both original archives and logs are copied to local ignored
diagnostics; no save is replaced or edited. After restarting, load
**`autosave_293_pg17`** to keep its later progress. Expect `corrected stale saved
grid header from 64x44 to 63x44` before room loading, retained marker health, and
no duplicate-room/ghost-room errors for B-14EA. Verify atmosphere, items, pending
construction and stockpile zones, then save to a new slot and reload. A later
native trim should log `synchronized outgoing saved grid dimensions` when needed.
The unmodified earlier `pg17` remains available. Owner testing of this third fix
is pending; no guarantee is made about already-lost gas or unrelated save defects.

## Second incident: marker wear and zero template health

Later on 26 September 2026, the owner supplied a new `pg15` plus the working
preceding `autosave_285_pg15`. Logs show Framework 0.23.0 and Shipbreaker 0.21.0.
The existing grid guard still runs on other loads; it does not run on the failed
load because native item spawning has discarded the G4 and H4 before it can see
them. The log records **69** `Fully damaged; Skipped loading: Placeholder` lines,
each comparing small damage against `maxH: 0`. Duplicate exterior-room assignments
then appear at indices 187 and 1069, followed by five ghost-room removals.

| Saved evidence | Working autosave, 01:41:56 | New pg15, 01:58:47 |
| --- | --- | --- |
| Saved construction markers | 122 | 115 |
| Separate item damage overrides | 0 | 69 |
| G4 saved damage / maximum | 0.00024176 / 20 | 0.00153847 / 20 |
| H4 saved damage / maximum | 0.00063425 / 20 | 0.00196768 / 20 |

Times above are local save metadata. Local inspection of **Blue Bottle Games'
Ostranauts 1.0.1.5** saving/loading code identifies the boundary: item damage is
written separately only when it is **greater than 0.001**. On reload, the native
early destruction check compares this override against the generic `Placeholder`
definition, whose maximum health is zero. The saved marker conditions instead
record maxima of **15** for the 67 affected vanilla wall/floor markers and **20**
for G4/H4. All 69 records are alive and far below their own limits. This explains
why a slightly older save reloads successfully without a placement change. This
is local code/save/log evidence, not an independent unmodded gameplay reproduction
or a developer-confirmed diagnosis.

Framework 0.23.1 corrects only that early false-destruction decision during saved
item spawning. It requires matching full item/CO/unique placeholder IDs, available
installed/action definitions, a living world marker, a zero generic maximum and
unambiguous deterministic saved health conditions. Missing, duplicate, invalid,
zeroed or exhausted health remains native. Templates and checks outside the active
ship's spawning call remain native. Nested calls and exceptions restore the prior
scope. Neither serialized nor in-memory damage/progress is changed. Shipbreaker's
existing grid guard is still needed after these markers survive the first pass.

Read-only audits using the production predicate and installed native DTOs report
**69 candidates / 69 retained** for new pg15 (60 grate floors, seven walls, G4 and
H4), versus **0 / 0** for the working autosave. Native-boundary checks exercise
the actual hook methods, global saved-CO lookup, template/nested/error cleanup,
other-ship isolation, missing providers and exhausted health. These run outside
Unity and do not establish successful game loading. Optional repeatable audit:

```powershell
dotnet run --project tests/PhobosNative.Tests -c Release "-p:OstranautsPath=$gamePath" -- $gamePath $repoPath --audit-placeholder-health $archivePath B-14EA
```

Both archives and the contemporaneous logs were preserved locally. New pg15's
three principal compartments still record approximately **117.27–117.29 kPa**;
the two airlocks record approximately **21.76 / 24.88 kPa**, and two other rooms
are already at vacuum. The update must preserve these differences, not invent gas.
It neither rewrites saves nor guarantees recovery of damage already saved.

For this recurrence, update **Framework only** alongside the already-installed
Shipbreaker 0.21.0, restart the game and load the new pg15. Look for `retained 69
saved construction markers` and the existing `restored saved grid bounds` message
for B-14EA. Confirm pressure, pending marker positions/progress and absence of the
duplicate-room/ghost-room errors, then test another save/reload in a new slot.
**Owner result:** after Framework 0.23.1 installation, the owner reported that
the fix was working. This confirms that reported reload, not every future save
or the separate stale-header case above.

## First incident: evidence and cause

The owner's 26 September 2026 report was investigated against the supplied
`pg15` save, seven adjacent autosaves and local game logs. The running versions
recorded by BepInEx were Shipbreaker 0.17.0 and Framework 0.20.0. The save's last
writer was Ostranauts 1.0.1.5.

**Observed save/log evidence:** the pending D4 fixture, hull chute and G4 grabber
are construction markers, not finished installed machines. The rotated G4 at
the right edge determines the saved 63-column by 44-row grid. Its completed
geometry is 3 columns by 4 rows at that rotation; the first loading pass uses
the native 1-by-1 marker. No rotor construction markers remain in these saves.

**Locally inspected game implementation:** [Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts)
1.0.1.5 calls `Ship.SpawnItems`, restores saved zones and rooms, and only then
recreates full construction markers. The initial generic marker uses `Blank`.
Room assignments use flat tile indices, whose meaning depends on grid width
and origin. This is local assembly inspection, not a published developer
diagnosis or an independently reproduced unmodded failure. Proprietary code,
logs and saves are not included in the repository.

**Offline calculation corroborated by logs:** the temporary grid is 62 by 44,
one column narrower than the saved grid. All eight inspected saves have this
mismatch. The G4 is the object defining the lost rightmost column. Four interior
compartments consequently select saved exterior-room data:

| Correct saved first tile | Temporary loading tile | Result in the recorded loading attempts |
| --- | --- | --- |
| 196 | 193 | Exterior room reused |
| 1157 | 1139 | Exterior room reused |
| 1410 | 1388 | Exterior room reused |
| 2297 | 2261 | Exterior room reused |

These four temporary indices match the game log exactly. The quoted exterior
ID belongs to adjacent autosaves 270/271; `pg15` has a different exterior ID
and the same four index failures in other recorded loading attempts. The log
also removes seven saved room records. This strongly identifies pending G4
placement as a trigger of the native loading-order defect in this case, rather
than showing that Shipbreaker processing consumes atmosphere on reload.

The older rotor/RCS report describes another trigger of the same sequence.
This mitigation does not apply to ships with only other providers' markers.

## Mitigation and boundaries

After native item spawning, before saved room and zone assignments:

1. Require a saved-game load with at least one actually spawned Shipbreaker
   equipment marker. Ownership comes from Shipbreaker's published installation
   targets, including damaged forms; other mods' targets are not claimed.
2. Use Framework's pure `SavedGridBounds` planner to check that the current grid
   fits entirely inside the serialized bounds with integral tile offsets.
   Limit padding on each edge to the largest relevant equipment dimension.
3. Expand through the game's existing `TileUtils.PadTilemap`. This adds empty
   grid cells, preserving existing cells and world positions. It does not add
   floors, walls, gas, construction materials or completed machinery.
4. Leave native room/zone restoration and subsequent full marker restoration
   to the game. Log the old and restored dimensions. Matching grids are a no-op;
   unsupported geometry is left unchanged with a diagnostic.

The hook excludes templates, shallow-only loads and ordinary room recalculation.
Shallow-to-full loading is covered. It does not change saved identities,
inventories, processing permissions, material budgets or machine footprints.
Framework provides the calculation; Shipbreaker owns the narrow activation rule.

This is prevention during loading, not a save-repair or atmosphere-replacement
tool. Gas already lost **before the save was written** cannot be recovered by
this change. In the supplied `pg15`, three principal pressurized compartments
still record approximately 121.85 kPa; other compartments have different
pressures, including two already at or near vacuum. Keeping those differences
is correct preservation, not failed recovery.

## Verification and owner check

**Owner result, 26 September 2026:** after installation of Shipbreaker 0.19.1
and Framework 0.21.1, the owner reported: "Yes! It seems to work well".
This is a successful owner-reported reload following the affected-save
investigation, not a measured comparison of every saved field. A subsequent
save/reload cycle and other placements were not separately reported.

Automated tests reproduce the four wrong indices and verify preservation after
padding, every edge and corner combination, origin shifts, repeated application,
invalid bounds and bounded allocation. Loader-boundary tests exercise the real
patch against controlled native doubles. Native integration checks inspect the
installed assembly's hook signature and ordering, generic marker geometry and
Shipbreaker install targets. These are not Unity/gameplay tests.

With the game closed, install the prepared Shipbreaker and Framework packages
using the [shared installer](installing-mods.md). Load the original affected
save. In the BepInEx log, look for `restored saved grid bounds from 62x44 to 63x44
before room and zone loading`. Confirm the established compartments retain
their expected pressure, that duplicate-room/ghost-room messages do not recur
for this ship, and that the pending construction is still pending with its
original positions and progress. Check a subsequent ordinary save/reload too.

The original save and a local evidence copy remain available. Installation
does not edit either. A failure to apply the guard is explicitly logged; do not
interpret a successful build as confirmation that a loaded game has been repaired.
