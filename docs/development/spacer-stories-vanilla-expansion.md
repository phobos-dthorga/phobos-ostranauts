# Spacer Stories: life around vanilla lore

Prepared 6 October 2026 for the owner's request to expand Ostranauts' world primarily
through vanilla lore, with occasional Phobos connections. The owner asked for
schema-only storytelling, left code to Claude, welcomed literary influences and
suggestions, and reported frustrating interplanetary gig deadlines.

This is original community fiction alongside Blue Bottle Games' setting. It does
not become official canon by being plausible. All runtime additions are four JSON
story overlays; no plugin, engine code, native plot, pledge or Gig Nexus definition
was changed. The previous nine overlays and every existing entry and step id are
unchanged. The source draft is 0.3.0; publication and gameplay review remain held.

## What was written

**Agent choices, open to owner revision:** 164 new content entries, about 11,600 words
including correspondence, plus twelve people and twelve threads. Counts exclude
author notes and the people/thread tables. The complete collection has 393 content
entries, 22 chains, 50 steps, 23 data files, 21 people and 22 threads.

| New overlay | Contents |
| --- | --- |
| [10-vanilla-threads.json](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/10-vanilla-threads.json) | Twelve offscreen correspondents and twelve place-based threads |
| [11-station-life.json](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/11-station-life.json) | 37 news items, 13 adverts, 59 small-talk lines and 16 loading tips |
| [12-vanilla-correspondence.json](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/12-vanilla-correspondence.json) | Thirteen chains and 28 steps; three skill-based alternative paths |
| [13-vanilla-archives.json](../../mods/PhobosSpacerStories/phobos/PhobosFramework/story/13-vanilla-archives.json) | Fourteen readable files and twelve general encyclopedia articles |

The additions work with Framework alone. Four optional lines/adverts mention
Verdemorrow, Asterel or Rivetline when Agriculture, Auto Nav or Shipbreaker is
present. Those entries do not supply or certify equipment. The older maker
collection remains available with its existing mod gates.

| Chain, after the spacertales- prefix | Home and correspondent | What happens |
| --- | --- | --- |
| estimate-with-a-return | OKLG; Dara Osei | A profitable-looking quotation omits the return journey and a family commitment. A comparison changes the estimators' own form, pays 300 credits once and gives COMING_HOME.TXT. |
| names-under-the-label | OKLG; Noor Iman | A pump's old label preserves maintenance and training rather than suspicious ownership. A carried Bingham-12 EVA battery provides a short-name comparison; it is kept. Wages and title remain unresolved. |
| address-while-away | Flotilla; Laleh Ade | Misfiled letters reveal why a willing message-holder must not silently become a captain's debtor. The neighbours correct their voluntary contact sheet. |
| petition-keeps-its-age | Old Ring; Ren Wu | Recopied maintenance covers make a long wait look recent. The earliest date is restored; acknowledgement does not become a fictional repair. Civil engineering offers a shorter reading path. |
| two-descriptions | Port Mojave; Matti Sorell | A household and a broker describe the same box differently. Both descriptions survive; neither becomes a customs ruling. |
| yard-supper-letter | Titan Shipyards; June Havel | An on-time launch hides a missed family supper. The family asks for time, rather than another flattering bulletin, and saves portions for the last watch. |
| unfinished-lunar-account | Port Shajiang; Pema Das | A contractor closes an account while a work list still expects another shift. Mechanical engineering offers a shorter comparison. No abandoned-site expedition is requested. |
| view-from-the-shift | Qiantangmen; Wen Park | A maintenance worker can keep a desirable address but cannot keep a family visit. Their letters preserve affection and pressure without inventing a rescue or visa system. |
| the-extra-voice | Deimos Research & Development; Simone Kerr | An ordinary tea remark has an uncertain place in a service recording. Electrical engineering shortens the written comparison. Neither replay nor visitor is proved; the cleaner's work is credited. |
| invoice-for-the-missing | Long Beach Terminal; Ines Duarte | A closed cargo claim must not erase a family's return contact. The copying exercise ends; the crew's fate remains unknown. |
| politics-in-the-envelope | Porto Nuevo; Tomas Bell | Relatives disagree about the Atlantis blockade while correcting addresses and asking after a cousin. Both letters, including the recipe, remain together. |
| a-meal-or-a-debt | Corsair's Hollow; Rafe Kito | Three books expose a meal wrongly called a loan. The people involved settle their own account as a meal with shift; the cook keeps the crossed-out history. |
| a-letter-that-can-wait | Starts at OKLG; Dara Osei and June Havel | Reading COMING_HOME.TXT opens a possible later letter. A visit to Titan's shipyards receives its apology and reply, pays 600 credits once and imposes no return trip or expiry. |

