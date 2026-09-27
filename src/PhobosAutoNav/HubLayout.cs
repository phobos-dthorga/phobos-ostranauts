using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PhobosAutoNav;

// Parse plugin-owned data with the same managed JSON library used by Framework.
// Unity's serializer depends on its registered script types; this DLL is loaded later.
internal sealed class HubLayout
{
    [JsonProperty(Required = Required.Always)]
    public float width = 600, height = 960;
    [JsonProperty(Required = Required.Always)]
    public Box[] boxes = Array.Empty<Box>();
    [JsonProperty(Required = Required.Always)]
    public Box[] zones = Array.Empty<Box>();
    internal sealed class Box
    {
        [JsonProperty(Required = Required.Always)] public string id = "";
        [JsonProperty(Required = Required.Always)] public float x = 0, y = 0, w = 0, h = 0;
    }
    private readonly Dictionary<string, Box> byId = new(StringComparer.Ordinal);
    internal Box this[string id] => byId.TryGetValue(id, out var box) ? box :
        throw new InvalidDataException("Phobos flight hub layout is missing region '" + id + "'.");
    private static readonly Lazy<HubLayout> data = new(Read);
    internal static HubLayout Data => data.Value;
    private static HubLayout Read()
    {
        using var stream = typeof(HubLayout).Assembly.GetManifestResourceStream("PhobosAutoNav.hub-layout.json") ??
            throw new InvalidDataException("Phobos flight hub layout resource is missing from the plugin.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }
    internal static HubLayout Parse(string json)
    {
        var layout = JsonConvert.DeserializeObject<HubLayout>(json) ??
            throw new InvalidDataException("Phobos flight hub layout is empty.");
        if (!Positive(layout.width) || !Positive(layout.height) || layout.boxes.Length == 0 || layout.zones.Length == 0)
            throw new InvalidDataException("Phobos flight hub layout needs positive dimensions and named regions.");
        foreach (var box in layout.boxes)
        {
            if (box == null || string.IsNullOrWhiteSpace(box.id) || layout.byId.ContainsKey(box.id))
                throw new InvalidDataException("Phobos flight hub layout has a missing or duplicate region name.");
            if (!Finite(box.x) || !Finite(box.y) || box.x < 0 || box.y < 0 || !Positive(box.w) || !Positive(box.h) ||
                box.x + box.w > layout.width || box.y + box.h > layout.height)
                throw new InvalidDataException("Phobos flight hub layout has invalid bounds for '" + box.id + "'.");
            layout.byId.Add(box.id, box);
        }
        var zoneIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in layout.zones)
        {
            if (zone == null || string.IsNullOrWhiteSpace(zone.id) || !zoneIds.Add(zone.id) ||
                !Finite(zone.x) || !Finite(zone.y) || zone.x < 0 || zone.y < 0 || !Positive(zone.w) || !Positive(zone.h) ||
                zone.x + zone.w > layout.width || zone.y + zone.h > layout.height)
                throw new InvalidDataException("Phobos flight hub has invalid frame bounds.");
        }
        return layout;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Positive(float value) => Finite(value) && value > 0;
}
