using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;

namespace Phobos.Ostranauts.Framework.Crew;

[Flags]
public enum OrderFields { None=0, Stock=1, Source=2, Destination=4, Routine=8, Hazardous=16, ClearCrops=32, Drain=64, Target=128 }
/// <summary>Additive UI contract. Content owns applicability, candidate rules and validation.</summary>
public interface ICrewOrderPresentation
{
    OrderFields Fields(CondOwner equipment);
    OrderState Activity(CondOwner equipment,StandingOrder order);
    bool RelevantStore(CondOwner equipment,StandingOrder draft,CondOwner store,bool output);
    IEnumerable<Ship> Targets(CondOwner equipment);
    bool Validate(CondOwner equipment,StandingOrder draft,out string reason);
}
public sealed class OrderDraft
{
    public readonly string EquipmentId, Expected;
    public StandingOrder Value {get;}
    public OrderDraft(string id,StandingOrder source){EquipmentId=id;Expected=Fingerprint(source);Value=StandingOrder.Read(source.Save());}
    public bool Dirty=>Expected!=Fingerprint(Value);
    public static string Fingerprint(StandingOrder order)=>CrewBalance.Binding(order.Save().OrderBy(p=>p.Key,StringComparer.Ordinal).SelectMany(p=>new[]{p.Key,p.Value}));
    public static StandingOrder Commit(StandingOrder value)
    {
        var next=StandingOrder.Read(value.Save());
        if(next.Permission==WorkPermission.Enabled){next.Permission=WorkPermission.Suspended;next.StopReason="changed";}
        next.Binding="";return next;
    }
}
public enum OrderState { Running, Disabled, NeedsSetup, Waiting, Blocked, Stopped }
public sealed class OrderStatus
{
    public OrderState State {get;}
    public string Work {get;}
    public string Worker {get;}
    public string Detail {get;}
    public OrderStatus(OrderState state,string work,string worker,string detail){State=state;Work=work;Worker=worker;Detail=detail;}
    public string Label=>ConsoleText.Get("state_"+State);
}
public static class OrderConfiguration
{
    public static OrderFields Fields(CondOwner co)=>CrewWork.Provider(co) is ICrewOrderPresentation p?p.Fields(co):OrderFields.None;
    public static bool Apply(CondOwner co,OrderDraft draft,out string reason)
    {
        reason=ConsoleText.Get("stale");
        if(!CrewWork.CanManage(co)||CrewSim.GetSelectedCrew()?.ship!=co.ship||draft.EquipmentId!=co.strID||CrewWork.Order(co).Protected||
            draft.Expected!=OrderDraft.Fingerprint(CrewWork.Order(co)))return false;
        if(!draft.Dirty){reason=ConsoleText.Get("no_changes");return true;}
        var next=StandingOrder.Read(draft.Value.Save());var provider=CrewWork.Provider(co);
        reason=ConsoleText.Get("invalid_process");if(next.Protected||provider==null||!provider.Recipes(co).Contains(next.Recipe))return false;
        var fields=Fields(co);
        foreach(var pair in new[]{(OrderFields.Source,next.Source,CrewWork.Order(co).Source),(OrderFields.Destination,next.Destination,CrewWork.Order(co).Destination)})
            if(fields.HasFlag(pair.Item1)&&pair.Item2!="none"&&pair.Item2!=pair.Item3 && !CrewWork.Stores(co.ship).Any(c=>c.strID==pair.Item2))
            {reason=ConsoleText.Get("invalid_store");return false;}
        if(provider is ICrewOrderPresentation presentation && !presentation.Validate(co,next,out reason))return false;
        next=OrderDraft.Commit(next);
        reason=ConsoleText.Get("protected");
        bool saved=CrewWork.Configure(co,o=>Copy(next,o));if(saved)reason=ConsoleText.Get("applied");return saved;
    }
    private static void Copy(StandingOrder a,StandingOrder b)
    {b.Permission=a.Permission;b.Recipe=a.Recipe;b.Source=a.Source;b.Destination=a.Destination;b.Target=a.Target;b.Binding=a.Binding;b.Stock=a.Stock;
        b.Hazardous=a.Hazardous;b.ClearCrops=a.ClearCrops;b.Drain=a.Drain;b.ResumeRoutine=a.ResumeRoutine;b.StopReason=a.StopReason;}
}
