"""Chastain Rush map generator.

Re-runnable, driven by the bounds in config.json. Steps:
  1. fetch   - OpenStreetMap data for the bounds (cached in raw/)
  2. dem     - download USGS 1 m tile(s) (raw/), crop to the bounds -> Data/heightmap_1m.tif
  3. process - project OSM to UTM metres, build roads / buildings / water -> Data/map.json
  4. terrain - resample heightmap to the terrain grid, flatten under roads -> Assets/Generated/terrain.r16
  5. driveways - OSM driveways, plus a straight one from each house that has none to its nearest road (map.json)
  6. trees   - NAIP 4-band imagery (raw/naip_4band.tif) -> tree canopy -> a jittered-grid scatter -> Data/trees.bin

The crop is a rectangle (size_x_m by size_z_m) in whole UTM metres, clamped to the elevation tile's edge. The terrain is
written as one global height grid that Unity cuts into square-sample tiles; neighbouring tiles share their edge samples,
so there are no seams. Roads that cross water are held level on a deck (bridges, see plan_bridges).

World frame (shared with Unity): x = easting - origin_e, z = northing - origin_n,
y = elevation in metres (NAVD88). 1 unit = 1 metre horizontally. Vertical scale is 1.0 (true) unless
config.json "vertical_scale" says otherwise; it is applied about the lowest point of the crop, to terrain
(and so to the roads draped on it) and to building heights together.

Usage:  python Tools/mapgen/mapgen.py [--refresh-osm]
"""
import argparse
import json
import math
import os
import struct
import sys
import zlib
import urllib.parse
import urllib.request

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.merge import merge
from rasterio.windows import from_bounds
from pyproj import Transformer
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from scipy.spatial import cKDTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "raw")
DATA = os.path.join(ROOT, "Data")
GENERATED = os.path.join(ROOT, "Assets", "Generated")
LOCAL = os.path.join(ROOT, "local")
USER_AGENT = "chastain-rush-mapgen"

DRIVABLE = {"motorway", "trunk", "primary", "secondary", "tertiary", "primary_link", "secondary_link",
            "tertiary_link", "residential", "unclassified", "living_street", "service"}
# Round 31 (BUG-004): the I-575 / Chastain Road interchange's entrance and exit ramps. They are added after every other
# road (APPENDED), so the indices of the roads already in map.json, which other data files list, do not move.
APPENDED = {"motorway_link"}


def log(msg):
    print(msg, flush=True)


# ---------------------------------------------------------------- fetch

def osm_bbox(cfg, rect):
    """Lat/lon box that contains the UTM rectangle (plus a little), for the OSM query."""
    tr = Transformer.from_crs(cfg["utm_epsg"], 4326, always_xy=True)
    w, s, e, n = rect
    pts = [tr.transform(x, y) for x in (w, e) for y in (s, n)]
    return (min(p[1] for p in pts) - 0.0005, min(p[0] for p in pts) - 0.0005,
            max(p[1] for p in pts) + 0.0005, max(p[0] for p in pts) + 0.0005)  # S, W, N, E


def fetch_osm(cfg, refresh, rect):
    south, west, north, east = osm_bbox(cfg, rect)
    b = {"south": south, "west": west, "north": north, "east": east}
    path = os.path.join(RAW, "osm_box.json")
    key = f'{south:.5f},{west:.5f},{north:.5f},{east:.5f}'
    if os.path.exists(path) and not refresh:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        if data.get("_bounds_key") == key:
            log(f"OSM: using cache {path}")
            return data
    data = None
    query = (f"[out:json][timeout:120];(nwr({key}););(._;>;);out body;")
    try:
        req = urllib.request.Request("https://overpass-api.de/api/interpreter",
                                     data=urllib.parse.urlencode({"data": query}).encode(),
                                     headers={"User-Agent": USER_AGENT})
        data = json.load(urllib.request.urlopen(req, timeout=180))
        log("OSM: fetched from Overpass")
    except Exception as e:  # Overpass is often busy; the main API handles small boxes
        log(f"OSM: Overpass failed ({e}); trying the main OSM API")
        url = (f"https://api.openstreetmap.org/api/0.6/map.json?"
               f'bbox={b["west"]},{b["south"]},{b["east"]},{b["north"]}')
        req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        data = json.load(urllib.request.urlopen(req, timeout=300))
        log("OSM: fetched from the main API")
    data["_bounds_key"] = key
    os.makedirs(RAW, exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f)
    return data


# ---------------------------------------------------------------- dem

def tile_paths(cfg):
    os.makedirs(RAW, exist_ok=True)
    paths = []
    for url in cfg["elevation_tiles"]:
        p = os.path.join(RAW, url.rsplit("/", 1)[1])
        if not os.path.exists(p):
            log(f"DEM: downloading {url}")
            urllib.request.urlretrieve(url, p)
        paths.append(p)
    return paths


def utm_rect(cfg):
    """UTM rectangle (whole metres) that contains the lat/lon bounds, clamped to the elevation tile(s).
    Returns (rect, clamped) where clamped lists the sides that were cut at the tile edge."""
    b = cfg["bounds"]
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    xs, ys = zip(*[tr.transform(lon, lat) for lon in (b["west"], b["east"]) for lat in (b["south"], b["north"])])
    w, e, s, n = math.floor(min(xs)), math.ceil(max(xs)), math.floor(min(ys)), math.ceil(max(ys))
    srcs = [rasterio.open(p) for p in tile_paths(cfg)]
    tw, ts = min(r.bounds.left for r in srcs), min(r.bounds.bottom for r in srcs)
    te, tn = max(r.bounds.right for r in srcs), max(r.bounds.top for r in srcs)
    for r in srcs:
        r.close()
    tw, ts, te, tn = math.ceil(tw) + 1, math.ceil(ts) + 1, math.floor(te) - 1, math.floor(tn) - 1  # 1 m inside the edge
    clamped = []
    if w < tw: w = tw; clamped.append("west")
    if s < ts: s = ts; clamped.append("south")
    if e > te: e = te; clamped.append("east")
    if n > tn: n = tn; clamped.append("north")
    return (w, s, e, n), clamped


def build_heightmap(cfg, rect):
    srcs = [rasterio.open(p) for p in tile_paths(cfg)]
    w, s, e, n = rect
    arr, transform = merge(srcs, bounds=(w, s, e, n), res=1.0, nodata=-999999.0)
    arr = arr[0]
    missing = int((arr <= -9999).sum())
    if missing:
        raise SystemExit(f"DEM: {missing} cells in the crop have no data - add the neighbouring tile to config.json")
    out = os.path.join(DATA, "heightmap_1m.tif")
    profile = dict(driver="GTiff", height=arr.shape[0], width=arr.shape[1], count=1, dtype="float32",
                   crs=srcs[0].crs, transform=transform, compress="deflate", predictor=3, nodata=-999999.0)
    with rasterio.open(out, "w", **profile) as dst:
        dst.write(arr.astype(np.float32), 1)
    for src in srcs:
        src.close()
    log(f"DEM: cropped {arr.shape[1]} x {arr.shape[0]} m, elevation {arr.min():.1f}-{arr.max():.1f} m -> {out}")
    return out


# ---------------------------------------------------------------- process

def poly_area(pts):
    a = 0.0
    for i in range(len(pts)):
        x1, z1 = pts[i]
        x2, z2 = pts[(i + 1) % len(pts)]
        a += x1 * z2 - x2 * z1
    return a / 2


def road_width(tags, cfg):
    if "width" in tags:
        try:
            return float(tags["width"].split()[0])
        except ValueError:
            pass
    hw = tags["highway"]
    base = cfg["road_widths_m"][hw]
    if "lanes" in tags and hw not in ("residential", "service", "living_street"):
        try:
            lanes = int(tags["lanes"].split(";")[0])
            base = lanes * cfg["lane_width_m"] + 1.0
        except ValueError:
            pass
    if tags.get("oneway") == "yes" and hw.endswith("_link"):
        base = min(base, 6.5)
    if hw == "motorway_link":
        base = max(base, 6.0)   # Round 31: a one-lane ramp with its shoulders, wide enough to take flat out
    return round(base, 2)


def building_height(tags, cfg):
    """True (unscaled) height in metres; the caller applies vertical_scale."""
    for k in ("height", "building:height"):
        if k in tags:
            try:
                return float(tags[k].replace("m", "").strip())
            except ValueError:
                pass
    if "building:levels" in tags:
        try:
            return float(tags["building:levels"]) * cfg["level_height_m"] + 1.5
        except ValueError:
            pass
    h = cfg["building_heights_m"]
    return h.get(tags["building"], h["default"])


def vscale(cfg):
    k = float(cfg.get("vertical_scale", 1.0))
    if k <= 0:
        raise SystemExit(f"config.json vertical_scale must be > 0, got {k}")
    return k


