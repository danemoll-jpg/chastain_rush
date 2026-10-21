"""Round 52 (Dan, 2026-10-19 22:23): the "Addison Heights" race, the neighborhood south of New Chastain Road and west of Canton Road,
from Dan's red line on a map (local/reference_photos/embry_lane/r52_route_map.jpg: looked at only; the course is laid on OSM's roads from
Data/map.json). Forward and reverse, no shortcuts yet (Dan picks them once he has driven it). Forward, one lap:
  1. Joel Drive west from Hayworth Drive to its west end
  2. round the bend onto Embry Lane NE: north up its west leg, east along it, down its east end past Joel Drive's end to Suholden Circle NE
  3. Suholden Circle west to its bend, then south on it to Kelly Drive NE
  4. Kelly Drive west to Crawford Circle, north up Crawford Circle's east leg and Hayworth Drive back to Joel Drive: the lap
Reverse is the same roads the other way, a lap of its own (RaceRoute: loop_rev and the *_rev fields), as Chastain Lakes'.
Open roads ("traffic": true), two-way roads raced on their right-hand lane, as the other open-road races. Every road that leaves the lap is
barred in a race (closures, from the roads themselves).
World pieces (Data/tracks/addison_heights.json, read by MapBuilder.Round52.cs): one cleared line, no tree trunk within 1.5 m of the lap's road edge
(Round 52 check: AI drivers coming down Embry Lane's 30 % hill ran into trunks standing in the gutter and one missed a checkpoint).

Hand choices are the constants below; everything else is computed, so it re-runs after any mapgen run. It writes
Data/routes/addison_heights.json. Picture for checking (git-ignored, it shows houses): local/round52/layout.png with --plot.

Usage: python Tools/mapgen/make_addison_route.py [--plot]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/addison_heights.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/addison_heights.json")
EDGE_CLEAR_M = 1.5   # no trunk this close to the lap's road edge
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
# Round 53: the ground (the map build changes it a little near houses and graded lines; the jumps' own heights are taken in the build)
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
roads = m["roads"]; by_osm = {r["osm_id"]: r for r in roads}

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
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Addison Heights",      # Dan's name for the neighborhood (2026-10-19 22:38; not confirmed online, easy to change)
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
JOEL, EMBRY, EMBRY_E, SUHOLDEN, KELLY, CRAWFORD, HAYWORTH = 9122504, 9134327, 1360431992, 9133976, 330740879, 9133382, 9140536
J_HAY = (3012.0, 414.0)      # Joel Drive / Hayworth Drive
J_WEST = (2645.0, 423.0)     # Joel Drive's west end, Embry Lane's start
J_SUH = (3284.0, 300.0)      # Embry Lane's east end on Suholden Circle
J_KELLY = (3140.0, 97.0)     # Suholden Circle / Kelly Drive
J_CRAW = (2987.0, 120.0)     # Kelly Drive / Crawford Circle's east leg
J_CRAW_TOP = (2979.0, 305.0) # Crawford Circle's east leg / Hayworth Drive
# the start / finish: Kelly Drive's straight between Suholden Circle and Crawford Circle (155 m with no turn, a steady 5 to 8 % grade:
# Joel Drive's longer straight falls 40 % and more west of Hayworth Drive, no place for a grid). The forward lead-in (70 m) and the
# reverse grid (45 m before the line) both sit on the straight
GATE_AT = (3055.0, 110.0)
CHECKPOINTS = [
    (2700.0, 419.0, "Joel Drive"),
    (2740.0, 516.0, "Embry Lane"),               # past the bend: a cut from Joel Drive to Embry Lane through the yards misses it
    (2900.0, 525.0, "Embry Lane at Lyndon"),
    (3120.0, 530.0, "Embry Lane east"),
    (3255.0, 302.0, "Suholden Circle"),
    (3136.0, 190.0, "Suholden Circle south"),
    (2985.0, 225.0, "Crawford Circle"),
    (2999.0, 350.0, "Hayworth Drive"),
]

# ---------------------------------------------------------------- the lap
def way(osm, a=None, b=None):
    """The road from its point nearest a to its point nearest b (the road's own direction or against it)."""
    p = pts(by_osm[osm]["points"])
    sa = nearest_s(p, a)[1] if a else 0.0
    sb = nearest_s(p, b)[1] if b else cum(p)[-1]
    return cut(p, sa, sb) if sa <= sb else cut(p, sb, sa)[::-1]
SPEC = [("Joel Drive", JOEL, way(JOEL, J_HAY, J_WEST)),
        ("Embry Lane Northeast", EMBRY, way(EMBRY, J_WEST, None)),
        ("Embry Lane Northeast (east end)", EMBRY_E, way(EMBRY_E, None, J_SUH)),
        ("Suholden Circle Northeast", SUHOLDEN, way(SUHOLDEN, J_SUH, J_KELLY)),
        ("Kelly Drive Northeast", KELLY, way(KELLY, J_KELLY, J_CRAW)),
        ("Crawford Circle", CRAWFORD, way(CRAWFORD, J_CRAW, J_CRAW_TOP)),
        ("Hayworth Drive Northeast", HAYWORTH, way(HAYWORTH, J_CRAW_TOP, J_HAY))]
pieces, line, offs = [], [], []
for name, osm, seg in SPEC:
    if line and math.dist(line[-1], seg[0]) > 0.6: raise SystemExit(f"the lap breaks before {name} ({osm}): gap {math.dist(line[-1], seg[0]):.1f} m")
    seg = resample(seg, 3.0); off = by_osm[osm]["width"] / 4.0
    pieces.append({"osm_id": osm, "name": name, "reverse": False, "length_m": round(cum(seg)[-1], 1)})
    line += seg if not line else seg[1:]; offs += [off] * (len(seg) if not offs else len(seg) - 1)
if math.dist(line[0], line[-1]) > 0.6: raise SystemExit("the lap does not close: gap %.1f m" % math.dist(line[0], line[-1]))
line[-1] = line[0]; offs[-1] = offs[0]
N = len(line)
def lane(ln, of):
    """The lap's line set off to the right of the travel direction (the right-hand lane), the joins between pieces eased."""
    out = []
    for i in range(N):
        a, b = ln[max(0, i - 1)], ln[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
        out.append((ln[i][0] + t[1] / n * of[i], ln[i][1] - t[0] / n * of[i]))
    out[-1] = out[0]
    return out
lap = lane(line, offs)
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
print(f"lap {L_lap:.0f} m: " + ", ".join(f"{p['name']} {p['length_m']:.0f}" for p in pieces))
gate_s = round(S(GATE_AT), 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M) + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
def checkpoints(lp, Cl, L, g):
    out = []
    for x, z, label in CHECKPOINTS:
        s = round(nearest_s(lp, (x, z))[1], 1); p = point_at(lp, s, Cl); out.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
    out = sorted(out, key=lambda c: (c["s"] - g) % L)
    for i, c in enumerate(out, 1): c["id"] = i
    return out
cps = checkpoints(lap, C, L_lap, gate_s)
print(f"gate at s {gate_s} {[round(v) for v in point_at(lap, gate_s, C)]}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps))

# ---------------------------------------------------------------- closures (as the Chastain Lakes race's)
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
def closures_for(lap):
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
    for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(lap, j["s"], cum(lap))]
    return closures, stubs, merged
closures, stubs, merged = closures_for(lap)
print(f"closures (barriers while racing): {len(closures)}; dead-end stubs not barred: {len(stubs)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
for t in stubs: print(f"  stub (not barred): {t['road']} at {t['contact']}")

# ---------------------------------------------------------------- the reverse lap: the same roads the other way, its own right-hand lane
lap_r = lane(line[::-1], offs[::-1])
Cr = cum(lap_r); L_rev = Cr[-1]
gate_s_rev = round(nearest_s(lap_r, GATE_AT)[1], 1)
cps_rev = checkpoints(lap_r, Cr, L_rev, gate_s_rev)
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps_rev))
closures_rev, stubs_rev, merged_rev = closures_for(lap_r)
print(f"reverse closures: {len(closures_rev)}; dead-end stubs not barred: {len(stubs_rev)}")

# ================================================================ Round 53 (Dan, 2026-10-20 12:38 and 2026-10-21 01:07): two FORWARD shortcuts
# (reports BUG-001 to BUG-005 of session 2026-10-20_01-26-12-613_8b4dc1), a house (BUG-004, MapBuilder.Round53.cs) and the Lyndon / Embry junction
# (BUG-001, MapBuilder.Round53.cs). Both shortcuts are shut in a reverse race.
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def along(o, d, k): return (o[0] + d[0] * k, o[1] + d[1] * k)
def chaikin(p, it=3):
    for _ in range(it):
        q = [p[0]]
        for a, b in zip(p[:-1], p[1:]): q += [(a[0] * .75 + b[0] * .25, a[1] * .75 + b[1] * .25), (a[0] * .25 + b[0] * .75, a[1] * .25 + b[1] * .75)]
        p = q + [p[-1]]
    return p
def inside(q, ring):
    c = False
    for i in range(len(ring)):
        a, b = ring[i], ring[i - 1]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]: c = not c
    return c
