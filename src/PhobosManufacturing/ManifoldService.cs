using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Propulsion;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The P1 manifold: Framework's RCS engine patch asks it for remass whenever a regulator draws, and it
/// takes the matching kilograms from its switched-on stores through Framework's buffered draws. Connection checks
/// (adjacent, or a propellant-line route) are cached for a few seconds; nothing heavy runs per frame. Commands
/// only change its own saved switches; the draw itself follows the engine's thrust.</summary>
internal sealed class ManifoldService : IRcsPropellantFeed
{
    internal static readonly ManifoldService Instance = new();
    public string Id => Plugin.Id + ".manifold";
    private sealed class Session
    {
        internal ManifoldState State = new();
        internal bool Protected;
        internal double NextCheck;
        internal Dictionary<string, string> Why = new(StringComparer.Ordinal);
        internal List<CondOwner> Ready = new();
        internal string LastSource = "";
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, ManifoldRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = ManifoldState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        sessions.Add(co, s);
        return s;
    }
    private static bool Save(CondOwner co, Session s)
    {
        if (Store(co).TryWrite(s.State.Save())) { s.NextCheck = 0; return true; }
        s.Protected = true; return false;
    }

    /// <summary>Whether the manifold sits on one of an installed RCS Intake Regulator's gas-input tiles.</summary>
    internal static bool OnRegulatorInput(CondOwner co)
    {
        if (co.ship == null) return false;
        var at = co.GetPos();
        foreach (var reg in co.ship.GetCOs(null, false, false, true))
        {
            if (reg == null || reg.bDestroyed || !reg.HasCond("IsRCSReg") || !reg.HasCond("IsInstalled") || reg.mapPoints == null) continue;
            foreach (var point in reg.mapPoints)
                if (point.Key.IndexOf("GasInput", StringComparison.Ordinal) >= 0 && (reg.GetPos(point.Key) - at).sqrMagnitude < 0.01f) return true;
        }
        return false;
    }
    /// <summary>Fuel stores this manifold could draw from: on the same ship, within one tile or on a propellant line.</summary>
    internal static IEnumerable<CondOwner> Candidates(CondOwner co) => (co.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && c.ship == co.ship && FuelStores.IsFamily(c.strCODef) && c.HasCond("IsInstalled") && Connection(co, c) != null)
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    /// <summary>How a store reaches the manifold: "adjacent", "line", or null when it does not.</summary>
    private static string? Connection(CondOwner manifold, CondOwner store)
    {
        if (ProcessorService.Adjacent(manifold, store)) return "adjacent";
        var path = NativeFluidRoute.Find(store, ManifoldRules.StoreOutlet, manifold, ManifoldRules.Inlet, c => PropellantLineRules.IsSegment(c.strCODef));
        return path != null && path.Length <= ManifoldRules.RouteTileLimit ? "line" : null;
    }
    /// <summary>Refreshes the cached list of switched-on stores that can feed right now, with a reason for each that cannot.</summary>
    private static void Refresh(CondOwner co, Session s)
    {
        if (StarSystem.fEpoch < s.NextCheck) return;
        s.NextCheck = StarSystem.fEpoch + ManifoldRules.RecheckSeconds;
        s.Ready.Clear(); s.Why.Clear();
        foreach (var source in s.State.Sources)
        {
            string why;
            var store = CrewWork.Resolve(source.Id);
            if (store == null || store.ship != co.ship || !FuelStores.IsFamily(store.strCODef)) why = Text.Get("Manifold.source_missing");
            else if (!NativeFluidRoute.EndpointReady(store)) why = Text.Get("Manifold.source_not_ready");
            else if (BulkVessel.Protected(store) || CommodityReservations.Held(store.strID)) why = Text.Get("Manifold.source_protected");
            else if (Connection(co, store) == null) why = Text.Get("Manifold.source_unconnected");
            else if (!source.Enabled) why = Text.Get("Manifold.source_off");
            else { s.Ready.Add(store); why = ""; }
            s.Why[source.Id] = why;
        }
    }
    private static bool Feeding(CondOwner co, out Session s)
    {
        s = Get(co);
        if (!Content.Ready || s.Protected || !s.State.On || co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return false;
        Refresh(co, s);
        return s.Ready.Count > 0;
    }

    // --- The RCS feed (Framework calls these from the engine's own RCS loops) -----------------------------------
    public bool IsFeed(CondOwner co) => co.strCODef == ManifoldRules.Installed;
    public bool DrawFirst(CondOwner feed) => Get(feed).State.First;
    public double Offer(CondOwner feed, double equivalentKg)
    {
        if (equivalentKg <= 0 || !Feeding(feed, out var s)) return 0;
        double remaining = equivalentKg;
        foreach (var store in s.Ready)
        {
            if (remaining <= 1e-12) break;
            string commodity = BulkVessels.Of(store)?.Commodity ?? "";
            double want = ManifoldRules.KilogramsFor(commodity, remaining);
            double taken = BufferedDrains.Take(store, want, Text.Get("Manifold.draw_reason"));
            if (taken <= 0) continue;
            remaining -= ManifoldRules.EquivalentKg(commodity, taken);
            s.LastSource = store.strID;
        }
        return Math.Max(0, equivalentKg - Math.Max(0, remaining));
    }
    public double ReserveEquivalentKg(CondOwner feed) =>
        !Feeding(feed, out var s) ? 0 : s.Ready.Sum(store => ManifoldRules.EquivalentKg(BulkVessels.Of(store)?.Commodity, BufferedDrains.AvailableKg(store)));
    public double CapacityEquivalentKg(CondOwner feed) =>
        !Feeding(feed, out var s) ? 0 : s.Ready.Sum(store => { var spec = BulkVessels.Of(store); return spec == null ? 0 : ManifoldRules.EquivalentKg(spec.Commodity, spec.CapacityKg); });

    // --- Presentation and commands --------------------------------------------------------------------------------
    internal static EquipmentState State(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected || co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        if (!s.State.On) return EquipmentState.Paused;
        return Feeding(co, out _) && OnRegulatorInput(co) ? EquipmentState.Running : EquipmentState.Waiting;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected) return Text.Get("Manifold.protected");
        Refresh(co, s);
        var lines = new List<string>
        {
            Text.Get(s.State.On ? "Manifold.on" : "Manifold.off") + " " + Text.Get(s.State.First ? "Manifold.order_first" : "Manifold.order_last"),
            OnRegulatorInput(co) ? Text.Get("Manifold.placed") : Text.Get("Manifold.not_placed")
        };
        foreach (var source in s.State.Sources)
        {
            var store = CrewWork.Resolve(source.Id);
            var spec = store == null ? null : BulkVessels.Of(store);
            double kg = store != null && spec != null && !BulkVessel.Protected(store) ? BulkVessel.Snapshot(store).AvailableKg : 0;
            string why = s.Why.TryGetValue(source.Id, out var w) ? w : "";
            lines.Add(Text.Get("Manifold.source_line", ObjectPresentation.Name(source.Id), kg, ManifoldRules.EquivalentKg(spec?.Commodity, kg),
                why.Length == 0 ? Text.Get("Manifold.source_feeding") : why));
        }
        if (s.State.Sources.Count == 0) lines.Add(Text.Get("Manifold.no_sources"));
        lines.Add(Text.Get("Manifold.reserve", Instance.ReserveEquivalentKg(co), Instance.CapacityEquivalentKg(co)));
        if (s.LastSource.Length > 0) lines.Add(Text.Get("Manifold.last_source", ObjectPresentation.Name(s.LastSource)));
        return string.Join("\n", lines);
    }
    internal static IReadOnlyList<ManifoldSource> Sources(CondOwner co) => Get(co).State.Sources;
    internal static bool On(CondOwner co) => Get(co).State.On;
    internal static bool First(CondOwner co) => Get(co).State.First;
    internal static string? MaintenanceReason(CondOwner co) => ManifoldRules.IsFamily(co.strCODef) && Get(co).Protected ? Text.Get("Maintenance.protected") : null;
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Content.Ready ? Content.Access(co, binding) ?? "" : Content.Status;
        if (message.Length > 0) return false;
        var s = Get(co);
        if (action == "status") { message = Describe(co); return true; }
        if (action == "accept")
        {
            var status = Store(co).Read(out var fields);
            ManifoldState? state = null;
            try { state = status == SavedStateStatus.Ready ? ManifoldState.Read(fields) : status == SavedStateStatus.Missing ? new ManifoldState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
            if (state == null) { message = Text.Get("Manifold.accept_unavailable"); return false; }
            s.State = state; s.Protected = false;
            bool ok = Save(co, s); message = Text.Get(ok ? "Manifold.accept_done" : "Manifold.accept_unavailable"); return ok;
        }
        if (s.Protected) { message = Text.Get("Manifold.protected"); return false; }
        bool changed;
        string[] parts = action.Split(new[] { ':' }, 2);
        string verb = parts[0], arg = parts.Length > 1 ? parts[1] : "";
        switch (verb)
        {
            case "on": changed = !s.State.On; s.State.On = true; message = Text.Get("Manifold.switched_on"); break;
            case "off": case "pause": s.State.On = false; changed = true; message = Text.Get("Manifold.switched_off"); break;
            case "feed": s.State.On = arg == "on"; changed = true; message = Text.Get(s.State.On ? "Manifold.switched_on" : "Manifold.switched_off"); break;
            case "order": s.State.First = arg == "first"; changed = true; message = Text.Get(s.State.First ? "Manifold.order_first" : "Manifold.order_last"); break;
            case "link":
                if (!Candidates(co).Any(c => c.strID == arg) || !s.State.Link(arg)) { message = Text.Get(s.State.Sources.Count >= ManifoldRules.MaxSources ? "Manifold.full" : "Manifold.link_missing"); return false; }
                changed = true; message = Text.Get("Manifold.linked"); break;
            case "unlink": changed = s.State.Unlink(arg); message = Text.Get(changed ? "Manifold.unlinked" : "Manifold.link_missing"); break;
            case "source-on": case "source-off":
                changed = s.State.Switch(arg, verb == "source-on"); message = Text.Get(!changed ? "Manifold.link_missing" : verb == "source-on" ? "Manifold.source_switched_on" : "Manifold.source_switched_off"); break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        if (!changed) return verb == "on";
        // Draws already taken still settle to their stores; only future draws follow the new switches.
        if (!Save(co, s)) { message = Text.Get("Manifold.protected"); return false; }
        return true;
    }
}
