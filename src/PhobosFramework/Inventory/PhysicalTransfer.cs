using System;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>A synchronous move of one existing item. Implementations never clone or consume it.</summary>
public interface IPhysicalTransfer
{
    bool AtSource { get; }
    bool AtDestination { get; }
    bool Detached { get; }
    bool Prepare();
    void Detach();
    void Place();
    void Restore();
}

public static class PhysicalTransfer
{
    public static bool Commit(IPhysicalTransfer move)
    {
        if (move.AtDestination) return true;
        if (!move.AtSource || !move.Prepare()) return false;
        try
        {
            move.Detach();
            if (!move.Detached) throw new InvalidOperationException(Text.Get("PhysicalTransfer.transfer_did_not_detach_its_item"));
            move.Place();
            if (!move.AtDestination) throw new InvalidOperationException(Text.Get("PhysicalTransfer.transfer_did_not_reach_its_destination"));
            return true;
        }
        catch (Exception original)
        {
            // A post-placement failure must never restore a second copy at the source.
            // Ambiguous native ownership is a fault: stop instead of retrying blindly.
            if (!move.AtDestination && !move.AtSource)
            {
                try
                {
                    if (!move.Detached) throw new InvalidOperationException(Text.Get("PhysicalTransfer.transfer_ownership_is_ambiguous"));
                    move.Restore();
                    if (!move.AtSource) throw new InvalidOperationException(Text.Get("PhysicalTransfer.transfer_recovery_did_not_restore_its_source"));
                }
                catch (Exception recovery) { throw new AggregateException(Text.Get("PhysicalTransfer.transfer_and_recovery_failed_inspect_the_retained"), original, recovery); }
            }
            throw;
        }
    }
}
