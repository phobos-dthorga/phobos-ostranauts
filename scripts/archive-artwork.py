"""Append superseded asset PNGs to the established archive branch and record their hashes.

Uses an isolated Git index; the checkout's index is never changed. --remove only
deletes exact verified files within assets after confirming no selected manifest
entry still needs them. Repeating a completed removal fails without further edits.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
BRANCH = "refs/heads/codex/rejected-artwork"


def git(*args, data=None, env=None):
    return subprocess.check_output(["git", *args], cwd=ROOT, input=data, env=env)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+")
    parser.add_argument("--reason", required=True)
    parser.add_argument("--date", required=True, help="Owner/client date, YYYY-MM-DD")
    parser.add_argument("--remove", action="store_true")
    args = parser.parse_args()
    files = [(ROOT / name).resolve() for name in args.paths]
    if len(set(files)) != len(files):
        raise ValueError("Duplicate archive paths")
    for path in files:
        if not path.is_relative_to(ASSETS.resolve()) or path.suffix.lower() != ".png" or not path.is_file():
            raise ValueError("Archive requires an existing PNG within assets: " + str(path))
    if args.remove:
        manifest_root = ASSETS / "artwork-completion"
        manifest = json.loads((manifest_root / "manifest.json").read_text(encoding="utf-8"))
        used = {(manifest_root / row[field]).resolve()
                for row in manifest["assets"] if row["status"] == "selected"
                for field in ("source", "productionSource", "registrationReference") if field in row}
        if used.intersection(files):
            raise ValueError("Selected artwork still needs an archive removal target")
    parent = git("rev-parse", BRANCH).decode().strip()
    local = ROOT / ".local"
    local.mkdir(exist_ok=True)
    index = local / ("artwork-archive-" + uuid.uuid4().hex + ".index")
    env = os.environ.copy()
    env["GIT_INDEX_FILE"] = str(index)
    try:
        git("read-tree", parent, env=env)
        for path in files:
            git("add", "--", path.relative_to(ROOT).as_posix(), env=env)
        tree = git("write-tree", env=env).decode().strip()
        commit = git("commit-tree", tree, "-p", parent,
                     data=("Archive artwork: " + args.reason + "\n").encode()).decode().strip()
        retained = []
        for path in files:
            name, data = path.relative_to(ROOT).as_posix(), path.read_bytes()
            if git("show", commit + ":" + name) != data:
                raise ValueError("Archive did not retain exact original: " + name)
            retained.append({"path": name, "sha256": hashlib.sha256(data).hexdigest(),
                             "reason": args.reason, "archiveCommit": commit,
                             "archiveStatus": "Local archive commit; not pushed", "archiveURL": None})
        # Compare-and-swap avoids overwriting another task's concurrent archive append.
        git("update-ref", BRANCH, commit, parent)
        inventory_path = ASSETS / "rejected-artwork-archive.json"
        inventory = json.loads(inventory_path.read_text(encoding="utf-8"))
        inventory["assets"].extend(retained)
        inventory.setdefault("additionalArchives", []).append({"commit": commit, "date": args.date, "note": args.reason})
        inventory_path.write_text(json.dumps(inventory, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        if args.remove:
            for path, row in zip(files, retained):
                # Check the final resolved path and contents again immediately before removing.
                if not path.resolve().is_relative_to(ASSETS.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != row["sha256"]:
                    raise ValueError("Removal target changed after archive verification: " + str(path))
                path.unlink()
        print(json.dumps({"commit": commit, "verifiedFiles": len(retained), "removed": args.remove, "assets": retained}))
    finally:
        if index.exists() and index.resolve().parent == local.resolve():
            index.unlink()


if __name__ == "__main__":
    main()
