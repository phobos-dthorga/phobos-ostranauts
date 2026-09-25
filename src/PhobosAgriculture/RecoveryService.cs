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
            var job=RecoveryWork.Read(d);s.RecoveryInput=job.Input;s.RecoveryFilter=job.Filter;s.RecoveryEnergy=job.Energy;
            if(s.RecoveryInput.Length>0&&!IrrigationDefinitions.IsSupply(s.Object))s.Protected=true;
        }
        else if(status!=SavedStateStatus.Missing) s.Protected=true;
        status=RecoveryJournal(s.Object).Read(out d);
        if(status!=SavedStateStatus.Missing && (status!=SavedStateStatus.Ready||d.Count!=1||!d.TryGetValue("state",out var state)||state!="clear")) s.Protected=true;
    }
    private static void SaveRecovery(Session s)
    {
        if(!RecoveryStore(s.Object).TryWrite(RecoveryWork.Save(s.RecoveryInput,s.RecoveryFilter,s.RecoveryEnergy))) throw new InvalidOperationException("Protected recovery state.");
    }
    private static bool QueueRecovery(Session s)
    {
        if(!IrrigationDefinitions.IsSupply(s.Object)||!Paused(s)||s.RecoveryInput.Length>0) return false;
        var input=s.Object.objContainer?.ContainedCOs.FirstOrDefault(c=>c.strCODef==CharacterizedDrainage && Input(s.Object,c.strCODef,c.GetTotalMass())==c);
        var filter=Input(s.Object,RecoveryCartridge,DrainageRecovery.CartridgeKg);
        if(input==null||filter==null) { s.Notice=Text.Get("recovery_input");return false; }
        _=ReadDrainage(input);s.RecoveryInput=input.strID;s.RecoveryFilter=filter.strID;s.RecoveryEnergy=0;Save(s);s.Notice=Text.Get("recovery_ready");return true;
    }
    private static void RecoveryTick(Session s,double energy)
    {
        if(!s.State.Running) return;
        var input=Resolve(s.RecoveryInput);var filter=Resolve(s.RecoveryFilter);
        if(input==null||filter==null||Input(s.Object,CharacterizedDrainage,input.GetTotalMass())!=input||Input(s.Object,RecoveryCartridge,DrainageRecovery.CartridgeKg)!=filter)
        {s.State.Running=false;s.Notice=Text.Get("recovery_input");return;}
        var measured=ReadDrainage(input);var recovered=DrainageRecovery.Recover(measured);
        double required=measured.TotalKg*DrainageRecovery.KWhPerKg;
        s.RecoveryEnergy=Math.Min(required,s.RecoveryEnergy+energy);Save(s);
        if(s.RecoveryEnergy+1e-9<required) {s.Notice=Text.Get("recovery_progress",100*s.RecoveryEnergy/required);return;}
        if(s.State.Water+recovered.CarrierKg>s.Solution.PlainWaterCapacity||s.State.Nutrients+recovered.SoluteKg>s.Solution.DryCapacity)
        {s.State.Running=false;s.Notice=Text.Get("recovery_capacity");return;}
        var next=s.State.Copy();next.Water+=recovered.CarrierKg;next.Nutrients+=recovered.SoluteKg;next.Running=next.Receiving=false;
        if(!RecoveryJournal(s.Object).TryWrite(new Dictionary<string,string>{["state"]="pending",["input"]=input.strID,["filter"]=filter.strID})) throw new InvalidOperationException("Protected recovery commit.");
        bool delivered=Deliver(s,new(){(RecoveryReject,DrainageRecovery.RejectKg(measured))},input,next,additionalInput:filter);
        if(delivered) {s.RecoveryInput=s.RecoveryFilter="";s.RecoveryEnergy=0;Save(s);}
        else s.State.Running=false;
        if(!RecoveryJournal(s.Object).TryWrite(new Dictionary<string,string>{["state"]="clear"})) throw new InvalidOperationException("Recovery journal completion failed.");
    }
}
