"""Validate Workshop drafts and export Steam BBCode from per-mod changelogs."""
from __future__ import annotations

import argparse
from datetime import date
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RELEASE = re.compile(r"^## \[(\d+\.\d+\.\d+)\] - (\d{4}-\d{2}-\d{2}) - (Draft|Released)$")


class NotesError(ValueError):
    pass


def plain_inline(text):
    """Small deliberate Markdown subset; fail rather than silently lose formatting."""
    if "![" in text or any(ord(c) < 32 for c in text):
        raise NotesError("Images and control characters are unsupported in release text")
    tokens = []
    def keep(value):
        tokens.append(value)
        return f"\x01{len(tokens) - 1}\x02"
    def link(match):
        label, url = match.groups()
        if not re.fullmatch(r"https?://[^\s\[\]<>]+", url) or any(c in label for c in "[]<>"):
            raise NotesError("Changelog links require an absolute HTTP(S) URL and plain label")
        return keep(f"[url={url}]{label}[/url]")
    text = re.sub(r"\[([^\]\n]+)\]\(([^)\n]+)\)", link, text)
    def bold(match):
        if any(c in match[1] for c in "[]<>`\x01"):
            raise NotesError("Use plain text inside bold emphasis")
        return keep("[b]" + match[1] + "[/b]")
    text = re.sub(r"\*\*([^*\n]+)\*\*", bold, text)
    if any(c in text for c in "[]<>`*|#") or re.match(r"\d+\.\s", text):
        raise NotesError(f"Unsupported changelog formatting: {text}")
    for index, token in enumerate(tokens):
        text = text.replace(f"\x01{index}\x02", token)
    return text


def render_body(body):
    output, in_list = [], False
    for line in body.splitlines():
        if line.startswith((" ", "\t")):
            raise NotesError("Indented/nested changelog content is unsupported")
        bullet = line.startswith("- ")
        if in_list and not bullet:
            output.append("[/list]")
            in_list = False
        if bullet:
            if not in_list:
                output.append("[list]")
                in_list = True
            output.append("[*]" + plain_inline(line[2:]))
        elif line.startswith("### "):
            output.append("[h2]" + plain_inline(line[4:]) + "[/h2]")
        elif line:
            output.append(plain_inline(line))
        else:
            output.append("")
    if in_list:
        output.append("[/list]")
    return "\n".join(output).strip()


def parse_changelog(text):
    releases, current, body, unreleased = {}, None, [], False
    def finish():
        if current:
            if not "\n".join(body).strip():
                raise NotesError(f"Empty release entry: {current[0]}")
            releases[current[0]] = {"date": current[1], "status": current[2], "body": render_body("\n".join(body))}
    for line in text.splitlines():
        if line.startswith("## "):
            finish()
            body = []
            if line == "## [Unreleased]":
                if unreleased or releases or current:
                    raise NotesError("Unreleased must appear once, before version entries")
                unreleased, current = True, None
            else:
                match = RELEASE.fullmatch(line)
                if not match:
                    raise NotesError(f"Invalid release heading: {line}")
                current = match.groups()
                date.fromisoformat(current[1])
                if current[0] in releases:
                    raise NotesError(f"Duplicate release version: {current[0]}")
        elif current:
            body.append(line)
    finish()
    if not unreleased:
        raise NotesError("Missing ## [Unreleased] section")
    return releases


