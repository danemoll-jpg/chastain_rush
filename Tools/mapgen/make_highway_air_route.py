"""Round 74 (I, Dan, 2026-10-27 04:40: "a second plane race utilizing the highways so we can fly under bypasses"): THE HIGHWAY AIR RACE.
A circuit of rings for the planes (the Sky Hopper, the Sky Dart) along I-75 and I-575, from Cobb County International's runway (the grid and the
start / finish ring as the Quarry Air Race's), built to Round 73 B's rules from the start (rings that line you up for the next, big except where the
place makes them tight; the 3D arrow, beacons, the ring after next and the minimap are the race layer's own). Clockwise, about 19 km:
  SF     the start / finish ring, 16 m over the runway (a standing take-off at GO)
  R1     past the runway's east end, climbing
  T1     low over I-75 at Town Center (a left sweep onto the interstate, heading north-west)
  U1     under Chastain Road's bridge over I-75 (a frame sized to the opening as built: 8.8 m from the road to the deck, the probe)
  U2     over the Skip Spann Connector (its deck sits on the ground as built: no room under it)
  N1     the north sweep: a high ring over the woods between the interstates (a right turn of about 150 degrees)
  U3     under Chastain Road Northwest's bridges over I-575
  L1     low over I-575 by Barrett Parkway
  X1     over the I-75 / I-575 interchange, where the two split
  S1, B1 south of the airport (the Quarry Air Race's last two), the turn onto the runway's line for the finish
Low over the lanes: 14 to 18 m over the road between the rings (traffic is off in a race: route "traffic": false); down to the deck's room under a
bridge. Heights, floors, ceilings and the rounding are make_air_route.py's (this script is that one with its own course; --room takes the exe's probe
of the BUILT world along this line: Round20Runner.R74.cs hprobe writes 74_line_room.tsv).
What it writes: Data/routes/highway_air_race.json; Data/tracks/highway_air_race.json (its low lanes' clear_lines, merged into the mountain's by
MapBuilder.SummitRamp.cs MergeAirLanes, as the Quarry Air Race's). Re-run after mapgen, then a full Build Map (cleared lanes) or a quick one (the route).
Usage: python Tools/mapgen/make_highway_air_route.py [--plot] [--room <check dir>/74_line_room.tsv]"""
import json, math, os, struct, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/routes/highway_air_race.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
raw = open(os.path.join(ROOT, "Data/trees.bin"), "rb").read()
TREE_REC = np.frombuffer(raw, dtype=np.dtype([("x", "<f4"), ("z", "<f4"), ("seed", "<u2"), ("yard", "u1"), ("depth", "u1")]), count=struct.unpack_from("<I", raw, 4)[0], offset=8)
TREES = np.column_stack([TREE_REC["x"], TREE_REC["z"]])


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)


def ground_max(x, z, r):
    return max(ground(x + dx, z + dz) for dx in (-r, 0, r) for dz in (-r, 0, r))


# the runway (PlaneFlight.RunwayWest / RunwayEast): the grid at its west end, heading east
RW_W, RW_E = np.array([-3690.0, -1909.0]), np.array([-1845.0, -1825.0])
RW_D = (RW_E - RW_W) / np.linalg.norm(RW_E - RW_W)
def on_runway(d): return tuple(RW_W + RW_D * d)

