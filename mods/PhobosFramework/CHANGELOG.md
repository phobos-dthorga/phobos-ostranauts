# Phobos Framework changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- JSON Schema files for the four data packs in the repository's schemas folder, for editor completion of shipped packs and player override files, generated from the same field sets the offline validator uses; the editing guide shows how to point an editor at them. Built packages now carry a manifest the installer checks.

- Audit linked player documentation and correct generated placement wording for section assembly while preserving direct installation of complete machinery. No gameplay or save changes.

- Extend maintained item evidence to actual native maintenance generation, attachment and fresh/worn/contained target checks. Document shared dismantling restrictions for cargo, lots and stacks; add a read-only aggregate save audit. Runtime behaviour is unchanged.

- Investigated additive bulk-storage and station-purchase contracts for Agriculture, including native refuelling and Ship's Water coexistence, custody, reservations and UI isolation. Published a research blueprint; the investigation itself registered no runtime API, equipment or service. Framework 0.27.0 below later added the shared bulk-supply storage and station purchase services.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Shared native INSTALL category constants and pre-publication validation prevent unreachable visible installation entries.

## [0.55.0] - 2026-09-30 - Draft

### Fixed

- Control panels redraw as soon as a setting is applied: link names, field values and offered actions no longer stay as they were until the panel is reopened. After Apply the notice carries the machine's own reply (for example "Hydrogen store linked.") and a selection sheet opens with the current setting marked.
- Machine states Paused, Ready and Unavailable now have names in the shared console instead of showing a bracketed key.

### Added

- The console command phobosframework loot, followed by a table name (open the console with F3), shows a mining or loot table as the game rolls it now, with any Phobos shares carved into it; without a name it lists the tables that carry our shares. Read only.
- Equipment providers can name their own console groups, so a console that lists every mod's equipment shows them in the owner's words.

### Compatibility and limits

- Phobos Shipbreaker, Agriculture and Manufacturing need this version for the panel fixes. Saves are unchanged. Offline checks are not gameplay validation.

## [0.54.0] - 2026-09-30 - Draft

### Added

- An equipment data-pack schema: a machine's footprint, empty weight, idle and working power, share of its heat into the room, feed cells, artwork, install tab and connection points. For now it is read-only: a player file that changes a shipped machine is refused with a message, because a footprint or connection change would move equipment already placed in a save.
- Process recipes may now declare a working volume a machine needs on hand but gives back (circulates), and heat the reaction itself releases into the room, or absorbs, over the charge (reactionKWh). Old recipes are unchanged, and their frozen fingerprints stay the same.
- A shared settlement for machines that draw from and deposit into bulk stores when a charge finishes: draws and deposits on the same store are netted, every store is checked first (protected, busy, catch chamber in use, too little, too full), and the finished items are delivered before any store changes, so a blocked delivery changes nothing.

### Compatibility and limits

- No gameplay or save change by itself; Manufacturing 0.17.0 is the first user. Offline checks are not gameplay validation.

## [0.53.0] - 2026-09-30 - Draft

### Added

- Faction kiosk stock. A content mod's economy data pack can now list items for the game's CCRE and GalCon faction kiosks, which sell for scrip, and the reputation each item needs there: Neutral, Warm, Friendly, Trusted or Honored. The new factionKiosks section names the kiosks and a tier per machine family (every size), supply or item; a player override file can retune a tier or add an item. Stock is stamped with a hidden tier mark, and the game's own kiosk tier checks are extended in place, so a marked item shows up exactly at its tier while vanilla kiosk stock keeps the tiers the game gave it.
- Scrip prices stay the game's own: an item's usual price converted at the faction's rate, one scrip for every 20 credits, the same as everything else those kiosks sell.

### Compatibility and limits

- Kiosks keep their current stock until their next normal restock; nothing is refilled or edited in a save. Offline checks are not gameplay validation.

## [0.52.0] - 2026-09-30 - Draft

### Added

- The economy data-pack schema now covers assembly sections (sold whole: price, dismantle work, salvage), items with a plain damaged twin (navigation boards), per-family offer scale and regional chance, salvage with a retained remainder (it may weigh less than the machine, never more), free-named lot and floor tables every entry points into, loose commodities offered in every region, and world finds listed item by item or across several tables. One shared stock resolver classifies any item to its lot and floor and applies offers, regional stock and world finds for every Phobos mod, so the four mods no longer keep their own copies.

### Compatibility and limits

- No gameplay or save change by itself; Manufacturing 0.14.0, Shipbreaker 0.48.0, Agriculture 0.24.0 and Auto Nav 0.30.0 read the new fields. Offline checks are not gameplay validation.

## [0.51.0] - 2026-09-30 - Draft

### Added

- A vessels data-pack schema: the small size of every bulk store or bin a mod ships (what it holds, capacity, empty weight, leak rate when damaged, cells per tile for a bin). Every family the code names needs an entry and no other may be added; kinds, commodities and identities stay with the mod. Larger sizes still follow the shared size ladder. A capacity or weight edit does not silently change a store you own: it shows as needing attention until accepted, as before.

### Compatibility and limits

- No gameplay or save change by itself; the first packs are Manufacturing 0.13.0, Shipbreaker 0.47.0 and Agriculture 0.23.0. Offline checks are not gameplay validation.

