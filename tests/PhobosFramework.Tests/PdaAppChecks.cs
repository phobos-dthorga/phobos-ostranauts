using System;
using Phobos.Ostranauts.Framework.Pda;

/// <summary>Framework 0.126.0 PDA apps: the name rules, the page split the game applies and the tooltip keys the game
/// reads. Registration with the game's tables and the open prefix are native checks.</summary>
internal static class PdaAppChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(PdaApps.NameProblem("phobos_bank") == null, "a prefixed lowercase name is accepted");
        check(PdaApps.NameProblem("") != null && PdaApps.NameProblem(null) != null, "an app needs a name");
        check(PdaApps.NameProblem("Phobos_Bank") != null, "capitals are refused: the game upper-cases the name for its tooltip keys");
        check(PdaApps.NameProblem("phobos:bank") != null, "a colon is refused: the game splits the name there");
        check(PdaApps.NameProblem("phobos bank") != null && PdaApps.NameProblem("phobos-bank") != null, "spaces and hyphens are refused");
        check(PdaApps.NameProblem("1bank") != null, "a name starts with a letter");
        check(PdaApps.NameProblem(new string('a', PdaApps.MaxNameLength + 1)) != null && PdaApps.NameProblem(new string('a', PdaApps.MaxNameLength)) == null, "the length limit is inclusive");
        foreach (var native in PdaApps.NativeNames) check(PdaApps.NameProblem(native) != null, "the game's own app " + native + " is never taken over");
        check(PdaApps.AppPart("phobos_bank:loans") == "phobos_bank" && PdaApps.AppPart("phobos_bank") == "phobos_bank" && PdaApps.AppPart(null) == "", "a page after the colon is not part of the app name");
        check(PdaApps.TitleKey("phobos_bank") == "GUI_PDA_BUTTON_PHOBOS_BANK_TITLE" && PdaApps.TooltipKey("phobos_bank") == "GUI_PDA_BUTTON_PHOBOS_BANK", "tooltip keys follow the game's own rule");
        bool refused = false;
        try { PdaApps.Register(new PdaApp { Name = "home", Icon = "x" }); } catch (ArgumentException) { refused = true; }
        check(refused, "registering a game app name throws at start-up");
        refused = false;
        try { PdaApps.Register(new PdaApp { Name = "phobos_test" }); } catch (ArgumentException) { refused = true; }
        check(refused, "an app without an icon throws at start-up");
        check(!PdaApps.TryGet("phobos_unregistered", out _), "an unregistered name is left to the game");
    }
}
