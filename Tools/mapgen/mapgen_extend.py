"""Round 59 (Dan, 2026-10-22: "can we possibly also add on the first stage of expanding the map out to what we had written up the other
night?"): THE MAP EXTENSION, stage 1, Town Center at Cobb and Kennesaw State University's Kennesaw campus. Called by mapgen.py when
config.json has "extension".

Everything that exists keeps its place and its data (Dan: "Every existing coordinate ... must still mean the same place"):
  - the core crop (config "bounds") is generated exactly as before, by mapgen.py, first: map.json's lists keep their order (house,
    road and bridge numbers that other data files name stay the same), trees.bin's trees, terrain.r16 and terrain.json (the python
    scripts that sample the ground read those, unchanged), Data/heightmap_1m.tif;
  - the new land lies west and south of it, so its coordinates are negative (x = easting - the core's west edge, z = northing - the
    core's south edge, as before). The terrain grid is extended by whole tiles of the core's own size (tiles_west, tiles_south), so
    every core sample stays on the same spot; the core's samples are pasted in as they are, and the new samples within 'seam_m' of
    the core are eased onto the core's edge;
  - the new pieces (roads, bridges, buildings, water, streams, woods, driveways, parking, trees) are appended after the core's;
    a road or stream that crosses the core's edge is cut there (the core keeps its own piece, the new piece starts at the edge);
  - the same height datum as the core (the vertical scale is about the core's lowest point), so heights mean the same too.
Writes (besides the core files): Data/heightmap_ext_1m.tif (the extension's DEM crop), Assets/Generated/terrain_full.r16 and
terrain_full.json (the whole grid, for Unity: origin_x_m / origin_z_m), map.json's "extent_m" [x0, z0, x1, z1] and "extension".
"""
import json
import math
import os
import struct
import urllib.parse
import urllib.request

import numpy as np
import rasterio
from PIL import Image
from scipy import ndimage as ndi

import mapgen as M


def log(msg):
    print(msg, flush=True)


