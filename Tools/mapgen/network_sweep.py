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


def regraded():
    """Round 24 Part B (item 4): every building within 2 m of the ground the mouths moved (Assets/Generated/regraded_mouths.txt, written by
    the map build: the exits and BF set into banks, GP's berm). Prints them; none is the rule."""
    path = os.path.join(ROOT, "Assets", "Generated", "regraded_mouths.txt")
    rows = [l.split() for l in open(path) if l.strip()]
    pts = np.array([[float(r[0]), float(r[1])] for r in rows]); dy = np.array([float(r[2]) for r in rows]); who = [r[3] for r in rows]
    m = json.load(open(os.path.join(ROOT, "Data", "map.json")))
    found = 0
    for i, bld in enumerate(m["buildings"]):
        f = np.array(bld["footprint"]).reshape(-1, 2); lo, hi = f.min(0) - 3, f.max(0) + 3
        sel = np.where((pts[:, 0] > lo[0]) & (pts[:, 0] < hi[0]) & (pts[:, 1] > lo[1]) & (pts[:, 1] < hi[1]))[0]
        near = [(poly_dist(f, pts[k]), k) for k in sel]
        near = [(d, k) for d, k in near if d < 2.0]
        if near:
            found += 1; c = f.mean(0); k = max(near, key=lambda t: abs(dy[t[1]]))[1]
            print(f"building {i} at x {c[0]:.1f}, z {c[1]:.1f}: ground moved up to {dy[k]:+.2f} m within 2 m of it ({who[k]})")
    by = {}
    for w, d in zip(who, dy): by.setdefault(w, []).append(d)
    print(f"{len(rows)} ground samples moved by the mouths ({', '.join(f'{w} {min(v):+.1f} to {max(v):+.1f} m' for w, v in by.items())}); {found} buildings within 2 m")


if __name__ == "__main__":
    regraded() if "--regraded" in sys.argv else main()