def poly_dist(q, ring): return 0.0 if inside(q, ring) else min(seg_dist(q, ring[i], ring[(i + 1) % len(ring)]) for i in range(len(ring)))
RINGS = {i: pts(b["footprint"]) for i, b in enumerate(m["buildings"])}
def houses_near(path, r):
    out = {}
    for i, ring in RINGS.items():
        if not any(abs(ring[0][0] - q[0]) < 60 and abs(ring[0][1] - q[1]) < 60 for q in path[::4]): continue
        d = min(poly_dist(q, ring) for q in path)
        if d < r: out[i] = round(d, 1)
    return out
def forgiven(sa, sb): return [c["id"] for c in cps if (c["s"] - sa) % L_lap < (sb - sa) % L_lap]
LYNDON = 9148109
lyn = pts(by_osm[LYNDON]["points"])

# ---------------------------------------------------------------- A, THE LYNDON DRIVE SHORTCUT: THE ROAD CREW AND THE FRESH CONCRETE
# Dan (BUG-002, Lyndon Drive at Joel Drive, heading north): "constructions crews are here working on the road. A whole part of the road is all freshly
# poured concrete if you try to drive through it you are slowed like you would be if you were driving in water. A ramp ... a trailer that was for the
# roller and other equipent has its ramp down and there are boards leaned up against the truck so as to get some extra height so you can clear the
# concrete"; BUG-003 (Lyndon at Embry Lane): "shortcut merges back to the road here". The lap runs west down Joel Drive (a 15 to 40 % hill) and round the
# west end up to Embry Lane: off it right at Lyndon Drive, north up Lyndon (97 m, climbing 5 %) to Embry Lane.
# The launch (MapBuilder.Round53.cs): the roller's tilt-deck equipment trailer, its deck tipped down to the road (how the roller came off it), 2.6 m wide
# (its true width), and heavy planks leaned from the deck's front up against its front rack: a real jump (physics only) off the planks' top. The truck
# that hauls it stands unhitched on the lawn beside it (in front of the trailer it would stand in the concrete, and the planks would launch you into its
# cab). The concrete: a fresh slab across the whole road beyond it; vehicles on it are slowed as in water (WetConcrete) and leave ruts. Shrub beds in the yards on both sides (ShrubBeds) so the only fast way past the concrete is through the air.
A_FROM = (2830.0, 419.3)                  # on the lap (Joel Drive, westbound), where the turn into Lyndon starts
A_TURN = [(2816.5, 419.8), (2813.6, 424.0), (2813.0, 432.0)]
T_OFF = 0.9                               # the trailer's middle this far right (east) of Lyndon's middle: the flagger on its left, by the west curb
T_FOOT_Z = 449.0                          # the trailer's tail on the road (its foot)
T_DECK, T_DECK_ANG, T_PLANK, T_PLANK_ANG, T_W = 6.8, 9.5, 3.0, 22.0, 2.6
# Round 53 check, tuning try 1: 26 m of concrete (to z 487) cost a car driven round the trailer only 3 s (the shortcut saves 9 to 10 s), and a flat-out car left the
# planks at 26 to 28 m/s (44 to 51 m flights); try 2: 39.5 m (to 13 m short of Embry Lane: a clean run needs about 25 m/s off the planks) and a stickier slab
CONC_FROM, CONC_LEN = 2.5, 39.5           # the concrete: from this far past the lip, this long (the whole road wide)
A_MERGE = [(2811.6, 503.0), (2812.6, 511.0), (2817.0, 515.6)]
def lyn_x(z):
    a, b = lyn[2], lyn[3]; t = (z - a[1]) / (b[1] - a[1]); return a[0] + t * (b[0] - a[0])
