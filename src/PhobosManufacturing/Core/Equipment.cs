using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosManufacturing.Core;

/// <summary>Manufacturing's <c>equipment</c> data pack (<c>framework/equipment.json</c>, Manufacturing 0.17.0): the shape
/// of each charge machine (footprint, dry mass, idle and working power, room-heat share, feed cells, art, INSTALL tab,
/// use and power points). Identities, ports and behaviour stay in code. The pack is a read-only reference for now:
/// a player file that changes a shipped machine is refused, because saved machines carry its mass and footprint.</summary>
public static class Equipment
{
    public const string Schema = EquipmentSchema.Name, Resource = "PhobosManufacturing.equipment.json";
    public const string ChargeMachineKind = "charge-machine";
    public static readonly IReadOnlyList<string> Kinds = new[] { ChargeMachineKind };
    public static readonly IReadOnlyList<string> InstallTabs = new[] { "APPS", "FURN", "MISC", "POWR", "HVAC", "CTRL", "SENS", "HULL" };
    /// <summary>Every machine the code builds from the pack.</summary>
    public static IReadOnlyList<string> Known => ChargeCatalog.MachinePrefixes.Values.ToArray();
    private static EquipmentPack? pack;
    public static EquipmentPack Pack => pack ??= Load();
    public static DataPackSource Source => new(ManufacturingRules.Owner, Economy.ModFolder, Schema, typeof(Equipment).Assembly, Resource);
    public static EquipmentPack Load()
    {
        var known = Known;
        // The shipped entries, read on their own, are the baseline every player file must leave unchanged.
        var baseline = DataPacks.LoadText<EquipmentPack>(DataPacks.ShippedText(Source), "", ManufacturingRules.Owner, Schema,
            p => EquipmentSchema.Validate(p, new EquipmentContext(known) { Kinds = Kinds, InstallTabs = InstallTabs }));
        pack = DataPacks.Load<EquipmentPack>(Source, p => EquipmentSchema.Validate(p, new EquipmentContext(known) { Kinds = Kinds, InstallTabs = InstallTabs, Baseline = baseline }));
        return pack;
    }
    public static EquipmentEntry Entry(string prefix) => Pack.equipment.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No equipment entry for " + prefix);
    /// <summary>The working power of the machine a recipe runs on, by its catalog key.</summary>
    public static double WorkingKW(string machine) => Entry(ChargeCatalog.PrefixOf(machine)).workingKW;
}
