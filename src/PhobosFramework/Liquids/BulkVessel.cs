using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>What happens to contents when a vessel's native damage mode switch fires: <c>Isolate</c> traps them in
/// the catch chamber for later recovery (silos, reservoirs); <c>Leak</c> leaves them in service and the owning
/// service drains them at the declared rate until repaired (pressurised stores).</summary>
public enum VesselDamagePolicy { Isolate, Leak }

/// <summary>What a content mod declares for a family of bulk vessels (silos, reservoirs, tanks): the definition
/// prefix, the one commodity it holds, its capacity and dry mass, and the names of its saved records. Framework
/// keeps the quantity as its own saved record (never a native stat), keeps the object's native mass equal to
/// dry mass plus contents plus cargo, journals every transfer and conversion, and never assumes a density.</summary>
public sealed class BulkVesselSpec
{
    public string Family { get; }
    public string Commodity { get; }
    public double CapacityKg { get; }
    public double DryKg { get; }
    public string Owner { get; }
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    public VesselDamagePolicy DamagePolicy { get; }
    /// <summary>Contents lost per hour while damaged, for the Leak policy; zero otherwise.</summary>
    public double LeakKgPerHour { get; }
    public BulkVesselSpec(string family, string commodity, double capacityKg, double dryKg, string owner, string record, string journal, string guard)
        : this(family, commodity, capacityKg, dryKg, owner, record, journal, guard, VesselDamagePolicy.Isolate, 0) { }
    public BulkVesselSpec(string family, string commodity, double capacityKg, double dryKg, string owner, string record, string journal, string guard,
        VesselDamagePolicy damagePolicy, double leakKgPerHour)
    {
        if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(commodity) || string.IsNullOrWhiteSpace(owner) ||
            string.IsNullOrWhiteSpace(record) || string.IsNullOrWhiteSpace(journal) || string.IsNullOrWhiteSpace(guard) ||
            new[] { record, journal, guard }.Distinct(StringComparer.Ordinal).Count() != 3 ||
            !Finite(capacityKg) || capacityKg <= 0 || !Finite(dryKg) || dryKg < 0) throw new ArgumentException("Invalid bulk vessel specification.");
        if (!Finite(leakKgPerHour) || leakKgPerHour < 0 || (damagePolicy == VesselDamagePolicy.Leak) != (leakKgPerHour > 0))
            throw new ArgumentException("A leaking vessel declares a positive leak rate; an isolating vessel declares none.");
        Family = family; Commodity = commodity; CapacityKg = capacityKg; DryKg = dryKg; Owner = owner; Record = record; Journal = journal; Guard = guard;
        DamagePolicy = damagePolicy; LeakKgPerHour = leakKgPerHour;
    }
    /// <summary>Contents a damaged leaking vessel loses over <paramref name="hours"/>, bounded by what it holds.</summary>
    public double LeakKg(double hours, double heldKg)
    {
        if (!Finite(hours) || hours < 0 || !Finite(heldKg) || heldKg < 0) throw new ArgumentException("Invalid leak interval.");
        return Math.Min(heldKg, LeakKgPerHour * hours);
    }
    /// <summary>Capacity from a volume at a density the content declares (kg per cubic metre); water is 1,000,
    /// heavy water 1,107, liquid helium-3 129 in the game's own refuelling code. Framework never picks one.</summary>
    public static double CapacityFromVolume(double cubicMetres, double densityKgPerM3) =>
        !Finite(cubicMetres) || !Finite(densityKgPerM3) || cubicMetres <= 0 || densityKgPerM3 <= 0 ?
            throw new ArgumentException("Invalid volume or density.") : cubicMetres * densityKgPerM3;
    internal static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
}

