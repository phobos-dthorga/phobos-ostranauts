using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>A station Bulk supplies provider over registered bulk vessel families: one offer per family, with
/// the content-declared price, step and cap. Eligible destinations are installed, ready, unprotected vessels
/// with nothing in the catch chamber; delivery is the measured change of the vessel's own record. Station
/// stock is not simulated as finite; the offer's steps bound one quote. Offers are enumerated on each use so
/// labels follow the selected language and content readiness.</summary>
public sealed class VesselSupplyProvider : IBulkSupplyProvider
{
    private readonly Func<IEnumerable<(BulkSupplyOffer Offer, string Family)>> offers;
    public VesselSupplyProvider(string id, Func<IEnumerable<(BulkSupplyOffer Offer, string Family)>> offers)
    {
        if (string.IsNullOrWhiteSpace(id) || offers == null) throw new ArgumentException("Invalid vessel supply provider.");
        Id = id; this.offers = offers;
    }
    public string Id { get; }
    private (BulkSupplyOffer Offer, string Family)[] Current => offers().ToArray();
    public IEnumerable<BulkSupplyOffer> Offers => Current.Select(o => o.Offer);
    private BulkVesselSpec? Spec(string offerId)
    {
        var family = Current.FirstOrDefault(o => o.Offer.Id == offerId).Family;
        return family == null ? null : BulkVessels.All.FirstOrDefault(s => s.Family == family);
    }
    public IEnumerable<CondOwner> Destinations(Ship ship, BulkSupplyOffer offer) => ship == null ? Enumerable.Empty<CondOwner>() :
        ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && Eligible(c, offer.Id)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    private bool Eligible(CondOwner c, string offerId)
    {
        var spec = Spec(offerId);
        return spec != null && EquipmentIdentity.IsFamily(c.strCODef, spec.Family) && NativeFluidRoute.EndpointReady(c) &&
            !BulkVessel.Protected(c) && BulkVessel.Read(c, spec).CatchKg == 0;
    }
    public string Revision(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(offer.Id);
        return (spec == null ? "none" : ConfigurationStamp.For(c, "PhobosState." + spec.Record, "PhobosMaterialPort.")) + ":" +
            string.Join(";", c.objContainer?.ContainedCOs.Select(x => x.strID).OrderBy(x => x, StringComparer.Ordinal) ?? Enumerable.Empty<string>());
    }
    public double Available(CondOwner c, BulkSupplyOffer offer)
    {
        var spec = Spec(offer.Id);
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
        var spec = Spec(q.Offer)!;
        var state = BulkVessel.Read(c, spec); double before = state.ServiceKg;
        state.SetService(before + q.Quantity); BulkVessel.Save(c, spec, state);
        return BulkVessel.Read(c, spec).ServiceKg - before;
    }
}
