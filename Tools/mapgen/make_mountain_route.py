"""Round 68 (Dan, 2026-10-25 08:35, after driving Round 67: "I specifically wanted to do a mountain trail so I have really big jumps but these are just
little bunny hops ... I had bigger jumps in my own neighborhood than I got in the mountains...cmon"): the "Kennesaw Mountain" race rebuilt for really big air.
Round 67 raced down the Kennesaw Mountain Trail's zig-zag (six jumps of 1.1 to 2.3 s). Round 68 keeps its start, the climb up Kennesaw Mountain Drive
(OSM road 4729) and its closures, and comes down the mountain on three long straight lines cut through the woods (TODO.md Round 68: "cut straight lines
through the woods where the trail zig-zags; clear and grade them"), each built round one huge jump designed from its flight:
  J1  THE SUMMIT FLIGHT   the signature: the drive's last straight below the summit is the run-up (the race line leaves the road where it bends round
                          the summit's knob and runs on straight, past the summit lot), off a granite ledge at the top of the mountain's north-east face,
                          out over it, landing far down the face (the longest air in the game)
  J2  THE WEST FACE       a hairpin, then west along the level shoulder under the summit to its brink, off a boulder ledge out over the steep west face,
                          down onto a catch slope cut into the foot of the face
  J3  THE EARTHWORKS      a hairpin at the foot of the face, then north-east down to the visitor center: off an old earthworks berm at the slope's foot
                          onto a long landing berm across the flats
Every lip is a real physics jump (the ground itself shaped; no aimed launch). Each flight was computed from the run-up speed (the vehicles' own
acceleration, flat out) and the lip's angle, and its catch slope shaped to it: the landing surface is marched so that every flight in the design
speed range touches down at about CATCH_REL_DEG to it (the slower cars early, the bikes late), then eased back to the ground (cut or fill as the
slope needs). The catch slopes are landing zones (RampHold: the speed kept on touching down).
FORWARD only (Round 67: reverse not offered; Dan decides). No shortcuts. Closed roads ("traffic": false): the park's drive.

Hand choices are the constants below; everything else is computed (re-run after mapgen; it reads Assets/Generated/terrain_full.r16). It writes:
  Data/routes/kennesaw_mountain.json   the race (RaceRoute.cs format)
  Data/tracks/kennesaw_mountain.json   what the map build makes (MapBuilder.Mountain.cs): the lines' bench with the jumps and catch slopes in its
                                       profile, cleared and breakable lines, landing zones, the jumps' looks
  local/round68/design.json            the designed flights per vehicle (git-ignored; the report's table)
Pictures for checking (git-ignored): local/round68/layout.png, local/round68/profile.png with --plot.

Usage: python Tools/mapgen/make_mountain_route.py [--plot]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/kennesaw_mountain.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/kennesaw_mountain.json")
OUT_DESIGN = os.path.join(ROOT, "local/round68/design.json")
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
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def bearing(a, b): return math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360
def dirb(brg): return (math.sin(math.radians(brg)), math.cos(math.radians(brg)))
def herm(y0, g0, y1, g1, L, t):
    t2, t3 = t * t, t * t * t
    return (2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * L * g0 + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * L * g1
def gsm_list(vals, sig):
    k = int(3 * sig) + 1; w = [math.exp(-(i / sig) ** 2 / 2) for i in range(-k, k + 1)]; out = []
    for i in range(len(vals)):
        s = ws = 0.0
        for j, wj in zip(range(i - k, i + k + 1), w):
            if 0 <= j < len(vals): s += vals[j] * wj; ws += wj
        out.append(s / ws)
    return out

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Kennesaw Mountain",
    "direction": "forward",
    "directions": ["forward"],
    "laps": 2,
    "traffic": False,
    "gate_half_width_m": 30.0,         # (the earthworks' flight crosses the line in the air, a little north of the drive)
    "checkpoint_half_width_m": 14.0,
    "gate_below_m": 6.0,
    "gate_above_m": 90.0,              # (a car flying a jump over a checkpoint still crosses it: the apexes are 30 to 60 m over the ground)
    "reverse_grid_back_m": 45.0,
    "ai_hill_targets": [56.0, 56.0, 56.0],   # the AI on the lines' grades outside the jumps' zones (RoadDriver: over 14 %; Round 70: Hard's at the difficulty's pace, no cap left but the crests)
    "ai_crest_g": [30.0, 40.0, 50.0],         # the jumps are taken, not driven round (RoadDriver's crest rule would brake for every lip)
}
ROAD = 4729                        # Kennesaw Mountain Drive, the visitor center (s 0) to the summit lot (its end)
LEAD_IN_M = 70.0
GATE_AT = (-830.0, -5190.0)        # on the drive east of the visitor center, its grid on the straight before it (Round 67)
# the descent: straight legs between these corners (each rounded with its radius), from the drive below the summit back onto the drive by the visitor center
DEPART = (-1082.0, -5839.0)        # where the race line leaves the drive below the summit (the drive bends round the summit's knob from here)
LEG1_BRG, LEG1_LEN = 47.0, 640.0   # the summit flight's leg: on past the summit lot, the lip where the north-east face begins, down the face to the first
                                   # hairpin (the bikes land up to 350 m past the lip at 60 to 70 m/s: 130 m to brake)
H2_AT = (-1146.0, -5440.0)         # the second hairpin, on the flats at the foot of the west face
JOIN_AT = (-878.0, -5215.0)        # where the last line meets the drive, eastbound to the gate (no braking: the jump's landing runs on to the finish)
H1_R, H2_R, JOIN_R = 17.0, 17.0, 45.0
# Round 73 A2 (Dan's BUG-003 at (-933, -5722): "the road will be inoperable in free roam. It needs to be fixed"): Round 68's summit flight leg ran on
# the drive's own line for its last 170 m (the leg's bearing within 3 degrees of the drive's), so its bench and J1's 15 m kicker were graded over the
# drive. Now the line leaves the drive at DEPART with a gentle left bend onto a bench of its own OFF_M north-west of the drive (down the sidehill),
# bends back parallel to it at K and runs on to the same first hairpin: J1's kicker stands on that bench, abeam the summit lot, and the drive keeps
# its own height all the way to the lot. PRE_M of the drive before DEPART are part of the bend's approach.
OFF_M, K_ALONG_M, OFF_R, PRE_M = 28.0, 45.0, 70.0, 40.0
J1_ON_LEG = 130.0                 # J1's lip on its leg (from K's bend): the kicker (90 m) on the straight
J1_LEG = 3
BENCH_HALF, BENCH_BLEND = 7.0, 4.0  # the lines' bench: 14 m level across (room to race and pass), blending back to the ground over at least 4 m
SIDE_SLOPE = 1.25                   # and over 1.25 m for every meter the bench stands above or below the ground (cut and fill banks)
HAIRPIN_HALF = 11.0                 # the bench this wide (half) through the hairpins
LAND_HALF = 16.0                    # and over the catch slopes (a flight a little off the line still lands on the graded slope)
CLEAR_HALF = 16.0                   # no tree within this of the line; within this of a flight's corridor too
BREAK_HALF = 32.0                   # trees within this of the line snap when hit
CATCH_REL_DEG = 16.0                # the catch slope: steepened (where the ground is flatter) so the middle flight touches down this much shallower than it
# (the kickers' radius: the run-up's speed squared over it is what the suspension must add to gravity; past about 6 m/s2 a car's body meets the ramp and
# loses half its speed, Round 68's first exe run: R 75 m at 38 m/s took the Drifter Twin from 37.7 to 18.6 m/s)
# the jumps: (id, look, name, the leg, the lip's station on its leg (m from the leg's start), the lip's angle over the horizontal, the kicker's radius (m),
# the design speeds at the lip (m/s: the slowest car flat out less a margin, the middle, the fastest bike), the outrun's radius (m), the most cut or fill
# the catch may take (m), where the catch meets the ground (0: at the middle flight's touch-down, cut below and fill above it; 1: at its far end, a
# landing berm standing on the ground))
JUMPS = [
    ("J1", "overlook", "the summit flight", 3, J1_ON_LEG, 17.0, 300.0, (38.0, 46.0, 52.0), 260.0, 12.0, 0.0),
    ("J2", "ledge", "the west face", 4, 128.0, 14.0, 340.0, (32.0, 38.0, 46.0), 220.0, 12.0, 0.6),
    ("J3", "berm", "the earthworks", 5, 160.0, 18.0, 320.0, (30.0, 36.0, 44.0), 120.0, 10.0, 1.0),
]
VEHICLES = {"street": ("Street Classic", 49.0, 14.5, 25.0), "drifter": ("Drifter Twin", 58.0, 16.0, 30.0), "needle": ("Needle 600", 61.0, 18.0, 32.0)}
AI_LIP = {"J1": 46.0, "J2": 38.0, "J3": 40.0}   # the AI's held speed up each run-up (at the lip): the catch's middle
CHECKPOINTS = [   # (x, z, label) in lap order from the gate; CP1 to CP4 Round 67's, the rest on the new lines
    (-470.0, -5333.0, "Kennesaw Mountain Drive"),
    (-556.0, -5640.0, "the east flank"),
    (-1076.0, -6088.0, "the south flank"),
    (-1110.0, -5859.0, "the saddle road"),
    (-930.0, -5714.0, "the summit"),
    (-560.0, -5470.0, "the east face"),
    (-700.0, -5476.0, "the shoulder"),
    (-1080.0, -5480.0, "the west face"),
    (-1050.0, -5360.0, "the earthworks"),
    (-939.0, -5256.0, "the visitor center"),
]

# ---------------------------------------------------------------- the line
road = pts(roads[ROAD]["points"]); rc = cum(road)
dep_s = nearest_s(road, DEPART)[1]
join_s = nearest_s(road, JOIN_AT)[1]; JOIN_S = join_s + 40.0
road_run = resample(cut(road, JOIN_S, dep_s - PRE_M), 3.0)
P0 = point_at(road, dep_s, rc)
H1c = (P0[0] + dirb(LEG1_BRG)[0] * LEG1_LEN, P0[1] + dirb(LEG1_BRG)[1] * LEG1_LEN)
_k0 = point_at(road, dep_s + K_ALONG_M, rc); _k1 = point_at(road, dep_s + K_ALONG_M + 2.0, rc); _ku = unit((_k1[0] - _k0[0], _k1[1] - _k0[1]))
K = (_k0[0] - _ku[1] * OFF_M, _k0[1] + _ku[0] * OFF_M)   # left of the drive's direction (north-west)
corners = [road_run[-1], P0, K, H1c, H2_AT, point_at(road, join_s, rc), road_run[0]]
radii = [None, OFF_R, OFF_R, H1_R, H2_R, JOIN_R, None]
def fillet(cs, rs, step=1.0):
    """the corners' polyline rounded: straight legs and circular arcs; returns points at ~step and, per point, its leg (1, 2, ...) or 0 on an arc"""
    tang = []
    for i in range(1, len(cs) - 1):
        a, b, c = cs[i - 1], cs[i], cs[i + 1]; u1 = unit((b[0] - a[0], b[1] - a[1])); u2 = unit((c[0] - b[0], c[1] - b[1]))
        th = math.acos(max(-1, min(1, u1[0] * u2[0] + u1[1] * u2[1]))); d = rs[i] * math.tan(th / 2)
        t1 = (b[0] - u1[0] * d, b[1] - u1[1] * d); t2 = (b[0] + u2[0] * d, b[1] + u2[1] * d)
        cr = u1[0] * u2[1] - u1[1] * u2[0]; nrm = (-u1[1], u1[0]) if cr > 0 else (u1[1], -u1[0])
        ctr = (t1[0] + nrm[0] * rs[i], t1[1] + nrm[1] * rs[i]); tang.append((t1, t2, ctr, th, cr))
    out, leg = [], []
    start = cs[0]
    for i in range(len(cs) - 1):
        end = tang[i][0] if i < len(tang) else cs[-1]
        L = math.dist(start, end); n = max(1, int(round(L / step)))
        for k in range(n): out.append((start[0] + (end[0] - start[0]) * k / n, start[1] + (end[1] - start[1]) * k / n)); leg.append(i + 1)
        if i < len(tang):
            t1, t2, ctr, th, cr = tang[i]; a0 = math.atan2(t1[1] - ctr[1], t1[0] - ctr[0]); r = math.dist(t1, ctr)
            n = max(2, int(round(r * th / step)))
            for k in range(n): a = a0 + (th if cr > 0 else -th) * k / n; out.append((ctr[0] + r * math.cos(a), ctr[1] + r * math.sin(a))); leg.append(0)
            start = t2
    out.append(cs[-1]); leg.append(len(cs) - 1)
    return out, leg
