using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Opt-in machine repeat run. The saved record is intent only; permission to seal,
/// heat, equalize and release exists only in this session after an explicit Repeat.</summary>
internal static partial class FurnaceService
{
    internal const double RepeatRetrySeconds = 5;
    private static readonly string[] ManualActions = { "stop", "pause", "isolate", "seal", "resume", "start", "next", "auto-run", "step-run", "recipe",
        "automatic", "step-mode", "equalize", "release", "cooling-direct", "cooling-left", "cooling-right", "pair", "unpair" };
    private static ObjectStateStore RepeatStore(CondOwner co) => new(co.mapGUIPropMaps, FurnaceRepeatRecord.StoreName, Text.Owner, FurnaceRepeatRecord.Schema);
    private static void ReadRepeat(Session s)
    {
        s.Repeat = null; s.RepeatProtected = false;
        if (!FurnaceRules.Machine(s.Object.strCODef)) return;
        var status = RepeatStore(s.Object).Read(out var fields);
        if (status == SavedStateStatus.Missing) return;
        if (status == SavedStateStatus.Ready && FurnaceRepeatRecord.TryLoad(fields, out var record)) s.Repeat = record;
        else s.RepeatProtected = true;
    }
    private static bool WriteRepeat(Session s) => s.Repeat != null && !s.RepeatProtected && RepeatStore(s.Object).TryWrite(s.Repeat.Save());
    internal static bool RepeatActive(CondOwner co) => FurnaceRules.Machine(co.strCODef) && Get(co).RepeatAuthorized;
    private static bool ChargeFull(Session s) => FurnaceMaterialRules.ChargeFull(Feed(s.Object)?.objContainer?.ContainedCOs.Count ?? 0);
    private static bool OwnedByPlayer(CondOwner co) => CrewSim.coPlayer != null && co.ship != null && CrewSim.system?.GetShipOwner(co.ship.strRegID) == CrewSim.coPlayer.strID;
    private static bool ManualOverride(string action) => ManualActions.Contains(action);