## [0.50.0] - 2026-09-30 - Draft

### Added

- Two more data-pack schemas. Process recipes: what a machine turns a charge into (inputs, products, gas breathed into the room, seconds, a furnace heat profile), checked on every file for mass conservation and the game's own gases. Materials: a mod's loose items (mass, price, stack, size, category, art). Both accept player files like the economy pack.
- Frozen recipe revisions: a running machine remembers only its recipe revision, so every shipped revision is frozen by a checksum. A file that changes or removes a frozen revision is skipped with that reason; a new revision beside it is allowed, and the machine offers the highest.

### Compatibility and limits

- No gameplay or save change by itself; the first packs are Manufacturing 0.12.0 and Shipbreaker 0.46.0. Offline checks are not gameplay validation.

## [0.49.0] - 2026-09-30 - Draft

### Added

- Data packs: one loader for the tables a Phobos mod keeps outside its code (prices, work, repair bills, salvage, merchant offers, lots, regional factors, world loot to begin with). Each mod ships its pack inside the plugin and as a readable copy under its framework folder; players override entries with small files in BepInEx/config/(mod)/(schema), merged by name, applied in name order. A file can tune a shipped entry or add one, never rename or remove one; a misspelt field or a broken rule rejects only that file, with the reason in the log and on the F3 console (phobosframework status).
- The economy schema and its checks, shared by every mod: every family the code names has an entry, prices and work are above zero, bills name known materials, salvage weighs what the machine weighs, merchants, conditions and regions are real, chances are within 0 to 1 and lots within 1 to 256.

### Compatibility and limits

- No gameplay or save change by itself; the first pack is Manufacturing 0.11.0. Player files apply on the next game load. Offline checks are not gameplay validation.

## [0.48.0] - 2026-09-30 - Draft

### Added

- Carved loot shares for content mods. A new mined item or asteroid type can take part of an existing entry's chance in one of the game's loot tables, instead of adding an extra roll on top. The new entry sits right after the entry it takes from, so every other entry keeps exactly its old odds and the table never yields more in total. It works on the game's item tables and on the asteroid-field tables, where an added roll could never be picked. Several mods can take from the same entry; a share of zero restores the game's table.

### Compatibility and limits

- A table that another mod rewrote after a share was taken is left as that mod wrote it, and the skipped share is written to the log. Tables are changed only while the game loads its data; saved games and already generated asteroids are not rewritten. Offline checks are not gameplay validation.

## [0.47.0] - 2026-09-30 - Draft

### Changed

- Performance pass, stage 9. Captures with 0.46.0 counted about 400,000 trigger checks a second at speed 8. Framework's one hook on those checks now rules out a trigger by its name length and first letter, two reads, before any table lookup; only names that could be registered construction or assembly selectors reach the table. Results are unchanged.
- The shared world sweep takes its snapshot of the world with one bulk copy instead of going through the world's objects one by one, removing a pause of about 6 ms every two real seconds.

### Fixed

- Capture timings added in 0.46.0 recorded every trigger check as a separate record. That filled the capture's record limit within about a second and pushed out the frame-time samples. Counts are now summed in memory and recorded once per frame.

### Compatibility and limits

- No saved data changes. Offline checks are not gameplay validation.

## [0.46.0] - 2026-09-30 - Draft

### Changed

- Performance pass, stage 8. Agriculture, Manufacturing and the Shipbreaker furnace each walked every object in the world (about 49,000 in the owner's save) every two real seconds, costing about 8.5 ms each time. Framework now keeps one shared record of which world objects belong to each mod's families. One sweep of the world runs every two real seconds, spread over the frames in between, so no single frame pays for it; the first sweep after a load runs at once. An object that is destroyed or leaves the world drops out the moment it is next read; a new object is found within about four real seconds, or at once when a mod hands it over (a damaged or repaired replacement part).
- Capture timings of the game's own main loop, simulation step, ship update, appliance updates and crew offer checks, with a count of trigger checks. They are installed only while a performance capture records and removed when it stops, so ordinary play pays nothing. They show where long frames sit, and how much time every mod's hooks on the offer check take in total.
- Recorder scopes framework.world.sweep and framework.world.sweep_objects, and the capture timings game.crewsim.update, game.sim.advance, game.starsystem.update, game.powered.update, game.interaction.offer_check, game.interaction.offer_postfixes and game.condtrigger.calls.

### Compatibility and limits

- No saved data changes. Offline checks are not gameplay validation.

## [0.45.1] - 2026-09-30 - Draft

### Fixed

- Framework 0.45.0 failed part-way through starting up. Its pipe-layout cache patched the game's Ship.AddCO by name alone, and the game has two versions of that method, so Harmony refused the patch and stopped installing the rest of Framework's hooks. The game's player log (Player.log, not BepInEx's LogOutput.log) showed an AmbiguousMatchException from Framework's start-up, and Framework's per-frame work (crew orders, buffered draws, performance frame samples) never ran. Both versions are now named, and every declared patch in every Phobos plugin is now resolved by an automated check, so this cannot ship again. Do not use 0.45.0; the other Phobos mods now require 0.45.1.
- Two allocation checks in the offline test suites, which measure the classification of vanilla appliances, no longer fail when the test runtime's own compilation lands inside the measurement.

### Compatibility and limits

- No saved data changes. Offline checks are not gameplay validation.

