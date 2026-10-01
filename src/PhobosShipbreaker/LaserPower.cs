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
        amount = Math.Min(amount * kw / LaserRules.WorkingKW, (r.Number("seconds") - r.Number("progress")) * kw / Units.SecondsPerHour);
        double stepHeatKJ = amount * Units.SecondsPerHour * LaserRules.WasteHeatFraction;
        var radiator = s.Radiator != null && !s.Radiator.bDestroyed && !s.Radiator.HasCond("IsDamaged") ? s.Radiator : null;
        if (radiator != null && LaserRules.HeatToRadiator(true, FurnaceService.SinkHeadroomKJ(radiator), stepHeatKJ))
        {
            transfer = new PowerTransfer { Laser = co.strID, Radiator = radiator, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
            return true;
        }
        var air = RoomHeat.Read(co, "use");
        if (!RoomHeat.Admit(air, LaserRules.HeatKW(kw), amount * Units.SecondsPerHour / kw, out _))
        {
            s.Notice = (radiator != null ? Text.Get("Laser.radiator_full") + " " : "") +
                (air == null ? Text.Get("Laser.cooling_block") : Text.Get("Laser.cooling_wait", air.Kelvin - Units.CelsiusToKelvin, air.PressureKPa));
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
        if (transfer.Radiator != null) FurnaceService.SinkDeposit(transfer.Radiator, supplied * Units.SecondsPerHour * LaserRules.WasteHeatFraction);
        else RoomHeat.Deposit(transfer.Air!, supplied, LaserRules.WasteHeatFraction);
        if (!sessions.TryGetValue(transfer.Laser, out var s)) throw new InvalidOperationException("Laser session lost during receipt.");
        if (s.Record.Phase == LaserPhase.Working) s.Record.Credit(supplied);
        s.Notice = Text.Get(supplied > 0 ? "Laser.cutting" : "Laser.power");
        Present(s, supplied > 0);
        if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
    }
}
