using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Carves the ice supply into the game's own tables at publication (see IceSupplyRules). A switched-off
/// setting carves a zero share, which leaves the game's table exactly as it was.</summary>
internal static class IceSupply
{
    internal static void Add(NativeDefinitions d, bool fields, bool deposits)
    {
        // An unknown cluster name at world generation could break a new game: carve fields only when the game's
        // own ice cluster blueprint is loaded.
        if (DataHandler.dictAsteroidClusterBlueprints?.ContainsKey(IceSupplyRules.IceCluster) == true)
        {
            Carve(d, IceSupplyRules.CFields, IceSupplyRules.CFieldDonor, IceSupplyRules.IceCluster, fields ? IceSupplyRules.FieldShare : 0);
            Carve(d, IceSupplyRules.SFields, IceSupplyRules.SFieldDonor, IceSupplyRules.IceCluster, fields ? IceSupplyRules.FieldShare : 0);
        }
        else if (fields) Plugin.Log?.Invoke(Text.Get("Ice.missing_cluster", IceSupplyRules.IceCluster));
        Carve(d, IceSupplyRules.DepositTable, IceSupplyRules.DepositDonor, IceSupplyRules.WaterIce, deposits ? IceSupplyRules.DepositIceShare : 0);
        // The game's methane ice is worth less than the water inside it; correct its price in place (never
        // republished) so thawing it still loses value. Blocks already in a save keep the price they were made with.
        d.Amend(() => { if (DataHandler.dictCOs != null && DataHandler.dictCOs.TryGetValue(ThawRules.MethaneIce, out var ice)) Content.SetStat(ice, "StatBasePrice", ThawRules.MethaneIcePrice); });
    }

    private static void Carve(NativeDefinitions d, string table, string donor, string choice, double share)
    {
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.ContainsKey(table)) { Plugin.Log?.Invoke(Text.Get("Ice.missing_table", table)); return; }
        AdditiveLoot.CarveChoice(d, table, donor, choice, share);
    }
}
