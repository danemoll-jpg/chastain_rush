"""Round 93 (A: the quarry bump after The Big One "and any like it on the lap"; C1: Wings & Wheels "check this course as well for unsmooth roads"): the
lines the map build smooths along (MapBuilder.Round93.cs R93SmoothLaps): each race's lap every 2 m, the speed a racer has at each point (from the bend
of the line: sqrt(lateral g x radius), at most the course's top, the most within 40 m either way), and the stretches kept as they are:
  Viking Quarry: each quarry jump from its run-up's head to 40 m past its landing deck (Data/tracks/quarry_jumps.json land_s), within 25 m of
                 its line: the catch beyond is smoothed (each catch's end met the bench with a lip that threw a car 2 to 3 s: Dan's bump after The Big One)
  Viking Quarry also: spike cells within 35 m of the lap cut down (the "jagged green wall" at the bend after The Big One)
  Wings & Wheels: within 12 m of a bridge deck the lap runs on or under (map.json bridges) and of the railroad (Data/tracks/railroad.json)
Writes Data/tracks/round93_smooth.json. Re-run after a route script or mapgen, then a full Build Map (a planning change).
Usage: python Tools/mapgen/make_round93_smooth.py"""
import json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/tracks/round93_smooth.json")


def load(name): return json.load(open(os.path.join(ROOT, name), encoding="utf-8"))
def resample(p, step):
    p = np.asarray(p, float); c = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(p, axis=0), axis=1))])
    s = np.arange(0, c[-1], step); return np.column_stack([np.interp(s, c, p[:, 0]), np.interp(s, c, p[:, 1])])


def speeds(p, top, lat_g=9.0):
    n = len(p); v = np.zeros(n)
    for i in range(n):
        a, b, c = p[(i - 6) % n], p[i], p[(i + 6) % n]
        h1 = math.atan2(b[0] - a[0], b[1] - a[1]); h2 = math.atan2(c[0] - b[0], c[1] - b[1])
        d = abs((h2 - h1 + math.pi) % (2 * math.pi) - math.pi); arc = np.linalg.norm(b - a) + np.linalg.norm(c - b)
        r = arc / max(d, 1e-6); v[i] = min(top, math.sqrt(lat_g * r))
    w = 20   # (40 m either way at 2 m)
    return [round(float(max(v[(i + k) % n] for k in range(-w, w + 1))), 1) for i in range(n)]


def ranges(mask):
    out, i, n = [], 0, len(mask)
    while i < n:
        if mask[i]:
            j = i
            while j + 1 < n and mask[j + 1]: j += 1
            out += [i, j]; i = j + 1
        else: i += 1
    return out


lines = []
# ---- Viking Quarry
r = load("Data/routes/viking_quarry.json"); lp = np.array(r["loop"]).reshape(-1, 2); p = resample(lp, 2.0)
keep = np.zeros(len(p), bool)
for j in load("Data/tracks/quarry_jumps.json")["jumps"]:
    o, d = np.array(j["origin"]), np.array(j["dir"]); nl = np.array([-d[1], d[0]])
    al = (p - o) @ d; lat = (p - o) @ nl
    keep |= (np.abs(lat) < 25.0) & (al > -10.0) & (al < j["land_s"][-1] + 40.0)
lines.append({"id": "Viking Quarry", "closed": True, "points": [round(float(v), 2) for v in p.ravel()], "speeds": speeds(p, 40.0), "keep": ranges(keep),
              "half": 8.0, "blend": 6.0, "sigma": 5.0, "crest_g": 0.8, "sag_g": 2.5, "max_change": 3.0, "despike_reach": 35.0})
print(f"Viking Quarry: {len(p)} points, {keep.sum()} kept ({len(ranges(keep)) // 2} stretches)")
# ---- Wings & Wheels (the car's lap)
r = load("Data/routes/wings_and_wheels.json"); lp = np.array(r["loop"]).reshape(-1, 2); p = resample(lp, 2.0)
keep = np.zeros(len(p), bool)
m = load("Data/map.json")
for b in m["bridges"]:
    q = np.array(b["points"]).reshape(-1, 2)
    if q[:, 0].max() < p[:, 0].min() - 50 or q[:, 0].min() > p[:, 0].max() + 50 or q[:, 1].max() < p[:, 1].min() - 50 or q[:, 1].min() > p[:, 1].max() + 50: continue
    for k in range(len(q)):
        keep |= np.hypot(p[:, 0] - q[k, 0], p[:, 1] - q[k, 1]) < b["width"] / 2 + 12.0
rail = load("Data/tracks/railroad.json")
for ln in rail.get("lines", []):
    q = np.array(ln["points"]).reshape(-1, 2)
    if q[:, 0].max() < p[:, 0].min() - 50 or q[:, 0].min() > p[:, 0].max() + 50: continue
    for k in range(len(q)):
        keep |= np.hypot(p[:, 0] - q[k, 0], p[:, 1] - q[k, 1]) < 12.0
lines.append({"id": "Wings & Wheels", "closed": True, "points": [round(float(v), 2) for v in p.ravel()], "speeds": speeds(p, 52.0), "keep": ranges(keep),
              "half": 5.5, "blend": 5.0, "sigma": 4.0, "crest_g": 0.8, "sag_g": 2.5, "max_change": 1.2})
print(f"Wings & Wheels: {len(p)} points, {keep.sum()} kept ({len(ranges(keep)) // 2} stretches)")
json.dump({"source": "Tools/mapgen/make_round93_smooth.py (Round 93 A, C1)", "lines": lines}, open(OUT, "w", encoding="utf-8"))
print("wrote", OUT)
