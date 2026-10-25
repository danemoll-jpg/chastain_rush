"""Round 66 (3, Dan: "roads that leave the map end in 'Road Closed' barricades / a detour (road-crew style)"): the faces of the closure signs, as the
real ones: ROAD CLOSED (black on white with a black border, 8 x 5, the shape of the R11-2 board) and DETOUR with an arrow (black on orange, 2 x 1;
the arrow pointing right). Hand-made content: writes Assets/HandMade/RoadSigns/RoadClosed.png
(1024 x 640) and Detour.png (1024 x 512), which the map builder only reads (MapBuilder.Edges.cs).

Usage: python Tools/handmade/make_closure_signs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/RoadSigns")
WHITE, INK, ORANGE = (242, 242, 238), (14, 14, 14), (240, 110, 20)

def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def centred(d, text, size, cx, cy, fill):
    f = font(size); b = d.textbbox((0, 0), text, font=f)
    d.text((cx - (b[2] - b[0]) / 2 - b[0], cy - (b[3] - b[1]) / 2 - b[1]), text, font=f, fill=fill)

def road_closed(path):
    W, H = 1024, 640; img = Image.new("RGB", (W, H), WHITE); d = ImageDraw.Draw(img)
    d.rounded_rectangle([18, 18, W - 18, H - 18], radius=40, outline=INK, width=22)
    centred(d, "ROAD", 230, W / 2, H * .31, INK); centred(d, "CLOSED", 230, W / 2, H * .70, INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

def detour(path):
    W, H = 1024, 512; img = Image.new("RGB", (W, H), ORANGE); d = ImageDraw.Draw(img)
    d.rounded_rectangle([14, 14, W - 14, H - 14], radius=34, outline=INK, width=18)
    centred(d, "DETOUR", 190, W / 2, H * .31, INK)
    y = H * .72; x0, x1 = W * .2, W * .8   # the arrow, pointing right
    d.rectangle([x0, y - 34, x1 - 120, y + 34], fill=INK); d.polygon([(x1 - 150, y - 96), (x1, y), (x1 - 150, y + 96)], fill=INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(OUT, exist_ok=True)
road_closed(os.path.join(OUT, "RoadClosed.png"))
detour(os.path.join(OUT, "Detour.png"))
