"""Round 73 (A3, Dan, 2026-10-26: "The closest thing to being good is the big ramp off of kennesaw, but why is it not even at the highest point of the
mountain" and 19:11: "for the ramp on the top of the mountain I would prefer it to be more realistic. It doesn't have to be some huge structure just a
jump off of something that could naturally be there that jumps and clears the mountain will be fine"): THE SUMMIT LEAP, a Free Roam world piece on
Kennesaw Mountain (not in the race's lap). It replaces Round 70's steel ramp on the south flank.

Where and why: the mountain's highest ground (689 m as built, x -1050, z -5933) is a sharp peak on the ridge inside the drive's ring: it falls 20 m
within 50 m every way, so no run-up can come in downhill. The gentlest way up is the north-east ridge from the summit lot (the drive's end); the
longest fall is to the south-south-west (bearing about 200: 400 m below the top 800 m out, over the drive's south leg). So the line runs straight
from the summit lot's entrance up the ridge, over the top and down the far side: a gravel track graded up the ridge at a steady climb, the top
rounded over (cut a little: no crest a car at speed leaves the ground on), then an in-run down the south-south-west face (the mountain's own ground,
steepened where it is gentler) into a ground kicker at a granite ledge (Round 68's "overlook" look: the mountain's own rock, slabs on its back face,
boulders along its banks). Nothing built of steel or timber: the ground itself is the jump. The flight passes high over the drive's south leg and
comes down on the mountain's lower flank, graded smooth along the line (a catch zone: RampHold keeps the speed on touching down), then a gravel
arrester bed and a graded track down to North Saint Marys Lane NW (the way back).

What it writes: Data/tracks/kennesaw_summit_leap.json (the RidgeFile shape: grades, clear_lines, break_lines, landing_zones; 'jump': the lip's look
(MapBuilder.Mountain.cs MountainJumpLook); 'leap': the arrester, the track, the fast-travel start). MapBuilder.SummitRamp.cs merges it into the
mountain's pieces. Re-run after mapgen and after make_mountain_route.py, then add_extension_places.py (the fast-travel spot), then Build Map (or the
quick step MountainRace).
Usage: python Tools/mapgen/make_summit_leap.py [--plot]"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/kennesaw_summit_leap.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def flat(p): return [round(v, 2) for q in p for v in q]
def gsm(vals, sig):
    out = []
    for i in range(len(vals)):
        lo, hi = max(0, i - int(3 * sig)), min(len(vals), i + int(3 * sig) + 1)
        w = [math.exp(-((k - i) / sig) ** 2 / 2) for k in range(lo, hi)]
        out.append(sum(vals[k] * wk for k, wk in zip(range(lo, hi), w)) / sum(w))
    return out
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
def seg_d(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0 if L2 < 1e-9 else max(0, min(1, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def poly_d(q, p): return min(seg_d(q, a, b) for a, b in zip(p, p[1:]))
# Round 75 (C, Dan's BUG-001: "there isn't a drop off for the road but a smooth slope"): a bank's width from the ground at the bank's own outer edge
# on each side (on a sidehill the uphill edge's cut is far deeper than the cut under the line's center, which left near-vertical faces): 'slope'
# meters out for every meter of cut or fill there, found by a few steps out from the old width; never narrower than 'base', at most 'cap'
def edge_blends(points, ys, halves, base, slope, cap=45.0):
    out = []
    for i, (q, y, h, b0) in enumerate(zip(points, ys, halves, base)):
        a = points[max(0, i - 1)]; c = points[min(len(points) - 1, i + 1)]
        dx, dz = c[0] - a[0], c[1] - a[1]; L = math.hypot(dx, dz) or 1.0; nx, nz = -dz / L, dx / L
        b = b0
        for _ in range(4):
            dh = max(abs(y - ground(q[0] + nx * sd * (h + b), q[1] + nz * sd * (h + b))) for sd in (-1, 1))
            b = min(cap, max(b0, slope * dh))
        out.append(b)
    return [round(v, 2) for v in gsm(out, 3.0)]
def smooth01(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- hand choices
DRIVE, LOT = 4729, 4887             # Kennesaw Mountain Drive (its end is the summit lot's entrance) and the lot's loop
START_IN = 8.0                      # the run-up starts this far from the drive's end, toward the summit (off the drive and the loop's mouth)
SUMMIT = None                       # found: the highest ground within 120 m of the ridge's top
LIP_UNDER = 0.0                     # (Round 73 try 2) the lip this much under the summit's height: a gentler climb
LIP_PAST = 50.0                     # the lip this far past the summit, on the line: the top's far edge, where the face begins to fall away
ANG = float(os.environ.get("LEAP_ANG", "24"))           # the lip's angle over the horizontal (Round 73: 21; Round 74, Dan: "pitch the ramp a little higher")
R_KICK = float(os.environ.get("LEAP_R", "450"))         # the kicker's radius (Round 68: past about 6.5 m/s2 at the lip's speed a car's body meets a ground kicker; Round 74: the bikes reached 45 m/s at its foot and lost 18 m/s in it at 340 m: 450)
TRACK_HALF, INRUN_HALF = 6.0, 9.0   # the run-up's half width (Round 74: 12 m, was 10), the in-run and kicker's (18 m, was 13)
SIDE_SLOPE = 2.0                    # cut and fill banks: 2 m out for every meter of cut or fill (Round 74: gentle banks, was 1.25)
CATCH_HALF = 28.0                   # the landing ground's half width
CATCH_REL = 16.0                    # the catch steepened (where the ground is flatter) so a middle flight touches down this much shallower than it
VEHICLES = {"Street Classic": (49.0, 14.5), "Drifter Twin": (58.0, 16.0), "Needle 600": (61.0, 18.0)}   # (top m/s, push m/s2): Round 68's model
ROAD_TO = 3335                      # North Saint Marys Lane NW: the way back

drive = pts(M["roads"][DRIVE]["points"]); lot = pts(M["roads"][LOT]["points"])
E = drive[-1]
# the summit: the highest ground near the top
best = (-1e9, None)
for x in np.arange(-1110, -990, 2.0):
    for z in np.arange(-5990, -5870, 2.0):
        g = ground(x, z)
        if g > best[0]: best = (g, (x, z))
SUMMIT = best[1]; y_summit = best[0]
brg = math.atan2(SUMMIT[0] - E[0], SUMMIT[1] - E[1]); d = (math.sin(brg), math.cos(brg)); nl = (-d[1], d[0])
BRG = math.degrees(brg) % 360
def P(s, lat=0.0): return (E[0] + d[0] * s + nl[0] * lat, E[1] + d[1] * s + nl[1] * lat)
s_top = math.dist(E, SUMMIT)
raw = raw_g = lambda s: ground(*P(s))

# ---------------------------------------------------------------- the profile
# Round 74 (F, Dan's BUG-005: "the end of the ramp makes you turn and lose momentum"; 03:13: "can't we pitch the ramp a little higher"). Try 1 (a
# flush lip at 27 degrees, the run-up cut gentler through the summit) flew SHORTER (the Needle 7.0 s / 187 m against Round 73's 7.9 / 228: less speed
# up the steeper kicker and a lower launch): try 2 keeps Round 73's climb and its lip's height (on fill, 18 m over the face's ground: that height is
# the flight), pitched up to ANG, with the turn and the jagged edge taken out: the kicker 18 m wide and level across (INRUN_HALF), its fill's
# banks gentle (SIDE_SLOPE), its back face a rock ledge falling 3 m at 60 degrees then 35 down to the ground (Round 73: 75 then 40, which the
# terrain drew jagged), and a steady, straight line (no sideways tilt at the lip)
# the lip at the summit's height LIP_PAST beyond it (the climb cuts a little through the peak; the lip's ledge stands over the face's first fall)
y0 = ground(*P(START_IN)); s_lip = s_top + LIP_PAST; y_lip = y_summit - LIP_UNDER
th1 = math.radians(ANG); g0 = math.atan((y_lip - y0) / (s_lip - START_IN))
for _ in range(60):
    Lk = max(0.0, R_KICK * (th1 - g0)); rise = sum(math.tan(g0 + (k + 0.5) / R_KICK) for k in range(int(Lk)))
    s_kick_foot = s_lip - Lk; g0 = math.atan((y_lip - rise - y0) / (s_kick_foot - START_IN))
S_, Y_, T_ = [], [], []
for k in range(int(s_lip - START_IN) + 1):
    s_ = START_IN + k
    if s_ <= s_kick_foot: th = g0; y_ = y0 + math.tan(g0) * (s_ - START_IN)
    else:
        th = g0 + (s_ - s_kick_foot) / R_KICK
        y_ = y0 + math.tan(g0) * (s_kick_foot - START_IN) + sum(math.tan(g0 + (q + 0.5) / R_KICK) for q in range(int(s_ - s_kick_foot)))
    S_.append(s_); Y_.append(y_); T_.append(th)
s_lip, y_lip = S_[-1], Y_[-1]
LEN_CLIMB = s_kick_foot - START_IN; CLIMB = math.tan(g0)
k_top = min(range(len(S_)), key=lambda i: abs(S_[i] - s_top)); s_crest, y_crest = s_top, Y_[k_top]

# ---------------------------------------------------------------- speeds and flights
def lip_speed(top, acc, v0=1.0):
    v = v0; vmin = 99.0
    for i in range(len(S_) - 1):
        th = T_[i]; a = acc * max(0, 1 - v / top) - 9.81 * math.sin(th) - 0.02 * v; v = math.sqrt(max(1.0, v * v + 2 * a)); vmin = min(vmin, v)
    return v
def flight(v, gfun):
    vx, vy = v * math.cos(math.radians(ANG)), v * math.sin(math.radians(ANG)); t = 0.0; apex = 0.0; x = 0.0
    while t < 25:
        t += 0.01; x = vx * t; yy = y_lip + vy * t - 4.905 * t * t; gg = gfun(s_lip + x); apex = max(apex, yy - gg)
        if x > 3 and yy <= gg + 0.3: break
    vyl = vy - 9.81 * t; fall = math.degrees(math.atan2(-vyl, vx))
    return dict(lip_speed=round(v, 1), air_s=round(t, 2), dist_m=round(x, 1), apex_over_ground_m=round(apex, 1), land_s=round(s_lip + x, 1), land_speed=round(math.hypot(vx, vyl), 1), fall_deg=round(fall, 1))
# Round 73: the exe's lip speeds on this climb were 0.74 of this model's (the check: the Drifter Twin 32.2 m/s against 44.0, the Street Classic 27.9 against
# 37.8, both flat out from the fast-travel spot): the flights are designed with the measured speeds
CORR = float(os.environ.get("LEAP_CORR", "0.74"))
speeds = {n: CORR * lip_speed(*tv) for n, tv in VEHICLES.items()}
first = {n: flight(v, raw) for n, v in speeds.items()}
land_lo = min(f["land_s"] for f in first.values()) - 60.0; land_hi = max(f["land_s"] for f in first.values()) + 120.0   # Round 74: +120 (the model's speeds are low)

# ---------------------------------------------------------------- the catch: the ground along the line smoothed, steepened where a flight would
# touch down more than CATCH_REL degrees steeper than it (cut below the middle flight's touch-down, never filled more than 6 m), then smoothed again
CATCH_FROM, CATCH_TO = land_lo - 50.0, land_hi + 40.0
cs = list(np.arange(CATCH_FROM - 40.0, CATCH_TO + 0.1, 2.0))
cy = gsm([raw(x) for x in cs], 6.0); cy = gsm(cy, 6.0)
mid_v = speeds["Drifter Twin"]
def fall_at(x):   # the middle flight's angle where it reaches station x
    vx, vy = mid_v * math.cos(math.radians(ANG)), mid_v * math.sin(math.radians(ANG)); t = (x - s_lip) / vx
    return math.degrees(math.atan2(-(vy - 9.81 * t), vx))
want = [max(0.0, fall_at(x) - CATCH_REL) if x > s_lip + 20 else 0.0 for x in cs]
# march down the line from the catch's start: each point no higher than the last less the wanted slope (a steeper catch where the flights fall steeper)
cy2 = list(cy)
for i in range(1, len(cs)):
    if not (land_lo - 40 <= cs[i] <= land_hi): continue
    lim = cy2[i - 1] - math.tan(math.radians(want[i])) * (cs[i] - cs[i - 1])
    cy2[i] = max(min(cy2[i], lim), cy[i] - 16.0)   # Round 74: up to 16 m (was 10): a steeper catch for the steeper lip
cy = gsm(cy2, 4.0)
cat = lambda s: float(np.interp(s, cs, cy)) if CATCH_FROM - 40 <= s <= CATCH_TO else raw(s)
flights = {n: flight(v, cat) for n, v in speeds.items()}
for n, f in flights.items(): f["touch_deg"] = round(f["fall_deg"] - math.degrees(math.atan(-(cat(f["land_s"] + 1) - cat(f["land_s"] - 1)) / 2.0)), 1)
short_v = 30.0; short = flight(short_v, cat)

# ---------------------------------------------------------------- the run-out and the way back: the arrester on the line, then a track to the lane
ARREST_FROM = max(f["land_s"] for f in flights.values()) + 45.0; ARREST_TO = ARREST_FROM + 90.0
ARREST_STRENGTH = [0.12, 0.25, 0.4]
lane = pts(M["roads"][ROAD_TO]["points"]); end_line = P(ARREST_TO + 10.0)
k_lane = min(range(len(lane)), key=lambda i: math.dist(lane[i], end_line)); L_ = lane[k_lane]
# the track: from the arrester's end round onto the lane (a gentle curve: a quadratic from the line's direction to the lane's nearest point)
c1 = P(ARREST_TO + 60.0)
track = [P(ARREST_TO + 10.0)]
for k in range(1, 41):
    t = k / 40; q = ((1 - t) ** 2 * track[0][0] + 2 * (1 - t) * t * c1[0] + t * t * L_[0], (1 - t) ** 2 * track[0][1] + 2 * (1 - t) * t * c1[1] + t * t * L_[1]); track.append(q)
track_y = gsm([ground(*q) for q in track], 3.0)
# Round 75 (C, BUG-001): the run-out's end and the track no longer meet at a step (the run-out's shelf ended 3.8 m over the track's start, a sheer
# face, the grade's open end): the run-out runs on TAIL_M past its end along the line, easing down to the ground, and the track starts at the
# run-out's height, easing into its own profile over the same length
TAIL_M = 46.0
s_end = ARREST_TO + 10.0; y_end = cat(s_end)
tail = [s_end + x for x in np.arange(2.0, TAIL_M + 0.1, 2.0)]
tail_y = [y_end + (raw(t) - y_end) * smooth01((t - s_end) / TAIL_M) for t in tail]
ts = [0.0]
for a, b in zip(track, track[1:]): ts.append(ts[-1] + math.dist(a, b))
track_y = [y_end + (ty - y_end) * smooth01(t / TAIL_M) for t, ty in zip(ts, track_y)]

# ---------------------------------------------------------------- the grades
on = list(np.arange(START_IN, s_lip + 0.01, 2.0))
on_y = [float(np.interp(s, S_, Y_)) for s in on]
on_half = [TRACK_HALF if s < s_kick_foot - 20 else INRUN_HALF for s in on]; on_half = gsm(on_half, 4.0)
on_blend = gsm([max(4.0, SIDE_SLOPE * abs(y - raw(s))) for s, y in zip(on, on_y)], 3.0)
# the kicker's back face: (Round 74) 60 degrees for 3 m, then 35 down to the ground (Round 73's 75 / 40 drew jagged in the terrain: Dan's BUG-005)
back = [s_lip + x for x in np.arange(1.0, 50.0, 1.0)]
back_y = [y_lip - (math.tan(math.radians(60)) * (x - s_lip) if x - s_lip < 3 else math.tan(math.radians(60)) * 3 + math.tan(math.radians(35)) * (x - s_lip - 3)) for x in back]
keep = [(b, y) for b, y in zip(back, back_y) if y > raw(b)]
# Round 75 (C): the catch with the run-out's tail, its banks (and the track's) from the ground at their edges (the run-up's left: the summit lot beside it)
c_s = [x for x in cs if x <= s_end] + tail; c_pts = [P(x) for x in c_s]; c_y = [cat(x) for x in cs if x <= s_end] + tail_y
c_bl = edge_blends(c_pts, c_y, [CATCH_HALF] * len(c_pts), [10.0] * len(c_pts), SIDE_SLOPE)
track_bl = edge_blends(track, track_y, [3.5] * len(track), [4.0] * len(track), SIDE_SLOPE)
grades = [
    {"id": "the summit leap's run-up, top and in-run", "points": flat([P(s) for s in on] + [P(b) for b, _ in keep]), "y": [round(v, 3) for v in on_y + [y for _, y in keep]],
     "half": TRACK_HALF, "blend": 4.0, "halves": [round(h, 2) for h in on_half] + [INRUN_HALF] * len(keep), "blends": [round(b, 2) for b in on_blend] + [4.0] * len(keep),
     "ground_first": 2, "ground_last": 0, "ground_ease_m": 20.0, "open_ends": True,
     "keep_off": flat(drive[-30:]), "keep_off_m": M["roads"][DRIVE]["width"] / 2 + 1.5},
    {"id": "the summit leap's catch and run-out", "points": flat(c_pts), "y": [round(v, 3) for v in c_y],
     "half": CATCH_HALF, "blend": 10.0, "halves": [CATCH_HALF] * len(c_pts), "blends": c_bl, "open_ends": True,
     # (the flight crosses the drive's south leg, the Kennesaw Mountain race's lap, just before the catch: the drive is kept off)
     "keep_off": flat(drive), "keep_off_m": M["roads"][DRIVE]["width"] / 2 + 2.0},
    {"id": "the summit leap's track to North Saint Marys Lane", "points": flat(track), "y": [round(v, 3) for v in track_y], "half": 3.5, "blend": 4.0, "blends": track_bl, "open_ends": True},
]
# the lot's loop and the drive are never under the in-run's fill: report the nearest
near_drive = min(poly_d(P(s), drive) for s in on if s > 30); near_lot = min(poly_d(P(s), lot) for s in on if s > 20)
clear = [{"id": "the summit leap's run-up and in-run", "points": flat([P(START_IN), P(s_lip)]), "half": 12.0},
         {"id": "the summit leap's flight and landing", "points": flat([P(s_lip), P(ARREST_TO + 20.0)]), "half": 22.0},
         {"id": "the summit leap's catch", "points": flat([P(CATCH_FROM), P(ARREST_TO)]), "half": CATCH_HALF + 4.0},
         {"id": "the summit leap's track", "points": flat(track), "half": 7.0}]
breaks = [{"id": "the summit leap's flight", "points": flat([P(s_lip), P(ARREST_TO + 20.0)]), "half": 40.0},
          {"id": "the summit leap's run-up", "points": flat([P(START_IN), P(s_lip)]), "half": 18.0}]
poly_l = [P(x, CATCH_HALF) for x in np.arange(CATCH_FROM, ARREST_FROM + 0.1, 10.0)]; poly_r = [P(x, -CATCH_HALF) for x in np.arange(CATCH_FROM, ARREST_FROM + 0.1, 10.0)]
zones = [{"id": "the summit leap's catch", "poly": flat(poly_l + poly_r[::-1])}]
lip = P(s_lip)
jump = {"id": "SL", "look": "overlook", "name": "the summit leap", "lip": [round(lip[0], 2), round(lip[1], 2)], "dir": [round(d[0], 5), round(d[1], 5)], "dig": [0.0, 0.0],
        "lip_y": round(y_lip, 3), "angle_deg": ANG, "height": round(y_lip - raw(s_lip), 2), "kicker_m": round(s_lip - s_kick_foot, 1), "s_lip": round(s_lip, 1), "bench": INRUN_HALF + 1.0}
st = P(START_IN + 24.0)   # (Round 74: on the run-up itself; at 2 m the fast-travel spot faced the cut's bank beside the drive and the cars stuck there)
leap = {"id": "the summit leap", "arrester": flat([P(ARREST_FROM + (ARREST_TO - ARREST_FROM) * k / 3.0) for k in range(4)]), "arrester_half": CATCH_HALF - 4.0,
        "arrester_strength": ARREST_STRENGTH, "start": [round(st[0], 2), round(st[1], 2)], "start_yaw": round(BRG, 1), "track": flat(track),
        "summit": [round(SUMMIT[0], 2), round(SUMMIT[1], 2)], "crest_y": round(y_crest, 2), "flights": flights}
out = {"note": "Round 73 (A3): the summit leap on Kennesaw Mountain (Free Roam world piece), from Tools/mapgen/make_summit_leap.py; merged into the "
               "mountain's pieces by MapBuilder.SummitRamp.cs. Coordinates are map meters (= Unity x, z).",
       "grades": grades, "clear_lines": clear, "break_lines": breaks, "landing_zones": zones, "jump": jump, "leap": leap}
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"the summit ({SUMMIT[0]:.0f}, {SUMMIT[1]:.0f}) {y_summit:.1f} m, {s_top:.0f} m from the drive's end at bearing {BRG:.0f}")
print(f"run-up: {LEN_CLIMB:.0f} m at {CLIMB * 100:.1f} % from {y0:.1f} m, over the summit at {y_crest:.1f} m ({y_summit - y_crest:.1f} m cut through the peak), the kicker "
      f"{s_lip - s_kick_foot:.0f} m (R {R_KICK:.0f}) to the lip at {s_lip:.0f} m, {y_lip:.1f} m ({y_lip - raw(s_lip):.1f} m over the ground, {ANG:.0f} deg)")
print(f"the line keeps {near_drive:.0f} m off the drive and {near_lot:.0f} m off the lot's loop (past the start); cut / fill up to {max(abs(y - raw(s)) for s, y in zip(on, on_y)):.1f} m")
for n, f in flights.items(): print(f"  {n:15} lip {f['lip_speed']} m/s ({f['lip_speed'] ** 2 / R_KICK:.1f} m/s2 on the kicker): {f['air_s']} s, {f['dist_m']} m, {f['apex_over_ground_m']} m over the ground at most, lands at {f['land_s']} m ({f['land_speed']} m/s, {f['touch_deg']} deg to the catch)")
print(f"  short (30 m/s at the lip): {short['air_s']} s, {short['dist_m']} m, lands at {short['land_s']} m")
print(f"catch {CATCH_FROM:.0f} to {ARREST_FROM:.0f} m, arrester {ARREST_FROM:.0f} to {ARREST_TO:.0f} m, then {sum(math.dist(a, b) for a, b in zip(track, track[1:])):.0f} m of track to North Saint Marys Lane NW; wrote {os.path.relpath(OUT, ROOT)}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(18, 7))
    xs = np.arange(0, ARREST_TO + 20, 2.0); ax.plot(xs, [raw(x) for x in xs], color="#999", label="ground")
    ax.plot(on, on_y, "k-", lw=2, label="run-up, top, in-run, kicker (graded)"); ax.plot(cs, [cat(x) for x in cs], "g-", label="catch (graded)")
    for n, f in flights.items():
        v = f["lip_speed"]; th = math.radians(ANG); xx = np.linspace(0, f["dist_m"], 200)
        ax.plot(s_lip + xx, [y_lip + math.tan(th) * x - 9.81 * x * x / (2 * v * v * math.cos(th) ** 2) for x in xx], "--", label=f"{n} {v} m/s")
    ax.axvspan(ARREST_FROM, ARREST_TO, color="orange", alpha=.3); ax.set_aspect("equal"); ax.grid(True, alpha=.3); ax.legend(fontsize=8)
    os.makedirs(os.path.join(ROOT, "local/round73"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round73/summit_leap_profile.png"), dpi=60, bbox_inches="tight")
