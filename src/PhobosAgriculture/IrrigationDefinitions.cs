using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
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

        var pipe = AgricultureEconomy.Supply(Pipe);
        // Ordinary native install/repair workflow on Framework's shared segment pattern (Framework 0.56.0), with entirely
        // independent tile sockets and its own draw layer.
        LineDefinitions.Add(d, new LineSegmentSpec
        {
            Prefix = Pipe, Name = Text.Get("water_pipe"), Description = Text.Get("water_pipe_desc"), Art = "phobos/agriculture/WaterPipe",
            Present = Segment, Intact = WorkingSegment, Kg = PipeKg, Price = pipe.price, InstallTab = InstallMenu.Miscellaneous,
            Controls = Definitions.Controls, LooseStack = StackLimits.Pipes, Layer = LineLayers.Irrigation
        });
        foreach (string form in new[] { "Installed", "InstalledDmg" })
        {
            foreach (string prefix in new[] { Supply, Definitions.Rack })
            {
                var fixture = d.Items[prefix + form];
                fixture.aSocketAdds[prefix == Supply ? 1 : 4] = Pipe + "FixturePort";
                fixture.ctSpriteSheet = Pipe + "Sprite"; // Native adjacent-sheet refresh, without making the appliance a sheet.
            }
        }
        // The W2's intake on Framework's process-water line (Agriculture 0.30.0): the neighbouring tile of its local -X
        // side, top row. Added after the irrigation fixture so the irrigation line keeps driving the item's own joints.
        var intake = LinePorts.Water(2);
        LineDefinitions.AddPort(d, Supply, Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(), LinePorts.WaterPoint, intake.X, intake.Y, intake.Socket);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool damaged = form.EndsWith("Dmg");
            var co = d.Objects[Pipe + form];
            if (damaged)
            {
                d.Installables[co.strName + "Repair"].aInputs = EconomyStock.RepairInputs(pipe.repairBill);
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", pipe.repairWork);
            }
            string waste = pipe.remainder ?? Pipe + "Waste";
            if (!d.Objects.ContainsKey(waste)) MaintenanceDefinitions.Remainder(d, waste, Text.Get("housing_waste"), PipeKg);
            MaintenanceDefinitions.Dismantle(d, co.strName, pipe.dismantleWork, new[] { waste });
        }
    }
}
