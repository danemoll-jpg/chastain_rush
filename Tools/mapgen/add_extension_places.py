"""Round 59 (the map extension, stage 1): the new destinations on the map (Town Center at Cobb, Kennesaw State University), added to
Data/explore/eggs.json's "destinations" without touching its eggs or the other destinations (make_eggs.py would place the eggs again).
Each is put on the nearest road to the place's middle (from OSM, Round 59), facing along it. Idempotent: run after mapgen (and after
make_eggs.py, which writes the file anew). No eggs: Round 59 adds none.

Usage: python Tools/mapgen/add_extension_places.py
"""
import json, math, os
from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EGGS = os.path.join(ROOT, "Data", "explore", "eggs.json")
M = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
tr = Transformer.from_crs(4326, M["utm_epsg"], always_xy=True)
ox, oz = M["origin_utm"]
first = M.get("extension", {}).get("first", {}).get("roads", 0)

# (id, title, lat, lon, the roads to stand on: name parts, first match wins; the place's middle from its OSM outline)
PLACES = [("town-center", "Town Center at Cobb", (34.0148368 + 34.020945) / 2, (-84.5678428 + -84.5601757) / 2, ("Town Center", "Barrett", "")),
          ("ksu", "Kennesaw State University", (34.0294357 + 34.0473709) / 2, (-84.5872929 + -84.5794173) / 2, ("Campus", "Chastain", "")),
          # Round 61 (B, stage 2, Dan's map: "A fast-travel spot at the summit and at the visitor center"): the summit's lot is the top of Kennesaw
          # Mountain Drive nearest OSM's peak (33.97621, -84.57966); the visitor center on the drive nearest its OSM outline's middle
          ("kennesaw-summit", "Kennesaw Mountain summit", 33.9762125, -84.5796556, ("Kennesaw Mountain Drive",)),
          ("kennesaw-visitor-center", "Kennesaw Mountain visitor center", 33.9830351, -84.5779436, ("Kennesaw Mountain Drive", ""))]
SERVICE_OK = {"kennesaw-summit", "kennesaw-visitor-center"}   # the drive's top is OSM highway=service


def near_road(q, parts, service=False):
    for part in parts:
        best = None
        for r in M["roads"][first:]:   # the extension's roads (a place there stands on one of them)
            if r["class"] in ("motorway", "motorway_link") or (r["class"] == "service" and not service): continue
            if part and part.lower() not in r["name"].lower(): continue
            p = r["points"]; pts = [(p[i], p[i + 1]) for i in range(0, len(p), 2)]
            for a, b in zip(pts, pts[1:]):
                ab = (b[0] - a[0], b[1] - a[1]); L2 = ab[0] ** 2 + ab[1] ** 2 or 1e-9
                t = max(0, min(1, ((q[0] - a[0]) * ab[0] + (q[1] - a[1]) * ab[1]) / L2)); c = (a[0] + ab[0] * t, a[1] + ab[1] * t)
                d = math.dist(c, q)
                if best is None or d < best[0]: best = (d, c, math.degrees(math.atan2(ab[0], ab[1])) % 360, r["name"])
        if best and best[0] < 400: return best
    return None


# Round 60 (A, Dan: "a fast-travel spot at the main entrance"): Town Center's destination stands in the lot outside the main (north) doors, 32 m
# out, facing them (the mall's frame: MapBuilder.Mall.cs, u -51, v -1235; u along the concourse, 14.75 degrees north of east)
FIXED = {"town-center": (265.11, -1207.29, 165.25, "the lot outside Town Center's main doors")}

data = json.load(open(EGGS, encoding="utf-8"))
dests = [d for d in data["destinations"] if d["id"] not in {p[0] for p in PLACES}]
for did, title, lat, lon, parts in PLACES:
    if did in FIXED:
        fx, fz, fyaw, where = FIXED[did]
        dests.append({"id": did, "title": title, "x": fx, "z": fz, "y": 0.0, "fixed": False, "yaw": fyaw}); print(f"{title}: {where} ({fx}, {fz})"); continue
    x, y = tr.transform(lon, lat); q = (x - ox, y - oz)
    rp = near_road(q, parts, did in SERVICE_OK)
    if rp is None: print(f"{title}: no road near ({q[0]:.0f}, {q[1]:.0f})"); continue
    dests.append({"id": did, "title": title, "x": round(rp[1][0], 2), "z": round(rp[1][1], 2), "y": 0.0, "fixed": False, "yaw": round(rp[2], 1)})
    print(f"{title}: on {rp[3] or 'an unnamed road'} at ({rp[1][0]:.1f}, {rp[1][1]:.1f}), {rp[0]:.0f} m from its middle")
data["destinations"] = dests
json.dump(data, open(EGGS, "w", encoding="utf-8"), indent=1)
print(f"{len(dests)} destinations -> {EGGS}")
