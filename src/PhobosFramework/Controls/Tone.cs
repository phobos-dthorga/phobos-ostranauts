using UnityEngine;
using Phobos.Ostranauts.Framework.Crew;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>What a colour on a panel means (Framework 0.118.0; owner request, 6 October 2026: colour by priority where
/// it helps). Three meanings only, and each sits beside a word or a count that says the same thing, because colour is
/// supplementary to text (owner rule). No red: nothing on these panels is an alarm.</summary>
public enum Tone
{
    /// <summary>Structure and everything else: slate, or the plain button face.</summary>
    Neutral,
    /// <summary>Needs the player: a stopped or unreadable order, an inspection due, work that waits. Amber.</summary>
    Attention,
    /// <summary>Working, or the one action that moves things forward now. Green.</summary>
    Good
}

/// <summary>Which tone each state takes. No game types beyond the colour, so the offline checks run it.</summary>
public static class Tones
{
    public static Color Of(Tone tone) => tone switch { Tone.Attention => ConsoleWidgets.Amber, Tone.Good => ConsoleWidgets.Green, _ => ConsoleWidgets.Slate };
    /// <summary>A stop or an unreadable record needs the player; running is good; waiting is the crew's business.</summary>
    public static Tone ForOrder(OrderState state) => state switch
    {
        OrderState.Blocked or OrderState.Stopped => Tone.Attention,
        OrderState.Running => Tone.Good,
        _ => Tone.Neutral
    };
    public static Tone ForOrderGroup(string key) => key == OrderGroups.NeedsYou ? Tone.Attention : Tone.Neutral;
    public static Tone ForUpkeep(UpkeepRules.Attention attention) => attention == UpkeepRules.Attention.None ? Tone.Neutral : Tone.Attention;
    public static Tone ForUpkeepGroup(string key) => key == UpkeepRules.AttentionGroup ? Tone.Attention : Tone.Neutral;
    public static Tone ForSkipGroup(string key) => key == CrewSkip.Waits ? Tone.Attention : key == CrewSkip.WillRun ? Tone.Good : Tone.Neutral;
    /// <summary>Apply is the next step while there are changes not yet applied.</summary>
    public static Tone ForApply(bool dirty) => dirty ? Tone.Good : Tone.Neutral;
    /// <summary>Resume is the next step when nothing is pending and an order that has work chosen is not running.</summary>
    public static Tone ForResume(bool dirty, WorkPermission permission, OrderState state) =>
        !dirty && permission != WorkPermission.Enabled && state != OrderState.NeedsSetup ? Tone.Good : Tone.Neutral;
}
