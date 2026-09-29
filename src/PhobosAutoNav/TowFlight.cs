using System;
using System.Collections.Generic;
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

    // 29 September 2026 pass (FF5): the guard asks about the own ship's docked partners once per other ship and
    // per obstacle in a step, and the game builds that dictionary afresh on every call. Within one physics step
    // (opened and closed by the navigation service) the partner list and the envelope radius are read once; the
    // docking topology and the partners' positions only change in native physics, outside the step.
    private static bool stepOpen;
    private static Ship? stepShip;
    private static readonly List<Ship> stepPartners = new();
    private static double stepRadius;
    internal static void BeginStep() { stepOpen = true; stepShip = null; }
    internal static void EndStep() { stepOpen = false; stepShip = null; stepPartners.Clear(); }
    private static IReadOnlyList<Ship> Partners(Ship own)
    {
        if (stepOpen && stepShip == own) return stepPartners;
        stepPartners.Clear();
        if (own.IsDocked()) foreach (var peer in own.GetDockedShipsAndPortIDs().Values) stepPartners.Add(peer);
        stepShip = stepOpen ? own : null;
        stepRadius = double.NaN;
        return stepPartners;
    }

    internal static bool Contains(Ship own, Ship? other)
    {
        if (other == null || own == other) return false;
        var partners = Partners(own);
        for (int i = 0; i < partners.Count; i++) if (partners[i] == other) return true;
        return false;
    }

    // A rotation-independent envelope about the piloted ship protects the entire pair.
    // Native RCSAccelMax/DeltaVRemainingRCS already include the attached mass.
    internal static double RadiusAU(Ship own)
    {
        var partners = Partners(own);
        if (stepOpen && stepShip == own && !double.IsNaN(stepRadius)) return stepRadius;
        double radius = own.objSS.GetRadiusAU();
        for (int i = 0; i < partners.Count; i++)
        {
            var peer = partners[i];
            if (peer?.objSS == null) { radius = double.NaN; break; }
            double x = peer.objSS.vPosx - own.objSS.vPosx, y = peer.objSS.vPosy - own.objSS.vPosy;
            radius = Math.Max(radius, Math.Sqrt(x*x+y*y) + peer.objSS.GetRadiusAU());
        }
        if (stepOpen && stepShip == own && !double.IsNaN(radius)) stepRadius = radius;
        return radius;
    }
    internal static double CollisionAU(Ship own, ShipSitu other) =>
        CollisionManager.GetCollisionDistanceAU(own.objSS, other) + RadiusAU(own) - own.objSS.GetRadiusAU();
}
