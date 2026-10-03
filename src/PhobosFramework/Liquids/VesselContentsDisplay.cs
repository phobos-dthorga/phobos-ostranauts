using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>What a bulk vessel holds, on the game's own right-click card (Framework 0.79.0; owner request, 4 October
/// 2026). The card's number module lists every condition on the object whose definition has <c>nDisplayType 1</c> and
/// re-reads it twice a second, which is how Ship's Water's tanks show "Water: ... L". Each commodity gets one such
/// condition here, a display-only mirror of the saved record written whenever the record is saved and once per ship
/// after loading. The record stays the only truth: nothing reads a mirror back, and a mirror never feeds a kiosk, a
/// transfer or another mod (its name is Phobos' own, not a native stat). Contents a damaged vessel holds back for
/// recovery show on a second, shared row. A condition at zero is removed by the game, so an empty vessel shows no
/// row, as an empty Ship's Water tank does.</summary>
public static class VesselContentsDisplay
{
    public const string Prefix = "StatPhobosVessel";
    /// <summary>The shared row for contents held back in a damaged vessel until it is repaired.</summary>
    public const string Trapped = Prefix + "Trapped";
    public const string Unit = " kg";
    /// <summary>The game's card shows conditions of this display type as a labelled number.</summary>
    public const int NumberDisplay = 1;
    /// <summary>The display level Ship's Water's own tank stats use: shown as a number, never in social reveals.</summary>
    public const int CardOnly = 3;
    private const double Tolerance = 1e-9;
    private static readonly Dictionary<string, string> declared = new(StringComparer.Ordinal);
    private static readonly Cadence cadence = new(5);
    private static ConditionalWeakTable<Ship, object> settled = new();
    private static bool faultLogged;

    /// <summary>The condition that shows one commodity: <c>StatPhobosVessel</c> plus the commodity in title case,
    /// letters and digits only ("carbon dioxide" is <c>StatPhobosVesselCarbonDioxide</c>).</summary>
    public static string ConditionName(string commodity)
    {
        if (string.IsNullOrWhiteSpace(commodity)) throw new ArgumentException("A commodity is required.");
        var name = new StringBuilder(Prefix);
        bool upper = true;
        foreach (char c in commodity)
        {
            if (!char.IsLetterOrDigit(c)) { upper = true; continue; }
            name.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        if (name.Length == Prefix.Length) throw new ArgumentException("A commodity needs a letter or digit: " + commodity);
        return name.ToString();
    }

    /// <summary>The two figures a vessel shows: contents in service, and contents held back in a damaged vessel. An
    /// unreadable record (a Protected vessel) shows neither; its panel says why.</summary>
    public static (double Service, double Trapped) Rows(StoredCommodity? state) =>
        state == null ? (0, 0) : (Math.Max(0, state.ServiceKg), Math.Max(0, state.CatchKg));

    /// <summary>Whether a commodity has its row; the native checks hold every registered vessel family to it.</summary>
    public static bool IsDeclared(string commodity) => declared.ContainsKey(commodity);
    public static IReadOnlyCollection<string> Declared => declared.Keys;

    /// <summary>Says how a commodity reads on the card: its localized name and one of the game's named colours (the
    /// native gas tints for gases). Content calls this beside <see cref="BulkVessels.Register"/>; declaring the same
    /// commodity again replaces the earlier wording.</summary>
    public static void Declare(NativeDefinitions d, string commodity, string friendlyName, string color)
    {
        if (d == null || string.IsNullOrWhiteSpace(friendlyName) || string.IsNullOrWhiteSpace(color)) throw new ArgumentException("A contents row needs a name and a colour.");
        string name = ConditionName(commodity);
        d.Conditions[name] = Row(name, friendlyName, color, Text.Get("VesselContentsDisplay.desc", friendlyName));
        declared[commodity] = name;
    }

    /// <summary>The shared held-back row, published with Framework's own items.</summary>
    internal static void AddTrapped(NativeDefinitions d) =>
        d.Conditions[Trapped] = Row(Trapped, Text.Get("VesselContentsDisplay.trapped"), "Neutral", Text.Get("VesselContentsDisplay.trapped_desc"));

    private static JsonCond Row(string name, string friendly, string color, string desc) => new()
    {
        strName = name, strNameFriendly = friendly, strColor = color, strDesc = desc,
        nDisplaySelf = CardOnly, nDisplayOther = CardOnly, nDisplayType = NumberDisplay,
        fConversionFactor = 1, strDisplayBonus = Unit, bPersists = true
    };

    internal static void Reset() { declared.Clear(); settled = new ConditionalWeakTable<Ship, object>(); cadence.Invalidate(); faultLogged = false; }

    /// <summary>Writes a vessel's rows from a record just saved (<see cref="BulkVessel.Save(CondOwner, BulkVesselSpec, StoredCommodity)"/>).
    /// A display fault is logged once and never reaches the transfer that saved.</summary>
    internal static void Refresh(CondOwner co, BulkVesselSpec spec, StoredCommodity? state)
    {
        try { Write(co, spec, state); }
        catch (Exception e) { Fault(e); }
    }

    /// <summary>Writes a vessel's rows from its saved record as it stands, or clears them when it cannot be read.</summary>
    public static void Refresh(CondOwner co)
    {
        var spec = BulkVessels.Of(co);
        if (spec == null) return;
        try { Write(co, spec, BulkVessel.TryRead(co, spec)); }
        catch (Exception e) { Fault(e); }
    }

    private static void Write(CondOwner co, BulkVesselSpec spec, StoredCommodity? state)
    {
        if (co == null || co.mapConds == null) return;
        var (service, trapped) = Rows(state);
        Set(co, ConditionName(spec.Commodity), service);
        Set(co, Trapped, trapped);
    }

    // Only a change is written, so a line topping up a full store every two seconds touches nothing.
    private static void Set(CondOwner co, string name, double value)
    {
        if (Math.Abs(co.GetCondAmount(name) - value) > Tolerance) co.SetCondAmount(name, value);
    }

    private static void Fault(Exception e)
    {
        if (faultLogged) return;
        faultLogged = true;
        FrameworkLifecycle.Log(Text.Get("VesselContentsDisplay.failed", e.Message));
    }

    /// <summary>Once per loaded ship, the player's or not (a silo on a derelict or at a station shows its contents too):
    /// every vessel aboard shows what its record holds, so vessels from saves made before Framework 0.79.0 gain their
    /// rows with no migration.</summary>
    internal static void Poll()
    {
        if (declared.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.system?.dictShips == null || !cadence.Due()) return;
        foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
        {
            if (ship == null || ship.bDestroyed || (int)ship.LoadState < 2) continue;
            if (settled.TryGetValue(ship, out _)) continue;
            settled.Add(ship, new object());
            foreach (var co in ship.GetCOs(null, true, false, true))
                if (co != null && !co.bDestroyed && co.ship == ship && BulkVessels.IsVessel(co)) Refresh(co);
        }
    }
}
