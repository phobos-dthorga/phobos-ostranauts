using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosAgriculture;
using PhobosAgriculture.Core;

/// <summary>Agriculture 0.40.0: crops and cooker recipes are data packs. Everything a crop entry names must exist in
/// the prepared definitions and the package: its items, its planting action on the installed rack, its feed action on
/// the W2 and its growth-stage artwork.</summary>
internal static class CropNativeChecks
{
    private static readonly string[] Stages = { "sprout", "young", "mature", "harvest", "wilted", "dead" };
    internal static void Run(NativeDefinitions agriculture, string repo, Action<bool, string> check)
    {
        check(Crops.All.Count >= 3 && HearthRecipes.All.Count >= 1, "The crops and cooking recipes loaded from their packs");
        string images = Path.Combine(repo, "mods", "PhobosAgriculture", "images", "phobos", "agriculture");
        var rack = agriculture.Objects[Definitions.Rack + "Installed"];
        foreach (var crop in Crops.All)
        {
            check(agriculture.Objects.ContainsKey(crop.Stock) && agriculture.Objects.ContainsKey(crop.Produce), "The crop is planted from and harvested into items the mod builds: " + crop.Id);
            string plant = Definitions.WorkId(Definitions.PlantPrefix + crop.Id);
            check(agriculture.Interactions.ContainsKey(plant) && rack.aInteractions.Contains(plant) && Definitions.WorkIds[plant] == Definitions.PlantPrefix + crop.Id,
                "The installed rack offers the crop's planting job: " + crop.Id);
            check(Text.Has(Definitions.PlantPrefix + crop.Id) && Text.Has(Definitions.MixPrefix + crop.Id) && Text.Has("crew_recipe_" + crop.Id) && Text.Has("solution_" + crop.Feed),
                "The crop's planting, feed and crew order wording exists: " + crop.Id);
            foreach (string stage in Stages)
                foreach (string suffix in new[] { "", "Normal", "Damaged", "DamagedNormal" })
                    check(File.Exists(Path.Combine(images, "Rack-" + crop.Art + "-" + stage + suffix + ".png")), "Growth artwork ships: " + crop.Art + "-" + stage + suffix);
        }
        foreach (var item in Crops.Items)
        {
            check(agriculture.Objects.TryGetValue(item.Key, out var co) && co.strNameFriendly == Text.Get(item.Value.text), "The crop item is built with its own name: " + item.Key);
            bool food = item.Value.hunger != null;
            check(agriculture.Loot.ContainsKey(item.Key + "Effects") == food && agriculture.Interactions.ContainsKey(item.Key + "AllowDirect") == food, "Only food carries eating effects: " + item.Key);
        }
        foreach (var recipe in HearthRecipes.All)
            check(agriculture.Objects.ContainsKey(recipe.Input) && agriculture.Objects.ContainsKey(recipe.Product), "The cooker's recipe names items the mod builds: " + recipe.Id);
        check(Definitions.MixActions.Length == Crops.All.Count + 1 && Definitions.MixActions.Last() == "water-only", "The W2 offers one feed per crop, then plain water");
    }
}
