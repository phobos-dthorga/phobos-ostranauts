using System;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Read-only native attachment policy. Never secure/release a brace or change its ports.
internal static class TowFlight
{
    internal static string? Problem(Ship ship)
    {
        if (!ship.IsDocked()) return null;
        var links = ship.GetDockedShipsAndPortIDs();
        if (links.Count != 1) return "Tow.single";
        var peer = links.Values.FirstOrDefault();
        if (peer == null || peer == ship || peer.bDestroyed || peer.objSS == null || ship.objSS == null ||
            CrewSim.system?.GetShipByRegID(peer.strRegID) != peer) return "Tow.unavailable";
        if (ship.IsMoored() || peer.IsMoored() || ship.objSS.bIsBO || peer.objSS.bIsBO ||
            ship.objSS.bBOLocked || peer.objSS.bBOLocked || ship.objSS.bGrounded || peer.objSS.bGrounded)
            return "NavigationService.undock_before_engagement";
        var back = peer.GetDockedShipsAndPortIDs();
        if (back.Count != 1 || back.Values.FirstOrDefault() != ship) return "Tow.single";
        if (ship.bCheckTowingBraces || peer.bCheckTowingBraces) return "Tow.updating";
        if (!ship.TowBraceSecured(peer.strRegID)) return "Tow.secure";
        if (TorchDriveController.ThrustRequested(peer) || peer.shipStationKeepingTarget != null ||
            peer.aWPs?.Count > 0 || AIShipManager.GetAIShipByRegID(peer.strRegID) != null) return "Tow.controls";
        return null;
    }

    internal static bool Contains(Ship own, Ship? other) => other != null && own != other && own.IsDocked() &&
        own.GetDockedShipsAndPortIDs().Values.Any(peer => peer == other);

    // A rotation-independent envelope about the piloted ship protects the entire pair.
    // Native RCSAccelMax/DeltaVRemainingRCS already include the attached mass.
    internal static double RadiusAU(Ship own)
    {
        double radius = own.objSS.GetRadiusAU();
        if (!own.IsDocked()) return radius;
        foreach (var peer in own.GetDockedShipsAndPortIDs().Values)
        {
            if (peer?.objSS == null) return double.NaN;
            double x = peer.objSS.vPosx - own.objSS.vPosx, y = peer.objSS.vPosy - own.objSS.vPosy;
            radius = Math.Max(radius, Math.Sqrt(x*x+y*y) + peer.objSS.GetRadiusAU());
        }
        return radius;
    }
    internal static double CollisionAU(Ship own, ShipSitu other) =>
        CollisionManager.GetCollisionDistanceAU(own.objSS, other) + RadiusAU(own) - own.objSS.GetRadiusAU();
}
