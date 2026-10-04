using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosManufacturing.Core;

/// <summary>The worked example add-on in <c>examples/addons/PhobosExampleRicherGangue</c> (Framework 0.90.0), loaded over
/// the shipped packs exactly as the game loads an enabled add-on: its recipe joins the LC-3's catalog under a derived
/// revision, its table change applies, and taking the add-on away puts the shipped odds back.</summary>
internal static class ExampleAddOnChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string? root = AppContext.BaseDirectory;
        while (root != null && !Directory.Exists(Path.Combine(root, "examples", "addons"))) root = Path.GetDirectoryName(root);
        check(root != null, "The examples folder is found from the test's own folder");
        if (root == null) return;
        string example = Path.Combine(root, "examples", "addons", "PhobosExampleRicherGangue"), template = Path.Combine(root, "examples", "addons", "PhobosAddOnTemplate");
        check(AddOns.ParseManifest(File.ReadAllText(Path.Combine(template, AddOns.ManifestFile))).idPrefix == "myaddon", "The template's manifest is valid as it ships");
        var saved = AddOns.EnabledModDirectories;
        try
        {
            AddOns.EnabledModDirectories = () => new[] { example };
            AddOns.Reset();
            int problems = DataPacks.Problems.Count;
            Materials.Load(); ChargeCatalog.Load(); ChargeOutcomes.Load();
            check(AddOns.Current.Count == 1 && AddOns.Current[0].Manifest.id == "example-richer-gangue" && AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems,
                "The example add-on is found and every file of it is accepted");
            var leach = ChargeCatalog.For(ChargeCatalog.Leach);
            var seam = leach.ById("richergangue-steel-seam");
            check(seam != null && seam.Revision == RecipeSchema.DerivedRevision("richergangue-steel-seam") && Math.Abs(seam.ChargeKg - 13) < 1e-9 && Math.Abs(seam.Products.Sum(p => p.Kg * p.Count) - 13) < 1e-9,
                "Its recipe joins the LC-3's catalog under a derived revision, and balances");
            // Its own item (Manufacturing 0.45.0): added after ours, with its own name and picture, and used by its recipes.
            var chunk = Materials.ById("RichergangueSeamChunk");
            check(chunk != null && chunk.Added && chunk.Kg == 4 && chunk.Name == "Seam Chunk" && chunk.Image == "richergangue/SeamChunk" && !chunk.Terminal &&
                  Materials.All.Count == Materials.Ids.Count + 1 && Materials.IsAdded("RichergangueSeamChunk") && !Materials.IsAdded(Materials.CarbonStock),
                "Its item joins the materials after ours, with its own name and picture");
            check(seam!.Products.Any(p => p.Id == "RichergangueSeamChunk" && p.Count == 1 && p.Kg == 4), "Its outcome yields its own item");
            var breaking = ChargeCatalog.For(ChargeCatalog.Refinery).ById("richergangue-chunk-break");
            check(breaking != null && breaking.Revision == RecipeSchema.DerivedRevision("richergangue-chunk-break") && Math.Abs(breaking.ChargeKg - 4) < 1e-9 &&
                  ChargeCatalog.For(ChargeCatalog.Refinery).FeedIds.Contains("RichergangueSeamChunk"), "A V4 recipe of the add-on takes its item, so the item has a use");
            var table = ChargeOutcomes.Pack.tables["gangue-wash"].outcomes;
            check(table["gangue-wash"] == 30 && table["richergangue-steel-seam"] == 10 && table["gangue-wash-steel"] == 30 && table.Count == 5, "Its table change retunes one shipped weight and adds its own outcome");
            check(ChargeOutcomes.IsHidden("richergangue-steel-seam") && ChargeOutcomes.BaseOf("richergangue-steel-seam") == "gangue-wash" && !leach.Available(_ => true).Any(r => r.Id == "richergangue-steel-seam"),
                "The added outcome is reached only through the gangue wash");
            var wash = leach.ById("gangue-wash")!;
            check(Enumerable.Range(0, 400).Any(i => ChargeOutcomes.Resolve(wash, new[] { "lump" + i, "a", "b", "c" }).Id == "richergangue-steel-seam"), "A wash can turn out as the add-on's outcome");
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            Materials.Load(); ChargeCatalog.Load(); ChargeOutcomes.Load();
        }
        check(Materials.ById("RichergangueSeamChunk") == null && Materials.All.Count == Materials.Ids.Count && ChargeCatalog.For(ChargeCatalog.Leach).ById("richergangue-steel-seam") == null && ChargeOutcomes.Pack.tables["gangue-wash"].outcomes["gangue-wash"] == 50, "Without the add-on the shipped recipes and odds stand");
    }
}
