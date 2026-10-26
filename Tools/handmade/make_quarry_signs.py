"""Round 73 (C3, Dan: "Truly don't even understand how to get to the ramps in general"): the Viking Quarry jumps' signs. Hand-made content: writes
Assets/HandMade/RoadSigns/QuarryJumps.png (1024 x 512: the turn-off boards on the service roads, white on brown like a park's sign, QUARRY JUMPS and
an arrow along the drive) and QuarryBoard_J1.png .. J3.png (1024 x 640: the board beside each start box: the jump's name, how to start, its lip
speed window from Data/tracks/quarry_jumps.json), which the map builder only reads (MapBuilder.QuarryJumps.cs). Re-run after make_quarry_jumps.py.

Usage: python Tools/handmade/make_quarry_signs.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/RoadSigns")
BROWN, CREAM, RED, BLUE = (92, 58, 34), (244, 236, 214), (178, 30, 32), (24, 52, 140)

def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def centred(d, text, size, cx, cy, fill):
    f = font(size); b = d.textbbox((0, 0), text, font=f)
    while b[2] - b[0] > 940 and size > 20: size -= 4; f = font(size); b = d.textbbox((0, 0), text, font=f)
    d.text((cx - (b[2] - b[0]) / 2 - b[0], cy - (b[3] - b[1]) / 2 - b[1]), text, font=f, fill=fill)

def turnoff(path):
    W, H = 1024, 512; img = Image.new("RGB", (W, H), BROWN); d = ImageDraw.Draw(img)
    d.rounded_rectangle([16, 16, W - 16, H - 16], radius=36, outline=CREAM, width=14)
    centred(d, "QUARRY JUMPS", 150, W / 2, H * .30, CREAM)
    y = H * .70; x0, x1 = W * .22, W * .78   # the arrow: straight on, along the drive
    d.rectangle([x0, y - 30, x1 - 110, y + 30], fill=CREAM); d.polygon([(x1 - 140, y - 88), (x1, y), (x1 - 140, y + 88)], fill=CREAM)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

def board(path, name, lo, hi):
    W, H = 1024, 640; img = Image.new("RGB", (W, H), CREAM); d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 150], fill=BLUE)
    for k in range(7): d.rectangle([0, 150 + k * 22, W, 150 + k * 22 + 11], fill=RED)
    d.rounded_rectangle([10, 10, W - 10, H - 10], radius=24, outline=BROWN, width=12)
    centred(d, name.upper(), 110, W / 2, 80, CREAM)
    centred(d, "STOP IN THE BOX", 84, W / 2, 380, BROWN)
    centred(d, "FACING THE RAMP", 84, W / 2, 470, BROWN)
    centred(d, f"HIT THE LIP AT {lo:.0f} TO {hi:.0f} MPH", 60, W / 2, 570, RED)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(OUT, exist_ok=True)
turnoff(os.path.join(OUT, "QuarryJumps.png"))
q = json.load(open(os.path.join(ROOT, "Data/tracks/quarry_jumps.json"), encoding="utf-8"))
for j in q["jumps"]:
    board(os.path.join(OUT, f"QuarryBoard_{j['id']}.png"), j["name"], j["window"]["lo"] * 2.23694, j["window"]["hi"] * 2.23694)
