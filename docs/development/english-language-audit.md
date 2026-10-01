# English language audit — 27 September 2026

The retrospective rule is now in AGENTS.md and [localization](localization.md).
[Writing for the crew](player-language.md) contains the source-backed audience
interpretation and shared glossary. The audience is defined by interests in
survival, ship operations and role-playing, not assumed education or measured
age/gender demographics.

## Coverage and decisions

The [review ledger](../../config/english-language-audit.json) accounts for all
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

## Polaris follow-up, 27 September 2026

Current coverage is 1,742 catalogue entries, 145 documents and 14 other surfaces.
The 18 new messages distinguish installed weapons from firing readiness, explain
reverse volley clicks and warn explicitly about old-group native automatic fire
during a handoff. Fire help and the maintained item reference were updated
together. The [Polaris report](polaris-interface-refresh.md) records browser
size checks separately from pending native Unity appearance and interaction.

## Polaris correction review

The off-weapon message now says "Off: check power or control signal". Native launcher definitions do not offer a manual Turn on action, so "switched off" implied an unsupported remedy. Damage takes precedence when present. Current guides distinguish the saved condition from an unverified live wiring fault. Recovered tracking displays existing Resume guidance and preserves the historical suspension reason. No keys, placeholders or scientific attribution changed.

The owner approved smaller secondary text and compact controls following the
boundary report. The revised 300/400/600-pixel previews retain prominent status,
use real Departure wording and check scroll containment and access to long text.
These are layout checks, with native font readability still awaiting owner review.

## Auto Nav 0.22.0 follow-up

Reviewed the new Combat controls and priority/exit messages against the service.
Current coverage is 1,756 catalogue entries (523 Auto Nav), 146 documents and
14 other surfaces. New text separates movement, aiming and firing, explains
effective separation and never promises automatic restart or Artemis firing.
Existing placeholder, native-token, brand and historical research contracts remain.
The new Track help uses an inset scroll area; browser evidence is not Unity approval.

## Auto Nav 0.22.1 towing follow-up

Five new warnings explain the connection, brace and competing-control blocker. Navigation warnings outrank idle FCS faults. Current review covers 1,761 entries (528 Auto Nav), 147 documents and 14 other surfaces. The towing guide distinguishes supported ordinary flight from terminal docking and Combat; no save changes or live validation are implied.

Auto Nav 0.22.2 adds one direct attached-target instruction and updates the current towing/FCS/Combat guides. Existing towing reasons are reused for brace faults; historical 0.22.1 release limitations remain historical.

The item handling correction reuses native action labels and updates current references to explain Pick Up versus the drag slot, sections versus machines, and saved-hand recovery. The new audit accounts for all item definitions without treating offline checks as live interaction approval.

### Merchant availability and salvage expansion

Reviewed revised settings help, maintained acquisition explanations, current merchant/salvage guides and publication drafts. Chances are per native roll, not guaranteed finds; normal restocking, existing configuration, finite quantities and unverified live placement remain explicit. Historic research is preserved and current summaries link the revised policy.

## Assembly and maintenance follow-up, 28 September 2026

Reviewed the new section-site instructions, cargo recovery and specific maintenance blockers against their native and service checks. Coverage is 1,791 catalogue entries, 149 documents and 14 other surfaces. Descriptions distinguish unfinished sections from operating machines and saved table contracts from current construction sites. Instructions preserve tool, mass and safety requirements; recovery promises no automatic cargo movement. Current references and Workshop drafts agree; historical research remains labelled. The [assembly guide](../section-assembly-and-maintenance.md) separates offline evidence from owner playtesting.

Framework 0.31.1: reviewed the unnamed-native-trigger correction, current version summaries and generated references. No catalogue messages or formatting contracts changed; the report distinguishes the reproduced exception and passing offline hooks from pending Unity confirmation.

## Documentation audit and audience separation, 28 September 2026

Reviewed furnace assembly, coolant servicing, maintenance and cargo recovery instructions against their implementation. Corrected stale current requirements, marked introduction versions and design records as historical, and fixed generated section/machine placement instructions. Player text leads with the next action and its limits. Contributor material now lives in `docs/development/`; player guides retain necessary safety explanations and research credits. The [documentation audit](documentation-consistency-audit.md) records the 151-document baseline screen and its limits. The maintained language inventory now includes 151 documents and 14 other surfaces; 1,791 catalogue entries are unchanged. Link-only moves preserve historical claims and citations.

