using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Propulsion;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The RM-1 reaction mass feeder (Manufacturing 0.43.0). While it has power it grinds the declared remainders in
/// its inventory, one unit at a time, crediting measured electricity to the unit in hand; a finished unit is destroyed
/// and its exact mass enters the feeder's bulk-vessel record under that record's conversion journal. Framework's RCS
/// patch finds the feeder on a regulator's gas-input tile, as it finds the P1 manifold, and draws on the record through
/// the buffered draws. Its own inventory is its feed; an optional feed store (Framework <see cref="StoreFeed"/>) tops
/// it up. Nothing is rolled, created or vented: mass in is mass thrown.</summary>
internal sealed class FeederService : IRcsPropellantFeed
{
    internal static readonly FeederService Instance = new();
    public string Id => Plugin.Id + ".feeder";
    private sealed class Session
    {
        internal FeederState State = new();
        internal bool Protected, HeatWait;
        internal CondOwner? Item;
        internal string Status = Text.Get("Feeder.idle");
        // How often an idle feeder looks in its feed store (real time).
        internal readonly Cadence StoreLook = new(ManufacturingRules.VesselRecheckSeconds);
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal EnergyReceipt Receipt = null!; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, FeederRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = FeederState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Feeder.protected");
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s)
    {
        if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Feeder.protected"); }
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected || BulkVessel.Protected(co);
    /// <summary>Owner-confirmed recovery of both records: the mass record through the vessel's own acceptance, and the
    /// settings to a readable or fresh state.</summary>
    internal static bool Accept(CondOwner co)
    {
        var s = Get(co);
        var status = Store(co).Read(out var fields);
        FeederState state = new();
        try { if (status == SavedStateStatus.Ready) state = FeederState.Read(fields); } catch (Exception e) { Plugin.Log(e.ToString()); }
        s.State = state; s.Protected = false; Save(co, s);
        bool mass = !BulkVessel.Protected(co) || BulkVessel.Accept(co, Plugin.Log);
        if (!s.Protected && mass) s.Status = Text.Get("Feeder.idle");
        return !s.Protected && mass;
    }

    /// <summary>What the feeder's inventory admits: a loose, empty unit of a declared remainder, and nothing else.</summary>
    internal static bool CanFeed(CondOwner input) => input != null && !input.bDestroyed && Remainders.IsDeclared(input.strCODef) && CrewLogistics.Loose(input);
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != FeederRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Feeder.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Feeder.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked") || co.objContainer?.Locked == true) return Text.Get("Feeder.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Feeder.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Grinding) != value) co.SetCondAmount(ManufacturingRules.Grinding, value ? 1 : 0); }
    // The record's own figure: draws not yet settled make it a little high, which only errs toward waiting.
    private static double HeldKg(CondOwner co) => BulkVessel.Snapshot(co).ServiceKg;

    /// <summary>The unit to grind: the one in hand if it is still inside, else the first remainder in the inventory, else
    /// one fetched from the chosen feed store every couple of real seconds.</summary>
    private static CondOwner? NextItem(CondOwner co, Session s)
    {
        if (s.Item != null && !s.Item.bDestroyed && StackUnits.Inside(s.Item, co) && CanFeed(s.Item)) return s.Item;
        s.Item = OwnInventoryFeed.Units(co, CanFeed).FirstOrDefault();
        if (s.Item != null || !s.StoreLook.Due()) return s.Item;
        var fetched = StoreFeed.Units(co, CanFeed).FirstOrDefault();
        if (fetched != null && OwnInventoryFeed.Take(fetched, co)) s.Item = fetched;
        return s.Item;
    }

    /// <summary>Before the native power step: the feeder works while it has a remainder to grind and room for its mass.</summary>
    internal static void BeforePower(CondOwner co)
    {
        var s = Get(co);
        var problem = MachineProblem(co);
        if (problem != null) { s.Status = problem; SetWorking(co, false); return; }
        if (s.Protected || BulkVessel.Protected(co)) { s.Status = Text.Get("Feeder.protected"); SetWorking(co, false); return; }
        try
        {
            var item = NextItem(co, s);
            if (item == null) { if (!s.HeatWait) s.Status = Text.Get("Feeder.idle"); SetWorking(co, false); return; }
            if (!FeederRules.Fits(HeldKg(co), UnitItemTransfer.UnitKg(item))) { s.Status = Text.Get("Feeder.full", FeederRules.CapacityKg); SetWorking(co, false); return; }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Grinding);
        double demand = working ? FeederRules.WorkingKW : FeederRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand;
        // Crew upkeep (0.55.0): a tuned machine asks for more power while it works and does that much more work.
        double tune = working ? Phobos.Ostranauts.Framework.Crew.Upkeep.Draw(co, ref amount, seconds) : 1;
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, (demand * FeederRules.RoomHeatFraction) * tune, seconds);
        var s = Get(co);
        if (!heat.Admitted)
        {
            if (working) { s.HeatWait = true; s.Status = RoomHeat.Describe(heat); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        s.HeatWait = false;
        transfer = new Transfer { Air = air!, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid grinding energy receipt.");
        RoomHeat.Deposit(transfer.Air, supplied, FeederRules.RoomHeatFraction);
        var s = Get(co);
        if (!co.HasCond(ManufacturingRules.Grinding) || s.Protected || s.Item == null || s.Item.bDestroyed || !StackUnits.Inside(s.Item, co)) return;
        double kg = UnitItemTransfer.UnitKg(s.Item);
        var (credited, complete) = FeederRules.Advance(Math.Min(s.State.GrindKWh, FeederRules.GrindKWh(kg)), supplied, kg);
        s.State.GrindKWh = credited;
        s.Status = co.HasCond("IsPowered") ? Text.Get("Feeder.grinding", ObjectPresentation.Name(s.Item), credited, FeederRules.GrindKWh(kg)) : Text.Get("Feeder.waiting_power");
        if (complete) Grind(co, s, s.Item, kg);
        else Save(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    /// <summary>A ground unit: under the mass record's conversion journal, the unit leaves the inventory and is destroyed
    /// and its mass joins the record. An interruption leaves the journal open and the feeder protected, never duplicated
    /// or vanished mass.</summary>
    private static void Grind(CondOwner co, Session s, CondOwner item, double kg)
    {
        BufferedDrains.Settle(co);
        var spec = BulkVessel.Spec(co);
        var mass = BulkVessel.Read(co, spec);
        if (!FeederRules.Fits(mass.TotalKg, kg)) { Save(co, s); s.Status = Text.Get("Feeder.full", FeederRules.CapacityKg); SetWorking(co, false); return; }
        BulkVessel.BeginConversion(co, item.strID, mass.TotalKg);
        string name = ObjectPresentation.Name(item);
        StackUnits.Detach(item);
        if (item.objCOParent != null || item.ship != null) item.RemoveFromCurrentHome(bForce: true);
        item.Destroy();
        mass.SetService(mass.ServiceKg + kg);
        BulkVessel.Save(co, spec, mass);
        BulkVessel.EndConversion(co);
        BufferedDrains.Settle(co);
        s.Item = null; s.State.GrindKWh = 0; Save(co, s);
        co.objContainer?.Redraw();
        s.Status = Text.Get("Feeder.ground", name, kg);
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co);
        s.Status = Text.Get("Feeder.fault"); SetWorking(co, false);
        Plugin.Log(ex.ToString());
    }

    // --- The RCS feed (Framework calls these from the engine's own RCS loops) -----------------------------------
    private static bool Feeding(CondOwner co)
    {
        if (!Content.Ready || co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return false;
        var s = Get(co);
        return !s.Protected && s.State.Feeding;
    }
    // The draw reason is one fixed line of text; formatted once per language, not per physics step.
    private static string drawReason = "", drawReasonLanguage = "";
    private static string DrawReason()
    {
        string language = Phobos.Ostranauts.Framework.Localization.Translations.Language;
        if (drawReasonLanguage != language) { drawReason = Text.Get("Feeder.draw_reason"); drawReasonLanguage = language; }
        return drawReason;
    }
    public bool IsFeed(CondOwner co) => co.strCODef == FeederRules.Installed;
    public bool DrawFirst(CondOwner feed) => Get(feed).State.First;
    public double Offer(CondOwner feed, double equivalentKg)
    {
        if (equivalentKg <= 0 || !Feeding(feed)) return 0;
        double want = FeederRules.KilogramsFor(equivalentKg, BufferedDrains.AvailableKg(feed));
        return want <= 0 ? 0 : FeederRules.EquivalentKg(BufferedDrains.Take(feed, want, DrawReason()));
    }
    // The game asks for the RCS reserve every frame for every ship: no closures, no allocation.
    public double ReserveEquivalentKg(CondOwner feed) => Feeding(feed) ? FeederRules.EquivalentKg(BufferedDrains.AvailableKg(feed)) : 0;
    public double CapacityEquivalentKg(CondOwner feed) => Feeding(feed) ? FeederRules.EquivalentKg(FeederRules.CapacityKg) : 0;

    // --- Presentation and commands --------------------------------------------------------------------------------
    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked") || Protected(co)) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Grinding)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return Get(co).State.Feeding ? EquipmentState.Ready : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        if (Protected(co)) return Text.Get("Feeder.protected");
        double held = BufferedDrains.AvailableKg(co);
        var lines = new List<string>
        {
            s.Status,
            Text.Get("Feeder.status", held, FeederRules.CapacityKg, co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power")),
            Text.Get(s.State.Feeding ? "Feeder.feeding_on" : "Feeder.feeding_off") + " " + Text.Get(s.State.First ? "Feeder.order_first" : "Feeder.order_last"),
            ManifoldService.OnRegulatorInput(co) ? Text.Get("Feeder.placed") : Text.Get("Feeder.not_placed"),
            Text.Get("Feeder.demand", FeederRules.WorkingKW, FeederRules.KWhPerKg)
        };
        string store = StoreFeed.Describe(co);
        if (store.Length > 0) lines.Add(store);
        return string.Join("\n", lines);
    }
    internal static IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        var s = Get(co);
        yield return new(Text.Get("Feeder.feeding_field"), Text.Get(s.State.Feeding ? "Provider.on" : "Provider.off"),
            new[] { ("feeding:" + FeederRules.SwitchOn, Text.Get("Provider.on")), ("feeding:" + FeederRules.SwitchOff, Text.Get("Provider.off")) },
            "feeding:" + (s.State.Feeding ? FeederRules.SwitchOn : FeederRules.SwitchOff));
        yield return new(Text.Get("Feeder.order_field"), Text.Get(s.State.First ? "Feeder.order_first" : "Feeder.order_last"),
            new[] { ("order:" + FeederRules.DrawFirst, Text.Get("Feeder.order_first")), ("order:" + FeederRules.DrawLast, Text.Get("Feeder.order_last")) },
            "order:" + (s.State.First ? FeederRules.DrawFirst : FeederRules.DrawLast));
        yield return StoreFeed.Field(co);
    }
    /// <summary>Why removal work is refused when offered: a protected feeder stays put, and one holding reaction mass is
    /// not dismantled (that would delete the mass). Uninstalling carries the mass along.</summary>
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        if (!FeederRules.IsFamily(co.strCODef)) return null;
        if (Protected(co)) return Text.Get("Maintenance.protected");
        return dismantle && BulkVessel.Snapshot(co).ServiceKg > 1e-8 ? Text.Get("Maintenance.feeder_mass") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Feeder.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        message = Content.Access(co, binding) ?? "";
        if (message.Length > 0) return false;
        var s = Get(co);
        if (action == "status") { message = Describe(co); return true; }
        if (action == "accept") { bool ok = Accept(co); message = Text.Get(ok ? "Feeder.accept_done" : "Feeder.accept_unavailable"); return ok; }
        if (Protected(co)) { message = Text.Get("Feeder.protected"); return false; }
        if (action.StartsWith(StoreFeed.ActionPrefix, StringComparison.Ordinal)) return StoreFeed.Command(co, action, out message);
        if (action == "jettison")
        {
            // An explicit discharge overboard: the mass leaves the ship without thrust, logged by the vessel.
            BufferedDrains.Settle(co);
            double gone = BulkVessel.Drain(co, BulkVessel.Snapshot(co).ServiceKg, Text.Get("Feeder.jettison_reason"));
            BufferedDrains.Settle(co);
            message = Text.Get(gone > 0 ? "Feeder.jettisoned" : "Feeder.nothing_to_jettison", gone);
            return gone > 0;
        }
        if (action.StartsWith("feeding:", StringComparison.Ordinal) || action == "on" || action == "off")
        {
            string value = action.StartsWith("feeding:", StringComparison.Ordinal) ? action.Substring(8) : action;
            if (value != FeederRules.SwitchOn && value != FeederRules.SwitchOff) { message = Text.Get("Content.unsupported_action"); return false; }
            s.State.Feeding = value == FeederRules.SwitchOn; Save(co, s);
            message = Text.Get(s.State.Feeding ? "Feeder.feeding_on" : "Feeder.feeding_off"); return !s.Protected;
        }
        if (action.StartsWith("order:", StringComparison.Ordinal))
        {
            string value = action.Substring(6);
            if (value != FeederRules.DrawFirst && value != FeederRules.DrawLast) { message = Text.Get("Content.unsupported_action"); return false; }
            s.State.First = value == FeederRules.DrawFirst; Save(co, s);
            message = Text.Get(s.State.First ? "Feeder.order_first" : "Feeder.order_last"); return !s.Protected;
        }
        message = Text.Get("Content.unsupported_action"); return false;
    }
}
