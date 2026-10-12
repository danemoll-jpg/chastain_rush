"""Round 22: the green windscreen banner on the Chastain Enclave tennis court fence, after Dan's pool photo
(local/reference_photos/enclave/, never copied): dark green with "Chastain Enclave" in white. Hand-made content: writes
Assets/HandMade/Enclave/EnclaveWindscreen.png, which the map builder only reads. The quad it goes on is 6.0 x 1.5 m, so at
1024 x 256 pixels one metre is about 170 pixels; the letters are about 0.45 m tall (readable at 20 to 30 m).

Usage: python Tools/handmade/make_windscreen_sign.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/HandMade/Enclave/EnclaveWindscreen.png")
W, H, S = 1024, 256, 2
GREEN, WHITE = (28, 74, 46, 255), (240, 240, 236, 255)

img = Image.new("RGBA", (W * S, H * S), GREEN)
d = ImageDraw.Draw(img)
d.rectangle([10 * S, 10 * S, (W - 10) * S, (H - 10) * S], outline=WHITE, width=4 * S)
font = None
for name in ("georgiab.ttf", "BOOKOSB.TTF", "timesbd.ttf", "cambriab.ttf"):
    try: font = ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), 96 * S); break
    except OSError: pass
font = font or ImageFont.load_default()
text = "Chastain Enclave"
box = d.textbbox((0, 0), text, font=font)
tw, th = box[2] - box[0], box[3] - box[1]
d.text(((W * S - tw) // 2 - box[0], (H * S - th) // 2 - box[1]), text, font=font, fill=WHITE)
img = img.resize((W, H), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print("wrote", OUT)
