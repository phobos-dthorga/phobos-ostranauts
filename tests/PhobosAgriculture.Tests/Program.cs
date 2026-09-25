using System;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;

int checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 1e-7, message + $": {actual} vs {expected}");
CropState New(Crop c) { var s = new CropState { Water = 20, Nutrients = .5 }; s.Plant(c, 1); return s; }
foreach (var crop in new[] { Crop.Potato, Crop.Lettuce })
{
    var s = New(crop); double energy = 0, oxygen = 0;
    for (int hour = 0; hour < crop.Hours; hour++)
    {
        double mass = s.ContentsMass, carbon = s.Carbon;
        var x = s.Step(1, crop.KW, 10, 10, true); energy += crop.KW; oxygen += x.OxygenKg;
        Near(s.ContentsMass - mass + x.CO2Kg + x.OxygenKg + x.VapourKg, 0, "Step conserves all material");
        Near(x.RoomHeatKWh + (s.Carbon - carbon) * CropState.HeatPerCarbonKWh + x.VapourKg * 2.45 / 3.6, crop.KW, "Step conserves energy");
    }
    Check(s.Ready, "Crop reaches harvest"); Near(s.Biomass, crop.Final, "Nominal biomass"); Near(oxygen, crop.Carbon * 32 / 30, "Oxygen follows net growth");
    var harvest = s.Harvest(); Check(harvest.Portions == (crop == Crop.Potato ? 10 : 4), "Complete crop provides useful food");
    Near(harvest.SeedKg, crop == Crop.Potato ? .2 : 0, "Potato seed reserved; lettuce does not invent seed");
    Near(harvest.SeedKg + harvest.Portions * harvest.PortionKg + harvest.ResidueKg, s.Biomass, "Harvest conserves all biomass");
    var cleared = s.Harvest(true); Check(cleared.SeedKg == 0 && cleared.Portions == 0, "Clearing cannot grant food/seed"); Near(cleared.ResidueKg, s.Biomass, "Clearing retains all tissue");
    var stressed = s.Copy(); stressed.Health = .5; var damagedHarvest = stressed.Harvest(); Check(damagedHarvest.Portions < harvest.Portions, "Damage reduces food");
    Near(damagedHarvest.SeedKg + damagedHarvest.Portions * damagedHarvest.PortionKg + damagedHarvest.ResidueKg, s.Biomass, "Damage retains nonedible matter");
    var afterHarvest = s.Copy(); afterHarvest.ClearCrop(); bool duplicated = false; try { afterHarvest.Harvest(); } catch { duplicated = true; } Check(duplicated, "Cleared cohort cannot be harvested twice");
    Near(20 - s.Water, crop.Water, "Finite water spent"); Near(.5 - s.Nutrients, crop.Nutrient, "Finite nutrients spent");
    var loaded = CropState.Read(s.Save()); Near(loaded.Biomass, s.Biomass, "Reload keeps biomass"); Check(!loaded.Running && !loaded.Receiving, "Reload cannot restore permissions");
    loaded.Pace = 2; Near(s.Pace, 1, "Saved cohort captured independently");
}
var powered = New(Crop.Potato); powered.Step(1, .375, 10, 10, true); Near(powered.Progress, .5 / 96, "Half power bounds growth");
var dry = New(Crop.Potato); dry.Water = 0; var heat = dry.Step(1, .75, 10, 10, true); Near(dry.Progress, 0, "No irrigation means no growth"); Check(heat.RoomHeatKWh >= .75, "Spent lamps plus respiration remain heat");
var dark = New(Crop.Potato); dark.Running = false; double before = dark.ContentsMass;
var exchange = dark.Step(1, 0, 10, 10, true); Check(exchange.OxygenKg < 0 && exchange.CO2Kg > 0, "Dark respiration has opposite gas direction"); Near(dark.ContentsMass - before + exchange.CO2Kg + exchange.OxygenKg + exchange.VapourKg, 0, "Respiration conserves mass");
var coarse = New(Crop.Potato); var fine = New(Crop.Potato); coarse.Running = fine.Running = false;
for (int n = 0; n < 10; n++) coarse.Step(1, 0, 10, 10, true);
for (int n = 0; n < 100; n++) fine.Step(.1, 0, 10, 10, true);
Near(coarse.Carbon, fine.Carbon, "Fast-forward respiration invariant"); Near(coarse.Health, fine.Health, "Stress grace invariant");
var savedFields = fine.Save(); savedFields["health"] = "NaN"; bool rejected = false; try { CropState.Read(savedFields); } catch { rejected = true; } Check(rejected, "Malformed state rejected");
var savedCooker = new CropState { CookerInput = "exact-portion", CookerProgress = .025, Running = true };
var loadedCooker = CropState.Read(savedCooker.Save()); Check(loadedCooker.CookerInput == "exact-portion" && loadedCooker.CookerProgress == .025 && !loadedCooker.Running, "Cooking reload retains exact input and partial energy, requiring Resume");
var carbonLimited = New(Crop.Potato); var carbonExchange = carbonLimited.Step(1, .75, 0, 10, true); Near(carbonLimited.Progress, 0, "Missing atmospheric carbon prevents food production"); Check(carbonExchange.OxygenKg <= 0, "No photosynthetic oxygen from a timer");
var source = new Reservoir("source", 12, 20); var dest = new Reservoir("dest", 0, 20);
var receipt = FiniteLiquidTransfer.Commit(source, dest, 5, 10); Near(receipt.ReceivedKg, 2, "Crew reserve wins"); Near(source.QuantityKg + dest.QuantityKg, 12, "Water conserved");
dest.Ship = "neighbour"; rejected = false; try { FiniteLiquidTransfer.Commit(source, dest, 1, 0); } catch { rejected = true; } Check(rejected, "Docked neighbour rejected");
dest.Ship = "ship"; dest.Fail = true; double total = source.QuantityKg + dest.QuantityKg;
try { FiniteLiquidTransfer.Commit(source, dest, 1, 0); } catch { }
Near(source.QuantityKg + dest.QuantityKg, total, "Failed destination returns unreceived debit");
dest.Fail = false; dest.Kg = 20; receipt = FiniteLiquidTransfer.Commit(source, dest, 1, 0); Near(receipt.DebitedKg, 0, "Full storage consumes nothing");
Console.WriteLine($"Agriculture: {checks} checks passed (offline; not gameplay validation).");

sealed class Reservoir : ILiquidReservoir
{
    public string Identity { get; }
    public string Ship = "ship"; public string ShipId => Ship; public string Commodity => "water";
    public double Kg; public double QuantityKg => Kg; public double CapacityKg { get; }
    public bool Fail;
    public Reservoir(string id, double kg, double max) { Identity = id; Kg = kg; CapacityKg = max; }
    public void SetQuantity(double kg) { if (Fail) throw new Exception("Unavailable"); Kg = kg; }
}
