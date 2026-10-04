using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.63.0 lines that hold their contents against the game's own data: the process-water and gas
/// lines are holding families with realistic hold-ups, their installed forms offer the crew's drain or vent and return
/// actions (belts offer neither), and the drain canister is a sold, non-stacking 3 kg item that only canister racks admit.</summary>
internal static class LineContentsNativeChecks
{
    internal static void Run(NativeDefinitions framework, Action<bool, string> check)
    {
        var water = LineContents.Families.FirstOrDefault(f => f.Family.Id == LineFamilies.ProcessWaterId);
        var gas = LineContents.Families.FirstOrDefault(f => f.Family.Id == LineFamilies.GasId);
        check(water != null && !water.Gas && water.Of("water") is { } w && Math.Abs(w.KgPerTile - 0.49) < 0.005 && Math.Abs(w.CanisterKg - 19.964) < 1e-9,
            "The process-water line holds about half a kilogram of water a tile, and a canister 20 litres");
        check(gas != null && gas.Gas && new[] { "hydrogen", "methane", "oxygen", "nitrogen", "carbon dioxide", "ammonia" }.All(n => gas.Of(n) != null) &&
            gas.Commodities.All(c => c.KgPerTile < 0.01),
            "The gas line holds any of the six line gases, a few grams a tile each");
        check(gas?.Of("hydrogen")?.RoomSpecies == null && gas?.Of("oxygen")?.RoomSpecies == "O2", "Vented hydrogen goes overboard; the others into the room as the game's own species");
        foreach (var (prefix, action) in new[] { (LineFamilies.ProcessWaterPrefix, LineContents.DrainAction), (LineFamilies.GasPrefix, LineContents.VentAction) })
            foreach (string form in LineDefinitions.Forms)
            {
                var interactions = framework.Objects[prefix + form].aInteractions;
                bool installed = form.StartsWith("Installed", StringComparison.Ordinal);
                check(installed == (interactions.Contains(action) && interactions.Contains(LineContents.ReopenAction)), "Installed line forms offer their drain or vent and Return to service: " + prefix + form);
            }
        check(!framework.Objects["PhobosConveyorBeltInstalled"].aInteractions.Any(LineContents.Actions.Contains), "Belts hold nothing and offer no line actions");
        foreach (string action in LineContents.Actions)
        {
            check(framework.Interactions.TryGetValue(action, out var ia) && ia.strActionGroup == "Work" && ia.fDuration > 0 && ia.fTargetPointRange == 2 && ia.strRaiseUI == null,
                "A line action is crew work at the line, taking time: " + action);
        }

        // The saved mark of running work (Framework 0.95.0) is a real, hidden condition, so the game saves it with the machine.
        check(framework.Conditions.TryGetValue(Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Condition, out var resume) && resume.nDisplaySelf == 2 && resume.nDisplayOther == 2,
            "Framework registers the hidden mark that lets a machine carry on after a reload");
        var co = framework.Objects[DrainCanisterRules.Id]; var item = framework.Items[co.strItemDef];
        check(co.strNameFriendly == "Phobos' Rivetline D20 Drain Canister", "The canister carries the Rivetline D20 name");
        check(co.nStackLimit == 1 && co.aStartingConds.Contains(DrainCanisterDefinitions.Marker + "=1x1") && co.aStartingConds.Contains("IsPocketable=1x1") &&
            EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass") == DrainCanisterRules.DryKg && EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatBasePrice") == DrainCanisterDefinitions.Price,
            "A canister is a 3 kg, 40 cr pocketable item that never stacks, since each records what it holds");
        check(item.strImg == DrainCanisterDefinitions.Art && co.aInteractions.SequenceEqual(new[] { "DropItem", "PickupItem" }), "The canister uses its own art and the game's pick-up and drop");
        var rack = framework.Triggers[DrainCanisterDefinitions.RackTrigger];
        check(rack.aReqs.SequenceEqual(new[] { DrainCanisterDefinitions.Marker }) && rack.aForbids.Contains("IsInstalled"), "A canister rack admits only drain canisters");
        check(ItemEconomy.Pack.regional?.items.TryGetValue(DrainCanisterRules.Id, out var regional) == true && regional!.lot == "canisters" && regional.expanded &&
            ItemEconomy.Pack.lots["canisters"] == 16 && ItemEconomy.Pack.factionKiosks?.tiers[DrainCanisterRules.Id] == "Neutral",
            "Canisters sell at every general market in lots of 16 and at the faction kiosks at any standing");
    }
}
