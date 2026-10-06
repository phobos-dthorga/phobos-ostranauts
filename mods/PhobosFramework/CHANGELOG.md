# Phobos Framework changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Changed

- Reviewed every English entry. Simplified crew orders, trading, storage and shared fault messages. Kept detailed file and developer diagnostics, units and commands.

### Documentation

- The crew guide has two new charts: where housekeeping puts a loose item, and how a time-skip moves forward step by step.
- The player guide now explains how much power machines draw, what one MHD generator can supply (about 27 MW, read from the game's code) and that drawing more electricity costs the reactor no extra fuel.

- JSON Schema files for the four data packs in the repository's schemas folder, for editor completion of shipped packs and player override files, generated from the same field sets the offline validator uses; the editing guide shows how to point an editor at them. Built packages now carry a manifest the installer checks.

- Audit linked player documentation and correct generated placement wording for section assembly while preserving direct installation of complete machinery. No gameplay or save changes.

- Extend maintained item evidence to actual native maintenance generation, attachment and fresh/worn/contained target checks. Document shared dismantling restrictions for cargo, lots and stacks; add a read-only aggregate save audit. Runtime behaviour is unchanged.

- Investigated additive bulk-storage and station-purchase contracts for Agriculture, including native refuelling and Ship's Water coexistence, custody, reservations and UI isolation. Published a research blueprint; the investigation itself registered no runtime API, equipment or service. Framework 0.27.0 below later added the shared bulk-supply storage and station purchase services.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Shared native INSTALL category constants and pre-publication validation prevent unreachable visible installation entries.

## [0.125.0] - 2026-10-06 - Draft

### Added

- Press twice to go ahead (owner rule, 6 October 2026). A change or button that needs other steps first, such as a machine paused for a moment, an old link removed or your unsaved changes applied, is no longer refused for that. The first press says in the notice what will be done, and the button reads Apply again or Start again; pressing it again does it. Machines paused for the change carry on afterwards and keep their standing crew orders; one that cannot, because it is damaged for example, stays paused and the notice says why. If anything changed between the two presses, the second says what it would now do and waits. In F3, end the command with confirm. Settings sheets and the shared Control Panel buttons work this way now; each content mod adopts it for its own machines.
- For mod authors: the Confirmations, PressGuard and PausedChange services, and a confirmation card for knobs and guarded switches. An audit file and check keep every do-first refusal classified.

### Changed

- Crew panel: Resume with unsaved changes applies them on the second press instead of refusing.
- Water silos: the refusal that mixed a silo fault with a transfer still running is now two, each saying what it is.

### Save compatibility

- Automatic. Nothing new is saved; a pending second press is forgotten when the panel closes.

### Compatibility and limits

- Things a press cannot fix are still refused, with the reason: a hot machine, contents in a line, damage, a saved-contents fault, another ship or console, or a full link bank where you choose which link goes.
- Manufacturing, Shipbreaker and Auto Nav adopt it in their next releases. Checked offline; not yet tested in play.

## [0.124.1] - 2026-10-06 - Draft

### Changed

- Gas and process-water lines, conveyor belts, water silos and drain canisters now use their makers' in-world product voice, with handling limits and warnings retained.

### Compatibility and limits

- Wording only. Equipment identities, translation keys, values, controls and saved state are unchanged. Checked offline; the owner still reviews the descriptions in the game.

## [0.124.2] - 2026-10-06 - Draft

### Fixed

- Story letters could stop the story record saving. Since 0.122.0 the letters kept for the Letters window were written with commas, which the save store refuses, so once any correspondence had a letter the whole story record (arcs, flags, news seen) was refused on save, with a refusal line in the log. Letters are now written in the store's own form and a long correspondence is split across several entries; a run the store would still refuse leaves out only that correspondence's letters, with a log line, never the rest of the record. Found by code review on 6 October 2026; not seen in play.

### Save compatibility

- Automatic. No record written by 0.122.0 to 0.124.0 holds letters (the store refused them), so nothing needs converting; the old form is still read in case.

## [0.124.0] - 2026-10-06 - Draft

### Added

- A FleetRefreshSeconds setting in the Gigs section (owner request, 6 October 2026): how often your ships are read again for fair gig deadlines, 10 to 600 real seconds. The default is now once a minute rather than every 10 seconds, since ships change rarely. Game speed, such as 16x, does not change real time, so it does not change this either.

### Save compatibility

- Automatic. Nothing is saved.

## [0.123.0] - 2026-10-06 - Draft

### Added

- Fair gig deadlines (owner direction, 6 October 2026). The game gives a far Gig Nexus delivery 70 hours per AU in a straight line, whatever ship takes it, quicker even than its own long-range ferry at 80 hours per AU. Framework now times the trip for each torch-equipped ship you own, on the game's own trip planner: accelerate at the game's 2 g torch limiter, coast at the torch speed limit if the trip is long enough, brake to arrive. It averages the trips across those ships, adds a quarter for course changes and 4 hours to undock, dock and turn in, and allows that time when it is longer than the game's. It never shortens the game's own time. A 0.7 AU delivery at 1 g, for example, needs about 57 hours where the game allowed 49; it now allows about 76.
- The speed bonuses (x2, x4, x8 for a third, two fifths and two thirds of the time) follow the new allowance, so a direct trip at your fleet's pace earns the base pay or the x2 bonus.
- A far offer's text at the Gig Nexus now names its destination, how far it is from the pickup, and how its time was set. With no torch ship of yours, it says the game's own time stands and how long the game's ferry would take.
- Settings in the new Gigs section: FairDeadlines (on), TripMargin (1 to 3, default 1.25) and DockingHours (0 to 48, default 4).
- Shared for other mods: a torch drive's rating at the game's limiter, torch trip times, and the player's own ships. Auto Nav and War Has Been Declared now use them.

### Save compatibility

- Automatic. The game saves each gig's time allowance itself. Offers already on a board are corrected the next time the board lists them; gigs you have already taken keep their deadline.

### Compatibility and limits

- The trip is a straight line between the two ends when the offer is listed, as the game measures it; orbits move on. Fuel is not counted: a refuelled torch is assumed. Gigs not counted as far by the game (within 5,000 km, or on a transit link) are unchanged. Not yet tried in the game.

## [0.122.0] - 2026-10-06 - Draft

### Added

- A Letters window for story correspondence (owner choice, 6 October 2026). Click a story goal in the GOALS list to open it: every correspondence you have begun, open ones first, with its letters in order and dated, the writer's face, and the goal it is at. Letters no longer get lost among kiosk lines in the crew log. F3 phobosframework story letters opens it too.
- Story replies. A story step can now wait for your answer: two to four replies, chosen in the Letters window, each leading the story its own way. A reply that needs something first (credits, an item, a skill) is shown locked with what it needs. You confirm a reply before it is sent, and it cannot be taken back. The crew log says when a letter is waiting for your answer. F3 phobosframework story answer sends a reply through the same checks.
- Story writers can offer replies with a step's choices field; the story guide, its ChatGPT prompt, the checker and the JSON Schema cover them.
- A shared pick-one card for any mod's panels, now also used for the console's unsaved-changes question.

### Save compatibility

- Automatic. Each correspondence's letters and replies join Framework's existing story record; older Framework versions keep them untouched and still read every arc. A correspondence begun earlier shows the letters its progress implies, without dates.

### Compatibility and limits

- Existing stories, including Spacer Stories, keep their current steps; replies appear as new content uses them. The window and replies have not yet been tried in the game.

## [0.121.0] - 2026-10-06 - Draft

### Added

- Story goals show who they are from (owner request, 6 October 2026). A goal from a story correspondent shows their face in the GOALS list, made from the game's own portrait parts by the game's own face roll, the same way crew portraits are made. The face is rolled the first time it is needed and kept in the save, so a correspondent looks the same for the whole game. A goal from no one in particular shows the game's own wrist PDA picture. Each goal also ends with a line such as From Orra Pell, equipment broker (Port Mojave).
- Story writers may give a person a face look (masculine, feminine or any) and a goal its own person. The story guide, the checker and the JSON Schema cover both.
- Two shared tools for other mods: game-made faces for people who never appear in the world, and the game's grammar for reworded social lines.

### Fixed

- Story small talk printed its lead-in as written, with the raw us, asks and them grammar tokens in brackets before the line. The game only expands grammar it prepared when it loaded, so each lead-in is now prepared through the game's own preparation first. A line now reads You ask Jorge White: or Jorge White asks you:, the same in the conversation and the social log. If a later game version removes that preparation, the game's own line is kept and the log says so once.

### Save compatibility

- Automatic. Each correspondent's face parts join Framework's existing story record on the player; older Framework versions keep them untouched. Goals already in a save gain their face and From line on the next load. The game itself does not save a goal's picture, so Framework gives it again on every load.

### Compatibility and limits

- Vanilla goals keep the game's own pictures (owner choice). Faces and the corrected small talk have not yet been seen in the game.

## [0.120.0] - 2026-10-06 - Draft

### Added

- Sounds you can replace (owner decision, 6 October 2026). The completion cue and the eight machine loops are now ordinary WAV files in the sounds folder beside the Framework plugin, not hidden inside its DLL. A file of the same name in BepInEx/config/PhobosFramework/sounds/ plays instead: 16-bit PCM, mono or stereo, up to 10 seconds for a loop and half a second for the cue. A replacement loop is levelled to the game's appliance loudness like the shipped ones, and a louder cue is turned down to the shipped cue's quiet peak. A file that cannot be read is named in the log and the shipped sound plays. The log also names every replacement in use. The machine work sounds guide lists which machines play each loop.
- The read-only data copies in every Phobos mod's framework folder now say so on their first line (owner request, 6 October 2026). The game reads those tables from inside each mod's DLL, so editing a copy changes nothing. The note names the folder to use instead, where a .json file of any name is applied on top. The editing data files guide explains both.

### Changed

- Framework's DLL is about 4 MB smaller, now that the sounds sit beside it.

### Save compatibility

- Automatic. Nothing is saved.

### Compatibility and limits

- Install with the repository's installer or the Workshop package. A DLL copied on its own plays no sounds, and the log names each missing file. Only the completion cue and machine loops can be replaced; recipes and other tables still change through data files. Not yet heard in the game.

## [0.119.0] - 2026-10-06 - Draft

### Added

- Machine work sounds for every Phobos mod (owner request, 6 October 2026): a machine plays a steady motor-and-pump loop while it actually works, fading in when work starts and out when it stops, idles, runs out of power or is unloaded. Each content mod chooses each machine's loop and pitch; Framework plays them.
- The sounds are set up like the game's own appliance loops: the same sound bus, distance falloff and range as the air scrubber, so they fade with distance, follow the game's volume settings and muffle in thin air as its own machines do. Each loop is levelled to the scrubber's measured loudness when the game starts, and the log says what it measured.
- Only the nearest working machines are heard at once (four by default), so a room full of machinery does not drone together. Two settings in the Audio section: MachineSoundVolume (0 to 1, 0 mutes, separate from the completion cue) and MachineSoundVoices (0 to 12).

### Save compatibility

- Automatic. Nothing is saved; sound never changes work, power or records.

### Compatibility and limits

- Eight loops generated with ElevenLabs Sound Effects (attribution: ElevenLabs, elevenlabs.io) ship inside the plugin; they are not covered by the repository MIT licence. The sounds have not yet been heard in the game; the loudness match, the number of voices and each machine's choice await the owner's listening.

## [0.118.0] - 2026-10-06 - Draft

### Added

- Colour now repeats what the words say in the Crew panel and on the Maintenance sheet, with the panel's three colours only. Amber: it needs you (a stopped or unreadable order, an inspection due, work that waits). Green: working, or the one button that moves things forward. Slate: everything else. There is no red.
- Tabs count what needs you there, such as Orders (2) or Upkeep (3), and such a tab is amber when you are on another one.
- Group headers take their group's colour: Needs you, Needs attention and Waits are amber; Will run is green.
- Status cards are tinted by the state they describe, and change with it.
- Apply is green only while you have changes to apply. Resume is green only when nothing is pending and a stopped order, with work chosen, is ready to go again. Stop stays amber in the same place. Every button still works as before.
- On the Maintenance sheet, Open standing orders is amber when the order needs you, and Open upkeep switches when the machine's upkeep is due.

### Save compatibility

- Automatic. Nothing new is saved.

### Compatibility and limits

- Every colour sits beside a word or a count that says the same thing, so nothing depends on telling colours apart. The shared Control Panel and the C1 console are unchanged. Checked offline; not yet seen in the game.

## [0.117.0] - 2026-10-06 - Draft

### Added

- The Crew operations panel opened from a machine shows that machine alone: its order, or a line saying it takes no crew orders and how it is loaded, and its upkeep. Show all ship widens to the whole ship. Opened from the crew roster it starts with the whole ship.
- The Orders list is in groups you can fold, with counts: Needs you (stopped or unreadable orders, open), Working (running or waiting for feed, space or crew) and Not set up or off. Each row names the machine, its state, its work and the one thing it waits for.
- The Upkeep page has one row per switch with a Turn on or Turn off button, and its answer goes to the footer. Machines are grouped as Needs attention (not tuned yet while tuning is on, an inspection due while rounds are on, or a record that cannot be read), Looked after and Inspection only. Choosing a machine shows its tune and last inspection.
- The Time-skip estimate reads each crew member's coming hours as one line (working for 3 h, then resting for 2 h) and groups the orders as Will run, Waits (with the reason) and Paused for the skip.
- A Phobos operations section in the game's encyclopedia: standing orders, upkeep, time-skips, store links and maintenance. Each Crew panel page and the Maintenance sheet have an About button that opens the matching article and says in the footer whether it did.
- F3: phobosframework articles lists those articles and phobosframework help with a name opens one; phobosframework skip with an optional number of hours prints the time-skip estimate.

### Changed

- The Maintenance sheet says each thing once: one line for a machine without crew orders, a sentence for its upkeep, then what blocks taking it up, or a line saying nothing does. The general explanation of install, repair, Restore and dismantling moved to the encyclopedia.
- The long explanations on the Upkeep and Time-skip pages moved to the encyclopedia. The Upkeep switches and machine list stay on screen in a narrow window, where they were hidden before.

### Save compatibility

- Automatic. Nothing new is saved. Which groups are folded is remembered until the game closes.

### Compatibility and limits

- Encyclopedia articles open by their name, as the game's own encyclopedia looks them up; a check confirms the game still offers that lookup. Whether the encyclopedia window shows above the Crew panel has not been seen in the game. If a story pack loads after the encyclopedia was built, About says so and asks for a restart. Checked offline; not yet seen in the game.

## [0.116.0] - 2026-10-06 - Draft

### Added

- The right-click Maintenance information entry is now called Maintenance, and on an installed machine it also shows the machine's standing order (what it is set to, its state and who is on it) and its upkeep (tune, last inspection, the ship-wide switches), with Open standing orders and Open upkeep switches buttons that open the Crew panel on that machine's order or on the Upkeep tab. The removal and repair notes follow as before; pipes, belts and stores show only those. If the Crew panel cannot open, the sheet says why. Every Phobos mod's equipment gets this without an update of its own.
- A new stores data pack says which of the game's containers are not stores. Ship weapons, battery chargers, CO2 scrubbers, nav consoles, sinks, toilets, the Testudo safe pump and anything without an Inventory entry on its right-click menu are no longer offered under Take feed from or Send products to, are left out of the list of stores aboard but not offered, of crew-order stores and of housekeeping, and crew never take anything out of them. Players and add-ons can extend the lists by the game's own condition and container names.
- Content mods can let equipment with crew orders still count as a store for other machines, so a machine's tray can take deliveries after it gains orders.

### Save compatibility

- Automatic. Nothing new is saved; the maintenance entries keep their ids. A store already chosen that the new rule excludes stays recorded, and the machine's status says it is not a store any more.

### Compatibility and limits

- Equipment from other mods is covered only where it carries one of the listed game conditions or container rules, or lacks an Inventory entry. Checked offline (rule, native and data-pack checks against the game's own definitions); not yet seen in the game.

## [0.115.0] - 2026-10-06 - Draft

### Added

- Story content can now read how the game's factions regard you, who is aboard and what time it is. New requirements: standing (a faction at least or at most a tier: disliked, neutral, warm, friendly, trusted, honored, read exactly as the FACTIONS app shows it), crewWith (someone aboard other than you has a skill or condition), crewCount, running (a Phobos machine is running on your ship), months and hours (a window of the day in UTC, wrapping midnight). Small talk can name speakerFactions, so AyoSec lines come from AyoSec people. The new placeholder for a crew member's name picks one of your crew at random.
- An arc step can change how a faction regards you, by up to 10 points either way and at most two factions a step (owner choice: small changes), through the game's own faction scores, with a line in the crew log saying who thinks better or worse of you and your standing now.
- F3: phobosframework story standing followed by a faction and a change, for testing; story where now lists crew aboard, the month and hour, and your standing with every faction a loaded place or person names.

### Save compatibility

- Automatic. Standing changes are the game's own faction scores, as a crime or a bounty changes them; nothing new is saved by Framework.

### Compatibility and limits

- Before a story pack relies on standing changes, run the F3 command once and confirm the FACTIONS app figure moved; the code uses the call the game's own debug command makes, but this has not been seen in play. The encyclopedia still cannot be unlocked by a flag. Checked offline (rule, native and data-pack checks).

## [0.114.0] - 2026-10-06 - Draft

### Added

- Story content now belongs somewhere (owner request: stories came out of nowhere, unrelated to anything or anywhere). Story files gain three tables: places (stations by the game's registration id), people (named recurring characters with a home and a role) and threads (a story's place, cast and shared requirements, which every entry declaring the thread inherits). Framework ships the game's twelve regional stations and the parts and neighbours within them as places, with the game's names, bodies, factions and news regions.
- News and adverts of the place you are at or in are picked about four times as often as news of elsewhere (settings localWeight and farWeight); a news item takes its Region News label from its place. Small talk with a place is said there: by your crew while you are at it, by others only when they are there themselves (new speakers value locals). An arc with a place starts by itself only while you are there, local arcs are tried first, and a dock-at test may leave out its station to mean the arc's place. Letters can come from a named person, shown as Name, role.
- Threads connect: an arc outcome can set and clear story flags (setFlags, clearFlags), and every kind of content can require flags, notFlags, arcsActive, arcsAtStep (an arc under way at a step), places, regions and newsSeen. People stop mentioning a news item mentionDays after it was shown (10 by default), and never mention one that was not shown. A once-only bulletin already shown is not shown again.
- Arriving in a new region runs the story check at once, so local news and arcs appear on arrival.
- New placeholders in story text for the place, its region, the docked station, the place's body, the date and a named person.
- F3: phobosframework story where (region, place, docked station, date, flags and open threads), story thread followed by an id (its members and what blocks each), story flag followed by an id and optionally clear, story places and story people. All optional, for authors.
- The writing guide has a new section, the requirements table and a rewritten ChatGPT prompt that makes every entry belong to a thread and a place and every letter to a person.

### Save compatibility

- Automatic. The player's story record gains flag entries and keeps the time each news item was shown; a record from an earlier version reads as before. Older story files load unchanged; a newer file needs Framework 0.114.0.

### Compatibility and limits

- Faction standing gates and rewards, crew and clock gates follow in the next release. The encyclopedia stays as it was: it cannot yet be unlocked by a flag. Checked offline (rule, native and data-pack checks); the arrival check, placed small talk and TV weighting have not been seen in the game.

## [0.113.0] - 2026-10-06 - Draft

### Added

- Crew upkeep gains two switches on the Upkeep tab, both off until chosen. Upkeep now runs tuning and inspection first, then housekeeping, then practice, always after standing orders.
- Housekeeping: idle crew on shift with the Haul duty put supplies lying on the deck away, one item at a time. A Phobos supply goes to the nearest store already holding the same kind, else to a store a content mod names for it (Shipbreaker's Y bins). Anything else goes only to such a store, so the game's own clutter is never sorted into lockers. Nothing is taken from hands, containers, trays or drawers; equipment waiting to be installed, items another job is carrying and items with nowhere to fit stay put; nothing is put into a machine.
- Practice at machines: a crew member not yet skilled at a machine with a Phobos speciality (Industrial Processing, Agriculture, Cooking) practises there for 10 game minutes a session, learning as fast as studying at a terminal. In play, one crew member per speciality and ship practises at a time; the machine is not changed.
- The upkeep data file gains practiceMinutes (2 to 60, default 10). F3: phobosframework upkeep practice on or off, and upkeep tidy on or off.
- Time-skips spend banked crew time on housekeeping and practice too, in the same order as in play.
- For mod authors: Upkeep.RegisterTidyStore names stores housekeeping may fill; Upkeep.RegisterUnavailable keeps chosen crew, such as a resting patient, out of upkeep work.

### Save compatibility

- Automatic. The switch record gains two fields; a record from 0.111.0 or 0.112.0 reads with both new switches off. Practice progress goes into the existing speciality record.

### Compatibility and limits

- A stack on the deck is carried one item per trip, as standing orders haul. Housekeeping figures and the practice rate are authored balance. Checked offline with the builds and their rule checks; not yet seen in the game.

## [0.112.0] - 2026-10-06 - Draft

### Fixed

- Time-skips with crew orders on no longer freeze the game for minutes (owner report, with Phobos Scope captures). Such a skip moved one game second at a time, about 14 ms of work for each second skipped on a fast PC, so a six-hour skip took roughly five minutes. Every stepped skip now moves 30 game seconds at a time by default.
- Crew jobs in a skip no longer cut the step short when they finish. A job ends with the step it finishes in, and the seconds left over start the worker's next job, so crew get through about as much work.
- In a skip, the game's own powered fittings (lights, doors, life support) take their power every fourth step instead of every step, still from the same supply as the machines. Phobos machines, crew-ordered equipment and rooms are still stepped every step.
- The reactor and battery chargers take each skip step in one-second slices. The game charges a battery by a fixed share of what it lacks at each power step, not each second, so a longer step would otherwise charge batteries more slowly than play does. This also corrects machine-only skips since 0.99.0, which charged batteries at a tenth of the usual rate.
- A machine replaced partway through a skip (damage, repair, installation) takes power and works from then on; before, it sat out the rest of the skip.

### Added

- Setting TimeSkip StepSeconds in Framework's config file: game seconds a skip moves at a time, from 1 to 60, 30 by default. Higher means a shorter freeze. A long step costs some accuracy: crew take their next job only when a step ends; a machine that finishes a batch partway through a step may wait for the next step; each MHD generator is topped up once a step, so on its own it feeds about 240 kW at 30 seconds and 120 kW at 60 (batteries supply too); and a busy machine in a small room can wait for cooler air all skip. 1 is the old crew-order behaviour.
- Phobos Scope captures now record each whole skip step (framework.skip.step) and its crew assignment (framework.skip.crew).

### Save compatibility

- None. Nothing is saved differently.

### Compatibility and limits

- The figure of about 14 ms per skipped second comes from the owner's capture of the start of one six-hour skip; the new skip length has not been measured. Checked offline with the builds and their rule checks; not yet seen in the game.

## [0.111.0] - 2026-10-06 - Draft

### Added

- Crew upkeep, for long hauls with little to do (owner request). Two switches for the whole crew, off until chosen, on the new Upkeep tab of Crew standing orders and training: Tune machinery and Inspection rounds. Idle crew on shift with AutoTask on take them after every standing order.
- Tune machinery: a session at a machine takes 10 game minutes and adds a fifth of a full tune, three tenths for a skilled crew member. A fully tuned machine works 10% faster and draws that much more power and gives off that much more heat while it works, so each job costs the same electricity and finishes sooner. What a job takes and gives never changes. The tune fades as the machine works, a full tune over 36 hours of work; an idle machine keeps it. Damage or taking the machine off its mount clears it.
- Inspection rounds: a 5 game-minute visit to each machine not inspected in the last 24 game hours. An inspected machine's tune fades at half the rate, and the crew log says what the machine is waiting for, or that it is more than half worn. A machine in good order gets no line.
- Each tunable machine's panel ends with its tune.
- Settings in Framework's config file, section Upkeep: InspectionMinutes (5), TuningMinutes (10), MaxTuningGain (0.10; 0 turns the benefit off) and TuneFadeHours (36).
- A new upkeep data file holds the session steps, how long an inspection is good and each machine family's share of the gain. Players and add-ons can change it; the data-files guide has a section, with a JSON Schema and the offline checker.
- F3: phobosframework upkeep lists the switches, the settings and every machine's tune; upkeep tune on or off and upkeep inspect on or off set the switches. Optional; the panel does the same.
- During a time-skip, on-shift crew time goes to upkeep sessions directly, without slowing the skip or taking from the game's own repairs.
- For mod authors: Upkeep.Register declares a machine family, and Upkeep.Draw scales a working machine's power request by its tune.

### Save compatibility

- Automatic. The switches are a new record on the player and each machine's tune a new record on the machine; a save without them reads as switched off and untuned. Nothing existing changes.

### Compatibility and limits

- All figures are authored balance. Practice at machines and housekeeping are planned for a later version. Checked offline with the builds and their rule checks; not yet seen in the game.

## [0.110.0] - 2026-10-06 - Draft

### Added

- Story data files: an arc reward can give a data card (the game's own Renbao R014) holding story files, read on any computer or PDA like the game's own files. The first opening of a file can start an arc, and arcs can require files opened (filesRead).
- Encyclopedia sections and articles can carry a picture (image), a path under a mod's images folder.
- F3: phobosframework story file followed by a file id gives you a data card with that file. Optional, for authors.

### Save compatibility

- Story files are Framework data objects on ordinary data cards, keeping their name and story file id. A file whose story pack is removed reads as corrupted; removing Framework removes the definition, as for every Framework item. The story record remembers which files were opened.

### Compatibility and limits

- Story files come only as arc rewards for now; where they could be found as world loot is held for an owner decision. Checked offline; not yet seen in the game.

## [0.109.0] - 2026-10-06 - Draft

### Added

- Story goals can test for credits held (and take them when the step finishes, with a line in the ledger) and for any game condition the player has, such as a skill.
- Story steps can branch: up to four other ways out of a step, each with its own tests, rewards and next step. A step can also name the step that follows, or end the arc; the first set of tests to pass decides.
- Story rewards can pay credits, up to 50,000, entered in the game's ledger.
- Story entries can require game days of story time with afterDays and beforeDays. Story time starts with the player's story record; in a game started earlier, the first time it loads with this version.
- The F3 story report shows each branch's tests.

### Save compatibility

- The player's story record gains the time story time began. Credits paid or taken are the game's own credits and ledger lines.

### Compatibility and limits

- Faction reputation rewards are held for an owner decision. Branches are chosen by tests, not from a menu. Checked offline; not yet seen in the game.

## [0.108.0] - 2026-10-06 - Draft

### Added

- Story small talk: lines from story files are said in the game's own chatter. When a character mentions a headline, cracks a joke, complains, tells a story, recites jargon, warns of a superstition, admits a worry, asks a deep question or shoots the breeze, about 40% of the time a fitting story line is said instead, after a short lead-in. Heard in the social log and in conversations the player takes part in.
- Each line can be for anyone, only for people aboard the player's ships, or only for people elsewhere, and can require what news and arcs can. A news item can carry a mention, so people talk about the news while it is current.
- Story loading tips: about 30% of loading-screen lore tips can come from story files.
- Story encyclopedia articles, under two shared sections, Makers and brands and Life between stations, or a pack's own. A section shows only while one of its articles does.
- The settings gain chatterShare and tipShare. F3: phobosframework story chatter lists the lines each moment can use now; story chatter followed by a line id makes the next matching small talk say it. Both are optional.

### Save compatibility

- Nothing new is saved. Only the text of the game's own small talk changes; tips and encyclopedia pages are not saved by the game. Removing a story file just stops its lines, tips and articles appearing.

### Compatibility and limits

- Tips and articles exist before any player does, so they can require only installed mods and use no placeholders. An article added mid-session may need a game restart to show. Checked offline; not yet seen in the game.

## [0.107.0] - 2026-10-06 - Draft

### Added

- Story content: TV news, adverts and story arcs (short chains of goals in the GOALS list) written in data files. Each mod may ship a story pack; players add files in BepInEx/config/PhobosFramework/story and add-ons under phobos/PhobosFramework/story.
- About a third of TV news and advert picks come from story entries the player qualifies for; the rest stay the game's own. A step of an arc can also put a news item on the next TV news.
- Arc steps send a message to the crew log, show a goal, and finish when their tests pass: docked at a station, carrying items (which can be taken), items on the player's ships, or game hours waited. A finished step can give items.
- Entries can require mods, player conditions, items aboard, a station and other arcs, and text can name the player and their ship.
- Dismissing a story goal sets that arc aside for good in that game.
- Story content needs no commands: every story file loads with the game, and news, adverts and arcs turn up by themselves. Optional F3 commands help authors test: phobosframework story lists the packs, anything left out and why, where you are docked and each arc's progress; story start, reset, news, check and items save waiting.
- The settings (share of the TV, seconds between checks, arcs at once) are in Framework's story pack.
- A guide for writing story content, with a ready prompt for ChatGPT, and a JSON Schema for editors.

### Save compatibility

- The player carries one Phobos record of story progress, and each story goal keeps its own name. Removing a story file, or Framework, lets its goals finish and disappear on the next load; the game logs No such CT once for each.

### Compatibility and limits

- No game plots, pledges or conversations are used, so crew do not yet talk about story topics; that is the next phase. Story steps advance at the 30 second check. Checked offline; not yet seen in the game.

## [0.106.0] - 2026-10-05 - Draft

### Changed

- Smoother play on ships with many objects. Two checks that rescanned every object aboard every two seconds now do so only when something changed, or every 30 seconds as a safety net. On the owner's ship each rescan covered about 4,800 objects and took up to 22 ms (pipe layouts) and 27 ms (crew orders), long enough to drop a frame on most PCs.
- Pipe and line layouts: every two seconds only the pipes and machines on the lines are checked for a lock, an owner change or a ship that finished loading. Laying, damaging, repairing or removing pipe, floors or walls still updates the layout at once, as before.
- Crew orders: every two seconds only machines with an order switched on, and machines just damaged, repaired or installed, are looked at. Equipment that comes aboard or into reach is found within 30 seconds instead of two.

### Save compatibility

- Nothing saved changes.

### Compatibility and limits

- A change the game reports in no way the mod can see, other than those above, can take up to 30 seconds to show in a pipe layout. Checked offline; not yet measured in the game.

## [0.105.0] - 2026-10-05 - Draft

### Added

- Performance captures record each measured section's self time: the time it spent outside its own measured sections. Totals overlap; self times do not, so they show where the time actually went.
- Every window of a recording carries the recording's id and its window number, so the Scope analyser's new series command can join an hour of windows into one timeline of memory, footprints, frame times and section cost, with the trend of each memory floor per recorded hour.

### Changed

- Ships Phobos Scope recorder 0.3.0 (capture format 3). The installer requires recorder 0.3.0 or later.
- The comparison script reads format 3 and reports self time per second.

### Save compatibility

- Nothing saved changes. Recording stays off until you start it.

### Compatibility and limits

- Self time is elapsed time on the main thread, not CPU use, and includes game work a section waited on. The series time axis is recorded time: gaps between windows, such as world loads, are not on it.
- Checked offline with synthetic captures; not yet recorded in the game.

## [0.104.0] - 2026-10-05 - Draft

### Added

- Performance captures now measure memory. Once a second while recording they read the managed heap, Unity's own heap and native memory figures, and the game process as Task Manager shows it, plus the heap at the first frame after each collection. A reading the game cannot give is left out and named in the capture, never recorded as zero.
- Footprint counts: Framework and each mod can report how many things they keep alive, such as crew orders, jobs, cached line layouts and machine sessions, read on the same once-a-second cadence. Mods add theirs with Performance.RegisterFootprint.
- Frame times are also counted into fixed buckets, so a summary capture keeps long-frame counts and frame percentiles as bucket bounds.

### Changed

- Ships Phobos Scope recorder 0.2.0, which writes capture format 2. Every counter keeps one complete total, and summary captures keep those totals instead of every sample, so one busy counter can no longer fill a window and push out the rest. The installer requires recorder 0.2.0 or later.
- The comparison script reads format 2 captures, reporting frame percentiles as bucket upper bounds and memory and footprint ranges, and warns when a comparison mixes formats.
- Capture metadata names the Medical and War Has Been Declared versions too.

### Save compatibility

- Nothing saved changes. Recording stays off until you start it.

### Compatibility and limits

- Memory figures are whole-game figures; footprint counts are counts, not bytes. Memory per operation is not available, because the game's runtime does not support the per-thread allocation counter.
- Checked offline with synthetic captures; not yet recorded in the game.

## [0.103.0] - 2026-10-05 - Draft

### Added

- A pipe its own machine fills can now join equipment the way the water line does without being topped up from stores. Agriculture's irrigation pipe uses this.

### Changed

- When a line does carry the cargo but a store or machine has no fitting for it, such as one of the game's own canisters on the gas line, the link list now says so and asks for it to stand within one tile. It used to say no line carries that cargo.
- Ship's Water tanks whose fittings another mod changed, so they cannot take a water-line fitting, are now named in the log. They join Phobos lines only by touching.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.102.0] - 2026-10-05 - Draft

### Added

- Recipes can be retired. A retired recipe stays known to the game, so a table job a save already queued finishes or cancels with the parts it was given, but no table offers it again. A changed construction bill now gets a new recipe id and retires the old one.
- Construction recipes can use the game's own Polaris navigation modules as parts.
- A part-used nutrient charge can follow a new price when the game loads, in proportion to what is left in it.

### Changed

- Economy audit, 5 October 2026: the S3 process water silo turns up in engineering salvage as often as each Shipbreaker machine, now about 1 roll in 220, three in four broken (it was 1 in 30, half broken).

### Save compatibility

- Automatic. Queued table jobs finish with their old parts; saved items keep everything else.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.101.0] - 2026-10-05 - Draft

### Fixed

- Loading a save, or returning to the menu, no longer reports stores as destroyed. Each time a ship was unloaded the log said every tank, silo and store aboard was destroyed and its contents gone, and a K2 or AX-2 claimed to have dumped its gas into the room. Nothing was lost: the save kept it all. Unloading is now recognised as unloading, so stores, reactors and pipe contents say nothing while a ship is unloaded.

### Compatibility and limits

- A store destroyed in play is still reported, and its hazards still apply.
- Automatic. Nothing saved changes.
- Checked offline; not yet seen in the game.

## [0.100.0] - 2026-10-05 - Draft

### Fixed

- Pipes laid in walls now work. The game lets you lay a water, gas, acid, ethanol, irrigation or coolant line inside a wall, where a ship's own conduit runs, but every segment on a wall tile was quietly ignored, so a line that looked whole was cut wherever it entered a wall. Machines at the far end then never offered the silo or store. A segment now counts when it sits on an intact floor or in an intact wall.

### Added

- A lines data pack, framework/lines.json, holds the rule for where a segment counts: the tiles that carry nothing, and what must stand on a tile, installed and intact. It is no longer written in code. Players and add-ons can change it, or give one kind of line its own rule, with a file in BepInEx/config/PhobosFramework/lines. The editing guide has the details, and schemas/lines.schema.json gives editor completion.

### Compatibility and limits

- Automatic. Lines already laid through walls join up as soon as the ship loads; link the machine from its panel as usual. Nothing saved changes.
- By the shipped rule a segment still does not count on flex floor, on an outside (EVA) tile, or where both the floor and the wall under it are gone or damaged.
- Checked offline; not yet seen in the game.

## [0.99.0] - 2026-10-05 - Draft

### Fixed

- Machines now work through the game's time-skip. Skipping hours used to hand every running machine the whole skip as one step: no room can take six hours of a machine's heat at once, so each machine refused the step and did nothing until the skip ended. A skip is now taken in ten-second steps whenever a machine is running, so refineries, reactors, crackers, racks and the rest draw power, warm the room and deliver their products as they do in ordinary play.
- A machine waiting on a full store or an empty feed looks again during a skip. Its five-second recheck followed real time, and a skip is over in a moment of real time, so one wait used to last the rest of the skip.

### Compatibility and limits

- With no Phobos machine running, the skip is the game's own single jump, as before. With crew orders enabled it keeps its one-second steps.
- A long skip with many machines running takes a little longer to work out than it did.
- Automatic. Nothing saved changes.
- Checked offline; not yet seen in the game.

## [0.98.0] - 2026-10-05 - Draft

### Added

- A machine can name one store, touching it or joined by conveyor belt, to send its finished products to. Content mods decide which machines offer it; Phobos Manufacturing 0.49.0 is the first. It is optional, like the feed store it mirrors.

### Save compatibility

- Automatic. Nothing saved changes; the choice is a new record on the machine.

### Compatibility and limits

- The version number skips 0.97.0, which was set aside for another change and not used.
- Checked offline; not yet seen in the game.

## [0.96.0] - 2026-10-05 - Draft

### Fixed

- Choosing Control Panel on a Phobos machine sometimes did nothing: the crew member stayed where they stood and no panel opened. The game counted them as close enough, measuring tile to tile, while Framework measured from their exact position and could find them a tile too far, then refused without a word. Framework now measures as the game does, so a crew member the game accepts is accepted. The same applies to panels in Manufacturing and Medical.
- When a panel does refuse to open, the crew member's log now says why.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- If a crew member still does not walk to a machine, the game's own log line says it cannot get there; that part is the game's pathfinding, not ours.
- Owner checked in play on 5 October 2026 (game 1.0.1.5): Control Panel opened as expected so far; longer play will confirm it.

## [0.95.0] - 2026-10-05 - Draft

### Changed

- Machines that were working when the game was saved carry on after loading, instead of waiting for you to press Start or Resume. Each one runs its own checks first; if a check fails it stays stopped and says why. A machine that was paused or stopped when you saved stays that way.

### Added

- A setting, ResumeAfterLoad under Persistence in the Framework configuration file. Set it to false to make machines wait for Start after loading, as before.

### Save compatibility

- Automatic. A save made before this version holds no record of what was running, so machines wait for Start once more after the first load; from the next save on they carry on.

### Compatibility and limits

- The F6 furnace still waits for Resume after loading. Auto Nav flights and docking follow Auto Nav's own setting.
- Owner checked in play on 5 October 2026 (game 1.0.1.5): machines resumed after a save and load, and the X2 delivered its first hydrogen. Not every machine was checked one by one.

## [0.94.0] - 2026-10-05 - Draft

### Changed

- Machines give off a quarter of the heat they did, for game balance. A room takes four times as long to warm to 40 C, and the mining laser fills a linked radiator four times more slowly. This is an authored gameplay choice, not a property of the machines.

### Added

- A setting, MachineHeatScale under Heat in the Framework configuration file, from 0.05 to 1. The default is 0.25; 1 gives the full heat of earlier versions. Fires and explosions are not affected.

### Save compatibility

- Automatic. Nothing saved changes. A warm room cools as before; machines simply add less heat from now on.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.93.0] - 2026-10-05 - Draft

### Fixed

- Machines on conduit power stood still while showing as powered and working: a refinery stayed at 0 seconds of work, an electrolyser never finished a cycle. Framework measured the electricity a machine received from conduit as nothing, so no work was credited. It now measures what was delivered. This affects every Phobos machine that works by measured electricity, in every mod.

### Save compatibility

- Automatic. Nothing saved changes. A charge that was waiting carries on from where it was.

### Compatibility and limits

- A machine with a stack of feed still takes one charge at a time; the rest of the stack waits in its inventory for the next charge.
- Owner checked in play on 5 October 2026 (game 1.0.1.5): the V4 refinery and X2 chemical processor now work through their charges. Other machines were not checked one by one.

## [0.92.0] - 2026-10-04 - Draft

### Added

- A content mod can let data files add items of their own to its materials pack, each with its own name, description and picture. Phobos Manufacturing 0.45.0 is the first to allow it. An added item in the trash category must be marked terminal, so the reaction mass feeder takes it.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- A save holding an add-on's items needs that add-on enabled; what the game does with them otherwise is untested.
- Checked offline; not yet seen in the game.

## [0.91.0] - 2026-10-04 - Draft

### Added

- Add-ons can carry text: a translation of any Phobos mod into another language, and names for the recipes and other things the add-on adds. Files go in phobos/translations in the add-on, one per language. A player's own translation file still has the last word.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- An add-on's own names exist only while it is enabled.
- Checked offline; not yet seen in the game.

## [0.90.0] - 2026-10-04 - Draft

### Added

- Add-ons players can publish on Steam Workshop (owner direction, 4 October 2026). An add-on is an ordinary mod folder with a phobos-addon.json manifest and the same data files a player keeps in BepInEx/config. Every Phobos data pack reads them: an add-on can retune anything, and add recipes, outcome tables and crops under its own id prefix. It is data only, with no code.
- Files apply in order: the shipped pack, add-ons in the game's mod order, then the player's own files. Any file may set a priority from -100 to 100 to change its place.
- The console command phobosframework addons lists the add-ons in use and any file that was skipped, with the reason. A crew-log notice says when files were skipped.
- A publishing guide, a template and a worked example add-on, and an offline checker for add-on folders.

### Changed

- A recipe added by a data file without a revision gets a stable one from its id, so two add-ons never clash.

### Save compatibility

- Automatic. Nothing saved changes. Removing an add-on leaves a charge bound to one of its recipes waiting until the add-on returns or the charge is cancelled.

### Compatibility and limits

- Names and translations from add-ons, and new items with their own art, are not in this version.
- The game's own UPLOAD button is described from its code; we have not published with it.
- Checked offline; not yet seen in the game.

## [0.89.0] - 2026-10-04 - Draft

### Added

- A Medical care crew role, on for every crew member by default, which can be switched off per person in the Crew panel like the other roles.
- Shared wound care for content mods: one real item goes onto a patient's wound slot through the game's own slotting, so a clean cloth staunches, a splint splints and a dirty dressing taken off stops counting, all by the game's own effects. A spent item comes off onto the deck, never destroyed. Phobos Medical 0.4.0 is the first user.
- A crew job can now name one person who must not take it, so a patient lying in a bed is never sent to treat themselves.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.88.0] - 2026-10-04 - Draft

### Added

- An outcomes data pack for content mods: tables of the recipes a charge may turn out to be, with whole-number odds players can retune or add to. Each outcome is an ordinary recipe with the same charge as its base. A result is picked from the items in the charge, so it is never rolled again. Phobos Manufacturing 0.44.0 uses it for the gangue wash.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.87.0] - 2026-10-04 - Draft

