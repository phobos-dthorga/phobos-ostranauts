using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Read-only presentation with isolated drafts and checked service actions. Since Framework 0.117.0 (owner
/// direction, 6 October 2026) the panel opened from a machine shows that machine alone until Show all ship; the lists
/// are collapsible groups by state with the ones that need the player open; each page keeps one short line and an
/// About button that opens the encyclopedia article, and the per-machine facts stay here.</summary>
public sealed class CrewPanel : GUIData
{
    public const string Key = "PhobosCrewPanel";
    public const string OrdersView="orders",UpkeepView="upkeep";
    private string shipId="", equipmentId="", focusId="", view=OrdersView, query="", crewId="", roleExpected="", upkeepSelected="", listSignature="";
    private bool shipWide;
    private ConsoleShell shell=null!;
    private RectTransform rows=null!;
    private GroupedList? list;
    private OrderDraft? draft;
    private readonly Dictionary<CrewRole,bool> roleDraft=new(), roleOriginal=new();
    private readonly Dictionary<string,TMP_Text> summaries=new();
    private readonly Dictionary<string,Button> tabs=new();
    private readonly Dictionary<string,Action<double>> training=new();
    private readonly Dictionary<string,(float List,float Detail,string Query)> positions=new();
    // Which groups each view keeps folded: remembered while the game runs, not saved (agent default).
    private static readonly Dictionary<string,HashSet<string>> folded=new(StringComparer.Ordinal);
    private TMP_Text? status;
    private TMP_Text? diagnostics;
    // The buttons that light green when they are the next step (Framework 0.118.0).
    private Button? applyButton, resumeButton;
    /// <summary>Resume with unsaved changes (0.125.0): the first press says the draft is applied first; the second does it.</summary>
    private readonly PressGuard resumeGuard = new();
    private const string ResumePress = "resume";
    private bool detailsOpen;
    private float next;
    private int previewHours=1;
    private global::Ostranauts.Core.Models.Tuple<string,CondOwner>? returnPanel;
    private Ship? Ship=>CrewSim.system?.GetShipByRegID(shipId);
    private bool Focused=>focusId.Length>0&&!shipWide;
    internal static void ShowPreview(Ship ship,int hours=1)
    {var previous=CrewSim.tplCurrentUI;if(Show(ship.ShipCO)){var panel=CrewSim.goUI.GetComponent<CrewPanel>();panel.returnPanel=previous;panel.previewHours=Math.Max(1,Math.Min(6,hours));panel.view="skip";panel.BuildView();}}
    public static void Button(Transform parent,CondOwner? co=null)=>C.Button(parent,CrewWork.Message("open"),()=>Show(co));
    /// <summary>Opens on one tab (Framework 0.116.0: the right-click Maintenance sheet's buttons): the orders tab with
    /// this machine's card, or the upkeep tab with its card. Any other view opens the orders tab.</summary>
    public static bool Show(CondOwner? co,string view)
    {
        if(!Show(co))return false;
        if(view==UpkeepView){var panel=CrewSim.goUI.GetComponent<CrewPanel>();panel.view=view;panel.upkeepSelected=panel.focusId;panel.BuildView();}
        return true;
    }
    /// <summary>Why <see cref="Show(CondOwner)"/> would refuse for this object now, or null when it would open.</summary>
    public static string? Unavailable(CondOwner? co)
    {
        var actor=CrewSim.GetSelectedCrew();var ship=co?.ship??actor?.ship;
        if(actor==null)return CrewWork.Message("panel_no_crew");
        if(ship==null||actor.ship!=ship)return CrewWork.Message("panel_not_aboard",actor.FriendlyName);
        if(CrewSim.system?.GetShipOwner(ship.strRegID)!=CrewSim.coPlayer?.strID)return CrewWork.Message("panel_not_owned");
        if(CrewSim.goIntUIPanel==null||CrewSim.bUILock)return CrewWork.Message("panel_blocked");
        return null;
    }
    public static bool Show(CondOwner? co=null)
    {
        var actor=CrewSim.GetSelectedCrew();var ship=co?.ship??actor?.ship;
        if(actor==null||ship==null||actor.ship!=ship||CrewSim.goIntUIPanel==null||CrewSim.bUILock||CrewSim.system?.GetShipOwner(ship.strRegID)!=CrewSim.coPlayer?.strID)return false;
        CrewSim.LowerUI();if(CrewSim.goUI!=null)return false;
        var root=W.Rect(CrewSim.goIntUIPanel.transform,Key);W.Fill(root);
        CrewSim.goUI=root.gameObject;var panel=root.gameObject.AddComponent<CrewPanel>();
        panel.shipId=ship.strRegID;panel.equipmentId=co!=null&&CrewWork.Provider(co)!=null?co.strID:"";
        // Opened from a machine (not the ship itself): that machine alone, until Show all ship.
        panel.focusId=co!=null&&co!=ship.ShipCO&&co.HasCond("IsInstalled")?co.strID:"";
        panel.Init(actor,new Dictionary<string,string>(),Key);panel.strFriendlyName=CrewWork.Message("open");panel.bActive=true;
        CrewSim.tplLastUI=CrewSim.tplCurrentUI;CrewSim.tplCurrentUI=new global::Ostranauts.Core.Models.Tuple<string,CondOwner>(Key,actor);
        CanvasManager.instance.ShipGUI();CrewSim.SetUIArrows();panel.Build();return true;
    }
    private bool Dirty()=>draft?.Dirty==true||roleDraft.Any(p=>!roleOriginal.TryGetValue(p.Key,out var v)||v!=p.Value);
    private void Build()
    {
        shell=ConsoleShell.Create(transform,C.Text("crew_title"),C.Slate);shell.Dirty=Dirty;shell.Apply=Apply;shell.Discard=Discard;
        foreach(var tab in new[]{OrdersView,"crew",UpkeepView,"skip"}){var id=tab;tabs[id]=C.Button(shell.Navigation,C.Text(tab),()=>shell.Navigate(()=>{RememberView();view=id;query=positions.TryGetValue(view,out var saved)?saved.Query:"";BuildView();RestoreView();}));}
        C.Button(shell.Navigation,C.Text("back"),()=>shell.Navigate(()=>{equipmentId=crewId=upkeepSelected="";Discard();shell.Page(false);}));
        C.Button(shell.Navigation,C.Text("close"),shell.Close);BuildView();
    }
    private void RememberView()=>positions[view]=(shell.ListScroll.verticalNormalizedPosition,shell.DetailScroll.verticalNormalizedPosition,query);
    private void RestoreView()
    {
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition=positions.TryGetValue(view,out var saved)?saved.List:1;
        shell.DetailScroll.verticalNormalizedPosition=positions.TryGetValue(view,out saved)?saved.Detail:1;
    }
    /// <summary>Tabs name how many things need the player there, "Orders (2)", and a tab other than the current one with
    /// a count is tinted amber (Framework 0.118.0). The count is the signal; the tint repeats it.</summary>
    private void RefreshTabs()
    {
        int orders=0,upkeep=0;
        if(Ship!=null)
        {
            orders=CrewWork.Equipment(Ship).Count(c=>Tones.ForOrder(CrewWork.ReadStatus(c).State)==Tone.Attention);
            upkeep=Upkeep.Report(Ship).Count(r=>Upkeep.Needs(r)!=UpkeepRules.Attention.None);
        }
        foreach(var tab in tabs)
        {
            int count=tab.Key==OrdersView?orders:tab.Key==UpkeepView?upkeep:0;
            string text=count>0?C.Text("tab_count",C.Text(tab.Key),count):C.Text(tab.Key);
            var label=tab.Value.GetComponentInChildren<TMP_Text>();if(label!=null&&label.text!=text)label.text=text;
            if(tab.Key==view)C.Accent(tab.Value,C.Slate);else C.Accent(tab.Value,count>0?Tone.Attention:Tone.Neutral);
        }
    }
    /// <summary>Apply lights while there are changes to apply; Resume lights when nothing is pending and the order, with
    /// work chosen, is not running (Framework 0.118.0). Both still work whenever they did before.</summary>
    private void RefreshActions()
    {
        if(applyButton!=null)C.Accent(applyButton,Tones.ForApply(Dirty()));
        if(resumeButton!=null&&CrewWork.Resolve(equipmentId) is CondOwner co)C.Accent(resumeButton,Tones.ForResume(Dirty(),CrewWork.Order(co).Permission,CrewWork.ReadStatus(co).State));
    }
    /// <summary>The groups a view keeps folded, seeded with its defaults the first time it is shown.</summary>
    private static HashSet<string> Folded(string view)
    {
        if(folded.TryGetValue(view,out var set))return set;
        IEnumerable<string> seed=view==OrdersView?OrderGroups.DefaultFolded:view==UpkeepView?new[]{UpkeepRules.FineGroup,UpkeepRules.InspectOnlyGroup}:new[]{CrewSkip.Paused};
        return folded[view]=new HashSet<string>(seed,StringComparer.Ordinal);
    }
    private void BuildView()
    {
        draft=null;roleDraft.Clear();roleOriginal.Clear();status=diagnostics=null;applyButton=resumeButton=null;summaries.Clear();list=null;listSignature="";shell.EmergencyStop=null;W.Clear(shell.List);W.Clear(shell.Detail);W.Clear(shell.Actions);
        RefreshTabs();
        if(Ship==null)return;
        if(view=="skip"){BuildSkip();return;}
        if(view==UpkeepView){BuildUpkeep();return;}
        if(view==OrdersView&&Focused&&CrewWork.Resolve(focusId) is CondOwner focus){shell.Page(true);if(CrewWork.Provider(focus)!=null)Edit(focus);else NoOrdersCard(focus);return;}
        C.Input(shell.List,C.Text("search"),query,s=>{query=s;Populate();});
        rows=W.Rect(shell.List,"Rows");var group=rows.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlWidth=group.childControlHeight=true;group.childForceExpandHeight=false;
        if(view==OrdersView)list=new GroupedList(rows,Folded(OrdersView));
        Populate();
        if(view==OrdersView&&CrewWork.Resolve(equipmentId) is CondOwner co)Edit(co);
        else if(view=="crew"&&CrewWork.Resolve(crewId) is CondOwner actor)EditCrew(actor);
        else if(view==OrdersView){C.Heading(shell.Detail,C.Text(OrdersView));C.Label(shell.Detail,OrdersSummary());Help.About(shell.Detail,"operations-standing-orders",shell.Notice);shell.Page(false);}
        else {C.Heading(shell.Detail,C.Text("crew"));C.Label(shell.Detail,C.Text("crew_intro"));shell.Page(false);}
    }
    private IEnumerable<CondOwner> OrderEquipment()=>Ship==null?Array.Empty<CondOwner>():CrewWork.Equipment(Ship)
        .Where(c=>(ObjectPresentation.Name(c)+" "+ObjectPresentation.Location(c)).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0).OrderBy(ObjectPresentation.Name);
    private List<GroupedList.Row> OrderRows()=>OrderEquipment().Select(co=>{var item=co;var s=CrewWork.ReadStatus(co);
        return new GroupedList.Row(OrderGroups.Group(s.State),co.strID,Summary(co),()=>shell.Navigate(()=>Edit(item)));}).ToList();
    private static IReadOnlyList<(string Key,string Label,Tone Tone)> GroupLabels(IEnumerable<string> keys,string prefix,Func<string,Tone> tone)=>keys.Select(k=>(k,C.Text(prefix+k),tone(k))).ToArray();
    private string OrdersSummary()
    {
        var states=OrderEquipment().Select(c=>OrderGroups.Group(CrewWork.ReadStatus(c).State)).ToList();
        return C.Text("orders_summary",states.Count(g=>g==OrderGroups.NeedsYou),states.Count(g=>g==OrderGroups.Working),states.Count(g=>g==OrderGroups.Off));
    }
    private void Populate()
    {
        if(Ship==null)return;
        if(view==OrdersView&&list!=null)
        {
            float scroll=shell.ListScroll.verticalNormalizedPosition;
            var orderRows=OrderRows();listSignature=GroupedList.Signature(orderRows);
            list.Render(GroupLabels(OrderGroups.Order,"group_",Tones.ForOrderGroup),orderRows,equipmentId,Populate);
            if(orderRows.Count==0)C.Label(rows,C.Text("no_orders_equipment"));
            Canvas.ForceUpdateCanvases();shell.ListScroll.verticalNormalizedPosition=scroll;
            return;
        }
        W.Clear(rows);summaries.Clear();
        foreach(var co in CrewRoster.Members().Where(c=>c.ship==Ship).Where(c=>(ObjectPresentation.Name(c)+" "+ObjectPresentation.Location(c)).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0).OrderBy(ObjectPresentation.Name))
        {
            var item=co;var b=C.Button(rows,Summary(co),()=>shell.Navigate(()=>EditCrew(item)),C.RowHeight);
            var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=16;summaries[co.strID]=label;
            C.Accent(b,C.Green,co.strID==crewId);
        }
    }
    private string Summary(CondOwner co)
    {
        if(view=="crew")return ObjectPresentation.ListName(co)+"\n"+Availability(co);
        return OrderGroups.RowText(ObjectPresentation.ListName(co),CrewWork.ReadStatus(co),CrewWork.Message("unassigned"));
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
        shell.SelectionOrigin=co;
        if(list!=null)foreach(var entry in list.Buttons)C.Accent(entry.Value,C.Green,entry.Key==co.strID);
        var row=C.Row(shell.Detail,96);ObjectPresentation.Picture(row,co,72);var caption=C.Label(row,ObjectPresentation.Name(co)+"\n"+ObjectPresentation.Location(co));C.Size(caption.transform,96);C.Fixed(caption);
        C.Button(shell.Detail,C.Text("display_name"),()=>Nickname(co));
        if(Focused)C.Button(shell.Detail,C.Text("show_all_ship"),()=>shell.Navigate(()=>{shipWide=true;BuildView();}));
    }
    /// <summary>The card for a machine without crew orders (Framework 0.117.0): how it is loaded, and its upkeep.</summary>
    private void NoOrdersCard(CondOwner co)
    {
        W.Clear(shell.Detail);W.Clear(shell.Actions);Header(co);
        status=C.Status(shell.Detail,C.Text("orders_none_card")+"\n"+LoadingText(co));
        string upkeep=Upkeep.Summary(co);
        if(upkeep.Length>0){C.Heading(shell.Detail,C.Text(UpkeepView));C.Label(shell.Detail,upkeep);}
        Help.About(shell.Detail,"operations-standing-orders",shell.Notice);
    }
    internal static string LoadingText(CondOwner co)=>OrderGroups.LoadingText(C.Text("loaded_by_hand"),StoreFeed.Describe(co),StoreDelivery.Describe(co));
    private void Edit(CondOwner co)
    {
        if(CrewWork.Order(co).Protected)
        {draft=null;W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);C.Label(shell.Detail,CrewWork.Message("protected"));return;}
        if(equipmentId!=co.strID)detailsOpen=false;
        equipmentId=co.strID;roleDraft.Clear();roleOriginal.Clear();draft=new OrderDraft(co.strID,CrewWork.Order(co));RenderOrder(co);
    }
    private void RenderOrder(CondOwner co)
    {
        float scroll=shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);Header(co);var provider=CrewWork.Provider(co)!;var value=draft!.Value;
        shell.EmergencyStop=()=>StopOrder(co);
        status=C.Status(shell.Detail,StatusText(co),Tones.ForOrder(CrewWork.ReadStatus(co).State));C.Heading(shell.Detail,C.Text("configuration"));
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
            C.Field(shell.Detail,C.Text("mission_target"),CrewSim.system.GetShipByRegID(value.Target)?.publicName??C.Text("not_selected"),()=>Choices(C.Text("mission_target"),p.Targets(co).Select(s=>(s.strRegID,s.publicName)),id=>{value.Target=id;RenderOrder(co);}),null,()=>{value.Target="none";RenderOrder(co);shell.Notice.text=C.Text("cleared_draft");},false,value.Target!="none"&&!string.IsNullOrEmpty(value.Target));
        var detailButton=C.Button(shell.Detail,C.Text(detailsOpen?"hide_diagnostics":"diagnostics"),()=>{});
        diagnostics=C.Label(shell.Detail,C.Text("technical_identity",co.strCODef,co.strID));diagnostics.gameObject.SetActive(detailsOpen);
        detailButton.onClick.AddListener(()=>{detailsOpen=!detailsOpen;diagnostics.gameObject.SetActive(detailsOpen);detailButton.GetComponentInChildren<TMP_Text>().text=C.Text(detailsOpen?"hide_diagnostics":"diagnostics");});
        applyButton=C.Button(shell.Actions,C.Text("apply"),()=>Apply());C.Button(shell.Actions,C.Text("discard"),Discard);
        resumeButton=C.Button(shell.Actions,resumeGuard.Label(ResumePress,C.Text("resume")),()=>Resume(co));
        C.Accent(C.Button(shell.Actions,C.Text("stop"),()=>StopOrder(co)),C.Amber);RefreshActions();
        Canvas.ForceUpdateCanvases();shell.DetailScroll.verticalNormalizedPosition=scroll;
    }
    /// <summary>Resume, applying an unsaved draft first on the second press (Framework 0.125.0; until then it refused).</summary>
    private void Resume(CondOwner co)
    {
        resumeGuard.Press(ResumePress,()=>
        {
            if(Dirty())
            {
                if(!Confirmations.Ask(C.Text("apply_first"),false,out var warning)){shell.Notice.text=warning;return false;}
                if(!Apply())return false;
            }
            CrewWork.SetPermission(co,WorkPermission.Enabled);Edit(co);return true;
        });
        var label=resumeButton==null?null:resumeButton.GetComponentInChildren<TMP_Text>();
        if(label!=null)label.text=resumeGuard.Label(ResumePress,C.Text("resume"));
    }
    private void StopOrder(CondOwner co)
    {CrewWork.SetPermission(co,WorkPermission.Stopped);if(draft?.Dirty!=true)Edit(co);else shell.Notice.text=C.Text("stopped_draft");}
    private static string StatusText(CondOwner co)
    {var s=CrewWork.ReadStatus(co);return s.Label+" · "+s.Worker+(s.Detail.Length>0?"\n"+s.Detail:"");}
    private void StoreField(CondOwner co,bool output)
    {
        string selected=output?draft!.Value.Destination:draft!.Value.Source;
        void Set(string id){if(output)draft!.Value.Destination=id;else draft!.Value.Source=id;RenderOrder(co);shell.Notice.text=C.Text(id=="none"?"cleared_draft":"selection_draft");}
        var presentation=CrewWork.Provider(co) as ICrewOrderPresentation;
        C.Field(shell.Detail,C.Text(output?"output_storage":"input_storage"),ObjectPresentation.Name(selected),
            ()=>ObjectPicker.Show(shell,C.Text(output?"output_storage":"input_storage"),()=>CrewWork.Stores(co.ship),c=>Set(c.strID),c=>presentation?.RelevantStore(co,draft!.Value,c,output)??true),
            ()=>ObjectPicker.Locate(shell,CrewWork.Resolve(selected)),()=>Set("none"),CrewWork.Resolve(selected)!=null,!string.IsNullOrEmpty(selected)&&selected!="none");
        // Supplies may also come from anywhere aboard, as the game's own Reload job searches.
        if(!output&&selected!=StandingOrder.ShipWide)C.Button(shell.Detail,C.Text("ship_wide_choose"),()=>Set(StandingOrder.ShipWide));
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
        shell.CancelOverlay=Close;C.Button(body,C.Text("apply"),()=>{if(ObjectPresentation.Rename(co,expected,value,out var reason)){Close();if(rows!=null)Populate();}else message.text=reason;});C.Button(body,C.Text("cancel"),Close);
    }
    private void EditCrew(CondOwner actor)
    {
        crewId=actor.strID;draft=null;diagnostics=null;roleDraft.Clear();roleOriginal.Clear();W.Clear(shell.Detail);W.Clear(shell.Actions);shell.Page(true);Header(actor);
        status=C.Status(shell.Detail,Availability(actor));C.Label(shell.Detail,C.Text("native_roster"));roleExpected=CrewSpecialities.RoleFingerprint(actor);
        C.Heading(shell.Detail,C.Text("permissions"));
        foreach(CrewRole role in Enum.GetValues(typeof(CrewRole))){var r=role;bool allowed=CrewSpecialities.Allowed(actor,role);roleDraft[r]=roleOriginal[r]=allowed;C.Check(shell.Detail,CrewWork.Message(role.ToString()),allowed,v=>roleDraft[r]=v);}
        training.Clear();C.Heading(shell.Detail,C.Text("training"));foreach(var skill in CrewSpecialities.All)training[skill.Id]=C.Progress(shell.Detail,skill.Label,CrewSpecialities.Progress(actor,skill.Id));
        applyButton=C.Button(shell.Actions,C.Text("apply"),()=>Apply());resumeButton=null;C.Button(shell.Actions,C.Text("discard"),Discard);RefreshActions();
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

    // Crew upkeep (Framework 0.111.0; redrawn in 0.117.0): four ship-wide switches in a row each, then the machines in
    // groups with the ones that need attention open; a machine's card on the right. Each press goes to the checked
    // service and its answer goes to the footer.
    private List<GroupedList.Row> UpkeepRows()=>Upkeep.Report(Ship).Select(r=>{var row=r;
        return new GroupedList.Row(UpkeepRules.Group(Upkeep.Needs(row),row.Tunable),row.Id,Upkeep.RowText(row),()=>{upkeepSelected=row.Id;BuildUpkeep();});}).ToList();
    private void BuildUpkeep()
    {
        W.Clear(shell.List);W.Clear(shell.Detail);W.Clear(shell.Actions);status=null;
        C.Heading(shell.List,C.Text(UpkeepView));
        foreach(var kind in Upkeep.Kinds)
        {
            var k=kind;bool on=Upkeep.Enabled(k);string word=Upkeep.Word(k);
            var row=C.Row(shell.List,C.ControlHeight);var label=C.Label(row,C.Text("upkeep_"+word)+": "+C.Text(on?"upkeep_on":"upkeep_off"));C.Fixed(label);
            var button=C.Button(row,C.Text(on?"upkeep_turn_off":"upkeep_turn_on"),()=>{shell.Notice.text=Upkeep.Set(k,!on);BuildUpkeep();});C.Size(button.transform,C.ControlHeight,120);
        }
        C.Heading(shell.List,C.Text("upkeep_machines"));
        rows=W.Rect(shell.List,"Rows");var group=rows.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlWidth=group.childControlHeight=true;group.childForceExpandHeight=false;
        list=new GroupedList(rows,Folded(UpkeepView));
        var upkeepRows=UpkeepRows();listSignature=GroupedList.Signature(upkeepRows);
        list.Render(GroupLabels(new[]{UpkeepRules.AttentionGroup,UpkeepRules.FineGroup,UpkeepRules.InspectOnlyGroup},"group_",Tones.ForUpkeepGroup),upkeepRows,upkeepSelected,BuildUpkeep);
        if(upkeepRows.Count==0)C.Label(rows,Text.Get("Upkeep.report_none"));
        string selected=upkeepSelected.Length>0?upkeepSelected:Focused?focusId:"";
        if(CrewWork.Resolve(selected) is CondOwner machine&&Upkeep.FamilyOf(machine.strCODef)!=null)
        {
            shell.Page(true);upkeepSelected=machine.strID;Header(machine);
            status=C.Status(shell.Detail,Upkeep.MachineReport(machine),Tones.ForUpkeep(Upkeep.Needs(machine)));
            if(!Focused)C.Button(shell.Detail,C.Text("back"),()=>{upkeepSelected="";BuildUpkeep();});
        }
        else
        {
            shell.Page(false);upkeepSelected="";C.Heading(shell.Detail,C.Text(UpkeepView));C.Label(shell.Detail,C.Text("upkeep_line"));
            var groups=upkeepRows.Select(r=>r.Group).ToList();
            C.Label(shell.Detail,C.Text("upkeep_summary",groups.Count(g=>g==UpkeepRules.AttentionGroup),groups.Count(g=>g==UpkeepRules.FineGroup),groups.Count(g=>g==UpkeepRules.InspectOnlyGroup)));
            Help.About(shell.Detail,"operations-upkeep",shell.Notice);
        }
    }
    // The Time-skip estimate (redrawn in 0.117.0): one column, the crew's coming shifts as runs, then the orders in
    // groups: what will run, what waits and why, what pauses for the skip.
    private void BuildSkip()
    {
        shell.Page(true);C.Heading(shell.Detail,C.Text("skip"));C.Label(shell.Detail,C.Text("skip_line",previewHours));Help.About(shell.Detail,"operations-time-skips",shell.Notice);
        var preview=CrewSkip.PreviewRows(Ship!,previewHours);
        C.Heading(shell.Detail,C.Text("crew"));
        foreach(var (name,availability,runs) in preview.Crew)C.Label(shell.Detail,name+"\n"+availability+" · "+runs);
        C.Heading(shell.Detail,C.Text("onboard"));
        if(preview.Machines.Count==0)C.Label(shell.Detail,C.Text("no_onboard_work"));
        rows=W.Rect(shell.Detail,"Rows");var group=rows.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlWidth=group.childControlHeight=true;group.childForceExpandHeight=false;
        list=new GroupedList(rows,Folded("skip"));
        var skipRows=preview.Machines.Select(m=>new GroupedList.Row(m.Group,m.Id,m.Name+"\n"+m.Detail,()=>{})).ToList();
        list.Render(GroupLabels(CrewSkip.PreviewGroups,"skip_group_",Tones.ForSkipGroup),skipRows,null,()=>{RememberView();BuildView();RestoreView();});
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
        // Lists refresh their text in place; a machine that changed group redraws the list, keeping the scroll.
        if(list!=null&&(view==OrdersView||view==UpkeepView))
        {
            var current=view==OrdersView?OrderRows():UpkeepRows();
            string signature=GroupedList.Signature(current);
            if(signature!=listSignature){if(view==OrdersView)Populate();else BuildUpkeep();}
            else foreach(var row in current)list.Refresh(row.Id,row.Text);
        }
        RefreshTabs();RefreshActions();
        foreach(var entry in summaries){var co=CrewWork.Resolve(entry.Key);entry.Value.text=co==null?C.Text("unavailable"):Summary(co);}
        if(status!=null){var co=view==OrdersView?CrewWork.Resolve(equipmentId.Length>0?equipmentId:focusId):view==UpkeepView?CrewWork.Resolve(upkeepSelected):null;var actor=view=="crew"?CrewWork.Resolve(crewId):null;
            status.text=co!=null?(view==UpkeepView?Upkeep.MachineReport(co):CrewWork.Provider(co)!=null?StatusText(co):C.Text("orders_none_card")+"\n"+LoadingText(co)):actor!=null?Availability(actor):C.Text("unavailable");
            // The card's tint follows its words (Framework 0.118.0).
            C.Retint(status,co==null?Tone.Neutral:view==UpkeepView?Tones.ForUpkeep(Upkeep.Needs(co)):CrewWork.Provider(co)!=null?Tones.ForOrder(CrewWork.ReadStatus(co).State):Tone.Neutral);}
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
        var button=C.Button(holder,C.Text("roster_shortcut"),()=>CrewPanel.Show(),28); W.Fill((RectTransform)button.transform);
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
