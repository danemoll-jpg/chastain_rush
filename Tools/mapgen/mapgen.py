"""Chastain Rush map generator.

Re-runnable, driven by the bounds in config.json. Steps:
  1. fetch   - OpenStreetMap data for the bounds (cached in raw/)
  2. dem     - download USGS 1 m tile(s) (raw/), crop to the bounds -> Data/heightmap_1m.tif
  3. process - project OSM to UTM metres, build roads / buildings / water -> Data/map.json
  4. terrain - resample heightmap to the terrain grid, flatten under roads -> Assets/Generated/terrain.r16

World frame (shared with Unity): x = easting - origin_e, z = northing - origin_n,
y = elevation in metres (NAVD88). 1 unit = 1 metre, no vertical exaggeration.

Usage:  python Tools/mapgen/mapgen.py [--refresh-osm]
"""
import argparse
import json
import math
import os
import sys
import urllib.parse
import urllib.request

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.merge import merge
from rasterio.windows import from_bounds
from pyproj import Transformer

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

def fetch_osm(cfg, refresh):
    b = cfg["bounds"]
    path = os.path.join(RAW, "osm_box.json")
    key = f'{b["south"]},{b["west"]},{b["north"]},{b["east"]}'
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

def utm_rect(cfg):
    """Square UTM rectangle (whole metres) that contains the lat/lon bounds."""
    b = cfg["bounds"]
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    xs, ys = zip(*[tr.transform(lon, lat) for lon in (b["west"], b["east"]) for lat in (b["south"], b["north"])])
    w, e, s, n = min(xs), max(xs), min(ys), max(ys)
    side = math.ceil(max(e - w, n - s))
    cx, cy = (w + e) / 2, (s + n) / 2
    w0, s0 = math.floor(cx - side / 2), math.floor(cy - side / 2)
    return w0, s0, w0 + side, s0 + side


def build_heightmap(cfg, rect):
    os.makedirs(RAW, exist_ok=True)
    paths = []
    for url in cfg["elevation_tiles"]:
        p = os.path.join(RAW, url.rsplit("/", 1)[1])
        if not os.path.exists(p):
            log(f"DEM: downloading {url}")
            urllib.request.urlretrieve(url, p)
        paths.append(p)
    srcs = [rasterio.open(p) for p in paths]
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


