using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

public delegate bool ConfigurationApply(string expected,string value,out string reason);
/// <summary>A focused configuration form. Selection is a draft; content validates and applies it.</summary>
public static class ConfigurationSheet
{
    public static void Objects(ConsoleShell shell,string title,string current,string expected,Func<IEnumerable<CondOwner>> candidates,ConfigurationApply apply,bool allowClear=true)
        =>Show(shell,title,current,expected,(body,get,set)=>ConsoleWidgets.Field(body,title,ObjectPresentation.Name(get()),
            ()=>ObjectPicker.Show(shell,title,candidates,co=>set(co.strID)),()=>ObjectPicker.Locate(shell,Crew.CrewWork.Resolve(get())),allowClear?()=>set("none"):null,
            Crew.CrewWork.Resolve(get())!=null,!string.IsNullOrEmpty(get())&&get()!="none"),apply);
    public static void Choices(ConsoleShell shell,string title,string current,string expected,IEnumerable<(string Value,string Label)> options,ConfigurationApply apply)
        =>Show(shell,title,current,expected,(body,get,set)=>{foreach(var o in options){var option=o;ConsoleWidgets.Button(body,(get()==option.Value?"[x] ":"[ ] ")+option.Label,()=>set(option.Value));}},apply);
    private static void Show(ConsoleShell shell,string title,string current,string expected,Action<Transform,Func<string>,Action<string>> render,ConfigurationApply apply)
    {
        var overlay=PanelWidgets.Rect(shell.transform,"Configuration draft");PanelWidgets.Fill(overlay);overlay.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var body=PanelWidgets.Scroll(overlay,"Setting",out var scroll);PanelWidgets.Fill((RectTransform)scroll.transform,24,76,24,24);
        var footer=ConsoleWidgets.Row(overlay);PanelWidgets.Fill(footer,24,24,24,0);footer.anchorMax=new Vector2(1,0);footer.offsetMax=new Vector2(-24,60);
        var oldDirty=shell.Dirty;var oldApply=shell.Apply;var oldDiscard=shell.Discard;var oldCancel=shell.CancelOverlay;
        string value=current,notice="";
        void Refresh(){PanelWidgets.Clear(body);ConsoleWidgets.Heading(body,title);if(notice.Length>0)ConsoleWidgets.Label(body,notice);render(body,()=>value,s=>{value=s;notice=ConsoleWidgets.Text(s=="none"?"cleared_draft":"selection_draft");Refresh();});}
        void Close(){shell.Dirty=oldDirty;shell.Apply=oldApply;shell.Discard=oldDiscard;shell.CancelOverlay=oldCancel;overlay.gameObject.SetActive(false);UnityEngine.Object.Destroy(overlay.gameObject);}
        bool Apply(){if(value==current&&value.Length>0)return true;if(value.Length==0){notice=ConsoleWidgets.Text("not_selected");Refresh();return false;}if(!apply(expected,value,out var reason)){notice=reason;Refresh();return false;}current=value;shell.Notice.text=ConsoleWidgets.Text("applied");return true;}
        shell.Dirty=()=>value!=current;shell.Apply=Apply;shell.Discard=()=>{value=current;Refresh();};shell.CancelOverlay=()=>shell.Navigate(Close);
        ConsoleWidgets.Button(footer,ConsoleWidgets.Text("apply"),()=>{if(Apply())Close();});
        ConsoleWidgets.Button(footer,ConsoleWidgets.Text("discard"),()=>{value=current;Close();});
        ConsoleWidgets.Button(footer,ConsoleWidgets.Text("back"),()=>shell.Navigate(Close));Refresh();
        if(shell.EmergencyStop!=null)ConsoleWidgets.Button(footer,ConsoleWidgets.Text("stop"),()=>shell.EmergencyStop?.Invoke());
    }
}