/// <summary>A read-only view for panels, consoles and station views. Headroom is zero while Protected.</summary>
public sealed class BulkVesselSnapshot
{
    public string ObjectId { get; }
    public string ShipId { get; }
    public string Commodity { get; }
    public double ServiceKg { get; }
    public double CatchKg { get; }
    public double ReserveKg { get; }
    public double CapacityKg { get; }
    public long Revision { get; }
    public bool Protected { get; }
    public double AvailableKg => Math.Max(0, ServiceKg - ReserveKg);
    public double HeadroomKg => Protected ? 0 : Math.Max(0, CapacityKg - ServiceKg - CatchKg);
    public BulkVesselSnapshot(string objectId, string shipId, string commodity, double service, double catchKg, double reserve, double capacity, long revision, bool isProtected)
    { ObjectId = objectId; ShipId = shipId; Commodity = commodity; ServiceKg = service; CatchKg = catchKg; ReserveKg = reserve; CapacityKg = capacity; Revision = revision; Protected = isProtected; }
}

/// <summary>The registry of vessel families, so a producer in one mod can deliver into a vessel of another
/// (a thaw unit into a silo or an agricultural reservoir) through the same guarded transfer.</summary>
public static class BulkVessels
{
    private static readonly List<BulkVesselSpec> specs = new();
    /// <summary>Registers a family. Registering the same family again for the same owner replaces the earlier
    /// declaration (content prepares definitions on every load); another owner cannot claim a registered family.</summary>
    public static void Register(BulkVesselSpec spec)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));
        if (specs.Any(s => s.Family == spec.Family && s.Owner != spec.Owner || s.Record == spec.Record && s.Owner == spec.Owner && s.Family != spec.Family))
            throw new ArgumentException("Bulk vessel family or record already registered by another declaration: " + spec.Family);
        specs.RemoveAll(s => s.Family == spec.Family);
        specs.Add(spec);
    }
    public static void Unregister(string owner) => specs.RemoveAll(s => s.Owner == owner);
    public static IReadOnlyList<BulkVesselSpec> All => specs.AsReadOnly();
    public static BulkVesselSpec? SpecFor(string? definition) => definition == null ? null : specs.FirstOrDefault(s => EquipmentIdentity.IsFamily(definition, s.Family));
    /// <summary>The declaration for a live object, by its definition id (a distinct name keeps the string lookup
    /// usable from code that does not reference the game assembly).</summary>
    public static BulkVesselSpec? Of(CondOwner? co) => co == null ? null : SpecFor(co.strCODef);
    public static bool IsVessel(CondOwner? co) => Of(co) != null;
    /// <summary>Installed, undamaged, ready vessels of one commodity aboard a ship.</summary>
    public static IEnumerable<CondOwner> Aboard(Ship? ship, string commodity) => ship == null ? Enumerable.Empty<CondOwner>() :
        ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && Of(c)?.Commodity == commodity &&
            c.HasCond("IsInstalled") && NativeFluidRoute.EndpointReady(c)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
}

