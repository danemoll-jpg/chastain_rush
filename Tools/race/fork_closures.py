"""Round 74 D (Dan, 2026-10-27, Kennesaw's BUG-002: "there needs to be a barrier for the right road. It is not obvious you don't go that way"): a race
barrier on every road that leaves a course's line and carries on (Tools/race/fork_audit.py's forks), each direction it is raced. Each barrier is walked
on along its road away from the line to the first place where the whole barrier (the road's width plus 3 m each side, as RaceBarriers builds it) is
CLEAR m or more off the line (every direction that uses the same closures list), as make_mountain_route.py's closure() does; a fork where that never
happens within WALK m is reported and left. The closures are added to the route files ("fork": true, ids F1 ...); re-running this replaces them.
Re-run after any route script (make_route.py, make_*_route.py), then a quick build (the routes are embedded).
Usage: python Tools/race/fork_closures.py [--dry]"""
import glob, json, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
ON, OFF, MIN_ON, REACH, CLEAR, WALK = 6.0, 14.0, 25.0, 90.0, 8.0, 70.0
M = json.load(open("Data/map.json", encoding="utf-8"))
def pts(f): return [(f[i], f[i + 1]) for i in range(0, len(f) - 1, 2)]
def dense(p, step=2.0):
    out = []
    for a, b in zip(p, p[1:]):
        n = max(1, int(math.dist(a, b) / step)); out += [(a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n) for k in range(n)]
    return out + p[-1:]
class Grid:
    def __init__(s, p, c=20.0): s.c = c; s.g = {}; [s.g.setdefault((int(x // c), int(z // c)), []).append((x, z)) for x, z in p]
    def d(s, q):
        i, j = int(q[0] // s.c), int(q[1] // s.c); best = 1e9
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                for r in s.g.get((i + di, j + dj), ()): best = min(best, math.dist(q, r))
        return best
roads = [(r, dense(pts(r["points"]), 1.0)) for r in M["roads"] if r.get("class") not in ("footway", "path", "track", "cycleway", "steps", "pedestrian")]
def forks(line_grid, sc_grids, bars):
    out = []
    for r, rp in roads:
        dd = [line_grid.d(q) for q in rp]
        if min(dd) > ON: continue
        for i in range(1, len(rp)):
            for a, b in ((i - 1, i), (i, i - 1)):
                if not (dd[a] <= ON and dd[b] > ON): continue
                step = 1 if b > a else -1; k = b; run = 0.0; far = 0.0
                while 0 <= k < len(rp) and dd[k] > ON:
                    if 0 <= k - step < len(rp): run += math.dist(rp[k], rp[k - step])
                    far = max(far, dd[k]); k += step
                if run < MIN_ON or far < OFF: continue
                if any(sg.d(rp[min(len(rp) - 1, max(0, b + step * 5))]) < 6 for sg in sc_grids): continue
                fork = rp[a]
                if min((math.dist(fork, q) for q in bars if min(math.dist(q, x) for x in rp[::4]) < 8), default=1e9) <= REACH: continue
                out.append((r, rp, a, step))
    return out
def place(r, rp, a, step, grids):
    h = r["width"] / 2 + 3.0; k = a; walked = 0.0
    while 0 <= k + step < len(rp) and walked <= WALK:
        k += step; walked += math.dist(rp[k], rp[k - step])
        k0, k1 = max(0, k - 2), min(len(rp) - 1, k + 2); t = (rp[k1][0] - rp[k0][0], rp[k1][1] - rp[k0][1]); n = math.hypot(*t) or 1; t = (t[0] / n, t[1] / n); ac = (-t[1], t[0])
        gap = min(g.d((rp[k][0] + ac[0] * h * j / 4, rp[k][1] + ac[1] * h * j / 4)) for g in grids for j in range(-4, 5))
        if gap >= CLEAR: return rp[k], t, walked, gap
    return None
dry = "--dry" in sys.argv; total = left = 0
for f in sorted(glob.glob("Data/routes/*.json")):
    R = json.load(open(f, encoding="utf-8"))
    if R.get("air") or "underground" in f: continue
    lists = {"closures": [R.get("loop")]}
    rev = "reverse" in (R.get("directions") or ["forward", "reverse"])
    if R.get("loop_rev"):
        if rev: lists["closures_rev"] = [R["loop_rev"]]
    # (a route without its own reverse lap races the loop backward with the same closures: its forks are the same roads)
    changed = False
    for key, loops in lists.items():
        cl = [c for c in (R.get(key) or []) if not c.get("fork")]
        if len(cl) != len(R.get(key) or []): changed = True
        line = dense(pts(loops[0]) + pts(R.get("lead_in") or [])); G = Grid(line)
        scs = [Grid(dense(pts(sc.get("path") or []))) for sc in R.get("official_shortcuts") or [] if sc.get("path")]
        bars = [(c["x"], c["z"]) for c in cl]
        n = 0
        for r, rp, a, step in forks(G, scs, bars):
            p = place(r, rp, a, step, [G])
            name = r.get("name") or f"(unnamed {r.get('class', '')})"
            if p is None: left += 1; print(f"  {os.path.basename(f)} {key}: {name} at ({rp[a][0]:.0f}, {rp[a][1]:.0f}): no place {CLEAR:.0f} m clear within {WALK:.0f} m (left open)"); continue
            q, t, walked, gap = p
            if any(math.dist(q, b) < 12 for b in bars): continue
            n += 1; total += 1; bars.append(q)
            cl.append({"road": name, "osm_id": r.get("osm_id"), "x": round(q[0], 2), "z": round(q[1], 2), "dx": round(t[0], 4), "dz": round(t[1], 4), "width_m": r["width"],
                       "junction_s": 0.0, "contact": [round(rp[a][0], 1), round(rp[a][1], 1)], "id": f"F{n}", "label": f"{name}: a fork off the line (Round 74)", "fork": True})
            print(f"  {os.path.basename(f)} {key} F{n}: {name}, the fork at ({rp[a][0]:.0f}, {rp[a][1]:.0f}), the barrier {walked:.0f} m along it at ({q[0]:.0f}, {q[1]:.0f}), {gap:.1f} m off the line")
        if n or changed: R[key] = cl; changed = True
    if changed and not dry: json.dump(R, open(f, "w", encoding="utf-8"), indent=1)
print(f"{total} fork barrier(s) added, {left} fork(s) with no clear place (left open){' (dry run)' if dry else ''}")
