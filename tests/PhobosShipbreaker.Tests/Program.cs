using Phobos.Ostranauts.Framework.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Inventory;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
void Throws(Action action, string message)
{
    bool failed = false;
    try { action(); } catch { failed = true; }
    Check(failed, message);
}

FurnaceChecks.Run(Check);
FurnaceMaterialChecks.Run(Check);
var recipeV1 = ProcessRecipes.WallPanels.Current;
var masses = recipeV1.Products.SelectMany(p => Enumerable.Repeat(p.Kg, p.Count)).ToArray();
Check(ProcessRules.Balanced(24, masses), "Recipe conserves all 24 kg including residue");
Check(!ProcessRules.Balanced(24, masses.Take(masses.Length - 1)), "Missing residue is not silently discarded");
Check(!ProcessRules.Balanced(24, masses.Concat(new[] { 0.5 })), "Extra product mass rejected");
Check(!ProcessRules.Balanced(24, new[] { double.NaN }), "Invalid mass rejected");
Check(!ProcessRules.Balanced(24, new[] { 25.0, -1 }), "Negative waste cannot hide excess output");

// Check the shipped recipe data against Phobos Framework's craft-size limit.
// This catches registration failures which compiling our own plugin cannot find.
using var recipePack = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "shipbreaker-recipes.json")));
var constructionRecipes = recipePack.RootElement.GetProperty("recipes").EnumerateArray().ToArray();
foreach (var recipe in constructionRecipes)
foreach (string side in new[] { "ingredients", "outputs" })
{
    var counts = recipe.GetProperty(side).EnumerateArray().Select(p => p.GetProperty("count").GetInt32()).ToArray();
    Check(counts.All(c => c > 0) && counts.Sum() <= 100,
        recipe.GetProperty("id").GetString() + " exceeds Phobos Framework's 100-unit " + side + " limit");
}

var sectionRecipe = constructionRecipes.Single(r => r.GetProperty("id").GetString() == "PhobosBuildShipbreakerSection");
var finalRecipe = constructionRecipes.Single(r => r.GetProperty("id").GetString() == "PhobosBuildShipbreaker");
var finalInputs = finalRecipe.GetProperty("ingredients").EnumerateArray().ToArray();
Check(finalInputs.Length == 1 && finalInputs[0].GetProperty("item").GetString() == "PhobosShipbreakerSection"
    && finalInputs[0].GetProperty("count").GetInt32() == 2, "Final assembly consumes exactly two sections");
var expectedBill = new Dictionary<string, (int count, double kg)> {
    ["ItmScrapSteel"] = (100, 1), ["ItmScrapAluminum"] = (48, 1),
    ["ItmPartsMechSmall01"] = (20, 0.5), ["ItmPartsElecSmall01"] = (4, 0.5)
}; // Audited native unit masses; runtime-modified definitions require a fresh audit.
var sectionInputs = sectionRecipe.GetProperty("ingredients").EnumerateArray().ToArray();
Check(sectionInputs.Length == expectedBill.Count, "Construction has no unaccounted ingredient family");
double sectionMass = 0;
foreach (var ingredient in sectionInputs)
{
    var expected = expectedBill[ingredient.GetProperty("item").GetString()!];
    int count = ingredient.GetProperty("count").GetInt32();
    Check(count * 2 == expected.count, "Two sections preserve the original bill of materials");
    sectionMass += count * expected.kg;
}
Check(ProcessRules.MassMatches(sectionMass * 2, ProcessRules.MachineKg), "Construction retains the full 160 kg");
Check(ProcessRules.MassMatches(sectionMass, ProcessRules.AssemblySectionKg), "Section object mass equals its material inputs");
var sectionOutput = sectionRecipe.GetProperty("outputs").EnumerateArray().Single();
Check(sectionOutput.GetProperty("item").GetString() == ProcessRules.AssemblySection && sectionOutput.GetProperty("count").GetInt32() == 1,
    "A section craft produces exactly one physical section");
var machineOutput = finalRecipe.GetProperty("outputs").EnumerateArray().Single();
Check(machineOutput.GetProperty("item").GetString() == "PhobosShipbreakerLoose" && machineOutput.GetProperty("count").GetInt32() == 1,
    "Final assembly produces one fixture with its existing saved identity");
