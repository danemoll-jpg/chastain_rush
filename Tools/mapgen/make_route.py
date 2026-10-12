"""Builds a race route from Data/map.json as data: Data/routes/chastain_glen_loop.json. The route is the road pieces it
follows (by OSM id), resolved to a centreline, with junctions, a shortcut analysis and checkpoints. Re-runnable after any
mapgen run. Hand choices live in the constants below, not in the output (except "direction", which is kept across runs).
Usage: python Tools/mapgen/make_route.py"""
import json, math, os, heapq
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/routes/chastain_glen_loop.json")

# The route as OSM ways: (osm_id, reverse, from_m, to_m); to_m "trail" = where Chastain Trail leaves that way.
# Chastain Glen Lane is not closed by itself: its long piece (9134737) passes the Chastain Trail junction 906 m in, and the
# lap leaves it there; the last 73 m of that piece (on to Courtfield Trace) are not part of the lap.
LEAD_IN = [(1161774882, False, None, None), (1360406926, True, None, None)]   # entrance, inbound lane, up to the Chastain Trail junction
LOOP = [(9134741, True, None, None), (9134737, False, 0, "trail"), (9125364, False, None, None)]
SETTINGS = {
    "name": "Chastain Glen loop",
    "direction": "forward",         # "forward" or "reverse": the one-line switch. Reverse drives the same loop backwards.
    "laps": 3,
    "gate_after_junction_m": 22.0,  # start/finish gate this far past the Chastain Trail junction, on the way to Glenbarrett Lane
    "reverse_grid_back_m": 45.0,    # reverse: the grid sits this far before the gate
    # Round 6: forgiving gates. Half widths from the route centreline (the road is 8 m wide, half 4 m): the start/finish gate
    # counts anywhere within 16 m of the centre, checkpoints within 14 m, i.e. the road, its verge and about 10-12 m of yard.
    # The car's own half extent is added at the moment of crossing (RaceGate.Crossed). Round 5 had 9 and 11.
    "gate_half_width_m": 16.0,
    "checkpoint_half_width_m": 12.0,   # Round 21: 12 (was 14): with 14 m a cut through the front yards beside the road still passed the gates
    # Round 21 (Dan, 2026-10-12 10:13: "to prevent major shortcuts, we can put a CP"): checkpoints placed by hand (stations along the loop)
    # so that every free cut through the yards saving 30 m or more misses one (Tools/mapgen/free_cuts.py; chosen with its scan), in
    # place of the old filler checkpoint at 1000.6 m. They go in before the filler, so no filler is needed.
    "extra_checkpoints_s": [952.0, 1056.0, 1216.0],
    "gate_below_m": 6.0,            # a crossing still counts this far under the road surface at the gate ...
    "gate_above_m": 25.0,           # ... and this far over it (a jump over the gate counts)
    "closure_clear_m": 12.0,        # Round 6: a barrier stands where a road that leaves the course is this far from it
    "closure_min_branch_m": 14.0,   # a leaving road shorter than this is a dead-end stub and is not barred
    "closure_extra_width_m": 6.0,   # (the barrier is the road width plus this many metres of verge, in the game)
    "max_checkpoint_gap_m": 330.0,
    "min_checkpoint_gap_m": 60.0,
}

m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]
by_osm = {r["osm_id"]: r for r in roads}
def pts(r): p = r["points"]; return [(p[i], p[i + 1]) for i in range(0, len(p), 2)]
def cum(p):
    c = [0.0]
    for i in range(len(p) - 1): c.append(c[-1] + math.dist(p[i], p[i + 1]))
    return c
def point_at(line, s):
    c = cum(line); s = max(0, min(c[-1], s))
    for i in range(len(line) - 1):
        if c[i] <= s <= c[i + 1] + 1e-9:
            t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9)
            return (line[i][0] + t * (line[i + 1][0] - line[i][0]), line[i][1] + t * (line[i + 1][1] - line[i][1]))
    return line[-1]
def cut(p, a, b):
    c = cum(p); b = c[-1] if b is None else b
    return [point_at(p, a)] + [p[i] for i in range(len(p)) if a + 1e-6 < c[i] < b - 1e-6] + [point_at(p, b)]
