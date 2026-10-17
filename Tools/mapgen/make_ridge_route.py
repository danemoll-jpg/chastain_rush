"""Round 33 (Dan, 2026-10-15 22:49): the "Chastain Ridge" race on open roads, from Dan's sketch (local/round33/route_sketch.png,
Google: looked at only; the course is laid on OSM's roads from Data/map.json). One lap, forward only:
  1. Canton Road north from the New Chastain Road junction to Chastain Corner Northeast
  2. left onto Chastain Corner, southwest and downhill to New Chastain Road
  3. straight across both carriageways onto Chastain Ridge Drive, round it the way it runs (west, south up to the crest, east)
  4. THE RIDGE LEAP (the sketch's dotted line): off the drive's dead end, over houses 923 / 706 and the hollow behind them, down
     onto New Chastain Road's southeast-bound carriageway. Part of the lap. The take-off is a dirt pile at the dead end (fill from
     road work, its real size); the launch is aimed (BigJump vmin = vmax): every vehicle that crosses the lip at 10 m/s or more
     leaves it at the same speed and angle, so every vehicle makes it flat out from the crest (and none overshoots the road)
  5. New Chastain Road southeast, climbing to Canton Road, left onto Canton Road: the lap
Shortcuts (the sketch's two red lines):
  S1 "Knoll hop": off Canton Road short of Chastain Corner, west across the lot north of the canopy (4) and the shops (6), a dirt pile
     that throws you onto the top of the knoll beside Chastain Corner, across its top and off its west edge onto Chastain Corner.
  S2 "Storage hop": off New Chastain Road before Canton Road, east up the bank, a dirt pile that throws you over the self-storage
     garage row (63), across the open ground, between the churches (74, 75) and out their drive onto Canton Road.
Open roads: "traffic": true (traffic drives the course during the race; side roads are barred as in the other races).
Two-way roads are raced on their right-hand lane (the lap's line is offset a quarter of the road's width), so racers and
oncoming traffic pass each other.

Hand choices are the constants below; everything else is computed, so this re-runs after any mapgen run (it reads
Assets/Generated/terrain.r16). It writes:
  Data/routes/chastain_ridge.json   the race (RaceRoute.cs format), with "reset_skip" (CarRecovery: a reset between the lip and the
                                    landing puts the vehicle on New Chastain Road past the landing, facing on)
  Data/tracks/chastain_ridge.json   what the map build makes (MapBuilder.ChastainRidge.cs): the three dirt piles (big_jumps, built
                                    by MapBuilder.BigJumps.cs), landing zones, lines cleared of trees and back-yard fences
Picture for checking (git-ignored, it shows houses): local/round33/layout.png with --plot.

Usage: python Tools/mapgen/make_ridge_route.py [--plot]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/chastain_ridge.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/chastain_ridge.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]; by_osm = {r["osm_id"]: r for r in roads}

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
def pts(flat): return [(flat[i], flat[i + 1]) for i in range(0, len(flat), 2)]
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
def cut(p, a, b):
    c = cum(p); b = c[-1] if b is None else b
    return [point_at(p, a, c)] + [p[i] for i in range(len(p)) if a + 1e-6 < c[i] < b - 1e-6] + [point_at(p, b, c)]
def resample(p, step):
    c = cum(p); n = max(1, int(math.ceil(c[-1] / step)))
    return [point_at(p, c[-1] * k / n, c) for k in range(n + 1)]
def chaikin(p, it=3):
    for _ in range(it):
        q = [p[0]]
        for a, b in zip(p[:-1], p[1:]): q += [(0.75 * a[0] + 0.25 * b[0], 0.75 * a[1] + 0.25 * b[1]), (0.25 * a[0] + 0.75 * b[0], 0.25 * a[1] + 0.75 * b[1])]
        p = q + [p[-1]]
    return p
def bearing_dir(deg): return (math.sin(math.radians(deg)), math.cos(math.radians(deg)))
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def poly_dist(q, ring):
    inside = False; n = len(ring)
    for i in range(n):
        a, b = ring[i], ring[(i + 1) % n]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]: inside = not inside
    if inside: return 0.0
    return min(seg_dist(q, ring[i], ring[(i + 1) % n]) for i in range(n))
B = [(i, pts(b["footprint"]), b.get("height", 6.0), b.get("type", "")) for i, b in enumerate(m["buildings"])]
BBOX = {i: (min(p[0] for p in ring), max(p[0] for p in ring), min(p[1] for p in ring), max(p[1] for p in ring)) for i, ring, h, k in B}
def near_box(i, q, r): b = BBOX[i]; return b[0] - r < q[0] < b[1] + r and b[2] - r < q[1] < b[3] + r

# ---------------------------------------------------------------- hand choices
# The take-off: the dirt pile at Chastain Ridge Drive's dead end, its lip just past the road's end, pointing over 706 / 923
RIDGE_LIP = (3150.0, 620.0)
RIDGE_BEARING = 98.0          # degrees from north (east and a little south): across the hollow onto the carriageway
RIDGE = dict(height=2.8, angle=22.0, ramp=14.0, width=7.0, back=24.0)   # lip over the ground, lip angle, take-off length, width, back slope
RIDGE_SPEED = None            # aimed launch speed (vmin = vmax); None: the speed that lands on the carriageway's centerline
RIDGE_RUNUP_FROM = (3112.0, 621.0)   # the lap leaves the drive's line here, straight to the pile (the drive bends north to its end)
# S1: the kicker in the lot, onto the knoll's top
S1_LIP, S1_BEARING = (3464.0, 1046.5), 271.0
S1 = dict(height=2.6, angle=27.0, ramp=11.0, width=6.0, back=24.0)
S1_CTRL = [(3524.0, 1006.0), (3522.0, 1024.0), (3512.0, 1040.0), (3496.0, 1047.0)]          # Canton Road to the pile (then the pile)
S1_AFTER = [(3400.0, 1044.5), (3385.0, 1045.5)]                                                  # across the knoll's top, off its west edge
S1_JOIN = (3352.0, 1047.5)                                                                       # onto Chastain Corner (its right lane)
# S2: the kicker south of the garage row 63
S2_LIP, S2_BEARING = (3458.0, 478.0), 33.0
S2 = dict(height=2.8, angle=22.0, ramp=11.0, width=6.0, back=24.0)
S2_CTRL = [(3336.0, 472.0), (3370.0, 474.0), (3408.0, 471.0), (3436.0, 466.0)]                   # New Chastain Road east up the bank
S2_AFTER = [(3492.0, 528.0), (3508.0, 538.0), (3536.0, 541.0), (3560.0, 541.5), (3570.0, 548.0)]  # between the churches, out their drive
S2_JOIN = (3576.0, 566.0)                                                                        # onto Canton Road northbound
# the way out of the hollow: a ramp graded along the foot of New Chastain Road's embankment, from the hollow's floor up to the
# carriageway's shoulder (a vehicle short of the road drives up it and on; the straight climb up the embankment is 50 % and more)
HOLLOW_RAMP = [(3224.0, 618.0), (3262.0, 579.0)]
SHORTCUT_SPEED = 25.0         # S1 / S2 aimed launch (vmin = vmax)
# checkpoints (x, z, label) in lap order; the gate
GATE_AT = (3573.0, 700.0)
CHECKPOINTS = [
    (3490.0, 1081.0, "Chastain Corner"),        # S1 skips it; a cut through the lot onto Chastain Corner misses it
    (3236.0, 927.0, "down Chastain Corner"),
    (3079.0, 620.0, "the ridge's crest"),       # a cut through the yards to the dead end misses it
    (3290.0, 530.0, "past the landing"),
    (3430.0, 400.0, "New Chastain Road"),        # S2 skips these two
    (3579.0, 470.0, "Canton Road"),
]
SETTINGS = {
    "name": "Chastain Ridge",
    "direction": "forward",
    "directions": ["forward", "reverse"],   # Round 39 (C): the reverse lap climbs the ridge (loop_rev)
    "laps": 3,
    "traffic": True,                # Round 30 (A3): a race on open roads
    "gate_half_width_m": 12.0,
    "checkpoint_half_width_m": 10.0,
    "gate_below_m": 6.0,
    "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    "closure_clear_m": 12.0,
    "closure_extra_width_m": 6.0,
}
LEAD_IN_M = 70.0

# ---------------------------------------------------------------- the lap from OSM ways: (osm id, reverse, from point, to point)
def way(osm, rev=False, a=None, b=None):
    p = pts(by_osm[osm]["points"])
    if rev: p = p[::-1]
    sa = nearest_s(p, a)[1] if a else 0.0
    sb = nearest_s(p, b)[1] if b else None
    return cut(p, sa, sb)
dR = bearing_dir(RIDGE_BEARING)
RL = RIDGE_LIP; RF = (RL[0] - dR[0] * RIDGE["ramp"], RL[1] - dR[1] * RIDGE["ramp"])
SPEC = [
    ("Canton Road", 629432042, way(629432042), False), ("Canton Road", 629432038, way(629432038), False),
    ("Canton Road", 330735261, way(330735261), False),
    ("Canton Road Northeast", 1416681832, way(1416681832, b=(3507.0, 1086.0)), True),
    ("Chastain Corner Northeast", 9139438, way(9139438, rev=True), True),
    ("Chastain Corner Northeast", 1360406923, way(1360406923, rev=True), True),
    ("Chastain Ridge Drive Northeast", 1360406918, way(1360406918, rev=True), False),
    ("Chastain Ridge Drive Northeast", 1360406916, way(1360406916, rev=True), False),
    ("Chastain Ridge Drive Northeast", 1360406917, way(1360406917, rev=True), False),
    ("Chastain Ridge Drive Northeast", 243965590, way(243965590, rev=True, b=RIDGE_RUNUP_FROM), False),
]
pieces, line, offs = [], [], []
for name, osm, seg, two_way in SPEC:
    if line and math.dist(line[-1], seg[0]) > 0.5: raise SystemExit(f"the lap breaks before {name} ({osm}): gap {math.dist(line[-1], seg[0]):.1f} m")
    seg = resample(seg, 3.0)
    off = by_osm[osm]["width"] / 4.0 if two_way else 0.0
    pieces.append({"osm_id": osm, "name": name, "reverse": osm not in (629432042, 629432038, 330735261, 1416681832), "length_m": round(cum(seg)[-1], 1)})
    line += seg if not line else seg[1:]; offs += [off] * (len(seg) if len(offs) == 0 else len(seg) - 1)
# the run-up to the pile and the pile (no offset: the pile is on the line)
for q in resample([line[-1], RF], 3.0)[1:] + resample([RF, RL], 2.0)[1:]: line.append(q); offs.append(0.0)
s_lip_pre = cum(line)[-1]

# ---------------------------------------------------------------- the Ridge leap: flight over the real ground and the houses
def flight(L, d, y0, v, ang, max_t=6.0):
    vx, vy = v * math.cos(math.radians(ang)), v * math.sin(math.radians(ang)); t = 0.0; top = y0; over = {}
    while t < max_t:
        t += 0.01; s = vx * t; q = (L[0] + d[0] * s, L[1] + d[1] * s); y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        for i, ring, h, kind in B:
            if not near_box(i, q, 2.0): continue
            if poly_dist(q, ring) < 1.0:
                base = min(ground(*p) for p in ring); over[i] = min(over.get(i, 1e9), y - (base + h))
        if t > 0.15 and y <= ground(*q): return {"t": round(t, 2), "dist": round(s, 1), "at": (round(q[0], 1), round(q[1], 1)), "y": round(y, 2), "top": round(top - y0, 1), "over": {k: round(v_, 1) for k, v_ in over.items()}}
    return None
NC = pts(by_osm[9151940]["points"])   # New Chastain Road, southeast-bound
ridge_y = ground(*RL) + RIDGE["height"]
# where the flight's line crosses the carriageway's centerline
Pl, s_on_line = None, None
for k in range(400):
    q = (RL[0] + dR[0] * k * 0.5, RL[1] + dR[1] * k * 0.5)
    dd, s = nearest_s(NC, q)
    if dd < 0.4 and k > 100: Pl, s_on_line = q, k * 0.5; break
if Pl is None: raise SystemExit("the Ridge leap's line never meets New Chastain Road")
if RIDGE_SPEED is None:
    best = None
    for v10 in range(240, 400):
        f = flight(RL, dR, ridge_y, v10 / 10.0, RIDGE["angle"])
        if f and (best is None or abs(f["dist"] - (s_on_line + 2.0)) < abs(best[1]["dist"] - (s_on_line + 2.0))): best = (v10 / 10.0, f)
    RIDGE_SPEED = best[0]
rf = flight(RL, dR, ridge_y, RIDGE_SPEED, RIDGE["angle"])
print(f"Ridge leap: lip ({RL[0]:.1f}, {RL[1]:.1f}) at {ridge_y:.2f} m ({RIDGE['height']} m over the dead end's ground), {RIDGE['angle']} deg, bearing {RIDGE_BEARING};"
      f" the carriageway's centerline {s_on_line:.1f} m out at ({Pl[0]:.1f}, {Pl[1]:.1f}), {ground(*Pl):.1f} m")
print(f"  aimed launch {RIDGE_SPEED} m/s: {rf}")
for dv in (-2.0, -1.0, 1.0, 2.0):
    f = flight(RL, dR, ridge_y, RIDGE_SPEED + dv, RIDGE["angle"]); print(f"    ({RIDGE_SPEED + dv:.1f} m/s would land {f['dist']} m out)")
# the lap: straight across to the carriageway, then on down it
s_land_nc = nearest_s(NC, Pl)[1]
for q in resample([RL, Pl], 4.0)[1:]: line.append(q); offs.append(0.0)
s_pl = cum(line)[-1]
rest = resample(cut(NC, s_land_nc, None), 3.0)
for q in rest[1:]: line.append(q); offs.append(0.0)
for osm in (330734318, 1086893635):
    seg = resample(pts(by_osm[osm]["points"]), 3.0)
    if math.dist(line[-1], seg[0]) > 0.5: raise SystemExit(f"the lap breaks before {osm}")
    for q in seg[1:]: line.append(q); offs.append(0.0)
pieces.append({"osm_id": 0, "name": "the Ridge leap", "reverse": False, "length_m": round(s_pl - s_lip_pre, 1)})
pieces.append({"osm_id": 9151940, "name": "New Chastain Road", "reverse": False, "length_m": round(cum(rest)[-1], 1)})
if math.dist(line[0], line[-1]) > 0.5: raise SystemExit("the lap does not close: gap %.1f m" % math.dist(line[0], line[-1]))
line[-1] = line[0]; offs[-1] = offs[0]
# the right-hand lane on two-way roads: the offset eased in over 15 m either side, along each vertex's right-hand normal
N = len(line); C = cum(line); L_lap = C[-1]
sm = []
for i in range(N):
    w = [offs[j] for j in range(max(0, i - 5), min(N, i + 6))]; sm.append(sum(w) / len(w))
lap = []
for i in range(N):
    a, b = line[max(0, i - 1)], line[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
    right = (t[1] / n, -t[0] / n)
    lap.append((line[i][0] + right[0] * sm[i], line[i][1] + right[1] * sm[i]))
lap[-1] = lap[0]
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
s_lip = S(RL); s_land = S(Pl)
print(f"lap {L_lap:.0f} m: " + ", ".join(f"{p['name']} {p['length_m']:.0f}" for p in pieces))

# ---------------------------------------------------------------- gate, grid, checkpoints
gate_s = round(S(GATE_AT), 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M) + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
cps = []
for i, (x, z, label) in enumerate(CHECKPOINTS, 1):
    s = round(S((x, z)), 1); p = point_at(lap, s, C); cps.append({"id": i, "s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
order = sorted(cps, key=lambda c: (c["s"] - gate_s) % L_lap)
if [c["id"] for c in order] != [c["id"] for c in cps]: raise SystemExit("checkpoints are not in lap order from the gate")
print(f"gate at s {gate_s} ({GATE_AT}); checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps))

# ---------------------------------------------------------------- the shortcuts: line, kicker, flight
def kicker(jid, L, bearing, k, speed, land_poly_extra=10.0):
    d = bearing_dir(bearing); y0 = ground(*L) + k["height"]
    f = flight(L, d, y0, speed, k["angle"])
    far = max(f["dist"], flight(L, d, y0, speed + 2.0, k["angle"])["dist"])
    bj = {"id": jid, "look": "dirtpile", "s_lip": -1.0, "lip": [round(L[0], 2), round(L[1], 2)], "dir": [round(d[0], 5), round(d[1], 5)],
          "height": k["height"], "lip_y": round(y0, 3), "angle_deg": k["angle"], "ramp_m": k["ramp"], "width": k["width"],
          "land_from": round(max(4.0, f["dist"] - 25.0), 1), "land_to": round(far + land_poly_extra, 1), "vmin": speed, "vmax": speed, "design": speed,
          "back_angle": k["back"], "flights": {str(speed): [f["t"], f["dist"], f["top"]]}}
    return bj, f, d
def foot(L, d, k): return (L[0] - d[0] * k["ramp"], L[1] - d[1] * k["ramp"])
def shortcut_line(start_q, ctrl, L, d, k, f, after, join_q):
    s0 = S(start_q); p0 = point_at(lap, s0, C)
    F = foot(L, d, k); land = f["at"]
    path = chaikin([p0] + ctrl + [F], 3) + resample([F, L], 2.0)[1:] + resample([L, land], 4.0)[1:]
    s1 = S(join_q); p1 = point_at(lap, s1, C)
    path += chaikin([land] + after + [p1], 3)[1:]
    return resample(path, 2.0), s0, s1
bj1, f1, d1 = kicker("S1", S1_LIP, S1_BEARING, S1, SHORTCUT_SPEED)
bj2, f2, d2 = kicker("S2", S2_LIP, S2_BEARING, S2, SHORTCUT_SPEED)
p1, s1a, s1b = shortcut_line(S1_CTRL[0], S1_CTRL[1:], S1_LIP, d1, S1, f1, S1_AFTER, S1_JOIN)
p2, s2a, s2b = shortcut_line(S2_CTRL[0], S2_CTRL[1:], S2_LIP, d2, S2, f2, S2_AFTER, S2_JOIN)
def through(path, label):
    near = {}
    for q in path[::2]:
        for i, ring, h, kind in B:
            if not near_box(i, q, 7.0): continue
            dd = poly_dist(q, ring)
            if dd < 6.0: near[i] = min(near.get(i, 99), dd)
    return near
def forgiven(sa, sb): return [c["id"] for c in cps if (c["s"] - sa) % L_lap < (sb - sa) % L_lap]
shortcuts = []
for sid, name, path, sa, sb, bj, f, k, L in (("S1", "Knoll hop", p1, s1a, s1b, bj1, f1, S1, S1_LIP), ("S2", "Storage hop", p2, s2a, s2b, bj2, f2, S2, S2_LIP)):
    plen = cum(path)[-1]; lap_len = (sb - sa) % L_lap; sF = nearest_s(path, foot(L, bearing_dir(S1_BEARING if sid == "S1" else S2_BEARING), k))[1]
    sL = nearest_s(path, L)[1]; sLand = nearest_s(path, f["at"])[1]
    fg = forgiven(sa, sb)
    print(f"{sid} {name}: leaves the lap at s {sa:.0f}, rejoins at s {sb:.0f}: {plen:.0f} m against {lap_len:.0f} m on the lap (saves {lap_len - plen:.0f} m); forgives CP {fg}")
    print(f"  kicker lip ({L[0]:.1f}, {L[1]:.1f}) at {bj['lip_y']:.2f} m ({k['height']} m, {k['angle']} deg), launch {SHORTCUT_SPEED} m/s: {f}")
    print(f"  buildings within 6 m of its line (index: metres): {through(path, sid)}; under its flight (clearance over the roof): {f['over']}")
    shortcuts.append({"id": sid.lower() + "_" + name.lower().replace(" ", "_"), "name": name, "direction": "forward",
                      "path": [round(v, 2) for q in path for v in q], "half_width_m": 3.5, "entry_inset_m": round(max(10.0, sF - 12.0), 1), "entry_margin_m": 1.5,
                      "forgives": fg, "ai_speed": 22.0,
                      # slow for the turn off the road, held into the pile (the launch is aimed), easy after the landing
                      "ai_profile": [round(v, 1) for v in (0.0, 18.0, max(1.0, sF - 25.0), 16.0, sF - 5.0, 20.0, sL + 1.0, 20.0, sLand + 2.0, 15.0, plen - 20.0, 15.0)]})
    bj["tracks"] = [round(v, 2) for q in path[: int(max(2, sF / 2.0))] for v in q]   # worn tracks from the road to the pile: the hint at the turn-off
    # and a road-closed barricade knocked over beside the tracks just off the road (the road work the dirt pile is from; N2's hint)
    k_h = next(k for k in range(len(path) - 1) if min(seg_dist(path[k], a, b) for a, b in zip(lap[:-1], lap[1:])) >= 12.0); a_, b_ = path[k_h], path[k_h + 1]; tdir = (b_[0] - a_[0], b_[1] - a_[1]); tn = math.hypot(*tdir) or 1.0
    hp = (a_[0] - tdir[1] / tn * 4.5, a_[1] + tdir[0] / tn * 4.5)   # 4.5 m to the left of the tracks
    bj["hint"] = {"kind": "barricade", "x": round(hp[0], 2), "z": round(hp[1], 2), "yaw": round(math.degrees(math.atan2(tdir[0], tdir[1])) + 20.0, 1)}

# ---------------------------------------------------------------- the race layer: AI zones, reset skip, closures, junctions
ai_zones = [{"id": "ridge_leap", "s0": round(s_lip - 80.0, 1), "s1": round(s_lip + 2.0, 1), "speed": 26.0}]
reset_skip = [{"id": "ridge_leap", "s0": round(s_lip - 3.0, 1), "s1": round(s_land + 4.0, 1), "to_s": round(s_land + 25.0, 1)}]
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
def closures_for(lap, sc_paths, skip):
    """barriers across every road that leaves this lap (and the junction list), as Round 33's; Round 39: a function, for the reverse lap too"""
    def course_dist(q): return min(seg_dist(q, a, b) for a, b in zip(lap[:-1], lap[1:]))
    def follow(ri, k, step, arc0, depth, seen):
        p = roads_pts[ri]; last = None
        for d, q, tan in walk(p, k, step):
            last = (arc0 + d, q, tan)
            if course_dist(q) >= SETTINGS["closure_clear_m"]: return (arc0 + d, q, tan, ri)
            if arc0 + d > 120: return None
        if last is None or depth >= 3: return None
        end = p[-1] if step > 0 else p[0]
        for rj, pj in enumerate(roads_pts):
            if rj == ri or rj in seen or roads[rj]["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
            for kj in (0, len(pj) - 1):
                if math.dist(pj[kj], end) < 1.5:
                    res = follow(rj, kj, 1 if kj == 0 else -1, last[0], depth + 1, seen | {ri})
                    if res: return res
        return None
    box = (min(q[0] for q in lap) - 150, max(q[0] for q in lap) + 150, min(q[1] for q in lap) - 150, max(q[1] for q in lap) + 150)
    closures, stubs, junc = [], [], []
    for ri, r in enumerate(roads):
        if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
        p = roads_pts[ri]
        if not any(box[0] < x < box[1] and box[2] < z < box[3] for x, z in p): continue
        on = [course_dist(v) < max(2.5, r["width"] / 4.0 + 1.0) for v in p]
        for k in range(len(p)):
            if not on[k]: continue
            for step in (1, -1):
                nb = k + step
                if not (0 <= nb < len(p)) or on[nb]: continue
                contact = p[k]
                # the carriageway the Ridge leap lands on: its piece from the Chastain Corner crossing to the landing is barred at the
                # crossing only (a barrier just short of the landing would stand in the flight's last meters)
                if skip(r, contact): continue
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
        # never across a shortcut's line
        if any(seg_dist((c["x"], c["z"]), a, b) < c["width_m"] / 2 + 4.0 for path in sc_paths for a, b in zip(path[:-1], path[1:])): print(f"  (no barrier on {c['road']} at ({c['x']:.0f}, {c['z']:.0f}): on a shortcut's line)"); continue
        kept.append(c)
    closures = sorted(kept, key=lambda c: c["junction_s"])
    for i, c in enumerate(closures, 1): c["id"] = f"B{i}"; c["label"] = f"{c['road']} at lap {c['junction_s']:.0f} m"
    junc.sort(); merged = []
    for s, nm in junc:
        if merged and s - merged[-1]["s"] < 6: merged[-1]["roads"].append(nm); continue
        merged.append({"s": round(s, 1), "roads": [nm]})
    for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(lap, j["s"], cum(lap))]
    return closures, stubs, merged
