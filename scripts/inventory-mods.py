"""Read installed Ostranauts mod metadata and startup evidence without changing it.

Supply local paths on the command line; keep the JSON report under .local/.
Configured native packages and startup plugin entries are intentionally separate.
The report does not inspect saves or certify gameplay compatibility.
"""

import argparse
import hashlib
import json
from pathlib import Path
import re

from ostranauts_json import read_json


def fingerprint(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def inventory(game, workshop, player_log=None):
    mods_dir = game / "Ostranauts_Data" / "Mods"
    load_file = mods_dir / "loading_order.json"
    configuration = read_json(load_file)
    configured = {}
    for group in configuration:
        for index, entry in enumerate(group.get("aLoadOrder", [])):
            if entry == "core":
                continue
            name, *flags = entry.split("|")
            folder = Path(name)
            if not folder.is_absolute():
                folder = mods_dir / folder
            configured[folder.resolve()] = {
                "order": index,
                "disabled": "disabled" in flags,
            }

    folders = set(configured)
    for root in (workshop, mods_dir):
        if root.is_dir():
            folders.update(p.parent.resolve() for p in root.glob("*/mod_info.json"))
    packages, errors = [], []
    for folder in sorted(folders):
        try:
            data = read_json(folder / "mod_info.json")
            info = data[0] if isinstance(data, list) else data
            config = configured.get(folder)
            record = {
                "folder": folder.name,
                "name": info.get("strName"),
                "version": info.get("strModVersion"),
                "target_game": info.get("strGameVersion"),
                "author": info.get("strAuthor"),
                "url": info.get("strModURL"),
                "workshop_id": info.get("strWorkshopID") or
                               (folder.name if folder.name.isdecimal() else None),
                "configured": config,
                "notes": info.get("strNotes", ""),
                "licence_files": [],
                "assemblies": [],
            }
            for path in sorted(folder.rglob("*")):
                if not path.is_file():
                    continue
                relative = path.relative_to(folder).as_posix()
                if path.name.lower().startswith(("license", "licence", "copying")):
                    record["licence_files"].append(relative)
                if path.suffix.lower() == ".dll":
                    record["assemblies"].append({"file": relative, "sha256": fingerprint(path)})
            packages.append(record)
        except (OSError, ValueError, KeyError, IndexError, TypeError) as exc:
            errors.append({"folder": folder.name, "error": type(exc).__name__})

    log_file = game / "BepInEx" / "LogOutput.log"
    log = log_file.read_text(encoding="utf-8-sig", errors="replace") if log_file.exists() else ""
    report = {
        "counts": {
            "packages": len(packages),
            "configured_enabled": sum(bool(p["configured"] is not None and
                                           not p["configured"]["disabled"]) for p in packages),
            "configured_disabled": sum(bool(p["configured"] and p["configured"]["disabled"])
                                       for p in packages),
        },
        "load_order_sha256": fingerprint(load_file),
        "game_assembly_sha256": fingerprint(game / "Ostranauts_Data/Managed/Assembly-CSharp.dll"),
        "plugin_log_modified_epoch": log_file.stat().st_mtime if log_file.exists() else None,
        "startup_plugins": re.findall(r"\[Info\s*:\s*BepInEx\] Loading \[(.+?)\]", log),
        "framework_registration": re.findall(r"Registered \d+ (?:crafting recipes|powered machine definitions)\.", log),
        "packages": sorted(packages, key=lambda p: (p["configured"]["order"] if p["configured"] else 9999,
                                                    p["name"] or "")),
        "errors": errors,
    }
    if player_log:
        # Stream the log; it may be very large. Never read save files.
        builds = set()
        with player_log.open(encoding="utf-8-sig", errors="replace") as stream:
            for line in stream:
                match = re.search(r"Release Build:\s*([\d.]+)", line)
                if match:
                    builds.add(match[1])
        report["logged_game_builds"] = sorted(builds)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-path", required=True, type=Path)
    parser.add_argument("--workshop-path", type=Path)
    parser.add_argument("--player-log", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    game = args.game_path.resolve()
    workshop = args.workshop_path or game.parents[1] / "workshop/content/1022980"
    output = args.output.resolve()
    for source in (game, workshop.resolve()):
        if output.is_relative_to(source):
            parser.error("Write the report outside the game and Workshop directories.")
    if output.suffix.lower() != ".json":
        parser.error("The output report must be a .json file.")
    report = inventory(game, workshop, args.player_log)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"counts": report["counts"], "startup_plugin_count": len(report["startup_plugins"]),
                      "errors": report["errors"]}))
    return 1 if report["errors"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
