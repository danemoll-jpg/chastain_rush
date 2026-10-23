"""Round 18, items 3 and 5: the Noonday Creek trail (track B, as Dan chose it) and its race route. Round 19, items 1 and 2:
wider (a 4.5 m tread, no solid trunk within 3.5 m of the line, the small trees left within 6 m breakable) and four big jumps.

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
JUMPS = [   # id, kind, x, z, lip height, run-up length, table length, landing length, look (Round 19: J3 gave way to the fallen giant;
            # J2, 63 m before the step-down, threw flat-out vehicles across the bend and past BJ2's kicker: gone)
    ("J1", "tabletop", 2372, 920, 1.2, 4.5, 5.0, 7.0, "dirt"),
    ("J5", "tabletop", 2545, 530, 1.2, 4.5, 5.0, 7.0, "dirt"),
    ("J6", "tabletop", 2458, 586, 1.0, 4.0, 3.0, 6.0, "logs"),
]
# Round 19, item 2 (Dan: "I feel like we should also add some big jumps too"): four big ones, each a different idea, sized for two
# seconds of air or more at 24 m/s (the AI holds 24 into them; 'vmax': no vehicle leaves the lip faster, BigJump.cs, so nobody
# overshoots the cleared landing). Lip (x, z) on the Round 18 line; the take-off points along the line there.
#   id, look, lip x, z, lip height (m over the ground under it), lip angle (deg), take-off length, width, landing from / to (m past the lip),
#   vmax (Round 19 checks: BJ2 and BJ4 at 24, the trail bends soon after their landings)
BIG_JUMPS = [
    ("BJ1", "footbridge", 2329.7, 1186.4, 2.2, 23.0, 16.5, 7.0, 12.0, 62.0, 27.0),    # the side gully: a washed-out footbridge, its middle gone (Round 20: 7 m wide, its rails clear of the line)
    ("BJ2", "stepdown", 2517.6, 1143.4, 1.2, 20.0, 7.5, 7.0, 10.0, 66.0, 24.0),   # off the top of the bank, down to the creek flats
    ("BJ3", "fallen_tree", 2529.6, 996.5, 1.7, 25.0, 8.0, 7.0, 10.0, 66.0, 27.0),     # an earth ramp heaped against a huge fallen tree
    ("BJ4", "outcrop", 2485.0, 668.2, 1.6, 24.0, 8.0, 7.0, 10.0, 66.0, 24.0),     # a granite slab tilted up out of the hump
]
BIG_DESIGN, BIG_VMAX = 24.0, 27.0
# Round 21, the reverse race (Dan: "a straight up reverse version of the noonday creek trail"). Every forward take-off faced the reverse
# lap with a wall (the lip's end; BJ1's broken span with a tunnel under it). Each big jump gets a back: from its lip down to the ground
# at about 20 degrees (BJ1: earth and broken planks heaped against the broken end, down into the gully; BJ2: earth down the bank; BJ3:
# the earth behind the log, now 20 degrees instead of 37; BJ4: the slab's back as a sloped rock shoulder). Coming the other way that
# back is the take-off (a BigJump the other way at the same lip, its own angle and speed cap), and the forward take-off is the landing;
# a forward vehicle that comes up short lands on the back. Reverse speed caps: BJ1 to BJ3 land on 60 m of straight trail behind them;
# behind BJ4 the trail bends after 20 m, so its reverse launch is held to 17 m/s.
#   id: (back angle deg, reverse lip angle deg, reverse vmax)
BIG_REVERSE = {"BJ1": (20.0, 21.0, 24.0), "BJ2": (20.0, 21.0, 24.0), "BJ3": (20.0, 21.0, 24.0), "BJ4": (20.0, 20.0, 17.0)}
# J4 in reverse (Dan: "The road jump over New Chastain Road in reverse too"): a second dirt kicker, the mirror of J4, up the south bank to
# a lip on the south verge 31.5 m past J4's lip (New Chastain Road's two carriageways lie 5 to 27 m past it), over the road onto J4's
# own kicker (its slope is the landing). Both kickers get a 4 m back down to the verge facing the road, so a vehicle short of either
# jump drives across the road and over the other one (a hump, never a wall). The reverse launch is aimed (BigJump: anyone over its lip
# at 10 m/s or more leaves at 23 to 25 m/s, 20 degrees): it clears J4's lip by about 2 m and comes down 13 to 22 m down J4's slope.
RJ4_LIP_M, RJ4_BACK_M, RJ4_ANGLE, RJ4_VMIN, RJ4_VMAX = 31.5, 4.0, 20.0, 23.0, 25.0
CROSSINGS = [("C1", 2333, 1168), ("C2", 2306, 1360), ("C3", 2443, 1379), ("C4", 2545, 440), ("C5", 2432, 728), ("C6", 2420, 800)]
CHECKPOINTS = [(1, 2338, 1130, "up the creek"), (2, 2340, 1358, "at the hairpin"), (3, 2516, 1170, "down the east side"),
               (4, 2558, 500, "south loop"), (5, 2466, 560, "south loop, west side")]
GATE_AFTER_J1 = 90.0         # the start / finish this far past J1's lip (a straight on the creek's west side)
GRID_BACK = 75.0             # the grid this far before the gate (lead-in 35 m + the lap's first 40 m)
CLEAR_HALF = 3.5             # Round 19 (Dan: "probably needs a little wider track"): no solid trunk within 3.5 m of the line (the tread is 4.5 m)
WIDE_HALF = 4.5              # at the jumps, fords and the grid: two small vehicles side by side with room
BEND_WIDEN = 30.0            # on a bend of radius r, the cleared half width grows by BEND_WIDEN / r (at most 1.5 m)
BREAK_HALF = 10.0            # the trees left within this of the line snap when hit (MapBuilder: TreeField.breakable; Round 21 follow-ups: was 6)
TREAD_HALF = 2.25
STEP = 1.0
SETTINGS = {
    # Round 62 (Dan, 2026-10-23 02:34): renamed "Noonday Creek Extension" (it is the trail's proposed extension, not built yet; the real trail round
    # Town Center is its own race now: make_trail_route.py); its records, ghosts and replays are kept under the old name (records_as)
    "name": "Noonday Creek Extension", "records_as": "Noonday Creek", "direction": "forward", "directions": ["forward", "reverse"], "laps": 3,   # Round 21: and reverse
    "gate_half_width_m": 5.0, "checkpoint_half_width_m": 5.0, "gate_below_m": 6.0, "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    # Dan: "perhaps even we restrict it to motorcycles and rtvs"; Claude added the mower (1.15 m wide, it fits)
    "vehicles": ["atv", "moto", "scrambler", "drifter", "mower"],   # the first is the one a player in a car is put in
    "trail": True, "ai_half_width_m": 2.25, "ai_lane_m": 0.7, "ai_pass_lane_m": 1.4,   # Round 19: the 4.5 m tread
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
for b in BIG_JUMPS:   # Round 19: the big jumps on the smoothed line itself (straight), from the run-up to the landing's end
    k = idx_of((b[2], b[3])); freeze(k - int(b[6]) - 30, k + int(b[9]) + 6)

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
# Round 19: wider on bends (radius from three points 6 m apart)
for k in range(N1):
    a, b, c = line[(k - 6) % N1], line[k], line[(k + 6) % N1]
    ar = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]))
    r = 1e9 if ar < 1e-6 else np.linalg.norm(b - a) * np.linalg.norm(c - b) * np.linalg.norm(c - a) / (2 * ar)
    clear[k] += min(1.5, BEND_WIDEN / r)
# Round 21 follow-ups (Dan: "can we make the breakable trees a little bit wider around the tracks?"): breakable within BREAK_HALF (10 m)
# everywhere, and BREAK_WIDE (15 m) on the outside of the bends where the Round 21 flat-out laps hit trunks (the stop points of that
# check, by the bends Dan named): forward the C3 bend, the east leg and the south loop; reverse the lake corner, the hairpin and the
# south loop's east side. Around each point, 30 m either way, wherever the line bends (radius under 120 m), 15 m on both sides (the
# outside only, first: the follow-ups' laps still hit trunks on the other side, where the vehicles run wide out of S-bends).
BREAK_WIDE = 15.0
BEND_HITS = [   # (x, z) where a flat-out lap stopped on a trunk (Round 21 check: forward, then reverse)
    (2458.4, 1376.7), (2450.1, 1383.2), (2523.8, 1222.4), (2546.1, 526.8), (2564.3, 494.3), (2514.0, 424.3),
    (2468.4, 1375.7), (2412.3, 1385.7), (2418.9, 1386.4), (2279.8, 1341.6), (2552.4, 440.3), (2511.5, 593.4), (2501.2, 594.0),
    (2520.7, 584.6), (2521.2, 596.3)]   # the last two: the follow-ups' own reverse laps
# trunks the follow-ups' flat-out laps still stopped on, 12 to 20 m off the line where the vehicles run wide (made breakable by position)
HIT_TRUNKS = [(2521.6, 597.0), (2511.0, 593.8), (2518.5, 596.9), (2521.1, 586.8), (2418.6, 1384.6), (2524.2, 1221.5), (2528.7, 1211.8), (2473.3, 1383.9), (2411.6, 827.3)]
break_left = np.zeros(N1); break_right = np.zeros(N1); wide_bends = []
for hx, hz in BEND_HITS:
    k0 = S((hx, hz)); n_out = 0
    for k in range(k0 - 30, k0 + 31):
        a, b, c = line[(k - 6) % N1], line[k % N1], line[(k + 6) % N1]
        cr = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])   # > 0: a left bend (forward), so its outside is the right
        ar = abs(cr); r = 1e9 if ar < 1e-6 else np.linalg.norm(b - a) * np.linalg.norm(c - b) * np.linalg.norm(c - a) / (2 * ar)
        if r > 120.0: continue
        # the outside of the bend; and the inside too: the laps hit trunks on both sides of these bends (S-bends, wide exits)
        break_right[k % N1] = BREAK_WIDE; break_left[k % N1] = BREAK_WIDE
        n_out += 1
    wide_bends.append((k0, n_out))
print(f"  breakable trees: {BREAK_HALF:.0f} m either side everywhere; {BREAK_WIDE:.0f} m both sides of the bends at stations " + ", ".join(f"{k} ({n} m)" for k, n in wide_bends))
def widen(s0, s1, half):
    for k in range(int(s0), int(s1) + 1): clear[k % N1] = max(clear[k % N1], half)
jumps = []
for jid, kind, x, z, h, up, table, down, look in JUMPS:
    s = S((x, z))   # the mark is where the take-off rises
    lip = s + up
    jumps.append({"id": jid, "kind": kind, "look": look, "s_start": s, "s_lip": lip, "height": h, "table": table, "down": down, "width": 6.0})
    widen(s - 6, lip + table + down + 12, WIDE_HALF)
crossings = []
for cid, x, z in CROSSINGS:
    s = S((x, z)); crossings.append({"id": cid, "s": s}); widen(s - 8, s + 8, WIDE_HALF)
widen(gate_s - GRID_BACK - 5, gate_s + 5, WIDE_HALF)
widen(gate_s - 5, gate_s + SETTINGS["reverse_grid_back_m"] + 10, WIDE_HALF)   # Round 21: the reverse grid, 45 m past the gate (forward stations)
# Round 19: the big jumps. The take-off points along the line at the lip; trajectories over the real ground (g 9.81, no drag, as
# the game) at each lip speed for the report; the flight's corridor cleared of trunks (a straight line, 'clear_lines' below)
def ballistic(P, d, y0, v, th):
    vx, vy = v * math.cos(th), v * math.sin(th); t = 0.0; top = y0
    while t < 6.0:
        t += 0.01; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        if t > 0.1 and y <= ground(*q): return t, float(vx * t), top - y0
    return t, float(vx * t), top - y0
big = []; big_clear = []
for bid, look, x, z, h, ang, ramp, width, l0, l1, vmax in BIG_JUMPS:
    s = S((x, z)); P = line[s % N1]; d = line[(s + 3) % N1] - line[(s - 3) % N1]; d = d / np.linalg.norm(d)
    y0 = ground(*P) + h
    table = {}
    for v in (14, 18, 21, 24, 27):
        t, dist, top = ballistic(P, d, y0, v, math.radians(ang)); table[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
    back_ang, rev_ang, rev_vmax = BIG_REVERSE[bid]
    rtable = {}
    for v in (14, 18, 21, rev_vmax):
        t, dist, top = ballistic(P, -d, y0, v, math.radians(rev_ang)); rtable[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
    rev_far = max(a_[1] for a_ in rtable.values())
    big.append({"id": bid, "look": look, "s_lip": float(s), "lip": [round(float(P[0]), 2), round(float(P[1]), 2)], "dir": [round(float(d[0]), 5), round(float(d[1]), 5)],
                "height": h, "lip_y": round(float(y0), 3), "angle_deg": ang, "ramp_m": ramp, "width": width, "land_from": l0, "land_to": l1, "vmax": vmax, "design": BIG_DESIGN,
                "flights": table, "back_angle": back_ang, "rev_angle": rev_ang, "rev_vmax": rev_vmax, "rev_land_to": round(rev_far + 6.0, 1), "rev_flights": rtable})   # Round 21
    widen(s - rev_far - 8, s + 2, WIDE_HALF)   # Round 21: the reverse landing
    big_clear.append({"points": [round(float(v), 2) for q in [P, P - d * (rev_far + 8.0)] for v in q], "half": 5.5})
    widen(s - ramp - 12, s + l1 + 8, WIDE_HALF)
    big_clear.append({"points": [round(float(v), 2) for q in [P - d * (ramp + 8.0), P + d * (l1 + 6.0)] for v in q], "half": 5.5})
    print(f"  {bid} {look}: lip s {s} ({P[0]:.1f}, {P[1]:.1f}), {h} m at {ang} deg; air / distance / top at the lip speed: " +
          ", ".join(f"{v} m/s {a[0]:.2f} s {a[1]:.0f} m {a[2]:.1f} m" for v, a in table.items()) +
          f"; REVERSE (off its back at {rev_ang} deg, held to {rev_vmax}): " + ", ".join(f"{v} m/s {a[0]:.2f} s {a[1]:.0f} m" for v, a in rtable.items()))   # the grid (RaceController moves a slot toward the line where it is not clear)
lip4 = S(J4_LIP)
widen(lip4 - 30, lip4 + 70, 3.5)
c5a, c6 = S((2432, 712)), S((2420, 801))

# AI: a held speed at each jump (the crest rule would otherwise brake it to a crawl over every lip)
ai_zones = [{"id": j["id"], "s0": j["s_start"] - 35, "s1": j["s_lip"] + 2, "speed": 20.0} for j in jumps]
ai_zones.append({"id": "J4", "s0": lip4 - 45, "s1": lip4 + 1, "speed": 24.0})
ai_zones += [{"id": b["id"], "s0": b["s_lip"] - b["ramp_m"] - 40, "s1": b["s_lip"] + 2, "speed": BIG_DESIGN} for b in big]   # Round 19

# landing zones (the landing carry, RampHold): after every jump, and J4's long one over the road and down the far side
def strip(s0, s1, half):
    a, b = int(s0) % N1, int(s1) % N1
    ks = [(a + k) % N1 for k in range(((b - a) % N1) + 1)]
    left = [line[k] + nrm1[k] * half for k in ks]; right = [line[k] - nrm1[k] * half for k in reversed(ks)]
    return [round(float(v), 2) for p in left + right for v in p]
landings = [{"id": j["id"], "poly": strip(j["s_lip"] + 1, j["s_lip"] + 40, 3.5)} for j in jumps]
def big_zone(b):   # Round 19: from just past the take-off to beyond the longest flight, 13 to 15 m wide
    P, d = np.array(b["lip"]), np.array(b["dir"]); n = np.array([-d[1], d[0]])
    return [round(float(v), 2) for q in [P + d * 3 + n * 6.5, P + d * (b["land_to"] + 10) + n * 7.5, P + d * (b["land_to"] + 10) - n * 7.5, P + d * 3 - n * 6.5] for v in q]
landings += [{"id": b["id"], "poly": big_zone(b)} for b in big]
def big_zone_rev(b):   # Round 21: the reverse landing, behind the lip (the forward take-off and its run-up)
    P, d = np.array(b["lip"]), -np.array(b["dir"]); n = np.array([-d[1], d[0]])
    return [round(float(v), 2) for q in [P + d * 3 + n * 6.5, P + d * (b["rev_land_to"] + 4) + n * 7.5, P + d * (b["rev_land_to"] + 4) - n * 7.5, P + d * 3 - n * 6.5] for v in q]
landings += [{"id": b["id"] + "R", "poly": big_zone_rev(b)} for b in big]
landings.append({"id": "J4", "poly": [round(float(v), 2) for p in [j4(14) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(110) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6,
                                                                  j4(110) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(14) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6] for v in p]})

# Round 21: J4 in reverse (RJ4): the flight over the road from the south verge, onto J4's slope; its landing zone beyond J4's foot
rj4_lip = np.array(J4_LIP) + J4_DIR * RJ4_LIP_M
rj4_y = ground(*(np.array(J4_LIP) + J4_DIR * 22.7)) + 1.6   # the south carriageway's height (+ J4's 1.6 m), as the build does for J4
rtab4 = {}
for v in (RJ4_VMIN, RJ4_VMAX):
    t, dist, top = ballistic(rj4_lip, -J4_DIR, rj4_y, v, math.radians(RJ4_ANGLE)); rtab4[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
print(f"  RJ4 (J4 in reverse): lip {RJ4_LIP_M} m past J4's on the south verge at about {rj4_y:.2f} m, {RJ4_ANGLE} deg, {RJ4_VMIN} to {RJ4_VMAX} m/s over the road: " +
      ", ".join(f"{v} m/s {a[0]:.2f} s {a[1]:.0f} m ({a[1] - RJ4_LIP_M:.0f} m past J4's lip)" for v, a in rtab4.items()))
landings.append({"id": "RJ4", "poly": [round(float(v), 2) for p in [j4(-6) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(-36) + np.array([-J4_DIR[1], J4_DIR[0]]) * 6,
                                                                   j4(-36) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6, j4(-6) - np.array([-J4_DIR[1], J4_DIR[0]]) * 6] for v in p]})

# ---------------------------------------------------------------- 4. the pipe shortcut P1 (its layout: Data/pipes/pipes.json)
pipes = json.load(open(PIPES, encoding="utf-8"))
p1 = [p for p in pipes["pipes"] if p["id"] == "P1"][0]
A = np.array(p1["a"]); B = np.array(p1["b"])
# from the trail on the creek's west side, straight along the pipe's axis (25 m: down the bank and across the creek on the
# graded ford, Data/pipes/pipes.json approach_a) into the inlet, through, out on the pond's west shore and over the pond
# (Round 20, below) onto the trail's east side. Trunks on the way in and out go
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
path_in = [line[(leave - 8) % N1]] + [tuple(q) for q in arc] + [tuple(A - uA * 8.0), tuple(A)]
print(f"P1 way in: leaves the trail at s {leave}, an arc of {R_IN:.0f} m turning {math.degrees(phi_in):.0f} degrees, onto the pipe's axis {float((A - end_in) @ uA):.0f} m before the inlet (off it by {best[0]:.2f} m)")
# Round 20 (BUG-002, Dan: "Lets make a ramp right here so that you don't have to drive through the lake"): out of the pipe
# straight on along its axis, up a concrete outfall apron at the mouth (7 m, turned up to 24 degrees at its lip, 1.5 m over the
# shore) and OVER the small pond to its east shore, then on across the little stream there and onto the trail's east side (the
# old way out, left up the pond's west shore, ran into the water). An aimed launch (BigJump: 'vmin' to 'vmax' off the lip; a
# vehicle under 10 m/s just rolls off into the shallow edge): every vehicle clears the water and comes down before the stream;
# then a right turn onto the trail (brake after landing: it runs south-south-east there).
OUT_RAMP, OUT_RISE, OUT_ANG, OUT_VMIN, OUT_VMAX, OUT_W = 7.0, 1.5, 24.0, 23.5, 26.0, 8.5
P_out = B + uA * OUT_RAMP; y_out = ground(*B) + OUT_RISE
table = {}
for v in (16, 20, OUT_VMIN, OUT_VMAX):
    t, dist, top = ballistic(P_out, uA, y_out, v, math.radians(OUT_ANG)); table[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
outfall = {"id": "P1J", "look": "outfall", "s_lip": -1.0, "lip": [round(float(P_out[0]), 2), round(float(P_out[1]), 2)], "dir": [round(float(uA[0]), 5), round(float(uA[1]), 5)],
           "height": OUT_RISE, "lip_y": round(float(y_out), 3), "angle_deg": OUT_ANG, "ramp_m": OUT_RAMP, "width": OUT_W, "land_from": 30.0, "land_to": 70.0,
           "vmin": OUT_VMIN, "vmax": OUT_VMAX, "design": OUT_VMIN, "flights": table}
big.append(outfall); landings.append({"id": "P1J", "poly": big_zone(outfall)})
print(f"  P1J outfall: lip ({P_out[0]:.1f}, {P_out[1]:.1f}) at {y_out:.2f} m, {OUT_ANG} deg; air / distance / top: " +
      ", ".join(f"{v} m/s {a[0]:.2f} s {a[1]:.0f} m {a[2]:.1f} m" for v, a in table.items()))
# after landing a right turn onto the trail's east side (it runs south-south-east there), eased over 40 m
join2 = S((2494.6, 1307.2))
path_out = [tuple(B)] + [tuple(B + uA * k) for k in (2.0, OUT_RAMP, 20.0, 35.0, 50.0, 62.0)] + [(2473.0, 1327.5), (2482.5, 1322.5), (2489.0, 1314.5),
            line[join2 % N1], line[(join2 + 8) % N1], line[(join2 + 18) % N1]]
path = resample(path_in + [tuple(A + uA * t) for t in np.arange(5.0, np.linalg.norm(B - A), 5.0)] + path_out, 2.0)
clear_lines = [{"points": [round(float(v), 2) for q in resample(path_in[1:], 1.0) for v in q], "half": 3.5},
               {"points": [round(float(v), 2) for q in resample(path_out, 1.0) for v in q], "half": 3.5}] + big_clear
clear_lines.append({"points": [round(float(v), 2) for q in [P_out - uA * 2.0, P_out + uA * (outfall["land_to"] + 6.0)] for v in q], "half": 5.5})
sc_p1 = {"id": "p1_pipe", "name": "The drain pipe", "direction": "forward", "path": [round(float(v), 2) for q in path for v in q],
         "half_width_m": 3.0, "entry_inset_m": 14.0, "entry_margin_m": 2.0, "forgives": [2], "ai_speed": 16.0}

# ---------------------------------------------------------------- 4b. N2 and N5 (Round 20 item 4, decided by Dan 2026-10-12: "N2 is
# the only one that even makes sense ... we could put in N5 if you miss the pipe shortcut")
# Each: off the trail on a circular arc (radius R, tangent to the trail where it leaves) onto a kicker's axis, the kicker 8 m long,
# an aimed launch (BigJump: anyone over the lip at 10 m/s or more leaves it at vmin to vmax), the flight over Noonday Creek.
#   N2 "Low creek leap" (official, forgives CP4): from the east side of the south loop across the creek's gorge to the west side.
#      The proposal's lip (2528, 539) sat 9 m from the trail facing west: a 125 degree turn in 10 m that nothing can drive. Here the
#      lip is 20 m further in, at the end of a 12 m arc; the loop's two sides are only 35 m apart there, so the launch is held to
#      15 to 17 m/s (about 1.5 to 1.7 s and 23 to 28 m in the exe: down short of the west side's trail with room to turn onto it;
#      at 17 to 19 m/s three of the five ran on past the turn into the bank; the proposal's 23 to 25 m/s would carry a vehicle 15 m
#      into the woods beyond). Then a right bend onto the trail heading north, before CP5.
#   N5 "Ford leap" (part of the lap: forgives nothing, 'lap_option'): a log ramp beside the hairpin's ford (C2), for those who
#      did not take P1; it lands back on the trail 8 to 13 m before CP2, which is passed as usual. The AI takes it ('ai_take').
def rot2l(v, a): c_, s_ = math.cos(a), math.sin(a); return np.array([v[0] * c_ - v[1] * s_, v[0] * s_ + v[1] * c_])
def leap(jid, leave_xz, R, bearing, straight, h, ang, vmin, vmax, ramp=8.0):
    k = S(leave_xz); P0 = line[k % N1]; h0 = line[(k + 2) % N1] - line[(k - 2) % N1]; h0 = h0 / np.linalg.norm(h0)
    b = math.radians(bearing); d = np.array([math.sin(b), math.cos(b)])
    phi = math.atan2(h0[0] * d[1] - h0[1] * d[0], h0 @ d)
    c = P0 + np.array([h0[1], -h0[0]]) * R * (-np.sign(phi))   # the centre on the side the arc turns to
    arc = [c + rot2l(P0 - c, phi * t / 16.0) for t in range(17)]
    foot = arc[-1] + d * straight; L = foot + d * ramp; y0 = ground(*L) + h
    table = {}
    for v in sorted({vmin, vmax, round((vmin + vmax) / 2, 1)}):
        t, dist, top = ballistic(L, d, y0, v, math.radians(ang)); table[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
    far = max(a_[1] for a_ in table.values())
    bj = {"id": jid, "look": "", "s_lip": -1.0, "lip": [round(float(L[0]), 2), round(float(L[1]), 2)], "dir": [round(float(d[0]), 5), round(float(d[1]), 5)],
          "height": h, "lip_y": round(float(y0), 3), "angle_deg": ang, "ramp_m": ramp, "width": 6.0, "land_from": 12.0, "land_to": round(far + 8.0, 1),
          "vmin": vmin, "vmax": vmax, "design": vmin, "flights": table}
    print(f"  {jid}: leaves the trail at s {k} on a {R:.0f} m arc turning {math.degrees(phi):.0f} degrees, lip ({L[0]:.1f}, {L[1]:.1f}) at {y0:.2f} m facing {bearing} degrees; air / distance / top: " +
          ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.0f} m {a_[2]:.1f} m" for v, a_ in table.items()))
    return k, arc, foot, L, d, bj, far
# N2
k2, arc2, foot2, L2, d2, n2, far2 = leap("N2", (2520.24, 565.95), 12.0, 245.0, 4.0, 1.8, 24.0, 15.0, 17.0)
n2["look"] = "washout"
n2["back_angle"] = 20.0   # Round 21 follow-ups: an earth back down into the gorge, as RC's (a vehicle short into the gorge from RC drives out up it)
land2 = L2 + d2 * 25.0                               # about the longest landing (17 m/s in the exe): the turn back onto the trail starts here
k2j = S((2468.0, 538.5))                             # where the right turn meets the trail (heading north)
P2a = line[k2j % N1]; h2a = line[(k2j + 2) % N1] - line[(k2j - 2) % N1]; h2a = h2a / np.linalg.norm(h2a)
mid2 = land2 + d2 * 4.0                              # a little straight, then one smooth bend onto the trail (a fitted quadratic)
path2 = [line[(k2 - 14 + i) % N1] for i in range(0, 14, 2)] + arc2 + [foot2, L2] + [L2 + d2 * t for t in (8.0, 16.0, 25.0)] + \
        [(1 - u) ** 2 * mid2 + 2 * (1 - u) * u * (mid2 + d2 * 6.0) + u * u * P2a for u in np.linspace(0.15, 1.0, 7)] + [line[(k2j + i) % N1] for i in (4, 8, 12)]
path2 = resample(path2, 2.0)
s2 = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(path2, axis=0), axis=1))])
def st_on(P, cum, q): return float(cum[int(np.argmin(np.linalg.norm(P - np.array(q), axis=1)))])
sc_n2 = {"id": "n2_creek_leap", "name": "Low creek leap", "direction": "forward", "path": [round(float(v), 2) for q in path2 for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(path2, s2, foot2 + d2 * 4.0), 1), "entry_margin_m": 1.0, "forgives": [4], "ai_speed": 15.0,
         # the AI: 11 m/s round the 12 m arc, 14 up the kicker (the launch takes it to 15), slow for the bend after landing
         "ai_profile": [round(v, 1) for v in (st_on(path2, s2, arc2[0]) - 4.0, 11.0, st_on(path2, s2, arc2[-1]), 11.0, st_on(path2, s2, foot2), 14.0,
                                              st_on(path2, s2, L2) + 2.0, 14.0, st_on(path2, s2, mid2), 9.0, st_on(path2, s2, P2a), 10.0)]}
big.append(n2); landings.append({"id": "N2", "poly": big_zone(n2)})
clear_lines.append({"points": [round(float(v), 2) for q in resample([line[(k2 - 6) % N1]] + arc2 + [foot2, L2], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in [L2, L2 + d2 * (far2 + 8.0)] for v in q], "half": 6.0})
clear_lines.append({"points": [round(float(v), 2) for q in resample([mid2, mid2 + d2 * 6.0, P2a], 1.0) for v in q], "half": 8.0})   # 8 m: a fast vehicle swings wide across the trail here (the exe check: the Turf Rocket into a trunk 8.5 m past it)
# the hint at the turn-off (Dan's shortcut-hints rule): a road-closed barricade knocked over on its side at the trail's edge where the
# arc leaves it, and tire tracks from there over the bank to the kicker's foot
n_left2 = np.array([-arc2[1][1] + arc2[0][1], arc2[1][0] - arc2[0][0]]); n_left2 /= np.linalg.norm(n_left2)
bar2 = arc2[0] - n_left2 * 3.6 + (arc2[0] - arc2[2]) / np.linalg.norm(arc2[0] - arc2[2]) * 2.0
n2["hint"] = {"kind": "barricade", "x": round(float(bar2[0]), 2), "z": round(float(bar2[1]), 2), "yaw": round(math.degrees(math.atan2(*(arc2[2] - arc2[0]))) + 70.0, 1)}
n2["tracks"] = [round(float(v), 2) for q in resample([line[(k2 - 8) % N1]] + arc2 + [foot2], 1.0) for v in q]
# N5
k5, arc5, foot5, L5, d5, n5, far5 = leap("N5", (2291.19, 1334.04), 10.0, 65.0, 0.0, 1.4, 20.0, 18.0, 22.0)
n5["look"] = "logramp"
k5j = S((2345.0, 1358.0))
path5 = [line[(k5 - 14 + i) % N1] for i in range(0, 14, 2)] + arc5 + [foot5, L5] + [L5 + d5 * t for t in (8.0, 16.0, 24.0)] + [line[(k5j + i) % N1] for i in (-2, 4, 10)]
path5 = resample(path5, 2.0)
s5 = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(path5, axis=0), axis=1))])
sc_n5 = {"id": "n5_ford_leap", "name": "Ford leap", "direction": "forward", "path": [round(float(v), 2) for q in path5 for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(path5, s5, foot5 + d5 * 4.0), 1), "entry_margin_m": 1.0, "forgives": [], "lap_option": True,
         "ai_take": 1.0, "ai_speed": 15.0,
         "ai_profile": [round(v, 1) for v in (st_on(path5, s5, arc5[0]) - 4.0, 10.0, st_on(path5, s5, arc5[-1]), 11.0, st_on(path5, s5, L5) + 2.0, 15.0)]}
big.append(n5); landings.append({"id": "N5", "poly": big_zone(n5)})
clear_lines.append({"points": [round(float(v), 2) for q in resample([line[(k5 - 6) % N1]] + arc5 + [foot5, L5], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in [L5, L5 + d5 * (far5 + 6.0)] for v in q], "half": 5.5})
n5["tracks"] = [round(float(v), 2) for q in resample([line[(k5 - 8) % N1]] + arc5 + [foot5], 1.0) for v in q]
RC_RR = 10.0   # Round 21 follow-ups: RC's right arc off the trail (m)
# ---------------------------------------------------------------- 4c. RA and RC, the reverse race's shortcuts (Round 21 follow-ups, Dan's
# picks from the Part C candidates; their script went with the markers). Both 'direction': "reverse", their paths in reverse travel order.
#   RA "Pipe dive" (official, forgives reverse CP4 = id 2): off the east side's trail where P1's way out meets it, west along P1's axis
#      to a heap of creek rock on the small pond's east shore (lip 1.4 m, 22 degrees), an aimed launch (BigJump: 23.0 to 23.4 m/s, a
#      tight window: the apron is 7 m long) over the pond onto P1's outfall apron (its slope is the landing), down the outlet cut into
#      the pipe, through the clear stretch, up the inlet cut, over the rock bar and round the arc onto the west side's trail. The heap
#      stands in P1J's landing (forward flights come down on its run-up slope, a landing ramp in their direction, or past it).
#      Hint: a lamp on the outlet headwall, seen across the pond (MapBuilder.Pipes.cs), and worn tracks off the trail to the heap.
#   RC "Creek leap back" (official, forgives reverse CP2 = id 4): N2 backwards. The west side's trail runs only 12 to 15 m from the
#      gorge's edge here, so the twin heap sits between them (its foot clear of the tread, its lip on the bank's top) and the run-up
#      comes from the woods west of the trail: off the trail just past reverse CP1, a loop out to the west and round to the
#      east-north-east, across the trail and up the heap; an aimed launch (15.0 to 16.5 m/s, 24 degrees) over the gorge onto N2's own
#      heap (its slope is the landing), then left up the east side. The heap's back is an earth ramp down into the gorge (20 degrees):
#      a forward N2 flight comes down on the twin's kicker (a landing ramp in its direction) or, short, on that back, and a vehicle
#      short into the gorge drives up it and out.
#      Hint: N2's knocked-over barricade is in plain sight across the creek; worn tracks from the trail round the loop to the heap.
# RA
RA_K, RA_H, RA_TH, RA_VMIN, RA_VMAX, RA_RAMP = 35.0, 1.4, 22.0, 23.2, 23.5, 8.0   # first exe check at 36 m, 23.0 to 23.4: the player down 3.3 to 4.1 m onto the apron, the AI 1.6 m short of it
lipA = P_out + uA * RA_K; dA = -uA; yA_lip = ground(*lipA) + RA_H
nA = np.array([-uA[1], uA[0]])
ra_tab = {}
for v in (RA_VMIN, RA_VMAX):
    t, dist, top = ballistic(lipA, dA, yA_lip, v, math.radians(RA_TH)); ra_tab[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
ra = {"id": "RA", "look": "rockheap", "s_lip": -1.0, "lip": [round(float(lipA[0]), 2), round(float(lipA[1]), 2)], "dir": [round(float(dA[0]), 5), round(float(dA[1]), 5)],
      "height": RA_H, "lip_y": round(float(yA_lip), 3), "angle_deg": RA_TH, "ramp_m": RA_RAMP, "width": 6.0, "land_from": RA_K - 4.0, "land_to": RA_K + 2.0,
      "vmin": RA_VMIN, "vmax": RA_VMAX, "design": RA_VMIN, "flights": ra_tab}
footA = lipA + uA * RA_RAMP
# out of the inlet: straight on over the rock bar and up the west bank, then a wide left bend (radius 25 m) onto the trail heading south
# (P1's own way in, an 11 m arc, was too tight out of the pipe: the first flat-out check ran wide 30 m into the woods)
RA_OUT_R = 25.0
oA = A - uA * 12.0; hA = -uA; leftA = np.array([-hA[1], hA[0]]); cA = oA + leftA * RA_OUT_R
exitA = [tuple(A - uA * 6.0), tuple(oA)]; kAj = None
for deg in range(2, 121, 2):
    a_ = math.radians(deg); v_ = oA - cA; q_ = cA + np.array([v_[0] * math.cos(a_) - v_[1] * math.sin(a_), v_[0] * math.sin(a_) + v_[1] * math.cos(a_)])
    exitA.append(tuple(q_)); k_, d_ = project(line, q_)[0], float(np.min(np.linalg.norm(line - q_, axis=1)))
    if d_ < 1.5: kAj = int(np.argmin(np.linalg.norm(line - q_, axis=1))); break
exitA += [tuple(line[(kAj - i) % N1]) for i in (4, 8, 12)]
pathA = [line[(join2 + 18) % N1], line[(join2 + 8) % N1], line[join2 % N1], (2489.0, 1314.5), (2482.5, 1322.5), (2473.0, 1327.5), tuple(footA + uA * 6.0), tuple(footA), tuple(lipA),
         tuple(P_out), tuple(B)] + [tuple(B - uA * t) for t in np.arange(5.0, np.linalg.norm(B - A), 5.0)] + [tuple(A)] + exitA
pathA = resample(pathA, 2.0)
sA = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(pathA, axis=0), axis=1))])
sc_ra = {"id": "ra_pipe_dive", "name": "Pipe dive", "direction": "reverse", "path": [round(float(v), 2) for q in pathA for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(pathA, sA, footA + uA * 4.0), 1), "entry_margin_m": 1.0, "forgives": [2], "ai_speed": 16.0,
         # the AI: 12 m/s round the turn off the trail, 22 up the heap (the launch takes it to 23), 14 through the pipe, 9 for the arc at the end
         "ai_profile": [round(v, 1) for v in (st_on(pathA, sA, (2489.0, 1314.5)) - 4.0, 12.0, st_on(pathA, sA, (2473.0, 1327.5)), 13.0, st_on(pathA, sA, footA), 22.0,
                                              st_on(pathA, sA, lipA) + 1.0, 22.0, st_on(pathA, sA, B) + 4.0, 14.0, st_on(pathA, sA, A), 12.0, st_on(pathA, sA, oA), 13.0)]}
ra["tracks"] = [round(float(v), 2) for q in resample([line[join2 % N1], (2489.0, 1314.5), (2482.5, 1322.5), (2473.0, 1327.5), tuple(footA + uA * 4.0)], 1.0) for v in q]
big.append(ra)
landings.append({"id": "RA", "poly": [round(float(v), 2) for q in [P_out + uA * 3.0 + nA * 6.0, B - uA * 6.0 + nA * 4.0, B - uA * 6.0 - nA * 4.0, P_out + uA * 3.0 - nA * 6.0] for v in q]})
clear_lines.append({"points": [round(float(v), 2) for q in resample([(2473.0, 1327.5), tuple(footA + uA * 6.0), tuple(lipA)], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in resample(exitA, 1.0) for v in q], "half": 3.5})
print(f"  RA pipe dive: heap lip ({lipA[0]:.1f}, {lipA[1]:.1f}) at {yA_lip:.2f} m, {RA_TH} deg, {RA_VMIN} to {RA_VMAX} m/s: " +
      ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.1f} m" for v, a_ in ra_tab.items()) + f" (the apron's lip is {RA_K:.0f} m away); out on a {RA_OUT_R:.0f} m bend onto the trail at s {kAj}; {float(sA[-1]):.0f} m of line")
# RC
L2b = np.array(n2["lip"]); d2b = np.array(n2["dir"]); dC = -d2b; nC = np.array([-d2b[1], d2b[0]])
RC_S, RC_H, RC_TH, RC_VMIN, RC_VMAX, RC_RAMP = 18.0, 1.4, 24.0, 15.5, 16.3, 7.0   # first exe check at 15 to 16: the AI down at N2's lip, the player 7 to 8 m past it
lipC = L2b + d2b * RC_S; footC = lipC - dC * RC_RAMP; yC_lip = ground(*lipC) + RC_H
def n2_surface(q):   # N2's kicker (from its foot up to its lip), for the landing of RC's flight
    s_ = float((q - L2b) @ d2b)   # 0 at N2's lip, negative toward its foot
    if -n2["ramp_m"] <= s_ <= 0.0 and abs(float((q - L2b) @ nC)) < n2["width"] / 2:
        u = (s_ + n2["ramp_m"]) / n2["ramp_m"]; return ground(*(L2b - d2b * n2["ramp_m"])) + (n2["lip_y"] - ground(*(L2b - d2b * n2["ramp_m"]))) * u * u
    return -1e9
def ballistic2(P, d, y0, v, th, extra):
    vx, vy = v * math.cos(th), v * math.sin(th); t = 0.0
    while t < 6.0:
        t += 0.01; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t
        if t > 0.1 and y <= max(ground(*q), extra(q)): return t, float(vx * t), float((q - L2b) @ -d2b)
    return t, float(vx * t), 0.0
rc_tab = {}
for v in (RC_VMIN, RC_VMAX):
    t, dist, past = ballistic2(lipC, dC, yC_lip, v, math.radians(RC_TH), n2_surface); rc_tab[str(v)] = [round(t, 2), round(dist, 1), round(past, 1)]
rc = {"id": "RC", "look": "washout", "s_lip": -1.0, "lip": [round(float(lipC[0]), 2), round(float(lipC[1]), 2)], "dir": [round(float(dC[0]), 5), round(float(dC[1]), 5)],
      "height": RC_H, "lip_y": round(float(yC_lip), 3), "angle_deg": RC_TH, "ramp_m": RC_RAMP, "width": 6.0, "land_from": 12.0, "land_to": 30.0,
      "vmin": RC_VMIN, "vmax": RC_VMAX, "design": RC_VMIN, "flights": rc_tab, "back_angle": 20.0}
# the run-up: off the trail just past reverse CP1 (forward CP5, s 1707), a loop out west and round onto the heap's axis 27 m before its lip
kC = S((2470.5, 554.0)); hC = line[(kC - 2) % N1] - line[(kC + 2) % N1]; hC = hC / np.linalg.norm(hC)   # the reverse heading there
RC_RUN = 38.0
TC = lipC - dC * RC_RUN
# a left turning circle (radius RC_R) tangent to the heap's axis at TC; a straight line onto it from the trail, eased off the trail's heading
RC_R = 9.0
leftC = np.array([-dC[1], dC[0]]); cC = TC + leftC * RC_R; b0 = line[kC % N1]
# a right arc off the trail (radius RC_RR) until the heading is tangent to the left circle, a straight, then the left circle (a Dubins path)
def rotc(v, a): c_, s_ = math.cos(a), math.sin(a); return np.array([v[0] * c_ + v[1] * s_, -v[0] * s_ + v[1] * c_])   # clockwise by a (x east, z north)
rightC = np.array([hC[1], -hC[0]]); cR = b0 + rightC * RC_RR
best = None
for al in np.linspace(0.0, math.radians(150), 1501):
    P_ = cR + rotc(b0 - cR, al); h_ = rotc(hC, al)
    w = cC - P_; along_ = float(w @ h_); side_ = float(w @ np.array([-h_[1], h_[0]]))   # the circle's center: ahead, and to the left by RC_R
    if along_ > 2.0 and (best is None or abs(side_ - RC_R) < best[0]): best = (abs(side_ - RC_R), al, P_, h_, along_)
_, alR, PR, hR, runR = best
T1 = PR + hR * runR; phi1 = math.atan2(*(T1 - cC)[::-1])
phiT = math.atan2(*(TC - cC)[::-1]); sweep = (phiT - phi1) % (2 * math.pi)
arcR = [cR + rotc(b0 - cR, alR * t) for t in np.linspace(0, 1, max(3, int(alR * RC_RR / 1.0)))]
straightR = [PR + hR * runR * t for t in np.linspace(0, 1, max(2, int(runR / 1.0)))]
arcC = [cC + RC_R * np.array([math.cos(phi1 + sweep * t), math.sin(phi1 + sweep * t)]) for t in np.linspace(0, 1, max(3, int(sweep * RC_R / 1.0)))]
loopC = arcR + straightR[1:] + arcC[1:]
print(f"  RC's way in: off the trail turning right {math.degrees(alR):.0f} degrees (radius {RC_RR:.0f} m), {runR:.1f} m straight, left {math.degrees(sweep):.0f} degrees (radius {RC_R:.0f} m) onto the heap's axis {RC_RUN:.0f} m before its lip")
def min_radius(P):
    P = np.asarray(P); r = 1e9
    for i in range(2, len(P) - 2):
        a, b, c = P[i - 2], P[i], P[i + 2]; ar = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]))
        if ar > 1e-6: r = min(r, np.linalg.norm(b - a) * np.linalg.norm(c - b) * np.linalg.norm(c - a) / (2 * ar))
    return r
kCj = S((2533.0, 582.0))   # the east side's trail, heading north (reverse) after the landing: a gentle left bend onto it (at (2545, 540) the flat-out check ran wide into trunks)
P2j = line[kCj % N1]
landC = L2b - d2b * 6.0
midC = landC + dC * 6.0
pathC = [line[(kC + 14 - i) % N1] for i in range(0, 14, 2)] + loopC[1:] + [tuple(footC), tuple(lipC), tuple(L2b), tuple(landC)] + \
        [(1 - u) ** 2 * midC + 2 * (1 - u) * u * (midC + dC * 14.0) + u * u * P2j for u in np.linspace(0.1, 1.0, 10)] + [line[(kCj - i) % N1] for i in (4, 8, 12)]
pathC = resample(pathC, 2.0)
sCc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(pathC, axis=0), axis=1))])
sc_rc = {"id": "rc_creek_leap_back", "name": "Creek leap back", "direction": "reverse", "path": [round(float(v), 2) for q in pathC for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(pathC, sCc, footC + dC * 3.0), 1), "entry_margin_m": 1.0, "forgives": [4], "ai_speed": 14.0,
         # the AI: 9 m/s round the loop, 15 up the heap (the launch holds it to 15 to 16.5), slow for the bend onto the east side
         "ai_profile": [round(v, 1) for v in (st_on(pathC, sCc, loopC[0]) - 4.0, 10.0, st_on(pathC, sCc, loopC[8]), 9.0, st_on(pathC, sCc, loopC[-1]), 10.0, st_on(pathC, sCc, footC), 15.0,
                                              st_on(pathC, sCc, lipC) + 2.0, 15.0, st_on(pathC, sCc, midC), 9.0, st_on(pathC, sCc, P2j), 10.0)]}
rc["tracks"] = [round(float(v), 2) for q in resample([tuple(q) for q in loopC] + [tuple(footC)], 1.0) for v in q]
big.append(rc)
landings.append({"id": "RC", "poly": [round(float(v), 2) for q in [L2b + d2b * 1.0 + nC * 6.0, L2b - d2b * 16.0 + nC * 7.0, L2b - d2b * 16.0 - nC * 7.0, L2b + d2b * 1.0 - nC * 6.0] for v in q]})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(q) for q in loopC] + [tuple(footC), tuple(lipC)], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(landC), tuple(midC), tuple(midC + dC * 14.0), tuple(P2j)], 1.0) for v in q], "half": 6.0})
# the trees within 12 m of RA's and RC's lines snap when hit (the flat-out check: a vehicle running wide out of RA's last turn and
# out of RC's landing stopped dead on trunks 13 to 16 m off the trail)
break_lines = [{"points": [round(float(v), 2) for q in pathA for v in q], "half": 12.0}, {"points": [round(float(v), 2) for q in pathC for v in q], "half": 12.0},
               {"points": [round(float(v), 2) for q in resample(exitA, 1.0) for v in q], "half": 25.0}]   # 25 m round RA's way out (out of the pipe at speed over the bank, a flat-out run went 24 m wide)
break_lines += [{"points": [hx, hz, hx + 0.01, hz], "half": 1.5} for hx, hz in HIT_TRUNKS]
print(f"  RC creek leap back: heap lip ({lipC[0]:.1f}, {lipC[1]:.1f}) at {yC_lip:.2f} m ({RC_S:.0f} m from N2's lip), {RC_TH} deg, {RC_VMIN} to {RC_VMAX} m/s: " +
      ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.1f} m, down {a_[2]:.1f} m past N2's lip" for v, a_ in rc_tab.items()) +
      f"; the loop's tightest radius {min_radius(loopC):.1f} m; heap's foot {float(np.min(np.linalg.norm(line - footC, axis=1))):.1f} m from the trail's line; {float(sCc[-1]):.0f} m of line")
# ---------------------------------------------------------------- 4d. RD and RE, the reverse race's OWN shortcuts (Round 25, Dan's pick of
# Round 24 A3's candidates: "RD and RE", RA and RC kept). Dan: "are the shortcuts on the trail the same both ways? ... I would want the
# trails to feel different". Both 'direction': "reverse" (their paths in reverse travel order), held to CLAUDE.md "Shortcut design".
#   RD "Gully leap" (official, forgives nothing: it rejoins before reverse CP2 / forward CP4): on the south loop's west side, a kicker of
#      planks on the gully's west rim aimed east over Noonday Creek's gully (about 20 m across, 3.7 m deep, banks too steep to climb) onto
#      the flat east bank, then left onto the east side's trail. The kicker's foot stands clear of the trail's tread, so the run-up comes
#      from the woods west of the trail (off the trail, round to the east, across the trail and up the planks: as RC). Short: into the
#      gully (out by the C4 ford downstream, or up the kicker's back); long: into breakable trunks. Hint: worn tracks off the trail round
#      to the kicker and a road-closed barricade knocked over where they leave the trail.
#   RE "Flood-plain run" (official, forgives the hairpin's checkpoint, as RA): coming north up the east leg, a left turn off the trail
#      west over the plateau's edge (an earth step-down: it only works this way), 100 m across the flood plain's woods on a cleared line,
#      and a log kicker on the east rim of Noonday Creek's gorge up onto the west leg's higher bank, then right onto the west leg. A line
#      of its own 40 to 50 m south of P1 / RA. Short at the gorge: into the creek, out downstream. Hint: a barricade knocked flat in a
#      gap in the plateau's trees and worn tracks into it.
def loop_in(kC, lipC, dC, RUN, RR, R, tag, quiet=False):
    """RC's way in, as a function: off the trail (reverse heading at kC) turning right (radius RR), a straight, then a left circle (radius
    R) onto the kicker's axis RUN m before its lip"""
    hC = line[(kC - 2) % N1] - line[(kC + 2) % N1]; hC = hC / np.linalg.norm(hC)
    TC = lipC - dC * RUN
    leftC = np.array([-dC[1], dC[0]]); cC = TC + leftC * R; b0 = line[kC % N1]
    rightC = np.array([hC[1], -hC[0]]); cR = b0 + rightC * RR
    best = None
    for al in np.linspace(0.0, math.radians(150), 1501):
        P_ = cR + rotc(b0 - cR, al); h_ = rotc(hC, al)
        w = cC - P_; along_ = float(w @ h_); side_ = float(w @ np.array([-h_[1], h_[0]]))
        if along_ > 2.0 and (best is None or abs(side_ - R) < best[0]): best = (abs(side_ - R), al, P_, h_, along_)
    if best is None: return ([line[kC % N1]], 999.0) if quiet else None
    _, alR, PR, hR, runR = best
    T1 = PR + hR * runR; phi1 = math.atan2(*(T1 - cC)[::-1])
    phiT = math.atan2(*(TC - cC)[::-1]); sweep = (phiT - phi1) % (2 * math.pi)
    arcR = [cR + rotc(b0 - cR, alR * t) for t in np.linspace(0, 1, max(3, int(alR * RR / 1.0)))]
    straightR = [PR + hR * runR * t for t in np.linspace(0, 1, max(2, int(runR / 1.0)))]
    arcC = [cC + R * np.array([math.cos(phi1 + sweep * t), math.sin(phi1 + sweep * t)]) for t in np.linspace(0, 1, max(3, int(sweep * R / 1.0)))]
    loop = arcR + straightR[1:] + arcC[1:] + [TC + dC * t for t in np.arange(2.0, RUN, 2.0)]
    if not quiet: print(f"  {tag}'s way in: off the trail turning right {math.degrees(alR):.0f} degrees (radius {RR:.0f} m), {runR:.1f} m straight, left {math.degrees(sweep):.0f} degrees (radius {R:.0f} m) onto the kicker's axis {RUN:.0f} m before its lip; tightest radius {min_radius(loop):.1f} m")
    return (loop, math.degrees(sweep)) if quiet else loop
