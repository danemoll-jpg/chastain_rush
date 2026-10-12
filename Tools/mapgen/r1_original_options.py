"""Round 20, item 3 (report only, builds nothing): the ORIGINAL R1 line on the reverse loop with a house slightly altered.

Dan (2026-10-11): "Can we suggest going with the original idea for the first jump on the reverse slightly altering the house so
that it will work?" The original R1 (Round 18's markers): a kicker on the natural bank between houses 1987 and 1552, its lip at
(2801, 1035), flying west-north-west toward the gap between 2065 and 1572, rejoining the reverse lap at (2737, 1071). Round 18
found it unsafe: vehicles reach the kicker at 24 to 28 m/s and the flight and the run-on after it cross that gap at an angle,
into 1572's (or 2065's) wall.

This tests every speed a vehicle can arrive at (14 to 32 m/s), 1.5 m and 6 degrees off the line, for the line as it is and for
house changes, and finds the smallest change of each kind that makes every case safe:
  flight   g 9.81, no drag (as the game); the launch is aimed as BigJump.cs does it (the lip's angle, half the sideways drift
           taken out, no faster than 'vmax'), so above vmax every vehicle flies as at vmax
  safe     no point of the flight within 0.9 m (half a vehicle) of a house below its ridge + 1 m, and after touching down 30 m
           of straight run-on (no steering yet) that stays 0.9 m clear of every house wall
Writes local/round20/r1_options.png (overhead of each option, git-ignored) and prints the tables.

Usage: python Tools/mapgen/r1_original_options.py
"""
import json, math, os
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
meta = json.load(open(os.path.join(ROOT, "Assets/Generated/terrain.json")))
H = np.fromfile(os.path.join(ROOT, "Assets/Generated/terrain.r16"), dtype="<u2").reshape(meta["grid_z"], meta["grid_x"]).astype(np.float64)
H = H / 65535.0 * (meta["max_height"] - meta["min_height"]) + meta["min_height"]
SX = meta["size_x_m"] / (meta["grid_x"] - 1); SZ = meta["size_z_m"] / (meta["grid_z"] - 1)
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
route = json.load(open(os.path.join(ROOT, "Data/routes/chastain_glen_loop.json"), encoding="utf-8"))
OUT = os.path.join(ROOT, "local/round20")

def ground(x, z):
    fx, fz = x / SX, z / SZ; i, j = int(fx), int(fz); a, b = fx - i, fz - j
    return float(H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b)
V = lambda x, z: np.array([x, z], float)
def unit(v): return v / np.linalg.norm(v)
def left(d): return np.array([-d[1], d[0]])
def rot(v, a): c, s = math.cos(a), math.sin(a); return np.array([v[0] * c - v[1] * s, v[0] * s + v[1] * c])

# the kicker's lip in the middle of the north end of the 1987 / 1552 gap (between 1987's north corner and 1552's west corner),
# aimed through the middle of the 2065 / 1572 gap (Round 18's markers put the lip at (2801, 1035), 1.3 m from 1552's corner)
LIP = (V(2795.4, 1031.6) + V(2802.1, 1035.7)) / 2; GAP = (V(2764.9, 1054.8) + V(2760.0, 1064.65)) / 2; REJOIN = V(2737.0, 1071.0)
D0 = unit(GAP - LIP)
GAP_OUT = V(2757.0, 1070.0)   # the 2065 / 1572 gap's north-west end, toward the lap
EAVE = 6.3                                   # two storeys and the floor (as make_shortcuts.py's roof model)
NEAR = [i for i, b in enumerate(m["buildings"]) if np.linalg.norm(np.array(b["footprint"]).reshape(-1, 2).mean(0) - V(2775, 1050)) < 70]

def footprint(i): return np.array(m["buildings"][i]["footprint"], float).reshape(-1, 2)
def ground_v(Q):
    fx, fz = Q[:, 0] / SX, Q[:, 1] / SZ; i, j = fx.astype(int), fz.astype(int); a, b = fx - i, fz - j
    return H[j, i] * (1 - a) * (1 - b) + H[j, i + 1] * a * (1 - b) + H[j + 1, i] * (1 - a) * b + H[j + 1, i + 1] * a * b