## Construction appearance audit, 28 September 2026

The [construction artwork audit](construction-artwork-audit.md) records the missing presentation code for D4/R4/F6 and distinguishes the owner’s observed R4 changes from a deliberately implemented stage sequence. It is developer-facing evidence, with no catalogue or runtime changes. Document coverage is now 152; the 1,791 catalogue entries and 14 other surfaces are unchanged.

The Framework/Shipbreaker 0.32.0 follow-up now binds those appearances. Reviewed
current assembly instructions, section-reference notes, Workshop drafts and version
summaries against the stage policy: early while parts arrive, intermediate only
with a complete valid bill and positive finite work, finished only through native
completion. The placement cursor and whole-machine installation remain native.
Missing images fall back without changing requirements. Historical audit evidence
is preserved with a dated implementation follow-up. No catalogue messages or
formatting contracts changed; coverage remains 1,791 entries, 152 documents and
14 other surfaces. Owner Unity approval is explicitly pending.

## Repeat run and storage outputs, 28 September 2026

Shipbreaker 0.33.0 adds 52 reviewed catalogue entries for the F6 repeat run and the
D4/R4 storage outputs, and extends the furnace and routing F3 help. New messages
state the situation, what is kept and the next action: a paused run names its cause
and asks for Repeat batches again; a full store keeps products in the tray. Pause
conditions never promise automatic recovery, and the partial-charge rule is explicit.
Existing placeholders, commands and keys are unchanged. Current guides, item-reference
inputs and both Workshop drafts were reviewed together. Coverage is now 1,843
entries, 153 documents and 14 other surfaces. Unity layout of the added buttons
and status lines is unverified.

## Auto Nav sensor suite, 28 September 2026

Auto Nav 0.23.0 adds 21 reviewed entries for the per-sensor breakdown, explicit
sensor switch-on and asteroid docking refusal, and rewrites four existing entries:
the F3 help, the target-selection refusal, the unavailable-contact message and the
sensor help. Emitting sensors are named as such wherever switching them on is
offered, and no message implies that Auto Nav switches sensors itself. The weak
contact reach and asteroid range are stated as numbers the player can act on.
Placeholders and existing keys are unchanged. Coverage is now 1,864 entries,
153 documents and 14 other surfaces. The Details layout is unverified in Unity.

## Auto Nav automatic sensor engagement, 28 September 2026

Auto Nav 0.24.0 adds 12 reviewed entries: the log and banner warnings, their emitting
variants, the switch-off confirmation, the hub line, the brief-hold status, the
setting help and the Details marker. Sensors help is rewritten to replace the retired
never-automatic rule. Warnings say what was switched on, why and whether it emits,
and that only Auto Nav's own sensors are switched off afterwards; no message promises
a track that sensors cannot provide. The sensor guide, departure guide, player guide,
item-reference inputs and both Workshop drafts were reviewed together; 0.23.0 and
0.9.0 statements remain as labelled history. Coverage is now 1,876 entries, 153
documents and 14 other surfaces. Banner and log appearance in Unity is unverified.

## Crew study through the vanilla chain, 28 September 2026

Framework 0.35.0 adds 35 reviewed entries: the per-speciality study action titles,
descriptions and tooltip mirror the vanilla study actions with the speciality named
and the same grammar tokens; the studying condition, retry status, AI-history log
lines and the read-only crew diagnostic state what is happening and what the player
can check. The retired 15-minute action label now says it is retired, and the console
help lists the crew command. The crew guide leads with the terminal action, the
AutoTask behaviour and the diagnostic; the author guide records the in-place
amendment rule. Coverage is now 1,911 entries, 153 documents and 14 other surfaces.
Unity menu wording and animation are unverified.

## Vanilla precedence, round 1, 28 September 2026

Framework 0.36.0 adds four reviewed entries and retires one: a crew-log line for a
craft action whose recipe is not registered, a developer log line for a failed
native task closure, and two read-only diagnostic lines naming who could take an
order step now. The unused "crew need rest" refusal is removed because the game's
own pledges now decide. The crew guide states the vanilla rule for needs and the
kept repair allowance; the author guide records in-place amendment, refusal at
effects time, unbounded clock steps and the native window stack; the new
vanilla-precedence audit is a contributor record. Coverage is now 1,914 entries,
154 documents and 14 other surfaces. Unity wording and layout are unverified.

