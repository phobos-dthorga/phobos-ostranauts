# Phobos Spacer Stories changelog

Draft dates record preparation, not Workshop publication.

## [Unreleased]

### Documentation

- Recorded the owner's first live small-talk screenshot and a focused handoff for Claude: the story sentence appears, but Framework's dialogue lead-in prints its speaker, listener and verb tokens literally. No formatting fix or broader gameplay validation is claimed.

## [0.3.0] - 2026-10-06 - Draft

### Added

- Expanded life around vanilla Ostranauts with 164 new story entries: 37 news items, 13 adverts, 59 small-talk lines, 16 loading tips, twelve encyclopedia articles, thirteen chains and fourteen readable files. Twelve new correspondents and twelve threads ground the writing at the game's existing stations.
- Stories of freight estimates, salvage workers, berth neighbours, Ceres housing, lunar work left unfinished, maintenance visas, Deimos night shifts, Venusian losses, family letters around the Atlantis blockade, Titan launches and dockside wages. Most additions need only Framework; a few optional lines connect them with Phobos equipment.
- Three skill-based paths shorten written comparisons. A local battery-label comparison keeps the carried battery and checks neither its charge nor history.
- A freight comparison pays 300 credits once. Reading its card opens an optional letter to Titan's shipyards that pays 600 credits once, waits for the captain's own route and requires no return trip. None of the new chains has an expiry, deposit, penalty or repeatable payment.
- Thirteen outcome reports wait for saved story flags; private talk waits for the relevant correspondence or file. News introduces every new chain before it can start at its home station.

### Compatibility and limits

- Existing story files, entry ids and step ids are unchanged. New progress, flags, files and payment bookkeeping use Framework's existing story record and the game's data-card and credit systems. Requires Framework 0.114.0; no native gigs, plots, factions, NPCs or station rules are changed.
- All thirteen files pass the story checker. The actual add-on loader resolves the collection in 128 optional-mod combinations, with old and unknown story record fields retained. In-game pacing, cards and older saves remain for owner review.
- Held draft. The normal build's native suite stops at its old nine-file inventory check; Claude's code handoff is recorded in the expansion document. No package, installation or Workshop publication is claimed.
- Setting evidence and the thematic influences of Ursula K. Le Guin, C. J. Cherryh and William Gibson are recorded in the development document. All new incidents are original community fiction, not official canon or adaptations of those writers' stories.

## [0.2.0] - 2026-10-06 - Draft

### Changed

- Every story now belongs somewhere. The collection is arranged in ten threads, each with a home among the game's stations: the second-shift ledger and Asterel service letters at OKLG, the galley and drawing letters at the Flotilla, the mixed-plant trade at Port Mojave on Ceres, Alembrine's letters at Corsair's Hollow, Halewright's at the Central Medical Facility on Deimos, Slingwright's at the Titan Shipyards, and the beam-tool trade at Weaver's Needle. News of the place you are at is picked about four times as often, and its Region News label comes from the place.
- Letters come from named people with a home and a role: Neri Vale, receiving clerk; Orra Pell, equipment broker; and new correspondents for Verdemorrow, Asterel, Alembrine, Halewright and Slingwright, with the Sable Margin's cook and installer named in the galley account.
- Nothing tells a story before it happens. Talk and news about a correspondence chain wait until the player has reached that point in it; loading tips and encyclopedia articles were reworded as background. Each standalone chain is introduced by its own news item, and finishes at its home station rather than at any station.
- Crew talk is split by who says it: lines about this ship are said by your crew, local lines by people at the place, and the rest by anyone.
- Nine new news items report how chains ended, so every chain now has outcome news.
- The misfiled-can chain no longer needs a drain can aboard to start; it asks for one to be carried at the receiving desk.

### Compatibility and limits

- Requires Phobos Framework 0.114.0. Every entry id and chain step id is unchanged, so saves carry over: a chain under way keeps its step. Checked offline with the story checker and the game-data checks; how it reads and paces in the game is still to be reviewed.

## [0.1.2] - 2026-10-06 - Draft

### Changed

- Added a matching Phobos cover for the mod menu and Steam Workshop: two spacers sharing a story at a mess table, with correspondence, a terminal and an archive data card.
- The cover is original promotional artwork. Story content is unchanged; the mod remains a held draft awaiting review in the game.

## [0.1.1] - 2026-10-06 - Draft

### Fixed

- The game no longer logs Mod folder not found for this mod: the folder now carries the data folder Ostranauts looks for in every mod.

### Compatibility and limits

- Moved from the repository's examples to its own mod folder, with its own build, installer and Workshop records. The story files are unchanged.

## [0.1.0] - 2026-10-06 - Draft

### Added

- Original company histories for Rivetline, Ablatine, Asterel, Verdemorrow Agronomics, Fennmark, Tolvane, Lixivar, Oxsmith, Alembrine, Slingwright and Halewright, with crew-life accounts of growing food, processing material, fitting a sickbay and remembering a ship's refits.
- TV news and adverts, small talk and loading lore connect those histories with local freight disputes, working crews and the equipment they use.
- Nine short correspondence chains. Neri Vale's second-shift ledger links a misfiled Rivetline shipment, workers' repair letters, an Asterel service record and a Verdemorrow meal. Separate letters cover a mixed process plant, Alembrine's missing-pump story, Halewright's sickbay watch, rebuilding drawings and Slingwright's loading work.
- Each completed chain gives one data card carrying a readable archive document. Opening the first freight file makes the three related follow-up letters available.
- Outcome news appears only after the relevant correspondence finishes. Each chain runs once; its goals can be dismissed through the game's GOALS list.

### Dependencies

- Requires Phobos Framework 0.110.0 or newer. Individual entries require their relevant content mods; none of those mods is mandatory for the add-on.

### Save compatibility

- Uses Framework's existing story record and ordinary story goals. No new item definitions, conditions, native plots or schematics. Stable story and step names are retained in saves.
- These chains take no items or money and grant no money or process goods. Completion gives only a data card containing a story file. They do not start machines, certify production, change reputation or perform medical treatment.
- Cards and their files use the game's existing card system and Framework's shared story-file identity. Removing the pack leaves its files on their cards, readable as corrupted until the pack is restored.
- Removing the files uses Framework's existing story-removal behaviour. Re-adding them picks up the retained story records. Dismissing a goal sets its chain aside in that save.

### Compatibility and limits

- Prepared as a data-only add-on using the established story schema. No plugin or schema changes.
- Company histories and incidents are original Phobos fiction, designed around inspected vanilla lore and existing equipment. They are not official game canon.
- Offline verification is recorded in the authoring document. In-game appearance, pacing and goal behaviour remain for the owner to check. Not installed or published.