# the carriageway the Ridge leap lands on: its piece from the Chastain Corner crossing to the landing is barred at the crossing only
# (a barrier just short of the landing would stand in the flight's last meters)
closures, stubs, merged = closures_for(lap, (p1, p2), lambda r, contact: r["osm_id"] == 9151940 and math.dist(contact, Pl) < 40.0)

# ---------------------------------------------------------------- Round 39 (C): the reverse lap, THE RIDGE CLIMB and the shortcuts backward
# The reverse is a lap of its own (RaceRoute: loop_rev and the *_rev fields), not the forward loop run backward: two-way roads on their
# right-hand lane for this direction, Canton Road's southbound and New Chastain Road's northwest-bound carriageways.
#   Canton Road south from Chastain Corner to New Chastain Road; right onto New Chastain Road northwest, downhill; THE RIDGE CLIMB up
#   onto the dead end; Chastain Ridge Drive the way OSM runs it (west from the dead end, over the crest, north down the ridge, east
#   along z 706); straight across New Chastain Road onto Chastain Corner, uphill; right onto Canton Road: the lap.
# THE RIDGE CLIMB: a pile of road-work fill pushed out over the hollow's edge just past the southbound carriageway's west shoulder,
# north of the way out of the hollow (it never covers it). Its ramp rises 2.8 m from the shoulder like the forward pile's; its lip
# stands about 7 m over the hollow's floor. An aimed launch (vmin = vmax) up over the hollow and house 706 that comes down a few
# meters past the forward pile's lip, on its ramp face (the forward take-off is the climb's landing ramp), and on down it onto the
# drive. Short of it: the hollow, and the way out of it back to New Chastain Road (Round 33). A reset between the climb's foot and
# the landing puts the vehicle on Chastain Ridge Drive past the landing, facing on (reset_skip_rev).
CLIMB_LIP = (3221.0, 632.0)
CLIMB = dict(ramp=11.0, width=6.0, rise=2.8, angle=40.0, back=30.0)
CLIMB_DOWN_RAMP_M = 6.0        # where it lands: this far down the forward pile's ramp face from its lip
CLIMB_RUNUP = [(3272.0, 597.0), (3260.0, 616.0), (3246.0, 632.0)]   # off the northwest-bound carriageway, across both, to the pile's foot
# reverse S1 "Knoll drop": off Chastain Corner up the knoll's northeast shoulder, across its top, down its east face, up the S1 pile's
# back (24 deg) and off its lip into the lot (a launch off the back, rev_angle / rev_vmax, as the creek's jumps), onto Canton Road south
RS1_CTRL = [(3398.0, 1058.0), (3421.0, 1060.0), (3430.0, 1050.0), (3436.0, 1041.0), (3446.0, 1044.0), (3456.0, 1046.4)]
RS1_REV = dict(angle=24.0, vmax=15.0)
RS1_AFTER = [(3492.0, 1040.0), (3506.0, 1028.0)]
RS1_JOIN = (3519.0, 1012.0)
# S2 backward is not built: the garage row (63) stands between its landing and its pile's back, so there is no run at the back to
# take off from; it is a forward-only shortcut (direction "forward": not in a reverse race)
CHECKPOINTS_REV = [
    (3565.0, 470.0, "Canton Road"),
    (3430.0, 413.0, "New Chastain Road"),
    (3104.0, 620.0, "past the climb"),          # a cut through the yards up to the drive misses it
    (3090.0, 707.0, "Chastain Ridge Drive"),
    (3236.0, 927.0, "up Chastain Corner"),
    (3490.0, 1081.0, "Chastain Corner"),        # the Knoll drop skips it
]
RF_ = (RL[0] - dR[0] * RIDGE["ramp"], RL[1] - dR[1] * RIDGE["ramp"])
def fwd_ramp_y(back):   # the forward pile's ramp face, 'back' m behind its lip (MapBuilder.BigJumps.cs KickerProfile)
    y0 = ground(*RF_) - 0.02; k = math.tan(math.radians(RIDGE["angle"])); R = ridge_y - y0; Lk = min(RIDGE["ramp"], 2 * R / k); u = max(0.0, Lk - back)
    return y0 + k * u * u / (2 * Lk)
