using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>The Rivetline conveyor belt (Framework 0.61.0) against the game's own data: a Framework line segment in
/// the lowest layer with no ports or contents, sold and installed like the other lines, with a bill that conserves its
/// mass and a segment family that joins only belt segments.</summary>
internal static class BeltNativeChecks
{
    internal static void Run(NativeDefinitions framework, Action<bool, string> check)
    {
        foreach (string form in LineDefinitions.Forms)
        {
            var co = framework.Objects[BeltNetwork.Prefix + form]; var item = framework.Items[co.strItemDef];
            check(co.strNameFriendly.StartsWith("Phobos' Rivetline Conveyor Belt", StringComparison.Ordinal), "The belt carries the Rivetline name: " + form);
            check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass") == SharedLines.BeltKg && item.fZScale == LineLayers.Belt && (!form.StartsWith("Installed", StringComparison.Ordinal) || co.aInteractions.All(a => a == "PhobosFrameworkMaintenanceInformation")),
                "A belt segment weighs 4 kg, lies in the lowest layer and, installed, has no controls beyond maintenance information: " + form);
            check(framework.Installables.TryGetValue(BeltNetwork.Prefix + form + "Dismantle", out var dismantle) && dismantle.aLootCOs.All(l => l.StartsWith("PhobosConveyorBeltWaste", StringComparison.Ordinal)),
                "Dismantling returns only retained belt waste: " + form);
            if (form == "Loose") check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatBasePrice") == 24, "A belt segment costs 24");
        }
        check(EquipmentSaveUpgrade.Amount(framework.Objects["PhobosConveyorBeltWaste"].aStartingConds, "StatMass") == SharedLines.BeltKg, "The retained waste is the whole segment's mass");
        check(framework.Installables[BeltNetwork.Prefix + "LooseInstall"].strBuildType == InstallMenu.Miscellaneous, "The belt installs from INSTALL, MISC");
        check(framework.Installables[BeltNetwork.Prefix + "InstalledDmgRepair"].aInputs.Contains("TIsScrapSteel=1x1"), "Repair takes a steel scrap");
        check(ItemEconomy.SupplyKeys.Contains(BeltNetwork.Prefix) && ItemEconomy.Pack.factionKiosks?.tiers[BeltNetwork.Prefix] == "Neutral", "The belt is a Framework supply at the faction kiosks at any standing");
        check(!BeltNetwork.Family.IsNetwork && !BeltNetwork.Family.AdjacencyJoins && BeltNetwork.Family.Label?.Invoke() == "belt",
            "Belts are plain segments: equipment joins by lying on or beside them, never as a port participant");
        check(!LineFamilies.For("water")!.Id.Equals(BeltNetwork.FamilyId, StringComparison.Ordinal), "No fluid rides the belt");
    }
}
