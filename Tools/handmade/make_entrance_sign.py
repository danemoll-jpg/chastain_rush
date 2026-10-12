"""Round 9 (BUG-004): the lettered panel of the brick "Chastain Glen" entrance monuments, drawn after the entrance photo
(local/reference_photos/entrance/, never copied): a black panel with a gold border, an arch at the top with white cattails,
"CHASTAIN GLEN" in gold capitals. Transparent outside the panel (the material cuts it out). Hand-made content: writes
Assets/HandMade/Entrance/ChastainGlenSign.png, which the map builder only reads. The quad it goes on is 2.7 x 1.9 m, so at
1024 x 720 pixels one metre is about 380 pixels; the capitals are about 0.3 m tall (readable at 20 to 30 m).

Usage: python Tools/handmade/make_entrance_sign.py [LINE1 LINE2 FILE]
Round 22: the same panel for the other communities' monuments, e.g.
  python Tools/handmade/make_entrance_sign.py CHASTAIN COMMONS ChastainCommonsSign.png
  python Tools/handmade/make_entrance_sign.py CHASTAIN ENCLAVE ChastainEnclaveSign.png
(no arguments: "CHASTAIN GLEN", ChastainGlenSign.png, as before).
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LINES = (sys.argv[1], sys.argv[2]) if len(sys.argv) >= 3 else ("CHASTAIN", "GLEN")
OUT = os.path.join(ROOT, "Assets/HandMade/Entrance", sys.argv[3] if len(sys.argv) >= 4 else "ChastainGlenSign.png")
W, H, S = 1024, 720, 2            # drawn at twice the size, then reduced (smooth edges)
GOLD, BLACK, WHITE = (196, 160, 72, 255), (22, 22, 24, 255), (236, 236, 230, 255)

def panel(draw, inset, fill):
    """The panel: a wide band with a round arch in the middle of its top and a deep curved bottom."""
    w, h, i = W * S, H * S, inset * S
    band_top, band_bot = int(h * 0.30) + i, int(h * 0.62)
    draw.rectangle([i, band_top, w - i, band_bot], fill=fill)
    draw.ellipse([i, int(h * 0.12) + i - band_top + band_top, w - i, h * 1.0 - i], fill=fill)          # the deep bottom
    aw = int(w * 0.30)
    draw.ellipse([w // 2 - aw + i, i, w // 2 + aw - i, int(h * 0.62)], fill=fill)                       # the arch on top
    draw.rectangle([i, band_top, w - i, int(h * 0.55)], fill=fill)

img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
panel(d, 0, GOLD)
panel(d, 14, BLACK)
# a thin gold line inside the border
inner = Image.new("RGBA", img.size, (0, 0, 0, 0)); di = ImageDraw.Draw(inner)
panel(di, 26, GOLD); panel(di, 31, (0, 0, 0, 0))
img.alpha_composite(inner)
d = ImageDraw.Draw(img)
# white cattails in the arch
cx, base = W * S // 2, int(H * S * 0.30)
for k, (dx, hgt) in enumerate([(-70, 150), (-30, 190), (8, 170), (44, 205), (82, 140)]):
    x = cx + dx * S // 2 * 2
    d.line([(x, base), (x + (k - 2) * 6, base - hgt)], fill=WHITE, width=7)
    d.ellipse([x + (k - 2) * 6 - 10, base - hgt - 6, x + (k - 2) * 6 + 10, base - hgt + 48], fill=WHITE)
for k in range(-6, 7):
    d.line([(cx + k * 22, base), (cx + k * 30, base - 60 - abs(k) * 4)], fill=WHITE, width=5)
d.ellipse([cx - 210, base - 12, cx + 210, base + 26], fill=WHITE)
# the name
font = None
for name in ("georgiab.ttf", "BOOKOSB.TTF", "timesbd.ttf", "cambriab.ttf"):
    try: font = ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), 112 * S); break
    except OSError: pass
font = font or ImageFont.load_default()
for text, y in ((LINES[0], 0.43), (LINES[1], 0.62)):
    box = d.textbbox((0, 0), text, font=font)
    tw = box[2] - box[0]
    d.text(((W * S - tw) // 2 - box[0], int(H * S * y) - box[1]), text, font=font, fill=GOLD)
img = img.resize((W, H), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print("wrote", OUT)