CT = (RL[0] - dR[0] * CLIMB_DOWN_RAMP_M, RL[1] - dR[1] * CLIMB_DOWN_RAMP_M)
D_c = math.dist(CLIMB_LIP, CT); dC = ((CT[0] - CLIMB_LIP[0]) / D_c, (CT[1] - CLIMB_LIP[1]) / D_c)
CF = (CLIMB_LIP[0] - dC[0] * CLIMB["ramp"], CLIMB_LIP[1] - dC[1] * CLIMB["ramp"])
climb_y = ground(*CF) + CLIMB["rise"]
dy_c = fwd_ramp_y(CLIMB_DOWN_RAMP_M) - climb_y; th = math.radians(CLIMB["angle"])
CLIMB_SPEED = round(math.sqrt(9.81 * D_c * D_c / (2 * math.cos(th) ** 2 * (D_c * math.tan(th) - dy_c))), 1)
vxc = CLIMB_SPEED * math.cos(th); t_l = math.dist(CLIMB_LIP, RL) / vxc
over_lip = climb_y + CLIMB_SPEED * math.sin(th) * t_l - 4.905 * t_l * t_l - ridge_y
houses_c = {}
for k_ in range(1, int(D_c)):
    q = (CLIMB_LIP[0] + dC[0] * k_, CLIMB_LIP[1] + dC[1] * k_); tt = k_ / vxc; y = climb_y + CLIMB_SPEED * math.sin(th) * tt - 4.905 * tt * tt
    for i, ring, h, kind in B:
        if near_box(i, q, 2.0) and poly_dist(q, ring) < 1.0:
            houses_c[i] = round(min(houses_c.get(i, 1e9), y - (min(ground(*p_) for p_ in ring) + h)), 1)
