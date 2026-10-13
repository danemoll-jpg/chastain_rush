"""Round 23 follow-ups (the underground pipe network, Part C): the painted signs in its chambers. Hand-made content: writes
Assets/HandMade/Network/NetworkSigns.png, one label per row, in the order of MapBuilder.NetworkHub.cs's NetSignRows.

Each row is 512 x 96 pixels and its board 4.8 m x 0.9 m on the wall (the hub's name 12 m x 2.25 m): stenciled capitals, as
painted on concrete, each main's name in its stripe's color (Data/pipes/network.json; Tools/mapgen/pipe_network.py STRIPES), a
junction's name in white. Transparent round the letters (a cutout material). The mains have signs and stripes; the maze branches
stay unmarked (Dan's vision, Round 23).

Usage: python Tools/handmade/make_network_signs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Network/NetworkSigns.png")
ROWS = ["M1 TRUNK", "M5 ENCLAVE MAIN", "M3 NOONDAY MAIN", "M4 GLEN MAIN", "M2 LAKES MAIN", "M6 NELSON MAIN", "THE HUB",
        "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9", "J10", "J12", "J13"]
COLORS = {"M1": (230, 41, 41), "M5": (255, 140, 13), "M3": (38, 191, 64), "M4": (255, 217, 26), "M2": (46, 115, 255), "M6": (158, 71, 242)}
W, H, S = 512, 96, 2


def font(px):
    for n in ("impact.ttf", "arialbd.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), px)
        except OSError: pass
    return ImageFont.load_default()


img = Image.new("RGBA", (W * S, H * S * len(ROWS)), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
for r, text in enumerate(ROWS):
    col = COLORS.get(text.split()[0], (235, 235, 230))
    px = 78 * S
    f = font(px)
    while d.textlength(text, font=f) > W * S * 0.94 and px > 20: px -= 4; f = font(px)
    tw = d.textlength(text, font=f); bb = d.textbbox((0, 0), text, font=f)
    x = (W * S - tw) / 2; y = r * H * S + (H * S - (bb[3] - bb[1])) / 2 - bb[1]
    d.text((x, y), text, font=f, fill=col + (255,), stroke_width=3 * S, stroke_fill=(20, 20, 20, 235))
img = img.resize((W, H * len(ROWS)), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print(f"wrote {OUT}: {len(ROWS)} rows of {W} x {H}")
