"""Round 89 (C, Dan, 2026-11-01 03:04: "either spreadout the goose eggs or create more to be found all across the map"): 150 more goose eggs
over the map's extension (stages 1 to 3), 250 in all. Keeps Round 48's 100 (make_eggs.py) exactly as they are (their ids, so found eggs
stay found) and appends egg-101 .. egg-250, their areas and the new destinations to Data/explore/eggs.json; the tiers rescaled to 250.
Idempotent: drops what an earlier run added (eggs past egg-100, the areas and destinations it names) and adds them again.
Run after make_eggs.py and add_extension_places.py (both rewrite or add to the file): make_eggs.py -> add_extension_places.py -> this.

How each new egg is found (the same mix as Round 48: about half easy, half hard):
  easy   by a road or a lot: on the verge 1.5 m off the pavement, clear of buildings and water
  hard   a back yard, the woods, a trail, a parking deck's roof (driven up), inside Town Center's mall, under a highway deck, on a jump's
         landing (reached by the jump: the quarry decks, the summit leap)
Kinds: "ground" (the top surface under it, as Round 48), and new "low" (the lowest surface under it with a car's room over it: inside the
mall, under a deck; Explore/EggHunt.cs Settle).

Usage: python Tools/mapgen/make_eggs_more.py [--plot]   (picture: local/round89/eggs_more.png)
"""
import json, math, os, random, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/explore/eggs.json")
def J(p): return json.load(open(os.path.join(ROOT, p), encoding="utf-8"))
M = J("Data/map.json")
EG = J("Data/explore/eggs.json")
rng = random.Random(8989)

# ---- the ground (terrain_full: the core and stages 1 and 2; stage 3's piece beside it) for slope checks
def load_terrain(name):
    meta = J(f"Assets/Generated/{name}.json")
    H = np.memmap(os.path.join(ROOT, f"Assets/Generated/{name}.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
    return meta, H
TF = load_terrain("terrain_full"); TP = load_terrain("terrain_piece3")
def height(x, z):
    for meta, H in (TF, TP):
        fx = (x - meta["origin_x_m"]) / meta["step_x_m"]; fz = (z - meta["origin_z_m"]) / meta["step_z_m"]
        i, j = int(fx), int(fz)
        if 0 <= i < meta["grid_x"] - 1 and 0 <= j < meta["grid_z"] - 1:
            v = float(H[j, i])
            if v == 0 and meta is TF[0]: continue
            return v / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
    return None
def slope(q, r=3.0):
    h0 = height(*q)
    if h0 is None: return 9.0
    s = 0.0
    for dx, dz in ((r, 0), (-r, 0), (0, r), (0, -r)):
        h = height(q[0] + dx, q[1] + dz)
        if h is None: return 9.0
        s = max(s, abs(h - h0) / r)
    return s

# ---- geometry
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f) - 1, 2)]
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.hypot(q[0] - a[0] - ab[0] * t, q[1] - a[1] - ab[1] * t)
def inside(q, ring):
    c = False; n = len(ring)
    for i in range(n):
        a, b = ring[i], ring[(i + 1) % n]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1] + 1e-12) + a[0]: c = not c
    return c
def centroid(r): return (sum(p[0] for p in r) / len(r), sum(p[1] for p in r) / len(r))
def area_of(r): return abs(sum(r[i][0] * r[(i + 1) % len(r)][1] - r[(i + 1) % len(r)][0] * r[i][1] for i in range(len(r)))) / 2

