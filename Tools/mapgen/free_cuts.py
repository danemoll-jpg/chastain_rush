"""Round 20 follow-up (report only, changes nothing): free cuts through yards on the Chastain Glen loop.

Dan (2026-10-12), about his line across the west lobe: "there is literally nothing stopping you and since there is no cp, whats the
motivation of not taking this?" R1 is now that line, with risk. This scans both directions of the loop for other cuts like it: a way
off the lap and back onto it, across yards, that skips no checkpoint (and not the start / finish), and is shorter than the lap
between the same two points by SAVE_MIN metres or more.

The ground is a 1 m grid round the loop. Walls: the houses (0.6 m grown; the area's styled houses on their best-fit rectangles, as built), R1's solid fences (and, in reverse, the
ones that stand only during a reverse race), the race's road barriers (8 m across the road plus 2 m either side). Everything else is
open at the same cost per metre: lawns, drives, streets that leave the course past their barriers, ponds (slow in the game but open
here). Breakable fences, mailboxes, trees and slopes are ignored. So each cut is "possible on the map", not proven drivable at
speed: Dan drives the ones worth a look. The official shortcuts are not listed (they skip checkpoints or go through houses).

Writes local/round20/free_cuts.txt and local/round20/free_cuts.png (git-ignored: they show the houses); prints the list.
Usage: python Tools/mapgen/free_cuts.py
"""
import json, math, os
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import dijkstra

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
route = json.load(open(os.path.join(ROOT, "Data/routes/chastain_glen_loop.json"), encoding="utf-8"))
scf = json.load(open(os.path.join(ROOT, "Data/shortcuts/chastain_glen_loop.json"), encoding="utf-8"))
OUT_TXT = os.path.join(ROOT, "local/round20/free_cuts.txt"); OUT_PNG = os.path.join(ROOT, "local/round20/free_cuts.png")
SAVE_MIN, STEP_S, MAX_LAP = 25.0, 4.0, 420.0

loop = np.array(route["loop"], float).reshape(-1, 2)
seg = np.linalg.norm(np.diff(loop, axis=0), axis=1); CUM = np.concatenate([[0], np.cumsum(seg)]); LAP = CUM[-1]
def at(s):
    s %= LAP; k = min(max(int(np.searchsorted(CUM, s, side="right")) - 1, 0), len(seg) - 1); t = (s - CUM[k]) / seg[k]
    return loop[k] * (1 - t) + loop[k + 1] * t
def station(q):
    best = (1e9, 0.0)
    for k in range(len(seg)):
        a, b = loop[k], loop[k + 1]; ab = b - a; t = float(np.clip((q - a) @ ab / (ab @ ab), 0, 1)); d = float(np.linalg.norm(a + ab * t - q))
        if d < best[0]: best = (d, CUM[k] + t * seg[k])
    return best[1]
CPS = [(f"CP{c['id']}", station(np.array(c["pos"], float))) for c in route["checkpoints"]] + [("the start / finish", station(at(route["gate_s"])))]

x0, z0 = loop.min(0) - 60; x1, z1 = loop.max(0) + 60
NX, NZ = int(x1 - x0) + 1, int(z1 - z0) + 1
def cell(x, z): return int(round(z - z0)) * NX + int(round(x - x0))
XX, ZZ = np.meshgrid(np.arange(NX) + x0, np.arange(NZ) + z0)
P = np.stack([XX.ravel(), ZZ.ravel()], 1)

def wall_poly(poly, grow):
    """cells inside a polygon grown by 'grow' (point in polygon, or within grow of an edge)"""
    poly = np.asarray(poly, float); lo = poly.min(0) - grow - 1; hi = poly.max(0) + grow + 1
    sel = np.where((P[:, 0] >= lo[0]) & (P[:, 0] <= hi[0]) & (P[:, 1] >= lo[1]) & (P[:, 1] <= hi[1]))[0]
    q = P[sel]; inside = np.zeros(len(q), bool); dmin = np.full(len(q), 1e9)
    for i in range(len(poly)):
        a, b = poly[i], poly[(i + 1) % len(poly)]
        cond = ((a[1] > q[:, 1]) != (b[1] > q[:, 1])) & (q[:, 0] < (b[0] - a[0]) * (q[:, 1] - a[1]) / (b[1] - a[1] + 1e-12) + a[0]); inside ^= cond
        ab = b - a; t = np.clip(((q - a) @ ab) / (ab @ ab), 0, 1); dmin = np.minimum(dmin, np.linalg.norm(a + t[:, None] * ab - q, axis=1))
    return sel[inside | (dmin < grow)]
