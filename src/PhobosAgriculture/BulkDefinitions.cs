using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;
/// <summary>One size of the Groundwork reservoir: R3 (3 x 3), R4 (4 x 4) or R5 (5 x 5), scaled from the R3 by Framework's
/// shared size ladder. The R3 keeps its original identity and the record names every saved R3 carries.</summary>
internal sealed class TankSize
{
    internal VesselSize Size { get; }
    internal string Prefix { get; }
    internal int Footprint { get; }
    private BulkVesselSpec? spec; private Phobos.Ostranauts.Framework.Data.VesselPack? specFrom;
    /// <summary>The Framework vessel spec, built from the vessels data pack (Agriculture 0.23.0) and rebuilt if that pack is reloaded.</summary>
    internal BulkVesselSpec Spec
    {
        get
        {
            var pack = AgricultureVessels.Pack;
            if (spec == null || !ReferenceEquals(specFrom, pack))
            {
                spec = BulkVesselSizes.Spec(BulkDefinitions.Tank, 3, Size, BulkDefinitions.Commodity, BulkDefinitions.CapacityKg, BulkDefinitions.DryKg, Plugin.Id, "AgricultureBulk", "AgricultureBulkWork", "AgricultureBulkTransfer");
                specFrom = pack;
            }
            return spec;
        }
    }
    internal double Price => BulkVesselSizes.Scale(AgricultureEconomy.Price(BulkDefinitions.Tank), BulkVesselSizes.PriceFactor(3, Size));
    internal double CapacityKg => Spec.CapacityKg;
    internal double DryKg => Spec.DryKg;
    internal string NameKey => "bulk_tank" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    internal string Image => "phobos/agriculture/Reservoir" + (Size == VesselSize.Small ? "" : Size.ToString());
    internal TankSize(VesselSize size)
    {
        Size = size; Prefix = BulkVesselSizes.Prefix(BulkDefinitions.Tank, size); Footprint = BulkVesselSizes.Footprint(3, size);
    }
}
internal static class BulkDefinitions
{
    internal const string Tank="PhobosVerdemorrowGroundworkR3",Nutrients="PhobosVerdemorrowGroundworkBulkNutrients",Controls="PhobosAgricultureBulkControls";
    /// <summary>The commodity id the Shipbreaker silo uses too, so guarded transfers between them stay compatible.</summary>
    internal const string Commodity="water";
    /// <summary>The R3's ratings, from the vessels data pack (framework/vessels.json); larger sizes scale through the ladder.</summary>
    internal static double CapacityKg=>AgricultureVessels.Entry(Tank).capacityKg??0;
    internal static double DryKg=>AgricultureVessels.Entry(Tank).dryKg;
    internal const double NutrientKg=.5,WaterPricePerKg=10;
    /// <summary>The R3 as a Framework bulk vessel (Agriculture 0.18.0). Record, journal and guard names are the
    /// ones every saved R3 already carries, so an existing save reads unchanged; a Shipbreaker T2 may deliver into it.</summary>
    /// <summary>The R3, R4 and R5, smallest first.</summary>
    internal static readonly IReadOnlyList<TankSize> Sizes=BulkVesselSizes.All.Select(s=>new TankSize(s)).ToArray();
    internal static BulkVesselSpec Spec=>Sizes[0].Spec;
    /// <summary>Any size of reservoir.</summary>
    internal static bool IsTank(CondOwner? co)=>co!=null && BulkVesselSizes.InLadder(co.strCODef,Tank);
    internal static TankSize? SizeOf(string? id)=>Sizes.FirstOrDefault(s=>EquipmentIdentity.IsFamily(id,s.Prefix));
    /// <summary>The capacity of any registered water vessel (a reservoir of any size, a Shipbreaker silo).</summary>
    internal static double CapacityOf(CondOwner co)=>BulkVessels.Of(co)?.CapacityKg??CapacityKg;
    /// <summary>Reserve steps for a reservoir of any size, in the R3's proportions (0 to 120 kg for the R3).</summary>
    internal static double[] ReserveChoices(double capacityKg)=>new[]{0,1/24d,1/12d,1/6d,1/3d,2/3d,1}.Select(f=>Math.Round(capacityKg*f)).ToArray();
    internal static readonly string[] Work={"bulk-load","bulk-recover","bulk-drain"};
    internal static string WorkId(string action)=>"PhobosAgriculture_"+action;
    internal static void Add(NativeDefinitions d)
    {
        foreach(var size in Sizes)BulkVessels.Register(size.Spec);
        var controls=NativeDefinitions.Clone(d.Interactions[Definitions.Controls]);controls.strName=Controls;d.Interactions[Controls]=controls;
        foreach(var action in Work)
        {
            var work=NativeDefinitions.Clone(controls);work.strName=WorkId(action);work.strTitle=work.strTooltip=Text.Get(action);work.fDuration=(action=="bulk-load"?10:60)/3600d;work.strAnim="Tablet";work.strActionGroup="Work";d.Interactions[work.strName]=work;
            Phobos.Ostranauts.Framework.Crew.CrewSpecialities.RegisterPractical(work.strName,"Agriculture");
        }
        foreach(var size in Sizes)
        {
            string p=size.Prefix;int step=size.Footprint-3;bool small=size.Size==VesselSize.Small;
            ApplianceDefinitions.Add(d,p,Text.Get(size.NameKey),Text.Get("bulk_tank_details",size.CapacityKg,size.Footprint,size.DryKg),size.Footprint,size.DryKg,size.Price,size.Image,Controls,0);
            d.Power.Remove(p+"Power");d.Interactions.Remove(p+"PowerChange");
            foreach(var form in new[]{"Installed","Loose","InstalledDmg","LooseDmg"})
            {
                var co=d.Objects[p+form];co.jsonPI=null;co.aTickers=Array.Empty<string>();
                co.mapPoints=new[]{"use,0,"+(-8*size.Footprint-8),"PhobosBulkOut,"+8*size.Footprint+",0"};
                if(form == "Installed")co.aInteractions=co.aInteractions.Concat(Work.Select(WorkId)).ToArray();
                // The R3 keeps its four drawn states; the R4 and R5 use one sprite with the game's damage tint.
                if(small)
                {
                    string image="phobos/agriculture/Reservoir"+(form.Contains("Loose")?"Loose":"")+(form.EndsWith("Dmg")?"Damaged":"");
                    co.strPortraitImg=image;d.Items[co.strItemDef].strImg=image;d.Items[co.strItemDef].strImgNorm=image+"Normal";
                }
            }
        }
        Definitions.Stock(d,Nutrients,"bulk_nutrients");
    }
}
