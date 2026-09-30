using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal static class IrrigationDefinitions
{
    internal const string Supply = "PhobosVerdemorrowGroundworkW2", Pipe = "PhobosVerdemorrowWaterConduit";
    internal const string Segment = "PhobosWaterConduitPresent", WorkingSegment = "PhobosWaterConduitIntact";
    internal const string Outlet = "PhobosWaterOut", Inlet = "PhobosWaterIn";
    internal const double DryKg = 20, CapacityKg = 20, PipeKg = 1;
    internal const double RateKgPerSecond = .05, EnergyKWhPerKg = .001;
    internal const double PumpKW = RateKgPerSecond * EnergyKWhPerKg * 3600;
    internal static bool IsSupply(CondOwner co) => co.strCODef.StartsWith(Supply, StringComparison.Ordinal);
    internal static void Add(NativeDefinitions d)
    {
        ApplianceDefinitions.Add(d, Supply, Text.Get("water_supply"), Text.Get("water_supply_desc"), 2, DryKg, AgricultureEconomy.Price(Supply), "phobos/agriculture/WaterSupply", Definitions.Controls, .02);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = d.Objects[Supply + form];
            co.mapPoints = co.mapPoints.Concat(new[] { Outlet + ",24,8", "PhobosBulkIn,-16,0" }).ToArray();
            co.strContainerCT = Definitions.Rack + "Supplies";
            if (form == "Installed") co.aInteractions = co.aInteractions.Concat(new[] { "load-water", "load-irrigation", "load-nutrients", "recover-solution", "drain" }.Select(Definitions.WorkId)).ToArray();
        }
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Definitions.Rack, StringComparison.Ordinal)))
            co.mapPoints = co.mapPoints.Concat(new[] { Inlet + ",-40,8" }).ToArray();

        foreach (string key in new[] { Segment, WorkingSegment })
            d.Conditions[key] = new JsonCond { strName = key, strNameFriendly = Text.Get("water_pipe"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[Pipe + "Sprite"] = new CondTrigger { strName = Pipe + "Sprite", fChance = 1, fCount = 1, bAND = true, aReqs = new[] { Segment }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        foreach (bool intact in new[] { true, false })
        {
            string key = Pipe + (intact ? "Adds" : "Off");
            d.Loot[key] = new Loot { strName = key, strType = "condition", aCOs = intact ? new[] { Segment + "=1x1", WorkingSegment + "=1x1" } : new[] { Segment + "=1x1" }, aLoots = Array.Empty<string>() };
        }
        d.Loot[Pipe + "FixturePort"] = new Loot { strName = Pipe + "FixturePort", strType = "condition", aCOs = new[] { Segment + "=1x1" }, aLoots = new[] { "TILFixtureAdds=1x1" } };
        foreach (string form in new[] { "Installed", "InstalledDmg" })
        {
            foreach (string prefix in new[] { Supply, Definitions.Rack })
            {
                var fixture = d.Items[prefix + form];
                fixture.aSocketAdds[prefix == Supply ? 1 : 4] = Pipe + "FixturePort";
                fixture.ctSpriteSheet = Pipe + "Sprite"; // Native adjacent-sheet refresh, without making the appliance a sheet.
            }
        }
        // Ordinary native install/repair workflow, with entirely independent tile sockets.
        ApplianceDefinitions.Add(d, Pipe, Text.Get("water_pipe"), Text.Get("water_pipe_desc"), 1, PipeKg, AgricultureEconomy.Supply(Pipe).price, "phobos/agriculture/WaterPipe", Definitions.Controls, 0, InstallMenu.Miscellaneous);
        d.Power.Remove(Pipe + "Power");
        var pipe = AgricultureEconomy.Supply(Pipe);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool installed = form.StartsWith("Installed"), damaged = form.EndsWith("Dmg");
            var co = d.Objects[Pipe + form]; var item = d.Items[co.strItemDef];
            co.nStackLimit = installed ? 1 : StackLimits.Pipes;
            if (damaged)
            {
                d.Installables[co.strName + "Repair"].aInputs = EconomyStock.RepairInputs(pipe.repairBill);
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", pipe.repairWork);
            }
            co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            co.mapPoints = new[] { "use,0,-16" };
            co.aStartingConds = co.aStartingConds.Where(x => !x.StartsWith("IsContainer=") && !x.StartsWith("IsCumbersome=")).Concat(new[] { "IsPocketable=1x1" }).ToArray();
            co.nContainerWidth = co.nContainerHeight = 0; co.mapGUIPropMaps = Array.Empty<string>(); co.strContainerCT = null;
            item.fZScale = 1.01f;
            if (installed)
            {
                item.strImg = "phobos/agriculture/WaterPipeSheet"; item.strImgNorm = item.strImg + "Normal";
                item.bHasSpriteSheet = true; item.ctSpriteSheet = Pipe + "Sprite";
                item.aSocketAdds = new[] { Pipe + (damaged ? "Off" : "Adds") };
                item.aSocketForbids = Enumerable.Range(0, 9).Select(i => i == 4 ? Pipe + "Off" : "Blank").ToArray();
                item.aSocketReqs = Enumerable.Range(0, 9).Select(i => i == 4 ? "TILFloor" : "Blank").ToArray();
            }
            string waste = pipe.remainder ?? Pipe + "Waste";
            if (!d.Objects.ContainsKey(waste)) MaintenanceDefinitions.Remainder(d, waste, Text.Get("housing_waste"), PipeKg);
            MaintenanceDefinitions.Dismantle(d, co.strName, pipe.dismantleWork, new[] { waste });
        }
    }
}
