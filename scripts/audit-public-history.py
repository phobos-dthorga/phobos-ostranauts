"""Read-only publication screen of all locally reachable Git file objects.

Prints only object IDs, categories and paths, never matching contents. This is
a bounded pattern screen, not a guarantee of no secrets or a licensing audit.
Run separately inside each submodule. Fetch remote refs before publication.
"""
import collections
from pathlib import PurePosixPath
import re
import subprocess
import sys

patterns = {
    "credential-pattern": re.compile(rb"(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|sk-proj-[A-Za-z0-9_-]{20,}|AKIA[A-Z0-9]{16}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----)"),
    "personal-machine-path": re.compile(rb"[C-Z]:[\\/](?:Users|SynologyDrive)[\\/][^\s\x00<>\"\r\n]+"),
}
prohibited_suffixes = {".dll", ".exe", ".zip", ".sav", ".dmp", ".pfx", ".p12", ".pem", ".key"}
rows = subprocess.check_output(["git", "rev-list", "--objects", "--all"]).decode("utf-8").splitlines()
counts = collections.Counter()
findings = []
with subprocess.Popen(["git", "cat-file", "--batch"], stdin=subprocess.PIPE, stdout=subprocess.PIPE) as process:
    for row in rows:
        oid, _, path = row.partition(" ")
        process.stdin.write((oid + "\n").encode("ascii"))
        process.stdin.flush()
        header = process.stdout.readline().decode("ascii").split()
        if len(header) != 3:
            raise RuntimeError("Unexpected Git object response")
        data = process.stdout.read(int(header[2]))
        process.stdout.read(1)
        if header[1] != "blob":
            continue
        counts["blobs"] += 1
        if PurePosixPath(path).suffix.lower() in prohibited_suffixes:
            findings.append(("prohibited-file-type", path, oid[:12]))
        if b"\0" in data[:8192]:
            continue
        for category, pattern in patterns.items():
            if pattern.search(data):
                findings.append((category, path, oid[:12]))
    process.stdin.close()
    if process.wait() != 0:
        raise RuntimeError("Git object scan failed")
for finding in sorted(set(findings)):
    print(" | ".join(finding))
print(f"Scanned {counts['blobs']} reachable file objects; {len(findings)} pattern/file-type findings.")
sys.exit(bool(findings))
