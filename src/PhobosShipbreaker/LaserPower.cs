using System;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static partial class LaserService
{
    internal sealed class PowerTransfer
    {
        internal RoomHeat.Air Air = null!;
        internal EnergyReceipt Receipt = null!;
        internal string Laser = "";
        internal bool Finished;
    }

    /// <summary>The working step: the request is scaled to the job's captured draw and capped at the work left, the
    /// room behind the mount must be able to take this step's heat, and the electricity that actually arrives is
    /// witnessed. A room that cannot take the heat is a wait, not a stop: nothing is drawn and the job keeps its
    /// progress and permission.</summary>
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out PowerTransfer? transfer, out bool owned)
    {
        transfer = null;
        owned = sessions.TryGetValue(co.strID, out var s) && s.Authorized && s.Demand;
        if (!owned) return true;
        var r = s!.Record; double kw = r.Number("kw");
        amount = Math.Min(amount * kw / LaserRules.WorkingKW, (r.Number("seconds") - r.Number("progress")) * kw / Units.SecondsPerHour);
        var air = RoomHeat.Read(co, "use");
        if (!RoomHeat.Admit(air, LaserRules.HeatKW(kw), amount * Units.SecondsPerHour / kw, out _))
        {
            s.Notice = air == null ? Text.Get("Laser.cooling_block") : Text.Get("Laser.cooling_wait", air.Kelvin - Units.CelsiusToKelvin, air.PressureKPa);
            co.ZeroCondAmount(LaserRules.Working); Present(s, false);
            return false;
        }
        transfer = new PowerTransfer { Laser = co.strID, Air = air!, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        return true;
    }

    internal static void FinishPower(Powered power, CondOwner co, PowerTransfer? transfer)
    {
        if (transfer == null || transfer.Finished) return;
        transfer.Finished = true;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!LaserRules.Finite(supplied) || supplied < 0) throw new InvalidOperationException("Invalid laser energy receipt.");
        // The declared share warms the room's air; the rest left with the vapour and debris.
        RoomHeat.Deposit(transfer.Air, supplied, LaserRules.WasteHeatFraction);
        if (!sessions.TryGetValue(transfer.Laser, out var s)) throw new InvalidOperationException("Laser session lost during receipt.");
        if (s.Record.Phase == LaserPhase.Working) s.Record.Credit(supplied);
        s.Notice = Text.Get(supplied > 0 ? "Laser.cutting" : "Laser.power");
        Present(s, supplied > 0);
        if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
    }
}
