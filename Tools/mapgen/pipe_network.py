"""Round 23 follow-ups: the underground pipe network AS BUILT (Part B and Part C), the data the map build reads.

Dan (2026-10-13 01:01): "I think go ahead and build it out (maybe wait on the race) so that I can get a better feel. I am not
that worried about it since we can always add other branches as needed." The layout follows Docs/PIPE-NETWORK-PROPOSAL.md
(Tools/mapgen/pipe_network_proposal.py), changed where Dan asked (Round 23 follow-ups, items 5 and 6: TWO HOLES = TWO
DIRECTIONS) and where the ground or the build needs it; every change is listed in DIFFERS below and in the round's report.

The network is world, not race (CLAUDE.md, "Build the world to be reused"): pipes, chambers, the hub, exits, manholes. No
race route, gates or switches here.

  entrances  BF (Bells Ferry, Data/pipes/pipes.json) and GP (the Chastain Glen pond culvert): twin 5 m pipes whose two barrels
             split just past the headwall and go off in two directions, each its own pipe into the network
  tubes      round pipes (5 m mains with a color stripe each; 3.5 m maze branches, unmarked; a few 5 m branches), each from an
             end to an end: a chamber's wall, an entrance barrel, a manhole riser, an exit or outfall cut (built from the same
             parts as P1: Data/pipes/pipes.json style 'open_b' pipes), or a dead end (a small turning chamber)
  chambers   junctions (a concrete room, every branch point is one), drop shafts (one pipe enters high in the wall), the hub
             (the chasm: ledges, ramps, waterfalls, the pool)
  glass      where a main runs under a pond, the pond is dug deep round it and the pipe is a glass tube (as P1)
  flooded    a sag in the floor that holds water (it slows you; never a trap)

Every tube has a 'stage' (1 to 4; 0 = Part B, the entrances); the map build builds what is at or under 'build_stage' and
ends a built chamber's opening onto a pipe not built yet with a short stub and a closed, lit grille.

Writes Data/pipes/network.json (and local/round23f/network_built.png, git-ignored). Re-run after mapgen (it reads
Assets/Generated/terrain.r16), then Build Map.

Usage: python Tools/mapgen/pipe_network.py [--stage N] [--plot]
"""
import argparse, json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/pipes/network.json")
OUT_PNG = os.path.join(ROOT, "local/round23f/network_built.png")

meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
MW, MD = M["size_x_m"], M["size_z_m"]
PIPES = {p["id"]: p for p in json.load(open(os.path.join(ROOT, "Data/pipes/pipes.json"), encoding="utf-8"))["pipes"]}


def ground(x, z):
    x = min(max(x, 0.0), MW - 1.0); z = min(max(z, 0.0), MD - 1.0)
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)


def P2(a): return [(a[i], a[i + 1]) for i in range(0, len(a), 2)]


def in_poly(p, poly):
    x, z = p; c = False; n = len(poly)
    for i in range(n):
        x1, z1 = poly[i]; x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z) and x < x1 + (z - z1) * (x2 - x1) / (z2 - z1): c = not c
    return c


def seg_dist(p, a, b):
    ax, az = a; bx, bz = b; px, pz = p
    dx, dz = bx - ax, bz - az; L2 = dx * dx + dz * dz
    t = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / L2))
    return math.hypot(ax + dx * t - px, az + dz * t - pz), (ax + dx * t, az + dz * t), t


ROADS = [(r["name"] or "", r["class"], r["width"], P2(r["points"])) for r in M["roads"]]
WATER = [P2(w["outline"]) for w in M["water"]]
STREAMS = [P2(s["points"]) for s in M["streams"]]


