using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class FurnaceConduitDefinitions
{
    internal const string Segment = "PhobosFurnaceCoolantSegment";
    internal const int LooseStackLimit = 10;
    internal const string Art = "phobos/shipbreaker/FurnaceCoolantPipe";
    internal static void Add(NativeDefinitions d)
    {
        string p = FurnaceCooling.Conduit;
        var supply = ShipbreakerEconomy.Pack.supplies[p];
        // Framework's shared segment pattern (Framework 0.56.0): presence alone, its own draw layer, its fixture name kept.
        LineDefinitions.Add(d, new LineSegmentSpec
        {
            Prefix = p, Name = Text.Get("Furnace.coolant_pipe"), Description = Text.Get("Furnace.coolant_pipe_desc", FurnaceCooling.RouteLimit), Art = Art,
            Present = Segment, Intact = null, Kg = 1, Price = supply.price, InstallTab = InstallMenu.Hvac, Controls = "Inventory",
            LooseStack = LooseStackLimit, Layer = LineLayers.Coolant, FixtureLoot = p + "Fixture"
        });
        string waste = supply.remainder ?? p + "Waste";
        MaintenanceDefinitions.Remainder(d, waste, Text.Get("Furnace.coolant_waste"), 1);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = d.Objects[p + form];
            MaintenanceDefinitions.SetStat(co, "StatInstallProgressMax", 50);
            MaintenanceDefinitions.SetStat(co, "StatUninstallProgressMax", 50);
            MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", supply.repairWork);
            // Small sealed joints are serviced mechanically. Replacement metal remains
            // accounted by the Framework repair helper registered by ApplianceDefinitions.
            if (form.EndsWith("Dmg"))
                d.Installables[co.strName + "Repair"].aInputs = EquipmentEconomy.RepairInputs(supply);
            else EquipmentEconomy.SetRestoreRate(d, co.strName, p + "RestoreProgress", 1);
            MaintenanceDefinitions.Dismantle(d, co.strName, supply.dismantleWork, new[] { waste });
        }
        // Native sheet sockets show the furnace connector independently of electricity (the fixture loot is published
        // by the shared pattern above).
        foreach (string form in new[] { "Installed", "InstalledDmg" })
        {
            var item = d.Items[FurnaceRules.Prefix + form];
            // Row at local y=+0.5, left/right boundary of the 6x6 body.
            item.aSocketAdds[12] = item.aSocketAdds[17] = p + "Fixture";
            item.ctSpriteSheet = p + "Sprite";
        }
    }
}
