using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosShipbreaker;

/// <summary>Native GUIData lifecycle, presentation only. All mutations use IndustryService.</summary>
public sealed class IndustrialPanel : GUIData
{
    internal const string Key = "PhobosIndustrialPanel";
    private ConsoleShell shell = null!;
    private ConsoleBinding? binding;
    private string hostId = "", actorId = "", selected = "", query = "", tab = "overview", result = "", displayedResult = "";
    private int stateFilter;
    private bool compact, detailPage, invalid;
    private float nextRefresh;
    private string roster = "";
    private EquipmentCard[] cards = Array.Empty<EquipmentCard>();
    private readonly HashSet<string> collapsed = new HashSet<string>();
    private readonly Dictionary<string, TMP_Text> rows = new Dictionary<string, TMP_Text>();
    private RectTransform plate = null!, list = null!, details = null!;
    private ScrollRect listScroll = null!, detailScroll = null!;
    private TMP_Text header = null!, readout = null!;
    private CanvasGroup commands = null!;
    private Button pause = null!;
    private Button furnaceStop = null!;
    private TMP_InputField search = null!;

    private bool Central => binding != null;

    internal static bool Open(CondOwner target)
    {
        if (target == null || target.bDestroyed || target.ship == null || (int)target.ship.LoadState < 2 || !target.HasCond("IsInstalled") ||
            CrewSim.goIntUIPanel == null || CrewSim.objInstance.coConnectMode != null || CrewSim.bUILock ||
            GUIInventory.instance?.Selected != null || CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL || CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER) return false;
        ConsoleBinding? context = null;
        string? problem;
        if (IndustrialRules.Console(target.strCODef))
        { if (!ControlAuthority.TryBind(target, out context, out string error)) { Plugin.Log(error); return false; } }
        else
        {
            if (!IndustrialRules.Equipment(target.strCODef)) return false;
            problem = CollectorService.EndpointAccess(target);
            if (problem != null) { Plugin.Log(problem); return false; }
        }
        CrewSim.LowerUI();
        if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu();
        InputManager.ToggleMovementMode(forceOff: true);
        if (CrewSim.guiPDA != null) CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject;
        var panel = root.gameObject.AddComponent<IndustrialPanel>();
        panel.binding = context; panel.hostId = target.ship.strRegID; panel.actorId = CrewSim.GetSelectedCrew().strID;
        panel.selected = context == null ? target.strID : ""; panel.tab = context == null ? "equipment" : "overview";
        panel.Init(target, new Dictionary<string, string>(), Key);
        panel.strFriendlyName = Text.Get("Industry.title"); panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI;
        CrewSim.tplCurrentUI = new Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, target);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows();
        try { panel.Build(); panel.Refresh(); return CrewSim.goUI == root.gameObject; }
        catch (Exception ex) { Plugin.Log(Text.Get("Industry.ui_error", ex.Message)); CrewSim.LowerUI(); return false; }
    }
    private void Build()
    {
        shell=ConsoleShell.Create(transform,Text.Get("Industry.title"),C.Amber);plate=(RectTransform)shell.transform;header=shell.Title;
        shell.EmergencyStop=StopSelected;
        list=shell.List;details=shell.Detail;listScroll=shell.ListScroll;detailScroll=shell.DetailScroll;
        var tabs=Central?new[]{"overview","equipment","routing","maintenance","observations","attention","details"}:new[]{"equipment","routing","maintenance","details"};
        foreach(var key in tabs){var captured=key;C.Button(shell.Navigation,key=="maintenance"||key=="details"?C.Text(key):Text.Get("Industry.tab_"+key),()=>shell.Navigate(()=>
        {tab=captured;if(tab=="observations"||tab=="attention"){selected="";detailPage=false;}if(tab=="overview")detailPage=true;RebuildRows();ShowDetail();}));}
        if(Central)C.Button(shell.Actions,C.Text("back"),()=>shell.Navigate(()=>{detailPage=false;Layout();}));
        pause=C.Button(shell.Actions,Text.Get("Industry.pause_all"),()=>{if(binding!=null){result=IndustryService.PauseAll(binding);RefreshReadout();}});pause.gameObject.SetActive(Central);
        furnaceStop=C.Button(shell.Actions,C.Text("stop"),StopSelected);
        C.Button(shell.Actions,C.Text("crew_settings"),()=>shell.Navigate(()=>Phobos.Ostranauts.Framework.Crew.CrewPanel.Show(CollectorService.Resolve(selected)??COSelf)));
        C.Button(shell.Actions,C.Text("close"),shell.Close);
        search=C.Input(list,Text.Get("Industry.search"),query,text=>{query=text;RebuildRows();});
        if(Central)C.Button(list,Text.Get("Industry.cycle_state"),()=>{stateFilter=(stateFilter+1)%(Enum.GetValues(typeof(EquipmentState)).Length+1);RebuildRows();});
        var rowsRoot=W.Rect(list,"Rows");var group=rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlHeight=group.childControlWidth=true;group.childForceExpandHeight=false;
        Layout();
    }
    private Transform RowsRoot => list.GetChild(list.childCount - 1);
    private void Layout(){compact=shell.IsNarrow;shell.Page(!Central||detailPage);}
    private void Update()
    {
        if (!bActive || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + (float)IndustrialRules.RefreshSeconds;
        try { Refresh(); }
        catch (Exception ex) { invalid = true; if (commands != null) commands.interactable = false; Plugin.Log(Text.Get("Industry.ui_error", ex.Message)); CrewSim.LowerUI(); }
    }
    private string? Access()
    {
        var actor = CrewSim.GetSelectedCrew(); var anchor = COSelf;
        // A moved/uninstalled console or changed operator ends this session permanently.
        if (anchor == null || anchor.bDestroyed || anchor.ship?.strRegID != hostId || !anchor.HasCond("IsInstalled") || actor?.strID != actorId || actor?.ship?.strRegID != hostId)
            invalid = true;
        if (invalid) return Text.Get("Industry.session_ended");
        return binding != null ? ControlAuthority.Check(anchor!, binding) : CollectorService.EndpointAccess(anchor!);
    }
    private void Refresh()
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PanelRefresh);
        var problem = Access();
        if (invalid) { shell.ForceClose(); return; }
        header.text = Central ? Text.Get("Industry.title") : ObjectPresentation.Name(COSelf);
        if(problem!=null)shell.Notice.text=problem;
        else if(result!=displayedResult){shell.Notice.text=result;displayedResult=result;}
        pause.interactable = problem == null;
        var selectedObject=CollectorService.Resolve(selected);
        furnaceStop.gameObject.SetActive(selectedObject!=null&&(FurnaceRules.Machine(selectedObject.strCODef)||ProcessingService.IsProcessor(selectedObject.strCODef)||RoutingRules.IsReceiver(selectedObject.strCODef)||ProcessingService.IsGrabber(selectedObject)||EquipmentProviders.For(selectedObject.strCODef)!=null));
        furnaceStop.interactable = problem == null;
        // Losing console/operator access must stop observation reads as well as commands.
        if (problem != null)
        {
            cards = Array.Empty<EquipmentCard>(); roster = ""; RebuildRows(); ShowDetail();
            return;
        }
        cards = Central ? IndustryService.SnapshotShip(COSelf.ship) : new[] { IndustryService.Snapshot(COSelf) };
        if (Central && IndustryObservations.TryRead(binding!, out var instruments, out _)) cards = cards.Concat(instruments).ToArray();
        // Stable rows while status text changes; rebuild only when membership changes.
        string signature = string.Join("|", cards.Select(c => c.Id + (tab == "attention" ? ":" + c.Attention : "") + (stateFilter != 0 ? ":" + c.State : "") + (query.Length != 0 ? ":" + MatchesQuery(c) : "")));
        if (signature != roster) { roster = signature; RebuildRows(); }
        foreach (var card in cards) if (rows.TryGetValue(card.Id, out var row)) row.text = RowText(card);
        if (readout == null) ShowDetail(); else RefreshReadout();
        if (commands != null) commands.interactable = problem == null && CollectorService.Resolve(selected)!=null;
        Layout();
    }
    private static string RowText(EquipmentCard c) => c.Name + "\n" + (c.Instrument ? c.InstrumentStatus : IndustryService.StateName(c.State)) + (c.Attention ? " — " + Text.Get("Industry.tab_attention") : "");
    private bool MatchesQuery(EquipmentCard c) => query.Length == 0 || c.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
        c.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || c.SearchScope.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    private void RebuildRows()
    {
        if (list == null) return;
        float scroll = listScroll.verticalNormalizedPosition;
        W.Clear(RowsRoot); rows.Clear();
        W.Label(RowsRoot, Text.Get("Industry.filter_state", stateFilter == 0 ? Text.Get("Industry.all") : IndustryService.StateName((EquipmentState)(stateFilter - 1))));
        var visible = cards.Where(c => MatchesQuery(c) &&
            (tab == "attention" || c.Instrument == (tab == "observations")) &&
            (tab != "attention" || c.Attention) && (stateFilter == 0 || (int)c.State == stateFilter - 1));
        foreach (var group in visible.GroupBy(c => c.Group))
        {
            string key = group.Key;
            C.Button(RowsRoot, (collapsed.Contains(key) ? "+ " : "- ") + Text.Get("Industry.group_" + key) + " (" + group.Count() + ")", () => { if (!collapsed.Add(key)) collapsed.Remove(key); RebuildRows(); });
            if (collapsed.Contains(key)) continue;
            foreach (var card in group)
            {
                string id = card.Id;
                var button = C.Button(RowsRoot, RowText(card), () => shell.Navigate(() => { selected = id; result = ""; if (tab == "overview") tab = "equipment"; detailPage = true; ShowDetail(); }),48);
                button.GetComponentInChildren<TMP_Text>().fontSize=16;
                rows[id] = button.GetComponentInChildren<TMP_Text>();
            }
        }
        if (!visible.Any()) W.Label(RowsRoot, Text.Get(tab == "observations" ? "Observations.no_match" : "Industry.none"));
        listScroll.verticalNormalizedPosition = scroll;
    }
    private void RefreshReadout()
    {
        var problem = Access();
        if (problem != null) { readout.text = problem; return; }
        if (tab == "overview" && Central)
            readout.text = Text.Get("Industry.overview", cards.Count(c => !c.Instrument), cards.Count(c => !c.Instrument && c.State == EquipmentState.Running), cards.Count(c => !c.Instrument && c.Attention)) +
                "\n\n" + Text.Get("Observations.overview", cards.Count(c => c.Instrument), cards.Count(c => c.Instrument && c.Attention)) +
                "\n\n" + Text.Get("Industry.scope_help");
        else
        {
            var card = cards.FirstOrDefault(c => c.Id == selected);
            readout.text = card == null ? Text.Get("Industry.select") : card.Name + "\n" + (tab=="details"||card.Instrument?card.Detail+(tab=="details"?"\n\n"+card.Id:""):IndustryService.StateName(card.State)+(card.Summary.Length>0?"\n"+card.Summary:""));
        }
    }
    private void ShowDetail()
    {
        W.Clear(details); commands = null!;
        readout = W.Label(details, ""); RefreshReadout();
        var target = cards.Any(c => c.Id == selected && !c.Instrument) ? CollectorService.Resolve(selected) : null;
        shell.SelectionOrigin=target;
        if (target == null || tab == "overview" && Central) { Layout(); return; }
        var identity=C.Row(details,72);ObjectPresentation.Picture(identity,target,64);C.Label(identity,ObjectPresentation.Location(target));
        if(tab=="details"){Layout();return;}
        var actions = W.Rect(details, "Commands");
        var layout = actions.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = W.Gap;
        layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
        commands = actions.gameObject.AddComponent<CanvasGroup>(); commands.interactable = Access() == null;
        if(tab=="routing"){ShowRouting(target);return;}
        if(tab=="maintenance"){ShowMaintenance(actions,target);Layout();return;}
        var provider = EquipmentProviders.For(target.strCODef);
        if (provider != null)
            foreach (var command in provider.Snapshot(target).Actions)
            {
                var captured=command;
                if(provider is IEquipmentPanelPresentation presentation&&presentation.IsConfiguration(command.Id))
                    C.Button(actions,command.Label,()=>ConfigurationSheet.Choices(shell,captured.Label,"",presentation.ConfigurationStamp(target),new[]{(captured.Id,captured.Label)},
                        (string expected,string chosen,out string reason)=>presentation.ApplyConfiguration(target,binding,expected,chosen,out reason)));
                else Add(actions,command.Id,label:command.Label);
            }
        if (FurnaceRules.Machine(target.strCODef))
        {
            string targetId = target.strID;
            FurnaceInstrumentView.Build(actions, target, (action, value) => {if(action=="automatic"||action=="step-mode")Setting(target,action);else RunInstrument(targetId,action,value);}, shell, binding);
            if (!Central) { Add(actions, "feed"); Add(actions, "products"); }

        }
        if (ProcessingService.IsProcessor(target.strCODef))
        {
            W.Label(actions, Text.Get("Industry.processing"));
            Add(actions, "start"); Add(actions, "pause"); Add(actions, "cancel");
            Add(actions, "watch"); Add(actions, "unwatch");
            C.Button(actions, Phobos.Ostranauts.Framework.Audio.CompletionCues.VolumeLabel, () => { Phobos.Ostranauts.Framework.Audio.CompletionCues.CycleVolume(); ShowDetail(); });
            if (!Central) { Add(actions, "feed"); Add(actions, "products"); }
        }
        if (RoutingRules.IsReceiver(target.strCODef))
        {
            W.Label(actions, Text.Get("Industry.receiving")); Add(actions, "receive"); Add(actions, "pause-receive");
            if (!Central && !ProcessingService.IsProcessor(target.strCODef) && !FurnaceRules.Machine(target.strCODef)) Add(actions, "inventory");
        }
        if (RoutingRules.IsReceiver(target.strCODef) || RoutingRules.IsSender(target.strCODef))
        {
            C.Button(actions, Text.Get("Industry.tab_routing"), () => shell.Navigate(()=>{tab="routing";ShowDetail();}));
        }
        if (IndustrialRules.Group(target.strCODef) == "chute" || IndustrialRules.Group(target.strCODef) == "grabber")
        {
            if (ProcessingService.IsGrabber(target))
            {
                W.Label(actions, Text.Get("Reclamation.controls"));
                foreach(string action in new[]{"reclaim-start","reclaim-resume","reclaim-pause","reclaim-stop"}) Add(actions,action,label:Text.Get("Reclamation.action_"+action));
                W.Label(actions, Text.Get("Capture.controls"));
                string console=CaptureService.Read(target,out var capture)?capture["console"]:"none";
                C.Label(actions,C.Text("mission_target")+": "+(capture==null?C.Text("not_selected"):CrewSim.system.GetShipByRegID(capture["target"])?.publicName??C.Text("missing_selection")));
                C.Field(actions,C.Text("navigation_console"),ObjectPresentation.Name(console),()=>Connection(target,"capture-bind",C.Text("navigation_console")+" · "+(GUIOrbitDraw.CrossHairTarget?.Ship?.publicName??C.Text("not_selected")),"",()=>CaptureService.Consoles(target.ship)),
                    ()=>ObjectPicker.Locate(shell,CollectorService.Resolve(console)),null,CollectorService.Resolve(console)!=null,false);
                foreach (string action in new[] { "capture-start", "capture-stop", "capture-release" })
                    Add(actions, action, label: Text.Get("Capture.action_" + action));
            }
            var processor = ProcessingService.PipelineFor(target);
            if (processor != null)
            {
                string? localProblem = Central ? null : ProcessingService.AccessProblem(processor);
                C.Button(actions, Text.Get("Industry.open_processor"), () => { if (Central) { selected = processor.strID; ShowDetail(); } else Open(processor); }).interactable = localProblem == null;
                if (localProblem != null) W.Label(actions, localProblem);
            }
        }
        if (Central) W.Label(details, Text.Get("Industry.local_only"));
        if (tab == "routing") ShowRouting(target); else Layout();
    }
    private void RunInstrument(string targetId, string action, string? value)
    {
        bool success = IndustryService.Run(binding, targetId, action, value, out result);
        // Refresh first: old cards can otherwise echo a success response or leave
        // stale notices below the live readout when the machine changes later.
        Refresh();
        if (!bActive || invalid) return;
        result = PanelFeedback.Additional(success, result, cards.FirstOrDefault(c => c.Id == targetId)?.Detail ?? "");
        shell.Notice.text=result;
        RefreshReadout();
    }
    private void StopSelected()
    {
        var co=CollectorService.Resolve(selected);if(co==null)return;
        if(ProcessingService.IsGrabber(co)){RunInstrument(selected,"reclaim-stop",null);RunInstrument(selected,"capture-stop",null);}
        else RunInstrument(selected,FurnaceRules.Machine(co.strCODef)?"stop":ProcessingService.IsProcessor(co.strCODef)||EquipmentProviders.For(co.strCODef)!=null?"pause":"pause-receive",null);
    }
    private void Add(Transform parent, string action, string? value = null, string? label = null)
    {
        string targetId = selected;
        var last=parent.childCount>0?parent.GetChild(parent.childCount-1):null;
        var row=last!=null&&last.name=="Command row"&&last.childCount<2?last:C.Row(parent);row.name="Command row";
        C.Button(row, label ?? Text.Get("Industry.action_" + action), () =>
        {
            bool inventory=!Central&&(action=="feed"||action=="products"||action=="inventory");
            void Execute()
            {
                if(inventory)CrewSim.LowerUI();
                bool success = IndustryService.Run(binding, targetId, action, value, out string message);
                result = success ? Text.Get("Industry.success", label ?? Text.Get("Industry.action_" + action)) : Text.Get("Industry.rejected", message);
                if(!inventory){shell.Notice.text=result;RefreshReadout();}
            }
            if(inventory)shell.Navigate(Execute);else Execute();
        });
    }
    private void ShowMaintenance(Transform actions,CondOwner target)
    {
        if(FurnaceRules.Machine(target.strCODef))
        {
            FurnaceInstallationView.Build(actions,target);
            string cooling=FurnaceService.CoolingSelection(target);
            C.Field(actions,C.Text("cooling"),ObjectPresentation.Name(cooling),
                ()=>Connection(target,"pair",C.Text("cooling"),cooling,()=>IndustryService.Discover(target.ship).Where(c=>FurnaceRules.Cooling(c.strCODef)),"unpair"),
                ()=>ObjectPicker.Locate(shell,CollectorService.Resolve(cooling)),()=>Setting(target,"unpair"),CollectorService.Resolve(cooling)!=null,cooling!="none"&&!string.IsNullOrEmpty(cooling));
            foreach(var mode in new[]{"direct","left","right"})C.Button(actions,Text.Get("Furnace.coolant_"+mode),()=>Setting(target,"cooling-"+mode));
            if(!Central)foreach(var service in new[]{"managed","sealed","fill","drain"})
            {var action="coolant-"+service;if(service=="fill"||service=="drain")Add(actions,action,label:Text.Get("Furnace.coolant_"+service));else C.Button(actions,Text.Get("Furnace.coolant_"+service),()=>Setting(target,action));}
        }
        else C.Label(actions,Text.Get("Industry.local_only"));
        if(!Central){if(ProcessingService.IsProcessor(target.strCODef)){Add(actions,"feed");Add(actions,"products");}else if(RoutingRules.IsReceiver(target.strCODef)&&target.objContainer!=null)Add(actions,"inventory");}
    }
    private void Setting(CondOwner target,string action,string? value=null,string? label=null)
    {
        string title=label??Text.Get(action.StartsWith("cooling-",StringComparison.Ordinal)?"Furnace.coolant_"+action.Substring(8):action.StartsWith("coolant-",StringComparison.Ordinal)?"Furnace.coolant_"+action.Substring(8):"Furnace.action_"+action);
        ConfigurationSheet.Choices(shell,title,"",PanelConfiguration.Stamp(target),new[]{(value??action,title)},
            (string expected,string chosen,out string reason)=>PanelConfiguration.Apply(binding,target,expected,action,value==null?null:chosen,out reason));
    }
    private void Connection(CondOwner target,string action,string label,string current,Func<IEnumerable<CondOwner>> candidates,string? unlink=null)
    {
        ConfigurationSheet.Objects(shell,label,current,PanelConfiguration.Stamp(target),candidates,
            (string expected,string value,out string reason)=>PanelConfiguration.Apply(binding,target,expected,value=="none"?unlink??action:action,value=="none"?null:value,out reason),allowClear:unlink!=null);
    }
    private void ShowRouting(CondOwner target)
    {
        var actions=commands.transform;W.Clear(actions);
        if(RoutingRules.IsReceiver(target.strCODef))
        {
            string peer=PanelConfiguration.Peer(target,false);
            C.Field(actions,C.Text("source"),ObjectPresentation.Name(peer),()=>Connection(target,"link-input",C.Text("source"),peer,
                ()=>IndustryService.Discover(target.ship).Where(c=>c!=target&&RoutingRules.CanConnect(c.strCODef,target.strCODef)),"unlink-input"),
                ()=>ObjectPicker.Locate(shell,CollectorService.Resolve(peer)),()=>Setting(target,"unlink-input",label:C.Text("clear")),CollectorService.Resolve(peer)!=null,peer!="none"&&!string.IsNullOrEmpty(peer));
            C.Field(actions,C.Text("filter"),CollectorService.FilterLabel(target),()=>ConfigurationSheet.Choices(shell,C.Text("filter"),"",PanelConfiguration.Stamp(target),RoutingRules.Choices(target.strCODef).Select(f=>(f,Text.Get("Industry.filter_"+f))),
                (string expected,string value,out string reason)=>PanelConfiguration.Apply(binding,target,expected,"filter",value,out reason)));
        }
        if(RoutingRules.IsSender(target.strCODef))Output(false);
        if(ProcessingService.IsReclaimer(target))Output(true);
        void Output(bool metals)
        {
            string peer=PanelConfiguration.Peer(target,true,metals),label=metals?Text.Get("Routing.metals_port"):C.Text("destination"),unlink=metals?"unlink-metals":"unlink-output";
            C.Field(actions,label,ObjectPresentation.Name(peer),()=>Connection(target,"link-output",label,peer,
                ()=>IndustryService.Discover(target.ship).Where(c=>c!=target&&RoutingRules.CanConnect(target.strCODef,RoutingRules.OutputPort(target.strCODef,metals),c.strCODef)),unlink),
                ()=>ObjectPicker.Locate(shell,CollectorService.Resolve(peer)),()=>Setting(target,unlink,label:C.Text("clear")),CollectorService.Resolve(peer)!=null,peer!="none"&&!string.IsNullOrEmpty(peer));
        }
        detailPage=true;Layout();
    }
    public override void SaveAndClose()
    {
        if (!bActive) return; // GUIData and IGUIHarness call each other during native cleanup.
        if (search != null && search.isFocused) CrewSim.EndTyping();
        base.SaveAndClose();
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class IndustrialControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!isCancelIa && (__instance.strName == IndustrialRules.Controls || __instance.strName == IndustrialRules.LocalControls) &&
            __instance.objUs == CrewSim.GetSelectedCrew() && __instance.objThem != null) IndustrialPanel.Open(__instance.objThem);
    }
}

// Exact-key restoration after native fast-forward panels; other panels keep the original path.
[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class IndustrialRestorePatch
{
    private static bool Prefix(string strCOGUIKey, CondOwner coSelf)
    {
        if (strCOGUIKey != IndustrialPanel.Key) return true;
        if (!IndustrialPanel.Open(coSelf) && CrewSim.goUI == null)
        {
            // A vanished/powerless console cannot strand the player in native FFWD restore state.
            CanvasManager.instance.CrewSimNormal();
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        }
        return false;
    }
}