def pond_level(wi):
    """the lidar level of a pond (the median ground inside it: the map build's WaterLevel) + 0.15 (PondLift)"""
    ring = WATER[wi]; xs = [x for x, z in ring]; zs = [z for x, z in ring]; s = []
    for x in np.arange(min(xs), max(xs), 2.0):
        for z in np.arange(min(zs), max(zs), 2.0):
            if in_poly((x, z), ring): s.append(ground(x, z))
    s.sort(); return s[len(s) // 2] + 0.15


def road_point(name, near):
    """the nearest point on the named road's centerline and its direction there"""
    best = (1e9, None, None)
    for n, _, _, p in ROADS:
        if n != name: continue
        for k in range(len(p) - 1):
            d, q, _ = seg_dist(near, p[k], p[k + 1])
            if d < best[0]:
                dx, dz = p[k + 1][0] - p[k][0], p[k + 1][1] - p[k][1]; L = math.hypot(dx, dz)
                best = (d, q, (dx / L, dz / L))
    if best[1] is None: raise SystemExit(f"no road {name}")
    return best[1], best[2]


def norm(v): L = math.hypot(v[0], v[1]); return (v[0] / L, v[1] / L)
def add(a, b, k=1.0): return (a[0] + b[0] * k, a[1] + b[1] * k)
def sub(a, b): return (a[0] - b[0], a[1] - b[1])
def dot(a, b): return a[0] * b[0] + a[1] * b[1]
def hdg(v): return math.degrees(math.atan2(v[0], v[1])) % 360.0
def from_hdg(h): r = math.radians(h); return (math.sin(r), math.cos(r))


# ================================================================ the layout
MAIN, NARROW = 5.0, 3.5
STRIPES = {"M1": (0.90, 0.16, 0.16), "M5": (1.0, 0.55, 0.05), "M3": (0.15, 0.75, 0.25), "M4": (1.0, 0.85, 0.10), "M2": (0.18, 0.45, 1.0), "M6": (0.62, 0.28, 0.95)}

# the hub: a chasm 110 x 70 m; its long axis east (yaw 90). Ledges round the walls: the top ledge 7 m wide, the mid ledge 9 m more
HUB = dict(c=(1900.0, 1145.0), yaw=90.0, hl=55.0, hw=35.0, levels=dict(floor=252.0, mid=262.0, top=272.0), insets=dict(top=0.0, mid=7.0, floor=16.0))

# chambers: kind junction (a room), shaft (a drop: 'drop' m from its high opening to its floor), turn (a dead end's turning
# room), hub. size (length along yaw, width, height); floor None = as deep as the ground over it needs (1.5 m cover)
J5M = (18.0, 18.0, 7.0)     # a junction on the mains
J35 = (12.0, 12.0, 5.5)     # a junction of 3.5 m branches only
TURN = (9.0, 9.0, 5.0)
CH = {
    # Part B: the two entrances' far ends
    "J1":  dict(name="J1 SOUTH FORK", kind="junction", at=(1496.0, 1148.0), yaw=123.0, size=J5M, stage=0, sign="M1 TRUNK  /  M5 ENCLAVE MAIN"),
    "J10": dict(name="J10 NORTH FORK", kind="junction", at=(1452.0, 1256.0), yaw=45.0, size=J5M, stage=0, sign="M2 LAKES MAIN"),
    "J12": dict(name="J12 GLEN NORTH", kind="junction", at=(2966.0, 908.0), yaw=10.0, size=J5M, stage=0, sign="M4 GLEN MAIN"),
    "J13": dict(name="J13 GLEN SOUTH", kind="junction", at=(2908.0, 704.0), yaw=200.0, size=J5M, stage=0),
    # stage 1: the trunk, the Enclave, the hub, the north and Enclave mazes
    # Round 28 (B1, S3 "High line"): J2 is 3 m taller and 2 m wider, with a high opening (2.5 m up its west wall) that S3 comes in at
    "J2":  dict(name="J2 POND JCT", kind="junction", at=(1628.0, 1096.0), yaw=95.0, size=(18.0, 20.0, 10.0), drop=2.5, stage=1, sign="M1 TRUNK"),
    "J3":  dict(name="J3 ENCLAVE JCT", kind="junction", at=(1785.0, 1000.0), yaw=45.0, size=J5M, stage=1, sign="M5 ENCLAVE MAIN"),
    # Round 28 (B1, S2 "Dive pipe"): two junctions on M5 where the dive pipe leaves and rejoins it, at M5's own floor (M5 kept as it was);
    # J15 at M5's station 430 (the proposal's 389 was 2 m from B8; here 30 m clear of it)
    "J14": dict(name="J14 POND WEST", kind="junction", at=(1601.4, 999.9), yaw=97.4, size=J5M, stage=1, sign="M5 ENCLAVE MAIN", floor=274.3),
    "J15": dict(name="J15 POND EAST", kind="junction", at=(1698.0, 919.5), yaw=42.6, size=J5M, stage=1, sign="M5 ENCLAVE MAIN", floor=273.2),
    "HUB": dict(name="THE HUB", kind="hub", at=HUB["c"], yaw=HUB["yaw"], stage=1),
    "K1":  dict(name="K1", kind="junction", at=(1512.0, 1246.0), yaw=0.0, size=J5M, stage=1, sign="M2 LAKES MAIN"),
    "K2":  dict(name="K2", kind="junction", at=(1585.0, 1214.0), yaw=0.0, size=J5M, stage=1, sign="M2 LAKES MAIN"),
    "K3":  dict(name="K3", kind="junction", at=(1700.0, 1230.0), yaw=0.0, size=J5M, stage=1, sign="M2 LAKES MAIN"),
    "S4":  dict(name="S4 DROP SHAFT", kind="shaft", at=(1660.0, 1166.0), yaw=0.0, size=(12.0, 12.0, 15.0), drop=8.0, stage=1),
    "D5":  dict(name="D5", kind="turn", at=(1608.0, 1180.0), yaw=90.0, size=TURN, stage=1),
    "K7":  dict(name="K7", kind="junction", at=(1720.0, 890.0), yaw=90.0, size=J35, stage=1),
    "K8":  dict(name="K8", kind="junction", at=(1985.0, 1010.0), yaw=0.0, size=J35, stage=1),
    "D10": dict(name="D10", kind="turn", at=(2060.0, 1060.0), yaw=60.0, size=TURN, stage=1),
    # stage 2: Noonday
    "J4":  dict(name="J4 NOONDAY JCT", kind="junction", at=(2280.0, 1215.0), yaw=75.0, size=J5M, stage=2, sign="M3 NOONDAY MAIN"),
    "J5":  dict(name="J5 LAKE JCT", kind="junction", at=(2350.0, 1352.0), yaw=20.0, size=J5M, stage=2, sign="M3 NOONDAY MAIN"),
    "K15": dict(name="K15", kind="junction", at=(2472.0, 1562.0), yaw=0.0, size=J5M, stage=2, sign="M3 NOONDAY MAIN"),
    # stage 3: the Glen and the shops
    "J6":  dict(name="J6 BRIDGE JCT", kind="junction", at=(2384.0, 884.0), yaw=110.0, size=J5M, stage=3, sign="M4 GLEN MAIN"),
    "J7":  dict(name="J7 GLEN JCT", kind="junction", at=(2722.0, 975.0), yaw=90.0, size=J5M, stage=3, sign="M4 GLEN MAIN"),
    "J8":  dict(name="J8 SHOPS JCT", kind="junction", at=(3194.0, 1204.0), yaw=45.0, size=J5M, stage=3, sign="M4 GLEN MAIN"),
    "K19": dict(name="K19", kind="junction", at=(2700.0, 560.0), yaw=0.0, size=J35, stage=3),
    # stage 4: the lakes
    "J9":  dict(name="J9 LAKES JCT", kind="junction", at=(868.0, 1236.0), yaw=110.0, size=J5M, stage=4, sign="M2 LAKES MAIN", floor=284.0),
    "K25": dict(name="K25", kind="junction", at=(560.0, 1418.0), yaw=110.0, size=J5M, stage=4, sign="M2 LAKES MAIN"),
}

# exits (secret ways out: a small cut and headwall in a bank; a way in too), outfalls (the same, a lip at the mouth, out over
# the water), manholes (pop out of the street). 'at' is the mouth (exits) or the street point (manholes, snapped onto it)
EXITS = {
    "X_W20":   dict(name="EXIT W20 POND", at=(1373.5, 800.0)),
    # Round 24 A2 (BUG-002, Dan: "this border should not go in the middle of the road"): the creek-trail exit's mouth just west
    # of the trail (its tread 2.25 m either side of the line, plus a meter), its cut going straight away from the trail (it ran
    # along the trail before, its wall and railing across the tread)
    "X_TRAIL": dict(name="EXIT CREEK TRAIL", at=(2338.7, 1111.7)),
    "X_W12":   dict(name="EXIT W12 POND", at=(2497.0, 1443.0)),
    "X_W7":    dict(name="EXIT W7 POND", at=(2446.0, 1700.0)),
    "X_BRIDGE": dict(name="EXIT UNDER BRIDGES", at=(2422.5, 780.0)),   # Round 24 A2: west of the trail under the bridges (its cut was on the trail's line)
    "X_W11":   dict(name="EXIT W11 POND", at=(2686.0, 429.5)),   # Round 24 A2 (Dan's BUG-003 on Joel Drive): north of the road, clear of its curb, between the houses (its cut crossed the road)
    "X_SHOPS": dict(name="EXIT BEHIND SHOPS", at=(3153.0, 1256.0)),
    "X_LNC":   dict(name="EXIT LITTLE NOONDAY", at=(3752.0, 1468.0)),
    "X_STREAM": dict(name="EXIT LAKE STREAM", at=(470.0, 1425.0)),
    "X_W5":    dict(name="EXIT FAR WEST POND", at=(182.0, 1488.0)),
    "X_NELSON": dict(name="EXIT NELSON LAKE", at=(262.0, 118.0)),
    # Round 24 (Dan, 2026-10-13: "restore the property completely for now and worry about where that pipe gets rerouted later"):
    # L_W8, the launch over the Chastain Glen pond, is out with its pipe B18 until Dan picks a new place
    "L_W15":   dict(name="LAUNCH BIG LAKE", at=(2394.0, 1528.0), outfall=15),
}
MANHOLES = {
    "MH1": ("Parkwood Drive", (1548.0, 1278.0)), "MH2": ("Parkwood Drive", (1760.0, 1276.0)), "MH3": ("Bloombridge Way", (1880.0, 955.0)),
    "MH4": ("Rotherhithe Landing", (1700.0, 845.0)), "MH5": ("Bloombridge Way", (2010.0, 930.0)), "MH6": ("Guidhall Trail", (2520.0, 1540.0)),
    "MH7": ("Westminister Way", (2700.0, 1260.0)), "MH8": ("Paxton Court Northeast", (2124.0, 620.0)), "MH9": ("Grasmere Court Northeast", (2730.0, 800.0)),
    "MH10": ("Weybourne Court Northeast", (2690.0, 650.0)), "MH11": ("Chasefield Way", (3000.0, 1120.0)), "MH12": ("Laurel Green Court Northwest", (815.0, 1160.0)),
    "MH13": ("Chastain Lakes Drive Northwest", (960.0, 980.0)),
}

# tubes: (id, name, diameter, main?, stage, end a, way points, end b, extras). Ends: ("ch", id[, level]), ("pipe", entrance id,
# +1 left / -1 right barrel), ("mh", id), ("x", exit id). 'glass': pond indices it runs under in glass. 'flood': (x, z) of a
# flooded sag on it.
M6_WAY = [(330.0, 180.0), (620.0, 250.0), (1000.0, 330.0), (1450.0, 470.0), (1790.0, 760.0), (1840.0, 1000.0)]


def T(id, name, d, main, stage, a, way, b, glass=(), flood=None, stripe=None, under=(), max_grade=None):
    return dict(id=id, name=name, d=d, main=main, stage=stage, a=a, way=way, b=b, glass=list(glass), flood=flood, stripe=stripe, under=list(under), max_grade=max_grade)

TUBES = [
    # ---- Part B: the two entrances, each barrel its own direction
    T("BF-R", "BF right pipe (to J1, the trunk and the Enclave main)", MAIN, False, 0, ("pipe", "BF", -1), [(1446.0, 1200.0), (1468.0, 1178.0)], ("ch", "J1")),
    T("BF-L", "BF left pipe (to J10, the lakes main and the north maze)", MAIN, False, 0, ("pipe", "BF", 1), [(1434.0, 1222.0)], ("ch", "J10")),
    T("GP-L", "Glen pond left pipe (north, to J12 and the Glen main)", MAIN, False, 0, ("pipe", "GP", 1), [(2962.0, 811.0), (2968.0, 862.0)], ("ch", "J12")),
    T("GP-R", "Glen pond right pipe (south, under New Chastain Road to J13)", MAIN, False, 0, ("pipe", "GP", -1), [(2962.0, 800.0), (2958.0, 768.0), (2938.0, 742.0), (2922.0, 728.0)], ("ch", "J13")),
    # ---- stage 1
    T("M1a", "M1 Trunk (J1 to J2, glass under the pond)", MAIN, True, 1, ("ch", "J1"), [(1540.0, 1162.0), (1600.0, 1104.0)], ("ch", "J2"), glass=[1], stripe="M1"),
    T("M1b", "M1 Trunk (J2 to the hub)", MAIN, True, 1, ("ch", "J2"), [(1700.0, 1096.0), (1770.0, 1112.0)], ("ch", "HUB", "mid"), stripe="M1"),
    # Round 28 (B1, S2): M5a in three lengths, through J14 and J15 (its line as before)
    T("M5a", "M5 Enclave Main (J1 to J14)", MAIN, True, 1, ("ch", "J1"), [(1500.0, 1100.0), (1506.0, 1036.0), (1570.0, 1004.0)], ("ch", "J14"), stripe="M5"),
    T("M5c", "M5 Enclave Main (J14 to J15, glass along the pond)", MAIN, True, 1, ("ch", "J14"), [(1647.0, 994.0), (1645.0, 962.0), (1635.0, 922.0), (1625.0, 895.0), (1662.0, 880.0)], ("ch", "J15"), glass=[4], stripe="M5"),
    T("M5d", "M5 Enclave Main (J15 to J3)", MAIN, True, 1, ("ch", "J15"), [(1722.0, 945.0)], ("ch", "J3"), stripe="M5"),
    # Round 28 (B1): the underground race's switch shortcuts, world pipes like any other (open in free roam; the race's gates are the race's)
    #   S2 "Dive pipe": J15 to J14 straight, diving under M5 where it crosses it, a flooded sump at the bottom
    #   S3 "High line": J1 straight to J2, in high on J2's west wall (a jump across J2's floor), lined up with M1b's opening opposite
    T("S2", "S2 Dive pipe (J15 to J14, under M5)", MAIN, False, 1, ("ch", "J15"), [], ("ch", "J14"), under=["M5c"], flood="low", max_grade=0.20),
    T("S3", "S3 High line (J1 to J2, in high over J2's floor)", MAIN, False, 1, ("ch", "J1"), [(1570.0, 1095.0)], ("ch", "J2", "high")),
    T("M5b", "M5 Enclave Main (J3 to the hub)", MAIN, True, 1, ("ch", "J3"), [(1840.0, 1060.0), (1935.0, 1085.0)], ("ch", "HUB", "top"), stripe="M5"),
    T("B1a", "B1", MAIN, False, 1, ("ch", "J2"), [(1600.0, 1150.0)], ("ch", "K2")),
    # Round 24 Part B (item 7, Dan: "shouldn't all main lines intersect here?"): M2 goes on from J10 to the hub through K1, K2 and
    # K3 (the north maze's 5 m line, B2a / B2c / B1b / B1c before; B2c widened to 5 m), into the hub's top ledge
    T("M2g", "M2 Lakes Main (K2 to K3)", MAIN, True, 1, ("ch", "K2"), [], ("ch", "K3"), stripe="M2"),
    T("M2h", "M2 Lakes Main (K3 to the hub)", MAIN, True, 1, ("ch", "K3"), [(1800.0, 1215.0)], ("ch", "HUB", "top"), stripe="M2"),
    T("M2e", "M2 Lakes Main (J10 to K1)", MAIN, True, 1, ("ch", "J10"), [], ("ch", "K1"), stripe="M2"),
    T("B2b", "B2", NARROW, False, 1, ("ch", "K1"), [], ("mh", "MH1")),
    T("M2f", "M2 Lakes Main (K1 to K2)", MAIN, True, 1, ("ch", "K1"), [], ("ch", "K2"), stripe="M2"),
    T("B3", "B3", NARROW, False, 1, ("ch", "K3"), [], ("mh", "MH2")),
    T("B4a", "B4 (down the drop shaft)", NARROW, False, 1, ("ch", "K3"), [(1680.0, 1195.0)], ("ch", "S4", "high")),
    T("B4b", "B4", NARROW, False, 1, ("ch", "S4"), [], ("ch", "J2")),
    T("B5", "B5", NARROW, False, 1, ("ch", "S4"), [], ("ch", "D5")),
    T("B6", "B6", NARROW, False, 1, ("ch", "J3"), [(1860.0, 975.0)], ("mh", "MH3")),
    T("B7a", "B7", NARROW, False, 1, ("ch", "J3"), [(1745.0, 935.0)], ("ch", "K7")),
    T("B7b", "B7", NARROW, False, 1, ("ch", "K7"), [], ("mh", "MH4")),
    T("B8", "B8 (flooded under Bells Ferry Road)", NARROW, False, 1, ("ch", "K7"), [(1600.0, 880.0), (1470.0, 846.0)], ("x", "X_W20"), flood=(1470.0, 846.0)),
    T("B9a", "B9", NARROW, False, 1, ("ch", "HUB", "mid"), [(1960.0, 1060.0)], ("ch", "K8")),
    T("B9b", "B9", NARROW, False, 1, ("ch", "K8"), [], ("mh", "MH5")),
    T("B10", "B10", NARROW, False, 1, ("ch", "K8"), [], ("ch", "D10")),
    # ---- stage 2
    # Round 24 Part B (item 7): M3 now pours into the hub as a waterfall off the mid ledge (it came in at the floor)
    T("M3a", "M3 Noonday Main (J4 to the hub)", MAIN, True, 2, ("ch", "J4"), [(2150.0, 1185.0), (2030.0, 1160.0)], ("ch", "HUB", "mid"), stripe="M3"),
    T("M3b", "M3 Noonday Main (J4 to J5, under Noonday Creek, west of P1)", MAIN, True, 2, ("ch", "J4"), [(2294.0, 1262.0), (2302.0, 1318.0)], ("ch", "J5"), stripe="M3"),
    T("M3c", "M3 Noonday Main (J5 up the big lake in glass, to K15)", MAIN, True, 2, ("ch", "J5"), [(2364.0, 1400.0), (2374.0, 1480.0), (2378.0, 1586.0), (2420.0, 1600.0), (2436.0, 1566.0)], ("ch", "K15"), glass=[15], stripe="M3"),
    T("M3d", "M3 Noonday Main (the launch over the big lake)", MAIN, True, 2, ("ch", "K15"), [(2472.0, 1522.0), (2440.0, 1522.0)], ("x", "L_W15"), stripe="M3"),
    T("B11", "B11", NARROW, False, 2, ("ch", "J4"), [(2300.0, 1150.0), (2290.0, 1106.5)], ("x", "X_TRAIL")),   # Round 24 A2: in from the west
    T("B12", "B12", NARROW, False, 2, ("ch", "J5"), [(2452.0, 1462.0), (2512.0, 1486.0)], ("x", "X_W12")),
    T("B13", "B13", NARROW, False, 2, ("ch", "K15"), [], ("mh", "MH6")),
    T("B14", "B14", NARROW, False, 2, ("ch", "J4"), [(2420.0, 1236.0), (2560.0, 1256.0)], ("mh", "MH7")),
    T("B15", "B15", NARROW, False, 2, ("ch", "K15"), [(2478.0, 1640.0), (2484.0, 1700.0)], ("x", "X_W7")),
    # ---- stage 3
    # Round 24 Part B (item 7): M4 starts at the hub (it started at J4): a new first length from J6 straight to the hub's mid ledge;
    # the old J4 to J6 length stays as an unmarked 5 m branch, B27, with its flooded sag
    T("M4a", "M4 Glen Main (J6 to the hub)", MAIN, True, 3, ("ch", "J6"), [(2200.0, 990.0), (2040.0, 1110.0)], ("ch", "HUB", "mid"), stripe="M4"),
    T("B27", "B27 (J4 to J6)", MAIN, False, 3, ("ch", "J4"), [(2290.0, 1080.0), (2318.0, 990.0), (2340.0, 940.0)], ("ch", "J6"), flood=(2318.0, 995.0)),
    T("M4b", "M4 Glen Main (J6 to J7)", MAIN, True, 3, ("ch", "J6"), [(2460.0, 880.0), (2560.0, 905.0)], ("ch", "J7"), stripe="M4"),
    T("M4c", "M4 Glen Main (J7 to J12, glass under the Chastain Glen pond)", MAIN, True, 3, ("ch", "J7"), [(2790.0, 915.0), (2860.0, 862.0)], ("ch", "J12"), glass=[8], stripe="M4"),
    T("M4d", "M4 Glen Main (J12 to J8)", MAIN, True, 3, ("ch", "J12"), [(3010.0, 1010.0)], ("ch", "J8"), stripe="M4"),
    T("B16", "B16", NARROW, False, 3, ("ch", "J6"), [(2300.0, 800.0), (2230.0, 700.0)], ("mh", "MH8")),
    T("B17", "B17", NARROW, False, 3, ("ch", "J7"), [(2712.0, 900.0)], ("mh", "MH9")),
    # B18 (J12 to L_W8, the launch over the Chastain Glen pond): out until it is re-routed (Round 24, Dan)
    T("B19a", "B19", NARROW, False, 3, ("ch", "J13"), [(2800.0, 650.0)], ("ch", "K19")),
    T("B19b", "B19", NARROW, False, 3, ("ch", "K19"), [], ("mh", "MH10")),
    T("B19c", "B19", NARROW, False, 3, ("ch", "K19"), [], ("x", "X_W11")),   # Round 24 A2: straight from K19 to the new mouth
    T("B26", "B26", NARROW, False, 3, ("ch", "J6"), [(2380.0, 779.0)], ("x", "X_BRIDGE")),   # Round 24 A2: in from the west
    T("B20", "B20", NARROW, False, 3, ("ch", "J8"), [(3060.0, 1150.0)], ("mh", "MH11")),
    T("B21", "B21", MAIN, False, 3, ("ch", "J8"), [(3168.0, 1232.0)], ("x", "X_SHOPS")),
    T("B22", "B22", NARROW, False, 3, ("ch", "J8"), [(3300.0, 1340.0), (3560.0, 1440.0)], ("x", "X_LNC")),
    # ---- stage 4
    T("M2a", "M2 Lakes Main (J10 to J9, glass along East Chastain Lake)", MAIN, True, 4, ("ch", "J10"), [(1385.0, 1230.0), (1345.0, 1160.0), (1250.0, 1170.0), (1060.0, 1200.0)], ("ch", "J9"), glass=[9], stripe="M2"),
    T("M2b", "M2 Lakes Main (J9 to K25, glass along West Chastain Lake)", MAIN, True, 4, ("ch", "J9"), [(760.0, 1335.0), (640.0, 1385.0)], ("ch", "K25"), glass=[0], stripe="M2"),
    T("M2c", "M2 Lakes Main (K25 under the interstate to the far-west pond)", MAIN, True, 4, ("ch", "K25"), [(440.0, 1440.0), (300.0, 1470.0)], ("x", "X_W5"), stripe="M2"),
    # Round 24 Part B (item 7): M6 runs from Nelson Lake to the hub's mid ledge (it ran from J9, on M2, 1 km from the hub)
    T("M6", "M6 Nelson Main", MAIN, True, 4, ("x", "X_NELSON"), M6_WAY, ("ch", "HUB", "mid"), stripe="M6"),
    T("B23", "B23", NARROW, False, 4, ("ch", "J9"), [], ("mh", "MH12")),
    T("B24", "B24", NARROW, False, 4, ("ch", "J9"), [(930.0, 1090.0)], ("mh", "MH13")),
    T("B25", "B25", NARROW, False, 4, ("ch", "K25"), [(520.0, 1450.0)], ("x", "X_STREAM")),
]

DIFFERS = [
    "J1 is no longer a big room straight inside BF (Dan, BUG-006): BF's two barrels split just past the headwall; the right one runs to J1, now a junction 75 m further on (M1 and M5 start there), the left one to J10 (M2 and B2), near where the old J1 stood",
    "The Chastain Glen pond culvert is the second entrance (Dan, BUG-001): two 5 m pipes, the left one north to J12 (on M4, which now passes it), the right one south under New Chastain Road to J13 (B19 now starts there, not at J7)",
    "M1 runs under the north Enclave pond (W1) in a glass tube along the pond's length; J2 is a plain junction on the pond's east shore instead of a chamber with a glass roof",
    "Every place where a branch leaves another is a junction chamber (K1 to K25): pipes only ever meet in a room",
    "B4's drop shaft is a chamber of its own (S4): B4 drops 8 m into it from K3; B5 is a dead end off the shaft's floor with a small turning room (D5); B10 likewise (D10)",
    "M3: no glass under Noonday Creek (a creek is too narrow to dig round a tube) and no grate from P1's floor (P1 is a finished round pipe); the glass is under the big lake, J5 beside it on the shore",
    "No drop shaft at J7 (one drop shaft, S4, in the north maze)",
    "Stage 2: J5 sits on the big lake's south shore (the proposal's J5 was in the water); M3 passes west of P1's inlet (it would have hit P1) and runs up the big lake in glass to K15, a junction on its east side; the launch over the big lake goes out from its east shore; B12 comes out on the north bank of the pond east of the lake",
    "Stage 3: J6 moved 50 m north of the New Chastain Road bridges (the exit under the bridges needs room for its cut; B26 is 3.5 m); the launch over the Chastain Glen pond comes off J12 into its north-east corner (from K17 it could not climb to the pond's level in time; K17 is gone, B17 runs from J7 to its manhole); J8 sits a little up the slope; the exit behind the shops is in the low strip north-west of the center, its cut into the bank",
    "Stage 4: J9 sits deeper (its floor 284 m) so M2 stays under East Chastain Lake's bed to its shore; the exit at the pond by Chastain Road (W21) is dropped (the ground there is too low for a pipe to come up under it) and B24 runs from J9 straight to its manhole",
    "Round 24 Part B (Dan: \"shouldn't all main lines intersect here?\"): every main now pours into the hub as a waterfall off a ledge. M3 comes in at the mid ledge (it came in at the floor); M4 starts with a new length from J6 straight to the hub's mid ledge (the old J4 to J6 length is B27, an unmarked 5 m branch, with its flooded sag); M2 goes on from J10 through K1, K2 and K3 to the hub's top ledge (the north maze's 5 m line; K1 to K2 widened to 5 m); M6 runs from Nelson Lake to the hub's mid ledge, 2 km (it ran from J9, on M2)",
    "Round 24 Part B (Dan: \"It is very easy to get turned around in the tunnels\"): chevrons on the mains' stripes point the way to the hub along the mains, and every junction on a main has a HUB sign with an arrow beside the main that leads there",
    "Round 24 (Dan): L_W8, the launch over the Chastain Glen pond, and its pipe B18 are out (they left a house floating); GP, 100 m away, is that pond's way in and out",
    "Round 28 (Dan: \"Lets try the shortcuts\"): the underground race's switch shortcuts are built as world pipes. S2 the dive pipe: two junctions on M5 (J14, J15; M5's line and floor kept) and a straight pipe between them that dives under M5's bend (up to 24 %) with a flooded sump at the bottom; J15 sits 30 m clear of B8. S3 the high line: J1 straight to a high opening 2.5 m up J2's west wall (J2 3 m taller and 2 m wider), lined up with M1b's opening across the room: you fly over J2's floor",
]


# ================================================================ resolve
def end_pos(e):
    k = e[0]
    if k == "ch": return CH[e[1]]["at"]
    if k == "mh": return MANHOLES[e[1]][1]
    if k == "x": return EXITS[e[1]]["at"]
    if k == "pipe":
        p = PIPES[e[1]]; A, B = p["a"], p["b"]; U = norm(sub(B, A)); Nl = (-U[1], U[0]); o = p["diameter_m"] * 0.5 + 0.4
        return add(B, Nl, o * e[2])
    raise SystemExit(e)


def ch_frame(c):
    u = from_hdg(c["yaw"]); v = (u[1], -u[0])   # v: the right of u
    return u, v


def ch_half(c):
    if c["kind"] == "hub": return HUB["hl"], HUB["hw"]
    return c["size"][0] * 0.5, c["size"][1] * 0.5


# chamber openings: wall 0 = +u (the far wall), 1 = +v, 2 = -u, 3 = -v; 't' along the wall (from its left end seen from inside,
# no: along +v for the u walls and along +u for the v walls), 'inset' (the hub: the ledge face it is in)
openings = {k: [] for k in CH}


def chamber_level(c, lvl):
    if c["kind"] == "hub": return HUB["levels"][lvl or "floor"]
    return None   # set when the chamber's floor is known


# Round 25 (A3, BUG-005, Dan at M6's mouth in the hub: "this entrance is blocked"): the hub's two ramps (MapBuilder.NetworkHub.cs)
# run along a ledge's wall, so an opening there opened onto the ramp's side (M6's mouth faced a 2.8 m high ramp side). The ramp
# from the mid ledge to the top ledge lies along wall 1's mid-level band from a = -(hl - mid inset) up to a + 50 m; the one from the
# floor to the mid ledge along wall 3's floor band from a = (hl - floor inset) - 50 m to its end. No opening at those levels within
# 1.5 m of them (t along u on walls 1 and 3)
HUB_RAMP_GRADE = 0.2
def hub_ramp_clamp(o):
    if o["wall"] == 1 and o["lvl"] == "mid":
        end = -(HUB["hl"] - HUB["insets"]["mid"]) + (HUB["levels"]["top"] - HUB["levels"]["mid"]) / HUB_RAMP_GRADE
        o["t"] = max(o["t"], end + o["d"] * 0.5 + 1.5)
    if o["wall"] == 3 and (o["lvl"] or "floor") == "floor":
        start = (HUB["hl"] - HUB["insets"]["floor"]) - (HUB["levels"]["mid"] - HUB["levels"]["floor"]) / HUB_RAMP_GRADE
        o["t"] = min(o["t"], start - o["d"] * 0.5 - 1.5)


def alloc_opening(cid, toward, d, lvl, tube_id):
    c = CH[cid]; u, v = ch_frame(c); hl, hw = ch_half(c)
    inset = HUB["insets"][lvl or "floor"] if c["kind"] == "hub" else 0.0
    hl2, hw2 = hl - inset, hw - inset
    rel = sub(toward, c["at"]); a = dot(rel, u); b = dot(rel, v)
    if abs(a) / hl2 >= abs(b) / hw2:
        wall = 0 if a > 0 else 2; t = b * hl2 / max(abs(a), 1e-6); half = hw2
    else:
        wall = 1 if b > 0 else 3; t = a * hw2 / max(abs(b), 1e-6); half = hl2
    lim = half - d * 0.5 - 1.2
    if c["kind"] == "hub":   # over the band below it, whose water falls on down (not over the corner of its own ledge)
        nxt = HUB["insets"]["floor"]   # (a top ledge's water falls to the mid ledge, then on to the floor: both under it)
        lim = (half + inset) - nxt - d * 0.5 - 2.5
    t = max(-lim, min(lim, t))
    o = dict(wall=wall, t=t, d=d, lvl=lvl, tube=tube_id, inset=inset, half=half)
    if c["kind"] == "hub": hub_ramp_clamp(o)   # Round 25 (A3)
    openings[cid].append(o)
    return o


def spread_openings():
    """push openings on the same wall (and level) apart so their rims are 1.4 m clear"""
    for cid, ops in openings.items():
        for wall in range(4):
            for lvl in set(o["lvl"] for o in ops):
                ws = sorted([o for o in ops if o["wall"] == wall and o["lvl"] == lvl], key=lambda o: o["t"])
                if len(ws) < 2: continue
                for _ in range(40):
                    moved = False
                    for i in range(len(ws) - 1):
                        need = (ws[i]["d"] + ws[i + 1]["d"]) * 0.5 + 1.4
                        gap = ws[i + 1]["t"] - ws[i]["t"]
                        if gap < need:
                            sh = (need - gap) * 0.5 + 0.01; ws[i]["t"] -= sh; ws[i + 1]["t"] += sh; moved = True
                    for o in ws:
                        if CH[cid]["kind"] == "hub":
                            nxt = HUB["insets"]["floor"]
                            lim = (o["half"] + o["inset"]) - nxt - o["d"] * 0.5 - 2.5
                        else: lim = o["half"] - o["d"] * 0.5 - 1.0
                        o["t"] = max(-lim, min(lim, o["t"]))
                        if CH[cid]["kind"] == "hub": hub_ramp_clamp(o)   # Round 25 (A3)
                    if not moved: break
                for i in range(len(ws) - 1):
                    if ws[i + 1]["t"] - ws[i]["t"] < (ws[i]["d"] + ws[i + 1]["d"]) * 0.5 + 0.8:
                        raise SystemExit(f"chamber {cid}: openings {ws[i]['tube']} and {ws[i + 1]['tube']} do not fit on wall {wall}")


# Round 28 (B1, S3): openings set by hand after spreading: in J2, S3's high opening straight across from M1b's (the jump lands lined up
# with M1b), M1a's to the north of it (S3 comes in south of M1a's line); in J14, S2 out of the south wall (out of the east wall it ran
# beside M5 and could not dive under M5's bend in time). A pin is t (on the wall it was given) or (wall, t)
PINS = {"J2": {"M1a": -6.0, "S3": 1.5, "M1b": 1.5}, "J14": {"S2": (1, 3.0)}}
def pin_openings():
    for cid, pins in PINS.items():
        c = CH[cid]; hl, hw = ch_half(c)
        for o in openings[cid]:
            if o["tube"] not in pins: continue
            pv = pins[o["tube"]]
            if isinstance(pv, tuple): o["wall"], o["t"] = pv; o["half"] = hw if o["wall"] in (0, 2) else hl
            else: o["t"] = pv


def opening_point(cid, o):
    c = CH[cid]; u, v = ch_frame(c); hl, hw = ch_half(c); ins = o["inset"]
    n = [u, v, (-u[0], -u[1]), (-v[0], -v[1])][o["wall"]]
    along = [v, u, v, u][o["wall"]]
    dist = (hl - ins) if o["wall"] in (0, 2) else (hw - ins)
    p = add(add(c["at"], n, dist), along, o["t"])
    return p, n


def fillet(pts, radii, keep_first=0.0, keep_last=0.0):
    """round each interior corner with an arc (radius radii[i]), never eating into the first keep_first m and the last keep_last m"""
    out = [pts[0]]
    n = len(pts)
    segL = [math.hypot(*sub(pts[i + 1], pts[i])) for i in range(n - 1)]
    for i in range(1, n - 1):
        a, b, c = pts[i - 1], pts[i], pts[i + 1]
        d1 = norm(sub(b, a)); d2 = norm(sub(c, b))
        cr = d1[0] * d2[1] - d1[1] * d2[0]; dt = max(-1.0, min(1.0, dot(d1, d2)))
        th = math.acos(dt)
        if th < 1e-3: out.append(b); continue
        avail1 = segL[i - 1] * (0.5 if i - 1 > 0 else 1.0) - (keep_first if i - 1 == 0 else 0.0)
        avail2 = segL[i] * (0.5 if i < n - 2 else 1.0) - (keep_last if i == n - 2 else 0.0)
        tlen = min(radii * math.tan(th / 2), max(0.5, avail1), max(0.5, avail2))
        r = tlen / math.tan(th / 2)
        p1 = add(b, d1, -tlen); p2 = add(b, d2, tlen)
        nrm = (-d1[1], d1[0]) if cr > 0 else (d1[1], -d1[0])
        cc = add(p1, nrm, r)
        a0 = math.atan2(p1[1] - cc[1], p1[0] - cc[0]); a1 = math.atan2(p2[1] - cc[1], p2[0] - cc[0])
        da = a1 - a0
        while da > math.pi: da -= 2 * math.pi
        while da < -math.pi: da += 2 * math.pi
        steps = max(2, int(abs(da) * r / 1.0))
        for k in range(steps + 1): ang = a0 + da * k / steps; out.append((cc[0] + r * math.cos(ang), cc[1] + r * math.sin(ang)))
    out.append(pts[-1])
    return out


def densify(line, step=1.0):
    out = [line[0]]
    for k in range(len(line) - 1):
        a, b = line[k], line[k + 1]; L = math.hypot(b[0] - a[0], b[1] - a[1])
        if L < 1e-6: continue
        n = max(1, int(math.ceil(L / step)))
        for i in range(1, n + 1): out.append((a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n))
    return out


def min_ground_across(p, t, half):
    nx, nz = -t[1], t[0]; lo = 1e9
    for o in np.linspace(-half, half, 5): lo = min(lo, ground(p[0] + nx * o, p[1] + nz * o))
    wet = any(in_poly(p, w) for w in WATER_NEAR(p)) or any(seg_dist(p, s[k], s[k + 1])[0] < 4.0 for s in STREAMS_NEAR(p) for k in range(len(s) - 1))
    return lo - (0.6 if wet else 0.0)


_wb = [(min(x for x, z in w) - 5, max(x for x, z in w) + 5, min(z for x, z in w) - 5, max(z for x, z in w) + 5) for w in WATER]
_sb = [(min(x for x, z in s) - 5, max(x for x, z in s) + 5, min(z for x, z in s) - 5, max(z for x, z in s) + 5) for s in STREAMS]
def WATER_NEAR(p): return [WATER[i] for i, b in enumerate(_wb) if b[0] <= p[0] <= b[1] and b[2] <= p[1] <= b[3]]
def STREAMS_NEAR(p): return [STREAMS[i] for i, b in enumerate(_sb) if b[0] <= p[0] <= b[1] and b[2] <= p[1] <= b[3]]


def chamber_floor(cid):
    c = CH[cid]
    if "floor" in c: return c["floor"]
    if c["kind"] == "hub": c["floor"] = HUB["levels"]["floor"]; return c["floor"]
    u, v = ch_frame(c); hl, hw = ch_half(c); lo = 1e9
    for a in np.linspace(-hl - 3, hl + 3, 9):
        for b in np.linspace(-hw - 3, hw + 3, 9):
            q = add(add(c["at"], u, a), v, b); lo = min(lo, ground(*q) - (0.6 if any(in_poly(q, w) for w in WATER_NEAR(q)) else 0.0))
    c["floor"] = round(lo - 1.2 - c["size"][2] - 0.3, 2)
    c["ground_min"] = lo
    return c["floor"]


# ---------------------------------------------------------------- profile
def max_grade(tb): return tb.get("max_grade") or (0.10 if tb["main"] else (0.12 if tb["d"] >= 5 else 0.16))   # Round 28: S2 its own


def profile(s, pts, d, fixed, main, maxg, glass_ranges, caps=None):
    """the floor along the line: the fixed levels (s, y, lock half-length) kept; elsewhere as straight as the grade limit and
    the cover over the crown allow (1.5 m on the mains, 1.2 m in the maze; under a glass stretch the crown 1 m under the water)"""
    n = len(s); cover = 1.5 if main else 1.2
    t = [norm(sub(pts[min(i + 1, n - 1)], pts[max(i - 1, 0)])) for i in range(n)]
    g = [min_ground_across(pts[i], t[i], d * 0.5 + 0.5) for i in range(n)]
    need = [g[i] - cover - d - 0.3 for i in range(n)]
    inglass = [False] * n
    for (a, b, y) in glass_ranges:
        for i in range(n):
            if a <= s[i] <= b: need[i] = y; inglass[i] = True
    if caps:   # Round 28: under another pipe where it crosses it
        for i in range(n):
            if caps[i] is not None: need[i] = min(need[i], caps[i])
    fs = [f[0] for f in fixed]; fl = [f[1] for f in fixed]
    lock = [False] * n
    for f in fixed:
        for i in range(n):
            if abs(s[i] - f[0]) <= f[2]: lock[i] = True
    base = [float(np.interp(s[i], fs, fl)) for i in range(n)]
    floor = [base[i] if lock[i] else min(base[i], need[i]) for i in range(n)]
    for _ in range(3):
        for i in range(1, n):
            if not lock[i]: floor[i] = min(floor[i], floor[i - 1] + maxg * (s[i] - s[i - 1]))
        for i in range(n - 2, -1, -1):
            if not lock[i]: floor[i] = min(floor[i], floor[i + 1] + maxg * (s[i + 1] - s[i]))
        for i in range(1, n):
            if not lock[i]: floor[i] = max(floor[i], floor[i - 1] - maxg * (s[i] - s[i - 1]))
        for i in range(n - 2, -1, -1):
            if not lock[i]: floor[i] = max(floor[i], floor[i + 1] - maxg * (s[i + 1] - s[i]))
    # round the vertical bends (sigma 5 m); the locked stretches kept and blended into over 6 m
    sm = list(floor); sig = 5.0; r = int(3 * sig)
    lock_idx = [i for i in range(n) if lock[i]]
    for i in range(n):
        if lock[i]: continue
        acc = w = 0.0
        for k in range(max(0, i - r), min(n, i + r + 1)):
            ww = math.exp(-0.5 * ((s[k] - s[i]) / sig) ** 2); acc += floor[k] * ww; w += ww
        dl = min((abs(s[i] - s[j]) for j in lock_idx), default=99.0)
        k = min(1.0, dl / 6.0); sm[i] = floor[i] * (1 - k) + (acc / w) * k
    floor = sm
    cov = [(g[i] - (floor[i] + d + 0.3)) if not inglass[i] else 9.0 for i in range(n)]
    grade = max((abs(floor[i + 1] - floor[i]) / max(1e-6, s[i + 1] - s[i]) for i in range(n - 1)), default=0.0)
    return floor, cov, grade, g


RISER_R, RISER_A = 8.0, math.radians(50.0)
def riser_run(Hh):
    """the horizontal reach of a riser of height Hh: the 8 m vertical curve up to 50 degrees, then straight at 50"""
    return RISER_R * math.sin(RISER_A) + max(0.0, Hh - RISER_R * (1 - math.cos(RISER_A))) / math.tan(RISER_A)


def make_heads(tb, ops, est):
    ends = [tb["a"], tb["b"]]; d = tb["d"]; heads = {}; segs = {}
    for k, e in enumerate(ends):
        nb = (tb["way"][0] if tb["way"] else end_pos(ends[1])) if k == 0 else (tb["way"][-1] if tb["way"] else end_pos(ends[0]))
        if e[0] == "ch":
            cid = e[1]; o = ops[k]; p, nrm = opening_point(cid, o)
            lead = o["inset"] + 12.0
            high = len(e) > 2 and e[2] == "high"
            if CH[cid]["kind"] == "hub": lvl = HUB["levels"][o["lvl"] or "floor"]
            else: lvl = chamber_floor(cid) + (CH[cid].get("drop", 0.0) if high else 0.0)   # Round 28: a junction's high opening too (J2, S3)
            segs[k] = [p, add(p, nrm, lead)]; heads[k] = dict(kind="ch", level=lvl, lock=lead, chamber=cid, wall=o["wall"], t=o["t"], inset=o["inset"], lvl=o["lvl"], high=high)
        elif e[0] == "pipe":
            pj = PIPES[e[1]]; U = norm(sub(pj["b"], pj["a"])); p = end_pos(e)
            segs[k] = [p, add(p, U, 8.0)]; heads[k] = dict(kind="pipe", level=pj["b_floor"], lock=4.0, pipe=e[1], side=e[2])
        elif e[0] == "mh":
            road, near = MANHOLES[e[1]]; m, tdir = road_point(road, near)
            if dot(sub(nb, m), tdir) > 0: tdir = (-tdir[0], -tdir[1])   # the riser climbs along the road, away from where the pipe comes from
            gm = ground(*m); Hh = est.get(("mh", k), d + 0.3 + 1.2 + 1.0)
            foot = add(m, tdir, -riser_run(Hh))
            segs[k] = [foot, add(foot, tdir, -18.0)]
            heads[k] = dict(kind="mh", level=est.get(("mhlevel", k)), lock=6.0, mh=e[1], street=m, dir=tdir, ground=gm, H=Hh, road=road)
        elif e[0] == "x":
            x = EXITS[e[1]]; A = x["at"]; dirx = norm(sub(nb, A)); room = math.hypot(*sub(nb, A))
            cap = room - (40.0 if not tb["way"] else 14.0)
            Lx = est.get(("x", k), 30.0); Lx = max(20.0, min(Lx, cap if cap > 20.0 else room * 0.4))
            if x.get("max_len"): Lx = min(Lx, x["max_len"])   # Round 24 A2: a cut that must stop short of a street
            B = add(A, dirx, Lx)
            segs[k] = [B, add(B, dirx, 12.0)]
            heads[k] = dict(kind="x", level=est.get(("xlevel", k)), lock=4.0, exit=e[1], A=A, B=B, dir=dirx, L=Lx, room=room, gA=ground(*A), outfall=x.get("outfall"))
    plan = segs[0] + list(tb["way"]) + list(reversed(segs[1]))
    return heads, plan


def run_profile(tb, heads, plan):
    d = tb["d"]; R = d * 0.5
    rad = 22.0 if d >= 5 else 12.0
    kf = 0.0 if heads[0]["kind"] == "pipe" else 3.0; kl = 0.0 if heads[1]["kind"] == "pipe" else 3.0
    pts = densify(fillet(plan, rad, keep_first=kf, keep_last=kl), 1.0)
    # the tightest bend (radius from three points 4 m apart): under twice the pipe's radius its inside would fold
    tight = 1e9
    for i in range(4, len(pts) - 4):
        a, b, c = pts[i - 4], pts[i], pts[i + 4]
        ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a); ar = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) / 2
        if ar > 1e-6: tight = min(tight, ab * bc * ca / (4 * ar))
    if tight < 2.0 * R: print(f"  BEND {tb['id']}: tightest radius {tight:.1f} m (pipe radius {R:.1f})")
    s = [0.0]
    for i in range(1, len(pts)): s.append(s[-1] + math.hypot(*sub(pts[i], pts[i - 1])))
    L = s[-1]
    fixed = [(sv, heads[k]["level"], heads[k]["lock"]) for k, sv in ((0, 0.0), (1, L)) if heads[k]["level"] is not None]
    gr, glass_out = [], []
    for wi in tb["glass"]:
        ring = WATER[wi]; lv = pond_level(wi)
        inside = [i for i in range(len(pts)) if in_poly(pts[i], ring) and min(seg_dist(pts[i], ring[j], ring[(j + 1) % len(ring)])[0] for j in range(len(ring))) > R + 1.0]
        if len(inside) < 12: print(f"  {tb['id']}: glass under W{wi}: only {len(inside)} m inside, none"); continue
        runs, cur = [], [inside[0]]
        for i in inside[1:]:
            if i == cur[-1] + 1: cur.append(i)
            else: runs.append(cur); cur = [i]
        runs.append(cur); run = max(runs, key=len)
        a, b = s[run[0]], s[run[-1]]
        gr.append((a, b, lv - 1.0 - d - 0.05)); glass_out.append(dict(pond=wi, s0=round(a, 1), s1=round(b, 1), surface=round(lv, 2)))
    caps = None
    if tb.get("under"):   # Round 28 (S2): its shell 1.8 m under the floor of each pipe it crosses, within 14 m of that pipe's line (the dip survives the smoothing)
        caps = [None] * len(pts)
        for oid in tb["under"]:
            other = BUILT[oid]
            for i, q in enumerate(pts):
                near = [o for o in other if abs(o[0] - q[0]) < 15 and abs(o[2] - q[1]) < 15 and math.hypot(o[0] - q[0], o[2] - q[1]) < 14.0]
                if near:
                    y = min(o[1] for o in near) - d - 0.3 - 1.8   # shell to shell 1.8 m (the clearance rule is 1.5)
                    caps[i] = y if caps[i] is None else min(caps[i], y)
    floor, cov, grade, g = profile(s, pts, d, fixed, tb["main"], max_grade(tb), gr, caps)
    return dict(pts=pts, s=s, floor=floor, cov=cov, grade=grade, g=g, glass=glass_out)