def nearest_s(p, q):
    c = cum(p); best = (1e18, 0)
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; d = math.dist(a, b)
        t = max(0, min(1, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / (d * d)))
        dd = math.dist(q, (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])))
        if dd < best[0]: best = (dd, c[i] + t * d)
    return best

def assemble(spec):
    line, pieces = [], []
    for osm, rev, a, b in spec:
        r = by_osm[osm]; p = pts(r)
        if b == "trail": b = nearest_s(p, pts(by_osm[9125364])[0])[1]
        seg = cut(p, a or 0.0, b)
        if rev: seg = seg[::-1]
        L = cum(seg)[-1]
        pieces.append({"osm_id": osm, "name": r["name"], "class": r["class"], "reverse": rev, "length_m": round(L, 1)})
        if line and math.dist(line[-1], seg[0]) > 0.5: raise SystemExit(f"route breaks before {r['name']} ({osm}): gap {math.dist(line[-1], seg[0]):.1f} m")
        line += seg if not line else seg[1:]
    return line, pieces

lead, lead_pieces = assemble(LEAD_IN)
loop, loop_pieces = assemble(LOOP)
if math.dist(loop[0], loop[-1]) > 0.5: raise SystemExit("loop does not close: gap %.1f m" % math.dist(loop[0], loop[-1]))
if math.dist(lead[-1], loop[0]) > 0.5: raise SystemExit("lead-in does not reach the loop start")
loop[-1] = loop[0]
sp = (m["spawn"]["x"], m["spawn"]["z"])
lead = cut(lead, nearest_s(lead, sp)[1], None)          # the grid is the map's spawn point, on the entrance lane
L = cum(loop)[-1]

# --- junctions: other roads with an end within 1.5 m of the loop centreline
own = {pc["osm_id"] for pc in loop_pieces} | {x[0] for x in LEAD_IN}
junc = []
for r in roads:
    if r["osm_id"] in own: continue
    p = pts(r)
    for end in (p[0], p[-1]):
        d, s = nearest_s(loop, end)
        if d < 1.5: junc.append((s, r["name"] or "(unnamed " + r["class"] + ")"))
junc.append((0.0, "Chastain Glen Lane entrance"))
junc.append((0.0, "Chastain Trail"))
junc.sort()
merged = []
for s, nm in junc:
    if merged and s - merged[-1]["s"] < 6: merged[-1]["roads"].append(nm); continue
    merged.append({"s": round(s, 1), "roads": [nm]})
for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(loop, j["s"])]

# --- shortcut analysis: drivable graph without the lap's own roads. A side branch that leaves the lap at one junction and
# meets it again at another is a shortcut if it is shorter than the lap between them (forward travel).
K = lambda q: (round(q[0] * 10), round(q[1] * 10))
adj = {}
def add(a, b, w, one):
    adj.setdefault(a, []).append((b, w)); adj.setdefault(b, [])
    if not one: adj[b].append((a, w))
for r in roads:
    if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link") or r["osm_id"] in own: continue
    p = pts(r)
    for i in range(len(p) - 1): add(K(p[i]), K(p[i + 1]), math.dist(p[i], p[i + 1]), r["oneway"])
nodes = list(adj.keys())
def snap(q):
    best = None
    for n in nodes:
        d = math.dist((n[0] / 10, n[1] / 10), q)
        if d < 2.5 and (best is None or d < best[0]): best = (d, n)
    return best[1] if best else None
shortcuts = []
for a in merged:
    na = snap(tuple(a["pos"]))
    if na is None: continue
    dist = {na: 0.0}; pq = [(0.0, na)]
    while pq:
        d, u = heapq.heappop(pq)
        if d > dist.get(u, 1e18) or d > 1500: continue
        for v, w in adj.get(u, []):
            if d + w < dist.get(v, 1e18): dist[v] = d + w; heapq.heappush(pq, (d + w, v))
    for b in merged:
        if b is a: continue
        nb = snap(tuple(b["pos"]))
        if nb is None or nb not in dist: continue
        lap_d = (b["s"] - a["s"]) % L
        if dist[nb] < lap_d - 15:
            shortcuts.append({"from_s": a["s"], "to_s": b["s"], "via_m": round(dist[nb]), "lap_m": round(lap_d), "saves_m": round(lap_d - dist[nb]),
                              "from": a["roads"], "to": b["roads"]})

