"""Round 78 (B, Dan, 2026-10-28 04:21: "A car race through Marietta Square taking advantage of the roads and parking decks (I assume there is one I can
drive through) and also crossing the train tracks which can lead to having to beat the train, jump over or get stuck waiting for it").
The "Marietta Square" race, forward only, no shortcuts (Dan picks them after driving it). One lap (about 1.3 km), from the line on South Park Square:
  1. east on South Park Square and Roswell Street to Waddell Street, north up Waddell
  2. right into the Lawrence Street parking deck (Round 77's deck 34256), through its ground floor along the north side's lane, out its far end
  3. right, south on Haynes Street to Lawrence Street, west on Lawrence Street and North Park Square past the square
  4. west on Mill Street over the railroad at the level crossing by the depot (the train comes: beat it, wait at the gates, or take the ballast pile
     JUMP beside the road over both tracks), south on North Marietta Parkway, east on Whitlock Avenue back over the railroad to the square
The jump (Data/tracks/marietta_square_race.json, MapBuilder.MariettaRace.cs; world pieces): the track crew's ballast piled between Mill Street's
north curb and the shop fronts east of the tracks, a real jump (physics only: the standing rule of 2026-10-17) over both tracks and a train on them at
21 m/s or more; its spoil pile beside the road west of the tracks the landing (a landing zone over it). Too slow and you come down on the train or the
rails (a train is a crash and a reset). The race asks the trains' clock (RailTrains.Align) for a southbound train to reach the Mill Street crossing
about when the leader does on lap 1 ("train_meet"). Closed roads: no traffic; every side road barred (closures).
Hand choices are the constants below; the lap is found on map.json's roads by name between the corners, so it re-runs after a mapgen run.
Writes Data/routes/marietta_square.json and Data/tracks/marietta_square_race.json; --plot draws local/round78/marietta_route.png.
Usage: python Tools/mapgen/make_marietta_route.py [--plot]"""
import heapq
import json
import math
import os
import sys

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/marietta_square.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/marietta_square_race.json")
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_piece3.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain_piece3.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]


def ground(x, z):
    fx, fz = (x - meta["origin_x_m"]) / meta["step_x_m"], (z - meta["origin_z_m"]) / meta["step_z_m"]
    i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)


def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
def flat(p): return [round(v, 2) for q in p for v in q]
def cum(p):
    c = [0.0]
    for i in range(len(p) - 1): c.append(c[-1] + math.dist(p[i], p[i + 1]))
    return c
def point_at(line, s, c=None):
    c = c or cum(line); s = max(0.0, min(c[-1], s))
    for i in range(len(line) - 1):
        if c[i] <= s <= c[i + 1] + 1e-9:
            t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9)
            return (line[i][0] + t * (line[i + 1][0] - line[i][0]), line[i][1] + t * (line[i + 1][1] - line[i][1]))
    return line[-1]
def nearest_s(p, q):
    c = cum(p); best = (1e18, 0.0)
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; d = math.dist(a, b)
        if d < 1e-9: continue
        t = max(0, min(1, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / (d * d)))
        dd = math.dist(q, (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])))
        if dd < best[0]: best = (dd, c[i] + t * d)
    return best
