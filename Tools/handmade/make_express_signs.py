"""Round 80 (E, Dan's photo local/reference_photos/express_lanes/express_lane_gate.png): the faces of the Express Lanes' gate signs, drawn here (not
copied from the photo): EXPRESS LANE with a down-right arrow (white on green, 2 x 3), the keep-right sign (a black up arrow beside a black wedge on
white, 2 x 3) and the DO NOT ENTER banner hung under the swing arm (white on red, 4 x 1). Hand-made content: writes Assets/HandMade/RoadSigns/
ExpressLane.png, ExpressKeepRight.png and ExpressDoNotEnter.png, which the map builder only reads (MapBuilder.Round80.cs).

Usage: python Tools/handmade/make_express_signs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/RoadSigns")
WHITE, INK, GREEN, RED = (244, 244, 240), (14, 14, 14), (0, 112, 60), (200, 28, 36)

def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def centred(d, text, size, cx, cy, fill):
    f = font(size); b = d.textbbox((0, 0), text, font=f)
    d.text((cx - (b[2] - b[0]) / 2 - b[0], cy - (b[3] - b[1]) / 2 - b[1]), text, font=f, fill=fill)

def express_lane(path):
    W, H = 512, 768; img = Image.new("RGB", (W, H), GREEN); d = ImageDraw.Draw(img)
    d.rounded_rectangle([12, 12, W - 12, H - 12], radius=36, outline=WHITE, width=12)
    centred(d, "EXPRESS", 82, W / 2, H * .17, WHITE); centred(d, "LANE", 104, W / 2, H * .36, WHITE)
    # the arrow, down and to the right
    import math
    cx, cy, L = W * .5, H * .7, 250; a = math.radians(45)
    ux, uy = math.cos(a), math.sin(a); px, py = -uy, ux
    x0, y0 = cx - ux * L / 2, cy - uy * L / 2; x1, y1 = cx + ux * L / 2, cy + uy * L / 2
    hw, head = 26, 90
    xb, yb = x1 - ux * head, y1 - uy * head
    d.polygon([(x0 + px * hw, y0 + py * hw), (xb + px * hw, yb + py * hw), (xb + px * 80, yb + py * 80), (x1, y1),
               (xb - px * 80, yb - py * 80), (xb - px * hw, yb - py * hw), (x0 - px * hw, y0 - py * hw)], fill=WHITE)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

def keep_right(path):
    W, H = 512, 768; img = Image.new("RGB", (W, H), WHITE); d = ImageDraw.Draw(img)
    d.rounded_rectangle([12, 12, W - 12, H - 12], radius=30, outline=INK, width=12)
    # the divider: a black wedge on the left, from a point at the bottom middle-left widening up
    d.polygon([(W * .40, H * .88), (W * .40, H * .40), (W * .16, H * .22), (W * .16, H * .14), (W * .28, H * .14), (W * .44, H * .30), (W * .44, H * .88)], fill=INK)
    # the arrow up on the right
    x = W * .66; d.rectangle([x - 30, H * .30, x + 30, H * .88], fill=INK); d.polygon([(x - 92, H * .34), (x, H * .12), (x + 92, H * .34)], fill=INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

def do_not_enter(path):
    W, H = 1024, 256; img = Image.new("RGB", (W, H), RED); d = ImageDraw.Draw(img)
    d.rectangle([10, 10, W - 10, H - 10], outline=WHITE, width=10)
    centred(d, "DO NOT ENTER", 112, W / 2, H / 2, WHITE)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(OUT, exist_ok=True)
express_lane(os.path.join(OUT, "ExpressLane.png"))
keep_right(os.path.join(OUT, "ExpressKeepRight.png"))
do_not_enter(os.path.join(OUT, "ExpressDoNotEnter.png"))