BUILT = {}   # Round 28: each tube's floor line as it is built (for 'under')
def build(stage_max):
    plans = []
    for tb in TUBES:
        ends = [tb["a"], tb["b"]]; way = list(tb["way"]); ops = [None, None]
        for k, e in enumerate(ends):
            if e[0] != "ch": continue
            nb = (way[0] if way else end_pos(ends[1])) if k == 0 else (way[-1] if way else end_pos(ends[0]))
            ops[k] = alloc_opening(e[1], nb, tb["d"], e[2] if len(e) > 2 else None, tb["id"])
        plans.append(dict(tb=tb, ops=ops, est={}))
    spread_openings(); pin_openings()
    for cid in CH: chamber_floor(cid)
    # chamber floors: lowered (never raised: the cover over them) until every pipe between two fixed levels fits its grade
    for it in range(60):
        changed = False
        for pl in plans:
            tb = pl["tb"]; heads, plan = make_heads(tb, pl["ops"], pl["est"])
            if heads[0]["level"] is None or heads[1]["level"] is None: continue
            L = sum(math.hypot(*sub(plan[i + 1], plan[i])) for i in range(len(plan) - 1))
            room = max(4.0, L - heads[0]["lock"] - heads[1]["lock"] - 8.0) * max_grade(tb)
            y0, y1 = heads[0]["level"], heads[1]["level"]
            if abs(y0 - y1) <= room + 1e-3: continue
            hi = 0 if y0 > y1 else 1; e = [tb["a"], tb["b"]][hi]
            if heads[hi]["kind"] == "ch" and CH[e[1]]["kind"] != "hub":
                CH[e[1]]["floor"] = round(CH[e[1]]["floor"] - (abs(y0 - y1) - room), 2); changed = True
            elif it == 0:
                lo = 1 - hi
                print(f"  CONFLICT {tb['id']}: {y0:.1f} to {y1:.1f} over {L:.0f} m (end {hi} fixed {heads[hi]['kind']}, the other {heads[lo]['kind']})")
        if not changed: break
    out_tubes, exits_out, manholes_out = [], [], []
    for pl in plans:
        tb = pl["tb"]; d = tb["d"]; est = pl["est"]
        heads, plan = make_heads(tb, pl["ops"], est); pr = run_profile(tb, heads, plan)
        # free ends: a manhole's riser foot as deep as the pipe arrives (at least its own depth), an exit's level where its cut can reach
        again = False
        for k in (0, 1):
            h = heads[k]; yend = pr["floor"][0 if k == 0 else -1]
            other = heads[1 - k]["level"] if heads[1 - k]["level"] is not None else yend
            reach = max_grade(tb) * max(0.0, pr["s"][-1] - heads[0]["lock"] - heads[1]["lock"] - 10.0)
            if h["kind"] == "mh":
                # the riser's foot as high as the pipe can climb to from its other end (a riser no taller than it has to be), and at
                # least the riser's own depth under the street
                fp = add(h["street"], h["dir"], -riser_run(h["H"]))
                glow = min([h["ground"]] + [ground(*add(fp, h["dir"], -x)) - 0.6 for x in np.linspace(0, 18, 7)])   # the street, and the ground along the approach
                foot = min(glow - (d + 0.3 + 1.2 + 0.5), other + reach); est[("mhlevel", k)] = round(foot, 2); est[("mh", k)] = h["ground"] - foot; again = True
            elif h["kind"] == "x":
                Lx = min(max(20.0, (h["gA"] - yend) / 0.18 + 6.0), max(20.0, h["room"] - (40.0 if not tb["way"] else 14.0)))
                for _ in range(40):   # far enough in that the ground over the pipe's end is 1 m over its crown
                    yb = max(yend, h["gA"] - 0.18 * (Lx - 3.0) + 0.05); Bp = add(h["A"], h["dir"], Lx)
                    if min(ground(*add(Bp, h["dir"], x)) for x in (-4.0, 0.0, 4.0, 8.0)) - (yb + d + 0.3) >= 1.0 or Lx > h["room"] - (40.0 if not tb["way"] else 14.0): break
                    Lx += 2.0
                est[("x", k)] = Lx
                heads2, _ = make_heads(tb, pl["ops"], est); Lx2 = heads2[k]["L"]
                est[("xlevel", k)] = round(max(yend, h["gA"] - 0.18 * (Lx2 - 3.0) + 0.05), 2); again = True
                if est[("xlevel", k)] - other > reach + 0.5: print(f"  EXIT {tb['id']}: its cut ends {est[('xlevel', k)] - other:.1f} m over the pipe's other end, more than it can climb ({reach:.1f} m)")
        if again:
            heads, plan = make_heads(tb, pl["ops"], est); pr = run_profile(tb, heads, plan)
        pts, s, floor, cov = pr["pts"], pr["s"], pr["floor"], pr["cov"]
        flood_out = None
        if tb["flood"]:   # a flooded sag: the floor dipped 0.9 m over 30 m (a smooth trough), its water 0.55 m deep in the middle
            if tb["flood"] == "low": fi = min(range(len(pts)), key=lambda i: floor[i])   # Round 28 (S2): at its lowest point
            else: fi = min(range(len(pts)), key=lambda i: math.hypot(*sub(pts[i], tb["flood"])))
            c0 = s[fi]
            for i in range(len(s)):
                x = (s[i] - c0) / 15.0
                if abs(x) < 1.0: floor[i] -= 0.9 * 0.5 * (1 + math.cos(math.pi * x))
            flood_out = dict(s0=round(c0 - 11.0, 1), s1=round(c0 + 11.0, 1), level=round(floor[fi] + 0.55, 2))
        pts3 = [(pts[i][0], floor[i], pts[i][1]) for i in range(len(pts))]
        BUILT[tb["id"]] = list(pts3)
        for k in (0, 1):
            h = heads[k]
            if h["kind"] != "mh": continue
            foot3 = pts3[-1] if k == 1 else pts3[0]; tdir = h["dir"]; ys = foot3[1]; riser = []
            for a in np.linspace(0, RISER_A, 14)[1:]:
                riser.append((foot3[0] + tdir[0] * RISER_R * math.sin(a), ys + RISER_R * (1 - math.cos(a)), foot3[2] + tdir[1] * RISER_R * math.sin(a)))
            xa, ya = RISER_R * math.sin(RISER_A), ys + RISER_R * (1 - math.cos(RISER_A))
            run = max(0.0, h["ground"] - ya) / math.tan(RISER_A)
            for q in np.linspace(0, 1, max(2, int(run / 0.4) + 1))[1:]:
                riser.append((foot3[0] + tdir[0] * (xa + run * q), ya + run * q * math.tan(RISER_A), foot3[2] + tdir[1] * (xa + run * q)))
            if k == 1: pts3 = pts3 + riser
            else: pts3 = list(reversed(riser)) + pts3
            manholes_out.append(dict(id=h["mh"], name=f"{h['mh']} MANHOLE", tube=tb["id"], road=h["road"], x=round(h["street"][0], 2), z=round(h["street"][1], 2), y=round(h["ground"], 2),
                                     dir=[round(tdir[0], 4), round(tdir[1], 4)], d=d, stage=tb["stage"], riser_points=len(riser), height=round(h["ground"] - ys, 2)))
        for k in (0, 1):
            h = heads[k]
            if h["kind"] != "x": continue
            exits_out.append(dict(id=h["exit"], name=EXITS[h["exit"]]["name"], tube=tb["id"], a=[round(h["A"][0], 2), round(h["A"][1], 2)],
                                  b=[round(h["B"][0], 2), round(h["B"][1], 2)], b_floor=round(pts3[-1][1] if k == 1 else pts3[0][1], 2), d=d,
                                  outfall=h["outfall"], stage=tb["stage"], ground_a=round(h["gA"], 2)))
        s3 = [0.0]
        for i in range(1, len(pts3)): s3.append(s3[-1] + math.dist(pts3[i], pts3[i - 1]))
        worst = min(range(len(cov)), key=lambda i: cov[i])
        ends_out = []
        for k in (0, 1):
            h = heads[k]; e = dict(kind=h["kind"])
            if h["kind"] == "ch": e.update(chamber=h["chamber"], wall=h["wall"], t=round(h["t"], 3), level=round(h["level"], 2), inset=h["inset"], lvl=h["lvl"], high=h["high"])
            elif h["kind"] == "pipe": e.update(pipe=h["pipe"], side=h["side"])
            elif h["kind"] == "mh": e.update(manhole=h["mh"])
            elif h["kind"] == "x": e.update(exit=h["exit"])
            ends_out.append(e)
        g = pr["g"]
        out_tubes.append(dict(id=tb["id"], name=tb["name"], d=d, main=tb["main"], stage=tb["stage"],
                              stripe=list(STRIPES[tb["stripe"]]) if tb["stripe"] else None,
                              a=ends_out[0], b=ends_out[1], pts=[round(v, 3) for q in pts3 for v in q],
                              glass=pr["glass"], flood=flood_out,
                              report=dict(length=round(s3[-1], 1), grade=round(pr["grade"] * 100, 1), floor_min=round(min(floor), 2), floor_max=round(max(floor), 2),
                                          cover_min=round(cov[worst], 2), cover_at=[round(pts[worst][0]), round(pts[worst][1])], deepest=round(max(g[i] - floor[i] for i in range(len(g))), 1))))
    hub_routes(out_tubes)
    chambers_out = []
    for cid, c in CH.items():
        u, v = ch_frame(c)
        e = dict(id=cid, name=c["name"], kind=c["kind"], x=c["at"][0], z=c["at"][1], yaw=c["yaw"], stage=c["stage"], sign=c.get("sign"),
                 openings=[dict(wall=o["wall"], t=round(o["t"], 3), d=o["d"], tube=o["tube"], inset=o["inset"], lvl=o["lvl"]) for o in openings[cid]])
        if c["kind"] == "hub":
            e.update(length=2 * HUB["hl"], width=2 * HUB["hw"], floor=HUB["levels"]["floor"], mid=HUB["levels"]["mid"], top=HUB["levels"]["top"],
                     inset_mid=HUB["insets"]["mid"], inset_floor=HUB["insets"]["floor"])
            lo = min(ground(*add(add(c["at"], u, a), v, b)) for a in np.linspace(-HUB["hl"] - 2, HUB["hl"] + 2, 23) for b in np.linspace(-HUB["hw"] - 2, HUB["hw"] + 2, 15))
            e["ceiling"] = round(lo - 1.8, 2); e["ground_min"] = round(lo, 2)
        else:
            e.update(length=c["size"][0], width=c["size"][1], height=c["size"][2], floor=c["floor"], drop=c.get("drop", 0.0))
        for o in e["openings"]:
            tb = next(t for t in TUBES if t["id"] == o["tube"])
            o["high"] = any(en[0] == "ch" and en[1] == cid and len(en) > 2 and en[2] == "high" for en in (tb["a"], tb["b"]))
        e["hub_tube"] = HUB_TUBE.get(cid)
        chambers_out.append(e)
    return out_tubes, chambers_out, exits_out, manholes_out