Check(sectionRecipe.GetProperty("workSeconds").GetInt32() * 2 + finalRecipe.GetProperty("workSeconds").GetInt32() == 9000,
    "Full equipment fabrication includes both sections and final assembly: 150 minutes");

var job = new ProcessJob("panel-A", 0, recipeV1, ProcessRules.CycleSeconds);
job.Advance("panel-A", 15, true, true);
job.Advance("panel-A", 20, false, true);
Check(job.Progress == 15, "Blackout time earns no work");
job.Advance("panel-A", 15, true, true);
Check(job.Progress == 30, "Restoration does not credit the blackout");
job.Advance("panel-B", 10, true, true);
Check(!job.Running && job.Progress == 30, "Swapping the input pauses without transferring work");
var resumed = new ProcessJob("panel-A", job.Progress, recipeV1, ProcessRules.CycleSeconds);
Check(resumed.Advance("panel-A", 40, true, true) == 30 && resumed.Complete, "Resume uses saved progress and caps final credit");
Check(resumed.Advance("panel-A", 5, true, true) == 0, "Finished work cannot earn more progress");
var fresh = new ProcessJob("panel-B", 0, recipeV1, ProcessRules.CycleSeconds);
Check(fresh.Progress == 0, "Different panel begins at zero");
fresh.Pause();
fresh.Advance("panel-B", 10, true, true);
Check(fresh.Progress == 0, "Manual pause prevents work");
foreach (double delta in new[] { -1.0, 61, double.NaN, double.PositiveInfinity })
{
    var invalid = new ProcessJob("A", 12, recipeV1, ProcessRules.CycleSeconds);
    invalid.Advance("A", delta, true, true);
    Check(invalid.Progress == 12 && !invalid.Running, "Unobserved time gap pauses: " + delta);
}
Throws(() => ProcessJob.CreateOrResume(ProcessRecipes.WallPanels, "A", 12, 99, 60, 60), "Unknown saved recipe revision rejected");
Throws(() => new ProcessJob("A", 61, recipeV1, ProcessRules.CycleSeconds), "Invalid saved progress rejected");
var customDuration = new ProcessJob("custom", 45, recipeV1, 90);
customDuration.Advance("custom", 20, true, true);
Check(customDuration.Progress == 65 && !customDuration.Complete, "Custom job does not complete at the default 60 seconds");
var customResumed = new ProcessJob("custom", customDuration.Progress, recipeV1, customDuration.Duration);
Check(customResumed.Duration == 90 && customResumed.Progress == 65, "Saved duration and progress survive resume independently of new-job settings");
customResumed.Advance("custom", 30, true, true);
Check(customResumed.Progress == 90 && customResumed.Complete, "Custom duration caps completion correctly");
Throws(() => new ProcessJob("A", 0, recipeV1, double.NaN), "Corrupt saved duration rejected");
Throws(() => new ProcessJob("A", 0, recipeV1, 0), "Zero-duration job rejected");
RecipeChecks.Run(Check, Throws);
ReclaimerChecks.Run(Check, Throws);

var empty = new bool[8, 8];
var outputSizes = masses.Select(_ => new ItemSize(1, 1)).ToArray();
Check(BatchPlacement.Plan(empty, outputSizes) != null, "Complete batch fits");
Check(!empty.Cast<bool>().Any(v => v), "Planning does not mutate the real tray");
Check(BatchPlacement.Plan(new bool[2, 2], outputSizes) == null, "Batch cannot fit by independently reusing cells");
Check(BatchPlacement.Plan(empty, new[] { new ItemSize(9, 1) }) == null, "Oversized item rejected");

// Independently check reservation invariants over occupied trays and mixed sizes.
var random = new Random(7612);
for (int sample = 0; sample < 300; sample++)
{
    var occupied = new bool[8, 8];
    for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++) occupied[x, y] = random.Next(4) == 0;
    var sizes = Enumerable.Range(0, 6).Select(_ => new ItemSize(random.Next(1, 4), random.Next(1, 4))).ToArray();
    var plan = BatchPlacement.Plan(occupied, sizes);
    if (plan == null) continue;
    var reserved = new HashSet<(int, int)>();
    for (int i = 0; i < plan.Length; i++)
    for (int x = plan[i].X; x < plan[i].X + sizes[i].Width; x++)
    for (int y = plan[i].Y; y < plan[i].Y + sizes[i].Height; y++)
        Check(x >= 0 && y >= 0 && x < 8 && y < 8 && !occupied[x, y] && reserved.Add((x, y)),
            "Reserved cells must be in bounds, unoccupied and disjoint");
}

