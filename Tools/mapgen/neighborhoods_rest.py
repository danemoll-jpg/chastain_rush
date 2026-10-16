"""Round 30 (Part B): every neighborhood not yet dressed (Chastain Glen, Grasmere Court, Chastain Commons and Chastain Enclave
are), grouped by its streets, with a look of its own picked from its footprints. From Data/map.json, the Round 7 / Round 22
lists and the cached OSM download (raw/osm_box.json, for the few subdivision names OSM has).

A neighborhood: the residential streets joined to each other without going out onto a bigger road (connected through shared
nodes; service roads, parking aisles and drives do not join two neighborhoods). Its houses: the same rule as Round 7 / 22 (a
gable roof, 55 m2 or more, within 60 m of one of its streets and not nearer another street), not already dressed. Its name:
the OSM residential area it lies in, when OSM names one; else its main street ("Parkwood Drive").

Its era, from the footprints (Dan has no styles for these; Code picks believable East Cobb ones, see ERA below):
  townhomes   most footprints are long rows (several units joined), or the houses stand under 18 m apart: townhome rows
  ranch       wide lots (median spacing 32 m or more) and modest footprints (under 245 m2, garage included): the older
              subdivisions, 1970s ranches and split-levels
  craftsman   big footprints (200 m2 or more) packed close (spacing under 26 m): the newest, 2000s craftsman-style
  traditional the rest (about 26 to 32 m apart): 1980s to 1990s two-story traditionals
(OSM footprints include the garage and are coarse, so the spacing between houses, the lot width, says the most.)
Writes Data/neighborhood/neighborhoods.json (read by the map builder, MapBuilder.Neighborhoods.cs) and prints the list.
Re-run after every mapgen run (indices change).

Usage: python Tools/mapgen/neighborhoods_rest.py
"""
import json, math, os, collections, statistics
from pyproj import Transformer

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
m = json.load(open(os.path.join(ROOT, 'Data/map.json')))
R, B = m['roads'], m['buildings']
glen = json.load(open(os.path.join(ROOT, 'Data/neighborhood/chastain_glen_area.json')))
comm = json.load(open(os.path.join(ROOT, 'Data/neighborhood/communities.json')))
STREETS = ('residential', 'living_street', 'unclassified')

def pts(flat): return list(zip(flat[0::2], flat[1::2]))
def key(p): return (round(p[0] * 2), round(p[1] * 2))
def seg_d(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]; L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / L))
    return math.hypot(a[0] + t * dx - p[0], a[1] + t * dy - p[1])
def area(q): return abs(sum(q[i][0] * q[(i + 1) % len(q)][1] - q[(i + 1) % len(q)][0] * q[i][1] for i in range(len(q)))) / 2
def cent(q): return (sum(x for x, _ in q) / len(q), sum(z for _, z in q) / len(q))
def in_poly(p, ring):
    x, y = p; inside = False
    for i in range(len(ring)):
        (x1, y1), (x2, y2) = ring[i], ring[(i + 1) % len(ring)]
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1): inside = not inside
    return inside
def rect_aspect(q):
    """long / short side of the footprint's minimum-area rectangle (its edges' directions tried)"""
    best = None
    for i in range(len(q)):
        a, b = q[i], q[(i + 1) % len(q)]; L = math.dist(a, b)
        if L < 1e-3: continue
        ux, uy = (b[0] - a[0]) / L, (b[1] - a[1]) / L
        us = [x * ux + y * uy for x, y in q]; vs = [-x * uy + y * ux for x, y in q]
        w, h = max(us) - min(us), max(vs) - min(vs)
        if best is None or w * h < best[0]: best = (w * h, max(w, h) / max(.1, min(w, h)), max(w, h))
    return best[1], best[2]

taken_roads = set(glen['roads']) | {i for c in comm['communities'] for i in c['roads']}
taken_houses = set(glen['buildings']) | {i for c in comm['communities'] for i in c['houses']}
streets = [i for i, r in enumerate(R) if r['class'] in STREETS and i not in taken_roads]
node_roads = collections.defaultdict(set)
for i in streets:
    for p in pts(R[i]['points']): node_roads[key(p)].add(i)
road_pts = {i: pts(R[i]['points']) for i in range(len(R))}
def road_d(i, p): q = road_pts[i]; return min(seg_d(p, q[k], q[k + 1]) for k in range(len(q) - 1))

# the neighborhoods: streets joined through shared nodes
group_of = {}; groups = []
for s in streets:
    if s in group_of: continue
    g = [s]; group_of[s] = len(groups); queue = [s]
    while queue:
        i = queue.pop()
        for p in road_pts[i]:
            for j in node_roads[key(p)]:
                if j not in group_of: group_of[j] = len(groups); g.append(j); queue.append(j)
    groups.append(g)

