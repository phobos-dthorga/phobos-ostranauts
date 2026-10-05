using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Localization;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Story packs (Framework 0.107.0). Framework ships its own pack (the settings, and the folder where players
/// and add-ons may put story files of their own); each content mod registers its pack once, in Awake. All packs are
/// reread on every game load, after every mod's items exist, then merged into one <see cref="StoryLibrary"/>.</summary>
public static class StoryContent
{
    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.story.json";
    private static readonly List<DataPackSource> sources = new();
    private static readonly List<string> packLines = new();
    public static StoryLibrary Library { get; private set; } = StoryLibrary.Empty;
    internal static DataPackSource FrameworkSource => new(FrameworkInfo.PluginId, ModFolder, StorySchema.Name, typeof(StoryContent).Assembly, Resource);

    /// <summary>A mod's story pack: <c>framework/story.json</c> in its folder, embedded in its assembly. Call once in
    /// Awake; players then add files under <c>BepInEx/config/&lt;ModFolder&gt;/story</c>.</summary>
    public static void Register(DataPackSource source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.Schema != StorySchema.Name) throw new ArgumentException(Text.Get("Story.wrong_schema", source.Schema));
        if (source.ModFolder == ModFolder || sources.Any(s => s.Owner == source.Owner && s.ModFolder == source.ModFolder)) return;
        sources.Add(source);
    }

    /// <summary>Loads and merges every pack, then publishes the goal tests. Runs when content has loaded.</summary>
    internal static void Load()
    {
        packLines.Clear();
        var packs = new List<(string Owner, StoryPack Pack)>();
        StorySettings? settings = null;
        foreach (var (source, framework) in new[] { (FrameworkSource, true) }.Concat(sources.Select(s => (s, false))))
        {
            try
            {
                var pack = DataPacks.Load<StoryPack>(source, p => StorySchema.Validate(p, framework));
                if (framework) settings = pack.settings;
                packs.Add((source.Owner, pack));
                packLines.Add(Text.Get("Story.pack_line", source.ModFolder, pack.broadcasts.Count, pack.adverts.Count, pack.arcs.Count));
            }
            catch (Exception ex)
            {
                packLines.Add(Text.Get("Story.pack_failed", source.ModFolder, ex.Message));
                FrameworkLifecycle.Log(Text.Get("Story.pack_failed", source.ModFolder, ex.Message));
            }
        }
        Library = StoryLibrary.Build(packs, settings, ModInstalled,
            id => DataHandler.dictCOs != null && DataHandler.dictCOs.ContainsKey(id), c => DataHandler.dictConds != null && DataHandler.dictConds.ContainsKey(c));
        foreach (var problem in Library.Problems) FrameworkLifecycle.Log(problem);
        try
        {
            var d = new NativeDefinitions();
            StoryArcs.AddDefinitions(d, Library);
            d.Publish();
        }
        catch (Exception ex)
        {
            FrameworkLifecycle.Log(Text.Get("Story.definitions_failed", ex.Message));
            Library = StoryLibrary.Empty;
        }
        StoryArcs.LibraryChanged();
    }

    /// <summary>A mod named in <c>requires.mods</c>: a Phobos mod by folder name, or any BepInEx plugin id.</summary>
    public static bool ModInstalled(string mod) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(PluginId(mod));
    public static string PluginId(string mod) =>
        mod.IndexOf('.') < 0 && mod.StartsWith("Phobos", StringComparison.Ordinal) ? "phobosgekko.ostranauts." + mod.Substring("Phobos".Length).ToLowerInvariant() : mod;

    /// <summary>Item definition ids whose name contains the words, for authors writing tests and rewards.</summary>
    internal static string Items(string words)
    {
        if (DataHandler.dictCOs == null) return Text.Get("Story.not_in_game");
        var found = DataHandler.dictCOs.Values.Where(co => co != null && !string.IsNullOrEmpty(co.strNameFriendly) &&
                (co.strNameFriendly.IndexOf(words, StringComparison.OrdinalIgnoreCase) >= 0 || co.strName.IndexOf(words, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(co => co.strName, StringComparer.Ordinal).ToList();
        var lines = new List<string> { Text.Get("Story.items_found", found.Count, words) };
        lines.AddRange(found.Take(MaxItemsListed).Select(co => "  " + co.strName + "  " + co.strNameFriendly));
        if (found.Count > MaxItemsListed) lines.Add(Text.Get("Story.items_more", found.Count - MaxItemsListed));
        return string.Join("\n", lines);
    }
    private const int MaxItemsListed = 25;

    /// <summary>A text from a pack, replaced by the owner's translation of <c>Story.&lt;key&gt;</c> when it has one.</summary>
    internal static string Words(string owner, string key, string inline) => Translations.Get(owner, "Story." + key, inline);

    /// <summary>The F3 report: packs, problems, then the player's arcs.</summary>
    internal static string Command(string[] words)
    {
        string sub = words.Length > 2 ? words[2].ToLowerInvariant() : "";
        string? id = words.Length > 3 ? words[3] : null;
        switch (sub)
        {
            case "" when words.Length == 2:
                var lines = new List<string> { Text.Get("Story.title", Library.Broadcasts.Count, Library.Adverts.Count, Library.Arcs.Count) };
                lines.AddRange(packLines.Select(l => "  " + l));
                lines.AddRange(Library.Problems.Select(p => "  " + p));
                lines.Add(StoryArcs.Describe());
                return string.Join("\n", lines);
            case "start" when id != null && words.Length == 4: return StoryArcs.StartCommand(id);
            case "reset" when id != null && words.Length == 4: return StoryArcs.ResetCommand(id);
            case "news" when id != null && words.Length == 4: return StoryArcs.NewsCommand(id);
            case "check" when words.Length == 3: StoryArcs.Check(); return Text.Get("Story.checked");
            case "items" when words.Length >= 4: return Items(string.Join(" ", words.Skip(3)));
            default: return Text.Get("Story.help");
        }
    }
}
