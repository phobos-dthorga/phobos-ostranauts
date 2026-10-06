using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>The Letters window (Framework 0.122.0; owner choice, 6 October 2026): every correspondence the player has
/// begun, open ones first, with the letters in order, the correspondent's face, the goal it is at and, where a letter
/// waits for an answer, the replies. Opened by clicking a story goal in the GOALS list, or by F3 <c>story letters</c>.
/// Presentation only: a reply goes through <see cref="StoryArcs.Answer"/> after a confirmation card.</summary>
public sealed class LettersPanel : GUIData
{
    public const string Key = "PhobosLettersPanel";
    private const string OpenGroup = "open", DoneGroup = "done", AsideGroup = "aside";
    // Finished and set-aside correspondence starts folded; remembered while the game runs, not saved (agent default).
    private static readonly HashSet<string> folded = new(StringComparer.Ordinal) { DoneGroup, AsideGroup };
    private ConsoleShell shell = null!;
    private string selected = "", signature = "";
    private float next;

    /// <summary>Opens the window, at one correspondence when given; false when no game is running or another window
    /// cannot be closed.</summary>
    public static bool Show(string? arcId = null)
    {
        var player = CrewSim.coPlayer;
        if (player == null || CrewSim.goIntUIPanel == null || CrewSim.bUILock) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<LettersPanel>();
        panel.selected = arcId ?? "";
        panel.Init(player, new Dictionary<string, string>(), Key); panel.strFriendlyName = Text.Get("Story.letters_title"); panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, player);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); panel.Build(); return true;
    }

    private void Build()
    {
        shell = ConsoleShell.Create(transform, Text.Get("Story.letters_title"), C.Slate);
        C.Button(shell.Navigation, C.Text("back"), () => { selected = ""; shell.Page(false); Render(); });
        C.Button(shell.Navigation, C.Text("close"), shell.Close);
        Render();
    }

    private static string Signature(IEnumerable<CorrespondenceView> views) =>
        string.Join("|", views.Select(v => v.ArcId + ":" + v.State + ":" + v.Letters.Count + ":" + string.Join(",", v.Replies.Select(r => r.Id + (r.Locked == null ? "" : "!")))));

    private void Render()
    {
        var views = StoryArcs.Correspondence();
        signature = Signature(views);
        float listScroll = shell.ListScroll.verticalNormalizedPosition, detailScroll = shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.List); W.Clear(shell.Detail); W.Clear(shell.Actions);
        if (views.Count == 0) { C.Label(shell.Detail, Text.Get("Story.letters_none")); shell.Page(true); return; }
        if (selected.Length == 0 || views.All(v => v.ArcId != selected)) selected = views[0].ArcId;
        var rows = W.Rect(shell.List, "Rows"); var group = rows.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 6; group.childControlWidth = group.childControlHeight = true; group.childForceExpandHeight = false;
        var list = new GroupedList(rows, folded);
        var rowData = views.Select(v => new GroupedList.Row(v.State == ArcState.Active ? OpenGroup : v.State == ArcState.Done ? DoneGroup : AsideGroup, v.ArcId,
            v.Correspondent + "\n" + v.Title, () => { selected = v.ArcId; shell.Page(true); Render(); })).ToList();
        var groups = new List<(string Key, string Label, Tone Tone)>
        {
            (OpenGroup, Text.Get("Story.letters_group_open"), views.Any(v => v.State == ArcState.Active && v.Replies.Any(r => r.Locked == null)) ? Tone.Attention : Tone.Neutral),
            (DoneGroup, Text.Get("Story.letters_group_done"), Tone.Neutral),
            (AsideGroup, Text.Get("Story.letters_group_aside"), Tone.Neutral)
        };
        list.Render(groups, rowData, selected, Render);
        Detail(views.First(v => v.ArcId == selected));
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition = listScroll; shell.DetailScroll.verticalNormalizedPosition = detailScroll;
    }

    private void Detail(CorrespondenceView view)
    {
        var header = C.Row(shell.Detail, 132);
        if (view.Face != null && DataHandler.LoadPNG(view.Face + ".png", false) is Texture2D face && face.name != "missing.png")
        {
            var frame = W.Rect(header, "Face"); C.Size(frame, 129, 83);
            var image = frame.gameObject.AddComponent<RawImage>(); image.texture = face; image.raycastTarget = false;
        }
        var who = C.Label(header, view.Correspondent + "\n" + Text.Get(view.State == ArcState.Active ? "Story.letters_state_open" : view.State == ArcState.Done ? "Story.letters_state_done" : "Story.letters_state_aside"));
        who.fontSize = 20;
        foreach (var letter in view.Letters)
        {
            string head = letter.Date.Length > 0 ? letter.Date + " · " + letter.From : letter.From;
            var text = C.Label(shell.Detail, head + "\n" + letter.Text);
            if (letter.Reply) text.color = C.Green;
        }
        if (view.GoalTitle != null)
        {
            C.Heading(shell.Detail, Text.Get("Story.letters_now"));
            C.Status(shell.Detail, view.GoalTitle + (string.IsNullOrEmpty(view.GoalDescription) ? "" : "\n" + view.GoalDescription), view.Replies.Count > 0 ? Tone.Attention : Tone.Neutral);
        }
        if (view.Replies.Count == 0) return;
        C.Heading(shell.Detail, Text.Get("Story.letters_reply"));
        foreach (var reply in view.Replies)
        {
            var r = reply;
            var button = C.Button(shell.Detail, r.Locked == null ? r.Label : Text.Get("Story.letters_locked", r.Label, r.Locked), () => Confirm(view, r), C.RowHeight);
            button.interactable = r.Locked == null;
            C.Accent(button, r.Locked == null ? Tone.Good : Tone.Neutral);
        }
    }

    private void Confirm(CorrespondenceView view, ReplyView reply) =>
        ChoiceCard.Show(shell.transform, Text.Get("Story.letters_confirm", view.Correspondent, reply.Label), new[]
        {
            new ChoiceCard.Choice(Text.Get("Story.letters_send"), () => { shell.Notice.text = StoryArcs.Answer(view.ArcId, reply.Id); Render(); }, Tone.Good),
            new ChoiceCard.Choice(Text.Get("Story.letters_not_yet"), () => { })
        });

    private void Update()
    {
        if (!bActive || CrewSim.goUI != gameObject || shell == null || Time.unscaledTime < next) return;
        next = Time.unscaledTime + 2;
        // A new letter, a finished correspondence or a reply that unlocked redraws the window, keeping its scroll.
        if (Signature(StoryArcs.Correspondence()) != signature) Render();
    }
}

/// <summary>Clicking a story goal in the GOALS list opens its correspondence in the Letters window instead of moving
/// the camera to the player (Framework 0.122.0). Every other goal keeps the game's own click.</summary>
[HarmonyPatch(typeof(ObjectivePanel), "FocusObjective", new[] { typeof(CondOwner) })]
internal static class LettersFromGoal
{
    private static bool Prefix(ObjectivePanel __instance)
    {
        var objective = __instance?.Objective;
        if (objective == null || !StoryRules.TryGoal(objective.strCT, out var arc, out _)) return true;
        try { return !LettersPanel.Show(arc); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); return true; }
    }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class LettersPanelRestore
{
    private static bool Prefix(string strCOGUIKey) { if (strCOGUIKey != LettersPanel.Key) return true; LettersPanel.Show(); return false; }
}
