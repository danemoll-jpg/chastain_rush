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

# ---------------------------------------------------------------- Round 39 (C) / Round 40 (A): the reverse lap, the manhole plume, the reverse shortcuts
# The reverse is a lap of its own (RaceRoute: loop_rev and the *_rev fields), not the forward loop run backward: two-way roads on their
# right-hand lane for this direction, Canton Road's southbound and New Chastain Road's northwest-bound carriageways.
#   Canton Road south from Chastain Corner to New Chastain Road; right onto New Chastain Road northwest, downhill; THE MANHOLE PLUME up
#   onto the dead end; Chastain Ridge Drive the way OSM runs it (west from the dead end, over the crest, north down the ridge, east
#   along z 706); straight across New Chastain Road onto Chastain Corner, uphill; right onto Canton Road: the lap.
# Round 40 (A1 / A2, Dan BUG-001 / BUG-002 of session 2026-10-17_12-09-19-449_081d93: "lets use a fire hydrant water plume instead ...
# Not everything needs to be dirt pile jump"; then, 12:45: "a manhole cover that blows up as the first driver gets close to it and you
# can't bypass the plume"): Round 39's road-work climb is gone. A manhole cover in the northwest-bound carriageway, on the line you drive
# straight on along (12 m past Dan's BUG-001 spot); it blows (ManholePlume) and a water plume covers the whole carriageway, curb to the
# far side of the median, so anything driving on along it goes through it. The plume throws a vehicle on an aimed flight (GuidedFlight:
# from where the vehicle is in it to the same landing, whatever its line in) west over the hollow and house 706, onto the forward pile's
# ramp face 6 m down from its lip (as the climb landed), and on down onto the drive. Short of it (it never is: the plume is the road):
# the hollow and its way out back to New Chastain Road (Round 33). A reset between the cover and the landing puts the vehicle on
# Chastain Ridge Drive past the landing, facing on (reset_skip_rev).
PLUME_S = 440.0               # along New Chastain Road's northwest-bound carriageway (OSM 290282527) from its start
PLUME_ANGLE = 40.0            # the throw's angle (degrees over the horizontal)
PLUME_DOWN_RAMP_M = 6.0       # where it lands: this far down the forward pile's ramp face from its lip
PLUME_RIGHT_M, PLUME_LEFT_M, PLUME_HALF_LEN_M = 5.0, 9.0, 3.5   # the plume across the carriageway: to the curb and past it, to the far side of the median; along it
PLUME_TRIGGER_M = 80.0        # it blows when the first racer comes within this of it
# Round 40 (A5, BUG-006: "lets not count this as an official shortcut this way"): no reverse S1 (Round 39's Knoll drop is gone, the S1
# pile's back is only a back again). S2 backward was never built (the garage row stands where its run at the pile's back would be).
# Round 40 (A4, BUG-004: "This reads as a shortcut when it isnt going this direction"): in a reverse race S1's and S2's turn-off clues
# (worn tracks, the knocked-over barricade) are hidden (clues_hidden_reverse_of; MapBuilder.BigJumps.cs, RaceOnly).
CHECKPOINTS_REV = [
    (3565.0, 470.0, "Canton Road"),             # the church leap skips it
    (3430.0, 413.0, "New Chastain Road"),
    (3104.0, 620.0, "past the plume"),          # a cut through the yards up to the drive misses it
    (3090.0, 707.0, "Chastain Ridge Drive"),    # the crest cut skips it
    (3236.0, 927.0, "up Chastain Corner"),
    (3490.0, 1081.0, "Chastain Corner"),
]
RF_ = (RL[0] - dR[0] * RIDGE["ramp"], RL[1] - dR[1] * RIDGE["ramp"])
def fwd_ramp_y(back):   # the forward pile's ramp face, 'back' m behind its lip (MapBuilder.BigJumps.cs KickerProfile)
    y0 = ground(*RF_) - 0.02; k = math.tan(math.radians(RIDGE["angle"])); R = ridge_y - y0; Lk = min(RIDGE["ramp"], 2 * R / k); u = max(0.0, Lk - back)
    return y0 + k * u * u / (2 * Lk)
CT = (RL[0] - dR[0] * PLUME_DOWN_RAMP_M, RL[1] - dR[1] * PLUME_DOWN_RAMP_M); CT_y = fwd_ramp_y(PLUME_DOWN_RAMP_M)
NWc = pts(by_osm[290282527]["points"]); cNW = cum(NWc)
PP = point_at(NWc, PLUME_S, cNW); _a = point_at(NWc, PLUME_S - 2.0, cNW); _b = point_at(NWc, PLUME_S + 2.0, cNW)
PDIR = ((_b[0] - _a[0]) / math.dist(_a, _b), (_b[1] - _a[1]) / math.dist(_a, _b))   # northwest, the way the carriageway runs
PP_y = ground(*PP)
D_p = math.dist(PP, CT); dP = ((CT[0] - PP[0]) / D_p, (CT[1] - PP[1]) / D_p)
def ballistic_u(L, dy, th): return math.sqrt(9.81 * L * L / (2 * (L * math.tan(th) - dy)))   # the horizontal speed for a flight of L m at angle th that ends dy higher
thP = math.radians(PLUME_ANGLE); uP = ballistic_u(D_p, CT_y - PP_y, thP); vyP = uP * math.tan(thP)
houses_p, low_p = {}, []
for k_ in range(1, int(D_p)):
    q = (PP[0] + dP[0] * k_, PP[1] + dP[1] * k_); tt = k_ / uP; y = PP_y + vyP * tt - 4.905 * tt * tt
    low_p.append((k_, y - ground(*q)))
    for i, ring, h, kind in B:
        if near_box(i, q, 2.0) and poly_dist(q, ring) < 1.0:
            houses_p[i] = round(min(houses_p.get(i, 1e9), y - (min(ground(*p_) for p_ in ring) + h)), 1)