def length(P): P = np.asarray(P); return float(np.sum(np.linalg.norm(np.diff(P, axis=0), axis=1)))
def trail_gap(P):
    return float(np.min(np.linalg.norm(line - np.asarray(P), axis=1)))
def forgiven(k_join, k_leave):   # the checkpoints a reverse shortcut skips: forward stations between where it rejoins and where it leaves
    return [cid for cid, x, z, _ in CHECKPOINTS if k_join < S((x, z)) < k_leave]
def bend_to(land, d, kj, out, n=8):   # a smooth bend from the landing onto the trail at kj (a quadratic through a point 6 m on)
    mid = land + d * 4.0; P = line[kj % N1]
    return [(1 - u) ** 2 * mid + 2 * (1 - u) * u * (mid + d * out) + u * u * P for u in np.linspace(0.1, 1.0, n)]
def rev_after(kj, steps=(4, 8, 12)): return [line[(kj - i) % N1] for i in steps]   # on along the trail in reverse travel

# RD
RD_H, RD_TH, RD_RAMP, RD_VMIN, RD_VMAX = 1.4, 20.0, 6.0, 21.0, 23.0
cands_D = []
for kk in range(S((2495.0, 501.0)) - 8, S((2495.0, 501.0)) + 12):
    P0 = line[kk % N1]
    for brg in range(90, 131, 2):
        d_ = np.array([math.sin(math.radians(brg)), math.cos(math.radians(brg))])
        g0 = ground(*P0); rim = None
        for s_ in np.arange(2.0, 16.0, 0.25):
            if ground(*(P0 + d_ * s_)) < g0 - 0.6: rim = s_ - 0.5; break
        if rim is None: continue
        lip_ = P0 + d_ * rim; foot_ = lip_ - d_ * RD_RAMP
        if trail_gap(foot_) < 3.1 or min(trail_gap(lip_ - d_ * t) for t in np.arange(0.0, RD_RAMP + 0.1, 1.0)) < 3.1: continue
        y0_ = max(ground(*lip_), ground(*foot_)) + RD_H; ok = True; far_ = 0.0
        for v in (RD_VMIN, RD_VMAX):
            t, dist, top = ballistic(lip_, d_, y0_, v, math.radians(RD_TH)); q = lip_ + d_ * dist
            if ground(*q) < g0 - 1.2 or trail_gap(q) < 5.0: ok = False
            far_ = max(far_, dist)
        if not ok: continue
        cands_D.append((kk, brg, lip_, d_, y0_, far_))
