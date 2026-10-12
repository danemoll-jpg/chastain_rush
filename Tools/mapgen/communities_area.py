"""Round 22: the other parts of the community, dressed like Chastain Glen (Round 7): Grasmere Court (part of Chastain Glen),
Chastain Commons (Weybourne Court and its side streets) and Chastain Enclave (Rotherhithe Landing, Bloombridge Way and
their side streets, with its pool and tennis courts). From Data/map.json and the cached OSM download (raw/osm_box.json).

For each community: its street pieces (reached from a seed street without going out onto New Chastain Road or a bigger
road), its houses (the same rule as neighborhood_area.py: a gable roof, 55 m2 or more, nearest to one of its streets and
within 60 m, not already a Chastain Glen house), and its entrance (where one of its streets meets New Chastain Road).
The Enclave also gets its pool, tennis courts and the two small buildings beside them (OSM, inside the Rotherhithe loop).
Writes Data/neighborhood/communities.json (read by the map builder, MapBuilder.Communities.cs) and prints a report,
including the streets near Chastain Glen that are still plain boxes. Re-run after every mapgen run (indices change).

Usage: python Tools/mapgen/communities_area.py
"""
import json, math, os, collections
from pyproj import Transformer

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
m = json.load(open(os.path.join(ROOT, 'Data/map.json')))
R, B = m['roads'], m['buildings']
glen = json.load(open(os.path.join(ROOT, 'Data/neighborhood/chastain_glen_area.json')))

# key, name, seed streets (by name), streets left out (they belong to a neighborhood south of New Chastain Road)
COMMUNITIES = [
    ('grasmere', 'Grasmere Court', ['Grasmere Court Northeast'], []),
    ('commons', 'Chastain Commons', ['Weybourne Court Northeast'], []),
    ('enclave', 'Chastain Enclave', ['Rotherhithe Landing', 'Bloombridge Way', 'Chastain Crossing'],
     ['Chastain Crossing Northeast', 'Chastain Court Northeast']),
]
STREET_CLASSES = ('residential', 'service', 'living_street', 'unclassified')
MAIN = 'New Chastain Road'

def pts(flat): return list(zip(flat[0::2], flat[1::2]))
def key(p): return (round(p[0] * 2), round(p[1] * 2))
def seg_d(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]; L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / L))
    return math.hypot(a[0] + t * dx - p[0], a[1] + t * dy - p[1])
def road_d(i, p):
    q = pts(R[i]['points']); return min(seg_d(p, q[k], q[k + 1]) for k in range(len(q) - 1))
def area(q): return abs(sum(q[i][0] * q[(i + 1) % len(q)][1] - q[(i + 1) % len(q)][0] * q[i][1] for i in range(len(q)))) / 2
def cent(q): return (sum(x for x, _ in q) / len(q), sum(z for _, z in q) / len(q))

node_roads = collections.defaultdict(set)
for i, r in enumerate(R):
    for p in pts(r['points']): node_roads[key(p)].add(i)
glen_houses = set(glen['buildings'])
glen_roads = set(glen['roads'])
streetish = [i for i in range(len(R)) if R[i]['class'] not in ('footway', 'path', 'cycleway', 'track', 'steps', 'pedestrian')]

def streets_of(seeds, leave_out):
    start = [i for i, r in enumerate(R) if r['name'] in seeds]
    seen = set(start); queue = list(start)
    while queue:
        i = queue.pop()
        for p in pts(R[i]['points']):
            for j in node_roads[key(p)]:
                r = R[j]
                if j in seen or r['name'] == MAIN or r['name'] in leave_out or r['class'] not in STREET_CLASSES: continue
                seen.add(j); queue.append(j)
    return sorted(seen)

def houses_of(roads):
    xs = [x for i in roads for x, _ in pts(R[i]['points'])]; zs = [z for i in roads for _, z in pts(R[i]['points'])]
    bb = (min(xs) - 70, max(xs) + 70, min(zs) - 70, max(zs) + 70)
    others = [i for i in streetish if i not in roads]
    out = []
    for k, b in enumerate(B):
        q = pts(b['footprint']); c = cent(q)
        if not (bb[0] <= c[0] <= bb[1] and bb[2] <= c[1] <= bb[3]) or k in glen_houses: continue
        if b['roof'] != 'gable' or area(q) < 55: continue
        da = min(road_d(i, c) for i in roads)
        if da > 60: continue
        if min([road_d(i, c) for i in others] or [1e9]) < da - 4: continue   # belongs to another street
        out.append(k)
    return out

