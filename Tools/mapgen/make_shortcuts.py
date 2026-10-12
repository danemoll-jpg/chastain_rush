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

def rect_dist(p, hn):
    """Distance from p to house hn's rectangle (-1 inside)."""
    r = rect(hn); d = p - r["c"]
    a = abs(float(d @ r["d"])) - r["hl"]; b = abs(float(d @ r["p"])) - r["hw"]
    return -1.0 if a < 0 and b < 0 else math.hypot(max(a, 0.0), max(b, 0.0))

def corridor_poly(pts, half):
    """A band 'half' either side of a line (left side forward, right side back): a landing zone's outline."""
    P = [np.array(p, float) for p in pts]; P = [p for i, p in enumerate(P) if i == 0 or np.linalg.norm(p - P[i - 1]) > 1e-3]; L, R = [], []
    for i, p in enumerate(P):
        d = unit(P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]); L.append(p + left(d) * half); R.append(p - left(d) * half)
    return L + R[::-1]

# =============================================================================================================== #2, Round 12
# Dan (2026-10-10), after rejecting three versions through house 1763: "I think just restore that house completely. Remove the
# ugly ramp and have the trail actually start right after that house." Nothing is attached to, cut into or aimed at 1763.
#   * the line leaves Chastain Glen Lane on a left turn round 1763's north-east corner into the side yard between 1763 and
#     1539 (5.5 m between their corners at the lane end), runs straight past 1539's west corner, then bends right (after the
#     corner, where 1539's wall falls away) on to one straight through house 1627. The bend cannot come sooner: seen along
#     that straight, 1539's west corner and 1763's north-east corner line up;
#   * the ground is grass all the way: natural up to the crest by 1539's corner, then the terrain graded to a profile
#     ("grade", MapBuilder: cut and fill): a landing hill (the path of a car leaving the crest at the design speed), a
#     hollow and a 12 degree climb. From the lane's edge to the door the grass is "held" (RampHold, as the held ramps of
#     Round 10): the car stays on it at any speed, so it can steer through the S;
#   * 1627 (Dan: "slightly elevate it so that you are already on an incline when you bust through the sliding glass doors
#     and once you hit the stairs and fly out the other window you should have pretty good momentum"): the climb runs on
#     through a wide sliding glass door in its back wall and up its stairs without a kink; the stairs curve up into a
#     kicker that ends at the upstairs front wall, out through a large upstairs window (only glass and frames break). The
#     window has to clear the garage roof, so the straight points a few degrees east of the gap between 1615 and 1459 (the
#     gap admits about half a degree from 1627's front): the kicker is sized so the car comes down in 1627's front yard at
#     racing speed and the line bends back into the gap on the ground;
#   * from there, as before: through the gap between 1615 and 1459, down the second slope and back on to the lane about 544 m
#     after the gate. Skips CP2, as before. A sign at the turn-off is the clue (Round 12, item 3).
SC2_LIP_DEG = float(os.environ.get("SC2_LIP_DEG", "18"))   # the kicker's last angle (Round 12, item 4: sized for the flight)
SC2_LIP_UP = 0.0                 # the lip against the upstairs floor (higher needs a steeper kicker: the car roof must clear the window top)
SC2_INCLINE_DEG = 12.0           # the climb into the sliding door and up the stairs
SC2_CORNER_CLEAR = 2.2           # the line's centre off house 1539's west corner (on 1763's side)
SC2_EXIT_U = 2.4                 # 1627's upstairs window: along its front from its middle (clear of the garage roof at -0.1)
DOOR_HALF = 2.3                  # 1627's sliding door and upstairs window: half their width of glass along the wall