CELL = 64.0
class Grid:
    def __init__(self): self.c = {}
    def add(self, i, xs, zs, pad=0.0):
        for cx in range(int((min(xs) - pad) // CELL), int((max(xs) + pad) // CELL) + 1):
            for cz in range(int((min(zs) - pad) // CELL), int((max(zs) + pad) // CELL) + 1):
                self.c.setdefault((cx, cz), []).append(i)
    def near(self, q):
        return self.c.get((int(q[0] // CELL), int(q[1] // CELL)), [])

BLD = [pts(b["footprint"]) for b in M["buildings"]]
GB = Grid()
for i, r in enumerate(BLD): GB.add(i, [p[0] for p in r], [p[1] for p in r], 12)
WATER = [pts(w["outline"]) for w in M["water"]]
GW = Grid()
for i, r in enumerate(WATER): GW.add(i, [p[0] for p in r], [p[1] for p in r], 4)
ROADS = [(pts(r["points"]), r["width"], r.get("name") or "", r["class"]) for r in M["roads"]]
GR = Grid()
for i, (p, w, nm, c) in enumerate(ROADS):
    if len(p) > 1: GR.add(i, [a[0] for a in p], [a[1] for a in p], w + 10)
BR = [(pts(b["points"]), b["width"]) for b in M["bridges"]]
GBR = Grid()
for i, (p, w) in enumerate(BR):
    if len(p) > 1: GBR.add(i, [a[0] for a in p], [a[1] for a in p], w + 4)
WOODS = [pts(w["outline"]) for w in M["woods"]]
PARK = [pts(p["outline"]) for p in M["parking"]]
TRAILS = [(pts(t["points"]), t.get("name") or "", t.get("width", 2)) for t in M["trails"]]

def bdist(q):
    best = 99.0
    for i in GB.near(q):
        r = BLD[i]
        if inside(q, r): return 0.0
        best = min(best, min(seg_dist(q, r[k], r[(k + 1) % len(r)]) for k in range(len(r))))
    return best
def wet(q): return any(inside(q, WATER[i]) for i in GW.near(q))
def rdist(q, skip_service=False):
    best = 99.0
    for i in GR.near(q):
        p, w, nm, c = ROADS[i]
        if skip_service and c == "service": continue
        for a, b in zip(p[:-1], p[1:]):
            best = min(best, seg_dist(q, a, b) - w / 2)
    return best
def under_deck(q):
    for i in GBR.near(q):
        p, w = BR[i]
        if any(seg_dist(q, a, b) < w / 2 for a, b in zip(p[:-1], p[1:])): return True
    return False
def ok(q, clear=3.0, max_slope=0.22, road_clear=1.0, deck_ok=False):
    return (bdist(q) > clear and not wet(q) and slope(q) < max_slope and rdist(q) > road_clear and (deck_ok or not under_deck(q)))

# ---- the eggs
keep = [e for e in EG["eggs"] if int(e["id"].split("-")[1]) <= 100]
assert len(keep) == 100, f"expected Round 48's 100 eggs, found {len(keep)}"
eggs = []
def add(area, how, kind, q, note, radius=2.8):
    if any(math.dist((q[0], q[1]), (o["x"], o["z"])) < 30 for o in keep + eggs): return False
    eggs.append({"id": f"egg-{100 + len(eggs) + 1:03d}", "area": area, "how": how, "kind": kind, "x": round(q[0], 2), "z": round(q[1], 2),
                 "y": 0.0, "radius": radius, "note": note})
    return True

def in_box(q, box): return box[0] <= q[0] <= box[2] and box[1] <= q[1] <= box[3]
def road_ids(box, classes=None, names=None):
    out = []
    for i, (p, w, nm, c) in enumerate(ROADS):
        if len(p) < 2 or c in ("motorway", "motorway_link", "service" if classes is None else "-"): continue
        if classes and c not in classes: continue
        if names and not any(n in nm for n in names): continue
        if any(in_box(a, box) for a in p): out.append(i)
    return out

def street(area, ids, n, note, box=None, tries=600):
    got = 0
    for _ in range(tries):
        if got >= n or not ids: break
        p, w, nm, c = ROADS[rng.choice(ids)]
        k = rng.randrange(len(p) - 1); a, b = p[k], p[k + 1]; L = math.dist(a, b)
        if L < 2: continue
        t = rng.random(); cpt = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
        if box and not in_box(cpt, box): continue
        nx, nz = -(b[1] - a[1]) / L, (b[0] - a[0]) / L; s = rng.choice((-1, 1))
        q = (cpt[0] + nx * s * (w / 2 + 1.5), cpt[1] + nz * s * (w / 2 + 1.5))
        if ok(q, 4.0, 0.18, 0.8) and add(area, "easy", "ground", q, f"{note}: by {nm or 'the road'}"): got += 1
    return got

def yards(area, box, n, note, max_area=450, tries=1500):
    houses = [i for i, r in enumerate(BLD) if 60 < area_of(r) < max_area and in_box(centroid(r), box) and M["buildings"][i]["type"] in ("house", "residential", "detached", "yes", "")]
    got = 0
    for _ in range(tries):
        if got >= n or not houses: break
        h = rng.choice(houses); r = BLD[h]; c = centroid(r)
        near = None
        for i in GR.near(c):
            p, w, nm, cl = ROADS[i]
            if cl in ("service", "motorway", "motorway_link"): continue
            for a, b in zip(p[:-1], p[1:]):
                d = seg_dist(c, a, b)
                if near is None or d < near[0]: near = (d, a, b)
        if near is None or near[0] > 70: continue
        _, a, b = near; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9
        t = max(0, min(1, ((c[0] - a[0]) * ab[0] + (c[1] - a[1]) * ab[1]) / L2)); f = (a[0] + ab[0] * t, a[1] + ab[1] * t)
        d = (c[0] - f[0], c[1] - f[1]); L = math.hypot(*d)
        if L < 1: continue
        d = (d[0] / L, d[1] / L); half = max(abs((p[0] - c[0]) * d[0] + (p[1] - c[1]) * d[1]) for p in r)
        q = (c[0] + d[0] * (half + 7.0), c[1] + d[1] * (half + 7.0))
        if ok(q, 3.0, 0.2, 8.0) and add(area, "hard", "ground", q, f"{note}: behind a house"): got += 1
    return got

def lots(area, box, n, note, how="easy", tries=400):
    cand = [r for r in PARK if in_box(centroid(r), box) and area_of(r) > 600]
    rng.shuffle(cand); got = 0
    for r in cand:
        if got >= n: break
        for _ in range(8):
            c = centroid(r); q = (c[0] + rng.uniform(-8, 8), c[1] + rng.uniform(-8, 8))
            if inside(q, r) and ok(q, 4.0, 0.15, -99.0) and add(area, how, "ground", q, f"{note}: in a parking lot"): got += 1; break
    return got

def woods(area, box, n, note, tries=3000):
    cand = [r for r in WOODS if any(in_box(p, box) for p in r)]
    got = 0
    for _ in range(tries):
        if got >= n or not cand: break
        r = rng.choice(cand); xs = [p[0] for p in r]; zs = [p[1] for p in r]
        q = (rng.uniform(max(min(xs), box[0]), min(max(xs), box[2])), rng.uniform(max(min(zs), box[1]), min(max(zs), box[3])))
        if inside(q, r) and ok(q, 5.0, 0.2, 25.0) and rdist(q) < 140 and add(area, "hard", "ground", q, f"{note}: in the woods"): got += 1
    return got

def trail(area, names, n, note, box=None, how="hard"):
    lines = [(p, nm) for p, nm, w in TRAILS if any(x in nm for x in names) and len(p) > 1 and (box is None or any(in_box(a, box) for a in p))]
    got = 0
    for _ in range(800):
        if got >= n or not lines: break
        p, nm = rng.choice(lines); q = p[rng.randrange(len(p))]
        if ok(q, 4.0, 0.25, 6.0) and add(area, how, "ground", q, f"{note}: on {nm}"): got += 1
    return got

def deck_roofs(area, box, n, note):
    got = 0
    for d in J("Data/tracks/kennestone.json")["decks"]:
        if got >= n: break
        c = d["centre"]
        if in_box(c, box) and add(area, "hard", "ground", (c[0] + d["u"][0] * d["hl"] * 0.4, c[1] + d["u"][1] * d["hl"] * 0.4), f"{note}: on the roof of a {d['levels']}-level parking deck"): got += 1
    return got

def under_highway(area, box, n, note):
    """under a highway's deck, where a road runs beneath it (the lowest surface: the road's verge)"""
    got = 0
    order = list(range(len(BR))); rng.shuffle(order)
    for i in order:
        if got >= n: break
        b = M["bridges"][i]
        if b.get("class") not in ("motorway", "trunk") or not b.get("points"): continue
        p = BR[i][0]; mid = p[len(p) // 2]
        if not in_box(mid, box): continue
        # a road crossing under the deck's middle third
        for j in GR.near(mid):
            rp, w, nm, c = ROADS[j]
            if c in ("motorway", "motorway_link") or ROADS[j][0] is None: continue
            hit = None
            for a, b2 in zip(rp[:-1], rp[1:]):
                if seg_dist(mid, a, b2) < BR[i][1] / 2 + 2: hit = (a, b2); break
            if not hit: continue
            a, b2 = hit; L = math.dist(a, b2) or 1; nx, nz = -(b2[1] - a[1]) / L, (b2[0] - a[0]) / L
            ab = (b2[0] - a[0], b2[1] - a[1]); t = max(0, min(1, ((mid[0] - a[0]) * ab[0] + (mid[1] - a[1]) * ab[1]) / (L * L)))
            q = (a[0] + ab[0] * t + nx * (w / 2 + 1.2), a[1] + ab[1] * t + nz * (w / 2 + 1.2))
            if bdist(q) > 3 and not wet(q) and add(area, "hard", "low", q, f"{note}: under {b.get('name') or 'the highway'}, beside {nm or 'the road'}"): got += 1
            break
    return got

def near_points(area, how, kind, pts_, n, note, clear=3.0, road_clear=-99.0, max_slope=0.25, deck_ok=False, jitter=0.0):
    got = 0
    for q in pts_:
        if got >= n: break
        for _ in range(6):
            qq = (q[0] + rng.uniform(-jitter, jitter), q[1] + rng.uniform(-jitter, jitter)) if jitter else q
            if (kind == "low" or ok(qq, clear, max_slope, road_clear, deck_ok)) and add(area, how, kind, qq, note): got += 1; break
    return got

AREAS = []
def area(name, direction): AREAS.append({"name": name, "direction": direction}); return name
def ring_pts(flat):
    r = pts(flat); return r

# ================================================================== the areas (150)
# Town Center at Cobb (16): inside the mall on the trail race's own line (low), its lots, Ring Road, the decks' roofs
TC = (-450.0, -1800.0, 1100.0, -300.0)
A = area("Town Center at Cobb", "South-west, the mall at Barrett Parkway and I-75: inside on both levels, its lots, Ring Road and the parking decks")
MALL = [BLD[i] for i in range(2389, 2395)]
tr = J("Data/routes/noonday_creek_trail.json"); L = pts(tr["loop"])
inmall = [q for k, q in enumerate(L) if k % 9 == 0 and any(inside(q, r) for r in MALL)]
rng.shuffle(inmall)
near_points(A, "hard", "low", inmall, 6, "Town Center: inside the mall, on the race's way through")
deck_roofs(A, TC, 2, "Town Center")
lots(A, TC, 4, "Town Center")
street(A, road_ids(TC, names=("Ring Road", "Town Center", "Mall Boulevard", "Barrett")), 4, "Town Center", TC)

# Kennesaw State University (10)
KSU = (-2300.0, 450.0, -1150.0, 1750.0)
A = area("Kennesaw State University", "North-west, the campus off Chastain Road and Frey Road: its loop roads, lots and parking decks")
street(A, road_ids(KSU), 5, "KSU", KSU)
lots(A, KSU, 2, "KSU", "easy")
deck_roofs(A, KSU, 3, "KSU")

# Cobb County International Airport (8)
AP = [a for a in M["aeroways"]]
def apts(a): return pts(a.get("outline") or a.get("points"))
xs = [p[0] for a in AP for p in apts(a)]; zs = [p[1] for a in AP for p in apts(a)]
APB = (min(xs) - 300, min(zs) - 300, max(xs) + 300, max(zs) + 300)
A = area("The airport", "Cobb County International Airport, west of I-75: the aprons, taxiways, hangars and helipads")
apron = []
for a in AP:
    r = apts(a)
    if a["kind"] == "taxiway": apron += r[:: max(1, len(r) // 3)]; continue
    if a["kind"] in ("apron", "helipad"):
        c = centroid(r)
        if inside(c, r): apron.append(c)
        else: apron += r[:: max(1, len(r) // 4)]
rng.shuffle(apron)
near_points(A, "easy", "ground", apron, 4, "the airport: on the apron and taxiways", clear=6.0, max_slope=0.12, jitter=6.0)
street(A, road_ids(APB), 2, "the airport", APB)
yards(A, APB, 2, "near the airport")

# Viking Quarry (8): the jumps' landings (reached by the jump), the access drive, the overlook
A = area("Viking Quarry", "The quarry lake west of the airport: its three jumps (eggs on their landings), the gantry and the drive in")
Q = J("Data/tracks/quarry_jumps.json")
for z in Q["landing_zones"]:
    r = pts(z["poly"]); c = centroid(r)
    add(A, "hard", "ground", c, f"Viking Quarry: by {z['id'].replace('quarry jump ', '')}'s landing", 3.5)
dr = pts(Q["access"]["drive"]); near_points(A, "easy", "ground", dr[:: max(1, len(dr) // 6)], 3, "Viking Quarry: by the drive in", clear=4.0, jitter=4.0)
QB = (min(p[0] for p in dr) - 400, min(p[1] for p in dr) - 400, max(p[0] for p in dr) + 400, max(p[1] for p in dr) + 400)
woods(A, QB, 2, "Viking Quarry")

# Kennesaw Mountain (14)
KM = (-2200.0, -6700.0, 300.0, -4600.0)
A = area("Kennesaw Mountain", "South-west, the mountain: up the drive, the summit, the visitor center, its trails and the race's big jumps")
SL = J("Data/tracks/kennesaw_summit_leap.json")
for z in SL["landing_zones"]:
    r = pts(z["poly"]) if "poly" in z else None
    if r: add(A, "hard", "ground", centroid(r), "Kennesaw Mountain: on the summit leap's landing (do the leap)", 3.5)
KMJ = J("Data/tracks/kennesaw_mountain.json")
for j in KMJ["jumps"]:
    s = (j["catch_from_m"] + j["catch_to_m"]) / 2
    q = (j["lip"][0] + j["dir"][0] * s, j["lip"][1] + j["dir"][1] * s)
    add(A, "hard", "ground", q, f"Kennesaw Mountain: on {j['name']}'s catch slope", 3.5)
street(A, road_ids(KM, names=("Kennesaw Mountain", "Old Mountain", "Burnt Hickory", "Stilesboro")), 4, "Kennesaw Mountain", KM)
trail(A, ("Kennesaw Mountain Trail", "Little Kennesaw", "Pigeon Hill", "Gun Battery", "Visitor Center"), 4, "Kennesaw Mountain", KM)
woods(A, KM, 2, "Kennesaw Mountain")

# Marietta Square (12)
MS = (1250.0, -9000.0, 2350.0, -7950.0)
A = area("Marietta Square", "South, the old town square: Glover Park, the streets round it, the depot and the railroad, the race's jumps")
MSQ = J("Data/tracks/marietta_square.json")
park = pts(MSQ["park"]) if isinstance(MSQ["park"][0], (int, float)) else [tuple(p) for p in MSQ["park"]]
pc = MSQ["park_center"]
near_points(A, "easy", "ground", [(pc[0] + 18 * math.cos(a), pc[1] + 18 * math.sin(a)) for a in (0.4, 2.5, 4.4)], 2, "Marietta Square: in Glover Park", clear=3.0, road_clear=2.0)
street(A, road_ids(MS, names=("Park Square", "Church Street", "Cherokee", "Roswell", "Atlanta", "Powder Springs", "Whitlock", "Mill Street", "Polk")), 5, "Marietta Square", MS)
MR = J("Data/tracks/marietta_square_race.json")
for z in MR["landing_zones"]:
    if "poly" in z: add(A, "hard", "ground", centroid(pts(z["poly"])), "Marietta Square: on a race jump's landing", 3.5)
deck_roofs(A, MS, 2, "Marietta Square")
yards(A, MS, 2, "Marietta Square")

# Kennestone (8)
KN = (1150.0, -7050.0, 2000.0, -6350.0)
A = area("Kennestone", "South, the hospital on Church Street: the ER drive, the portico, the skybridges and its parking decks")
er = J("Data/tracks/kennestone.json")["er"]["entrances"]
near_points(A, "easy", "ground", [(e["x"], e["z"]) for e in er], 2, "Kennestone: by the ER", clear=4.0, jitter=8.0)
deck_roofs(A, KN, 3, "Kennestone")
lots(A, KN, 1, "Kennestone")
street(A, road_ids(KN), 2, "Kennestone", KN)

# Noonday Creek Trail (10)
NT = J("Data/tracks/noonday_creek_trail.json")
tl = pts(NT["trail"]["line"])
TB = (min(p[0] for p in tl) - 60, min(p[1] for p in tl) - 60, max(p[0] for p in tl) + 60, max(p[1] for p in tl) + 60)
A = area("Noonday Creek Trail", "The paved trail round Town Center: under I-575, the red footbridge, the trailhead and the woods beside it")
idx = list(range(0, len(tl), 7)); rng.shuffle(idx)
near_points(A, "easy", "ground", [tl[k] for k in idx], 5, "the Noonday Creek Trail: on the trail", clear=3.0, max_slope=0.3, deck_ok=True)
for z in NT["landing_zones"][:1]:
    if "poly" in z: add(A, "hard", "ground", centroid(pts(z["poly"])), "the Noonday Creek Trail: on the creek leap's landing", 3.5)
woods(A, TB, 4, "the Noonday Creek Trail")

# Addison Heights (10)
AH = (2600.0, 60.0, 3320.0, 560.0)
A = area("Addison Heights", "East of Canton Road, the steep streets of the Addison Heights race: the terrace, the bounce house, the yards")
AT = J("Data/tracks/addison_heights.json")
for z in AT["landing_zones"]:
    if "poly" in z: add(A, "hard", "ground", centroid(pts(z["poly"])), f"Addison Heights: on {z.get('id', 'a jump')}'s landing", 3.5)
for key in ("terrace", "bounce_house"):
    d = AT.get(key) or {}
    q = d.get("at") or d.get("center") or d.get("centre") or (d.get("x"), d.get("z"))
    if q and q[0] is not None: near_points(A, "hard", "ground", [tuple(q[:2])], 1, f"Addison Heights: at the {key.replace('_', ' ')}", clear=2.0, jitter=6.0)
street(A, road_ids(AH), 4, "Addison Heights", AH)
yards(A, AH, 4, "Addison Heights")

# the highways (10): beside them on the frontage, under the decks
HW = (-8600.0, -9500.0, 3980.0, 1900.0)
A = area("The highways", "I-75, I-575 and the Express Lanes: under the bridges where roads cross, and beside the ramps")
under_highway(A, (-2000, -5000, 3900, 1900), 6, "the highways")
ramps = [i for i, (p, w, nm, c) in enumerate(ROADS) if c == "motorway_link" and len(p) > 1]
got = 0
for _ in range(400):
    if got >= 4: break
    p, w, nm, c = ROADS[rng.choice(ramps)]; k = rng.randrange(len(p) - 1); a, b = p[k], p[k + 1]; L = math.dist(a, b)
    if L < 3: continue
    nx, nz = -(b[1] - a[1]) / L, (b[0] - a[0]) / L; s = rng.choice((-1, 1)); m_ = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    q = (m_[0] + nx * s * (w / 2 + 2.0), m_[1] + nz * s * (w / 2 + 2.0))
    if ok(q, 4.0, 0.15, 1.5) and add(A, "easy", "ground", q, "the highways: beside a ramp"): got += 1

# the neighborhoods of the extension (34): west (Kennesaw, stage 2), the Barrett / Big Shanty middle (stage 1), south (Marietta, stage 3)
WN = (-8600.0, -4500.0, -2300.0, 1900.0)
A = area("West neighborhoods", "West of the campus and the quarry, out to the map's west edge: Kennesaw's streets and back yards")
street(A, road_ids(WN, ("residential",)), 6, "the west neighborhoods", WN); yards(A, WN, 6, "the west neighborhoods")
BN = (-2300.0, -4600.0, 3980.0, 0.0)
A = area("Middle neighborhoods", "South of Chastain Road to the mountain and Marietta: off Barrett Parkway, Big Shanty Road, Bells Ferry's south end and Canton Road")
street(A, road_ids(BN, ("residential",)), 6, "the middle neighborhoods", BN); yards(A, BN, 5, "the middle neighborhoods")
SN = (-8600.0, -9600.0, 3980.0, -4600.0)
A = area("Marietta neighborhoods", "South of the mountain, round Marietta and Kennestone, out to the map's south edge")
street(A, road_ids(SN, ("residential",)), 6, "the south neighborhoods", SN); yards(A, SN, 5, "the south neighborhoods")

# the woods (6 + top-up): out in the extension's woods
A = area("The woods", "The extension's woods: between the neighborhoods, along the creeks and round the mountain")
woods(A, (-8600, -9600, 3980, 0), 6, "the extension")

# top up to 150 from the bigger areas (by a road), trim to 150
order = ["West neighborhoods", "Middle neighborhoods", "Marietta neighborhoods", "The woods"]
boxes = {"West neighborhoods": WN, "Middle neighborhoods": BN, "Marietta neighborhoods": SN, "The woods": (-8600, -9600, 3980, 0)}
k = 0
while len(eggs) < 150 and k < 400:
    nm = order[k % len(order)]; k += 1
    if nm == "The woods": woods(nm, boxes[nm], 1, "the extension")
    else: street(nm, road_ids(boxes[nm], ("residential", "unclassified")), 1, nm.split(" (")[0].lower(), boxes[nm])
eggs = eggs[:150]
for i, e in enumerate(eggs): e["id"] = f"egg-{101 + i:03d}"

# ---- --fix <check report>: the eggs the exe's probe could not reach (lines "  egg-NNN ... <- WRONG") moved, each to a nearer, open spot of the same
# area and kind of finding, its id kept; every other egg stays exactly where it is (the placement above is deterministic)
if "--fix" in sys.argv:
    import re
    rep = open(sys.argv[sys.argv.index("--fix") + 1], encoding="utf-8").read()
    wrong = set(re.findall(r"^  (egg-\d+) .*<- WRONG", rep, re.M))
    BOXES = {"Town Center at Cobb": TC, "Kennesaw State University": KSU, "The airport": APB, "Viking Quarry": QB, "Kennesaw Mountain": KM,
             "Marietta Square": MS, "Kennestone": KN, "Noonday Creek Trail": TB, "Addison Heights": AH, "The highways": (-2000, -5000, 3900, 1900),
             "West neighborhoods": WN, "Middle neighborhoods": BN, "Marietta neighborhoods": SN, "The woods": (-8600, -9600, 3980, 0)}
    rejected = [(e["x"], e["z"]) for e in eggs if e["id"] in wrong]
    def free(q): return all(math.dist(q, r) > 40 for r in rejected)
    def near_yard(area_, box):
        """a back yard close to its street: 4.5 m past the wall, the house within 35 m of the road"""
        houses = [i for i, r in enumerate(BLD) if 60 < area_of(r) < 450 and in_box(centroid(r), box)]
        for _ in range(3000):
            if not houses: return None
            h = rng.choice(houses); r = BLD[h]; c = centroid(r); near = None
            for i in GR.near(c):
                p, w, nm, cl = ROADS[i]
                if cl in ("service", "motorway", "motorway_link"): continue
                for a, b in zip(p[:-1], p[1:]):
                    d = seg_dist(c, a, b)
                    if near is None or d < near[0]: near = (d, a, b)
            if near is None or near[0] > 35: continue
            _, a, b = near; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9
            t = max(0, min(1, ((c[0] - a[0]) * ab[0] + (c[1] - a[1]) * ab[1]) / L2)); f = (a[0] + ab[0] * t, a[1] + ab[1] * t)
            d = (c[0] - f[0], c[1] - f[1]); L = math.hypot(*d)
            if L < 1: continue
            d = (d[0] / L, d[1] / L); half = max(abs((p[0] - c[0]) * d[0] + (p[1] - c[1]) * d[1]) for p in r)
            q = (c[0] + d[0] * (half + 4.5), c[1] + d[1] * (half + 4.5))
            if free(q) and ok(q, 3.0, 0.12, 6.0) and not any(math.dist(q, (o["x"], o["z"])) < 30 for o in keep + eggs): return q, "behind a house"
        return None
    def near_road_side(box):
        ids = road_ids(box)
        for _ in range(2000):
            if not ids: return None
            p, w, nm, c = ROADS[rng.choice(ids)]
            k = rng.randrange(len(p) - 1); a, b = p[k], p[k + 1]; L = math.dist(a, b)
            if L < 2: continue
            t = rng.random(); cpt = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
            if not in_box(cpt, box): continue
            nx, nz = -(b[1] - a[1]) / L, (b[0] - a[0]) / L; s = rng.choice((-1, 1))
            q = (cpt[0] + nx * s * (w / 2 + 1.5), cpt[1] + nz * s * (w / 2 + 1.5))
            if free(q) and ok(q, 5.0, 0.10, 0.8) and not any(math.dist(q, (o["x"], o["z"])) < 30 for o in keep + eggs): return q, f"by {nm or 'the road'}"
        return None
    def near_woods(box):
        cand = [r for r in WOODS if any(in_box(p, box) for p in r)]
        for _ in range(4000):
            if not cand: return None
            r = rng.choice(cand); xs_ = [p[0] for p in r]; zs_ = [p[1] for p in r]
            q = (rng.uniform(max(min(xs_), box[0]), min(max(xs_), box[2])), rng.uniform(max(min(zs_), box[1]), min(max(zs_), box[3])))
            if inside(q, r) and free(q) and ok(q, 5.0, 0.12, 10.0) and rdist(q) < 30 and not any(math.dist(q, (o["x"], o["z"])) < 30 for o in keep + eggs): return q, "in the woods, off the road"
        return None
    moved = 0
    for e in eggs:
        if e["id"] not in wrong: continue
        box = BOXES.get(e["area"]); got = None
        if "summit leap" in e["note"]:
            tr_ = pts(SL["leap"]["track"]); got = (tr_[min(len(tr_) - 1, 12)], "past the summit leap, on its run-out")
        elif "Glover Park" in e["note"]:
            got = near_road_side(box)
        elif e["how"] == "easy":
            got = near_road_side(box)
        elif "woods" in e["note"]:
            got = near_woods(box) or near_yard(e["area"], box)
        else:
            got = near_yard(e["area"], box) or near_woods(box)
        if got is None: print("  no new spot for", e["id"], e["area"]); continue
        q, why = got; e["x"], e["z"], e["kind"] = round(q[0], 2), round(q[1], 2), "ground"
        e["note"] = f"{e['note'].split(':')[0]}: {why} (moved, Round 89's probe)"; moved += 1
    print(f"--fix: {moved} of {len(wrong)} eggs moved")

# ---- the file: Round 48's 100 + these; areas (old ones kept with their counts, new ones added); destinations; tiers 250
names = {a["name"] for a in AREAS}
old_areas = [a for a in EG["areas"] if a["name"] not in names]
counts = {}
for e in eggs: counts.setdefault(e["area"], [0, 0])[0 if e["how"] == "easy" else 1] += 1
for a_ in AREAS:
    c = counts.get(a_["name"], [0, 0]); a_["eggs"] = c[0] + c[1]
    print(f"  {a_['name']:40} {c[0] + c[1]:3} eggs ({c[0]} easy, {c[1]} hard)")
new_areas = [a for a in AREAS if a["eggs"] > 0]

# new destinations (fast travel): one per new area that has none near it, on its nearest road
def road_point(q, maxd=300):
    best = None
    for i in GR.near(q) + [j for j in range(0)]:
        p, w, nm, c = ROADS[i]
        if c in ("motorway", "motorway_link", "service"): continue
        for a, b in zip(p[:-1], p[1:]):
            d = seg_dist(q, a, b)
            if best is None or d < best[0]:
                ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9; t = max(0, min(1, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
                best = (d, (a[0] + ab[0] * t, a[1] + ab[1] * t), math.degrees(math.atan2(ab[0], ab[1])) % 360)
    if best is None:
        # wider search
        for i, (p, w, nm, c) in enumerate(ROADS):
            if c in ("motorway", "motorway_link", "service") or len(p) < 2: continue
            for a, b in zip(p[:-1], p[1:]):
                if abs(a[0] - q[0]) > maxd or abs(a[1] - q[1]) > maxd: continue
                d = seg_dist(q, a, b)
                if best is None or d < best[0]:
                    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9; t = max(0, min(1, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
                    best = (d, (a[0] + ab[0] * t, a[1] + ab[1] * t), math.degrees(math.atan2(ab[0], ab[1])) % 360)
    return best
MINE = ["airport", "kennestone", "addison-heights", "noonday-creek-trailhead", "west-kennesaw", "middle-neighborhoods", "marietta-south", "highways-i575-merge"]
dests = [d for d in EG["destinations"] if d["id"] not in MINE]
have = [(d["x"], d["z"]) for d in dests]
def dest(did, title, q):
    rp = road_point(q)
    if rp is None: print("  no road for destination", did); return
    if any(math.dist(rp[1], h) < 250 for h in have): print(f"  destination {did}: one within 250 m already, left"); return
    dests.append({"id": did, "title": title, "x": round(rp[1][0], 2), "z": round(rp[1][1], 2), "y": 0.0, "fixed": False, "yaw": round(rp[2], 1)}); have.append(rp[1])
dest("airport", "Cobb County International Airport", centroid([p for a in AP if a["kind"] == "apron" for p in apts(a)][:40]))
dest("kennestone", "Kennestone Hospital", (er[0]["x"], er[0]["z"]))
dest("addison-heights", "Addison Heights", centroid(pts(J("Data/routes/addison_heights.json")["loop"])))
dest("noonday-creek-trailhead", "Noonday Creek trailhead", tl[-1])
def mid_egg(nm):
    e = [x for x in eggs if x["area"] == nm]
    return (sum(x["x"] for x in e) / len(e), sum(x["z"] for x in e) / len(e)) if e else None
for did, title, nm in (("west-kennesaw", "West Kennesaw neighborhoods", "West neighborhoods"), ("middle-neighborhoods", "Middle neighborhoods", "Middle neighborhoods"),
                       ("marietta-south", "South Marietta neighborhoods", "Marietta neighborhoods")):
    q = mid_egg(nm)
    if q:
        # the egg nearest the middle (a point in the area, not between its parts)
        e = min((x for x in eggs if x["area"] == nm), key=lambda x: math.dist((x["x"], x["z"]), q)); dest(did, title, (e["x"], e["z"]))

out = dict(EG)
out["note"] = EG["note"].split(" Round 89:")[0] + " Round 89: 150 more over the map's extension (Tools/mapgen/make_eggs_more.py), 250 in all."
out["tiers"] = [10, 25, 60, 125, 190, 250]   # 10 and 25 kept (a save with them keeps them), then the 100-egg steps scaled to 250
out["areas"] = old_areas + new_areas
out["eggs"] = keep + eggs
out["destinations"] = dests
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
easy = sum(1 for e in eggs if e["how"] == "easy")
print(f"{len(eggs)} new eggs ({easy} easy, {len(eggs) - easy} hard; kinds {dict((k, sum(1 for e in eggs if e['kind'] == k)) for k in ('ground', 'low', 'fixed'))}), "
      f"{len(keep) + len(eggs)} in all, {len(out['areas'])} areas, {len(dests)} destinations; tiers {out['tiers']}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(18, 15))
    for p, w, nm, c in ROADS:
        if c in ("motorway", "trunk", "primary", "secondary", "tertiary"): ax.plot(*zip(*p), color="#bbb", lw=0.5)
    cols = {}; palette = ["#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#a65628", "#f781bf", "#999999", "#66c2a5", "#e6ab02", "#1b9e77", "#d95f02", "#7570b3", "#e7298a", "#66a61e"]
    for e in keep: ax.plot(e["x"], e["z"], ".", color="#444", ms=4)
    for e in eggs:
        c = cols.setdefault(e["area"], palette[len(cols) % len(palette)])
        ax.plot(e["x"], e["z"], "o" if e["how"] == "easy" else "^", color=c, ms=6)
    for d in dests: ax.plot(d["x"], d["z"], "k*", ms=9)
    for nm, c in cols.items(): ax.plot([], [], "o", color=c, label=nm)
    ax.legend(loc="lower left", fontsize=8); ax.set_aspect("equal")
    os.makedirs(os.path.join(ROOT, "local/round89"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round89/eggs_more.png"), dpi=60, bbox_inches="tight"); print("picture: local/round89/eggs_more.png")
