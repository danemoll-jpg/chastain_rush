"""Round 92 (F, Dan, 2026-11-02 01:40: "a race that you could race as a plane or a car. The estimated time for both should be about the same and should use much
of the same road ... planes could have things like forcing them to go a longer path and have cps under bridges to force them to fly more precisely and having to
lower speed whereas cars could have underground shortcuts using the underground tube system"): WINGS & WHEELS, the car lap.
One lap of about 5 km between Marietta Square and Kennestone (Cherokee Street is one-way north, Church Street one-way south), from the line on Cherokee Street:
  1. north up Cherokee Street to Tower Road, west along Tower Road (divided: the westbound carriageway) and round by North Avenue
  2. east along Tower Road's eastbound carriageway past Kennestone, south down Church Street to the square, east on Hansell Street to Cherokee Street and the line
THE TUBE (official shortcut): off Tower Road onto the grass to the Kennestone exit's mouth (Round 91 C, X_KEN), down it into B41, through junction J25, the
length of M7d (its express run) under Church Street to J26 under the square, up B42 and out of the manhole in Root Street (MH26), round onto Hansell Street.
It skips Church Street; the pipe ride is quick when the express carries you, but the turn-off is a tight loop on grass to a mouth that faces away.
The plane's lap (make_wings_air_route.py) flies the same streets the long way round. The two are one race (route "twin"): race setup's RACE TYPE row picks
which one you race; the AI field mixes cars and planes (route "mixed_ai").
Hand choices are the constants below; the lap is found on map.json's roads by name, so it re-runs after a mapgen run and after pipe_network.py.
Writes Data/routes/wings_and_wheels.json; --plot draws local/round92/wings_route.png. Then: python Tools/race/fork_closures.py; a quick build.
Usage: python Tools/mapgen/make_wings_route.py [--plot]"""
import heapq, json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/wings_and_wheels.json")
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]
NET = json.load(open(os.path.join(ROOT, "Data/pipes/network.json"), encoding="utf-8"))
_ext = {}
for key, name in (("full", "terrain_full"), ("piece", "terrain_piece3")):
    mt = json.load(open(os.path.join(ROOT, f"Assets/Generated/{name}.json")))
    _ext[key] = (mt, np.memmap(os.path.join(ROOT, f"Assets/Generated/{name}.r16"), dtype="<u2", mode="r", shape=(mt["grid_z"], mt["grid_x"])))


def ground(x, z):
    for key in ("piece", "full"):
        mt, arr = _ext[key]
        fx, fz = (x - mt["origin_x_m"]) / mt["step_x_m"], (z - mt["origin_z_m"]) / mt["step_z_m"]
        if not (0 <= fx < mt["grid_x"] - 1 and 0 <= fz < mt["grid_z"] - 1): continue
        i, j = int(fx), int(fz); a, b = fx - i, fz - j
        h = float(arr[j, i]) * (1 - a) * (1 - b) + float(arr[j, i + 1]) * a * (1 - b) + float(arr[j + 1, i]) * (1 - a) * b + float(arr[j + 1, i + 1]) * a * b
        return h / 65535.0 * (mt["max_height"] - mt["min_height"]) + mt["min_height"]
    return 300.0


def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
def flat(p): return [round(v, 2) for q in p for v in q]
def cum(p):
    c = [0.0]
    for a, b in zip(p[:-1], p[1:]): c.append(c[-1] + math.dist(a, b))
    return c
def point_at(line, s, c=None):
    c = c or cum(line); s = s % c[-1]
    i = max(0, min(len(line) - 2, int(np.searchsorted(c, s) - 1)))
    t = (s - c[i]) / max(1e-9, c[i + 1] - c[i]); a, b = line[i], line[i + 1]
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
def nearest_s(p, q):
    c = cum(p); best = (1e18, 0.0)
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-12
        t = max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2)); d = math.hypot(a[0] + ab[0] * t - q[0], a[1] + ab[1] * t - q[1])
        if d < best[0]: best = (d, c[i] + t * (c[i + 1] - c[i]))
    return best
def resample(p, step):
    c = cum(p); n = max(2, int(c[-1] / step) + 1)
    return [point_at(p, c[-1] * k / (n - 1), c) for k in range(n - 1)] + [p[-1]]