## [0.45.0] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 2 (owner request: the game crawls at fast-forward with every mod active). The game's trigger check, its hottest method, now passes through one Framework hook that returns before any lookup unless a registered selector is involved. Crew task admission checks cheap facts first and reuses each path search within a step; the claim itself still checks fresh. RCS fuel queries share one input list per step and allocate nothing per query. Bulk vessel readings, damaged-vessel checks and the Ship's Water adapter no longer throw, rescan or reflect on every poll. The power-receipt, room-alarm and time-skip hooks lose their per-call allocations and reflection.
- Saved records can be written only when they changed (TryWriteIfChanged), and services that settle on a real-time cadence flush before every native save through one shared save boundary. Every validation rule is unchanged.
- Fluid routes (irrigation, coolant, gas line) can be answered from a topology snapshot per ship that is reread every two real seconds or at once when a part changes mode, is destroyed, or joins or leaves the ship; endpoints are still checked fresh on every call. A pipe that just broke is noticed within two seconds instead of on the same step. Content mods opt in per segment family.
- New shared helpers for content mods: a definition index, a per-step memo, a real-time cadence, deferred status text and the save boundary. Recorder scopes for the fluid route, crew filter, RCS collection, state writes, water refill and time-skip machine steps.

### Compatibility and limits

- Settled draws are logged at Debug level (BepInEx leaves it out of the disk log by default); the saved record is unchanged. No save or definition changes. Offline checks are not gameplay validation; owner before/after captures are pending.

## [0.44.0] - 2026-09-29 - Draft

### Added

- Shared size ladder for bulk storage: content mods declare a small vessel and get matching medium and large sizes, one tile wider each, with capacity, housing mass and price scaled by one rule. The small size keeps its original identity and saved records.
- Safe filling of the game's own gas vessels: installed or loose O2, N2 and CO2 canisters and suit O2 bottles are filled to 99% of their rating, counting everything inside, and never past it.
- Journalled gas moves between bulk stores and the game's canisters and bottles, so an interrupted move can lose gas but never create it.
- Station bulk supply offers can fill every size of a family from one line, and equipment can offer a restricted rack through the game's own Inventory window.
- One shared within-one-tile rule for machines and bulk vessels, used by Manufacturing and by Agriculture's W2 intake.

## [0.43.0] - 2026-09-29 - Draft

### Added

- Construction.NativePlaceholders: lays the game's own construction build sites from code for any part with an INSTALL job, vanilla or modded, the way the game rebuilds saved build sites when a ship loads. It finds the part to rebuild from a destroyed form: damage is followed back to the intact part (cosmetic variants such as branded conduit included), and a working state with no install job of its own, such as a closed or locked door or a lit alarm, is rebuilt through the loose part its uninstall job yields. It also says whether a build site would block walking, which it does whenever the finished part would. First consumer: Phobos' War Has Been Declared 0.1.0.
- Observations.NativeCombat: read-only combat facts for a ship: another ship engaged with it, its own weapons target, and the last time it took damage this session. It never touches weapons, targets or AI.

### Compatibility and limits

- No existing behaviour or saved data changes. A build site carries its part's full footprint, as a player-placed one does.

## [0.42.0] - 2026-09-29 - Draft

### Added

- RCS thrusters burn each gas at its real cold-gas worth instead of treating every kilogram alike: hydrogen about 3.7 times nitrogen, methane about 1.45, oxygen 0.94, carbon dioxide 0.90. RCS fuel, delta-v and Auto Nav's planning all count in nitrogen-equivalent kilograms, so a ship that only uses nitrogen flies exactly as before. Distant, unloaded ships and station refuelling are unchanged.
- Content mods can register an RCS propellant feed: an object on a regulator's gas-input tile that supplies remass from somewhere the game cannot see (Phobos Manufacturing's propellant manifold uses it).
- Buffered draws on bulk vessels for consumers that take a little every frame; they settle into the vessel's record every couple of seconds and before a save.

## [0.41.0] - 2026-09-29 - Draft

### Added

- Processing.RoomHeat: the shared air-cooled operating rule (10 kPa floor, 40 C ceiling, the game's 20.7 J per mol K) as one budget, a room reader, admission and a deposit into the room's pending temperature. Shipbreaker and Agriculture keep their own copies unchanged.
- Processing.NativeGasCanister and RoomGas: the game's gas species and molar masses, a rated canister's capacity by the game's own refuelling arithmetic, guarded adds (capped at the rated pressure, which the game itself does not cap) and takes (clamped to contents, because a negative total corrupts the game's count) on installed O2, CO2 and N2 canisters, and kilogram-based emission into or consumption from a room's air, native species only.
- Hazards.NativeExplosions.Spawn: places one of the game's own explosion objects on a ship so the native Explosion component runs.
- Liquids.VesselDamagePolicy: a bulk vessel family may declare Leak with a rate instead of the default Isolate; a damaged leaking vessel keeps its contents in service for its owner to drain. BulkVessel.Drain removes contents without a receiver and logs the loss.
- Registration.ApplianceDefinitions.AddFeedBin and SetPowerOverride, and Controls.ConsoleAuthority.Check: the hidden feed compartment, the idle/working demand override and the remote-console rule that three content mods had each written for themselves. First consumer: Manufacturing 0.1.0.

### Compatibility and limits