def extend(cfg, core_rect, core, core_h, core_meta, refresh, ext=None, prev_box=None, last=True):
    """One stage of the extension. Round 61 (stage 2): 'ext' the stage's config ("extension", "extension2"); 'core_rect', 'core_h' and
    'core_meta' the map so far (stage 1: the core; stage 2: the core with stage 1: its UTM rectangle, terrain grid and meta); 'prev_box'
    that map's rectangle in the core's frame (x0, z0, x1, z1; stage 1: 0, 0 and the core's size). The new land is whole tiles west and
    south of it; everything there is appended after what exists. 'last': this stage writes the terrain. Returns (grid, meta, UTM rect)."""
    ext = ext or cfg["extension"]
    stage = int(ext.get("stage", 1))
    kx, kz = int(ext["tiles_west"]), int(ext["tiles_south"])
    tw, td = core_meta["tile_w_m"], core_meta["tile_d_m"]
    cells = cfg["terrain"]["tile_resolution"] - 1
    px0, pz0, px1, pz1 = prev_box or (0.0, 0.0, core["size_x_m"], core["size_z_m"])
    Wc, Hc = px1 - px0, pz1 - pz0                   # the map so far
    ox, oz = kx * tw, kz * td                       # its south-west corner in this stage's own frame
    gx0, gz0 = px0 - ox, pz0 - oz                   # this stage's frame origin in the core's frame (stage 1: -ox, -oz)
    cw0, cs0, ce0, cn0 = core_rect
    erect = (cw0 - ox, cs0 - oz, ce0, cn0)          # the whole map, UTM (its west and south edges fall between whole metres)
    We, He = ce0 - erect[0], cn0 - erect[1]
    log(f"Extension (stage {stage}): {kx} tiles west and {kz} south of the map so far ({tw:.2f} x {td:.2f} m tiles): "
        f"the map is now {We:.0f} x {He:.0f} m, x {gx0:.2f} to {px1:.0f}, z {gz0:.2f} to {pz1:.0f}")
    tr = M.Transformer.from_crs(cfg["utm_epsg"], 4326, always_xy=True)
    sw, ne = tr.transform(erect[0], erect[1]), tr.transform(erect[2], erect[3])
    log(f"Extension: bounds south {sw[1]:.5f}, west {sw[0]:.5f}, north {ne[1]:.5f}, east {ne[0]:.5f}")

    def in_core(p):   # the extension's frame
        return ox <= p[0] <= ox + Wc and oz <= p[1] <= oz + Hc

    # ---- OSM for the whole map, kept to what lies outside the core
    osm = fetch_osm_ext(cfg, refresh, erect, stage)
    em = M.process_osm(cfg, osm, erect, owner_marker=False)
    out = lambda p: not in_core(p)
    roads = []
    for r in em["roads"]:
        for run, _ in M.clip_runs(r["points"], list(range(len(r["points"]))), lambda p: not in_core(p) and -1e-6 <= p[0] <= We + 1e-6 and -1e-6 <= p[1] <= He + 1e-6):
            if len(run) >= 2 and sum(math.dist(a, b) for a, b in zip(run, run[1:])) > 0.5:
                roads.append(dict(r, points=run))
    em["roads"] = roads
    em["buildings"] = [b for b in em["buildings"] if out((sum(p[0] for p in b["footprint"]) / len(b["footprint"]), sum(p[1] for p in b["footprint"]) / len(b["footprint"])))]
    em["water"] = [w for w in em["water"] if not any(in_core(p) for p in w["outline"])]
    em["woods"] = [w for w in em["woods"] if not any(in_core(p) for p in w["outline"])]
    em["parking"] = [a for a in em["parking"] if not any(in_core(p) for p in a["outline"])]
    streams = []
    for st in em["streams"]:
        for run, _ in M.clip_runs(st["points"], list(range(len(st["points"]))), out):
            streams.append(dict(st, points=run))
    em["streams"] = streams
    drives = []
    for d in em["driveways"]:
        for run, _ in M.clip_runs(d["points"], list(range(len(d["points"]))), out):
            drives.append(dict(d, points=run))
    em["driveways"] = drives
    log(f"Extension: OSM outside the core: {len(em['roads'])} road pieces, {len(em['buildings'])} buildings, {len(em['water'])} water areas, "
        f"{len(em['streams'])} streams, {len(em['woods'])} woods, {len(em['driveways'])} OSM driveways, {len(em['parking'])} parking lots")

    # Round 61 (stage 2, B): the airport's runways, taxiways and aprons (over the whole map: the runway runs on into stage 1's land) and the
    # trails (named paths and tracks in the new land: Kennesaw Mountain's), for the map build; kept off by the new trees
    air, trails = ([], []) if stage < 2 else aeroways_and_trails(cfg, osm, erect, We, He, in_core)
    if stage >= 2:
        log(f"Extension: {sum(1 for a in air if a['kind'] == 'runway')} runways, {sum(1 for a in air if a['kind'] == 'taxiway')} taxiways, "
            f"{sum(1 for a in air if a['kind'] == 'apron')} aprons; {len(trails)} trail pieces ({sum(sum(math.dist(a, b) for a, b in zip(t['points'], t['points'][1:])) for t in trails):.0f} m)")

    # the core's things in the extension's frame (for levelling, driveways and trees near the seam)
    def sh(pts, dx, dz): return [(round(p[0] + dx, 2), round(p[1] + dz, 2)) for p in pts]
    core_roads_e = [dict(r, points=sh(r["points"], -gx0, -gz0)) for r in core["roads"]]
    core_bridges_e = [dict(b, points=sh(b["points"], -gx0, -gz0)) for b in core["bridges"]]
    core_build_e = [dict(b, footprint=sh(b["footprint"], -gx0, -gz0)) for b in core["buildings"]]

    # ---- the DEM and the terrain grid
    drect = (math.floor(erect[0]), math.floor(erect[1]), math.ceil(erect[2]), math.ceil(erect[3]))
    dem_path = build_heightmap_ext(cfg, drect, stage)
    with rasterio.open(dem_path) as src:
        dem = src.read(1)[::-1].astype(np.float32)   # row 0 = south
    nx, nz = core_meta["tiles_x"] + kx, core_meta["tiles_z"] + kz
    gx_n, gz_n = nx * cells + 1, nz * cells + 1
    sx, sz = core_meta["step_x_m"], core_meta["step_z_m"]
    fx0, fz0 = erect[0] - drect[0], erect[1] - drect[1]   # the frame's origin in the DEM's cells

    def axis(n, step, off, size):
        p = np.clip(np.arange(n) * step + off - 0.5, 0, size - 1.001)
        i0 = p.astype(int)
        return i0, (p - i0).astype(np.float32)
    j0, fx = axis(gx_n, sx, fx0, dem.shape[1])
    i0, fz = axis(gz_n, sz, fz0, dem.shape[0])
    rows = dem[:, j0] * (1 - fx) + dem[:, j0 + 1] * fx
    h = rows[i0, :] * (1 - fz)[:, None] + rows[i0 + 1, :] * fz[:, None]
    del rows
    k, datum = core_meta["vertical_scale"], core_meta["vertical_datum_m"]   # the core's datum: heights mean the same
    h = (datum + (h - datum) * k).astype(np.float32)
    raw = h.copy()

    def sample(x, z):
        gx = np.clip(np.asarray(x) / sx, 0, gx_n - 1.001)
        gz = np.clip(np.asarray(z) / sz, 0, gz_n - 1.001)
        i, j = gz.astype(int), gx.astype(int)
        fz_, fx_ = gz - i, gx - j
        return (raw[i, j] * (1 - fx_) * (1 - fz_) + raw[i, j + 1] * fx_ * (1 - fz_) +
                raw[i + 1, j] * (1 - fx_) * fz_ + raw[i + 1, j + 1] * fx_ * fz_).astype(np.float64)

    # ---- bridges on the new roads, then their levelling (with the core's roads, so the seam is levelled alike from both sides)
    em["bridges"] = []
    at_grade, _ = M.plan_bridges(cfg, em, sample)
    log(f"Extension: bridges {len(em['bridges'])} built, {len(at_grade)} crossings left at grade")
    join_bridges(core_bridges_e, em["bridges"])
    h = M.flatten_roads(cfg, h, sample, em["roads"] + [r for r in core_roads_e if near_box(r["points"], ox, oz, Wc, Hc, 80.0)], sx, sz, gx_n, gz_n)

    # ---- the core pasted in as it is; the new samples near it eased onto its edge
    ci, cj = kz * cells, kx * cells
    gzc, gxc = core_h.shape
    seam = float(ext.get("seam_m", 40.0))
    delta = np.zeros_like(h)
    # west band (left of the core, its rows), south band (below, its columns), the corner (from the core's corner sample)
    west_d = core_h[:, 0] - h[ci:ci + gzc, cj]
    south_d = core_h[0, :] - h[ci, cj:cj + gxc]
    corner_d = core_h[0, 0] - h[ci, cj]
    X = np.arange(gx_n) * sx; Z = np.arange(gz_n) * sz
    dxw = np.clip(ox - X, 0, None); dzs = np.clip(oz - Z, 0, None)
    wx = M.smoothstep(1 - np.clip(dxw / seam, 0, 1)); wz = M.smoothstep(1 - np.clip(dzs / seam, 0, 1))
    delta[ci:ci + gzc, :cj] = west_d[:, None] * wx[None, :cj]
    delta[:ci, cj:cj + gxc] = south_d[None, :] * wz[:ci, None]
    dist = np.hypot(dxw[None, :cj], dzs[:ci, None]); wc = M.smoothstep(1 - np.clip(dist / seam, 0, 1))
    delta[:ci, :cj] = corner_d * wc
    h = (h + delta).astype(np.float32)
    h[ci:ci + gzc, cj:cj + gxc] = core_h
    log(f"Extension: the seam eased over {seam:g} m (the most a new sample moved to meet the core: {np.abs(delta).max():.2f} m)")

    lo, hi = float(h.min()) - 1.0, float(h.max()) + 1.0
    meta = dict(core_meta)
    meta.update({"grid_x": gx_n, "grid_z": gz_n, "tiles_x": nx, "tiles_z": nz, "size_x_m": We, "size_z_m": He,
                 "origin_x_m": gx0, "origin_z_m": gz0, "min_height": lo, "max_height": hi, "core_tiles": [kx, kz, core_meta["tiles_x"], core_meta["tiles_z"]]})
    if stage > 1:   # Round 61: core_tiles stays stage 1's (the core's place in the grid moves by this stage's tiles: see stage<N>_tiles)
        meta["core_tiles"] = core_meta.get("core_tiles"); meta[f"stage{stage}_tiles"] = [kx, kz, core_meta["tiles_x"], core_meta["tiles_z"]]
    if last:
        norm = np.round((h - lo) / (hi - lo) * 65535).astype("<u2")
        norm.tofile(os.path.join(M.GENERATED, "terrain_full.r16"))
        del norm
        with open(os.path.join(M.GENERATED, "terrain_full.json"), "w", encoding="utf-8") as f:
            json.dump(meta, f, indent=1)
        log(f"Extension: terrain {gx_n} x {gz_n} samples = {nx} x {nz} tiles, origin ({gx0:.2f}, {gz0:.2f}), heights {h.min():.2f}-{h.max():.2f} m "
            f"(quantum {(hi - lo) / 65535 * 1000:.1f} mm) -> Assets/Generated/terrain_full.r16")

    # ---- driveways for the new houses (to any road, the core's near the seam too)
    tmp = dict(em, roads=em["roads"] + core_roads_e, size_x_m=We, size_z_m=He)
    M.plan_driveways(cfg, tmp)

    # ---- trees outside the core, on the core's own cell grid
    keep_off = [{"points": a["points"], "width": a["width"]} for a in air if a["kind"] != "apron"] + [{"points": a["outline"] + a["outline"][:1], "width": 4.0} for a in air if a["kind"] == "apron"]         + [{"points": t["points"], "width": t["width"] + 1.0} for t in trails]
    new_trees = plan_trees_ext(cfg, em, core_roads_e + keep_off, core_bridges_e, core_build_e, erect, We, He, gx0, gz0, (px0, pz0, px1, pz1), stage, (ox, oz))

    # ---- into the core's frame, appended after the core's own
    for r in em["roads"]: r["points"] = sh(r["points"], gx0, gz0)
    for b in em["bridges"]: b["points"] = sh(b["points"], gx0, gz0)
    for b in em["buildings"]: b["footprint"] = sh(b["footprint"], gx0, gz0)
    for w in em["water"]: w["outline"] = sh(w["outline"], gx0, gz0)
    for w in em["woods"]: w["outline"] = sh(w["outline"], gx0, gz0)
    for a in em["parking"]: a["outline"] = sh(a["outline"], gx0, gz0)
    for st in em["streams"]: st["points"] = sh(st["points"], gx0, gz0)
    for d in em["driveways"]: d["points"] = sh(d["points"], gx0, gz0)
    if stage >= 2:
        for a in air:
            if "points" in a: a["points"] = sh(a["points"], gx0, gz0)
            if "outline" in a: a["outline"] = sh(a["outline"], gx0, gz0)
        for t in trails: t["points"] = sh(t["points"], gx0, gz0)
        core["aeroways"] = core.get("aeroways", []) + air
        core["trails"] = core.get("trails", []) + trails
    n0 = {key: len(core[key]) for key in ("roads", "bridges", "buildings", "water", "streams", "woods", "driveways", "parking")}
    n_first = dict(n0, streams=sum(1 for s_ in core["streams"] if not s_["tunnel"]))   # as map.json lists them (tunnels are left out of it)
    for key in n0: core[key] = core[key] + em[key]
    core["extent_m"] = [round(gx0, 3), round(gz0, 3), px1, pz1]
    core["extension" if stage == 1 else f"extension{stage}"] = {"stage": stage, "name": ext.get("name", ""), "first": n_first,
                         "bounds": {"south": round(sw[1], 6), "west": round(sw[0], 6), "north": round(ne[1], 6), "east": round(ne[0], 6)}}
    append_trees(new_trees)
    log(f"Extension: appended {', '.join(f'{len(core[key]) - n0[key]} {key}' for key in n0)}; {len(new_trees[0])} trees")
    return h, meta, erect


