using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal sealed class AgricultureBulkSupplies : IBulkSupplyProvider
{
    internal const string CropNutrients="agriculture.crop-nutrients";
    public string Id=>Plugin.Id;
    public IEnumerable<BulkSupplyOffer> Offers
    {
        get
        {
            if(!Definitions.Ready)yield break;
            // Water is Framework's offer since Agriculture 0.31.0: the reservoirs became Framework's water tanks.
            yield return new("agriculture.nutrients",Text.Get("bulk_nutrients"),Text.Get("bulk_unit_charge"),AgricultureMaterials.Price(BulkDefinitions.Nutrients),1,1);
            // Crop nutrients by the kilogram into a Groundwork hopper, at the bulk charge's own price per kilogram.
            yield return new(CropNutrients,Text.Get("hopper_offer"),Text.Get("bulk_unit_kg"),Core.HopperRules.PricePerKg(AgricultureMaterials.Price(BulkDefinitions.Nutrients),BulkDefinitions.NutrientKg),
                Core.HopperRules.KioskStepKg,(int)Math.Ceiling(HopperDefinitions.Sizes.Max(s=>s.CapacityKg)/Core.HopperRules.KioskStepKg));
        }
    }
    public IEnumerable<CondOwner> Destinations(Ship ship,BulkSupplyOffer offer)=>ship.GetCOs(null,false,false,true).Where(c=>Eligible(c,offer.Id));
    private static bool Eligible(CondOwner c,string offer)=>Phobos.Ostranauts.Framework.Liquids.NativeFluidRoute.EndpointReady(c)&&
        (offer==CropNutrients?HopperService.Ready(c):
        offer=="agriculture.nutrients"&&IrrigationDefinitions.IsSupply(c)&&!Service.Get(c).Protected&&c.objContainer!=null&&!c.objContainer.Locked);
    public string Revision(CondOwner c,BulkSupplyOffer offer)=>ConfigurationStamp.For(c,new[]{"PhobosState."+(Phobos.Ostranauts.Framework.Liquids.BulkVessels.Of(c)?.Record??"AgricultureBulk"),"PhobosMaterialPort."})+":"+
        string.Join(";",c.objContainer?.ContainedCOs.Select(x=>x.strID).OrderBy(x=>x,StringComparer.Ordinal)??Enumerable.Empty<string>());
    public double Available(CondOwner c,BulkSupplyOffer offer)
    {
        if(!Eligible(c,offer.Id))return 0;
        if(offer.Id==CropNutrients)return Phobos.Ostranauts.Framework.Liquids.BulkVessels.Of(c)!.CapacityKg-HopperService.Read(c).TotalKg;
        // The authored charge occupies one slot. This is only a read-only estimate;
        // the native acceptance and exact item bounds are checked again on delivery.
        return c.objContainer.gridLayout.FindFirstUnoccupiedTile(1,1,"").IsValid()?1:0;
    }
    public bool Validate(CondOwner c,BulkPurchaseQuote q,out string reason)
    {
        reason=Text.Get("bulk_quote_changed");var offer=Offers.FirstOrDefault(o=>o.Id==q.Offer);
        if(offer==null||!Eligible(c,q.Offer)||Revision(c,offer)!=q.Revision||q.UnitPrice!=offer.UnitPrice||q.Quantity>offer.Increment*offer.MaximumSteps||
            Math.Abs(q.Quantity/offer.Increment-Math.Round(q.Quantity/offer.Increment))>1e-8||Available(c,offer)+1e-8<q.Quantity)return false;
        return true;
    }
    public double Deliver(CondOwner c,BulkPurchaseQuote q)
    {
        if(!Validate(c,q,out _))return 0;
        if(q.Offer==CropNutrients)
        {
            var state=BulkService.Read(c);double before=state.ServiceKg;state.SetService(before+q.Quantity);BulkService.Save(c,state);
            return BulkService.Read(c).ServiceKg-before;
        }
        var item=DataHandler.GetCondOwner(BulkDefinitions.Nutrients);
        try
        {
            if(!c.objContainer.AllowedCO(item)||!c.objContainer.CanAddSimple(item,out var cell))return 0;
            c.objContainer.AddCOSimple(item,cell);
            if(item.objCOParent!=c)throw new InvalidOperationException("Uncertain nutrient delivery custody");
            c.objContainer.Redraw();return 1;
        }
        finally{if(item.objCOParent==null&&item.ship==null)item.Destroy();}
    }
}
