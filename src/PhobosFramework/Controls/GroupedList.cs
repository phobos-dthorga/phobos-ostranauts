using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>A list in collapsible groups with counts (Framework 0.117.0), as the C1 console draws its equipment:
/// one header button per group that has rows, "+" when folded and "-" when open, then the rows. The caller keeps the
/// folded set, so folds outlive a redraw, and refreshes row text through <see cref="Refresh"/> without rebuilding.
/// The header and signature texts have no game types, so the offline checks run them.</summary>
public sealed class GroupedList
{
    public readonly struct Row
    {
        public readonly string Group, Id, Text;
        public readonly Action Click;
        public Row(string group, string id, string text, Action click) { Group = group; Id = id; Text = text; Click = click; }
    }
    private readonly RectTransform host;
    private readonly ISet<string> folded;
    private readonly Dictionary<string, TMP_Text> labels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> buttons = new(StringComparer.Ordinal);
    public GroupedList(RectTransform host, ISet<string> folded)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.folded = folded ?? throw new ArgumentNullException(nameof(folded));
    }
    public IReadOnlyDictionary<string, TMP_Text> Labels => labels;
    public IReadOnlyDictionary<string, Button> Buttons => buttons;

    public static string HeaderText(bool collapsed, string label, int count) => (collapsed ? "+ " : "- ") + label + " (" + count + ")";
    /// <summary>Which rows sit in which group: when this is unchanged, a redraw is not needed.</summary>
    public static string Signature(IEnumerable<Row> rows) => string.Join("|", rows.Select(r => r.Id + ":" + r.Group));

    /// <summary>Draws the groups in the given order, skipping empty ones. A folded group holding the selected row is
    /// opened for this draw, so a selection is never hidden.</summary>
    public void Render(IReadOnlyList<(string Key, string Label)> groups, IReadOnlyList<Row> rows, string? selectedId, Action rebuild) =>
        Render(groups.Select(g => (g.Key, g.Label, Tone.Neutral)).ToArray(), rows, selectedId, rebuild);
    /// <summary>As above, each group header tinted by its tone (Framework 0.118.0): slate when neutral, as before.</summary>
    public void Render(IReadOnlyList<(string Key, string Label, Tone Tone)> groups, IReadOnlyList<Row> rows, string? selectedId, Action rebuild)
    {
        PanelWidgets.Clear(host); labels.Clear(); buttons.Clear();
        foreach (var group in groups)
        {
            string key = group.Key;
            var members = rows.Where(r => r.Group == key).ToArray();
            if (members.Length == 0) continue;
            bool collapsed = folded.Contains(key) && !(selectedId != null && members.Any(r => r.Id == selectedId));
            var header = C.Button(host, HeaderText(collapsed, group.Label, members.Length), () => { if (!folded.Add(key)) folded.Remove(key); rebuild(); });
            C.Accent(header, Tones.Of(group.Tone));
            if (collapsed) continue;
            foreach (var row in members)
            {
                var r = row;
                var button = C.Button(host, r.Text, r.Click, C.RowHeight);
                var label = button.GetComponentInChildren<TMP_Text>(); label.fontSize = 16;
                labels[r.Id] = label; buttons[r.Id] = button;
                if (r.Id == selectedId) C.Accent(button, C.Green);
            }
        }
    }
    /// <summary>Updates one row's text in place; false when the row is not drawn (folded or gone).</summary>
    public bool Refresh(string id, string text)
    {
        if (!labels.TryGetValue(id, out var label)) return false;
        if (label.text != text) label.text = text;
        return true;
    }
}
