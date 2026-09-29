using System.Collections.Generic;
using Ostranauts.Ships;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal readonly struct SensedObject
{
    internal readonly string Id;
    internal readonly ShipSitu Situ;
    internal readonly ContactReading Reading;
    internal SensedObject(string id, ShipSitu situ, ContactReading reading) { Id = id; Situ = situ; Reading = reading; }
}

/// <summary>Asteroid-field rocks near a planned leg. Native collision checks individual field
/// asteroids, so avoidance must see the ones native sensing registers. Geometry is used only to
/// skip distant rocks before sensing; every returned rock carries its own contact reading.</summary>
internal static class NativeHazards
{
    // Candidates are gathered before any is sensed (a stellar read refreshes the rock's cached position);
    // the buffer is reused between calls, which run on the main thread only.
    private static readonly List<IStellarObject> candidates = new();

    /// <param name="startM">Leg start relative to the own ship, metres.</param>
    /// <param name="goalM">Leg end relative to the own ship, metres.</param>
    /// <param name="reachM">Extra lateral distance to consider beyond collision contact.</param>
    /// <param name="into">Receives the sensed rocks; cleared first.</param>
    internal static void Asteroids(Ship own, NavVector startM, NavVector goalM, double reachM, string? skipId, List<SensedObject> into)
    {
        into.Clear();
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Hazards);
        var system = CrewSim.system;
        if (system?.aBOs == null || own?.objSS == null || !ArrivalBrake.Finite(reachM) || reachM < 0) return;
        double ownX = own.objSS.vPosx, ownY = own.objSS.vPosy, ownRadius = own.objSS.GetRadiusAU();
        candidates.Clear();
        foreach (var body in system.aBOs.Values)
        {
            if (body is not AsteroidField field || field.Asteroids == null) continue;
            foreach (var rock in field.Asteroids)
            {
                if (rock?.objSS == null || rock.strID == skipId) continue;
                var situ = rock.objSS;
                // Field-locked rocks sit at a fixed offset from their field; no native state is touched here.
                bool locked = situ.bBOLocked && situ.strBOPORShip == field.strName;
                double x = locked ? field.dXReal + situ.vBOOffsetx : situ.vPosx, y = locked ? field.dYReal + situ.vBOOffsety : situ.vPosy;
                var position = new NavVector((x - ownX) / AutoNavCore.M_TO_AU, (y - ownY) / AutoNavCore.M_TO_AU);
                double contact = (situ.GetRadiusAU() + ownRadius) / AutoNavCore.M_TO_AU;
                if (position.Finite && ObstacleRoute.Distance(startM, goalM, position) <= contact + reachM) candidates.Add(rock);
            }
        }
        foreach (var rock in candidates)
            into.Add(new SensedObject(rock.strID, rock.objSS, NativeContactReader.Read(own, rock.strID)));
        candidates.Clear();
    }
}
