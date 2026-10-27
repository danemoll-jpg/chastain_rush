"""Round 71 (Dan, 2026-10-26 03:09: "a plane race ... the typical fly through the rings course. We could utilize the quary right next to the airport and
just expand it ... I would like to have low spots and maybe have to fly under bridges if possible. It could be a circuit"): THE QUARRY AIR RACE.

A circuit of rings for the Sky Hopper (Round 69), starting on Cobb County International's runway 09 and finishing low over it, laid round Viking
Quarry Lake and out from it. Clockwise, about 16 km, a lap about 2.5 minutes flat out:
  S/F    the start / finish ring, 14 m over the runway (the grid is at the runway's west end: a standing take-off at GO)
  Q1-Q3  Viking Quarry: in over its south-west rim and down the 200 m deep pit (Q1 halfway down), Q2 10 m over the lake, then up the east wall
         (Q3 over the rim)
  C1-C2  Noonday Creek, just south of Big Shanty Road: under I-75's southbound deck (a frame sized to the opening, the lane through the creek's
         trees cleared) and straight up over the northbound and express decks (their opening under them is too low for the wings)
  H1     a high ring 270 m over Town Center (climb for it, dive from it)
  M1     over the Barrett Parkway interchange
  W1     low over the woods' tops north of Kennesaw Mountain
  H2     over Kennesaw Mountain's summit
  M2     over the woods west of the mountain
  L1     low over Daniell Lake
  B1     over Cobb Parkway's bridge (the railroad's cut under it is too narrow for the wings), then a right turn onto the runway's line for the finish
Why here: the quarry is the deepest, steepest drop on the map and sits beside the runway; I-75's southbound deck over Noonday Creek is the one
highway bridge near it with room under it for a plane with 11 m wings (9 m from the bank to the deck as built) and no street lamps, signs or traffic
under it; the mountain is the landmark to climb for. Bridges along roads were left out: lamps, sign gantries and traffic under them.

What it writes: Data/routes/quarry_air_race.json (an "air" route: the 3D line through the rings (loop + loop_y), the rings as checkpoints with
their centers and sizes (y, ring_m, or ring_w / ring_h for a frame), the grid and the take-off run on the runway (lead_in)). No world change: the
rings are the race layer (RaceMarkers), so no Build Map step builds anything; the scene embeds the route file (a quick or full Build Map).
Heights: the floor is the planning's ground, the trees' tops (TreeBuilder's tallest for the spot) and the buildings' roofs, and with --room <file> the
BUILT world as the exe's probe found it over and under the line (Round20Runner.R71.cs aprobe writes 71_line_room.tsv: the map build's banks and
abutments by the bridges stand higher than the planning's ground); crests and dips rounded for the plane's pitch rate.
Usage: python Tools/mapgen/make_air_route.py [--plot] [--room local/round71/<check>/71_line_room.tsv]"""
import json, math, os, struct, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/routes/quarry_air_race.json")
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
    ((-1300, -1790), ("R1", "Past the runway", "ring", 26, ("agl", 55))),
    ((-382, -1726), None),                                         # the sweep onto I-75's line: a 400 m left turn (141 degrees)
    ((-222, -1680), None),
    ((-94, -1573), None),
    ((-21, -1424), ("H1", "High ring", "ring", 28, ("agl", 200))),
    ((-16, -1257), None),
    ((-78, -1103), None),
    ((-198, -988), None),
    ((-336, -901), ("C2", "Over I-75 northbound", "ring", 16, ("agl", 21))),
    ((-459, -823), ("C1", "Under I-75 southbound", "frame", (10, 3.4), ("agl", 5.6))),
    ((-600, -733), None),
    ((-790, -610), None),
    ((-990, -620), None),
    ((-1150, -830), None),
    ((-1340, -1170), None),
    ((-1590, -1480), ("Q1", "Quarry rim", "ring", 22, ("abs", 345))),
    ((-1850, -1522), None),
    ((-2150, -1545), ("Q2", "Quarry lake", "ring", 20, ("abs", 128))),
    ((-2450, -1570), ("Q3", "Quarry climb", "ring", 20, ("abs", 215))),
    ((-2640, -1660), None),
    ((-2760, -1900), None),
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
CLEARED = {"C1": (105, 260)}   # the low stretch in woods: its lane cleared, meters before and after the ring
LOW = {"SF": (420, 420), "Q2": (150, 150), "C1": (105, 260)}   # meters before and after the ring
LOW_CLEAR = {"SF": 10.0, "Q2": 4.0, "C1": 7.0}   # over the ground there (under a bridge: as low as its deck allows)

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
        if d[j] < b["width"] / 2 + 7.0: CEIL[i] = min(CEIL[i], dy[j] - 1.7 - 2.2)
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
floor = np.where(pinned, np.maximum(np.maximum(G + 4.3, G2 + 1.2), np.where(BUILT_LO > -1e4, BUILT_LO + 1.25 + 1.6, -1e4)), floor)   # under a deck: the body 4.3 m up (the wheels 3 m)

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
    "name": "Quarry Air Race", "direction": "forward", "directions": ["forward"], "laps": 2, "traffic": False, "air": True, "vehicles": ["plane"],
    "source": "Tools/mapgen/make_air_route.py (Round 71): the rings' line round Viking Quarry, under I-75 at Noonday Creek, over Kennesaw Mountain and under Cobb Parkway. Map meters (= Unity x, z), heights in the world's meters.",
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
json.dump({"source": "Tools/mapgen/make_air_route.py (Round 71): the air race's low flight lanes, cleared of trees by the map build (MapBuilder.Mountain.cs MergeAirLanes)", "clear_lines": clear_lines},
          open(os.path.join(ROOT, "Data/tracks/quarry_air_race.json"), "w"), indent=1)
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
    os.makedirs(os.path.join(ROOT, "local/round71"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round71/air_route.png"), dpi=90, bbox_inches="tight"); print("plot local/round71/air_route.png")
if "--dbg" in sys.argv:
    a, b = [int(v) for v in sys.argv[sys.argv.index("--dbg") + 1].split(",")]
    for i in range(a // 5, b // 5, 2): print(f"s {S[i]:6.0f} G {G[i]:6.1f} G2 {G2[i]:6.1f} lo {BUILT_LO[i]:7.1f} hi {BUILT_HI[i]:7.1f} top {TOPS[i]:6.1f} ceil {CEIL[i]:7.1f} floor {floor[i]:6.1f} Y {Y[i]:6.1f} low {low[i]} pin {pinned[i]}")