def validate_bbcode(text):
    stack = []
    for match in re.finditer(r"\[([^\]]*)\]", text):
        tag = match[1]
        if tag == "*":
            if not stack or stack[-1] != "list":
                raise NotesError("List item outside a list")
            continue
        closing = tag.startswith("/")
        name = tag.lstrip("/").split("=", 1)[0]
        if name not in {"h1", "h2", "h3", "b", "i", "u", "list", "url"}:
            raise NotesError(f"Unsupported Steam tag: {tag}")
        if name == "url" and not closing:
            if not re.fullmatch(r"url=https?://[^\s\[\]<>]+", tag):
                raise NotesError("Steam links must use absolute HTTP(S) URLs")
        elif "=" in tag:
            raise NotesError(f"Unexpected tag attribute: {tag}")
        if closing:
            if not stack or stack.pop() != name:
                raise NotesError(f"Unbalanced Steam tag: {tag}")
        else:
            stack.append(name)
    if stack:
        raise NotesError("Unclosed Steam tags")
    if "[" in re.sub(r"\[[^\]]*\]", "", text) or "]" in re.sub(r"\[[^\]]*\]", "", text):
        raise NotesError("Unmatched Steam bracket")
    if re.search(r"(?m)^#{1,6} |\*\*|```", text):
        raise NotesError("Markdown found in Steam page; use BBCode")


def safe(root, path):
    if not path.resolve().is_relative_to(root.resolve()):
        raise NotesError("Path escapes repository")
    for part in (path, *path.parents):
        if part == root.parent:
            break
        reparse = part.exists() and getattr(part.lstat(), "st_file_attributes", 0) & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0)
        if part.is_symlink() or reparse:
            raise NotesError(f"Filesystem link refused: {path}")
    return path


def plan(root, selected, version):
    all_mods = sorted(p.parent.name for p in (root / "mods").glob("*/mod_info.json"))
    names = selected or all_mods
    names = [n if n.startswith("Phobos") else "Phobos" + n for n in names]
    if not names or len(names) != len(set(names)) or set(names) - set(all_mods):
        raise NotesError("Unknown or duplicate mod selection")
    if version and len(names) != 1:
        raise NotesError("--version requires exactly one --mod")
    outputs, inputs = {}, {}
    for name in names:
        metadata = safe(root, root / "mods" / name / "mod_info.json")
        changelog = safe(root, metadata.parent / "CHANGELOG.md")
        page = safe(root, root / "workshop" / name / "page.bbcode")
        for path in (metadata, changelog, page):
            inputs[path] = path.read_bytes()
        info = json.loads(inputs[metadata].decode("utf-8-sig"))[0]
        title = info["strName"]
        if any(c in title for c in "[]\n\r"):
            raise NotesError("Unsafe mod title")
        releases = parse_changelog(inputs[changelog].decode("utf-8-sig"))
        current = info["strModVersion"]
        if current not in releases:
            raise NotesError(f"{name}: current version {current} needs a dated Draft/Released changelog entry")
        page_text = inputs[page].decode("utf-8-sig")
        validate_bbcode(page_text)
        expected = f"[b]Version:[/b] {current}"
        if page_text.splitlines().count(expected) != 1 or f"[h1]{title}[/h1]" not in page_text:
            raise NotesError(f"{name}: stale page title/version; expected {expected}")
        if not re.search(r"(?m)^\[b\]Publication status:\[/b\] .+", page_text):
            raise NotesError(f"{name}: page needs explicit publication status")
        if version and version not in releases:
            raise NotesError(f"{name}: release version not found: {version}")
        versions = [version] if version else list(releases)
        for release_version in versions:
            entry = releases[release_version]
            status = "Draft - not published" if entry["status"] == "Draft" else "Released"
            text = f"[h1]{title} {release_version}[/h1]\n[b]{status} | {entry['date']}[/b]\n\n{entry['body']}\n"
            validate_bbcode(text)
            path = safe(root, root / "workshop" / name / "releases" / f"{release_version}.bbcode")
            before = path.read_bytes() if path.exists() else None
            outputs[path] = (before, text.encode("utf-8"))
        if not version:
            directory = root / "workshop" / name / "releases"
            extras = {p.name for p in directory.glob("*.bbcode")} - {v + ".bbcode" for v in releases}
            if extras:
                raise NotesError(f"{name}: orphan release files: {', '.join(sorted(extras))}")
    return inputs, outputs


