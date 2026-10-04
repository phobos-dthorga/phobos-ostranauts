using System;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>The native definitions Framework itself owns (Framework 0.57.0): items every Phobos mod shares, so no
/// content mod has to be installed for another to use them. They are published at the start of each content load,
/// before any content mod registers, so ports, conversions and stock in content mods can name them.</summary>
public static class FrameworkItems
{
    public static bool Ready { get; private set; }
    /// <summary>Framework's whole item set, with merchant, regional and faction-kiosk stock.</summary>
    public static NativeDefinitions Prepare()
    {
        var d = new NativeDefinitions();
        // The trigger behind hidden legacy receptacles (Framework 0.70.0), before any content mod names it.
        EquipmentInventory.AddTrigger(d);
        ItemVessels.Load();
        ItemEconomy.Load(NativeMass, id => DataHandler.dictLoot != null && DataHandler.dictLoot.ContainsKey(id));
        // Lines hold their contents (Framework 0.63.0): the crew actions, the drain canister and the two pipes' hold-ups.
        SharedLines.DeclareHoldUps();
        Liquids.LineContents.AddActions(d);
        DrainCanisterDefinitions.Add(d);
        SharedLines.Add(d);
        // Owner direction (3 October 2026): repairs follow the game and leave nothing behind, and the spent parts older
        // repairs left are removed from saves as their ships load (Framework 0.74.0).
        Persistence.LegacyItemConversions.Retire(Registration.MaintenanceDefinitions.SpentParts,
            Text.Get("MaintenanceDefinitions.spent_service_parts_kg"), Text.Get("LegacyItemConversions.spent_parts_reason"));
        // The water tank ladder (Framework 0.58.0): definitions, ports, economy, merchant offers and world finds.
        WaterTanks.Add(d);
        // Contents on the right-click card (Framework 0.79.0): the shared held-back row and the water row.
        Liquids.VesselContentsDisplay.AddTrapped(d);
        Liquids.VesselContentsDisplay.Declare(d, Liquids.LineCommodities.Water, Text.Get("VesselContentsDisplay.water"), "N2Blue");
        TankEconomy.Apply(d);
        var sales = TankEconomy.Sales;
        EconomyStock.ApplyRegional(d, ItemEconomy.Pack, "Framework", sales);
        EconomyStock.ApplyFactionKiosks(d, ItemEconomy.Pack, "Framework", sales);
        MaintenanceInformation.Register(d, "PhobosFrameworkMaintenanceInformation", co =>
            WaterTanks.IsTank(co) ? WaterTankService.MaintenanceReason(co, true) ?? Text.Get("WaterTanks.maintenance_ready") : "");
        ItemHandling.Apply(d);
        return d;
    }
    internal static void Register(Action<string> log)
    {
        Ready = false;
        // A new content load reads the game's mod list afresh, before any pack is loaded (add-ons, Framework 0.90.0).
        Data.AddOns.Reset();
        try { Prepare().Publish(); Ready = true; }
        catch (Exception e) { log(Text.Get("FrameworkItems.failed", e)); }
    }
    /// <summary>A native definition's starting mass, as the game writes it (StatMass=1xN).</summary>
    internal static double? NativeMass(string id)
    {
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(id, out var co)) return null;
        foreach (string cond in co.aStartingConds ?? Array.Empty<string>())
        {
            if (!cond.StartsWith("StatMass=", StringComparison.Ordinal)) continue;
            string amount = cond.Substring(cond.IndexOf('x') + 1);
            return double.TryParse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double kg) ? kg : null;
        }
        return null;
    }
}
