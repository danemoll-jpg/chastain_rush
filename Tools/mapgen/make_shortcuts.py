"""Round 8, item 4: the two official shortcuts of the Chastain Glen loop (forward), laid out from the terrain and the houses.

Hand choices (the lines, ramp sizes) are the constants below; everything else is computed from the data, so this re-runs
after any mapgen run. It writes:
  Data/shortcuts/chastain_glen_loop.json   what the map builder builds: the line, the dirt trail, the ramps / decks (solid,
                                           with colliders), the houses opened up for a passage, the house that is jumped,
                                           the corridor kept clear of trees, mailboxes and lamps, and the entry marker
  Data/routes/chastain_glen_loop.json      "official_shortcuts": the two entries the race uses (entry plane, forgiven checkpoints)
Picture of both, for checking: Data/screenshots/round8/shortcut_layout.png (with --plot).

Heights are absolute (the game's terrain from Assets/Generated/terrain.r16, the same samples Unity builds its terrain from).
A "prism" is a solid: a convex outline in plan, its top following a height profile along an axis, sides down into the ground.

Usage: python Tools/mapgen/make_shortcuts.py [--plot]
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/shortcuts/chastain_glen_loop.json")
ROUTE = os.path.join(ROOT, "Data/routes/chastain_glen_loop.json")

meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
route = json.load(open(ROUTE, encoding="utf-8"))

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)

V = lambda x, z: np.array([x, z], float)
def unit(v): return v / np.linalg.norm(v)
def left(d): return np.array([-d[1], d[0]])   # 90 degrees anticlockwise seen from above (x east, z north)

# ---------------------------------------------------------------- the lap (distances from the start / finish gate, as in the Round 6 report)
loop = np.array(route["loop"], float).reshape(-1, 2)
seg = np.linalg.norm(np.diff(loop, axis=0), axis=1); CUM = np.concatenate([[0], np.cumsum(seg)])
GATE = route["gate_s"]
def lap(s):
    s = (s + GATE) % CUM[-1]; k = max(0, min(int(np.searchsorted(CUM, s)) - 1, len(seg) - 1)); t = (s - CUM[k]) / seg[k]
    return loop[k] * (1 - t) + loop[k + 1] * t

# ---------------------------------------------------------------- houses
def footprint(i): return np.array(m["buildings"][i]["footprint"], float).reshape(-1, 2)
def rect(i):
    """Best-fit rectangle (as MapBuilder.BestRectangle): centre, unit axes and half sizes, and its 4 corners anticlockwise."""
    f = footprint(i); best = None
    for k in range(len(f)):
        d = f[(k + 1) % len(f)] - f[k]; n = np.linalg.norm(d)
        if n < 1e-3: continue
        d = d / n; p = left(d); u = f @ d; v = f @ p; a = (u.max() - u.min()) * (v.max() - v.min())
        if best is None or a < best[0]:
            c = d * (u.max() + u.min()) / 2 + p * (v.max() + v.min()) / 2
            best = (a, c, d, p, (u.max() - u.min()) / 2, (v.max() - v.min()) / 2)
    _, c, d, p, hl, hw = best
    return {"c": c, "d": d, "p": p, "hl": hl, "hw": hw, "corners": [c + d * hl + p * hw, c - d * hl + p * hw, c - d * hl - p * hw, c + d * hl - p * hw]}

def ray_box(r, o, u):
    """Where the line o + t u enters and leaves house rectangle r (t values), or None."""
    t0, t1 = -1e9, 1e9
    for ax, half in ((r["d"], r["hl"]), (r["p"], r["hw"])):
        a = float((o - r["c"]) @ ax); b = float(u @ ax)
        if abs(b) < 1e-9:
            if abs(a) > half: return None
            continue
        ta, tb = (-half - a) / b, (half - a) / b
        t0, t1 = max(t0, min(ta, tb)), min(t1, max(ta, tb))
    return (t0, t1) if t0 < t1 else None

def wall_line(r, o, u, exit=True):
    """The house wall the line o + t u leaves (exit) or enters through: a point on it and its direction."""
    t0, t1 = ray_box(r, o, u); t = t1 if exit else t0; q = o + u * t
    cs = r["corners"]; best = None
    for k in range(4):
        a, b = cs[k], cs[(k + 1) % 4]; ab = b - a; tt = np.clip((q - a) @ ab / (ab @ ab), 0, 1); dd = np.linalg.norm(a + ab * tt - q)
        if best is None or dd < best[0]: best = (dd, a, unit(ab))
    return q, best[2]

def intersect(p, d, q, e):
    """Point where the line p + a d meets the line q + b e."""
    A = np.array([[d[0], -e[0]], [d[1], -e[1]]]); a, b = np.linalg.solve(A, q - p)
    return p + d * a

# ---------------------------------------------------------------- building blocks of the output

def polyline(points, step=2.0):
    out = [np.array(points[0], float)]
    for a, b in zip(points[:-1], points[1:]):
        a, b = np.array(a, float), np.array(b, float); n = max(1, int(math.ceil(np.linalg.norm(b - a) / step)))
        for k in range(1, n + 1): out.append(a + (b - a) * k / n)
    return out

def smooth(pts, iterations=3, step=2.0):
    """Chaikin corner cutting (straights stay straight, corners become curves), then resampled every 'step' metres."""
    P = [np.array(p, float) for p in pts]
    for _ in range(iterations):
        Q = [P[0]]
        for a, b in zip(P[:-1], P[1:]): Q += [a * 0.75 + b * 0.25, a * 0.25 + b * 0.75]
        Q.append(P[-1]); P = Q
    out = [P[0]]; carry = 0.0
    for a, b in zip(P[:-1], P[1:]):
        L = float(np.linalg.norm(b - a)); d = step - carry
        while d <= L: out.append(a + (b - a) * (d / L)); d += step
        carry = L - (d - step)
    if np.linalg.norm(out[-1] - P[-1]) > 0.3: out.append(P[-1])
    return out

def length(pts): return sum(float(np.linalg.norm(b - a)) for a, b in zip(pts[:-1], pts[1:]))
def station(pts, q):
    best = (1e9, 0.0); acc = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        ab = b - a; L = float(np.linalg.norm(ab)); t = float(np.clip((q - a) @ ab / (L * L), 0, 1)); d = float(np.linalg.norm(a + ab * t - q))
        if d < best[0]: best = (d, acc + t * L)
        acc += L
    return best[1]

LANE_CLEAR = 5.0   # nothing built within this of the lap's centreline (the lane is 8 m wide): a ramp's corner on the lane
                   # stopped a car driving the plain lap (Round 8 race check)

def lap_dist(q):
    best = (1e9, None)
    for a, b in zip(loop[:-1], loop[1:]):
        ab = b - a; t = float(np.clip((q - a) @ ab / (ab @ ab), 0, 1)); d = float(np.linalg.norm(a + ab * t - q))
        if d < best[0]: best = (d, (a, b))
    return best

def clip_off_lane(poly):
    """Cuts a convex outline back to LANE_CLEAR metres from the lap's centreline (the nearest lap segment, as a line)."""
    P = [np.array(p, float) for p in poly]
    d, seg = min((lap_dist(p) for p in P), key=lambda x: x[0])
    if d >= LANE_CLEAR: return P
    a, b = seg; t = unit(b - a); nrm = left(t)
    c = sum(P) / len(P)
    side = 1.0 if (c - a) @ nrm > 0 else -1.0
    f = lambda q: side * float((q - a) @ nrm) - LANE_CLEAR
    out = []
    for i in range(len(P)):
        p, q = P[i], P[(i + 1) % len(P)]; fp, fq = f(p), f(q)
        if fp >= 0: out.append(p)
        if (fp >= 0) != (fq >= 0): out.append(p + (q - p) * (fp / (fp - fq)))
    return out

