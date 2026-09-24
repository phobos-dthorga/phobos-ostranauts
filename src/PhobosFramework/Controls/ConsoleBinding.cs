using System;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Session-only identity. Capture at the console, never from selected crew's destination.</summary>
public sealed class ConsoleBinding
{
    public string ConsoleId { get; }
    public string ShipId { get; }
    public string ActorId { get; }
    private ConsoleAccessFailure ended;
    public ConsoleBinding(string consoleId, string shipId, string actorId)
    {
        if (string.IsNullOrWhiteSpace(consoleId) || string.IsNullOrWhiteSpace(shipId) || string.IsNullOrWhiteSpace(actorId))
            throw new ArgumentException("Console, ship and actor identities are required.");
        ConsoleId = consoleId; ShipId = shipId; ActorId = actorId;
    }

    /// <summary>Call with freshly resolved native facts on EVERY command, including pause and pairing.</summary>
    public ConsoleAccessFailure Check(string? consoleId, string? consoleShip, string? actorId, string? actorShip,
        string? targetShip, string? ownerId, string? playerId, bool hardwareReady, bool actorReady)
    {
        if (ended != ConsoleAccessFailure.None) return ended;
        if (consoleId != ConsoleId || consoleShip != ShipId) return ended = ConsoleAccessFailure.ConsoleMoved;
        if (actorId != ActorId || actorShip != ShipId) return ended = ConsoleAccessFailure.OperatorChanged;
        if (targetShip != ShipId) return ConsoleAccessFailure.OtherShip;
        if (string.IsNullOrEmpty(playerId) || ownerId != playerId) return ConsoleAccessFailure.NotOwned;
        if (!actorReady) return ConsoleAccessFailure.OperatorUnavailable;
        return hardwareReady ? ConsoleAccessFailure.None : ConsoleAccessFailure.ConsoleUnavailable;
    }
}

public enum ConsoleAccessFailure { None, ConsoleMoved, OperatorChanged, OtherShip, NotOwned, OperatorUnavailable, ConsoleUnavailable }

/// <summary>Machine-independent UI state; localized prose must never drive filtering.</summary>
public enum EquipmentState { Paused, Running, Waiting, Blocked, Ready, Unavailable }

public readonly struct EquipmentActivity
{
    public EquipmentState State { get; }
    public string Detail { get; }
    public bool NeedsAttention => State == EquipmentState.Blocked || State == EquipmentState.Unavailable;
    public EquipmentActivity(EquipmentState state, string detail) { State = state; Detail = detail; }
}
