using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Registration;

internal static class CompletionArtworkChecks
{
    // Native values include Unity vectors whose normalized property recursively
    // creates another vector. Compare serialized public data fields only.
    private sealed class FieldsOnly : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        protected override System.Collections.Generic.List<System.Reflection.MemberInfo> GetSerializableMembers(Type type) =>
            type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Cast<System.Reflection.MemberInfo>().ToList();
    }

    internal static void Run(NativeDefinitions definitions, string mod, string repo, Action<bool, string> check)
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new FieldsOnly() });
        var runtimeHashes = JObject.Parse(File.ReadAllText(Path.Combine(repo, "assets/artwork-completion/runtime-hashes.json")));
        foreach (var entry in runtimeHashes.Properties().Where(p => p.Name.StartsWith("mods/" + mod + "/", StringComparison.Ordinal)))
        {
            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(repo, entry.Name)))).ToLowerInvariant();
            check(hash == (string)entry.Value!, "Direct/composed runtime image matches reviewed export: " + entry.Name);
        }
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(repo, "assets/artwork-completion/manifest.json")));
        foreach (var entry in manifest["assets"]!.Where(e => (string)e["mod"]! == mod && (string)e["status"]! == "selected"))
        {
            string id = (string)entry["definition"]!, path = (string)entry["runtime"]!;
            var owner = definitions.Objects[id];
            var item = definitions.Items[owner.strItemDef];
            check(item.strImg == path && item.strImgNorm == path + "Normal", "Dedicated artwork bound to live item: " + id);
            check(owner.strPortraitImg == path, "Dedicated inventory portrait: " + id);
            foreach (string suffix in new[] { "", "Normal" })
            {
                var png = File.ReadAllBytes(Path.Combine(repo, "mods", mod, "images", path + suffix + ".png"));
                int Dimension(int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
                check(Dimension(16) == (int)entry["nativeSize"]![0]! && Dimension(20) == (int)entry["nativeSize"]![1]!, "Dedicated image retains native size: " + id);
            }
        }
        // Exercise the shared binding against real physical definitions: only the
        // four named presentation fields may change, including repeat application.
        string prefix = mod == "PhobosAgriculture" ? "PhobosVerdemorrowFirstlight4" : "PhobosFurnace";
        foreach (int repeat in new[] { 1, 2 })
        {
            var owners = definitions.Objects.ToDictionary(p => p.Key, p => JObject.FromObject(p.Value, serializer));
            var items = definitions.Items.ToDictionary(p => p.Key, p => JObject.FromObject(p.Value, serializer));
            string basis = definitions.Items[definitions.Objects[prefix + "Installed"].strItemDef].strImg;
            ApplianceDefinitions.ApplyStateArtwork(definitions, prefix, basis);
            foreach (var pair in definitions.Objects)
            {
                var actual = JObject.FromObject(pair.Value, serializer); actual.Remove("strPortraitImg");
                owners[pair.Key].Remove("strPortraitImg");
                check(JToken.DeepEquals(owners[pair.Key], actual), "Artwork preserves native identity, placement, actions, conditions and economy: " + pair.Key);
            }
            foreach (var pair in definitions.Items)
            {
                var actual = JObject.FromObject(pair.Value, serializer);
                foreach (string field in new[] { "strImg", "strImgNorm", "strImgDamaged" }) { actual.Remove(field); items[pair.Key].Remove(field); }
                check(JToken.DeepEquals(items[pair.Key], actual), "Artwork preserves native sockets and item configuration: " + pair.Key);
            }
        }
    }
}
