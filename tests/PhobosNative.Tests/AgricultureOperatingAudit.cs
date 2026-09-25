using System;
using System.Collections.Generic;
using System.Globalization;
using PhobosAgriculture.Core;

// Offline sensitivity analysis. This neither adds recipes nor sets runtime balance.
internal static class AgricultureOperatingAudit
{
    internal static void Append(List<string> rows, Func<string,double> price)
    {
        string N(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
        double waterPrice = price(PhobosAgriculture.Definitions.Irrigation) / PhobosAgriculture.Definitions.IrrigationKg;
        double nutrientPrice = price(PhobosAgriculture.Definitions.Nutrient) / .04;
        double Inputs(Crop crop) => crop.Water * waterPrice + crop.Nutrient * nutrientPrice;
        HarvestBudget Harvest(Crop crop)
        {
            var state = new CropState { Water = 20, Nutrients = .5 };
            state.Plant(crop, 1);
            for (int n = 0; n < crop.Hours; n++) state.Step(1, crop.KW, 10, 10, true);
            return state.Harvest();
        }
        rows.AddRange(new[] { "", "## Cultivation cost sensitivity and propagation", "",
            "Computed from current crop rules and native base values. Groundwork irrigation, consumed portions only; retained potato seed is reused. Each lettuce sowing below buys its seed. These are ideal healthy cycles, not live profits.",
            "The 0, 0.25 and 1 cr/kWh columns are hypothetical marginal electricity costs, not game tariffs; zero does not mean the plant uses no energy. Add cooking, pumping, standby, cooling, crew, equipment, treatments, losses and trade adjustments separately. No climate-control or labour rate is invented.",
            "", "| Cycle | Water + nutrients + purchased seed | Cultivation kWh | At 0 cr/kWh | At 0.25 cr/kWh | At 1 cr/kWh |",
            "|---|---:|---:|---:|---:|---:|" });
        foreach (var crop in new[] { Crop.Potato, Crop.Lettuce, Crop.LettuceSeed })
        {
            double inputs = Inputs(crop) + (crop == Crop.Potato ? 0 : price(PhobosAgriculture.Definitions.LettuceSeed));
            double energy = crop.Hours * crop.KW;
            rows.Add($"| {crop.Id} | {N(inputs)} | {N(energy)} | {N(inputs)} | {N(inputs + energy * .25)} | {N(inputs + energy)} |");
        }
        int availableSeeds = Harvest(Crop.LettuceSeed).Portions - 1;
        if (availableSeeds <= 0) throw new InvalidOperationException("Seed rotation cannot replace its own planting stock.");
        double rotationInputs = Inputs(Crop.LettuceSeed) + availableSeeds * Inputs(Crop.Lettuce);
        double rotationEnergy = Crop.LettuceSeed.Hours * Crop.LettuceSeed.KW + availableSeeds * Crop.Lettuce.Hours * Crop.Lettuce.KW;
        double rotationHours = Crop.LettuceSeed.Hours + availableSeeds * Crop.Lettuce.Hours;
        rows.Add($"\nA repeating lettuce rotation reserves one returned seed packet, grows {availableSeeds} food cohorts and yields {availableSeeds * Harvest(Crop.Lettuce).Portions} edible servings over {N(rotationHours)} rack-growth hours. It consumes {N(rotationInputs)} cr of water/nutrients and {N(rotationEnergy)} kWh. Per food cohort: {N(rotationInputs / availableSeeds)} cr and {N(rotationEnergy / availableSeeds)} kWh, before other costs. Initial stock is a one-off investment; no retained packet is simultaneously counted as sold or bought each rotation.\n");
        rows.AddRange(new[] { "## Proposed crop-residue recovery ceiling — not a recipe", "",
            "Illustrative conservative allocation: distribute only nutrients consumed by growth in proportion to final biomass; allocate the residue share, then recover 60% of that share. The 60% is an authored sensitivity assumption, not NASA's leaching yield. Seed nutrients receive no extra credit. Recovered material is an incomplete concentrate until formulation is defined.",
            "", "| Ideal crop | Wet residue kg | Allocated nutrient ceiling g | Candidate recovered equivalent g | Maximum avoided stock cost cr |",
            "|---|---:|---:|---:|---:|" });
        foreach (var crop in new[] { Crop.Potato, Crop.Lettuce, Crop.LettuceSeed })
        {
            double residue = Harvest(crop).ResidueKg;
            double allocated = crop.Nutrient * residue / crop.Final, recovered = allocated * .6;
            if (recovered < 0 || recovered > allocated || allocated > crop.Nutrient || recovered > residue)
                throw new InvalidOperationException("Recovery proposal exceeds retained material or consumed nutrients.");
            rows.Add($"| {crop.Id} | {N(residue)} | {N(allocated * 1000)} | {N(recovered * 1000)} | {N(recovered * nutrientPrice)} |");
        }
        rows.Add("\nAvoided stock cost is an upper bound, not sale value: subtract processing, make-up nutrients, consumables, labour and capital. Terminal rejects cannot be rerun. Larger batches can amortize setup work; no waste-processing recipe is registered by this audit. See [nutrient recovery direction](agriculture-nutrient-recovery.md).\n");
    }
}