def dist_poly(Q, P):
    """Distance from each point of Q (n x 2) to polygon P's outline, negative inside."""
    Q = np.atleast_2d(Q); best = np.full(len(Q), 1e9); inside = np.zeros(len(Q), bool)
    for k in range(len(P)):
        a, b = P[k], P[(k + 1) % len(P)]; ab = b - a; t = np.clip((Q - a) @ ab / (ab @ ab), 0, 1)
        best = np.minimum(best, np.linalg.norm(a + t[:, None] * ab - Q, axis=1))
        if a[1] != b[1]:
            cross = (a[1] > Q[:, 1]) != (b[1] > Q[:, 1]); xi = a[0] + (Q[:, 1] - a[1]) / (b[1] - a[1]) * (b[0] - a[0])
            inside ^= cross & (Q[:, 0] < xi)
    return np.where(inside, -best, best)

def houses(change=None):
    """[(index, footprint, ridge y)] round the line, with one house changed: change(index, footprint, ridge) -> (footprint, ridge)."""
    out = []
    for i in NEAR:
        f = footprint(i); c = f.mean(0); g = ground(*c); ridge = g + m["buildings"][i]["height"]
        if change: f, ridge = change(i, f, ridge)
        if f is not None: out.append((i, f, ridge, g))
    return out

STEER = False   # Round 20: False = 30 m straight on after landing (no steering yet, the strict test used for the options)
def fly(hs, lat, err, v, h=1.2, th=15.0, vmax=99.0, runon=30.0):
    """One case: the line moved 'lat' m sideways and turned 'err' degrees. Returns (ok, why, flight distance, air, top, path)."""
    d = rot(D0, math.radians(err * 0.5))        # BigJump takes out half the sideways drift
    P = LIP + left(D0) * lat; y0 = ground(*P) + h; s = min(v, vmax)
    vx, vy = s * math.cos(math.radians(th)), s * math.sin(math.radians(th))
    t = np.arange(0.01, 6.0, 0.01); Q = P + np.outer(vx * t, d); Y = y0 + vy * t - 4.905 * t * t
    down = np.nonzero((t > 0.1) & (Y <= ground_v(Q)))[0]; n = down[0] + 1 if len(down) else len(t)
    Q, Y, t = Q[:n], Y[:n], t[:n]; top = float(Y.max() - y0); dist = float(vx * t[-1]); air = float(t[-1])
    path = [P] + list(Q[::10]) + [Q[-1]]
    for i, f, ridge, g in hs:
        hit = np.nonzero((Y < ridge + 1.0) & (dist_poly(Q, f) < 0.9) & (t > 0.15))[0]   # the kicker's own sides stand between 1987 and 1552
        if len(hit): return False, f"flight into {i}", dist, air, top, path
    if not STEER:
        R = Q[-1] + np.outer(np.arange(0.5, runon, 0.5), d); path += list(R[::4])
        for i, f, ridge, g in hs:
            hit = np.nonzero(dist_poly(R, f) < 0.9)[0]
            if len(hit): return False, f"lands {dist:.0f} m out, runs into {i}'s wall {0.5 * (hit[0] + 1):.0f} m on", dist, air, top, path
        return True, "", dist, air, top, path
    # after touching down the driver steers for the gap's far end, then the lap (grip 9 m/s2 sideways, braking 6 m/s2 down to
    # 15 m/s while the heading is more than 10 degrees off): the line it drives must stay 0.9 m clear of every wall
    p = Q[-1].copy(); h = d.copy(); sp = vx; R = []; tgt = [GAP, GAP_OUT, REJOIN]; k = 0
    for _ in range(int(4.0 / 0.02)):
        while k < len(tgt) - 1 and (np.linalg.norm(tgt[k] - p) < 4.0 or (tgt[k] - p) @ h < 0): k += 1   # reached, or already past it
        if np.linalg.norm(tgt[k] - p) < 4.0:
            k += 1
            if k == len(tgt): break
        want = unit(tgt[k] - p); ang = math.atan2(h[0] * want[1] - h[1] * want[0], h @ want)
        turn_ = np.clip(ang, -9.0 / max(sp, 1.0) * 0.02, 9.0 / max(sp, 1.0) * 0.02); h = rot(h, turn_)
        if abs(ang) > math.radians(10) and sp > 15.0: sp = max(15.0, sp - 6.0 * 0.02)
        p = p + h * sp * 0.02; R.append(p.copy())
    R = np.array(R); path += list(R[::4])
    for i, f, ridge, g in hs:
        hit = np.nonzero(dist_poly(R, f) < 0.9)[0]
        if len(hit): return False, f"lands {dist:.0f} m out, steers, into {i}'s wall {0.02 * sp * (hit[0] + 1):.0f} m on", dist, air, top, path
    return True, "", dist, air, top, path

