"""Round 60, Part A (Dan: Town Center made to look like itself; store and brand names faked as at the Kroger center): the faces of Town
Center's signs, all made-up names (no real store, brand or anchor). Hand-made content: writes Assets/HandMade/Mall/MallSigns.png, one sign per
row, which the map builder (MapBuilder.Mall.cs / MallInside.cs) only reads. The row list here and the Sign* constants there must stay in the same
order (SignShopFirst = 12, 30 shops; SignFoodFirst = 42, 8 food counters; SignPlayArea = 50).

Each row is 2048 x 160 pixels (12.8 : 1); the atlas is 2048 x 8192 (51 rows).

Usage: python Tools/handmade/make_mall_signs.py
"""
import os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Mall/MallSigns.png")
RW, RH, H = 2048, 160, 8192
S = 2

CREAM = (238, 233, 220)
SIGNS = [
    ("TOWN CENTER", (64, 54, 46), (244, 236, 220), "arialbd.ttf"),            # 0 over the main entrance and the side doors
    ("HALVERSON'S", (242, 238, 230), (165, 28, 38), "georgiab.ttf"),           # 1 the east anchor (drive-through store)
    ("HALVERSON'S HOME", (242, 238, 230), (165, 28, 38), "georgiab.ttf"),      # 2 the north anchor
    ("BELMONT", (246, 246, 248), (28, 62, 122), "arialbd.ttf"),                # 3 the west anchor
    ("J.C. CARROWAY", (238, 232, 220), (58, 58, 60), "timesbd.ttf"),           # 4 the south anchor
    ("STERLING & CO.", (150, 146, 138), (72, 68, 62), "arialbd.ttf"),          # 5 the closed north-west anchor (letters gone)
    ("SPACE AVAILABLE  555-0187", (246, 246, 242), (196, 32, 34), "arialbd.ttf"),   # 6 its banner (555: not a real number)
    ("FOOD COURT", (96, 34, 22), (246, 236, 214), "georgiab.ttf"),             # 7
    ("ESCALATORS  \u00b7  RESTROOMS  \u00b7  SECURITY", (232, 226, 212), (120, 104, 84), "arial.ttf"),   # 8 the way-finding band
    ("SUMMER SALE  UP TO 50% OFF", (226, 104, 40), (255, 255, 255), "arialbd.ttf"),   # 9 a hanging banner
    ("SHOP LATE  OPEN TIL 9 PM", (24, 120, 128), (255, 255, 255), "arialbd.ttf"),     # 10 another
    ("TOWN CENTER", (14, 14, 16), (246, 246, 246), "arial.ttf"),               # 11 the small black boards by the side doors
    # 12 to 41: the shops (MallInside's ShopCategory follows this order)
    ("URBAN THREADS", (20, 20, 22), (240, 240, 240), "arialbd.ttf"),
    ("SOLE CITY", (190, 30, 40), (255, 255, 255), "impact.ttf"),
    ("GLOW BEAUTY", (250, 236, 240), (196, 60, 120), "georgiab.ttf"),
    ("PAGE TURNER BOOKS", (40, 70, 50), (240, 230, 200), "georgiab.ttf"),
    ("GAME VAULT", (20, 20, 40), (120, 220, 255), "impact.ttf"),
    ("PRETZEL TWIST", (250, 214, 120), (130, 60, 20), "arialbd.ttf"),
    ("CANDY CORNER", (250, 240, 246), (220, 40, 120), "impact.ttf"),
    ("SUNSHADE OPTICS", (240, 240, 236), (30, 30, 30), "arial.ttf"),
    ("TEEN SCENE", (250, 60, 140), (255, 255, 255), "impact.ttf"),
    ("DENIM DEPOT", (40, 70, 130), (240, 240, 240), "arialbd.ttf"),
    ("CASE & CHARGE", (20, 20, 20), (90, 230, 120), "arialbd.ttf"),
    ("SPARKLE JEWELERS", (30, 30, 40), (230, 200, 120), "georgiab.ttf"),
    ("TOY TOWN", (250, 210, 40), (200, 30, 40), "impact.ttf"),
    ("KICKS CORNER", (240, 240, 240), (20, 20, 20), "impact.ttf"),
    ("COZY HOME", (236, 228, 214), (110, 80, 60), "georgiab.ttf"),
    ("SCENT & SOAP", (230, 244, 236), (40, 120, 90), "georgiab.ttf"),
    ("TRAIL OUTFITTERS", (60, 80, 50), (240, 230, 200), "arialbd.ttf"),
    ("PETITE MODE", (246, 240, 236), (120, 60, 90), "georgiab.ttf"),
    ("THE HAT RACK", (120, 40, 30), (246, 230, 200), "arialbd.ttf"),
    ("CARD & PARTY", (250, 250, 250), (230, 80, 40), "arialbd.ttf"),
    ("MUSIC BOX", (30, 20, 40), (240, 180, 60), "impact.ttf"),
    ("PHONE ZONE", (240, 240, 244), (40, 90, 200), "arialbd.ttf"),
    ("LITTLE SPROUTS", (236, 248, 230), (70, 150, 60), "arialbd.ttf"),
    ("GENTRY MENSWEAR", (30, 36, 50), (230, 220, 200), "timesbd.ttf"),
    ("BELLA BRIDAL", (250, 248, 246), (150, 120, 120), "georgiab.ttf"),
    ("SPORT ZONE", (20, 60, 140), (255, 255, 255), "impact.ttf"),
    ("TEA LEAF", (60, 90, 50), (236, 230, 200), "georgiab.ttf"),
    ("STUDIO PORTRAITS", (30, 30, 30), (236, 236, 236), "arial.ttf"),
    ("PET PALACE", (250, 240, 200), (60, 110, 160), "arialbd.ttf"),
    ("ARCADE ALLEY", (20, 10, 40), (255, 80, 200), "impact.ttf"),
    # 42 to 49: the food court's counters
    ("WOK EXPRESS", (170, 30, 30), (250, 210, 80), "arialbd.ttf"),
    ("PIZZA PRONTO", (250, 245, 235), (200, 40, 30), "impact.ttf"),
    ("BURGER BARN", (120, 50, 20), (250, 210, 60), "impact.ttf"),
    ("TACO FIESTA", (250, 180, 40), (120, 30, 20), "impact.ttf"),
    ("HERO SUBS", (30, 110, 60), (250, 230, 60), "arialbd.ttf"),
    ("CHICK'N COOP", (220, 60, 30), (255, 255, 255), "arialbd.ttf"),
    ("GYRO HUT", (30, 60, 140), (255, 255, 255), "arialbd.ttf"),
    ("SMOOTHIE SPOT", (240, 120, 160), (255, 255, 255), "arialbd.ttf"),
    ("PLAY AREA", (40, 140, 200), (255, 240, 80), "impact.ttf"),               # 50
]