dL = unit((lyn[3][0] - lyn[2][0], lyn[3][1] - lyn[2][1])); nL = (-dL[1], dL[0])   # nL: the left (west) of a northbound driver
T_FOOT = (lyn_x(T_FOOT_Z) - nL[0] * T_OFF, T_FOOT_Z)
T_LEN = T_DECK + T_PLANK
T_LIP = along(T_FOOT, dL, T_LEN)
t_rise = T_DECK * math.tan(math.radians(T_DECK_ANG)) + T_PLANK * math.tan(math.radians(T_PLANK_ANG))
t_foot_y = ground(*T_FOOT); t_lip_y = t_foot_y + t_rise
def a_flight(v, max_t=6.0):
    a = math.radians(T_PLANK_ANG); vx, vy = v * math.cos(a), v * math.sin(a); t = 0.0
    while t < max_t:
        t += 0.01; q = along(T_LIP, dL, vx * t); y = t_lip_y + vy * t - 4.905 * t * t
        if t > 0.15 and y <= ground(*q) + 0.05: return {"t": round(t, 2), "dist": round(vx * t, 1), "at": (round(q[0], 1), round(q[1], 1))}
    return None
c_mid = lambda k: (lyn_x(T_LIP[1] + dL[1] * k), T_LIP[1] + dL[1] * k)
CONC = [c_mid(CONC_FROM), c_mid(CONC_FROM + CONC_LEN)]
FLAGGER = along(along(T_FOOT, dL, -2.0), nL, T_W / 2 + 1.9)
print(f"A trailer: foot ({T_FOOT[0]:.1f}, {T_FOOT[1]:.1f}) {t_foot_y:.2f} m, lip ({T_LIP[0]:.1f}, {T_LIP[1]:.1f}) {t_lip_y:.2f} m ({t_rise:.2f} m up), {T_W} m wide, "
      f"deck {T_DECK} m at {T_DECK_ANG} deg, planks {T_PLANK} m at {T_PLANK_ANG} deg; concrete z {CONC[0][1]:.0f} to {CONC[1][1]:.0f}; the flagger at ({FLAGGER[0]:.1f}, {FLAGGER[1]:.1f})")