HUB_TUBE = {}
def hub_routes(tubes):
    """Round 24 Part B (item 6, Dan: "It is very easy to get turned around in the tunnels. Can we [make] the lines always point
    towards the hub"): along the mains (the striped pipes only, the way a driver follows them) the way to the hub from every chamber
    on them. Each main gets 'to_hub' (+1: the hub is on toward its b end, -1: back toward a) for the arrows on its stripes; each
    chamber on a main gets 'hub_tube', the main to take out of it (its sign)."""
    import heapq
    dist = {"HUB": 0.0}; first = {}; q = [(0.0, "HUB")]
    mains = [t for t in tubes if t["main"]]
    while q:
        d0, c = heapq.heappop(q)
        if d0 > dist.get(c, 1e18): continue
        for t in mains:
            ends = [t["a"].get("chamber") if t["a"]["kind"] == "ch" else None, t["b"].get("chamber") if t["b"]["kind"] == "ch" else None]
            if c not in ends or None in ends: continue
            o = ends[1] if ends[0] == c else ends[0]; nd = d0 + t["report"]["length"]
            if nd < dist.get(o, 1e18): dist[o] = nd; first[o] = t["id"]; heapq.heappush(q, (nd, o))
    for t in mains:
        ca = t["a"].get("chamber") if t["a"]["kind"] == "ch" else None; cb = t["b"].get("chamber") if t["b"]["kind"] == "ch" else None
        da = dist.get(ca, 1e18) if ca else 1e18; db = dist.get(cb, 1e18) if cb else 1e18
        t["to_hub"] = 1 if db < da else -1
        if min(da, db) >= 1e18: print(f"  HUB: main {t['id']} does not lead to the hub along the mains")
    HUB_TUBE.clear(); HUB_TUBE.update(first)
    print("  the way to the hub along the mains: " + ", ".join(f"{c} by {first[c]} ({dist[c]:.0f} m)" for c in sorted(first, key=lambda k: dist[k])))