tl = math.dist(PP, RL) / uP; over_lip_p = PP_y + vyP * tl - 4.905 * tl * tl - ridge_y
print(f"Manhole plume: at ({PP[0]:.1f}, {PP[1]:.1f}) {PP_y:.2f} m, {PLUME_S:.0f} m along the northwest-bound carriageway, its way {math.degrees(math.atan2(PDIR[0], PDIR[1])) % 360:.0f} deg; "
      f"the throw at {PLUME_ANGLE} deg toward bearing {math.degrees(math.atan2(dP[0], dP[1])) % 360:.0f}, {D_p:.1f} m to the forward pile's ramp ({CT_y - PP_y:+.1f} m): "
      f"{uP:.1f} m/s across, {vyP:.1f} up ({math.hypot(uP, vyP):.1f} m/s), {D_p / uP:.2f} s, {vyP * vyP / 19.62:.1f} m up; {over_lip_p:.1f} m over the forward lip; roofs under it {houses_p}")
# the trees under the flight's lower part kept off (clearance over the ground under 22 m), and off its first meters over the road and the median
clear_p = [k_ for k_, c in low_p if c < 22.0]
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
nw = way(290282527, a=line_r[-1], b=PP)
for q in resample(nw, 3.0)[1:]: line_r.append(q); offs_r.append(0.0)
for q in resample([PP, CT], 4.0)[1:] + resample([CT, RF_], 2.0)[1:]: line_r.append(q); offs_r.append(0.0)
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
s_plume = SR(PP); s_pland = SR(CT)
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; plume s {s_plume:.0f}, landing s {s_pland:.0f}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps_rev))
def flat(p): return [round(v, 2) for q in p for v in q]
def forgiven_rev(sa, sb): return [c["id"] for c in cps_rev if (c["s"] - sa) % L_rev < (sb - sa) % L_rev]

# Round 40 (A6, BUG-005: "lets put a new shortcut here straight through to the next checkpoint. Just make an obstacle so it is a bit of a
# challenge but saves time if you dont crash"): THE CREST CUT, reverse only. Off Chastain Ridge Drive at the crest just past CP3, north
# straight down the slope through the gap between houses 1245 and 1076 (5.5 m: 1245 moved 1.6 m west, MapBuilder.Round40.cs), through the one gap in a boulder wall terracing the back
# yards (a real thing on a slope this steep: it runs from house 946's wall to past 767, so there is no way round it), between houses
# 1014 and 1092 and down onto the drive past CP4. The obstacle is threading the two gaps at speed on a 27 % slope: a clean run saves
# time, a boulder or a wall costs more. The hint at the turn-off: worn tire tracks into the gap between the houses.
CREST_CTRL = [(3101.0, 621.0), (3096.5, 626.0), (3092.2, 632.0), (3090.95, 636.5), (3090.95, 648.0), (3088.6, 656.0), (3083.6, 664.0),
              (3081.5, 671.5), (3080.8, 681.0), (3083.0, 692.0), (3091.0, 701.0)]
CREST_JOIN = (3103.0, 705.5)
CREST_WALL = dict(z=671.5, x0=3047.0, x1=3136.0, gap=(3081.5, 5.2), r=(0.65, 0.95), step=1.55)   # Round 40: below the steepest drop (z 650-660), its gap over the one between 1014 and 1092   # the boulder wall: its line, the gap (middle, width), the boulders' sizes
s_cr0 = SR(CREST_CTRL[0]); s_cr1 = SR(CREST_JOIN)
crest = resample(chaikin([point_at(lap_r, s_cr0, Cr)] + CREST_CTRL[1:] + [point_at(lap_r, s_cr1, Cr)], 2), 2.0)
fg_cr = forgiven_rev(s_cr0, s_cr1)
boulders = []
x_ = CREST_WALL["x0"]; k_ = 0
while x_ <= CREST_WALL["x1"]:
    g0, gw = CREST_WALL["gap"]
    if abs(x_ - g0) > gw / 2 + 0.8 and not any(near_box(i, (x_, CREST_WALL["z"]), 2.0) and poly_dist((x_, CREST_WALL["z"]), ring) < 1.2 for i, ring, h, kind in B):
        r_ = CREST_WALL["r"][0] + (CREST_WALL["r"][1] - CREST_WALL["r"][0]) * ((k_ * 0.618) % 1.0)
        boulders.append([round(x_, 2), round(CREST_WALL["z"] + 0.35 * math.sin(k_ * 1.7), 2), round(r_, 2)])
    x_ += CREST_WALL["step"]; k_ += 1
