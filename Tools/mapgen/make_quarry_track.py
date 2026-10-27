"""Round 73 (C5, Dan, 2026-10-26 19:11: "I think for the quarry, it has to be some sort of track. We can have those ramps in free roam but having a
track will make it easier to at least discover the first time. Problem is its too big of an area to search for them"): THE QUARRY TRACK, a race
round Viking Quarry that takes in The Long One and The Corner (the quarry jumps, Tools/mapgen/make_quarry_jumps.py), forward only.

The ground decides the lap: every jump goes from the airport's plateau (about 310 m) north across the lake into the quarry's pit (about 225 m), and
the pit's only way out is its haul road (OSM service road 2525) climbing north-east to the east plateau. A lap with two jumps climbs out twice, so the
haul road and the track on south across the east plateau are a TWO-LANE haul road, a row of barriers down its middle while this race runs: the first
climb (after The Long One) takes the right lane, the second (after The Corner) the left. The lap (about 4.2 km):
  the start in The Long One's box on the quarry jumps drive; its run-up north across the airport, the jump, the landing deck, the run-out
  onto the haul road (right lane) up to the east plateau, the old service road (821) and a graded track south across the plateau
  a hairpin onto The Corner's run-up, the jump, the north-east bench, a hairpin onto the haul road (left lane, crossing the right lane there)
  up again and south again (left lane) to the quarry jumps drive, west along it behind The Corner's box to The Long One's box
Checkpoints lead you to each ramp's run-up in line with it. Only the vehicles that can make both jumps race it (the bikes and the four-wheelers
quick enough: the cars come up short), the AI holding each run-up at its speed window's middle.

What it writes: Data/routes/quarry_track.json (the race) and Data/tracks/quarry_track.json (its world pieces: the benches, the cleared lines, the
divider; merged into the mountain's planning by MapBuilder.QuarryJumps.cs). Re-run after make_quarry_jumps.py, then a full Build Map.
NOT BUILT (Round 74's check, the second try: one lane): the race's forgiving checkpoint rule credits ANY later checkpoint, so on the shared haul
road the AI crossed the second climb's checkpoint on its first climb and skipped The Corner's whole loop for 50 s of penalties; the human-like rider
stuck where the lap leaves The Long One's run-out for the haul road (-2000, -1322: a grass rise), 133 resets back onto the same spot; the start line
was not taken. Its output moved to local/round74/quarry_track_unbuilt/. What it needs (options for Dan): a per-route window on which later checkpoint
counts (only the next 3), and a graded way off the run-out onto the haul road. Round 73's check, NOT BUILT then: the human-like rider stuck on the haul road after The Long One, the AI riders took the haul road's other lane at the
crossing and skipped The Corner's whole loop for 20 s of penalties, and two of three AI did not finish. Its output is not in Data/ (the race is not in the
game, its benches and divider not in the world) until a lap without that crossing is chosen (TODO.md Round 73 C5: the options).
Usage: python Tools/mapgen/make_quarry_track.py [--plot]  (writes the two files; move them out of Data/ again if the lap is not fixed)"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/quarry_track.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/quarry_track.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
Q = json.load(open(os.path.join(ROOT, "Data/tracks/quarry_jumps.json"), encoding="utf-8"))


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
def offset(p, d):
    """the polyline p moved d to its left (+) or right (-)"""
    out = []
    for i in range(len(p)):
        a, b = p[max(0, i - 1)], p[min(len(p) - 1, i + 1)]; t = unit((b[0] - a[0], b[1] - a[1])); out.append((p[i][0] - t[1] * d, p[i][1] + t[0] * d))
    return out
def nearest(p, q):
    best = (1e18, 0, 0.0)
    for i, (a, b) in enumerate(zip(p, p[1:])):
        ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
        t = 0 if L2 < 1e-9 else max(0, min(1, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
        d = math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))
        if d < best[0]: best = (d, i, t)
    return best
def cut(p, q0, q1):
    """the part of p between its nearest points to q0 and q1 (in p's order)"""
    _, i0, t0 = nearest(p, q0); _, i1, t1 = nearest(p, q1)
    a = (p[i0][0] + t0 * (p[i0 + 1][0] - p[i0][0]), p[i0][1] + t0 * (p[i0 + 1][1] - p[i0][1]))
    b = (p[i1][0] + t1 * (p[i1 + 1][0] - p[i1][0]), p[i1][1] + t1 * (p[i1 + 1][1] - p[i1][1]))
    return [a] + p[i0 + 1:i1 + 1] + [b]
def fillet(cs, R, step=1.0):
    """the corners' polyline rounded with arcs of radius up to R (less where the legs are short); points every ~step"""
    out = [cs[0]]
    tang = []
    for i in range(1, len(cs) - 1):
        a, b, c = cs[i - 1], cs[i], cs[i + 1]; u1 = unit((b[0] - a[0], b[1] - a[1])); u2 = unit((c[0] - b[0], c[1] - b[1]))
        th = math.acos(max(-1, min(1, u1[0] * u2[0] + u1[1] * u2[1])))
        if th < 1e-3: tang.append(None); continue
        r = R[i] if isinstance(R, list) else R
        r = min(r, 0.48 * min(math.dist(a, b), math.dist(b, c)) / math.tan(th / 2)); d = r * math.tan(th / 2)
        t1 = (b[0] - u1[0] * d, b[1] - u1[1] * d); t2 = (b[0] + u2[0] * d, b[1] + u2[1] * d)
        cr = u1[0] * u2[1] - u1[1] * u2[0]; nrm = (-u1[1], u1[0]) if cr > 0 else (u1[1], -u1[0])
        ctr = (t1[0] + nrm[0] * r, t1[1] + nrm[1] * r); tang.append((t1, t2, ctr, th, cr, r))
    start = cs[0]
    for i in range(len(cs) - 1):
        tg = tang[i] if i < len(tang) else None
        end = tg[0] if tg else cs[i + 1]
        L = math.dist(start, end); n = max(1, int(round(L / step)))
        for k in range(1, n + 1): out.append((start[0] + (end[0] - start[0]) * k / n, start[1] + (end[1] - start[1]) * k / n))
        if tg:
            t1, t2, ctr, th, cr, r = tg; a0 = math.atan2(t1[1] - ctr[1], t1[0] - ctr[0]); n = max(2, int(round(r * th / step)))
            for k in range(1, n + 1): a = a0 + (th if cr > 0 else -th) * k / n; out.append((ctr[0] + r * math.cos(a), ctr[1] + r * math.sin(a)))
            start = t2
        else: start = cs[i + 1]
    return out


J = {j["id"]: j for j in Q["jumps"]}
def jp(j, s, lat=0.0): o, d = j["origin"], j["dir"]; return (o[0] + d[0] * s - d[1] * lat, o[1] + d[1] * s + d[0] * lat)
J1, J2 = J["J1"], J["J2"]
haul = list(reversed(pts(M["roads"][2525]["points"])))   # the haul road from its pit end (west) up to the east plateau
r821 = pts(M["roads"][821]["points"])                      # (-1367, -1329) ... (-1562, -1535), (-1609, -1457), (-1649, -1129) ...
drive = pts(Q["access"]["drive"])                          # the quarry jumps drive, west to east
# Round 74 (E, C5's first option, Dan asked for a track): ONE lane. Round 73's two lanes crossed where the second climb joined the haul road, and
# the racers (the AI's line too) took the other lane there and skipped The Corner. Now both climbs run the same lane, the same way (no crossing,
# no divider); a skip of The Corner's loop is paid for: its checkpoints (one every SKIP_CP_M along it and the second climb) cost more than it saves
LANE = 0.0                                                 # (Round 73: 7 m, the two lanes' middles either side of the haul road's)
SKIP_CP_M = 110.0

# ---------------------------------------------------------------- the shared two-lane stretch: up the haul road from where the second climb joins it
# (J1's run-out line meets it) to its top on road 821, 821 south, and a track south across the east plateau
def jline(j, t): return jp(j, j["lip_s"] + t)
# where J1's line crosses the haul road
best = None
for t in np.arange(200.0, 700.0, 0.5):
    d, i, u = nearest(haul, jline(J1, t))
    if best is None or d < best[0]: best = (d, t)
JOIN = jline(J1, best[1])
top = haul[-1]
plateau = [top] + cut(r821, (-1612.0, -1400.0), (-1562.0, -1535.0))[1:] + [(-1560.0, -1640.0), (-1561.0, -1760.0), (-1562.0, -1800.0)]
shared = resample(cut(haul, JOIN, top) + plateau[1:], 2.0)
right, left = offset(shared, -LANE), offset(shared, LANE)   # the first climb takes the right lane, the second the left
# ---------------------------------------------------------------- the lap's pieces in order (corner points; fillet rounds them)
# 1 The Long One: from its box straight north, the run-up, the jump, the deck, on through its catch to the haul road
s2_box = tuple(J2["start"])
best2 = min(((nearest(haul, jline(J2, t))[0], t) for t in np.arange(150.0, 600.0, 0.5)))
haul_j2 = jline(J2, best2[1]); j2_out = jline(J2, best2[1] - 30.0)   # where The Long One's line crosses the haul road; the lap bends onto it there
lower = cut(haul, haul_j2, JOIN)   # the haul road from where The Long One's run-out meets it to the join (the first climb alone: its middle)
# the first climb: onto the haul road's middle, easing into the right lane before the join
first = resample(lower, 4.0)
ease = 40.0; c_low = cum(first)
first = [(q[0] + (offset(first, -LANE)[k][0] - q[0]) * min(1, max(0, (c_low[k] - (c_low[-1] - ease)) / ease)),
          q[1] + (offset(first, -LANE)[k][1] - q[1]) * min(1, max(0, (c_low[k] - (c_low[-1] - ease)) / ease))) for k, q in enumerate(first)]
# 2 the hairpin off the right lane onto The Corner's run-up (joining it 60 m out of its box: the box's own first meters stay the Free Roam start's)
j1_merge = jp(J1, 70.0); j1_back = jp(J1, 30.0)
hair1 = [right[-1], (right[-1][0], right[-1][1] - 25.0), (j1_back[0] + 40.0, j1_back[1] - 10.0), j1_back]
# 3 The Corner: its run-up, the jump, the deck, the bench, on to the haul road (the hairpin onto the left lane, crossing the right)
j1_run_end = jline(J1, best[1] - 45.0)
hair2 = [j1_run_end, (JOIN[0] + 10.0, JOIN[1] - 30.0), left[0]]
# 4 the second climb and the plateau (left lane), then on south to the quarry jumps drive and west along it to The Long One's box
k_d1 = nearest(drive, (-1560.0, -1912.0))[1]; k_d2 = nearest(drive, (J2["start"][0], J2["start"][1] - 12.0))[1]
home = [left[-1], (left[-1][0], left[-1][1] - 60.0)] + list(reversed(drive[k_d2:k_d1 + 1])) + [(s2_box[0] + 18.0, s2_box[1] - 14.0), (s2_box[0], s2_box[1] - 6.0), s2_box]

corners = [s2_box, jp(J2, J2["lip_s"]), j2_out] + first[1:] + right[1:-1:6] + [right[-1]]
R = [0] * len(corners)
corners += hair1[1:]; R += [18.0] * (len(hair1) - 1)
corners += [j1_merge, jp(J1, J1["lip_s"]), j1_run_end]; R += [40.0, 0, 0]
corners += hair2[1:]; R += [16.0, 16.0]
corners += left[1:-1:6] + [left[-1]]; R += [30.0] * (len(left[1:-1:6]) + 1)
corners += home[1:]; R += [20.0] * (len(home) - 1)
R = [r if r else 30.0 for r in R]
for k, c in enumerate(corners):   # the jump lines stay straight: no rounding at their own points
    if c in (jp(J2, J2["lip_s"]), jp(J1, J1["lip_s"])): R[k] = 0.01
lap = fillet(corners, R, 2.0)
dd = [math.dist(a, b) for a, b in zip(lap, lap[1:])]
lap = [lap[0]] + [b for a, b, d_ in zip(lap, lap[1:], dd) if d_ > 0.3]
if math.dist(lap[0], lap[-1]) > 0.5: lap.append(lap[0])
C = cum(lap); L = C[-1]
def S(q): d_, i_, t_ = nearest(lap, q); return C[i_] + t_ * (C[i_ + 1] - C[i_])

# the start / finish line 30 m up The Long One's run-up (Round 73, the check: at the box itself the grid stood on the line's own plane and the
# player turned up the run-up outside it: the clock never started)
GATE_S = 30.0
# ---------------------------------------------------------------- checkpoints (half widths small where the two lanes run side by side)
CPS = [(jp(J2, 200.0), "The Long One's run-up", 12.0), (jp(J2, J2["land_s"][-1] + 40.0), "the north bench", 16.0),
       (first[len(first) // 2], "the haul road", 9.0), (right[len(right) // 3], "the haul road's top", 9.0), (right[int(len(right) * .8)], "the plateau", 9.0),
       (drive[(k_d1 + k_d2) // 2], "the quarry jumps drive", 9.0)]
# (Round 74) The Corner's loop and the second climb: a checkpoint every SKIP_CP_M from the hairpin off the plateau to the bottom of the second
# climb's plateau stretch (skipping it all misses them all: their 5 s each is more than the loop saves); none within 40 m of the first climb's
def S_after(q, s_min):   # (one lane: the second climb's points are the first's too; this finds the later pass)
    best_ = (1e18, 0.0)
    for k_ in range(len(lap) - 1):
        if C[k_] < s_min: continue
        d_ = math.dist(lap[k_], q)
        if d_ < best_[0]: best_ = (d_, C[k_])
    return best_[1]
s_lip1 = S(jp(J1, J1["lip_s"]))
s_loop0, s_loop1 = S(right[-1]) + 30.0, S_after(left[-1], s_lip1) - 30.0
first_cps = [S(q) for q, _, _ in CPS]
s_ = s_loop0
while s_ < s_loop1:
    if all(abs(s_ - f) > 40.0 for f in first_cps):
        i_ = min(len(lap) - 2, max(0, int(np.searchsorted(C, s_)) - 1)); t_ = (s_ - C[i_]) / max(C[i_ + 1] - C[i_], 1e-9)
        CPS.append(((lap[i_][0] + t_ * (lap[i_ + 1][0] - lap[i_][0]), lap[i_][1] + t_ * (lap[i_ + 1][1] - lap[i_][1])), "The Corner's loop", 12.0, s_))
    s_ += SKIP_CP_M
cps = []
for cp_ in CPS:
    q, label, half = cp_[:3]; s = cp_[3] if len(cp_) > 3 else S(q); i_ = min(len(lap) - 2, max(0, int(np.searchsorted(C, s)) - 1)); t_ = (s - C[i_]) / max(C[i_ + 1] - C[i_], 1e-9)
    p = (lap[i_][0] + t_ * (lap[i_ + 1][0] - lap[i_][0]), lap[i_][1] + t_ * (lap[i_ + 1][1] - lap[i_][1]))
    cps.append({"s": round(s, 1), "pos": [round(p[0], 1), round(p[1], 1)], "label": label, "half_m": half})
cps.sort(key=lambda c: c["s"])
for k, c in enumerate(cps, 1): c["id"] = k

# ---------------------------------------------------------------- closures: the roads that leave the lap (a barrier 20 m along each, away from it)
def closure(cid, label, road_pts, at, away, width):
    _, i_, t_ = nearest(road_pts, at)
    q = (road_pts[i_][0] + t_ * (road_pts[i_ + 1][0] - road_pts[i_][0]), road_pts[i_][1] + t_ * (road_pts[i_ + 1][1] - road_pts[i_][1]))
    u_ = unit((away[0] - q[0], away[1] - q[1])); p_ = (q[0] + u_[0] * 22.0, q[1] + u_[1] * 22.0)
    return {"road": label, "osm_id": 0, "x": round(p_[0], 2), "z": round(p_[1], 2), "dx": round(u_[0], 4), "dz": round(u_[1], 4), "width_m": width, "junction_s": round(S(q), 1),
            "contact": [round(q[0], 1), round(q[1], 1)], "arc_m": 22.0, "barrier_on": label, "id": cid, "label": label}
closures = [closure("B1", "the haul road into the pit", haul, haul_j2, haul[0], 6.0),
            closure("B2", "road 821 north", r821, top, (-1649.0, -1129.0), 6.0),
            closure("B3", "road 821 east", r821, (-1562.0, -1535.0), (-1442.0, -1459.0), 6.0),
            closure("B4", "the quarry jumps drive east", drive, drive[k_d1], drive[-1], 8.0),
            closure("B5", "the quarry jumps drive west", drive, drive[k_d2], drive[0], 8.0)]

# ---------------------------------------------------------------- the AI: the run-ups held at the windows' middles, the hairpins slow
zones = []
def Z(id_, s0, s1, v): zones.append({"id": id_, "s0": round(min(s0, s1), 1), "s1": round(max(s0, s1), 1), "speed": v})
for j, s_from in ((J2, 0.0), (J1, 70.0)):
    w = j["window"]; Z(f"{j['name']}: the run-up", S(jp(j, s_from)) + 1.0, S(jp(j, j["lip_s"])) + 1.0, round((w["lo"] + w["hi"]) / 2, 1))
    Z(f"{j['name']}: the landing and the catch", S(jp(j, j["land_s"][0])), S(jp(j, j["land_s"][-1] + 60.0)), 30.0)
for k in range(1, len(lap) - 1):   # every tight bend
    a, b, c = lap[k - 1], lap[k], lap[k + 1]
    cr = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])); ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a)
    r = 1e9 if cr < 1e-9 else ab * bc * ca / (2 * cr)
    if r < 30.0: Z("a hairpin", C[k] - 6.0, C[k] + 6.0, 12.0)
zones.sort(key=lambda z: z["s0"])
merged = []
for z in zones:
    if merged and z["id"] == "a hairpin" and merged[-1]["id"] == "a hairpin" and z["s0"] <= merged[-1]["s1"] + 2: merged[-1]["s1"] = max(merged[-1]["s1"], z["s1"])
    else: merged.append(z)
zones = merged

# ---------------------------------------------------------------- the world pieces: the benches (the two-lane haul road 2 x 7 m and its edges; the
# connectors and hairpins 7 m either side), cleared lines, the divider (race-time barriers along the haul road's middle)
def bench(id_, line, half, step=2.0, sig=4):
    line = resample(line, step); y = [ground(*q) for q in line]
    for _ in range(3): y = [float(np.mean(y[max(0, i - sig):i + sig + 1])) for i in range(len(y))]
    return {"id": id_, "points": flat(line), "y": [round(v, 3) for v in y], "half": half, "blend": 6.0, "open_ends": True}
grades = [bench("the quarry track's haul road", shared, 7.0),
          bench("the quarry track's way onto the haul road", [j2_out] + lower[1:4], 7.0),
          bench("the quarry track's hairpin onto The Corner", fillet(hair1 + [j1_merge], 18.0, 2.0), 7.5),
          bench("the quarry track's hairpin onto the haul road", fillet([j1_run_end] + hair2[1:], 16.0, 2.0), 7.5),
          bench("the quarry track's way down to the drive", [left[-1], (left[-1][0], left[-1][1] - 60.0), drive[k_d1]], 6.0)]
clear = [{"id": "the quarry track", "points": flat(lap[::3] + [lap[-1]]), "half": 14.0}]
divider = []   # (Round 74: one lane, no divider)

route = {
    "name": "Quarry Track", "direction": "forward", "directions": ["forward"], "laps": 2, "traffic": False,
    "vehicles": ["drifter", "moto", "atv", "scrambler", "mower"],
    "gate_half_width_m": 14.0, "checkpoint_half_width_m": 12.0, "gate_below_m": 6.0, "gate_above_m": 90.0, "reverse_grid_back_m": 45.0,
    "ai_hill_targets": [50.0, 50.0, 50.0], "ai_crest_g": [30.0, 40.0, 50.0],
    "source": "Tools/mapgen/make_quarry_track.py (Round 73 C5): The Long One and The Corner (Data/tracks/quarry_jumps.json), the pit's haul road (OSM 2525) as two lanes, road 821, the quarry jumps drive. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L, 1), "loop": flat(lap), "gate_s": GATE_S,
    "lead_in": flat(resample([lap[k] for k in range(len(lap)) if C[k] >= L - 72.0], 2.0)), "lead_in_length_m": 70.0,   # (ends at the loop's start, the box: the line is 30 m on)
    "checkpoints": cps, "junctions": [{"s": c["junction_s"], "roads": [c["road"]], "pos": c["contact"]} for c in closures],
    "loop_pieces": [{"osm_id": 0, "name": "the quarry track", "reverse": False, "length_m": round(L, 1)}],
    "official_shortcuts": [], "closures": closures, "closure_stubs_not_barred": [], "ai_zones": zones,

    # a reset anywhere from a jump's lip to past its landing deck's catch: on the run-out past it (never on the bank face under the flight)
    "reset_skip": [{"id": f"{j['id']} {j['name']}", "s0": round(S(jp(j, j["lip_s"])) - 2.0, 1), "s1": round(S(jp(j, j["land_s"][-1] + 30.0)), 1),
                    "to_s": round(S(jp(j, j["land_s"][-1] + 45.0)), 1)} for j in (J2, J1)],
}
track = {"note": "Round 73 (C5): the Quarry Track's world pieces, from Tools/mapgen/make_quarry_track.py; merged into the mountain's planning by "
                 "MapBuilder.QuarryJumps.cs. Coordinates are map meters (= Unity x, z).",
         "grades": grades, "clear_lines": clear, "divider": divider, "route_name": "Quarry Track"}
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print(f"the Quarry Track: a lap of {L:.0f} m; the shared two-lane stretch {cum(shared)[-1]:.0f} m (the haul road from ({JOIN[0]:.0f}, {JOIN[1]:.0f}) up to ({top[0]:.0f}, {top[1]:.0f}), 821, the plateau)")
print("checkpoints: " + ", ".join(f"CP{c['id']} {c['s']:.0f} {c['label']}" for c in cps))
print(f"{len(zones)} AI zones, {len(closures)} closures; wrote {os.path.relpath(OUT_ROUTE, ROOT)} and {os.path.relpath(OUT_TRACK, ROOT)}")
# the lap must never come near itself except where it should (the crossing at the haul road's join, the lanes side by side)
lap_d = resample(lap, 4.0); cd = cum(lap_d); close = []
for a in range(0, len(lap_d), 3):
    for b in range(a + 1, len(lap_d), 3):
        if cd[b] - cd[a] > 60 and L - (cd[b] - cd[a]) > 60 and math.dist(lap_d[a], lap_d[b]) < 9.0: close.append((round(cd[a]), round(cd[b]), round(math.dist(lap_d[a], lap_d[b]), 1)))
print(f"places where the lap passes within 9 m of itself: {len(close)}" + (f" (first: {close[:6]})" if close else ""))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(14, 14))
    for i, rd in enumerate(M["roads"]):
        p = pts(rd["points"])
        if any(-2400 < x < -1300 and -2100 < z < -1000 for x, z in p): ax.plot([q[0] for q in p], [q[1] for q in p], color="#bbb", lw=3)
    ax.plot([q[0] for q in lap], [q[1] for q in lap], "c-", lw=1.5)
    for c in cps: ax.plot(*c["pos"], "g^"); ax.text(*c["pos"], f"CP{c['id']}")
    for c in closures: ax.plot(c["x"], c["z"], "rs")
    ax.plot([q[0] for q in shared], [q[1] for q in shared], "k:", lw=1)
    ax.set_aspect("equal"); ax.grid(alpha=.3)
    os.makedirs(os.path.join(ROOT, "local/round74"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round74/quarry_track.png"), dpi=60, bbox_inches="tight")
