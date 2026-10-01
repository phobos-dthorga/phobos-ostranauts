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
/// <summary>The station's buy-back side (Framework 0.68.0; owner decision of 1 October 2026). Content owns which
/// stores may sell, what each holds above its reserve and the measured withdrawal; an offer's price is what the
/// station pays, <see cref="BulkSupplies.BuybackShare"/> of its own selling price, so buying and selling back always
/// loses. No UI mutation.</summary>
public interface IBulkBuybackProvider
{
    string Id { get; }
    IEnumerable<BulkSupplyOffer> Buybacks { get; }
    IEnumerable<CondOwner> Sources(Ship ship,BulkSupplyOffer offer);
    string Revision(CondOwner source,BulkSupplyOffer offer);
    double Sellable(CondOwner source,BulkSupplyOffer offer);
    bool ValidateSale(CondOwner source,BulkPurchaseQuote quote,out string reason);
    double Withdraw(CondOwner source,BulkPurchaseQuote quote);
}
public static class BulkSupplies
{
    /// <summary>The share of its own selling price the refuelling kiosk pays for bulk it buys back, inside the game's own
    /// kiosk buying range of 0.4 to 0.5 (owner decision, 1 October 2026).</summary>
    public const double BuybackShare=0.45;
    public static double BuybackPrice(double sellingPrice)=>sellingPrice*BuybackShare;
    private static readonly Dictionary<string,IBulkSupplyProvider> providers=new(StringComparer.Ordinal);
    private static readonly Dictionary<string,IBulkBuybackProvider> buyers=new(StringComparer.Ordinal);
    public static void Register(IBulkSupplyProvider provider){if(providers.ContainsKey(provider.Id))throw new ArgumentException("Duplicate bulk provider");providers.Add(provider.Id,provider);}
    public static void RegisterBuyback(IBulkBuybackProvider provider){if(buyers.ContainsKey(provider.Id))throw new ArgumentException("Duplicate bulk buy-back provider");buyers.Add(provider.Id,provider);}
    /// <summary>Removes the owner's supply and buy-back providers alike.</summary>
    public static void Unregister(string id){providers.Remove(id);buyers.Remove(id);}
    internal static IEnumerable<(IBulkSupplyProvider Provider,BulkSupplyOffer Offer)> Offers=>providers.Values.SelectMany(p=>p.Offers.Select(o=>(p,o)));
    internal static IEnumerable<(IBulkBuybackProvider Provider,BulkSupplyOffer Offer)> Buybacks=>buyers.Values.SelectMany(p=>p.Buybacks.Select(o=>(p,o)));
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
        if(!compatible.Value||!BulkSupplies.Offers.Any()&&!BulkSupplies.Buybacks.Any())return;
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
    // The buy-back side (Framework 0.68.0): set instead of provider when the player chose to sell.
    private IBulkBuybackProvider? buyer;
    private BulkSupplyOffer? offer;
    private CondOwner? destination;
    private BulkPurchaseQuote? quote;
    private CondOwner actor=null!,terminal=null!;
    private Ship ship=null!;
    private int steps=1;
    private TMPro.TMP_Text summary=null!;
    private bool Selling=>buyer!=null;
    private ObjectStateStore PurchaseJournal=>new(actor.mapGUIPropMaps,"BulkPurchase","phobos.framework.bulk",1);
    private ObjectStateStore SaleJournal=>new(actor.mapGUIPropMaps,"BulkSale","phobos.framework.bulk",1);
    internal static void Open(GUIStationRefuel source)
    {
        if(source.transform.Find("PhobosBulkView")!=null)return;
        var root=W.Rect(source.transform,"PhobosBulkView");W.Fill(root);
        root.gameObject.AddComponent<Image>().color=new Color(.075f,.095f,.115f);
        var view=root.gameObject.AddComponent<BulkStationView>();view.native=source;
        view.terminal=source.COSelf;view.actor=view.terminal?.GetInteractionCurrent()?.objThem!;view.ship=(Ship)ShipField.GetValue(source);
        view.shell=ConsoleShell.Create(root,BulkSupplies.Message("title"),C.Green);view.shell.SelectionOrigin=view.terminal;
        view.shell.CancelOverlay=view.Close;
        C.Button(view.shell.Navigation,C.Text("back"),()=>{if(view.shell.IsNarrow&&view.offer!=null){view.provider=null;view.buyer=null;view.offer=null;view.Build();}else view.Close();});
        C.Button(view.shell.Navigation,C.Text("close"),view.Close);
        foreach(var entry in BulkSupplies.Offers){var selected=entry;C.Button(view.shell.List,entry.Offer.Label,()=>{view.provider=selected.Provider;view.buyer=null;view.offer=selected.Offer;view.destination=null;view.steps=1;view.Build();});}
        var buybacks=BulkSupplies.Buybacks.ToArray();
        if(buybacks.Length>0)C.Heading(view.shell.List,BulkSupplies.Message("sell_heading",BulkSupplies.BuybackShare));
        foreach(var entry in buybacks){var selected=entry;C.Button(view.shell.List,BulkSupplies.Message("sell_entry",entry.Offer.Label),()=>{view.provider=null;view.buyer=selected.Provider;view.offer=selected.Offer;view.destination=null;view.steps=1;view.Build();});}
        view.Build();
    }
    private bool ContextValid()=>actor!=null&&terminal!=null&&ship!=null&&!actor.bDestroyed&&!terminal.bDestroyed&&!Endgame.IsEmptyUniverse&&
        native.COSelf==terminal&&terminal.ship!=null&&terminal.GetInteractionCurrent()?.objThem==actor&&ShipField.GetValue(native)==ship&&
        terminal.ship.GetDockedShips().Contains(ship)&&CrewSim.coPlayer!=null&&CrewSim.system?.GetShipOwner(ship.strRegID)==CrewSim.coPlayer.strID&&
        !terminal.HasCond("IsLocked")&&actor.ship==terminal.ship&&TileUtils.TileRange(actor.GetPos(),terminal.GetPos("use"))<=2.5;
    private IEnumerable<CondOwner> Candidates()=>buyer!=null?buyer.Sources(ship,offer!):provider!.Destinations(ship,offer!);
    /// <summary>What the chosen store can take (buying) or give (selling).</summary>
    private double Room(CondOwner c)=>buyer!=null?buyer.Sellable(c,offer!):provider!.Available(c,offer!);
    private string Revision(CondOwner c)=>buyer!=null?buyer.Revision(c,offer!):provider!.Revision(c,offer!);
    private void Build()
    {
        W.Clear(shell.Detail);W.Clear(shell.Actions);quote=null;shell.Page(true);
        if(!ContextValid()){C.Label(shell.Detail,BulkSupplies.Message("access"));C.Button(shell.Actions,C.Text("back"),Close);return;}
        if(BulkSettlement.Protected(PurchaseJournal)||BulkSettlement.Protected(SaleJournal))
        {C.Label(shell.Detail,BulkSupplies.Message("protected"));C.Button(shell.Actions,C.Text("back"),Close);return;}
        if(provider==null&&buyer==null||offer==null){C.Label(shell.Detail,BulkSupplies.Message("choose"));shell.Page(false);return;}
        C.Heading(shell.Detail,Selling?BulkSupplies.Message("sell_entry",offer.Label):offer.Label);
        string pick=BulkSupplies.Message(Selling?"source":"destination");
        C.Field(shell.Detail,pick,destination==null?C.Text("not_selected"):ObjectPresentation.Name(destination),
            ()=>ObjectPicker.Show(shell,pick,Candidates,c=>{destination=c;Build();}),
            ()=>ObjectPicker.Locate(shell,destination),()=>{destination=null;Build();},destination!=null,destination!=null);
        C.Stepper(shell.Detail,BulkSupplies.Message("quantity",offer.Increment,offer.Unit),steps,1,offer.MaximumSteps,n=>{steps=n;Quote();});
        if(offer.MaximumSteps>1)C.Button(shell.Detail,BulkSupplies.Message(Selling?"sell_available":"fill_available"),()=>
        {
            if(destination==null){shell.Notice.text=BulkSupplies.Message(Selling?"choose_source":"choose_destination");return;}
            double available=Room(destination);
            if(!BulkPurchaseQuote.Finite(available)||available<offer.Increment){shell.Notice.text=BulkSupplies.Message(Selling?"nothing_to_sell":"no_capacity");return;}
            steps=(int)Math.Min(offer.MaximumSteps,Math.Floor((available+1e-8)/offer.Increment));Build();
        });
        summary=C.Label(shell.Detail,"");Quote();C.Button(shell.Actions,BulkSupplies.Message(Selling?"sell":"buy"),Selling?Sell:Buy);C.Button(shell.Actions,C.Text("back"),Close);
    }
    private void Quote()
    {
        quote=null;if(destination==null||provider==null&&buyer==null||offer==null){summary.text=BulkSupplies.Message(Selling?"choose_source":"choose_destination");return;}
        try
        {
            double amount=steps*offer.Increment;
            quote=new BulkPurchaseQuote(actor.strID,terminal.ship.strRegID,ship.strRegID,destination.strID,offer.Id,Revision(destination),amount,offer.UnitPrice);
            summary.text=BulkSupplies.Message(Selling?"sale_quote":"quote",amount,offer.Unit,quote.Total,Room(destination));
        }
        catch{summary.text=BulkSupplies.Message("protected");}
    }
    private void Buy()
    {
        if(quote==null||provider==null||destination==null){shell.Notice.text=BulkSupplies.Message("choose_destination");return;}
        var target=new SettlementTarget(this,provider,destination);
        var receipt=BulkSettlement.Buy(quote,target,PurchaseJournal);
        shell.Notice.text=receipt.Success?BulkSupplies.Message(receipt.Reason,receipt.Delivered,receipt.Paid):receipt.Reason.StartsWith("!",StringComparison.Ordinal)?receipt.Reason.Substring(1):BulkSupplies.Message(receipt.Reason);
        // Completed operation cannot be replayed. A new explicit quantity/destination choice creates another quote.
        if(receipt.Success)quote=null;
    }
    private void Sell()
    {
        if(quote==null||buyer==null||destination==null){shell.Notice.text=BulkSupplies.Message("choose_source");return;}
        var receipt=BulkSettlement.Sell(quote,new SaleTarget(this,buyer,destination),SaleJournal);
        shell.Notice.text=receipt.Success?BulkSupplies.Message(receipt.Reason,receipt.Delivered,receipt.Paid):receipt.Reason.StartsWith("!",StringComparison.Ordinal)?receipt.Reason.Substring(1):BulkSupplies.Message(receipt.Reason);
        // As for a purchase: a completed sale cannot be replayed, and the store's new level needs a new quote.
        if(receipt.Success)Build();
    }
    private void Close(){gameObject.SetActive(false);Destroy(gameObject);}
    private bool QuoteMatches(BulkPurchaseQuote q,CondOwner destination)=>ContextValid()&&q.Actor==actor.strID&&q.Station==terminal.ship.strRegID&&q.Ship==ship.strRegID&&q.Destination==destination.strID&&destination.ship==ship;
    private sealed class SettlementTarget:IBulkSettlementTarget
    {
        private readonly BulkStationView view;private readonly IBulkSupplyProvider provider;private readonly CondOwner destination;
        internal SettlementTarget(BulkStationView view,IBulkSupplyProvider provider,CondOwner destination){this.view=view;this.provider=provider;this.destination=destination;}
        public double Balance=>view.actor.GetCondAmount("StatUSD");
        public bool Validate(BulkPurchaseQuote q,out string reason)
        {reason="access";if(!view.QuoteMatches(q,destination))return false;
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
    private sealed class SaleTarget:IBulkSaleTarget
    {
        private readonly BulkStationView view;private readonly IBulkBuybackProvider buyer;private readonly CondOwner source;
        internal SaleTarget(BulkStationView view,IBulkBuybackProvider buyer,CondOwner source){this.view=view;this.buyer=buyer;this.source=source;}
        public bool Validate(BulkPurchaseQuote q,out string reason)
        {reason="access";if(!view.QuoteMatches(q,source))return false;
            if(!buyer.ValidateSale(source,q,out var detail)){reason="!"+detail;return false;}return true;}
        public double Withdraw(BulkPurchaseQuote q)=>buyer.Withdraw(source,q);
        public void Credit(double amount){double before=view.actor.GetCondAmount("StatUSD");view.actor.AddCondAmount("StatUSD",amount);if(Math.Abs(view.actor.GetCondAmount("StatUSD")-before-amount)>1e-6)throw new InvalidOperationException("Uncertain credit");}
        public void RecordPaid(BulkPurchaseQuote q,double sold,double paid)
        {
            // The station pays the player: the payee and payor of a purchase line, reversed.
            var line=new LedgerLI(q.Actor,q.Station+DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX"),(float)paid,BulkSupplies.Message("sale_ledger",view.offer!.Label,sold),"PhobosBulkSale."+q.Operation){fTimePaid=StarSystem.fEpoch};
            if(!line.Paid)throw new InvalidOperationException("Invalid ledger epoch");Ledger.AddLI(line);
        }
    }
}