plen_cr = cum(crest)[-1]
_cc = cum(crest); _gy = [ground(*q) for q in crest]
_off_road = [nearest_s(lap_r, q)[0] > 9.0 for q in crest]   # never on the drive: the strip and its blend stay 9 m off the lap's line
_keep = [k for k in range(len(crest)) if _off_road[k]]
crest_g = [crest[k] for k in _keep]
crest_gy = [round(sum(_gy[j] * math.exp(-((_cc[j] - _cc[k]) / 5.0) ** 2 / 2) for j in range(len(crest))) / sum(math.exp(-((_cc[j] - _cc[k]) / 5.0) ** 2 / 2) for j in range(len(crest))), 2) for k in _keep]
print(f"crest cut's line graded: {len(crest_g)} points (from {_cc[_keep[0]]:.0f} to {_cc[_keep[-1]]:.0f} m along it), the ground moved by up to {max(abs(a - _gy[k]) for k, a in zip(_keep, crest_gy)):.2f} m")
print(f"crest cut: leaves the reverse lap at s {s_cr0:.0f}, rejoins at s {s_cr1:.0f}: {plen_cr:.0f} m against {(s_cr1 - s_cr0) % L_rev:.0f} m on the lap; forgives CP {fg_cr}; "
      f"{len(boulders)} boulders in the wall at z {CREST_WALL['z']}, its gap at x {CREST_WALL['gap'][0]} ({CREST_WALL['gap'][1]} m); buildings within 3 m of its line: "
      f"{ {i: round(min(poly_dist(q, ring) for q in crest), 1) for i, ring, h, kind in B if near_box(i, crest[len(crest) // 2], 60) and min(poly_dist(q, ring) for q in crest) < 3.0} }")
st_cr = cum(crest)
def st_at(path, q): return nearest_s(path, q)[1]
sGap0, sGap1, sWall, sGap2 = st_at(crest, (3090.95, 637.0)), st_at(crest, (3090.95, 648.0)), st_at(crest, (CREST_WALL["gap"][0], CREST_WALL["z"])), st_at(crest, (3080.3, 681.0))

# Round 41 (A, Dan 2026-10-17 21:17: "Lets not do fake jumps off of ramps ... For this particular one, lets make it go downstairs instead to an
# underground crypt, you emerge out in graveyard with a jump that is made of the stairs of the exit. This should make it enough to get a few
# seconds bonus"): THE CRYPT, reverse only; Round 40's church leap (the climb, the gallery, the aimed flight out of the window) is gone.
# Off Canton Road southbound just past the start, across the church's front lawn, in through its front doors (they break), down the aisle and
# down a straight stair (driven as a ramp) through the nave (church 74's north wing and its spine are hollow: MapBuilder.ChurchCrypt.cs) into
# the crypt under the spine; round to the southwest in its vaulted hall (tombs, candles), then the catacomb under the churchyard, the storage
# row (63) and its lot, a bend to the right under the lot's bank and up the exit stair out of the bank into the graveyard below it, beside New
# Chastain Road. The top of the stair and its stone surround are the launch: a real jump (the standing rule: no aimed flight), over the
# graveyard and its open gate onto New Chastain Road's northwest-bound carriageway. The ground right beside the church is S2's (its landing and
# its way out between the churches), so the graveyard is on the low ground between the lot's bank and New Chastain Road, the side toward it.
# The bend before the exit stair sets the jump: round it at its grip (about 24 m/s) the stair brings a vehicle to the lip at 25 to 30 m/s and it
# lands on the carriageway 40 to 57 m on; too fast round the bend is its wall, too slow at the lip and it comes down among the headstones.
def bdir(b): return (math.sin(math.radians(b)), math.cos(math.radians(b)))
def arc_from(p, head, R, turn, n=16):   # turn > 0: to the right (the bearing grows)
    sgn = 1 if turn > 0 else -1; rd = bdir(head + 90 * sgn); c = (p[0] + rd[0] * R, p[1] + rd[1] * R); out = []
    for k in range(1, n + 1):
        h = head + turn * k / n; rr = bdir(h - 90 * sgn); out.append((c[0] + rr[0] * R, c[1] + rr[1] * R))
    return out
CR = dict(door=(3535.0, 576.95), door_w=5.6, door_h=4.4, stair_top=(3530.5, 576.95), stair_foot=(3501.0, 576.95), hall_r=30.0, hall_w=13.0, lane=7.0, hall_lead=6.0,
          bend_r=40.0, lip=(3385.0, 450.0), lip_head=296.0, exit_stair=22.0, clear=5.5, cover=0.6, lip_up=0.85, floor=330.5, hall_floor=324.0, bend_floor=317.5)   # bend_floor: the vault stays covered under the parking lot's lane (x 3416) and opens in the bank west of it