### Added

- A shared list of remainders: the waste items no recipe takes. Each mod declares its own, and retained maintenance waste is declared automatically, so a consumer such as Phobos Manufacturing's reaction mass feeder can take exactly those and nothing useful.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.86.0] - 2026-10-04 - Draft

### Added

- A shared test for whether two pieces of installed equipment touch, for footprints that are not square and are turned on the deck: they touch when they stand side by side or one tile apart without overlapping, the same rule the equipment links use. First used by Phobos Medical's Vigil-2 monitor to find the bed beside it.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.85.0] - 2026-10-04 - Draft

### Added

- Optional feed stores: a machine can name one store, touching it or joined by conveyor belt, and take its feed from there while started and powered. Phobos Shipbreaker 0.73.0 and Phobos Manufacturing 0.42.0 use it.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.84.0] - 2026-10-04 - Draft

### Added

- Weightless care for medical equipment. The game slows every wound's healing, and the easing of its bleeding, to a twentieth when a person is weightless. A mod can now give a patient a stat for the share of normal healing it restores, and the game's own wound code uses the larger of the two. Nobody has the stat unless a mod gives it, so without one nothing changes. If a game update changes that code, the change is skipped with one log line and the rest of Framework starts as normal. First used by Phobos Medical's Ward-3.
- Mods can list the player's loaded crew, from the same roster the game's time skip uses.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline against the installed game's own wound code; not yet seen in the game.

