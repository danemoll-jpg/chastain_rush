"""Round 72 (Dan, 2026-10-26 07:17: "Quarry stunt jumps"; 2026-10-25 02:11: "stunt races that have large ramps to jump you over evel knievel
style"): THE VIKING QUARRY JUMPS, three Evel Knievel stunt jumps across Viking Quarry Lake (Free Roam events; world pieces built by the map build).

Round 73 (C4, Dan: "nothing lined up and the ramp was buried in the hillside"): rebuilt. Round 72's landing decks ran INTO the north bank (a straight
deck through a concave bank: its lower half under the hill, the flights hitting the bank above it), the kickers stood between the rim's mounds and
the run-up lanes were graded into a dip through the runway. Now each jump is one straight line a player sees whole from the start box:
  the run-up     on the airport's own ground (smoothed only where it is not paved; never cut: the runway and taxiways keep their surface)
  the kicker     a steel ramp standing on a pad cleared flat round it (nothing of the rim over or in front of it), its lip at the rim's edge
  the landing    a steel deck standing ON TOP of the far bench at its edge, sloping down onto the bench, in plain view across the water; a solid
                 front face down to the bank under its top (short: into the face and the lake), its sides skirted solid to the ground (nothing to
                 drive in under); past its foot the bench graded smooth (a catch zone: an overshoot lands rough, not in a hole)
  the crowd      bleachers on graded pads on the bench beside the landing and on the plateau beside the kicker
The lines (from a scan of the far side's benches, local/round73/qscan.py):
  J1 "The Corner"   across the east corner, 345 degrees, about 220 m to the north-east bench (74 m below the lip); its run-up east of the runway
  J2 "The Long One" across the middle, due north, about 250 m to the north bench
  J3 "The Big One"  15 degrees across the lake's widest water, about 270 m to the north-east bench
The speed window (Evel Knievel's speed check, the HUD shows it): the lip speeds whose flights come down on the landing deck. Short of it: the face and
the lake; past it: the catch beyond (rough).

What it writes: Data/tracks/quarry_jumps.json (per jump: the line, the run-up, the kicker's deck, the landing deck, the flights, the start box, the
bleachers; and the RidgeFile shape's grades / clear_lines / landing_zones, merged into the mountain's planning pieces by MapBuilder.QuarryJumps.cs).
Re-run after mapgen, then Tools/mapgen/add_extension_places.py (the fast-travel spots), then a full Build Map (the trees cleared, the ground graded).
Usage: python Tools/mapgen/make_quarry_jumps.py [--plot]"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/quarry_jumps.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
WATER = 113.6   # the lake's surface (the DEM's flat water)


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def flat(p): return [round(v, 2) for q in p for v in q]
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]


import os as _os
OVH1, OVH2, OVH3 = (float(v) for v in _os.environ.get("QJ_OVH", "30,30,55").split(","))   # the landing decks' fronts over the bank face (m before the bench's edge)
# (each deck runs on at least 25 m past the bench's edge onto its cut: LAND_L per jump = its overhang + 25 or more)
JUMPS = [
    dict(id="J1", name="The Corner", lip=(-1720.0, -1590.0), heading=345.0, run=320.0, bench_min=200.0, design="Drifter Twin", overhang=OVH1, land_l=OVH1 + 26.0),
    dict(id="J2", name="The Long One", lip=(-2000.0, -1649.0), heading=0.0, run=360.0, bench_min=200.0, design="Needle 600", overhang=OVH2, land_l=OVH2 + 26.0),
    dict(id="J3", name="The Big One", lip=(-2200.0, -1716.0), heading=15.0, run=340.0, bench_min=185.0, design="Needle 600", overhang=OVH3, land_l=OVH3 + 26.0),
]
# (top m/s, push m/s2) as Round 68 / 70; CORR: Round 70's ramp flew 8 to 16 % shorter in the exe than this model said (its lip speeds lower)
VEHICLES = {"Street Classic": (49.0, 14.5), "Highball Fastback": (52.0, 16.0), "Drifter Twin": (58.0, 16.0), "Needle 600": (61.0, 18.0),
            "Stunt Buggy": (50.0, 17.5), "Stunt Bike": (55.0, 18.0)}
CORR = 0.79   # Round 73: the exe through the game's own input (the check's human-like rider): the Street Classic flat out off The Corner's lip at 43.7 m/s, CLEAN at 250 m (the bikes slower on the rough first run-ups: 31 to 34 m/s),
              # this model's 49.8 (Round 72's 0.93 came from a scripted check that drove the vehicle with its physics off)
DECK_HALF = 4.5            # the kicker's deck: 9 m wide
LAND_HALF = 10.0           # the landing deck: 20 m wide
LAND_L = 38.0              # the landing deck's length: from the far bench's edge (its top at the bench's height) down into a shallow cut in the bench
LAND_REL = 38.0            # the landing deck lies this much shallower than the design flight comes down (about 10 degrees: a cut of 6 to 7 m that
                           # climbs back within 80 m, clear of the haul road beyond The Long One; the catch zone sets a vehicle on its wheels)
R_KICK = 110.0             # the kicker's radius: Evel Knievel's short steep ramp (a held steel deck: RampHold keeps a vehicle on it; 15 m/s2 at 40 m/s)
RISE_BACK = 0.15           # past the landing deck's foot the cut's floor climbs back to the bench at this grade (the run-out)
RUNOUT = 110.0             # the catch past the landing deck's foot, graded smooth along the line
PAD_HALF = 15.0            # the pad round the kicker: flat at its foot's height this far either side of the line
PAVED = [a for a in M["aeroways"] if a.get("kind") in ("runway", "taxiway")]
ROADS_NEAR = [pts(r["points"]) for r in M["roads"] if any(-3000 < x < -1300 and -2300 < z < -1000 for x, z in pts(r["points"]))] + [pts(a["points"]) for a in PAVED]


def design(J):
    d = (math.sin(math.radians(J["heading"])), math.cos(math.radians(J["heading"]))); nl = (-d[1], d[0])
    o = (J["lip"][0] - d[0] * J["run"], J["lip"][1] - d[1] * J["run"])
    def P(s, lat=0.0): return (o[0] + d[0] * s + nl[0] * lat, o[1] + d[1] * s + nl[1] * lat)
    raw = lambda s, lat=0.0: ground(*P(s, lat))
    # ---- the far bench's edge: past the water, the first station where the ground has climbed to within 4 m of the bench's height (the bench: the
    # ground's median over the 60 m after the climb)
    s_lipg = J["run"]
    climb = next(s for s in np.arange(s_lipg + 60, s_lipg + 450, 1.0) if raw(s) >= J["bench_min"])
    bench = float(np.median([raw(climb + k) for k in range(5, 50, 3)]))
    edge = next(s for s in np.arange(climb - 40, climb + 40, 0.5) if raw(s) >= bench - 3.0)
    # ---- the run-up: the ground smoothed (a 30 m running mean) where it is not paved; on the runway and taxiways the pavement's own height
    run_s = list(np.arange(0.0, J["run"] - 100.0 + 0.1, 5.0))
    g = [raw(s) for s in run_s]; k = 3; sm = [float(np.mean(g[max(0, i - k):i + k + 1])) for i in range(len(g))]
    def on_paved(q):
        for a in PAVED:
            pp = pts(a["points"]); w = a.get("width", 15.0) / 2 + 2.0
            for u, v in zip(pp, pp[1:]):
                ab = (v[0] - u[0], v[1] - u[1]); L2 = ab[0] ** 2 + ab[1] ** 2
                t = 0 if L2 < 1e-9 else max(0, min(1, ((q[0] - u[0]) * ab[0] + (q[1] - u[1]) * ab[1]) / L2))
                if math.dist(q, (u[0] + ab[0] * t, u[1] + ab[1] * t)) < w: return True
        return False
    paved = [on_paved(P(s)) for s in run_s]
    # (Round 73: the check's rider found the run-ups rough (thrown into the air, 16 m/s lost at the airfield's edge): one smooth lane now, the ground
    # smoothed along it (a Gaussian of 25 m, twice), across the paving too: the runway and taxiways are laid on the graded ground after it)
    def gsm(vals, sig):
        out = []
        for i in range(len(vals)):
            lo_, hi_ = max(0, i - int(3 * sig)), min(len(vals), i + int(3 * sig) + 1); w = [math.exp(-((k - i) / sig) ** 2 / 2) for k in range(lo_, hi_)]
            out.append(sum(vals[k] * wk for k, wk in zip(range(lo_, hi_), w)) / sum(w))
        return out
    run_y = gsm(gsm(g, 5.0), 5.0)
    # ---- the kicker: from the run-up's last point (its foot on a flat pad) curving up to the lip's angle at the rim's edge; the angle chosen so the
    # window's middle flight comes down on the landing deck's middle (bisection on the angle)
    y_foot = run_y[-1]
    def kicker(ang):
        th1 = math.radians(ang); R = R_KICK; Lk = R * th1
        foot = J["run"] - Lk; ds, dy = [], []; y = y_foot; s = foot
        for i in range(int(Lk) + 1):
            ds.append(round(s, 2)); dy.append(round(y, 3)); th = min(1.0, (i + .5) / Lk) * th1; y += math.tan(th); s += 1.0
        return ds, dy, R
    def lip_speed(top, acc, ds, dy):
        v = 1.0
        for i in range(len(run_s) - 1):
            L = run_s[i + 1] - run_s[i]; gr = (run_y[i + 1] - run_y[i]) / L
            for _ in range(int(L)): a = acc * max(0, 1 - v / top) - 9.81 * gr - 0.02 * v; v = math.sqrt(max(1, v * v + 2 * a))
        for _ in range(int(ds[0] - run_s[-1])): a = acc * max(0, 1 - v / top) - 0.02 * v; v = math.sqrt(max(1, v * v + 2 * a))   # (the pad: level)
        return CORR * math.sqrt(max(1.0, v * v - 2 * 9.81 * (dy[-1] - dy[0])))
    def traj(y0, ang, v, x): th = math.radians(ang); return y0 + math.tan(th) * x - 9.81 * x * x / (2 * v * v * math.cos(th) ** 2)
    def fall(ang, v, x): th = math.radians(ang); vx = v * math.cos(th); return math.atan2(9.81 * x / vx - v * math.sin(th), vx)
    # the landing deck: its top DECK_UP over the bench at the edge (+2 m, its front face's thickness), sloping down at the window's middle flight's
    # descent less LAND_REL to the bench
    def deck(ang, v_mid, ds, dy):
        front = edge - J["overhang"]
        x_mid = front + 0.4 * J["land_l"] - ds[-1]
        alpha = min(math.radians(14.0), max(math.radians(6.0), fall(ang, v_mid, x_mid) - math.radians(LAND_REL)))
        ls = list(np.arange(front, front + J["land_l"] + 0.01, 1.0)); ly = [bench - math.tan(alpha) * (s - front) for s in ls]
        return ls, ly, alpha
    def comes_down(y0, ang, v, s0, surf):
        x = 5.0
        while x < 700:
            if traj(y0, ang, v, x) <= surf(s0 + x): return x
            x += 0.25
        return None
    # pick the angle: the jump's design vehicle flat out comes down 35 % along the deck
    def score(ang):
        ds, dy, R = kicker(ang); v = lip_speed(*VEHICLES[J["design"]], ds, dy)
        ls, ly, al = deck(ang, v, ds, dy)
        surf = lambda s: float(np.interp(s, ls, ly)) if ls[0] <= s <= ls[-1] else (max(WATER, raw(s)) if s < ls[0] else raw(s))
        x = comes_down(dy[-1], ang, v, ds[-1], surf) or 999
        return (ds[-1] + x) - (ls[0] + 0.3 * (ls[-1] - ls[0]))
    lo, hi = 4.0, 32.0
    for _ in range(40):
        mid = (lo + hi) / 2
        if score(mid) > 0: hi = mid
        else: lo = mid
    ang = (lo + hi) / 2
    deck_s, deck_y, R = kicker(ang); s_lip, y_lip = deck_s[-1], deck_y[-1]
    v_mid = lip_speed(*VEHICLES[J["design"]], deck_s, deck_y)
    land_s, land_y, alpha = deck(ang, v_mid, deck_s, deck_y)
    surf = lambda s: float(np.interp(s, land_s, land_y)) if land_s[0] <= s <= land_s[-1] else (max(WATER, raw(s)) if s < land_s[0] else raw(s))
    # the window: the lip speeds that come down on the deck (its first 3 m to its last 3 m)
    def land_at(v): x = comes_down(y_lip, ang, v, s_lip, surf); return None if x is None else s_lip + x
    def speed_for(target):
        a, b = 15.0, 80.0
        for _ in range(40):
            m_ = (a + b) / 2; la = land_at(m_)
            if la is None or la > target: b = m_
            else: a = m_
        return (a + b) / 2
    v_lo, v_hi = speed_for(land_s[0] + 3.0), speed_for(land_s[-1] - 3.0)
    flights = {}
    for name, (top, acc) in VEHICLES.items():
        v = lip_speed(top, acc, deck_s, deck_y); la = land_at(v) or 0.0; x = la - s_lip; t = x / (v * math.cos(math.radians(ang)))
        apex = max(traj(y_lip, ang, v, xx) for xx in np.arange(0, x + 0.1, 1.0)) - y_lip
        flights[name] = dict(lip_speed=round(v, 1), air_s=round(t, 2), dist_m=round(x, 1), over_lip_m=round(apex, 1), lands_at=round(la, 1),
                             on_deck=bool(land_s[0] <= la <= land_s[-1]), descent_deg=round(math.degrees(fall(ang, v, x)), 1))
    tall = max(yy - raw(ss) for ss, yy in zip(deck_s, deck_y))
    # ---- the grades: the run-up lane (kept off the runway and taxiways), the pad round the kicker (flat at its foot's height, the rim's mounds cut off;
    # never past the lip), the catch past the landing deck's foot, and the bleachers' pads
    # the lane in pieces between the runway and taxiways (open ends: nothing graded past a piece's ends, so the paving keeps its own surface)
    lanes = [list(zip(run_s, run_y))]
    lane_grades = [{"id": f"quarry jump {J['id']} run-up {k}", "points": flat([P(s_) for s_, _ in ln]), "y": [round(y_, 3) for _, y_ in ln], "half": DECK_HALF + 3.0, "blend": 8.0,
                    "open_ends": True} for k, ln in enumerate(lanes)]
    # (Round 73: the pad level with the deck's foot: 0.6 m under it left a step the check's bike stuck at five times)
    pad_s = list(np.arange(run_s[-1], s_lip - 1.0, 2.0))
    pad = {"id": f"quarry jump {J['id']} kicker pad", "points": flat([P(s) for s in pad_s]), "y": [round(y_foot, 3)] * len(pad_s), "half": PAD_HALF, "blend": 10.0, "open_ends": True}
    # under the landing deck: the bench cut down to the deck (it lies on its ground: nothing under it), then the cut's floor climbing back to the bench
    depth = bench - land_y[-1]; back_L = depth / RISE_BACK
    # (Round 74, Dan's BUG-003 / BUG-004 of 2026-10-27: the ground stood up to 11 m through J2's and J3's decks a few meters before the bench's edge (the
    # face's top is not square across the deck): the cut now starts where the ground anywhere across the deck first comes within 2 m of it, 4 m sooner)
    def proud(s):
        dy = float(np.interp(s, land_s, land_y)); return max(raw(s, lat) - dy for lat in np.arange(-(LAND_HALF + 4.0), LAND_HALF + 4.01, 2.0))
    cut_from = next((float(s) for s in np.arange(land_s[0], edge + 0.1, 1.0) if proud(s) > -2.0), edge) - 4.0
    ro_s = list(np.arange(max(land_s[0], min(edge, cut_from)), land_s[-1] + back_L + 30.0, 2.0))   # (from there: over the face the deck stands on its trestle)
    def ro_at(s):
        if s <= land_s[-1]: return float(np.interp(s, land_s, land_y)) - 0.25
        y = land_y[-1] - 0.25 + RISE_BACK * (s - land_s[-1]); return min(y, max(bench, raw(s))) if s > land_s[-1] + back_L else y
    ro_y = [ro_at(s) for s in ro_s]
    runout = {"id": f"quarry jump {J['id']} landing cut and catch", "points": flat([P(s) for s in ro_s]), "y": [round(v, 3) for v in ro_y], "half": LAND_HALF + 3.0, "blend": 16.0, "open_ends": True}   # (Round 74: 3 m clear each side, gentler banks; were 1.5, 12)
    under = None
    stands, pads = [], []
    # Round 73 (C2): each stand at the first of its candidate spots clear of every road (24 m from a road's middle) and the jump's own corridor
    def road_clear(q):
        for rd in ROADS_NEAR:
            for u, v in zip(rd, rd[1:]):
                ab = (v[0] - u[0], v[1] - u[1]); L2 = ab[0] ** 2 + ab[1] ** 2
                t = 0 if L2 < 1e-9 else max(0, min(1, ((q[0] - u[0]) * ab[0] + (q[1] - u[1]) * ab[1]) / L2))
                if math.dist(q, (u[0] + ab[0] * t, u[1] + ab[1] * t)) < 24.0: return False
        return True
    spots = []
    for side, sgn in ((1, 1.0), (2, -1.0)):
        cands = [P(land_s[-1] + a, sgn * (LAND_HALF + l)) for a in (20.0, 0.0, -20.0, 45.0) for l in (34.0, 48.0, 62.0)]
        spots.append((next((c for c in cands if road_clear(c)), None), (J["heading"] + 90.0 * sgn) % 360))
    kc = [P(s_lip - a, l) for a in (60.0, 90.0, 120.0) for l in (32.0, -32.0, 45.0, -45.0)]
    k = next((c for c in kc if road_clear(c)), None)
    spots.append((k, (J["heading"] + 90.0 * (1 if k is None or ((k[0] - P(s_lip - 60.0)[0]) * nl[0] + (k[1] - P(s_lip - 60.0)[1]) * nl[1]) > 0 else -1)) % 360))
    for at, face_yaw in [sp for sp in spots if sp[0] is not None]:
        gy = float(np.median([ground(at[0] + dx, at[1] + dz) for dx in (-12, 0, 12) for dz in (-6, 0, 6)]))
        stands.append(dict(at=[round(at[0], 2), round(at[1], 2)], yaw=round(face_yaw, 1), y=round(gy, 3)))
        pads.append({"id": f"quarry jump {J['id']} bleachers' pad {len(pads)}", "points": flat([at, (at[0] + 0.01, at[1])]), "y": [round(gy, 3)] * 2, "half": 16.0, "blend": 8.0})
    clear = [{"id": f"quarry jump {J['id']}", "points": flat([P(0.0), P(ro_s[-1])]), "half": 18.0}]
    for st in stands: clear.append({"id": f"quarry jump {J['id']} bleachers", "points": flat([st["at"], (st["at"][0] + 0.01, st["at"][1])]), "half": 18.0})
    zone_l = [P(s, LAND_HALF + 3) for s in (land_s[0] - 2, land_s[-1] + back_L)]; zone_r = [P(s, -LAND_HALF - 3) for s in (land_s[0] - 2, land_s[-1] + back_L)]
    zones = [{"id": f"quarry jump {J['id']} landing", "poly": flat(zone_l + zone_r[::-1]), "catch": True}]
    grades = lane_grades + [pad, runout] + pads
    # (the speed window shown on the HUD: the exe's flights carry a little less far than this ballistic model: the Street Classic off The Corner at
    # 43.7 m/s came down at 250 m, where the model puts 41.3 m/s: the window's speeds raised by that much)
    FLIGHT_CORR = 43.7 / 41.3; v_lo, v_hi = v_lo * FLIGHT_CORR, v_hi * FLIGHT_CORR
    jump = dict(window=dict(lo=round(v_lo, 1), hi=round(v_hi, 1), lands=[round(land_s[0] + 3 - s_lip, 1), round(land_s[-1] - 3 - s_lip, 1)]), id=J["id"], name=J["name"],
                origin=[round(o[0], 2), round(o[1], 2)], dir=[round(d[0], 5), round(d[1], 5)], run_s=[round(v, 2) for v in run_s], run_y=[round(v, 3) for v in run_y],
                deck_s=deck_s, deck_y=deck_y, half=DECK_HALF, angle_deg=round(ang, 2), radius_m=round(R, 1), lip_s=s_lip, lip_y=round(y_lip, 3), tallest_m=round(tall, 1),
                land_s=[round(v, 2) for v in land_s], land_y=[round(v, 3) for v in land_y], land_half=LAND_HALF, land_angle_deg=round(math.degrees(alpha), 1),
                bench_y=round(bench, 2), edge_s=round(edge, 1), runout_to=round(ro_s[-1], 1), water_y=WATER, design=J["design"],
                start=flat([P(6.0)]), start_yaw=J["heading"], gantry=dict(at=flat([P(10.0)]), yaw=J["heading"]), stands=stands, flights=flights)
    return jump, grades, clear, zones


out = {"note": "Round 72 / 73: the Viking Quarry jumps (Free Roam events), from Tools/mapgen/make_quarry_jumps.py; merged into the mountain's planning "
               "pieces and built by MapBuilder.QuarryJumps.cs; run by Stunts/QuarryJumps.cs. Coordinates are map meters (= Unity x, z).",
       "grades": [], "clear_lines": [], "landing_zones": [], "jumps": []}
for J in JUMPS:
    jump, grades, clear, zones = design(J)
    out["jumps"].append(jump); out["grades"] += grades; out["clear_lines"] += clear; out["landing_zones"] += zones
    w = jump["window"]
    print(f"{jump['id']} {jump['name']}: lip {jump['angle_deg']} deg (R {jump['radius_m']:.0f}), {jump['lip_y']:.1f} m ({jump['tallest_m']} m over the ground), the far bench "
          f"{jump['bench_y']:.0f} m from {jump['edge_s'] - jump['lip_s']:.0f} m out; landing deck {jump['land_s'][0] - jump['lip_s']:.0f} to {jump['land_s'][-1] - jump['lip_s']:.0f} m out at "
          f"{jump['land_angle_deg']} deg; window {w['lo']} to {w['hi']} m/s ({w['lo'] * 2.237:.0f} to {w['hi'] * 2.237:.0f} mph)")
    for n, f in jump["flights"].items():
        print(f"   {n:17} lip {f['lip_speed']:5.1f} m/s: {f['air_s']:4.1f} s, {f['dist_m']:5.0f} m, +{f['over_lip_m']:4.1f} m, lands {'ON THE DECK' if f['on_deck'] else 'past it (catch)' if f['lands_at'] > jump['land_s'][-1] else 'SHORT'}, {f['descent_deg']:.0f} deg down")
# ---------------------------------------------------------------- Round 73 (C3, Dan: "Truly don't even understand how to get to the ramps in general"):
# the quarry jumps drive, a gravel track from the airport's service road by The Big One's start, behind each start box, to the service road east of
# The Corner's (both turn-offs signed QUARRY JUMPS), so a player on the roads finds every start; each box has its jump's board
def chaikin(p, it=3):
    for _ in range(it):
        q = [p[0]]
        for a, b in zip(p[:-1], p[1:]): q += [(0.75 * a[0] + 0.25 * b[0], 0.75 * a[1] + 0.25 * b[1]), (0.25 * a[0] + 0.75 * b[0], 0.25 * a[1] + 0.75 * b[1])]
        p = q + [p[-1]]
    return p
def behind(j, m): o, d = j["origin"], j["dir"]; return (j["start"][0] - d[0] * m, j["start"][1] - d[1] * m)
J1_, J2_, J3_ = out["jumps"]
WEST_END, EAST_END = (-2296.0, -2060.0), (-1472.0, -1923.0)   # on the service roads (OSM 7091, 8910)
ctrl = [WEST_END, behind(J3_, 12.0), (-2150.0, -2036.0), behind(J2_, 12.0), (-1820.0, -1972.0), behind(J1_, 12.0), (-1560.0, -1912.0), EAST_END]
drive = chaikin(ctrl, 3)
dense = []
for a, b in zip(drive, drive[1:]):
    n = max(1, int(math.dist(a, b) / 2.0)); dense += [(a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n) for k in range(n)]
dense.append(drive[-1])
dy = [ground(*q) for q in dense]
for _ in range(3): dy = [float(np.mean(dy[max(0, i - 5):i + 6])) for i in range(len(dy))]
out["grades"].append({"id": "the quarry jumps drive", "points": flat(dense), "y": [round(v, 3) for v in dy], "half": 3.5, "blend": 5.0, "open_ends": True})
out["clear_lines"].append({"id": "the quarry jumps drive", "points": flat(dense[::4] + [dense[-1]]), "half": 7.0})
def yaw_of(a, b): return round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 1)
out["access"] = {"drive": flat(dense), "half": 3.5,
                 # the turn-off boards: beside each end, facing a driver on the service road (toward the drive's first meters), the arrow along the drive
                 "signs": [dict(at=flat([(dense[6][0] + 5.0, dense[6][1] + 5.0)]), facing=yaw_of(dense[8], dense[0]), text="QUARRY JUMPS"),
                           dict(at=flat([(dense[-7][0] + 5.0, dense[-7][1] - 5.0)]), facing=yaw_of(dense[-9], dense[-1]), text="QUARRY JUMPS")],
                 "boards": [dict(jump=j["id"], at=flat([(j["start"][0] - j["dir"][1] * 9.0 - j["dir"][0] * 2.0, j["start"][1] + j["dir"][0] * 9.0 - j["dir"][1] * 2.0)]), facing=(j["start_yaw"] + 180.0) % 360) for j in out["jumps"]],
                 # Round 74 (E, Dan's BUG-004: on The Big One's landing deck from the north, "ramp is blocked. I don't understand where these ramps lead
                 # to"): a LANDING board beside each landing deck's run-out, facing anyone arriving from the bench's side (the north): the deck is a landing,
                 # its run-up is across the quarry
                 "landings": [dict(jump=j["id"], at=flat([(j["origin"][0] + j["dir"][0] * (j["land_s"][-1] + 12.0) - j["dir"][1] * (LAND_HALF + 7.0),
                                                           j["origin"][1] + j["dir"][1] * (j["land_s"][-1] + 12.0) + j["dir"][0] * (LAND_HALF + 7.0))]),
                                   facing=math.degrees(math.atan2(j["dir"][0], j["dir"][1])) % 360) for j in out["jumps"]]}
print(f"the quarry jumps drive: {sum(math.dist(a, b) for a, b in zip(dense, dense[1:])):.0f} m, from ({WEST_END[0]:.0f}, {WEST_END[1]:.0f}) behind the three start boxes to ({EAST_END[0]:.0f}, {EAST_END[1]:.0f})")
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, axs = plt.subplots(3, 1, figsize=(18, 16))
    for ax, j in zip(axs, out["jumps"]):
        o, d = j["origin"], j["dir"]; P = lambda s: (o[0] + d[0] * s, o[1] + d[1] * s)
        xs = np.arange(0, j["runout_to"], 2.0); ax.plot(xs, [max(WATER, ground(*P(s))) for s in xs], color="#999")
        ax.plot(j["run_s"], j["run_y"], "g"); ax.plot(j["deck_s"], j["deck_y"], "k", lw=2); ax.plot(j["land_s"], j["land_y"], "k", lw=3)
        th = math.radians(j["angle_deg"])
        for n, f in j["flights"].items():
            v = f["lip_speed"]; xx = np.linspace(0, f["dist_m"], 100); ax.plot(j["lip_s"] + xx, [j["lip_y"] + math.tan(th) * x - 9.81 * x * x / (2 * v * v * math.cos(th) ** 2) for x in xx], "--", label=f"{n} {v} m/s")
        ax.axhline(WATER, color="c"); ax.set_aspect("equal"); ax.grid(alpha=.3); ax.legend(fontsize=7); ax.set_title(j["id"] + " " + j["name"])
    os.makedirs(os.path.join(ROOT, "local/round73"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round73/quarry_jumps.png"), dpi=60, bbox_inches="tight")