# ---------------------------------------------------------------- the line's control points (x, z), clockwise, from the loop's start on the runway
# a ring at a control point: (name, label, kind, size, height rule); kind "ring": size = radius; "frame": size = (half width, half height);
# height: ("agl", m) over the ground there, or ("abs", y)
# Round 73 (B, Dan: "The first ring is down in the quarry nearly right beside the take off point ... the plane should have already been going in the
# right direction to set that up ... after I got through the first 3 or 4, I had no idea where the next gate even was"): the course redrawn so every
# ring lines you up for the next: the start ring straight ahead of the take-off, R1 straight on past the runway's end (a climb), Town Center high,
# a wide left sweep to I-75 (over the northbound deck, under the southbound), a wide loop north and round to the quarry's east rim, the dive west down
# the lake and the climb out of its west end, a wide U-turn south of the airport to Cobb Parkway, and back east onto the runway for the line.
# Rings bigger (radius 20 to 28 m in the open; the quarry's 18 to 22; the frame under I-75 as tight as the bridge allows).
CTRL = [
    (on_runway(390), None),                                        # the loop's start, over the runway (after the take-off run)
    (on_runway(500), None),
    (on_runway(650), ("SF", "Start / finish", "ring", 22, ("agl", 16))),
    (on_runway(1300), None),
    ((-1300, -1790), ("R1", "Past the runway", "ring", 26, ("agl", 50))),
    ((-760, -1770), None),                                         # the left sweep onto I-75 (north-west)
    ((-420, -1640), None),
    ((-300, -1380), ("T1", "I-75 at Town Center", "ring", 22, ("agl", 16))),
    ((-370, -1000), None),
    ((-470, -720), None),
    ((-620, -420), None),
    ((-820, -110), None),
    ((-935, 260), None),
    ((-972, 432), ("U1", "Under Chastain Road", "frame", (10, 3.4), ("agl", 5.6))),
    ((-1010, 600), None),
    ((-1055, 776), ("U2", "Over the Skip Spann Connector", "ring", 22, ("agl", 20))),   # (the probe: its deck sits on the ground as built: no room under it)
    ((-1130, 1000), None),
    ((-1240, 1300), None),
    ((-1250, 1560), None),                                         # the north sweep (right, about 150 degrees)
    ((-1000, 1720), None),
    ((-500, 1760), ("N1", "North sweep", "ring", 28, ("agl", 80))),
    ((0, 1720), None),
    ((240, 1550), None),
    ((290, 1250), None),
    ((345, 935), ("U3", "Under Chastain Road Northwest", "frame", (10, 3.4), ("agl", 5.6))),
    ((440, 500), None),
    ((530, 100), None),
    ((620, -380), None),
    ((715, -880), None),
    ((752, -1400), ("L1", "I-575 by Barrett Parkway", "ring", 22, ("agl", 16))),
    ((745, -1800), None),
    ((700, -2250), None),
    ((600, -2700), None),
    ((470, -3080), ("X1", "The interchange", "ring", 26, ("agl", 30))),
    ((250, -3200), None),                                          # the right sweep west off the interstates
    ((-200, -3050), None),
    ((-900, -2800), None),
    ((-1700, -2550), None),
    ((-2950, -2200), ("S1", "South of the airport", "ring", 26, ("agl", 70))),
    ((-3300, -2450), None),
    ((-3700, -2510), None),
    ((-3941, -2500), None),                                        # the U-turn: a 300 m right turn onto the runway's line
    ((-4153, -2412), None),
    ((-4241, -2200), ("B1", "The turn", "ring", 26, ("agl", 45))),
    ((-4153, -1988), None),
    ((-3941, -1900), None),
    ((-3800, -1905), None),
]
CLEARED = {}   # (the low stretches are over the interstates' own open lanes: nothing to clear)
LOW = {"SF": (420, 420), "T1": (250, 250), "U1": (400, 220), "U3": (400, 250), "L1": (300, 300)}   # (U1 / U3 low and level 400 m before: the restart 150 m before is on it)   # meters before and after the ring
LOW_CLEAR = {"SF": 10.0, "T1": 12.0, "U1": 5.0, "U3": 5.0, "L1": 12.0}   # over the ground there (under a bridge: as low as its deck allows)
# ---------------------------------------------------------------- the line in plan: a closed centripetal Catmull-Rom through the control points
P = np.array([c[0] for c in CTRL], dtype=float); n = len(P)
def catmull(p0, p1, p2, p3, k=24):
    def tj(ti, a, b): return ti + max(np.linalg.norm(b - a), 1e-3) ** .5
    t0 = 0.0; t1 = tj(t0, p0, p1); t2 = tj(t1, p1, p2); t3 = tj(t2, p2, p3)
    out = []
    for t in np.linspace(t1, t2, k, endpoint=False):
        a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1; a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2
        a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3
        b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2; b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3
        out.append((t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2)
    return out
dense, ctrl_at = [], []
for i in range(n):
    ctrl_at.append(len(dense)); seg = catmull(P[i - 1], P[i], P[(i + 1) % n], P[(i + 2) % n], 60); dense.extend(seg)
dense = np.array(dense); dense = np.vstack([dense, dense[:1]])
cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(dense, axis=0), axis=1))]); L = cum[-1]
STEP = 5.0
S = np.arange(0, L, STEP); XZ = np.column_stack([np.interp(S, cum, dense[:, 0]), np.interp(S, cum, dense[:, 1])]); N = len(S)
ctrl_s = [cum[i] for i in ctrl_at]

