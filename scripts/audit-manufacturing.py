"""Read-only Manufacturing research audit; redirect evidence to ignored .local/.

Reuses the furnace repair audit. Native definition facts are separate from the
unregistered Manufacturing proposal. Never writes game files or reads saves.
"""
import argparse
import importlib.util
import json
from pathlib import Path


def audit(game):
    module = importlib.util.spec_from_file_location(
        "furnace_repair_audit", Path(__file__).with_name("audit-furnace-repair.py"))
    repair = importlib.util.module_from_spec(module)
    module.loader.exec_module(repair)
    result = repair.audit(game)
    # Those fields belong to the superseded twelve-sink research, not native data.
    result.pop("authored_casting_budget")
    result.pop("native_equivalent_output_base_value")
    data = game / "Ostranauts_Data/StreamingAssets/data"
    owners = {r["strName"]: r for r in repair.records(data / "condowners")}
    triggers = {r["strName"]: r for r in repair.records(data / "condtrigs")}
    loot = {r["strName"]: r for r in repair.records(data / "loot")}
    result["tool_trigger_definitions"] = {
        key: {field: triggers[key].get(field) for field in
              ("aReqs", "aForbids", "aTriggers", "bAND")}
        for key in ("TIsToolMortorq", "TIsToolSoldering", "TIsToolWelding")}
    # Candidates by starting flag only: this is not a runtime usability evaluator.
    flags = {"IsToolMortorq", "IsToolSoldering", "IsToolWelding"}
    result["tool_definition_candidates"] = []
    for row in owners.values():
        present = {c.split("=", 1)[0] for c in row.get("aStartingConds", [])}
        if present & flags:
            values = repair.stats(row)
            result["tool_definition_candidates"].append({
                "id": row["strName"], "tool_flags": sorted(present & flags),
                "mass_kg": values.get("StatMass"),
                "base_price": values.get("StatBasePrice")})
    result["merchant_leaf_candidates"] = {
        key: {"exists": key in loot, "type": loot.get(key, {}).get("strType")}
        for key in ("ItmOKLGSupplyKioskInv", "ItmOKLGFixer",
                    "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv")}
    result["native_sink_starting_flags"] = [
        c for c in owners["ItmHeatSink01"].get("aStartingConds", [])
        if not c.startswith("Stat")]
    result["scope"] = (
        "Blue Bottle Games native data only. No Manufacturing definitions are "
        "registered. Trigger flags and stock tables do not prove live repair "
        "gathering, merchant offers, tool readiness or gameplay compatibility.")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-root", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(audit(args.game_root), indent=2))
