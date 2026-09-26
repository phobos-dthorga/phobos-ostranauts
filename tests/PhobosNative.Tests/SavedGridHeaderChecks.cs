using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Persistence;

internal static class SavedGridHeaderChecks
{
    private sealed class FieldsOnly : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type type) => type.Namespace == "UnityEngine"
            ? type.GetFields(BindingFlags.Instance | BindingFlags.Public).Cast<MemberInfo>().ToList()
            : base.GetSerializableMembers(type);
    }
    private static JToken Snapshot(JsonShip data) => JToken.FromObject(data,
        JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new FieldsOnly() }));
    private static Ship ShipShell(JsonShip saved, Ship.Loaded state = Ship.Loaded.None)
    {
        var ship = (Ship)RuntimeHelpers.GetUninitializedObject(typeof(Ship));
        ship.json = saved;
        typeof(Ship).GetField("nLoadState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ship, state);
        return ship;
    }

    private static JsonShip Fixture()
    {
        return new JsonShip {
            nCols = 9, nRows = 6, vShipPos = new UnityEngine.Vector2(-10, 20),
            aRooms = new[] {
                new JsonRoom { strID = "outside", bVoid = true, aTiles = Enumerable.Range(0, 48).Where(t => t != 18).ToArray() },
                new JsonRoom { strID = "inside", bVoid = false, aTiles = new[] { 18 } }
            },
            aItems = new[] {
                new JsonItem { strID = "outside", strName = "Compartment", fX = -10, fY = 20 },
                new JsonItem { strID = "inside", strName = "Compartment", fX = -8, fY = 18 }
            }, aZones = new[] { new JsonZone { aTiles = new[] { 18, 19 } } }
        };
    }

    internal static void Run(Action<bool, string> check)
    {
        var init = typeof(Ship).GetMethod(nameof(Ship.InitShip))!;
        check(init.GetParameters()[0].Name == "bTemplateOnly" && init.GetParameters()[1].ParameterType == typeof(Ship.Loaded), "Native grid-header load hook resolves");
        var save = typeof(Ship).GetMethod(nameof(Ship.GetJSON))!;
        check(save.ReturnType == typeof(JsonShip) && save.GetParameters().Any(p => p.Name == "bSaveGame" && p.ParameterType == typeof(bool)), "Native grid-header save hook resolves");
        var calls = PlaceholderLoadChecks.Calls(save);
        check(calls.FindIndex(m => m.Name == "TrimAllSides") >= 0 && calls.FindIndex(m => m.Name == "TrimAllSides") < calls.FindIndex(m => m.Name == "SaveCOs") &&
            calls.FindIndex(m => m.Name == "TrimAllSides") < calls.FindIndex(m => m.DeclaringType == typeof(Room) && m.Name == "GetJSONSave"),
            "Native save trims before serializing item positions and room indices");
        var oldLog = FrameworkLifecycle.Log;
        var messages = new List<string>();
        FrameworkLifecycle.Log = messages.Add;
        try
        {
            var data = Fixture(); var ship = ShipShell(data);
            var original = Snapshot(data);
            SavedGridHeaderLoad.Prefix(ship, true, Ship.Loaded.Edit);
            check(JToken.DeepEquals(original, Snapshot(data)), "Template loads remain untouched");
            SavedGridHeaderLoad.Prefix(ship, false, Ship.Loaded.Shallow);
            check(data.nCols == 9, "Shallow-only loads remain untouched");
            ship = ShipShell(data, Ship.Loaded.Shallow);
            SavedGridHeaderLoad.Prefix(ship, false, Ship.Loaded.Edit);
            check(data.nCols == 8 && data.nRows == 6, "Shallow-to-full loading corrects stale dimensions before SpawnItems and its padding hook");
            original["nCols"] = 8;
            check(JToken.DeepEquals(original, Snapshot(data)), "Only header dimensions change; room, zone, item and origin data survive exactly");
            int logged = messages.Count;
            SavedGridHeaderLoad.Prefix(ship, false, Ship.Loaded.Edit);
            check(messages.Count == logged, "Repeated correction is a no-op");
            data = Fixture(); data.aItems = data.aItems.Take(1).ToArray();
            check(!SavedGridHeader.TryResolve(data, out _, out _), "Missing compartment anchors block load correction");
            data = Fixture(); data.aItems = data.aItems.Concat(new[] { data.aItems[1] }).ToArray();
            check(!SavedGridHeader.TryResolve(data, out _, out _), "Duplicate item identities block load correction");
            data = Fixture(); data.aItems[1].strName = "Placeholder";
            check(!SavedGridHeader.TryResolve(data, out _, out _), "Non-compartment records cannot anchor a room");
            data = Fixture(); ship = ShipShell(data, Ship.Loaded.Edit);
            SavedGridHeaderLoad.Prefix(ship, false, Ship.Loaded.Full);
            check(data.nCols == 9, "Already populated ships do not have their loading header changed");

            ship.nCols = 8; ship.nRows = 6; ship.vShipPos = data.vShipPos;
            ship.aTiles = Enumerable.Repeat<Tile>(null!, 48).ToList();
            original = Snapshot(data);
            SavedGridHeaderSave.Postfix(ship, false, data);
            check(data.nCols == 9, "Template exports retain native behavior");
            SavedGridHeaderSave.Postfix(ship, true, data);
            original["nCols"] = 8;
            check(data.nCols == 8 && JToken.DeepEquals(original, Snapshot(data)), "Outgoing save dimensions match post-trim live geometry without altering other records");
            check(ship.nCols == 8 && ship.aTiles.Count == 48, "Saving never pads, trims or replaces live tiles");
            logged = messages.Count;
            SavedGridHeaderSave.Postfix(ship, true, data);
            check(messages.Count == logged, "A consistent outgoing header is a no-op");
            data = Fixture(); data.vShipPos = new UnityEngine.Vector2(-9, 20);
            SavedGridHeaderSave.Postfix(ship, true, data);
            check(data.nCols == 9, "A differing serialized origin blocks correction");
            data = Fixture(); data.nCols = 7;
            SavedGridHeaderSave.Postfix(ship, true, data);
            check(data.nCols == 7, "The save fix never expands a header from a different grid");
            data = Fixture(); ship.aTiles.RemoveAt(0);
            SavedGridHeaderSave.Postfix(ship, true, data);
            check(data.nCols == 9, "Inconsistent live tile counts are not serialized as a correction");
            ship = ShipShell(data, Ship.Loaded.Shallow); ship.nCols = 8; ship.nRows = 6; ship.aTiles = Enumerable.Repeat<Tile>(null!, 48).ToList(); ship.vShipPos = data.vShipPos;
            SavedGridHeaderSave.Postfix(ship, true, data);
            check(data.nCols == 9, "Shallow snapshots keep their stored dimensions");
        }
        finally { FrameworkLifecycle.Log = oldLog; }
    }

    internal static void Audit(string archive, string shipId)
    {
        using var zip = ZipFile.OpenRead(archive);
        using var reader = new StreamReader(zip.GetEntry("ships/" + shipId + ".json")!.Open());
        var raw = JArray.Parse(reader.ReadToEnd())[0];
        // Avoid instantiating unrelated Unity-backed COs in the offline host.
        var data = new JsonShip {
            strRegID = shipId, nCols = (int)raw["nCols"]!, nRows = (int)raw["nRows"]!,
            vShipPos = raw["vShipPos"]!.ToObject<UnityEngine.Vector2>(),
            aRooms = raw["aRooms"]!.ToObject<JsonRoom[]>()!, aZones = raw["aZones"]?.ToObject<JsonZone[]>(),
            aItems = raw["aItems"]!.Where(i => (string?)i["strName"] == "Compartment").Select(i => i.ToObject<JsonItem>()!).ToArray()
        };
        var before = Snapshot(data);
        bool corrected = SavedGridHeader.TryResolve(data, out int columns, out int rows);
        if (!JToken.DeepEquals(before, Snapshot(data))) throw new Exception("Read-only planner mutated the saved data");
        var mappings = data.aRooms.Select(r => {
            var item = data.aItems.Single(i => i.strID == r.strID);
            int x = (int)(item.fX - data.vShipPos.x), y = (int)(data.vShipPos.y - item.fY);
            int oldIndex = y * data.nCols + x, index = y * (corrected ? columns : data.nCols) + x;
            return new { id = r.strID, oldIndex, correctedIndex = index, correctRoom = r.aTiles.Contains(index),
                oldSelectedRoom = data.aRooms.FirstOrDefault(room => room.aTiles.Contains(oldIndex))?.strID };
        }).ToArray();
        if (mappings.Any(m => !m.correctRoom)) throw new Exception("A saved room position still maps to another compartment");
        Console.WriteLine(JsonConvert.SerializeObject(new { archive = Path.GetFileName(archive), ship = shipId,
            savedColumns = data.nCols, savedRows = data.nRows, corrected,
            resolvedColumns = corrected ? columns : data.nCols, resolvedRows = corrected ? rows : data.nRows,
            mappings, originalArchiveModified = false }, Formatting.Indented));
    }
}
