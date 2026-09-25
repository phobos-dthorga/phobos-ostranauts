using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using Ostranauts.Utils;

// Native boundary check for the unresolved close-work model. No Unity scene,
// ship loading, collision mutation or installed/save data is touched.
internal static class ShipbreakerGeometryChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var port = DataHandler.dictCOs["MooringPort"];
        check(port.strItemDef == "Blank" && (port.mapPoints == null || !port.mapPoints.Any(p => p.StartsWith("DockA,") || p.StartsWith("DockB,"))),
            "Native mooring anchors use coincident origins; changed anchors require a capture adapter review");
        check(port.aStartingConds.Any(c => c.StartsWith("IsMooringPort=")) && port.aStartingConds.Any(c => c.StartsWith("IsDockSys=")), "Capture uses native mooring metadata, never a renamed G4");
        check(typeof(CrimeManager).GetMethods().Count(m => m.Name == "ClearCrimeFlags") == 1, "Scoped crime-preservation patch has one unambiguous native target");
        foreach (int ownAngle in new[] { 0, 90, 180, 270 })
        foreach (int targetAngle in new[] { 0, 90, 180, 270 })
        {
            int rotation = GridUtils.GetIncomingDockRotation(ownAngle, targetAngle, out _);
            check((targetAngle + rotation) % 360 == (ownAngle + 180) % 360, "Native fit rotation opposes mooring endpoints for every mounting");
        }
        // Read metadata rather than letting the compiler inline a game constant.
        double metresPerTile = Convert.ToDouble(typeof(CrewSim)
            .GetField("M_PER_TILE", BindingFlags.Public | BindingFlags.Static)!.GetRawConstantValue());
        check(Math.Abs(metresPerTile - .32) < 1e-7, "Native deck scale changed; revisit G4 physical reach before admission");
        var own = (ShipSitu)RuntimeHelpers.GetUninitializedObject(typeof(ShipSitu));
        var target = (ShipSitu)RuntimeHelpers.GetUninitializedObject(typeof(ShipSitu));
        own.SetSize(20); target.SetSize(20);
        check(own.Size == 400 && target.Size == 400, "Native twenty-unit floor-span collision radius remains 400 metres");
        double collision = CollisionManager.GetCollisionDistanceAU(own, target);
        check(Math.Abs(collision - own.GetRadiusAU() - target.GetRadiusAU()) < 1e-15,
            "Native free-flight collision boundary is the sum of radii");

        // Synthetic 21-cell square, floor-centre span 20. Even a generous full
        // three-tile G4 extension cannot bridge these circles under a centred,
        // physical-deck interpretation. This is not a measured cutter reach.
        double physicalBound = 2 * Math.Sqrt(2) * 10.5 * metresPerTile + 3 * metresPerTile;
        check(physicalBound < 11 && own.Size + target.Size > physicalBound,
            "Physical G4 contact does not follow from native circle proximity");
        target.SetSize(10);
        check(target.Size == 200, "Explicit SetSize changes radius; removal still needs a verified refresh path");
        target.SetSize(1);
        check(target.Size == ShipSitu.MINSTATIONSIZE && target.Size > 400,
            "Final one-unit remnant uses the native minimum-body branch, not a shrinking-to-zero radius");
    }
}
