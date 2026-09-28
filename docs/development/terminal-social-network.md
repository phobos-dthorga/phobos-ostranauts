# Terminal social network

Concept recorded 2026-09-20. The owner proposes social uses for the installed
computer used to study Software Engineering: discovering contacts, developing
friendships and rivalries, and finding quests. This is the current exploration,
not an implemented feature or a selected release scope.

## Intended experience

Give the computer a place in the character's social life. Online acquaintances
should connect to people and consequences in the physical game world, with a
shared identity and history across both settings.

Possible activities include:

- Local discussion boards: ask for advice, exchange stories and meet people.
- Private correspondence: keep in touch, ask favours and settle disagreements.
- Specialist communities: engineering, medicine, scavenging and other interests.
- Recreation: games and hobby groups that create reasons to return to a contact.
- Opportunities: introductions, requests for help, disputes and eventual quests.

Not every exchange needs to become a job. Companionship, useful information and
changes in how someone treats the player can be worthwhile outcomes themselves.
Relationships should depend on choices and shared experiences; repeated clicks
should not produce unlimited friendship or rewards.

Medical and technical communities could connect to the earlier equipment ideas,
but the social experience should be useful without requiring those future mods.
The installed terminal is the proposed starting point. PDA notifications or
specialist cartridges are optional later extensions, not prerequisites.

## Initial evidence and limits

Read-only inspection of the previously verified 1.0.1.4 installation found:

- `condowners/condowners.json`: `ItmTerminal01` has installed, powered, terminal
  and Software Engineering study flags. Its empty direct interaction list does
  not mean it has no interactions; generic condition tests also select targets.
- `condtrigs/condtrigs.json`: computer/terminal accessibility tests distinguish
  off, locked and installed states. Their existence does not establish which
  checks a new interaction would continuously enforce.
- `GUIPDA.GetKnownSocialContacts` and `Social.GetRelationship`: existing contact
  and relationship handling offers a foundation to investigate. The paths for
  introducing a contact and safely changing a persistent relationship have not
  yet been traced for this proposal.
- Native social interaction, encounter and plot definitions exist. Their
  presence is not proof that a terminal can create a persistent custom quest.

The [official starting guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3347080066)
also describes contacts and the PDA's Socials page. None of this establishes an
existing terminal internet interface or proves our integration will work.
A description of a system-wide comms terminal found under `GUIChargenHomeworld`
belongs to character creation; it is not evidence of a live terminal chat feature.

No gameplay test, game-file change or social-mod compatibility audit was made.
No claim of novelty or absence of competing mods has been established.

## Small first proof

Explore one local discussion with one named correspondent: read a post at the
terminal, choose a reply, gain a persistent contact, and see that exchange affect
a later encounter. An optional small favour could follow once that works.

Use authored dialogue and a few meaningful responses. A simulated population of
internet users is not required to prove the experience. Prefer a suitable existing
NPC for the first identity/persistence investigation before adding NPC generation.

The next research should resolve contact introduction, relationship changes,
terminal entry points and save ownership. Check how other mods use those same
systems. A later test-save experiment must cover interruption, power loss,
fast-forward, save/reload, repeat replies and an unavailable or dead correspondent.
Only add a quest after establishing how its state and rewards persist without
duplication. UI should delegate any gameplay changes to the appropriate service.

## Open design choices

Local boards, direct correspondence and multiplayer-game acquaintances are
possible entry points; none has been chosen. Prompt local replies and delayed
long-distance messages could support the space setting, but communication
availability and delays remain design proposals, not verified lore or mechanics.

The broader opportunity looks worth investigating. The main unknowns are safe
integration with persistent people and the amount of writing needed to make
repeated use interesting. Reassess after the first proof; do not build a general
social framework or promise a whole internet before demonstrating that loop.
