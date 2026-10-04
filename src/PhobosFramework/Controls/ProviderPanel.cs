using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>What a mod supplies to show its equipment in the shared <see cref="ProviderPanel"/>: the native GUI key
/// its Control Panel interaction opens, the provider behind it, and the mod's own access, resolution and texts.</summary>
public sealed class ProviderPanelSpec
{
    /// <summary>The native GUI key (kept when a mod moves onto the host, so saved open panels still resolve).</summary>
    public string Key { get; set; } = "";
    public IEquipmentProvider Provider { get; set; } = null!;
    /// <summary>Whether this panel shows the object at all (the mod's own equipment).</summary>
    public Func<CondOwner, bool> Handles { get; set; } = _ => false;
    /// <summary>Why the selected crew member may not use it now, or null.</summary>
    public Func<CondOwner, string?> Access { get; set; } = _ => null;
    public Func<string, CondOwner?> Resolve { get; set; } = _ => null;
    /// <summary>The mod's own words for the page names ("operation", "connections", "details") and for "help" and
    /// "no_connections".</summary>
    public Func<string, string> Text { get; set; } = key => key;
    public Action<string> Log { get; set; } = _ => { };
    /// <summary>The command the Stop button and the emergency stop send.</summary>
    public string StopAction { get; set; } = "pause";
}

/// <summary>The shared equipment Control Panel over the console shell (Framework 0.56.0, lifted from Manufacturing's
/// panel so other mods reuse it): live status, the provider's actions, and its connections as configuration drafts.
/// Presentation only; every change goes through the provider. It redraws its page after a command and after a
/// configuration sheet applies a change.</summary>
public sealed class ProviderPanel : GUIData
{
    private static readonly Dictionary<string, ProviderPanelSpec> specs = new(StringComparer.Ordinal);
    public static void Register(ProviderPanelSpec spec)
    {
        if (spec == null || string.IsNullOrEmpty(spec.Key) || spec.Provider == null) throw new ArgumentException("A provider panel needs a key and a provider.");
        specs[spec.Key] = spec;
    }
    internal static bool Handles(string key) => specs.ContainsKey(key);

    private ProviderPanelSpec spec = null!;
    private string id = "", actorId = "", result = "", tab = "operation";
    private TMP_Text readout = null!, live = null!;
    private ConsoleShell shell = null!;
    private readonly PresentationRefresh refresh = new(.5);
    private IEquipmentPanelFields? Fields => spec.Provider as IEquipmentPanelFields;