def near_box(pts, ox, oz, W, H, m):
    return any(ox - m <= p[0] <= ox + W + m and oz - m <= p[1] <= oz + H + m for p in pts) and any(not (ox + m <= p[0] <= ox + W - m and oz + m <= p[1] <= oz + H - m) for p in pts)


def join_bridges(core_b, new_b, tol=0.6, ease=20.0):
    """A new bridge piece that starts or ends where a core bridge piece ends (a road cut at the old edge on a bridge: I-575's overpass)
    takes the core deck's height there, eased into its own over 'ease' m."""
    ends = []
    for b in core_b:
        for i in (0, -1):
            ends.append((b["points"][i], b["deck_y"][i], b["name"]))
    joined = 0
    for b in new_b:
        P, Y = b["points"], b["deck_y"]
        for idx in (0, len(P) - 1):
            best = min(ends, key=lambda e: math.dist(e[0], P[idx]), default=None)
            if best is None or math.dist(best[0], P[idx]) > tol:
                continue
            d0 = best[1] - Y[idx]
            cum = [0.0]
            order = range(len(P)) if idx == 0 else range(len(P) - 1, -1, -1)
            order = list(order)
            for a, c in zip(order, order[1:]): cum.append(cum[-1] + math.dist(P[a], P[c]))
            for s, i in zip(cum, order):
                if s > ease: break
                Y[i] = round(Y[i] + d0 * (1 - s / ease), 3)
            joined += 1
            log(f"Extension: bridge '{b['name']}' joined to the core's at ({P[idx][0]:.1f}, {P[idx][1]:.1f}), moved {d0:+.2f} m there")
    return joined


