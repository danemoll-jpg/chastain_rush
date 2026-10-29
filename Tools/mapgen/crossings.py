"""Round 82 A: every place on the map where two roads cross without sharing an OSM node (report and data).

Reads Data/map.json (roads on the ground, bridges with their deck heights) and the raw OSM downloads (for layer / bridge / tunnel tags),
finds every pair of road pieces whose center lines cross away from a shared node, and decides which one goes over:
  1. OSM's own tags: a bridge or a higher layer is over, a tunnel or a lower layer is under;
  2. the Northwest Corridor Express Lanes go over a road they cross (Dan, 2026-10-29 01:36: "the express lanes are usually bridges when they cross 75/575");
  3. otherwise the road whose ground is higher there goes over (less earth moved), the higher class breaking a tie.
Roads that share a node (a merge, a split, a junction) are joining, never crossing: a crossing within 60 m of a node the two share is left out.
Writes Data/tracks/crossings.json (read by MapBuilder.Round82.cs) and prints the list.
Usage: python Tools/mapgen/crossings.py
"""
import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EXPRESS = "Northwest Corridor Express Lanes"
RANK = {"motorway": 9, "trunk": 8, "motorway_link": 7, "primary": 6, "trunk_link": 6, "primary_link": 5, "secondary": 5, "secondary_link": 4,
        "tertiary": 4, "tertiary_link": 3, "unclassified": 2, "residential": 2, "living_street": 1, "service": 1}
JOIN_M = 60.0


def load_tags():
    tags = {}
    for f in ("osm_box.json", "osm_ext1.json", "osm_ext2.json", "osm_ext3.json"):
        p = os.path.join(ROOT, "raw", f)
        if not os.path.exists(p):
            continue
        with open(p, encoding="utf-8") as fh:
            d = json.load(fh)
        for e in d.get("elements", []):
            if e.get("type") == "way":
                tags.setdefault(e["id"], e.get("tags", {}))
    return tags


def seg_x(a, b, c, d):
    rx, ry = b[0] - a[0], b[1] - a[1]
    sx, sy = d[0] - c[0], d[1] - c[1]
    den = rx * sy - ry * sx
    if abs(den) < 1e-9:
        return None
    cax, cay = c[0] - a[0], c[1] - a[1]
    ta = (cax * sy - cay * sx) / den
    tb = (cax * ry - cay * rx) / den
    if 1e-4 < ta < 1 - 1e-4 and 1e-4 < tb < 1 - 1e-4:
        return ta, tb
    return None


def near_line(p, q):
    best = 1e18
    for i in range(len(q) - 1):
        a, b = q[i], q[i + 1]
        dx, dz = b[0] - a[0], b[1] - a[1]
        t = max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / max(dx * dx + dz * dz, 1e-9)))
        best = min(best, math.hypot(a[0] + dx * t - p[0], a[1] + dz * t - p[1]))
    return best


