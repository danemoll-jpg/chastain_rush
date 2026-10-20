"""Round 52 (Dan, 2026-10-19 22:23): the "Addison Heights" race, the neighborhood south of New Chastain Road and west of Canton Road,
from Dan's red line on a map (local/reference_photos/embry_lane/r52_route_map.jpg: looked at only; the course is laid on OSM's roads from
Data/map.json). Forward and reverse, no shortcuts yet (Dan picks them once he has driven it). Forward, one lap:
  1. Joel Drive west from Hayworth Drive to its west end
  2. round the bend onto Embry Lane NE: north up its west leg, east along it, down its east end past Joel Drive's end to Suholden Circle NE
  3. Suholden Circle west to its bend, then south on it to Kelly Drive NE
  4. Kelly Drive west to Crawford Circle, north up Crawford Circle's east leg and Hayworth Drive back to Joel Drive: the lap
Reverse is the same roads the other way, a lap of its own (RaceRoute: loop_rev and the *_rev fields), as Chastain Lakes'.
Open roads ("traffic": true), two-way roads raced on their right-hand lane, as the other open-road races. Every road that leaves the lap is
barred in a race (closures, from the roads themselves).
World pieces (Data/tracks/addison_heights.json, read by MapBuilder.Round52.cs): one cleared line, no tree trunk within 1.5 m of the lap's road edge
(Round 52 check: AI drivers coming down Embry Lane's 30 % hill ran into trunks standing in the gutter and one missed a checkpoint).

Hand choices are the constants below; everything else is computed, so it re-runs after any mapgen run. It writes
Data/routes/addison_heights.json. Picture for checking (git-ignored, it shows houses): local/round52/layout.png with --plot.

Usage: python Tools/mapgen/make_addison_route.py [--plot]
"""
import json, math, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_ROUTE = os.path.join(ROOT, "Data/routes/addison_heights.json")
OUT_TRACK = os.path.join(ROOT, "Data/tracks/addison_heights.json")
EDGE_CLEAR_M = 1.5   # no trunk this close to the lap's road edge
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
roads = m["roads"]; by_osm = {r["osm_id"]: r for r in roads}

def pts(flat): return [(flat[i], flat[i + 1]) for i in range(0, len(flat), 2)]
def flat(p): return [round(v, 2) for q in p for v in q]
def cum(p):
    c = [0.0]
    for i in range(len(p) - 1): c.append(c[-1] + math.dist(p[i], p[i + 1]))
    return c
def point_at(line, s, c=None):
    c = c or cum(line); s = max(0.0, min(c[-1], s))
    for i in range(len(line) - 1):
        if c[i] <= s <= c[i + 1] + 1e-9:
            t = (s - c[i]) / max(c[i + 1] - c[i], 1e-9)
            return (line[i][0] + t * (line[i + 1][0] - line[i][0]), line[i][1] + t * (line[i + 1][1] - line[i][1]))
    return line[-1]
def nearest_s(p, q):
    c = cum(p); best = (1e18, 0.0)
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; d = math.dist(a, b)
        if d < 1e-9: continue
        t = max(0, min(1, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / (d * d)))
        dd = math.dist(q, (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])))
        if dd < best[0]: best = (dd, c[i] + t * d)
    return best
def cut(p, a, b):
    c = cum(p); b = c[-1] if b is None else b
    return [point_at(p, a, c)] + [p[i] for i in range(len(p)) if a + 1e-6 < c[i] < b - 1e-6] + [point_at(p, b, c)]
def resample(p, step):
    c = cum(p); n = max(1, int(math.ceil(c[-1] / step)))
    return [point_at(p, c[-1] * k / n, c) for k in range(n + 1)]
