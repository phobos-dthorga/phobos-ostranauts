using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Hazards;

/// <summary>Applies damage to an object through the game's own destructible chain, exactly as one hit of the game's
/// <c>DamageSystem.DamageRay</c> does (the path its tools, weapons and mining charges take): add to
/// <c>StatDamage</c> no more than the object has left, run its <c>Destructable</c> check and end its turn. Whatever
/// follows (a damaged form, destruction, the object's own loot roll) is the game's and is not reproduced here. Content
/// decides who may be damaged, how much work that costs and when; nothing here picks a target.</summary>
public static class NativeDamage
{
    public const string Stat = "StatDamage";

    /// <summary>Damage points the object can still take before its next native threshold, or zero when it has no
    /// destructible damage check.</summary>
    public static double Left(CondOwner? target)
    {
        if (target == null || target.bDestroyed) return 0;
        var destructable = target.GetComponent<Destructable>();
        return destructable == null ? 0 : Math.Max(0, destructable.DmgLeft(Stat));
    }

    /// <summary>Adds up to <paramref name="points"/> of damage and runs the native check once. Returns the points
    /// actually applied: zero for an object that is gone, off any ship or not destructible.</summary>
    public static double Apply(CondOwner target, double points)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (double.IsNaN(points) || double.IsInfinity(points) || points <= 0 || target.bDestroyed || target.ship == null) return 0;
        var destructable = target.GetComponent<Destructable>();
        if (destructable == null) return 0;
        double applied = Math.Min(points, destructable.DmgLeft(Stat));
        if (applied <= 0) return 0;
        target.AddCondAmount(Stat, applied);
        destructable.DamageCheck();
        target.EndTurn();
        return applied;
    }

    /// <summary>The game's own tracer streak between two world points (the trail its damage rays draw). Presentation
    /// only; false when the game's trail is unavailable.</summary>
    public static bool Trail(Vector3 from, Vector3 to)
    {
        try
        {
            if (CrewSim.BulletTrail == null || CrewSim.objInstance == null) return false;
            var trail = UnityEngine.Object.Instantiate(CrewSim.BulletTrail, from, Quaternion.identity);
            CrewSim.objInstance.StartCoroutine(CrewSim.objInstance.SpawnTrail(trail, to));
            return true;
        }
        catch { return false; }
    }
}
