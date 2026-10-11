"""Round 19, item 5 (Dan: "the entrance needs to be more obvious... a clear hint where it is would be good"): the face of a
kid's birthday-party yard sign at shortcut R3's turn-off: a white-painted board with "PARTY" in bright hand-painted capitals,
each letter its own color, and a big red arrow toward the gap between the two houses. Hand-made content: writes
Assets/HandMade/Shortcut/PartySignLeft.png and PartySignRight.png (the arrow pointing left or right), and the same board
with "JUMP!" for R1's turn-off (JumpSignLeft.png, JumpSignRight.png), which the map builder only reads. The board is 3.2 x 1.6 m, so at 2048 x 1024 pixels one meter is 640 pixels; the capitals are about 0.67 m tall and
the arrow about 0.5 m through (readable flat out from 60 m).

Usage: python Tools/handmade/make_party_sign.py
"""
import os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Shortcut")
W, H = 2048, 1024
PAINT, SHADE = (244, 240, 228), (222, 214, 196)
COLORS = [(222, 38, 44), (30, 96, 214), (32, 160, 62), (246, 140, 20), (140, 52, 186)]
RED, OUTLINE = (214, 30, 36), (30, 26, 24)

def font(size):
    for name in ("impact.ttf", "arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()

def draw(arrow_left, text="PARTY"):
    random.seed(19)
    img = Image.new("RGB", (W, H), PAINT)
    d = ImageDraw.Draw(img)
    # brush strokes in the white paint, the board's edge a little grubby
    for _ in range(220):
        y = random.randint(0, H); x0 = random.randint(-300, W); L = random.randint(200, 900)
        d.line([(x0, y), (x0 + L, y + random.randint(-8, 8))], fill=tuple(c + random.randint(-14, 4) for c in SHADE), width=random.randint(3, 9))
    img = img.filter(ImageFilter.GaussianBlur(1.6))
    d = ImageDraw.Draw(img)
    for k in range(22):
        c = tuple(int(v * (0.78 + 0.01 * k)) for v in PAINT)
        d.rectangle([k, k, W - 1 - k, H - 1 - k], outline=c)
    # PARTY: each capital its own color with a dark outline, a little tilted and off its line, as a parent paints it
    f = font(470)
    boxes = [d.textbbox((0, 0), ch, font=f) for ch in text]
    widths = [b[2] - b[0] for b in boxes]
    gap = 30
    x = (W - (sum(widths) + gap * (len(text) - 1))) // 2; top = 60
    for i, (ch, w, b) in enumerate(zip(text, widths, boxes)):
        layer = Image.new("RGBA", (w + 120, b[3] - b[1] + 120), (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        ld.text((60 - b[0], 60 - b[1]), ch, font=f, fill=COLORS[i % len(COLORS)] + (255,), stroke_width=14, stroke_fill=OUTLINE + (255,))
        layer = layer.rotate(random.uniform(-6, 6), resample=Image.BICUBIC)
        img.paste(layer, (x - 60, top - 60 + random.randint(-14, 14)), layer)
        x += w + gap
    # the arrow under it, toward the gap
    d = ImageDraw.Draw(img)
    cy = int(H * 0.755); x0, x1 = 180, W - 180; hh = 82; head = 300
    pts = [(x0, cy), (x0 + head, cy - 2 * hh - 36), (x0 + head, cy - hh), (x1, cy - hh + 10), (x1, cy + hh - 8), (x0 + head, cy + hh), (x0 + head, cy + 2 * hh + 36)]
    if not arrow_left: pts = [(W - px, py) for px, py in pts]
    d.polygon(pts, fill=RED, outline=OUTLINE)
    d.line(pts + [pts[0]], fill=OUTLINE, width=12, joint="curve")
    return img

os.makedirs(OUT, exist_ok=True)
for word, name in (("PARTY", "PartySign"), ("JUMP!", "JumpSign")):   # Round 19: JUMP! for R1's turn-off (the kids' ramp)
    for left in (True, False):
        p = os.path.join(OUT, name + ("Left.png" if left else "Right.png"))
        draw(left, word).save(p); print("wrote", p)
