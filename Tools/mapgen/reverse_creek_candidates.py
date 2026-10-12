"""Round 21, Part C (report and markers only, builds nothing): shortcut candidates for the REVERSE Noonday Creek race.

Dan (2026-10-12): "I would also like to see some potential shortcuts flagged out so I can decide. As usual shortcuts should be more
challenging than just following the track but rewarding if pulled off successfully. We also don't want the shortcuts to cut off too
much of the track. Shortcuts should always be fun, and interesting". Each candidate is held to CLAUDE.md "Shortcut design".

For each: where it leaves and rejoins the reverse lap (reverse stations: meters from the reverse lap's first point, and forward
stations for the files), the lap it skips, the line's length, the time a clean run saves (the trail at the AI's lap pace, the line at
its own speeds), what a fumble costs, the arc off its take-off (an aimed launch, as BigJump: every trail vehicle that reaches the lip
fast enough flies the same arc; each vehicle's speed at the lip from its run-up), the checkpoint it would forgive, what is in the way
(trunks to clear, water, slopes), its look and its hint at the turn-off.

Writes Data/proposals/round21_markers.json (TEMPORARY markers: MapBuilder.Proposals.cs, Debug/ProposalMarkers.cs; labels by
Tools/handmade/make_proposal_labels.py) and local/round21/reverse_candidates.png / .txt (git-ignored: houses in view).
Usage: python Tools/mapgen/reverse_creek_candidates.py
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
pipes = json.load(open(os.path.join(ROOT, "Data/pipes/pipes.json")))
LINE = np.array(track["line"]).reshape(-1, 2); N = len(LINE); L = float(route["lap_length_m"])
CLEAR = np.array(track["clear_half"])
OUT_JSON = os.path.join(ROOT, "Data/proposals/round21_markers.json")
OUT_TXT = os.path.join(ROOT, "local/round21/reverse_candidates.txt"); OUT_PNG = os.path.join(ROOT, "local/round21/reverse_candidates.png")
# the AI's lap pace on the trail (Round 18 race: 3 laps in 5:21 to 5:46 = about 19.5 m/s); the trail vehicles flat out reach 31 to 33 m/s
TRAIL_PACE = 19.5
VEHICLES = {"atv": ("Trail Four", 31.6), "moto": ("Needle 600", 33.1), "scrambler": ("Ridge Scrambler", 32.1), "drifter": ("Drifter Twin", 31.6), "mower": ("Turf Rocket", 33.1)}
ACCEL = 6.5   # m/s2 on dirt, about what the exe drives show for the trail vehicles from a bend

def fwd_s(q):
    d = np.linalg.norm(LINE - np.asarray(q), axis=1); k = int(np.argmin(d)); return k, float(d[k])
def rev_s(sf): return (L - sf) % L
def rev_gap(sf_leave, sf_join):   # reverse travel goes from higher forward stations to lower ones
    return (sf_leave - sf_join) % L
def length(P): P = np.asarray(P); return float(np.sum(np.linalg.norm(np.diff(P, axis=0), axis=1)))
def resample(P, step=1.0):
    P = np.asarray(P, float); c = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    s = np.arange(0, c[-1] + 1e-6, step); return np.stack([np.interp(s, c, P[:, 0]), np.interp(s, c, P[:, 1])], 1)
def unit(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def arc(P, d, y0, v, th, extra=None):
    """an aimed launch from P (height y0) along d at v, th degrees: (air s, distance m, top m over the lip, landing point)"""
    vx, vy = v * math.cos(math.radians(th)), v * math.sin(math.radians(th)); t = 0.0; top = y0
    while t < 6.0:
        t += 0.01; q = P + d * vx * t; y = y0 + vy * t - 4.905 * t * t; top = max(top, y)
        g = ground(*q) if extra is None else max(ground(*q), extra(q))
        if t > 0.1 and y <= g: return t, float(vx * t), top - y0, q
    return t, float(vx * t), top - y0, q
def trunks_on(P, half):
    """trunks within 'half' of the line that the trail's own clearing does not already take"""
    out = set()
    for q in resample(P, 1.0):
        d = np.linalg.norm(TREES - q, axis=1)
        for i in np.nonzero(d < half)[0]:
            k, dl = fwd_s(TREES[i])
            if dl > CLEAR[k]: out.add(int(i))
    return len(out)
