"""Round 93 (C2, Dan: "on that race since there is a uturn lets make it more obvious"): the face of the race's U-turn board (a race-layer board that
stands on the outside of a route's U-turn in race time only, Race/RaceUTurns.cs): a black U-turn arrow on an orange board with a black border and
"U-TURN" under it, left and right. Hand-made content: writes Assets/Resources/RaceSigns/UTurnLeft.png and UTurnRight.png (512 x 384), which the game
only reads.

Usage: python Tools/handmade/make_uturn_sign.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/Resources/RaceSigns")
W, H = 512, 384
ORANGE, INK = (255, 128, 20), (14, 14, 14)


def font(size):
    for name in ("arialbd.ttf", "verdanab.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError: pass
    return ImageFont.load_default()


def uturn(left, path):
    img = Image.new("RGB", (W, H), ORANGE); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, W - 9, H - 9], outline=INK, width=16)
    # the arrow: up the stem on the near side, round the top, down the far side with the head pointing down (drawn for a left turn, mirrored for right)
    c = W / 2; r = 70; w = 30; top = 70; foot = 258
    xn, xf = (c + r, c - r) if left else (c - r, c + r)   # the near stem (you come up it) and the far one (you leave down it)
    d.rectangle([xn - w / 2, top + r, xn + w / 2, foot], fill=INK)
    d.arc([c - r - w / 2, top, c + r + w / 2, top + 2 * r + w], 180, 360, fill=INK, width=w)
    d.rectangle([xf - w / 2, top + r, xf + w / 2, foot - 46], fill=INK)
    d.polygon([(xf - 46, foot - 50), (xf + 46, foot - 50), (xf, foot + 6)], fill=INK)
    f = font(64); t = "U-TURN"; b = d.textbbox((0, 0), t, font=f)
    d.text(((W - (b[2] - b[0])) / 2 - b[0], 300 - (b[3] - b[1]) / 2 - b[1]), t, font=f, fill=INK)
    img.save(path); print("wrote", os.path.relpath(path, ROOT))


os.makedirs(OUT, exist_ok=True)
uturn(True, os.path.join(OUT, "UTurnLeft.png"))
uturn(False, os.path.join(OUT, "UTurnRight.png"))
