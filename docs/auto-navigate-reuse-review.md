# Auto Navigate: reuse and permissions review

Checked **2026-09-23** for Ostranauts **1.0.1.4**. This is a source and policy
review, not a gameplay compatibility result. The owner later downloaded Auto
Navigate and kept it disabled. Its package and binary were inspected without
running it. The agent changed no game files, loading order or saves and contacted
no author.

## Current assessment

**Static game-interface checks passed for Auto Navigate 1.2.0 against 1.0.1.4;
runtime compatibility remains untested. Its reuse terms remain unverified.**
The owner requested a [standalone adaptation](auto-navigate-adaptation.md), rather
than a dependency, and intends eventual public releases. No public rule
was found that makes unlicensed Ostranauts mods automatically permissive.
Distinguish depending on an author's distributed mod from copying and publishing
its implementation. Neither availability nor credit alone establishes a reuse grant.

The owner's permissive working assumption remains a research preference; it is
not evidence of an author-issued licence. Nothing in this review establishes that
Gravy refuses extensions. This uncertainty does not require stopping independent
Phobos work or asking permission for every research step.

## Availability and capability

- Item: [Auto Navigate by Gravy](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691),
  Workshop **3745533691**. The author describes RCS travel, coasting/braking,
  fuel checks, manual takeover and off-console operation. It substantially
  overlaps our proposed first approach-and-brake feature.
- [Change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3745533691)
  identify **1.2.0**, updated **July 3** as displayed by Workshop, including a `ShipSitu.UpdateTime`
  compatibility change, vendor availability and return to normal time on arrival.
- The public Steam metadata API returned success, `visibility=0` (public),
  `banned=0`, no ban reason, and a 560,846-byte package. The apparent removal and
  incompatibility notices in the text fetch also appear on the official guide;
  they are not reliable evidence of this item's removal. The response is retained
  outside Git at `.local/research/autonavigate/steam-metadata.json`.
- The owner subsequently supplied the Workshop package. Its loading-order entry
  explicitly ends in `|disabled`. The assembly was inspected as data, not loaded
  into the game. See the binary review below.

Documented defaults include a 5 m/s tolerance when requesting zero arrival speed,
and automatic resumption after loading. Our close-work requirements would need
separate assessment: reliable work clearance, drift/attitude control, a single
owner of thrust commands, and deliberate rearming after reload. An arrival
controller is not demonstrated continuous work-position control.

The September 15 startup complaint cannot establish current incompatibility:
Blue Bottle Games published a same-day hotfix restoring `DataHandler.GetCondTrigger`
after its signature change broke mods. Whether that fixes this particular report
is an inference to investigate, not a verified diagnosis. See the
[official hotfix announcement](https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1844115010488088).
Arrival behaviour under time compression remains a concrete concern for a later
owner-run evaluation; ordinary upstream power use does not need another proof test.

## What was searched for licensing