def water_on(P):
    n = 0
    for q in resample(P, 1.0):
        for w in m["water"]:
            o = np.array(w["outline"]).reshape(-1, 2)
            c = False
            for i in range(len(o)):
                a, b = o[i], o[(i + 1) % len(o)]
                if (a[1] > q[1]) != (b[1] > q[1]) and q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1] + 1e-12) + a[0]: c = not c
            if c: n += 1; break
    return n
def lip_speed(run_m, v_in):
    return {k: min(top, math.sqrt(v_in * v_in + 2 * ACCEL * run_m)) for k, (nm, top) in VEHICLES.items()}

CPS = route["checkpoints"]
def rev_cp_number(cid):   # the reverse race numbers the checkpoints in travel order (RaceRoute)
    gate = route["gate_s"]; order = sorted(CPS, key=lambda c: ((L - c["s"]) - (L - gate)) % L)
    return 1 + [c["id"] for c in order].index(cid)

cands = []
def candidate(cid, name, look, hint, leave_xz, lip, d, h, th, vmin, vmax, land_extra, join_xz, path, note, fumble, run_m, v_in):
    kl, _ = fwd_s(leave_xz); kj, _ = fwd_s(join_xz)
    lap = rev_gap(kl, kj); line = length(path)
    skipped = [c for c in CPS if ((kl - c["s"]) % L) < lap]
    y0 = ground(*lip) + h
    arcs = {v: arc(np.asarray(lip), unit(d), y0, v, th, land_extra) for v in sorted({vmin, vmax})}
    # a clean run: the line at about the aimed launch's speed in the air, 14 m/s through its bends; against the trail at the AI's pace
    t_line = line / max(12.0, 0.75 * vmin); t_lap = lap / TRAIL_PACE
    cands.append({"id": cid, "name": name, "look": look, "hint": hint, "leave_f": kl, "join_f": kj, "leave_r": rev_s(kl), "join_r": rev_s(kj),
                  "lap": lap, "line": line, "saves_m": lap - line, "saves_s": t_lap - t_line, "skipped": skipped, "lip": lip, "dir": unit(d), "h": h, "th": th,
                  "vmin": vmin, "vmax": vmax, "arcs": arcs, "path": np.asarray(path), "note": note, "fumble": fumble, "trunks": trunks_on(path, 3.0),
                  "water_m": water_on(path), "lipspeed": lip_speed(run_m, v_in), "run_m": run_m, "v_in": v_in})

# ------------------------------------------------------------------------------------------------ RA: the pipe dive (P1 backwards)
P1 = [p for p in pipes["pipes"] if p["id"] == "P1"][0]
A = np.array(P1["a"]); B = np.array(P1["b"]); uA = unit(B - A)
outfall = [b for b in track["big_jumps"] if b["id"] == "P1J"][0]
P_out = np.array(outfall["lip"]); y_out = outfall["lip_y"]
# a creek-rock kicker on the small pond's east shore, on the pipe's axis (where P1J's flights come down, beside them), facing back down
# the axis: over the pond onto the outfall apron (it slopes down toward the pipe from this side: a landing ramp) and into the pipe

def apron(q):   # the apron's top (from the lip down to the pipe's mouth over 7 m), else nothing
    s = float((q - B) @ uA)
    if 0.0 <= s <= 7.0 and abs(float((q - B) @ np.array([-uA[1], uA[0]]))) < 4.25: return ground(*B) + 1.5 * (s / 7.0)
    return -1e9
RA_TH, RA_VMIN, RA_VMAX, RA_H = 22.0, 23.0, 23.4, 1.4   # a tight window (aimed launch): the apron is 7 m long
def apron_hit(K, v):
    P = P_out + uA * K; t, dist, top, q = arc(P, -uA, ground(*P) + RA_H, v, RA_TH, lambda q_: apron(q_))
    return float((q - B) @ uA)   # where it comes down, meters from the pipe's mouth along the apron (0 to 7 m: on it)