def chaikin(p, it=2):
    for _ in range(it):
        q = [p[0]]
        for a, b in zip(p[:-1], p[1:]): q += [(0.75 * a[0] + 0.25 * b[0], 0.75 * a[1] + 0.25 * b[1]), (0.25 * a[0] + 0.75 * b[0], 0.25 * a[1] + 0.75 * b[1])]
        q.append(p[-1]); p = q
    return p
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)


SETTINGS = {
    "name": "Wings & Wheels",
    "direction": "forward",
    "directions": ["forward"],
    "laps": 3,
    "traffic": False,
    "records_as": "Wings & Wheels (cars)",
    "twin": "wings_and_wheels_air.json",
    "mixed_ai": True,
    "pipe_express": True,
    "gate_half_width_m": 12.0,
    "checkpoint_half_width_m": 12.0,
    "gate_below_m": 6.0,
    "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    "closure_clear_m": 12.0,
    "closure_extra_width_m": 6.0,
}
BOX = (1050.0, 2150.0, -8650.0, -6300.0)
# the corners, in order (each leg on the streets named, found on the road graph, one-way pieces respected)
LEGS = [
    ((1801.0, -8170.0), (1880.0, -7000.0), {"Cherokee Street Northeast"}),
    ((1880.0, -7000.0), (1450.0, -6508.0), {"Cherokee Street Northeast", "(link)", "Church Street Northwest", "Tower Road Northwest", "Tower Road Northeast"}),
    ((1450.0, -6508.0), (1300.0, -6522.0), {"Tower Road Northwest", "Tower Road Northeast", "North Avenue Northwest"}),
    ((1300.0, -6522.0), (1690.0, -7100.0), {"Tower Road Northwest", "Tower Road Northeast", "Church Street Northwest", "(link)"}),
    ((1690.0, -7100.0), (1708.0, -8327.0), {"Church Street Northwest"}),
    ((1708.0, -8327.0), (1801.0, -8170.0), {"Church Street Northwest", "Hansell Street Northeast", "Cherokee Street Northeast"}),
]
GATE_AT = (1801.0, -8120.0)   # the line on Cherokee Street, northbound
LEAD_IN_M = 60.0
CHECKPOINTS = [
    (1840.0, -7590.0, "Cherokee Street"),
    (1870.0, -6800.0, "Cherokee Street north"),
    (1450.0, -6508.0, "Tower Road west"),
    (1222.0, -6509.0, "The U-turn"),   # Round 93 (C2): on the westbound approach 25 m before the apex, so its arch faces you (it stood side-on at the apex)
    (1500.0, -6524.0, "Tower Road east"),
    (1688.0, -7100.0, "Church Street"),
    (1700.0, -7900.0, "Church Street south"),
    (1760.0, -8327.0, "Hansell Street"),
]

# ---------------------------------------------------------------- the road graph (one-way pieces one way)
POS, EDGES = {}, {}
_ids = {}
def nid(q):
    k = (round(q[0] * 2), round(q[1] * 2))
    if k not in _ids: _ids[k] = len(_ids); POS[_ids[k]] = q
    return _ids[k]
for ri, r in enumerate(roads):
    if r["class"] in ("footway", "path", "steps", "pedestrian", "cycleway", "track", "service"): continue
    if not r["name"] and not r["class"].endswith("_link"): continue
    nm_ = r["name"] or "(link)"
    p = pts(r["points"])
    if not any(BOX[0] < x < BOX[1] and BOX[2] < z < BOX[3] for x, z in p): continue
    p = resample(p, 4.0)
    for a, b in zip(p[:-1], p[1:]):
        ia, ib = nid(a), nid(b); w = math.dist(a, b)
        EDGES.setdefault(ia, []).append((ib, w, nm_, r["width"], ri))
        if not r["oneway"]: EDGES.setdefault(ib, []).append((ia, w, nm_, r["width"], ri))
keys = list(POS.items()); arr = np.array([q for _, q in keys])
for ia, qa in keys:
    d = np.hypot(arr[:, 0] - qa[0], arr[:, 1] - qa[1])
    for j in np.nonzero(d < 2.5)[0]:
        ib = keys[j][0]
        if ib != ia: EDGES.setdefault(ia, []).append((ib, float(d[j]), None, 8.0, -1))


