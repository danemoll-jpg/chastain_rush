"""Round 7: small features of Chastain Glen that mapgen does not carry into map.json: the pool, its parking lot, the
playground, the fountain node and the gate, from the cached OSM download (raw/osm_box.json). Writes
Data/neighborhood/extras.json in the map's world frame (x east, z north, metres). Re-run after a mapgen run."""
import json, os
from pyproj import Transformer
root = os.path.join(os.path.dirname(__file__), '..', '..')
m = json.load(open(os.path.join(root, 'Data/map.json')))
a = json.load(open(os.path.join(root, 'Data/neighborhood/chastain_glen_area.json')))
ox, oy = m['origin_utm']
tr = Transformer.from_crs(4326, m['utm_epsg'], always_xy=True)
els = json.load(open(os.path.join(root, 'raw/osm_box.json')))['elements']
nodes = {e['id']: e for e in els if e['type'] == 'node'}
b = a['bounds']
def loc(n):
    x, y = tr.transform(n['lon'], n['lat']); return [round(x-ox, 2), round(y-oy, 2)]
def inside(p): return b['x0'] <= p[0] <= b['x1'] and b['z0'] <= p[1] <= b['z1']
out = dict(pool=None, parking=None, playground=None, fountain=None, gate=None)
for e in els:
    t = e.get('tags', {})
    if e['type'] == 'way' and t.get('leisure') in ('swimming_pool', 'playground') or e['type'] == 'way' and t.get('amenity') == 'parking':
        ring = [loc(nodes[n]) for n in e['nodes'] if n in nodes]
        if not ring or not inside(ring[0]): continue
        kind = 'pool' if t.get('leisure') == 'swimming_pool' else 'playground' if t.get('leisure') == 'playground' else 'parking'
        out[kind] = dict(osm_id=e['id'], outline=[v for p in ring for v in p], tags=t)
    if e['type'] == 'node' and t.get('amenity') == 'fountain':
        p = loc(e)
        if inside(p): out['fountain'] = dict(osm_id=e['id'], x=p[0], z=p[1])
    if e['type'] == 'node' and t.get('barrier') == 'gate':
        p = loc(e)
        if inside(p): out['gate'] = dict(osm_id=e['id'], x=p[0], z=p[1])
json.dump(out, open(os.path.join(root, 'Data/neighborhood/extras.json'), 'w'))
for k, v in out.items(): print(k, None if v is None else {kk: vv for kk, vv in v.items() if kk != 'tags'})
