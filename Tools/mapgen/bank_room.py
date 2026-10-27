"""Round 75 (C): how far a graded line's bank may reach before it meets a road or a trail of map.json.

make_summit_leap.py and make_mountain_route.py size each bank from the ground at its own edge (Dan's BUG-001: a near-vertical face beside the
summit leap's way back); a wider bank must not regrade a road or a trail beside it (Kennesaw Mountain Drive is the race's own climb; the park's
trails are drawn on the ground they lie on). room(q) is the distance from q to the nearest road or trail edge, less a 2 m margin."""
import math


def ways_room(M, points, reach=120.0, margin=2.0):
    xs = [p[0] for p in points]; zs = [p[1] for p in points]
    x0, x1, z0, z1 = min(xs) - reach, max(xs) + reach, min(zs) - reach, max(zs) + reach
    segs = []   # (ax, az, bx, bz, half width)
    for w in list(M["roads"]) + list(M.get("trails", [])):
        p = w["points"]; q = list(zip(p[0::2], p[1::2])); hw = max(1.0, w.get("width", 2.0) / 2.0)
        for a, b in zip(q, q[1:]):
            if max(a[0], b[0]) < x0 or min(a[0], b[0]) > x1 or max(a[1], b[1]) < z0 or min(a[1], b[1]) > z1: continue
            segs.append((a[0], a[1], b[0], b[1], hw))
    cell = 40.0; grid = {}
    for k, (ax, az, bx, bz, _) in enumerate(segs):
        for cx in range(int(math.floor(min(ax, bx) / cell)), int(math.floor(max(ax, bx) / cell)) + 1):
            for cz in range(int(math.floor(min(az, bz) / cell)), int(math.floor(max(az, bz) / cell)) + 1): grid.setdefault((cx, cz), []).append(k)

    def room(q, look=reach):
        best = look; n = int(math.ceil(look / cell)); qx, qz = int(math.floor(q[0] / cell)), int(math.floor(q[1] / cell)); seen = set()
        for cx in range(qx - n, qx + n + 1):
            for cz in range(qz - n, qz + n + 1):
                for k in grid.get((cx, cz), ()):
                    if k in seen: continue
                    seen.add(k); ax, az, bx, bz, hw = segs[k]; dx, dz = bx - ax, bz - az; L2 = dx * dx + dz * dz
                    t = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((q[0] - ax) * dx + (q[1] - az) * dz) / L2))
                    best = min(best, math.hypot(q[0] - ax - dx * t, q[1] - az - dz * t) - hw - margin)
        return best
    return room