def route(a, b, names):
    def near(q): return min((k for k in POS if any(e[2] in names for e in EDGES.get(k, []))), key=lambda k: math.dist(POS[k], q))
    s, t = near(a), near(b)
    dist = {s: 0.0}; prev = {}; pq = [(0.0, s)]
    while pq:
        d, u = heapq.heappop(pq)
        if u == t: break
        if d > dist.get(u, 1e18): continue
        for v, w, nm, wd, ri in EDGES.get(u, []):
            if nm is not None and nm not in names: continue
            nd = d + w
            if nd < dist.get(v, 1e18): dist[v] = nd; prev[v] = (u, wd); heapq.heappush(pq, (nd, v))
    if t not in dist: raise SystemExit(f"no way from {a} to {b} on {names}")
    seq, widths = [t], []
    while seq[-1] != s: u, wd = prev[seq[-1]]; seq.append(u); widths.append(wd)
    seq.reverse(); widths.reverse()
    return [POS[k] for k in seq], [widths[0]] + widths


line, offs, legs_txt = [], [], []
for a, b, names in LEGS:
    p, w = route(a, b, names)
    p = resample(p, 3.0); ww = [w[min(len(w) - 1, int(i * len(w) / len(p)))] for i in range(len(p))]
    legs_txt.append(f"{sorted(names)[0]} {cum(p)[-1]:.0f} m")
    line += p if not line else p[1:]; offs += [x / 4.0 for x in (ww if not offs else ww[1:])]
line[-1] = line[0]; offs[-1] = offs[0]
N = len(line)
of = np.convolve(np.pad(np.array(offs), 6, mode="wrap"), np.ones(13) / 13, mode="valid")[:N]
lap = []
for i in range(N):
    a, b = line[max(0, i - 1)], line[min(N - 1, i + 1)]; t = unit((b[0] - a[0], b[1] - a[1]))
    # a one-way carriageway is driven on its middle (no right-hand offset)
    lap.append((line[i][0] + t[1] * of[i], line[i][1] - t[0] * of[i]))
def dekink(q):
    while True:
        out, drop = [q[0]], False
        for i in range(1, len(q) - 1):
            a, b, c = out[-1], q[i], q[i + 1]
            h1, h2 = math.atan2(b[0] - a[0], b[1] - a[1]), math.atan2(c[0] - b[0], c[1] - b[1])
            if not drop and abs(math.degrees((h2 - h1 + math.pi) % (2 * math.pi) - math.pi)) > 50: drop = True; continue
            out.append(b)
        out.append(q[-1])
        if not drop: return out
        q = out
lap = resample(chaikin(dekink(lap), 2), 3.0); lap[-1] = lap[0]
C = cum(lap); L_lap = C[-1]
print(f"lap {L_lap:.0f} m: " + ", ".join(legs_txt))
gate_s = round(nearest_s(lap, GATE_AT)[1], 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M + k * 2.0) % L_lap, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
cps = []
for x, z, label in CHECKPOINTS:
    d, s = nearest_s(lap, (x, z)); s = round(s, 1); p = point_at(lap, s, C)
    if d > 20: print(f"  WARNING checkpoint {label} is {d:.0f} m off the lap")
    cps.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
cps.sort(key=lambda c: (c["s"] - gate_s) % L_lap)
for i, c in enumerate(cps, 1): c["id"] = i
print(f"gate at s {gate_s}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']:.0f} {c['label']}" for c in cps))
def S(q): return nearest_s(lap, q)[1]
def rel(s): return (s - gate_s) % L_lap