assert cands_D, "RD: no kicker place clear of the trail found"
# the way in that saves the most: off the trail 20 to 70 m before the kicker's trail point, a smooth line (no bend under 8 m, the last
# turn under 150 degrees) onto the axis 16 to 30 m before the lip; and the landing's way onto the east side's trail
best = None
for kk, brg, lip_, d_, y0_, far_ in cands_D:
    land_ = lip_ + d_ * min(far_, 30.0); kj_ = S(land_ + (d_ + np.array([-d_[1], d_[0]])) * 12.0)
    for off in range(20, 72, 4):
        for RUN in (16.0, 20.0, 24.0, 30.0):
            for RR, R in ((12.0, 10.0), (15.0, 12.0), (20.0, 15.0)):
                loop_, sw = loop_in(kk + off, lip_, d_, RUN, RR, R, "RD", True)
                if sw > 150.0 or min_radius(loop_) < 8.0: continue
                ln = length(loop_) + RUN + 30.0
                saves = ((kk + off - kj_) % N1) - ln
                if best is None or saves > best[0]: best = (saves, kk, brg, lip_, d_, y0_, far_, off, RUN, RR, R)
assert best is not None, "RD: no smooth way in found"
_, kD, brgD, lipD, dD, yD_lip, farD, offD, runD, rrD, rD = best
footD = lipD - dD * RD_RAMP
rd_tab = {}
for v in (RD_VMIN, RD_VMAX):
    t, dist, top = ballistic(lipD, dD, yD_lip, v, math.radians(RD_TH)); rd_tab[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
rd = {"id": "RD", "look": "logramp", "s_lip": -1.0, "lip": [round(float(lipD[0]), 2), round(float(lipD[1]), 2)], "dir": [round(float(dD[0]), 5), round(float(dD[1]), 5)],
      "height": RD_H, "lip_y": round(float(yD_lip), 3), "angle_deg": RD_TH, "ramp_m": RD_RAMP, "width": 6.0, "land_from": 12.0, "land_to": round(farD + 8.0, 1),
      "vmin": RD_VMIN, "vmax": RD_VMAX, "design": RD_VMIN, "flights": rd_tab, "back_angle": 20.0}   # its back: earth down into the gully (a vehicle short drives out up it)
kDL = kD + offD   # off the trail (forward stations: reverse travel comes down them) before the kicker's trail point
loopD = loop_in(kDL, lipD, dD, runD, rrD, rD, "RD")
landD = lipD + dD * min(farD, 30.0)
kDj = S(landD + (dD + np.array([-dD[1], dD[0]])) * 12.0)   # the east side's trail, north of the landing (left in reverse)
pathD = [line[(kDL + 14 - i) % N1] for i in range(0, 14, 2)] + loopD[1:] + [tuple(footD), tuple(lipD), tuple(landD)] + bend_to(landD, dD, kDj, 8.0) + rev_after(kDj)
pathD = resample(pathD, 2.0)
sD = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(pathD, axis=0), axis=1))])
sc_rd = {"id": "rd_gully_leap", "name": "Gully leap", "direction": "reverse", "path": [round(float(v), 2) for q in pathD for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(pathD, sD, footD + dD * 2.0), 1), "entry_margin_m": 1.0, "forgives": forgiven(kDj, kDL), "ai_speed": 14.0,
         "ai_profile": [round(v, 1) for v in (st_on(pathD, sD, loopD[0]) - 4.0, 10.0, st_on(pathD, sD, loopD[8]), 9.0, st_on(pathD, sD, footD) - 14.0, 12.0, st_on(pathD, sD, footD), RD_VMIN,
                                              st_on(pathD, sD, lipD) + 2.0, RD_VMIN, st_on(pathD, sD, landD), 9.0, st_on(pathD, sD, line[kDj % N1]), 10.0)]}
