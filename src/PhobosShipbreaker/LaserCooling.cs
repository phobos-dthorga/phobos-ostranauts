using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The ML-2's radiator link and power setting (Shipbreaker 0.61.0). A head may be paired with one of the
/// F6's cooling assemblies that touches it; while that assembly is ready and has room, the head's heat goes there in
/// place of the room behind the mount, and the high setting applies to the jobs it starts. The pairing is the same
/// saved one-to-one port link the furnace uses, on the assembly's own single port, so an assembly never serves two
/// machines. Without a ready assembly everything falls back to the room, exactly as before.</summary>
internal static partial class LaserService
{
    internal const string PowerStoreName = "Shipbreaker.LaserPower";
    private static ObjectStateStore PowerStore(CondOwner co) => new(co.mapGUIPropMaps, PowerStoreName, co.strID, 1);
    private static MaterialPort CoolingPort(CondOwner laser) => new(laser.strID, LaserRules.CoolingPort, laser.mapGUIPropMaps);

    /// <summary>Whether the player has chosen the high setting. It only takes effect with a ready assembly.</summary>
    internal static bool HighPower(CondOwner co) =>
        PowerStore(co).Read(out var fields) == SavedStateStatus.Ready && fields.TryGetValue("mode", out var mode) && LaserRules.ParsePower(mode, out bool high) && high;

    /// <summary>The saved link's peer id, or empty when the head is not paired.</summary>
    internal static string CoolingPeer(CondOwner laser)
    {
        var link = PortPairing.Read(CoolingPort(laser));
        return link.State == PortLinkState.Linked ? link.PeerObjectId : "";
    }

    internal static bool Touching(CondOwner laser, CondOwner assembly)
    {
        if (laser.ship == null || laser.ship != assembly.ship || assembly.Item == null) return false;
        var a = laser.GetPos(); var b = assembly.GetPos();
        // A quarter turn swaps the assembly's width and depth on the deck.
        bool turned = Math.Abs(Math.IEEERemainder(assembly.tf.eulerAngles.z, 180)) > 45;
        double width = turned ? assembly.Item.nHeightInTiles : assembly.Item.nWidthInTiles, depth = turned ? assembly.Item.nWidthInTiles : assembly.Item.nHeightInTiles;
        return LaserRules.Touching(a.x, a.y, LaserRules.Footprint, LaserRules.Footprint, b.x, b.y, width, depth);
    }

    /// <summary>The paired cooling assembly when it can take the head's heat now; otherwise null and why.</summary>
    internal static CondOwner? Radiator(CondOwner laser, out string problem)
    {
        problem = Text.Get("Laser.radiator_none");
        var link = PortPairing.Read(CoolingPort(laser));
        if (link.State == PortLinkState.Unlinked) return null;
        problem = Text.Get("Laser.radiator_missing");
        var assembly = link.State == PortLinkState.Linked ? CollectorService.Resolve(link.PeerObjectId) : null;
        if (assembly == null || !FurnaceService.IsSink(assembly) || !PortPairing.Matches(CoolingPort(laser), FurnaceService.SinkPort(assembly))) return null;
        if (!Touching(laser, assembly)) { problem = Text.Get("Laser.radiator_apart"); return null; }
        string? fault = FurnaceService.SinkProblem(assembly);
        if (fault != null) { problem = fault; return null; }
        problem = "";
        return assembly;
    }

    /// <summary>Installed cooling assemblies touching the head that are free, or already this head's.</summary>
    internal static IEnumerable<CondOwner> RadiatorCandidates(CondOwner laser)
    {
        string mine = CoolingPeer(laser);
        return ShipEquipment.Read(laser.ship, c => FurnaceService.IsSink(c) && c.HasCond("IsInstalled") && Touching(laser, c) &&
            (c.strID == mine || PortPairing.Read(FurnaceService.SinkPort(c)).State == PortLinkState.Unlinked))
            .OrderBy(c => c.strID, StringComparer.Ordinal);
    }