desc, desc_leg = fillet(corners, radii)
line = road_run + desc[1:]
kind = ["road"] * len(road_run) + ["cut"] * (len(desc) - 1)
legno = [0] * len(road_run) + desc_leg[1:]
gap = math.dist(line[-1], line[0])
if gap > 4.0: raise SystemExit(f"the lap does not close: {gap:.1f} m")
line = line[:-1] + [line[0]]
# the lap starts at the gate
C0 = cum(line); g_s = nearest_s(line, GATE_AT)[1]; k0 = min(range(len(line)), key=lambda k: abs(C0[k] - g_s))
def rotate(a, k): return a[k:-1] + a[:k] + [a[k]]
lap = rotate(line, k0); kind = rotate(kind, k0); legno = rotate(legno, k0)
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
ck = [k for k in range(len(lap)) if kind[k] == "cut"]
s_cut0, s_cut1 = C[ck[0]], C[ck[-1]]
leg_s = {}   # each leg's straight: its first and last lap station
for k in ck:
    if legno[k] > 0: a = leg_s.setdefault(legno[k], [C[k], C[k]]); a[0] = min(a[0], C[k]); a[1] = max(a[1], C[k])
print(f"lap {L_lap:.0f} m: the drive {cum(road_run)[-1]:.0f} m (from its {JOIN_S:.0f} m to its {dep_s:.0f} m), the descent {s_cut1 - s_cut0:.0f} m (lap {s_cut0:.0f} to {s_cut1:.0f} m), "
      + ", ".join(f"leg {n} {b - a:.0f} m" for n, (a, b) in sorted(leg_s.items())))