def wall_seg(a, b, half):
    a, b = np.asarray(a, float), np.asarray(b, float); lo = np.minimum(a, b) - half - 1; hi = np.maximum(a, b) + half + 1
    sel = np.where((P[:, 0] >= lo[0]) & (P[:, 0] <= hi[0]) & (P[:, 1] >= lo[1]) & (P[:, 1] <= hi[1]))[0]
    q = P[sel]; ab = b - a; t = np.clip(((q - a) @ ab) / (ab @ ab), 0, 1)
    return sel[np.linalg.norm(a + t[:, None] * ab - q, axis=1) < half]

AREA_HOUSES = set(json.load(open(os.path.join(ROOT, "Data/neighborhood/chastain_glen_area.json")))["houses"])
def best_rect(f):
    """the best-fit rectangle (as MapBuilder.BestRectangle): the area's styled houses are built on it, not on the raw footprint"""
    best = None
    for k in range(len(f)):
        d = f[(k + 1) % len(f)] - f[k]; n = np.linalg.norm(d)
        if n < 1e-3: continue
        d = d / n; p = np.array([-d[1], d[0]]); u = f @ d; v = f @ p; a = (u.max() - u.min()) * (v.max() - v.min())
        if best is None or a < best[0]: best = (a, d, p, u.min(), u.max(), v.min(), v.max())
    _, d, p, u0, u1, v0, v1 = best
    return np.array([d * u0 + p * v0, d * u1 + p * v0, d * u1 + p * v1, d * u0 + p * v1])
def blocked(reverse):
    w = np.zeros(NX * NZ, bool)
    for bi, b in enumerate(m["buildings"]):
        f = np.array(b["footprint"], float).reshape(-1, 2); c = f.mean(0)
        if bi in AREA_HOUSES: f = best_rect(f)
        if x0 - 30 < c[0] < x1 + 30 and z0 - 30 < c[1] < z1 + 30: w[wall_poly(f, 0.6)] = True
    for sc in scf["shortcuts"]:
        for fe in sc.get("fences", []):
            if fe["kind"] == "solid" or (reverse and fe["kind"] == "reverse_only"):
                a, b = np.array(fe["a"]), np.array(fe["b"])
                cells = wall_seg(a, b, 0.75)
                if "gate" in fe:   # the gate's opening stays open
                    c = np.array(fe["gate"]["c"]); cells = cells[np.linalg.norm(P[cells] - c, axis=1) > fe["gate"]["w"] / 2]
                w[cells] = True
    for c in route.get("closures", []):
        d = np.array([c["dx"], c["dz"]]); n = np.array([-d[1], d[0]]); ctr = np.array([c["x"], c["z"]]); half = c.get("width_m", 8.0) / 2 + 2.0
        w[wall_seg(ctr - n * half, ctr + n * half, 0.75)] = True
    return w

def graph(w):
    idx = np.arange(NX * NZ).reshape(NZ, NX); rows, cols, data = [], [], []
    for dz, dx, cst in ((0, 1, 1.0), (1, 0, 1.0), (1, 1, math.sqrt(2)), (1, -1, math.sqrt(2))):
        a = idx[max(0, -dz):NZ - max(0, dz), max(0, -dx):NX - max(0, dx)].ravel(); b = a + dz * NX + dx
        ok = ~w[a] & ~w[b]; rows += [a[ok], b[ok]]; cols += [b[ok], a[ok]]; data += [np.full(ok.sum(), cst)] * 2
    return coo_matrix((np.concatenate(data), (np.concatenate(rows), np.concatenate(cols))), shape=(NX * NZ, NX * NZ)).tocsr()

