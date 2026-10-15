"""Round 31 check lists for the exe (-exeShots): Dan's three report spots, one picture per kind of dressed building (from
Assets/Generated/business_views.tsv, written by Build Map), and a drive flat out over each I-575 / Chastain Road ramp (on
the carriageway before it, the ramp, and on along the road it joins). Writes local/round31/shots_pics.json and
local/round31/shots_ramps.json.  Usage: python Tools/mapgen/round31_shots.py"""
import json, math, os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
m = json.load(open(os.path.join(ROOT, "Data/map.json")))
by_osm = {}
for r in m["roads"]: by_osm.setdefault(r["osm_id"], []).append(r)
def P(osm): r = by_osm[osm][0]; p = r["points"]; return [(p[k], p[k + 1]) for k in range(0, len(p), 2)]
def L(i): p = m["roads"][i]["points"]; return [(p[k], p[k + 1]) for k in range(0, len(p), 2)]
def length(pts): return sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1))
def tail(pts, d):   # the last d metres
    out = [pts[-1]]; acc = 0
    for i in range(len(pts) - 1, 0, -1):
        s = math.dist(pts[i], pts[i - 1])
        if acc + s >= d: f = (d - acc) / s; out.append((pts[i][0] + (pts[i - 1][0] - pts[i][0]) * f, pts[i][1] + (pts[i - 1][1] - pts[i][1]) * f)); break
        acc += s; out.append(pts[i - 1])
    return out[::-1]
def head(pts, d): return tail(pts[::-1], d)[::-1]
def upto(pts, q):   # pts up to the vertex nearest q
    k = min(range(len(pts)), key=lambda i: math.dist(pts[i], q)); return pts[:k + 1]
def after(pts, q):
    k = min(range(len(pts)), key=lambda i: math.dist(pts[i], q)); return pts[k:]
def join(*parts):
    out = []
    for p in parts:
        for q in p:
            if not out or math.dist(out[-1], q) > 0.5: out.append(q)
    return out
def yaw(p): return math.degrees(math.atan2(p[1][0] - p[0][0], p[1][1] - p[0][1]))
def drive(name, pts, speed=45):
    secs = length(pts) / 14 + 10
    return {"name": name, "x": pts[0][0], "z": pts[0][1], "yaw": yaw(pts), "drive_speed": speed, "drive_seconds": round(secs), "path": [round(v, 2) for q in pts for v in q], "shot_at": 6}
ramps = [
    drive("ramp_nb_exit", join(tail(upto(L(237), (469, 381)), 220), P(9119806), P(442141352), P(196252347), after(L(208), (532, 899)), L(209))),
    drive("ramp_nb_entrance", join(L(129), P(196252346), P(442141351), P(290280696), head(L(239), 150))),
    drive("ramp_sb_exit", join(tail(L(242), 200), P(442141931), P(442141932), P(442141933), P(196252348), head(L(259), 90))),
    drive("ramp_sb_entrance", join(tail(L(390), 120), P(196252345), P(9116461), head(after(L(194), (437, 399)), 200))),
]
os.makedirs(os.path.join(ROOT, "local/round31"), exist_ok=True)
json.dump({"shots": ramps}, open(os.path.join(ROOT, "local/round31/shots_ramps.json"), "w"))
pics = [
    {"name": "bug002_dan_spot", "x": 3544.56, "z": 495.74, "yaw": 266.45},
    {"name": "bug002_far", "x": 3544.56, "z": 495.74, "yaw": 266.45, "view": "far"},
    {"name": "bug003_dan_spot", "x": 1524.43, "z": 561.77, "yaw": 48.36},
    {"name": "bug004_dan_spot", "x": 319.08, "z": 984.29, "yaw": 185.2, "view": "far"},
]
tsv = os.path.join(ROOT, "Assets/Generated/business_views.tsv")
if os.path.exists(tsv):
    for line in open(tsv).read().splitlines()[1:]:
        k, idx, x, z, yw, cx, cz = line.split("\t")
        pics.append({"name": f"kind_{k.lower()}_{idx}", "x": float(x), "z": float(z), "yaw": float(yw), "view": "far"})
json.dump({"shots": pics}, open(os.path.join(ROOT, "local/round31/shots_pics.json"), "w"))
print(len(ramps), "ramp drives,", len(pics), "pictures;", ", ".join(f"{d['name']} {length(list(zip(d['path'][0::2], d['path'][1::2]))):.0f} m" for d in ramps))
