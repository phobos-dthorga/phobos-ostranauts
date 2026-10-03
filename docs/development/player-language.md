# Writing for the crew

Owner direction, 27 September 2026: this rule applies retrospectively to current
player text and to future work in every Phobos Ostranauts mod. It covers item and
recipe descriptions, controls, warnings, settings, notifications, guides and
Workshop pages. The companion [audit](english-language-audit.md) records coverage.

## Audience and evidence

Blue Bottle Games describes Ostranauts as a ship-owning, scavenging, survival and
role-playing simulation, where fuel, air, food and debt matter. Its
[official Steam description](https://store.steampowered.com/app/1022980/Ostranauts/)
also stresses functional ship parts and learning to fly with manuals. This is
evidence about the game's intended experience, not its players' qualifications.

In the [Blue Bottle Games developer AMA](https://www.reddit.com/r/Games/comments/1vfi8v8/we_are_blue_bottle_games_the_team_behind/),
Daniel Fedor names Alien, Blade Runner, tabletop role-playing games, Cowboy Bebop
and Firefly among the influences, and describes an interest in blue-collar lives
in those worlds. His captain-and-ragtag-crew focus supports practical language
about making a living and keeping a ship going. It does not authorize copying
another work's dialogue or inventing demographic statistics.

Our editorial interpretation: write for interested survival, simulation and
role-playing players without assuming engineering or programming knowledge.
No verified age, gender or education profile was established by these sources.

## Voice and order

Use an original working-spacer voice: practical, worn-in and occasionally dry.
A meal can sound like a welcome break; a machine can sound like something bought
to earn its keep. Controls and warnings must say exactly what happens.

Put the situation, consequence and available action first. Prefer “Output tray
full. Clear space and resume” to an explanation of an output transaction. Do not
invent a retry, repair or recovery option merely to make a message friendlier.
Avoid forced slang, invented dialect, gratuitous profanity and jokes in faults.

Keep useful words such as pressure, coolant, docking and nutrients. Explain an
unfamiliar term when it first matters. Put detailed limits, scientific sources
and simplifications in the relevant help or guide. Preserve numerical operating
limits in warnings where the player needs them. Research credit remains beside
the claim it supports, with authors distinguished from hosting institutions.

Preserve `Phobos'`, the established brand and model, and a recognizable equipment
type. A shorter nickname in a panel does not replace a full equipment name.
Do not add machine model numbers to ordinary food or materials.

## Shared glossary

| Term | Player meaning and usage |
| --- | --- |
| Start | Begin the selected operation; linking equipment alone does not start it. |
| Resume | Continue saved or paused work after checking conditions. |
| Pause | Stop work while keeping supplies and progress where supported. |
| Cancel | Abandon the selected work; state exactly what progress is lost. |
| Stop / Coast, Disengage | Release flight thrust. The ship keeps moving; this is not a brake. |
| Link / Unlink | Connect or disconnect selected machines; use the actual button label. |
| Sender / receiver | Machine cargo leaves / machine cargo enters. Avoid “endpoint” in routine prose. |
| Feed | Material loaded for processing. |
| Charge | A measured furnace load or nutrient/coolant supply; name the substance where ambiguous. |
| Product tray | Where finished material is collected. |
| Residue | Material left after a process; specify whether it has another supported use. |
| Rejects | Material with no further supported recovery recipe. Collection does not dispose of it. |
| Crop / crop batch | The plants sharing one rack's cycle; four trays are not four independent crops. |
| Nutrient solution | Water mixed with nutrients for roots; not drinking water. |
| Makeup salts | Supplement added to recovered nutrients; not a complete fertilizer on its own. |
| RCS | Reaction-control thrusters, used for small manoeuvres and braking. |
| Relative speed | Total motion relative to the target, including sideways drift. |
| Closing speed | Motion towards the target; negative when moving apart. |
| Hold fire | Block automatic offensive fire; explain how Return to ship controls releases that hold. |
| Repair / Restore | Replace failed parts / treat wear. Keep these distinct game actions. |
| Saved-state fault | Saved contents or settings cannot be checked safely; retain precise details in logs. |

“Native”, “receipt”, “reconciliation”, “commit”, “binding”, “authored” and
“authority” remain appropriate in developer documentation and diagnostics.
They normally do not explain a player's next action. “Game setting”, “record”,
“confirmation”, “release”, “selection”, “gameplay choice” and “control” are often
clearer, but do not apply a blind substitution where the meaning differs.

## Keeping the voice plain

The owner's 4 October 2026 follow-up calls for whole-catalogue review, not just
new entries or a list of banned words. Read controls, descriptions and messages
as a crew member would encounter them. A correct sentence can still be too dense.

- Say what is waiting: a batch, a wall, a tank or a crew member. Avoid “bound
  charge”, “captured identity” and “endpoint” in routine controls.
- Say what stays and what is lost: “supplies and progress are kept”, or “Cancel
  keeps the ice but loses the electricity used”. Do not promise a refund.
- Explain unfamiliar workshop words at first use: a bund is the outer tank that
  catches a leak; gangue is leftover rock. Keep material and equipment names so
  players can find the right item.
- Use electricity used for kWh and power for kW. Keep pressure, temperature,
  quantities and operating limits precise even when the surrounding prose is plain.
- Give equipment descriptions a practical opening, followed by loading,
  connections and hazards. Break long instructions into short paragraphs.
- Leave a little character in descriptions. Leave it out of failure messages.
  A fault needs a clear consequence and a supported next action, not a joke.

Examples: “Available RCS acceleration” rather than “RCS authority”; “Empty into
stores” rather than “Decant”; “Recover trapped acid” rather than “Recover acid
from the bund”. A changed label must also change in current operating guides.
Keep older release entries as historical evidence and record the new label in
Unreleased. File-validation diagnostics and developer console reports may need
precise technical terms; that exception does not extend to routine crew panels.

## Contracts and review

Keep translation keys, IDs, commands, configuration keys, placeholders and their
format specifications, native grammar tokens, units and gameplay values intact.
Do not use display text to drive behaviour. Keep precise developer diagnostics;
if a message genuinely serves both audiences, separate its presentation only
after tracing its callers. Do not weaken an error to avoid explaining it.

Review every English entry, including unchanged text, and record its role,
decision and source references in the audit ledger. Review recipe fallbacks,
assembled names and hard-coded player text too. Update current guides, maintained
item-reference inputs and Workshop drafts together. Preserve historical releases,
original research and technical evidence as such.

Run the existing localization, placeholder, grammar-token, branding, fallback and
native-definition checks. Regenerate references and release notes through their
maintained tools. Inspect substituted examples of long names, narrow buttons and
multiline warnings. Offline previews do not establish Unity layout approval.
