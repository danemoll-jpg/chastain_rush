"""Round 6, item 7 (REPORT ONLY - builds nothing in the game): finds places on the Chastain Glen loop where a backyard route
could cut the lap short and launch the car, from the elevation data (Data/heightmap_1m.tif), the building footprints, trees
(Data/trees.bin), water and roads (Data/map.json) and the lap itself (Data/routes/chastain_glen_loop.json).

How it looks: the lap is sampled every 3 m; every pair of points (A earlier in the lap, B later) is a candidate straight cut
when B is close to A in the plane but far from it along the lap. Each cut is measured along a 1 m profile: height difference,
the steepest drop (a launch lip), houses in the way, trees within 2.5 m, water, existing roads it would just duplicate, and
the checkpoints it skips. The best non-overlapping ones are kept, with variety, and an overhead image is drawn.
Heights are what the player sees (vertical_scale applied to the relief, about the lowest point of the crop).

Usage: python Tools/mapgen/shortcut_candidates.py      Writes Data/screenshots/round6/shortcut_candidates.{png,md,json}
Not part of the game build, and a candidate is not a shortcut: nothing here is added to the route file."""
import json, math, os, struct
import numpy as np
import rasterio
from scipy.ndimage import map_coordinates
from scipy.spatial import cKDTree
from matplotlib.path import Path as MPath
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource
from matplotlib.patches import Polygon as MPoly

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data", "screenshots", "round6")
os.makedirs(OUT, exist_ok=True)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
route = json.load(open(os.path.join(ROOT, "Data/routes/chastain_glen_loop.json"), encoding="utf-8"))
VS = m["vertical_scale"]
H = 1918  # map height in metres (rows of the DEM)

# ------------------------------------------------------------------ data
dem_ds = rasterio.open(os.path.join(ROOT, "Data/heightmap_1m.tif"))
dem = dem_ds.read(1).astype("float64")
DATUM = float(dem.min())
def hz(x, z):
    """Game height (vertical_scale applied about the lowest point) at map metres x, z (bilinear, pixel centres)."""
    r = H - np.asarray(z) - 0.5; c = np.asarray(x) - 0.5
    return DATUM + (map_coordinates(dem, [r, c], order=1, mode="nearest") - DATUM) * VS

loop = np.array(route["loop"]).reshape(-1, 2)
seg = np.hypot(*np.diff(loop, axis=0).T); cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
GATE_S = route["gate_s"]
def at(s):
    s = np.mod(s, L)
    return np.stack([np.interp(s, cum, loop[:, 0]), np.interp(s, cum, loop[:, 1])], axis=-1)

trees = np.array(struct.unpack_from("<%df" % (2 * struct.unpack_from("<I", open(os.path.join(ROOT, "Data/trees.bin"), "rb").read(), 4)[0]), open(os.path.join(ROOT, "Data/trees.bin"), "rb").read(), 8)).reshape(-1, 2)
tree_kd = cKDTree(trees)
bpoly = []
for i, b in enumerate(m["buildings"]):
    f = np.array(b["footprint"]).reshape(-1, 2)
    bpoly.append((f, MPath(f), f.min(0), f.max(0), b))
road_pts, road_w, road_name, road_id = [], [], [], []
for r in m["roads"]:
    if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
    p = np.array(r["points"]).reshape(-1, 2)
    for a, b in zip(p[:-1], p[1:]):
        n = max(1, int(math.dist(a, b)))
        for t in np.linspace(0, 1, n, endpoint=False):
            road_pts.append(a + (b - a) * t); road_w.append(r["width"]); road_name.append(r["name"] or "(unnamed)"); road_id.append(r["osm_id"])
road_pts = np.array(road_pts); road_w = np.array(road_w); road_kd = cKDTree(road_pts)
water_polys = [(MPath(np.array(w["outline"]).reshape(-1, 2)), w.get("name", "")) for w in m["water"]]
stream_pts = []
for s_ in m["streams"]:
    p = np.array(s_["points"]).reshape(-1, 2)
    for a, b in zip(p[:-1], p[1:]):
        for t in np.linspace(0, 1, max(1, int(math.dist(a, b))), endpoint=False): stream_pts.append(a + (b - a) * t)