def font(name, px):
    for n in (name, "arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), px)
        except OSError: pass
    return ImageFont.load_default()


def row(text, bg, fg, fname, idx):
    img = Image.new("RGB", (RW * S, RH * S), bg)
    d = ImageDraw.Draw(img)
    for k in range(6 * S):
        d.rectangle([k, k, RW * S - 1 - k, RH * S - 1 - k], outline=tuple(int(v * 0.78) for v in bg))
    px = int(RH * S * 0.66)
    while px > 10:
        f = font(fname, px)
        b = d.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= RW * S * 0.88 and b[3] - b[1] <= RH * S * 0.66: break
        px -= 4
    b = d.textbbox((0, 0), text, font=f)
    x = (RW * S - (b[2] - b[0])) // 2 - b[0]
    y = (RH * S - (b[3] - b[1])) // 2 - b[1]
    if idx == 5:
        # the closed anchor: three letters taken down (their outlines darker where the sun never reached), the rest faded and streaked
        random.seed(5)
        gone = {1, 4, 9}
        for i, ch in enumerate(text):
            cx = x + d.textlength(text[:i], font=f)
            d.text((cx, y), ch, font=f, fill=tuple(int(v * 0.88) for v in bg) if i in gone else fg)
        for _ in range(90):
            sx = random.randint(0, RW * S); L = random.randint(20, RH * S)
            d.line([(sx, 0), (sx + random.randint(-6, 6), L)], fill=(98, 94, 86), width=random.randint(2, 7))
        img = img.filter(ImageFilter.GaussianBlur(1.6))
    else:
        d.text((x, y), text, font=f, fill=fg)
    return img.resize((RW, RH), Image.LANCZOS)


assert len(SIGNS) == 51, len(SIGNS)
full = Image.new("RGB", (RW, H), (128, 128, 128))
for i, (t, bg, fg, fn) in enumerate(SIGNS):
    full.paste(row(t, bg, fg, fn, i), (0, i * RH))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
full.save(OUT)
print("wrote", OUT, len(SIGNS), "signs")