# the descent must keep off the drive (its other parts) and the park's buildings
others = [pts(roads[ROAD]["points"])]
bl = []
for b in m["buildings"]:
    fp = b.get("footprint")
    if fp and -1300 < min(fp[0::2]) < -400 and -5900 < min(fp[1::2]) < -5100: bl.append(pts(fp))
def poly_dist(q, poly):
    return min(math.dist(q, (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)) for a, b in zip(poly, poly[1:] + poly[:1]) for t in (0, .25, .5, .75, 1))
dmin_road = min((nearest_s(road, lap[k])[0], C[k]) for k in ck[::4] if not (s_cut0 + 40 > C[k] or C[k] > s_cut1 - 10))
dmin_bld = min((poly_dist(lap[k], p), C[k]) for k in ck[::4] for p in bl)
print(f"the descent keeps {dmin_road[0]:.0f} m off the drive (nearest at lap {dmin_road[1]:.0f}) and {dmin_bld[0]:.0f} m off the park's buildings (at lap {dmin_bld[1]:.0f})")

# ---------------------------------------------------------------- the profile
tg = [ground(*q) for q in lap]
prof = list(tg)
kk = [k for k in range(len(lap)) if kind[k] == "cut"]
k_lo, k_hi = kk[0], kk[-1]
seg = list(range(max(0, k_lo - 30), min(len(lap), k_hi + 30)))
base = gsm_list([tg[k] for k in seg], 6.0)
for i, k in enumerate(seg):
    if kind[k] == "cut": prof[k] = base[i]
# Round 73 A2: the departure leaves the drive like a side road: the line keeps the drive's height (at its nearest point) until the bench's edge is past
# the drive's edge (SEP_M off its center), then falls at most DEP_GRADE (a fill bench over the sidehill where its ground is lower) until it meets
# its own ground
DEP_GRADE, SEP_M = 0.12, 20.0   # (Round 73: 12 m left a drop-off at the drive's edge (the check's Drifter Twin went over it): from 20 m off the bank is a gentle one)
k_dep = min(ck, key=lambda k: math.dist(lap[k], P0)); y_sep = None; s_sep = None
for k in ck:
    if C[k] > C[k_dep] + 300.0: break
    d_, s_ = nearest_s(road, lap[k])
    if y_sep is None:
        if C[k] < C[k_dep] or d_ < SEP_M: prof[k] = ground(*point_at(road, s_, rc)); continue
        y_sep, s_sep = prof[k - 1], C[k - 1]
    prof[k] = max(prof[k], y_sep - DEP_GRADE * (C[k] - s_sep))
# the hairpins: a level pad over the arc and PAD_M either side (the two legs' benches overlap near a hairpin: one height there, no step between
# them), eased back into the legs' ground over EASE_M
PAD_M, EASE_M = 22.0, 70.0
arcs = []
for k in ck:
    if legno[k] == 0 and legno[k - 1] != 0:
        e = k
        while e + 1 < len(lap) and legno[e + 1] == 0: e += 1
        # (Round 73 A2: only the hairpins, not the departure's gentle bends: an arc's radius from its length and its turn)
        h0 = math.atan2(lap[k + 1][0] - lap[k - 1][0], lap[k + 1][1] - lap[k - 1][1]); h1 = math.atan2(lap[e + 1][0] - lap[e - 1][0], lap[e + 1][1] - lap[e - 1][1])
        turn = abs((h1 - h0 + math.pi) % (2 * math.pi) - math.pi)
        if C[e] - C[k] > 25.0 and (C[e] - C[k]) / max(turn, 1e-6) < 60.0: arcs.append((C[k], C[e]))
for a0, a1 in arcs:
    pad = [k for k in ck if a0 - PAD_M <= C[k] <= a1 + PAD_M]
    yp = sum(prof[k] for k in pad) / len(pad)
    for k in ck:
        if a0 - PAD_M <= C[k] <= a1 + PAD_M: prof[k] = yp
        elif a0 - PAD_M - EASE_M < C[k] < a0 - PAD_M: t = (a0 - PAD_M - C[k]) / EASE_M; w = t * t * (3 - 2 * t); prof[k] = yp + w * (prof[k] - yp)
        elif a1 + PAD_M < C[k] < a1 + PAD_M + EASE_M: t = (C[k] - a1 - PAD_M) / EASE_M; w = t * t * (3 - 2 * t); prof[k] = yp + w * (prof[k] - yp)
