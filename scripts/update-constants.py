"""Update explicitly registered source constants. Standard library, Python 3.10+."""
from __future__ import annotations

import argparse
from decimal import Decimal, InvalidOperation
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
CATALOG = "config/maintained-constants.json"


class UpdateError(ValueError):
    pass


def digest(data):
    return hashlib.sha256(data).hexdigest()


def validate(entry, value):
    if not isinstance(value, str):
        raise UpdateError("Catalogue values must be strings")
    kind = entry["type"]
    if kind == "version":
        parts = entry.get("parts", 3)
        if parts not in (3, 4):
            raise UpdateError("Version types require 3 or 4 components")
        if not re.fullmatch(r"(?:0|[1-9][0-9]*)" + r"\.(?:0|[1-9][0-9]*)" * (parts - 1), value):
            raise UpdateError(f"Expected {parts}-part numeric version: {value!r}")
        if any(int(part) > 65534 for part in value.split(".")):
            raise UpdateError("Version components must be at most 65534")
        return value
    if kind in ("number", "integer"):
        if not re.fullmatch(r"-?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)", value):
            raise UpdateError(f"Expected finite decimal literal: {value!r}")
        number = Decimal(value)
        if kind == "integer" and number != number.to_integral_value():
            raise UpdateError("Expected integer")
        if not Decimal(str(entry["min"])) <= number <= Decimal(str(entry["max"])):
            raise UpdateError(f"Value outside [{entry['min']}, {entry['max']}]")
        return str(int(number)) if kind == "integer" else format(number, "f")
    if kind == "boolean" and value in ("true", "false"):
        return value
    raise UpdateError(f"Unsupported type or value: {kind}, {value!r}")


def equal(entry, left, right):
    left, right = validate(entry, left), validate(entry, right)
    return Decimal(left) == Decimal(right) if entry["type"] in ("number", "integer") else left == right


def safe_path(root, name):
    relative = Path(name)
    if relative.is_absolute() or ".." in relative.parts or not relative.parts:
        raise UpdateError(f"Unsafe target path: {name}")
    workshop_page = len(relative.parts) == 3 and relative.parts[0] == "workshop" and relative.parts[2] == "page.bbcode"
    if relative.parts[0] not in ("src", "mods", "docs", "config") and name != "README.md" and not workshop_page:
        raise UpdateError(f"Target outside maintained source/documentation: {name}")
    path = root / relative
    for component in (path, *path.parents):
        if component == root.parent:
            break
        reparse = component.exists() and getattr(component.lstat(), "st_file_attributes", 0) & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0)
        if component.is_symlink() or reparse:
            raise UpdateError(f"Linked target refused: {name}")
    if not path.resolve().is_relative_to(root.resolve()) or not path.is_file():
        raise UpdateError(f"Missing or external target: {name}")
    return path


def assignments(items):
    result = {}
    for item in items:
        key, separator, value = item.partition("=")
        if not separator or not key or key in result:
            raise UpdateError(f"Expected unique key=value assignment: {item!r}")
        result[key] = value
    return result


def load_catalog(root):
    path = safe_path(root, CATALOG)
    original = path.read_bytes()
    catalog = json.loads(original.decode("utf-8-sig"))
    if catalog.get("schemaVersion") != 1 or not isinstance(catalog.get("constants"), dict):
        raise UpdateError("Unsupported catalogue schema")
    for key, entry in catalog["constants"].items():
        validate(entry, entry["value"])
        if not entry.get("targets"):
            raise UpdateError(f"No targets for {key}")
    return catalog, original


