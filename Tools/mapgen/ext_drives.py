"""Round 59 (check only): the places and drives the Round 59 exe check uses (-r59Drives <file>): the shortest way along the map's roads
(no motorways, no ramps) from Chastain Glen's spawn to Town Center at Cobb and to Kennesaw State University, and the places for the
frame rate and the pictures (Town Center, KSU, the I-75 / I-575 interchange, the old I-575 edge). Writes local/round59/r59_places.json.

Usage: python Tools/mapgen/ext_drives.py
"""
import heapq, json, math, os
from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
M = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
OUT = os.path.join(ROOT, "local", "round59", "r59_places.json")
tr = Transformer.from_crs(4326, M["utm_epsg"], always_xy=True)
ox, oz = M["origin_utm"]
def ll(lat, lon): x, y = tr.transform(lon, lat); return (x - ox, y - oz)

# the graph: every road point a node (rounded to 0.5 m), each piece's segments its edges; bridges join as roads
nodes, adj = {}, {}
def key(p): return (round(p[0] * 2) / 2, round(p[1] * 2) / 2)
def add(pts):
    for a, b in zip(pts, pts[1:]):
        ka, kb = key(a), key(b); nodes[ka] = a; nodes[kb] = b; d = math.dist(a, b)
        adj.setdefault(ka, []).append((kb, d)); adj.setdefault(kb, []).append((ka, d))
def pts(flat): return [(flat[i], flat[i + 1]) for i in range(0, len(flat), 2)]
for r in M["roads"]:
    if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
    add(pts(r["points"]))
for b in M["bridges"]:
    if b["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
    add(pts(b["points"]))
# pieces cut at a bridge's end or the old edge meet within a meter: joined
ks = list(nodes)
grid = {}
for k in ks: grid.setdefault((int(k[0] // 2), int(k[1] // 2)), []).append(k)
for k in ks:
    cx, cz = int(k[0] // 2), int(k[1] // 2)
    for i in range(cx - 1, cx + 2):
        for j in range(cz - 1, cz + 2):
            for o in grid.get((i, j), ()):
                if o != k and math.dist(k, o) < 1.2: adj[k].append((o, math.dist(k, o)))

def nearest(q): return min(nodes, key=lambda k: math.dist(k, q))
def reach(s):
    seen = {s}; st = [s]
    while st:
        u = st.pop()
        for v, _ in adj.get(u, ()):
            if v not in seen: seen.add(v); st.append(v)
    return seen
def path(a, b):
    s = nearest(a); comp = reach(s); t = min(comp, key=lambda k: math.dist(k, b))   # the nearest road point that can be reached
    dist = {s: 0.0}; prev = {}; pq = [(0.0, s)]
    while pq:
        d, u = heapq.heappop(pq)
        if u == t: break
        if d > dist[u]: continue
        for v, w in adj.get(u, ()):
            nd = d + w
            if nd < dist.get(v, 1e18): dist[v] = nd; prev[v] = u; heapq.heappush(pq, (nd, v))
    if t not in prev and t != s: return None, 0
    out = [t]
    while out[-1] != s: out.append(prev[out[-1]])
    out.reverse()
    # every ~4 m
    res = [nodes[out[0]]]
    for k in out[1:]:
        if math.dist(res[-1], nodes[k]) >= 4.0: res.append(nodes[k])
    if res[-1] != nodes[out[-1]]: res.append(nodes[out[-1]])
    return res, dist[t]

spawn = (M["spawn"]["x"], M["spawn"]["z"])
tc = ll((34.0148368 + 34.020945) / 2, (-84.5678428 + -84.5601757) / 2)
ksu = ll(34.0384, -84.5834)
inter = ll(34.0185, -84.5745)      # I-75 / I-575 (from OSM, Round 59)
edge = (0.0, 0.0)
# the old west edge where I-575 crossed it (the map's x 0): the motorway's point nearest x 0
best = None
for r in list(M["roads"]) + list(M["bridges"]):
    if r["class"] != "motorway": continue
    for p in pts(r["points"]):
        if best is None or abs(p[0]) < abs(best[0]): best = p
if best: edge = best
drives, places = [], []
for name, q in (("Chastain Glen to Town Center", tc), ("Chastain Glen to KSU", ksu)):
    line, L = path(spawn, q)
    if line is None: print(f"{name}: no way along the roads"); continue
    print(f"{name}: {L:.0f} m, {len(line)} points")
    drives.append({"name": name, "points": [round(v, 2) for p in line for v in p]})
def place(name, q, yaw=0.0, h=60.0, back=110.0):
    yr = math.radians(yaw); fx, fz = math.sin(yr), math.cos(yr)
    places.append({"name": name, "x": round(q[0], 1), "z": round(q[1], 1), "yaw": yaw,
                   "look": [round(q[0] - fx * back, 1), 0.0, round(q[1] - fz * back, 1), round(q[0], 1), 0.0, round(q[1], 1)], "h": h})
# the pictures: from 160 m up, 260 m back (south of each, looking north); the interchange and the old edge on the motorway itself
mway = [p for r in list(M["roads"]) + list(M["bridges"]) if r["class"] in ("motorway", "motorway_link") for p in pts(r["points"])]
for name, q, onm in (("Town Center", tc, False), ("KSU", ksu, False), ("I-75 and I-575", inter, True), ("the old I-575 edge", edge, True)):
    at = min(mway, key=lambda p: math.dist(p, q)) if onm else nodes[nearest(q)]
    place(name, at, 0.0, 160.0, 260.0)
json.dump({"places": places, "drives": drives}, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"{len(places)} places, {len(drives)} drives -> {OUT}")
