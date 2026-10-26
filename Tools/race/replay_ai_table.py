"""Round 70 (B1): the AI's driving measured from race replays (.crreplay, Race/Replay/RaceReplay.cs, version 2).
Per racer: vehicle, laps, best and average lap, time at full throttle, average speed; and against the player on the same stretches of the lap
(the lap cut into 50 m pieces by the route's centerline): where the AI loses time (its speed there, its throttle, how far off the route's center).
Usage: python Tools/race/replay_ai_table.py <file.crreplay> [...]   (route files from Data/routes by the replay's routeFile)"""
import gzip, struct, sys, os, json, math
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

class R:
    def __init__(s, b): s.buf, s.i = b, 0
    def take(s, n): v = s.buf[s.i:s.i + n]; s.i += n; return v
    def u(s, f): n = struct.calcsize(f); return struct.unpack("<" + f, s.take(n))
    def i32(s): return s.u("i")[0]
    def f32(s): return s.u("f")[0]
    def b(s): return s.u("?")[0]
    def str(s):
        n, sh = 0, 0
        while True:
            c = s.take(1)[0]; n |= (c & 0x7F) << sh; sh += 7
            if c < 0x80: break
        return s.take(n).decode("utf-8")

def track(r, full):
    idx, start, dt, n = r.i32(), r.f32(), r.f32(), r.i32(); fr = []
    for _ in range(n):
        t, x, y, z = r.u("ffff"); q = r.u("hhhh"); sp = r.u("h")[0] / 100.0
        st = th = 0.0; wh = 4
        if full: st = r.u("b")[0] / 127.0; th = r.u("B")[0] / 255.0; wh = r.u("B")[0]
        fr.append((t, x, y, z, sp, st, th, wh))
    return fr

def read(path):
    r = R(gzip.open(path).read())
    assert r.take(4) == b"CRRP"; ver = r.i32()
    h = {k: r.str() for k in ["build", "routeFile", "routeName", "recordName", "direction", "category", "routeHash", "date", "label"]}
    h["laps"] = r.i32(); h["kind"] = r.str(); h["t0"], h["t1"], h["best"] = r.f32(), r.f32(), r.f32(); h["focus"] = r.i32()
    racers = []
    for _ in range(r.i32()):
        x = {"name": r.str(), "vehicle": r.str(), "rider": r.str(), "hasPaint": r.b()}; r.u("fff"); x["player"] = r.b(); x["seat"] = r.i32()
        x["finishedAt"], x["total"] = r.f32(), r.f32()
        x["laps"] = [r.u("iff") for _ in range(r.i32())]
        x["frames"] = track(r, True); racers.append(x)
    return h, racers

def lap_line(h):
    f = os.path.join(ROOT, "Data/routes", os.path.basename(h["routeFile"]) if h["routeFile"] else "")
    if not os.path.isfile(f):
        for g in os.listdir(os.path.join(ROOT, "Data/routes")):
            j = json.load(open(os.path.join(ROOT, "Data/routes", g), encoding="utf-8"))
            if j.get("name") == h["routeName"] or j.get("records_as") == h["recordName"]: f = os.path.join(ROOT, "Data/routes", g); break
    j = json.load(open(f, encoding="utf-8")); key = "loop_rev" if h["direction"] == "reverse" and j.get("loop_rev") else "loop"
    p = j[key]; pts = [(p[i], p[i + 1]) for i in range(0, len(p), 2)]
    if h["direction"] == "reverse" and key == "loop": pts = pts[::-1]
    c = [0.0]
    for a, b in zip(pts, pts[1:]): c.append(c[-1] + math.dist(a, b))
    return pts, c