- Additive API; no saves, definitions or behaviour of existing mods change (every existing vessel keeps Isolate). Nothing here starts a fire or ignites gas by itself, models pressure inside a Phobos vessel, or creates a gas species the game lacks. Offline checks pass; owner play-testing is pending.

## [0.40.0] - 2026-09-29 - Draft

### Added

- Processing.ReactorRules, IReactorPanel, IReactorState, ReactorControls and NativeReactor: shared facts about the game's fusion reactor (its 0.27 second update cadence, the ideal core, the course plot's correction and abort bands, the wall-damage temperature, the pilot flow grace), read-only readiness and no-wake reads, guarded flight-control writes that tell an owner's own commands from a pilot's and hand the idle settings back on release, and the vanilla flow regulation with a tighter hot side. First consumer: Auto Nav 0.27.0.

### Compatibility and limits

- Additive API; no saves, definitions or behaviour of existing mods change. The rules are pure so consumers and offline checks share one copy; the game-facing helpers wrap a CondOwner. Nothing here ignites, repairs or refuels a reactor. Offline checks pass; owner play-testing is pending.

## [0.39.0] - 2026-09-29 - Draft

### Added

- Bulk vessels: Liquids.BulkVesselSpec declares a family of silos, reservoirs or tanks (definition prefix, one commodity, capacity and dry mass in kg, record names); BulkVessels is the registry; BulkVessel keeps custody (native mass equal to dry mass plus contents plus cargo, transfer and conversion journals, Protected state, owner-confirmed Accept, snapshots and a reservoir endpoint). Contents follow a mode switch into a successor of the same family and are isolated when it is damaged; contents lost with a destroyed vessel are logged, never blocked.
- Trading.VesselSupplyProvider: a station Bulk supplies provider over registered vessel families with content-declared offers and measured delivery.
- ShipsWaterSupply.DepositWaste and WasteCapacityKg: optional, 0.16.1-pinned deposit into installed Ship's Water waste tanks through guarded transfers, up to the capacity their own configuration declares; the potable tanks are never written to.

### Compatibility and limits

- Additive API. Agriculture's R3 registers with the record names every saved R3 already carries, so saves read unchanged. Framework never assumes a fluid density; a vessel commodity is an id in kilograms, not a native gas or fuel stat. Required by Shipbreaker 0.37.0 and Agriculture 0.18.0. Offline checks pass; owner play-testing is pending.

## [0.38.0] - 2026-09-29 - Draft

### Added

- Standing orders may take supplies from anywhere aboard: choose Use anything aboard for the input store, and crew search the deck, unlocked stores and other machines' product trays on the same ship, nearest first, the way the game's own PDA Reload job searches. Items lying on the deck are carried like items in a store, in ordinary play and during a managed time-skip.
- StandingOrder.ShipWide names that source for content mods; CrewLogistics.Aboard lists what an order may take.

### Compatibility and limits

- Additive API. Existing orders and chosen stores are unchanged. Crew never take from a hidden feed bin, a locked container, the equipment itself or someone's hands. Required by Shipbreaker 0.35.0. Offline checks pass; owner play-testing is pending.

## [0.37.0] - 2026-09-28 - Draft

### Added

- Appliance definitions name a self-targeted power-change action in their power info, the native pattern that makes the game itself set and clear IsPowered. Agriculture machines therefore show their real power state to the game, the crew console and the panel.
- NativeDefinitions.Trigger returns a native trigger by name or null; the game's own lookup returns its always-true Blank trigger for an unknown name, which must never gate an optional provider.
- StackUnits enumerates the units in a container counting each native stack's members separately, since the game stacks matching items dropped into a container; machines take one member at a time and leave the head in place.
- LiquidTransferGuard.Resolve closes an interrupted-transfer journal on the owner's say-so, for the new accept-contents commands.

### Compatibility and limits

- Additive API. Required by Agriculture 0.17.0 and Shipbreaker 0.34.0. Offline checks pass; owner play-testing is pending.

## [0.36.0] - 2026-09-28 - Draft

### Fixed

- Any crew member the game admits can take a standing-order step. Tasks used to name your own character as their owner, and the game forbids everyone not on an owner list, so no other crew member ever took one.
- Several orders can feed one store. The game keeps one task per target and action unless told otherwise, so later orders on the same store were silently dropped.
- Crew who are merely not rested, sated or slaked, or in moderate pain, are no longer refused work. The game's own pledges put eating, drinking and rest first, as they do for painted jobs; unconsciousness, combat and emergencies still block.
- A step is no longer refused because the equipment's contents changed after the claim (a tidied stack, for example); the provider checks the actual contents when it completes.
- Training credit is no longer lost when the game reuses a pooled action object for a later action.
- Time-skip repairs keep the game's own repair allowance, scaled by the share of on-shift crew time our jobs left free, instead of a replacement count that was up to six times smaller. A skip suspends only orders on the skipping crew's ships that it cannot advance.
- A refused dismantle, repair or construction finish now closes the game's task for it, as native effects do, instead of leaving that task listed forever. The reason goes to the actor's crew log.
- An internal bin counts as empty within the shared mass tolerance rather than at exactly zero.
- Transfer clocks, pump budgets and processing jobs accept long intervals such as a time-skip or a reload gap and catch up like native machines; the electricity actually received bounds the work. Content mods' own "time gap" pauses no longer trigger and are removed in their next versions.
- Without Ship's Water, a missing tank rule no longer counts as always true (the game returns its always-true Blank trigger for an unknown name).
- The unsaved-changes guard applies only to the panel hosting a Phobos shell; closing any other panel proceeds natively.
- Escape closes a store picker before its panel, as with native sub-windows, and pause, time-scale, console and screenshot keys stay live while picking in the world.