# the way to the doors: off the southbound lane in one arc, then straight in square to them (Round 41 checks: Round 40's tighter turn, then an arc ending at
# the doors, put fast vehicles into the wall beside them)
CR_ARC_R, CR_RUN_IN = 28.0, 6.0   # the arc ends 7.5 m short of the doors: a straight run-in square to them (a vehicle cuts an arc's inside)
_ac = (CR["door"][0] + CR_RUN_IN, CR["door"][1] + CR_ARC_R)
CR_APPROACH = [(_ac[0] + CR_ARC_R, _ac[1] + 14.0)] + [(_ac[0] + CR_ARC_R * math.cos(math.radians(a)), _ac[1] - CR_ARC_R * math.sin(math.radians(a))) for a in range(0, 91, 6)]
L_ = CR["lip"]; LH_ = CR["lip_head"]; F2_ = (L_[0] - bdir(LH_)[0] * CR["exit_stair"], L_[1] - bdir(LH_)[1] * CR["exit_stair"])
def _build(h1):
    hall = [(CR["stair_foot"][0] - CR["hall_lead"], CR["stair_foot"][1])] + arc_from((CR["stair_foot"][0] - CR["hall_lead"], CR["stair_foot"][1]), 270.0, CR["hall_r"], h1 - 270.0); back = arc_from(F2_, LH_ + 180.0, CR["bend_r"], -(LH_ - h1)); return hall, back
def _miss(h1):
    hall, back = _build(h1); E, B0 = hall[-1], back[-1]; return abs((math.degrees(math.atan2(B0[0] - E[0], B0[1] - E[1])) % 360) - h1)
cr_h1 = min((x * 0.05 for x in range(3000, 5200)), key=_miss)   # the catacomb's bearing: the hall's arc and the bend's meet it tangent
_hall, _back = _build(cr_h1)
crypt = resample([CR["door"], CR["stair_top"], CR["stair_foot"]] + _hall + [_back[-1]] + list(reversed(_back))[1:] + [F2_, L_], 1.0)
cc = cum(crypt)
s_top, s_foot, s_hall1, s_bend0, s_bend1 = (st_at(crypt, q) for q in (CR["stair_top"], CR["stair_foot"], _hall[-1], _back[-1], F2_))
s_lipc = cc[-1]
# the least ground over the crypt (6 m either side) and the floor under it: a 5.5 m clear vault under 0.6 m of cover at least
def _low(k):
    a, b = crypt[max(k - 1, 0)], crypt[min(k + 1, len(crypt) - 1)]; t = (b[0] - a[0], b[1] - a[1]); n_ = math.hypot(*t) or 1.0; nn = (-t[1] / n_, t[0] / n_)
    return min(ground(crypt[k][0] + nn[0] * o, crypt[k][1] + nn[1] * o) for o in (-6.0, -3.0, 0.0, 3.0, 6.0))
cr_low = [_low(k) for k in range(len(crypt))]
cr_lim = [v - CR["cover"] - CR["clear"] for v in cr_low]
lip_y_c = ground(*L_) + CR["lip_up"]
def stair_u(u, r):   # 0 to 1, straight between ends rounded over r (a fraction) at each end
    m = 1.0 / (1.0 - r)
    if u < r: return m * u * u / (2 * r)
    if u > 1 - r: return 1.0 - m * (1 - u) ** 2 / (2 * r)
    return m * (u - r / 2)
# the floor: the stair from the church's floor down to the hall; the hall level; then the most a 9 % grade allows under the cover's limit
cr_y = []
for k, s_ in enumerate(cc):
    if s_ <= s_top: y = CR["floor"]
    elif s_ <= s_foot: y = CR["floor"] + (CR["hall_floor"] - CR["floor"]) * stair_u((s_ - s_top) / (s_foot - s_top), 3.0 / (s_foot - s_top))
    elif s_ >= s_bend1: y = None
    else: y = min(CR["hall_floor"], cr_lim[k] - 0.15, CR["bend_floor"] if s_ >= s_bend0 - 12.0 else 1e9)
    cr_y.append(y)
_k1 = next(k for k, s_ in enumerate(cc) if s_ > s_foot); _k2 = next(k for k, s_ in enumerate(cc) if s_ >= s_bend1)
for _ in range(2):
    for k in range(_k1, _k2): cr_y[k] = min(cr_y[k], cr_lim[k] - 0.15) if cc[k] < s_bend0 else cr_y[k]
    for k in range(_k1, _k2): cr_y[k] = min(cr_y[k], cr_y[k - 1] + 0.08)
    for k in range(_k2 - 2, _k1 - 1, -1): cr_y[k] = min(cr_y[k], cr_y[k + 1] + 0.08)
# eased (a running mean over 11 m, never over the cover's limit): no crest a vehicle at speed comes light over before the bend (Round 41 check)
for _ in range(3):
    _sm = cr_y[:]
    for k in range(_k1 + 5, _k2 - 5): _sm[k] = min(sum(cr_y[k - 5:k + 6]) / 11.0, cr_lim[k] - 0.15 if cc[k] < s_bend0 else 1e9)
    cr_y = _sm
