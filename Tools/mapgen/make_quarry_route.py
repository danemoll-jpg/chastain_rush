"""Round 90 (Dan, 2026-11-01 11:37: "The quarry - I wanted a car race with huge jumps over the lake"): THE VIKING QUARRY race, a car circuit
round and through Viking Quarry whose heart is three huge jumps over the lake, on the course's line.

Why these three jumps. A car clears the lake only from the airport's plateau on the quarry's south rim (about 320 m) down onto the pit's benches on the
north side (200 to 227 m): the south wall is a cliff straight into the water, so nothing can land on the south side, and the north bank climbs back to
the pit, so every crossing goes south to north and drops 90 to 120 m. Where the pit's bench lies within about 270 m of the rim (x -2200 to -1720) a
flight of 200 to 250 m reaches it; further west the gap is 330 to 390 m (too far for any car). Free Roam's three quarry jumps (The Big One, The Long One,
The Corner, Tools/mapgen/make_quarry_jumps.py) already sit on the three lines that work, so the race flies those three lines (their kickers widened to
18 m and landing decks to 30 m for four cars abreast, each kicker a different quarry ramp: steel, the old conveyor's gantry, crushed stone).

Why the lap goes the way it does. Every jump lands in the pit, so every jump needs its own climb back out (three climbs of about 100 m), and the
course may never cross itself. The lap (forward only):
  The Big One (J3) from the plateau, landing on the point's bench
  S, the low haul bench: down the north bank to the quarry's west end (the water's edge), round it, then up a bench cut along the south cliff under
     the kickers to a cutting through the rim east of the runway (x -1790) and back onto the plateau
  The Corner (J1), landing on the north-east bench, then the quarry's own haul road (OSM 1173718687) up the east wall to the east rim
  the east rim road south (OSM 30655025), a gravel road round The Corner's start, west behind the start boxes
  The Long One (J2), landing on the north bench, north across the pit floor
  X, the high haul bench: west across the pit floor, up the north-west lobe and the west wall to the plateau's south-west corner
  across the plateau (over the runway's west end) to The Big One's run-up.
The flights cross over the low bench (it runs on the cliff under the kickers) and that is the only place two parts of the lap meet.

What it writes: Data/routes/viking_quarry.json (the race) and Data/tracks/viking_quarry.json (its world pieces: the benches' grades, cleared lines,
the berms along the cliff edges, the gravel surfaces; merged into the mountain's planning by MapBuilder.QuarryRace.cs). Re-run after
make_quarry_jumps.py (then make_quarry_jumps.py again: its bleachers keep off this course), then a full Build Map.
Usage: python Tools/mapgen/make_quarry_route.py [--plot]"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/viking_quarry.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/viking_quarry.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
Q = json.load(open(os.path.join(ROOT, "Data/tracks/quarry_jumps.json"), encoding="utf-8"))
JJ = {j["id"]: j for j in Q["jumps"]}
WATER = 113.6


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def flat(p): return [round(v, 2) for q in p for v in q]
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
def cum(p):
    c = [0.0]
    for a, b in zip(p, p[1:]): c.append(c[-1] + math.dist(a, b))
    return c
def unit(v): n = math.hypot(*v) or 1.0; return (v[0] / n, v[1] / n)
def resample(p, step):
    c = cum(p); n = max(1, int(math.ceil(c[-1] / step))); out = []
    for k in range(n + 1):
        s = c[-1] * k / n; i = min(len(p) - 2, max(0, int(np.searchsorted(c, s)) - 1)); t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9)
        out.append((p[i][0] + t * (p[i + 1][0] - p[i][0]), p[i][1] + t * (p[i + 1][1] - p[i][1])))
    return out
def point_at(p, s, c=None):
    c = c or cum(p); s = max(0.0, min(c[-1], s)); i = min(len(p) - 2, max(0, int(np.searchsorted(c, s)) - 1)); t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9)
    return (p[i][0] + t * (p[i + 1][0] - p[i][0]), p[i][1] + t * (p[i + 1][1] - p[i][1]))
def nearest_s(p, q, c=None):
    c = c or cum(p); best = (1e18, 0.0)
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
        t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
        d = math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
        if d < best[0]: best = (d, c[i] + t * math.sqrt(L2))
    return best
def gsm(vals, sig):
    out = []
    for i in range(len(vals)):
        lo, hi = max(0, i - int(3 * sig)), min(len(vals), i + int(3 * sig) + 1); w = [math.exp(-((k - i) / sig) ** 2 / 2) for k in range(lo, hi)]
        out.append(sum(vals[k] * wk for k, wk in zip(range(lo, hi), w)) / sum(w))
    return out
def catmull(ctrl, step=2.0):
    """a Catmull-Rom curve through the control points (centripetal), resampled every 'step' m"""
    P = [np.array(ctrl[0]) * 2 - np.array(ctrl[1])] + [np.array(c) for c in ctrl] + [np.array(ctrl[-1]) * 2 - np.array(ctrl[-2])]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        def tj(ti, a, b): return ti + max(1e-6, np.linalg.norm(b - a)) ** 0.5
        t0 = 0.0; t1 = tj(t0, p0, p1); t2 = tj(t1, p1, p2); t3 = tj(t2, p2, p3)
        n = max(2, int(np.linalg.norm(p2 - p1) / 0.5))
        for k in range(n):
            t = t1 + (t2 - t1) * k / n
            A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1; A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2
            A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3
            B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2; B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3
            C = (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2; out.append((float(C[0]), float(C[1])))
    out.append(tuple(ctrl[-1]))
    return resample(out, step)
def curve(ctrl, first=1, last=None, step=2.0):
    """the Catmull-Rom curve through ctrl, from ctrl[first] to ctrl[last] (the points outside them only shape its ends' tangents)"""
    c = catmull(ctrl, 0.5); last = len(ctrl) - 1 if last is None else (last % len(ctrl))
    i0 = min(range(len(c)), key=lambda i: math.dist(c[i], ctrl[first])); i1 = min(range(len(c)), key=lambda i: math.dist(c[i], ctrl[last]))
    return resample(c[i0:i1 + 1], step)
def heading(a, b): return math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360
def on_line(j, s): return (j["origin"][0] + j["dir"][0] * s, j["origin"][1] + j["dir"][1] * s)


SETTINGS = {"name": "Viking Quarry", "direction": "forward", "directions": ["forward"], "laps": 2, "traffic": False, "lake_reset_s": 1.5,
            "gate_half_width_m": 22.0, "checkpoint_half_width_m": 16.0, "gate_below_m": 6.0, "gate_above_m": 120.0, "reverse_grid_back_m": 45.0,
            # every ground vehicle that clears The Long One from its run-up (the jumps' design: Tools/mapgen/make_quarry_jumps.py); never the stunt ones
            "vehicles": ["original", "tourer", "roadster", "fastback", "pebble", "skyfin", "moto", "scrambler", "drifter", "atv", "mower"]}
BENCH_HALF = 8.0      # the haul benches and new roads: 16 m wide (four cars abreast, as the kickers)
CLEAR_HALF = 16.0
J1, J2, J3 = JJ["J1"], JJ["J2"], JJ["J3"]
def catch_end(j): return j["runout_to"] - 30.0   # (the catch's floor back at the bench's height, Tools/mapgen/make_quarry_jumps.py)

# ---------------------------------------------------------------- the pieces, in lap order; each a list of (x, z) every 2 m and their kind:
# 'jump' (a quarry jump's line from its run-up's head to its catch: built by the jumps, never graded here), 'road' (an OSM road: its own surface),
# 'bench' (graded here to y), 'gravel' (a new road on the plateau: graded to the smoothed ground)
pieces = []
def add(name, p, kind, y=None): pieces.append(dict(name=name, p=p, kind=kind, y=y))
def jline(j, s0, s1): return [on_line(j, s) for s in np.arange(s0, s1 + 0.01, 2.0)]

# ---- J3 The Big One: its run-up's head to the catch
add("J3 The Big One", jline(J3, 0.0, catch_end(J3)), "jump")
print(f"J3 catch end at {on_line(J3, catch_end(J3))}")

# ---------------------------------------------------------------- S, the low haul bench
def contour_z(x, h, z0, z1, step=0.5):
    """along x, from z0 toward z1, the first z where the ground reaches h (the cliff's face at that height)"""
    zs = np.arange(z0, z1, step if z1 > z0 else -step)
    for z in zs:
        if ground(x, z) >= h: return float(z)
    return None
S_TOE_Y, S_TOP_Y, S_TOP_X = 177.0, 266.0, -1860.0   # the bench at the quarry's south-west corner, and where it turns into the cutting
s_ctrl = [on_line(J3, catch_end(J3) - 20.0), on_line(J3, catch_end(J3)),
          (-2092.0, -1262.0), (-2122.0, -1243.0), (-2165.0, -1252.0), (-2205.0, -1285.0), (-2300.0, -1300.0), (-2420.0, -1296.0), (-2500.0, -1300.0),
          (-2565.0, -1340.0), (-2620.0, -1395.0), (-2690.0, -1458.0), (-2745.0, -1520.0), (-2765.0, -1562.0), (-2748.0, -1586.0), (-2705.0, -1594.0)]
s_y_ctrl = [None, 206.6, 207.0, 208.0, 210.0, 214.0, 222.0, 222.0, 220.0, 214.0, 205.0, 194.0, 184.0, 179.0, S_TOE_Y, S_TOE_Y]
# along the south cliff: every 40 m of x from the south-west corner to the cutting, the bench on the cliff's face at its height (rising evenly)
wall_x = list(np.arange(-2660.0, S_TOP_X - 0.1, 40.0)) + [S_TOP_X]
for x in wall_x:
    f = (x - (-2705.0)) / (S_TOP_X - (-2705.0)); h = S_TOE_Y + f * (S_TOP_Y - S_TOE_Y)
    z = contour_z(x, h, -1560.0, -1720.0)
    s_ctrl.append((float(x), z)); s_y_ctrl.append(h)
print("S on the south cliff: " + ", ".join(f"({x:.0f}, {z:.0f}) {h:.0f}" for (x, z), h in zip(s_ctrl[-len(wall_x):], s_y_ctrl[-len(wall_x):])))
# (the cliff's face wanders in and out round the promontory at x -2340: the bench's line smoothed along it, cut in or built out a little)
_wz = gsm([z for _, z in s_ctrl[-len(wall_x):]], 1.2)
s_ctrl[-len(wall_x):] = [(x, z) for (x, _), z in zip(s_ctrl[-len(wall_x):], _wz)]
# then the turn south into the cutting through the rim, and the cutting rising to the plateau
CUT_X, CUT_END_Z, CUT_END_Y = -1798.0, -1995.0, None
# (the last wall point is the cutting's turn: a bend of about 45 m from along the cliff to south into the rim)
s_ctrl += [(-1822.0, -1614.0), (-1806.0, -1632.0), (CUT_X, -1662.0), (CUT_X, -1690.0)]
s_y_ctrl += [None, None, None, None]
cut_pts = [(CUT_X, float(z)) for z in np.arange(-1720.0, CUT_END_Z + 30.0, -40.0)] + [(CUT_X, CUT_END_Z)]
s_ctrl += cut_pts; s_y_ctrl += [None] * len(cut_pts)
S_line = curve(s_ctrl, 1)

# ---------------------------------------------------------------- D, the cutting's top round to The Corner's run-up
d_ctrl = [(CUT_X, CUT_END_Z + 30.0), (CUT_X, CUT_END_Z), (CUT_X + 2.0, -2032.0), (-1765.0, -2062.0), (-1715.0, -2072.0), (-1668.0, -2052.0), on_line(J1, -95.0), on_line(J1, -50.0), on_line(J1, 0.0)]
D_line = curve(d_ctrl, 1)

# ---------------------------------------------------------------- J1 The Corner, then F up the haul road
add("S the low haul bench", S_line, "bench")
add("D round to The Corner", D_line, "gravel")
add("J1 The Corner", jline(J1, 0.0, catch_end(J1)), "jump")
roads = {r["osm_id"]: pts(r["points"]) for r in M["roads"]}
haul = roads[1173718687]   # from the east rim down: (-1612, -1355) ... (-1946, -1160) ... (-2241, -1397)
k_junction = next(i for i, q in enumerate(haul) if abs(q[0] + 1946) < 1 and abs(q[1] + 1160) < 1)
haul_up = list(reversed(haul[:k_junction + 1]))   # from the junction (-1946, -1160) up to the east rim
# the haul road's own line has a switchback of 6 m radius and a step of 20 %; the race's haul road follows it round one hairpin of about 25 m at the
# bench's north end and climbs evenly from The Corner's bench to the east rim (a bench graded over the old road)
f_ctrl = [on_line(J1, catch_end(J1) - 20.0), on_line(J1, catch_end(J1)), (-1836.0, -1214.0), (-1872.0, -1178.0), (-1912.0, -1152.0), (-1940.0, -1132.0),
          (-1938.0, -1108.0), (-1912.0, -1100.0), (-1872.0, -1110.0), (-1810.0, -1140.0), (-1755.0, -1170.0), (-1722.0, -1200.0), (-1706.0, -1245.0),
          (-1690.0, -1295.0), (-1664.0, -1350.0), (-1632.0, -1400.0), (-1612.0, -1450.0), (-1610.0, -1504.0), (-1600.0, -1550.0)]
F_line = curve(f_ctrl, 1, -2)
add("F the haul road", F_line, "bench")
# ---------------------------------------------------------------- G: the east rim road south, a gravel road round The Corner's start, west behind the boxes
east = roads[30655025]
k_top = next(i for i, q in enumerate(east) if abs(q[0] + 1612) < 1 and abs(q[1] + 1355) < 1)
k_corner = next(i for i, q in enumerate(east) if abs(q[0] + 1610) < 1 and abs(q[1] + 1504) < 1)
er = list(reversed(east[k_corner:k_top + 1]))   # (-1612, -1355) south to (-1610, -1504)
g_ctrl = [(-1612.0, -1450.0), (-1610.0, -1504.0), (-1596.0, -1560.0), (-1578.0, -1640.0), (-1572.0, -1760.0), (-1572.0, -1900.0), (-1580.0, -2040.0), (-1620.0, -2100.0), (-1700.0, -2116.0),
          (-1850.0, -2118.0), (-1940.0, -2112.0), (-1990.0, -2080.0), on_line(J2, -40.0), on_line(J2, 0.0)]
G_line = curve(g_ctrl, 1)
add("G round The Corner's start", G_line, "gravel")
add("J2 The Long One", jline(J2, 0.0, catch_end(J2)), "jump")
# ---------------------------------------------------------------- X, the high haul bench: north across the pit floor, then up the north and north-west walls
x_ctrl = [on_line(J2, catch_end(J2) - 20.0), on_line(J2, catch_end(J2)), (-2000.0, -1200.0), (-2012.0, -1150.0), (-2050.0, -1115.0), (-2120.0, -1088.0), (-2200.0, -1072.0),
          (-2280.0, -1108.0), (-2370.0, -1172.0), (-2460.0, -1208.0), (-2550.0, -1232.0), (-2610.0, -1240.0), (-2655.0, -1252.0)]
x_y_ctrl = [None, 224.9, 226.0, 228.0, 233.0, 244.0, 256.0, 268.0, 283.0, 299.0, 314.0, 318.0, 318.0]
X_line = curve(x_ctrl, 1)
add("X the high haul bench", X_line, "bench")
# ---------------------------------------------------------------- T: the north-west rim road (OSM 9122211), the west rim south, across the plateau to The Big One
nw = roads[9122211]
nw = [q for q in nw if q[0] <= -2680.0]
t_ctrl = [X_line[-1]] + nw + [(-2890.0, -1440.0), (-2905.0, -1530.0), (-2905.0, -1640.0), (-2885.0, -1720.0), (-2820.0, -1800.0), (-2700.0, -1900.0),
                              (-2580.0, -1990.0), (-2460.0, -2075.0), (-2380.0, -2112.0), (-2330.0, -2118.0), on_line(J3, -60.0), on_line(J3, -30.0), on_line(J3, 0.0)]
T_line = curve(t_ctrl, 0)
add("T the west rim and the plateau", T_line, "gravel")
# ---------------------------------------------------------------- the loop: every piece end to end (each starts where the last one ends)
loop = []
for pc in pieces:
    p = pc["p"]
    if loop and math.dist(loop[-1], p[0]) < 0.5: p = p[1:]
    pc["i0"] = len(loop); loop += p; pc["i1"] = len(loop)
gaps = [(a["name"], b["name"], round(math.dist(a["p"][-1], b["p"][0]), 1)) for a, b in zip(pieces, pieces[1:] + pieces[:1])]
print("joins: " + "; ".join(f"{a} -> {b} {g} m" for a, b, g in gaps))
C = cum(loop); L_lap = C[-1] + math.dist(loop[-1], loop[0])
print(f"the lap: {L_lap:.0f} m; " + ", ".join(f"{pc['name']} {C[min(pc['i1'], len(C)) - 1] - C[pc['i0']]:.0f}" for pc in pieces))

# ---------------------------------------------------------------- heights: the benches to their designed profiles, the gravel roads on the smoothed ground
tg = [ground(*q) for q in loop]
def prof_from(pc, ctrl, ys):
    """heights along a piece: the control points' heights where given (each at the piece's point nearest it), straight between, the ends of a cutting
    held to the ground; smoothed"""
    p = loop[pc["i0"]:pc["i1"]]; c = cum(p); known = []
    for q, y in zip(ctrl, ys):
        if y is None: continue
        d, s = nearest_s(p, q, c)
        if d < 6.0: known.append((s, y))
    return c, known
def interp_known(c, known, fallback):
    known = sorted(known); ks = [k[0] for k in known]; kv = [k[1] for k in known]
    return [float(np.interp(s, ks, kv)) if ks and ks[0] <= s <= ks[-1] else fallback(i) for i, s in enumerate(c)]
prof = list(tg)
for pc in pieces:
    i0, i1 = pc["i0"], pc["i1"]; n = i1 - i0
    if pc["kind"] == "gravel":
        g = gsm(gsm(tg[i0:i1], 15.0), 15.0); prof[i0:i1] = g
    elif pc["name"].startswith("S"):
        c, known = prof_from(pc, s_ctrl, s_y_ctrl)
        # the cutting: from the bench's top (S_TOP_Y at its turn) up to the plateau's ground at its head, evenly
        s_top = max(k[0] for k in known); s_end = c[-1]; y_end = float(np.mean(gsm(tg[i1 - 20:i1], 6.0)[-5:]))
        known.append((s_end, y_end))
        prof[i0:i1] = interp_known(c, known, lambda i: tg[i0 + i])
    elif pc["name"].startswith("X"):
        c, known = prof_from(pc, x_ctrl, x_y_ctrl); known.append((c[-1], tg[i1 - 1]))
        prof[i0:i1] = interp_known(c, known, lambda i: tg[i0 + i])
    elif pc["name"].startswith("F"):
        # (evenly from The Corner's bench to the east rim road's height where the bench ends, held level 30 m at the hairpin's foot)
        c = cum(loop[i0:i1]); y0 = JJ["J1"]["bench_y"]; y1 = ground(*loop[i1 - 1])
        prof[i0:i1] = [y0 + (y1 - y0) * s / c[-1] for s in c]
for pc in pieces:
    if pc["kind"] in ("bench", "gravel"):
        i0, i1 = pc["i0"], pc["i1"]; prof[i0:i1] = gsm(prof[i0:i1], 4.0)
# ---------------------------------------------------------------- the report: grades, bends, cut and fill
def turn_r(k, w=4):
    a, b, c = loop[(k - w) % len(loop)], loop[k], loop[(k + w) % len(loop)]
    ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a)
    cr = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]))
    return 1e6 if cr < 1e-6 else ab * bc * ca / (2 * cr)