# --- checkpoints, in forward order, s along the loop from the Chastain Trail junction. One at every junction a shortcut leaves
# from or returns to (at least min gap apart), then more wherever the gap would exceed max gap, so the next one is in sight.
gate_s = SETTINGS["gate_after_junction_m"]
cps = []
for s in sorted({x["from_s"] for x in shortcuts} | {x["to_s"] for x in shortcuts}):
    if (s - gate_s) % L < SETTINGS["min_checkpoint_gap_m"] or (gate_s + L - s) % L < SETTINGS["min_checkpoint_gap_m"]: continue
    if any(abs(s - c["s"]) < SETTINGS["min_checkpoint_gap_m"] for c in cps): continue
    cps.append({"s": round(s, 1)})
for s in SETTINGS.get("extra_checkpoints_s", []):   # Round 21
    if not any(abs(s - c["s"]) < SETTINGS["min_checkpoint_gap_m"] for c in cps): cps.append({"s": round(s, 1)})
cps.sort(key=lambda c: (c["s"] - gate_s) % L)
marks = [0.0] + [(c["s"] - gate_s) % L for c in cps] + [L]
extra = []
for a, b in zip(marks[:-1], marks[1:]):
    n = int(math.ceil((b - a) / SETTINGS["max_checkpoint_gap_m"]))
    extra += [{"s": round((gate_s + a + (b - a) * k / n) % L, 1)} for k in range(1, n)]
cps = sorted(cps + extra, key=lambda c: (c["s"] - gate_s) % L)
for i, c in enumerate(cps, 1):
    c["id"] = i; c["pos"] = [round(v, 1) for v in point_at(loop, c["s"])]
    near = min(merged, key=lambda j: abs(j["s"] - c["s"]))
    c["label"] = ("at " + " / ".join(near["roads"])) if abs(near["s"] - c["s"]) < 20 else "on the loop"

# --- Round 6: roads that leave the course. Every drivable road that meets the lap or the lead-in (it may also be the lap's own
# road, past the point where the lap leaves it) is followed away from the course until it is closure_clear_m from it; a
# barrier goes there during a race. The grid end of the lead-in counts as the course end, so the road behind the grid is
# closed as well. Roads that only run beside the course closer than that for their whole length are dead ends (not barred).
course_lines = [loop, lead]
def course_dist(q):
    return min(min(math.dist(q, (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]))) for t in [max(0.0, min(1.0, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / max((b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2, 1e-9)))])
               for line in course_lines for a, b in zip(line[:-1], line[1:]))
def walk(p, k, step):
    """Points along polyline p from vertex k, towards k+step (+1 or -1): yields (arc_m, point, tangent)."""
    arc = 0.0
    i = k
    while 0 <= i + step < len(p):
        a, b = p[i], p[i + step]; seg = math.dist(a, b)
        if seg < 1e-6: i += step; continue
        n = int(seg / 0.5) + 1
        for q in range(1, n + 1):
            d = min(seg, q * 0.5)
            yield arc + d, (a[0] + (b[0] - a[0]) * d / seg, a[1] + (b[1] - a[1]) * d / seg), ((b[0] - a[0]) / seg, (b[1] - a[1]) / seg)
        arc += seg; i += step
