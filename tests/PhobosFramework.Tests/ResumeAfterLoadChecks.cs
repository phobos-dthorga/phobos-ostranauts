using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;

/// <summary>Work carries on after a reload (Framework 0.95.0): a marked machine is offered once per load. The mark
/// itself is a condition on a game object and is checked in the native and in-game checks; this is the pure rule.</summary>
internal static class ResumeAfterLoadChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        check(ResumeAfterLoad.Decide(seen, "a") && !ResumeAfterLoad.Decide(seen, "a"), "A machine is offered once after a load, never twice");
        check(ResumeAfterLoad.Decide(seen, "b"), "Another machine has its own offer");
        check(!ResumeAfterLoad.Decide(seen, "") && !ResumeAfterLoad.Decide(seen, null), "An object without an id is never offered");
        seen.Clear();
        check(ResumeAfterLoad.Decide(seen, "a"), "After the next load the machine may be offered again");
        check(ResumeAfterLoad.Enabled && ResumeAfterLoad.Condition == "PhobosResumeAfterLoad", "The behaviour is on by default, under one saved mark");
    }
}
