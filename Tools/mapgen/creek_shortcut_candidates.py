"""Round 20, item 4 (report only, builds nothing): shortcut candidates on the Noonday Creek lap besides P1.

Dan (2026-10-11): "let it give us some suggestions for short cuts on noonday creek". Each candidate is a hand-chosen line (below)
over the real ground; everything else is measured from the data: where it leaves and rejoins the lap (lap stations, forward),
its length and the meters saved, the checkpoints it skips (an official shortcut forgives them), the ground along it, what it
crosses (the creek, a road, water), the trunks within 3.5 m of it, and for a jump the flight (g 9.81, no drag, as the game) off
an aimed lip (BigJump.cs: the lip's angle, held between vmin and vmax; the five trail vehicles all reach the lip faster than
vmax, 31 to 37 m/s flat out, so they all fly the vmax flight; the AI holds 24).

Writes Data/proposals/round20_markers.json (the markers in the world and on the map: MapBuilder.Proposals.cs,
Debug/ProposalMarkers.cs, Map/WorldMap.cs) and local/round20/creek_candidates.png (overhead, git-ignored); prints the table.
Then run Tools/handmade/make_proposal_labels.py and Build Map.

Usage: python Tools/mapgen/creek_shortcut_candidates.py
"""
import json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
track = json.load(open(os.path.join(ROOT, "Data/tracks/noonday_creek.json"), encoding="utf-8"))
route = json.load(open(os.path.join(ROOT, "Data/routes/noonday_creek.json"), encoding="utf-8"))
raw = open(os.path.join(ROOT, "Data/trees.bin"), "rb").read()
TREES = np.frombuffer(raw, dtype=np.uint8, offset=8).reshape(-1, 12)[:, :8].copy().view("<f4").reshape(-1, 2).astype(np.float64)
OUT_JSON = os.path.join(ROOT, "Data/proposals/round20_markers.json"); OUT_PNG = os.path.join(ROOT, "local/round20/creek_candidates.png")

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
V = lambda x, z: np.array([x, z], float)
def unit(v): return v / np.linalg.norm(v)
LINE = np.array(track["line"], float).reshape(-1, 2); N = len(LINE)
CUM = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(np.vstack([LINE, LINE[:1]]), axis=0), axis=1))]); LAP = CUM[-1]
def lap(s): s %= LAP; k = min(int(np.searchsorted(CUM, s, side="right")) - 1, N - 1); t = (s - CUM[k]) / (CUM[k + 1] - CUM[k]); return LINE[k] * (1 - t) + LINE[(k + 1) % N] * t
def station(q, near=None):
    d = np.linalg.norm(LINE - q, axis=1)
    if near is not None: d = d + 1e6 * (np.abs(((CUM[:-1] - near + LAP / 2) % LAP) - LAP / 2) > 80)
    return float(CUM[int(np.argmin(d))])
def length(P): return float(np.linalg.norm(np.diff(P, axis=0), axis=1).sum())
def resample(P, step=1.0):
    out = [P[0]]
    for a, b in zip(P[:-1], P[1:]):
        n = max(1, int(math.ceil(np.linalg.norm(b - a) / step)))
        out += [a + (b - a) * k / n for k in range(1, n + 1)]
    return np.array(out)
def seg_cross(a, b, c, d):
    def o(p, q, r): return np.sign((q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0]))
    return o(a, b, c) != o(a, b, d) and o(c, d, a) != o(c, d, b)
def crosses(P, lines):
    n = 0
    for L in lines:
        for a, b in zip(P[:-1], P[1:]):
            for c, d in zip(L[:-1], L[1:]):
                if seg_cross(a, b, c, d): n += 1
    return n
STREAMS = [np.array(s["points"], float).reshape(-1, 2) for s in m["streams"]]
ROADS = [np.array(r["points"], float).reshape(-1, 2) for r in m["roads"]] + [np.array(b["points"], float).reshape(-1, 2) for b in m["bridges"]]
WATER = [np.array(w["outline"], float).reshape(-1, 2) for w in m["water"]]
CPS = route["checkpoints"]

def flight(P, d, y0, v, th):
    vx, vy = v * math.cos(math.radians(th)), v * math.sin(math.radians(th)); t = 0.0; top = y0
    while t < 6.0:
        t += 0.01; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        if t > 0.1 and y <= ground(*q): return t, float(vx * t), top - y0, q
    return t, float(vx * t), top - y0, q