def build_sc2():
    B = house_frame(1627)
    fB = B["floor"]; upB = fB + 2.85                   # the upstairs floor (MapBuilder.StoreyH)
    M = V(3036.6, 1145.9); G2 = V(3019.2, 1200.0); uL = unit(G2 - M)   # the gap between 1615 and 1459 (Round 8)
    T = M + uL * 8.0 - left(uL) * 0.2                  # aimed 8 m into the gap past its narrowest point, as Round 10
    vFrontB = B["D"] / 2 - 1.3                         # the upstairs front of a style A (the entry part's plane)
    ne63, s39, w39 = V(3078.5, 1053.2), V(3081.1, 1058.1), V(3074.3, 1064.3)   # 1763's north-east corner, 1539's south and west corners
    # -- the line in plan
    Pf = hpt(B, SC2_EXIT_U, vFrontB); u_exit = SC2_EXIT_U
    if u_exit - DOOR_HALF - 0.12 < -0.4 or u_exit + DOOR_HALF + 0.12 > B["W"] / 2 - 0.1:
        raise SystemExit(f"#2: 1627's upstairs window ({u_exit:.2f} +- {DOOR_HALF}) does not fit between its garage roof and its corner")
    Mo = (ne63 + s39) / 2 - unit(s39 - ne63) * 0.6     # the side yard's mouth, 0.6 m toward 1763 from its middle (2.15 m off each corner)
    hM = unit(w39 - Mo)
    for _ in range(8):
        W1 = w39 + left(hM) * SC2_CORNER_CLEAR; hM = unit(W1 - Mo)
    W1 = w39 + left(hM) * SC2_CORNER_CLEAR             # abeam 1539's west corner
    # the straight: from 1627's upstairs window at SC2_EXIT_U, aimed at the gap; behind 1627 it would pass through 1539's
    # corner, so the line reaches it from W1 by an S (right, then left, radius R_S) on the hill
    h2 = unit(T - Pf); dvB = float(h2 @ B["F"]); off_sq = math.degrees(math.acos(min(1.0, dvB)))
    Pb = Pf - h2 * ((vFrontB + B["D"] / 2) / dvB)      # the sliding door in its back wall
    tB = float(np.linalg.norm(Pf - Pb)); aim = 0.0
    def rot(v, a): return np.array([v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a)])
    R_S = 25.0; dh = math.atan2(hM[0] * h2[1] - hM[1] * h2[0], hM @ h2)
    best = None
    for pa in np.linspace(0.0, 1.2, 2401):
        pb = pa + dh
        if pb < 0: continue
        q = W1.copy(); hq = hM.copy(); pts = [q.copy()]
        for ang, sg in ((pa, -1), (pb, +1)):
            n = max(2, int(ang * R_S / 0.5))
            for k in range(n): hq = rot(hq, sg * ang / n); q = q + hq * (ang * R_S / n); pts.append(q.copy())
        off = float((q - Pf) @ left(h2))
        if best is None or abs(off) < abs(best[0]): best = (off, pa, pb, pts)
    _, pa, pb, fil = best
    T1, T2 = W1, fil[-1]
    RIGHT = 2.0                                        # the turn starts from the lane's right-hand side, as a driver swings wide
    arc_in, s_leave, R_in, ang_in, err_in = arc_from_lap(Mo - hM * 3.0, hM, 195, 250, right=RIGHT)
    t_leave = unit(lap(s_leave + 0.5) - lap(s_leave))
    run_in_pts = polyline([lap(s_leave - 30), lap(s_leave - 22), lap(s_leave - 12) - left(t_leave) * RIGHT * 0.8, lap(s_leave) - left(t_leave) * RIGHT], 2.0)
    down = smooth(run_in_pts)[:-1] + polyline(arc_in, 1.0) + polyline([arc_in[-1], T1], 1.0)[1:] + fil[1:] + polyline([fil[-1], Pb], 1.0)[1:]
    S = np.concatenate([[0.0], np.cumsum([float(np.linalg.norm(b - a)) for a, b in zip(down[:-1], down[1:])])])
    s_door = float(S[-1])
    cl = min(min(rect_dist(p, 1763), rect_dist(p, 1539)) for p in down)
    note(f"#2 off the lane: an arc of radius {R_in:.0f} m turning {ang_in:.0f} deg left from {RIGHT:.0f} m right of the lane's centre, leaving it {s_leave:.0f} m after the gate "
         f"(fit {err_in:.2f} rad), into the side yard between 1763 and 1539, straight past 1539's west corner ({SC2_CORNER_CLEAR} m off it), an S (right {math.degrees(pa):.0f} deg, "
         f"left {math.degrees(pb):.0f} deg, radius {R_S:.0f} m) on to one straight ({float(np.linalg.norm(Pb - T2)):.0f} m to 1627's door) through 1627, aimed at the gap between 1615 and 1459; "
         f"the line's centre stays {cl:.1f} m or more from 1763 and 1539")
    # -- the ground along the line (grass: the terrain graded)
    def at(s): i = min(int(np.searchsorted(S, s)), len(down) - 1); return down[i]
    s_e = next(float(S[i]) for i, p in enumerate(down) if S[i] > 20 and lap_dist(p)[0] > 4.8)   # where the line leaves the street
    s_c = station(down, W1)                            # the crest, abeam 1539's corner
    y_e, y_c = ground(*at(s_e)), ground(*at(s_c))
    beta = math.atan((y_c - ground(*at(s_c - 3.0))) / 3.0)   # the natural ground's slope coming up to the crest (it is not graded before it)
    ALPHA_MAX, Rs, Li = math.radians(30.0), 18.0, 2.5; th = math.radians(SC2_INCLINE_DEG); DX = 0.05
    def run_profile(vd):
        xs = [s_c]; ys = [y_c]; ang = beta; phase = 0; x = s_c
        while x < s_door - 1e-6:
            if phase < 2 and (s_door - Li) - x <= Rs * (math.sin(th) - math.sin(ang)): phase = 2
            if phase == 0:
                ang = math.atan(math.tan(beta) - 9.81 * (x - s_c + DX) / (vd * vd * math.cos(beta) ** 2))
                if ang <= -ALPHA_MAX: ang = -ALPHA_MAX; phase = 1
            elif phase == 2:
                ang += DX / (Rs * math.cos(ang))
                if ang >= th: ang = th; phase = 3
            x += DX; xs.append(x); ys.append(ys[-1] + math.tan(ang) * DX)
        return xs, ys
    lo_, hi_ = 6.0, 40.0                               # the design speed that brings the climb to the door's sill
    for _ in range(40):
        v_ = (lo_ + hi_) / 2; _, ys_ = run_profile(v_)
        if ys_[-1] > fB: hi_ = v_
        else: lo_ = v_
    VD = lo_; px, py = run_profile(VD)
    steepest = -float(np.degrees(np.arctan(np.diff(py) / DX)).min())
    def grade_y(s): return float(np.interp(s, px, py))
    gs = np.arange(s_c, s_door + 0.01, 0.5)
    low = min(grade_y(s) for s in gs)
    cut = max(ground(*at(s)) - grade_y(s) for s in gs); fill = max(grade_y(s) - ground(*at(s)) for s in gs)
    note(f"#2 the ground along the line (grass): natural from the lane's edge ({y_e:.2f}) up to the crest by 1539's corner ({y_c:.2f}, coming up at {math.degrees(beta):.1f} deg), then graded: "
         f"a landing hill for {VD:.1f} m/s (steepest {steepest:.1f} deg), a hollow (radius {Rs:.0f} m, lowest {low:.2f}, {fB - low:.2f} m under 1627's floor) and up at "
         f"{SC2_INCLINE_DEG:.0f} deg into the door ({fB:.2f}); against the natural ground it is cut by up to {cut:.2f} m and filled by up to {fill:.2f} m along the line")
    gi = max(int(np.searchsorted(S, s_c)) - 3, 0)
    grade = {"path": [round(float(v), 3) for p in down[gi:] for v in p], "profile": [round(float(v), 3) for s in gs for v in (s - S[gi], grade_y(s))],
             "half_width": 3.5, "label": "sc2 run down to house 1627"}
    # -- 1627: the climb goes on through the door and up the stairs, then curves up (a kicker) to the lip at the upstairs
    # front wall, SC2_LIP_UP above the upstairs floor, leaving at SC2_LIP_DEG
    lipY = upB + SC2_LIP_UP; thL = math.radians(SC2_LIP_DEG)
    lo_, hi_ = 1.0, 400.0                              # the kicker's radius so the lip lands at lipY at the front wall
    for _ in range(80):
        Rk = (lo_ + hi_) / 2; xk = Rk * (math.sin(thL) - math.sin(th))
        rise = (tB - xk) * math.tan(th) + Rk * (math.cos(th) - math.cos(thL))
        if rise > lipY - fB: hi_ = Rk
        else: lo_ = Rk
    Rk = lo_; xk = Rk * (math.sin(thL) - math.sin(th)); tk = tB - xk
    if tk < 0 or Rk >= 399.0: raise SystemExit(f"#2: a {SC2_LIP_DEG} deg kicker does not fit in 1627 (it would start {-tk:.1f} m outside the door)")
    def stair_y(t):
        if t <= tk: return fB + t * math.tan(th)
        cx, cy = tk - Rk * math.sin(th), fB + tk * math.tan(th) + Rk * math.cos(th)
        x = min(t, tB) - cx
        return cy - math.sqrt(max(Rk * Rk - x * x, 0.0))
    note(f"#2 house 1627 (floor {fB:.2f}, upstairs {upB:.2f}): the line crosses its back and front walls {off_sq:.0f} deg off square, {tB:.1f} m inside; "
         f"in through a sliding door at {SC2_INCLINE_DEG:.0f} deg, the stairs straight for {tk:.1f} m, then a kicker of radius {Rk:.0f} m up to {SC2_LIP_DEG:.0f} deg; "
         f"its lip at the upstairs front wall, {lipY:.2f} ({SC2_LIP_UP:.1f} m over the upstairs floor, {lipY - ground(*(Pf + h2 * 6.0)):.2f} m over the front yard)")
    yard = ground(*(Pf + h2 * 6.0))
    for v in (16.0, 20.0, 22.0, 24.0, 26.0):
        vx, vy = v * math.cos(thL), v * math.sin(thL); h0 = lipY - yard
        tf = (vy + math.sqrt(vy * vy + 2 * 9.81 * h0)) / 9.81
        note(f"#2 off 1627's kicker at {v:.0f} m/s: up to {h0 + vy * vy / 19.62:.1f} m over the front yard, down {vx * tf:.0f} m out (level yard; the gap between 1615 and 1459 is {float((M - Pf) @ h2):.0f} m out)")
    prisms = []
    HWB = 3.0
    # its ends run along the two walls (the line crosses them off square: ends square to the line would poke out of both),
    # 0.15 m out at the back door, 0.03 m inside the front
    def end_on(Pw, side): return intersect(Pb + left(h2) * side, h2, Pw, B["ua"])
    sp = [end_on(Pb - B["F"] * 0.15, -HWB), end_on(Pf - B["F"] * 0.03, -HWB), end_on(Pf - B["F"] * 0.03, HWB), end_on(Pb - B["F"] * 0.15, HWB)]
    st_ = [float((q - Pb) @ h2) for q in sp]
    prisms.append(prism("sc2 stairs in house 1627", "stairs", sp, Pb, h2, [(t, stair_y(t)) for t in np.linspace(min(st_), max(st_), 60)], STAIRS, held=True))
    # the opening along the line, seen square to it: the door and the window are DOOR_HALF either side of the crossing,
    # along walls the line crosses off_sq off square
    across = DOOR_HALF * math.cos(math.radians(off_sq))
    note(f"#2 1627's sliding door and upstairs window: {2 * DOOR_HALF:.1f} m wide, {2 * across:.2f} m across the line (a car 1.85 m wide has {across - 0.925:.2f} m either side)")
    run_out = polyline([Pf, Pf + h2 * 12.0, M - uL * 5.0, M, T, G2, G2 + uL * 6.0], 2.0)   # back on to the gap's axis in 1627's front yard
    arc, s_end, R, ang, err = arc_onto_lap(G2 + uL * 6.0, uL, 520, 560, +1)
    note(f"#2 back on to the lane: an arc of radius {R:.0f} m turning {ang:.0f} deg left, joining the lane {s_end:.0f} m after the gate (fit {err:.2f} rad)")
    through = polyline([Pb, Pf], 1.0)
    dense = down + through[1:] + smooth(run_out + polyline(arc + [lap(s_end + 8)], 2.0)[1:])[1:]
    for hn in (1539, 1615, 1459, 1602, 1919, 1993, 1961):
        note(f"#2 house {hn}: nearest corner {nearest_corner(dense, hn):.1f} m from the line")
    # houses a car could come down on after the flight out of 1627 (Dan: "If someone ends up on a roof of a house, it is not a
    # tragedy, they can just drive off of it"): every house within 12 m of the line from 1627's front to 75 m out gets a roof
    # collider with no valley, chimney pocket or lip (MapBuilder: the convex hull of the house)
    flight = [Pf + h2 * d for d in np.arange(0.0, 75.0, 1.0)]
    roof_houses = sorted({i for i in AREA["houses"] if i != 1627 and min(rect_dist(p, i) for p in flight) < 12.0})
    note(f"#2 houses near the flight out of 1627 (roofs a car can drive off): {roof_houses}")
    entry_s = station(dense, W1)                        # the entry plane: at 1539's corner, well clear of the lane
    def wall(P, N): return [round(float(P[0]), 3), round(float(P[1]), 3), round(float(N[0]), 5), round(float(N[1]), 5)]
    houses = [
        # Round 12: in through a sliding door (door_half: its glass's half width along the back wall), out through an upstairs
        # window (window_b); only glass and frames break. upper_u: the upstairs front kept free of the house's own windows.
        {"building": 1627, "a": list(map(float, Pb)), "b": list(map(float, Pf)), "half_width": round(across, 3), "wall_a": wall(Pb, -B["F"]), "wall_b": wall(Pf, B["F"]),
         "y0_a": round(fB + 0.05, 3), "y1_a": round(fB + 2.25, 3), "y0_b": round(lipY - 0.15, 3), "y1_b": round(upB + 2.62, 3), "floor_y": round(fB, 3),
         "sliding_door_a": True, "door_half": DOOR_HALF, "window_b": DOOR_HALF, "no_entry_gable": True,
         "upper_u": [round(u_exit - DOOR_HALF - 0.4, 3), round(u_exit + DOOR_HALF + 0.4, 3)]},
    ]
    # the side yard and the run down to the door are held (Round 12, RampHold): at racing speed the car would leave the yard's
    # knee or the crest still turning and fly off the line; held, it stays on the grass and steers. It is a landing zone too
    landing = [{"poly": [round(float(v), 2) for p_ in corridor_poly([at(s) for s in np.arange(s_e + 2.0, s_door, 1.0)], 4.0) for v in p_], "hold": True},
               {"poly": [round(float(v), 2) for p_ in corridor_poly([Pf + h2 * d for d in np.arange(0.0, 62.0, 2.0)], 6.5) for v in p_]}]
    return {
        "id": "sc2_two_houses", "name": "Two-house drop", "forgives": [2],
        "path": dense, "half_width": 4.0, "trail": [], "clear_half_width": 4.5, "entry_half_width": 3.0, "entry_margin": 1.0,   # 4.5: the side yard is narrow, keep the houses' shrubs
        "entry_s": entry_s, "prisms": prisms, "open_houses": houses, "jump_houses": [], "roof_houses": [{"building": i} for i in roof_houses],
        "grades": [grade], "landing_zones": landing,
        "sign": {"x": round(float(Mo[0]), 2), "z": round(float(Mo[1]), 2)},
        "jumps": [{"label": "upstairs of house 1627 (out through its front)", "x": float(Pf[0]), "z": float(Pf[1])}],
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
    # Round 11 (BUG-007, Dan: "is the reception ramp really necesarry? I think it will be ok without it"): no landing deck.
    # The car comes down on the ground behind 1641. That ground is a landing zone instead: RampHold keeps the car's speed on
    # touching down there, as it did on the deck (from the back wall to 75 m, 6.5 m either side of the line)
    LZ_FAR, LZ_HALF = 75.0, 6.5
    landing_zone = [o + u * sf - n * LZ_HALF, o + u * LZ_FAR - n * LZ_HALF, o + u * LZ_FAR + n * LZ_HALF, o + u * sf + n * LZ_HALF]
    gp = [ground(*(o + u * t)) for t in np.arange(sf, LZ_FAR, 1.0)]
    note(f"#6 no landing deck: the car lands on the ground behind 1641 (a landing zone from its back wall, station {sf:.1f}, to {LZ_FAR:.0f} m, {LZ_HALF} m either side); "
         f"the ground along the line falls {gp[0] - gp[-1]:.1f} m over it, steepest {max(abs(a - b) for a, b in zip(gp[:-1], gp[1:])) * 100:.0f}% over 1 m; the eaves are about {yTop - 4.6 + 5.9 - ground(*q):.1f} m above the ground at the back wall")
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
    trail = []                                          # Round 11: no dirt trail
    lipP = o + u * lip_t
    return {
        "id": "sc6_over_house", "name": "Rooftop leap", "forgives": [5], "entry_half_width": 3.5, "entry_margin": 1.0,
        "path": pts, "half_width": 4.0, "trail": trail, "clear_half_width": 6.5,
        "entry_s": entry_s, "prisms": prisms, "open_houses": [], "jump_houses": [{"building": 1641}],
        "landing_zones": [{"poly": [round(float(v), 2) for p_ in landing_zone for v in p_]}],
        "jumps": [{"label": "kicker over house 1641", "x": float(lipP[0]), "z": float(lipP[1])}],
        "leave_lap_s": 729, "rejoin_lap_s": float(REJOIN_6),
    }

# =============================================================================================================== R1, Round 18
# The reverse loop's lobe drop (Dan, 2026-10-11: "I would like r1 built out", "build it as marked ... sized for 20 m/s ... it
# must not put a vehicle on a roof it cannot get off, or into a wall"). NOT BUILT: as marked it does not fit (Round 18
# report in TODO.md). The kicker on the bank between houses 1987 and 1552 (the 6.6 m between 1987's north corner and 1552's
# west corner) facing the gap between 2065 and 1572: the flight crosses that gap at an angle (the gap runs north-north-west),
# so even exactly on the line a vehicle at 24 m/s or more comes down into 1572's wall (lip 1.2 m, 15 degrees; a taller,
# steeper kicker only moves the band), and 1.5 m or 6 degrees off the line it reaches 1572 or 2065 from 22 m/s. Vehicles
# reach that kicker at 24 to 28 m/s from the corner (a straight run up the gap). The only robust lip found (every speed
# from 14 to 32 m/s, 1.5 m and 6 degrees off the line, comes down on open ground) is in the lobe's middle yard facing
# west-south-west, reached by a long left bend after the gap, not on the bank; it would rejoin on the lobe's west side. Dan
# to choose. R2 was not chosen.
RETIRED = {"r1_lobe_drop", "r1_kids_ramp"}   # removed from the route file's official_shortcuts on a re-run

# =============================================================================================================== R3, Round 18
# The reverse loop's house hop, redesigned to Dan's idea (2026-10-11: "use a slide from a play ground as a ramp which would
# bounce you onto a trampoline which would ultimately bounce you over the house instead of driving through it"). REVERSE
# ONLY. House 1752 stays whole and untouched. From the top street the line goes through the gap between 1602 and 1919 and
# east along the hollow into 1752's back yard, where a giant play set stands: the vehicle drives UP its slide (held, as the
# Round 10 ramps), across the tower's deck and off its far edge onto a giant trampoline. The trampoline (Trampoline.cs)
# throws it up and over 1752 onto Chastain Glen Lane beyond, where it comes down on its wheels pointing down the lane.
# Forgives CP2 (file numbering; the reverse lap's CP4).
#   Sizes (a vehicle needs them; the look is a play set and a trampoline at that size): slide 3.6 m wide (its foot flared to
#   9 m), 16 m long, up 7 m;
#   tower deck 4.0 x 3.5 m, 1.8 m above the trampoline's mat, 13 m from the mat's middle; trampoline 13 m across the mat on a
#   1 m high frame. The play set cannot stand further east (a throw from the mat's near edge would not clear 1752's eaves),
#   so the play set faces due east (round the same trampoline) and the way from the 1602 / 1919 gap to the slide's foot is one
#   fitted arc onto the slide's axis (Round 18 checks: on a 6 m corner the cars went wide into the slide's side).
#   Off a level deck 1.8 m above the mat a vehicle comes down 0.61 x its speed out: the mat (6.5 to 19.5 m out) takes any
#   speed from about 11 to 32 m/s; slower lands short in the yard (drive out round the house), faster is caught as it
#   passes low over the mat. The bounce aims every vehicle at the same point on the lane with the same flight time (4.0 s,
#   about 20 m over the mat at the top, 3 m or more over the roofs of 1752 and its neighbors from anywhere on the mat), so
#   where on the mat it lands does not matter. 3.4 s cleared 1752's eaves by 0.1 m: too close.
R3_T = V(3068.0, 1184.5); R3_U = V(1.0, 0.0); R3_R = 6.5; R3_FLIGHT = 4.0; R3_GAP = 13.0
def build_r3():
    T, u = R3_T, R3_U
    mat_y = ground(*T) + 1.0
    deck_y = mat_y + 1.8
    E = T - u * R3_GAP; D0 = E - u * 3.5; LC = 16.0; C0 = D0 - u * LC
    gC = ground(*C0) - 0.03
    th, y = two_arcs(deck_y - gC, LC, 10.0, 8.0)
    # the foot flares (a wide run-out, as a big slide's): 9 m wide on the grass, narrowing to the 3.6 m chute over its first 6 m,
    # with low sides that lead a vehicle in (Round 18 checks: the small vehicles came round the bend wide, beside the chute)
    FOOT_HALF, FLARE = 4.5, 6.0
    nS = left(u); foot = [C0 - nS * FOOT_HALF, C0 + u * FLARE - nS * 1.8, C0 + u * LC - nS * 1.8, C0 + u * LC + nS * 1.8, C0 + u * FLARE + nS * 1.8, C0 + nS * FOOT_HALF]
    slide = prism("sc R3 play set slide", "slide", foot, C0, u, [(s, gC + y(s)) for s in np.linspace(0.0, LC, 33)], [0.95, 0.76, 0.14], held=True)
    deck = prism("sc R3 play set deck", "deck", strip_poly(D0, u, -0.05, 3.5, 2.0), D0, u, [(0.0, deck_y), (3.5, deck_y)], DECK, held=True)
    note(f"R3 slide: {LC:.0f} m from {gC:.2f} m up {deck_y - gC:.1f} m to the deck at {deck_y:.2f} m (steepest {math.degrees(th):.0f} degrees); deck 3.5 m; trampoline at ({T[0]:.0f}, {T[1]:.0f}), mat {mat_y:.2f} m, {2 * R3_R:.0f} m across")
    # the landing point on the lane past 1752 (the reverse lap runs south-south-east there) and the heading down it
    s_land = station([lap(s) for s in np.arange(0, CUM[-1], 1.0)], V(3124.8, 1165.0))
    P = lap(s_land); rev = unit(lap(s_land - 1.0) - lap(s_land + 1.0))
    land_yaw = math.degrees(math.atan2(rev[0], rev[1]))
    Py = ground(*P) + 0.7
    # check the flight from every part of the mat: apex and the height over 1752 and 1927 (ridges about 11 m up)
    worst = 99.0
    for ox in (-R3_R, 0.0, R3_R):
        for oz in (-R3_R, 0.0, R3_R):
            p0 = T + V(ox, oz); y0 = mat_y + 0.7
            v_h = (P - p0) / R3_FLIGHT; vy = (Py - y0) / R3_FLIGHT + 4.905 * R3_FLIGHT
            for t in np.arange(0, R3_FLIGHT, 0.02):
                q = p0 + v_h * t; yy = y0 + vy * t - 4.905 * t * t
                for hn in (1752, 1927, 1844):
                    # the roof: eaves 6.3 m up at the walls (two storeys and the floor), the ridge at the house's full height
                    # in the middle, whichever way the ridge runs (the higher of the two), 1 m of margin round the walls
                    r = rect(hn); d = q - r["c"]; da, dp = abs(d @ r["d"]), abs(d @ r["p"])
                    if da > r["hl"] + 1.0 or dp > r["hw"] + 1.0: continue
                    g_ = ground(*r["c"]); ridge = g_ + m["buildings"][hn]["height"]; eave = g_ + 6.3
                    top = max(eave + (ridge - eave) * max(0.0, 1 - dp / r["hw"]), eave + (ridge - eave) * max(0.0, 1 - da / r["hl"]))
                    worst = min(worst, yy - top)
    vy0 = (Py - (mat_y + 0.7)) / R3_FLIGHT + 4.905 * R3_FLIGHT
    note(f"R3 bounce: to ({P[0]:.1f}, {P[1]:.1f}) on the lane in {R3_FLIGHT} s, up at {vy0:.1f} m/s (apex {vy0 * vy0 / 19.62:.1f} m over the mat), "
         f"{float(np.linalg.norm(P - T)) / R3_FLIGHT:.1f} m/s across; lowest clearance over the roofs of 1752 / 1927 / 1844: {worst:.1f} m; lands facing {land_yaw:.0f} degrees")
    # the line (reverse travel): along the top street, through the 1602 / 1919 gap, the hollow, slide, deck, trampoline, the
    # flight (straight in plan), the lane
    s_l = station([lap(s) for s in np.arange(0, CUM[-1], 1.0)], V(3003.0, 1216.0))
    Gn, Gs = V(3015.9, 1205.4), V(3022.0, 1193.5)
    # out of the gap (heading down it), a straight of d m and one circular arc of radius R that ends on the slide's axis, then
    # straight into its foot: the d and R whose arc ends nearest the axis with 5 m or more of straight before the foot
    hG = unit(Gs - Gn); nU = left(u); best = None
    def turn(v, a): return np.array([v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a)])
    for e in np.arange(-1.5, 1.51, 0.5):   # across the gap's south end (about 6 m wide): up to 1.5 m off its middle
        for d in np.arange(0.0, 8.01, 0.5):
            for R in np.arange(6.0, 24.01, 0.5):
                P0 = Gs + left(hG) * e + hG * d; phi = math.atan2(hG[0] * u[1] - hG[1] * u[0], hG @ u)
                c = P0 + left(hG) * R * np.sign(phi); end = c + turn(P0 - c, phi)
                off = abs(float((end - C0) @ nU)); before = float((C0 - end) @ u)
                if before < 5.0: continue
                if best is None or off < best[0] - 0.05 or (abs(off - best[0]) <= 0.05 and R > best[2]): best = (off, d, R, phi, c, P0, end, e)
    off, d_, R_, phi_, c_, P0_, end_, e_ = best
    arc = [c_ + turn(P0_ - c_, phi_ * k / 12) for k in range(13)]
    straight = float((C0 - end_) @ u)
    note(f"R3 way in: out of the 1602 / 1919 gap ({e_:+.2f} m off its middle), {d_:.1f} m straight, an arc of {R_:.1f} m turning {math.degrees(phi_):.0f} degrees onto the slide's axis {straight:.1f} m before its foot (off it by {off:.2f} m)")
    head = smooth(polyline([lap(s_l + 40), lap(s_l + 6), Gn + left(hG) * e_], 2.0), 2)   # Round 19: from 40 m back on the street (the turn-off's sign needs 16 points on it) + polyline([Gn + left(hG) * e_, P0_], 1.0)[1:] + arc[1:]
    pts = head + polyline([C0 - u * straight, C0], 1.0)[1:] + polyline([C0, T], 1.0)[1:] + polyline([T, P], 2.0)[1:] \
        + smooth(polyline([P, lap(s_land - 6), lap(s_land - 16)], 2.0), 2)[1:]
    entry_s = station(pts, C0 + u * 4.0)
    note(f"R3: leaves the top street {s_l:.0f} m after the gate (forward stations), lands on the lane at {s_land:.0f}; {length(pts):.0f} m of line")
    return {
        "id": "r3_trampoline", "name": "Trampoline hop", "direction": "reverse", "forgives": [2], "entry_half_width": 3.0, "entry_margin": 1.0,
        "path": pts, "half_width": 3.0, "trail": [], "clear_half_width": 5.0, "entry_s": entry_s, "prisms": [slide, deck], "open_houses": [],
        "jump_houses": [], "landing_zones": [], "ai_speed": 17.0,
        # Round 19 (item 5, Dan: "the entrance needs to be more obvious"): a birthday-party hint at the turn-off: a PARTY yard sign
        # with an arrow and balloons in the grass strip, worn tire tracks through the gap, bunting and balloons on the play set
        "sign": {"x": round(float(Gn[0]), 2), "z": round(float(Gn[1]), 2)}, "sign_face": "party",
        # the AI's speed along the line (station, m/s): 11 through the 8 m bend out of the gap (the AI's own bend rule samples
        # the line 6 m apart and took it at 17: wide past the slide's foot), full speed from the slide's foot
        "ai_profile": [round(v, 1) for v in (station(pts, P0_) - 12.0, 11.0, station(pts, C0) - 1.0, 11.0, station(pts, C0) + 2.0, 20.0)],
        "playground": {"tower": {"o": rnd(D0), "u": [round(float(u[0]), 5), round(float(u[1]), 5)], "len": 3.5, "half": 2.0, "deck_y": round(deck_y, 3), "roof_y": round(deck_y + 3.4, 3)},
                       "slide": {"o": rnd(C0), "len": LC, "half": 1.8, "foot_half": FOOT_HALF, "flare": FLARE},
                       "trampoline": {"x": round(float(T[0]), 2), "z": round(float(T[1]), 2), "r": R3_R, "mat_y": round(mat_y, 3),
                                      "target": [round(float(P[0]), 2), round(Py, 3), round(float(P[1]), 2)], "flight_s": R3_FLIGHT, "land_yaw": round(land_yaw, 2)}},
        "leave_lap_s": float(s_l), "rejoin_lap_s": float(s_land - 16),
    }


