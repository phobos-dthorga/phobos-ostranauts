using System;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>The story system's view of the game, for other mods (Framework 0.127.0): the same facts and record the arcs
/// read, so a lender, a shop or any other content can be local, gated and remembered exactly as story content is.</summary>
public static partial class StoryArcs
{
    /// <summary>Whether a game is running with a player whose story record is read; reads it on first use.</summary>
    internal static bool Attached()
    {
        if (!Ready) return false;
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        return player != null;
    }

    internal static GameFacts? Facts() => Attached() ? new GameFacts(player!) : null;

    /// <summary>A flag changed from code: saved now, and the next check comes at once so gated content follows.</summary>
    internal static void FlagsChanged() { Save(); cadence.Invalidate(); }

    /// <summary>F3 <c>story try &lt;arc&gt;</c>: <see cref="TryBegin"/>, the requirement-honouring start other mods use.</summary>
    internal static string TryCommand(string id) => TryBegin(id, out var message) ? message : Text.Get("Story.try_refused", id, message);

    /// <summary>Starts an arc from code, as if it had started by itself (Framework 0.127.0): its requirements, its
    /// thread's and its place must hold, and an arc already under way, or finished and not repeatable, is not started
    /// again. The arc limit for arcs starting by themselves does not apply. Returns false with the reason when it does
    /// not start.</summary>
    public static bool TryBegin(string arcId, out string message)
    {
        if (!Attached()) { message = Text.Get("Story.not_in_game"); return false; }
        if (arcId == null || !StoryContent.Library.Arcs.TryGetValue(arcId, out var arc)) { message = Text.Get("Story.unknown_arc_command", arcId ?? ""); return false; }
        if (record.Arcs.TryGetValue(arcId, out var progress))
        {
            if (progress.State == ArcState.Active) { message = Text.Get("Story.already_active", arcId); return false; }
            if (!(arc.Value.repeatable && progress.State == ArcState.Done)) { message = Text.Get("Story.not_again"); return false; }
        }
        var facts = new GameFacts(player!);
        string? blocked = new Gates(facts).Blocked(arc.Value.requires, arc.Value.thread) ??
            (PlaceOf(arc.Value.thread, arc.Value.place) is string place && !facts.Near(place) ? Text.Get("Story.needs_place", place) : null);
        if (blocked != null) { message = blocked; return false; }
        Begin(arc, facts);
        Save();
        message = Text.Get("Story.started", arcId);
        return true;
    }
}

/// <summary>Where the player is, as story content sees it (Framework 0.127.0): the regional place they are in and the
/// place they are docked at, from Framework's places table. Every answer is read now; nothing is cached between calls.</summary>
public static class StoryLocation
{
    /// <summary>The regional place the player is in (the nearest regional station, as the game's traffic control sees
    /// it), or null when no game is running or no place is known.</summary>
    public static string? Region => StoryArcs.Facts()?.Region;
    /// <summary>The place the player is docked at or aboard, or null.</summary>
    public static string? DockedPlace => StoryArcs.Facts()?.DockedPlace;
    /// <summary>Whether the player is at a place: docked at it or a part of it, or, for a regional place, anywhere in
    /// its region. False when no game is running.</summary>
    public static bool Near(string place) => place != null && StoryArcs.Facts()?.Near(place) == true;
}

/// <summary>A story <c>requires</c> block checked against the live game and the player's story record (Framework
/// 0.127.0), with the same wording the F3 story report uses.</summary>
public static class StoryGates
{
    /// <summary>Null when every requirement (and the thread's, when one is named) holds; otherwise the first that does
    /// not. With no game running, says so.</summary>
    public static string? Blocked(StoryRequires? requires, string? thread = null)
    {
        var facts = StoryArcs.Facts();
        if (facts == null) return Text.Get("Story.not_in_game");
        return new StoryArcs.Gates(facts).Blocked(requires, thread);
    }
}

/// <summary>The player's story flags, for mods that mark what happened so story packs can react (Framework 0.127.0):
/// the same flags arc outcomes set, which <c>requires.flags</c> and <c>notFlags</c> read. Flags are saved with the
/// player's story record.</summary>
public static class StoryFlags
{
    /// <summary>Whether the flag is set. False with no game running.</summary>
    public static bool Has(string flag) => StorySchema.IsId(flag) && StoryArcs.Attached() && StoryArcs.Record.Flags.ContainsKey(flag);

    /// <summary>When the flag was set (game epoch, seconds), or null.</summary>
    public static double? SetAt(string flag) => StorySchema.IsId(flag) && StoryArcs.Attached() && StoryArcs.Record.Flags.TryGetValue(flag, out var at) ? at : null;

    /// <summary>Sets a flag, keeping the time it was first set. False when no game is running; throws for a flag that
    /// is not a story id (lowercase words joined by dashes), so a mistake shows at once.</summary>
    public static bool Set(string flag)
    {
        if (!StorySchema.IsId(flag)) throw new ArgumentException(Text.Get("Story.bad_flag", flag ?? ""), nameof(flag));
        if (!StoryArcs.Attached()) return false;
        if (StoryArcs.Record.Flags.ContainsKey(flag)) return true;
        StoryArcs.Record.SetFlag(flag, StarSystem.fEpoch);
        StoryArcs.FlagsChanged();
        return true;
    }

    /// <summary>Sets a flag as of now, renewing its time when it was set already (Framework 0.131.0): the thing it marks
    /// happened again, so recurring news (<c>onceEach</c>) and other mods watching the time can follow it.</summary>
    public static bool Renew(string flag)
    {
        if (!StorySchema.IsId(flag)) throw new ArgumentException(Text.Get("Story.bad_flag", flag ?? ""), nameof(flag));
        if (!StoryArcs.Attached()) return false;
        StoryArcs.Record.RenewFlag(flag, StarSystem.fEpoch);
        StoryArcs.FlagsChanged();
        return true;
    }

    /// <summary>Clears a flag. False when no game is running.</summary>
    public static bool Clear(string flag)
    {
        if (!StorySchema.IsId(flag)) throw new ArgumentException(Text.Get("Story.bad_flag", flag ?? ""), nameof(flag));
        if (!StoryArcs.Attached()) return false;
        if (!StoryArcs.Record.Flags.ContainsKey(flag)) return true;
        StoryArcs.Record.ClearFlag(flag);
        StoryArcs.FlagsChanged();
        return true;
    }
}
