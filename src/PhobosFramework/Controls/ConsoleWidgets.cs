using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Opt-in compact controls. Existing panel and native instrument dimensions are untouched.</summary>
public static class ConsoleWidgets
{
    public const float ControlHeight = 36, RowHeight = 48, BodySize = 18;
    public static readonly Color Slate = new(.32f,.49f,.59f), Green = new(.43f,.62f,.46f), Amber = new(.83f,.60f,.28f);
    public static string Text(string key, params object[] args) => ConsoleText.Get(key,args);
    public static RectTransform Row(Transform parent, float height = ControlHeight)
    {
        var r=PanelWidgets.Rect(parent,"Row"); Size(r,height);
        var g=r.gameObject.AddComponent<HorizontalLayoutGroup>(); g.spacing=8; g.childControlWidth=g.childControlHeight=true;
        g.childForceExpandWidth=true; g.childForceExpandHeight=false; g.childAlignment=TextAnchor.MiddleLeft; return r;
    }
    public static void Size(Transform t,float height,float width=-1)
    {
        var l=t.GetComponent<LayoutElement>()??t.gameObject.AddComponent<LayoutElement>();
        l.minHeight=l.preferredHeight=height; l.flexibleHeight=0;
        if(width>=0){l.minWidth=l.preferredWidth=width;l.flexibleWidth=0;}else l.flexibleWidth=1;
    }
    public static TMP_Text Label(Transform p,string text,bool flowing=true)=>PanelWidgets.Label(p,text,flowing);
    public static TMP_Text Heading(Transform p,string text)
    {var t=Label(p,text);t.color=Green;t.fontStyle=FontStyles.Bold;return t;}
    public static Button Button(Transform p,string text,Action click,float height=ControlHeight)
    {
        var r=PanelWidgets.Rect(p,"Action");Size(r,height);
        r.gameObject.AddComponent<RectMask2D>();
        var image=r.gameObject.AddComponent<Image>();image.color=PanelWidgets.ButtonColor;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
        var t=Label(r,text,false);PanelWidgets.Fill((RectTransform)t.transform,10,3,10,3);
        t.alignment=TextAlignmentOptions.MidlineLeft;t.overflowMode=TextOverflowModes.Ellipsis;
        PanelWidgets.FitFixedText(t);t.fontSize=height>=RowHeight&&height<64?16:BodySize;
        b.onClick.AddListener(()=>{CrewSim.bJustClickedInput=true;click();});return b;
    }
    public static TMP_InputField Input(Transform p,string hint,string value,Action<string> changed)
    {var f=PanelWidgets.Search(p,hint,changed);Size(f.transform,ControlHeight);f.SetTextWithoutNotify(value);return f;}
    public static Toggle Check(Transform p,string text,bool value,Action<bool> changed)
    {
        var r=PanelWidgets.Rect(p,"Check");Size(r,ControlHeight);var toggle=r.gameObject.AddComponent<Toggle>();
        var box=PanelWidgets.Rect(r,"Box");box.anchorMin=box.anchorMax=new Vector2(0,.5f);box.pivot=new Vector2(0,.5f);box.sizeDelta=new Vector2(24,24);
        var image=box.gameObject.AddComponent<Image>();image.color=PanelWidgets.ButtonColor;toggle.targetGraphic=image;
        var mark=PanelWidgets.Rect(box,"Mark");PanelWidgets.Fill(mark,5,5,5,5);var check=mark.gameObject.AddComponent<Image>();check.color=Green;toggle.graphic=check;
        var label=Label(r,text,false);PanelWidgets.Fill((RectTransform)label.transform,36,2,0,2);label.alignment=TextAlignmentOptions.MidlineLeft;
        toggle.SetIsOnWithoutNotify(value);toggle.onValueChanged.AddListener(v=>{CrewSim.bJustClickedInput=true;changed(v);});return toggle;
    }
    public static TMP_Text Stepper(Transform p,string label,int value,int min,int max,Action<int> changed)
    {
        var row=Row(p);var text=Label(row,label);Size(text.transform,ControlHeight);
        var less=Button(row,"−",()=>{});Size(less.transform,ControlHeight,36);
        var read=Label(row,value.ToString());Size(read.transform,ControlHeight,70);
        var more=Button(row,"+",()=>{});Size(more.transform,ControlHeight,36);
        less.onClick.AddListener(()=>{value=Math.Max(min,value-1);read.text=value.ToString();changed(value);});
        more.onClick.AddListener(()=>{value=Math.Min(max,value+1);read.text=value.ToString();changed(value);});return read;
    }
    public static Action<double> Progress(Transform p,string label,double percent)
    {
        var caption=Label(p,"");
        var rail=PanelWidgets.Rect(p,"Progress");Size(rail,8);rail.gameObject.AddComponent<Image>().color=PanelWidgets.ButtonColor;
        var fill=PanelWidgets.Rect(rail,"Value");PanelWidgets.Fill(fill);fill.gameObject.AddComponent<Image>().color=Green;
        void Refresh(double value){caption.text=Text("progress",label,double.IsNaN(value)?Text("unavailable"):value.ToString("0")+"%");fill.anchorMax=new Vector2((float)(double.IsNaN(value)?0:Math.Max(0,Math.Min(100,value))/100),1);}
        Refresh(percent);return Refresh;
    }
    public static void Field(Transform p,string title,string value,Action change,Action? locate=null,Action? clear=null)
    {
        Heading(p,title);var row=Row(p);var t=Label(row,value);Size(t.transform,ControlHeight);t.overflowMode=TextOverflowModes.Ellipsis;
        var b=Button(row,Text("change"),change);Size(b.transform,ControlHeight,100);
        if(locate!=null){b=Button(row,Text("locate"),locate);Size(b.transform,ControlHeight,90);}
        if(clear!=null){b=Button(row,Text("clear"),clear);Size(b.transform,ControlHeight,80);}
    }
}
