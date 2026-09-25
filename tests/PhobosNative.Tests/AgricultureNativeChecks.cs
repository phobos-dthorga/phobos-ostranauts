using System;
using Ostranauts.Trading;
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
        // Check assets selected by the shared panel/world policy, not just base definitions.
        foreach (string crop in new[] { "Potato", "Lettuce" })
        foreach (string stage in new[] { "sprout", "young", "mature", "harvest", "wilted", "dead" })
        foreach (string suffix in new[] { "", "Normal" })
        {
            string image = Path.Combine(repo, "mods/PhobosAgriculture/images/phobos/agriculture/Rack-" + crop + "-" + stage + suffix + ".png");
            check(File.Exists(image), "Registered crop stage exists: " + image);
            var png = File.ReadAllBytes(image);
            int Dimension(int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            check(Dimension(16) == 64 && Dimension(20) == 64, "Crop texture preserves the 4 x 4 native footprint");
        }
        check(d.Triggers[d.Objects["PhobosVerdemorrowFirstlight4Installed"].strContainerCT].aTriggers.Contains("TIsWater"), "Rack accepts liquid water rather than inheriting a solids-only container");
        check(DataHandler.dictCOs["LiquidWater"].aStartingConds.Any(c => c.StartsWith("StatMass=") && Math.Abs(double.Parse(c.Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture) - .25) < 1e-7), "Manual water quantity matches installed native ration");
        JsonCondOwner Definition(string id) => d.Objects.TryGetValue(id, out var definition) ? definition : DataHandler.dictCOs[id];
        double Mass(string id) => double.Parse(Definition(id).aStartingConds.Single(c => c.StartsWith("StatMass=")).Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture);
        foreach (string prefix in new[] { PhobosAgriculture.Definitions.Rack, PhobosAgriculture.Definitions.Cooker, PhobosAgriculture.IrrigationDefinitions.Supply, PhobosAgriculture.IrrigationDefinitions.Pipe })
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = prefix + form;
            var products = d.Installables[id + "Dismantle"].aLootCOs;
            check(Math.Abs(products.Sum(Mass) - Mass(id)) < 1e-7, "Agriculture salvage retains every kilogram: " + id);
            double scrap = products.Sum(p => EquipmentValueAudit.Price(Definition(p)));
            check(scrap * .5 < EquipmentValueAudit.Price(Definition(id), .99) * .4, "Salvage sale stays below worn whole sale across VORB discount endpoints: " + id);
        }
        var recipes = Newtonsoft.Json.JsonConvert.DeserializeObject<Phobos.Ostranauts.Framework.Construction.RecipePack>(File.ReadAllText(Path.Combine(repo, "mods/PhobosAgriculture/framework/recipes.json")))!;
        foreach (var recipe in recipes.recipes)
        {
            double inputs = recipe.ingredients.Sum(i => EquipmentValueAudit.Price(Definition(i.item)) * i.count);
            double outputs = recipe.outputs.Sum(o => EquipmentValueAudit.Price(Definition(o.item)) * o.count);
            check(outputs * .5 < inputs * 1.2, "New-material assembly does not profit even at favorable VORB discount endpoints: " + recipe.id);
        }
        var irrigation = Definition(PhobosAgriculture.Definitions.Irrigation);
        check(d.Objects[PhobosAgriculture.IrrigationDefinitions.Supply + "Installed"].aInteractions.Contains(PhobosAgriculture.Definitions.WorkId("load-nutrients")), "W2 exposes native crew nutrient loading");
        check(Math.Abs(Mass(irrigation.strName) - PhobosAgriculture.Definitions.IrrigationKg) < 1e-7, "Irrigation transfer uses the physical commodity mass");
        check(!DataHandler.dictCTs["TIsWater"].TriggeredDataCO(new DataCO(irrigation), false), "Root-water charge cannot impersonate native drinking water");
        check(!irrigation.aStartingConds.Any(c => c.StartsWith("IsEdible=") || c.StartsWith("IsHydrator=")), "Irrigation cannot grant food or hydration");
        var pipe = d.Items[PhobosAgriculture.IrrigationDefinitions.Pipe + "Installed"];
        var pipeAdds = d.Loot[pipe.aSocketAdds.Single()];
        check(pipe.bHasSpriteSheet && d.Triggers.ContainsKey(pipe.ctSpriteSheet), "Pipe uses native cardinal auto-tiling with its own trigger");
        check(pipeAdds.aCOs.All(c => !c.Contains("Power")), "Water pipes never participate in native electrical routing");
        foreach (var socket in d.Items.Values.SelectMany(i => i.aSocketAdds).Distinct().Where(d.Loot.ContainsKey))
        {
            var loot = d.Loot[socket];
            check(loot.strType == "condition", "Native tile additions must apply conditions rather than resolve condition names as triggers: " + socket);
            check(loot.aCOs.All(c => d.Conditions.ContainsKey(c.Split('=')[0]) || DataHandler.dictConds.ContainsKey(c.Split('=')[0])), "Tile additions reference real conditions");
            check(loot.aLoots.All(c => DataHandler.dictLoot.ContainsKey(c.Split('=')[0]) || d.Loot.ContainsKey(c.Split('=')[0])), "Nested tile additions retain real native fixture sockets");
        }
        check(pipe.aSocketReqs[4] == "TILFloor" && pipe.aSocketForbids[4] == PhobosAgriculture.IrrigationDefinitions.Pipe + "Off", "Pipe requires floor and rejects duplicate pipe, independent of power sockets");
        check(d.Objects[PhobosAgriculture.IrrigationDefinitions.Pipe + "Installed"].jsonPI == null, "A pipe cannot draw or distribute electricity");
        check(d.Objects[PhobosAgriculture.IrrigationDefinitions.Supply + "Installed"].mapPoints.Contains(PhobosAgriculture.IrrigationDefinitions.Outlet + ",24,8"), "Supply outlet has a rotating native named point");
        check(d.Objects[PhobosAgriculture.Definitions.Rack + "Installed"].mapPoints.Contains(PhobosAgriculture.IrrigationDefinitions.Inlet + ",-40,8"), "Rack inlet is outside its unchanged four-tile footprint");
        check(Math.Abs(PhobosAgriculture.IrrigationDefinitions.CapacityKg - PhobosAgriculture.Core.CropState.ReservoirKg) < 1e-9, "Shared empty-appliance reservoir schema retains its existing capacity bound");
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