rd["tracks"] = [round(float(v), 2) for q in resample([tuple(q) for q in loopD] + [tuple(footD)], 1.0) for v in q]
n_leaveD = loopD[2] - loopD[0]; n_leaveD = n_leaveD / np.linalg.norm(n_leaveD); barD = loopD[0] - np.array([-n_leaveD[1], n_leaveD[0]]) * 3.4
rd["hint"] = {"kind": "barricade", "x": round(float(barD[0]), 2), "z": round(float(barD[1]), 2), "yaw": round(math.degrees(math.atan2(*n_leaveD)) + 70.0, 1)}
big.append(rd)
nD = np.array([-dD[1], dD[0]])
landings.append({"id": "RD", "poly": [round(float(v), 2) for q in [lipD + dD * 3.0 + nD * 6.5, lipD + dD * (farD + 10.0) + nD * 7.5, lipD + dD * (farD + 10.0) - nD * 7.5, lipD + dD * 3.0 - nD * 6.5] for v in q]})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(q) for q in loopD] + [tuple(footD), tuple(lipD)], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in [lipD, lipD + dD * (farD + 8.0)] for v in q], "half": 6.0})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(landD)] + [tuple(q) for q in bend_to(landD, dD, kDj, 8.0)], 1.0) for v in q], "half": 6.0})
print(f"  RD gully leap: kicker on the rim at ({lipD[0]:.1f}, {lipD[1]:.1f}), lip {yD_lip:.2f} m, facing {brgD} degrees, {RD_TH} deg, {RD_VMIN} to {RD_VMAX} m/s: " +
      ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.1f} m" for v, a_ in rd_tab.items()) + f"; its foot {trail_gap(footD):.1f} m from the trail's line; leaves at s {kDL}, rejoins at s {kDj} " +
      f"({(kDL - kDj) % N1} m of lap for {float(sD[-1]):.0f} m of line); forgives {sc_rd['forgives']}")

