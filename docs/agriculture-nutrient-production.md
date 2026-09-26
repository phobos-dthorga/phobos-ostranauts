# Nutrient production and retained waste

26 September 2026. Prepared in Agriculture 0.9.0, Framework 0.22.0 and
Shipbreaker 0.20.0. These are implemented candidates, not in-game validation
or Steam publication. Agriculture's crop chain needs Framework only.

## Groundwork B2 Workup Bench

Buy **Phobos' Verdemorrow Groundwork B2 Workup Bench** from the supply kiosk,
Fixer or Halvorson, or build its loose form at a supported table. Install the
2 x 2, 20 kg machine through **APPS** and connect electricity. Its base value
is 250 cr. Its sealed chamber and original artwork represent an authored
aggregate recovery process, not a validated digester, sterilizer or chemical lab.

1. Harvest or clear a crop planted with this version. Put its **Recorded Crop
   Residue** in B2 Inventory. Older cohorts and old residue retain their existing
   uncharacterized identity and cannot enter this process.
2. While paused, choose **Prepare crop-residue recovery**. An awake crew member
   performs one minute of setup. The job binds that exact packet. Choose **Start**.
3. B2 consumes received electricity at up to 0.5 kW. It returns **Recovered
   Nutrient Concentrate** and **Spent Crop Biomass**. Every kilogram stays in
   an output; the biomass cannot be run through recovery again.
4. Leave the concentrate in Inventory and add purchased **Groundwork Makeup
   Salts**. While paused, choose **Prepare nutrient formulation**, then Start
   after its one-minute setup. This consumes equal masses of concentrate and
   makeup salts, returning a **Recovered Crop Nutrient Mixture** and any unused
   makeup packet with its remaining mass and proportional value.
5. Remove the products. Each stage needs output space in addition to its retained
   inputs. A full bin pauses with bound inputs and paid progress intact. Cancel
   while paused to release a job; cancellation forfeits paid work, never duplicates
   ingredients. Reload retains the job but requires explicit Resume.

Each stage requires 0.02 kWh/kg of its primary input, with a 0.001 kWh minimum.
Received energy heats the room through the existing Agriculture power path.
No extra process water is charged: residue is already wet, and retained spent
biomass includes its unrecovered water. This is simplified gameplay chemistry.
The first version processes one recorded packet per stage; batch aggregation
and detailed mineral recipes remain possible later improvements.

## Gradual W2 mixture use

Put a finished B2 mixture or an ordinary 40 g formulated-nutrient packet into
W2's existing Inventory. Pause both operation and receiving, select the desired
charge in the panel, and select a crop formulation under the existing empty,
unpaired-circuit rules. Start blending and distribution normally.

Selection stores the full item identity. W2 never silently selects another
packet after removal or exhaustion. The panel reports grams and percentage
remaining; choose another charge explicitly. No dose occurs without paid blending
work, water and solution headroom. Pausing or reloading stops consumption.
Existing numeric nutrients and manual whole-packet loading remain supported.

Depletion reduces physical mass and base value. Charge records are separate
from machine wear: **Repair and Restore cannot replenish any mixture or makeup
salts**. Ordinary machine repairs still work. Empty charges disappear because
their existing mass contract contains no separate packaging. Unknown, corrupt
or mass-mismatched records remain unusable. Interrupted native commits leave
protected journals rather than replaying the transfer. Save records written by this version contain
new fields; older Agriculture versions cannot safely resume them. Keep the
matching newer mod set when loading those saves.

## Optional Recycler collector attachment

With **Valtora's [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
0.16.1**, open **Residue Collector attachment** on an installed Recycler.
Agriculture 0.11.1 and Shipbreaker 0.24.1 support a normal C2 Residue Collector
on two bare structural floor tiles, including directly in front of the Recycler.
Place its full two-tile pocket flush against a two-tile Recycler edge; turn its
cream service face away and leave both adjacent service tiles walkable. Quarter
turns are supported. No wall is required for this floor arrangement; existing
hull mounts retain their wall/exterior-mouth rules. Connect power separately.
Stand beside both unlocked endpoints, choose the collector and explicitly enable
collection. Mere proximity, a corner contact or a gap does not establish alignment.
Old links and cargo are retained; a misaligned pair waits for repositioning or
explicit Unlink. The panel shows one live status and no unused crop-image square.
Framework 0.24.2 supplies the shared display rule, also used by Shipbreaker's
collector routing and furnace panels. Distinct action notices and errors remain
visible. Other Agriculture panels hide unavailable portraits rather than showing
an empty image slot.