## Vanilla precedence, round 2, 28 September 2026

Agriculture 0.17.0 adds five reviewed entries (the accept-contents control, its
two result lines, a lost-water log line and an occupied-rack refusal) and
Shipbreaker 0.34.0 adds one (the heat-wait status with the room's numbers),
rewrites the cooling-block line so it no longer instructs a resume, and retires
five entries that told players to resume after a time gap or a hot room, since
those machines now continue by themselves. Guides state the vanilla rules for
power state, stacked supplies, tank destruction, eating, offer-time refusals,
catch-up after long intervals, heat waiting and wall eligibility. Coverage is now
1,915 entries, 154 documents and 14 other surfaces. Unity wording is unverified.

## Vanilla precedence, round 3, 28 September 2026

Auto Nav 0.25.0 adds four reviewed entries (a held oversized step, the clamp and
release sequences running through the docking console, and holding for the
console's own alignment check) and rewrites three step lines that now cover
only an invalid step, since oversized steps are held rather than ending the
work. The docking, sensors, departure and torch guides state the vanilla rules:
no target-spin gate, the clamp as the game's own button, a refresh as a short
hold, the native release when the console is open, and the step-valid torch
guard. Coverage is now 1,919 entries, 154 documents and 14 other surfaces.
Unity wording is unverified.

## Vanilla precedence, round 4, 29 September 2026

Shipbreaker 0.35.0 adds nine reviewed entries (the armed-queue status, the
unsupported-mass refusal, the Load feed by crew action with its description and
tooltip, and four crew-loading replies), rewrites nine (feed and fixture
descriptions, the wall refusals and the completion line now state the wall
mass range rather than a fixed 24 kg, and the reclaimer feed hint names the
toggle) and retires two that told players to use a Manual feed. Framework
0.38.0 adds the Anywhere aboard label and its button. The player guide gains
a hand-fed operation section; the crew, intake, reclaimer, furnace,
reclamation and console guides state the new rules. Coverage is now
1,928 entries, 154 documents and 14 other surfaces.
Unity wording is unverified.

## Feed families, 29 September 2026

Shipbreaker 0.36.0 adds twenty reviewed entries (six family labels, two
step fragments, two mass refusals that name the family and its range, and five
reject names with their descriptions), rewrites thirteen wall-only lines so the
feed, the grabber and the refusals name every part family, and retires the old
fixed-mass wall refusal. The player guide, intake guide and crew guide name
the families; the new feed-families record explains each budget and refusal.
Coverage is now 1,947 entries, 155 documents and 14 other
surfaces. Unity wording is unverified.

## Bulk silos, 29 September 2026

Shipbreaker 0.37.0 adds seventy-four reviewed entries for the S3 process
water silo and the T2 ice thaw unit (names and descriptions, the silo status,
reserve and Ship's Water draw and deposit replies, the thaw queue states and
the reasons a block waits for its vessel, panel choices, console groups, crew
recipe and action labels, and one settings help line). Framework 0.39.0 adds
the shared vessel-loss log line and quote notice; Agriculture 0.18.0 retires
its own tank-loss line. The new silo guide and artwork handoff, and the crew,
console, player, economy and bulk-storage guides, state the new rules.
Coverage is now 2,021 entries, 157 documents and 14 other
surfaces. Unity wording is unverified.

## Custom ingots, 29 September 2026

Shipbreaker 0.38.0 adds twenty reviewed entries for the F6 recipe catalog
(three recipe labels, the next-charge control, recipe status, replies and
refusals, two feed labels, the ingot and steel-remainder names and
descriptions, and the two recovery recipe names and descriptions) and rewrites
five (the furnace and chamber descriptions and name now cover every recipe, the
crew order label covers the selected charge, and the F3 help lists the recipe
command). The furnace guide gains a recipes section with the NIST iron credit.
Coverage is now 2,041 entries, 157 documents and 14 other
surfaces. Unity wording is unverified.

## Bulk silo artwork, 29 September 2026

Shipbreaker 0.38.1 replaces the S3, T2, ingot and steel-remainder placeholder
drawings with selected artwork. No translation entries change. The bulk silo
guide drops its placeholder sentence, the artwork handoff becomes a produced
record with its deviations explained, the Workshop page loses its placeholder
note and gains a 0.38.1 bullet, and version lines move to 0.38.1. Coverage is
now 2,041 entries, 158 documents and 14 other surfaces.
Unity wording is unverified.

## Silo gauge ruling, 29 September 2026

The owner ruled that the S3 silo needs no painted level gauge, which is
contrary to the vanilla art style. The artwork handoff records the ruling and
withdraws the filled-state overlay, the asset generation policy gains a rule
against painted live-state instruments on world sprites, and the Shipbreaker
0.38.1 changelog moves its water-level pointer out of the limits. No translation
entries change. Coverage is now 2,041 entries, 158 documents and
14 other surfaces. Unity wording is unverified.

## Economy coverage audit, 29 September 2026

Shipbreaker 0.39.0, Agriculture 0.19.0 and Auto Nav 0.26.0 record the economy
coverage audit. No translation entries change. The new audit record joins the
reviewed documents; the economy guide corrects its remainder-category sentence
and gains missing S3/T2 and older repair and dismantle rows; the Agriculture,
Auto Nav, bulk-silo and furnace guides gain one sentence each on the new offers,
ingot finds and purchase-only machines; version lines move. Coverage is now
2,041 entries, 159 documents and 14 other surfaces.
Unity wording is unverified.

## Vanilla precedence, round 5 (Auto Nav 0.27.0, Framework 0.40.0), 29 September 2026

Auto Nav 0.27.0 and Framework 0.40.0 record the torch ignition and reactor
control round. Six new Auto Nav entries: the reactor core stop notice and its
crew log line (what stopped, that RCS continues, when the torch returns), the
RCS authority status line with the one action that raises it, the weak-contact
suffix, and the avoidance takeover count and its idle form. Two rewrites: the
Detour and Blocked notices gain the obstacle range and a weak-contact mark;
their held-commands and braking statements are retained. Torch starting is
now shown for a real reactor wait and keeps its wording. Reviewed documents:
the torch guide gains the reactor Flow and Cycle section, the departure guide
the seizure rules, the flight-profiles guide the slider curve and authority
readout, the author guide the NativeReactor paragraph, the audit its round 5,
and version lines move. Coverage is now 2,053 entries, 159 documents
and 14 other surfaces. Unity wording is unverified.

## Installer retired files, 29 September 2026

The installation guide describes the retired-file catalogue: a file an earlier
package installed and later packages no longer ship is backed up, recorded in the
receipt and removed only when its SHA-256 matches the catalogue, and previews and
verification report it. No translation entries change. Coverage is unchanged at
2,053 entries, 159 documents and 14 other surfaces. Unity wording is unverified.

## Manufacturing 0.1.0, 29 September 2026

Manufacturing 0.1.0 and Framework 0.41.0 add the Fennmark refinery, chemical
processor and hydrogen store. Every Manufacturing catalog entry is new (168
strings, reviewed against the player-language rule: situation, meaning and next
action; IDs, units, commands and placeholders kept) and the 0.0.1 scaffold
diagnostic is removed. Framework adds one vessel-loss journal line. The new
player guide and two development records join the reviewed documents; the
Workshop page is rewritten as an unpublished draft. Coverage is now
2,215 entries, 162 documents and 14 other surfaces.
Unity wording is unverified.

## Manufacturing 0.1.1 late-game economy, 29 September 2026

Manufacturing 0.1.1 prices its three machines as late-game plant. No translation
entries change. The player guide gains the late-game prices, buy-back routes,
rare finds and component repairs, and says plainly that only the machines are
late-game priced; the economy guide gains a Manufacturing section; the refinery
record, audit record, item-reference inputs and Workshop draft follow. Coverage
is 2,215 entries, 162 documents and 14 other surfaces.
Unity wording is unverified.

## Manufacturing 0.2.0 Sabatier and methane, 29 September 2026

Manufacturing 0.2.0 adds the Fennmark K2 Sabatier reactor and M2 methane store.
New catalogue entries cover the reactor's controls, waits, logs and hazards, the
methane store's text and four connection labels; three entries are rewritten (the
explosion object now reads "Gas deflagration", the panel help and the console
help). The player guide, design and implementation records, economy guide and item
reference follow. Coverage is 2,284 entries, 162 documents and
14 other surfaces. Unity wording is unverified.

## Workshop readiness, 29 September 2026

All five Workshop pages are rewritten as current, player-first descriptions under
Steam's 8,000-byte description limit (6.0 to 6.8 KB each): what the mod does,
getting started, requirements, Workshop installation, saves and limits, credits
and one list of guides. Per-version update sections now live only in changelogs.
The installation and getting-started guides gain the Workshop route; ten player
guides gain flowcharts with one-sentence lead-ins, and several stale statements
found on the way are corrected. No translation entries change. Coverage is 2,284
entries, 162 documents and 14 other surfaces. Unity wording is unverified.

## RCS propellant (Framework 0.42.0, Manufacturing 0.3.0), 29 September 2026

Manufacturing 0.3.0 adds the Fennmark P1 RCS propellant manifold and the Fennmark
propellant line; Framework 0.42.0 gives each RCS gas its real cold-gas worth. New
catalogue entries cover the manifold's switches, draw order, per-store rows,
waits and logs, and the line's names and remainder. The player guide gains an RCS
propellant section that leads with the three set-up steps; the Workshop pages,
design and implementation records, economy guide and item references follow.
Nitrogen-equivalent kilograms are named wherever a fuel reading is explained.
Coverage is 2,327 entries, 162 documents and 14 other surfaces. Unity wording is
unverified.

## Gas store sizes and canister filling (Framework 0.44.0, Manufacturing 0.4.0), 29 September 2026

Manufacturing 0.4.0 adds medium and large gas stores, oxygen, nitrogen and carbon
dioxide stores and the L2 canister filling station. New catalogue entries cover
each gas's store text, the L2's controls, waits and rack lines, and store
transfers. The store status and description keys moved to new keys (`level`,
`details`) because their placeholders changed; the old wording is retired with
its keys. The propellant line is now the gas line in every current text; the
player guide gains Gas stores and Canister filling station sections that lead
with the steps. Coverage is 2504 entries. Unity wording is unverified.

## S4 and S5 silos (Shipbreaker 0.40.0), 29 September 2026

Shipbreaker 0.40.0 adds the S4 and S5 process water silos. Two new name entries
reuse the S3's wording; the station offer now reads Process water (Rivetline
S-series silos). The silo guide, player guide, economy guide, item reference and
Workshop page follow. Unity wording is unverified.

