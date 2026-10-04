using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Native boundary for the F6. UI and F3 delegate here; only simulation callbacks advance time.</summary>
internal static partial class FurnaceService
{
    internal sealed class Session
    {
        internal CondOwner Object = null!;
        internal FurnaceState State = new();
        internal double SinkKJ, SinkLast, DeliveredKW, LastReceiptEpoch = double.NegativeInfinity;
        internal bool Protected;
        internal string Notice = "";
        internal string CoolingMode = "direct";
        internal CoolantCharge Coolant = new();
        internal double AccountedCoolantKg;
        internal ObjectStateStore Store = null!;
        // Repeat-run intent is saved; its permission is session-only and never restored by loading.
        internal FurnaceRepeatRecord? Repeat;
        internal bool RepeatProtected, RepeatAuthorized;
        internal string RepeatNotice = "";
        internal double RepeatRetry;
        // Records settle every couple of real seconds and before every native save, not on every advance.
        internal bool Dirty;
        internal readonly Cadence Settle = new(SettleSeconds);
    }
    internal sealed class PowerTransfer
    { internal Session Session = null!, Cooling = null!; internal EnergyReceipt Receipt = null!; internal double Seconds, MotorKJ; internal bool Routed, Finished; internal object? MaterialToken; }
    internal const double SettleSeconds = 2, DiscoverySeconds = 2;
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private static float nextScan;
    // Furnace-family objects anywhere in the loaded world, reread every couple of real seconds and on a mode switch;
    // passive physics still advances every quarter second for each of them (audit P3).
    private static readonly List<CondOwner> tracked = new();
    private static readonly Cadence discovery = new(DiscoverySeconds);
    private static readonly Dictionary<string, bool> equipmentDefinitions = new(StringComparer.Ordinal);
    internal static bool IsEquipment(CondOwner? co) => co != null && IsEquipmentDefinition(co.strCODef);
    internal static bool IsEquipmentDefinition(string? id)
    {
        if (id == null) return false;
        if (!equipmentDefinitions.TryGetValue(id, out bool yes))
        {
            yes = FurnaceRules.Machine(id) || FurnaceRules.Cooling(id);
            if (equipmentDefinitions.Count < 65536) equipmentDefinitions[id] = yes;
        }
        return yes;
    }
    static FurnaceService() { SaveBoundary.BeforeShipSave += FlushAll; }
    internal static void Reset() { sessions.Clear(); nextScan = 0; tracked.Clear(); discovery.Invalidate(); }
    /// <summary>A replacement object (damage, repair, installation) joins the tracked set at once.</summary>
    internal static void Track(CondOwner? co) { if (co != null && !co.bDestroyed && IsEquipment(co)) family.Offer(co); }
    // Stage 8: furnace-family parts come from Framework's shared world sweep instead of a pass over every world object.
    private static readonly Phobos.Ostranauts.Framework.Discovery.WorldFamily family =
        Phobos.Ostranauts.Framework.Discovery.WorldFamilies.Register(Plugin.Id + ".furnace", IsEquipmentDefinition);
    internal static CondOwner? Feed(CondOwner co) => co.compSlots?.GetCOs(FurnaceRules.Slot, true, null)?.FirstOrDefault(c => c != null && c.strCODef == FurnaceRules.Feed);
    private static MaterialPort Port(CondOwner co) => new(co.strID, "PhobosFurnace.Cooling", co.mapGUIPropMaps);
    internal static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co.strID, out var found) && found.Object == co) return found;
        var s = new Session { Object = co, Store = new ObjectStateStore(co.mapGUIPropMaps, "Furnace", Text.Owner, 1) };
        var status = s.Store.Read(out var fields);
        if (status == SavedStateStatus.Ready)
        {
            if (FurnaceRules.Cooling(co.strCODef))
            {
                s.Protected = !FurnaceCooling.TryRead(fields, out s.SinkKJ);
            }
            else
            {
                s.Protected = !FurnaceState.TryLoad(fields, out s.State);
                if (!s.Protected && s.State.Batch.Chamber.Species.Keys.Concat(s.State.Batch.Receiver.Species.Keys).Any(id => DataHandler.GetCond(id) == null)) s.Protected = true;
            }
            s.Notice = Text.Get(s.Protected ? "Furnace.protected" : "Furnace.reloaded");
        }
        else if (status != SavedStateStatus.Missing) { s.Protected = true; s.Notice = Text.Get("Furnace.protected"); }
        // Unobserved intervals cannot establish an intact radiator/room history.
        // Retain all heat and reset the time anchor; never simulate offline heating.
        s.State.LastEpoch = s.SinkLast = StarSystem.fEpoch;
        ReadCoolingMode(s); ReadCharge(s); ReadRepeat(s);
        sessions[co.strID] = s;
        return s;
    }
    /// <summary>Marks the session's records for settlement; they are written on the session's real-time cadence,
    /// on a fault, and before every native save. Between settlements a crash loses at most that interval.</summary>
    private static void Save(Session s)
    {
        if (s.Protected) return;
        s.Dirty = true;
        if (s.Settle.Due()) Flush(s);
    }
    /// <summary>Writes a dirty session now; a record that already holds these values is left untouched.</summary>
    internal static void Flush(Session s)
    {
        if (s.Protected || !s.Dirty) return;
        s.Dirty = false;
        Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.FurnaceSaves);
        SaveCharge(s);
        var fields = FurnaceRules.Cooling(s.Object.strCODef) ? FurnaceCooling.Save(s.SinkKJ) : s.State.Save();
        if (!s.Store.TryWriteIfChanged(fields)) { s.Protected = true; s.State.Batch.Armed = false; s.Notice = Text.Get("Furnace.protected"); }
    }
    private static void FlushAll(Ship? ship)
    {
        foreach (var s in sessions.Values)
            if (s.Dirty && s.Object != null && !s.Object.bDestroyed && (ship == null || s.Object.ship == ship))
            { try { Flush(s); } catch (Exception ex) { Plugin.Log(ex.ToString()); } }
    }
    /// <summary>The recipe the furnace is set to (bound while a batch is in progress).</summary>
    internal static FurnaceRecipe Recipe(Session s) => FurnaceRecipes.ByRevision(s.State.Recipe) ?? FurnaceRecipes.Housing;
    internal static FurnaceRecipe Recipe(CondOwner furnace) => Recipe(Get(furnace));
    internal static bool CanFeed(CondOwner bin, CondOwner input) => !bin.HasCond("IsLocked") && bin.objCOParent is CondOwner furnace && FurnaceRules.Machine(furnace.strCODef) &&
        (Phobos.Ostranauts.Framework.Crew.CrewLogistics.IsUnitPreflight(input) ? CrewFeed(furnace, input) : ValidFeed(furnace, input)) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || !FurnaceMaterialRules.ChargeFull(bin.objContainer.ContainedCOs.Count));
    internal static bool ValidFeed(CondOwner furnace, CondOwner input) => input != null && !input.bDestroyed && input.Crew == null &&
        FurnaceMaterialRules.Feed(Recipe(furnace), input.strCODef, input.GetTotalMass(), !input.HasCond("IsInstalled"), input.GetCOsSafe(true).Count == 0,
            input.coStackHead == null && input.aStack.Count == 0, input.GetLotCOs(true).Count == 0);
    internal static bool CrewFeed(CondOwner furnace, CondOwner input) => Phobos.Ostranauts.Framework.Crew.CrewLogistics.Loose(input) &&
        input.strCODef==Recipe(furnace).FeedId && ProcessRules.MassMatches(input.GetCondAmount("StatMass"),FurnaceRules.FeedUnitKg);
    private static bool ChargePresent(Session s)
    {
        var bin = Feed(s.Object); var items = bin?.objContainer?.ContainedCOs;
        return items != null && items.Count == FurnaceRules.ChargeUnits && s.State.Inputs.Count == FurnaceRules.ChargeUnits &&
            items.All(c => ValidFeed(s.Object, c) && c.objCOParent == bin && s.State.Inputs.Contains(c.strID));
    }
    private static bool Mounted(CondOwner co) => co != null && !co.bDestroyed && co.HasCond("IsInstalled") &&
        co.objCOParent == null && co.ship != null && (int)co.ship.LoadState >= 2;
    private static bool Intact(CondOwner co) => Mounted(co) && !co.HasCond("IsDamaged");
    private static bool ControlsReady(CondOwner co) => Intact(co) && !co.HasCond("IsLocked") && !co.HasCond("IsOverrideOff") && !co.HasCond("IsSignalOff");
    internal static bool ProbeValid(CondOwner co) => ControlsReady(co) && StarSystem.fEpoch - Get(co).LastReceiptEpoch <= FurnaceRules.ProbeFreshSeconds;
    private static CondOwner? Room(CondOwner co) => co.ship?.GetRoomAtWorldCoords1(co.GetPos("use"), false)?.CO;
    private static bool RoomValues(CondOwner? room, out double kelvin, out double moles)
    {
        kelvin = (room?.GetCondAmount("StatGasTemp") ?? 0) + (room?.GasContainer?.fDGasTemp ?? 0);
        moles = 0;
        var gas = room?.GasContainer;
        if (gas == null || !gas.mapGasMols1.TryGetValue("StatGasMolTotal", out moles)) return false;
        moles += gas.mapDGasMols.Where(p => p.Key != "StatGasMolTotal").Sum(p => p.Value);
        return ThermalMath.Finite(kelvin) && kelvin > 0 && ThermalMath.Finite(moles) && moles > 0;
    }
    internal static bool Exterior(CondOwner radiator)
    {
        if (!Exposed(radiator)) return false;
        var pos = radiator.GetPos(); double angle = radiator.tf.eulerAngles.z;
        for (int x = 0; x < 6; x++)
        {
            var wall = IntakeRules.Rotate(x - 2.5, -2.5, angle);
            var tile = radiator.ship.GetTileAtWorldCoords1((float)(pos.x + wall.X), (float)(pos.y + wall.Y), false);
            if (tile?.coProps?.HasCond("IsWall") != true) return false;
        }
        return true;
    }
    private static bool Exposed(CondOwner radiator)
    {
        if (!Mounted(radiator)) return false;
        var pos = radiator.GetPos();
        for (int x = 0; x < FurnaceRules.Footprint; x++)
        for (int y = 0; y < FurnaceRules.RadiatorDepth; y++)
        {
            var p = IntakeRules.Rotate(x - 2.5, y - 1.5, radiator.tf.eulerAngles.z);
            var outside = radiator.ship.GetTileAtWorldCoords1((float)(pos.x + p.X), (float)(pos.y + p.Y), false);
            if (outside?.coProps != null && (outside.coProps.HasCond("IsWall") || outside.coProps.HasCond("IsFloor"))) return false;
        }
        return true;
    }
    internal static bool PortSupported(CondOwner port)
    {
        if (!Mounted(port)) return false;
        var pos = port.GetPos();
        var props = port.ship.GetTileAtWorldCoords1(pos.x, pos.y, false)?.coProps;
        var floors = new List<CondOwner>();
        port.ship.GetCOsAtWorldCoords1(pos, null, false, true, floors);
        return FurnaceCooling.FloorSupport(props?.HasCond("IsFloor") == true, props?.HasCond("IsFloorSealed") == true,
            props?.HasCond("IsWall") == true, props?.HasCond("IsEVATile") == true,
            floors.Any(f => f.ship == port.ship && Phobos.Ostranauts.Framework.Construction.NativeFloors.IsSoundFloorObject(f)));
    }
    private static bool CoolingMounted(CondOwner endpoint) => FurnaceRules.Underside(endpoint.strCODef) ? PortSupported(endpoint) : Exterior(endpoint);
    private static bool CanRadiate(CondOwner endpoint) => FurnaceRules.Underside(endpoint.strCODef) ? PortSupported(endpoint) : Exposed(endpoint);
    private static bool Geometry(CondOwner furnace, CondOwner endpoint)
    {
        if (furnace.ship == null || furnace.ship != endpoint.ship || !Mounted(furnace) || !CoolingMounted(endpoint)) return false;
        return Routed(furnace) ? CoolantRoute(furnace, endpoint, out _) :
            IntakeRules.SameAngle(furnace.tf.eulerAngles.z, endpoint.tf.eulerAngles.z) && SocketAt(furnace, endpoint) != FurnaceCooling.Socket.None;
    }
    private static CondOwner? SelectedCooling(CondOwner co)
    {
        var link = PortPairing.Read(Port(co));
        return link.State == PortLinkState.Linked ? CollectorService.Resolve(link.PeerObjectId) : null;
    }
    internal static CondOwner? CoolingEndpoint(CondOwner co)
    {
        var link = PortPairing.Read(Port(co));
        var peer = link.State == PortLinkState.Linked ? CollectorService.Resolve(link.PeerObjectId) : null;
        return peer != null && FurnaceRules.Cooling(peer.strCODef) && Geometry(co, peer) && PortPairing.Matches(Port(co), Port(peer)) ? peer : null;
    }
    internal static void Update()
    {
        if (CrewSim.objInstance?.FinishedLoading != true || !Content.Ready || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + .25f;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Furnace);
        family.Members(tracked);
        if (discovery.Due())
        {
            Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.FurnaceCandidates, tracked.Count);
            foreach (var id in sessions.Where(p => p.Value.Object == null || p.Value.Object.bDestroyed).Select(p => p.Key).ToArray()) sessions.Remove(id);
        }
        for (int i = tracked.Count - 1; i >= 0; i--)
        {
            var co = tracked[i];
            if (co.bDestroyed) { tracked.RemoveAt(i); continue; }
            if (co.ship == null || (int)co.ship.LoadState < 2) continue;
            try { var s = Get(co); Advance(s); RepeatStep(s); } catch (Exception ex) { Fault(co, ex); }
            FurnaceConnectionView.Refresh(co);
        }
    }
    private static void AdvanceCooling(Session s)
    {
        double now = StarSystem.fEpoch, dt = now - s.SinkLast; s.SinkLast = now;
        if (s.Protected || dt <= 0 || !ThermalMath.Finite(dt)) return;
        if (dt > FurnaceRules.MaxIntervalSeconds) { s.Notice = Text.Get("Furnace.interval"); Save(s); return; }
        FurnaceCooling.Radiate(ref s.SinkKJ, dt, CanRadiate(s.Object), s.Object.HasCond("IsDamaged"));
        Save(s);
    }
    private static void Advance(Session s)
    {
        if (FurnaceRules.Cooling(s.Object.strCODef)) { AdvanceCooling(s); return; }
        if ((s.Protected || s.State.Batch.Phase != FurnacePhase.Idle) && Feed(s.Object) is CondOwner bin && !bin.HasCond("IsLocked")) bin.AddCondAmount("IsLocked", 1);
        if ((s.Protected || s.State.Batch.Phase == FurnacePhase.Delivering) && !s.Object.HasCond("IsLocked")) s.Object.AddCondAmount("IsLocked", 1);
        var b = s.State.Batch; double now = StarSystem.fEpoch, dt = now - s.State.LastEpoch; s.State.LastEpoch = now;
        if (s.Protected || !ThermalMath.Finite(dt) || dt <= 0) return;
        if (dt > FurnaceRules.MaxIntervalSeconds) { b.Armed = false; b.Hold = 0; s.Notice = Text.Get("Furnace.interval"); Save(s); return; }
        var radiator = CoolingEndpoint(s.Object); Session? sink = radiator == null ? null : Get(radiator);
        if (sink != null) AdvanceCooling(sink);
        bool connected = sink != null && !sink.Protected;
        if(s.Coolant.Enabled) { int cells=ChargeCells(s);s.Coolant.Advance(dt,cells>0&&!s.Object.HasCond("IsDamaged"),false,cells,cells>0&&CircuitFull(s));if(!ChargeReady(s))b.Armed=false; }
        if (connected) b.SinkKJ = sink!.SinkKJ;
        bool probe = ProbeValid(s.Object);
        if (b.Phase != FurnacePhase.Idle && (!ChargePresent(s) || s.State.ShipId != s.Object.ship?.strRegID))
        { b.Armed = false; b.Hold = 0; s.Notice = Text.Get("Furnace.charge_changed"); }
        if (!connected || radiator!.HasCond("IsDamaged") || !probe || Flight(s.Object.ship)) { b.Armed = false; b.Hold = 0; }
        if (b.Armed && (s.Object.HasCond("IsOverrideOff") || s.Object.HasCond("IsSignalOff")))
        { b.Armed = false; b.Hold = 0; s.Notice = Text.Get("Furnace.power_lost"); }
        var room = Room(s.Object);
        bool accepting = RoomValues(room, out double roomK, out double moles);
        double capacity = accepting ? Math.Max(0, Math.Min(b.TemperatureK, FurnaceRules.MaxRoomK) - roomK) * moles * FurnaceRules.GasCv : 0;
        // Radiator advances separately, including when it is unpaired. This call only
        // moves hot-node energy into its finite store and the accepting native room.
        var loss = b.Passive(dt, false, connected && !Routed(s.Object), accepting ? roomK : FurnaceRules.ReferenceK, capacity, probe);
        // The furnace cools as before; the room receives the machine heat share of what it loses (Framework 0.94.0).
        if (loss.Room > 0) room!.GasContainer.fDGasTemp += Phobos.Ostranauts.Framework.Processing.RoomHeat.Machine(loss.Room) / (moles * FurnaceRules.GasCv);
        if (connected) { sink!.SinkKJ = b.SinkKJ; Save(sink); }
        Save(s);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out PowerTransfer? transfer)
    {
        transfer = null; var s = Get(co); Advance(s);
        double seconds = amount * 3600; amount = 0;
        var radiator = CoolingEndpoint(co); var sink = radiator == null ? null : Get(radiator);
        if (!Content.Ready || s.Protected || sink == null || sink.Protected || !ControlsReady(co) ||
            (s.State.Batch.Phase != FurnacePhase.Idle && !ChargePresent(s)) ||
            !ThermalMath.Finite(seconds) || seconds <= 0 || seconds > FurnaceRules.MaxIntervalSeconds)
        { Plugin.Collectors.Interrupt(co, Text.Get("Routing.furnace_interlock")); return false; }
        if (Flight(co.ship) || radiator!.HasCond("IsDamaged")) { s.State.Batch.Armed = false; s.State.Batch.Hold = 0; }
        s.State.Batch.SinkKJ = sink.SinkKJ;
        double headroom = Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - s.State.Batch.SinkK));
        double instrumentation = Math.Min(FurnaceRules.InstrumentKW * seconds, headroom);
        bool routed = Routed(co);
        if(!ChargeReady(s)) {s.State.Batch.Armed=false;s.State.Batch.Hold=0;}
        double pump = routed ? Math.Min(FurnaceCooling.PumpKW * seconds, Math.Max(0, headroom - instrumentation)) : 0;
        bool feeding = Plugin.Collectors.BeforePower(co);
        double motor = feeding ? FurnaceMaterialRules.MotorRequest(seconds, Plugin.Options.FeederKW, headroom, instrumentation + pump) : 0;
        amount = (pump + motor + Math.Max(instrumentation, s.State.Batch.RequestedKJ(seconds, true, pump + motor))) / 3600;
        if (amount <= 0) { Plugin.Collectors.Interrupt(co, Text.Get("Routing.furnace_interlock")); return false; }
        transfer = new PowerTransfer { Session = s, Cooling = sink, Seconds = seconds, Routed = routed, MotorKJ = motor,
            MaterialToken = feeding ? Plugin.Collectors.TransferToken(co) : null, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, PowerTransfer? transfer)
    {
        if (transfer == null || transfer.Finished) return;
        transfer.Finished = true;
        var s = transfer.Session; var b = s.State.Batch;
        double kJ = NativeEnergyReceipts.Complete(power, co, transfer.Receipt) * 3600;
        if (kJ >= FurnaceRules.InstrumentKW * transfer.Seconds - 1e-9) s.LastReceiptEpoch = StarSystem.fEpoch;
        else s.LastReceiptEpoch = double.NegativeInfinity;
        if (CoolingEndpoint(co) != transfer.Cooling.Object)
        {
            // A path lost after admission cannot transport heater losses to the old sink.
            b.Armed = false; b.Hold = 0; b.HotKJ += kJ; s.LastReceiptEpoch = double.NegativeInfinity;
            s.Notice = Text.Get("Furnace.coolant_fault", FurnaceCooling.RouteLimit);
            Plugin.Collectors.Interrupt(co, Text.Get("Routing.furnace_interlock")); Save(s); return;
        }
        double pump = transfer.Routed ? Math.Min(FurnaceCooling.PumpKW * transfer.Seconds,
            Math.Max(0, kJ - FurnaceRules.InstrumentKW * transfer.Seconds)) : 0;
        double motor = FurnaceMaterialRules.MotorReceipt(kJ, transfer.MotorKJ, FurnaceRules.InstrumentKW * transfer.Seconds, pump);
        b.Receive(kJ - pump - motor, transfer.Seconds);
        b.SinkKJ += motor;
        if (transfer.Routed)
        {
            int cells=ChargeCells(s);
            double pumpSeconds=transfer.Seconds*Math.Min(1,pump/(FurnaceCooling.PumpKW*transfer.Seconds));
            // The pump primes the conduit from the reservoir's surplus first (Shipbreaker 0.58.0); circulation needs it full.
            if(s.Coolant.Enabled&&cells>0&&pump>0)PrimeCircuit(s,pumpSeconds);
            bool full=cells>0&&CircuitFull(s);
            double flow=s.Coolant.Enabled?(cells>0?s.Coolant.Flow(cells,full):0):1;
            if(s.Coolant.Enabled&&cells>0)s.Coolant.Advance(pumpSeconds,true,pump>0,cells,full);
            b.Circulate(transfer.Seconds,pump,flow);
        }
        transfer.Cooling.SinkKJ = b.SinkKJ; s.DeliveredKW = kJ / transfer.Seconds;
        if (kJ <= 0) { b.Armed = false; b.Hold = 0; s.Notice = Text.Get("Furnace.power_lost"); Plugin.Collectors.Interrupt(co, Text.Get("Routing.no_power")); }
        Save(transfer.Cooling); Save(s);
        // Revalidate the same physical item and route after settlement. Motor losses stay
        // in the sink even when a late interlock prevents movement; no credit is banked.
        if (transfer.MaterialToken != null)
            Plugin.Collectors.AfterPower(co, motor > 0, motor / Plugin.Options.FeederKW, transfer.MaterialToken);
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        Plugin.Collectors.Interrupt(co, Text.Get("Routing.furnace_interlock"));
        var s = Get(co); s.State.Batch.Armed = false; s.Notice = Text.Get("Furnace.fault_player"); Save(s);
        try { Flush(s); } catch (Exception flush) { Plugin.Log(flush.ToString()); }
        SuspendRepeat(s, s.Notice, true);
        if (s.State.NativeMutation || s.State.Batch.Phase == FurnacePhase.Delivering) s.Protected = true;
        Plugin.Log(Text.Get("Furnace.fault", ex.Message));
    }
    private static bool Flight(Ship? ship) => ship != null && (ship.IsUsingTorchDrive ||
        ship.Reactor?.mapGUIPropMaps.TryGetValue("Panel A", out var map) == true && map.TryGetValue("slidCycle", out var raw) &&
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double cycle) && cycle > 0);
    internal static void FlightCommand(Ship ship)
    {
        // Called for every ship's every nonzero manoeuvre and burn: nothing is allocated, and with no furnace
        // sessions it returns at once.
        if (sessions.Count == 0) return;
        foreach (var s in sessions.Values)
        {
            if (s.Object.ship != ship || !FurnaceRules.Machine(s.Object.strCODef) || !(s.State.Batch.Armed || s.RepeatAuthorized || Plugin.Collectors.ReceivingEnabled(s.Object))) continue;
            Plugin.Collectors.Interrupt(s.Object, Text.Get("Furnace.flight")); s.State.Batch.Armed = false; s.State.Batch.Hold = 0; s.Notice = Text.Get("Furnace.flight"); Save(s);
            // The run never re-arms on the next guidance pulse; the player resumes it after manoeuvring.
            SuspendRepeat(s, Text.Get("Furnace.flight"), true);
        }
    }
}

// A damage, repair or installation replacement is tracked at once rather than at the next discovery pass.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class FurnaceTrackPatch
{
    private static void Postfix(CondOwner coNew) => FurnaceService.Track(coNew);
}
[HarmonyPatch(typeof(Ship), nameof(Ship.Maneuver))]
internal static class FurnaceManeuverPriority
{
    private static void Prefix(Ship __instance, float fX, float fY, float fR)
    { if (fX != 0 || fY != 0 || fR != 0) FurnaceService.FlightCommand(__instance); }
}
[HarmonyPatch(typeof(Ship), nameof(Ship.SetThrust))]
internal static class FurnaceTorchPriority
{
    private static void Prefix(Ship __instance, double fAmount) { if (fAmount > 0) FurnaceService.FlightCommand(__instance); }
}