for v in (12.0, 14.0, 16.0, 18.0, 20.0, 22.0, 24.0, 27.0):
    f = a_flight(v); print(f"    off the planks at {v:.0f} m/s: {f['t']:.2f} s, {f['dist']:.0f} m, down at z {f['at'][1]:.0f}{' IN THE CONCRETE' if f['at'][1] < CONC[1][1] else ''}")
s_a0 = S(A_FROM); s_a1 = S(A_MERGE[-1]) + 12.0
a_pre = resample(chaikin([point_at(lap, s_a0, C)] + A_TURN + [(lyn_x(440.0) - nL[0] * T_OFF, 440.0)], 3) + [along(T_FOOT, dL, -3.0), T_FOOT], 2.0)
a_ramp = [along(T_FOOT, dL, k) for k in range(2, int(T_LEN) + 1, 2)] + [T_LIP]
a_fl = [along(T_LIP, dL, k) for k in range(2, 34, 2)]
a_post = resample(chaikin([along(T_LIP, dL, 34.0)] + A_MERGE + [point_at(lap, s_a1, C)], 3), 2.0)
apath = a_pre + a_ramp + a_fl + a_post
def ramp_y(k):
    k = max(0.0, min(T_LEN, k)); return t_foot_y + (k * math.tan(math.radians(T_DECK_ANG)) if k <= T_DECK else T_DECK * math.tan(math.radians(T_DECK_ANG)) + (k - T_DECK) * math.tan(math.radians(T_PLANK_ANG)))
def arc_y(L, y0, ang, v, q): vx, vy = v * math.cos(math.radians(ang)), v * math.sin(math.radians(ang)); u = math.dist(L, q) / vx; return round(max(ground(*q), y0 + vy * u - 4.905 * u * u), 2)
apath_y = [round(ground(*q), 2) for q in a_pre] + [round(ramp_y(math.dist(T_FOOT, q)), 2) for q in a_ramp] + [arc_y(T_LIP, t_lip_y, T_PLANK_ANG, 23.0, q) for q in a_fl] + [round(ground(*q), 2) for q in a_post]
sAFoot, sALip = nearest_s(apath, T_FOOT)[1], nearest_s(apath, T_LIP)[1]; sAEnd = cum(apath)[-1]
fg_a = forgiven(s_a0 + 16.0, s_a1)
print(f"A: leaves the lap at s {s_a0:.0f}, rejoins at s {s_a1:.0f}: {sAEnd:.0f} m against {(s_a1 - s_a0) % L_lap:.0f} m on the lap; forgives CP {fg_a}; houses by its line {houses_near(apath, 8.0)}")
# the yards beside the concrete: shrub beds from each curb to the houses, and across the side yards between them (houses 1160 / 825 west,
# 1134 / 1027 east); a bed's drag works on the ground only (never on the road: the beds stop at the curb)
lyx = lambda z: lyn_x(z)
# Round 53 check, tuning try 1: beds from z 452 to 500 let a car through the west yards 0.7 s faster than the lap; try 2: from z 436 to 506
A_BEDS = [("A Lyndon's west yards", [(lyx(436.0) - 10.6, 436.0), (lyx(506.0) - 10.6, 506.0)], 6.3),
          ("A between 1160 and 825", [(2797.0, 470.0), (2776.0, 470.0)], 8.6),
          ("A Lyndon's east yards", [(lyx(446.0) + 12.0, 446.0), (lyx(506.0) + 12.0, 506.0)], 7.6),
          ("A between 1134 and 1027", [(2830.0, 464.0), (2852.0, 464.0)], 6.8)]
