"""Round 24 Part A3, item B3 (report and markers only, builds nothing): NEW reverse-only shortcut candidates for the Noonday Creek
reverse race, in other places than the forward ones (P1, N2, N5) and their reverse twins (RA is P1 backwards, RC is N2 backwards).

Dan (2026-10-13): "are the shortcuts on the trail the same both ways? ... I would want the trails to feel different". Each candidate
is held to CLAUDE.md "Shortcut design" (harder than the trail, a real saving when clean, a fumble costs more, never cuts too much,
fun and interesting, a hint at the turn-off, no easier free way through the same ground). Reverse-only by its take-off: each lip
faces the reverse lap and is a step down from higher ground (forward, it is a bank to climb).

What the trail allows: it is two long, nearly straight legs (the west leg beside Noonday Creek's gully, the east leg on the plateau)
joined by the hairpin in the north and the south loop. Away from those two places no cut between two points of the lap saves more
than about 15 m (0.5 s), so a new reverse shortcut that saves real time has to be in the hairpin's or the south loop's stretch, on
a line and a feature of its own. Two such lines are proposed (RD, RE); a third and fourth would repeat a place or save under 1 s.

Writes Data/proposals/round24_creek_reverse.json (TEMPORARY markers: MapBuilder.Proposals.cs, Debug/ProposalMarkers.cs; labels by
Tools/handmade/make_proposal_labels.py) and local/round24a3/creek_reverse_candidates.png / .txt (git-ignored: houses in view).
Usage: python Tools/mapgen/creek_reverse_candidates.py
"""
import json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
raw = open(os.path.join(ROOT, "Data/trees.bin"), "rb").read()
TREES = np.frombuffer(raw, dtype=np.uint8, offset=8).reshape(-1, 12)[:, :8].copy().view("<f4").reshape(-1, 2).astype(np.float64)
track = json.load(open(os.path.join(ROOT, "Data/tracks/noonday_creek.json")))
route = json.load(open(os.path.join(ROOT, "Data/routes/noonday_creek.json")))
LINE = np.array(track["line"]).reshape(-1, 2); N = len(LINE); L = float(route["lap_length_m"])
CLEAR = np.array(track["clear_half"])
OUT_JSON = os.path.join(ROOT, "Data/proposals/round24_creek_reverse.json")
OUT_TXT = os.path.join(ROOT, "local/round24a3/creek_reverse_candidates.txt"); OUT_PNG = os.path.join(ROOT, "local/round24a3/creek_reverse_candidates.png")
TRAIL_PACE = 19.5   # the AI's lap pace on the trail (m/s), as Round 21's candidates
VEHICLES = {"atv": ("Trail Four", 31.6), "moto": ("Needle 600", 33.1), "scrambler": ("Ridge Scrambler", 32.1), "drifter": ("Drifter Twin", 31.6), "mower": ("Turf Rocket", 33.1)}
ACCEL = 6.5

def fwd_s(q):
    d = np.linalg.norm(LINE - np.asarray(q), axis=1); k = int(np.argmin(d)); return k, float(d[k])
def rev_s(sf): return (L - sf) % L
def length(P): P = np.asarray(P); return float(np.sum(np.linalg.norm(np.diff(P, axis=0), axis=1)))
def resample(P, step=1.0):
    P = np.asarray(P, float); c = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    s = np.arange(0, c[-1] + 1e-6, step); return np.stack([np.interp(s, c, P[:, 0]), np.interp(s, c, P[:, 1])], 1)