def follow(ri, k, step, arc0, depth, seen):
    """Walk road ri from vertex k towards k+step. Returns (arc, point, tangent, road) where the walk first reaches
    closure_clear_m from the course, continuing onto roads that meet the end of this one (up to 3 hops, 120 m); or None."""
    p = roads_pts[ri]; arc = arc0; last = None
    for d, q, tan in walk(p, k, step):
        last = (arc0 + d, q, tan)
        if course_dist(q) >= SETTINGS["closure_clear_m"]: return (arc0 + d, q, tan, ri)
        if arc0 + d > 120: return None
    if last is None or depth >= 3: return None
    end = p[len(p) - 1] if step > 0 else p[0]
    for rj, pj in enumerate(roads_pts):
        if rj == ri or rj in seen or roads[rj]["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
        for kj in (0, len(pj) - 1):
            if math.dist(pj[kj], end) < 1.5:
                res = follow(rj, kj, 1 if kj == 0 else -1, last[0], depth + 1, seen | {ri})
                if res: return res
    return None
roads_pts = [pts(r) for r in roads]
closures, stubs = [], []
for ri, r in enumerate(roads):
    if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
    p = roads_pts[ri]
    on = [course_dist(v) < 2.5 for v in p]
    for k in range(len(p)):
        if not on[k]: continue
        for step in (1, -1):
            nb = k + step
            if not (0 <= nb < len(p)) or on[nb]: continue
            contact = p[k]
            res = follow(ri, k, step, 0.0, 0, frozenset())
            if res is None:
                stubs.append({"road": r["name"] or "(unnamed " + r["class"] + ")", "osm_id": r["osm_id"], "contact": [round(contact[0], 1), round(contact[1], 1)]})
                continue
            arc, q, tan, rj = res
            sj = nearest_s(loop, contact)
            closures.append({"road": r["name"] or "(unnamed " + r["class"] + ")", "osm_id": r["osm_id"], "x": round(q[0], 2), "z": round(q[1], 2),
                             "dx": round(tan[0], 4), "dz": round(tan[1], 4), "width_m": roads[rj]["width"], "junction_s": round(sj[1], 1) if sj[0] < 3 else -1.0,
                             "contact": [round(contact[0], 1), round(contact[1], 1)], "arc_m": round(arc, 1), "barrier_on": roads[rj]["name"] or "(unnamed " + roads[rj]["class"] + ")"})
# the same barrier twice (two roads meeting at one spot, a way split in two): keep one, the wider
closures.sort(key=lambda c: -c["width_m"])
kept = []
for c in closures:
    if any(math.dist((c["x"], c["z"]), (o["x"], o["z"])) < 5.0 and abs(c["dx"] * o["dx"] + c["dz"] * o["dz"]) > 0.8 for o in kept): continue
    kept.append(c)
closures = sorted(kept, key=lambda c: (c["junction_s"] < 0, c["junction_s"], c["x"]))
for i, c in enumerate(closures, 1):
    c["id"] = f"B{i}"
    c["label"] = f"{c['road']} at {'lap ' + format(c['junction_s'], '.0f') + ' m' if c['junction_s'] >= 0 else 'the entrance'}"
    c["x"] = c["x"]; c["z"] = c["z"]

route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_route.py from Data/map.json. Coordinates are map metres (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L, 1),
    "loop_pieces": loop_pieces, "lead_in_pieces": lead_pieces,
    "loop": [round(v, 2) for q in loop for v in q],      # flat x, z, x, z ... like map.json
    "lead_in": [round(v, 2) for q in lead for v in q],
    "lead_in_length_m": round(cum(lead)[-1], 1),
    "gate_s": gate_s,
    "junctions": merged, "shortcuts": shortcuts, "checkpoints": cps,
    "closures": closures, "closure_stubs_not_barred": stubs,
    "official_shortcuts": [],   # hand-authored, kept across runs (see below); format in Assets/Scripts/Race/RaceRoute.cs (RouteShortcutJson)
})
if os.path.exists(OUT):
    try:
        old = json.load(open(OUT, encoding="utf-8"))
        route["direction"] = old["direction"]
        route["official_shortcuts"] = old.get("official_shortcuts", [])   # hand-made: never lost by a re-run
    except Exception: pass
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(route, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"lap {L:.0f} m ({', '.join(p['name'] + ' ' + str(round(p['length_m'])) for p in loop_pieces)}); lead-in {route['lead_in_length_m']:.0f} m")
print("junctions on the lap:")
for j in merged: print(f"  s={j['s']:7.1f}  {', '.join(j['roads'])}")
print("shortcuts:")
for x in shortcuts: print(f"  {x['from_s']:.0f} -> {x['to_s']:.0f}: {x['via_m']} m vs {x['lap_m']} m on the lap (saves {x['saves_m']}); {x['from']} -> {x['to']}")
print(f"closures (barriers while racing): {len(closures)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}) road width {c['width_m']:.0f} m")
print(f"dead-end stubs not barred: {len(stubs)}")
for t in stubs: print(f"  {t['road']} {t['contact']}")
print("checkpoints:")
for c in cps: print(f"  CP{c['id']} s={c['s']:7.1f} {c['pos']} {c['label']}")
