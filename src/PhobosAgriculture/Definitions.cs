using System;
using Ostranauts.Trading;
using PhobosAgriculture.Core;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal static class Definitions
{
    internal const string Rack = "PhobosVerdemorrowFirstlight4", Cooker = "PhobosVerdemorrowHearth2", Controls = "PhobosAgricultureControls";
    internal const double RackKg = 80, CookerKg = 12;
    // What each inventory is for (Framework 0.70.0), fitted in Agriculture 0.37.0 now that harvests arrive as stacks: the
    // rack holds two harvests (ten portions to a cell), its seed and residue, and about six kinds of supply; the cooker a
    // stack of raw portions, a stack of meals and two cells to spare. Both were 8 x 8.
    internal static readonly InventorySpec RackInventory = InventorySpec.ProductTray(4, 3, Rack + "Supplies"), CookerInventory = InventorySpec.ProductTray(2, 2);
    /// <summary>Masses the crop model is written for; the materials pack is bound to them.</summary>
    internal const double IrrigationKg = 5, NutrientKg = .04;
    // Planting stock, produce and foods are named by the crops data pack (Agriculture 0.40.0), not here.
    internal const string Nutrient = "PhobosVerdemorrowGroundworkNutrients", Residue = "PhobosVerdemorrowCropResidue", Drainage = "PhobosVerdemorrowProcessSolution";
    internal const string Irrigation = "PhobosVerdemorrowGroundworkIrrigation";
    internal static bool Ready;
    internal const string PlantPrefix = "plant-", MixPrefix = "mix-";
    /// <summary>The crew work actions: one planting action per crop in the crops pack, between the fixed ones.</summary>
    internal static string[] Work = BuildWork();
    private static string[] BuildWork() => WorkupDefinitions.Work.Concat(Crops.All.Select(c => PlantPrefix + c.Id))
        .Concat(new[] { "load-water", "load-irrigation", "load-nutrients", "recover-solution", "harvest", "pick", "clear", "drain" }).ToArray();
    internal static string WorkId(string action) => "PhobosAgricultureWork_" + action.Replace('-', '_');
    /// <summary>Work action ids to their actions, and the consumable supplies, built for the interaction hooks that run
    /// on every native offer and completion; rebuilt when the crops pack is loaded.</summary>
    internal static System.Collections.Generic.Dictionary<string, string> WorkIds = Work.ToDictionary(WorkId, a => a, StringComparer.Ordinal);
    /// <summary>The crop a planting action names, or null.</summary>
    internal static Crop? PlantCrop(string action) => action.StartsWith(PlantPrefix, StringComparison.Ordinal) ? Crops.Find(action.Substring(PlantPrefix.Length)) : null;
    /// <summary>The W2's feed actions: one per crop, then plain water.</summary>
    internal static string[] MixActions => Crops.All.Select(c => MixPrefix + c.Id).Concat(new[] { "water-only" }).ToArray();
    internal static readonly System.Collections.Generic.HashSet<string> Supplies = new(new[] { Nutrient, BulkDefinitions.Nutrients, WorkupDefinitions.Makeup, WorkupDefinitions.Mixture, WorkupDefinitions.Concentrate, Service.RecoveryCartridge }, StringComparer.Ordinal);
    /// <summary>The vanilla direct-eating reply our food replies are cloned from, and the native openers that list it.</summary>
    internal const string EatTemplate = "SeekFoodAllowDirect";
    internal static readonly string[] EatOpeners = { "SeekFoodDirect", "SeekFoodDirectLowNeed", "SeekFoodDirectGlutton", "SeekConsumeFoodAirtight" };
    private static readonly System.Collections.Generic.HashSet<string> Machines = new(new[] { Rack, Cooker, IrrigationDefinitions.Supply, WorkupDefinitions.Bench }.SelectMany(prefix => new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" }.Select(form => prefix + form)), StringComparer.Ordinal);
    internal static bool Machine(CondOwner? co) => co != null && Machines.Contains(co.strCODef);
    internal static bool MachineDefinition(string? id) => id != null && Machines.Contains(id);
    /// <summary>Every tradeable Agriculture machine family (the R3 retired into Framework's S3 in Agriculture 0.31.0).</summary>
    internal static readonly string[] MachineFamilies = { Rack, Cooker, IrrigationDefinitions.Supply, WorkupDefinitions.Bench };
    internal static bool IsCooker(CondOwner co) => co.strCODef.StartsWith(Cooker, StringComparison.Ordinal);
    internal static double DryMass(CondOwner co) => WorkupDefinitions.IsBench(co) ? WorkupDefinitions.DryKg : IrrigationDefinitions.IsSupply(co) ? IrrigationDefinitions.DryKg : IsCooker(co) ? CookerKg : RackKg;
    internal static void Load()
    {
        Ready = false;
        var mod = DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Agriculture" && !m.GetIsDisabled());
        if (mod == null) throw new InvalidOperationException(Text.Get("missing_package"));
        var prepared = Prepare(Plugin.LootEnabled.Value, Plugin.LootMultiplier.Value); prepared.Publish(); ConstructionRegistry.RegisterPack(Plugin.Id, Path.Combine(mod.GetDirectory(), "framework", "recipes.json"));
    }
    internal static NativeDefinitions Prepare(bool lootEnabled = true, double lootMultiplier = LootContent.DefaultMultiplier)
    {
        var d = new NativeDefinitions();
        AgricultureVessels.Load();
        AgricultureMaterials.Load();
        // Crops and cooker recipes are data packs (Agriculture 0.40.0); the planting actions follow the crops.
        Crops.Load(); HearthRecipes.Load();
        Work = BuildWork(); WorkIds = Work.ToDictionary(WorkId, a => a, StringComparer.Ordinal);
        AgricultureEconomy.Load(NativeMass, id => DataHandler.dictLoot != null && DataHandler.dictLoot.ContainsKey(id));
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("controls"); controls.strDesc = controls.strTooltip = Text.Get("controls"); controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        if (RecyclerCapture.Available)
        {
            var capture = NativeDefinitions.Clone(controls); capture.strName = RecyclerCapture.Controls; capture.strTitle = capture.strTooltip = Text.Get("capture_controls");
            d.Interactions[capture.strName] = capture;
            // Ship's Water owns its recycler definitions: the capture control is appended to them in place at
            // publication, never by republishing them under their own names.
            var trigger = NativeDefinitions.Trigger(RecyclerCapture.RecyclerTrigger);
            if (trigger != null) foreach (var original in DataHandler.dictCOs.Values.Where(c => trigger.TriggeredDataCO(new DataCO(c), false)).ToArray())
                d.Amend(() => DefinitionAmendments.AppendInteractions(original, capture.strName));
        }
        foreach (string action in Work)
        {
            Phobos.Ostranauts.Framework.Crew.CrewSpecialities.RegisterPractical(WorkId(action), "Agriculture");
            var work = NativeDefinitions.Clone(controls); work.strName = WorkId(action); work.strTitle = work.strTooltip = Text.Action(action);
            work.fDuration = WorkupDefinitions.IsWork(action) ? 1d / 60 : action == "harvest" ? .5 : action.StartsWith("load-", StringComparison.Ordinal) ? 10d / 3600 : .25; work.strAnim = "Tablet"; work.strActionGroup = "Work";
            d.Interactions[work.strName] = work;
        }
        ApplianceDefinitions.Add(d, Rack, Text.Get("rack"), Text.Get("rack_desc"), 4, RackKg, AgricultureEconomy.Price(Rack), "phobos/agriculture/Rack", Controls, .02);
        ApplianceDefinitions.Add(d, Cooker, Text.Get("cooker"), Text.Get("cooker_desc"), 2, CookerKg, AgricultureEconomy.Price(Cooker), "phobos/agriculture/Cooker", Controls, .02);
        // The ordinary solid-inventory trigger rejects native liquid rations. The
        // rack's contained supply cassette explicitly accepts water as well.
        d.Triggers[Rack + "Supplies"] = new CondTrigger { strName = Rack + "Supplies", fChance = 1, fCount = 1, bAND = false,
            aReqs = Array.Empty<string>(), aForbids = new[] { "IsInstalled", "IsCumbersome", "IsOversized" }, aTriggers = new[] { "TIsFitContainerSolid", "TIsWater" } };
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Rack, StringComparison.Ordinal))) co.strContainerCT = Rack + "Supplies";
        EquipmentInventory.Apply(d, Rack, RackInventory);
        EquipmentInventory.Apply(d, Cooker, CookerInventory);
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Rack) && c.strName.EndsWith("Installed"))) co.aInteractions = co.aInteractions.Concat(Work.Where(a=>a!="recover-solution" && !WorkupDefinitions.IsWork(a)).Select(WorkId)).ToArray();
        IrrigationDefinitions.Add(d);
        WorkupDefinitions.Add(d);
        BulkDefinitions.Add(d);
        HopperDefinitions.Add(d);
        Stock(d, RecyclerCapture.Wet, "wet_rejects");
        foreach (var co in d.Objects.Values.Where(c => c.strName.EndsWith("Dmg"))) co.strNameFriendly = co.strNameShort = Text.Get("damaged", co.strNameFriendly);
        foreach (var item in Crops.Items) Stock(d, item.Key, item.Value.text);
        // Materials a data file added (Agriculture 0.48.0) that no crop item entry names: plain loose stock. Added trash
        // is terminal by the schema's rule, and so a declared remainder.
        foreach (var added in AgricultureMaterials.Added)
        {
            if (!Crops.Pack.items.ContainsKey(added.Key)) Stock(d, added.Key, "");
            if (added.Value.terminal) Phobos.Ostranauts.Framework.Registration.Remainders.Declare(added.Key);
        }
        Stock(d, Nutrient, "nutrients"); Stock(d, Residue, "residue");
        Stock(d, Drainage, "drainage");
        Stock(d, Service.CharacterizedDrainage, "characterized_drainage");
        Stock(d, Service.RecoveryReject, "recovery_reject");
        // Owner rule (4 October 2026): the treatment rejects and the recycler's wet rejects, which no recipe takes, are declared remainders.
        Phobos.Ostranauts.Framework.Registration.Remainders.Declare(Service.RecoveryReject, RecyclerCapture.Wet);
        Stock(d, Service.RecoveryCartridge, "recovery_cartridge");
        WorkupDefinitions.AddFeedIdentities(d);
        foreach (var eaten in Crops.Items.Where(i => i.Value.hunger != null))
        {
            string food = eaten.Key;
            d.Loot[food + "Effects"] = new Loot { strName = food + "Effects", strType = "trigger", aCOs = new[] { "TDnFood=1x" + eaten.Value.hunger, "TUpSatiety=1x" + eaten.Value.satiety, "TDnTeethBrushed=1x1" }, aLoots = Array.Empty<string>() };
            // Authored food values reach the crew through the game's own direct-eating chain: an identity
            // condition on the food, a trigger on it and a reply cloned from the vanilla one that carries our
            // effects, inserted ahead of the vanilla replies in every native seek-food opener.
            string identity = "Is" + food, trigger = "TIs" + food, reply = food + "AllowDirect";
            var mark = NativeDefinitions.Clone(DataHandler.dictConds["IsFood"]); mark.strName = identity; d.Conditions[identity] = mark;
            d.Objects[food].aStartingConds = d.Objects[food].aStartingConds.Concat(new[] { identity + "=1x1" }).ToArray();
            d.Triggers[trigger] = new CondTrigger { strName = trigger, fChance = 1, fCount = 1, bAND = true, aReqs = new[] { identity }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
            var eat = NativeDefinitions.Clone(DataHandler.dictInteractions[EatTemplate]); eat.strName = reply; eat.CTTestUs = trigger; eat.LootCTsThem = food + "Effects";
            d.Interactions[reply] = eat;
            d.Amend(() => { foreach (string parent in EatOpeners) if (DataHandler.dictInteractions.TryGetValue(parent, out var opener)) DefinitionAmendments.InsertInverse(opener, reply, name => name == "SeekFoodAllowDirectPrepared" || name == EatTemplate); });
        }
        Stock(d, Irrigation, "irrigation");
        EquipmentEconomy.Apply(d);
        LootContent.Add(d, lootEnabled, lootMultiplier);
        RegionalEconomy.Apply(d);
        foreach (var art in new[] { (Rack, "Rack"), (Cooker, "Cooker"), (IrrigationDefinitions.Supply, "WaterSupply"), (WorkupDefinitions.Bench, "Workup") })
            ApplianceDefinitions.ApplyStateArtwork(d, art.Item1, "phobos/agriculture/" + art.Item2);
        // Large rack housings remain bulky after dismantling; small loose supplies do not.
        foreach (string id in new[] { Rack + "HousingWaste", Rack + "BrokenHousingWaste" }.Concat(BulkDefinitions.Sizes.SelectMany(s => new[] { s.Prefix + "HousingWaste", s.Prefix + "BrokenHousingWaste" })))
            ItemHandling.Cumbersome(d, id);
        ItemHandling.Apply(d);
        MaintenanceInformation.Register(d, "PhobosAgricultureMaintenanceInformation", co =>
            ContentsEligibilityPatch.RemovalReason(co) ?? Text.Get("Maintenance.ready"));
        return d;
    }
    /// <summary>A native definition's starting mass, as the game writes it (StatMass=1xN), for the economy pack's salvage check.</summary>
    internal static double? NativeMass(string id)
    {
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(id, out var co)) return null;
        foreach (string cond in co.aStartingConds ?? Array.Empty<string>())
        {
            if (!cond.StartsWith("StatMass=", StringComparison.Ordinal)) continue;
            string amount = cond.Substring(cond.IndexOf('x') + 1);
            return double.TryParse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double kg) ? kg : null;
        }
        return null;
    }
    /// <summary>A loose item from the materials pack: mass, price, stack, market category and art come from its entry;
    /// the translation key, the donor (a ration for food, scrap for everything else) and the identity stay here.</summary>
    /// <summary>Nutrient charges, whose price is what is left in them at the pack's price per kilogram.</summary>
    internal static bool PricedByMass(string id) => id == Nutrient || id == BulkDefinitions.Nutrients || id == WorkupDefinitions.Makeup;
    internal static void Stock(NativeDefinitions d, string id, string key)
    {
        var entry = AgricultureMaterials.Entry(id); bool food = entry.kind == AgricultureMaterials.Food;
        double kg = entry.kg, price = entry.price; string? artKey = entry.art;
        var co = NativeDefinitions.Clone(DataHandler.dictCOs[food ? "ItmTrencherAcceptableAlgae" : "ItmScrapTrash"]);
        // An added material (0.48.0) carries its own name, text and picture, which an add-on's translation may replace.
        bool added = AgricultureMaterials.IsAdded(id);
        co.strName = id; co.strNameFriendly = co.strNameShort = added ? Phobos.Ostranauts.Framework.Localization.Translations.Get(Text.Owner, "Material." + id, entry.name ?? id) : Text.Get(key);
        co.strDesc = added ? Phobos.Ostranauts.Framework.Localization.Translations.Get(Text.Owner, "Material." + id + "_description", entry.description ?? "") : Text.Get(key + "_desc");
        co.nStackLimit = entry.stack; co.aUpdateCommands = Array.Empty<string>(); co.aTickers = Array.Empty<string>(); co.inventoryWidth = co.inventoryHeight = 1;
        co.aStartingConds = food ? new[] { "IsSolid=1x1", "IsEdible=1x1", "IsFood=1x1", "IsCategoryFood=1x1", "IsPocketable=1x1" } : new[] { "IsSolid=1x1", "IsPocketable=1x1" };
        if (entry.category != null && !co.aStartingConds.Contains(entry.category + "=1x1")) co.aStartingConds = co.aStartingConds.Concat(new[] { entry.category + "=1x1" }).ToArray();
        MaintenanceDefinitions.SetStat(co, "StatMass", kg); MaintenanceDefinitions.SetStat(co, "StatBasePrice", price);
        // Economy audit (Agriculture 0.52.0): saved meals, produce and nutrient charges take the pack's current price on
        // every load, a part-used charge in proportion to what is left in it, so a repricing needs no manual step.
        if (PricedByMass(id)) EquipmentSaveUpgrade.FollowPrice(id, byMass: true);
        else if (food) EquipmentSaveUpgrade.FollowPrice(id);
        // Keep the donor's native item behavior and socket geometry, but give each
        // commodity its own registered image. Saved commodity identities stay fixed.
        var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[co.strItemDef]);
        string image = added ? entry.image! : "phobos/agriculture/Stock-" + (artKey ?? key);
        co.strItemDef = item.strName = id;
        co.strPortraitImg = item.strImg = image;
        item.strImgNorm = image + "Normal";
        item.strImgDamaged = "blank";
        d.Items[id] = item;
        d.Objects[id] = co;
    }
}
