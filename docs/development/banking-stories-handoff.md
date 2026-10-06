# Phobos Banking stories: handoff for ChatGPT

Handoff, 7 October 2026. The owner asked for the lore and storytelling behind Phobos
Banking's lenders to come from ChatGPT; Claude wires and checks it. This page says what
already exists, what to write, and the rules the game and our code hold the text to.
Everything here is a request; nothing is written yet except one example set.

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

**The example set.** `mods/PhobosBank/framework/story.json` holds the officers, a thread
per lender (`bank-<lender id>`) and Corvane Mutual's four letters, written by Claude to
prove the wiring. Treat them as a model you may rewrite: the welcome letter, a late
reminder with two replies (one sets the flag `bank-corvane-mutual-asked-time`), a sterner
notice when late for days, and thanks on repayment with a small standing gain with
OKLGCorp.

## What to write

1. **Letters for every lender's four events**: Halcyon Bond, Aerie Savings Union,
   Stillwater Advances and the Narrow Ledger (sixteen arcs), and Corvane Mutual's four if
   you want to improve them. Each lender's letters should sound like that lender. The
   unregistered lenders may be colder, slicker or more threatening in tone, but see the
   rules below on what they may not claim.
2. **Replies** where they add something: a late letter may offer two to four replies
   (promise to pay, ask for patience, refuse, bluster), each with its own answer letter
   and, if you like, a flag of your own the lender's later letters can read.
3. **Adverts** for each lender, placed at its home (`place`), and perhaps a news item or
   two about the credit trade (a lender expanding, a crackdown on unregistered lending at
   a station, the Narrow Ledger's reputation in Corsair's Hollow).
4. **Small talk** about borrowing and debt, placed where it fits, and optionally lines
   that react to the player's flags (crew noticing the player owes the Narrow Ledger).
5. **An encyclopedia article or two** on credit in the system: Ogiso's Bank and the
   registered houses, how station mortgages work by the shift, and the counters that lend
   when nobody else will.
6. **Optional longer threads**: a lender with a story beyond the letters (Halcyon Bond's
   exclusive client list, a Stillwater clerk who quietly helps borrowers, Pell's
   history). These can start on their own (`chance` above 0, gated by flags and places)
   rather than from an event.

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

## How it gets wired

Send the pack as one JSON file (or several, one per lender). Claude merges it into
`mods/PhobosBank/framework/story.json`, runs the story validator, the Banking tests (every
event arc must belong to a known lender and event) and the native checks against the
game's data, adds the text to the language ledger and updates the changelog. The arcs
start working with no code change.