# ---------------------------------------------------------------- THE TUBE: the Kennestone exit to the Root Street manhole
xk = next(e for e in NET["exits"] if e["id"] == "X_KEN"); mh = next(h for h in NET["manholes"] if h["id"] == "MH26")
def tube(tid): t = next(t for t in NET["tubes"] if t["id"] == tid); p = t["pts"]; return [(p[i], p[i + 1], p[i + 2]) for i in range(0, len(p), 3)]
b41 = list(reversed(tube("B41")))   # X_KEN to J25
m7d = tube("M7d")                   # J25 to J26
b42 = tube("B42")                   # J26 up to MH26
A, B = tuple(xk["a"]), tuple(xk["b"]); din = unit((B[0] - A[0], B[1] - A[1]))   # into the mouth
LEAVE_AT = (1300.0, -6509.0)        # off Tower Road's westbound carriageway, before the U-turn at the median
REJOIN_PAST = 25.0
s_off = min((s for s in np.arange(0, L_lap, 3.0) if math.dist(point_at(lap, s, C), LEAVE_AT) < 8.0), key=lambda s: math.dist(point_at(lap, s, C), LEAVE_AT))
# a right-hand loop on the grass north of the road round to face the mouth (it faces north-west): north-west, north, round to the right, in heading south-east
approach = [point_at(lap, s_off - 8.0, C), point_at(lap, s_off, C), (1278.0, -6492.0), (1262.0, -6468.0), (1256.0, -6440.0), (1266.0, -6414.0), (1288.0, -6408.0),
            (A[0] - din[0] * 12.0, A[1] - din[1] * 12.0), (A[0] - din[0] * 4.0, A[1] - din[1] * 4.0), A]
pre = resample(chaikin(approach, 3), 2.0)
pre_y = [round(ground(*q), 2) for q in pre]
cut = resample([A, B], 2.0)[1:]; gA, yB = ground(*A), xk["b_floor"]
cut_y = [round(gA + (yB - gA) * math.dist(A, q) / max(1e-6, math.dist(A, B)), 2) for q in cut]
# through the junctions: J25 a corner (B41 in on its west wall, M7d out of its south one); J26 a U-turn (M7d in and B42 out through the same north wall,
# 6 m apart: round the chamber's floor, 18 m square)
def chamber(cid): return next(c for c in NET["chambers"] if c["id"] == cid)
j25, j26 = chamber("J25"), chamber("J26")
turn25 = [(1476.0, j25["floor"], -6558.0), (1480.0, j25["floor"], -6564.0)]
c26 = (j26["x"], j26["z"]); f26 = j26["floor"]
turn26 = [(m7d[-1][0], f26, m7d[-1][2] - 4.0), (c26[0] - 5.0, f26, c26[1] - 2.0), (c26[0] - 2.5, f26, c26[1] + 5.5) if False else (c26[0] - 3.0, f26, c26[1] - 6.5),
          (c26[0] + 3.0, f26, c26[1] - 6.5), (c26[0] + 5.5, f26, c26[1] - 2.0), (b42[0][0] + 1.5, f26, b42[0][2] - 4.0)]
under = b41[1:] + turn25 + m7d[1:] + turn26 + b42[1:]
under2 = [(q[0], q[2]) for q in under[::2]]; under_y = [round(q[1], 2) for q in under[::2]]
# out of the manhole heading north up Root Street, right onto Dobbs Street, onto Cherokee Street just short of the line
DOBBS = (1761.0, -8255.0); ON_CHEROKEE = (1814.0, -8236.0)
s_on = S(ON_CHEROKEE)
out = resample(chaikin([(mh["x"], mh["z"]), (mh["x"], mh["z"] + 20.0), (DOBBS[0], DOBBS[1] - 6.0), (DOBBS[0] + 10.0, DOBBS[1]), (1800.0, -8256.0), (1812.0, -8250.0), point_at(lap, s_on, C), point_at(lap, s_on + 12.0, C)], 2), 2.0)[1:]
out_y = [round(ground(*q), 2) for q in out]
tpath = pre + cut + under2 + out; tpath_y = pre_y + cut_y + under_y + out_y
forgiven = [c["id"] for c in cps if rel(s_off) < rel(c["s"]) < rel(s_on)]
Lt = cum(tpath)[-1]
print(f"THE TUBE: leaves the lap at s {s_off:.0f}, rejoins at s {s_on:.0f}: {Lt:.0f} m (pipes {cum(under2)[-1]:.0f} m) against {rel(s_on) - rel(s_off):.0f} m on the lap; forgives CP {forgiven}")
s_mouth = nearest_s(tpath, A)[1]; s_mh = nearest_s(tpath, (mh["x"], mh["z"]))[1]; s_j26 = nearest_s(tpath, c26)[1]
# the entry plane where the path is 14 m clear of the lap (driving on along the road never crosses it)
_ct = cum(tpath); inset = next(_ct[i] for i, q in enumerate(tpath) if min(math.dist(q, v) for v in lap[::2]) > 14.0)
print(f"  the tube's entry plane {inset:.0f} m along it")
shortcut = {"id": "f_tube", "name": "The Tube", "direction": "forward", "path": flat(tpath), "path_y": tpath_y, "half_width_m": 3.0,
            "entry_inset_m": round(inset, 1), "entry_margin_m": 1.0, "forgives": forgiven, "ai_speed": 30.0,
            "ai_profile": [round(v, 1) for v in (0.0, 16.0, s_mouth - 30.0, 12.0, s_mouth, 14.0, s_mouth + 30.0, 30.0, s_j26 - 40.0, 26.0, s_j26 - 10.0, 7.0, s_j26 + 25.0, 7.0, s_j26 + 45.0, 14.0, s_mh, 12.0, Lt, 18.0)],
            "ai_slip": 0.15, "ai_slip_profile": [round(v, 1) for v in (0.0, 14.0, s_mouth - 30.0, 10.0, s_mouth, 12.0, s_mh, 12.0, Lt, 16.0)]}