def cut_edges(poly):
    """Indices of the outline edges that lie along the lane cut (both ends LANE_CLEAR from the centreline)."""
    P = [np.array(p, float) for p in poly]
    return [i for i in range(len(P)) if abs(lap_dist(P[i])[0] - LANE_CLEAR) < 0.05 and abs(lap_dist(P[(i + 1) % len(P)])[0] - LANE_CLEAR) < 0.05]

def prism(name, kind, poly, origin, axis, profile, colour, bank=None):
    poly = clip_off_lane(poly)
    # edges along the lane cut get a sloped skirt down to the ground (MapBuilder), not a vertical face a car could hit
    return {"skirt_edges": cut_edges(poly),"bank": [round(float(v), 4) for sb in (bank or []) for v in sb],"name": name, "kind": kind, "poly": [round(float(v), 3) for p in poly for v in p], "origin": [round(float(origin[0]), 3), round(float(origin[1]), 3)],
            "axis": [round(float(axis[0]), 5), round(float(axis[1]), 5)], "profile": [round(float(v), 3) for s, y in profile for v in (s, y)], "colour": colour}

def strip_poly(o, u, s0, s1, half_l, half_r=None):
    """Convex quad along u from s0 to s1, half_l to the left and half_r to the right."""
    half_r = half_l if half_r is None else half_r; n = left(u)
    return [o + u * s0 - n * half_r, o + u * s1 - n * half_r, o + u * s1 + n * half_l, o + u * s0 + n * half_l]

def max_ground(poly, step=0.5):
    xs = [p[0] for p in poly]; zs = [p[1] for p in poly]; best = -1e9
    P = np.array(poly)
    for x in np.arange(min(xs), max(xs) + step, step):
        for z in np.arange(min(zs), max(zs) + step, step):
            if inside(V(x, z), P): best = max(best, ground(x, z))
    return best

