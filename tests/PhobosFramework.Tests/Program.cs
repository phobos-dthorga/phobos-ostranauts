using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

LocalizationChecks.Run(Check);

// Consume the built public assembly, without compiling private copies of its code.
Check(typeof(BatchPlacement).Assembly.GetName().Name == "PhobosFramework", "Consumer uses the shared assembly");
Check(typeof(DefinitionTransaction).Assembly == typeof(BatchPlacement).Assembly, "Both services have one provider");
var occupied = new bool[4, 2];
occupied[0, 0] = true;
var placements = BatchPlacement.Plan(occupied, new[] { new ItemSize(2, 2) });
Check(placements != null && placements[0].X == 1 && placements[0].Y == 0, "Public planner respects occupied cells");
Check(!occupied[1, 0], "Plans do not publish reservations into live inventories");

// A failing recovery must not prevent attempts to restore the other tables.
var first = new Dictionary<string, int> { ["existing"] = 10 };
var failure = new FailingDictionary();
var registration = new DefinitionTransaction();
registration.Stage(first, new Dictionary<string, int> { ["existing"] = 11, ["new"] = 12 });
registration.Stage((IDictionary<string, int>)failure, new Dictionary<string, int> { ["existing"] = 21 });
bool aggregate = false;
try { registration.Commit(); }
catch (AggregateException ex) { aggregate = ex.InnerExceptions.Count == 2; }
Check(aggregate, "Initial write and recovery failures are both reported");
Check(first.Count == 1 && first["existing"] == 10, "Other tables recover despite a rollback failure");
ConstructionChecks.Run(Check);
TransferChecks.Run(Check);
RoutingChecks.Run(Check);
PairingChecks.Run(Check);
Console.WriteLine($"PASS: {checks} public-assembly and recovery checks. Shipbreaker tests also exercise this compiled provider.");

sealed class FailingDictionary : Dictionary<string, int>, IDictionary<string, int>
{
    public FailingDictionary() { base["existing"] = 20; }
    int IDictionary<string, int>.this[string key]
    {
        get => base[key];
        set { base[key] = value; throw new InvalidOperationException("Write/recovery fault"); }
    }
}
