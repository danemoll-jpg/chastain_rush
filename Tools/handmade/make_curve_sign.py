"""Round 54 (A6, Dan BUG-005 on Embry Lane before its west bend: "lets put some slow down signs here. Warning of a sharp turn"): the faces of the
yellow warning signs before that bend, as the real ones: a turn sign (a black arrow bent 90 degrees on a yellow diamond with a black border, left and
right) and the advisory speed plate under it (15 MPH, black on yellow). Hand-made content: writes Assets/HandMade/RoadSigns/TurnLeft.png,
TurnRight.png (512 x 512, the diamond's corners touch the middles of the square's edges; the board's mesh is the diamond) and Advisory15.png
(256 x 256, the plate), which the map builder only reads.

Usage: python Tools/handmade/make_curve_sign.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/RoadSigns")
N = 512
YELLOW, INK = (250, 196, 18), (14, 14, 14)

def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def turn(left, path):
    img = Image.new("RGB", (N, N), YELLOW); d = ImageDraw.Draw(img)
    c = N / 2; r = N / 2 - 6; inset = 26
    d.polygon([(c, c - r + inset), (c + r - inset, c), (c, c + r - inset), (c - r + inset, c)], outline=INK, width=16)   # the border band
    # the arrow: up from the bottom, then a square turn, the head pointing the way the road goes
    s = -1 if left else 1; w = 26
    x0, y0, y1 = c - s * 34, c + 96, c - 14           # the stem's middle, its foot, its top (where it turns)
    x1 = c + s * 46                                    # where the head starts
    d.rectangle([min(x0 - w, x0 + w), y1 - w, max(x0 - w, x0 + w), y0], fill=INK)
    d.rectangle([min(x0 - w, x1), y1 - w, max(x0 + w, x1), y1 + w], fill=INK)
    d.polygon([(x1, y1 - 58), (x1 + s * 64, y1), (x1, y1 + 58)], fill=INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

def advisory(mph, path):
    M = 256; img = Image.new("RGB", (M, M), YELLOW); d = ImageDraw.Draw(img)
    d.rounded_rectangle([10, 10, M - 10, M - 10], radius=18, outline=INK, width=10)
    for text, size, y in ((str(mph), 132, 92), ("MPH", 60, 196)):
        f = font(size); b = d.textbbox((0, 0), text, font=f)
        d.text(((M - (b[2] - b[0])) / 2 - b[0], y - (b[3] - b[1]) / 2 - b[1]), text, font=f, fill=INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(OUT, exist_ok=True)
turn(True, os.path.join(OUT, "TurnLeft.png"))
turn(False, os.path.join(OUT, "TurnRight.png"))
advisory(15, os.path.join(OUT, "Advisory15.png"))