# the departure from the drive: the drive's own surface for the first 30 m, blending into the line's ground
k_d = ck[0]
for k in ck[:40]:
    t = (C[k] - C[k_d]) / 40.0; prof[k] = prof[k_d - 1] * (1 - t) + prof[k] * t if k_d > 0 else prof[k]
k_e = ck[-1]
for k in ck[-40:]:
    t = (C[k_e] - C[k]) / 40.0; prof[k] = prof[k_e + 1] * (1 - t) + prof[k] * t if k_e + 1 < len(lap) else prof[k]
def idx(s): return max(0, min(len(C) - 2, int(np.searchsorted(C, s)) - 1))
def at(arr, s):
    i = idx(s); t = (s - C[i]) / max(C[i + 1] - C[i], 1e-9); return arr[i] + t * (arr[i + 1] - arr[i])
def slope_at(arr, s, d=1.5): return (at(arr, s + d) - at(arr, s - d)) / (2 * d)
def traj(yL, ang, v, x):
    a = math.radians(ang); return yL + math.tan(a) * x - 9.81 * x * x / (2 * v * v * math.cos(a) ** 2)
def traj_slope(ang, v, x):
    a = math.radians(ang); return math.tan(a) - 9.81 * x / (v * v * math.cos(a) ** 2)
def cap_crests(s0, s1, R):
    """no convex crest sharper than R on [s0, s1] (cut down, never filled): a car at v stays on the ground below sqrt(g R)"""
    ks = [k for k in range(len(lap)) if s0 <= C[k] <= s1]
    for _ in range(4000):
        moved = 0.0
        for k in ks[1:-1]:
            h1, h2 = C[k] - C[k - 1], C[k + 1] - C[k]
            avg = (prof[k - 1] * h2 + prof[k + 1] * h1) / (h1 + h2); lim = avg + h1 * h2 / (2 * R)
            if prof[k] > lim: moved = max(moved, prof[k] - lim); prof[k] = lim
        if moved < 1e-4: break
jumps = []
for (jid, look, name, leg, s_on_leg, ang, Rk, (v_lo, v_mid, v_hi), R_out, most, bal) in JUMPS:
    a_leg, b_leg = leg_s[leg]; sL = a_leg + s_on_leg
    if not (a_leg + 60 < sL < b_leg - 60): raise SystemExit(f"{jid}: its lip at {s_on_leg} m is off its leg's straight ({b_leg - a_leg:.0f} m)")
    # the run-up: no crest a car at the top of the range would leave the ground on (v_hi^2 / g, and a margin)
    cap_crests(a_leg + 10, sL - 5, v_hi * v_hi / 9.81 * 1.25)
    # the kicker: a circular arc from the run-up's grade at its foot to the lip's angle, back from the lip
    th_l = math.radians(ang); Lk = None
    for back in np.arange(1.0, 200.0, 0.5):
        if math.tan(th_l - back / Rk) <= slope_at(prof, sL - back, 3.0): Lk = back; break
    if Lk is None: raise SystemExit(f"{jid}: no foot for its kicker")
    s_foot = sL - Lk; y = at(prof, s_foot)
    ks = [k for k in range(len(lap)) if s_foot <= C[k] <= sL]
    ky = {}
    s_prev = s_foot
    for k in ks:
        ss = C[k]; ym = y + sum(math.tan(th_l - (sL - (s_prev + (ss - s_prev) * (q + .5) / 8)) / Rk) * (ss - s_prev) / 8 for q in range(8)); ky[k] = ym; y = ym; s_prev = ss
    for k, v in ky.items(): prof[k] = v
    yL = y + math.tan(th_l) * (sL - s_prev)
    # the catch: marched from the slowest flight's touch-down, each point's slope the flight landing there eased by CATCH_REL_DEG, until the fastest
    # has landed; then the outrun's arc back to the ground's grade and on until it meets the ground
    jumps.append(dict(id=jid, look=look, name=name, leg=leg, sL=sL, yL=yL, ang=ang, Rk=Rk, Lk=Lk, v_lo=v_lo, v_mid=v_mid, v_hi=v_hi, R_out=R_out, most=most, bal=bal))
base_full = list(prof)   # the ground the catches start from (the run-ups and kickers in it)
def landing(j, v, arr):
    x = 3.0
    while traj(j["yL"], j["ang"], v, x) > at(arr, j["sL"] + x) and x < 700: x += 0.25
    return x
