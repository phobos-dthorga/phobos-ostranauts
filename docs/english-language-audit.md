# English language audit — 27 September 2026

The retrospective rule is now in AGENTS.md and [localization](localization.md).
[Writing for the crew](player-language.md) contains the source-backed audience
interpretation and shared glossary. The audience is defined by interests in
survival, ship operations and role-playing, not assumed education or measured
age/gender demographics.

## Coverage and decisions

The [review ledger](../config/english-language-audit.json) accounts for all
**1,722 original catalogue entries** across Agriculture (226), Auto Nav (490), Framework
(313), Manufacturing (1) and Shipbreaker (692), plus one new furnace warning
separating player presentation from the existing diagnostic. Each row retains the original
and revised text, its category, decision, rationale and source references.
Retained labels and diagnostic exceptions are included, not counted as rewrites.

| Mod | Reviewed | Rewritten | Retained |
| --- | ---: | ---: | ---: |
| Agriculture | 226 | 113 | 113 |
| Auto Nav | 490 | 51 | 439 |
| Framework | 313 | 28 | 285 |
| Manufacturing | 1 | 0 | 1 |
| Shipbreaker | 692 | 69 | 623 |
| Total | 1,722 | 261 | 1,461 |

The document inventory contains 143 files; 14 further surfaces cover recipe
fallbacks, overlays, metadata, naming maps and maintained item explanations.
There are 1,667 literal/dynamic references and 56 compatibility entries without
a direct lexical caller. They remain reviewed and retained rather than deleted.

The ledger separately inventories current guides, Workshop drafts, generated
references, research/developer documents, recipe fallbacks, mod-menu notes,
native overlays and equipment-name maps. Research and historical evidence remain
technical where appropriate. Generated item references are rebuilt from their
maintained inputs and fresh native data, not edited by hand.

References distinguish literal uses, dynamic key families and compatibility
entries with no direct lexical caller found. A family reference is not an exact
callsite proof. Older compact panel strings remain in the catalogue for
compatibility even when the current shared panel no longer calls them.

Hard-coded C# prose was inspected through definition fields, panel assignments,
catalogue calls and quoted sentences. Remaining exception/logger strings retain
their diagnostic role; game-owned widget captions stay with the game. Runtime
brand/model assembly remains in the existing equipment-name maps. No command,
translation key, placeholder contract, save identifier or gameplay rule changed.

## Examples

| Before | After | Reason |
| --- | --- | --- |
| This operation has no complete onboard accounting adapter | This operation cannot run during time-skip. Resume it afterwards. | Explain the consequence and action. |
| Purchase needs reconciliation. Saved custody/payment evidence is retained | Purchase could not be confirmed. Payment and delivery records are kept | Preserve the blocked retry without exposing implementation terms. |
| Evaluating further work windows within the per-update planning budget | Looking for another reachable wall. | Say what the grabber is doing. |
| Output commit requires recovery | Product release needs inspection | Keep the fault visible without promising an automatic fix. |
| Each offer remains at most one item | This changes availability, not the size of each stock lot | Correct an obsolete claim after the stock-lot feature. |

Prepared food gains a little character: “Hot potatoes from the galley. A proper
meal after a shift in the boneyard.” Failure messages remain direct. Technical
words such as pressure, coolant, nutrient solution and RCS stay where useful;
the glossary and relevant guides explain them.

The audit also corrected stale grabber/collector recipe descriptions and mod-menu
notes: powered wall cutting and floor-mounted collectors are implemented, and
Shipbreaker requires Auto Nav. Current Workshop drafts describe present features
instead of accumulating technical patch notices. Historical release notes remain
unchanged; new patch entries are explicitly Draft.

## Verification and limits

Run `python scripts/audit-player-language.py` to check coverage, reviewed wording,
placeholder formatting, grammar tokens and document/source freshness. It does
not automatically approve new text. Update the affected review rows after a
human/editorial review; preserve their prior wording and reason. New documents
must be classified. This check is included in documentation CI.

Existing builds check localization, English recipe/overlay fallbacks, branding,
native definitions and behaviour independently of display wording. Offline
substitution and browser samples check long names, narrow controls and multiline
warnings. They do not establish Unity font metrics, truncation, focus or UI scale.
**Unity layout and gameplay approval remain unverified.**

Validation: 71 Python maintenance tests passed, along with 19,012 Framework,
824 Agriculture, 8,378 Shipbreaker and 11,243 native-definition assertions and
the existing Auto Nav flight/docking/sensor/fire suites. Counts include parameter
sweeps and do not measure gameplay coverage. Substituted browser samples at
420 px and 1,100 px had no page or button overflow; long names and multiline
readouts were inspected. These samples use browser fonts, not Unity's fonts.
The synthetic installer suite also passed 288 checks, including dependency
downgrade protection. It does not run the game.

No artwork generation, mechanical rebalance, interface redesign, public API
change or save-format migration is included. The furnace's log keeps the existing
exception detail while its panel uses the new plain warning. Manufacturing remains a held
scaffold with one precise bootstrap diagnostic and no playable machinery.

## Performance follow-up, 27 September 2026

The performance candidate adds one reviewed Auto Nav message: "DOCK clearance
received. Hull fit is checked when you start." This distinguishes display
information from the fresh command checks. Current coverage is 1,724 entries,
144 documents and 14 other surfaces. Version requirements, generated reference
headings and the performance help/report were reviewed together; existing
placeholder contracts, equipment names and research attribution remain intact.
See the [performance audit](performance-audit.md) for this candidate's offline
checks and the deliberately deferred in-game measurements.
