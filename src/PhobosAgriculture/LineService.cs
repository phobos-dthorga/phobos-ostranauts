using System;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;
internal static partial class Service
{
    // Authored closed microtube surrogate. Contents are saved/massed at the receiving
    // rack as their custodian, including when a pipe is removed; they are never free cargo.
    private const double LineKgPerTile=.01, TransitPerTile=.5, ResistanceTiles=16;
    private static ObjectStateStore LineStore(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureLine",Plugin.Id,1);
    internal static LiquidTransferGuard LineGuard(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureLineTransfer",Plugin.Id);
    private static void ReadLine(Session s)
    {
        var status=LineStore(s.Object).Read(out var fields);
        if(status==SavedStateStatus.Ready) s.Line=FluidLine.Read(fields);
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        if(LineGuard(s.Object).Protected) s.Protected=true;
        if(s.Line.Profile.Length>0)
        {
            _=NutrientSolution.CropId(s.Line.Profile);
            var expected=s.Line.Profile==NutrientSolution.None?new LiquidMixture(s.Line.TotalKg,0):NutrientSolution.Ratio(s.Line.Profile).Scale(s.Line.TotalKg/NutrientSolution.Ratio(s.Line.Profile).TotalKg);
            if(!MixtureTransfer.Same(expected,s.Line.Quantity))s.Protected=true;
        }
        if(!s.Line.Empty && (IrrigationDefinitions.IsSupply(s.Object)||Definitions.IsCooker(s.Object)||s.Line.Profile!=s.Solution.Profile)) s.Protected=true;
    }
    private static double PumpLine(Session source,Session target,int[] path,double elapsed,double share)
    {
        using var digest=System.Security.Cryptography.SHA256.Create();
        string route=BitConverter.ToString(digest.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source.Object.ship.strRegID+"|"+source.Object.strID+"|"+target.Object.strID+"|"+string.Join(";",path)))).Replace("-","");
        if(!target.Line.Empty && target.Line.Route!=route) { target.Notice=Text.Get("line_drain"); return 0; }
        target.Line.Configure(route,source.Solution.Profile,path.Length*LineKgPerTile,path.Length*TransitPerTile);
        double flow=HydraulicRoute.FlowFraction(path.Length,ResistanceTiles);
        double budget=Math.Min(share,elapsed*IrrigationDefinitions.RateKgPerSecond*flow), used=0;
        // Delay advances only on a valid, powered route, with no reload/offline catch-up.
        bool ready=target.Line.Ready, transporting=!target.Line.Empty&&!ready;
        target.Line.Advance(elapsed*Math.Min(flow,share/(elapsed*IrrigationDefinitions.RateKgPerSecond))); Save(target);
        // Transit spends this branch's pump work too; it cannot also fund blending/refill.
        if(transporting)return budget;
        var line=new LineReservoir(target);
        if(ready&&!target.Line.Empty)
        {
            var receipt=LiquidTransferGuard.Commit(line,new FeedReservoir(target),budget,LineGuard(target.Object),WaterGuard(target.Object));
            used+=receipt.Received.TotalKg; budget-=receipt.Received.TotalKg;
        }
        // One discrete line parcel at a time prevents new feed bypassing its transit delay.
        if(target.Line.Empty&&budget>1e-9)
        {
            var receipt=LiquidTransferGuard.Commit(new FeedReservoir(source),line,budget,WaterGuard(source.Object),LineGuard(target.Object));
            used+=receipt.Received.TotalKg;
        }
        target.Notice=Text.Get("line_flow",flow*100,path.Length,100*flow*flow); return used;
    }
    private static string DescribeLine(Session s)=>Text.Get("line_status",s.Line.TotalKg,s.Line.CapacityKg,s.Line.RemainingSeconds);
    private sealed class LineReservoir:IMixtureReservoir
    {
        private readonly Session s; internal LineReservoir(Session value){s=value;}
        public string Identity=>s.Object.strID+".irrigation-line";
        public string ShipId=>s.Object.ship.strRegID;
        public string Profile=>"phobos.agriculture.feed."+s.Line.Profile;
        public LiquidMixture Quantity=>s.Line.Quantity;
        public LiquidMixture ComponentCapacity=>new(s.Line.CapacityKg,s.Line.CapacityKg);
        public double TotalCapacityKg=>s.Line.CapacityKg;
        public void SetQuantity(LiquidMixture q){s.Line.SetQuantity(q);Save(s);}
    }
    private sealed class FeedReservoir:IMixtureReservoir
    {
        private readonly Session s; internal FeedReservoir(Session value){s=value;}
        public string Identity=>s.Object.strID;
        public string ShipId=>s.Object.ship.strRegID;
        public string Profile=>"phobos.agriculture.feed."+s.Solution.Profile;
        public LiquidMixture Quantity=>s.Solution.Enabled?s.Solution.Quantity:new(s.State.Water,0);
        public LiquidMixture ComponentCapacity=>s.Solution.Enabled?new(Math.Max(0,CropState.ReservoirKg-s.State.Water),Math.Max(0,CropState.NutrientCapacityKg-s.State.Nutrients)):new(s.Solution.PlainWaterCapacity,0);
        public double TotalCapacityKg=>s.Solution.Enabled?Math.Max(0,CropState.ReservoirKg-s.State.Water):s.Solution.PlainWaterCapacity;
        public void SetQuantity(LiquidMixture q){if(s.Solution.Enabled)s.Solution.Quantity=q;else s.State.Water=q.CarrierKg;Save(s);}
    }
}
