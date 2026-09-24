using System;
using Phobos.Ostranauts.Framework.Controls;

internal static class ConsoleAccessChecks
{
    internal static void Run(Action<bool,string> check)
    {
        ConsoleBinding New() => new ConsoleBinding("console-a", "ship-a", "crew-a");
        ConsoleAccessFailure Apply(ConsoleBinding b, string? target = "ship-a", string? owner = "player", bool power = true, bool awake = true,
            string? host = "ship-a", string? actor = "crew-a", string? actorShip = "ship-a", string? console = "console-a", string? player = "player") =>
            b.Check(console, host, actor, actorShip, target, owner, player, power, awake);
        check(Apply(New()) == ConsoleAccessFailure.None, "Own console grants only same-ship authority");
        foreach (var target in new[] { "docked-owned-ship", "station", "towed-derelict", "moored-ship", null })
            check(Apply(New(), target:target) == ConsoleAccessFailure.OtherShip, "Docking/ownership never widens console scope: " + target);
        foreach (var owner in new[] { "UNREGISTERED", "another-owner", "leased-owner", null, "" })
            check(Apply(New(), owner:owner) == ConsoleAccessFailure.NotOwned, "Ownership is explicit: " + owner);
        check(Apply(New(), owner:null, player:null) == ConsoleAccessFailure.NotOwned, "Two missing identities cannot confer ownership");
        var powered = New();
        check(Apply(powered, power:false) == ConsoleAccessFailure.ConsoleUnavailable, "Loss of console power blocks commands");
        check(Apply(powered) == ConsoleAccessFailure.None, "Restored power allows same still-valid console session");
        check(Apply(New(), awake:false) == ConsoleAccessFailure.OperatorUnavailable, "Absent/incapacitated operator cannot command");
        var moved = New();
        check(Apply(moved, host:"ship-b") == ConsoleAccessFailure.ConsoleMoved, "Moving console ends session");
        check(Apply(moved) == ConsoleAccessFailure.ConsoleMoved, "Returning moved console does not revive stale authority");
        var crew = New();
        check(Apply(crew, actor:"crew-b") == ConsoleAccessFailure.OperatorChanged, "Changing selected crew ends session");
        check(Apply(crew) == ConsoleAccessFailure.OperatorChanged, "Switching back requires fresh access");
        check(Apply(New(), actorShip:"ship-b") == ConsoleAccessFailure.OperatorChanged, "Crew crossing dock boundary ends session");
        check(Apply(New(), console:null) == ConsoleAccessFailure.ConsoleMoved, "Destroyed/unloaded console fails closed");
        var sold = New();
        check(Apply(sold, owner:"buyer") == ConsoleAccessFailure.NotOwned, "Selling ship blocks an existing session immediately");
        check(new EquipmentActivity(EquipmentState.Blocked,"translated error").NeedsAttention, "Attention uses state, never translated text");
        check(!new EquipmentActivity(EquipmentState.Paused,"blocked wording").NeedsAttention, "Deliberately paused equipment is not an alarm");
    }
}
