using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Localization;

internal static class LocalizationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var warnings = new List<string>();
        var catalog = new TranslationCatalog("{\"work\":\"{0}: {1:F1} kg\",\"plain\":\"Ready\",\"grammar\":\"[us] [crafts] {0} at [them].\"}", warnings.Add);
        catalog.Select("fr", "{\"work\":\"Masse {1:F1} kg : {0}\",\"plain\":\"Prêt\"}");
        check(catalog.Get("work", "acier", 12.5) == "Masse 12,5 kg : acier", "Translation reorders arguments and formats numbers with the selected culture");
        check(catalog.Get("plain") == "Prêt", "Unicode translation survives JSON and formatting");
        catalog.Select("ja", "{\"plain\":\"準備完了\"}");
        check(catalog.Get("plain") == "準備完了" && catalog.Get("work", "steel", 12.5) == "steel: 12.5 kg", "Partial non-Latin catalog uses English for missing messages");
        foreach (string broken in new[] { "{", "{\"plain\":12}", "{\"plain\":\"one\",\"plain\":\"two\"}", "{} {}" })
        {
            catalog.Select("en", broken);
            check(catalog.Get("plain") == "Ready", "Malformed/duplicate/non-string translation file cannot disable fallback");
        }
        foreach (string broken in new[] { "{0}", "{0} {2}", "{0} {1", "{0} {1:F1}}", "{0} {1,2147483647}", "{0} {1:F999999}", "" })
        {
            string overlay = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string> { ["work"] = broken });
            catalog.Select("en", overlay);
            check(catalog.Get("work", "steel", 12.5) == "steel: 12.5 kg", "Broken placeholders retain the English message: " + broken);
        }
        catalog.Select("en", "{\"grammar\":\"{0}\"}");
        check(catalog.Get("grammar", "item") == "[us] [crafts] item at [them].", "Missing native grammar tokens reject the translation");
        check(TranslationCatalog.Signature("{{value}} {1} {0:F2} {1}") == "0,1", "Escaped braces and repeated/reordered placeholders are valid");
        catalog.Select("en");
        int before = warnings.Count;
        check(catalog.Get("absent") == "[absent]" && catalog.Get("absent") == "[absent]" && warnings.Count == before + 1,
            "Unknown source key is visible and logged only once");
        check(catalog.Get("work") == "{0}: {1:F1} kg", "Caller argument error does not throw into gameplay");
        check(Translations.Get("absent.provider", "recipe.key", "English recipe") == "English recipe", "Optional translation provider preserves recipe fallback");

        var makers = new EquipmentNames("{\"item\":{\"brand\":\"Asterel\",\"model\":\"N1\"},\"waste\":{\"brand\":\"\",\"model\":\"\"}}");
        var branded = new TranslationCatalog("{\"item\":\"Module{0}\",\"waste\":\"Spent Parts\",\"status\":\"Ready\"}", warnings.Add, makers);
        branded.Select("fr", "{\"item\":\"Module de navigation{0}\"}");
        check(branded.Get("item", " (endommagé)") == "Phobos' Asterel N1 Module de navigation (endommagé)", "Brand/model stay fixed while type and damage suffix translate");
        check(branded.Get("waste") == "Phobos' Spent Parts" && branded.Get("status") == "Ready", "Unmodelled material names and ordinary messages use their own paths");
        branded.Select("en", "{\"item\":\"Phobos Polaris Module{0}\"}");
        check(branded.Get("item", "") == "Phobos' Asterel N1 Module", "Old full-name override falls back without duplicating the maker prefix");
        branded.Select("en", "{\"item\":\"Asterel N1 Module{0}\"}");
        check(branded.Get("item", "") == "Phobos' Asterel N1 Module", "An embedded maker/model is not doubled");
        branded.Select("ja", "{\"item\":\"航法装置{0}\"}");
        check(branded.Get("item", "") == "Phobos' Asterel N1 航法装置", "Name formatting survives repeated language changes");
        foreach (string invalid in new[] { "{\"x\":{\"brand\":\"\",\"model\":\"N1\"}}", "{\"x\":{\"brand\":\"A\",\"model\":\"N1\",\"typo\":1}}", "{\"x\":{\"brand\":\"A\",\"model\":\"<b>\"}}", "{} {}" })
        {
            bool rejected = false;
            try { _ = new EquipmentNames(invalid); } catch (Exception ex) when (ex is FormatException || ex is Newtonsoft.Json.JsonException) { rejected = true; }
            check(rejected, "Malformed naming metadata fails during author registration");
        }
        bool missingName = false;
        try { _ = new TranslationCatalog("{\"other\":\"Ready\"}", null, makers); } catch (FormatException) { missingName = true; }
        check(missingName, "A misspelled name key cannot silently lose branding");

        // Only our own temporary files; no game directory or save is touched.
        string root = Path.Combine(Path.GetTempPath(), "PhobosTranslationChecks-" + Guid.NewGuid().ToString("N"));
        const string owner = "phobos.tests.localization";
        string folder = Path.Combine(root, owner);
        Directory.CreateDirectory(folder);
        string? previous = Translations.UserDirectory;
        string language = Translations.Language;
        try
        {
            File.WriteAllText(Path.Combine(folder, "fr.json"), "{\"ConstructionRegistry.ready\":\"Prêt\"}");
            File.WriteAllText(Path.Combine(folder, "fr-CA.json"), "{\"ConstructionRegistry.ready\":\"Prêt régional\"}");
            Translations.UserDirectory = root;
            var shared = Translations.Register(owner, typeof(FrameworkInfo).Assembly, "PhobosFramework.en.json");
            Translations.Select("fr-CA");
            check(shared.Get("ConstructionRegistry.ready") == "Prêt régional", "Regional user override has priority");
            File.WriteAllText(Path.Combine(folder, "fr-CA.json"), "{");
            Translations.Select("fr-CA");
            check(shared.Get("ConstructionRegistry.ready") == "Prêt", "Malformed regional file retains valid parent language");
            Translations.Select("../../outside");
            check(Translations.Language == "en" && shared.Get("ConstructionRegistry.ready") == "Ready", "Language selection cannot escape its translation directory");
        }
        finally
        {
            Translations.UserDirectory = previous; Translations.Select(language);
            File.Delete(Path.Combine(folder, "fr.json")); File.Delete(Path.Combine(folder, "fr-CA.json"));
            Directory.Delete(folder); Directory.Delete(root);
        }

        // Build-time content audit: every literal key and every construction/overlay
        // key must exist. This also validates all contributed language files.
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var consoleEnglish = TranslationCatalog.Parse(File.ReadAllText(Path.Combine(repo, "translations/PhobosFramework/en.json")));
        foreach (string mod in new[] { "PhobosFramework", "PhobosShipbreaker", "PhobosAutoNav" })
        {
            string translations = Path.Combine(repo, "translations", mod);
            var baseline = TranslationCatalog.Parse(File.ReadAllText(Path.Combine(translations, "en.json")));
            string namingFile = Path.Combine(repo, "mods", mod, "framework", "equipment-names.json");
            var equipment = File.Exists(namingFile) ? new EquipmentNames(File.ReadAllText(namingFile)) : null;
            var english = new TranslationCatalog(File.ReadAllText(Path.Combine(translations, "en.json")), null, equipment);
            foreach (var entry in baseline) { TranslationCatalog.Signature(entry.Value); check(true, "Valid English template: " + entry.Key); }
            foreach (string file in Directory.GetFiles(Path.Combine(repo, "src", mod), "*.cs", SearchOption.AllDirectories))
            {
            string source = File.ReadAllText(file);
            foreach (Match key in Regex.Matches(source, "\\bText\\.Get\\(\"([^\"]+)\"\\s*[,)]"))
                check(baseline.ContainsKey(key.Groups[1].Value), "English key exists: " + mod + "/" + key.Groups[1].Value);
            foreach (Match key in Regex.Matches(source, "\\b(?:ConsoleText\\.Get|ConsoleWidgets\\.Text|C\\.Text)\\(\"([^\"]+)\"\\s*[,)]"))
                check(consoleEnglish.ContainsKey("Console." + key.Groups[1].Value), "Shared console key exists: " + key.Groups[1].Value);
            }
            foreach (string file in Directory.GetFiles(translations, "*.json"))
            {
                var entries = TranslationCatalog.Parse(File.ReadAllText(file));
                var errors = new List<string>();
                var verifier = new TranslationCatalog(File.ReadAllText(Path.Combine(translations, "en.json")), errors.Add, equipment);
                verifier.Select(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
                check(errors.Count == 0 && entries.Keys.All(baseline.ContainsKey), "Valid contributed catalog: " + file + " " + string.Join("; ", errors));
            }
            string recipeFile = Path.Combine(repo, "mods", mod, "framework", "recipes.json");
            if (File.Exists(recipeFile))
            foreach (var recipe in Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(recipeFile))["recipes"]!)
            foreach (string field in new[] { "name", "description" })
                check(english.Get((string)recipe[field + "Key"]!) == (string)recipe[field]!, "Construction key and English fallback agree: " + recipe["id"]);
            if (mod == "PhobosAutoNav")
            foreach (var overlay in Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Path.Combine(repo, "mods", mod, "data/cooverlays/phobos_approach_assist.json"))))
            {
                check(english.Get("Overlay." + overlay["strName"] + ".name") == (string)overlay["strNameFriendly"]!, "Overlay name has a translation key");
                check(baseline["Overlay." + overlay["strName"] + ".description"] == (string)overlay["strDesc"]!, "Overlay description has a translation key");
            }
        }
    }
}
