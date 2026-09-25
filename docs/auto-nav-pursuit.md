# Polaris pursuit — Auto Nav 0.13.0

Current extension: Auto Nav 0.18.0 supplies [shared local avoidance and explicit
departure](auto-nav-departure.md). Earlier version-specific exclusions of these
features below are historical. Ordinary Fly still never disconnects the ship;
tracking loss suspends automatic guidance.

Prepared 25 September 2026 against Blue Bottle Games' Ostranauts 1.0.1.5.
Implemented and checked offline; owner-run gameplay evaluation is still pending.
The original reported flight has not been reproduced. No installation or save
changes are implied by this document.

## Operating the N2 instrument

**Phobos' Asterel N2 Polaris Pursuit Module** adds pursuit capability to the
[shared flight hub](auto-nav-instruments.md) in the existing Auto Nav package.
N1 is not required. Either module supplies navigation/docking; pursuit requires working N2.
Fire controls require N3. Any combination produces one hub and one service
owns flight. Place the new tall hub through native Edit; old compact placements
are never silently enlarged. Framework 0.17.0 or newer is required.

The Pursuit page exposes routine controls directly. The shared graphite/slate
faceplate uses live accents and Blue Bottle Games' native controls by runtime
reference. Physical module sprites, IDs and recipes retain their earlier contracts.
No extracted native artwork is distributed. UI interaction awaits owner evaluation.

1. Select a qualified native sensor contact. Set separation and cruise speed.
   Separation is centre-to-centre, subject to larger native hull clearance.
2. **Rendezvous** approaches that separation, matches motion and finishes.
   **Follow** on the Pursuit page continues maintaining a band while the target moves.
   These modes always request zero arrival speed, independently of ordinary Fly's
   saved arrival-speed preference. Stop the current flight before selecting another.
3. Optional fire control now requires the separate [N3 Fire Control System](auto-nav-fire-control.md).
   Its Fire page owns target/group selection, limited volleys, Auto Aim and guarded
   Engage. Cease Fire retains Follow and offensive hold; Return to Native releases
   that hold. N2 no longer grants firing permission.
4. **Dock** or **Approach & Dock** remains a separate command after Stop, requiring
   native clearance and compatible assigned ports. Shooting and Follow never dock.

F3 pursuit equivalents are `phobosnav rendezvous [km]`, `phobosnav follow [km]`,
`phobosnav stop` and `phobosnav resume`. Flight settings remain shared. Native
spawning is `spawn PhobosNavModPursuit`; `spawnpursuit` remains a development helper.
Navigation braking and clearance take priority over N3's optional aiming request.

## Acquisition, persistence and limits

The N2 is an authored 0.4 kg, $5,400 module ($1,350 damaged base value). Build it
on the existing electronics-table route from two 0.5 kg small electronics parts:
30 minutes, native Mortorq and soldering tool requirements, with the existing
0.6 kg electronics offcut output. Repair uses two electronics parts; Restore and
dismantling follow the existing native maintenance route. Dismantling retains
the existing 0.4 kg board-residue identity. The Polaris merchant has a 60% offer
chance for the pristine N2. Existing N1 salvage probabilities and saved identities
are unchanged. These are gameplay prices, bills and probabilities, not research.

N2 is registered as `PhobosNavModPursuit` / `PhobosNavModPursuitDmg`, with board
definitions `PhobosPursuitBoard` / `PhobosPursuitBoardDmg` and construction recipe
`PhobosBuildPursuit`. It depends on Framework's existing services; no new public
navigation API or new mandatory mod dependency was introduced.

Flight intent binds the exact console, module instance, player, ship and destination.
Rendezvous, Follow, Dock and Approach & Dock **always suspend on loading**, even when ordinary Fly
auto-resume is enabled. Resume rechecks sensors, hardware and braking room. Predictions,
observation history, weapon aim and fire authority are session-only. N3 owns the weapon-group
preference and retained offensive hold in a separate console-owned Framework store; selecting an offensive
target and granting Engage must be done again after loading. Unknown future preference
or flight records are protected. New pursuit mode names are rejected by older
plugins rather than silently treated as ordinary Fly; do not downgrade or remove
the provider from saves containing the new module.

