using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

/// <summary>Add-ons players publish (Framework 0.90.0): the manifest's rules, discovery in load order, the order files
/// apply in with the priority header, the id prefix rule, and recipe revisions derived from ids.</summary>
internal static class AddOnChecks
{
    private const string Shipped = "{\"schemaVersion\":1,\"schema\":\"outcomes\",\"tables\":{\"wash\":{\"outcomes\":{\"wash\":50,\"wash-steel\":30}}}}";
    private static string Manifest(string id, string prefix, string requires = "") =>
        "{\"schemaVersion\":1,\"id\":\"" + id + "\",\"name\":\"N\",\"author\":\"A\",\"version\":\"1.0.0\",\"idPrefix\":\"" + prefix + "\"" + (requires.Length > 0 ? ",\"requires\":{" + requires + "}" : "") + "}";

    internal static void Run(Action<bool, string> check)
    {
        void Refused(string json, string message) { bool failed = false; try { AddOns.ParseManifest(json); } catch (FormatException) { failed = true; } check(failed, message); }
        var ok = AddOns.ParseManifest(Manifest("my-add-on", "myaddon", "\"PhobosManufacturing\":\"0.44.0\""));
        check(ok.id == "my-add-on" && ok.idPrefix == "myaddon" && ok.requires["PhobosManufacturing"] == "0.44.0", "A manifest reads its id, prefix and requirements");
        Refused(Manifest("My Add On", "myaddon"), "An id with capitals or spaces is refused");
        Refused(Manifest("my-add-on", "PhobosThing"), "A prefix that starts with Phobos is refused");
        Refused(Manifest("my-add-on", "Itmx"), "A prefix that starts like the game's own ids is refused");
        Refused(Manifest("my-add-on", "ab"), "A prefix under three characters is refused");
        Refused(Manifest("my-add-on", "my-addon"), "A prefix with a hyphen is refused");
        Refused(Manifest("my-add-on", "myaddon", "\"PhobosManufacturing\":\"new\""), "A requirement that is not a version is refused");
        Refused(Manifest("my-add-on", "myaddon").Replace("\"author\":\"A\"", "\"author\":\"A\",\"code\":\"x\""), "An unknown manifest field is refused");
        check(AddOns.AtLeast("0.44.0.0", "0.44.0") && AddOns.AtLeast("0.45.1", "0.44.0") && !AddOns.AtLeast("0.43.9", "0.44.0") && !AddOns.AtLeast("unknown", "0.1.0"), "Requirements compare versions, not text");
        check(AddOns.Owns(ok, "myaddon-steel") && AddOns.Owns(ok, "MyAddonSteel") && !AddOns.Owns(ok, "gangue-wash"), "An id belongs to an add-on when it starts with its prefix, in any case");

        // Translations and names from add-ons (Framework 0.91.0).
        check(AddOns.IsFolderOf("PhobosManufacturing", "phobosgekko.ostranauts.manufacturing") && AddOns.IsFolderOf("phobosframework", "phobosgekko.ostranauts.framework") &&
              !AddOns.IsFolderOf("PhobosAgriculture", "phobosgekko.ostranauts.manufacturing"), "A translation folder is named after the mod folder, in any case");
        check(AddOns.OwnsKey(ok, "Recipe.myaddon-rich-seam") && AddOns.OwnsKey(ok, "Material.MyaddonAlloy_description") && !AddOns.OwnsKey(ok, "Recipe.gangue-wash") && !AddOns.OwnsKey(ok, "Refinery.name"),
            "A text key is an add-on's own when one of its parts starts with the add-on's prefix");
        var catalog = new Phobos.Ostranauts.Framework.Localization.TranslationCatalog("{\"plain\":\"Ready\",\"count\":\"{0} items\"}");
        catalog.Select("en", new[]
        {
            new Phobos.Ostranauts.Framework.Localization.TranslationOverlay("{\"plain\":\"All set\",\"Recipe.myaddon-rich-seam\":\"Rich seam\",\"Recipe.other-thing\":\"Other\",\"Recipe.myaddon-count\":\"{0} seams\"}", key => AddOns.OwnsKey(ok, key)),
            new Phobos.Ostranauts.Framework.Localization.TranslationOverlay("{\"Recipe.myaddon-local\":\"Local\"}")
        });
        check(catalog.Get("plain") == "All set" && catalog.Get("Recipe.myaddon-rich-seam") == "Rich seam" && catalog.Get("Recipe.myaddon-count", 3) == "3 seams" && catalog.Contains("Recipe.myaddon-rich-seam"),
            "An add-on's file retunes our text and names its own additions");
        check(!catalog.Contains("Recipe.other-thing") && !catalog.Contains("Recipe.myaddon-local") && catalog.Get("Recipe.other-thing") == "[Recipe.other-thing]",
            "A new key outside the add-on's prefix, or from a plain file, is still ignored");
        catalog.Select("en");
        check(!catalog.Contains("Recipe.myaddon-rich-seam") && catalog.Get("plain") == "Ready", "Without the add-on its text is gone and ours is back");

        // The same numbers scripts/validate-data-packs.py derives, so the game and the offline checker agree.
        check(Outcomes.Hash(new[] { "a1", "b2" }) == 1848931155u && RecipeSchema.DerivedRevision("richergangue-steel-seam") == 843336921, "The stable hash and a derived revision match the offline checker's");
        check(RecipeSchema.DerivedRevision("x") >= RecipeSchema.DerivedRevisionFloor && RecipeSchema.DerivedRevision("x") == RecipeSchema.DerivedRevision("x") &&
              RecipeSchema.DerivedRevision("x") != RecipeSchema.DerivedRevision("y"), "A derived revision is stable, its own, and far above any shipped revision");
        var overlay = Newtonsoft.Json.Linq.JObject.Parse("{\"recipes\":{\"mine-new\":{\"machine\":\"m\"},\"shipped\":{\"seconds\":5},\"mine-own\":{\"revision\":7}}}");
        RecipeSchema.PrepareOverlay(overlay, Newtonsoft.Json.Linq.JObject.Parse("{\"recipes\":{\"shipped\":{\"revision\":2}}}"));
        check((int)overlay["recipes"]!["mine-new"]!["revision"]! == RecipeSchema.DerivedRevision("mine-new") && overlay["recipes"]!["shipped"]!["revision"] == null && (int)overlay["recipes"]!["mine-own"]!["revision"]! == 7,
            "Only a recipe a file adds without a revision gets a derived one");

        var recipes = new[] { "wash", "wash-steel", "alpha-rich", "beta-rich" }.ToDictionary(id => id, _ => new OutcomeRecipe("leach", "sig"));
        OutcomeRecipe? Find(string id) => recipes.TryGetValue(id, out var r) ? r : null;
        string root = Path.Combine(Path.GetTempPath(), "phobos-addons-" + Guid.NewGuid().ToString("N"));
        string MakeAddOn(string folder, string id, string prefix, string requires = "")
        {
            string dir = Path.Combine(root, folder); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, AddOns.ManifestFile), Manifest(id, prefix, requires));
            return dir;
        }
        void Put(string dir, string name, string json)
        {
            string folder = Path.Combine(dir, AddOns.Folder, "TestMod", OutcomeSchema.Name); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), json);
        }
        var saved = AddOns.EnabledModDirectories;
        try
        {
            string alpha = MakeAddOn("100", "alpha", "alpha"), beta = MakeAddOn("200", "beta", "beta"), plain = Path.Combine(root, "plain"), broken = MakeAddOn("300", "broken", "Phobosx"),
                twin = MakeAddOn("400", "alpha", "other"), needy = MakeAddOn("500", "needy", "needy", "\"TestMod\":\"9.0.0\""), old = MakeAddOn("600", "oldfw", "oldfw", "\"PhobosFramework\":\"99.0.0\"");
            Directory.CreateDirectory(plain);
            AddOns.EnabledModDirectories = () => new[] { plain, alpha, beta, broken, twin, needy, old };
            AddOns.Reset();
            check(AddOns.Current.Select(a => a.Manifest.id).SequenceEqual(new[] { "alpha", "beta", "needy" }) && AddOns.Current[0].Order < AddOns.Current[1].Order,
                "Add-ons are found in load order; a folder without a manifest, a broken manifest, a repeated id and an unmet Framework requirement are left out");
            check(AddOns.Refused.Count == 3, "Each add-on left out is named with its reason");
            var usable = AddOns.For("TestMod", "1.0.0").ToArray();
            check(usable.Select(a => a.Manifest.id).SequenceEqual(new[] { "alpha", "beta" }) && AddOns.Refused.Count == 4, "An add-on that needs a newer version of a mod is skipped for that mod, with its reason");

            // Default order: add-ons in load order, then the player's own file.
            Put(alpha, "a.json", "{\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":40,\"alpha-rich\":5}}}}");
            Put(beta, "b.json", "{\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":60}}}}");
            string local = Path.Combine(root, "local"); Directory.CreateDirectory(local);
            File.WriteAllText(Path.Combine(local, "mine.json"), "{\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":70}}}}");
            OutcomePack Load() => DataPacks.LoadText<OutcomePack>(Shipped, local, usable, "TestMod", "test", OutcomeSchema.Name, (p, _) => OutcomeSchema.Validate(p, Find));
            var table = Load().tables["wash"].outcomes;
            check(table["wash-steel"] == 70 && table["alpha-rich"] == 5 && table["wash"] == 50, "The player's own file has the last word, and an add-on's addition stands");

            // The priority header: a higher number applies later, wherever the file comes from.
            Put(beta, "b.json", "{\"priority\":10,\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":60}}}}");
            check(Load().tables["wash"].outcomes["wash-steel"] == 60, "An add-on file with a higher priority wins over the player's file");
            File.WriteAllText(Path.Combine(local, "mine.json"), "{\"priority\":20,\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":70}}}}");
            check(Load().tables["wash"].outcomes["wash-steel"] == 70, "A player who raises their own priority has the last word again");
            Put(alpha, "a.json", "{\"priority\":-5,\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":40,\"alpha-rich\":5}}}}");
            check(Load().tables["wash"].outcomes["alpha-rich"] == 5, "A lower priority applies earlier and its additions still stand");

            // Refusals: a foreign id, a bad priority; the rest stands.
            int before = DataPacks.Problems.Count;
            Put(beta, "c-foreign.json", "{\"tables\":{\"gamma-table\":{\"outcomes\":{\"gamma-table\":1}}}}");
            Put(beta, "d-priority.json", "{\"priority\":500,\"tables\":{\"wash\":{\"outcomes\":{\"wash\":1}}}}");
            Put(beta, "e-own.json", "{\"tables\":{\"beta-rich\":{\"outcomes\":{\"beta-rich\":1}}}}");
            var pack = Load();
            check(!pack.tables.ContainsKey("gamma-table") && pack.tables.ContainsKey("beta-rich") && pack.tables["wash"].outcomes["wash"] == 50 && DataPacks.Problems.Count == before + 2,
                "An add-on may add only entries with its own prefix, and a priority out of range is refused; its other files still apply");
            check(DataPacks.Problems.Skip(before).All(p => p.File.StartsWith("beta/", StringComparison.Ordinal)), "A refused add-on file is named with its add-on");
        }
        finally
        {
            AddOns.EnabledModDirectories = saved; AddOns.Reset();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
