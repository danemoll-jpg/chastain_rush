"""Round 73 A1: every route's race barriers against the lines the race drives (report only).
A barrier is the closure's segment across its road (width_m / 2 + 3 m verge each side, as RaceBarriers builds it).
Prints each closure within CLEAR m of the lap (each direction's own), the lead-in or an official shortcut's line."""
import glob, json, math, sys
CLEAR = 4.0
def seg_d(p, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0 if L2 < 1e-9 else max(0, min(1, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / L2))
    return math.dist(p, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def line_pts(flat):
    pts = [(flat[i], flat[i + 1]) for i in range(0, len(flat) - 1, 2)]
    out = []
    for p, q in zip(pts, pts[1:]):
        n = max(1, int(math.dist(p, q) / 0.5))
        out += [(p[0] + (q[0] - p[0]) * k / n, p[1] + (q[1] - p[1]) * k / n) for k in range(n)]
    return out + pts[-1:]
bad = 0
for f in sorted(glob.glob("Data/routes/*.json")):
    r = json.load(open(f))
    lines = {"loop": r.get("loop") or [], "lead_in": r.get("lead_in") or []}
    if r.get("loop_rev"): lines["loop_rev"] = r["loop_rev"]
    for sc in r.get("official_shortcuts") or []: lines["shortcut " + sc.get("direction", "forward") + " " + sc.get("id", "?")] = sc.get("path") or []
    lines = {k: line_pts(v) for k, v in lines.items() if len(v) >= 4}
    for key in ("closures", "closures_rev"):
        for c in r.get(key) or []:
            al = (c["dx"], c["dz"]); n = math.hypot(*al) or 1; al = (al[0] / n, al[1] / n); ac = (-al[1], al[0])
            h = c["width_m"] / 2 + 3.0
            a = (c["x"] - ac[0] * h, c["z"] - ac[1] * h); b = (c["x"] + ac[0] * h, c["z"] + ac[1] * h)
            for name, pts in lines.items():
                if key == "closures" and name == "loop_rev": continue
                if key == "closures_rev" and name in ("loop", "lead_in"): continue
                if name.startswith("shortcut "):
                    d_ = name.split()[1]
                    if d_ != "both" and d_ != ("reverse" if key == "closures_rev" else "forward") and (key == "closures_rev" or r.get("closures_rev")): continue
                d = min(seg_d(p, a, b) for p in pts)
                if d < CLEAR:
                    bad += 1
                    print(f"{f}: {key} {c.get('id')} '{c.get('label')}' at ({c['x']:.1f}, {c['z']:.1f}) is {d:.1f} m from {name}")
print(f"{bad} barrier(s) on a race's line")
