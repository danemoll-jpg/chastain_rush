"""Round 80 (D1 / E): drive lines for the check over every Northwest Corridor Express Lanes deck (map.json bridges, OSM pieces that meet end to end joined
into one deck), each with 120 m of the lanes before it and 60 m after, and every Express gate's ramp; written to local/round80/r80_express.json for
ChastainRush.exe -exeRound80 ... -r80Only ddecks (both ways each) and express. Report only (map meters).

Usage: python Tools/mapgen/r80_express_lines.py
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
NAME = "Northwest Corridor Express Lanes"
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f) - 1, 2)]

pieces = [pts(r["points"]) for r in m["roads"] if r.get("name") == NAME] + [pts(b["points"]) for b in m["bridges"] if b.get("name") == NAME]
decks = [pts(b["points"]) for b in m["bridges"] if b.get("name") == NAME]

def chain(items):
    left = [list(p) for p in items]; out = []
    while left:
        c = left.pop(0); grew = True
        while grew:
            grew = False
            for i, p in enumerate(left):
                if math.dist(p[0], c[-1]) < 1.5: c += p[1:]
                elif math.dist(p[-1], c[-1]) < 1.5: c += p[::-1][1:]
                elif math.dist(p[-1], c[0]) < 1.5: c = p[:-1] + c
                elif math.dist(p[0], c[0]) < 1.5: c = p[::-1][:-1] + c
                else: continue
                left.pop(i); grew = True; break
        out.append(c)
    return out

lanes = chain(pieces)   # the lanes' long lines (I-75's and I-575's, and the connector if named)
def cum(p):
    c = [0.0]
    for a, b in zip(p, p[1:]): c.append(c[-1] + math.dist(a, b))
    return c
def nearest(line, q):
    best = (1e9, 0)
    for i, v in enumerate(line):
        d = math.dist(v, q)
        if d < best[0]: best = (d, i)
    return best
def resample(p, step=4.0):
    c = cum(p); out = []; s = 0.0; k = 1
    while s <= c[-1]:
        while k < len(c) - 1 and c[k] < s: k += 1
        t = (s - c[k - 1]) / max(1e-6, c[k] - c[k - 1]); a, b = p[k - 1], p[k]
        out.append((round(a[0] + (b[0] - a[0]) * t, 2), round(a[1] + (b[1] - a[1]) * t, 2))); s += step
    return out

out = []
for d in chain(decks):
    L = cum(d)[-1]
    host = min(lanes, key=lambda l: nearest(l, d[0])[0] + nearest(l, d[-1])[0])
    c = cum(host); i0 = nearest(host, d[0])[1]; i1 = nearest(host, d[-1])[1]
    if i0 > i1: i0, i1 = i1, i0
    s0, s1 = max(0.0, c[i0] - 120.0), min(c[-1], c[i1] + 60.0)
    seg = [host[i] for i in range(len(host)) if s0 <= c[i] <= s1]
    if len(seg) < 3: continue
    out.append({"name": f"deck at ({d[0][0]:.0f}, {d[0][1]:.0f}), {L:.0f} m", "length": round(L, 1), "line": [v for q in resample(seg) for v in q]})
    print(out[-1]["name"], len(out[-1]["line"]) // 2, "points")
os.makedirs(os.path.join(ROOT, "local/round80"), exist_ok=True)
json.dump({"decks": out}, open(os.path.join(ROOT, "local/round80/r80_express.json"), "w"), indent=0)
print("wrote local/round80/r80_express.json:", len(out), "decks")
