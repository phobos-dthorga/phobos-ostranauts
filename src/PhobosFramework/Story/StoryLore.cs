using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Lore outside play (Framework 0.108.0): story tips on loading screens and story articles in the game's
/// encyclopedia. Neither has a player at hand, so their entries may require only installed mods. The game picks a tip
/// at random and saves nothing about it; the encyclopedia is built from <c>DataHandler.dictInfoNodes</c> and nothing
/// about reading it is saved either.</summary>
internal static class StoryLore
{
    /// <summary>Every encyclopedia node of ours is named with this prefix; the parent of an article is always its
    /// section's node, so the game's own parent lookup never meets an unknown name.</summary>
    public const string NodePrefix = "PhobosStory.";
    private static readonly List<JsonInfoNode> nodes = new();
    internal static int ShownArticles { get; private set; }

    /// <summary>A node of the encyclopedia, as data.</summary>
    internal readonly struct Node
    {
        public readonly string Name, Label, Title, Body;
        public readonly string? Parent, Image;
        public Node(string name, string label, string? parent, string title, string body, string? image = null)
        { Name = name; Label = label; Parent = parent; Title = title; Body = body; Image = image; }
    }

    /// <summary>The sections and articles to show: every article whose mods (and its section's) are installed, under its
    /// section, and only sections with at least one such article.</summary>
    internal static List<Node> Nodes(StoryLibrary library, Func<string, bool> modInstalled, Func<string, string, string, string> words)
    {
        bool Met(StoryRequires? r) => r == null || r.mods.All(modInstalled);
        var articles = library.Articles.Values.Where(a => Met(a.Value.requires) && library.Sections.TryGetValue(a.Value.section, out var s) && Met(s.Value.requires)).ToList();
        var result = new List<Node>();
        foreach (var section in library.Sections.Values.Where(s => articles.Any(a => a.Value.section == s.Id)))
            result.Add(new Node(NodePrefix + section.Id, words(section.Owner, section.Id + ".label", section.Value.label), null,
                words(section.Owner, section.Id + ".title", section.Value.title), section.Value.body == null ? "" : words(section.Owner, section.Id + ".body", section.Value.body), section.Value.image));
        foreach (var article in articles)
            result.Add(new Node(NodePrefix + article.Id, words(article.Owner, article.Id + ".label", article.Value.label), NodePrefix + article.Value.section,
                words(article.Owner, article.Id + ".title", article.Value.title), words(article.Owner, article.Id + ".body", article.Value.body), article.Value.image));
        return result;
    }

    /// <summary>Prepares the encyclopedia nodes from a new library and puts them in the game's table.</summary>
    internal static void Prepare(StoryLibrary library)
    {
        nodes.Clear();
        var prepared = Nodes(library, StoryContent.ModInstalled, StoryContent.Words);
        ShownArticles = prepared.Count(n => n.Parent != null);
        nodes.AddRange(prepared.Select(n => new JsonInfoNode { strName = n.Name, strNodeLabel = n.Label, strNodeParent = n.Parent, strArticleTitle = n.Title, strArticleBody = n.Body, strImage = n.Image }));
        Apply();
    }

    /// <summary>Replaces our nodes in the game's table with the prepared ones; the game's own nodes are untouched.
    /// Runs again just before the encyclopedia builds its tree, in case the game builds it before our packs load.</summary>
    internal static void Apply()
    {
        var table = DataHandler.dictInfoNodes;
        if (table == null) return;
        foreach (var name in table.Keys.Where(k => k.StartsWith(NodePrefix, StringComparison.Ordinal)).ToList()) table.Remove(name);
        foreach (var node in nodes) table[node.strName] = node;
    }

    /// <summary>A share of loading-screen tips (<c>tipShare</c>) comes from story tips whose mods are installed.</summary>
    internal static JsonTip Tip(JsonTip original)
    {
        var library = StoryContent.Library;
        if (library.Tips.Count == 0 || !(StoryArcs.Roll() < library.Settings.tipShare)) return original;
        var pool = library.Tips.Values.Where(t => t.Value.requires == null || t.Value.requires.mods.All(StoryContent.ModInstalled)).Select(t => (t.Id, t.Value.weight)).ToList();
        string? id = StoryRules.Pick(pool, StoryArcs.Roll());
        if (id == null || !library.Tips.TryGetValue(id, out var tip)) return original;
        // The game's own tips start with a line break; ours look the same.
        return new JsonTip { strName = StoryRules.TestPrefix + id, strCategory = original?.strCategory ?? "lore", strBody = "\n" + StoryContent.Words(tip.Owner, id + ".text", tip.Value.text) };
    }
}

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetTip))]
internal static class StoryTipPatch
{
    private static void Postfix(ref JsonTip __result)
    {
        try { __result = StoryLore.Tip(__result); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}

[HarmonyPatch(typeof(Info), nameof(Info.BuildHierarchyFromJSON))]
internal static class StoryEncyclopediaPatch
{
    private static void Prefix()
    {
        try { StoryLore.Apply(); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}
