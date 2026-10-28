"""Round 77 (C, Dan's BUG-006 x 1853.58 z -9077.33 Atlanta Street SE: "this should be a rail bridge"; BUG-007 x 1620.14 z -8259.97 downtown
Marietta: "rail line shouldn't run into the building. The rail should be quite significant ... still shouldn't stop right here"): the railroad
through the whole map from OSM (railway=rail), edge to edge. Round 76 laid only 650 m round the square from stage 3's download; this takes every
download (raw/osm_box.json, osm_ext1..3.json) and keeps the running lines: usage main or branch (the CSX W&A Subdivision, the Atlanta Terminal
Subdivision, the Georgia Northeastern) and their sidings (the second track beside the main); yards and industry spurs are left out.
Writes Data/tracks/railroad.json (world, not game layer), read by MapBuilder.Railroad.cs:
  lines       each running line clipped to the land: points every 2 m (x, z), per point 1 where OSM tags that stretch a bridge
  crossings   where a line crosses a road on the ground at grade: point, road index (map.json), the road's direction
  overpasses  where a line on a bridge passes over a road: point, road index (the deck must clear it)
  drop        map.json buildings whose footprint the line runs through (the building gives way; Marietta's landmarks are never dropped)
Re-run after mapgen (map.json's indices), then Build Map. Usage: python Tools/mapgen/make_railroad.py"""
import json
import math
import os

import numpy as np
from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data", "tracks", "railroad.json")
CACHES = ["raw/osm_box.json", "raw/osm_ext1.json", "raw/osm_ext2.json", "raw/osm_ext3.json"]
STEP, BED_HALF = 2.0, 3.0
NO_CROSS = {"footway", "path", "cycleway", "steps", "pedestrian", "track", "bridleway", "corridor"}


