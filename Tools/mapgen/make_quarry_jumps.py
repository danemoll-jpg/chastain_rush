"""Round 72 (Dan, 2026-10-26 07:17: "Quarry stunt jumps"; 2026-10-25 02:11: "stunt races that have large ramps to jump you over evel knievel
style"): THE VIKING QUARRY JUMPS, three Evel Knievel stunt jumps across Viking Quarry Lake (Free Roam events; world pieces built by the map build).

Each jump: a straight run-up from the airport's south apron north across the runway and the grass to a tall steel kicker on the quarry's south rim,
a flight across the lake, a steel landing ramp on the far side sloped to the flight's descent (a catch slope: RampHold sets a vehicle on its wheels),
its foot run out onto graded ground. Designed from the flights (as Round 68 / 70): the kicker's angle and height and the landing ramp's place and length
from each vehicle's speed at the lip (the slowest car to the fastest bike, from rest down the run-up):
  J1 "The Corner"   across the lake's east end, rim to rim
  J2 "The Long One" across the middle onto the north bench
  J3 "The Big One"  across the widest part, south-west rim to the north-east bench (the longest jump after the summit ramp, Round 70 A)
Miss the landing ramp and you are in the lake (the game's water: reset). Why the south rim: the airport's flat ground gives every jump a long
straight run-up with nothing in the way; the far side is open ground (the north bench, the east rim) to run out on.

What it writes: Data/tracks/quarry_jumps.json (per jump: the line, the run-up, the kicker's deck, the landing deck, the run-out grade, the flights, the
start gantry, bleachers; and the RidgeFile shape's grades / clear_lines / landing_zones, merged into the mountain's pieces by MapBuilder.QuarryJumps.cs
like the summit ramp's). Re-run after mapgen, then Build Map (a full build: the trees cleared, the ground graded). Then python
Tools/mapgen/add_extension_places.py (the fast-travel spot at the gantries).
Usage: python Tools/mapgen/make_quarry_jumps.py [--plot]"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/quarry_jumps.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
WATER = 113.6   # the lake's surface (the DEM's flat water)


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def flat(p): return [round(v, 2) for q in p for v in q]


# (name, label, start x, z, heading, lip station, lip angle, kicker radius, how high the lip stands over the ground there, landing slope's extra)
# the lines (from a rim scan of the planning's ground): the kicker's lip just short of the south rim's edge, its heading, the run-up's length back
# from the lip, the far side's edge and where the window's middle speed should come down (meters from the lip), the speed window at the lip
JUMPS = [
    dict(id="J1", name="The Corner", lip=(-1743.6, -1595.0), heading=20.0, run=380.0, edge=179.0, land=198.0, window=(36.5, 39.5)),
    dict(id="J2", name="The Long One", lip=(-2000.0, -1647.0), heading=0.0, run=380.0, edge=190.0, land=232.0, window=(41.5, 44.5)),
    dict(id="J3", name="The Big One", lip=(-2489.7, -1674.9), heading=15.0, run=320.0, edge=270.0, land=292.0, window=(41.5, 44.5)),
]
# (top m/s, push m/s2) as Round 68 / 70: the slowest car, the default, a fast car, the bikes, the stunt vehicles
VEHICLES = {"Street Classic": (49.0, 14.5), "Highball Fastback": (52.0, 16.0), "Drifter Twin": (58.0, 16.0), "Needle 600": (61.0, 18.0),
            "Stunt Buggy": (50.0, 17.5), "Stunt Bike": (55.0, 18.0)}
DECK_HALF = 4.5; LAND_HALF = 9.0; RUNOUT = 120.0; CORR = 0.93


def design(J, angle=None, solve=True):
    if solve:
        # the lip's angle: the window's middle speed comes down MID_UP m over the ground at the jump's land_s (bisection on the angle)
        lo, hi = 6.0, 24.0
        for _ in range(30):
            mid = (lo + hi) / 2; e = design(J, mid, False)
            if e > 0: hi = mid
            else: lo = mid
        return design(J, (lo + hi) / 2, False) if False else design_final(J, (lo + hi) / 2)
    return design_body(J, angle, True)
def design_final(J, angle): return design_body(J, angle, False)
def design_body(J, angle, probe):
    d = (math.sin(math.radians(J["heading"])), math.cos(math.radians(J["heading"]))); nl = (-d[1], d[0])
    o = (J["lip"][0] - d[0] * J["run"], J["lip"][1] - d[1] * J["run"]); J["lip_s"] = J["run"]; J["edge_s"] = J["run"] + J["edge"]; J["land_s"] = J["run"] + J["land"]
    def P(s, lat=0.0): return (o[0] + d[0] * s + nl[0] * lat, o[1] + d[1] * s + nl[1] * lat)
    # the run-up: straight on the ground, up to the kicker's foot
    # the kicker's radius: the window's top speed pressing 8 m/s2 on the suspension (a held steel deck: Round 70 took 6.8; Round 68: about 6 on a ground kicker)
    th1 = math.radians(angle); R = J["window"][1] ** 2 / 8.0; Lk = R * th1
    foot = J["lip_s"] - Lk
    run_s = list(np.arange(0.0, foot + 0.1, 5.0)); run_y = [ground(*P(s)) for s in run_s]
    # graded smooth (an 80 m running mean of the ground: the first check's cars took off over the airport's humps at 40 m/s); the runway and
    # taxiways it crosses are laid on the graded ground by the map build after it
    k = 16; run_y = [float(np.mean(run_y[max(0, i - k):i + k + 1])) for i in range(len(run_y))]
    y_foot = run_y[-1]
    deck_s, deck_y = [], []; y = y_foot; s = foot
    for i in range(int(Lk) + 1):
        deck_s.append(round(s, 2)); deck_y.append(round(y, 3)); th = min(1.0, (i + .5) / Lk) * th1; y += math.tan(th); s += 1.0
    s_lip, y_lip = deck_s[-1], deck_y[-1]
    tall = max(yy - ground(*P(ss)) for ss, yy in zip(deck_s, deck_y))

    def lip_speed(top, acc):
        v = 1.0
        for i in range(len(run_s) - 1):
            L = run_s[i + 1] - run_s[i]; g = (run_y[i + 1] - run_y[i]) / L
            for _ in range(int(L)): a = acc * max(0, 1 - v / top) - 9.81 * g - 0.02 * v; v = math.sqrt(max(1, v * v + 2 * a))
        # up the kicker (a held ramp: nothing lost but the climb); then CORR: Round 70's summit ramp flew 8 to 16 % shorter in the exe than this
        # model said (its lip speeds lower than modelled), so the speeds here are taken 7 % lower
        return CORR * math.sqrt(max(1.0, v * v - 2 * 9.81 * (y_lip - y_foot)))
    flights = {}
    for name, (top, acc) in VEHICLES.items():
        v = lip_speed(top, acc); flights[name] = dict(lip_speed=round(v, 1), vx=v * math.cos(th1), vy=v * math.sin(th1))
    # the speed window at the lip (Evel Knievel's speed check: a lit sign at the run-up and the HUD show it), 3 m/s wide: The Corner's within every
    # vehicle's reach flat out, The Long One's and The Big One's only the faster ones' (the bikes, the ATV, the fast cars). Slower: short, into the
    # lake. Faster: past the landing deck's end.
    vmin, vmax = J["window"]
    def traj(v, x): return y_lip + math.tan(th1) * x - 9.81 * x * x / (2 * v * v * math.cos(th1) ** 2)
    # the landing ramp: a straight slope through the slowest and fastest flights' points where they come down past the far side's ground + 12 m
    # (it stands on trestles there), its angle the mean of their descent angles, then curving (R 90) out onto the ground
    far = [(s, ground(*P(s))) for s in np.arange(s_lip + 40, s_lip + 520, 2.0)]
    def come_down(v, top_y, x0=40.0):
        # where the flight comes down onto the surface: the last time it goes from over it to under it (a deck line stands over the lip behind)
        x = x0; above = traj(v, x) > top_y(s_lip + x); last = None
        while x < 620:
            now = traj(v, x) > top_y(s_lip + x)
            if above and not now: return x
            above = now; x += 0.5
        return last
    # the landing deck: straight, MISMATCH degrees shallower than the window's middle speed comes down (a clean landing; the catch slope sets a
    # vehicle on its wheels), placed so that speed comes down MID_UP m over the far side's ground; 15 m of deck before the window's slowest touch-down
    # and 15 m past its fastest, then on down at its slope to the ground. A flatter deck than the flights keeps the window's landings close together.
    MID_UP, MISMATCH = 8.0, 14.0
    def slope_at(v, x): vx = v * math.cos(th1); return math.atan2(9.81 * x / vx - v * math.sin(th1), vx)
    def gnd(s): return max(WATER, ground(*P(s)))
    vmid = (vmin + vmax) / 2
    x_mid = J["land_s"] - s_lip
    if probe: return traj(vmid, x_mid) - (gnd(J["land_s"]) + MID_UP)   # (the angle's bisection: over (+) or under (-) the mark)
    y_mid = traj(vmid, x_mid); alpha = slope_at(vmid, x_mid) - math.radians(MISMATCH)
    def deck_line(s): return y_mid - math.tan(alpha) * (s - (s_lip + x_mid))
    x_lo = come_down(vmin, deck_line, 30.0); x_hi = come_down(vmax, deck_line, 30.0)
    s_a = s_lip + x_lo - 15.0
    land_s, land_y = [], []
    s = s_a
    while True:
        y = deck_line(s)
        gy = ground(*P(s))
        if s > s_lip + x_hi + 15.0 and y <= gy + 1.0: break
        land_s.append(round(s, 2)); land_y.append(round(y, 3)); s += 1.0
        if s > s_a + 260: break
    # past the fastest landing + 15 m, ease the slope out (R 90) toward the ground's grade
    s_end = land_s[-1]
    rows = []
    for name, f in flights.items():
        v = f["lip_speed"]; x = come_down(v, deck_line, s_a - s_lip) or 0.0; t = x / f["vx"]; apex = max([traj(v, xx) for xx in np.arange(0, x, 1.0)] + [y_lip]) - y_lip
        f.update(dict(air_s=round(t, 2), dist_m=round(x, 1), over_lip_m=round(apex, 1), lands_at=round(s_lip + x, 1), on_deck=bool(land_s[0] <= s_lip + x <= land_s[-1]),
                      land_speed=round(math.hypot(f["vx"], f["vy"] - 9.81 * t), 1), descent_deg=round(math.degrees(slope_at(v, x)), 1)))
        del f["vx"], f["vy"]
    # the run-out: the ground from the landing deck's foot graded smooth along the line for RUNOUT m, level across
    ro_s = list(np.arange(s_end - 2.0, s_end + RUNOUT, 2.0)); ro_y = [ground(*P(s)) for s in ro_s]
    ro_y[0] = land_y[-1]
    for _ in range(3): ro_y = [ro_y[0]] + [float(np.mean(ro_y[max(0, i - 6):i + 7])) for i in range(1, len(ro_y))]
    # the run-up lane and the ground under the kicker graded: the lane to the smoothed run-up, under the deck 0.6 m below it (the rim's ground stood
    # over the deck: the first check's cars hit it); then the run-out
    lane_s = run_s + [s_ for s_ in deck_s[1:]]; lane_y = run_y + [y_ - 0.6 for y_ in deck_y[1:]]
    grades = [
        {"id": f"quarry jump {J['id']} run-up and kicker", "points": flat([P(s_) for s_ in lane_s]), "y": [round(v, 3) for v in lane_y], "half": DECK_HALF + 2.0, "blend": 8.0},
        {"id": f"quarry jump {J['id']} run-out", "points": flat([P(s) for s in ro_s]), "y": [round(v, 3) for v in ro_y], "half": LAND_HALF + 3.0, "blend": 10.0},
    ]
    clear = [{"id": f"quarry jump {J['id']}", "points": flat([P(0.0), P(s_end + RUNOUT)]), "half": 16.0}]
    zone_l = [P(s, LAND_HALF + 2) for s in (land_s[0] - 4, s_end + 40)]; zone_r = [P(s, -LAND_HALF - 2) for s in (land_s[0] - 4, s_end + 40)]
    zones = [{"id": f"quarry jump {J['id']} landing", "poly": flat(zone_l + zone_r[::-1]), "catch": True}]
    # the start gantry at the run-up's start; bleachers either side of the landing (20 m off the deck's edge) and one by the kicker
    gantry = dict(at=flat([P(6.0)]), yaw=J["heading"])
    stands = [dict(at=flat([P(s_end + 12.0, side * (LAND_HALF + 20.0))]), yaw=(J["heading"] - side * 90.0) % 360) for side in (-1, 1)]
    stands.append(dict(at=flat([P(s_lip - 70.0, 26.0)]), yaw=(J["heading"] - 90.0) % 360))
    clear_edge = traj(vmin, J["edge_s"] - s_lip) - gnd(J["edge_s"])
    window = dict(reach=[n for n, f in flights.items() if f["lip_speed"] >= vmin], lo=round(vmin, 1), hi=round(vmax, 1), lands=[round(x_lo, 1), round(x_hi, 1)], slowest_over_edge_m=round(clear_edge, 1))
    J["angle"] = round(angle, 2)
    jump = dict(window=window, id=J["id"], name=J["name"], origin=[round(o[0], 2), round(o[1], 2)], dir=[round(d[0], 5), round(d[1], 5)],
                run_s=[round(v, 2) for v in run_s], run_y=[round(v, 3) for v in run_y],
                deck_s=deck_s, deck_y=deck_y, half=DECK_HALF, angle_deg=J["angle"], radius_m=R, lip_s=s_lip, lip_y=round(y_lip, 3), tallest_m=round(tall, 1),
                land_s=land_s, land_y=land_y, land_half=LAND_HALF, land_angle_deg=round(math.degrees(alpha), 1), runout_to=round(s_end + RUNOUT, 1),
                water_y=WATER, start=flat([P(4.0)]), start_yaw=J["heading"], gantry=gantry, stands=stands, flights=flights)
    return jump, grades, clear, zones


out = {"note": "Round 72: the Viking Quarry jumps (Free Roam events), from Tools/mapgen/make_quarry_jumps.py; merged into the mountain's planning "
               "pieces and built by MapBuilder.QuarryJumps.cs; run by Stunts/QuarryJumps.cs. Coordinates are map meters (= Unity x, z).",
       "grades": [], "clear_lines": [], "landing_zones": [], "jumps": []}
for J in JUMPS:
    jump, grades, clear, zones = design(J)
    out["jumps"].append(jump); out["grades"] += grades; out["clear_lines"] += clear; out["landing_zones"] += zones
    w = jump["window"]; print(f"{jump['id']} {jump['name']}: the speed window {w['lo']} to {w['hi']} m/s at the lip ({w['lo'] * 2.237:.0f} to {w['hi'] * 2.237:.0f} mph): lands {w['lands'][0]} to {w['lands'][1]} m out, the slowest {w['slowest_over_edge_m']} m over the far edge; lip {J['angle']} deg; in reach of: {', '.join(w['reach'])}")
    print(f"   run-up {jump['deck_s'][0]:.0f} m, kicker {len(jump['deck_s'])} m to {jump['angle_deg']} deg (R {jump['radius_m']:.0f}), lip {jump['lip_y']:.1f} m "
          f"({jump['tallest_m']} m over the ground); landing deck {jump['land_s'][0] - jump['lip_s']:.0f} to {jump['land_s'][-1] - jump['lip_s']:.0f} m out, "
          f"{len(jump['land_s'])} m long at {jump['land_angle_deg']} deg, its top {jump['land_y'][0]:.0f} m")
    for n, f in jump["flights"].items():
        print(f"   {n:17} lip {f['lip_speed']:5.1f} m/s: {f['air_s']:4.1f} s, {f['dist_m']:5.0f} m, +{f['over_lip_m']:4.1f} m over the lip, lands {'ON THE DECK' if f['on_deck'] else 'OFF'} "
              f"at {f['land_speed']:.0f} m/s, {f['descent_deg']:.0f} deg down")
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, axs = plt.subplots(3, 1, figsize=(18, 16))
    for ax, j in zip(axs, out["jumps"]):
        o, d = j["origin"], j["dir"]; P = lambda s: (o[0] + d[0] * s, o[1] + d[1] * s)
        xs = np.arange(0, j["runout_to"], 2.0); ax.plot(xs, [max(WATER, ground(*P(s))) for s in xs], color="#999")
        ax.plot(j["run_s"], j["run_y"], "g"); ax.plot(j["deck_s"], j["deck_y"], "k", lw=2); ax.plot(j["land_s"], j["land_y"], "k", lw=2)
        th = math.radians(j["angle_deg"])
        for n, f in j["flights"].items():
            v = f["lip_speed"]; xx = np.linspace(0, f["dist_m"], 100); ax.plot(j["lip_s"] + xx, [j["lip_y"] + math.tan(th) * x - 9.81 * x * x / (2 * v * v * math.cos(th) ** 2) for x in xx], "--", label=f"{n} {v} m/s")
        ax.axhline(WATER, color="c"); ax.set_aspect("equal"); ax.grid(alpha=.3); ax.legend(fontsize=7); ax.set_title(j["id"] + " " + j["name"])
    os.makedirs(os.path.join(ROOT, "local/round72"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round72/quarry_jumps.png"), dpi=60, bbox_inches="tight")
