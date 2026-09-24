using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class ControlAuthority
{
    internal static string? Check(CondOwner target, ConsoleBinding binding)
    {
        var console = CollectorService.Resolve(binding.ConsoleId);
        var actor = CrewSim.GetSelectedCrew();
        bool ready = console != null && !console.bDestroyed && console.strCODef == IndustrialRules.Prefix + "Installed" &&
            console.HasCond("IsInstalled") && !console.HasCond("IsDamaged") && !console.HasCond("IsLocked") &&
            !console.HasCond("IsOverrideOff") && !console.HasCond("IsSignalOff") && console.HasCond("IsPowered") &&
            console.objCOParent == null && console.ship != null && (int)console.ship.LoadState >= 2;
        bool operatorReady = actor != null && !actor.bDestroyed && !actor.HasCond("IsDead") && !actor.HasCond("Unconscious") &&
            console != null && TileUtils.TileRange(actor.GetPos(), console.GetPos("use")) <= IndustrialRules.AccessTiles;
        var failure = binding.Check(console?.strID, console?.ship?.strRegID, actor?.strID, actor?.ship?.strRegID,
            target == null || target.bDestroyed ? null : target.ship?.strRegID,
            CrewSim.system?.GetShipOwner(binding.ShipId), CrewSim.coPlayer?.strID, ready, operatorReady);
        return failure == ConsoleAccessFailure.None ? null : Text.Get("Industry.access_" + failure);
    }

    internal static bool TryBind(CondOwner console, out ConsoleBinding? binding, out string message)
    {
        binding = null; message = Text.Get("Industry.access_ConsoleUnavailable");
        var actor = CrewSim.GetSelectedCrew();
        if (console == null || actor == null || console.ship == null) return false;
        var candidate = new ConsoleBinding(console.strID, console.ship.strRegID, actor.strID);
        message = Check(console, candidate) ?? "";
        if (message.Length != 0) return false;
        binding = candidate; return true;
    }
}
