"""Round 62 (the Noonday Creek Trail race): the faces of its new signs. Hand-made content (made-up wording, no real brand), which the map builder
(MapBuilder.TrailRace.cs, MapBuilder.MallRace.cs) only reads:
  Assets/HandMade/RoadSigns/WindingRoad.png   the trail's yellow winding-road warning sign (Dan's photos 1 and 2): a black winding arrow on a yellow
                                              diamond with a black border (512 x 512; the diamond's corners touch the middles of the square's edges)
  Assets/HandMade/Mall/RaffleBoard.png        the car raffle stage's board at the top of the east escalator (1024 x 256)
  Assets/HandMade/Mall/MattressSale.png       the mattress mountain's banner in Halverson's aisle (1024 x 256)

Usage: python Tools/handmade/make_trail_signs.py
"""
import math, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
YELLOW, INK = (250, 196, 18), (14, 14, 14)

def font(name, size):
    for n in (name, "arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), size)
        except OSError: pass
    return ImageFont.load_default()

def winding(path):
    N, S = 512, 4; img = Image.new("RGB", (N * S, N * S), YELLOW); d = ImageDraw.Draw(img)
    c = N * S / 2; r = N * S / 2 - 6 * S; inset = 26 * S
    d.polygon([(c, c - r + inset), (c + r - inset, c), (c, c + r - inset), (c - r + inset, c)], outline=INK, width=16 * S)
    # the winding arrow: up from the bottom, three bends, the head at the top
    pts = []
    for k in range(601):
        t = k / 600.0; y = c + 120 * S - t * 220 * S; x = c + 34 * S * math.sin(t * math.pi * 3.0)
        pts.append((x, y))
    for x, y in pts: d.ellipse([x - 13 * S, y - 13 * S, x + 13 * S, y + 13 * S], fill=INK)   # a round pen (a thick line frays at its joints)
    tx, ty = pts[-1]; ang = math.atan2(pts[-1][1] - pts[-25][1], pts[-1][0] - pts[-25][0])
    def rot(dx, dy): return (tx + dx * math.cos(ang) - dy * math.sin(ang), ty + dx * math.sin(ang) + dy * math.cos(ang))
    d.polygon([rot(52 * S, 0), rot(-6 * S, -46 * S), rot(-6 * S, 46 * S)], fill=INK)
    img.resize((N, N), Image.LANCZOS).save(path); print("wrote", os.path.relpath(path, ROOT))

def board(path, lines, bg, fg, border):
    W, H, S = 1024, 256, 2; img = Image.new("RGB", (W * S, H * S), bg); d = ImageDraw.Draw(img)
    for k in range(10 * S): d.rectangle([k, k, W * S - 1 - k, H * S - 1 - k], outline=border)
    y = H * S * 0.5 - sum(sz for _, sz, _ in lines) * S * 0.5
    for text, size, fname in lines:
        f = font(fname, size * S); b = d.textbbox((0, 0), text, font=f)
        while b[2] - b[0] > W * S * 0.9: size -= 4; f = font(fname, size * S); b = d.textbbox((0, 0), text, font=f)
        d.text(((W * S - (b[2] - b[0])) / 2 - b[0], y - b[1]), text, font=f, fill=fg); y += size * S
    img.resize((W, H), Image.LANCZOS).save(path); print("wrote", os.path.relpath(path, ROOT))

os.makedirs(os.path.join(ROOT, "Assets/HandMade/RoadSigns"), exist_ok=True); os.makedirs(os.path.join(ROOT, "Assets/HandMade/Mall"), exist_ok=True)
winding(os.path.join(ROOT, "Assets/HandMade/RoadSigns/WindingRoad.png"))
board(os.path.join(ROOT, "Assets/HandMade/Mall/RaffleBoard.png"), [("WIN A NEW CAR!", 120, "impact.ttf"), ("ENTER TODAY  \u00b7  DRAWING SATURDAY", 52, "arialbd.ttf")],
      (196, 24, 32), (255, 240, 120), (250, 214, 60))
board(os.path.join(ROOT, "Assets/HandMade/Mall/MattressSale.png"), [("MATTRESS MOUNTAIN", 112, "impact.ttf"), ("SALE  \u00b7  EVERY SIZE 40% OFF", 56, "arialbd.ttf")],
      (28, 62, 132), (255, 255, 255), (120, 170, 230))
