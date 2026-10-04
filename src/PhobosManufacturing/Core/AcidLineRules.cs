using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>One liquid line family (Manufacturing 0.38.0 made the acid line's rules a value, so the ethanol line is a
/// second row, not a copy): ordinary 1 x 1 segments on Framework's shared pattern in their own lane, joining equipment at
/// their own port, each segment holding its liquid through Framework's <see cref="LineContents"/>.</summary>
public sealed class LiquidLineRules
{
    public string FamilyId { get; }
    public string Prefix { get; }
    public string Art { get; }
    public string TextPrefix { get; }
    public LiquidFamily Liquid { get; }
    public float Layer { get; }
    public string PortPoint { get; }
    public Func<int, (int X, int Y, int Socket)> Port { get; }
    public string Present => Prefix + "Present";
    public string Intact => Prefix + "Intact";
    public string Installed => Prefix + "Installed";
    public const double SegmentKg = 1;
    public LiquidLineRules(string familyId, string prefix, string art, string textPrefix, LiquidFamily liquid, float layer, string portPoint, Func<int, (int, int, int)> port)
    { FamilyId = familyId; Prefix = prefix; Art = art; TextPrefix = textPrefix; Liquid = liquid; Layer = layer; PortPoint = portPoint; Port = port; }
    /// <summary>What one segment holds: Framework's shared 25 mm bore, one metre a tile (0.49 L), of the liquid at its
    /// density; a liquid that mists releases its tanks' mist fraction when a segment is damaged.</summary>
    public LineCommodity Held() => Liquid.MistSpecies == null
        ? LineCommodity.Liquid(Liquid.Commodity, Liquid.DensityKgPerM3)
        : LineCommodity.Liquid(Liquid.Commodity, Liquid.DensityKgPerM3, mistSpecies: Liquid.MistSpecies, mistFraction: Liquid.MistFraction);
    public bool IsFamily(string? definition) => definition != null && definition.StartsWith(Prefix, StringComparison.Ordinal);
}

/// <summary>The Lixivar acid line (Manufacturing 0.24.0; owner decision, 30 September 2026: an acid line, with leak and
/// mist on damage). Ordinary 1 x 1 segments on Framework's shared pattern, in the acid lane, joining the LC-3, the SA-3
/// and the AT acid tanks at their acid ports (the +X side, one row below the gas port). Since Manufacturing 0.25.0
/// (owner decision, 1 October 2026: lines hold their contents until drained) each segment holds its own acid through
/// Framework's <see cref="LineContents"/>: filled from the tanks on its network, drained into a drain canister, and a
/// damaged or destroyed segment releases the tank-damage mist fraction of what it holds into the room.</summary>
public static class AcidLineRules
{
    public const string FamilyId = "PhobosManufacturing.AcidLine", Prefix = "PhobosAcidLine";
    public const string Present = Prefix + "Present", Intact = Prefix + "Intact", Installed = Prefix + "Installed";
    public const string Art = "AcidPipe";
    public const double SegmentKg = LiquidLineRules.SegmentKg;
    public static readonly LiquidLineRules Rules = new(FamilyId, Prefix, Art, "AcidLine", LiquidStores.AcidFamily, LineLayers.Acid, LinePorts.AcidPoint, LinePorts.Acid);
    /// <summary>What one segment holds: 98% sulfuric acid at <see cref="LiquidStores.AcidDensityKgPerM3"/>, about 0.90 kg; the
    /// mist on damage is the tanks' own <see cref="LiquidStores.MistFraction"/> as the game's H2SO4. A drain canister holds
    /// 20 L, about 36.7 kg.</summary>
    public static LineCommodity HeldAcid() => Rules.Held();
    public static bool IsFamily(string? definition) => Rules.IsFamily(definition);
}

/// <summary>The Alembrine ethanol line (Manufacturing 0.38.0; owner decision, 4 October 2026: ethanol has its own line,
/// holding its contents like the acid line). Its port is on the -X side one row below the water port. A segment holds
/// about 0.39 kg of ethanol; ethanol has no game species, so a damaged segment keeps it, and an authored share burns when
/// the room has oxygen and an ignition source. Its art shares the coolant conduit's lane (the tile has room for five).</summary>
public static class EthanolLineRules
{
    public const string FamilyId = "PhobosManufacturing.EthanolLine", Prefix = "PhobosEthanolLine";
    public const string Art = "EthanolPipe";
    public static readonly LiquidLineRules Rules = new(FamilyId, Prefix, Art, "EthanolLine", LiquidStores.EthanolFamily, LineLayers.Ethanol, LinePorts.EthanolPoint, LinePorts.Ethanol);
}

public static class LiquidLines
{
    public static readonly IReadOnlyList<LiquidLineRules> All = new[] { AcidLineRules.Rules, EthanolLineRules.Rules };
    public static LiquidLineRules? ForDefinition(string? definition) { foreach (var line in All) if (line.IsFamily(definition)) return line; return null; }
    public static LiquidLineRules? ForLiquid(LiquidFamily liquid) { foreach (var line in All) if (line.Liquid == liquid) return line; return null; }
}
