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
AREA = json.load(open(os.path.join(ROOT, "Data/neighborhood/chastain_glen_area.json")))
AREA_SEGS = []
for _ri in AREA["roads"]:
    _r = m["roads"][_ri]; _P = np.array(_r["points"], float).reshape(-1, 2)
    for _a, _b in zip(_P[:-1], _P[1:]): AREA_SEGS.append((_a, _b, _r["width"] / 2))
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

def prism(name, kind, poly, origin, axis, profile, colour, bank=None, held=False, cut=False):
    """held: the car is held on its top (Round 10: a rounded top it would otherwise fly off at speed; ShortcutSurface).
    cut: the terrain under it is lowered to its top (Round 10: the landing slope behind 1763 runs through the crest)."""
    poly = clip_off_lane(poly)
    # edges along the lane cut get a sloped skirt down to the ground (MapBuilder), not a vertical face a car could hit
    return {"held": held, "cut": cut, "skirt_edges": cut_edges(poly),"bank": [round(float(v), 4) for sb in (bank or []) for v in sb],"name": name, "kind": kind, "poly": [round(float(v), 3) for p in poly for v in p], "origin": [round(float(origin[0]), 3), round(float(origin[1]), 3)],
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

STAIRS = [0.56, 0.47, 0.37]
WOOD = [0.47, 0.33, 0.20]; RAMP = [0.55, 0.40, 0.24]; DECK = [0.50, 0.36, 0.22]; FLOOR = [0.50, 0.35, 0.21]; DIRT = [0.38, 0.255, 0.135]

notes = []
def note(s): notes.append(s); print(s)

# =============================================================================================================== #2
# Round 10 (Dan: "it simply has to restore the houses", then "a smaller ramp where it crashes through windows on the
# upstairs level of the first house, lands in the yard ...", then for the second house "drive through a sliding glass door,
# immediately up stairs which is used as a ramp as you jump through another set of windows"). Both houses are whole; only
# the wall panels the car goes through break (MapBuilder.Shortcuts.cs). "Two-house drop" now:
#   * leaves Chastain Glen Lane about 200 m after the gate and turns about 46 degrees left on to a ramp in house 1763's
#     front yard. The lane runs along 1763's front 21 m out, so a ramp can start no further than 15 m from its front wall
#     (5 m from the lane's centre) and has to climb 5.8 m to the upstairs floor: a short, steep wooden ramp (curved foot,
#     straight, a rounded top that the car is held on, "held"), which goes in through the upstairs front windows and levels
#     out on the upstairs floor inside;
#   * the car crosses the upstairs room and leaves through the upstairs windows of 1763's north side wall (the line cannot
#     reach the back wall: going through the front at an angle a car can be turned on to from the lane, the side wall
#     comes first; a squarer line would need a 75 degree turn off the lane on to a still shorter ramp);
#   * it comes off the upstairs floor level and lands on a straight slope of about 14 degrees that runs down the yard to
#     house 1627 (a level flight off a level edge meets a 14 degree slope at 12.6 degrees whatever the speed, landing about
#     v^2 / 20 m out: 20 m at 20 m/s, 40 m at 28 m/s); the ground's crest behind 1763 is cut down to the slope;
#   * the slope bends about 10 degrees left 18 m before 1627, flattens and ends at the sliding glass door in 1627's back wall; inside,
#     a staircase (a smooth ramp under the look of steps, held over its top) climbs to the upstairs and runs out through
#     1627's upstairs front windows;
#   * from there, as before: over 1627's front yard, through the gap between 1615 and 1459, down the second slope and back
#     on to the lane about 544 m after the gate. Skips CP2.
def nearest_road(p):
    best = None
    for a, b, h in AREA_SEGS:
        ab = b - a; t = float(np.clip((p - a) @ ab / (ab @ ab), 0, 1)); c = a + ab * t; d = float(np.linalg.norm(c - p))
        if best is None or d < best[0]: best = (d, c, h)
    return best

def house_frame(i):
    """The styled house's frame as MapBuilder.Neighborhood.PlanHouses makes it: centre, front F, the u axis (R * mirror),
    width W along u, depth D along F, the floor level and the garage's u range (style A: the -u side)."""
    r = rect(i); c = r["c"]; U, Vv = r["d"], r["p"]
    _, q, _ = nearest_road(c); to_road = unit(q - c)
    F = max([U, -U, Vv, -Vv], key=lambda cd: float(cd @ to_road))
    along = abs(float(F @ U)) < 0.7
    W = r["hl"] * 2 if along else r["hw"] * 2; D = r["hw"] * 2 if along else r["hl"] * 2
    Rv = V(-F[1], F[0]); M = -1.0 if float((q - c) @ Rv) > 0 else 1.0
    ua = Rv * M
    gW = min(max(W * 0.47, 5.7), 6.7); gU0 = -W / 2; gU1 = gU0 + gW
    g = c + ua * (gU0 + gU1) / 2 + F * (D / 2 + 0.4)
    return {"c": c, "F": F, "ua": ua, "W": W, "D": D, "floor": ground(*g) + 0.16, "gU1": gU1}

def hpt(h, u, v): return h["c"] + h["ua"] * u + h["F"] * v

def arc_from_lap(P1, h1, s_min, s_max, right=0.0):
    """A circular arc that leaves the lap tangent to it ('right' metres right of its centreline) and ends at P1 heading h1
    (the mirror of arc_onto_lap)."""
    best = None
    for s0 in np.arange(s_min, s_max, 0.25):
        t = unit(lap(s0 + 0.5) - lap(s0)); q = lap(s0) - left(t) * right
        ang = math.atan2(t[0] * h1[1] - t[1] * h1[0], t @ h1)
        chord = P1 - q; L = float(np.linalg.norm(chord))
        if L < 2 or abs(ang) < 0.05: continue
        cang = math.atan2(t[0] * chord[1] - t[1] * chord[0], t @ chord)
        err = abs(cang - ang / 2)
        if best is None or err < best[0]: best = (err, s0, ang, L / (2 * math.sin(abs(ang) / 2)), q, t)
    err, s0, ang, R, q, t = best
    c = q + left(t) * R * np.sign(ang); a0 = math.atan2(*(q - c)[::-1])
    return [c + R * np.array([math.cos(a0 + ang * k / 24), math.sin(a0 + ang * k / 24)]) for k in range(25)], s0, R, math.degrees(ang), err

def two_arcs(H, L, R1, R2):
    """A profile that rises H over L: a concave arc of radius R1, a straight at angle th and a convex arc of radius R2 back
    to level. Returns th and a function y(s) for s in 0..L (y(0) = 0, level at both ends)."""
    lo, hi = 0.01, 1.2
    def rise(th):
        a = (R1 + R2) * math.sin(th)
        if a > L: return None
        return (R1 + R2) * (1 - math.cos(th)) + (L - a) * math.tan(th)
    for _ in range(60):
        mid = (lo + hi) / 2; r = rise(mid)
        if r is None or r > H: hi = mid
        else: lo = mid
    th = lo; s1 = R1 * math.sin(th); s3 = L - R2 * math.sin(th)
    y1 = R1 * (1 - math.cos(th))
    def y(s):
        if s <= 0: return 0.0
        if s <= s1: return R1 - math.sqrt(max(R1 * R1 - s * s, 0.0))
        if s <= s3: return y1 + (s - s1) * math.tan(th)
        if s <= L: return H - R2 + math.sqrt(max(R2 * R2 - (L - s) ** 2, 0.0))
        return H
    return th, y

def nearest_corner(pts, hn):
    best = 99.0
    for p, q in zip(pts[:-1], pts[1:]):
        d = q - p; L = float(np.linalg.norm(d))
        if L < 1e-6: continue
        d = d / L
        for cn in rect(hn)["corners"]:
            t = float((cn - p) @ d)
            if 0 <= t <= L: best = min(best, abs(float((cn - p) @ left(d))))
    return best

def build_sc2():
    A = house_frame(1763); B = house_frame(1627)
    fA, fB = A["floor"], B["floor"]
    upA, upB = fA + 2.85, fB + 2.85                   # the upstairs floors (MapBuilder.StoreyH)
    M = V(3036.6, 1145.9); G2 = V(3019.2, 1200.0); uL = unit(G2 - M)   # the gap between 1615 and 1459 (Round 8)
    # -- 1627: out of its upstairs front (v = D/2 - 1.3, the entry part of a style A front) 2 m along from its middle, aimed at the gap
    vFrontB = B["D"] / 2 - 1.3
    # aimed 8 m into the gap past its narrowest point M (0.2 m east of its axis), where it widens (1459's wall falls away): the car flies straight
    # out of 1627 at it and turns a little right in the wide part. That keeps the bend on the slope before 1627 small
    # (about 5 degrees), which a fast car that lands late on the slope must still make to hit 1627's door (aiming at the
    # gap's axis 10 m before M needed 13 degrees there, and cars landing past the bend missed the door)
    T = M + uL * 8.0 - left(uL) * 0.2
    Pf = hpt(B, 2.0, vFrontB); h2 = unit(T - Pf)
    dvB, duB = float(h2 @ B["F"]), float(h2 @ B["ua"])
    tB = (vFrontB + B["D"] / 2) / dvB
    Pb = Pf - h2 * tB                                  # the sliding door in its back wall
    ub = 2.0 - duB * tB
    # -- 1763: in through its upstairs front (v = D/2 - 1.3) 3.5 m along from its middle; the line runs to a bend 18 m before 1627's door
    vFrontA = A["D"] / 2 - 1.3
    E = hpt(A, 3.5, vFrontA); BEND = 18.0
    Bp = Pb - h2 * BEND; h1 = unit(Bp - E)
    dvA, duA = float(h1 @ A["F"]), float(h1 @ A["ua"])
    tX = (A["W"] / 2 - 3.5) / duA                      # where the line leaves through the north side wall (u = +W/2)
    X = E + h1 * tX; vX = vFrontA + dvA * tX
    note(f"#2 house 1763 (floor {fA:.2f}, upstairs {upA:.2f}): in through the upstairs front {math.degrees(math.acos(min(1.0, -dvA))):.0f} deg off square, out through the north side wall {vX:+.1f} m from its middle, {tX:.1f} m inside; "
         f"house 1627 (floor {fB:.2f}, upstairs {upB:.2f}): in through the back wall at u {ub:+.1f}, out through the upstairs front at u +2.0, {tB:.1f} m inside; bend {math.degrees(math.acos(min(1.0, float(h1 @ h2)))):.1f} deg left {BEND:.0f} m before 1627's door")
    # -- the ramp's foot: as far back along the line as the lane allows (5 m from its centre, plus a little)
    s_foot = 0.0
    while lap_dist(E - h1 * (s_foot + 0.25))[0] >= LANE_CLEAR + 0.6: s_foot += 0.25
    F0 = E - h1 * s_foot
    gF = ground(*F0)
    CREST_IN = 3.5                                     # the ramp's rounded top ends level this far inside the front wall
    L_r = s_foot + CREST_IN
    R1A, R2A = 14.0, 6.0
    th, ry = two_arcs(upA - (gF - 0.03), L_r, R1A, R2A)
    def ramp_y(s): return gF - 0.03 + ry(s + s_foot)  # s from the front wall (negative outside)
    worst = min(ramp_y(s) - ground(*(E + h1 * s)) for s in np.arange(-s_foot, 0, 0.25))
    note(f"#2 ramp up into house 1763: {L_r:.1f} m from its foot ({s_foot:.1f} m in front of the wall, ground {gF:.2f}) to level on the upstairs floor {CREST_IN} m inside; "
         f"rises {upA - gF:.2f} m: curved foot (radius {R1A:.0f} m), straight at {math.degrees(th):.0f} deg, rounded top (radius {R2A:.0f} m, the car is held on it); "
         f"{ramp_y(0) - upA:+.2f} m against the upstairs floor at the wall; lowest {worst:+.2f} m against the ground under it")
    HW, HWB = 2.6, 3.0      # the passage's half width through 1763 and (wider: a car that lands late on the slope arrives off line) 1627
    prisms = []
    prisms.append(prism("sc2 ramp up into house 1763", "ramp", strip_poly(E, h1, -s_foot, CREST_IN, HW), E, h1,
                        [(s, ramp_y(s)) for s in np.linspace(-s_foot, CREST_IN, 48)], RAMP, held=True, cut=True))
    prisms.append(prism("sc2 upstairs floor in house 1763", "floor", strip_poly(E, h1, CREST_IN - 0.2, tX + 0.05, HW), E, h1, [(CREST_IN - 0.2, upA), (tX + 0.05, upA)], FLOOR, held=True))
    # -- the landing slope: from just under the side wall's sill, straight down, flaring level into 1627's door; its angle
    # (about 14 degrees) is set so it arrives at 1627's floor exactly
    FLARE_R = 31.0
    y_top = upA - 0.25
    L1 = float(np.linalg.norm(Bp - X)); L_tot = L1 + BEND
    drop_needed = y_top - fB
    lo_, hi_ = 0.05, 0.4
    for _ in range(50):
        a = (lo_ + hi_) / 2
        d_ = (L_tot - FLARE_R * math.sin(a)) * math.tan(a) + FLARE_R * (1 - math.cos(a))
        if d_ > drop_needed: hi_ = a
        else: lo_ = a
    SLOPE = lo_; flare = FLARE_R * math.sin(SLOPE)
    def land_y(d):                                     # d: distance along the slope from the side wall
        if d <= L_tot - flare: return y_top - d * math.tan(SLOPE)
        x = L_tot - d
        return fB + FLARE_R - math.sqrt(max(FLARE_R ** 2 - x * x, 0.0))
    note(f"#2 landing slope: {L_tot:.0f} m from 1763's side wall to 1627's door, {math.degrees(SLOPE):.1f} deg, starting {y_top - upA:+.2f} m against 1763's upstairs floor, "
         f"level over its last {flare:.1f} m into 1627's door ({fB:.2f}); a level flight off the upstairs edge lands about v*v*{2 * math.tan(SLOPE) / 9.81:.3f} m out")
    dX = 0.05
    cut_max = max(ground(*(X + h1 * d)) - land_y(d) for d in np.arange(0, L1, 0.5))
    note(f"#2 the ground's crest behind 1763 stands up to {cut_max:.1f} m above the slope: cut down to it (the slope's prisms are marked 'cut')")
    prisms.append(prism("sc2 landing slope behind house 1763", "landing", strip_poly(X, h1, dX, L1 + 1.0, 3.2), X, h1,
                        [(d, land_y(d)) for d in np.linspace(dX, L1 + 1.0, 60)], DECK, cut=True))
    prisms.append(prism("sc2 landing slope to house 1627", "landing", strip_poly(Bp, h2, -1.0, BEND + 0.3, 3.4), Bp, h2,
                        [(d - L1, land_y(d)) for d in np.linspace(L1 - 1.0, L_tot + 0.3, 30)], DECK, cut=True))
    # -- 1627: level from the door to the foot of the stairs, then the stairs (a held ramp) up to the upstairs, level for the last metre to the front wall
    STAIR_FOOT, STAIR_TOP = 1.2, tB - 1.0
    th2, sy = two_arcs(upB - fB, STAIR_TOP - STAIR_FOOT, 10.0, 8.0)
    def stair_y(t): return fB + sy(t - STAIR_FOOT) if t < STAIR_TOP else upB
    note(f"#2 stairs in house 1627: from {STAIR_FOOT} m inside the door to {STAIR_TOP:.1f} m (1 m before the front wall), rising {upB - fB:.2f} m, "
         f"straight part {math.degrees(th2):.0f} deg (curved foot radius 10 m, rounded top radius 8 m, held); level out through the upstairs front windows")
    prisms.append(prism("sc2 ground floor in house 1627", "floor", strip_poly(Pb, h2, -0.3, STAIR_FOOT + 0.1, HWB), Pb, h2, [(-0.3, fB), (STAIR_FOOT + 0.1, fB)], FLOOR))
    prisms.append(prism("sc2 stairs in house 1627", "stairs", strip_poly(Pb, h2, STAIR_FOOT, tB + 0.05, HWB), Pb, h2,
                        [(t, stair_y(t)) for t in np.linspace(STAIR_FOOT, tB + 0.05, 40)], STAIRS, held=True))
    # -- the line: off the lane on an arc on to the ramp's line, through both houses, out of 1627 and on as before
    # the turn off the lane starts from its right-hand side (3 m right of the centre), as a driver swings wide for a left
    # turn: from the centre the arc was 23 m in radius and the car ran wide at racing speed
    arc_in, s_leave, R_in, ang_in, err_in = arc_from_lap(F0, h1, 150, 215, right=3.0)
    note(f"#2 off the lane: an arc of radius {R_in:.0f} m turning {ang_in:.0f} deg left from 3 m right of the lane's centre, leaving it {s_leave:.0f} m after the gate (fit {err_in:.2f} rad), on to the ramp's foot")
    arc, s_end, R, ang, err = arc_onto_lap(G2 + uL * 6.0, uL, 520, 560, +1)
    note(f"#2 back on to the lane: an arc of radius {R:.0f} m turning {ang:.0f} deg left, joining the lane {s_end:.0f} m after the gate (fit {err:.2f} rad)")
    land2 = Pf + h2 * 20.0
    t_leave = unit(lap(s_leave + 0.5) - lap(s_leave))
    # from the lane's centre 30 m before the arc, easing over to its right-hand side
    run_in_pts = polyline([lap(s_leave - 30), lap(s_leave - 22), lap(s_leave - 12) - left(t_leave) * 2.4] + arc_in, 2.0)
    through = polyline([F0, E, X, Bp, Pb, Pf], 1.0)
    run_out = polyline([Pf, land2, T, G2, G2 + uL * 6.0], 2.0) + polyline(arc + [lap(s_end + 8)], 2.0)[1:]
    # the straight runs through the houses stay exact (no corner cutting there): only the run-in and the run-out are smoothed
    dense = smooth(run_in_pts)[:-1] + through + smooth(run_out)[1:]
    for hn in (1539, 1615, 1459, 1602, 1919, 1993, 1961):
        note(f"#2 house {hn}: nearest corner {nearest_corner(dense, hn):.1f} m from the line")
    entry_s = station(dense, E - h1 * 4.0)              # the entry plane: on the ramp, well clear of the lane
    trail = [[0.0, station(dense, F0)], [station(dense, Pf + h2 * 6.0), length(dense)]]
    # the houses the line goes through: the two wall crossings (a point on the wall and its outward normal), the passage's
    # half width and, for each wall, the height range of the panel that breaks (MapBuilder.Shortcuts.cs)
    def wall(P, N): return [round(float(P[0]), 3), round(float(P[1]), 3), round(float(N[0]), 5), round(float(N[1]), 5)]
    houses = [
        {"building": 1763, "a": list(map(float, E)), "b": list(map(float, X)), "half_width": HW, "wall_a": wall(E, A["F"]), "wall_b": wall(X, A["ua"]),
         "y0_a": round(ramp_y(0), 3), "y1_a": round(upA + 2.35, 3), "y0_b": round(upA - 0.1, 3), "y1_b": round(upA + 2.35, 3), "floor_y": round(fA, 3), "no_entry_gable": True},
        {"building": 1627, "a": list(map(float, Pb)), "b": list(map(float, Pf)), "half_width": HWB, "wall_a": wall(Pb, -B["F"]), "wall_b": wall(Pf, B["F"]),
         "y0_a": round(fB - 0.1, 3), "y1_a": round(fB + 2.45, 3), "y0_b": round(upB - 0.1, 3), "y1_b": round(upB + 2.35, 3), "floor_y": round(fB, 3), "sliding_door_a": True},
    ]
    return {
        "id": "sc2_two_houses", "name": "Two-house drop", "forgives": [2],
        "path": dense, "half_width": 4.0, "trail": trail, "clear_half_width": 6.5,
        "entry_s": entry_s, "prisms": prisms, "open_houses": houses, "jump_houses": [],
        "jumps": [{"label": "upstairs of house 1763 (out through its side wall)", "x": float(X[0]), "z": float(X[1])}, {"label": "upstairs of house 1627 (out through its front)", "x": float(Pf[0]), "z": float(Pf[1])}],
        "leave_lap_s": round(float(s_leave - 30), 1), "rejoin_lap_s": float(s_end + 8),
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