for pc in pieces:
    if pc["kind"] == "jump": continue
    i0, i1 = pc["i0"], pc["i1"]
    gr = [abs(prof[k + 5] - prof[k]) / max(1e-6, C[k + 5] - C[k]) for k in range(i0, i1 - 5)]
    rmin = min(turn_r(k) for k in range(i0, i1)); cf = [prof[k] - tg[k] for k in range(i0, i1)]
    print(f"  {pc['name']:32} {C[i1 - 1] - C[i0]:5.0f} m, {prof[i0]:.0f} to {prof[i1 - 1]:.0f} m, steepest {100 * max(gr or [0]):.1f} %, tightest bend {rmin:.0f} m, "
          f"cut {-min(cf):.0f} m, fill {max(cf):.0f} m")

# ---------------------------------------------------------------- the start line: on the west rim road (T), heading south; the lap's stations from it
k_start = min(range(len(loop)), key=lambda k: math.dist(loop[k], (-2905.0, -1575.0)))
def st(k): return round((C[k] - C[k_start]) % L_lap, 1)
def k_of(q, piece=None):
    rng = range(len(loop)) if piece is None else range(piece["i0"], piece["i1"])
    return min(rng, key=lambda k: math.dist(loop[k], q))
PC = {pc["name"].split(" ")[0]: pc for pc in pieces}
def jk(j, s): pc = PC[j["id"]]; return pc["i0"] + int(round(s / 2.0))   # a jump piece's point at its own station s (2 m apart from 0)

