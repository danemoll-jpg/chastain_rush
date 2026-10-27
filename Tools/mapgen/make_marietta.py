"""Round 76 (B, Dan, 2026-10-27: "MARIETTA SQUARE MADE TO LOOK LIKE ITSELF"): what the map build needs to dress downtown Marietta, from the piece's
OSM download (raw/osm_ext3.json, mapgen_piece.py) and Data/map.json. Writes Data/tracks/marietta_square.json (world, not game layer):
  park        Glover Park's outline (the square's lawn, paths, fountain, benches, lamps, trees)
  gazebo      the bandstand's footprint (map.json building index and outline)
  historic    the buildings round the square (within HISTORIC_M of the park, not the big civic blocks, decks or apartments): the old two- and
              three-story brick storefronts (MapBuilder.Businesses.cs takes them as shops with a brick front, upper windows and a cornice)
  landmarks   theater, market, museum, depot: map.json building index each (their own fronts and signs, MapBuilder.MariettaSquare.cs)
  churches    places of worship within CHURCH_M of the square (dressed as churches)
  rail        the railroad's lines within RAIL_M of the square (rails on ties, MapBuilder.MariettaSquare.cs)
  place       the fast-travel spot "Marietta Square" (on the street at the park's east side, facing the park)
Every name shown in the game is made up (Tools/handmade/make_business_signs.py's "marietta" rows); no OSM name is drawn.
Re-run after mapgen (map.json's indices), then Build Map. Usage: python Tools/mapgen/make_marietta.py"""
import json
import math
import os

from pyproj import Transformer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Data", "tracks", "marietta_square.json")
HISTORIC_M, CHURCH_M, RAIL_M = 75.0, 700.0, 650.0
GLOVER_PARK, GAZEBO = 245388613, 284555404
LANDMARKS = {"theater": 245388644, "market": 245388640, "museum": 245388643, "depot": 321673366}
NOT_HISTORIC = {"public", "civic", "parking", "apartments", "church", "school", "industrial", "roof", "garage", "house", "detached"}


def main():
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    osm = json.load(open(os.path.join(ROOT, "raw", "osm_ext3.json"), encoding="utf-8"))
    cfg = json.load(open(os.path.join(ROOT, "Tools", "mapgen", "config.json"), encoding="utf-8"))
    ox, oz = m["origin_utm"]
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    nodes = {}
    for e in osm["elements"]:
        if e["type"] == "node":
            x, y = tr.transform(e["lon"], e["lat"]); nodes[e["id"]] = (round(x - ox, 2), round(y - oz, 2))
    ways = {e["id"]: e for e in osm["elements"] if e["type"] == "way"}
    ring = lambda w: [nodes[n] for n in ways[w]["nodes"] if n in nodes]
    cen = lambda q: (sum(p[0] for p in q) / len(q), sum(p[1] for p in q) / len(q))
    B = m["buildings"]
    px0, pz0, px1, pz1 = m["pieces"][:4]   # the piece, and the old map's buildings just north of it (the market stands there)
    bc = [(k, cen(list(zip(B[k]["footprint"][0::2], B[k]["footprint"][1::2])))) for k in range(len(B))]
    bc = [(k, c) for k, c in bc if px0 - 50 <= c[0] <= px1 + 50 and pz0 - 50 <= c[1] <= pz1 + 400]
    pos = {k: c for k, c in bc}

    def index_of(w):
        c = cen(ring(w)[:-1] if ring(w)[0] == ring(w)[-1] else ring(w))
        k, d = min(((k, math.dist(c, q)) for k, q in bc), key=lambda t: t[1])
        return k if d < 2.0 else None

    park = ring(GLOVER_PARK)[:-1]
    pc = cen(park)

    def seg_d(p, a, b):
        dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz
        t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L))
        return math.hypot(a[0] + t * dx - p[0], a[1] + t * dz - p[1])
    to_park = lambda p: min(seg_d(p, park[i], park[(i + 1) % len(park)]) for i in range(len(park)))

    marks = {k: index_of(w) for k, w in LANDMARKS.items()}
    # the bandstand: the gazebo inside Glover Park (OSM maps two; the named one stands elsewhere); the park's other buildings (a hut, a small
    # public building) are left out so the lawn is open (park_drop)
    def in_park(q):
        x, y = q; ins = False
        for i in range(len(park)):
            (x1, y1), (x2, y2) = park[i], park[(i + 1) % len(park)]
            if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1): ins = not ins
        return ins
    inside = [k for k, c in bc if in_park(c)]
    gz = next((k for k in inside if B[k]["type"] == "gazebo"), index_of(GAZEBO))
    park_drop = [k for k in inside if k != gz]
    taken = set(v for v in marks.values() if v is not None) | {gz} | set(park_drop)
    historic = []
    for k, c in bc:
        if k in taken: continue
        b = B[k]; q = list(zip(b["footprint"][0::2], b["footprint"][1::2]))
        area = abs(sum(q[i][0] * q[(i + 1) % len(q)][1] - q[(i + 1) % len(q)][0] * q[i][1] for i in range(len(q)))) / 2
        if b["type"] in NOT_HISTORIC or area < 60 or area > 3500: continue
        if min(to_park(p) for p in q) <= HISTORIC_M: historic.append(k)
    churches = []
    for w, e in ways.items():
        t = e.get("tags", {})
        if t.get("amenity") == "place_of_worship" or t.get("building") == "church":
            k = index_of(w)
            if k is not None and math.dist(pos[k], pc) <= CHURCH_M and k not in taken: churches.append(k)
    rail = []
    for w, e in ways.items():
        if e.get("tags", {}).get("railway") != "rail": continue
        pts = ring(w)
        run = []
        for p in pts:
            if math.dist(p, pc) <= RAIL_M: run.append(p)
            elif run:
                if len(run) >= 2: rail.append(run)
                run = []
        if len(run) >= 2: rail.append(run)
    # the place: on the street just east of the park, looking west over it
    east = max(park, key=lambda p: p[0])
    place = {"name": "Marietta Square", "x": round(east[0] + 14.0, 1), "z": round(pc[1], 1), "heading": 270.0}
    out = {"note": "Round 76 (Tools/mapgen/make_marietta.py): Marietta Square's dressing data, read by MapBuilder.MariettaSquare.cs and MapBuilder.Businesses.cs",
           "park": [round(v, 2) for p in park for v in p], "park_center": [round(pc[0], 2), round(pc[1], 2)],
           "gazebo": gz, "park_drop": park_drop, "historic": sorted(historic), "landmarks": marks, "churches": sorted(set(churches)),
           "rail": [{"points": [round(v, 2) for p in r for v in p]} for r in rail], "place": place}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
    print(f"park {len(park)} points at ({pc[0]:.0f}, {pc[1]:.0f}); gazebo {gz}, left out of the park {park_drop}; landmarks {marks}; {len(historic)} historic storefronts; "
          f"{len(set(churches))} churches; {len(rail)} rail lines ({sum(sum(math.dist(a, b) for a, b in zip(r, r[1:])) for r in rail):.0f} m); place {place}")


if __name__ == "__main__":
    main()