/// <summary>Custody of one registered vessel: the saved record, its journals and guard, the native mass
/// invariant, owner-confirmed acceptance, and the reservoir endpoint used by guarded transfers.</summary>
public static class BulkVessel
{
    public const double MassToleranceKg = 1e-5;
    public static ObjectStateStore Store(CondOwner co, BulkVesselSpec spec) => new(co.mapGUIPropMaps, spec.Record, spec.Owner, 1);
    public static ObjectStateStore Journal(CondOwner co, BulkVesselSpec spec) => new(co.mapGUIPropMaps, spec.Journal, spec.Owner, 1);
    public static LiquidTransferGuard Guard(CondOwner co) => Guard(co, Spec(co));
    public static LiquidTransferGuard Guard(CondOwner co, BulkVesselSpec spec) => new(co.mapGUIPropMaps, spec.Guard, spec.Owner);
    public static BulkVesselSpec Spec(CondOwner co) => BulkVessels.Of(co) ?? throw new InvalidOperationException("Not a registered bulk vessel: " + co?.strCODef);
    /// <summary>Physical cargo the game already adds to the object's own mass: contents of its container.</summary>
    public static double Cargo(CondOwner co) => co.objContainer?.ContainedCOs.Sum(x => x.GetTotalMass()) ?? 0;
    /// <summary>The native mass the object must show for a record: dry housing plus contents plus cargo.</summary>
    public static double ExpectedMassKg(BulkVesselSpec spec, StoredCommodity state, double cargoKg) => spec.DryKg + state.TotalKg + cargoKg;
    public static bool MassMatches(double actualKg, double expectedKg) => BulkVesselSpec.Finite(actualKg) && BulkVesselSpec.Finite(expectedKg) && Math.Abs(actualKg - expectedKg) <= MassToleranceKg;
    /// <summary>The saved contents. Throws when the record is unreadable or the object's native mass no longer
    /// equals dry mass plus contents plus cargo: the vessel is then Protected until the owner accepts it.</summary>
    public static StoredCommodity Read(CondOwner co) => Read(co, Spec(co));
    public static StoredCommodity Read(CondOwner co, BulkVesselSpec spec)
    {
        var s = ReadRecord(co, spec) ?? throw new InvalidOperationException("Protected bulk vessel record.");
        if (!MassMatches(co.GetCondAmount("StatMass"), ExpectedMassKg(spec, s, Cargo(co)))) throw new InvalidOperationException("Bulk vessel physical mass mismatch.");
        return s;
    }
    private static StoredCommodity? ReadRecord(CondOwner co, BulkVesselSpec spec)
    {
        var status = Store(co, spec).Read(out var fields);
        return status == SavedStateStatus.Ready ? StoredCommodity.Read(fields, spec.Commodity, spec.CapacityKg) :
            status == SavedStateStatus.Missing ? new StoredCommodity(spec.Commodity, spec.CapacityKg) : null;
    }
    public static void Save(CondOwner co, StoredCommodity state) => Save(co, Spec(co), state);
    public static void Save(CondOwner co, BulkVesselSpec spec, StoredCommodity state)
    {
        if (state.Commodity != spec.Commodity) throw new InvalidOperationException("Wrong commodity for this vessel.");
        if (!Store(co, spec).TryWrite(state.Save())) throw new InvalidOperationException("Protected bulk vessel save.");
        co.AddMass(ExpectedMassKg(spec, state, Cargo(co)) - co.GetCondAmount("StatMass"), true);
    }
    public static bool Protected(CondOwner co)
    {
        var spec = BulkVessels.Of(co);
        if (spec == null) return true;
        try
        {
            Read(co, spec);
            var status = Journal(co, spec).Read(out var d);
            return Guard(co, spec).Protected || status != SavedStateStatus.Missing && (status != SavedStateStatus.Ready || d.Count != 1 || d["state"] != "clear");
        }
        catch { return true; }
    }
    /// <summary>Owner-confirmed recovery: a readable record is trusted, interrupted transfer and conversion
    /// journals are closed, and the native mass is set back to dry mass plus record plus cargo. An unreadable
    /// record cannot be accepted; the vessel stays Protected until repaired.</summary>
    public static bool Accept(CondOwner co, Action<string>? log = null)
    {
        var spec = BulkVessels.Of(co);
        if (spec == null) return false;
        StoredCommodity? s = null;
        try { s = ReadRecord(co, spec); }
        catch (Exception e) { log?.Invoke(e.ToString()); }
        if (s == null || !Guard(co, spec).Resolve() || !Journal(co, spec).TryWrite(new Dictionary<string, string> { ["state"] = "clear" })) return false;
        Save(co, spec, s);
        return !Protected(co);
    }
    public static BulkVesselSnapshot Snapshot(CondOwner co)
    {
        var spec = Spec(co);
        try
        {
            var s = Read(co, spec);
            return new BulkVesselSnapshot(co.strID, co.ship?.strRegID ?? "", spec.Commodity, s.ServiceKg, s.CatchKg, s.ReserveKg, spec.CapacityKg, s.Revision, Protected(co));
        }
        catch { return new BulkVesselSnapshot(co.strID, co.ship?.strRegID ?? "", spec.Commodity, 0, 0, 0, spec.CapacityKg, 0, true); }
    }
    /// <summary>Durable evidence around a content conversion (an item becoming contents, or contents becoming an
    /// item): Begin before the first mutation, End after the last. An open journal leaves the vessel Protected.</summary>
    public static void BeginConversion(CondOwner co, string item, double beforeKg)
    {
        if (!Journal(co, Spec(co)).TryWrite(new Dictionary<string, string> { ["state"] = "pending", ["item"] = item, ["before"] = beforeKg.ToString("R", CultureInfo.InvariantCulture) }))
            throw new InvalidOperationException("Protected bulk vessel conversion.");
    }
    public static void EndConversion(CondOwner co)
    {
        if (!Journal(co, Spec(co)).TryWrite(new Dictionary<string, string> { ["state"] = "clear" })) throw new InvalidOperationException("Bulk vessel conversion incomplete.");
    }
    /// <summary>Contents that leave the vessel without a receiver (a leak, an explicit discharge overboard):
    /// service contents fall by up to <paramref name="kg"/>, the loss is logged, and the kilograms actually
    /// removed are returned. A protected vessel loses nothing here.</summary>
    public static double Drain(CondOwner co, double kg, string reason) => Drain(co, kg, reason, true);
    /// <summary>As <see cref="Drain(CondOwner, double, string)"/>; <paramref name="log"/> false leaves the loss unlogged
    /// for callers that settle many small draws and log once themselves (<see cref="BufferedDrains"/>).</summary>
    public static double Drain(CondOwner co, double kg, string reason, bool log)
    {
        if (!BulkVesselSpec.Finite(kg) || kg < 0) throw new ArgumentException("Invalid drain amount.");
        var spec = Spec(co);
        if (Protected(co)) return 0;
        var s = Read(co, spec);
        double removed = Math.Min(kg, s.ServiceKg);
        if (removed <= 0) return 0;
        s.SetService(s.ServiceKg - removed);
        Save(co, spec, s);
        if (log) FrameworkLifecycle.Log(Text.Get("BulkVessel.drained", co.strID, spec.Commodity, removed, reason));
        return removed;
    }
    /// <summary>The vessel as a reservoir for guarded transfers; contents in the catch chamber take capacity.</summary>
    public sealed class Endpoint : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly BulkVesselSpec spec;
        public Endpoint(CondOwner co) { this.co = co; spec = Spec(co); }
        public string Identity => co.strID;
        public string ShipId => co.ship.strRegID;
        public string Commodity => spec.Commodity;
        public double QuantityKg => Read(co, spec).ServiceKg;
        public double CapacityKg => spec.CapacityKg - Read(co, spec).CatchKg;
        public void SetQuantity(double kg) { var s = Read(co, spec); s.SetService(kg); Save(co, spec, s); }
    }
}