### Changed

- Native loot tables and construction stations are amended in place instead of being cloned and republished under their own names; the game's own objects and every other mod's entries stay.
- The standing-order task action is cloned from the vanilla Toggle Power job (Operate duty, tooling animation) rather than from the inventory panel action.
- The F3 command phobosframework crew also names who could take each order step right now.

### Compatibility and limits

- Additive API: NativeDefinitions.Amend and LootBranches, NativeEffects.Refuse, ShipsWaterSupply.Rule and CrewBalance.RepairShare. TransferClock.MaximumStepSeconds is replaced by MaximumCycleSeconds: steps are unbounded, cycles stay 1 to 60 seconds. No saved identities or records change. Content mods built against 0.35.0 keep working; Agriculture and Shipbreaker updates that remove their own time-gap and finish-time workarounds follow. Offline checks pass; owner play-testing is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.35.0] - 2026-09-28 - Draft

### Fixed

- Standing orders no longer interrupt crew study. The game cancels every on-shift crew member's study whenever its task list grows, and each completed order step used to add a new task. An order now announces one task when it is enabled, like a painted job, and later steps are added quietly.
- A failed order step is retried after 30 seconds, then 1, 2, 5 and 10 minutes, instead of every 2 seconds. The order stays enabled and shows the wait and the reason; the worker is free for native tasks, study and rest in between.
- A step a crew member cannot take (no route, nothing to carry, a skilled colleague available, or a reservation held) is withheld from that crew member's task search instead of ending it, so lower-priority native tasks stay reachable.
- Terminal definitions are amended in place rather than cloned and republished, so PDA job painting on terminals keeps its native actions. The retired 15-minute study action no longer carries a work duty, so an old queued one cannot turn a direct order into a task.

### Changed

- Specialities are studied through the game's own study chain: powered terminals offer Study Agriculture, Cooking and Industrial Processing with the vanilla stages, refusals, tablet animation, work-shift interruption, Stop and time-skip continuation. Each completed study step credits the speciality; a skipped hour credits an hour. The 10-hour study balance is unchanged.
- AutoTask crew can choose Phobos study on their own. The game only picks actions present in a crew member's AI history, so on load the vanilla construction-study entries are copied for each speciality into every crew member's history and the new-crew template, only where absent. Nothing is overwritten or removed.
- The retired 15-minute study action is no longer offered on terminals; its definition stays registered so older saves load.
- New F3 command phobosframework crew, optionally followed by a crew member's name, explains per crew member AutoTask, shift, current action and study eligibility, each terminal's study admission and users, AI-history entries for Phobos study, order retry waits and the game's task count.
- Registration refuses a trigger whose chance is zero and restores missing lists before publishing.

### Compatibility and limits

- Additive changes to saved crew AI history and to live terminal action lists; no saved identities change. Existing saved terminals qualify for study through the vanilla rule without a power cycle. Content mods need no update. Offline checks pass; owner play-testing of study, interruption and retry behaviour is pending.

## [0.34.0] - 2026-09-28 - Draft

### Added

- SensorLeases lets automation switch native ship sensor types on through the native Sensors page switch, noting a lease on each sensor unit, and later switch off only what it switched on. Sensors already on are never claimed. Any other switch clears the lease, or turns a switch-off into a decline for that work. Notes use the existing object-state store, so they follow power mode switches, repairs and saves. IfOn predicts a sensor's contribution with the native formula without writing state, and SwitchedOffByOthers reports other switch-offs.
- PlayerNotices posts one crew message-log line and, while that ship's navigation station is open, the native nav-map warning banner with its tone, limited to once every 20 seconds per notice kind.

### Compatibility and limits

- Additive services; existing saves and consumers are unaffected. Content mods own their switching policy and wording. Offline checks pass; banner and log appearance in Unity remain owner-tested.

## [0.33.0] - 2026-09-28 - Draft

### Added

- A public single-container check for crew storage eligibility: finite, unlocked, not a person and not provider-owned equipment. Crew store lists keep the same rule, and Shipbreaker storage outputs now share it instead of copying it.

### Compatibility

- Additive only; saved orders, stores and equipment are unchanged. Required by Shipbreaker 0.33.0. Offline checks are not in-game validation.

## [0.32.0] - 2026-09-28 - Draft

### Added

- Optional unfinished construction images for section-based machinery. Stages follow delivered parts and saved work; native completion still creates the finished machine.
- Construction views preserve native geometry and selection effects, limit routine refreshes to ten per second per site, and release their private materials when removed. Missing images fall back without changing the job.

### Compatibility

- Existing assembly calls, material bills, saved IDs and work rules remain unchanged. No new saved appearance fields. Offline assembly, renderer-adapter and native-boundary checks pass; Unity visual approval remains pending.

### Fixed

- Keep documentation packaging status out of the returned package path so archive creation succeeds after the guide-directory split.

