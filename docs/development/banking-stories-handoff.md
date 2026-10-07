# Phobos Banking stories: handoff for ChatGPT

Handoff, 7 October 2026. The owner asked ChatGPT to write the lore and storytelling
behind Phobos Banking's lenders, with Claude wiring and checking the pack. The core
request is now implemented in the held 0.5.0 draft. This page preserves the lender
choices and the rules for future story edits; optional longer lender threads were left
out. Nothing here records Steam publication or gameplay verification.

## What exists (Phobos Banking 0.4.0)

Five lenders, from `mods/PhobosBank/framework/lenders.json`. Rates and terms are agent
choices the owner may change; the lore should not quote exact rates, which may move.

| Lender id | Name | Kind | Home (story place) | Officer (story person) | Character so far |
| --- | --- | --- | --- | --- | --- |
| `corvane-mutual` | Corvane Mutual | registered | `oklg`, around OKLG | `corvane-ines-varga`, Ines Varga, loans officer | Old yard-town mutual society; steady, plain-spoken |
| `halcyon-bond` | Halcyon Bond | registered | `mtrs`, around Port Yangshan, Mars | `halcyon-laurent-chao`, Laurent Chao, client manager | Bond house for established captains; polite, a little exclusive; wants Warm standing with Xinhua |
| `aerie-savings` | Aerie Savings Union | registered | `vnca`, around Long Beach Terminal, Venus | `aerie-maeve-okonjo`, Maeve Okonjo, members' adviser | Members' savings union of the cloud habitats; warm, egalitarian; finances apartments |
| `stillwater-advances` | Stillwater Advances | unregistered | `jfts`, around Port Independence, Ganymede | `stillwater-dario-kessel`, Dario Kessel, advances clerk | Quick money, honest that it costs more; brisk |
| `narrow-ledger` | The Narrow Ledger | unregistered | `coho`, docked at Corsair's Hollow, Ceres | `narrow-ledger-pell`, Pell, the counter | Lends to anyone who finds the counter; steep, knowing, faintly menacing in tone only |

The lenders' own pitches (their sales voice) are in `lenders.json`; keep the lore
consistent with them.

**Events Phobos Banking reports.** When one of these happens, Phobos Banking starts the
story arc named `bank-<lender id>-<event>`, if a story pack has one, as long as its
requirements hold:

| Event | When | Arc id example |
| --- | --- | --- |
| `borrowed` | The player takes a loan from the lender (cash, or a ship or apartment financed at a broker) | `bank-halcyon-bond-borrowed` |
| `late` | One of the lender's bills turns late (the game marks it overdue at a shift change) | `bank-halcyon-bond-late` |
| `late-long` | The lender's oldest late bill has been late for three game days | `bank-halcyon-bond-late-long` |
| `repaid` | A loan from the lender is paid off in full | `bank-halcyon-bond-repaid` |

**Story flags Phobos Banking keeps**, which any story content may require with `flags` or
`notFlags`: `bank-<lender id>-borrowed` (ever borrowed), `bank-<lender id>-repaid` (ever
repaid a loan), `bank-<lender id>-late` (a bill is late now; cleared when none is) and
`bank-<lender id>-late-long` (late for three days now; cleared with it).

**The example set.** `mods/PhobosBank/framework/story.json` retains Corvane Mutual's
four letters, first written by Claude as proof of the event wiring. They remain the model
for the event arcs: a welcome, a late notice with replies, a longer-late notice and thanks
on repayment. In 0.5.0 the other four lenders receive the same event coverage, with their
own voices and reply letters.

## What the 0.5.0 draft adds

- Four repeatable event arcs for each of the five lenders: borrowed, late, late-long and
  repaid. Late letters offer replies with separate answer letters; none changes the debt.
- A home advert, station news item and local small-talk line for each lender.
- An encyclopedia article on credit, station lenders and shift payments.
- Optional longer lender threads were left out. The letters and local material stand on
  their own, and the owner can choose a direction for longer stories later.

## Orrery Credit (Phobos Banking 0.6.0)

A sixth institution, unlike the five lenders: **Orrery Credit**, a system-wide credit line
the player opens and draws on from the PDA anywhere (owner choices, 7 October 2026). It
costs more than a lender's counter and says so: a fee on each draw and a higher rate.
Claude chose the name (an orrery is a model of the whole system) and a placeholder
person, **Tamsin Ware, accounts desk** (`orrery-tamsin-ware`, home `oklg` for now); both
are yours to develop or replace (keep the keys).

- **Events:** `line-opened` (arc `bank-orrery-credit-line-opened`) when the player opens
  the line, and `late`, `late-long` and `repaid` as for the lenders (`repaid` when the
  balance is paid off; the line stays open). There is no event per draw.
- **Flags:** `bank-orrery-credit-line-open`, and the late ones as for the lenders.
- **Thread:** `bank-orrery-credit`, no place.
- **Lore rule:** immediate credit anywhere in the system is a gameplay convenience, not
  established Ostranauts canon. Do not invent a system-wide network, instant settlement or
  a technology to explain it; the fiction may say the line is authorised against the
  captain's account, and leave it there.
- **Wanted:** its four event letters (line-opened, late, late-long, repaid), a voice, a
  little history, adverts that are not tied to one place (use a `thread` with no place,
  or several placed adverts), and whatever small talk suits a credit line everyone has
  heard of.

## Rules (the game and our code hold the text to these)

- Write story pack JSON exactly as [writing story content](../writing-story-content.md)
  describes (the schema, lengths, placeholders such as `[player-first]`, and `person`
  senders). Keep every id lowercase with dashes and starting with `bank-`, and keep the
  existing person and thread keys (you may change a person's display name and role).
- **Event arcs**: `"chance": 0` (only the event starts them), `"repeatable": true` (a second
  loan or a second late spell sends the letter again), `"thread": "bank-<lender id>"`. Do
  not give the lender threads a `place`: letters must reach the player wherever they are.
  Adverts and news do take a `place`.
- **Consequences stay the game's own** (owner decision, 6 October 2026). The game adds a
  17.5% late fee every shift a bill stays unpaid, and the debt sits in the Finances
  window; that is all. Letters must not say or imply that anything else will happen
  (repossession, collectors, violence, seized cargo, blocked docking, a bounty). A
  lender may sound displeased, worried or menacing, but may not promise an action the code
  does not take.
- **Money moves only in the Finances window.** A reply cannot pay the debt; do not use
  `credits` tests that take money as a way to "pay the loan", and do not give credit
  rewards for borrowing. A small `credits` reward elsewhere is fine if the story earns it.
- **Standing** changes are small (up to 10 points, the code allows two factions per
  outcome) and go to the game's own factions (OKLGCorp, Xinhua, GalileanConfederacy,
  VenusCrim, BeltPirates and the others the places list). A good repayment might earn a
  point or two with the lender's station; a lender's displeasure should not cost standing
  with a government.
- **Never quote exact rates, limits or fees** in story text except the game's own 17.5%
  late fee; the lenders file may change.
- Ogiso's Bank and Ogiso's Register are the game's institutions: refer to them, never
  speak for them.

## How it is wired and checked

The 0.5.0 draft is merged into `mods/PhobosBank/framework/story.json`. The pack uses the
existing Banking event hooks, lender officers and place-less threads; no code change was
needed. The story validator checks the pack shape, and Banking checks require each event
arc to belong to a known lender and event. Future edits follow the same checks, update the
language ledger and changelog, and stay in Draft until the owner confirms publication.
