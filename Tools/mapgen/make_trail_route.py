"""Round 62 (Dan, 2026-10-23 02:23 / 02:32): the "Noonday Creek Trail" race, on the real trail round Town Center at Cobb (Dan's pictures in
local/reference_photos/noonday_trail/: Google / Tripadvisor / Cobb County, looked at only; the course is laid on OSM's roads from Data/map.json and
on OSM's own "Noonday Creek Trail" ways from raw/osm_ext1.json). Dan: "I want the forward track to start at Bells Ferry, cut through the parking lot
and then until the Noonday creek trail ... I want to cut through town center mall, lets create at least a jump or two each way in the mall".
One lap, FORWARD (counterclockwise on the map):
  1. Bells Ferry Road south from the Noonday Creek Trailhead (the start / finish just south of its bridge over the creek) to Barrett Parkway
  2. Barrett Parkway west (the two-way stretch, then the westbound carriageway under I-575) to Mall Boulevard
  3. Mall Boulevard north to Town Center Drive, Town Center Drive north up the mall's east side
  4. THE LOT: off Town Center Drive across the parking lot to Halverson's (the east anchor) and in through its east doors
  5. THE MALL (Round 60's drivable Town Center, MapBuilder.Mall*.cs): along the department store's aisle (the mattress mountain: a sale display
     of stacked mattresses, a hump that throws you both ways), out through its glass front onto the upper concourse, up the car raffle stage at the
     top of the east escalator and off it over the escalator well (land on the escalator's steps and ride it down, or clear the well and stay up),
     west to the main entrance's balcony and off it over the two-storey entrance hall, out through the main doors (MapBuilder.TrailRace.cs builds
     the mall's race pieces; they stand in Free Roam too)
  6. THE LOT north of the mall, across Ring Road and Town Center Drive onto the trail by Hidden Forest Court
  7. THE TRAIL (OSM's ways 392529960, 392529962, 392529961, 392522751): north over the red truss footbridge, north-east along Noonday Creek, under
     I-575 (the line moved onto the creek's north bank there so the decks clear it), east past Chastain Meadows Parkway to the trailhead, out of its
     lot onto Bells Ferry Road
REVERSE: a lap of its own (RaceRoute: loop_rev and the *_rev fields), the same way round the other way: Bells Ferry north, the trail west, in the mall's
main doors, along the lower concourse and up the east escalator flat out (off its top over the raffle stage and through Halverson's glass front), the
mattress mountain, out the east doors, Mall Boulevard south, Barrett Parkway's eastbound carriageway east, Bells Ferry north.
No shortcuts yet (Dan picks them after driving it). Open roads ("traffic": true); two-way roads raced on their right-hand lane.

Heights: the lap's own heights inside the mall (RouteJson loop_y / loop_rev_y): codes that the map build turns into the mall's floors when it embeds
the route (MapBuilder.BuildRace: CODE_GROUND the built ground; level codes -(20000 + 1000 k) + offset: k 0 the lower floor, 1 the upper floor,
2 the east escalator's surface at that point).

Hand choices are the constants below; everything else is computed (re-run after mapgen; it reads Assets/Generated/terrain_full.r16 and
raw/osm_ext1.json). It writes:
  Data/routes/noonday_creek_trail.json   the race (RaceRoute.cs format)
  Data/tracks/noonday_creek_trail.json   what the map build makes (MapBuilder.TrailRace.cs): the trail's tread and center line, its shoulders
                                         graded and cleared, railings, the red truss footbridge, warning signs, the lot lines kept clear
Picture for checking (git-ignored): local/round62/layout.png with --plot [x0,x1,z0,z1].

Usage: python Tools/mapgen/make_trail_route.py [--plot [x0,x1,z0,z1]]
"""
import json, math, os, sys
import numpy as np
from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/noonday_creek_trail.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/noonday_creek_trail.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]; by_osm = {r["osm_id"]: r for r in roads}; bridges_osm = {b["osm_id"]: b for b in m["bridges"]}

def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
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
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def bearing(a, b): return math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360

# ---------------------------------------------------------------- the mall's frame (MapBuilder.Mall.cs: MallFrameDeg, its layout's numbers)
_A = math.radians(14.75); U = (math.cos(_A), math.sin(_A)); V = (-U[1], U[0])
def W(u, v): return (U[0] * u + V[0] * v, U[1] * u + V[1] * v)
def UVof(p): return (p[0] * U[0] + p[1] * U[1], p[0] * V[0] + p[1] * V[1])
CODE_GROUND = -10000.0
def LV(k, off=0.0): return round(-(20000.0 + 1000.0 * k) + off, 3)   # k 0 lower floor, 1 upper floor, 2 the east escalator's surface, 3 the red footbridge's deck,
                                                                     # 4 the deck of the road bridge under the point (Bells Ferry Road's over the creek)
VC = -1303.5                       # the concourse's middle (and the store's aisle, the east escalator's hole, the east doors)
EAST_DOOR_U, STORE_FRONT_U = 163.5, 53.0
ESC_FOOT_U, ESC_TOP_U = 8.0, 46.4  # the east escalator bank: its foot (lower floor) and its top, the raffle stage's lip (MapBuilder.Mall.cs: uFoot 8, run about 38.4 m to 1.4 m over the upper floor)
STAGE_LIP_H, STAGE_RAMP = 1.4, 4.31   # the raffle stage: its lip over the upper floor at the escalator's top, its ramp's length (18 degrees; check 1: 15 degrees from 1.07 m threw too flat)
MAIN_U, BALCONY_V, HALL_LAND_V, MAIN_DOOR_V = -51.0, -1286.0, -1274.0, -1267.1
TURN_R = 11.5                      # the right / left turn between the concourse and the main entrance's hall
MATTRESS_U = 110.8                 # the mattress mountain, in the store's aisle (MapBuilder.MallRace.cs MallMattressU)

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Noonday Creek Trail",
    "direction": "forward",
    "directions": ["forward", "reverse"],
    "laps": 3,
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
GATE_AT = (1735.0, -800.0)        # Bells Ferry Road, south of its bridge over Noonday Creek and the trailhead's drive
TRAIL_W = 3.0                     # the path: dark asphalt, a dashed yellow center line (photo 3)
BENCH_HALF, BENCH_BLEND = 5.5, 3.0   # the path and its grass shoulders lie level across 11 m (four vehicles race and pass: Dan), blending back over 3 m
CLEAR_HALF = 7.5                  # no tree within this of the trail's middle (the shoulders and a little more); the woods solid past it
DECK_CLEAR = 5.6                  # under I-575: the trail's ground at least this far under each deck
# the trail's OSM ways, in forward order (each driven against its OSM direction), and the underpass line that replaces 392529961 (the trail on the
# creek's north bank, 5 to 9 m south of OSM's line: OSM's runs along the embankment's toe at the decks' ends, 1 to 5 m under them)
TRAIL_WAYS = [392529960, 392529962, 392529961, 392522751]
UNDERPASS = [(598.0, -470.5), (607.0, -474.5), (616.0, -476.0), (626.0, -473.5), (636.0, -468.5), (646.0, -463.0), (658.0, -457.5), (672.0, -452.0),
             (688.0, -446.0), (703.0, -441.5), (714.0, -439.0)]
