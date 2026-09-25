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
        internal string DoseId = "";
        internal NutrientSolution Solution = new();
        internal FluidLine Line = new();
        internal string RecoveryInput="", RecoveryFilter="";
        internal double RecoveryEnergy;
        internal bool RecoveryMetered;
        internal double Last, Received, DeliveredKW, LastPower = double.NegativeInfinity;
        internal bool Protected, Routed;
        internal string Notice = "";
    }
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    internal static void Reset() => sessions.Clear();
    internal static void ModeChanged(CondOwner replacement, Session previous)
    {
        // Native damage/repair modes retain ID/property maps, but rebuild dry mass.
        var next = new Session { Object = replacement, Store = new ObjectStateStore(replacement.mapGUIPropMaps, "Agriculture", Plugin.Id, 1),
            DoseId = previous.DoseId, Workup = previous.Workup.Copy(), State = previous.State.Copy(), Solution = previous.Solution.Copy(), Line=previous.Line.Copy(), RecoveryInput=previous.RecoveryInput, RecoveryFilter=previous.RecoveryFilter, RecoveryEnergy=previous.RecoveryEnergy, RecoveryMetered=previous.RecoveryMetered, Protected = previous.Protected, Routed = previous.Routed, Last = StarSystem.fEpoch, Notice = Text.Get("paused") };
        next.State.Running = next.State.Receiving = false;
        sessions[replacement.strID] = next;
        if (!next.Protected) Save(next);
    }
    internal static void PassiveScan()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || DataHandler.mapCOs == null) return;
        foreach (var co in DataHandler.mapCOs.Values.Where(c => Definitions.Machine(c) && !c.bDestroyed && c.ship != null && (int)c.ship.LoadState >= 2).ToArray())
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
    internal static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co.strID, out var found) && found.Object == co) return found;
        var s = new Session { Object = co, Store = new ObjectStateStore(co.mapGUIPropMaps, "Agriculture", Plugin.Id, 1), Last = StarSystem.fEpoch };
        var status = s.Store.Read(out var fields);
        try
        {
            if (status == SavedStateStatus.Ready) s.State = CropState.Read(fields);
            else if (status != SavedStateStatus.Missing) s.Protected = true;
            var solutionStatus = SolutionStore(co).Read(out var solutionFields);
            if (solutionStatus == SavedStateStatus.Ready) s.Solution = NutrientSolution.Read(solutionFields, s.State);
            else if (solutionStatus != SavedStateStatus.Missing) s.Protected = true;
            ReadWaterMode(s);
            ReadLine(s);
            ReadRecovery(s);
            ReadWorkup(s);
            if (WaterGuard(co).Protected) s.Protected = true;
            if (IrrigationDefinitions.IsSupply(co) && (s.State.CropId.Length != 0 || s.State.CookerInput.Length != 0 || s.State.CookerProgress != 0)) s.Protected = true;
            if (Definitions.IsCooker(co) && s.Solution.Enabled) s.Protected = true;
            if (Math.Abs(co.GetCondAmount("StatMass") - Definitions.DryMass(co) - s.State.ContentsMass - s.Solution.TotalKg - s.Line.TotalKg - PhysicalMass(co)) > 1e-5) s.Protected = true;
        }
        catch { s.Protected = true; }
        s.Notice = Text.Get(s.Protected ? "protected" : "paused"); sessions[co.strID] = s; return s;
    }
    internal static void Save(Session s)
    {
        if (s.Protected) throw new InvalidOperationException(Text.Get("protected"));
        var solutionFields = s.Solution.Save(s.State);
        SaveRecovery(s);
        if (!DosingStore(s.Object).TryWrite(new Dictionary<string,string>{["source"]=s.DoseId})) throw new InvalidOperationException("Protected dosing binding.");
        if (!WorkupStore(s.Object).TryWrite(s.Workup.Save())) throw new InvalidOperationException("Protected workup state.");
        if (!LineStore(s.Object).TryWrite(s.Line.Save())) { s.Protected=true; throw new InvalidOperationException(Text.Get("protected")); }
        if (!s.Store.TryWrite(s.State.Save())) { s.Protected = true; throw new InvalidOperationException(Text.Get("protected")); }
        if (!SolutionStore(s.Object).TryWrite(solutionFields)) { s.Protected = true; throw new InvalidOperationException(Text.Get("protected")); }
        // Native containers already include child cargo in StatMass. Keep it and propagate
        // only the numerical reservoir/biomass difference to any native parent.
        s.Object.AddMass(Definitions.DryMass(s.Object) + s.State.ContentsMass + s.Solution.TotalKg + s.Line.TotalKg + PhysicalMass(s.Object) - s.Object.GetCondAmount("StatMass"), true);
    }
    private static double PhysicalMass(CondOwner co) => co.objContainer?.ContainedCOs.Sum(c => c.GetTotalMass()) ?? 0;
    internal static void Fault(CondOwner co, Exception error)
    { var s = Get(co); s.Watch.Cancel(); s.State.Running = s.State.Receiving = false; s.Protected = true; s.Notice = Text.Get("fault"); Plugin.Log(error.ToString()); }
    internal static CondOwner? Room(CondOwner co) => co.ship?.GetRoomAtWorldCoords1(co.GetPos(), false)?.CO;
    internal static double Moles(GasContainer gas, string key) => Math.Max(0, (gas.mapGasMols1.TryGetValue(key, out var x) ? x : 0) + (gas.mapDGasMols.TryGetValue(key, out var y) ? y : 0));
    internal static bool RoomReady(CondOwner co)
    {
        var room = Room(co); var gas = room?.GasContainer;
        return gas != null && room!.GetCondAmount("StatGasPressure") >= 20 && Moles(gas, "StatGasMolTotal") > 1 &&
            room.GetCondAmount("StatGasTemp") + gas.fDGasTemp < 318.15;
    }
    internal static void BeginRun(CondOwner co) { var s = Get(co); s.Received = 0; }
    internal static double Requested(CondOwner co, double nativeAmount)
    {
        var s = Get(co);
        if (s.Protected || WaterGuard(co).Protected || !RoomReady(co)) return 0;
        double kw = WorkupDefinitions.IsBench(co) ? s.State.Running && s.Workup.Mode.Length > 0 ? NutrientRecovery.PowerKW : 0 : IrrigationDefinitions.IsSupply(co) ? SupplyDemand(s) : Definitions.IsCooker(co) ? s.State.Running && CookerInput(s) != null ? 2 : .02 : s.State.DemandKW;
        return nativeAmount / .02 * kw;
    }
    internal static void Tick(CondOwner co)
    {
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
            gas.fDGasTemp += received * 3600000 / (Moles(gas, "StatGasMolTotal") * 20.8);
            if (s.State.Receiving && !s.Routed && !WorkupDefinitions.IsBench(co) && !Definitions.IsCooker(co) && !IrrigationDefinitions.IsSupply(co) && received > 0 && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged"))
                ShipsWaterSupply.Refill(co.ship, new Reservoir(s), Math.Min(.25 * elapsed, Math.Max(0, s.Solution.PlainWaterCapacity - s.State.Water)), Plugin.ReserveLitres.Value, WaterGuard(co));
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
                if (s.State.Running && CookerInput(s) != null) s.State.CookerProgress = Math.Min(.05, s.State.CookerProgress + received);
                if (s.State.CookerProgress >= .05) Cook(s);
            }
            else
            {
                double temp = room!.GetCondAmount("StatGasTemp") + gas.fDGasTemp, pressure = room.GetCondAmount("StatGasPressure");
                bool habitable = co.HasCond("IsInstalled") && !co.HasCond("IsDamaged") && temp >= 291.15 && temp <= 299.15 && pressure >= 70 && pressure <= 110;
                exchange = s.State.Step(elapsed / 3600, received, Moles(gas, "StatGasMolCO2") * .044, Moles(gas, "StatGasMolO2") * .032, habitable, s.Solution);
                gas.AddGasMols("CO2", exchange.CO2Kg / .044, false); gas.AddGasMols("O2", exchange.OxygenKg / .032, false); gas.AddGasMols("H2O", exchange.VapourKg / .018, false);
                gas.Run();
            }
            // Native gas simulation owns room mixing and later cooling. 20.8 J/mol/K follows native heat accounting.
            gas.fDGasTemp += (exchange.RoomHeatKWh - received) * 3600000 / (Moles(gas, "StatGasMolTotal") * 20.8);
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
        var actor = worker ?? CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") || co.bDestroyed || co.ship == null || actor.ship != co.ship || co.HasCond("IsLocked") || co.objContainer?.Locked == true) return Text.Get("access");
        if (binding == null) return TileUtils.TileRange(actor.GetPos(), co.GetPos("use")) <= LocalAccessTiles ? null : Text.Get("access");
        var console = Resolve(binding.ConsoleId);
        bool consoleReady = console != null && console.strCODef == "PhobosIndustrialConsoleInstalled" && console.HasCond("IsInstalled") && console.HasCond("IsPowered") && !console.HasCond("IsDamaged") && !console.HasCond("IsLocked") && !console.HasCond("IsOverrideOff") && !console.HasCond("IsSignalOff") && console.objCOParent == null;
        return binding.Check(console?.strID, console?.ship?.strRegID, actor.strID, actor.ship?.strRegID, co.ship.strRegID, CrewSim.system?.GetShipOwner(binding.ShipId), CrewSim.coPlayer?.strID, consoleReady,
            console != null && TileUtils.TileRange(actor.GetPos(), console.GetPos("use")) <= ConsoleAccessTiles) == ConsoleAccessFailure.None ? null : Text.Get("access");
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        try { return CheckedCommand(co, binding, action, out message); }
        catch (Exception error) { Fault(co, error); message = Text.Get("fault"); return false; }
    }
    private static bool CheckedCommand(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Access(co, binding) ?? ""; if (message.Length > 0) return false;
        if (action == "status") { message = Describe(co); return true; }
        var s = Get(co); if (s.Protected || WaterGuard(co).Protected || !Definitions.Ready) { message = Text.Get("protected"); return false; }
        if (!WorkupDefinitions.IsBench(co) && !IrrigationDefinitions.IsSupply(co) && (action == "watch" || action == "unwatch" || action == "cue-volume"))
        {
            if (action == "cue-volume") CompletionCues.CycleVolume();
            else if (action == "unwatch") s.Watch.Cancel();
            else
            {
                if (!s.State.Running || (Definitions.IsCooker(co) ? CookerInput(s) == null || s.State.CookerProgress >= .05 : s.State.CropId.Length == 0 || s.State.Ready))
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
            if (action == "recover-crop" || action == "formulate-nutrients")
            {
                if (binding != null) { message = Text.Get("local_work"); return false; }
                CrewSim.GetSelectedCrew().QueueInteraction(co, DataHandler.GetInteraction(Definitions.WorkId(action))); message = Text.Get("queued"); return true;
            }
            if (action != "start" && action != "resume" && action != "pause") { message = Text.Get("help"); return false; }
            if (action != "pause" && s.Workup.Mode.Length == 0) { message = Text.Get("workup_input"); return false; }
        }
        else if (action == "recover-crop" || action == "formulate-nutrients" || action == "cancel-workup") { message = Text.Get("help"); return false; }
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
                    var raw = Input(co, Definitions.Raw, .4);
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
                if (Definitions.IsCooker(co) || !s.Routed && !ShipsWaterSupply.Available) { message = Text.Get("no_provider"); return false; }
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
        if (Definitions.IsCooker(co)) return Text.Get("cooker_status", b.CookerProgress / .05 * 100, Text.Get(b.Running ? "cooking" : "stopped"), environment, s.Protected ? Text.Get("protected") : s.Notice);
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
        }
        return Text.Get("status", b.CropId.Length == 0 ? Text.Get("empty") : Text.Get(b.CropId), b.Progress * 100, b.Health * 100, b.Water, b.Nutrients, b.Biomass,
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
