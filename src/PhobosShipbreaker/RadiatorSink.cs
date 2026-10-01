using System;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The F6's cooling assemblies (the F6-R radiator and the F6-P underside port) as a heat sink for other
/// Shipbreaker equipment (Shipbreaker 0.61.0; first user the ML-2 mining laser). Nothing about the assembly changes:
/// it keeps its saved store, its own radiation every quarter second whether paired or not, its 250 C limit and its
/// single cooling port, so it serves one furnace or one other machine, never both. A user pays heat in here only
/// after checking the room left; the assembly sheds it exactly as it sheds the furnace's.</summary>
internal static partial class FurnaceService
{
    /// <summary>The assembly's one cooling port, the same the furnace pairs with.</summary>
    internal static MaterialPort SinkPort(CondOwner assembly) => Port(assembly);
    internal static bool IsSink(CondOwner? co) => co != null && FurnaceRules.Cooling(co.strCODef);
    /// <summary>Why the assembly cannot take heat now, or null: it must be installed and intact where it can shed
    /// heat (the radiator outside against its hull walls, the port on sealed floor) with a readable saved store.</summary>
    internal static string? SinkProblem(CondOwner? assembly)
    {
        if (assembly == null || assembly.bDestroyed || !IsSink(assembly)) return Text.Get("Laser.radiator_missing");
        if (!Mounted(assembly)) return Text.Get("Laser.radiator_missing");
        if (assembly.HasCond("IsDamaged")) return Text.Get("Laser.radiator_damaged");
        if (!CoolingMounted(assembly)) return Text.Get(FurnaceRules.Underside(assembly.strCODef) ? "Furnace.port_support" : "Furnace.radiator_mount_fault");
        return Get(assembly).Protected ? Text.Get("Furnace.protected") : null;
    }
    internal static double SinkKelvin(CondOwner assembly) => FurnaceRules.ReferenceK + Get(assembly).SinkKJ / FurnaceRules.SinkCapacity;
    /// <summary>Heat the assembly can still take before its limit, after radiating up to now.</summary>
    internal static double SinkHeadroomKJ(CondOwner assembly)
    {
        var s = Get(assembly);
        AdvanceCooling(s);
        return s.Protected ? 0 : Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK) - s.SinkKJ);
    }
    /// <summary>Pays heat into the assembly's store. The caller has checked the room left for this step.</summary>
    internal static void SinkDeposit(CondOwner assembly, double kJ)
    {
        if (double.IsNaN(kJ) || double.IsInfinity(kJ) || kJ < 0) throw new ArgumentOutOfRangeException(nameof(kJ));
        var s = Get(assembly);
        if (s.Protected) throw new InvalidOperationException(Text.Get("Furnace.protected"));
        s.SinkKJ += kJ;
        Save(s);
    }
    /// <summary>Cool enough to change what it is paired with: the furnace's own rule, 50 C or below.</summary>
    internal static bool SinkCool(CondOwner assembly) => !Get(assembly).Protected && SinkKelvin(assembly) <= FurnaceRules.ReleaseK;
}