## [0.83.0] - 2026-10-04 - Draft

### Added

- A shared way for machines to take feed from their own inventory, one checked unit at a time, and to give it back on Cancel. Phobos Shipbreaker 0.72.0 and Phobos Manufacturing 0.41.0 use it.

### Fixed

- A panel setting that cannot be saved now says so and tells you to Discard, and the fault is logged, where it used to claim the settings had changed.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.82.0] - 2026-10-04 - Draft

### Added

- Rectangular equipment. A machine family can now be wider than it is deep, or the other way round, with its use point in front and its power point in the wall row behind its back edge. Square machines are built exactly as before. First used by Phobos Medical's three-by-five Ward-3 bed.
- Putting down a carried person at a chosen spot on a piece of equipment, the way the game's own Drop Corpse releases a body, for content mods that place a patient (Phobos Medical's Lay patient here).
- A read-only reading of a person's health as the game holds it: blood lost, infection, pain and each wound's cut, blunt, bleeding, dressing and splint state. Reading never changes the person.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.81.0] - 2026-10-04 - Draft

### Changed

- Link pickers call the water line by its name in the INSTALL menu, the process water line, so it is not mistaken for the Irrigation Conduit.
- When a link is not offered because no line of the right kind touches a machine or store, the picker now also names any other kind of line lying there, such as an Irrigation Conduit run to a W2's water intake, and says it does not count.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.80.0] - 2026-10-04 - Draft