RA_K = min(np.arange(25.0, 70.0, 0.5), key=lambda K: abs(apron_hit(K, RA_VMIN) - 5.5))
lipA = P_out + uA * RA_K; dA = -uA
print(f"  RA: the rock heap {RA_K:.1f} m from the apron's lip; at {RA_VMIN} m/s it comes down {apron_hit(RA_K, RA_VMIN):.1f} m from the pipe's mouth, at {RA_VMAX} m/s {apron_hit(RA_K, RA_VMAX):.1f} m (the apron is 7 m)")
kA, _ = fwd_s(np.array([2494.6, 1307.2]))   # where P1's way out meets the trail (forward); the reverse leaves the trail there
pathA = list(resample([LINE[(kA + 4) % N], LINE[kA], (2489.0, 1314.5), (2482.5, 1322.5), (2473.0, 1327.5), tuple(lipA + uA * 8.0), tuple(lipA)], 2.0)) + \
        list(resample([lipA, P_out, B, A, A - uA * 8.0, (2317.3, 1295.6), (2306.0, 1293.0), tuple(LINE[317]), tuple(LINE[305])], 2.0))
candidate("RA", "Pipe dive", "a heap of creek rock on the pond's east shore",
          "P1's pipe lights seen across the pond from the trail; worn tracks off the trail to the rock heap",
          LINE[(kA + 4) % N], lipA, dA, RA_H, RA_TH, RA_VMIN, RA_VMAX, apron, LINE[305], pathA,
          "P1 driven backwards: over the small pond, down the outfall apron into the pipe, out across the ford onto the west side. "
          "The forward P1 skips the same stretch (the hairpin).",
          "short: into the pond (wade out to the apron, 6 to 10 s); long: past the apron into the cut's end wall over the pipe's mouth (a stop, then drive in)",
          40.0, 12.0)

# ------------------------------------------------------------------------------------------------ RB: the hairpin cut
# coming west along the lake's south side toward the hairpin's ford, a log kicker on Noonday Creek's east bank 40 m before the ford, aimed
# south-west across the creek and its west bank onto the west side's trail after the hairpin
kB0 = 506; lipB = np.array(LINE[kB0 - 6]) + np.array([-3.0, -4.0]); dB = unit(np.array(LINE[330]) - lipB)
landB = lipB + dB * 40.0
pathB = list(resample([LINE[kB0 + 14], LINE[kB0], lipB, landB, LINE[328], LINE[316]], 2.0))
candidate("RB", "Hairpin cut", "a log kicker on the creek's east bank (logs on two stringers, as N5's)",
          "tire tracks off the trail at the lake's corner and a fallen 'TRAIL CLOSED' tape between two trees",
          LINE[kB0 + 14], lipB, dB, 1.6, 20.0, 21.0, 22.0, None, LINE[316], pathB,
          "Cuts the hairpin's corner: a diagonal leap over Noonday Creek instead of the ford and the bend.",
          "short: into the creek (shallow, drive up the west bank, 4 to 6 s); off line: into the trunks beside the landing (breakable)",
          45.0, 14.0)

# ------------------------------------------------------------------------------------------------ RC: N2 backwards (creek leap back)
n2 = [b for b in track["big_jumps"] if b["id"] == "N2"][0]
L2 = np.array(n2["lip"]); d2 = np.array(n2["dir"])
lipC = L2 + d2 * 27.0; dC = -d2      # on the west bank, in N2's landing (its forward landers come down on this heap's slope)
kC0 = 1688   # just past reverse CP1 (forward CP5 at 1707): RC skips only the loop's bottom
join_C = np.array([2512.08, 577.31])
def n2heap(q):   # N2's washout heap: from this side its slope falls toward the east (a landing ramp)
    s = float((q - (L2 - d2 * 8.0)) @ d2)
    if 0.0 <= s <= 8.0 and abs(float((q - L2) @ np.array([-d2[1], d2[0]]))) < 3.0: return n2["lip_y"] - (n2["height"]) * (1 - s / 8.0)
    return -1e9
