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
            if (!move.Detached) throw new InvalidOperationException("Transfer did not detach its item.");
            move.Place();
            if (!move.AtDestination) throw new InvalidOperationException("Transfer did not reach its destination.");
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
                    if (!move.Detached) throw new InvalidOperationException("Transfer ownership is ambiguous.");
                    move.Restore();
                    if (!move.AtSource) throw new InvalidOperationException("Transfer recovery did not restore its source.");
                }
                catch (Exception recovery) { throw new AggregateException("Transfer and recovery failed; inspect the retained item before retrying.", original, recovery); }
            }
            throw;
        }
    }
}
