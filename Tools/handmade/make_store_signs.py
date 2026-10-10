"""Round 17, Part B (Dan: "Store names can be faked"): the faces of the shopping center's signs, all made-up names (no real
store or brand). Hand-made content: writes Assets/HandMade/ShoppingCenter/StoreSigns.png, one sign per row, which the map
builder (MapBuilder.ShoppingCenter.cs) only reads. The row list here and SignRows there must stay in the same order.

Each row is 2048 x 160 pixels (12.8 : 1). A shop board is 9 m x 0.7 m, so its capitals are about 0.45 m tall (readable at
20 to 30 m); the supermarket's board is 26 m x 2 m.

Usage: python Tools/handmade/make_store_signs.py
"""
import os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/ShoppingCenter/StoreSigns.png")
RW, RH, ROWS = 2048, 160, 24      # 24 rows fit in 2048 x 4096 (rows beyond the list stay blank)
S = 2                              # drawn at twice the size, then reduced (smooth edges)

# (text, board color, letter color, font) in row order. Keep in step with MapBuilder.ShoppingCenter.cs (SignRows).
SIGNS = [
    ("FRESH ACRES MARKET", (238, 236, 228), (34, 112, 52), "arialbd.ttf"),     # 0 the supermarket
    ("PHARMACY", (238, 236, 228), (180, 36, 40), "arialbd.ttf"),              # 1 its second sign
    ("BEAN THERE COFFEE", (60, 40, 30), (240, 222, 190), "georgiab.ttf"),     # 2 the north row, east to west
    ("PACK & SHIP CENTER", (30, 60, 120), (250, 250, 250), "arialbd.ttf"),    # 3
    ("LUCKY WOK", (170, 30, 30), (250, 210, 80), "arialbd.ttf"),              # 4
    ("MANE STREET SALON", (20, 20, 22), (230, 190, 210), "georgiab.ttf"),     # 5
    ("CORNER SLICE PIZZA", (250, 245, 235), (200, 40, 30), "impact.ttf"),     # 6
    ("BRIGHT SMILE DENTAL", (245, 250, 252), (30, 120, 170), "arialbd.ttf"),  # 7
    ("CHASTAIN CLEANERS", (30, 50, 90), (250, 250, 250), "arialbd.ttf"),      # 8
    ("TAQUERIA SOL", (250, 180, 40), (120, 30, 20), "impact.ttf"),            # 9
    ("BLOOM FLORIST", (240, 240, 236), (190, 60, 120), "georgiab.ttf"),       # 10
    ("SPIN CITY LAUNDRY", (40, 120, 170), (255, 255, 255), "impact.ttf"),     # 11
    ("PAWS & SUDS DOG WASH", (60, 150, 160), (255, 255, 255), "arialbd.ttf"), # 12 the south face (BUG-003)
    ("GLOW SKIN STUDIO", (245, 238, 232), (150, 110, 90), "georgiab.ttf"),    # 13
    ("BARGAIN BOOKS", (110, 30, 30), (245, 230, 200), "georgiab.ttf"),        # 14
    ("SUNRISE NAILS", (250, 230, 235), (200, 70, 110), "arialbd.ttf"),        # 15
    ("QUIK FUEL", (200, 30, 36), (255, 255, 255), "impact.ttf"),              # 16 the gas station canopy
    ("FIRST CHASTAIN BANK", (150, 146, 136), (70, 66, 60), "georgiab.ttf"),  # 17 the abandoned bank: letters gone, faded
    ("FOR LEASE  555-0142", (245, 245, 240), (200, 30, 30), "arialbd.ttf"),   # 18 the bank's banner (555: not a real number)
]


def font(name, px):
    for n in (name, "arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), px)
        except OSError: pass
    return ImageFont.load_default()


def row(text, bg, fg, fname, idx):
    img = Image.new("RGB", (RW * S, RH * S), bg)
    d = ImageDraw.Draw(img)
    # a thin darker frame round the board
    for k in range(6 * S):
        c = tuple(int(v * 0.78) for v in bg)
        d.rectangle([k, k, RW * S - 1 - k, RH * S - 1 - k], outline=c)
    # the largest size that fits 88% of the width and 66% of the height
    px = int(RH * S * 0.66)
    while px > 10:
        f = font(fname, px)
        b = d.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= RW * S * 0.88 and b[3] - b[1] <= RH * S * 0.66: break
        px -= 4
    b = d.textbbox((0, 0), text, font=f)
    x = (RW * S - (b[2] - b[0])) // 2 - b[0]
    y = (RH * S - (b[3] - b[1])) // 2 - b[1]
    d.text((x, y), text, font=f, fill=fg)
    if idx == 17:
        # the abandoned bank: three letters fell off (their outlines still show, darker where the sun never reached),
        # the rest sun-faded and streaked with grime
        random.seed(17)
        img = Image.new("RGB", (RW * S, RH * S), bg); d = ImageDraw.Draw(img)
        for k in range(6 * S):
            d.rectangle([k, k, RW * S - 1 - k, RH * S - 1 - k], outline=tuple(int(v * 0.78) for v in bg))
        gone = {1, 10, 17}   # FIRST CHASTAIN BANK -> F RST CHAS AIN B NK
        for i, ch in enumerate(text):
            cx = x + d.textlength(text[:i], font=f)
            d.text((cx, y), ch, font=f, fill=tuple(int(v * 0.88) for v in bg) if i in gone else fg)
        for _ in range(90):
            sx = random.randint(0, RW * S); L = random.randint(20, RH * S)
            d.line([(sx, 0), (sx + random.randint(-6, 6), L)], fill=(92, 88, 80), width=random.randint(2, 7))
        img = img.filter(ImageFilter.GaussianBlur(1.6))
    return img.resize((RW, RH), Image.LANCZOS)


atlas = Image.new("RGB", (RW, RH * ROWS), (128, 128, 128))
for i, (t, bg, fg, fn) in enumerate(SIGNS):
    atlas.paste(row(t, bg, fg, fn, i), (0, i * RH))
# pad to a power of two (2048 x 4096); row i occupies v from 1 - (i + 1) * RH / H to 1 - i * RH / H
full = Image.new("RGB", (RW, 4096), (128, 128, 128))
full.paste(atlas, (0, 0))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
full.save(OUT)
print("wrote", OUT, len(SIGNS), "signs")
