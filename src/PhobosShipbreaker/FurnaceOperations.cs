using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static partial class FurnaceService
{
    internal static bool Command(ConsoleBinding? binding, CondOwner co, string action, string? value, out string message)
    {
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length > 0) return false;
        var s = Get(co); var b = s.State.Batch;
        if (s.Protected) { message = Text.Get("Furnace.protected"); return false; }
        try
        {
            // Isolation remains available with broken instrumentation or missing power.
            if (action == "stop" || action == "pause" || action == "isolate")
            { b.Stop(); s.Notice = Text.Get("Furnace.stopped"); Save(s); message = s.Notice; return true; }
            if (action == "status") { message = Describe(co); return true; }
            if (!Content.Ready || !Intact(co)) { message = Text.Get("Furnace.install"); return false; }
            if (co.HasCond("IsLocked")) { message = Text.Get("Furnace.locked"); return false; }
            if (action == "pair")
            {
                if (CrewSim.coPlayer == null || CrewSim.system?.GetShipOwner(co.ship.strRegID) != CrewSim.coPlayer.strID)
                { message = Text.Get("Furnace.owned_ship"); return false; }
                var peer = value == null ? null : CollectorService.Resolve(value);
                if (!FurnaceRules.Machine(co.strCODef) || peer == null || !FurnaceRules.Cooling(peer.strCODef) || !Geometry(co, peer) || Get(peer).Protected)
                { message = peer != null && FurnaceRules.Cooling(peer.strCODef) ? ConnectionProblem(co, peer) ?? Text.Get("Furnace.protected") : Text.Get("Furnace.geometry"); return false; }
                if (PortPairing.Matches(Port(co), Port(peer))) return true;
                if (UnsafeMaintenance(co) || UnsafeMaintenance(peer))
                { message = Text.Get("Furnace.hot_maintenance"); return false; }
                return PortPairing.TryLink(Port(co), Port(peer), out message);
            }
            if (action == "unpair")
            {
                if (UnsafeMaintenance(co)) { message = Text.Get("Furnace.hot_maintenance"); return false; }
                var link = PortPairing.Read(Port(co)); var peer = CollectorService.Resolve(link.PeerObjectId);
                if (peer != null && UnsafeMaintenance(peer)) { message = Text.Get("Furnace.hot_maintenance"); return false; }
                PortPairing.Unlink(Port(co), peer == null ? null : Port(peer)); return true;
            }
            if (!FurnaceRules.Machine(co.strCODef)) { message = Text.Get("Industry.unsupported_action"); return false; }
            if (action == "feed" || action == "products")
            {
                if (binding != null) { message = Text.Get("Industry.local_only"); return false; }
                if (action == "feed" && b.Phase != FurnacePhase.Idle) { message = Text.Get("Furnace.sealed"); return false; }
                var item = action == "feed" ? Feed(co) : co;
                if (item?.objContainer == null || CrewSim.inventoryGUI == null) return false;
                CrewSim.inventoryGUI.SpawnInventoryWindow(item, Ostranauts.Inventory.InventoryWindowType.Container, null); return true;
            }
            if (action == "heat" || action == "ramp" || action == "cool")
            {
                double max = action == "heat" ? FurnaceRules.HeatLimitKW : action == "ramp" ? FurnaceRules.MaxRamp : FurnaceRules.CoolingKW;
                double min = action == "ramp" ? FurnaceRules.MinRamp : FurnaceRules.MinPowerSettingKW;
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) || !ThermalMath.Finite(n) || n < min || n > max)
                { message = Text.Get("Furnace.range", min, max); return false; }
                if (action == "heat") b.HeatCapKW = n; else if (action == "ramp") b.RampKPerSecond = n; else b.CoolingCapKW = n;
                Save(s); return true;
            }
            if (action == "automatic" || action == "step-mode") { b.StepMode = action == "step-mode"; b.StepWaiting = false; Save(s); return true; }
            if (action == "seal") return Seal(s, out message);
            if (action == "equalize") return Equalize(s, binding == null, out message);
            if (action == "release") return Release(s, out message);
            if (action == "resume" || action == "start" || action == "next" || action == "auto-run" || action == "step-run")
            {
                if (!ProbeValid(co) || !ChargePresent(s) || CoolingEndpoint(co) == null || CoolingEndpoint(co)!.HasCond("IsDamaged") || Flight(co.ship) || s.State.ShipId != co.ship.strRegID ||
                    b.Phase == FurnacePhase.Idle || b.Phase >= FurnacePhase.Equalize)
                { message = Text.Get("Furnace.resume_block"); return false; }
                b.PumpSeconds = 0;
                if (action == "auto-run" || action == "step-run") b.StepMode = action == "step-run";
                if (action == "next" && b.Armed) b.AdvanceStep(); else b.Resume();
                s.Notice = Text.Get("Furnace.enabled"); Save(s); message = s.Notice; return true;
            }
            message = Text.Get("Industry.unsupported_action"); return false;
        }
        catch (Exception ex) { Fault(co, ex); message = s.Notice; return false; }
    }
    private static bool Seal(Session s, out string message)
    {
        var co = s.Object; var b = s.State.Batch; var bin = Feed(co);
        message = Text.Get("Furnace.seal_block");
        if (b.Phase != FurnacePhase.Idle || !b.SafeOpen || bin?.objContainer == null || bin.HasCond("IsLocked") ||
            CoolingEndpoint(co) == null || CoolingEndpoint(co)!.HasCond("IsDamaged") || !ProbeValid(co)) return false;
        var items = bin.objContainer.ContainedCOs.ToArray();
        if (items.Length != FurnaceRules.ChargeUnits || items.Any(c => !ValidFeed(c) || c.objCOParent != bin)) return false;
        var room = Room(co); var gas = room?.GasContainer;
        if (gas == null) return false;
        gas.Run();
        if (!RoomValues(room, out double kelvin, out double moles) || kelvin < FurnaceRules.MinSealRoomK || kelvin > FurnaceRules.ReleaseK ||
            room!.GetCondAmount("StatVolume") <= FurnaceRules.ChamberM3) return false;
        double fraction = FurnaceRules.ChamberM3 / room.GetCondAmount("StatVolume");
        var captured = new GasParcel(FurnaceRules.GasCv);
        foreach (var pair in gas.mapGasMols1.Where(p => p.Key != "StatGasMolTotal"))
        {
            if (!pair.Key.StartsWith("StatGasMol", StringComparison.Ordinal) || !ThermalMath.Finite(pair.Value) || pair.Value < 0 || DataHandler.GetCond(pair.Key) == null) return false;
            captured.Add(pair.Key, pair.Value * fraction, kelvin);
        }
        if (Math.Abs(captured.Moles - moles * fraction) > 1e-6 ||
            captured.Moles * FurnaceRules.GasR * kelvin / FurnaceRules.ReceiverM3 > FurnaceRules.ReceiverLimitKPa) return false;
        b.Seal(captured, kelvin); s.State.Inputs.AddRange(items.Select(c => c.strID));
        s.State.ShipId = co.ship.strRegID; s.State.RoomId = room.strID;
        s.State.NativeMutation = true; Save(s);
        if (s.Protected) throw new InvalidOperationException(Text.Get("Furnace.protected"));
        // Native locked child container keeps the exact physical feed aboard until
        // safe release. It is not converted to an invisible inventory or mass credit.
        bin.AddCondAmount("IsLocked", 1 - bin.GetCondAmount("IsLocked"));
        foreach (var pair in captured.Species) gas.AddGasMols(pair.Key.Substring(10), -pair.Value, false);
        gas.Run(); co.AddCondAmount("StatMass", GasMass(captured));
        s.State.NativeMutation = false;
        s.Notice = Text.Get("Furnace.sealed"); Save(s); message = s.Notice; return true;
    }
    private static double GasMass(GasParcel parcel) => parcel.Species.Sum(p => GasContainer.GetGasMass(p.Key.Substring(10), p.Value));
    private static bool Equalize(Session s, bool localValveOperation, out string message)
    {
        var co = s.Object; var b = s.State.Batch;
        message = Text.Get("Furnace.equalize_block");
        if (b.Phase != FurnacePhase.Equalize || !b.SafeOpen || !ProbeValid(co) || !ChargePresent(s)) return false;
        var room = Room(co);
        // A rebuilt compartment can have a new native ID. Remote commands retain
        // their captured scope; a crew member at the cool valve may explicitly
        // return gas to the current adjacent room on the same ship.
        if (room?.GasContainer == null || (!localValveOperation && room.strID != s.State.RoomId) || co.ship.strRegID != s.State.ShipId) return false;
        room.GasContainer.Run();
        if (!RoomValues(room, out double temp, out double moles)) return false;
        var returned = new GasParcel(FurnaceRules.GasCv); b.Chamber.SetTemperature(b.TemperatureK);
        returned.Add(b.Chamber); returned.Add(b.Receiver);
        double mixed = (moles * FurnaceRules.GasCv * temp + returned.EnergyKJ) / ((moles + returned.Moles) * FurnaceRules.GasCv);
        if (mixed > FurnaceRules.MaxRoomK || !ThermalMath.Finite(mixed)) return false;
        s.State.NativeMutation = true; Save(s);
        if (s.Protected) throw new InvalidOperationException(Text.Get("Furnace.protected"));
        foreach (var pair in returned.Species) room.GasContainer.AddGasMols(pair.Key.Substring(10), pair.Value, false);
        room.GasContainer.fDGasTemp += mixed - temp; room.GasContainer.Run();
        co.AddCondAmount("StatMass", -GasMass(returned));
        b.HotKJ -= b.Chamber.EnergyKJ - b.Chamber.Moles * FurnaceRules.GasCv * FurnaceRules.ReferenceK;
        b.Chamber.Take(b.Chamber.Moles); b.Receiver.Take(b.Receiver.Moles);
        s.State.NativeMutation = false;
        b.Phase = FurnacePhase.Ready; b.Armed = false; s.Notice = Text.Get("Furnace.ready"); Save(s); message = s.Notice; return true;
    }
    private static bool Release(Session s, out string message)
    {
        var co = s.Object; var b = s.State.Batch; message = Text.Get("Furnace.release_block");
        if (b.Phase != FurnacePhase.Ready || !b.SafeOpen || !ChargePresent(s)) return false;
        var radiator = CoolingEndpoint(co); if (radiator == null) return false;
        var sink = Get(radiator); if (sink.Protected) return false;
        double carried = FurnaceRules.ChargeUnits * FurnaceRules.SolidCp * (b.TemperatureK - FurnaceRules.ReferenceK);
        if (sink.SinkKJ + carried > FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK)) return false;
        if (b.Qualified)
        {
            var products = new List<CondOwner>();
            bool committing = false;
            try
            {
                foreach (string id in new[] { FurnaceRules.Blank, FurnaceRules.Remainder })
                {
                    var p = DataHandler.GetCondOwner(id); if (p == null) throw new InvalidOperationException("Furnace product definition unavailable."); products.Add(p);
                }
                if (!ProcessRules.MassMatches(products[0].GetTotalMass(), 19) || !ProcessRules.MassMatches(products[1].GetTotalMass(), 1) ||
                    products.Any(p => p.coStackHead != null || p.aStack.Count != 0 || p.GetCOsSafe(true).Count != 0 || !co.objContainer.AllowedCO(p))) return false;
                var plan = BatchPlacement.Plan(ProcessingService.Occupancy(co.objContainer), products.Select(p => GUIInventoryItem.GetWidthHeightForCO(p)).Select(p => new ItemSize(p.x, p.y)).ToArray());
                if (plan == null) return false;
                for (int i = 0; i < products.Count; i++)
                {
                    co.objContainer.AddCOSimple(products[i], new PairXY(plan[i].X, plan[i].Y));
                    if (products[i].objCOParent != co || !co.objContainer.ContainedCOs.Contains(products[i])) throw new InvalidOperationException("Furnace output placement failed.");
                }
                // Synchronous native commit, with a persisted no-retry marker. If native
                // destruction fails midway, retain all evidence and require recovery;
                // never replay twenty inputs into another pair of products on reload.
                b.Phase = FurnacePhase.Delivering; co.AddCondAmount("IsLocked", 1); Save(s); committing = true;
                if (s.Protected) throw new InvalidOperationException(Text.Get("Furnace.protected"));
                foreach (var input in Feed(co)!.objContainer.ContainedCOs.ToArray())
                { input.RemoveFromCurrentHome(true); if (input.objCOParent != null || input.ship != null) throw new InvalidOperationException("Furnace input retirement failed."); input.Destroy(); }
            }
            finally
            {
                if (!committing)
                    foreach (var p in products) if (p != null && !p.bDestroyed) { if (p.objCOParent != null || p.ship != null) p.RemoveFromCurrentHome(true); p.Destroy(); }
            }
        }
        // Released stock starts at the model's reference temperature. Its residual
        // sensible heat remains accounted in the radiator; no hot-output energy vanishes.
        b.HotKJ -= carried; sink.SinkKJ += carried; b.Phase = FurnacePhase.Idle;
        b.Armed = b.Qualified = b.StepWaiting = false; b.Hold = b.PumpSeconds = 0;
        s.State.Inputs.Clear(); s.State.ShipId = s.State.RoomId = "";
        co.ZeroCondAmount("IsLocked");
        Feed(co)!.ZeroCondAmount("IsLocked"); Feed(co)!.objContainer.Redraw(); co.objContainer.Redraw();
        s.Notice = Text.Get("Furnace.released"); Save(sink); Save(s); message = s.Notice; return true;
    }
    private static bool OwnUnsafeMaintenance(CondOwner co)
    {
        var s = Get(co);
        return !FurnaceCooling.CanChange(s.Protected || s.State.NativeMutation,
            FurnaceRules.Cooling(co.strCODef) || s.State.Batch.Phase == FurnacePhase.Idle,
            FurnaceRules.Cooling(co.strCODef) ? FurnaceRules.ReferenceK + s.SinkKJ / FurnaceRules.SinkCapacity : s.State.Batch.TemperatureK,
            Feed(co)?.objContainer?.ContainedCOs.Count ?? 0, co.objContainer?.ContainedCOs.Count ?? 0);
    }
    internal static bool UnsafeMaintenance(CondOwner co)
    {
        if (OwnUnsafeMaintenance(co)) return true;
        // Use the saved endpoint even when mounting has failed. A broken connection cannot bypass hot-removal checks.
        var peer = SelectedCooling(co);
        return peer != null && IsEquipment(peer) && OwnUnsafeMaintenance(peer);
    }
    internal static bool UnsafeRepair(CondOwner co)
    {
        var s = Get(co);
        return s.Protected || s.State.NativeMutation || s.State.Batch.Phase == FurnacePhase.Delivering ||
            (FurnaceRules.Cooling(co.strCODef) ? FurnaceRules.ReferenceK + s.SinkKJ / FurnaceRules.SinkCapacity > FurnaceRules.ReleaseK : !s.State.Batch.SafeOpen);
    }
    private static string CoolingMountStatus(CondOwner endpoint)
    {
        bool port = FurnaceRules.Underside(endpoint.strCODef);
        string state = CoolingMounted(endpoint) ? Text.Get(port ? "Furnace.underside_ready" : "Furnace.exterior") :
            Text.Get(port ? "Furnace.port_support" : "Furnace.radiator_mount_fault");
        return Text.Get("Furnace.endpoint_status", endpoint.strNameFriendly, endpoint.strID, state);
    }
    private static string CoolingConnectionStatus(CondOwner furnace)
    {
        var endpoint = SelectedCooling(furnace);
        if (endpoint == null || !FurnaceRules.Cooling(endpoint.strCODef)) return Text.Get("Furnace.no_cooling");
        return CoolingMountStatus(endpoint) + "\n" + (ConnectionProblem(furnace, endpoint) ?? Text.Get("Furnace.socket_connected", Text.Get("Furnace.socket_" + SocketAt(furnace, endpoint))));
    }
    internal static FurnaceCooling.Socket SocketAt(CondOwner furnace, CondOwner endpoint)
    {
        var f = furnace.GetPos(); var p = endpoint.GetPos();
        foreach (var socket in FurnaceRules.Underside(endpoint.strCODef) ? new[] { FurnaceCooling.Socket.Left, FurnaceCooling.Socket.Right } : new[] { FurnaceCooling.Socket.Rear })
        {
            string name = "Cooling" + socket;
            if (furnace.mapPoints.ContainsKey(name))
            {
                var point = furnace.GetPos(name);
                if (IntakeRules.Near(point.x, point.y, p.x, p.y)) return socket;
                continue;
            }
            // An older saved object can lack a newly added named point. Preserve
            // its historical geometry without rewriting maps or saved pairing.
            var offset = FurnaceCooling.Offset(socket);
            var rotated = IntakeRules.Rotate(offset.X, offset.Y, furnace.tf.eulerAngles.z);
            if (IntakeRules.Near(f.x + rotated.X, f.y + rotated.Y, p.x, p.y)) return socket;
        }
        return FurnaceCooling.Socket.None;
    }
    internal static FurnaceCooling.Socket ConnectedSocket(CondOwner endpoint)
    {
        var furnace = SelectedCooling(endpoint);
        return furnace != null && FurnaceRules.Machine(furnace.strCODef) && CoolingEndpoint(furnace) == endpoint ? SocketAt(furnace, endpoint) : FurnaceCooling.Socket.None;
    }
    internal static string? ConnectionProblem(CondOwner furnace, CondOwner endpoint)
    {
        if (!FurnaceRules.Machine(furnace.strCODef) || furnace.ship == null || furnace.ship != endpoint.ship) return Text.Get("Furnace.connection_ship");
        if (!Mounted(furnace) || !Mounted(endpoint)) return Text.Get("Furnace.install");
        if (SocketAt(furnace, endpoint) == FurnaceCooling.Socket.None) return Text.Get("Furnace.connection_socket");
        if (!IntakeRules.SameAngle(furnace.tf.eulerAngles.z, endpoint.tf.eulerAngles.z)) return Text.Get("Furnace.connection_rotation");
        if (!CoolingMounted(endpoint)) return Text.Get(FurnaceRules.Underside(endpoint.strCODef) ? "Furnace.port_support" : "Furnace.radiator_mount_fault");
        if (!PortPairing.Matches(Port(furnace), Port(endpoint))) return Text.Get("Furnace.connection_unpaired");
        return null;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co); if (s.Protected) return Text.Get("Furnace.protected");
        if (FurnaceRules.Cooling(co.strCODef)) return Text.Get("Furnace.radiator_status", Intact(co) ? (FurnaceRules.ReferenceK + s.SinkKJ / FurnaceRules.SinkCapacity - 273.15).ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown"),
            CoolingMountStatus(co), s.Notice);
        var b = s.State.Batch;
        string Reading(double n, string format) => ProbeValid(co) ? n.ToString(format, CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown");
        var radiator = CoolingEndpoint(co); var sink = radiator == null ? null : Get(radiator);
        string cooling = sink == null || sink.Protected || radiator!.HasCond("IsDamaged") || !ProbeValid(co) ? Text.Get("Furnace.unknown") :
            Text.Get("Furnace.cooling_status", FurnaceRules.ReferenceK + sink.SinkKJ / FurnaceRules.SinkCapacity - 273.15,
                Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK) - sink.SinkKJ) / 1000);
        return Text.Get("Furnace.status", Text.Get("Furnace.phase_" + b.Phase), Text.Get(b.Armed ? "Furnace.heating_enabled" : "Furnace.heating_paused"),
            Reading(b.TemperatureK - 273.15, "F1"), Reading(b.PressureKPa, "F3"), Reading(b.ReceiverKPa, "F2"),
            Reading(s.DeliveredKW, "F1"), b.Hold.ToString("F1", CultureInfo.CurrentCulture),
            CoolingConnectionStatus(co), b.HeatCapKW, b.RampKPerSecond, b.CoolingCapKW,
            b.StepMode ? Text.Get("Furnace.step") : Text.Get("Furnace.auto"), s.Notice) + "\n" + cooling;
    }
    internal static bool F3(string input, out bool success, out string response)
    {
        success = false; response = ""; var words = input.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosfurnace", StringComparison.OrdinalIgnoreCase)) return false;
        response = Text.Get("Furnace.help");
        if (words.Length < 2 || words[1] == "help") { success = true; return true; }
        if (CrewSim.objInstance?.FinishedLoading != true) { response = Text.Get("Industry.world_unavailable"); return true; }
        if (words[1] == "list") { response = string.Join("\n", IndustryService.Discover(CrewSim.GetSelectedCrew()?.ship).Where(IsEquipment).Select(c => c.strNameFriendly + " — " + c.strID)); success = true; return true; }
        var co = words.Length >= 3 ? CollectorService.Resolve(words[2]) : null;
        if (!IsEquipment(co)) return true;
        if (words[1] == "controls") { success = IndustrialPanel.Open(co!); return true; }
        success = Command(null, co!, words[1], words.Length > 3 ? words[3] : null, out response); return true;
    }
}

// Native work orders may have been queued before sealing; recheck both offer and
// effects. Damage transitions retain native persistent properties and physical cargo.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class FurnaceMaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (__result && FurnaceMaintenanceFinish.Blocked(__instance.strName, objUs, objThem))
        { __instance.AddFailReason("main", Text.Get("Furnace.hot_maintenance")); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class FurnaceMaintenanceFinish
{
    internal static bool Blocked(string action, CondOwner us, CondOwner them)
    {
        bool removal = action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0 || action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0;
        bool repair = action.IndexOf("Repair", StringComparison.OrdinalIgnoreCase) >= 0 || action.IndexOf("Undamage", StringComparison.OrdinalIgnoreCase) >= 0 || action.IndexOf("Restore", StringComparison.OrdinalIgnoreCase) >= 0;
        return (removal || repair) && new[] { us, them }.Any(c => FurnaceService.IsEquipment(c) &&
            (removal ? FurnaceService.UnsafeMaintenance(c) : FurnaceService.UnsafeRepair(c)));
    }
    private static bool Prefix(Interaction __instance) => !Blocked(__instance.strName, __instance.objUs, __instance.objThem);
}