# Round 25 (B2, Dan: "some of the tunnels are really long. Part of the underground network was to make it quicker ... This wouldn't
# be everywhere. for example races would move normally"): the PIPE EXPRESS. Free roam only (MapBuilder.NetworkWarp.cs builds the marks,
# Pipes/PipeExpress.cs carries you).
# Round 26 (item 3, Dan: "never tried warp but like boost. Would like to add it in more places anytime there is any distance"): an
# express on EVERY run of pipe longer than EXPRESS_RUN between two places where the driver has a choice or a reason to stop: a pipe's
# ends (a junction, the hub, an exit, a manhole's foot, the twin entrances' split) and a glass stretch's ends (glass is never express:
# it is for looking at) or a flooded sag's. Each starts and ends EXPRESS_IN short of those places and lets go at 20 m/s, so every turn
# is still the driver's and nothing along the network is skipped. No ride longer than about RIDE_MAX s: a long one goes faster ('top',
# m/s; PipeExpress uses the same speed-up and slow-down as ride_time() here). The first five keep their Round 25 ids and names.
EXPRESS_OLD = {"M6": ("E1", "M6 Nelson express"), "M4a": ("E2", "M4 Glen express"), "M4d": ("E3", "M4 J12 to J8 express"),
               "M3a": ("E4", "M3 Noonday express"), "B22": ("E5", "B22 Little Noonday express")}
