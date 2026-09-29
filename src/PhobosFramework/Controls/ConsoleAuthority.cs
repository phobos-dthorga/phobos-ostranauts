using Phobos.Ostranauts.Framework.Crew;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>The remote-command rule every content mod applied on its own: the binding's console must be
/// the same installed, powered, undamaged, unlocked console on the same player-owned ship as the target,
/// with the operator alive and within reach of it. Wording stays content-owned; callers map the failure.</summary>
public static class ConsoleAuthority
{
    public const string IndustrialConsole = "PhobosIndustrialConsoleInstalled";
    public static ConsoleAccessFailure Check(CondOwner? target, ConsoleBinding binding, double accessTiles, string consoleDefinition = IndustrialConsole, CondOwner? actor = null)
    {
        var console = CrewWork.Resolve(binding.ConsoleId);
        actor ??= CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        bool ready = console != null && console.strCODef == consoleDefinition && console.HasCond("IsInstalled") && !console.HasCond("IsDamaged") &&
            !console.HasCond("IsLocked") && !console.HasCond("IsOverrideOff") && !console.HasCond("IsSignalOff") && console.HasCond("IsPowered") &&
            console.objCOParent == null && console.ship != null && (int)console.ship.LoadState >= 2;
        bool operatorReady = actor != null && !actor.bDestroyed && !actor.HasCond("IsDead") && !actor.HasCond("Unconscious") &&
            console != null && TileUtils.TileRange(actor.GetPos(), console.GetPos("use")) <= accessTiles;
        return binding.Check(console?.strID, console?.ship?.strRegID, actor?.strID, actor?.ship?.strRegID,
            target == null || target.bDestroyed ? null : target.ship?.strRegID,
            CrewSim.system?.GetShipOwner(binding.ShipId), CrewSim.coPlayer?.strID, ready, operatorReady);
    }
}
