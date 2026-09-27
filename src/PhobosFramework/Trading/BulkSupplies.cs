using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Persistence;
using C=Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W=Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace Phobos.Ostranauts.Framework.Trading;
public sealed class BulkSupplyOffer
{
    public readonly string Id,Label,Unit;
    public readonly double UnitPrice,Increment;
    public readonly int MaximumSteps;
    public BulkSupplyOffer(string id,string label,string unit,double price,double increment,int maxSteps){Id=id;Label=label;Unit=unit;UnitPrice=price;Increment=increment;MaximumSteps=maxSteps;if(string.IsNullOrWhiteSpace(id)||!BulkPurchaseQuote.Finite(price)||!BulkPurchaseQuote.Finite(increment)||price<=0||increment<=0||maxSteps<1)throw new ArgumentException("Invalid bulk offer");}
}
/// <summary>Content owns eligibility, quantity, capacity and measured delivery; no UI mutation.</summary>
public interface IBulkSupplyProvider
{
    string Id { get; }
    IEnumerable<BulkSupplyOffer> Offers { get; }
    IEnumerable<CondOwner> Destinations(Ship ship,BulkSupplyOffer offer);
    string Revision(CondOwner destination,BulkSupplyOffer offer);
    double Available(CondOwner destination,BulkSupplyOffer offer);
    bool Validate(CondOwner destination,BulkPurchaseQuote quote,out string reason);
    double Deliver(CondOwner destination,BulkPurchaseQuote quote);
}
public static class BulkSupplies
{
    private static readonly Dictionary<string,IBulkSupplyProvider> providers=new(StringComparer.Ordinal);
    public static void Register(IBulkSupplyProvider provider){if(providers.ContainsKey(provider.Id))throw new ArgumentException("Duplicate bulk provider");providers.Add(provider.Id,provider);}
    public static void Unregister(string id)=>providers.Remove(id);
    internal static IEnumerable<(IBulkSupplyProvider Provider,BulkSupplyOffer Offer)> Offers=>providers.Values.SelectMany(p=>p.Offers.Select(o=>(p,o)));
    internal static string Message(string key,params object[] args)=>Text.Get("Bulk."+key,args);
}

/// <summary>Additive native entry. It never initializes native fuel rows or calls OnSubmit.</summary>
[HarmonyPatch(typeof(GUIStationRefuel),"SetupFields")]
internal static class BulkStationEntry
{
    private const string Audited="91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e";
    private static readonly Lazy<bool> compatible=new(()=>NativeAssemblyAudit.Matches(typeof(GUIStationRefuel).Assembly,BepInEx.Paths.ManagedPath,Audited));
    private static void Postfix(GUIStationRefuel __instance)
    {
        if(!compatible.Value||!BulkSupplies.Offers.Any())return;
        var host=__instance.transform.Find("pnlScrollingList/Viewport/pnlListContent");
        if(host==null||host.Find("PhobosBulkEntry")!=null)return;
        var b=C.Button(host,BulkSupplies.Message("title"),()=>BulkStationView.Open(__instance));b.name="PhobosBulkEntry";
    }
}