def scan(reverse):
    w = blocked(reverse); G = graph(w)
    ss = np.arange(0, LAP, STEP_S); src = [cell(*at(s)) for s in ss]
    for k in src: w[k] = False
    found = []
    for b0 in range(0, len(src), 25):
        D, pred = dijkstra(G, indices=src[b0:b0 + 25], return_predecessors=True)
        for i in range(D.shape[0]):
            sa = ss[b0 + i]
            for j, sb in enumerate(ss):
                lapd = ((sa - sb) if reverse else (sb - sa)) % LAP
                if lapd < SAVE_MIN + 10 or lapd > MAX_LAP: continue
                g = D[i, src[j]]
                if not np.isfinite(g) or lapd - g < SAVE_MIN: continue
                lo_, hi_ = (sb, sa) if reverse else (sa, sb)
                skipped = [nm for nm, s in CPS if ((s - lo_) % LAP) < ((hi_ - lo_) % LAP)]
                if skipped: continue
                found.append((lapd - g, sa, sb, g, lapd, pred[i], src[j]))
    # one per cluster (start and end within 30 m of a better one)
    found.sort(key=lambda r: -r[0]); keep = []
    for r in found:
        if all(min(abs(r[1] - k[1]), LAP - abs(r[1] - k[1])) > 30 or min(abs(r[2] - k[2]), LAP - abs(r[2] - k[2])) > 30 for k in keep): keep.append(r)
    out = []
    for save, sa, sb, g, lapd, pr, tgt in keep:
        path = []; k = tgt
        while k >= 0 and len(path) < 5000: path.append(P[k]); k = pr[k]
        out.append({"save": save, "from": sa, "to": sb, "line": g, "lap": lapd, "path": np.array(path[::-1])})
    return out

def main():
    os.makedirs(os.path.dirname(OUT_TXT), exist_ok=True)
    lines = [f"Free cuts on the Chastain Glen loop (Tools/mapgen/free_cuts.py): off the lap and back on across yards, skipping no checkpoint, {SAVE_MIN:.0f} m or more shorter than the lap.",
             "Stations are meters along the loop file from its first point (forward). 'possible on the map', not driven: slopes, trees, breakable fences ignored.", ""]
    res = {}
    for reverse in (False, True):
        cuts = scan(reverse); res[reverse] = cuts
        lines.append(f"{'REVERSE' if reverse else 'FORWARD'}: {len(cuts)} cut(s)")
        for i, c in enumerate(cuts, 1):
            a, b = at(c["from"]), at(c["to"])
            lines.append(f"  {'R' if reverse else 'F'}{i}: off at station {c['from']:.0f} ({a[0]:.0f}, {a[1]:.0f}), back on at {c['to']:.0f} ({b[0]:.0f}, {b[1]:.0f}): {c['line']:.0f} m across against {c['lap']:.0f} m of lap, saves {c['save']:.0f} m")
        lines.append("")
    open(OUT_TXT, "w", encoding="utf-8").write("\n".join(lines)); print("\n".join(lines))
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, axs = plt.subplots(1, 2, figsize=(22, 10))
    for ax, reverse in zip(axs, (False, True)):
        for b in m["buildings"]:
            f = np.array(b["footprint"]).reshape(-1, 2); c = f.mean(0)
            if x0 < c[0] < x1 and z0 < c[1] < z1: ax.fill(f[:, 0], f[:, 1], color="#a76")
        ax.plot(loop[:, 0], loop[:, 1], "-", color="gray", lw=4, alpha=0.5)
        for sc in scf["shortcuts"]:
            for fe in sc.get("fences", []):
                if fe["kind"] == "solid" or reverse: ax.plot([fe["a"][0], fe["b"][0]], [fe["a"][1], fe["b"][1]], "k-", lw=2)
        for nm, s in CPS: q = at(s); ax.plot(*q, "g^"); ax.text(q[0] + 3, q[1] + 3, nm, color="g", fontsize=8)
        for i, c in enumerate(res[reverse], 1):
            ax.plot(c["path"][:, 0], c["path"][:, 1], "r-", lw=1.5); q = c["path"][len(c["path"]) // 2]; ax.text(q[0], q[1], f"{'R' if reverse else 'F'}{i} {c['save']:.0f} m", color="r", fontsize=9, weight="bold")
        ax.set_aspect("equal"); ax.set_title(("Reverse" if reverse else "Forward") + ": free cuts (red), checkpoints (green), R1's fences (black)")
    fig.savefig(OUT_PNG, dpi=80, bbox_inches="tight"); print("wrote", OUT_TXT, "and", OUT_PNG)

if __name__ == "__main__":
    main()
