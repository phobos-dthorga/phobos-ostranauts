// Adapted in part from OCF 0.8.71's licensed recipe registration/ingredient
// integration. See the complete notice in licenses/CraftingFramework-MIT.md.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Construction;

public static class ConstructionRegistry
{
    internal sealed class Registered
    {
        internal string Owner = "";
        internal Recipe Recipe = null!;
        internal HashSet<string> Stations = null!;
    }
    internal static readonly Dictionary<string, Registered> Recipes = new Dictionary<string, Registered>(StringComparer.Ordinal);
    internal static readonly Dictionary<string, Ingredient> Selectors = new Dictionary<string, Ingredient>(StringComparer.Ordinal);
    internal static readonly Dictionary<string, Registered> StationSelectors = new Dictionary<string, Registered>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> Owners = new Dictionary<string, string>(StringComparer.Ordinal);
    private static bool loading, complete;
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
        TypeNameHandling = TypeNameHandling.None, MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 24
    };

    public static string Status(string owner) => Owners.TryGetValue(owner, out var status) ? status : "No construction pack registered.";
    public static string Describe(bool includeRecipes = false)
    {
        string phase = loading ? "Registering content" : complete ? "Content registration finished" : "Awaiting content loading";
        var lines = Owners.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ": " + p.Value);
        string result = phase + ". " + Recipes.Count + " registered construction recipes.\n" + string.Join("\n", lines);
        if (includeRecipes) result += "\n" + string.Join("\n", Recipes.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => p.Key + " -> " + string.Join(", ", p.Value.Stations.OrderBy(id => id, StringComparer.Ordinal))));
        return result + "\nStartup checks only; in-game compatibility remains subject to testing.";
    }
    public static bool Ready(string owner) => complete && Status(owner) == "Ready";
    public static string ResolveAction(string id) => id != null && Aliases.TryGetValue(id, out var target) ? target : id!;

    internal static void BeginLoad()
    {
        complete = false; loading = true;
        Recipes.Clear(); Selectors.Clear(); StationSelectors.Clear(); Aliases.Clear(); Owners.Clear();
        ConstructionHooks.ResetAll();
    }
    internal static void CompleteLoad()
    {
        loading = false;
        foreach (string owner in Owners.Keys.ToArray())
        {
            if (Owners[owner] != "Registered") continue;
            var owned = Recipes.Where(p => p.Value.Owner == owner).ToArray();
            string? conflict = owned.SelectMany(p => p.Value.Recipe.legacyActionIds)
                .FirstOrDefault(id => DataHandler.dictInteractions.ContainsKey(id));
            Owners[owner] = conflict == null ? "Ready" : "Competing provider registered legacy action " + conflict +
                ". Remove the obsolete recipe pack through the supported updater before using construction.";
            FrameworkLifecycle.Log(owner + ": " + Owners[owner]);
        }
        complete = true;
    }

    /// <summary>Explicit opt-in path; no scan of OCF's crafting directories.</summary>
    public static void RegisterPack(string owner, string filePath)
    {
        if (new FileInfo(filePath).Length > 1048576) throw new ArgumentException("Recipe pack exceeds 1 MiB.");
        var pack = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(filePath), JsonSettings);
        if (pack == null || pack.schemaVersion != 1) throw new ArgumentException("Expected recipe schemaVersion 1.");
        Register(owner, pack.recipes);
    }

    /// <summary>Register one complete owner pack during FrameworkLifecycle.ContentLoading.</summary>
    public static void Register(string owner, IEnumerable<Recipe> recipes)
    {
        if (!loading) throw new InvalidOperationException("Register during ContentLoading, before native interaction generation.");
        if (string.IsNullOrWhiteSpace(owner) || owner.Length > 200) throw new ArgumentException("An owner ID is required.");
        if (Owners.ContainsKey(owner)) throw new ArgumentException("This owner already attempted registration during this load.");
        Owners.Add(owner, "Registration failed");
        // Snapshot DTOs; a consumer cannot mutate live rules after publication.
        var inputs = recipes?.ToArray() ?? throw new ArgumentNullException(nameof(recipes));
        if (inputs.Length == 0 || inputs.Length > 128) throw new ArgumentException("Expected 1..128 recipes.");
        var copies = JsonConvert.DeserializeObject<Recipe[]>(JsonConvert.SerializeObject(inputs), JsonSettings)!;
        var native = new NativeDefinitions();
        var registered = new Dictionary<string, Registered>(StringComparer.Ordinal);
        var selectors = new Dictionary<string, Ingredient>(StringComparer.Ordinal);
        var stationSelectors = new Dictionary<string, Registered>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var stations = new Dictionary<string, JsonCondOwner>(StringComparer.Ordinal);
        try
        {
            foreach (var recipe in copies)
            {
                RecipeRules.Validate(recipe);
                string action = RecipeRules.ActionId(recipe.id);
                string inputLoot = "PhobosCraftInput_" + recipe.id, outputLoot = "PhobosCraftOutput_" + recipe.id;
                string stationTrigger = "PhobosCraftStation_" + recipe.id;
                if (Recipes.ContainsKey(action) || registered.ContainsKey(action) || DataHandler.dictInteractions.ContainsKey(action))
                    throw new ArgumentException("Recipe action already owned: " + action);
                foreach (string alias in recipe.legacyActionIds)
                {
                    if (Aliases.ContainsKey(alias) || aliases.ContainsKey(alias) || DataHandler.dictInteractions.ContainsKey(alias))
                        throw new ArgumentException("Legacy action already owned: " + alias);
                    aliases.Add(alias, action);
                }
                foreach (string id in recipe.stationIds)
                    if (!DataHandler.dictCOs.ContainsKey(id)) throw new ArgumentException("Missing construction station: " + id);
                var available = recipe.stationIds.Concat(recipe.optionalStationIds.Where(DataHandler.dictCOs.ContainsKey)).ToArray();
                foreach (string id in available)
                {
                    var station = DataHandler.dictCOs[id];
                    if (!HasInitialCondition(station, "IsInstalled")) throw new ArgumentException("Station is not installed: " + id);
                    if (!stations.TryGetValue(id, out var copy)) stations.Add(id, copy = NativeDefinitions.Clone(station));
                    copy.aInteractions = (copy.aInteractions ?? Array.Empty<string>()).Concat(new[] { action }).Distinct().ToArray();
                }
                foreach (var ingredient in recipe.ingredients)
                {
                    if (!DataHandler.dictCTs.ContainsKey(ingredient.trigger)) throw new ArgumentException("Missing ingredient trigger: " + ingredient.trigger);
                    VerifyMass(ingredient.item, ingredient.unitMassKg);
                }
                foreach (var product in recipe.outputs) VerifyMass(product.item, product.unitMassKg);
                foreach (string tool in recipe.toolTriggers)
                    if (!DataHandler.dictCTs.ContainsKey(tool)) throw new ArgumentException("Missing construction tool trigger: " + tool);
                var entry = new Registered { Owner = owner, Recipe = recipe, Stations = new HashSet<string>(available, StringComparer.Ordinal) };
                registered.Add(action, entry); stationSelectors.Add(stationTrigger, entry);
                native.Triggers.Add(stationTrigger, Trigger(stationTrigger, new[] { "IsInstalled" }, new[] { "IsDamaged" }));
                var requirements = new List<string>();
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    var ingredient = recipe.ingredients[i];
                    string selector = "PhobosCraftSelect_" + recipe.id + "_" + i;
                    selectors.Add(selector, ingredient);
                    var trigger = Trigger(selector, Array.Empty<string>(), new[] { "IsInstalled" });
                    trigger.aTriggers = new[] { ingredient.trigger };
                    native.Triggers.Add(selector, trigger);
                    requirements.Add(selector + "=1.0x" + ingredient.count.ToString(CultureInfo.InvariantCulture));
                }
                native.Loot.Add(inputLoot, new Loot { strName = inputLoot, strType = "trigger", aCOs = requirements.ToArray(), aLoots = Array.Empty<string>() });
                native.Loot.Add(outputLoot, new Loot { strName = outputLoot, strType = "item", aCOs = recipe.outputs.Select(p =>
                    p.item + "=1.0x" + p.count.ToString(CultureInfo.InvariantCulture)).ToArray(), aLoots = Array.Empty<string>() });
                string toolLoot = "PhobosCraftTools_" + recipe.id;
                var itemEffects = new List<string> { "removeus," + inputLoot + ",true,true,false", "addus," + outputLoot };
                if (recipe.toolTriggers.Length > 0)
                {
                    native.Loot.Add(toolLoot, new Loot { strName = toolLoot, strType = "trigger",
                        aCOs = recipe.toolTriggers.Select(t => t + "=1x1").ToArray(), aLoots = Array.Empty<string>() });
                    itemEffects.Add("Use," + toolLoot + ",true");
                }
                native.Interactions.Add(action, new JsonInteraction {
                    strName = action, strTitle = "Craft: " + recipe.name,
                    strDesc = "[us] [crafts] " + recipe.name + " at [them].",
                    strTooltip = recipe.description + " Requires: " + string.Join(", ", recipe.ingredients.Select(i =>
                        i.count.ToString(CultureInfo.InvariantCulture) + " x " + ItemLabel(i.item))) + ". Produces: " +
                        string.Join(", ", recipe.outputs.Select(p => p.count.ToString(CultureInfo.InvariantCulture) + " x " + ItemLabel(p.item))) +
                        ". Collects available materials before assembly.",
                    strActionGroup = "Work", strTargetPoint = "use", fTargetPointRange = recipe.range,
                    strAnim = "Tooling", strIdleAnim = "Idle", strUseCase = "Normal", strMapIcon = "IcoInstall",
                    strThemType = "Other", fDuration = recipe.workSeconds / 3600, bIgnoreFeelings = true,
                    bHumanOnly = true, nLogging = 1, CTTestThem = stationTrigger,
                    aLootItms = itemEffects.ToArray()
                });
            }
            foreach (string key in native.Triggers.Keys)
                if (DataHandler.dictCTs.ContainsKey(key)) throw new ArgumentException("Trigger collision: " + key);
            foreach (string key in native.Loot.Keys)
                if (DataHandler.dictLoot.ContainsKey(key)) throw new ArgumentException("Loot collision: " + key);
            // Publish native definitions, station copies and ownership together. No partial pack.
            var transaction = new DefinitionTransaction();
            transaction.Stage(DataHandler.dictInteractions, native.Interactions);
            transaction.Stage(DataHandler.dictCTs, native.Triggers); transaction.Stage(DataHandler.dictLoot, native.Loot);
            transaction.Stage(DataHandler.dictCOs, stations); transaction.Stage(Recipes, registered);
            transaction.Stage(Selectors, selectors); transaction.Stage(StationSelectors, stationSelectors);
            transaction.Stage(Aliases, aliases);
            transaction.Commit();
            Owners[owner] = "Registered";
            FrameworkLifecycle.Log(owner + ": registered " + copies.Length + " independent construction recipes.");
        }
        catch (Exception ex) { Owners[owner] = "Registration failed: " + ex.Message; throw; }
    }

    internal static CondTrigger Trigger(string id, string[] require, string[] forbid) => new CondTrigger {
        strName = id, fChance = 1, fCount = 1, bAND = true,
        aReqs = require, aForbids = forbid, aTriggers = Array.Empty<string>()
    };
    private static string ItemLabel(string id) => (DataHandler.dictCOOverlays.TryGetValue(id, out var overlay) ? overlay.strNameFriendly : null) ??
        (DataHandler.dictCOs.TryGetValue(id, out var co) ? co.strNameFriendly : id);
    internal static bool HasInitialCondition(JsonCondOwner co, string id) =>
        co.aStartingConds?.Any(c => c == id + "=1.0x1" || c == id + "=1.0x1.0" || c == id + "=1x1") == true;
    private static void VerifyMass(string id, double expected)
    {
        if (!DataHandler.dictCOs.TryGetValue(id, out var definition))
        {
            DataHandler.dictCOOverlays.TryGetValue(id, out var overlay);
            // Only base-mass-preserving overlays are supported. Do not silently
            // ignore arbitrary condition loot that might change the actual mass.
            if (overlay == null || !string.IsNullOrEmpty(overlay.strCondLoot) && overlay.strCondLoot != "Blank" ||
                !DataHandler.dictCOs.TryGetValue(overlay.strCOBase, out definition))
                throw new ArgumentException("Missing material or unsupported mass overlay: " + id);
        }
        var values = (definition.aStartingConds ?? Array.Empty<string>()).Where(c => c.StartsWith("StatMass=", StringComparison.Ordinal)).ToArray();
        if (values.Length != 1) throw new ArgumentException("Expected one explicit native mass for " + id);
        var parts = values[0].Substring("StatMass=".Length).Split('x');
        if (parts.Length != 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var chance) || chance != 1 ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var mass) || !RecipeRules.MassMatches(mass, expected))
            throw new ArgumentException("Material mass changed: " + id + "; expected " + expected.ToString(CultureInfo.InvariantCulture) + " kg per unit.");
    }
}