def process_osm(cfg, osm, rect, owner_marker=True):   # Round 59: owner_marker False for the map extension (the core run writes it)
    w0, s0, e0, n0 = rect
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    nodes = {}
    for el in osm["elements"]:
        if el["type"] == "node":
            x, y = tr.transform(el["lon"], el["lat"])
            nodes[el["id"]] = (round(x - w0, 2), round(y - s0, 2))
    ways = {el["id"]: el for el in osm["elements"] if el["type"] == "way"}
    size_x, size_z = e0 - w0, n0 - s0

    def pts_of(way):
        return [nodes[n] for n in way["nodes"] if n in nodes]

    def inside(p, margin=0.0):
        return -margin <= p[0] <= size_x + margin and -margin <= p[1] <= size_z + margin

    roads, buildings, water, streams, woods, driveways = [], [], [], [], [], []
    ramps, parking = [], []   # Round 31: motorway_link ways (appended to roads at the end); surface parking lots
    owner_cfg = None
    owner_path = os.path.join(ROOT, cfg["owner_house_file"])
    if owner_marker and os.path.exists(owner_path):
        with open(owner_path, encoding="utf-8") as f:
            owner_cfg = json.load(f)
    owner = None

    for way in ways.values():
        t = way.get("tags", {})
        pts = pts_of(way)
        if len(pts) < 2:
            continue
        hw = t.get("highway")
        if hw in APPENDED:
            if any(inside(p) for p in pts):
                for run, ids in clip_runs(pts, [n for n in way["nodes"] if n in nodes], inside):
                    ramps.append({"name": t.get("name", ""), "class": hw, "width": road_width(t, cfg),
                                  "oneway": t.get("oneway") == "yes", "points": run, "node_ids": ids,
                                  "bridge": t.get("bridge", "no") not in ("no", ""), "tunnel": t.get("tunnel", "no") not in ("no", ""),
                                  "osm_id": way["id"], "maxspeed_mph": maxspeed_mph(t)})
            continue
        if hw in DRIVABLE:
            if hw == "service" and t.get("service") == "driveway":
                for run, _ in clip_runs(pts, list(range(len(pts))), inside):
                    driveways.append({"points": run, "width": cfg["driveways"]["width_m"], "source": "osm"})
                continue
            if hw == "service" and t.get("service") in cfg["excluded_service"]:
                continue
            if not any(inside(p) for p in pts):
                continue
            for run, ids in clip_runs(pts, [n for n in way["nodes"] if n in nodes], inside):
                roads.append({"name": t.get("name", ""), "class": hw, "width": road_width(t, cfg),
                              "oneway": t.get("oneway") == "yes", "points": run, "node_ids": ids,
                              "bridge": t.get("bridge", "no") not in ("no", ""), "tunnel": t.get("tunnel", "no") not in ("no", ""),
                              "osm_id": way["id"], "maxspeed_mph": maxspeed_mph(t)})
        elif "building" in t and way["nodes"][0] == way["nodes"][-1] and len(pts) >= 4:
            ring = pts[:-1]
            cx = sum(p[0] for p in ring) / len(ring)
            cz = sum(p[1] for p in ring) / len(ring)
            if not inside((cx, cz)):
                continue
            if poly_area(ring) < 0:
                ring = ring[::-1]  # counter-clockwise
            b = {"type": t["building"], "height": round(building_height(t, cfg) * vscale(cfg), 2), "footprint": ring,
                 "roof": "gable" if t["building"] in cfg["pitched_roof_pitch_deg"] else "flat",
                 "pitch_deg": cfg["pitched_roof_pitch_deg"].get(t["building"], 0.0),
                 # stable between regenerations: depends only on where the building stands (to 0.5 m), not on OSM order
                 "seed": zlib.crc32(f"{round(cx * 2)},{round(cz * 2)}".encode()) & 0x7fffffff}
            if owner_cfg and t.get("addr:housenumber") == str(owner_cfg["housenumber"]) and \
                    owner_cfg["street"].lower() in t.get("addr:street", "").lower():
                owner = {"index": len(buildings), "x": round(cx, 2), "z": round(cz, 2)}
            buildings.append(b)  # address tags deliberately dropped
        elif t.get("natural") == "water" or t.get("landuse") in ("reservoir", "basin"):
            if way["nodes"][0] == way["nodes"][-1] and any(inside(p) for p in pts):
                ring = pts[:-1]
                water.append({"name": t.get("name", ""), "outline": ring if poly_area(ring) > 0 else ring[::-1]})
        elif "waterway" in t:
            for run, _ in clip_runs(pts, list(range(len(pts))), inside):
                streams.append({"name": t.get("name", ""), "kind": t["waterway"], "points": run,
                                "tunnel": t.get("tunnel", "no") not in ("no", "")})
        elif t.get("natural") == "wood" or t.get("landuse") == "forest":
            if any(inside(p) for p in pts):
                woods.append({"outline": pts})
        elif t.get("amenity") == "parking" and t.get("parking", "surface") == "surface":
            # Round 31 (4): a surface lot (not a garage or underground), paved by the map build beside its building
            if way["nodes"][0] == way["nodes"][-1] and len(pts) >= 4 and any(inside(p) for p in pts):
                ring = pts[:-1]
                parking.append({"outline": ring if poly_area(ring) > 0 else ring[::-1]})

    for rel in (el for el in osm["elements"] if el["type"] == "relation"):
        t = rel.get("tags", {})
        if t.get("natural") == "water":
            for m in rel["members"]:
                if m["type"] == "way" and m.get("role") == "outer" and m["ref"] in ways:
                    pts = pts_of(ways[m["ref"]])
                    if len(pts) > 3 and pts[0] == pts[-1] and any(inside(p) for p in pts):
                        ring = pts[:-1]
                        water.append({"name": t.get("name", ""), "outline": ring if poly_area(ring) > 0 else ring[::-1]})

    roads = trim_service_roads(cfg, roads, buildings)
    ramps.sort(key=lambda r: (r["osm_id"], r["points"][0]))   # a stable order, whatever order OSM lists them in
    roads += ramps
    spawn = find_spawn(cfg, roads)
    for r in roads:
        r.pop("node_ids")
    out = {
        "attribution": "© OpenStreetMap contributors (ODbL); elevation: USGS 3DEP 1 m (public domain)",
        "bounds": cfg["bounds"], "utm_epsg": cfg["utm_epsg"], "vertical_scale": vscale(cfg),
        "origin_utm": [w0, s0], "size_x_m": size_x, "size_z_m": size_z,
        "roads": roads, "buildings": buildings, "water": water, "streams": streams, "woods": woods,
        "driveways": driveways, "spawn": spawn, "parking": parking,
    }
    log(f"OSM: {len(roads)} road ways, {len(buildings)} buildings, {len(water)} water areas, "
        f"{len(streams)} streams, {len(woods)} woods, {len(driveways)} OSM driveways, "
        f"{len(ramps)} motorway ramp pieces (appended), {len(parking)} surface parking lots")
    os.makedirs(LOCAL, exist_ok=True)
    marker_path = os.path.join(LOCAL, "owner_marker.json")
    if owner:
        with open(marker_path, "w", encoding="utf-8") as f:
            json.dump(owner, f)
        log(f"Owner house: found (building #{owner['index']}) -> {marker_path} (not committed)")
    elif owner_cfg:
        log("Owner house: NOT found in OSM building addresses")
    return out


