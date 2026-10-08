"""Reports road stretches steeper than a threshold at TRUE scale (vertical_scale ignored) and whether they cross water.
Reads Data/map.json + Data/heightmap_1m.tif (whatever the last mapgen run produced). Usage: python steep_roads.py [pct] [window_m]"""
import json, os, sys
import numpy as np, rasterio
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
thr = float(sys.argv[1]) if len(sys.argv) > 1 else 15.0
win = int(sys.argv[2]) if len(sys.argv) > 2 else 20
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
with rasterio.open(os.path.join(ROOT, "Data/heightmap_1m.tif")) as s:
    dem = s.read(1)[::-1]  # row 0 = south; 1 m cells
k = float(m.get("vertical_scale", 1))
def h(x, z):
    gx = np.clip(np.asarray(x) - 0.5, 0, dem.shape[1] - 1.001); gz = np.clip(np.asarray(z) - 0.5, 0, dem.shape[0] - 1.001)
    i, j = gz.astype(int), gx.astype(int); fz, fx = gz - i, gx - j
    return dem[i, j]*(1-fx)*(1-fz) + dem[i, j+1]*fx*(1-fz) + dem[i+1, j]*(1-fx)*fz + dem[i+1, j+1]*fx*fz
def segs(flat): return [(flat[i], flat[i+1], flat[i+2], flat[i+3]) for i in range(0, len(flat)-3, 2)]
def cross(a, b, c, d):
    def o(p, q, r): return (q[0]-p[0])*(r[1]-p[1]) - (q[1]-p[1])*(r[0]-p[0])
    return o(a,b,c)*o(a,b,d) < 0 and o(c,d,a)*o(c,d,b) < 0
def in_poly(p, poly):
    ins = False; j = len(poly)-1
    for i in range(len(poly)):
        if (poly[i][1] > p[1]) != (poly[j][1] > p[1]) and p[0] < (poly[j][0]-poly[i][0])*(p[1]-poly[i][1])/(poly[j][1]-poly[i][1])+poly[i][0]: ins = not ins
        j = i
    return ins
streams = [(s["name"] or s["kind"], segs(s["points"])) for s in m["streams"]]
ponds = [(w["name"], [(w["outline"][i], w["outline"][i+1]) for i in range(0, len(w["outline"]), 2)]) for w in m["water"]]
def water_hits(pts):
    hits = set()
    for a in range(len(pts)-1):
        for nm, ss in streams:
            if any(cross(pts[a], pts[a+1], (s[0], s[1]), (s[2], s[3])) for s in ss): hits.add("stream " + nm)
        for nm, poly in ponds:
            if in_poly(pts[a], poly): hits.add("pond " + (nm or "unnamed"))
    return hits
rows = []; tot = 0.0
for r in m["roads"]:
    p = np.array(r["points"]).reshape(-1, 2)
    d = np.hypot(*np.diff(p, axis=0).T); cum = np.concatenate([[0], np.cumsum(d)])
    if cum[-1] < win: continue
    s = np.arange(0, cum[-1], 1.0); x = np.interp(s, cum, p[:, 0]); z = np.interp(s, cum, p[:, 1]); e = h(x, z)
    g = (e[win:] - e[:-win]) / win * 100
    over = np.abs(g) > thr
    i = 0
    while i < len(g):
        if over[i]:
            j = i
            while j + 1 < len(g) and over[j+1]: j += 1
            a, b = i, j + win
            sub = list(zip(x[a:b+1], z[a:b+1]))
            rows.append((r["name"] or "(unnamed)", r["class"], b-a, float(np.abs(g[i:j+1]).max()), (x[a], z[a]), sorted(water_hits(sub))))
            i = j + 1
        else: i += 1
rows.sort(key=lambda t: -t[3])
print(f"threshold {thr}% over {win} m windows, true scale (map built at x{k:g}); {len(rows)} stretches")
for n, c, L, mx, at, wh in rows:
    print(f"{n} [{c}] {L} m long, max {mx:.1f}% (true), at x={at[0]:.0f} z={at[1]:.0f}; crosses water: {', '.join(wh) or 'no'}")
