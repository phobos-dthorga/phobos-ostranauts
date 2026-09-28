using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

internal static class FurnaceChecks
{
    internal static void Run(Action<bool, string> check)
    {
        FurnaceCoolingChecks.Run(check);
        FurnaceCoolantChecks.Run(check);
        CoolantChargeChecks.Run(check);
        var receipt = new EnergyReceipt(10, 4); receipt.Gather(10, 7);
        check(receipt.Consume(0) == 7, "Electrical receipt includes partial source and stored consumption");
        bool rejected = false; try { receipt.Consume(0); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "Electrical receipt cannot be replayed");
        var charging = new EnergyReceipt(5, 2); charging.Gather(5, 0);
        check(charging.Consume(7) == 0, "Charging is not consumed heat");
        var gas = new GasParcel(FurnaceRules.GasCv); gas.Add("N2", 3, 300); gas.Add("O2", 1, 300);
        double original = gas.EnergyKJ; var other = gas.Take(1);
        check(Math.Abs(gas.Moles + other.Moles - 4) < 1e-10 && Math.Abs(gas.EnergyKJ + other.EnergyKJ - original) < 1e-10, "Gas transfer preserves species and energy");
        foreach (double f in new[] { 0, .5, 1 })
        {
            double t = FurnaceRules.Temperature(FurnaceRules.Enthalpy(FurnaceRules.MeltK, f), 0, out double liquid);
            check(t == FurnaceRules.MeltK && Math.Abs(liquid - f) < 1e-10, "Latent plateau does not invent sensible heat");
        }
        double cold = RunBatch(1, false, check), partial = RunBatch(.5, false, check);
        check(partial > cold, "Partial electricity takes longer to qualify the batch");
        RunBatch(1, true, check);
        // The steel profile through the same lining, sink, radiator and 250 kW rating: hotter and longer, still completes and cools.
        double steel = RunBatch(1, false, check, FurnaceProfile.Steel);
        check(steel > cold, "A steel charge takes longer to qualify than aluminium through the same hardware");
        RunBatch(1, true, check, FurnaceProfile.Steel);
        foreach (var recipe in FurnaceRecipes.All)
            check(Math.Abs(recipe.Products.Sum(p => p.Count * p.Kg) - FurnaceRules.ChargeUnits * FurnaceRules.FeedUnitKg) < 1e-9, "Every furnace recipe conserves the twenty-kilogram charge: " + recipe.Id);
        check(FurnaceRecipes.All.Select(r => r.Revision).Distinct().Count() == FurnaceRecipes.All.Count && FurnaceRecipes.Housing.Revision == FurnaceRules.RecipeRevision &&
            FurnaceRecipes.ByRevision(FurnaceRecipes.MaxRevision + 1) == null && FurnaceRecipes.ById("bronze") == null, "Recipe revisions are unique; unknown revisions and ids resolve to nothing");
        check(FurnaceRecipes.Housing.Products.Select(p => (p.Id, p.Count, p.Kg)).SequenceEqual(new[] { (FurnaceRules.Blank, 1, FurnaceRules.BlankKg), (FurnaceRules.Remainder, 1, FurnaceRules.RemainderKg) }),
            "The housing recipe is unchanged: one 19 kg blank and one 1 kg remainder");
        check(FurnaceRecipes.AluminiumIngots.Products.Single(p => p.Id == FurnaceRecipes.AluminiumIngot).Count == 4 && FurnaceRecipes.AluminiumIngots.Products.Single(p => p.Id == FurnaceMaterialRules.Aluminium).Count == 3 &&
            FurnaceRecipes.SteelIngots.FeedId == FurnaceRecipes.SteelScrap && FurnaceRecipes.SteelIngots.Products.Single(p => p.Id == FurnaceRecipes.SteelRemainder).Kg == 1 &&
            FurnaceRecipes.SteelIngots.Profile == FurnaceProfile.Steel && FurnaceRecipes.AluminiumIngots.Profile == FurnaceProfile.Aluminium, "Ingot recipes: four 4 kg ingots, three native gates and one remainder, each with its metal's profile");
        check(FurnaceProfile.Steel.MeltK == 1811 && FurnaceProfile.Steel.LatentKJ == 247 && FurnaceProfile.Aluminium.MeltK == FurnaceRules.MeltK, "Profiles carry NIST melting points and the original aluminium numbers");
        check(FurnaceRules.Enthalpy(FurnaceProfile.Steel, FurnaceProfile.Steel.TargetK, 1, 0) > 1.5 * FurnaceRules.Enthalpy(FurnaceProfile.Aluminium, FurnaceProfile.Aluminium.TargetK, 1, 0), "A steel charge needs well over half again the energy of an aluminium charge");
        var hot = new FurnaceBatch { Phase = FurnacePhase.Melt, HotKJ = FurnaceRules.Enthalpy(FurnaceRules.MeltK, .5), Armed = true };
        double before = hot.HotKJ;
        hot.Passive(1, false, false, FurnaceRules.ReferenceK, 0, false);
        check(!hot.Armed && hot.HotKJ == before, "Probe failure and unavailable cooling retain physical heat");
        hot.SinkKJ = FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK);
        check(hot.RequestedKJ(1, true) == 0, "Full sink admits no additional electrical heating");
        var saved = new FurnaceState { ShipId = "ship-A", RoomId = "room-A", LastEpoch = 100 };
        saved.Inputs.AddRange(Enumerable.Range(0, 20).Select(n => "charge-" + n));
        saved.Batch.Phase = FurnacePhase.Hold; saved.Batch.HotKJ = FurnaceRules.Enthalpy(FurnaceRules.TargetK, 1);
        saved.Batch.Armed = true; saved.Batch.Hold = 20; saved.Batch.StepMode = true;
        var fields = saved.Save();
        check(FurnaceState.TryLoad(fields, out var restored) && restored.Batch.HotKJ == saved.Batch.HotKJ &&
            !restored.Batch.Armed && restored.Batch.Hold == 0 && restored.Batch.StepMode && restored.Inputs.SequenceEqual(saved.Inputs), "Hot reload preserves enthalpy, exact input IDs and settings but clears heat permission and continuous hold");
        fields["hot"] = "NaN"; check(!FurnaceState.TryLoad(fields, out _), "Corrupt heat never becomes a cold empty batch");
        fields = saved.Save(); fields["input.1"] = fields["input.0"]; check(!FurnaceState.TryLoad(fields, out _), "Duplicate saved charge identities rejected");
        fields = saved.Save(); fields["revision"] = (FurnaceRecipes.MaxRevision + 1).ToString(); check(!FurnaceState.TryLoad(fields, out _), "Unknown recipe revision retained by native protected-state boundary");
        check(FurnaceState.TryLoad(saved.Save(), out var housing) && housing.Recipe == FurnaceRules.RecipeRevision && housing.Batch.Profile == FurnaceProfile.Aluminium, "A batch saved before recipes loads as the housing recipe with the aluminium profile");
        var steelSaved = new FurnaceState { ShipId = "ship-A", RoomId = "room-A", LastEpoch = 100, Recipe = FurnaceRecipes.SteelIngots.Revision };
        steelSaved.Batch.Profile = FurnaceProfile.Steel; steelSaved.Inputs.AddRange(Enumerable.Range(0, 20).Select(n => "steel-" + n));
        steelSaved.Batch.Phase = FurnacePhase.Hold; steelSaved.Batch.HotKJ = FurnaceRules.Enthalpy(FurnaceProfile.Steel, FurnaceProfile.Steel.TargetK, 1, 0);
        check(FurnaceState.TryLoad(steelSaved.Save(), out var steelLoaded) && steelLoaded.Recipe == 3 && steelLoaded.Batch.Profile == FurnaceProfile.Steel &&
            Math.Abs(steelLoaded.Batch.TemperatureK - FurnaceProfile.Steel.TargetK) < 1e-6, "A steel batch reloads with its own profile and reads its temperature correctly");
        fields = saved.Save(); fields["phase"] = ((int)FurnacePhase.Delivering).ToString(); check(!FurnaceState.TryLoad(fields, out _), "Interrupted native output commit cannot replay on reload");
        fields = saved.Save(); fields["nativeCommit"] = "1"; check(!FurnaceState.TryLoad(fields, out _), "Interrupted native gas transfer is quarantined rather than replayed");
        var warm = new FurnaceBatch { HotKJ = FurnaceRules.LiningCapacity * 20 };
        var warmGas = new GasParcel(FurnaceRules.GasCv); warmGas.Add("StatGasMolN2", 2, 308.15);
        double initialWarm = warm.TotalKJ;
        warm.Seal(warmGas, 308.15);
        check(Math.Abs(warm.TotalKJ - initialWarm - warmGas.EnergyKJ - 20 * FurnaceRules.SolidCp * 10) < 1e-7, "Warm feed, retained lining heat and captured chamber energy all enter the next batch");
        var fullReceiver = new FurnaceBatch(); fullReceiver.Seal(warmGas); fullReceiver.Receiver.Add("StatGasMolN2", 10, 300); fullReceiver.Resume();
        double trapped = fullReceiver.Chamber.Moles + fullReceiver.Receiver.Moles;
        fullReceiver.Receive(2, 1);
        check(!fullReceiver.Armed && Math.Abs(fullReceiver.Chamber.Moles + fullReceiver.Receiver.Moles - trapped) < 1e-10, "Full gas receiver pauses evacuation without deleting gas");
    }
    private static double RunBatch(double supply, bool interrupted, Action<bool,string> check, FurnaceProfile? profile = null)
    {
        var batch = new FurnaceBatch { Profile = profile ?? FurnaceProfile.Aluminium };
        var gas = new GasParcel(FurnaceRules.GasCv);
        gas.Add("N2", 100 * FurnaceRules.ChamberM3 / (FurnaceRules.GasR * FurnaceRules.ReferenceK), FurnaceRules.ReferenceK);
        batch.Seal(gas); batch.Resume();
        double initial = batch.TotalKJ, energy = 0, rejected = 0, elapsed = 0, qualifiedAt = 0;
        const double dt = .25;
        while (elapsed < 7200 && batch.Phase != FurnacePhase.Equalize)
        {
            if (interrupted && elapsed == 200) batch.Armed = false;
            if (interrupted && elapsed == 350) batch.Resume();
            double input = batch.RequestedKJ(dt, true) * supply; batch.Receive(input, dt); energy += input;
            var loss = batch.Passive(dt, true, true, FurnaceRules.ReferenceK, 100, true); rejected += loss.Radiated + loss.Room;
            elapsed += dt;
            if (batch.Qualified && qualifiedAt == 0) qualifiedAt = elapsed;
            if (Math.Abs(initial + energy - rejected - batch.TotalKJ) > 1e-6) throw new Exception("Furnace energy conservation failed");
            if (batch.SinkK > FurnaceRules.SinkMaxK + 1e-6) throw new Exception("Furnace sink exceeded operating capacity");
        }
        check(batch.Phase == FurnacePhase.Equalize && batch.Qualified && batch.SafeOpen, $"A supplied cycle melts, holds, solidifies and cools before equalization ({batch.Profile.Id}: phase {batch.Phase}, {batch.TemperatureK:F0} K, sink {batch.SinkK:F0} K, armed {batch.Armed}, {elapsed:F0} s)");
        check(Math.Abs(batch.Chamber.Moles + batch.Receiver.Moles - gas.Moles) < 1e-9, "Evacuation retains every mole");
        return qualifiedAt;
    }
}
