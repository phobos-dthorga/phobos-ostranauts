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
            // The radiator link (0.61.0): a touching cooling assembly, or the room behind the mount.
            string cooling = LaserService.CoolingPeer(co);
            yield return new(Text.Get("Laser.cooling_field"), cooling.Length == 0 ? Text.Get("Laser.cooling_room") : ObjectPresentation.Name(cooling),
                LaserService.RadiatorCandidates(co).Select(r => ("cooling:" + r.strID, r.strNameFriendly + " [" + Phobos.Ostranauts.Framework.Inventory.PortPairing.ShortId(r.strID) + "]"))
                    .Concat(new[] { ("cooling:none", Text.Get("Laser.cooling_room")) }),
                "cooling:" + (cooling.Length == 0 ? "none" : cooling), () => LaserService.CoolingNote(co));
            bool high = LaserService.HighPower(co);
            yield return new(Text.Get("Laser.power_field"), LaserService.PowerLabel(high),
                new[] { false, true }.Select(h => ("power:" + (h ? LaserRules.PowerHigh : LaserRules.PowerStandard), LaserService.PowerLabel(h))),
                "power:" + (high ? LaserRules.PowerHigh : LaserRules.PowerStandard));
            foreach (var (action, label, on) in new[] { (LaserRules.HaulJobsKey + ":", "Laser.haul_field", LaserService.HaulJobs(co)), (LaserRules.DepositJobsKey + ":", "Laser.deposit_field", LaserService.DepositJobs(co)) })
                yield return new(Text.Get(label), LaserService.SwitchLabel(on),
                    new[] { false, true }.Select(v => (action + (v ? LaserRules.SwitchOn : LaserRules.SwitchOff), LaserService.SwitchLabel(v))),
                    action + (on ? LaserRules.SwitchOn : LaserRules.SwitchOff));
            yield break;
        }
        if (!ThawRules.IsFamily(co.strCODef)) yield break;
        string water = ThawService.Peer(co), methane = ThawService.MethanePeer(co);
        // Each sheet says why a vessel aboard is not offered (loose, damaged, no line touching it): Framework 0.69.0.
        yield return new(Text.Get("Thaw.vessel_field"), ObjectPresentation.Name(water),
            ThawService.Candidates(co).Select(v => ("link:" + v.strID, LinkChoices.Label(co, v, ThawService.WaterLink, true))).Concat(new[] { ("link:none", Text.Get("Thaw.link_none")) }),
            "link:" + (water.Length == 0 ? "none" : water), () => LinkChoices.Note(co, ThawService.WaterLink, ThawService.Candidates(co)));
        // Methane ice needs a methane store (Phobos Manufacturing); the field appears once one is aboard or linked,
        // in reach or not, so the sheet can say what keeps it from linking.
        var stores = ThawService.MethaneCandidates(co).ToArray();
        if (stores.Length > 0 || methane.Length > 0 || Phobos.Ostranauts.Framework.Liquids.BulkVessels.AboardAnyState(co.ship, ThawService.MethaneLink.Commodity).Any())
            yield return new(Text.Get("Thaw.methane_field"), ObjectPresentation.Name(methane),
                stores.Select(v => ("methane-link:" + v.strID, LinkChoices.Label(co, v, ThawService.MethaneLink, true))).Concat(new[] { ("methane-link:none", Text.Get("Thaw.methane_link_none")) }),
                "methane-link:" + (methane.Length == 0 ? "none" : methane), () => LinkChoices.Note(co, ThawService.MethaneLink, ThawService.MethaneCandidates(co)));
        // Optional (Shipbreaker 0.73.0): a material bin or other store, touching or on a belt, that keeps the feed loaded.
        yield return Phobos.Ostranauts.Framework.Inventory.StoreFeed.Field(co);
    }
    // Every prefix a field above offers must be listed here: a choice that is not is refused on Apply as stale, and
    // the panel then asks to apply or discard it for ever (the ML-2's two job switches, until Shipbreaker 0.72.0).
    internal static readonly string[] ConfigurationPrefixes = new[] { "link:", "methane-link:", Phobos.Ostranauts.Framework.Inventory.StoreFeed.ActionPrefix }.Concat(LaserRules.SettingPrefixes).ToArray();
    public bool IsConfiguration(string action) => ConfigurationPrefixes.Any(p => action.StartsWith(p, StringComparison.Ordinal));
    // The laser's stamp covers only its saved choices and its cooling link, not the sweep record that changes with
    // every powered second.
    public string ConfigurationStamp(CondOwner co) => LaserRules.IsFamily(co.strCODef)
        ? Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosState." + LaserService.FilterStoreName, "PhobosState." + LaserService.PowerStoreName, "PhobosMaterialPort." })
        : Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort.", "PhobosState.crew-order", Phobos.Ostranauts.Framework.Inventory.StoreFeedRecord.Key });
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        string plain = action; Phobos.Ostranauts.Framework.Controls.Confirmations.Split(ref plain);
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(plain)) return false;
        bool saved = Command(co, binding, action, out reason);
        if (saved) Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);
        return saved;
    }
    public EquipmentSnapshot Snapshot(CondOwner co) => LaserRules.IsFamily(co.strCODef)
        ? new(co.strID, co.strNameFriendly, "laser", new EquipmentActivity(LaserService.State(co), LaserService.Describe(co)),
            new[] { new EquipmentAction("start", Text.Get("Laser.action_start")), new EquipmentAction("pause", Text.Get("Laser.action_pause")), new EquipmentAction("stop", Text.Get("Laser.action_stop")) })
        : new(co.strID, co.strNameFriendly, "thaw", new EquipmentActivity(ThawService.State(co), ThawService.Describe(co)),
            new[] { new EquipmentAction("start", Text.Get("Thaw.action_start")), new EquipmentAction("pause", Text.Get("Thaw.action_pause")), new EquipmentAction("cancel", Text.Get("Thaw.action_cancel")) });
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        bool confirmed = Phobos.Ostranauts.Framework.Controls.Confirmations.Split(ref action) || Overrides.Confirmed;
        string result = "";
        bool done = Overrides.With(confirmed, () => LaserRules.IsFamily(co.strCODef)
            ? LaserService.Command(co, binding, action, out result) : ThawService.Command(co, binding, action, out result));
        message = result; return done;
    }
}