def seg_dist(q, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2
    t = 0.0 if L2 < 1e-12 else max(0.0, min(1.0, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2))
    return math.dist(q, (a[0] + ab[0] * t, a[1] + ab[1] * t))

# ---------------------------------------------------------------- hand choices
SETTINGS = {
    "name": "Addison Heights",      # Dan's name for the neighborhood (2026-10-19 22:38; not confirmed online, easy to change)
    "direction": "forward",
    "directions": ["forward", "reverse"],
    "laps": 3,
    "traffic": True,
    "gate_half_width_m": 12.0,
    "checkpoint_half_width_m": 10.0,
    "gate_below_m": 6.0,
    "gate_above_m": 25.0,
    "reverse_grid_back_m": 45.0,
    "closure_clear_m": 12.0,
    "closure_extra_width_m": 6.0,
}
LEAD_IN_M = 70.0
JOEL, EMBRY, EMBRY_E, SUHOLDEN, KELLY, CRAWFORD, HAYWORTH = 9122504, 9134327, 1360431992, 9133976, 330740879, 9133382, 9140536
J_HAY = (3012.0, 414.0)      # Joel Drive / Hayworth Drive
J_WEST = (2645.0, 423.0)     # Joel Drive's west end, Embry Lane's start
J_SUH = (3284.0, 300.0)      # Embry Lane's east end on Suholden Circle
J_KELLY = (3140.0, 97.0)     # Suholden Circle / Kelly Drive
J_CRAW = (2987.0, 120.0)     # Kelly Drive / Crawford Circle's east leg
J_CRAW_TOP = (2979.0, 305.0) # Crawford Circle's east leg / Hayworth Drive
# the start / finish: Kelly Drive's straight between Suholden Circle and Crawford Circle (155 m with no turn, a steady 5 to 8 % grade:
# Joel Drive's longer straight falls 40 % and more west of Hayworth Drive, no place for a grid). The forward lead-in (70 m) and the
# reverse grid (45 m before the line) both sit on the straight
GATE_AT = (3055.0, 110.0)
CHECKPOINTS = [
    (2700.0, 419.0, "Joel Drive"),
    (2740.0, 516.0, "Embry Lane"),               # past the bend: a cut from Joel Drive to Embry Lane through the yards misses it
    (2900.0, 525.0, "Embry Lane at Lyndon"),
    (3120.0, 530.0, "Embry Lane east"),
    (3255.0, 302.0, "Suholden Circle"),
    (3136.0, 190.0, "Suholden Circle south"),
    (2985.0, 225.0, "Crawford Circle"),
    (2999.0, 350.0, "Hayworth Drive"),
]

# ---------------------------------------------------------------- the lap
def way(osm, a=None, b=None):
    """The road from its point nearest a to its point nearest b (the road's own direction or against it)."""
    p = pts(by_osm[osm]["points"])
    sa = nearest_s(p, a)[1] if a else 0.0
    sb = nearest_s(p, b)[1] if b else cum(p)[-1]
    return cut(p, sa, sb) if sa <= sb else cut(p, sb, sa)[::-1]
SPEC = [("Joel Drive", JOEL, way(JOEL, J_HAY, J_WEST)),
        ("Embry Lane Northeast", EMBRY, way(EMBRY, J_WEST, None)),
        ("Embry Lane Northeast (east end)", EMBRY_E, way(EMBRY_E, None, J_SUH)),
        ("Suholden Circle Northeast", SUHOLDEN, way(SUHOLDEN, J_SUH, J_KELLY)),
        ("Kelly Drive Northeast", KELLY, way(KELLY, J_KELLY, J_CRAW)),
        ("Crawford Circle", CRAWFORD, way(CRAWFORD, J_CRAW, J_CRAW_TOP)),
        ("Hayworth Drive Northeast", HAYWORTH, way(HAYWORTH, J_CRAW_TOP, J_HAY))]
pieces, line, offs = [], [], []
for name, osm, seg in SPEC:
    if line and math.dist(line[-1], seg[0]) > 0.6: raise SystemExit(f"the lap breaks before {name} ({osm}): gap {math.dist(line[-1], seg[0]):.1f} m")
    seg = resample(seg, 3.0); off = by_osm[osm]["width"] / 4.0
    pieces.append({"osm_id": osm, "name": name, "reverse": False, "length_m": round(cum(seg)[-1], 1)})
    line += seg if not line else seg[1:]; offs += [off] * (len(seg) if not offs else len(seg) - 1)
if math.dist(line[0], line[-1]) > 0.6: raise SystemExit("the lap does not close: gap %.1f m" % math.dist(line[0], line[-1]))
line[-1] = line[0]; offs[-1] = offs[0]
N = len(line)
def lane(ln, of):
    """The lap's line set off to the right of the travel direction (the right-hand lane), the joins between pieces eased."""
    out = []
    for i in range(N):
        a, b = ln[max(0, i - 1)], ln[min(N - 1, i + 1)]; t = (b[0] - a[0], b[1] - a[1]); n = math.hypot(*t) or 1.0
        out.append((ln[i][0] + t[1] / n * of[i], ln[i][1] - t[0] / n * of[i]))
    out[-1] = out[0]
    return out
lap = lane(line, offs)
C = cum(lap); L_lap = C[-1]
def S(q): return nearest_s(lap, q)[1]
print(f"lap {L_lap:.0f} m: " + ", ".join(f"{p['name']} {p['length_m']:.0f}" for p in pieces))
gate_s = round(S(GATE_AT), 1)
lead = [point_at(lap, (gate_s - LEAD_IN_M) + k * 2.0, C) for k in range(int(LEAD_IN_M / 2.0) + 1)]
def checkpoints(lp, Cl, L, g):
    out = []
    for x, z, label in CHECKPOINTS:
        s = round(nearest_s(lp, (x, z))[1], 1); p = point_at(lp, s, Cl); out.append({"s": s, "pos": [round(p[0], 1), round(p[1], 1)], "label": label})
    out = sorted(out, key=lambda c: (c["s"] - g) % L)
    for i, c in enumerate(out, 1): c["id"] = i
    return out
cps = checkpoints(lap, C, L_lap, gate_s)
print(f"gate at s {gate_s} {[round(v) for v in point_at(lap, gate_s, C)]}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps))

# ---------------------------------------------------------------- closures (as the Chastain Lakes race's)
def walk(p, k, step):
    arc = 0.0; i = k
    while 0 <= i + step < len(p):
        a, b = p[i], p[i + step]; seg = math.dist(a, b)
        if seg < 1e-6: i += step; continue
        n = int(seg / 0.5) + 1
        for q in range(1, n + 1):
            d = min(seg, q * 0.5)
            yield arc + d, (a[0] + (b[0] - a[0]) * d / seg, a[1] + (b[1] - a[1]) * d / seg), ((b[0] - a[0]) / seg, (b[1] - a[1]) / seg)
        arc += seg; i += step
roads_pts = [pts(r["points"]) for r in roads]
def closures_for(lap):
    def course_dist(q): return min(seg_dist(q, a, b) for a, b in zip(lap[:-1], lap[1:]))
    def follow(ri, k, step, arc0, depth, seen):
        p = roads_pts[ri]; last = None
        for d, q, tan in walk(p, k, step):
            last = (arc0 + d, q, tan)
            if course_dist(q) >= SETTINGS["closure_clear_m"]: return (arc0 + d, q, tan, ri)
            if arc0 + d > 120: return None
        if last is None or depth >= 3: return None
        end = p[-1] if step > 0 else p[0]
        for rj, pj in enumerate(roads_pts):
            if rj == ri or rj in seen or roads[rj]["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
            for kj in (0, len(pj) - 1):
                if math.dist(pj[kj], end) < 1.5:
                    res = follow(rj, kj, 1 if kj == 0 else -1, last[0], depth + 1, seen | {ri})
                    if res: return res
        return None
    box = (min(q[0] for q in lap) - 150, max(q[0] for q in lap) + 150, min(q[1] for q in lap) - 150, max(q[1] for q in lap) + 150)
    closures, stubs, junc = [], [], []
    for ri, r in enumerate(roads):
        if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
        p = roads_pts[ri]
        if not any(box[0] < x < box[1] and box[2] < z < box[3] for x, z in p): continue
        on = [course_dist(v) < max(2.5, r["width"] / 4.0 + 1.0) for v in p]
        for k in range(len(p)):
            if not on[k]: continue
            for step in (1, -1):
                nb = k + step
                if not (0 <= nb < len(p)) or on[nb]: continue
                contact = p[k]
                res = follow(ri, k, step, 0.0, 0, frozenset())
                nm = r["name"] or "(unnamed " + r["class"] + ")"
                if res is None: stubs.append({"road": nm, "osm_id": r["osm_id"], "contact": [round(contact[0], 1), round(contact[1], 1)]}); continue
                arc, q, tan, rj = res
                sj = nearest_s(lap, contact)
                junc.append((sj[1], nm))
                closures.append({"road": nm, "osm_id": r["osm_id"], "x": round(q[0], 2), "z": round(q[1], 2), "dx": round(tan[0], 4), "dz": round(tan[1], 4),
                                 "width_m": roads[rj]["width"], "junction_s": round(sj[1], 1), "contact": [round(contact[0], 1), round(contact[1], 1)], "arc_m": round(arc, 1),
                                 "barrier_on": roads[rj]["name"] or "(unnamed " + roads[rj]["class"] + ")"})
    closures.sort(key=lambda c: -c["width_m"])
    kept = []
    for c in closures:
        if any(math.dist((c["x"], c["z"]), (o["x"], o["z"])) < 5.0 and abs(c["dx"] * o["dx"] + c["dz"] * o["dz"]) > 0.8 for o in kept): continue
        kept.append(c)
    closures = sorted(kept, key=lambda c: c["junction_s"])
    for i, c in enumerate(closures, 1): c["id"] = f"B{i}"; c["label"] = f"{c['road']} at lap {c['junction_s']:.0f} m"
    junc.sort(); merged = []
    for s, nm in junc:
        if merged and s - merged[-1]["s"] < 6: merged[-1]["roads"].append(nm); continue
        merged.append({"s": round(s, 1), "roads": [nm]})
    for j in merged: j["roads"] = sorted(set(j["roads"])); j["pos"] = [round(v, 1) for v in point_at(lap, j["s"], cum(lap))]
    return closures, stubs, merged
closures, stubs, merged = closures_for(lap)
print(f"closures (barriers while racing): {len(closures)}; dead-end stubs not barred: {len(stubs)}")
for c in closures: print(f"  {c['id']:4} {c['label']}  at ({c['x']:.0f}, {c['z']:.0f}), road width {c['width_m']:.0f} m")
for t in stubs: print(f"  stub (not barred): {t['road']} at {t['contact']}")

# ---------------------------------------------------------------- the reverse lap: the same roads the other way, its own right-hand lane
lap_r = lane(line[::-1], offs[::-1])
Cr = cum(lap_r); L_rev = Cr[-1]
gate_s_rev = round(nearest_s(lap_r, GATE_AT)[1], 1)
cps_rev = checkpoints(lap_r, Cr, L_rev, gate_s_rev)
print(f"reverse lap {L_rev:.0f} m, gate at s {gate_s_rev}; checkpoints: " + ", ".join(f"CP{c['id']} s {c['s']} {c['label']}" for c in cps_rev))
closures_rev, stubs_rev, merged_rev = closures_for(lap_r)
print(f"reverse closures: {len(closures_rev)}; dead-end stubs not barred: {len(stubs_rev)}")

route = dict(SETTINGS)
route.update({
    "source": "Tools/mapgen/make_addison_route.py from Data/map.json. Coordinates are map meters (= Unity x, z); heights come from the world.",
    "lap_length_m": round(L_lap, 1), "loop": flat(lap), "gate_s": gate_s, "lead_in": flat(lead), "lead_in_length_m": LEAD_IN_M,
    "checkpoints": cps, "junctions": merged, "loop_pieces": pieces, "official_shortcuts": [], "closures": closures,
    "closure_stubs_not_barred": stubs, "ai_zones": [],
    "lap_length_rev_m": round(L_rev, 1), "loop_rev": flat(lap_r), "gate_s_rev": gate_s_rev, "checkpoints_rev": cps_rev, "junctions_rev": merged_rev,
    "closures_rev": closures_rev, "ai_zones_reverse": [],
})
if os.path.exists(OUT_ROUTE):
    try: route["direction"] = json.load(open(OUT_ROUTE, encoding="utf-8"))["direction"]
    except Exception: pass
json.dump(route, open(OUT_ROUTE, "w", encoding="utf-8"), indent=1)
# the cleared line: the roads' centerline (both lanes), half the road's width plus EDGE_CLEAR_M
centre = resample(line, 6.0)
track = {"note": "Round 52: the Addison Heights race's world pieces, from Tools/mapgen/make_addison_route.py (MapBuilder.Round52.cs). Coordinates are map meters (= Unity x, z).",
         "clear_lines": [{"id": "the lap's road edges", "points": flat(centre), "half": round(max(by_osm[o]["width"] for _, o, _ in SPEC) / 2.0 + EDGE_CLEAR_M, 2)}]}
json.dump(track, open(OUT_TRACK, "w", encoding="utf-8"), indent=1)
print("wrote", os.path.relpath(OUT_ROUTE, ROOT), "and", os.path.relpath(OUT_TRACK, ROOT))

if "--plot" in sys.argv:
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    x0, x1, z0, z1 = 2560, 3360, 0, 620
    fig, ax = plt.subplots(figsize=(16, 16 * (z1 - z0) / (x1 - x0)))
    for i, b in enumerate(m["buildings"]):
        ring = pts(b["footprint"])
        if x0 < ring[0][0] < x1 and z0 < ring[0][1] < z1: ax.fill(*zip(*ring), color="#c96"); ax.text(ring[0][0], ring[0][1], str(i), fontsize=6, clip_on=True)
    for r in roads:
        p = pts(r["points"])
        if any(x0 < x < x1 and z0 < z < z1 for x, z in p): ax.plot(*zip(*p), color="#999", lw=r["width"] * 0.6); ax.text(*p[len(p) // 2], r["name"] or "", fontsize=7, clip_on=True)
    ax.plot(*zip(*lap), "b-", lw=1.0); ax.plot(*zip(*lap_r), "c--", lw=0.6)
    for c in cps: ax.plot(*c["pos"], "go"); ax.text(c["pos"][0] + 5, c["pos"][1], f"CP{c['id']}", color="g", clip_on=True)
    for c in closures: ax.plot(c["x"], c["z"], "kx"); ax.text(c["x"] + 3, c["z"] + 3, c["id"], fontsize=7, clip_on=True)
    gp_ = point_at(lap, gate_s, C); ax.plot(*gp_, "ks"); ax.text(gp_[0] + 5, gp_[1], "gate", clip_on=True)
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.grid(True, alpha=.3)
    os.makedirs(os.path.join(ROOT, "local/round52"), exist_ok=True)
    out = os.path.join(ROOT, "local/round52/layout.png")
    plt.savefig(out, dpi=80, bbox_inches="tight"); print("picture:", os.path.relpath(out, ROOT))
