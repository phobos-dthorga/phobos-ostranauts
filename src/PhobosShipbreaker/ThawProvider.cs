using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Local panel and C1 presentation of the T2 through Framework's equipment-provider contract. Choices are
/// content-owned; every application goes through <see cref="ThawService"/>. The silos it fills are Framework's water
/// tanks since Shipbreaker 0.54.0, with their own provider and panel. The ML-2 mining laser (0.59.0) is presented by the
/// same provider, because Framework admits one provider per mod; its choices go through <see cref="LaserService"/>.</summary>
internal sealed class VesselProvider : IEquipmentProvider, IEquipmentPanelFields
{
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { ThawRules.Installed, ThawRules.Installed + "Dmg", LaserRules.Installed, LaserRules.Installed + "Dmg" });
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (LaserRules.IsFamily(co.strCODef))
        {
            var filter = LaserService.Filter(co);
            yield return new(Text.Get("Laser.filter_field"), Text.Get("Laser.filter_" + LaserRules.FilterId(filter)),
                new[] { LaserFilter.Rock, LaserFilter.Walls, LaserFilter.Both }.Select(f => ("filter:" + LaserRules.FilterId(f), Text.Get("Laser.filter_" + LaserRules.FilterId(f)))),
                "filter:" + LaserRules.FilterId(filter));
            yield break;
        }
        if (!ThawRules.IsFamily(co.strCODef)) yield break;
        string water = ThawService.Peer(co), methane = ThawService.MethanePeer(co);
        yield return new(Text.Get("Thaw.vessel_field"), ObjectPresentation.Name(water),
            ThawService.Candidates(co).Select(v => ("link:" + v.strID, LinkChoices.Label(co, v, ThawService.WaterLink, true))).Concat(new[] { ("link:none", Text.Get("Thaw.link_none")) }),
            "link:" + (water.Length == 0 ? "none" : water));
        // Methane ice needs a methane store (Phobos Manufacturing); the field appears once one is in reach or linked.
        var stores = ThawService.MethaneCandidates(co).ToArray();
        if (stores.Length > 0 || methane.Length > 0)
            yield return new(Text.Get("Thaw.methane_field"), ObjectPresentation.Name(methane),
                stores.Select(v => ("methane-link:" + v.strID, LinkChoices.Label(co, v, ThawService.MethaneLink, true))).Concat(new[] { ("methane-link:none", Text.Get("Thaw.methane_link_none")) }),
                "methane-link:" + (methane.Length == 0 ? "none" : methane));
    }
    public bool IsConfiguration(string action) => action.StartsWith("link:", StringComparison.Ordinal) || action.StartsWith("methane-link:", StringComparison.Ordinal) ||
        action.StartsWith("filter:", StringComparison.Ordinal);
    // The laser's stamp covers only its saved choice, not the sweep record that changes with every powered second.
    public string ConfigurationStamp(CondOwner co) => LaserRules.IsFamily(co.strCODef)
        ? Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosState." + LaserService.FilterStoreName })
        : Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort.", "PhobosState.crew-order" });
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        bool saved = Command(co, binding, action, out reason);
        if (saved) Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);
        return saved;
    }
    public EquipmentSnapshot Snapshot(CondOwner co) => LaserRules.IsFamily(co.strCODef)
        ? new(co.strID, co.strNameFriendly, "laser", new EquipmentActivity(LaserService.State(co), LaserService.Describe(co)),
            new[] { new EquipmentAction("start", Text.Get("Laser.action_start")), new EquipmentAction("pause", Text.Get("Laser.action_pause")), new EquipmentAction("stop", Text.Get("Laser.action_stop")) })
        : new(co.strID, co.strNameFriendly, "thaw", new EquipmentActivity(ThawService.State(co), ThawService.Describe(co)),
            new[] { new EquipmentAction("start", Text.Get("Thaw.action_start")), new EquipmentAction("pause", Text.Get("Thaw.action_pause")), new EquipmentAction("cancel", Text.Get("Thaw.action_cancel")) });
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) => LaserRules.IsFamily(co.strCODef)
        ? LaserService.Command(co, binding, action, out message) : ThawService.Command(co, binding, action, out message);
}
