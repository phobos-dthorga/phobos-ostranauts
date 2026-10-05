using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;

internal static partial class Service
{
    internal static bool CrewReplaceDose(CondOwner co,CondOwner actor)
    {
        if(Access(co,null,actor)!=null||!IrrigationDefinitions.IsSupply(co))return false;
        var s=Get(co);if(s.Protected||!s.Solution.Enabled||DoseReady(s))return false;
        var selected=DoseCandidates(s).OrderBy(c=>c.strID,StringComparer.Ordinal).FirstOrDefault();if(selected==null)return false;
        // Explicit standing-order permission authorizes this replacement only.
        s.State.Running=s.State.Receiving=false;s.DoseId=selected.strID;Save(s);return true;
    }
    private static ObjectStateStore DosingStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureDosing",Plugin.Id,1);
    private static ObjectStateStore WorkupStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureWorkup",Plugin.Id,1);
    private static ObjectStateStore ResidueStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureCropResidue",Plugin.Id,1);
    private static ObjectStateStore ChargeStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureNutrientCharge",Plugin.Id,1);
    private static ObjectStateStore DeliveryStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureDelivery",Plugin.Id,1);
    private static ObjectStateStore PressStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureStrawPress",Plugin.Id,1);
    private static void ReadWorkup(Session s)
    {
        var status=DosingStore(s.Object).Read(out var fields);
        if(status==SavedStateStatus.Ready)
        {
            var source=DosingBinding.Read(fields);
            if(source.Length>0&&!IrrigationDefinitions.IsSupply(s.Object)) s.Protected=true;
            else s.DoseId=source;
        }
        else if(status!=SavedStateStatus.Missing)s.Protected=true;
        status=WorkupStore(s.Object).Read(out fields);
        if(status==SavedStateStatus.Ready) s.Workup=WorkupJob.Read(fields);
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        if(s.Workup.Mode.Length>0 && !WorkupDefinitions.IsBench(s.Object)) s.Protected=true;
        status=PressStore(s.Object).Read(out fields);
        if(status==SavedStateStatus.Ready) s.Press=StrawPress.Read(fields);
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        if(!s.Press.Empty && !WorkupDefinitions.IsBench(s.Object)) s.Protected=true;
        status=DeliveryStore(s.Object).Read(out fields);
        if(status!=SavedStateStatus.Missing && (status!=SavedStateStatus.Ready || fields.Count!=1 || !fields.TryGetValue("state",out var state) || state!="clear")) s.Protected=true;
    }
    private static double ReadResidue(CondOwner co)
    {
        if(ResidueStore(co).Read(out var fields)!=SavedStateStatus.Ready) throw new ArgumentException("No recorded crop nutrient budget.");
        return NutrientRecovery.Read(fields,co.GetTotalMass());
    }
    private static void WriteResidue(CondOwner co,double nutrient,double? organic=null)
    {
        if(!ResidueStore(co).TryWrite(NutrientRecovery.Record(co.GetTotalMass(),nutrient,organic))) throw new InvalidOperationException("Protected crop residue record.");
    }
    /// <summary>The record of a residue or spent-biomass item the press can take, or null: recorded since Agriculture
    /// 0.44.0 with its organic matter. Older residue still recovers nutrients but cannot be baled.</summary>
    private static (double Nutrient,double Organic)? Pressable(CondOwner owner,CondOwner co)
    {
        if((co.strCODef!=WorkupDefinitions.Residue&&co.strCODef!=WorkupDefinitions.Spent)||!IsInput(owner,co,co.strCODef,co.GetTotalMass()))return null;
        if(ResidueStore(co).Read(out var fields)!=SavedStateStatus.Ready)return null;
        try{var r=NutrientRecovery.ReadFull(fields,co.GetTotalMass());return r.Organic is double o&&o>1e-9?(r.Nutrient,o):null;}
        catch(Exception error) when (error is ArgumentException || error is FormatException || error is KeyNotFoundException || error is OverflowException){return null;}
    }
    /// <summary>A one-item bench conversion (flax scutching since Agriculture 0.45.0, beet sugar since 0.46.0): the bound
    /// item gives its product and a recorded residue for the straw press.</summary>
    private static void ConversionTick(Session s,BenchConversion c,double energy)
    {
        var job=s.Workup;var input=Resolve(job.Input);
        if(input==null||!IsInput(s.Object,input,c.Input,c.InputKg)){s.State.Running=false;s.Notice=Text.Get("workup_input");return;}
        job.Energy=Math.Min(c.KWh,job.Energy+energy);Save(s);
        if(job.Energy+1e-10<c.KWh)return;
        var products=Enumerable.Repeat((c.Product,c.ProductKg),c.ProductCount).Append((WorkupDefinitions.Residue,c.ResidueKg)).ToList();
        var next=s.State.Copy();next.Running=false;
        bool success=Deliver(s,products,input,next,initialize:(p,id)=>{if(id==WorkupDefinitions.Residue)WriteResidue(p,c.ResidueMineralsKg,c.ResidueOrganicKg);});
        if(success){s.Workup=new();Save(s);}else s.State.Running=false;
    }
    /// <summary>Whether any pressable item sits in the bench and the press has room for one, for the crew's flax order.</summary>
    internal static bool PressLoadable(CondOwner co)
    {
        var s=Get(co);
        return StackUnits.All(co).Any(c=>c.strID!=s.Workup.Input&&c.strID!=s.Workup.Supplement&&Pressable(co,c)!=null&&s.Press.TotalKg+c.GetTotalMass()<=StrawPress.CapacityKg+1e-9);
    }
    /// <summary>Whether the press has work to do while the bench runs with no workup job: drying, or bales to pack.</summary>
    internal static bool PressWork(Session s)=>s.Workup.Mode.Length==0&&!s.Press.Empty&&(s.Press.Surplus>1e-9||s.Press.Bales>0);
    /// <summary>Crew work: every pressable item in the tray goes into the press whole, while it has room. The job's own
    /// supplies stay where they are.</summary>
    private static bool LoadPress(Session s)
    {
        var next=s.Press.Copy();var taken=new List<CondOwner>();
        foreach(var co in StackUnits.All(s.Object).ToArray())
        {
            if(co.strID==s.Workup.Input||co.strID==s.Workup.Supplement||Pressable(s.Object,co) is not (double nutrient,double organic))continue;
            if(next.TotalKg+co.GetTotalMass()>StrawPress.CapacityKg+1e-9)continue;
            next.Absorb(co.GetTotalMass(),nutrient,organic);taken.Add(co);
        }
        if(taken.Count==0){s.Notice=Text.Get(s.Press.TotalKg>=StrawPress.CapacityKg-.2?"press_full":"press_input");return false;}
        // Each item leaves before its mass becomes press contents; the bench's own mass is rebuilt by the save.
        foreach(var co in taken){co.RemoveFromCurrentHome(true);if(co.objCOParent!=null||co.ship!=null)throw new InvalidOperationException("Straw input did not detach.");}
        s.Press=next;Save(s);foreach(var co in taken)co.Destroy();
        s.Object.objContainer?.Redraw();s.Notice=Text.Get("press_loaded",s.Press.TotalKg);return true;
    }
    /// <summary>The dryer and the baler, while the bench runs with no workup job. Drying needs a water tank the bench
    /// reaches with room for all the water it boils off; the steam is condensed there, never lost.</summary>
    private static void PressTick(Session s,double energy)
    {
        var press=s.Press;
        if(press.Surplus>1e-9)
        {
            press.Energy=Math.Min(press.DryingKWh,press.Energy+energy);Save(s);
            if(press.Energy+1e-10<press.DryingKWh)return;
            double water=press.Surplus;
            if(VapourReturn.Room(s)+1e-9<water){s.State.Running=false;s.Notice=Text.Get("press_no_tank",water);return;}
            if(Math.Abs(VapourReturn.Deposit(s,water)-water)>1e-9)throw new InvalidOperationException("Straw dryer water was not stored.");
            press.Dry();Save(s);
        }
        int bales=press.Bales;
        if(bales<1){s.State.Running=false;s.Notice=Text.Get("press_short");return;}
        // As many whole bales as fit the tray, down to one.
        for(int n=bales;n>=1;n--)
        {
            var next=press.Copy();next.Pack(n);
            if(Deliver(s,Enumerable.Repeat((WorkupDefinitions.Bale,StrawPress.BaleKg),n).ToList(),null,s.State.Copy(),nextPress:next))
            {s.Notice=Text.Get("press_baled",n);if(s.Press.Bales<1){s.State.Running=false;if(!s.Press.Empty)s.Notice=Text.Get("press_baled_rest",n,s.Press.TotalKg);}return;}
        }
        s.State.Running=false;s.Notice=Text.Get("full");
    }
    /// <summary>Empties the press back into the tray as one recorded residue, organic matter and all, which the press
    /// or nutrient recovery can take again.</summary>
    private static bool EmptyPress(Session s)
    {
        if(s.Press.Empty){s.Notice=Text.Get("press_empty");return false;}
        var press=s.Press;double mass=press.TotalKg,minerals=press.Minerals,organic=press.Organic;
        if(!Deliver(s,new List<(string,double)>{(WorkupDefinitions.Residue,mass)},null,s.State.Copy(),initialize:(p,id)=>WriteResidue(p,minerals,organic),nextPress:new StrawPress()))return false;
        s.Notice=Text.Get("press_emptied",mass);return true;
    }
    private static string DescribePress(Session s)=>s.Press.Empty?Text.Get("press_status_empty"):
        Text.Get("press_status",s.Press.TotalKg,s.Press.Organic,s.Press.Minerals,s.Press.Water,s.Press.Surplus,s.Press.Energy,s.Press.DryingKWh,s.Press.Bales);
    private static NutrientCharge ReadCharge(CondOwner co)
    {
        var status=ChargeStore(co).Read(out var fields);
        double initial=co.strCODef==BulkDefinitions.Nutrients?BulkDefinitions.NutrientKg:co.strCODef==Definitions.Nutrient?Definitions.NutrientKg:co.strCODef==WorkupDefinitions.Makeup?NutrientRecovery.MakeupKg:0;
        if(status==SavedStateStatus.Missing && initial>0 && Math.Abs(co.GetTotalMass()-initial)<1e-8) return new(initial,initial);
        if(status!=SavedStateStatus.Ready) throw new ArgumentException("Unrecorded or protected nutrient charge.");
        return NutrientCharge.Read(fields,co.GetTotalMass());
    }
    private static void WriteCharge(CondOwner co,NutrientCharge charge)
    {
        if(!ChargeStore(co).TryWrite(charge.Save())) throw new InvalidOperationException("Protected nutrient charge.");
        co.AddMass(charge.Remaining-co.GetTotalMass(),true);
        // What is left, at the materials pack's own price per kilogram for this charge (Agriculture 0.52.0).
        co.SetCondAmount("StatBasePrice",charge.Remaining*AgricultureMaterials.Price(co.strCODef)/AgricultureMaterials.Entry(co.strCODef).kg);
    }
    private static bool QueueWorkup(Session s,string mode)
    {
        if(!WorkupDefinitions.IsBench(s.Object)||!Paused(s)||s.Workup.Mode.Length>0) return false;
        if(BenchConversions.ForMode(mode) is BenchConversion c)
        {
            var item=Input(s.Object,c.Input,c.InputKg);
            if(item==null){s.Notice=Text.Get("workup_input");return false;}
            s.Workup=new WorkupJob{Mode=mode,Input=item.strID}; Save(s); s.Notice=Text.Get("workup_queued"); return true;
        }
        string definition=mode=="recover"?WorkupDefinitions.Residue:WorkupDefinitions.Concentrate;
        foreach(var input in s.Object.objContainer?.ContainedCOs ?? Enumerable.Empty<CondOwner>())
        {
            if(!IsInput(s.Object,input,definition,input.GetTotalMass())) continue;
            double nutrient;
            try { nutrient=ReadResidue(input); } catch(Exception error) when (error is ArgumentException || error is FormatException || error is System.Collections.Generic.KeyNotFoundException || error is OverflowException) { continue; }
            if(nutrient<=1e-8 || mode=="formulate" && Math.Abs(nutrient-input.GetTotalMass())>1e-8) continue;
            CondOwner? supplement=null;
            if(mode=="formulate")
            {
                supplement=s.Object.objContainer!.ContainedCOs.FirstOrDefault(c=>
                {
                    if(!IsInput(s.Object,c,WorkupDefinitions.Makeup,c.GetTotalMass()))return false;
                    try{return ReadCharge(c).Remaining+1e-10>=nutrient;}catch(Exception error) when (error is ArgumentException || error is FormatException || error is System.Collections.Generic.KeyNotFoundException || error is OverflowException){return false;}
                });
                if(supplement==null) continue;
            }
            s.Workup=new WorkupJob{Mode=mode,Input=input.strID,Supplement=supplement?.strID??""}; Save(s); s.Notice=Text.Get("workup_queued"); return true;
        }
        s.Notice=Text.Get("workup_input");return false;
    }
    private static void WorkupTick(Session s,double energy)
    {
        if(!s.State.Running) return;
        if(s.Workup.Mode.Length==0) { if(PressWork(s)) PressTick(s,energy); return; }
        if(BenchConversions.ForMode(s.Workup.Mode) is BenchConversion conversion) { ConversionTick(s,conversion,energy); return; }
        var job=s.Workup;var input=Resolve(job.Input);var supplement=Resolve(job.Supplement);
        string definition=job.Mode=="recover"?WorkupDefinitions.Residue:WorkupDefinitions.Concentrate;
        if(input==null || !IsInput(s.Object,input,definition,input.GetTotalMass())) {s.State.Running=false;s.Notice=Text.Get("workup_input");return;}
        if(ResidueStore(input).Read(out var recordFields)!=SavedStateStatus.Ready) throw new ArgumentException("No recorded crop nutrient budget.");
        var record=NutrientRecovery.ReadFull(recordFields,input.GetTotalMass());
        double nutrient=record.Nutrient, recovered=nutrient*NutrientRecovery.Fraction;
        NutrientCharge? remaining=null;
        if(job.Mode=="formulate")
        {
            if(Math.Abs(nutrient-input.GetTotalMass())>1e-8 || supplement==null || !IsInput(s.Object,supplement,WorkupDefinitions.Makeup,supplement.GetTotalMass()))
            {s.State.Running=false;s.Notice=Text.Get("workup_input");return;}
            var charge=ReadCharge(supplement);
            if(charge.Remaining<nutrient) {s.State.Running=false;s.Notice=Text.Get("workup_input");return;}
            remaining=charge.Spend(nutrient);
        }
        if(nutrient<=1e-8) throw new ArgumentException("Empty recovery allocation.");
        double required=Math.Max(.001,input.GetTotalMass()*NutrientRecovery.KWhPerKg);
        job.Energy=Math.Min(required,job.Energy+energy);Save(s);
        if(job.Energy+1e-10<required)return;
        var products=new List<(string,double)>();
        if(job.Mode=="recover") {products.Add((WorkupDefinitions.Concentrate,recovered));products.Add((WorkupDefinitions.Spent,input.GetTotalMass()-recovered));}
        else {products.Add((WorkupDefinitions.Mixture,nutrient*2));if(remaining!.Remaining>0)products.Add((WorkupDefinitions.Makeup,remaining.Remaining));}
        var next=s.State.Copy();next.Running=false;
        bool success=Deliver(s,products,input,next,additionalInput:supplement,initialize:(p,id)=>
        {
            if(id==WorkupDefinitions.Concentrate)WriteResidue(p,recovered);
            // Spent biomass keeps its organic matter and the nutrients left in it, for the straw press (Agriculture 0.44.0).
            if(id==WorkupDefinitions.Spent&&record.Organic is double organic)WriteResidue(p,nutrient-recovered,organic);
            if(id==WorkupDefinitions.Mixture)WriteCharge(p,new NutrientCharge(nutrient*2,nutrient*2));
            if(id==WorkupDefinitions.Makeup)WriteCharge(p,remaining!);
        });
        if(success){s.Workup=new();Save(s);}else s.State.Running=false;
    }
    private static string DescribeWorkup(Session s) => DescribePress(s)+"\n"+Text.Get("workup_status",s.Workup.Mode.Length==0?Text.Get("empty"):Text.Get("workup_"+s.Workup.Mode),s.Workup.Energy,
        Text.Get(s.State.Running?"running":"paused"),s.Protected?Text.Get("protected"):s.Notice);
    /// <summary>The selected dose source, if it can dose now. A selected hopper is resolved directly (one lookup, no
    /// ship scan), because the W2 asks for its source on every power step.</summary>
    private static CondOwner? DosingCharge(Session s)
    {
        if(Resolve(s.DoseId) is CondOwner hopper&&HopperDefinitions.IsHopper(hopper))return HopperService.CanDose(hopper,s.Object)?hopper:null;
        return ItemDoseCandidates(s).FirstOrDefault(c=>c.strID==s.DoseId);
    }
    internal static bool DoseReady(Session s)=>DosingCharge(s)!=null;
    /// <summary>Every dose source a crew member or the panel may choose: charges in the W2's inventory, then hoppers
    /// within one tile. The hopper part scans the ship; it runs at choice time, never per power step.</summary>
    /// <summary>Why a nutrient hopper aboard is not offered as the W2's dosing source (Agriculture 0.53.0): crop nutrients
    /// travel on no line, so a hopper has to stand within one tile of the W2.</summary>
    internal static string DoseNote(CondOwner co)=>co?.ship==null||!IrrigationDefinitions.IsSupply(co)?"":
        Phobos.Ostranauts.Framework.Controls.LinkChoices.Note(co,null,co.ship.GetCOs(null,false,false,true).Where(c=>c!=co&&c.ship==co.ship&&HopperDefinitions.IsHopper(c)),DoseCandidates(Get(co)));
    internal static IEnumerable<CondOwner> DoseCandidates(Session s)
    {
        foreach(var co in ItemDoseCandidates(s))yield return co;
        // A Groundwork hopper within one tile, while it holds nutrients it can release (Agriculture 0.27.0).
        foreach(var hopper in HopperService.Near(s.Object))if(HopperService.Available(hopper)>1e-10)yield return hopper;
    }
    private static IEnumerable<CondOwner> ItemDoseCandidates(Session s)
    {
        foreach(var co in s.Object.objContainer?.ContainedCOs ?? Enumerable.Empty<CondOwner>())
        {
            if((co.strCODef!=Definitions.Nutrient && co.strCODef!=BulkDefinitions.Nutrients && co.strCODef!=WorkupDefinitions.Mixture)||!IsInput(s.Object,co,co.strCODef,co.GetTotalMass()))continue;
            bool valid=false;try {valid=ReadCharge(co).Remaining>1e-10;}catch(Exception error) when (error is ArgumentException || error is FormatException || error is System.Collections.Generic.KeyNotFoundException || error is OverflowException){}
            if(valid)yield return co;
        }
    }
    // Deplete only what this powered blend will use. No offline timer and no repairable durability.
    private static void Dose(Session s,double budget)
    {
        if(!s.State.Running || !s.Solution.Enabled || budget<=0)return;
        double amount=NutrientCharge.DoseAllowance(s.State,s.Solution,budget);
        var co=amount>1e-10?DosingCharge(s):null;if(co==null)return;
        if(HopperDefinitions.IsHopper(co)){HopperService.Dose(co,s.Object,new DryNutrients(s),amount,WaterGuard(s.Object));return;}
        var charge=ReadCharge(co);amount=Math.Min(amount,charge.Remaining);
        if(!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="pending"}))throw new InvalidOperationException("Protected nutrient dosing.");
        WriteCharge(co,charge.Spend(amount)); // physical debit precedes reservoir credit
        s.State.Nutrients+=amount;Save(s);
        if(charge.Remaining-amount<=0){co.RemoveFromCurrentHome(true);co.Destroy();}
        if(!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Nutrient dosing journal failed.");
    }
    private static string DescribeDose(Session s)
    {
        // A selected hopper is reported even when empty or unready, so the panel says which hopper and why.
        if(Resolve(s.DoseId) is CondOwner hopper&&HopperDefinitions.IsHopper(hopper))
            return Text.Get(HopperService.Ready(hopper)?"dose_hopper_status":"dose_hopper_unready",hopper.strNameFriendly,HopperService.Available(hopper));
        var co=DosingCharge(s);if(co==null)return Text.Get("dose_empty");
        var charge=ReadCharge(co);return Text.Get("dose_status",co.strNameFriendly,charge.Remaining*1000,100*charge.Remaining/charge.Initial);
    }
    /// <summary>Crew work: packs up to one bulk charge (500 g) of a hopper's nutrients into an ordinary Groundwork bulk
    /// nutrient charge in the hopper's tray, at the same value per kilogram, so a hopper can be emptied before it is
    /// moved and its nutrients carried by hand. The hopper's conversion journal covers the swap.</summary>
    internal static bool BagFromHopper(CondOwner hopper,CondOwner actor)
    {
        if(!Definitions.Ready||Access(hopper,null,actor)!=null||!HopperService.Ready(hopper)||hopper.objContainer==null||hopper.objContainer.Locked)return false;
        var state=HopperService.Read(hopper);double kg=Math.Min(BulkDefinitions.NutrientKg,state.AvailableKg);if(kg<=1e-6)return false;
        CondOwner? product=null;bool published=false;Phobos.Ostranauts.Framework.Inventory.TrayDelivery? delivery=null;
        try
        {
            product=DataHandler.GetCondOwner(BulkDefinitions.Nutrients);WriteCharge(product,new NutrientCharge(kg,kg));
            // A full bag joins a stack of identical full bags in the rack (three to a cell), or takes a free cell (Agriculture 0.37.0).
            delivery=hopper.objContainer.AllowedCO(product)?Phobos.Ostranauts.Framework.Inventory.TrayDelivery.Plan(hopper.objContainer,new[]{product}):null;
            if(delivery==null)return false;
            Phobos.Ostranauts.Framework.Liquids.BulkVessel.BeginConversion(hopper,product.strID,state.TotalKg);
            try{delivery.Place();}
            catch{delivery.Rollback();Phobos.Ostranauts.Framework.Liquids.BulkVessel.EndConversion(hopper);throw;}
            if(!Phobos.Ostranauts.Framework.Inventory.StackUnits.Inside(product,hopper))throw new InvalidOperationException("Hopper bag placement failed");published=true;
            state.SetService(state.ServiceKg-kg);Phobos.Ostranauts.Framework.Liquids.BulkVessel.Save(hopper,state);
            Phobos.Ostranauts.Framework.Liquids.BulkVessel.EndConversion(hopper);hopper.objContainer.Redraw();return true;
        }
        catch(Exception e){Plugin.Log(e.ToString());return false;}
        finally{if(product!=null&&!published&&product.objCOParent==null)product.Destroy();}
    }
    /// <summary>The W2's dry-nutrient reservoir as the receiving end of a hopper transfer (capacity is the crop
    /// model's own 0.5 kg); the record is saved with the rest of the W2's state.</summary>
    private sealed class DryNutrients : Phobos.Ostranauts.Framework.Liquids.ILiquidReservoir
    {
        private readonly Session s; internal DryNutrients(Session s) { this.s = s; }
        public string Identity => s.Object.strID; public string ShipId => s.Object.ship.strRegID; public string Commodity => Core.HopperRules.Commodity;
        public double QuantityKg => s.State.Nutrients; public double CapacityKg => CropState.NutrientCapacityKg;
        public void SetQuantity(double kg) { s.State.Nutrients = kg; Save(s); }
    }
}
