using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Alembrine Corker-2 Bottling Unit (Manufacturing 0.40.0; owner decisions of 4 October 2026: a separate small
/// bottler turns stored ethanol into an own-brand spirit priced within the refining band). A 2 x 2 machine linked to an
/// ethanol cask and a water vessel: each batch draws the ethanol and water for seven 35 g servings of Alembrine spirit at
/// 40% alcohol by volume and places them in its tray in one settlement. The spirit is a clone of the game's own vodka
/// serving with its own identity and price, so the game's own liquor drinking and effects apply.</summary>
public static class BottlerRules
{
    public const string Prefix = "PhobosBottlingUnit", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingBottler";
    public const string EthanolPort = "PhobosManufacturing.BottlerEthanolIn", EthanolVesselPort = "PhobosManufacturing.BottlerEthanolOut",
        WaterPort = "PhobosManufacturing.BottlerWaterIn", WaterVesselPort = "PhobosManufacturing.BottlerWaterOut";
    public const string Spirit = "PhobosAlembrineSpirit", SpiritTrigger = "TIs" + Spirit, TrayTrigger = Prefix + "TTray";
    /// <summary>The game's own vodka serving the spirit is cloned from: 35 g, liquid, liquor, drinkable by the game's own
    /// SeekDrinkLiquor chain (which tests IsLiquor).</summary>
    public const string SpiritDonor = "LiquidVodka";
    /// <summary>Which of the donor's conditions the spirit keeps: all but its brand marker (IsBismertnaya), its mass, price
    /// and category, which come from the materials pack.</summary>
    public static bool KeepFromDonor(string condition) => condition != "IsBismertnaya" && condition != "StatMass" && condition != "StatBasePrice" && !condition.StartsWith("IsCategory", StringComparison.Ordinal);
    public const int Footprint = 2;
    public const double MachineKg = 90, WorkingKW = 0.4, IdleKW = 0.02;
    /// <summary>Servings a batch makes: one full stack of the game's vodka.</summary>
    public const int ServingsPerBatch = 7;
    public const double ServingKg = 0.035;
    /// <summary>A 35 g serving at 40% ethanol by volume: 400 mL of ethanol (789.3 kg/m3) and 600 mL of water (998.2 kg/m3)
    /// weigh 315.7 g and 598.9 g, so ethanol is 34.5% by mass (volume contraction left out): 12.1 g of ethanol and 22.9 g
    /// of water a serving, rounded to the tenth of a gram.</summary>
    public const double EthanolPerServingKg = 0.0121, WaterPerServingKg = 0.0229;
    public static double EthanolPerBatchKg => ServingsPerBatch * EthanolPerServingKg;
    public static double WaterPerBatchKg => ServingsPerBatch * WaterPerServingKg;
    /// <summary>Electricity per batch (authored): pumping, dilution, chilling and filling, half an hour at 0.4 kW.</summary>
    public const double BatchKWh = 0.2;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool Balanced() => Math.Abs(EthanolPerServingKg + WaterPerServingKg - ServingKg) < 1e-12;
    /// <summary>Credits supplied energy to the current batch (the shared one-cycle-per-step rule).</summary>
    public static (double BatchKWh, bool Complete) Advance(double batchKWh, double suppliedKWh) => ManufacturingRules.AdvanceCycle(batchKWh, suppliedKWh, BatchKWh);
}

/// <summary>The bottler's saved record: the current batch's credited energy and the batches made. Running permission is not
/// saved, so a reload waits for Start, as every Phobos batch machine does.</summary>
public sealed class BottlerState
{
    public double BatchKWh;
    public int Batches;
    public Dictionary<string, string> Save()
    {
        if (!ManufacturingRules.Finite(BatchKWh) || BatchKWh < 0 || BatchKWh > BottlerRules.BatchKWh + 1e-9 || Batches < 0) throw new ArgumentException("Invalid bottler record.");
        return new() { ["batch"] = BatchKWh.ToString("R", CultureInfo.InvariantCulture), ["batches"] = Batches.ToString(CultureInfo.InvariantCulture) };
    }
    public static BottlerState Read(IReadOnlyDictionary<string, string> d)
    {
        if (d.Count != 2) throw new ArgumentException("Unknown bottler record.");
        var s = new BottlerState { BatchKWh = double.Parse(d["batch"], CultureInfo.InvariantCulture), Batches = int.Parse(d["batches"], CultureInfo.InvariantCulture) };
        s.Save(); return s;
    }
}
