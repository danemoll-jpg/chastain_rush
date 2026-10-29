"""Round 81: drive lines for the check (ChastainRush.exe -exeRound81 ... -r81Only junction,...): every road through the I-75 / I-575 junction (the general
lanes their own way, the Express lanes both ways), and the Chastain Glen forward lap's Rooftop leap with Dan's BUG-003 run-ins. Written to
local/round81/r81_lines.json. Report only (map meters).

Usage: python Tools/mapgen/r81_lines.py
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f) - 1, 2)]
by = {}
for r in m["roads"]: by[r["osm_id"]] = pts(r["points"])
for b in m["bridges"]: by[b["osm_id"]] = pts(b["points"])

def join(ids):
    line = list(by[ids[0]])
    for i in ids[1:]:
        p = by[i]
        if math.dist(p[0], line[-1]) < 3: line += p[1:]
        elif math.dist(p[-1], line[-1]) < 3: line += p[::-1][1:]
        elif len(line) == len(by[ids[0]]) and math.dist(p[0], line[0]) < 3: line = line[::-1] + p[1:]
        elif len(line) == len(by[ids[0]]) and math.dist(p[-1], line[0]) < 3: line = line[::-1] + p[::-1][1:]
        else: raise SystemExit(f"{i} does not join")
    return line
def cum(p):
    c = [0.0]
    for a, b in zip(p, p[1:]): c.append(c[-1] + math.dist(a, b))
    return c
def resample(p, step=4.0):
    c = cum(p); out = []; s = 0.0; k = 1
    while s <= c[-1]:
        while k < len(c) - 1 and c[k] < s: k += 1
        t = (s - c[k - 1]) / max(1e-6, c[k] - c[k - 1]); a, b = p[k - 1], p[k]
        out.append((round(a[0] + (b[0] - a[0]) * t, 2), round(a[1] + (b[1] - a[1]) * t, 2))); s += step
    return out
def clip(p, near, before, after):
    """the stretch from 'before' m ahead of the point nearest 'near' to 'after' m past it"""
    r = resample(p, 2.0); c = cum(r); i = min(range(len(r)), key=lambda k: math.dist(r[k], near))
    return [q for q, s in zip(r, c) if c[i] - before <= s <= c[i] + after]

J = (500, -3250)
lines = [
    ("I-575 Express flyover southbound", clip(join([367181844, 624557373, 451950526, 451950527, 437647056]), J, 450, 700), True),
    ("I-575 southbound general lanes", clip(join([586430560, 163463460, 163463459, 536793083, 536793084]), J, 400, 700), False),
    ("I-575 northbound general lanes", clip(join([196231567]), (700, -3300), 500, 500), False),
    ("I-75 southbound", clip(join([119686448]), (700, -3420), 500, 600), False),
    ("I-75 northbound", clip(join([570368037]), (700, -3420), 600, 500), False),
    ("I-75 Express lanes", clip(join([536789456, 437647056]), (700, -3600), 600, 500), True),
]
out = {"lines": [{"name": n, "both": both, "line": [v for q in resample(l, 4.0) for v in q]} for n, l, both in lines]}
# F: the Rooftop leap (forward), from Dan's BUG-003 place (x 2853.66 z 1131.75, heading 248.65) into the shortcut's path at three points
route = json.load(open(os.path.join(ROOT, "Data/routes/chastain_glen_loop.json"), encoding="utf-8"))
sc = [s for s in route["official_shortcuts"] if s["id"] == "sc6_over_house"][0]; path = pts(sc["path"]); pc = cum(path)
dan = (2853.66, 1131.75); h = math.radians(248.65); back = (dan[0] - 30 * math.sin(h), dan[1] - 30 * math.cos(h))
for at in (0.0, 8.0, 16.0):
    k = min(range(len(path)), key=lambda i: abs(pc[i] - at))
    l = [back, dan] + path[k:]
    out["lines"].append({"name": f"Rooftop leap from Dan's place into its path at {at:0} m", "both": False, "line": [v for q in resample(l, 3.0) for v in q]})
# and lined up on the kicker's own axis from the lane (as Dan tries to): off the lane onto the axis 25 m and 15 m before the kicker's foot
K = (2814.65, 1097.57); kk = min(range(len(path)), key=lambda i: math.dist(path[i], K)); a0 = path[max(0, kk - 6)]
ax = ((K[0] - a0[0]) / math.dist(K, a0), (K[1] - a0[1]) / math.dist(K, a0))
for back_m in (25.0, 15.0):
    on = (K[0] - ax[0] * (back_m + 8), K[1] - ax[1] * (back_m + 8))
    l = [back, dan, on] + path[kk - 2:]
    out["lines"].append({"name": f"Rooftop leap lined up on the kicker's axis {back_m:0} m out", "both": False, "line": [v for q in resample(l, 3.0) for v in q]})
# and straight on from Dan's place at his heading (he says "if you try to line it up from this angle"): into the kicker's side
ahead = [(dan[0] + d * math.sin(h), dan[1] + d * math.cos(h)) for d in (10.0, 20.0, 30.0)]
l = [back, dan] + ahead + path[kk + 4:]
out["lines"].append({"name": "Rooftop leap straight on from Dan's heading", "both": False, "line": [v for q in resample(l, 3.0) for v in q]})
os.makedirs(os.path.join(ROOT, "local/round81"), exist_ok=True)
json.dump(out, open(os.path.join(ROOT, "local/round81/r81_lines.json"), "w"))
for d in out["lines"]: print(d["name"], len(d["line"]) // 2, "points")