# ---------------------------------------------------------------- checkpoints (Woodstock Rush's rules: a missed one costs the route's penalty, the lap counts)
CPS = [(k_of((-2700.0, -1900.0), PC["T"]), "the runway's end", 0.0),
       (jk(J3, 30.0), "The Big One's run-up", 12.0),
       (jk(J3, J3["land_s"][-1] + 6.0), "The Big One's landing", 22.0),
       (k_of((-2420.0, -1296.0), PC["S"]), "the pit floor", 0.0),
       (k_of((-2760.0, -1545.0), PC["S"]), "the west end", 0.0),
       (k_of((-2380.0, -1668.0), PC["S"]), "the south cliff", 0.0),
       (k_of((-2000.0, -1640.0), PC["S"]), "under The Long One", 0.0),
       (k_of((CUT_X, -1900.0), PC["S"]), "the cutting", 0.0),
       (jk(J1, 30.0), "The Corner's run-up", 12.0),
       (jk(J1, J1["land_s"][-1] + 6.0), "The Corner's landing", 22.0),
       (k_of((-1940.0, -1118.0), PC["F"]), "the haul road's hairpin", 0.0),
       (k_of((-1612.0, -1450.0), PC["F"]), "the east rim", 0.0),
       (k_of((-1572.0, -1900.0), PC["G"]), "the east plateau", 0.0),
       (k_of((-1850.0, -2118.0), PC["G"]), "behind the start boxes", 0.0),
       (jk(J2, 30.0), "The Long One's run-up", 12.0),
       (jk(J2, J2["land_s"][-1] + 6.0), "The Long One's landing", 22.0),
       (k_of((-2200.0, -1072.0), PC["X"]), "the north wall", 0.0),
       (k_of((-2550.0, -1232.0), PC["X"]), "the north-west rim", 0.0),
       (k_of((-2863.0, -1384.0), PC["T"]), "the west rim", 0.0)]