// The game creates the replacement under the old ID before it calls ModeSwitch, so blocking here would orphan
// that replacement. Contents follow into a successor of the same family; a damaged successor isolates them
// (Isolate policy) or keeps them in service for the owner's leak (Leak policy).
// Owner decision (28 September 2026): vanilla destructibility, with refusals only when work is offered.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class BulkVesselModeSwitch
{
    private static void Prefix(CondOwner __instance, CondOwner coNew, out StoredCommodity? __state)
    {
        __state = null;
        var spec = BulkVessels.Of(__instance);
        if (spec == null || BulkVessels.Of(coNew) != spec) return;
        try { __state = BulkVessel.Read(__instance, spec); } catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); }
    }
    private static void Postfix(CondOwner coNew, StoredCommodity? __state)
    {
        if (__state == null) return;
        try
        {
            if (coNew.HasCond("IsDamaged") && BulkVessels.Of(coNew)?.DamagePolicy == VesselDamagePolicy.Isolate) __state.Isolate();
            BulkVessel.Save(coNew, __state);
        }
        catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); }
    }
}
// Native destruction proceeds. Contents that vanish with a destroyed vessel are logged, never blocked; the old
// half of a mode switch and load-time cleanup are lifecycle disposal, not lost material.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class BulkVesselDestroy
{
    private static void Prefix(CondOwner __instance)
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || !BulkVessels.IsVessel(__instance) || __instance.HasCond("IsModeSwitching", false)) return;
        try { var s = BulkVessel.Read(__instance); if (s.TotalKg > 1e-8) FrameworkLifecycle.Log(Text.Get("BulkVessel.lost", __instance.strID, s.Commodity, s.TotalKg)); } catch { }
    }
}