print(f"Ridge climb: lip ({CLIMB_LIP[0]:.1f}, {CLIMB_LIP[1]:.1f}) at {climb_y:.2f} m ({climb_y - ground(*CLIMB_LIP):.1f} m over its own ground, {CLIMB['rise']} m over its foot on the shoulder), "
      f"foot {nearest_s(pts(by_osm[9151940]['points']), CF)[0]:.1f} m from the southbound carriageway's centerline; {CLIMB['angle']} deg, bearing {math.degrees(math.atan2(dC[0], dC[1])) % 360:.1f}; "
      f"aimed at {CLIMB_SPEED} m/s to land {CLIMB_DOWN_RAMP_M} m down the forward pile's ramp, {D_c:.1f} m away, {t_l:.2f} s to the forward lip, "
      f"{(CLIMB_SPEED * math.sin(th)) ** 2 / 19.62:.1f} m up; passes {over_lip:.1f} m over the forward lip; clearance over roofs {houses_c}")
for dv in (-1.0, 1.0):
    t2 = math.dist(CLIMB_LIP, RL) / ((CLIMB_SPEED + dv) * math.cos(th)); print(f"    ({CLIMB_SPEED + dv:.1f} m/s: {climb_y + (CLIMB_SPEED + dv) * math.sin(th) * t2 - 4.905 * t2 * t2 - ridge_y:+.1f} m over the forward lip)")