def main():
    with open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8") as fh:
        m = json.load(fh)
    tags = load_tags()
    pieces = []   # (kind 'road'/'bridge', index, osm_id, name, class, width, points [(x,z)], deck_y or None)
    for i, r in enumerate(m["roads"]):
        p = r["points"]
        pieces.append(("road", i, r["osm_id"], r.get("name", ""), r["class"], r["width"], [(p[k], p[k + 1]) for k in range(0, len(p), 2)], None, r.get("tunnel", False)))
    for i, b in enumerate(m.get("bridges", [])):
        p = b["points"]
        pieces.append(("bridge", i, b["osm_id"], b.get("name", ""), b["class"], b["width"], [(p[k], p[k + 1]) for k in range(0, len(p), 2)], b.get("deck_y"), False))
    # the nodes of each osm way (rounded points), for "shares a node"
    nodes_of = {}
    for pc in pieces:
        s = nodes_of.setdefault(pc[2], set())
        if pc[0] == "road":
            for q in pc[6]:
                s.add((round(q[0], 1), round(q[1], 1)))
        else:
            s.add((round(pc[6][0][0], 1), round(pc[6][0][1], 1)))
            s.add((round(pc[6][-1][0], 1), round(pc[6][-1][1], 1)))
    # grid of segments
    cell = 40.0
    grid = {}
    segs = []
    for pi, pc in enumerate(pieces):
        q = pc[6]
        for k in range(len(q) - 1):
            si = len(segs)
            segs.append((pi, k))
            x0, x1 = sorted((q[k][0], q[k + 1][0]))
            z0, z1 = sorted((q[k][1], q[k + 1][1]))
            for cx in range(int(math.floor(x0 / cell)), int(math.floor(x1 / cell)) + 1):
                for cz in range(int(math.floor(z0 / cell)), int(math.floor(z1 / cell)) + 1):
                    grid.setdefault((cx, cz), []).append(si)
    seen = set()
    out = []
    for cellsegs in grid.values():
        for ia in range(len(cellsegs)):
            for ib in range(ia + 1, len(cellsegs)):
                sa, sb = cellsegs[ia], cellsegs[ib]
                pa, ka = segs[sa]
                pb, kb = segs[sb]
                if pa == pb:
                    continue
                A, B = pieces[pa], pieces[pb]
                if A[2] == B[2]:
                    continue
                key = (min(sa, sb), max(sa, sb))
                if key in seen:
                    continue
                seen.add(key)
                r = seg_x(A[6][ka], A[6][ka + 1], B[6][kb], B[6][kb + 1])
                if r is None:
                    continue
                ta, tb = r
                X = (A[6][ka][0] + (A[6][ka + 1][0] - A[6][ka][0]) * ta, A[6][ka][1] + (A[6][ka + 1][1] - A[6][ka][1]) * ta)
                shared = nodes_of[A[2]] & nodes_of[B[2]]
                if any(math.hypot(n[0] - X[0], n[1] - X[1]) < JOIN_M for n in shared):
                    continue
                # a piece that ends ON the other (a ramp merging into a bridge mid-span: the bridge's points are resampled, so the node is not
                # among them): joining, not crossing
                # (and nearly parallel there: a road split right under a bridge crosses it)
                da = (A[6][ka + 1][0] - A[6][ka][0], A[6][ka + 1][1] - A[6][ka][1])
                db = (B[6][kb + 1][0] - B[6][kb][0], B[6][kb + 1][1] - B[6][kb][1])
                sin = abs(da[0] * db[1] - da[1] * db[0]) / max(math.hypot(*da) * math.hypot(*db), 1e-9)
                if sin < 0.5 and any(math.hypot(e[0] - X[0], e[1] - X[1]) < 6.0 and near_line(e, other[6]) < 1.5 for e, other in ((A[6][0], B), (A[6][-1], B), (B[6][0], A), (B[6][-1], A))):
                    continue
                out.append((A, ka, ta, B, kb, tb, X))
    # merge duplicates (same two osm ways within 15 m)
    merged = []
    for c in out:
        if any({c[0][2], c[3][2]} == {d[0][2], d[3][2]} and math.hypot(c[6][0] - d[6][0], c[6][1] - d[6][1]) < 15 for d in merged):
            continue
        merged.append(c)

    def info(pc):
        t = tags.get(pc[2], {})
        try:
            layer = int(t.get("layer", "0").split(";")[0])
        except ValueError:
            layer = 0
        br = pc[0] == "bridge" or t.get("bridge") not in (None, "no")
        tun = pc[8] or t.get("tunnel") not in (None, "no")
        return layer, br, tun

    res = []
    for A, ka, ta, B, kb, tb, X in merged:
        la, ba, ua = info(A)
        lb, bb, ub = info(B)
        why = ""
        if la != lb:
            over = A if la > lb else B
            why = f"OSM layer {la} / {lb}"
        elif ba != bb:
            over = A if ba else B
            why = "OSM bridge"
        elif ua != ub:
            over = B if ua else A
            why = "OSM tunnel"
        elif (A[3] == EXPRESS) != (B[3] == EXPRESS):
            over = A if A[3] == EXPRESS else B
            why = "Express Lanes over (Dan)"
        else:
            over = A if RANK.get(A[4], 0) >= RANK.get(B[4], 0) else B
            why = "untagged: the higher class"
        under = B if over is A else A
        ko, to = (ka, ta) if over is A else (kb, tb)
        ku, tu = (kb, tb) if over is A else (ka, ta)
        res.append({
            "x": round(X[0], 2), "z": round(X[1], 2), "why": why,
            "over": {"kind": over[0], "index": over[1], "osm_id": over[2], "name": over[3], "class": over[4], "seg": ko, "t": round(to, 4)},
            "under": {"kind": under[0], "index": under[1], "osm_id": under[2], "name": under[3], "class": under[4], "seg": ku, "t": round(tu, 4)},
        })
    res.sort(key=lambda c: (c["x"], c["z"]))
    os.makedirs(os.path.join(ROOT, "Data", "tracks"), exist_ok=True)
    with open(os.path.join(ROOT, "Data", "tracks", "crossings.json"), "w", encoding="utf-8") as fh:
        json.dump({"note": "Round 82 A: roads crossing without a shared node (Tools/mapgen/crossings.py)", "crossings": res}, fh, indent=1)
    kinds = {}
    for c in res:
        k = f"{c['over']['kind']} over {c['under']['kind']}"
        kinds[k] = kinds.get(k, 0) + 1
    print(f"{len(res)} crossings: {kinds}")
    for c in res:
        o, u = c["over"], c["under"]
        print(f"({c['x']:.0f}, {c['z']:.0f}) {o['kind']} {o['osm_id']} {o['name'] or o['class']} OVER {u['kind']} {u['osm_id']} {u['name'] or u['class']} [{c['why']}]")


if __name__ == "__main__":
    sys.exit(main())
