using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;

internal static partial class Service
{
    private static ObjectStateStore DosingStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureDosing",Plugin.Id,1);
    private static ObjectStateStore WorkupStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureWorkup",Plugin.Id,1);
    private static ObjectStateStore ResidueStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureCropResidue",Plugin.Id,1);
    private static ObjectStateStore ChargeStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureNutrientCharge",Plugin.Id,1);
    private static ObjectStateStore DeliveryStore(CondOwner co) => new(co.mapGUIPropMaps,"AgricultureDelivery",Plugin.Id,1);
    private static void ReadWorkup(Session s)
    {
        var status=DosingStore(s.Object).Read(out var fields);
        if(status==SavedStateStatus.Ready)
        {
            if(fields.Count!=1 || !fields.TryGetValue("source",out var source) || source.Length>200 || source.Length>0&&!IrrigationDefinitions.IsSupply(s.Object)) s.Protected=true;
            else s.DoseId=source;
        }
        else if(status!=SavedStateStatus.Missing)s.Protected=true;
        status=WorkupStore(s.Object).Read(out fields);
        if(status==SavedStateStatus.Ready) s.Workup=WorkupJob.Read(fields);
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        if(s.Workup.Mode.Length>0 && !WorkupDefinitions.IsBench(s.Object)) s.Protected=true;
        status=DeliveryStore(s.Object).Read(out fields);
        if(status!=SavedStateStatus.Missing && (status!=SavedStateStatus.Ready || fields.Count!=1 || !fields.TryGetValue("state",out var state) || state!="clear")) s.Protected=true;
    }
    private static double ReadResidue(CondOwner co)
    {
        if(ResidueStore(co).Read(out var fields)!=SavedStateStatus.Ready) throw new ArgumentException("No recorded crop nutrient budget.");
        return NutrientRecovery.Read(fields,co.GetTotalMass());
    }
    private static void WriteResidue(CondOwner co,double nutrient)
    {
        if(!ResidueStore(co).TryWrite(NutrientRecovery.Record(co.GetTotalMass(),nutrient))) throw new InvalidOperationException("Protected crop residue record.");
    }
    private static NutrientCharge ReadCharge(CondOwner co)
    {
        var status=ChargeStore(co).Read(out var fields);
        double initial=co.strCODef==Definitions.Nutrient?.04:co.strCODef==WorkupDefinitions.Makeup?NutrientRecovery.MakeupKg:0;
        if(status==SavedStateStatus.Missing && initial>0 && Math.Abs(co.GetTotalMass()-initial)<1e-8) return new(initial,initial);
        if(status!=SavedStateStatus.Ready) throw new ArgumentException("Unrecorded or protected nutrient charge.");
        return NutrientCharge.Read(fields,co.GetTotalMass());
    }
    private static void WriteCharge(CondOwner co,NutrientCharge charge)
    {
        if(!ChargeStore(co).TryWrite(charge.Save())) throw new InvalidOperationException("Protected nutrient charge.");
        co.AddMass(charge.Remaining-co.GetTotalMass(),true);
        co.SetCondAmount("StatBasePrice",charge.Remaining*(co.strCODef==WorkupDefinitions.Makeup?NutrientRecovery.MakeupPrice/NutrientRecovery.MakeupKg:NutrientRecovery.MixturePricePerKg));
    }
    private static bool QueueWorkup(Session s,string mode)
    {
        if(!WorkupDefinitions.IsBench(s.Object)||!Paused(s)||s.Workup.Mode.Length>0) return false;
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
        if(!s.State.Running || s.Workup.Mode.Length==0) return;
        var job=s.Workup;var input=Resolve(job.Input);var supplement=Resolve(job.Supplement);
        string definition=job.Mode=="recover"?WorkupDefinitions.Residue:WorkupDefinitions.Concentrate;
        if(input==null || !IsInput(s.Object,input,definition,input.GetTotalMass())) {s.State.Running=false;s.Notice=Text.Get("workup_input");return;}
        double nutrient=ReadResidue(input), recovered=nutrient*NutrientRecovery.Fraction;
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
            if(id==WorkupDefinitions.Mixture)WriteCharge(p,new NutrientCharge(nutrient*2,nutrient*2));
            if(id==WorkupDefinitions.Makeup)WriteCharge(p,remaining!);
        });
        if(success){s.Workup=new();Save(s);}else s.State.Running=false;
    }
    private static string DescribeWorkup(Session s) => Text.Get("workup_status",s.Workup.Mode.Length==0?Text.Get("empty"):Text.Get("workup_"+s.Workup.Mode),s.Workup.Energy,
        Text.Get(s.State.Running?"running":"paused"),s.Protected?Text.Get("protected"):s.Notice);
    private static CondOwner? DosingCharge(Session s) => DoseCandidates(s).FirstOrDefault(c=>c.strID==s.DoseId);
    internal static IEnumerable<CondOwner> DoseCandidates(Session s)
    {
        foreach(var co in s.Object.objContainer?.ContainedCOs ?? Enumerable.Empty<CondOwner>())
        {
            if((co.strCODef!=Definitions.Nutrient && co.strCODef!=WorkupDefinitions.Mixture)||!IsInput(s.Object,co,co.strCODef,co.GetTotalMass()))continue;
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
        var charge=ReadCharge(co);amount=Math.Min(amount,charge.Remaining);
        if(!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="pending"}))throw new InvalidOperationException("Protected nutrient dosing.");
        WriteCharge(co,charge.Spend(amount)); // physical debit precedes reservoir credit
        s.State.Nutrients+=amount;Save(s);
        if(charge.Remaining-amount<=0){co.RemoveFromCurrentHome(true);co.Destroy();}
        if(!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Nutrient dosing journal failed.");
    }
    private static string DescribeDose(Session s)
    {
        var co=DosingCharge(s);if(co==null)return Text.Get("dose_empty");
        var charge=ReadCharge(co);return Text.Get("dose_status",co.strNameFriendly,charge.Remaining*1000,100*charge.Remaining/charge.Initial);
    }
}
