using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAgriculture;

// Authored content balance. Framework retains native work and actual repair waste.
internal static class EquipmentEconomy
{
    internal const double RackPrice = 700, CookerPrice = 150, LettuceSeedPrice = 5;
    private const double RackRepairProgress = 2400, CookerRepairProgress = 1200;
    private const double RackDismantleProgress = 800, CookerDismantleProgress = 400;

    internal static void Apply(NativeDefinitions d)
    {
        foreach (string prefix in new[] { Definitions.Rack, Definitions.Cooker })
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool rack = prefix == Definitions.Rack, broken = form.EndsWith("Dmg");
            string id = prefix + form;
            if (broken)
            {
                d.Installables[id + "Repair"].aInputs = rack
                    ? new[] { "TIsPartsMechSmall=1x2", "TIsPartsElecSmall=1x1", "TIsScrapAluminum=1x2" }
                    : new[] { "TIsPartsMechSmall=1x1", "TIsScrapAluminum=1x1" };
                MaintenanceDefinitions.SetStat(d.Objects[id], "StatRepairProgressMax", rack ? RackRepairProgress : CookerRepairProgress);
            }
            int steel = rack ? (broken ? 4 : 12) : (broken ? 0 : 2);
            int aluminium = rack ? (broken ? 2 : 8) : 1;
            int parts = rack && !broken ? 2 : 0;
            string waste = prefix + (broken ? "Broken" : "") + "HousingWaste";
            double remainder = (rack ? 80 : 12) - steel - aluminium - parts * .5;
            if (!d.Objects.ContainsKey(waste)) MaintenanceDefinitions.Remainder(d, waste, Text.Get("housing_waste"), remainder);
            var products = Enumerable.Repeat("ItmScrapSteel", steel)
                .Concat(Enumerable.Repeat("ItmScrapAluminum", aluminium))
                .Concat(Enumerable.Repeat("ItmPartsMechSmall01", parts)).Concat(new[] { waste }).ToArray();
            MaintenanceDefinitions.Dismantle(d, id, rack ? RackDismantleProgress : CookerDismantleProgress, products);
        }
    }
}