## [0.31.1] - 2026-09-28 - Draft

### Fixed

- Leave unnamed native condition triggers unchanged instead of throwing during section selection. The item-information hook also ignores unnamed native interactions. Registered assembly material and completion checks remain intact.

### Validation

- Reproduced the owner's null-key exception before the fix. Regression checks cover null, empty and unrelated names with both native outcomes, including the compiled hooks. Live Unity confirmation remains pending.

## [0.31.0] - 2026-09-28 - Draft

### Added

- Shared native construction-site section contracts with exact input validation, staged native lots and preserved cancellation/save handling.
- Read-only item and maintenance information panels for content-owned instructions.

### Fixed

- Explain split-stack, retained cargo and pending-work restrictions without weakening dismantle guards.

### Compatibility

- Existing public APIs and table action identities remain valid. Native Unity hauling and information-panel interaction await owner checks.

## [0.30.3] - 2026-09-27 - Draft

### Changed

- Add a shared stock-coverage helper that fills omitted item offers without duplicating an already prepared lot or replacing its condition. Existing merchant inventories, native pricing and other providers remain untouched.
- Applies to future native stock and loot generation; no forced restocks, saved-cargo changes, price changes or live gameplay validation. See [merchant availability and salvage](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/merchant-stock.md).

## [0.30.2] - 2026-09-27 - Draft

### Fixed

- Add opt-in native transport-action normalization shared by content mods: retain useful actions, add missing pickup/drop, match stack actions to stack limits and keep installed machinery out of carry slots.
- Restore declared cumbersome flags in detached load data for explicitly registered bulky cargo. Preserve saved hand placement until successful native release, then retire the legacy hand attachment. No save files, contents, mass, wear or progress are rewritten.
- Audited all 118 implemented item definitions and checked native action/slot contracts offline. Live menus and loaded inventory handling still require owner testing; see the [item handling audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/item-handling-audit.md).

## [0.30.1] - 2026-09-27 - Draft

### Fixed

- Correct washed-out Polaris buttons by using explicit dark colours for every button state and resetting inherited brightness. Native artwork and other panel defaults remain unchanged.

### Known limits

- Native palette contrast and offline regressions are checked; corrected Unity appearance and scrolling still require owner playtesting. No gameplay values or save formats change.


## [0.30.0] - 2026-09-27 - Draft

### Changed

- Add opt-in Polaris button styling using native artwork, retained bindings and live selected markers. Existing panel styles remain available.
- Add guarded secondary-click handling and wrapped Polaris console navigation/actions. No gameplay policy or saved-state format changes.

### Requirements

Additive Framework API update; normal game/loader requirements remain unchanged.

### Known limits

- Offline regression and browser-layout checks are separate from owner-run Unity interaction tests. No additional performance captures or Steam publication are claimed.
- Existing faceplates and suitable vanilla graphics are reused after review; no new artwork generation was warranted.


## [0.29.0] - 2026-09-27 - Draft

### Added

- Add shared presentation pacing, change-only widget updates, retained native-widget bindings, fresh ship-scoped equipment discovery and allocation-free equipment-family matching for existing mod consumers.
- Extend disabled-by-default Scope recording with bounded frame intervals, collection counts, calibrated allocation support and crew/discovery timings. Export stays explicit; world changes stop recording.

### Fixed

- Avoid allocating empty state copies and copying prior saved data solely to validate a write. Preserve fresh validation and detached read snapshots.

### Compatibility and limits

- Public helper additions are additive; save formats and gameplay rules are unchanged. See docs/development/performance-audit.md for the six baseline captures, complete source ledger and offline checks. Follow-up captures and measured improvement targets are deferred by owner direction; Unity interaction and performance remain unverified.

## [0.28.1] - 2026-09-27 - Draft

### Documentation

- Review English controls, warnings, descriptions and help for practical player language; retain precise diagnostics and established equipment names. Update current guides, item-reference inputs and the Workshop draft.
- Follow the retrospective language rule and glossary in docs/development/player-language.md, informed by Blue Bottle Games' official Ostranauts description and Daniel Fedor's developer AMA. This is an interest-based audience interpretation, not measured demographic data.

### Compatibility and limits

- Wording only: translation keys, placeholders, commands, saved identities, resource values and gameplay rules are unchanged. This is an unpublished development candidate; Unity text layout remains unverified.

## [0.28.0] - 2026-09-27 - Draft

### Added

- Add shared native appliance state-art binding for dedicated damaged, loose and loose-damaged imagery. Content mods own sprites; the helper changes image and portrait references while preserving physical definitions and saved identities.

### Compatibility and limits

- Agriculture 0.15.0 and Shipbreaker 0.28.0 use the new helper. Existing appliance registration signatures remain available. Native definition checks verify that geometry, conditions, actions and economic fields survive repeat binding. No installation, Steam publication or gameplay validation is claimed.

## [0.27.0] - 2026-09-27 - Draft

### Bulk custody and purchasing

- Add validated commodity/catch/reserve storage and operation-owned endpoint reservations, without changing existing scalar/mixture transfer interfaces.
- Add an independent station Bulk supplies view, exact quotes and measured payment/delivery settlement. Known partial receipts refund missing quantity; uncertain journals block retry and retain evidence. Native fuel and optional Ship’s Water services remain separate.
- Add optional structured equipment selectors for checked C1 configuration drafts. No content-owned chemistry or resources move into UI callbacks.
- Retain the Agriculture-first research and native Blue Bottle Games / Valtora attribution. Offline accounting/native checks are not Unity validation; no Steam publication.


