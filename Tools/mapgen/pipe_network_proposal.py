"""Round 23, Part B: the map-wide underground pipe network, as a PROPOSAL (nothing here is built into the world).

Dan (2026-10-12): "I want a maze, with secret exits all over but a space for full on race as well. I envision a main hub
that is a big open chasm with water flowing in from all the sources. Love the idea of pop-out manhole covers as well."
And: "during underground races we can [have] gates sealing off other [areas] and potential shortcuts would require you to
drive over a switch or something to open them." The main entrance is decided and built: BF, two pipes from the end of the
creek channel west of Bells Ferry Road down INTO the network's first junction chamber, J1, under the far side of the road,
and J1's four onward stubs ending at closed grilles (Data/pipes/pipes.json; Round 23 items 2b and 2c). The proposal starts
the rest of the network at those grilles.

The layout below is hand-made (map x, z in meters, as Data/map.json). This script checks it against the map and writes:
  Data/proposals/round23_network.json   the markers (lines, labeled marks, go-to points) for MapBuilder.Proposals.cs, the
                                         in-game map layer and the labels (Tools/handmade/make_proposal_labels.py)
  Docs/PIPE-NETWORK-PROPOSAL.md         the proposal to read: every main, branch, junction, exit and manhole, what each
                                         passes under, lengths and depths, the race, the build stages, the frame-rate plan
  local/round23/network_overhead.png    the overhead picture (git-ignored, like every whole-map picture since Round 20)

Re-run after mapgen (it reads Assets/Generated/terrain.r16 for the depths). Then Tools/handmade/make_proposal_labels.py,
then Build Map.

Usage: python Tools/mapgen/pipe_network_proposal.py
"""
import json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_MARKS = os.path.join(ROOT, "Data/proposals/round23_network.json")
OUT_DOC = os.path.join(ROOT, "Docs/PIPE-NETWORK-PROPOSAL.md")
OUT_PNG = os.path.join(ROOT, "local/round23/network_overhead.png")

meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
W, D = M["size_x_m"], M["size_z_m"]


def ground(x, z):
    x = min(max(x, 0.0), W - 1.0); z = min(max(z, 0.0), D - 1.0)
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)


def pts(a): return [(a[i], a[i + 1]) for i in range(0, len(a), 2)]


ROADS = [(r["name"], r["class"], r["width"], pts(r["points"])) for r in M["roads"]]
HOUSES = [pts(b["footprint"]) for b in M["buildings"]]
WATER = [(w.get("name") or "", pts(w["outline"])) for w in M["water"]]
STREAMS = [(s.get("name") or "", pts(s["points"])) for s in M["streams"]]


def seg_dist(p, a, b):
    ax, az = a; bx, bz = b; px, pz = p
    dx, dz = bx - ax, bz - az; L2 = dx * dx + dz * dz
    t = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / L2))
    return math.hypot(ax + dx * t - px, az + dz * t - pz), (ax + dx * t, az + dz * t)


def on_road(name, near):
    """the nearest point of the named road's centerline to 'near' (a manhole sits in the road)"""
    best = (1e9, None)
    for n, _, _, p in ROADS:
        if n != name: continue
        for k in range(len(p) - 1):
            d, q = seg_dist(near, p[k], p[k + 1])
            if d < best[0]: best = (d, q)
    if best[1] is None: raise SystemExit(f"no road {name}")
    return (round(best[1][0], 1), round(best[1][1], 1))


def in_poly(p, poly):
    x, z = p; c = False; n = len(poly)
    for i in range(n):
        x1, z1 = poly[i]; x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z) and x < x1 + (z - z1) * (x2 - x1) / (z2 - z1): c = not c
    return c


def densify(line, step=2.0):
    out = [line[0]]
    for k in range(len(line) - 1):
        a, b = line[k], line[k + 1]; L = math.hypot(b[0] - a[0], b[1] - a[1]); n = max(1, int(math.ceil(L / step)))
        for i in range(1, n + 1): out.append((a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n))
    return out


def length(line): return sum(math.hypot(line[k + 1][0] - line[k][0], line[k + 1][1] - line[k][1]) for k in range(len(line) - 1))


