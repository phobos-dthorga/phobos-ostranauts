using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;
internal static partial class Service
{
    // Since Agriculture 0.33.0 (owner decision, 1 October 2026: lines hold their contents until drained) the irrigation
    // conduit holds its own water or feed, tile by tile, through Framework's line contents: the W2's pump fills its
    // branch's conduit first and feeds the rack straight through once the conduit is full. The per-rack parcel record
    // ("AgricultureLine") survives only for parcels saved by earlier versions, which the next powered step delivers
    // into their rack; it is still saved and massed at the rack until then.
    private const double ResistanceTiles=16;
    /// <summary>The irrigation conduit's inner bore, authored at 16 mm (drip lateral), about 0.20 kg of feed a tile.</summary>
    internal const double ConduitBoreMm=16;
    /// <summary>Feed is a dilute solution (a few grams of salts a kilogram), held at water's density.</summary>
    internal const double FeedDensityKgPerM3=998.2;
    internal static LineHoldUpFamily? IrrigationHolding;
    /// <summary>The line commodity a feed profile is held as in the conduit and in drain canisters (stable saved names).</summary>
    internal static string FeedCommodity(string profile)=>profile==NutrientSolution.Potato?"potato feed":profile==NutrientSolution.Lettuce?"lettuce feed":
        profile==NutrientSolution.LettuceSeed?"lettuce seed feed":profile==NutrientSolution.None?"water":throw new ArgumentException("Unknown solution profile.");
    internal static readonly string[] FeedProfiles={NutrientSolution.None,NutrientSolution.Potato,NutrientSolution.Lettuce,NutrientSolution.LettuceSeed};
    internal static IEnumerable<LineCommodity> ConduitCommodities()=>FeedProfiles.Select(p=>LineCommodity.Liquid(FeedCommodity(p),FeedDensityKgPerM3,ConduitBoreMm));
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
        double flow=HydraulicRoute.FlowFraction(path.Length,ResistanceTiles);
        double budget=Math.Min(share,elapsed*IrrigationDefinitions.RateKgPerSecond*flow), used=0;
        // A parcel saved by an earlier version was on its way to this rack: it arrives first, whatever the route now is.
        if(!target.Line.Empty)
        {
            var receipt=LiquidTransferGuard.Commit(new LineReservoir(target),new FeedReservoir(target),budget,LineGuard(target.Object),WaterGuard(target.Object));
            used+=receipt.Received.TotalKg; budget-=receipt.Received.TotalKg;
            if(!target.Line.Empty) { target.Notice=Text.Get("line_legacy",target.Line.TotalKg); return used; }
        }
        if(budget<=1e-9) return used;
        string commodity=FeedCommodity(source.Solution.Profile);
        var circuit=IrrigationHolding==null?Array.Empty<CondOwner>():LineContents.Circuit(source.Object.ship,IrrigationHolding,path);
        double room=IrrigationHolding==null?0:LineContents.Room(circuit,IrrigationHolding,commodity);
        if(room>1e-6)
        {
            // Priming: the pump fills the branch's conduit from the W2's feed before anything reaches the rack.
            used+=Prime(source,circuit,commodity,Math.Min(budget,room));
            double held=LineContents.Holding(circuit,commodity);
            target.Notice=Text.Get("line_priming",held,held+LineContents.Room(circuit,IrrigationHolding!,commodity),path.Length);
            return used;
        }
        // A full conduit passes feed straight through: what the W2 pushes in, the rack receives.
        var delivered=LiquidTransferGuard.Commit(new FeedReservoir(source),new FeedReservoir(target),budget,WaterGuard(source.Object),WaterGuard(target.Object));
        used+=delivered.Received.TotalKg;
        target.Notice=Text.Get("line_flow",flow*100,path.Length,100*flow*flow); return used;
    }
    /// <summary>Fills the conduit from the W2's reservoir: the kilograms the segments take leave the reservoir in its own
    /// proportions (water, or feed at its profile's ratio).</summary>
    private static double Prime(Session source,IReadOnlyList<CondOwner> circuit,string commodity,double kg)
    {
        var reservoir=new FeedReservoir(source); var quantity=reservoir.Quantity; double total=quantity.TotalKg;
        double amount=Math.Min(kg,total);
        if(amount<=NutrientSolution.Tolerance||IrrigationHolding==null) return 0;
        double used=LineContents.Top(circuit,IrrigationHolding,commodity,amount);
        if(used>0) reservoir.SetQuantity(quantity-quantity.Scale(used/total));
        return used;
    }
    private static string DescribeLine(Session s)=>s.Line.Empty?"":Text.Get("line_legacy",s.Line.TotalKg);
    /// <summary>A drain canister of water, or of this W2's own feed, put in its inventory pours into its reservoir
    /// (Agriculture 0.33.0), as far as the reservoir has room for each component.</summary>
    internal static double AcceptCanister(CondOwner co,string commodity,double kg)
    {
        if(!IrrigationDefinitions.IsSupply(co)||!NativeFluidRoute.EndpointReady(co)) return 0;
        var s=Get(co);
        if(s.Protected||WaterGuard(co).Protected||s.RecoveryInput.Length>0||!double.IsFinite(kg)||kg<=0) return 0;
        if(commodity=="water")
        {
            double moved=Math.Min(kg,Math.Max(0,s.Solution.PlainWaterCapacity-s.State.Water));
            if(moved<=NutrientSolution.Tolerance) return 0;
            s.State.Water+=moved; Save(s); return moved;
        }
        if(!s.Solution.Enabled||commodity!=FeedCommodity(s.Solution.Profile)) return 0;
        var reservoir=new FeedReservoir(s); var ratio=NutrientSolution.Ratio(s.Solution.Profile); var q=reservoir.Quantity; var cap=reservoir.ComponentCapacity;
        double fit=kg;
        if(ratio.CarrierKg>0) fit=Math.Min(fit,Math.Max(0,cap.CarrierKg-q.CarrierKg)*ratio.TotalKg/ratio.CarrierKg);
        if(ratio.SoluteKg>0) fit=Math.Min(fit,Math.Max(0,cap.SoluteKg-q.SoluteKg)*ratio.TotalKg/ratio.SoluteKg);
        fit=Math.Min(fit,Math.Max(0,reservoir.TotalCapacityKg-q.TotalKg));
        if(fit<=NutrientSolution.Tolerance) return 0;
        reservoir.SetQuantity(q+ratio.Scale(fit/ratio.TotalKg)); return fit;
    }
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

/// <summary>The W2 as a drain-canister receiver (Agriculture 0.33.0): water, or its own feed, pours into its reservoir.</summary>
internal sealed class W2CanisterReceiver : ICanisterReceiver
{
    public string Id => "PhobosAgriculture.W2";
    public bool Handles(CondOwner machine) => machine != null && IrrigationDefinitions.IsSupply(machine);
    public double Accept(CondOwner machine, string commodity, double kg) => Service.AcceptCanister(machine, commodity, kg);
}