stream_kd = cKDTree(np.array(stream_pts)) if stream_pts else None

# the lap's own checkpoints in lap order from the start/finish gate (u = distance after the gate)
cps = [(c["id"], (c["s"] - GATE_S) % L, c["pos"], c["label"]) for c in route["checkpoints"]]

# ------------------------------------------------------------------ measuring one cut
def measure(sa, sb):
    """The straight cut from lap distance sa to sb (lap coordinates, sa < sb)."""
    A, B = at(sa), at(sb)
    D = float(np.hypot(*(B - A)))
    n = max(2, int(D))
    t = np.linspace(0, 1, n + 1)
    P = A + (B - A) * t[:, None]
    d = D / n
    z = hz(P[:, 0], P[:, 1])
    out = {"sa": float(sa), "sb": float(sb), "A": A.tolist(), "B": B.tolist(), "D": D, "lap_m": float(sb - sa), "step": d}
    out["saving_m"] = (sb - sa) - D * 1.1   # the cut is rarely a perfect line
    out["dz"] = float(z[0] - z[-1])         # + = the cut ends lower than it starts
    out["zA"], out["zB"] = float(z[0]), float(z[-1])
    # steepest drop within a 25 m window along the cut, and the best launch lip: a spot after a calm approach where the
    # ground falls away by several metres in the next 15 m
    w = max(1, int(round(25 / d))); w15 = max(1, int(round(15 / d)))
    out["max_drop25"] = float(max((z[i] - z[min(len(z) - 1, i + w)]) for i in range(len(z))))
    best = (0.0, 0.0, 0.0)
    for i in range(3, len(z) - 1):
        j = min(len(z) - 1, i + w15)
        fall = z[i] - z[i:j + 1].min()
        approach = (z[i - 3] - z[i]) / (3 * d)    # slope arriving at i (positive = descending)
        if fall > best[0] and approach < 0.12: best = (float(fall), float(i * d), float(approach))
    out["lip_fall15"], out["lip_at_m"], out["lip_approach"] = best
    li = int(round(best[1] / d)); out["lip_xy"] = P[min(li, len(P) - 1)].tolist()
    out["crest_xy"] = P[int(np.argmax(z))].tolist(); out["crest_z"] = float(z.max())
    out["crest_over_ends"] = float(z.max() - max(z[0], z[-1]))
    out["steepest_pct"] = float(np.abs(np.diff(z) / d).max() * 100)
    # houses: metres of the cut inside a footprint (centre line), the corridor +-1.5 m for "does a car fit"
    hit = {}; inside = np.zeros(len(P), bool)
    lo, hi = P.min(0) - 3, P.max(0) + 3
    for f, path, bmin, bmax, b in bpoly:
        if bmax[0] < lo[0] or bmin[0] > hi[0] or bmax[1] < lo[1] or bmin[1] > hi[1]: continue
        ins = path.contains_points(P)
        near = path.contains_points(P + np.array([-(B - A)[1], (B - A)[0]]) / max(D, 1) * 1.5) | path.contains_points(P - np.array([-(B - A)[1], (B - A)[0]]) / max(D, 1) * 1.5)
        if ins.any() or near.any():
            hit[id(b)] = (b["type"], b["height"], float(ins.sum() * d), float(P[ins][:, 0].mean()) if ins.any() else float(P[near][:, 0].mean()), float(P[ins][:, 1].mean()) if ins.any() else float(P[near][:, 1].mean()))
            inside |= ins
    out["houses"] = [{"type": v[0], "height": round(v[1], 1), "metres_inside": round(v[2], 1), "x": round(v[3]), "z": round(v[4])} for v in hit.values()]
    # trees within 2.5 m of the cut
    tr = set()
    for p in P[::2]: tr.update(tree_kd.query_ball_point(p, 2.5))
    out["trees"] = len(tr)
    # existing roads the cut would only duplicate: metres within the road's half width + 1.5 m, not counting the last 18 m at each end
    dist, idx = road_kd.query(P)
    onroad = dist < (road_w[idx] * 0.5 + 1.5)
    core = (np.arange(len(P)) * d > 18) & (np.arange(len(P)) * d < D - 18)
    out["road_m"] = float((onroad & core).sum() * d)
    out["crosses"] = sorted({road_name[i] for i, o in zip(idx, onroad & core) if o})
    # water
    wet = np.zeros(len(P), bool); wnames = set()
    for path, name in water_polys:
        ins = path.contains_points(P)
        if ins.any(): wet |= ins; wnames.add(name or "pond")
    if stream_kd is not None:
        sd, _ = stream_kd.query(P); wet |= sd < 2.5
        if (sd < 2.5).any(): wnames.add("stream")
    out["water_m"] = float(wet.sum() * d); out["water"] = sorted(wnames)
    out["skipped_cps"] = [c[0] for c in cps if (sa - GATE_S) % L < c[1] < (sb - GATE_S) % L] if (sa - GATE_S) % L < (sb - GATE_S) % L else []
    out["profile"] = [round(float(v), 2) for v in z[:: max(1, len(z) // 60)]]
    return out

# ------------------------------------------------------------------ search
STEP = 3.0
us = np.arange(0, L, STEP)            # u = distance along the lap after the gate; lap coordinate s = GATE_S + u
cands = []
for i, ua in enumerate(us):
    A = at(GATE_S + ua)
    for ub in us[i + 1:]:
        lap_d = ub - ua
        if lap_d < 100 or lap_d > 700: continue
        B = at(GATE_S + ub)
        D = float(np.hypot(*(B - A)))
        if not (30 <= D <= 260) or lap_d - 1.1 * D < 80 or lap_d / D < 1.5: continue
        cands.append((ua, ub))
print(f"{len(cands)} geometric candidates")
rows = []
for ua, ub in cands:
    r = measure(GATE_S + ua, GATE_S + ub)
    r["ua"], r["ub"] = float(ua), float(ub)
    if r["road_m"] > 0.25 * r["D"] or r["water_m"] > 10: continue   # mostly an existing street, or in the pond
    nh = len(r["houses"])
    # score: what saves distance and what launches the car; mild cost for trees; houses are allowed (Dan wants some) but cost a little
    r["score"] = r["saving_m"] / 80 + 0.5 * r["max_drop25"] + 0.6 * r["lip_fall15"] + 0.25 * r["crest_over_ends"] - 0.015 * r["trees"] - 0.4 * nh - 0.04 * r["road_m"]
    rows.append(r)
print(f"{len(rows)} after removing street duplicates and ponds")

def tag(r):
    nh = len(r["houses"]); inside = sum(h["metres_inside"] for h in r["houses"])
    t = []
    if r["lip_fall15"] >= 3.5 or r["max_drop25"] >= 6: t.append("drop")
    if r["crest_over_ends"] >= 3: t.append("crest")
    if nh == 0: t.append("open ground" if r["trees"] < 12 else "woods")
    elif inside > 0 and nh <= 2 and inside <= 16: t.append("over a house")
    elif inside > 16 or nh > 2: t.append("through houses")
    else: t.append("between houses")
    return t
for r in rows: r["tags"] = tag(r)

# greedy selection: best score first, skipping a cut that shares more than 40% of its lap interval with one already kept or
# that starts or ends within 35 m of one already kept; quota of kinds so the list is not eight copies of the same idea
rows.sort(key=lambda r: -r["score"])
kept = []
def clash(r):
    ma = (np.array(r["A"]) + np.array(r["B"])) / 2
    for k in kept:
        mk = (np.array(k["A"]) + np.array(k["B"])) / 2
        if np.hypot(*(ma - mk)) < 35: return True                                   # the same patch of ground
        if abs(r["ua"] - k["ua"]) < 25 or abs(r["ub"] - k["ub"]) < 25 or abs(r["ua"] - k["ub"]) < 25 or abs(r["ub"] - k["ua"]) < 25: return True   # same junction
    return False
quota = {"over a house": 3, "through houses": 3, "woods": 2, "between houses": 2, "open ground": 2}
used = {k: 0 for k in quota}
for r in rows:
    kind = [t for t in r["tags"] if t in quota][0]
    if used[kind] >= quota[kind] or clash(r): continue
    kept.append(r); used[kind] += 1
    if len(kept) == 8: break
kept.sort(key=lambda r: r["ua"])
for i, r in enumerate(kept, 1): r["n"] = i
print(f"kept {len(kept)}")

# ------------------------------------------------------------------ report
def where(u):
    s = (GATE_S + u) % L
    j = min(route["junctions"], key=lambda j: min(abs(j["s"] - s), L - abs(j["s"] - s)))
    return s, j
lines = ["# Round 6, item 7: shortcut candidates (report only - nothing of this is built)", "",
         "Made by `Tools/mapgen/shortcut_candidates.py` from the 1 m elevation data, the building footprints, the tree file, water and roads. "
         f"Heights are as the player sees them (vertical scale {VS}). The lap is {L:.0f} m, driven forward from the start/finish gate; "
         "distances are along the lap from the gate. A candidate is a straight cut between two points of the lap: the real route would bend "
         "around what is in the way, so \"distance saved\" is the lap length between the points minus 1.1 x the straight cut. "
         "Airtime is not simulated; \"launch drop\" is the ground falling away from a lip over the next 15 m, which sets how much air a car gets.", "",
         "| # | leaves | rejoins | straight cut | height change | steepest 25 m drop | best launch drop | houses | trees (2.5 m) | skips | saves |", "|---|---|---|---|---|---|---|---|---|---|---|"]
for r in kept:
    sa, ja = where(r["ua"]); sb, jb = where(r["ub"])
    lines.append(f"| {r['n']} | {r['ua']:.0f} m ({r['A'][0]:.0f}, {r['A'][1]:.0f}) | {r['ub']:.0f} m ({r['B'][0]:.0f}, {r['B'][1]:.0f}) | {r['D']:.0f} m | {r['dz']:+.1f} m | {r['max_drop25']:.1f} m | {r['lip_fall15']:.1f} m | {len(r['houses'])} | {r['trees']} | "
                 + (", ".join("CP%d" % c for c in r["skipped_cps"]) or "none") + f" | {r['saving_m']:.0f} m |")
lines += ["", "## Each candidate in words", ""]
pieces = []
acc = 0.0
for pc in route["loop_pieces"]:
    pieces.append((acc, acc + pc["length_m"], pc["name"])); acc += pc["length_m"]
def road_at(s_):
    s_ = s_ % L
    for lo, hi, nm in pieces:
        if lo <= s_ < hi + 1e-6: return nm
    return pieces[-1][2]
def near_junction(u):
    s_ = (GATE_S + u) % L
    j = min(route["junctions"], key=lambda j: min(abs(j["s"] - s_), L - abs(j["s"] - s_)))
    dd = min(abs(j["s"] - s_), L - abs(j["s"] - s_))
    return (" / ".join(j["roads"]), dd)
for r in kept:
    ja, da = near_junction(r["ua"]); jb, db = near_junction(r["ub"])
    leave = f"{road_at(GATE_S + r['ua'])} at {r['ua']:.0f} m after the gate (ground {r['zA']:.1f} m)" + (f", {da:.0f} m from the {ja} junction" if da < 60 else "")
    rejoin = f"{road_at(GATE_S + r['ub'])} at {r['ub']:.0f} m (ground {r['zB']:.1f} m)" + (f", {db:.0f} m from the {jb} junction" if db < 60 else "")
    hs = r["houses"]
    in_way = []
    for h in hs: in_way.append(f"a {h['type'].replace('_', ' ')} house at ({h['x']}, {h['z']}), {h['height']:.1f} m to the roof, {h['metres_inside']:.0f} m of the cut inside its footprint" if h["metres_inside"] > 0 else f"a house at ({h['x']}, {h['z']}) whose wall is within 1.5 m of the line")
    in_way.append(f"{r['trees']} tree{'s' if r['trees'] != 1 else ''} within 2.5 m" if r["trees"] else "no trees in the way")
    in_way.append("no water" if r["water_m"] < 1 else "water: " + ", ".join(r["water"]))
    if r["crosses"]: in_way.append("a street crossing: " + ", ".join(r["crosses"]) + f" ({r['road_m']:.0f} m on or beside a road)")
    kind = [t for t in r["tags"] if t in quota][0]
    built = []
    if r["lip_fall15"] >= 3.5:
        built.append(f"a launch ramp at the lip {r['lip_at_m']:.0f} m along the cut (near ({r['lip_xy'][0]:.0f}, {r['lip_xy'][1]:.0f})), where the ground falls {r['lip_fall15']:.1f} m within the next 15 m, and a landing ramp on the slope below, so the car lands nose-down rather than on flat ground")
    elif r["max_drop25"] >= 3:
        built.append(f"a low kicker where the ground drops {r['max_drop25']:.1f} m in 25 m; the natural fall is enough for a short hop")
    else:
        built.append("a built ramp: the land gives almost no fall, so the jump has to be made (a ramp onto a raised landing)")
    if kind == "over a house":
        built.append("a big ramp pair over the house (roof about 11 m up: a long, fast, high jump, the hardest and most spectacular of the list), with the house made solid and a landing road behind it; or, easier, a gap through the property line beside it")
    elif kind == "through houses":
        built.append(f"an OPEN HOUSE to drive through for each of the {len(hs)} houses: a door-sized opening in the front and back walls and a floor at ground level, with garage-door gaps in any fences. Houses are solid boxes now, so these would be built in Round 7's hand-made houses first")
    elif kind == "between houses":
        built.append("only a gap in a fence or hedge between two lots, plus a path; no house is in the way")
    elif kind == "woods":
        built.append("a cleared path through the trees")
    else:
        built.append("a path across open yards, with fence gaps where the property lines cross it")
    if r["crest_over_ends"] >= 3: built.append(f"a hill crest {r['crest_over_ends']:.1f} m above both ends near ({r['crest_xy'][0]:.0f}, {r['crest_xy'][1]:.0f}) gives a natural blind jump on the way")
    lines.append(f"**#{r['n']} ({', '.join(r['tags'])}).** Leaves the lap on {leave}; rejoins on {rejoin}. The straight cut is {r['D']:.0f} m against {r['lap_m']:.0f} m of lap, so it saves about {r['saving_m']:.0f} m "
                 f"and skips " + (", ".join("CP%d" % c for c in r["skipped_cps"]) if r["skipped_cps"] else "no checkpoint") + f". Height change {r['dz']:+.1f} m ({'downhill' if r['dz'] > 0 else 'uphill'}); steepest 25 m: {r['max_drop25']:.1f} m of fall; steepest 1 m step {r['steepest_pct']:.0f}%. "
                 f"In the way: " + "; ".join(in_way) + ". To build: " + "; and ".join(built) + ".")
    lines.append("")
lines += ["## Notes", "",
          f"* Only {int((cKDTree(loop).query(trees)[0] < 100).sum())} of the map's {len(trees):,} trees are within 100 m of the lap (the neighbourhood is open yards), so none of these is a woods cut. The woods and the steepest ground are east of the loop beyond the creek ravine (x about 3170 to 3200, z 1000 to 1100 in the picture, dark hillshade): a jump across it would be an out-and-back excursion from the lap rather than a cut between two lap points, so it is not in this list. Say so if you want that kind too.",
          "* Every candidate is a straight line. A real route would curve around the houses it does not go through and follow the best slope; the distance saved and the houses listed are for the straight line.",
          "* The ground is the 1 m elevation data, not the game's road-flattened terrain, so heights beside the lap can differ by a few centimetres.",
          "* A cut skipping checkpoints is forgiven for those checkpoints once it is an official shortcut (Round 6, item 5); none of these exists in the game yet.",
          "* Directions: all of these are for the forward race. A reverse race would use each cut backwards (uphill where these go downhill), which is a different, usually harder, jump."]
lines += ["", "See `shortcut_candidates.png` (overhead) and `shortcut_candidate_profiles.png` (ground height along each cut). Raw numbers: `shortcut_candidates.json`."]
json.dump(kept, open(os.path.join(OUT, "shortcut_candidates.json"), "w"), indent=1)
open(os.path.join(OUT, "shortcut_candidates.md"), "w", encoding="utf-8").write("\n".join(lines))

# ------------------------------------------------------------------ overhead image
x0, x1 = loop[:, 0].min() - 70, loop[:, 0].max() + 70
z0, z1 = loop[:, 1].min() - 70, loop[:, 1].max() + 70
gx = np.arange(x0, x1, 1.0); gz = np.arange(z0, z1, 1.0)
GX, GZ = np.meshgrid(gx, gz)
Z = hz(GX, GZ)
ls = LightSource(azdeg=300, altdeg=40)
shade = ls.hillshade(Z, vert_exag=1.0, dx=1, dy=1)
fig, ax = plt.subplots(figsize=(17, 15), dpi=100)
ax.imshow(shade, extent=[x0, x1, z0, z1], origin="lower", cmap="gray", vmin=0.2, vmax=1.0)
cs = ax.contour(GX, GZ, Z, levels=np.arange(math.floor(Z.min()), math.ceil(Z.max()), 3.0), colors="#7a5c2e", linewidths=0.4, alpha=0.7)
for w in m["water"]:
    p = np.array(w["outline"]).reshape(-1, 2)
    if p[:, 0].max() < x0 or p[:, 0].min() > x1 or p[:, 1].max() < z0 or p[:, 1].min() > z1: continue
    ax.add_patch(MPoly(p, closed=True, fc="#4a90d9", ec="#2a5f9a", alpha=0.85, zorder=2))
for s_ in m["streams"]:
    p = np.array(s_["points"]).reshape(-1, 2); ax.plot(p[:, 0], p[:, 1], color="#4a90d9", lw=1.5, zorder=2)
tin = trees[(trees[:, 0] > x0) & (trees[:, 0] < x1) & (trees[:, 1] > z0) & (trees[:, 1] < z1)]
ax.scatter(tin[:, 0], tin[:, 1], s=7, c="#2f7d32", alpha=0.35, linewidths=0, zorder=3)
for r in m["roads"]:
    if r["class"] in ("motorway", "motorway_link", "trunk", "trunk_link"): continue
    p = np.array(r["points"]).reshape(-1, 2)
    if p[:, 0].max() < x0 or p[:, 0].min() > x1 or p[:, 1].max() < z0 or p[:, 1].min() > z1: continue
    ax.plot(p[:, 0], p[:, 1], color="#e8e8e8", lw=max(1.5, r["width"] * 0.55), solid_capstyle="round", zorder=4, alpha=0.9)
for f, path, bmin, bmax, b in bpoly:
    if bmax[0] < x0 or bmin[0] > x1 or bmax[1] < z0 or bmin[1] > z1: continue
    ax.add_patch(MPoly(f, closed=True, fc="#8a6f5a" if b["type"] in ("detached", "semidetached_house", "terrace") else "#6a6a78", ec="#222", lw=0.5, zorder=5))
ax.plot(loop[:, 0], loop[:, 1], color="#d81b1b", lw=3.2, zorder=6, solid_capstyle="round")
for u in np.arange(40, L, 110):
    p = at(GATE_S + u); q = at(GATE_S + u + 6); ax.annotate("", xy=q, xytext=p, arrowprops=dict(arrowstyle="-|>", color="#d81b1b", lw=2.2), zorder=7)
for c in cps:
    ax.scatter(*c[2], s=420, c="#ffb020", edgecolors="black", linewidths=1.6, zorder=8)
    ax.text(c[2][0], c[2][1], "CP%d" % c[0], ha="center", va="center", fontsize=9, fontweight="bold", zorder=9)
g = at(GATE_S)
ax.scatter(*g, s=520, marker="s", c="white", edgecolors="black", linewidths=2.5, zorder=8)
ax.text(g[0], g[1], "S/F", ha="center", va="center", fontsize=8, fontweight="bold", zorder=9)
pal = ["#00d0ff", "#ffe000", "#ff40ff", "#40ff70", "#ff8a00", "#b0b0ff", "#00ffc0", "#ff6080"]
for r in kept:
    col = pal[(r["n"] - 1) % len(pal)]
    ax.plot([r["A"][0], r["B"][0]], [r["A"][1], r["B"][1]], color=col, lw=4.2, zorder=10, path_effects=None)
    ax.plot([r["A"][0], r["B"][0]], [r["A"][1], r["B"][1]], color="black", lw=1.0, ls=(0, (3, 3)), zorder=11)
    ax.scatter(*r["A"], s=110, c="#28c840", edgecolors="black", linewidths=1.5, zorder=12)
    ax.scatter(*r["B"], s=110, c="#ff3030", edgecolors="black", linewidths=1.5, zorder=12)
    mid = (np.array(r["A"]) + np.array(r["B"])) / 2
    ax.text(mid[0], mid[1], str(r["n"]), ha="center", va="center", fontsize=15, fontweight="bold", color="black", zorder=13,
            bbox=dict(boxstyle="circle,pad=0.3", fc=col, ec="black", lw=2))
ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal")
ax.set_title("Chastain Glen loop (forward, red) with checkpoints and numbered shortcut candidates\n"
             "green dot = where a cut leaves the lap, red dot = where it rejoins; brown contours every 3 m (vertical scale %.1f); houses brown, trees green" % VS, fontsize=13)
ax.set_xlabel("x (m, map east)"); ax.set_ylabel("z (m, map north)")
fig.tight_layout()
fig.savefig(os.path.join(OUT, "shortcut_candidates.png"))
print("wrote", os.path.join(OUT, "shortcut_candidates.png"))

# profiles: one small panel per candidate
fig, axs = plt.subplots(2, 4, figsize=(17, 7), dpi=90)
for a, r in zip(axs.ravel(), kept):
    prof = np.array(r["profile"]); xs = np.linspace(0, r["D"], len(prof))
    a.fill_between(xs, prof.min() - 2, prof, color="#caa86a"); a.plot(xs, prof, color="#5a3e12")
    a.set_title("#%d  cut %.0f m, %+.1f m, launch drop %.1f m" % (r["n"], r["D"], r["dz"], r["lip_fall15"]), fontsize=10)
    a.set_ylim(prof.min() - 2, prof.max() + 3); a.grid(alpha=0.3)
for a in axs.ravel()[len(kept):]: a.axis("off")
fig.suptitle("Ground height along each straight cut (metres, game scale)")
fig.tight_layout(); fig.savefig(os.path.join(OUT, "shortcut_candidate_profiles.png"))

for r in kept:
    print(f"#{r['n']} u {r['ua']:.0f}->{r['ub']:.0f}  cut {r['D']:.0f} m  dz {r['dz']:+.1f}  drop25 {r['max_drop25']:.1f}  lip {r['lip_fall15']:.1f}@{r['lip_at_m']:.0f}  crest {r['crest_over_ends']:.1f}  "
          f"houses {len(r['houses'])} {[ (h['type'], h['height'], h['metres_inside']) for h in r['houses']]}  trees {r['trees']}  road {r['road_m']:.0f}  water {r['water_m']:.0f}  skips {r['skipped_cps']}  saves {r['saving_m']:.0f}  {r['tags']}")

if os.environ.get("CAND_DEBUG"):
    import collections
    cl = collections.Counter((round((r["A"][0] + r["B"][0]) / 2 / 40) * 40, round((r["A"][1] + r["B"][1]) / 2 / 40) * 40) for r in rows)
    print(cl.most_common(30))
    print("tags", collections.Counter(t for r in rows for t in r["tags"]))
