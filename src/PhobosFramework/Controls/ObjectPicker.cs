using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using HarmonyLib;
using Ostranauts.InputControl;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Modal native hit testing without connection commands, targeting or crew selection.</summary>
public sealed class ObjectPicker : MonoBehaviour, IPointerClickHandler, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static ObjectPicker? worldPicker;
    private static readonly PickerInputGate input=new();
    internal static bool CapturesWorldInput=>input.Captures(Time.frameCount);
    internal static bool AllowCommand(Command command)
    {
        if(!CapturesWorldInput)return true;
        if(command is CommandUICancel&&worldPicker!=null)worldPicker.Cancel();
        return false;
    }
    private ConsoleShell shell=null!;
    private Func<IEnumerable<CondOwner>> candidates=null!;
    private Func<CondOwner,bool> relevant=null!;
    private Action<CondOwner> select=null!;
    private RectTransform rows=null!,card=null!,banner=null!;
    private CanvasGroup? hidden;
    private PickerGraphic? overlay;
    private TMP_Text? hint;
    private string query="",title="";
    private bool includeOther,shipPick,locateOnly,restored;
    private float oldAlpha,nextCandidates,feedbackUntil;
    private bool oldInteract,oldRaycast;
    private Action? previousCancel;
    private CondOwner[] visible=Array.Empty<CondOwner>();
    private Camera? viewCamera;
    private Vector3 oldCameraPosition;
    private float oldZoom;
    private bool oldFollow;
    public static void Show(ConsoleShell shell,string title,Func<IEnumerable<CondOwner>> candidates,Action<CondOwner> select,Func<CondOwner,bool>? relevant=null)
        =>Create(shell,title,candidates,select,relevant);
    private static ObjectPicker Create(ConsoleShell shell,string title,Func<IEnumerable<CondOwner>> candidates,Action<CondOwner> select,Func<CondOwner,bool>? relevant=null)
    {
        var r=PanelWidgets.Rect(shell.transform,"Object picker");PanelWidgets.Fill(r);var p=r.gameObject.AddComponent<ObjectPicker>();
        p.shell=shell;p.title=title;p.candidates=candidates;p.select=select;p.relevant=relevant??(_=>true);p.Build();p.previousCancel=shell.CancelOverlay;shell.CancelOverlay=p.Cancel;return p;
    }
    public static void Locate(ConsoleShell shell,CondOwner? co)
    {
        if(co==null||co.bDestroyed){shell.Notice.text=ConsoleWidgets.Text("locate_unavailable");return;}
        var p=Create(shell,ConsoleWidgets.Text("locate"),()=>new[]{co},_=>{});p.locateOnly=true;p.ExposeShip();p.Centre(co);
    }
    private void Build()
    {
        gameObject.AddComponent<Image>().color=new Color(.065f,.085f,.105f,.99f);
        card=PanelWidgets.Rect(transform,"Picker contents");PanelWidgets.Fill(card,18,18,18,18);
        var header=ConsoleWidgets.Row(card);PanelWidgets.Fill(header);header.anchorMin=new Vector2(0,1);header.offsetMin=new Vector2(0,-36);
        var caption=ConsoleWidgets.Label(header,title);ConsoleWidgets.Size(caption.transform,36);ConsoleWidgets.Fixed(caption);
        var pick=ConsoleWidgets.Button(header,ConsoleWidgets.Text("pick_ship"),ExposeShip);ConsoleWidgets.Size(pick.transform,36,150);
        var cancel=ConsoleWidgets.Button(header,ConsoleWidgets.Text("cancel"),Cancel);ConsoleWidgets.Size(cancel.transform,36,100);
        var controls=ConsoleWidgets.Row(card);PanelWidgets.Fill(controls,0,0,0,46);controls.anchorMin=new Vector2(0,1);controls.offsetMin=new Vector2(0,-82);
        ConsoleWidgets.Input(controls,ConsoleWidgets.Text("search"),query,s=>{query=s;Populate();});
        ConsoleWidgets.Check(controls,ConsoleWidgets.Text("show_other"),includeOther,v=>{includeOther=v;Populate();});
        rows=PanelWidgets.Scroll(card,"Candidates",out var scroll);PanelWidgets.Fill((RectTransform)scroll.transform,0,0,0,96);Populate();
    }
    private CondOwner[] Current()=>candidates().Where(c=>c!=null&&!c.bDestroyed).Distinct().ToArray();
    private CondOwner[] Eligible()=>Current().Where(c=>includeOther||relevant(c)).ToArray();
    private void Populate()
    {
        PanelWidgets.Clear(rows);var shown=Eligible().Where(c=>(ObjectPresentation.Name(c)+" "+ObjectPresentation.Location(c)+" "+ObjectPresentation.Contents(c)).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0)
            .OrderByDescending(relevant).ThenBy(ObjectPresentation.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
        if(shown.Length==0)ConsoleWidgets.Label(rows,ConsoleWidgets.Text("no_candidates"));
        foreach(var co in shown)CandidateRow(co);
    }
    private void CandidateRow(CondOwner co)
    {
        var row=ConsoleWidgets.Row(rows,72);ObjectPresentation.Picture(row,co,60);
        ConsoleWidgets.Button(row,ObjectPresentation.Name(co)+"\n"+ObjectPresentation.Location(co)+"\n"+ObjectPresentation.Contents(co)+(relevant(co)?"":" · "+ConsoleWidgets.Text("not_relevant")),()=>Choose(co),72);
    }
    private void Choose(CondOwner co)
    {
        if(!Current().Contains(co)){shell.Notice.text=ConsoleWidgets.Text("locate_unavailable");ShowList();return;}
        Cancel();select(co);
    }
    private void ExposeShip()
    {
        if(shipPick){card.gameObject.SetActive(false);banner.gameObject.SetActive(true);GetComponent<Image>().color=Color.clear;return;}
        if(CrewSim.objInstance?.ActiveCam==null||CanvasManager.instance==null)return;
        CrewSim.EndTyping();shipPick=true;worldPicker=this;input.Begin();
        var controlCanvas=CanvasManager.instance.goCanvasControlPanels;
        hidden=controlCanvas.GetComponent<CanvasGroup>()??controlCanvas.AddComponent<CanvasGroup>();
        oldAlpha=hidden.alpha;oldInteract=hidden.interactable;oldRaycast=hidden.blocksRaycasts;
        hidden.alpha=0;hidden.interactable=false;hidden.blocksRaycasts=false;
        // Independent canvas: no inherited native fading/hidden CanvasGroup.
        float scale=CanvasManager.instance.goCanvasGUI.GetComponent<Canvas>().rootCanvas.scaleFactor;
        transform.SetParent(null,false);
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=short.MaxValue;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=scale;
        gameObject.AddComponent<GraphicRaycaster>();GetComponent<Image>().color=Color.clear;
        var visual=PanelWidgets.Rect(transform,"Selection view");PanelWidgets.Fill(visual);visual.SetAsFirstSibling();overlay=visual.gameObject.AddComponent<PickerGraphic>();overlay.raycastTarget=false;
        banner=PanelWidgets.Rect(transform,"Selection mode");PanelWidgets.Fill(banner,36,0,36,24);banner.anchorMin=new Vector2(0,1);banner.offsetMin=new Vector2(36,-140);
        banner.gameObject.AddComponent<Image>().color=new Color(.065f,.105f,.14f,.98f);
        var layout=banner.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(16,16,8,8);layout.spacing=4;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var line=ConsoleWidgets.Row(banner);var heading=ConsoleWidgets.Label(line,ConsoleWidgets.Text(locateOnly?"locate_mode":"pick_mode",title));ConsoleWidgets.Size(heading.transform,36);ConsoleWidgets.Fixed(heading);heading.color=ConsoleWidgets.Amber;
        if(!locateOnly){var list=ConsoleWidgets.Button(line,ConsoleWidgets.Text("candidate_list"),ShowList);ConsoleWidgets.Size(list.transform,36,170);}
        var back=ConsoleWidgets.Button(line,ConsoleWidgets.Text(locateOnly?"back":"cancel"),Cancel);ConsoleWidgets.Size(back.transform,36,110);
        var help=ConsoleWidgets.Label(banner,ConsoleWidgets.Text(locateOnly?"locate_help":"pick_help"));ConsoleWidgets.Size(help.transform,26);ConsoleWidgets.Fixed(help);help.fontSize=16;
        hint=ConsoleWidgets.Label(banner,"");ConsoleWidgets.Size(hint.transform,26);ConsoleWidgets.Fixed(hint);hint.fontSize=16;
        card.gameObject.SetActive(false);viewCamera=CrewSim.objInstance.camMain;oldCameraPosition=viewCamera.transform.position;oldZoom=viewCamera.orthographicSize;oldFollow=CrewSim.objInstance.camFollow;CrewSim.objInstance.camFollow=false;
        if(!locateOnly&&shell.SelectionOrigin!=null)Centre(shell.SelectionOrigin);
        nextCandidates=0;
    }
    private void Centre(CondOwner co)
    {if(viewCamera==null)return;var p=co.GetPos("use");viewCamera.transform.position=new Vector3(p.x,p.y,viewCamera.transform.position.z);}
    private void ShowList()
    {card.gameObject.SetActive(true);if(banner!=null)banner.gameObject.SetActive(false);GetComponent<Image>().color=new Color(.065f,.085f,.105f,.99f);Populate();}
    private CondOwner[] Hits(Vector2 point,CondOwner[] permitted)
    {
        var ids=PickerRules.Hits(CrewSim.objInstance.GetMouseOverCOsExternal(new Vector3(point.x,point.y,0)).Where(c=>c!=null).Select(c=>c.strID),permitted.Select(c=>c.strID));
        return ids.Select(id=>permitted.First(c=>c.strID==id)).ToArray();
    }
    public void OnPointerClick(PointerEventData e)
    {
        CrewSim.bJustClickedInput=true;e.Use();if(!shipPick||locateOnly||card.gameObject.activeSelf||e.button!=PointerEventData.InputButton.Left)return;
        if(RectTransformUtility.RectangleContainsScreenPoint(banner,e.position))return;
        var hit=Hits(e.position,Eligible());
        if(hit.Length==1){Choose(hit[0]);return;}
        if(hit.Length>1){ShowList();PanelWidgets.Clear(rows);ConsoleWidgets.Heading(rows,ConsoleWidgets.Text("overlap_choices"));foreach(var co in hit)CandidateRow(co);}
        else if(hint!=null){hint.text=ConsoleWidgets.Text("pick_invalid");feedbackUntil=Time.unscaledTime+2;}
    }
    public void OnScroll(PointerEventData e)
    {CrewSim.bJustClickedInput=true;e.Use();if(shipPick&&!card.gameObject.activeSelf)CrewSim.objInstance.CamZoom(Mathf.Exp(-e.scrollDelta.y*.1f));}
    public void OnBeginDrag(PointerEventData e){CrewSim.bJustClickedInput=true;e.Use();}
    public void OnDrag(PointerEventData e)
    {
        e.Use();if(!shipPick||card.gameObject.activeSelf||viewCamera==null||e.button==PointerEventData.InputButton.Left)return;
        float scale=2*viewCamera.orthographicSize/viewCamera.pixelHeight;viewCamera.transform.Translate(-e.delta.x*scale,-e.delta.y*scale,0);
    }
    public void OnEndDrag(PointerEventData e){CrewSim.bJustClickedInput=true;e.Use();}
    private Vector2 Local(Vector2 screen)
    {RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var local);return local;}
    private Rect Project(CondOwner co)
    {
        var camera=CrewSim.objInstance.ActiveCam;var centre=Local(camera.WorldToScreenPoint(co.GetPos("use")));var size=new Vector2(28,28);
        var renderer=co.GetComponent<Renderer>();
        if(renderer!=null){var bounds=renderer.bounds;var a=Local(camera.WorldToScreenPoint(bounds.min));var b=Local(camera.WorldToScreenPoint(bounds.max));size=new Vector2(Mathf.Clamp(Math.Abs(b.x-a.x)+8,24,420),Mathf.Clamp(Math.Abs(b.y-a.y)+8,24,420));centre=(a+b)/2;}
        return new Rect(centre-size/2,size);
    }
    private void LateUpdate()
    {
        if(!shipPick||overlay==null||card.gameObject.activeSelf)return;
        if(Time.unscaledTime>=nextCandidates){visible=locateOnly?Current():Eligible();nextCandidates=Time.unscaledTime+.5f;}
        var current=visible.Where(c=>c!=null&&!c.bDestroyed).ToArray();var bounds=((RectTransform)transform).rect;
        var boxes=current.Select(Project).Where(r=>bounds.Overlaps(r)).ToArray();var mouse=(Vector2)InputManager.MousePosition;
        bool onBanner=RectTransformUtility.RectangleContainsScreenPoint(banner,mouse);
        var hit=onBanner||locateOnly?Array.Empty<CondOwner>():Hits(mouse,current);
        var origin=shell.SelectionOrigin;Vector2 from=origin==null?Vector2.zero:Local(CrewSim.objInstance.ActiveCam.WorldToScreenPoint(origin.GetPos("use")));
        Vector2 to=locateOnly&&current.Length>0?Project(current[0]).center:Local(mouse);
        overlay.Present(boxes,from,to,hit.Length>0,origin!=null&&!onBanner);
        if(hint!=null&&(hit.Length>0||Time.unscaledTime>=feedbackUntil))hint.text=locateOnly?(current.Length==0?ConsoleWidgets.Text("locate_unavailable"):ObjectPresentation.Name(current[0])+" · "+ObjectPresentation.Location(current[0])):
            hit.Length>0?ConsoleWidgets.Text(hit.Length==1?"hover_choice":"hover_overlap",ObjectPresentation.Name(hit[0]),hit.Length):ConsoleWidgets.Text("pick_count",current.Length,boxes.Length);
    }
    private void Cancel()
    {Restore();if(shell!=null)shell.CancelOverlay=previousCancel;gameObject.SetActive(false);Destroy(gameObject);}
    private void Restore()
    {
        if(restored)return;restored=true;
        if(worldPicker==this){worldPicker=null;input.End(Time.frameCount);CrewSim.bJustClickedInput=true;}
        if(hidden!=null){hidden.alpha=oldAlpha;hidden.interactable=oldInteract;hidden.blocksRaycasts=oldRaycast;hidden=null;}
        if(viewCamera!=null&&CrewSim.objInstance!=null&&viewCamera==CrewSim.objInstance.camMain)
        {CrewSim.objInstance.camFollow=oldFollow;if(CanvasManager.instance?.State==CanvasManager.GUIState.SHIPGUI){viewCamera.transform.position=oldCameraPosition;CrewSim.objInstance.CamZoom(oldZoom/viewCamera.orthographicSize);}}
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
[HarmonyPatch(typeof(Command),nameof(Command.Perform))]
internal static class PickerCommandIsolation {private static bool Prefix(Command __instance)=>ObjectPicker.AllowCommand(__instance);}
