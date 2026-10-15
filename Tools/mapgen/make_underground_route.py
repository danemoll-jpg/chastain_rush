"""Round 27 (Part D): the UNDERGROUND RACE (Dan's vision, Round 23: "a route through the network (through the hub) raced from the menu
with the AI, its own checkpoints. During that race, GATES (barred grilles / sluice gates) seal off the rest of the maze"). Writes
Data/routes/underground.json from Data/pipes/network.json (re-run after pipe_network.py, then Build Map).

The lap, on the network as built (the proposal's loop, driven the way the hub's ramps let it flow; the hub's ramps climb floor to mid
along the north side toward the east and mid to top along the south side toward the west, so the way round is up, never down a ramp
into the racers coming up it):
  the hub's TOP LEDGE, south side (start / finish) -> M5's mouth -> M5b -> J3 -> M5a along the Enclave pond (glass under W4) -> J1 ->
  M1a (glass under W1) -> J2 -> M1b -> out onto the hub's MID LEDGE at the west end -> round the mid ledge (north, east, south) -> up the
  mid-to-top ramp -> round the corner onto the top ledge -> the line.
Nothing on the hub's ledges' edges (as built: "falling off is part of the fun"): a fall to the floor costs the climb back up the floor
ramp, which comes out on the mid ledge's east side (no faster than the ledge). Checkpoints: J3, the glass under W4, J1, the glass under
W1, the mid ledge's east side. Seals: every pipe the race does not use where it opens into J1, J2, J3 and the hub (G1 to G11 of the
proposal). Vehicles: all (the mains are 5 m). Forward only. Heights are the route's own (loop_y): the network's floors and the hub's
levels. No switch shortcuts: the proposal's S1 to S3 do not fit the network as built (see TODO.md, Round 27 Part D).

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
hu = (math.sin(math.radians(hub["yaw"])), math.cos(math.radians(hub["yaw"])))
hv = (hu[1], -hu[0])


def H(a, b, y):
    """a point of the hub: a along its length (east), b across it (south), at height y"""
    return (hub["x"] + hu[0] * a + hv[0] * b, y, hub["z"] + hu[1] * a + hv[1] * b)


def pts(tid, rev=False):
    p = T[tid]["pts"]; q = [(p[i], p[i + 1], p[i + 2]) for i in range(0, len(p), 3)]
    return list(reversed(q)) if rev else q


def centre(cid):
    c = C[cid]; return (c["x"], c["floor"], c["z"])


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


# ---- the lap
gate = H(-25.0, 31.5, TOP)
top_to_mouth = dense([gate, H(0, 31.5, TOP), H(12, 31.5, TOP), H(17, 32.3, TOP), H(19.6, 33.8, TOP)])
mouth_m5 = pts("M5b", rev=True)                                         # the hub's top ledge back to J3
lap = top_to_mouth + every(mouth_m5, 2.0)
lap += [centre("J3")]
lap += every(pts("M5a", rev=True), 2.0)                                # J3 to J1 along the Enclave pond
lap += [centre("J1")]
lap += every(pts("M1a"), 2.0)                                          # J1 to J2 under the north pond
lap += [centre("J2")]
lap += every(pts("M1b"), 2.0)                                          # J2 to the hub's mid ledge, west end
mid = [H(-45, 12.0, MID), H(-43.5, 8, MID), H(-43.5, -14, MID), H(-42, -21, MID), H(-38, -23.5, MID), H(38, -23.5, MID), H(42, -21, MID),
       H(43.5, -14, MID), H(43.5, 14, MID), H(42, 21, MID), H(38, 23.5, MID), H(10, 23.7, MID), H(2, 24, MID)]
ramp = [H(2, 24, MID), H(-48, 24, TOP)]                                # the mid-to-top ramp, up toward the west (20 %)
corner = [H(-50.8, 25.2, TOP), H(-51.8, 28.6, TOP), H(-50, 31.3, TOP), H(-44, 31.5, TOP), H(-35, 31.5, TOP)]
lap += dense(mid) + dense(ramp)[1:] + dense(corner)
lap += [gate]                                                          # closed: the last point is the first


def flat_len(line):
    return sum(math.dist((a[0], a[2]), (b[0], b[2])) for a, b in zip(line, line[1:]))


cum = [0.0]
for a, b in zip(lap, lap[1:]): cum.append(cum[-1] + math.dist((a[0], a[2]), (b[0], b[2])))
L = cum[-1]


def s_near(p, after=0.0):
    best, bs = 1e9, 0.0
    for i, q in enumerate(lap):
        if cum[i] < after: continue
        d = math.dist((p[0], p[2]), (q[0], q[2]))
        if d < best: best, bs = d, cum[i]
    return bs


def glass_mid(tid):
    t = T[tid]; g = t["glass"][0]; q = pts(tid); s = 0.0; target = (g["s0"] + g["s1"]) * 0.5
    for a, b in zip(q, q[1:]):
        s += math.dist((a[0], a[2]), (b[0], b[2]))
        if s >= target: return b
    return q[-1]


s_m5mouth = s_near(H(20.4, 35, TOP))
cps = []
for cid, label, at, half, arch in [
    (1, "J3 ENCLAVE JCT", centre("J3"), 7.5, 5.5),
    (2, "GLASS UNDER W4", glass_mid("M5a"), 2.2, 4.0),
    (3, "J1 SOUTH FORK", centre("J1"), 7.5, 5.5),
    (4, "GLASS UNDER W1", glass_mid("M1a"), 2.2, 4.0),
    (5, "HUB MID LEDGE", H(43.5, 0, MID), 4.3, 6.0)]:
    s = s_near(at, after=s_m5mouth if cid == 1 else (cps[-1]["s"] if cps else 0.0))
    cps.append(dict(id=cid, s=round(s, 1), pos=[round(at[0], 2), round(at[2], 2)], label=label, half_m=half, arch_m=arch))


# ---- the seals: every pipe the race does not use, where it opens into J1, J2, J3 and the hub
def seal(sid, label, tid, at_b):
    t = T[tid]; q = pts(tid); end = q[-1] if at_b else q[0]; nxt = q[-3] if at_b else q[2]
    dx, dz = nxt[0] - end[0], nxt[2] - end[2]; n = math.hypot(dx, dz); dx, dz = dx / n, dz / n
    inside = (end[0] + dx * 0.8, end[1], end[2] + dz * 0.8)
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

# ---- the AI's held speeds: slow for the turn out of the line onto M5's mouth, the turn off M1b onto the mid ledge, and the ramp's
# top corner onto the top ledge
zones = []
def zone(zid, s0, s1, v): zones.append(dict(id=zid, s0=round(max(0.0, s0), 1), s1=round(min(L, s1), 1), speed=v))
zone("M5 mouth", s_m5mouth - 14, s_m5mouth + 8, 9.0)
s_m1b_out = s_near(H(-45, 12.0, MID), after=s_m5mouth + 200)
zone("off M1b", s_m1b_out - 16, s_m1b_out + 14, 9.0)
s_ramp_top = s_near(H(-48, 24, TOP), after=s_m1b_out)
zone("ramp top corner", s_ramp_top - 8, s_ramp_top + 18, 6.5)
zone("J3", s_near(centre("J3"), after=s_m5mouth) - 10, s_near(centre("J3"), after=s_m5mouth) + 10, 15.0)
zone("J1", s_near(centre("J1"), after=s_m5mouth) - 10, s_near(centre("J1"), after=s_m5mouth) + 10, 13.0)

lead = [H(-53, 31.5, TOP), gate]
route = dict(
    name="Underground", direction="forward", directions=["forward"], laps=3, underground=True, trail=True,
    source="Tools/mapgen/make_underground_route.py from Data/pipes/network.json. Coordinates are map meters (= Unity x, z); heights are "
           "the route's own (loop_y, lead_in_y): the network's floors and the hub's levels.",
    lap_length_m=round(L, 1), gate_s=0.0, gate_half_width_m=3.4, gate_arch_m=5.0, checkpoint_half_width_m=2.2, gate_below_m=4.0, gate_above_m=6.0,
    reverse_grid_back_m=30.0, ai_half_width_m=1.6, ai_lane_m=0.6, ai_pass_lane_m=1.2,
    loop=[round(v, 2) for p in lap for v in (p[0], p[2])], loop_y=[round(p[1], 2) for p in lap],
    lead_in=[round(v, 2) for p in lead for v in (p[0], p[2])], lead_in_y=[round(p[1], 2) for p in lead], lead_in_length_m=round(flat_len(lead), 1),
    checkpoints=cps, junctions=[], loop_pieces=[], official_shortcuts=[], closures=[], ai_zones=zones,
    seals=seals, rails=[], switch_gates=[],
)
json.dump(route, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"wrote {OUT}: lap {L:.0f} m ({len(lap)} points), {len(cps)} checkpoints at " + ", ".join(f"{c['label']} {c['s']:.0f} m" for c in cps)
      + f"; {len(seals)} seals; AI zones " + ", ".join(f"{z['id']} {z['s0']:.0f}-{z['s1']:.0f} at {z['speed']}" for z in zones))
