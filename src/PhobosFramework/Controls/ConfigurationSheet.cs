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
    /// <summary>The object form with a note under the field (Framework 0.69.0): why something aboard is not offered.</summary>
    public static void Objects(ConsoleShell shell,string title,string current,string expected,Func<IEnumerable<CondOwner>> candidates,ConfigurationApply apply,bool allowClear,Func<string>? note)
    {
        string text=Read(note);
        Show(shell,title,current,expected,(body,get,set)=>{ConsoleWidgets.Field(body,title,ObjectPresentation.Name(get()),
            ()=>ObjectPicker.Show(shell,title,candidates,co=>set(co.strID)),()=>ObjectPicker.Locate(shell,Crew.CrewWork.Resolve(get())),allowClear?()=>set("none"):null,
            Crew.CrewWork.Resolve(get())!=null,!string.IsNullOrEmpty(get())&&get()!="none");if(text.Length>0)ConsoleWidgets.Label(body,text);},apply);
    }
    public static void Choices(ConsoleShell shell,string title,string current,string expected,IEnumerable<(string Value,string Label)> options,ConfigurationApply apply)
        =>Choices(shell,title,current,expected,options,apply,null);
    /// <summary>The choice form with a note under the choices (Framework 0.69.0). The note is read once, as the sheet opens.</summary>
    public static void Choices(ConsoleShell shell,string title,string current,string expected,IEnumerable<(string Value,string Label)> options,ConfigurationApply apply,Func<string>? note)
    {
        string text=Read(note);
        Show(shell,title,current,expected,(body,get,set)=>{foreach(var o in options){var option=o;ConsoleWidgets.Button(body,(get()==option.Value?"[x] ":"[ ] ")+option.Label,()=>set(option.Value));}
            if(text.Length>0)ConsoleWidgets.Label(body,text);},apply);
    }
    // A note is presentation only: a fault in it must never keep the sheet from opening.
    private static string Read(Func<string>? note){try{return note?.Invoke()??"";}catch(Exception e){FrameworkLifecycle.Log(e.ToString());return "";}}
    /// <summary>The notice after a successful Apply: the provider's own words (for example "Hydrogen store linked.")
    /// followed by the shared reminder, or the reminder alone.</summary>
    public static string AppliedNotice(string? providerMessage)
    {
        string applied=ConsoleWidgets.Text("applied");
        return string.IsNullOrWhiteSpace(providerMessage)||providerMessage==applied?applied:providerMessage!.Trim()+" "+applied;
    }
    private static void Show(ConsoleShell shell,string title,string current,string expected,Action<Transform,Func<string>,Action<string>> render,ConfigurationApply apply)
    {
        var overlay=PanelWidgets.Rect(shell.transform,"Configuration draft");PanelWidgets.Fill(overlay);overlay.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var body=PanelWidgets.Scroll(overlay,"Setting",out var scroll);PanelWidgets.Fill((RectTransform)scroll.transform,24,76,24,24);
        var footer=ConsoleWidgets.Row(overlay);PanelWidgets.Fill(footer,24,24,24,0);footer.anchorMax=new Vector2(1,0);footer.offsetMax=new Vector2(-24,60);
        var oldDirty=shell.Dirty;var oldApply=shell.Apply;var oldDiscard=shell.Discard;var oldCancel=shell.CancelOverlay;
        string value=current,notice="";bool changed=false;
        // Press twice to go ahead (0.125.0): an Apply that needs other steps first warns, and Apply again does them.
        var guard=new PressGuard();Button? applyButton=null;
        void Relabel(){var label=applyButton==null?null:applyButton.GetComponentInChildren<TMPro.TMP_Text>();if(label!=null)label.text=guard.Label(value,ConsoleWidgets.Text("apply"));}
        void Refresh(){PanelWidgets.Clear(body);ConsoleWidgets.Heading(body,title);if(notice.Length>0)ConsoleWidgets.Label(body,notice);render(body,()=>value,s=>{value=s;guard.Disarm();Relabel();notice=ConsoleWidgets.Text(s=="none"?"cleared_draft":"selection_draft");Refresh();});}
        // Every way out of the sheet ends here; a sheet that applied a change asks the host to redraw its pages, whose
        // field and action buttons were built from the settings before the change.
        void Close(){shell.Dirty=oldDirty;shell.Apply=oldApply;shell.Discard=oldDiscard;shell.CancelOverlay=oldCancel;overlay.gameObject.SetActive(false);UnityEngine.Object.Destroy(overlay.gameObject);if(changed)shell.Changed?.Invoke();}
        bool Apply()
        {
            if(value==current&&value.Length>0)return true;
            if(value.Length==0){notice=ConsoleWidgets.Text("not_selected");Refresh();return false;}
            string reason="";bool done=guard.Press(value,()=>apply(expected,value,out reason));Relabel();
            if(!done){notice=reason;Refresh();return false;}
            current=value;changed=true;shell.Notice.text=AppliedNotice(reason);return true;
        }
        shell.Dirty=()=>value!=current;shell.Apply=Apply;shell.Discard=()=>{value=current;guard.Disarm();Relabel();Refresh();};shell.CancelOverlay=()=>shell.Navigate(Close);
        applyButton=ConsoleWidgets.Button(footer,ConsoleWidgets.Text("apply"),()=>{if(Apply())Close();});
        ConsoleWidgets.Button(footer,ConsoleWidgets.Text("discard"),()=>{value=current;Close();});
        ConsoleWidgets.Button(footer,ConsoleWidgets.Text("back"),()=>shell.Navigate(Close));Refresh();
        if(shell.EmergencyStop!=null)ConsoleWidgets.Button(footer,ConsoleWidgets.Text("stop"),()=>shell.EmergencyStop?.Invoke());
    }
}
