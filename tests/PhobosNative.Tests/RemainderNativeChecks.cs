using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

/// <summary>Owner rule, 4 October 2026: no trash-like object is left to pile up without a consumer. Every Phobos item the
/// game would file as trash is a declared remainder, and the reaction mass feeder takes exactly the declared ones.</summary>
internal static class RemainderNativeChecks
{
    /// <summary>Trash-category items a recipe takes, so they do not pile up: listed here with their consumer.</summary>
    private static readonly HashSet<string> Consumed = new(StringComparer.Ordinal)
    {
        "PhobosLeachedResidue",             // the V4 calcines it
        "PhobosVerdemorrowCropResidue",     // the B2 works it up, scutches or presses it
        "PhobosVerdemorrowSpentBiomass",    // the B2 straw press takes it
        "PhobosVerdemorrowProcessSolution"  // the W2 drainage treatment takes it
    };
    internal static void Run(IEnumerable<NativeDefinitions> all, NativeDefinitions manufacturing, Action<bool, string> check)
    {
        static bool Trash(JsonCondOwner co) => (co.aStartingConds ?? Array.Empty<string>()).Any(s => s.StartsWith("IsCategoryTrash=", StringComparison.Ordinal));
        var trash = all.SelectMany(d => d.Objects.Values).Where(Trash).Select(co => co.strName).Distinct(StringComparer.Ordinal).ToArray();
        var loose = trash.Where(id => !Remainders.IsDeclared(id) && !Consumed.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        check(loose.Length == 0, "Every Phobos trash item is a declared remainder (the feeder consumes it) or has a recipe that takes it: " + string.Join(", ", loose));
        check(trash.Length >= 20, "The remainder check saw the mods' trash items: " + trash.Length);
        foreach (string id in Consumed) check(!Remainders.IsDeclared(id), "A trash-category item with its own consumer is not also a remainder: " + id);
        // Nothing useful is a remainder: the game's own scrap, ore, gangue and our stock stay out of the feeder.
        foreach (string useful in new[] { "ItmScrapSteel", "ItmScrapAluminum", "ItmScrapTrash", "ItmMiningTrash", "ItmIceTrash01", "ItmIce01", RefineryRules.Hydrates,
            Materials.NickelIronIngot, Materials.CarbonStock, Materials.Spirit })
            check(!Remainders.IsDeclared(useful), "Not a remainder, so the feeder refuses it: " + useful);
        foreach (var m in Materials.All)
            check(Remainders.IsDeclared(m.Id) == m.Terminal, "A Manufacturing material is a remainder exactly when it is terminal: " + m.Id);

        // The Slingwright RM-1 itself.
        foreach (string state in PhobosManufacturing.Definitions.Forms)
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            var feeder = manufacturing.Objects[FeederRules.Prefix + state];
            double mass = double.Parse(feeder.aStartingConds.Single(s => s.StartsWith("StatMass=", StringComparison.Ordinal)).Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture);
            check(manufacturing.Items[feeder.strItemDef].nCols == 1 && mass == FeederRules.MachineKg && feeder.strNameFriendly.StartsWith("Phobos' Slingwright RM-1 Reaction Mass Feeder", StringComparison.Ordinal),
                "The feeder is a 40 kg one-tile Slingwright machine: " + state);
            check((feeder.jsonPI == FeederRules.Prefix + "Power") == (installed && !damaged) && !feeder.aStartingConds.Any(s => s.StartsWith("IsAirtight=", StringComparison.Ordinal)),
                "The feeder draws power only when installed and intact, and is never a gas canister: " + state);
        }
        var power = manufacturing.Power[FeederRules.Prefix + "Power"];
        check(Math.Abs(power.fOverrideAmount - FeederRules.WorkingKW / 3600) < 1e-12 && power.strOverrideCond == ManufacturingRules.Grinding &&
              PhobosManufacturing.Core.Economy.Pack.factionKiosks?.tiers[FeederRules.Prefix] == "Friendly", "The feeder draws 3 kW grinding and sits at the Friendly kiosk tier");
        check(manufacturing.Installables[FeederRules.Prefix + "LooseInstall"].strBuildType == InstallMenu.Hvac, "The feeder installs from INSTALL, HVAC, beside the P1");
        var spec = Phobos.Ostranauts.Framework.Liquids.BulkVessels.SpecFor(FeederRules.Installed);
        check(spec != null && spec.Commodity == FeederRules.Commodity && spec.CapacityKg == FeederRules.CapacityKg && spec.DryKg == FeederRules.MachineKg &&
              Phobos.Ostranauts.Framework.Liquids.VesselContentsDisplay.IsDeclared(FeederRules.Commodity), "The feeder's reaction mass is a 60 kg record shown on its right-click card");
    }
}
