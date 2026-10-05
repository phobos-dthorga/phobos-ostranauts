# Phobos Spacer Stories changelog

Draft dates record preparation, not Workshop publication.

## [Unreleased]

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
