"""Round 48 (Dan, 2026-10-18 16:25 and 2026-10-19 01:43): the goose egg hunt and exploration (Racer's acorn hunt, its map fog,
destinations, travel and waypoints). "lets go with goose eggs. We can have 100 of them spread around with tier unlocks instead of just
one unlock." This script places the 100 eggs and lists the map's areas and the destinations; Build Map puts them in the scene
(MapBuilder.Eggs.cs, game layer: its own root "Goose eggs"), the game collects them (Explore/EggHunt.cs).

Each egg: an id, its area (the map shows areas, never spots), how it is found ("easy": by a road; "hard": a back yard, a roof, the air
over a jump, a pipe, a shortcut's line), and where:
  kind "ground"  x, z: the game finds the top surface under it at start (a roof, a deck, the ground) and sets the egg on it
  kind "fixed"   x, y, z: in the air over a jump (on the design flight's top) or in a pipe (over its floor)
  radius         how near a vehicle has to pass (2.8 m as Racer's acorns; 4 m in the air; 2.5 m in a pipe)
Destinations (fast travel once discovered, 45 m): a point on a road at each neighborhood, community and course, the stunt park, the
lake trail, the hub. Placement is deterministic (fixed seed): re-run after mapgen, after a course or the pipe network changes, then
Build Map (or `-executeMethod ChastainRush.MapBuilder.RebuildEggsFromCommandLine` for the eggs alone).

Usage: python Tools/mapgen/make_eggs.py [--plot]      writes Data/explore/eggs.json (picture: local/round48/eggs.png, git-ignored)
"""
import json, math, os, random, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/explore/eggs.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
def J(p): return json.load(open(os.path.join(ROOT, p), encoding="utf-8"))
m = J("Data/map.json"); NET = J("Data/pipes/network.json")
HOODS = J("Data/neighborhood/neighborhoods.json")["neighborhoods"]; COMM = J("Data/neighborhood/communities.json")["communities"]
GLEN = J("Data/neighborhood/chastain_glen_area.json")
ROUTES = {f: J("Data/routes/" + f) for f in sorted(os.listdir(os.path.join(ROOT, "Data/routes"))) if f.endswith(".json")}
CREEK, RIDGE, LAKES, PARK = J("Data/tracks/noonday_creek.json"), J("Data/tracks/chastain_ridge.json"), J("Data/tracks/chastain_lakes.json"), J("Data/tracks/stunt_park.json")
rng = random.Random(4848)

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def inside(q, ring):
    ins = False
    for i in range(len(ring)):
        a, b = ring[i], ring[(i + 1) % len(ring)]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]: ins = not ins
    return ins
def centroid(r): return (sum(p[0] for p in r) / len(r), sum(p[1] for p in r) / len(r))
BLD = [pts(b["footprint"]) for b in m["buildings"]]
BOX = [(min(p[0] for p in r), max(p[0] for p in r), min(p[1] for p in r), max(p[1] for p in r)) for r in BLD]
WATER = [pts(w["outline"]) for w in m["water"]]
ROADS = [(pts(r["points"]), r["width"], r["name"]) for r in m["roads"]]
def bdist(q):
    best = 99.0
    for i, r in enumerate(BLD):
        b = BOX[i]
        if b[0] - 10 < q[0] < b[1] + 10 and b[2] - 10 < q[1] < b[3] + 10:
            best = min(best, 0.0 if inside(q, r) else min(seg_dist(q, r[k], r[(k + 1) % len(r)]) for k in range(len(r))))
    return best
def wet(q): return any(inside(q, w) for w in WATER)
def rdist(q):
    return min((seg_dist(q, a, b) - w / 2 for p, w, _ in ROADS for a, b in zip(p[:-1], p[1:]) if abs(a[0] - q[0]) < 120 and abs(a[1] - q[1]) < 120), default=99.0)
def ok_ground(q, clear=2.5): return 5 < q[0] < meta["size_x_m"] - 5 and 5 < q[1] < meta["size_z_m"] - 5 and bdist(q) > clear and not wet(q)

eggs = []
def add(area, how, kind, q, y=None, radius=2.8, note=""):
    e = {"id": f"egg-{len(eggs) + 1:03d}", "area": area, "how": how, "kind": kind, "x": round(q[0], 2), "z": round(q[1], 2),
         "y": round(y if y is not None else ground(*q) + 0.9, 2), "radius": radius, "note": note}
    if any(math.dist((e["x"], e["z"]), (o["x"], o["z"])) < 25 and abs(e["y"] - o["y"]) < 6 for o in eggs): return False
    eggs.append(e); return True

