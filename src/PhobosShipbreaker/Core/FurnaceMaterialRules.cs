using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Physical material routing policy, independent of cycle permission.</summary>
public static class FurnaceMaterialRules
{
    public const string Aluminium = "ItmScrapAluminum";
    public static bool ChargeFull(int count) => count >= FurnaceRules.ChargeUnits;
    public static (double X, double Y) Point(bool input, double angle) => IntakeRules.Rotate(input ? -2.5 : 2.5, -2.5, angle);
    public static bool Accessible(FurnacePhase phase, bool safeOpen, bool protectedState, bool mutation) =>
        phase == FurnacePhase.Idle && safeOpen && !protectedState && !mutation;
    /// <summary>A released product of any recipe at its exact mass, including the native scrap gates.</summary>
    public static bool Product(string? id, double mass) => id != null && FurnaceRecipes.All.Any(r => r.Products.Any(p => p.Id == id && ProcessRules.MassMatches(mass, p.Kg)));
    public static bool IsProduct(string? id) => id != null && FurnaceRecipes.ProductIds.Contains(id);
    public static IReadOnlyList<string> ProductIds => FurnaceRecipes.ProductIds;
    /// <summary>The original aluminium rule, unchanged for callers without a recipe.</summary>
    public static bool Feed(string? id, double mass, bool detached, bool empty, bool unstacked, bool noLots) =>
        Feed(FurnaceRecipes.Housing, id, mass, detached, empty, unstacked, noLots);
    public static bool Feed(FurnaceRecipe recipe, string? id, double mass, bool detached, bool empty, bool unstacked, bool noLots) =>
        id == recipe.FeedId && ProcessRules.MassMatches(mass, FurnaceRules.FeedUnitKg) && detached && empty && unstacked && noLots;
    public static double MotorRequest(double seconds, double kW, double headroom, double reserved) =>
        ThermalMath.Finite(seconds) && seconds > 0 && seconds <= FurnaceRules.MaxIntervalSeconds && ThermalMath.Finite(kW) && kW > 0 &&
        ThermalMath.Finite(headroom) && ThermalMath.Finite(reserved) ? Math.Min(kW * seconds, Math.Max(0, headroom - reserved)) : 0;
    public static double MotorReceipt(double received, double requested, double instruments, double pump) =>
        Math.Min(requested, Math.Max(0, received - instruments - pump));
}
