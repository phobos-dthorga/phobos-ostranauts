using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Pda;

/// <summary>One app on the wrist PDA's home screen (Framework 0.126.0). The game draws its icon from
/// <c>DataHandler.dictPDAAppIcons</c> and its tooltip from the strings <c>GUI_PDA_BUTTON_&lt;NAME&gt;_TITLE</c> and
/// <c>GUI_PDA_BUTTON_&lt;NAME&gt;</c>; Framework writes all three from the registering mod's catalogue each time content
/// loads, so the label follows the player's language and nothing is left on the screen when the mod is removed.</summary>
public sealed class PdaApp
{
    /// <summary>The app's stable name: lowercase letters, digits and underscores, starting with the mod's own prefix
    /// (for example <c>phobos_bank</c>). Never one of the game's own app names.</summary>
    public string Name = "";
    /// <summary>The short label under the icon, in capitals like the game's own (ITEMS, GOALS).</summary>
    public Func<string> Label = () => "";
    /// <summary>The tooltip's title and its one-line explanation.</summary>
    public Func<string> Title = () => "", Tooltip = () => "";
    /// <summary>The icon's image path under the mod's <c>images/</c> folder, without <c>.png</c>. The game tints it, so
    /// draw it as the game's own icons are: a white disc with a black glyph, 256 pixels square.</summary>
    public string Icon = "";
    /// <summary>Opens the app once the PDA has closed. Returns null when it opened, or the reason it did not, which
    /// Framework writes to the player's log (no silent refusal).</summary>
    public Func<string?> Open = () => null;
}

/// <summary>Adds apps of our own to the PDA (Framework 0.126.0; design: <c>docs/development/pda-apps-and-banking-research.md</c>).
/// The game's apps are a fixed switch in <c>GUIPDA.OpenApp</c>; one prefix there opens a registered app instead and
/// leaves every other name to the game. Like the game's Roster and Duties apps, ours leave the PDA and raise a full
/// panel. Register in the plugin's Awake, or during ContentLoading once the mod knows its package is enabled.</summary>
public static class PdaApps
{
    /// <summary>The game's own app names (1.0.1.5): never taken over.</summary>
    public static readonly IReadOnlyCollection<string> NativeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "home", "zones", "goals", "gigs", "bounties", "ferry", "files", "navmap", "navlink", "socials", "tasks", "roster",
        "vote", "exit", "power", "duties", "build", "orders", "inventory", "viz", "notes", "timer", "standings"
    };
    /// <summary>The game splits an app name at its first colon into the app and a page.</summary>
    public const char PageSeparator = ':';
    public const int MaxNameLength = 48;
    public const string TooltipPrefix = "GUI_PDA_BUTTON_", TitleSuffix = "_TITLE";

    private static readonly Dictionary<string, PdaApp> apps = new(StringComparer.Ordinal);
    private static bool listening;

    public static IReadOnlyCollection<string> Names => apps.Keys;

    /// <summary>Why a name cannot be used, or null when it can. Pure, for the offline checks.</summary>
    public static string? NameProblem(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "an app needs a name";
        if (name!.Length > MaxNameLength) return "app name '" + name + "' is longer than " + MaxNameLength + " characters";
        if (name.Any(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) || !(name[0] >= 'a' && name[0] <= 'z'))
            return "app name '" + name + "' must start with a lowercase letter and use only lowercase letters, digits and underscores";
        if (NativeNames.Contains(name)) return "app name '" + name + "' belongs to the game";
        return null;
    }

    /// <summary>The app part of a name the game was asked to open (<c>name:page</c> opens <c>name</c>).</summary>
    public static string AppPart(string? requested)
    {
        if (string.IsNullOrEmpty(requested)) return "";
        int colon = requested!.IndexOf(PageSeparator);
        return colon < 0 ? requested : requested.Substring(0, colon);
    }

    public static string TitleKey(string name) => TooltipPrefix + name.ToUpperInvariant() + TitleSuffix;
    public static string TooltipKey(string name) => TooltipPrefix + name.ToUpperInvariant();

    /// <summary>Adds or replaces an app. Throws for a bad name or a missing icon or opener, so a mistake shows at start-up.</summary>
    public static void Register(PdaApp app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));
        string? problem = NameProblem(app.Name);
        if (problem != null) throw new ArgumentException(problem, nameof(app));
        if (string.IsNullOrEmpty(app.Icon)) throw new ArgumentException("app '" + app.Name + "' needs an icon", nameof(app));
        if (app.Open == null || app.Label == null || app.Title == null || app.Tooltip == null) throw new ArgumentException("app '" + app.Name + "' needs a label, title, tooltip and opener", nameof(app));
        apps[app.Name] = app;
        if (listening) return;
        listening = true;
        FrameworkLifecycle.ContentLoaded += Publish;
    }

    public static bool TryGet(string? requested, out PdaApp app)
    {
        app = null!;
        return apps.TryGetValue(AppPart(requested), out app!) && app != null;
    }

    /// <summary>Writes each app's icon entry and tooltip strings into the game's tables. Runs after every content load,
    /// after the game has read the mods' own data, so a data file of the same name cannot hide it.</summary>
    internal static void Publish()
    {
        if (DataHandler.dictPDAAppIcons == null || DataHandler.dictStrings == null) return;
        foreach (var app in apps.Values)
        {
            try
            {
                DataHandler.dictPDAAppIcons[app.Name] = new JsonPDAAppIcon { strName = app.Name, strFriendlyName = app.Label(), strIcon = app.Icon, bHidden = false };
                DataHandler.dictStrings[TitleKey(app.Name)] = app.Title();
                DataHandler.dictStrings[TooltipKey(app.Name)] = app.Tooltip();
            }
            catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("PdaApps.publish_failed", app.Name, ex.Message)); }
        }
    }

    /// <summary>Closes the PDA as the game's own panel apps do, then opens the app; a refusal goes to the player's log.</summary>
    internal static void Launch(PdaApp app)
    {
        if (GUIPDA.instance != null) GUIPDA.instance.State = GUIPDA.UIState.Closed;
        string? refusal = app.Open();
        if (string.IsNullOrEmpty(refusal)) return;
        var crew = CrewSim.GetSelectedCrew() ?? CrewSim.coPlayer;
        crew?.LogMessage(refusal, "Bad", crew.strID);
    }
}

/// <summary>Opens a registered app; every other name goes to the game's own switch untouched.</summary>
[HarmonyPatch(typeof(GUIPDA), nameof(GUIPDA.OpenApp), new[] { typeof(string) })]
internal static class PdaAppOpen
{
    private static bool Prefix(string appName)
    {
        if (!PdaApps.TryGet(appName, out var app)) return true;
        try { PdaApps.Launch(app); }
        catch (Exception ex)
        {
            FrameworkLifecycle.Log(Text.Get("PdaApps.open_failed", app.Name, ex));
            var crew = CrewSim.GetSelectedCrew() ?? CrewSim.coPlayer;
            crew?.LogMessage(Text.Get("PdaApps.open_failed_player", app.Label()), "Bad", crew.strID);
        }
        return false;
    }
}
