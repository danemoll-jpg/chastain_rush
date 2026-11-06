"""0.75: tile renders into one labelled sheet.  python contact_sheet.py <out.png> <cols> <width> <img>...  (label = file stem)"""
import sys, pathlib
from PIL import Image, ImageDraw, ImageFont
out, cols, W = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); files = sys.argv[4:]
ims = [Image.open(f).convert('RGB') for f in files]; H = int(W * ims[0].height / ims[0].width); L = 26
rows = (len(ims) + cols - 1) // cols; sheet = Image.new('RGB', (W * cols, (H + L) * rows), (235, 237, 240)); d = ImageDraw.Draw(sheet)
try: font = ImageFont.truetype('arial.ttf', 18)
except Exception: font = ImageFont.load_default()
for i, (f, im) in enumerate(zip(files, ims)):
    x, y = (i % cols) * W, (i // cols) * (H + L); sheet.paste(im.resize((W, H)), (x, y + L)); d.text((x + 6, y + 4), pathlib.Path(f).stem, fill=(20, 20, 20), font=font)
sheet.save(out); print(out, sheet.size)