internal sealed class BulkStationView:MonoBehaviour
{
    private static readonly FieldInfo ShipField=AccessTools.Field(typeof(GUIStationRefuel),"ship");
    private GUIStationRefuel native=null!;
    private ConsoleShell shell=null!;
    private IBulkSupplyProvider? provider;
    private BulkSupplyOffer? offer;
    private CondOwner? destination;
    private BulkPurchaseQuote? quote;
    private CondOwner actor=null!,terminal=null!;
    private Ship ship=null!;
    private int steps=1;
    private TMPro.TMP_Text summary=null!;
    internal static void Open(GUIStationRefuel source)
    {
        if(source.transform.Find("PhobosBulkView")!=null)return;
        var root=W.Rect(source.transform,"PhobosBulkView");W.Fill(root);
        root.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var view=root.gameObject.AddComponent<BulkStationView>();view.native=source;
        view.terminal=source.COSelf;view.actor=view.terminal?.GetInteractionCurrent()?.objThem!;view.ship=(Ship)ShipField.GetValue(source);
        view.shell=ConsoleShell.Create(root,BulkSupplies.Message("title"),C.Green);view.shell.SelectionOrigin=view.terminal;
        view.shell.CancelOverlay=view.Close;
        C.Button(view.shell.Navigation,C.Text("back"),()=>{if(view.shell.IsNarrow&&view.provider!=null){view.provider=null;view.offer=null;view.Build();}else view.Close();});
        C.Button(view.shell.Navigation,C.Text("close"),view.Close);
        foreach(var entry in BulkSupplies.Offers){var selected=entry;C.Button(view.shell.List,entry.Offer.Label,()=>{view.provider=selected.Provider;view.offer=selected.Offer;view.destination=null;view.steps=1;view.Build();});}
        view.Build();
    }
    private bool ContextValid()=>actor!=null&&terminal!=null&&ship!=null&&!actor.bDestroyed&&!terminal.bDestroyed&&!Endgame.IsEmptyUniverse&&
        native.COSelf==terminal&&terminal.ship!=null&&terminal.GetInteractionCurrent()?.objThem==actor&&ShipField.GetValue(native)==ship&&
        terminal.ship.GetDockedShips().Contains(ship)&&CrewSim.coPlayer!=null&&CrewSim.system?.GetShipOwner(ship.strRegID)==CrewSim.coPlayer.strID&&
        !terminal.HasCond("IsLocked")&&actor.ship==terminal.ship&&TileUtils.TileRange(actor.GetPos(),terminal.GetPos("use"))<=2.5;
    private void Build()
    {
        W.Clear(shell.Detail);W.Clear(shell.Actions);quote=null;shell.Page(true);
        if(!ContextValid()){C.Label(shell.Detail,BulkSupplies.Message("access"));C.Button(shell.Actions,C.Text("back"),Close);return;}
        if(BulkSettlement.Protected(new ObjectStateStore(actor.mapGUIPropMaps,"BulkPurchase","phobos.framework.bulk",1)))
        {C.Label(shell.Detail,BulkSupplies.Message("protected"));C.Button(shell.Actions,C.Text("back"),Close);return;}
        if(provider==null||offer==null){C.Label(shell.Detail,BulkSupplies.Message("choose"));shell.Page(false);return;}
        C.Heading(shell.Detail,offer.Label);
        C.Field(shell.Detail,BulkSupplies.Message("destination"),destination==null?C.Text("not_selected"):ObjectPresentation.Name(destination),
            ()=>ObjectPicker.Show(shell,BulkSupplies.Message("destination"),()=>provider.Destinations(ship,offer),c=>{destination=c;Build();}),
            ()=>ObjectPicker.Locate(shell,destination),()=>{destination=null;Build();},destination!=null,destination!=null);
        C.Stepper(shell.Detail,BulkSupplies.Message("quantity",offer.Increment,offer.Unit),steps,1,offer.MaximumSteps,n=>{steps=n;Quote();});
        if(offer.MaximumSteps>1)C.Button(shell.Detail,BulkSupplies.Message("fill_available"),()=>
        {
            if(destination==null){shell.Notice.text=BulkSupplies.Message("choose_destination");return;}
            double available=provider.Available(destination,offer);
            if(!BulkPurchaseQuote.Finite(available)||available<offer.Increment){shell.Notice.text=BulkSupplies.Message("no_capacity");return;}
            steps=(int)Math.Min(offer.MaximumSteps,Math.Floor((available+1e-8)/offer.Increment));Build();
        });
        summary=C.Label(shell.Detail,"");Quote();C.Button(shell.Actions,BulkSupplies.Message("buy"),Buy);C.Button(shell.Actions,C.Text("back"),Close);
    }
    private void Quote()
    {
        quote=null;if(destination==null||provider==null||offer==null){summary.text=BulkSupplies.Message("choose_destination");return;}
        try
        {
            double amount=steps*offer.Increment;
            quote=new BulkPurchaseQuote(actor.strID,terminal.ship.strRegID,ship.strRegID,destination.strID,offer.Id,provider.Revision(destination,offer),amount,offer.UnitPrice);
            summary.text=BulkSupplies.Message("quote",amount,offer.Unit,quote.Total,provider.Available(destination,offer));
        }
        catch{summary.text=BulkSupplies.Message("protected");}
    }
    private void Buy()
    {
        if(quote==null||provider==null||destination==null){shell.Notice.text=BulkSupplies.Message("choose_destination");return;}
        var target=new SettlementTarget(this,provider,destination);
        var receipt=BulkSettlement.Buy(quote,target,new ObjectStateStore(actor.mapGUIPropMaps,"BulkPurchase","phobos.framework.bulk",1));
        shell.Notice.text=receipt.Success?BulkSupplies.Message(receipt.Reason,receipt.Delivered,receipt.Paid):receipt.Reason.StartsWith("!",StringComparison.Ordinal)?receipt.Reason.Substring(1):BulkSupplies.Message(receipt.Reason);
        // Completed operation cannot be replayed. A new explicit quantity/destination choice creates another quote.
        if(receipt.Success)quote=null;
    }
    private void Close(){gameObject.SetActive(false);Destroy(gameObject);}
    private sealed class SettlementTarget:IBulkSettlementTarget
    {
        private readonly BulkStationView view;private readonly IBulkSupplyProvider provider;private readonly CondOwner destination;
        internal SettlementTarget(BulkStationView view,IBulkSupplyProvider provider,CondOwner destination){this.view=view;this.provider=provider;this.destination=destination;}
        public double Balance=>view.actor.GetCondAmount("StatUSD");
        public bool Validate(BulkPurchaseQuote q,out string reason)
        {reason="access";if(!view.ContextValid()||q.Actor!=view.actor.strID||q.Station!=view.terminal.ship.strRegID||q.Ship!=view.ship.strRegID||q.Destination!=destination.strID||destination.ship!=view.ship)return false;
            if(!provider.Validate(destination,q,out var detail)){reason="!"+detail;return false;}return true;}
        public void Debit(double amount){double before=Balance;view.actor.AddCondAmount("StatUSD",-amount);if(Math.Abs(Balance-(before-amount))>1e-6)throw new InvalidOperationException("Uncertain debit");}
        public void Refund(double amount){double before=Balance;view.actor.AddCondAmount("StatUSD",amount);if(Math.Abs(Balance-before-amount)>1e-6)throw new InvalidOperationException("Uncertain refund");}
        public double Deliver(BulkPurchaseQuote q)=>provider.Deliver(destination,q);
        public void RecordPaid(BulkPurchaseQuote q,double delivered,double paid)
        {
            var line=new LedgerLI(q.Station+DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX"),q.Actor,(float)paid,BulkSupplies.Message("ledger",view.offer!.Label,delivered),"PhobosBulk."+q.Operation){fTimePaid=StarSystem.fEpoch};
            if(!line.Paid)throw new InvalidOperationException("Invalid ledger epoch");Ledger.AddLI(line);
        }
    }
}