# ---------------------------------------------------------------- the candidates (hand-chosen lines, forward direction)
# kind 'jump': lip (x, z), heading toward 'to', lip height over its ground, lip angle, vmin / vmax (BigJump); 'pipe': mouths a, b
C = [
    dict(id="N1", name="South loop pipe", kind="pipe", look="a second drain pipe (P2): a concrete headwall in the east side's bank, under the creek, out on the west side",
         hint="worn tracks off the trail to a rusty trash rack leaning by the headwall (as P1's rock bar, something that is there)",
         path=[(2487.0, 660.0), (2487.0, 640.0), (2486.5, 530.0), (2484.0, 515.0)], a=(2487.0, 640.0), b=(2486.5, 530.0)),
    dict(id="N2", name="Low creek leap", kind="jump", look="a heap of creek rock and earth on the east bank, bulldozed up where the bank is cut (a ford that washed out)",
         hint="a road-closed barricade knocked over on its side and tire tracks over the bank",
         path=[(2534.0, 548.0), (2536.0, 540.0), (2530.0, 539.0), (2470.0, 538.0), (2467.0, 548.0), (2465.0, 558.0)], lip=(2528.0, 539.2), to=(2470.0, 538.0), h=1.8, th=24.0, vmin=23.0, vmax=25.0),
    dict(id="N3", name="Road leap", kind="jump", look="up the road's grass bank west of the bridges, across both lanes, off a dirt kicker on the north verge and down over the creek's flats",
         hint="a gap kicked in the road's guardrail and tire tracks up the bank (the creek bed run under the bridges is the slow way: 62 m in the water)",
         path=[(2433.0, 668.0), (2421.0, 700.0), (2405.0, 735.0), (2401.0, 760.0), (2400.0, 776.0), (2397.0, 800.0), (2392.0, 824.0), (2385.0, 832.0)],
         lip=(2400.0, 776.0), to=(2393.0, 822.0), h=1.2, th=18.0, vmin=20.0, vmax=24.0),
    dict(id="N4", name="Big creek leap", kind="jump", look="the biggest one: off the east side's high bank over the creek and the west side's trees to the far trail (the loop's two sides are 44 m apart here)",
         hint="orange survey stakes and flagging tape on the bank's lip (it looks like a planned crossing nobody built)",
         path=[(2486.0, 652.0), (2484.0, 646.0), (2478.0, 645.0), (2440.0, 641.0), (2436.0, 652.0), (2433.0, 664.0)], lip=(2479.0, 645.0), to=(2440.0, 641.0), h=1.6, th=22.0, vmin=18.0, vmax=21.0),
    dict(id="N5", name="Ford leap", kind="jump", look="a log ramp beside the hairpin's ford (C2): jump Noonday Creek instead of wading it",
         hint="the ramp is in plain sight beside the ford; worn tracks to it",
         path=[(2290.0, 1345.0), (2294.0, 1351.0), (2300.0, 1352.5), (2337.0, 1355.0), (2345.0, 1358.0)], lip=(2300.5, 1352.5), to=(2337.0, 1355.0), h=1.4, th=20.0, vmin=18.0, vmax=22.0),
]

