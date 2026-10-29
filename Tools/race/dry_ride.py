"""Round 83: ride the lines of a planning dry run (MapBuilder.R83Dry.cs writes <out>.surf.tsv) both ways with a point mass at each line's cap.

The car rides the highest surface it can reach (the ground or a deck top no higher than 0.6 m over where it is); a surface above that within a
car's height is a wall or a low slab. Faults: in the air over 0.3 s, a landing over 6 m/s (vertical, against the surface), a step up over 0.25 m in
0.5 m, a slab or face under 1.6 m over the surface. A forecast for choosing what to fix before a full Build Map; the exe check is the proof.

Usage: python Tools/race/dry_ride.py <lines.json> <dry.surf.tsv> [--only text] [--profile text] [--brief]
"""
import json
import sys

G = 9.81


def load(lines_path, surf_path):
    lines = json.load(open(lines_path, encoding="utf-8"))["lines"]
    pts = {}
    with open(surf_path, encoding="utf-8") as f:
        next(f)
        for row in f:
            c = row.rstrip("\n").split("\t")
            li = int(c[0])
            decks = []
            if len(c) > 5 and c[5]:
                for d in c[5].split("|"):
                    t, b = d.split("/")
                    decks.append((float(t), float(b)))
            pts.setdefault(li, []).append((float(c[2]), float(c[3]), float(c[4]), decks))
    return lines, pts


def ride(P, v):
    n = len(P)
    S = [0.0] * n
    walls, steps = [], []
    prev = None
    for k, (x, z, g, decks) in enumerate(P):
        cands = [g] + [t for t, b in decks]
        if prev is None:
            pick = g   # (main() starts each ride at its first point with no deck over it)
        else:
            reach = prev + 0.6
            under = [y for y in cands if y <= reach]
            pick = max(under) if under else min(cands)
            # a seam between two deck pieces (the line a hair outside both): a deck at the car's height within 3 m on is the same pavement
            if prev - pick > 0.5:
                for j in range(k + 1, min(n, k + 7)):
                    near = [t for t, b in P[j][3] if abs(t - prev) < 0.5]
                    if near:
                        pick = prev + (near[0] - prev) / (j - k + 1)
                        break
            if pick - prev > 0.25:
                steps.append((x, z, pick - prev))
        for t, b in decks:
            if pick + 0.05 < b < pick + 1.6 and not (abs(t - pick) < 0.05):
                walls.append((x, z, b - pick))
                break
        S[k] = pick
        prev = pick
    # the car spans about 2.5 m (its wheels average the ground) and its springs take a few centimetres: the surface it rides, averaged over 2.5 m
    raw = S
    S = [sum(raw[max(0, k - 2):k + 3]) / len(raw[max(0, k - 2):k + 3]) for k in range(n)]
    dt = 0.5 / v
    y, vy, up, air = S[0], 0.0, False, 0.0
    airs, hard, hard_at = [], 0.0, None
    for k in range(n - 1):
        yb = y + vy * dt - 0.5 * G * dt * dt
        vb = vy - G * dt
        sv = (S[k + 1] - S[k]) / dt
        if not up:
            if raw[k + 1] - raw[k] > 0.25:
                y, vy = S[k + 1], 0.0   # a step: the car hits it (counted as a step), it is not thrown by it
            elif yb > S[k + 1] + 0.05:
                up, air, y, vy = True, dt, yb, vb
            else:
                y, vy = S[k + 1], sv
        else:
            if yb <= S[k + 1]:
                imp = max(0.0, sv - vb)
                if imp > hard:
                    hard, hard_at = imp, P[k + 1][:2]
                airs.append((air, P[k + 1][0], P[k + 1][1]))
                up, y, vy = False, S[k + 1], sv
            else:
                y, vy, air = yb, vb, air + dt
    return raw, airs, hard, hard_at, steps, walls


def main():
    a = sys.argv[1:]
    only = a[a.index("--only") + 1] if "--only" in a else None
    prof = a[a.index("--profile") + 1] if "--profile" in a else None
    brief = "--brief" in a
    lines, pts = load(a[0], a[1])
    bad = runs = 0
    for li, L in enumerate(lines):
        if only and only not in L["name"]:
            continue
        P = pts.get(li, [])
        if len(P) < 4:
            continue
        v = L.get("cap") or 33
        for back in (False, True):
            Q = list(reversed(P)) if back else P
            k0 = next((k for k, q in enumerate(Q) if not q[3]), 0)
            Q = Q[k0:]
            S, airs, hard, hard_at, steps, walls = ride(Q, v)
            ma = max(airs, default=(0, 0, 0))
            fault = ma[0] > 0.3 or hard > 6 or steps or walls
            runs += 1
            bad += 1 if fault else 0
            if brief and not fault:
                continue
            print(f"{'LOOK' if fault else 'ok  '} {L['name']}{' (the other way)' if back else ''} at {v:.0f} m/s: air {ma[0]:.2f} s (to ({ma[1]:.0f}, {ma[2]:.0f})), "
                  f"landing {hard:.1f} m/s{f' at ({hard_at[0]:.0f}, {hard_at[1]:.0f})' if hard_at else ''}, {len(steps)} step(s), {len(walls)} low slab/face point(s)")
            for t, x, z in [q for q in airs if q[0] > 0.3][:5]:
                print(f"    air {t:.2f} s to ({x:.0f}, {z:.0f})")
            for x, z, d in steps[:4]:
                print(f"    step {d:.2f} m at ({x:.0f}, {z:.0f})")
            seen = set()
            for x, z, d in walls:
                key = (round(x / 10), round(z / 10))
                if key in seen:
                    continue
                seen.add(key)
                if len(seen) > 4:
                    break
                print(f"    low slab/face {d:.2f} m over the surface at ({x:.0f}, {z:.0f})")
            if prof and prof in L["name"]:
                for k in range(0, len(Q), 10):
                    print(f"      s {k * 0.5:7.1f} ({Q[k][0]:.0f}, {Q[k][1]:.0f}) surface {S[k]:.2f} ground {Q[k][2]:.2f}")
    print(f"{bad} of {runs} runs to look at")


if __name__ == "__main__":
    main()