# =============================================================================================================== R1, Round 20 follow-up
# Dan's line across the west lobe, WITH RISK (Dan, 2026-10-12: "I don't mind this area as a shortcut, but it has to has some risk";
# his session 2026-10-12_01-54-20-969_70e57b, BUG-001 "why not start here" at (2804.0, 1018.7), BUG-002 "and come out here" at
# (2774.9, 1079.1)). REVERSE ONLY, forgives CP5 (file numbering; the reverse lap's CP1). It replaces Round 19's kids'-ramp trial.
# Dan's line is one straight line (about 334 degrees) through two side yards: between houses 1987 and 1552, then across the open
# ground behind them, then between 1572 and 1622 out onto Chastain Glen Lane. The open ground in the middle is shortcut #6's landing
# strip (forward races; 13 m wide): nothing may stand in it. The risk:
#   * a big homemade plywood ramp in the front yard, its lip 0.85 m over a privacy fence across the 1987 / 1552 gap ("over the
#     first fence"); the side yard behind it levelled (a lawn);
#   * (Dan, 2026-10-12 03:51: "if someone had a pool it would be in the back yard ... If the pool is further back it would be
#     riskier ... I would prefer it was an above ground pool ... you would drop out of it") an ABOVE-GROUND pool in the back yards
#     behind 1572 / 1622, about 42 m past the first fence, just past #6's strip: a round steel-walled pool, 6 m across, its wall
#     1.25 m, 1 m of water, a ladder on the near side and a small deck on the rim on the gate's side. The aimed launch (BigJump: the
#     lip's angle, no faster than vmax, nothing added for the slow) is held to the speed that just clears the deck: arrive flat out
#     and lined up and you clear the whole pool onto the grass beyond; slow or short and you come down in it (or short of it).
#     Out of the pool: its far half is a wooden ramp up to the deck; drop off the deck's edge to the grass, toward the gate. A vehicle
#     in the water is stopped for 2.5 s, then held to 1 m/s (PoolHold: a fall costs real time). Never a trap;
#   * then a privacy fence across the 1572 / 1622 gap, near its front, with one open side gate 3.5 m wide (the widest vehicle is
#     2.1 m; at 3.3 m the Skyfin clipped a post on the line) to line up for; its posts are solid and round (clip one and you lose speed, or stop);
#   * patio chairs, a table, an umbrella and a grill round the pool and by the gate, breakable.
# The lobe's back yards are fenced off (solid privacy fences across every gap between its houses, as on a property line), so the
# only way through is the shortcut. One gap cannot be fenced for good: #6 leaves the lobe through the gap between 713 and 1052
# (forward). Its fence stands only during a reverse race (RaceOnly). The hint at the turn-off: the ramp itself (homemade, in plain
# sight from the lap) and worn tire tracks to it.
R1_HOUSES = ((1987, 1552), (1572, 1622))     # the two side yards, in travel order
R1_FENCED = ((1552, 1862), (1862, 1861), (1861, 1499), (1572, 2065), (2065, 1452), (1452, 1052), (713, 1987))
R1_REVERSE_ONLY = ((1052, 713, 0.5), (1499, 1641, 0.75), (1641, 1622, 0.75))   # #6's corridor: 713 / 1052 its way out, 1641 the house it jumps
R1_LIP_H, R1_TH, R1_RAMP = 2.6, 18.0, 8.0   # the lip 0.85 m over the first fence's boards; the angle steepens to fit the 8 m ramp
R1_GATE_W, R1_GATE_FRAC = 3.5, 0.88   # the gate fence 88% of the way from yard 2's back to its front (room to line up after landing)
R1_POOL_AT, R1_POOL_LAT, R1_POOL_R, R1_WALL_H, R1_WATER_H, R1_DECK_LEN = 42.0, -1.5, 3.0, 1.25, 1.0, 1.4   # a 6 m (20 ft) pool: a 4.6 m one left a 1 m/s band to land in
R1_CLEAR_M = 1.5   # the lip speed is held to the slowest that comes down this far past the deck's edge
def gap_frame(i, j):
    """The side yard between houses i and j: its front (street side) and back middles, its axis (front to back), its width."""
    ri, rj = rect(i), rect(j)
    def facing(r, other):
        cs = r["corners"]; best = None
        for k in range(4):
            a_, b_ = cs[k], cs[(k + 1) % 4]; m_ = (a_ + b_) / 2; out = m_ - r["c"]; out = out / np.linalg.norm(out)
            score = float(out @ unit(other["c"] - r["c"]))
            if best is None or score > best[0]: best = (score, a_, b_, out)
        return best[1:]
    ai, bi, ni = facing(ri, rj); aj, bj, nj = facing(rj, ri)
    u = unit(bi - ai); w = float((((aj + bj) / 2) - ai) @ ni)
    ts = sorted([float((q - ai) @ u) for q in (ai, bi)]); tj = sorted([float((q - ai) @ u) for q in (aj, bj)])
    t0, t1 = max(ts[0], tj[0]), min(ts[1], tj[1])
    mid = lambda t: ai + u * t + ni * (w / 2)
    e0, e1 = mid(t0), mid(t1)
    if nearest_road(e0)[0] > nearest_road(e1)[0]: e0, e1 = e1, e0
    return {"front": e0, "back": e1, "u": unit(e1 - e0), "w": abs(w), "len": float(np.linalg.norm(e1 - e0)), "overlap": t1 - t0}
