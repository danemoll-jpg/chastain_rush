"""Round 70 (A, Dan, 2026-10-25 23:20: "I want a big ramp on the edge that drops all the way off the mountain ... at the moment just give me a big ramp
that will clear the mountain"): THE SUMMIT RAMP, a Free Roam world piece on Kennesaw Mountain (not in the race's lap).

Where and why: the drive rings the summit's knob 100 to 250 m out on every side, so there is no 200 m straight on the knob itself, and the ridge's
north-east run off the summit lot is the race's J1. The drive's own straight along the south flank (from its bend at x -988, z -6126 to the hairpin
round the knob's west end, 254 m at about +9 %) is the run-up: drive it flat out from the fast-travel spot and go straight on past the hairpin's
outside onto a steel stunt ramp standing out over the north-west face. Heading 296 degrees, the line threads the gap between Greymont Circle's houses:
the face falls about 200 m under the flight, the landings come down on the open ground at the mountain's foot (the park's west side), and the first
house within 35 m of the line is 960 m out; the run-out ends in a gravel arrester bed and a graded track on to Old Mountain Road NW (the way back).

What it writes: Data/tracks/kennesaw_summit_ramp.json (the RidgeFile shape: grades, clear_lines, break_lines, landing_zones; and 'ramp': the deck's
line and heights, its width, the arrester bed, the race-time barrier across the ramp's mouth, the fast-travel start). MapBuilder.Mountain.cs merges
its grades / lines / zones into the mountain's and builds the ramp (MapBuilder.SummitRamp.cs). Re-run after mapgen, then Build Map (or the quick
step MountainRace). Then python Tools/mapgen/add_extension_places.py (the fast-travel spot).
Usage: python Tools/mapgen/make_summit_ramp.py [--plot]"""
import json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/kennesaw_summit_ramp.json")
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain_full.json")))
H = np.memmap(os.path.join(ROOT, "Assets/Generated/terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
SX, SZ, OX, OZ = meta["step_x_m"], meta["step_z_m"], meta["origin_x_m"], meta["origin_z_m"]
HLO, HHI = meta["min_height"], meta["max_height"]
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))


def ground(x, z):
    fx, fz = (x - OX) / SX, (z - OZ) / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    h = H[j:j + 2, i:i + 2].astype(np.float64) / 65535.0 * (HHI - HLO) + HLO
    return float(h[0, 0] * (1 - a) * (1 - b) + h[0, 1] * a * (1 - b) + h[1, 0] * (1 - a) * b + h[1, 1] * a * b)
def flat(p): return [round(v, 2) for q in p for v in q]
def gsm(vals, sig):
    out = []
    for i in range(len(vals)):
        w = [math.exp(-((k - i) / sig) ** 2 / 2) for k in range(max(0, i - int(3 * sig)), min(len(vals), i + int(3 * sig) + 1))]
        out.append(sum(vals[k] * wk for k, wk in zip(range(max(0, i - int(3 * sig)), min(len(vals), i + int(3 * sig) + 1)), w)) / sum(w))
    return out


# ---------------------------------------------------------------- hand choices
DRIVE = 4729                    # Kennesaw Mountain Drive
RUN_FROM, RUN_TO = 78, 92       # its points: the run-up's start (the fast-travel spot) and its last before the hairpin
BRG = 296.0                     # the ramp's heading (degrees from north): between Greymont Circle's houses (see the docstring)
PAD_FROM = 24.0                 # past the drive's point 92 the drive runs on along the line for about 20 m (its hairpin's outer lane), then swings
                                # north: the pad starts here, off the road (kept 5 m from its middle), at the ground's height there
PAD_M = 40.0                    # the kicker's foot (clear of the hairpin by 10 m and more), the pad continuing the drive's grade to it
ANG = 21.0                      # the lip's angle over the horizontal
R_KICK = 300.0                  # the kicker's radius (m): at 45 m/s, 6.8 m/s2 on the suspension (Round 68: past about 6 a car's body meets a
                                # ground kicker; the deck here is a held ramp, RampHold keeps a vehicle on it)
DECK_HALF = 4.5                 # the deck's half width (9 m: room to be off line a little at 45 m/s)
CATCH_HALF = 28.0               # the landing ground's half width
ARREST_FROM, ARREST_TO = 0.0, 0.0   # set below from the flights: the gravel bed's stations
VEHICLES = {"Street Classic": (49.0, 14.5), "Drifter Twin": (58.0, 16.0), "Needle 600": (61.0, 18.0)}   # (top m/s, push m/s2): Round 68's model

road = M["roads"][DRIVE]["points"]; rp = [(road[i], road[i + 1]) for i in range(0, len(road), 2)]
A, B = rp[RUN_FROM], rp[RUN_TO]
run = rp[RUN_FROM:RUN_TO + 1]
d = (math.sin(math.radians(BRG)), math.cos(math.radians(BRG))); nl = (-d[1], d[0])
def P(s, lat=0.0): return (B[0] + d[0] * s + nl[0] * lat, B[1] + d[1] * s + nl[1] * lat)