The existing collector inlet is exclusive: unlink an industrial source first.
At settlement the shared adapter restricts tank lists to this exact ship,
reserves collector space, caps processing to that space, and measures actual
waste debit minus potable credit. It preserves the provider's power/filter
processing. Collectors retain up to four packets of at most 13 kg each, within
their existing 52 kg payload budget. Remove full packets manually. There is no
automatic transfer of this cargo into industrial processors.

A linked full, unpowered, damaged, moved, obstructed or unavailable collector
stops the attached Recycler before further wastewater consumption. Links survive
reload; permission does not. **Unlink** restores ordinary Ship's Water operation.
An unlinked Recycler is unaffected. Unsupported provider versions have no new
attachment controls or interception. The original Shipbreaker collector still
works independently when Agriculture is absent.

The product is **Recycler Wet Rejects**, reusing our retained-waste artwork but
keeping a distinct identity. At the provider's default 60% recovery, 10 L of
processed waste nominally leaves 4 kg of wet rejects under our authored 1 kg/L
water convention. It is not 4 kg of fertilizer. At 100% there is no reject yield;
an attached collector still needs operational readiness and reservation space.
Provider exceptions or impossible receipts protect the journal for investigation.

**Wastewater-to-fertilizer remains unimplemented.** Ship's Water records water
volume, not nutrient origin or composition; fixture priming can itself produce
waste. Neither legacy tanks nor fresh volume establishes a nutrient budget.
Wet rejects therefore have no B2 recipe. This prevents fresh-water refill or
recycle loops from creating fertilizer.

## Balance and research boundaries

The recovery revision is captured only when planting a new cohort. Its aggregate
nutrient allocation derives from actual growth progress and consumed feed, then
the fraction of biomass retained as residue. Seeds receive no extra nutrient
credit. B2 recovers 60% of that allocation. Equal-mass makeup salts complete the
authored mixture; the model does not simulate individual N/P/K deficiencies.

| Ideal harvest | Concentrate | Makeup consumed | Finished mixture | Fresh-stock expense avoided, less makeup |
|---|---:|---:|---:|---:|
| Potato | 3.84 g | 3.84 g | 7.68 g | 8.64 cr |
| Food lettuce | 0.50 g | 0.50 g | 1.00 g | 1.125 cr |
| Seed lettuce | 5.90 g | 5.90 g | 11.80 g | 13.275 cr |

These are authored ideal-cycle values before work, electricity, capital,
losses and merchant adjustments. Makeup costs 30 cr per 40 g; finished mixture
uses the same 1,500 cr/kg base value as ordinary nutrients. See the regenerated
[economic evidence](agriculture-economy-evidence.md), including construction,
service, salvage and crop-cost comparisons.

**Jay Garland of Bionetics Corporation**, in **NASA Technical Memorandum
107557 (1992)**, [Characterization of the Water Soluble Component of Inedible
Residue from Candidate CELSS Crops](https://ntrs.nasa.gov/citations/19930008922),
supports distinguishing recovered fractions from complete formulations.
**ESA MELiSSA's [nitrifying compartment](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Compartment_III_The_nitrifying_compartment)**
and **Stephanie Engeli's Eawag report on
[VUNA (2018)](https://www.eawag.ch/en/info/portal/news/news-detail/fertiliser-from-urine-set-to-flourish/)**
support separate waste conversion and treatment, not automatic sewage-to-feed.
The [research record](agriculture-nutrient-recovery.md) states source access limits
and distinguishes authors from hosting institutions. None of these sources
establishes our 60% yield, equal-mass recipe, work rate, energy, price or safety,
and none endorses the mod.

## Owner gameplay checks

Check the B2 INSTALL entry and artwork, both jobs with full and partial inputs,
blocked output space, pause/damage/reload, and retained work. Check a selected
W2 charge with no power, water or headroom; remove it and verify there is no
automatic substitution. Inspect partial mass/value and absent Repair/Restore.
For the optional Recycler, test pairing, full slots/payload, a missing or
unpowered collector, same-ship isolation, reload and explicit Unlink. Compare
actual waste/potable changes to retained wet-reject mass. Offline builds do not
establish Unity hook execution, crew animation, visuals or live economy.
