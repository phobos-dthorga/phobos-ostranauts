using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Local panel and C1 presentation of the five machines through Framework's equipment-provider
/// contract. Choices are content-owned; every application goes through the checked services.</summary>
internal sealed class Provider : IEquipmentProvider, IEquipmentPanelFields
{
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(ChargeMachines.All.Select(m => m.Spec.Installed).Concat(new[] { ProcessorRules.Installed, SabatierRules.Installed, CrackerRules.Installed, ManifoldRules.Installed, FillerRules.Installed, RegulatorRules.Installed, BottlerRules.Installed, FeederRules.Installed })
        .Concat(GasStores.All.Select(s => s.Installed)).Concat(LiquidStores.All.Select(s => s.Installed)).SelectMany(id => new[] { id, id + "Dmg" }).ToArray());
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    /// <summary>Every snapshot group this provider reports; each has a "Group." name in the catalogue, registered with
    /// Framework so consoles that list every mod's equipment show them in our words.</summary>
    internal static readonly string[] Groups = { "refinery", "leach", "acid-plant", "fermenter", "electrolysis-cell", "carbothermal-reactor", "bottler", "processor", "filler", "regulator", "manifold", "feeder", "reactor", "cracker", "store" };
    /// <summary>A link field: the linked object's name, every candidate and None, with the current link marked.</summary>
    internal static EquipmentField LinkField(string label, string prefix, string? peer, IEnumerable<CondOwner> candidates, Func<string>? note = null) =>
        LinkField(label, prefix, peer, candidates.Select(v => (v, ObjectPresentation.Name(v))), note);
    /// <summary>A link field whose choices carry their own labels (how each is reached, and whether it is full or empty).
    /// The note, read when the sheet opens, says why something aboard is not among them (Framework 0.69.0).</summary>
    internal static EquipmentField LinkField(string label, string prefix, string? peer, IEnumerable<(CondOwner Vessel, string Label)> candidates, Func<string>? note = null)
    {
        string linked = string.IsNullOrEmpty(peer) || peer == "none" ? "none" : peer!;
        return new(label, ObjectPresentation.Name(peer ?? ""), candidates.Select(c => (prefix + c.Vessel.strID, c.Label)).Concat(new[] { (prefix + "none", Text.Get("Provider.link_none")) }), prefix + linked, note);
    }
    /// <summary>A shared-vessel link field: every candidate the link reaches, labelled with how, and a note for every
    /// vessel of the link's cargo aboard that is not offered.</summary>
    internal static EquipmentField LinkField(string label, string prefix, CondOwner co, VesselLink link, bool deposit, IEnumerable<CondOwner> candidates)
    {
        var offered = candidates.ToArray();
        return LinkField(label, prefix, link.PeerId(co), offered.Select(v => (v, LinkChoices.Label(co, v, link, deposit))), () => LinkChoices.Note(co, link, offered));
    }
    // The note for a canister field: the game's canisters and the bulk stores of each gas aboard that are not offered.
    // The game's canisters have no gas-line fitting, so the note says they must stand within one tile (Manufacturing 0.54.0;
    // until then they were left out of the note).
    private static Func<string> StoreNote(CondOwner co, IEnumerable<CondOwner> canisters, IEnumerable<CondOwner> offered, params string[] commodities) =>
        () => LinkChoices.Note(co, LineFamilies.Gas, canisters.Concat(commodities.SelectMany(c => BulkVessels.AboardAnyState(co.ship, c))), offered);
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (ChargeMachines.For(co.strCODef) is ChargeMachine charge)
        {
            foreach (var field in charge.Fields(co)) yield return field;
        }
        else if (ProcessorRules.IsFamily(co.strCODef))
        {
            yield return LinkField(Text.Get("Provider.water_field"), "water:", co, ProcessorService.WaterLink, false, ProcessorService.WaterCandidates(co));
            yield return LinkField(Text.Get("Provider.store_field"), "store:", co, ProcessorService.HydrogenLink, true, ProcessorService.StoreCandidates(co));
            yield return new(Text.Get("Provider.canister_field"), ProcessorService.CanisterName(co),
                ProcessorService.CanisterCandidates(co).Select(c => ("canister:" + c.strID, LinkChoices.Label(co, c, LineFamilies.Gas, true))).Concat(new[] { ("canister:none", Text.Get("Processor.cabin")) }),
                "", StoreNote(co, ProcessorService.CanistersAboard(co, ProcessorRules.CanisterTrigger), ProcessorService.CanisterCandidates(co), ManufacturingRules.Oxygen));
        }
        else if (FeederRules.IsFamily(co.strCODef))
        {
            foreach (var field in FeederService.Fields(co)) yield return field;
        }
        else if (BottlerRules.IsFamily(co.strCODef))
        {
            // The Corker-2 (Manufacturing 0.40.0): an ethanol cask and a water vessel, each touching it or on its line.
            yield return LinkField(Text.Get("Provider.ethanol_field"), "ethanol:", co, BottlerService.EthanolLink, false, BottlerService.EthanolLink.Candidates(co));
            yield return LinkField(Text.Get("Provider.vessel_field"), "water:", co, BottlerService.WaterLink, false, BottlerService.WaterLink.Candidates(co));
            yield return Phobos.Ostranauts.Framework.Inventory.StoreDelivery.Field(co);
        }
        else if (SabatierRules.IsFamily(co.strCODef))
        {
            yield return LinkField(Text.Get("Provider.hydrogen_field"), "hydrogen:", co, SabatierService.HydrogenLink, false, SabatierService.HydrogenCandidates(co));
            yield return new(Text.Get("Provider.co2_field"), SabatierService.CanisterName(co),
                SabatierService.CanisterCandidates(co).Select(c => ("canister:" + c.strID, LinkChoices.Label(co, c, LineFamilies.Gas, false))).Concat(new[] { ("canister:none", Text.Get("Provider.link_none")) }),
                "", StoreNote(co, ProcessorService.CanistersAboard(co, SabatierRules.CanisterTrigger), SabatierService.CanisterCandidates(co),
                    ManufacturingRules.CarbonDioxide, ManufacturingRules.CarbonMonoxide));
            yield return LinkField(Text.Get("Provider.water_out_field"), "water:", co, SabatierService.WaterLink, true, SabatierService.WaterCandidates(co));
            yield return LinkField(Text.Get("Provider.methane_field"), "methane:", co, SabatierService.MethaneLink, true, SabatierService.MethaneCandidates(co));
        }
        else if (CrackerRules.IsFamily(co.strCODef))
        {
            yield return LinkField(Text.Get("Provider.ammonia_field"), "ammonia:", co, CrackerService.LinkOf(CrackerService.Link.Ammonia), false, CrackerService.AmmoniaCandidates(co));
            yield return LinkField(Text.Get("Provider.nitrogen_out_field"), "nitrogen:", co, CrackerService.LinkOf(CrackerService.Link.Nitrogen), true, CrackerService.NitrogenCandidates(co));
            yield return LinkField(Text.Get("Provider.hydrogen_out_field"), "hydrogen:", co, CrackerService.LinkOf(CrackerService.Link.Hydrogen), true, CrackerService.HydrogenCandidates(co));
        }
        else if (ManifoldRules.IsFamily(co.strCODef))
        {
            yield return new(Text.Get("Provider.feed_field"), Text.Get(ManifoldService.On(co) ? "Provider.on" : "Provider.off"),
                new[] { ("feed:on", Text.Get("Provider.on")), ("feed:off", Text.Get("Provider.off")) }, ManifoldService.On(co) ? "feed:on" : "feed:off");
            yield return new(Text.Get("Provider.order_field"), Text.Get(ManifoldService.First(co) ? "Provider.order_first" : "Provider.order_last"),
                new[] { ("order:first", Text.Get("Provider.order_first")), ("order:last", Text.Get("Provider.order_last")) }, ManifoldService.First(co) ? "order:first" : "order:last");
            foreach (var source in ManifoldService.Sources(co))
                yield return new(ObjectPresentation.Name(source.Id), Text.Get(source.Enabled ? "Provider.on" : "Provider.off"),
                    new[] { ("source-on:" + source.Id, Text.Get("Provider.on")), ("source-off:" + source.Id, Text.Get("Provider.off")), ("unlink:" + source.Id, Text.Get("Provider.unlink")) });
            if (ManifoldService.Sources(co).Count < ManifoldRules.MaxSources)
                yield return new(Text.Get("Provider.add_source_field"), Text.Get("Provider.link_none"),
                    ManifoldService.Candidates(co).Where(c => ManifoldService.Sources(co).All(x => x.Id != c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))),
                    "", () => GasLine.Note(co, _ => true));
        }
        else if (FillerRules.IsFamily(co.strCODef))
        {
            var state = FillerService.StateOf(co);
            yield return new(Text.Get("Provider.mode_field"), Text.Get(state.Mode == FillerMode.Decant ? "Provider.mode_decant" : "Provider.mode_fill"),
                new[] { ("mode:fill", Text.Get("Provider.mode_fill")), ("mode:decant", Text.Get("Provider.mode_decant")) }, state.Mode == FillerMode.Decant ? "mode:decant" : "mode:fill");
            foreach (var link in state.Links)
            {
                var choices = new List<(string, string)>();
                if (link.Kind == FillerLinkKind.Store) choices.AddRange(new[] { ("source-on:" + link.Id, Text.Get("Provider.on")), ("source-off:" + link.Id, Text.Get("Provider.off")) });
                else choices.AddRange(new[] { ("target:" + link.Id, Text.Get("Provider.role_target")), ("draw:" + link.Id, Text.Get("Provider.role_source")) });
                choices.Add(("unlink:" + link.Id, Text.Get("Provider.unlink")));
                string current = link.Kind == FillerLinkKind.Store ? Text.Get(link.Enabled ? "Provider.on" : "Provider.off")
                    : Text.Get(link.Kind == FillerLinkKind.Source ? "Provider.role_source" : "Provider.role_target");
                yield return new(ObjectPresentation.Name(link.Id), current, choices);
            }
            var linked = new HashSet<string>(state.Links.Select(l => l.Id), StringComparer.Ordinal);
            if (state.OfKind(FillerLinkKind.Store).Count() < FillerRules.MaxStores)
                yield return new(Text.Get("Provider.add_source_field"), Text.Get("Provider.link_none"),
                    FillerService.StoreCandidates(co).Where(c => !linked.Contains(c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))),
                    "", () => GasLine.Note(co, FillerService.Fills));
            if (state.Links.Count(l => l.Kind != FillerLinkKind.Store) < FillerRules.MaxCanisters)
                yield return new(Text.Get("Provider.add_canister_field"), Text.Get("Provider.link_none"),
                    FillerService.CanisterCandidates(co).Where(c => !linked.Contains(c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))),
                    "", () => LinkChoices.Note(co, LineFamilies.Gas, FillerService.CanistersAboard(co).Where(c => !linked.Contains(c.strID)), FillerService.CanisterCandidates(co)));
        }
        else if (RegulatorRules.IsFamily(co.strCODef))
        {
            var state = RegulatorService.StateOf(co);
            yield return new(Text.Get("Provider.o2_target_field"), Text.Get("Regulator.kpa", state.OxygenKPa),
                RegulatorRules.OxygenTargets.Select(v => ("o2:" + N(v), Text.Get("Regulator.kpa", v))), "o2:" + N(state.OxygenKPa));
            yield return new(Text.Get("Provider.pressure_target_field"), state.PressureKPa > 0 ? Text.Get("Regulator.kpa", state.PressureKPa) : Text.Get("Regulator.pressure_off"),
                RegulatorRules.PressureTargets.Select(v => ("pressure:" + N(v), v > 0 ? Text.Get("Regulator.kpa", v) : Text.Get("Regulator.pressure_off"))), "pressure:" + N(state.PressureKPa));
            yield return LinkField(Text.Get("Provider.oxygen_store_field"), "oxygen:", state.OxygenStore, RegulatorService.Candidates(co, ManufacturingRules.Oxygen),
                () => GasLine.Note(co, c => GasStores.Holds(c.strCODef, ManufacturingRules.Oxygen)));
            yield return LinkField(Text.Get("Provider.nitrogen_store_field"), "nitrogen:", state.NitrogenStore, RegulatorService.Candidates(co, ManufacturingRules.Nitrogen),
                () => GasLine.Note(co, c => GasStores.Holds(c.strCODef, ManufacturingRules.Nitrogen)));
            // Carbon dioxide for grow rooms (Manufacturing 0.27.0).
            yield return new(Text.Get("Provider.co2_target_field"), state.CarbonDioxideKPa > 0 ? Text.Get("Regulator.co2_kpa", state.CarbonDioxideKPa) : Text.Get("Regulator.pressure_off"),
                RegulatorRules.CarbonDioxideTargets.Select(v => ("co2:" + N(v), v > 0 ? Text.Get("Regulator.co2_kpa", v) : Text.Get("Regulator.pressure_off"))), "co2:" + N(state.CarbonDioxideKPa));
            yield return LinkField(Text.Get("Provider.co2_store_field"), "carbon-dioxide:", state.CarbonDioxideStore, RegulatorService.Candidates(co, ManufacturingRules.CarbonDioxide),
                () => GasLine.Note(co, c => GasStores.Holds(c.strCODef, ManufacturingRules.CarbonDioxide)));
        }
        else if (LiquidStores.IsFamily(co.strCODef) && !BulkVessel.Protected(co))
        {
            // Shown whenever another tank of the liquid is aboard, in reach or not, so the sheet can say what keeps it out.
            var targets = LiquidStoreService.PourTargets(co).ToArray();
            var tanks = BulkVessels.AboardAnyState(co.ship, BulkVessel.Spec(co).Commodity).Where(v => v != co && LiquidStores.IsFamily(v.strCODef)).ToArray();
            if (targets.Length > 0 || tanks.Length > 0)
                yield return new(Text.Get(LiquidStores.For(co.strCODef)!.Family.TextPrefix + ".pour_field"), Text.Get("Provider.link_none"), targets.Select(c => ("pour:" + c.strID, LinkChoices.Label(co, c, LiquidStoreService.Line(co), deposit: true))),
                    "", () => LinkChoices.Note(co, LiquidStoreService.Line(co), tanks, targets));
        }
        else if (GasStores.For(co.strCODef) is GasStore fuel && !BulkVessel.Protected(co))
        {
            yield return new(Text.Get("Provider.vent_field"), Text.Get("Provider.kg", BulkVessel.Snapshot(co).ServiceKg), StoreService.VentChoices(fuel).Select(n => ("vent:" + N(n), Text.Get("Provider.kg", n))));
            var targets = StoreService.TransferCandidates(co).ToArray();
            var others = BulkVessels.AboardAnyState(co.ship, fuel.Commodity).Where(v => v != co && GasStores.IsFamily(v.strCODef)).ToArray();
            if (targets.Length > 0 || others.Length > 0)
                yield return new(Text.Get("Provider.transfer_field"), Text.Get("Provider.link_none"), targets.Select(c => ("transfer:" + c.strID, ObjectPresentation.Name(c))),
                    "", () => LinkChoices.Note(co, GasLine.Family, others, targets));
        }
    }
    public bool IsConfiguration(string action) => new[] { "link:", "water:", "store:", "canister:", "vent:", "hydrogen:", "methane:", "feed:", "order:", "source-on:", "source-off:", "unlink:",
            "mode:", "target:", "draw:", "transfer:", "o2:", "pressure:", "oxygen:", "nitrogen:", "gas-link:", "ammonia:", "recipe:", "acid:", "pour:", "nutrients:", "ethanol:", "co2:", ChargeMachine.PreferPrefix, Phobos.Ostranauts.Framework.Inventory.StoreFeed.ActionPrefix, Phobos.Ostranauts.Framework.Inventory.StoreDelivery.ActionPrefix }.Concat(FeederRules.SettingPrefixes)
        .Any(p => action.StartsWith(p, StringComparison.Ordinal));
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort.", "PhobosState.crew-order", Phobos.Ostranauts.Framework.Inventory.StoreFeedRecord.Key, Phobos.Ostranauts.Framework.Inventory.StoreDelivery.Key,
        "PhobosState." + ProcessorRules.Record, "PhobosState." + SabatierRules.Record, "PhobosState." + CrackerRules.Record, "PhobosState." + ManifoldRules.Record,
        "PhobosState." + FillerRules.Record, "PhobosState." + RegulatorRules.Record, "PhobosState." + BottlerRules.Record, "PhobosState." + FeederRules.Record }.Concat(ChargeMachines.All.Select(m => "PhobosState." + m.Spec.Record)).Concat(GasStores.All.Select(s => "PhobosState." + s.Spec.Record)).Concat(LiquidStores.All.Select(s => "PhobosState." + s.Spec.Record)).ToArray());
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        bool saved = Command(co, binding, action, out reason);
        if (saved) Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);
        return saved;
    }
    private static bool ProductStore(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        if (ChargeMachines.For(co.strCODef) == null && !BottlerRules.IsFamily(co.strCODef)) { message = Text.Get("Content.unsupported_action"); return false; }
        message = Content.Access(co, binding) ?? "";
        return message.Length == 0 && Phobos.Ostranauts.Framework.Inventory.StoreDelivery.Command(co, action, out message);
    }
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        if (ChargeMachines.For(co.strCODef) is ChargeMachine charge)
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, charge.Spec.SnapshotKind, new EquipmentActivity(charge.State(co), charge.Describe(co)), Actions("start", "pause", "cancel"));
        if (ProcessorRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "processor", new EquipmentActivity(ProcessorService.State(co), ProcessorService.Describe(co)),
                ProcessorService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        if (BottlerRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "bottler", new EquipmentActivity(BottlerService.State(co), BottlerService.Describe(co)),
                BottlerService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        if (FeederRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "feeder", new EquipmentActivity(FeederService.State(co), FeederService.Describe(co)),
                FeederService.Protected(co) ? Actions("accept") : new[] { new EquipmentAction("jettison", Text.Get("Feeder.action_jettison")) });
        if (FillerRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "filler", new EquipmentActivity(FillerService.State(co), FillerService.Describe(co)),
                FillerService.Protected(co) ? Actions("accept") : Actions("start", "pause"));
        if (RegulatorRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "regulator", new EquipmentActivity(RegulatorService.State(co), RegulatorService.Describe(co)),
                RegulatorService.Protected(co) ? Actions("accept")
                    : new[] { new EquipmentAction("on", Text.Get("Regulator.action_on")), new EquipmentAction("off", Text.Get("Regulator.action_off")) });
        if (ManifoldRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "manifold", new EquipmentActivity(ManifoldService.State(co), ManifoldService.Describe(co)), Actions("on", "off"));
        if (SabatierRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "reactor", new EquipmentActivity(SabatierService.State(co), SabatierService.Describe(co)),
                SabatierService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        if (CrackerRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "cracker", new EquipmentActivity(CrackerService.State(co), CrackerService.Describe(co)),
                CrackerService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        if (LiquidStores.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "store", new EquipmentActivity(LiquidStoreService.State(co), LiquidStoreService.Describe(co)),
                BulkVessel.Protected(co) ? Actions("accept") : BulkVessel.Snapshot(co).CatchKg > 1e-8 ? Actions("recover") : Array.Empty<EquipmentAction>());
        var actions = new List<EquipmentAction>();
        if (BulkVessel.Protected(co)) actions.Add(new EquipmentAction("accept", Text.Get("Provider.action_accept")));
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "store", new EquipmentActivity(StoreService.State(co), StoreService.Describe(co)), actions);
    }
    private static EquipmentAction[] Actions(params string[] ids) => ids.Select(a => new EquipmentAction(a, Text.Get("Provider.action_" + a))).ToArray();
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) =>
        // The optional product store (0.49.0) is the same choice on every machine that offers it.
        action.StartsWith(Phobos.Ostranauts.Framework.Inventory.StoreDelivery.ActionPrefix, StringComparison.Ordinal) ? ProductStore(co, binding, action, out message) :
        ChargeMachines.For(co.strCODef) is ChargeMachine charge ? charge.Command(co, binding, action, out message) :
        ProcessorRules.IsFamily(co.strCODef) ? ProcessorService.Command(co, binding, action, out message) :
        SabatierRules.IsFamily(co.strCODef) ? SabatierService.Command(co, binding, action, out message) :
        CrackerRules.IsFamily(co.strCODef) ? CrackerService.Command(co, binding, action, out message) :
        ManifoldRules.IsFamily(co.strCODef) ? ManifoldService.Command(co, binding, action, out message) :
        FillerRules.IsFamily(co.strCODef) ? FillerService.Command(co, binding, action, out message) :
        BottlerRules.IsFamily(co.strCODef) ? BottlerService.Command(co, binding, action, out message) :
        FeederRules.IsFamily(co.strCODef) ? FeederService.Command(co, binding, action, out message) :
        RegulatorRules.IsFamily(co.strCODef) ? RegulatorService.Command(co, binding, action, out message) :
        LiquidStores.IsFamily(co.strCODef) ? LiquidStoreService.Command(co, binding, action, out message) : StoreService.Command(co, binding, action, out message);
}