### Added

- An ethanol line lane and port for content mods (Phobos Manufacturing 0.38.0 uses them): the port sits on the left side of a machine, one row below the water port.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- The pipe tile has room for five lanes, all taken, so the ethanol line's art shares the furnace coolant conduit's lane.
- Checked offline; not yet seen in the game.

## [0.79.0] - 2026-10-04 - Draft

### Added

- Tanks show what they hold when you right-click them (owner request, 4 October 2026). Every Phobos tank, silo and store now shows its contents in kilograms on the game's own card, under Wear, the way a Ship's Water tank shows its water. The figure follows filling and draining while the card is open. Contents a damaged vessel trapped in its catch chamber show on a separate Trapped row until recovered.
- VesselContentsDisplay: content mods declare how their stored commodity reads on the card (its name and one of the game's colours). Framework declares water for the Rivetline water tanks.

### Fixed

- Station bulk purchases and sales no longer fail with The destination or its available room changed. A quote was tied to the tank's exact contents, so any tank on a line, or one a running machine draws from, changed between choosing the quantity and pressing Buy, and the purchase was refused every time. A quote now names the tank; room is checked again on delivery, and anything that no longer fits is refunded, as before.
- CommodityReservations.HeldByOther: a purchase reserves its tank before checking it a last time, and that check no longer counts the purchase's own reservation against it.

### Save compatibility

- Automatic. Vessels in older saves show their rows once their ship has loaded. The rows only mirror the saved record; the record, its journals and the vessel's mass are unchanged.

### Compatibility and limits

- An empty vessel shows no row, as an empty Ship's Water tank shows none. A vessel whose records need checking shows nothing on the card; its panel says why. Checked offline; not yet seen in the game.

## [0.78.0] - 2026-10-04 - Draft

### Added

- NativeJobs: equipment can queue the game's own crew jobs, exactly as the PDA paints them: a Haul job on a loose item, and a Mine job on an opened ore deposit. Phobos crew orders work within one ship; the game's own jobs reach a ship moored to yours, so this is how work left on a moored rock or hull gets to the crew. The first user is the ML-2 mining laser.

### Save compatibility

- None. The jobs are the game's own and are saved by the game.

### Compatibility and limits

- Nothing here moves an item or pays ore: duties, tools, haul zones and crew pathing stay the game's. Checked offline against the game's data; not yet seen in the game.

## [0.77.0] - 2026-10-04 - Draft

### Changed

- Machines take power from the wall behind them (owner direction, 3 October 2026). A floor machine's power points now sit in the tile row directly behind its back edge, where ships run their electrical conduit through the walls, as the game's own consoles and chargers do. Before, they sat inside the machine's own back row, so a machine set against a powered wall stayed dark. This covers every machine built on Framework's shared appliance pattern, including Agriculture's Firstlight-4 rack, W2, B2 and Hearth-2.
- ApplianceDefinitions.WallRowY gives content mods the same position; a native check now holds every Phobos machine to it.

### Save compatibility

- Automatic: installed machines take their new power points as the save loads. Manual step, stated plainly: if you ran conduit under a machine's back row rather than in the wall or row behind it, run it one tile further back; a machine with its back against a powered wall needs nothing.

### Compatibility and limits

- The F6 furnace keeps its documented front power points; the G4 and ML-2 hull mounts already took power from the wall row on the hull side, and the C2 from its own wall tiles. Checked offline; not yet seen in the game.

## [0.76.0] - 2026-10-03 - Draft

### Fixed

- A machine standing in a compartment open to space said it was waiting for the room to cool, even at -270 C. Phobos machines do not work in the vacuum of space (owner decision, 3 October 2026), and now say exactly that, with the room's pressure: each sheds its heat into the room's air and needs at least 10 kPa. A warm room still makes a machine wait, and now gives the room's temperature.
- Every machine now uses one shared heat check and one set of wait messages, so the reasons read the same everywhere.

### Save compatibility

- None. Nothing saved changes.

### Compatibility and limits

- Unchanged: the 10 kPa floor, the 40 C ceiling, and the F6-R and F6-P radiators' own cooling. Shedding heat by radiation in vacuum was considered and declined.

## [0.75.0] - 2026-10-03 - Draft

### Changed

- Performance recording exports itself (owner request, 3 October 2026). After phobosframework perf start, recording runs in windows of the chosen length (30 seconds in the usual start summary 30 20000) until phobosframework perf stop. Each window is written to its own file under BepInEx/captures/PhobosScope the moment it ends, and the next begins at once with the same settings, so there is no manual export between captures.
- perf stop ends recording and writes the partial last window. perf status shows how many windows have been written and the latest file. perf export still writes the latest window again.
- Loading a save or starting a new game ends the current window, which is written; recording stays on and carries on once the new world has finished loading. Before, a world change ended recording for good.

### Save compatibility

- None. Recording never touches saves.

### Compatibility and limits

- Quitting the game writes nothing, as before: stop first to keep the window in progress. If a window cannot be written, recording stops with one log line and keeps it for perf export. With 30-second windows expect about 120 small files an hour in summary mode.
- Checked offline (60 recorder checks, including real roll-overs). Not yet run in the game.

## [0.74.0] - 2026-10-03 - Draft

### Changed

- Repairs now end the way the game's own repairs do (owner direction, 3 October 2026). A repair uses up its parts and gives back only the repaired machine; Restore removes wear and leaves nothing. Until now a Phobos repair also handed back the used parts as Spent Service Parts, half a kilogram each, which piled up on long-running ships with nothing to do with them.
- Saves are cleared of the Spent Service Parts older repairs left behind (owner request, same day). They go as each ship loads, on any ship, so a crate of them sold to a station goes too.
- A repair no longer pauses because its parts held cargo or weighed an unusual amount; those checks only existed for the spent parts.

### Save compatibility

- Automatic. Spent Service Parts left by older repairs are removed from each ship as it loads, wherever they lie (deck, containers, machines, pockets), with one crew-log line on your ships saying how many went. Nothing else in the save changes, and repairs already under way finish normally.

### Compatibility and limits

- For mod authors: MaintenanceDefinitions.Repair replaces ReturnRepairMaterials, which stays as an obsolete alias for older builds. LegacyItemConversions.Retire removes a retired item from saves as ships load.
- Checked offline against the game's data: every one of its repair jobs returns only the repaired item. Not yet seen in the game.

## [0.73.0] - 2026-10-03 - Draft

### Fixed

- Pipes now carry what they are for. Since the first irrigation conduit, a pipe segment only counted when the floor object under it carried a mark the game gives to the tile, never to the floor itself. So no segment ever counted as laid on floor, whatever the layout: water, gas and acid lines, irrigation conduits and the F6-C coolant conduit joined nothing, never filled and never delivered. Only equipment standing right against its store ever linked, which is why hydrogen, ammonia and methane worked and water, nitrogen and carbon dioxide never did. A segment now counts over any installed, undamaged floor the game itself treats as floor: grates, the 4 x 4 aero grate and asteroid rock.
- Laid pipe segments are kept out of the ground inventory and out of pockets, like the game's own power conduit. They used to show in the ground inventory, where one could be dragged out of a line, skipping the Uninstall job and the drain-first guard. Loose sections stay pocketable.
- Reloading or quitting no longer reports water tanks and silos as destroyed with their water lost, and no longer warns about pipe contents. Nothing was lost: the save keeps it.
- A ship read as having no pipes is read again within 30 seconds. Since 0.72.0 a ship whose pipes were not ready when it was first read could stay unfilled for good.

### What to expect

- Lines that never worked start working on load. Each run fills from the stores it joins, so store levels drop by the water or gas the pipes hold, and a gas line holds a mix of the gases of every store on it. Irrigation conduits deliver to their racks, and the F6-C circuit primes from the furnace charge.
- Link pickers now offer stores along a line as well as stores touching the machine.

### Save compatibility

- Automatic. Laid segments in a save stop being pocketable and stay out of the ground inventory as they load; their position, wear and mass are unchanged. Pipes in older saves hold nothing yet and fill on the first top-up after loading.

### Compatibility and limits

- Checked offline against the game's own floor data (every object that makes a tile floor is recognised; floor labels, walls and pipes are not). Not yet seen in the game: the owner's checks of the water, nitrogen and carbon dioxide links remain.

## [0.72.0] - 2026-10-01 - Draft

### Changed

- Performance pass, first round (owner request, 1 October 2026). None of this changes what the equipment does.
- A ship's line layout is no longer read afresh every time something is picked up, dropped, eaten, destroyed or a door cycles. Only pipes, the equipment they join, floors and walls drop the remembered layout now; anything else is still caught by the two-second recheck.
- A ship with no lines is no longer scanned every two seconds for lines to top up, and drain canisters are found through the shared world sweep instead of a walk over every object of every owned ship.
- Machine-to-store link checks, which run on every power step, allocate far less, and a route between the same two points is worked out once per layout instead of on every step.
- Several hooks that run for every interaction or sensor in the game do less before deciding the object is not ours.

### Fixed

- Four hooks no longer look up an interaction's name without checking that it has one; a nameless game interaction could have thrown there.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Static review only: no capture was taken for this round. The performance record lists the captures the owner can take to measure it, and the findings left for a measurement or a decision. A new recorder counter, framework.fluid_route.invalidations, counts how often a ship's line layout is actually thrown away.

## [0.71.0] - 2026-10-01 - Draft

### Added

- Products are delivered into stacks. A machine's products now top up the stacks of the same item already in its tray, then form new stacks up to the game's own stack limit, where each product used to take a cell of its own. Items the game does not stack, and items that carry a record of their own that differs, keep their own cells as before.
- When a save loads, items that no longer have a cell in a smaller tray first join stacks of their own kind inside it; only what still finds no place goes to the deck. A tray saved full of single items re-packs into stacks.
- For mod authors: BatchPlacement.PlanStacked, TrayDelivery (plan, fits, place, rollback) and StackUnits.Kind, Room and Detach. New stacks are built and placed the way the game restores a saved stack; a stack that takes more units is taken out, rebuilt with its head still on top and put back at its cell.

### Save compatibility

- Automatic. Nothing saved changes format. Jobs in progress finish with the same products, counts and masses; only where they sit in the tray differs.

### Compatibility and limits

- Stacking follows the game's own stack limits (fifteen scrap, ten ingots, twenty salts to a cell). Offline checks prove the plans and the game's call signatures; how the stacks look and behave in play is for the owner's check.

## [0.70.0] - 2026-10-01 - Draft

### Changed

- Inventories are sized to what the equipment does (owner direction, 1 October 2026). A Rivetline water silo's inventory is now a four-cell service rack at every size, for an irrigation charge, a drain canister or a drained-solution item, where it was a general 8 x 8 grid. The water itself is a record in kilograms and never took a cell.

### Added

- Saved contents follow a smaller inventory by themselves. When a save loads, items stored beyond a smaller grid are moved to free cells inside it; whatever finds no cell is put on the deck beside the equipment as ordinary cargo, and the crew log says so once per ship. Nothing is destroyed or hidden.
- For mod authors: inventory roles (product tray, service rack, storage, feed, legacy receptacle) through InventorySpec and EquipmentInventory, the GridFit planner and ContainerFit, with a keep-first rule for items a saved job names. The native checks require a role on every Phobos container.

### Save compatibility

- Automatic. A silo that held more than four stacks of cargo, or cargo carried over from a converted reservoir, loads with the first stacks in its rack and the rest on the deck beside it. Water, links and reserves are untouched.

### Compatibility and limits

- A locked container, or one whose inventory window is open, is fitted on a later pass. Offline checks are not gameplay validation.

## [0.69.0] - 2026-10-01 - Draft

### Fixed

- Pipes now join the equipment they touch (owner report, 1 October 2026). A process-water, gas or acid line joins a machine, tank or store when it runs under it or right beside it on any side, the way a conveyor belt already does. Before, a line had to end on one particular tile beside the equipment; a line laid under or into a machine looked joined and was not, so link choices stayed empty. Every layout that already worked still works.

### Added

- Link choices say why something is missing. When a silo, tank or store aboard is not offered, the setting's sheet lists it with the first thing to fix: loose, damaged, locked, no working line touching it, a drained line, or two lines that do not meet.
- A machine's Details page names what it is joined to through each line, by pipe or by touching.
- For mod authors: FluidTopology.OnOrBeside for join cells, LineReach.Problem and LinkDiagnosis for one plain reason, LinkChoices.Note and NetworkSummary for panels, a note on EquipmentField and on the ConfigurationSheet forms, and BulkVessels.AboardAnyState. A line port now marks who takes part in a line and where the joint is drawn, not where the pipe must lie.

### Changed

- Because lines join what they touch, an open run now fills from every tank or store on it: about half a kilogram of water a tile, a few grams of gas a tile. Ship's Water drinking tanks beside a water line fill it too, above the crew reserve. The oxygen-and-fuel caution can appear where oxygen and fuel stores now share a gas line.
- One tile under each machine still refuses its own kind of pipe: the edge tile where that line's joint is drawn. Every other tile under it takes pipe.

### Save compatibility

- Automatic. Nothing saved changes; lines and equipment are read as they stand when the save loads.

### Compatibility and limits

- The irrigation conduit and the F6-C coolant conduit keep their own connection points. Reasons shown in a link sheet can be up to two seconds old. Offline checks are not gameplay validation.

## [0.68.0] - 2026-10-01 - Draft

### Added

- Sell bulk back to the station. At a refuelling kiosk, Bulk supplies now lists Sell lines below the supplies: choose an installed store or water silo on your docked ship and a quantity, and the station pays 45% of its own selling price for what actually leaves the store. A reserve kept for the crew is never sold, and neither is anything in a catch chamber. Process water from a Rivetline water silo sells at 4.50 cr/kg.
- Sales use the same protection as purchases: a sale is measured, paid once and recorded in the ledger, and an interrupted sale blocks a retry instead of paying twice.
- For mod authors: a buy-back provider interface and a ready-made one over bulk vessel families; a recipe field that lets a later revision supersede an earlier one for new charges while jobs bound to the old revision still settle by it; and a load-time price refresh that brings saved items to their definition's current price.

### Save compatibility

- Automatic. Nothing in a save changes until you sell. Items a content mod registers for the price refresh take their current price when the save loads; their identity, mass and stacks are untouched, and the save file itself is never edited.

### Compatibility and limits

- The 45% share is our choice, inside the 40 to 50% the game's own kiosks pay. It follows the refining rules the owner approved on 1 October 2026. Offline checks are not gameplay validation.

## [0.67.0] - 2026-10-01 - Draft

### Added

- Legacy item conversions: retired loose parts that cannot simply be renamed are converted on the player's own loaded ships. Complete sets become one whole item and leftovers their exact materials, dropped through the game's own deck placement where the part, or whatever held it, lay. Saved section build sites that can no longer finish are cancelled through the game's own cancellation first; complete ones are left to the crew. Every output is mass-checked before anything moves, a failed placement puts the parts back, and one crew-log notice summarises each sweep.
- Construction stages can follow a whole machine's own Install site, so content can keep its construction artwork when it stops using sections.
- Content can read back a copy of a registered recipe, to derive a related rule from the same published bill.

### Changed

- Section build jobs are retired from new work: INSTALL lists each machine's own whole-machine job, and a section no longer offers Install. The jobs stay registered so saved sites load, finish and cancel unchanged. The previous menu preference for section sites is removed; Shipbreaker 0.60.0 was its only user.

### Compatibility and limits

- Nothing saved changes until a content mod registers a conversion. Offline checks are not gameplay validation.

## [0.66.0] - 2026-10-01 - Draft

### Added

- Shared helpers for content mods, first used by Shipbreaker's ML-2 mining laser: applying damage through the game's own destructible chain, deck geometry for an arc swept from a fixed emitter over the one ship moored to yours, playing a sprite sheet on an installed item with the game's own frame animation, and a beam drawn from an item to a point on the deck.

### Compatibility and limits

- No gameplay or save change by itself. The beam and animation are presentation only and are never saved. Phobos Shipbreaker 0.59.0 needs this version. Offline checks are not gameplay validation.

## [0.65.0] - 2026-10-01 - Draft

### Added

- Ship's Water drinking tanks on a process-water line now fill it too, like the Phobos water silos. They never give below the crew water reserve (Framework's WaterTanks setting CrewWaterReserveKg, 50 kg unless you changed it), counted across every drinking tank aboard. Phobos silos on the same line are drawn from first.

