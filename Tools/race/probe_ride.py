"""Round 88 (C): ride the real surfaces under each line (R88Probe's tsv: every collider hit under the line every 0.5 m), both ways.

The car keeps to the surface nearest where it is heading (a deck or the ground with n.y over 0.6); a face (n.y under 0.6) standing between
where it is and 1.2 m above it is a wall; a point mass at the line's cap shows the air and hard landings. Prints faults per run and, with
--at x,z, the surfaces within 40 m of that point.

Usage: python Tools/race/probe_ride.py <lines.json> <probe.tsv> [--only text] [--at x,z] [--brief]
"""
import json
import math
import sys

G = 9.81


def load(lines_path, tsv):
    lines = json.load(open(lines_path, encoding="utf-8"))["lines"]
    pts = {}
    with open(tsv, encoding="utf-8") as f:
        next(f)
        for row in f:
            c = row.rstrip("\n").split("\t")
            hits = []
            if len(c) > 4 and c[4]:
                for h in c[4].split("|"):
                    y, ny, nm = h.split("/", 2)
                    hits.append((float(y), float(ny), nm))
            pts.setdefault(int(c[0]), []).append((float(c[2]), float(c[3]), hits))
    return lines, pts


def ride(P, v, start_high):
    n = len(P)
    S, names, walls = [], [], []
    prev, slope = None, 0.0
    for k, (x, z, hits) in enumerate(P):
        surf = [(y, nm) for y, ny, nm in hits if ny >= 0.6]
        if not surf:
            S.append(prev if prev is not None else 0.0); names.append("MISS"); continue
        if prev is None:
            ter = [s for s in surf if s[1].startswith("Terrain")]
            pick = max(surf) if start_high or not ter else ter[0]
        else:
            pred = prev + slope * 0.5
            # never jump up more than 0.6 m to a surface (that is a slab above), unless it is the only one
            cands = [s for s in surf if s[0] <= prev + 0.6] or surf
            pick = min(cands, key=lambda s: abs(s[0] - pred))
        for y, ny, nm in hits:
            if ny < 0.6 and pick[0] + 0.05 < y < pick[0] + 1.2:
                walls.append((x, z, y - pick[0], nm)); break
        if prev is not None:
            slope = 0.5 * slope + 0.5 * (pick[0] - prev) / 0.5
        S.append(pick[0]); names.append(pick[1]); prev = pick[0]
    dt = 0.5 / v
    y, vy, up, air, maxair, hard = S[0], 0.0, False, 0.0, 0.0, 0.0
    airs, steps = [], []
    for k in range(n - 1):
        yb, vb, sv = y + vy * dt - 0.5 * G * dt * dt, vy - G * dt, (S[k + 1] - S[k]) / dt
        if not up:
            if S[k + 1] - S[k] > 0.2:
                steps.append((P[k][0], P[k][1], S[k + 1] - S[k], names[k + 1]))
            if yb > S[k + 1] + 0.02:
                up, air, y, vy = True, dt, yb, vb; t0 = k
            else:
                y, vy = S[k + 1], sv
        else:
            if yb <= S[k + 1]:
                imp = max(0.0, sv - vb); hard = max(hard, imp)
                if air > 0.3: airs.append((P[t0][0], P[t0][1], P[k + 1][0], P[k + 1][1], air, imp, names[t0], names[k + 1]))
                maxair = max(maxair, air); up = False; y, vy = S[k + 1], sv
            else:
                y, vy, air = yb, vb, air + dt
    return S, names, walls, steps, airs, maxair, hard


def main():
    a = sys.argv[1:]
    lines, pts = load(a[0], a[1])
    only = a[a.index("--only") + 1] if "--only" in a else None
    at = tuple(map(float, a[a.index("--at") + 1].split(","))) if "--at" in a else None
    bad = 0
    for li, L in enumerate(lines):
        if li not in pts or (only and only not in L["name"]):
            continue
        P = pts[li]
        v = L.get("cap") or 30
        for d in (0, 1):
            R = P if d == 0 else P[::-1]
            S, names, walls, steps, airs, maxair, hard = ride(R, v, " over " in L["name"])
            fault = maxair > 0.3 or hard > 6 or walls or steps
            bad += bool(fault)
            print(f"{'LOOK' if fault else 'ok  '} {L['name']}{' (the other way)' if d else ''}: air {maxair:.2f} s, landing {hard:.1f} m/s, {len(steps)} steps, {len(walls)} walls")
            for x0, z0, x1, z1, t, imp, n0, n1 in airs[:4]:
                print(f"     air {t:.2f} s ({x0:.0f}, {z0:.0f}) [{n0}] -> ({x1:.0f}, {z1:.0f}) [{n1}] landing {imp:.1f}")
            for x, z, h, nm in steps[:3]:
                print(f"     step {h:.2f} m at ({x:.0f}, {z:.0f}) onto {nm}")
            seen = set()
            for x, z, h, nm in walls:
                key = (round(x / 5), round(z / 5))
                if key in seen: continue
                seen.add(key); print(f"     wall {h:.2f} m up at ({x:.0f}, {z:.0f}) {nm}")
                if len(seen) >= 3: break
            if at and d == 0:
                for k, (x, z, hits) in enumerate(R):
                    if math.hypot(x - at[0], z - at[1]) < 40 and k % 2 == 0:
                        hs = " | ".join(f"{y:.2f} {ny:.2f} {nm[:22]}" for y, ny, nm in hits[:5])
                        print(f"       k {k:4d} ({x:7.1f}, {z:7.1f}) ride {S[k]:.2f} {names[k][:18]:18s} :: {hs}")
    print(f"{bad} runs to look at")


if __name__ == "__main__":
    main()
