"""Tests for the offline design calculator, not the proposed agriculture mod."""
import importlib.util
from pathlib import Path
import sys
import unittest
from dataclasses import replace

path = Path(__file__).resolve().parents[1] / "scripts/calculate-agriculture-budget.py"
spec = importlib.util.spec_from_file_location("agriculture_budget", path)
model = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = model
spec.loader.exec_module(model)


class AgricultureBudgetTests(unittest.TestCase):
    def test_every_crop_balances_at_all_recovery_endpoints(self):
        for crop in model.CROPS.values():
            for recovery in (0, .5, .95, 1):
                with self.subTest(crop=crop.name, recovery=recovery):
                    result = model.budget(crop, recovery)
                    self.assertLess(abs(result["mass_residual_kg"]), model.MASS_TOLERANCE_KG)
                    self.assertAlmostEqual(result["energy_residual_kwh"], 0)
                    self.assertGreater(result["net_makeup_water_kg"], 0)

    def test_recovery_relocates_water_without_creating_biomass_or_energy(self):
        crop = model.CROPS["potato"]
        low, high = model.budget(crop, 0), model.budget(crop, 1)
        self.assertEqual(low["inputs_kg"], high["inputs_kg"])
        self.assertEqual(low["food_units"], high["food_units"])
        self.assertAlmostEqual(low["net_makeup_water_kg"]-high["net_makeup_water_kg"],
                               crop.transpired_water_kg)
        self.assertAlmostEqual(high["direct_room_heat_kwh"]-low["direct_room_heat_kwh"],
                               low["exported_vapour_latent_kwh"])

    def test_partial_power_changes_ideal_duration_not_batch_resources(self):
        crop = model.CROPS["potato"]
        full, half = model.budget(crop), model.budget(crop, supply=.5)
        self.assertEqual(full["inputs_kg"], half["inputs_kg"])
        self.assertEqual(full["electric_kwh"], half["electric_kwh"])
        self.assertEqual(half["ideal_growth_hours"], full["ideal_growth_hours"]*2)
        stopped = model.budget(crop, supply=0)
        self.assertIsNone(stopped["ideal_growth_hours"])
        self.assertEqual(stopped["food_units_per_day"], 0)

    def test_pace_and_hardware_cap_cannot_discount_batch_costs(self):
        crop = model.CROPS["potato"]
        base = model.budget(crop)
        for pace in (.5, 2):
            changed = model.budget(crop, duration_multiplier=pace)
            self.assertEqual(base["inputs_kg"], changed["inputs_kg"])
            self.assertEqual(base["electric_kwh"], changed["electric_kwh"])
            self.assertLessEqual(changed["requested_kw"], model.RACK_MAX_KW)
        oversized = model.budget(replace(crop, average_kw=2), duration_multiplier=.5)
        self.assertEqual(oversized["requested_kw"], model.RACK_MAX_KW)
        self.assertGreater(oversized["ideal_growth_hours"], crop.growth_hours*.5)

    def test_propagation_cost_and_crew_scaling_remain_visible(self):
        potato = model.CROPS["potato"]
        self.assertAlmostEqual(potato.propagation_reserve_kg, potato.seed_kg)
        seed = model.CROPS["lettuce_seed"]
        self.assertAlmostEqual(seed.product_kg-seed.propagation_reserve_kg,
                               3*model.CROPS["lettuce"].seed_kg)
        self.assertEqual(model.budget(seed)["food_units"], 0)
        single, three = model.farm(1, 1, 1), model.farm(1, 1, 3)
        self.assertAlmostEqual(single["nominal_food_coverage_fraction"],
                               3*three["nominal_food_coverage_fraction"])
        self.assertLess(model.farm(1, 1, 1, True)["food_units_per_day"],
                        single["food_units_per_day"])

    def test_invalid_or_impossible_inputs_are_rejected(self):
        for options in ({"recovery": 1.1}, {"supply": -.1}, {"supply": float("nan")},
                        {"duration_multiplier": 0}, {"recovery": float("inf")}):
            with self.subTest(options=options), self.assertRaises(ValueError):
                model.budget(model.CROPS["potato"], **options)
        for crop in (replace(model.CROPS["potato"], retained_nutrient_kg=10),
                     replace(model.CROPS["potato"], average_kw=.0001),
                     replace(model.CROPS["potato"], propagation_reserve_kg=100)):
            with self.assertRaises(ValueError):
                model.budget(crop)


if __name__ == "__main__":
    unittest.main()