### Changed

- The Rivetline D20 drain canister has new PixelLab artwork, seen from directly above: a steel lid with pressed stiffening ribs and a filler cap.

### Compatibility and limits

- Needs Valtora's Ship's Water 0.16.1 for the tank filling; without it nothing changes. Phobos Agriculture 0.34.0 needs this version. Offline checks are not gameplay validation.

## [0.64.0] - 2026-10-01 - Draft

### Added

- Pumped circuits can hold their contents too: a content mod may make a line without store ports hold what its own pump puts in it, using LineContents.Circuit, Room, Full, Holding and Top. Phobos Shipbreaker 0.58.0's furnace coolant conduit is the first.
- A drain canister put in a machine that registers as a canister receiver pours into that machine, not only into Phobos tanks; the F6 furnace takes coolant this way.

### Compatibility and limits

- Phobos Shipbreaker 0.58.0 needs this version. Nothing changes for lines already holding water, gas or acid. Offline checks are not gameplay validation.

## [0.63.0] - 2026-10-01 - Draft

### Added

- Lines now hold what flows through them. A process-water line keeps about half a kilogram of water on every tile, and a gas line a few grams of whatever gases its stores hold, mixed if several are on it. Each section weighs that much more. An open line tops itself up from the tanks on it every couple of seconds, as soon as a tank has water or gas above its reserve.
- Drain line into canister: right-click an installed water or acid line and a crew member drains the whole run into a drain canister they carry or that lies within two tiles, nearest sections first. A long line takes several canisters.
- Vent gas line: a gas line vents instead, its few grams going into the room air, except hydrogen, which goes overboard.
- Draining or venting closes the run, so its tank no longer refills it and no machine reaches a store through it. Return line to service opens it again, and it refills.
- The Rivetline D20 drain canister: a 20 litre lined can, 3 kg empty, 40 cr, sold with the lines at every general market in lots of 16 and at the faction kiosks at any standing. It holds one liquid at a time and never stacks. Haul a full one with the game's own Haul orders, or with a hauling mod such as Common Sense, into a tank that holds the same liquid, and it pours in, leaving the empty can there.
- For content mods: declare which commodities a line holds with LineContents.Declare, offer the actions with LineContents.OfferActions, and give a store a canister rack with ApplianceDefinitions.SetRack and the drain canister's rack trigger.

