# PDA apps and a banking mod: research

Research record, 6 October 2026. The owner asked for Phobos mods to add their own apps to
the wrist PDA, beginning with a banking service that offers loans from accredited and
non-accredited lenders, as a separate mod that may grow beyond loans. ChatGPT writes the
lore; Claude designs the code and proposes names. **Round 1 (Framework 0.126.0 `PdaApps`
and Phobos Banking 0.1.0, the debts screen) is built and checked offline; nothing is tested
in play yet.** See [Round 1 as built](#round-1-as-built). Observations come from the game 1.0.1.5 decompile (kept in ignored `.local`, line
numbers below refer to it), the game's own data files and our code. Proposals are marked
as such; owner choices carry the date.

**Owner choices (6 October 2026):**

- The bank app is a full Framework panel opened from its PDA icon, as the game's own
  Roster and Duties apps leave the PDA.
- Loans are the game's own ledger lines plus our own loan book.
- Our lenders are our own, beside the game's Ogiso's institutions.
- Default has only the consequences the game already has, for now. Repossession may be
  explored later, when more resources are available.
- Loans may help fund ship purchases at the broker, integrated neatly with its own window
  (see [Financing at the broker](#financing-at-the-broker)).
- The app also shows the debts the game already tracks, which fits the setting (see
  [Existing debts in the app](#existing-debts-in-the-app)).
- Lenders may be local to stations and regions, building on the story system's places,
  people and threads (see [Local lenders](#local-lenders-through-the-story-system)).

## How the PDA works (observed)

The PDA is the scene object `GUIPDA` (decompile 134427), a `MonoBehaviour`, not a
`GUIData` panel.

- **Apps are a fixed enum.** `GUIPDA.UIState` (134429) lists Closed, Home, Objectives,
  JobOrder, JobBuild, Tasks, Socials, GigNexus, Ferry, NavLink, Viz, Notes, Timer, Standings
  and Bounties. Each is a `CanvasGroup` already in the scene. The `State` setter (134849)
  hides every group, shows one and slides the PDA in or out.
- **Opening is a fixed switch.** `GUIPDA.OpenApp(string)` (135263) handles about twenty
  names (`home`, `goals`, `gigs`, `ferry`, `standings`, `roster`, `files` and others). Any
  other name logs "Tried to open unrecognised app" and opens Home.
- **The home screen is data.** `GUIPDAHomepage` (54070) builds one icon per entry of
  `DataHandler.dictPDAAppIcons`, loaded from every mod's `data/pda_apps/*.json`
  (`JsonPDAAppIcon`: `strName`, `strFriendlyName`, `strIcon`, `bHidden`). An icon
  (`GUIPDAApp`, 54002) loads `strIcon + ".png"` from any mod's `images/`, and takes its
  tooltip from `GUI_PDA_BUTTON_<NAME>` and `GUI_PDA_BUTTON_<NAME>_TITLE`. Tapping it calls
  `OpenApp(strName)`. Vanilla icons are 256 by 256 pixels in `images/pda_apps/`.
- **The hotbar is the player's.** `GUIPDAHotBar` (54168) shows the apps listed in the
  player's `UserSettings.strApps`; a mod cannot add to it without changing user settings.
- **Some apps leave the PDA.** `roster`, `duties` and `vote` raise a normal panel through
  `CrewSim.RaiseUI`; `files` and `navlink` open the PDA item's computer screen. `RaiseUI`
  (41077) always closes the PDA; our own panels opened without `RaiseUI` (Letters, Crew)
  leave it open behind them.
- **Console.** F3 `pda open|show|hide|unlock|reset <app>` (29995) shows or hides home icons.
- **Precedent.** LOGUSS's Common Sense Cargo Manifest (Workshop 3790481501) adds a PDA app
  exactly this way: a `pda_apps` entry, the two tooltip strings, an icon and a Harmony
  prefix on `OpenApp`. Our earlier [locator research](locator-research.md#app-integration)
  reached the same route.

## Framework design: `PdaApps` (proposed)

A shared service any Phobos mod can register an app with:

```csharp
PdaApps.Register(new PdaApp {
    Name = "phobos_bank",                       // the pda_apps entry's strName
    Open = () => BankPanel.Show(),              // opens a GUIData + ConsoleShell panel
    Unavailable = () => null                    // or the reason it cannot open now
});
```

- **One Harmony prefix on `GUIPDA.OpenApp(string)`.** For a registered exact name it
  closes the PDA (`State = Closed`), opens the panel and returns false; every other name
  is untouched. If `Unavailable` gives a reason, the crew log says it (no silent refusal).
- **The content mod ships** `data/pda_apps/<mod>.json`, `data/strings/<mod>.json` with the
  two tooltip strings, and an original icon at `images/pda_apps/<name>.png` (the unused
  vanilla `IcoPDACash` is not ours to reuse, per the
  [asset policy](asset-generation-policy.md)).
- **The panel** follows the Letters and Crew pattern: a `GUIData` on
  `CrewSim.goIntUIPanel` with a `ConsoleShell` inside, and a `RaiseUI` restore prefix keyed
  by the panel's name.
- **Native checks when built:** `OpenApp(string)` is static and public; `JsonPDAAppIcon`
  has its four fields; the tooltip key rule; `GUIPDAHomepage.UnHideApp` exists.
- **Owner checks when built:** the icon shows on the home screen; tapping it closes the PDA
  and opens the panel; Escape and Close return to the game; the PDA hotkey while the panel
  is up; switching crew; save and reload. The icon does not prove the player wears a PDA,
  as the locator research noted.
- **Not chosen:** a screen inside the PDA frame. It would look native, but it means
  parenting our own objects under the PDA's scene hierarchy and tracking its private
  `State` changes; nothing in the game supports a new state.

## How the game's money and debt work (observed)

- **Money** is the player's `StatUSD` condition (`Ledger.CURRENCY`). Faction scrip
  (`StatCCREScrip`, `StatGalileanConfederacyScrip`) is separate.
- **The ledger** (`Ledger`, 72993) is static and saved with the game. A line (`LedgerLI`,
  73918) has payee, payor, amount, description, object id, currency, time, paid time and a
  frequency: one-time, hourly, shiftly, daily, monthly, yearly or **mortgage**.
  `RecordTransaction` writes a paid line and a log message but moves no money; callers
  change `StatUSD` themselves.
- **Repeating lines** are processed at clock boundaries in `StarSystem.Update`
  (125995); a line dated in the future waits. A single large time jump processes only the
  largest boundary crossed, so daily and shiftly lines can be under-billed by one big
  native skip (our stepped time-skip avoids this when machines or orders are running).
- **Mortgages are the game's loans.** 720 payments, one per shift, at 0.21% a shift
  (`MathUtils.MortgagePaymentPerShift`, 139274). The ship broker sells on mortgage with at
  least 50% down (191332). Character creation can start the player with a mortgage to
  "Ogiso's Bank" (99796; eight life events, from 97,395 to 910,636). Fines for public
  disorder are issued as 15,000-credit mortgage debts (`data/ledgerdefs`).
- **A native mortgage charges no interest overall.** Each shift's instalment is sized by
  the 0.21% annuity formula over the shifts left of a fixed 15,552,000-second term
  (`MortgagePaymentPerShift(LedgerLI)`, 139280), but paying it takes the whole instalment
  off the balance (`Ledger.PayLI`, 73546). The balance therefore falls faster than an
  interest-bearing loan's, and the total repaid equals the amount borrowed; only late
  fees add to it. The rate and term are the same for every mortgage line and cannot be set
  per line, so any lender's own interest has to be charged by us.
- **Paying and not paying.** The Finances window (`GUIFinance`, 130613, opened from the
  money button) lists and pays bills; a prepay window lowers a mortgage's principal. At
  each shift end every unpaid player line gains a **17.5% late fee** (`Ledger.Skip`,
  73428), with a warning five minutes before. Selling a mortgaged ship repays the lender
  from the sale (`Ledger.AddDownPayment`). Unpaid docking fees block undocking. There is
  **no repossession and no debt collector** in the game.
- **Compatibility.** Common Sense Finances (Workshop 3790482485) pays affordable bills
  automatically through the same ledger, so it would pay our loan instalments too.

## Loan design for Phobos Banking (proposed)

- **A loan is native ledger lines** (owner choice). The balance is a `Frequency.Mortgage`
  line whose payee is the lender's name: the game then bills its instalments each shift,
  shows them in the Finances window with the prepay window, applies the late fee, lets
  Common Sense Finances pay them, and repays it from a mortgaged ship's sale. Because the
  native mortgage carries no interest (above), the lender's interest is a second, one-time
  line our loan service adds at each shift boundary: the lender's rate times the balance
  the mortgage line holds then, with `strObjId = PhobosBank.<loan id>`. That id must be
  unique, because the game's duplicate check (`LedgerLI.Same`) treats a missing id as
  matching anything. A `Shiftly` line cannot carry interest either: its amount is fixed
  when it is created. When a ship is collateral the mortgage's description holds its
  registration; the game finds a ship's mortgage by substring (`GetMortgageForShip`), so a
  registration that begins another is a trap to test for.
- **Our loan book** on the player, `PhobosState.PhobosBank` through `ObjectStateStore`:
  one key per loan holding lender, principal, rate, term, opening time, collateral and
  status, joined with `|`. Values hold no `,` or `=` and stay within 512 characters
  (`ObjectStateStore.MaxValueLength`); unknown keys are kept. Each lender keeps a small
  standing score of our own (not a game faction).
- **Interest** is computed from the time elapsed between game epochs, never by counting
  checks, so time-skips cannot add or lose interest. A game day is 87,658.125 seconds.
- **Accredited lenders** charge less, need a standing tier with a game faction (read
  through Framework's `GameFacts.Standing`), take collateral, lend for longer, and use the
  game's own late fee only.
- **Non-accredited lenders** charge more, ask no standing and lend for shorter terms.
  For now (owner choice) default carries only what the game already does: the late fee,
  growing each shift, and the debt standing in the Finances window. Story reactions
  (letters, news, small talk) through flags, and small standing changes through the
  game's own faction scores, stay within that rule. Repossession, collectors or a wanted
  state are later work. Never a new player condition, plot or pledge
  ([story rules](story-system-design.md)).
- **Value rules.** Borrowing creates no value: interest is a cost to the player, and no
  loop may earn from it. `scripts/audit-economy.py` is rerun if any price or bill changes.
- **Data.** A `lenders` pack (`mods/PhobosBank/framework/lenders.json`): lender id, display
  name, accredited flag, rate per shift, smallest and largest principal, term, standing
  requirement, collateral rule and home station. It needs a C# validator, the Python
  mirror, a JSON Schema and a row in the editing guide. Once a loan can be saved against
  a lender's terms, those terms are frozen by content hash, as recipes are.

## Financing at the broker

Owner question (6 October 2026): how could a loan help fund a ship purchase, neatly, in
the game's own broker window?

**How the broker sells (observed).**

- Ship brokers are the `ItmKioskOKLGShipBroker01` kiosk, placed at OKLG (the Bureaus) and
  at Mars (Heifei District). The same window (`GUIShipBroker`, 191931) also serves the
  real-estate kiosk at Venus (Porto do Encantado), which sells apartments on the same
  terms. The ship broker kiosk also carries Ogiso's ship-rating panel.
- Buying opens `ConfirmBuyShipPopup` (191281). For a used ship or apartment
  (`TransactionTypes.Mortgage`) it shows a mortgage area: a slider for the share paid now
  (at least 50%, or 0% for a special offer, 191332), the percentage, the price paid now and
  the payment per shift from the vanilla formula (191348). A derelict is cash only.
- Confirming calls `GUIShipBroker.UpdateCash` (192470): the player pays the share now to
  the kiosk, and the rest becomes a `Mortgage` ledger line with the **kiosk's own name as
  payee** and "Mortgage on <registration>" as its description (192493).
- Selling (`ConfirmSellShipPopup`, 191368) shows the existing mortgage and repays it from
  the sale (`Ledger.AddDownPayment`).

**Proposed integration (agent design).** Four small, separate pieces, each falling back
to the game's own behaviour:

1. **A lender choice in the mortgage area.** A postfix on
   `ConfirmBuyShipPopup.ShowPanel` adds one row, built by cloning the popup's own text and
   button so it matches the window: "Lender: <name>". Tapping it cycles through the
   broker itself and the Phobos lenders available here to this player (see
   [Local lenders](#local-lenders-through-the-story-system)). Choosing the broker leaves
   every vanilla figure untouched.
2. **The lender's terms on screen.** With a Phobos lender chosen, the slider's lowest share
   follows that lender (for example 30% for an accredited lender with good standing), and
   the payment line reads honestly: the base instalment plus that lender's interest per
   shift. This is a postfix on `OnMortgageSliderchanged` and on `ShowPanel` setting the
   slider's minimum.
3. **The loan on confirm.** A postfix on `GUIShipBroker.UpdateCash` finds the mortgage
   line it has just added (by the registration) and, for a Phobos lender, changes its
   payee to the lender and records the loan in our loan book. Prepay, sale escrow and late
   fees keep working because the line is still the game's own mortgage.
4. **Pre-approval in the app.** The bank app can ask a lender for a ship or home loan up to
   a limit, valid for a while; the broker's popup then opens with that lender chosen. This
   keeps the decision in the bank app, and the broker window only shows its result.

**Not chosen for broker financing (6 October 2026):** a cash advance taken in the app
and spent at the broker as a cash purchase. It needs no broker changes, but the loan
would not be tied to the ship, so the game's sale escrow would not repay it. That was a
broker-specific choice, not a decision against a general-purpose remote credit product;
the owner reopened that question on 7 October 2026 below.

**Unknowns to test in play.** The popup is a Unity prefab we cannot read offline: the row's
place and size need a hierarchy dump at runtime, and other mods touching the broker are
possible. The apartment and the ship share one code path, which the owner should check
both ways.

## Existing debts in the app

Owner choice (6 October 2026): the app shows the debts the game already tracks.

- **Unpaid bills:** `Ledger.GetUnpaidLIs(null, <player id>, null, false)` (73664, public)
  returns the player's unpaid one-time lines, including mortgage instalments, fines and
  docking fees.
- **Mortgage balances:** the game keeps them in a private list; the public save snapshot
  `Ledger.GetJSONSave()` (73870) holds them as lines with the mortgage frequency, read-only.
  It is built on demand, which is fine for a window the player opens.
- **Paying:** the app does not pay bills itself; an "Open Finances" button calls the game's
  own `CrewSim.objInstance.ToggleFinances()` (40537), so payment, prepay and the late-fee
  rules stay the game's.
- Ogiso's Bank (the starting mortgage), the broker kiosks and fines appear under their own
  names; our lenders' loans appear beside them with their terms from our loan book.

## Local lenders through the story system

Owner question (6 October 2026): if lenders are local to stations or regions, can they
build on the story code and schema made today? **Yes, for most of it.** What follows is
observed in our code, with the Framework additions the bank mod would need.

**What can be reused as it is.**

- **Places.** Framework's story pack ships a `places` table of the game's stations: twelve
  regional places (OKLG, Port Mojave on Ceres, Port Independence on Ganymede, Porto Nuevo on
  Europa, Cassini Spaceport on Titan, Port Yangshan on Mars, Upsilon Docking on Deimos, Long
  Beach Terminal on Venus, Port Shajiang on Luna, Qincheng Station on Mercury, Qiantangmen
  and Panmen) and 29 parts within them (the Titan Shipyards, the Flotilla, the CCRE offices,
  Corsair's Hollow and others), each with its station id, body, region label and factions
  (`mods/PhobosFramework/framework/story.json`). `StoryPlaces` (public) turns a station id
  into a place and knows which region contains it. A lender's `home` would be one of these
  keys.
- **Where the player is.** The story runner already finds the place the player is docked
  at and the region around them (`GameFacts.Locate`, `StoryArcs.cs`), the same rule that
  starts local arcs and weights local news. A lender is offered while the player is at its
  home place or, for a regional lender, anywhere in that region.
- **Requirements.** The story `requires` block (`StoryRequires`) already reads standing
  with the game's factions by tier, flags, finished arcs, places, regions, crew skills,
  months and more, and `StoryRules.Blocked` (public) says why one is not met. A lender's
  `requires` can be that same block, with the same validator and the same wording, so an
  accredited lender may ask "Warm with the Galilean Confederacy" in exactly the story
  form.
- **People.** A lender's officer can be a story `person` with a home and a `face` (Framework
  0.121.0), so letters from the bank show the officer's face and From line in the GOALS
  list and the Letters window.
- **Threads, news and adverts.** A lender can be a story `thread` whose place and
  requirements its news, adverts, small talk and letters inherit. Its adverts play on local
  TVs; news can follow what the player did (`requires.flags`).
- **Letters and replies.** Loan letters (an offer, a reminder, a final notice) can be story
  arcs with replies (Framework 0.122.0), for example "Pay now" and "Ask for time".

**What the bank mod's own code must do.** Interest, ledger lines, the loan book, the broker
integration and the app; story packs cannot create ledger lines or compute interest, and a
story reward is capped at 50,000 credits.

**Framework additions this needs** (proposed; each small, public and documented, so any
mod can use them):

- `StoryLocation`: where the player is now (the docked place, the region, whether they are
  near a place), opened up from the story runner's private `GameFacts`.
- `StoryGates.Blocked(requires)`: a `requires` block checked against the live game and the
  player's story record, with the reason.
- `StorySchema.ValidateRequires`: the requirement validator made public, so another pack
  (the lenders pack) can carry a `requires` block checked the same way.
- `StoryFlags.Set` / `Clear` / `Has`: so the bank can mark what happened ("late with
  Corvane Mutual") for any story pack to react to.
- `StoryArcs.Begin(arcId)`: start an arc from code, still honouring its requirements, so a
  late payment can open a letter.
- Load order: the lenders pack checks its places and people after the story library is
  built (`FrameworkLifecycle.ContentLoaded`), as story packs check items today.

**Related fix.** The story system counts a day as 86,400 seconds; the game's day is
87,658.125 (`MathUtils`). Loan terms would use the game's day, and the story rules should
be corrected in the same round.

## What a new content mod needs (from Medical and War Has Been Declared)

- **Source:** `src/PhobosBank/` with `Plugin.cs` (plugin id, Framework dependency),
  `Text.cs` (the three-argument `Translations.Register`; no equipment names),
  `Content.cs`, the pack class and schema, and a csproj that embeds `en.json` and the
  packs.
- **Native folder:** `mods/PhobosBank/` with `mod_info.json`, a **mandatory `data/`
  file** (a marker condition, as War Has Been Declared ships), the packs under
  `framework/`, `CHANGELOG.md`, `THIRD-PARTY.md` and `preview.png`.
- **Publishing:** `workshop/PhobosBank/page.bbcode`, generated release notes, a
  `config/workshop-publishing.json` entry, version and minimum-Framework entries in
  `config/maintained-constants.json` and `config/mod-dependency-minimums.json`.
- **Scripts and tests:** `scripts/build-bank.ps1`; the mod added to the installer,
  Workshop preparation, uploader and removal scripts; `tests/PhobosBank.Tests`; native
  checks; the installer test's load order; a pack test.
- **Ledgers and docs:** every key in the language ledger, every source file in the
  performance ledger, an item-reference entry even with no items, a player guide, and the
  indexes.

## Names (agent proposals)

All names still need the [branding record](equipment-branding.md)'s collision search
before use.

| Role | Proposal | Note |
| --- | --- | --- |
| Mod | Phobos Banking | id `phobosgekko.ostranauts.bank`, folder `PhobosBank` |
| PDA app label | CREDIT | short, fits the vanilla labels' style |
| Accredited lenders | Corvane Mutual; Halcyon Bond | registered, slow, respectable |
| Non-accredited lenders | Stillwater Advances; the Narrow Ledger | quick money, worse terms |
| Vanilla institutions | Ogiso's Register; Ogiso's Bank | the game's; we refer to them, never speak for them |

Chosen names go in the branding record even though they are services, so later equipment
cannot collide.

## Lore anchors for ChatGPT

- **Ogiso's Register** records ship ownership and gives ships an OSR safety rating; its
  kiosks nag unregistered captains, and its rating panel sits on the ship broker kiosk.
  **Ogiso's Bank** holds starting ship mortgages.
- Ship brokers trade at OKLG's Bureaus and in Mars's Heifei District; Venus's Porto do
  Encantado has a real-estate broker selling apartments on mortgage.
- Fines become debts paid by the shift; mortgages run 720 shifts; a late shift costs
  17.5%.
- Station owners and their law: OKLG (Ayotimiwa Ship Breaking Co., AyoSec), CCRE,
  Galilean Confederacy (Ceres and Jupiter), Titan, Xinhua (Mars), Venus (Newcal PD).
- The game's own description says debt matters to its scavengers.
- **What a story pack can do for a bank arc:** pay up to 50,000 credits once, test or take
  a credit balance, set flags and change standing a little. It cannot require a balance as
  a gate, schedule payments or run interest; those need the mod's code.

## Answered questions

The first round's four questions were answered by the owner on 6 October 2026; the
answers are under the owner choices at the top.

## Round 1 as built

Framework 0.126.0 and Phobos Banking 0.1.0, 6 October 2026, held drafts. Checked offline
(builds, `tests/PhobosBank.Tests`, `PdaAppChecks` in the Framework tests and
`BankNativeChecks` against the installed game); owner gameplay checks pending.

- **Registration differs from the proposal (agent choice).** The content mod ships no
  `data/pda_apps` or `data/strings` files. `PdaApps.Register` takes the name, an icon path
  and the label, title and tooltip as catalogue lookups, and Framework writes the icon
  entry and both tooltip strings into the game's tables after every content load. The
  label then follows the player's language, the text lives in the translation catalogue
  like all our player text, and a disabled or removed mod leaves no icon behind. Banking
  registers only when its package is enabled in the game's mod list.
- **One opener instead of `Open` plus `Unavailable`.** `Open` returns null when the app
  opened, or the reason it did not, which Framework writes to the player's log. An
  exception is logged and the player is told the app would not open.
- **Name rules.** Lowercase letters, digits and underscores, starting with a letter, never
  one of the game's own app names. The native check compares Framework's list of game app
  names with the literals in `GUIPDA.OpenApp` itself (the switch also passes `actions`, the
  job-paint page, which is not an app).
- **Loans are read from the private list** (`Ledger.aMortgage`, by reflection, checked
  natively) rather than from `GetJSONSave`, which copies the whole ledger. Bills are the
  public `GetUnpaidLIs(null, player, null, false)`; regular charges are the repeating lines
  the same call adds with its repeating flag set.
- **Mortgage figures.** The panel shows the game's own `MathUtils.MortgagePaymentPerShift`,
  capped at the balance as the game caps the bill it raises. `BankRules` mirrors the
  formula for the offline checks and the native check compares the two across a
  mortgage's life. Observed in that comparison: the game counts days of 87,658.125 s as
  four shifts each, so a new mortgage has 709 instalments to run, not the 720 its constant
  names, and with one shift left the raw formula asks for 1.0021 times the balance before
  the cap. Paid instalments come straight off the balance (`Ledger.PayLI`); the game adds
  no interest to it.
- **Late fees.** The game adds 17.5% of every unpaid one-time bill at each shift change
  (`Ledger.Skip`, called from `StarSystem.Update`); late lines are recognised by the game's
  own `GUI_FINANCE_OVERDUE` and `GUI_FINANCE_LATE` wording, so the check follows the
  player's language.
- **Icon and cover.** Drawn by `scripts/export-bank-art.py` in the style of the game's own
  icons (a white disc with a black glyph, 256 px); the Workshop cover's scene is a
  placeholder from the same script while the mod is held.

## Rounds 2 and 3 as built

Framework 0.127.0 (round 2) and Phobos Banking 0.2.0 (round 3), 7 October 2026, held
drafts, done while the owner slept (owner instruction: proceed without waiting for
answers; agent choices labelled). Checked offline and natively; borrowing not yet
seen in play.

- **Framework story services (0.127.0)** as proposed under
  [Local lenders](#local-lenders-through-the-story-system): `StoryLocation`, `StoryGates`,
  public `StorySchema.ValidateRequires` and `StoryLibrary.UnknownReference`,
  `StoryFlags` and `StoryArcs.TryBegin`; and `GameClock`, with the story day set to the
  game's 87,658.125-second day. Details in the
  [story system record](story-system-design.md).
- **The lenders pack** (owner direction, 7 October 2026: use the data-pack system so
  players can add lenders). Schema `lenders`, file `mods/PhobosBank/framework/lenders.json`,
  C# validator `LenderSchema`, Python mirror in `scripts/validate-data-packs.py`, JSON
  Schema `schemas/lenders.schema.json`, guide section
  [Adding a lender](../editing-data-files.md#adding-a-lender). Fields: name, pitch,
  accredited, home (a story place), person, requires (a story block), ratePerShift,
  minPrincipal, maxPrincipal, maxLoans, offers (cash, ship, home), minDownShare.
- **Terms copied, not frozen (agent choice).** The research proposed freezing published
  terms by content hash, as recipes are. Instead each loan copies its rate into the loan
  book when taken, so the pack stays freely editable and no running loan changes.
- **A loan** is the game's own `Mortgage` line, payee the lender's name, description
  `<lender> loan (#n)`: the game raises its instalments on its own schedule (the first
  as the loan is made, as the broker's do), takes payment and prepay in the Finances
  window and applies the late fee. The loan number closes with a bracket because the
  game matches an instalment to its loan by looking for the loan's description inside
  the instalment's (`Ledger.GetMortgageForPayment`): `(#3)` never matches inside
  `(#30)`. A native check runs the game's own matcher to prove it.
- **Interest** is a one-time bill at each shift change, the rate on the balance the
  mortgage line holds then, counted with `GameClock.ShiftCount` so a time-skip bills
  every shift it crossed in one line. Its wording never contains the loan's
  description, so paying it can never pay the loan down (also proved natively). Interest
  is simple, on the balance only; unpaid interest grows by the game's late fee alone.
- **The loan book** (`PhobosState.PhobosBank` on the player): `next`, `loan.<n>` as
  `1|lender|payee|description|principal|rate|opened|billedTo|interestBilled|state|kind|collateral|closed`
  and `approval` (round 4). Unknown keys and unreadable loans are kept exactly; a record
  from a newer version stops borrowing and billing until it can be read.
- **Shipped lenders (agent choices):** Corvane Mutual (OKLG, OKLGCorp at least neutral,
  0.025% a shift), Halcyon Bond (Mars, Xinhua at least warm, 0.02%), Aerie Savings Union
  (Venus, anyone, 0.03%). A whole loan paid on time costs 5.4% to 8.1% in interest (the
  Banking tests print the figures). Branding in the
  [branding record](equipment-branding.md).
- **Value:** borrowing creates no value; interest is a cost and no loop earns from it.
  No price, bill or loot changed, so the economy audit is unaffected.
- **Flags:** `bank-<lender>-borrowed`, `bank-<lender>-repaid` and `bank-<lender>-late`
  (set while any of the lender's bills is late), for round 5's stories.

## Round 4 as built

Phobos Banking 0.3.0, 7 October 2026, held draft (owner absent; agent choices labelled).
It follows [Financing at the broker](#financing-at-the-broker) with one change of
approach for safety: no rows are added to the broker's window, whose prefab cannot be
read offline.

- **Pre-approval in the app** (piece 4 as proposed): `Financing.PreApprove` records the
  lender, ship or home, the limit (what the lender would lend now) and an expiry one
  game day later (agent default) as the loan book's `approval` field. The loan poll
  drops an expired one with a crew-log line.
- **The lender's terms in the window** (pieces 1 and 2, simplified): a postfix on
  `ConfirmBuyShipPopup.ShowPanel` lowers the slider's minimum (`sldrMortgage.minValue`)
  to the lender's least down payment, raised where needed so the financed part stays
  within the approval and above the lender's smallest loan (`LoanRules.MinDownShare`).
  The window's own update runs from the slider. The terms go to the crew log rather than
  a new row in the window; the payment line still shows the game's own instalment.
  Kind: a kiosk definition containing `ResBroker` is the real-estate broker.
- **The loan on confirm** (piece 3): a postfix on `GUIShipBroker.UpdateCash` finds the
  mortgage line the broker just wrote (payor the user, payee the kiosk, description
  "Mortgage on <registration>"), moves it and the first instalment the game raised with
  it to the lender, and opens a loan of kind `ship` or `home` with the registration as
  collateral. The description is unchanged, so `GetMortgageForShip` and the sale escrow
  still repay the lender.
- **Left to the broker** (crew log says why): no pre-approval, the other kind, the lender
  not trading here or no longer lending, a special offer (the broker's own 0% down is
  better), a price too small for the lender's smallest loan, paid in full, or more
  financed than approved.
- **Owner checks:** OKLG Bureaus with Corvane Mutual, Heifei District on Mars with
  Halcyon Bond (needs Warm standing with Xinhua), and Porto do Encantado on Venus with
  Aerie Savings Union for an apartment; then sell a financed ship and check the lender is
  repaid.

## Round 5 as built

Phobos Banking 0.5.0, 7 October 2026, held draft. The story writing requested in the
[banking stories handoff](banking-stories-handoff.md) is in the pack; no gameplay or
publication is implied.

- **Two unregistered lenders** (agent choices): Stillwater Advances at Port Independence,
  Ganymede (0.08% a shift, about 21.5% over a whole loan, ships from 25% down) and the
  Narrow Ledger at Corsair's Hollow (0.15%, about 40%, cash only, only while docked
  there). Neither asks standing. Default still has only the game's consequences (owner
  choice).
- **Events start story arcs** by name: on `borrowed`, `late`, `late-long` (three game days,
  agent default) and `repaid`, Phobos Banking calls Framework's `StoryArcs.TryBegin` for
  `bank-<lender>-<event>` if the story library has it. TryBegin honours the arc's
  requirements and ignores the limit on arcs starting by themselves. Flags are set first,
  so an arc may require them. `bank-<lender>-late-long` joins the flags.
- **Lender threads have no place**, so their letters reach the player anywhere; adverts
  and news take the lender's place. Each lender names its officer (`person`).
- **The story pack** (`mods/PhobosBank/framework/story.json`, registered in Awake) has
  four repeatable event arcs for each lender, local adverts, station news and small talk,
  and one encyclopedia article on credit and lenders. The letters and their replies do
  not pay debts or promise any outcome beyond the game's existing late fee and ledger.

## Remote credit line: owner direction and research

Owner direction (7 October 2026): consider borrowing from anywhere in real time, at a
higher cost or otherwise worse terms than borrowing directly at a lender's location,
with a familiar credit-card feel. This is research only: no product, balance, price,
provider, eligibility rule or implementation has been chosen.

### What the modern comparison suggests

The closest low-friction analogy is a **credit-card cash advance**, not a card purchase.
The U.S. Consumer Financial Protection Bureau (CFPB) describes cash advances as commonly
having a separate, lower limit, an extra fee and a higher interest rate; interest commonly
starts on the transaction date rather than after a grace period ([CFPB, “Can I withdraw
money from my credit card at an ATM?”, reviewed 2 September 2026](https://www.consumerfinance.gov/ask-cfpb/can-i-withdraw-money-from-my-credit-card-at-an-atm-en-34/)).
For card purchases, the CFPB describes a statement cycle and due date; a grace period may
apply if the balance is paid in full, while cash advances usually do not receive that
period ([CFPB, “What is a grace period for a credit card?”, reviewed 23 September 2024](https://www.consumerfinance.gov/ask-cfpb/what-is-a-grace-period-for-a-credit-card-en-47/);
[CFPB, “Know Before You Owe: Credit Cards”, updated 12 December 2024](https://www.consumerfinance.gov/data-research/credit-card-data/know-before-you-owe-credit-cards/)).
These are U.S. consumer-finance explanations, not Australian legal advice, Ostranauts
canon or a mandate for the game. They suggest that a remote draw should disclose its fee,
rate, limit and first payment plainly, and should not quietly borrow the cheaper purchase
grace period's language.

### Product shapes to consider

1. **Remote revolving cash line — best fit for the owner's card analogy.** The PDA shows
   one account limit, current balance, available amount, cost of a draw and next amount
   due. The player can draw cash anywhere up to the remaining limit; the cash goes into
   the game's existing `StatUSD`, and payment remains in Finances. The balance can be
   borrowed again as it is repaid. At a lender's counter, a separate secured loan can
   offer a larger limit, lower price or longer term.
2. **Remote cash loan per draw — simpler bridge, weaker card feel.** Each app draw opens
   an ordinary Phobos Banking loan under the existing mortgage and shift-payment model.
   It can be more expensive and capped, but it is another fixed loan rather than a
   revolving account; repeated borrowing creates multiple ledger lines and obligations.
3. **Pay-by-card at vendors — most literal card, widest scope.** Charge purchases
   directly to credit rather than giving the player cash. This would need to cover each
   relevant purchase route, including kiosks and the ship broker, and clearly separate
   financed purchases from cash advances. It would create many chances for a purchase to
   bypass the credit limit or be charged twice. I would not start here.

### Suggested shape, still unapproved

- Make the service available through the PDA wherever the player is. The player should
  be able to make a draw immediately once eligible; a physical branch should improve the
  terms, not be required to access the remote product. Whether the player first opens or
  accepts the account at a station, or can enrol from the PDA, remains an owner choice.
- Keep the global offer to one lender or one clearly identified credit service. Do not
  make all five station lenders appear to have counters everywhere; their homes and
  local characters are part of what distinguishes them. A registered lender could offer
  remote account access while keeping its staffed, secured loans local. A separate
  system-wide clearing service is another lore option, but would introduce an institution
  that needs its own identity and story rationale. Neither network infrastructure nor
  instant inter-station settlement has been verified as canon here; present immediate
  credit as a gameplay abstraction unless a lore source is established.
- Make the remote offer less favourable in a small number of visible ways: a lower
  ceiling and higher total borrowing cost than a comparable loan offered face to face.
  A remote convenience fee or higher rate could supply the price difference. Do not stack
  several surprise penalties; show the amount drawn, fee, rate, next amount due and
  estimated total cost before the player accepts. Leave all exact values for a later
  balance pass.
- Do not add repossession, collectors, seized cargo, access restrictions or other new
  default effects. The existing 17.5% shift late fee is already consequential; adding a
  remote-credit penalty on top would depart from the owner's recorded default choice.
  Keep payment in the game's Finances window. Following the [story handoff](banking-stories-handoff.md),
  letters may explain a bill or ask the player to act, but a reply cannot pay it and no
  story may promise consequences the code does not implement.

### My recommendation and reservations

I recommend designing toward the revolving cash line in option 1, while keeping its
draw as cash rather than trying to charge every vendor. That gives the player the
emergency flexibility requested, keeps the current purchase systems intact, and leaves
local lenders with a meaningful advantage: collateral-backed terms, higher limits and
longer repayment when the player travels to them. The PDA can be the always-reachable
account interface; the fiction can say the line is already authorised against the
captain or ship account, without claiming a new universal communications technology.

My main reservation is that the existing Banking loans are finite mortgages with
shiftly instalments; they are not a revolving credit balance with a statement and minimum
payment. A faithful card-like line therefore needs a distinct account balance, limit,
draw and repayment rules, plus correct billing across time skips. Reusing one fixed loan
per draw is cheaper but can turn emergencies into a pile of shift bills and does not
deliver the requested card experience. I would rather keep the first implementation
small and honestly call it a cash-advance line than label fixed loans a credit card.

I also recommend resisting the real-world convention of making the product convenient
by hiding its cost in dense terms. A single explicit draw fee or a plainly higher rate,
combined with a modest cap, is easier to understand. The game's existing late fee is
severe enough that the remote line should not make missed payments even harsher.

### Decisions still open

- Can any eligible player enrol and draw from the PDA at any location, or must they
  first meet a lender or accept its offer in person?
- Should remote credit be one reusable account balance with a minimum payment, or a
  sequence of ordinary, individually visible loans? The former matches the request;
  the latter reuses more of the current loan model.
- Is the provider one current lender with remote account access, or a new system-wide
  credit service? Keep the other lenders local unless their lore and data explicitly say
  otherwise.
- Does remote borrowing deliver cash only, or should a later feature add direct merchant
  charges? Cash is the recommended first boundary.
- What repayment interval and amount can the native Finances window support cleanly for
  a revolving balance? This needs code-path research before terms are drafted.
- Does the owner want credit available from a new save's beginning, or unlocked by an
  account/story event? No good-standing gate is proposed yet; denying emergency access
  for already being in trouble could undermine the feature's purpose.

## Remote credit line as built

Phobos Banking 0.6.0, 7 October 2026, held draft. Owner choices that day settled the open
decisions above: **a new system-wide service** (not one of the five lenders), **opened from
anywhere** through the PDA, **one revolving balance**, and **a draw fee added to the
balance plus a higher rate**. Claude's design, against the research above:

- **Repayment uses the game's own Prepay.** The decompile shows `PrepayWindow.OnPrepayConfirm`
  paying an amount off a mortgage line, removing the selected instalment and resetting
  the line's start time (`fTime`), which spreads the rest over a fresh full term. So the
  balance is one native mortgage line. Its shift instalment is the minimum payment,
  Prepay pays more, and a draw adds the amount and its fee to the line and resets its
  start time as Prepay does. This answers the question of what repayment interval and
  amount the Finances window can support: per shift, with Prepay for any extra amount. No
  statement cycle or grace period was imitated; the game bills by the shift.
- **Data:** a `creditLines` table in the lenders pack (limit, ratePerShift, drawFee,
  minDraw, optional person and requires; ids shared with the lenders, at most 31
  characters). **Orrery Credit** (agent choices): limit 25,000, 0.06% a shift (dearer
  than the registered lenders' 0.02 to 0.03%, cheaper than the Narrow Ledger's 0.15%;
  raised to 0.10% in Phobos Banking 0.8.1, owner decision of 8 October 2026, because
  0.06% undercut Stillwater Advances' 0.08%), a
  3% draw fee, smallest draw 500, no requirements.
- **Book:** `account.<id>` holds the terms copied at opening; each balance cycle is a
  loan of kind `line`, so interest, the late flags and story events reuse the loan
  service. Opening sets `bank-<id>-line-open` and fires `line-opened`.
- **Not added:** merchant card payments, new default consequences, a standing gate, limit
  growth. A later round could raise limits for good payers (the per-lender standing score
  set aside earlier).
- **Fix found on the way:** Prepay paying a loan off in full removes its mortgage line,
  which 0.2.0 to 0.5.0 reported as "settled" by a ship sale. A cash loan or a line is now
  repaid when its line is gone; a ship or home loan is settled only when the collateral
  has changed hands (`LoanRules.Closed`).

## Proposed rounds

1. **Framework `PdaApps` and the debts screen** (built; see above). The PDA icon, the panel, and the
   player's existing debts with an "Open Finances" button: proves the hosting in play
   before any lending exists.
2. **Framework story services and the day length** (built in Framework 0.127.0). `StoryLocation`, `StoryGates`, the
   public requirement validator, `StoryFlags` and `StoryArcs.Begin`; the story day set to
   the game's day.
3. **Phobos Banking: lenders and the loan book** (built in Banking 0.2.0). The `lenders` pack (home place,
   `requires`, terms), one accredited lender, the per-shift interest line, the app's loan
   screens.
4. **Financing at the broker** (built in Banking 0.3.0). The lender row, terms on screen, the loan on confirm and
   pre-approval; owner tests at OKLG, Mars and Venus.
5. **Non-accredited lenders and stories** (built in Banking 0.5.0; see above). Higher-cost lenders, flags for what happened,
   and the completed story pack (officers as people, adverts, letters and local station text).