No controller can guarantee pursuit of a ship with superior available acceleration.
The instrument reports limited control authority while retaining bounded correction
and braking. It does not read crew intentions, target manoeuvre commands or hidden
AI plans. Native signal strength is not presented as a calibrated error percentage.
There is no general traffic/obstacle avoidance. Reaching a ship or shooting at it
does not establish intact life support, surviving crew or permission to dock.

## Guidance implementation

Both ships are sampled at the `StarSystem.Update` boundary before individual ship
updates. The old unconditional one-step target-velocity subtraction is gone.
Successive **relative** velocity observations, compensated for our delivered control,
estimate target acceleration relative to our external acceleration. This cancels
shared orbital motion and common gravity. Prediction residuals shorten a trusted
2–20 second horizon. Missing intervals, invalid data and velocity discontinuities
reseed history; contact loss still suspends for explicit Resume.

The bounded planner evaluates four response times against two target models:
continued observed acceleration and acceleration decaying toward coasting. It
propagates both position and velocity over at most eight intervals per model. The
first interval is the complete actual control step, not an assumed faster replan.
Scores include separation, relative velocity, propulsion and command changes.
Swept hull checks include an acceleration margin. If every candidate threatens
clearance, the controller keeps a bounded braking command and reports the limit.
This is a small deterministic feedback-trajectory search, not a general optimizer
or a mathematical guarantee of collision avoidance.

RCS translation and rotation retain the combined selected-throttle budget. Follow
uses a 3% distance band, bounded between 5 and 250 metres. Torch selection accounts
for heading, angular speed, turning delay, native startup/check interval, available
acceleration and an explicit turn/burn/RCS-brake envelope under both target models.
Only the next control interval is issued. Burn strength follows the chosen response
time; applying the entire correction at once caused reversals in an early test.
Torch loss or native restrictions leave RCS correction available. The legacy
`UseThrusterRotation=false` comparison setting no longer teleports heading: all
guidance turns through physical bounded RCS commands.

Docking tracks relative motion before and after physics. It requires five seconds
of controllable, settled observations before entering terminal capture. Changed
burns, target spin above the terminal limit or inadequate control hold/retreat toward at least 100 m hull gap (or 1.5 hull
contact radii) instead of repeatedly entering clamp range. The controller retains
braking if an active terminal trajectory becomes unsafe. Port assignment, hull fit,
clearance, final speed/alignment and native attachment consequences remain checked;
final capture stays RCS-only. A sufficiently aggressive target can still defeat
holding: the controller cannot manufacture control authority.

These horizons, thresholds, response times, bands and scoring weights are authored
game-control choices. Owner evaluation may justify tuning them.

## Offline evidence and reproduction

Run `scripts/build-autonav.ps1 -OstranautsPath <local-game-directory>`. It builds the
plugin and executes the numerical, torch/pursuit, docking, sensor/lifecycle and
fire-control suites. `tests/PhobosAutoNav.Torch.Tests/PursuitChecks.cs` prints the
benchmark rows, including runtime per guidance step. The test-only frozen legacy
core is never shipped. Its existing attribution and licence exclusions remain.

Benchmark conditions: 5 km initial centre separation, 1 km requested separation,
0.5 m/s² maximum RCS, full throttle, 100 m/s cruise, 0.25 s steps, 10 m hull contact
radius. The crossing target starts with 12 m/s sideways relative velocity. Other
authored target burns range from 0.035 to 0.12 m/s². The same manoeuvre schedules
drive each comparison; targets do not react strategically to the test controller.

| Target | Legacy RCS reversals | Predictive RCS reversals | Predictive torch reversals | Predictive RCS / torch settling, seconds |
| --- | ---: | ---: | ---: | ---: |
| Accelerating | 2 | 1 | 1 | 279.50 / 212.75 |
| Reversing | 2 | 3 | 3 | 304.25 / 231.25 |
| Crossing | 0 | 0 | 0 | 284.75 / 220.50 |
| Burst dodging | 4 | 0 | 0 | 273.50 / 219.25 |

