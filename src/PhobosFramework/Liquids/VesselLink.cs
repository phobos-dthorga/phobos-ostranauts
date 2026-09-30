using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One kind of link from a machine to a shared bulk vessel (Framework 0.57.0): the machine's port, the
/// vessel-side port (a bank of <see cref="SharedPorts.Slots"/>, so several machines share one vessel), the commodity,
/// and which way the saved pairing runs. The owner's link rule applies throughout: a vessel is a candidate, and a
/// linked vessel is usable, only while it touches the machine or shares a line network with it
/// (<see cref="LineReach"/>, the commodity's line from <see cref="LineFamilies"/>). Saved pairs from before banks read
/// as slot zero with no rewrite. Content keeps its own texts, protection checks and settlement.</summary>
public sealed class VesselLink
{
    public string MachinePort { get; }
    public string VesselPort { get; }
    public string Commodity { get; }
    /// <summary>Whether the machine side is the pairing's sender (every Manufacturing and Shipbreaker link; a vessel
    /// that feeds a machine, as Agriculture's tanks feed a W2, is the sender instead).</summary>
    public bool MachineSends { get; }
    public FluidSegmentFamily? Family => LineFamilies.For(Commodity);
    public VesselLink(string machinePort, string vesselPort, string commodity, bool machineSends = true)
    {
        if (string.IsNullOrEmpty(machinePort) || string.IsNullOrEmpty(vesselPort) || string.IsNullOrEmpty(commodity)) throw new ArgumentException("A vessel link needs both ports and a commodity.");
        MachinePort = machinePort; VesselPort = vesselPort; Commodity = commodity; MachineSends = machineSends;
    }
    public MaterialPort Ours(CondOwner machine) => new(machine.strID, MachinePort, machine.mapGUIPropMaps);
    /// <summary>The vessel-side slot the machine is saved against (the primary port when unlinked).</summary>
    public MaterialPort Theirs(CondOwner machine, CondOwner vessel) => SharedPorts.Peer(Ours(machine), vessel, VesselPort);
    public string PeerId(CondOwner machine) => PortPairing.Read(Ours(machine)).PeerObjectId;
    public LineReachKind Reach(CondOwner machine, CondOwner vessel) => LineReach.Of(machine, vessel, Family);
    /// <summary>Whether the saved pair is reciprocal (either side's record alone is not enough).</summary>
    public bool Paired(CondOwner machine, CondOwner vessel)
    {
        var ours = Ours(machine); var theirs = SharedPorts.Peer(ours, vessel, VesselPort);
        return MachineSends ? PortPairing.Matches(ours, theirs) : PortPairing.Matches(theirs, ours);
    }
    /// <summary>A linked vessel usable now: on the machine's ship, ready, reciprocally paired and in reach.</summary>
    public bool Connected(CondOwner machine, CondOwner vessel) =>
        vessel != null && machine != null && vessel.ship != null && vessel.ship == machine.ship && NativeFluidRoute.EndpointReady(vessel) &&
        Paired(machine, vessel) && Reach(machine, vessel) != LineReachKind.None;
    /// <summary>Registered vessels of the commodity the machine reaches, nearest network first is not implied.</summary>
    public IEnumerable<CondOwner> Candidates(CondOwner machine, Func<CondOwner, bool>? accepts = null) =>
        machine?.ship == null ? Enumerable.Empty<CondOwner>() :
        BulkVessels.Aboard(machine.ship, Commodity).Where(v => v != machine && (accepts == null || accepts(v)) && Reach(machine, v) != LineReachKind.None);
    /// <summary>Links the machine to a vessel slot, replacing its previous vessel. Checks of reach, access and the
    /// machine's own state belong to the caller, which passes a vessel from <see cref="Candidates"/>.</summary>
    public bool Link(CondOwner machine, CondOwner vessel, Func<string, CondOwner?> resolve, out string problem)
    {
        problem = "";
        var ours = Ours(machine);
        var current = resolve(PeerId(machine));
        if (current != null && current != vessel) SharedPorts.Unlink(ours, current, VesselPort);
        if (Paired(machine, vessel)) return true;
        return SharedPorts.TryLink(ours, vessel, VesselPort, MachineSends, resolve, out problem);
    }
    /// <summary>Clears the machine's link and its vessel slot (when that slot still points back).</summary>
    public void Unlink(CondOwner machine, Func<string, CondOwner?> resolve) => SharedPorts.Unlink(Ours(machine), resolve(PeerId(machine)), VesselPort);
}
