using System;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Short powered work bound to one item; it never owns or moves that item.</summary>
public sealed class TransferClock
{
    public const double MaximumStepSeconds = 60;
    public static bool ValidStep(double seconds) => seconds >= 0 && seconds <= MaximumStepSeconds && !double.IsNaN(seconds) && !double.IsInfinity(seconds);
    public double Duration { get; }
    public double Progress { get; private set; }
    public string ItemId { get; }
    public bool Complete => Progress >= Duration;
    public TransferClock(string itemId, double duration)
    {
        if (string.IsNullOrEmpty(itemId) || duration < 1 || !ValidStep(duration))
            throw new ArgumentException(Text.Get("TransferClock.invalid_transfer_cycle"));
        ItemId = itemId; Duration = duration;
    }
    public bool Advance(string itemId, double elapsed, bool powered)
    {
        if (itemId != ItemId || !ValidStep(elapsed)) return false;
        if (powered) Progress = Math.Min(Duration, Progress + elapsed);
        return true;
    }
}