# turn radius along the line (over 30 m)
def heading(i): d = XZ[(i + 3) % N] - XZ[(i - 3) % N]; return math.atan2(d[0], d[1])
rad = np.array([30.0 / max(1e-6, abs((math.degrees(heading((i + 3) % N) - heading((i - 3) % N)) + 180) % 360 - 180) * math.pi / 180) for i in range(N)])

G = np.array([ground(x, z) for x, z in XZ])
rings = []
for (pt, ring), s in zip(CTRL, ctrl_s):
    if not ring: continue
    name, label, kind, size, (rule, v) = ring
    i = int(round(s / STEP)) % N
    y = G[i] + v if rule == "agl" else v
    rings.append(dict(name=name, label=label, kind=kind, size=size, i=i, s=float(S[i]), y=float(y)))
# trees near the line (within 16 m across): over woods the line keeps above the crowns (up to 45 m tall with the vertical scale)
# what stands up along the line (a raster of 4 m cells): the trees' tops (the tallest a tree there can be: TreeBuilder's sizes with the vertical
# scale, its crown reaching 7.5 m out, the wing 6.5 m more) and the buildings' roofs (their height with the vertical scale, a roof on top)
CELL = 4.0; bx0, bz0 = XZ[:, 0].min() - 60, XZ[:, 1].min() - 60; bx1, bz1 = XZ[:, 0].max() + 60, XZ[:, 1].max() + 60
NXc, NZc = int((bx1 - bx0) / CELL) + 1, int((bz1 - bz0) / CELL) + 1
TOP = np.full((NZc, NXc), -1e4)
low = np.zeros(N, bool)
for r in rings:
    if r["name"] in LOW:
        k0, k1 = int(LOW[r["name"]][0] / STEP), int(LOW[r["name"]][1] / STEP)
        for d in range(-k0, k1 + 1): low[(r["i"] + d) % N] = True
# the low stretches' flight lane is cleared of trees (the world's Trees step reads these lines: MapBuilder.Mountain.cs MergeAirLanes), CLEAR_HALF either side
CLEAR_HALF = 17.0
clear_lines = []
for r in rings:
    if r["name"] in CLEARED:
        k0, k1 = int(CLEARED[r["name"]][0] / STEP), int(CLEARED[r["name"]][1] / STEP); idx = [(r["i"] + d) % N for d in range(-k0, k1 + 1, 4)]
        clear_lines.append({"id": "air race lane " + r["name"], "points": [round(float(v), 2) for i in idx for v in XZ[i]], "half": CLEAR_HALF})
keep = np.ones(len(TREES), bool)
for cl in clear_lines:
    pts = np.array(cl["points"]).reshape(-1, 2)
    for a, b in zip(pts[:-1], pts[1:]):
        ab = b - a; t = np.clip(((TREES - a) @ ab) / max(ab @ ab, 1e-6), 0, 1); d = np.linalg.norm(TREES - (a + t[:, None] * ab), axis=1); keep &= d > CLEAR_HALF
print(f"cleared lanes: {len(clear_lines)}, {np.sum(~keep)} trees in them")
TREES, TREE_REC = TREES[keep], TREE_REC[keep]
sel = (TREES[:, 0] > bx0) & (TREES[:, 0] < bx1) & (TREES[:, 1] > bz0) & (TREES[:, 1] < bz1)
tx, tz = TREES[sel, 0], TREES[sel, 1]; tr = TREE_REC[sel]
th = 23 * 1.2 * (.75 + .35 * np.clip(tr["depth"] * .1 / 10, 0, 1)) * np.where(tr["yard"] > 0, .9, 1.0) * 1.5
ty = np.array([ground(x, z) for x, z in zip(tx, tz)]) + th
ix, iz = ((tx - bx0) / CELL).astype(int), ((tz - bz0) / CELL).astype(int)
R = 14.0; k = int(R / CELL) + 1
for dz in range(-k, k + 1):
    for dx in range(-k, k + 1):
        if (dx * dx + dz * dz) * CELL * CELL > R * R: continue
        jx, jz = np.clip(ix + dx, 0, NXc - 1), np.clip(iz + dz, 0, NZc - 1); np.maximum.at(TOP, (jz, jx), ty)
