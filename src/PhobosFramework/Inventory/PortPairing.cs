using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

public enum PortRole { Sender, Receiver }
public enum PortLinkState { Unlinked, Linked, Invalid }

/// <summary>A logical port on one persistent game object. Pass that object's mapGUIPropMaps.</summary>
public sealed class MaterialPort
{
    internal const string Prefix = "PhobosMaterialPort.";
    internal Dictionary<string, Dictionary<string, string>> Maps { get; }
    internal string MapKey => Prefix + PortId;
    public string ObjectId { get; }
    public string PortId { get; }
    public MaterialPort(string objectId, string portId, Dictionary<string, Dictionary<string, string>> propertyMaps)
    {
        if (!SafeValue(objectId)) throw new ArgumentException("A persistent object ID is required.", nameof(objectId));
        if (!SafePortId(portId))
            throw new ArgumentException("Use a stable namespaced port ID containing letters, digits, dots, underscores or hyphens.", nameof(portId));
        ObjectId = objectId; PortId = portId; Maps = propertyMaps ?? throw new ArgumentNullException(nameof(propertyMaps));
    }
    // Keep identities usable in console commands and native property-change strings too.
    internal static bool SafeValue(string? value) => !string.IsNullOrWhiteSpace(value) && value!.Length <= 200 &&
        !value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c == ',' || c == '=');
    internal static bool SafePortId(string? value) => !string.IsNullOrEmpty(value) && value!.Length <= 100 &&
        value.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-');
}

/// <summary>An immutable snapshot. Invalid/future data is occupied until explicitly unlinked.</summary>
public sealed class PortLink
{
    public PortLinkState State { get; }
    public PortRole Role { get; }
    public string PairId { get; }
    public string PeerObjectId { get; }
    public string PeerPortId { get; }
    internal PortLink(PortLinkState state, PortRole role = PortRole.Sender, string pairId = "", string peer = "", string peerPort = "")
    { State = state; Role = role; PairId = pairId; PeerObjectId = peer; PeerPortId = peerPort; }
}

/// <summary>
/// Reciprocal one-to-one pairing, saved in namespaced native property maps like electrical links.
/// No game mutation beyond those maps: consumers own access, endpoint resolution, routes and moves.
/// Use on the game's main thread. A link is permission to select endpoints, never permission to move cargo.
/// </summary>
public static class PortPairing
{
    public static PortLink Read(MaterialPort port)
    {
        if (!port.Maps.TryGetValue(port.MapKey, out var map)) return new PortLink(PortLinkState.Unlinked);
        if (map == null) return new PortLink(PortLinkState.Invalid);
        string Value(string key) => map.TryGetValue(key, out var value) ? value : "";
        string role = Value("role"), peer = Value("peer"), peerPort = Value("peerPort"), pair = Value("pair");
        if (Value("schema") != "1" || Value("owner") != port.ObjectId || Value("port") != port.PortId ||
            (role != "send" && role != "receive") || !MaterialPort.SafeValue(peer) || peer == port.ObjectId ||
            !MaterialPort.SafePortId(peerPort) || !Guid.TryParseExact(pair, "N", out _)) return new PortLink(PortLinkState.Invalid);
        return new PortLink(PortLinkState.Linked, role == "send" ? PortRole.Sender : PortRole.Receiver, pair, peer, peerPort);
    }

    public static bool Matches(MaterialPort sender, MaterialPort receiver)
    {
        var a = Read(sender); var b = Read(receiver);
        return a.Role == PortRole.Sender && b.Role == PortRole.Receiver && Reciprocal(sender, a, receiver, b);
    }
    private static bool Reciprocal(MaterialPort aPort, PortLink a, MaterialPort bPort, PortLink b) =>
        a.State == PortLinkState.Linked && b.State == PortLinkState.Linked && a.Role != b.Role &&
        a.PairId == b.PairId && a.PeerObjectId == bPort.ObjectId && b.PeerObjectId == aPort.ObjectId &&
        a.PeerPortId == bPort.PortId && b.PeerPortId == aPort.PortId;

    public static bool TryLink(MaterialPort sender, MaterialPort receiver, out string problem)
    {
        problem = "";
        if (sender.ObjectId == receiver.ObjectId || ReferenceEquals(sender.Maps, receiver.Maps))
        { problem = "A material route needs two different objects."; return false; }
        if (Matches(sender, receiver)) return true;
        if (Read(sender).State != PortLinkState.Unlinked || Read(receiver).State != PortLinkState.Unlinked)
        { problem = "An endpoint already has a saved link. Unlink it before choosing a new pair."; return false; }
        string pair = Guid.NewGuid().ToString("N");
        var send = Record(sender, receiver, pair, "send");
        var receive = Record(receiver, sender, pair, "receive");
        sender.Maps.Add(sender.MapKey, send);
        try { receiver.Maps.Add(receiver.MapKey, receive); }
        catch { sender.Maps.Remove(sender.MapKey); throw; }
        return true;
    }
    private static Dictionary<string, string> Record(MaterialPort owner, MaterialPort peer, string pair, string role) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["schema"] = "1", ["owner"] = owner.ObjectId, ["port"] = owner.PortId,
            ["role"] = role, ["pair"] = pair, ["peer"] = peer.ObjectId, ["peerPort"] = peer.PortId
        };

    /// <summary>Clear this endpoint, and the peer only if it still reciprocates this exact pair.</summary>
    public static void Unlink(MaterialPort endpoint, MaterialPort? peer = null)
    {
        if (peer != null && Reciprocal(endpoint, Read(endpoint), peer, Read(peer))) peer.Maps.Remove(peer.MapKey);
        endpoint.Maps.Remove(endpoint.MapKey);
    }

    /// <summary>For display only. Resolution and persistence always use the full object ID.</summary>
    public static string ShortId(string objectId) => objectId.Length <= 8 ? objectId : objectId.Substring(0, 8);
}