EXPRESS_RUN = 100.0; EXPRESS_IN = 20.0; RIDE_MAX = 12.0
EXPRESS_BOOST = 45.0; EXPRESS_EXIT = 20.0; EXPRESS_SURGE = 30.0


def express_accel(top): return max(EXPRESS_SURGE, top / 2.5)       # PipeExpress.Accel


def ride_time(length, top, v0=12.0, dt=0.02):
    """the express's ride, as PipeExpress.Carry runs it: from v0 up to top, slowing to 20 m/s by the far end"""
    a = express_accel(top); v = max(8.0, v0); s = 0.0; t = 0.0
    while s < length - 1e-3 and t < 120:
        left = length - s; brake = (v * v - EXPRESS_EXIT ** 2) / (2 * a) + 3.0
        target = EXPRESS_EXIT if left < brake else top
        v = min(v + a * dt, target) if v < target else max(v - a * dt, target)
        s = min(length, s + v * dt); t += dt
    return t


def express_top(length):
    if ride_time(length, EXPRESS_BOOST) <= RIDE_MAX - 0.5: return EXPRESS_BOOST
    lo, hi = EXPRESS_BOOST, 400.0
    for _ in range(40):
        mid = 0.5 * (lo + hi)
        if ride_time(length, mid) <= RIDE_MAX - 0.5: hi = mid
        else: lo = mid
    return round(hi + 0.5)


