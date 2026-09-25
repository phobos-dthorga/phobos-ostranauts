using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Persistence;

internal static class PlaceholderHealthChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var method = typeof(Ship).GetMethod("IsItemDestroyed", BindingFlags.Instance | BindingFlags.NonPublic);
        check(method?.ReturnType == typeof(bool) && method.GetParameters().Single().ParameterType == typeof(JsonItem),
            "Native destruction-check hook signature resolves");
        double generic = new DataCO(DataHandler.dictCOs["Placeholder"]).GetMaxHealth();
        check(generic == 0, "Native generic Placeholder has zero maximum health");
        var item = new JsonItem { strName = "Placeholder", strID = "marker" };
        item.SetCondAmount("StatDamage", .0015384719410736);
        var marker = new JsonPlaceholder { strName = item.strID, strInstalledCO = "PhobosExteriorGrabberInstalled", strActionCO = "PhobosExteriorGrabberLoose" };
        var saved = new JsonCondOwnerSave { strID = item.strID, strCODef = item.strName, bAlive = true,
            aConds = new[] { "IsPlaceholder=1.0x1", "StatDamageMax=1.0x20", "StatDamage=1.0x0.0015384719410736", "StatInstallProgress=1.0x17", "DEFAULT" } };
        string before = JsonConvert.SerializeObject(new object[] { item, marker, saved });
        check(item.GetCondAmountOverride("StatDamage") >= generic, "Reproduce native false destroyed classification using actual JsonItem override parsing");
        check(PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Recorded health prevents false destruction");
        check(before == JsonConvert.SerializeObject(new object[] { item, marker, saved }), "Native DTOs including damage and install progress are unchanged");
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, 20), "A future/native corrected template remains authoritative");
        saved.bAlive = false;
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Dead saved markers are not revived"); saved.bAlive = true;
        saved.strID = "other";
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Health from another full ID is rejected"); saved.strID = item.strID;
        item.strName = "PhobosExteriorGrabberInstalled";
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Finished equipment destruction stays native"); item.strName = "Placeholder";
        item.strParentID = "container";
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Contained records are not world construction markers"); item.strParentID = null;
        saved.aCondZeroes = new[] { "StatDamageMax" };
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Explicit zeroed health is respected"); saved.aCondZeroes = null;
        item.SetCondAmount("StatDamage", 20);
        check(!PlaceholderHealthLoad.CanRetain(item, marker, saved, generic), "Truly exhausted marker stays destroyed");
        check(PlaceholderHealthLoad.UniqueMarkers(new[] { marker, marker }).Count == 0, "Duplicate marker IDs are not guessed");
        HookChecks(check, item, marker, saved);
    }

    private static void HookChecks(Action<bool, string> check, JsonItem item, JsonPlaceholder marker, JsonCondOwnerSave saved)
    {
        var calls = PlaceholderLoadChecks.Calls(typeof(Ship).GetMethod("SpawnItems", BindingFlags.NonPublic | BindingFlags.Instance)!);
        check(calls.FindIndex(m => m.Name == "IsItemDestroyed") >= 0 &&
            calls.FindIndex(m => m.Name == "IsItemDestroyed") < calls.FindIndex(m => m.Name == "CreatePart"),
            "Native damage rejection occurs before marker spawning");
        // Managed shells avoid the real Ship constructor's Unity calls. Exercise
        // the actual prefixes/finalizer with native DTOs and saved-CO lookup.
        var ship = (Ship)RuntimeHelpers.GetUninitializedObject(typeof(Ship));
        ship.json = new JsonShip { aPlaceholders = new[] { marker } };
        var other = (Ship)RuntimeHelpers.GetUninitializedObject(typeof(Ship));
        other.json = new JsonShip { aPlaceholders = new[] { marker } };
        var oldSaves = DataHandler.dictCOSaves;
        var oldCache = DataHandler.dictDataCOs;
        var oldScope = PlaceholderHealthLoad.Current;
        string oldTarget = marker.strInstalledCO, oldAction = marker.strActionCO;
        try
        {
            PlaceholderHealthLoad.Current = null;
            DataHandler.dictCOSaves = new() { [item.strID] = saved };
            DataHandler.dictDataCOs = new() { ["Placeholder"] = new DataCO(DataHandler.dictCOs["Placeholder"]) };
            marker.strInstalledCO = marker.strActionCO = "ItmWall1x1";
            check(DataHandler.dictCOs.ContainsKey(marker.strInstalledCO), "Hook fixture uses a real native definition");
            item.SetCondAmount("StatDamage", .0015384719410736);
            bool result = true;
            check(PlaceholderDamageCheck.Prefix(ship, item, ref result), "Outside saved spawning the native damage check runs");
            PlaceholderHealthLoad.Prefix(ship, false, out var outer);
            check(!PlaceholderDamageCheck.Prefix(ship, item, ref result) && !result && PlaceholderHealthLoad.Current!.Retained == 1,
                "Actual hook retains a healthy matching marker using global saved COs even after json.aCOs was cleared");
            check(PlaceholderDamageCheck.Prefix(other, item, ref result), "Another ship cannot use this load's health records");
            marker.strInstalledCO = "MissingProviderTarget";
            check(PlaceholderDamageCheck.Prefix(ship, item, ref result), "Missing provider targets remain native");
            marker.strInstalledCO = "ItmWall1x1";
            PlaceholderHealthLoad.Prefix(other, true, out var template);
            check(PlaceholderHealthLoad.Current == null && PlaceholderDamageCheck.Prefix(other, item, ref result), "Nested template spawning disables correction");
            PlaceholderHealthLoad.Finalizer(null, template);
            check(ReferenceEquals(PlaceholderHealthLoad.Current!.Ship, ship), "Template completion restores the parent load");
            PlaceholderHealthLoad.Prefix(other, false, out var nested);
            var failure = new InvalidOperationException("Native load failure");
            check(ReferenceEquals(PlaceholderHealthLoad.Finalizer(failure, nested), failure) && ReferenceEquals(PlaceholderHealthLoad.Current!.Ship, ship),
                "Failed nested loads unwind without swallowing native exceptions");
            // Logging runs through the Unity localization lifecycle; that part is
            // game-owned and is not invoked by this standalone adapter test.
            PlaceholderHealthLoad.Current!.Retained = 0;
            PlaceholderHealthLoad.Finalizer(null, outer);
            check(PlaceholderHealthLoad.Current == null, "Completed loads do not leak a health override");
        }
        finally
        {
            DataHandler.dictCOSaves = oldSaves; DataHandler.dictDataCOs = oldCache;
            PlaceholderHealthLoad.Current = oldScope;
            marker.strInstalledCO = oldTarget; marker.strActionCO = oldAction;
        }
    }

    // Optional read-only audit of owner-provided archives. No saves are extracted
    // or rewritten; the same production predicate evaluates each real record.
    internal static void Audit(string archive, string shipId)
    {
        using var zip = ZipFile.OpenRead(archive);
        using var reader = new StreamReader(zip.GetEntry("ships/" + shipId + ".json")!.Open());
        var raw = JArray.Parse(reader.ReadToEnd())[0];
        var items = raw["aItems"]!.Where(i => (string?)i["strName"] == "Placeholder").Select(i => i.ToObject<JsonItem>()!).ToArray();
        // Read only the marker health fields. Other CO records include Unity-
        // backed crew/navigation objects that a standalone audit must not instantiate.
        var owners = raw["aCOs"]!.Where(c => (string?)c["strCODef"] == "Placeholder").Select(c => new JsonCondOwnerSave {
            strID = (string)c["strID"]!, strCODef = (string)c["strCODef"]!, bAlive = (bool)c["bAlive"]!,
            aConds = c["aConds"]?.ToObject<string[]>(), aCondZeroes = c["aCondZeroes"]?.ToObject<string[]>()
        }).ToDictionary(c => c.strID);
        var markers = PlaceholderHealthLoad.UniqueMarkers(raw["aPlaceholders"]!.ToObject<JsonPlaceholder[]>()!);
        double generic = new DataCO(DataHandler.dictCOs["Placeholder"]).GetMaxHealth();
        var affected = items.Where(i => i.strName == "Placeholder" && i.GetCondAmountOverride("StatDamage") >= generic).ToArray();
        var retained = affected.Where(i => markers.TryGetValue(i.strID, out var marker) && owners.TryGetValue(i.strID, out var saved) &&
            PlaceholderHealthLoad.CanRetain(i, marker, saved, generic)).ToArray();
        Console.WriteLine(JsonConvert.SerializeObject(new {
            archive = Path.GetFileName(archive), ship = shipId, nativeFalseDestructionCandidates = affected.Length,
            retainedWithRecordedHealth = retained.Length,
            targets = retained.GroupBy(i => markers[i.strID].strInstalledCO).ToDictionary(g => g.Key, g => g.Count()),
            savedColumns = (int)raw["nCols"]!, savedRows = (int)raw["nRows"]!, savesModified = false
        }, Formatting.Indented));
    }
}
