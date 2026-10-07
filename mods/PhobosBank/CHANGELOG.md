# Phobos Banking changelog

Maintained from 6 October 2026. Dates on Draft entries record preparation,
not Steam publication.

## [Unreleased]

## [0.8.1] - 2026-10-08 - Released

### Balance

- Orrery Credit now charges 0.10% a shift on what you owe, up from 0.06%. The line is sold as dearer than walking into any lender's office, but 0.06% undercut Stillwater Advances (0.08%). It stays cheaper than the Narrow Ledger (0.15%), and the 3% draw fee is unchanged (owner decision, 8 October 2026).

### Compatibility and limits

- Saves: a credit line you already opened keeps the rate it opened with, as every line does. Only lines opened after the update pay 0.10%.
- Checked offline; not yet seen in the game.

## [0.8.0] - 2026-10-08 - Draft

### Added

- Lender letters can come in different words. Corvane Mutual's late-bill letter now has a second wording, and its late letters take the two in turn. The second is a placeholder until the writers replace it; the other letters follow at the writers' pace.

### Compatibility and limits

- Requires Phobos Framework 0.132.0, which reads text written several ways.
- Saves: nothing new is saved.
- Checked offline; not yet seen in the game.

## [0.7.0] - 2026-10-07 - Released

### Added

- The overview shows what you own through other Phobos mods: with Phobos Exchange installed, what your shares are worth at today's selling prices, how many companies you hold, and a button that opens the exchange. Neither mod needs the other.

### Compatibility and limits

- Requires Phobos Framework 0.128.0, which adds the shared list of holdings.
- Saves: nothing new is saved. The line reads the exchange's own figures each time the overview is drawn.
- Checked offline; not yet seen in play.

## [0.6.0] - 2026-10-07 - Draft

### Added

- A credit line you can use anywhere: Orrery Credit, a system-wide service, opened and drawn on from your PDA wherever you are. Limit 25,000, interest 0.06% a shift on what you owe and a 3% fee on each draw, added to the balance (agent choices for the owner to review). The Credit line page shows the limit, what you owe, what is available and the least you can pay this shift, and quotes each draw's fee and new minimum before you confirm.
- Repaying works through the Finances window: each shift the line raises one instalment, the least you can pay; Prepay pays off more. Each draw spreads the whole balance over a fresh term, as the game's own Prepay does. When the balance is paid off, interest stops and the line stays open.
- Credit lines are data: a creditLines table in the lenders file, for players and add-ons to add or tune. Story writers get the line-opened event and the line-open flag.
- F3 console: phobosbank line, phobosbank openline and phobosbank draw amount.

### Fixed

- A loan paid off in full with the Finances window's Prepay is now reported as repaid, with the repaid flag and letter. It was reported as settled by a ship sale, because the game removes the mortgage line in both cases. A ship or apartment loan counts as settled only when the ship or apartment has changed hands.

### Compatibility and limits

- Saves: an opened credit line is one more record in the loan book, with its terms copied when it was opened; its balance is one of the game's own mortgage lines. Nothing changes in existing loans.
- Not paying has only the game's own consequences, the 17.5% late fee on each unpaid bill.
- Checked offline and against the game's own Prepay and instalment code; not yet seen in play.

## [0.5.0] - 2026-10-07 - Draft

### Added

- Complete lender correspondence: repeatable letters when a loan is taken, a bill turns late, a bill has been late for three game days, and a loan is paid off. All five lenders now have four event letters; each late letter offers two or three replies with its own answer.
- A local advert, station news item and small-talk line for each lender, plus an encyclopedia article about credit and shift payments.

### Compatibility and limits

- No new Phobos Banking save fields. Story progress uses Framework’s existing story record; Corvane Mutual’s existing reply flag is unchanged.
- Replies only advance the story. They do not pay, delay or change a bill; payments remain in the Finances window, and the game's late-fee rule is unchanged.
- The story pack passes the offline data-pack validator and Banking checks. This draft has not been verified in play.

## [0.4.0] - 2026-10-07 - Draft

### Added

- Two unregistered lenders (agent choices for the owner to review): Stillwater Advances at Port Independence on Ganymede, which lends 2,000 to 60,000 and finances ships from 25% down, and the Narrow Ledger, which lends 1,000 to 30,000 to anyone docked at Corsair's Hollow on Ceres. Neither asks for standing; both cost more (about 21% and 40% in interest over a whole loan paid on time). Not paying them has only the game's own consequences, the late fee.
- Letters from your lenders. Each lender has an officer who writes when you borrow, when a bill turns late, when it has been late for three game days, and when a loan is paid off. Letters reach you anywhere and appear in the Letters window; a late letter may ask for a reply. Replies change only the story; bills are paid in the Finances window. This version ships Corvane Mutual's letters; the other lenders' letters, adverts and news are being written.
- For story writers: on each of those events Phobos Banking starts the story arc bank-lender-event (borrowed, late, late-long, repaid) when a story pack has one, and keeps the flag bank-lender-late-long beside the others.

### Compatibility and limits

- Saves: nothing new is saved by Phobos Banking. Letters are kept in Framework's story record, like any story correspondence.
- Paying a Corvane Mutual loan off gains 2 standing with OKLGCorp, through the game's own faction score.
- Checked offline and against the game's data (the story pack loads whole, every faction named exists); not yet seen in play.

### Changed

- Replaced the placeholder Workshop cover scene with an original overhead pixel-art illustration of a spacer checking a debt overview on their PDA. The cover does not change gameplay.

## [0.3.0] - 2026-10-07 - Draft

### Added