## [0.26.1] - 2026-09-27 - Draft

### Panel corrections

- Keep compact text inside its actual element bounds, shorten the roster shortcut, respect fixed picture/button widths and draw both stock-stepper symbols without relying on font glyph coverage.
- Make Details & diagnostics a reusable expand/collapse block. Show status once, highlight the selected view/equipment, and explain empty time-skip sections.
- Disable unavailable Locate/Clear controls. Locate centres a temporary ship view; Clear explicitly reports a pending draft change, which still needs Apply.
- Add a visible ship-selection banner, dimmed background with candidate openings, object brackets and a cyan line that animates over native-valid hits. Right/middle drag pans, scroll zooms, and Cancel/Escape restores the previous view and draft. Overlaps open a short choice list.
- Isolate native shortcut commands as well as mouse/keyboard world handlers while picking. Do not enter native signal-connection mode or change crew selection, target authorization, object lights or layers.
- Bound multi-line confirmations by both width and height, so notices cannot overflow the footer.
- Preserve saved orders, manual stops, names, resource accounting and existing presentation APIs. Automated geometry, compiled-wiring and build checks are separate from pending owner-run Unity evaluation.

## [0.26.0] - 2026-09-26 - Draft

### Control panels

- Added a shared compact console shell with fixed navigation/actions, independent viewports and a narrow-screen Back layout. Existing panels and native instrument dimensions are not globally resized.
- Separated Orders, Crew & Training and Time-skip. Equipment-specific forms use drafts, complete form validation, stale rejection, Apply/Discard and explicit Resume after changing enabled orders. Disabled and manual-stop states remain intact.
- Added searchable storage/connection pickers, opt-in nicknames, native artwork with neutral placeholders, and an input-isolated ship picker with overlap disambiguation. Native names, object IDs and physical inventories are unchanged.
- Reused the existing original Phobos console frame. Native components remain behind audited adapters with standard-widget fallbacks. Browser previews and automated checks are not Unity gameplay validation.

## [0.25.1] - 2026-09-26 - Draft

### Fixed

- Resolve crew through the native company roster instead of the unused legacy crew-list field, fixing repeated CrewWork.Poll null exceptions and the same fault in crew controls and time-skip preview.
- Skip unresolved, destroyed and uninitialized workers during work discovery. Departure checks use the same native roster but block if any member is unresolved or away.
- Keep saved orders, manual stops, training and native roster entries intact. Automated regression checks are not in-game validation.

## [0.25.0] - 2026-09-26 - Draft

### Crew automation

- Added default-disabled standing orders, native task publication, exact worker context, equipment/input/capacity reservations, per-crew permissions and shared roster/equipment/C1 controls.
- Added persistent specialities and powered-terminal study; completed practice and study combine toward authored 20-hour/10-hour thresholds, with a 20% hands-on duration benefit. Existing terminal actions are retained.
- Added bounded native time-skip coordination, checked travel/handling budgets, measured machine ticks and repair-time sharing. Native rest, care, events, payroll and fuel remain native; exterior operations suspend.
- Saved manual stops and routine resume preferences remain authoritative. Unknown saved records are retained and blocked.

### Compatibility and limits

- Native method/definition and offline checks are not in-game validation. Common Sense integration uses native tasks; live compatibility and UI review remain owner-run.
- See [crew controls, sources and owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/crew-automation.md).

## [0.24.2] - 2026-09-26 - Draft

### Fixed

- Share a presentation-only command-feedback rule across Agriculture and Shipbreaker panels: omit successful complete-line or paragraph echoes already shown in fresh live status. Keep rejection explanations, distinct notices and other-endpoint responses visible. No gameplay, saved state or console command responses change.

### Compatibility and limits

- Agriculture 0.11.1 and Shipbreaker 0.24.1 require this shared helper. Offline regression checks cover full-status echoes, notices, line endings, errors and substring coincidences; native visual confirmation remains pending.

## [0.24.1] - 2026-09-26 - Draft

### Fixed

- Correct outgoing ship dimensions after native save trimming. Blue Bottle Games' inspected Ostranauts 1.0.1.5 writes dimensions before trimming but room/zone indices afterwards; removal of edge objects can therefore produce an internally inconsistent save.
- Load an affected save using a uniquely validated smaller grid, requiring the full exterior boundary and all saved room positions to agree. Correct only in-memory dimensions before Shipbreaker's padding guard. Preserve room IDs, atmosphere, zones, items, wear and construction progress; reject missing or ambiguous evidence. Original archives are not edited.
- The supplied later autosave resolves from its stale 64-by-44 header to 63 by 44, restoring all eight room-position lookups offline. The earlier save remains 64 by 44. Both earlier guards ran; trusting the stale header was the remaining Phobos contribution. See the [evidence and reload check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-room-load-mitigation.md).

### Compatibility and limits

- Framework-only update retaining Shipbreaker's existing grid guard, including 0.23.0 from the failure log and 0.24.0 present at delivery. The owner associates the recurrence with torch use and a six-hour time skip; those triggers are not independently established. The earlier marker-health fix was owner-confirmed, while this third fix awaits gameplay confirmation. No lost gas is invented, and no drive/time controls, saved identities or recipe contracts are changed.

