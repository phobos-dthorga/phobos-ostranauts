using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;
internal static partial class Service
{
    internal const string RecoveryReject="PhobosVerdemorrowTreatmentRejects";
    internal const string CharacterizedDrainage="PhobosVerdemorrowCharacterizedProcessSolution", RecoveryCartridge="PhobosVerdemorrowGroundworkRecoveryCartridge";
    private static ObjectStateStore DrainageStore(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureDrainage",Plugin.Id,1);
    private static ObjectStateStore RecoveryStore(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureRecovery",Plugin.Id,1);
    private static ObjectStateStore RecoveryJournal(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureRecoveryCommit",Plugin.Id,1);
    private static ObjectStateStore CartridgeStore(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureCartridge",Plugin.Id,1);
    private static double ReadCartridge(CondOwner co)
    {
        var status=CartridgeStore(co).Read(out var fields);
        if(status==SavedStateStatus.Missing && Math.Abs(co.GetTotalMass()-DrainageRecovery.CartridgeKg)<1e-7)
            return TreatmentCartridge.CapacityKg;
        if(status!=SavedStateStatus.Ready) throw new ArgumentException("Protected or unrecorded partial cartridge.");
        return TreatmentCartridge.Read(fields,co.GetTotalMass());
    }
    private static void WriteCartridge(CondOwner co,double remaining)
    {
        if(!CartridgeStore(co).TryWrite(TreatmentCartridge.Save(remaining))) throw new InvalidOperationException("Protected cartridge record.");
        co.SetCondAmount("StatMass",TreatmentCartridge.Mass(remaining));
        co.SetCondAmount("StatBasePrice",TreatmentCartridge.Price(remaining));
    }
    private static CondOwner? FindCartridge(Session s,double required)
    {
        foreach(var co in s.Object.objContainer?.ContainedCOs ?? Enumerable.Empty<CondOwner>())
        {
            if(co.strCODef!=RecoveryCartridge || !IsInput(s.Object,co,RecoveryCartridge,co.GetTotalMass())) continue;
            try { if(ReadCartridge(co)>=required) return co; } catch(ArgumentException) { }
        }
        return null;
    }
    private static string DescribeCartridge(Session s)
    {
        if(!s.RecoveryMetered) return Text.Get("recovery_legacy");
        var filter=Resolve(s.RecoveryFilter);
        try { return filter==null?Text.Get("recovery_input"):Text.Get("recovery_capacity_status",ReadCartridge(filter)); }
        catch(ArgumentException) { return Text.Get("recovery_input"); }
    }
    private static void WriteDrainage(CondOwner co,LiquidMixture q)
    { if(!DrainageStore(co).TryWrite(DrainageRecovery.Save(q))) throw new InvalidOperationException("Protected drainage record."); }
    private static LiquidMixture ReadDrainage(CondOwner co)
    {
        if(co.strCODef!=CharacterizedDrainage||DrainageStore(co).Read(out var d)!=SavedStateStatus.Ready) throw new ArgumentException("Uncharacterized drainage.");
        return DrainageRecovery.Read(d,co.GetTotalMass());
    }
    private static void ReadRecovery(Session s)
    {
        var status=RecoveryStore(s.Object).Read(out var d);
        if(status==SavedStateStatus.Ready)
        {
            var job=RecoveryWork.Read(d);s.RecoveryInput=job.Input;s.RecoveryFilter=job.Filter;s.RecoveryEnergy=job.Energy;s.RecoveryMetered=job.Metered;
            if(s.RecoveryInput.Length>0&&!IrrigationDefinitions.IsSupply(s.Object))s.Protected=true;
        }
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        status=RecoveryJournal(s.Object).Read(out d);
        if(status!=SavedStateStatus.Missing && (status!=SavedStateStatus.Ready||d.Count!=1||!d.TryGetValue("state",out var state)||state!="clear")) s.Protected=true;
    }
    private static void SaveRecovery(Session s)
    {
        if(!RecoveryStore(s.Object).TryWrite(RecoveryWork.Save(s.RecoveryInput,s.RecoveryFilter,s.RecoveryEnergy,s.RecoveryMetered))) throw new InvalidOperationException("Protected recovery state.");
    }
    private static bool QueueRecovery(Session s)
    {
        if(!IrrigationDefinitions.IsSupply(s.Object)||!Paused(s)||s.RecoveryInput.Length>0) return false;
        var input=s.Object.objContainer?.ContainedCOs.FirstOrDefault(c=>c.strCODef==CharacterizedDrainage && IsInput(s.Object,c,c.strCODef,c.GetTotalMass()));
        var filter=input==null?null:FindCartridge(s,ReadDrainage(input).TotalKg);
        if(input==null||filter==null) { s.Notice=Text.Get("recovery_input");return false; }
        _=ReadDrainage(input);s.RecoveryInput=input.strID;s.RecoveryFilter=filter.strID;s.RecoveryEnergy=0;s.RecoveryMetered=true;Save(s);s.Notice=Text.Get("recovery_ready");return true;
    }
    private static void RecoveryTick(Session s,double energy)
    {
        if(!s.State.Running) return;
        var input=Resolve(s.RecoveryInput);var filter=Resolve(s.RecoveryFilter);
        if(input==null||filter==null||!IsInput(s.Object,input,CharacterizedDrainage,input.GetTotalMass())||!IsInput(s.Object,filter,RecoveryCartridge,s.RecoveryMetered?filter.GetTotalMass():DrainageRecovery.CartridgeKg))
        {s.State.Running=false;s.Notice=Text.Get("recovery_input");return;}
        var measured=ReadDrainage(input);var recovered=DrainageRecovery.Recover(measured);
        double remaining=0;
        if(s.RecoveryMetered)
        {
            double capacity=ReadCartridge(filter);
            if(capacity<measured.TotalKg) {s.State.Running=false;s.Notice=Text.Get("recovery_input");return;}
            remaining=TreatmentCartridge.Spend(capacity,measured.TotalKg);
        }
        else if(CartridgeStore(filter).Read(out _)!=SavedStateStatus.Missing)
        {s.State.Running=false;s.Notice=Text.Get("recovery_input");return;} // Historic jobs require their original unused cartridge.
        double required=measured.TotalKg*DrainageRecovery.KWhPerKg;
        s.RecoveryEnergy=Math.Min(required,s.RecoveryEnergy+energy);Save(s);
        if(s.RecoveryEnergy+1e-9<required) {s.Notice=Text.Get("recovery_progress",100*s.RecoveryEnergy/required);return;}
        if(s.State.Water+recovered.CarrierKg>s.Solution.PlainWaterCapacity||s.State.Nutrients+recovered.SoluteKg>s.Solution.DryCapacity)
        {s.State.Running=false;s.Notice=Text.Get("recovery_capacity");return;}
        var next=s.State.Copy();next.Water+=recovered.CarrierKg;next.Nutrients+=recovered.SoluteKg;next.Running=next.Receiving=false;
        if(!RecoveryJournal(s.Object).TryWrite(new Dictionary<string,string>{["state"]="pending",["input"]=input.strID,["filter"]=filter.strID})) throw new InvalidOperationException("Protected recovery commit.");
        double returnedMass=TreatmentCartridge.Mass(remaining);
        var products=new List<(string,double)> { (RecoveryReject,measured.TotalKg+filter.GetTotalMass()-returnedMass-recovered.TotalKg) };
        if(remaining>0) products.Add((RecoveryCartridge,returnedMass));
        bool delivered=Deliver(s,products,input,next,additionalInput:filter,cartridgeRemaining:remaining);
        if(delivered) {s.RecoveryInput=s.RecoveryFilter="";s.RecoveryEnergy=0;s.RecoveryMetered=false;Save(s);}
        else s.State.Running=false;
        if(!RecoveryJournal(s.Object).TryWrite(new Dictionary<string,string>{["state"]="clear"})) throw new InvalidOperationException("Recovery journal completion failed.");
    }
}