def fetch_osm_ext(cfg, refresh, rect, stage=1):
    south, west, north, east = M.osm_bbox(cfg, rect)
    key = f'{south:.5f},{west:.5f},{north:.5f},{east:.5f}'
    path = os.path.join(M.RAW, f"osm_ext{stage}.json")
    if os.path.exists(path) and not refresh:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        if data.get("_bounds_key") == key:
            log(f"Extension OSM: using cache {path}")
            return data
    query = f"[out:json][timeout:300];(nwr({key}););(._;>;);out body;"
    last = None
    for host in ("https://overpass-api.de/api/interpreter", "https://lz4.overpass-api.de/api/interpreter", "https://z.overpass-api.de/api/interpreter"):
        try:
            req = urllib.request.Request(host, data=urllib.parse.urlencode({"data": query}).encode(), headers={"User-Agent": M.USER_AGENT})
            data = json.load(urllib.request.urlopen(req, timeout=400))
            log(f"Extension OSM: fetched from {host}")
            break
        except Exception as e:
            last = e; log(f"Extension OSM: {host} failed ({e})")
    else:
        raise SystemExit(f"Extension OSM: could not fetch ({last})")
    data["_bounds_key"] = key
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f)
    return data


def build_heightmap_ext(cfg, rect, stage=1):
    srcs = [rasterio.open(p) for p in M.tile_paths(cfg)]
    w, s, e, n = rect
    arr, transform = M.merge(srcs, bounds=(w, s, e, n), res=1.0, nodata=-999999.0)
    arr = arr[0]
    missing = int((arr <= -9999).sum())
    if missing:
        raise SystemExit(f"Extension DEM: {missing} cells have no data - add the neighbouring tile to config.json")
    # Round 61: stage 2's crop (a couple of hundred MB) is too big for the repository: it stays in raw/ (made again from the tiles)
    out = os.path.join(M.DATA, "heightmap_ext_1m.tif") if stage == 1 else os.path.join(M.RAW, f"heightmap_ext{stage}_1m.tif")
    profile = dict(driver="GTiff", height=arr.shape[0], width=arr.shape[1], count=1, dtype="float32", crs=srcs[0].crs, transform=transform,
                   compress="deflate", predictor=3, nodata=-999999.0)
    with rasterio.open(out, "w", **profile) as dst:
        dst.write(arr.astype(np.float32), 1)
    for src in srcs: src.close()
    log(f"Extension DEM: cropped {arr.shape[1]} x {arr.shape[0]} m, elevation {arr.min():.1f}-{arr.max():.1f} m -> {out}")
    return out


