"""Round 18, items 3 and 5: the Noonday Creek trail (track B, as Dan chose it) and its race route.

Hand choices are the constants below (the line as marked in Round 18 item 2b, the jumps, crossings, checkpoints, the
start); everything else is computed from the data, so this re-runs after any mapgen run. It writes:
  Data/tracks/noonday_creek.json   what the map builder builds (MapBuilder.CreekTrack.cs): the trail's centerline, the
                                   tree-clearing half width along it, the jumps, the road jump J4, landing zones
  Data/routes/noonday_creek.json   the race route (RaceRoute.cs format): the lap, start, checkpoints, the pipe shortcut P1,
                                   the vehicles allowed (small ones only), the AI's trail settings
Picture for checking: local/round18/noonday_creek_layout.png (git-ignored: it shows houses), with --plot.

A narrow woods trail, not a road (Dan, 2026-10-11): the line is moved up to 2.5 m either way round tree trunks (a cheapest
path over lateral offsets, 2 m stations), so few trees have to go; trunks still within CLEAR_HALF of the final line are
removed by the map build and counted there.

Usage: python Tools/mapgen/make_creek_track.py [--plot]
"""
import json, math, os, struct, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_TRACK = os.path.join(ROOT, "Data/tracks/noonday_creek.json")
OUT_ROUTE = os.path.join(ROOT, "Data/routes/noonday_creek.json")
PIPES = os.path.join(ROOT, "Data/pipes/pipes.json")

meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
raw = open(os.path.join(ROOT, "Data/trees.bin"), "rb").read()
TREES = np.frombuffer(raw, dtype=np.uint8, offset=8).reshape(-1, 12)[:, :8].copy().view("<f4").reshape(-1, 2).astype(np.float64)

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)

# ---------------------------------------------------------------- hand choices
# The line in travel order (Round 18 item 2b, proposal B as marked in cyan): up the creek's west side, the hairpin across
# Noonday Creek, between the big lake and the small pond, down the east side, the run to J4, over New Chastain Road,
# the south loop (down the creek's east side, C4 at the bottom, up the west side) and back north in the creek bed under the
# two bridges, up its bank.
J4_LIP = (2494.5, 774.5)                       # the road jump's lip, on the north verge of New Chastain Road
J4_TO = (2486.0, 700.0)                        # it points here (along the trail on the far side)
_d4 = np.array(J4_TO) - np.array(J4_LIP); J4_DIR = _d4 / np.linalg.norm(_d4)
def j4(s): return tuple(np.array(J4_LIP) + J4_DIR * s)
CONTROL = [
    (2380, 840), (2374, 900), (2366, 960), (2352, 1040), (2342, 1100), (2333, 1168), (2322, 1230), (2306, 1280),
    (2290, 1325), (2288, 1348), (2300, 1360), (2340, 1358), (2392, 1362), (2440, 1380), (2475, 1362), (2495, 1310),
    (2510, 1230), (2520, 1140), (2527, 1050), (2532, 960), (2537, 890), (2526, 860), (2512, 834),
    j4(-34), j4(-20), j4(0), j4(30), j4(60),
    (2483, 680), (2486, 645), (2493, 612), (2505, 585), (2525, 560), (2545, 530), (2558, 500), (2564, 470), (2560, 448),
    (2545, 440), (2528, 440), (2510, 452), (2502, 475), (2498, 500), (2482, 525), (2466, 560), (2452, 600), (2438, 640),
    (2432, 680), (2430, 705), (2432, 728), (2430, 745), (2428, 770), (2426, 790), (2418, 802), (2400, 815), (2385, 828),
]
# features, by where they are (projected onto the final line). Jumps: lip station from the point; heights in meters.
JUMPS = [   # id, kind, x, z, lip height, run-up length, table length, landing length, look
    ("J1", "tabletop", 2372, 920, 1.2, 4.5, 5.0, 7.0, "dirt"),
    ("J2", "tabletop", 2512, 1210, 1.0, 4.0, 3.0, 6.0, "logs"),
    ("J3", "kicker", 2534, 950, 0.8, 3.5, 0.0, 4.0, "spoil"),
    ("J5", "tabletop", 2545, 530, 1.2, 4.5, 5.0, 7.0, "dirt"),
    ("J6", "tabletop", 2458, 586, 1.0, 4.0, 3.0, 6.0, "logs"),
]
CROSSINGS = [("C1", 2333, 1168), ("C2", 2306, 1360), ("C3", 2443, 1379), ("C4", 2545, 440), ("C5", 2432, 728), ("C6", 2420, 800)]
CHECKPOINTS = [(1, 2338, 1130, "up the creek"), (2, 2340, 1358, "at the hairpin"), (3, 2516, 1170, "down the east side"),
               (4, 2558, 500, "south loop"), (5, 2466, 560, "south loop, west side")]
