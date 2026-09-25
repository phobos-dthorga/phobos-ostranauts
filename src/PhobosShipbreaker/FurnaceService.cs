using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
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
        internal ObjectStateStore Store = null!;
    }
    internal sealed class PowerTransfer
    { internal Session Session = null!, Cooling = null!; internal EnergyReceipt Receipt = null!; internal double Seconds; }
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private static float nextScan;
    internal static bool IsEquipment(CondOwner? co) => co != null && (FurnaceRules.Machine(co.strCODef) || FurnaceRules.Cooling(co.strCODef));
    internal static void Reset() { sessions.Clear(); nextScan = 0; }
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
        sessions[co.strID] = s;
        return s;
    }
    private static void Save(Session s)
    {
        if (s.Protected) return;
        var fields = FurnaceRules.Cooling(s.Object.strCODef) ? FurnaceCooling.Save(s.SinkKJ) : s.State.Save();
        if (!s.Store.TryWrite(fields)) { s.Protected = true; s.State.Batch.Armed = false; s.Notice = Text.Get("Furnace.protected"); }
    }
    internal static bool CanFeed(CondOwner bin, CondOwner input) => !bin.HasCond("IsLocked") && ValidFeed(input) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || bin.objContainer.ContainedCOs.Count < FurnaceRules.ChargeUnits);
    private static bool ValidFeed(CondOwner input) => input != null && !input.bDestroyed && input.strCODef == "ItmScrapAluminum" &&
        !input.HasCond("IsInstalled") && input.coStackHead == null && input.aStack.Count == 0 && input.GetCOsSafe(true).Count == 0 &&
        input.GetLotCOs(true).Count == 0 && ProcessRules.MassMatches(input.GetTotalMass(), 1);
    private static bool ChargePresent(Session s)
    {
        var bin = Feed(s.Object); var items = bin?.objContainer?.ContainedCOs;
        return items != null && items.Count == FurnaceRules.ChargeUnits && s.State.Inputs.Count == FurnaceRules.ChargeUnits &&
            items.All(c => ValidFeed(c) && c.objCOParent == bin && s.State.Inputs.Contains(c.strID));
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
            floors.Any(f => !f.bDestroyed && f.ship == port.ship && (f.HasCond("IsFloorGrate") || f.HasCond("IsFloor")) && f.HasCond("IsInstalled") && !f.HasCond("IsDamaged")));
    }
    private static bool CoolingMounted(CondOwner endpoint) => FurnaceRules.Underside(endpoint.strCODef) ? PortSupported(endpoint) : Exterior(endpoint);
    private static bool CanRadiate(CondOwner endpoint) => FurnaceRules.Underside(endpoint.strCODef) ? PortSupported(endpoint) : Exposed(endpoint);
    private static bool Geometry(CondOwner furnace, CondOwner endpoint)
    {
        if (furnace.ship == null || furnace.ship != endpoint.ship || !Mounted(furnace) || !CoolingMounted(endpoint)) return false;
        var p = furnace.GetPos(); var c = endpoint.GetPos();
        return FurnaceCooling.Aligned(FurnaceRules.Underside(endpoint.strCODef), p.x, p.y, furnace.tf.eulerAngles.z, c.x, c.y, endpoint.tf.eulerAngles.z);
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
        foreach (var co in DataHandler.mapCOs.Values.Where(IsEquipment).ToArray())
        {
            if (co.bDestroyed || co.ship == null || (int)co.ship.LoadState < 2) continue;
            try { Advance(Get(co)); } catch (Exception ex) { Fault(co, ex); }
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
        var loss = b.Passive(dt, false, connected, accepting ? roomK : FurnaceRules.ReferenceK, capacity, probe);
        if (loss.Room > 0) room!.GasContainer.fDGasTemp += loss.Room / (moles * FurnaceRules.GasCv);
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
            !ThermalMath.Finite(seconds) || seconds <= 0 || seconds > FurnaceRules.MaxIntervalSeconds) return false;
        if (Flight(co.ship) || radiator!.HasCond("IsDamaged")) { s.State.Batch.Armed = false; s.State.Batch.Hold = 0; }
        s.State.Batch.SinkKJ = sink.SinkKJ;
        double instrumentation = Math.Min(FurnaceRules.InstrumentKW * seconds, Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - s.State.Batch.SinkK)));
        amount = Math.Max(instrumentation, s.State.Batch.RequestedKJ(seconds, true)) / 3600;
        if (amount <= 0) return false;
        transfer = new PowerTransfer { Session = s, Cooling = sink, Seconds = seconds, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, PowerTransfer? transfer)
    {
        if (transfer == null) return;
        var s = transfer.Session; var b = s.State.Batch;
        double kJ = NativeEnergyReceipts.Complete(power, co, transfer.Receipt) * 3600;
        if (kJ >= FurnaceRules.InstrumentKW * transfer.Seconds - 1e-9) s.LastReceiptEpoch = StarSystem.fEpoch;
        else s.LastReceiptEpoch = double.NegativeInfinity;
        b.Receive(kJ, transfer.Seconds); transfer.Cooling.SinkKJ = b.SinkKJ; s.DeliveredKW = kJ / transfer.Seconds;
        if (kJ <= 0) { b.Armed = false; b.Hold = 0; s.Notice = Text.Get("Furnace.power_lost"); }
        Save(transfer.Cooling); Save(s);
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co); s.State.Batch.Armed = false; s.Notice = Text.Get("Furnace.fault", ex.Message); Save(s);
        if (s.State.NativeMutation || s.State.Batch.Phase == FurnacePhase.Delivering) s.Protected = true;
        Plugin.Log(s.Notice);
    }
    private static bool Flight(Ship? ship) => ship != null && (ship.IsUsingTorchDrive ||
        ship.Reactor?.mapGUIPropMaps.TryGetValue("Panel A", out var map) == true && map.TryGetValue("slidCycle", out var raw) &&
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double cycle) && cycle > 0);
    internal static void FlightCommand(Ship ship)
    {
        foreach (var s in sessions.Values.Where(s => s.Object.ship == ship && FurnaceRules.Machine(s.Object.strCODef) && s.State.Batch.Armed))
        { s.State.Batch.Armed = false; s.State.Batch.Hold = 0; s.Notice = Text.Get("Furnace.flight"); Save(s); }
    }
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