def resample(p, step):
    c = cum(p); n = max(1, int(math.ceil(c[-1] / step)))
    return [point_at(p, c[-1] * k / n, c) for k in range(n + 1)]
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def chaikin(p, it=2):
    for _ in range(it):
        q = [p[0]]
        for a, b in zip(p[:-1], p[1:]): q += [(a[0] * .75 + b[0] * .25, a[1] * .75 + b[1] * .25), (a[0] * .25 + b[0] * .75, a[1] * .25 + b[1] * .75)]
        p = q + [p[-1]]
    return p
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Marietta Square",
    "direction": "forward",
    "directions": ["forward"],
    "laps": 3,
    "traffic": False,
    "gate_half_width_m": 10.0,
    "checkpoint_half_width_m": 10.0,
    "gate_below_m": 6.0,
    "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    "closure_clear_m": 12.0,
    "closure_extra_width_m": 6.0,
}
# the corners, in order (each leg on the streets named, found on the road graph); the deck leg is laid by hand
LEGS = [
    ((1760.0, -8516.0), (1904.0, -8519.0), {"South Park Square Northeast", "South Park Square Southeast", "Roswell Street Southeast", "Whitlock Avenue Southwest"}),
    ((1904.0, -8519.0), (1904.0, -8352.0), {"Waddell Street Northeast", "Waddell Street Southeast"}),
    None,   # the deck
    ((1997.0, -8352.0), (1998.0, -8405.0), {"Haynes Street Northeast"}),
    ((1998.0, -8405.0), (1715.0, -8417.0), {"Lawrence Street Northeast", "North Park Square Northeast", "North Park Square Northwest", "Cherokee Street Northeast", "East Park Square Northeast"}),
    ((1715.0, -8417.0), (1546.0, -8411.0), {"Mill Street Northwest", "North Park Square Northwest", "West Park Square Northwest"}),
    ((1546.0, -8411.0), (1539.0, -8523.0), {"North Marietta Parkway"}),
    ((1539.0, -8523.0), (1760.0, -8516.0), {"Whitlock Avenue Southwest", "South Park Square Northwest", "South Park Square Southwest", "South Park Square Northeast", "South Park Square Southeast"}),
]
# the deck (map.json building 34256, Round 77's deck): through the middle of its ground floor (the columns stand only along the lanes' edges, v = +-(hw - 7.4)
# and +-(hw - 0.4): the middle is 45 m of open floor under the first level; the north lane between its column rows is 6.4 m, too tight at speed)
DECK = 34256
GATE_AT = (1795.0, -8516.0)   # the line on South Park Square, the square's south side, eastbound
LEAD_IN_M = 60.0
CHECKPOINTS = [
    (1904.0, -8440.0, "Waddell Street"),
    (1950.0, -8364.0, "the parking deck"),
    (1998.0, -8385.0, "Haynes Street"),
    (1860.0, -8408.0, "Lawrence Street"),
    (1660.0, -8410.0, "Mill Street"),
    (1543.0, -8470.0, "North Marietta Parkway"),
    (1660.0, -8521.0, "Whitlock Avenue"),
]
TRAIN_MEET = (1624.7, -8409.9)   # the Mill Street crossing by the depot
TRAIN_MEET_S = 42.0              # a southbound train reaches it this long after the start (about when the leader does, lap 1)

# ---------------------------------------------------------------- the road graph
NODE = {}
EDGES = {}
def nid(q):
    k = (round(q[0] * 2), round(q[1] * 2))
    if k not in NODE: NODE[k] = (q, len(NODE))
    return NODE[k][1]
POS = {}
for ri, r in enumerate(roads):
    if r["class"] in ("footway", "path", "steps", "pedestrian", "cycleway", "track", "service") or not r["name"]: continue
    p = pts(r["points"])
    if not any(1450 < x < 2150 and -8650 < z < -8250 for x, z in p): continue
    p = resample(p, 4.0)
    for a, b in zip(p[:-1], p[1:]):
        ia, ib = nid(a), nid(b); POS[ia] = a; POS[ib] = b; w = math.dist(a, b)
        EDGES.setdefault(ia, []).append((ib, w, r["name"], r["width"], ri)); EDGES.setdefault(ib, []).append((ia, w, r["name"], r["width"], ri))
# nodes of different roads within 2.5 m: joined (OSM's pieces meet at shared nodes; resampling moved them)
keys = list(POS.items())
arr = np.array([q for _, q in keys])
for i, (ia, qa) in enumerate(keys):
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


# ---------------------------------------------------------------- the deck's lane
dk = next(d for d in json.load(open(os.path.join(ROOT, "Data/tracks/kennestone.json")))["decks"] if d["index"] == DECK)
dc, du = dk["centre"], unit(dk["u"]); dv = (-du[1], du[0])
def deck_at(u, v): return (dc[0] + du[0] * u + dv[0] * v, dc[1] + du[1] * u + dv[1] * v)
LANE_V = 0.0
deck_in, deck_out = deck_at(-dk["hl"] - 1.0, LANE_V), deck_at(dk["hl"] + 1.0, LANE_V)

line, offs, legs_txt = [], [], []
for k, leg in enumerate(LEGS):
    if leg is None:
        # off Waddell (its northbound lane) right into the lane, straight through, out and right onto Haynes (its southbound lane)
        a = line[-1]
        seg = chaikin([a, (a[0], deck_in[1] - 8.0), (deck_in[0] - 4.0, deck_in[1]), deck_in] + [deck_at(u, LANE_V) for u in np.arange(-dk["hl"] + 3, dk["hl"] - 2, 6.0)] +
                      [deck_out, (deck_out[0] + 4.0, deck_out[1]), (LEGS[k + 1][0][0] - 3.75, deck_out[1] - 7.0), (LEGS[k + 1][0][0] - 3.75, LEGS[k + 1][0][1] - 4.0)], 3)
        seg = resample(seg, 3.0)
        line += seg[1:]; offs += [0.0] * (len(seg) - 1); legs_txt.append(f"the deck {cum(seg)[-1]:.0f} m")
        continue
    a, b, names = leg
    if line: a = line[-1]
    p, w = route(a, b, names)
    p = resample(p, 3.0); ww = [w[min(len(w) - 1, int(i * len(w) / len(p)))] for i in range(len(p))]
    legs_txt.append(f"{sorted(names)[0].split(' ')[0]} {cum(p)[-1]:.0f} m")
    # a leg after the deck starts from the deck's own end: no right-hand offset step at the join
    line += p if not line else p[1:]; offs += [x / 4.0 for x in (ww if not offs else ww[1:])]
