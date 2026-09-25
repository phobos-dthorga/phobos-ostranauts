"""Check relative inline Markdown file links; no network or game installation.

Checks tracked and untracked (nonignored) Markdown, excluding submodules. Fenced
examples are ignored. Remote URLs and fragments are not validated by this check.
"""
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

root = Path(__file__).resolve().parents[1]
names = subprocess.check_output(
    ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=root
).decode("utf-8").split("\0")
failures = []
checked = 0
for name in sorted(set(names)):
    path = root / name
    if not name.endswith(".md") or not path.is_file():
        continue
    content = path.read_text(encoding="utf-8-sig")
    content = re.sub(r"(?ms)^```.*?^```[^\n]*", "", content)
    for match in re.finditer(r"\[[^\]\n]*\]\((<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)", content):
        link = match.group(1).strip("<>")
        parsed = urlsplit(link)
        if parsed.scheme or parsed.netloc or not parsed.path:
            continue
        target = root / unquote(parsed.path.lstrip("/")) if parsed.path.startswith("/") else path.parent / unquote(parsed.path)
        checked += 1
        if not target.exists():
            failures.append(f"{name}: missing {link}")
for failure in failures:
    print(failure)
print(f"Checked {checked} relative file links; {len(failures)} missing targets. Fragments/external URLs not checked.")
sys.exit(bool(failures))
