using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using PhobosAutoNav;

internal static class AutoNavHubChecks
{
    internal static void Run(string repo, Action<bool, string> check, Action<Action, string> throws)
    {
        // Exercise the compiled plugin's embedded resource and production parser,
        // not a browser reconstruction or a substitute Unity serializer.
        var layout = HubLayout.Data;
        var source = JObject.Parse(File.ReadAllText(Path.Combine(repo, "assets/phobos-autonav/hub-layout.json")));
        check(layout.width == (float)source["width"]! && layout.height == (float)source["height"]!, "Embedded hub dimensions match source");
        check(layout.boxes.Length == source["boxes"]!.Count(), "Embedded hub retains every region");
        foreach (var region in source["boxes"]!)
        {
            string id = (string)region["id"]!;
            var actual = layout[id];
            check(actual.x == (float)region["x"]! && actual.y == (float)region["y"]! &&
                actual.w == (float)region["w"]! && actual.h == (float)region["h"]!, "Embedded hub region survives managed deserialization: " + id);
        }
        Reject(copy => copy["boxes"] = new JArray(), "Reject empty region data before UI construction");
        Reject(copy => copy["boxes"] = null, "Reject null region data");
        Reject(copy => copy.Remove("width"), "Do not silently default missing canvas width");
        Reject(copy => copy["height"] = 0, "Reject zero canvas height");
        Reject(copy => copy["width"] = "Infinity", "Reject nonfinite canvas width");
        Reject(copy => ((JArray)copy["boxes"]!).Add(copy["boxes"]![0]!.DeepClone()), "Reject duplicate region names");
        Reject(copy => copy["boxes"]![0] = null, "Reject null region");
        Reject(copy => ((JObject)copy["boxes"]![0]!).Remove("id"), "Reject missing region name");
        Reject(copy => copy["boxes"]![0]!["w"] = -1, "Reject negative region size");
        Reject(copy => copy["boxes"]![0]!["x"] = 600, "Reject off-canvas region");
        throws(() => HubLayout.Parse("null"), "Reject null layout");
        throws(() => HubLayout.Parse("{"), "Reject truncated layout");
        var missing = (JObject)source.DeepClone();
        missing["boxes"]![0]!.Remove();
        var incomplete = HubLayout.Parse(missing.ToString());
        try { _ = incomplete["title"]; check(false, "Missing region must be diagnosed"); }
        catch (InvalidDataException ex) { check(ex.Message.Contains("'title'"), "Missing lookup identifies the region instead of an opaque LINQ exception"); }

        var originalMods = DataHandler.dictModInfos;
        try
        {
            DataHandler.dictModInfos = new Dictionary<string, JsonModInfo>();
            check(!EquipmentContent.NativePackageEnabled, "Missing native package does not create a hub");
            var mod = new JsonModInfo { strName = "Phobos Auto Nav" };
            DataHandler.dictModInfos.Add("PhobosAutoNav", mod);
            check(EquipmentContent.NativePackageEnabled, "Enabled native package permits a hub");
            mod.SetDisabled(true);
            check(!EquipmentContent.NativePackageEnabled, "Disabled native package does not create a hub");
            var objects = DataHandler.dictCOs.ToArray(); var loot = DataHandler.dictLoot.ToArray(); var overlays = DataHandler.dictCOOverlays.ToArray();
            try { EquipmentContent.Register(); check(false, "Disabled native package must reject registration"); }
            catch (InvalidOperationException ex) { check(ex.Message.Contains("native package"), "Native prerequisite is checked before any equipment preparation"); }
            check(objects.Length == DataHandler.dictCOs.Count && objects.All(x => DataHandler.dictCOs[x.Key] == x.Value) &&
                loot.Length == DataHandler.dictLoot.Count && loot.All(x => DataHandler.dictLoot[x.Key] == x.Value) &&
                overlays.Length == DataHandler.dictCOOverlays.Count && overlays.All(x => DataHandler.dictCOOverlays[x.Key] == x.Value),
                "Disabled registration publishes no boards, offers, salvage or overlays");
        }
        finally { DataHandler.dictModInfos = originalMods; }

        void Reject(Action<JObject> mutate, string message)
        {
            var copy = (JObject)source.DeepClone(); mutate(copy);
            throws(() => HubLayout.Parse(copy.ToString()), message);
        }
    }
}
