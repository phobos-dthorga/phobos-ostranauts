using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>A station Bulk supplies provider over registered bulk vessel families: one offer per commodity line,
/// with the content-declared price, step and cap, filling every family (every size) it names. Eligible
/// destinations are installed, ready, unprotected vessels of those families with nothing in the catch chamber;
/// delivery is the measured change of the vessel's own record. Station stock is not simulated as finite; the
/// offer's steps bound one quote. Offers are enumerated on each use so labels follow the selected language and
/// content readiness.</summary>
public sealed class VesselSupplyProvider : IBulkSupplyProvider
{
    private readonly Func<IEnumerable<(BulkSupplyOffer Offer, IReadOnlyList<string> Families)>> offers;
    /// <summary>One family per offer (the original contract, kept for compiled consumers).</summary>
    public VesselSupplyProvider(string id, Func<IEnumerable<(BulkSupplyOffer Offer, string Family)>> offers)
        : this(id, offers == null ? null! : new Func<IEnumerable<(BulkSupplyOffer, IReadOnlyList<string>)>>(() => offers().Select(o => (o.Offer, (IReadOnlyList<string>)new[] { o.Family })))) { }
    /// <summary>Several families per offer: one "bulk nitrogen" line fills small, medium and large stores alike.</summary>
    public VesselSupplyProvider(string id, Func<IEnumerable<(BulkSupplyOffer Offer, IReadOnlyList<string> Families)>> offers)
    {
        if (string.IsNullOrWhiteSpace(id) || offers == null) throw new ArgumentException("Invalid vessel supply provider.");
        Id = id; this.offers = offers;
    }
    public string Id { get; }
    private (BulkSupplyOffer Offer, IReadOnlyList<string> Families)[] Current => offers().ToArray();
    public IEnumerable<BulkSupplyOffer> Offers => Current.Select(o => o.Offer);
    /// <summary>The declaration of a destination when the offer covers its family.</summary>
    private BulkVesselSpec? Spec(CondOwner c, string offerId)
    {
        var families = Current.FirstOrDefault(o => o.Offer.Id == offerId).Families;
        var spec = BulkVessels.Of(c);
        return families == null || spec == null || !families.Contains(spec.Family, StringComparer.Ordinal) ? null : spec;
    }
    public IEnumerable<CondOwner> Destinations(Ship ship, BulkSupplyOffer offer) => ship == null ? Enumerable.Empty<CondOwner>() :
        ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && Eligible(c, offer.Id)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    private bool Eligible(CondOwner c, string offerId)
    {
        var spec = Spec(c, offerId);
        return spec != null && NativeFluidRoute.EndpointReady(c) && !BulkVessel.Protected(c) && BulkVessel.Read(c, spec).CatchKg == 0;
    }
    public string Revision(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(c, offer.Id);
        return (spec == null ? "none" : ConfigurationStamp.For(c, "PhobosState." + spec.Record, "PhobosMaterialPort.")) + ":" +
            string.Join(";", c.objContainer?.ContainedCOs.Select(x => x.strID).OrderBy(x => x, StringComparer.Ordinal) ?? Enumerable.Empty<string>());
    }
    public double Available(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(c, offer.Id);
        return spec == null || !Eligible(c, offer.Id) ? 0 : spec.CapacityKg - BulkVessel.Read(c, spec).TotalKg;
    }
    public bool Validate(CondOwner c, BulkPurchaseQuote q, out string reason)
    {
        reason = Text.Get("BulkVessel.quote_changed");
        var offer = Offers.FirstOrDefault(o => o.Id == q.Offer);
        return offer != null && Eligible(c, q.Offer) && Revision(c, offer) == q.Revision && q.UnitPrice == offer.UnitPrice &&
            q.Quantity <= offer.Increment * offer.MaximumSteps + 1e-8 &&
            Math.Abs(q.Quantity / offer.Increment - Math.Round(q.Quantity / offer.Increment)) <= 1e-8 && Available(c, offer) + 1e-8 >= q.Quantity;
    }
    public double Deliver(CondOwner c, BulkPurchaseQuote q)
    {
        if (!Validate(c, q, out _)) return 0;
        var spec = Spec(c, q.Offer)!;
        var state = BulkVessel.Read(c, spec); double before = state.ServiceKg;
        state.SetService(before + q.Quantity); BulkVessel.Save(c, spec, state);
        return BulkVessel.Read(c, spec).ServiceKg - before;
    }
}