TRAILHEAD_DRIVE = 392522748        # the trailhead's drive from its lot up to Bells Ferry Road
BELLS_FERRY_S = [2673, 2674, "bridge:320582950", 2113, 2675, 2676, 2121, 2124, 2123, 2492, 2493, 2122]   # map.json road indices (and the bridge), north to south
BARRETT_TWO_WAY = [1282, 1103, 2735, 2736, 2729, 2728]          # east to west
BARRETT_WB = [650, 2082, 1422, 1418, 1307, 2367, 2368, 2626, 2369, 2370, 2371, 2372]   # the westbound carriageway, east to west
MALL_BLVD_NB = [2396, 2397, 2387]
TCD_EAST = [2716, 2715]            # Town Center Drive up the mall's east side
BARRETT_EB = [2380, 2386, 2381, 2255, 2379, 822, 2453, 1104, 820, 1420, 1421]   # the eastbound carriageway, west to east (reverse)
MALL_BLVD_SB = [2033, 2451, 2034, 2452, 2373]                  # Town Center Drive's stub, then Mall Boulevard south to Barrett (reverse)
# the lots (world points): off Town Center Drive to Halverson's east doors; from the main doors north to the trail
EAST_LEAVE_Z = -1385.0             # where the lap leaves Town Center Drive (its z; check 1: a vertex at -1295 made a hairpin of the turn)
EAST_LOT = [(622.0, -1335.0), (598.0, -1285.0), (575.0, -1248.0)]
NORTH_LOT = [(233.0, -1150.0), (222.0, -1060.0), (208.0, -990.0), (188.0, -940.0), (168.0, -903.0), (156.0, -878.0)]   # 9 m and more off the north anchor
NORTH_FROM = (196.5, -960.0)       # the north lot's way down to Town Center Drive starts here (the lot about 5 m over the road, the red bridge 2 m under it)
BRIDGE_GRADE = 0.07                # the red bridge's south end at most this much under Town Center Drive per meter from the road's edge
BOARD_PAD = (10.0, 20.0)           # the boardwalk under I-575 reaches this far before the first creek crossing and past the last (build 3: 10 m east left the stream's far bank as a wall)
CHECKPOINTS = [   # (x, z, label), forward order from the gate
    (1712.0, -1200.0, "Bells Ferry Road"),
    (1450.0, -1668.0, "Barrett Parkway"),
    (800.0, -1672.0, "Barrett Parkway under I-575"),
    (575.0, -1513.0, "Town Center Drive"),
    None,   # Halverson's (the store's aisle): set below from the mall's frame
    None,   # the main entrance (outside its doors)
    (190.0, -938.0, "the north lot"),
    (360.0, -620.0, "the trail by Hidden Forest Court"),
    (655.0, -460.0, "under I-575"),
    (1150.0, -720.0, "the trail by Chastain Meadows"),
    (1560.0, -690.0, "the trailhead"),
]
SIGN_BENDS = [(260.0, -715.0), (520.0, -520.0), (900.0, -540.0), (1340.0, -800.0)]   # yellow winding-road warning signs (breakable), near these bends

# ---------------------------------------------------------------- the pieces
def road_pts(ref):
    if isinstance(ref, str) and ref.startswith("bridge:"):
        return pts(bridges_osm[int(ref.split(":")[1])]["points"]), bridges_osm[int(ref.split(":")[1])]
    r = roads[ref]; return pts(r["points"]), r
def chain(refs, start=None):
    """the pieces end to end, each turned to run on from the last; returns [(points, road, two_way)]"""
    out = []; at = start
    for ref in refs:
        p, r = road_pts(ref)
        if at is not None and math.dist(p[-1], at) < math.dist(p[0], at): p = p[::-1]
        if at is not None and math.dist(p[0], at) > 1.0: raise SystemExit(f"the chain breaks before {ref} ({r.get('name')}): {math.dist(p[0], at):.1f} m")
        out.append((p, r, not r.get("oneway", False))); at = p[-1]
    return out
def osm_trail():
    tr = Transformer.from_crs(4326, m["utm_epsg"], always_xy=True); ox, oz = m["origin_utm"]
    o = json.load(open(os.path.join(ROOT, "raw/osm_ext1.json"), encoding="utf-8"))
    nodes = {e["id"]: e for e in o["elements"] if e["type"] == "node"}; ways = {e["id"]: e for e in o["elements"] if e["type"] == "way"}
    def way(wid):
        w = ways[wid]; q = [tr.transform(nodes[n]["lon"], nodes[n]["lat"]) for n in w["nodes"]]
        return [(round(x - ox, 2), round(y - oz, 2)) for x, y in q]
    return way

class Run:
    """a stretch of the lap: points, the lane offset to the right (two-way roads), the height codes, what it is"""
    def __init__(self, name, p, kind, off=0.0, y=None, osm=0):
        self.name, self.p, self.kind, self.off, self.osm = name, p, kind, off, osm
        self.y = y if y is not None else [CODE_GROUND] * len(p)

def road_runs(ch, step=3.0):
    rs = []
    for p, r, two in ch:
        q = resample(p, step); rs.append(Run(r.get("name") or "(unnamed)", q, "road", r["width"] / 4.0 if two else 0.0, osm=r["osm_id"]))
    return rs

def mall_line(pts_uv, codes, step=2.0):
    """points in the mall's frame (u, v) with a height code each: resampled (codes interpolated: levels kept, offsets blended)"""
    wpts = [W(u, v) for u, v in pts_uv]; c = cum(wpts); n = max(1, int(math.ceil(c[-1] / step)))
    out, ys = [], []
    for k in range(n + 1):
        s = c[-1] * k / n; p = point_at(wpts, s, c)
        i = max(0, min(len(c) - 2, next((j for j in range(len(c) - 1) if c[j] <= s <= c[j + 1] + 1e-9), len(c) - 2)))
        t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9); a, b = codes[i], codes[i + 1]
        ka, kb = round((-a - 20000) / 1000) if a < -15000 else None, round((-b - 20000) / 1000) if b < -15000 else None
        if ka is not None and kb is not None and ka == kb: ys.append(round(a + t * (b - a), 3))
        elif ka is not None and kb is not None:   # level to level (a drop, the escalator's lip): through real offsets on the first level
            ys.append(a if t < 0.5 else b)
        else: ys.append(a if (t < 0.5 or kb is None) and ka is not None else (b if kb is not None and t >= 0.5 else CODE_GROUND))
        out.append(p)
    return out, ys

