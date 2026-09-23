# Fusion-powered industry: ideas and research sequence

Decision record: **2026-09-23**. The owner wants all five ideas retained and
researched as relevant equipment and activities arise during play, so experiments
can be grounded in something they can actually test. This is an opportunity list,
not a commitment to implement every machine or a calendar schedule.

**Immediate research: idea 4, powered shipbreaking. Onboard processing comes
first; external cutting comes later.** The owner also identified a possible
autopilot requirement for external work. See the
[shipbreaking research and test brief](powered-shipbreaking-research.md).
An onboard [first fixture build](shipbreaker-first-build.md) is now prepared:
4 x 4 footprint, four-panel feed, one mass-balanced recipe and player settings.
Build/offline checks passed; in-game use remains to be checked by the owner.

The earlier suggestion to start with a furnace is superseded by this choice.
Approach Assist remains an existing prototype with owner testing outstanding;
selecting industrial research does not claim that its guidance is complete.

## Common design principles

- Support the owner's [long-term habitation ambition](project-direction.md#long-term-habitation-and-endurance).
  Prefer useful inputs to ship maintenance, life support and crew care when
  choosing industrial outputs. Identify which shortage or failure the process
  helps overcome and what supplies still need replenishment.
- Use a controlled fraction of reactor output. A small cutting workshop does not
  realistically require terawatts; sustained throughput can make electrical
  generation useful without an arbitrary reactor-presence requirement.
- Separate electricity, high-temperature process heat and lower-temperature
  waste heat. A warm coolant loop cannot directly run a much hotter furnace.
- Preserve material accounting. Sorting and melting do not automatically purify
  mixed scrap; chemical processing does not create missing elements.
  Each new process must balance workpiece and consumed inputs against products,
  retained residue and any explicitly accounted discharge. Native gameplay yields
  are not reliable physical composition; see the [salvage audit](powered-shipbreaking-design-findings.md#materials-do-not-copy-native-yields-as-physical-composition).
- Give machinery a purpose through throughput, useful outputs and handling costs.
  Balance against repair, intact sale and existing dismantling, including hauling.
- Heat must leave the equipment through a credible path. Start with bounded
  machine cooling if it proves useful; a whole-ship thermal overhaul is not a
  prerequisite. [NASA's thermal-control overview](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/)
  explains spacecraft heat transport and rejection.
- Prefer native systems and installed mods. Steam Workshop dependencies are
  explicitly welcome; do not rebuild crafting, logistics, tank filling or
  manifests merely to keep every feature under the Phobos name.

## 1. Salvage remelting and refining

**Loop:** sort scrap, prepare a batch, melt it, separate appropriate contaminants,
cast usable stock, and use or sell the output. Begin with known, reasonably
homogeneous metal streams. Mixed alloys require different processing routes.

**Energy:** induction/arc heating from electricity; a dedicated high-temperature
reactor heat interface is a later engineering proposal. Crucibles, furnace
linings, batch cooling and contamination can provide maintenance and quality
tradeoffs.

**Research trigger:** the owner accumulates metal scrap, uses Salvage Workshop
recipes and can compare the value of intact salvage against material recovery.
First investigate existing material identifiers, masses, prices and buyers.

**Useful experiment:** one known input and one useful cast product, with measured
energy, yield, heat handling and interruption recovery. Avoid duplicating current
scrap recipes or producing purposeless ingots. Set aside if there is no useful
consumer for the output.

## 2. Oxygen and metals from mineral feedstock

**Loop:** acquire mineral feedstock, heat and electrolyse it, collect oxygen,
handle metal-bearing products and slag, and compress/store the gas.

**Energy:** high-temperature heating and electrical current. NASA demonstrated
oxygen and metal production from molten simulated lunar regolith at about
1,700 degrees C; this supports the process, not the feasibility of our proposed
shipboard installation. [NASA experiment](https://www.nasa.gov/centers-and-facilities/kennedy/nasa-kennedy-breathes-life-into-moon-soil-testing/).

**Research trigger:** mineral cargo/mining becomes relevant and the owner has
oxygen storage and an observable gas-use or sales loop. Purchased feedstock may
support an early experiment without implementing asteroid mining.

**Useful experiment:** a characterised batch with bounded oxygen yield, storage
capacity, electrode wear and residual material. Reuse gas handling where suitable.
Do not promise pure, separately recovered metals from every ore composition.
Defer if feedstock acquisition or output demand is contrived.

## 3. Industrial gas recovery and cryogenics

**Loop:** recover or buy gas-bearing feedstock, separate suitable products,
compress or liquefy them, fill compatible tanks and service customers.

**Energy:** compressors, separation and refrigeration. Liquefaction also requires
heat rejection. Nitrogen, oxygen and other products must come from appropriate
feedstocks; ambient space is not a free gas reservoir.

**Research trigger:** tank management, refuelling, Testudo Safe Pump and Ship's
Water become familiar enough to compare losses and handling effort.

**Useful experiment:** one compatible gas family with explicit input/output
accounting. Confirm the game's cryo consumable identity before proposing to
manufacture it. Reuse Testudo's filling role and Ship's Water's water economy;
the crafting framework already has an optional water adapter. Boil-off and
contamination are candidate additions, not established engine features.

## 4. Powered shipbreaking workshop — research now

**Loop:** recover suitable loose salvage, bring it to a powered fixture, cut it
into useful material or subassemblies, and return those outputs to existing
repair/crafting/sales loops. Compare total labour and recovery with the Weber
laser torch and Salvage Workshop's existing bench.

**Energy:** electrical cutting equipment, actuators and extraction/cooling.
Onboard processing does not require proximity autopilot. An external cutter
working directly on a derelict later requires demonstrated relative positioning,
clearance and immediate cutting interlocks.

**Research trigger:** already selected. Initial observations should use the
existing bench, sorter and hauling mods. The first Phobos machine must address an
observed gap: handling larger workpieces, a meaningful processing choice, or
sustained throughput. A duplicate broken-item dismantling recipe is insufficient.

**Next evidence:** [framework limits, native hooks, proposed cutter and owner-run
checks](powered-shipbreaking-research.md). Neither a cutter nor autopilot expansion
has been implemented by this research round.

## 5. Plasma separation

**Loop:** prepare feedstock, vaporise/ionise it, separate selected ion streams,
collect products and deal with contamination and collector wear.

**Energy:** electrically generated plasma; direct reactor-exhaust coupling would
be a more ambitious design. Plasma mass filtering is a real research topic, but
a compact universal salvage refinery is an extrapolation.
[PPPL mass-filter research](https://www.pppl.gov/m-839).

**Research trigger:** ordinary refining has demonstrated useful material demand
and the owner has experience operating reactor-powered industry.

**Useful experiment:** one narrowly specified separation with imperfect recovery
and expensive equipment. Do not promise arbitrary element transmutation or
perfect sorting. Defer if it adds complexity without a distinct gameplay choice.

## How to resume a research round

When the owner encounters a relevant machine, commodity or difficulty, record
what happened, refresh the [mod inventory](mod-extension-survey.md), and compare
existing solutions. Choose one observable result for a separate test save.
Record versions, normal/accelerated behaviour, power interruption and save/reload.
Expand only after the result shows useful gameplay value. These triggers do not
create a background automation or require the owner to advance every idea.