/// <summary>The station buy-back over registered bulk vessel families (Framework 0.68.0; owner decision of 1 October
/// 2026). Each line carries the station's own selling price per unit (the game's GasPrices, or content's authored
/// price for a commodity the station does not sell) and the families it buys from; the station pays
/// <see cref="BulkSupplies.BuybackShare"/> of that price. Sources are installed, ready, unprotected stores of those
/// families with nothing in the catch chamber; what may be sold is the service contents above the store's reserve,
/// and the withdrawal is the measured change of the store's own record. Lines are enumerated on each use, as offers
/// are.</summary>
public sealed class VesselBuybackProvider : IBulkBuybackProvider
{
    /// <summary>Appended to a line's id, so a buy-back never shares an id with the matching supply offer.</summary>
    public const string Suffix = ".sell";
    private readonly Func<IEnumerable<(BulkSupplyOffer Offer, IReadOnlyList<string> Families)>> lines;
    /// <param name="lines">Each line's offer carries the station's selling price, not the buy-back price.</param>
    public VesselBuybackProvider(string id, Func<IEnumerable<(BulkSupplyOffer Offer, IReadOnlyList<string> Families)>> lines)
    {
        if (string.IsNullOrWhiteSpace(id) || lines == null) throw new ArgumentException("Invalid vessel buy-back provider.");
        Id = id; this.lines = lines;
    }
    public string Id { get; }
    private (BulkSupplyOffer Offer, IReadOnlyList<string> Families)[] Current => lines().Select(l => (new BulkSupplyOffer(l.Offer.Id + Suffix, l.Offer.Label, l.Offer.Unit,
        BulkSupplies.BuybackPrice(l.Offer.UnitPrice), l.Offer.Increment, l.Offer.MaximumSteps), l.Families)).ToArray();
    public IEnumerable<BulkSupplyOffer> Buybacks => Current.Select(o => o.Offer);
    private BulkVesselSpec? Spec(CondOwner c, string offerId)
    {
        var families = Current.FirstOrDefault(o => o.Offer.Id == offerId).Families;
        var spec = BulkVessels.Of(c);
        return families == null || spec == null || !families.Contains(spec.Family, StringComparer.Ordinal) ? null : spec;
    }
    private bool Eligible(CondOwner c, string offerId)
    {
        var spec = Spec(c, offerId);
        return spec != null && NativeFluidRoute.EndpointReady(c) && !BulkVessel.Protected(c) && BulkVessel.Read(c, spec).CatchKg == 0;
    }
    public IEnumerable<CondOwner> Sources(Ship ship, BulkSupplyOffer offer) => ship == null ? Enumerable.Empty<CondOwner>() :
        ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && Eligible(c, offer.Id) && Sellable(c, offer) > 0)
            .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    public string Revision(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(c, offer.Id);
        return (spec == null ? "none" : ConfigurationStamp.For(c, "PhobosState." + spec.Record, "PhobosMaterialPort.")) + ":" +
            string.Join(";", c.objContainer?.ContainedCOs.Select(x => x.strID).OrderBy(x => x, StringComparer.Ordinal) ?? Enumerable.Empty<string>());
    }
    /// <summary>The service contents above the store's reserve: a reserve set for the crew is never sold.</summary>
    public double Sellable(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(c, offer.Id);
        if (spec == null || !Eligible(c, offer.Id)) return 0;
        var s = BulkVessel.Read(c, spec);
        return Math.Max(0, s.ServiceKg - s.ReserveKg);
    }
    public bool ValidateSale(CondOwner c, BulkPurchaseQuote q, out string reason)
    {
        reason = Text.Get("BulkVessel.quote_changed");
        var offer = Buybacks.FirstOrDefault(o => o.Id == q.Offer);
        return offer != null && Eligible(c, q.Offer) && Revision(c, offer) == q.Revision && q.UnitPrice == offer.UnitPrice &&
            q.Quantity <= offer.Increment * offer.MaximumSteps + 1e-8 &&
            Math.Abs(q.Quantity / offer.Increment - Math.Round(q.Quantity / offer.Increment)) <= 1e-8 && Sellable(c, offer) + 1e-8 >= q.Quantity;
    }
    public double Withdraw(CondOwner c, BulkPurchaseQuote q)
    {
        if (!ValidateSale(c, q, out _)) return 0;
        var spec = Spec(c, q.Offer)!;
        var state = BulkVessel.Read(c, spec); double before = state.ServiceKg;
        state.SetService(Math.Max(0, before - q.Quantity)); BulkVessel.Save(c, spec, state);
        return before - BulkVessel.Read(c, spec).ServiceKg;
    }
}