SPEC_REV = [
    ("Canton Road Northeast", 1416681832, way(1416681832, rev=True, a=(3507.0, 1086.0)), True),
    ("Canton Road", 330735258, way(330735258), False),
    ("Canton Road", 330735259, way(330735259), False),
]
line_r, offs_r = [], []
for name, osm, seg, two_way in SPEC_REV:
    if line_r and math.dist(line_r[-1], seg[0]) > 0.5: raise SystemExit(f"the reverse lap breaks before {name} ({osm}): gap {math.dist(line_r[-1], seg[0]):.1f} m")
    seg = resample(seg, 3.0); off = by_osm[osm]["width"] / 4.0 if two_way else 0.0
    line_r += seg if not line_r else seg[1:]; offs_r += [off] * (len(seg) if not offs_r else len(seg) - 1)
nw = way(290282527, a=line_r[-1], b=CLIMB_RUNUP[0])
for q in resample(nw, 3.0)[1:]: line_r.append(q); offs_r.append(0.0)
runup = chaikin([line_r[-1]] + CLIMB_RUNUP + [CF], 3)
for q in resample(runup, 3.0)[1:] + resample([CF, CLIMB_LIP], 2.0)[1:]: line_r.append(q); offs_r.append(0.0)
for q in resample([CLIMB_LIP, CT], 4.0)[1:] + resample([CT, RF_], 2.0)[1:]: line_r.append(q); offs_r.append(0.0)
drive = way(243965590, a=RF_)
for osm in (None, 1360406917, 1360406916, 1360406918, 1360406923, 9139438):
    seg = drive if osm is None else pts(by_osm[osm]["points"])
    if math.dist(line_r[-1], seg[0]) > 1.0: raise SystemExit(f"the reverse lap breaks before {osm}: gap {math.dist(line_r[-1], seg[0]):.1f} m")
    two = osm in (1360406923, 9139438)
    for q in resample(seg, 3.0)[1:]: line_r.append(q); offs_r.append(by_osm[osm]["width"] / 4.0 if two else 0.0)
