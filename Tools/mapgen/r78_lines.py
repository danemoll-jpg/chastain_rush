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


def add(name, kind, x, z, heading, back=60.0, length=120.0):
    if any(math.dist((x, z), q) < 25 for q in seen): return
    seen.append((x, z))
    h = math.radians(heading)
    out.append({"name": re.sub(r"[^A-Za-z0-9]+", "_", name).strip("_")[:40] + f"_{len(out)}", "kind": kind,
                "x": round(x - math.sin(h) * back, 1), "z": round(z - math.cos(h) * back, 1), "heading": round(heading, 1), "len": length})


for c in rj["overpasses"] + rj["crossings"]:
    if c in rj["overpasses"] or c["class"] in OVER or (c["name"] and c["name"] in named):
        add(c["name"] or c["class"], "under the railroad", c["x"], c["z"], c["road_heading"])
lines = [[(l["points"][k], l["points"][k + 1]) for k in range(0, len(l["points"]), 2)] for l in rj["lines"]]
for b in m["bridges"]:
    p = [(b["points"][k], b["points"][k + 1]) for k in range(0, len(b["points"]), 2)]
    x0, x1, z0, z1 = min(q[0] for q in p) - 4, max(q[0] for q in p) + 4, min(q[1] for q in p) - 4, max(q[1] for q in p) + 4
    hit = any(any(x0 < q[0] < x1 and z0 < q[1] < z1 and min(math.dist(q, r) for r in p) < 3 for q in ln) for ln in lines)
    if hit:
        a, e = p[0], p[-1]; hd = math.degrees(math.atan2(e[0] - a[0], e[1] - a[1])) % 360
        add(b["name"] or b["class"], "over the railroad", a[0], a[1], hd, 60.0, 60.0 + b["length_m"] + 60.0)
os.makedirs(os.path.join(ROOT, "local/round78"), exist_ok=True)
json.dump({"rail": out}, open(os.path.join(ROOT, "local/round78/r78_lines.json"), "w"), indent=1)
print(f"{len(out)} viewpoints ({sum(o['kind'] == 'under the railroad' for o in out)} under, {sum(o['kind'] == 'over the railroad' for o in out)} over); named in the log: {sorted(named)}")
for o in out: print(" ", o["name"], o["kind"], o["x"], o["z"], o["heading"])
