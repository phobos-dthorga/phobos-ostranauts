"""Offline AGRICULTURE DESIGN budgets; no game, saves or provider APIs are used.

See docs/agriculture-first-slice.md. Every crop parameter is an authored candidate.
CH2O-equivalent dry growth plus retained nutrient solids is a bookkeeping surrogate,
not a crop assay, kinetic model, complete nutrient chemistry or human diet model.
Schedules assume ideal cultivation; partial supply does not simulate stress.
"""
import argparse
from dataclasses import dataclass
import json
import math


HOURS_PER_DAY = 24
MJ_PER_KWH = 3.6
CARBOHYDRATE_MJ_PER_KG = 17.0  # Authored effective energy store, not measured crop data.
VAPORIZATION_MJ_PER_KG = 2.45  # Rounded reference-temperature bookkeeping assumption.
RACK_MAX_KW = 1.5
TURNAROUND_HOURS = 1.0
DEFAULT_RECOVERY = 0.95
BASELINE_FOOD_UNITS_PER_HOUR = 1.0  # Native Food ticker, before character modifiers.
MASS_TOLERANCE_KG = 1e-9


@dataclass(frozen=True)
class Crop:
    name: str
    growth_hours: float
    average_kw: float
    seed_kg: float
    seed_dry_fraction: float
    product_kg: float
    product_dry_fraction: float
    residue_kg: float
    residue_dry_fraction: float
    retained_nutrient_kg: float
    transpired_water_kg: float
    propagation_reserve_kg: float
    portion_kg: float
    food_units_per_portion: float


CROPS = {
    "potato": Crop("potato", 96, .75, .2, .2, 4.2, .2, .8, .1, .04, 4, .2, .4, 5),
    "lettuce": Crop("lettuce", 48, .4, .005, .9, 1, .05, .2, .1, .005, 2, 0, .25, 1),
    # One seed crop: four 5-g packets, one retained to repeat seed production.
    "lettuce_seed": Crop("lettuce_seed", 96, .4, .005, .9, .02, .9, 1.18, .05,
                         .01, 4, .005, .005, 0),
}


def finite_range(value, low, high, name):
    if not math.isfinite(value) or not low <= value <= high:
        raise ValueError(f"{name} must be finite and between {low} and {high}")


def budget(crop, recovery=DEFAULT_RECOVERY, duration_multiplier=1.0, supply=1.0):
    finite_range(recovery, 0, 1, "recovery")
    finite_range(duration_multiplier, .5, 2, "duration multiplier")
    finite_range(supply, 0, 1, "supply")
    for fraction in (crop.seed_dry_fraction, crop.product_dry_fraction,
                     crop.residue_dry_fraction):
        finite_range(fraction, 0, 1, "dry fraction")
    for amount in (crop.growth_hours, crop.average_kw, crop.seed_kg, crop.product_kg,
                   crop.residue_kg, crop.retained_nutrient_kg, crop.transpired_water_kg,
                   crop.propagation_reserve_kg, crop.food_units_per_portion):
        finite_range(amount, 0, float("inf"), "crop amount")
    if crop.growth_hours <= 0 or crop.average_kw <= 0 or crop.portion_kg <= 0:
        raise ValueError("Duration, power and portion must be positive")
    if not math.isfinite(crop.portion_kg):
        raise ValueError("Portion must be finite")
    if crop.propagation_reserve_kg > crop.product_kg:
        raise ValueError("Propagation reserve exceeds product")
    dry_in = crop.seed_kg * crop.seed_dry_fraction
    dry_out = (crop.product_kg * crop.product_dry_fraction
               + crop.residue_kg * crop.residue_dry_fraction)
    carbohydrate = dry_out - dry_in - crop.retained_nutrient_kg
    tissue_water_gain = (crop.product_kg + crop.residue_kg - dry_out
                         - (crop.seed_kg - dry_in))
    if carbohydrate < 0 or tissue_water_gain < 0:
        raise ValueError("This growth-only surrogate cannot represent negative growth")
    # Formal net reaction: 44 CO2 + 18 H2O -> 30 CH2O + 32 O2 by mass.
    co2 = carbohydrate * 44 / 30
    reaction_water = carbohydrate * 18 / 30
    oxygen = carbohydrate * 32 / 30
    gross_water = tissue_water_gain + reaction_water + crop.transpired_water_kg
    recovered_water = crop.transpired_water_kg * recovery
    vapour = crop.transpired_water_kg - recovered_water
    inputs = dict(planting_stock=crop.seed_kg, water=gross_water,
                  retained_nutrient=crop.retained_nutrient_kg, co2=co2)
    outputs = dict(product_including_seed_reserve=crop.product_kg,
                   organic_residue=crop.residue_kg, recovered_process_water=recovered_water,
                   room_water_vapour=vapour, oxygen=oxygen)
    energy = crop.growth_hours * crop.average_kw
    chemical = carbohydrate * CARBOHYDRATE_MJ_PER_KG / MJ_PER_KWH
    vapour_energy = vapour * VAPORIZATION_MJ_PER_KG / MJ_PER_KWH
    room_heat = energy - chemical - vapour_energy
    if room_heat < 0:
        raise ValueError("Electrical input cannot fund the proposed energy outputs")
    requested_kw = min(RACK_MAX_KW, crop.average_kw / duration_multiplier)
    received_kw = requested_kw * supply
    hours = energy / received_kw if received_kw else None
    portions = (crop.product_kg - crop.propagation_reserve_kg) / crop.portion_kg
    food = portions * crop.food_units_per_portion
    return dict(
        crop=crop.name, inputs_kg=inputs, outputs_kg=outputs,
        mass_residual_kg=sum(inputs.values())-sum(outputs.values()),
        net_makeup_water_kg=gross_water-recovered_water,
        carbohydrate_equivalent_gain_kg=carbohydrate,
        electric_kwh=energy, stored_chemical_kwh=chemical,
        direct_room_heat_kwh=room_heat, exported_vapour_latent_kwh=vapour_energy,
        energy_residual_kwh=energy-chemical-room_heat-vapour_energy,
        portions_after_seed_reserve=portions, food_units=food,
        requested_kw=requested_kw, received_kw=received_kw,
        ideal_growth_hours=hours, turnaround_hours=TURNAROUND_HOURS,
        food_units_per_day=food*HOURS_PER_DAY/(hours+TURNAROUND_HOURS) if hours else 0,
    )


