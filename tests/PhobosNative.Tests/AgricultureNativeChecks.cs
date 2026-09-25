using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Registration;

internal static class AgricultureNativeChecks
{
    internal static void Run(NativeDefinitions d, string repo, Action<bool,string> check, Action<Action,string> throws)
    {
        foreach (var co in d.Objects.Values)
        foreach (string condition in co.aStartingConds)
        {
            string key = condition.Split('=')[0];
            check(d.Conditions.ContainsKey(key) || DataHandler.dictConds.ContainsKey(key), "Agriculture condition exists: " + key);
        }
        foreach (var pair in d.Items)
        {
            foreach (string image in new[] { pair.Value.strImg, pair.Value.strImgNorm })
                check(File.Exists(Path.Combine(repo, "mods/PhobosAgriculture/images", image + ".png")), "Agriculture image exists: " + image);
            check(pair.Value.aSocketAdds.Length == pair.Value.nCols * pair.Value.nCols, "Appliance footprint agrees with native tile additions");
        }
        foreach (var action in d.Interactions.Values)
            check(action.strRaiseUI == null, "Agriculture actions cannot raise nonexistent native prefabs");
        check(d.Triggers[d.Objects["PhobosCultivationInstalled"].strContainerCT].aTriggers.Contains("TIsWater"), "Rack accepts liquid water rather than inheriting a solids-only container");
        check(DataHandler.dictCOs["LiquidWater"].aStartingConds.Any(c => c.StartsWith("StatMass=") && Math.Abs(double.Parse(c.Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture) - .25) < 1e-7), "Manual water quantity matches installed native ration");
        var provider = new TestProvider("test.agriculture", "TestAgriculture");
        EquipmentProviders.Register(provider);
        check(ReferenceEquals(provider, EquipmentProviders.For("TestAgriculture")), "Registered equipment is discoverable");
        throws(() => EquipmentProviders.Register(new TestProvider("test.other", "TestAgriculture")), "Duplicate definition rejected without replacing owner");
        check(ReferenceEquals(provider, EquipmentProviders.For("TestAgriculture")), "Collision leaves original provider intact");
        EquipmentProviders.Unregister(provider.Id); check(EquipmentProviders.For("TestAgriculture") == null, "Absent optional provider leaves no stale registration");
    }
    private sealed class TestProvider : IEquipmentProvider
    {
        public string Id { get; }
        public IReadOnlyList<string> Definitions { get; }
        public TestProvider(string id, string definition) { Id = id; Definitions = Array.AsReadOnly(new[] { definition }); }
        public EquipmentSnapshot Snapshot(CondOwner equipment) => throw new NotSupportedException();
        public bool Command(CondOwner equipment, ConsoleBinding? binding, string action, out string message) { message = ""; return false; }
    }
}
