using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Ostranauts.Ships;

namespace Phobos.Ostranauts.Framework.Observations;

/// <summary>What the game itself knows about a ship being in a fight, at the moment of asking.</summary>
public readonly struct CombatFacts
{
    public CombatFacts(bool engaged, bool targeting, double? lastDamageEpoch)
    { Engaged = engaged; Targeting = targeting; LastDamageEpoch = lastDamageEpoch; }
    /// <summary>Another ship has this ship as its combat target or among its AI combatants.</summary>
    public bool Engaged { get; }
    /// <summary>This ship has a combat target of its own (weapons locked on).</summary>
    public bool Targeting { get; }
    /// <summary>Game time of the last native damage event on this ship this session, if any.</summary>
    public double? LastDamageEpoch { get; }
}

/// <summary>Read-only adapter over the game's combat bookkeeping. The game has no ship-wide combat mode;
/// the facts are <c>Ship.shipCombatTarget</c>, <c>Ship.IsInCombatWith</c> (target or AI "Combatants")
/// and <c>DamageSystem.OnShipTookDamageEvent</c>, which fires for weapon hits, collisions and explosion
/// shrapnel alike. Nothing here switches weapons, targets or AI state.</summary>
public static class NativeCombat
{
    private static readonly Dictionary<string, double> lastDamage = new(StringComparer.Ordinal);
    private static bool listening;

    public static CombatFacts Facts(Ship ship)
    {
        if (ship == null || ship.bDestroyed || string.IsNullOrEmpty(ship.strRegID)) return new CombatFacts(false, false, null);
        EnsureListening();
        bool engaged = false;
        var ships = CrewSim.system?.dictShips;
        if (ships != null)
            foreach (var other in ships.Values)
                if (other != null && other != ship && !other.bDestroyed && other.IsInCombatWith(ship.strRegID)) { engaged = true; break; }
        return new CombatFacts(engaged, ship.shipCombatTarget != null && !ship.shipCombatTarget.bDestroyed,
            lastDamage.TryGetValue(ship.strRegID, out var at) ? at : (double?)null);
    }

    /// <summary>Subscribes to the native damage event once; safe to call repeatedly.</summary>
    public static void EnsureListening()
    {
        if (listening) return;
        DamageSystem.OnShipTookDamageEvent.AddListener(Damaged);
        listening = true;
    }

    private static void Damaged(string regId)
    {
        if (!string.IsNullOrEmpty(regId)) lastDamage[regId] = StarSystem.fEpoch;
    }

    internal static void Reset() => lastDamage.Clear();
}

[HarmonyPatch]
internal static class NativeCombatReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => NativeCombat.Reset();
}
