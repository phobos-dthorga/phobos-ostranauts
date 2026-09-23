"""Read-only, reproducible vanilla comparison; no game data is bundled.

Usage: python scripts/audit-vanilla-economy.py --game PATH --output REPORT.md
Prices are definition values, not live merchant quotes. Unsupported loot syntax
raises rather than silently omitting a product. Only selected direct loot is used.
"""
import argparse
from pathlib import Path
import re
from ostranauts_json import read_json


def stat(item, name):
    terms = [s for s in item.get("aStartingConds", []) if s.startswith(name + "=")]
    return float(terms[-1].rsplit("x", 1)[1]) if terms else 0.0


def term(value):
    match = re.fullmatch(r"([^=|]+)=([\d.]+)x([\d.]+)(?:-([\d.]+))?", value)
    if not match:
        raise ValueError("Unsupported audit loot term: " + value)
    name, probability, low, high = match.groups()
    return name, float(probability), float(low), float(high or low)


def run(game):
    data = game / "Ostranauts_Data/StreamingAssets/data"
    tables = {}
    for name in ("condowners", "installables", "loot"):
        tables[name] = {row["strName"]: row for f in sorted((data / name).rglob("*.json")) for row in read_json(f)}
    objects, jobs, loot = (tables[n] for n in ("condowners", "installables", "loot"))
    selected = ["ItmWall1x1Loose", "ItmRCSCluster01Loose", "ItmBattery02Loose",
                "ItmAirPump02OffLoose", "ItmTowingBrace01Loose", "ItmNavModMobo"]
    lines = ["# Vanilla economy audit", "", "Generated from the installed vanilla definitions; excludes Workshop overrides.",
             "Base values are not merchant quotes. Dismantling products below are newly generated, before wear/market adjustments.", "",
             "| Native item | kg | Base value | Dismantling value lower / upper bound | Upper recovery / base | Output kg bounds |",
             "|---|---:|---:|---:|---:|---:|"]
    details = []
    for name in selected:
        item = objects[name]
        job = next(j for j in jobs.values() if j.get("strActionCO") == name and j.get("strProgressStat") == "StatDismantleProgress")
        outputs = loot[job["strLootOut"]]
        if outputs.get("aLoots"):
            raise ValueError("Nested loot needs explicit audit support: " + outputs["strName"])
        cost = [0.0, 0.0, 0.0]
        mass = [0.0, 0.0]
        products = []
        for entry in outputs["aCOs"]:
            product, chance, low, high = term(entry)
            price = stat(objects[product], "StatBasePrice") or stat(objects[product], "StatMass")
            kg = stat(objects[product], "StatMass")
            cost[0] += price * (low if chance == 1 else 0)
            cost[1] += price * chance * (low + high) / 2
            cost[2] += price * high
            mass[0] += kg * (low if chance == 1 else 0)
            mass[1] += kg * high
            products.append(f"{product} × {low:g}" + (f"–{high:g}" if high != low else ""))
        base = stat(item, "StatBasePrice")
        lines.append(f"| `{name}` | {stat(item, 'StatMass'):g} | ${base:,.2f} | " + " / ".join(f"${cost[i]:,.2f}" for i in (0,2)) +
                     f" | {cost[2]/base:.1%} | {mass[0]:g}–{mass[1]:g} |")
        details.append(f"- `{outputs['strName']}`: " + "; ".join(products) + ".")
    lines += ["", "Towing-brace loose form is **one 90 kg half**, not the complete 180 kg brace.",
              "Bounds use the counts written in the definitions. Native item loot floors random quantities; upper endpoints are conservative bounds, not a measured distribution or expected payout.",
              "The wall is an economic exception: its recovery range extends above its fully restored value; even minimum recovery exceeds the worn item's value.",
              "The nav board also creates more output mass than its input. Neither anomaly is a target for Phobos.", "", *details,
              "", "## Native maintenance thresholds", "", "| Item | Install / uninstall | Repair | Dismantle | Damage maximum |", "|---|---:|---:|---:|---:|"]
    for name in selected + ["ItmRCSCluster01DmgLoose", "ItmBattery02DmgLoose", "ItmAirPump02DmgLoose", "ItmTowingBrace01DmgLoose", "ItmNavModMoboDmg"]:
        item = objects[name]
        values = [stat(item, "Stat" + s) for s in ("InstallProgressMax", "UninstallProgressMax", "RepairProgressMax", "DismantleProgressMax", "DamageMax")]
        lines.append(f"| `{name}` | {values[0]:g} / {values[1]:g} | {values[2]:g} | {values[3]:g} | {values[4]:g} |")
        for job in jobs.values():
            if job.get("strActionCO") == name and job.get("strProgressStat") == "StatRepairProgress":
                details.append(f"- `{job['strName']}` repair inputs: " + ", ".join(job.get("aInputs", [])))
    lines += ["", "Zero in this table means the stat is absent, not an instant action. Native installation, removal and repair advance 5 units per unmodified 3.6-second tick; dismantling advances 1. Tool/skill modifiers and hauling are additional.",
              "", "## Merchant multipliers", "", "| Native discount table | Range |", "|---|---|"]
    for suffix in ("BuyKiosk", "SellKioskScrap", "BuyFixer", "SellFixer", "BuyKioskScrapVORB", "SellKioskScrapVORB", "BuySanDiego", "SellSanDiego"):
        name = "CONDTraderDiscount" + suffix
        _, _, lo, hi = term(loot[name]["aCOs"][0])
        lines.append(f"| `{name}` | {lo:g}–{hi:g} × |")
    lines += ["", "Buy is the merchant buying from the player; Sell is the merchant selling to the player. Market category supply/demand and negotiation can further change a live quote. Compare input and outputs at the same buyer/market; do not compare an item's shop purchase quote with scrap's inventory value.",
              "", "## Repair inputs", "", *[d for d in details if 'repair inputs:' in d], ""]
    return "\n".join(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.write_text(run(args.game), encoding="utf-8")
    print("Vanilla economy audit written:", args.output)