# houses to their nearest street (any street class but footways), within 60 m
allstreets = [i for i, r in enumerate(R) if r['class'] not in ('footway', 'path', 'cycleway', 'track', 'steps', 'pedestrian')]
grid = collections.defaultdict(list)
for i in allstreets:
    for x, z in road_pts[i]: grid[(int(x // 60), int(z // 60))].append(i)
houses_of = collections.defaultdict(list)
for k, b in enumerate(B):
    if k in taken_houses or b['roof'] != 'gable': continue
    q = pts(b['footprint'])
    if area(q) < 55: continue
    c = cent(q); cx, cz = int(c[0] // 60), int(c[1] // 60)
    near = {i for dx in (-1, 0, 1) for dz in (-1, 0, 1) for i in grid[(cx + dx, cz + dz)]}
    if not near: continue
    best = min(near, key=lambda i: road_d(i, c)); d = road_d(best, c)
    if d > 60 or best not in group_of: continue
    houses_of[group_of[best]].append(k)

# the few subdivision names OSM has (residential areas)
ox, oy = m['origin_utm']; tr = Transformer.from_crs(4326, m['utm_epsg'], always_xy=True)
els = json.load(open(os.path.join(ROOT, 'raw/osm_box.json')))['elements']
nodes = {e['id']: e for e in els if e['type'] == 'node'}
def loc(n): x, y = tr.transform(n['lon'], n['lat']); return (x - ox, y - oy)
named = []
for e in els:
    t = e.get('tags', {})
    if e['type'] == 'way' and t.get('landuse') == 'residential' and t.get('name'):
        ring = [loc(nodes[n]) for n in e['nodes'] if n in nodes]
        if len(ring) >= 3: named.append((t['name'], ring))

ERA = {
    'ranch': '1970s ranches and split-levels: one story or split, brick and painted wood siding, low roofs, carports and one-car garages',
    'traditional': '1980s to 1990s two-story traditionals: brick fronts, siding sides and back, two-car garages, a bonus room over the garage',
    'craftsman': '2000s craftsman-style: stone or brick wainscot, front gables, covered porches with tapered columns',
    'townhomes': 'townhome rows: two-story units joined in a row, each with its own door, a one-car garage or a stoop',
}
# Round 32 (C1): names OSM does not have, by the name the main street gave. Lakeside Drive is the Chastain Lakes subdivision.
NAME_OVERRIDE = {'Lakeside Drive': 'Chastain Lakes'}
out = dict(note='Round 30 (Tools/mapgen/neighborhoods_rest.py). Read by MapBuilder.Neighborhoods.cs.', neighborhoods=[])
rows = []
for gi, g in enumerate(groups):
    hs = houses_of.get(gi, [])
    if len(hs) < 4: continue
    feet = [pts(B[k]['footprint']) for k in hs]
    areas = [area(q) for q in feet]; cs = [cent(q) for q in feet]
    aspects = [rect_aspect(q) for q in feet]
    spacing = [min(math.dist(c, o) for j, o in enumerate(cs) if j != i) for i, c in enumerate(cs)]
    med_a, med_s = statistics.median(areas), statistics.median(spacing)
    rowish = sum(1 for (asp, L), a in zip(aspects, areas) if (asp >= 2.4 and L >= 24 and a >= 180)) / len(hs)
    if rowish >= .4 or med_s < 18: era = 'townhomes'
    elif med_s >= 32 and med_a < 245: era = 'ranch'
    elif med_s < 26 and med_a >= 200: era = 'craftsman'
    else: era = 'traditional'
    allc = cent([p for i in g for p in road_pts[i]])
    name = next((n for n, ring in named if sum(in_poly(c, ring) for c in cs) > len(cs) / 2), None)
    lengths = collections.Counter()
    for i in g:
        q = road_pts[i]; lengths[R[i]['name'] or ''] += sum(math.dist(q[k], q[k + 1]) for k in range(len(q) - 1))
    main = next((n for n, _ in lengths.most_common() if n), 'Unnamed streets')
    names = sorted({R[i]['name'] for i in g if R[i]['name']})
    why = f"median footprint {med_a:.0f} m2, median spacing {med_s:.0f} m" + (f", {rowish:.0%} long rows" if rowish > 0 else "")
    nb = dict(key=f"n{len(out['neighborhoods'])}", name=name or (main.replace(' Northeast', '').replace(' Northwest', '')),
              osm_name=bool(name), era=era, roads=sorted(g), houses=sorted(hs), streets=names,
              median_area=round(med_a), median_spacing=round(med_s), rows=round(rowish, 2), x=round(allc[0]), z=round(allc[1]), why=why)
    nb['name'] = NAME_OVERRIDE.get(nb['name'], nb['name'])
    out['neighborhoods'].append(nb)
    rows.append(nb)
json.dump(out, open(os.path.join(ROOT, 'Data/neighborhood/neighborhoods.json'), 'w'))
print(f"{len(rows)} neighborhoods, {sum(len(n['houses']) for n in rows)} houses; per era: " +
      ', '.join(f"{e} {sum(1 for n in rows if n['era'] == e)} ({sum(len(n['houses']) for n in rows if n['era'] == e)} houses)" for e in ERA))
for n in rows:
    print(f"  {n['key']:4s} {n['name'][:34]:34s} {n['era']:11s} {len(n['houses']):4d} houses, x {n['x']}, z {n['z']}; {n['why']}; streets: {', '.join(n['streets'][:6])}{'...' if len(n['streets']) > 6 else ''}")
left = sum(1 for k, b in enumerate(B) if k not in taken_houses and b['roof'] == 'gable' and area(pts(b['footprint'])) >= 55) - sum(len(n['houses']) for n in rows)
print(f"gable houses not in any neighborhood (along big roads, or fewer than 4 on a street group): {left}")