_k0 = next(k for k, s_ in enumerate(cc) if s_ >= s_bend0)
for k in range(_k0, _k2): cr_y[k] = cr_y[_k0 - 1]   # level round the bend (it opens into the bank's cut there)
_yb = cr_y[_k2 - 1]
for k in range(_k2, len(crypt)): u = (cc[k] - cc[_k2 - 1]) / (s_lipc - cc[_k2 - 1]); cr_y[k] = _yb + (lip_y_c - _yb) * u   # the exit stair
# where the vault opens: from the portal (its clear height reaches the cover's limit) to the lip the stair is an open cut in the bank
s_portal = next(cc[k] for k in range(len(crypt)) if cc[k] > s_bend0 and cr_y[k] + CR["clear"] > cr_low[k] - CR["cover"])
s_west = st_at(crypt, (3502.6, 576.95))   # the spine's west wall: the stairwell is in the church to here, the vault from here
_over = max(cr_y[k] + CR["clear"] - (cr_low[k] - CR["cover"]) for k in range(len(crypt)) if s_west <= cc[k] < s_portal)
if _over > 0.01: raise SystemExit(f"the crypt's vault stands {_over:.2f} m into its cover")
_gr = [(cr_y[k + 1] - cr_y[k]) / max(cc[k + 1] - cc[k], 1e-6) for k in range(len(crypt) - 1)]
_pk = next(k for k in range(len(crypt)) if cc[k] >= s_portal)
print(f"crypt: the portal at ({crypt[_pk][0]:.1f}, {crypt[_pk][1]:.1f}); the catacomb's bearing {cr_h1:.1f}; door to lip {s_lipc:.0f} m (the stair {s_top:.0f}-{s_foot:.0f}, the hall to {s_hall1:.0f}, the catacomb to {s_bend0:.0f}, the bend to "
      f"{s_bend1:.0f}, the portal at {s_portal:.0f}, the lip at {s_lipc:.0f}); floor {min(cr_y):.2f} to {CR['hall_floor']:.2f} m, the lip {lip_y_c:.2f} m; grades {min(_gr) * 100:.0f} % to {max(_gr) * 100:.0f} %")
# the flight from the lip (heading 288 over the graveyard onto the carriageway), a real one: where each speed comes down
_ang = math.atan2(cr_y[-1] - cr_y[-4], cc[-1] - cc[-4])
def cr_flight(v):
    vx, vy = v * math.cos(_ang), v * math.sin(_ang); t = 0.0; q = L_
    while t < 6.0:
        t += 0.01; q = (L_[0] + bdir(LH_)[0] * vx * t, L_[1] + bdir(LH_)[1] * vx * t); y = lip_y_c + vy * t - 4.905 * t * t
        if y <= ground(*q): break
    return t, vx * t, q, vy * vy / 19.62
CR_DESIGN = 30.0
for v in (22.0, 25.0, CR_DESIGN, 30.0, 34.0):
    t_, d_, q_, top_ = cr_flight(v); dd, ss = nearest_s(lap_r, q_)
    print(f"    off the lip ({math.degrees(_ang):.0f} deg) at {v:.0f} m/s: {t_:.2f} s, {d_:.0f} m, {top_:.1f} m up, down at ({q_[0]:.1f}, {q_[1]:.1f}), {dd:.1f} m from the race line at s {ss:.0f}")
_tD, _dD, cr_land, _ = cr_flight(CR_DESIGN)
s_cland = SR(cr_land); s_ch0 = SR(CR_APPROACH[0]); s_ch1 = s_cland + 30.0
_fl = [(L_[0] + bdir(LH_)[0] * d, L_[1] + bdir(LH_)[1] * d) for d in range(2, int(_dD), 2)]
_vx, _vy = CR_DESIGN * math.cos(_ang), CR_DESIGN * math.sin(_ang)
_fl_y = [lip_y_c + _vy * (d / _vx) - 4.905 * (d / _vx) ** 2 for d in range(2, int(_dD), 2)]
_after = resample([point_at(lap_r, s_cland + k_ * 2.0, Cr) for k_ in range(16)], 2.0)
_appr = resample([point_at(lap_r, s_ch0, Cr)] + CR_APPROACH[1:] + [CR["door"]], 2.0)[:-1]
_join = chaikin(_fl[-4:] + _after[1:6], 3)   # the flight's line eased onto the lap's (no kink where a vehicle comes down)
_join_y = [ground(*q) for q in _join]
church = _appr + crypt[::2] + [crypt[-1]] + _fl[:-4] + resample(_join, 2.0) + _after[6:]
church_y = [round(ground(*q), 2) for q in _appr] + [round(cr_y[k], 2) for k in range(0, len(crypt), 2)] + [round(cr_y[-1], 2)] + [round(v, 2) for v in _fl_y[:-4]] + [round(ground(*q), 2) for q in resample(_join, 2.0)] + [round(ground(*q), 2) for q in _after[6:]]
fg_ch = forgiven_rev(s_ch0, s_ch1)
_canton = [point_at(lap_r, s_ch0 + d, Cr) for d in range(-60, 60, 4)]
_ac_c = cum(_appr); _ag_g = [ground(*q) for q in _appr]
_agk = [k for k, q in enumerate(_appr) if nearest_s(_canton, q)[0] > 4.0 and q[0] > CR["door"][0] + 4.0]   # off the road (its edge 2.9 m from the race line) and short of the doors
_ag = [_appr[k] for k in _agk]
_gs = lambda k: sum(_ag_g[j] * math.exp(-((_ac_c[j] - _ac_c[k]) / 6.0) ** 2 / 2) for j in range(len(_appr))) / sum(math.exp(-((_ac_c[j] - _ac_c[k]) / 6.0) ** 2 / 2) for j in range(len(_appr)))
_sA = _ac_c[_agk[0]]; _kB = min(_agk, key=lambda k: abs(_ac_c[k] - (_sA + 24.0))); _yA, _yB = _ag_g[_agk[0]], _gs(_kB)
# from the road's edge a steady 5 % up to the lawn (it rose 1 m in 8 m there), then the lawn smoothed
_agy = [round(_yA + (_yB - _yA) * (_ac_c[k] - _sA) / (_ac_c[_kB] - _sA), 2) if _ac_c[k] <= _ac_c[_kB] else round(_gs(k), 2) for k in _agk]
print(f"the crypt's way to the doors graded: {len(_ag)} points, the ground moved by up to {max(abs(a - _ag_g[k]) for k, a in zip(_agk, _agy)):.2f} m")
plen_ch = cum(church)[-1]
sDoor = st_at(church, CR["door"]); sLipC = st_at(church, L_); sLandC = st_at(church, cr_land)
sHall = sDoor + s_foot; sBend0 = sDoor + s_bend0; sBend1 = sDoor + s_bend1; sCat = sDoor + s_hall1
print(f"crypt: leaves the reverse lap at s {s_ch0:.0f}, rejoins at s {s_ch1:.0f}: {plen_ch:.0f} m against {(s_ch1 - s_ch0) % L_rev:.0f} m on the lap; forgives CP {fg_ch}")
# the graveyard: an iron fence round the low ground below the bank (its gate open where the flight leaves it, onto the road), headstones in
# rows, a few that break (beside the flight's line, where a short or crooked landing goes), old trees; nothing on the flight's line
GY_RING = [(3407.5, 461.5), (3407.5, 437.0), (3396.0, 436.0), (3383.0, 442.5), (3370.0, 451.0), (3361.0, 461.5)]
def _fl_d(q):   # off the flight's line (from the lip, 60 m on)
    b = (L_[0] + bdir(LH_)[0] * 60.0, L_[1] + bdir(LH_)[1] * 60.0); return seg_dist(q, L_, b)
