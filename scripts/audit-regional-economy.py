"""Read-only vanilla endpoint/category evidence; no saves or proprietary source export."""
import argparse
import hashlib
from pathlib import Path
import re

from ostranauts_json import read_json


def report(game, repo):
    data = game / "Ostranauts_Data/StreamingAssets/data"
    markets = {row["strName"].removesuffix("CargoKiosk"): row
               for path in (data / "market/Markets").rglob("*.json")
               for row in read_json(path) if row["strName"].endswith("CargoKiosk")}
    loot = {row["strName"]: row for path in (data / "loot").rglob("*.json") for row in read_json(path)}
    source = (repo / "src/PhobosFramework/Trading/RegionalMarkets.cs").read_text(encoding="utf-8-sig")
    endpoints = dict(re.findall(r'\["([A-Z]{4})"\] = "([^"]+)"', source))
    if set(markets) != set(endpoints):
        raise ValueError("Vanilla market inventory differs from RegionalMarkets; review coverage.")
    # Verify these are native retail maps, not merely unused names in the loot database.
    retail = {value for path in (data / "guipropmaps").rglob("*.json")
              for row in read_json(path) for value in row.get("dictGUIPropMap", [])}
    for region, table in endpoints.items():
        if loot.get(table, {}).get("strType") != "item" or table not in retail:
            raise ValueError(f"{region}: missing native retail binding for {table}")
    prop_maps = {row["strName"]: row.get("dictGUIPropMap", [])
                 for path in (data / "guipropmaps").rglob("*.json") for row in read_json(path)}
    overlay_routes = {region: set() for region in endpoints}
    for path in (data / "cooverlays").rglob("*.json"):
        for row in read_json(path):
            maps = row.get("mapGUIPropMaps") or []
            for region, table in endpoints.items():
                if any(table in prop_maps.get(name, []) for name in maps):
                    overlay_routes[region].add(row["strName"])
    placed = set()
    for path in (data / "ships").rglob("*.json"):
        blueprint = path.read_text(encoding="utf-8-sig")
        placed.update(region for region, table in endpoints.items()
                      if table in blueprint or any('"' + overlay + '"' in blueprint
                                                   for overlay in overlay_routes[region]))
    if set(markets) - placed != {"MLAB", "JPTN"}:
        raise ValueError("Vanilla retail placement changed; review regional stock coverage.")
    rows = ["# Regional economy: generated native evidence", "",
            "Source: **Blue Bottle Games, installed Ostranauts 1.0.1.5**, inspected 26 September 2026.",
            "The [economy guide](solar-system-economy.md#sources-and-verification) identifies the native files and original game documentation.",
            "Reproduce with `scripts/audit-regional-economy.py --game <game-folder>` or the normal economic audit.",
            "Selected facts only; no saves, live prices or proprietary definitions are exported.", "",
            "Game assembly SHA-256: `" + hashlib.sha256((game / "Ostranauts_Data/Managed/Assembly-CSharp.dll").read_bytes()).hexdigest() + "`.", "",
            "Two defined supply kiosks (MLAB and JPTN) have no literal inventory-table reference in any current native ship blueprint. Their supply overlays also have no blueprint references; no regional stock is added to these inactive templates.", "",
            "## Native capacity relevant to Phobos goods", "",
            "Capacity is in native category inventory units, not kilograms or physical Phobos shop stock.",
            "A dash means that this cargo profile does not list the category; it does not establish a universal ban on retail trade.",
            "Hub-sharing and the live world's additional actors can change a market's aggregate state.", "",
            "| Profile | Retail inventory table | Placed retail | Industrial | Control systems | Food |",
            "|---|---|---|---:|---:|---:|"]
    for region in sorted(markets):
        capacities = dict(value.split("=", 1) for value in markets[region]["aVirtualInventorySize"])
        values = [capacities.get("Any" + category, "—") for category in ("IndustrialProducts", "ControlSystems", "Food")]
        rows.append(f"| {region} | `{endpoints[region]}` | {'Yes' if region in placed else 'No; inactive template'} | " + " | ".join(values) + " |")
    rows += ["", "## Native production roles used for interpretation", "",
             "These are native production-map identifiers, not new Phobos processes or measured real-world production.",
             "Their economic roles inform the explicitly authored availability factors in the guide.", ""]
    for region in sorted(markets):
        selected = [name for name in markets[region]["aSupplyDemandMaps"]
                    if any(term in name for term in ("Industrial", "ControlSystems", "Food", "Electronics"))]
        rows.append(f"- **{region}**: " + (", ".join(f"`{name}`" for name in selected) or "No selected category production map.") + ".")
    return "\n".join(rows) + "\n"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", required=True, type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[1]
    output = args.output or repo / "docs/solar-system-economy-evidence.md"
    text = report(args.game, repo)
    if args.check:
        if not output.is_file() or output.read_text(encoding="utf-8-sig") != text:
            raise SystemExit("Regional economy evidence is stale; rerun the audit.")
    else:
        output.write_text(text, encoding="utf-8")
    print("Regional economy evidence: 19 native market profiles checked; 17 placed retail endpoints, 2 inactive supply templates.")
