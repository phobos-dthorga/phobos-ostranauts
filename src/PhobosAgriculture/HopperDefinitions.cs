using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAgriculture;

/// <summary>One size of the Groundwork nutrient hopper: E2 (2 x 2), E3 (3 x 3) or E4 (4 x 4), scaled from the E2's
/// vessels-pack entry by Framework's shared size ladder. A damaged hopper isolates its contents in a catch chamber
/// (the ladder's default policy); nothing leaks.</summary>
internal sealed class HopperSize
{
    internal VesselSize Size { get; }
    internal string Prefix { get; }
    internal int Footprint { get; }
    private BulkVesselSpec? spec; private Phobos.Ostranauts.Framework.Data.VesselPack? specFrom;
    internal BulkVesselSpec Spec
    {
        get
        {
            var pack = AgricultureVessels.Pack;
            if (spec == null || !ReferenceEquals(specFrom, pack))
            {
                spec = BulkVesselSizes.Spec(HopperRules.Prefix, HopperRules.SmallFootprint, Size, HopperRules.Commodity, HopperDefinitions.CapacityKg, HopperDefinitions.DryKg,
                    Plugin.Id, HopperRules.Record, HopperRules.Journal, HopperRules.Guard);
                specFrom = pack;
            }
            return spec;
        }
    }
    internal double Price => BulkVesselSizes.Scale(AgricultureEconomy.Price(HopperRules.Prefix), BulkVesselSizes.PriceFactor(HopperRules.SmallFootprint, Size));
    internal double CapacityKg => Spec.CapacityKg;
    internal double DryKg => Spec.DryKg;
    internal string NameKey => "hopper" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    internal string Image => "phobos/agriculture/Hopper" + (Size == VesselSize.Small ? "E2" : Size == VesselSize.Medium ? "E3" : "E4");
    internal HopperSize(VesselSize size)
    {
        Size = size; Prefix = BulkVesselSizes.Prefix(HopperRules.Prefix, size); Footprint = BulkVesselSizes.Footprint(HopperRules.SmallFootprint, size);
    }
}

internal static class HopperDefinitions
{
    internal static double CapacityKg => AgricultureVessels.Entry(HopperRules.Prefix).capacityKg ?? 0;
    internal static double DryKg => AgricultureVessels.Entry(HopperRules.Prefix).dryKg;
    /// <summary>The E2, E3 and E4, smallest first.</summary>
    internal static readonly IReadOnlyList<HopperSize> Sizes = BulkVesselSizes.All.Select(s => new HopperSize(s)).ToArray();
    internal static bool IsHopper(CondOwner? co) => co != null && BulkVesselSizes.InLadder(co.strCODef, HopperRules.Prefix);
    internal static HopperSize? SizeOf(string? id) => Sizes.FirstOrDefault(s => EquipmentIdentity.IsFamily(id, s.Prefix));
    internal const string RecoverWork = "hopper-recover", BagWork = "hopper-bag";
    internal static readonly string[] Work = { RecoverWork, BagWork };
    internal static string WorkId(string action) => BulkDefinitions.WorkId(action);
    internal static void Add(NativeDefinitions d)
    {
        foreach (var size in Sizes) BulkVessels.Register(size.Spec);
        foreach (string action in Work)
        {
            var work = NativeDefinitions.Clone(d.Interactions[BulkDefinitions.Controls]); work.strName = WorkId(action); work.strTitle = work.strTooltip = Text.Get(action);
            work.fDuration = 60 / 3600d; work.strAnim = "Tablet"; work.strActionGroup = "Work"; d.Interactions[work.strName] = work;
            Phobos.Ostranauts.Framework.Crew.CrewSpecialities.RegisterPractical(work.strName, "Agriculture");
        }
        foreach (var size in Sizes)
        {
            string p = size.Prefix;
            ApplianceDefinitions.Add(d, p, Text.Get(size.NameKey), Text.Get("hopper_details", size.CapacityKg, size.Footprint, size.DryKg), size.Footprint, size.DryKg, size.Price, size.Image,
                BulkDefinitions.Controls, 0);
            // A hopper's contents are a record in kilograms; its rack only takes the charges it bags by hand.
            EquipmentInventory.Apply(d, p, BulkDefinitions.Rack);
            d.Power.Remove(p + "Power"); d.Interactions.Remove(p + "PowerChange");
            foreach (var form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
            {
                var co = d.Objects[p + form]; co.jsonPI = null; co.aTickers = Array.Empty<string>();
                co.mapPoints = new[] { "use,0," + (-8 * size.Footprint - 8), "PhobosHopperOut," + 8 * size.Footprint + ",0" };
                if (form == "Installed") co.aInteractions = co.aInteractions.Concat(Work.Select(WorkId)).ToArray();
            }
        }
    }
}
