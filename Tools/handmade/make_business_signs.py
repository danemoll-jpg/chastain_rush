"""Round 31 (4) (Dan: "I want to have the buildings that aren't part of a neighborhood also dressed up. Much of Canton Road
would be businesses or schools"; 2026-10-10: "Store names can be faked"): the faces of the business signs, every name made
up (no real store, brand, school or church). Hand-made content: writes Assets/HandMade/Businesses/BusinessSigns.png, which
the map builder (MapBuilder.Businesses.cs) only reads. The kinds and their row ranges there (SignKinds) must match KINDS here.

The atlas is 2048 x 4096: two columns of 1024 x 128 pixel rows (8 : 1), 32 rows a column, 64 signs. Row r sits in column
r // 32, row r % 32 from the top. A board is w x w / 8 metres (6 m wide: 0.75 m tall, capitals about 0.45 m: readable at 20 to
30 m).

Usage: python Tools/handmade/make_business_signs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Businesses/BusinessSigns.png")
RW, RH, PER_COL = 1024, 128, 32
S = 2

# kind -> signs (text, board, letters, font). Order of KINDS = row order; keep in step with MapBuilder.Businesses.cs SignKinds.
KINDS = [
    ("shop", [
        ("PEACHTREE PAWN", (245, 200, 40), (30, 30, 30), "impact.ttf"),
        ("SMOKY RIDGE BBQ", (60, 30, 20), (250, 200, 120), "georgiab.ttf"),
        ("TIRE TOWN", (20, 20, 20), (250, 210, 0), "impact.ttf"),
        ("NAIL BAR", (250, 236, 240), (190, 60, 110), "georgiab.ttf"),
        ("BIG DOG VAPE", (40, 40, 120), (255, 255, 255), "impact.ttf"),
        ("CANTON ROAD CYCLES", (200, 30, 30), (255, 255, 255), "arialbd.ttf"),
        ("MAMA ROSA'S ITALIAN", (30, 90, 40), (250, 245, 230), "georgiab.ttf"),
        ("QUICK CASH LOANS", (20, 110, 50), (255, 255, 255), "arialbd.ttf"),
        ("GOLDEN DRAGON", (170, 20, 20), (250, 210, 80), "georgiab.ttf"),
        ("ACE MATTRESS", (30, 60, 140), (255, 255, 255), "impact.ttf"),
        ("SOUTHERN THREADS", (240, 236, 228), (110, 40, 60), "georgiab.ttf"),
        ("HOT WINGS HUT", (220, 90, 20), (255, 245, 220), "impact.ttf"),
        ("SPARKLE CAR WASH", (40, 140, 200), (255, 255, 255), "impact.ttf"),
        ("DOLLAR DEPOT", (250, 230, 60), (40, 90, 30), "impact.ttf"),
        ("EL RANCHITO", (30, 120, 60), (255, 230, 120), "georgiab.ttf"),
        ("FIT ZONE GYM", (20, 20, 22), (230, 60, 50), "impact.ttf"),
        ("PHO SAIGON", (250, 240, 220), (160, 30, 20), "georgiab.ttf"),
        ("GREEN LEAF SMOOTHIES", (60, 160, 70), (255, 255, 255), "arialbd.ttf"),
        ("U-STORE-IT", (240, 120, 20), (255, 255, 255), "impact.ttf"),
        ("BEST BUY USED CARS", (255, 255, 255), (20, 60, 150), "impact.ttf"),
    ]),
    ("office", [
        ("CHASTAIN PROFESSIONAL CENTER", (60, 64, 70), (240, 240, 236), "georgiab.ttf"),
        ("NOONDAY OFFICE PARK", (40, 70, 60), (240, 236, 220), "georgiab.ttf"),
        ("HARGROVE & LEE LAW", (30, 34, 50), (220, 200, 150), "georgiab.ttf"),
        ("PINE CREST MEDICAL", (240, 244, 248), (30, 100, 160), "arialbd.ttf"),
        ("SUMMIT INSURANCE", (20, 50, 100), (255, 255, 255), "arialbd.ttf"),
        ("BELLWOOD DENTAL", (245, 250, 252), (30, 130, 150), "arialbd.ttf"),
        ("RIVERSTONE REALTY", (110, 30, 30), (245, 235, 215), "georgiab.ttf"),
        ("KESTREL SOFTWARE", (30, 30, 34), (90, 200, 220), "arialbd.ttf"),
        ("OAK HOLLOW PEDIATRICS", (250, 248, 240), (60, 120, 60), "georgiab.ttf"),
        ("TRI-COUNTY TITLE", (60, 60, 64), (250, 250, 250), "arialbd.ttf"),
    ]),
    ("industrial", [
        ("CENTERLINE LOGISTICS", (30, 60, 110), (255, 255, 255), "arialbd.ttf"),
        ("SOUTHERN PALLET CO.", (150, 100, 40), (255, 250, 235), "impact.ttf"),
        ("RIDGELINE DISTRIBUTION", (50, 50, 54), (250, 200, 40), "arialbd.ttf"),
        ("CHEROKEE STEEL SUPPLY", (120, 30, 30), (255, 255, 255), "impact.ttf"),
        ("BLUE RIDGE PLUMBING SUPPLY", (30, 90, 150), (255, 255, 255), "arialbd.ttf"),
        ("ACME FABRICATION", (240, 240, 236), (180, 30, 30), "impact.ttf"),
        ("METRO MOVING & STORAGE", (250, 180, 20), (30, 30, 30), "impact.ttf"),
        ("PRECISION MACHINE WORKS", (70, 74, 80), (240, 240, 240), "arialbd.ttf"),
    ]),
    ("church", [
        ("GRACE FELLOWSHIP CHURCH", (245, 242, 232), (60, 40, 90), "georgiab.ttf"),
        ("FIRST COMMUNITY CHURCH", (245, 242, 232), (30, 60, 110), "georgiab.ttf"),
        ("HOPE BAPTIST CHURCH", (245, 242, 232), (100, 30, 30), "georgiab.ttf"),
        ("ST. ANDREW'S CHAPEL", (245, 242, 232), (40, 70, 50), "georgiab.ttf"),
    ]),
    ("school", [
        ("PINE HOLLOW ELEMENTARY", (40, 70, 140), (255, 255, 255), "arialbd.ttf"),
        ("NOONDAY CREEK MIDDLE SCHOOL", (120, 30, 40), (255, 240, 200), "arialbd.ttf"),
        ("LITTLE ACORNS PRESCHOOL", (250, 220, 60), (40, 100, 60), "arialbd.ttf"),
    ]),
    ("civic", [
        ("FIRE STATION 21", (170, 20, 20), (255, 255, 255), "arialbd.ttf"),
    ]),
    ("apartments", [
        ("THE PINES APARTMENTS", (40, 70, 50), (245, 240, 225), "georgiab.ttf"),
        ("CHASTAIN COMMONS VILLAS", (240, 236, 228), (60, 50, 40), "georgiab.ttf"),
        ("WILLOW BEND TOWNHOMES", (60, 60, 80), (240, 236, 228), "georgiab.ttf"),
    ]),
]


def font(name, px):
    for n in (name, "arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), px)
        except OSError: pass
    return ImageFont.load_default()


def row(text, bg, fg, fname):
    img = Image.new("RGB", (RW * S, RH * S), bg)
    d = ImageDraw.Draw(img)
    for k in range(5 * S):
        d.rectangle([k, k, RW * S - 1 - k, RH * S - 1 - k], outline=tuple(int(v * 0.78) for v in bg))
    px = int(RH * S * 0.62)
    while px > 10:
        f = font(fname, px)
        b = d.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= RW * S * 0.88 and b[3] - b[1] <= RH * S * 0.62: break
        px -= 4
    b = d.textbbox((0, 0), text, font=f)
    d.text(((RW * S - (b[2] - b[0])) // 2 - b[0], (RH * S - (b[3] - b[1])) // 2 - b[1]), text, font=f, fill=fg)
    return img.resize((RW, RH), Image.LANCZOS)


full = Image.new("RGB", (RW * 2, RH * PER_COL), (128, 128, 128))
r = 0
for kind, signs in KINDS:
    print(f"{kind}: rows {r} to {r + len(signs) - 1}")
    for t, bg, fg, fn in signs:
        full.paste(row(t, bg, fg, fn), ((r // PER_COL) * RW, (r % PER_COL) * RH))
        r += 1
assert r <= 2 * PER_COL
os.makedirs(os.path.dirname(OUT), exist_ok=True)
full.save(OUT)
print("wrote", OUT, r, "signs")
