"""Round 77 (B, Dan's BUG-004 x 1608.76 z -6601.44: "lets make the hospital resemble the hospital"; BUG-005 x 1665.13 z -6716.61 Church Street NW:
"pretty sure that maybe this is the parking deck. So not businesses"; his photos in local/reference_photos/kennestone/, reference only): what the map
build needs to build Kennestone hospital as itself and every parking deck on the map as a deck. From raw/osm_ext2.json and Data/map.json. Writes
Data/tracks/kennestone.json (world, not game layer), read by MapBuilder.Kennestone.cs:
  owned     map.json buildings the hospital's own builder replaces (the campus: mapgen's one-level box over the whole site, the ER, the skybridges,
            the canopies, the clinics and offices round it)
  parts     OSM's building:part ways on the campus (the towers): outline, levels, min_level, colour, name (mapgen keeps buildings only)
  podium    the campus's main footprint (map.json index): one story under the towers
  raised    buildings that start a floor up over a road (OSM min_level / layer: the skybridges; BUG-005's building is the one over Church Street)
  er        the emergency department (map.json index), its ambulance canopy (OSM building=roof over the bay), the emergency entrances
  portico   the main entrance: the drop-off loop off Church Street (its inner point); the portico stands on the main footprint's face toward it
  decks     every parking deck on the map (map.json type parking: OSM building=parking): the rectangle the deck is built in (its centre, long axis,
            half length and width; the largest one inside an odd footprint), its levels (OSM building:levels, else from its height)
Re-run after mapgen (map.json's indices), then Build Map. Usage: python Tools/mapgen/make_kennestone.py"""
import json
import math
import os

import numpy as np
from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data", "tracks", "kennestone.json")
CAMPUS = (1580.0, -6690.0)
CAMPUS_R = 260.0
OWNED_OSM = [248052493, 509320497, 792730867, 792730864, 1235837602, 248052477, 509320498, 546669622, 546674882, 866018674, 1235676362,
             318633318, 318633320, 366980641, 509325965, 509333189, 1505424382]
PODIUM_OSM = 509320497
ER_OSM, ER_CANOPY_OSM = 792730867, 866018674
LOOP = (1633.0, -6665.0)   # the drop-off loop's inner point (map roads 11039 / 11040 / 4813 off Church Street)


def main():
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    cfg = json.load(open(os.path.join(ROOT, "Tools", "mapgen", "config.json"), encoding="utf-8"))
    ox, oz = m["origin_utm"]
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    osm = json.load(open(os.path.join(ROOT, "raw", "osm_ext2.json"), encoding="utf-8"))
    nodes = {}
    for e in osm["elements"]:
        if e["type"] == "node":
            x, y = tr.transform(e["lon"], e["lat"]); nodes[e["id"]] = (x - ox, y - oz, e.get("tags"))
    ways = {e["id"]: e for e in osm["elements"] if e["type"] == "way"}
    ring = lambda w: [nodes[n][:2] for n in ways[w]["nodes"] if n in nodes]
    cen = lambda q: (sum(p[0] for p in q) / len(q), sum(p[1] for p in q) / len(q))
    B = m["buildings"]
    bc = [(k, cen(list(zip(b["footprint"][0::2], b["footprint"][1::2])))) for k, b in enumerate(B)]
    bc = [(k, c) for k, c in bc if math.dist(c, CAMPUS) < CAMPUS_R + 200]

    def index_of(w):
        q = ring(w); q = q[:-1] if q[0] == q[-1] else q
        c = cen(q); k, d = min(((k, math.dist(c, p)) for k, p in bc), key=lambda t: t[1])
        return k if d < 3.0 else None
    owned = sorted({k for k in (index_of(w) for w in OWNED_OSM if w in ways) if k is not None})
    parts = []
    for w, e in ways.items():
        t = e.get("tags", {})
        if "building:part" not in t: continue
        q = ring(w); q = q[:-1] if q and q[0] == q[-1] else q
        if len(q) < 3 or math.dist(cen(q), CAMPUS) > CAMPUS_R: continue
        if t.get("man_made") == "cooling": continue
        try: lv = int(float(t.get("building:levels", "1")))
        except ValueError: lv = 1
        try: ml = int(float(t.get("building:min_level", "0")))
        except ValueError: ml = 0
        parts.append({"osm": w, "outline": [round(v, 2) for p in q for v in p], "levels": lv, "min_level": ml,
                      "colour": t.get("building:colour", ""), "name": t.get("name", "")})
    raised = []
    for w in (792730864, 1505424382):
        if w not in ways: continue
        k = index_of(w); t = ways[w]["tags"]
        if k is not None: raised.append({"index": k, "levels": int(t.get("building:levels", "3")), "min_level": int(t.get("building:min_level", "1"))})
    er_ents = [{"x": round(x, 2), "z": round(z, 2)} for x, z, t in nodes.values() if t and t.get("emergency") == "emergency_ward_entrance" and math.dist((x, z), CAMPUS) < CAMPUS_R]
    er = {"index": index_of(ER_OSM), "canopy": index_of(ER_CANOPY_OSM), "entrances": er_ents}
    podium = index_of(PODIUM_OSM)
    # the decks: every building=parking on the map
    decks = []
    for k, b in enumerate(B):
        if b["type"] != "parking": continue
        q = np.array(b["footprint"]).reshape(-1, 2)
        rect = deck_rect(q)
        if rect is None: continue
        c, U, hl, hw, fill = rect
        lv = max(2, int(round(b["height"] / (3.4 * m.get("vertical_scale", 1.0)))))
        decks.append({"index": k, "centre": [round(c[0], 2), round(c[1], 2)], "u": [round(U[0], 4), round(U[1], 4)], "hl": round(hl, 2), "hw": round(hw, 2),
                      "levels": lv, "fill": round(fill, 2)})
    out = {"note": "Round 77 (Tools/mapgen/make_kennestone.py): Kennestone hospital and the map's parking decks, read by MapBuilder.Kennestone.cs",
           "owned": owned, "parts": parts, "podium": podium, "raised": raised, "er": er, "portico": {"loop": list(LOOP), "on": podium}, "decks": decks}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
    print(f"owned {owned}; {len(parts)} building parts (towers up to {max(p['levels'] for p in parts)} levels); podium {podium}; raised {raised}; ER {er['index']} "
          f"canopy {er['canopy']} ({len(er_ents)} entrances); {len(decks)} decks:")
    for d in decks: print(f"  deck {d['index']} at ({d['centre'][0]:.0f}, {d['centre'][1]:.0f}) {2 * d['hl']:.0f} x {2 * d['hw']:.0f} m, {d['levels']} levels (fills {d['fill']:.0%} of the footprint's box)")