def trim_service_roads(cfg, roads, buildings):
    """Cuts service roads where they run through, or within service_building_clearance_m of, a building footprint.
    OSM parking aisles and loops around commercial buildings often clip a wall or a corner; the building has a collider
    and the road does not, so the car would hit a wall on what looks like open road (Round 4 scan: OBSTRUCT / COLLIDER)."""
    clear = cfg.get("service_building_clearance_m", 0.0)
    if clear <= 0:
        return roads
    cell = 32.0
    grid = {}
    polys = [np.asarray(b["footprint"], dtype=np.float64) for b in buildings]
    for i, q in enumerate(polys):
        for gx in range(int((q[:, 0].min() - clear) // cell), int((q[:, 0].max() + clear) // cell) + 1):
            for gz in range(int((q[:, 1].min() - clear) // cell), int((q[:, 1].max() + clear) // cell) + 1):
                grid.setdefault((gx, gz), []).append(i)

    def blocked(x, z):
        for i in grid.get((int(x // cell), int(z // cell)), ()):
            q = polys[i]
            if points_in_poly(np.array([x]), np.array([z]), q.tolist())[0]:
                return True
            a, b = q, np.roll(q, -1, axis=0)
            ab = b - a
            t = np.clip(((x - a[:, 0]) * ab[:, 0] + (z - a[:, 1]) * ab[:, 1]) / np.maximum((ab ** 2).sum(1), 1e-9), 0, 1)
            if np.min(np.hypot(a[:, 0] + t * ab[:, 0] - x, a[:, 1] + t * ab[:, 1] - z)) < clear:
                return True
        return False

    out, cut, removed = [], 0, 0.0
    for r in roads:
        if r["class"] != "service" or r["bridge"]:
            out.append(r)
            continue
        pts = r["points"]
        # walk the polyline in ~1 m steps (keeping the original vertices), marking each point free or blocked
        walk = [(pts[0], True, 0)]
        for k in range(1, len(pts)):
            (x0, z0), (x1, z1) = pts[k - 1], pts[k]
            n = max(1, int(math.ceil(math.hypot(x1 - x0, z1 - z0))))
            for j in range(1, n + 1):
                walk.append(((x0 + (x1 - x0) * j / n, z0 + (z1 - z0) * j / n), j == n, k))
        bad = [blocked(p[0], p[1]) for p, _, _ in walk]
        if not any(bad):
            out.append(r)
            continue
        cut += 1
        total = sum(math.hypot(pts[k][0] - pts[k - 1][0], pts[k][1] - pts[k - 1][1]) for k in range(1, len(pts)))
        kept, run = 0.0, []
        runs = []
        for (p, _, _), b in zip(walk, bad):
            if b:
                if len(run) >= 2:
                    runs.append(run)
                run = []
            else:
                run.append((round(p[0], 2), round(p[1], 2)))
        if len(run) >= 2:
            runs.append(run)
        for run in runs:
            ln = sum(math.hypot(run[k][0] - run[k - 1][0], run[k][1] - run[k - 1][1]) for k in range(1, len(run)))
            if ln < 3.0:
                continue
            kept += ln
            piece = dict(r, points=run, node_ids=[None] * len(run))
            out.append(piece)
        removed += total - kept
    log(f"Service roads: {cut} cut where they ran through or within {clear:g} m of a building; {removed:.0f} m of service road removed")
    return out


def clip_runs(pts, ids, inside):
    """Splits a polyline into the runs that lie on the map, cut exactly at the map edge."""
    runs, cur, cur_ids = [], [], []
    for i, (p, n) in enumerate(zip(pts, ids)):
        if inside(p):
            if not cur and i > 0:
                cur.append(edge_point(pts[i - 1], p, inside)); cur_ids.append(None)
            cur.append(p); cur_ids.append(n)
        elif cur:
            cur.append(edge_point(p, pts[i - 1], inside)); cur_ids.append(None)
            runs.append((cur, cur_ids)); cur, cur_ids = [], []
    if cur:
        runs.append((cur, cur_ids))
    return [r for r in runs if len(r[0]) >= 2]


def edge_point(outside_p, inside_p, inside):
    lo, hi = 0.0, 1.0  # bisect from inside_p (t=0) towards outside_p (t=1)
    for _ in range(30):
        mid = (lo + hi) / 2
        q = (inside_p[0] + (outside_p[0] - inside_p[0]) * mid, inside_p[1] + (outside_p[1] - inside_p[1]) * mid)
        lo, hi = (mid, hi) if inside(q) else (lo, mid)
    return (round(inside_p[0] + (outside_p[0] - inside_p[0]) * lo, 2), round(inside_p[1] + (outside_p[1] - inside_p[1]) * lo, 2))


def flat_for_unity(mapdata):
    """Unity's JsonUtility can't read nested arrays, so point lists are written as [x0, z0, x1, z1, ...]."""
    def flat(pts):
        return [c for p in pts for c in p]
    out = dict(mapdata)
    out["roads"] = [dict(r, points=flat(r["points"])) for r in mapdata["roads"]]
    out["bridges"] = [dict(b, points=flat(b["points"])) for b in mapdata["bridges"]]
    out["buildings"] = [dict(b, footprint=flat(b["footprint"])) for b in mapdata["buildings"]]
    out["water"] = [dict(w, outline=flat(w["outline"])) for w in mapdata["water"]]
    out["streams"] = [dict(s, points=flat(s["points"])) for s in mapdata["streams"] if not s["tunnel"]]
    out["woods"] = [dict(w, outline=flat(w["outline"])) for w in mapdata["woods"]]
    out["driveways"] = [dict(d, points=flat(d["points"])) for d in mapdata["driveways"]]
    out["parking"] = [dict(a, outline=flat(a["outline"])) for a in mapdata.get("parking", [])]
    return out


def find_spawn(cfg, roads):
    sp = cfg["spawn"]
    street = [r for r in roads if r["name"] == sp["street"]]
    cross = {n for r in roads if r["name"].startswith(sp["from_street"]) for n in r["node_ids"] if n is not None}
    for r in street:
        ids = r["node_ids"]
        pts = r["points"]
        if ids[0] in cross or ids[-1] in cross:
            if ids[-1] in cross:
                pts = pts[::-1]
            # walk distance_m along the street from the junction
            remaining = sp["distance_m"]
            for (x1, z1), (x2, z2) in zip(pts, pts[1:]):
                seg = math.hypot(x2 - x1, z2 - z1)
                if seg >= remaining:
                    f = remaining / seg
                    x, z = x1 + (x2 - x1) * f, z1 + (z2 - z1) * f
                    heading = math.degrees(math.atan2(x2 - x1, z2 - z1))
                    return {"x": round(x, 2), "z": round(z, 2), "heading_deg": round(heading, 1),
                            "street": sp["street"]}
                remaining -= seg
    raise SystemExit(f"Spawn: no {sp['street']} way touching {sp['from_street']}")


# ---------------------------------------------------------------- bridges

def densify_line(pts, step=1.0):
    pts = np.asarray(pts, dtype=np.float64)
    seg = np.hypot(*np.diff(pts, axis=0).T)
    cum = np.concatenate([[0.0], np.cumsum(seg)])
    n = max(2, int(math.ceil(cum[-1] / step)) + 1)
    s = np.linspace(0, cum[-1], n)
    return s, np.interp(s, cum, pts[:, 0]), np.interp(s, cum, pts[:, 1]), cum


def subline(pts, cum, a, b):
    """Part of a polyline between arc lengths a and b, cut exactly at both ends."""
    pts = np.asarray(pts, dtype=np.float64)
    inner = [(float(pts[i, 0]), float(pts[i, 1])) for i in range(len(pts)) if a + 1e-6 < cum[i] < b - 1e-6]
    return [(float(np.interp(a, cum, pts[:, 0])), float(np.interp(a, cum, pts[:, 1])))] + inner + \
           [(float(np.interp(b, cum, pts[:, 0])), float(np.interp(b, cum, pts[:, 1])))]


def points_in_poly(x, z, poly):
    inside = np.zeros(len(x), bool)
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, zi = poly[i]
        xj, zj = poly[j]
        if zi != zj:
            hit = ((zi > z) != (zj > z)) & (x < (xj - xi) * (z - zi) / (zj - zi) + xi)
            inside ^= hit
        j = i
    return inside


def road_stream_crossings(x, z, s, line):
    """Arc lengths along the densified road where it crosses a polyline (stream)."""
    out = []
    lx0, lx1 = min(p[0] for p in line), max(p[0] for p in line)
    lz0, lz1 = min(p[1] for p in line), max(p[1] for p in line)
    if x.max() < lx0 - 1 or x.min() > lx1 + 1 or z.max() < lz0 - 1 or z.min() > lz1 + 1:
        return out
    ax, az, bx, bz = x[:-1], z[:-1], x[1:], z[1:]
    rx, rz = bx - ax, bz - az
    for (cx, cz), (dx, dz) in zip(line, line[1:]):
        qx, qz = dx - cx, dz - cz
        den = rx * qz - rz * qx
        with np.errstate(divide="ignore", invalid="ignore"):
            u = ((cx - ax) * qz - (cz - az) * qx) / den
            v = ((cx - ax) * rz - (cz - az) * rx) / den
        ok = (np.abs(den) > 1e-9) & (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
        for k in np.nonzero(ok)[0]:
            out.append(float(s[k] + u[k] * (s[k + 1] - s[k])))
    return out


def plan_bridges(cfg, mapdata, sample):
    """Holds roads level where they cross water or carry an OSM bridge tag.

    Each road way is cut into ordinary pieces (kept in mapdata["roads"], draped on the terrain) and bridge pieces
    (mapdata["bridges"], with a deck height every ~2 m). A bridge span is the shortest stretch around the water whose
    straight-line deck between its two ends clears the bare-earth ground; if none is found within max_half_span_m the
    deck is lifted to clear it instead. A water crossing whose dip is under min_depth_m is left on the ground
    (culvert / ford)."""
    bc = cfg["bridges"]
    step = bc["search_step_m"]
    lines = [(st["name"] or st["kind"], st["points"]) for st in mapdata["streams"] if not st["tunnel"]]
    culverts = [(st["name"] or st["kind"], st["points"]) for st in mapdata["streams"] if st["tunnel"]]
    ponds = [(w["name"] or "unnamed pond", [tuple(p) for p in w["outline"]]) for w in mapdata["water"]]
    new_roads, bridges, at_grade, culvert_hits = [], [], [], []
    for r in mapdata["roads"]:
        if r["tunnel"]:
            new_roads.append(r)
            continue
        s, x, z, cum = densify_line(r["points"])
        L = float(s[-1])
        g = sample(x, z)
        tx, tz = np.gradient(x), np.gradient(z)
        nrm = np.maximum(np.hypot(tx, tz), 1e-9)
        ox, oz = -tz / nrm * r["width"] / 2, tx / nrm * r["width"] / 2
        gmax = np.maximum(g, np.maximum(sample(x + ox, z + oz), sample(x - ox, z - oz)))
        water = []  # [start_s, end_s, label]
        for nm, line in lines:
            for c in road_stream_crossings(x, z, s, line):
                water.append([c - bc["stream_halfwidth_m"], c + bc["stream_halfwidth_m"], "stream " + nm])
        for nm, poly in ponds:
            inside = points_in_poly(x, z, poly)
            if inside.any():
                idx = np.nonzero(inside)[0]
                for run in np.split(idx, np.nonzero(np.diff(idx) > 1)[0] + 1):
                    water.append([s[run[0]] - 1, s[run[-1]] + 1, "pond " + nm])
        for nm, line in culverts:
            for c in road_stream_crossings(x, z, s, line):
                ia, ib = int(max(0, round(c - 12))), int(min(len(s) - 1, round(c + 12)))
                dip = float((g[ia] + (g[ib] - g[ia]) * (np.arange(ia, ib + 1) - ia) / max(ib - ia, 1) - g[ia:ib + 1]).max())
                culvert_hits.append((r["name"] or r["class"], nm, round(dip, 2), round(float(x[int(round(c))]), 0), round(float(z[int(round(c))]), 0)))
        water.sort()
        merged = []
        for w in water:
            if merged and w[0] <= merged[-1][1] + 6:
                merged[-1][1] = max(merged[-1][1], w[1])
                merged[-1][2] += "; " + w[2]
            else:
                merged.append(list(w))
        spans = []  # [a, b, labels, tagged]
        if r["bridge"]:
            spans.append([0.0, L, [m[2] for m in merged], True])
        else:
            for wa, wb, lab in merged:
                wa, wb = max(0.0, wa), min(L, wb)
                chosen = None
                w_ext = 0.0
                while w_ext <= bc["max_half_span_m"] + 1e-9:
                    a, b = max(0.0, wa - w_ext), min(L, wb + w_ext)
                    ia, ib = int(round(a)), int(round(b))
                    if ib - ia >= 2:
                        deck = g[ia] + (g[ib] - g[ia]) * (np.arange(ia, ib + 1) - ia) / (ib - ia)
                        if (gmax[ia:ib + 1] - deck).max() <= bc["clear_m"]:
                            chosen = [a, b]
                            break
                    w_ext += step
                if chosen is None:
                    chosen = [max(0.0, wa - bc["max_half_span_m"]), min(L, wb + bc["max_half_span_m"])]
                ia, ib = int(round(chosen[0])), int(round(chosen[1]))
                if ib - ia < 2:
                    continue
                deck = g[ia] + (g[ib] - g[ia]) * (np.arange(ia, ib + 1) - ia) / (ib - ia)
                depth = float((deck - g[ia:ib + 1]).max())
                if depth < bc["min_depth_m"]:
                    mid = int(round((wa + wb) / 2))
                    at_grade.append({"road": r["name"] or r["class"], "over": lab, "depth_m": round(depth, 2),
                                     "x": round(float(x[mid]), 1), "z": round(float(z[mid]), 1)})
                    continue
                if spans and chosen[0] <= spans[-1][1]:
                    spans[-1][1] = max(spans[-1][1], chosen[1])
                    spans[-1][2].append(lab)
                else:
                    spans.append([chosen[0], chosen[1], [lab], False])
        if not spans:
            new_roads.append(r)
            continue
        cursor = 0.0
        for a, b, lab, tagged in spans:
            if a - cursor > 0.5:
                piece = dict(r, points=subline(r["points"], cum, cursor, a))
                piece["taper_start"], piece["taper_end"] = cursor > 0, True
                new_roads.append(piece)
            ia, ib = int(round(a)), int(round(b))
            bs = np.unique(np.concatenate([np.arange(a, b, 2.0), [b]]))
            bx, bz = np.interp(bs, s, x), np.interp(bs, s, z)
            chord = g[ia] + (g[ib] - g[ia]) * np.clip((bs - ia) / max(ib - ia, 1), 0, 1)
            deck = np.maximum(chord, np.interp(bs, s, gmax) - bc["clear_m"])  # lift only where the banks would poke through
            clamped = bool((deck > chord + 0.25).any())
            bridges.append({"name": r["name"], "class": r["class"], "width": r["width"], "oneway": r["oneway"],
                            "osm_id": r["osm_id"], "tagged": tagged,
                            "over": "; ".join(lab) if lab else "(no mapped water)",
                            "length_m": round(float(b - a), 1),
                            "depth_m": round(float((deck - np.interp(bs, s, g)).max()), 2), "deck_lifted": clamped,
                            "points": [(round(float(px), 2), round(float(pz), 2)) for px, pz in zip(bx, bz)],
                            "deck_y": [round(float(d), 3) for d in deck]})
            cursor = b
        if L - cursor > 0.5:
            piece = dict(r, points=subline(r["points"], cum, cursor, L))
            piece["taper_start"], piece["taper_end"] = True, False
            new_roads.append(piece)
    mapdata["roads"] = new_roads
    mapdata["bridges"] = bridges
    return at_grade, culvert_hits


def write_bridge_report(bridges, at_grade, culvert_hits):
    lines = ["Bridges built by Tools/mapgen/mapgen.py (regenerated every run)", ""]
    lines.append(f"{len(bridges)} bridge pieces: {sum(b['tagged'] for b in bridges)} from an OSM bridge tag, "
                 f"{sum(not b['tagged'] for b in bridges)} from a road crossing mapped water with no bridge tag.")
    lines.append("")
    for i, b in enumerate(sorted(bridges, key=lambda b: (b["name"], b["points"][0])), 1):
        lines.append(f"{i:3d}. {b['name'] or '(unnamed ' + b['class'] + ')'} [{b['class']}] at x={b['points'][0][0]:.0f} "
                     f"z={b['points'][0][1]:.0f}, {b['length_m']:.0f} m long, {b['width']:.1f} m wide, deck up to "
                     f"{b['depth_m']:.1f} m above the bare earth; {'OSM bridge tag' if b['tagged'] else 'NO bridge tag'}; "
                     f"over {b['over']}{'; deck lifted to clear banks' if b['deck_lifted'] else ''}")
    lines += ["", f"{len(at_grade)} water crossings left on the ground (dip under the minimum: culvert or ford):"]
    for c in at_grade:
        lines.append(f"  {c['road']} over {'; '.join(c['over']) if isinstance(c['over'], list) else c['over']} "
                     f"at x={c['x']:.0f} z={c['z']:.0f} (dip {c['depth_m']:.2f} m)")
    lines += ["", f"{len(culvert_hits)} road crossings of streams that OSM maps as a culvert (left on the ground; dip = how far the "
              "bare earth sits below a straight line 12 m either side):"]
    for nm, st, dip, cx, cz in sorted(culvert_hits):
        lines.append(f"  {nm} over {st} at x={cx:.0f} z={cz:.0f} (dip {dip:.2f} m)")
    path = os.path.join(DATA, "bridges.txt")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    return path


# ---------------------------------------------------------------- terrain

def limit_curvature(prof, half, sag_max, fixed, iters=800):
    """Relaxes a road profile until no crest or dip is sharper than sag_max: at every station the height may not differ
    from the chord between the stations 'half' before and after by more than sag_max. Straight grades have no sag, so
    they are untouched (unlike a moving average, which also shaves the tops off hills); only humps, dips and kinks shorter
    than a car are rounded off. Each step moves a too-sharp station toward its chord and the chord ends half as far the
    other way. 'fixed' marks stations that must keep their height. Returns (profile, iterations used)."""
    prof = prof.copy()
    n = len(prof)
    if n < 2 * half + 1:
        return prof, 0
    for it in range(iters):
        sag = prof[half:n - half] - 0.5 * (prof[:n - 2 * half] + prof[2 * half:])
        excess = np.sign(sag) * np.maximum(np.abs(sag) - sag_max, 0.0)
        if float(np.abs(excess).max()) < 0.004:
            return prof, it
        a = excess / 3.0
        d = np.zeros(n)
        d[half:n - half] -= a
        d[:n - 2 * half] += a / 2
        d[2 * half:] += a / 2
        d[fixed] = 0.0
        prof += d
    return prof, iters


def shape_road_profiles(fl, plans):
    """Gives every road a drivable vertical profile (see CLAUDE.md, Round 4):
      1. curvature limit - removes the lidar's culvert humps, dips and kinks (the car launches off them);
      2. junctions - a road that meets a wider one takes the wider road's height at the junction, fading back to its own
         profile over junction_blend_m, so the two surfaces do not step across the through road's pavement;
      3. curvature limit again, with the junction heights held."""
    half = int(round(fl.get("sag_half_chord_m", 3.0)))
    sag_max = float(fl.get("sag_max_m", 0.0))
    blend = float(fl.get("junction_blend_m", 12.0))
    if sag_max <= 0:
        return
    before = [pl["prof"].copy() for pl in plans]
    # stations that must not move: the ends of a road piece that meets a bridge deck
    for pl in plans:
        fixed = np.zeros(len(pl["prof"]), bool)
        if pl["road"].get("taper_start"):
            fixed[:2] = True
        if pl["road"].get("taper_end"):
            fixed[-2:] = True
        pl["fixed"] = fixed
        pl["prof"], _ = limit_curvature(pl["prof"], half, sag_max, fixed)
    # junction nodes: a vertex that two or more road pieces share
    nodes = {}
    for i, pl in enumerate(plans):
        for (px, pz), c in zip(pl["pts"], pl["cum"]):
            nodes.setdefault((round(px * 20), round(pz * 20)), []).append((i, float(c)))
    shared = [v for v in nodes.values() if len({i for i, _ in v}) > 1]
    shared.sort(key=lambda v: -max(plans[i]["road"]["width"] for i, _ in v))
    adjusted = 0
    for v in shared:
        major = max(v, key=lambda t: plans[t[0]]["road"]["width"])
        pm = plans[major[0]]
        hj = float(np.interp(major[1], pm["s"], pm["prof"]))
        for i, c in v:
            if i == major[0]:
                continue
            pl = plans[i]
            delta = hj - float(np.interp(c, pl["s"], pl["prof"]))
            w = np.clip(1.0 - np.abs(pl["s"] - c) / blend, 0.0, 1.0)
            pl["prof"] = pl["prof"] + delta * (w * w * (3 - 2 * w))
            adjusted += 1
    # Round 31 (BUG-004): a motorway ramp that runs alongside the road it leaves or joins overlaps that road's pavement for
    # tens of metres before their shared node; there its own profile could sit half a metre off the road's, a step across the
    # pavement. Where a ramp station overlaps another (non-ramp) road it takes that road's height: fully where they overlap by
    # 2 m or more, fading back to its own profile as they separate, and only where the two run within about 25 degrees of each
    # other (a ramp that crosses a road is left to the junction rule). Only ramps move. (A ramp that converges on a road at a
    # wider angle is smoothed in the map build: MapBuilder.Round31.cs, RampSeams.)
    others = [(i, pl) for i, pl in enumerate(plans) if pl["road"]["class"] != "motorway_link"]
    if any(pl["road"]["class"] == "motorway_link" for pl in plans) and others:
        pts_o = np.concatenate([np.column_stack([pl["dx"], pl["dz"]]) for _, pl in others])
        h_o = np.concatenate([pl["prof"] for _, pl in others])
        w_o = np.concatenate([np.full(len(pl["dx"]), pl["road"]["width"] / 2) for _, pl in others])
        def dirs(pl):
            tx, tz = np.gradient(pl["dx"]), np.gradient(pl["dz"]); n = np.maximum(np.hypot(tx, tz), 1e-9)
            return np.column_stack([tx / n, tz / n])
        d_o = np.concatenate([dirs(pl) for _, pl in others])
        tree = cKDTree(pts_o)
        ramp_moved = 0.0
        for pl in plans:
            if pl["road"]["class"] != "motorway_link":
                continue
            half_r = pl["road"]["width"] / 2
            d_r = dirs(pl)
            for k in range(len(pl["prof"])):
                idx = tree.query_ball_point((pl["dx"][k], pl["dz"][k]), half_r + 12.0)
                best = 0.0; hb = None   # best: the weight toward the other road
                for j in idx:
                    par = float(smoothstep((abs(d_o[j] @ d_r[k]) - 0.85) / 0.06))
                    ov = half_r + w_o[j] - math.hypot(pts_o[j][0] - pl["dx"][k], pts_o[j][1] - pl["dz"][k])
                    wt = float(smoothstep(ov / 2.0)) * par
                    if wt > best: best, hb = wt, h_o[j]
                if hb is not None:
                    w = best
                    new = pl["prof"][k] * (1 - w) + hb * w
                    ramp_moved = max(ramp_moved, abs(new - pl["prof"][k]))
                    pl["prof"][k] = new
        log(f"Ramps: where a motorway ramp overlaps another road it takes that road's height (largest change {ramp_moved:.2f} m)")
    held = 0
    for v in shared:
        for i, c in v:
            pl = plans[i]
            k = int(np.argmin(np.abs(pl["s"] - c)))
            pl["fixed"][k] = True
            held += 1
    for pl in plans:
        pl["prof"], _ = limit_curvature(pl["prof"], half, sag_max, pl["fixed"])
    moved = np.concatenate([np.abs(pl["prof"] - b) for pl, b in zip(plans, before)])
    log(f"Road profiles: curvature limit {sag_max:g} m over +-{half} m (crest radius {half * half / (2 * sag_max):.0f} m), "
        f"{len(shared)} junction nodes ({adjusted} road ends levelled to the wider road over {blend:g} m; "
        f"{(moved > 0.02).mean() * 100:.1f}% of {len(moved)} stations moved more than 2 cm, "
        f"{(moved > 0.10).mean() * 100:.2f}% more than 10 cm, max {moved.max():.2f} m, mean of those moved {moved[moved > 0.02].mean():.2f} m")


def junction_stems(fl, plans):
    """Round 39 (A1 BUG-001, A3 BUG-005): a road that ENDS on another road that runs on through the junction (a T, or a
    service road ending at a street's centerline) is that junction's stem. Its cross-section is level, so on a sloping through
    road its flat end won the cells of the through road's pavement nearest its own centerline: a level wedge into the lane,
    0.3-0.5 m out of the through road's grade (Chasefield Way on Chastain Glen Lane), or a strip standing over the road mesh
    (grass on Chastain Corner). Round 34's wider_wins_rects fixed two such places by hand. Here, for every stem end: the
    through road's centerline near the junction (its own profile) as short segments, which build_terrain uses to warp the
    stem's surface onto the through road's (see there). Returns {plan index: [(end_station, through_segments, core_t)]}."""
    tol = 1.5
    pts = [np.column_stack([pl["dx"], pl["dz"]]) for pl in plans]
    lo = np.array([p.min(axis=0) for p in pts]) - tol; hi = np.array([p.max(axis=0) for p in pts]) + tol
    def tangent(pl, end):
        n = len(pl["dx"]); k = min(4, n - 1)
        if end == 0: v = np.array([pl["dx"][k] - pl["dx"][0], pl["dz"][k] - pl["dz"][0]])
        else: v = np.array([pl["dx"][n - 1 - k] - pl["dx"][-1], pl["dz"][n - 1 - k] - pl["dz"][-1]])
        return v / max(np.hypot(*v), 1e-9)
    stems = {}
    for i, pl in enumerate(plans):
        if pl["road"]["class"] == "motorway_link":
            continue
        for end in (0, -1):
            if (end == 0 and pl["road"].get("taper_start")) or (end == -1 and pl["road"].get("taper_end")):
                continue
            E = pts[i][end]
            near = np.where((lo[:, 0] <= E[0]) & (hi[:, 0] >= E[0]) & (lo[:, 1] <= E[1]) & (hi[:, 1] >= E[1]))[0]
            interior, ends = [], []
            for j in near:
                if j == i:
                    continue
                d = np.hypot(pts[j][:, 0] - E[0], pts[j][:, 1] - E[1]); k = int(np.argmin(d))
                if d[k] > tol:
                    continue
                pj = plans[j]; core_j = pj["road"]["width"] / 2 + fl["shoulder_m"]; L = pj["s"][-1]
                if pj["s"][k] > core_j and L - pj["s"][k] > core_j:
                    interior.append((j, k))
                elif pj["s"][k] <= 0.5 or L - pj["s"][k] <= 0.5:
                    ends.append((j, 0 if pj["s"][k] <= 0.5 else -1))
            through = []
            if interior:
                j, k = max(interior, key=lambda t: plans[t[0]]["road"]["width"])
                through = [(j, k)]
            else:
                ti = tangent(pl, end)
                for a in range(len(ends)):
                    for b in range(a + 1, len(ends)):
                        (ja, ea), (jb, eb) = ends[a], ends[b]
                        ta, tb = tangent(plans[ja], ea), tangent(plans[jb], eb)
                        if ta @ tb < -0.85 and ti @ ta > -0.85 and ti @ tb > -0.85:
                            through = [(ja, 0 if ea == 0 else len(plans[ja]["s"]) - 1), (jb, 0 if eb == 0 else len(plans[jb]["s"]) - 1)]
                            break
                    if through:
                        break
            if not through:
                continue
            # a T, not a Y: the stem meets the through road at 40 degrees or more (two roads that leave a node side by side
            # share their pavement for metres; there each keeps its own surface, as before)
            ti = tangent(pl, end); jt, kt = through[0]; pt = plans[jt]
            k0, k1 = max(0, kt - 3), min(len(pt["dx"]) - 1, kt + 3)
            tt = np.array([pt["dx"][k1] - pt["dx"][k0], pt["dz"][k1] - pt["dz"][k0]]); tt = tt / max(np.hypot(*tt), 1e-9)
            if abs(ti @ tt) > 0.766:
                continue
            segs = []
            for j, k in through:
                pj = plans[j]; sel = np.where(np.abs(pj["s"] - pj["s"][k]) <= 40.0)[0]
                if len(sel) < 2:
                    continue
                a0, a1 = sel[0], sel[-1]
                segs.append(np.column_stack([pj["dx"][a0:a1], pj["dz"][a0:a1], pj["dx"][a0 + 1:a1 + 1], pj["dz"][a0 + 1:a1 + 1],
                                             pj["prof"][a0:a1], pj["prof"][a0 + 1:a1 + 1]]))
            if not segs:
                continue
            core_t = max(plans[j]["road"]["width"] for j, _ in through) / 2 + fl["shoulder_m"]
            stems.setdefault(i, []).append((0.0 if end == 0 else float(pl["s"][-1]), np.concatenate(segs), core_t))
    return stems


def through_surface(segs, X, Z):
    """distance from each cell to the through road's centerline and the through road's height at the nearest point"""
    ax, az, bx, bz, ha, hb = (segs[:, c][None, :] for c in range(6))
    vx, vz = bx - ax, bz - az; L2 = np.maximum(vx * vx + vz * vz, 1e-9)
    x = X.reshape(-1, 1); z = Z.reshape(-1, 1)
    t = np.clip(((x - ax) * vx + (z - az) * vz) / L2, 0, 1)
    d = np.hypot(x - (ax + t * vx), z - (az + t * vz))
    k = np.argmin(d, axis=1); r = np.arange(len(k))
    return d[r, k].reshape(X.shape), (ha + t * (hb - ha))[r, k].reshape(X.shape)


def maxspeed_mph(tags):
    """Round 39 (D2): OSM's posted speed limit in mph ("45 mph", or km/h as a bare number), 0 when the way has none"""
    v = (tags.get("maxspeed") or "").strip().lower()
    try:
        if v.endswith("mph"): return int(round(float(v[:-3])))
        if v and v.replace(".", "", 1).isdigit(): return int(round(float(v) * 0.621371))
    except ValueError:
        pass
    return 0


def smoothstep(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def build_terrain(cfg, dem_path, mapdata):
    tc = cfg["terrain"]
    cells = tc["tile_resolution"] - 1
    W, H = mapdata["size_x_m"], mapdata["size_z_m"]
    nx, nz = math.ceil(W / tc["max_tile_m"]), math.ceil(H / tc["max_tile_m"])
    gx_n, gz_n = nx * cells + 1, nz * cells + 1
    sx, sz = W / (nx * cells), H / (nz * cells)  # metres per sample
    with rasterio.open(dem_path) as src:
        dem = src.read(1)[::-1].astype(np.float32)  # row 0 = south (Unity convention); 1 m cells, value at the cell centre
    src_min, src_max = float(dem.min()), float(dem.max())

    def axis(n, step, size):
        p = np.clip(np.arange(n) * step - 0.5, 0, size - 1.001)
        i0 = p.astype(int)
        return i0, (p - i0).astype(np.float32)

    # bilinear onto the global grid; sample i sits at i * step metres from the crop's south-west corner
    j0, fx = axis(gx_n, sx, dem.shape[1])
    i0, fz = axis(gz_n, sz, dem.shape[0])
    rows = dem[:, j0] * (1 - fx) + dem[:, j0 + 1] * fx
    h = rows[i0, :] * (1 - fz)[:, None] + rows[i0 + 1, :] * fz[:, None]
    del rows
    # Vertical scale: stretch heights about the lowest point of the crop, so the map keeps its elevations near
    # their real values and only the relief grows. Roads are flattened from the scaled surface, so their grades scale too.
    k = vscale(cfg)
    datum = src_min
    h = (datum + (h - datum) * k).astype(np.float32)
    raw = h.copy()

    def sample(x, z):
        gx = np.clip(np.asarray(x) / sx, 0, gx_n - 1.001)
        gz = np.clip(np.asarray(z) / sz, 0, gz_n - 1.001)
        i, j = gz.astype(int), gx.astype(int)
        fz_, fx_ = gz - i, gx - j
        return (raw[i, j] * (1 - fx_) * (1 - fz_) + raw[i, j + 1] * fx_ * (1 - fz_) +
                raw[i + 1, j] * (1 - fx_) * fz_ + raw[i + 1, j + 1] * fx_ * fz_).astype(np.float64)

    at_grade, culvert_hits = plan_bridges(cfg, mapdata, sample)
    rpt = write_bridge_report(mapdata["bridges"], at_grade, culvert_hits)
    log(f"Bridges: {len(mapdata['bridges'])} built, {len(at_grade)} crossings left at grade -> {rpt}")
    h = flatten_roads(cfg, h, sample, mapdata["roads"], sx, sz, gx_n, gz_n)
    return finish_terrain(cfg, h, raw, gx_n, gz_n, nx, nz, W, H, sx, sz, k, datum, src_min, src_max)


def flatten_roads(cfg, h, sample, roads, sx, sz, gx_n, gz_n):
    """The roads levelled across their width into the terrain samples h (Round 59: its own function, so the map extension
    levels its roads the same way)."""
    fl = cfg["road_flatten"]
    taper = cfg["bridges"]["end_taper_m"]
    best_w = np.zeros_like(h)
    best_d = np.full_like(h, np.inf)
    target = np.zeros_like(h)
    # Round 34 (A1, BUG-001 to 003): inside these rectangles a road's level core keeps its own height where a narrower road
    # ends on it (a service road that meets Canton Road at its centerline levelled a flat strip across the lane, steps of
    # 0.3-0.4 m). There a cell in a road's core (weight 1) goes to the widest road whose core it is in; elsewhere unchanged.
    wide_rects = fl.get("wider_wins_rects", [])
    best_k = np.zeros_like(h)
    plans = []
    for road in roads:
        if road["tunnel"]:
            continue
        pts = np.array(road["points"], dtype=np.float64)
        seglen = np.hypot(*np.diff(pts, axis=0).T)
        if seglen.sum() < 0.5:
            continue
        # densify to ~1 m and sample the bare-earth profile along the road centreline
        cum = np.concatenate([[0], np.cumsum(seglen)])
        s = np.linspace(0, cum[-1], max(2, int(cum[-1]) + 1))
        dx = np.interp(s, cum, pts[:, 0])
        dz = np.interp(s, cum, pts[:, 1])
        prof = sample(dx, dz)
        # Optional smoothing ALONG the road. It shaves crests and fills dips, so it reduces grades; 0 (the default)
        # keeps the centreline profile exactly as sampled and only levels the surface across the road's width.
        win = int(fl.get("profile_smoothing_m", 0) / 2)
        if win > 0:
            padded = np.pad(prof, win, mode="edge")
            prof = np.convolve(padded, np.ones(2 * win + 1) / (2 * win + 1), mode="valid")
        plans.append({"road": road, "s": s, "dx": dx, "dz": dz, "prof": prof, "pts": pts, "cum": cum})
    shape_road_profiles(fl, plans)
    stems = junction_stems(fl, plans) if fl.get("junction_warp_m", 0) > 0 else {}
    warp = float(fl.get("junction_warp_m", 0)); warped = 0
    log(f"Junctions: {sum(len(v) for v in stems.values())} road ends meet a road that runs on through (warped onto it over {warp:g} m)")
    for pi, plan in enumerate(plans):
        road, s, dx, dz, prof = plan["road"], plan["s"], plan["dx"], plan["dz"], plan["prof"]
        core = road["width"] / 2 + fl["shoulder_m"]
        reach = core + fl["blend_m"]
        total = s[-1]
        t_start, t_end = road.get("taper_start", False), road.get("taper_end", False)
        for a in range(len(dx) - 1):
            ax, az, bx, bz = dx[a], dz[a], dx[a + 1], dz[a + 1]
            ha, hb = prof[a], prof[a + 1]
            j0_ = max(0, int((min(ax, bx) - reach) / sx))
            j1_ = min(gx_n - 1, int((max(ax, bx) + reach) / sx) + 1)
            i0_ = max(0, int((min(az, bz) - reach) / sz))
            i1_ = min(gz_n - 1, int((max(az, bz) + reach) / sz) + 1)
            if j0_ > j1_ or i0_ > i1_:
                continue
            X, Z = np.meshgrid(np.arange(j0_, j1_ + 1) * sx, np.arange(i0_, i1_ + 1) * sz)
            vx, vz = bx - ax, bz - az
            L2 = vx * vx + vz * vz or 1e-9
            t_raw = ((X - ax) * vx + (Z - az) * vz) / L2
            t = np.clip(t_raw, 0, 1)
            d = np.hypot(X - (ax + t * vx), Z - (az + t * vz))
            tgt = ha + t * (hb - ha)
            # Round 39 (A1, A3): a junction stem's surface near the through road is the through road's own surface (its height at
            # the nearest point of its centerline), fading back to the stem's level cross-section from the through road's level
            # core out over junction_warp_m, and along the stem over its last core + warp + 4 m. Inside the through road's core
            # the two roads now ask the same height, so neither can level a wedge across the other.
            for s_end, segs, core_t in stems.get(pi, ()):
                if min(abs(s[a] - s_end), abs(s[a + 1] - s_end)) > core_t + warp + 1.0:
                    continue
                sp = s[a] + t * (s[a + 1] - s[a])
                along = np.abs(sp - s_end)
                f = smoothstep((along - core_t) / warp)
                if (f >= 1).all():
                    continue
                # the stem's level cross-section tilted to the through road's surface: the through road's height here less
                # its height under the stem's centerline at the same station (the profile already holds that), fading out
                _, ht = through_surface(segs, X, Z)
                _, hc = through_surface(segs, ax + t * vx, az + t * vz)
                tgt = tgt + (ht - hc) * (1 - f)
                warped += 1
            wgt = np.clip((reach - d) / fl["blend_m"], 0, 1)
            wgt = wgt * wgt * (3 - 2 * wgt)  # smoothstep
            # next to a bridge the levelling fades out over taper_m, so the terrain meets the deck end untouched
            if (t_start and s[a] < taper + 2) or (t_end and total - s[a] < taper + 2):
                sp = s[a] + t_raw * (s[a + 1] - s[a])
                if t_start:
                    wgt = wgt * smoothstep(sp / taper)
                if t_end:
                    wgt = wgt * smoothstep((total - sp) / taper)
            sl = (slice(i0_, i1_ + 1), slice(j0_, j1_ + 1))
            # A cell takes the target of the road segment with the strongest pull; weights within TIE of each other
            # count as equal and the nearer segment wins. (With a tolerance of 1e-6 a cell at the edge of the road's
            # level core, weight 0.999999, kept the height of a segment 5 m away and left single-sample spikes up to
            # a metre high in the road surface: the Round 4 scan's STEP events on ordinary streets.)
            TIE = 1e-3
            better = (wgt > best_w[sl] + TIE) | ((np.abs(wgt - best_w[sl]) <= TIE) & (d < best_d[sl]))
            better &= wgt > 0
            if wide_rects:
                inr = np.zeros(X.shape, bool)
                for x0, z0, x1, z1 in wide_rects:
                    inr |= (X >= x0) & (X <= x1) & (Z >= z0) & (Z <= z1)
                if inr.any():
                    k_new = np.where(wgt >= 0.999, road["width"], 0.0)
                    k_old = best_k[sl]
                    better = np.where(inr, (k_new > k_old) | ((k_new == k_old) & better), better)
                    best_k[sl] = np.where(better, k_new, k_old)
            best_w[sl] = np.where(better, wgt, best_w[sl])
            best_d[sl] = np.where(better, d, best_d[sl])
            target[sl] = np.where(better, tgt, target[sl])
    h = (h * (1 - best_w) + target * best_w).astype(np.float32)
    if stems: log(f"Junctions: {warped} stem segments warped onto their through road")
    return h


def finish_terrain(cfg, h, raw, gx_n, gz_n, nx, nz, W, H, sx, sz, k, datum, src_min, src_max):
    tc = cfg["terrain"]
    lo, hi = float(h.min()) - 1.0, float(h.max()) + 1.0
    norm = np.round((h - lo) / (hi - lo) * 65535).astype("<u2")
    os.makedirs(GENERATED, exist_ok=True)
    out = os.path.join(GENERATED, "terrain.r16")
    norm.tofile(out)
    meta = {"grid_x": gx_n, "grid_z": gz_n, "tiles_x": nx, "tiles_z": nz, "tile_resolution": tc["tile_resolution"],
            "size_x_m": W, "size_z_m": H, "tile_w_m": W / nx, "tile_d_m": H / nz,
            "step_x_m": sx, "step_z_m": sz, "min_height": lo, "max_height": hi,
            "vertical_scale": k, "vertical_datum_m": datum}
    with open(os.path.join(GENERATED, "terrain.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=1)
    moved = np.abs(h - raw)
    log(f"Terrain: {gx_n} x {gz_n} samples = {nx} x {nz} tiles of {tc['tile_resolution']}^2 samples "
        f"({W / nx:.1f} x {H / nz:.1f} m each), {sx:.3f} x {sz:.3f} m/sample, vertical scale {k:g} about {datum:.2f} m; "
        f"road flattening moved {(moved > 0.05).mean() * 100:.1f}% of samples, max {moved.max():.2f} m -> {out}")
    log(f"Elevation check: USGS crop {src_min:.3f}-{src_max:.3f} m (x{k:g} about {datum:.3f} -> "
        f"{src_min:.3f}-{datum + (src_max - datum) * k:.3f} m expected); terrain {h.min():.3f}-{h.max():.3f} m")
    return h, meta


# ---------------------------------------------------------------- shared raster of what trees must keep off

class Mask:
    """A 1 m raster of the crop (row 0 = north, like NAIP), drawn from map data in world metres (x east, z north)."""

    def __init__(self, w, h):
        self.w, self.h = int(w), int(h)
        self.img = Image.new("L", (self.w, self.h), 0)
        self.d = ImageDraw.Draw(self.img)

    def poly(self, pts, margin=0.0):
        xy = [(x, self.h - z) for x, z in pts]
        self.d.polygon(xy, fill=255)
        if margin > 0:
            self.d.line(xy + [xy[0]], fill=255, width=max(1, int(round(margin * 2))), joint="curve")

    def line(self, pts, width):
        xy = [(x, self.h - z) for x, z in pts]
        self.d.line(xy, fill=255, width=max(1, int(round(width))), joint="curve")
        r = max(0.5, width / 2)
        for x, y in (xy[0], xy[-1]):
            self.d.ellipse([x - r, y - r, x + r, y + r], fill=255)

    def array(self):
        return np.asarray(self.img) > 0


def keep_off_masks(cfg, mapdata):
    tc = cfg["trees"]
    W, H = mapdata["size_x_m"], mapdata["size_z_m"]
    roads, builds, water, drives = Mask(W, H), Mask(W, H), Mask(W, H), Mask(W, H)
    for r in mapdata["roads"]:
        roads.line(r["points"], r["width"] + 2 * tc["road_margin_m"])
    for b in mapdata["bridges"]:
        roads.line(b["points"], b["width"] + 2 * tc["road_margin_m"] + 1.4)  # parapets
    for b in mapdata["buildings"]:
        builds.poly(b["footprint"], tc["building_margin_m"])
    for w in mapdata["water"]:
        water.poly(w["outline"], tc["water_margin_m"])
    for st in mapdata["streams"]:
        if not st["tunnel"]:
            water.line(st["points"], 2.0 + 2 * tc["water_margin_m"])
    for d in mapdata["driveways"]:
        drives.line(d["points"], d["width"] + 2 * tc["driveway_margin_m"])
    return {"roads": roads.array(), "buildings": builds.array(), "water": water.array(), "driveways": drives.array()}


# ---------------------------------------------------------------- driveways

def dens(pts, step=1.0):
    pts = np.asarray(pts, dtype=np.float64)
    if len(pts) < 2:
        return pts
    s, x, z, _ = densify_line(pts, step)
    return np.stack([x, z], 1)


def _ccw(a, b, c):
    return (c[1] - a[1]) * (b[0] - a[0]) > (b[1] - a[1]) * (c[0] - a[0])


def seg_cross(a, b, c, d):
    return _ccw(a, c, d) != _ccw(b, c, d) and _ccw(a, b, c) != _ccw(a, b, d)


def point_in_ring(p, ring):
    x, z = p
    inside = False
    j = len(ring) - 1
    for i in range(len(ring)):
        xi, zi = ring[i]
        xj, zj = ring[j]
        if (zi > z) != (zj > z) and x < (xj - xi) * (z - zi) / (zj - zi) + xi:
            inside = not inside
        j = i
    return inside


def segment_hits_ring(a, b, ring, margin=0.0):
    """True if the segment a-b touches the polygon ring (crosses an edge, lies inside, or comes within margin of it)."""
    if point_in_ring(a, ring) or point_in_ring(b, ring):
        return True
    n = len(ring)
    for i in range(n):
        c, d = ring[i], ring[(i + 1) % n]
        if seg_cross(a, b, c, d):
            return True
    if margin > 0:
        rd = dens(ring + [ring[0]], 1.0)
        ab = dens([a, b], 0.5)
        if cKDTree(rd).query(ab)[0].min() < margin:
            return True
    return False


def plan_driveways(cfg, mapdata):
    """OSM driveways were read in process_osm. For each pitched-roof house with none close to it, add a straight one
    from the footprint edge nearest a road to that road. Skipped when it would run through another building or water,
    or is too long or too short. Visual only (the car drives on the terrain, which is not touched)."""
    dc = cfg["driveways"]
    skip = set(dc["skip_road_classes"])
    road_pts = []
    for r in mapdata["roads"]:
        if r["class"] in skip or r["tunnel"]:
            continue
        for p in dens(r["points"], 1.0):
            road_pts.append((p[0], p[1], r["width"]))
    road_pts = np.array(road_pts)
    road_tree = cKDTree(road_pts[:, :2])
    osm_pts = np.concatenate([dens(d["points"], 1.0) for d in mapdata["driveways"]]) if mapdata["driveways"] else np.zeros((0, 2))
    osm_tree = cKDTree(osm_pts) if len(osm_pts) else None
    builds = mapdata["buildings"]
    bboxes = np.array([[min(p[0] for p in b["footprint"]), min(p[1] for p in b["footprint"]),
                        max(p[0] for p in b["footprint"]), max(p[1] for p in b["footprint"])] for b in builds])
    masks_water = Mask(mapdata["size_x_m"], mapdata["size_z_m"])
    for w in mapdata["water"]:
        masks_water.poly(w["outline"], 0.5)
    for st in mapdata["streams"]:
        if not st["tunnel"]:
            masks_water.line(st["points"], 3.0)
    water = masks_water.array()
    H = mapdata["size_z_m"]
    houses = [i for i, b in enumerate(builds) if b["roof"] == "gable" and b["type"] in
              ("detached", "house", "semidetached_house", "terrace", "residential", "bungalow")]
    stats = {"houses": len(houses), "had_osm": 0, "added": 0, "too_far": 0, "blocked": 0}
    for i in houses:
        ring = [tuple(p) for p in builds[i]["footprint"]]
        edge = dens(ring + [ring[0]], 1.0)
        if osm_tree is not None and osm_tree.query(edge)[0].min() < dc["has_one_within_m"]:
            stats["had_osm"] += 1
            continue
        dist, idx = road_tree.query(edge)
        order = np.argsort(dist)
        done = False
        reason = "too_far"
        tried = []
        for k in order:
            if dist[k] > dc["max_length_m"]:
                break
            a = (float(edge[k][0]), float(edge[k][1]))
            rp = road_pts[idx[k]]
            if any(math.hypot(a[0] - t[0], a[1] - t[1]) < 6 for t in tried):
                continue
            tried.append(a)
            b = (float(rp[0]), float(rp[1]))
            L = math.hypot(b[0] - a[0], b[1] - a[1])
            if L < dc["min_length_m"] + rp[2] / 2:
                continue
            # leave the wall in the direction of the road and stop at the road edge, not its centre line
            ux, uz = (b[0] - a[0]) / L, (b[1] - a[1]) / L
            b = (b[0] - ux * rp[2] / 2, b[1] - uz * rp[2] / 2)
            ok = True
            lo = (min(a[0], b[0]) - 1.5, min(a[1], b[1]) - 1.5, max(a[0], b[0]) + 1.5, max(a[1], b[1]) + 1.5)
            for j in np.nonzero((bboxes[:, 0] <= lo[2]) & (bboxes[:, 2] >= lo[0]) & (bboxes[:, 1] <= lo[3]) & (bboxes[:, 3] >= lo[1]))[0]:
                if j == i:
                    continue
                if segment_hits_ring(a, b, [tuple(p) for p in builds[j]["footprint"]], 1.2):
                    ok = False
                    break
            if ok:
                for q in dens([a, b], 1.0):
                    c, r = int(q[0]), int(H - q[1])
                    if 0 <= r < water.shape[0] and 0 <= c < water.shape[1] and water[r, c]:
                        ok = False
                        break
            if not ok:
                reason = "blocked"
                continue
            # start slightly inside the footprint so the strip meets the wall with no gap
            a2 = (a[0] + ux * 0.4, a[1] + uz * 0.4)
            mapdata["driveways"].append({"points": [(round(a2[0], 2), round(a2[1], 2)), (round(b[0], 2), round(b[1], 2))],
                                         "width": dc["width_m"], "source": "generated"})
            stats["added"] += 1
            done = True
            break
        if not done:
            stats["too_far" if reason == "too_far" else "blocked"] += 1
    log(f"Driveways: {len([d for d in mapdata['driveways'] if d['source'] == 'osm'])} from OSM, {stats['added']} generated for "
        f"{stats['houses']} houses ({stats['had_osm']} already had an OSM driveway nearby, {stats['too_far']} no road within "
        f"{dc['max_length_m']:.0f} m, {stats['blocked']} blocked by another building or water)")
    return stats


# ---------------------------------------------------------------- trees

def fetch_naip(cfg, rect):
    """4-band (R, G, B, near-infrared) NAIP imagery for the crop at 1 m, from USGS's public NAIP image service.
    Cached in raw/ (not committed). Returns the array (4, rows, cols) with row 0 = north, or None if it can't be fetched."""
    w, s, e, n = rect
    cols, rows = e - w, n - s
    path = os.path.join(RAW, "naip_4band.tif")
    key = f"{w},{s},{e},{n}"
    keypath = os.path.join(RAW, "naip_4band.key")
    if os.path.exists(path) and os.path.exists(keypath) and open(keypath).read() == key:
        log(f"NAIP: using cache {path}")
    else:
        nc = cfg["naip"]
        if cols > nc["max_px"] or rows > nc["max_px"]:
            log(f"NAIP: crop {cols} x {rows} m is over the service's {nc['max_px']} px limit; not handled")
            return None
        q = urllib.parse.urlencode({"bbox": f"{w},{s},{e},{n}", "bboxSR": cfg["utm_epsg"], "imageSR": cfg["utm_epsg"],
                                    "size": f"{cols},{rows}", "format": "tiff", "pixelType": "U8",
                                    "interpolation": "RSP_NearestNeighbor", "f": "image"})
        try:
            log("NAIP: downloading 4-band imagery from USGS ...")
            req = urllib.request.Request(nc["url"] + "?" + q, headers={"User-Agent": USER_AGENT})
            data = urllib.request.urlopen(req, timeout=600).read()
            if data[:2] not in (b"II", b"MM"):
                raise RuntimeError("response is not a TIFF: " + data[:200].decode("latin1"))
            os.makedirs(RAW, exist_ok=True)
            with open(path, "wb") as f:
                f.write(data)
            with open(keypath, "w") as f:
                f.write(key)
        except Exception as ex:
            log(f"NAIP: could not fetch ({ex})")
            return None
    with rasterio.open(path) as src:
        if src.count < 4 or (src.height, src.width) != (rows, cols):
            log(f"NAIP: cached image is {src.width} x {src.height} x {src.count}, expected {cols} x {rows} x 4; ignoring it")
            return None
        return src.read().astype(np.float32)


def canopy_from_naip(cfg, naip):
    """Tree canopy as a boolean raster: green in near-infrared (NDVI) and either textured (lawns are smooth) or dark
    (shaded canopy); small speckles removed. Tuned by eye against the imagery around Chastain Glen Lane."""
    tc = cfg["trees"]
    R, G, B, N = naip
    ndvi = (N - R) / (N + R + 1e-3)
    mean = ndi.uniform_filter(N, 5)
    sd = np.sqrt(np.maximum(ndi.uniform_filter(N * N, 5) - mean * mean, 0))
    lum = (R + G + B) / 3
    m = ((ndvi > tc["ndvi_min"]) & (sd > tc["texture_min"])) | ((ndvi > 0.1) & (lum < tc["dark_lum_max"]))
    m = ndi.binary_closing(m, iterations=2)
    m = ndi.binary_opening(m, iterations=1)
    lab, n = ndi.label(m)
    if n:
        sizes = ndi.sum(m, lab, range(1, n + 1))
        m = np.isin(lab, 1 + np.nonzero(sizes >= tc["min_blob_px"])[0])
    return m


def hash01(i, j, salt):
    """Deterministic per-cell pseudo-random numbers in [0, 1) (so tree positions never change between runs)."""
    x = (i.astype(np.uint64) * np.uint64(73856093)) ^ (j.astype(np.uint64) * np.uint64(19349663)) ^ np.uint64(salt * 83492791 + 1)
    x &= np.uint64(0xFFFFFFFF)
    for mult in (0x45d9f3b, 0x45d9f3b):
        x ^= x >> np.uint64(16)
        x = (x * np.uint64(mult)) & np.uint64(0xFFFFFFFF)
    x ^= x >> np.uint64(16)
    return (x & np.uint64(0xFFFFFF)).astype(np.float64) / float(1 << 24)


def plan_trees(cfg, mapdata, rect):
    """One tree per jittered-grid cell where there is canopy, none on roads, buildings, water, bridges or driveways;
    thinned in yards. Writes Data/trees.bin: 'CRT1', uint32 count, then per tree float32 x, float32 z (metres from the
    crop's south-west corner), uint16 seed, uint8 kind (0 woods, 1 yard), uint8 depth (tenths of a metre inside the canopy)."""
    tc = cfg["trees"]
    W, H = int(mapdata["size_x_m"]), int(mapdata["size_z_m"])
    masks = keep_off_masks(cfg, mapdata)
    blocked = masks["roads"] | masks["buildings"] | masks["water"] | masks["driveways"]
    near_things = masks["buildings"] | masks["roads"]
    dist_thing = ndi.distance_transform_edt(~near_things)  # metres to the nearest building or road
    near_bld = ndi.distance_transform_edt(~masks["buildings"])
    naip = fetch_naip(cfg, rect)
    if naip is not None:
        canopy = canopy_from_naip(cfg, naip)
        source = "NAIP 4-band imagery (NDVI + texture)"
    else:
        canopy = np.ones((H, W), bool)
        source = "RULE FALLBACK (no imagery): dense woods more than %.0f m from any road or building, sparse in yards" % tc["rule_forest_distance_m"]
    depth = ndi.distance_transform_edt(canopy)
    S = tc["cell_m"]
    ni, nj = int(math.ceil(W / S)), int(math.ceil(H / S))
    ii, jj = np.meshgrid(np.arange(ni), np.arange(nj), indexing="ij")
    u = 0.12 + 0.76 * hash01(ii, jj, 1)
    v = 0.12 + 0.76 * hash01(ii, jj, 2)
    x = (ii + u) * S
    z = (jj + v) * S
    c = np.clip(x.astype(int), 0, W - 1)
    r = np.clip((H - z).astype(int), 0, H - 1)
    ok = (x < W - 2) & (z < H - 2) & (x > 2) & (z > 2)
    ok &= canopy[r, c] & ~blocked[r, c]
    yard_keep = hash01(ii, jj, 3)
    if naip is not None:
        yard = near_bld[r, c] < tc["yard_distance_m"]
        ok &= ~yard | (yard_keep < tc["yard_keep"])
    else:
        yard = dist_thing[r, c] < tc["rule_forest_distance_m"]
        ok &= ~yard | (yard_keep < tc["rule_yard_keep"])
    seed = (hash01(ii, jj, 4) * 65536).astype(np.uint16)
    sel = ok
    out_x, out_z = x[sel], z[sel]
    order = np.lexsort((out_x, out_z))  # fixed order, so the file is identical run to run
    out_x, out_z = out_x[order], out_z[order]
    out_seed, out_yard = seed[sel][order], yard[sel][order].astype(np.uint8)
    out_depth = np.clip(depth[r, c][sel][order] * 10, 0, 255).astype(np.uint8)
    path = os.path.join(DATA, "trees.bin")
    with open(path, "wb") as f:
        f.write(b"CRT1" + struct.pack("<I", len(out_x)))
        rec = np.zeros(len(out_x), dtype=[("x", "<f4"), ("z", "<f4"), ("s", "<u2"), ("k", "u1"), ("d", "u1")])
        rec["x"], rec["z"], rec["s"], rec["k"], rec["d"] = out_x, out_z, out_seed, out_yard, out_depth
        f.write(rec.tobytes())
    log(f"Trees: {len(out_x)} placed from {source}; canopy covers {canopy.mean() * 100:.0f}% of the crop; "
        f"{int(out_yard.sum())} in yards, {len(out_x) - int(out_yard.sum())} in woods -> {path}")
    if naip is not None:  # preview for a human check: canopy outline and tree dots over the imagery
        prev = np.clip(naip[:3].transpose(1, 2, 0), 0, 255).astype(np.uint8).copy()
        edge = canopy & ~ndi.binary_erosion(canopy)
        prev[edge] = (0, 255, 0)
        for px, pz in zip(out_x, out_z):
            rr, cc = int(H - pz), int(px)
            prev[max(0, rr - 1):rr + 2, max(0, cc - 1):cc + 2] = (255, 255, 0)
        Image.fromarray(prev).save(os.path.join(RAW, "tree_preview.png"))
    return len(out_x)


def write_map(mapdata):
    out = dict(mapdata)
    out["roads"] = [{kk: v for kk, v in r.items() if kk not in ("taper_start", "taper_end")} for r in mapdata["roads"]]
    path = os.path.join(DATA, "map.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(flat_for_unity(out), f, ensure_ascii=False, separators=(",", ":"))
    log(f"Map: {len(out['roads'])} road pieces, {len(out['bridges'])} bridges, {len(out['buildings'])} buildings -> {path}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--refresh-osm", action="store_true", help="re-download OSM even if cached")
    args = ap.parse_args()
    with open(os.path.join(os.path.dirname(__file__), "config.json"), encoding="utf-8") as f:
        cfg = json.load(f)
    os.makedirs(DATA, exist_ok=True)
    rect, clamped = utm_rect(cfg)
    tr = Transformer.from_crs(cfg["utm_epsg"], 4326, always_xy=True)
    sw, ne, nw, se = (tr.transform(rect[0], rect[1]), tr.transform(rect[2], rect[3]),
                      tr.transform(rect[0], rect[3]), tr.transform(rect[2], rect[1]))
    log(f"Requested bounds: {cfg['bounds']}")
    log(f"Crop: UTM {rect} = {rect[2] - rect[0]} m east-west x {rect[3] - rect[1]} m north-south; "
        f"lat/lon corners SW {sw[1]:.5f},{sw[0]:.5f}  SE {se[1]:.5f},{se[0]:.5f}  NW {nw[1]:.5f},{nw[0]:.5f}  NE {ne[1]:.5f},{ne[0]:.5f}")
    if clamped:
        log(f"*** The requested area runs past the elevation tile on the {', '.join(clamped)} side: stopped at the tile edge. ***")
    osm = fetch_osm(cfg, args.refresh_osm, rect)
    dem = build_heightmap(cfg, rect)
    mapdata = process_osm(cfg, osm, rect)
    core_h, core_meta = build_terrain(cfg, dem, mapdata)
    plan_driveways(cfg, mapdata)
    plan_trees(cfg, mapdata, rect)
    if cfg.get("extension"):   # Round 59: the map extension (Tools/mapgen/mapgen_extend.py): new land appended, the core as it was
        import mapgen_extend
        mapgen_extend.extend(cfg, rect, mapdata, core_h, core_meta, args.refresh_osm)
    write_map(mapdata)
    log("Done. In Unity: Chastain Rush > Build Map (or run the batch build).")


if __name__ == "__main__":
    sys.exit(main())
