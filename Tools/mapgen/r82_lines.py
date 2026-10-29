"""Round 82: the lines the exe check drives (local/round82/r82_lines.json, read by -exeRound82 ... -r82Only lines).

  - each Express Lanes carriageway end to end (both ways), pictures at Dan's BUG-005 / BUG-006 / BUG-007 places and the places Round 81 listed
  - the Big Shanty Express ramp from Big Shanty Road onto the lanes and 300 m on (C)
  - both roads through every crossing whose clearance this round changed or that is still short (A; from Assets/Generated/round82_crossings.txt), 150 m
    each side, both ways, a picture at the crossing on the first ten
  - the railroad through BUG-008's deck, both ways (D)
Usage (after the build): python Tools/mapgen/r82_lines.py
"""
import json
import math
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EXPRESS = "Northwest Corridor Express Lanes"


def pts(flat):
    return [(flat[k], flat[k + 1]) for k in range(0, len(flat), 2)]


def length(q):
    return sum(math.hypot(q[i + 1][0] - q[i][0], q[i + 1][1] - q[i][1]) for i in range(len(q) - 1))


def resample(q, step):
    o = [q[0]]
    carry = 0.0
    for i in range(1, len(q)):
        a, b = q[i - 1], q[i]
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        s = step - carry
        while s <= L:
            o.append((a[0] + (b[0] - a[0]) * s / max(L, 1e-6), a[1] + (b[1] - a[1]) * s / max(L, 1e-6)))
            s += step
        carry = L - (s - step)
    if math.hypot(o[-1][0] - q[-1][0], o[-1][1] - q[-1][1]) > step * .3:
        o.append(q[-1])
    return o


def station(q, p):
    best, bs, acc = 1e18, 0.0, 0.0
    for i in range(len(q) - 1):
        a, b = q[i], q[i + 1]
        dx, dz = b[0] - a[0], b[1] - a[1]
        L2 = dx * dx + dz * dz
        t = max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / max(L2, 1e-9)))
        d = math.hypot(a[0] + dx * t - p[0], a[1] + dz * t - p[1])
        if d < best:
            best, bs = d, acc + t * math.sqrt(L2)
        acc += math.sqrt(L2)
    return bs, best


