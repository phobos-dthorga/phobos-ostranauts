using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Persistence;

internal static class CrewWorkChecks
{
    internal static void Run(Action<bool,string> check)
    {
        bool Near(double a,double b)=>Math.Abs(a-b)<1e-7;
        check(new StandingOrder().Permission==WorkPermission.Disabled,"Standing orders opt in");
        var maps=new Dictionary<string,Dictionary<string,string>>();
        var store=new ObjectStateStore(maps,"crew-order","Framework",1);
        check(store.TryWrite(new StandingOrder().Save()),"A default order fits the actual persistence envelope");
        check(store.Read(out var savedFields)==SavedStateStatus.Ready&&!StandingOrder.Read(savedFields).Protected,
            "Empty optional order values round-trip without invalidating the save map");
        var order=new StandingOrder{Permission=WorkPermission.Enabled,Recipe="potato",Source="A",Destination="B",Stock=8};
        var restored=StandingOrder.Read(order.Save()); restored.Reload(true);
        check(!restored.Protected&&restored.Permission==WorkPermission.Enabled&&restored.Stock==8,"Routine order and exact stores survive reload");
        restored.Reload(false);
        check(restored.Permission==WorkPermission.Suspended&&restored.StopReason=="reload","Industrial reload never grants restart");
        order.Hazardous=true; restored=StandingOrder.Read(order.Save()); restored.Reload(true);
        check(restored.Permission==WorkPermission.Suspended,"Hazardous routine permission still requires Resume");
        order.Hazardous=false; order.ResumeRoutine=false; restored=StandingOrder.Read(order.Save()); restored.Reload(true);
        check(restored.Permission==WorkPermission.Suspended,"Routine resume opt-out persists");
        order.Permission=WorkPermission.Stopped;order.StopReason="manual";
        restored=StandingOrder.Read(order.Save());restored.Reload(true);
        check(restored.Permission==WorkPermission.Stopped&&restored.StopReason=="manual","Manual stop survives every resume policy");
        var corrupt=order.Save();corrupt["permission"]="99";
        check(StandingOrder.Read(corrupt).Protected,"Future permission is not silently enabled");
        corrupt=order.Save();corrupt["stock"]="-1";
        check(StandingOrder.Read(corrupt).Protected,"Invalid target is protected");
        corrupt=order.Save();corrupt.Remove("source");
        check(StandingOrder.Read(corrupt).Protected,"Missing store binding does not become a wildcard");
        order.Binding=CrewBalance.Binding(new[]{"ship","module","processor","target"});
        check(StandingOrder.Read(order.Save()).Binding==order.Binding,"Exact mission scope survives save/load");
        check(store.TryWrite(order.Save())&&ObjectStateStore.SafeValue(order.Binding),"Mission binding fits native saved-value limits");
        check(order.Binding!=CrewBalance.Binding(new[]{"ship","other-module","processor","target"}),"A replaced module changes mission scope");
        check(CrewBalance.Binding(new[]{"a|b","c"})!=CrewBalance.Binding(new[]{"a","b|c"}),"Binding delimiters cannot broaden target identity");
        var leases=new WorkReservations();
        check(leases.Acquire("one",new[]{"machine","input","capacity"}),"First worker reserves all requirements");
        check(!leases.Acquire("two",new[]{"another-machine","capacity"}),"Competing worker cannot overbook destination");
        check(leases.Available("another-machine","three"),"Failed multi-resource acquisition leaves no partial lease");
        leases.Release("two");
        check(!leases.Available("input","two"),"Cancellation cannot release another worker's input");
        leases.Release("one");
        check(leases.Acquire("two",new[]{"machine","input","capacity"}),"Cancellation releases own complete reservation");
        check(Near(CrewBalance.Credit(0,20*3600,false),100),"Twenty practical hours qualify");
        check(Near(CrewBalance.Credit(0,10*3600,true),100),"Ten study hours qualify");
        check(Near(CrewBalance.Credit(CrewBalance.Credit(0,10*3600,false),5*3600,true),100),"Practice and study combine proportionally");
        check(Near(CrewBalance.Credit(95,3600,true),100),"Training is capped");
        check(CrewBalance.Credit(10,0,false)==10&&CrewBalance.Credit(10,double.NaN,false)==10,"No idle or invalid credit");
        check(Near(CrewBalance.Duration(900,true),720)&&CrewBalance.Duration(900,false)==900,"Proficiency changes hands-on time only");
        foreach(int hours in new[]{1,6})
        {
            var budget=new CrewTimeBudget(hours*3600);
            double rest=hours*1200,travel=hours*100,work=hours*1000,study=hours*500;
            check(budget.TrySpend(rest,false)&&budget.TrySpend(travel,true)&&budget.TrySpend(work,true)&&budget.TrySpend(study,false),"Mixed skip fits one crew budget");
            double repairs=budget.Available;
            check(budget.TrySpend(repairs,false)&&budget.Available==0&&!budget.TrySpend(1,true),"Repairs consume only remaining minutes");
            check(Near(rest+travel+work+study+repairs,hours*3600)&&Near(budget.Worked,travel+work),"One and six hour skips cannot double-book a minute");
        }
        check(CrewBalance.UntilHour(15*3600+43*60)==17*60,"Skip splits at real roster boundary");
        check(CrewBalance.UntilHour(16*3600)==3600,"Exact boundaries do not produce zero-length steps");
        var interrupted=new CrewTimeBudget(3600);interrupted.TrySpend(30,true);
        check(interrupted.Available==3570,"Interrupted work still costs elapsed time without completion credit");
    }
}