def fetch_naip_ext(cfg, rect, W, H, stage=1, inner=None):
    """NAIP for the whole map in pieces under the service's size limit (cached in raw/naip_ext1_*.tif), mosaicked; (4, H, W), row 0 = north.
    A piece the service keeps timing out on is asked for in four smaller ones (twice over); what still cannot be had is left at 0
    (plan_trees_ext uses mapgen's rule there instead of the imagery)."""
    import time
    nc = cfg["naip"]; mx = int(nc.get("ext_piece_px", 1600))   # the service times out on pieces near its 4000 px limit
    w, s, e, n = rect
    img = np.zeros((4, int(H), int(W)), np.float32)
    missing = []

    def get(x0, x1, r0, r1, name, depth):
        bw, be, bn, bs = w + x0, w + x1, n - r0, n - r1
        if inner is not None and x0 >= inner[0] and H - r1 >= inner[1]:   # Round 61: a piece well inside the map so far (it keeps its own trees)
            return
        path = os.path.join(M.RAW, f"naip_ext{stage}_{name}.tif"); key = f"{bw},{bs},{be},{bn}"; keypath = path + ".key"
        if not (os.path.exists(path) and os.path.exists(keypath) and open(keypath).read() == key):
            q = urllib.parse.urlencode({"bbox": key, "bboxSR": cfg["utm_epsg"], "imageSR": cfg["utm_epsg"], "size": f"{x1 - x0},{r1 - r0}",
                                        "format": "tiff", "pixelType": "U8", "interpolation": "RSP_NearestNeighbor", "f": "image"})
            log(f"Extension NAIP: downloading piece {name} ({x1 - x0} x {r1 - r0} px) ...")
            data = None
            for attempt in range(3 if depth < 2 else 4):
                try:
                    data = urllib.request.urlopen(urllib.request.Request(nc["url"] + "?" + q, headers={"User-Agent": M.USER_AGENT}), timeout=600).read()
                    if data[:2] not in (b"II", b"MM"): raise RuntimeError("not a TIFF: " + data[:120].decode("latin1"))
                    break
                except Exception as ex:
                    data = None; log(f"Extension NAIP: piece {name} attempt {attempt + 1} failed ({ex})"); time.sleep(5)
            if data is None:
                if depth < 2 and x1 - x0 > 200 and r1 - r0 > 200:
                    xm, rm = (x0 + x1) // 2, (r0 + r1) // 2
                    for (a0, a1, b0, b1, k) in ((x0, xm, r0, rm, "a"), (xm, x1, r0, rm, "b"), (x0, xm, rm, r1, "c"), (xm, x1, rm, r1, "d")):
                        get(a0, a1, b0, b1, name + k, depth + 1)
                else:
                    missing.append((x0, x1, r0, r1)); log(f"Extension NAIP: piece {name} left out (the rule is used there)")
                return
            with open(path, "wb") as f: f.write(data)
            with open(keypath, "w") as f: f.write(key)
        with rasterio.open(path) as src:
            a = src.read().astype(np.float32)
        img[:, r0:r1, x0:x1] = a[:4, :r1 - r0, :x1 - x0]

    ni, nj = math.ceil(W / mx), math.ceil(H / mx)
    cw, ch = math.ceil(W / ni), math.ceil(H / nj)
    for i in range(ni):
        for j in range(nj):
            get(i * cw, min(int(W), (i + 1) * cw), j * ch, min(int(H), (j + 1) * ch), f"{i}_{j}", 0)
    return img, missing