if math.dist(line_r[0], line_r[-1]) > 0.5: raise SystemExit("the reverse lap does not close: gap %.1f m" % math.dist(line_r[0], line_r[-1]))
line_r[-1] = line_r[0]; offs_r[-1] = offs_r[0]
Nr = len(line_r); sm_r = [sum(offs_r[max(0, i - 5):min(Nr, i + 6)]) / len(offs_r[max(0, i - 5):min(Nr, i + 6)]) for i in range(Nr)]
lap_r = []
for i in range(Nr):
    a, b = line_r[max(0, i - 1)], line_r[min(Nr - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
    lap_r.append((line_r[i][0] + t[1] / n * sm_r[i], line_r[i][1] - t[0] / n * sm_r[i]))
lap_r[-1] = lap_r[0]
Cr = cum(lap_r); L_rev = Cr[-1]
def SR(q): return nearest_s(lap_r, q)[1]
gate_s_rev = round(SR(GATE_AT), 1)
cps_rev = []
for i, (x, z, label) in enumerate(CHECKPOINTS_REV, 1):
    s_ = round(SR((x, z)), 1); p_ = point_at(lap_r, s_, Cr); cps_rev.append({"id": i, "s": s_, "pos": [round(p_[0], 1), round(p_[1], 1)], "label": label})
if [c["id"] for c in sorted(cps_rev, key=lambda c: (c["s"] - gate_s_rev) % L_rev)] != [c["id"] for c in cps_rev]: raise SystemExit("reverse checkpoints are not in lap order from the gate")
s_clip = SR(CLIMB_LIP); s_cfoot = SR(CF); s_cland = SR(CT)
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; climb foot s {s_cfoot:.0f}, lip s {s_clip:.0f}, landing s {s_cland:.0f}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps_rev))
# reverse S1 "Knoll drop": the S1 pile's back is its take-off
d1b = (-d1[0], -d1[1])
s1r0 = SR(RS1_CTRL[0]); q0 = point_at(lap_r, s1r0, Cr)
v_r = RS1_REV["vmax"]
fr = flight(S1_LIP, d1b, bj1["lip_y"], v_r, RS1_REV["angle"])
path_r = chaikin([q0] + RS1_CTRL[1:] + [S1_LIP], 3) + resample([S1_LIP, fr["at"]], 3.0)[1:]
s1r1 = SR(RS1_JOIN); q1 = point_at(lap_r, s1r1, Cr)
path_r = resample(path_r + chaikin([fr["at"]] + RS1_AFTER + [q1], 3)[1:], 2.0)
fg_r = [c["id"] for c in cps_rev if (c["s"] - s1r0) % L_rev < (s1r1 - s1r0) % L_rev]
plen_r = cum(path_r)[-1]; sLip_r = nearest_s(path_r, S1_LIP)[1]; sLand_r = nearest_s(path_r, fr["at"])[1]
print(f"reverse S1 Knoll drop: leaves the reverse lap at s {s1r0:.0f}, rejoins at s {s1r1:.0f}: {plen_r:.0f} m against {(s1r1 - s1r0) % L_rev:.0f} m (saves {(s1r1 - s1r0) % L_rev - plen_r:.0f} m); "
      f"forgives CP {fg_r}; off the S1 pile's back at {RS1_REV['angle']} deg, {v_r} m/s at most: {fr}")