def smooth01(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
for j in jumps:
    sL, yL, ang = j["sL"], j["yL"], j["ang"]
    gnd = lambda x: at(base_full, sL + x)
    gsl = lambda x: (gnd(x + 3.0) - gnd(x - 3.0)) / 6.0
    x_lo, x_mid, x_hi = (landing(j, v, base_full) for v in (j["v_lo"], j["v_mid"], j["v_hi"]))
    # the shaping ends on the jump's own leg, 15 m before its bend (the last leg: 25 m before the drive; its bend onto the drive is a gentle one)
    limit = (leg_s[j["leg"]][1] - 15.0 if j["leg"] < max(leg_s) - 1 else s_cut1 - 25.0) - sL
    # the catch: a straight slope through the middle flight's touch-down, steepened (only where the ground is flatter) so that flight meets it at
    # CATCH_REL_DEG, the cut and fill balanced about the touch-down, no more than 'most', eased in from the ground over TAPER m and out by the outrun
    # (its slope turned back at 1 / R_out, held until it meets the ground), all of it before the leg's bend; less steep until it fits
    fs = traj_slope(ang, j["v_mid"], x_mid); n_mid = gsl(x_mid)
    want = math.tan(math.atan(fs) + math.radians(CATCH_REL_DEG))
    w0, w1 = x_lo - 12.0, x_hi + 12.0; TAPER = 25.0
    x_an = x_mid + j["bal"] * (w1 - x_mid); y_m = gnd(x_an)
    def herm_len(p0, m0, p1, m1, R):
        """the shortest ease (cubic Hermite) from (p0, slope m0) to (p1, m1) whose curvature stays under 1 / R"""
        for L in np.arange(8.0, 600.0, 2.0):
            if max(abs((6 * t - 4) * m0 * L + (6 - 12 * t) * (p1 - p0) + (6 * t - 2) * m1 * L) / (L * L) for t in np.linspace(0, 1, 21)) <= 1.0 / R: return L
        return 600.0
    def catch_path(sigma):
        """the catch: the ground tilted by (sigma - its grade at the middle touch-down) about x_an over the window, eased in and out"""
        a = sigma - n_mid; d0, d1 = a * (w0 - x_an), a * (w1 - x_an)
        T = herm_len(0.0, 0.0, d0, a, j["R_out"]); Lr = herm_len(d1, a, 0.0, 0.0, j["R_out"])
        path = {}
        for x in np.arange(max(12.0, w0 - T), w1 + Lr + 0.01, 0.5):
            if x < w0: dl = herm(0.0, 0.0, d0, a, T, (x - (w0 - T)) / T)
            elif x <= w1: dl = d0 + a * (x - w0)
            else: dl = herm(d1, a, 0.0, 0.0, Lr, (x - w1) / Lr)
            path[round(x, 1)] = gnd(x) + dl
        return path, w1 + Lr, max(abs(d0), abs(d1))
    shaped = j["most"] > 0 and want < n_mid
    sigma, path, x_end = n_mid, {}, w1
    if shaped:
        lo_, hi_ = 0.0, 1.0
        for _ in range(26):
            mid = (lo_ + hi_) / 2; pth, xe, dev = catch_path(n_mid + (want - n_mid) * mid)
            if dev > j["most"] or xe > limit: hi_ = mid
            else: lo_ = mid
        sigma = n_mid + (want - n_mid) * lo_
        path, x_end, _ = catch_path(sigma)
        if lo_ < 1e-3: shaped, path = False, {}
    worst = 0.0
    for x, y in path.items():
        if x > limit: continue
        k = idx(sL + x)
        if kind[k] != "cut": continue
        worst = max(worst, abs(y - base_full[k])); prof[k] = y
    # the kicker's back: 75 degrees for its first 3 m (steeper than a wheel counts as ground: a front wheel past the lip never touches it while the rear
    # is still on the kicker; the third exe run lost 17 m/s there at 65 degrees), then 40 degrees down to the ground (the kicker stands over it)
    for k in range(idx(sL) + 1, idx(sL + 30.0) + 1):
        xx = C[k] - sL
        if kind[k] == "cut" and str(round(xx, 1)) not in path: prof[k] = max(prof[k], yL - (math.tan(math.radians(75.0)) * xx if xx < 3.0 else math.tan(math.radians(75.0)) * 3.0 + math.tan(math.radians(40.0)) * (xx - 3.0)))
    j.update(xa=x_lo, xm=x_mid, xb=x_hi, worst=worst, sigma=sigma, n_mid=n_mid, shaped=shaped, want=want, x_end=x_end, limit=limit)
for j in jumps:
    cap_crests(j["sL"] + max(20.0, j["xa"] - 30.0), leg_s[j["leg"]][1] - 5.0, 330.0)
# a little rounding of every corner of the profile (never over the lips and kickers)
p2 = list(prof)
for k in range(k_lo, k_hi + 1):
    if any(j["sL"] - j["Lk"] - 1 <= C[k] <= j["sL"] + 0.5 for j in jumps): continue
    lo_, hi_ = max(0, k - 4), min(len(lap), k + 5)
    w = [math.exp(-((C[q] - C[k]) / 1.2) ** 2 / 2) for q in range(lo_, hi_)]
    p2[k] = sum(prof[q] * wq for q, wq in zip(range(lo_, hi_), w)) / sum(w)
prof = p2

# ---------------------------------------------------------------- the drive down, per vehicle: flat out, braking for the bends, flying the lips
def turn_r(k):
    a, b, c = lap[max(0, k - 3)], lap[k], lap[min(len(lap) - 1, k + 3)]
    ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a)
    cr = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]))
    return 1e6 if cr < 1e-6 else ab * bc * ca / (2 * cr)
def drive(top, acc, grip, v0, s0, s1, lip_cap=None):
    """the speed along [s0, s1] at 1 m: forward flat out, a backward pass for the bends (braking 18 m/s2); a lip launches its flight"""
    n = int(s1 - s0) + 1; ss = [s0 + i for i in range(n)]
    vmax = []
    for s in ss:
        k = idx(s); r = turn_r(k); vmax.append(min(top * 1.5, math.sqrt(grip * 0.85 * r), 1.55 * r, 14.0 if legno[k] == 0 and r < 40 else 999.0))
    for i in range(n - 2, -1, -1):
        g = slope_at(prof, ss[i]); vmax[i] = min(vmax[i], math.sqrt(vmax[i + 1] ** 2 + 2 * (18.0 + 9.81 * g) * 1.0))
    v = v0; out = []; flights = {}; i = 0
    while i < n:
        s = ss[i]
        lip = next((j for j in jumps if abs(s - j["sL"]) < 0.5), None)
        if lip is not None:
            vl = v if lip_cap is None else min(v, lip_cap.get(lip["id"], v))
            a = math.radians(lip["ang"]); vx, vy = vl * math.cos(a), vl * math.sin(a); t = 0.0; best = 0.0
            while True:
                t += 0.005; x = vx * t; y = lip["yL"] + vy * t - 4.905 * t * t; gy = at(prof, lip["sL"] + x)
                best = max(best, y - gy)
                if x > 3 and y <= gy + 0.3: break
            vyl = vy - 9.81 * t; slp = slope_at(prof, lip["sL"] + x)
            relang = math.degrees(math.atan2(-vyl, vx)) - math.degrees(math.atan(-slp))
            vland = math.hypot(vx, vyl)
            flights[lip["id"]] = dict(lip_speed=round(vl, 1), air_s=round(t, 2), dist_m=round(x, 1), apex_over_ground_m=round(best, 1), touch_angle_deg=round(relang, 1),
                                      landing_speed=round(vland * 0.97, 1), land_s=round(lip["sL"] + x, 1))
            v = vland * 0.97; i += int(round(x)); out.append((s, vl)); continue
        if v > vmax[i]: v = vmax[i]
        g = slope_at(prof, s); a = acc * max(0.0, 1 - v / top) - 9.81 * g / math.sqrt(1 + g * g) - 0.02 * v
        v = math.sqrt(max(4.0, v * v + 2 * a)); v = min(v, vmax[i] if i < n else v); out.append((s, v)); i += 1
    return out, flights
