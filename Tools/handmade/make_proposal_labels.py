"""Round 21 Part C (the reverse Noonday Creek candidates), as Round 20 item 4 and Round 18 items 2b and 2c (TEMPORARY, remove when Dan has chosen): the faces of the floating labels on the proposal
markers. Hand-made content: reads Data/proposals/round21_markers.json and writes
Assets/HandMade/Proposals/ProposalLabels.png, one label per cell, in the order of the file's "marks" (MapBuilder.
Proposals.cs reads the cells in the same order).

A cell is 512 x 160 pixels and the board 4.8 m x 1.5 m, so the title's capitals are about 0.55 m tall and the line under
it about 0.3 m (readable at 20 to 30 m).

Usage: python Tools/handmade/make_proposal_labels.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA = os.path.join(ROOT, "Data/proposals/round21_markers.json")
OUT = os.path.join(ROOT, "Assets/HandMade/Proposals/ProposalLabels.png")
CW, CH, COLS = 512, 160, 4
S = 2   # drawn at twice the size, then reduced

# board and letter colors per kind (the post caps use the same board colors)
KINDS = {
    "start": ((245, 245, 245), (20, 20, 20)),
    "checkpoint": ((40, 150, 60), (255, 255, 255)),
    "jump": ((200, 40, 40), (255, 255, 255)),
    "crossing": ((40, 90, 210), (255, 255, 255)),
    "pipe": ((130, 50, 220), (255, 255, 255)),
    "info": ((90, 90, 95), (255, 255, 255)),
    "shortcut": ((255, 64, 217), (255, 255, 255)),
}


def font(name, px):
    for n in (name, "arialbd.ttf"):
        try: return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), px)
        except OSError: pass
    return ImageFont.load_default()


def fit(d, text, name, px, width):
    while px > 10:
        f = font(name, px)
        b = d.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= width: return f, b
        px -= 2
    return font(name, px), d.textbbox((0, 0), text, font=font(name, px))


def cell(mark):
    bg, fg = KINDS.get(mark["kind"], KINDS["info"])
    img = Image.new("RGB", (CW * S, CH * S), bg)
    d = ImageDraw.Draw(img)
    for k in range(5 * S):
        d.rectangle([k, k, CW * S - 1 - k, CH * S - 1 - k], outline=tuple(int(v * 0.7) for v in bg))
    f, b = fit(d, mark["title"], "arialbd.ttf", 74 * S, int(CW * S * 0.92))
    d.text(((CW * S - (b[2] - b[0])) / 2 - b[0], 14 * S - b[1]), mark["title"], font=f, fill=fg)
    f, b = fit(d, mark["text"], "arial.ttf", 40 * S, int(CW * S * 0.92))
    d.text(((CW * S - (b[2] - b[0])) / 2 - b[0], 104 * S - b[1]), mark["text"], font=f, fill=fg)
    return img.resize((CW, CH), Image.LANCZOS)


def main():
    marks = json.load(open(DATA))["marks"]
    rows = (len(marks) + COLS - 1) // COLS
    atlas = Image.new("RGB", (CW * COLS, CH * rows), (0, 0, 0))
    for i, mk in enumerate(marks):
        atlas.paste(cell(mk), ((i % COLS) * CW, (i // COLS) * CH))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    atlas.save(OUT)
    print(f"wrote {OUT}: {len(marks)} labels, {COLS} x {rows} cells")


if __name__ == "__main__":
    main()
