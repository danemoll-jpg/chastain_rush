"""Round 74 D (Dan, 2026-10-27, Kennesaw's BUG-002: "there needs to be a barrier for the right road. It is not obvious you don't go that way"): every
course, each direction it is raced: the forks where the race line leaves a road (or joins one) while the road carries on, a way that looks like the course
but is not. Report only: prints each fork with no race barrier (that direction's closures) on the road within REACH m of it.
A road 'carries on' when it runs on MIN_ON m or more past the place the line leaves it, more than OFF m from the line. Footways, tracks and driveways are
not roads here (map.json roads only: streets and service roads); a road shorter than MIN_ON past the fork (a stub) is listed apart.
Usage: python Tools/race/fork_audit.py [route.json ...]"""
import glob, json, math, sys
ON, OFF, MIN_ON, REACH = 6.0, 14.0, 25.0, 90.0
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
roads = [(r, dense(pts(r["points"]))) for r in M["roads"] if r.get("class") not in ("footway", "path", "track", "cycleway", "steps", "pedestrian")]
files = sys.argv[1:] or sorted(glob.glob("Data/routes/*.json"))
total = 0
for f in files:
    R = json.load(open(f, encoding="utf-8"))
    if R.get("air") or "underground" in f: continue
    dirs = [("forward", R.get("loop"), R.get("closures"))]
    if "reverse" in (R.get("directions") or ["forward", "reverse"]):
        dirs.append(("reverse", R.get("loop_rev") or R.get("loop"), R.get("closures_rev") if R.get("closures_rev") is not None else R.get("closures")))
    for dname, loop, closures in dirs:
        if not loop: continue
        line = dense(pts(loop) + pts(R.get("lead_in") or [])); G = Grid(line)
        bars = [(c["x"], c["z"]) for c in closures or []]
        for sc in R.get("official_shortcuts") or []: line_sc = dense(pts(sc.get("path") or [])); [bars.append(q) for q in []]
        scs = [Grid(dense(pts(sc.get("path") or []))) for sc in R.get("official_shortcuts") or [] if sc.get("path")]
        for r, rp in roads:
            dd = [G.d(q) for q in rp]
            if min(dd) > ON: continue
            # each run of the road on the line; at each end of a run the road leaves: how far it carries on off the line
            for i in range(1, len(rp)):
                for a, b in ((i - 1, i), (i, i - 1)):
                    if dd[a] <= ON and dd[b] > ON:
                        step = 1 if b > a else -1; k = b; run = 0.0; far = 0.0
                        while 0 <= k < len(rp) and dd[k] > ON:
                            run += math.dist(rp[k], rp[k - step]) if 0 <= k - step < len(rp) else 0; far = max(far, dd[k]); k += step
                        if run < MIN_ON or far < OFF: continue
                        fork = rp[a]
                        if any(sg.d(rp[min(len(rp) - 1, max(0, b + step * 5))]) < 6 for sg in scs): continue   # an official shortcut goes that way
                        bar = min((math.dist(fork, q) for q in bars if min(math.dist(q, x) for x in rp) < 8), default=1e9)
                        if bar <= REACH: continue
                        total += 1
                        print(f"{f.split('/')[-1].split(chr(92))[-1]:28} {dname:7} {r.get('name') or '(unnamed ' + r.get('class', '') + ')':34} fork at ({fork[0]:.0f}, {fork[1]:.0f}): carries on {run:.0f} m off the line, nearest barrier {('none' if bar > 1e8 else f'{bar:.0f} m')}")
print(f"{total} fork(s) without a barrier within {REACH:.0f} m")
