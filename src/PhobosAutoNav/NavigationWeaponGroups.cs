using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed class WeaponGroupReading
{
    internal int Group, Installed;
}

internal sealed partial class NavigationService
{
    // Inventory is presentation data, never a replacement for native shot eligibility.
    private static List<CondOwner> InstalledWeapons(CondOwner? co)
    {
        if (!IsLocalConsole(co) || co!.ship == null || (int)co.ship.LoadState < 2) return new();
        return co.ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(w => w != null && !w.bDestroyed && w.ship == co.ship && w.objCOParent == null && w.Item != null &&
                w.HasCond("IsShipWeapon") && w.HasCond("IsInstalled"))
            .OrderBy(w => w.strID, StringComparer.Ordinal).ToList();
    }
    private static int InstalledGroup(CondOwner w) => MathUtils.RoundToInt(w.GetCondAmount("IsShipWeaponFiringGroup")) + 1;
    internal sealed class GroupSwitch
    {
        internal CondOwner Console = null!, Actor = null!, Player = null!;
        internal Ship Ship = null!;
        internal int Before, After, Volleys;
        internal bool Held, Lease;
        internal long Revision;
        internal string? Target;
        internal CondOwner[] Members = Array.Empty<CondOwner>();
    }
    internal GroupSwitch? PrepareGroupSwitch(CondOwner? co, int group)
    {
        var actor = CrewSim.GetSelectedCrew(); var player = CrewSim.coPlayer;
        if (actor == null || player == null) return null;
        if (group < 1 || group > 9 || FireHardwareProblem(co) != null || !Plugin.Enabled.Value ||
            Fire.OtherOwner(co!.strID) || !FirePreferences(co, out int before, out int volleys, out bool held)) return null;
        var members = InstalledWeapons(co).Where(w => InstalledGroup(w) == group).ToArray();
        if (members.Length == 0 || group == before) return null;
        return new GroupSwitch { Console = co!, Ship = co!.ship, Actor = actor, Player = player,
            Before = before, After = group, Volleys = volleys, Held = held, Lease = Fire.Owns(co.strID),
            Revision = Fire.OwnershipRevision, Target = fireTargetId, Members = members };
    }
    internal bool GroupSwitchCurrent(GroupSwitch ticket) => IsLocalConsole(ticket.Console) && ticket.Console.ship == ticket.Ship &&
        CrewSim.GetSelectedCrew() == ticket.Actor && CrewSim.coPlayer == ticket.Player && !Fire.OtherOwner(ticket.Console.strID) &&
        Fire.OwnershipRevision == ticket.Revision && Fire.Owns(ticket.Console.strID) == ticket.Lease && fireTargetId == ticket.Target &&
        FirePreferences(ticket.Console, out int group, out int volleys, out bool held) &&
        group == ticket.Before && volleys == ticket.Volleys && held == ticket.Held;
    internal bool ConfirmGroupSwitch(GroupSwitch ticket)
    {
        if (!Plugin.Enabled.Value || !GroupSwitchCurrent(ticket) || FireHardwareProblem(ticket.Console) != null ||
            !InstalledWeapons(ticket.Console).Where(w => InstalledGroup(w) == ticket.After).SequenceEqual(ticket.Members))
        { status = Text.Get("FCS.group_changed_retry"); return false; }
        bool hold = ticket.Held || ticket.Lease;
        // A failed write leaves ownership and active permission untouched. Everything below
        // is synchronous on the native main thread; no native dispatch runs between these steps.
        if (!SaveFirePreferences(ticket.Console, ticket.After, ticket.Volleys, hold))
        { status = Text.Get("Preferences.invalid"); return false; }
        CeaseFire();
        Fire.SetOwnership(ticket.Console.strID, ticket.Ship, ticket.After, hold);
        fireConsole = ticket.Console; aimReference = viewedWeapon = null; Fire.Invalidate();
        status = Text.Get(hold ? "FCS.group_now_held" : "FCS.group_selected", ticket.After);
        return true;
    }
    private void ReadWeaponInventory(CondOwner co, HubSnapshot view, bool fresh)
    {
        var installed = InstalledWeapons(co);
        view.Groups = installed.GroupBy(InstalledGroup).Where(g => g.Key >= 1 && g.Key <= 9)
            .Select(g => new WeaponGroupReading { Group = g.Key, Installed = g.Count() }).OrderBy(g => g.Group).ToArray();
        var members = installed.Where(w => InstalledGroup(w) == view.WeaponGroup).ToList();
        var item = members.FirstOrDefault(w => w.strID == viewedWeapon) ?? members.FirstOrDefault();
        view.WeaponLabel = Text.Get("FCS.weapon", item == null ? 0 : members.IndexOf(item) + 1, members.Count,
            aimReference == null ? "—" : (members.FindIndex(w => w.strID == aimReference) + 1).ToString());
        view.FireReason = Text.Get("FCS.inventory_ready", view.Ownership, fresh ? Fire.ReadyCount?.ToString() ?? "—" : "—", members.Count);
        if (item == null) { view.WeaponCard = Text.Get("FCS.group_empty"); return; }
        string? reason = item.HasCond("IsDamaged") ? "FCS.damaged" : item.HasCond("IsOff") ? "FCS.switched_off" :
            !item.HasCond("IsPowered") ? "FCS.unpowered" : null;
        var reading = fresh ? Fire.Weapons.FirstOrDefault(w => w.Id == item.strID) : null;
        if (reason != null || reading == null)
            view.WeaponCard = Text.Get("FCS.inventory_status", item.strNameFriendly, Text.Get(reason ?? "FCS.reading_pending"));
        else view.WeaponCard = Text.Get("FCS.weapon_card", reading.Name, Text.Get(reading.Reason),
            reading.InArc.HasValue ? Text.Get(reading.InArc.Value ? "FCS.yes" : "FCS.no") : "—", Text.Get(reading.Loaded ? "FCS.yes" : "FCS.no"),
            reading.ReloadSeconds?.ToString("0.0") ?? "—", reading.AimSeconds?.ToString("0.0") ?? "—", Text.Get(reading.Manual ? "FCS.mode_manual" : "FCS.mode_auto"));
    }
}