# RE
k700 = S((2508.0, 1255.0)); k300 = S((2347.0, 1262.0))
uE = line[k300 % N1] - line[k700 % N1]; uE = uE / np.linalg.norm(uE)
RE1_H, RE1_TH, RE1_RAMP, RE1_VMIN, RE1_VMAX = 0.8, 12.0, 8.0, 18.0, 22.0
RE2_H, RE2_TH, RE2_RAMP, RE2_VMIN, RE2_VMAX = 1.6, 22.0, 8.0, 17.0, 19.0
lipE1 = line[k700 % N1] + uE * 30.0; lipE2 = line[k700 % N1] + uE * 158.0
def kicker(jid, look, lip, d, h, th, ramp, vmin, vmax):
    foot = lip - d * ramp; y0 = max(ground(*lip), ground(*foot)) + h; tab = {}
    for v in (vmin, vmax):
        t, dist, top = ballistic(lip, d, y0, v, math.radians(th)); tab[str(v)] = [round(t, 2), round(dist, 1), round(top, 1)]
    far = max(a_[1] for a_ in tab.values())
    return {"id": jid, "look": look, "s_lip": -1.0, "lip": [round(float(lip[0]), 2), round(float(lip[1]), 2)], "dir": [round(float(d[0]), 5), round(float(d[1]), 5)],
            "height": h, "lip_y": round(float(y0), 3), "angle_deg": th, "ramp_m": ramp, "width": 6.0, "land_from": 8.0, "land_to": round(far + 8.0, 1),
            "vmin": vmin, "vmax": vmax, "design": vmin, "flights": tab}, foot, far
