"""Retain one completed PixelLab job's original PNG in the repository asset tree."""
import argparse
import hashlib
from pathlib import Path
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("job_id", type=uuid.UUID)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    path = (ROOT / args.destination).resolve()
    if not path.is_relative_to((ROOT / "assets").resolve()) or path.suffix.lower() != ".png":
        raise ValueError("Destination must be a PNG inside repository assets")
    url = "https://api.pixellab.ai/mcp/images/" + str(args.job_id) + "/download"
    with urllib.request.urlopen(url, timeout=60) as response:
        data = response.read()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError("Provider did not return a PNG")
    if path.exists() and path.read_bytes() != data:
        raise ValueError("Refusing to replace a different retained original")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    print(str(path.relative_to(ROOT)) + " " + hashlib.sha256(data).hexdigest())


if __name__ == "__main__":
    main()