def main():
    os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)
    rows = []; lines = []; marks = []; starts = []
    for c in C:
        P = np.array(c["path"], float); s0 = station(P[0]); s1 = station(P[-1], None)
        skipped = (s1 - s0) % LAP; L = length(P); saved = skipped - L
        cps = [cp["id"] for cp in CPS if 0 < (cp["s"] - s0) % LAP < skipped]
        R = resample(P, 1.0)
        near = set()
        for q in R: near.update(np.nonzero(np.linalg.norm(TREES - q, axis=1) < 3.5)[0].tolist())
        gs = [ground(*q) for q in R]
        water = crosses(P, STREAMS); roads = crosses(P, ROADS); ponds = crosses(P, WATER)
        row = dict(id=c["id"], name=c["name"], kind=c["kind"], leave=round(s0), rejoin=round(s1), length=round(L), skipped=round(skipped), saved=round(saved), cps=cps,
                   trunks=len(near), ground=(round(min(gs), 1), round(max(gs), 1)), creek=water, roads=roads, look=c["look"], hint=c["hint"])
        if c["kind"] == "jump":
            lip = V(*c["lip"]); d = unit(V(*c["to"]) - lip); y0 = ground(*lip) + c["h"]
            row["flights"] = {}
            for v in sorted({c["vmin"], min(24.0, c["vmax"]), c["vmax"]}):   # the slowest launch (anyone at 10 m/s or more), the AI, the five flat out
                t, dist, top, q = flight(lip, d, y0, v, c["th"])
                row["flights"][v] = (round(t, 2), round(dist), round(top, 1))
            row["lip"] = f"({lip[0]:.0f}, {lip[1]:.0f}) {c['h']} m, {c['th']:.0f} deg, held {c['vmin']:.0f} to {c['vmax']:.0f} m/s"
            gap = [ground(*(lip + d * k)) for k in np.arange(0, 70, 1.0)]
            row["drop"] = round(ground(*lip) - min(gap), 1)
        else:
            a, b = V(*c["a"]), V(*c["b"]); row["pipe"] = f"{np.linalg.norm(b - a):.0f} m between the mouths, ground {ground(*a):.1f} / {ground(*b):.1f} m, the creek bed about {min(ground(*q) for q in resample(np.array([a, b]), 1.0)):.1f} m"
        rows.append(row)
        lines.append({"set": "N", "name": f"{c['id']} {c['name']}", "points": [round(float(v), 2) for q in P for v in q], "spacing": 12})
        sv = f"saves {saved:.0f} m" + (f", skips CP{', CP'.join(str(x) for x in cps)}" if cps else "")
        marks.append({"set": "N", "kind": "shortcut", "title": f"{c['id']} {c['name'].upper()}", "text": "leave here: " + sv, "x": round(float(P[0][0]), 1), "z": round(float(P[0][1]), 1)})
        if c["kind"] == "jump":
            f24 = row["flights"][c["vmax"]]
            marks.append({"set": "N", "kind": "jump", "title": f"{c['id']} LIP", "text": f"{f24[1]} m, {f24[2]} m high, {f24[0]} s", "x": round(float(lip[0]), 1), "z": round(float(lip[1]), 1)})
        else:
            marks.append({"set": "N", "kind": "pipe", "title": f"{c['id']} PIPE MOUTH", "text": row["pipe"].split(",")[0], "x": c["a"][0], "z": c["a"][1]})
        back = lap(s0 - 60.0); fwd = P[0] - back
        starts.append({"name": f"{c['id']} {c['name']}", "x": round(float(back[0]), 1), "z": round(float(back[1]), 1), "yaw": round(math.degrees(math.atan2(fwd[0], fwd[1])), 1)})
    doc = {"note": "Round 20, item 4: Noonday Creek shortcut candidates (TEMPORARY markers, nothing built). Made by Tools/mapgen/creek_shortcut_candidates.py.",
           "lines": lines, "marks": marks, "starts": starts}
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    json.dump(doc, open(OUT_JSON, "w", encoding="utf-8"), indent=1)
    for r in rows:
        print(f"{r['id']} {r['name']} ({r['kind']}): leaves the lap at s {r['leave']}, rejoins at s {r['rejoin']}; {r['length']} m of line for {r['skipped']} m of lap, saves {r['saved']} m; "
              f"skips {', '.join('CP%d' % x for x in r['cps']) or 'no checkpoint'}; ground {r['ground'][0]} to {r['ground'][1]} m; crosses the creek / streams {r['creek']}x, roads {r['roads']}x; {r['trunks']} trunks within 3.5 m")
        if "flights" in r: print(f"    lip {r['lip']}; the ground falls {r['drop']} m below the lip; flights (air s, distance m, top m over the lip): " + ", ".join(f"{v} m/s {f}" for v, f in r["flights"].items()))
        if "pipe" in r: print(f"    {r['pipe']}")
        print(f"    looks like: {r['look']}\n    hint: {r['hint']}")
    json.dump(rows, open(os.path.join(ROOT, "local/round20/creek_candidates.json"), "w"), indent=1)
    # the overhead
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    fig, ax = plt.subplots(figsize=(10, 22)); x0, x1, z0, z1 = 2250, 2650, 420, 1420
    i0, i1, k0, k1 = int(x0 / SX), int(x1 / SX), int(z0 / SZ), int(z1 / SZ)
    ax.imshow(H[k0:k1, i0:i1], extent=(i0 * SX, i1 * SX, k0 * SZ, k1 * SZ), origin="lower", cmap="terrain", vmin=270, vmax=292)
    t = TREES[(TREES[:, 0] > x0) & (TREES[:, 0] < x1) & (TREES[:, 1] > z0) & (TREES[:, 1] < z1)]; ax.plot(t[:, 0], t[:, 1], ".", color="darkgreen", ms=1)
    for w in WATER: ax.fill(w[:, 0], w[:, 1], color="deepskyblue", alpha=0.5)
    for s in STREAMS: ax.plot(s[:, 0], s[:, 1], "b-", lw=1)
    for r in ROADS: ax.plot(r[:, 0], r[:, 1], "k-", lw=2)
    ax.plot(np.r_[LINE[:, 0], LINE[0, 0]], np.r_[LINE[:, 1], LINE[0, 1]], "-", color="saddlebrown", lw=2)
    p1 = np.array(route["official_shortcuts"][0]["path"]).reshape(-1, 2); ax.plot(p1[:, 0], p1[:, 1], "-", color="purple", lw=1.5); ax.text(p1[40, 0], p1[40, 1] + 6, "P1", color="purple", fontsize=10)
    for cp in CPS: ax.text(cp["pos"][0] + 5, cp["pos"][1], f"CP{cp['id']}", color="green", fontsize=9, weight="bold")
    for c, r in zip(C, rows):
        P = np.array(c["path"]); ax.plot(P[:, 0], P[:, 1], "-", color="magenta", lw=2.5); ax.text(P[0, 0] + 6, P[0, 1] + 6, f"{c['id']}  saves {r['saved']} m", color="magenta", fontsize=10, weight="bold")
    ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal"); ax.set_title("Noonday Creek: shortcut candidates N1 to N5 (magenta), P1 (purple), the lap (brown)")
    fig.savefig(OUT_PNG, dpi=90, bbox_inches="tight"); print("wrote", OUT_JSON, "and", OUT_PNG)

if __name__ == "__main__":
    main()