re1, footE1, farE1 = kicker("RE1", "stepdown", lipE1, uE, RE1_H, RE1_TH, RE1_RAMP, RE1_VMIN, RE1_VMAX)
re2, footE2, farE2 = kicker("RE2", "logramp", lipE2, uE, RE2_H, RE2_TH, RE2_RAMP, RE2_VMIN, RE2_VMAX)
re2["back_angle"] = 20.0   # a vehicle short into the gorge drives up the kicker's back and out
# the way in: a left arc (radius 12) off the trail (heading north, reverse) onto the axis, ended at least 10 m before the step-down's foot
nE = np.array([-uE[1], uE[0]]); bestE = None
for kk in range(k700 + 4, k700 + 40):
    P0 = line[kk % N1]; h0 = line[(kk - 2) % N1] - line[(kk + 2) % N1]; h0 = h0 / np.linalg.norm(h0)
    phi = math.atan2(h0[0] * uE[1] - h0[1] * uE[0], h0 @ uE)
    c_ = P0 + np.array([-h0[1], h0[0]]) * 12.0 * np.sign(phi)
    end = c_ + rot2l(P0 - c_, phi)
    off = abs(float((end - lipE1) @ nE)); along = float((footE1 - end) @ uE)
    if along < 10.0: continue
    if bestE is None or off < bestE[0]: bestE = (off, kk, c_, phi)
