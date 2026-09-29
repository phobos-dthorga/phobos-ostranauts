using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Local panel and C1 presentation of the three machines through Framework's equipment-provider
/// contract. Choices are content-owned; every application goes through the checked services.</summary>
internal sealed class Provider : IEquipmentProvider, IEquipmentPanelFields
{
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { RefineryRules.Installed, RefineryRules.Installed + "Dmg", ProcessorRules.Installed, ProcessorRules.Installed + "Dmg", HydrogenRules.Installed, HydrogenRules.Installed + "Dmg" });
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
        else if (HydrogenRules.IsFamily(co.strCODef) && !BulkVessel.Protected(co))
            yield return new(Text.Get("Provider.vent_field"), Text.Get("Provider.kg", BulkVessel.Snapshot(co).ServiceKg), HydrogenService.VentChoices.Select(n => ("vent:" + N(n), Text.Get("Provider.kg", n))));
    }
    public bool IsConfiguration(string action) => new[] { "link:", "water:", "store:", "canister:", "vent:" }.Any(p => action.StartsWith(p, StringComparison.Ordinal));
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, "PhobosMaterialPort.", "PhobosState.crew-order",
        "PhobosState." + RefineryRules.Record, "PhobosState." + ProcessorRules.Record, "PhobosState." + HydrogenRules.Record);
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
        var actions = new List<EquipmentAction>();
        if (BulkVessel.Protected(co)) actions.Add(new EquipmentAction("accept", Text.Get("Provider.action_accept")));
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "store", new EquipmentActivity(HydrogenService.State(co), HydrogenService.Describe(co)), actions);
    }
    private static EquipmentAction[] Actions(params string[] ids) => ids.Select(a => new EquipmentAction(a, Text.Get("Provider.action_" + a))).ToArray();
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) =>
        RefineryRules.IsFamily(co.strCODef) ? RefineryService.Command(co, binding, action, out message) :
        ProcessorRules.IsFamily(co.strCODef) ? ProcessorService.Command(co, binding, action, out message) : HydrogenService.Command(co, binding, action, out message);
}
