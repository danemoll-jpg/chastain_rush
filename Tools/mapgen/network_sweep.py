"""Round 24 Part A3, item B0 (report only): every building footprint on the map (Data/map.json) against the terrain holes the
pipes and the network cut (holes.txt from ChastainRush.NetworkSweep.Run) and the ground the network re-graded (the twin
aprons of BF and GP, P1's approach). Lists every building within 3 m of a hole or graded patch, with its position.

    python Tools/mapgen/network_sweep.py <sweep dir>        (writes <sweep dir>/houses.txt)
"""
import json, math, os, sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def main():
    d = sys.argv[1]
    holes = np.loadtxt(os.path.join(d, "holes.txt")).reshape(-1, 2)
    m = json.load(open(os.path.join(ROOT, "Data", "map.json")))
    pipes = json.load(open(os.path.join(ROOT, "Data", "pipes", "pipes.json")))["pipes"]
    graded = []   # (name, polygon of the graded patch, as a rough box 14 m out from each graded mouth)
    for p in pipes:
        a, b = np.array(p["a"]), np.array(p["b"])
        u = (b - a) / np.linalg.norm(b - a)
        if p.get("twin"):
            graded.append((p["id"] + " apron", a - u * 10.0, 9.5))
        if p.get("approach_a"):
            q = p["approach_a"]
            for i in range(0, len(q), 2):
                graded.append((p["id"] + " approach", np.array(q[i:i + 2]), 7.5))
    out = []
    for i, bld in enumerate(m["buildings"]):
        f = np.array(bld["footprint"]).reshape(-1, 2)
        lo, hi = f.min(0) - 4, f.max(0) + 4
        sel = holes[(holes[:, 0] > lo[0]) & (holes[:, 0] < hi[0]) & (holes[:, 1] > lo[1]) & (holes[:, 1] < hi[1])]
        best = 1e9
        for h in sel:
            best = min(best, poly_dist(f, h))
        why = []
        if best < 3.0:
            why.append(f"hole {best:.1f} m away")
        for name, c, r in graded:
            dd = poly_dist(f, c) - r
            if dd < 3.0:
                why.append(f"{name} graded ground {max(dd, 0):.1f} m away")
        if why:
            c = f.mean(0)
            out.append(f"building {i} ({bld.get('type', '')}) at x {c[0]:.1f}, z {c[1]:.1f}: " + "; ".join(why))
    txt = f"Network sweep of {len(m['buildings'])} buildings against {len(holes)} hole cells and {len(graded)} graded patches\n"
    txt += "\n".join(out) + f"\n{len(out)} buildings found\n"
    open(os.path.join(d, "houses.txt"), "w").write(txt)
    print(txt)


def poly_dist(f, p):
    """0 inside the polygon, else the distance to its edge"""
    x, z = p
    inside = False
    n = len(f)
    best = 1e9
    for i in range(n):
        a, b = f[i], f[(i + 1) % n]
        if (a[1] > z) != (b[1] > z) and x < (b[0] - a[0]) * (z - a[1]) / (b[1] - a[1]) + a[0]:
            inside = not inside
        ab = b - a
        t = 0.0 if ab @ ab < 1e-9 else max(0.0, min(1.0, ((p - a) @ ab) / (ab @ ab)))
        best = min(best, float(np.linalg.norm(a + ab * t - p)))
    return 0.0 if inside else best


if __name__ == "__main__":
    main()
