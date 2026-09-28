#!/usr/bin/env python3
"""Deterministic placeholder sprites for the S3 process-water silo and the T2 ice thaw unit.

These are stand-ins until the handoff in docs/development/bulk-silo-art-handoff.md is produced
with ChatGPT or PixelLab under the asset-generation policy. They are drawn procedurally at 4x
(the resolution memorandum's working convention), kept as masters under
assets/phobos-shipbreaker/placeholders/source, and exported by nearest-neighbour sampling to
the native footprint sizes (16 px per tile) plus flat normal maps and 256 px portraits.
No image library is needed; the PNG writer is pure Python.

Usage: python scripts/export-silo-placeholder-art.py [--check]
"""
from __future__ import annotations
import hashlib
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MASTERS = ROOT / 'assets/phobos-shipbreaker/placeholders/source'
RUNTIME = ROOT / 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
SCALE = 4
TILE = 16
PORTRAIT = 256


def png(width: int, height: int, rows) -> bytes:
    raw = b''.join(b'\x00' + bytes(row) for row in rows)

    def chunk(kind: bytes, body: bytes) -> bytes:
        return struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)) +
            chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


class Canvas:
    def __init__(self, size: int):
        self.size = size
        self.pixels = [[(0, 0, 0, 0)] * size for _ in range(size)]

    def rect(self, x0, y0, x1, y1, colour):
        for y in range(max(0, y0), min(self.size, y1)):
            for x in range(max(0, x0), min(self.size, x1)):
                self.pixels[y][x] = colour

    def rounded(self, x0, y0, x1, y1, radius, colour):
        for y in range(max(0, y0), min(self.size, y1)):
            for x in range(max(0, x0), min(self.size, x1)):
                dx = max(x0 + radius - x, 0, x - (x1 - 1 - radius))
                dy = max(y0 + radius - y, 0, y - (y1 - 1 - radius))
                if dx * dx + dy * dy <= radius * radius:
                    self.pixels[y][x] = colour

    def disc(self, cx, cy, radius, colour):
        for y in range(max(0, cy - radius), min(self.size, cy + radius + 1)):
            for x in range(max(0, cx - radius), min(self.size, cx + radius + 1)):
                if (x - cx) ** 2 + (y - cy) ** 2 <= radius * radius:
                    self.pixels[y][x] = colour

    def ring(self, cx, cy, radius, width, colour):
        for y in range(max(0, cy - radius), min(self.size, cy + radius + 1)):
            for x in range(max(0, cx - radius), min(self.size, cx + radius + 1)):
                d2 = (x - cx) ** 2 + (y - cy) ** 2
                if (radius - width) ** 2 <= d2 <= radius * radius:
                    self.pixels[y][x] = colour

    def rows(self):
        return [[c for px in row for c in px] for row in self.pixels]

    def sample(self, factor: int) -> 'Canvas':
        out = Canvas(self.size // factor)
        for y in range(out.size):
            for x in range(out.size):
                out.pixels[y][x] = self.pixels[y * factor + factor // 2][x * factor + factor // 2]
        return out

    def scale_up(self, factor: int, canvas: int) -> 'Canvas':
        out = Canvas(canvas)
        offset = (canvas - self.size * factor) // 2
        for y in range(self.size * factor):
            for x in range(self.size * factor):
                out.pixels[offset + y][offset + x] = self.pixels[y // factor][x // factor]
        return out

    def normal(self) -> 'Canvas':
        out = Canvas(self.size)
        for y in range(self.size):
            for x in range(self.size):
                out.pixels[y][x] = (128, 128, 255, 255) if self.pixels[y][x][3] else (0, 0, 0, 0)
        return out


RIM = (52, 56, 62, 255)
BODY = (96, 102, 110, 255)
BODY_LIGHT = (122, 130, 140, 255)
HATCH = (140, 150, 160, 255)
TEAL = (48, 122, 128, 255)
TEAL_LIGHT = (92, 176, 180, 255)
BOLT = (36, 38, 42, 255)
ICE = (176, 206, 224, 255)
ICE_DARK = (120, 156, 184, 255)
GAUGE_BG = (30, 32, 36, 255)


def silo(k: int) -> Canvas:
    """Overhead view of a sealed cylindrical silo in a square 3 x 3 frame: rim, dome, top hatch, level gauge."""
    n = 3 * TILE * k
    c = Canvas(n)
    inset = 1 * k
    c.rounded(inset, inset, n - inset, n - inset, 3 * k, RIM)
    c.rounded(inset + k, inset + k, n - inset - k, n - inset - k, 3 * k, BODY)
    centre = n // 2
    c.disc(centre, centre, n // 2 - 4 * k, RIM)
    c.disc(centre, centre, n // 2 - 5 * k, BODY_LIGHT)
    c.ring(centre, centre, n // 2 - 8 * k, 2 * k, TEAL)
    c.disc(centre, centre, 7 * k, RIM)
    c.disc(centre, centre, 6 * k, HATCH)
    c.rect(centre - k, centre - 4 * k, centre + k, centre + 4 * k, RIM)
    for bx, by in ((inset + 3 * k, inset + 3 * k), (n - inset - 4 * k, inset + 3 * k), (inset + 3 * k, n - inset - 4 * k), (n - inset - 4 * k, n - inset - 4 * k)):
        c.rect(bx, by, bx + k, by + k, BOLT)
    # A level gauge strip along the right edge, reading empty.
    c.rect(n - inset - 4 * k, inset + 8 * k, n - inset - 2 * k, n - inset - 8 * k, GAUGE_BG)
    c.rect(n - inset - 4 * k, n - inset - 10 * k, n - inset - 2 * k, n - inset - 8 * k, TEAL_LIGHT)
    return c


def thaw(k: int) -> Canvas:
    """Overhead view of a boxy 2 x 2 thaw unit: rim, lid with an ice-blue inspection window, vent slots, outlet stub."""
    n = 2 * TILE * k
    c = Canvas(n)
    inset = 1 * k
    c.rounded(inset, inset, n - inset, n - inset, 2 * k, RIM)
    c.rounded(inset + k, inset + k, n - inset - k, n - inset - k, 2 * k, BODY)
    # Inspection window over the feed bin, top-left.
    c.rect(inset + 3 * k, inset + 3 * k, n // 2 + 2 * k, n // 2 + 2 * k, RIM)
    c.rect(inset + 4 * k, inset + 4 * k, n // 2 + k, n // 2 + k, ICE_DARK)
    c.rect(inset + 5 * k, inset + 5 * k, n // 2 - k, n // 2 - k, ICE)
    # Vent slots on the right half.
    for i in range(4):
        y = inset + 4 * k + i * 3 * k
        c.rect(n // 2 + 4 * k, y, n - inset - 3 * k, y + k, RIM)
    # Gangue tray hatch along the bottom.
    c.rect(inset + 3 * k, n - inset - 7 * k, n - inset - 3 * k, n - inset - 3 * k, RIM)
    c.rect(inset + 4 * k, n - inset - 6 * k, n - inset - 4 * k, n - inset - 4 * k, BODY_LIGHT)
    # Water outlet stub on the right edge, teal.
    c.rect(n - inset - 2 * k, n // 2 - 2 * k, n - inset, n // 2 + 2 * k, TEAL)
    for bx, by in ((inset + 2 * k, inset + 2 * k), (n - inset - 3 * k, inset + 2 * k), (inset + 2 * k, n - inset - 3 * k), (n - inset - 3 * k, n - inset - 3 * k)):
        c.rect(bx, by, bx + k, by + k, BOLT)
    return c


def outputs():
    for name, draw, tiles in (('PhobosProcessSilo', silo, 3), ('PhobosIceThaw', thaw, 2)):
        master = draw(SCALE)
        native = master.sample(SCALE)
        portrait_scale = PORTRAIT // native.size
        yield MASTERS / f'{name}-placeholder-4x.png', png(master.size, master.size, master.rows())
        yield RUNTIME / f'{name}.png', png(native.size, native.size, native.rows())
        yield RUNTIME / f'{name}Normal.png', png(native.size, native.size, native.normal().rows())
        portrait = native.scale_up(portrait_scale, PORTRAIT)
        yield RUNTIME / f'{name}Portrait.png', png(PORTRAIT, PORTRAIT, portrait.rows())


def main(argv):
    check = '--check' in argv
    stale = []
    for path, data in outputs():
        if check:
            if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != hashlib.sha256(data).hexdigest():
                stale.append(path)
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        print('wrote', path.relative_to(ROOT).as_posix(), hashlib.sha256(data).hexdigest()[:16])
    if stale:
        print('STALE:', ', '.join(p.relative_to(ROOT).as_posix() for p in stale))
        return 1
    if check:
        print('placeholder silo/thaw artwork is current')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
