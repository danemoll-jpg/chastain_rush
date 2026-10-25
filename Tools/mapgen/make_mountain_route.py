"""Round 67 (A, Dan, 2026-10-25 03:01: "I want one with a lot of air so I am wondering if we could do the trails of Kennesaw mountain. I want huge jumps
on this"): the "Kennesaw Mountain" race on Kennesaw Mountain National Battlefield Park's real road and trail (OSM, map.json: road 4729 Kennesaw Mountain
Drive, trail 30 the Kennesaw Mountain Trail, both from Round 61's stage 2 of the map).
One lap, FORWARD only (the downhill-on-trails way, Dan's (4); see REVERSE below):
  1. Kennesaw Mountain Drive from the visitor center up the mountain's east and south flanks, round the saddle with Little Kennesaw, to the summit lot
     (2.3 km, about 10 % up: the climb is on the road)
  2. across the summit lot to the overlook at its north-east corner
  3. DOWN the Kennesaw Mountain Trail (1.2 km, 240 m down, about 20 % and more in places) to the visitor center, back onto the drive
The trail is raced on a bench graded level across 12 m (the path itself at its real 1.5 m width, drawn by the map build as before; Dan: vehicles also
drive in the grass), the woods cleared 8 m either side and breakable to 15 m (a racer who runs wide snaps trees, never stops dead).
The jumps (real physics jumps: the ground itself shaped, no aimed launch; each lands on the trail downhill, heading the race's way):
  J1  the summit overlook   a granite rock face at the lot's corner, off it down the ridge
  J2  the ridge drop        a huge fallen oak across the trail where the line cuts the first switchback straight down the ridge
  J3  the earthworks berm   an old Civil War earthwork the trail crosses on the steep east leg (logs along its face)
  J4  the switchback cut    off the flat leg's end straight down the hillside onto the next leg (a boulder ledge), cutting the zig-zag
  J5  the washout           the old timber trail bridge over a wash, washed out: its near abutment the lip, the wash dug across the trail
  J6  the last ledge        a boulder ledge on the long straight down to the flats
REVERSE: not offered. Up the trail is a 20 to 30 % climb with nothing to fly off; Dan decides (TODO.md).
No shortcuts (Dan picks them after driving it). Closed roads ("traffic": false): the park's drive.

Hand choices are the constants below; everything else is computed (re-run after mapgen; it reads Assets/Generated/terrain_full.r16). It writes:
  Data/routes/kennesaw_mountain.json   the race (RaceRoute.cs format)
  Data/tracks/kennesaw_mountain.json   what the map build makes (MapBuilder.Mountain.cs): the bench graded with the jumps in its profile, cleared and
                                       breakable lines, landing zones, the jumps' looks
Picture for checking (git-ignored): local/round67/layout.png, local/round67/profile.png with --plot. The jumps' flights at 15 to 40 m/s: --flights.

Usage: python Tools/mapgen/make_mountain_route.py [--plot] [--flights]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/kennesaw_mountain.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/kennesaw_mountain.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]; trails = m["trails"]

def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def pts(flat_): return [(flat_[i], flat_[i + 1]) for i in range(0, len(flat_), 2)]
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
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def bearing(a, b): return math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360
def gsm(vals, cv, k, sig, reach=None):
    reach = reach or int(3 * sig / 1.0) + 2
    j0, j1 = max(0, k - reach), min(len(vals), k + reach + 1)
    w = [math.exp(-((cv[j] - cv[k]) / sig) ** 2 / 2) for j in range(j0, j1)]
    return sum(vals[j] * w[j - j0] for j in range(j0, j1)) / sum(w)
def herm(y0, g0, y1, g1, L, t):
    t2, t3 = t * t, t * t * t
    return (2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * L * g0 + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * L * g1
CODE_GROUND = -10000.0

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Kennesaw Mountain",
    "direction": "forward",
    "directions": ["forward"],
    "laps": 2,
    "traffic": False,
    "gate_half_width_m": 12.0,
    "checkpoint_half_width_m": 12.0,
    "gate_below_m": 6.0,
    "gate_above_m": 40.0,              # (a car flying a jump over a checkpoint still crosses it)
    "reverse_grid_back_m": 45.0,
    "ai_hill_targets": [22.0, 26.0, 30.0],   # the AI on the trail's 20 to 30 % grades (RoadDriver: over 14 %)
    "ai_crest_g": [30.0, 40.0, 50.0],         # the jumps are taken, not driven round (RoadDriver's crest rule would brake for every lip)
}
ROAD = 4729                        # Kennesaw Mountain Drive, the visitor center (s 0) to the summit lot (its end)
TRAIL = 30                         # the Kennesaw Mountain Trail, the summit lot (its start) to the visitor center
LEAD_IN_M = 70.0
GATE_AT = (-830.0, -5190.0)        # on the drive east of the visitor center, its grid on the straight before it
TRAIL_DROP = {36, 37, 38, 39, 40, 41, 42, 43,   # J4: the zig-zag below the flat leg, cut straight down the hillside
              5, 7, 8, 9, 10,                  # J2: the first switchback, cut straight down the ridge (6 lies on the straight)
              16, 17, 18}                      # the little jog on the ridge (a step in OSM's line)
TRAIL_END = 71                     # the trail's point where the lap turns right onto the drive (it crosses the drive at 74)
LOT = [(-925.0, -5724.0), (-903.0, -5716.0), (-882.0, -5707.0), (-866.0, -5699.0)]   # across the summit lot from the drive's end to the trail
BENCH_HALF, BENCH_BLEND = 6.0, 4.0  # the trail's bench: 12 m level across (Dan: room to race and pass), blending back over 4 m
CLEAR_HALF = 10.0                   # no tree within this of the line on the trail (the woods solid past BREAK_HALF)
BREAK_HALF = 25.0                   # trees within this of the line snap when hit (the second build's check: long flights run on past the bends)
HAIRPIN_HALF = 9.0                  # the bench this wide (half) through the hairpins (the second build's check: cars running wide met the cut bank)
# the jumps: (id, look, name, the lip at the line's nearest point to (x, z) (moved 'along' m on), the lip's angle over the horizontal, its height over the
# smoothed ground, the kicker's length, how far the ground is dug down past the lip (a gap: depth, length), the landing window (m past the lip))
JUMPS = [
    ("J1", "overlook", "the summit overlook", (-858.0, -5695.0), 12.0, 10.0, 1.8, 11.0, (0.0, 0.0), (12.0, 60.0)),
    ("J2", "oak", "the ridge drop", (-809.1, -5603.0), 0.0, 12.0, 2.2, 14.0, (0.0, 0.0), (14.0, 75.0)),
    ("J3", "berm", "the earthworks berm", (-589.6, -5509.2), 2.0, 12.0, 2.2, 14.0, (0.0, 0.0), (12.0, 60.0)),
    ("J4", "ledge", "the switchback cut", (-650.0, -5394.0), 14.0, 10.0, 2.0, 12.0, (0.0, 0.0), (12.0, 70.0)),
    ("J5", "washout", "the washout", (-719.5, -5289.1), 0.0, 12.0, 2.0, 13.0, (2.6, 9.0), (14.0, 70.0)),
    ("J6", "ledge2", "the last ledge", (-818.3, -5281.1), 0.0, 12.0, 2.0, 12.0, (0.0, 0.0), (12.0, 65.0)),
]
APPROACH_M, APPROACH_SIGMA = 45.0, 12.0   # the 45 m before each kicker's foot smoothed harder (a Gaussian of 12 m): no crest there throws a car before the lip
AI_JUMP_SPEED = {"J1": 20.0, "J2": 24.0, "J3": 22.0, "J4": 21.0, "J5": 23.0, "J6": 24.0}   # the AI's speed up to each lip
CHECKPOINTS = [   # (x, z, label) in lap order from the gate
    (-470.0, -5333.0, "Kennesaw Mountain Drive"),
    (-556.0, -5640.0, "the east flank"),
    (-1076.0, -6088.0, "the south flank"),
    (-1110.0, -5859.0, "the saddle road"),
    (-890.0, -5713.0, "the summit lot"),
    (-680.0, -5553.0, "the ridge"),
    (-541.6, -5520.4, "the east leg"),
    (-600.0, -5428.0, "the flat"),
    (-760.8, -5280.6, "the long drop"),
    (-939.0, -5256.0, "the visitor center"),
]

# ---------------------------------------------------------------- the line
road = pts(roads[ROAD]["points"]); rc = cum(road)
trail = pts(trails[TRAIL]["points"])
tr_keep = [q for i, q in enumerate(trail[:TRAIL_END + 1]) if i not in TRAIL_DROP]
# onto the drive: from the trail's last kept point a right turn onto the drive 30 m past where the trail crosses it, eastbound
cross_s = nearest_s(road, trail[74])[1]
JOIN_S = cross_s + 28.0
join_pts = [trail[TRAIL_END], ((trail[TRAIL_END][0] + trail[73][0]) / 2, (trail[TRAIL_END][1] + trail[73][1]) / 2 + 2.0), point_at(road, cross_s + 6.0, rc), point_at(road, JOIN_S, rc)]
road_lap = cut(road, JOIN_S, None)
# the trail run: from the lot's end to the drive, smoothed (corners rounded: chaikin), at 2 m
trail_run = resample(chaikin(LOT[-2:] + tr_keep + join_pts[1:], 3), 2.0)
lot_run = resample(chaikin([road[-1]] + LOT[:-1] + [trail_run[0]], 2), 2.0)
road_run = resample(road_lap, 3.0)
line = road_run + lot_run[1:] + trail_run[1:]
gap = math.dist(line[-1], line[0])
if gap > 4.0: raise SystemExit(f"the lap does not close: {gap:.1f} m")
line = line[:-1] + [line[0]]
kind = ["road"] * len(road_run) + ["lot"] * (len(lot_run) - 1) + ["trail"] * (len(trail_run) - 1)
# the lap starts at the gate
C0 = cum(line); g_s = nearest_s(line, GATE_AT)[1]; k0 = min(range(len(line)), key=lambda k: abs(C0[k] - g_s))
def rotate(a, k): return a[k:-1] + a[:k] + [a[k]]
lap = rotate(line, k0); kind = rotate(kind, k0)
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
t_k = [k for k in range(len(lap)) if kind[k] == "trail"]
s_trail0, s_trail1 = C[t_k[0]], C[t_k[-1]]
print(f"lap {L_lap:.0f} m: the drive {cum(road_run)[-1]:.0f} m (from its {JOIN_S:.0f} m to its end), the summit lot {cum(lot_run)[-1]:.0f} m, the trail {cum(trail_run)[-1]:.0f} m "
      f"(lap {s_trail0:.0f} to {s_trail1:.0f} m)")

# ---------------------------------------------------------------- the trail's profile: the ground along the line smoothed, the jumps shaped into it
tg = [ground(*q) for q in lap]
prof = list(tg)
ks = [k for k in range(len(lap)) if s_trail0 - 20 <= C[k] <= s_trail1 + 20]
sm = {k: gsm(tg, C, k, 5.0) for k in ks}
for k in ks: prof[k] = sm[k]
def prof_at(s):
    s = max(0.0, min(L_lap, s)); i = max(0, min(len(C) - 2, int(np.searchsorted(C, s)) - 1))
    t = (s - C[i]) / max(C[i + 1] - C[i], 1e-9); return prof[i] + t * (prof[i + 1] - prof[i])
# the approach to each kicker smoothed harder (the first build's check: at 26 m/s and more the slope's own crests threw cars before the lips)
for (jid, look, name, at, along, ang, hlip, Lk, dig, land) in JUMPS:
    sL = S(at) + along; a0, a1 = sL - Lk - APPROACH_M, sL - Lk
    kk = [k for k in ks if a0 - 10 <= C[k] <= a1]
    hard = {k: gsm(tg, C, k, APPROACH_SIGMA, 60) for k in kk}
    for k in kk:
        w = min(1.0, max(0.0, (C[k] - (a0 - 10)) / 10.0)); prof[k] = prof[k] + w * (hard[k] - prof[k])
base = list(prof)
def base_at(s):
    s = max(0.0, min(L_lap, s)); i = max(0, min(len(C) - 2, int(np.searchsorted(C, s)) - 1))
    t = (s - C[i]) / max(C[i + 1] - C[i], 1e-9); return base[i] + t * (base[i + 1] - base[i])
def grade_at(s, f=base_at, d=3.0): return (f(s + d) - f(s - d)) / (2 * d)
jumps = []
for (jid, look, name, at, along, ang, hlip, Lk, dig, land) in JUMPS:
    sL = S(at) + along
    yL = base_at(sL) + hlip; g0 = grade_at(sL - Lk); y0 = base_at(sL - Lk); k_ = math.tan(math.radians(ang))
    # the kicker: a Hermite from the ground's grade at its foot to the lip's angle at the lip
    for k in ks:
        s = C[k]
        if sL - Lk <= s <= sL: prof[k] = herm(y0, g0, yL, k_, Lk, (s - (sL - Lk)) / Lk)
    # past the lip: its back face at 35 degrees down to the ground (or to the dug wash), the wash (if any) a rounded trough, its far bank back to the ground
    dd, dl = dig
    back = math.tan(math.radians(35.0))
    for k in ks:
        s = C[k] - sL
        if 0 < s <= 30.0:
            y = yL - back * s
            floor = base_at(C[k])
            if dd > 0 and s <= dl + 6.0:   # the wash: down dd m over its length, its far side rising back over 6 m (about 25 degrees)
                if s <= dl: floor -= dd * math.sin(math.pi * min(1.0, s / dl) * 0.5) ** 0.5 if s < dl * 0.5 else dd
                else: floor -= dd * (1 - (s - dl) / 6.0) ** 2
            prof[k] = max(min(prof[k], floor), y) if y > floor else floor
    jumps.append(dict(id=jid, look=look, name=name, sL=sL, yL=yL, ang=ang, hlip=hlip, Lk=Lk, dig=dig, land=land))
# and no crest on the 70 m before a kicker's foot or on the kicker itself sharper than APPROACH_CREST_R (cut down, never filled: a car stays on the ground there below 30 m/s)
APPROACH_CREST_R = 92.0
for (jid, look, name, at, along, ang, hlip, Lk, dig, land) in JUMPS:
    sL = S(at) + along; kk = [k for k in ks if sL - Lk - 70 <= C[k] <= sL - 2.5]
    for _ in range(3000):
        for k in kk[1:-1]:
            h1, h2 = C[k] - C[k - 1], C[k + 1] - C[k]
            avg = (prof[k - 1] * h2 + prof[k + 1] * h1) / (h1 + h2)
            prof[k] = min(prof[k], avg + h1 * h2 / (2 * APPROACH_CREST_R))
# a little rounding of every corner of the profile (the back faces' feet, the wash): a Gaussian of 1 m, never over the lips
p2 = list(prof)
for k in ks:
    if any(-0.5 < C[k] - j["sL"] < 0.5 or j["sL"] - j["Lk"] <= C[k] <= j["sL"] for j in jumps): continue
    p2[k] = gsm(prof, C, k, 1.0, 4)
prof = p2

# ---------------------------------------------------------------- the flights (vertical plane along the line, straight)
def flight(j, v):
    a = math.radians(j["ang"]); vx, vy = v * math.cos(a), v * math.sin(a); t = 0.0; dt = 0.01; top = 0.0
    while t < 8.0:
        t += dt; x = vx * t; y = j["yL"] + vy * t - 4.905 * t * t; top = max(top, y - j["yL"])
        if x > 2.0 and y <= prof_at(j["sL"] + x) + 0.35:
            vyl = vy - 9.81 * t; slope = grade_at(j["sL"] + x, prof_at, 1.5)
            rel = math.degrees(math.atan2(-vyl, vx)) - math.degrees(math.atan(-slope))
            return t, x, top, rel
    return t, None, top, None
print("the jumps (lip station, height, angle; flights at 15 to 40 m/s: air s / distance m / up m / landing angle against the ground's deg):")
for j in jumps:
    k = min(range(len(lap)), key=lambda k: abs(C[k] - j["sL"]))
    j["lip"] = lap[k]; j["dir"] = unit((lap[min(k + 2, len(lap) - 1)][0] - lap[max(k - 2, 0)][0], lap[min(k + 2, len(lap) - 1)][1] - lap[max(k - 2, 0)][1]))
    fl = [(v,) + flight(j, v) for v in (15, 20, 25, 30, 35, 40)]
    j["flights"] = fl
    print(f"  {j['id']} {j['name']:22} lap {j['sL']:.0f} m at ({j['lip'][0]:.0f}, {j['lip'][1]:.0f}) {j['yL']:.1f} m, {j['ang']:.0f} deg, {j['hlip']:.1f} m over the ground, ground grade there "
          f"{grade_at(j['sL']) * 100:.0f} %")
    print("      " + "; ".join(f"{v}: {t:.1f} s {x if x is None else round(x)} m +{top:.1f} {'' if rel is None else round(rel)}" for v, t, x, top, rel in fl))
    # the sharpest crest on the 70 m before the kicker (a radius under v^2 / g throws a car at v)
    cr = []
    for k in ks:
        if j["sL"] - j["Lk"] - 70 <= C[k] <= j["sL"] - j["Lk"] and 0 < k < len(lap) - 1:
            g1 = (prof[k] - prof[k - 1]) / max(C[k] - C[k - 1], 1e-6); g2 = (prof[k + 1] - prof[k]) / max(C[k + 1] - C[k], 1e-6); cu = (g2 - g1) / max(C[k + 1] - C[k - 1], 1e-6) * 2
            if cu < 0: cr.append((-1 / cu, C[k] - j["sL"]))
    if cr: rmin = min(cr); print(f"      the approach's sharpest crest: radius {rmin[0]:.0f} m at {rmin[1]:.0f} m (throws a car over {math.sqrt(9.81 * rmin[0]):.0f} m/s)")
    # how straight the line runs past the lip (for a flight's landing)
    bs = [bearing(point_at(lap, j["sL"] + d - 2, C), point_at(lap, j["sL"] + d + 2, C)) for d in range(0, int(j["land"][1]) + 1, 5)]
    b0 = bs[0]; dev = max(abs((b - b0 + 180) % 360 - 180) for b in bs)
    print(f"      the line turns up to {dev:.0f} deg within {j['land'][1]:.0f} m past the lip")

# ---------------------------------------------------------------- checkpoints
cps = []
for x, z, label in CHECKPOINTS:
    s = round(S((x, z)), 1); p = point_at(lap, s, C); cps.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
cps.sort(key=lambda c: c["s"])
for i, c in enumerate(cps, 1): c["id"] = i
print("checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps))

# ---------------------------------------------------------------- closures: the drive west of the join (to the visitor center's drive and Old Mountain Road)
closures = []
p0 = point_at(road, JOIN_S - 22.0, rc); p1 = point_at(road, JOIN_S - 20.0, rc); t_ = unit((p0[0] - p1[0], p0[1] - p1[1]))
closures.append({"road": "Kennesaw Mountain Drive", "osm_id": roads[ROAD]["osm_id"], "x": round(p0[0], 2), "z": round(p0[1], 2), "dx": round(t_[0], 4), "dz": round(t_[1], 4),
                 "width_m": roads[ROAD]["width"], "junction_s": round(S(point_at(road, JOIN_S, rc)), 1), "contact": [round(p1[0], 1), round(p1[1], 1)], "arc_m": 20.0,
                 "barrier_on": "Kennesaw Mountain Drive", "id": "B1", "label": "Kennesaw Mountain Drive to the visitor center"})
junctions = [{"s": c["junction_s"], "roads": [c["road"]], "pos": c["contact"]} for c in closures]

# ---------------------------------------------------------------- the AI: held speeds (lap stations)
zones = []
def Z(id_, s0, s1, speed): zones.append({"id": id_, "s0": round(min(s0, s1), 1), "s1": round(max(s0, s1), 1), "speed": speed})
s_lot0 = S(LOT[0])
Z("the summit lot", s_lot0 - 10, s_trail0 + 4, 14.0)
for j in jumps:
    Z(f"{j['id']} {j['name']}", j["sL"] - j["Lk"] - 18.0, j["sL"] + 1.0, AI_JUMP_SPEED[j["id"]])
# the hairpins and the tight bends (from the line's own curvature: a bend of more than 50 degrees over 20 m)
for k in t_k[5:-5]:
    s = C[k]
    if any(j["sL"] - j["Lk"] - 20 <= s <= j["sL"] + 2 for j in jumps): continue
    turn = abs((bearing(point_at(lap, s, C), point_at(lap, s + 10, C)) - bearing(point_at(lap, s - 10, C), point_at(lap, s, C)) + 180) % 360 - 180)
    if turn > 40:
        if zones and zones[-1]["id"].startswith("bend") and s - zones[-1]["s1"] < 8: zones[-1]["s1"] = round(s + 6, 1); continue
        Z(f"bend at lap {s:.0f} m", s - 6, s + 6, 9.0 if turn > 70 else 12.0)
Z("onto the drive", S(join_pts[1]) - 12, S(join_pts[2]) + 6, 10.0)
zones = [z for z in zones if z["s1"] > z["s0"]]
zones.sort(key=lambda z: z["s0"])
widths = [{"id": "the trail", "s0": round(s_trail0, 1), "s1": round(s_trail1, 1), "half": 3.0}]

# ---------------------------------------------------------------- the world pieces
tr_pts = [lap[k] for k in t_k]; tr_y = [prof[k] for k in t_k]
tr_c = cum(tr_pts); turn_ = []
for k in range(len(tr_pts)):
    sk = tr_c[k]; a_ = point_at(tr_pts, sk - 10, tr_c); b_ = point_at(tr_pts, sk, tr_c); c_ = point_at(tr_pts, sk + 10, tr_c)
    turn_.append(abs((bearing(b_, c_) - bearing(a_, b_) + 180) % 360 - 180) if math.dist(a_, b_) > 1 and math.dist(b_, c_) > 1 else 0.0)
halves = []
for k in range(len(tr_pts)):
    near = max([turn_[q] for q in range(len(tr_pts)) if abs(tr_c[q] - tr_c[k]) < 15.0] + [0.0])
    halves.append(round(BENCH_HALF + (HAIRPIN_HALF - BENCH_HALF) * min(1.0, max(0.0, (near - 40.0) / 30.0)), 2))
print(f"the bench: {BENCH_HALF:.0f} m half, {HAIRPIN_HALF:.0f} m through the hairpins ({sum(1 for h in halves if h > BENCH_HALF + 0.5)} of {len(halves)} points wider)")
grades = [{"id": "the Kennesaw Mountain Trail's bench", "points": flat(tr_pts), "y": [round(v, 3) for v in tr_y], "half": BENCH_HALF, "blend": BENCH_BLEND, "halves": halves}]
clear = [{"id": "the trail", "points": flat(tr_pts[::2] + [tr_pts[-1]]), "half": CLEAR_HALF},
         {"id": "the summit lot", "points": flat(lot_run[::2] + [lot_run[-1]]), "half": 6.0}]
breaks = [{"id": "the trail", "points": flat(tr_pts[::2] + [tr_pts[-1]]), "half": BREAK_HALF}]
# each jump's flight straight on from its lip (the line bends under a long flight): no tree within 8 m of it for 60 m, breakable within 22 m for 140 m
# (the second build's check: at 26 m/s J1 and J3 flew straight on over a bend into solid trunks 17 to 29 m off the line)
for j in jumps:
    L0 = j["lip"]; dd = j["dir"]
    clear.append({"id": f"{j['id']}'s flight", "points": flat([L0, (L0[0] + dd[0] * 60.0, L0[1] + dd[1] * 60.0)]), "half": 8.0})
    breaks.append({"id": f"{j['id']}'s flight", "points": flat([L0, (L0[0] + dd[0] * 140.0, L0[1] + dd[1] * 140.0)]), "half": 22.0})
zones_land = []
for j in jumps:
    a, b = j["land"]; poly_l, poly_r = [], []
    for s in np.arange(j["sL"] + a, j["sL"] + b + 0.1, 4.0):
        p = point_at(lap, s, C); q = point_at(lap, s + 1.0, C); t = unit((q[0] - p[0], q[1] - p[1])); n = (-t[1], t[0])
        poly_l.append((p[0] + n[0] * (BENCH_HALF + 1.0), p[1] + n[1] * (BENCH_HALF + 1.0))); poly_r.append((p[0] - n[0] * (BENCH_HALF + 1.0), p[1] - n[1] * (BENCH_HALF + 1.0)))
    zones_land.append({"id": j["id"] + " " + j["name"], "poly": flat(poly_l + poly_r[::-1])})
jumps_out = [{"id": j["id"], "look": j["look"], "name": j["name"], "lip": [round(j["lip"][0], 2), round(j["lip"][1], 2)], "dir": [round(j["dir"][0], 4), round(j["dir"][1], 4)],
              "lip_y": round(j["yL"], 3), "angle_deg": j["ang"], "height": j["hlip"], "kicker_m": j["Lk"], "dig": list(j["dig"]), "s_lip": round(j["sL"], 1)} for j in jumps]
track = {"note": "Round 67: the Kennesaw Mountain race's world pieces, from Tools/mapgen/make_mountain_route.py (MapBuilder.Mountain.cs). Coordinates are map meters (= Unity x, z).",
         "grades": grades, "clear_lines": clear, "break_lines": breaks, "landing_zones": zones_land, "jumps": jumps_out, "route_name": SETTINGS["name"]}
lead = [point_at(lap, L_lap - LEAD_IN_M + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0))] + [lap[0]]
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_mountain_route.py from Data/map.json (OSM's Kennesaw Mountain Drive and Kennesaw Mountain Trail). Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": 0.0, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": junctions, "loop_pieces": [{"osm_id": roads[ROAD]["osm_id"], "name": "Kennesaw Mountain Drive", "reverse": False, "length_m": round(cum(road_run)[-1], 1)},
                                                                {"osm_id": 0, "name": "the summit lot", "reverse": False, "length_m": round(cum(lot_run)[-1], 1)},
                                                                {"osm_id": 0, "name": "Kennesaw Mountain Trail", "reverse": False, "length_m": round(cum(trail_run)[-1], 1)}],
    "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": [], "ai_zones": zones, "ai_widths": widths, "reset_skip": [],
})
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print(f"{len(zones)} AI zones; wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    os.makedirs(os.path.join(ROOT, "local/round67"), exist_ok=True)
    x0, x1, z0, z1 = -1300, -350, -6200, -5150
    i0, i1 = int((x0 - OX) / SX), int((x1 - OX) / SX); j0, j1 = int((z0 - OZ) / SZ), int((z1 - OZ) / SZ)
    A = H[j0:j1:3, i0:i1:3].astype(float) / 65535 * (HHI - HLO) + HLO
    fig, ax = plt.subplots(figsize=(18, 18 * (z1 - z0) / (x1 - x0)))
    ax.imshow(A, origin="lower", extent=(x0, x1, z0, z1), cmap="terrain")
    ax.contour(np.linspace(x0, x1, A.shape[1]), np.linspace(z0, z1, A.shape[0]), A, levels=range(300, 700, 10), colors="k", linewidths=.3)
    ax.plot(*zip(*trail), color="m", lw=1)
    ax.plot(*zip(*lap), "r-", lw=1.4)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g")
    for j in jumps: ax.plot(*j["lip"], "k^", ms=10); ax.text(j["lip"][0] + 6, j["lip"][1] + 4, j["id"], fontsize=12)
    for zl in zones_land: ax.fill(*zip(*pts(zl["poly"])), color="y", alpha=.4)
    ax.plot(*lap[0], "ks", ms=10)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    plt.savefig(os.path.join(ROOT, "local/round67/layout.png"), dpi=60, bbox_inches="tight")
    fig, axs = plt.subplots(len(jumps), 1, figsize=(16, 3.2 * len(jumps)))
    for ax, j in zip(axs, jumps):
        s0, s1 = j["sL"] - 40, j["sL"] + 110
        kk = [k for k in range(len(lap)) if s0 <= C[k] <= s1]
        ax.plot([C[k] - j["sL"] for k in kk], [tg[k] for k in kk], color="#999", lw=1, label="ground")
        ax.plot([C[k] - j["sL"] for k in kk], [prof[k] for k in kk], "k-", lw=1.5, label="the bench")
        for v in (20, 25, 30, 35):
            a = math.radians(j["ang"]); xs = np.linspace(0, 110, 200); ys = j["yL"] + np.tan(a) * xs - 9.81 * xs ** 2 / (2 * v * v * math.cos(a) ** 2)
            ax.plot(xs, ys, "--", lw=.8, label=f"{v} m/s")
        ax.set_title(f"{j['id']} {j['name']}"); ax.set_ylim(min(prof[k] for k in kk) - 3, j["yL"] + 12); ax.grid(True, alpha=.3); ax.legend(fontsize=7)
    plt.tight_layout(); plt.savefig(os.path.join(ROOT, "local/round67/profile.png"), dpi=60)
    print("pictures: local/round67/layout.png, profile.png")
