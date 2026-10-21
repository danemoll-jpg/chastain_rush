"""Round 53 (A, Dan 2026-10-21: "a man, right before the ramp but to the left of it. He should be waving a handheld stop sign"): the two faces of
the road crew flagger's STOP / SLOW paddle (Neighborhood/Flagger.cs). A 24 in octagonal paddle: STOP, white on red with a white border; SLOW, black
on orange with a black border, as the real ones. Hand-made content: writes Assets/HandMade/RoadCrew/StopPaddle.png and SlowPaddle.png (512 x 512, the
octagon's flats touch the square's edges), which the map builder only reads.

Usage: python Tools/handmade/make_stop_paddle.py
"""
import math, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/RoadCrew")
N = 512

def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf", "impact.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def octagon(r):
    c = N / 2; R = r / math.cos(math.pi / 8)
    return [(c + R * math.cos(math.pi / 8 + k * math.pi / 4), c + R * math.sin(math.pi / 8 + k * math.pi / 4)) for k in range(8)]

def face(text, fill, ink, path):
    img = Image.new("RGB", (N, N), fill); d = ImageDraw.Draw(img)
    d.polygon(octagon(N / 2 - 14), outline=ink, width=22)       # the border band just inside the edge
    f = font(132); b = d.textbbox((0, 0), text, font=f)
    d.text(((N - (b[2] - b[0])) / 2 - b[0], (N - (b[3] - b[1])) / 2 - b[1]), text, font=f, fill=ink)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(OUT, exist_ok=True)
face("STOP", (196, 22, 28), (250, 250, 250), os.path.join(OUT, "StopPaddle.png"))
face("SLOW", (246, 128, 20), (16, 16, 16), os.path.join(OUT, "SlowPaddle.png"))