bj1["rev_angle"] = RS1_REV["angle"]; bj1["rev_vmax"] = v_r
shortcuts.append({"id": "s1r_knoll_drop", "name": "Knoll drop", "direction": "reverse", "path": [round(v, 2) for q in path_r for v in q], "half_width_m": 3.5,
                  "entry_inset_m": 22.0, "entry_margin_m": 1.5, "forgives": fg_r, "ai_speed": 18.0,
                  "ai_profile": [round(v, 1) for v in (0.0, 14.0, 20.0, 12.0, sLip_r - 14.0, 12.0, sLip_r + 1.0, 14.0, sLand_r + 2.0, 12.0, plen_r - 12.0, 14.0)]})
# the reverse race layer
ai_zones_rev = [{"id": "ridge_climb", "s0": round(s_cfoot - 45.0, 1), "s1": round(s_clip + 1.0, 1), "speed": 17.0}]
reset_skip_rev = [{"id": "ridge_climb", "s0": round(s_cfoot - 3.0, 1), "s1": round(s_cland + 4.0, 1), "to_s": round(s_cland + 30.0, 1)}]
closures_rev, stubs_rev, merged_rev = closures_for(lap_r, (path_r,), lambda r, contact: False)
print(f"reverse closures: {len(closures_rev)}; dead-end stubs not barred: {len(stubs_rev)}")
for c in closures_rev: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
climb_bj = {"id": "CLIMB", "look": "dirtpile", "s_lip": -1.0, "lip": [round(CLIMB_LIP[0], 2), round(CLIMB_LIP[1], 2)], "dir": [round(dC[0], 5), round(dC[1], 5)],
            "height": round(climb_y - ground(*CLIMB_LIP), 2), "lip_y": round(climb_y, 3), "angle_deg": CLIMB["angle"], "ramp_m": CLIMB["ramp"], "width": CLIMB["width"],
            "land_from": round(D_c - 12.0, 1), "land_to": round(D_c + 25.0, 1), "vmin": CLIMB_SPEED, "vmax": CLIMB_SPEED, "design": CLIMB_SPEED, "back_angle": CLIMB["back"],
            "flights": {str(CLIMB_SPEED): [round(t_l, 2), round(D_c, 1), round((CLIMB_SPEED * math.sin(th)) ** 2 / 19.62, 1)]}}

# ---------------------------------------------------------------- the world pieces (Data/tracks/chastain_ridge.json)
rbj = {"id": "RL", "look": "dirtpile", "s_lip": -1.0, "lip": [round(RL[0], 2), round(RL[1], 2)], "dir": [round(dR[0], 5), round(dR[1], 5)], "height": RIDGE["height"],
       "lip_y": round(ridge_y, 3), "angle_deg": RIDGE["angle"], "ramp_m": RIDGE["ramp"], "width": RIDGE["width"], "land_from": round(rf["dist"] - 30.0, 1),
       "land_to": round(rf["dist"] + 40.0, 1), "vmin": RIDGE_SPEED, "vmax": RIDGE_SPEED, "design": RIDGE_SPEED, "back_angle": RIDGE["back"],
       "flights": {str(RIDGE_SPEED): [rf["t"], rf["dist"], rf["top"]]}}
def zone(L, d, a, b, half_a, half_b):
    n = (-d[1], d[0])
    return [round(v, 2) for q in [(L[0] + d[0] * a + n[0] * half_a, L[1] + d[1] * a + n[1] * half_a), (L[0] + d[0] * b + n[0] * half_b, L[1] + d[1] * b + n[1] * half_b),
                                   (L[0] + d[0] * b - n[0] * half_b, L[1] + d[1] * b - n[1] * half_b), (L[0] + d[0] * a - n[0] * half_a, L[1] + d[1] * a - n[1] * half_a)] for v in q]