## R4 and R5 reservoirs (Agriculture 0.20.0), 29 September 2026

Agriculture 0.20.0 adds the R4 and R5 reservoirs and lets a W2 draw from any water
vessel within one tile. The reservoir description moves to a new key with its size
placeholders (the old fixed wording is retired), the pairing message states the
one-tile rule, and the station offer names the Groundwork reservoirs. The storage
guide, item reference, economy guide and Workshop page follow. Unity wording is
unverified.

## Mining laser (Shipbreaker 0.59.0, Framework 0.66.0), 1 October 2026

Shipbreaker 0.59.0 adds the Ablatine ML-2 mining laser. Fifty-five new entries
cover its name and description, its three controls and the Set to cut choice,
its status lines, and its waits and refusals. Each wait says what the laser is
waiting for and what the player can do: give the room behind the mount air, let
it cool, move the crew member, moor a target. Stop says the cut in hand is
dropped. The beam and animation setting says that turning it off changes nothing
about the work. The limits a player needs are kept in the text (10 kPa, 40 C,
one tile from the beam). A new player guide and a development design record are
added; the player guide, economy guide, item reference, indexes and the
Shipbreaker Workshop page follow. The Workshop page lost its sentence about
furnace record settlement and its example of a full name to stay within its
size limit. Unity wording is unverified.

## Mining laser radiator link (Shipbreaker 0.61.0), 1 October 2026

Shipbreaker 0.61.0 lets the ML-2 mining laser shed its heat into a touching F6
cooling assembly and adds a Power setting. Twenty-two new entries cover the
Cooling and Power choices, the link and unlink confirmations, the reasons a
linked assembly is not taking heat (gone, damaged, no longer touching, at its
limit), and the status lines that say where the heat is going. Each refusal
says what to do: pause the laser, let the assembly cool to 50 C, choose an
assembly that touches the laser. The limits a player needs stay in the text
(50 C to change a pairing, one tile). The player guide gains a radiator section
with both settings side by side; the item reference, changelog and Workshop
page follow. Unity wording is unverified.