- Financing at the broker. Ask a lender for a pre-approval in the Credit panel (Pre-approve a ship purchase, or an apartment); it stands for one game day. At a ship broker or the Venus real-estate broker where that lender trades, the broker's own purchase window then lets the down payment go as low as the lender allows (Corvane Mutual 35%, Halcyon Bond 30%, Aerie Savings Union 40% for apartments) instead of the broker's 50%, within the approved amount, and the crew log states the terms. On confirming, the broker's mortgage becomes the lender's loan, with the lender's interest at each shift change. Selling the ship later repays the lender from the sale, as the game does for any mortgaged ship.
- The overview shows a standing pre-approval; Withdraw pre-approval drops it.
- F3 console: phobosbank approve lender ship or home, and phobosbank withdraw.

### Compatibility and limits

- Without a pre-approval, for a derelict, for a special offer (the broker finances those itself) or where the lender does not trade, the broker's window is exactly the game's own, and the crew log says why the lender is not used. A purchase paid in full uses nothing.
- The purchase window's payment line still shows the game's own instalment; the lender's interest is billed separately and stated in the crew log.
- Saves: a pre-approval is one more field in the loan book. A financed purchase is a loan like any other, with the ship or apartment named as its collateral.
- Checked offline and against the game's broker code (the window's slider, the mortgage it writes, the kiosks); not yet seen in play.

## [0.2.0] - 2026-10-07 - Draft

### Added

- Lenders. A Lenders page in the Credit panel lists who lends where: lenders that will lend to you where you are come first, the rest say the one thing in the way (go to their home, better standing with a faction, a loan already running, or as much owed as they lend).
- Borrowing. Pick a lender and an amount and the money is paid into your account. The loan is a mortgage line in your ledger with the lender as creditor, repaid by the shift on the same schedule as any station mortgage, in the Finances window. The lender's interest is a separate bill at each shift change, its rate on what you still owe; a long time-skip bills every shift it crossed. The panel shows the first instalment and what a whole loan would cost in interest if paid on time, before you confirm.
- Three accredited lenders (agent choices for the owner to review): Corvane Mutual around OKLG, Halcyon Bond around Port Yangshan on Mars for those warm with the Xinhua administration, and Aerie Savings Union around Long Beach Terminal on Venus. A whole loan paid on time costs about 5% to 8% in interest.
- The lenders pack (framework/lenders.json): add your own lenders or change these in BepInEx/config/PhobosBank/lenders, or ship them in an add-on. Each lender has a home place, optional story requirements (standing, flags and the rest), a rate, a range and what it lends for. The editing guide has an example.
- A loan from a Phobos lender shows its rate and the interest billed so far in Your debts. When it is paid off, interest stops and the crew log says so.
- Story flags for story writers: bank-lender-borrowed, bank-lender-repaid and bank-lender-late (with the lender's id), so a story can follow what you did with money.
- F3 console: phobosbank lenders, phobosbank borrow lender amount and phobosbank loans.

### Changed

- The Overview button is now Your debts, beside the new Lenders button.

### Save compatibility

- Loans from Phobos lenders are kept in one small Phobos record on your character (lender, terms, interest billed). The balance itself is the game's own mortgage line. A lender's terms are fixed when you borrow; a later change to the lenders file never changes a running loan. Removing the mod leaves the loans as ordinary mortgages owed to the lender's name, with no more interest billed.

### Compatibility and limits

- Requires Phobos Framework 0.127.0 for the story services lenders use (where you are, requirements, flags).
- Not paying has only the game's own consequences: late fees and the debt in the Finances window. Lenders do not repossess anything.
- Checked offline and against the game's own ledger code: an interest bill can never pay a loan down, and one loan's instalments can never pay another's. Borrowing has not yet been seen in play.

## [0.1.1] - 2026-10-07 - Draft

### Fixed

- The overview counts properly: one loan, two loans, one bill waiting, all of them late, and so on, instead of loan(s) and bill(s). A loan on its last instalment says so.
- The Back button shows only on a narrow screen, where it pages from a debt back to the list. On a wide screen the list and the details sit side by side, so it had nothing to do.

### Compatibility and limits

- Wording and layout only. Nothing saved changes. Owner tested 0.1.0 in play on 6 October 2026: the CREDIT icon and its tooltip show on the PDA home screen and the panel opens and lists the ledger.

## [0.1.0] - 2026-10-06 - Draft

### Added

- A CREDIT app on the PDA home screen. Tap it and the PDA closes and the Credit panel opens, as the game's own Roster and Duties apps do.
- The Credit panel lists what you owe, straight from your own ledger: loans and mortgages (Ogiso's Bank's starting mortgage, ship mortgages from the broker, fines the game books as debt), bills waiting to be paid (instalments, docking fees, late fees) and regular charges such as crew wages.
- An overview with your cash on hand, the total left to repay, the next instalments, the bills waiting and how many are late.
- For each loan: the balance, the next instalment, how many instalments are left and when it was taken out, all worked out the way the game charges them. For each bill: the amount, when it was raised, whether it is late, and the late fee the next shift change will add if it stays unpaid.
- An Open Finances button into the game's own Finances window, where bills are paid.
- F3 console: phobosbank debts lists the same debts as text, phobosbank open shows the panel and phobosbank finances opens the Finances window.

### Compatibility and limits

- Requires Phobos Framework 0.126.0, which adds the shared PDA apps service.
- Read-only: the panel never pays, charges, lends or changes your ledger. Lending, broker financing and local lenders are planned in later rounds and are not in this version.
- Saves need nothing and nothing is saved. Removing the mod removes the app and leaves your ledger as it was.
- The PDA's quick bar under the home screen is the player's own list; the app is on the home screen only.
- The Workshop cover is a placeholder drawn by a script while the mod is held.
- Checked offline: the mortgage figures against the game's own formula, the PDA hooks against the game's code, and the panel's sums. Not yet seen in the game; owner gameplay checks are pending.
