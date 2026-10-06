using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Flight;

/// <summary>A ship's torch drive as the game rates it (Framework 0.123.0), shared by every mod that plans with it:
/// Auto Nav's flight and Framework's fair gig deadlines. The cycle limit is the game's own: with the console's torch
/// safety on, the slider position that keeps the drive at or under 2 g (<c>NavModTorchDrive.GetLimiterSafetyMax</c>);
/// with it off, the whole slider. Works on ships that are not loaded, from the figures the game keeps for them.</summary>
public static class TorchPerformance
{
    /// <summary>Whether the ship has a torch drive the game can rate.</summary>
    public static bool HasTorch(Ship? ship) => ship != null && !ship.bDestroyed && ship.fFusionThrustMax > 0 && ship.Mass > 0;

    /// <summary>The torch slider's top: the game's 2 g limit with safety on, else the whole slider.</summary>
    public static float CycleLimit(Ship ship, bool safetyOn) => safetyOn ? global::Ostranauts.ShipGUIs.NavStation.NavModTorchDrive.GetLimiterSafetyMax(ship) : 1f;

    /// <summary>Full torch acceleration at that limit, in AU/s², as the game's planner uses it.</summary>
    public static double Acceleration(Ship ship, bool safetyOn) => ship.GetMaxTorchThrust(CycleLimit(ship, safetyOn));
}

/// <summary>The player's own ships (Framework 0.123.0): every ship the game's registry names the player as owner of,
/// as War Has Been Declared and the Crew panel decide ownership. Loaded ships only, unless asked for all.</summary>
public static class PlayerFleet
{
    public static bool Owns(Ship? ship) => ship != null && !ship.bDestroyed && !string.IsNullOrEmpty(ship.strRegID) &&
        CrewSim.coPlayer != null && CrewSim.system?.GetShipOwner(ship.strRegID) == CrewSim.coPlayer.strID;

    /// <summary>Fills <paramref name="into"/> with the player's ships and returns it.</summary>
    public static List<Ship> Owned(List<Ship> into, bool includeUnloaded)
    {
        into.Clear();
        var ships = CrewSim.system?.dictShips;
        if (ships != null)
            foreach (var s in ships.Values)
                if (Owns(s) && (includeUnloaded || s.LoadState >= Ship.Loaded.Edit)) into.Add(s);
        return into;
    }
}