The repeated concern is **who remains visible in a successful account**. Ownership
does not describe all the work; a launch does not describe every person's evening;
a closed claim does not describe a family's wait. The captain's intervention is
small but has a persistent outcome in correspondence, flags and news. Nobody
single-handedly overturns a colony, repairs the blockade or reveals the truth of
Earth's collapse.

## Canon evidence and the boundary of invention

Blue Bottle Games' [official Ostranauts description on Steam](https://store.steampowered.com/app/1022980/Ostranauts/)
establishes a ship-owning scavenger's life in a System cut off from a ravaged Earth,
with debt, supplies, functional equipment, Newtonian flight and social needs. Our
inference is that small-scale freight, housing, family and service work fit that
experience; Steam does not establish any new character or incident here.

Read-only primary evidence is Blue Bottle Games' installed **Ostranauts 1.0.1.5**
data, principally `StreamingAssets/data/tips/tips.json`, with the station catalogue
checked through Framework's existing place records. No extracted game text or
assets are committed. The [story guide](../writing-story-content.md),
[Framework place table](../../mods/PhobosFramework/framework/story.json) and
[earlier authoring record](spacer-stories-authoring.md) provide the maintained
implementation and provenance links.

| Blue Bottle Games primary game entries | Observed setting fact | Our original addition |
| --- | --- | --- |
| TipLoreStartDate | Play begins in 2079 after Earth's orbital ablation cascade. | Addresses, uncertain family memory and offworld message-holders; no date or cause assigned to the cascade. |
| TipLoreOKLGGeneral | Ayotimiwa established K-Leg as a shipping stop in 2032; part of the original settlement remains. | Freight comparisons, sale labels and neighbours' correspondence; no new harbour policy or role for Adeyemi. |
| TipLoreLunaGeneral | Infrastructure was abandoned as business moved toward Mars and the Belt; ordinary work and recreation remain. | One incomplete contractor account and meal order, without site access or an explanation of the departure. |
| TipLoreMareIbrium | The lunar hotel has an established disturbing history. | Deliberately no new diagnosis, culprit, supernatural explanation or hotel quest. |
| TipLoreHangzhouGeneral, TipLoreHangzhouWorkers, TipLoreWestLakeHousing | An attractive orbital habitat, maintenance visas used for indentured servitude, and extreme housing costs. | One worker's missed visits and private family letters; no invented universal visa contract or new numerical wage ratio. |
| TipLoreVoltaire | Brave New World runs the Deimos research network, rents space to other companies, manufactures electronics and supports a university. | Ordinary night-service work and an inconclusive recorded recollection; no corporate experiment, research result or explanation for vanilla mysteries. |
| TipLoreNewcalGeneral, TipLoreNewcalPirates, TipLoreNewcalPirateTactics | Venusian aerostats and piracy affecting transfers, cargo and colonists. | One uncertain shipment account, with its cause explicitly unresolved. |
| TipLoreAtlantisBlockade, TipLoreAtlantisStructure, TipLoreFortSimpson | A blockade after the Ganymede Coup, Atlantis' under-ice habitats and the Confederacy's political setting. | Private relatives' disagreement; no blockade ending, new route or faction-wide policy. |
| TipLoreVirginia, TipLoreTitanGeneral, TipLoreTitanShips | Titan's advanced ships, habitats and military-industrial importance. | A service-craft launch, shift coverage and family archive; no new military campaign or institution. |

Ceres threads use the existing Port Mojave, CCRE Old Ring and Corsair's Hollow place
records. Their housing and meal-book cases are **agent-authored local fiction**,
not observations of a vanilla tenant dispute or a newly discovered CCRE rule.
Likewise, every new person, letter, commercial notice, estimators' circle and
private arrangement is invented. General articles extrapolate everyday life from
the facts above, without replacing the game's account of major history.

