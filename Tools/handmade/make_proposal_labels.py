"""Round 23, Part B (the underground pipe network proposal), as Round 18, 20 and 21's (TEMPORARY, remove when Dan has chosen): the
faces of the floating labels on the proposal markers. Hand-made content: reads Data/proposals/round23_network.json and writes
Assets/HandMade/Proposals/ProposalLabels.png, one label per cell, in the order of the file's "marks" (MapBuilder.
Proposals.cs reads the cells in the same order).

A cell is 512 x 160 pixels and the board 6.4 m x 2.0 m, so the title's capitals are about 0.7 m tall and the line under
it about 0.4 m (readable at 20 to 30 m).

Usage: python Tools/handmade/make_proposal_labels.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA = [os.path.join(ROOT, "Data/proposals/round23_network.json"), os.path.join(ROOT, "Data/proposals/round24_creek_reverse.json")]   # Round 24 A3: both, in MapBuilder.Proposals.cs' order
OUT = os.path.join(ROOT, "Assets/HandMade/Proposals/ProposalLabels.png")
CW, CH, COLS = 512, 160, 4
S = 2   # drawn at twice the size, then reduced

# board and letter colors per kind (the post caps use the same board colors)
KINDS = {
    "grille": ((150, 20, 20), (255, 255, 255)),
    "mouth": ((245, 245, 245), (20, 20, 20)),
    "junction": ((140, 140, 153), (255, 255, 255)),
    "hub": ((240, 64, 216), (255, 255, 255)),
    "exit": ((34, 170, 68), (255, 255, 255)),
    "manhole": ((255, 122, 0), (255, 255, 255)),
    "outfall": ((26, 106, 208), (255, 255, 255)),
    "aquarium": ((84, 209, 255), (10, 30, 50)),
    "shaft": ((138, 43, 226), (255, 255, 255)),
    "flooded": ((59, 110, 158), (255, 255, 255)),
    "sign": ((255, 215, 0), (20, 20, 20)),
    "gate": ((208, 16, 16), (255, 255, 255)),
    "switch": ((255, 184, 51), (20, 20, 20)),
    "checkpoint": ((40, 150, 60), (255, 255, 255)),
    "start": ((15, 15, 15), (255, 255, 255)),
    "shortcut": ((255, 51, 204), (255, 255, 255)),   # Round 24 A3
    "jump": ((140, 20, 115), (255, 255, 255)),
    "info": ((90, 90, 95), (255, 255, 255)),
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
    marks = [mk for f in DATA if os.path.exists(f) for mk in json.load(open(f))["marks"]]
    rows = (len(marks) + COLS - 1) // COLS
    atlas = Image.new("RGB", (CW * COLS, CH * rows), (0, 0, 0))
    for i, mk in enumerate(marks):
        atlas.paste(cell(mk), ((i % COLS) * CW, (i // COLS) * CH))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    atlas.save(OUT)
    print(f"wrote {OUT}: {len(marks)} labels, {COLS} x {rows} cells")


if __name__ == "__main__":
    main()
