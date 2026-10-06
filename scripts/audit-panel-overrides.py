#!/usr/bin/env python3
"""Keep the press-twice rule true (Framework 0.125.0; owner rule, 6 October 2026).

On every control panel screen and F3 command, a change that needs other steps first (pausing, unlinking,
applying a draft, stopping work, cancelling a batch) is never refused for that reason: the first press warns,
the second does it. This check scans the player text catalogs of the mods named in
config/panel-override-audit.json for refusals that read like "do this first", and requires every one to be
classified there:

  offered  the second press is in place
  pending  a bookkeeping (A) or progress-losing (L) case still to convert, in the named round
  refused  stays a refusal: heat, contents, faults, damage, another console, or a choice only the player makes
  status   matched the wording but is not a do-first refusal (a status line, a label, a requirement)
  unused   no code uses it; remove it in the named round

A key may be classified one by one ("entries") or by a key pattern ("rules"). An entry whose key no longer
exists is reported too, so the record cannot go stale.

  python scripts/audit-panel-overrides.py --check [--format json]
  python scripts/audit-panel-overrides.py --pending
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
AUDIT = Path("config/panel-override-audit.json")
CLASSES = {"offered", "pending", "refused", "status", "unused"}
_VERBS = r"(pause|paused|stop|stopped|unlink|cancel|finish|apply|discard|empty|drain|release|disengage|undock|return|wait|cool)"
TEXT = re.compile(r"(?i)(\b" + _VERBS + r"\b[^.!?]{0,80}\b(first|before)\b)|(\b(first|before)\b[^.!?]{0,40}\b" + _VERBS + r"\b)")
KEY = re.compile(r"(_busy|_first|stop_rebind|_block|not_ready|hot_maintenance|charge_changed|feed_blocked|cancel_missing|line_drain|_protected|\.busy|captured|engaged)")
# Descriptions, help, settings, maintenance (native removal) and story text are not panel refusals.
IGNORE = re.compile(r"(_desc$|_description$|\.description$|_details$|^help$|\.help$|_help$|_setting$|\.setting$|^Maintenance\.|\.Maintenance\.|_guide|[Tt]ip|_hint$|^Story\.|_note$|Note$|^Plugin\.)")


def scan(root: Path, mods: list[str]) -> dict[tuple[str, str], str]:
    found: dict[tuple[str, str], str] = {}
    for mod in mods:
        catalog = root / "translations" / mod / "en.json"
        if not catalog.exists():
            continue
        for key, value in json.loads(catalog.read_text(encoding="utf-8-sig")).items():
            if isinstance(value, str) and not IGNORE.search(key) and (KEY.search(key) or TEXT.search(value)):
                found[(mod, key)] = value
    return found


def keys(root: Path, mod: str) -> set[str]:
    catalog = root / "translations" / mod / "en.json"
    return set(json.loads(catalog.read_text(encoding="utf-8-sig"))) if catalog.exists() else set()


def check(root: Path) -> dict:
    audit = json.loads((root / AUDIT).read_text(encoding="utf-8"))
    errors: list[str] = []
    mods = audit.get("mods", [])
    rules = [(re.compile(r["keys"]), r) for r in audit.get("rules", [])]
    entries = {(e["mod"], e["key"]): e for e in audit.get("entries", [])}
    for item in [*audit.get("rules", []), *audit.get("entries", [])]:
        if item.get("class") not in CLASSES:
            errors.append(f"unknown class {item.get('class')!r} in {item}")
        if item.get("class") in {"pending", "unused"} and not item.get("round"):
            errors.append(f"{item.get('mod', '')} {item.get('key', item.get('keys'))}: pending and unused need a round")
        if item.get("class") in {"refused", "pending"} and not item.get("note"):
            errors.append(f"{item.get('mod', '')} {item.get('key', item.get('keys'))}: say why it is refused or what the second press will do")
    found = scan(root, mods)
    unclassified = sorted(k for k in found if k not in entries and not any(p.search(k[1]) for p, _ in rules))
    for mod, key in unclassified:
        errors.append(f"{mod} {key}: a do-first refusal with no classification; add it to {AUDIT.as_posix()}")
    for (mod, key) in entries:
        if mod not in mods:
            errors.append(f"{mod} {key}: the mod is not in the audit's mods list")
        elif key not in keys(root, mod):
            errors.append(f"{mod} {key}: the key no longer exists; remove the entry")
    counts: dict[str, int] = {}
    for k in found:
        cls = entries[k]["class"] if k in entries else next((r["class"] for p, r in rules if p.search(k[1])), "unclassified")
        counts[cls] = counts.get(cls, 0) + 1
    pending = sorted((e.get("round", ""), e.get("mod", ""), e.get("key", ""), e.get("note", "")) for e in audit.get("entries", []) if e.get("class") in {"pending", "unused"})
    return {"schemaVersion": 1, "status": "invalid" if errors else "valid", "scanned": len(found), "classes": counts,
            "pending": [{"round": r, "mod": m, "key": k, "note": n} for r, m, k, n in pending], "errors": errors}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true", help="verify every do-first refusal is classified")
    parser.add_argument("--pending", action="store_true", help="list the keys still to convert or remove")
    parser.add_argument("--format", choices=["text", "json"], default="text")
    parser.add_argument("--root", type=Path, default=ROOT)
    args = parser.parse_args(argv)
    result = check(args.root)
    if args.format == "json":
        print(json.dumps(result, indent=2))
    else:
        if args.pending or not args.check:
            for p in result["pending"]:
                print(f"{p['round']}: {p['mod']} {p['key']} - {p['note']}")
        for e in result["errors"]:
            print("ERROR: " + e)
        print(f"{result['status'].upper()}: {result['scanned']} do-first texts scanned; " + ", ".join(f"{v} {k}" for k, v in sorted(result["classes"].items())))
    return 1 if args.check and result["errors"] else 0


if __name__ == "__main__":
    sys.exit(main())