design = {}
print("the jumps (designed flights, flat out from the summit straight; the AI at its held speeds):")
for vk, (vname, top, acc, grip) in VEHICLES.items():
    _, fl = drive(top, acc, grip, 32.0, s_cut0 - 150, s_cut1)
    design[vname] = fl
for vk in ("drifter",):
    vname, top, acc, grip = VEHICLES[vk]
    _, fl = drive(top, acc, grip, 32.0, s_cut0 - 150, s_cut1, lip_cap=AI_LIP)
    design["AI (" + vname + ")"] = fl
for j in jumps:
    print(f"  {j['id']} {j['name']:18} lip lap {j['sL']:.0f} m, {j['yL']:.1f} m ({j['yL'] - tg[idx(j['sL'])]:.1f} over the ground), {j['ang']:.0f} deg, kicker {j['Lk']:.0f} m (R {j['Rk']:.0f}); "
          f"touch-downs {j['xa']:.0f} / {j['xm']:.0f} / {j['xb']:.0f} m past the lip at {j['v_lo']:.0f} / {j['v_mid']:.0f} / {j['v_hi']:.0f} m/s; the ground there {math.degrees(math.atan(j['n_mid'])):.0f} deg, "
          f"the catch {math.degrees(math.atan(j['sigma'])):.0f} deg (wanted {math.degrees(math.atan(j['want'])):.0f}), to {j['x_end']:.0f} m; cut / fill up to {j['worst']:.1f} m")
    for vname, fl in design.items():
        f = fl.get(j["id"])
        if f: print(f"      {vname:22} lip {f['lip_speed']:4.1f} m/s: {f['air_s']:.2f} s, {f['dist_m']:.0f} m, {f['apex_over_ground_m']:.0f} m over the ground, touches down at {f['touch_angle_deg']:.0f} deg to it, {f['landing_speed']:.0f} m/s")
os.makedirs(os.path.dirname(OUT_DESIGN), exist_ok=True)
json.dump({"vehicles": design, "jumps": [{k: (round(v, 2) if isinstance(v, float) else v) for k, v in j.items() if k not in ("catch", "outrun")} for j in jumps]}, open(OUT_DESIGN, "w"), indent=1)

# ---------------------------------------------------------------- checkpoints
cps = []
for x, z, label in CHECKPOINTS:
    s = round(S((x, z)), 1); p = point_at(lap, s, C); cps.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
