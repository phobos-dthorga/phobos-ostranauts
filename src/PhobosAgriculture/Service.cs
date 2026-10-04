using System;
using Phobos.Ostranauts.Framework.Audio;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosAgriculture;

internal static partial class Service
{
    private const double LocalAccessTiles = 2, ConsoleAccessTiles = 2.5;
    internal sealed class Session
    {
        internal CondOwner Object = null!;
        internal readonly CompletionWatch Watch = new();
        internal bool MealCommitted;
        internal ObjectStateStore Store = null!;
        internal CropState State = new();
        internal WorkupJob Workup = new();
        /// <summary>The B2's straw press (Agriculture 0.44.0); empty on every other machine.</summary>
        internal StrawPress Press = new();
        internal string DoseId = "";
        internal NutrientSolution Solution = new();
        internal FluidLine Line = new();
        internal string RecoveryInput="", RecoveryFilter="";
        internal double RecoveryEnergy;
        internal bool RecoveryMetered;
        internal double Last, Received, DeliveredKW, LastPower = double.NegativeInfinity;
        internal bool Protected, Routed;
        /// <summary>The water tank a full rack sends spare condensate to, and when to look for one again (Agriculture 0.43.0).</summary>
        internal string VapourTank = "";
        internal double VapourCheck;
        internal string Notice = "";
        // Per-session cache for the per-step paths: the water port bank (its ports read the maps live).
        internal Phobos.Ostranauts.Framework.Inventory.PortBank? Bank;
    }
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private static readonly List<CondOwner> scanned = new();
    // Stage 8: the machines come from Framework's shared world sweep instead of a pass over every world object here.
    private static readonly Phobos.Ostranauts.Framework.Discovery.WorldFamily machines =
        Phobos.Ostranauts.Framework.Discovery.WorldFamilies.Register(Plugin.Id + ".machines", Definitions.MachineDefinition);
    internal static void Reset() => sessions.Clear();
    internal static void ModeChanged(CondOwner replacement, Session previous)
    {
        // Native damage/repair modes retain ID/property maps, but rebuild dry mass.
        var next = new Session { Object = replacement, Store = new ObjectStateStore(replacement.mapGUIPropMaps, "Agriculture", Plugin.Id, 1),
            DoseId = previous.DoseId, Workup = previous.Workup.Copy(), Press = previous.Press.Copy(), State = previous.State.Copy(), Solution = previous.Solution.Copy(), Line=previous.Line.Copy(), RecoveryInput=previous.RecoveryInput, RecoveryFilter=previous.RecoveryFilter, RecoveryEnergy=previous.RecoveryEnergy, RecoveryMetered=previous.RecoveryMetered, Protected = previous.Protected, Routed = previous.Routed, Last = StarSystem.fEpoch, Notice = Text.Get("paused") };
        next.State.Running = next.State.Receiving = false;
        sessions[replacement.strID] = next;
        if (!next.Protected) Save(next);
    }
    internal static void PassiveScan()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || DataHandler.mapCOs == null) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Scan);
        machines.Members(scanned);
        Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.Candidates, scanned.Count);
        scanned.RemoveAll(c => c.ship == null || (int)c.ship.LoadState < 2);
        foreach (var co in scanned)
        {
            if (!co.HasCond("IsInstalled") || co.HasCond("IsDamaged")) { BeginRun(co); Tick(co); }
            var display = Get(co);
            Artwork.Refresh(co, display.State, display.Protected);
        }
        foreach (var entry in sessions.ToArray())
        {
            var s = entry.Value;
            if (s.Object == null || s.Object.bDestroyed) { sessions.Remove(entry.Key); continue; }
            if (s.Object.ship == null || (int)s.Object.ship.LoadState < 2) { s.Watch.Cancel(); s.Last = StarSystem.fEpoch; s.State.Running = s.State.Receiving = false; }
        }
    }
    internal static CondOwner? Resolve(string id) => DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(id, out var c) && !c.bDestroyed ? c : null;
    /// <summary>Whether a saved job on the machine names this item (a dose in hand, a recovery input or its filter, a
    /// workup input or supplement, the cooker's portion), so a fitted inventory keeps it inside before anything else.</summary>
    internal static bool NamedByJob(CondOwner machine, CondOwner item)
    {
        try
        {
            var s = Get(machine); string id = item.strID;
            return id == s.DoseId || id == s.RecoveryInput || id == s.RecoveryFilter || id == s.Workup.Input || id == s.Workup.Supplement || id == s.State.CookerInput;
        }
        catch { return false; }
    }
    internal static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co.strID, out var found) && found.Object == co) return found;
        var s = Load(co, out _);
        s.Notice = Text.Get(s.Protected ? "protected" : "paused"); sessions[co.strID] = s; return s;
    }
    /// <summary>Reads the saved records. <paramref name="acceptable"/> is true when the records themselves are
    /// readable and the machine is protected only by evidence an owner may accept: an interrupted transfer
    /// journal, or an item mass that drifted from the records (a native repair or mode switch).</summary>
    private static Session Load(CondOwner co, out bool acceptable)
    {
        var s = new Session { Object = co, Store = new ObjectStateStore(co.mapGUIPropMaps, "Agriculture", Plugin.Id, 1), Last = StarSystem.fEpoch };
        var status = s.Store.Read(out var fields);
        bool records = true, evidence = false;
        try
        {
            if (status == SavedStateStatus.Ready) s.State = CropState.Read(fields);
            else if (status != SavedStateStatus.Missing) { s.Protected = true; records = false; }
            var solutionStatus = SolutionStore(co).Read(out var solutionFields);
            if (solutionStatus == SavedStateStatus.Ready) s.Solution = NutrientSolution.Read(solutionFields, s.State);
            else if (solutionStatus != SavedStateStatus.Missing) { s.Protected = true; records = false; }
            ReadWaterMode(s);
            ReadLine(s);
            ReadRecovery(s);
            ReadWorkup(s);
            if (s.Protected) records = false;
            if (WaterGuard(co).Protected) { s.Protected = true; evidence = true; }
            if (IrrigationDefinitions.IsSupply(co) && (s.State.CropId.Length != 0 || s.State.CookerInput.Length != 0 || s.State.CookerProgress != 0)) { s.Protected = true; records = false; }
            if (Definitions.IsCooker(co) && s.Solution.Enabled) { s.Protected = true; records = false; }
            if (Math.Abs(co.GetCondAmount("StatMass") - ExpectedMass(co, s)) > 1e-5) { s.Protected = true; evidence = true; }
        }
        catch { s.Protected = true; records = false; }
        acceptable = records && evidence;
        return s;
    }
    private static double ExpectedMass(CondOwner co, Session s) => Definitions.DryMass(co) + s.State.ContentsMass + s.Solution.TotalKg + s.Line.TotalKg + s.Press.TotalKg + PhysicalMass(co);
    /// <summary>Owner-confirmed recovery of a protected machine: the readable records are trusted, an interrupted
    /// transfer journal is closed and the item's mass is set back to what the records say. Unreadable or
    /// inconsistent records cannot be accepted.</summary>
    internal static bool Accept(CondOwner co, out string message)
    {
        message = Text.Get("accept_unavailable");
        var probe = Load(co, out bool acceptable);
        if (!acceptable || !WaterGuard(co).Resolve()) return false;
        co.AddMass(ExpectedMass(co, probe) - co.GetCondAmount("StatMass"), true);
        sessions.Remove(co.strID);
        if (Get(co).Protected) return false;
        message = Text.Get("accept_done"); return true;
    }
    internal static void Save(Session s)
    {
        if (s.Protected) throw new InvalidOperationException(Text.Get("protected"));
        var solutionFields = s.Solution.Save(s.State);
        SaveRecovery(s);
        // Every record is validated on each save; a record that already holds these values is left as it is.
        Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.Saves);
        if (!DosingStore(s.Object).TryWriteIfChanged(DosingBinding.Save(s.DoseId))) throw new InvalidOperationException("Protected dosing binding.");
        if (!WorkupStore(s.Object).TryWriteIfChanged(s.Workup.Save())) throw new InvalidOperationException("Protected workup state.");
        if (WorkupDefinitions.IsBench(s.Object) && !PressStore(s.Object).TryWriteIfChanged(s.Press.Save())) throw new InvalidOperationException("Protected straw press.");
        if (!LineStore(s.Object).TryWriteIfChanged(s.Line.Save())) { s.Protected=true; throw new InvalidOperationException(Text.Get("protected")); }
        if (!s.Store.TryWriteIfChanged(s.State.Save())) { s.Protected = true; throw new InvalidOperationException(Text.Get("protected")); }
        if (!SolutionStore(s.Object).TryWriteIfChanged(solutionFields)) { s.Protected = true; throw new InvalidOperationException(Text.Get("protected")); }
        // Native containers already include child cargo in StatMass. Keep it and propagate
        // only the numerical reservoir/biomass difference to any native parent.
        s.Object.AddMass(Definitions.DryMass(s.Object) + s.State.ContentsMass + s.Solution.TotalKg + s.Line.TotalKg + s.Press.TotalKg + PhysicalMass(s.Object) - s.Object.GetCondAmount("StatMass"), true);
    }
    private static double PhysicalMass(CondOwner co) => co.objContainer?.ContainedCOs.Sum(c => c.GetTotalMass()) ?? 0;
    internal static void Fault(CondOwner co, Exception error)
    { var s = Get(co); s.Watch.Cancel(); s.State.Running = s.State.Receiving = false; s.Protected = true; s.Notice = Text.Get("fault"); Plugin.Log(error.ToString()); }
    internal static CondOwner? Room(CondOwner co) => co.ship?.GetRoomAtWorldCoords1(co.GetPos(), false)?.CO;
    internal static double Moles(GasContainer gas, string key) => Math.Max(0, (gas.mapGasMols1.TryGetValue(key, out var x) ? x : 0) + (gas.mapDGasMols.TryGetValue(key, out var y) ? y : 0));
    /// <summary>The room's carbon dioxide partial pressure in kPa: its share of the room's moles times the room pressure.</summary>
    internal static double Co2KPa(CondOwner room, GasContainer gas)
    {
        double total = Moles(gas, "StatGasMolTotal");
        return total > 0 ? room.GetCondAmount("StatGasPressure") * Moles(gas, "StatGasMolCO2") / total : 0;
    }
    internal static bool RoomReady(CondOwner co)
    {
        var room = Room(co); var gas = room?.GasContainer;
        return gas != null && room!.GetCondAmount("StatGasPressure") >= 20 && Moles(gas, "StatGasMolTotal") > 1 &&
            room.GetCondAmount("StatGasTemp") + gas.fDGasTemp < 318.15;
    }
    internal static void BeginRun(CondOwner co) { var s = Get(co); s.Received = 0; }
    /// <summary>The definition's own idle draw. Every installed appliance keeps it, so the game's power state
    /// (IsPowered) follows the real connection even when nothing runs or the machine is protected.</summary>
    internal const double StandbyKW = .02;
    internal static double Requested(CondOwner co, double nativeAmount)
    {
        var s = Get(co);
        double kw = s.Protected || WaterGuard(co).Protected || !RoomReady(co) ? 0 :
            WorkupDefinitions.IsBench(co) ? !s.State.Running ? 0 : s.Workup.Mode.Length > 0 ? NutrientRecovery.PowerKW : PressWork(s) ? StrawPress.PowerKW : 0 :
            IrrigationDefinitions.IsSupply(co) ? SupplyDemand(s) : Definitions.IsCooker(co) ? s.State.Running && CookerInput(s) != null ? HearthRecipes.CookerKW : 0 : s.State.DemandKW;
        return nativeAmount / StandbyKW * Math.Max(kw, StandbyKW);
    }
    internal static void Tick(CondOwner co)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Tick);
        var s = Get(co); double elapsed = StarSystem.fEpoch - s.Last; s.Last = StarSystem.fEpoch;
        if (s.Protected || co.ship == null || (int)co.ship.LoadState < 2) return;
        try
        {
            bool wasReady = s.State.Ready; s.MealCommitted = false;
            double received = s.Received; s.Received = 0;
            if (elapsed > 0 && elapsed <= 3600) { s.DeliveredKW = received * 3600 / elapsed; s.LastPower = StarSystem.fEpoch; }
            if (!CropState.Finite(elapsed) || elapsed < 0 || elapsed > 3600)
            { s.Watch.Cancel(); s.State.Running = s.State.Receiving = false; elapsed = 0; s.Notice = Text.Get("gap"); }
            var room = Room(co); var gas = room?.GasContainer;
            if (gas == null || Moles(gas, "StatGasMolTotal") < 1)
            {
                // No imaginary vacuum sink. Power admission above prevents consumption here.
                if (received > 0) throw new InvalidOperationException("Agriculture lost its heat recipient during a native power call.");
                s.Watch.Cancel(); s.State.Health = Math.Max(0, s.State.Health - elapsed / 3600 * .1); s.State.Running = false; Save(s); return;
            }
            // Settle measured electricity even if a later liquid adapter fails.
            // Machine heat share (Framework 0.94.0): both halves of the room's heat are scaled alike.
            gas.fDGasTemp += RoomHeat.Machine(received) * 3600000 / (Moles(gas, "StatGasMolTotal") * 20.8);
            if (s.State.Receiving && !s.Routed && !WorkupDefinitions.IsBench(co) && !Definitions.IsCooker(co) && !IrrigationDefinitions.IsSupply(co) && received > 0 && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged"))
            {
                // Only tanks touching the rack or on its water line (Agriculture 0.32.0, the owner's link rule).
                if (ShipsWaterSupply.Available && ShipsWaterSupply.ReachableTanks(co).Count == 0) s.Notice = Text.Get("shipswater_unreached");
                else ShipsWaterSupply.Refill(co, new Reservoir(s), Math.Min(.25 * elapsed, Math.Max(0, s.Solution.PlainWaterCapacity - s.State.Water)), Plugin.ReserveLitres.Value, WaterGuard(co));
            }
            Exchange exchange;
            if (IrrigationDefinitions.IsSupply(co))
            {
                exchange = new Exchange { RoomHeatKWh = received };
                Pump(s, elapsed, received);
            }
            else if (WorkupDefinitions.IsBench(co))
            {
                exchange = new Exchange { RoomHeatKWh = received };
                if (co.HasCond("IsInstalled") && !co.HasCond("IsDamaged")) WorkupTick(s, received);
                else s.State.Running = false;
            }
            else if (Definitions.IsCooker(co))
            {
                exchange = new Exchange { RoomHeatKWh = received };
                double needKWh = CookerKWh(s);
                if (s.State.Running && CookerInput(s) != null) s.State.CookerProgress = Math.Min(needKWh, s.State.CookerProgress + received);
                if (s.State.CookerProgress >= needKWh) Cook(s);
            }
            else
            {
                double temp = room!.GetCondAmount("StatGasTemp") + gas.fDGasTemp, pressure = room.GetCondAmount("StatGasPressure");
                bool habitable = co.HasCond("IsInstalled") && !co.HasCond("IsDamaged") && temp >= 291.15 && temp <= 299.15 && pressure >= 70 && pressure <= 110;
                // Standby draw is machine heat, not lamp energy. Transpired water stays in the rack: the game's
                // air has no water vapour species, so an H2O emission was silently discarded.
                double standby = Math.Min(received, StandbyKW * elapsed / 3600);
                // Agriculture 0.43.0: an enriched room grows the crop faster for the same light (Co2Response).
                double co2Factor = Co2Response.Factor(Co2KPa(room, gas));
                exchange = s.State.Step(elapsed / 3600, received - standby, Moles(gas, "StatGasMolCO2") * .044, Moles(gas, "StatGasMolO2") * .032, habitable, s.Solution, co2Factor);
                exchange.RoomHeatKWh += standby;
                // Condensate a full reservoir cannot hold goes to a linked water tank instead of vanishing (Agriculture 0.43.0).
                exchange.VapourKg -= VapourReturn.Deposit(s, exchange.VapourKg);
                gas.AddGasMols("CO2", exchange.CO2Kg / .044, false); gas.AddGasMols("O2", exchange.OxygenKg / .032, false);
                gas.Run();
            }
            // Native gas simulation owns room mixing and later cooling. 20.8 J/mol/K follows native heat accounting.
            gas.fDGasTemp += RoomHeat.Machine(exchange.RoomHeatKWh - received) * 3600000 / (Moles(gas, "StatGasMolTotal") * 20.8);
            Save(s);
            if (s.MealCommitted || !wasReady && s.State.Ready)
            {
                var actor = CrewSim.GetSelectedCrew();
                CompletionCues.Complete(s.Watch, actor?.strID ?? "", actor?.ship == co.ship ? co.ship.strRegID : "");
            }
            else if (s.Watch.Armed && (!s.State.Running || co.HasCond("IsDamaged") || !co.HasCond("IsInstalled"))) s.Watch.Cancel();
        }
        catch (Exception ex) { Fault(co, ex); }
    }
    internal static string? Access(CondOwner co, ConsoleBinding? binding = null, CondOwner? worker = null)
    {
        var actor = worker ?? Phobos.Ostranauts.Framework.Crew.CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") || co.bDestroyed || co.ship == null || actor.ship != co.ship || co.HasCond("IsLocked") || co.objContainer?.Locked == true) return Text.Get("access");
        if (binding == null) return Phobos.Ostranauts.Framework.Crew.CrewWork.LocalAccess(actor, co, LocalAccessTiles) ? null : Text.Get("access");
        var console = Resolve(binding.ConsoleId);
        bool consoleReady = console != null && console.strCODef == "PhobosIndustrialConsoleInstalled" && console.HasCond("IsInstalled") && console.HasCond("IsPowered") && !console.HasCond("IsDamaged") && !console.HasCond("IsLocked") && !console.HasCond("IsOverrideOff") && !console.HasCond("IsSignalOff") && console.objCOParent == null;
        return binding.Check(console?.strID, console?.ship?.strRegID, actor.strID, actor.ship?.strRegID, co.ship.strRegID, CrewSim.system?.GetShipOwner(binding.ShipId), CrewSim.coPlayer?.strID, consoleReady,
            console != null && TileUtils.TileRange(actor.GetPos(), console.GetPos("use")) <= ConsoleAccessTiles) == ConsoleAccessFailure.None ? null : Text.Get("access");
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        try { bool done = CheckedCommand(co, binding, action, out message); if (done && (action == "pause" || action == "pause-receive" || action == "cancel")) Phobos.Ostranauts.Framework.Crew.CrewWork.ManualStop(co); return done; }
        catch (Exception error) { Fault(co, error); message = Text.Get("fault"); return false; }
    }
    private static bool CheckedCommand(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        if(IrrigationDefinitions.IsSupply(co)&&action.StartsWith("bulk-target:",StringComparison.Ordinal))return BulkService.SetTarget(co,binding,action.Substring(12),out message);
        message = Access(co, binding) ?? ""; if (message.Length > 0) return false;
        if(IrrigationDefinitions.IsSupply(co)&&action.StartsWith("bulk-link:",StringComparison.Ordinal))return BulkService.Link(co,action.Substring(10),binding,out message);
        if (action == "status") { message = Describe(co); return true; }
        if (action == "accept") return Accept(co, out message);
        var s = Get(co); if (s.Protected || WaterGuard(co).Protected || !Definitions.Ready) { message = Text.Get("protected"); return false; }
        if (!WorkupDefinitions.IsBench(co) && !IrrigationDefinitions.IsSupply(co) && (action == "watch" || action == "unwatch" || action == "cue-volume"))
        {
            if (action == "cue-volume") CompletionCues.CycleVolume();
            else if (action == "unwatch") s.Watch.Cancel();
            else
            {
                if (!s.State.Running || (Definitions.IsCooker(co) ? CookerInput(s) == null || s.State.CookerProgress >= CookerKWh(s) : s.State.CropId.Length == 0 || s.State.Ready))
                { message = Text.Get("cue_start_first"); return false; }
                s.Watch.Arm(CrewSim.GetSelectedCrew().strID, co.ship.strRegID);
            }
            message = Describe(co); return true;
        }
        if (IrrigationDefinitions.IsSupply(co) && (action.StartsWith("dose:",StringComparison.Ordinal) || action == "dose-inventory" || action == "dose-off"))
        {
            if(!Paused(s)) {message=Text.Get("water_pause");return false;}
            var selected=action=="dose-inventory"?DoseCandidates(s).OrderBy(c=>c.strID,StringComparer.Ordinal).FirstOrDefault():action=="dose-off"?null:DoseCandidates(s).FirstOrDefault(c=>c.strID==action.Substring(5));
            if(selected==null && action!="dose-off"){message=Text.Get("dose_empty");return false;}
            s.DoseId=selected?.strID??"";Save(s);message=Describe(co);return true;
        }
        if (WorkupDefinitions.IsBench(co))
        {
            if (action == "cancel-workup" && Paused(s)) { s.Workup = new(); Save(s); message = Describe(co); return true; }
            if (action == "empty-press")
            {
                if (!Paused(s)) { message = Text.Get("press_pause"); return false; }
                bool emptied = EmptyPress(s); message = s.Notice; return emptied;
            }
            if (WorkupDefinitions.IsWork(action))
            {
                if (binding != null) { message = Text.Get("local_work"); return false; }
                CrewSim.GetSelectedCrew().QueueInteraction(co, DataHandler.GetInteraction(Definitions.WorkId(action))); message = Text.Get("queued"); return true;
            }
            if (action != "start" && action != "resume" && action != "pause") { message = Text.Get("help"); return false; }
            if (action != "pause" && s.Workup.Mode.Length == 0 && !PressWork(s)) { message = Text.Get(s.Press.Empty ? "workup_input" : "press_short"); return false; }
        }
        else if (WorkupDefinitions.IsWork(action) || action == "cancel-workup" || action == "empty-press") { message = Text.Get("help"); return false; }
        if(action=="cancel-recovery" && IrrigationDefinitions.IsSupply(co) && Paused(s)) {s.RecoveryInput=s.RecoveryFilter="";s.RecoveryEnergy=0;s.RecoveryMetered=false;Save(s);message=Describe(co);return true;}
        if (SolutionCommand(s, action, out message) is bool solutionHandled) return solutionHandled;
        if (WaterCommand(s, action, out message) is bool handled) return handled;
        if (Definitions.Work.Contains(action))
        {
            if (binding != null || Definitions.IsCooker(co) || IrrigationDefinitions.IsSupply(co) && action != "load-water" && action != "load-irrigation" && action != "load-nutrients" && action != "drain" && action != "recover-solution") { message = Text.Get("local_work"); return false; }
            CrewSim.GetSelectedCrew().QueueInteraction(co, DataHandler.GetInteraction(Definitions.WorkId(action))); message = Text.Get("queued"); return true;
        }
        switch (action)
        {
            case "start": case "resume":
                if (!co.HasCond("IsInstalled") || co.HasCond("IsDamaged")) { message = Text.Get("repair"); return false; }
                if (Definitions.IsCooker(co) && s.State.CookerInput.Length == 0)
                {
                    var raw = Cookable(co);
                    if (raw == null) { message = Text.Get("missing_input"); return false; }
                    s.State.CookerInput = raw.strID;
                }
                if (Definitions.IsCooker(co) && CookerInput(s) == null) { message = Text.Get("cancel_missing"); return false; }
                s.State.Running = true; break;
            case "pause": s.Watch.Cancel(); s.State.Running = false; break;
            case "cancel":
                if (!Definitions.IsCooker(co)) { message = Text.Get("help"); return false; }
                s.Watch.Cancel(); s.State.Running = false; s.State.CookerInput = ""; s.State.CookerProgress = 0; break;
            case "receive":
                if (Definitions.IsCooker(co) || !s.Routed && !ShipsWaterSupply.Available && !(IrrigationDefinitions.IsSupply(co)&&BulkService.HasSelection(co))) { message = Text.Get("no_provider"); return false; }
                s.State.Receiving = true; break;
            case "pause-receive": s.State.Receiving = false; break;
            default: message = Text.Get("help"); return false;
        }
        s.Notice = ""; Save(s); message = Describe(co); return true;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co); var b = s.State; var room = Room(co);
        if (WorkupDefinitions.IsBench(co)) return DescribeWorkup(s);
        if (IrrigationDefinitions.IsSupply(co)) return DescribeSupply(s);
        string environment = room == null || room.GasContainer == null ? Text.Get("unknown") : Text.Get("environment", room.strID, room.GetCondAmount("StatGasTemp") - 273.15, room.GetCondAmount("StatGasPressure"));
        environment += "\n" + Text.Get(s.Watch.Armed ? "cue_watching" : s.Watch.Completed ? "cue_completed" : "cue_off") + "\n" + CompletionCues.VolumeLabel;
        environment += "\n" + (StarSystem.fEpoch - s.LastPower <= 5 ? Text.Get("power_reading", s.DeliveredKW) : Text.Get("power_unknown"));
        if (Definitions.IsCooker(co)) return Text.Get("cooker_status", b.CookerProgress / CookerKWh(s) * 100, Text.Get(b.Running ? "cooking" : "stopped"), environment, s.Protected ? Text.Get("protected") : s.Notice);
        if (b.CropId.Length > 0)
        {
            var warnings = new List<string>();
            if (b.Health <= 0) warnings.Add(Text.Get("crop_dead"));
            if (b.DarkHours > 0) warnings.Add(Text.Get("crop_stress", b.DarkHours));
            if (b.Water + s.Solution.Quantity.CarrierKg < .25) warnings.Add(Text.Get("water_low"));
            if (b.Nutrients + s.Solution.Quantity.SoluteKg < .001) warnings.Add(Text.Get("nutrient_low"));
            if (room?.GasContainer != null && Moles(room.GasContainer, "StatGasMolCO2") <= 1e-6) warnings.Add(Text.Get("carbon_low"));
            if (b.Running && !b.Ready && StarSystem.fEpoch - s.LastPower <= 5 && s.DeliveredKW < b.DemandKW * .95) warnings.Add(Text.Get("power_low"));
            environment += "\n" + string.Join("\n", warnings);
            var planted = Crop.Get(b.CropId);
            if (planted.Picks > 0) environment += "\n" + Text.Get("picks_status", b.Picks, planted.Picks);
            if (room?.GasContainer != null) { double kPa = Co2KPa(room, room.GasContainer); environment += "\n" + Text.Get("co2_growth", kPa, Co2Response.Factor(kPa)); }
        }
        return Text.Get("status", b.CropId.Length == 0 ? Text.Get("empty") : Crop.Get(b.CropId).Name, b.Progress * 100, b.Health * 100, b.Water, b.Nutrients, b.Biomass,
            Text.Get(b.Running ? "running" : "paused"), Text.Get(b.Receiving ? "receiving" : "manual"), environment, s.Protected || WaterGuard(co).Protected ? Text.Get("protected") : s.Notice) + "\n" + DescribeWaterRoute(s);
    }
    private sealed class Reservoir : ILiquidReservoir
    {
        private readonly Session s; internal Reservoir(Session s) { this.s = s; }
        public string Identity => s.Object.strID; public string ShipId => s.Object.ship.strRegID; public string Commodity => "water";
        public double QuantityKg => s.State.Water; public double CapacityKg => s.Solution.PlainWaterCapacity;
        public void SetQuantity(double kg) { s.State.Water = kg; Save(s); }
    }
}
