using System;
using System.IO;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

/// <summary>The worked example add-on built into native definitions over the game's real data (Framework 0.92.0,
/// Manufacturing 0.45.0): its item becomes a real object with its own picture, its recipes pass the loader with the
/// game's own masses, and the machines admit the item. Nothing is published; the shipped packs are put back.</summary>
internal static class AddOnNativeChecks
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        string example = Path.Combine(repo, "examples", "addons", "PhobosExampleRicherGangue");
        var saved = AddOns.EnabledModDirectories;
        try
        {
            AddOns.EnabledModDirectories = () => new[] { example };
            AddOns.Reset();
            int problems = DataPacks.Problems.Count;
            var d = PhobosManufacturing.Content.Prepare(true);
            check(AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems, "The example add-on loads over the game's real data with no file refused: " +
                string.Join("; ", DataPacks.Problems.Skip(problems).Select(p => p.File + ": " + p.Message)));
            const string id = "RichergangueSeamChunk";
            check(d.Objects.TryGetValue(id, out var chunk) && d.Items.TryGetValue(id, out var item) && item.strImg == "richergangue/SeamChunk" && item.strImgNorm == "richergangue/SeamChunkNormal" &&
                  chunk.strNameFriendly == "Seam Chunk" && chunk.nStackLimit == 6 && chunk.aStartingConds.Contains(id + "Identity=1x1") && chunk.aStartingConds.Contains("IsCategoryMetals=1x1") &&
                  chunk.aStartingConds.Any(s => s == "StatMass=1x4"), "Its item is a real object: named, 4 kg, stacking six, with its own picture from the add-on's images folder");
            check(File.Exists(Path.Combine(example, "images", "richergangue", "SeamChunk.png")) && File.Exists(Path.Combine(example, "images", "richergangue", "SeamChunkNormal.png")),
                "The picture and its normal map are in the add-on, where the game's own image loader looks in every enabled mod");
            check(!Remainders.IsDeclared(id) && Materials.IsAdded(id), "An added item that is not trash is not a remainder");
            var feed = d.Triggers[RefineryRules.StockTrigger];
            check(feed.aReqs.Contains(id + "Identity"), "The V4's feed rule admits the added item its recipe takes");
            check(ChargeCatalog.For(ChargeCatalog.Refinery).ById("richergangue-chunk-break") is ChargeRecipe r && r.ItemInputs.Single().Kg == 4 &&
                  ChargeCatalog.For(ChargeCatalog.Leach).ById("richergangue-steel-seam") != null, "Its recipes pass the loader against the game's own item masses");
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            PhobosManufacturing.Content.Prepare(true);
        }
        check(Materials.ById("RichergangueSeamChunk") == null, "Without the add-on the shipped materials stand");
    }
}