def arc_uv(c, r, a0, a1, n=10):
    return [(c[0] + r * math.cos(math.radians(a0 + (a1 - a0) * k / n)), c[1] + r * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]

# ---- forward
osmway = osm_trail()
bf_s = chain(BELLS_FERRY_S, start=pts(by_osm[TRAILHEAD_DRIVE]["points"])[0])          # from the drive's end on Bells Ferry Road (1689, -580)
bar_tw = chain(BARRETT_TWO_WAY, start=bf_s[-1][0][-1])
bar_wb = chain(BARRETT_WB, start=bar_tw[-1][0][-1])
mb_nb = chain(MALL_BLVD_NB, start=bar_wb[-1][0][-1])
tcd = chain(TCD_EAST, start=mb_nb[-1][0][-1])
tcd_line = [q for p, r, two in tcd for q in p]
_tcd1 = resample(tcd_line, 1.0); s_leave = min(range(len(_tcd1)), key=lambda k: abs(_tcd1[k][1] - EAST_LEAVE_Z)) * cum(_tcd1)[-1] / (len(_tcd1) - 1)
tcd_cut = cut(tcd_line, 0.0, s_leave)
D_EAST = W(EAST_DOOR_U, VC)                                                             # Halverson's east doors (the middle)
east_lot = chaikin([tcd_cut[-1]] + EAST_LOT + [(D_EAST[0] + U[0] * 30.0, D_EAST[1] + U[1] * 30.0), (D_EAST[0] + U[0] * 14.0, D_EAST[1] + U[1] * 14.0)], 3)
east_lot = resample(east_lot, 2.0)
# inside, forward (east doors to main doors): the store, the raffle stage, the well, the upper concourse, the balcony, the hall
f_uv, f_y = [], []
def add(u, v, y): f_uv.append((u, v)); f_y.append(y)
add(EAST_DOOR_U + 14.0, VC, CODE_GROUND); add(EAST_DOOR_U + 4.0, VC, CODE_GROUND); add(EAST_DOOR_U, VC, LV(1))
add(STORE_FRONT_U, VC, LV(1)); add(ESC_TOP_U + STAGE_RAMP, VC, LV(1)); add(ESC_TOP_U, VC, LV(1, STAGE_LIP_H))
add(ESC_FOOT_U, VC, LV(1, 0.0))                                                         # over the well (the lap's line in the air: reset_skip below)
tc = (MAIN_U + TURN_R, VC + TURN_R)                                                     # the right turn onto the balcony
add(tc[0], VC, LV(1))
for q in arc_uv(tc, TURN_R, 270.0, 180.0, 8)[1:]: add(q[0], q[1], LV(1))
add(MAIN_U, BALCONY_V, LV(1)); add(MAIN_U, HALL_LAND_V, LV(0)); add(MAIN_U, MAIN_DOOR_V, LV(0))
add(MAIN_U, MAIN_DOOR_V + 6.0, CODE_GROUND); add(MAIN_U - 2.0, MAIN_DOOR_V + 20.0, CODE_GROUND)
mall_f, mall_fy = mall_line(f_uv, f_y, 2.0)
D_MAIN = W(MAIN_U, MAIN_DOOR_V)
north_lot = resample(chaikin([mall_f[-1], W(MAIN_U - 6.0, MAIN_DOOR_V + 40.0)] + NORTH_LOT, 3), 2.0)
# the trail
t960, t962, t961, t751 = (osmway(w) for w in TRAIL_WAYS)
t960 = t960[::-1] if math.dist(t960[0], north_lot[-1]) > math.dist(t960[-1], north_lot[-1]) else t960
t962 = t962[::-1] if math.dist(t962[0], t960[-1]) > math.dist(t962[-1], t960[-1]) else t962
t751 = t751[::-1] if math.dist(t751[0], UNDERPASS[-1]) > math.dist(t751[-1], UNDERPASS[-1]) else t751
bridge_red = (t960[0], t960[-1])                                                        # the red truss footbridge (its OSM way, south to north)
# off the red bridge's north end straight along its axis for 12 m, then easing into OSM's line over the next 16 m (build 3: the trail turned 40 degrees
# within 20 m of the end and cars arriving from the north clipped the truss's corner)
_ax = unit((t960[-1][0] - t960[0][0], t960[-1][1] - t960[0][1])); _t2 = resample(t962, 1.0); _c2 = cum(_t2)
_t2 = [(t960[-1][0] + _ax[0] * _c2[k], t960[-1][1] + _ax[1] * _c2[k]) if _c2[k] <= 12.0 else
       (lambda w, q, sq: (sq[0] + (q[0] - sq[0]) * w, sq[1] + (q[1] - sq[1]) * w))(min(1.0, (_c2[k] - 12.0) / 16.0), _t2[k], (t960[-1][0] + _ax[0] * _c2[k], t960[-1][1] + _ax[1] * _c2[k]))
       if _c2[k] <= 28.0 else _t2[k] for k in range(len(_t2))]
t962 = _t2
trail = resample([north_lot[-1]] + t960 + t962[1:], 2.0)
under = resample(chaikin([trail[-1]] + UNDERPASS[1:], 2), 2.0)
trail = trail + under[1:]
t751_s = nearest_s(t751, UNDERPASS[-1])[1]
# on past the boardwalk's east end straight along the underpass's axis for 12 m, then rounded into OSM's line from 34 m on (build 4's check: the trail
# turned 60 degrees 4 m past the deck's end and reverse cars arriving from the southeast met its north railing)
_ax = unit((UNDERPASS[-1][0] - UNDERPASS[-2][0], UNDERPASS[-1][1] - UNDERPASS[-2][1])); _t5 = resample(cut(t751, t751_s, None), 1.0); _c5 = cum(_t5)
_k34 = next(k for k in range(len(_t5)) if _c5[k] >= 34.0); _p0 = (UNDERPASS[-1][0] + _ax[0] * 12.0, UNDERPASS[-1][1] + _ax[1] * 12.0); _p3 = _t5[_k34]
_d3 = unit((_t5[_k34 + 1][0] - _p3[0], _t5[_k34 + 1][1] - _p3[1])); _L = math.dist(_p0, _p3)
def _herm(t): h00, h10, h01, h11 = 2*t**3 - 3*t**2 + 1, t**3 - 2*t**2 + t, -2*t**3 + 3*t**2, t**3 - t**2; return tuple(h00 * _p0[i] + h10 * _L * _ax[i] + h01 * _p3[i] + h11 * _L * _d3[i] for i in (0, 1))
_t5 = [UNDERPASS[-1], _p0] + [_herm(k / 20.0) for k in range(1, 20)] + _t5[_k34:]
trail = trail + resample(_t5, 2.0)[1:]
# off the trail's end into the trailhead's lot and up its drive to Bells Ferry Road
drive = pts(by_osm[TRAILHEAD_DRIVE]["points"])[::-1]                                    # lot end first
th = resample(chaikin([trail[-1], (trail[-1][0] + 14.0, trail[-1][1] + 6.0), drive[0], drive[2], drive[5]], 3) + drive[6:], 2.0)

# ---------------------------------------------------------------- the trail's heights (check 1: the bench, the red bridge and the north lot's way down to
# Town Center Drive were set apart and met in steps; the creek crosses the trail twice under I-575, where OSM's way is a bridge): one profile
streams = [pts(s_["points"]) for s_ in m["streams"]]
def stream_dist(q): return min([seg_dist(q, a, b) for st in streams for a, b in zip(st[:-1], st[1:]) if abs(a[0] - q[0]) < 60 and abs(a[1] - q[1]) < 60] + [99.0])
ROAD_SEGS = [(a, b, r["width"] / 2.0, r["class"]) for r in roads for a, b in zip(pts(r["points"])[:-1], pts(r["points"])[1:])]
def road_dist(q, skip_cls=("service",)):
    return min([seg_dist(q, a, b) - hw for a, b, hw, cl in ROAD_SEGS if cl not in skip_cls and abs(a[0] - q[0]) < 80 and abs(a[1] - q[1]) < 80] + [99.0])
decks575 = [(pts(b["points"]), b["deck_y"]) for b in m["bridges"] if b["osm_id"] in (196231552, 196231556, 451950529)]   # I-575 and its express lanes over the creek
def deck_over(q):
    best = None
    for p_, dy in decks575:
        for i in range(len(p_) - 1):
            if seg_dist(q, p_[i], p_[i + 1]) < 7.0:
                y = dy[min(i, len(dy) - 1)]; best = y if best is None else min(best, y)
    return best
tc_ = cum(trail); tg = [ground(*q) for q in trail]
def gsm(vals, cv, k, sig):
    j0, j1 = max(0, k - 14), min(len(vals), k + 15)
    w = [math.exp(-((cv[j] - cv[k]) / sig) ** 2 / 2) for j in range(j0, j1)]
    return sum(vals[j] * w[j - j0] for j in range(j0, j1)) / sum(w)
gy = [gsm(tg, tc_, k, 6.0) for k in range(len(trail))]
capped = 0
for k, q in enumerate(trail):
    d_ = deck_over(q)
    if d_ is not None and gy[k] > d_ - DECK_CLEAR: gy[k] = d_ - DECK_CLEAR; capped += 1
def segx(a, b, c, d):
    def cr(o, p_, q): return (p_[0] - o[0]) * (q[1] - o[1]) - (p_[1] - o[1]) * (q[0] - o[0])
    return cr(a, b, c) * cr(a, b, d) < 0 and cr(c, d, a) * cr(c, d, b) < 0
rb0, rb1 = nearest_s(trail, bridge_red[0])[1], nearest_s(trail, bridge_red[1])[1]
us0, us1 = nearest_s(trail, UNDERPASS[0])[1], nearest_s(trail, UNDERPASS[-1])[1]
cross = [tc_[k] for k in range(len(trail) - 1) if us0 - 5 <= tc_[k] <= us1 + 5 for st in streams for a, b in zip(st[:-1], st[1:])
         if abs(a[0] - trail[k][0]) < 40 and abs(a[1] - trail[k][1]) < 40 and segx(trail[k], trail[k + 1], a, b)]
bw0, bw1 = (max(us0 - 4.0, min(cross) - BOARD_PAD[0]), min(us1 + 12.0, max(cross) + BOARD_PAD[1])) if cross else (None, None)
# Town Center Drive where the north lot's line crosses it (its own height: a road is the terrain), the red bridge's south end under it, the north end on the bench
nl_from = nearest_s(north_lot, NORTH_FROM)[1]; nlc = cum(north_lot)
nl = [q for k, q in enumerate(north_lot) if nlc[k] >= nl_from]
on_tcd = [road_dist(q) < 0.6 for q in nl]
k_t1 = max(k for k, o in enumerate(on_tcd) if o); k_t0 = k_t1
while k_t0 > 0 and on_tcd[k_t0 - 1]: k_t0 -= 1   # the last road the line crosses (Town Center Drive), not the lot's ring road before it
y_tcd = sum(ground(*nl[k]) for k in range(k_t0, k_t1 + 1)) / (k_t1 - k_t0 + 1)
d_north = math.dist(nl[k_t1], bridge_red[0]) + 2.0
y_s = round(max(min(y_tcd - 0.3, gy[min(range(len(trail)), key=lambda k: abs(tc_[k] - rb0))] + 3.5), y_tcd - BRIDGE_GRADE * d_north), 2)
k_rb1 = min(range(len(trail)), key=lambda k: abs(tc_[k] - rb1)); y_n = round(gy[k_rb1], 2)
def herm(y0, g0, y1, g1, L, t):
    t2, t3 = t * t, t * t * t
    return (2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * L * g0 + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * L * g1
# the north lot's way down: from NORTH_FROM (the lot's own height and grade) to Town Center Drive's near edge (level), across it, from its far edge down to
# the red bridge's south end (at the deck's grade)
nly = [0.0] * len(nl); nlcc = cum(nl)
gA = (ground(*nl[2]) - ground(*nl[0])) / max(nlcc[2], 0.1); yA = ground(*nl[0])
LA = nlcc[k_t0]; deck_g = (y_n - y_s) / math.dist(*bridge_red)
for k in range(len(nl)):
    if k < k_t0: nly[k] = herm(yA, gA, y_tcd, 0.0, LA, nlcc[k] / LA)
    elif k <= k_t1: nly[k] = y_tcd
    else: nly[k] = None
s_t1 = nlcc[k_t1]; L2 = (nlcc[-1] - s_t1) + rb0
def after_tcd(s_after): return herm(y_tcd, 0.0, y_s, deck_g, L2, max(0.0, min(1.0, s_after / L2)))
for k in range(k_t1 + 1, len(nl)): nly[k] = after_tcd(nlcc[k] - s_t1)
for k in range(len(trail)):
    if tc_[k] <= rb0: gy[k] = after_tcd(nlcc[-1] - s_t1 + tc_[k])
# the red bridge: straight from y_s to y_n; the bench beyond its north end eased onto it over 14 m (build 3: a 0.6 m step up off its end)
for k in range(len(trail)):
    if rb0 < tc_[k] < rb1: gy[k] = y_s + (y_n - y_s) * (tc_[k] - rb0) / (rb1 - rb0)
for k in range(len(trail)):
    dd = tc_[k] - rb1
    if 0 <= dd < 14.0: gy[k] = y_n + (gy[k] - y_n) * (dd / 14.0) ** 2 * (3 - 2 * dd / 14.0)
# the boardwalk under I-575: 0.9 m over the ground under it at least, smoothed along (a Gaussian of 6 m), held DECK_CLEAR under I-575's decks, meeting the bench
if bw0 is not None:
    ks = [k for k in range(len(trail)) if bw0 <= tc_[k] <= bw1]
    raw_b = [max(gy[k], tg[k] + 0.9) for k in ks]; cb = [tc_[k] for k in ks]
    sm_b = [gsm(raw_b, cb, i, 6.0) for i in range(len(ks))]
    for i, k in enumerate(ks):
        d_ = deck_over(trail[k]); y = max(sm_b[i], tg[k] + 0.6)
        if d_ is not None: y = min(y, d_ - DECK_CLEAR)
        w = min(1.0, (tc_[k] - bw0) / 8.0, (bw1 - tc_[k]) / 8.0)
        gy[k] = gy[k] + max(0.0, w) * (y - gy[k])
    # the bench either side eased onto its ends over 12 m
    for e, sgn in ((bw0, -1), (bw1, 1)):
        ke = min(range(len(trail)), key=lambda k: abs(tc_[k] - e)); ye = gy[ke]
        for k in range(len(trail)):
            dd = (tc_[k] - e) * sgn
            if 0 < dd < 12.0: gy[k] = ye + (gy[k] - ye) * (dd / 12.0) ** 2 * (3 - 2 * dd / 12.0)
TRAIL_DECKS = [{"id": "the red truss footbridge", "style": "truss", "s0": rb0, "s1": rb1, "width": 3.7}]
if bw0 is not None: TRAIL_DECKS.append({"id": "the boardwalk under I-575", "style": "boardwalk", "s0": bw0, "s1": bw1, "width": 3.6})
for d_ in TRAIL_DECKS:
    ks = [k for k in range(len(trail)) if d_["s0"] - 1e-6 <= tc_[k] <= d_["s1"] + 1e-6]
    d_["line"] = flat([trail[k] for k in ks]) if d_["style"] != "truss" else flat(list(bridge_red)); d_["y"] = [round(gy[k], 3) for k in ks] if d_["style"] != "truss" else [y_s, y_n]
print(f"the trail's profile: Town Center Drive {y_tcd:.2f} m where the lap crosses it; the red bridge {y_s:.2f} to {y_n:.2f} m over {math.dist(*bridge_red):.0f} m (its valley "
      f"{min(tg[k] for k in range(len(trail)) if rb0 < tc_[k] < rb1):.2f} m at its lowest); the boardwalk under I-575 from {bw0:.0f} to {bw1:.0f} m along the trail ({len(cross)} creek crossings); "
      f"{capped} points held {DECK_CLEAR} m under I-575")
def on_trail_deck(q):
    for d_ in TRAIL_DECKS:
        ln = pts(d_["line"])
        if any(seg_dist(q, a, b) < d_["width"] / 2.0 + 0.3 for a, b in zip(ln[:-1], ln[1:])): return True
    return False
# the runs, forward, from Bells Ferry Road's start (the gate is set on it below)
runs = road_runs(bf_s) + road_runs(bar_tw) + road_runs(bar_wb) + road_runs(mb_nb) + [Run("Town Center Drive", resample(tcd_cut, 3.0), "road", by_osm[1377606893]["width"] / 4.0, osm=1377606893)]
runs += [Run("the east lot", east_lot, "lot"), Run("Town Center (the mall)", mall_f, "mall", y=mall_fy), Run("the north lot", north_lot, "lot"),
         Run("Noonday Creek Trail", trail, "trail"), Run("the trailhead", th, "lot")]
def join(runs):
    line, offs, ys, kinds, names = [], [], [], [], []
    for r in runs:
        p, y = r.p, r.y
        if line:
            gap = math.dist(line[-1], p[0])
            if gap > 3.5: raise SystemExit(f"the lap breaks before {r.name}: {gap:.1f} m")
            if gap < 0.3: p, y = p[1:], y[1:]
        line += p; offs += [r.off] * len(p); ys += y; kinds += [r.kind] * len(p); names += [r.name] * len(p)
    return line, offs, ys, kinds, names
line, offs, ys, kinds, names = join(runs)
if math.dist(line[0], line[-1]) > 3.5: raise SystemExit("the lap does not close: gap %.1f m" % math.dist(line[0], line[-1]))
line[-1] = line[0]
def lane(line, offs):
    N = len(line); sm = [sum(offs[max(0, i - 5):min(N, i + 6)]) / len(offs[max(0, i - 5):min(N, i + 6)]) for i in range(N)]
    out = []
    for i in range(N):
        a, b = line[max(0, i - 1)], line[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
        out.append((line[i][0] + t[1] / n * sm[i], line[i][1] - t[0] / n * sm[i]))
    out[-1] = out[0]; return out
lap = lane(line, offs)
# the lap starts at the gate (gate_s 0: the run-in is the lead-in alone)
_gd, _gs = nearest_s(lap, GATE_AT); _C = cum(lap)
k0 = min(range(len(lap)), key=lambda k: abs(_C[k] - _gs))
def rotate(a, k): return a[k:-1] + a[:k] + [a[k]]
lap = rotate(lap, k0); ys = rotate(ys, k0); kinds = rotate(kinds, k0); names = rotate(names, k0)
# on a deck: the red footbridge's (the trail over it), a road bridge's (the road on it): their own heights, from the map build (RouteJson codes 3, 4)
DECKS = [(pts(b["points"]), b["width"] / 2.0 + 0.5, b["name"]) for b in m["bridges"]]
def on_decks(lp, ys_, kinds_, names_):
    n3 = n4 = 0
    for k, q in enumerate(lp):
        if ys_[k] != CODE_GROUND: continue
        if kinds_[k] == "trail":
            if on_trail_deck(q): ys_[k] = LV(3); n3 += 1
        elif kinds_[k] == "road":
            for dp, hw, dn in DECKS:
                if dn and names_[k] and dn.split(" ")[0] == names_[k].split(" ")[0] and any(seg_dist(q, a, b) < hw for a, b in zip(dp[:-1], dp[1:]) if abs(a[0] - q[0]) < 120 and abs(a[1] - q[1]) < 120):
                    ys_[k] = LV(4); n4 += 1; break
    return n3, n4
print("on decks (forward): the trail's decks %d points, road bridges %d points" % on_decks(lap, ys, kinds, names))
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
print(f"forward lap {L_lap:.0f} m: Bells Ferry {sum(cum(p)[-1] for p, r, t in bf_s):.0f}, Barrett {sum(cum(p)[-1] for p, r, t in bar_tw + bar_wb):.0f}, "
      f"Mall Boulevard {sum(cum(p)[-1] for p, r, t in mb_nb):.0f}, Town Center Drive {cum(tcd_cut)[-1]:.0f}, east lot {cum(east_lot)[-1]:.0f}, "
      f"the mall {cum(mall_f)[-1]:.0f}, north lot {cum(north_lot)[-1]:.0f}, the trail {cum(trail)[-1]:.0f}, the trailhead {cum(th)[-1]:.0f}")
lead = [point_at(lap, L_lap - LEAD_IN_M + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0))] + [lap[0]]
def mall_s(lp, u, v):
    return nearest_s(lp, W(u, v))[1]
cps = []
CPS = list(CHECKPOINTS)
CPS[4] = (*W(118.0, VC), "Halverson's"); CPS[5] = (*W(MAIN_U - 2.0, MAIN_DOOR_V + 14.0), "the main entrance")
for x, z, label in CPS:
    s = round(S((x, z)), 1); p = point_at(lap, s, C); cps.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
cps.sort(key=lambda c: c["s"])
for i, c in enumerate(cps, 1): c["id"] = i
print("forward checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps))

# ---- reverse (a lap of its own)
bf_n = chain(BELLS_FERRY_S[::-1], start=pts(roads[2122]["points"])[0] if math.dist(pts(roads[2122]["points"])[0], (1714.0, -1686.0)) < 2 else pts(roads[2122]["points"])[-1])
bar_eb = chain(BARRETT_EB, start=pts(roads[2380]["points"])[0])
bar_tw_e = chain(BARRETT_TWO_WAY[::-1], start=bar_eb[-1][0][-1])
mb_sb = chain(MALL_BLVD_SB, start=tcd_line[0])                                         # from Town Center Drive at (466, -1579)
tcd_rev = cut(tcd_line, 0.0, s_leave)[::-1]
east_lot_r = east_lot[::-1]
r_uv, r_y = [], []
def addr(u, v, y): r_uv.append((u, v)); r_y.append(y)
addr(MAIN_U - 2.0, MAIN_DOOR_V + 20.0, CODE_GROUND); addr(MAIN_U, MAIN_DOOR_V + 6.0, CODE_GROUND); addr(MAIN_U, MAIN_DOOR_V, LV(0))
addr(MAIN_U, -1292.0, LV(0))
rc = (MAIN_U + TURN_R, -1292.0)                                                         # the left turn onto the lower concourse
for q in arc_uv(rc, TURN_R, 180.0, 270.0, 8)[1:]: addr(q[0], q[1], LV(0))
addr(ESC_FOOT_U, VC, LV(2)); addr(ESC_TOP_U, VC, LV(2))                                  # up the east escalator (its own surface)
addr(ESC_TOP_U + 9.0, VC, LV(1)); addr(STORE_FRONT_U, VC, LV(1)); addr(EAST_DOOR_U, VC, LV(1))
addr(EAST_DOOR_U + 4.0, VC, CODE_GROUND); addr(EAST_DOOR_U + 14.0, VC, CODE_GROUND)
mall_r, mall_ry = mall_line(r_uv, r_y, 2.0)
north_lot_r = north_lot[::-1]
trail_r = trail[::-1]; th_r = th[::-1]
runs_r = road_runs(bf_n)[:]
runs_r = [Run("the trailhead", th_r, "lot"), Run("Noonday Creek Trail", trail_r, "trail"), Run("the north lot", north_lot_r, "lot"),
          Run("Town Center (the mall)", mall_r, "mall", y=mall_ry), Run("the east lot", east_lot_r, "lot"),
          Run("Town Center Drive", resample(tcd_rev, 3.0), "road", by_osm[1377606893]["width"] / 4.0, osm=1377606893)] + road_runs(mb_sb) + road_runs(bar_eb) + road_runs(bar_tw_e) + road_runs(bf_n)
line_r, offs_r, ys_r, kinds_r, names_r = join(runs_r)
if math.dist(line_r[0], line_r[-1]) > 3.5: raise SystemExit("the reverse lap does not close: gap %.1f m" % math.dist(line_r[0], line_r[-1]))
line_r[-1] = line_r[0]
lap_r = lane(line_r, offs_r)
print("on decks (reverse): the trail's decks %d points, road bridges %d points" % on_decks(lap_r, ys_r, kinds_r, names_r))
Cr = cum(lap_r); L_rev = Cr[-1]
def SR(q): return nearest_s(lap_r, q)[1]
gate_s_rev = round(SR(GATE_AT), 1)
cps_rev = []
for x, z, label in CPS:
    s_ = round(SR((x, z)), 1); p_ = point_at(lap_r, s_, Cr); cps_rev.append({"s": s_, "pos": [round(p_[0], 1), round(p_[1], 1)], "label": label})
cps_rev = sorted(cps_rev, key=lambda c: (c["s"] - gate_s_rev) % L_rev)
for i, c in enumerate(cps_rev, 1): c["id"] = i
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps_rev))

# ---------------------------------------------------------------- closures (as the other open-road races'), only off the road stretches
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
SKIP_CLASS = ("motorway", "motorway_link", "trunk_link", "service", "footway", "cycleway", "path")
def closures_for(lap, center, kinds, other_way=()):
    # a side road is "on" the course where it meets the road stretches' centerlines (the lap itself is set off to the right-hand lane: 6 m on Barrett)
    road_k = [k for k in range(len(center) - 1) if kinds[k] == "road" and kinds[k + 1] == "road"]
    segs = [(center[k], center[k + 1]) for k in road_k]; allsegs = list(zip(lap[:-1], lap[1:]))
    grid = {}
    for i, (a, b) in enumerate(segs):
        for gx in range(int(min(a[0], b[0]) // 50) - 1, int(max(a[0], b[0]) // 50) + 2):
            for gz in range(int(min(a[1], b[1]) // 50) - 1, int(max(a[1], b[1]) // 50) + 2): grid.setdefault((gx, gz), []).append(i)
    agrid = {}
    for i, (a, b) in enumerate(allsegs):
        for gx in range(int(min(a[0], b[0]) // 50) - 1, int(max(a[0], b[0]) // 50) + 2):
            for gz in range(int(min(a[1], b[1]) // 50) - 1, int(max(a[1], b[1]) // 50) + 2): agrid.setdefault((gx, gz), []).append(i)
    def road_dist(q): return min([seg_dist(q, *segs[i]) for i in grid.get((int(q[0] // 50), int(q[1] // 50)), [])] + [1e9])
    def course_dist(q): return min([seg_dist(q, *allsegs[i]) for i in agrid.get((int(q[0] // 50), int(q[1] // 50)), [])] + [1e9])
    def follow(ri, k, step, arc0, depth, seen):
        p = roads_pts[ri]; last = None
        for d, q, tan in walk(p, k, step):
            last = (arc0 + d, q, tan)
            if course_dist(q) >= SETTINGS["closure_clear_m"]: return (arc0 + d, q, tan, ri)
            if arc0 + d > 120: return None
        if last is None or depth >= 3: return None
        end = p[-1] if step > 0 else p[0]
        for rj, pj in enumerate(roads_pts):
            if rj == ri or rj in seen or roads[rj]["class"] in SKIP_CLASS or roads[rj]["class"] == "trunk": continue
            for kj in (0, len(pj) - 1):
                if math.dist(pj[kj], end) < 1.5:
                    res = follow(rj, kj, 1 if kj == 0 else -1, last[0], depth + 1, seen | {ri})
                    if res: return res
        return None
    xs = [q[0] for a, b in segs for q in (a, b)]; zs = [q[1] for a, b in segs for q in (a, b)]
    box = (min(xs) - 150, max(xs) + 150, min(zs) - 150, max(zs) + 150)
    closures, junc = [], []
    for ri, r in enumerate(roads):
        if r["class"] in SKIP_CLASS or r["class"] == "trunk" or ri in other_way: continue   # (the other direction's carriageways: Barrett's, Mall Boulevard's, never barred)   # (the lap's own roads too: Town Center Drive runs on past where the lap leaves it)
        p = roads_pts[ri]
        if not any(box[0] < x < box[1] and box[2] < z < box[3] for x, z in p): continue
        on = [road_dist(v) < max(2.5, r["width"] / 4.0 + 1.0) for v in p]
        for k in range(len(p)):
            if not on[k]: continue
            for step in (1, -1):
                nb = k + step
                if not (0 <= nb < len(p)) or on[nb]: continue
                res = follow(ri, k, step, 0.0, 0, frozenset())
                if res is None: continue
                arc, q, tan, rj = res; sj = nearest_s(lap, p[k])
                nm = r["name"] or "(unnamed " + r["class"] + ")"
                junc.append((sj[1], nm))
                closures.append({"road": nm, "osm_id": r["osm_id"], "x": round(q[0], 2), "z": round(q[1], 2), "dx": round(tan[0], 4), "dz": round(tan[1], 4),
                                 "width_m": roads[rj]["width"], "junction_s": round(sj[1], 1), "contact": [round(p[k][0], 1), round(p[k][1], 1)], "arc_m": round(arc, 1),
                                 "barrier_on": roads[rj]["name"] or "(unnamed " + roads[rj]["class"] + ")"})
    closures.sort(key=lambda c: -c["width_m"]); kept = []
    for c in closures:
        if any(math.dist((c["x"], c["z"]), (o["x"], o["z"])) < 5.0 and abs(c["dx"] * o["dx"] + c["dz"] * o["dz"]) > 0.8 for o in kept): continue
        kept.append(c)
    closures = sorted(kept, key=lambda c: c["junction_s"])
    for i, c in enumerate(closures, 1): c["id"] = f"B{i}"; c["label"] = f"{c['road']} at lap {c['junction_s']:.0f} m"
    junc.sort(); merged = []
    for s, nm in junc:
        if merged and s - merged[-1]["s"] < 6: merged[-1]["roads"].append(nm); continue
        merged.append({"s": round(s, 1), "roads": [nm]})
    for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(lap, j["s"], cum(lap))]
    return closures, merged
used = {r.osm for r in runs + runs_r if r.osm}
used |= {roads[i]["osm_id"] for i in BELLS_FERRY_S + BARRETT_TWO_WAY + BARRETT_WB + MALL_BLVD_NB + TCD_EAST + BARRETT_EB + MALL_BLVD_SB if isinstance(i, int)}
closures, merged = closures_for(lap, rotate(line, k0), kinds, set(BARRETT_EB + MALL_BLVD_SB[1:]))   # (not Town Center Drive's stub: it leads on west)
closures_rev, merged_rev = closures_for(lap_r, line_r, kinds_r, set(BARRETT_WB + MALL_BLVD_NB))
for c in closures_rev: c["junction_s"] = round(c["junction_s"], 1)
print(f"closures (barriers while racing): forward {len(closures)}, reverse {len(closures_rev)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")

# ---------------------------------------------------------------- the AI: held speeds and narrow stretches; resets past the flights
def zone_s(lp, a, b, s_of): return round(s_of(a), 1), round(s_of(b), 1)
def zones(lp, s_of, rev):
    z = []
    def Z(id_, a, b, speed): s0, s1 = round(s_of(a), 1), round(s_of(b), 1); z.append({"id": id_, "s0": min(s0, s1), "s1": max(s0, s1), "speed": speed})
    if not rev:
        Z("the trailhead's drive", th[0], drive[4], 11.0)
        Z("onto Bells Ferry Road", drive[4], (1708.0, -616.0), 12.0)
        Z("onto Mall Boulevard", (540.0, -1686.0), (505.0, -1660.0), 13.0)
        Z("into the east lot", tcd_cut[-1], east_lot[len(east_lot) // 2], 16.0)
        Z("to Halverson's doors", east_lot[len(east_lot) // 2], W(EAST_DOOR_U - 6.0, VC), 18.0)
        Z("the mattress mountain", W(MATTRESS_U + 14.0, VC), W(MATTRESS_U - 14.0, VC), 26.0)
        Z("the raffle stage", W(ESC_TOP_U + 14.0, VC), W(ESC_TOP_U - 4.0, VC), 28.0)   # off the lip at 28: clears the well (24.5 m/s and up), lands about u -3
        Z("to the balcony", W(-4.0, VC), W(MAIN_U, BALCONY_V), 12.0)
        Z("the hall", W(MAIN_U, BALCONY_V), W(MAIN_U - 6.0, MAIN_DOOR_V + 40.0), 13.0)
        Z("across Town Center Drive", NORTH_FROM, trail[8], 13.0)
        Z("the red bridge", trail[8], point_at(trail, rb1 + 6.0, tc_), 12.0)
        Z("under I-575", point_at(trail, min(us0, bw0) - 20.0, tc_), point_at(trail, max(us1, bw1) + 8.0, tc_), 14.0)   # (build 3: the boardwalk's ends met at speed)
        Z("the bend past the boardwalk", point_at(trail, max(us1, bw1) + 8.0, tc_), point_at(trail, max(us1, bw1) + 34.0, tc_), 10.0)   # (build 4: cars ran wide of it)
    else:
        Z("off Bells Ferry Road", (1708.0, -616.0), drive[4], 11.0)
        Z("the trailhead's drive", drive[4], th[0], 11.0)
        Z("the bend past the boardwalk", point_at(trail, max(us1, bw1) + 45.0, tc_), point_at(trail, max(us1, bw1) + 8.0, tc_), 10.0)   # (build 4: into the deck's railing)
        Z("under I-575", point_at(trail, max(us1, bw1) + 8.0, tc_), point_at(trail, min(us0, bw0) - 6.0, tc_), 14.0)
        Z("the red bridge", point_at(trail, rb1 + 25.0, tc_), trail[8], 12.0)   # (build 3: onto its north end at 24.7 m/s, into the truss)
        Z("across Town Center Drive", trail[8], NORTH_FROM, 13.0)
        Z("the main doors", W(MAIN_U - 6.0, MAIN_DOOR_V + 40.0), W(MAIN_U, -1290.0), 13.0)
        Z("the escalator", W(-10.0, VC), W(ESC_TOP_U + 10.0, VC), 30.0)
        Z("the mattress mountain", W(MATTRESS_U - 14.0, VC), W(MATTRESS_U + 14.0, VC), 26.0)
        Z("out of Halverson's", W(EAST_DOOR_U - 6.0, VC), east_lot[len(east_lot) // 2], 16.0)
        Z("onto Town Center Drive", east_lot[len(east_lot) // 2], tcd_cut[-1], 14.0)
        Z("onto Barrett Parkway", (505.0, -1680.0), (540.0, -1705.0), 12.0)
    return [q for q in z if q["s1"] > q["s0"] and q["s1"] - q["s0"] < 400]
ai_zones = zones(lap, S, False); ai_zones_rev = zones(lap_r, SR, True)
def widths(s_of, rev):
    w = []
    def Wd(id_, a, b, half): s0, s1 = round(s_of(a), 1), round(s_of(b), 1); w.append({"id": id_, "s0": min(s0, s1), "s1": max(s0, s1), "half": half})
    Wd("the red bridge", bridge_red[0], bridge_red[1], 1.2)
    Wd("under I-575", UNDERPASS[0], UNDERPASS[-1], 1.4)
    Wd("Halverson's", W(EAST_DOOR_U + 2.0, VC), W(STORE_FRONT_U - 2.0, VC), 2.8)
    Wd("the escalator and the stage", W(ESC_TOP_U + STAGE_RAMP + 6.0, VC), W(ESC_FOOT_U - 6.0, VC), 1.6)
    Wd("the balcony and the hall", W(MAIN_U + 4.0, VC), W(MAIN_U, MAIN_DOOR_V + 4.0), 2.4)
    Wd("the main doors", W(MAIN_U, MAIN_DOOR_V + 4.0), W(MAIN_U - 2.0, MAIN_DOOR_V + 16.0), 2.6)
    Wd("the east doors", W(EAST_DOOR_U + 12.0, VC), W(EAST_DOOR_U + 2.0, VC), 2.6)
    return [q for q in w if q["s1"] > q["s0"] and q["s1"] - q["s0"] < 400]
ai_widths, ai_widths_rev = widths(S, False), widths(SR, True)
# resets: a reset over the escalator well (forward) puts the vehicle on the upper floor past it; on the balcony's flight, in the hall past the landing;
# over the raffle stage's flight (reverse), in the store past the front
def skip(lp, s_of, gate_s, a, b, to, id_):
    s0, s1, st = s_of(a), s_of(b), s_of(to)
    return {"id": id_, "s0": round(s0, 1), "s1": round(s1, 1), "to_s": round(st, 1)}
reset_skip = [skip(lap, S, 0.0, W(ESC_TOP_U + 1.0, VC), W(ESC_FOOT_U - 1.0, VC), W(ESC_FOOT_U - 8.0, VC), "the escalator well"),
              skip(lap, S, 0.0, W(MAIN_U, BALCONY_V - 0.5), W(MAIN_U, HALL_LAND_V), W(MAIN_U, MAIN_DOOR_V - 2.0), "the balcony's drop")]
reset_skip_rev = [skip(lap_r, SR, gate_s_rev, W(ESC_TOP_U - 1.0, VC), W(STORE_FRONT_U, VC), W(STORE_FRONT_U + 6.0, VC), "off the escalator's top")]

# ---------------------------------------------------------------- the world pieces (Data/tracks/noonday_creek_trail.json)
B = [(i, pts(b["footprint"])) for i, b in enumerate(m["buildings"])]
BBOX = {i: (min(p[0] for p in ring), max(p[0] for p in ring), min(p[1] for p in ring), max(p[1] for p in ring)) for i, ring in B}
def near_box(i, q, r): b = BBOX[i]; return b[0] - r < q[0] < b[1] + r and b[2] - r < q[1] < b[3] + r
def bld_dist(q): return min([poly_dist(q, ring) for i, ring in B if near_box(i, q, 12.0)] + [99.0])
# the bench: the profile's heights (not over a deck: the ground under each is cut away to its deck's underside by the map build), not on a road
# (Town Center Drive) or within 3 m of a building; narrower beside a stream (its edge 2.5 m from the stream's middle)
def over_deck(st): return any(d_["s0"] + 0.5 < st < d_["s1"] - 0.5 for d_ in TRAIL_DECKS)
g_pts, g_y, g_left = [], [], []
for k, q in enumerate(trail):
    if over_deck(tc_[k]) or road_dist(q) < 1.0 or bld_dist(q) < 3.0: continue
    sd = stream_dist(q); half = max(1.6, min(BENCH_HALF, sd - 2.5))
    g_pts.append(q); g_y.append(round(gy[k], 2)); g_left.append(round(half, 2))
def split_runs(p, y, hl):
    runs_, cur = [], [0]
    for k in range(1, len(p)):
        if math.dist(p[k], p[k - 1]) > 3.5: runs_.append(cur); cur = [k]
        else: cur.append(k)
    runs_.append(cur)
    return [r for r in runs_ if len(r) >= 3]
grades = []
for n_, r in enumerate(split_runs(g_pts, g_y, g_left)):
    grades.append({"id": f"the trail ({n_ + 1})", "points": flat([g_pts[k] for k in r]), "y": [g_y[k] for k in r], "half": BENCH_HALF, "blend": BENCH_BLEND,
                   "halves": [g_left[k] for k in r]})
# the east lot's way (build 3: a bank between Town Center Drive and the lot stopped the Trail Four): its ground smoothed along (a Gaussian of 8 m), not on a road
el = east_lot; elc = cum(el); elg = [ground(*q) for q in el]
ely = [gsm(elg, elc, k, 8.0) for k in range(len(el))]
el_k = [k for k in range(len(el)) if road_dist(el[k]) > 1.0 and bld_dist(el[k]) > 3.0]
for n_, r in enumerate(split_runs([el[k] for k in el_k], None, None)):
    grades.append({"id": f"the east lot's way ({n_ + 1})", "points": flat([el[el_k[k]] for k in r]), "y": [round(ely[el_k[k]], 2) for k in r], "half": 4.0, "blend": 3.0})
# the north lot's way down to Town Center Drive and on to the red bridge (not on the road itself), 4 m either side
nl_k = [k for k in range(len(nl)) if not (k_t0 - 1 <= k <= k_t1 + 1) and road_dist(nl[k]) > 1.0]
for n_, r in enumerate(split_runs([nl[k] for k in nl_k], None, None)):
    grades.append({"id": f"the north lot's way down ({n_ + 1})", "points": flat([nl[nl_k[k]] for k in r]), "y": [round(nly[nl_k[k]], 2) for k in r], "half": 4.0, "blend": 3.0})
print(f"the trail: {cum(trail)[-1]:.0f} m, {len(grades)} graded stretches; {capped} points held {DECK_CLEAR} m under I-575's decks; "
      f"the bench narrowed by a stream at {sum(1 for h in g_left if h < BENCH_HALF)} points")
# railings (black steel pipe, photo 1): both sides under I-575, not on the boardwalk (its own: MapBuilder.TrailRace.cs)
rails = []
for side, nm in ((1, "left"), (-1, "right")):
    run_ = []
    for k in range(len(trail)):
        on = (us0 - 4.0 <= tc_[k] <= us1 + 4.0) and not over_deck(tc_[k])
        if on: run_.append(k)
        elif run_:
            if len(run_) >= 6: rails.append({"id": f"under I-575 ({nm})", "side": side, "s0": round(tc_[run_[0]], 1), "s1": round(tc_[run_[-1]], 1)})
            run_ = []
    if run_ and len(run_) >= 6: rails.append({"id": f"under I-575 ({nm})", "side": side, "s0": round(tc_[run_[0]], 1), "s1": round(tc_[run_[-1]], 1)})
print(f"railings: {len(rails)} runs, {sum(r['s1'] - r['s0'] for r in rails):.0f} m")
# the yellow winding-road signs: on the path's right, 2.5 m past its edge, facing the way the race comes (forward), one each way at each bend
signs = []
for bx, bz in SIGN_BENDS:
    s0 = nearest_s(trail, (bx, bz))[1]
    for rev, ds in ((False, -45.0), (True, 45.0)):
        s = max(2.0, min(tc_[-1] - 2.0, s0 + ds)); p = point_at(trail, s, tc_); a = point_at(trail, s - 2.0, tc_); b = point_at(trail, s + 2.0, tc_)
        t = unit((b[0] - a[0], b[1] - a[1]));
        if rev: t = (-t[0], -t[1])
        n = (t[1], -t[0])   # the right of travel
        q = (p[0] + n[0] * (TRAIL_W / 2 + 2.5), p[1] + n[1] * (TRAIL_W / 2 + 2.5))
        signs.append({"at": [round(q[0], 2), round(q[1], 2)], "facing": [round(-t[0], 4), round(-t[1], 4)]})
clear = [{"id": "the trail", "points": flat(trail[::2] + [trail[-1]]), "half": CLEAR_HALF},
         {"id": "the trailhead", "points": flat(th[::2] + [th[-1]]), "half": 5.0},
         {"id": "the north lot", "points": flat(north_lot[::2] + [north_lot[-1]]), "half": 6.0},
         {"id": "the east lot", "points": flat(east_lot[::2] + [east_lot[-1]]), "half": 6.0}]
for nm_, ln_ in (("the east lot", east_lot), ("the north lot", north_lot), ("the trailhead", th)):
    near_ = min((bld_dist(q), q) for q in ln_ if not any(inside(q, pts(m["buildings"][i]["footprint"])) for i in (2389, 2391)))
    print(f"{nm_}: its line at least {near_[0]:.1f} m from a building (at ({near_[1][0]:.0f}, {near_[1][1]:.0f}))")
pole_free = [{"id": "the east lot", "points": flat(east_lot[::2] + [east_lot[-1]]), "half": 5.0},
             {"id": "the north lot", "points": flat(north_lot[::2] + [north_lot[-1]]), "half": 5.0}]
tread = {"line": flat(trail), "width": TRAIL_W, "decks": TRAIL_DECKS, "underpass": flat([UNDERPASS[0], UNDERPASS[-1]]),
         "rails": rails, "signs": signs, "tread_y": [round(v, 2) for v in gy]}
track = {"note": "Round 62: the Noonday Creek Trail race's world pieces, from Tools/mapgen/make_trail_route.py (MapBuilder.TrailRace.cs). Coordinates are map meters (= Unity x, z).",
         "big_jumps": [], "landing_zones": [], "clear_lines": clear, "break_lines": [], "grades": grades, "trail": tread, "pole_free": pole_free,
         "route_name": SETTINGS["name"]}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_trail_route.py from Data/map.json and raw/osm_ext1.json (OSM's Noonday Creek Trail). Coordinates are map meters (= Unity x, z); heights come from the world, "
              "the mall's floors from the map build (loop_y codes).",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "loop_y": [round(v, 3) for v in ys], "gate_s": 0.0, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": [{"osm_id": r.osm, "name": r.name, "reverse": False, "length_m": round(cum(r.p)[-1], 1)} for r in runs],
    "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": [], "ai_zones": ai_zones, "ai_widths": ai_widths, "reset_skip": reset_skip,
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "loop_rev_y": [round(v, 3) for v in ys_r], "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev,
    "junctions_rev": merged_rev, "closures_rev": closures_rev, "ai_zones_reverse": ai_zones_rev, "ai_widths_rev": ai_widths_rev, "reset_skip_rev": reset_skip_rev,
})
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    view = sys.argv[sys.argv.index("--plot") + 1].split(",") if len(sys.argv) > sys.argv.index("--plot") + 1 else None
    x0, x1, z0, z1 = [float(v) for v in view] if view else (-50, 1850, -1800, -350)
    fig, ax = plt.subplots(figsize=(18, 18 * (z1 - z0) / (x1 - x0)))
    for i, ring in B:
        if x0 < ring[0][0] < x1 and z0 < ring[0][1] < z1: ax.fill(*zip(*ring), color="#ccc")
    for r in roads:
        p = pts(r["points"])
        if any(x0 < x < x1 and z0 < z < z1 for x, z in p): ax.plot(*zip(*p), color="#999", lw=max(.4, r["width"] * 0.12))
    for st in streams:
        if any(x0 < x < x1 and z0 < z < z1 for x, z in st): ax.plot(*zip(*st), color="#39f", lw=1)
    ax.plot(*zip(*lap), "b-", lw=1.0); ax.plot(*zip(*lap_r), "c--", lw=0.6)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g", clip_on=True)
    for c in closures: ax.plot(c["x"], c["z"], "kx")
    for c in closures_rev: ax.plot(c["x"], c["z"], "mx")
    for sg in signs: ax.plot(*sg["at"], "y^")
    ax.plot(*lap[0], "ks"); ax.text(lap[0][0] + 5, lap[0][1], "gate", clip_on=True)
    ax.plot(*zip(*bridge_red), "r-", lw=3)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    os.makedirs(os.path.join(ROOT, "local/round62"), exist_ok=True)
    out = os.path.join(ROOT, "local/round62/layout.png" if not view else "local/round62/layout_zoom.png")
    plt.savefig(out, dpi=72, bbox_inches="tight"); print("picture:", os.path.relpath(out, ROOT))
