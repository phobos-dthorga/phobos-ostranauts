using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The AX-2 ammonia cracker's chemistry, heat, saved record and ports, on numbers alone.</summary>
internal static class CrackerChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        // 2 NH3 -> N2 + 3 H2 with the game's molar masses; nitrogen is the balancing remainder.
        check(CrackerRules.Balanced(), "Every cycle conserves mass, makes the stoichiometric nitrogen within 0.01% and absorbs less energy than it draws");
        check(Math.Abs(CrackerRules.AmmoniaMolesPerCycle - 58.716) < 0.01, "1 kg of ammonia is 58.7 mol");
        check(Math.Abs(CrackerRules.NitrogenKgPerCycle - 0.8225) < 0.0005 && Math.Abs(CrackerRules.HydrogenKgPerCycle - 0.1775) < 0.0005 &&
              Math.Abs(CrackerRules.NitrogenKgPerCycle + CrackerRules.HydrogenKgPerCycle - CrackerRules.AmmoniaKgPerCycle) < 1e-12,
            "Each cycle turns 1 kg of ammonia into 0.822 kg of nitrogen and 0.178 kg of hydrogen");
        // Heat: the reverse of ammonia's NIST formation enthalpy, taken out of the electricity's room heat.
        check(Math.Abs(CrackerRules.ReactionKJPerMol - 45.94) < 1e-9 && Math.Abs(CrackerRules.ReactionKWhPerCycle - 0.7493) < 0.001,
            "Cracking absorbs 45.94 kJ per mole of ammonia (NIST), 0.75 kWh per cycle");
        check(Math.Abs(CrackerRules.RoomHeatKW(true) - (CrackerRules.WorkingKW - CrackerRules.ReactionKW)) < 1e-12 && Math.Abs(CrackerRules.RoomHeatKW(true) - 1.2507) < 0.001 &&
              CrackerRules.RoomHeatKW(false) == CrackerRules.IdleKW, "The working cracker puts its electricity less the reaction's share, about 1.25 kW, into the room");
        check(Math.Abs(CrackerRules.AbsorbedKWh(CrackerRules.CycleKWh) - CrackerRules.ReactionKWhPerCycle) < 1e-12 && CrackerRules.AbsorbedKWh(-1) == 0 &&
              CrackerRules.AbsorbedKWh(0.5) < 0.5, "The reaction takes its share of credited progress, never more than the electricity that paid for it");
        var (cycle, complete) = CrackerRules.Advance(0, 10);
        check(cycle == CrackerRules.CycleKWh && complete, "Surplus energy completes one cycle and is not banked");
        (cycle, complete) = CrackerRules.Advance(0.5, 0.2);
        check(Math.Abs(cycle - 0.7) < 1e-12 && !complete, "Partial supply advances the cycle");
        throws(() => CrackerRules.Advance(CrackerRules.CycleKWh, 0.1), "A completed cycle cannot be advanced");
        // A salt crust charge (0.955 kg of ammonia) is about one cycle.
        check(Math.Abs(RefineryRecipes.CrustAmmoniaKg - CrackerRules.AmmoniaKgPerCycle) < 0.05, "One salt crust charge feeds about one cracker cycle");

        // The saved record: charge, convert, deliver.
        var s = new CrackerState();
        check(!s.Charged && !s.HoldsProducts && s.HeldKg == 0, "A new cracker holds nothing");
        throws(() => s.Convert(), "An uncharged cracker cannot convert");
        s.AmmoniaKg = CrackerRules.AmmoniaKgPerCycle; s.CycleKWh = 0.4;
        double before = s.HeldKg;
        s.Convert();
        check(Math.Abs(s.HeldKg - before) < 1e-12 && s.HoldsProducts && s.AmmoniaKg == 0 && s.CycleKWh == 0 && s.Cycles == 1 &&
              Math.Abs(s.ConsumedAmmoniaKg - CrackerRules.AmmoniaKgPerCycle) < 1e-12, "Conversion turns ammonia into products mass for mass and closes the cycle");
        throws(() => s.Convert(), "Products must leave before the next conversion");
        var round = CrackerState.Read(s.Save());
        check(round.NitrogenKg == s.NitrogenKg && round.HydrogenKg == s.HydrogenKg && round.Cycles == 1 && round.ConsumedAmmoniaKg == s.ConsumedAmmoniaKg, "The record round-trips");
        foreach (var bad in new[] {
            new Dictionary<string, string> { ["nh3"] = "1.5" }, new Dictionary<string, string> { ["n2"] = "-1" }, new Dictionary<string, string> { ["h2"] = "5" },
            new Dictionary<string, string> { ["cycle"] = "2.5" }, new Dictionary<string, string> { ["cycle"] = "0.1", ["n2"] = "0.1" },
            new Dictionary<string, string> { ["canister"] = "x" }, new Dictionary<string, string> { ["h2"] = "NaN" } })
            throws(() => CrackerState.Read(bad), "A corrupt record is refused: " + string.Join(",", bad.Select(p => p.Key + "=" + p.Value)));
        check(CrackerRules.IsFamily("PhobosAmmoniaCrackerInstalledDmg") && !CrackerRules.IsFamily("PhobosAmmoniaStoreInstalled") && !SabatierRules.IsFamily("PhobosAmmoniaCrackerInstalled"),
            "Family identity, distinct from the ammonia store");

        // Ports: the cracker's own are distinct; it draws from a store's ordinary outlet and delivers into a separate
        // inlet, so one hydrogen store can take from an X2 and a cracker, and one nitrogen store feed an A2.
        var ours = new[] { CrackerRules.AmmoniaInPort, CrackerRules.NitrogenOutPort, CrackerRules.HydrogenOutPort, CrackerRules.StoreInPort };
        var others = new[] { SabatierRules.WaterOutPort, SabatierRules.VesselInPort, SabatierRules.HydrogenInPort, SabatierRules.StoreOutPort, SabatierRules.MethaneOutPort,
            ProcessorRules.WaterInPort, ProcessorRules.VesselOutPort, ProcessorRules.HydrogenOutPort, ProcessorRules.StoreInPort, RefineryRules.VesselPort, RefineryRules.GasInPort };
        check(ours.Distinct().Count() == ours.Length && !ours.Intersect(others).Any() && ours.All(p => p.StartsWith("PhobosManufacturing.", StringComparison.Ordinal)),
            "The cracker's ports are its own; no store inlet is shared with the X2, K2 or V4");
        check(CrackerRules.StoreOutPort == SabatierRules.StoreOutPort, "It draws ammonia from a store's ordinary outlet, as the K2 draws hydrogen");
        check(NativeGasCanister.IsRoomSpecies(CrackerRules.AmmoniaSpecies) && NativeGasCanister.IsRoomSpecies(CrackerRules.NitrogenSpecies),
            "Ammonia and nitrogen are the game's own gases, so a damaged cracker can release them into the room");
        check(GasStores.FamilyOf(ManufacturingRules.Ammonia) == GasStores.AmmoniaFamily && GasStores.FamilyOf(ManufacturingRules.Nitrogen) == GasStores.NitrogenFamily &&
              GasStores.FamilyOf(ManufacturingRules.Hydrogen) == GasStores.HydrogenFamily, "Each link resolves to its own store family");
    }
}