cps.sort(key=lambda c: c["s"])
for i, c in enumerate(cps, 1): c["id"] = i
print("checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps))

# ---------------------------------------------------------------- closures: the drive west of the join (Round 67's), and the drive past the departure (the summit lot's end)
# Round 73 A1 (Dan's BUG-001 / BUG-002: both barriers stood across the race's own road): the lap runs ON the drive for a way either side of
# where it joins / leaves it (the cut lines meet the drive at a slant), so each barrier is walked on along the drive, away from the lap, to the
# first place where the whole barrier (the road's width plus 3 m each side, as RaceBarriers builds it) is CLOSURE_CLEAR m or more off the lap.
CLOSURE_CLEAR = 8.0
lap_dense = resample(lap, 1.0)
def lap_gap(p0, t_):
    ac = (-t_[1], t_[0]); h = roads[ROAD]["width"] / 2 + 3.0
    return min(min(math.dist((p0[0] + ac[0] * h * k / 4, p0[1] + ac[1] * h * k / 4), q) for q in lap_dense) for k in range(-4, 5))
def closure(s_from, step, cid, label):
    s_ = s_from
    while 0 < s_ < rc[-1]:
        p0 = point_at(road, s_, rc); p1 = point_at(road, s_ - step * 2.0, rc); t_ = unit((p0[0] - p1[0], p0[1] - p1[1]))
        if lap_gap(p0, t_) >= CLOSURE_CLEAR: break
        s_ += step
    j_s = s_from - step * 20.0
    print(f"closure {cid}: {abs(s_ - j_s):.0f} m along the drive from the lap's join, {lap_gap(p0, t_):.1f} m clear of the lap")
    return {"road": "Kennesaw Mountain Drive", "osm_id": roads[ROAD]["osm_id"], "x": round(p0[0], 2), "z": round(p0[1], 2), "dx": round(t_[0], 4), "dz": round(t_[1], 4),
            "width_m": roads[ROAD]["width"], "junction_s": round(S(point_at(road, j_s, rc)), 1), "contact": [round(p1[0], 1), round(p1[1], 1)], "arc_m": round(abs(s_ - j_s), 1),
            "barrier_on": "Kennesaw Mountain Drive", "id": cid, "label": label}
# (Round 67's B2 across the drive past the departure is gone: since Round 68 the lap runs up the drive to its very end at the summit lot, so B2 stood on
# the lap (BUG-002); past it is only the summit lot's loop, a dead end, so nothing needs closing there)
closures = [closure(JOIN_S - 20.0, -1.0, "B1", "Kennesaw Mountain Drive to the visitor center")]
junctions = [{"s": c["junction_s"], "roads": [c["road"]], "pos": c["contact"]} for c in closures]

# ---------------------------------------------------------------- the AI: held speeds (lap stations)
zones = []
def Z(id_, s0, s1, speed): zones.append({"id": id_, "s0": round(min(s0, s1), 1), "s1": round(max(s0, s1), 1), "speed": speed})
for j in jumps:
    a_leg = leg_s[j["leg"]][0]
    Z(f"{j['id']} {j['name']}: the run-up", max(a_leg + 5, j["sL"] - 240), j["sL"] + 1.0, AI_LIP[j["id"]])
for k in ck:
    if legno[k] == 0 and (k == 0 or legno[k - 1] != 0):
        e = k
        while e < len(lap) - 1 and legno[e + 1] == 0: e += 1
        Z(f"the bend at lap {C[k]:.0f} m", C[k] - 8, C[e] + 8, 14.0 if (C[e] - C[k]) > 30 else 20.0)
zones = [z for z in zones if z["s1"] > z["s0"]]
zones.sort(key=lambda z: z["s0"])
widths = [{"id": "the descent", "s0": round(s_cut0, 1), "s1": round(s_cut1, 1), "half": 4.0}]

# ---------------------------------------------------------------- the world pieces
gk = [k for k in range(len(lap)) if kind[k] == "cut"]
# (the grade starts 40 m back on the drive and runs 40 m on along it past the join: its ends' flat discs lie on the drive, at the drive's height)
gk = [k for k in range(len(lap)) if kind[k] == "road" and C[gk[0]] - 40.0 <= C[k] < C[gk[0]]] + gk + [k for k in range(len(lap)) if kind[k] == "road" and C[gk[-1]] < C[k] <= C[gk[-1]] + 40.0]
g_pts = [lap[k] for k in gk]; g_y = [prof[k] for k in gk]
halves, blends = [], []
for k in gk:
    h = BENCH_HALF
    if legno[k] == 0 and kind[k] == "cut": h = HAIRPIN_HALF
    if any(j["sL"] + j["xa"] - 30 <= C[k] <= j["sL"] + j["x_end"] for j in jumps): h = LAND_HALF
    # Round 70 (F2, Dan's BUG-004 at (-950, -5726): "the road shouldn't be doing this. It should be flat while still keeping this jump"): J1's run-up
    # and kicker run beside the drive's last stretch to the summit lot, the drive's center 2 to 11 m off the line, so the drive lay half on the
    # bench and half down its bank and rolled; the bench there is as wide as the drive's far edge and 2 m more (the line's profile unchanged)
    # (Round 73 A2: no longer needed, the line now leaves the drive on its own bench; the widening had spread the bench over the drive)
    halves.append(h); blends.append(round(max(BENCH_BLEND, SIDE_SLOPE * abs(prof[k] - tg[k])), 2))
# a per-point half width eased along the line (no step in the bench's edge)
halves = gsm_list(halves, 6.0); halves = [round(h, 2) for h in halves]; blends = [round(b, 2) for b in gsm_list(blends, 4.0)]
# where the line runs on or beside the drive (the departure, the join) the drive's own levelled surface is kept: those points take the built ground's
# height in the map build (the drive is levelled before this grade), eased into the line's profile over the next 40 m
on_road = [nearest_s(road, q)[0] < 9.0 for q in g_pts]
g_first = next((i for i, o in enumerate(on_road) if not o), 0); g_last = next((i for i, o in enumerate(reversed(on_road)) if not o), 0)
# Round 73 (A2, the check's Street Classic: 25 degrees of roll where the line leaves the drive): at the departure only the points on the drive itself
# (2.5 m of its middle) take the built ground's height; past them the line's own profile (the drive's height at its nearest point) holds the bench
# level with the drive (the built ground a few meters off it is the sidehill, which pulled the drive's edge down)
g_first = next((i for i, q in enumerate(g_pts) if nearest_s(road, q)[0] >= 2.5), 0)
# (never into a kicker: the drive's height only up to 45 m before the first kicker's foot, from 45 m after the last jump's catch)
g_first = min(g_first, max(0, min(range(len(gk)), key=lambda i: abs(C[gk[i]] - (jumps[0]["sL"] - jumps[0]["Lk"] - 45.0)))))
g_last = g_last if not jumps[-1]["shaped"] else min(g_last, max(0, len(gk) - 1 - min(range(len(gk)), key=lambda i: abs(C[gk[i]] - (jumps[-1]["sL"] + jumps[-1]["x_end"] + 45.0)))))
# Round 73 A2: the drive from where the line has left it (KEEP_FROM_M off; the check's second drive: tied to SEP_M it left 20 m of the drive's edge
# graded to the drive's raw ground, not its built surface: 41 degrees of roll) to the summit lot is kept off
KEEP_FROM_M = 8.0   # (the drive keeps its own surface, its width and a meter more)
_ks = nearest_s(road, lap[next(k for k in ck if C[k] > C[k_dep] and nearest_s(road, lap[k])[0] >= KEEP_FROM_M)])[1]
keep_off = flat(resample(cut(road, _ks - 4.0, None), 4.0))
grades = [{"id": "the Kennesaw Mountain race's lines", "points": flat(g_pts), "y": [round(v, 3) for v in g_y], "half": BENCH_HALF, "blend": BENCH_BLEND, "halves": halves, "blends": blends,
           "ground_first": g_first, "ground_last": g_last, "ground_ease_m": 40.0, "open_ends": True, "keep_off": keep_off, "keep_off_m": roads[ROAD]["width"] / 2 + 1.0, "keep_off_fade": 10.0}]
print(f"the grade: its first {g_first} and last {g_last} points on the drive take the built drive's height")
clear = [{"id": "the descent", "points": flat(g_pts[::2] + [g_pts[-1]]), "half": CLEAR_HALF}]
breaks = [{"id": "the descent", "points": flat(g_pts[::2] + [g_pts[-1]]), "half": BREAK_HALF}]
zones_land = []
for j in jumps:
    k = idx(j["sL"]); j["lip"] = lap[k]
    j["dir"] = unit((lap[min(k + 3, len(lap) - 1)][0] - lap[max(k - 3, 0)][0], lap[min(k + 3, len(lap) - 1)][1] - lap[max(k - 3, 0)][1]))
    # the flight's corridor straight on from the lip (the flights are straight; the line is straight there too): cleared and breakable wider
    L0, dd = j["lip"], j["dir"]; reach = j["x_end"] + 40
    clear.append({"id": f"{j['id']}'s flight", "points": flat([L0, (L0[0] + dd[0] * reach, L0[1] + dd[1] * reach)]), "half": CLEAR_HALF + 4.0})
    breaks.append({"id": f"{j['id']}'s flight", "points": flat([L0, (L0[0] + dd[0] * (reach + 60), L0[1] + dd[1] * (reach + 60))]), "half": BREAK_HALF + 6.0})
    poly_l, poly_r = [], []
    for s in np.arange(j["sL"] + max(8.0, j["xa"] - 40.0), j["sL"] + j["x_end"] + 0.1, 4.0):
        p = point_at(lap, s, C); q = point_at(lap, s + 1.0, C); t = unit((q[0] - p[0], q[1] - p[1])); nn = (-t[1], t[0])
        poly_l.append((p[0] + nn[0] * (LAND_HALF + 2.0), p[1] + nn[1] * (LAND_HALF + 2.0))); poly_r.append((p[0] - nn[0] * (LAND_HALF + 2.0), p[1] - nn[1] * (LAND_HALF + 2.0)))
    zones_land.append({"id": j["id"] + " " + j["name"], "poly": flat(poly_l + poly_r[::-1])})
jumps_out = [{"id": j["id"], "look": j["look"], "name": j["name"], "lip": [round(j["lip"][0], 2), round(j["lip"][1], 2)], "dir": [round(j["dir"][0], 4), round(j["dir"][1], 4)],
              "lip_y": round(j["yL"], 3), "angle_deg": j["ang"], "height": round(j["yL"] - tg[idx(j["sL"])], 2), "kicker_m": round(j["Lk"], 1), "dig": [0.0, 0.0],
              "s_lip": round(j["sL"], 1), "catch_from_m": round(j["xa"], 1), "catch_to_m": round(j["xb"], 1), "outrun_to_m": round(j["x_end"], 1)} for j in jumps]
track = {"note": "Round 68: the Kennesaw Mountain race's world pieces, from Tools/mapgen/make_mountain_route.py (MapBuilder.Mountain.cs). Coordinates are map meters (= Unity x, z).",
         "grades": grades, "clear_lines": clear, "break_lines": breaks, "landing_zones": zones_land, "jumps": jumps_out, "route_name": SETTINGS["name"]}
lead = [point_at(lap, L_lap - LEAD_IN_M + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0))] + [lap[0]]
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_mountain_route.py from Data/map.json (OSM's Kennesaw Mountain Drive; the descent's lines cut through the park's woods). Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": 0.0, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": junctions, "loop_pieces": [{"osm_id": roads[ROAD]["osm_id"], "name": "Kennesaw Mountain Drive", "reverse": False, "length_m": round(cum(road_run)[-1], 1)},
                                                                {"osm_id": 0, "name": "the descent", "reverse": False, "length_m": round(s_cut1 - s_cut0, 1)}],
    "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": [], "ai_zones": zones, "ai_widths": widths, "reset_skip": [],
})
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print(f"{len(zones)} AI zones; wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    os.makedirs(os.path.join(ROOT, "local/round68"), exist_ok=True)
    x0, x1, z0, z1 = -1300, -350, -6200, -5150
    i0, i1 = int((x0 - OX) / SX), int((x1 - OX) / SX); j0, j1 = int((z0 - OZ) / SZ), int((z1 - OZ) / SZ)
    A = H[j0:j1:3, i0:i1:3].astype(float) / 65535 * (HHI - HLO) + HLO
    fig, ax = plt.subplots(figsize=(18, 18 * (z1 - z0) / (x1 - x0)))
    ax.imshow(A, origin="lower", extent=(x0, x1, z0, z1), cmap="terrain")
    ax.contour(np.linspace(x0, x1, A.shape[1]), np.linspace(z0, z1, A.shape[0]), A, levels=range(300, 700, 10), colors="k", linewidths=.3)
    ax.plot(*zip(*pts(trails[30]["points"])), color="m", lw=1)
    ax.plot(*zip(*lap), "r-", lw=1.4)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g")
    for j in jumps: ax.plot(*j["lip"], "k^", ms=10); ax.text(j["lip"][0] + 6, j["lip"][1] + 4, j["id"], fontsize=12)
    for zl in zones_land: ax.fill(*zip(*pts(zl["poly"])), color="y", alpha=.4)
    for p in bl: ax.fill(*zip(*p), color="k")
    ax.plot(*lap[0], "ks", ms=10)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    plt.savefig(os.path.join(ROOT, "local/round68/layout.png"), dpi=60, bbox_inches="tight")
    fig, axs = plt.subplots(len(jumps), 1, figsize=(16, 4.2 * len(jumps)))
    for ax, j in zip(axs, jumps):
        s0, s1 = j["sL"] - 260, j["sL"] + j["x_end"] + 40
        kk2 = [k for k in range(len(lap)) if s0 <= C[k] <= s1]
        ax.plot([C[k] - j["sL"] for k in kk2], [tg[k] for k in kk2], color="#999", lw=1, label="ground")
        ax.plot([C[k] - j["sL"] for k in kk2], [prof[k] for k in kk2], "k-", lw=1.5, label="the line")
        for vname, fl in design.items():
            f = fl.get(j["id"])
            if not f: continue
            xs = np.linspace(0, f["dist_m"], 120); ax.plot(xs, [traj(j["yL"], j["ang"], f["lip_speed"], x) for x in xs], "--", lw=.9, label=f"{vname} {f['lip_speed']} m/s")
        ax.set_title(f"{j['id']} {j['name']}"); ax.set_aspect("equal"); ax.grid(True, alpha=.3); ax.legend(fontsize=7)
    plt.tight_layout(); plt.savefig(os.path.join(ROOT, "local/round68/profile.png"), dpi=60)
    fig, ax = plt.subplots(figsize=(30, 6))
    ax.plot([C[k] for k in kk], [tg[k] for k in kk], color="#999", lw=1); ax.plot([C[k] for k in kk], [prof[k] for k in kk], "k-", lw=1.2)
    for j in jumps: ax.axvline(j["sL"], color="r", lw=.6)
    for k in ck:
        if legno[k] == 0 and legno[k - 1] != 0: ax.axvline(C[k], color="b", lw=.6)
    ax.grid(True, alpha=.3); plt.savefig(os.path.join(ROOT, "local/round68/descent.png"), dpi=50, bbox_inches="tight")
    print("pictures: local/round68/layout.png, profile.png")
