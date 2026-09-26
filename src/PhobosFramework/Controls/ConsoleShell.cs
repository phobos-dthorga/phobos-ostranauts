using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Layout policy in native reference units, independent of monitor pixels.</summary>
public static class ConsoleLayout
{
    public const float NarrowWidth=1000, Header=104, Footer=72, Margin=24;
    public static bool Narrow(float width)=>width<NarrowWidth;
    public static float ListWidth(float width)=>Math.Min(370,Math.Max(260,width*.31f));
}

/// <summary>Owned frame and independent viewports. Refresh data, not the hierarchy.</summary>
public sealed class ConsoleShell : MonoBehaviour
{
    public RectTransform Navigation=null!, List=null!, Detail=null!, Actions=null!;
    public ScrollRect ListScroll=null!, DetailScroll=null!;
    public TMP_Text Title=null!, Notice=null!;
    public Func<bool>? Dirty;
    public Func<bool>? Apply;
    public Action? Discard;
    public Action? CancelOverlay;
    public Action? EmergencyStop;
    public CondOwner? SelectionOrigin;
    private RectTransform body=null!, listRoot=null!, detailRoot=null!;
    private GameObject? dialog;
    private bool detailPage, forceClose;
    private float width=-1;
    private Texture2D? frameTexture;
    private Sprite? frameSprite;
    private static ConsoleShell? active;
    public static ConsoleShell Create(Transform parent,string title,Color accent)
    {
        var root=PanelWidgets.Rect(parent,"Phobos console");PanelWidgets.Fill(root,20,20,20,20);
        var shell=root.gameObject.AddComponent<ConsoleShell>();shell.Build(title,accent);active=shell;return shell;
    }
    /// <summary>Draft lifecycle for an existing native panel whose approved artwork and geometry stay intact.</summary>
    public static ConsoleShell AttachDraftGuard(Transform parent,Func<bool> dirty,Func<bool> apply,Action discard)
    {
        var root=PanelWidgets.Rect(parent,"Phobos draft guard");PanelWidgets.Fill(root);
        var shell=root.gameObject.AddComponent<ConsoleShell>();shell.Dirty=dirty;shell.Apply=apply;shell.Discard=discard;active=shell;return shell;
    }
    private void Build(string title,Color accent)
    {
        var surface=gameObject.AddComponent<Image>();surface.color=new Color(.075f,.095f,.115f);
        // Original Phobos frame, embedded to keep Framework independent of content packages.
        using(var source=typeof(ConsoleShell).Assembly.GetManifestResourceStream("PhobosFramework.console.png"))
        {
            if(source!=null)
            {
                using var bytes=new System.IO.MemoryStream();source.CopyTo(bytes);frameTexture=new Texture2D(2,2);
                if(ImageConversion.LoadImage(frameTexture,bytes.ToArray()))
                {frameSprite=Sprite.Create(frameTexture,new Rect(0,0,frameTexture.width,frameTexture.height),new Vector2(.5f,.5f),700,0,SpriteMeshType.FullRect,new Vector4(110,140,110,140));surface.sprite=frameSprite;surface.type=Image.Type.Sliced;surface.color=Color.white;}
            }
        }
        var edge=PanelWidgets.Rect(transform,"Accent");PanelWidgets.Fill(edge,0,0,0,0);edge.anchorMin=new Vector2(0,1);edge.offsetMin=new Vector2(0,-4);edge.gameObject.AddComponent<Image>().color=accent;
        Title=ConsoleWidgets.Label(transform,title,false);var tr=(RectTransform)Title.transform;PanelWidgets.Fill(tr,24,0,24,18);tr.anchorMin=new Vector2(0,1);tr.offsetMin=new Vector2(24,-50);Title.fontSize=24;Title.color=accent;Title.overflowMode=TextOverflowModes.Ellipsis;
        Navigation=ConsoleWidgets.Row(transform);PanelWidgets.Fill(Navigation,24,0,24,58);Navigation.anchorMin=new Vector2(0,1);Navigation.offsetMin=new Vector2(24,-94);
        body=PanelWidgets.Rect(transform,"Workspace");PanelWidgets.Fill(body,24,ConsoleLayout.Footer+10,24,ConsoleLayout.Header);
        List=PanelWidgets.Scroll(body,"Equipment",out ListScroll);listRoot=(RectTransform)ListScroll.transform;
        Detail=PanelWidgets.Scroll(body,"Details",out DetailScroll);detailRoot=(RectTransform)DetailScroll.transform;
        Actions=ConsoleWidgets.Row(transform);PanelWidgets.Fill(Actions,24,26,24,0);Actions.anchorMax=new Vector2(1,0);Actions.offsetMax=new Vector2(-24,62);
        Notice=ConsoleWidgets.Label(transform,"",false);var nr=(RectTransform)Notice.transform;PanelWidgets.Fill(nr,24,3,24,0);nr.anchorMax=new Vector2(1,0);nr.offsetMax=new Vector2(-24,24);Notice.fontSize=14;ConsoleWidgets.Fixed(Notice);
        Layout();
    }
    public void Page(bool detail){detailPage=detail;Layout();}
    public bool IsNarrow=>ConsoleLayout.Narrow(((RectTransform)transform).rect.width);
    private void Update(){if(body==null)return;float w=((RectTransform)transform).rect.width;if(Math.Abs(w-width)>1)Layout();}
    private void Layout()
    {
        width=((RectTransform)transform).rect.width;bool narrow=ConsoleLayout.Narrow(width);
        listRoot.gameObject.SetActive(!narrow||!detailPage);detailRoot.gameObject.SetActive(!narrow||detailPage);
        PanelWidgets.Fill(listRoot);PanelWidgets.Fill(detailRoot);
        if(!narrow){float split=ConsoleLayout.ListWidth(width);listRoot.anchorMax=new Vector2(0,1);listRoot.offsetMax=new Vector2(split,0);detailRoot.offsetMin=new Vector2(split+20,0);}
    }
    public void Navigate(Action action)
    {
        if(Dirty?.Invoke()!=true){action();return;}
        Confirm(action);
    }
    public void Close()=>CrewSim.LowerUI();
    public void ForceClose(){forceClose=true;CrewSim.LowerUI();}
    private void Confirm(Action continuation)
    {
        if(dialog!=null)return;
        var shade=PanelWidgets.Rect(transform,"Unsaved changes");PanelWidgets.Fill(shade);dialog=shade.gameObject;
        shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.9f);
        var card=PanelWidgets.Rect(shade,"Confirmation");card.anchorMin=new Vector2(.1f,.15f);card.anchorMax=new Vector2(.9f,.85f);card.offsetMin=card.offsetMax=Vector2.zero;
        var group=card.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=12;group.childControlHeight=group.childControlWidth=true;group.childForceExpandHeight=false;
        ConsoleWidgets.Label(card,ConsoleWidgets.Text("unsaved"));
        ConsoleWidgets.Button(card,ConsoleWidgets.Text("apply"),()=>{bool applied=Apply?.Invoke()==true;Dismiss();if(applied)continuation();});
        ConsoleWidgets.Button(card,ConsoleWidgets.Text("discard"),()=>{Discard?.Invoke();Dismiss();continuation();});
        ConsoleWidgets.Button(card,ConsoleWidgets.Text("keep_editing"),Dismiss);
        if(EmergencyStop!=null)ConsoleWidgets.Button(card,ConsoleWidgets.Text("stop"),()=>{Dismiss();EmergencyStop?.Invoke();});
    }
    private void Dismiss(){if(dialog!=null)Destroy(dialog);dialog=null;}
    internal static bool Closing()
    {
        if(active==null||active.forceClose)return true;
        if(active.CancelOverlay!=null){active.CancelOverlay();return false;}
        if(active.Dirty?.Invoke()!=true)return true;
        var owner=active;owner.Confirm(()=>{owner.forceClose=true;CrewSim.LowerUI();});return false;
    }
    private void OnDestroy(){if(active==this)active=null;if(frameSprite!=null)Destroy(frameSprite);if(frameTexture!=null)Destroy(frameTexture);CrewSim.EndTyping();}
}
[HarmonyPatch(typeof(CrewSim),nameof(CrewSim.LowerUI))]
internal static class ConsoleCloseGuard {private static bool Prefix()=>ConsoleShell.Closing();}
