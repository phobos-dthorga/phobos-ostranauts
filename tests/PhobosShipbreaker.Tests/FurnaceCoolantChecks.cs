using System;
using System.Collections.Generic;
using PhobosShipbreaker.Core;

internal static class FurnaceCoolantChecks
{
    internal static void Run(Action<bool, string> check)
    {
        FurnaceBatch Hot() => new() { Phase = FurnacePhase.Cool, Qualified = true,
            HotKJ = FurnaceRules.Enthalpy(700), SinkKJ = 0 };
        void Near(double a, double b, string label) => check(Math.Abs(a - b) < 1e-7, label);
        var noPower = Hot(); double retained = noPower.TotalKJ;
        Near(noPower.Circulate(10, 0), 0, "Piped circulation cannot occur without electricity");
        Near(noPower.TotalKJ, retained, "No-power routed cooling retains all heat");
        double priorTransfer = 0;
        foreach (double fraction in new[] { .1, .5, 1d })
        {
            var b = Hot(); double before = b.TotalKJ;
            double moved = b.Circulate(10, fraction * 10 * FurnaceCooling.PumpKW);
            Near(b.TotalKJ, before + fraction * 10 * FurnaceCooling.PumpKW, "Pump electricity is retained once; transfer conserves energy");
            check(moved > priorTransfer && moved <= FurnaceRules.CoolingKW * 10 * fraction, "Measured partial power bounds circulation");
            check(b.SinkK <= FurnaceRules.SinkMaxK, "Finite sink bounds pumped cooling"); priorTransfer = moved;
        }
        var probeFailed = Hot(); probeFailed.Armed = true;
        probeFailed.Passive(1, false, false, FurnaceRules.ReferenceK, 0, false);
        check(!probeFailed.Armed && probeFailed.Circulate(1, FurnaceCooling.PumpKW) > 0, "Probe failure stops heating but measured pump power can still cool");
        var disconnected = Hot(); double initialHot = disconnected.HotKJ;
        disconnected.Passive(10, false, false, FurnaceRules.ReferenceK, 0, true);
        Near(disconnected.HotKJ, initialHot, "Disconnected route cannot move heat to a saved peer");
        var full = Hot(); full.SinkKJ = FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK);
        Near(full.Circulate(1, 0), 0, "Full sink accepts no additional thermal transfer");
        var heating = Hot(); heating.Phase = FurnacePhase.Preheat; heating.Armed = true;
        double request = heating.RequestedKJ(1, true, FurnaceCooling.PumpKW);
        double total = heating.TotalKJ; heating.Receive(request, 1); heating.Circulate(1, FurnaceCooling.PumpKW);
        Near(heating.TotalKJ, total + request + FurnaceCooling.PumpKW, "Heater and additional pump have a single complete energy budget");
        foreach (string mode in new[] { "direct", "left", "right" })
        {
            check(FurnaceCooling.TryReadMode(new Dictionary<string,string> { ["mode"] = mode }, out var restored) && restored == mode, "Explicit mode survives reload");
        }
        check(!FurnaceCooling.TryReadMode(new Dictionary<string,string> { ["mode"] = "future" }, out _), "Unknown route mode is protected, never treated as direct");
        foreach (double angle in new[] { 0d, 90, 180, 270 })
        foreach (string mode in new[] { "left", "right" })
        {
            var p = FurnaceCooling.CoolantOffset(true, mode); var r = IntakeRules.Rotate(p.X, p.Y, angle);
            var restored = IntakeRules.Rotate(r.X, r.Y, -angle);
            Near(restored.X, p.X, "Furnace service fitting rotates without moving tile alignment");
            Near(restored.Y, p.Y, "Furnace service fitting rotates without moving tile alignment");
            var radiator = FurnaceCooling.CoolantOffset(false, "");
            var q = IntakeRules.Rotate(radiator.X, radiator.Y, angle);
            check(Math.Abs(Math.Abs(q.X % 1) - .5) < 1e-7 && Math.Abs(Math.Abs(q.Y % 1) - .5) < 1e-7, "Radiator fitting stays on a tile centre in every cardinal rotation");
        }
        var saved = new FurnaceState(); saved.Batch.HotKJ = 100; saved.Batch.Armed = true;
        check(FurnaceState.TryLoad(saved.Save(), out var loaded) && !loaded.Batch.Armed && Math.Abs(loaded.Batch.HotKJ - saved.Batch.HotKJ) < 1e-7,
            "Piped integration preserves the original hot-state contract and explicit heating resume");
    }
}
