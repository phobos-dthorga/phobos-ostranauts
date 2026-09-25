using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
internal static class FluidNetworkChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var maps=new Dictionary<string,Dictionary<string,string>>();var bank=new PortBank("pump","example.Water",maps,8);
        var store=new Phobos.Ostranauts.Framework.Persistence.ObjectStateStore(new(),"FluidLine","test",1);
        check(store.TryWrite(new FluidLine().Save()),"Empty line records meet the native property's nonempty value contract");
        check(store.Read(out var emptyFields)==Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Ready&&FluidLine.Read(emptyFields).Empty,"Empty marker round-trips without creating contents");
        var receivers=new List<MaterialPort>();
        for(int n=0;n<8;n++){var r=new MaterialPort("rack"+n,"example.Water",new());receivers.Add(r);check(bank.TryLink(r,out _),"Fan-out accepts eight distinct reciprocal receivers");}
        check(bank.Ports[0].PortId=="example.Water","Original single-pair slot remains unchanged");
        check(!bank.TryLink(new MaterialPort("ninth","example.Water",new()),out _),"Ninth rack cannot overrun bounded fan-out");
        var reloaded=new PortBank("pump","example.Water",maps,8);
        check(reloaded.ForReceiver(receivers[6])!=null,"Reload retains exact logical peer port");
        PortPairing.Unlink(receivers[3],bank.ForReceiver(receivers[3]));
        check(bank.ForReceiver(receivers[3])==null&&bank.ForReceiver(receivers[4])!=null,"Unpairing one rack preserves other branches");
        double allocated=0;for(int n=0;n<8;n++)allocated+=HydraulicRoute.Share(.4,8);
        check(Math.Abs(allocated-.4)<1e-9,"Parallel branches share, not multiply, the pump budget");
        check(HydraulicRoute.FlowFraction(64,16)<HydraulicRoute.FlowFraction(1,16),"Longer route reduces bounded flow");
        var line=new FluidLine();line.Configure("saved-route","feed",.1,5);line.SetQuantity(new(.099,.001));line.Advance(2);
        var copy=FluidLine.Read(line.Save());check(Math.Abs(copy.TotalKg-.1)<1e-9&&copy.RemainingSeconds==3,"Reload preserves line components and remaining delay");
        check(store.TryWrite(line.Save()),"Filled line records remain writable through the real Framework store");
        bool refused=false;try{copy.Configure("other-route","feed",.1,5);}catch{refused=true;}
        check(refused&&copy.TotalKg==line.TotalKg,"Rerouting never deletes trapped fluid");
        copy.Advance(3);check(copy.Ready,"Finite powered time completes transit");copy.SetQuantity(default);copy.Configure("new-route","feed",.2,10);copy.SetQuantity(new(.1,0));
        check(copy.RemainingSeconds==10,"A new parcel cannot borrow the previous parcel's transit credit");
        var future=line.Save();future["extra"]="unknown";refused=false;try{FluidLine.Read(future);}catch{refused=true;}check(refused,"Unknown line schema fields fail closed");
    }
}