def inside(q, P):
    c = False
    for i in range(len(P)):
        a, b = P[i], P[(i + 1) % len(P)]
        if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]: c = not c
    return c

def clip_to_rect(poly_line_a, u, r, half):
    """The corridor (centre line a + t u, half width 'half') inside house rectangle r: the t range covering it."""
    n = left(u); ts = []
    for off in (-half, 0, half):
        hit = ray_box(r, poly_line_a + n * off, u)
        if hit: ts += list(hit)
    return min(ts), max(ts)

def arc_onto_lap(P0, h0, s_min, s_max, turn=+1):
    """A circular arc that leaves P0 heading h0, turns (left = +1) and ends tangent to the lap, on its centreline.
    Returns the arc points and the lap station where it ends (radius chosen to land on the lap)."""
    best = None
    for s_end in np.arange(s_min, s_max, 0.5):
        q = lap(s_end); qn = lap(s_end + 0.5); h1 = unit(qn - q)
        ang = math.atan2(h0[0] * h1[1] - h0[1] * h1[0], h0 @ h1)
        if turn * ang <= 0.05: continue
        # radius so that the arc from P0 with heading h0 ends at q: the chord makes half the turn angle with h0
        chord = q - P0; L = float(np.linalg.norm(chord))
        if L < 1: continue
        cang = math.atan2(h0[0] * chord[1] - h0[1] * chord[0], h0 @ chord)
        err = abs(cang - ang / 2)
        R = L / (2 * math.sin(abs(ang) / 2))
        if best is None or err < best[0]: best = (err, s_end, ang, R)
    err, s_end, ang, R = best
    c = P0 + left(h0) * R * np.sign(ang)
    a0 = math.atan2(*(P0 - c)[::-1])
    pts = [c + R * np.array([math.cos(a0 + ang * k / 24), math.sin(a0 + ang * k / 24)]) for k in range(25)]
    return pts, s_end, R, math.degrees(ang), err

WOOD = [0.47, 0.33, 0.20]; RAMP = [0.55, 0.40, 0.24]; DECK = [0.50, 0.36, 0.22]; FLOOR = [0.50, 0.35, 0.21]; DIRT = [0.38, 0.255, 0.135]

notes = []
def note(s): notes.append(s); print(s)