# ---------------------------------------------------------------- closures (as the Marietta Square race's)
def walk(p, k, step):
    arc = 0.0; i = k
    while 0 <= i + step < len(p):
        a, b = p[i], p[i + step]; seg = math.dist(a, b)
        if seg < 1e-6: i += step; continue
        n = int(seg / 0.5) + 1
        for q in range(1, n + 1):
            d = min(seg, q * 0.5)
            yield arc + d, (a[0] + (b[0] - a[0]) * d / seg, a[1] + (b[1] - a[1]) * d / seg), ((b[0] - a[0]) / seg, (b[1] - a[1]) / seg)
        arc += seg; i += step
roads_pts = [pts(r["points"]) for r in roads]
LAPA = np.array(lap)
def course_dist(q):
    a, b = LAPA[:-1], LAPA[1:]; ab = b - a; L2 = np.maximum((ab ** 2).sum(1), 1e-12)
    t = np.clip(((q[0] - a[:, 0]) * ab[:, 0] + (q[1] - a[:, 1]) * ab[:, 1]) / L2, 0, 1)
    return float(np.hypot(a[:, 0] + ab[:, 0] * t - q[0], a[:, 1] + ab[:, 1] * t - q[1]).min())
def follow(ri, k, step, arc0, depth, seen):
    p = roads_pts[ri]; last = None
    for d, q, tan in walk(p, k, step):
        last = (arc0 + d, q, tan)
        if course_dist(q) >= SETTINGS["closure_clear_m"]: return (arc0 + d, q, tan, ri)
        if arc0 + d > 120: return None
    if last is None or depth >= 3: return None
    end = p[-1] if step > 0 else p[0]
    for rj, pj in enumerate(roads_pts):
        if rj == ri or rj in seen: continue
        for kj in (0, len(pj) - 1):
            if math.dist(pj[kj], end) < 1.5:
                res = follow(rj, kj, 1 if kj == 0 else -1, last[0], depth + 1, seen | {ri})
                if res: return res
    return None
box = (LAPA[:, 0].min() - 150, LAPA[:, 0].max() + 150, LAPA[:, 1].min() - 150, LAPA[:, 1].max() + 150)
closures, stubs, junc = [], [], []
MH26 = (mh["x"], mh["z"])
for ri, r in enumerate(roads):
    if r["class"] in ("footway", "path", "steps", "pedestrian", "cycleway", "track"): continue
    p = roads_pts[ri]
    if not any(box[0] < x < box[1] and box[2] < z < box[3] for x, z in p): continue
    on = [course_dist(v) < max(2.5, r["width"] / 4.0 + 1.0) for v in p]
    for k in range(len(p)):
        if not on[k]: continue
        for step in (1, -1):
            nb = k + step
            if not (0 <= nb < len(p)) or on[nb]: continue
            contact = p[k]
            res = follow(ri, k, step, 0.0, 0, frozenset())
            nm = r["name"] or "(unnamed " + r["class"] + ")"
            if res is None: stubs.append({"road": nm, "osm_id": r["osm_id"], "contact": [round(contact[0], 1), round(contact[1], 1)]}); continue
            arc, q, tan, rj = res
            # the tube's way out: Root Street's barrier beyond the manhole (the lid pops you out between the barrier and Hansell Street)
            if nm.startswith("Root Street") and q[1] > MH26[1]: continue   # (north of the manhole: the tube's way out to Dobbs Street)
            if nm.startswith("Dobbs Street") and q[0] > 1755.0: continue   # (the tube's way east along Dobbs Street to Cherokee)
            sj = nearest_s(lap, contact)
            junc.append((sj[1], nm))
            closures.append({"road": nm, "osm_id": r["osm_id"], "x": round(q[0], 2), "z": round(q[1], 2), "dx": round(tan[0], 4), "dz": round(tan[1], 4),
                             "width_m": roads[rj]["width"], "junction_s": round(sj[1], 1), "contact": [round(contact[0], 1), round(contact[1], 1)], "arc_m": round(arc, 1),
                             "barrier_on": roads[rj]["name"] or "(unnamed " + roads[rj]["class"] + ")"})