A_LAND = {"id": "LY", "poly": [round(v, 2) for q in [along(c_mid(CONC_FROM + CONC_LEN - 1.0), nL, 4.4), along(c_mid(CONC_FROM + CONC_LEN + 11.0), nL, 4.4),
                                                       along(c_mid(CONC_FROM + CONC_LEN + 11.0), nL, -4.4), along(c_mid(CONC_FROM + CONC_LEN - 1.0), nL, -4.4)] for v in q]}

# ---------------------------------------------------------------- B, THE ELAINE CIRCLE JUMP: NOT BUILT (Round 53 status in TODO.md)
# Dan (BUG-005): the crest at Suholden / Elaine, a natural jump down Elaine Circle, with a longer way back to the lap after it. Built and driven in
# the exe in Round 53 (a graded way through the woods from Elaine's valley to Kelly Drive), then taken out: the start / finish is on Kelly Drive,
# so the way back has to rejoin Kelly before the line, and Suholden Circle (the lap) is already the straight way from the crest to Kelly: the way
# was as long as the lap it left (330 m against 332 m) and slower in the exe (Drifter Twin 13.7 s against 12.0 s). Elaine Circle stays barred at
# the corner. Options for Dan in TODO.md.
for nm, ln, h in A_BEDS:
    pathd = min(seg_dist(q, a, b) for q in apath for a, b in zip(ln[:-1], ln[1:])) - h
    print(f"  bed {nm}: {cum(ln)[-1]:.0f} m long, {2 * h:.0f} m wide; its edge {pathd:.1f} m from the shortcut's line")

shortcuts = [
    {"id": "f_lyndon_crew", "name": "Lyndon road crew", "direction": "forward", "path": flat(apath), "path_y": apath_y, "half_width_m": 3.5,
     "entry_inset_m": round(nearest_s(apath, (lyn_x(436.0), 436.0))[1], 1), "entry_margin_m": 1.5, "forgives": fg_a, "ai_speed": 34.0,
     # into Lyndon at the corner's speed, flat out up the trailer (it needs about 20 m/s off the planks to clear the concrete), slow for the turn onto Embry
     "ai_profile": [round(v, 1) for v in (0.0, 13.0, 22.0, 13.0, sAFoot - 14.0, 34.0, sALip, 34.0, sALip + 30.0, 22.0, sAEnd - 14.0, 12.0, sAEnd, 16.0)],
     # a sloppy try: eased off into the trailer (short, into the concrete)
     "ai_slip": 0.2, "ai_slip_profile": [round(v, 1) for v in (0.0, 13.0, 22.0, 11.0, sAFoot - 14.0, 14.0, sALip, 14.0, sALip + 30.0, 14.0, sAEnd - 14.0, 12.0, sAEnd, 16.0)]},
]
# the barriers: in a forward race Lyndon Drive between Joel Drive and Embry Lane is open (its two barriers go); in a reverse race they stay
def bar(label, at, d, width, Sf, note):
    n = math.hypot(*d); return {"road": label, "osm_id": 0, "x": round(at[0], 2), "z": round(at[1], 2), "dx": round(d[0] / n, 4), "dz": round(d[1] / n, 4), "width_m": round(width, 1),
                                "junction_s": round(Sf(at), 1), "contact": [round(at[0], 1), round(at[1], 1)], "arc_m": 0.0, "barrier_on": label, "label": f"{label} ({note})"}