def cases(hs, vmax=99.0, h=1.2, th=15.0, speeds=range(14, 33)):
    bad = []; res = []
    for v in speeds:
        for lat in (-1.5, 0.0, 1.5):
            for err in (-6.0, 0.0, 6.0):
                ok, why, dist, air, top, _ = fly(hs, lat, err, v, h, th, vmax)
                res.append((v, lat, err, ok, dist, air, top))
                if not ok: bad.append((v, lat, err, why))
    return bad, res

def summary(name, hs, vmax, h=1.2, th=15.0):
    bad, res = cases(hs, vmax, h, th)
    on = [r for r in res if r[1] == 0 and r[2] == 0]
    lo, hi = on[0], max(on, key=lambda r: r[4])
    print(f"{name}: lip {h} m, {th:.0f} deg, held to {vmax:.0f} m/s: {'SAFE at every speed and line' if not bad else f'{len(bad)} of {len(res)} cases unsafe'}; "
          f"air {lo[5]:.2f} to {hi[5]:.2f} s, {lo[4]:.0f} to {hi[4]:.0f} m, top {hi[6]:.1f} m over the lip")
    for b in bad[:6]: print(f"    {b[0]} m/s, {b[1]:+.1f} m, {b[2]:+.0f} deg: {b[3]}")
    return bad

# ---------------------------------------------------------------- the house changes
def move(idx, off):          # move house idx by the vector off (m)
    return lambda i, f, r: (f + off, r) if i == idx else (f, r)
def turn(idx, pivot, deg):   # turn house idx about a point
    a = math.radians(deg); return lambda i, f, r: (np.array([pivot + rot(p - pivot, a) for p in f]), r) if i == idx else (f, r)
def clip(P, a, n):           # keep the part of polygon P where (p - a) . n >= 0
    out = []
    for k in range(len(P)):
        p, q = P[k], P[(k + 1) % len(P)]; sp, sq = (p - a) @ n, (q - a) @ n
        if sp >= 0: out.append(p)
        if (sp >= 0) != (sq >= 0): out.append(p + (q - p) * (sp / (sp - sq)))
    return np.array(out)
def cut(idx, a, n):          # cut house idx back to a wall through a with outward normal -n
    return lambda i, f, r: (clip(f, a, n), r) if i == idx else (f, r)
def lower(idx, ridge_m):     # a lower roof on house idx (ridge this many m over its ground)
    return lambda i, f, r: (f, r - m["buildings"][idx]["height"] + ridge_m) if i == idx else (f, r)
def both(*ch):
    def f_(i, f, r):
        for c in ch: f, r = c(i, f, r)
        return f, r
    return f_

def smallest(name, make, values, vmax, h=1.2, th=15.0):
    for x in values:
        hs = houses(make(x)); bad, _ = cases(hs, vmax, h, th)
        if not bad: summary(f"  {name(x)}", hs, vmax, h, th); return x
    print(f"  {name(values[-1])} and less: still unsafe at {vmax} m/s"); return None

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    print(f"lip ({LIP[0]:.1f}, {LIP[1]:.1f}), heading {math.degrees(math.atan2(D0[0], D0[1])):.0f} deg, through ({GAP[0]:.1f}, {GAP[1]:.1f})")
    base = houses()
    print()
    print("--- as it is")
    for vm in (99, 26, 24, 22, 20, 18): summary("  no change", base, vm)
    gap_axis = unit(V(2756.6, 1064.5) - V(2761.0, 1054.7)); n_gap = left(gap_axis)
    away1572 = n_gap if (V(2771.4, 1066.3) - GAP) @ n_gap > 0 else -n_gap
    A_, B_ = V(2768.2, 1072.1), V(2770.3, 1068.4); nw = unit(left(B_ - A_)); nw = nw if (V(2775, 1063) - A_) @ nw > 0 else -nw
    for vm in (22, 24, 26):
        print()
        print(f"=== held to {vm} m/s")
        smallest(lambda x: f"A 1572 moved {x:.1f} m north-east (away from the gap)", lambda x: move(1572, away1572 * x), np.arange(1.0, 15.1, 1.0), vm)
        smallest(lambda x: f"B 1572 turned {x:+.0f} deg about its north corner", lambda x: turn(1572, V(2775.1, 1075.7), x), list(range(-2, -41, -2)), vm)
        smallest(lambda x: f"B' 1572 turned {x:+.0f} deg about its north corner", lambda x: turn(1572, V(2775.1, 1075.7), x), list(range(2, 41, 2)), vm)
        smallest(lambda x: f"C 1572 cut back {x:.1f} m past its west wing", lambda x: cut(1572, A_ + nw * x, nw), np.arange(0.0, 12.1, 1.0), vm)
        smallest(lambda x: f"E 2065 moved {x:.1f} m south-west", lambda x: move(2065, -away1572 * x), np.arange(1.0, 15.1, 1.0), vm)
        smallest(lambda x: f"A+E both moved {x:.1f} m apart", lambda x: both(move(1572, away1572 * x), move(2065, -away1572 * x)), np.arange(1.0, 10.1, 1.0), vm)