    private static bool StartRepeat(Session s, ConsoleBinding? binding, out string message)
    {
        var co = s.Object; var b = s.State.Batch;
        ReadRepeat(s);
        if (s.RepeatProtected) { message = Text.Get("Furnace.repeat_protected"); return false; }
        if (!FurnaceRules.Machine(co.strCODef)) { message = Text.Get("Industry.unsupported_action"); return false; }
        if (!OwnedByPlayer(co)) { message = Text.Get("Furnace.owned_ship"); return false; }
        var room = Room(co); var cooling = CoolingEndpoint(co);
        if (room == null || cooling == null || cooling.HasCond("IsDamaged") || Get(cooling).Protected || s.State.NativeMutation)
        { message = Text.Get("Furnace.repeat_block"); return false; }
        // A deliberate Repeat is also the explicit resume of an interrupted batch.
        bool resume = FurnaceCycle.Repeat(b.Phase, b.Armed, b.Qualified, b.SafeOpen, ChargeFull(s)) == FurnaceRepeatAction.Suspend;
        if (resume && !ResumeReady(s)) { message = Text.Get("Furnace.resume_block"); return false; }
        var record = s.Repeat ?? new FurnaceRepeatRecord();
        record.ShipId = co.ship.strRegID; record.RoomId = room.strID; record.CoolingId = cooling.strID; record.Revision = s.State.Recipe;
        s.Repeat = record;
        if (!WriteRepeat(s)) { s.RepeatProtected = true; message = Text.Get("Furnace.repeat_protected"); return false; }
        // The run always uses the automatic sequence; step mode would wait for a person.
        b.StepMode = false; b.StepWaiting = false;
        if (resume) { b.PumpSeconds = 0; b.Resume(); }
        s.RepeatAuthorized = true; s.RepeatRetry = 0;
        s.RepeatNotice = Text.Get("Furnace.repeat_started");
        IndustryObservations.ClearStop(co);
        if (b.Phase == FurnacePhase.Idle && !ChargeFull(s)) ArmRepeatReceiving(s);
        Save(s);
        message = RepeatStatus(co); return true;
    }
    private static bool EndRepeat(Session s, out string message)
    {
        s.RepeatAuthorized = false; s.RepeatNotice = "";
        // Explicit forget: also clears an unreadable record. Cargo, heat and batch state are untouched.
        RepeatStore(s.Object).Clear(); s.Repeat = null; s.RepeatProtected = false;
        message = Text.Get("Furnace.repeat_ended"); return true;
    }
    private static void SuspendRepeat(Session s, string reason, bool isolate)
    {
        if (!s.RepeatAuthorized) return;
        s.RepeatAuthorized = false;
        var b = s.State.Batch;
        // Revoking automation never cools or opens anything; it only withdraws heating permission.
        if (isolate && b.Armed) { b.Armed = false; b.Hold = 0; Save(s); }
        s.RepeatNotice = Text.Get("Furnace.repeat_paused", reason);
        IndustryObservations.RecordStop(s.Object, s.RepeatNotice);
    }
    private static bool ResumeReady(Session s)
    {
        var co = s.Object; var b = s.State.Batch; var cooling = CoolingEndpoint(co);
        return ChargeReady(s) && ProbeValid(co) && ChargePresent(s) && cooling != null && !cooling.HasCond("IsDamaged") && !Flight(co.ship) &&
            s.State.ShipId == co.ship.strRegID && b.Phase != FurnacePhase.Idle && b.Phase < FurnacePhase.Equalize;
    }
    private static string? RepeatProblem(Session s)
    {
        var co = s.Object; var r = s.Repeat;
        if (s.Protected || s.State.NativeMutation || s.RepeatProtected || r == null) return Text.Get("Furnace.protected");
        if (!ControlsReady(co)) return Text.Get("Furnace.install");
        if (!OwnedByPlayer(co)) return Text.Get("Furnace.owned_ship");
        if (r.Revision != s.State.Recipe || co.ship.strRegID != r.ShipId || CoolingEndpoint(co)?.strID != r.CoolingId || Room(co)?.strID != r.RoomId)
            return Text.Get("Furnace.repeat_changed");
        return null;
    }
    private static void ArmRepeatReceiving(Session s)
    {
        // Receiving follows the saved pair and its own checks. No pair means loading by hand or crew.
        if (PortPairing.Read(CollectorService.Receiver(s.Object)).State == PortLinkState.Linked) Plugin.Collectors.StartAuthorized(s.Object);
    }
    private static void RepeatStep(Session s)
    {
        if (!s.RepeatAuthorized || StarSystem.fEpoch < s.RepeatRetry) return;
        string? problem = RepeatProblem(s);
        if (problem != null) { SuspendRepeat(s, problem, true); return; }
        var co = s.Object; var b = s.State.Batch; string message;
        switch (FurnaceCycle.Repeat(b.Phase, b.Armed, b.Qualified, b.SafeOpen, ChargeFull(s)))
        {
            case FurnaceRepeatAction.Suspend:
                SuspendRepeat(s, Text.Get("Furnace.repeat_interrupted"), true); return;
            case FurnaceRepeatAction.Seal:
                if (Flight(co.ship)) { Retry(s, Text.Get("Furnace.flight")); return; }
                if (!Seal(s, out message)) { Retry(s, message); return; }
                goto case FurnaceRepeatAction.Start;
            case FurnaceRepeatAction.Start:
                if (!ResumeReady(s)) { Retry(s, Text.Get("Furnace.resume_block")); return; }
                b.PumpSeconds = 0; b.StepMode = false; b.Resume(); Save(s);
                s.RepeatNotice = Text.Get("Furnace.repeat_heating"); return;
            case FurnaceRepeatAction.Equalize:
                // Automation never claims the local-valve exception for a rebuilt compartment.
                if (!Equalize(s, false, out message)) Retry(s, message); else s.RepeatNotice = Text.Get("Furnace.repeat_equalized"); return;
            case FurnaceRepeatAction.Release:
                // An unfinished (stopped) batch returns its charge unchanged; only a qualified melt counts.
                bool qualified = b.Qualified;
                if (!Release(s, out message)) { Retry(s, message); return; }
                if (qualified)
                {
                    s.Repeat!.Completed++;
                    if (!WriteRepeat(s)) { s.RepeatProtected = true; SuspendRepeat(s, Text.Get("Furnace.repeat_protected"), true); return; }
                }
                s.RepeatNotice = qualified ? Text.Get("Furnace.repeat_released", s.Repeat!.Completed) : Text.Get("Furnace.repeat_returned");
                ArmRepeatReceiving(s); return;
            default:
                s.RepeatNotice = b.Phase == FurnacePhase.Idle ? Text.Get("Furnace.repeat_waiting", Feed(co)?.objContainer?.ContainedCOs.Count ?? 0, FurnaceRules.ChargeUnits) :
                    Text.Get("Furnace.repeat_cycle", Text.Get("Furnace.phase_" + b.Phase));
                return;
        }
    }
    private static void Retry(Session s, string reason) { s.RepeatRetry = StarSystem.fEpoch + RepeatRetrySeconds; s.RepeatNotice = Text.Get("Furnace.repeat_retry", reason); }
    /// <summary>Presentation only; never advances the run or grants permission.</summary>
    internal static string RepeatStatus(CondOwner co)
    {
        if (!FurnaceRules.Machine(co.strCODef)) return "";
        var s = Get(co);
        if (s.RepeatProtected) return Text.Get("Furnace.repeat_protected");
        if (s.RepeatAuthorized) return Text.Get("Furnace.repeat_on", s.Repeat?.Completed ?? 0, s.RepeatNotice);
        if (s.Repeat != null) return Text.Get("Furnace.repeat_held", s.Repeat.Completed, s.RepeatNotice.Length > 0 ? s.RepeatNotice : Text.Get("Furnace.repeat_reloaded"));
        return Text.Get("Furnace.repeat_off");
    }
}
