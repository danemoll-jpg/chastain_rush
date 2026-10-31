"""Round 87: a top-down picture of a course's HIGH MODE bumpers and closure barriers around a place, from the exe check's layout dump.

python Tools/race/plot_layout.py <87_layout_*.txt> <x> <z> [radius] [out.png] [--heading deg]
Lines (the lap, shortcuts) in blue with their paved width, bumper runs in red (ends marked), barrier blocks in black, jumps in green.
"""
import math
import sys

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    path, x0, z0 = args[0], float(args[1]), float(args[2])
    r = float(args[3]) if len(args) > 3 else 60.0
    out = args[4] if len(args) > 4 else path.replace(".txt", f"_{x0:.0f}_{z0:.0f}.png")
    heading = None
    if "--heading" in sys.argv:
        heading = float(sys.argv[sys.argv.index("--heading") + 1])
    fig, ax = plt.subplots(figsize=(8, 8))
    near = lambda x, z: abs(x - x0) < r * 1.5 and abs(z - z0) < r * 1.5
    for row in open(path, encoding="utf-8"):
        parts = row.split()
        if not parts:
            continue
        kind = parts[0]
        if kind == "line":
            pts = [tuple(float(v) for v in p.split(",")) for p in parts[1:]]
            pts = [p for p in pts if near(p[0], p[1])]
            if pts:
                ax.plot([p[0] for p in pts], [p[1] for p in pts], ".", color="#3a6fd8", ms=2)
                for x, z, h in pts[::2]:
                    ax.add_patch(plt.Circle((x, z), h, color="#c8d8f8", alpha=.25, lw=0))
        elif kind == "bump":
            pts = [tuple(float(v) for v in p.split(",")) for p in parts[1:]]
            if not any(near(*p) for p in pts):
                continue
            ax.plot([p[0] for p in pts], [p[1] for p in pts], "-", color="#d23", lw=2)
            ax.plot([pts[0][0], pts[-1][0]], [pts[0][1], pts[-1][1]], "o", color="#d23", ms=4)
        elif kind == "block":
            x, z, rx, rz, w = (float(v) for v in parts[2].split(","))
            if near(x, z):
                ax.plot([x - rx * w / 2, x + rx * w / 2], [z - rz * w / 2, z + rz * w / 2], "-", color="k", lw=3)
        elif kind == "jump":
            a = [float(v) for v in parts[2].split(",")]
            b = [float(v) for v in parts[3].split(",")]
            if near(*a) or near(*b):
                ax.annotate("", xy=b, xytext=a, arrowprops=dict(arrowstyle="->", color="g"))
    ax.plot([x0], [z0], "*", color="orange", ms=14)
    if heading is not None:
        hx, hz = math.sin(math.radians(heading)), math.cos(math.radians(heading))
        ax.annotate("", xy=(x0 + hx * 10, z0 + hz * 10), xytext=(x0, z0), arrowprops=dict(arrowstyle="->", color="orange", lw=2))
    ax.set_xlim(x0 - r, x0 + r)
    ax.set_ylim(z0 - r, z0 + r)
    ax.set_aspect("equal")
    ax.set_title(f"{path.split('/')[-1].split(chr(92))[-1]} at ({x0:.0f}, {z0:.0f})", fontsize=8)
    fig.savefig(out, dpi=90)
    print(out)


if __name__ == "__main__":
    main()