GATE_AFTER_J1 = 90.0         # the start / finish this far past J1's lip (a straight on the creek's west side)
GRID_BACK = 75.0             # the grid this far before the gate (lead-in 35 m + the lap's first 40 m)
CLEAR_HALF = 1.8             # trunks closer than this to the line go (an ATV anywhere on the 2.5 m tread, 0.5 m off its middle: 0.5 + 0.73 + a trunk's 0.3 m + 0.25 m; Round 18 race check: 1.5 m caught the AI)
WIDE_HALF = 2.6              # at the jumps and fords: two small vehicles side by side (2 x 0.8 m + room)
STEP = 1.0
SETTINGS = {
    "name": "Noonday Creek", "direction": "forward", "directions": ["forward"], "laps": 3,
    "gate_half_width_m": 5.0, "checkpoint_half_width_m": 5.0, "gate_below_m": 6.0, "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    # Dan: "perhaps even we restrict it to motorcycles and rtvs"; Claude added the mower (1.15 m wide, it fits)
    "vehicles": ["atv", "moto", "scrambler", "drifter", "mower"],   # the first is the one a player in a car is put in
    "trail": True, "ai_half_width_m": 1.25, "ai_lane_m": 0.25, "ai_pass_lane_m": 0.45,
}

# ---------------------------------------------------------------- geometry helpers
def resample(pts, step, closed=False):
    p = np.array(pts, float)
    if closed: p = np.vstack([p, p[:1]])
    seg = np.linalg.norm(np.diff(p, axis=0), axis=1); c = np.concatenate([[0], np.cumsum(seg)])
    n = max(2, int(round(c[-1] / step)) + (0 if closed else 1))
    s = np.linspace(0, c[-1], n, endpoint=not closed)
    return np.stack([np.interp(s, c, p[:, 0]), np.interp(s, c, p[:, 1])], 1)
def chaikin(p, it=4):
    for _ in range(it):
        q = np.roll(p, -1, 0); p = np.stack([0.75 * p + 0.25 * q, 0.25 * p + 0.75 * q], 1).reshape(-1, 2)
    return p
def normals(p):
    t = np.roll(p, -1, 0) - np.roll(p, 1, 0); t /= np.linalg.norm(t, axis=1)[:, None]
    return np.stack([-t[:, 1], t[:, 0]], 1), t
def project(p, q):
    """station (index into the closed polyline p at 'STEP' spacing, as a float) of the point nearest q"""
    d = np.linalg.norm(p - np.array(q), axis=1); i = int(np.argmin(d))
    return i, float(d[i])

# ---------------------------------------------------------------- 1. the base line, smoothed, at 2 m
base = resample(chaikin(resample(CONTROL, 6.0, True), 3), STEP, True)
N = len(base); L = N * STEP
nrm, tan = normals(base)
def idx_of(q): return project(base, q)[0]
lip4 = idx_of(J4_LIP)

# frozen stretches (offset 0): J4 from its run-up to the landing, the creek bed under the bridges (C5 to C6), each
# jump's run-up to landing, each ford
frozen = np.zeros(N, bool)
def freeze(i0, i1):
    for k in range(i0, i1 + 1): frozen[k % N] = True