def picture(options, out=os.path.join(OUT, "r1_options.png")):
    """Per option: an overhead (houses, the changed one in red with its old outline dashed, the flights on the line and off it)
    and a view from the approach (houses as boxes with their roofs, the flight drawn), side by side."""
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    from mpl_toolkits.mplot3d.art3d import Poly3DCollection
    fig = plt.figure(figsize=(16, 7.2 * len(options)))
    for row, (title, change, vmax, changed) in enumerate(options):
        hs = houses(change); base = {i: f for i, f, _, _ in houses()}
        ax = fig.add_subplot(len(options), 2, 2 * row + 1)
        for i, f, ridge, g in hs:
            ax.fill(f[:, 0], f[:, 1], color="indianred" if i in changed else "peru"); ax.text(*f.mean(0), str(i), fontsize=8, ha="center")
            if i in changed: b = np.vstack([base[i], base[i][:1]]); ax.plot(b[:, 0], b[:, 1], "k--", lw=1)
        loop = np.array(route["loop"]).reshape(-1, 2); ax.plot(loop[:, 0], loop[:, 1], "c-", lw=3, alpha=.5)
        for lat in (-1.5, 0.0, 1.5):
            for err in (-6.0, 0.0, 6.0):
                for v in (14, vmax):
                    ok, why, dist, air, top, path = fly(hs, lat, err, v, vmax=vmax); P = np.array(path)
                    ax.plot(P[:, 0], P[:, 1], "-", color="green" if ok else "red", lw=0.8 if (lat or err) else 2)
        ax.plot(*LIP, "k^", ms=10); ax.set_xlim(2725, 2815); ax.set_ylim(1015, 1085); ax.set_aspect("equal"); ax.grid(alpha=.3)
        ax.set_title(f"{title}: overhead (green: safe, red: not; 14 and {vmax} m/s, 1.5 m and 6 deg off)", fontsize=10)
        ax3 = fig.add_subplot(len(options), 2, 2 * row + 2, projection="3d")
        for i, f, ridge, g in hs:
            top = g + EAVE; c = "indianred" if i in changed else "burlywood"
            walls = [[(f[k][0], f[k][1], g), (f[(k + 1) % len(f)][0], f[(k + 1) % len(f)][1], g), (f[(k + 1) % len(f)][0], f[(k + 1) % len(f)][1], top), (f[k][0], f[k][1], top)] for k in range(len(f))]
            ax3.add_collection3d(Poly3DCollection(walls, facecolor=c, edgecolor="k", lw=0.3, alpha=0.95))
            cen = f.mean(0); roof = [[(f[k][0], f[k][1], top), (f[(k + 1) % len(f)][0], f[(k + 1) % len(f)][1], top), (cen[0], cen[1], ridge)] for k in range(len(f))]
            ax3.add_collection3d(Poly3DCollection(roof, facecolor="dimgray", edgecolor="k", lw=0.3))
        for v in (14, vmax):
            d = D0; y0 = ground(*LIP) + 1.2; s = min(v, vmax); vx, vy = s * math.cos(math.radians(15)), s * math.sin(math.radians(15))
            T = np.arange(0, 2.5, 0.02); Q = LIP + np.outer(vx * T, d); Y = y0 + vy * T - 4.905 * T * T
            keep = Y >= np.array([ground(*q) for q in Q]) - 0.05; ax3.plot(Q[keep, 0], Q[keep, 1], Y[keep], "g-", lw=2)
        ax3.set_xlim(2735, 2805); ax3.set_ylim(1020, 1080); ax3.set_zlim(275, 300); ax3.view_init(elev=14, azim=math.degrees(math.atan2(-D0[1], -D0[0])) + 180 + 8)
        ax3.set_title(f"{title}: from the approach (behind the kicker, looking along the flight)", fontsize=10); ax3.set_axis_off()
    fig.savefig(out, dpi=70, bbox_inches="tight"); print("wrote", out)
