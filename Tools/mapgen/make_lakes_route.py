"""Round 44 (Dan, 2026-10-18 18:10): the "Chastain Lakes" race, from Dan's route map (local/reference_photos/chastain_lakes/r44_*,
Google: looked at only; the course is laid on OSM's roads from Data/map.json and on the Chastain Lakes amenity area of Round 32).
One lap, forward only, counterclockwise round the lakes (Dan's arrows):
  1. North Lakeside Drive west along the top, from Bells Ferry Road to its west end
  2. left onto Lakeside Drive: the west loop, then east and south past Laurel Green Court, round to its end at the amenity lot
  3. THE LAKE TRAIL: through the lot, the walk between the pool house and the courts, over the footbridge across the lake's outflow
     (its caution tape and 2x4s breakable), north along the dam's crest with the lake on the left, onto Bells Ferry Road
  4. Bells Ferry Road north to North Lakeside Drive, left: the lap
Shortcuts (Dan's two red lines):
  W "Boat jump" (Round 46; Round 44's spoil pile is gone): off North Lakeside Drive westbound by Bankside Point, down between houses
     146 and 147 to a dock on West Chastain Lake's north shore, onto a pontoon party boat tied up off its end and up its water slide,
     a real jump (physics only) down the lake's south arm (77 m of water) onto Lakeside Drive at Lakeside Point (MapBuilder.LakesBoat.cs).
     Skips the west loop and Lakeside Drive's west end.
  E "Lake drain": off Lakeside Drive just short of the lot, across the lawn, along the gazebo pier and through the gazebo (its far
     railing breaks), off the end into the lake drain (a morning-glory spillway, MapBuilder.LakesRace.cs), down its shaft into LD and
     along B28 (Tools/mapgen/pipe_network.py) under the lake, up MH14's riser and out of the manhole in North Lakeside Drive, facing
     west. Skips the lot, the trail, Bells Ferry Road and the east end of North Lakeside Drive.
Open roads ("traffic": true), two-way roads raced on their right-hand lane, as the Chastain Ridge race.

Hand choices are the constants below; everything else is computed (re-run after mapgen and after pipe_network.py; it reads
Assets/Generated/terrain.r16 and Data/pipes/network.json). It writes:
  Data/routes/chastain_lakes.json   the race (RaceRoute.cs format)
  Data/tracks/chastain_lakes.json   what the map build makes (MapBuilder.LakesRace.cs): the trail, the widened footbridge, pier and
                                    gazebo, the lake drain, the boat jump (lakes.boat), landing zones, cleared lines, grades
Picture for checking (git-ignored, it shows houses): local/round44/layout.png with --plot.

Usage: python Tools/mapgen/make_lakes_route.py [--plot]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/chastain_lakes.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/chastain_lakes.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
NET = json.load(open(os.path.join(ROOT, "Data/pipes/network.json")))
roads = m["roads"]; by_osm = {r["osm_id"]: r for r in roads}

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
def pts(flat): return [(flat[i], flat[i + 1]) for i in range(0, len(flat), 2)]
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
def bdir(deg): return (math.sin(math.radians(deg)), math.cos(math.radians(deg)))
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
def ring_dist(q, ring): return min(seg_dist(q, ring[i], ring[(i + 1) % len(ring)]) for i in range(len(ring)))
def poly_dist(q, ring): return 0.0 if inside(q, ring) else ring_dist(q, ring)
B = [(i, pts(b["footprint"]), b.get("height", 6.0)) for i, b in enumerate(m["buildings"])]
BBOX = {i: (min(p[0] for p in ring), max(p[0] for p in ring), min(p[1] for p in ring), max(p[1] for p in ring)) for i, ring, h in B}
def near_box(i, q, r): b = BBOX[i]; return b[0] - r < q[0] < b[1] + r and b[2] - r < q[1] < b[3] + r
LAKES = {w["name"]: pts(w["outline"]) for w in m["water"]}
WEST, EAST = LAKES["West Chastain Lake"], LAKES["East Chastain Lake"]
def pond_level(ring):   # the map build's WaterLevel (the median ground inside) + PondLift, as pipe_network.py's
    xs = [x for x, z in ring]; zs = [z for x, z in ring]; s = []
    for x in np.arange(min(xs), max(xs), 2.0):
        for z in np.arange(min(zs), max(zs), 2.0):
            if inside((x, z), ring): s.append(ground(x, z))
    s.sort(); return s[len(s) // 2] + 0.15
WEST_Y, EAST_Y = pond_level(WEST), pond_level(EAST)

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Chastain Lakes",
    "direction": "forward",
    "directions": ["forward", "reverse"],   # Round 47 (A): the reverse lap of its own (loop_rev), no shortcuts that way yet
    "laps": 2,
    "traffic": True,
    "gate_half_width_m": 12.0,
    "checkpoint_half_width_m": 10.0,
    "gate_below_m": 6.0,
    "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    "closure_clear_m": 12.0,
    "closure_extra_width_m": 6.0,
}
LEAD_IN_M = 70.0
GATE_AT = (760.0, 1504.0)            # North Lakeside Drive's straight along the top, 260 m before the lake jump's turn-off
TRAIL_W = 7.0                        # the lake trail's tread (every offered vehicle, room to pass: wider than real, Noonday's lesson)
TRAIL_LAKE_GAP = 4.2                 # the tread's middle at least this far from the lake's edge
# the lake trail, hand points: the lot's aisle, between pool house 266 and the north courts, the bend onto the footbridge, the
# footbridge (moved 7 m east at its south end so the trail meets it in one S: it still spans the outflow just below the spillway), the
# dam's crest (Round 32's shore path pushed off the water), the lawn up to Bells Ferry Road
# Round 49 (C, Dan 2026-10-19 08:37: "the trail goes behind the pool and twists around to the bridge, not between the pool area and the tennis
# courts"): from the lot's west end north past the bathhouse (267) on its lake side (photo 2: the concrete walk below the pool's granite wall and
# iron fence), round the pool deck's north-west corner and along its north side, then a bend onto the footbridge (photo 3: gravel with tan brick
# edging, the lake on the left); the footbridge moved and turned to meet it (it spans the spillway where the outflow leaves the lake). The piece between the pool
# house and the courts is gone (grass again).
BRIDGE_S, BRIDGE_N = (1342.0, 1138.5), (1351.0, 1151.5)   # over the spillway where the outflow leaves the lake (photos 3 and 7)
TRAIL_CTRL = [(1308.0, 1074.4), (1314.0, 1079.6), (1317.8, 1085.5), (1319.0, 1093.0), (1318.2, 1101.0), (1318.4, 1108.0), (1320.0, 1115.0), (1323.5, 1121.0),
              (1328.0, 1126.5), (1333.5, 1131.5), (1338.3, 1135.4)]
CONCRETE_TO = (1320.6, 1117.0)       # photo 2's concrete walk past the bathhouse; gravel with tan brick edging from here on (photo 3)
DAM_CTRL = [(1351.4, 1156.5), (1353.5, 1165.0), (1357.0, 1175.0), (1360.0, 1185.0), (1363.0, 1195.0), (1365.0, 1205.0), (1368.0, 1215.0), (1372.0, 1225.0),
            (1378.0, 1235.0), (1388.0, 1245.0), (1398.5, 1253.5)]
BF_JOIN = (1407.0, 1260.0)           # onto Bells Ferry Road's northbound lane
CHECKPOINTS = [
    (1150.0, 1340.0, "North Lakeside Drive"),   # the lake drain's exit is past it: a cut from the drain's exit back east misses nothing
    (640.0, 1500.0, "Bankside Point"),
    (402.0, 1352.0, "the west loop"),           # the lake jump skips it
    (560.0, 1264.0, "Lakeside Drive"),
    (800.0, 1102.0, "Laurel Green Court"),
    (1100.0, 950.0, "Lakeside Drive south"),
    (1268.0, 1035.0, "the amenity lot"),        # the lake drain leaves before it
    (1361.0, 1200.0, "the dam"),
    (1385.0, 1310.0, "Bells Ferry Road"),
]
# W, the boat jump (Round 46, Dan 2026-10-18 22:19: "a boat off the dock that has a large slide on it that can be used for a ramp"; Round 44's
# spoil pile and its narrow crossing are gone): off North Lakeside Drive by Bankside Point, down between houses 146 and 147 to a dock on West
# Chastain Lake's north shore; a pontoon party boat tied up off its end, its big water slide the ramp (driven up from the main deck to its top
# over the bow); a real jump (physics alone) down the lake's south arm, 77 m of water on the line, onto Lakeside Drive at Lakeside Point.
# The run-up is the line measured in the exe (Round 46: 34.4 to 37.5 m/s at the shore, flat out, every offered vehicle)
W_RUN = [(578.5, 1503.0), (574.8, 1450.0), (571.5, 1410.0)]
W_BEARING = 184.0
W_LIP = (567.3, 1352.0)               # the slide's top over the boat's bow
# a big two-deck pontoon party boat (20 m, 7 m beam), its main deck 1.0 m over the water; its slide tops out 4.5 m over the water, 23 degrees
# at the top (a curve from the main deck, as a slide's run-out is); the chute 4.6 m wide between low walls
BOAT = dict(lip_over=4.5, angle=23.0, deck_over=1.0, beam=7.0, chute=4.6, length=20.0, dock_w=5.0, wall=0.6)
W_DESIGN = 40.0                       # the AI flat out down the run-up (no vehicle reaches it there)
W_SLIP = 27.0                         # a sloppy AI try: off the slide at this, into the lake
W_REJOIN_PAST = 35.0                  # rejoins the lap this far east of Lakeside Point
W_SIGN = (589.0, 1496.6)              # the hint: a PARTY sign with balloons on North Lakeside Drive's south verge, facing the westbound lap
# E, the lake drain: off Lakeside Drive, across the lawn, onto the pier on its line; the drain 9.5 m past the gazebo's end
PIER_ROOT = (1301.5, 1098.5); PIER_DIR = (-0.825, 0.565); PIER_LEN = 15.0; GAZEBO_HALF = 3.5; PIER_HW = 2.2
DRAIN_AT = 31.0                      # along the pier's line from its root (LD in pipe_network.py stands here)
DRAIN_R, SHAFT_R = 4.8, 4.2          # the bell's rim, the shaft
E_FROM = (1307.1, 1075.7)            # leaves the lap on the drive into the lot: one left turn (12 m radius) onto the pier's line, 6 m short of its root
E_TURN_R = 12.0
E_REJOIN_PAST = 45.0                 # rejoins the lap this far west of the manhole
E_DESIGN = 20.0                      # the AI's speed along the pier

# ---------------------------------------------------------------- the lap
def way(osm, rev=False, a=None, b=None):
    p = pts(by_osm[osm]["points"])
    if rev: p = p[::-1]
    sa = nearest_s(p, a)[1] if a else 0.0
    sb = nearest_s(p, b)[1] if b else None
    return cut(p, sa, sb)
NL, LD, BF = 9136281, 9128458, 320582951
lake_trail = chaikin(TRAIL_CTRL, 3)
def off_lake(q, ring, gap):   # pushed east (away from the water) until its middle is 'gap' from the lake's edge
    x, z = q
    for _ in range(80):
        if not inside((x, z), ring) and ring_dist((x, z), ring) >= gap: break
        x += 0.25
    return (x, z)
dam = [off_lake(q, EAST, TRAIL_LAKE_GAP) for q in chaikin(DAM_CTRL, 3)]
bf_line = pts(by_osm[BF]["points"])
_d, s_bfj = nearest_s(bf_line, BF_JOIN)
# Round 49 check: the trail ran straight off the footbridge's north end into the dam's line (a 25 degree bend on the rail end: in reverse an AI
# hit it and a mower slid off beside the bridge); now 6 m straight on along the bridge first
_bd = ((BRIDGE_N[0] - BRIDGE_S[0]) / math.dist(BRIDGE_S, BRIDGE_N), (BRIDGE_N[1] - BRIDGE_S[1]) / math.dist(BRIDGE_S, BRIDGE_N)); BRIDGE_X = (BRIDGE_N[0] + _bd[0] * 6.0, BRIDGE_N[1] + _bd[1] * 6.0)
_dam0 = [q for q in dam[::6] if q[1] > BRIDGE_X[1] + 4.0]
trail = resample(lake_trail + [BRIDGE_S] + resample([BRIDGE_S, BRIDGE_N, BRIDGE_X], 2.0)[1:] + chaikin([BRIDGE_X] + _dam0 + [point_at(bf_line, s_bfj)], 2)[1:], 2.0)
_kb = nearest_s(trail, BRIDGE_N)[1]; _ct = cum(trail)
_kbs = nearest_s(trail, BRIDGE_S)[1]
trail = [q if _kbs - 1.0 <= _ct[k] <= _kb + 3.0 else off_lake(q, EAST, TRAIL_LAKE_GAP) for k, q in enumerate(trail)]   # the tread kept off the water after the smoothing too (Round 49: the shore behind the pool as well)
_kk = [k for k, q in enumerate(trail) if _ct[k] > _kb + 1.0]
for _ in range(3):   # and eased again (the push leaves steps)
    trail = [trail[k] if k not in _kk or k + 1 >= len(trail) else ((trail[k - 1][0] + 2 * trail[k][0] + trail[k + 1][0]) / 4, (trail[k - 1][1] + 2 * trail[k][1] + trail[k + 1][1]) / 4) for k in range(len(trail))]
_h266 = pts(m["buildings"][266]["footprint"])
_h267 = pts(m["buildings"][267]["footprint"]); _deck = [(1332.0, 1095.4), (1356.2, 1095.4), (1356.2, 1124.6), (1332.0, 1124.6)]   # the pool deck's fence (MapBuilder.ChastainLakes.cs)
print(f"the trail (Round 49): its middle from bathhouse 267 {min(poly_dist(q, _h267) for q in trail):.1f} m, from the pool fence {min(poly_dist(q, _deck) for q in trail):.1f} m, "
      f"from the lake {min(ring_dist(q, EAST) for k, q in enumerate(trail) if _ct[k] < _kb - 1.0 and q[1] > 1092.0):.1f} m before the footbridge, from pool house 266 {min(poly_dist(q, _h266) for q in trail):.1f} m")
print(f"the trail: {cum(trail)[-1]:.0f} m; its middle from the lake {min(ring_dist(q, EAST) for k, q in enumerate(trail) if _ct[k] > _kb + 1.0):.1f} m at least past the footbridge")
SPEC = [("North Lakeside Drive", NL, way(NL), True), ("Lakeside Drive", LD, way(LD, a=(378.2, 1422.0)), True)]
pieces, line, offs = [], [], []
for name, osm, seg, two_way in SPEC:
    if line and math.dist(line[-1], seg[0]) > 0.6: raise SystemExit(f"the lap breaks before {name} ({osm}): gap {math.dist(line[-1], seg[0]):.1f} m")
    seg = resample(seg, 3.0); off = by_osm[osm]["width"] / 4.0 if two_way else 0.0
    pieces.append({"osm_id": osm, "name": name, "reverse": False, "length_m": round(cum(seg)[-1], 1)})
    line += seg if not line else seg[1:]; offs += [off] * (len(seg) if not offs else len(seg) - 1)
# the drive into the lot (OSM's drive 93 from the end of Lakeside Drive), then the trail
tr = resample([line[-1]] + trail, 2.0)
for q in tr[1:]: line.append(q); offs.append(0.0)
pieces.append({"osm_id": 0, "name": "the lake trail", "reverse": False, "length_m": round(cum(tr)[-1], 1)})
bfseg = resample(cut(bf_line, s_bfj, nearest_s(bf_line, line[0])[1]), 3.0)
for q in bfseg[1:]: line.append(q); offs.append(by_osm[BF]["width"] / 4.0)
pieces.append({"osm_id": BF, "name": "Bells Ferry Road", "reverse": False, "length_m": round(cum(bfseg)[-1], 1)})
if math.dist(line[0], line[-1]) > 0.6: raise SystemExit("the lap does not close: gap %.1f m" % math.dist(line[0], line[-1]))
line[-1] = line[0]; offs[-1] = offs[0]
N = len(line); sm = [sum(offs[max(0, i - 5):min(N, i + 6)]) / len(offs[max(0, i - 5):min(N, i + 6)]) for i in range(N)]
lap = []
for i in range(N):
    a, b = line[max(0, i - 1)], line[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
    lap.append((line[i][0] + t[1] / n * sm[i], line[i][1] - t[0] / n * sm[i]))
lap[-1] = lap[0]
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
print(f"lap {L_lap:.0f} m: " + ", ".join(f"{p['name']} {p['length_m']:.0f}" for p in pieces) + f"; lakes: West {WEST_Y:.2f} m, East {EAST_Y:.2f} m")
gate_s = round(S(GATE_AT), 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M) + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
cps = []
for i, (x, z, label) in enumerate(CHECKPOINTS, 1):
    s = round(S((x, z)), 1); p = point_at(lap, s, C); cps.append({"id": i, "s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
if [c["id"] for c in sorted(cps, key=lambda c: (c["s"] - gate_s) % L_lap)] != [c["id"] for c in sorted(cps, key=lambda c: c["id"])]:
    print("  (checkpoint order from the gate: " + ", ".join(f"CP{c['id']}" for c in sorted(cps, key=lambda c: (c["s"] - gate_s) % L_lap)) + ")")
# the checkpoints numbered in lap order from the gate
cps = sorted(cps, key=lambda c: (c["s"] - gate_s) % L_lap)
for i, c in enumerate(cps, 1): c["id"] = i
print(f"gate at s {gate_s}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps))
def forgiven(sa, sb): return [c["id"] for c in cps if (c["s"] - sa) % L_lap < (sb - sa) % L_lap]

# ---------------------------------------------------------------- W, the boat jump (a real jump: physics only, the landing sized to it)
dW = bdir(W_BEARING)
deck_yW, w_lip_y = WEST_Y + BOAT["deck_over"], WEST_Y + BOAT["lip_over"]
kW = math.tan(math.radians(BOAT["angle"])); slide_len = 2 * (w_lip_y - deck_yW) / kW
STERN = (W_LIP[0] - dW[0] * BOAT["length"], W_LIP[1] - dW[1] * BOAT["length"])
if not inside(STERN, WEST): raise SystemExit(f"the boat's stern {STERN} is not in the water")
_k = next(k for k in range(1, 200) if not inside((STERN[0] - dW[0] * k * 0.5, STERN[1] - dW[1] * k * 0.5), WEST))
DOCK_ROOT = (STERN[0] - dW[0] * (_k * 0.5 + 2.0), STERN[1] - dW[1] * (_k * 0.5 + 2.0))   # 2 m up the bank from the water's edge
SLIDE_FOOT = (W_LIP[0] - dW[0] * slide_len, W_LIP[1] - dW[1] * slide_len)
def flight(L, d, y0, v, ang, max_t=6.0):
    vx, vy = v * math.cos(math.radians(ang)), v * math.sin(math.radians(ang)); t = 0.0; top = y0
    while t < max_t:
        t += 0.01; s = vx * t; q = (L[0] + d[0] * s, L[1] + d[1] * s); y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        g = WEST_Y if inside(q, WEST) else ground(*q)
        if t > 0.15 and y <= g: return {"t": round(t, 2), "dist": round(s, 1), "at": (round(q[0], 1), round(q[1], 1)), "y": round(y, 2), "top": round(top - y0, 1), "wet": inside(q, WEST)}
    return None
wet_to = next(k * 0.5 for k in range(1, 400) if not inside((W_LIP[0] + dW[0] * k * 0.5, W_LIP[1] + dW[1] * k * 0.5), WEST))
print(f"W boat jump: dock {math.dist(DOCK_ROOT, STERN):.1f} m from ({DOCK_ROOT[0]:.1f}, {DOCK_ROOT[1]:.1f}); the boat {BOAT['length']} m, deck {deck_yW:.2f} m; the slide {slide_len:.1f} m "
      f"to its top at ({W_LIP[0]}, {W_LIP[1]}) {w_lip_y:.2f} m ({BOAT['lip_over']} m over the water, {BOAT['angle']} deg), bearing {W_BEARING}; "
      f"the water {wet_to:.1f} m on from the top (West Chastain Lake at {WEST_Y:.2f} m)")
w_flights = {}
for v in (24.0, 26.0, 28.0, 30.0, 32.0, 33.0, 34.0, 35.0, 36.0, 37.0, 38.0):
    f = flight(W_LIP, dW, w_lip_y, v, BOAT["angle"]); w_flights[v] = f
    near = {i: round(poly_dist(f["at"], ring), 1) for i, ring, h in B if near_box(i, f["at"], 8.0) and poly_dist(f["at"], ring) < 8.0}
    print(f"    off the slide at {v:.0f} m/s: {f['t']:.2f} s, {f['dist']:.0f} m, {f['top']:.1f} m up, down at {f['at']} {f['y']:.1f} m{' IN THE LAKE' if f['wet'] else ''}; houses within 8 m {near}")
# the line: the turn-off, the run-up, the dock, the boat and its slide, the flight (straight on), the landing at Lakeside Point, east on the lap
s_w0 = S((592.0, 1503.0))
turn = chaikin([point_at(lap, s_w0, C), (581.0, 1503.5), W_RUN[0], (577.6, 1490.0)], 3)
w_pre = resample(turn + [W_RUN[1], W_RUN[2], DOCK_ROOT], 2.0)
w_boat = resample([DOCK_ROOT, STERN, W_LIP], 2.0)[1:]
LAND = (W_LIP[0] + dW[0] * 92.0, W_LIP[1] + dW[1] * 92.0)   # Lakeside Point's mouth (a lip speed of about 34 m/s)
s_land = S(LAND); s_w1 = s_land + W_REJOIN_PAST
w_fl = [(W_LIP[0] + dW[0] * k, W_LIP[1] + dW[1] * k) for k in range(2, 92, 2)]
w_post = resample(chaikin([LAND, (561.6, 1255.0), (568.0, 1254.0), point_at(lap, s_land + 14.0, C), point_at(lap, s_w1, C)], 3), 2.0)
wpath = w_pre + w_boat + w_fl + w_post
def slide_y(q):
    u = math.dist(SLIDE_FOOT, q) if math.dist(STERN, q) > math.dist(STERN, SLIDE_FOOT) else 0.0
    return deck_yW + kW * u * u / (2 * slide_len)
_vx, _vy = 34.0 * math.cos(math.radians(BOAT["angle"])), 34.0 * math.sin(math.radians(BOAT["angle"]))
wpath_y = [round(ground(*q), 2) for q in w_pre] + [round(min(ground(*q), deck_yW) if not inside(q, WEST) else slide_y(q), 2) for q in w_boat] + \
          [round(w_lip_y + _vy * (math.dist(W_LIP, q) / _vx) - 4.905 * (math.dist(W_LIP, q) / _vx) ** 2, 2) for q in w_fl] + [round(ground(*q), 2) for q in w_post]
gap_houses = {i: round(min(poly_dist(q, ring) for q in w_pre), 1) for i, ring, h in B if any(near_box(i, q, 8.0) for q in w_pre) and min(poly_dist(q, ring) for q in w_pre) < 8.0}
fg_w = forgiven(s_w0, s_w1)
print(f"W: leaves the lap at s {s_w0:.0f}, rejoins at s {s_w1:.0f}: {cum(wpath)[-1]:.0f} m against {(s_w1 - s_w0) % L_lap:.0f} m on the lap; forgives CP {fg_w}; "
      f"houses by the run-up (m): {gap_houses}")
sWDock, sWLip, sWLand = nearest_s(wpath, DOCK_ROOT)[1], nearest_s(wpath, W_LIP)[1], nearest_s(wpath, LAND)[1]

# ---------------------------------------------------------------- E, the lake drain
pd = (PIER_DIR[0] / math.hypot(*PIER_DIR), PIER_DIR[1] / math.hypot(*PIER_DIR))
def along_pier(k): return (PIER_ROOT[0] + pd[0] * k, PIER_ROOT[1] + pd[1] * k)
DRAIN = along_pier(DRAIN_AT); G_END = along_pier(PIER_LEN + 2 * GAZEBO_HALF)
ld = next(c for c in NET["chambers"] if c["id"] == "LD")
if math.dist((ld["x"], ld["z"]), DRAIN) > 0.5: raise SystemExit(f"LD ({ld['x']}, {ld['z']}) is not under the drain ({DRAIN[0]:.1f}, {DRAIN[1]:.1f}): pipe_network.py")
b28 = next(t for t in NET["tubes"] if t["id"] == "B28"); tp = b28["pts"]; tube3 = [(tp[i], tp[i + 1], tp[i + 2]) for i in range(0, len(tp), 3)]
mh = next(h for h in NET["manholes"] if h["id"] == "MH14")
deck_y = max(ground(*PIER_ROOT) + 0.03, EAST_Y + 0.45)   # MapBuilder.ChastainLakes.cs LakesPier
s_e0 = S(E_FROM)
mh_s = S((mh["x"], mh["z"])); s_e1 = mh_s + E_REJOIN_PAST
_e1 = along_pier(-6.0); _lf = lambda phi: (-math.cos(math.radians(phi)), math.sin(math.radians(phi)))   # the left of heading phi
_h1 = math.degrees(math.atan2(pd[0], pd[1])) % 360; _cc = (_e1[0] + _lf(_h1)[0] * E_TURN_R, _e1[1] + _lf(_h1)[1] * E_TURN_R)
_p0 = point_at(lap, s_e0, C); _h0 = math.degrees(math.atan2(*[b - a for a, b in zip(_p0, point_at(lap, s_e0 + 2.0, C))])) % 360
_turn = (_h0 - _h1) % 360   # degrees to the left
_arc = [(_cc[0] - _lf(_h0 - _turn * k / 24)[0] * E_TURN_R, _cc[1] - _lf(_h0 - _turn * k / 24)[1] * E_TURN_R) for k in range(25)]
print(f"E: off the lap at ({_p0[0]:.1f}, {_p0[1]:.1f}) heading {_h0:.0f}, the turn onto the pier's line {_turn:.0f} deg left at {E_TURN_R:.0f} m, starting {math.dist(_p0, _arc[0]):.1f} m from the lap")
e_pre = resample([_p0] + _arc + [PIER_ROOT], 2.0)
e_pier = [along_pier(k) for k in range(2, int(PIER_LEN + 2 * GAZEBO_HALF) + 1, 2)]
e_drop = [along_pier(k) for k in range(int(PIER_LEN + 2 * GAZEBO_HALF) + 2, int(DRAIN_AT), 2)] + [DRAIN]
# then the shaft (straight down: one point at the room's floor), the room to the tube's mouth, the tube, the riser to the street
room_c = (ld["x"], ld["z"])
e_tube = [(q[0], q[2]) for q in tube3[::2]] + [(tube3[-1][0], tube3[-1][2])]
e_tube_y = [q[1] for q in tube3[::2]] + [tube3[-1][1]]
mdir = mh["dir"]; street = (mh["x"], mh["z"])
e_out = resample([street, point_at(lap, s_e1, C)], 2.0)[1:]
epath = e_pre + e_pier + e_drop + e_tube + e_out   # the shaft straight down from the drain (LD's middle is under it): no point of its own
epath_y = [round(ground(*q), 2) for q in e_pre] + [round(deck_y, 2)] * len(e_pier) + [round(EAST_Y, 2)] * len(e_drop) + \
          [round(v, 2) for v in e_tube_y] + [round(ground(*q), 2) for q in e_out]
fg_e = forgiven(s_e0, s_e1)
sEPier, sEEnd, sEDrain = nearest_s(epath, PIER_ROOT)[1], nearest_s(epath, G_END)[1], nearest_s(epath, DRAIN)[1]
tube_len = sum(math.dist(tube3[i], tube3[i + 1]) for i in range(len(tube3) - 1))
print(f"E lake drain: deck {deck_y:.2f} m ({deck_y - EAST_Y:.2f} m over East Chastain Lake at {EAST_Y:.2f}); the drain at ({DRAIN[0]:.1f}, {DRAIN[1]:.1f}) "
      f"{DRAIN_AT - PIER_LEN - 2 * GAZEBO_HALF:.1f} m past the gazebo's end, its rim {DRAIN_R} m; LD's floor {ld['floor']} m ({EAST_Y - ld['floor']:.1f} m down); B28 {tube_len:.0f} m to MH14 at "
      f"({mh['x']:.0f}, {mh['z']:.0f}) {mh['y']:.1f} m, its riser heading {math.degrees(math.atan2(mdir[0], mdir[1])) % 360:.0f} deg")
print(f"E: leaves the lap at s {s_e0:.0f}, rejoins at s {s_e1:.0f}: {cum(epath)[-1]:.0f} m against {(s_e1 - s_e0) % L_lap:.0f} m on the lap; forgives CP {fg_e}")
# what a vehicle off the gazebo's end does: level off the deck at v, down to the water; the bell's catch ring (DRAIN_R) from the end
for v in (8.0, 12.0, 16.0, 20.0, 24.0, 28.0):
    t = math.sqrt(2 * (deck_y + 0.6 - EAST_Y) / 9.81); print(f"    off the gazebo at {v:.0f} m/s: down to the water {v * t:.1f} m out (the catch from {DRAIN_AT - PIER_LEN - 2 * GAZEBO_HALF - DRAIN_R:.1f} to {DRAIN_AT - PIER_LEN - 2 * GAZEBO_HALF + DRAIN_R:.1f} m)")

shortcuts = [
    {"id": "w_lake_jump", "name": "Boat jump", "direction": "forward", "path": flat(wpath), "path_y": wpath_y, "half_width_m": 3.5,
     # ai_speed caps the AI's whole run (RoadDriver.AiShortcut.SpeedAt): flat out here, the profile slows it for the turn and after the landing
     "entry_inset_m": 14.0, "entry_margin_m": 1.5, "forgives": fg_w, "ai_speed": W_DESIGN,
     # the turn off the road, then flat out down the run-up, the dock and up the slide; easy after the landing (a hard left onto Lakeside Drive)
     "ai_profile": [round(v, 1) for v in (0.0, 16.0, 26.0, 18.0, 46.0, W_DESIGN, sWLip, W_DESIGN, sWLand + 2.0, 14.0, cum(wpath)[-1] - 8.0, 16.0)],
     # a sloppy try (Dan's rule: the AI is not perfect): eased off down the run-up, too slow up the slide, into the lake
     "ai_slip": 0.2, "ai_slip_profile": [round(v, 1) for v in (0.0, 16.0, 26.0, 18.0, 46.0, 24.0, sWDock, W_SLIP, sWLip, W_SLIP, sWLand + 2.0, 14.0, cum(wpath)[-1] - 8.0, 16.0)]},
    {"id": "e_lake_drain", "name": "Lake drain", "direction": "forward", "path": flat(epath), "path_y": epath_y, "half_width_m": 3.0,
     "entry_inset_m": round(sEPier + 4.0, 1), "entry_margin_m": 1.0, "forgives": fg_e, "ai_speed": 18.0,
     # across the lawn and onto the pier lined up, along it and off the end at the pier's speed, flat out along the tube and up the riser
     "ai_profile": [round(v, 1) for v in (0.0, 18.0, sEPier - 14.0, 14.0, sEPier, 15.0, sEEnd, E_DESIGN, sEDrain + 6.0, 24.0, cum(epath)[-1] - 10.0, 24.0)],
     # a sloppy try: in too hot onto the pier (a rail, or off the end crooked into the lake)
     "ai_slip": 0.18, "ai_slip_profile": [round(v, 1) for v in (0.0, 22.0, sEPier - 14.0, 20.0, sEPier, 21.0, sEEnd, 26.0, sEDrain + 6.0, 24.0, cum(epath)[-1] - 10.0, 24.0)]},
]

# ---------------------------------------------------------------- closures (as the Chastain Ridge race's), AI zones
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
CLEAR_OVERRIDE = {9134890: 45.0}   # Round 46: Lakeside Point's barrier 45 m down it (the boat jump's long landings run out on it)
def closures_for(lap, sc_paths):
    def course_dist(q): return min(seg_dist(q, a, b) for a, b in zip(lap[:-1], lap[1:]))
    def follow(ri, k, step, arc0, depth, seen):
        p = roads_pts[ri]; last = None
        for d, q, tan in walk(p, k, step):
            last = (arc0 + d, q, tan)
            if course_dist(q) >= CLEAR_OVERRIDE.get(roads[ri]["osm_id"], SETTINGS["closure_clear_m"]): return (arc0 + d, q, tan, ri)
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
closures, stubs, merged = closures_for(lap, (w_pre + w_post, e_pre + e_out))
print(f"closures (barriers while racing): {len(closures)}; dead-end stubs not barred: {len(stubs)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
# the AI along the trail: held to the trail's pace through the lot, the S onto the footbridge and the turn onto Bells Ferry Road
s_lot, s_br, s_bf = S((1322.0, 1084.0)), S(BRIDGE_S), S(BF_JOIN)
ai_zones = [{"id": "lot", "s0": round(s_lot - 25.0, 1), "s1": round(s_lot + 10.0, 1), "speed": 14.0},
            {"id": "behind the pool", "s0": round(s_lot + 10.0, 1), "s1": round(s_br - 6.0, 1), "speed": 10.0},   # Round 49 (C): round the bathhouse and the deck's corner
            {"id": "footbridge", "s0": round(s_br - 6.0, 1), "s1": round(s_br + 26.0, 1), "speed": 12.0},
            {"id": "the dam", "s0": round(s_br + 26.0, 1), "s1": round(s_bf - 12.0, 1), "speed": 20.0},
            {"id": "onto Bells Ferry Road", "s0": round(s_bf - 12.0, 1), "s1": round(s_bf + 6.0, 1), "speed": 12.0}]

# ---------------------------------------------------------------- the world pieces (Data/tracks/chastain_lakes.json)
# ---------------------------------------------------------------- Round 47 (A, Dan 2026-10-19 01:43): the reverse lap, no shortcuts
# "Can we build the Reverse version of this track ... I don't expect the current short cuts to work and I want to decide on the shortcuts after I
# drive it in reverse." The same roads and the lake trail driven clockwise, a lap of its own as the reverse Chastain Ridge (RaceRoute: loop_rev and
# the *_rev fields): on two-way roads the other lane (the forward lap's centerline reversed, set off to its right), the trail north to south.
# Both shortcuts are closed in a reverse race: a barrier across the boat jump's way between houses 146 and 147 and one across the pier's foot (the
# lake drain's grate is shut and the PARTY sign and the worn tracks hidden: MapBuilder.LakesBoat.cs / LakesRace.cs, RaceOnly, LakeDrain).
line_r, sm_r = line[::-1], sm[::-1]
lap_r = []
for i in range(N):
    a, b = line_r[max(0, i - 1)], line_r[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
    lap_r.append((line_r[i][0] + t[1] / n * sm_r[i], line_r[i][1] - t[0] / n * sm_r[i]))
lap_r[-1] = lap_r[0]
Cr = cum(lap_r); L_rev = Cr[-1]
def SR(q): return nearest_s(lap_r, q)[1]
gate_s_rev = round(SR(GATE_AT), 1)
cps_rev = []
for x, z, label in CHECKPOINTS:
    s_ = round(SR((x, z)), 1); p_ = point_at(lap_r, s_, Cr); cps_rev.append({"s": s_, "pos": [round(p_[0], 1), round(p_[1], 1)], "label": label})
cps_rev = sorted(cps_rev, key=lambda c: (c["s"] - gate_s_rev) % L_rev)
for i, c in enumerate(cps_rev, 1): c["id"] = i
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps_rev))
# ---------------------------------------------------------------- Round 49 (Dan, 2026-10-19 08:37): the reverse race's two shortcuts
def forgiven_rev(sa, sb): return [c["id"] for c in cps_rev if (c["s"] - sa) % L_rev < (sb - sa) % L_rev]
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def along(o, d, k): return (o[0] + d[0] * k, o[1] + d[1] * k)
# A, THE INTAKE ("Lets do a similar idea as the gazebo shortcut on the forward race. On the bit of land that sticks out before the CP, jump into an
# intake with into a tunnel across the lake and you come out of a manhole just after the 9 cp in the correct direction"): westbound on Lakeside
# Drive, 45 m before CP7, right onto the lake association's service track up the point east of Lakeside Point (a sign at its mouth, worn ruts), along
# the intake's concrete jetty off the point's tip (6 m wide, 2 m over the water: off its side you are in the lake) and off its end into the intake,
# a square concrete riser in the lake 11.5 m out (MapBuilder.LakesIntake.cs). LakeDrain takes a vehicle that comes in over its weir down its shaft
# to LI and on into B29 (pipe_network.py), glass under West Chastain Lake, under M2b by the north shore, up MH15's riser onto North Lakeside Drive
# 30 m past CP9, facing east. Too slow off the jetty (under about 11 m/s) and you come down short in the lake; off line and you miss the weir.
I_AT, I_EDGE = (581.6, 1318.0), (582.6, 1306.5)   # LI in pipe_network.py stands under I_AT
I_PAD_LEN, I_PAD_HALF, I_PAD_OVER = 11.0, 3.0, 2.0
I_R, I_SHAFT, I_WALL = 4.0, 3.6, 4.6                # the catch (the weir's inside), the shaft, the riser's outside (half widths)
I_WALL_UP = 1.2                                     # the riser's walls over the water (Round 49 check: at 0.35 m a car short in the lake waded up and climbed in)
I_FROM = (606.0, 1262.0)
I_REJOIN_PAST = 45.0
I_SIGN = (612.0, 1269.6, 90.0)                      # on Lakeside Drive's north verge at the track's mouth, facing the westbound lap
dI = unit((I_AT[0] - I_EDGE[0], I_AT[1] - I_EDGE[1])); I_ROOT = along(I_EDGE, dI, -I_PAD_LEN); pad_y = WEST_Y + I_PAD_OVER
li = next(c for c in NET["chambers"] if c["id"] == "LI")
if math.dist((li["x"], li["z"]), I_AT) > 0.5: raise SystemExit(f"LI ({li['x']}, {li['z']}) is not under the intake {I_AT}: pipe_network.py")
b29 = next(t for t in NET["tubes"] if t["id"] == "B29"); tp9 = b29["pts"]; tube9 = [(tp9[i], tp9[i + 1], tp9[i + 2]) for i in range(0, len(tp9), 3)]
mh15 = next(h for h in NET["manholes"] if h["id"] == "MH15")
s_i0 = SR(I_FROM); s_i1 = SR((mh15["x"], mh15["z"])) + I_REJOIN_PAST
i_turn = chaikin([point_at(lap_r, s_i0, Cr), (603.0, 1268.5), (595.0, 1279.0), along(I_ROOT, dI, -5.0), I_ROOT], 3)
i_pre = resample(i_turn, 2.0)
i_pad = [along(I_ROOT, dI, k) for k in range(2, int(I_PAD_LEN), 2)] + [I_EDGE]
i_drop = [along(I_EDGE, dI, k) for k in range(2, 11, 2)] + [I_AT]
i_tube = [(q[0], q[2]) for q in tube9[::2]] + [(tube9[-1][0], tube9[-1][2])]; i_tube_y = [q[1] for q in tube9[::2]] + [tube9[-1][1]]
i_out = resample([(mh15["x"], mh15["z"]), point_at(lap_r, s_i1, Cr)], 2.0)[1:]
ipath = i_pre + i_pad + i_drop + i_tube + i_out
ipath_y = [round(ground(*q), 2) for q in i_pre] + [round(pad_y, 2)] * len(i_pad) + [round(WEST_Y, 2)] * len(i_drop) + [round(v, 2) for v in i_tube_y] + [round(ground(*q), 2) for q in i_out]
fg_i = forgiven_rev(s_i0, s_i1)
sIRoot, sIEdge, sIDrop = nearest_s(ipath, I_ROOT)[1], nearest_s(ipath, I_EDGE)[1], nearest_s(ipath, I_AT)[1]
_tl9 = sum(math.dist(tube9[i], tube9[i + 1]) for i in range(len(tube9) - 1))
print(f"A intake: the jetty {I_PAD_LEN:.0f} m x {2 * I_PAD_HALF:.0f} m from ({I_ROOT[0]:.1f}, {I_ROOT[1]:.1f}) at {pad_y:.2f} m ({I_PAD_OVER} m over West Chastain Lake); the intake at {I_AT} "
      f"{math.dist(I_EDGE, I_AT):.1f} m off its end, its catch {I_R} m; LI's floor {li['floor']} m; B29 {_tl9:.0f} m to MH15 at ({mh15['x']:.0f}, {mh15['z']:.0f}) {mh15['y']:.1f} m, "
      f"its riser heading {math.degrees(math.atan2(*mh15['dir'])) % 360:.0f} deg")
for v in (8.0, 10.0, 12.0, 16.0, 20.0, 25.0):
    t_ = math.sqrt(2 * I_PAD_OVER / 9.81); print(f"    off the jetty at {v:.0f} m/s: down to the water {v * t_:.1f} m out (the catch from {math.dist(I_EDGE, I_AT) - I_R:.1f} to {math.dist(I_EDGE, I_AT) + I_R:.1f} m)")
print(f"A: leaves the reverse lap at s {s_i0:.0f}, rejoins at s {s_i1:.0f}: {cum(ipath)[-1]:.0f} m against {(s_i1 - s_i0) % L_rev:.0f} m on the lap; forgives CP {fg_i}")

# B, THE KICKER ("around CP 4, you go between houses and find a ramp that jumps past 5. It should be a fairly large jump, I would prefer something other
# than a dirt ramp"): southbound past CP4, right through the front yard of 239 and between 239 and 237, down the back yard to the neighborhood kids'
# big wooden BMX kicker (plywood on a 2x6 frame, 3 m at its lip, 32 degrees, built out over the slope: MapBuilder.LakesKicker.cs), a real jump
# (physics only) west over the creek hollow below the lake's south tip, between houses 232 and 246, onto the end of Northlake Point and on through the
# yards to the lap past CP5. Come in slow and you drop into the hollow (the creek bottom 4 to 8 m down; out along it to the lap's bend, a slow way).
# Physics cannot reach the lap itself past CP5 from here (165 m at least): the flight is the hollow (about 55 m), the rest is driven.
# Round 49 check, tuning try 1: the first kicker (lip 9.6 m further up this line, 3 m, 32 deg, a 20 m turn off the lap) gave 17.8 / 20.1 m/s at its lip
# flat out and both came down in the hollow; now one 28 m arc off the lap (it starts just past CP4) onto the line through the gap, 15 m of straight
# down the yard, the kicker 3.5 m and 35 deg: about 20 m/s clears the hollow, 18 m/s comes down on its far side, slower in it
# tuning try 2: try 1 cleared at 22 m/s flat out but a run at 18 m/s cleared too (too little risk); the kicker 3 m back up its line (a longer gap)
K_X, K_BEARING, K_BACK, K_H, K_ANGLE, K_W, K_TURN_R = (1270.5, 1011.8), 256.0, 33.9, 3.5, 35.0, 4.6, 28.0   # K_X: where the kicker's line meets the lap
dK = bdir(K_BEARING); kk = math.tan(math.radians(K_ANGLE)); k_len = 2 * K_H / kk
K_LIP = (K_X[0] + dK[0] * (K_BACK + k_len), K_X[1] + dK[1] * (K_BACK + k_len))
K_REJOIN = (1066.0, 971.0)
K_SIGN = (1263.6, 1028.5, 0.0)                     # a hand-painted JUMP! sign on the lap's west verge at the turn-off, facing the southbound lap
K_FOOT = along(K_LIP, dK, -k_len)
k_foot_y = ground(*K_FOOT); k_lip_y = k_foot_y + K_H
def k_flight(v, ang=K_ANGLE, max_t=6.0):
    vx, vy = v * math.cos(math.radians(ang)), v * math.sin(math.radians(ang)); t = 0.0; top = k_lip_y
    while t < max_t:
        t += 0.01; s = vx * t; q = along(K_LIP, dK, s); y = k_lip_y + vy * t - 4.905 * t * t; top = max(top, y)
        g = EAST_Y if inside(q, EAST) else ground(*q)
        if t > 0.15 and y <= g: return {"t": round(t, 2), "dist": round(s, 1), "at": (round(q[0], 1), round(q[1], 1)), "y": round(y, 2), "g": round(g, 2), "top": round(top - k_lip_y, 1)}
    return None
print(f"B kicker: foot ({K_FOOT[0]:.1f}, {K_FOOT[1]:.1f}) {k_foot_y:.2f} m, lip {K_LIP} {k_lip_y:.2f} m ({k_lip_y - ground(*K_LIP):.1f} m over the ground under it), {k_len:.1f} m long, "
      f"{K_ANGLE:.0f} deg at the lip, bearing {K_BEARING:.0f}")
k_flights = {}
for v in (14.0, 16.0, 17.0, 18.0, 19.0, 20.0, 22.0, 24.0, 26.0, 28.0):
    f = k_flight(v); k_flights[v] = f
    near = {i: round(poly_dist(f["at"], ring), 1) for i, ring, h in B if near_box(i, f["at"], 10.0) and poly_dist(f["at"], ring) < 10.0}
    print(f"    off the kicker at {v:.0f} m/s: {f['t']:.2f} s, {f['dist']:.0f} m, {f['top']:.1f} m up, down at {f['at']} on {f['g']:.1f} m{' IN THE HOLLOW' if f['g'] < 299.0 else ''}; houses within 10 m {near}")
K_LAND = along(K_LIP, dK, 50.0)
# the arc: tangent to the lap (heading south) and to the kicker's line; it meets the line at the gap between 239 and 237
_kt = K_TURN_R * math.tan(math.radians((K_BEARING - 180.0) / 2.0)); _kc = (K_X[0] - K_TURN_R, K_X[1] + _kt)
_arc = [(_kc[0] + K_TURN_R * math.cos(math.radians(-a)), _kc[1] + K_TURN_R * math.sin(math.radians(-a))) for a in np.linspace(0.0, K_BEARING - 180.0, 20)]
s_k0 = SR(_arc[0])
k_pre = resample([point_at(lap_r, s_k0, Cr)] + _arc[1:] + [K_FOOT], 2.0)
k_ramp = [along(K_FOOT, dK, k) for k in range(2, int(k_len) + 1, 2)] + [K_LIP]
k_fl = [along(K_LIP, dK, k) for k in range(2, 50, 2)]
s_k1 = SR(K_REJOIN) + 6.0
k_post = resample(chaikin([K_LAND, (1160.0, 985.0), (1150.0, 983.5), (1131.0, 980.5), (1110.0, 976.5), (1090.0, 970.0), point_at(lap_r, s_k1 - 10.0, Cr), point_at(lap_r, s_k1, Cr)], 3), 2.0)
kpath = k_pre + k_ramp + k_fl + k_post
_vx, _vy = 21.0 * math.cos(math.radians(K_ANGLE)), 25.0 * math.sin(math.radians(K_ANGLE))
kpath_y = [round(ground(*q), 2) for q in k_pre] + [round(k_foot_y + kk * math.dist(K_FOOT, q) ** 2 / (2 * k_len), 2) for q in k_ramp] + \
          [round(k_lip_y + _vy * (math.dist(K_LIP, q) / _vx) - 4.905 * (math.dist(K_LIP, q) / _vx) ** 2, 2) for q in k_fl] + [round(ground(*q), 2) for q in k_post]
fg_k = forgiven_rev(s_k0 + 30.0, s_k1)   # from its entry plane (30 m in): the arc leaves the lap just past CP4
sKFoot, sKLip, sKLand = nearest_s(kpath, K_FOOT)[1], nearest_s(kpath, K_LIP)[1], nearest_s(kpath, K_LAND)[1]
k_houses = {i: round(min(poly_dist(q, ring) for q in k_pre + k_post), 1) for i, ring, h in B if any(near_box(i, q, 8.0) for q in k_pre + k_post) and min(poly_dist(q, ring) for q in k_pre + k_post) < 8.0}
print(f"B: leaves the reverse lap at s {s_k0:.0f}, rejoins at s {s_k1:.0f}: {cum(kpath)[-1]:.0f} m against {(s_k1 - s_k0) % L_rev:.0f} m on the lap; forgives CP {fg_k}; "
      f"houses by its ways (m): {k_houses}; the flight's line between 232 ({min(poly_dist(along(K_LIP, dK, k), pts(m['buildings'][232]['footprint'])) for k in range(40, 70)):.1f} m) "
      f"and 246 ({min(poly_dist(along(K_LIP, dK, k), pts(m['buildings'][246]['footprint'])) for k in range(40, 70)):.1f} m)")
rev_shortcuts = [
    {"id": "r_lake_intake", "name": "Lake intake", "direction": "reverse", "path": flat(ipath), "path_y": ipath_y, "half_width_m": 3.0,
     # the AI off the jetty's end at 23 m/s: at 18 it hit the riser's 1.2 m wall every time (the check's driver clears it at 21.5 to 24)
     "entry_inset_m": round(sIRoot - 2.0, 1), "entry_margin_m": 1.0, "forgives": fg_i, "ai_speed": 24.0,
     # up the track and along the jetty lined up, off its end at the jetty's speed, flat out along the tube and up the riser
     "ai_profile": [round(v, 1) for v in (0.0, 15.0, sIRoot - 14.0, 16.0, sIRoot, 21.0, sIEdge, 23.0, sIDrop + 6.0, 22.0, cum(ipath)[-1] - 10.0, 22.0)],
     # a sloppy try: off the end too slow (short, into the lake)
     "ai_slip": 0.2, "ai_slip_profile": [round(v, 1) for v in (0.0, 15.0, sIRoot - 10.0, 12.0, sIRoot, 9.0, sIEdge, 8.0, sIDrop + 6.0, 22.0, cum(ipath)[-1] - 10.0, 22.0)]},
    {"id": "r_kicker_jump", "name": "Kicker jump", "direction": "reverse", "path": flat(kpath), "path_y": kpath_y, "half_width_m": 3.5,
     # its entry plane 30 m in, 14 m off the lap (at 14 m in the arc was still beside the road: every car on the lap crossed it)
     "entry_inset_m": 30.0, "entry_margin_m": 1.5, "forgives": fg_k, "ai_speed": 34.0,
     # flat out through the yard and up the kicker; easy after the landing (the yards to the lap)
     "ai_profile": [round(v, 1) for v in (0.0, 22.0, 16.0, 24.0, sKFoot - 8.0, 34.0, sKLip, 34.0, sKLand + 4.0, 15.0, cum(kpath)[-1] - 8.0, 18.0)],
     # a sloppy try: eased off into the kicker, short, into the hollow
     "ai_slip": 0.2, "ai_slip_profile": [round(v, 1) for v in (0.0, 22.0, 16.0, 20.0, sKFoot - 8.0, 17.0, sKLip, 17.0, sKLand + 4.0, 15.0, cum(kpath)[-1] - 8.0, 18.0)]},
]
shortcuts += rev_shortcuts
# both closed in a forward race (as the forward ones are in reverse): a barrier across the track 12 m off Lakeside Drive and one across the gap between
# 239 and 237; the clues hidden and the intake's grate on (RaceOnly.hideInForwardOf / showInForwardOf, LakeDrain.shutForwardOf)
def shut_fwd(label, at, d, width):
    n = math.hypot(*d); sj = S(at)
    return {"road": label, "osm_id": 0, "x": round(at[0], 2), "z": round(at[1], 2), "dx": round(d[0] / n, 4), "dz": round(d[1] / n, 4), "width_m": round(width, 1),
            "junction_s": round(sj, 1), "contact": [round(at[0], 1), round(at[1], 1)], "arc_m": 0.0, "barrier_on": label, "label": f"{label} (shut forward)"}
_ki = min(range(len(i_pre)), key=lambda k: abs(math.dist(i_pre[k], point_at(lap_r, s_i0, Cr)) - 12.0))
_kg2 = min(range(len(k_pre)), key=lambda k: poly_dist(k_pre[k], pts(m["buildings"][239]["footprint"])) + poly_dist(k_pre[k], pts(m["buildings"][237]["footprint"])))
_gap2 = poly_dist(k_pre[_kg2], pts(m["buildings"][239]["footprint"])) + poly_dist(k_pre[_kg2], pts(m["buildings"][237]["footprint"]))
def _dir(p, k): return (p[min(k + 1, len(p) - 1)][0] - p[max(k - 1, 0)][0], p[min(k + 1, len(p) - 1)][1] - p[max(k - 1, 0)][1])
fwd_shut = [shut_fwd("the intake's track", i_pre[_ki], _dir(i_pre, _ki), 8.0), shut_fwd("the kicker's way", k_pre[_kg2], _dir(k_pre, _kg2), _gap2)]
print(f"forward race: the intake's track shut at ({i_pre[_ki][0]:.0f}, {i_pre[_ki][1]:.0f}), the kicker's way between 239 and 237 ({_gap2:.1f} m) at ({k_pre[_kg2][0]:.0f}, {k_pre[_kg2][1]:.0f})")
closures_rev, stubs_rev, merged_rev = closures_for(lap_r, (i_pre + i_out, k_pre + k_post))
for c in fwd_shut: c["id"] = f"B{len(closures) + 1}"; closures.append(c)   # Round 49: the reverse shortcuts shut in a forward race
# a stub the reverse lane passes too near to bar (North Lakeside Drive's 12 m spur at x 645: the lane 4 m nearer its end) keeps the forward barrier
for st in list(stubs_rev):
    fw = [dict(c) for c in closures if c["osm_id"] == st["osm_id"]]
    for c in fw: c["junction_s"] = round(SR(tuple(c["contact"])), 1); c["label"] = f"{c['road']} at lap {c['junction_s']:.0f} m"
    if fw: closures_rev += fw; stubs_rev.remove(st)
closures_rev.sort(key=lambda c: c["junction_s"])
for i, c in enumerate(closures_rev, 1): c["id"] = f"B{i}"
def shut(cid, label, at, along, width):
    n = math.hypot(*along); sj = SR(at)
    return {"road": label, "osm_id": 0, "x": round(at[0], 2), "z": round(at[1], 2), "dx": round(along[0] / n, 4), "dz": round(along[1] / n, 4), "width_m": round(width, 1),
            "junction_s": round(sj, 1), "contact": [round(at[0], 1), round(at[1], 1)], "arc_m": 0.0, "barrier_on": label, "id": cid, "label": f"{label} (shut in reverse)"}
# the boat jump: across its way where it passes between houses 146 and 147 (the gap's width, wall to wall)
_kg = min(range(len(w_pre)), key=lambda k: poly_dist(w_pre[k], pts(m["buildings"][146]["footprint"])) + poly_dist(w_pre[k], pts(m["buildings"][147]["footprint"])))
_qg = w_pre[_kg]; _gap = poly_dist(_qg, pts(m["buildings"][146]["footprint"])) + poly_dist(_qg, pts(m["buildings"][147]["footprint"]))
_ga = (w_pre[min(_kg + 1, len(w_pre) - 1)][0] - w_pre[max(_kg - 1, 0)][0], w_pre[min(_kg + 1, len(w_pre) - 1)][1] - w_pre[max(_kg - 1, 0)][1])
# the lake drain: across the pier's foot, on the lawn 1.5 m short of its root (the deck stands proud of the ground further out)
_pf = along_pier(-1.5)
closures_rev += [shut(f"B{len(closures_rev) + 1}", "the boat jump's way", _qg, _ga, _gap), shut(f"B{len(closures_rev) + 2}", "the pier", _pf, pd, 2 * PIER_HW)]
print(f"reverse closures: {len(closures_rev)} (the boat jump's way shut between 146 and 147, a {_gap:.1f} m gap, at ({_qg[0]:.0f}, {_qg[1]:.0f}); the pier's foot at ({_pf[0]:.0f}, {_pf[1]:.0f})); "
      f"dead-end stubs not barred: {len(stubs_rev)}")
s_lot_r, s_br_r, s_bf_r = SR((1322.0, 1084.0)), SR(BRIDGE_S), SR(BF_JOIN)
s_brn_r = SR(BRIDGE_N)
ai_zones_rev = [{"id": "off Bells Ferry Road", "s0": round(s_bf_r - 12.0, 1), "s1": round(s_bf_r + 8.0, 1), "speed": 12.0},
                {"id": "the dam", "s0": round(s_bf_r + 8.0, 1), "s1": round(s_brn_r - 25.0, 1), "speed": 20.0},
                {"id": "footbridge", "s0": round(s_brn_r - 25.0, 1), "s1": round(s_br_r + 6.0, 1), "speed": 12.0},
                {"id": "behind the pool", "s0": round(s_br_r + 6.0, 1), "s1": round(s_lot_r - 10.0, 1), "speed": 10.0},   # Round 49 (C)
                {"id": "lot", "s0": round(s_lot_r - 10.0, 1), "s1": round(s_lot_r + 25.0, 1), "speed": 14.0}]
ai_zones = [z for z in ai_zones if z["s1"] > z["s0"]]; ai_zones_rev = [z for z in ai_zones_rev if z["s1"] > z["s0"]]

def zone(L, d, a, b, half_a, half_b):
    n = (-d[1], d[0])
    return [round(v, 2) for q in [(L[0] + d[0] * a + n[0] * half_a, L[1] + d[1] * a + n[1] * half_a), (L[0] + d[0] * b + n[0] * half_b, L[1] + d[1] * b + n[1] * half_b),
                                   (L[0] + d[0] * b - n[0] * half_b, L[1] + d[1] * b - n[1] * half_b), (L[0] + d[0] * a - n[0] * half_a, L[1] + d[1] * a - n[1] * half_a)] for v in q]
land_from = wet_to - 2.0
land_to = w_flights[38.0]["dist"] + 8.0
landings = [{"id": "WJ", "poly": zone(W_LIP, dW, land_from, land_to, 9.0, 12.0)}]
landings.append({"id": "RK", "poly": zone(K_LIP, dK, 30.0, 85.0, 7.0, 9.0)})   # Round 49 (B): the kicker's landing past the hollow
clear = [{"id": "W the run-up to the dock", "points": flat(w_pre[::2] + [w_pre[-1]]), "half": 5.0},
         {"id": "W the dock and the boat", "points": flat([DOCK_ROOT, W_LIP]), "half": 6.0},
         {"id": "W the landing", "points": flat([(W_LIP[0] + dW[0] * land_from, W_LIP[1] + dW[1] * land_from), (W_LIP[0] + dW[0] * land_to, W_LIP[1] + dW[1] * land_to)]), "half": 11.0},
         {"id": "W the way on to Lakeside Drive", "points": flat(w_post[::2] + [w_post[-1]]), "half": 4.5},
         {"id": "E the lawn to the pier", "points": flat(e_pre[::2] + [e_pre[-1]]), "half": 4.5},
         {"id": "the lake trail", "points": flat(trail[::2] + [trail[-1]]), "half": TRAIL_W / 2.0 + 1.5},
         # Round 49: the reverse shortcuts' ways (A the service track and the jetty; B the yard, the kicker, the flight over the hollow, the way on)
         {"id": "A the intake's track", "points": flat(i_pre[::2] + [i_pre[-1], I_EDGE]), "half": 4.5},
         {"id": "B the run-up to the kicker", "points": flat(k_pre[::2] + [k_pre[-1], K_LIP]), "half": 4.5},
         {"id": "B the flight over the hollow", "points": flat([K_LIP, along(K_LIP, dK, 85.0)]), "half": 6.0},
         {"id": "B the way on to Lakeside Drive", "points": flat(k_post[::2] + [k_post[-1]]), "half": 4.5}]
# the trail's ground smoothed along its line (a Gaussian over 6 m: the dam's slope stays, the lumps go), 7 m wide, off the lot, the pool
# deck, the courts, the buildings and the stream (BF and the stream are left as they are: nothing graded within 7 m of the stream)
streams = [pts(s["points"]) for s in m["streams"]]
def near_stream(q, r): return any(seg_dist(q, a, b) < r for st in streams for a, b in zip(st[:-1], st[1:]) if abs(a[0] - q[0]) < 60 and abs(a[1] - q[1]) < 60)
keep_boxes = [(1316.0 - 1.0, 1362.0 + 1.0, 1076.0 - 1.0, 1092.0 + 1.0), (1331.4, 1356.8, 1094.8, 1125.2), (1377.0, 1409.0, 1094.0, 1129.0)]
tc = cum(trail); tg = [ground(*q) for q in trail]
keep = [k for k, q in enumerate(trail) if not near_stream(q, 7.0) and not any(b[0] <= q[0] <= b[1] and b[2] <= q[1] <= b[3] for b in keep_boxes)
        and not any(near_box(i, q, 5.0) and poly_dist(q, ring) < 5.0 for i, ring, h in B)]
def gauss(k, sig): return sum(tg[j] * math.exp(-((tc[j] - tc[k]) / sig) ** 2 / 2) for j in range(len(trail))) / sum(math.exp(-((tc[j] - tc[k]) / sig) ** 2 / 2) for j in range(len(trail)))
grade_trail = [trail[k] for k in keep]; grade_trail_y = [round(gauss(k, 6.0), 2) for k in keep]
# Round 49 check: the lot is set level at the median of its ground (MapBuilder.ChastainLakes.cs, 1.5 m samples), 0.7 m over the ground at its west end;
# the trail's smoothed ground left a 1.1 m step 2 m off its north-west corner (in reverse the weaker cars all but stalled climbing it). Off the lot the
# trail's ground meets the lot's level at its edge and falls at most 16 % from it (it only fills: up to about 0.6 m) until it meets the slope
_lot = (1316.0, 1362.0, 1076.0, 1092.0)
_ls = sorted(ground(1339.0 + x, 1084.0 + z) for x in np.arange(-23.0, 23.0 + 1e-6, 1.5) for z in np.arange(-8.0, 8.0 + 1e-6, 1.5)); LOT_Y = _ls[len(_ls) // 2]
def _lot_d(q): return math.hypot(max(0.0, _lot[0] - q[0], q[0] - _lot[1]), max(0.0, _lot[2] - q[1], q[1] - _lot[3]))
_filled = max([0.0] + [max(0.0, LOT_Y - 0.16 * _lot_d(q) - y_) for q, y_ in zip(grade_trail, grade_trail_y)])
grade_trail_y = [round(max(y_, LOT_Y - 0.16 * _lot_d(q)), 2) for q, y_ in zip(grade_trail, grade_trail_y)]
print(f"the trail off the lot (its level {LOT_Y:.2f} m): ramped at 16 % from its edge, filled by up to {_filled:.2f} m")
# the lake jump's way down through the yards smoothed the same way (the lawns' steps and the gap's lumps: a vehicle at speed comes to the
# pile straight), from 12 m off North Lakeside Drive to the pile's foot; and the way on from the landing to Lakeside Drive
def smooth_line(p, sig, k0, k1):
    c_ = cum(p); g_ = [ground(*q) for q in p]
    return [p[k] for k in range(k0, k1)], [round(sum(g_[j] * math.exp(-((c_[j] - c_[k]) / sig) ** 2 / 2) for j in range(len(p))) / sum(math.exp(-((c_[j] - c_[k]) / sig) ** 2 / 2) for j in range(len(p))), 2) for k in range(k0, k1)]
k0w = next(k for k, q in enumerate(w_pre) if nearest_s(lap, q)[0] > 12.0); k1w = len(w_pre) - 3
gw, gwy = smooth_line(w_pre, 5.0, k0w, k1w)
k1e = len(e_pre) - 2; k0e = next(k for k, q in enumerate(e_pre) if nearest_s(lap, q)[0] > 6.0)
ge, gey = smooth_line(e_pre, 5.0, k0e, k1e)
grades = [{"id": "the lake trail", "points": flat(grade_trail), "y": grade_trail_y, "half": 2.5, "blend": 1.0},   # its middle 5 m (the tread's edges follow the ground): never into the lake (4.2 m off it)
          {"id": "W the run-up to the dock", "points": flat(gw), "y": gwy, "half": 2.5, "blend": 2.0},
          {"id": "E the lawn to the pier", "points": flat(ge), "y": gey, "half": 2.5, "blend": 2.0}]
# Round 49: the intake's track up the point to the jetty's root (its last meters level with the jetty), the kicker's run-up, the way on from its landing
k0i = next(k for k, q in enumerate(i_pre) if nearest_s(lap_r, q)[0] > 10.0); gi_, giy = smooth_line(i_pre, 5.0, k0i, len(i_pre))
giy = [min(y_, pad_y) if math.dist(q, I_ROOT) < 8.0 else y_ for q, y_ in zip(gi_, giy)]
k0k = next(k for k, q in enumerate(k_pre) if nearest_s(lap_r, q)[0] > 10.0); gk_, gky = smooth_line(k_pre, 5.0, k0k, len(k_pre) - 2)
k1p = next(k for k in range(len(k_post) - 1, -1, -1) if nearest_s(lap_r, k_post[k])[0] > 8.0); gp_, gpy = smooth_line(k_post, 5.0, 4, k1p)
grades += [{"id": "A the intake's track", "points": flat(gi_), "y": giy, "half": 2.5, "blend": 2.0},
           {"id": "B the run-up to the kicker", "points": flat(gk_), "y": gky, "half": 2.5, "blend": 2.0},
           {"id": "B the way on", "points": flat(gp_), "y": gpy, "half": 2.5, "blend": 2.0}]
# no grade reaches a road (the roads are the terrain flattened: an edit there is a bump) or the water (a lake's bed): such points are left out
ROAD_SEGS = [(a, b, r["width"] / 2.0) for r in roads for a, b in zip(pts(r["points"])[:-1], pts(r["points"])[1:])]
def clear_of(q, reach):
    if any(abs(a[0] - q[0]) < 80 and abs(a[1] - q[1]) < 80 and seg_dist(q, a, b) < reach + hw + 0.5 for a, b, hw in ROAD_SEGS): return False
    return not any(inside(q, w) or ring_dist(q, w) < reach + 0.5 for w in (WEST, EAST))
for gr in grades:
    p = pts(gr["points"]); keep_k = [k for k, q in enumerate(p) if clear_of(q, gr["half"] + gr["blend"])]
    gr["points"] = flat([p[k] for k in keep_k]); gr["y"] = [gr["y"][k] for k in keep_k]
# grades with a gap in them (the trail's kept points jump across the lot / stream): split into runs
def split_runs(gr):
    p = pts(gr["points"]); y = gr["y"]; runs, cur = [], [0]
    for k in range(1, len(p)):
        if math.dist(p[k], p[k - 1]) > 3.5: runs.append(cur); cur = [k]
        else: cur.append(k)
    runs.append(cur)
    return [dict(gr, id=f"{gr['id']} ({n + 1})", points=flat([p[k] for k in r]), y=[y[k] for k in r]) for n, r in enumerate(runs) if len(r) >= 3]
grades = [g for gr in grades for g in split_runs(gr)]
for gr in grades:
    p = pts(gr["points"]); print(f"  grade {gr['id']}: {len(p)} points, the ground moved by up to {max(abs(a - ground(*q)) for q, a in zip(p, gr['y'])):.2f} m")
lakes = {"trail": flat(trail), "trail_w": TRAIL_W, "concrete_to": flat([CONCRETE_TO]), "bridge": flat([BRIDGE_S, BRIDGE_N]), "bridge_half": 3.4,
         "pier_root": list(PIER_ROOT), "pier_dir": [round(pd[0], 5), round(pd[1], 5)], "pier_len": PIER_LEN, "pier_half": PIER_HW, "gazebo_half": GAZEBO_HALF,
         "drain": [round(DRAIN[0], 2), round(DRAIN[1], 2)], "drain_r": DRAIN_R, "shaft_r": SHAFT_R, "drain_chamber": "LD", "shut_reverse_of": SETTINGS["name"],
         "drain_exit": [round(tube3[0][0], 2), round(tube3[0][1], 2), round(tube3[0][2], 2)], "drain_tracks": flat(e_pre[2:-1]),
         "drain_hint": [round(e_pre[len(e_pre) // 3][0] + 3.0, 2), round(e_pre[len(e_pre) // 3][1] - 2.0, 2), 300.0],
         # Round 46: the boat jump (MapBuilder.LakesBoat.cs)
         "boat": {"lip": [round(W_LIP[0], 2), round(W_LIP[1], 2)], "dir": [round(dW[0], 5), round(dW[1], 5)], "lip_y": round(w_lip_y, 3), "deck_y": round(deck_yW, 3),
                  "water_y": round(WEST_Y, 3), "angle_deg": BOAT["angle"], "slide_m": round(slide_len, 2), "chute": BOAT["chute"], "wall": BOAT["wall"], "beam": BOAT["beam"],
                  "length": BOAT["length"], "dock_root": [round(DOCK_ROOT[0], 2), round(DOCK_ROOT[1], 2)], "dock_w": BOAT["dock_w"],
                  "sign": [W_SIGN[0], W_SIGN[1], 90.0], "clues_hidden_reverse_of": SETTINGS["name"], "tracks": flat(w_pre[3:int(len(w_pre) * 0.75)]),
                  "flights": {str(v): [f["t"], f["dist"], f["top"], f["wet"]] for v, f in w_flights.items()}},
         # Round 49 (A): the intake (MapBuilder.LakesIntake.cs) and its jetty; LI (pipe_network.py) under it, B29 on from its wall
         "intake": {"at": list(I_AT), "edge": list(I_EDGE), "dir": [round(dI[0], 5), round(dI[1], 5)], "pad_len": I_PAD_LEN, "pad_half": I_PAD_HALF, "pad_y": round(pad_y, 3),
                    "water_y": round(WEST_Y, 3), "catch_r": I_R, "shaft_r": I_SHAFT, "wall": I_WALL, "wall_up": I_WALL_UP, "chamber": "LI",
                    "exit": [round(tube9[0][0], 2), round(tube9[0][1], 2), round(tube9[0][2], 2)], "sign": list(I_SIGN), "tracks": flat(i_pre[3:]),
                    "shut_forward_of": SETTINGS["name"]},
         # Round 49 (B): the kids' wooden kicker (MapBuilder.LakesKicker.cs): a real jump
         "kicker": {"lip": list(K_LIP), "dir": [round(dK[0], 5), round(dK[1], 5)], "foot": [round(K_FOOT[0], 2), round(K_FOOT[1], 2)], "foot_y": round(k_foot_y, 3),
                    "lip_y": round(k_lip_y, 3), "len": round(k_len, 2), "width": K_W, "angle_deg": K_ANGLE, "sign": list(K_SIGN), "tracks": flat(k_pre[3:-2]),
                    "clues_hidden_forward_of": SETTINGS["name"], "flights": {str(v): [f["t"], f["dist"], f["top"], f["g"]] for v, f in k_flights.items()}}}
track = {"note": "Round 44 (Round 46: the boat jump): the Chastain Lakes race's world pieces, from Tools/mapgen/make_lakes_route.py (MapBuilder.LakesRace.cs). Coordinates are map meters (= Unity x, z).",
         "big_jumps": [], "landing_zones": landings, "clear_lines": clear, "break_lines": [], "grades": grades, "lakes": lakes}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_lakes_route.py from Data/map.json and Data/pipes/network.json. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": pieces, "official_shortcuts": shortcuts, "closures": closures,
    "closure_stubs_not_barred": stubs, "ai_zones": ai_zones,
    # Round 47 (A): the reverse lap of its own (RaceRoute: loop_rev and the *_rev fields); no reverse shortcuts
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev, "junctions_rev": merged_rev,
    "closures_rev": closures_rev, "ai_zones_reverse": ai_zones_rev,
})
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    view = sys.argv[sys.argv.index("--plot") + 1].split(",") if len(sys.argv) > sys.argv.index("--plot") + 1 else None
    x0, x1, z0, z1 = [float(v) for v in view] if view else (330, 1430, 880, 1530)
    fig, ax = plt.subplots(figsize=(16, 16 * (z1 - z0) / (x1 - x0)))
    for w in LAKES.values(): ax.fill(*zip(*w), color="#9cf")
    for i, ring, h in B:
        if x0 < ring[0][0] < x1 and z0 < ring[0][1] < z1: ax.fill(*zip(*ring), color="#c96"); ax.text(ring[0][0], ring[0][1], str(i), fontsize=6, clip_on=True)
    for r in roads:
        p = pts(r["points"])
        if any(x0 < x < x1 and z0 < z < z1 for x, z in p): ax.plot(*zip(*p), color="#999", lw=r["width"] * 0.25)
    ax.plot(*zip(*lap), "b-", lw=1.0)
    for path in (wpath, epath): ax.plot(*zip(*path), "r-", lw=1.0)
    for path in (ipath, kpath): ax.plot(*zip(*path), "m-", lw=1.0)   # Round 49: the reverse shortcuts
    ax.plot(*zip(*lap_r), "c--", lw=0.6)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g", clip_on=True)
    for c in closures: ax.plot(c["x"], c["z"], "kx")
    for z_ in landings: ax.fill(*zip(*pts(z_["poly"])), color="y", alpha=0.4)
    gp_ = point_at(lap, gate_s, C); ax.plot(*gp_, "ks"); ax.text(gp_[0] + 5, gp_[1], "gate", clip_on=True)
    ax.add_patch(plt.Circle(DRAIN, DRAIN_R, color="k", fill=False))
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    os.makedirs(os.path.join(ROOT, "local/round44"), exist_ok=True)
    out = os.path.join(ROOT, "local/round44/layout.png" if not view else "local/round44/layout_zoom.png")
    plt.savefig(out, dpi=80, bbox_inches="tight"); print("picture:", os.path.relpath(out, ROOT))