freeze(lip4 - 40, lip4 + 64)
freeze(idx_of((2432, 712)), idx_of((2420, 801)))
# jumps: no freeze, but straight (at most 0.1 m sideways per meter from the run-up to the landing)
steady = np.zeros(N, bool)
for j in JUMPS:
    k = idx_of((j[2], j[3]))
    for q in range(k - 12, k + 20): steady[q % N] = True

# ---------------------------------------------------------------- 2. round the trunks: cheapest lateral offsets
OFFS = np.arange(-2.5, 2.501, 0.1)
near = [None] * N
for k in range(N):
    d = np.linalg.norm(TREES - base[k], axis=1); near[k] = TREES[d < 6.0]
cost = np.zeros((N, len(OFFS)))
for k in range(N):
    for a, o in enumerate(OFFS):
        q = base[k] + nrm[k] * o
        c = 0.12 * o * o
        if len(near[k]):
            dd = np.linalg.norm(near[k] - q, axis=1)
            c += 25.0 * np.sum(dd < CLEAR_HALF + 0.7) + np.sum(np.clip(2.6 - dd, 0, None) ** 2) * 0.8   # CLEAR_HALF + 0.7: a margin, the smoothing below rounds the swerves a little
        if frozen[k] and abs(o) > 1e-6: c += 1e6
        cost[k, a] = c
# the loop is cut open in J4's frozen stretch (offset 0 there), so the path starts and ends at 0 (1 m stations)
start = lip4
order = [(start + k) % N for k in range(N + 1)]
INF = 1e18; dmax = 0.2   # at most 0.2 m sideways per meter (Round 18 race check: 0.4 made kinks a racer cannot follow)
acc = np.full(len(OFFS), INF); acc[np.argmin(np.abs(OFFS))] = 0.0
back = []
for k in order[1:]:
    nxt = np.full(len(OFFS), INF); bp = np.zeros(len(OFFS), int)
    for a, o in enumerate(OFFS):
        ok = np.abs(OFFS - o) <= (0.1 if steady[k] else dmax) + 1e-6
        tr = acc + 5.0 * (OFFS - o) ** 2; tr[~ok] = INF
        b = int(np.argmin(tr)); nxt[a] = tr[b] + cost[k, a]; bp[a] = b
    acc = nxt; back.append(bp)
a = int(np.argmin(np.abs(OFFS))); off = np.zeros(N)
for k, bp in zip(reversed(order[1:]), reversed(back)):
    off[k] = OFFS[a]; a = bp[a]
# a Gaussian on the offsets (sigma 2.5 m) so the line winds smoothly, never kinks
kern = np.exp(-0.5 * (np.arange(-8, 9) * STEP / 2.5) ** 2); kern /= kern.sum()
off_s = np.array([np.sum(kern * off[(k + np.arange(-8, 9)) % N]) for k in range(N)])
off_s[frozen] = 0.0
line = base + nrm * off_s[:, None]
line = resample(line, 1.0, True)   # the final line, 1 m apart
N1 = len(line); L1 = float(np.sum(np.linalg.norm(np.diff(np.vstack([line, line[:1]]), axis=0), axis=1)))
nrm1, tan1 = normals(line)

# ---------------------------------------------------------------- 3. stations on the final line
def st(q): return project(line, q)
lip4_s = st(J4_LIP)[0]
j1_s = st((JUMPS[0][2], JUMPS[0][3]))[0]
gate_i = (j1_s + int(GATE_AFTER_J1)) % N1
first = (gate_i - 40) % N1                      # the lap file starts 40 m before the gate
line = np.roll(line, -first, 0); nrm1 = np.roll(nrm1, -first, 0); tan1 = np.roll(tan1, -first, 0)
def S(q): return st(q)[0]
gate_s = 40.0

clear = np.full(N1, CLEAR_HALF)
def widen(s0, s1, half):
    for k in range(int(s0), int(s1) + 1): clear[k % N1] = max(clear[k % N1], half)
