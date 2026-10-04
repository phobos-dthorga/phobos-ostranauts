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
    internal const string Group = "bed", MonitorGroup = "monitor";
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { MedicalRules.BedInstalled, MedicalRules.BedPrefix + "InstalledDmg", MedicalRules.MonitorInstalled, MedicalRules.MonitorPrefix + "InstalledDmg" });
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (MedicalRules.IsMonitor(co.strCODef))
        {
            if (MonitorService.Protected(co)) yield break;
            var m = MonitorService.StateOf(co);
            var beds = MonitorService.Beds(co).ToList();
            var watched = MonitorService.Bed(co);
            yield return new(Text.Get("Provider.watch_field"), watched != null ? ObjectPresentation.Name(watched) : Text.Get("Provider.watch_none"),
                beds.Select(b => ("bed:" + b.strID, ObjectPresentation.Name(b))).Concat(new[] { ("bed:auto", Text.Get("Provider.watch_auto")) }),
                m.Bed.Length == 0 ? "bed:auto" : "bed:" + m.Bed, () => beds.Count == 0 ? Text.Get("Provider.watch_note") : "");
            yield return new(Text.Get("Provider.alerts_field"), Text.Get(m.Alerts ? "Provider.on" : "Provider.off"),
                new[] { ("alerts:on", Text.Get("Provider.on")), ("alerts:off", Text.Get("Provider.off")) }, m.Alerts ? "alerts:on" : "alerts:off");
            yield break;
        }
        if (!MedicalRules.IsBed(co.strCODef) || BedService.Protected(co)) yield break;
        bool reserved = BedService.StateOf(co).Reserved;
        yield return new(Text.Get("Provider.use_field"), Text.Get(reserved ? "Provider.use_injured" : "Provider.use_anyone"),
            new[] { ("use:anyone", Text.Get("Provider.use_anyone")), ("use:injured", Text.Get("Provider.use_injured")) }, reserved ? "use:injured" : "use:anyone");
        bool send = BedService.StateOf(co).SendInjured;
        yield return new(Text.Get("Provider.send_field"), Text.Get(send ? "Provider.on" : "Provider.off"),
            new[] { ("send:on", Text.Get("Provider.on")), ("send:off", Text.Get("Provider.off")) }, send ? "send:on" : "send:off");
    }
    public bool IsConfiguration(string action) => action.StartsWith("use:", StringComparison.Ordinal) || action.StartsWith("send:", StringComparison.Ordinal) ||
        action.StartsWith("alerts:", StringComparison.Ordinal) || action.StartsWith("bed:", StringComparison.Ordinal);
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosState." + MedicalRules.Record, "PhobosState." + MedicalRules.MonitorRecord });
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        return Command(co, binding, action, out reason);
    }
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        bool monitor = MedicalRules.IsMonitor(co.strCODef);
        bool locked = monitor ? MonitorService.Protected(co) : BedService.Protected(co);
        var activity = monitor ? new EquipmentActivity(MonitorService.State(co), MonitorService.Describe(co)) : new EquipmentActivity(BedService.State(co), BedService.Describe(co));
        return new(co.strID, co.strNameFriendly, monitor ? MonitorGroup : Group, activity,
            locked ? new[] { new EquipmentAction("accept", Text.Get("Provider.action_accept")) } : Array.Empty<EquipmentAction>());
    }
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) =>
        MedicalRules.IsMonitor(co.strCODef) ? MonitorService.Command(co, binding, action, out message) : BedService.Command(co, binding, action, out message);
}
