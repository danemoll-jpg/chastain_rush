"""Round 36 (Part A2): the stunt park's own signs, banners and painted boards (Dan: "The course should seem like someone specifically designed it
for the purpose of being a stunt course"): the park's name, the feature boards (BIG AIR, ROOF ACCESS, THE LOOP ...), chevron and checkered bands
in the park's colors. Hand-made content: writes Assets/HandMade/StuntPark/StuntSigns.png, one board per row, which the map builder
(MapBuilder.StuntPark.cs, its Sign* rows) only reads. Keep the row order in step with it. Every name is the park's own (no brand).

Each row is 2048 x 160 pixels (12.8 : 1), drawn at twice the size and reduced. Usage: python Tools/handmade/make_stunt_signs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/StuntPark/StuntSigns.png")
RW, RH, ROWS, S = 2048, 160, 24, 2
RED, YEL, BLK, WHT, BLU, ORG, TEAL, PURP, GRN = (214, 38, 32), (250, 200, 30), (18, 18, 20), (250, 250, 246), (30, 90, 200), (245, 120, 20), (20, 150, 150), (130, 50, 170), (40, 150, 60)

def font(name, size):
    for n in (name, "arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try: return ImageFont.truetype(n, size)
        except OSError: continue
    return ImageFont.load_default()

def chevrons(d, w, h, a, b, step=120, left=False):
    d.rectangle([0, 0, w, h], fill=a)
    for x in range(-h, w + h, step):
        pts = [(x, 0), (x + step // 2, 0), (x + step // 2 + h // 2, h // 2), (x + step // 2, h), (x, h), (x + h // 2, h // 2)]
        if left: pts = [(w - px, py) for px, py in pts]
        d.polygon(pts, fill=b)

def checker(d, w, h, n=2):
    c = h // n
    for i in range(0, w // c + 1):
        for j in range(n): d.rectangle([i * c, j * c, i * c + c, j * c + c], fill=BLK if (i + j) % 2 else WHT)

def board(text, bg, fg, fnt="impact.ttf", band=None, arrows=None):
    w, h = RW * S, RH * S; im = Image.new("RGB", (w, h), bg); d = ImageDraw.Draw(im)
    if band: d.rectangle([0, 0, w, h * 0.12], fill=band); d.rectangle([0, h * 0.88, w, h], fill=band)
    f = font(fnt, int(h * 0.62)); tw = d.textlength(text, font=f)
    while tw > w * (0.78 if arrows else 0.94): f = font(fnt, int(f.size * 0.94)); tw = d.textlength(text, font=f)
    d.text(((w - tw) / 2, h * 0.5), text, font=f, fill=fg, anchor="lm")
    if arrows:   # chevrons pointing the way at both ends
        for side in (0, 1):
            for k in range(3):
                x0 = (w * 0.02 + k * h * 0.28) if side == 0 else (w * 0.98 - k * h * 0.28)
                dx = h * 0.22 * (1 if arrows == ">" else -1)
                d.polygon([(x0, h * 0.2), (x0 + dx, h * 0.5), (x0, h * 0.8), (x0 + dx * 0.45, h * 0.5)], fill=fg)
    return im

ROWS_OUT = [
    board("CHASTAIN CORNER STUNT PARK", RED, YEL, band=YEL),                  # 0 the park's name (the sign at the entrance, banners)
    board("BIG AIR", BLU, WHT, band=YEL, arrows=">"),                          # 1 the mega launch off the supermarket's roof
    board("ROOF ACCESS", (60, 64, 70), YEL, band=YEL, arrows="<"),             # 2 the roof ramp
    board("SMASH IN!", ORG, BLK, band=BLK),                                    # 3 over the stores' windows
    board("THE LOOP", RED, WHT, band=WHT),                                     # 4
    board("BARREL ROLL", RED, WHT, band=YEL),                                  # 5
    board("HALF-PIPE", (190, 130, 60), WHT, band=BLK),                         # 6
    board("TOWER DROP", GRN, WHT, band=YEL),                                   # 7
    board("ROOF GAP", PURP, WHT, band=YEL),                                    # 8
    board("BUS JUMP", YEL, BLK, band=BLK),                                     # 9
    board("CAR GAP", (120, 70, 40), YEL, band=YEL),                            # 10
    board("START", WHT, BLK, band=BLK),                                        # 11 the start line's gantry
    None,                                                                      # 12 chevrons (yellow on black), the barrier boards
    None,                                                                      # 13 checkered band
    board("ALL AIR  ·  NO LIMITS", BLK, YEL, band=RED),                        # 14 a banner on the catch fence
    board("SHOP ROW DROP", TEAL, WHT, band=YEL),                               # 15
    board("DROP IN", ORG, WHT, band=BLK, arrows=">"),                          # 16 the inside: the mezzanine's drop
    board("UP TO THE ROOF", (60, 64, 70), YEL, band=YEL, arrows=">"),          # 17 the skylight ramp
    None,                                                                      # 18 chevrons (red on white)
]

def main():
    atlas = Image.new("RGB", (RW, 4096), (128, 128, 128))   # 4096 tall: the boards' UVs count 160 px rows in 4096 (as the store signs)
    for i, im in enumerate(ROWS_OUT):
        if im is None:
            w, h = RW * S, RH * S; im = Image.new("RGB", (w, h)); d = ImageDraw.Draw(im)
            if i == 12: chevrons(d, w, h, BLK, YEL)
            elif i == 13: checker(d, w, h)
            elif i == 18: chevrons(d, w, h, WHT, RED)
        atlas.paste(im.resize((RW, RH), Image.LANCZOS), (0, i * RH))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    atlas.save(OUT); print("wrote", OUT)

if __name__ == "__main__":
    main()