def street_eggs(area, road_ids, n, note):
    """by the road: on the verge, 1.5 m off the pavement's edge (either side), clear of buildings and water"""
    got, tries = 0, 0
    cands = [i for i in road_ids if i < len(ROADS) and len(ROADS[i][0]) > 1]
    while got < n and tries < 400 and cands:
        tries += 1; p, w, nm = ROADS[rng.choice(cands)]
        k = rng.randrange(len(p) - 1); a, b = p[k], p[k + 1]; t = rng.random(); L = math.dist(a, b)
        if L < 1: continue
        c = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t); nx, nz = -(b[1] - a[1]) / L, (b[0] - a[0]) / L; s = rng.choice((-1, 1))
        q = (c[0] + nx * s * (w / 2 + 1.5), c[1] + nz * s * (w / 2 + 1.5))
        if ok_ground(q, 4.0) and add(area, "easy", "ground", q, note=f"{note}: by {nm or 'the road'}"): got += 1
    return got

def yard_eggs(area, houses, n, note):
    """a back yard: behind a house (the far side from its nearest road), 7 m past the wall, clear of every building"""
    got, tries = 0, 0
    while got < n and tries < 400 and houses:
        tries += 1; h = rng.choice(houses); r = BLD[h]; c = centroid(r)
        near = min(((seg_dist(c, a, b), a, b) for p, w, _ in ROADS for a, b in zip(p[:-1], p[1:]) if abs(a[0] - c[0]) < 80 and abs(a[1] - c[1]) < 80), default=None)
        if near is None: continue
        _, a, b = near; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9
        t = max(0, min(1, ((c[0] - a[0]) * ab[0] + (c[1] - a[1]) * ab[1]) / L2)); f = (a[0] + ab[0] * t, a[1] + ab[1] * t)
        d = (c[0] - f[0], c[1] - f[1]); L = math.hypot(*d)
        if L < 1: continue
        d = (d[0] / L, d[1] / L); half = max(abs((p[0] - c[0]) * d[0] + (p[1] - c[1]) * d[1]) for p in r)
        q = (c[0] + d[0] * (half + 7.0), c[1] + d[1] * (half + 7.0))
        if ok_ground(q, 3.0) and rdist(q) > 8 and add(area, "hard", "ground", q, note=f"{note}: behind house {h}"): got += 1
    return got

def line_eggs(area, sc_paths, n, note, how="hard"):
    """along a shortcut's line, where it is outdoors and on the ground (60 % of the way, then other points)"""
    got = 0
    for nm, p in sc_paths:
        if got >= n: break
        order = sorted(range(len(p)), key=lambda k: abs(k / max(1, len(p) - 1) - 0.6))
        for k in order:
            q = p[k]
            if ok_ground(q, 3.0) and add(area, how, "ground", q, note=f"{note}: on {nm}'s line"): got += 1; break
    return got

def air_egg(area, lip, d, lip_y, dist, top, note):
    """over a jump: at the top of its design flight (half its length out), 0.6 m under the body's highest point"""
    q = (lip[0] + d[0] * dist * 0.5, lip[1] + d[1] * dist * 0.5)
    return add(area, "hard", "fixed", q, lip_y + top - 0.6 + 0.8, 4.0, note)

def jumps(track, ids=None):
    out = []
    for b in track.get("big_jumps", []):
        if ids and b["id"] not in ids: continue
        fl = b.get("flights") or {}
        if not fl: continue
        k = min(fl, key=lambda v: abs(float(v) - b.get("design", float(v))))
        out.append((b["id"], b["lip"], b["dir"], b["lip_y"], fl[k][1], fl[k][2]))
    return out

def hood(name): return next(h for h in HOODS if h["name"] == name)
AREAS = []
def area(name, direction): AREAS.append({"name": name, "direction": direction}); return name

# ---------------------------------------------------------------- Chastain Glen (12)
A = area("Chastain Glen", "Off Chastain Glen Lane, where the game starts: its streets, back yards and shortcuts")
glen_shortcuts = [(s["name"], pts(s["path"])) for s in ROUTES["chastain_glen_loop.json"]["official_shortcuts"]]
street_eggs(A, GLEN["roads"], 6, "Chastain Glen"); yard_eggs(A, GLEN["houses"], 3, "Chastain Glen"); line_eggs(A, glen_shortcuts, 3, "Chastain Glen")