pathC = list(resample([LINE[(kC0 + 12) % N], LINE[kC0], lipC - dC * 10.0, lipC, L2, L2 - d2 * 14.0, join_C, LINE[1404], LINE[1394]], 2.0))
candidate("RC", "Creek leap back", "an earth and creek-rock heap on the west bank, N2's twin",
          "N2's knocked-over barricade is across the creek in plain sight; worn tracks off the west side's trail to the heap",
          LINE[(kC0 + 12) % N], lipC, dC, 1.8, 24.0, 15.0, 17.0, n2heap, LINE[1394], pathC,
          "N2 driven backwards: over the creek's gorge from the west side onto N2's own heap (its slope is the landing), then north up the east side.",
          "short: into the creek (shallow, out up the east bank, 8 to 12 s, the Round 20 check's N2 numbers); long: over the heap into the bend's trunks",
          30.0, 12.0)

# (RD, a hop over the small pond's outflow inside the sweep round it, was looked at and dropped: it saves 6 m)

# ------------------------------------------------------------------------------------------------ report
lines = ["Round 21, Part C: shortcut candidates for the REVERSE Noonday Creek race (markers only, nothing built). Tools/mapgen/reverse_creek_candidates.py",
         f"Reverse lap {L:.0f} m. Stations: 'rev' = meters along the reverse lap from its first point, 'fwd' = the files' forward stations.", ""]
for c in cands:
    cps = ", ".join(f"reverse CP{rev_cp_number(k['id'])} ({k['label']})" for k in c["skipped"]) or "none"
    lines.append(f"{c['id']} {c['name']}: leaves the reverse lap at rev {c['leave_r']:.0f} (fwd {c['leave_f']}), rejoins at rev {c['join_r']:.0f} (fwd {c['join_f']}); "
                 f"skips {c['lap']:.0f} m of lap for {c['line']:.0f} m of line (saves {c['saves_m']:.0f} m, about {c['saves_s']:.1f} s clean); forgives {cps}")
    lines.append(f"   take-off: {c['look']}, lip {c['h']} m at {c['th']:.0f} deg, aimed launch {c['vmin']:.1f} to {c['vmax']:.1f} m/s; arcs: " +
                 "; ".join(f"{v:.1f} m/s {a[0]:.2f} s in the air, {a[1]:.0f} m, {a[2]:.1f} m up" for v, a in c["arcs"].items()))
    lines.append(f"   per vehicle (speed at the lip after a {c['run_m']:.0f} m run-up from {c['v_in']:.0f} m/s; at or over {c['vmin']:.0f} all fly the same arc): " +
                 ", ".join(f"{VEHICLES[k][0]} {v:.0f} m/s" for k, v in c["lipspeed"].items()))
    lines.append(f"   in the way: {c['trunks']} trunks within 3 m of the line (beyond the trail's clearing), {c['water_m']} m of the line over water; hint: {c['hint']}")
    lines.append(f"   fumble: {c['fumble']}")
    lines.append(f"   {c['note']}")
    lines.append("")
os.makedirs(os.path.dirname(OUT_TXT), exist_ok=True)
open(OUT_TXT, "w", encoding="utf-8").write("\n".join(lines)); print("\n".join(lines))

# ------------------------------------------------------------------------------------------------ markers (as Round 20's)
marks, plines, starts = [], [], []
for c in cands:
    P = c["path"]
    plines.append({"set": "N", "name": f"{c['id']} {c['name']}", "points": [round(float(v), 2) for q in P for v in q], "spacing": 12})
    lip = c["lip"]; land = c["arcs"][c["vmin"]][3]
    marks.append({"set": "N", "kind": "shortcut", "title": f"{c['id']} {c['name'].upper()}", "text": f"saves {c['saves_m']:.0f} m, {c['saves_s']:.1f} s", "x": round(float(P[0][0]), 2), "z": round(float(P[0][1]), 2)})
    marks.append({"set": "N", "kind": "jump", "title": f"{c['id']} TAKE-OFF", "text": f"{c['h']} m lip, {c['vmin']:.0f}-{c['vmax']:.0f} m/s", "x": round(float(lip[0]), 2), "z": round(float(lip[1]), 2)})
    marks.append({"set": "N", "kind": "checkpoint", "title": f"{c['id']} LANDS", "text": f"{c['arcs'][c['vmin']][0]:.1f} s in the air", "x": round(float(land[0]), 2), "z": round(float(land[1]), 2)})
    back = LINE[(c["leave_f"] + 60) % N]; fwdv = LINE[c["leave_f"]] - back
    starts.append({"name": f"{c['id']} {c['name']}", "x": round(float(back[0]), 2), "z": round(float(back[1]), 2), "yaw": round(math.degrees(math.atan2(fwdv[0], fwdv[1])), 1)})