landings = [{"id": "RL", "poly": zone(RL, dR, rf["dist"] - 30.0, rf["dist"] + 45.0, 14.0, 20.0)},
            {"id": "S1", "poly": zone(S1_LIP, d1, 8.0, f1["dist"] + 30.0, 7.0, 9.0)},
            {"id": "S2", "poly": zone(S2_LIP, d2, 8.0, f2["dist"] + 14.0, 7.0, 9.0)},
            {"id": "CLIMB", "poly": zone(CLIMB_LIP, dC, D_c - 12.0, D_c + 24.0, 6.0, 8.0)}]
def flat(p): return [round(v, 2) for q in p for v in q]
clear = [{"id": "RL flight's last meters and the landing", "points": flat([(RL[0] + dR[0] * (rf["dist"] - 45.0), RL[1] + dR[1] * (rf["dist"] - 45.0)), (RL[0] + dR[0] * (rf["dist"] + 30.0), RL[1] + dR[1] * (rf["dist"] + 30.0))]), "half": 9.0},
         {"id": "RL run-up and pile", "points": flat([RIDGE_RUNUP_FROM, RL]), "half": 6.0},
         {"id": "the way out of the hollow", "points": flat(HOLLOW_RAMP), "half": 7.0},
         {"id": "S1", "points": flat(p1[::3] + [p1[-1]]), "half": 6.0},
         {"id": "S2", "points": flat(p2[::3] + [p2[-1]]), "half": 6.0},
         {"id": "RC run-up and pile", "points": flat([CLIMB_RUNUP[-1], CF, CLIMB_LIP]), "half": 6.0},
         {"id": "RC the landing on the dead end", "points": flat([CT, RF_, (3112.0, 621.0)]), "half": 6.0},
         {"id": "S1 backward (Knoll drop)", "points": flat(path_r[::3] + [path_r[-1]]), "half": 6.0}]
# the run-out past the landing: a vehicle that lands across the road at speed runs on into the woods east of it; the trees there
# snap when hit (TreeField.breakable) instead of stopping it dead (Round 33 check: 4 of 11 vehicles stopped on trunks 30-40 m past the road)
breaks = [{"id": "RL run-out", "points": flat([(RL[0] + dR[0] * (rf["dist"] + 6.0), RL[1] + dR[1] * (rf["dist"] + 6.0)), (RL[0] + dR[0] * (rf["dist"] + 75.0), RL[1] + dR[1] * (rf["dist"] + 75.0))]), "half": 22.0},
          # and the woods along the road's east side beyond it, where a vehicle turning back to the road after the landing runs
          {"id": "RL run-out along the road", "points": flat([(Pl[0] + 32.0, Pl[1] + 4.0), (Pl[0] + 52.0, Pl[1] - 60.0)]), "half": 18.0}]
track = {"note": "Round 33: the Chastain Ridge race's world pieces, from Tools/mapgen/make_ridge_route.py (MapBuilder.ChastainRidge.cs). Coordinates are map meters (= Unity x, z).",
         "big_jumps": [rbj, bj1, bj2, climb_bj], "landing_zones": landings, "clear_lines": clear, "break_lines": breaks,
         "grades": [{"id": "the way out of the hollow", "points": flat(HOLLOW_RAMP), "y": [round(ground(*HOLLOW_RAMP[0]), 2), round(ground(*HOLLOW_RAMP[1]), 2)], "half": 4.0, "blend": 5.0,
                     "keep_off": flat(NC), "keep_off_m": 5.0}]}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_ridge_route.py from Data/map.json. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": pieces, "official_shortcuts": shortcuts, "closures": closures,
    "closure_stubs_not_barred": stubs, "ai_zones": ai_zones, "reset_skip": reset_skip,
    # Round 39 (C): the reverse lap of its own (RaceRoute: loop_rev and the *_rev fields)
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev, "junctions_rev": merged_rev,
    "closures_rev": closures_rev, "ai_zones_reverse": ai_zones_rev, "reset_skip_rev": reset_skip_rev,
})
os.makedirs(os.path.dirname(OUT_TRACK), exist_ok=True)
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print(f"Ridge leap on the lap: lip s {s_lip:.0f}, carriageway s {s_land:.0f}; reset skip {reset_skip}")
print(f"closures (barriers while racing): {len(closures)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
print(f"dead-end stubs not barred: {len(stubs)}")
A_, B_ = HOLLOW_RAMP; Lr = math.dist(A_, B_)
print(f"the way out of the hollow: graded from ({A_[0]:.0f}, {A_[1]:.0f}) {ground(*A_):.1f} m to ({B_[0]:.0f}, {B_[1]:.0f}) {ground(*B_):.1f} m, {Lr:.0f} m, "
      f"{(ground(*B_) - ground(*A_)) / Lr * 100:.0f} %; its top {nearest_s(NC, B_)[0]:.1f} m from the carriageway's centerline ({ground(*point_at(NC, nearest_s(NC, B_)[1])):.1f} m there)")
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(14, 16))
    for i, ring, h, kind in B:
        if box[0] < ring[0][0] < box[1] and box[2] < ring[0][1] < box[3]: ax.fill(*zip(*ring), color="#c96")
    for r in roads:
        p = pts(r["points"])
        if any(box[0] < x < box[1] and box[2] < z < box[3] for x, z in p): ax.plot(*zip(*p), color="#999", lw=r["width"] * 0.3)
    ax.plot(*zip(*lap), "b-", lw=1.2)
    for path in (p1, p2): ax.plot(*zip(*path), "r-", lw=1.2)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g")
    for c in closures: ax.plot(c["x"], c["z"], "kx")
    for z_ in landings: ax.fill(*zip(*pts(z_["poly"])), color="y", alpha=0.3)
    gp = point_at(lap, gate_s, C); ax.plot(*gp, "ks"); ax.text(gp[0] + 5, gp[1], "gate")
    ax.set_xlim(3000, 3650); ax.set_ylim(350, 1150); ax.set_aspect("equal")
    os.makedirs(os.path.join(ROOT, "local/round33"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round33/layout.png"), dpi=90, bbox_inches="tight"); print("picture: local/round33/layout.png")