var blocked = new FakeDelivery { Fits = false };
Check(BatchDelivery.Commit(blocked) == DeliveryResult.Blocked && blocked.InputPresent && blocked.Products == 0,
    "Full tray preserves input and removes staged products");
for (int fault = 0; fault < masses.Length; fault++)
{
    var partial = new FakeDelivery { FailAtProduct = fault };
    Throws(() => BatchDelivery.Commit(partial), "Insertion failure surfaces");
    Check(partial.InputPresent && partial.Products == 0 && partial.Staged == 0, "Partial insertion fully rolls back");
}
var success = new FakeDelivery();
Check(BatchDelivery.Commit(success) == DeliveryResult.Completed && !success.InputPresent && success.Products == masses.Length,
    "Successful delivery replaces exactly one input with one whole batch");
BatchDelivery.Commit(success);
Check(success.Products == masses.Length && success.PrepareCount == 1, "Repeated completion does not duplicate or remove products");
var interruptedAfterConsume = new FakeDelivery { FailAfterConsume = true };
Throws(() => BatchDelivery.Commit(interruptedAfterConsume), "Post-consumption error surfaces for pausing");
Check(!interruptedAfterConsume.InputPresent && interruptedAfterConsume.Products == masses.Length,
    "A late exception cannot roll back the only surviving material");
foreach (string foreign in new[] { "", "spawn PhobosShipbreakerLoose", "phobosapproach status", "phobosshipbreakers start" })
    Check(Command.Parse(foreign).Action == CommandAction.Foreign, "Foreign console command passes through: " + foreign);
Check(Command.Parse("phobosshipbreaker").Action == CommandAction.Help, "Bare command shows help");
var targeted = Command.Parse("  PHOBOSSHIPBREAKER\tStArT fixture-123  ");
Check(targeted.Action == CommandAction.Start && targeted.TargetId == "fixture-123", "Target identity and mixed case survive parsing");
Check(Command.Parse("phobosshipbreaker pause").TargetId == null, "Omitted target remains explicit for ambiguity checks");
foreach (string invalidCommand in new[] { "phobosshipbreaker cancel A B", "phobosshipbreaker settings 20", "phobosshipbreaker help A", "phobosshipbreaker typo" })
    Check(Command.Parse(invalidCommand).Action == CommandAction.Invalid, "Reject malformed command without unintended action: " + invalidCommand);
DependencyChecks.Run(Check, Throws);
PortRoutingChecks.Run(Check);
IntakeChecks.Run(Check, Throws, constructionRecipes);
CollectorChecks.Run(Check, constructionRecipes);
Console.WriteLine($"PASS: {checks} checks of dependency contracts/registration rollback, construction limits/mass, material balance, work identity, interruption, batch placement, completion recovery and console routing.");

sealed class FakeDelivery : IBatchDelivery
{
    public ProcessRecipe Recipe = ProcessRecipes.WallPanels.Current;
    public readonly List<string> ProductIds = new();
    private string[] stagedIds = Array.Empty<string>();
    public bool Fits = true, InputPresent = true, FailAfterConsume;
    public int FailAtProduct = -1, Products, Staged, PrepareCount;
    public bool InputConsumed => !InputPresent;
    public bool Prepare()
    {
        PrepareCount++;
        stagedIds = Recipe.Products.SelectMany(p => Enumerable.Repeat(p.Id, p.Count)).ToArray();
        Staged = stagedIds.Length;
        return Fits;
    }
    public void PlaceProducts()
    {
        int count = Staged;
        for (int i = 0; i < count; i++)
        {
            if (i == FailAtProduct) throw new InvalidOperationException("Injected insertion fault");
            ProductIds.Add(stagedIds[i]); Products++; Staged--;
        }
    }
    public void ConsumeInput()
    {
        InputPresent = false;
        if (FailAfterConsume) throw new InvalidOperationException("Injected late fault");
    }
    public void RollbackProducts() { Products = 0; Staged = 0; ProductIds.Clear(); }
}
