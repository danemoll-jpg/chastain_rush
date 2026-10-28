"""Round 78 (D): the check's viewpoints (local/round78/r78_lines.json, git-ignored; read by Checks/Round20Runner.R78.cs): every road under a rail bridge
(railroad.json's overpasses, the crossings of highways and trunk roads, and the crossings the map build made bridges: named in the build log's
"[Round77] C: railroad planned" line) and every road bridge over the railroad (map.json bridges within 3 m of a line), each from 60 m back along its road,
heading through it. Usage: python Tools/mapgen/r78_lines.py [build log]"""
import json
import math
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
rj = json.load(open(os.path.join(ROOT, "Data/tracks/railroad.json")))
log = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "local/round78/build_full1.log")
named = set()
if os.path.exists(log):
    for line in open(log, encoding="utf-8", errors="ignore"):
        if "[Round77] C: railroad planned" in line:
            mm = re.search(r"bridges over roads \(([^)]*)\)", line)
            if mm: named |= {n.strip() for n in mm.group(1).split(",") if n.strip()}
OVER = {"motorway", "motorway_link", "trunk", "trunk_link"}
out, seen = [], []


PIECES = [(r["name"], r["width"], [(r["points"][k], r["points"][k + 1]) for k in range(0, len(r["points"]), 2)]) for r in m["roads"]] +          [(b["name"], b["width"], [(b["points"][k], b["points"][k + 1]) for k in range(0, len(b["points"]), 2)]) for b in m["bridges"]]


def road_line(name, x, z, heading, reach=140.0):
    """the road through (x, z): its pieces of that name chained end to end, from reach m before to reach m after, in the heading's direction, set off to
    the right-hand lane (a quarter of its width)"""
    cand = [(w, p) for nm, w, p in PIECES if nm == name and any(math.dist(q, (x, z)) < reach + 60 for q in p)]
    if not cand: return None, 0.0
    w0, p0 = min(cand, key=lambda c: min(math.dist(q, (x, z)) for q in c[1]))
    line = list(p0); used = {id(p0)}
    for _ in range(12):
        grown = False
        for w, p in cand:
            if id(p) in used: continue
            if math.dist(p[0], line[-1]) < 2.5: line += p[1:]
            elif math.dist(p[-1], line[-1]) < 2.5: line += p[::-1][1:]
            elif math.dist(p[-1], line[0]) < 2.5: line = p[:-1] + line
            elif math.dist(p[0], line[0]) < 2.5: line = p[::-1][:-1] + line
            else: continue
            used.add(id(p)); grown = True
        if not grown: break
    # dense, in the heading's direction, cut to the reach
    dense = []
    for a, c in zip(line[:-1], line[1:]):
        n = max(1, int(math.dist(a, c) / 3.0))
        dense += [(a[0] + (c[0] - a[0]) * k / n, a[1] + (c[1] - a[1]) * k / n) for k in range(n)]
    dense.append(line[-1])
    h = (math.sin(math.radians(heading)), math.cos(math.radians(heading)))
    k0 = min(range(len(dense)), key=lambda k: math.dist(dense[k], (x, z)))
    k1 = min(len(dense) - 1, k0 + 1)
    if (dense[k1][0] - dense[k0][0]) * h[0] + (dense[k1][1] - dense[k0][1]) * h[1] < 0: dense.reverse(); k0 = len(dense) - 1 - k0
    lo, hi = max(0, k0 - int(reach / 3)), min(len(dense), k0 + int(reach / 3) + 1)
    seg = dense[lo:hi]; offs = []
    for i in range(len(seg)):
        a, c = seg[max(0, i - 1)], seg[min(len(seg) - 1, i + 1)]; t = (c[0] - a[0], c[1] - a[1]); nn = math.hypot(*t) or 1.0
        offs.append((round(seg[i][0] + t[1] / nn * w0 / 4, 2), round(seg[i][1] - t[0] / nn * w0 / 4, 2)))
    return [v for q in offs for v in q], w0


def add(name, kind, x, z, heading, back=60.0, length=120.0):
    if any(math.dist((x, z), q) < 25 for q in seen): return
    seen.append((x, z))
    h = math.radians(heading)
    ln, w = road_line(name, x, z, heading) if name else (None, 0.0)
    rec = {"name": re.sub(r"[^A-Za-z0-9]+", "_", name or kind).strip("_")[:40] + f"_{len(out)}", "kind": kind,
           "x": round(x - math.sin(h) * back, 1), "z": round(z - math.cos(h) * back, 1), "heading": round(heading, 1), "len": length}
    if ln and len(ln) >= 8:
        rec["line"] = ln; rec["x"], rec["z"] = ln[0], ln[1]; rec["heading"] = round(math.degrees(math.atan2(ln[4] - ln[0], ln[5] - ln[1])) % 360, 1)
    out.append(rec)


for c in rj["overpasses"] + rj["crossings"]:
    if c in rj["overpasses"] or c["class"] in OVER or (c["name"] and c["name"] in named):
        add(c["name"], "under the railroad", c["x"], c["z"], c["road_heading"])
lines = [[(l["points"][k], l["points"][k + 1]) for k in range(0, len(l["points"]), 2)] for l in rj["lines"]]
for b in m["bridges"]:
    p = [(b["points"][k], b["points"][k + 1]) for k in range(0, len(b["points"]), 2)]
    x0, x1, z0, z1 = min(q[0] for q in p) - 4, max(q[0] for q in p) + 4, min(q[1] for q in p) - 4, max(q[1] for q in p) + 4
    hit = any(any(x0 < q[0] < x1 and z0 < q[1] < z1 and min(math.dist(q, r) for r in p) < 3 for q in ln) for ln in lines)
    if hit:
        a, e = p[0], p[-1]; hd = math.degrees(math.atan2(e[0] - a[0], e[1] - a[1])) % 360
        mid = p[len(p) // 2]
        add(b["name"], "over the railroad", mid[0], mid[1], hd, 60.0, 60.0 + b["length_m"] + 60.0)
os.makedirs(os.path.join(ROOT, "local/round78"), exist_ok=True)
json.dump({"rail": out}, open(os.path.join(ROOT, "local/round78/r78_lines.json"), "w"), indent=1)
print(f"{len(out)} viewpoints ({sum(o['kind'] == 'under the railroad' for o in out)} under, {sum(o['kind'] == 'over the railroad' for o in out)} over); named in the log: {sorted(named)}")
for o in out: print(" ", o["name"], o["kind"], o["x"], o["z"], o["heading"])
