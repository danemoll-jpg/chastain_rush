"""Round 12, item 3 (Dan: "since it is off the road, we would need some sort of clue that there is a shortcut that way (a
sign if need be)"): the face of a home-made sign at shortcut #2's turn-off: a weathered plywood board with "SHORTCUT" and
an arrow pointing left, painted by hand. Hand-made content: writes Assets/HandMade/Shortcut/ShortcutSign.png, which the
map builder only reads. The board is 1.6 x 0.8 m, so at 1024 x 512 pixels one metre is 640 pixels; the capitals are about
0.25 m tall and the arrow about 0.3 m (readable at 20 to 30 m).

Usage: python Tools/handmade/make_shortcut_sign.py
"""
import os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Shortcut/ShortcutSign.png")
W, H, S = 1024, 512, 2            # drawn at twice the size, then reduced (smooth edges)
PLY, GRAIN, RED, BLACK = (205, 176, 128), (176, 146, 101), (168, 30, 24), (28, 24, 22)
random.seed(12)

img = Image.new("RGB", (W * S, H * S), PLY)
d = ImageDraw.Draw(img)
# plywood: long streaks of grain, a few knots, darker weathered edges
for _ in range(140):
    y = random.randint(0, H * S); x0 = random.randint(-200, W * S); L = random.randint(300, 1400)
    d.line([(x0, y), (x0 + L, y + random.randint(-14, 14))], fill=tuple(c + random.randint(-12, 8) for c in GRAIN), width=random.randint(2, 6))
for _ in range(5):
    x, y = random.randint(80, W * S - 80), random.randint(60, H * S - 60)
    d.ellipse([x - 26, y - 12, x + 26, y + 12], fill=(150, 118, 76))
img = img.filter(ImageFilter.GaussianBlur(1.2))
d = ImageDraw.Draw(img)
for k in range(18):
    c = tuple(int(v * (0.80 + 0.011 * k)) for v in PLY)
    d.rectangle([k, k, W * S - 1 - k, H * S - 1 - k], outline=c)
# the word, in thick brush capitals, each letter a little off its line as if painted by hand
font = None
for name in ("impact.ttf", "arialbd.ttf", "verdanab.ttf"):
    try: font = ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), 200 * S); break
    except OSError: pass
font = font or ImageFont.load_default()
text = "SHORTCUT"
widths = [d.textbbox((0, 0), ch, font=font)[2] - d.textbbox((0, 0), ch, font=font)[0] for ch in text]
gap = 14 * S
total = sum(widths) + gap * (len(text) - 1)
x = (W * S - total) // 2; top = 34 * S
for ch, w in zip(text, widths):
    box = d.textbbox((0, 0), ch, font=font)
    layer = Image.new("RGBA", (w + 80, box[3] - box[1] + 80), (0, 0, 0, 0))
    ImageDraw.Draw(layer).text((40 - box[0], 40 - box[1]), ch, font=font, fill=RED + (255,))
    layer = layer.rotate(random.uniform(-4, 4), resample=Image.BICUBIC)
    img.paste(layer, (x - 40, top - 40 + random.randint(-8, 8)), layer)
    x += w + gap
# the arrow, pointing left (the turn), painted under the word
d = ImageDraw.Draw(img)
cy = int(H * S * 0.74); x0, x1 = 150 * S, (W - 150) * S; hh = 34 * S; head = 120 * S
d.polygon([(x0, cy), (x0 + head, cy - 2 * hh - 20 * S), (x0 + head, cy - hh), (x1, cy - hh + 6 * S), (x1, cy + hh - 4 * S), (x0 + head, cy + hh), (x0 + head, cy + 2 * hh + 20 * S)], fill=BLACK)
img = img.resize((W, H), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print("wrote", OUT)
