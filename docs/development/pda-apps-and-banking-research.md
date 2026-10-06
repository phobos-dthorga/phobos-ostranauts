# PDA apps and a banking mod: research

Research record, 6 October 2026. The owner asked for Phobos mods to add their own apps to
the wrist PDA, beginning with a banking service that offers loans from accredited and
non-accredited lenders, as a separate mod that may grow beyond loans. ChatGPT writes the
lore; Claude designs the code and proposes names. **Nothing here is built or tested in
play.** Observations come from the game 1.0.1.5 decompile (kept in ignored `.local`, line
numbers below refer to it), the game's own data files and our code. Proposals are marked
as such; owner choices carry the date.

**Owner choices (6 October 2026):**

- The bank app is a full Framework panel opened from its PDA icon, as the game's own
  Roster and Duties apps leave the PDA.
- Loans are the game's own ledger lines plus our own loan book.
- Our lenders are our own, beside the game's Ogiso's institutions.

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
- **Paying and not paying.** The Finances window (`GUIFinance`, 130613, opened from the
  money button) lists and pays bills; a prepay window lowers a mortgage's principal. At
  each shift end every unpaid player line gains a **17.5% late fee** (`Ledger.Skip`,
  73428), with a warning five minutes before. Selling a mortgaged ship repays the lender
  from the sale (`Ledger.AddDownPayment`). Unpaid docking fees block undocking. There is
  **no repossession and no debt collector** in the game.
- **Compatibility.** Common Sense Finances (Workshop 3790482485) pays affordable bills
  automatically through the same ledger, so it would pay our loan instalments too.

## Loan design for Phobos Banking (proposed)

- **A loan is a native ledger line** (owner choice): `Frequency.Mortgage` for amortised
  loans, or `Shiftly` for fixed instalments. Payee is the lender's name; `strObjId` is
  `PhobosBank.<loan id>`, unique, because the game's duplicate check (`LedgerLI.Same`)
  treats a missing id as matching anything. The Finances window shows it, the late fee
  applies, Common Sense Finances can pay it, and a mortgaged ship's sale repays it. When a
  ship is collateral the description holds its registration; the game finds a ship's
  mortgage by substring (`GetMortgageForShip`), so a registration that begins another is a
  trap to test for.
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
- **Non-accredited lenders** charge more, ask no standing, lend for shorter terms and
  carry consequences we author inside the game's own machinery: standing changes through
  the game's faction scores with criminal factions such as `OKLGCrim` and `VenusCrim`,
  news through a `story` pack, and a wanted state only where a vanilla crime trigger
  already fits. Never a new player condition, plot or pledge
  ([story rules](story-system-design.md)).
- **Value rules.** Borrowing creates no value: interest is a cost to the player, and no
  loop may earn from it. `scripts/audit-economy.py` is rerun if any price or bill changes.
- **Data.** A `lenders` pack (`mods/PhobosBank/framework/lenders.json`): lender id, display
  name, accredited flag, rate per shift, smallest and largest principal, term, standing
  requirement, collateral rule and home station. It needs a C# validator, the Python
  mirror, a JSON Schema and a row in the editing guide. Once a loan can be saved against
  a lender's terms, those terms are frozen by content hash, as recipes are.

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
  kiosks nag unregistered captains. **Ogiso's Bank** holds starting ship mortgages.
- Fines become debts paid by the shift; mortgages run 720 shifts; a late shift costs
  17.5%.
- Station owners and their law: OKLG (Ayotimiwa Ship Breaking Co., AyoSec), CCRE,
  Galilean Confederacy (Ceres and Jupiter), Titan, Xinhua (Mars), Venus (Newcal PD).
- The game's own description says debt matters to its scavengers.
- **What a story pack can do for a bank arc:** pay up to 50,000 credits once, test or take
  a credit balance, set flags and change standing a little. It cannot require a balance as
  a gate, schedule payments or run interest; those need the mod's code.

## Open questions for the owner

1. What happens when a borrower defaults, beyond the game's late fee?
2. May a loan fund a ship purchase at the broker, alongside its own mortgage?
3. Should the app also show the player's existing native mortgages and debts?
4. Are lenders local to regions and stations, or everywhere?

## Proposed rounds

1. Framework `PdaApps`, and a first app screen that lists the player's native debts:
   proves the hosting in play before any lending exists.
2. The `lenders` pack and the loan book, with one accredited lender.
3. Non-accredited lenders and their consequences.
4. A story pack with ChatGPT's lore.
