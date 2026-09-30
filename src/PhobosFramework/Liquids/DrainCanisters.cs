using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Drain canisters (Framework 0.63.0): a portable 20 litre canister that holds one liquid drained from a line,
/// as a saved record, with its native mass the housing plus the liquid. A filled canister is ordinary cargo, so the game's
/// own Haul orders (and hauling mods such as Common Sense) move it. Placed in the inventory of an installed store that
/// holds the same liquid, it pours in on the next two-second pass through the store's conversion journal, and the empty
/// canister stays there for reuse.</summary>
public static class DrainCanisters
{
    public const string Record = "DrainCanister";
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, Record, FrameworkInfo.PluginId, 1);
    public static bool Is(CondOwner? co) => co != null && !co.bDestroyed && co.strCODef == DrainCanisterRules.Id;

    /// <summary>The liquid a canister holds and how much; ("", 0) when empty, null when its record is unreadable.</summary>
    public static (string Commodity, double Kg)? Contents(CondOwner co)
    {
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Missing) return ("", 0);
        if (status != SavedStateStatus.Ready || !fields.TryGetValue("commodity", out var name) || !fields.TryGetValue("kg", out var text) ||
            !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) || !LineGeometry.Finite(kg) || kg < 0) return null;
        return (name, kg);
    }
    public static string? Commodity(CondOwner co) => Contents(co) is { Kg: > LineMixture.Tolerance } c ? c.Commodity : null;
    /// <summary>The kilograms of a liquid a canister still has room for (none for another liquid or an unreadable record).</summary>
    public static double Room(CondOwner co, LineCommodity liquid)
    {
        var c = Contents(co);
        if (c == null || c.Value.Kg > LineMixture.Tolerance && c.Value.Commodity != liquid.Name) return 0;
        return Math.Max(0, liquid.CanisterKg - c.Value.Kg);
    }
    /// <summary>Adds liquid to a canister and its native mass (the game carries the change to whatever holds it).</summary>
    public static void Fill(CondOwner co, string commodity, double kg)
    {
        var c = Contents(co) ?? throw new InvalidOperationException("Unreadable drain canister record.");
        if (c.Kg > LineMixture.Tolerance && c.Commodity != commodity) throw new InvalidOperationException("A drain canister holds one liquid.");
        Set(co, commodity, c.Kg + kg, kg);
    }
    private static void Set(CondOwner co, string commodity, double kg, double delta)
    {
        bool empty = kg <= LineMixture.Tolerance;
        var fields = new Dictionary<string, string> { ["commodity"] = empty ? "none" : commodity, ["kg"] = (empty ? 0 : kg).ToString("R", CultureInfo.InvariantCulture) };
        if (!Store(co).TryWrite(fields)) throw new InvalidOperationException("Protected drain canister record.");
        if (Math.Abs(delta) > 1e-12) co.AddMass(delta, true);
    }

    /// <summary>A canister for draining a run holding <paramref name="liquids"/>: one the worker carries first, then one
    /// lying loose on the deck within <see cref="DrainCanisterRules.ReachTiles"/> of the segment. A canister already holding
    /// one of the run's liquids with room left comes before an empty one.</summary>
    public static CondOwner? Find(CondOwner? actor, CondOwner segment, IEnumerable<string> liquids)
    {
        var wanted = new HashSet<string>(liquids, StringComparer.Ordinal);
        bool Usable(CondOwner c)
        {
            if (!Is(c)) return false;
            var contents = Contents(c);
            return contents != null && (contents.Value.Kg <= LineMixture.Tolerance || wanted.Contains(contents.Value.Commodity) && Headroom(c, contents.Value));
        }
        var carried = actor?.GetCOsSafe(false).Where(Usable) ?? Enumerable.Empty<CondOwner>();
        var nearby = segment.ship == null ? Enumerable.Empty<CondOwner>() : segment.ship.GetCOs(null, false, false, true)
            .Where(c => c != null && c.ship == segment.ship && c.objCOParent == null && c.slotNow == null && Near(c, segment) && Usable(c));
        return carried.Concat(nearby).OrderBy(c => Contents(c)!.Value.Kg > LineMixture.Tolerance ? 0 : 1).ThenBy(c => c.strID, StringComparer.Ordinal).FirstOrDefault();
    }
    private static bool Headroom(CondOwner c, (string Commodity, double Kg) contents)
    {
        var liquid = LineContents.Families.Select(f => f.Of(contents.Commodity)).FirstOrDefault(x => x != null && !x.Gas);
        return liquid != null && contents.Kg < liquid.CanisterKg - LineMixture.Tolerance;
    }
    private static bool Near(CondOwner a, CondOwner b)
    {
        var p = a.GetPos(); var q = b.GetPos();
        return Math.Max(Math.Abs(p.x - q.x), Math.Abs(p.y - q.y)) <= DrainCanisterRules.ReachTiles + 0.01;
    }

    /// <summary>Pours every filled canister in an installed store's inventory into that store when it holds the same liquid.</summary>
    internal static void Pour(Ship ship)
    {
        foreach (var vessel in ship.GetCOs(null, false, false, true))
        {
            if (vessel?.objContainer == null || vessel.ship != ship || vessel.objContainer.ContainedCOs.Count == 0 || !vessel.HasCond("IsInstalled") || vessel.HasCond("IsDamaged")) continue;
            var spec = BulkVessels.Of(vessel);
            if (spec == null) continue;
            foreach (var canister in vessel.objContainer.ContainedCOs.Where(Is).ToArray())
                if (Contents(canister) is { Kg: > LineMixture.Tolerance } c && c.Commodity == spec.Commodity) PourInto(vessel, spec, canister, c.Kg);
        }
    }
    private static void PourInto(CondOwner vessel, BulkVesselSpec spec, CondOwner canister, double kg)
    {
        if (BulkVessel.Protected(vessel) || CommodityReservations.Held(vessel.strID)) return;
        BufferedDrains.Settle(vessel);
        var s = BulkVessel.Read(vessel, spec);
        double moved = Math.Min(kg, spec.CapacityKg - s.ServiceKg - s.CatchKg);
        if (moved <= LineMixture.Tolerance) return;
        BulkVessel.BeginConversion(vessel, DrainCanisterRules.Id, s.ServiceKg);
        // The canister's mass leaves the store's cargo as the same kilograms join its contents: the store's mass is unchanged.
        Set(canister, spec.Commodity, kg - moved, -moved);
        s.SetService(s.ServiceKg + moved);
        BulkVessel.Save(vessel, spec, s);
        BulkVessel.EndConversion(vessel);
        FrameworkLifecycle.Log(Text.Get("DrainCanister.poured_log", canister.strID, moved, spec.Commodity, vessel.strID));
    }
}
