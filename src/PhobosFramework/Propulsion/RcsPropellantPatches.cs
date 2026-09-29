using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Processing;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Propulsion;

/// <summary>The game's RCS gas accounting with each gas at its own worth. For loaded ships these reproduce the
/// native loops exactly (regulators in order, their GasInput points, the game's TIsRCSValidInput trigger, each
/// container's own GasContainer.RemoveGasMass) and add registered content feeds on the same tiles. Shallow-loaded
/// ships keep the native nitrogen model untouched. Every total is in nitrogen-equivalent kilograms, the unit the
/// engine's thrust, delta-v and fuel planning already use, so a nitrogen-only ship is unchanged.</summary>
internal static class RcsInputs
{
    internal sealed class Input { internal CondOwner Co = null!; internal IRcsPropellantFeed? Feed; }
    /// <summary>Draw passes: feeds that asked to go first, then the native canisters, then the other feeds.</summary>
    internal static readonly int[] Passes = { 0, 1, 2 };
    private static readonly HashSet<CondOwner> seen = new();
    private static readonly List<CondOwner> list = new();
    private static readonly StepMemo<Ship, List<Input>> inputsByStep = new();
    /// <summary>The ship's inputs for this step: the tiles and their occupants do not change within one step, and
    /// drawing changes masses, not the list, so the nav display, the flight controller and the manoeuvre all share
    /// one collection per step.</summary>
    internal static List<Input> Collect(Ship ship, List<CondOwner>? distros, CondTrigger? nativeInput)
    {
        if (inputsByStep.TryGet(NativeSteps.Frame, ship, out var cached)) return cached;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.RcsCollect);
        var inputs = Collect(distros, nativeInput);
        inputsByStep.Set(NativeSteps.Frame, ship, inputs);
        return inputs;
    }
    /// <summary>Everything on the regulators' gas-input tiles, in the native order.</summary>
    internal static List<Input> Collect(List<CondOwner>? distros, CondTrigger? nativeInput)
    {
        var inputs = new List<Input>();
        if (distros == null) return inputs;
        seen.Clear();
        foreach (var distro in distros)
        {
            if (distro == null || distro.ship == null || distro.mapPoints == null) continue;
            foreach (var point in distro.mapPoints)
            {
                if (point.Key.IndexOf("GasInput", StringComparison.Ordinal) < 0) continue;
                list.Clear();
                // No trigger here, so content feeds on the tile are found too; natives are filtered by the game's own rule.
                distro.ship.GetCOsAtWorldCoords1(distro.GetPos(point.Key), null, true, false, list);
                foreach (var co in list)
                {
                    if (co == null || co.bDestroyed || !seen.Add(co)) continue;
                    var feed = RcsPropellant.AnyFeeds ? RcsPropellant.FeedFor(co) : null;
                    if (feed != null) inputs.Add(new Input { Co = co, Feed = feed });
                    else if (nativeInput != null && nativeInput.Triggered(co, null, false) && co.GasContainer != null) inputs.Add(new Input { Co = co });
                }
            }
        }
        return inputs;
    }
    private static bool Loaded(Ship ship) => ship.LoadState > Ship.Loaded.Shallow;

    [HarmonyPatch(typeof(Ship), nameof(Ship.RemoveGasMass))]
    internal static class Remove
    {
        private static bool Prefix(Ship __instance, float fMassNeeded, List<CondOwner> ___aRCSDistros, CondTrigger ___ctRCSGasInput, ref float __result)
        {
            if (fMassNeeded <= 0 || !Loaded(__instance)) return true;
            try
            {
                var inputs = Collect(__instance, ___aRCSDistros, ___ctRCSGasInput);
                double need = fMassNeeded, served = 0;
                // Feeds that asked to go first, then the native canisters, then the other feeds.
                foreach (int pass in Passes)
                    foreach (var input in inputs)
                    {
                        if (need - served <= 1e-9) break;
                        if (input.Feed == null)
                        {
                            if (pass != 1) continue;
                            double ratio = RcsPropellant.ContainerRatio(input.Co.GasContainer);
                            double removed = input.Co.GasContainer.RemoveGasMass((need - served) / ratio);
                            if (removed > 0 && !double.IsNaN(removed)) served += removed * ratio;
                        }
                        else
                        {
                            bool first = input.Feed.DrawFirst(input.Co);
                            if (pass == 1 || first != (pass == 0)) continue;
                            double offered = input.Feed.Offer(input.Co, need - served);
                            if (offered > 0 && !double.IsNaN(offered)) served += Math.Min(offered, need - served);
                        }
                    }
                __result = (float)Math.Min(served, need);
                return false;
            }
            catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); return true; }
        }
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.GetRCSRemain))]
    internal static class Remain
    {
        private static bool Prefix(Ship __instance, List<CondOwner> ___aRCSDistros, CondTrigger ___ctRCSGasInput, ref double __result)
        {
            if (!Loaded(__instance)) return true;
            try
            {
                double total = 0;
                foreach (var input in Collect(__instance, ___aRCSDistros, ___ctRCSGasInput))
                    total += input.Feed != null ? Math.Max(0, input.Feed.ReserveEquivalentKg(input.Co))
                        : input.Co.GasContainer.Mass * RcsPropellant.ContainerRatio(input.Co.GasContainer);
                __result = total;
                return false;
            }
            catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); return true; }
        }
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.GetRCSMax))]
    internal static class Max
    {
        private static bool Prefix(Ship __instance, List<CondOwner> ___aRCSDistros, CondTrigger ___ctRCSGasInput, ref double __result)
        {
            if (!Loaded(__instance)) return true;
            try
            {
                double total = 0;
                foreach (var input in Collect(__instance, ___aRCSDistros, ___ctRCSGasInput))
                {
                    if (input.Feed != null) { total += Math.Max(0, input.Feed.CapacityEquivalentKg(input.Co)); continue; }
                    // The game's own capacity arithmetic at its 293 K, in the canister's rated species (nitrogen when unknown).
                    string species = NativeGasCanister.Species(input.Co) ?? RcsPropellant.Reference;
                    double moles = input.Co.GetCondAmount("StatGasPressureMax") * input.Co.GetCondAmount("StatVolume") / 293.0 / NativeGasCanister.GasConstantKJPerMolK;
                    total += GasContainer.GetGasMass(species, moles) * RcsPropellant.ExhaustRatio(species);
                }
                __result = total;
                return false;
            }
            catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); return true; }
        }
    }
}
