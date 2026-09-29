using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The V4: one bound charge at a time from its feed bin (the F6's charge model), progress in powered
/// seconds on the machine's own record, measured electricity with a declared share into the room's air, the
/// carbon charge's off-gas breathed into that air as it progresses, solids to the tray and water to the linked
/// vessel. A reload drops the session; Start resumes the retained charge. A melt left waiting for a cool room
/// longer than its own duration freezes and is lost to slag.</summary>
internal static class RefineryService
{
    private sealed class Session
    {
        internal RefineryState State = new();
        internal bool Protected, AwaitingFeed, HeatWait, VesselWait, NeedsAttention;
        internal double Last, NextVesselCheck, WaitSince;
        internal string Status = Text.Get("Refinery.paused");
        internal string? LastStop;
    }
    internal sealed class Transfer
    {
        internal RoomHeat.Air Air = null!;
        internal double RequestedKWh, WorkSeconds, HeatFraction;
        internal EnergyReceipt Receipt = null!;
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, RefineryRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = RefineryState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        s.State.Running = false;
        if (s.Protected) s.Status = Text.Get("Refinery.protected");
        else if (s.State.Bound) s.Status = Text.Get("Refinery.retained", s.State.RecipeId);
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s) { if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Refinery.protected"); } }

    internal static CondOwner? Feed(CondOwner co) => co.compSlots?.GetCOs(RefineryRules.InputSlot, true, null)?.FirstOrDefault(c => c != null && c.strCODef == RefineryRules.InputBin);
    internal static bool ValidFeed(CondOwner? input) => input != null && !input.bDestroyed && input.Crew == null &&
        RefineryRules.ValidFeed(input.strCODef, input.GetTotalMass(), !input.HasCond("IsInstalled"),
            input.GetCOsSafe(true).Count == 0 && input.GetLotCOs(true).Count == 0, input.coStackHead == null && input.aStack.Count == 0, ShipbreakerStock.Available);
    internal static bool CrewFeed(CondOwner input) => CrewLogistics.Loose(input) && RefineryRules.FeedKg(input.strCODef, ShipbreakerStock.Available) is double kg && ProcessMaterial.MassMatches(input.GetCondAmount("StatMass"), kg);
    internal static bool CanFeed(CondOwner bin, CondOwner input) => !bin.HasCond("IsLocked") &&
        (CrewLogistics.IsUnitPreflight(input) ? CrewFeed(input) : ValidFeed(input)) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || bin.objContainer.ContainedCOs.Count < RefineryRules.FeedCapacity);

    internal static MaterialPort Outlet(CondOwner co) => new(co.strID, RefineryRules.OutPort, co.mapGUIPropMaps);
    internal static MaterialPort Inlet(CondOwner vessel) => new(vessel.strID, RefineryRules.VesselPort, vessel.mapGUIPropMaps);
    internal static string Peer(CondOwner co) => PortPairing.Read(Outlet(co)).PeerObjectId;
    private static int Footprint(CondOwner co) => Math.Max(1, DataHandler.GetCondOwnerDef(co.strCODef)?.inventoryWidth ?? 1);
    internal static bool Adjacent(CondOwner a, CondOwner b) { var p = a.GetPos(); var q = b.GetPos(); return ManufacturingRules.Adjacent(p.x, p.y, Footprint(a), q.x, q.y, Footprint(b)); }
    internal static IEnumerable<CondOwner> Candidates(CondOwner co) => BulkVessels.Aboard(co.ship, ManufacturingRules.Water).Where(v => v != co && Adjacent(co, v));
    /// <summary>The linked water vessel when it can take this much water now; otherwise null with the reason.</summary>
    internal static CondOwner? Vessel(CondOwner co, double kg, out string reason)
    {
        reason = Text.Get("Refinery.no_vessel");
        var vessel = CrewWork.Resolve(Peer(co));
        if (vessel == null || !BulkVessels.IsVessel(vessel)) return null;
        reason = Text.Get("Refinery.vessel_not_ready");
        if (vessel.ship != co.ship || !NativeFluidRoute.EndpointReady(vessel) || !PortPairing.Matches(Outlet(co), Inlet(vessel)) || !Adjacent(co, vessel)) return null;
        reason = Text.Get("Refinery.vessel_protected");
        if (BulkVessel.Protected(vessel) || CommodityReservations.Held(vessel.strID)) return null;
        var s = BulkVessel.Snapshot(vessel);
        reason = Text.Get("Refinery.vessel_catch");
        if (s.CatchKg > 1e-8) return null;
        reason = Text.Get("Refinery.vessel_full", s.HeadroomKg, kg);
        if (s.HeadroomKg + 1e-8 < kg) return null;
        reason = ""; return vessel;
    }
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != RefineryRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Refinery.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Refinery.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked") || Feed(co)?.HasCond("IsLocked") == true || co.objContainer?.Locked == true || Feed(co)?.objContainer?.Locked == true) return Text.Get("Refinery.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Refinery.switched_off");
        if (co.objContainer == null || Feed(co)?.objContainer == null) return Text.Get("Refinery.missing_feed");
        return null;
    }
    private static void SetWorking(CondOwner co, bool value) => co.SetCondAmount(ManufacturingRules.Working, value ? 1 : 0);
    private static ChargeRecipe? Recipe(Session s) => s.State.Bound ? RefineryRecipes.ByRevision(s.State.Revision) : null;
    /// <summary>The bound units, when every one of them still sits in the feed bin and is still valid feed.</summary>
    private static List<CondOwner>? Charge(CondOwner co, Session s)
    {
        var bin = Feed(co)?.objContainer?.ContainedCOs;
        if (bin == null || !s.State.Bound) return null;
        var items = new List<CondOwner>();
        foreach (string id in s.State.Charge)
        {
            var item = bin.FirstOrDefault(c => c.strID == id);
            if (item == null || !ValidFeed(item)) return null;
            items.Add(item);
        }
        return items;
    }

    internal static bool Start(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Refinery.protected"); return false; }
        try
        {
            s.LastStop = null; s.NeedsAttention = false; s.AwaitingFeed = true;
            if (s.State.Running) return true;
            bool started = Resume(co, s) || Bind(co, s);
            if (!started && !s.VesselWait) s.AwaitingFeed = false;
            return started || s.VesselWait;
        }
        catch (Exception ex) { Fault(co, ex); return false; }
    }
    /// <summary>A retained charge resumes exactly; one whose recipe this installation lacks stays retained.</summary>
    private static bool Resume(CondOwner co, Session s)
    {
        if (!s.State.Bound) return false;
        var recipe = Recipe(s);
        if (recipe == null || recipe.NeedsSteelStock && !ShipbreakerStock.Available) { s.Status = Text.Get("Refinery.retained_unavailable", s.State.RecipeId); return false; }
        if (Charge(co, s) == null) { s.Status = Text.Get("Refinery.charge_changed"); return false; }
        return Run(co, s, recipe);
    }
    /// <summary>Binds the largest exact charge present in the bin.</summary>
    private static bool Bind(CondOwner co, Session s)
    {
        SetWorking(co, false); s.VesselWait = false;
        var bin = Feed(co)?.objContainer?.ContainedCOs;
        if (bin == null || bin.Count == 0) { s.Status = Text.Get("Refinery.feed_empty"); return false; }
        var valid = bin.Where(ValidFeed).ToList();
        var recipe = RefineryRecipes.Match(valid.Select(c => c.strCODef), ShipbreakerStock.Available);
        if (recipe == null) { s.Status = Text.Get(valid.Count == 0 ? "Refinery.invalid_feed" : "Refinery.no_charge"); return false; }
        var units = new List<CondOwner>();
        foreach (var input in recipe.Inputs) units.AddRange(valid.Where(c => c.strCODef == input.Id).OrderBy(c => c.strID, StringComparer.Ordinal).Take(input.Count));
        if (units.Any(u => !RefineryState.SafeId(u.strID))) { s.Status = Text.Get("Refinery.invalid_feed"); return false; }
        s.State.RecipeId = recipe.Id; s.State.Revision = recipe.Revision; s.State.ProgressSeconds = 0; s.State.WaitSeconds = 0; s.State.EmittedKg = 0;
        s.State.Charge = units.Select(u => u.strID).ToList();
        Save(co, s);
        if (s.Protected) return false;
        return Run(co, s, recipe);
    }
    private static bool Run(CondOwner co, Session s, ChargeRecipe recipe)
    {
        double water = recipe.Products.Where(p => p.Id == ManufacturingRules.Water).Sum(p => p.Kg * p.Count);
        if (water > 0 && Vessel(co, water, out string why) == null) { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + ManufacturingRules.VesselRecheckSeconds; s.Status = Text.Get("Refinery.waiting_vessel", why); return false; }
        s.State.Running = true; s.Last = StarSystem.fEpoch; s.VesselWait = false;
        s.Status = Text.Get("Refinery.working", Text.Get("Recipe." + recipe.Id));
        if (s.State.ProgressSeconds >= recipe.Seconds) return Finish(co, s);
        SetWorking(co, true);
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        s.NeedsAttention = false;
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Refinery.cancelled" : "Refinery.paused_retained"), needsAttention: false);
        if (!cancel || s.Protected) return !s.Protected;
        // Cancelling releases the bound units unchanged; the charge's work is forfeited, its mass is not.
        s.State.Clear(); Save(co, s);
        return true;
    }
    private static void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        s.LastStop = message; s.NeedsAttention = needsAttention; s.AwaitingFeed = false; s.VesselWait = false; s.HeatWait = false;
        s.State.Running = false; s.Status = message; SetWorking(co, false);
    }
    internal static void Block(CondOwner co, string reason) => Stop(co, Get(co), reason);
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co);
        Stop(co, s, Text.Get("Refinery.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }

    internal static void BeforePower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s)) { SetWorking(co, false); return; }
        if (s.VesselWait && StarSystem.fEpoch >= s.NextVesselCheck)
        {
            s.NextVesselCheck = StarSystem.fEpoch + ManufacturingRules.VesselRecheckSeconds;
            var fault = MachineProblem(co);
            if (fault != null) { Stop(co, s, fault); return; }
            var recipe = Recipe(s);
            if (recipe != null && s.State.ProgressSeconds >= recipe.Seconds) Finish(co, s);
            else if (s.AwaitingFeed) { if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) s.AwaitingFeed = false; }
        }
        else if (s.AwaitingFeed && !s.State.Running && !s.VesselWait)
        {
            var problem = MachineProblem(co);
            if (problem != null) { Stop(co, s, problem); return; }
            if (Feed(co)?.objContainer?.ContainedCOs.Count == 0 && !s.State.Bound) { s.Status = Text.Get("Refinery.feed_empty"); return; }
            if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) s.AwaitingFeed = false;
        }
        if (!s.State.Running) { if (!s.VesselWait) SetWorking(co, false); return; }
        string? machineProblem = MachineProblem(co);
        if (machineProblem != null || Charge(co, s) == null) { Stop(co, s, machineProblem ?? Text.Get("Refinery.charge_changed")); return; }
        var current = Recipe(s);
        SetWorking(co, current != null && s.State.ProgressSeconds < current.Seconds);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Working);
        double demand = working ? RefineryRules.WorkingKW : RefineryRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand, heatKW = RefineryRules.RoomHeatKW(working);
        var air = RoomHeat.Read(co);
        if (!RoomHeat.Admit(air, heatKW, seconds, out _))
        {
            // Not a stop: no power is drawn this step and the charge keeps its permission. A melt that waits
            // too long freezes (see AfterPower); a drying charge simply resumes when the room can take the heat.
            if (sessions.TryGetValue(co, out var s) && s.State.Running)
            {
                if (!s.HeatWait) { s.HeatWait = true; s.WaitSince = StarSystem.fEpoch; }
                s.Status = Text.Get("Refinery.heat_wait");
            }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) SettleWait(co, ready);
        transfer = new Transfer { Air = air!, RequestedKWh = amount, WorkSeconds = seconds, HeatFraction = heatKW / demand, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    /// <summary>Heat-wait time accrues to the bound charge; a melt over its own duration is frozen.</summary>
    private static void SettleWait(CondOwner co, Session s)
    {
        if (!s.HeatWait) return;
        s.HeatWait = false;
        double waited = StarSystem.fEpoch - s.WaitSince;
        if (waited > 0 && s.State.Bound) { s.State.WaitSeconds += waited; Save(co, s); }
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid refinery energy receipt.");
        RoomHeat.Deposit(transfer.Air, supplied, transfer.HeatFraction);
        if (!sessions.TryGetValue(co, out var s) || !s.State.Running || !co.HasCond(ManufacturingRules.Working)) return;
        var recipe = Recipe(s);
        if (recipe == null) return;
        // Progress is powered seconds: a partial supply credits a partial step.
        double poweredSeconds = transfer.RequestedKWh > 0 ? transfer.WorkSeconds * Math.Min(1, supplied / transfer.RequestedKWh) : 0;
        double now = StarSystem.fEpoch, elapsed = Math.Max(0, now - s.Last); s.Last = now;
        s.State.ProgressSeconds = Math.Min(recipe.Seconds, s.State.ProgressSeconds + Math.Min(elapsed, poweredSeconds));
        double due = recipe.OffGasDueKg(s.State.ProgressSeconds / recipe.Seconds, s.State.EmittedKg);
        if (due > 0)
        {
            foreach (var pair in recipe.Split(due)) if (pair.Value > 0) RoomGas.Emit(transfer.Air, pair.Key, pair.Value);
            s.State.EmittedKg += due;
        }
        Save(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    internal static void AfterPower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s) || !s.State.Running) return;
        try
        {
            var recipe = Recipe(s);
            if (recipe == null) { Stop(co, s, Text.Get("Refinery.charge_changed")); return; }
            if (s.HeatWait) { double waited = StarSystem.fEpoch - s.WaitSince; if (RefineryRecipes.Spoiled(recipe, s.State.WaitSeconds + Math.Max(0, waited))) { SettleWait(co, s); Finish(co, s); } return; }
            s.Status = co.HasCond("IsPowered") ? Text.Get("Refinery.working", Text.Get("Recipe." + recipe.Id)) : Text.Get("Refinery.waiting_power");
            if (s.State.ProgressSeconds >= recipe.Seconds && co.HasCond("IsPowered")) Finish(co, s);
        }
        catch (Exception ex) { Fault(co, ex); }
    }

    /// <summary>The charge becomes its products: solids planned and placed in the tray first, the bound units
    /// removed second, water into the vessel under its conversion journal. A frozen melt yields slag.</summary>
    private static bool Finish(CondOwner co, Session s)
    {
        SetWorking(co, false);
        var recipe = Recipe(s);
        var units = Charge(co, s);
        if (recipe == null || units == null || MachineProblem(co) != null) { Stop(co, s, Text.Get("Refinery.charge_changed")); return false; }
        bool spoiled = RefineryRecipes.Spoiled(recipe, s.State.WaitSeconds);
        var products = spoiled ? RefineryRecipes.SpoiledProducts(recipe) : recipe.Products;
        double water = products.Where(p => p.Id == ManufacturingRules.Water).Sum(p => p.Kg * p.Count);
        CondOwner? vessel = null;
        if (water > 0 && (vessel = Vessel(co, water, out string why)) == null)
        { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + ManufacturingRules.VesselRecheckSeconds; s.Status = Text.Get("Refinery.waiting_vessel", why); return false; }
        var delivery = new ChargeDelivery(co, units, products.Where(p => p.Id != ManufacturingRules.Water).ToList(), recipe.ChargeKg - recipe.OffGasKg - water);
        StoredCommodity? state = null;
        if (vessel != null) { state = BulkVessel.Read(vessel); BulkVessel.BeginConversion(vessel, units[0].strID, state.TotalKg); }
        var result = BatchDelivery.Commit(delivery);
        if (result != DeliveryResult.Completed)
        {
            if (vessel != null) BulkVessel.EndConversion(vessel);
            s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + ManufacturingRules.VesselRecheckSeconds; s.Status = Text.Get("Refinery.tray_full"); return false;
        }
        if (vessel != null && state != null) { state.SetService(state.ServiceKg + water); BulkVessel.Save(vessel, state); BulkVessel.EndConversion(vessel); }
        Plugin.Log(Text.Get(spoiled ? "Refinery.spoiled_log" : "Refinery.completed_log", Text.Get("Recipe." + recipe.Id), co.strID));
        if (spoiled) Phobos.Ostranauts.Framework.Notices.PlayerNotices.Post(co.ship, "PhobosManufacturing.spoiled", Phobos.Ostranauts.Framework.Notices.NoticeLevel.Caution, Text.Get("Refinery.spoiled_notice", co.strNameFriendly));
        s.State.Cycles++; s.State.Clear(); Save(co, s);
        if (s.Protected) return true;
        if (!(Resume(co, s) || Bind(co, s)) && !s.VesselWait) { s.AwaitingFeed = Feed(co)?.objContainer?.ContainedCOs.Count > 0 && s.AwaitingFeed; if (!s.AwaitingFeed) s.Status = Text.Get("Refinery.complete"); }
        return true;
    }
    private sealed class ChargeDelivery : IBatchDelivery
    {
        private readonly CondOwner machine; private readonly List<CondOwner> units; private readonly IReadOnlyList<ProductSpec> specs; private readonly double solidsKg;
        private readonly List<CondOwner> products = new(); private Position[]? plan; private bool retired;
        internal ChargeDelivery(CondOwner machine, List<CondOwner> units, IReadOnlyList<ProductSpec> specs, double solidsKg) { this.machine = machine; this.units = units; this.specs = specs; this.solidsKg = solidsKg; }
        public bool InputConsumed => retired || units.All(u => u.bDestroyed);
        public bool Prepare()
        {
            if (machine.objContainer == null || units.Any(u => u.objCOParent != Feed(machine) || !ValidFeed(u))) return false;
            foreach (var spec in specs)
            for (int n = 0; n < spec.Count; n++)
            {
                var product = DataHandler.GetCondOwner(spec.Id) ?? throw new InvalidOperationException(Text.Get("Refinery.missing_output", spec.Id));
                products.Add(product);
                if (product.coStackHead != null || product.aStack.Count != 0 || product.GetCOsSafe(true).Count != 0 || !ProcessMaterial.MassMatches(product.GetTotalMass(), spec.Kg))
                    throw new InvalidOperationException(Text.Get("Refinery.output_definition_changed", spec.Id));
                if (!machine.objContainer.AllowedCO(product)) return false;
            }
            if (!ProcessMaterial.Balanced(solidsKg, products.Select(p => p.GetTotalMass()))) throw new InvalidOperationException(Text.Get("Refinery.unbalanced"));
            var sizes = products.Select(p => GUIInventoryItem.GetWidthHeightForCO(p)).Select(v => new ItemSize(v.x, v.y)).ToArray();
            plan = BatchPlacement.Plan(Occupancy(machine.objContainer), sizes);
            return plan != null;
        }
        public void PlaceProducts()
        {
            if (plan == null) throw new InvalidOperationException(Text.Get("Refinery.unprepared"));
            for (int i = 0; i < products.Count; i++)
            {
                machine.objContainer.AddCOSimple(products[i], new PairXY(plan[i].X, plan[i].Y));
                if (products[i].objCOParent != machine || !machine.objContainer.ContainedCOs.Contains(products[i])) throw new InvalidOperationException(Text.Get("Refinery.output_placement_failed"));
            }
        }
        public void ConsumeInput()
        {
            if (units.Any(u => u.objCOParent != Feed(machine) || !ValidFeed(u))) throw new InvalidOperationException(Text.Get("Refinery.charge_changed"));
            foreach (var unit in units)
            {
                bool gone;
                try { unit.RemoveFromCurrentHome(bForce: true); }
                finally { gone = unit.objCOParent == null && unit.ship == null; }
                if (!gone) throw new InvalidOperationException(Text.Get("Refinery.input_not_removed"));
                unit.Destroy();
            }
            retired = true;
            machine.objContainer.Redraw(); Feed(machine)?.objContainer?.Redraw();
        }
        public void RollbackProducts()
        {
            foreach (var product in products)
            {
                if (product == null || product.bDestroyed) continue;
                if (product.objCOParent != null || product.ship != null) product.RemoveFromCurrentHome(bForce: true);
                product.Destroy();
            }
            products.Clear();
            machine.objContainer.Redraw();
        }
        private static bool[,] Occupancy(Container tray)
        {
            var grid = tray.gridLayout;
            var result = new bool[grid.gridMaxX, grid.gridMaxY];
            for (int x = 0; x < grid.gridMaxX; x++)
            for (int y = 0; y < grid.gridMaxY; y++)
                result[x, y] = grid.gridID[x, y] != null || grid.gridInventoryItem[x, y] != null;
            return result;
        }
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        sessions.TryGetValue(co, out var s);
        if (s?.NeedsAttention == true || s?.Protected == true) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Working)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s != null && (s.AwaitingFeed || s.VesselWait) ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        var recipe = Recipe(s);
        int count = Feed(co)?.objContainer?.ContainedCOs.Count ?? 0;
        string charge = recipe == null ? Text.Get("Refinery.no_charge_bound") : Text.Get("Refinery.charge", Text.Get("Recipe." + recipe.Id), s.State.ProgressSeconds, recipe.Seconds, s.State.WaitSeconds);
        return Text.Get("Refinery.status", s.Status, count, RefineryRules.FeedCapacity, charge,
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"), ObjectPresentation.Name(Peer(co)), s.State.Cycles) +
            "\n" + Text.Get("Refinery.demand", RefineryRules.WorkingKW, RefineryRules.WorkingKW * RefineryRules.RoomHeatFraction) +
            (ShipbreakerStock.Available ? "" : "\n" + Text.Get("Refinery.no_steel")) + (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    internal static bool Link(CondOwner co, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        if (co.HasCond(ManufacturingRules.Working) || sessions.TryGetValue(co, out var s) && s.State.Running) { reason = Text.Get("Refinery.link_busy"); return false; }
        var current = CrewWork.Resolve(Peer(co));
        if (id == "none") { PortPairing.Unlink(Outlet(co), current == null ? null : Inlet(current)); reason = Text.Get("Refinery.unlinked"); return true; }
        var vessel = Candidates(co).FirstOrDefault(v => v.strID == id);
        if (vessel == null) { reason = Text.Get("Refinery.link_missing"); return false; }
        if (current != null && current != vessel) PortPairing.Unlink(Outlet(co), Inlet(current));
        if (PortPairing.Matches(Outlet(co), Inlet(vessel))) { reason = Text.Get("Refinery.linked"); return true; }
        if (!PortPairing.TryLink(Outlet(co), Inlet(vessel), out reason)) return false;
        reason = Text.Get("Refinery.linked"); return true;
    }
    internal static string? MaintenanceReason(CondOwner co)
    {
        if (!RefineryRules.IsFamily(co.strCODef)) return null;
        var s = Get(co);
        if (s.Protected) return Text.Get("Maintenance.protected");
        return s.State.Bound ? Text.Get("Maintenance.charge") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Refinery.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        if (action.StartsWith("link:", StringComparison.Ordinal)) return Link(co, action.Substring(5), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