def _cut_d(q): return min(seg_dist(q, crypt[k], crypt[k + 1]) for k in range(len(crypt) - 1) if cc[k] >= s_portal - 2.0)
def _inside(q, ring):
    ins = False
    for i in range(len(ring)):
        a, b = ring[i], ring[(i + 1) % len(ring)]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]: ins = not ins
    return ins
graves = []
_rd = bdir(LH_ + 90.0); _fd = bdir(LH_)
for i_ in range(-12, 13):
    for j_ in range(-20, 21):
        q = (L_[0] + _fd[0] * j_ * 2.2 + _rd[0] * i_ * 2.6, L_[1] + _fd[1] * j_ * 2.2 + _rd[1] * i_ * 2.6)
        if not _inside(q, GY_RING) or min(seg_dist(q, GY_RING[i], GY_RING[(i + 1) % len(GY_RING)]) for i in range(len(GY_RING))) < 1.6 or _fl_d(q) < 4.2 or _cut_d(q) < 5.6: continue
        h_ = (i_ * 7 + j_ * 13) % 5
        graves.append([round(q[0], 2), round(q[1], 2), round((LH_ + 180.0 + (h_ - 2) * 3.0) % 360, 1), h_, 1 if _fl_d(q) < 7.5 and (i_ + j_) % 2 == 0 else 0])
gy_trees = [[x, z, r] for x, z, r in ((3403.0, 458.0, 0.55), (3373.0, 457.5, 0.6), (3400.0, 439.5, 0.5), (3385.0, 459.5, 0.5)) if _fl_d((x, z)) > 8.0 and _cut_d((x, z)) > 6.0]
_gate = None
for i in range(len(GY_RING)):
    a, b = GY_RING[i], GY_RING[(i + 1) % len(GY_RING)]
    for k_ in range(0, 61):
        q = (L_[0] + _fd[0] * k_, L_[1] + _fd[1] * k_)
        if seg_dist(q, a, b) < 0.6: _gate = (i, round(q[0], 2), round(q[1], 2)); break
    if _gate: break
print(f"graveyard: {len(graves)} headstones ({sum(g_[4] for g_ in graves)} that break), {len(gy_trees)} old trees, the gate (fence side {_gate[0]}) where the flight leaves it at ({_gate[1]}, {_gate[2]}); "
      f"the fence from the race line {min(nearest_s(lap_r, q)[0] for q in GY_RING):.1f} m at least")
crypt_data = {"building": 74, "door": list(CR["door"]), "door_w": CR["door_w"], "door_h": CR["door_h"], "line": flat(crypt), "y": [round(v, 3) for v in cr_y],
              "s_top": round(s_top, 2), "s_foot": round(s_foot, 2), "s_west": round(s_west, 2), "s_hall": round(s_hall1, 2), "s_bend0": round(s_bend0, 2), "s_bend1": round(s_bend1, 2),
              "s_portal": round(s_portal, 2), "lane": CR["lane"], "hall_w": CR["hall_w"], "clear": CR["clear"], "lip_dir": [round(bdir(LH_)[0], 5), round(bdir(LH_)[1], 5)],
              "graveyard": flat(GY_RING), "gate": [_gate[0], _gate[1], _gate[2]], "graves": [v for g_ in graves for v in g_], "trees": [v for t_ in gy_trees for v in t_]}