## [0.24.0] - 2026-09-26 - Draft

### Changed

- Quantity-aware merchant and regional-stock overloads request bounded physical lots while retaining the old single-unit API. Probability and stock condition remain independent; native and third-party branches and existing inventories are preserved.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.23.1] - 2026-09-26 - Draft

### Fixed

- Retain living saved construction markers whose recorded damage is below their saved health limit when the native loader would compare that damage against the generic marker's zero health. Applies to verified vanilla and available mod targets during saved item spawning; templates, finished equipment, dead/exhausted markers and ambiguous records keep native behavior. Damage, construction progress and save files are not rewritten.
- Addresses the second pending-construction room-load trigger found in Blue Bottle Games' Ostranauts 1.0.1.5: lightly worn G4/H4 markers were discarded before Shipbreaker's existing grid guard could run. The supplied earlier autosave has no damage overrides; the later save has 69 affected markers, including 67 vanilla wall/floor markers. Read-only native-data audits retain all 69 with the patch.

### Compatibility and limits

- Keep Shipbreaker 0.19.1 or newer for its separate saved-grid protection. Framework's health correction does not replace it, restore already-lost gas or recover missing providers. Shipbreaker 0.21.0 was installed for this Framework-only update. Automated checks cover native hook boundaries and both supplied saves; the owner subsequently confirmed the reported reload worked. The separate stale-header recurrence is addressed in 0.24.1. See the [investigation and owner check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-room-load-mitigation.md).

## [0.23.0] - 2026-09-26 - Draft

### Added

- Shared verified vanilla retail endpoint lookup and additive regional offers. Missing optional kiosks are reported and skipped without redirecting stock or disabling content. Consumers retain ownership of balance; native market pricing and inventories remain authoritative.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Content using the regional helper requires Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

## [0.22.0] - 2026-09-26 - Draft

### Added

- Version-scoped opt-in reject reservations around the inspected [Valtora Ship's Water 0.16.1 Recycler](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189). Bound processing to finite destination space and measure waste debit/potable credit using same-ship tank lists. Unlinked recyclers retain provider behavior.
- Shared admission registry for content-owned collector cargo and the owning collector's mount validator. Existing capacity and native container checks remain authoritative; this does not enable industrial routing of new cargo.

### Known limits

- The adapter reports wet remainder under an authored water-mass convention, not a nutrient assay. The content consumer owns access, pairing, journals and explicit post-load resume. No provider files are modified. Unity hook execution remains an owner check.

## [0.21.2] - 2026-09-26 - Draft

### Fixed

- Resolve native instrument donor audits from BepInEx's managed directory when its byte-loaded game assembly has an empty Location. Keep the pinned SHA-256 check and component/callback isolation; changed or missing donors remain unavailable.
- Supply fixed-field text layout that preserves glyph size, normalizes native-font baseline spacing and delegates clipping to the owning mask. Auto Nav uses it to correct blank compact labels and clipped telemetry without modifying shared font assets.
- Add regressions for memory-loaded assemblies, mismatched/missing donor files and native font metrics. No saved-state changes; native interaction and visual confirmation remain owner checks.

## [0.21.1] - 2026-09-26 - Draft

### Added

- Shared saved-grid padding planner for Shipbreaker's pending-construction load mitigation. Validate integral origins, containment and bounded expansion without changing saved records or shrinking live geometry.
- Regression coverage for the reported room-index mismatch, all grid edges, origin changes and invalid bounds. Framework alone does not patch ship loading; Shipbreaker owns activation. The owner reported a successful affected-save reload with Shipbreaker 0.19.1 on 26 September 2026; broader coverage remains unverified.

## [0.21.0] - 2026-09-25 - Draft

### Added

- Share the original quiet completion cue across content mods with one native-effects player, volume/mute and real-time burst suppression. Transient watches retain visible outcomes; dropped events never replay.
- Seed the shared volume from an existing first-trial Shipbreaker setting only when no shared setting exists. Audio failure remains isolated from game operations.

### Known limits

- No save migration. Unity playback and listening evaluation remain owner checks. Low playback priority does not guarantee suppression during native alarms.

## [0.20.0] - 2026-09-25 - Draft

### Added

- Bounded multi-receiver port banks retain the original single-pair identity.
- Reusable retained two-component fluid lines preserve cargo, route binding and transit clocks across reloads.
- Bounded hydraulic resistance and shared-budget allocation support Agriculture and optional furnace coolant servicing.

## [0.19.0] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Native construction, maintenance, additive merchant stock and material accounting.
- Versioned saved state, physical transfers, explicit endpoint pairing and ship-scoped equipment controls.
- Measured power and liquid delivery, shared conduit routes, finite solution accounting and optional provider adapters.
- Translation lookup, reusable native instruments and opt-in Phobos Scope performance recording.

### Requirements

Ostranauts 1.0.1.5 and BepInEx 5 (inspected baseline 5.4.23.5). No Shipbreaker, Crafting Framework or Salvage Workshop dependency.

### Known limits

- This is an experimental shared library. Successful offline checks do not establish in-game compatibility.
- Phobos Scope recording is disabled by default. The recorder is bundled; no Rust process is needed during play.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/framework-author-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
