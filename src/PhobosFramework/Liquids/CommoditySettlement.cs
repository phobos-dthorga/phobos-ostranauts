using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>What a charge does to one commodity: draws it from a vessel, deposits it into one, or needs a working
/// volume of it to be present (circulated through the machine and returned, so it never enters the mass balance).</summary>
public enum SettlementRole { Draw, Deposit, Circulate }

public sealed class SettlementLeg
{
    public string Commodity { get; }
    public SettlementRole Role { get; }
    public double Kg { get; }
    public SettlementLeg(string commodity, SettlementRole role, double kg)
    {
        if (string.IsNullOrWhiteSpace(commodity) || double.IsNaN(kg) || double.IsInfinity(kg) || kg <= 0) throw new ArgumentException("Invalid settlement leg.");
        Commodity = commodity; Role = role; Kg = kg;
    }
}

/// <summary>Why a vessel cannot settle its commodity now.</summary>
public enum SettlementRefusal { None, Protected, Busy, Catch, Short, Full }

/// <summary>One commodity's net effect on its vessel: what must be available first (draws plus any circulating
/// volume), and what the vessel gains or loses at settlement.</summary>
public sealed class SettlementNeed
{
    public string Commodity { get; }
    public double DrawKg { get; }
    public double DepositKg { get; }
    public double CirculateKg { get; }
    /// <summary>Deposits less draws: the vessel's change at settlement.</summary>
    public double NetKg => DepositKg - DrawKg;
    /// <summary>What must be available (above the vessel's reserve) before the charge may start or settle.</summary>
    public double NeedAvailableKg => DrawKg + CirculateKg;
    /// <summary>Room the vessel must have for the net gain.</summary>
    public double NeedHeadroomKg => Math.Max(0, NetKg);
    /// <summary>Whether settlement changes the vessel at all (a circulating volume alone does not).</summary>
    public bool Changes => Math.Abs(NetKg) > Units.MassToleranceKg;
    public SettlementNeed(string commodity, double draw, double deposit, double circulate) { Commodity = commodity; DrawKg = draw; DepositKg = deposit; CirculateKg = circulate; }
}

/// <summary>The pure part of a multi-vessel settlement: legs netted per commodity, and the checks a vessel must pass.</summary>
public static class SettlementPlan
{
    public static IReadOnlyList<SettlementNeed> Build(IEnumerable<SettlementLeg> legs)
    {
        if (legs == null) throw new ArgumentNullException(nameof(legs));
        return legs.GroupBy(l => l.Commodity, StringComparer.Ordinal).Select(g => new SettlementNeed(g.Key,
            g.Where(l => l.Role == SettlementRole.Draw).Sum(l => l.Kg), g.Where(l => l.Role == SettlementRole.Deposit).Sum(l => l.Kg),
            g.Where(l => l.Role == SettlementRole.Circulate).Sum(l => l.Kg))).ToList().AsReadOnly();
    }
    /// <summary>Why the vessel shown by <paramref name="snapshot"/> cannot settle <paramref name="need"/> now, or None.</summary>
    public static SettlementRefusal Check(SettlementNeed need, BulkVesselSnapshot snapshot, bool held)
    {
        if (need == null) throw new ArgumentNullException(nameof(need));
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (snapshot.Protected) return SettlementRefusal.Protected;
        if (held) return SettlementRefusal.Busy;
        if (snapshot.CatchKg > 1e-8) return SettlementRefusal.Catch;
        if (snapshot.AvailableKg + 1e-8 < need.NeedAvailableKg) return SettlementRefusal.Short;
        if (snapshot.HeadroomKg + 1e-8 < need.NeedHeadroomKg) return SettlementRefusal.Full;
        return SettlementRefusal.None;
    }
}

/// <summary>Settles a finished charge across several bulk vessels and the machine's tray in one fixed order, so an
/// interruption can lose mass but never create it: every vessel whose contents change opens its conversion journal;
/// the item swap commits (a blocked swap changes nothing and closes the journals); draws are applied, then deposits;
/// each journal closes. Not crash-atomic: an exception after the swap leaves the remaining journals open as evidence,
/// and those vessels stay Protected until their owner accepts them.</summary>
public static class CommoditySettlement
{
    public static DeliveryResult Commit(IReadOnlyList<(SettlementNeed Need, CondOwner Vessel)> vessels, string item, IBatchDelivery delivery)
    {
        if (vessels == null) throw new ArgumentNullException(nameof(vessels));
        if (delivery == null) throw new ArgumentNullException(nameof(delivery));
        var changing = vessels.Where(v => v.Need.Changes).Select(v => (v.Need, v.Vessel, State: BulkVessel.Read(v.Vessel))).ToList();
        foreach (var v in changing) BulkVessel.BeginConversion(v.Vessel, item, v.State.TotalKg);
        DeliveryResult result;
        try { result = BatchDelivery.Commit(delivery); }
        catch
        {
            // A swap that failed before the charge was consumed changed nothing: the journals are closed, not left as evidence.
            if (!delivery.InputConsumed) foreach (var v in changing) BulkVessel.EndConversion(v.Vessel);
            throw;
        }
        if (result != DeliveryResult.Completed) { foreach (var v in changing) BulkVessel.EndConversion(v.Vessel); return result; }
        foreach (var v in changing.Where(v => v.Need.NetKg < 0).Concat(changing.Where(v => v.Need.NetKg > 0)))
        {
            v.State.SetService(v.State.ServiceKg + v.Need.NetKg);
            BulkVessel.Save(v.Vessel, v.State);
            BulkVessel.EndConversion(v.Vessel);
        }
        return result;
    }
}
