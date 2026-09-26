using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework.Controls;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosAgriculture;

// Native panel lifecycle and shared widgets. Presentation delegates all mutations.
public sealed class Panel : GUIData
{
    internal const string Key = "PhobosAgriculturePanel";
    private string id = "", actorId = "", result = "";
    private TMP_Text readout = null!;
    private RawImage portrait = null!;
    private string portraitKey = "", tab = "operation";
    private ConsoleShell shell = null!;
    private TMP_Text live = null!;
    private float nextRefresh;
    internal static bool Show(CondOwner co)
    {
        if (!(Definitions.Machine(co) || RecyclerCapture.IsRecycler(co)) || Service.Access(co) != null || CrewSim.goIntUIPanel == null || CrewSim.bUILock ||
            CrewSim.objInstance.coConnectMode != null || GUIInventory.instance?.Selected != null ||
            CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL || CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu(); InputManager.ToggleMovementMode(forceOff: true);
        if (CrewSim.guiPDA != null) CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<Panel>();
        panel.id = co.strID; panel.actorId = CrewSim.GetSelectedCrew().strID;
        panel.Init(co, new Dictionary<string, string>(), Key); panel.strFriendlyName = co.strNameFriendly; panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, co);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows();
        try { panel.Build(co); return true; } catch (Exception e) { Plugin.Log(e.ToString()); CrewSim.LowerUI(); return false; }
    }
    private void Build(CondOwner co)
    {
        shell=ConsoleShell.Create(transform,co.strNameFriendly,C.Green);
        shell.SelectionOrigin=co;
        shell.EmergencyStop=()=>Execute(co,RecyclerCapture.IsRecycler(co)?"capture-pause":"pause");
        foreach(var page in new[]{"operation","supplies","details"})
        {var name=page;C.Button(shell.Navigation,C.Text(name),()=>shell.Navigate(()=>{tab=name;Page(co);}));}
        if(Definitions.Machine(co))C.Button(shell.Navigation,C.Text("crew_settings"),()=>shell.Navigate(()=>Phobos.Ostranauts.Framework.Crew.CrewPanel.Show(co)));
        C.Button(shell.Navigation,C.Text("close"),shell.Close);
        ObjectPresentation.Picture(shell.List,co,120);C.Label(shell.List,ObjectPresentation.Location(co));
        live=C.Label(shell.List,"");
        if(Definitions.Machine(co))
        {
            var rect=W.Rect(shell.List,"Crop stage");C.Size(rect,160);portrait=rect.gameObject.AddComponent<RawImage>();portrait.color=Color.clear;portrait.raycastTarget=false;
            var ratio=rect.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.HeightControlsWidth;
        }
        Page(co);
    }
    private void Page(CondOwner co)
    {
        if(portrait!=null)portrait.transform.SetParent(shell.List,false);
        W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);readout=C.Label(shell.Detail,"");
        bool recycler=RecyclerCapture.IsRecycler(co);
        if(tab=="details") {C.Label(shell.Detail,Text.Get("panel_help"));C.Label(shell.Detail,co.strCODef+"\n"+co.strID);}
        else if(tab=="supplies")
        {
            if(recycler)
                C.Field(shell.Detail,C.Text("collector"),ObjectPresentation.Name(PanelConfiguration.Collector(co)),()=>Connection(co,"collector"),
                    ()=>ObjectPicker.Locate(shell,Service.Resolve(PanelConfiguration.Collector(co))),()=>Setting(co,"capture-unlink"),Service.Resolve(PanelConfiguration.Collector(co))!=null,PanelConfiguration.Collector(co)!="none");
            else if(!Definitions.IsCooker(co)&&!WorkupDefinitions.IsBench(co))
            {
                var peers=PanelConfiguration.WaterPeers(co);C.Field(shell.Detail,C.Text("water"),peers.Length==0?C.Text("not_selected"):string.Join(" · ",peers.Select(ObjectPresentation.Name)),()=>Connection(co,"water"),
                    ()=>ObjectPicker.Locate(shell,Service.Resolve(peers.FirstOrDefault()??"")),()=>Setting(co,"unlink-water"),Service.Resolve(peers.FirstOrDefault()??"")!=null,peers.Length>0);
                if(IrrigationDefinitions.IsSupply(co))
                {
                    C.Field(shell.Detail,C.Text("charge"),ObjectPresentation.Name(Service.Get(co).DoseId),()=>Connection(co,"charge"),
                        ()=>ObjectPicker.Locate(shell,Service.Resolve(Service.Get(co).DoseId)),()=>Setting(co,"dose-off"),Service.Resolve(Service.Get(co).DoseId)!=null,Service.Get(co).DoseId!="none");
                    foreach(var action in new[]{"mix-potato","mix-lettuce","mix-lettuce-seed","water-only"})AddButton(shell.Detail,co,action,true);
                }
                else foreach(var action in new[]{"water-routed","water-legacy"})AddButton(shell.Detail,co,action,true);
                var row=C.Row(shell.Detail);AddButton(row,co,"receive");AddButton(row,co,"pause-receive");
            }
            else C.Label(shell.Detail,Text.Get("panel_help"));
        }
        else
        {
            C.Heading(shell.Detail,C.Text("actions"));
            var actions=recycler?new[]{"capture-start","capture-pause"}:Service.Actions(co).Where(a=>!a.StartsWith("mix-")&&a!="water-only"&&a!="water-routed"&&a!="water-legacy"&&a!="unlink-water"&&!a.StartsWith("dose-")).ToArray();
            for(int i=0;i<actions.Length;i+=2){var row=C.Row(shell.Detail);foreach(var action in actions.Skip(i).Take(2))AddButton(row,co,action);}
            if(!recycler&&!Definitions.IsCooker(co)&&!WorkupDefinitions.IsBench(co))
            {
                C.Heading(shell.Detail,C.Text("maintenance"));var work=IrrigationDefinitions.IsSupply(co)?new[]{"load-water","load-irrigation","load-nutrients","recover-solution","drain"}:Definitions.Work.Where(a=>a!="recover-solution"&&a!="recover-crop"&&a!="formulate-nutrients").ToArray();
                for(int i=0;i<work.Length;i+=2){var row=C.Row(shell.Detail);foreach(var action in work.Skip(i).Take(2))AddButton(row,co,action);}
            }
        }
        C.Button(shell.Actions,C.Text("stop"),()=>Execute(co,recycler?"capture-pause":"pause"));
        C.Button(shell.Actions,C.Text("details"),()=>{tab="details";Page(co);});C.Button(shell.Actions,C.Text("close"),shell.Close);Refresh(co);
    }
    private void Connection(CondOwner co,string kind)
    {
        string current=kind=="collector"?PanelConfiguration.Collector(co):kind=="charge"?Service.Get(co).DoseId:PanelConfiguration.WaterPeers(co).FirstOrDefault()??"none";
        ConfigurationSheet.Objects(shell,C.Text(kind),current,PanelConfiguration.Stamp(co),
            ()=>kind=="collector"?RecyclerCapture.Candidates(co):kind=="charge"?Service.DoseCandidates(Service.Get(co)):Service.WaterCandidates(co),
            (string expected,string value,out string reason)=>PanelConfiguration.Apply(co,expected,value=="none"?(kind=="collector"?"capture-unlink":kind=="charge"?"dose-off":"unlink-water"):
                (kind=="collector"?"capture-link:":kind=="charge"?"dose:":"link-water:")+value,out reason));
    }
    private void Setting(CondOwner co,string action)=>ConfigurationSheet.Choices(shell,Text.Get(action),"",PanelConfiguration.Stamp(co),new[]{(action,Text.Get(action))},
        (string expected,string value,out string reason)=>PanelConfiguration.Apply(co,expected,value,out reason));
    private void AddButton(Transform parent,CondOwner co,string action,bool setting=false)=>C.Button(parent,Text.Get(action),()=>{if(setting)Setting(co,action);else Execute(co,action);});
    private void Execute(CondOwner co, string action)
    {
        bool recycler = RecyclerCapture.IsRecycler(co);
        bool success = recycler ? RecyclerCapture.Command(co, action, out result) : Service.Command(co, null, action, out result);
        // Services also serve console callers. Their successful status response is
        // already rendered live here; preserve distinct notices (e.g. queued work).
        result = Phobos.Ostranauts.Framework.Controls.PanelFeedback.Additional(success, result,
            recycler ? RecyclerCapture.Describe(co) : Service.Describe(co));
        shell.Notice.text=result;
        Refresh(co);
    }
    private void Update()
    {
        if (!bActive || Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .5f;
        var co = Service.Resolve(id);
        if (co == null || CrewSim.GetSelectedCrew()?.strID != actorId || Service.Access(co) != null) { shell.ForceClose(); return; }
        Refresh(co);
    }
    private void Refresh(CondOwner co)
    {
        if(portrait!=null)
        {
            var parent=shell.IsNarrow?shell.Detail:shell.List;
            if(portrait.transform.parent!=parent){portrait.transform.SetParent(parent,false);if(shell.IsNarrow)portrait.transform.SetAsFirstSibling();}
        }
        if(RecyclerCapture.IsRecycler(co)){readout.text=RecyclerCapture.Describe(co);live.text=C.Text("collector");return;}
        var session=Service.Get(co);var b=session.State;
        string state=C.Text(session.Protected?"state_Blocked":b.Running?"state_Running":"state_Stopped");
        live.text=state;readout.text=tab=="details"?Service.Describe(co):state+"\n"+(session.Protected?Text.Get("protected"):session.Notice);
        if(tab=="operation")readout.text+="\n"+(Definitions.IsCooker(co)?Text.Get("panel_cooker",b.CookerProgress/.05*100):
            WorkupDefinitions.IsBench(co)?Text.Get("panel_workup",session.Workup.Mode.Length==0?C.Text("not_selected"):Text.Get(session.Workup.Mode=="recover"?"recover-crop":"formulate-nutrients"),session.Workup.Energy):
            IrrigationDefinitions.IsSupply(co)?Text.Get("panel_supply",b.Water,session.Solution.TotalKg):Text.Get("panel_live",b.Progress*100,b.Health*100,b.Water,b.Nutrients));
        string key=Artwork.Key(co,b,session.Protected);
        if(portrait==null||key==portraitKey)return;portraitKey=key;portrait.texture=Artwork.Texture(key);portrait.color=portrait.texture!=null&&portrait.texture.name!="missing.png"?Color.white:Color.clear;
    }
    public override void SaveAndClose() { if (bActive) base.SaveAndClose(); }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class PanelRestorePatch
{
    private static bool Prefix(string strCOGUIKey, CondOwner coSelf)
    {
        if (strCOGUIKey != Panel.Key) return true;
        if (!Panel.Show(coSelf) && CrewSim.goUI == null) { CanvasManager.instance.CrewSimNormal(); if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true; }
        return false;
    }
}