    public static bool Show(string key, CondOwner co)
    {
        if (!specs.TryGetValue(key, out var spec)) return false;
        if (!spec.Handles(co) || spec.Access(co) != null || CrewSim.goIntUIPanel == null || CrewSim.bUILock ||
            CrewSim.objInstance.coConnectMode != null || GUIInventory.instance?.Selected != null ||
            CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL || CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu(); InputManager.ToggleMovementMode(forceOff: true);
        if (CrewSim.guiPDA != null) CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<ProviderPanel>();
        panel.spec = spec; panel.id = co.strID; panel.actorId = CrewSim.GetSelectedCrew().strID;
        panel.Init(co, new Dictionary<string, string>(), key); panel.strFriendlyName = co.strNameFriendly; panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>(key, co);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows();
        try { panel.Build(co); return true; } catch (Exception e) { spec.Log(e.ToString()); CrewSim.LowerUI(); return false; }
    }
    private void Build(CondOwner co)
    {
        shell = ConsoleShell.Create(transform, co.strNameFriendly, C.Green);
        shell.SelectionOrigin = co;
        shell.EmergencyStop = () => Execute(co, spec.StopAction);
        // An applied setting or a finished action can change link names, offered actions and field values: redraw.
        shell.Changed = () => Page(co);
        foreach (var page in new[] { "operation", "connections", "details" })
        { var name = page; C.Button(shell.Navigation, spec.Text(name), () => shell.Navigate(() => { tab = name; Page(co); })); }
        // Any mod's crew orders for this object (Framework 0.58.0): Agriculture's fill order on a water tank, the L2's
        // bottle order. The crew panel is the shared one.
        if (Crew.CrewWork.Provider(co) != null) C.Button(shell.Navigation, C.Text("crew_settings"), () => shell.Navigate(() => Crew.CrewPanel.Show(co)));
        C.Button(shell.Navigation, C.Text("close"), shell.Close);
        ObjectPresentation.Picture(shell.List, co, 120); C.Label(shell.List, ObjectPresentation.Location(co));
        live = C.Label(shell.List, "");
        Page(co);
    }
    private void Page(CondOwner co)
    {
        W.Clear(shell.Detail); W.Clear(shell.Actions); shell.Page(true); readout = C.Label(shell.Detail, "");
        if (tab == "details")
        {
            C.Label(shell.Detail, spec.Text("help"));
            // What it is joined to through each line it takes part in, by pipe or by touching (Framework 0.69.0).
            string joined = LinkChoices.NetworkSummary(co);
            if (joined.Length > 0) C.Label(shell.Detail, joined);
            C.Label(shell.Detail, co.strCODef + "\n" + co.strID);
        }
        else if (tab == "connections")
        {
            var fields = Fields?.Fields(co).ToArray() ?? Array.Empty<EquipmentField>();
            foreach (var field in fields)
            {
                var f = field;
                C.Button(shell.Detail, f.Label + ": " + f.Value, () => ConfigurationSheet.Choices(shell, f.Label, f.Current, Fields!.ConfigurationStamp(co), f.Choices,
                    (string expected, string value, out string reason) =>
                    {
                        if (Fields!.ApplyConfiguration(co, null, expected, value, out reason)) return true;
                        // A choice its own provider does not list can never be saved; say so instead of calling it stale.
                        if (!Fields.IsConfiguration(value)) { reason = ConsoleText.Get("unsupported_choice"); FrameworkLifecycle.Log("Panel choice '" + value + "' on " + co.strCODef + " is not listed by its provider's IsConfiguration."); }
                        return false;
                    }, f.Note));
            }
            if (fields.Length == 0) C.Label(shell.Detail, spec.Text("no_connections"));
        }
        else
        {
            C.Heading(shell.Detail, C.Text("actions"));
            var row = C.Row(shell.Detail);
            foreach (var action in spec.Provider.Snapshot(co).Actions) { var a = action; C.Button(row, a.Label, () => Execute(co, a.Id)); }
        }
        C.Button(shell.Actions, C.Text("stop"), () => Execute(co, spec.StopAction));
        C.Button(shell.Actions, C.Text("details"), () => { tab = "details"; Page(co); }); C.Button(shell.Actions, C.Text("close"), shell.Close); Refresh(co);
    }
    private void Execute(CondOwner co, string action)
    {
        bool success = spec.Provider.Command(co, null, action, out result);
        result = PanelFeedback.Additional(success, result, spec.Provider.Snapshot(co).Activity.Detail);
        Page(co);
        shell.Notice.text = result;
    }
    private void Update()
    {
        if (!bActive || spec == null) return;
        refresh.Bind(COSelf, CrewSim.GetSelectedCrew(), null, Localization.Translations.Language);
        if (!refresh.Due(Time.unscaledTime)) return;
        var co = spec.Resolve(id);
        if (co == null || CrewSim.GetSelectedCrew()?.strID != actorId || spec.Access(co) != null) { shell.ForceClose(); return; }
        Refresh(co);
    }
    private void Refresh(CondOwner co)
    {
        var snapshot = spec.Provider.Snapshot(co);
        live.text = C.Text("state_" + snapshot.Activity.State);
        readout.text = snapshot.Activity.Detail;
    }
    public override void SaveAndClose() { if (bActive) base.SaveAndClose(); }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class ProviderPanelRestorePatch
{
    private static bool Prefix(string strCOGUIKey, CondOwner coSelf)
    {
        if (strCOGUIKey == null || !ProviderPanel.Handles(strCOGUIKey)) return true;
        if (!ProviderPanel.Show(strCOGUIKey, coSelf) && CrewSim.goUI == null) { CanvasManager.instance.CrewSimNormal(); if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true; }
        return false;
    }
}