cps = []
for k, label, half in CPS:
    c = {"s": st(k), "pos": [round(loop[k][0], 1), round(loop[k][1], 1)], "label": label}
    if half > 0: c["half_m"] = half
    cps.append(c)
cps.sort(key=lambda c: c["s"])
for i, c in enumerate(cps, 1): c["id"] = i
print("checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps))

# ---------------------------------------------------------------- closures: every road (and Free Roam's quarry jumps drive) that meets the course is barred where it
# leaves it, at the first place its whole barrier stands CLOSURE_CLEAR m off the course
CLOSURE_CLEAR = 6.0
dense = resample(loop + [loop[0]], 2.0)
cell = {}
for i, q in enumerate(dense): cell.setdefault((int(q[0] // 20), int(q[1] // 20)), []).append(i)
def course_gap(q):
    best = 1e9; cx, cz = int(q[0] // 20), int(q[1] // 20)
    for dx in (-2, -1, 0, 1, 2):
        for dz in (-2, -1, 0, 1, 2):
            for i in cell.get((cx + dx, cz + dz), ()): best = min(best, math.dist(q, dense[i]))
    return best
ways = [(r["osm_id"], r.get("name") or r["class"], r["width"], pts(r["points"])) for r in M["roads"]
        if any(-3000 < x < -1450 and -2260 < z < -950 for x, z in pts(r["points"]))]
ways.append((0, "the quarry jumps drive", 7.0, pts(Q["access"]["drive"])))
closures = []
for oid, name, width, wp in ways:
    p = resample(wp, 2.0); h = width / 2 + 3.0
    near = [course_gap(q) < BENCH_HALF + width / 2 + 1.0 for q in p]
    if not any(near) or all(near): continue
    for i in range(len(p)):
        for step in (1, -1):
            j = i + step
            if not (0 <= j < len(p)) or not near[i] or near[j]: continue
            k = j
            while 0 <= k < len(p):   # walk on along the road until the barrier's whole width is clear of the course
                q = p[k]; t_ = unit((p[min(k + 1, len(p) - 1)][0] - p[max(k - 1, 0)][0], p[min(k + 1, len(p) - 1)][1] - p[max(k - 1, 0)][1])); ac = (-t_[1], t_[0])
                if all(course_gap((q[0] + ac[0] * h * f, q[1] + ac[1] * h * f)) >= BENCH_HALF + CLOSURE_CLEAR for f in (-1, -.5, 0, .5, 1)): break
                k += step
            if not (0 <= k < len(p)): continue
            q = p[k]; t_ = unit((p[min(k + 1, len(p) - 1)][0] - p[max(k - 1, 0)][0], p[min(k + 1, len(p) - 1)][1] - p[max(k - 1, 0)][1]))
            if step < 0: t_ = (-t_[0], -t_[1])
            kc = min(range(len(loop)), key=lambda kk: math.dist(loop[kk], p[i]))
            closures.append({"road": name, "osm_id": oid, "x": round(q[0], 2), "z": round(q[1], 2), "dx": round(t_[0], 4), "dz": round(t_[1], 4), "width_m": width,
                             "junction_s": st(kc), "contact": [round(p[i][0], 1), round(p[i][1], 1)], "arc_m": round(abs(k - i) * 2.0, 1), "barrier_on": name,
                             "label": f"{name} off the Viking Quarry course"})
_cl = []   # (two barriers within 4 m of each other on one road: one)
for c in closures:
    if not any(o["osm_id"] == c["osm_id"] and math.dist((o["x"], o["z"]), (c["x"], c["z"])) < 4.0 for o in _cl): _cl.append(c)
closures = _cl
for i, c in enumerate(closures, 1): c["id"] = f"Q{i}"
print(f"{len(closures)} closures: " + ", ".join(f"{c['id']} {c['road']} ({c['x']:.0f}, {c['z']:.0f})" for c in closures))
junctions = [{"s": c["junction_s"], "roads": [c["road"]], "pos": c["contact"]} for c in closures]

# ---------------------------------------------------------------- the AI: held speeds
# the run-ups: each jump's speed window (the lip speeds whose flights come down on its landing deck, Free Roam's HUD window) at its middle
AI_LIP = {"J1": 41.0, "J2": 45.0, "J3": 45.5}
zones = []
def Z(id_, k0, k1, speed): zones.append({"id": id_, "s0": st(k0), "s1": st(k1), "speed": speed})
for j in (J1, J2, J3):
    Z(f"{j['id']} {j['name']}: the run-up", jk(j, max(20.0, j["lip_s"] - 250.0)), jk(j, j["lip_s"] + 1.0), AI_LIP[j["id"]])
in_jump = set()
for pc in pieces:
    if pc["kind"] == "jump": in_jump.update(range(pc["i0"], pc["i1"]))
k = 0; nb = 0
while k < len(loop):   # the bends: the speed a car takes them at about 9 m/s2 sideways (no slower than 12 m/s)
    if turn_r(k, 6) < 60.0 and k not in in_jump:
        e = k
        while e + 1 < len(loop) and turn_r(e + 1, 6) < 60.0: e += 1
        rmin = min(turn_r(i, 6) for i in range(k, e + 1)); v = round(max(12.0, min(26.0, math.sqrt(9.0 * rmin))), 1)
        Z(f"the bend at lap {st(k):.0f} m (radius {rmin:.0f} m)", max(0, k - 12), min(len(loop) - 1, e + 4), v); nb += 1; k = e + 1
    else: k += 1
zones = [z for z in zones if z["s1"] > z["s0"]]; zones.sort(key=lambda z: z["s0"])
# the run-ups narrower for the AI (the kickers 18 m wide: the AI's line kept 5 m either side of the middle)
widths = [{"id": f"{j['id']} {j['name']}: the run-up and kicker", "s0": st(jk(j, 20.0)), "s1": st(jk(j, j["lip_s"])), "half": 5.0} for j in (J1, J2, J3)]
print(f"{len(zones)} AI zones ({nb} bends)")

# ---------------------------------------------------------------- resets: anywhere a short jump ends up (the lake, the bank under the landing deck, the deck's front) back on
# the run-up's head before the jump, facing it; on the flight or the landing past the deck's front: on past the landing (but never past the next
# checkpoint: CarRecovery then puts it at the run-up's head too)
reset_skip = []
for j in (J3, J1, J2):
    d = j["dir"]; nl = (-d[1], d[0]); o = j["origin"]
    def W(s, lat, o=o, d=d, nl=nl): return (o[0] + d[0] * s + nl[0] * lat, o[1] + d[1] * s + nl[1] * lat)
    a0, a1 = j["lip_s"] + 45.0, j["land_s"][0] - 1.0
    poly = [W(a0, -34.0), W(a1, -34.0), W(a1, 34.0), W(a0, 34.0)]
    reset_skip.append({"id": f"{j['id']} {j['name']}", "s0": st(jk(j, j["lip_s"] - 2.0)), "s1": st(jk(j, j["land_s"][-1])), "to_s": st(jk(j, j["land_s"][-1] + 25.0)),
                       "short_s": st(jk(j, 8.0)), "short_area": flat(poly)})   # (the run-up's head: the whole run-up ahead, as Free Roam's start box)

# ---------------------------------------------------------------- the world pieces
grades, surfaces, berms, clear = [], [], [], []
# (the roads the course meets: every way with a closure, and the quarry jumps drive; the jumps' run-up heads)
shut_ids = {c["osm_id"] for c in closures}
shut_cells = {}
for oid, name, width, wp in ways:
    if oid in shut_ids:
        for q in resample(wp, 3.0):
            if course_gap(q) > BENCH_HALF + 3.0: shut_cells.setdefault((int(q[0] // 20), int(q[1] // 20)), []).append(q)   # (the parts off the course)
heads = [on_line(j, 0.0) for j in (J1, J2, J3)]
def every(lst, n): return lst[::n] + ([lst[-1]] if (len(lst) - 1) % n else [])
for pc in pieces:
    if pc["kind"] == "jump": continue
    i0, i1 = pc["i0"], pc["i1"]
    ks = list(range(max(0, i0 - 2), min(len(loop), i1 + 2)))   # (from 4 m back on the piece before: no gap at a join)
    P = [loop[k] for k in ks]; Y = [prof[k] for k in ks]
    lat_g = []   # the ground beyond each edge (left, right) and the road's height
    for k in ks:
        a, b = loop[max(0, k - 2)], loop[min(len(loop) - 1, k + 2)]; t_ = unit((b[0] - a[0], b[1] - a[1])); nn = (-t_[1], t_[0]); q = loop[k]
        gl = ground(q[0] + nn[0] * (BENCH_HALF + 6), q[1] + nn[1] * (BENCH_HALF + 6)); gr = ground(q[0] - nn[0] * (BENCH_HALF + 6), q[1] - nn[1] * (BENCH_HALF + 6))
        lat_g.append((gl, gr, prof[k]))
    if pc["name"][0] in "SX":
        # a bench on a quarry wall: the uphill side (the right: the cliff, the north wall) cut steeply (4 m: never into a kicker's pad on the rim above),
        # the lake or pit side banked down at a meter out per meter of fill (4 to 28 m); in the cutting both sides steep
        bl = [round(min(28.0, max(4.0, (y - min(gl, y)) + 3.0)), 2) for (gl, gr, y) in lat_g]
        if pc["name"][0] == "S":
            kc = k_of((CUT_X, -1662.0), pc)
            bl = [4.0 if ks[i] >= kc else b for i, b in enumerate(bl)]
        g = {"id": f"the Viking Quarry race: {pc['name']}", "points": flat(every(P, 2)), "y": [round(v, 3) for v in every(Y, 2)], "half": BENCH_HALF + 1.0, "blend": 6.0,
             "blend_left": every(bl, 2), "blend_r": 4.0, "open_ends": True}
    else:
        bl = [round(min(20.0, max(5.0, abs(y - 0.5 * (gl + gr)) + 4.0)), 2) for (gl, gr, y) in lat_g]
        g = {"id": f"the Viking Quarry race: {pc['name']}", "points": flat(every(P, 2)), "y": [round(v, 3) for v in every(Y, 2)], "half": BENCH_HALF + 1.0, "blend": 6.0,
             "blends": every(bl, 2), "open_ends": True, "legs": pc["name"][0] == "F"}
    grades.append(g)
    surfaces.append({"id": pc["name"], "points": flat(every(P, 2)), "half": BENCH_HALF})
    clear.append({"id": f"the Viking Quarry race: {pc['name']}", "points": flat(every(P, 4)), "half": CLEAR_HALF})
    # the berms: a low rock berm along an edge where the ground beyond falls 3 m or more below the road (a haul road's safety berm); none within
    # 14 m of a road the course meets or crosses (its closures), Free Roam's quarry jumps drive, or a jump's run-up head (nothing shut in Free Roam)
    def berm_ok(k, side):
        a, b = loop[max(0, k - 2)], loop[min(len(loop) - 1, k + 2)]; t_ = unit((b[0] - a[0], b[1] - a[1])); q = loop[k]
        e = (q[0] - t_[1] * side * (BENCH_HALF + .8), q[1] + t_[0] * side * (BENCH_HALF + .8))
        if any(math.dist(e, c) < 30.0 for c in heads): return False
        cx, cz = int(e[0] // 20), int(e[1] // 20)
        return not any(math.dist(e, w) < 14.0 for dx in (-1, 0, 1) for dz in (-1, 0, 1) for w in shut_cells.get((cx + dx, cz + dz), ()))
    for side in (1, -1):
        run = []
        for k, (gl, gr, y) in zip(ks + [None], lat_g + [(1e9, 1e9, 0.0)]):
            if k is not None and y - (gl if side > 0 else gr) >= 3.0 and berm_ok(k, side): run.append(k); continue
            if len(run) >= 10: berms.append({"id": f"{pc['name']} {'left' if side > 0 else 'right'} {len(berms)}", "points": flat([loop[i] for i in every(run, 2)]), "side": side, "lat": BENCH_HALF + 0.8})
            run = []
print(f"{len(grades)} grades, {len(berms)} berm runs ({sum(len(b['points']) // 2 for b in berms) * 4:.0f} m)")
# the pit dividers (race time only): the pit floor is open between the three landings' legs; a row of barriers keeps each to its own (a car landing
# The Big One cannot drive across to the haul bench behind The Long One, and so on)
dividers = [{"id": "between The Big One and The Long One", "points": flat(resample([(-2048.0, -1432.0), (-2046.0, -1300.0), (-2046.0, -1200.0), (-2090.0, -1168.0), (-2170.0, -1165.0),
                                                                                     (-2270.0, -1190.0), (-2370.0, -1236.0), (-2440.0, -1262.0)], 4.0))},
            {"id": "between The Long One and The Corner", "points": flat(resample([(-1972.0, -1452.0), (-1972.0, -1300.0), (-1974.0, -1160.0), (-1980.0, -1060.0)], 4.0))}]
for dv in dividers:
    dp = pts(dv["points"]); print(f"divider {dv['id']}: {cum(dp)[-1]:.0f} m, nearest the course {min(course_gap(q) for q in dp):.1f} m")
track = {"note": "Round 90: the Viking Quarry race's world pieces, from Tools/mapgen/make_quarry_route.py (MapBuilder.QuarryRace.cs). Coordinates are map meters (= Unity x, z).",
         "route_name": SETTINGS["name"], "grades": grades, "clear_lines": clear, "landing_zones": [], "surfaces": surfaces, "berms": berms, "dividers": dividers,
         "course": flat(every(loop, 4))}
lead = [point_at(loop[k_start - 40:k_start + 1], 2.0 * i) for i in range(41)]
loop_out = loop[k_start:] + loop[:k_start]
route = dict(SETTINGS)
route.update({"source": "Tools/mapgen/make_quarry_route.py: Viking Quarry, the airport's plateau, the quarry's benches and haul roads (OSM 1173718687, 30655025, 9122211), "
                        "Free Roam's three quarry jumps. Coordinates are map meters (= Unity x, z); heights come from the world.",
              "miss_penalty_s": 30.0, "ai_hill_targets": [56.0, 56.0, 56.0], "ai_crest_g": [30.0, 40.0, 50.0],
              "lap_length_m": round(L_lap, 1), "loop": flat(loop_out), "gate_s": 0.0, "lead_in": flat(lead), "lead_in_length_m": 80.0,
              "checkpoints": cps, "junctions": junctions, "loop_pieces": [{"osm_id": 0, "name": pc["name"], "reverse": False, "length_m": round(C[pc["i1"] - 1] - C[pc["i0"]], 1)} for pc in pieces],
              "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": [], "ai_zones": zones, "ai_widths": widths, "reset_skip": reset_skip})
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    os.makedirs(os.path.join(ROOT, "local/round90"), exist_ok=True)
    x0, x1, z0, z1 = -2960, -1500, -2200, -1000
    i0_, i1_ = int((x0 - OX) / SX), int((x1 - OX) / SX); j0_, j1_ = int((z0 - OZ) / SZ), int((z1 - OZ) / SZ)
    A = H[j0_:j1_:3, i0_:i1_:3].astype(float) / 65535 * (HHI - HLO) + HLO
    fig, ax = plt.subplots(figsize=(26, 26 * (z1 - z0) / (x1 - x0)))
    ls = matplotlib.colors.LightSource(315, 45)
    ax.imshow(ls.hillshade(A, vert_exag=1, dx=SX * 3, dy=SZ * 3), origin="lower", extent=(x0, x1, z0, z1), cmap="gray")
    ax.imshow(np.ma.masked_where(A > 114.5, A), origin="lower", extent=(x0, x1, z0, z1), cmap="Blues", alpha=.5)
    ax.contour(np.linspace(x0, x1, A.shape[1]), np.linspace(z0, z1, A.shape[0]), A, levels=range(120, 340, 20), colors="k", linewidths=.3)
    cols = {"jump": "orange", "bench": "r", "gravel": "m", "road": "b"}
    for pc in pieces:
        ax.plot(*zip(*pc["p"]), color=cols[pc["kind"]], lw=2.2)
        m_ = pc["p"][len(pc["p"]) // 2]; ax.text(m_[0], m_[1], pc["name"].split(" ")[0], fontsize=12, color=cols[pc["kind"]], weight="bold")
    for j in JJ.values():
        for s, mk in ((j["lip_s"], "k^"), (j["land_s"][0], "ks")): ax.plot(*on_line(j, s), mk, ms=9)
        for sd in j["stands"]: ax.plot(*sd["at"], "c*", ms=12)
    for k in range(0, len(loop), 250): ax.text(loop[k][0], loop[k][1], f"{st(k):.0f}", fontsize=7)
    for c in cps: ax.plot(*c["pos"], "go", ms=8); ax.text(c["pos"][0] + 8, c["pos"][1], f"CP{c['id']}", color="g", fontsize=11)
    for c in closures: ax.plot(c["x"], c["z"], "rx", ms=10, mew=3)
    for b in berms: ax.plot(*zip(*pts(b["points"])), color="brown", lw=1, ls=":")
    for dv in dividers: ax.plot(*zip(*pts(dv["points"])), color="k", lw=2.5)
    for rs in reset_skip: ax.fill(*zip(*pts(rs["short_area"])), color="c", alpha=.3)
    ax.plot(*loop[k_start], "ks", ms=14)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    plt.savefig(os.path.join(ROOT, "local/round90/layout.png"), dpi=45, bbox_inches="tight")
    fig, ax = plt.subplots(figsize=(30, 6))
    ax.plot(C, tg, color="#999", lw=.8); ax.plot(C, prof, "k-", lw=1.2)
    for pc in pieces: ax.axvline(C[pc["i0"]], color="r", lw=.6); ax.text(C[pc["i0"]], 330, pc["name"].split(" ")[0], fontsize=9)
    ax.set_ylim(100, 340); ax.grid(True, alpha=.3); plt.savefig(os.path.join(ROOT, "local/round90/profile.png"), dpi=45, bbox_inches="tight")
    print("pictures: local/round90/layout.png, profile.png")
