"""Round 36 (Part B): Racer's Free Roam activities on this map (Dan: "We can probably add free roam events"). Writes Data/activities/free_roam.json;
Build Map (MapBuilder.Activities.cs) puts the sites in the scene under their own root (game layer, not the world) and finds each smash run's props
among the built breakables. Re-run after mapgen (it reads Data/map.json and the track files), then Build Map.

Racer's FreeRoamWorld has 10 sites on about 1 km2 (5 jumps, 4 speed traps, 1 smash). This map is about 7.6 km2 but most of it is woods and roads
between the neighborhoods; the sites here: 8 jumps (the big jumps and ramps that are already there), 6 speed traps (Canton Road, New Chastain
Road, Chastain Road, Bells Ferry Road: each at the straightest stretch of the road, away from its junctions), 4 smash runs (a street of mailboxes,
the gas pumps, the shop row's partitions, a street's lamps and hydrants).

The medals' targets (per base vehicle, Racer's tables) come from scripted runs in the exe (ChastainRush.exe -exeRound36 <dir> -r36Only measure):
written into Data/activities/targets.json, read here. Until then the provisional targets below (from the track files' design flights).

Usage: python Tools/mapgen/make_activities.py
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data/activities/free_roam.json")
TARGETS = os.path.join(ROOT, "Data/activities/targets.json")
m = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))

def pts(flat): return [(flat[i], flat[i + 1]) for i in range(0, len(flat), 2)]
def dens(p, step=2.0):
    out = [p[0]]
    for i in range(len(p) - 1):
        a, b = p[i], p[i + 1]; n = max(1, int(math.dist(a, b) / step))
        for k in range(1, n + 1): out.append((a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n))
    return out
ends = []   # road ends: (point, name) - a junction unless it is the same road going on
for r in m["roads"]:
    p = pts(r["points"]); ends += [(p[0], r.get("name") or ""), (p[-1], r.get("name") or "")]
AVOID = [((3270.0, 590.0), 90.0, "the Ridge leap's landing")]

def speed_traps(name_prefix, count, sep=300.0):
    """the straightest 120 m stretches of the named road (heading change over +-80 m), 60 m or more from any road's end (a junction)"""
    cands = []
    for r in m["roads"]:
        if not (r.get("name") or "").startswith(name_prefix) or r.get("bridge"): continue
        line = dens(pts(r["points"]))
        if len(line) < 62: continue
        for i in range(30, len(line) - 30, 3):
            a, b, c = line[i - 30], line[i], line[i + 30]
            h1 = math.atan2(b[1] - a[1], b[0] - a[0]); h2 = math.atan2(c[1] - b[1], c[0] - b[0])
            bend = abs((h2 - h1 + math.pi) % (2 * math.pi) - math.pi)
            if min(math.dist(b, e) for e, nm in ends if not nm.startswith(name_prefix)) < 50: continue
            if any(math.dist(b, c0) < r0 for c0, r0, _ in AVOID): continue
            d = (c[0] - a[0], c[1] - a[1]); n = math.hypot(*d)
            cands.append((bend, b, (d[0] / n, d[1] / n), r["width"]))
    cands.sort(key=lambda x: x[0]); out = []
    for bend, p, d, w in cands:
        if all(math.dist(p, q[0]) > sep for q in out): out.append((p, d, w, math.degrees(bend)))
        if len(out) == count: break
    return out

sites = []
def site(**k): sites.append(k)
# --- speed traps (both directions: Racer's)
traps = [("Canton Road", 2, "Canton Road"), ("New Chastain Road", 2, "New Chastain Road"), ("Chastain Road", 1, "Chastain Road"), ("Bells Ferry Road", 1, "Bells Ferry Road")]
for prefix, n, title in traps:
    for i, (p, d, w, bend) in enumerate(speed_traps(prefix, n)):
        site(id=f"speed-{title.lower().replace(' ', '-')}-{i + 1}", title=f"{title} speed trap{' ' + str(i + 1) if n > 1 else ''}", kind="speed", x=round(p[0], 2), z=round(p[1], 2),
             fx=round(d[0], 4), fz=round(d[1], 4), radius=round(w * 0.5 + 1.5, 1), bronze=22.0, silver=28.0, gold=34.0, seconds=45.0, note=f"heading change {bend:.1f} deg over 120 m")
