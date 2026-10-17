"""Round 36 (Part A2, A3): the stunt park on the shopping center's lot (Dan: "change the Kroger parking lot into a stunt course ... where you can
get up on top of the building area with stunts up there. Would like to get big air on these"; "The course should seem like someone specifically
designed it for the purpose of being a stunt course", inspired by the stunt arena of Rush 2: Extreme Racing USA, copied from none).

Writes Data/tracks/stunt_park.json, read by Assets/Editor/MapBuilder.StuntPark.cs (Build Map). Re-run after mapgen (it reads
Assets/Generated/terrain.r16 and Data/map.json), then Build Map. With --plot also local/round36/park_layout.png (git-ignored).

Everything is laid out in the store's own frame: u along the shop row (east-north-east), v across it (north-north-west); map meters, as Unity
x / z (world = u U + v V). The supermarket's front is the line u = 3455.5, the shop row's front v = 469.5.

Heights. The park's floor is graded to one gently tilted plane (Racer's park is one level floor; this lot rises 4 m to the east, so the floor
keeps that tilt, 2 % east and 3 % south) with a level apron 8 m deep in front of the stores at the stores' own floor (FRONT_Y, the ground at the
supermarket's doors), so every door and smashed window is flush. The store interiors are level at FRONT_Y; the roofs level at ROOF_Y (the
building's height over its mean ground, as the map build had it). The knoll by Chastain Corner (up to 4 m over the plane) is left as it is, a
paved berm inside the park's catch fence. Every ramp's profile is drawn along its lane over that floor (Racer's turtle profiles: one mesh per
ramp, no seams); a roof feature over the roof.

Big air (A3, Dan: "Would like to get big air on these"): the launches off the roofs are aimed (BigJump: every vehicle crossing the lip at 3 m/s
or more leaves at the design speed and angle, as the Chastain Ridge leap), so each flight is known and lands on a down-slope sized to it: the
landing's slope matches the flight's angle where it comes down (Racer's ballistic sizing, Tools/Report106 size.py), its table catches a short
one, its run-out is clear. The floor's kickers and tables are Racer's (Racer's own sizes, natural physics). The map build re-does the landing
sizing on the graded ground (MapBuilder.StuntPark.cs) and reports each flight; this script prints the same numbers for the layout.
"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/stunt_park.json")
G = 9.81
U = (0.9780962345219478, 0.20815320322778294); V = (-U[1], U[0])
def W(u, v): return (u * U[0] + v * V[0], u * U[1] + v * V[1])
def UV(x, z): return (x * U[0] + z * U[1], x * V[0] + z * V[1])
def D(du, dv):   # a direction given in (u, v), in world (x, z)
    x, z = du * U[0] + dv * V[0], du * U[1] + dv * V[1]; n = math.hypot(x, z); return (x / n, z / n)

meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
CENTER = 2   # the supermarket and shop row (one footprint, retail, 9 m)
fp = m["buildings"][CENTER]["footprint"]; RING = [(fp[i], fp[i + 1]) for i in range(0, len(fp), 2)]
assert m["buildings"][CENTER]["type"] == "retail", "building 2 is not the shopping center any more: re-check the index"

# ------------------------------------------------------------------ the frame, the floor, the roof
FRONT_U, ROW_V = 3455.5, 469.5
ROOF_Y = round(sum(ground(x, z) for x, z in RING) / len(RING) + m["buildings"][CENTER]["height"], 3)
# the floor polygon (u, v): the lot east of the stores, the knoll strip by Chastain Corner left out, round the stores' fronts
FLOOR_UV = [(3449.0, 333.0), (3486.7, 340.1), (3515.0, 341.8), (3515.0, 368.0), (3590.0, 368.0), (3590.0, 345.3), (3635.6, 345.7), (3637.5, 362.0),
            (3637.5, 447.2), (3621.3, 466.5), (3592.4, 469.5), (3458.4, 469.5), (3448.5, 460.1), (3448.5, 444.1), (3454.6, 444.2), (3455.5, 363.7),
            (3448.5, 363.5), (3448.9, 348.1)]
# the park's ground for the scoring (StuntPark.Inside): the lot east of the stores (the knoll included) and the building
LOT_SOUTH_UV = [(3449.0, 324.0), (3486.7, 338.0), (3546.7, 341.5), (3579.7, 342.6), (3597.1, 343.0), (3638.1, 343.4)]
OUTLINE_UV = LOT_SOUTH_UV + [(3640.0, 362.2), (3640.0, 448.1), (3622.8, 468.2)] + [UV(*p) for p in RING[0:28]]
# the plane, fitted to the ground of the floor (the gas station's pad and the bank left out)
def inside(p, poly):
    x, y = p; c = False
    for i in range(len(poly)):
        a, b = poly[i], poly[i - 1]
        if (a[1] > y) != (b[1] > y) and x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]: c = not c
    return c
CANOPY_UV = (3571.0, 3599.4, 420.1, 433.5); BANK_UV = (3592.7, 3609.7, 359.1, 393.3)
pts = []
for u in np.arange(3452, 3637, 3.0):
    for v in np.arange(334, 468, 3.0):
        if not inside((u, v), FLOOR_UV): continue
        if CANOPY_UV[0] - 3 < u < CANOPY_UV[1] + 3 and CANOPY_UV[2] - 3 < v < CANOPY_UV[3] + 3: continue
        if BANK_UV[0] - 3 < u < BANK_UV[1] + 3 and BANK_UV[2] - 3 < v < BANK_UV[3] + 3: continue
        pts.append((u, v, ground(*W(u, v))))
A = np.array([[1, p[0] - 3540, p[1] - 405] for p in pts]); y = np.array([p[2] for p in pts])
PLANE = np.linalg.lstsq(A, y, rcond=None)[0]
FRONT_Y = round(float(PLANE[0] + PLANE[1] * (FRONT_U - 3540) + PLANE[2] * (404 - 405)), 3)
APRON = 8.0
def smooth01(t): t = min(1.0, max(0.0, t)); return t * t * (3 - 2 * t)
def floor_y(u, v):
    p = PLANE[0] + PLANE[1] * (u - 3540) + PLANE[2] * (v - 405)
    w = smooth01((u - FRONT_U - 2.0) / APRON) * smooth01((ROW_V - 2.0 - v) / APRON)   # level by the supermarket's front and by the shop row's
    return FRONT_Y + w * (p - FRONT_Y)

# ------------------------------------------------------------------ Racer's profile turtle (StuntPark.Prof), the same steps
class Prof:
    def __init__(s): s.pts = [(0.0, 0.0)]; s.s = 0.0; s.y = 0.0; s.a = 0.0
    def add(s): s.pts.append((round(s.s, 4), round(s.y, 4))); return s
    def arc(s, r, da):
        n = max(2, math.ceil(abs(da) / 3)); a0 = math.radians(s.a)
        c = (s.s - r * math.sin(a0), s.y + r * math.cos(a0)) if da > 0 else (s.s + r * math.sin(a0), s.y - r * math.cos(a0))
        for i in range(1, n + 1):
            a1 = math.radians(s.a + da * i / n)
            if da > 0: s.s, s.y = c[0] + r * math.sin(a1), c[1] - r * math.cos(a1)
            else: s.s, s.y = c[0] - r * math.sin(a1), c[1] + r * math.cos(a1)
            s.add()
        s.a += da; return s
    def line(s, L): s.s += L * math.cos(math.radians(s.a)); s.y += L * math.sin(math.radians(s.a)); return s.add()
    def flat(s, L): s.a = 0; s.s += L; return s.add()
    def up(s, to): s.y = to; return s.add()
    def drop(s): s.y = 0; s.a = 0; return s.add()
    def landing(s, phi, rk, rb):
        s.a = 0; s.arc(rk, -phi)
        s.line(max(0.0, (s.y - rb * (1 - math.cos(math.radians(phi)))) / math.sin(math.radians(phi)))); s.arc(rb, phi); s.y = 0; s.pts[-1] = (s.pts[-1][0], 0.0)
        return s
    @property
    def length(s): return s.pts[-1][0]
def kicker(h, th): r = h / (1 - math.cos(math.radians(th))); return Prof().arc(r, th)
def lip_of(h, th): r = h / (1 - math.cos(math.radians(th))); return r * math.sin(math.radians(th))

# ------------------------------------------------------------------ the features
F = []   # every feature: kind, id, name, origin (u, v), dir (du, dv), width, base ("floor" / "roof" / "front"), profile, look
def ramp(fid, name, prof, u, v, du, dv, width, look, base="floor", bank_end=0.0, loop=False, note=""):
    F.append(dict(kind="ramp", id=fid, name=name, u=u, v=v, du=du, dv=dv, width=width, base=base, profile=prof.pts, look=look, bank_end=bank_end, note=note))
    return prof

# --- the floor (Racer's sizes, natural physics)
# K-line, westbound (start line -> K2 -> on to the stores or round to the roof ramp). Round 38 (Part A, BUG-001: "Can we make this bank part of
# the course ... otherwise this ramp is kind of useless the way it is positioned"): K1 is turned round and is the BANK HOP, below.
k2 = kicker(2.0, 25); k2.flat(14).landing(18, 3, 10); ramp("K2", "K2 table", k2, 3570, 374, -1, 0, 9, "table")
# the roof ramp: up from the floor to the supermarket's roof at its south-east corner, westbound (absolute heights: built by the map build)
F.append(dict(kind="climb", id="RR", name="Roof ramp", u_top=3448.9, v=356.0, du=-1, dv=0, width=10, to="roof", angle=12.0, r=30.0, top_flat=2.5, look="steel"))
# the mega hill: the landing of the BIG AIR (sized from the flight by the map build); its back a drive-up for the westbound line
# the barrel-roll ramp and its landing, eastbound, south of the mega hill
barrel = kicker(3.0, 28); barrel.drop(); blen = barrel.pts[-2][0]
ramp("BR", "Barrel-roll ramp", barrel, 3476, 382, 1, 0, 6, "barrel", bank_end=50.0)
bl = Prof().arc(3, 22); bl.line((1.6 - 2 * 3 * (1 - math.cos(math.radians(22)))) / math.sin(math.radians(22))).arc(3, -22); bl.flat(2).landing(12, 3, 10)
ramp("BRL", "Barrel-roll landing", bl, 3476 + blen + 1.2, 380.5, 1, 0, 15, "dirt")
# the school-bus gap, eastbound, after the barrel roll's run-out (bus lengthwise, 12 m)
bk = kicker(3.4, 26); bk.drop(); ramp("BG", "Bus gap take-off", bk, 3512, 388, 1, 0, 7, "ply")
blip = 3512 + lip_of(3.4, 26)
F.append(dict(kind="bus", id="BUS", u=blip + 2.3, v=388, du=1, dv=0))
bland = Prof().up(3.4); bland.flat(1).landing(18, 3, 12); ramp("BGL", "Bus gap landing", bland, blip + 16.5, 388, 1, 0, 11, "dirt")
# the junk-car gap, westbound, north of the gas station (five wrecks side by side across the flight)
ramp("CG", "Car gap take-off", kicker(2.0, 22).drop(), 3600, 443, -1, 0, 7, "ply")
clip = 3600 - lip_of(2.0, 22)
for k in range(5): F.append(dict(kind="junkcar", id=f"JC{k}", u=clip - 3 - k * 2.05, v=443, du=0, dv=1, k=k))
cl = Prof().up(2.0); cl.flat(1.2).landing(16, 3, 10); ramp("CGL", "Car gap landing", cl, clip - 13.6, 443, -1, 0, 10, "dirt")
# the half-pipe along the east side, two plywood quarter-pipes 8 m apart (Racer's: radius 6 m to 65 degrees, a 1.5 m deck), 36 m long
for (u, du) in ((3617.0, -1), (3625.0, 1)):
    qp = Prof().arc(6, 65); qp.flat(1.5).drop(); ramp("HP" + ("w" if du < 0 else "e"), "Half-pipe", qp, u, 431.0, du, 0, 36, "ply")
# the loop (Racer's ribbon, radius 5 m, moving 6.5 m sideways on the way round), eastbound on the north lane
F.append(dict(kind="loop", id="LOOP", u=3550.0, v=460.0, du=1, dv=0, side=(0, -1), radius=5.0, shift=6.5, width=5.6))
# the tower: Racer's two-level deck, here a scaffold tower whose top is level with the roofs (a gap off the supermarket's roof onto it) and
# whose far end is a drop onto a landing ramp; a launch ramp up onto it from the floor for the eastbound line too
# --- the roofs (base "roof": over ROOF_Y)
F.append(dict(kind="bigjump", id="MEGA", name="BIG AIR", u_lip=FRONT_U - 0.4, v=410.0, du=1, dv=0, width=10, base="roof", h=1.8, th=30.0, v_design=27.0, angle=32.0,
              landing=dict(contact_h=4.0, slope_less=4.0, up=4.0, table=8.0, rb=18.0, width=18.0, back=20.0, look="mega"), note="the supermarket roof's front edge, eastbound over the park"))
F.append(dict(kind="bigjump", id="DROP", name="Shop row drop", u=3474.0, v_lip=ROW_V - 0.4, du=0, dv=-1, width=8, base="roof", h=1.2, th=20.0, v_design=19.0, angle=20.0,
              landing=dict(contact_h=2.2, slope_less=3.0, up=4.0, table=6.0, rb=12.0, width=14.0, back=0.0, look="drop"), note="the shop row roof's front edge, southbound"))
F.append(dict(kind="bigjump", id="GAP", name="Roof gap", u_lip=3510.5, v=481.5, du=1, dv=0, width=8, base="roof", h=1.4, th=24.0, v_design=22.0, angle=24.0,
              landing=dict(contact_h=2.0, slope_less=3.0, up=4.0, table=2.0, rb=10.0, width=12.0, back=0.0, look="roofgap", on="roof", from_u=3525.0), atrium=(3512.0, 3525.0, 471.5, 492.0),
              note="along the shop row roof, eastbound over the skylight atrium of shop 5"))
F.append(dict(kind="bigjump", id="TOWER", name="Tower gap", u_lip=3448.1, v=452.0, du=1, dv=0, width=7, base="roof", h=0.8, th=12.0, v_design=17.0, angle=12.0,
              landing=dict(contact_h=5.0, slope_less=3.0, up=4.5, table=0.0, rb=14.0, width=12.0, back=0.0, look="tower", tower=True, from_u=3460.0), note="the supermarket roof's north-east corner onto the tower and down to the floor"))
# the roof's quarter-pipes on the parapets and a table on the shop row's roof
qp = Prof().arc(5, 70); qp.flat(1.2).drop(); ramp("RQPW", "Roof quarter-pipe (west)", qp, 3391.0, 400.0, -1, 0, 60, "ply", base="roof")
qp2 = Prof().arc(4.5, 70); qp2.flat(1.0).drop(); ramp("RQPE", "Roof quarter-pipe (shop row east end)", qp2, 3585.0, 481.5, 1, 0, 18, "ply", base="roof")
rt = kicker(1.2, 20); rt.flat(6).landing(14, 2, 6); ramp("RT", "Roof table", rt, 3556.0, 481.5, 1, 0, 8, "table", base="roof")


# ------------------------------------------------------------------ Round 38 (Part A): the abandoned bank in the park (base "bank": over BANK_ROOF)
# The bank (OSM building 1, commercial, 10.5 m) stands boxed in at the park's east end (the lot's fence 29 m east of it, 14 m south), so its
# roof is reached by an aimed pop-up and left by an aimed drop:
#   K1, the BANK HOP: K1 turned round to face the bank, a steep launch (2.5 m, curved to 60 deg) 10.7 m from its west wall, with the K-line's
#     floor behind it as the run-up (over K2's table the other way, or straight along the lane). Every vehicle over its lip leaves at 17.2 m/s
#     at 62 deg (BigJump), tops out 2.2 m over the roof just past the wall and comes down on the roof's west half with 8 m/s left (2.2 s up).
#   On the roof: a 0.6 m parapet round it (a low berm along the east edge turns a vehicle that does not stop), the BANK DROP off the north edge.
#   The BANK DROP: an aimed launch off the roof's north edge, north over the start lane onto a landing sized to it in the lane between the gas
#     station's canopy and the half-pipe (its table clear of the start lane, its run-out across the car gap's run-up).
#   Through it: its boarded front doors (west) and the boarded drive-up bay (east) break; inside, one tall hall (Part C dresses it).
BANK_RING = [(3592.77, 393.25), (3592.72, 373.71), (3596.22, 373.69), (3596.21, 370.22), (3596.2, 365.22), (3596.19, 359.19), (3602.97, 359.16), (3609.61, 359.13),
           (3609.61, 365.34), (3609.63, 370.21), (3609.69, 393.17)]
assert m["buildings"][1]["type"] == "commercial" and abs(m["buildings"][1]["height"] - 10.5) < .01, "building 1 is not the bank any more: re-check"
BANK_ROOF = round(sum(floor_y(u, v) for u, v in BANK_RING) / len(BANK_RING) + m["buildings"][1]["height"], 3)   # its pad graded to the floor plane
BANK_PARAPET = 0.6
BANKDROP_V, BANKDROP_A = 16.0, 10.0   # the BANK DROP's flight: its landing's table starts 18 m out, clear of the start lane (v 399.5 to 407.5)
BANK_HOP = dict(lip_u=3582.0, v=377.5, h=2.5, th=60.0, width=8.0, v_design=17.2, angle=62.0)
ramp("K1", "Bank hop", kicker(BANK_HOP["h"], BANK_HOP["th"]).drop(), BANK_HOP["lip_u"] - lip_of(BANK_HOP["h"], BANK_HOP["th"]), BANK_HOP["v"], 1, 0, BANK_HOP["width"], "launch",
     note="Round 38: K1 turned round, the pop-up onto the bank's roof")
F.append(dict(kind="stepup", id="K1", name="Bank hop", u_lip=BANK_HOP["lip_u"], v=BANK_HOP["v"], du=1, dv=0, width=BANK_HOP["width"], base="floor", h=BANK_HOP["h"],
              th=BANK_HOP["th"], v_design=BANK_HOP["v_design"], angle=BANK_HOP["angle"], note="aimed up onto the bank's roof"))
F.append(dict(kind="bigjump", id="BANKDROP", name="Bank drop", u=3605.0, v_lip=393.25 - 0.4, du=0, dv=1, width=8, base="bank", h=0.8, th=12.0, v_design=BANKDROP_V, angle=BANKDROP_A,
              landing=dict(contact_h=2.6, slope_less=3.0, up=3.0, table=4.0, rb=12.0, width=9.0, back=0.0, look="bank"), note="the bank roof's north edge, northbound over the start lane"))
BANK_DOORS = [("west", 3592.75, 388.0, 6.0, 3.2), ("east", 3609.65, 388.0, 6.0, 3.2)]   # (side, u, v middle, width, height): boarded, breakable

# ------------------------------------------------------------------ Round 38 (Part C): more inside the stores
# The shop row's ten shops get rooms that fit their made-up names (built by MapBuilder.StuntParkShops.cs: counters, racks, tables, chairs, all breakable;
# the hall's front 8 m kept as the shop-to-shop run through the partitions) and two lines:
#   SHOP RAMP: up through the roof. A steel ramp along the back of the laundry, the florist and the taqueria (k9 to k7), eastbound, from the
#     supermarket's north end (its run-up) up 9 m through a hatch in the roof, arriving on the shop row's roof in the roof gap's lane, 7 m short of
#     its kicker (so the line goes on over the atrium). The partitions it passes are open over its width.
#   PIZZA WINDOW: an aimed kicker inside Corner Slice Pizza (k4), out through its front window into the park, onto a landing sized to it.
# The bank (Part A) gets its teller counter across the lobby and its vault against the east wall, the vault door (breakable) on the line through.
SHOP_RAMP = dict(v=481.5, width=6.0, u_top=3497.0, angle=16.0, r=20.0)
F.append(dict(kind="bigjump", id="PIZZA", name="Pizza window", u=3532.0, v_lip=ROW_V + 1.0, du=0, dv=-1, width=4, base="front", h=0.45, th=30.0, v_design=17.0, angle=36.0,
              landing=dict(contact_h=0.4, slope_less=3.0, up=3.0, table=4.0, rb=12.0, width=8.0, back=0.0, look="drop"), note="Corner Slice Pizza's kicker, out through its front window"))
BANK_VAULT = dict(u0=3603.0, u1=3609.25, v0=383.6, v1=392.4, door_w=3.0, door_h=3.0)   # against the east wall, behind the bay
BANK_TELLER = dict(u=3599.0, v0=373.9, v1=392.9)                                       # the counter across the lobby (breakable)

# ------------------------------------------------------------------ the inside (base "front": over FRONT_Y, the stores' level floor)
INSIDE = dict(
    market_glass=[(389.0, 419.0), (426.0, 435.0)],            # the supermarket's front windows (v ranges at u = FRONT_U)
    shop_fronts=10,                                            # the shop row's fronts, each with its window
    shelves=[358 + 8 * k for k in range(8)], shelf_u=(3398.0, 3432.0), shelf_h=2.2, shelf_d=1.4,
    checkouts=[383 + 6 * k for k in range(8)], checkout_u=3440.0,
    aisle_kickers=[(3418.0, 362 + 16 * k) for k in range(4)],
    mezzanine=dict(u0=3382.0, u1=3392.0, v0=366.0, v1=440.0, h=4.5, ramp_v=(366.0, 386.0), dropin_v=(399.0, 405.0)),   # (the store is 6 m narrower south of v 364)
    skylight=dict(u0=3392.0, u1=3416.0, v0=428.0, v1=438.0),   # the ramp from the mezzanine up through the roof
    partitions=9,
)

# ------------------------------------------------------------------ Round 38 (Part D): the stunt rivals' lines (Assets/Scripts/Stunts/StuntRival.cs)
# A graph of lines through the park, each a list of points (u, v, base, speed): base "f" the floor, "r" the roofs, "s" the stores' floor, "b" the
# bank's roof, a number an absolute height (the roof ramp, the shop ramp: interpolated); speed the most to arrive with there (0: as the bends allow; a
# natural kicker's own; the aimed launches take any speed). Each line ends where the next ones begin (next: ids with weights): the hub (east, the
# start lane westbound) -> SW (south-west, by the store front) -> the roof ramp, the bus line or the bank hop; the roof (R) -> BIG AIR, the tower and
# loop, or the shop row drop; after BIG AIR (M) -> home, or into the shops and out of the pizza window. Every big feature is on one: the roof ramp,
# the roofs, BIG AIR, the tower gap, the loop, the half-pipe's trough, the shop row drop, the barrel roll, the bus gap, the bank through its doors
# and vault, K2, the bank hop, the bank drop, the shop row's windows, partitions and rooms, the pizza window. Left out: the car gap (its run-up is
# 10 m between the half-pipe and the kicker: too short for a line that holds) and the walls of the half-pipe and the roof quarter-pipes.
RR_FOOT, RR_TOP = 3499.0, 3448.9
def rr_y(u): t = min(1.0, max(0.0, (RR_FOOT - u) / (RR_FOOT - RR_TOP))); return floor_y(RR_FOOT, 356.0) + t * (ROOF_Y - floor_y(RR_FOOT, 356.0))
AI_LINES = {
    "hub": ([(3612, 398.6, "f", 0), (3580, 398.6, "f", 0), (3540, 398.6, "f", 0), (3495, 398.4, "f", 0), (3478, 397, "f", 12), (3466, 394, "f", 10), (3462, 388, "f", 10)], [("sw_rr", 35), ("sw_bus", 35), ("sw_hop", 30)]),
    "sw_rr": ([(3462, 388, "f", 10), (3462, 378, "f", 9), (3466, 368, "f", 8), (3482, 366, "f", 9), (3497, 366, "f", 7), (3504, 364, "f", 5), (3505, 359, "f", 5), (3501, 356, "f", 6),
               (3498, 356, rr_y(3498), 12), (3475, 356, rr_y(3475), 0), (3452, 356, rr_y(3452), 0), (3440, 357, "r", 9), (3428, 362, "r", 8), (3422, 372, "r", 9), (3422, 380, "r", 10)],
              [("roof_big", 45), ("roof_tower", 35), ("roof_drop", 20)]),
    "roof_big": ([(3422, 380, "r", 10), (3422, 394, "r", 9), (3427, 404, "r", 9), (3436, 409.5, "r", 0), (3446, 410, "r", 0), (3455.1, 410, "r", 0), (3500, 410, "f", 0),
                  (3530, 410, "f", 0), (3545, 410, "f", 0)], [("m_home", 50), ("m_pizza", 50)]),
    "roof_tower": ([(3422, 380, "r", 10), (3422, 420, "r", 0), (3424, 436, "r", 9), (3430, 448, "r", 8), (3437, 452, "r", 0), (3448.1, 452, "r", 0), (3470, 452, "f", 0),
                    (3490, 452.5, "f", 0), (3510, 456, "f", 16), (3528, 460, "f", 18), (3540, 460, "f", 18), (3550, 460, "f", 18), (3557, 453.5, "f", 0),
                    (3575, 453.5, "f", 0), (3598, 454, "f", 12), (3611, 455, "f", 9), (3619, 452, "f", 8), (3621, 444, "f", 10), (3621, 420, "f", 10), (3621, 404, "f", 8),
                    (3614, 399.5, "f", 8), (3612, 398.6, "f", 8)], [("hub", 1)]),
    "roof_drop": ([(3422, 380, "r", 10), (3422, 420, "r", 0), (3426, 440, "r", 10), (3433, 458, "r", 9), (3443, 472, "r", 8), (3455, 480.5, "r", 8), (3465, 481, "r", 7),
                   (3471, 479, "r", 6), (3474, 476, "r", 6), (3474, 469.1, "r", 0), (3474, 440, "f", 0), (3474, 425, "f", 0), (3473, 410, "f", 12), (3466, 400, "f", 10),
                   (3462, 388, "f", 10)], [("sw_bus", 50), ("sw_hop", 50)]),
    "sw_bus": ([(3462, 388, "f", 10), (3463, 386, "f", 9), (3467, 384, "f", 11), (3474, 383.5, "f", 13), (3488, 383.5, "f", 0), (3500, 384, "f", 0), (3506, 386, "f", 17),
                (3512, 387.5, "f", 17), (3526.7, 388, "f", 17), (3545, 388, "f", 0), (3560, 388, "f", 0), (3585, 388, "f", 13), (3592.7, 388, "f", 12), (3600, 388, "f", 12),
                (3606, 388, "f", 12), (3612, 388, "f", 10), (3619, 389, "f", 8), (3623, 393, "f", 7), (3619, 398.6, "f", 7), (3612, 398.6, "f", 8)], [("hub", 1)]),
    "sw_hop": ([(3462, 388, "f", 10), (3462, 380, "f", 9), (3466, 372.5, "f", 9), (3480, 371, "f", 11), (3505, 371.5, "f", 11), (3530, 373.5, "f", 10), (3547, 374.5, "f", 9),
                (3561, 375, "f", 9), (3572, 377, "f", 0), (3582, 377.5, "f", 0), (3600, 377.5, "b", 0), (3604, 382, "b", 6), (3605, 386.5, "b", 0), (3605, 392.85, "b", 0),
                (3605, 420, "f", 0), (3605, 438, "f", 0), (3608, 446, "f", 10), (3614, 452, "f", 8), (3620, 451, "f", 8), (3621, 444, "f", 10), (3621, 420, "f", 10),
                (3621, 404, "f", 8), (3614, 399.5, "f", 8), (3612, 398.6, "f", 8)], [("hub", 1)]),
    "m_home": ([(3545, 410, "f", 0), (3575, 409, "f", 0), (3596, 406, "f", 12), (3610, 405, "f", 9), (3619, 403, "f", 7), (3620, 399.5, "f", 7), (3612, 398.6, "f", 8)], [("hub", 1)]),
    "m_pizza": ([(3545, 410, "f", 14), (3551, 417, "f", 10), (3549, 430, "f", 10), (3536, 444, "f", 10), (3518, 455, "f", 9), (3506, 463, "f", 8), (3505, 469.5, "s", 8),
                 (3507, 476, "s", 7), (3513, 480, "s", 7), (3525, 480, "s", 7), (3529, 479, "s", 5), (3532, 476, "s", 4), (3532, 472.2, "s", 4), (3532, 470.5, "s", 0),
                 (3532, 455, "f", 0), (3532, 440, "f", 0), (3533, 434, "f", 10), (3541, 426, "f", 10), (3551, 418, "f", 11), (3566, 410, "f", 0), (3596, 406, "f", 12), (3610, 405, "f", 9),
                 (3619, 403, "f", 7), (3620, 399.5, "f", 7), (3612, 398.6, "f", 8)], [("hub", 1)]),
}
def ai_y(u, v, b):
    if isinstance(b, (int, float)): return float(b)
    return {"f": floor_y(u, v), "r": ROOF_Y, "s": FRONT_Y, "b": BANK_ROOF}[b]
# the rivals' places on the start line (beside and behind the player's: u, v)
AI_GRID = [(3628.0, 399.0), (3628.0, 408.0), (3637.0, 403.5)]

# ------------------------------------------------------------------ ballistics (the aimed launches), for the layout and the plot
def lip_height(f):
    if f.get("base") == "bank": return BANK_ROOF + f["h"]
    if f.get("base") == "floor": return floor_y(*lip_uv(f)) + f["h"]
    return (ROOF_Y if f.get("base") == "roof" else FRONT_Y) + f["h"]
# the flight is the body's: it leaves the lip between 0.8 m under and 0.3 m over the lip's height (the exe's flights, Round 36: the Clay Hopper
# low, the Skywhip high) and its wheels find the ground when it is 0.8 m over it (the suspension's reach)
BODY0, BODY_LO, BODY_HI, REACH = -0.25, -0.8, 0.3, 0.8
def lip_uv(f): return (f["u_lip"], f["v"]) if "u_lip" in f else (f["u"], f["v_lip"])
def flight(f, dv=0.0, dy=0.0):
    """a flight off the lip at the design speed (+dv) and lip height (+dy): (s along the jump, y of the wheels, t), every 0.01 s"""
    a = math.radians(f["angle"]); sp = f["v_design"] + dv; vx, vy = sp * math.cos(a), sp * math.sin(a)
    y0 = lip_height(f) + BODY0 + dy - REACH; out = []; t = 0.0   # (the wheels' reach taken off: "y" meets the surface when the wheels do)
    while t < 8: out.append((vx * t, y0 + vy * t - 0.5 * G * t * t, t)); t += 0.01
    return out
def landing_base(f):
    u0, v0 = lip_uv(f); L = f["landing"]
    if L.get("on") == "roof": return lambda s: ROOF_Y
    return lambda s: floor_y(u0 + f["du"] * s, v0 + f["dv"] * s)
def first_below(fl, surface, after=0.15):
    # coming down: after the apex (a flight starts under the landing's height when the landing is on the roof it left)
    apex_t = max(fl, key=lambda q: q[1])[2]
    for s, y, t in fl:
        if t >= max(after, apex_t) and y <= surface(s): return s, y, t
    return None
LANDINGS = []
def size_landings():
    """each aimed launch's landing hill: Racer's landing (a table at Ht, a knuckle, a slope of phi, a transition onto the floor), placed so the
    design flight's wheels meet the slope contact_h over the floor, phi a little under the flight's angle there; checked at -1 / +1 m/s"""
    for f in F:
        if f["kind"] != "bigjump": continue
        L = f["landing"]; base = landing_base(f); fl = flight(f)
        hit = first_below(fl, lambda s: base(s) + L["contact_h"])
        s_c, y_c, t_c = hit
        a = math.radians(f["angle"]); vx = f["v_design"] * math.cos(a); vy = f["v_design"] * math.sin(a) - G * t_c
        down = math.degrees(math.atan2(-vy, vx)); phi = round(down - L["slope_less"], 1)
        ht = round(L["contact_h"] + L["up"] * math.tan(math.radians(phi)), 2)
        prof = Prof().up(ht); prof.flat(L["table"] if not L.get("tower") else 0.0).landing(phi, 3.0, L["rb"])
        # where the profile comes down through contact_h (on the slope), so the hill's origin puts it under the flight's contact
        pts = prof.pts; sp = None
        for i in range(1, len(pts)):
            (s0, y0), (s1, y1) = pts[i - 1], pts[i]
            if y0 >= L["contact_h"] > y1 and s1 > s0: sp = s0 + (y0 - L["contact_h"]) / (y0 - y1) * (s1 - s0); break
        origin_s = s_c - sp
        if L.get("from_u"):   # the tower and the roof gap: the table reaches back to the gap's far side
            back = origin_s - (L["from_u"] - lip_uv(f)[0]); prof = Prof().up(ht); prof.flat(back).landing(phi, 3.0, L["rb"]); origin_s -= back
        u0, v0 = lip_uv(f); ou, ov = u0 + f["du"] * origin_s, v0 + f["dv"] * origin_s
        pts = prof.pts
        def hill(s):   # the hill's top over its base at distance s from the lip
            q = s - origin_s
            if q < 0 or q > pts[-1][0]: return -99.0
            for i in range(1, len(pts)):
                if pts[i][0] >= q: (s0, y0), (s1, y1) = pts[i - 1], pts[i]; return y0 + (q - s0) / max(s1 - s0, 1e-6) * (y1 - y0)
            return 0.0
        checks = []
        front = origin_s + 0.01; low = flight(f, -1, BODY_LO - BODY0)
        clear = min((y + REACH) - (base(sx) + ht) for sx, y, _ in low if abs(sx - front) < 0.6) if any(abs(sx - front) < 0.6 for sx, _, _ in low) else 99
        checks.append(f"the lowest flight's body {clear:.1f} m over the table's front edge")
        if clear < 1.0: print("  WARNING: the table's front is in the way of a low flight")
        for dv, dy in ((0, 0), (-1, 0), (1, 0), (0, BODY_LO - BODY0), (0, BODY_HI - BODY0)):
            h = first_below(flight(f, dv, dy), lambda s: base(s) + max(hill(s), 0.0))
            q = h[0] - origin_s; on = "slope" if hill(h[0]) > 0.05 and q > pts[1][0] + (L["table"] if not L.get("tower") else 0) else "table" if hill(h[0]) > 0.05 else "FLOOR"
            checks.append(f"{dv:+.0f} m/s {dy:+.1f} m: {h[0]:.1f} m out, {h[2]:.2f} s, on the {on}")
        apex = max(y for _, y, _ in fl) - (lip_height(f) + BODY0 - REACH)
        f["landing_ramp"] = dict(u=round(ou, 3), v=round(ov, 3), table_h=ht, phi=phi, length=round(pts[-1][0], 2))
        # the take-off itself: Racer's kicker up to the lip (h, th), ending at the lip (its back a drop: the roof's edge or the atrium)
        L_k = lip_of(f["h"], f["th"])
        ramp(f["id"] + "K", f["name"] + " take-off", kicker(f["h"], f["th"]).drop(), u0 - f["du"] * L_k, v0 - f["dv"] * L_k, f["du"], f["dv"], f["width"], "launch", base=f.get("base", "floor"))
        ramp(f["id"] + "L", f["name"] + " landing", prof, ou, ov, f["du"], f["dv"], L["width"], L["look"], base="roof" if L.get("on") == "roof" else "floor",
             note=f"sized for {f['v_design']} m/s at {f['angle']} deg")
        # its back: a drive-up slope from the floor onto the table (the mega hill: the westbound line drives up it and drops off the face)
        if L.get("back", 0) > 0:
            bk = Prof().up(ht); bk.flat(0.5).landing(L["back"], 6.0, 12.0)   # back: its slope in degrees
            ramp(f["id"] + "B", f["name"] + " back", bk, ou, ov, -f["du"], -f["dv"], L["width"], L["look"], base="floor", note="the drive-up behind the table")
        line = (f"{f['id']:6s} {f['name']:14s} lip {lip_height(f):.2f} ({lip_height(f) - floor_y(u0, v0):.1f} m over the floor) {f['v_design']} m/s at {f['angle']} deg, apex {apex:.1f} m over the lip: "
                f"design contact {s_c:.1f} m out after {t_c:.2f} s coming down at {down:.0f} deg; landing table {ht:.1f} m, slope {phi:.0f} deg, from {origin_s:.1f} to {origin_s + pts[-1][0]:.1f} m; " + "; ".join(checks))
        LANDINGS.append(line); print(line)
def report(): size_landings()

def flat(pairs): return [round(float(c), 4) for p in pairs for c in p]
FIELDS = dict(kind="", id="", name="", note="", look="", base="floor", u=0.0, v=0.0, du=0.0, dv=0.0, width=0.0, profile=[], bank_end=0.0,
              u_top=0.0, angle=0.0, r=0.0, top_flat=0.0, h=0.0, th=0.0, v_design=0.0, lip_u=0.0, lip_v=0.0, atrium=[], side_u=0.0, side_v=0.0,
              radius=0.0, shift=0.0, k=0)
def feature_out(f):
    o = dict(FIELDS)
    for key in FIELDS:
        if key in f: o[key] = f[key]
    if f["kind"] in ("bigjump", "stepup"): o["lip_u"], o["lip_v"] = lip_uv(f); o["u"], o["v"] = o["lip_u"], o["lip_v"]
    if f["kind"] == "stepup": o["kind"] = "bigjump"   # Round 38: an aimed launch with no landing hill (the bank hop lands on the bank's roof)
    if f["kind"] == "climb": o["u"] = f["u_top"]
    if f["kind"] == "loop": o["side_u"], o["side_v"] = f["side"]
    if "profile" in f: o["profile"] = flat(f["profile"])
    if "atrium" in f: o["atrium"] = list(f["atrium"])
    return o
def write():
    out = dict(note="Round 36: the stunt park (Tools/mapgen/make_stunt_park.py; built by MapBuilder.StuntPark.cs). Map meters; u, v the store's frame (world x, z = u U + v V).",
               title="Chastain Corner Stunt Park", U=list(U), V=list(V), center_building=CENTER, roof_y=ROOF_Y, front_y=FRONT_Y, front_u=FRONT_U, row_v=ROW_V, apron=APRON,
               plane_y=float(PLANE[0]), plane_du=float(PLANE[1]), plane_dv=float(PLANE[2]), plane_u0=3540.0, plane_v0=405.0,
               floor=flat(FLOOR_UV), outline=flat(OUTLINE_UV), lot_south=flat(LOT_SOUTH_UV), canopy=list(CANOPY_UV), bank=list(BANK_UV),
               features=[feature_out(f) for f in F],
               market_glass=flat(INSIDE["market_glass"]), shelves=INSIDE["shelves"], shelf_u=list(INSIDE["shelf_u"]), shelf_h=INSIDE["shelf_h"], shelf_d=INSIDE["shelf_d"],
               checkouts=INSIDE["checkouts"], checkout_u=INSIDE["checkout_u"], aisle_kickers=flat(INSIDE["aisle_kickers"]),
               mezz=[INSIDE["mezzanine"][k] for k in ("u0", "u1", "v0", "v1", "h")], mezz_ramp=list(INSIDE["mezzanine"]["ramp_v"]), mezz_dropin=list(INSIDE["mezzanine"]["dropin_v"]),
               skylight=[INSIDE["skylight"][k] for k in ("u0", "u1", "v0", "v1")], shop_fronts=INSIDE["shop_fronts"],
               start=[3628.0, 403.5, -1.0, 0.0], entrance=[3630.0, 403.5, 26.0],
               openings=flat([(3640.0, 398.0), (3640.0, 409.0), (3545.0, 341.4), (3554.0, 341.7), (3455.0, 326.4), (3466.0, 330.0), (3609.0, 467.7), (3619.0, 467.3)]),
               resets=flat([(3628, 403.5, -1, 0), (3560, 400, -1, 0), (3470, 404, 1, 0), (3466, 372, 1, 0), (3490, 438, 1, 0), (3530, 440, 1, 0), (3600, 452, -1, 0), (3606, 400, -1, 0),
                            (3600, 388, 1, 0), (3420, 395, 1, 0), (3545, 482, 1, 0), (3420, 380, 1, 0)]),
               lights=flat([(3462, 338), (3510, 366), (3592, 366), (3634, 350), (3634, 396), (3634, 444), (3600, 465), (3540, 466), (3462, 466), (3540, 425)]),
               landings=LANDINGS,
               bank_ring=flat(BANK_RING), bank_roof_resets=flat([(3601.0, 381.0, 0, 1)]),
               ai_lines=[dict(id=k, next=[n for n, _ in nx], weights=[float(w) for _, w in nx], points=[c for (u, v, b, sp) in pts for c in (round(W(u, v)[0], 3), round(ai_y(u, v, b), 3), round(W(u, v)[1], 3))],
                              speeds=[float(sp) for (_, _, _, sp) in pts]) for k, (pts, nx) in AI_LINES.items()],
               ai_grid=flat([W(u, v) for u, v in AI_GRID]),
               shop_ramp=[SHOP_RAMP[k] for k in ("v", "width", "u_top", "angle", "r")], bank_vault=[BANK_VAULT[k] for k in ("u0", "u1", "v0", "v1", "door_w", "door_h")],
               bank_teller=[BANK_TELLER[k] for k in ("u", "v0", "v1")], bank_roof=BANK_ROOF, bank_parapet=BANK_PARAPET, bank_doors=[c for d in BANK_DOORS for c in ((0.0 if d[0] == "west" else 1.0), d[1], d[2], d[3], d[4])])
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, "w"), indent=1)
    print("wrote", OUT, f"roof {ROOF_Y:.2f}, the stores' floor {FRONT_Y:.2f}, plane {PLANE}")

def plot():
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(16, 12))
    def poly(pts, **kw): xs, ys = zip(*(pts + [pts[0]])); ax.plot(xs, ys, **kw)
    poly([UV(*p) for p in RING], color="k", lw=1.5); poly(FLOOR_UV, color="b", lw=1); poly(OUTLINE_UV, color="orange", lw=1, ls="--")
    for r in (CANOPY_UV, BANK_UV): poly([(r[0], r[2]), (r[1], r[2]), (r[1], r[3]), (r[0], r[3])], color="gray", lw=2)
    def rect(u, v, du, dv, length, w, c, label):
        ru, rv = -dv, du
        pts = [(u + ru * s * w / 2 + du * l, v + rv * s * w / 2 + dv * l) for s, l in ((-1, 0), (1, 0), (1, length), (-1, length))]
        ax.fill(*zip(*pts), color=c, alpha=.45); ax.text(u + du * length / 2, v + dv * length / 2, label, fontsize=6, ha="center")
    cols = dict(ply="peru", table="gold", dirt="sienna", barrel="red", steel="slategray", mega="royalblue", drop="teal", roofgap="purple", tower="green")
    for f in F:
        if f["kind"] == "ramp": rect(f["u"], f["v"], f["du"], f["dv"], f["profile"][-1][0], f["width"], cols.get(f["look"], "c"), f["id"])
        elif f["kind"] == "climb": rect(f["u_top"] + 50, f["v"], -1, 0, 50, f["width"], cols["steel"], f["id"])
        elif f["kind"] == "bus": rect(f["u"], f["v"], 1, 0, 12, 2.5, "yellow", "bus")
        elif f["kind"] == "junkcar": rect(f["u"] - 2.15, f["v"], 1, 0, 4.3, 1.75, "brown", "")
        elif f["kind"] == "loop": rect(f["u"] - 6, f["v"], 1, 0, 12, 12, "crimson", "LOOP")
        elif f["kind"] == "bigjump":
            u0, v0 = lip_uv(f); fl = flight(f); end = f["landing_ramp"]
            s1 = math.hypot(end["u"] - u0, end["v"] - v0) + end["length"]
            ax.plot([u0, u0 + f["du"] * s1], [v0, v0 + f["dv"] * s1], "r:", lw=1); ax.text(u0, v0, f["id"], color="r", fontsize=8)
    for k, (pts, nx) in AI_LINES.items():   # Round 38: the rivals' lines
        ax.plot([p[0] for p in pts], [p[1] for p in pts], "-", lw=.8, color="magenta"); ax.text(pts[len(pts) // 2][0], pts[len(pts) // 2][1], k, fontsize=5, color="magenta")
    ax.set_aspect("equal"); ax.grid(True, lw=.3); ax.set_xlim(3360, 3650); ax.set_ylim(320, 500)
    os.makedirs(os.path.join(ROOT, "local/round36"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round36/park_layout.png"), dpi=120, bbox_inches="tight"); print("plot written")
    plt.savefig(os.path.join(ROOT, "Data/screenshots/round38/park_layout.png"), dpi=120, bbox_inches="tight")

if __name__ == "__main__":
    report(); write()
    if "--plot" in sys.argv: plot()
