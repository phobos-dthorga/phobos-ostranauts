using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

internal static class RoutingChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var ids = new[] { "Residue" }; var filter = new ItemDefinitionFilter(ids); ids[0] = "Valuable";
        check(filter.Allows("Residue") && !filter.Allows("Valuable") && !filter.Allows("residue") && !filter.Allows(null), "Exact filter snapshots definitions, not display names or case-folded guesses");
        check(!new ItemDefinitionFilter(Array.Empty<string>()).Allows("Residue"), "Empty filter accepts nothing");
        var floor = Enumerable.Range(0, 25).ToHashSet();
        var goals = new HashSet<int> { 24 };
        var path = GridRoute.Find(5, 5, new[] { 0 }, goals, floor.Contains)!;
        check(path.Length == 9 && path[0] == 0 && path.Last() == 24, "Shortest cardinal route includes endpoints");
        check(path.Zip(path.Skip(1)).All(p => Math.Abs(p.First-p.Second)==5 || p.First/5==p.Second/5 && Math.Abs(p.First-p.Second)==1), "Every route edge is a cardinal neighbour");
        floor.ExceptWith(new[] { 10,11,12,13,14 });
        check(GridRoute.Find(5,5,new[] { 0 },goals,floor.Contains)==null, "Hull separation cannot bridge missing floors");
        floor.Add(12);
        check(GridRoute.Find(5,5,new[] { 0 },goals,floor.Contains)!.Contains(12), "A narrow continuous connection supplies the route");
        check(GridRoute.Find(5,5,new[] { 4 },new HashSet<int> {5},i => i==4 || i==5)==null, "Adjacent array indices do not wrap across grid edges");
        check(GridRoute.Find(2,2,new[] {0},new HashSet<int> {3},i=>i==0 || i==3)==null, "Diagonal contact alone is disconnected");
        check(GridRoute.Find(5,5,new[] {-1,30,0},goals,_=>true,2)==null, "Search budget and invalid starts are bounded");
        check(GridRoute.Find(5,5,new[] {24},goals,_=>true,1)!.SequenceEqual(new[]{24}), "Destination already under source needs one valid cell");
        check(GridRoute.Find(5,5,new[] {24},goals,_=>false)==null, "Even the shared endpoint requires infrastructure");
        var clock = new TransferClock("cargo-A",5);
        check(clock.Advance("cargo-A",2,true) && clock.Progress==2,"Active powered transfer earns progress");
        check(clock.Advance("cargo-A",30,false) && clock.Progress==2,"Blocked/unpowered interval earns no work");
        check(!clock.Advance("cargo-B",3,true) && clock.Progress==2,"A competing removal/replacement cannot inherit progress");
        check(clock.Advance("cargo-A",3,true) && clock.Complete,"Retained work resumes without changing the item");
    }
}