Read the description, both change-note entries, all four public comment pages,
the author's profile and other visible Ostranauts Workshop descriptions. No
licence, blanket reuse grant or Auto Navigate source link was found there.
The public `mrkmg` GitHub repository listing returned 94 repositories; it includes
an [Ostranauts loader fork](https://github.com/mrkmg/BepInEx_Loader_Ostranauts),
but no identified Auto Navigate source repository. That loader is a different
project; its terms cannot be assigned to Auto Navigate.

An isolated SteamCMD anonymous download failed. The client found the item and
manifest but logged `No workshop depot defined, skipping non-legacy item` and
returned `Failure`. This does not establish that ordinary account-based Steam
downloads fail. The metadata API supplied no direct file URL. No account login,
subscription or game installation was performed; SteamCMD and its research logs
remain under ignored `.local/` paths.

The subsequent owner-supplied package contains one DLL, metadata, native JSON,
two item images and a preview. No licence file or embedded resource was found;
the assembly metadata supplies authorship/version but no reuse grant. Selected
reconstructed mod code was subsequently adapted at the owner's explicit request,
with its origin and unverified terms preserved in the
[third-party notice](../THIRD_PARTY_NOTICES.md). No upstream DLL or artwork is
bundled. Decompiled game source remains under ignored research paths only.

## Binary review against the installed game

The package and assembly identify **1.2.0**; metadata targets **0.15.1.0**. The
local game log identifies **1.0.1.4**, Unity **6000.3.23f1**, BepInEx **5.4.23.5**.
The exact upstream DLL hash is recorded in the third-party notice.

- All **70 direct Assembly-CSharp member references** resolved against the
  installed game using Cecil metadata inspection. This does not execute the DLL.
- All **10 Harmony target signatures** were found: three world lifecycle
  overloads, data post-load, nav-module loading, loot generation, two ShipSitu
  orbit/body locks, Ship orbit lock and ShipSitu time advancement. Named patch
  arguments and the reflected UI/throttle fields inspected were also present.
- The previously problematic one-argument `DataHandler.GetCondTrigger` exists in
  this installation. `ShipSitu.UpdateTime`'s three-argument call also resolves.
  No missing game member was identified. Resource availability, Harmony execution,
  other mods' patches and actual flight behaviour remain untested.
- No supported public controller API was found. `AutoNavCore` and `TargetRef`
  are internal types. The public panel exposes a toggle-like `TriggerEngage`;
  it is not a stable service contract for setting a flight or querying its state.

Code findings relevant to adaptation:

| Finding | Evidence and implication |
| --- | --- |
| Arrival can precede braking completion | `AutoNavCore.SteerFlight` ends as ARRIVED when distance is inside the ring, without requiring speed to be acceptable in that branch. Applies even to a requested zero arrival speed. |
| Save/load resumption is not implemented in the inspected DLL | Lifecycle prefixes clear the static active-flight state; only three panel settings are saved. No flight-state restoration was found. This contradicts the advertised resume capability. |
| Zero throttle is not respected as zero | The guidance floors it to 1%; unreadable UI throttle falls back to 25%. |
| Equipment ownership is weak after engagement | The core tick does not retain and validate the controlling console/module or recheck the master Enabled setting. Panel disappearance does not itself end flight. |
| Steering exceptions do not safely end flight | The upstream time prefix only writes a verbose diagnostic when steering throws; it does not explicitly clear prior thrust or disengage. |
| Fuel check is an estimate | Its approximate delta-v budget and 5% margin do not prove coverage for rotation, changing targets or damaged hardware. |

These are static findings, not observed player-session incidents. The standalone
prototype changes arrival validation, throttle access, equipment checks, error
handling and controls. It preserves explicit disarming on world changes. It does
not claim obstacle avoidance, work-position holding or verified fuel sufficiency.

## Game rules and observed community practice

The [official Ostranauts modding guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946)
teaches modification of core data, independent mod folders, loading overrides and
Workshop uploads. It does not state a default licence for another modder's original
work or an abandoned-mod exception. The Ostranauts Workshop agreement endpoint
links to the standard Steam Subscriber Agreement; no additional game-specific
reuse grant was shown there. Private Discord rules were not accessible in this
review, so this is a bounded public-source finding, not a claim about every
community conversation.

Actual author-issued examples differ:

| Example | Evidence |
| --- | --- |
| [Sol Storage Solutions](https://steamcommunity.com/app/1022980/discussions/3/3273564019248169064/) | The author expressly allows inclusion and derivatives without individual written requests. |
| Installed Crafting Framework / Salvage Workshop | Their `LICENSE.txt` grants MIT terms for new code/documentation, with express exclusions for game-derived material. See the [inventory](mod-extension-survey.md#licences-actually-found). |
| [Kriil's Ostranauts mods](https://github.com/Kriil/ostranauts/blob/main/LICENSE) | An explicit AGPL-3.0 licence, with different obligations from MIT. |

These show that authors provide reusable work under chosen terms. They do not
establish a community-wide rule that silence means permission. Search results
from an unrelated wiki-like site claiming community defaults were not used as
authority: no corresponding developer statement or named community rule was found.

[Steam's agreement, sections 2 and 6](https://store.steampowered.com/subscriber_agreement/)
distinguishes subscribers' use rights from grants to Valve and allows app-specific
terms. Its grant to Valve is not a blanket right for us to republish another
creator's mod. For public source repositories, [GitHub's licensing guidance](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
also explicitly distinguishes publicly visible code from licensed reuse. Preserve
that distinction when recording this project's findings.

## Practical next step

The earlier candidate was a companion integration requiring the original item.
The owner's later choice is the standalone adaptation, documented above. Original
authorship and terms remain distinct from our own code. Do not bundle the upstream
DLL or images or imply that the original author endorses the adaptation.
[Workshop dependencies](https://partner.steamgames.com/doc/api/ISteamUGC#AddDependency)
are supported, but do not themselves supply a licence or guarantee compatibility.
An original companion also needs its own terms assessed if it depends on patching
private internals; lack of redistribution is not universal legal clearance.

If a substantive source fork becomes the best engineering route, establish the
specific author grant or bundled licence covering that fork before distribution.
A later question to the author could cover source availability, attributed fixes
and a Workshop-dependent extension together. No outreach is implied by this
research request. If no suitable integration emerges, keep our independent
Approach Assist implementation and add only the behaviour needed for endurance
and salvage, documenting why reuse was impractical.