def main():
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    cfg = json.load(open(os.path.join(ROOT, "Tools", "mapgen", "config.json"), encoding="utf-8"))
    ox, oz = m["origin_utm"]
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    x0, z0, x1, z1 = m["extent_m"]
    rects = [(x0, z0, x1, z1)]
    pc = m.get("pieces") or []
    for k in range(0, len(pc), 4): rects.append(tuple(pc[k:k + 4]))
    on_land = lambda p: any(a <= p[0] <= c and b <= p[1] <= d for a, b, c, d in rects)
    nodes, ways = {}, {}
    for f in CACHES:
        path = os.path.join(ROOT, f)
        if not os.path.exists(path): continue
        for e in json.load(open(path, encoding="utf-8"))["elements"]:
            if e["type"] == "node" and e["id"] not in nodes:
                x, y = tr.transform(e["lon"], e["lat"]); nodes[e["id"]] = (x - ox, y - oz)
            elif e["type"] == "way":
                t = e.get("tags", {})
                if t.get("railway") != "rail": continue
                if not (t.get("usage") in ("main", "branch") or t.get("service") in ("siding", "crossover")): continue
                ways[e["id"]] = e
    # chains: ways joined end to end where exactly two of them meet
    ends = {}
    for w, e in ways.items():
        for n in (e["nodes"][0], e["nodes"][-1]): ends.setdefault(n, []).append(w)
    used, chains = set(), []
    for w0 in sorted(ways):
        if w0 in used: continue
        used.add(w0)
        seq = [(n, ways[w0].get("tags", {}).get("bridge") not in (None, "no")) for n in ways[w0]["nodes"]]
        for _ in range(2):
            while True:
                tail = seq[-1][0]
                nxt = [w for w in ends.get(tail, []) if w not in used]
                if len(ends.get(tail, [])) != 2 or len(nxt) != 1: break
                w = nxt[0]; used.add(w); ns = ways[w]["nodes"]
                if ns[0] != tail: ns = ns[::-1]
                br = ways[w].get("tags", {}).get("bridge") not in (None, "no")
                seq += [(n, br) for n in ns[1:]]
            seq.reverse()
            # a node's bridge flag belongs to the segment after it: re-attach after reversing
            seq = [(seq[i][0], seq[i + 1][1] if i + 1 < len(seq) else seq[i][1]) for i in range(len(seq))]
        name = next((ways[w].get("tags", {}).get("name") for w in ways if ways[w]["nodes"][0] == seq[0][0] and ways[w].get("tags", {}).get("name")), None)
        chains.append([(nodes[n], br) for n, br in seq if n in nodes])
    # densified, clipped to the land
    lines = []
    for ch in chains:
        run = []
        def flush():
            if len(run) >= 2:
                L = sum(math.dist(run[i][0], run[i + 1][0]) for i in range(len(run) - 1))
                if L >= 40: lines.append(run[:])
            run.clear()
        for i in range(len(ch) - 1):
            (a, br), (b, _) = ch[i], ch[i + 1]
            d = math.dist(a, b); n = max(1, int(math.ceil(d / STEP)))
            for k in range(n):
                p = (a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n)
                if on_land(p): run.append((p, br))
                else: flush()
        if on_land(ch[-1][0]): run.append((ch[-1][0], ch[-1][1]))
        flush()
    # roads: crossings at grade, and the roads a bridge passes over
    R = m["roads"]
    segs = []
    for i, r in enumerate(R):
        if r.get("tunnel") or r.get("bridge") or r["class"] in NO_CROSS: continue
        p = r["points"]
        for k in range(0, len(p) - 2, 2): segs.append((i, p[k], p[k + 1], p[k + 2], p[k + 3]))
    S = np.array([[s[1], s[2], s[3], s[4]] for s in segs]); SI = np.array([s[0] for s in segs])
    crossings, overpasses = [], []
    for li, ln in enumerate(lines):
        P = np.array([q for q, _ in ln]); B = [b for _, b in ln]
        for k in range(len(P) - 1):
            ax, az = P[k]; bx, bz = P[k + 1]
            mx0, mx1, mz0, mz1 = min(ax, bx), max(ax, bx), min(az, bz), max(az, bz)
            cand = np.nonzero((np.minimum(S[:, 0], S[:, 2]) <= mx1) & (np.maximum(S[:, 0], S[:, 2]) >= mx0) & (np.minimum(S[:, 1], S[:, 3]) <= mz1) & (np.maximum(S[:, 1], S[:, 3]) >= mz0))[0]
            for c in cand:
                cx, cz, dx, dz = S[c]
                rx, rz, qx, qz = bx - ax, bz - az, dx - cx, dz - cz
                den = rx * qz - rz * qx
                if abs(den) < 1e-9: continue
                u = ((cx - ax) * qz - (cz - az) * qx) / den; v = ((cx - ax) * rz - (cz - az) * rx) / den
                if not (0 <= u <= 1 and 0 <= v <= 1): continue
                pt = [round(ax + u * rx, 2), round(az + u * rz, 2)]
                hd = math.degrees(math.atan2(qx, qz)) % 360
                rec = {"line": li, "i": k, "x": pt[0], "z": pt[1], "road": int(SI[c]), "road_heading": round(hd, 1), "name": R[SI[c]]["name"], "class": R[SI[c]]["class"], "width": R[SI[c]]["width"]}
                (overpasses if B[k] else crossings).append(rec)
    # one crossing per road per place (OSM splits roads into pieces)
    def dedupe(lst):
        out = []
        for c in lst:
            if any(o["road"] == c["road"] and math.dist((o["x"], o["z"]), (c["x"], c["z"])) < 6 for o in out): continue
            out.append(c)
        return out
    crossings, overpasses = dedupe(crossings), dedupe(overpasses)
    # buildings the line runs through (Marietta Square's landmarks kept)
    keep = set()
    mp = os.path.join(ROOT, "Data", "tracks", "marietta_square.json")
    if os.path.exists(mp):
        md = json.load(open(mp, encoding="utf-8")); keep |= {v for v in md.get("landmarks", {}).values() if v is not None}
    allp = np.array([q for ln in lines for q, _ in ln])
    drop, kept_hits = [], []
    for k, b in enumerate(m["buildings"]):
        f = np.array(b["footprint"]).reshape(-1, 2)
        if allp.size == 0: break
        lo, hi = f.min(0) - BED_HALF, f.max(0) + BED_HALF
        near = allp[(allp[:, 0] >= lo[0]) & (allp[:, 0] <= hi[0]) & (allp[:, 1] >= lo[1]) & (allp[:, 1] <= hi[1])]
        if len(near) == 0: continue
        hit = False
        for p in near:
            ins = False
            for i in range(len(f)):
                (xa, ya), (xb, yb) = f[i], f[(i + 1) % len(f)]
                if (ya > p[1]) != (yb > p[1]) and p[0] < xa + (p[1] - ya) * (xb - xa) / (yb - ya): ins = not ins
            dmin = min(_seg(p, f[i], f[(i + 1) % len(f)]) for i in range(len(f)))
            if ins or dmin < BED_HALF: hit = True; break
        if hit:
            (kept_hits if k in keep else drop).append(k)
    out = {"note": "Round 77 (Tools/mapgen/make_railroad.py): the railroad's running lines from OSM, read by MapBuilder.Railroad.cs",
           "lines": [{"points": [round(v, 2) for q, _ in ln for v in q], "bridge": [1 if b else 0 for _, b in ln]} for ln in lines],
           "crossings": crossings, "overpasses": overpasses, "drop": drop, "kept": kept_hits}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, "w", encoding="utf-8"), indent=0)
    tot = sum(sum(math.dist(ln[i][0], ln[i + 1][0]) for i in range(len(ln) - 1)) for ln in lines)
    nb = sum(1 for ln in lines for i in range(len(ln) - 1) if ln[i][1])
    print(f"{len(ways)} OSM ways, {len(chains)} chains, {len(lines)} lines on the land, {tot / 1000:.1f} km ({nb * STEP:.0f} m on bridges); "
          f"{len(crossings)} level crossings, {len(overpasses)} roads under a rail bridge; {len(drop)} buildings give way {drop}; landmarks on the line kept: {kept_hits}")
    for c in overpasses: print(f"  over {c['name'] or c['class']} at ({c['x']:.0f}, {c['z']:.0f})")


def _seg(p, a, b):
    ab = b - a; L = float(ab @ ab)
    t = 0.0 if L == 0 else max(0.0, min(1.0, float((p - a) @ ab) / L))
    return float(np.hypot(*(a + ab * t - p)))


if __name__ == "__main__":
    main()