dropped = [c for c in closures if c["osm_id"] == LYNDON and 425.0 < c["z"] < 510.0]
closures = [c for c in closures if c not in dropped]
for lst in (closures,):
    lst.sort(key=lambda c: c["junction_s"])
    for i, c in enumerate(lst, 1): c["id"] = f"B{i}"; c["label"] = c["label"] if "(" in c["label"] else f"{c['road']} at lap {c['junction_s']:.0f} m"
print(f"forward barriers: {len(closures)} (dropped: {', '.join(c['road'] + ' at ' + str(c['contact']) for c in dropped)}); reverse: {len(closures_rev)}")

route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_addison_route.py from Data/map.json. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": pieces, "official_shortcuts": shortcuts, "closures": closures,
    "closure_stubs_not_barred": stubs, "ai_zones": [],
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev, "junctions_rev": merged_rev,
    "closures_rev": closures_rev, "ai_zones_reverse": [],
})
if os.path.exists(OUT_ROUTE):
    try: route["direction"] = json.load(open(OUT_ROUTE, encoding="utf-8"))["direction"]
    except Exception: pass
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
# the cleared line: the roads' centerline (both lanes), half the road's width plus EDGE_CLEAR_M
centre = resample(line, 6.0)
track = {"note": "Round 52 / 53: the Addison Heights race's world pieces, from Tools/mapgen/make_addison_route.py (MapBuilder.Round52.cs, MapBuilder.Round53.cs). Coordinates are map meters (= Unity x, z).",
         "clear_lines": [{"id": "the lap's road edges", "points": flat(centre), "half": round(max(by_osm[o]["width"] for _, o, _ in SPEC) / 2.0 + EDGE_CLEAR_M, 2)}]}
# Round 53: the shortcut's world pieces (MapBuilder.Round53.cs; the landing zone: the Ridge shape, AddisonNow)
track["landing_zones"] = [A_LAND]
track["shrub_beds"] = [{"id": n_, "points": flat(l_), "half": h_} for n_, l_, h_ in A_BEDS]
track["crew"] = {"foot": [round(T_FOOT[0], 2), round(T_FOOT[1], 2)], "dir": [round(dL[0], 5), round(dL[1], 5)], "deck": T_DECK, "deck_angle_deg": T_DECK_ANG,
                 "plank": T_PLANK, "plank_angle_deg": T_PLANK_ANG, "width": T_W,
                 "concrete": flat([along(c_mid(CONC_FROM), nL, 4.5), along(c_mid(CONC_FROM + CONC_LEN), nL, 4.5), along(c_mid(CONC_FROM + CONC_LEN), nL, -4.5), along(c_mid(CONC_FROM), nL, -4.5)]),
                 "flagger": [round(FLAGGER[0], 2), round(FLAGGER[1], 2)],
                 # Lyndon Drive between Joel Drive and Embry Lane is out of the traffic's and the police's road graph (the crew has it)
                 "closed_road": {"osm_id": LYNDON, "from": list(lyn[2]), "to": list(lyn[3])}}
track["route_name"] = SETTINGS["name"]
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    x0, x1, z0, z1 = 2560, 3360, 0, 620
    fig, ax = plt.subplots(figsize=(16, 16 * (z1 - z0) / (x1 - x0)))
    for i, b in enumerate(m["buildings"]):
        ring = pts(b["footprint"])
        if x0 < ring[0][0] < x1 and z0 < ring[0][1] < z1: ax.fill(*zip(*ring), color="#c96"); ax.text(ring[0][0], ring[0][1], str(i), fontsize=6, clip_on=True)
    for r in roads:
        p = pts(r["points"])
        if any(x0 < x < x1 and z0 < z < z1 for x, z in p): ax.plot(*zip(*p), color="#999", lw=r["width"] * 0.6); ax.text(*p[len(p) // 2], r["name"] or "", fontsize=7, clip_on=True)
    ax.plot(*zip(*lap), "b-", lw=1.0); ax.plot(*zip(*lap_r), "c--", lw=0.6)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g", clip_on=True)
    for c in closures: ax.plot(c["x"], c["z"], "kx"); ax.text(c["x"] + 3, c["z"] + 3, c["id"], fontsize=7, clip_on=True)
    gp_ = point_at(lap, gate_s, C); ax.plot(*gp_, "ks"); ax.text(gp_[0] + 5, gp_[1], "gate", clip_on=True)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    os.makedirs(os.path.join(ROOT, "local/round52"), exist_ok=True)
    out = os.path.join(ROOT, "local/round52/layout.png")
    plt.savefig(out, dpi=80, bbox_inches="tight"); print("picture:", os.path.relpath(out, ROOT))
