using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>The About buttons (Framework 0.117.0; owner direction, 6 October 2026): the long explanations live in the
/// game's encyclopedia as Phobos operations articles, and a panel keeps one short line and a button that opens the
/// article. Presentation only: the button hands over to the story service and reports what happened.</summary>
public static class Help
{
    /// <summary>Opens the encyclopedia at an article; null when it did, otherwise why it did not.</summary>
    public static string? Open(string articleId) => Story.StoryLore.Open(articleId);
    /// <summary>An About button whose outcome goes to <paramref name="notice"/>.</summary>
    public static Button About(Transform parent, string articleId, TMP_Text? notice) =>
        C.Button(parent, C.Text("about"), () => { string result = Open(articleId) ?? C.Text("about_opened"); if (notice != null) notice.text = result; });
    /// <summary>The F3 <c>phobosframework help [article]</c> command: the list of articles, or the result of opening one.</summary>
    internal static string Command(string[] words)
    {
        if (words.Length <= 2)
        {
            var lines = new System.Collections.Generic.List<string> { Text.Get("Help.list") };
            foreach (var (id, label) in Story.StoryLore.Articles(Story.StoryLore.OperationsSection)) lines.Add("  " + id + ": " + label);
            return string.Join("\n", lines);
        }
        string article = words[2].StartsWith("operations-", StringComparison.Ordinal) ? words[2] : "operations-" + words[2];
        return Open(article) ?? Text.Get("Help.opened", article);
    }
}