    /// <summary>Why each cooling assembly aboard is not offered to this head (Shipbreaker 0.80.0): loose, not touching,
    /// or paired with something else.</summary>
    internal static string CoolingNote(CondOwner laser)
    {
        string mine = CoolingPeer(laser);
        return LinkNotes.For(ShipEquipment.Read(laser.ship, c => FurnaceService.IsSink(c)), c =>
            !c.HasCond("IsInstalled") ? Text.Get("Furnace.install") : !Touching(laser, c) ? Text.Get("Links.not_touching") :
            c.strID != mine && PortPairing.Read(FurnaceService.SinkPort(c)).State != PortLinkState.Unlinked ? Text.Get("Links.paired_elsewhere") : null);
    }
    /// <summary>Pairs the head with a cooling assembly, or clears the pairing. Both need a paused laser and an
    /// assembly at 50 C or below, the furnace's own rule for changing what an assembly serves.</summary>
    internal static bool SetCooling(CondOwner co, ConsoleBinding? binding, string id, out string message)
    {
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length != 0) return false;
        if (co.ship == null || CrewSim.coPlayer == null || CrewSim.system?.GetShipOwner(co.ship.strRegID) != CrewSim.coPlayer.strID)
        { message = Text.Get("Furnace.owned_ship"); return false; }
        var port = CoolingPort(co); var link = PortPairing.Read(port);
        if (link.State == PortLinkState.Linked && link.PeerObjectId == id && Radiator(co, out _) != null)
        { message = Text.Get("Laser.cooling_linked", ObjectPresentation.Name(id)); return true; }
        // Everything a press cannot fix is checked before anything changes (Shipbreaker 0.85.0): a hot assembly, old or new.
        var current = link.State == PortLinkState.Linked ? CollectorService.Resolve(link.PeerObjectId) : null;
        bool ours = current != null && FurnaceService.IsSink(current) && PortPairing.Matches(port, FurnaceService.SinkPort(current));
        if (link.State != PortLinkState.Unlinked && ours && !FurnaceService.SinkCool(current!)) { message = Text.Get("Laser.radiator_hot"); return false; }
        CondOwner? assembly = null;
        if (id != "none")
        {
            assembly = RadiatorCandidates(co).FirstOrDefault(c => c.strID == id);
            if (assembly == null) { message = Text.Get("Laser.radiator_choose"); return false; }
            string? fault = FurnaceService.SinkProblem(assembly);
            if (fault != null) { message = fault; return false; }
            if (!FurnaceService.SinkCool(assembly)) { message = Text.Get("Laser.radiator_hot"); return false; }
        }
        // Cutting pauses for the change and carries on (0.85.0); until then it had to be paused first.
        bool cutting = sessions.TryGetValue(co.strID, out var s) && s.Authorized;
        return Overrides.HoldAround(co, cutting, () => Pause(co, false), () => Start(co, out var why) ? null : why, () =>
        {
            if (link.State != PortLinkState.Unlinked)
            {
                PortPairing.Unlink(port, ours ? FurnaceService.SinkPort(current!) : null);
                if (sessions.TryGetValue(co.strID, out var held)) held.Radiator = null;
            }
            if (assembly == null) return null;
            return PortPairing.TryLink(port, FurnaceService.SinkPort(assembly), out var problem) ? null : problem;
        }, () => assembly == null ? Text.Get("Laser.cooling_unlinked") : Text.Get("Laser.cooling_linked", assembly.strNameFriendly), out message);
    }

    /// <summary>The power setting for the jobs the head starts from now on. High needs a ready assembly.</summary>
    internal static bool SetPower(CondOwner co, ConsoleBinding? binding, string id, out string message)
    {
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length != 0) return false;
        if (!LaserRules.ParsePower(id, out bool high)) { message = Text.Get("Industry.unsupported_action"); return false; }
        if (high && Radiator(co, out string why) == null) { message = Text.Get("Laser.power_needs_radiator", why); return false; }
        if (!PowerStore(co).TryWrite(new Dictionary<string, string> { ["mode"] = id })) { message = Text.Get("Laser.save"); return false; }
        message = Text.Get("Laser.power_set", PowerLabel(high));
        return true;
    }
    internal static string PowerLabel(bool high) => Text.Get(high ? "Laser.power_high" : "Laser.power_standard", high ? LaserRules.HighKW : LaserRules.WorkingKW);

    /// <summary>Where the heat goes now, for the panel.</summary>
    private static string CoolingStatus(CondOwner co)
    {
        var assembly = Radiator(co, out string problem);
        if (assembly != null)
            return Text.Get("Laser.cooling_status_radiator", assembly.strNameFriendly, FurnaceService.SinkKelvin(assembly) - Phobos.Ostranauts.Framework.Units.CelsiusToKelvin,
                FurnaceService.SinkHeadroomKJ(assembly) / 1000);
        return CoolingPeer(co).Length == 0 && PortPairing.Read(CoolingPort(co)).State == PortLinkState.Unlinked
            ? Text.Get("Laser.cooling_status_room") : Text.Get("Laser.cooling_status_fallback", problem);
    }
}
