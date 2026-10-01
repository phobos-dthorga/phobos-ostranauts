using System.Collections.Generic;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Observations;

/// <summary>Read-only facts about the one ship the game has moored to another (a tethered asteroid, a captured hull):
/// which ship it is, what stands on a deck position of either, and who is aboard both. A moored pair shares one deck
/// space, so positions are plain world coordinates on both. Nothing here moors, releases or changes a ship.</summary>
public static class AttachedTiles
{
    /// <summary>The marker the game puts in the port key of a mooring, as its own <c>Ship.IsMoored</c> reads it.</summary>
    public const string MooringKey = "MP|";

    /// <summary>The single ship attached to <paramref name="own"/>, when that attachment is a mooring. Several
    /// attachments, or a docking at a station, are not one target and give false.</summary>
    public static bool Moored(Ship? own, out Ship? target, out string portId)
    {
        target = null; portId = "";
        if (own == null || own.bDestroyed) return false;
        var attached = own.GetDockedShipsAndPortIDs();
        if (attached.Count != 1) return false;
        foreach (var pair in attached)
        {
            if (pair.Key == null || !pair.Key.Contains(MooringKey) || pair.Value == null || pair.Value.bDestroyed) return false;
            target = pair.Value; portId = pair.Key;
        }
        return target != null;
    }

    /// <summary>A deck tile something stands on that a beam at deck height cannot pass: a wall or any obstruction.</summary>
    public static bool Solid(Tile? tile) => tile != null && tile.coProps != null && tile.IsShipTileOrSub && (tile.IsWall || !tile.bPassable);

    /// <summary>What stands at a world position: our own hull or fitted equipment first, then the other ship's solid
    /// tiles. Our own fixtures count as in the way even where a person could walk past them.</summary>
    public static BeamHit Probe(Ship own, Ship other, float x, float y)
    {
        var mine = own.GetTileAtWorldCoords1(x, y, false, false);
        if (mine != null && mine.coProps != null && mine.IsShipTileOrSub &&
            (mine.IsWall || !mine.bPassable || mine.IsFixture || mine.coProps.HasCond("IsFixtureExt"))) return BeamHit.Own;
        return Solid(other.GetTileAtWorldCoords1(x, y, false, false)) ? BeamHit.Other : BeamHit.None;
    }

    /// <summary>The first solid thing on the straight line between two deck positions, sampled every
    /// <paramref name="step"/> tiles, and where it was met.</summary>
    public static BeamHit FirstSolid(Ship own, Ship other, Vector2 from, Vector2 to, double step, out Vector2 at)
    {
        var hit = BeamGeometry.Walk(from.x, from.y, to.x, to.y, step, (x, y) => Probe(own, other, (float)x, (float)y), out double hitX, out double hitY);
        at = new Vector2((float)hitX, (float)hitY);
        return hit;
    }

    /// <summary>Everyone aboard the ship and the ships attached to it.</summary>
    public static List<CondOwner> People(Ship? own)
    {
        var people = new List<CondOwner>();
        if (own == null || own.bDestroyed) return people;
        foreach (var person in own.GetPeople(true)) if (person != null && !person.bDestroyed) people.Add(person);
        return people;
    }
}