# the run-up: along the drive's points (their heights the ground's; the drive is levelled on it), from rest
run_s = [0.0]
for a, b in zip(run, run[1:]): run_s.append(run_s[-1] + math.dist(a, b))
run_y = [ground(*q) for q in run]
y_B = run_y[-1]; grade_end = (run_y[-1] - run_y[-6]) / (run_s[-1] - run_s[-6])
# the pad: from where the line leaves the hairpin's outer lane, at the ground's height there, on at the drive's grade to the kicker's foot
y_pad = ground(*P(PAD_FROM)); y_foot = y_pad + grade_end * (PAD_M - PAD_FROM)
th0 = math.atan(grade_end); th1 = math.radians(ANG); Lk = R_KICK * (th1 - th0)
deck_s, deck_y = [], []
y = y_foot; s = PAD_M
for k in range(int(Lk) + 1):
    deck_s.append(s); deck_y.append(y)
    th = th0 + min(1.0, (k + 0.5) / Lk) * (th1 - th0); y += math.tan(th); s += 1.0
s_lip = deck_s[-1]; y_lip = deck_y[-1]
tall = max(yy - ground(*P(ss)) for ss, yy in zip(deck_s, deck_y))

# the flights (held ramp: no speed lost on the deck)
def speed_at_lip(top, acc):
    v = 1.0
    for i in range(len(run) - 1):
        L = run_s[i + 1] - run_s[i]; g = (run_y[i + 1] - run_y[i]) / L
        for _ in range(int(round(L))): a = acc * max(0, 1 - v / top) - 9.81 * g / math.sqrt(1 + g * g) - 0.02 * v; v = math.sqrt(max(1, v * v + 2 * a))
    for _ in range(int(PAD_M)): a = acc * max(0, 1 - v / top) - 9.81 * grade_end - 0.02 * v; v = math.sqrt(max(1, v * v + 2 * a))   # (the hairpin's outer lane, then the pad)
    return v
flights = {}
for name, (top, acc) in VEHICLES.items():
    v = speed_at_lip(top, acc); vx, vy = v * math.cos(th1), v * math.sin(th1); t = 0.0; apex = 0.0
    while True:
        t += 0.01; x = vx * t; yy = y_lip + vy * t - 4.905 * t * t; gg = ground(*P(s_lip + x)); apex = max(apex, yy - gg)
        if yy <= gg + 0.3 or t > 25: break
    flights[name] = dict(lip_speed=round(v, 1), air_s=round(t, 2), dist_m=round(x, 1), apex_over_ground_m=round(apex, 1), land_s=round(s_lip + x, 1),
                         land_speed=round(math.hypot(vx, vy - 9.81 * t), 1), load=round(v * v / R_KICK, 1))
land_lo = min(f["land_s"] for f in flights.values()); land_hi = max(f["land_s"] for f in flights.values())
CATCH_FROM, CATCH_TO = land_lo - 90.0, land_hi + 50.0
ARREST_FROM, ARREST_TO = land_hi + 60.0, 745.0      # the gravel arrester: from past the longest landing to just short of Old Mountain Road NW
ARREST_STRENGTH = [0.12, 0.25, 0.4]                 # its three lengths' drag (ShrubBeds: 2.5 / s at 1): about 2.4 g at 80 m/s where it begins, all but
                                                    # stopped by its end (a car crawls on through at a few m/s, never stuck)
TRACK_TO = 760.0                 # on to Old Mountain Road NW

# the ground under the landings and the run-out: smoothed along the line (a 12 m Gaussian, no bump or step a landing at speed meets), level across
gs = list(np.arange(CATCH_FROM - 30.0, TRACK_TO + 0.1, 2.0))
gy = [ground(*P(x)) for x in gs]
for _ in range(2): gy = gsm(gy, 6.0)
halves = [CATCH_HALF if x <= ARREST_TO else 6.0 for x in gs]; halves = gsm(halves, 4.0)
grades = [
    {"id": "the summit ramp's approach pad", "points": flat([P(PAD_FROM - 6.0), P(PAD_M + 6.0)]), "y": [round(y_pad - grade_end * 6.0, 3), round(y_foot + grade_end * 6.0, 3)], "half": DECK_HALF + 2.0, "blend": 6.0,
     "keep_off": flat(rp[RUN_TO - 3:RUN_TO + 12]), "keep_off_m": 5.0},
    {"id": "the summit ramp's landing and run-out", "points": flat([P(x) for x in gs]), "y": [round(v, 3) for v in gy], "half": CATCH_HALF, "blend": 10.0,
     "halves": [round(h, 2) for h in halves], "blends": [10.0] * len(gs)},
]
clear = [{"id": "the summit ramp's flight and landing", "points": flat([P(0.0), P(TRACK_TO)]), "half": 20.0},
         {"id": "the summit ramp's catch", "points": flat([P(CATCH_FROM), P(ARREST_TO + 20.0)]), "half": CATCH_HALF + 4.0}]
