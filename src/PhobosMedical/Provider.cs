using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>The Ward-3's panel and console presentation through Framework's equipment-provider contract. Every change
/// goes through <see cref="BedService"/>.</summary>
internal sealed class Provider : IEquipmentProvider, IEquipmentPanelFields
{
    internal const string Group = "bed";
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { MedicalRules.BedInstalled, MedicalRules.BedPrefix + "InstalledDmg" });
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (!MedicalRules.IsBed(co.strCODef) || BedService.Protected(co)) yield break;
        bool reserved = BedService.StateOf(co).Reserved;
        yield return new(Text.Get("Provider.use_field"), Text.Get(reserved ? "Provider.use_injured" : "Provider.use_anyone"),
            new[] { ("use:anyone", Text.Get("Provider.use_anyone")), ("use:injured", Text.Get("Provider.use_injured")) }, reserved ? "use:injured" : "use:anyone");
        bool send = BedService.StateOf(co).SendInjured;
        yield return new(Text.Get("Provider.send_field"), Text.Get(send ? "Provider.on" : "Provider.off"),
            new[] { ("send:on", Text.Get("Provider.on")), ("send:off", Text.Get("Provider.off")) }, send ? "send:on" : "send:off");
    }
    public bool IsConfiguration(string action) => action.StartsWith("use:", StringComparison.Ordinal) || action.StartsWith("send:", StringComparison.Ordinal);
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosState." + MedicalRules.Record });
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        return Command(co, binding, action, out reason);
    }
    public EquipmentSnapshot Snapshot(CondOwner co) => new(co.strID, co.strNameFriendly, Group, new EquipmentActivity(BedService.State(co), BedService.Describe(co)),
        BedService.Protected(co) ? new[] { new EquipmentAction("accept", Text.Get("Provider.action_accept")) } : Array.Empty<EquipmentAction>());
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) => BedService.Command(co, binding, action, out message);
}