def wall_to_wall(i, j, frac, extra=0.8):   # 0.8 m into each wall: no slot where a footprint is set back from its best-fit rectangle
    """A fence across the gap between houses i and j, 'frac' of the way from its front to its back, wall to wall."""
    g = gap_frame(i, j); c = g["front"] + g["u"] * g["len"] * frac; n = left(g["u"])
    return c - n * (g["w"] / 2 + extra), c + n * (g["w"] / 2 + extra), g
def corner_to_corner(i, j):
    """Wide gaps (the lots' corners far apart): the nearest corners of the two houses."""
    best = None
    for p in rect(i)["corners"]:
        for q in rect(j)["corners"]:
            d = float(np.linalg.norm(p - q))
            if best is None or d < best[0]: best = (d, p, q)
    e = unit(best[2] - best[1]) * 0.8   # into both houses
    return best[1] - e, best[2] + e
def fence_line(i, j):
    g = gap_frame(i, j)
    if g["w"] < 13.0 and g["overlap"] > 3.0: a_, b_, _ = wall_to_wall(i, j, 0.35); return a_, b_   # side by side: across the side yard
    return corner_to_corner(i, j)
def ray_flight(P, d, y0, v, th, step=0.005):
    vx, vy = v * math.cos(math.radians(th)), v * math.sin(math.radians(th)); t = 0.0
    while t < 5.0:
        t += step; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t
        if t > 0.05 and y <= ground(*q): return t, vx * t, q
    return t, vx * t, q