breaks = [{"id": "the summit ramp's flight", "points": flat([P(0.0), P(TRACK_TO)]), "half": 40.0}]
poly_l = [P(x, CATCH_HALF) for x in np.arange(CATCH_FROM, ARREST_FROM + 0.1, 10.0)]; poly_r = [P(x, -CATCH_HALF) for x in np.arange(CATCH_FROM, ARREST_FROM + 0.1, 10.0)]
zones = [{"id": "the summit ramp's catch", "poly": flat(poly_l + poly_r[::-1])}]
ramp = {
    "id": "the summit ramp", "dir": [round(d[0], 5), round(d[1], 5)], "origin": [round(B[0], 2), round(B[1], 2)],
    "deck_s": [round(v, 2) for v in deck_s], "deck_y": [round(v, 3) for v in deck_y], "half": DECK_HALF, "angle_deg": ANG, "radius_m": R_KICK,
    "lip": [round(P(s_lip)[0], 2), round(P(s_lip)[1], 2)], "lip_y": round(y_lip, 3), "tallest_m": round(tall, 1),
    "arrester": flat([P(ARREST_FROM + (ARREST_TO - ARREST_FROM) * k / 3.0) for k in range(4)]), "arrester_half": CATCH_HALF - 4.0, "arrester_strength": ARREST_STRENGTH,
    "race_block": flat([P(37.0, -7.0), P(37.0, 12.0)]),   # (just short of the deck's foot, across all of it; the drive's hairpin is 14 m and more to the right here)
    "run": flat(run), "start": [round(A[0], 2), round(A[1], 2)], "start_yaw": round(math.degrees(math.atan2(run[1][0] - A[0], run[1][1] - A[1])) % 360, 1),
    "flights": flights, "track_to": TRACK_TO,
}
out = {"note": "Round 70: the summit ramp on Kennesaw Mountain (Free Roam world piece), from Tools/mapgen/make_summit_ramp.py; merged into the mountain's "
               "pieces by MapBuilder.Mountain.cs, the ramp built by MapBuilder.SummitRamp.cs. Coordinates are map meters (= Unity x, z).",
       "grades": grades, "clear_lines": clear, "break_lines": breaks, "landing_zones": zones, "ramp": ramp}
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"run-up {run_s[-1]:.0f} m on the drive from ({A[0]:.0f}, {A[1]:.0f}) at {(run_y[-1] - run_y[0]) / run_s[-1] * 100:.1f} %, then the pad {PAD_M:.0f} m; "
      f"the kicker {Lk:.0f} m (R {R_KICK:.0f}, {math.degrees(th0):.1f} to {ANG:.0f} deg), the lip at ({P(s_lip)[0]:.0f}, {P(s_lip)[1]:.0f}) {y_lip:.1f} m, "
      f"standing up to {tall:.1f} m over the ground")
for n, f in flights.items(): print(f"  {n:15} lip {f['lip_speed']} m/s ({f['load']} m/s2 on the kicker): {f['air_s']} s, {f['dist_m']} m, {f['apex_over_ground_m']} m over the ground at most, lands at {f['land_s']} m ({f['land_speed']} m/s)")
print(f"catch {CATCH_FROM:.0f} to {ARREST_FROM:.0f} m, arrester {ARREST_FROM:.0f} to {ARREST_TO:.0f} m, the track on to {TRACK_TO:.0f} m; wrote {os.path.relpath(OUT, ROOT)}")

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(18, 6))
    xs = np.arange(-20, TRACK_TO, 2.0); ax.plot(xs, [ground(*P(x)) for x in xs], color="#999", label="ground")
    ax.plot(gs, gy, "g-", label="landing and run-out (graded)"); ax.plot(deck_s, deck_y, "k-", lw=2, label="the deck")
    for n, f in flights.items():
        v = f["lip_speed"]; xx = np.linspace(0, f["dist_m"], 200); ax.plot(s_lip + xx, [y_lip + math.tan(th1) * x - 9.81 * x * x / (2 * v * v * math.cos(th1) ** 2) for x in xx], "--", label=f"{n} {v} m/s")
    ax.axvspan(ARREST_FROM, ARREST_TO, color="orange", alpha=.3); ax.set_aspect("equal"); ax.grid(True, alpha=.3); ax.legend(fontsize=8)
    os.makedirs(os.path.join(ROOT, "local/round70"), exist_ok=True); plt.savefig(os.path.join(ROOT, "local/round70/summit_ramp_profile.png"), dpi=60, bbox_inches="tight")
