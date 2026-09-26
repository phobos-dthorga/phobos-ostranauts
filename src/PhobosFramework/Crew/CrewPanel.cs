using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Read-only presentation with isolated drafts and checked service actions.</summary>
public sealed class CrewPanel : GUIData
{
    public const string Key = "PhobosCrewPanel";
    private string shipId="", equipmentId="", view="orders", query="", crewId="", roleExpected="";
    private ConsoleShell shell=null!;
    private RectTransform rows=null!;
    private OrderDraft? draft;
    private readonly Dictionary<CrewRole,bool> roleDraft=new(), roleOriginal=new();
    private readonly Dictionary<string,TMP_Text> summaries=new();
    private readonly Dictionary<string,Action<double>> training=new();
    private readonly Dictionary<string,(float List,float Detail,string Query)> positions=new();
    private TMP_Text? status;
    private float next;
    private int previewHours=1;
    private global::Ostranauts.Core.Models.Tuple<string,CondOwner>? returnPanel;
    private Ship? Ship=>CrewSim.system?.GetShipByRegID(shipId);
    internal static void ShowPreview(Ship ship,int hours=1)
    {var previous=CrewSim.tplCurrentUI;if(Show(ship.ShipCO)){var panel=CrewSim.goUI.GetComponent<CrewPanel>();panel.returnPanel=previous;panel.previewHours=Math.Max(1,Math.Min(6,hours));panel.view="skip";panel.BuildView();}}
    public static void Button(Transform parent,CondOwner? co=null)=>C.Button(parent,CrewWork.Message("open"),()=>Show(co));
    public static bool Show(CondOwner? co=null)
    {
        var actor=CrewSim.GetSelectedCrew();var ship=co?.ship??actor?.ship;
        if(actor==null||ship==null||actor.ship!=ship||CrewSim.goIntUIPanel==null||CrewSim.bUILock||CrewSim.system?.GetShipOwner(ship.strRegID)!=CrewSim.coPlayer?.strID)return false;
        CrewSim.LowerUI();if(CrewSim.goUI!=null)return false;
        var root=W.Rect(CrewSim.goIntUIPanel.transform,Key);W.Fill(root);
        CrewSim.goUI=root.gameObject;var panel=root.gameObject.AddComponent<CrewPanel>();
        panel.shipId=ship.strRegID;panel.equipmentId=co!=null&&CrewWork.Provider(co)!=null?co.strID:"";
        panel.Init(actor,new Dictionary<string,string>(),Key);panel.strFriendlyName=CrewWork.Message("open");panel.bActive=true;
        CrewSim.tplLastUI=CrewSim.tplCurrentUI;CrewSim.tplCurrentUI=new global::Ostranauts.Core.Models.Tuple<string,CondOwner>(Key,actor);
        CanvasManager.instance.ShipGUI();CrewSim.SetUIArrows();panel.Build();return true;
    }
    private bool Dirty()=>draft?.Dirty==true||roleDraft.Any(p=>!roleOriginal.TryGetValue(p.Key,out var v)||v!=p.Value);
    private void Build()
    {
        shell=ConsoleShell.Create(transform,C.Text("crew_title"),C.Slate);shell.Dirty=Dirty;shell.Apply=Apply;shell.Discard=Discard;
        foreach(var tab in new[]{"orders","crew","skip"}){var id=tab;C.Button(shell.Navigation,C.Text(tab),()=>shell.Navigate(()=>{RememberView();view=id;query=positions.TryGetValue(view,out var saved)?saved.Query:"";BuildView();RestoreView();}));}
        C.Button(shell.Navigation,C.Text("back"),()=>shell.Navigate(()=>{equipmentId=crewId="";Discard();shell.Page(false);}));
        C.Button(shell.Navigation,C.Text("close"),shell.Close);BuildView();
    }
    private void RememberView()=>positions[view]=(shell.ListScroll.verticalNormalizedPosition,shell.DetailScroll.verticalNormalizedPosition,query);
    private void RestoreView()
    {
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition=positions.TryGetValue(view,out var saved)?saved.List:1;
        shell.DetailScroll.verticalNormalizedPosition=positions.TryGetValue(view,out saved)?saved.Detail:1;
    }
    private void BuildView()
    {
        draft=null;roleDraft.Clear();roleOriginal.Clear();status=null;summaries.Clear();shell.EmergencyStop=null;W.Clear(shell.List);W.Clear(shell.Detail);W.Clear(shell.Actions);
        if(Ship==null)return;
        if(view=="skip"){BuildSkip();return;}
        C.Input(shell.List,C.Text("search"),query,s=>{query=s;Populate();});
        rows=W.Rect(shell.List,"Rows");var group=rows.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlWidth=group.childControlHeight=true;group.childForceExpandHeight=false;
        Populate();
        if(view=="orders"&&CrewWork.Resolve(equipmentId) is CondOwner co)Edit(co);
        else if(view=="crew"&&CrewWork.Resolve(crewId) is CondOwner actor)EditCrew(actor);
        else {C.Heading(shell.Detail,C.Text(view=="orders"?"orders":"crew"));C.Label(shell.Detail,C.Text(view=="orders"?"orders_intro":"crew_intro"));shell.Page(false);}
    }
    private void Populate()
    {
        W.Clear(rows);summaries.Clear();if(Ship==null)return;
        var choices=view=="orders"?CrewWork.Equipment(Ship):CrewRoster.Members().Where(c=>c.ship==Ship);
        foreach(var co in choices.Where(c=>(ObjectPresentation.Name(c)+" "+ObjectPresentation.Location(c)).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0).OrderBy(ObjectPresentation.Name))
        {
            var item=co;var b=C.Button(rows,Summary(co),()=>shell.Navigate(()=>{if(view=="orders")Edit(item);else EditCrew(item);}),C.RowHeight);
            var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=16;summaries[co.strID]=label;
        }
    }
    private string Summary(CondOwner co)
    {
        if(view=="crew")return ObjectPresentation.ListName(co)+"\n"+Availability(co);
        var s=CrewWork.ReadStatus(co);return ObjectPresentation.ListName(co)+"\n"+s.Label+" · "+s.Worker+" · "+s.Work;
    }
    private static string Availability(CondOwner actor)
    {
        string auto=C.Text(actor.HasCond("IsAIManual")?"autotask_off":"autotask_on");
        int shift=actor.Company?.GetShift(StarSystem.nUTCHour,actor).nID??-1;
        string state=C.Text(shift==2?"working":shift==0?"resting":"free_time");
        var offer=new CrewWorkOffer("preview","",CrewRole.Agriculture,actor,1,"");
        CrewWork.Eligible(actor,offer,out var reason,false);
        return auto+" · "+state+(reason.Length>0?" · "+reason:"");
    }
    private void Header(CondOwner co)
    {
        var row=C.Row(shell.Detail,80);ObjectPresentation.Picture(row,co,72);C.Label(row,ObjectPresentation.Name(co)+"\n"+ObjectPresentation.Location(co));
        C.Button(shell.Detail,C.Text("display_name"),()=>Nickname(co));
    }
    private void Edit(CondOwner co)
    {
        if(CrewWork.Order(co).Protected)
        {draft=null;W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);C.Label(shell.Detail,CrewWork.Message("protected"));return;}
        equipmentId=co.strID;roleDraft.Clear();roleOriginal.Clear();draft=new OrderDraft(co.strID,CrewWork.Order(co));RenderOrder(co);
    }
    private void RenderOrder(CondOwner co)
    {
        W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);Header(co);var provider=CrewWork.Provider(co)!;var value=draft!.Value;
        shell.EmergencyStop=()=>StopOrder(co);
        status=C.Label(shell.Detail,StatusText(co));C.Heading(shell.Detail,C.Text("configuration"));
        var recipes=provider.Recipes(co);C.Field(shell.Detail,C.Text("process"),recipes.Contains(value.Recipe)?provider.RecipeLabel(value.Recipe):CrewWork.Message("choose_work"),()=>Choices(C.Text("process"),recipes.Select(r=>(r,provider.RecipeLabel(r))),id=>{value.Recipe=id;RenderOrder(co);}));
        var fields=OrderConfiguration.Fields(co);
        if(fields.HasFlag(OrderFields.Stock))C.Stepper(shell.Detail,C.Text("stock"),value.Stock,1,256,n=>value.Stock=n);
        if(fields.HasFlag(OrderFields.Source))StoreField(co,false);
        if(fields.HasFlag(OrderFields.Destination))StoreField(co,true);
        if(fields.HasFlag(OrderFields.Routine))C.Check(shell.Detail,C.Text("routine"),value.ResumeRoutine,v=>value.ResumeRoutine=v);
        if(fields.HasFlag(OrderFields.Hazardous))C.Check(shell.Detail,C.Text("hazardous"),value.Hazardous,v=>value.Hazardous=v);
        if(fields.HasFlag(OrderFields.ClearCrops))C.Check(shell.Detail,C.Text("clear_crops"),value.ClearCrops,v=>value.ClearCrops=v);
        if(fields.HasFlag(OrderFields.Drain))C.Check(shell.Detail,C.Text("drain"),value.Drain,v=>value.Drain=v);
        if(fields.HasFlag(OrderFields.Target)&&provider is ICrewOrderPresentation p)
            C.Field(shell.Detail,C.Text("mission_target"),CrewSim.system.GetShipByRegID(value.Target)?.publicName??C.Text("not_selected"),()=>Choices(C.Text("mission_target"),p.Targets(co).Select(s=>(s.strRegID,s.publicName)),id=>{value.Target=id;RenderOrder(co);}),clear:()=>{value.Target="none";RenderOrder(co);});
        C.Button(shell.Detail,C.Text("diagnostics"),()=>C.Label(shell.Detail,co.strCODef+"\n"+co.strID+"\n"+CrewWork.Status(co)));
        C.Button(shell.Actions,C.Text("apply"),()=>Apply());C.Button(shell.Actions,C.Text("discard"),Discard);
        C.Button(shell.Actions,C.Text("resume"),()=>{if(Dirty()){shell.Notice.text=C.Text("apply_first");return;}CrewWork.SetPermission(co,WorkPermission.Enabled);Edit(co);});
        C.Button(shell.Actions,C.Text("stop"),()=>StopOrder(co));
    }
    private void StopOrder(CondOwner co)
    {CrewWork.SetPermission(co,WorkPermission.Stopped);if(draft?.Dirty!=true)Edit(co);else shell.Notice.text=C.Text("stopped_draft");}
    private static string StatusText(CondOwner co)
    {var s=CrewWork.ReadStatus(co);return s.Label+" · "+s.Worker+(s.Detail.Length>0?"\n"+s.Detail:"");}
    private void StoreField(CondOwner co,bool output)
    {
        string selected=output?draft!.Value.Destination:draft!.Value.Source;
        void Set(string id){if(output)draft!.Value.Destination=id;else draft!.Value.Source=id;RenderOrder(co);}
        var presentation=CrewWork.Provider(co) as ICrewOrderPresentation;
        C.Field(shell.Detail,C.Text(output?"output_storage":"input_storage"),ObjectPresentation.Name(selected),
            ()=>ObjectPicker.Show(shell,C.Text(output?"output_storage":"input_storage"),()=>CrewWork.Stores(co.ship),c=>Set(c.strID),c=>presentation?.RelevantStore(co,draft!.Value,c,output)??true),
            ()=>ObjectPicker.Locate(shell,CrewWork.Resolve(selected)),()=>Set("none"));
    }
    private void Choices(string title,IEnumerable<(string Id,string Label)> choices,Action<string> choose)
    {
        var overlay=W.Rect(shell.transform,"Selection");W.Fill(overlay);overlay.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var body=W.Scroll(overlay,"Options",out var scroll);W.Fill((RectTransform)scroll.transform,24,24,24,24);
        void Close(){shell.CancelOverlay=null;overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
        shell.CancelOverlay=Close;C.Heading(body,title);C.Button(body,C.Text("cancel"),Close);
        foreach(var choice in choices){var entry=choice;C.Button(body,entry.Label,()=>{Close();choose(entry.Id);},48);}
    }
    private void Nickname(CondOwner co)
    {
        var overlay=W.Rect(shell.transform,"Display name");W.Fill(overlay);overlay.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var body=W.Scroll(overlay,"Name",out var scroll);W.Fill((RectTransform)scroll.transform,24,24,24,24);
        string expected=ObjectPresentation.Nickname(co),value=expected;C.Heading(body,C.Text("display_name"));C.Label(body,co.FriendlyName);
        C.Input(body,C.Text("nickname_hint"),value,s=>value=s);var message=C.Label(body,"");
        void Close(){shell.CancelOverlay=null;Destroy(overlay.gameObject);}
        shell.CancelOverlay=Close;C.Button(body,C.Text("apply"),()=>{if(ObjectPresentation.Rename(co,expected,value,out var reason)){Close();Populate();}else message.text=reason;});C.Button(body,C.Text("cancel"),Close);
    }
    private void EditCrew(CondOwner actor)
    {
        crewId=actor.strID;draft=null;roleDraft.Clear();roleOriginal.Clear();W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);Header(actor);
        status=C.Label(shell.Detail,Availability(actor));C.Label(shell.Detail,C.Text("native_roster"));roleExpected=CrewSpecialities.RoleFingerprint(actor);
        C.Heading(shell.Detail,C.Text("permissions"));
        foreach(CrewRole role in Enum.GetValues(typeof(CrewRole))){var r=role;bool allowed=CrewSpecialities.Allowed(actor,role);roleDraft[r]=roleOriginal[r]=allowed;C.Check(shell.Detail,CrewWork.Message(role.ToString()),allowed,v=>roleDraft[r]=v);}
        training.Clear();C.Heading(shell.Detail,C.Text("training"));foreach(var skill in CrewSpecialities.All)training[skill.Id]=C.Progress(shell.Detail,skill.Label,CrewSpecialities.Progress(actor,skill.Id));
        C.Button(shell.Actions,C.Text("apply"),()=>Apply());C.Button(shell.Actions,C.Text("discard"),Discard);
    }
    private bool Apply()
    {
        bool done;string reason;
        if(draft!=null&&CrewWork.Resolve(equipmentId) is CondOwner co){done=OrderConfiguration.Apply(co,draft,out reason);if(done)Edit(co);}
        else if(CrewWork.Resolve(crewId) is CondOwner actor){done=CrewSpecialities.ApplyRoles(actor,roleExpected,roleDraft,out reason);if(done)EditCrew(actor);}
        else {done=false;reason=C.Text("unavailable");}
        shell.Notice.text=reason;return done;
    }
    private void Discard(){draft=null;roleDraft.Clear();roleOriginal.Clear();BuildView();}
    private void BuildSkip()
    {
        shell.Page(true);C.Heading(shell.List,C.Text("skip"));C.Label(shell.List,C.Text("skip_estimate"));C.Label(shell.List,C.Text("skip_native"));
        C.Heading(shell.Detail,C.Text("crew"));
        foreach(var actor in CrewRoster.Members().Where(c=>c.ship==Ship))
        {
            C.Label(shell.Detail,ObjectPresentation.Name(actor)+"\n"+Availability(actor));
            var shifts=Enumerable.Range(0,previewHours).Select(i=>C.Text("hour_shift",i+1,C.Text(actor.Company.GetShift((StarSystem.nUTCHour+i)%24,actor).nID==2?"working":actor.Company.GetShift((StarSystem.nUTCHour+i)%24,actor).nID==0?"resting":"free_time")));
            C.Label(shell.Detail,string.Join(" · ",shifts));
        }
        C.Heading(shell.Detail,C.Text("onboard"));
        var equipment=CrewWork.Equipment(Ship!).Where(c=>CrewWork.Order(c).Permission==WorkPermission.Enabled).ToArray();
        foreach(var co in equipment.Where(c=>CrewWork.Provider(c) is ICrewSkipProvider))
        {
            var provider=CrewWork.Provider(co)!;string detail=CrewWork.Message("skip_wait");
            try{if(((ICrewSkipProvider)provider).CanAdvance(co,out var limit)){var offer=provider.Next(co,CrewWork.Order(co),out var blocker);detail=offer==null?blocker:C.Text("skip_work",offer.Label,offer.Seconds/60);if(offer!=null&&!CrewRoster.Members().Any(a=>CrewWork.Eligible(a,offer,out _)))detail+=" · "+CrewWork.Message("crew_unavailable");}else detail=limit;}
            catch{detail=CrewWork.Message("protected");}
            C.Label(shell.Detail,ObjectPresentation.Name(co)+"\n"+detail);
        }
        C.Heading(shell.Detail,C.Text("suspended_operations"));
        foreach(var co in equipment.Where(c=>CrewWork.Provider(c) is not ICrewSkipProvider || OrderConfiguration.Fields(c).HasFlag(OrderFields.Target)))C.Label(shell.Detail,ObjectPresentation.Name(co)+"\n"+CrewWork.Message("skip_wait"));
        C.Label(shell.Detail,C.Text("skip_estimate"));
        C.Button(shell.Actions,C.Text(returnPanel==null?"close":"back"),()=>
        {
            var previous=returnPanel;CrewSim.LowerUI();if(CrewSim.goUI!=null||previous==null||previous.Item2==null||previous.Item2.bDestroyed)return;
            CrewSim.RaiseUI(previous.Item1,previous.Item2);
            var native=CrewSim.goUI?.GetComponent<GUIFFWD>();if(native!=null&&AccessTools.Field(typeof(GUIFFWD),"sldHours").GetValue(native) is Slider hours)hours.value=previewHours;
        });
    }
    private void Update()
    {
        if(!bActive||CrewSim.goUI!=gameObject||shell==null||Time.unscaledTime<next)return;next=Time.unscaledTime+1;
        if(Ship==null||CrewSim.GetSelectedCrew()?.ship!=Ship){shell.ForceClose();return;}
        foreach(var entry in summaries){var co=CrewWork.Resolve(entry.Key);entry.Value.text=co==null?C.Text("unavailable"):Summary(co);}
        if(status!=null){var co=view=="orders"?CrewWork.Resolve(equipmentId):null;var actor=view=="crew"?CrewWork.Resolve(crewId):null;status.text=co!=null?StatusText(co):actor!=null?Availability(actor):C.Text("unavailable");}
        if(view=="crew"&&status!=null&&CrewWork.Resolve(crewId) is CondOwner trainee)foreach(var bar in training)bar.Value(CrewSpecialities.Progress(trainee,bar.Key));
    }
}
[HarmonyPatch(typeof(GUIRosterRow),nameof(GUIRosterRow.SetOwner))]
internal static class CrewRosterButton
{
    private static void Postfix(GUIRosterRow __instance)
    {
        if(__instance.transform.Find("PhobosCrewButton")!=null)return;
        var holder=W.Rect(__instance.transform,"PhobosCrewButton"); holder.anchorMin=holder.anchorMax=new Vector2(0,1);
        holder.pivot=new Vector2(0,1); holder.anchoredPosition=new Vector2(4,-32); holder.sizeDelta=new Vector2(150,28);
        var button=W.Button(holder,CrewWork.Message("open"),()=>CrewPanel.Show()); W.Fill((RectTransform)button.transform);
        button.GetComponentInChildren<TMP_Text>().fontSize=13;
    }
}
[HarmonyPatch(typeof(GUIFFWD),"SetSlider")]
internal static class CrewSkipPreview
{
    private static void Postfix(GUIFFWD __instance)
    {
        var ship=CrewSim.GetSelectedCrew()?.ship;if(ship==null)return;
        var buttonOld=__instance.transform.Find("PhobosCrewPreviewButton");
        if(buttonOld!=null)return;
        var button=C.Button(__instance.transform,C.Text("preview_button"),()=>CrewPanel.ShowPreview(ship,
            (int)((AccessTools.Field(typeof(GUIFFWD),"sldHours").GetValue(__instance) as Slider)?.value??1)));
        button.name="PhobosCrewPreviewButton"; var br=(RectTransform)button.transform;
        br.anchorMin=new Vector2(.04f,.01f);br.anchorMax=new Vector2(.38f,.055f);br.offsetMin=br.offsetMax=Vector2.zero;
        button.GetComponentInChildren<TMP_Text>().fontSize=14;
    }
}
[HarmonyPatch(typeof(CrewSim),nameof(CrewSim.RaiseUI))]
internal static class CrewPanelRestore
{
    private static bool Prefix(string strCOGUIKey)
    { if (strCOGUIKey != CrewPanel.Key) return true; CrewPanel.Show(); return false; }
}
[HarmonyPatch]
internal static class CrewWorldReset
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>typeof(CrewSim).GetMethods().Where(m=>m.Name==nameof(CrewSim.LoadGame)||m.Name==nameof(CrewSim.NewGame));
    private static void Prefix()=>CrewWork.Reset();
}