def process_osm(cfg, osm, rect):
    w0, s0, e0, n0 = rect
    tr = Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    nodes = {}
    for el in osm["elements"]:
        if el["type"] == "node":
            x, y = tr.transform(el["lon"], el["lat"])
            nodes[el["id"]] = (round(x - w0, 2), round(y - s0, 2))
    ways = {el["id"]: el for el in osm["elements"] if el["type"] == "way"}
    size = e0 - w0

    def pts_of(way):
        return [nodes[n] for n in way["nodes"] if n in nodes]

    def inside(p, margin=0.0):
        return -margin <= p[0] <= size + margin and -margin <= p[1] <= size + margin

    roads, buildings, water, streams, woods = [], [], [], [], []
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
            if hw == "service" and t.get("service") in cfg["excluded_service"]:
                continue
            if not any(inside(p) for p in pts):
                continue
            for run, ids in clip_runs(pts, [n for n in way["nodes"] if n in nodes], inside):
                roads.append({"name": t.get("name", ""), "class": hw, "width": road_width(t, cfg),
                              "oneway": t.get("oneway") == "yes", "points": run, "node_ids": ids})
        elif "building" in t and way["nodes"][0] == way["nodes"][-1] and len(pts) >= 4:
            ring = pts[:-1]
            cx = sum(p[0] for p in ring) / len(ring)
            cz = sum(p[1] for p in ring) / len(ring)
            if not inside((cx, cz)):
                continue
            if poly_area(ring) < 0:
                ring = ring[::-1]  # counter-clockwise
            b = {"type": t["building"], "height": round(building_height(t, cfg), 2), "footprint": ring}
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
                streams.append({"name": t.get("name", ""), "kind": t["waterway"], "points": run})
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
        "bounds": cfg["bounds"], "utm_epsg": cfg["utm_epsg"],
        "origin_utm": [w0, s0], "size_m": size,
        "roads": roads, "buildings": buildings, "water": water, "streams": streams, "woods": woods,
        "spawn": spawn,
    }
    path = os.path.join(DATA, "map.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(flat_for_unity(out), f, ensure_ascii=False, separators=(",", ":"))
    log(f"OSM: {len(roads)} road ways, {len(buildings)} buildings, {len(water)} water areas, "
        f"{len(streams)} streams, {len(woods)} woods -> {path}")
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
    out["buildings"] = [dict(b, footprint=flat(b["footprint"])) for b in mapdata["buildings"]]
    out["water"] = [dict(w, outline=flat(w["outline"])) for w in mapdata["water"]]
    out["streams"] = [dict(s, points=flat(s["points"])) for s in mapdata["streams"]]
    out["woods"] = [dict(w, outline=flat(w["outline"])) for w in mapdata["woods"]]
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


# ---------------------------------------------------------------- terrain

def build_terrain(cfg, dem_path, mapdata):
    res = cfg["terrain_resolution"]
    with rasterio.open(dem_path) as src:
        # grid sample i spans the full crop, row 0 = south (Unity terrain convention)
        h = src.read(1, out_shape=(res, res), resampling=Resampling.bilinear)[::-1].astype(np.float64)
    size = mapdata["size_m"]
    step = size / (res - 1)
    raw = h.copy()

    def sample(x, z):
        gx = np.clip(x / step, 0, res - 1.001)
        gz = np.clip(z / step, 0, res - 1.001)
        i0, j0 = gz.astype(int), gx.astype(int)
        fz, fx = gz - i0, gx - j0
        return (raw[i0, j0] * (1 - fx) * (1 - fz) + raw[i0, j0 + 1] * fx * (1 - fz) +
                raw[i0 + 1, j0] * (1 - fx) * fz + raw[i0 + 1, j0 + 1] * fx * fz)

    fl = cfg["road_flatten"]
    best_w = np.zeros_like(h)
    best_d = np.full_like(h, np.inf)
    target = np.zeros_like(h)
    for road in mapdata["roads"]:
        pts = np.array(road["points"], dtype=np.float64)
        seglen = np.hypot(*np.diff(pts, axis=0).T)
        if seglen.sum() < 0.5:
            continue
        # densify to ~1 m, sample the bare-earth profile and smooth it along the road
        cum = np.concatenate([[0], np.cumsum(seglen)])
        s = np.linspace(0, cum[-1], max(2, int(cum[-1]) + 1))
        dx = np.interp(s, cum, pts[:, 0])
        dz = np.interp(s, cum, pts[:, 1])
        prof = sample(dx, dz)
        k = max(1, int(fl["profile_smoothing_m"] / 2))
        padded = np.pad(prof, k, mode="edge")
        prof = np.convolve(padded, np.ones(2 * k + 1) / (2 * k + 1), mode="valid")
        core = road["width"] / 2 + fl["shoulder_m"]
        reach = core + fl["blend_m"]
        for a in range(len(dx) - 1):
            ax, az, bx, bz = dx[a], dz[a], dx[a + 1], dz[a + 1]
            ha, hb = prof[a], prof[a + 1]
            j0 = max(0, int((min(ax, bx) - reach) / step))
            j1 = min(res - 1, int((max(ax, bx) + reach) / step) + 1)
            i0 = max(0, int((min(az, bz) - reach) / step))
            i1 = min(res - 1, int((max(az, bz) + reach) / step) + 1)
            if j0 > j1 or i0 > i1:
                continue
            gx = np.arange(j0, j1 + 1) * step
            gz = np.arange(i0, i1 + 1) * step
            X, Z = np.meshgrid(gx, gz)
            vx, vz = bx - ax, bz - az
            L2 = vx * vx + vz * vz or 1e-9
            t = np.clip(((X - ax) * vx + (Z - az) * vz) / L2, 0, 1)
            d = np.hypot(X - (ax + t * vx), Z - (az + t * vz))
            wgt = np.clip((reach - d) / fl["blend_m"], 0, 1)
            wgt = wgt * wgt * (3 - 2 * wgt)  # smoothstep
            sl = (slice(i0, i1 + 1), slice(j0, j1 + 1))
            better = (wgt > best_w[sl] + 1e-6) | ((np.abs(wgt - best_w[sl]) <= 1e-6) & (d < best_d[sl]))
            better &= wgt > 0
            best_w[sl] = np.where(better, wgt, best_w[sl])
            best_d[sl] = np.where(better, d, best_d[sl])
            target[sl] = np.where(better, ha + t * (hb - ha), target[sl])
    h = h * (1 - best_w) + target * best_w

    lo, hi = float(h.min()) - 1.0, float(h.max()) + 1.0
    norm = np.round((h - lo) / (hi - lo) * 65535).astype("<u2")
    os.makedirs(GENERATED, exist_ok=True)
    out = os.path.join(GENERATED, "terrain.r16")
    norm.tofile(out)
    meta = {"resolution": res, "size_m": size, "min_height": lo, "max_height": hi}
    with open(os.path.join(GENERATED, "terrain.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=1)
    moved = np.abs(h - raw)
    log(f"Terrain: {res}x{res} ({step:.3f} m/sample), {lo:.1f}-{hi:.1f} m; road flattening moved "
        f"{(moved > 0.05).mean() * 100:.1f}% of samples, max {moved.max():.2f} m -> {out}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--refresh-osm", action="store_true", help="re-download OSM even if cached")
    args = ap.parse_args()
    with open(os.path.join(os.path.dirname(__file__), "config.json"), encoding="utf-8") as f:
        cfg = json.load(f)
    os.makedirs(DATA, exist_ok=True)
    rect = utm_rect(cfg)
    log(f"Bounds: {cfg['bounds']} -> UTM {rect} ({rect[2] - rect[0]} m square)")
    osm = fetch_osm(cfg, args.refresh_osm)
    dem = build_heightmap(cfg, rect)
    mapdata = process_osm(cfg, osm, rect)
    build_terrain(cfg, dem, mapdata)
    log("Done. In Unity: Chastain Rush > Build Map (or run the batch build).")


if __name__ == "__main__":
    sys.exit(main())