def write_file(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, temp = tempfile.mkstemp(prefix=".release-", dir=path.parent)
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
        os.replace(temp, path)
    finally:
        if os.path.exists(temp):
            os.unlink(temp)


def require_changed_records(paths):
    changed = set(paths)
    mods = set()
    for name in changed:
        parts = name.split("/")
        if len(parts) >= 3 and parts[0] in {"src", "mods", "translations"} and parts[1].startswith("Phobos"):
            if name != f"mods/{parts[1]}/CHANGELOG.md":
                mods.add(parts[1])
    for name in sorted(mods):
        needed = {f"mods/{name}/CHANGELOG.md", f"workshop/{name}/page.bbcode"}
        if needed - changed:
            raise NotesError(f"{name}: changed mod content requires changelog and page updates in the same change; missing: {', '.join(sorted(needed - changed))}")


def check_change_scope(root, base):
    # Resolve as a commit, never as a command option; include local new records.
    result = subprocess.run(["git", "rev-parse", "--verify", "--end-of-options", base + "^{commit}"], cwd=root, capture_output=True, text=True)
    if result.returncode:
        raise NotesError("--base must resolve to an available Git commit")
    revision = result.stdout.strip()
    changed = subprocess.check_output(["git", "diff", "--name-only", "-z", revision, "--"], cwd=root).decode().split("\0")
    untracked = subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard", "-z"], cwd=root).decode().split("\0")
    require_changed_records(changed + untracked)


def execute(root, args):
    if getattr(args, "base", None):
        check_change_scope(root, args.base)
    inputs, outputs = plan(root, args.mod, args.version)
    changes = {path: pair for path, pair in outputs.items() if pair[0] != pair[1]}
    if args.write:
        # Preflight every selected mod and compare all inputs before any writes.
        for path, data in inputs.items():
            if safe(root, path).read_bytes() != data:
                raise NotesError(f"Concurrent source change: {path.relative_to(root)}")
        for path, (before, _) in outputs.items():
            if (path.read_bytes() if path.exists() else None) != before:
                raise NotesError(f"Concurrent output change: {path.relative_to(root)}")
        for path, (_, data) in changes.items():
            safe(root, path)
            write_file(path, data)
        for path, data in inputs.items():
            if safe(root, path).read_bytes() != data:
                raise NotesError(f"Source changed during export: {path.relative_to(root)}; rerun before publication")
        _, verified = plan(root, args.mod, args.version)
        if any(before != after for before, after in verified.values()):
            raise NotesError("Read-back failed; rerun --check before publication")
    stale = bool(changes) and not args.write
    return {"schemaVersion": 1, "status": "stale" if args.check and stale else "written" if args.write and changes else "preview" if stale else "valid",
            "checkedReleases": len(outputs), "changedFiles": [{"path": str(path.relative_to(root)).replace("\\", "/"),
            "sha256": hashlib.sha256(data).hexdigest(), "content": data.decode("utf-8") if not args.check and not args.write else None}
            for path, (_, data) in changes.items()]}, 1 if args.check and stale else 0


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mod", action="append", help="Repeatable mod name; default all mods")
    parser.add_argument("--version", help="Export one version; default all dated entries")
    parser.add_argument("--base", help="Require changed mod content to include changelog and page edits since this Git commit")
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--check", action="store_true", help="Fail on missing/stale generated files; no writes")
    modes.add_argument("--write", action="store_true", help="Generate or refresh release files")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)
    try:
        report, code = execute(ROOT, args)
    except (ValueError, OSError, KeyError, IndexError, TypeError, subprocess.SubprocessError) as error:
        report, code = {"schemaVersion": 1, "status": "error", "error": str(error)}, 1
    if args.format == "json":
        print(json.dumps(report, indent=2, ensure_ascii=False))
    else:
        print(report["status"].upper() + ": " + report.get("error", f"{report.get('checkedReleases', 0)} release entries checked"))
        for item in report.get("changedFiles", []):
            print(f"  {item['path']}  SHA-256 {item['sha256']}")
            if item["content"]:
                print(item["content"])
        if report["status"] == "preview":
            print("No files written. Use --write to save these generated drafts.")
    return code


if __name__ == "__main__":
    sys.exit(main())
