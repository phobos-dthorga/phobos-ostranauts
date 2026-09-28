using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Prepare private definitions; publish together before native installable generation.</summary>
public sealed class NativeDefinitions
{
    public readonly Dictionary<string, JsonCondOwner> Objects = new Dictionary<string, JsonCondOwner>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonItemDef> Items = new Dictionary<string, JsonItemDef>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonSlot> Slots = new Dictionary<string, JsonSlot>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonCond> Conditions = new Dictionary<string, JsonCond>(StringComparer.Ordinal);
    public readonly Dictionary<string, CondTrigger> Triggers = new Dictionary<string, CondTrigger>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonPowerInfo> Power = new Dictionary<string, JsonPowerInfo>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonInteraction> Interactions = new Dictionary<string, JsonInteraction>(StringComparer.Ordinal);
    public readonly Dictionary<string, Loot> Loot = new Dictionary<string, Loot>(StringComparer.Ordinal);
    public readonly Dictionary<string, JsonInstallable> Installables = new Dictionary<string, JsonInstallable>(StringComparer.Ordinal);
    /// <summary>Loot branches this set links into native tables in place, by table id (see AdditiveLoot).</summary>
    public readonly Dictionary<string, HashSet<string>> LootBranches = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private readonly List<Action> amendments = new List<Action>();

    /// <summary>Queue an additive, idempotent in-place change to a definition the game or another mod
    /// owns (see DefinitionAmendments). It runs after this set's own definitions commit, so a failed
    /// commit amends nothing. Never clone and republish a native definition by name instead.</summary>
    public void Amend(Action amendment)
    {
        if (amendment == null) throw new ArgumentNullException(nameof(amendment));
        amendments.Add(amendment);
    }

    public void Publish()
    {
        InstallMenu.Validate(Installables.Values);
        foreach (var trigger in Triggers.Values) Validate(trigger);
        var batch = new DefinitionTransaction();
        batch.Stage(DataHandler.dictCOs, Objects); batch.Stage(DataHandler.dictItemDefs, Items);
        batch.Stage(DataHandler.dictSlots, Slots); batch.Stage(DataHandler.dictConds, Conditions);
        batch.Stage(DataHandler.dictCTs, Triggers); batch.Stage(DataHandler.dictPowerInfo, Power);
        batch.Stage(DataHandler.dictInteractions, Interactions); batch.Stage(DataHandler.dictLoot, Loot);
        batch.Stage(DataHandler.dictInstallables, Installables);
        batch.Commit();
        foreach (var amendment in amendments) amendment();
    }

    /// <summary>A native trigger by name, or null when none exists. The game's own lookup returns its
    /// always-true Blank trigger for an unknown name, which must never gate an optional provider.</summary>
    public static CondTrigger? Trigger(string name) =>
        !string.IsNullOrEmpty(name) && DataHandler.dictCTs != null && DataHandler.dictCTs.ContainsKey(name) ? DataHandler.GetCondTrigger(name) : null;

    /// <summary>A trigger built in code starts with a zero chance and null lists, unlike one loaded
    /// from JSON; the game rolls against that chance, so such a trigger never passes.</summary>
    public static void Validate(CondTrigger trigger)
    {
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (!(trigger.fChance > 0)) throw new ArgumentException(Text.Get("NativeDefinitions.trigger_never_passes", trigger.strName, trigger.fChance));
        trigger.aReqs ??= Array.Empty<string>(); trigger.aForbids ??= Array.Empty<string>();
        trigger.aTriggers ??= Array.Empty<string>(); trigger.aTriggersForbid ??= Array.Empty<string>();
        trigger.aLowerConds ??= Array.Empty<string>();
    }

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
        TypeNameHandling = TypeNameHandling.None,
        Converters = new List<JsonConverter> { new VectorConverter() },
        ContractResolver = new DefinitionProperties()
    };
    public static T Clone<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, Settings), Settings)!;

    private sealed class VectorConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => type == typeof(Vector3);
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var v = (Vector3)value!;
            writer.WriteStartObject(); writer.WritePropertyName("x"); writer.WriteValue(v.x);
            writer.WritePropertyName("y"); writer.WriteValue(v.y); writer.WritePropertyName("z");
            writer.WriteValue(v.z); writer.WriteEndObject();
        }
        public override object ReadJson(JsonReader reader, Type type, object? existing, JsonSerializer serializer)
        {
            var v = JObject.Load(reader);
            return new Vector3((float)v["x"]!, (float)v["y"]!, (float)v["z"]!);
        }
    }
    private sealed class DefinitionProperties : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization) =>
            base.CreateProperties(type, serialization).Where(p => p.Readable && p.Writable).ToList();
    }
}
