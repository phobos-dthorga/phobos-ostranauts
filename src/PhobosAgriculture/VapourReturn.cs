using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture;

/// <summary>Transpired water a full rack cannot keep (Agriculture 0.43.0). A rack condenses its plants' vapour back into
/// its own reservoir while there is room; what is left over used to vanish, because the game's air has no water vapour
/// to receive it. It now goes to a Framework water tank the rack touches or shares a process-water line with, the
/// owner's link rule, so the water stays in the ship's books. With no such tank, or a full or protected one, the
/// remainder is lost as before. The chosen tank is looked up at most every <see cref="RecheckSeconds"/>.</summary>
internal static class VapourReturn
{
    internal const double RecheckSeconds = 30, MinimumKg = 1e-9;

    /// <summary>Puts up to <paramref name="kg"/> of condensate into the rack's tank and returns what was kept.</summary>
    internal static double Deposit(Service.Session s, double kg)
    {
        if (!CropStateFinite(kg) || kg <= MinimumKg || s.Object.ship == null) return 0;
        if (StarSystem.fEpoch >= s.VapourCheck)
        {
            s.VapourTank = Pick(s.Object)?.strID ?? "";
            s.VapourCheck = StarSystem.fEpoch + RecheckSeconds;
        }
        var tank = Service.Resolve(s.VapourTank);
        if (tank == null || !Usable(s.Object, tank)) { s.VapourCheck = 0; return 0; }
        var spec = BulkVessels.Of(tank)!;
        var state = BulkVessel.TryRead(tank, spec);
        if (state == null) return 0;
        double kept = Math.Min(kg, Math.Max(0, spec.CapacityKg - state.TotalKg));
        if (kept <= MinimumKg) return 0;
        state.SetService(state.ServiceKg + kept);
        BulkVessel.Save(tank, spec, state);
        return kept;
    }

    /// <summary>The nearest usable water tank the rack reaches: touching first, then fewest line steps.</summary>
    internal static CondOwner? Pick(CondOwner rack) =>
        BulkVessels.Aboard(rack.ship, LineCommodities.Water).Where(t => Usable(rack, t))
            .OrderBy(t => LineReach.Hops(rack, t, LineFamilies.ProcessWater)).ThenBy(t => t.strID, StringComparer.Ordinal).FirstOrDefault();

    private static bool Usable(CondOwner rack, CondOwner tank) =>
        !tank.bDestroyed && tank.ship == rack.ship && BulkVessels.Of(tank)?.Commodity == LineCommodities.Water && tank.HasCond("IsInstalled") && !tank.HasCond("IsDamaged") &&
        NativeFluidRoute.EndpointReady(tank) && !BulkVessel.Protected(tank) && !CommodityReservations.Held(tank.strID) &&
        LineReach.Of(rack, tank, LineFamilies.ProcessWater) != LineReachKind.None;

    private static bool CropStateFinite(double x) => Core.CropState.Finite(x);
}
