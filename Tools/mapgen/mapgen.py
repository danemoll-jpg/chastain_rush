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


def process_osm(cfg, osm, rect):
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
    owner_cfg = None
    owner_path = os.path.join(ROOT, cfg["owner_house_file"])
    if os.path.exists(owner_path):
        with open(owner_path, encoding="utf-8") as f:
            owner_cfg = json.load(f)
    owner = None

    for way in ways.values():
        t = way.get("tags", {})
        pts = pts_of(way)
        if len(pts) < 2:
            continue
        hw = t.get("highway")
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
                              "osm_id": way["id"]})
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

    for rel in (el for el in osm["elements"] if el["type"] == "relation"):
        t = rel.get("tags", {})
        if t.get("natural") == "water":
            for m in rel["members"]:
                if m["type"] == "way" and m.get("role") == "outer" and m["ref"] in ways:
                    pts = pts_of(ways[m["ref"]])
                    if len(pts) > 3 and pts[0] == pts[-1] and any(inside(p) for p in pts):
                        ring = pts[:-1]
                        water.append({"name": t.get("name", ""), "outline": ring if poly_area(ring) > 0 else ring[::-1]})

    spawn = find_spawn(cfg, roads)
    for r in roads:
        r.pop("node_ids")
    out = {
        "attribution": "© OpenStreetMap contributors (ODbL); elevation: USGS 3DEP 1 m (public domain)",
        "bounds": cfg["bounds"], "utm_epsg": cfg["utm_epsg"], "vertical_scale": vscale(cfg),
        "origin_utm": [w0, s0], "size_x_m": size_x, "size_z_m": size_z,
        "roads": roads, "buildings": buildings, "water": water, "streams": streams, "woods": woods,
        "driveways": driveways, "spawn": spawn,
    }
    log(f"OSM: {len(roads)} road ways, {len(buildings)} buildings, {len(water)} water areas, "
        f"{len(streams)} streams, {len(woods)} woods, {len(driveways)} OSM driveways")
    os.makedirs(LOCAL, exist_ok=True)
    marker_path = os.path.join(LOCAL, "owner_marker.json")
    if owner:
        with open(marker_path, "w", encoding="utf-8") as f:
            json.dump(owner, f)
        log(f"Owner house: found (building #{owner['index']}) -> {marker_path} (not committed)")
    elif owner_cfg:
        log("Owner house: NOT found in OSM building addresses")
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

    fl = cfg["road_flatten"]
    taper = cfg["bridges"]["end_taper_m"]
    best_w = np.zeros_like(h)
    best_d = np.full_like(h, np.inf)
    target = np.zeros_like(h)
    for road in mapdata["roads"]:
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
            better = (wgt > best_w[sl] + 1e-6) | ((np.abs(wgt - best_w[sl]) <= 1e-6) & (d < best_d[sl]))
            better &= wgt > 0
            best_w[sl] = np.where(better, wgt, best_w[sl])
            best_d[sl] = np.where(better, d, best_d[sl])
            target[sl] = np.where(better, ha + t * (hb - ha), target[sl])
    h = (h * (1 - best_w) + target * best_w).astype(np.float32)

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
    build_terrain(cfg, dem, mapdata)
    plan_driveways(cfg, mapdata)
    plan_trees(cfg, mapdata, rect)
    write_map(mapdata)
    log("Done. In Unity: Chastain Rush > Build Map (or run the batch build).")


if __name__ == "__main__":
    sys.exit(main())