from matplotlib.path import Path as MPath
VS = M.get("vertical_scale", 1.5)
for b in M["buildings"]:
    fp = np.array(b["footprint"]).reshape(-1, 2)
    if fp[:, 0].max() < bx0 or fp[:, 0].min() > bx1 or fp[:, 1].max() < bz0 or fp[:, 1].min() > bz1: continue
    top = max(ground(x, z) for x, z in fp) + b["height"] * VS + 4.0
    lo, hi = fp.min(0) - 7, fp.max(0) + 7
    xs = np.arange(lo[0], hi[0], CELL / 2); zs = np.arange(lo[1], hi[1], CELL / 2)
    gx, gz = np.meshgrid(xs, zs); pts = np.column_stack([gx.ravel(), gz.ravel()])
    c = fp.mean(0); grown = c + (fp - c) * (1 + 7.0 / max(1.0, np.linalg.norm(fp - c, axis=1).mean()))
    inside = MPath(grown).contains_points(pts)
    for x, z in pts[inside]:
        jx, jz = int((x - bx0) / CELL), int((z - bz0) / CELL)
        if 0 <= jx < NXc and 0 <= jz < NZc: TOP[jz, jx] = max(TOP[jz, jx], top)
def top_at(x, z): return TOP[int((z - bz0) / CELL), int((x - bx0) / CELL)]
TOPS = np.array([top_at(x, z) for x, z in XZ])
# the bridge decks over the line: under one, the line keeps the tail clear of the deck (map.json deck_y is about its underside in the built scene: the probe; tail 1.7 m, 1.3 m to spare)
CEIL = np.full(N, 1e4)
for b in M["bridges"]:
    p = np.array(b["points"]).reshape(-1, 2); dy = np.array(b["deck_y"])
    if p[:, 0].max() < bx0 or p[:, 0].min() > bx1 or p[:, 1].max() < bz0 or p[:, 1].min() > bz1: continue
    for i, q in enumerate(XZ):
        d = np.linalg.norm(p - q, axis=1); j = d.argmin()
        if d[j] >= b["width"] / 2 + 7.0: continue
        # (Round 74: along an interstate the line flies over the interstate's own bridges (decks running the line's way): only a deck across the
        # line is a ceiling)
        bd = p[min(j + 1, len(p) - 1)] - p[max(j - 1, 0)]; ld = XZ[(i + 2) % N] - XZ[(i - 2) % N]
        if abs(np.dot(bd, ld)) / max(np.linalg.norm(bd) * np.linalg.norm(ld), 1e-6) > .6: continue
        CEIL[i] = min(CEIL[i], dy[j] - 1.7 - 2.2)
