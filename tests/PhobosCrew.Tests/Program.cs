using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;

// Production roster adapter linked against minimal native boundary doubles. The native
// suite separately verifies the real API contract; this is not a Unity game session.
int checks=0;
void Check(bool value,string message) { if(!value)throw new Exception(message); checks++; }
Check(CrewRoster.Members().Length==0,"No captain during loading is safe");
CrewSim.coPlayer=new();
Check(CrewRoster.Members().Length==0,"No company during loading is safe");
var company=new JsonCompany(); CrewSim.coPlayer.Company=company;
DataHandler.mapCOs=new();
var first=new CondOwner{ship=new(),aQueue=new()}; var second=new CondOwner{ship=new(),aQueue=new()};
foreach(var entry in new Dictionary<string,CondOwner?>{
    ["first"]=first,["second"]=second,["null"]=null,
    ["destroyed"]=new(){ship=new(),aQueue=new(),bDestroyed=true},
    ["unloaded"]=new(){aQueue=new()},["loading-queue"]=new(){ship=new()}})
{
    company.mapRoster!.Add(entry.Key,new()); DataHandler.mapCOs.Add(entry.Key,entry.Value);
}
company.mapRoster!.Add("missing-save",new());
Check(CrewSim.aCrew==null,"Legacy crew list is absent in the reported game scenario");
Check(CrewRoster.Members().SequenceEqual(new[]{first,second}),"Missing, null, destroyed and uninitialized members do not prevent valid crew discovery");
Check(company.Reads==1,"Resolver delegates to native company API");
Check(company.mapRoster.ContainsKey("missing-save")&&DataHandler.mapCOs.ContainsKey("null"),"Discovery never repairs or deletes save identities");
company.mapRoster.Remove("first");
Check(CrewRoster.Members().SequenceEqual(new[]{second}),"Dismissal takes effect on next snapshot");
first.ship=new(); first.aQueue=new(); company.mapRoster.Add("first",new());
Check(CrewRoster.Members().Contains(first),"Reloaded or newly hired crew can participate");
company.mapRoster=null;
Check(CrewRoster.Members().Length==0,"Company teardown is safe");
company.mapRoster=new(); DataHandler.mapCOs=null;
Check(CrewRoster.Members().Length==0,"World object teardown is safe");
var own=new Ship(); company.mapRoster=new(){{"first",new()}}; DataHandler.mapCOs=new(){{"first",first}};
first.ship=own;
Check(CrewRoster.AllAboard(own),"Departure uses the company roster while the legacy list is absent");
company.mapRoster.Add("missing-save",new());
Check(!CrewRoster.AllAboard(own),"Missing saved crew cannot be ignored for departure");
company.mapRoster.Remove("missing-save"); first.ship=new();
Check(!CrewRoster.AllAboard(own),"Crew on another ship blocks departure");
first.ship=null;
Check(!CrewRoster.AllAboard(own),"Unloaded crew blocks departure");
first.ship=own; first.bDestroyed=true;
Check(!CrewRoster.AllAboard(own),"Destroyed roster member requires native roster resolution before departure");
first.bDestroyed=false; DataHandler.mapCOs["first"]=null;
Check(!CrewRoster.AllAboard(own),"Null roster object blocks departure");
company.mapRoster.Clear();
Check(!CrewRoster.AllAboard(own),"Unavailable or empty roster cannot authorize departure");
Console.WriteLine($"PASS: {checks} crew roster adapter checks with native boundary doubles.");

public sealed class CondOwner
{
    public JsonCompany? Company;
    public Ship? ship;
    public List<object>? aQueue;
    public bool bDestroyed;
}
public sealed class Ship { }
public static class CrewSim
{
    public static CondOwner? coPlayer;
    public static List<CondOwner>? aCrew;
}
public static class DataHandler { public static Dictionary<string,CondOwner?>? mapCOs; }
public sealed class JsonCompany
{
    public Dictionary<string,object>? mapRoster=new();
    public int Reads;
    public List<CondOwner> GetCrewMembers(CondOwner? excludedCO=null)
    {
        Reads++;
        return mapRoster!.Keys.Where(id=>DataHandler.mapCOs!.ContainsKey(id)).Select(id=>DataHandler.mapCOs![id]!).ToList();
    }
}