# =============================================================================================================== #2
# "Two-house drop": leaves Chastain Glen Lane about 180 m after the gate (just past CP1), drives THROUGH house 1763 on the
# crest (its floor is the top of the hill), off the natural lip behind it where the ground falls about 8 m in 18 m, on to a
# built landing ramp that runs down into house 1627 (the second house is straight ahead, on the same line: the landing
# ramp ends at its door), out of its back, through the gap between houses 1615 and 1459, over the crest and down the
# second slope, between houses 1602 and 1919 and back on to the lane about 540 m after the gate. Skips CP2.
def build_sc2():
    A = rect(1763); B = rect(1627)
    u = unit(B["c"] - A["c"])                         # the straight line through both houses' centres
    o = A["c"]                                         # station 0 = the middle of house 1763
    pA = ray_box(A, o, u); pB = ray_box(B, o, u)
    note(f"#2 line through houses 1763 and 1627: direction ({u[0]:.3f}, {u[1]:.3f}); 1763 walls at t {pA[0]:.1f} / {pA[1]:.1f}, 1627 at {pB[0]:.1f} / {pB[1]:.1f} m from 1763's centre")
    HALF = 3.6          # passage half width (7.2 m: wider than a two-car garage door, enough to hit at speed)
    PASS_H = 3.7        # passage height
    PASS_H_B = 4.6      # house 1627: higher, because a car that flies long off the lip arrives at its door in the air
    # floors: level with the highest ground the corridor crosses inside each house, plus a little
    def house_floor(r, t0, t1):
        n = left(u); g = -1e9
        for t in np.arange(t0, t1 + 0.01, 0.5):
            for off in np.arange(-HALF, HALF + 0.01, 0.5):
                q = o + u * t + n * off
                if ray_box(r, q, u) is not None and abs(float((q - r["c"]) @ r["d"])) <= r["hl"] and abs(float((q - r["c"]) @ r["p"])) <= r["hw"]: g = max(g, ground(*q))
        return g + 0.06
    a0, a1 = clip_to_rect(o, u, A, HALF); b0, b1 = clip_to_rect(o, u, B, HALF)
    fA = house_floor(A, a0, a1); fB = house_floor(B, b0, b1)
    lip_t = None
    for t in np.arange(a1, a1 + 25, 0.25):            # the lip: where the ground behind 1763 starts to fall away
        if ground(*(o + u * (t + 2))) < ground(*(o + u * t)) - 0.25: lip_t = t; break
    yLip = ground(*(o + u * lip_t))
    note(f"#2 floors: house 1763 {fA:.2f} m, house 1627 {fB:.2f} m; natural lip {lip_t - a1:.1f} m behind 1763 at ({(o + u * lip_t)[0]:.1f}, {(o + u * lip_t)[1]:.1f}), ground {yLip:.2f}; "
         f"lip to 1627's wall {b0 - lip_t:.1f} m, {yLip - fB:.1f} m lower")
    prisms = []
    # the passage floors (solid; the house's own collider is rebuilt around the passage by the map builder)
    prisms.append(prism("sc2 floor in house 1763", "floor", strip_poly(o, u, a0 - 0.3, a1 + 0.3, HALF), o, u, [(a0 - 0.3, fA), (a1 + 0.3, fA)], FLOOR))
    prisms.append(prism("sc2 floor in house 1627", "floor", strip_poly(o, u, b0 - 0.3, b1 + 0.3, HALF), o, u, [(b0 - 0.3, fB), (b1 + 0.3, fB)], FLOOR))
    # the drive up to 1763's door: the house sits on the crest, its floor about 4 m above the lane, so the ramp is long (it
    # starts at the lane's edge) and eased at both ends: a short steep one threw the car into the passage ceiling
    APRON = 25.0
    gIn = ground(*(o + u * (a0 - APRON)))
    def ease(t): return t * t * (3 - 2 * t)
    # the ramp's corner on the lane side is cut back off the lane; it starts rising only past that cut, so the cut edge
    # stays at ground level (a car turning in late meets no step)
    cutp = [q for q in clip_off_lane(strip_poly(o, u, a0 - APRON, a0 - 0.3, HALF + 0.6)) if abs(lap_dist(q)[0] - LANE_CLEAR) < 0.05]
    s_rise = max([float((q - o) @ u) for q in cutp] + [a0 - APRON])
    gIn = min([ground(*q) for q in cutp] + [ground(*(o + u * s_rise))])
    L_r = (a0 - 0.3) - s_rise
    ap = [(a0 - APRON, gIn - 0.05)] + [(s_rise + k, gIn - 0.05 + (fA - gIn + 0.05) * ease(k / L_r)) for k in np.linspace(0, L_r, 24)]
    APRON = L_r
    worst = min(y - ground(*(o + u * t)) for t, y in ap)
    note(f"#2 drive-up ramp: {APRON:.0f} m, {fA - gIn:.1f} m of rise, steepest {math.degrees(math.atan(1.5 * (fA - gIn) / APRON)):.0f} deg; lowest point {worst:+.2f} m against the ground under it")
    prisms.append(prism("sc2 ramp up to house 1763", "apron", strip_poly(o, u, a0 - APRON, a0 - 0.3, HALF + 0.6), o, u, ap, RAMP))
    # behind 1763: level from its back door to the lip (the yard is a little lower than the floor in places)
    yDeck = max(yLip, fA - 0.3)
    prisms.append(prism("sc2 deck to the lip", "deck", strip_poly(o, u, a1 + 0.3, lip_t + 1.0, HALF + 0.4), o, u, [(a1 + 0.3, fA), (lip_t + 1.0, yDeck)], DECK))
    # the landing: a concave ramp (steep where the car comes down, flattening into 1627's door), shaped from the flight
    # paths off the lip: a quadratic curve from just past the lip down to the floor of 1627 at its door; its bend is placed
    # where the flights from 18 to 26 m/s meet it at the smallest angle
    x0, x1 = lip_t + 2.0, b0 - 0.3
    def ramp_y(cx, cy, x):
        # quadratic Bezier (x0, y0) - (cx, cy) - (x1, fB), solved for the parameter at x
        y0 = yDeck - 0.9
        a = x0 - 2 * cx + x1; b = 2 * (cx - x0); c = x0 - x
        t = (-c / b) if abs(a) < 1e-9 else (-b + math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)
        t = min(max(t, 0.0), 1.0)
        return (1 - t) ** 2 * y0 + 2 * (1 - t) * t * cy + t * t * fB
    def flights(cx, cy, speeds=(18, 20, 22, 24, 26)):
        out = []
        for v in speeds:
            for k in range(1, 400):
                x = lip_t + 1.0 + k * 0.1; tt = (x - (lip_t + 1.0)) / v; y = yDeck + 0.6 - 4.9 * tt * tt
                ry = ramp_y(cx, cy, x) if x <= x1 else fB
                if y <= ry + 0.6:
                    dy = -9.8 * tt; ang = math.degrees(math.atan2(-dy, v))
                    h = 0.05; slope = (ramp_y(cx, cy, x + h) - ramp_y(cx, cy, x - h)) / (2 * h) if x < x1 - h else 0.0
                    out.append((v, x - lip_t, ang - math.degrees(math.atan(-slope)), x < x1 - 1.0)); break
        return out
    best = None
    for cx in np.arange(x0 + 3, x1 - 3, 0.5):
        for cy in np.arange(fB, yDeck - 1.0, 0.25):
            fl = flights(cx, cy)
            if len(fl) < 5 or not all(f[3] for f in fl): continue
            score = max(abs(f[2]) for f in fl)
            if best is None or score < best[0]: best = (score, cx, cy, fl)
    _, cx, cy, fl = best
    xs = np.linspace(x0, x1, 30)
    profile = [(x, max(ramp_y(cx, cy, x), ground(*(o + u * x)) + 0.03)) for x in xs] + [(b0 - 0.3, fB)]
    land_poly = strip_poly(o, u, x0, b0 - 0.3, HALF + 1.6)
    prisms.append(prism("sc2 landing ramp into house 1627", "landing", land_poly, o, u, profile, DECK))
    note("#2 landing ramp: concave, from " + f"{x0 - lip_t:.1f} m past the lip to 1627's door; flights off the lip (ballistic estimate): " +
         "; ".join(f"{v} m/s lands {x:.0f} m out, {d:+.0f} deg against the ramp" for v, x, d, _ in fl))
    # out of 1627's back: down to the yard
    gOut = ground(*(o + u * (b1 + 6)))
    REJOIN_2 = 0
    prisms.append(prism("sc2 ramp out of house 1627", "apron", strip_poly(o, u, b1 + 0.3, b1 + 6, HALF + 0.4), o, u, [(b1 + 0.3, fB), (b1 + 6, gOut - 0.05)], RAMP))
    exitB = o + u * (b1 + 6)
    # then through the gap between 1615 and 1459. The gap runs almost due north and is narrowest (6.9 m between the two
    # houses' back corners) at its south end, so the line lines up with it 10 m before it and goes through its middle.
    # then one straight line through the gap between 1615 and 1459 (6.9 m wide at its narrow south end) and on between
    # 1602 and 1919: the ground falls 8 m over a crest in the middle of it, and the car is in the air there, so the line
    # must not bend on the crest. Clearances from the line: 2.3 m to 1615's corners, 2.7 m to 1459's, 3.3 / 4.7 m at 1602 / 1919.
    M = V(3036.6, 1145.9); G2 = V(3019.2, 1200.0)
    uL = unit(G2 - M)
    J0 = M - uL * 13.0                                # where the car is on that line, 12 m before the crest
    back = o + u * (b1 + 0.3)
    arc, s_end, R, ang, err = arc_onto_lap(G2 + uL * 6.0, uL, 520, 560, +1)
    note(f"#2 back on to the lane: an arc of radius {R:.0f} m turning {ang:.0f} deg left, joining the lane {s_end:.0f} m after the gate (fit {err:.2f} rad)")
    pts = polyline([lap(176), lap(186), o + u * (a0 - 28), o + u * (a0 - 9)], 2.0) + polyline([o + u * (a0 - 9), back], 2.0)[1:]         + polyline([back, back + u * 3.0, J0, M, G2, G2 + uL * 6.0], 2.0)[1:] + polyline([p for p in arc] + [lap(s_end + 8)], 2.0)[1:]
    for hn in (1615, 1459, 1602, 1919):
        lat = min(abs(float((cn - M) @ left(uL))) for cn in rect(hn)["corners"] if -15 < float((cn - M) @ uL) < 70)
        note(f"#2 house {hn}: nearest corner {lat:.1f} m from the straight through the gap")
    G1b = M + uL * 8.0
    exitB = back + u * 6.0
    REJOIN_2 = s_end + 8
    lip2 = G1b
    entry_t = a0 - 0.5   # the entry plane: at 1763's door
    dense = smooth(pts)
    entry_s = station(dense, o + u * entry_t)
    trail = [[0.0, station(dense, o + u * (a0 - 9))], [station(dense, exitB), length(dense)]]
    open_houses = [
        {"building": 1763, "a": list(map(float, o + u * (a0 - 1))), "b": list(map(float, o + u * (a1 + 1))), "half_width": HALF, "floor_y": round(fA, 3), "height": PASS_H},
        {"building": 1627, "a": list(map(float, o + u * (b0 - 1))), "b": list(map(float, o + u * (b1 + 1))), "half_width": HALF, "floor_y": round(fB, 3), "height": PASS_H_B},
    ]
    lipP = o + u * lip_t
    return {
        "id": "sc2_two_houses", "name": "Two-house drop", "forgives": [2],
        "path": dense, "half_width": 4.0, "trail": trail, "clear_half_width": 6.5,
        "entry_s": entry_s, "prisms": prisms, "open_houses": open_houses, "jump_houses": [],
        "jumps": [{"label": "natural lip behind house 1763", "x": float(lipP[0]), "z": float(lipP[1])}, {"label": "crest past the gap between 1615 and 1459", "x": float(lip2[0]), "z": float(lip2[1])}],
        "leave_lap_s": 176, "rejoin_lap_s": float(REJOIN_2),
    }