shortcuts.append({"id": "rc_crest_cut", "name": "Crest cut", "direction": "reverse", "path": flat(crest), "half_width_m": 3.0,
                  "entry_inset_m": 14.0, "entry_margin_m": 1.0, "forgives": fg_cr, "ai_speed": 17.0,
                  # slow through the gap between the houses, slower through the boulders' gap, then on down
                  "ai_profile": [round(v, 1) for v in (0.0, 18.0, sGap0 - 2.0, 17.0, sGap1, 17.0, sWall - 3.0, 16.0, sWall + 2.0, 16.0, sGap2, 15.0, plen_cr - 8.0, 16.0)],
                  # a sloppy try: too fast through the gaps (a wall, a boulder)
                  "ai_slip": 0.22, "ai_slip_profile": [round(v, 1) for v in (0.0, 21.0, sGap0 - 2.0, 21.0, sGap1, 20.0, sWall - 3.0, 20.0, sWall + 2.0, 19.0, plen_cr - 8.0, 19.0)]})
shortcuts.append({"id": "rch_crypt", "name": "Crypt", "direction": "reverse", "path": flat(church), "path_y": church_y, "half_width_m": 3.0,
                  "entry_inset_m": round(max(10.0, sDoor - 8.0), 1), "entry_margin_m": 1.5, "forgives": fg_ch, "ai_speed": 20.0,
                  # Round 41: through the doors, down the stair, round the hall, flat out down the catacomb, braked for the bend, up the stair to the lip at the jump's speed
                  "ai_profile": [round(v, 1) for v in (0.0, 24.0, sDoor - 6.0, 22.0, sHall + 2.0, 24.0, sCat, 25.0, sCat + 20.0, 36.0, sBend0 - 30.0, 36.0, sBend0, 27.0, sBend1, 27.0, sLipC, CR_DESIGN, sLandC + 4.0, 24.0, plen_ch - 6.0, 26.0)],
                  # a sloppy try: in too hot round the hall and the bend (a tomb, the wall), or out of the bend slow (short among the headstones)
                  "ai_slip": 0.2, "ai_slip_profile": [round(v, 1) for v in (0.0, 24.0, sDoor - 6.0, 22.0, sHall + 2.0, 28.0, sCat, 28.0, sCat + 20.0, 38.0, sBend0 - 30.0, 38.0, sBend0, 33.0, sBend1, 22.0, sLipC, 22.0, sLandC + 4.0, 22.0, plen_ch - 6.0, 26.0)]})
# the reverse race layer
ai_zones_rev = [{"id": "manhole_plume", "s0": round(s_plume - 90.0, 1), "s1": round(s_plume + 1.0, 1), "speed": 25.0}]
reset_skip_rev = [{"id": "manhole_plume", "s0": round(s_plume - 3.0, 1), "s1": round(s_pland + 4.0, 1), "to_s": round(s_pland + 30.0, 1)}]
closures_rev, stubs_rev, merged_rev = closures_for(lap_r, (crest, _appr, _fl + _after), lambda r, contact: False)   # Round 41: the crypt's ways above ground only
# the barrier across the northwest-bound carriageway past the plume: 40 m on (12 m past the cover it stood in the plume's first meters)
for c in closures_rev:
    if c["osm_id"] == 290282527 and math.dist((c["x"], c["z"]), PP) < 35.0:
        q = point_at(NWc, PLUME_S + 40.0, cNW); q2 = point_at(NWc, PLUME_S + 42.0, cNW); c["x"], c["z"] = round(q[0], 2), round(q[1], 2)
        c["dx"], c["dz"] = round((q2[0] - q[0]) / math.dist(q, q2), 4), round((q2[1] - q[1]) / math.dist(q, q2), 4); c["arc_m"] = 40.0
        print(f"  (the barrier on New Chastain Road past the plume moved 40 m on, to ({q[0]:.0f}, {q[1]:.0f}))")
print(f"reverse closures: {len(closures_rev)}; dead-end stubs not barred: {len(stubs_rev)}")
for c in closures_rev: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
plume = {"id": "RP", "at": [round(PP[0], 2), round(PP[1], 2)], "y": round(PP_y, 2), "dir": [round(PDIR[0], 5), round(PDIR[1], 5)], "right_m": PLUME_RIGHT_M, "left_m": PLUME_LEFT_M,
         "half_len_m": PLUME_HALF_LEN_M, "target": [round(CT[0], 2), round(CT_y, 2), round(CT[1], 2)], "target_dir": [round(-dR[0], 5), round(-dR[1], 5)], "angle_deg": PLUME_ANGLE, "trigger_m": PLUME_TRIGGER_M}
for b_ in (bj1, bj2): b_["clues_hidden_reverse_of"] = SETTINGS["name"]

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
            {"id": "PLUME", "poly": zone(PP, dP, D_p - 12.0, D_p + 24.0, 6.0, 8.0)},   # Round 40: the plume's landing (as the climb's was)
            {"id": "CRYPT", "poly": zone(L_, bdir(LH_), 22.0, 72.0, 7.0, 11.0)}]      # Round 41: the crypt's jump, the graveyard's gate to the far side of the carriageway