def deck_rect(q):
    """The rectangle a deck is built in: the footprint's smallest box along one of its edges; where the footprint fills less than 85 % of it, the
    largest rectangle inside the footprint in that box's frame (1 m raster). (centre, long axis, half length, half width, fill)"""
    best = None
    for i in range(len(q)):
        d = q[(i + 1) % len(q)] - q[i]; L = np.hypot(*d)
        if L < 1e-3: continue
        d = d / L; p = np.array([-d[1], d[0]])
        a = q @ d; b = q @ p; area = (a.max() - a.min()) * (b.max() - b.min())
        if best is None or area < best[0]: best = (area, d, p, a.min(), a.max(), b.min(), b.max())
    if best is None: return None
    area, d, p, a0, a1, b0, b1 = best
    poly = abs(sum(q[i][0] * q[(i + 1) % len(q)][1] - q[(i + 1) % len(q)][0] * q[i][1] for i in range(len(q)))) / 2
    fill = poly / max(area, 1e-6)
    if fill < 0.85:
        nx, ny = int(a1 - a0) + 1, int(b1 - b0) + 1
        inside = np.zeros((ny, nx), bool)
        qa = q @ d; qb = q @ p
        for j in range(ny):
            for i in range(nx):
                x, y = a0 + i + .5, b0 + j + .5; ins = False
                for k in range(len(q)):
                    xa, ya, xb, yb = qa[k], qb[k], qa[(k + 1) % len(q)], qb[(k + 1) % len(q)]
                    if (ya > y) != (yb > y) and x < xa + (y - ya) * (xb - xa) / (yb - ya): ins = not ins
                inside[j, i] = ins
        h = np.zeros(nx, int); bestR = (0, 0, 0, 0, 0)
        for j in range(ny):
            h = np.where(inside[j], h + 1, 0)
            stack = []
            for i in range(nx + 1):
                cur = h[i] if i < nx else 0
                start = i
                while stack and stack[-1][1] >= cur:
                    s, hh = stack.pop(); ar = hh * (i - s)
                    if ar > bestR[0]: bestR = (ar, s, i, j - hh + 1, j + 1)
                    start = s
                stack.append((start, cur))
        _, i0, i1, j0, j1 = bestR
        a0, a1, b0, b1 = a0 + i0, a0 + i1, b0 + j0, b0 + j1
    c = d * (a0 + a1) / 2 + p * (b0 + b1) / 2
    hl, hw = (a1 - a0) / 2 - .3, (b1 - b0) / 2 - .3
    if hw > hl: d, hl, hw = p, hw, hl
    return c, d, hl, hw, fill


if __name__ == "__main__":
    main()
