using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>A machine that takes a drain canister's liquid from its own inventory (Framework 0.64.0). <see cref="Accept"/>
/// records up to <c>kg</c> of the commodity in the machine's own custody, adds it to the machine's own mass, and returns
/// the kilograms taken (zero to refuse); Framework then takes the same kilograms out of the canister.</summary>
public interface ICanisterReceiver
{
    string Id { get; }
    bool Handles(CondOwner machine);
    double Accept(CondOwner machine, string commodity, double kg);
}

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

    private static readonly List<ICanisterReceiver> receivers = new();
    /// <summary>Lets a machine that is not a bulk vessel take a canister's liquid when one is put in its inventory
    /// (Framework 0.64.0: the F6 furnace takes coolant, the W2 takes water and its own feed). One receiver per id.</summary>
    public static void RegisterReceiver(ICanisterReceiver receiver)
    {
        if (receiver == null) throw new ArgumentNullException(nameof(receiver));
        receivers.RemoveAll(r => r.Id == receiver.Id);
        receivers.Add(receiver);
    }
    /// <summary>Pours every filled canister in an installed store's inventory into that store when it holds the same
    /// liquid, and into a registered receiver machine when it accepts it.</summary>
    internal static void Pour(Ship ship, IReadOnlyList<CondOwner> canisters)
    {
        for (int i = 0; i < canisters.Count; i++)
        {
            var canister = canisters[i];
            var holder = canister?.objCOParent;
            if (holder?.objContainer == null || holder.ship != ship || !Is(canister) || !holder.HasCond("IsInstalled") || holder.HasCond("IsDamaged")) continue;
            var spec = BulkVessels.Of(holder);
            ICanisterReceiver? receiver = null;
            if (spec == null) { foreach (var r in receivers) if (r.Handles(holder)) { receiver = r; break; } }
            if (spec == null && receiver == null) continue;
            if (Contents(canister!) is not { Kg: > LineMixture.Tolerance } c) continue;
            if (spec != null) { if (c.Commodity == spec.Commodity) PourInto(holder, spec, canister!, c.Kg); }
            else PourInto(holder, receiver!, canister!, c.Commodity, c.Kg);
        }
    }
    // Every drain canister in the world, from the shared sweep (a new one is seen within a cycle or two).
    private static readonly Discovery.WorldFamily family = Discovery.WorldFamilies.Register(FrameworkInfo.PluginId + ".drain-canisters", id => id == DrainCanisterRules.Id);
    private static readonly List<CondOwner> stowed = new();
    /// <summary>The drain canisters that sit inside something, read once per top-up pass for every ship.</summary>
    internal static IReadOnlyList<CondOwner> Stowed()
    {
        family.Members(stowed);
        stowed.RemoveAll(c => c == null || c.objCOParent == null);
        return stowed;
    }
    private static void PourInto(CondOwner holder, ICanisterReceiver receiver, CondOwner canister, string commodity, double kg)
    {
        // The receiver records the liquid and adds it to its own mass; the canister's loss reaches the holder as its cargo,
        // so the holder's mass is unchanged overall.
        double moved = receiver.Accept(holder, commodity, kg);
        if (!LineGeometry.Finite(moved) || moved <= LineMixture.Tolerance) return;
        if (moved > kg + 1e-9) throw new InvalidOperationException("A canister receiver took more than the canister held.");
        Set(canister, commodity, kg - moved, -moved);
        FrameworkLifecycle.Log(Text.Get("DrainCanister.poured_log", canister.strID, moved, commodity, holder.strID));
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
