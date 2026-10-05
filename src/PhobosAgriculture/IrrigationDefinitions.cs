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
    /// <summary>The Firstlight-4 rack's footprint in tiles; its irrigation inlet and water port sit beside the middle of its -X side.</summary>
    internal const int RackFootprint = 4;
    internal const double RateKgPerSecond = .05, EnergyKWhPerKg = .001;
    internal const double PumpKW = RateKgPerSecond * EnergyKWhPerKg * 3600;
    internal static bool IsSupply(CondOwner co) => co.strCODef.StartsWith(Supply, StringComparison.Ordinal);
    /// <summary>The W2's supply rack: packets, charges, a cartridge, a drain canister and recovered solution.</summary>
    /// One cell for each kind it handles (nutrient packets, a bulk charge, a mixture, irrigation charges, a cartridge, a
    /// drain canister) and two for drained or treated solution: eight cells, where it had sixty-four.
    internal static readonly InventorySpec SupplyInventory = InventorySpec.ServiceRack(4, 2, Definitions.Rack + "Supplies");
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
        EquipmentInventory.Apply(d, Supply, SupplyInventory);
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Definitions.Rack, StringComparison.Ordinal)))
            co.mapPoints = co.mapPoints.Concat(new[] { Inlet + "," + LinePorts.Water(RackFootprint).X + "," + LinePorts.Water(RackFootprint).Y }).ToArray();
        // The irrigation pipe's ports (Agriculture 0.53.0): every W2 and rack form takes part in the irrigation network,
        // so any pipe under or beside it joins it. The outlet and inlet points stay, and mark where the joint is drawn.
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            LinePorts.Register(Service.WaterPipesId, Supply + form, Outlet);
            if (d.Objects.ContainsKey(Definitions.Rack + form)) LinePorts.Register(Service.WaterPipesId, Definitions.Rack + form, Inlet);
        }

        var pipe = AgricultureEconomy.Supply(Pipe);
        // Ordinary native install/repair workflow on Framework's shared segment pattern (Framework 0.56.0), with entirely
        // independent tile sockets and its own draw layer.
        LineDefinitions.Add(d, new LineSegmentSpec
        {
            Prefix = Pipe, Name = Text.Get("water_pipe"), Description = Text.Get("water_pipe_desc"), Art = "phobos/agriculture/WaterPipe",
            Present = Segment, Intact = WorkingSegment, Kg = PipeKg, Price = pipe.price, InstallTab = InstallMenu.Miscellaneous,
            Controls = Definitions.Controls, LooseStack = StackLimits.Pipes, Layer = LineLayers.Irrigation
        });
        // The conduit holds its water or feed until drained (Agriculture 0.33.0; owner decision, 1 October 2026): about
        // 0.20 kg a tile, filled by the W2's pump, drained into Framework's drain canister, which pours back into a W2.
        // Pumped: only the W2 fills it (Agriculture 0.53.0 keeps that now the pipe joins as a network).
        Service.IrrigationHolding = LineContents.Declare(Service.WaterPipes, Pipe, Service.ConduitCommodities(), pumped: true);
        LineContents.OfferActions(d, Pipe, gas: false);
        DrainCanisters.RegisterReceiver(new W2CanisterReceiver());
        foreach (string form in new[] { "Installed", "InstalledDmg" })
        {
            foreach (string prefix in new[] { Supply, Definitions.Rack })
            {
                var fixture = d.Items[prefix + form];
                fixture.aSocketAdds[prefix == Supply ? 1 : LinePorts.Water(RackFootprint).Socket] = Pipe + "FixturePort";
                fixture.ctSpriteSheet = Pipe + "Sprite"; // Native adjacent-sheet refresh, without making the appliance a sheet.
            }
        }
        // The W2's intake on Framework's process-water line (Agriculture 0.30.0): the neighbouring tile of its local -X
        // side, top row. Added after the irrigation fixture so the irrigation line keeps driving the item's own joints.
        var intake = LinePorts.Water(2);
        LineDefinitions.AddPort(d, Supply, Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(), LinePorts.WaterPoint, intake.X, intake.Y, intake.Socket);
        // The rack's process-water port (Agriculture 0.32.0), so a rack refilling straight from Ship's Water reaches a
        // tank by the owner's link rule: touching, or on the water line. It shares the irrigation inlet's tile, each
        // family in its own lane, so the footprint tile beside it draws both joints.
        var rackPort = LinePorts.Water(RackFootprint);
        LineDefinitions.AddPort(d, Definitions.Rack, Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(), LinePorts.WaterPoint, rackPort.X, rackPort.Y, rackPort.Socket);
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
