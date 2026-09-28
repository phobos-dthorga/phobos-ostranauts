using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosShipbreaker.Core;

/// <summary>The next hot-cycle step for a complete twenty-piece charge. Shared by crew
/// standing orders and the machine repeat run; each consumer decides what Resume means.</summary>
public enum FurnaceStep { Wait, Seal, Resume, Equalize, Release }

/// <summary>What the machine repeat run does next. It never re-arms an interrupted batch.</summary>
public enum FurnaceRepeatAction { Wait, Seal, Start, Equalize, Release, Suspend }

public static class FurnaceCycle
{
    /// <summary>Pure step selection. Phases between Idle and Equalize without heating permission
    /// report Resume; passive cooling and valve steps never require re-arming.</summary>
    public static FurnaceStep Next(FurnacePhase phase, bool armed, bool safeOpen, bool chargeFull)
    {
        if (phase == FurnacePhase.Idle) return safeOpen && chargeFull ? FurnaceStep.Seal : FurnaceStep.Wait;
        if (phase == FurnacePhase.Equalize) return safeOpen ? FurnaceStep.Equalize : FurnaceStep.Wait;
        if (phase == FurnacePhase.Ready) return safeOpen ? FurnaceStep.Release : FurnaceStep.Wait;
        if (phase > FurnacePhase.Idle && phase < FurnacePhase.Equalize && !armed) return FurnaceStep.Resume;
        return FurnaceStep.Wait;
    }

    /// <summary>The repeat run starts only a batch it has just sealed. Any other unarmed heating or
    /// unqualified cooling phase is an interruption, which suspends the run for the player.</summary>
    public static FurnaceRepeatAction Repeat(FurnacePhase phase, bool armed, bool qualified, bool safeOpen, bool chargeFull)
    {
        if (phase == FurnacePhase.Delivering) return FurnaceRepeatAction.Suspend;
        switch (Next(phase, armed, safeOpen, chargeFull))
        {
            case FurnaceStep.Seal: return FurnaceRepeatAction.Seal;
            case FurnaceStep.Equalize: return FurnaceRepeatAction.Equalize;
            case FurnaceStep.Release: return FurnaceRepeatAction.Release;
            case FurnaceStep.Resume:
                if (phase == FurnacePhase.Sealed) return FurnaceRepeatAction.Start;
                // A qualified melt only needs passive cooling; heating permission is irrelevant.
                return qualified && (phase == FurnacePhase.Solidify || phase == FurnacePhase.Cool) ? FurnaceRepeatAction.Wait : FurnaceRepeatAction.Suspend;
            default: return FurnaceRepeatAction.Wait;
        }
    }
}

/// <summary>Saved intent of a player-started repeat run. Permission to heat is never saved:
/// a loaded record always waits for an explicit Resume.</summary>
public sealed class FurnaceRepeatRecord
{
    public const string StoreName = "FurnaceRepeatRun";
    public const int Schema = 1;
    public string ShipId = "", RoomId = "", CoolingId = "";
    public int Revision = FurnaceRules.RecipeRevision;
    public int Completed;
    public Dictionary<string, string> Save() => new(StringComparer.Ordinal)
    {
        ["ship"] = ShipId, ["room"] = RoomId, ["cooling"] = CoolingId,
        ["revision"] = Revision.ToString(CultureInfo.InvariantCulture),
        ["completed"] = Completed.ToString(CultureInfo.InvariantCulture)
    };
    public static bool TryLoad(IReadOnlyDictionary<string, string> fields, out FurnaceRepeatRecord record)
    {
        record = new FurnaceRepeatRecord();
        string Id(string key) => fields.TryGetValue(key, out var value) && ObjectStateStore.SafeValue(value) ? value : throw new FormatException(key);
        int Count(string key, int min, int max) => fields.TryGetValue(key, out var value) &&
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n >= min && n <= max ? n : throw new FormatException(key);
        try
        {
            record.ShipId = Id("ship"); record.RoomId = Id("room"); record.CoolingId = Id("cooling");
            // A future recipe revision is protected rather than reinterpreted by this version.
            record.Revision = Count("revision", 1, FurnaceRecipes.MaxRevision);
            record.Completed = Count("completed", 0, int.MaxValue);
            return true;
        }
        catch (FormatException) { record = new FurnaceRepeatRecord(); return false; }
    }
}