### Changed

- A section that holds anything can no longer be uninstalled or dismantled; the refusal says to drain it first. A damaged gas section lets its gas out into the room. A damaged water section keeps its water until drained. A destroyed section's contents are released and logged.
- The process-water and gas line descriptions now say what they hold.

### Save compatibility

- Automatic. Lines already laid start empty and fill from their tanks within seconds after loading. Those tanks drop by the line's hold-up: about half a kilogram of water per tile of water line, a few grams of gas per tile of gas line. Nothing else in the save changes.

### Compatibility and limits

- Phobos Manufacturing 0.25.0 needs this version. Irrigation and furnace coolant lines are not yet on this model; they follow in later releases.
- Ship's Water tanks do not fill lines; only Phobos tanks and stores do.
- Hold-up is authored: a 25 mm bore, a metre a tile, and gas at 10 bar. It is not a flow or pressure simulation.
- Draining, venting and pouring have not been seen in play yet. Offline checks are not gameplay validation.

## [0.62.0] - 2026-10-01 - Draft

### Added

- You can see an item ride the conveyor belt while it is sent: a small copy of its own art moves along the belt from the sending machine to the receiving one, on the belt and under the pipes. It is display only; the item itself stays in the sender until it arrives. Switch it off with the setting Belts, ShowMovingItems.
- For content mods: a belt can join a store on any side of its footprint, and any mod can ask whether one unit of a stack would be accepted, judged at its own mass.