def express(tubes, stage, exits):
    out = []; n = 6
    xname = {x["id"]: x["name"].title() for x in exits}
    def end_name(e):
        if e["kind"] == "ch": return e["chamber"]
        if e["kind"] == "mh": return e["manhole"] + "'s foot"
        if e["kind"] == "x": return xname.get(e["exit"], e["exit"])
        return e.get("pipe", "?") + " split"
    for t in tubes:
        if t["stage"] > stage: continue
        L = t["report"]["length"]
        stops = sorted([(g["s0"], g["s1"], f"glass under W{g['pond']}") for g in t["glass"]]
                       + ([(t["flood"]["s0"], t["flood"]["s1"], "the flooded sag")] if t.get("flood") else []))
        runs = []; a, an = 0.0, end_name(t["a"])
        for s0, s1, what in stops:
            runs.append((a, s0, an, what + "'s start")); a, an = s1, what + "'s end"
        runs.append((a, L, an, end_name(t["b"])))
        for a, b, an, bn in runs:
            if b - a < EXPRESS_RUN: continue
            s0, s1 = a + EXPRESS_IN, b - EXPRESS_IN
            if t["id"] in EXPRESS_OLD and len(runs) == 1: eid, name = EXPRESS_OLD[t["id"]]
            else: eid, name = f"E{n}", f"{t['id']} express"; n += 1
            top = express_top(s1 - s0)
            out.append(dict(id=eid, name=name, tube=t["id"], s0=round(s0, 1), s1=round(s1, 1), length=round(s1 - s0, 1), ends=f"{an} to {bn}",
                            d=t["d"], stage=t["stage"], top=top, ride=round(ride_time(s1 - s0, top), 1)))
    return sorted(out, key=lambda e: int(e["id"][1:]))


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--stage", type=int, default=None); ap.add_argument("--plot", action="store_true"); ap.add_argument("--out", default=None); a = ap.parse_args()
    global OUT
    if a.out: OUT = a.out
    old = json.load(open(OUT)) if os.path.exists(OUT) else {}
    stage = a.stage if a.stage is not None else old.get("build_stage", 0)
    tubes, chambers, exits, manholes = build(stage)
    doc = dict(note="Round 23 follow-ups: the underground pipe network as built. Made by Tools/mapgen/pipe_network.py; read by MapBuilder.Network.cs. "
                    "Tubes: 'pts' is the floor's (invert's) line x, y, z every meter (risers every 0.5 m); ends 'ch' (a chamber's wall, its opening "
                    "'wall' 0 +u, 1 +v, 2 -u, 3 -v of the chamber's frame, 't' along it), 'pipe' (an entrance barrel of Data/pipes/pipes.json), 'mh' "
                    "(a manhole riser up to the street), 'x' (an exit or outfall cut). Only what is at or under build_stage is built; a built "
                    "chamber's opening onto a pipe not built yet ends in a short stub at a closed grille.",
               build_stage=stage, tubes=tubes, chambers=chambers, exits=exits, manholes=manholes, differs=DIFFERS, express=express(tubes, stage, exits))
    clearance_report(tubes, chambers)
    json.dump(doc, open(OUT, "w", encoding="utf-8"), indent=0)
    if not a.out: write_doc(tubes, chambers, exits, manholes, stage, doc["express"])
    print(f"wrote {OUT}: build_stage {stage}; {len(tubes)} tubes, {len(chambers)} chambers, {len(exits)} exits, {len(manholes)} manholes")
    for t in tubes:
        r = t["report"]; mark = "" if t["stage"] <= stage else "   (not built yet)"
        warn = "  << LOW COVER" if r["cover_min"] < 0.6 else ""
        print(f"  {t['id']:5s} st{t['stage']} {t['d']:.1f} m {r['length']:6.0f} m  floor {r['floor_min']:.1f}-{r['floor_max']:.1f}  grade {r['grade']:.1f} %  cover >= {r['cover_min']:.1f} at {r['cover_at']}{warn}"
              + (f"  glass {[(g['pond'], g['s1'] - g['s0']) for g in t['glass']]}" if t["glass"] else "") + mark)
    for c in chambers:
        print(f"  {c['id']:4s} {c['kind']:8s} floor {c['floor']:.1f}  openings {len(c['openings'])}" + (f"  ceiling {c['ceiling']}" if 'ceiling' in c else ""))
    if a.plot: plot(tubes, chambers, exits, manholes, stage)


DOC = os.path.join(ROOT, "Docs/PIPE-NETWORK.md")
STAGE_NAMES = {0: "Part B: the two entrances", 1: "Stage 1: the hub, the trunk, the Enclave", 2: "Stage 2: Noonday", 3: "Stage 3: the Glen and the shops", 4: "Stage 4: the lakes"}


