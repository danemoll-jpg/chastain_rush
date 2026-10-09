"""Round 9 (BUG-005), report only: the overhead picture and list of the Chastain Glen houses whose walls are buried by more
than 0.5 m, from Data/screenshots/round9/buried_houses.json (written by Build Map, MapBuilder.Round9.cs). Writes
Data/screenshots/round9/buried_houses.png and buried_houses.txt. Nothing in the game changes.

Usage: python Tools/mapgen/buried_houses_plot.py
"""
import json, os
import numpy as np
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/screenshots/round9")
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
area = json.load(open(os.path.join(ROOT, "Data/neighborhood/chastain_glen_area.json")))
rows = json.load(open(os.path.join(OUT, "buried_houses.json")))["houses"]
by = {r["building"]: r for r in rows}

fig, ax = plt.subplots(figsize=(16, 13))
for i in area["roads"]:
    P = np.array(m["roads"][i]["points"]).reshape(-1, 2); ax.plot(P[:, 0], P[:, 1], "-", color="#bbb", lw=5, solid_capstyle="round")
for w in m["water"]:
    P = np.array(w["outline"]).reshape(-1, 2)
    if 2700 < P[:, 0].mean() < 3200 and 700 < P[:, 1].mean() < 1300: ax.fill(P[:, 0], P[:, 1], color="#9cd")
cmap = plt.get_cmap("Reds")
for i in area["houses"]:
    P = np.array(m["buildings"][i]["footprint"]).reshape(-1, 2)
    r = by.get(i)
    if r is None: ax.fill(P[:, 0], P[:, 1], color="#ddd"); continue
    if r["style"] == "Dan": continue   # Dan's own house is not marked on a committed picture
    col = "#2a7" if r["regraded"] else cmap(min(1.0, 0.25 + r["buried_m"] / 4.0))
    ax.fill(P[:, 0], P[:, 1], color=col, ec="k", lw=0.6)
    c = P.mean(0); ax.text(c[0], c[1] + 4, f"{i}\n{r['buried_m']:.1f} m" + ("\nFIXED" if r["regraded"] else ""), fontsize=7, ha="center")
ax.set_aspect("equal"); ax.grid(alpha=0.3)
ax.set_title("Chastain Glen houses with walls buried by more than 0.5 m (red, darker = deeper; green = BUG-005, regraded in Round 9). Grey: not buried.")
os.makedirs(OUT, exist_ok=True)
plt.savefig(os.path.join(OUT, "buried_houses.png"), dpi=90, bbox_inches="tight")
with open(os.path.join(OUT, "buried_houses.txt"), "w", encoding="utf-8") as f:
    f.write("Chastain Glen houses whose walls are buried by more than 0.5 m (ground above the floor level at the walls), before Round 9.\n")
    f.write("Report only: none of these was changed except the BUG-005 house (FIXED). Position = footprint centre, game coordinates.\n\n")
    for r in rows:
        if r["style"] == "Dan": f.write(f"  Dan's house: buried {r['buried_m']:.2f} m (not changed)\n"); continue
        f.write(f"  house {r['building']:5d} at x {r['x']:7.1f}  z {r['z']:7.1f}  style {r['style']}  buried {r['buried_m']:.2f} m" + (f"  -> FIXED (BUG-005), now {r['after_m']:.2f} m" if r["regraded"] else "") + "\n")
print(f"{len(rows)} rows; wrote {OUT}/buried_houses.png and .txt")