def build_r1(P6):
    g1, g2 = gap_frame(*R1_HOUSES[0]), gap_frame(*R1_HOUSES[1])
    F1, B1, u1 = g1["front"], g1["back"], g1["u"]; F2, B2 = g2["front"], g2["back"]
    n1 = left(u1)
    # the side yard between 1987 and 1552 levelled (a lawn: the ground rises 1 m to the front there, and the ramp needs the lower
    # ground to reach its lip at a sane angle), from the first fence to 1 m short of the yard's back
    yard = [F1 + n1 * (g1["w"] / 2 + 0.3), F1 - n1 * (g1["w"] / 2 + 0.3), B1 - u1 * 1.0 - n1 * (g1["w"] / 2 + 0.3), B1 - u1 * 1.0 + n1 * (g1["w"] / 2 + 0.3)]
    gs = sorted(ground(*(F1 + u1 * s_ + n1 * o)) for s_ in np.arange(0.5, g1["len"] - 1.0, 0.5) for o in np.arange(-g1["w"] / 2 + 0.5, g1["w"] / 2 - 0.4, 0.5))
    yard_y = gs[len(gs) // 2] + 0.05
    # the above-ground pool (Dan, 03:51): in the back yards behind 1572 / 1622, R1_POOL_AT m past the first fence, R1_POOL_LAT m to the
    # line's left side (clear of #6's corridor and of 1572); its exit (an inside ramp up to a small deck on the rim, on the gate's side)
    C = F1 + u1 * R1_POOL_AT + n1 * R1_POOL_LAT; r = R1_POOL_R
    pads = [ground(*(C + np.array([math.cos(a), math.sin(a)]) * rr)) for a in np.linspace(0, 2 * math.pi, 16, endpoint=False) for rr in (0.0, r * 0.5, r)]
    pad_y = float(np.median(pads)) + 0.02
    rim = pad_y + R1_WALL_H
    # the exit: the pool's whole far half is a ramp (a convex half disc just inside the wall: nowhere for a wheel to drop between
    # ramp and wall), from the floor 0.3 m short of the middle up to the rim where the deck crosses the wall; the deck (3.2 m wide)
    # on the rim, over the wall and 1.4 m beyond it, whose outer edge you drop off
    EXIT_HALF = 1.6; s_top = math.sqrt(r * r - EXIT_HALF ** 2); ex0 = C - u1 * 0.3; rin = r - 0.08
    half_disc = [ex0 - n1 * math.sqrt(rin * rin - 0.09)] + [C + u1 * (rin * math.cos(a_)) + n1 * (rin * math.sin(a_)) for a_ in np.linspace(-math.pi / 2 + 0.1, math.pi / 2 - 0.1, 11)] + [ex0 + n1 * math.sqrt(rin * rin - 0.09)]
    ramp_in = prism("sc R1 pool exit ramp", "deck", half_disc, ex0, u1,
                    [(s_, pad_y - 0.02 + (rim + 0.05 - pad_y + 0.02) * min(1.0, s_ / (s_top + 0.3))) for s_ in np.linspace(0.0, r + 0.3, 13)], [0.55, 0.42, 0.28])
    D0 = C + u1 * s_top
    deck = prism("sc R1 pool deck", "deck", strip_poly(D0, u1, 0.0, r - s_top + R1_DECK_LEN, EXIT_HALF), D0, u1, [(0.0, rim + 0.05), (r - s_top + R1_DECK_LEN, rim + 0.05)], [0.55, 0.42, 0.28])
    far = float((C - F1) @ u1) + r + R1_DECK_LEN         # the deck's outer edge, from the first fence
    near = float((C - F1) @ u1) - math.sqrt(max(r * r - R1_POOL_LAT ** 2, 0.0))   # the near wall where the line crosses it
    # the ramp: in the front yard, its lip 0.3 m in front of the first fence, R1_LIP_H over the levelled yard
    LIP = F1 - u1 * 0.3; y1 = yard_y + R1_LIP_H
    foot = LIP - u1 * R1_RAMP; y0 = ground(*foot) - 0.03
    k = max(math.tan(math.radians(R1_TH)), 2 * (y1 - y0) / R1_RAMP); th = math.degrees(math.atan(k))   # steeper than R1_TH when the 8 m ramp is short for its rise
    Lk = min(R1_RAMP, 2 * (y1 - y0) / k); flat = R1_RAMP - Lk; rise_flat = (y1 - y0) - k * Lk / 2
    prof = [(s_, y0 + (rise_flat * min(s_, flat) / flat if flat > 1e-6 else 0.0) + k * max(0.0, s_ - flat) ** 2 / (2 * Lk)) for s_ in np.linspace(0.0, R1_RAMP, 33)]
    ramp = prism("sc R1 pool ramp", "deck", strip_poly(foot, u1, 0.0, R1_RAMP, 2.6), foot, u1, prof, [0.74, 0.62, 0.44])
    # flights (the aimed launch: every vehicle on the same arc for the same lip speed) over the real ground, the pool (its walls, the
    # water's surface and the deck count as "in the pool"): where it comes down, from the first fence
    def top(q):
        s_ = float((q - F1) @ u1)
        inside = np.linalg.norm(q - C) < r + 0.05 or (abs(float((q - D0) @ n1)) < EXIT_HALF and 0.0 <= float((q - D0) @ u1) <= r - s_top + R1_DECK_LEN)
        return max(ground(*q), rim if inside else -1e9)
    def flight(v):
        vx, vy = v * math.cos(math.radians(th)), v * math.sin(math.radians(th)); t = 0.0
        while t < 5.0:
            t += 0.005; q = LIP + u1 * vx * t; y = y1 + vy * t - 4.905 * t * t
            if t > 0.05 and y <= top(q): return round(t, 2), round(float((q - F1) @ u1), 1), bool(y - ground(*q) > 0.5)
        return t, -1.0, False
    fl = {v: flight(v) for v in np.arange(14.0, 25.01, 0.5)}
    clean = [v for v, f in fl.items() if f[1] > far + 0.3]
    clean_from = min(clean) if clean else 99.0
    vmax = min(v for v, f in fl.items() if f[1] >= far + R1_CLEAR_M) if any(f[1] >= far + R1_CLEAR_M for f in fl.values()) else max(fl)
    in_pool = [v for v, f in fl.items() if f[2]]
    pool_v = (float(np.median(in_pool)) if in_pool else clean_from - 1.0) - 0.45   # the near half of the band (exe: 20.3 and 20.8 come down in the pool, the AI at 20.7 cleared it)
    to_gate_back = float((B2 - F1) @ u1)
    note(f"R1 (Dan's line, with risk): yard 1 between 1987 / 1552 {g1['w']:.1f} m wide, {g1['len']:.1f} m long, levelled at {yard_y:.2f} m; yard 2 between 1572 / 1622 {g2['w']:.1f} m wide; "
         f"its back {to_gate_back:.0f} m from the first fence")
    note(f"R1 ramp {R1_RAMP:.0f} m from {y0:.2f} m to the lip at {y1:.2f} m ({R1_LIP_H} m over the yard, {th:.1f} degrees at the lip), 5.2 m wide; "
         f"the above-ground pool {2 * r:.1f} m across, its wall {R1_WALL_H} m, at {float((C - F1) @ u1):.1f} m past the first fence ({R1_POOL_LAT:+.1f} m to the side), "
         f"the line crosses its near wall at {near:.1f} m, its deck's edge at {far:.1f} m; "
         f"flights (air s, m past the fence, in the pool): " + ", ".join(f"{v:.1f} {f}" for v, f in fl.items() if v in (14, 16, 18, 19, 20, 20.5, 21, 21.5, 22, 23, 24)) +
         f"; clean from {clean_from:.1f} m/s, lip speed held to {vmax:.1f}; in the pool around {pool_v:.1f}")
    # the line: on the reverse lap toward the junction, up the ramp, the flight over the pool, through the gate, onto the lane
    lapl = [lap(s_) for s_ in np.arange(0, CUM[-1], 1.0)]
    s_ramp = station(lapl, foot)
    head = smooth(polyline([lap(s_ramp + 30), lap(s_ramp + 15), foot - u1 * 6.0, foot], 2.0), 2)
    G = B2 + (F2 - B2) * R1_GATE_FRAC; u2 = unit(F2 - B2)
    s_join = station(lapl, F2 + u2 * 9.0)
    out = smooth(polyline([F2, F2 + u2 * 5.0, lap(s_join), lap(s_join - 10), lap(s_join - 22)], 2.0), 2)
    # through the pool's middle (it sits 1 m to the side of the flight's line): the way a vehicle in it goes, up its exit ramp and off the deck
    pts = head + polyline([foot, LIP], 1.0)[1:] + polyline([LIP, C - u1 * (r + 6.0), C - u1 * (r + 0.5)], 2.0)[1:] + polyline([C - u1 * (r + 0.5), C + u1 * (r + R1_DECK_LEN + 1.5), B2], 1.0)[1:]         + polyline([B2, G, F2], 1.0)[1:] + out[1:]
    entry_s = station(pts, LIP - u1 * 2.0)
    landing = [{"poly": [round(float(v), 2) for p_ in corridor_poly([F1 + u1 * d_ for d_ in np.arange(far, to_gate_back + 2.0, 1.0)], 3.0) for v in p_]}]
    # the fences
    fences = []
    def add(a_, b_, label, kind="solid", gate=None):
        f_ = {"a": rnd(a_), "b": rnd(b_), "label": label, "kind": kind}
        if gate: f_["gate"] = gate
        fences.append(f_)
    add(F1 + u1 * 0.2 - n1 * (g1["w"] / 2 + 0.8), F1 + u1 * 0.2 + n1 * (g1["w"] / 2 + 0.8), "1987/1552 (the ramp's fence)")
    ga, gb, _ = wall_to_wall(*R1_HOUSES[1], 1.0 - R1_GATE_FRAC)
    add(ga, gb, "1572/1622 (the side gate)", gate={"c": rnd(G), "w": R1_GATE_W})
    for i, j in R1_FENCED:
        a_, b_ = fence_line(i, j); add(a_, b_, f"{i}/{j}")
    for i, j, fr in R1_REVERSE_ONLY:
        a_, b_, _ = wall_to_wall(i, j, fr); add(a_, b_, f"{i}/{j} (reverse races only: in #6's corridor)", kind="reverse_only")
    def dist_to_line(q, P):
        return min(float(np.linalg.norm(q - (a_ + (b_ - a_) * np.clip((q - a_) @ (b_ - a_) / max(1e-9, (b_ - a_) @ (b_ - a_)), 0, 1)))) for a_, b_ in zip(P[:-1], P[1:]))
    for f_ in fences:
        a_, b_ = np.array(f_["a"]), np.array(f_["b"])
        d6 = min(dist_to_line(a_ + (b_ - a_) * t, P6) for t in np.linspace(0, 1, 11))
        note(f"R1 fence {f_['label']}: {np.linalg.norm(b_ - a_):.1f} m, {f_['kind']}, {d6:.1f} m from #6's line" + (" (INSIDE #6's 6.5 m corridor)" if d6 < 6.5 and f_["kind"] == "solid" else ""))
    d6p = min(dist_to_line(C + np.array([math.cos(a), math.sin(a)]) * r, P6) for a in np.linspace(0, 2 * math.pi, 36))
    note(f"R1 pool: {d6p:.1f} m from #6's line at its nearest (its corridor is 6.5 m)")
    # breakable patio things round the pool (on the lawn, off the line and out of #6's corridor) and by the gate
    props = []
    for s_, lat, kind in ((-0.8, r + 1.6, "chair"), (0.8, r + 1.6, "chair"), (-1.8, r + 1.4, "table"), (-1.8, r + 2.6, "umbrella"), (1.5, -(r + 1.3), "grill")):
        q = C + u1 * s_ + n1 * lat
        if dist_to_line(q, P6) < 7.0: continue
        props.append({"kind": kind, "x": round(float(q[0]), 2), "z": round(float(q[1]), 2), "yaw": round(math.degrees(math.atan2(*(unit(C - q)))), 1)})
    for off, kind in ((-2.6, "chair"), (2.4, "chair")):
        q = G - u2 * 3.0 + left(u2) * off; props.append({"kind": kind, "x": round(float(q[0]), 2), "z": round(float(q[1]), 2), "yaw": round(math.degrees(math.atan2(*u2)), 1)})
    tracks = polyline([lap(s_ramp + 14), foot - u1 * 4.0, foot], 1.0)
    # the AI: on to the ramp at 24 (held to vmax off the lip: clean), slow for the gate, out onto the lane; a sloppy try comes up the
    # ramp at the speed that lands it in the pool (then up its exit, off the deck, on to the gate)
    sF, sL, sG = station(pts, foot), station(pts, LIP), station(pts, G)
    ai = [round(v, 1) for v in (sF - 25.0, 24.0, sL + 2.0, 24.0, sG - 8.0, 10.0, sG + 6.0, 10.0)]
    ai_slip = [round(v, 1) for v in (sF - 25.0, pool_v, sL + 2.0, pool_v, sG - 8.0, 10.0, sG + 6.0, 10.0)]
    note(f"R1: leaves the reverse lap at forward station {s_ramp + 15:.0f}, joins it at {s_join:.0f}; {length(pts):.0f} m of line against {abs(s_ramp + 15 - s_join):.0f} m of lap; entry plane {entry_s:.0f} m in")
    return {
        "id": "r1_pool_jump", "name": "Pool jump", "direction": "reverse", "forgives": [5], "entry_half_width": 3.0, "entry_margin": 1.0,
        "path": pts, "half_width": 3.0, "trail": [], "clear_half_width": 4.0, "entry_s": entry_s, "prisms": [ramp, ramp_in, deck], "open_houses": [],
        "jump_houses": [], "landing_zones": landing, "ai_speed": 24.0, "ai_profile": ai, "ai_take": 0.85, "ai_slip": 0.0, "ai_slip_profile": ai_slip,   # sloppy tries off: the exe check put one on the pool's near rim and against its wall (11 rescues); the band that lands in the pool is under 1 m/s wide
        "look": "pool_jump",
        "big_jump": {"lip": [round(float(LIP[0]), 2), round(float(y1), 3), round(float(LIP[1]), 2)], "dir": [round(float(u1[0]), 5), round(float(u1[1]), 5)], "vmax": round(float(vmax), 1), "half": 2.6, "angle": round(th, 2)},
        "pool": {"yard": [v for q in yard for v in rnd(q)], "yard_y": round(float(yard_y), 3), "c": rnd(C), "r": r, "pad_y": round(pad_y, 3), "wall_h": R1_WALL_H, "water_h": R1_WATER_H,
                 "axis": [round(float(u1[0]), 5), round(float(u1[1]), 5)], "front": rnd(F1), "clean_from": round(float(clean_from), 1), "pool_speed": round(pool_v, 1)},
        "fences": fences, "props": props, "tracks": [v for q in tracks for v in rnd(q)],
        "leave_lap_s": float(s_ramp + 15), "rejoin_lap_s": float(s_join),
    }

def rnd(p): return [round(float(p[0]), 2), round(float(p[1]), 2)]
shortcuts = [build_sc2(), build_sc6(), build_r3()]
shortcuts.append(build_r1(shortcuts[1]["path"]))   # Round 20 follow-up: R1 on Dan's line, with risk (replaces Round 19's trial)

# ---------------------------------------------------------------- checks against the barriers and the checkpoints
for sc in shortcuts:
    P = sc["path"]
    for c in route.get("closures", []):
        b = V(c["x"], c["z"]); d = min(float(np.linalg.norm(p - b)) for p in P)
        # the race leaves a barrier out only if the line passes within 3 m of it (RaceBarriers.ShortcutClearM)
        if d < c["width_m"] / 2 + 3 + 3: note(f"barrier {c['id']} ({c['label']}) is in the way of {sc['id']} ({d:.1f} m from the line): the race leaves it out")
        elif d < 20: note(f"{sc['id']}: barrier {c['id']} ({c['label']}) stands {d:.1f} m from the line, clear of it, and stays")
    note(f"{sc['id']}: {length(P):.0f} m of line, against {abs(sc['rejoin_lap_s'] - sc['leave_lap_s']):.0f} m of lap between the same points; entry plane {sc['entry_s']:.0f} m in")

# ---------------------------------------------------------------- write
out = {"source": "Tools/mapgen/make_shortcuts.py (hand-made design; re-run after mapgen). Heights are absolute world y.", "route": "chastain_glen_loop", "notes": notes, "shortcuts": []}
for sc in shortcuts:
    o = dict(sc); o["path"] = [v for p in sc["path"] for v in rnd(p)]
    o["trail"] = []; o["entry_s"] = round(sc["entry_s"], 2)
    out["shortcuts"].append(o)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)

# official_shortcuts in the route file: keep any other hand-made entries, replace these (Round 18: R3, reverse only)
keep = [s for s in route.get("official_shortcuts", []) if s.get("id") not in {sc["id"] for sc in shortcuts} | RETIRED]
for sc in shortcuts:
    keep.append({"id": sc["id"], "name": sc["name"], "direction": sc.get("direction", "forward"), "path": [v for p in sc["path"] for v in rnd(p)],
                 "half_width_m": sc.get("entry_half_width", 5.0), "entry_inset_m": round(sc["entry_s"], 2), "entry_margin_m": sc.get("entry_margin", 2.0), "forgives": sc["forgives"],
                 "ai_speed": sc.get("ai_speed", 0.0), "ai_profile": sc.get("ai_profile", []),
                 **{k_: sc[k_] for k_ in ("ai_take", "ai_slip", "ai_slip_profile") if k_ in sc}})
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
