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
# K-line, westbound (start line -> K1 -> K2 -> on to the stores or round to the roof ramp)
ramp("K1", "K1 kicker", kicker(1.0, 20).drop(), 3588, 374, -1, 0, 7, "ply")
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

# ------------------------------------------------------------------ ballistics (the aimed launches), for the layout and the plot
def lip_height(f):
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
    if f["kind"] == "bigjump": o["lip_u"], o["lip_v"] = lip_uv(f); o["u"], o["v"] = o["lip_u"], o["lip_v"]
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
                            (3600, 374, -1, 0), (3420, 395, 1, 0), (3545, 482, 1, 0), (3420, 380, 1, 0)]),
               lights=flat([(3462, 338), (3510, 366), (3592, 366), (3634, 350), (3634, 396), (3634, 444), (3600, 465), (3540, 466), (3462, 466), (3540, 425)]),
               landings=LANDINGS)
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
    ax.set_aspect("equal"); ax.grid(True, lw=.3); ax.set_xlim(3360, 3650); ax.set_ylim(320, 500)
    os.makedirs(os.path.join(ROOT, "local/round36"), exist_ok=True)
    plt.savefig(os.path.join(ROOT, "local/round36/park_layout.png"), dpi=120, bbox_inches="tight"); print("plot written")

if __name__ == "__main__":
    report(); write()
    if "--plot" in sys.argv: plot()
