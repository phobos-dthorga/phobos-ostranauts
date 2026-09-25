# Agriculture nutrient-solution piping

Current extension: [fluid-network operations](fluid-network-operations.md) documents Agriculture 0.6.0 / Framework 0.20.0 fan-out, line contents, treatment and optional Shipbreaker 0.17.0 coolant servicing. Earlier version-specific sections below retain their baseline scope.

25 September 2026. Agriculture **0.5.0**, requiring Framework **0.19.0**,
extends the [W2 water circuit](agriculture-water-conduits.md) with finite mixed
feed. Prepared packages are not installed or gameplay-validated.

## Using the circuit

The existing **Phobos' Verdemorrow Groundwork W2 Water Supply Unit** now mixes
feed as well as pumping water. Its dimensions, construction, stock acquisition,
saved identity and artwork are unchanged. The same irrigation conduits connect
one W2 to one Firstlight-4 rack. Keep one supply unit per connected circuit.

1. Pause operation and receiving at both endpoints; unpair the W2 if necessary.
   Drain any previous solution before selecting a different formulation. Drainage
   remains retained Process Solution waste, with no drinking-water or nutrient recovery.
2. Select **potato nutrient solution**, **lettuce nutrient solution** or
   **water only** in the W2 controls. Nutrient mode requires at least 0.5 kg free
   liquid capacity when selected. Optional provider intake keeps that headroom.
3. Put finite irrigation charges/water rations and Groundwork nutrient packets
   in the W2 inventory, then queue their crew loading actions. For potatoes,
   one 5 kg water charge and one 0.04 kg nutrient packet support one complete
   authored feed batch. Unused water remains available. Lettuce uses less nutrient
   per cohort; one packet can supply eight full feed batches if water is replenished.
4. Install the W2 and conduit route as described in the water guide. Pair the
   paused rack with the W2. Its planted crop must match the selected feed; an
   empty rack may receive solution before planting the matching crop.
5. Enable **receiving at the rack**, then start the W2 pump/mixer. Start cultivation
   separately. Enable the W2's optional provider inlet only if desired. Manual
   water and dry nutrients remain usable at the rack.

The local panel, F3 and optional C1 access call the same checked service. The
formulation actions are `mix-potato`, `mix-lettuce` and `water-only`; existing
pairing, start/pause, receiving and drain actions retain their meanings. Operation
and receiving remain paused after reload and require explicit resumption.

## Finite quantities and limits

| Authored feed profile | Water | Dissolved nutrient | Meaning |
| --- | ---: | ---: | --- |
| Potato v1 | 4.624 kg | 0.040 kg | Existing full potato growth input budget |
| Lettuce v1 | 1.2658 kg | 0.005 kg | Existing full lettuce growth input budget |

These are **gameplay formulations**, derived from existing crop budgets, not
real fertilizer recipes or recommended hydroponic concentrations. Crop yields,
accelerated growth times and simplified chemistry are unchanged. No pH,
electrical conductivity, ionic species, contamination or nutrient recovery is simulated.

Each W2/rack shares **20 kg combined liquid capacity** between plain water and
prepared solution, and **0.5 kg total nutrient capacity** between dry and
dissolved nutrient. Dissolving stock adds to liquid mass; a completely full water
tank cannot mix further. No solution-density-to-litres assumption is made.

Mixing consumes measured water and dry stock. Delivery moves the two components
in their declared proportion, limited by both destination component headrooms
and total liquid headroom. Matching mixed feed is consumed before supplementary
manual stock. Incompatible solution must be drained before changing crops or feed.
Pipe tiles hold no inventory; hold-up, pressure and travel time remain abstracted.

Delivery, mixing and optional provider intake share one received-electricity
budget per interval, in that order: up to 0.05 kg of work per second and
0.001 kWh per kg of work, with a 0.18 kW maximum demand. Thus preparing and
delivering one kilogram requires two kilograms of pump/mixer work; optional
provider intake adds its own work. These are authored energy costs, not a measured
pump curve. All received electrical energy heats native cabin gas. No power means
no new mixing or delivery; route failure retains stock at the equipment.

Valtora's [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
remains an optional, version-scoped **plain-water inlet only** (inspected 0.16.1).
Its source ownership, crew reserve and same-ship safeguards remain intact. Nutrient
solution never enters potable storage. Independent circuits can use different profiles.

## Saves and shared services

Existing Agriculture schema-1 water and dry-nutrient fields retain their exact
meaning. The additive `PhobosState.AgricultureSolution` schema-1 record holds the
profile and both dissolved components. Missing records mean zero prepared solution;
loading old saves neither dissolves dry stock nor creates new matter. Unknown,
corrupt or incompatible records protect the equipment instead of silently converting it.
Contents mass and uninstall/dismantle guards include the prepared solution.

Framework owns reusable `LiquidMixture`, `IMixtureReservoir`, `MixtureTransfer`
and the mixture overload of `LiquidTransferGuard.Commit`. Agriculture owns profile
ratios, mixing, crop compatibility and consumption. Both endpoint journals record
both components before transfer. Partial receipts must preserve composition;
uncertain writes leave protection evidence for reload. These guards do not make
the game's multi-object save operation crash-atomic. Do not downgrade a save with
new solution records to an older Agriculture build.

## Research, artwork and verification

NASA's [PONDS account by Danielle Sempsrott, 4 March 2020](https://www.nasa.gov/missions/station/the-shape-of-watering-plants-in-space/)
supports treating root-zone water delivery as a dedicated engineering concern.
Bruce Dunn and Hardeep Singh at Oklahoma State University explain composition
management through [electrical conductivity and pH, April 2017](https://extension.okstate.edu/fact-sheets/electrical-conductivity-and-ph-guide-for-hydroponics).
Our two-number authored feed does **not** implement those measurements or establish
real crop suitability. Neither institution endorses or validates this mod.

Placement and cardinal connectivity reuse the inspected conventions of
[Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts), documented
with local evidence in [the shared-fluid research](fluid-conduits-and-irrigation-research.md).
They do not imply vanilla has a nutrient-fluid simulation. Existing W2 and pipe
artwork is reused with live localized formulation text; no new generation or
artwork licensing claim is introduced. Provenance remains in the water guide.

Automated checks cover component conservation, bounded/partial delivery, wrong
profiles/ships, interrupted-write protection, save compatibility, mixing headroom
and full crop growth with original yields. Owner gameplay checks should cover
both crop formulations, manual top-ups, a full destination, a broken pipe,
power loss, drain/switch, and save/reload during an active circuit. Builds and
offline checks do not establish Unity behavior or gameplay readiness.
