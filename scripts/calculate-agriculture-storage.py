"""Source-backed DESIGN estimates for bulk storage; no game/save access.

Uses current runtime crop demand, not the older gross-transpiration design model.
Continuous ideal cultivation gives a conservative supply envelope: zero turnaround,
no stress, full power and atmosphere, no automatic drainage or recovery credit.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "docs/agriculture-bulk-storage-calculations.md"
TANK_KG = 120.0  # Reviewed R3 material budget; verified against source below.
TANK_DRY_KG = 25.0
TANK_TILES = 9
NUTRIENT_PACKAGE_KG = .5  # Reviewed implemented stock size.
TRAVEL_SECONDS = (0, 30, 120)  # Illustrative round trips, not observed pathfinding.


def source_inputs():
    paths = ["src/PhobosAgriculture/Core/Crop.cs", "src/PhobosAgriculture/Definitions.cs",
             "src/PhobosAgriculture/Core/NutrientRecovery.cs",
             "src/PhobosAgriculture/Core/DrainageRecovery.cs",
             "src/PhobosAgriculture/Core/TreatmentCartridge.cs",
             "src/PhobosAgriculture/IrrigationDefinitions.cs", "src/PhobosAgriculture/BulkDefinitions.cs"]
    texts = {p: (ROOT/p).read_text(encoding="utf-8-sig") for p in paths}
    crops = {}
    for name, body in re.findall(r'public static readonly Crop \w+ = new\("([^"]+)",\s*([^;]+)\);', texts[paths[0]]):
        values = [float(x.strip()) for x in body.split(",")]
        if len(values) != 9:
            raise ValueError("Crop constructor changed; review this model")
        crops[name] = dict(zip(("hours", "kw", "seed", "final", "carbon", "nutrient", "water", "vapour", "seed_carbon"), values))
    if set(crops) != {"potato", "lettuce", "lettuce-seed"}:
        raise ValueError("Crop coverage changed; extend the storage study")

    def number(path, key):
        match = re.search(r"\b"+key+r"\s*=\s*([\d.]+)\s*[,;]", texts[path])
        if not match:
            raise ValueError(f"Missing numeric constant: {key}")
        return float(match[1])

    if (number(paths[6], "CapacityKg"), number(paths[6], "DryKg"), number(paths[6], "NutrientKg")) != (TANK_KG, TANK_DRY_KG, NUTRIENT_PACKAGE_KG):
        raise ValueError("R3 material budget changed; review the endurance model")
    return dict(crops=crops,
                buffer_kg=number(paths[0], "ReservoirKg"),
                nutrient_capacity=number(paths[0], "NutrientCapacityKg"),
                water_package=number(paths[1], "IrrigationKg"),
                recovery=number(paths[2], "Fraction"),
                workup_kwh_kg=number(paths[2], "KWhPerKg"),
                workup_kw=number(paths[2], "PowerKW"),
                water_yield=number(paths[3], "WaterYield"),
                nutrient_yield=number(paths[3], "NutrientYield"),
                treatment_kwh_kg=number(paths[3], "KWhPerKg"),
                treatment_kw=number(paths[3], "PowerKW"),
                medium_kg=number(paths[3], "CartridgeKg"),
                cartridge_capacity=number(paths[4], "CapacityKg"),
                w2_dry=number(paths[5], "DryKg"),
                # Fingerprint decoded source with LF line endings, independent of checkout OS.
                source_sha256={p: hashlib.sha256(texts[p].encode("utf-8")).hexdigest() for p in paths})


def count_loads(kg, package):
    if not math.isfinite(kg) or kg < 0 or not math.isfinite(package) or package <= 0:
        raise ValueError("Invalid quantity")
    return max(0, math.ceil(kg/package-1e-10))


def scenario(inputs, crop_id, racks, days, pace=1):
    if racks not in (1, 4, 8) or days not in (7, 30) or pace not in (.5, 1, 2):
        raise ValueError("Unsupported study scenario")
    c = inputs["crops"][crop_id]
    cycles = days*24/(c["hours"]*pace)
    water, nutrient = (c[k]*cycles*racks for k in ("water", "nutrient"))
    # Reserve 0.5 kg of each 20 kg liquid buffer for mixed-feed solute/headroom.
    # A deliberately conservative water-equivalent envelope, not a packing algorithm.
    initial_water = (inputs["buffer_kg"]-inputs["nutrient_capacity"])*(racks+1)
    initial_nutrient = inputs["nutrient_capacity"]  # Central W2 only; rack stocks excluded.
    loads = count_loads(water, inputs["water_package"])+count_loads(nutrient, .04)
    bigger_loads = count_loads(water, inputs["water_package"])+count_loads(nutrient, NUTRIENT_PACKAGE_KG)
    return dict(crop=crop_id, racks=racks, days=days, pace=pace, cycles_per_rack=cycles,
                water_kg=water, nutrient_kg=nutrient, supply_mass_kg=water+nutrient,
                crop_electric_kwh=c["hours"]*c["kw"]*cycles*racks,
                crop_only_floor_tiles=16*racks, rack_w2_floor_tiles=16*racks+4,
                rack_w2_dry_kg=80*racks+inputs["w2_dry"],
                with_r3_floor_tiles=16*racks+4+TANK_TILES,
                all_trip_supplies_and_equipment_kg=80*racks+inputs["w2_dry"]+water+nutrient,
                with_r3_trip_mass_kg=80*racks+inputs["w2_dry"]+TANK_DRY_KG+water+nutrient,
                water_loads=count_loads(water, inputs["water_package"]),
                nutrient_loads=count_loads(nutrient, .04),
                large_nutrient_loads=count_loads(nutrient, NUTRIENT_PACKAGE_KG),
                manual_minutes={str(t):loads*(10+t)/60 for t in TRAVEL_SECONDS},
                large_stock_minutes={str(t):bigger_loads*(10+t)/60 for t in TRAVEL_SECONDS},
                initial_water_kg=initial_water, baseline_water_days=initial_water/(water/days),
                r3_water_days=(initial_water+TANK_KG)/(water/days),
                central_nutrient_days=initial_nutrient/(nutrient/days),
                baseline_water_reloads=count_loads(max(0,water-initial_water),inputs["water_package"]),
                r3_water_reloads=count_loads(max(0,water-initial_water-TANK_KG),inputs["water_package"]),
                baseline_nutrient_reloads=count_loads(max(0,nutrient-initial_nutrient),.04))


def recovery_case(inputs, crop_id):
    c=inputs["crops"][crop_id]
    # Healthy authored harvest partition, matching CropState.Harvest.
    residue={"potato":.8,"lettuce":.2,"lettuce-seed":1.18}[crop_id]
    allocated=c["nutrient"]*residue/c["final"]
    concentrate=allocated*inputs["recovery"]
    spent=residue-concentrate
    energy=max(.001,residue*inputs["workup_kwh_kg"])+max(.001,concentrate*inputs["workup_kwh_kg"])
    return dict(residue_kg=residue,concentrate_kg=concentrate,makeup_kg=concentrate,
                mixture_kg=2*concentrate,spent_biomass_kg=spent,
                mass_residual_kg=residue+concentrate-spent-2*concentrate,
                crew_setup_minutes=2,electric_kwh=energy,machine_minutes=energy/inputs["workup_kw"]*60,
                extra_water_kg=0)


def treatment_case(inputs, water=19.5, nutrient=.1):
    total=water+nutrient
    if any(not math.isfinite(x) or x < 0 for x in (water, nutrient)) or not 0 < total <= inputs["cartridge_capacity"]:
        raise ValueError("Treatment example must fit one fresh cartridge and contain material")
    medium=total/inputs["cartridge_capacity"]*inputs["medium_kg"]
    recovered_w=water*inputs["water_yield"]; recovered_n=nutrient*inputs["nutrient_yield"]
    reject=total+medium-recovered_w-recovered_n
    return dict(input_water_kg=water,input_nutrient_kg=nutrient,spent_medium_kg=medium,
                recovered_water_kg=recovered_w,recovered_nutrient_kg=recovered_n,reject_kg=reject,
                cartridge_remaining_capacity_kg=inputs["cartridge_capacity"]-total,
                electricity_kwh=total*inputs["treatment_kwh_kg"],
                full_power_minutes=total*inputs["treatment_kwh_kg"]/inputs["treatment_kw"]*60,
                crew_setup_minutes=15,
                mass_residual_kg=total+medium-recovered_w-recovered_n-reject)


def report():
    inputs=source_inputs()
    return dict(status="Source-backed R3 estimates; no gameplay validation",
                inputs=inputs,proposal=dict(tank_kg=TANK_KG,tank_dry_kg=TANK_DRY_KG,tank_tiles=TANK_TILES,
                nutrient_package_kg=NUTRIENT_PACKAGE_KG),
                scenarios=[scenario(inputs,c,r,d,p) for c in inputs["crops"] for r in (1,4,8) for d in (7,30) for p in (.5,1,2)],
                recovery={c:recovery_case(inputs,c) for c in inputs["crops"]},
                treatment=treatment_case(inputs))


def markdown(data):
    lines=["# Agriculture bulk-storage calculations", "", "Generated by `scripts/calculate-agriculture-storage.py`; do not edit tables by hand.", "",
           "**Design estimates, not gameplay validation.** Current runtime net crop demands; ideal continuous growth, zero turnaround, no automatic drainage/recovery credit. Pace 0.5 takes half the time; pace 2 takes twice the time. Full requested power, suitable atmosphere and prompt harvest/replant are assumed.", "",
           "Loads include initial supply from empty and round up whole 5 kg water / 40 g nutrient packets. Handling is 10 seconds per load plus the stated illustrative round trip, excluding planting, harvesting, inventory limits and specialist bonuses. Larger-stock handling is an illustrative equal-time comparison; actual W2 charge replacement includes native carrying and checked selection.", "",
           "## Demand and manual handling at default pace", "", "| Crop | Racks | Days | Water kg | Nutrient kg | Water loads | 40 g loads | 500 g loads | Minutes: no travel / 30 s / 120 s |", "|---|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for s in data["scenarios"]:
        if s["pace"]==1:
            minutes=" / ".join(f'{s["manual_minutes"][str(t)]:.1f}' for t in TRAVEL_SECONDS)
            lines.append(f'| {s["crop"]} | {s["racks"]} | {s["days"]} | {s["water_kg"]:.3f} | {s["nutrient_kg"]:.3f} | {s["water_loads"]} | {s["nutrient_loads"]} | {s["large_nutrient_loads"]} | {minutes} |')
    lines += ["", "## Endurance and space at default pace", "", "Water envelope: 19.5 kg per rack plus W2; 0.5 kg central dry stock, excluding rack dry stocks. Additional R3 is 120 kg of water and 25 kg dry equipment. Water and nutrient durations are separate limits; overall endurance cannot exceed the smaller one. Pipes, access aisles, seeds and output storage are excluded from floor/mass figures.", "", "| Crop | Racks | Baseline water days | With R3 days | Central nutrient days | Tiles baseline / R3 | 30-day supply mass kg | Dry kg baseline / R3 |", "|---|---:|---:|---:|---:|---:|---:|---:|"]
    for s in data["scenarios"]:
        if s["days"]==30 and s["pace"]==1:
            lines.append(f'| {s["crop"]} | {s["racks"]} | {s["baseline_water_days"]:.2f} | {s["r3_water_days"]:.2f} | {s["central_nutrient_days"]:.2f} | {s["rack_w2_floor_tiles"]} / {s["with_r3_floor_tiles"]} | {s["supply_mass_kg"]:.3f} | {s["rack_w2_dry_kg"]:.0f} / {s["rack_w2_dry_kg"]+TANK_DRY_KG:.0f} |')
    lines += ["", "## Growth-pace sensitivity: thirty-day demand", "", "| Crop | Racks | Pace | Water kg | Nutrient kg | Water loads after full baseline / full R3 |", "|---|---:|---:|---:|---:|---:|"]
    for s in data["scenarios"]:
        if s["days"]==30:
            lines.append(f'| {s["crop"]} | {s["racks"]} | {s["pace"]} | {s["water_kg"]:.3f} | {s["nutrient_kg"]:.3f} | {s["baseline_water_reloads"]} / {s["r3_water_reloads"]} |')
    lines += ["", "## Recovery per completed healthy cohort", "", "No recovery is credited against horizon totals: completed harvests, B2 setup, consumables, electricity and output clearance must occur first. Fractional growth-equivalent cycles are not fractional harvests.", "", "| Crop | Residue kg | Concentrate g | Makeup g | Mixture g | Spent biomass kg | Electricity kWh | Powered minutes |", "|---|---:|---:|---:|---:|---:|---:|---:|"]
    for c,r in data["recovery"].items():
        lines.append(f'| {c} | {r["residue_kg"]:.3f} | {r["concentrate_kg"]*1000:.2f} | {r["makeup_kg"]*1000:.2f} | {r["mixture_kg"]*1000:.2f} | {r["spent_biomass_kg"]:.5f} | {r["electric_kwh"]:.5f} | {r["machine_minutes"]:.3f} |')
    t=data["treatment"]
    lines += ["", f'One illustrative recorded-drainage batch: {t["input_water_kg"]} kg water + {t["input_nutrient_kg"]} kg nutrients; consumes {t["spent_medium_kg"]:.4f} kg cartridge medium; recovers {t["recovered_water_kg"]:.3f} kg water + {t["recovered_nutrient_kg"]:.3f} kg nutrients; retains {t["reject_kg"]:.4f} kg rejects. Needs {t["electricity_kwh"]:.3f} kWh, {t["full_power_minutes"]:.2f} powered minutes plus 15 crew setup minutes. The remaining cartridge has {t["cartridge_remaining_capacity_kg"]:.1f} kg treatment capacity. Outputs require fresh headroom.', "", "## Reproduction and source fingerprints", "", "Run `python scripts/calculate-agriculture-storage.py --check` to detect stale evidence; `--format json` includes all 54 scenarios and handling sensitivities. Use `--write` after reviewing source or proposal changes.", ""]
    for p,h in data["inputs"]["source_sha256"].items():
        lines.append(f'- `{p}` (UTF-8/LF, no BOM): `{h}`')
    return "\n".join(lines)+"\n"


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--format",choices=("markdown","json"),default="markdown")
    mode=parser.add_mutually_exclusive_group();mode.add_argument("--write",action="store_true");mode.add_argument("--check",action="store_true")
    args=parser.parse_args(); data=report(); rendered=markdown(data)
    if args.check:
        if not REPORT.exists() or REPORT.read_text(encoding="utf-8")!=rendered:
            raise SystemExit("Storage research evidence is stale; review and regenerate")
        print("Storage research evidence matches current source and proposal")
    elif args.write:
        REPORT.write_text(rendered,encoding="utf-8");print(REPORT)
    else:
        print(json.dumps(data,indent=2) if args.format=="json" else rendered)