line[-1] = line[0]; offs[-1] = offs[0]
N = len(line)
# the right-hand lane: the line set off to the right of the travel direction, the offsets eased along
of = np.convolve(np.pad(np.array(offs), 6, mode="wrap"), np.ones(13) / 13, mode="valid")[:N]
lap = []
for i in range(N):
    a, b = line[max(0, i - 1)], line[min(N - 1, i + 1)]; t = unit((b[0] - a[0], b[1] - a[1]))
    lap.append((line[i][0] + t[1] * of[i], line[i][1] - t[0] * of[i]))
lap = resample(chaikin(lap, 2), 3.0); lap[-1] = lap[0]
C = cum(lap); L_lap = C[-1]
print(f"lap {L_lap:.0f} m: " + ", ".join(legs_txt))
gate_s = round(nearest_s(lap, GATE_AT)[1], 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M + k * 2.0) % L_lap, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
cps = []
for x, z, label in CHECKPOINTS:
    s = round(nearest_s(lap, (x, z))[1], 1); p = point_at(lap, s, C); cps.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
cps.sort(key=lambda c: (c["s"] - gate_s) % L_lap)
for i, c in enumerate(cps, 1): c["id"] = i
print(f"gate at s {gate_s}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps))
s_deck0, s_deck1 = nearest_s(lap, deck_in)[1], nearest_s(lap, deck_out)[1]
print(f"deck lane from s {s_deck0:.0f} to {s_deck1:.0f}: in at ({deck_in[0]:.1f}, {deck_in[1]:.1f}), out at ({deck_out[0]:.1f}, {deck_out[1]:.1f})")

# ---------------------------------------------------------------- closures (as Addison Heights')
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

# ---------------------------------------------------------------- the jump over the tracks (forward, westbound on Mill Street)
# the kicker: ballast piled between Mill Street's north curb and the shop fronts (building 34757/34758 at z -8400), its lip 12 m east of the first
# track; the landing: the spoil pile beside the road west of the tracks, its slope falling west
mill = [q for q in lap if 1590 < q[0] < 1700 and -8420 < q[1] < -8400]
mill_z = sum(q[1] for q in mill) / len(mill)   # the lap's line along Mill Street (the westbound lane)
K_Z = -8403.6                                  # the kicker's middle line (between the curb and the shops)
K_LIP_X, K_LEN, K_ANG, K_H, K_W = 1637.5, 15.0, 26.0, 4.0, 4.2
L_TOP_X, L_LEN, L_H, L_W = 1612.0, 31.0, 4.4, 6.0   # (its foot clear of building 34315's corner at x 1578: Round 78's check landed a car against it)
rails_x = (1620.0, 1624.9)
k_lip = (K_LIP_X, K_Z); k_lip_y = ground(*k_lip) + K_H
l_top = (L_TOP_X, K_Z); l_top_y = ground(*l_top) + L_H
train_top = max(ground(x, K_Z) for x in rails_x) + 0.46 + 4.65
L_ANG = math.degrees(math.atan(2 * L_H / L_LEN))   # the landing pile's curve runs its whole length (BigJumps' KickerProfile)
def land_y(x):
    # the landing pile's surface: its top at L_TOP_X, its curve falling west to its foot L_LEN on
    sf = L_LEN - (L_TOP_X - x)
    if sf < 0 or sf > L_LEN: return ground(x, K_Z)
    y0 = ground(L_TOP_X - L_LEN, K_Z); k = math.tan(math.radians(L_ANG)); Lk = min(L_LEN, 2 * (l_top_y - y0) / k); u = max(0.0, sf - (L_LEN - Lk))
    return y0 + k * u * u / (2 * Lk)
def flight(v):
    a = math.radians(K_ANG); vx, vy = v * math.cos(a), v * math.sin(a); t = 0.0; low = 1e9
    while t < 6:
        t += 0.01; x = K_LIP_X - vx * t; y = k_lip_y + vy * t - 4.905 * t * t
        if rails_x[0] - 1.6 < x < rails_x[1] + 1.6: low = min(low, y - 0.9 - train_top)   # the car's underside over a train's roof
        if t > 0.2 and y - 0.4 <= max(land_y(x), ground(x, K_Z)): return {"t": round(t, 2), "x": round(x, 1), "clear": round(low, 2), "slope_at": round(K_LIP_X - x, 1)}
    return None
print(f"jump: kicker lip ({K_LIP_X}, {K_Z}) {k_lip_y:.2f} m ({K_H} m up, {K_ANG} deg, {K_LEN} m), landing top ({L_TOP_X}, {K_Z}) {l_top_y:.2f} m, a train's roof {train_top:.2f} m; the lap on Mill at z {mill_z:.1f}")
vmin = None
for v in (16, 18, 19, 20, 21, 22, 24, 26, 28):
    f = flight(v)
    if f and f["clear"] > 0 and vmin is None: vmin = v
    print(f"    off the lip at {v} m/s: {f}")
jumps = [
    {"id": "MJ kicker", "look": "rockheap", "physics": True, "lip": [K_LIP_X, K_Z], "dir": [-1.0, 0.0], "ramp_m": K_LEN, "height": K_H, "lip_y": round(k_lip_y, 2), "angle_deg": K_ANG,
     "width": K_W, "vmin": float(vmin or 21), "vmax": 30.0},
    {"id": "MJ landing", "look": "rockheap", "physics": True, "lip": [L_TOP_X, K_Z], "dir": [1.0, 0.0], "ramp_m": L_LEN, "height": L_H, "lip_y": round(l_top_y, 2),
     "angle_deg": round(L_ANG, 1), "width": L_W, "vmin": 0.0, "vmax": 0.0},
]
landing_zone = {"id": "MJ landing", "poly": flat([(L_TOP_X + 1, K_Z - L_W / 2 - 1), (L_TOP_X + 1, K_Z + L_W / 2 + 1), (L_TOP_X - L_LEN - 6, K_Z + L_W / 2 + 1), (L_TOP_X - L_LEN - 6, K_Z - L_W / 2 - 1)])}

# ---------------------------------------------------------------- out
ai_zones = [{"id": "into the deck", "s0": round(s_deck0 - 18, 1), "s1": round(s_deck0 + 6, 1), "speed": 9.0},
            {"id": "out of the deck onto Haynes", "s0": round(s_deck1 - 4, 1), "s1": round(s_deck1 + 16, 1), "speed": 9.0}]
ai_widths = [{"id": "the deck's middle", "s0": round(s_deck0 - 2, 1), "s1": round(s_deck1 + 2, 1), "half": 6.0}]
out = dict(SETTINGS)
out.update({
    "source": "Tools/mapgen/make_marietta_route.py (Round 78 B): the square, the Lawrence Street deck's ground floor, Mill Street over the railroad, Whitlock back",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": [], "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": stubs,
    "ai_zones": ai_zones, "ai_widths": ai_widths, "train_meet": list(TRAIN_MEET), "train_meet_s": TRAIN_MEET_S,
})
json.dump(out, open(OUT_ROUTE, "w", encoding="utf-8"), indent=0)
json.dump({"note": "Round 78 (B): the Marietta Square race's jump over the tracks beside the Mill Street crossing (Tools/mapgen/make_marietta_route.py; built by MapBuilder.MariettaRace.cs). Map meters.",
           "big_jumps": jumps, "landing_zones": [landing_zone]}, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print(f"wrote {OUT_ROUTE} and {OUT_TRACK}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(14, 12))
    for b in m["buildings"]:
        f = b["footprint"]
        if box[0] < f[0] < box[1] and box[2] < f[1] < box[3]: ax.fill(f[0::2], f[1::2], color="#ddd")
    for r in roads:
        p = r["points"]
        if any(box[0] < p[j] < box[1] and box[2] < p[j + 1] < box[3] for j in range(0, len(p), 2)): ax.plot(p[0::2], p[1::2], color="#888", lw=r["width"] / 3)
    ax.plot([q[0] for q in lap], [q[1] for q in lap], "r-", lw=1.5)
    for c in cps: ax.plot(*c["pos"], "bo"); ax.text(*c["pos"], f"CP{c['id']}", color="b")
    for c in closures: ax.plot(c["x"], c["z"], "kx")
    ax.plot(*point_at(lap, gate_s, C), "g*", ms=14)
    for j in jumps: ax.plot(*j["lip"], "m^", ms=10)
    ax.set_aspect("equal"); ax.set_xlim(box[0] + 100, box[1] - 100); ax.set_ylim(box[2] + 100, box[3] - 100)
    os.makedirs(os.path.join(ROOT, "local/round78"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round78/marietta_route.png"), dpi=100, bbox_inches="tight")
