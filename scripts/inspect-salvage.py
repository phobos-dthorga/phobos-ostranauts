"""Inspect selected salvage definitions and native output references, read-only.

This is a static load-order view, not the runtime database: plugin patches,
generated definitions, ignore patterns and same-package duplicate ordering may
alter it. Output belongs under .local/ and includes no save data.
"""

import argparse
import json
from pathlib import Path
import re

from ostranauts_json import read_json

KINDS = ("condowners", "items", "installables", "loot")


def load_definitions(game):
    mods = game / "Ostranauts_Data/Mods"
    sources = [("core", game / "Ostranauts_Data/StreamingAssets/data")]
    warnings = []
    for group in read_json(mods / "loading_order.json"):
        if group.get("aIgnorePatterns"):
            warnings.append("Configured ignore patterns are not evaluated by this audit.")
        for entry in group.get("aLoadOrder", []):
            name, *flags = entry.split("|")
            if name == "core" or "disabled" in flags:
                continue
            path = Path(name)
            if not path.is_absolute():
                path = mods / path
            sources.append((path.name, path / "data"))
    db = {kind: {} for kind in KINDS}
    origins = {}
    for label, root in sources:
        seen = set()
        for kind in KINDS:
            for path in sorted((root / kind).rglob("*.json")):
                try:
                    records = read_json(path)
                except (OSError, ValueError) as exc:
                    warnings.append(f"{label}/{path.relative_to(root).as_posix()}: {type(exc).__name__}")
                    continue
                if not isinstance(records, list):
                    warnings.append(f"{label}/{path.relative_to(root).as_posix()}: non-list definitions skipped")
                    continue
                for record in records:
                    if not isinstance(record, dict) or "strName" not in record:
                        continue
                    key = (kind, record["strName"])
                    if key in seen:
                        warnings.append(f"Same-package duplicate: {label}/{kind}/{key[1]}")
                    seen.add(key)
                    db[kind][key[1]] = record
                    origins[key] = f"{label}/data/{path.relative_to(root).as_posix()}"
    return db, origins, warnings


def static_stats(record):
    stats = {}
    for value in record.get("aStartingConds", []):
        match = re.fullmatch(r"([^=]+)=1(?:\.0)?x(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)", value)
        if match:
            stats[match[1]] = float(match[2])
    return stats


def output_bounds(entries, nested, db):
    """Bound simple guaranteed loot, refusing to guess more complex rolls."""
    totals = {key: [0.0, 0.0] for key in ("mass", "base_price")}
    issues = ["Nested loot is not expanded"] if nested else []
    products = []
    for entry in entries:
        match = re.fullmatch(r"([^=]+)=1(?:\.0)?x(\d+)(?:-(\d+))?", entry)
        if not match:
            issues.append(f"Unsupported output expression: {entry}")
            continue
        name, low, high = match.groups()
        counts = [int(low), int(high or low)]
        if counts[0] > counts[1]:
            issues.append(f"Reversed output range: {entry}")
            continue
        stats = static_stats(db["condowners"].get(name, {}))
        product = {"id": name, "count_bounds": counts}
        for key, stat in (("mass", "StatMass"), ("base_price", "StatBasePrice")):
            amount = stats.get(stat)
            product[key] = amount
            if amount is None or amount < 0:
                issues.append(f"Missing or invalid fixed {stat}: {name}")
                totals[key] = None
            elif totals[key] is not None:
                totals[key] = [total + amount * count
                               for total, count in zip(totals[key], counts)]
        products.append(product)
    # Partial arithmetic must never look like a complete physical balance.
    complete = not issues
    return {"complete": complete, "products": products, "issues": issues,
            "mass_bounds": totals["mass"] if complete else None,
            "base_price_bounds": totals["base_price"] if complete else None}


def inspect_item(name, db, origins):
    co = db["condowners"].get(name)
    if co is None:
        return {"id": name, "missing": True}
    stats = static_stats(co)
    item = db["items"].get(co.get("strItemDef"), {})
    actions = []
    for action in db["installables"].values():
        if action.get("strActionCO") != name or action.get("strJobType") != "dismantle":
            continue
        loot_id = action.get("strLootOut")
        loot = db["loot"].get(loot_id, {})
        direct = action.get("aLootCOs")
        # Installables.Create uses named loot first; direct outputs are bare IDs.
        entries = (loot.get("aCOs", []) if loot_id is not None else
                   [value + "=1.0x1" for value in (direct or [])])
        nested = loot.get("aLoots", []) if loot_id is not None else []
        bounds = output_bounds(entries, nested, db)
        if (loot_id is not None and not loot) or (loot_id is None and not direct):
            bounds.update(complete=False, mass_bounds=None, base_price_bounds=None)
            bounds["issues"].append("No resolved output definition; not evidence of zero yield")
        actions.append({
            "id": action["strName"],
            "source": origins[("installables", action["strName"])],
            "target_trigger": action.get("CTThem"),
            "progress_stat": action.get("strProgressStat"),
            "step_hours": action.get("fDuration"),
            "tool_triggers": action.get("aToolCTsUse"),
            "custom_finish_interaction": action.get("strFinishInteraction"),
            "no_destructable": action.get("bNoDestructable", False),
            "loot_id": loot_id,
            "loot_source": origins.get(("loot", loot_id)),
            "loot_entries": loot.get("aCOs", []),
            "nested_loot": loot.get("aLoots", []),
            "direct_outputs": action.get("aLootCOs"),
            "output_audit": bounds,
        })
    return {
        "id": name, "source": origins[("condowners", name)],
        "label": co.get("strNameFriendly") or item.get("strNameFriendly") or name,
        "mass": stats.get("StatMass"), "base_price": stats.get("StatBasePrice"),
        "dismantle_progress_max": stats.get("StatDismantleProgressMax"),
        "stack_limit_declared": co.get("nStackLimit"),
        "item_definition": co.get("strItemDef"),
        "item_geometry": {"socket_columns": item.get("nCols"),
                          "socket_add_count": len(item.get("aSocketAdds", []))},
        "dismantle_actions": actions,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-path", type=Path, required=True)
    parser.add_argument("--item", action="append", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    game = args.game_path.resolve()
    output = args.output.resolve()
    # This inspector writes research only within the repository's ignored area.
    research = Path(__file__).resolve().parents[1] / ".local"
    if not output.is_relative_to(research) or output.suffix.lower() != ".json":
        parser.error("Write a .json report under this repository's .local/ directory.")
    db, origins, warnings = load_definitions(game)
    records = [inspect_item(name, db, origins) for name in args.item]
    report = {"scope": "Static definitions; runtime/plugin changes not simulated",
              "items": records, "warnings": warnings}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"items": len(records), "missing": sum(x.get("missing", False) for x in records),
                      "warnings": warnings}))
    return 1 if warnings or any(x.get("missing") for x in records) else 0


if __name__ == "__main__":
    raise SystemExit(main())