closures.sort(key=lambda c: -c["width_m"])
kept = []
for c in closures:
    if any(math.dist((c["x"], c["z"]), (o["x"], o["z"])) < 5.0 and abs(c["dx"] * o["dx"] + c["dz"] * o["dz"]) > 0.8 for o in kept): continue
    kept.append(c)
closures = sorted(kept, key=lambda c: c["junction_s"])
for i, c in enumerate(closures, 1): c["id"] = f"B{i}"; c["label"] = f"{c['road']} at lap {c['junction_s']:.0f} m"
junc.sort(); merged = []
for s, nm in junc:
    if merged and s - merged[-1]["s"] < 6: merged[-1]["roads"].append(nm); continue
    merged.append({"s": round(s, 1), "roads": [nm]})
for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(lap, j["s"], C)]
print(f"closures: {len(closures)}; dead-end stubs not barred: {len(stubs)}")
for c in closures:
    if c["road"].startswith("Root Street"): print(f"  Root Street barrier at ({c['x']:.0f}, {c['z']:.0f}); the manhole at ({MH26[0]:.0f}, {MH26[1]:.0f})")

# Round 93 (C2, Dan: "since there is a uturn lets make it more obvious"): the U-turn at Tower Road's west end (its apex: the lap's westmost point
# there), for the race layer's barrier row along its outside, its U-turn board and the minimap (Race/RaceUTurns.cs)
apex = min((q for q in lap if math.dist(q, (1195.0, -6515.0)) < 60.0), key=lambda q: q[0])
u_turns = [{"pos": [round(apex[0], 1), round(apex[1], 1)], "reach": 30.0}]
print(f"the U-turn's apex at ({apex[0]:.1f}, {apex[1]:.1f})")

out = dict(SETTINGS)
out.update({
    "source": "Tools/mapgen/make_wings_route.py (Round 92 F): Cherokee Street, Tower Road, Church Street, Hansell Street; the tube from the Kennestone exit to the Root Street manhole",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": [], "official_shortcuts": [shortcut], "closures": closures, "closure_stubs_not_barred": stubs,
    "u_turns": u_turns,
})
json.dump(out, open(OUT_ROUTE, "w", encoding="utf-8"), indent=0)
print(f"wrote {OUT_ROUTE}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(11, 18))
    for b in m["buildings"]:
        f = b["footprint"]
        if box[0] < f[0] < box[1] and box[2] < f[1] < box[3]: ax.fill(f[0::2], f[1::2], color="#ddd")
    for r in roads:
        p = r["points"]
        if any(box[0] < p[j] < box[1] and box[2] < p[j + 1] < box[3] for j in range(0, len(p), 2)): ax.plot(p[0::2], p[1::2], color="#888", lw=max(.3, r["width"] / 4))
    ax.plot([q[0] for q in lap], [q[1] for q in lap], "r-", lw=1.5)
    ax.plot([q[0] for q in tpath], [q[1] for q in tpath], "m-", lw=1.5)
    for c in cps: ax.plot(*c["pos"], "bo"); ax.text(*c["pos"], f"CP{c['id']}", color="b")
    for c in closures: ax.plot(c["x"], c["z"], "kx")
    ax.plot(*point_at(lap, gate_s, C), "g*", ms=14)
    ax.set_aspect("equal"); ax.set_xlim(box[0] + 100, box[1] - 100); ax.set_ylim(box[2] + 100, box[3] - 100)
    os.makedirs(os.path.join(ROOT, "local/round92"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round92/wings_route.png"), dpi=80, bbox_inches="tight")