_, kEL, cE, phiE = bestE
arcE = [cE + rot2l(line[kEL % N1] - cE, phiE * t / 12.0) for t in range(13)]
landE2 = lipE2 + uE * min(farE2, 24.0)
kEj = S(landE2 + uE * 14.0)
pathE = [line[(kEL + 14 - i) % N1] for i in range(0, 14, 2)] + arcE + [tuple(footE1), tuple(lipE1)] + [tuple(lipE1 + uE * t) for t in np.arange(10.0, 128.0 - RE2_RAMP, 10.0)] + \
        [tuple(footE2), tuple(lipE2), tuple(landE2)] + bend_to(landE2, uE, kEj, 6.0) + rev_after(kEj)
pathE = resample(pathE, 2.0)
sE = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(pathE, axis=0), axis=1))])
sc_re = {"id": "re_flood_plain_run", "name": "Flood-plain run", "direction": "reverse", "path": [round(float(v), 2) for q in pathE for v in q],
         "half_width_m": 3.0, "entry_inset_m": round(st_on(pathE, sE, footE1 + uE * 2.0), 1), "entry_margin_m": 1.0, "forgives": forgiven(kEj, kEL), "ai_speed": 17.0,
         "ai_profile": [round(v, 1) for v in (st_on(pathE, sE, arcE[0]) - 4.0, 11.0, st_on(pathE, sE, arcE[-1]), 13.0, st_on(pathE, sE, footE1), RE1_VMIN,
                                              st_on(pathE, sE, lipE1) + 2.0, RE1_VMIN, st_on(pathE, sE, footE2) - 20.0, 17.0, st_on(pathE, sE, footE2), RE2_VMIN,
                                              st_on(pathE, sE, lipE2) + 2.0, RE2_VMIN, st_on(pathE, sE, landE2), 10.0, st_on(pathE, sE, line[kEj % N1]), 11.0)]}