# --- jumps: on the launches that are there (BigJump ids), the stunt park's biggest; medal distances in meters
jumps = [("BJ1", "Footbridge leap"), ("BJ3", "Fallen tree leap"), ("BJ4", "Outcrop leap"), ("P1J", "Outfall leap"), ("RL", "Ridge leap"), ("S2", "Storage hop"),
         ("park-MEGA", "BIG AIR"), ("park-DROP", "Shop row drop")]
# the flight flat out off each launch, from the scripted runs recorded with the launches (the track files' flights, Rounds 19 to 33: air, meters,
# apex at each speed off the lip; the fastest is what a launch gives) and the stunt park's sizing (make_stunt_park.py): gold 95 % of it, silver
# 80 %, bronze 60 % (a launch that takes every vehicle at one speed lands near the same spot, so its medals sit close together)
flights = {}
for f in ("noonday_creek.json", "chastain_ridge.json"):
    for j in json.load(open(os.path.join(ROOT, "Data/tracks", f))).get("big_jumps", []):
        best = max(j["flights"].items(), key=lambda kv: float(kv[0])); flights[j["id"]] = best[1][1]
park = json.load(open(os.path.join(ROOT, "Data/tracks/stunt_park.json")))
for line in park.get("landings", []):
    jid = line.split()[0]; dist = float(line.split("+0 m/s +0.0 m: ")[1].split(" m out")[0]); flights["park-" + jid] = dist
for jid, title in jumps:
    d = flights.get(jid, 40.0); fixed = jid.startswith("park-") or jid in ("RL", "S2")
    g, sv, b = (0.96, 0.9, 0.8) if fixed else (0.95, 0.8, 0.6)
    site(id="jump-" + jid.lower(), title=title, kind="jump", jump=jid, radius=28.0, bronze=round(d * b, 1), silver=round(d * sv, 1), gold=round(d * g, 1), seconds=120.0, note=f"flat out {d:.1f} m")
# --- smash runs: the breakables of these kinds in the area (Build Map takes the densest row of up to `count`); the time and medals per run
site(id="smash-mailboxes", title="Mailbox run", kind="smash", x=2938.7, z=799.3, radius=260.0, props="Mailbox", count=12, seconds=30.0, bronze=4.0, silver=8.0, gold=12.0)
site(id="smash-pumps", title="Pump smash", kind="smash", x=3418.0, z=1164.0, radius=25.0, props="Sign", count=4, seconds=15.0, bronze=2.0, silver=3.0, gold=4.0)
site(id="smash-shop-row", title="Shop row smash", kind="smash", x=3402.0, z=1209.0, radius=80.0, props="Wood", count=0, seconds=40.0, bronze=12.0, silver=24.0, gold=40.0)
site(id="smash-street-lamps", title="Street lamp smash", kind="smash", x=2938.7, z=799.3, radius=400.0, props="Lamp", count=10, seconds=40.0, bronze=3.0, silver=6.0, gold=10.0)

# measured targets (the exe's scripted runs), per base vehicle: original, tourer, moto, atv
if os.path.exists(TARGETS):
    t = json.load(open(TARGETS))
    for s in sites:
        if s["id"] in t: s.update(t[s["id"]])
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(dict(note="Round 36 Part B: the Free Roam activities (Tools/mapgen/make_activities.py; Build Map: MapBuilder.Activities.cs). Map meters.", sites=sites), open(OUT, "w"), indent=1)
for s in sites: print(s["kind"], s["id"], s.get("x", ""), s.get("z", ""), s.get("note", ""))
print("wrote", OUT, len(sites), "sites")
