"""Round 62 (C): the jolt scan's drives (-exeShots): the map extension's main roads (and the core's near its south-west seam), chained by name and
class into lines, each driven at 28 m/s in its own direction (one-way) or one way (two-way), in pieces of up to 1.4 km; writes local/round62/jolt_shots.json.
Usage: python Tools/mapgen/jolt_shots.py, then ChastainRush.exe -exeShots local/round62/jolt_shots.json <dir> -vehicle drifter (and Tools/mapgen/jolt_table.py)"""
import os; os.makedirs("local/round62", exist_ok=True)
import json, math
m = json.load(open('Data/map.json'))
def P(f): return [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
first = m['extension']['first']['roads']
CLS = {'motorway', 'trunk', 'primary', 'secondary', 'tertiary'}
NAMES = ("Larry McDonald", "Ernest West Barrett", "North Marietta Parkway", "Canton Road Connector", "Phillip Landrum", "Northwest Corridor", "Interstate 75", "Big Shanty", "Barrett Parkway", "Chastain Road", "Chastain Meadows", "Bells Ferry", "Cobb Parkway", "Town Center Drive", "Kennesaw Mountain Drive", "Mall Boulevard", "George Busbee", "Barrett Lakes", "Frey Road", "Stilesboro", "Old 41", "McCollum")
def core_seam(p): return p[0] < 0 or p[1] < 0 or (p[1] < 120 and p[0] < 1400) or p[0] < 120
pick = [i for i, r in enumerate(m['roads']) if not r['bridge'] and (r['class'] in CLS or r['name'].startswith("Kennesaw Mountain Drive")) and any(r['name'].startswith(n) for n in NAMES) and (i >= first or any(core_seam(q) for q in P(r['points'])))]
# chains by name and class (ends within 1 m; bridges between pieces bridged by their decks: a gap up to 120 m along the same direction)
ends = {}
for i in pick:
    q = P(m['roads'][i]['points'])
    for k, e in ((0, q[0]), (1, q[-1])): ends.setdefault((round(e[0] / 2), round(e[1] / 2)), []).append((i, k))
used = set(); chains = []
def nxt(tail, name, cls, dirv):
    best = None
    for i in pick:
        if i in used or m['roads'][i]['name'] != name or m['roads'][i]['class'] != cls: continue
        q = P(m['roads'][i]['points'])
        for rev in (False, True):
            qq = q[::-1] if rev and not m['roads'][i]['oneway'] else (None if rev else q)
            if qq is None: continue
            d = math.dist(tail, qq[0])
            if d > 120: continue
            if d > 2:
                v = ((qq[0][0] - tail[0]) / d, (qq[0][1] - tail[1]) / d)
                if v[0] * dirv[0] + v[1] * dirv[1] < 0.95: continue
            if best is None or d < best[0]: best = (d, i, qq)
    return best
for i in pick:
    if i in used: continue
    r = m['roads'][i]; used.add(i); line = P(r['points'])
    while True:
        a, b = line[-2], line[-1]; L = math.dist(a, b) or 1
        n = nxt(b, r['name'], r['class'], ((b[0] - a[0]) / L, (b[1] - a[1]) / L))
        if not n: break
        used.add(n[1]); line += n[2] if n[0] > 2 else n[2][1:]
    while not r['oneway']:
        a, b = line[1], line[0]; L = math.dist(a, b) or 1
        n = nxt(b, r['name'], r['class'], ((b[0] - a[0]) / L, (b[1] - a[1]) / L))
        if not n: break
        used.add(n[1]); line = (n[2] if n[0] > 2 else n[2][1:])[::-1] + line
    L = sum(math.dist(a, b) for a, b in zip(line, line[1:]))
    if L > 150: chains.append((r['name'], r['class'], line, L))
def resample(p, step):
    c = [0.0]
    for a, b in zip(p, p[1:]): c.append(c[-1] + math.dist(a, b))
    out = []; s = 0.0; i = 0
    while s <= c[-1]:
        while i + 2 < len(c) and c[i + 1] < s: i += 1
        t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9); out.append((round(p[i][0] + t * (p[i + 1][0] - p[i][0]), 2), round(p[i][1] + t * (p[i + 1][1] - p[i][1]), 2))); s += step
    return out
shots = []; total = 0
for name, cls, line, L in sorted(chains, key=lambda c: -c[3]):
    q = resample(line, 2.0)
    for k in range(0, len(q) - 10, 700):   # pieces of 1.4 km (overlapping 0)
        piece = q[k:k + 720]
        if len(piece) < 40: continue
        yaw = math.degrees(math.atan2(piece[1][0] - piece[0][0], piece[1][1] - piece[0][1])) % 360
        nm = ''.join(ch for ch in name if ch.isalnum())[:22] + f"_{len(shots):03d}"
        shots.append({"name": nm, "x": piece[0][0], "z": piece[0][1], "yaw": round(yaw, 1), "drive_speed": 28.0, "drive_seconds": round(len(piece) * 2.0 / 28.0 + 3.0, 1), "trace": True,
                      "path": [v for pt in piece for v in pt], "road": name})
        total += len(piece) * 2.0
json.dump({"shots": shots}, open('local/round62/jolt_shots.json', 'w'), indent=0)
print(len(chains), "chains,", len(shots), "drives,", round(total / 1000, 1), "km,", round(total / 28 / 60, 1), "min of driving")
from collections import Counter
print(Counter(s['road'] for s in shots).most_common(40))