def unit(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def arc(P, d, y0, v, th):
    """an aimed launch from P (height y0) along d at v, th degrees: (air s, distance m, top m over the lip, landing point)"""
    vx, vy = v * math.cos(math.radians(th)), v * math.sin(math.radians(th)); t = 0.0; top = y0
    while t < 6.0:
        t += 0.01; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        if t > 0.1 and y <= ground(*q): return t, float(vx * t), top - y0, q
    return t, float(vx * t), top - y0, q
def trunks_on(P, half):
    out = set()
    for q in resample(P, 1.0):
        d = np.linalg.norm(TREES - q, axis=1)
        for i in np.nonzero(d < half)[0]:
            k, dl = fwd_s(TREES[i])
            if dl > CLEAR[k]: out.add(int(i))
    return len(out)
def lip_speed(run_m, v_in): return {k: min(top, math.sqrt(v_in * v_in + 2 * ACCEL * run_m)) for k, (nm, top) in VEHICLES.items()}
def profile(a, b, step=2.0):
    a, b = np.asarray(a, float), np.asarray(b, float); n = max(2, int(np.linalg.norm(b - a) / step) + 1)
    return [round(ground(*(a + (b - a) * u)), 1) for u in np.linspace(0, 1, n)]

CPS = route["checkpoints"]
def rev_cp_number(cid):
    gate = route["gate_s"]; order = sorted(CPS, key=lambda c: ((L - c["s"]) - (L - gate)) % L)
    return 1 + [c["id"] for c in order].index(cid)

cands = []
def candidate(cid, name, look, hint, leave_k, jumps, join_k, path, note, fumble, free_way, run_m, v_in):
    lap = (leave_k - join_k) % L; line = length(path)
    skipped = [c for c in CPS if ((leave_k - c["s"]) % L) < lap]
    flights = []
    for (lip, d, h, th, vmin, vmax) in jumps:
        y0 = ground(*lip) + h
        flights.append({"lip": np.asarray(lip), "dir": unit(d), "h": h, "th": th, "vmin": vmin, "vmax": vmax,
                        "arcs": {v: arc(np.asarray(lip), unit(d), y0, v, th) for v in sorted({vmin, vmax})}})
    t_line = line / max(12.0, 0.75 * min(f["vmin"] for f in flights)); t_lap = lap / TRAIL_PACE
    cands.append({"id": cid, "name": name, "look": look, "hint": hint, "leave_f": leave_k, "join_f": join_k, "leave_r": rev_s(leave_k), "join_r": rev_s(join_k),
                  "lap": lap, "line": line, "saves_m": lap - line, "saves_s": t_lap - t_line, "skipped": skipped, "flights": flights, "path": np.asarray(path),
                  "note": note, "fumble": fumble, "free_way": free_way, "trunks": trunks_on(path, 3.0), "lipspeed": lip_speed(run_m, v_in), "run_m": run_m, "v_in": v_in})

# ------------------------------------------------------------------------------------------------ RD: the C4 gully leap (south loop)
# Coming south down the loop's west side (reverse), 25 m before the trail bends down to the C4 ford, a kicker on the gully's west rim
# aimed east across Noonday Creek's gully (about 16 m across, 3.7 m deep, banks too steep to drive) onto the flat east bank, then
# left onto the east side's trail 6 m before CP4 (not skipped: forgives nothing). Skips the ford and the loop's bottom.
kD0, kD1 = 1640, 1490
uD = unit(LINE[kD1] - LINE[kD0])
lipD = LINE[kD0] + uD * 6.0
landD_far = LINE[kD0] + uD * 40.0
pathD = list(resample([LINE[kD0 + 22], LINE[kD0 + 8], LINE[kD0], lipD, landD_far, LINE[kD1 + 3], LINE[kD1]], 2.0))
candidate("RD", "Gully leap", "a kids' kicker of planks on creek rock at the gully's west rim",
          "worn tire tracks off the trail to the kicker and a fallen 'BRIDGE OUT' board leaned on a trunk (no bridge: there never was one)",
          kD0 + 22, [(lipD, uD, 1.4, 20.0, 21.0, 23.0)], kD1, pathD,
          "The south loop's bottom: over Noonday Creek's gully instead of down to the C4 ford and round. 60 m south of N2 / RC's line, a different "
          "gully crossing; it rejoins before CP4, so CP4 is still passed.",
          "short: into the gully (shallow water, 3.7 m down; out by the C4 ford 30 m downstream, 6 to 9 s); long or off line: into the trunks on the east bank (breakable)",
          "none: the gully's banks are 50 to 60 % (no vehicle climbs out where the line crosses); the ford is the trail itself", 22.0, 14.0)

# ------------------------------------------------------------------------------------------------ RE: the flood-plain run (two jumps)
# Coming north up the east leg (reverse), before the trail bends west round the small pond, straight on west off the plateau's edge (a
# 2.5 m step down to the flood plain: the lip faces west, so it only works this way), 120 m through the flood plain's woods on a
# narrow cleared line, and a second kicker on the east rim of Noonday Creek's gorge up onto the west leg's higher bank. 40 to 50 m
# south of P1 / RA (over the pond and through the pipe): skips the same hairpin, so it would replace RA rather than sit beside it.
kE0, kE1 = 700, 300
uE = unit(LINE[kE1] - LINE[kE0])
lipE1 = LINE[kE0] + uE * 30.0                  # the plateau's edge
k_g = 158.0                                    # the gorge's east rim, along the line
lipE2 = LINE[kE0] + uE * k_g
pathE = list(resample([LINE[kE0 + 20], LINE[kE0 + 6], LINE[kE0], lipE1, lipE2, LINE[kE0] + uE * (k_g + 26.0), LINE[kE1 - 2], LINE[kE1 - 12]], 2.0))
candidate("RE", "Flood-plain run", "a step-down off the plateau's edge (an earth lip), then a log kicker at the gorge's east rim",
          "a gap in the plateau's trees with old tire tracks into it, and a hand-painted 'NO BIKES' sign knocked flat",
          kE0 + 20, [(lipE1, uE, 0.8, 12.0, 18.0, 22.0), (lipE2, uE, 1.6, 22.0, 17.0, 19.0)], kE1 - 12, pathE,
          "South of the small pond: off the plateau and across the flood plain, over Noonday Creek's gorge onto the west leg. Skips the hairpin "
          "(as P1 / RA do) on a line of its own; forgives the hairpin checkpoint (as RA).",
          "the step-down is forgiving (a drop onto flat ground); short at the gorge: into the creek (1.6 m down, out downstream on the west bank, 6 to 10 s); "
          "slow through the woods: the line's trunks are breakable",
          "the flood plain is open to anyone, but the gorge stops a vehicle without the kicker (its west bank stands 1.5 m over the east rim, 68 %)", 40.0, 14.0)

# ------------------------------------------------------------------------------------------------ report
lines = ["Round 24 A3, B3: NEW reverse-only shortcut candidates, Noonday Creek reverse race (markers only, nothing built). Tools/mapgen/creek_reverse_candidates.py",
         f"Reverse lap {L:.0f} m. 'rev' = meters along the reverse lap, 'fwd' = the files' forward stations.",
         "Why two: the trail is two long straight legs joined by the hairpin and the south loop; elsewhere no cut saves more than about 15 m.", ""]
for c in cands:
    cps = ", ".join(f"reverse CP{rev_cp_number(k['id'])} ({k['label']})" for k in c["skipped"]) or "none"
    lines.append(f"{c['id']} {c['name']}: leaves the reverse lap at rev {c['leave_r']:.0f} (fwd {c['leave_f']}), rejoins at rev {c['join_r']:.0f} (fwd {c['join_f']}); "
                 f"skips {c['lap']:.0f} m of lap for {c['line']:.0f} m of line (saves {c['saves_m']:.0f} m, about {c['saves_s']:.1f} s clean); forgives {cps}")
    for i, f in enumerate(c["flights"]):
        lines.append(f"   jump {i + 1}: lip {f['h']} m at {f['th']:.0f} deg, aimed launch {f['vmin']:.0f} to {f['vmax']:.0f} m/s; arcs: " +
                     "; ".join(f"{v:.0f} m/s {a[0]:.2f} s in the air, {a[1]:.0f} m, {a[2]:.1f} m up" for v, a in f["arcs"].items()))
    lines.append(f"   look: {c['look']}")
    lines.append(f"   per vehicle (speed at the first lip after a {c['run_m']:.0f} m run-up from {c['v_in']:.0f} m/s): " +
                 ", ".join(f"{VEHICLES[k][0]} {v:.0f} m/s" for k, v in c["lipspeed"].items()))
    lines.append(f"   in the way: {c['trunks']} trunks within 3 m of the line (beyond the trail's clearing); hint: {c['hint']}")
    lines.append(f"   fumble: {c['fumble']}")
    lines.append(f"   free way: {c['free_way']}")
    lines.append(f"   {c['note']}")
    lines.append("")
lines.append("RD ground along its jump (2 m steps from the trail): " + str(profile(LINE[kD0], landD_far)))
lines.append("RE ground along its line (2 m steps from the trail): " + str(profile(LINE[kE0], LINE[kE0] + uE * (k_g + 18.0))))
os.makedirs(os.path.dirname(OUT_TXT), exist_ok=True)
open(OUT_TXT, "w", encoding="utf-8").write("\n".join(lines)); print("\n".join(lines))

# ------------------------------------------------------------------------------------------------ markers (as Round 21 Part C's)
marks, plines, starts = [], [], []
for c in cands:
    P = c["path"]
    plines.append({"set": "C", "name": f"{c['id']} {c['name']}", "points": [round(float(v), 2) for q in P for v in q], "spacing": 12, "color": [1.0, 0.2, 0.8]})
    marks.append({"set": "C", "kind": "shortcut", "title": f"{c['id']} {c['name'].upper()}", "text": f"reverse only, saves {c['saves_m']:.0f} m, {c['saves_s']:.1f} s", "x": round(float(P[0][0]), 2), "z": round(float(P[0][1]), 2)})
    for i, f in enumerate(c["flights"]):
        lip = f["lip"]; land = f["arcs"][f["vmin"]][3]
        tag = f"{c['id']} TAKE-OFF" + (f" {i + 1}" if len(c["flights"]) > 1 else "")
        marks.append({"set": "C", "kind": "jump", "title": tag, "text": f"{f['h']} m lip, {f['vmin']:.0f}-{f['vmax']:.0f} m/s", "x": round(float(lip[0]), 2), "z": round(float(lip[1]), 2)})
        marks.append({"set": "C", "kind": "checkpoint", "title": f"{c['id']} LANDS" + (f" {i + 1}" if len(c["flights"]) > 1 else ""), "text": f"{f['arcs'][f['vmin']][0]:.1f} s in the air", "x": round(float(land[0]), 2), "z": round(float(land[1]), 2)})
    back = LINE[(c["leave_f"] + 60) % N]; fwdv = LINE[c["leave_f"]] - back
    starts.append({"name": f"{c['id']} {c['name']} (reverse)", "x": round(float(back[0]), 2), "z": round(float(back[1]), 2), "yaw": round(math.degrees(math.atan2(fwdv[0], fwdv[1])), 1)})
os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
json.dump({"note": "Round 24 A3, B3: new reverse-only Noonday Creek shortcut candidates (TEMPORARY markers, nothing built). Made by Tools/mapgen/creek_reverse_candidates.py.",
           "lines": plines, "marks": marks, "starts": starts}, open(OUT_JSON, "w", encoding="utf-8"), indent=1)
print("wrote", OUT_JSON)

# ------------------------------------------------------------------------------------------------ overhead
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
fig, ax = plt.subplots(figsize=(12, 26))
x0, x1, z0, z1 = 2270, 2590, 420, 1420
xs = np.arange(x0, x1, 2.0); zs = np.arange(z0, z1, 2.0); G = np.array([[ground(x, z) for x in xs] for z in zs])
ax.imshow(G, extent=(x0, x1, z0, z1), origin="lower", cmap="terrain", vmin=272, vmax=290)
t = TREES[(TREES[:, 0] > x0) & (TREES[:, 0] < x1) & (TREES[:, 1] > z0) & (TREES[:, 1] < z1)]; ax.plot(t[:, 0], t[:, 1], ".", color="darkgreen", ms=1.5)
def near(q): return q[:, 0].max() > x0 and q[:, 0].min() < x1 and q[:, 1].max() > z0 and q[:, 1].min() < z1
for w in m["water"]:
    q = np.array(w["outline"]).reshape(-1, 2)
    if near(q): ax.fill(q[:, 0], q[:, 1], color="deepskyblue", alpha=0.6)
for w in m["streams"]:
    q = np.array(w["points"]).reshape(-1, 2)
    if near(q): ax.plot(q[:, 0], q[:, 1], "b-", lw=0.8)
for r in m["roads"] + m["bridges"]:
    q = np.array(r["points"]).reshape(-1, 2)
    if near(q): ax.plot(q[:, 0], q[:, 1], "k-", lw=3, alpha=0.6)
for b in m["buildings"]:
    q = np.array(b["footprint"]).reshape(-1, 2)
    if near(q): ax.fill(q[:, 0], q[:, 1], color="peru")
ax.plot(LINE[:, 0], LINE[:, 1], "-", color="saddlebrown", lw=2)
for s in range(0, N, 50):
    dd = LINE[(s - 3) % N] - LINE[(s + 3) % N]; ax.arrow(LINE[s][0], LINE[s][1], dd[0] * 1.5, dd[1] * 1.5, head_width=4, color="red")
for c in CPS: ax.plot(*c["pos"], "g^", ms=10); ax.text(c["pos"][0] + 5, c["pos"][1] + 3, f"rev CP{rev_cp_number(c['id'])}", color="darkgreen", fontsize=10, weight="bold")
for sc in route["official_shortcuts"]:   # the shortcuts there now, thin, for comparison
    q = np.array(sc["path"]).reshape(-1, 2); ax.plot(q[:, 0], q[:, 1], "--", color="gray", lw=1.5); ax.text(q[len(q) // 2][0], q[len(q) // 2][1], sc["id"].split("_")[0].upper(), color="dimgray", fontsize=9)
for c in cands:
    P = c["path"]; ax.plot(P[:, 0], P[:, 1], "-", color="magenta", lw=2.5)
    for f in c["flights"]:
        lip = f["lip"]; land = f["arcs"][f["vmin"]][3]; ax.plot([lip[0], land[0]], [lip[1], land[1]], ":", color="black", lw=2); ax.plot(*lip, "ko", ms=6)
    ax.text(P[0][0] + 6, P[0][1], f"{c['id']} {c['name']}\nsaves {c['saves_m']:.0f} m", color="magenta", fontsize=11, weight="bold")
ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal")
ax.set_title("Reverse Noonday Creek: NEW reverse-only candidates (magenta), flights (dotted), the shortcuts there now (gray dashed), reverse travel (red arrows)")
fig.savefig(OUT_PNG, dpi=60, bbox_inches="tight"); print("wrote", OUT_PNG)
