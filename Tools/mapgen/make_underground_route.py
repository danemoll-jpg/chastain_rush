"""Round 27 (Part D): the UNDERGROUND RACE (Dan's vision, Round 23: "a route through the network (through the hub) raced from the menu
with the AI, its own checkpoints. During that race, GATES (barred grilles / sluice gates) seal off the rest of the maze"). Writes
Data/routes/underground.json from Data/pipes/network.json (re-run after pipe_network.py, then Build Map).

Round 28 (Dan, after driving it: "The tight hairpin turn right before the finish is a terrible idea. Lets move it down a level where
there is some open space"; "The entrance back into the hub I think should have a ramp or something"; "Lets try the shortcuts"). The lap:
  the LINE on the span -> up onto the top ledge -> M5b -> J3 -> M5d -> J15 -> M5c along the Enclave pond (glass under W4) -> J14 -> M5a
  -> J1 -> M1a (glass under W1) -> J2 -> M1b -> out of M1's mouth (its apron) onto the hub's MID LEDGE (west end) -> right, round the mid
  ledge's south, east and north sides (the turn-round, a level down) -> left onto THE SPAN, straight up across the chasm onto the top
  ledge -> M5's mouth.
The grid is on the mid ledge's north side west of the span, off the lap: a stopped player is not in anyone's way. The mid-to-top ramp is gone (the
span replaces it), so the mid ledge is a ring. Railings (the race only) along every edge of the pit the race passes (Round 58; Round 28
had the west and south-west only) and along the span's sides over the chasm; open only at S1's gate and the top of S1's floor ramp.
SWITCH SHORTCUTS (Round 27's proposal, as built; each behind a striped grille that rises for everyone, for the rest of the race, once any
racer drives over its plate):
  S1 "Waterfall leap": straight on out of M1's mouth over the ledge's edge (its grille, a gate in the railing) with M1's water, down to
     the hub's floor, round the pool's west end and up the floor ramp to the mid ledge's east side. Skips the mid ledge's south side
     (forgives CP5). Plate: up M5's right-hand wall, 60 m in from the hub (ride the wall to hit it).
  S2 "Dive pipe": at J15, the pipe straight to J14, diving under M5's bend to a flooded sump. Skips the glass under W4 (forgives CP2).
     Plate: on J3's floor, in the corner to the right of the way in from M5 (6.5 m off the line).
  S3 "High line": at J1, the pipe straight to J2, in high on J2's west wall: a jump over J2's floor, lined up with M1b across the room.
     Skips the glass under W1 (forgives CP3 and CP4). Plate: in J1's far corner.
The AI take an open shortcut by ShortcutStrategy and can go for a plate (each plate's detour is a lap option, also by ShortcutStrategy).

Usage: python Tools/mapgen/make_underground_route.py
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
NET = json.load(open(os.path.join(ROOT, "Data/pipes/network.json")))
OUT = os.path.join(ROOT, "Data/routes/underground.json")
T = {t["id"]: t for t in NET["tubes"]}
C = {c["id"]: c for c in NET["chambers"]}
hub = C["HUB"]
FLOOR, MID, TOP = hub["floor"], hub["mid"], hub["top"]
HL, HW, IM, IF = hub["length"] / 2, hub["width"] / 2, hub["inset_mid"], hub["inset_floor"]
hu = (math.sin(math.radians(hub["yaw"])), math.cos(math.radians(hub["yaw"])))
hv = (hu[1], -hu[0])


def H(a, b, y):
    """a point of the hub: a along its length (east), b across it (south), at height y"""
    return (hub["x"] + hu[0] * a + hv[0] * b, y, hub["z"] + hu[1] * a + hv[1] * b)


def opening(cid, tube):
    return next(o for o in C[cid]["openings"] if o["tube"] == tube)


def pts(tid, rev=False):
    p = T[tid]["pts"]; q = [(p[i], p[i + 1], p[i + 2]) for i in range(0, len(p), 3)]
    return list(reversed(q)) if rev else q


def centre(cid):
    c = C[cid]; return (c["x"], c["floor"], c["z"])


def local(cid, u, v):
    """a point on a chamber's floor: u along its yaw, v to its right"""
    c = C[cid]; cu = (math.sin(math.radians(c["yaw"])), math.cos(math.radians(c["yaw"]))); cv = (cu[1], -cu[0])
    return (c["x"] + cu[0] * u + cv[0] * v, c["floor"], c["z"] + cu[1] * u + cv[1] * v)


