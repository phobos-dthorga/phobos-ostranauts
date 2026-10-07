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
                packLines.Add(Text.Get("Story.pack_line", source.ModFolder, pack.broadcasts.Count, pack.adverts.Count, pack.arcs.Count) +
                    (pack.threads.Count + pack.places.Count + pack.people.Count > 0 ? " " + Text.Get("Story.pack_world", pack.threads.Count, pack.places.Count, pack.people.Count) : ""));
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
        // Encyclopedia sections and articles (0.108.0), published once the library is known.
        try { StoryLore.Prepare(Library); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.definitions_failed", ex.Message)); }
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

    /// <summary>One variant of a text that may have several (Framework 0.132.0), replaced by the owner's translation of
    /// its own key (<c>Story.&lt;key&gt;</c> for the first, <c>Story.&lt;key&gt;.2</c> and on for the others).</summary>
    internal static string Words(string owner, string key, TextVariants text, int variant)
    {
        int index = text.Count == 0 ? 0 : Math.Max(0, Math.Min(variant, text.Count - 1));
        return Translations.Get(owner, "Story." + TextVariants.Key(key, index), text.At(index));
    }

    /// <summary>The F3 report: packs, problems, then the player's arcs. The commands that change the save outside ordinary
    /// play (start, try, reset, news, file, flag, standing and check) are test commands (owner rule, 7 October 2026;
    /// Framework 0.128.1): they need the game's own unlockdebug and confirm at the end, through
    /// <see cref="Diagnostics.DebugCommands"/>. Readouts, the Letters window and replies stay open.</summary>
    internal static string Command(string[] words)
    {
        words = Controls.Confirmations.TakeWord(words, out bool confirmed);
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
            case "start" when id != null && words.Length == 4: return Test(Text.Get("Story.test_start", id), confirmed, () => StoryArcs.StartCommand(id));
            // Framework 0.127.0: start an arc the way another mod would, honouring its requirements and place. Only the F3
            // form is a test command; StoryArcs.TryBegin, which other mods call, is not gated.
            case "try" when id != null && words.Length == 4: return Test(Text.Get("Story.test_try", id), confirmed, () => StoryArcs.TryCommand(id));
            case "reset" when id != null && words.Length == 4: return Test(Text.Get("Story.test_reset", id), confirmed, () => StoryArcs.ResetCommand(id));
            case "news" when id != null && words.Length == 4: return Test(Text.Get("Story.test_news", id), confirmed, () => StoryArcs.NewsCommand(id));
            // An extra check is an extra chance for waiting arcs to start, beyond the normal pace.
            case "check" when words.Length == 3: return Test(Text.Get("Story.test_check"), confirmed, () => { StoryArcs.Check(); return Text.Get("Story.checked"); });
            case "items" when words.Length >= 4: return Items(string.Join(" ", words.Skip(3)));
            case "chatter" when words.Length == 3: return StoryChatter.Describe();
            case "file" when id != null && words.Length == 4: return Test(Text.Get("Story.test_file", id), confirmed, () => StoryArcs.FileCommand(id));
            case "chatter" when id != null && words.Length == 4: return StoryChatter.Force(id);
            // Grounding (Framework 0.114.0): where the player is, a thread's members, flags, and the places and people known.
            case "where" when words.Length == 3: return StoryArcs.WhereCommand();
            case "thread" when id != null && words.Length == 4: return StoryArcs.ThreadCommand(id);
            case "flag" when id != null && (words.Length == 4 || words.Length == 5 && words[4] == "clear"):
                return Test(Text.Get(words.Length == 5 ? "Story.test_flag_clear" : "Story.test_flag", id), confirmed, () => StoryArcs.FlagCommand(id, words.Length == 5));
            case "places" when words.Length == 3: return StoryArcs.PlacesCommand();
            case "people" when words.Length == 3: return StoryArcs.PeopleCommand();
            case "standing" when words.Length == 5: return Test(Text.Get("Story.test_standing", words[3], words[4]), confirmed, () => StoryArcs.StandingCommand(words[3], words[4]));
            // Letters and replies (Framework 0.122.0): the same service the Letters window's buttons use.
            case "answer" when words.Length == 5: return StoryArcs.Answer(words[3], words[4]);
            case "letters" when words.Length <= 4: return LettersPanel.Show(id) ? Text.Get("Story.letters_opened") : Text.Get("Story.letters_unavailable");
            // Framework 0.132.0, read-only: every variant of an entry's lines, with the key that translates each.
            case "variants" when id != null && words.Length == 4: return Variants(id);
            default: return Text.Get("Story.help");
        }
    }

    /// <summary>F3 (Framework 0.132.0): every variant of a news item (its text and mention), an advert, a small-talk line or
    /// every letter of an arc, each with its translation key, for writers checking their work.</summary>
    internal static string Variants(string id)
    {
        var lines = new List<string>();
        void Add(string owner, string key, TextVariants? text)
        {
            if (text == null) return;
            for (int i = 0; i < text.Count; i++) lines.Add("  " + Text.Get("Story.variant_line", "Story." + TextVariants.Key(key, i), Words(owner, key, text, i)));
        }
        if (Library.Broadcasts.TryGetValue(id, out var b)) { Add(b.Owner, id + ".text", b.Value.text); Add(b.Owner, id + ".mention", b.Value.mention); }
        else if (Library.Adverts.TryGetValue(id, out var a)) Add(a.Owner, id + ".text", a.Value.text);
        else if (Library.Chatter.TryGetValue(id, out var c)) Add(c.Owner, id + ".line", c.Value.line);
        else if (Library.Arcs.TryGetValue(id, out var arc))
            foreach (var step in arc.Value.steps)
            {
                string stepKey = id + "." + step.id;
                Add(arc.Owner, stepKey + ".message", step.delivery?.message?.text);
                Add(arc.Owner, stepKey + ".done", step.onComplete?.message?.text);
                for (int i = 0; i < (step.branches?.Count ?? 0); i++) Add(arc.Owner, stepKey + ".b" + i + ".done", step.branches![i].onComplete?.message?.text);
                foreach (var choice in step.choices ?? new List<StoryChoice>()) Add(arc.Owner, stepKey + ".choice." + choice.id + ".done", choice.onComplete?.message?.text);
            }
        else return Text.Get("Story.variants_unknown", id);
        return Text.Get("Story.variants_title", id, lines.Count) + (lines.Count > 0 ? "\n" + string.Join("\n", lines) : "");
    }

    /// <summary>Runs a story test command through the gate, then marks the save as test-changed.</summary>
    private static string Test(string what, bool confirmed, Func<string> run)
    {
        if (!Diagnostics.DebugCommands.Gate(Text.Get("Story.mod"), what, confirmed, out string message)) return message;
        string result = run();
        Diagnostics.DebugCommands.Record(Text.Owner, what);
        return result + "\n" + Diagnostics.DebugCommands.Done(what);
    }
}