def write_doc(tubes, chambers, exits, manholes, stage, exp=()):
    """Docs/PIPE-NETWORK.md: the network as built (and what is still to come), for Dan to find his way round it"""
    md = ["# The underground pipe network, as built", "",
          f"Made by `Tools/mapgen/pipe_network.py` from its layout (Data/pipes/network.json); built by the map build "
          f"(MapBuilder.Network.cs, MapBuilder.NetworkHub.cs) up to **stage {stage}**. The plan it follows: Docs/PIPE-NETWORK-PROPOSAL.md. "
          "World, not race: there is no underground race, no gates and no switches (the race's proposal markers are kept: debug menu, "
          "Proposal markers).", "",
          "**Ways in**: BF (the creek channel west of Bells Ferry Road) and GP (the Chastain Glen pond's culvert), each two pipes that go off "
          "in different directions; every exit is a way in as well. Manholes open only from below.", "",
          "**Finding your way**: the mains carry a color stripe along both walls, with chevrons on it every 8 m pointing to the hub, their "
          "names are painted over their openings in the junctions (M1 red, M5 orange, M3 green, M4 yellow, M2 blue, M6 purple) and a "
          "HUB sign with an arrow stands beside the one that leads to the hub; every main ends in the hub; the maze branches are unmarked. Underground, the map "
          "(M or View) shows the pipes and the junctions', manholes' and exits' names.", ""]
    for st in range(0, 5):
        ts = [t for t in tubes if t["stage"] == st]
        if not ts: continue
        state = "BUILT" if st <= stage else "not built yet"
        md.append(f"## {STAGE_NAMES[st]} ({state})")
        md.append("")
        md.append("| Pipe | | Size | Length | Floor | Notes |")
        md.append("|---|---|---|---|---|---|")
        for t in ts:
            r = t["report"]; notes = []
            if t["glass"]: notes.append("glass under " + ", ".join(f"pond W{g['pond']} ({g['s1'] - g['s0']:.0f} m)" for g in t["glass"]))
            if t.get("flood"): notes.append("a flooded sag (slow)")
            for e in (t["a"], t["b"]):
                if e["kind"] == "mh": notes.append(f"pops out at {e['manhole']}")
                if e["kind"] == "x": notes.append(f"comes out at {next(x['name'] for x in exits if x['id'] == e['exit']).lower()}")
                if e["kind"] == "ch" and e.get("high"): notes.append(f"drops into {e['chamber']}")
            ends = f"{t['a'].get('chamber') or t['a'].get('pipe') or ''} to {t['b'].get('chamber') or t['b'].get('manhole') or t['b'].get('exit') or ''}"
            md.append(f"| {t['id']} | {t['name']} | {t['d']:.1f} m | {r['length']:.0f} m | {r['floor_min']:.0f} to {r['floor_max']:.0f} m | {ends}; {'; '.join(notes)} |")
        cs = [c for c in chambers if c["stage"] == st]
        if cs:
            md.append(""); md.append("Chambers: " + "; ".join(f"**{c['name']}** ({c['kind']}, x {c['x']:.0f}, z {c['z']:.0f}, floor {c['floor']:.0f} m)" for c in cs))
        ms = [m for m in manholes if m["stage"] == st]
        if ms: md.append(""); md.append("Manholes: " + "; ".join(f"**{m['id']}** in {m['road']} (x {m['x']:.0f}, z {m['z']:.0f}; riser {m['height']:.0f} m)" for m in ms))
        xs = [x for x in exits if x["stage"] == st]
        if xs: md.append(""); md.append("Exits: " + "; ".join(f"**{x['name'].title()}** (x {x['a'][0]:.0f}, z {x['a'][1]:.0f}{', a launch over the water' if x.get('outfall') else ''})" for x in xs))
        md.append("")
    if exp:
        md.append("## The pipe express (free roam only)"); md.append("")
        md.append("On every run of pipe longer than about 100 m between two places where you have a choice or a reason to stop (a junction, "
                  "the hub, an exit, a manhole's foot, the split behind BF or GP, either end of a glass stretch or a flooded sag), an **EXPRESS** "
                  "band painted round the pipe and arrows on the floor mark each end, 20 m short of those places. Drive in at either end and the "
                  "water surges: the controls lock and you are carried along the floor's middle (a whoosh, speed streaks), and let go at the far "
                  "end at 20 m/s, facing on out of it, so every turn is still yours. A long one goes faster, so no ride is longer than about 12 s. "
                  "Glass stretches are never express. Debug menu, **Pipe express**: Boost (the default), Warp (a short dark fade, then you are at "
                  "the far end) or Off. During a race it is off and these pipes drive as any other; the AI never uses it. On the map the express "
                  "stretches are drawn in cyan.")
        md.append("")
        md.append("| Express | Pipe | From | To | Length | Top speed | Ride | Driving it (20 m/s) |")
        md.append("|---|---|---|---|---|---|---|---|")
        for e in exp:
            fr, to = e["ends"].split(" to ", 1)
            md.append(f"| {e['id']} {e['name']} | {e['tube']} ({e['d']:.1f} m) | {fr} ({e['s0']:.0f} m along) | {to} ({e['s1']:.0f} m along) | {e['length']:.0f} m | {e['top']:.0f} m/s | about {e['ride']:.0f} s | {e['length'] / 20.0:.0f} s |")
        md.append("")
    md.append("## What differs from the proposal"); md.append("")
    for d in DIFFERS: md.append(f"- {d}")
    md.append("")
    open(DOC, "w", encoding="utf-8").write(chr(10).join(md))
    print("wrote", DOC)


def clearance_report(tubes, chambers):
    """pipes that cross another pipe or a chamber in plan must pass at least 1.5 m clear of it (shell to shell) over or under"""
    def pts3(t): p = t["pts"]; return [(p[i], p[i + 1], p[i + 2]) for i in range(0, len(p), 3)]
    P = {t["id"]: pts3(t) for t in tubes}
    bad = 0
    ids = list(P)
    for i, a in enumerate(ids):
        ta = next(t for t in tubes if t["id"] == a)
        for b in ids[i + 1:]:
            tb = next(t for t in tubes if t["id"] == b)
            if any(ta[k].get("kind") == "ch" and tb[j].get("kind") == "ch" and ta[k].get("chamber") == tb[j].get("chamber") for k in ("a", "b") for j in ("a", "b")):
                shared = True
            else: shared = False
            if ta["a"].get("kind") == "pipe" and tb["a"].get("kind") == "pipe" and ta["a"].get("pipe") == tb["a"].get("pipe"): shared = True   # an entrance's two barrels: side by side where they start
            pa, pb = P[a][::2], P[b][::2]
            for q in pa:
                for r in pb:
                    if abs(q[0] - r[0]) > 8 or abs(q[2] - r[2]) > 8: continue
                    dxz = math.hypot(q[0] - r[0], q[2] - r[2])
                    if dxz > (ta["d"] + tb["d"]) * 0.5 + 1.0: continue
                    if shared and min(math.hypot(q[0] - P[a][0][0], q[2] - P[a][0][2]), math.hypot(q[0] - P[a][-1][0], q[2] - P[a][-1][2])) < 25: continue
                    lo_a, hi_a = q[1], q[1] + ta["d"] + 0.3; lo_b, hi_b = r[1], r[1] + tb["d"] + 0.3
                    gap = max(lo_b - hi_a, lo_a - hi_b)
                    if gap < 1.5:
                        print(f"  CLEARANCE {a} x {b} at ({q[0]:.0f}, {q[2]:.0f}): {gap:.1f} m between them (floors {q[1]:.1f} / {r[1]:.1f})"); bad += 1; break
                else: continue
                break
    for t in tubes:
        own = {t["a"].get("chamber"), t["b"].get("chamber")}
        for c in chambers:
            if c["id"] in own: continue
            u = (math.sin(math.radians(c["yaw"])), math.cos(math.radians(c["yaw"]))); v = (u[1], -u[0]); hl, hw = c["length"] / 2 + 2, c["width"] / 2 + 2
            top = c.get("ceiling", c["floor"] + c.get("height", 7)) + 0.3
            for q in P[t["id"]][::2]:
                rel = (q[0] - c["x"], q[2] - c["z"])
                if abs(rel[0] * u[0] + rel[1] * u[1]) < hl + t["d"] / 2 and abs(rel[0] * v[0] + rel[1] * v[1]) < hw + t["d"] / 2:
                    if not (q[1] > top + 1.0 or q[1] + t["d"] + 0.3 < c["floor"] - 1.0):
                        print(f"  CLEARANCE {t['id']} through chamber {c['id']} at ({q[0]:.0f}, {q[2]:.0f})"); bad += 1; break
    print(f"  clearance: {bad} problem(s)")


def plot(tubes, chambers, exits, manholes, stage):
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(40, 21), dpi=50)
    for b in M["buildings"]:
        p = P2(b["footprint"]); ax.fill([x for x, z in p], [z for x, z in p], color="#d9cfc4", lw=0)
    for n, c, w, p in ROADS: ax.plot([x for x, z in p], [z for x, z in p], color="#aaa", lw=max(1, w / 3))
    for w in WATER: ax.fill([x for x, z in w], [z for x, z in w], color="#9cc6ef")
    for t in tubes:
        pp = t["pts"]; xs = pp[0::3]; zs = pp[2::3]
        col = t["stripe"] if t["stripe"] else ("#333" if t["d"] < 5 else "#666")
        ax.plot(xs, zs, color=col, lw=5 if t["d"] >= 5 else 2.5, alpha=1.0 if t["stage"] <= stage else 0.25)
        ax.text(xs[len(xs) // 2], zs[len(zs) // 2], t["id"], fontsize=9)
    for c in chambers:
        u = from_hdg(c["yaw"]); v = (u[1], -u[0]); hl, hw = c["length"] / 2, c["width"] / 2
        cs = [add(add((c["x"], c["z"]), u, a), v, b) for a, b in ((hl, hw), (hl, -hw), (-hl, -hw), (-hl, hw), (hl, hw))]
        ax.plot([q[0] for q in cs], [q[1] for q in cs], color="purple" if c["stage"] <= stage else "#caf")
        ax.text(c["x"], c["z"] + hw + 3, c["id"], fontsize=10, color="purple")
    for m in manholes: ax.scatter([m["x"]], [m["z"]], c="orange", s=60, zorder=5); ax.text(m["x"] + 4, m["z"], m["id"], fontsize=8)
    for x in exits: ax.scatter([x["a"][0]], [x["a"][1]], c="green", marker="^", s=80, zorder=5); ax.text(x["a"][0] + 4, x["a"][1], x["id"], fontsize=8)
    ax.set_aspect("equal"); ax.set_xlim(0, MW); ax.set_ylim(0, MD); ax.grid(alpha=0.3)
    os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True); plt.savefig(OUT_PNG, bbox_inches="tight"); print("wrote", OUT_PNG)


if __name__ == "__main__":
    main()
