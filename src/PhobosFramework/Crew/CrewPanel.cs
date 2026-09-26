using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Presentation only. Edits delegate to saved-order/role services.</summary>
public sealed class CrewPanel : GUIData
{
    public const string Key = "PhobosCrewPanel";
    private string shipId = "", equipmentId = "";
    private Transform content = null!;
    private TMP_Text? status;
    private float next;
    private bool preview;
    internal static void ShowPreview(Ship ship)
    {
        if(!Show(ship.ShipCO))return;
        var panel=CrewSim.goUI.GetComponent<CrewPanel>(); panel.preview=true; panel.Build();
    }
    public static void Button(Transform parent,CondOwner? co = null) => W.Button(parent,CrewWork.Message("open"),()=>Show(co));
    public static bool Show(CondOwner? co = null)
    {
        var actor=CrewSim.GetSelectedCrew(); var ship=co?.ship??actor?.ship;
        if(actor==null || ship==null || CrewSim.goIntUIPanel==null || CrewSim.bUILock || CrewSim.system?.GetShipOwner(ship.strRegID)!=CrewSim.coPlayer?.strID) return false;
        CrewSim.LowerUI(); if(CrewSim.goUI!=null) return false;
        var root=W.Rect(CrewSim.goIntUIPanel.transform,Key); W.Fill(root,30,30,30,30);
        root.gameObject.AddComponent<Image>().color=new Color(.12f,.15f,.18f);
        CrewSim.goUI=root.gameObject; var panel=root.gameObject.AddComponent<CrewPanel>();
        panel.shipId=ship.strRegID; panel.equipmentId=co!=null && CrewWork.Provider(co)!=null?co.strID:"";
        panel.Init(actor,new Dictionary<string,string>(),Key); panel.strFriendlyName=CrewWork.Message("open"); panel.bActive=true;
        CrewSim.tplLastUI=CrewSim.tplCurrentUI; CrewSim.tplCurrentUI=new global::Ostranauts.Core.Models.Tuple<string,CondOwner>(Key,actor);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); panel.Build(); return true;
    }
    private void Build()
    {
        W.Clear(transform); content=W.Scroll(transform,"Orders",out var scroll); W.Fill((RectTransform)scroll.transform,18,18,18,18);
        var ship=CrewSim.system.GetShipByRegID(shipId); if(ship==null) return;
        W.Label(content,CrewWork.Message("open"));
        W.Button(content,CrewWork.Message("overview"),()=>{equipmentId="";Build();});
        W.Button(content,CrewWork.Message("close"),()=>CrewSim.LowerUI());
        if(preview) { W.Label(content,CrewSkip.Preview(ship)); return; }
        var machine=CrewWork.Resolve(equipmentId);
        if(machine!=null) { Equipment(machine); return; }
        W.Label(content,CrewWork.Message("orders_help"));
        foreach(var co in CrewWork.Equipment(ship))
        { var id=co.strID; W.Button(content,co.strNameFriendly+" — "+CrewWork.Status(co),()=>{equipmentId=id;Build();}); }
        foreach(var actor in CrewSim.aCrew.Where(c=>c!=null && c.ship==ship))
        {
            W.Label(content,actor.FriendlyName);
            foreach(CrewRole role in Enum.GetValues(typeof(CrewRole)))
            { var r=role; W.Button(content,CrewWork.Message("role",CrewWork.Message(r.ToString()),CrewWork.Message(CrewSpecialities.Allowed(actor,r)?"allowed":"not_allowed")),()=>{CrewSpecialities.ToggleRole(actor,r);Build();}); }
            foreach(var skill in CrewSpecialities.All) W.Label(content,CrewWork.Message("training",skill.Label,CrewSpecialities.Progress(actor,skill.Id)));
        }
    }
    private void Equipment(CondOwner co)
    {
        var order=CrewWork.Order(co); var provider=CrewWork.Provider(co)!;
        W.Label(content,co.strNameFriendly); status=W.Label(content,CrewWork.Status(co));
        W.Button(content,CrewWork.Message("enable"),()=>{CrewWork.SetPermission(co,WorkPermission.Enabled);Build();});
        W.Button(content,CrewWork.Message("stop"),()=>{CrewWork.SetPermission(co,WorkPermission.Stopped);Build();});
        if(provider.RoutineResume(co))W.Button(content,CrewWork.Message("routine_resume",CrewWork.Message(order.ResumeRoutine?"allowed":"not_allowed")),
            ()=>{CrewWork.Configure(co,o=>o.ResumeRoutine=!o.ResumeRoutine);Build();});
        W.Label(content,CrewWork.Message("recipe",provider.Recipes(co).Contains(order.Recipe)?provider.RecipeLabel(order.Recipe):CrewWork.Message("choose_work")));
        foreach(var recipe in provider.Recipes(co)) { var selected=recipe; W.Button(content,provider.RecipeLabel(recipe),()=>{CrewWork.Configure(co,o=>o.Recipe=selected);Build();}); }
        W.Label(content,CrewWork.Message("stock",order.Stock));
        W.Button(content,CrewWork.Message("stock_less"),()=>{CrewWork.Configure(co,o=>o.Stock=Math.Max(1,o.Stock-1));Build();});
        W.Button(content,CrewWork.Message("stock_more"),()=>{CrewWork.Configure(co,o=>o.Stock=Math.Min(256,o.Stock+1));Build();});
        W.Button(content,CrewWork.Message("hazardous",CrewWork.Message(order.Hazardous?"allowed":"not_allowed")),()=>{CrewWork.Configure(co,o=>{o.Hazardous=!o.Hazardous;o.Permission=WorkPermission.Suspended;});Build();});
        W.Button(content,CrewWork.Message("clear_crops",CrewWork.Message(order.ClearCrops?"allowed":"not_allowed")),()=>{CrewWork.Configure(co,o=>o.ClearCrops=!o.ClearCrops);Build();});
        W.Button(content,CrewWork.Message("drain",CrewWork.Message(order.Drain?"allowed":"not_allowed")),()=>{CrewWork.Configure(co,o=>o.Drain=!o.Drain);Build();});
        W.Label(content,CrewWork.Message("stores_help"));
        W.Button(content,CrewWork.Message("clear_stores"),()=>{CrewWork.Configure(co,o=>{o.Source=o.Destination="none";});Build();});
        foreach(var store in CrewWork.Stores(co.ship))
        {
            var id=store.strID;
            W.Button(content,CrewWork.Message("source",store.strNameFriendly,id,order.Source==id?"✓":""),()=>{CrewWork.Configure(co,o=>o.Source=id);Build();});
            W.Button(content,CrewWork.Message("destination",store.strNameFriendly,id,order.Destination==id?"✓":""),()=>{CrewWork.Configure(co,o=>o.Destination=id);Build();});
        }
        W.Label(content,CrewWork.Message("target_help"));
        foreach(var ship in CrewSim.system.dictShips.Values.Where(s=>s!=co.ship &&
            (co.HasCond("IsNavStation") || CrewSim.system.GetShipOwner(s.strRegID)==CrewSim.coPlayer.strID)))
        { string id=ship.strRegID; W.Button(content,CrewWork.Message("target",ship.publicName,id,order.Target==id?"✓":""),()=>{CrewWork.Configure(co,o=>{o.Target=id;o.Permission=WorkPermission.Suspended;});Build();}); }
    }
    private void Update()
    {
        if(Time.unscaledTime<next)return; next=Time.unscaledTime+1;
        var co=CrewWork.Resolve(equipmentId); if(co!=null && status!=null)status.text=CrewWork.Status(co);
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
        var old=__instance.transform.Find("PhobosCrewPreview"); if(old!=null) UnityEngine.Object.Destroy(old.gameObject);
        var ship=CrewSim.GetSelectedCrew()?.ship;if(ship==null)return;
        var text=W.Label(__instance.transform,CrewSkip.Preview(ship),false); text.name="PhobosCrewPreview";
        var r=(RectTransform)text.transform;r.anchorMin=new Vector2(.04f,.06f);r.anchorMax=new Vector2(.72f,.18f);r.offsetMin=r.offsetMax=Vector2.zero;
        text.fontSize=14; text.overflowMode=TextOverflowModes.Ellipsis;
        var buttonOld=__instance.transform.Find("PhobosCrewPreviewButton");
        if(buttonOld!=null)UnityEngine.Object.Destroy(buttonOld.gameObject);
        var button=W.Button(__instance.transform,CrewWork.Message("preview_detail"),()=>CrewPanel.ShowPreview(ship));
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