def flat(p): return [round(v, 2) for q in p for v in q]
clear = [{"id": "RL flight's last meters and the landing", "points": flat([(RL[0] + dR[0] * (rf["dist"] - 45.0), RL[1] + dR[1] * (rf["dist"] - 45.0)), (RL[0] + dR[0] * (rf["dist"] + 30.0), RL[1] + dR[1] * (rf["dist"] + 30.0))]), "half": 9.0},
         {"id": "RL run-up and pile", "points": flat([RIDGE_RUNUP_FROM, RL]), "half": 6.0},
         {"id": "the way out of the hollow", "points": flat(HOLLOW_RAMP), "half": 7.0},
         {"id": "S1", "points": flat(p1[::3] + [p1[-1]]), "half": 6.0},
         {"id": "S2", "points": flat(p2[::3] + [p2[-1]]), "half": 6.0},
         {"id": "the plume's flight, its lower part", "points": flat([(PP[0] + dP[0] * k_, PP[1] + dP[1] * k_) for k_ in clear_p[::4]] + [(PP[0] + dP[0] * clear_p[-1], PP[1] + dP[1] * clear_p[-1])]), "half": 6.0},
         {"id": "RC the landing on the dead end", "points": flat([CT, RF_, (3112.0, 621.0)]), "half": 6.0},
         {"id": "the crest cut", "points": flat(crest[::2] + [crest[-1]]), "half": 4.0},
         {"id": "the crypt's way to the doors", "points": flat(_appr + [CR["door"]]), "half": 4.0},
         {"id": "the crypt's cut, the graveyard, the jump and its landing", "points": flat([crypt[int(s_portal) - 2], L_, (L_[0] + bdir(LH_)[0] * 75.0, L_[1] + bdir(LH_)[1] * 75.0)]), "half": 9.0},
         {"id": "the graveyard", "points": flat([(3407.0, 449.0), (3363.0, 457.0)]), "half": 13.0}]
# the run-out past the landing: a vehicle that lands across the road at speed runs on into the woods east of it; the trees there
# snap when hit (TreeField.breakable) instead of stopping it dead (Round 33 check: 4 of 11 vehicles stopped on trunks 30-40 m past the road)
breaks = [{"id": "RL run-out", "points": flat([(RL[0] + dR[0] * (rf["dist"] + 6.0), RL[1] + dR[1] * (rf["dist"] + 6.0)), (RL[0] + dR[0] * (rf["dist"] + 75.0), RL[1] + dR[1] * (rf["dist"] + 75.0))]), "half": 22.0},
          # and the woods along the road's east side beyond it, where a vehicle turning back to the road after the landing runs
          {"id": "RL run-out along the road", "points": flat([(Pl[0] + 32.0, Pl[1] + 4.0), (Pl[0] + 52.0, Pl[1] - 60.0)]), "half": 18.0}]
track = {"note": "Round 33: the Chastain Ridge race's world pieces, from Tools/mapgen/make_ridge_route.py (MapBuilder.ChastainRidge.cs). Coordinates are map meters (= Unity x, z).",
         "big_jumps": [rbj, bj1, bj2], "plumes": [plume], "crypt": crypt_data, "boulders": [v for b_ in boulders for v in b_], "crest_tracks": flat(crest[:int(sGap1 / 2.0) + 1]), "landing_zones": landings, "clear_lines": clear, "break_lines": breaks,
         "grades": [{"id": "the way out of the hollow", "points": flat(HOLLOW_RAMP), "y": [round(ground(*HOLLOW_RAMP[0]), 2), round(ground(*HOLLOW_RAMP[1]), 2)], "half": 4.0, "blend": 5.0,
                     "keep_off": flat(NC), "keep_off_m": 5.0},
                    # Round 40 (A6): the crest cut's line through the back yards smoothed along it (a Gaussian of the ground over 5 m: the slope stays,
                    # the lumps that threw a vehicle at speed go), 3 m wide, blending back over 2 m; from the drive's edge to the drive's edge
                    {"id": "the crest cut's line", "points": flat(crest_g), "y": crest_gy, "half": 1.5, "blend": 2.0},
                    # Round 41 (A): the crypt's way to the doors smoothed along it (a Gaussian of the ground over 6 m: the lawn's 0.8 m rise off Canton
                    # Road's edge threw a vehicle into the air in the middle of its turn), kept off the road and short of the doors
                    {"id": "the crypt's way to the doors", "points": flat(_ag), "y": _agy, "half": 3.5, "blend": 3.0, "keep_off": flat(_canton), "keep_off_m": 4.0}]}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_ridge_route.py from Data/map.json. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": pieces, "official_shortcuts": shortcuts, "closures": closures,
    "closure_stubs_not_barred": stubs, "ai_zones": ai_zones, "reset_skip": reset_skip,
    # Round 39 (C): the reverse lap of its own (RaceRoute: loop_rev and the *_rev fields)
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev, "junctions_rev": merged_rev,
    "closures_rev": closures_rev, "ai_zones_reverse": ai_zones_rev, "reset_skip_rev": reset_skip_rev,
    "plumes_rev": ["RP"],   # Round 40: the manhole plume blows in a reverse race (ManholePlume)
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
