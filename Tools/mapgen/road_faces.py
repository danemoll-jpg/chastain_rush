"""Round 77 (A, "scan the whole of stage 3's new roads and interchanges for the same (jolt and face scans as Rounds 61 / 75) ... list it"): steep faces
in the built ground beside the roads of a rectangle (report only), from a regular grid of the built terrain (ChastainRush.GroundGrid.Run -gridRects
"piece:x0,z0,x1,z1,2"). A face: a rise of over FACE_M between neighbouring cells 2 m apart (over 50 degrees) within NEAR_M of a road's edge, not
inside a building (+2 m) or under a bridge deck; faces within 25 m are one spot. Prints the spots, worst first, with the road beside each.
Usage: python Tools/mapgen/road_faces.py <grid.csv> [out.txt]"""
import csv
import json
import math
import os
import sys

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FACE_M, NEAR_M = 2.4, 25.0


def main():
    rows = list(csv.DictReader(open(sys.argv[1])))
    xs = sorted({float(r["x"]) for r in rows}); zs = sorted({float(r["z"]) for r in rows})
    ix = {x: i for i, x in enumerate(xs)}; iz = {z: i for i, z in enumerate(zs)}
    H = np.full((len(zs), len(xs)), np.nan)
    for r in rows:
        try: H[iz[float(r["z"])], ix[float(r["x"])]] = float(r["terrain"])
        except ValueError: pass
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    X, Z = np.meshgrid(np.array(xs), np.array(zs))
    x0, x1, z0, z1 = xs[0] - 30, xs[-1] + 30, zs[0] - 30, zs[-1] + 30
    near = np.full(X.shape, np.inf); name = np.full(X.shape, -1)
    roads = [r for r in m["roads"] if not r.get("tunnel")]
    for k, r in enumerate(roads):
        p = np.array(r["points"]).reshape(-1, 2)
        if not ((p[:, 0] > x0) & (p[:, 0] < x1) & (p[:, 1] > z0) & (p[:, 1] < z1)).any(): continue
        for a, b in zip(p[:-1], p[1:]):
            ab = b - a; L2 = max(float(ab @ ab), 1e-9)
            lo = np.minimum(a, b) - NEAR_M - 10; hi = np.maximum(a, b) + NEAR_M + 10
            sx = slice(max(0, int((lo[0] - xs[0]) / 2)), min(len(xs), int((hi[0] - xs[0]) / 2) + 1)); sz = slice(max(0, int((lo[1] - zs[0]) / 2)), min(len(zs), int((hi[1] - zs[0]) / 2) + 1))
            qx, qz = X[sz, sx], Z[sz, sx]
            t = np.clip(((qx - a[0]) * ab[0] + (qz - a[1]) * ab[1]) / L2, 0, 1)
            d = np.hypot(a[0] + ab[0] * t - qx, a[1] + ab[1] * t - qz) - r["width"] / 2
            better = d < near[sz, sx]; near[sz, sx] = np.where(better, d, near[sz, sx]); name[sz, sx] = np.where(better, k, name[sz, sx])
    excl = np.zeros(X.shape, bool)
    for b in m["bridges"]:
        p = np.array(b["points"]).reshape(-1, 2)
        if not ((p[:, 0] > x0) & (p[:, 0] < x1) & (p[:, 1] > z0) & (p[:, 1] < z1)).any(): continue
        for a, c in zip(p[:-1], p[1:]):
            ab = c - a; L2 = max(float(ab @ ab), 1e-9); t = np.clip(((X - a[0]) * ab[0] + (Z - a[1]) * ab[1]) / L2, 0, 1)
            excl |= np.hypot(a[0] + ab[0] * t - X, a[1] + ab[1] * t - Z) < b["width"] / 2 + 6
    for bd in m["buildings"]:
        f = np.array(bd["footprint"]).reshape(-1, 2)
        if f[:, 0].max() < x0 or f[:, 0].min() > x1 or f[:, 1].max() < z0 or f[:, 1].min() > z1: continue
        sx = slice(max(0, int((f[:, 0].min() - 2 - xs[0]) / 2)), min(len(xs), int((f[:, 0].max() + 2 - xs[0]) / 2) + 2)); sz = slice(max(0, int((f[:, 1].min() - 2 - zs[0]) / 2)), min(len(zs), int((f[:, 1].max() + 2 - zs[0]) / 2) + 2))
        excl[sz, sx] = True
    dx = np.abs(np.diff(H, axis=1)); dz = np.abs(np.diff(H, axis=0))
    face = np.zeros(X.shape); face[:, :-1] = np.fmax(face[:, :-1], np.nan_to_num(dx)); face[:-1, :] = np.fmax(face[:-1, :], np.nan_to_num(dz))
    hit = (face > FACE_M) & (near < NEAR_M) & ~excl
    vals = sorted(((face[a, b], X[a, b], Z[a, b], near[a, b], name[a, b]) for a, b in np.argwhere(hit)), reverse=True)
    spots = []
    for v, x, z, d, k in vals:
        if any(math.hypot(x - s[1], z - s[2]) < 25 for s in spots): continue
        spots.append((v, x, z, d, k))
    lines = [f"Round 77 (A): faces over {FACE_M} m in 2 m (over 50 degrees) within {NEAR_M:.0f} m of a road's edge, from {sys.argv[1]}: {len(spots)} spots ({hit.sum()} cells)"]
    for v, x, z, d, k in spots:
        r = roads[k] if k >= 0 else {"name": "?", "class": "?"}
        lines.append(f"  {v:5.1f} m at ({x:.0f}, {z:.0f}), {d:4.1f} m off {r['name'] or '(unnamed)'} [{r['class']}]")
    print("\n".join(lines[:60]))
    if len(sys.argv) > 2: open(sys.argv[2], "w").write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