def plan(root, updates, expected):
    catalog, catalog_bytes = load_catalog(root)
    entries = catalog["constants"]
    unknown = (updates.keys() | expected.keys()) - entries.keys()
    if unknown:
        raise UpdateError("Unknown keys: " + ", ".join(sorted(unknown)))
    if expected.keys() - updates.keys():
        raise UpdateError("--expect keys must also be supplied with --set")
    for key, value in expected.items():
        if not equal(entries[key], value, entries[key]["value"]):
            raise UpdateError(f"Expected-value conflict for {key}")
    replacements = {key: validate(entries[key], value) for key, value in updates.items()}
    original, edits, changes = {}, {}, []
    # Check ALL registered copies before writing ANY file, even for a one-key edit.
    for key, entry in entries.items():
        for target in entry["targets"]:
            if type(target.get("count", 1)) is not int or target.get("count", 1) < 1:
                raise UpdateError(f"Expected positive match count for {key}")
            name = target["path"]
            if name == CATALOG:
                raise UpdateError("Catalogue cannot target itself")
            path = safe_path(root, name)
            if name not in original:
                original[name] = path.read_bytes()
            text = original[name].decode("utf-8-sig")
            pattern = re.compile(target["pattern"], re.MULTILINE)
            matches = list(pattern.finditer(text))
            if len(matches) != target.get("count", 1) or "value" not in pattern.groupindex:
                raise UpdateError(f"Target shape changed: {key} in {name}; found {len(matches)} matches")
            for match in matches:
                current = match.group("value")
                if not equal(entry, current, entry["value"]):
                    raise UpdateError(f"Drift: {key} in {name}; catalogue={entry['value']}, target={current}")
                start, end = match.span("value")
                if start == end:
                    raise UpdateError(f"Empty value capture: {key} in {name}")
                # Check overlaps even when no update is requested.
                intervals = edits.setdefault(name, [])
                if any(start < other[1] and end > other[0] for other in intervals):
                    raise UpdateError(f"Overlapping targets in {name}")
                new = replacements.get(key, entry["value"])
                changing = key in replacements and not equal(entry, current, new)
                rendered = new
                # C# floats whose original value was an integer still need an f
                # suffix when a later update introduces a fractional value.
                if target.get("literal") == "csharp-float" and text[end:end + 1] not in ("f", "F"):
                    rendered += "f"
                intervals.append((start, end, rendered if changing else current))
                if changing:
                    changes.append({"key": key, "path": name, "line": text.count("\n", 0, start) + 1,
                                    "before": current, "after": new, "replacement": rendered})
    proposed = {}
    for name, intervals in edits.items():
        text = original[name].decode("utf-8-sig")
        for start, end, value in sorted(intervals, reverse=True):
            text = text[:start] + value + text[end:]
        data = (b"\xef\xbb\xbf" if original[name].startswith(b"\xef\xbb\xbf") else b"") + text.encode("utf-8")
        if data != original[name]:
            # Syntax-check structured files after all changes are composed.
            if name.endswith(".json"):
                json.loads(text)
            if name.endswith(".csproj"):
                import xml.etree.ElementTree as ET
                ET.fromstring(text)
            proposed[name] = data
    changed_keys = [key for key, value in replacements.items() if not equal(entries[key], value, entries[key]["value"])]
    if changed_keys:
        for key in changed_keys:
            entries[key]["value"] = replacements[key]
        original[CATALOG] = catalog_bytes
        proposed[CATALOG] = (json.dumps(catalog, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    report = {"schemaVersion": 1, "status": "preview" if proposed else "unchanged", "verified": True,
              "verification": "preflight",
              "checkedConstants": len(entries), "changes": changes,
              "files": [{"path": name, "beforeSha256": digest(original[name]), "afterSha256": digest(data)}
                        for name, data in sorted(proposed.items())],
              "review": {key: entries[key].get("review", []) for key in changed_keys}}
    return original, proposed, report


def replace_file(path, data):
    mode = path.stat().st_mode
    fd, temporary = tempfile.mkstemp(prefix=".constants-", dir=path.parent)
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def apply(root, original, proposed):
    written = []
    try:
        for name, before in original.items():
            if safe_path(root, name).read_bytes() != before:
                raise UpdateError(f"Concurrent edit detected: {name}")
        for name, after in proposed.items():
            path = safe_path(root, name)
            if path.read_bytes() != original[name]:
                raise UpdateError(f"Concurrent edit detected: {name}")
            replace_file(path, after)
            written.append(name)
        plan(root, {}, {})  # Re-read all registered targets and catalogue.
        for name, after in proposed.items():
            if safe_path(root, name).read_bytes() != after:
                raise UpdateError(f"Read-back failed: {name}")
    except Exception as error:
        failures = []
        for name in reversed(written):
            try:
                path = safe_path(root, name)
                if path.read_bytes() != proposed[name]:
                    raise UpdateError("File changed again; retained concurrent edit")
                replace_file(path, original[name])
            except Exception:
                failures.append(name)
        if failures:
            raise UpdateError(f"Update failed ({error}); manual recovery required for: {', '.join(failures)}") from error
        raise UpdateError(f"Update failed; written files restored: {error}") from error


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--list", action="store_true", help="List registered keys, values, bounds and targets")
    mode.add_argument("--check", action="store_true", help="Verify all registered copies without writing")
    mode.add_argument("--apply", action="store_true", help="Apply changes; default is preview only")
    parser.add_argument("--set", action="append", default=[], metavar="KEY=VALUE")
    parser.add_argument("--expect", action="append", default=[], metavar="KEY=OLD_VALUE")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)
    try:
        updates, expected = assignments(args.set), assignments(args.expect)
        if (args.list or args.check) and (updates or expected):
            raise UpdateError("--list/--check cannot be combined with assignments")
        if not (args.list or args.check or updates):
            raise UpdateError("Choose --list, --check or one or more --set assignments")
        original, proposed, report = plan(ROOT, updates, expected)
        if args.list:
            report["status"] = "listed"
            report["constants"] = load_catalog(ROOT)[0]["constants"]
        elif args.check:
            report["status"] = "valid"
        elif args.apply and proposed:
            apply(ROOT, original, proposed)
            report["status"] = "applied"
            report["verification"] = "read-back"
        code = 0
    except (UpdateError, OSError, ValueError, KeyError, TypeError, InvalidOperation, re.error) as error:
        report = {"schemaVersion": 1, "status": "error", "verified": False, "error": str(error)}
        code = 1
    if args.format == "json":
        print(json.dumps(report, indent=2, ensure_ascii=False))
    else:
        print(f"{report['status'].upper()}: " + (report.get("error") or f"{report['checkedConstants']} constants verified; {len(report['files'])} files to change/changed"))
        for key, entry in report.get("constants", {}).items():
            bounds = f", range {entry['min']}..{entry['max']}" if "min" in entry else ""
            print(f"  {key} = {entry['value']} ({entry['type']}{bounds}) — {entry['description']}")
        for change in report.get("changes", []):
            print(f"  {change['key']}: {change['before']} -> {change['after']}  [{change['path']}:{change['line']}]")
        for key, reviews in report.get("review", {}).items():
            for item in reviews:
                print(f"  REVIEW {key}: {item}")
        if report["status"] == "preview":
            print("No files written. Repeat with --apply to update, then run affected builds/checks.")
    return code


if __name__ == "__main__":
    sys.exit(main())
