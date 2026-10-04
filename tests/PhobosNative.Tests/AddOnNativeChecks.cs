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
        Dockside(repo, check);
    }

    /// <summary>The second worked example (Shipbreaker 0.75.0, Agriculture 0.48.0): an F6 casting and a Hearth-2 meal,
    /// each built into a real object with its own picture.</summary>
    private static void Dockside(string repo, Action<bool, string> check)
    {
        string example = Path.Combine(repo, "examples", "addons", "PhobosExampleDocksideExtras");
        const string cleat = "DocksideMooringCleat", stew = "DocksideStewedTomatoes";
        var saved = AddOns.EnabledModDirectories; var folders = Phobos.Ostranauts.Framework.Localization.Translations.AddOnDirectories;
        try
        {
            AddOns.EnabledModDirectories = () => new[] { example };
            AddOns.Reset();
            // The game reads add-on text when content loads; here that step is taken by hand, as Framework's plugin wires it.
            Phobos.Ostranauts.Framework.Localization.Translations.AddOnDirectories = AddOns.TranslationFolders;
            Phobos.Ostranauts.Framework.Localization.Translations.Reload();
            int problems = DataPacks.Problems.Count;
            var s = PhobosShipbreaker.Content.Prepare();
            var a = PhobosAgriculture.Definitions.Prepare();
            check(AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems, "The dockside example loads over the game's real data with no file refused: " +
                string.Join("; ", DataPacks.Problems.Skip(problems).Select(p => p.File + ": " + p.Message)));
            check(s.Objects.TryGetValue(cleat, out var cast) && s.Items.TryGetValue(cleat, out var castItem) && castItem.strImg == "dockside/MooringCleat" && castItem.strImgNorm == "dockside/MooringCleatNormal" &&
                  cast.strPortraitImg == "dockside/MooringCleat" && cast.strNameFriendly == "Mooring Cleat" && cast.nStackLimit == 4 && cast.aStartingConds.Contains(cleat + "Identity=1x1") &&
                  cast.aStartingConds.Contains("IsCategoryMetals=1x1") && cast.aStartingConds.Any(c => c.StartsWith("StatMass=", StringComparison.Ordinal) && c.EndsWith("x5", StringComparison.Ordinal)) &&
                  s.Conditions.ContainsKey(cleat + "Identity") && !Remainders.IsDeclared(cleat), "Shipbreaker builds the cleat as a real object: named, 5 kg, stacking four, with its own picture, and no remainder");
            var recipe = PhobosShipbreaker.Core.FurnaceRecipes.ById("dockside-mooring-cleats");
            check(recipe != null && PhobosShipbreaker.Core.FurnaceRecipes.Label(recipe) == "Mooring cleats (twenty aluminium)" && PhobosShipbreaker.Core.FurnaceMaterialRules.Product(cleat, 5),
                "Its F6 recipe passes the loader against the game's own scrap, and the add-on's text names it on the panel");
            check(a.Objects.TryGetValue(stew, out var meal) && a.Items.TryGetValue(stew, out var mealItem) && mealItem.strImg == "dockside/StewedTomatoes" && mealItem.strImgNorm == "dockside/StewedTomatoesNormal" &&
                  meal.strNameFriendly == "Stewed Tomatoes" && meal.nStackLimit == 10 && meal.aStartingConds.Contains("IsFood=1x1") && meal.aStartingConds.Count(c => c == "IsCategoryFood=1x1") == 1 &&
                  meal.aStartingConds.Contains("Is" + stew + "=1x1") && a.Loot.TryGetValue(stew + "Effects", out var effects) && effects.aCOs.Contains("TDnFood=1x2") && effects.aCOs.Contains("TUpSatiety=1x2") &&
                  a.Interactions.ContainsKey(stew + "AllowDirect"), "Agriculture builds the meal as real food: named, pictured, with its food values on the game's own eating chain");
            check(PhobosAgriculture.Core.HearthRecipes.ForInput("PhobosVerdemorrowTomatoes")?.Products.Single().Id == stew, "The Hearth-2 cooks tomatoes into it");
            foreach (string picture in new[] { "MooringCleat", "StewedTomatoes" })
                check(File.Exists(Path.Combine(example, "images", "dockside", picture + ".png")) && File.Exists(Path.Combine(example, "images", "dockside", picture + "Normal.png")), "The add-on carries the picture and normal map of " + picture);
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            Phobos.Ostranauts.Framework.Localization.Translations.AddOnDirectories = folders;
            Phobos.Ostranauts.Framework.Localization.Translations.Reload();
            PhobosShipbreaker.Content.Prepare(); PhobosAgriculture.Definitions.Prepare();
        }
        check(!PhobosShipbreaker.Core.ShipbreakerMaterials.IsAdded(cleat) && PhobosShipbreaker.Core.FurnaceRecipes.ById("dockside-mooring-cleats") == null &&
              PhobosAgriculture.Core.HearthRecipes.ForInput("PhobosVerdemorrowTomatoes") == null, "Without the add-on the shipped packs stand");
    }
}