near_tree = TOPS > G + 1.0
# --room <file>: what the exe's probe found over and under the line in the BUILT world (Round20Runner.R71.cs aprobe writes 71_line_room.tsv: the
# highest solid under the wings and body, the lowest over them): the floor and the ceiling take it where it is known (near the bridges the map
# build's abutments and graded banks stand higher than the planning's ground)
BUILT_LO = np.full(N, -1e4); BUILT_HI = np.full(N, 1e4)
if "--room" in sys.argv:
    from scipy.spatial import cKDTree
    rows = [l.split("\t") for l in open(sys.argv[sys.argv.index("--room") + 1]).read().splitlines()[1:]]
    rp = np.array([[float(r[0]), float(r[1])] for r in rows]); kdr = cKDTree(rp)
    for i, q in enumerate(XZ):
        d, j = kdr.query(q)
        if d > 4: continue
        if rows[j][3] != "nan": BUILT_LO[i] = float(rows[j][3])
        if rows[j][4] != "nan": BUILT_HI[i] = float(rows[j][4])
    print(f"built room read: {np.sum(BUILT_LO > -1e4)} points with ground, {np.sum(BUILT_HI < 1e4)} with something over them")
    # (Round 74: the map build lifts bridge decks (map.json's deck_y is the planning's: Chastain Road's over I-75 stands 10 m higher as built): with the
    # probe, a planned deck over the line takes the built one's underside found within 40 m along the line; none found: no deck over it as built)
    planned = np.where(CEIL < 1e4)[0]
    for i in planned:
        near_ = [BUILT_HI[(i + k) % N] for k in range(-8, 9) if BUILT_HI[(i + k) % N] < 1e4]
        CEIL[i] = (min(near_) - 1.7 - 2.4) if near_ else 1e4   # (Round 74's check: 1.2 m over the tail was too tight: the AI pilots hit Chastain Road's deck; 2.4)
# the highest ground under the wings (6.5 m either side, across the line) and under the body
def across(i):
    d = XZ[(i + 2) % N] - XZ[(i - 2) % N]; d = d / max(np.linalg.norm(d), 1e-6); r = np.array([d[1], -d[0]])
    return max(ground(*(XZ[i] + r * o)) for o in (-6.5, -3.5, 0, 3.5, 6.5))
G2 = np.array([across(i) for i in range(N)])


lowc = np.zeros(N)
for r in rings:
    if r["name"] in LOW_CLEAR:
        k0, k1 = int(LOW[r["name"]][0] / STEP), int(LOW[r["name"]][1] / STEP)
        for d in range(-k0, k1 + 1): lowc[(r["i"] + d) % N] = LOW_CLEAR[r["name"]]
f_low = np.maximum(np.maximum(G + np.maximum(2.8, lowc), G2 + 1.2), TOPS + 4.0); f_open = np.maximum(G2 + 22.0, TOPS + 8.0)
f_low = np.maximum(f_low, np.where(BUILT_LO > -1e4, BUILT_LO + 1.25 + 2.2, -1e4)); f_open = np.maximum(f_open, np.where(BUILT_LO > -1e4, BUILT_LO + 1.25 + 8.0, -1e4))
# into a low stretch the floor comes down over 75 m from its edge (no step: the plane's pull-up out of it is rounded too)
edge = np.full(N, 99)
for i in np.where(~low)[0]: edge[i] = 0
for k in range(2 * N): i = k % N; edge[i] = min(edge[i], edge[i - 1] + 1)
for k in range(2 * N, 0, -1): i = k % N; edge[i] = min(edge[i], edge[(i + 1) % N] + 1)
w = np.clip(edge / 15.0, 0, 1)
floor = np.where(low, w * f_low + (1 - w) * np.maximum(f_low, f_open), f_open)
CEIL = np.minimum(CEIL, np.where(low & (BUILT_HI < 1e4), BUILT_HI - 1.7 - 1.2, 1e4))   # the built world's own underside, where the probe saw one
   # and its own ground: the wheels 2.2 m over it, low
pinned = low & (CEIL < 1e4)
floor = np.where(pinned, np.maximum(np.maximum(G + 3.8, G2 + 1.2), np.where(BUILT_LO > -1e4, BUILT_LO + 1.25 + 1.6, -1e4)), floor)   # under a deck: the body 3.8 m up (the wheels 2.5 m; Round 74, was 4.3)

# heights: between rings straight, then never under the floor, then the climb / dive limits (raising only), then smoothed
Y = np.zeros(N); order = sorted(rings, key=lambda r: r["i"])
for a, b in zip(order, order[1:] + order[:1]):
    ia, ib = a["i"], b["i"] + (N if b["i"] <= a["i"] else 0)
    for k in range(ia, ib + 1): t = (k - ia) / max(1, ib - ia); Y[k % N] = a["y"] * (1 - t) + b["y"] * t
