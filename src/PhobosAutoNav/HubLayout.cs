using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PhobosAutoNav;

// Shared registration data is also consumed by the offline layout verifier.
[Serializable]
internal sealed class HubLayout
{
    public float width = 600, height = 960;
    public Box[] boxes = Array.Empty<Box>();
    [Serializable] internal sealed class Box { public string id = ""; public float x, y, w, h; }
    internal Box this[string id] => boxes.First(box => box.id == id);
    internal static readonly HubLayout Data = Read();
    private static HubLayout Read()
    {
        using var stream = typeof(HubLayout).Assembly.GetManifestResourceStream("PhobosAutoNav.hub-layout.json");
        using var reader = new StreamReader(stream!);
        return JsonUtility.FromJson<HubLayout>(reader.ReadToEnd());
    }
}
