using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Modal selection only: native hit testing, no gameplay controller, target or selection mutation.</summary>
public sealed class ObjectPicker : MonoBehaviour, IPointerClickHandler, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static ObjectPicker? worldPicker;
    private static readonly PickerInputGate input=new();
    internal static bool CapturesWorldInput=>input.Captures(Time.frameCount);
    private ConsoleShell shell=null!;
    private Func<IEnumerable<CondOwner>> candidates=null!;
    private Func<CondOwner,bool> relevant=null!;
    private Action<CondOwner> select=null!;
    private RectTransform rows=null!, card=null!;
    private CanvasGroup hidden=null!;
    private string query="";
    private bool includeOther, shipPick, locateOnly;
    private float oldAlpha;
    private bool oldInteract,oldRaycast;
    private Action? previousCancel;
    private readonly List<GameObject> markers=new();
    public static void Show(ConsoleShell shell,string title,Func<IEnumerable<CondOwner>> candidates,Action<CondOwner> select,Func<CondOwner,bool>? relevant=null)
    {
        var r=PanelWidgets.Rect(shell.transform,"Object picker");PanelWidgets.Fill(r);var p=r.gameObject.AddComponent<ObjectPicker>();
        p.shell=shell;p.candidates=candidates;p.select=select;p.relevant=relevant??(_=>true);p.Build(title);p.previousCancel=shell.CancelOverlay;shell.CancelOverlay=p.Cancel;
    }
    public static void Locate(ConsoleShell shell,CondOwner? co)
    {if(co==null)return;Show(shell,ConsoleWidgets.Text("locate"),()=>new[]{co},_=>{});var p=shell.transform.GetComponentInChildren<ObjectPicker>();p.locateOnly=true;p.ExposeShip();}
    private void Build(string title)
    {
        gameObject.AddComponent<Image>().color=new Color(.065f,.085f,.105f,.99f);
        card=PanelWidgets.Rect(transform,"Picker contents");PanelWidgets.Fill(card,18,18,18,18);
        var header=ConsoleWidgets.Row(card);PanelWidgets.Fill(header,0,0,0,0);header.anchorMin=new Vector2(0,1);header.offsetMin=new Vector2(0,-36);
        ConsoleWidgets.Label(header,title);ConsoleWidgets.Button(header,ConsoleWidgets.Text("pick_ship"),ExposeShip);ConsoleWidgets.Button(header,ConsoleWidgets.Text("cancel"),Cancel);
        var controls=ConsoleWidgets.Row(card);PanelWidgets.Fill(controls,0,0,0,46);controls.anchorMin=new Vector2(0,1);controls.offsetMin=new Vector2(0,-82);
        ConsoleWidgets.Input(controls,ConsoleWidgets.Text("search"),query,s=>{query=s;Populate();});
        ConsoleWidgets.Check(controls,ConsoleWidgets.Text("show_other"),includeOther,v=>{includeOther=v;Populate();});
        rows=PanelWidgets.Scroll(card,"Candidates",out var scroll);PanelWidgets.Fill((RectTransform)scroll.transform,0,0,0,96);Populate();
    }
    private CondOwner[] Current()=>candidates().Where(c=>c!=null&&!c.bDestroyed).Distinct().ToArray();
    private void Populate()
    {
        PanelWidgets.Clear(rows);var all=Current();var shown=all.Where(c=>(includeOther||relevant(c)) && (ObjectPresentation.Name(c)+" "+ObjectPresentation.Location(c)+" "+ObjectPresentation.Contents(c)).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0)
            .OrderByDescending(relevant).ThenBy(ObjectPresentation.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
        if(shown.Length==0)ConsoleWidgets.Label(rows,ConsoleWidgets.Text("no_candidates"));
        foreach(var co in shown)
        {var row=ConsoleWidgets.Row(rows,72);ObjectPresentation.Picture(row,co,60);
            ConsoleWidgets.Button(row,ObjectPresentation.Name(co)+" · "+ObjectPresentation.Location(co)+"\n"+ObjectPresentation.Contents(co)+(relevant(co)?"":" · "+ConsoleWidgets.Text("not_relevant")),()=>Choose(co),72);}
    }
    private void Choose(CondOwner co)
    {
        if(!Current().Contains(co)){Populate();return;}
        Cancel();select(co);
    }
    private void ExposeShip()
    {
        if(shipPick)return;
        CrewSim.EndTyping();shipPick=true;
        worldPicker=this;
        input.Begin();
        var controlCanvas=CanvasManager.instance.goCanvasControlPanels;
        hidden=controlCanvas.GetComponent<CanvasGroup>()??controlCanvas.AddComponent<CanvasGroup>();
        oldAlpha=hidden.alpha;oldInteract=hidden.interactable;oldRaycast=hidden.blocksRaycasts;hidden.alpha=0;hidden.interactable=false;hidden.blocksRaycasts=false;
        // Move out of the hidden panel. A full-canvas raycast surface swallows all world pointer input.
        var canvas=CanvasManager.instance.goCanvasGUI.GetComponent<Canvas>().rootCanvas;transform.SetParent(canvas.transform,false);PanelWidgets.Fill((RectTransform)transform);transform.SetAsLastSibling();
        var captureCanvas=gameObject.AddComponent<Canvas>();captureCanvas.overrideSorting=true;captureCanvas.sortingOrder=short.MaxValue;gameObject.AddComponent<GraphicRaycaster>();
        GetComponent<Image>().color=Color.clear;card.gameObject.SetActive(false);
        var bar=ConsoleWidgets.Row(transform,48);PanelWidgets.Fill(bar,80,0,80,32);bar.anchorMin=new Vector2(0,1);bar.offsetMin=new Vector2(80,-80);
        bar.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f,.97f);
        ConsoleWidgets.Label(bar,ConsoleWidgets.Text(locateOnly?"locate_help":"pick_help"));ConsoleWidgets.Button(bar,ConsoleWidgets.Text("back"),Cancel);
        foreach(var co in Current())
        {
            var point=CrewSim.objInstance.ActiveCam.WorldToScreenPoint(co.GetPos("use"));if(point.z<=0)continue;
            var marker=PanelWidgets.Rect(transform,"Candidate marker");marker.anchorMin=marker.anchorMax=Vector2.zero;marker.sizeDelta=new Vector2(36,36);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,point,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            marker.localPosition=local;var t=ConsoleWidgets.Label(marker,"[ ]",false);PanelWidgets.Fill((RectTransform)t.transform);t.fontSize=28;t.color=ConsoleWidgets.Amber;markers.Add(marker.gameObject);
        }
    }
    public void OnPointerClick(PointerEventData e)
    {
        CrewSim.bJustClickedInput=true;e.Use();if(!shipPick||locateOnly||e.button!=PointerEventData.InputButton.Left)return;
        var permitted=Current();var ids=PickerRules.Hits(CrewSim.objInstance.GetMouseOverCOsExternal(new Vector3(e.position.x,e.position.y,0)).Where(c=>c!=null).Select(c=>c.strID),permitted.Select(c=>c.strID));
        var hit=ids.Select(id=>permitted.First(c=>c.strID==id)).ToArray();
        if(hit.Length==1){Choose(hit[0]);return;}
        if(hit.Length>1)
        {
            card.gameObject.SetActive(true);GetComponent<Image>().color=new Color(.065f,.085f,.105f,.98f);PanelWidgets.Clear(rows);
            foreach(var co in hit)ConsoleWidgets.Button(rows,ObjectPresentation.Name(co)+" · "+ObjectPresentation.Location(co),()=>Choose(co),48);
        }
    }
    public void OnScroll(PointerEventData e){CrewSim.bJustClickedInput=true;e.Use();}
    public void OnBeginDrag(PointerEventData e){e.Use();}public void OnDrag(PointerEventData e){e.Use();}public void OnEndDrag(PointerEventData e){e.Use();}
    private void Cancel()
    {Restore();if(shell!=null)shell.CancelOverlay=previousCancel;gameObject.SetActive(false);Destroy(gameObject);}
    private void Restore()
    {
        if(worldPicker==this){worldPicker=null;input.End(Time.frameCount);CrewSim.bJustClickedInput=true;}
        if(hidden!=null)
        {if(CanvasManager.instance!=null&&CanvasManager.instance.State==CanvasManager.GUIState.SHIPGUI){hidden.alpha=oldAlpha;hidden.interactable=oldInteract;hidden.blocksRaycasts=oldRaycast;}hidden=null!;}
    }
    private void Update()
    {
        if(shell==null||shipPick&&(CanvasManager.instance==null||CanvasManager.instance.State!=CanvasManager.GUIState.SHIPGUI)){Cancel();return;}
        if(hidden!=null){hidden.alpha=0;hidden.interactable=false;hidden.blocksRaycasts=false;}
    }
    private void OnDestroy(){Restore();}
}
[HarmonyPatch(typeof(CrewSim),"MouseHandler")]
internal static class PickerMouseIsolation {private static bool Prefix()=>!ObjectPicker.CapturesWorldInput;}
[HarmonyPatch(typeof(CrewSim),"KeyHandler")]
internal static class PickerKeyIsolation {private static bool Prefix()=>!ObjectPicker.CapturesWorldInput;}