CLIMB, DIVE = math.tan(math.radians(31)), math.tan(math.radians(31))
fixed = {r["i"] for r in rings}
for it in range(6):
    Y = np.maximum(Y, floor)
    Y = np.where(pinned, np.minimum(Y, CEIL), Y)
    for k in range(2 * N): i = k % N; Y[i] = Y[i] if pinned[i] else max(Y[i], Y[i - 1] - DIVE * STEP)            # forward: never dive steeper
    for k in range(2 * N, 0, -1): i = k % N; Y[i] = Y[i] if pinned[i] else max(Y[i], Y[(i + 1) % N] - CLIMB * STEP)   # backward: never climb steeper
    sm = np.convolve(np.concatenate([Y[-8:], Y, Y[:8]]), np.ones(17) / 17, "same")[8:-8]
    Y = np.where(low, np.maximum(Y, floor), np.maximum(sm, floor))
    Y = np.where(pinned, np.minimum(Y, CEIL), Y)
Y = np.where(pinned, np.minimum(np.maximum(Y, floor), CEIL), np.maximum(Y, floor))
for k in range(2 * N): i = k % N; Y[i] = Y[i] if pinned[i] else max(Y[i], Y[i - 1] - DIVE * STEP)
for k in range(2 * N, 0, -1): i = k % N; Y[i] = Y[i] if pinned[i] else max(Y[i], Y[(i + 1) % N] - CLIMB * STEP)
# and never a step up or down steeper than the climb / dive either side of a deck's held stretch (the passes above only raise): capped, never under the floor
for k in range(1, 2 * N): i = k % N; Y[i] = Y[i] if (pinned[i] or i in fixed) else max(floor[i], min(Y[i], Y[i - 1] + CLIMB * STEP))
for k in range(2 * N, 0, -1): i = k % N; Y[i] = Y[i] if (pinned[i] or i in fixed) else max(floor[i], min(Y[i], Y[(i + 1) % N] + DIVE * STEP))
# a plane changes its pitch at about 25 degrees a second: crests and dips rounded to a 350 m vertical radius, by lifting only (never toward the ground)
VR = 280.0; cmax = STEP * STEP / (2 * VR); free = ~pinned
for it in range(2500):
    want = .5 * (np.roll(Y, 1) + np.roll(Y, -1)) - cmax
    Y = np.where(free, np.maximum(Y, want), Y)
bad = np.where(pinned & (Y < floor - .01))[0]
if len(bad): print("UNDER A DECK WITH NO ROOM:", ", ".join(f"s {S[i]:.0f} floor {floor[i]:.1f} ceiling {CEIL[i]:.1f}" for i in bad[:10]))
report = []
for r in rings:
    dy = Y[r["i"]] - r["y"]
    report.append(f'{r["name"]:3s} s {r["s"]:7.0f}  at ({XZ[r["i"]][0]:7.1f}, {XZ[r["i"]][1]:7.1f})  ring y {r["y"]:6.1f}  line y {Y[r["i"]]:6.1f} ({dy:+.1f})  ground {G[r["i"]]:6.1f}  radius here {rad[r["i"]]:5.0f} m')
    r["y"] = float(Y[r["i"]]) if abs(dy) > 0.5 else r["y"]
pitch = np.degrees(np.arctan2(np.diff(np.concatenate([Y, Y[:1]])), STEP))
print(f"lap {L:.0f} m (flat), {N} points; min turn radius {rad.min():.0f} m at s {S[rad.argmin()]:.0f}; pitch {pitch.min():.1f} (s {S[pitch.argmin()]:.0f}) to {pitch.max():.1f} (s {S[pitch.argmax()]:.0f}) deg")
print("tightest turns:", ", ".join(f"s {S[i]:.0f} r {rad[i]:.0f}" for i in np.argsort(rad)[:8]))
print("\n".join(report))
agl = Y - G
print(f"height over ground: min {agl.min():.1f} at s {S[agl.argmin()]:.0f}; low (under 25 m) {np.sum(agl < 25) * STEP:.0f} m of the lap")