def farm(potato_racks, lettuce_racks, crew, self_seeded_lettuce=False):
    for count in (potato_racks, lettuce_racks, crew):
        if not isinstance(count, int) or isinstance(count, bool) or count < 0:
            raise ValueError("Rack and crew counts must be non-negative integers")
    if crew == 0:
        raise ValueError("Crew count must be positive")
    potato, leaf, seed = (budget(CROPS[k]) for k in ("potato", "lettuce", "lettuce_seed"))
    potato_rate = potato["food_units_per_day"]
    leaf_rate = leaf["food_units_per_day"]
    if self_seeded_lettuce:
        # 3 food crops + 1 seed crop: reserve one seed packet and supply 3 food sowings.
        leaf_rate = 3*leaf["food_units"]*HOURS_PER_DAY / (
            3*(leaf["ideal_growth_hours"]+TURNAROUND_HOURS)
            + seed["ideal_growth_hours"]+TURNAROUND_HOURS)
    production = potato_racks*potato_rate + lettuce_racks*leaf_rate
    demand = crew*HOURS_PER_DAY*BASELINE_FOOD_UNITS_PER_HOUR
    return dict(potato_racks=potato_racks, lettuce_racks=lettuce_racks, crew=crew,
                lettuce_stock="renewed by seed crops" if self_seeded_lettuce else "supplied externally",
                food_units_per_day=production, baseline_demand_per_day=demand,
                nominal_food_coverage_fraction=production/demand)


def report():
    return dict(
        status="Authored ideal design budgets, NOT gameplay or biological validation",
        assumptions=dict(recovery=DEFAULT_RECOVERY, rack_max_kw=RACK_MAX_KW,
                         turnaround_hours=TURNAROUND_HOURS,
                         food_units_per_crew_hour=BASELINE_FOOD_UNITS_PER_HOUR),
        crops={key: budget(crop) for key, crop in CROPS.items()},
        farms=[farm(1, 1, 1), farm(1, 1, 3), farm(2, 1, 1),
               farm(1, 1, 1, True), farm(2, 1, 1, True)],
        sensitivity={name: budget(CROPS["potato"], **options) for name, options in {
            "half_supply": {"supply": .5}, "no_supply": {"supply": 0},
            "faster_setting": {"duration_multiplier": .5},
            "slower_setting": {"duration_multiplier": 2},
            "no_condensate_recovery": {"recovery": 0},
        }.items()},
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--compact", action="store_true", help="Print budgets and farm coverage only")
    args = parser.parse_args()
    result = report()
    if args.compact:
        result = {
            "crops": {key: {k: value[k] for k in (
                "ideal_growth_hours", "food_units", "net_makeup_water_kg", "electric_kwh",
                "direct_room_heat_kwh", "mass_residual_kg")} for key, value in result["crops"].items()},
            "farms": result["farms"],
        }
    print(json.dumps(result, indent=2, allow_nan=False))