jumps = []
for jid, kind, x, z, h, up, table, down, look in JUMPS:
    s = S((x, z))   # the mark is where the take-off rises
    lip = s + up
    jumps.append({"id": jid, "kind": kind, "look": look, "s_start": s, "s_lip": lip, "height": h, "table": table, "down": down, "width": 5.0})
    widen(s - 6, lip + table + down + 12, WIDE_HALF)
crossings = []
for cid, x, z in CROSSINGS:
    s = S((x, z)); crossings.append({"id": cid, "s": s}); widen(s - 8, s + 8, WIDE_HALF)
widen(gate_s - GRID_BACK - 5, gate_s + 5, WIDE_HALF)   # the grid (RaceController moves a slot toward the line where it is not clear)
lip4 = S(J4_LIP)
widen(lip4 - 30, lip4 + 70, 3.5)
c5a, c6 = S((2432, 712)), S((2420, 801))

# AI: a held speed at each jump (the crest rule would otherwise brake it to a crawl over every lip)
ai_zones = [{"id": j["id"], "s0": j["s_start"] - 35, "s1": j["s_lip"] + 2, "speed": 20.0} for j in jumps]
ai_zones.append({"id": "J4", "s0": lip4 - 45, "s1": lip4 + 1, "speed": 24.0})

# landing zones (the landing carry, RampHold): after every jump, and J4's long one over the road and down the far side
def strip(s0, s1, half):
    a, b = int(s0) % N1, int(s1) % N1
    ks = [(a + k) % N1 for k in range(((b - a) % N1) + 1)]
    left = [line[k] + nrm1[k] * half for k in ks]; right = [line[k] - nrm1[k] * half for k in reversed(ks)]
    return [round(float(v), 2) for p in left + right for v in p]
landings = [{"id": j["id"], "poly": strip(j["s_lip"] + 1, j["s_lip"] + 40, 3.5)} for j in jumps]
landings.append({"id": "J4", "poly": [round(float(v), 2) for p in [j4(14) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(110) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6,
                                                                  j4(110) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(14) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6] for v in p]})

# ---------------------------------------------------------------- 4. the pipe shortcut P1 (its layout: Data/pipes/pipes.json)
pipes = json.load(open(PIPES, encoding="utf-8"))
p1 = [p for p in pipes["pipes"] if p["id"] == "P1"][0]
A = np.array(p1["a"]); B = np.array(p1["b"])
# from the trail on the creek's west side, straight along the pipe's axis (25 m: down the bank and across the creek on the
# graded ford, Data/pipes/pipes.json approach_a) into the inlet, through, out on the pond's west shore, then left up the dry
# slope west of the pond (the pond's edge ends about z 1340) and onto the trail past it. Trunks on the way in and out go
# ('clear_lines', as on the trail; nothing else marks it: shortcuts are found, not signposted)
uA = (B - A) / np.linalg.norm(B - A)
# the turn off the trail: a circular arc of 11 m radius, tangent to the trail where it leaves and to the pipe's axis where it
# ends (the station whose arc ends nearest the axis); a sharper corner sent vehicles wide into the trunks
R_IN = 11.0
def rot2(v, a): c_, s_ = math.cos(a), math.sin(a); return np.array([v[0] * c_ - v[1] * s_, v[0] * s_ + v[1] * c_])
best = None
for k in range(S((2309.0, 1272.0)), S((2296.0, 1312.0)) + 1):
    P0 = line[k % N1]; h0 = line[(k + 1) % N1] - line[(k - 1) % N1]; h0 = h0 / np.linalg.norm(h0)
    phi = math.atan2(h0[0] * uA[1] - h0[1] * uA[0], h0 @ uA)   # signed turn from the trail's heading to the axis
    c = P0 + np.array([-h0[1], h0[0]]) * R_IN * np.sign(phi)
    end = c + rot2(P0 - c, phi)
    off = abs(float((end - A) @ np.array([-uA[1], uA[0]]))); along = float((A - end) @ uA)
    if along < 14.0: continue
    if best is None or off < best[0]: best = (off, k, c, phi, end)
