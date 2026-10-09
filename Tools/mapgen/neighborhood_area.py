"""Round 7 item 1: the Chastain Glen area, from Data/map.json.

The streets reached from the entrance (the map spawn) without going out onto New Chastain Road, the buildings that
belong to them, and the ponds inside. Writes Data/neighborhood/chastain_glen_area.json and prints a report.
Re-run after every mapgen run (road and building indices change when the map is regenerated)."""
import json, math, os, sys, collections
root = os.path.join(os.path.dirname(__file__), '..', '..')
m = json.load(open(os.path.join(root, 'Data/map.json')))
R, B, W = m['roads'], m['buildings'], m['water']
sp = (m['spawn']['x'], m['spawn']['z'])

def pts(flat): return list(zip(flat[0::2], flat[1::2]))
def seg_d(p, a, b):
    dx, dy = b[0]-a[0], b[1]-a[1]; L = dx*dx+dy*dy
    t = 0 if L == 0 else max(0, min(1, ((p[0]-a[0])*dx+(p[1]-a[1])*dy)/L))
    return math.hypot(a[0]+t*dx-p[0], a[1]+t*dy-p[1])
def road_d(r, p):
    q = pts(r['points']); return min(seg_d(p, q[i], q[i+1]) for i in range(len(q)-1))
def key(p): return (round(p[0]*2), round(p[1]*2))

# road graph through shared end/interior nodes; stop at New Chastain Road
node_roads = collections.defaultdict(set)
for i, r in enumerate(R):
    for p in pts(r['points']): node_roads[key(p)].add(i)
start = min(range(len(R)), key=lambda i: road_d(R[i], sp))
BLOCK = lambda r: r['name'] == 'New Chastain Road'
seen = {start}; queue = [start]
while queue:
    i = queue.pop()
    for p in pts(R[i]['points']):
        for j in node_roads[key(p)]:
            if j not in seen and not BLOCK(R[j]) and R[j]['class'] not in ('primary', 'trunk', 'motorway', 'secondary', 'tertiary'):
                seen.add(j); queue.append(j)
area_roads = sorted(seen)
named = collections.OrderedDict()
for i in area_roads:
    n = R[i]['name'] or '(unnamed)'
    named.setdefault(n, []).append(i)

# buildings: nearest road is an area road, within 60 m of it, and not a tiny shed
others = [i for i in range(len(R)) if i not in seen]
def bcent(b):
    q = pts(b['footprint']); return (sum(x for x, _ in q)/len(q), sum(z for _, z in q)/len(q))
def area(q): return abs(sum(q[i][0]*q[(i+1) % len(q)][1]-q[(i+1) % len(q)][0]*q[i][1] for i in range(len(q))))/2
# coarse candidate filter by bounding box of area roads
xs = [x for i in area_roads for x, _ in pts(R[i]['points'])]; zs = [z for i in area_roads for _, z in pts(R[i]['points'])]
bb = (min(xs)-90, max(xs)+90, min(zs)-90, max(zs)+90)
members = []
for k, b in enumerate(B):
    c = bcent(b)
    if not (bb[0] <= c[0] <= bb[1] and bb[2] <= c[1] <= bb[3]): continue
    da = min(road_d(R[i], c) for i in area_roads)
    if da > 60: continue
    do = min([road_d(R[i], c) for i in others if road_d(R[i], c) < 200] or [1e9])
    if do < da - 4: continue          # belongs to a street outside the area
    members.append((k, da, area(pts(b['footprint']))))
houses = [k for k, da, a in members if B[k]['roof'] == 'gable' and a >= 55]
other = [k for k, da, a in members if k not in houses]

# ponds inside: water polygons within 120 m of an area road
def poly_c(o): q = pts(o); return (sum(x for x, _ in q)/len(q), sum(z for _, z in q)/len(q))
ponds = []
for wi, w in enumerate(W):
    q = pts(w['outline'])
    d = min(min(road_d(R[i], p) for i in area_roads) for p in q)
    if d < 120: ponds.append(dict(index=wi, name=w['name'], centre=[round(v, 1) for v in poly_c(w['outline'])], area_m2=round(area(q)), nearest_road_m=round(d, 1)))
owner = json.load(open(os.path.join(root, 'local/owner_marker.json'))) if os.path.exists(os.path.join(root, 'local/owner_marker.json')) else None
out = dict(entrance=dict(x=sp[0], z=sp[1]), roads=area_roads, buildings=sorted(houses+other), houses=sorted(houses), other_buildings=sorted(other),
           ponds=ponds, bounds=dict(x0=bb[0], x1=bb[1], z0=bb[2], z1=bb[3]))
# the owner's house is NOT recorded here (privacy): the map builder reads it from the git-ignored local/owner_marker.json
os.makedirs(os.path.join(root, 'Data/neighborhood'), exist_ok=True)
json.dump(out, open(os.path.join(root, 'Data/neighborhood/chastain_glen_area.json'), 'w'))
print('streets (road pieces, metres):')
for n, ids in named.items():
    print('  %-34s %2d pieces %5.0f m' % (n, len(ids), sum(sum(math.dist(a, b) for a, b in zip(pts(R[i]['points']), pts(R[i]['points'])[1:])) for i in ids)))
print('road pieces', len(area_roads), '| buildings', len(out['buildings']), '| houses (gable, >=55 m2)', len(houses), '| other', len(other))
print('ponds:', ponds)
print('owner house inside the area:', bool(owner and owner['index'] in houses))
