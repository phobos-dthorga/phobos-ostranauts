using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosShipbreaker.Core;

/// <summary>The worked example add-on in <c>examples/addons/PhobosExampleDocksideExtras</c> (Shipbreaker 0.75.0), loaded
/// over the shipped packs exactly as the game loads an enabled add-on: its item joins the materials as plain stock, its
/// F6 recipe joins the furnace catalog under a derived revision, and taking the add-on away puts the shipped packs back.</summary>
internal static class ExampleAddOnChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string? root = AppContext.BaseDirectory;
        while (root != null && !Directory.Exists(Path.Combine(root, "examples", "addons"))) root = Path.GetDirectoryName(root);
        check(root != null, "The examples folder is found from the test's own folder");
        if (root == null) return;
        string example = Path.Combine(root, "examples", "addons", "PhobosExampleDocksideExtras");
        const string cleat = "DocksideMooringCleat", recipe = "dockside-mooring-cleats";
        int shipped = FurnaceRecipes.All.Count;
        var saved = AddOns.EnabledModDirectories;
        try
        {
            AddOns.EnabledModDirectories = () => new[] { example };
            AddOns.Reset();
            int problems = DataPacks.Problems.Count;
            ShipbreakerMaterials.Load(); ShipbreakerRecipes.Load();
            check(AddOns.Current.Count == 1 && AddOns.Current[0].Manifest.id == "example-dockside-extras" && AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems,
                "The example add-on is found and every Shipbreaker file of it is accepted: " + string.Join("; ", DataPacks.Problems.Skip(problems).Select(p => p.File + ": " + p.Message)));
            var added = ShipbreakerMaterials.Added;
            check(added.Count == 1 && added[0].Key == cleat && added[0].Value.kind == ShipbreakerMaterials.Stock && added[0].Value.kg == 5 && added[0].Value.name == "Mooring Cleat" &&
                  added[0].Value.image == "dockside/MooringCleat" && ShipbreakerMaterials.IsAdded(cleat) && !ShipbreakerMaterials.IsAdded(FurnaceRecipes.AluminiumIngot) && ShipbreakerMaterials.KgOf(cleat) == 5,
                "Its item joins the materials after ours, as plain stock with its own name and picture");
            var cast = FurnaceRecipes.ById(recipe);
            check(cast != null && cast.Revision == RecipeSchema.DerivedRevision(recipe) && FurnaceRecipes.All.Count == shipped + 1 && cast.FeedId == FurnaceMaterialRules.Aluminium &&
                  ReferenceEquals(cast.Profile, FurnaceProfile.Aluminium) && cast.Products.Any(p => p.Id == cleat && p.Count == 3 && p.Kg == 5) && Math.Abs(cast.Products.Sum(p => p.Kg * p.Count) - 20) < 1e-9,
                "Its recipe joins the F6 catalog under a derived revision, on the aluminium profile, and balances");
            check(FurnaceMaterialRules.Product(cleat, 5) && FurnaceMaterialRules.IsProduct(cleat) && FurnaceRecipes.ByRevision(cast!.Revision) == cast,
                "The furnace's product rules and a saved batch's revision both find it");
            check(FurnaceRecipes.Label(cast!) == recipe && FurnaceRecipes.Label(FurnaceRecipes.Housing) != "housing", "A recipe the catalogue has no wording for shows its id until an add-on's text names it; ours keep their wording");
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            ShipbreakerMaterials.Load(); ShipbreakerRecipes.Load();
        }
        check(ShipbreakerMaterials.Added.Count == 0 && FurnaceRecipes.All.Count == shipped && FurnaceRecipes.ById(recipe) == null, "Without the add-on the shipped materials and recipes stand");
        // The loader's own rule for this mod: an added material is plain stock, and ours never are.
        void Refused(Action<MaterialPack> change, string label)
        {
            var pack = DataPacks.Parse(DataPacks.ShippedText(ShipbreakerMaterials.Source), MaterialSchema.Name, true).ToObject<MaterialPack>()!;
            change(pack);
            bool failed = false;
            try { ShipbreakerMaterials.Check(pack); } catch (ArgumentException) { failed = true; }
            check(failed, label);
        }
        ShipbreakerMaterials.Check(DataPacks.Parse(DataPacks.ShippedText(ShipbreakerMaterials.Source), MaterialSchema.Name, true).ToObject<MaterialPack>()!);
        MaterialEntry Entry(string kind) => new() { kind = kind, kg = 2, price = 5, stack = 4, side = 1, category = "IsCategoryMetals", name = "Nugget", image = "myaddon/Nugget" };
        Refused(p => p.materials["MyaddonNugget"] = Entry(ShipbreakerMaterials.FurnacePacket), "An added material of one of our own kinds is refused");
        Refused(p => p.materials[FurnaceRecipes.SteelIngot].kind = ShipbreakerMaterials.Stock, "One of ours may not be turned into plain stock");
        Refused(p => { var e = Entry(ShipbreakerMaterials.Stock); e.category = MaterialSchema.TrashCategory; p.materials["MyaddonSlag"] = e; }, "Added trash without a consumer is refused");
    }
}