def seg_cross(a, b, c, d):
    def o(p, q, r): return (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
    return (o(a, b, c) > 0) != (o(a, b, d) > 0) and (o(c, d, a) > 0) != (o(c, d, b) > 0)


# ================================================================ the layout (hand-made)
# Levels are floor heights (m, the map's own heights). The ground here is about 276 (Noonday Creek) to 305 m.
HUB = dict(c=(1900.0, 1145.0), hu=55.0, hv=35.0, floor=252.0, mid=262.0, top=272.0)   # the chasm: 110 x 70 m, its floor 30 m down

# kinds: mouth (a headwall you drive in by), exit (a secret way out), manhole (pop-out through a lid in a street),
# junction (a chamber under the ground), hub, shaft (a drop shaft), aquarium (a glass stretch under water), flooded (a
# flooded stretch: slow), outfall (a launch out of a headwall over water), sign (a painted junction sign), gate / switch
# (race layer), checkpoint / start (race layer)
NODES = {
    # --- the main entrance (BUILT, Round 23)
    "BF":  dict(kind="mouth", title="BF MAIN ENTRANCE", text="Built: twin pipes down", at=(1415.8, 1183.4), level=282.7),
    # --- junction chambers
    "J1":  dict(kind="junction", title="J1 FIRST CHAMBER", text="Built: 4 pipes, grilles", at=(1452.6, 1225.9), level=277.5),
    "J2":  dict(kind="junction", title="J2 POND JCT", text="Glass roof under pond", at=(1572.0, 1128.0), level=276.5),
    "J3":  dict(kind="junction", title="J3 ENCLAVE JCT", text="Chamber, 15 m down", at=(1785.0, 1000.0), level=270.0),
    "J4":  dict(kind="junction", title="J4 NOONDAY JCT", text="Chamber, 11 m down", at=(2280.0, 1215.0), level=266.0),
    "J5":  dict(kind="junction", title="J5 LAKE JCT", text="Glass dome under lake", at=(2352.0, 1470.0), level=268.0),
    "J6":  dict(kind="junction", title="J6 BRIDGE JCT", text="Under the bridges", at=(2398.0, 826.0), level=268.0),
    "J7":  dict(kind="junction", title="J7 GLEN JCT", text="Chamber, 11 m down", at=(2722.0, 975.0), level=276.0),
    "J8":  dict(kind="junction", title="J8 SHOPS JCT", text="Chamber, 9 m down", at=(3170.0, 1185.0), level=294.0),
    "J9":  dict(kind="junction", title="J9 LAKES JCT", text="Between the lakes", at=(872.0, 1240.0), level=290.0),
    "HUB": dict(kind="hub", title="THE HUB", text="Chasm 110 x 70 m, 33 m deep", at=HUB["c"], level=HUB["floor"]),
}

# mains: 5 m round, every vehicle; a color stripe each; painted signs at their junctions
MAINS = [
    dict(id="BF", name="BF Entrance (built)", stripe="none (built)", color=(1.0, 1.0, 1.0), stage=0,
         note="BUILT: two 5 m pipes from the end of the creek channel (Dan's BUG-002 spot, straight ahead) down at up to 18 % under Bells Ferry Road into J1",
         way=[("BF", None), ((1437.8, 1209.9), 277.5), ("J1", None)]),
    dict(id="M1", name="M1 Trunk", stripe="red", color=(0.90, 0.16, 0.16), stage=1,
         note="J1's south-east grille (built) to the hub along the creek valley; under the Enclave's north pond (W1) in glass (J2)",
         way=[("J1", None), ("G_M1", 277.5), ((1525.0, 1150.0), None), ("J2", None), ((1650.0, 1092.0), None),
              ((1760.0, 1102.0), None), ((HUB["c"][0] - HUB["hu"], 1150.0), HUB["mid"])]),
    dict(id="M5", name="M5 Enclave Main", stripe="orange", color=(1.0, 0.55, 0.05), stage=1,
         note="J1's other south-east grille (built) south along the east side of Bells Ferry Road, under the Enclave pond (W4) in glass, J3, into the hub's top ledge",
         way=[("J1", None), ("G_M5", 277.5), ((1488.0, 1120.0), None), ((1500.0, 1030.0), None), ((1615.0, 975.0), None), ((1638.0, 935.0), 279.0),
              ((1700.0, 960.0), None), ("J3", None), ((1850.0, 1075.0), None), ((HUB["c"][0] - 10.0, HUB["c"][1] - HUB["hv"]), HUB["top"])]),
    dict(id="M3", name="M3 Noonday Main", stripe="green", color=(0.15, 0.75, 0.25), stage=2,
         note="The hub's floor east under the woods to J4, under Noonday Creek's bed (glass), past P1 (a grate in P1's floor drops into it), J5 in a glass dome under the big lake, out over the water",
         way=[((HUB["c"][0] + HUB["hu"], 1140.0), HUB["floor"] + 2.0), ((2100.0, 1180.0), None), ("J4", None), ((2330.0, 1250.0), 268.5),
              ((2345.0, 1330.0), None), ("J5", None), ((2372.0, 1560.0), None), ("L_W15", None)]),
    dict(id="M4", name="M4 Glen Main", stripe="yellow", color=(1.0, 0.85, 0.1), stage=3,
         note="J4 south down the creek's west side to J6 under the New Chastain Road bridges, east to J7, under the Chastain Glen pond (W8) in glass, on to J8 behind the shopping center",
         way=[("J4", None), ((2290.0, 1060.0), None), ((2330.0, 930.0), None), ("J6", None), ((2560.0, 905.0), None), ("J7", None),
              ((2790.0, 905.0), 279.5), ((2860.0, 870.0), 279.5), ((2990.0, 1010.0), None), ("J8", None)]),
    dict(id="M2", name="M2 Lakes Main", stripe="blue", color=(0.18, 0.45, 1.0), stage=4,
         note="J1's north-west grille (built) back west under Bells Ferry Road (north of the entrance) to East Chastain Lake, along its bottom in glass, J9, under West Chastain Lake in glass, under the interstate to the far-west pond (W5)",
         way=[("J1", None), ("G_M2", 277.5), ((1385.0, 1222.0), None), ((1345.0, 1150.0), 284.0), ((1250.0, 1170.0), 284.5), ((1060.0, 1200.0), 284.5), ("J9", None),
              ((760.0, 1335.0), 291.0), ((600.0, 1390.0), 291.0), ((440.0, 1440.0), None), ((300.0, 1470.0), None), ("X_W5", None)]),
    dict(id="M6", name="M6 Nelson Main", stripe="purple", color=(0.62, 0.28, 0.95), stage=4,
         note="J9 south under Lakeside Drive and Chastain Road, down the I-575 side to Nelson Lake (south-west corner), under the interstate",
         way=[("J9", None), ((800.0, 1050.0), None), ((640.0, 900.0), None), ((520.0, 640.0), None), ((420.0, 360.0), None), ((330.0, 160.0), None),
              ("X_NELSON", None)]),
]

# maze branches: unmarked (no stripes, no signs), mostly 3.5 m (motorcycles, the ATV, the mower only), a few 5 m.
# 'to': the exit / manhole they end in (titles below), or None for a dead end or a link
B_NARROW, B_WIDE = 3.5, 5.0
BRANCHES = [
    # stage 1: the north maze between M1 and Parkwood Drive, and the Enclave's
    dict(id="B1", stage=1, d=B_WIDE, way=[("J2", None), ((1585.0, 1215.0), None), ((1700.0, 1232.0), None), ((1845.0, 1190.0), None), ((HUB["c"][0] - 30.0, HUB["c"][1] + HUB["hv"]), HUB["top"])], note="J2 north and east into the hub's top ledge (north side)"),
    dict(id="B2", stage=1, d=B_WIDE, way=[("J1", None), ("G_B", 277.5), ((1530.0, 1245.0), None), ("MH1", None)], note="J1's far grille (built) north to the Parkwood Drive west manhole"),
    dict(id="B2a", stage=1, d=B_NARROW, way=[((1530.0, 1245.0), None), ((1585.0, 1215.0), None)], note="a link from B2 to B1 (the north maze)"),
    dict(id="B3", stage=1, d=B_NARROW, way=[((1700.0, 1232.0), None), ((1740.0, 1255.0), None), ("MH2", None)], note="to the Parkwood Drive east manhole"),
    dict(id="B4", stage=1, d=B_NARROW, way=[((1650.0, 1092.0), None), ((1655.0, 1175.0), None), ((1640.0, 1222.0), None)], note="a link up from M1 to B1 (a drop shaft on it)"),
    dict(id="B5", stage=1, d=B_NARROW, way=[((1655.0, 1175.0), None), ((1610.0, 1180.0), None)], note="dead end (the maze)"),
    dict(id="B6", stage=1, d=B_NARROW, way=[("J3", None), ((1860.0, 960.0), None), ("MH3", None)], note="J3 to the Bloombridge Way manhole"),
    dict(id="B7", stage=1, d=B_NARROW, way=[("J3", None), ((1720.0, 890.0), None), ("MH4", None)], note="J3 to the Rotherhithe Landing manhole"),
    dict(id="B8", stage=1, d=B_NARROW, way=[((1720.0, 890.0), None), ((1560.0, 880.0), None), ((1430.0, 830.0), None), ("X_W20", None)], note="on under Bells Ferry Road to the pond by Chastain Road (W20)"),
    dict(id="B9", stage=1, d=B_NARROW, way=[((HUB["c"][0] + 40.0, HUB["c"][1] - HUB["hv"]), HUB["mid"]), ((1985.0, 1010.0), None), ("MH5", None)], note="the hub's mid ledge south-east to the Bloombridge Way east manhole"),
    dict(id="B10", stage=1, d=B_NARROW, way=[((1985.0, 1010.0), None), ((2060.0, 1060.0), None)], note="dead end (the maze)"),
    # stage 2: round J4 and J5
    dict(id="B11", stage=2, d=B_NARROW, way=[("J4", None), ((2310.0, 1150.0), None), ("X_TRAIL", None)], note="J4 up into the Noonday Creek woods trail"),
    dict(id="B12", stage=2, d=B_NARROW, way=[("J5", None), ((2440.0, 1440.0), None), ("X_W12", None)], note="J5 to the bank of the pond east of the big lake (W12)"),
    dict(id="B13", stage=2, d=B_NARROW, way=[("J5", None), ((2470.0, 1520.0), None), ("MH6", None)], note="J5 to the Guidhall Trail manhole"),
    dict(id="B14", stage=2, d=B_NARROW, way=[("J4", None), ((2420.0, 1240.0), None), ((2560.0, 1260.0), None), ("MH7", None)], note="J4 under the creek and the trail to the Westminister Way manhole"),
    dict(id="B15", stage=2, d=B_NARROW, way=[((2372.0, 1560.0), None), ((2425.0, 1640.0), None), ("X_W7", None)], note="M3's end to the pond at the top of the big lake (W7)"),
    # stage 3: round J6, J7, J8
    dict(id="B16", stage=3, d=B_NARROW, way=[("J6", None), ((2250.0, 720.0), None), ("MH8", None)], note="J6 under New Chastain Road to the Paxton Court manhole"),
    dict(id="B17", stage=3, d=B_NARROW, way=[("J7", None), ((2735.0, 880.0), None), ("MH9", None)], note="J7 to the Grasmere Court manhole"),
    dict(id="B18", stage=3, d=B_WIDE, way=[((2735.0, 880.0), None), ((2752.0, 845.0), None), ("L_W8", None)], note="an outfall launch out over the Chastain Glen pond"),
    dict(id="B19", stage=3, d=B_NARROW, way=[("J7", None), ((2650.0, 760.0), None), ("MH10", None), ((2690.0, 520.0), None), ("X_W11", None)], note="J7 south under New Chastain Road, the Weybourne Court manhole, on to the ponds south of the Commons (W11)"),
    dict(id="B26", stage=3, d=B_WIDE, way=[("J6", None), ("X_BRIDGE", None)], note="J6 out into the creek bank under the New Chastain Road bridges"),
    dict(id="B20", stage=3, d=B_NARROW, way=[("J8", None), ("MH11", None)], note="J8 to the Chasefield Way manhole"),
    dict(id="B21", stage=3, d=B_WIDE, way=[("J8", None), ("X_SHOPS", None)], note="J8 up a ramp to the storm drain behind the shopping center"),
    dict(id="B22", stage=3, d=B_NARROW, way=[("J8", None), ((3300.0, 1340.0), None), ((3560.0, 1440.0), None), ("X_LNC", None)], note="J8 under Canton Road to Little Noonday Creek"),
    # stage 4: round J9
    dict(id="B23", stage=4, d=B_NARROW, way=[("J9", None), ("MH12", None)], note="J9 to the Laurel Green Court manhole"),
    dict(id="B24", stage=4, d=B_NARROW, way=[("J9", None), ((960.0, 1000.0), None), ("MH13", None), ((1080.0, 880.0), None), ("X_W21", None)], note="J9 to the Chastain Lakes Drive manhole and the pond by Chastain Road (W21)"),
    dict(id="B25", stage=4, d=B_NARROW, way=[((600.0, 1390.0), None), ((520.0, 1450.0), None), ("X_STREAM", None)], note="M2 to the stream bank north of West Chastain Lake"),
]

# exits (secret ways out), manholes (pop-outs), outfalls; manholes are snapped onto their streets
# the four onward stubs out of J1, built in Round 23 and closed by grilles at these points until the network is built
GRILLES = {
    "G_M5": dict(kind="grille", title="GRILLE M5 (CLOSED)", text="Built: stub out of J1", at=(1467.4, 1206.4)),
    "G_M1": dict(kind="grille", title="GRILLE M1 (CLOSED)", text="Built: stub out of J1", at=(1474.0, 1214.0)),
    "G_B":  dict(kind="grille", title="GRILLE NORTH (CLOSED)", text="Built: stub out of J1", at=(1466.4, 1241.8)),
    "G_M2": dict(kind="grille", title="GRILLE M2 (CLOSED)", text="Built: stub out of J1", at=(1434.5, 1241.6)),
}
NODES.update(GRILLES)

EXITS = {
    "X_W20":   dict(kind="exit", title="EXIT W20 POND", text="Bank, by Chastain Rd", at=(1376.0, 812.0)),
    "X_TRAIL": dict(kind="exit", title="EXIT CREEK TRAIL", text="Up into the woods trail", at=(2342.0, 1112.0)),
    "X_W12":   dict(kind="exit", title="EXIT W12 POND", text="Bank, east of big lake", at=(2478.0, 1420.0)),
    "X_W7":    dict(kind="exit", title="EXIT W7 POND", text="Bank, top of big lake", at=(2420.0, 1668.0)),
    "X_BRIDGE": dict(kind="exit", title="EXIT UNDER BRIDGES", text="Creek bank, New Chastain", at=(2425.0, 790.0)),
    "X_W11":   dict(kind="exit", title="EXIT W11 POND", text="Bank, south of Commons", at=(2668.0, 410.0)),
    "X_SHOPS": dict(kind="exit", title="EXIT BEHIND SHOPS", text="Storm drain, north side", at=(3250.0, 1262.0)),
    "X_LNC":   dict(kind="exit", title="EXIT LITTLE NOONDAY", text="Creek bank, past Canton", at=(3752.0, 1468.0)),
    "X_W21":   dict(kind="exit", title="EXIT W21 POND", text="Bank, by Chastain Rd", at=(1098.0, 850.0)),
    "X_STREAM": dict(kind="exit", title="EXIT LAKE STREAM", text="Stream bank, north", at=(470.0, 1425.0)),
    "X_W5":    dict(kind="exit", title="EXIT FAR WEST POND", text="Bank, past I-575 (W5)", at=(178.0, 1488.0)),
    "X_NELSON": dict(kind="exit", title="EXIT NELSON LAKE", text="Bank, past I-575", at=(262.0, 110.0)),
    "L_W8":    dict(kind="outfall", title="LAUNCH GLEN POND", text="Headwall, over the water", at=(2760.0, 828.0)),
    "L_W15":   dict(kind="outfall", title="LAUNCH BIG LAKE", text="Headwall, over the water", at=(2386.0, 1602.0)),
}
MANHOLES = {
    "MH1": ("Parkwood Drive", (1520.0, 1276.0)), "MH2": ("Parkwood Drive", (1760.0, 1276.0)), "MH3": ("Bloombridge Way", (1880.0, 955.0)),
    "MH4": ("Rotherhithe Landing", (1700.0, 845.0)), "MH5": ("Bloombridge Way", (2010.0, 930.0)), "MH6": ("Guidhall Trail", (2520.0, 1540.0)),
    "MH7": ("Westminister Way", (2700.0, 1260.0)), "MH8": ("Paxton Court Northeast", (2124.0, 620.0)), "MH9": ("Grasmere Court Northeast", (2730.0, 800.0)),
    "MH10": ("Weybourne Court Northeast", (2690.0, 650.0)), "MH11": ("Chasefield Way", (3000.0, 1120.0)), "MH12": ("Laurel Green Court Northwest", (815.0, 1160.0)),
    "MH13": ("Chastain Lakes Drive Northwest", (960.0, 980.0)),
}
for k, (road, near) in MANHOLES.items():
    at = on_road(road, near)
    short = road.replace(" Northeast", "").replace(" Northwest", "")
    EXITS[k] = dict(kind="manhole", title=f"{k} MANHOLE", text=short, at=at, road=road)

# the pieces along the lines
FEATURES = [
    dict(kind="aquarium", title="GLASS UNDER W1 POND", text="M1, J2's glass roof", at=(1572.0, 1128.0), stage=1, line="M1"),
    dict(kind="aquarium", title="GLASS UNDER W4 POND", text="M5, along the pond", at=(1636.0, 945.0), stage=1, line="M5"),
    dict(kind="aquarium", title="GLASS UNDER CREEK", text="M3 under Noonday bed", at=(2330.0, 1250.0), stage=2, line="M3"),
    dict(kind="aquarium", title="GLASS UNDER BIG LAKE", text="M3, J5's dome", at=(2352.0, 1500.0), stage=2, line="M3"),
    dict(kind="aquarium", title="GLASS UNDER GLEN POND", text="M4 across the pond", at=(2825.0, 888.0), stage=3, line="M4"),
    dict(kind="aquarium", title="GLASS UNDER E CHASTAIN", text="M2 along the lake bed", at=(1160.0, 1185.0), stage=4, line="M2"),
    dict(kind="aquarium", title="GLASS UNDER W CHASTAIN", text="M2 along the lake bed", at=(680.0, 1362.0), stage=4, line="M2"),
    dict(kind="shaft", title="SHAFT P1 GRATE", text="P1's floor drops to M3", at=(2338.0, 1300.0), stage=2, line="M3"),
    dict(kind="shaft", title="SHAFT B4", text="8 m drop, B1 to M1", at=(1652.0, 1150.0), stage=1, line="B4"),
    dict(kind="shaft", title="SHAFT J7", text="10 m drop to M4", at=(2700.0, 940.0), stage=3, line="M4"),
    dict(kind="flooded", title="FLOODED B8", text="Slow, under Bells Ferry", at=(1470.0, 846.0), stage=1, line="B8"),
    dict(kind="flooded", title="FLOODED M4", text="Slow, by the creek", at=(2318.0, 980.0), stage=3, line="M4"),
    dict(kind="flooded", title="FLOODED M2", text="Slow, low under the lake", at=(1300.0, 1162.0), stage=4, line="M2"),
    dict(kind="sign", title="SIGNS AT J1", text="M1 TRUNK / M5 / M2", at=(1443.8, 1215.9), stage=1, line="M1"),
    dict(kind="sign", title="SIGNS AT J3", text="M5 ENCLAVE MAIN", at=(1795.0, 1010.0), stage=1, line="M5"),
]

# ================================================================ the underground race (race layer, stage 1)
# a lap through the hub: the hub floor -> M1 west -> J1 (a hairpin in the built chamber) -> M5 south along Bells Ferry Road ->
# W4's glass -> J3 -> the hub's top ledge -> down the ramps past the waterfalls -> the floor. Gates seal every other way;
# three shortcuts open only after a racer drives over their switch.
RACE_LINE = [(1900.0, 1165.0), (1870.0, 1170.0), (HUB["c"][0] - HUB["hu"] + 2.0, 1150.0), (1760.0, 1102.0),
             (1650.0, 1092.0), (1572.0, 1128.0), (1525.0, 1150.0), (1474.0, 1214.0), (1459.8, 1221.9), (1447.8, 1223.9), (1451.8, 1215.9),
             (1467.4, 1206.4), (1488.0, 1120.0), (1500.0, 1030.0), (1615.0, 975.0), (1638.0, 935.0), (1700.0, 960.0),
             (1785.0, 1000.0), (1850.0, 1075.0), (1880.0, 1112.0), (1945.0, 1114.0), (1950.0, 1140.0), (1930.0, 1162.0), (1900.0, 1165.0)]
RACE = [
    dict(kind="start", title="RACE START / FINISH", text="Hub floor, by the pool", at=(1900.0, 1165.0)),
    dict(kind="checkpoint", title="CP1 GLASS UNDER W1", text="M1, J2", at=(1580.0, 1124.0)),
    dict(kind="checkpoint", title="CP2 J1 CHAMBER", text="The hairpin in J1", at=(1447.8, 1227.9)),
    dict(kind="checkpoint", title="CP3 GLASS UNDER W4", text="M5, along the pond", at=(1640.0, 952.0)),
    dict(kind="checkpoint", title="CP4 ENCLAVE JCT", text="M5, J3", at=(1792.0, 1006.0)),
    dict(kind="checkpoint", title="CP5 HUB TOP LEDGE", text="South side", at=(1890.0, 1112.0)),
    dict(kind="gate", title="GATE G1 ENTRANCE", text="Seals BF's pipes (race)", at=(1437.8, 1209.9)),
    dict(kind="gate", title="GATE G2 M2 AT J1", text="Seals M2 (race)", at=(1439.8, 1235.9)),
    dict(kind="gate", title="GATE G10 NORTH AT J1", text="Seals B2 (race)", at=(1462.8, 1237.9)),
    dict(kind="gate", title="GATE G3 B1 AT J2", text="Seals the north maze", at=(1578.0, 1150.0)),
    dict(kind="gate", title="GATE G4 B4 ON M1", text="Seals the north maze", at=(1650.0, 1102.0)),
    dict(kind="gate", title="GATE G5 B6 AT J3", text="Seals the Enclave maze", at=(1800.0, 992.0)),
    dict(kind="gate", title="GATE G6 B7 AT J3", text="Seals the Enclave maze", at=(1774.0, 988.0)),
    dict(kind="gate", title="GATE G7 M3 AT HUB", text="Seals M3 east", at=(HUB["c"][0] + HUB["hu"] - 4.0, 1140.0)),
    dict(kind="gate", title="GATE G8 B1 AT HUB", text="Seals the north maze", at=(1870.0, 1176.0)),
    dict(kind="gate", title="GATE G9 B9 AT HUB", text="Seals the south-east maze", at=(1940.0, 1112.0)),
    dict(kind="gate", title="SHORTCUT GATE S1", text="Waterfall leap (hub)", at=(1880.0, 1114.0)),
    dict(kind="switch", title="SWITCH S1", text="Plate up M5's wall", at=(1830.0, 1055.0)),
    dict(kind="gate", title="SHORTCUT GATE S2", text="Pond tube (W4)", at=(1610.0, 978.0)),
    dict(kind="switch", title="SWITCH S2", text="Lever, side alcove", at=(1545.0, 1010.0)),
    dict(kind="gate", title="SHORTCUT GATE S3", text="High line over J2", at=(1650.0, 1100.0)),
    dict(kind="switch", title="SWITCH S3", text="Plate in J1's corner", at=(1488.0, 1184.0)),
]
SHORTCUTS = [
    dict(id="S1", name="Waterfall leap", line=[(1880.0, 1112.0), (1878.0, 1136.0), (1886.0, 1162.0), (1900.0, 1165.0)],
         what="off the end of the hub's top ledge, through M5's waterfall, 20 m down onto a long landing ramp on the floor beside the pool; skips the two ramps down (about 220 m)",
         switch="a pressure plate high on the wall of M5's barrel 80 m before the hub (ride the wall to hit it)", forgives="CP5 is before it: nothing"),
    dict(id="S2", name="Pond tube", line=[(1500.0, 1030.0), (1560.0, 990.0), (1610.0, 978.0), (1638.0, 935.0)],
         what="a straight glass tube through the middle of the Enclave pond (W4) where M5 bends round its end; a fish tank you blast through, a gap jump across a flooded sump at its far end",
         switch="a lever in a dead-end alcove off M5 just south of J1 (a 50 m detour)", forgives="none"),
    dict(id="S3", name="High line", line=[(1650.0, 1100.0), (1610.0, 1112.0), (1572.0, 1140.0), (1525.0, 1158.0)],
         what="a raised pipe off M1 that crosses J2's glass chamber on a gap jump over the floor below, instead of M1's S-bend down through it",
         switch="a pressure plate in J1's far corner, off the racing line", forgives="CP1 (the glass under W1)"),
]

GOTO = [
    dict(name="Main entrance (BF)", x=1413.47, z=1176.45, yaw=41.0),
    dict(name="Above the hub", x=1840.0, z=1145.0, yaw=90.0),
    dict(name="Parkwood Drive manholes", x=1640.0, z=1279.0, yaw=90.0),
    dict(name="Enclave (W4 pond)", x=1660.0, z=960.0, yaw=270.0),
    dict(name="Under the bridges (J6)", x=2410.0, z=840.0, yaw=180.0),
    dict(name="Glen pond (W8)", x=2760.0, z=812.0, yaw=0.0),
    dict(name="Behind the shops (J8)", x=3240.0, z=1275.0, yaw=270.0),
    dict(name="Between the lakes (J9)", x=900.0, z=1240.0, yaw=270.0),
]


# ================================================================ resolve, measure
def node_at(key):
    if key in NODES: return NODES[key]["at"], NODES[key].get("level")
    if key in EXITS: return EXITS[key]["at"], None
    raise SystemExit(f"unknown node {key}")


def resolve(way):
    """[(point or key, level or None)] -> points, levels (None where free)"""
    P, L = [], []
    for w, lv in way:
        if isinstance(w, str): p, l0 = node_at(w); P.append(tuple(p)); L.append(lv if lv is not None else l0)
        else: P.append(tuple(w)); L.append(lv)
    return P, L


def profile(P, L, diameter, ends, max_grade):
    """the floor along the line. Given levels (junctions, the hub's ledges, glass stretches) are kept; between them the floor
    runs straight, but never closer than 1.5 m of cover over the crown (it dips under a hill), and no steeper than max_grade
    where it is free. An end at an exit or outfall climbs out at 20 % in an open cut (a cut like P1's, walls either side);
    an end at a manhole stops 8 m under the street (the riser and its ramp are the manhole's own)."""
    dens = densify(P, 2.0); s = [0.0]
    for k in range(1, len(dens)): s.append(s[-1] + math.hypot(dens[k][0] - dens[k - 1][0], dens[k][1] - dens[k - 1][1]))
    n = len(s); g = [ground(x, z) for x, z in dens]
    ws = [0.0]
    for k in range(1, len(P)): ws.append(ws[-1] + math.hypot(P[k][0] - P[k - 1][0], P[k][1] - P[k - 1][1]))
    need = [gi - 1.5 - diameter - 0.3 for gi in g]   # the highest floor with 1.5 m of cover over the crown
    fixed = [(ws[k], L[k]) for k in range(len(P)) if L[k] is not None]
    def end_level(i, kind):
        if kind == "exit": return g[i]
        if kind == "manhole": return g[i] - 8.0
        return min(need[max(0, i - 4): i + 5])
    if not fixed or fixed[0][0] > 0.0: fixed.insert(0, (0.0, end_level(0, ends[0])))
    if fixed[-1][0] < s[-1] - 0.1: fixed.append((s[-1], end_level(n - 1, ends[1])))
    fs = [f[0] for f in fixed]; fl = [f[1] for f in fixed]
    lock = [any(abs(si - f) < 1.01 for f in fs) for si in s]
    floor = [min(float(np.interp(s[i], fs, fl)), need[i]) if not lock[i] else float(np.interp(s[i], fs, fl)) for i in range(n)]
    for _ in range(2):   # the grade limit, by lowering free stations only
        for i in range(1, n):
            if not lock[i]: floor[i] = min(floor[i], floor[i - 1] + max_grade * (s[i] - s[i - 1]))
        for i in range(n - 2, -1, -1):
            if not lock[i]: floor[i] = min(floor[i], floor[i + 1] + max_grade * (s[i + 1] - s[i]))
    for _ in range(2):   # and no steeper than max_grade away from a given level either (it comes up toward a shallow one in the open)
        for i in range(1, n):
            if not lock[i]: floor[i] = max(floor[i], floor[i - 1] - max_grade * (s[i] - s[i - 1]))
        for i in range(n - 2, -1, -1):
            if not lock[i]: floor[i] = max(floor[i], floor[i + 1] - max_grade * (s[i + 1] - s[i]))
    cut = [False] * n
    for side, kind in ((0, ends[0]), (1, ends[1])):
        if kind != "exit": continue
        idx = range(n) if side == 0 else range(n - 1, -1, -1)
        e = 0 if side == 0 else n - 1
        for i in idx:
            if i == e: cut[i] = True; continue
            up = g[e] - 0.20 * abs(s[i] - s[e])
            if up <= floor[i]: break
            floor[i] = up; cut[i] = True
    riser = [False] * n
    for side, kind in ((0, ends[0]), (1, ends[1])):
        if kind == "manhole":
            for i in range(n):
                if (s[i] < 4.0 if side == 0 else s[i] > s[-1] - 4.0): riser[i] = True
    cover = [g[i] - (floor[i] + diameter + 0.3) for i in range(n)]
    inner = [i for i in range(n) if not cut[i] and not riser[i]]
    worst = min(inner, key=lambda i: cover[i]) if inner else 0
    free = [i for i in range(n - 1) if not (lock[i] and lock[i + 1])]
    grade = max((abs(floor[i + 1] - floor[i]) / max(1e-6, s[i + 1] - s[i]) for i in range(n - 1)), default=0.0)
    deep = max(range(n), key=lambda i: g[i] - floor[i])
    wet = sum(s[i + 1] - s[i] for i in range(n - 1) if any(in_poly(dens[i], wp) for _, wp in WATER))
    opened = sum(s[i + 1] - s[i] for i in range(n - 1) if cover[i] < 0.6 and not riser[i])
    return dict(len=s[-1], gmin=min(g), gmax=max(g), fmin=min(floor), fmax=max(floor), grade=grade, cut=sum(1 for c in cut if c) * 2.0, opened=opened,
                cover_min=cover[worst] if inner else None, cover_at=dens[worst], deepest=g[deep] - floor[deep], deep_at=dens[deep], wet=wet)


def under(P, half):
    """roads (by name), houses, water bodies and streams the line passes under"""
    dens = densify(P, 2.0)
    roads, waters, streams = [], [], []
    for n, cls, w, rp in ROADS:
        if not n: continue
        hit = any(seg_cross(P[k], P[k + 1], rp[j], rp[j + 1]) for k in range(len(P) - 1) for j in range(len(rp) - 1))
        if hit and n not in roads: roads.append(n)
    houses = 0
    for hp in HOUSES:
        xs = [x for x, z in hp]; zs = [z for x, z in hp]
        cand = [q for q in dens if min(xs) - half <= q[0] <= max(xs) + half and min(zs) - half <= q[1] <= max(zs) + half]
        if any(in_poly(q, hp) for q in cand): houses += 1
    for n, wp in WATER:
        if any(in_poly(q, wp) for q in dens[::2]):
            idx = [i for i, (nn, pp) in enumerate(WATER) if pp is wp][0]
            waters.append(n or f"pond W{idx}")
    for n, sp in STREAMS:
        if any(seg_cross(P[k], P[k + 1], sp[j], sp[j + 1]) for k in range(len(P) - 1) for j in range(len(sp) - 1)):
            nn = n or "a stream"
            if nn not in streams: streams.append(nn)
    return roads, houses, waters, streams


def end_kind(w):
    if isinstance(w, str) and w in EXITS: return "manhole" if EXITS[w]["kind"] == "manhole" else "exit"
    return None


lines_out = []
report_lines = []
for m in MAINS:
    P, L = resolve(m["way"])
    pr = profile(P, L, 5.0, (end_kind(m["way"][0][0]), end_kind(m["way"][-1][0])), 0.10); u = under(P, 3.0)
    lines_out.append(dict(set="M", name=m["name"], points=[round(v, 2) for q in P for v in q], spacing=20.0, color=list(m["color"]), stage=m["stage"]))
    report_lines.append(("main", m, P, pr, u))
for b in BRANCHES:
    P, L = resolve(b["way"])
    pr = profile(P, L, b["d"], (end_kind(b["way"][0][0]), end_kind(b["way"][-1][0])), 0.16); u = under(P, b["d"] / 2 + 0.3)
    lines_out.append(dict(set="B", name=b["id"], points=[round(v, 2) for q in P for v in q], spacing=26.0, color=[0.93, 0.93, 0.93] if b["d"] < 5 else [0.75, 0.75, 0.78], stage=b["stage"]))
    report_lines.append(("branch", b, P, pr, u))
lines_out.append(dict(set="R", name="Underground race (lap)", points=[round(v, 2) for q in RACE_LINE for v in q], spacing=30.0, color=[0.85, 0.75, 1.0], stage=1))
for sc in SHORTCUTS:
    lines_out.append(dict(set="R", name=f"{sc['id']} {sc['name']}", points=[round(v, 2) for q in sc["line"] for v in q], spacing=12.0, color=[1.0, 0.72, 0.2], stage=1))
# the hub's outline as a line (a ring of posts)
c = HUB["c"]; hu, hv = HUB["hu"], HUB["hv"]
ring = [(c[0] - hu, c[1] - hv), (c[0] + hu, c[1] - hv), (c[0] + hu, c[1] + hv), (c[0] - hu, c[1] + hv), (c[0] - hu, c[1] - hv)]
lines_out.append(dict(set="H", name="The hub (outline)", points=[round(v, 2) for q in ring for v in q], spacing=12.0, color=[0.95, 0.25, 0.85], stage=1))

marks = []
def mark(set_, d, extra=""):
    marks.append(dict(set=set_, kind=d["kind"], title=d["title"], text=d["text"], x=round(d["at"][0], 2), z=round(d["at"][1], 2)))
for k, n in NODES.items(): mark("N", n)
for k, e in EXITS.items(): mark("N", e)
for f in FEATURES: mark("N", f)
for r in RACE: mark("R", r)

# stage of each node / exit (the lowest stage of a line through it)
def stage_of(key):
    st = 9
    for m in MAINS + BRANCHES:
        if any(isinstance(w, str) and w == key for w, _ in m["way"]): st = min(st, m["stage"])
    return st if st < 9 else 1

os.makedirs(os.path.dirname(OUT_MARKS), exist_ok=True)
doc = dict(note="Round 23, Part B: the underground pipe network PROPOSAL (TEMPORARY markers, nothing built but BF). Made by "
                "Tools/mapgen/pipe_network_proposal.py; read by MapBuilder.Proposals.cs and Tools/handmade/make_proposal_labels.py.",
           lines=lines_out, marks=marks, starts=GOTO)
json.dump(doc, open(OUT_MARKS, "w", encoding="utf-8"), indent=1)

# ================================================================ the readable proposal
def fmt_u(u):
    roads, houses, waters, streams = u
    out = []
    if roads: out.append("roads: " + ", ".join(roads))
    if houses: out.append(f"{houses} house{'s' if houses != 1 else ''}")
    if waters: out.append("water: " + ", ".join(waters))
    if streams: out.append("streams: " + ", ".join(streams))
    return "; ".join(out) if out else "woods and yards only"

tot = {"main": 0.0, "branch": 0.0}
for kind, d, P, pr, u in report_lines: tot[kind] += pr["len"]
md = []
md.append("# The underground pipe network: proposal (Round 23, Part B)\n")
md.append("Made by `Tools/mapgen/pipe_network_proposal.py` (re-run after mapgen). Nothing in this document is built except the main "
          "entrance: BF, two pipes from the end of the creek channel west of Bells Ferry Road down into the first junction chamber, "
          "J1, under the far side of the road, whose four onward stubs end at closed grilles (no way out on the far side of the road). "
          "The network below starts at those grilles. Dan picks and changes; then it is built stage by stage.\n")
md.append("Picture: `local/round23/network_overhead.png` (git-ignored). In the game: debug menu (F6 or `\\`), **Proposal markers: Shown**; "
          "**Go to** puts you at a stop (free roam). The map (M or View) draws every line and its name while the markers exist.\n")
md.append("## What it is\n")
md.append(f"- **Mains**: {len(MAINS)} lines, {tot['main']:.0f} m, 5 m round (every vehicle fits), a color stripe each, painted signs at their junctions.")
md.append(f"- **Maze branches**: {len(BRANCHES)} lines, {tot['branch']:.0f} m, unmarked; most 3.5 m round (motorcycles, the ATV and the mower only), a few 5 m.")
md.append(f"- **The hub**: a chasm {2 * hu:.0f} x {2 * hv:.0f} m under the woods north of the creek valley at x {c[0]:.0f}, z {c[1]:.0f}; its floor at {HUB['floor']:.0f} m "
          f"(about {ground(*c) - HUB['floor']:.0f} m under the ground), a mid ledge at {HUB['mid']:.0f} m and a top ledge at {HUB['top']:.0f} m round the walls, joined by ramps; "
          "every main pours its water in as a waterfall off its ledge into a pool on the floor; lit from below and by shafts of daylight through grates.")
md.append(f"- **Junctions**: {sum(1 for n in NODES.values() if n['kind'] == 'junction')} chambers. **Exits**: {sum(1 for e in EXITS.values() if e['kind'] == 'exit')} secret ways out, "
          f"{sum(1 for e in EXITS.values() if e['kind'] == 'outfall')} outfall launches over water, **manholes**: {sum(1 for e in EXITS.values() if e['kind'] == 'manhole')} pop-outs in residential streets.")
md.append("- Glass (aquarium) stretches wherever a main passes under a pond or lake (as P1), drop shafts, flooded stretches that slow you, "
          "the map showing the pipes when you are underground.\n")
md.append("## The hub\n")
md.append("```")
md.append("            north wall: B1 in at the top ledge (north maze), its waterfall")
md.append("   +------------------------------------------------------------+")
md.append("   | top ledge 272 ........................................     |")
md.append("   |   mid ledge 262 ...............................            |")
md.append(" M1| ==> (west wall, mid ledge)        [ POOL ]      floor 252 M3 ==> east (floor)")
md.append("   |   ramp mid -> floor (north-west)    S/F  <- race           |")
md.append("   |   ramp top -> floor (east wall)                            |")
md.append("   | top ledge 272 ..... S1 lip (waterfall leap) ...            |")
md.append("   +--------------------^---------------------------------------+")
md.append("            south wall: M5 in at the top ledge (race), B9 out at the mid ledge")
md.append("```")
md.append("Waterfalls: M1 (west), M5 (south), B1 (north) pour off their ledges; the pool on the floor takes them; M3 carries it on east "
          "to Noonday Creek. Its own space to drive round: the ledges, the ramps, the floor, the pool (shallow: it slows, never traps).\n")
md.append("## Mains\n")
md.append("| Line | Stage | Length | Passes under | Floor | Deepest under ground | Under water (glass) | Open cut at its ends | |")
md.append("|---|---|---|---|---|---|---|---|---|")
for kind, d, P, pr, u in report_lines:
    if kind != "main": continue
    if d["id"] == "BF":   # built: its own numbers (the map build's log)
        md.append(f"| **BF Entrance** (BUILT) | built | 54 m | {fmt_u(u)} | from the creek bed (283.5 m) down at up to 18 % to J1's floor (277.5 m) | 12 m | 0 m | 12 m (the mouth's cut) | {d['note']} |")
        continue
    md.append(f"| **{d['name']}** ({d['stripe']} stripe) | {d['stage']} | {pr['len']:.0f} m | {fmt_u(u)} | {pr['fmin']:.0f} to {pr['fmax']:.0f} m, grade up to {pr['grade'] * 100:.0f} % | "
              f"{pr['deepest']:.0f} m | {pr['wet']:.0f} m | {pr['opened']:.0f} m | {d['note']} |")
md.append("\nDepths: the floor is fitted under the ground with at least 1.5 m over the crown, given levels kept at the junctions, the hub "
          "and the glass stretches; grades are at most 10 % on the mains and 16 % in the maze. 'Open cut' is where it comes up to a "
          "mouth or an exit between walls, as P1's cuts. 'Passes under' is what is on the ground over it (nothing up there changes).")
md.append("\n## Maze branches\n")
md.append("| Branch | Stage | Size | Length | Passes under | Deepest | Grade | Open cut at its ends | |")
md.append("|---|---|---|---|---|---|---|---|---|")
for kind, d, P, pr, u in report_lines:
    if kind != "branch": continue
    md.append(f"| {d['id']} | {d['stage']} | {'3.5 m (bikes, ATV, mower)' if d['d'] < 5 else '5 m'} | {pr['len']:.0f} m | {fmt_u(u)} | {pr['deepest']:.0f} m | up to {pr['grade'] * 100:.0f} % | {pr['opened']:.0f} m | {d['note']} |")
md.append("\n## Junctions, exits, manholes and the pieces along the way\n")
md.append("| Marker | Kind | Where (x, z) | Stage | |")
md.append("|---|---|---|---|---|")
for k, n in NODES.items(): md.append(f"| {n['title']} | {n['kind']} | {n['at'][0]:.0f}, {n['at'][1]:.0f} | {1 if k in ('BF', 'J1', 'J2', 'J3', 'HUB') else stage_of(k)} | {n['text']} |")
for k, e in EXITS.items():
    extra = f" (pops out in {e['road']})" if e["kind"] == "manhole" else ""
    md.append(f"| {e['title']} | {e['kind']} | {e['at'][0]:.0f}, {e['at'][1]:.0f} | {stage_of(k)} | {e['text']}{extra} |")
for f in FEATURES: md.append(f"| {f['title']} | {f['kind']} | {f['at'][0]:.0f}, {f['at'][1]:.0f} | {f['stage']} | {f['text']} |")
md.append("\nManholes: the branch ends in a riser shaft under the street with a steep ramp (about 30 degrees) up it; the lid breaks "
          "and flies off and the vehicle pops out onto the road with a clear run; from the street the lid is flush (a car drives "
          "over it), the way in is the exits. Exits: a small headwall or a grate in a bank, half hidden (secret), each a way in as well.\n")
rl = length(RACE_LINE)
md.append("## The underground race (race layer, stage 1)\n")
md.append(f"A lap of about {rl:.0f} m, raced from the menu with the AI (a route file like the others; its own checkpoints): from the hub floor by "
          "the pool, up the north-west ramp onto the mid ledge, M1 west under the Enclave's north pond (glass, CP1), into J1 through "
          "its M1 grille (open for the race once built), a hairpin round the chamber (CP2), out through the M5 grille beside it, "
          "south along the east side of Bells Ferry Road, along the Enclave pond's glass (CP3), J3 "
          "(CP4), into the hub on its top ledge (CP5), down the ramps past the waterfalls to the floor.\n")
md.append("- **Gates** (barred grilles) close every other way while that race runs (G1 to G10 on the markers): BF's two "
          "entrance pipes and J1's M2 and north grilles, the maze branches at J2, J3 and the hub, M3. Out of a race they "
          "stand open (free roam, the maze).")
md.append("- **Switch shortcuts**: each behind a gate that opens when any racer drives over its switch (it stays open for that lap); "
          "the AI follows the shortcut rule (CLAUDE.md) and may go for a switch when it falls behind.\n")
for sc in SHORTCUTS:
    md.append(f"  - **{sc['id']} {sc['name']}**: {sc['what']}. Switch: {sc['switch']}. Forgives: {sc['forgives']}.")
md.append("\n## Build stages\n")
md.append("1. **Stage 1: the hub, the trunk, the race.** BF and J1 with its four stubs (built in Round 23), M1 with W1's glass, M5 with W4's glass and J3, the hub with "
          "its ledges, ramps, waterfalls and pool, the north and Enclave mazes (B1 to B10, five manholes, the W20 exit), the race route "
          "with checkpoints, gates and the three switch shortcuts.")
md.append("2. **Stage 2: Noonday.** M3 to J4 and J5 (glass under the creek and the big lake, P1's grate), the outfall over the big lake, "
          "B11 to B15 (the woods trail exit, W12, W7, two manholes).")
md.append("3. **Stage 3: the Glen and the shops.** M4 to J6 (the exit under the bridges), J7 and J8 (glass under the Chastain Glen "
          "pond), B16 to B22 (the launch over the pond, four manholes, behind the shopping center, Little Noonday Creek).")
md.append("4. **Stage 4: the lakes.** M2 (glass along both Chastain lakes, J9, under the interstate to W5), M6 to Nelson Lake, B23 to B25.\n")
md.append("## Frame-rate plan\n")
md.append("- Nothing underground is drawn or simulated when you are not near it: the network is cut into cells (the hub, each "
          "junction, each 150 m of pipe); a cell's meshes, lamps, streams and waterfalls are switched on only while the player (or "
          "the camera) is in it or the next one, or looking into its mouth from the surface within 60 m.")
md.append("- Lamps: point lights only in the cells switched on (at most about 12 at once); no shadows underground; the vehicles' "
          "own headlights as in P1.")
md.append("- Water: one scrolling-texture stream per pipe (as P1); waterfalls as scrolling sheets with a few particles; glass "
          "stretches as P1's (fish only while you are in that cell).")
md.append("- AI racers underground use the race line (no physics shortcuts); their pipe sounds only within earshot.")
md.append("- From the surface the network costs nothing but its mouths, exits and manhole lids (flush).\n")
md.append("## Versatile (the world, not the race)\n")
md.append("The pipes, chambers, hub, exits and manholes are world: data files and the map build, like P1. The race's gates, "
          "switches, checkpoints and the route are a race layer that comes and goes with the race (as `RaceBarriers` and "
          "`RaceOnly`); nothing in the network assumes it is only a race track.\n")
open(OUT_DOC, "w", encoding="utf-8").write("\n".join(md))

# ================================================================ the overhead picture
import matplotlib; matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
fig, ax = plt.subplots(figsize=(40, 21), dpi=60)
for hp in HOUSES: ax.fill([x for x, z in hp], [z for x, z in hp], color="#d9cfc4", lw=0)
for n, cls, w, rp in ROADS: ax.plot([x for x, z in rp], [z for x, z in rp], color="#8a8a8a" if w >= 9 else "#b5b5b5", lw=max(1.0, w / 3.2), solid_capstyle="round", zorder=1)
for n, wp in WATER: ax.fill([x for x, z in wp], [z for x, z in wp], color="#9cc6ef", zorder=2)
for n, sp in STREAMS: ax.plot([x for x, z in sp], [z for x, z in sp], color="#5b9bd8", lw=2.5, zorder=2)
seen = set()
for n, cls, w, rp in ROADS:
    if n and n not in seen and w >= 9: seen.add(n); x, z = rp[len(rp) // 2]; ax.text(x, z + 8, n, fontsize=10, color="#555", zorder=3)
for kind, d, P, pr, u in report_lines:
    if kind == "main":
        ax.plot([x for x, z in P], [z for x, z in P], color="black", lw=9, zorder=4, solid_capstyle="round")
        ax.plot([x for x, z in P], [z for x, z in P], color=d["color"], lw=6, zorder=5, solid_capstyle="round", label=f"{d['name']} ({pr['len']:.0f} m, stage {d['stage']})")
        mx, mz = P[len(P) // 2]; ax.text(mx, mz + 14, d["id"], fontsize=16, weight="bold", color=d["color"], zorder=9, bbox=dict(fc="white", ec="none", alpha=0.7, pad=1))
    else:
        ax.plot([x for x, z in P], [z for x, z in P], color="#333", lw=3.5 if d["d"] >= 5 else 2.4, ls=(0, (5, 2)), zorder=4)
        mx, mz = P[len(P) // 2]; ax.text(mx + 6, mz + 6, d["id"], fontsize=10, color="#222", zorder=9)
ax.plot([x for x, z in RACE_LINE], [z for x, z in RACE_LINE], color="#b48cff", lw=2.5, zorder=6, label=f"Underground race, one lap ({rl:.0f} m)")
for sc in SHORTCUTS: ax.plot([x for x, z in sc["line"]], [z for x, z in sc["line"]], color="#ffb833", lw=3, ls=(0, (2, 1)), zorder=6)
ax.add_patch(Rectangle((c[0] - hu, c[1] - hv), 2 * hu, 2 * hv, fc="#f040d8", ec="black", lw=2, alpha=0.55, zorder=5))
ax.text(c[0], c[1] + hv + 12, "THE HUB", ha="center", fontsize=18, weight="bold", color="#a0108c", zorder=9)
STY = {"grille": ("8", "#ff3030", 170), "mouth": ("s", "#ffffff", 220), "junction": ("o", "#f0f0f0", 170), "hub": ("*", "#f040d8", 1), "exit": ("^", "#22aa44", 170),
       "manhole": ("o", "#ff7a00", 150), "outfall": ("v", "#1a6ad0", 190), "aquarium": ("D", "#55d0ff", 110), "shaft": ("X", "#8a2be2", 140),
       "flooded": ("h", "#3b6e9e", 130), "sign": ("P", "#ffd700", 110), "gate": ("|", "#d01010", 260), "switch": ("p", "#ffb833", 170),
       "checkpoint": ("o", "#2a9a3a", 120), "start": ("s", "#000000", 160)}
for mk in marks:
    if mk["kind"] == "hub": continue
    sym, col, sz = STY.get(mk["kind"], ("o", "white", 100))
    ax.scatter([mk["x"]], [mk["z"]], marker=sym, s=sz, c=col, edgecolors="black", linewidths=1.2, zorder=8)
    if mk["kind"] in ("exit", "manhole", "outfall", "junction", "mouth", "grille"):
        ax.text(mk["x"] + 8, mk["z"] - 16, mk["title"].replace(" MANHOLE", ""), fontsize=9, color="#111", zorder=9, bbox=dict(fc="white", ec="none", alpha=0.6, pad=0.5))
from matplotlib.lines import Line2D
legend2 = [Line2D([0], [0], color="#333", lw=2.4, ls=(0, (5, 2)), label="maze branch (unmarked; 3.5 m: bikes, ATV, mower; thicker: 5 m)"),
           Line2D([0], [0], color="#ffb833", lw=3, ls=(0, (2, 1)), label="switch shortcut (race)")]
for k, (sym, col, sz) in STY.items():
    if k == "hub": continue
    legend2.append(Line2D([0], [0], marker=sym, color="w", markerfacecolor=col, markeredgecolor="black", markersize=12, label=k))
l1 = ax.legend(loc="upper left", fontsize=14, framealpha=0.9)
ax.add_artist(l1)
ax.legend(handles=legend2, loc="upper right", fontsize=12, framealpha=0.9, ncol=2)
ax.set_title(f"Chastain Rush, Round 23: the underground pipe network PROPOSAL. Mains {tot['main']:.0f} m, maze {tot['branch']:.0f} m, "
             f"{sum(1 for e in EXITS.values() if e['kind'] == 'exit')} exits, {sum(1 for e in EXITS.values() if e['kind'] == 'manhole')} manholes, "
             f"{sum(1 for e in EXITS.values() if e['kind'] == 'outfall')} launches. BF (main entrance) is built; everything else is a proposal.   "
             "Map data © OpenStreetMap contributors", fontsize=16)
ax.set_xlim(0, W); ax.set_ylim(0, D); ax.set_aspect("equal"); ax.set_xticks(range(0, 4001, 250)); ax.set_yticks(range(0, 1919, 250)); ax.grid(alpha=0.25)
# inset: the hub and the race round it
ia = fig.add_axes([0.70, 0.07, 0.24, 0.30])
ia.add_patch(Rectangle((c[0] - hu, c[1] - hv), 2 * hu, 2 * hv, fc="#f6d6f1", ec="black", lw=2))
ia.add_patch(Rectangle((c[0] - hu + 6, c[1] - hv + 6), 2 * hu - 12, 2 * hv - 12, fc="none", ec="#a0108c", lw=1.5, ls="--"))
ia.add_patch(Rectangle((c[0] - hu + 14, c[1] - hv + 14), 2 * hu - 28, 2 * hv - 28, fc="#efe2ec", ec="#a0108c", lw=1, ls=":"))
ia.add_patch(Rectangle((c[0] - 14, c[1] - 6), 28, 18, fc="#9cc6ef", ec="#1a6ad0"))
ia.text(c[0], c[1] + 3, "POOL", ha="center", fontsize=9, color="#1a6ad0")
ia.plot([x for x, z in RACE_LINE], [z for x, z in RACE_LINE], color="#b48cff", lw=2.5)   # clipped to the inset by its limits
for sc in SHORTCUTS[:1]: ia.plot([x for x, z in sc["line"]], [z for x, z in sc["line"]], color="#ffb833", lw=3, ls=(0, (2, 1)))
ia.text(c[0] - hu - 2, 1150, "M1 (mid ledge 262)", ha="right", fontsize=8); ia.text(c[0] + hu + 2, 1140, "M3 (floor 254)", fontsize=8)
ia.text(c[0] - 10, c[1] - hv - 9, "M5 in (top ledge 272)", ha="center", fontsize=8); ia.text(c[0] - 30, c[1] + hv + 3, "B1 (top ledge)", ha="center", fontsize=8)
ia.text(c[0] + 40, c[1] - hv - 9, "B9 (mid)", ha="center", fontsize=8); ia.text(c[0] + 2, c[1] + 22, "S/F", fontsize=9, weight="bold")
ia.text(1866, 1104, "S1 lip", fontsize=8, color="#b07000")
ia.set_xlim(c[0] - hu - 45, c[0] + hu + 45); ia.set_ylim(c[1] - hv - 22, c[1] + hv + 14); ia.set_aspect("equal"); ia.set_xticks([]); ia.set_yticks([])
ia.set_title("The hub (plan): top ledge (solid edge), mid ledge (dashed), floor (dotted), the race in lavender", fontsize=10)
os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)
plt.savefig(OUT_PNG, bbox_inches="tight")
print(f"wrote {OUT_MARKS}: {len(lines_out)} lines, {len(marks)} marks, {len(GOTO)} go-to points")
print(f"wrote {OUT_DOC}; {OUT_PNG}")
print(f"mains {tot['main']:.0f} m, branches {tot['branch']:.0f} m, race lap {rl:.0f} m")
for kind, d, P, pr, u in report_lines:
    cv = f"{pr['cover_min']:.1f}" if pr["cover_min"] is not None else "-"
    print(f"  {d.get('id')}: {pr['len']:.0f} m, open {pr['opened']:.0f} m, wet {pr['wet']:.0f} m, floor {pr['fmin']:.0f}-{pr['fmax']:.0f}, grade {pr['grade'] * 100:.0f} %, deepest {pr['deepest']:.0f}, min cover {cv} at {tuple(round(v) for v in pr['cover_at'])}; {fmt_u(u)}")