def station(pts, c, x, z, prev=None, win=200.0):
    best, bs, bd = 1e18, 0.0, 0.0
    for i in range(len(pts) - 1):
        if prev is not None:
            d0 = (c[i] - prev) % c[-1]
            if d0 > win and c[-1] - d0 > 40: continue
        (ax, az), (bx, bz) = pts[i], pts[i + 1]; dx, dz = bx - ax, bz - az; L = dx * dx + dz * dz or 1e-9
        t = max(0.0, min(1.0, ((x - ax) * dx + (z - az) * dz) / L)); px, pz = ax + dx * t, az + dz * t; d = (x - px) ** 2 + (z - pz) ** 2
        if d < best: best, bs, bd = d, c[i] + t * math.sqrt(L), math.sqrt(d)
    return bs, bd

def main(paths):
    for path in paths:
        h, racers = read(path); pts, c = lap_line(h); L = c[-1]
        print(f"\n=== {h['routeName']} {h['direction']} ({h['laps']} laps, lap {L:.0f} m) {os.path.basename(path)}")
        PIECE = 50.0; nP = int(L // PIECE) + 1; per = {}
        for x in racers:
            fr = x["frames"]; laps = [e - s for (_, s, e) in x["laps"] if e > s]
            moving = [f for f in fr if f[0] >= 0]
            full = sum(1 for f in moving if f[6] > 0.97) / max(1, len(moving)); avg = sum(f[4] for f in moving) / max(1, len(moving))
            # per 50 m piece of the lap: time spent, mean speed, mean throttle, mean distance off the centerline (laps 2 and on: a flying start)
            tp = [0.0] * nP; sp = [[] for _ in range(nP)]; th = [[] for _ in range(nP)]; off = [[] for _ in range(nP)]; prev = None
            lap2 = x["laps"][0][2] if len(x["laps"]) > 1 else None
            for a, b in zip(moving, moving[1:]):
                if lap2 is None or a[0] < lap2 or (x["finishedAt"] > 0 and a[0] > x["finishedAt"]): continue
                s, d = station(pts, c, a[1], a[3], prev); prev = s; k = min(nP - 1, int(s // PIECE))
                tp[k] += b[0] - a[0]; sp[k].append(a[4]); th[k].append(a[6]); off[k].append(d)
            nl = max(1, len(laps) - 1)
            per[x["name"]] = [(tp[k] / nl, (sum(sp[k]) / len(sp[k])) if sp[k] else 0, (sum(th[k]) / len(th[k])) if th[k] else 0, (sum(off[k]) / len(off[k])) if off[k] else 0) for k in range(nP)]
            print(f"  {'*' if x['player'] else ' '} {x['name']:<10} {x['vehicle']:<12} laps {' '.join(f'{v:6.1f}' for v in laps)}  best {min(laps) if laps else 0:6.1f}  "
                  f"total {x['total']:7.1f}  full throttle {full*100:4.0f} %  avg {avg:4.1f} m/s")
        pl = next((x["name"] for x in racers if x["player"]), None)
        if not pl: continue
        print(f"  where the AI loses to {pl} (per lap, laps 2+): piece  player s / m/s / throttle  vs each AI s / m/s / throttle / m off the center")
        loss = []
        for k in range(nP):
            p = per[pl][k]
            for n, vv in per.items():
                v = vv[k]
                if n == pl or p[0] <= 0 or v[0] <= 0: continue
                loss.append((v[0] - p[0], k, n, p, v))
        tot = {}
        for d, k, n, p, v in loss: tot[n] = tot.get(n, 0) + d
        print("  the AI's time lost against the player over a lap: " + ", ".join(f"{n} {t:+.1f} s" for n, t in tot.items()))
        for d, k, n, p, v in sorted(loss, reverse=True)[:12]:
            print(f"    {k*PIECE:5.0f}-{k*PIECE+PIECE:4.0f} m  {n:<9} +{d:4.1f} s   player {p[0]:4.1f} s {p[1]:4.1f} m/s thr {p[2]:.2f}   AI {v[0]:4.1f} s {v[1]:4.1f} m/s thr {v[2]:.2f} off {v[3]:4.1f} m")

if __name__ == "__main__": main(sys.argv[1:])