def plan_trees_ext(cfg, em, core_roads_e, core_bridges_e, core_build_e, erect, We, He, gx0, gz0, prev_box, stage=1, prev_at=None):
    """mapgen.plan_trees' rule on the whole map, on the core's own cell grid (a cell's place and seed are those it would have had), kept
    outside the map so far ('prev_box', the core's frame; this stage's frame starts at gx0, gz0 there; 'prev_at' where that map's
    south-west corner is in this stage's frame). Returns arrays (x, z, seed, kind, depth) in the core's frame."""
    tc = cfg["trees"]
    md = dict(em, roads=em["roads"] + core_roads_e, bridges=em["bridges"] + core_bridges_e, buildings=em["buildings"] + core_build_e,
              size_x_m=We, size_z_m=He)
    W, H = int(We), int(He)
    masks = M.keep_off_masks(cfg, md)
    blocked = masks["roads"] | masks["buildings"] | masks["water"] | masks["driveways"]
    near_bld = ndi.distance_transform_edt(~masks["buildings"])
    naip, missing = fetch_naip_ext(cfg, erect, W, H, stage, None if stage == 1 else (prev_at[0] + 150.0, prev_at[1] + 150.0))
    canopy = M.canopy_from_naip(cfg, naip)
    if missing:   # mapgen's rule where the imagery could not be had: woods more than rule_forest_distance_m from any road or building
        far = ndi.distance_transform_edt(~(masks["buildings"] | masks["roads"])) > tc["rule_forest_distance_m"]
        for x0, x1, r0, r1 in missing: canopy[r0:r1, x0:x1] = far[r0:r1, x0:x1]
        log(f"Extension: {len(missing)} imagery pieces could not be had: the rule there")
    depth = ndi.distance_transform_edt(canopy)
    S = tc["cell_m"]
    px0, pz0, px1, pz1 = prev_box
    i_lo, i_hi = int(math.floor(gx0 / S)) - 1, int(math.ceil((gx0 + We) / S)) + 1
    j_lo, j_hi = int(math.floor(gz0 / S)) - 1, int(math.ceil((gz0 + He) / S)) + 1
    ii, jj = np.meshgrid(np.arange(i_lo, i_hi), np.arange(j_lo, j_hi), indexing="ij")
    u = 0.12 + 0.76 * M.hash01(ii, jj, 1)
    v = 0.12 + 0.76 * M.hash01(ii, jj, 2)
    xc, zc = (ii + u) * S, (jj + v) * S                       # the core's frame
    x, z = xc - gx0, zc - gz0                                 # the extension's
    c = np.clip(x.astype(int), 0, W - 1)
    r = np.clip((H - z).astype(int), 0, H - 1)
    ok = (x < W - 2) & (z < H - 2) & (x > 2) & (z > 2)
    ok &= ~((xc >= px0) & (xc <= px1) & (zc >= pz0) & (zc <= pz1))   # the map so far keeps its own trees
    ok &= canopy[r, c] & ~blocked[r, c]
    yard = near_bld[r, c] < tc["yard_distance_m"]
    ok &= ~yard | (M.hash01(ii, jj, 3) < tc["yard_keep"])
    seed = (M.hash01(ii, jj, 4) * 65536).astype(np.uint16)
    sel = ok
    order = np.lexsort((xc[sel], zc[sel]))
    out = (xc[sel][order], zc[sel][order], seed[sel][order], yard[sel][order].astype(np.uint8),
           np.clip(depth[r, c][sel][order] * 10, 0, 255).astype(np.uint8))
    log(f"Extension: trees {len(out[0])} placed outside the core from NAIP (canopy {canopy.mean() * 100:.0f}% of the whole map)")
    return out