def entrances_of(roads):
    """Points where one of the community's streets meets New Chastain Road, with the direction into the community."""
    out = []
    for i in roads:
        q = pts(R[i]['points'])
        for end, nxt in ((0, 1), (len(q) - 1, len(q) - 2)):
            if any(R[j]['name'] == MAIN for j in node_roads[key(q[end])]):
                d = (q[nxt][0] - q[end][0], q[nxt][1] - q[end][1]); L = math.hypot(*d)
                out.append(dict(x=round(q[end][0], 2), z=round(q[end][1], 2), dx=round(d[0] / L, 4), dz=round(d[1] / L, 4), road=i, width=R[i]['width']))
    return out

# the Enclave's amenity block from OSM (pool, tennis courts), and the two small buildings beside them in the map data
def enclave_amenities():
    ox, oy = m['origin_utm']
    tr = Transformer.from_crs(4326, m['utm_epsg'], always_xy=True)
    els = json.load(open(os.path.join(ROOT, 'raw/osm_box.json')))['elements']
    nodes = {e['id']: e for e in els if e['type'] == 'node'}
    def loc(n):
        x, y = tr.transform(n['lon'], n['lat']); return [round(x - ox, 2), round(y - oy, 2)]
    pool, courts = None, []
    for e in els:
        t = e.get('tags', {})
        if e['type'] != 'way': continue
        ring = [loc(nodes[n]) for n in e['nodes'] if n in nodes]
        if not ring: continue
        c = cent(ring)
        if not (1700 <= c[0] <= 1900 and 720 <= c[1] <= 840): continue
        if t.get('leisure') == 'swimming_pool': pool = dict(osm_id=e['id'], outline=[v for p in ring for v in p])
        if t.get('leisure') == 'pitch' and t.get('sport') == 'tennis': courts.append(dict(osm_id=e['id'], outline=[v for p in ring for v in p]))
    courts.sort(key=lambda c: cent(pts(c['outline']))[0])
    pc = cent(pts(pool['outline']))
    near = []
    for k, b in enumerate(B):
        if b['roof'] == 'gable': continue
        c = cent(pts(b['footprint']))
        if math.dist(c, pc) < 45: near.append((math.dist(c, pc), k, c))
    near.sort()
    clubhouse = near[0][1] if near else -1                      # the one beside the pool (on its west side)
    gazebo = near[1][1] if len(near) > 1 else -1                # the one by the courts' west end
    return dict(pool=pool, courts=courts, clubhouse_building=clubhouse, gazebo_building=gazebo)

out = dict(note='Round 22 (Tools/mapgen/communities_area.py). Read by MapBuilder.Communities.cs.', communities=[])
for k, name, seeds, leave in COMMUNITIES:
    roads = [i for i in streets_of(seeds, leave) if i not in glen_roads]
    houses = houses_of(roads)
    c = dict(key=k, name=name, roads=roads, houses=houses, entrances=entrances_of(roads))
    if k == 'enclave': c['amenities'] = enclave_amenities()
    out['communities'].append(c)
    print(f"{name}: {len(roads)} street pieces ({', '.join(sorted(set(R[i]['name'] or '(unnamed)' for i in roads)))}), {len(houses)} houses, "
          f"entrances {[(e['x'], e['z'], R[e['road']]['name']) for e in c['entrances']]}")
    if k == 'enclave':
        a = c['amenities']
        print(f"  amenities: pool {a['pool']['osm_id'] if a['pool'] else None}, {len(a['courts'])} tennis courts, clubhouse building {a['clubhouse_building']}, gazebo building {a['gazebo_building']}")
json.dump(out, open(os.path.join(ROOT, 'Data/neighborhood/communities.json'), 'w'))

# report only: residential streets near Chastain Glen that are not dressed (no community), with their house counts
taken = set(glen['roads']) | {i for c in out['communities'] for i in c['roads']}
gx = [x for i in glen['roads'] for x, _ in pts(R[i]['points'])]; gz = [z for i in glen['roads'] for _, z in pts(R[i]['points'])]
box = (min(gx) - 500, max(gx) + 500, min(gz) - 400, max(gz) + 400)
plain = collections.OrderedDict()
for i, r in enumerate(R):
    if i in taken or r['class'] != 'residential' or not r['name']: continue
    q = pts(r['points']); c = cent(q)
    if not (box[0] <= c[0] <= box[1] and box[2] <= c[1] <= box[3]): continue
    plain.setdefault(r['name'], []).append(i)
print('streets near Chastain Glen that are still plain boxes (residential, not dressed):')
for nm, ids in plain.items():
    hs = houses_of(ids)
    c = cent([p for i in ids for p in pts(R[i]['points'])])
    print(f"  {nm:34s} about x {c[0]:.0f}, z {c[1]:.0f}: {len(hs)} houses")
