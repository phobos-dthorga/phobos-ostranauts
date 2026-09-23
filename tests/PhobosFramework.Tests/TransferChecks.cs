using System;
using Phobos.Ostranauts.Framework.Inventory;

internal static class TransferChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var ok = new Move();
        check(PhysicalTransfer.Commit(ok) && ok.AtDestination && !ok.AtSource && ok.Removed == 1, "Exactly one existing item moves");
        check(PhysicalTransfer.Commit(ok) && ok.Removed == 1, "Repeated commit cannot duplicate a delivered item");
        var full = new Move { Fits = false };
        check(!PhysicalTransfer.Commit(full) && full.AtSource && full.Removed == 0, "Blocked destination leaves source untouched");
        foreach (string fault in new[] { "before-remove", "after-remove", "before-place", "after-place", "restore", "ambiguous" })
        {
            var move = new Move { Fault = fault };
            bool failed = false;
            try { PhysicalTransfer.Commit(move); } catch (Exception) { failed = true; }
            check(failed, "Transfer fault reaches caller for pausing: " + fault);
            if (fault == "after-place") check(move.AtDestination && move.Restored == 0, "Late failure never restores a second copy");
            else if (fault == "restore") check(move.Detached, "Failed restore does not discard the surviving object");
            else if (fault == "ambiguous") check(move.Restored == 0, "Ambiguous ownership stops without another mutation");
            else check(move.AtSource && !move.AtDestination, "Interrupted move retains or recovers its original item: " + fault);
        }
    }
    private sealed class Move : IPhysicalTransfer
    {
        internal string Fault = "", Location = "source";
        internal bool Fits = true;
        internal int Removed, Restored;
        public bool AtSource => Location == "source";
        public bool AtDestination => Location == "destination";
        public bool Detached => Location == "detached";
        public bool Prepare() => Fits;
        public void Detach()
        {
            if (Fault == "before-remove") throw new Exception();
            Removed++; Location = Fault == "ambiguous" ? "unknown" : "detached";
            if (Fault == "after-remove") throw new Exception();
        }
        public void Place()
        {
            if (Fault == "before-place" || Fault == "restore") throw new Exception();
            Location = "destination";
            if (Fault == "after-place") throw new Exception();
        }
        public void Restore()
        {
            if (Fault == "restore") throw new Exception();
            Restored++; Location = "source";
        }
    }
}