os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
json.dump({"note": "Round 21, Part C: reverse Noonday Creek shortcut candidates (TEMPORARY markers, nothing built). Made by Tools/mapgen/reverse_creek_candidates.py.",
           "lines": plines, "marks": marks, "starts": starts}, open(OUT_JSON, "w", encoding="utf-8"), indent=1)
print("wrote", OUT_JSON)

# ------------------------------------------------------------------------------------------------ overhead
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
fig, ax = plt.subplots(figsize=(12, 26))
x0, x1, z0, z1 = 2270, 2590, 430, 1420
xs = np.arange(x0, x1, 2.0); zs = np.arange(z0, z1, 2.0); G = np.array([[ground(x, z) for x in xs] for z in zs])
ax.imshow(G, extent=(x0, x1, z0, z1), origin="lower", cmap="terrain", vmin=272, vmax=286)
t = TREES[(TREES[:, 0] > x0) & (TREES[:, 0] < x1) & (TREES[:, 1] > z0) & (TREES[:, 1] < z1)]; ax.plot(t[:, 0], t[:, 1], ".", color="darkgreen", ms=1.5)
for w in m["water"]: q = np.array(w["outline"]).reshape(-1, 2); ax.fill(q[:, 0], q[:, 1], color="deepskyblue", alpha=0.6)
for w in m["streams"]: q = np.array(w["points"]).reshape(-1, 2); ax.plot(q[:, 0], q[:, 1], "b-", lw=0.8)
for r in m["roads"] + m["bridges"]: q = np.array(r["points"]).reshape(-1, 2); ax.plot(q[:, 0], q[:, 1], "k-", lw=3, alpha=0.6)
for b in m["buildings"]: q = np.array(b["footprint"]).reshape(-1, 2); ax.fill(q[:, 0], q[:, 1], color="peru")
ax.plot(LINE[:, 0], LINE[:, 1], "-", color="saddlebrown", lw=2)
for s in range(0, N, 50):
    dd = LINE[(s - 3) % N] - LINE[(s + 3) % N]; ax.arrow(LINE[s][0], LINE[s][1], dd[0] * 1.5, dd[1] * 1.5, head_width=4, color="red")
for c in CPS: ax.plot(*c["pos"], "g^", ms=10); ax.text(c["pos"][0] + 5, c["pos"][1] + 3, f"rev CP{rev_cp_number(c['id'])}", color="darkgreen", fontsize=10, weight="bold")
for c in cands:
    P = c["path"]; ax.plot(P[:, 0], P[:, 1], "-", color="magenta", lw=2.5)
    lip = c["lip"]; land = c["arcs"][c["vmin"]][3]; ax.plot([lip[0], land[0]], [lip[1], land[1]], ":", color="black", lw=2)
    ax.plot(*lip, "ko", ms=6); ax.text(P[0][0] + 6, P[0][1], f"{c['id']} {c['name']}\nsaves {c['saves_m']:.0f} m", color="magenta", fontsize=11, weight="bold")
ax.set_xlim(x0, x1); ax.set_ylim(z0, z1); ax.set_aspect("equal")
ax.set_title("Reverse Noonday Creek: shortcut candidates (magenta), flights (dotted), reverse travel (red arrows)")
fig.savefig(OUT_PNG, dpi=60, bbox_inches="tight"); print("wrote", OUT_PNG)
