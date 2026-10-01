using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>One size of the Rivetline process water tank ladder: S2 (2 x 2, new in Framework 0.58.0), S3 (3 x 3, the
/// original silo), S4 and S5. The S3's ratings come from Framework's vessels pack; the others follow the shared size
/// rule (<see cref="BulkVesselSizes"/>), the S2 one step below it. The S3, S4 and S5 keep Shipbreaker's definition
/// ids, record names and record owner, so every saved silo reads unchanged.</summary>
public sealed class WaterTank
{
    public string Model { get; }
    /// <summary>Tiles wider (positive) or narrower (negative) than the S3.</summary>
    public int Step { get; }
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public int Footprint => WaterTanks.BaseFootprint + Step;
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    /// <summary>The translation key of this size's name (its own Rivetline model in the naming map).</summary>
    public string NameKey => "WaterTanks.name_" + Model.ToLowerInvariant();
    public string Art => Prefix;
    private double Area => Footprint * (double)Footprint / (WaterTanks.BaseFootprint * WaterTanks.BaseFootprint);
    public double CapacityKg => Scale(WaterTanks.BaseCapacityKg, Area * (1 + BulkVesselSizes.EfficiencyGainPerStep * Step));
    public double DryKg => Scale(WaterTanks.BaseDryKg, Area * (1 - BulkVesselSizes.DryReductionPerStep * Step));
    public double Price => Scale(WaterTanks.BasePrice, Math.Pow(Area, BulkVesselSizes.PriceAreaExponent));
    public BulkVesselSpec Spec => new(Prefix, LineFamilies.Water, CapacityKg, DryKg, WaterTanks.RecordOwner, Record, Journal, Guard);
    private double Scale(double value, double factor) => Step == 0 ? value : BulkVesselSizes.Round(value * factor);
    internal WaterTank(string model, int step, string suffix)
    {
        Model = model; Step = step;
        Prefix = WaterTanks.BasePrefix + suffix;
        Record = WaterTanks.BaseRecord + suffix; Journal = WaterTanks.BaseJournal + suffix; Guard = WaterTanks.BaseGuard + suffix;
    }
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
}

/// <summary>Framework's Rivetline process water tanks (Framework 0.58.0): one ladder every mod shares, moved from
/// Shipbreaker (the S3 to S5 silos) and joined by a smaller S2. Agriculture's R3 to R5 reservoirs convert to the S3 to
/// S5 on load. Each tank is a passive vessel of process water (a saved kilogram record, never the game's potable
/// water), with a general inventory for manual work, a process-water port on its local -X side, and no electricity.</summary>
public static class WaterTanks
{
    public const string BasePrefix = "PhobosProcessSilo", BaseRecord = "ShipbreakerSilo", BaseJournal = "ShipbreakerSiloWork", BaseGuard = "ShipbreakerSiloTransfer";
    /// <summary>The owner every saved silo record carries (Shipbreaker's plugin id, kept when the tanks moved).</summary>
    public const string RecordOwner = "phobosgekko.ostranauts.shipbreaker";
    public const int BaseFootprint = 3;
    public const string Controls = "PhobosFrameworkTankControls", ImagePath = "phobos/framework/";
    /// <summary>Station price of process water per kilogram, into any tank; bought in steps of ten kilograms.</summary>
    public const double WaterPricePerKg = 10, PurchaseStepKg = 10;
    /// <summary>Drinking water Ship's Water keeps for the crew when a tank draws from its tanks (a setting).</summary>
    public const double DefaultCrewReserveKg = 50;
    public static readonly double[] TransferChoices = { 50, 100, 250, 500 };
    public static double BaseCapacityKg => ItemVessels.Entry(BasePrefix).capacityKg ?? 0;
    public static double BaseDryKg => ItemVessels.Entry(BasePrefix).dryKg;
    public static double BasePrice => ItemEconomy.Price(BasePrefix);
    /// <summary>S2, S3, S4 and S5, smallest first. The S3 to S5 keep Shipbreaker's ladder suffixes.</summary>
    public static readonly IReadOnlyList<WaterTank> All = new[]
    {
        new WaterTank("S2", -1, "Compact"), new WaterTank("S3", 0, ""), new WaterTank("S4", 1, "Medium"), new WaterTank("S5", 2, "Large")
    };
    public static WaterTank? For(string? id) => id == null ? null : All.FirstOrDefault(t => t.IsFamily(id));
    public static bool IsTank(string? id) => For(id) != null;
    public static bool IsTank(CondOwner? co) => co != null && IsTank(co.strCODef);
    /// <summary>Reserve steps for a tank of any size: none, a tenth, a quarter, a half and all of it.</summary>
    public static double[] ReserveChoicesFor(double capacityKg) => new[] { 0, .1, .25, .5, 1 }.Select(f => Math.Round(capacityKg * f)).ToArray();
    /// <summary>A whole number of kilograms within a tank's capacity.</summary>
    public static bool ValidAmount(double kg, double capacityKg) => !double.IsNaN(kg) && !double.IsInfinity(kg) && kg >= 0 && kg <= capacityKg && kg == Math.Floor(kg);

    internal static void Add(NativeDefinitions d)
    {
        foreach (var tank in All) BulkVessels.Register(tank.Spec);
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("WaterTanks.controls"); controls.strDesc = controls.strTooltip = Text.Get("WaterTanks.controls_tooltip");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        var water = SharedLines.ProcessWaterSpec();
        foreach (var tank in All)
        {
            AddTank(d, tank);
            var port = LinePorts.Water(tank.Footprint);
            LineDefinitions.AddPort(d, tank.Prefix, water, LinePorts.WaterPoint, port.X, port.Y, port.Socket);
        }
    }
    private static void AddTank(NativeDefinitions d, WaterTank tank)
    {
        string p = tank.Prefix; int footprint = tank.Footprint;
        MachineFamilies.Add(d, p, InstallMenu.Appliances, Text.Get("WaterTanks.condition"), Text.Get(tank.NameKey), Text.Get(tank.NameKey));
        foreach (string state in MachineFamilies.Forms)
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get(tank.NameKey) + (damaged ? Text.Get("SharedLines.damaged") : "");
            co.strDesc = Text.Get("WaterTanks.description", tank.DryKg, tank.CapacityKg, footprint);
            // A passive vessel: no feed, no electricity, no tickers. Its water is a saved record; the service rack holds
            // loose supplies for manual work (Agriculture's charges, a drain canister). Cargo a converted reservoir or an
            // older, larger grid held beyond the rack goes to the deck when the save loads (ContainerFit).
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>();
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            // Installed forms keep the panel when damaged, as Shipbreaker's silos did: it shows the trapped water.
            co.aInteractions = installed ? new[] { "Inventory", Controls } : new[] { "Inventory" };
            co.inventoryWidth = co.inventoryHeight = footprint;
            MaintenanceDefinitions.SetStat(co, "StatMass", tank.DryKg);
            co.mapPoints = new[] { "use,0," + (-8 * footprint - 8) };
            item.nCols = footprint; item.fZScale = 0.5f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", footprint * footprint).ToArray();
            item.aSocketReqs = MachineFamilies.Border(footprint, installed ? "TILFloor" : "Blank");
            item.aSocketForbids = MachineFamilies.Border(footprint, installed ? "TILObstruction" : "TILItemForbids");
            // One overhead sprite for every form, as the inventory portrait too; damaged forms use the game's damage tint.
            item.strImg = ImagePath + tank.Art; item.strImgNorm = item.strImg + "Normal"; item.strImgDamaged = item.strImg;
            co.strPortraitImg = item.strImg;
        }
        EquipmentInventory.Apply(d, p, Rack);
    }
    /// <summary>A tank's inventory: a four-cell service rack for hand work (an irrigation charge, a drain canister, a
    /// drained-solution item), the same at every size (Framework 0.70.0; it was a general 8 x 8 grid before). The water
    /// itself is a record in kilograms and never takes a cell.</summary>
    public static readonly InventorySpec Rack = InventorySpec.ServiceRack(DrainCanisterDefinitions.RackWidth, DrainCanisterDefinitions.RackHeight);
    public static string Kg(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}

/// <summary>Framework's <c>vessels</c> data pack (<c>framework/vessels.json</c>): the S3 tank's capacity and dry mass.
/// Identities, record names, the water commodity and the size rule stay in code.</summary>
public static class ItemVessels
{
    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.vessels.json";
    private static VesselPack? pack;
    public static VesselPack Pack => pack ??= Load();
    public static DataPackSource Source => new(FrameworkInfo.PluginId, ModFolder, VesselSchema.Name, typeof(ItemVessels).Assembly, Resource);
    public static VesselPack Load()
    {
        pack = DataPacks.Load<VesselPack>(Source, p =>
        {
            VesselSchema.Validate(p, new VesselContext(new[] { WaterTanks.BasePrefix }) { Kinds = new[] { "silo" } });
            if (p.families[WaterTanks.BasePrefix].commodity != LineFamilies.Water) throw new ArgumentException(Text.Get("ItemVessels.commodity", WaterTanks.BasePrefix, LineFamilies.Water));
        });
        return pack;
    }
    public static VesselFamilyEntry Entry(string prefix) => Pack.families.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No vessels entry for " + prefix);
}