def append_trees(t):
    path = os.path.join(M.DATA, "trees.bin")
    with open(path, "rb") as f:
        data = f.read()
    assert data[:4] == b"CRT1"
    n = struct.unpack("<I", data[4:8])[0]
    rec = np.zeros(len(t[0]), dtype=[("x", "<f4"), ("z", "<f4"), ("s", "<u2"), ("k", "u1"), ("d", "u1")])
    rec["x"], rec["z"], rec["s"], rec["k"], rec["d"] = t
    with open(path, "wb") as f:
        f.write(b"CRT1" + struct.pack("<I", n + len(rec)))
        f.write(data[8:])
        f.write(rec.tobytes())
    log(f"Extension: trees.bin {n} core trees + {len(rec)} new = {n + len(rec)}")
    return n   # Round 76: where the new trees start (mapgen_piece.py)


def aeroways_and_trails(cfg, osm, rect, W, H, in_core):
    """Round 61 (B): from the stage's OSM, in its frame: aeroway runways and taxiways (lines, OSM's width or 30 / 15 m) and aprons
    (outlines) anywhere on the map; named trails (highway path / footway / bridleway / track with "Trail" or "Trace" in the name) outside
    the map so far, at their real width (OSM's, or 1.5 m, a track 3 m)."""
    tr = M.Transformer.from_crs(4326, cfg["utm_epsg"], always_xy=True)
    w0, s0 = rect[0], rect[1]
    nodes = {}
    for el in osm["elements"]:
        if el["type"] == "node":
            x, y = tr.transform(el["lon"], el["lat"])
            nodes[el["id"]] = (round(x - w0, 2), round(y - s0, 2))
    inside = lambda p: -1e-6 <= p[0] <= W + 1e-6 and -1e-6 <= p[1] <= H + 1e-6

    def width(t, d):
        try: return float(str(t.get("width", "")).replace("m", "").strip())
        except ValueError: return d
    air, trails = [], []
    for el in osm["elements"]:
        if el["type"] != "way": continue
        t = el.get("tags", {}); pts = [nodes[n] for n in el["nodes"] if n in nodes]
        if len(pts) < 2: continue
        aw, hw, name = t.get("aeroway"), t.get("highway"), t.get("name", "")
        if aw in ("runway", "taxiway"):
            for run, _ in M.clip_runs(pts, list(range(len(pts))), inside):
                if len(run) >= 2: air.append({"kind": aw, "name": name or t.get("ref", ""), "width": width(t, 30.0 if aw == "runway" else 15.0), "points": run, "osm_id": el["id"]})
        elif aw == "apron" and el["nodes"][0] == el["nodes"][-1] and len(pts) >= 4 and all(inside(p) for p in pts):
            ring = pts[:-1]
            air.append({"kind": "apron", "name": name, "outline": ring if M.poly_area(ring) > 0 else ring[::-1], "osm_id": el["id"]})
        elif hw in ("path", "footway", "bridleway", "track") and ("Trail" in name or "Trace" in name):
            for run, _ in M.clip_runs(pts, list(range(len(pts))), lambda p: inside(p) and not in_core(p)):
                if len(run) >= 2 and sum(math.dist(a, b) for a, b in zip(run, run[1:])) > 3.0:
                    trails.append({"name": name, "kind": hw, "width": width(t, 3.0 if hw == "track" else 1.5), "points": run, "osm_id": el["id"]})
    air.sort(key=lambda a: (a["kind"], a["osm_id"])); trails.sort(key=lambda a: (a["osm_id"], a["points"][0]))
    return air, trails