Aggregate RCS reversals fell from 8 to 4; the reversing case individually rose
from 2 to 3. Minimum hull clearance exceeded 989 m in these predictive rendezvous
cases. Maximum separation undershoot was below 0.1 m. A simple acceleration-aware
feedback comparison used fewer calculations but undershot by 3.2–6.03 m; it also
omits attitude manoeuvres, so its effort numbers are not directly interchangeable.
The legacy runs finish near the outer edge of the permitted arrival band, whereas
the new runs finish much closer to the requested separation; settling time is
therefore not the only useful comparison. The earlier screenshot is not this benchmark.

Over a 2,400-second burst-dodging Follow run, mean absolute separation error over
the final 600 seconds was 13.95 m with RCS and 13.62 m with torch. Reversing ship
update order, adding 25 km/s shared background velocity and adding common gravity
preserved those results. These tests exercise actual guidance with narrow native
boundary doubles, not Unity, native crew decision-making or measured fuel/heat.
Integrated translational delta-v is an effort estimate, not measured propellant.
Per-step timings vary with JIT/load; search work is bounded at 64 rollout intervals.

Other checks cover low throttle, ordinary stationary approaches, large control
steps, torch loss/no-wake and contact loss, explicit Resume after reload, separate
offensive target selection, changing clearance, native weapon arcs, empty ammunition,
missile lock, manual-only settings, Cease Fire, exceptional native shot cleanup and
no duplicate native automatic salvo. Owner checks should include evasive crew AI,
the actual N2 layout, hostile boarding conditions and changing reactor delivery.

## Research, game evidence and attribution

- **Zhou, NASA Langley Research Center**, [Trajectory Control of Rendezvous with
  Maneuver Target Spacecraft (2012)](https://ntrs.nasa.gov/citations/20120014309),
  uses relative distance/velocity feedback for a manoeuvring target. This supports
  our choice of relative feedback; its analytical results do not prove this mod's
  stability or performance.
- **Edward Hartley, Paul Trodden, Arthur Richards and Jan Maciejowski**, Cambridge
  and Bristol, [Model Predictive Control System Design and Implementation for
  Spacecraft Rendezvous (2012)](https://eprints.whiterose.ac.uk/id/eprint/90483/1/orcsatpaper_final.pdf),
  developed in **ESA's ORCSAT project**, motivates repeated planning with input
  constraints and separate phases. The paper's target is passive, not deliberately
  evasive. White Rose hosts the author manuscript; it is not the author.
- **ESA**, [ATV Jules Verne mission operations (2008)](https://www.esa.int/Enabling_Support/Operations/ATV_i_Jules_Verne_i),
  describes checked approach stages, holding, retreat and capture. Our terminal
  gate follows that operational principle; cooperative ISS docking is not evidence
  that an unwilling game ship can be captured.
- **Blue Bottle Games**, [Ostranauts](https://bluebottlegames.com/ostranauts), is the
  source of the inspected native simulation and weapon APIs. Local 1.0.1.5 inspection
  covered `StarSystem.Update`, `ShipSitu.TimeAdvance`, `NavModWeaponsControl`,
  `WeaponsSystem` and `ShipInfo`. `Assembly-CSharp.dll` SHA-256:
  `91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
  Game-derived decompilation stays local and is not distributed as source.
- **Gravy / mrkmg**, [Auto Navigate](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691),
  remains credited for the adapted controller shell and target adapter. Preserve
  [third-party notices](../THIRD_PARTY_NOTICES.md) and the upstream-derived MIT
  exclusions. Original predictive/fire policies do not resolve those upstream terms.
  Artwork provenance remains in [the Auto Nav artwork record](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/assets/phobos-autonav/README.md).

No researcher, NASA, ESA, developer or mod author is represented as endorsing or
validating Polaris pursuit.