def main():
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    pieces = [("road", i, pts(r["points"]), r) for i, r in enumerate(m["roads"])] + [("bridge", i, pts(b["points"]), b) for i, b in enumerate(m["bridges"])]
    ends = {}
    for k, pc in enumerate(pieces):
        for e in (pc[2][0], pc[2][-1]):
            ends.setdefault((round(e[0] / 4), round(e[1] / 4)), []).append(k)

    def near_ends(p):
        out = []
        cx, cz = round(p[0] / 4), round(p[1] / 4)
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                out += ends.get((cx + dx, cz + dz), [])
        return out

    def run_on(path, used, limit, same_name=None):
        """carry a path on through the pieces that continue from its last point (best heading), to 'limit' m"""
        while length(path) < limit:
            tail, prev = path[-1], path[-2]
            d = (tail[0] - prev[0], tail[1] - prev[1])
            dl = math.hypot(*d) or 1
            best, bk, bq = 0.5, None, None
            for k in near_ends(tail):
                if k in used:
                    continue
                q = pieces[k][2]
                if same_name is not None and (pieces[k][3].get("name") == EXPRESS) != same_name:
                    continue
                if math.hypot(q[0][0] - tail[0], q[0][1] - tail[1]) < 2.5:
                    qq = q
                elif math.hypot(q[-1][0] - tail[0], q[-1][1] - tail[1]) < 2.5:
                    qq = q[::-1]
                else:
                    continue
                nd = (qq[1][0] - qq[0][0], qq[1][1] - qq[0][1])
                dt = (nd[0] * d[0] + nd[1] * d[1]) / ((math.hypot(*nd) or 1) * dl)
                if dt > best:
                    best, bk, bq = dt, k, qq
            if bk is None:
                break
            used.add(bk)
            path += bq[1:]
        return path

    lines = []
    # ---- the Express Lanes end to end
    ex = [k for k, pc in enumerate(pieces) if pc[3].get("name") == EXPRESS]
    used = set()
    picks = [("BUG005", (114.42, -2636.04)), ("BUG006", (1499.89, -4269.27)), ("BUG007", (2589.0, -5263.69)), ("EL13", (2300.0, -4960.0)),
             ("BigShanty", (-600.0, -420.0)), ("merge", (507.0, -3350.0)), ("deck1414", (-1380.0, 1570.0)), ("creek", (-340.0, -900.0))]
    for k in ex:
        if k in used:
            continue
        q = pieces[k][2]
        # an end with no other Express piece at it
        free_end = None
        for e, qq in ((q[0], q), (q[-1], q[::-1])):
            if not any(j != k and j in ex and (math.hypot(pieces[j][2][0][0] - e[0], pieces[j][2][0][1] - e[1]) < 2.5 or math.hypot(pieces[j][2][-1][0] - e[0], pieces[j][2][-1][1] - e[1]) < 2.5) for j in near_ends(e)):
                free_end = qq
                break
        if free_end is None:
            continue
        used.add(k)
        path = run_on(list(free_end), used, 1e9, same_name=True)
        path = resample(path, 6.0)
        # (the road's end at the map's edge is closed (Round 66): start and end 120 m inside it)
        ext = m["extent_m"]
        def at_edge(p):
            return min(abs(p[0] - ext[0]), abs(p[0] - ext[2]), abs(p[1] - ext[1]), abs(p[1] - ext[3])) < 5
        while len(path) > 2 and at_edge(path[0]):
            path = path[20:]
        while len(path) > 2 and at_edge(path[-1]):
            path = path[:-20]
        L = length(path)
        if L < 1000:
            continue
        name = "I-575 Express Lanes end to end" if max(p[1] for p in path) > 0 and max(p[0] for p in path) < 1000 else "I-75 Express Lanes end to end"
        st, nm = [], []
        for n, p in picks:
            s, d = station(path, p)
            if d < 25:
                st.append(round(s, 1))
                nm.append(n)
        lines.append({"name": name, "both": True, "cap": 35, "line": [round(v, 2) for p in path for v in p], "pics": st, "picNames": nm})
    # ---- C: the Big Shanty Express ramp onto the lanes
    ramp = next((k for k, pc in enumerate(pieces) if pc[0] == "road" and pc[3].get("osm_id") == 536789460), None)
    if ramp is not None:
        q = pieces[ramp][2]
        if math.hypot(q[-1][0] + 551, q[-1][1] + 554) < math.hypot(q[0][0] + 551, q[0][1] + 554):
            q = q[::-1]
        # the ramp, its creek bridge, then on into the Express Lanes past the merge (-325.5, -947.9): 300 m
        path = run_on(list(q), {ramp}, length(q) + 400)
        path = resample(path, 4.0)
        st, nm = [], []
        for n, p in (("BUG002", (-374.14, -843.31)), ("BUG003", (-333.65, -943.01))):
            s, d = station(path, p)
            if d < 25:
                st.append(round(s, 1))
                nm.append(n)
        lines.append({"name": "Big Shanty Express ramp onto the lanes", "both": True, "cap": 30, "line": [round(v, 2) for p in path for v in p], "pics": st, "picNames": nm})
    # ---- A: both roads through every crossing this round changed
    rep = os.path.join(ROOT, "Assets", "Generated", "round82_crossings.txt")
    cj = json.load(open(os.path.join(ROOT, "Data", "tracks", "crossings.json"), encoding="utf-8"))["crossings"]
    changed = []
    if os.path.exists(rep):
        rows = [l for l in open(rep, encoding="utf-8") if l.startswith(("ok", "SHORT"))]
        for i, l in enumerate(rows):
            mm = re.search(r": (-?[\d.]+) -> (-?[\d.]+) -> (-?[\d.]+) m$", l.strip())
            if not mm or i >= len(cj):
                continue
            b, md, a = map(float, mm.groups())
            if b < 5 or md < 5 or a < 5 or abs(a - b) > .5:
                changed.append((cj[i], b, a))
    shots = 0
    for c, b, a in changed:
        X = (c["x"], c["z"])
        for side in ("over", "under"):
            ref = c[side]
            k = next(j for j, pc in enumerate(pieces) if pc[0] == ref["kind"] and pc[1] == ref["index"])
            q = pieces[k][2]
            s, _ = station(q, X)
            L = length(q)
            fwd = run_on(list(q), {k}, L + 150)
            back = run_on(list(q[::-1]), {k}, L + 150)
            whole = back[::-1] + fwd[len(q):]
            whole = resample(whole, 3.0)
            s0, _ = station(whole, X)
            cut = []
            acc = 0.0
            for i, p in enumerate(whole):
                if i:
                    acc += math.hypot(p[0] - whole[i - 1][0], p[1] - whole[i - 1][1])
                if s0 - 150 <= acc <= s0 + 150:
                    cut.append(p)
            if len(cut) < 5:
                continue
            nm = ref["name"] or ref["class"]
            s1, _ = station(cut, X)
            e = {"name": f"{nm} ({ref['osm_id']}) {side} at ({X[0]:.0f}, {X[1]:.0f}), {b:.1f} -> {a:.1f} m clear", "both": True, "cap": 30,
                 "line": [round(v, 2) for p in cut for v in p], "pics": [], "picNames": []}
            if shots < 10:
                e["pics"] = [round(max(5.0, s1 - 25.0), 1)]
                e["picNames"] = [f"X{shots}"]
            lines.append(e)
        shots += 1
    # ---- D: the railroad through BUG-008's deck
    rr = json.load(open(os.path.join(ROOT, "Data", "tracks", "railroad.json"), encoding="utf-8"))
    q = pts(rr["lines"][2]["points"])[990:1140]
    lines.append({"name": "the railroad through BUG-008's deck", "both": True, "cap": 12, "line": [round(v, 2) for p in q for v in p], "pics": [], "picNames": []})
    os.makedirs(os.path.join(ROOT, "local", "round82"), exist_ok=True)
    with open(os.path.join(ROOT, "local", "round82", "r82_lines.json"), "w", encoding="utf-8") as fh:
        json.dump({"lines": lines}, fh)
    print(f"{len(lines)} lines ({len(changed)} crossings changed): " + "; ".join(f"{l['name']} {length(pts(l['line'])):.0f} m" for l in lines[:6]))


if __name__ == "__main__":
    main()