def bulge(cid, a, b, c, n=8):
    """a smooth curve through a chamber: from a over b (the plate) to c (each (u, v)), a quadratic through b"""
    out = []
    for k in range(n + 1):
        t = k / n; u = (1 - t) ** 2 * a[0] + 2 * (1 - t) * t * (2 * b[0] - (a[0] + c[0]) / 2) + t * t * c[0]
        v = (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * (2 * b[1] - (a[1] + c[1]) / 2) + t * t * c[1]
        out.append(local(cid, u, v))
    return out

def every(line, step):
    """the line's points at least 'step' m apart (keeps the first and last)"""
    out = [line[0]]; acc = 0.0
    for i in range(1, len(line)):
        acc += math.dist((line[i][0], line[i][2]), (line[i - 1][0], line[i - 1][2]))
        if acc >= step or i == len(line) - 1: out.append(line[i]); acc = 0.0
    return out


def dense(line, step=2.0):
    """straight runs between hand-placed points filled in every 'step' m, heights interpolated"""
    out = [line[0]]
    for a, b in zip(line, line[1:]):
        n = max(1, int(math.ceil(math.dist((a[0], a[2]), (b[0], b[2])) / step)))
        for k in range(1, n + 1):
            t = k / n; out.append(tuple(a[i] + (b[i] - a[i]) * t for i in range(3)))
    return out


def flat_len(line):
    return sum(math.dist((a[0], a[2]), (b[0], b[2])) for a, b in zip(line, line[1:]))


def head(line, m):
    """the first m meters of a line"""
    out = [line[0]]; acc = 0.0
    for a, b in zip(line, line[1:]):
        acc += math.dist((a[0], a[2]), (b[0], b[2])); out.append(b)
        if acc >= m: break
    return out


def tail(line, m):
    return list(reversed(head(list(reversed(line)), m)))


# ---- the span (MapBuilder.NetworkHub.cs BuildSpan: the same numbers)
A5 = opening("HUB", "M5b")["t"]                                         # the span's line and M5's mouth (a)
SPAN_B0, SPAN_B1 = -(HW - IM), HW - IM; SPAN_RISE = SPAN_B0 + 3.0; SPAN_EASE = 6.0


def span_y(b):
    L, Hh = SPAN_B1 - SPAN_RISE, TOP - MID; g = Hh / (L - SPAN_EASE); s = b - SPAN_RISE
    if s <= 0: return MID
    if s >= L: return TOP
    if s < SPAN_EASE: return MID + g * s * s / (2 * SPAN_EASE)
    if s > L - SPAN_EASE: return TOP - g * (L - s) ** 2 / (2 * SPAN_EASE)
    return MID + g * (s - SPAN_EASE * 0.5)


def on_span(a, b):
    return abs(a - A5) <= 4.5 and SPAN_B0 <= b <= SPAN_B1


def HM(a, b):
    """a point on the mid ledge, or on the span where the span lies"""
    return H(a, b, span_y(b) if on_span(a, b) else MID)


# ---- the lap
# the LINE on the span, 13 m up it (9 m wide there; in M5's mouth the start jammed four racers into a 5 m pipe on the line)
GATE_B = -8.0
T1 = opening("HUB", "M1b")["t"]                                          # M1's mouth on the mid ledge's west end (b)
lap = [HM(A5, GATE_B)]
lap += [HM(A5, b) for b in range(int(GATE_B) + 2, int(SPAN_B1) + 1, 2)]   # up the span
lap += [H(A5, SPAN_B1 + 2.0, TOP), H(A5, SPAN_B1 + 4.0, TOP), H(A5, HW - 0.5, TOP)]   # across the top ledge into M5's mouth
lap += every(pts("M5b", rev=True), 2.0)[1:]                            # M5's mouth back to J3
lap += [centre("J3")]
lap += every(pts("M5d", rev=True), 2.0) + [centre("J15")]
lap += every(pts("M5c", rev=True), 2.0) + [centre("J14")]               # along the Enclave pond (glass under W4)
lap += every(pts("M5a", rev=True), 2.0) + [centre("J1")]
lap += every(pts("M1a"), 2.0) + [centre("J2")]                          # J1 to J2 under the north pond (glass under W1)
lap += every(pts("M1b"), 2.0)                                           # J2 to the hub's mid ledge, west end
# out of M1's mouth and right, through the mid ledge's south-west corner (never over the west side's edge at a = -(HL - IF))
mid = [HM(-45.0, T1 + 0.6), HM(-42.6, T1 + 2.6), HM(-41.6, T1 + 6.2), HM(-40.6, T1 + 9.3), HM(-37.5, 23.2), HM(-33, 23.5),
       HM(36, 23.5), HM(40, 22.6), HM(42.8, 20), HM(43.5, 16),                                    # the south side, the south-east corner
       HM(43.5, -16), HM(42.8, -20), HM(40, -22.8), HM(36, -23.5),                               # the east side, the north-east corner
       HM(30, -23.2), HM(26, -22.6), HM(23.2, -21.0), HM(21.3, -18.6), HM(A5, -15.0)]            # the north side, left onto the span
lap += dense(mid)[1:]
lap += [HM(A5, b) for b in range(-13, int(GATE_B), 2)]                  # onto the span
lap += [HM(A5, GATE_B)]                                                # closed: the last point is the first (the line)

cum = [0.0]
for a, b in zip(lap, lap[1:]): cum.append(cum[-1] + math.dist((a[0], a[2]), (b[0], b[2])))
L = cum[-1]


def s_near(p, after=0.0, vy=True):
    best, bs = 1e18, 0.0
    for i, q in enumerate(lap):
        if cum[i] < after: continue
        d = (q[0] - p[0]) ** 2 + (q[2] - p[2]) ** 2 + (4.0 * (q[1] - p[1]) ** 2 if vy else 0.0)
        if d < best: best, bs = d, cum[i]
    return bs


def glass_mid(tid):
    t = T[tid]; g = t["glass"][0]; q = pts(tid); s = 0.0; target = (g["s0"] + g["s1"]) * 0.5
    for a, b in zip(q, q[1:]):
        s += math.dist((a[0], a[2]), (b[0], b[2]))
        if s >= target: return b
    return q[-1]


cps = []
for cid, label, at, half, arch in [
    (1, "J3 ENCLAVE JCT", centre("J3"), 7.5, 5.5),
    (2, "GLASS UNDER W4", glass_mid("M5c"), 2.2, 4.0),
    (3, "J1 SOUTH FORK", centre("J1"), 7.5, 5.5),
    (4, "GLASS UNDER W1", glass_mid("M1a"), 2.2, 4.0),
    (5, "HUB MID LEDGE", H(0, 23.5, MID), 4.3, 6.0)]:
    s = s_near(at, after=(cps[-1]["s"] if cps else 5.0))
    cps.append(dict(id=cid, s=round(s, 1), pos=[round(at[0], 2), round(at[2], 2)], label=label, half_m=half, arch_m=arch))


# ---- the seals: every pipe the race does not use, where it opens into J1, J2, J3 and the hub
def seal(sid, label, tid, at_b, into=0.8):
    t = T[tid]; q = pts(tid); end = q[-1] if at_b else q[0]; nxt = q[-3] if at_b else q[2]
    dx, dz = nxt[0] - end[0], nxt[2] - end[2]; n = math.hypot(dx, dz); dx, dz = dx / n, dz / n
    inside = (end[0] + dx * into, end[1], end[2] + dz * into)
    return dict(id=sid, label=label, x=round(inside[0], 2), y=round(inside[1] - 0.05, 2), z=round(inside[2], 2), dx=round(dx, 4), dz=round(dz, 4), w=t["d"], h=t["d"])


seals = [
    seal("G1", "BF right pipe at J1", "BF-R", True),
    seal("G3", "B1a at J2", "B1a", False),
    seal("G4", "B4b at J2", "B4b", True),
    seal("G5", "B6 at J3", "B6", False),
    seal("G6", "B7a at J3", "B7a", False),
    seal("G7", "M3 at the hub", "M3a", True),
    seal("G8", "M2 at the hub", "M2h", True),
    seal("G9", "B9 at the hub", "B9a", False),
    seal("G10", "M4 at the hub", "M4a", True),
    seal("G11", "M6 at the hub", "M6", True),
]

# ---- railings (the race only) along the mid ledge's edge where a fall to the floor would be a free S1: the west side (S1's gate in it,
# in front of M1's mouth) and the south side's west half
EDGE_A, EDGE_B = -(HL - IF), HW - IF
G_HALF = 6.7
def P2(p): return [round(p[0], 2), round(p[2], 2)]
rails = [
    dict(id="R1", points=P2(H(EDGE_A, -EDGE_B, MID)) + P2(H(EDGE_A, T1 - G_HALF, MID)), y=MID, height=1.1),
    dict(id="R2", points=P2(H(EDGE_A, T1 + G_HALF, MID)) + P2(H(EDGE_A, EDGE_B, MID)) + P2(H(-12.0, EDGE_B, MID)), y=MID, height=1.1),
]
# Round 58 (B, Dan BUG-001 at the hub: "it still allows you falll down the pit of you are going to fastt, The gate should extend all th way down"):
# the race runs the whole mid ledge ring and over the span, so every edge of the pit it passes is railed, not only the west and south-west
# where a fall was a free S1: the south side's east half and the east side down to S1's floor ramp (its top comes in at the east side's -b
# end, HUB_RAMP_W wide: left open, it is S1's way back up), the north side either side of the span, and the span's two sides over the chasm
# (their heights follow the span: 'ys')
HUB_RAMP_W, SPAN_HALF = 8.0, 4.5
rails += [
    dict(id="R3", points=P2(H(-12.0, EDGE_B, MID)) + P2(H(-EDGE_A, EDGE_B, MID)) + P2(H(-EDGE_A, -EDGE_B + HUB_RAMP_W, MID)), y=MID, height=1.1),
    dict(id="R4", points=P2(H(EDGE_A, -EDGE_B, MID)) + P2(H(A5 - SPAN_HALF, -EDGE_B, MID)), y=MID, height=1.1),
    dict(id="R5", points=P2(H(A5 + SPAN_HALF, -EDGE_B, MID)) + P2(H(-EDGE_A, -EDGE_B, MID)), y=MID, height=1.1),
]
for side, rid in ((-1, "R6"), (1, "R7")):
    bs = [-EDGE_B + 2.0 * k for k in range(int(2 * EDGE_B / 2.0) + 1)]
    if bs[-1] < EDGE_B: bs.append(EDGE_B)
    rails.append(dict(id=rid, points=[v for b in bs for v in P2(H(A5 + side * SPAN_HALF, b, MID))], y=MID, height=1.1,
                      ys=[round(span_y(b), 2) for b in bs]))


# ---- the switch gates
def wall_plate(tid, from_b_end, s_in, theta_deg, right=True):
    """a plate on a pipe's wall, s_in m in from its b end (from_b_end) or a end, theta degrees round from the bottom on the right of
    the way in"""
    q = pts(tid, rev=from_b_end); s = 0.0
    for a, b in zip(q, q[1:]):
        s += math.dist((a[0], a[2]), (b[0], b[2]))
        if s >= s_in: break
    d = (b[0] - a[0], b[2] - a[2]); n = math.hypot(*d); d = (d[0] / n, d[1] / n); r = (d[1], -d[0]) if right else (-d[1], d[0])
    R = T[tid]["d"] * 0.5; th = math.radians(theta_deg)
    lat, up = R * math.sin(th), R * (1 - math.cos(th))
    p = (b[0] + r[0] * lat, b[1] + up, b[2] + r[1] * lat)
    nrm = (-r[0] * math.sin(th), math.cos(th), -r[1] * math.sin(th))       # inward, toward the axis
    return p, nrm, b, r, lat, up


p1, n1, p1floor, p1r, p1lat, p1up = wall_plate("M5b", True, 60.0, 68.0)
# the floor plates: well off the racing line across the room, reached on a smooth bulge (in a corner a car pinned itself turning back)
p2 = local("J3", 0.0, -6.5)
p3 = local("J1", -1.5, 0.0)
switch_gates = [
    dict(id="SG1", shortcut="S1", label="S1 WATERFALL LEAP",
         gate=dict(id="SG1", label="S1 gate", x=round(H(EDGE_A, T1, MID)[0], 2), y=MID, z=round(H(EDGE_A, T1, MID)[2], 2), dx=round(hu[0], 4), dz=round(hu[1], 4), w=2 * G_HALF, h=1.6),
         sx=round(p1[0], 2), sy=round(p1[1], 2), sz=round(p1[2], 2), sr=0.7, nx=round(n1[0], 4), ny=round(n1[1], 4), nz=round(n1[2], 4)),
    dict(id="SG2", shortcut="S2", label="S2 DIVE PIPE", gate=seal("SG2", "S2 gate", "S2", False),
         sx=round(p2[0], 2), sy=round(p2[1], 2), sz=round(p2[2], 2), sr=1.6, nx=0.0, ny=1.0, nz=0.0),
    dict(id="SG3", shortcut="S3", label="S3 HIGH LINE", gate=seal("SG3", "S3 gate", "S3", False),
         sx=round(p3[0], 2), sy=round(p3[1], 2), sz=round(p3[2], 2), sr=1.6, nx=0.0, ny=1.0, nz=0.0),
]


# ---- the shortcuts (path x, z and path_y; forward only)
def sc_json(sid, name, line, forgives, entry_inset, half, ai_speed=0.0, ai_profile=None, lap_option=False):
    j = dict(id=sid, name=name, direction="forward", path=[round(v, 2) for p in line for v in (p[0], p[2])], path_y=[round(p[1], 2) for p in line],
             half_width_m=half, entry_inset_m=entry_inset, entry_margin_m=1.0, forgives=forgives)
    if ai_speed: j["ai_speed"] = ai_speed
    if ai_profile: j["ai_profile"] = ai_profile
    if lap_option: j["lap_option"] = True
    return j


def sc_len(line): return flat_len(line)


# S1: from 12 m inside M1b, out of its mouth, over the edge (its gate), down to the floor, round the pool's west end, up the floor ramp
m1_end = tail(pts("M1b"), 12.0)
RAMP_FOOT, RAMP_TOP, RAMP_B = (HL - IF) - (MID - FLOOR) / 0.2, HL - IF, -(HW - IF) + 4.0
s1 = m1_end + [H(-44, T1, MID), H(EDGE_A + 0.5, T1, MID), H(-33, T1 - 0.2, FLOOR + 4.0), H(-27, T1 - 0.6, FLOOR), H(-22, 9.5, FLOOR),
               H(-19.5, 3, FLOOR), H(-19.5, -4, FLOOR), H(-17, -10.5, FLOOR), H(RAMP_FOOT - 1.0, RAMP_B, FLOOR)]
s1 += [H(a, RAMP_B, FLOOR + (MID - FLOOR) * (a - RAMP_FOOT) / (RAMP_TOP - RAMP_FOOT)) for a in range(int(RAMP_FOOT) + 4, int(RAMP_TOP), 4)]
s1 += [H(RAMP_TOP + 0.5, RAMP_B, MID), H(42.0, RAMP_B + 1.5, MID), H(43.5, RAMP_B + 4.0, MID)]
s1_inset = sc_len(m1_end) + 9.0

# S2: the last 10 m of M5d into J15, round J15 on a 9 m arc from M5d's opening to S2's (they are on walls at right angles: through
# the room's middle a car met the wall beside S2's opening), the dive pipe, J14, the first 10 m of M5a
o_in, o_s2 = opening("J15", "M5d"), opening("J15", "S2")
arc = [local("J15", 9.0 - 9.0 * math.sin(math.radians(a)), -9.0 + 9.0 * math.cos(math.radians(a))) for a in range(0, 91, 10)]
s2_in = tail(pts("M5d", rev=True), 10.0) + arc
s2 = s2_in + every(pts("S2"), 2.0) + [centre("J14")] + head(every(pts("M5a", rev=True), 2.0), 10.0)
s2_inset = sc_len(s2_in) + 6.0

# S3: the last 10 m of M5a into J1, the high line, the jump across J2 (out of its high opening, down onto its floor), into M1b
# round J1 from M5a's opening (+v wall, u 5.3) to S3's (+u wall): a quarter turn (quadratic) instead of through the middle
o3 = opening("J1", "S3")
# the lap runs 3.7 m from S3's wall here: swing out toward the room's middle first, then a 5 m arc into S3 (straight on, the AI went on
# into M1a's mouth across the room)
arc3 = [local("J1", 8.5 + 5.0 * math.cos(math.radians(a)), o3["t"] + 5.0 + 5.0 * math.sin(math.radians(a))) for a in range(180, 271, 15)]
s3_in = tail(every(pts("M5a", rev=True), 2.0), 10.0) + [local("J1", 5.3, 8.5), local("J1", 4.4, 7.0)] + arc3
s3_pipe = every(pts("S3"), 2.0)
hi = s3_pipe[-1]; j2 = centre("J2")
m1b0 = pts("M1b")[0]
land = (hi[0] + (m1b0[0] - hi[0]) * 0.55, j2[1], hi[2] + (m1b0[2] - hi[2]) * 0.55)
s3 = s3_in + s3_pipe + [land, m1b0] + head(every(pts("M1b"), 2.0), 10.0)[1:]
s3_inset = sc_len(s3_in) + 6.0

# the plates' detours (lap options: forgive nothing; the AI may take them, by ShortcutStrategy)
m5b_in = every(pts("M5b", rev=True), 1.0)
def at_s(line, s_want):
    s = 0.0
    for i in range(1, len(line)):
        s += math.dist((line[i][0], line[i][2]), (line[i - 1][0], line[i - 1][2]))
        if s >= s_want: return i
    return len(line) - 1
i0, i1 = at_s(m5b_in, 45.0), at_s(m5b_in, 75.0)
p1line = []
for i in range(i0, i1 + 1, 2):
    q = m5b_in[i]; k = 1.0 - abs((i - (i0 + i1) / 2) / ((i1 - i0) / 2)); k = k * k * (3 - 2 * k)
    lat = 1.7 * k; p1line.append((q[0] + p1r[0] * lat, q[1] + p1up * k * 0.8, q[2] + p1r[1] * lat))
# J3: in from M5b on its +u wall, out to M5d on its -u wall; the plate 6.5 m to the -v side of the line
p2line = [tail(pts("M5b", rev=True), 9.0)[0]] + bulge("J3", (8.0, -0.5), (0.0, -6.5), (-8.0, -2.0)) + [head(pts("M5d", rev=True), 9.0)[-1]]
# J1: in from M5a on its +v wall (u 5.3), out to M1a on its -v wall (u 5.3); the plate 6.8 m to the -u side of the line
p3line = [tail(every(pts("M5a", rev=True), 2.0), 9.0)[0]] + bulge("J1", (5.3, 8.0), (-1.5, 0.0), (5.3, -8.0)) + [head(pts("M1a"), 9.0)[-1]]

shortcuts = [
    sc_json("S1", "S1 Waterfall leap", s1, [5], round(s1_inset, 1), 6.0, 17.0, [0, 15.0, round(s1_inset - 6, 1), 17.0, round(s1_inset + 20, 1), 12.0, round(s1_inset + 60, 1), 15.0]),
    sc_json("S2", "S2 Dive pipe", s2, [2], round(s2_inset, 1), 2.6, 15.0, [0, 11.0, 8.0, 7.0, round(s2_inset, 1), 7.0, round(s2_inset + 12, 1), 15.0]),
    sc_json("S3", "S3 High line", s3, [3, 4], round(s3_inset, 1), 2.6, 17.0, [0, 9.0, 8.0, 6.0, round(s3_inset + 2, 1), 6.0, round(s3_inset + 14, 1), 17.0]),
    sc_json("P1", "S1's switch (up M5's wall)", p1line, [], 3.0, 2.6, 13.0, lap_option=True),
    sc_json("P2", "S2's switch (J3's far side)", p2line, [], 3.0, 3.0, 6.0, lap_option=True),
    sc_json("P3", "S3's switch (J1's far side)", p3line, [], 3.0, 3.0, 6.0, lap_option=True),
]

# ---- the AI's held speeds: the turn out of M1's mouth through the mid ledge's corner, the ledge's corners, the turn onto the span
zones = []
def zone(zid, s0, s1, v): zones.append(dict(id=zid, s0=round(max(0.0, s0), 1), s1=round(min(L, s1), 1), speed=v))
s_m1 = s_near(H(-48, T1, MID), after=cps[3]["s"])
zone("out of M1", s_m1 - 18, s_m1 + 16, 9.0)
zone("south-east corner", s_near(H(42.8, 20, MID), after=s_m1) - 8, s_near(H(42.8, 20, MID), after=s_m1) + 6, 11.0)
zone("north-east corner", s_near(H(42.8, -20, MID), after=s_m1) - 8, s_near(H(42.8, -20, MID), after=s_m1) + 6, 11.0)
s_foot = s_near(HM(23.2, -21.0), after=s_m1)
zone("onto the span", s_foot - 14, s_foot + 6, 8.5)
zone("J3", s_near(centre("J3"), after=5) - 10, s_near(centre("J3"), after=5) + 10, 15.0)
zone("J1", s_near(centre("J1"), after=5) - 10, s_near(centre("J1"), after=5) + 10, 13.0)

# ---- Round 35 (Dan: "The finish line needs to be on a straight stretch"): the line (the start: one line) 95 m into M5b from the hub,
# on its 80 m straight (33 to 114 m in): 62 m of straight pipe before it, past S1's plate (45 to 75 m in, ridden on the wall), 19 m
# after it. The loop itself is unchanged (the same lap, cut at gate_s): the span, the top ledge and the S-bend into the pipe are run-in.
LINE_IN_M5B = 95.0
GATE_S = s_near(head(pts("M5b", rev=True), LINE_IN_M5B)[-1], after=5.0)

# ---- the grid: on the mid ledge's north side west of the span (the lap comes along it from the east and turns onto the span: west of
# the span it is off the lap), facing east, then right onto the span and up it to the line
NB = -(HW - (IM + IF) / 2.0)
lead = [H(-30, NB, MID), H(10, NB, MID), H(15.5, NB + 0.6, MID), HM(18.8, NB + 3.6), HM(A5, -14.0), HM(A5, GATE_B)]
# Round 35: finished AI cars drive on down M5b (at 14 m/s: a slow car in the 5 m pipe held up the racers behind) into J3 and park along
# its +v wall, off the lap (in from M5b at v -0.4, out to M5d at v -2.1; S2's plate is on the -v side), 6 m apart
park = every(pts("M5b", rev=True), 4.0)
park = [q for q in park if s_near(q, after=5.0) > GATE_S + 6.0] + [local("J3", 6.0, 3.0), local("J3", 3.0, 6.0), local("J3", -7.0, 6.0)]
route = dict(
    name="Underground", direction="forward", directions=["forward"], laps=3, underground=True, trail=True,
    source="Tools/mapgen/make_underground_route.py from Data/pipes/network.json. Coordinates are map meters (= Unity x, z); heights are "
           "the route's own (loop_y, lead_in_y, path_y): the network's floors and the hub's levels.",
    lap_length_m=round(L, 1), gate_s=round(GATE_S, 1), gate_half_width_m=2.2, gate_arch_m=4.0, gate_ring_m=T["M5b"]["d"] * 0.5, checkpoint_half_width_m=2.2, gate_below_m=4.0, gate_above_m=6.0,
    reverse_grid_back_m=30.0, ai_half_width_m=1.6, ai_lane_m=0.6, ai_pass_lane_m=1.2,
    loop=[round(v, 2) for p in lap for v in (p[0], p[2])], loop_y=[round(p[1], 2) for p in lap],
    lead_in=[round(v, 2) for p in lead for v in (p[0], p[2])], lead_in_y=[round(p[1], 2) for p in lead], lead_in_length_m=round(flat_len(lead), 1),
    checkpoints=cps, junctions=[], loop_pieces=[], official_shortcuts=shortcuts, closures=[], ai_zones=zones,
    seals=seals, rails=rails, switch_gates=switch_gates,
    finish_park=[round(v, 2) for p in park for v in (p[0], p[2])], finish_park_y=[round(p[1], 2) for p in park],
    finish_park_gap_m=6.0, finish_park_speed=14.0,
)
json.dump(route, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"wrote {OUT}: line at {GATE_S:.1f} m; lap {L:.0f} m ({len(lap)} points), {len(cps)} checkpoints at " + ", ".join(f"{c['label']} {c['s']:.0f} m" for c in cps)
      + f"; {len(seals)} seals; {len(rails)} rails; AI zones " + ", ".join(f"{z['id']} {z['s0']:.0f}-{z['s1']:.0f} at {z['speed']}" for z in zones))
for sc in shortcuts:
    n = len(sc["path"]) // 2; line = [(sc["path"][2 * i], sc["path_y"][i], sc["path"][2 * i + 1]) for i in range(n)]
    a, b = line[0], line[-1]; sa, sb = s_near(a, vy=True), s_near(b, vy=True)
    lap_part = (sb - sa) % L
    print(f"  {sc['id']} {sc['name']}: {flat_len(line):.0f} m against {lap_part:.0f} m of the lap ({lap_part - flat_len(line):+.0f} m), forgives {sc['forgives']}")