_, leave, c_in, phi_in, end_in = best
arc = [c_in + rot2(line[leave % N1] - c_in, phi_in * t / 12.0) for t in range(13)]
join = S((2402.7, 1367.6))
path_in = [line[(leave - 8) % N1]] + [tuple(q) for q in arc] + [tuple(A - uA * 8.0), tuple(A)]
print(f"P1 way in: leaves the trail at s {leave}, an arc of {R_IN:.0f} m turning {math.degrees(phi_in):.0f} degrees, onto the pipe's axis {float((A - end_in) @ uA):.0f} m before the inlet (off it by {best[0]:.2f} m)")
# back onto the trail on a curve (a 70 degree corner at the join sent the AI wide into a trunk)
path_out = [tuple(B), tuple(B + uA * 2.0), (2399.5, 1321.0), (2398.5, 1330.0), (2400.5, 1341.0), (2402.0, 1353.0), (2404.5, 1362.0), (2408.5, 1368.0),
            line[(join + 6) % N1], line[(join + 16) % N1]]
path = resample(path_in + [tuple(A + uA * t) for t in np.arange(5.0, np.linalg.norm(B - A), 5.0)] + path_out, 2.0)
clear_lines = [{"points": [round(float(v), 2) for q in resample(path_in[1:], 1.0) for v in q], "half": 2.8},
               {"points": [round(float(v), 2) for q in resample(path_out, 1.0) for v in q], "half": 2.8}]
sc_p1 = {"id": "p1_pipe", "name": "The drain pipe", "direction": "forward", "path": [round(float(v), 2) for q in path for v in q],
         "half_width_m": 3.0, "entry_inset_m": 14.0, "entry_margin_m": 2.0, "forgives": [2], "ai_speed": 16.0}

# ---------------------------------------------------------------- 5. write
def flat(p): return [round(float(v), 2) for q in p for v in q]
lead = [line[(N1 - 35 + k) % N1] for k in range(0, 36, 1)]   # the lap's last 35 m, up to its first point
cps = [{"id": cid, "s": float(S((x, z))), "pos": [x, z], "label": label} for cid, x, z, label in CHECKPOINTS]
track = {
    "note": "Round 18 (items 3, 5): the Noonday Creek trail, made by Tools/mapgen/make_creek_track.py. Map x, z in meters; s = meters along 'line' from its first point (the lap's own stations, as in the route file). Built by MapBuilder.CreekTrack.cs.",
    "line": flat(line), "clear_half": [round(float(v), 2) for v in clear], "tread_half_m": 1.25,
    "jumps": jumps, "crossings": crossings, "creek_bed": {"s0": c5a, "s1": c6},
    "road_jump": {"id": "J4", "lip": [J4_LIP[0], J4_LIP[1]], "dir": [round(float(J4_DIR[0]), 5), round(float(J4_DIR[1]), 5)], "s_lip": lip4,
                  "ramp_m": 28.0, "lip_above_road_m": 1.6, "width_m": 5.0, "slope_start": 0.11, "slope_lip": 0.25,
                  "landing_from_m": 27.0, "landing_slope": 0.09, "landing_half_m": 5.0},
    "landing_zones": landings, "clear_lines": clear_lines,
}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_creek_track.py. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L1, 1), "loop": flat(np.vstack([line, line[:1]])), "gate_s": gate_s,
    "lead_in": flat(lead), "lead_in_length_m": 35.0, "checkpoints": cps, "junctions": [], "loop_pieces": [],
    "official_shortcuts": [sc_p1], "closures": [], "ai_zones": ai_zones,
    # for the checks (Round18Runner): J4 and the creek-bed run, in lap stations
    "j4": {"lip": [J4_LIP[0], J4_LIP[1]], "dir": [round(float(J4_DIR[0]), 5), round(float(J4_DIR[1]), 5)], "s_lip": lip4, "road_far_edge_m": 26.0},
    "creek_bed_s": [c5a, c6],
})
os.makedirs(os.path.dirname(OUT_TRACK), exist_ok=True)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)