# ---------------------------------------------------------------- the file
sf = next(r for r in rings if r["name"] == "SF"); cps = [r for r in rings if r["name"] != "SF"]
lead = [on_runway(d) for d in np.arange(0, 391, 10)]
def r2(v): return round(float(v), 2)
route = {
    "name": "Highway Air Race", "direction": "forward", "directions": ["forward"], "laps": 2, "traffic": False, "air": True, "vehicles": ["plane", "dart"],
    "source": "Tools/mapgen/make_highway_air_route.py (Round 74): the rings' line up I-75 and down I-575, under their bridges. Map meters (= Unity x, z), heights in the world's meters.",
    "lap_length_m": r2(L), "gate_s": r2(sf["s"]), "gate_half_width_m": sf["size"], "checkpoint_half_width_m": 14.0, "gate_y": r2(sf["y"]), "gate_ring_air_m": sf["size"],
    "gate_below_m": 40.0, "gate_above_m": 40.0, "reverse_grid_back_m": 0.0,
    "loop": [r2(v) for p in XZ for v in p], "loop_y": [r2(v) for v in Y],
    "lead_in": [r2(v) for p in lead for v in p], "lead_in_length_m": 390.0,
    "checkpoints": [dict(id=k + 1, s=r2(r["s"]), pos=[r2(XZ[r["i"]][0]), r2(XZ[r["i"]][1])], label=r["label"], y=r2(r["y"]),
                         ring_m=float(r["size"]) if r["kind"] == "ring" else 0.0,
                         ring_w=float(r["size"][0]) if r["kind"] == "frame" else 0.0, ring_h=float(r["size"][1]) if r["kind"] == "frame" else 0.0,
                         half_m=float(r["size"] if r["kind"] == "ring" else r["size"][0])) for k, r in enumerate(cps)],
}
json.dump(route, open(OUT, "w"), indent=1)
json.dump({"source": "Tools/mapgen/make_highway_air_route.py (Round 74): the highway air race's low flight lanes, cleared of trees by the map build (MapBuilder.SummitRamp.cs MergeAirLanes)", "clear_lines": clear_lines},
          open(os.path.join(ROOT, "Data/tracks/highway_air_race.json"), "w"), indent=1)
print("wrote", OUT, f"({len(cps)} rings and the line)")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, (a1, a2) = plt.subplots(2, 1, figsize=(14, 18), gridspec_kw=dict(height_ratios=[3, 1]))
    a1.scatter(TREES[::7, 0], TREES[::7, 1], s=.05, c="g")
    for w in M["water"]:
        p = w["outline"]; a1.fill(p[0::2], p[1::2], color="c", alpha=.5)
    for b in M["bridges"]:
        p = b["points"]; a1.plot(p[0::2], p[1::2], "y", lw=3)
    for r in M["roads"]:
        if r["class"] in ("motorway", "trunk", "primary"): p = r["points"]; a1.plot(p[0::2], p[1::2], color="r", lw=.6)
    sc = a1.scatter(XZ[:, 0], XZ[:, 1], c=agl, s=2, cmap="viridis", vmax=150)
    for r in rings: a1.annotate(r["name"], XZ[r["i"]], fontsize=10, color="k")
    a1.set_aspect("equal"); a1.set_xlim(XZ[:, 0].min() - 300, XZ[:, 0].max() + 300); a1.set_ylim(XZ[:, 1].min() - 300, XZ[:, 1].max() + 300); plt.colorbar(sc, ax=a1)
    a2.plot(S, G, "brown"); a2.plot(S, Y, "b"); a2.plot(S, floor, "g", lw=.4)
    for r in rings: a2.annotate(r["name"], (r["s"], r["y"]))
    os.makedirs(os.path.join(ROOT, "local/round74"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round74/highway_air_route.png"), dpi=90, bbox_inches="tight"); print("plot local/round74/highway_air_route.png")
if "--dbg" in sys.argv:
    a, b = [int(v) for v in sys.argv[sys.argv.index("--dbg") + 1].split(",")]
    for i in range(a // 5, b // 5, 2): print(f"s {S[i]:6.0f} G {G[i]:6.1f} G2 {G2[i]:6.1f} lo {BUILT_LO[i]:7.1f} hi {BUILT_HI[i]:7.1f} top {TOPS[i]:6.1f} ceil {CEIL[i]:7.1f} floor {floor[i]:6.1f} Y {Y[i]:6.1f} low {low[i]} pin {pinned[i]}")
