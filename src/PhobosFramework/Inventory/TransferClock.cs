using System;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Short powered work bound to one item; it never owns or moves that item.</summary>
public sealed class TransferClock
{
    public double Duration { get; }
    public double Progress { get; private set; }
    public string ItemId { get; }
    public bool Complete => Progress >= Duration;
    public TransferClock(string itemId, double duration)
    {
        if (string.IsNullOrEmpty(itemId) || double.IsNaN(duration) || double.IsInfinity(duration) || duration < 1 || duration > 60)
            throw new ArgumentException(Text.Get("TransferClock.invalid_transfer_cycle"));
        ItemId = itemId; Duration = duration;
    }
    public bool Advance(string itemId, double elapsed, bool powered)
    {
        if (itemId != ItemId || elapsed < 0 || elapsed > 60 || double.IsNaN(elapsed) || double.IsInfinity(elapsed)) return false;
        if (powered) Progress = Math.Min(Duration, Progress + elapsed);
        return true;
    }
}