# =============================================================================================================== #6
# "Rooftop leap": leaves Chastain Glen Lane about 742 m after the gate, turns 25 degrees left on to a big built kicker in
# the front yard of house 1641, flies OVER house 1641 (diagonally, ridge about 9.4 m up), comes down on a long landing deck
# that starts against the house's back wall, runs on straight through the back yards and the 11 m gap between houses 713
# and 1052, and turns hard left on to the lane about 985 m after the gate (by CP5, which it forgives). House 713, the
# second house on the straight candidate line, is passed on its west side, not jumped.
LIP_GAP = 2.5
def build_sc6():
    J = rect(1641)
    o = J["c"]
    # jump direction: from the lane over the middle of 1641 toward the open back yards. It is boxed in: house 1499's
    # front corner is 1.2 m from the kicker's side, house 1622's corner 1 m from the deck's
    u = unit(V(-0.428, -0.904))
    GAP = V(2760.5, 1014.8)          # the 11 m gap between houses 713 and 1052: the line bends 30 degrees right into it after the deck
    t0, t1 = ray_box(J, o, u)
    # kicker: an easement (the bend tightens gradually over 3 m, so the suspension is not hit by a sudden change), a circular
    # curve of radius 21 m (the car's suspension can follow it at racing speed; a tighter one threw the body into the ramp)
    # up to 33 degrees, then straight at 33 degrees to the lip, 5.6 m above the ground (6.6 m slowed the car so much on the climb that it landed on the roof)
    KICK_H, KICK_R, KICK_A, KICK_E = 5.6, 21.0, math.radians(33.0), 3.0
    a_e = math.atan(KICK_E / (2 * KICK_R)); y_e = KICK_E ** 2 / (6 * KICK_R)
    arc_x = KICK_R * (math.sin(KICK_A) - math.sin(a_e)); arc_y = KICK_R * (math.cos(a_e) - math.cos(KICK_A))
    KICK_L = KICK_E + arc_x + (KICK_H - y_e - arc_y) / math.tan(KICK_A)
    lip_t = t0 - LIP_GAP             # yard between the lip and the front wall: the car is higher by the time it reaches the eaves
    k0 = lip_t - KICK_L
    K0 = o + u * k0
    gK = ground(*K0)
    cx_ = KICK_E - KICK_R * math.sin(a_e); cy_ = y_e + KICK_R * math.cos(a_e)    # centre of the curve
    def kick(s):                     # s from the kicker start
        if s <= KICK_E: return gK - 0.03 + s ** 3 / (6 * KICK_R * KICK_E)
        if s <= KICK_E + arc_x: return gK - 0.03 + cy_ - math.sqrt(max(KICK_R ** 2 - (s - cx_) ** 2, 0.0))
        return gK - 0.03 + y_e + arc_y + (s - KICK_E - arc_x) * math.tan(KICK_A)
    end_slope = math.tan(KICK_A)
    note(f"#6 kicker: {KICK_L:.0f} m long, lip {KICK_H:.1f} m above the ground at its start, last slope {math.degrees(math.atan(end_slope)):.0f} deg; lip {t0 - lip_t:.1f} m before 1641's wall; "
         f"house crossed diagonally over {t1 - t0:.1f} m")
    # the yard rises to the left across the kicker's foot (about 20%): the foot is banked to match it, levelling out over
    # 10 m, so the car does not ride with one side on the bank and the other on a level ramp
    nK = left(u)
    def cross_at(t):
        q = o + u * t; return (ground(*(q + nK * 3.0)) - ground(*(q - nK * 3.0))) / 6.0
    bank = []
    for t in np.arange(k0, k0 + 14.01, 1.0):
        fade = min(1.0, max(0.0, (k0 + 14.0 - t) / 4.0))          # the bank fades out over the kicker's last metres of it
        bank.append((t, cross_at(t) * fade))
    bank.append((k0 + 14.5, 0.0))
    cross = max(abs(b[1]) for b in bank)
    note(f"#6 kicker's lower 14 m banked to the ground's cross slope there (up to {cross * 100:.0f}%), level from there to the lip")
    prisms = [prism("sc6 kicker over house 1641", "kicker", strip_poly(o, u, k0, lip_t, 3.8), o, u, [(k0 + s, kick(s)) for s in np.linspace(0, KICK_L, 37)], RAMP, bank)]
    # a wide apron so the kicker can be met from the road at an angle (its front edge is on the verge)
    # landing deck: its front edge runs along 1641's back wall (no gap to fall into), 4.6 m up (under the eaves, so a car that
    # slides off the back of the roof comes down on it), then down to the ground 54 m from the house's middle
    q, wdir = wall_line(J, o, u, exit=True)
    n = left(u); HALFD = 4.2
    s_far = 54.0
    # the right-hand edge is pulled in where house 1622's corner is close (it stays 0.8 m clear of all of 1622)
    R22 = rect(1622)
    need = 0.0
    fl = intersect(o + n * HALFD, u, q, wdir)
    s_front_l = float((fl - o) @ u)
    def right_edge_ok(r0, s_a, s_b):
        for cn in R22["corners"]:
            sc_, la = float((cn - o) @ u), float((cn - o) @ n)
            if s_a - 3 <= sc_ <= s_b + 3:
                edge = -(r0 + (HALFD - r0) * np.clip((sc_ - s_a) / (s_b - s_a), 0, 1))
                if la > edge - 0.8 and la < 0: return False
        return True
    r0 = HALFD
    while r0 > 1.0 and not right_edge_ok(r0, 8.0, 34.0): r0 -= 0.1
    fr = intersect(o - n * r0, u, q, wdir)
    s_front_r = float((fr - o) @ u)
    mid_r = o + u * 34.0 - n * HALFD
    deck_poly = [fr, mid_r, o + u * s_far - n * HALFD, o + u * s_far + n * HALFD, fl]
    yTop = ground(*q) + 4.6
    yEnd = ground(*(o + u * s_far)) - 0.05
    sf = min(s_front_l, s_front_r)
    prisms.append(prism("sc6 landing deck behind house 1641", "landing", deck_poly, o, u, [(sf, yTop), (max(s_front_l, s_front_r) + 2.0, yTop - 0.4), (s_far, yEnd)], DECK))
    note(f"#6 landing deck: front along 1641's back wall (stations {s_front_l:.1f} / {s_front_r:.1f}), right edge {r0:.1f} m from the line at the front (house 1622's corner), "
         f"{yTop - ground(*q):.1f} m up, to the ground at {s_far:.0f} m; slope {math.degrees(math.atan((yTop - 0.4 - yEnd) / (s_far - max(s_front_l, s_front_r) - 2))):.1f} deg")
    land_end = o + u * s_far
    # the line bends right off the deck's far half toward the gap between 713 and 1052, then sweeps left round 713's
    # front yard on to the lane in one arc
    hG = unit(GAP - (o + u * 44.0))
    arc, s_end, R, ang, err = arc_onto_lap(GAP + hG * 3.0, hG, 985, 1030, +1)
    note(f"#6 back on to the lane: an arc of radius {R:.0f} m turning {ang:.0f} deg left, joining the lane {s_end:.0f} m after the gate (fit {err:.2f} rad)")
    pts = polyline([lap(729), lap(736), K0 - u * 8.0, K0], 2.0) + polyline([K0, o + u * lip_t], 1.0)[1:] + polyline([o + u * lip_t, o + u * 44.0], 2.0)[1:]         + polyline([o + u * 44.0, GAP, GAP + hG * 3.0], 2.0)[1:] + polyline(arc + [lap(s_end + 8)], 2.0)[1:]
    REJOIN_6 = s_end + 8
    for cn_i in (1622, 713, 1052):
        cs_ = [(float((cn - o) @ u), float((cn - o) @ n)) for cn in rect(cn_i)["corners"]]
        note(f"#6 house {cn_i} corners (station along the line, metres to the left): " + ", ".join(f"({a:.1f}, {b:.1f})" for a, b in cs_))
    pts = smooth(pts)
    entry_s = station(pts, o + u * (lip_t - 3.0))   # on the kicker, near its lip: well clear of the lane (a plane near the
                                                     # kicker's foot reached back across the lane and fired from the road)
    lapd = min(float(np.linalg.norm(K0 - lap(s_))) for s_ in np.arange(700, 780, 0.5))
    note(f"#6 kicker foot {lapd:.1f} m from the lane's centreline (the lane is 8 m wide)")
    trail = [[0.0, station(pts, K0)], [station(pts, land_end) + 1.0, length(pts)]]
    lipP = o + u * lip_t
    return {
        "id": "sc6_over_house", "name": "Rooftop leap", "forgives": [5], "entry_half_width": 3.5, "entry_margin": 1.0,
        "path": pts, "half_width": 4.0, "trail": trail, "clear_half_width": 6.5,
        "entry_s": entry_s, "prisms": prisms, "open_houses": [], "jump_houses": [{"building": 1641}],
        "jumps": [{"label": "kicker over house 1641", "x": float(lipP[0]), "z": float(lipP[1])}],
        "leave_lap_s": 729, "rejoin_lap_s": float(REJOIN_6),
    }

