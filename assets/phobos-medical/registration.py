"""Shared mechanical registration for original Halewright equipment masters."""
import io
from PIL import Image


def cutout(image, tolerance=28):
    """Remove only near-white pixels connected to the outside canvas edges."""
    rgb = image.convert("RGB")
    w, h = rgb.size
    px = rgb.load()
    outside = bytearray(w * h)
    stack = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    while stack:
        x, y = stack.pop()
        i = y * w + x
        if outside[i] or min(px[x, y]) < 255 - tolerance:
            continue
        outside[i] = 1
        stack.extend(p for p in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)) if 0 <= p[0] < w and 0 <= p[1] < h)
    alpha = Image.frombytes("L", (w, h), bytes(0 if v else 255 for v in outside))
    if not alpha.getbbox():
        raise ValueError("No equipment silhouette remains after background removal")
    rgba = rgb.convert("RGBA")
    rgba.putalpha(alpha)
    return rgba.crop(alpha.getbbox())


def registered(image, canvas_size, body_size, origin, colours, tolerance=28):
    body = cutout(image, tolerance).resize(body_size, Image.Resampling.BOX)
    alpha = body.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    colour = body.convert("RGB").quantize(colors=colours, method=Image.Quantize.MEDIANCUT,
                                          dither=Image.Dither.NONE).convert("RGBA")
    colour.putalpha(alpha)
    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    canvas.alpha_composite(colour, origin)
    return canvas


def png(image):
    stream = io.BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()