re1["tracks"] = [round(float(v), 2) for q in resample([tuple(line[(kEL + 6) % N1])] + [tuple(q) for q in arcE] + [tuple(footE1)], 1.0) for v in q]
n_leaveE = arcE[2] - arcE[0]; n_leaveE = n_leaveE / np.linalg.norm(n_leaveE); barE = arcE[0] + np.array([n_leaveE[1], -n_leaveE[0]]) * 3.4
re1["hint"] = {"kind": "barricade", "x": round(float(barE[0]), 2), "z": round(float(barE[1]), 2), "yaw": round(math.degrees(math.atan2(*n_leaveE)) - 70.0, 1)}
big.append(re1); big.append(re2)
for b_, far_ in ((re1, farE1), (re2, farE2)):
    P_, d_ = np.array(b_["lip"]), np.array(b_["dir"]); n_ = np.array([-d_[1], d_[0]])
    landings.append({"id": b_["id"], "poly": [round(float(v), 2) for q in [P_ + d_ * 3.0 + n_ * 6.5, P_ + d_ * (far_ + 10.0) + n_ * 7.5, P_ + d_ * (far_ + 10.0) - n_ * 7.5, P_ + d_ * 3.0 - n_ * 6.5] for v in q]})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(line[(kEL + 4) % N1])] + [tuple(q) for q in arcE] + [tuple(footE1), tuple(lipE1)], 1.0) for v in q], "half": 4.5})
clear_lines.append({"points": [round(float(v), 2) for q in [lipE1, lipE2] for v in q], "half": 4.5})   # the flood plain's woods: a cleared line
clear_lines.append({"points": [round(float(v), 2) for q in [lipE2, lipE2 + uE * (farE2 + 8.0)] for v in q], "half": 6.0})
clear_lines.append({"points": [round(float(v), 2) for q in resample([tuple(landE2)] + [tuple(q) for q in bend_to(landE2, uE, kEj, 6.0)], 1.0) for v in q], "half": 6.0})
print(f"  RE flood-plain run: step-down lip ({lipE1[0]:.1f}, {lipE1[1]:.1f}) {re1['lip_y']:.2f} m: " + ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.1f} m" for v, a_ in re1["flights"].items()) +
      f"; gorge kicker ({lipE2[0]:.1f}, {lipE2[1]:.1f}) {re2['lip_y']:.2f} m: " + ", ".join(f"{v} m/s {a_[0]:.2f} s {a_[1]:.1f} m" for v, a_ in re2["flights"].items()) +
      f"; leaves at s {kEL}, rejoins at s {kEj} ({(kEL - kEj) % N1} m of lap for {float(sE[-1]):.0f} m of line); forgives {sc_re['forgives']}")
break_lines += [{"points": [round(float(v), 2) for q in pathD for v in q], "half": 12.0}, {"points": [round(float(v), 2) for q in pathE for v in q], "half": 12.0}]

def lap_gap(P, d, ramp, half):   # the ramp's nearest edge to the trail's line (the tread is 4.5 m wide)
    nl = np.array([-d[1], d[0]])
    return min(float(np.min(np.linalg.norm(line - (P - d * s_ + nl * o), axis=1))) for s_ in np.arange(0, ramp + 0.1, 1.0) for o in (-half, half))
print(f"  N2: {float(s2[-1]):.0f} m of line, leaves the lap at s {k2 - 14}, rejoins at s {k2j + 12} ({(k2j + 12 - (k2 - 14)) % N1} m of lap); kicker {lap_gap(L2, d2, 8.0, 3.0):.1f} m from the trail's line")
print(f"  N5: {float(s5[-1]):.0f} m of line, leaves the lap at s {k5 - 14}, rejoins at s {k5j + 10} ({(k5j + 10 - (k5 - 14)) % N1} m of lap); log ramp {lap_gap(L5, d5, 8.0, 3.0):.1f} m from the trail's line")

# Round 21: the reverse race's held speeds, in the reverse lap's stations (RaceRoute: the loop backwards, station = lap - forward station)
def sr(sf): return round(float((L1 - sf) % L1), 1)
ai_zones_rev = [{"id": j["id"] + "R", "s0": round(sr(j["s_lip"] + j["table"] + j["down"]) - 35, 1), "s1": round(sr(j["s_lip"]) + 2, 1), "speed": 20.0} for j in jumps]   # the tabletops backwards: up the landing face
ai_zones_rev.append({"id": "RJ4", "s0": round(sr(lip4 + RJ4_LIP_M) - 28 - 45, 1), "s1": round(sr(lip4 + RJ4_LIP_M) + 1, 1), "speed": 24.0})
ai_zones_rev += [{"id": b["id"] + "R", "s0": round(sr(b["s_lip"]) - 6 - 40, 1), "s1": round(sr(b["s_lip"]) + 2, 1), "speed": BIG_DESIGN} for b in big if b["s_lip"] >= 0]
for z in ai_zones_rev: z["s0"] = round(z["s0"] % L1, 1); z["s1"] = round(z["s1"] % L1, 1)

# ---------------------------------------------------------------- 5. write
def flat(p): return [round(float(v), 2) for q in p for v in q]
lead = [line[(N1 - 35 + k) % N1] for k in range(0, 36, 1)]   # the lap's last 35 m, up to its first point
cps = [{"id": cid, "s": float(S((x, z))), "pos": [x, z], "label": label} for cid, x, z, label in CHECKPOINTS]
track = {
    "note": "Round 18 (items 3, 5), Round 19 (items 1, 2): the Noonday Creek trail, made by Tools/mapgen/make_creek_track.py. Map x, z in meters; s = meters along 'line' from its first point (the lap's own stations, as in the route file). Built by MapBuilder.CreekTrack.cs.",
    "line": flat(line), "clear_half": [round(float(v), 2) for v in clear], "tread_half_m": TREAD_HALF, "break_half_m": BREAK_HALF,
    "break_left": [round(float(v), 1) for v in break_left], "break_right": [round(float(v), 1) for v in break_right], "break_lines": break_lines,
    "jumps": jumps, "big_jumps": big, "crossings": crossings, "creek_bed": {"s0": c5a, "s1": c6},
    "road_jump": {"id": "J4", "lip": [J4_LIP[0], J4_LIP[1]], "dir": [round(float(J4_DIR[0]), 5), round(float(J4_DIR[1]), 5)], "s_lip": lip4,
                  "ramp_m": 28.0, "lip_above_road_m": 1.6, "width_m": 5.0, "slope_start": 0.11, "slope_lip": 0.25,
                  "landing_from_m": 27.0, "landing_slope": 0.09, "landing_half_m": 5.0,
                  # Round 21: a back down to the verge facing the road, and the reverse kicker (RJ4) up the south bank, with its own back
                  "back_m": RJ4_BACK_M, "rev_lip_m": RJ4_LIP_M, "rev_ramp_m": 28.0, "rev_angle": RJ4_ANGLE, "rev_vmin": RJ4_VMIN, "rev_vmax": RJ4_VMAX},
    "landing_zones": landings, "clear_lines": clear_lines,
}
route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_creek_track.py. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L1, 1), "loop": flat(np.vstack([line, line[:1]])), "gate_s": gate_s,
    "lead_in": flat(lead), "lead_in_length_m": 35.0, "checkpoints": cps, "junctions": [], "loop_pieces": [],
    "official_shortcuts": [sc_p1, sc_n2, sc_n5, sc_ra, sc_rc, sc_rd, sc_re], "closures": [], "ai_zones": ai_zones, "ai_zones_reverse": ai_zones_rev,
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
print(f"  J4 lip s {lip4}; gate s {gate_s}; creek bed s {c5a}..{c6}; P1 path {len(path)} points, leaves the lap at s {leave}, rejoins at s {join2}")
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