No NASA or ESA scientific result informs this writing. The fictional Titan context
comes from Blue Bottle Games' world; it is not a claim about a real NASA programme.
No scientific, engineering or clinical finding is invented to justify a plot.

## Literary influences: decisions, not borrowed prose

These are agent-selected thematic and structural influences. The words, people,
companies, incidents and outcomes are original. No author is imitated at sentence
level, no named fictional setting is imported, and no author or publisher endorses
the mod. Source pages were checked on 6 October 2026.

- **Ursula K. Le Guin, The Carrier Bag Theory of Fiction.** The
  [essay's official author-estate page](https://www.ursulakleguin.com/the-carrier-bag-theory-of-fiction)
  describes stories and technology beyond a dominating hero. Our interpretation
  gives maintenance, meals, letters and making room for other people narrative
  weight. It shaped the berth, lunar-work and family-envelope cases. This is an
  editorial use of an essay, not evidence about human prehistory or life support.
- **C. J. Cherryh, Alliance/Union and the Merchanter books.** Cherryh's own
  [Universes of C. J. Cherryh](https://www.cherryh.com/www/univer.htm) identifies
  trading ships and commerce as the Merchanter setting and describes political
  control weakened by the distance between orders and local reality. Our
  interpretation treats the captain's itinerary, family commitments and local
  records as constraints deserving respect. It shaped the return estimate and
  unhurried letter. Her FTL, wars, factions and technologies are not imported into
  Ostranauts' Newtonian Solar System.
- **William Gibson, Neuromancer.** The
  [Gollancz/Hachette publisher description](https://www.hachette.co.uk/titles/william-gibson/neuromancer/9781399627658/)
  describes a precarious worker drawn into coercive, opaque employment. Our
  interpretation keeps corporate power visible through a payment description, a
  visa's terms and a service worker's missing credit. It shaped the three books,
  maintenance residency and Deimos shift. No cyberspace mechanics, implants,
  poison ultimatum, character or prose is adapted.

Le Guin contributes community; Cherryh contributes distance and commerce; Gibson
contributes unequal control over the terms. The unsettling Deimos account remains
a small original uncertainty within Blue Bottle Games' own noir premise. It does
not turn a genre influence into the explanation of a vanilla mystery.

## Gigs: delivered response and recommendations

**Owner report:** excellent vanilla rewards accompanied by travel constraints that
can demand an implausibly long journey in two days. That particular generated job
was not reproduced from a save in this authoring session, so its destination,
deadline, offered ship capability and generation path remain unverified.

**Observed data:** Blue Bottle Games' `data/jobs/jobs/jobs.json` has separate normal
courier and ShortHop definitions. For example, MVPCourierTestDeliverCO has
fContractMin/Max 100/300, fPayoutMin/Max 200/600 and fDuration 6; the OKLG ShortHop
definition has no contract deposit, payouts 100/250 and fDuration 0.0445. These are
raw authored fields, not a reproduced offer. Duration units, runtime scaling and
route feasibility were not established from the JSON alone. We do not relabel
those numbers as a verified two-day timer, or assume all gigs behave alike.

**Delivered schema choices:** none of the thirteen new chains expires. Twelve are
local correspondence rather than courier gigs. Their waits are one to six hours
per step; where a wait and docking are combined, the wait is elapsed time since
step entry, not a demand to remain continuously docked. Three comparisons have
skill-based shortcuts. The linked Titan letter waits for an itinerary the captain
chooses; its dialogue, goal and file all say the 600-credit receipt cannot justify
a dedicated voyage. The estimate case pays 300 credits once. There are no deposits,
penalties, consumptive tests, repeatable payments or altered vanilla gig rewards.
The total new monetary reward is 900 credits per save with ordinary retained story
records. This is authored gameplay balance, not a profitability calculation.

**Agent recommendation, not implemented:** improve native gig generation separately
with Claude. Show the destination, deposit, full time allowance and relevant route
before acceptance. Reject or label offers whose journey is infeasible for the
offered ship assumptions; account for departure and docking, queues, supplies and
a return or onward leg. Start a delivery clock when cargo is actually entrusted,
where that matches the contract. Offer local work and route-compatible cargo as
useful alternatives to dedicated expeditions. A price should not disguise the
absence of a workable route.

**A disagreement worth preserving:** removing every timer would also remove useful
urgency. A local errand, a perishable load or a negotiated emergency can justify a
clock the player understands and can meet. The stronger design distinguishes
urgency from an arbitrary map-spanning deadline. Late arrival could sometimes
change the fee or the recipient's circumstances instead of deleting a story;
those outcomes should be explicit when accepting. None of those native-gig
changes is claimed by this schema-only expansion.

One further recommendation for Claude is an explicit answer or accept/refuse
mechanism in the story vocabulary. The current skill branches are automatic, and
docking can count as receiving correspondence; neither represents a deliberate
dialogue choice. A small choice facility would give future disagreements more
player agency than another layer of passive waits. This is a proposal, not a
requirement for reading this volume and not a code change made here.

## Supported actions, saves and checks

All letters are crew-log deliveries from a person in the thread's cast. People do
not spawn. An arc's newsSeen gate gives it an introduction; its dockedAt requirement
keeps it from beginning merely because a regional place matches the whole Outer
System. Docking means being at the station, not finding a new office or pressing
an acknowledgement button. Background tips and encyclopedia articles contain no
private character, progress dependency or placeholder.

Three branches use the existing SkillEngCivil, SkillEngMechanical and
SkillEngElectronic conditions. They select a shorter reading route automatically;
they are not a choice menu and do not inspect or alter real buildings or devices.
The Bingham-12 comparison checks ItmBattery03 in carried inventory, including bags,
with consume false. It checks neither charge nor a particular object's identity.
The letter's outgoing card is a readable copy: the visit does not require that
particular card to survive or be carried. The receiving messages say so.

Outcome news uses thirteen persistent setFlags results and appears once per save.
Private Deimos talk waits for its file to be read; two crew lines follow the active
step. Introductory notices have weight 3 and other added news weight 1. Global
selection shares, check cadence, maximum active chains and Framework settings are
unchanged. Availability and pacing still need an in-game review: reading a large
collection through probabilistic media can take considerable play time.

Saved structures touched: new ids in Framework's existing story record, step
progress and reward bookkeeping, seen news, read-file markers, story flags and
ordinary data cards. Two existing reward paths also write ordinary credit ledger
entries. No new native identity, player condition, plot, room gas, machine record
or migration is needed. Old story ids and step meanings remain untouched. Future
and unknown record fields must continue to round-trip.

Offline checks: all thirteen overlays pass the add-on checker; the actual C#
add-on loader and StoryLibrary resolve all entries against read-only native item
and condition names in **128 optional-mod combinations**, preserve Framework's
settings and sections, and round-trip old and unknown story record fields. These
checks are not gameplay observations.

Maintained versions and Workshop notes pass their offline checks. Item references
were refreshed through the existing native export mode and reference renderer;
only the Spacer Stories guide and the source-hash snapshot changed. The combined
reference-maintenance wrapper shares the normal native-suite gate discussed below,
so this narrower export is not a claim that the full wrapper or build passes.

**Code handoff to Claude:** StoryNativeChecks currently hard-codes nine overlays,
nine additional arcs and nine additional files at lines 107–109. With this volume
there are thirteen overlays, 22 arcs and 23 files. Replace those inventory
assumptions with checks derived from the authored packs while retaining the
all.Problems and article-section checks. The normal build uses that suite, so a
distributable package remains held until the source checks are updated. Also add
this research document to the data-only package's ExtraDocs list if it should
travel with the package. No test or build code was edited here.

The native suite was run: it stops at line 107's nine-overlay assertion, with an
empty all.Problems list. The later fixed chain/file-count assertion is not reached.
The independent actual-loader check above therefore verifies the expanded
inventory while the normal package gate remains red. It would be misleading to
describe the full native suite or normal build as passing.

Owner gameplay review should begin with the estimate at OKLG, the carried-battery
comparison and one of the skill branches. Then load an older story save, confirm
its existing steps survive, receive and read a new card, and confirm an outcome
headline waits for its completion. Test the Titan letter only when a journey is
already useful; dismissing it is a valid decision, and the ordinary dismissal
mechanic sets it aside in that save. Review cadence and media repetition before
publication. No install, upload or save modification was performed by the agent.
