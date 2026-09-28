using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.Sensors;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Sensors;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Native boundary for selective sensor engagement: survey, switch, saved notes and player notices.
// Switching and notes belong to Framework's SensorLeases; guidance decisions stay in the service.
internal static class NativeSensorControl
{
    internal const string Automation = "PhobosAutoNav";
    private const string NoticeKey = "PhobosAutoNav.Sensors";

    /// <summary>Each listed contact's current signal and the signal navigation needs, and for every
    /// switched-off sensor type the player has not declined, what it would add to each contact.</summary>
    internal static void Survey(Ship own, IReadOnlyList<string> ids, string holder, out SensorNeed[] needs, out SensorOption[] options)
    {
        var types = SensorLeases.Types(own).Select(g => g.ToArray()).ToArray();
        var inputs = new List<(ShipSignature Signature, double RangeKM, float Visibility)>();
        var found = new List<SensorNeed>();
        foreach (string id in ids)
        {
            if (!NativeContactReader.TryInputs(own, id, out var signature, out double rangeKM, out float visibility, out double threshold)) continue;
            double current = own.ElectronicSystems.GetSignatureStrength(signature!, rangeKM, visibility);
            // Asteroid markers beyond the native live-track range stay partial whatever the signal.
            bool beyondTrack = CrewSim.system.GetShipByRegID(id) == null && rangeKM > ContactRules.StellarRangeKM;
            found.Add(beyondTrack ? new SensorNeed(current, double.PositiveInfinity) : SensorSelection.Need(current, threshold));
            inputs.Add((signature!, rangeKM, visibility));
        }
        var declined = SensorLeases.Declined(own, Automation, holder);
        options = types.Where(g => g.All(s => !s.On) && !declined.Contains(g[0].CondId))
            .Select(g => new SensorOption(g[0].CondId, NativeSensorSuite.TypeName(g[0].SensorType), g.Any(s => s.IsActiveSensor),
                inputs.Select(i => g.Sum(s => SensorLeases.IfOn(s, i.Signature, i.RangeKM, i.Visibility))).ToArray())).ToArray();
        needs = found.ToArray();
    }

    internal static IReadOnlyList<SensorOption> Engage(Ship own, IReadOnlyList<SensorOption> chosen, string holder)
    {
        var engaged = chosen.Where(o => SensorLeases.Engage(own, o.Key, Automation, holder)).ToArray();
        if (engaged.Length > 0) GUIOrbitDraw.Instance?.InvalidateAllShips();
        return engaged;
    }

    internal static IReadOnlyList<(string Name, bool Emits)> Release(Ship own, string holder)
    {
        var described = Describe(own, SensorLeases.Types(own).Select(g => g.Key).ToArray());
        var off = SensorLeases.Release(own, Automation, holder);
        if (off.Count > 0) GUIOrbitDraw.Instance?.InvalidateAllShips();
        return off.Select(c => described.TryGetValue(c, out var d) ? d : (c, false)).ToArray();
    }

    internal static IReadOnlyList<(string Name, bool Emits)> InUse(Ship? own, string holder)
    {
        var inUse = SensorLeases.InUse(own, Automation, holder);
        if (inUse.Count == 0) return System.Array.Empty<(string, bool)>();
        var described = Describe(own, inUse);
        return inUse.Select(c => described[c]).ToArray();
    }

    internal static IReadOnlyCollection<string> Holders(Ship own) => SensorLeases.Holders(own, Automation);
    internal static bool Decline(Ship own, string condition, string holder) => SensorLeases.Decline(own, condition, Automation, holder);
    internal static void Notify(Ship own, bool caution, string logText, string? banner) =>
        PlayerNotices.Post(own, NoticeKey, caution ? NoticeLevel.Caution : NoticeLevel.Info, logText, banner);

    private static Dictionary<string, (string Name, bool Emits)> Describe(Ship? own, IReadOnlyCollection<string> conditions)
    {
        var named = new Dictionary<string, (string Name, bool Emits)>(System.StringComparer.Ordinal);
        foreach (var group in SensorLeases.Types(own))
            if (conditions.Contains(group.Key)) named[group.Key] = (NativeSensorSuite.TypeName(group.First().SensorType), group.Any(s => s.IsActiveSensor));
        foreach (string condition in conditions) if (!named.ContainsKey(condition)) named[condition] = (condition, false);
        return named;
    }
}