shortcuts = [build_sc2(), build_sc6()]

# ---------------------------------------------------------------- checks against the barriers and the checkpoints
for sc in shortcuts:
    P = sc["path"]
    for c in route.get("closures", []):
        b = V(c["x"], c["z"]); d = min(float(np.linalg.norm(p - b)) for p in P)
        # the race leaves a barrier out only if the line passes within 3 m of it (RaceBarriers.ShortcutClearM)
        if d < c["width_m"] / 2 + 3 + 3: note(f"barrier {c['id']} ({c['label']}) is in the way of {sc['id']} ({d:.1f} m from the line): the race leaves it out")
        elif d < 20: note(f"{sc['id']}: barrier {c['id']} ({c['label']}) stands {d:.1f} m from the line, clear of it, and stays")
    note(f"{sc['id']}: {length(P):.0f} m of line, against {sc['rejoin_lap_s'] - sc['leave_lap_s']} m of lap between the same points; entry plane {sc['entry_s']:.0f} m in")

# ---------------------------------------------------------------- write
def rnd(p): return [round(float(p[0]), 2), round(float(p[1]), 2)]
out = {"source": "Tools/mapgen/make_shortcuts.py (hand-made design; re-run after mapgen). Heights are absolute world y.", "route": "chastain_glen_loop", "notes": notes, "shortcuts": []}
for sc in shortcuts:
    o = dict(sc); o["path"] = [v for p in sc["path"] for v in rnd(p)]
    o["trail"] = [round(v, 2) for a, b in sc["trail"] for v in (a, b)]; o["entry_s"] = round(sc["entry_s"], 2)
    out["shortcuts"].append(o)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)