### Compatibility and limits

- Phobos Shipbreaker 0.57.0 needs this version. The moving item has not been seen in play yet; if it misbehaves, the setting turns it off without touching transfers. Offline checks are not gameplay validation.

## [0.61.0] - 2026-10-01 - Draft

### Added

- The Rivetline conveyor belt: a 4 kg segment laid tile by tile from INSTALL, MISC, sold with the lines in lots of 128 at 24 cr and at the faction kiosks at any standing. It lies in the lowest lane under pipe and line. A belt run that lies on or beside two pieces of equipment joins them; items never ride it, they move straight from one to the other while the machine that sends or takes them has power. Phobos Shipbreaker's item routes use it.
- For content mods: the crew hauling orders' way of taking one unit from a stack is shared, so any mod's transfers can move stacked items one unit at a time.

### Compatibility and limits

- Phobos Shipbreaker 0.56.0 needs this version. Offline checks are not gameplay validation.

## [0.60.0] - 2026-10-01 - Draft

### Added

- For content mods: a bulk vessel can hold a spill in its catch chamber without losing it, and a line segment can tell which machines and stores its network joins. Phobos Manufacturing's acid line uses both. No change in play on its own.

### Compatibility and limits

- Phobos Manufacturing 0.24.0 needs this version. Offline checks are not gameplay validation.