# ---------------------------------------------------------------- Grasmere Court, Chastain Commons, Chastain Enclave (7)
A = area("Grasmere, Commons and Enclave", "Grasmere Court and Chastain Commons beside Chastain Glen; Chastain Enclave west, by Noonday Creek")
for c, n_st, n_yd in ((COMM[0], 1, 1), (COMM[1], 1, 1), (COMM[2], 2, 1)): street_eggs(A, c["roads"], n_st, c["name"]); yard_eggs(A, c["houses"], n_yd, c["name"])
en = COMM[2].get("amenities") or {}
line_eggs(A, [("the Enclave pool lot", [tuple(COMM[2]["entrances"][0][k] for k in ("x", "z"))])], 0, "")

# ---------------------------------------------------------------- Chastain Lakes (10)
A = area("Chastain Lakes", "West, round the two lakes off Bells Ferry Road: the streets, the lake trail, the dock and the drain")
n3 = hood("Chastain Lakes"); street_eggs(A, n3["roads"], 3, "Chastain Lakes"); yard_eggs(A, n3["houses"], 1, "Chastain Lakes")
lk = LAKES["lakes"]; tr = pts(lk["trail"])
add(A, "easy", "ground", tr[len(tr) // 3], note="the lake trail, by the pool"); add(A, "easy", "ground", tr[int(len(tr) * 0.8)], note="the lake trail, on the dam")
br = pts(lk["bridge"]); add(A, "easy", "ground", ((br[0][0] + br[1][0]) / 2, (br[0][1] + br[1][1]) / 2), note="on the footbridge")
bt = lk["boat"]; dock = (bt["dock_root"][0] * 0.4 + (bt["lip"][0] - bt["dir"][0] * bt["length"]) * 0.6, bt["dock_root"][1] * 0.4 + (bt["lip"][1] - bt["dir"][1] * bt["length"]) * 0.6)
add(A, "hard", "ground", dock, note="on the dock to the party boat")
fl = bt["flights"]["34.0"]; air_egg(A, bt["lip"], bt["dir"], bt["lip_y"], fl[1], fl[2], "over the boat jump (34 m/s)")
b28 = next(t for t in NET["tubes"] if t["id"] == "B28"); p3 = [(b28["pts"][i], b28["pts"][i + 1], b28["pts"][i + 2]) for i in range(0, len(b28["pts"]), 3)]
q = p3[len(p3) // 2]; add(A, "hard", "fixed", (q[0], q[2]), q[1] + 1.2, 2.5, "down the lake drain, in B28's glass under the lake")

# ---------------------------------------------------------------- Chastain Ridge and Canton Road (9)
A = area("Chastain Ridge and Canton Road", "East, along Canton Road and up Chastain Ridge Drive: the Ridge leap, the crest cut, the crypt")
ridge_roads = [i for i, r in enumerate(m["roads"]) if r["name"] in ("Chastain Ridge Drive", "Chastain Ridge Drive Northeast", "Chastain Corner", "Chastain Corner Northeast")]
street_eggs(A, ridge_roads, 3, "Chastain Ridge")
for j in jumps(RIDGE)[:2]: air_egg(A, j[1], j[2], j[3], j[4], j[5], f"over {j[0]}")
rsc = {s["id"]: s for s in ROUTES["chastain_ridge.json"]["official_shortcuts"]}
line_eggs(A, [(rsc[k]["name"], pts(rsc[k]["path"])) for k in ("s1_knoll_hop", "s2_storage_hop", "rc_crest_cut")], 3, "Chastain Ridge")
cr = rsc["rch_crypt"]; cp = pts(cr["path"]); k = len(cp) // 2; add(A, "hard", "fixed", cp[k], cr["path_y"][k] + 1.0, 2.8, "in the crypt under church 74")

# ---------------------------------------------------------------- Chastain Corner: the stunt park and the shops (10)
A = area("Chastain Corner", "South-east, at Canton Road and Chastain Road: the stunt park (roofs too) and the shops round it")
for i, nm in ((28, "the market's roof"), (27, "the bank's roof"), (88, "the shop row's roof"), (61, "the garage row's roof"), (57, "the warehouse roof")):
    add(A, "hard", "ground", centroid(BLD[i]), radius=3.2, note=f"stunt park / corner: on {nm} (building {i})")
st = PARK["start"]; add(A, "easy", "ground", (st[0], st[1]), note="the stunt park's start")
add(A, "easy", "ground", (st[0] - 40.0, st[1] + 25.0), note="the stunt park's lot")
for i, nm in ((26, "the corner shop"), (727, "the shop on Canton Road"), (75, "church 74's wing")):
    c = centroid(BLD[i]); near = min(((seg_dist(c, a, b), a, b) for p, w, _ in ROADS for a, b in zip(p[:-1], p[1:]) if abs(a[0] - c[0]) < 90 and abs(a[1] - c[1]) < 90))
    _, a, b = near; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2; t = max(0, min(1, ((c[0] - a[0]) * ab[0] + (c[1] - a[1]) * ab[1]) / L2)); f = (a[0] + ab[0] * t, a[1] + ab[1] * t)
    q = ((c[0] + f[0]) / 2, (c[1] + f[1]) / 2)
    if not ok_ground(q, 2.0): q = (c[0] * 0.3 + f[0] * 0.7, c[1] * 0.3 + f[1] * 0.7)
    add(A, "easy", "ground", q, note=f"in front of {nm}")

# ---------------------------------------------------------------- Noonday Creek (10)
A = area("Noonday Creek", "The woods trail along Noonday Creek, north of the middle of the map: on the trail and over its big jumps")
ln = pts(CREEK["line"])
for f in (0.08, 0.27, 0.45, 0.62, 0.80, 0.95): add(A, "easy", "ground", ln[int(len(ln) * f)], note=f"the creek trail, {int(f * 100)} % along")
for j in jumps(CREEK, ("BJ1", "BJ2", "BJ3", "BJ4", "J4", "N2", "N5"))[:4]: air_egg(A, j[1], j[2], j[3], j[4], j[5], f"over {j[0]}")

# ---------------------------------------------------------------- the pipes (12)
A = area("Underground", "In the pipe network: the mains (their glass under the ponds and lakes), the junctions and the hub")
for tid in ("M1a", "M5c", "M2a", "M3c", "M4c", "S3", "M4b", "M2c", "B14", "B19a"):
    t = next(t for t in NET["tubes"] if t["id"] == tid); p3 = [(t["pts"][i], t["pts"][i + 1], t["pts"][i + 2]) for i in range(0, len(t["pts"]), 3)]
    q = p3[int(len(p3) * 0.55)]; add(A, "hard", "fixed", (q[0], q[2]), q[1] + 1.2, 2.5, f"in {t['name']}")
for cid in ("HUB", "J4"):
    c = next(c for c in NET["chambers"] if c["id"] == cid); add(A, "hard", "fixed", (c["x"], c["z"]), c["floor"] + 1.0, 3.0, f"in {cid}'s room")

# ---------------------------------------------------------------- the other neighborhoods (30)
HOOD_AREAS = (("North neighborhoods", "North, up to the map's top edge, above Chastain Lakes and Noonday Creek", 8,
               ["Guidhall Trail", "Southwick Drive", "Station Drive", "Sellers Way", "Westmoreland Drive", "Lansing Drive", "Canterbury"]),
              ("Bells Ferry neighborhoods", "Off Bells Ferry Road between Chastain Lakes and the Enclave, north and south of the creek", 7,
               ["Suburban Drive", "Hartley Woods Drive", "Parkwood Drive", "Estates Landing Drive"]),
              ("East neighborhoods", "East of Canton Road, out to the map's east edge", 8, ["Centerview Drive", "Blackwell Circle", "Pine Manor", "Windridge Drive", "Old Farm Walk", "The Willows"]),
              ("South neighborhoods", "South of Chastain Road and New Chastain Road", 7, ["Embry Lane", "Willard Drive", "Kathryn Drive", "Chastain Crossing", "Chastain Trace", "Chastain Manor Way"]))
for nm, direction, want, keys in HOOD_AREAS:
    A = area(nm, direction); hs = [hood(k) for k in keys]
    got = 0; k = 0
    while got < want and k < 80:
        h = hs[k % len(hs)]; k += 1
        got += yard_eggs(A, h["houses"], 1, h["name"]) if k % 3 == 0 else street_eggs(A, h["roads"], 1, h["name"])

# ---------------------------------------------------------------- top up to 100 (by a road in those neighborhoods), then the destinations
k = 0
while len(eggs) < 100 and k < 200:
    nm, _, _, keys = HOOD_AREAS[k % len(HOOD_AREAS)]; k += 1
    h = hood(rng.choice(keys)); street_eggs(nm, h["roads"], 1, h["name"])
eggs = eggs[:100]
for i, e in enumerate(eggs, 1): e["id"] = f"egg-{i:03d}"

def road_point(near, names=None):
    best = None
    for p, w, nm in ROADS:
        if names and nm not in names: continue
        for a, b in zip(p[:-1], p[1:]):
            if abs(a[0] - near[0]) > 200 or abs(a[1] - near[1]) > 200: continue
            d = seg_dist(near, a, b)
            if best is None or d < best[0]:
                ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9; t = max(0, min(1, ((near[0] - a[0]) * ab[0] + (near[1] - a[1]) * ab[1]) / L2))
                best = (d, (a[0] + ab[0] * t, a[1] + ab[1] * t), math.degrees(math.atan2(ab[0], ab[1])) % 360)
    return best
dests = []
def dest(did, title, q, yaw=None, y=None, names=None):
    if yaw is None:
        rp = road_point(q, names)
        if rp is None: return
        q, yaw = rp[1], rp[2]
    dests.append({"id": did, "title": title, "x": round(q[0], 2), "z": round(q[1], 2), "y": round(y, 2) if y is not None else 0.0, "fixed": y is not None, "yaw": round(yaw, 1)})
dest("chastain-glen", "Chastain Glen", (m["spawn"]["x"], m["spawn"]["z"]), m["spawn"]["heading_deg"] % 360)
for c in COMM: e0 = c["entrances"][0]; dest(c["name"].lower().replace(" ", "-"), c["name"], (e0["x"] + e0["dx"] * 25, e0["z"] + e0["dz"] * 25), math.degrees(math.atan2(e0["dx"], e0["dz"])) % 360)
for h in HOODS: dest("hood-" + h["key"], h["name"], (h["x"], h["z"]), names=set(h["streets"]))
dest("stunt-park", PARK.get("title", "Stunt park"), (PARK["start"][0], PARK["start"][1]), math.degrees(math.atan2(PARK["start"][2], PARK["start"][3])) % 360)
for f, r in ROUTES.items():
    if r.get("underground"): continue
    li = r["lead_in"]; dest("race-" + f[:-5], r["name"] + " start", (li[0], li[1]), math.degrees(math.atan2(li[2] - li[0], li[3] - li[1])) % 360)
lt = pts(LAKES["lakes"]["trail"]); a, b = lt[int(len(lt) * 0.7)], lt[int(len(lt) * 0.7) + 2]
dest("lake-trail", "The lake trail", a, math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360)
hub = next(c for c in NET["chambers"] if c["id"] == "HUB")
ug = ROUTES.get("underground.json")
if ug and ug.get("lead_in_y"):
    li = ug["lead_in"]; dest("underground-hub", "The hub (underground)", (li[0], li[1]), math.degrees(math.atan2(li[2] - li[0], li[3] - li[1])) % 360, ug["lead_in_y"][0])

counts = {}
for e in eggs: counts.setdefault(e["area"], [0, 0])[0 if e["how"] == "easy" else 1] += 1
for a_ in AREAS: c = counts.get(a_["name"], [0, 0]); a_["eggs"] = c[0] + c[1]; print(f"  {a_['name']:32} {c[0] + c[1]:3} eggs ({c[0]} easy, {c[1]} hard)")
print(f"{len(eggs)} eggs, {len(AREAS)} areas, {len(dests)} destinations")
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump({"note": "Round 48: the goose egg hunt (Tools/mapgen/make_eggs.py; built by MapBuilder.Eggs.cs, collected by Explore/EggHunt.cs). Map meters.",
           "tiers": [10, 25, 50, 75, 100], "areas": AREAS, "eggs": eggs, "destinations": dests}, open(OUT, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(20, 10))
    for p, w, nm in ROADS: ax.plot(*zip(*p), color="#bbb", lw=0.6)
    for w in WATER: ax.fill(*zip(*w), color="#9cf")
    cols = {}; palette = ["#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#a65628", "#f781bf", "#999999", "#66c2a5", "#e6ab02"]
    for e in eggs:
        c = cols.setdefault(e["area"], palette[len(cols) % len(palette)])
        ax.plot(e["x"], e["z"], "o" if e["how"] == "easy" else "^", color=c, ms=6)
    for d in dests: ax.plot(d["x"], d["z"], "k*", ms=8)
    for nm, c in cols.items(): ax.plot([], [], "o", color=c, label=nm)
    ax.legend(loc="lower left", fontsize=8); ax.set_aspect("equal"); ax.set_xlim(0, meta["size_x_m"]); ax.set_ylim(0, meta["size_z_m"])
    os.makedirs(os.path.join(ROOT, "local/round48"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round48/eggs.png"), dpi=70, bbox_inches="tight"); print("picture: local/round48/eggs.png")
