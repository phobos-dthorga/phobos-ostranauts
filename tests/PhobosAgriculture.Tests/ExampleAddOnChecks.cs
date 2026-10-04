using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosAgriculture;
using PhobosAgriculture.Core;

/// <summary>The worked example add-on in <c>examples/addons/PhobosExampleDocksideExtras</c> (Agriculture 0.48.0), loaded
/// over the shipped packs exactly as the game loads an enabled add-on: its meal joins the materials, its crop item entry
/// gives it food values without a text key, its Hearth-2 recipe cooks it, and taking the add-on away puts the shipped
/// packs back. A crop grown from added stock is checked from text, with the same loader.</summary>
internal static class ExampleAddOnChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string? root = AppContext.BaseDirectory;
        while (root != null && !Directory.Exists(Path.Combine(root, "examples", "addons"))) root = Path.GetDirectoryName(root);
        check(root != null, "The examples folder is found from the test's own folder");
        if (root == null) return;
        string example = Path.Combine(root, "examples", "addons", "PhobosExampleDocksideExtras");
        const string stew = "DocksideStewedTomatoes", tomatoes = "PhobosVerdemorrowTomatoes";
        int crops = Crops.All.Count, recipes = HearthRecipes.All.Count;
        var saved = AddOns.EnabledModDirectories;
        try
        {
            AddOns.EnabledModDirectories = () => new[] { example };
            AddOns.Reset();
            int problems = DataPacks.Problems.Count;
            AgricultureMaterials.Load(); Crops.Load(); HearthRecipes.Load();
            check(AddOns.Current.Count == 1 && AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems,
                "The example add-on is found and every Agriculture file of it is accepted: " + string.Join("; ", DataPacks.Problems.Skip(problems).Select(p => p.File + ": " + p.Message)));
            var added = AgricultureMaterials.Added;
            check(added.Count == 1 && added[0].Key == stew && added[0].Value.kind == AgricultureMaterials.Food && added[0].Value.name == "Stewed Tomatoes" && added[0].Value.image == "dockside/StewedTomatoes" &&
                  AgricultureMaterials.IsAdded(stew) && !AgricultureMaterials.IsAdded(tomatoes) && AgricultureMaterials.KgOf(stew) == .25, "Its meal joins the materials after ours, with its own name and picture");
            check(Crops.Pack.items.TryGetValue(stew, out var item) && item.text.Length == 0 && item.hunger == 2 && item.satiety == 2 && Crops.All.Count == crops,
                "Its crop item entry gives the meal its food values and needs no text key; the crops are unchanged");
            var cook = HearthRecipes.ForInput(tomatoes);
            check(cook != null && cook.Id == "dockside-stewed-tomatoes" && cook.Revision == RecipeSchema.DerivedRevision(cook.Id) && cook.Kg == .25 && cook.Products.Single() == (stew, .25) &&
                  HearthRecipes.IsProduct(stew) && HearthRecipes.All.Count == recipes + 1, "Its recipe cooks one portion of tomatoes into its meal, under a derived revision, mass for mass");
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            AgricultureMaterials.Load(); Crops.Load(); HearthRecipes.Load();
        }
        check(AgricultureMaterials.Added.Count == 0 && !Crops.Pack.items.ContainsKey(stew) && HearthRecipes.ForInput(tomatoes) == null && HearthRecipes.All.Count == recipes, "Without the add-on the shipped packs stand");

        // The crop item rule itself: an added item carries no text key, one of ours always does.
        var context = new CropContext { KnownItem = _ => true, KnownText = _ => true, KnownArt = _ => true, AddedItem = id => id == "MyaddonBeans" };
        CropPack Shipped() => DataPacks.Parse(DataPacks.ShippedText(Crops.Source), CropSchema.Name, true).ToObject<CropPack>()!;
        void Refused(Action<CropPack> change, string label)
        {
            var pack = Shipped(); change(pack);
            bool failed = false;
            try { CropSchema.Validate(pack, context); } catch (ArgumentException) { failed = true; }
            check(failed, label);
        }
        var grown = Shipped();
        grown.items["MyaddonBeans"] = new CropItemEntry { hunger = 1, satiety = 1 };
        var bean = DataPacks.Parse(DataPacks.ShippedText(Crops.Source), CropSchema.Name, true).ToObject<CropPack>()!.crops["soybean"];
        bean.produce = "MyaddonBeans"; bean.feed = "myaddon-bean-feed"; bean.feedCommodity = "myaddon bean feed"; bean.name = "Runner bean";
        grown.crops["myaddon-bean"] = bean;
        CropSchema.Validate(grown, context);
        check(new Crop("myaddon-bean", grown.crops["myaddon-bean"]).Produce == "MyaddonBeans", "A crop a file adds may grow an item the same file adds");
        Refused(p => p.items["MyaddonBeans"] = new CropItemEntry { text = "meal" }, "An added item's entry may not borrow one of our text keys");
        Refused(p => p.items[tomatoes].text = "", "One of our crop items keeps its text key");
    }
}