## [0.59.0] - 2026-10-01 - Draft

### Added

- Ship's Water tanks join the process-water network. With Ship's Water 0.16.1, every installed drinking-water and waste tank (small, medium and large, intact or damaged) has a water port on the tile beside the middle of its left-hand side, the same rule as every Phobos machine. A silo or machine reaches a tank by touching it (within one tile) or through process-water line laid between their ports. The tanks' own definitions are amended in place; their water, sprites and behaviour are unchanged, and they never join a Phobos silo's water.

### Changed

- Drawing drinking water into a silo and sending water to the waste tanks use only the tanks that silo reaches, never tanks across open floor. The crew reserve still counts every drinking tank aboard, because the crew drink from all of them. A silo's status says how many drinking and waste tanks it reaches, and what to do when there are none.

### Compatibility and limits

- Tanks already aboard gain their port when the save loads; nothing is rewritten. **Manual step:** a silo that drew from, or sent to, Ship's Water tanks elsewhere aboard stops until a tank touches it or process-water line joins their ports. Lay the line or move a tank, then carry on. Ship's Water versions other than 0.16.1 get no ports and no transfers, as before. Offline checks are not gameplay validation.

## [0.58.0] - 2026-10-01 - Draft

### Added

- One ladder of Rivetline process water silos that every Phobos mod shares: the S3, S4 and S5 move here from Shipbreaker with their saved ids, records, names, prices, stock and odds of turning up in salvage unchanged, and a new 2 x 2 S2 holds 400 kg. Each silo now has a general inventory, a process-water port on its left-hand side, its own Control Panel, and can be shared by several machines. Stations sell process water into any of them under Bulk supplies.
- Load-time conversion can now adapt an object's saved conditions to its new definition (mass, price, family marks), keeping wear, locks and progress; Agriculture uses it to turn its retired reservoirs into silos.
- The shared Control Panel shows Crew settings whenever a mod offers crew orders for the equipment.

### Changed

- The crew water reserve setting moved from Shipbreaker's Silo section to Framework's WaterTanks section; the first time it is read, it takes the value you set under Shipbreaker.

### Compatibility and limits

- Shipbreaker 0.54.0 and Agriculture 0.31.0 need this version. Saved silos read unchanged. The S2's sprite is a recorded reduction of the S3's until a dedicated one is drawn. Offline checks are not gameplay validation.

## [0.57.0] - 2026-10-01 - Draft

### Added

- Framework owns its first items: the Fennmark gas line, moved from Manufacturing with its saved identity, name, price, bills and stock unchanged, and a new process-water line (blue, its own lane). Both install from INSTALL, HVAC, sell in lots of 128 at the usual supply merchants and for scrip at the faction kiosks at any standing, and no longer need any one content mod installed. Their art ships in Framework's package.
- One port rule for every machine and store: the water port is the tile beside the middle of the left-hand side, the gas port the tile beside the middle of the right-hand side, both turning with the equipment. Ports come from definitions, so equipment saved before gains them on load.
- A shared machine-to-store link that every mod uses: a store is offered, and a linked store keeps working, only while it touches the machine or shares its line network. Link lists say how each store is reached (touching, water line, gas line) and mark a full destination or an empty source. Store and tank panels list every machine linked to them, from any mod.
- A caution when an oxygen store and a fuel store (hydrogen, methane or ammonia) share one gas line: a line on the stores' panels and one crew-log note per ship. Nothing is blocked. Yards keep them apart; the U.S. Occupational Safety and Health Administration's oxygen-cylinder storage rule, 29 CFR 1910.253(b)(4)(iii), asks for 20 feet or a fire-rated barrier.

### Changed

- The offline data-pack validator accepts an economy pack with supplies and no equipment (Framework's own). The installer retires a file by any of its recorded released hashes, so the gas line art Manufacturing shipped in two versions is backed up and removed cleanly.

### Compatibility and limits

- Phobos Manufacturing 0.23.0, Shipbreaker 0.53.0 and Agriculture 0.30.0 need this version. Saved lines, links and records are kept; a link saved before reads as a store's first slot. The gas line keeps its saved ids, so laid lines stay where they are. Links that crossed open floor under the old one-tile rule still work only while the two touch; otherwise lay the matching line between their ports. Offline checks are not gameplay validation.

## [0.56.0] - 2026-09-30 - Draft

### Added

- Line networks: a line family can name the ports of the machines and stores it serves, and equipment that touches (or stands one tile apart) joins as if piped, with joins chaining across the ship. One shared reach test (touching, or on the same network) serves every mod, so no link ever runs across open floor.
- Stores shared by several machines: each store link now has eight slots. The first slot is the link as it was saved, so every existing link carries over unchanged.
- One shared pattern for line segments. Each kind of line draws in its own lane and depth, so different kinds can share a tile without hiding each other, and the PDA's Conduits filter takes our lines while Equipment painting leaves them alone.
- A shared equipment Control Panel that any mod can use (Manufacturing moves onto it), and load-time conversion of retired equipment to its new equivalent for later rounds.

### Changed

- Line layouts are read once per ship for every kind of line, and only a change on that ship to a line, a machine with a line port, a floor or a wall rereads them; opening a door no longer does.

### Compatibility and limits

- Phobos Manufacturing 0.22.0, Shipbreaker 0.52.0 and Agriculture 0.29.0 need this version. Saves are unchanged: line ids, links and records are kept. Offline checks are not gameplay validation.

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