# ---------------------------------------------------------------- report
def trunks_within(r):
    n = 0
    for k in range(N1):
        d = np.linalg.norm(TREES - line[k], axis=1)
        n += 0
    return n
from collections import OrderedDict
gone = set(); gone_wide = set()
for k in range(N1):
    d = np.linalg.norm(TREES - line[k], axis=1)
    for t in np.nonzero(d < clear[k])[0]: (gone if d[t] < CLEAR_HALF else gone_wide).add(int(t))
base_hits = set()
for k in range(N):
    d = np.linalg.norm(TREES - base[k], axis=1); base_hits.update(np.nonzero(d < CLEAR_HALF)[0].tolist())
print(f"lap {L1:.0f} m; offsets: max {np.max(np.abs(off_s)):.2f} m, mean {np.mean(np.abs(off_s)):.2f} m")
print(f"trunks within {CLEAR_HALF} m of the line: {len(base_hits)} on the marked line, {len(gone)} after moving it round them; "
      f"{len(gone_wide)} more at the wide spots (jumps, fords, the grid, J4); in all {len(gone) + len(gone_wide - gone)}")
for j in jumps: print(f"  {j['id']} {j['kind']}: start s {j['s_start']}, lip s {j['s_lip']:.1f}, {j['height']} m, look {j['look']}")
print(f"  J4 lip s {lip4}; gate s {gate_s}; creek bed s {c5a}..{c6}; P1 path {len(path)} points, leaves the lap at s {leave}, rejoins at s {join}")
for c in cps: print(f"  CP{c['id']} s {c['s']:.0f} ({c['label']})")
print(f"wrote {OUT_TRACK} and {OUT_ROUTE}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(10, 22))
    x0, x1, z0, z1 = 2260, 2600, 420, 1420
    i0, i1, k0, k1 = int(x0 / SX), int(x1 / SX), int(z0 / SZ), int(z1 / SZ)
    ax.imshow(H[k0:k1, i0:i1], extent=(i0 * SX, i1 * SX, k0 * SZ, k1 * SZ), origin="lower", cmap="terrain", vmin=270, vmax=292)
    t = TREES[(TREES[:, 0] > x0) & (TREES[:, 0] < x1) & (TREES[:, 1] > z0) & (TREES[:, 1] < z1)]
    ax.plot(t[:, 0], t[:, 1], ".", color="darkgreen", ms=1.5)
    for w in m["water"]: q = np.array(w["outline"]).reshape(-1, 2); ax.fill(q[:, 0], q[:, 1], color="deepskyblue", alpha=0.5)
    for r in m["roads"] + m["bridges"]: q = np.array(r["points"]).reshape(-1, 2); ax.plot(q[:, 0], q[:, 1], "k-", lw=1.5)
    for b in m["buildings"]: q = np.array(b["footprint"]).reshape(-1, 2); ax.fill(q[:, 0], q[:, 1], color="peru")
    ax.plot(base[:, 0], base[:, 1], "c-", lw=0.6)
    ax.plot(line[:, 0], line[:, 1], "-", color="saddlebrown", lw=1.5)
    for tt in gone: ax.plot(*TREES[tt], "rx", ms=4)
    ax.plot(path[:, 0], path[:, 1], "m-", lw=1.5)
    for j in jumps: q = line[int(j["s_lip"]) % N1]; ax.text(q[0] + 4, q[1], j["id"], color="red", fontsize=8)
    for c in cps: ax.text(c["pos"][0] + 4, c["pos"][1], f"CP{c['id']}", color="green", fontsize=8)
    q = line[int(gate_s)]; ax.text(q[0] + 4, q[1], "START", fontsize=8)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal")
    os.makedirs(os.path.join(ROOT, "local/round18"), exist_ok=True)
    fig.savefig(os.path.join(ROOT, "local/round18/noonday_creek_layout.png"), dpi=110, bbox_inches="tight")
