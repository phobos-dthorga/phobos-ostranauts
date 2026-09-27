"""Independent arithmetic checks for research estimates, not Unity tests."""
import importlib.util
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("storage",ROOT/"scripts/calculate-agriculture-storage.py")
storage=importlib.util.module_from_spec(spec);spec.loader.exec_module(storage)


class StorageResearchTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls): cls.inputs=storage.source_inputs()

    def test_potato_thirty_days_eight_racks(self):
        s=storage.scenario(self.inputs,"potato",8,30)
        self.assertAlmostEqual(s["water_kg"],277.44)
        self.assertAlmostEqual(s["nutrient_kg"],2.4)
        self.assertEqual((s["water_loads"],s["nutrient_loads"],s["large_nutrient_loads"]),(56,60,5))
        self.assertAlmostEqual(s["manual_minutes"]["30"],116*40/60)

    def test_pace_and_fleet_scale_but_no_free_recovery(self):
        one=storage.scenario(self.inputs,"lettuce",1,7)
        eight=storage.scenario(self.inputs,"lettuce",8,7,.5)
        self.assertAlmostEqual(eight["water_kg"],one["water_kg"]*16)
        self.assertAlmostEqual(eight["crop_electric_kwh"],one["crop_electric_kwh"]*16)
        self.assertAlmostEqual(one["nutrient_kg"],.0175)

    def test_more_capacity_does_not_reduce_matter(self):
        s=storage.scenario(self.inputs,"potato",8,30)
        self.assertEqual(s["r3_water_reloads"],0)
        self.assertEqual(s["baseline_water_reloads"],21)
        self.assertAlmostEqual(s["with_r3_trip_mass_kg"]-s["all_trip_supplies_and_equipment_kg"],25)
        self.assertLess(s["central_nutrient_days"],s["r3_water_days"])

    def test_completed_residue_mass_includes_external_makeup(self):
        r=storage.recovery_case(self.inputs,"potato")
        self.assertAlmostEqual(r["concentrate_kg"],.00384)
        self.assertAlmostEqual(r["mixture_kg"],.00768)
        self.assertAlmostEqual(r["mass_residual_kg"],0)
        self.assertAlmostEqual(r["electric_kwh"],.017)
        self.assertEqual(r["extra_water_kg"],0)

    def test_treatment_spends_only_measured_medium(self):
        t=storage.treatment_case(self.inputs)
        self.assertAlmostEqual(t["spent_medium_kg"],.0392)
        self.assertAlmostEqual(t["recovered_water_kg"],17.55)
        self.assertAlmostEqual(t["reject_kg"],2.0092)
        self.assertAlmostEqual(t["mass_residual_kg"],0)
        self.assertAlmostEqual(t["full_power_minutes"],23.52)

    def test_invalid_and_whole_packet_boundaries(self):
        self.assertEqual(storage.count_loads(.12,.04),3)
        self.assertEqual(storage.count_loads(0,5),0)
        for value in (float("nan"),float("inf"),-1):
            with self.assertRaises(ValueError):storage.count_loads(value,5)
        with self.assertRaises(ValueError):storage.scenario(self.inputs,"potato",9,30)
        for water,nutrient in ((0,0),(-1,1),(26,0),(float("nan"),0)):
            with self.assertRaises(ValueError):storage.treatment_case(self.inputs,water,nutrient)

    def test_coverage_and_saved_report(self):
        report=storage.report()
        self.assertEqual(len(report["scenarios"]),54)
        self.assertEqual(storage.REPORT.read_text(encoding="utf-8"),storage.markdown(report))


if __name__=="__main__":unittest.main()