# official_shortcuts in the route file: keep any other hand-made entries, replace these two
keep = [s for s in route.get("official_shortcuts", []) if s.get("id") not in {sc["id"] for sc in shortcuts}]
for sc in shortcuts:
    keep.append({"id": sc["id"], "name": sc["name"], "direction": "forward", "path": [v for p in sc["path"] for v in rnd(p)],
                 "half_width_m": sc.get("entry_half_width", 5.0), "entry_inset_m": round(sc["entry_s"], 2), "entry_margin_m": sc.get("entry_margin", 2.0), "forgives": sc["forgives"]})
route["official_shortcuts"] = keep
json.dump(route, open(ROUTE, "w", encoding="utf-8"), indent=1)
print(f"wrote {OUT} and the official_shortcuts of {ROUTE}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    area = json.load(open(os.path.join(ROOT, "Data/neighborhood/chastain_glen_area.json")))
    fig, axs = plt.subplots(1, 2, figsize=(18, 11))
    for ax, sc, box in zip(axs, shortcuts, [(2985, 3110, 990, 1235), (2735, 2850, 975, 1135)]):
        x0, x1, z0, z1 = box
        xs = np.arange(x0, x1, 1.0); zs = np.arange(z0, z1, 1.0)
        G = np.array([[ground(x, z) for x in xs] for z in zs])
        cs = ax.contour(xs, zs, G, levels=np.arange(int(G.min()), int(G.max()) + 1, 1.0), linewidths=0.4, colors="gray"); ax.clabel(cs, fontsize=6, fmt="%d")
        for i in area["houses"]:
            f = footprint(i); c = f.mean(0)
            if x0 - 20 < c[0] < x1 + 20 and z0 - 20 < c[1] < z1 + 20:
                hl = i in [h["building"] for h in sc["open_houses"]] or i in [h["building"] for h in sc["jump_houses"]]
                ax.fill(f[:, 0], f[:, 1], color="#c84" if hl else "#a76", alpha=0.85); ax.text(c[0], c[1], str(i), fontsize=7, ha="center")
        ax.plot(loop[:, 0], loop[:, 1], "r-", lw=6, alpha=0.3)
        for pr in sc["prisms"]:
            P = np.array(pr["poly"]).reshape(-1, 2); P = np.vstack([P, P[:1]]); ax.plot(P[:, 0], P[:, 1], "-", color="#06c", lw=1)
            ax.text(P[:, 0].mean(), P[:, 1].mean(), pr["kind"], fontsize=7, color="#036")
        P = np.array(sc["path"]); ax.plot(P[:, 0], P[:, 1], "k.-", ms=2, lw=0.8)
        for c in route.get("closures", []): ax.plot(c["x"], c["z"], "rs")
        for cp in route["checkpoints"]: ax.plot(*cp["pos"], "g^"); ax.text(*cp["pos"], f"CP{cp['id']}", color="g")
        ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.set_title(f"{sc['id']} ({sc['name']})"); ax.grid(alpha=0.3)
    os.makedirs(os.path.join(ROOT, "Data/screenshots/round8"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "Data/screenshots/round8/shortcut_layout.png"), dpi=100, bbox_inches="tight")
    print("plot: Data/screenshots/round8/shortcut_layout.png")
