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
    /// <summary>What the conduit holds: water. The old feed names (until 0.54.0) stay declared after it, so a pipe or
    /// canister that still holds one keeps draining and filling correctly until a running W2 flushes it out.</summary>
    internal const string WaterCommodity="water";
    internal static IEnumerable<LineCommodity> ConduitCommodities()=>new[]{WaterCommodity}.Concat(LegacyFeed.Commodities()).Select(n=>LineCommodity.Liquid(n,FeedDensityKgPerM3,ConduitBoreMm));
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
            // An old parcel is plain water or an old feed in its crop's proportion; anything else is not ours.
            var expected=s.Line.Profile==LegacyFeed.Water?new LiquidMixture(s.Line.TotalKg,0):LegacyFeed.Split(s.Line.Profile,s.Line.TotalKg);
            if(expected==null||!MixtureTransfer.Same(expected.Value,s.Line.Quantity))s.Protected=true;
        }
        if(!s.Line.Empty && (IrrigationDefinitions.IsSupply(s.Object)||Definitions.IsCooker(s.Object))) s.Protected=true;
    }
    /// <summary>One branch's pump step: <paramref name="tiles"/> of pipe for the flow estimate; through the pipe, the
    /// W2's whole irrigation network is filled before feed reaches the rack (Agriculture 0.53.0: the network, where it was
    /// the run through one route), and a touching rack is fed directly.</summary>
    private static double PumpLine(Session source,Session target,int tiles,bool throughPipe,double elapsed,double share)
    {
        double flow=HydraulicRoute.FlowFraction(tiles,ResistanceTiles);
        double budget=Math.Min(share,elapsed*IrrigationDefinitions.RateKgPerSecond*flow), used=0;
        // A parcel saved by an earlier version was on its way to this rack: it arrives first, whatever the route now is.
        if(!target.Line.Empty)
        {
            double arrived=DeliverLegacyParcel(target,budget);
            used+=arrived; budget-=arrived;
            if(!target.Line.Empty) { target.Notice=Text.Get("line_legacy",target.Line.TotalKg); return used; }
        }
        if(budget<=1e-9) return used;
        const string commodity=WaterCommodity;
        var circuit=IrrigationHolding==null||!throughPipe?Array.Empty<CondOwner>():LineContents.Circuit(source.Object,IrrigationHolding);
        // After a formulation change the pipes still hold the old feed: the pump flushes it back to the W2 first
        // (owner decision, 1 October 2026), as plain water into its reservoir or as recorded process solution for treatment.
        if(IrrigationHolding!=null&&circuit.Any(c=>(LineContents.Read(c)?.Kilograms.Keys??Enumerable.Empty<string>()).Any(k=>k!=commodity)))
        {
            double flushed=Flush(source,circuit,commodity,budget);
            target.Notice=Text.Get(flushed>LegacyFeed.Tolerance?"line_flushing":"line_flush_full",flushed);
            return used+flushed;
        }
        double room=IrrigationHolding==null?0:LineContents.Room(circuit,IrrigationHolding,commodity);
        if(room>1e-6)
        {
            // Priming: the pump fills the branch's conduit from the W2's feed before anything reaches the rack.
            used+=Prime(source,circuit,commodity,Math.Min(budget,room));
            double held=LineContents.Holding(circuit,commodity);
            target.Notice=Text.Get("line_priming",held,held+LineContents.Room(circuit,IrrigationHolding!,commodity),circuit.Count);
            return used;
        }
        // A full conduit passes water straight through: what the W2 pushes in, the rack receives. Nutrients ride with
        // the stream, and with water recirculated through the rack when it is already full (Agriculture 0.55.0).
        double fresh=DeliverWater(source,target,budget);
        used+=fresh;
        target.Notice=Text.Get("line_flow",flow*100,tiles,100*flow*flow);
        used+=FeedNutrients(source,target,fresh,budget-fresh);
        return used;
    }
    /// <summary>A parcel saved by a version before 0.33.0 arrives in its rack as water and nutrients, as much as the
    /// step's budget and the rack's room allow. Returns the kilograms that arrived.</summary>
    private static double DeliverLegacyParcel(Session target,double budget)
    {
        var q=target.Line.Quantity; double total=q.TotalKg;
        if(total<=LegacyFeed.Tolerance||budget<=0) return 0;
        double share=Math.Min(1,budget/total);
        if(q.CarrierKg>0) share=Math.Min(share,Math.Max(0,CropState.ReservoirKg-target.State.Water)/q.CarrierKg);
        if(q.SoluteKg>0) share=Math.Min(share,Math.Max(0,CropState.NutrientCapacityKg-target.State.Nutrients)/q.SoluteKg);
        if(share<=0) return 0;
        var line=target.Line.Copy(); var arriving=q.Scale(share); var left=share>=1-1e-12?default:new LiquidMixture(q.CarrierKg-arriving.CarrierKg,q.SoluteKg-arriving.SoluteKg);
        line.SetQuantity(left);
        target.State.Water+=arriving.CarrierKg; target.State.Nutrients+=arriving.SoluteKg; target.Line=line; Save(target);
        return arriving.TotalKg;
    }
    /// <summary>Fills the conduit with water from the W2's reservoir.</summary>
    private static double Prime(Session source,IReadOnlyList<CondOwner> circuit,string commodity,double kg)
    {
        double amount=Math.Min(kg,source.State.Water);
        if(amount<=LegacyFeed.Tolerance||IrrigationHolding==null) return 0;
        double used=LineContents.Top(circuit,IrrigationHolding,commodity,amount);
        if(used>0) { source.State.Water=Math.Max(0,source.State.Water-used); Save(source); }
        return used;
    }
    /// <summary>The most one recorded process solution item holds, as the tank drain makes them.</summary>
    internal const double DrainageItemKg=20;
    /// <summary>Takes up to <paramref name="budget"/> kilograms of every commodity other than <paramref name="keep"/> (an old
    /// feed) out of the circuit and returns it to the W2: its water into the reservoir and its nutrients into the nutrient
    /// store while there is room, and whatever does not fit as one recorded process solution item in its inventory,
    /// ready for drainage treatment. Nothing moves when that item has no room. Returns the kilograms flushed.</summary>
    private static double Flush(Session source,IReadOnlyList<CondOwner> circuit,string keep,double budget)
    {
        double limit=Math.Min(budget,DrainageItemKg), water=0; var feed=default(LiquidMixture);
        var plan=new List<(CondOwner Segment,LineMixture Mixture)>();
        foreach(var segment in circuit)
        {
            if(limit-(water+feed.TotalKg)<=LegacyFeed.Tolerance) break;
            var m=LineContents.Read(segment);
            if(m==null) continue;
            bool changed=false;
            foreach(string name in m.Kilograms.Keys.Where(k=>k!=keep).ToArray())
            {
                double take=m.Take(name,limit-(water+feed.TotalKg));
                if(take<=0) continue;
                changed=true;
                // An old feed is its crop's water and nutrients; a name no crop carries is kept as water.
                if(LegacyFeed.Split(name,take) is LiquidMixture parts) feed+=parts; else water+=take;
            }
            if(changed) plan.Add((segment,m));
        }
        double total=water+feed.TotalKg;
        if(total<=LegacyFeed.Tolerance) return 0;
        double allWater=water+feed.CarrierKg;
        double toReservoir=Math.Min(allWater,Math.Max(0,CropState.ReservoirKg-source.State.Water));
        double toStore=Math.Min(feed.SoluteKg,Math.Max(0,CropState.NutrientCapacityKg-source.State.Nutrients));
        var drainage=new LiquidMixture(allWater-toReservoir,feed.SoluteKg-toStore);
        if(drainage.TotalKg>LegacyFeed.Tolerance&&!PlaceDrainage(source,drainage)) return 0;
        foreach(var (segment,mixture) in plan) LineContents.Write(segment,mixture);
        source.State.Water+=toReservoir; source.State.Nutrients+=toStore; Save(source);
        return total;
    }
    /// <summary>Puts one recorded process solution item holding <paramref name="q"/> in the W2's inventory, as the drain
    /// work does, so drainage treatment can recover it. False, placing nothing, when there is no room.</summary>
    private static bool PlaceDrainage(Session s,LiquidMixture q)
    {
        var container=s.Object.objContainer;
        if(container==null||container.Locked||q.TotalKg<=LegacyFeed.Tolerance||q.TotalKg>DrainageItemKg+1e-9) return false;
        var product=DataHandler.GetCondOwner(CharacterizedDrainage); bool placed=false;
        try
        {
            product.SetCondAmount("StatMass",q.TotalKg); WriteDrainage(product,q);
            if(!container.AllowedCO(product)) return false;
            var grid=container.gridLayout; var cells=new bool[grid.gridMaxX,grid.gridMaxY];
            for(int x=0;x<grid.gridMaxX;x++) for(int y=0;y<grid.gridMaxY;y++) cells[x,y]=grid.gridID[x,y]!=null||grid.gridInventoryItem[x,y]!=null;
            var size=Ostranauts.Inventory.GUIInventoryItem.GetWidthHeightForCO(product);
            var spot=Phobos.Ostranauts.Framework.Inventory.BatchPlacement.Plan(cells,new[]{new Phobos.Ostranauts.Framework.Inventory.ItemSize(size.x,size.y)});
            if(spot==null) return false;
            container.AddCOSimple(product,new PairXY(spot[0].X,spot[0].Y));
            if(product.objCOParent!=s.Object) throw new InvalidOperationException("Recorded process solution placement failed.");
            placed=true; container.Redraw(); return true;
        }
        finally { if(!placed&&!product.bDestroyed) product.Destroy(); }
    }
    private static string DescribeLine(Session s)=>s.Line.Empty?"":Text.Get("line_legacy",s.Line.TotalKg);
    /// <summary>A drain canister of water put in a W2's inventory pours into its reservoir (Agriculture 0.33.0), as far as
    /// there is room. A canister of an old feed pours in as its water and nutrients, in proportion, as far as both fit.</summary>
    internal static double AcceptCanister(CondOwner co,string commodity,double kg)
    {
        if(!IrrigationDefinitions.IsSupply(co)||!NativeFluidRoute.EndpointReady(co)) return 0;
        var s=Get(co);
        if(s.Protected||WaterGuard(co).Protected||s.RecoveryInput.Length>0||!double.IsFinite(kg)||kg<=0) return 0;
        if(commodity=="water")
        {
            double moved=Math.Min(kg,Math.Max(0,CropState.ReservoirKg-s.State.Water));
            if(moved<=LegacyFeed.Tolerance) return 0;
            s.State.Water+=moved; Save(s); return moved;
        }
        if(LegacyFeed.Split(commodity,kg) is not LiquidMixture whole||whole.TotalKg<=LegacyFeed.Tolerance) return 0;
        double fit=1;
        if(whole.CarrierKg>0) fit=Math.Min(fit,Math.Max(0,CropState.ReservoirKg-s.State.Water)/whole.CarrierKg);
        if(whole.SoluteKg>0) fit=Math.Min(fit,Math.Max(0,CropState.NutrientCapacityKg-s.State.Nutrients)/whole.SoluteKg);
        if(fit*kg<=LegacyFeed.Tolerance)
        {
            // No room for it as water and nutrients: it becomes recorded process solution in the W2's inventory, for
            // drainage treatment (owner decision, 1 October 2026), one item at a time.
            double portion=Math.Min(kg,DrainageItemKg);
            if(!PlaceDrainage(s,whole.Scale(portion/kg))) return 0;
            Save(s); return portion;
        }
        var poured=whole.Scale(fit);
        s.State.Water+=poured.CarrierKg; s.State.Nutrients+=poured.SoluteKg; Save(s); return poured.TotalKg;
    }
}

/// <summary>The W2 as a drain-canister receiver (Agriculture 0.33.0): water, or its own feed, pours into its reservoir.</summary>
internal sealed class W2CanisterReceiver : ICanisterReceiver
{
    public string Id => "PhobosAgriculture.W2";
    public bool Handles(CondOwner machine) => machine != null && IrrigationDefinitions.IsSupply(machine);
    public double Accept(CondOwner machine, string commodity, double kg) => Service.AcceptCanister(machine, commodity, kg);
}
