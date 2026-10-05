using System;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static partial class LaserService
{
    internal sealed class PowerTransfer
    {
        /// <summary>The room that takes this step's heat, or null when the paired cooling assembly takes it.</summary>
        internal RoomHeat.Air? Air;
        internal CondOwner? Radiator;
        internal EnergyReceipt Receipt = null!;
        internal string Laser = "";
        internal bool Finished;
    }

    /// <summary>The working step: the request is scaled to the job's captured draw and capped at the work left, the
    /// heat of this step must have somewhere to go, and the electricity that actually arrives is witnessed. The heat
    /// goes to the paired cooling assembly when it is ready and has room for all of it; otherwise to the room behind
    /// the mount under the shared air-cooled rule. A room that cannot take it is a wait, not a stop: nothing is
    /// drawn and the job keeps its progress and permission.</summary>
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out PowerTransfer? transfer, out bool owned)
    {
        transfer = null;
        owned = sessions.TryGetValue(co.strID, out var s) && s.Authorized && s.Demand;
        if (!owned) return true;
        var r = s!.Record; double kw = r.Number("kw");
        double wanted = amount * kw / LaserRules.WorkingKW;
        // Crew upkeep (0.82.0): a tuned laser asks for more power while it cuts; its work is counted by what arrives.
        if (kw > 0) Phobos.Ostranauts.Framework.Crew.Upkeep.Draw(co, ref wanted, wanted * Units.SecondsPerHour / kw);
        amount = Math.Min(wanted, (r.Number("seconds") - r.Number("progress")) * kw / Units.SecondsPerHour);
        // The assembly takes the scaled heat, as a room would (Framework 0.94.0 machine heat setting).
        double stepHeatKJ = RoomHeat.Machine(amount * Units.SecondsPerHour * LaserRules.WasteHeatFraction);
        var radiator = s.Radiator != null && !s.Radiator.bDestroyed && !s.Radiator.HasCond("IsDamaged") ? s.Radiator : null;
        if (radiator != null && LaserRules.HeatToRadiator(true, FurnaceService.SinkHeadroomKJ(radiator), stepHeatKJ))
        {
            transfer = new PowerTransfer { Laser = co.strID, Radiator = radiator, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
            return true;
        }
        var air = RoomHeat.Read(co, "use");
        var heat = RoomHeat.Check(air, LaserRules.HeatKW(kw), amount * Units.SecondsPerHour / kw);
        if (!heat.Admitted)
        {
            s.Notice = (radiator != null ? Text.Get("Laser.radiator_full") + " " : "") + RoomHeat.Describe(heat) +
                (heat.Problem == RoomHeat.HeatProblem.NoAir ? " " + Text.Get("Laser.radiator_hint") : "");
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
        // The declared share is heat for the cooling assembly or the room's air; the rest left with the vapour and debris.
        if (transfer.Radiator != null) FurnaceService.SinkDeposit(transfer.Radiator, RoomHeat.Machine(supplied * Units.SecondsPerHour * LaserRules.WasteHeatFraction));
        else RoomHeat.Deposit(transfer.Air!, supplied, LaserRules.WasteHeatFraction);
        if (!sessions.TryGetValue(transfer.Laser, out var s)) throw new InvalidOperationException("Laser session lost during receipt.");
        if (s.Record.Phase == LaserPhase.Working) s.Record.Credit(supplied);
        s.Notice = Text.Get(supplied > 0 ? "Laser.cutting" : "Laser.power");
        Present(s, supplied > 0);
        if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
    }
}
