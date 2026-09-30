using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>Framework's own economy data pack (Framework 0.57.0): prices, bills and stock for the items Framework owns,
/// read from <c>framework/economy.json</c> through the shared loader with player overrides in
/// <c>BepInEx/config/PhobosFramework/economy</c>. <see cref="FrameworkItems.Prepare"/> reloads it with the native
/// lookups so a new game load sees edited files; offline checks read the shipped copy on first use.</summary>
public static class ItemEconomy
{
    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.economy.json";
    private static EconomyPack? pack;
    public static EconomyPack Pack => pack ??= Load();
    public static DataPackSource Source => new(FrameworkInfo.PluginId, ModFolder, EconomySchema.Name, typeof(ItemEconomy).Assembly, Resource);
    public static IReadOnlyList<string> EquipmentKeys { get; } = Array.Empty<string>();
    public static IReadOnlyList<string> SupplyKeys { get; } = new[] { LineFamilies.GasPrefix, LineFamilies.ProcessWaterPrefix };
    public static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(EquipmentKeys, SupplyKeys) { MassOf = _ => null, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p => EconomySchema.Validate(p, context));
        return pack;
    }
    public static double SupplyPrice(string prefix) => Pack.supplies.TryGetValue(prefix, out var s) ? s.price : throw new InvalidOperationException("No economy entry for " + prefix);
    public static int Quantity(string item) => EconomyStock.Quantity(Pack, item);
}
