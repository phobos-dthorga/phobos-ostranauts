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
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { RefineryRules.Installed, ProcessorRules.Installed, SabatierRules.Installed, ManifoldRules.Installed, FillerRules.Installed }
        .Concat(GasStores.All.Select(s => s.Installed)).SelectMany(id => new[] { id, id + "Dmg" }).ToArray());
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (RefineryRules.IsFamily(co.strCODef))
            yield return new(Text.Get("Provider.vessel_field"), ObjectPresentation.Name(RefineryService.Peer(co)),
                RefineryService.Candidates(co).Select(v => ("link:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("link:none", Text.Get("Provider.link_none")) }));
        else if (ProcessorRules.IsFamily(co.strCODef))
        {
            yield return new(Text.Get("Provider.water_field"), ObjectPresentation.Name(ProcessorService.WaterPeer(co)),
                ProcessorService.WaterCandidates(co).Select(v => ("water:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("water:none", Text.Get("Provider.link_none")) }));
            yield return new(Text.Get("Provider.store_field"), ObjectPresentation.Name(ProcessorService.StorePeer(co)),
                ProcessorService.StoreCandidates(co).Select(v => ("store:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("store:none", Text.Get("Provider.link_none")) }));
            yield return new(Text.Get("Provider.canister_field"), ProcessorService.CanisterName(co),
                ProcessorService.CanisterCandidates(co).Select(c => ("canister:" + c.strID, ObjectPresentation.Name(c))).Concat(new[] { ("canister:none", Text.Get("Processor.cabin")) }));
        }
        else if (SabatierRules.IsFamily(co.strCODef))
        {
            yield return new(Text.Get("Provider.hydrogen_field"), ObjectPresentation.Name(SabatierService.HydrogenPeer(co)),
                SabatierService.HydrogenCandidates(co).Select(v => ("hydrogen:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("hydrogen:none", Text.Get("Provider.link_none")) }));
            yield return new(Text.Get("Provider.co2_field"), SabatierService.CanisterName(co),
                SabatierService.CanisterCandidates(co).Select(c => ("canister:" + c.strID, ObjectPresentation.Name(c))).Concat(new[] { ("canister:none", Text.Get("Provider.link_none")) }));
            yield return new(Text.Get("Provider.water_out_field"), ObjectPresentation.Name(SabatierService.WaterPeer(co)),
                SabatierService.WaterCandidates(co).Select(v => ("water:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("water:none", Text.Get("Provider.link_none")) }));
            yield return new(Text.Get("Provider.methane_field"), ObjectPresentation.Name(SabatierService.MethanePeer(co)),
                SabatierService.MethaneCandidates(co).Select(v => ("methane:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("methane:none", Text.Get("Provider.link_none")) }));
        }
        else if (ManifoldRules.IsFamily(co.strCODef))
        {
            yield return new(Text.Get("Provider.feed_field"), Text.Get(ManifoldService.On(co) ? "Provider.on" : "Provider.off"),
                new[] { ("feed:on", Text.Get("Provider.on")), ("feed:off", Text.Get("Provider.off")) });
            yield return new(Text.Get("Provider.order_field"), Text.Get(ManifoldService.First(co) ? "Provider.order_first" : "Provider.order_last"),
                new[] { ("order:first", Text.Get("Provider.order_first")), ("order:last", Text.Get("Provider.order_last")) });
            foreach (var source in ManifoldService.Sources(co))
                yield return new(ObjectPresentation.Name(source.Id), Text.Get(source.Enabled ? "Provider.on" : "Provider.off"),
                    new[] { ("source-on:" + source.Id, Text.Get("Provider.on")), ("source-off:" + source.Id, Text.Get("Provider.off")), ("unlink:" + source.Id, Text.Get("Provider.unlink")) });
            if (ManifoldService.Sources(co).Count < ManifoldRules.MaxSources)
                yield return new(Text.Get("Provider.add_source_field"), Text.Get("Provider.link_none"),
                    ManifoldService.Candidates(co).Where(c => ManifoldService.Sources(co).All(x => x.Id != c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))));
        }
        else if (FillerRules.IsFamily(co.strCODef))
        {
            var state = FillerService.StateOf(co);
            yield return new(Text.Get("Provider.mode_field"), Text.Get(state.Mode == FillerMode.Decant ? "Provider.mode_decant" : "Provider.mode_fill"),
                new[] { ("mode:fill", Text.Get("Provider.mode_fill")), ("mode:decant", Text.Get("Provider.mode_decant")) });
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
                    FillerService.StoreCandidates(co).Where(c => !linked.Contains(c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))));
            if (state.Links.Count(l => l.Kind != FillerLinkKind.Store) < FillerRules.MaxCanisters)
                yield return new(Text.Get("Provider.add_canister_field"), Text.Get("Provider.link_none"),
                    FillerService.CanisterCandidates(co).Where(c => !linked.Contains(c.strID)).Select(c => ("link:" + c.strID, ObjectPresentation.Name(c))));
        }
        else if (GasStores.For(co.strCODef) is GasStore fuel && !BulkVessel.Protected(co))
        {
            yield return new(Text.Get("Provider.vent_field"), Text.Get("Provider.kg", BulkVessel.Snapshot(co).ServiceKg), StoreService.VentChoices(fuel).Select(n => ("vent:" + N(n), Text.Get("Provider.kg", n))));
            var targets = StoreService.TransferCandidates(co).ToArray();
            if (targets.Length > 0)
                yield return new(Text.Get("Provider.transfer_field"), Text.Get("Provider.link_none"), targets.Select(c => ("transfer:" + c.strID, ObjectPresentation.Name(c))));
        }
    }
    public bool IsConfiguration(string action) => new[] { "link:", "water:", "store:", "canister:", "vent:", "hydrogen:", "methane:", "feed:", "order:", "source-on:", "source-off:", "unlink:",
            "mode:", "target:", "draw:", "transfer:" }
        .Any(p => action.StartsWith(p, StringComparison.Ordinal));
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort.", "PhobosState.crew-order",
        "PhobosState." + RefineryRules.Record, "PhobosState." + ProcessorRules.Record, "PhobosState." + SabatierRules.Record, "PhobosState." + ManifoldRules.Record,
        "PhobosState." + FillerRules.Record }.Concat(GasStores.All.Select(s => "PhobosState." + s.Spec.Record)).ToArray());
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        bool saved = Command(co, binding, action, out reason);
        if (saved) Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);
        return saved;
    }
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        if (RefineryRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "refinery", new EquipmentActivity(RefineryService.State(co), RefineryService.Describe(co)), Actions("start", "pause", "cancel"));
        if (ProcessorRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "processor", new EquipmentActivity(ProcessorService.State(co), ProcessorService.Describe(co)),
                ProcessorService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        if (FillerRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "filler", new EquipmentActivity(FillerService.State(co), FillerService.Describe(co)),
                FillerService.Protected(co) ? Actions("accept") : Actions("start", "pause"));
        if (ManifoldRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "manifold", new EquipmentActivity(ManifoldService.State(co), ManifoldService.Describe(co)), Actions("on", "off"));
        if (SabatierRules.IsFamily(co.strCODef))
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "reactor", new EquipmentActivity(SabatierService.State(co), SabatierService.Describe(co)),
                SabatierService.Protected(co) ? Actions("accept", "pause") : Actions("start", "pause", "cancel"));
        var actions = new List<EquipmentAction>();
        if (BulkVessel.Protected(co)) actions.Add(new EquipmentAction("accept", Text.Get("Provider.action_accept")));
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "store", new EquipmentActivity(StoreService.State(co), StoreService.Describe(co)), actions);
    }
    private static EquipmentAction[] Actions(params string[] ids) => ids.Select(a => new EquipmentAction(a, Text.Get("Provider.action_" + a))).ToArray();
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) =>
        RefineryRules.IsFamily(co.strCODef) ? RefineryService.Command(co, binding, action, out message) :
        ProcessorRules.IsFamily(co.strCODef) ? ProcessorService.Command(co, binding, action, out message) :
        SabatierRules.IsFamily(co.strCODef) ? SabatierService.Command(co, binding, action, out message) :
        ManifoldRules.IsFamily(co.strCODef) ? ManifoldService.Command(co, binding, action, out message) :
        FillerRules.IsFamily(co.strCODef) ? FillerService.Command(co, binding, action, out message) : StoreService.Command(co, binding, action, out message);
}
