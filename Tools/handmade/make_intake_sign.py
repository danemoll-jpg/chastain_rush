"""Round 49 (A, Dan 2026-10-19: the reverse Chastain Lakes intake shortcut; a hint at the turn-off, "something that could be there"): the face of
the lake association's sign at the mouth of the service track up the point to West Chastain Lake's intake: a white aluminum board with a blue
header "CHASTAIN LAKES", "LAKE INTAKE" in big black capitals, "SERVICE VEHICLES ONLY" under it and a black arrow toward the track. Hand-made
content: writes Assets/HandMade/Shortcut/IntakeSignLeft.png and IntakeSignRight.png (the arrow pointing left or right), which the map builder
only reads. The board is 2.4 x 1.6 m, so at 1536 x 1024 pixels one meter is 640 pixels; the capitals are about 0.29 m tall (readable at 30 m).

Usage: python Tools/handmade/make_intake_sign.py
"""
import os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Shortcut")
W, H = 1536, 1024
WHITE, BLUE, BLACK, GRAY = (238, 238, 232), (22, 70, 150), (24, 24, 26), (150, 150, 146)

def font(size, bold=True):
    for name in (("arialbd.ttf", "verdanab.ttf") if bold else ("arial.ttf", "verdana.ttf")):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def centered(d, y, text, f, fill):
    b = d.textbbox((0, 0), text, font=f); d.text(((W - (b[2] - b[0])) // 2 - b[0], y - b[1]), text, font=f, fill=fill)

def draw(arrow_left):
    random.seed(49)
    img = Image.new("RGB", (W, H), WHITE); d = ImageDraw.Draw(img)
    # a little weathering: faint streaks, a darker rim, four bolts
    for _ in range(120):
        x = random.randint(0, W); d.line([(x, 0), (x + random.randint(-30, 30), H)], fill=tuple(c - random.randint(4, 12) for c in WHITE), width=random.randint(2, 6))
    img = img.filter(ImageFilter.GaussianBlur(1.2)); d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W - 1, H - 1], outline=GRAY, width=18); d.rectangle([30, 30, W - 31, H - 31], outline=BLACK, width=10)
    d.rectangle([40, 40, W - 41, 230], fill=BLUE)
    centered(d, 78, "CHASTAIN LAKES", font(120), WHITE)
    centered(d, 300, "LAKE INTAKE", font(185), BLACK)
    centered(d, 560, "SERVICE VEHICLES ONLY", font(96), BLACK)
    cy = 820; x0, x1, hh, head = 360, W - 360, 46, 170
    pts = [(x0, cy), (x0 + head, cy - 2 * hh - 30), (x0 + head, cy - hh), (x1, cy - hh), (x1, cy + hh), (x0 + head, cy + hh), (x0 + head, cy + 2 * hh + 30)]
    if not arrow_left: pts = [(W - px, py) for px, py in pts]
    d.polygon(pts, fill=BLACK)
    for bx, by in ((70, 270), (W - 70, 270), (70, H - 70), (W - 70, H - 70)): d.ellipse([bx - 14, by - 14, bx + 14, by + 14], fill=GRAY, outline=BLACK, width=3)
    return img

os.makedirs(OUT, exist_ok=True)
for left in (True, False):
    p = os.path.join(OUT, "IntakeSign" + ("Left.png" if left else "Right.png"))
    draw(left).save(p); print("wrote", p)
