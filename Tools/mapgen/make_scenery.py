"""Round 66 (Dan, 2026-10-24: "Speaking of edges, how can I handle this on my map especially now I have helicopters"; "And I like the
idea of Atlanta in the distance"): the scenery past the map's edge and the horizon. Not playable: no colliders, roads or traffic.

  1. THE RING: land 9 km wide beyond every edge of the map, from the USGS 3DEP elevation service (30 m cells) and the USGS NLCD 2021
     land cover (30 m): ground coloured by its cover (woods, fields, towns, water), a carpet of far trees where the cover is woods or
     leafy neighborhoods, simple house and building blocks where it is developed. Its inner edge takes the map's own terrain heights
     (Assets/Generated/terrain_full.r16) and blends to the elevation data over 300 m; one row of cells reaches under the map, lowered,
     so no crack shows.
  2. THE HORIZON: a coarse grid 140 km across (300 m cells near, coarser far) from 3DEP, with each point's prominence over the land round
     it (110 m and more, scaled) (the far ridges and mountains: Lost Mountain, Pine Mountain, Sweat Mountain and the like show as silhouettes, the plain melts into
     the haze), and the Atlanta skyline (Downtown, Midtown, Buckhead) with Cumberland and Perimeter Center: generic towers at their real
     places and heights (no names), on the ground there.
  Heights as the map's: datum + (elevation - datum) * vertical_scale (towers' heights scaled alike), and the earth's curve (refraction
  allowed for: R x 7/6) from the map's edge outwards, so the seam stays where the map is.

Writes Assets/Resources/Scenery/edge_scenery.bytes (read at run time by World/EdgeScenery.cs), a picture
Data/screenshots/round66/scenery_plan.png; downloads to raw/scenery/ (ignored). Re-run after a mapgen run that moves the map's edge.

Usage: python Tools/mapgen/make_scenery.py [--refresh]
"""
import json, math, os, struct, sys, urllib.parse, urllib.request
import numpy as np
from scipy import ndimage as ndi

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "raw", "scenery")
OUT = os.path.join(ROOT, "Assets", "Resources", "Scenery", "edge_scenery.bytes")
PIC = os.path.join(ROOT, "Data", "screenshots", "round66", "scenery_plan.png")
UA = "ChastainRush-mapgen/1.0 (personal game project)"
DEM_URL = "https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer/exportImage"
NLCD_URL = "https://www.mrlc.gov/geoserver/mrlc_display/wms"
RING_W = 9000.0          # the ring's width past every edge
RING_CELL = 30.0         # the elevation and cover cells
NEAR_BAND = 1000.0       # within this of the edge: denser trees (seen from the ground at the closures)
FAR_HALF = 70000.0       # the horizon grid's half width round the map's middle
EARTH_R = 6371000.0 * 7.0 / 6.0

# NLCD's legend colours -> class
NLCD = {(71, 107, 160): 11, (209, 222, 248): 12, (221, 201, 201): 21, (216, 147, 130): 22, (237, 0, 0): 23, (170, 0, 0): 24,
        (178, 173, 163): 31, (104, 170, 99): 41, (28, 99, 48): 42, (181, 201, 142): 43, (204, 186, 124): 52, (226, 226, 193): 71,
        (219, 216, 61): 81, (171, 108, 40): 82, (186, 216, 234): 90, (112, 163, 186): 95}
# per class: (trees per 225 m2 in the near band, house chance per 60 m cell, kind) - the cover's look is chosen at run time from the class
TREES = {41: 0.85, 42: 0.85, 43: 0.85, 90: 0.75, 21: 0.50, 22: 0.45, 23: 0.10, 52: 0.25, 71: 0.04, 81: 0.04, 82: 0.02, 95: 0.10, 31: 0.02}
BUILD = {22: (0.55, 0), 23: (0.75, 1), 24: (0.85, 2), 21: (0.12, 0)}

# the skyline: (lat, lon, height m, footprint m, top) - generic towers at real places; top 0 flat, 1 pyramid, 2 spire, 3 stepped, 4 round
TOWERS = [
    # Downtown and Midtown
    (33.7712, -84.3863, 312, 52, 1), (33.7625, -84.3874, 265, 46, 3), (33.7594, -84.3878, 220, 40, 4), (33.7584, -84.3864, 235, 42, 2),
    (33.7576, -84.3865, 212, 40, 3), (33.7866, -84.3836, 250, 46, 1), (33.7878, -84.3848, 210, 44, 3), (33.7889, -84.3829, 200, 40, 2),
    (33.7805, -84.3866, 206, 46, 0), (33.7843, -84.3833, 196, 40, 3), (33.7906, -84.3858, 180, 36, 0), (33.7815, -84.3850, 165, 36, 2),
    (33.7650, -84.3851, 116, 42, 0), (33.7600, -84.3897, 150, 40, 0), (33.7558, -84.3880, 170, 40, 3), (33.7615, -84.3842, 180, 40, 0),
    (33.7678, -84.3870, 140, 36, 0), (33.7752, -84.3856, 120, 36, 0), (33.7928, -84.3960, 130, 42, 0), (33.7550, -84.3905, 120, 50, 0),
    (33.7860, -84.3870, 160, 38, 0), (33.7830, -84.3880, 140, 36, 3), (33.7700, -84.3840, 130, 40, 0), (33.7640, -84.3900, 110, 44, 0),
    # Buckhead
    (33.8467, -84.3667, 175, 40, 2), (33.8429, -84.3738, 175, 36, 3), (33.8478, -84.3627, 150, 40, 0), (33.8440, -84.3680, 140, 40, 0),
    (33.8495, -84.3685, 130, 36, 1), (33.8410, -84.3790, 120, 36, 0), (33.8450, -84.3720, 110, 40, 0), (33.8460, -84.3600, 120, 38, 0),
    # Cumberland / the Galleria
    (33.8820, -84.4660, 120, 40, 0), (33.8790, -84.4640, 100, 36, 3), (33.8850, -84.4700, 95, 40, 0), (33.8760, -84.4680, 80, 46, 0),
    # Perimeter Center (the twin towers)
    (33.9233, -84.3592, 160, 30, 2), (33.9238, -84.3582, 160, 30, 2), (33.9270, -84.3410, 110, 40, 0), (33.9300, -84.3450, 100, 40, 0),
    (33.9220, -84.3380, 90, 40, 0),
]
# lower towers filling round each cluster: (lat, lon, count, spread m, low, high)
FILL = [(33.768, -84.387, 46, 1500, 40, 115), (33.786, -84.385, 30, 900, 45, 120), (33.846, -84.368, 24, 900, 35, 95),
        (33.881, -84.467, 14, 700, 30, 75), (33.925, -84.350, 14, 900, 30, 80)]


def log(m): print("[scenery]", m, flush=True)


def fetch(url, path, refresh, check):
    if os.path.exists(path) and not refresh:
        log(f"using cache {os.path.relpath(path, ROOT)}"); return
    log(f"downloading {os.path.relpath(path, ROOT)} ...")
    data = urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": UA}), timeout=900).read()
    if not check(data): raise SystemExit(f"unexpected response for {path}: {data[:300]!r}")
    os.makedirs(RAW, exist_ok=True)
    with open(path, "wb") as f: f.write(data)


def dem(box, cell, name, epsg, refresh):
    """3DEP bare-earth heights over box (UTM w, s, e, n) at 'cell' metres: rows south first."""
    import rasterio
    w, s, e, n = box; cols, rows = int(round((e - w) / cell)), int(round((n - s) / cell))
    q = urllib.parse.urlencode({"bbox": f"{w},{s},{e},{n}", "bboxSR": epsg, "imageSR": epsg, "size": f"{cols},{rows}", "format": "tiff",
                                "pixelType": "F32", "noData": "-9999", "interpolation": "RSP_BilinearInterpolation", "f": "image"})
    path = os.path.join(RAW, name)
    fetch(DEM_URL + "?" + q, path, refresh, lambda d: d[:2] in (b"II", b"MM"))
    with rasterio.open(path) as src: a = src.read(1).astype(np.float32)[::-1]
    bad = a < -1000
    if bad.any():   # fill holes from the nearest cell
        idx = ndi.distance_transform_edt(bad, return_distances=False, return_indices=True); a = a[tuple(idx)]
    log(f"{name}: {a.shape[1]} x {a.shape[0]} cells of {cell:.0f} m, {a.min():.0f}-{a.max():.0f} m")
    return a


def nlcd(box, cell, name, epsg, refresh):
    from PIL import Image
    w, s, e, n = box; cols, rows = int(round((e - w) / cell)), int(round((n - s) / cell))
    q = urllib.parse.urlencode({"service": "WMS", "version": "1.1.1", "request": "GetMap", "layers": "NLCD_2021_Land_Cover_L48", "styles": "",
                                "srs": f"EPSG:{epsg}", "bbox": f"{w},{s},{e},{n}", "width": cols, "height": rows, "format": "image/png"})
    path = os.path.join(RAW, name)
    fetch(NLCD_URL + "?" + q, path, refresh, lambda d: d[:4] == b"\x89PNG")
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.int32)[::-1]
    pal = np.array(list(NLCD.keys())); cls = np.array(list(NLCD.values()), dtype=np.uint8)
    flat = rgb.reshape(-1, 3); best = np.zeros(len(flat), dtype=np.int64); bd = np.full(len(flat), 1 << 30)
    for i, p in enumerate(pal):
        d = ((flat - p) ** 2).sum(1); m = d < bd; bd[m] = d[m]; best[m] = i
    out = cls[best].reshape(rgb.shape[:2]); out[bd.reshape(rgb.shape[:2]) > 900] = 0
    vals, cnt = np.unique(out, return_counts=True)
    log(f"{name}: {cols} x {rows}, cover " + ", ".join(f"{v}:{c * 100 / out.size:.0f}%" for v, c in zip(vals, cnt) if c * 100 / out.size >= 1))
    return out


def bilinear(a, fx, fz):
    fx = np.clip(fx, 0, a.shape[1] - 1.001); fz = np.clip(fz, 0, a.shape[0] - 1.001)
    j, i = fx.astype(int), fz.astype(int); tx, tz = fx - j, fz - i
    return a[i, j] * (1 - tx) * (1 - tz) + a[i, j + 1] * tx * (1 - tz) + a[i + 1, j] * (1 - tx) * tz + a[i + 1, j + 1] * tx * tz


def outside(x, z, R):
    x0, z0, x1, z1 = R
    dx = np.maximum(0, np.maximum(x0 - x, x - x1)); dz = np.maximum(0, np.maximum(z0 - z, z - z1))
    return np.hypot(dx, dz)


def axis_ring(a0, a1, near, far):
    """Grid lines: 'far' spacing out to 2 km from the edge, 'near' within it, ~60 m across the map (only its edge rows are used)."""
    def seg(a, b, step):
        n = max(1, int(math.ceil(abs(b - a) / step))); return list(np.linspace(a, b, n + 1))
    xs = seg(a0 - RING_W, a0 - 2000, far)[:-1] + seg(a0 - 2000, a0, near)[:-1] + seg(a0, a1, 60.0)[:-1] + seg(a1, a1 + 2000, near)[:-1] + seg(a1 + 2000, a1 + RING_W, far)
    return np.array(xs, dtype=np.float64)


def axis_far(c, half):
    xs = [0.0]; step = 300.0
    while xs[-1] < half:
        d = xs[-1]; step = 300.0 if d < 35000 else 300.0 + (d - 35000) / 35000 * 1200.0; xs.append(d + step)
    a = np.array(xs); return np.concatenate([c - a[:0:-1], c + a])


def main():
    refresh = "--refresh" in sys.argv
    mp = json.load(open(os.path.join(ROOT, "Data", "map.json")))
    tj = json.load(open(os.path.join(ROOT, "Assets", "Generated", "terrain_full.json")))
    oE, oN = mp["origin_utm"]; R = tuple(mp["extent_m"]); x0, z0, x1, z1 = R
    vs, datum, epsg = tj["vertical_scale"], tj["vertical_datum_m"], 26916
    scale = lambda e: datum + (e - datum) * vs
    log(f"map extent x {x0:.1f}..{x1:.1f}, z {z0:.1f}..{z1:.1f}; vertical scale {vs} about {datum:.2f} m")

    # ---- the map's own edge heights (the terrain the ring meets)
    gx, gz = tj["grid_x"], tj["grid_z"]; lo, hi = tj["min_height"], tj["max_height"]
    r16 = np.memmap(os.path.join(ROOT, "Assets", "Generated", "terrain_full.r16"), dtype="<u2", mode="r", shape=(gz, gx))
    sx, sz = tj["step_x_m"], tj["step_z_m"]; tox, toz = tj["origin_x_m"], tj["origin_z_m"]
    edge_s, edge_n = r16[0, :].astype(np.float32), r16[gz - 1, :].astype(np.float32)
    edge_w, edge_e = r16[:, 0].astype(np.float32), r16[:, gx - 1].astype(np.float32)
    toh = lambda v: lo + v / 65535.0 * (hi - lo)
    edge_s, edge_n, edge_w, edge_e = map(toh, (edge_s, edge_n, edge_w, edge_e))

    def edge_height(x, z):
        """The map's terrain height at the nearest point of its edge."""
        cx = np.clip(x, x0, x1); cz = np.clip(z, z0, z1)
        dW, dE, dS, dN = cx - x0, x1 - cx, cz - z0, z1 - cz
        # inside: the nearest side; outside: the clamped point lies on a side already
        side = np.argmin(np.stack([dW, dE, dS, dN]), axis=0)
        fi = (cx - tox) / sx; fk = (cz - toz) / sz
        hs = np.interp(fi, np.arange(gx), edge_s); hn = np.interp(fi, np.arange(gx), edge_n)
        hw = np.interp(fk, np.arange(gz), edge_w); he = np.interp(fk, np.arange(gz), edge_e)
        return np.choose(side, [hw, he, hs, hn])

    # ---- downloads
    rbox = (oE + x0 - RING_W - 300, oN + z0 - RING_W - 300, oE + x1 + RING_W + 300, oN + z1 + RING_W + 300)
    rbox = tuple(float(round(v / RING_CELL) * RING_CELL) for v in rbox)
    ring_dem = dem(rbox, RING_CELL, "dem_ring_30m.tif", epsg, refresh)
    ring_cov = nlcd(rbox, RING_CELL, "nlcd_ring_30m.png", epsg, refresh)
    if ring_cov.shape != ring_dem.shape: raise SystemExit(f"cover {ring_cov.shape} and heights {ring_dem.shape} differ")
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
    fbox = (oE + cx - FAR_HALF - 1000, oN + cz - FAR_HALF - 1000, oE + cx + FAR_HALF + 1000, oN + cz + FAR_HALF + 1000)
    fbox = tuple(float(round(v / 320) * 320) for v in fbox)
    far_dem = dem(fbox, 320.0, "dem_far_320m.tif", epsg, refresh)
    rw0, rs0 = rbox[0] - oE, rbox[1] - oN; fw0, fs0 = fbox[0] - oE, fbox[1] - oN
    ring_h = lambda x, z: scale(bilinear(ring_dem, (x - rw0) / RING_CELL - .5, (z - rs0) / RING_CELL - .5))
    far_hraw = scale(far_dem)
    curve = lambda d: d * d / (2 * EARTH_R)
    rng = np.random.default_rng(66)

    # ---- the ring grid
    xs, zs = axis_ring(x0, x1, RING_CELL, 75.0), axis_ring(z0, z1, RING_CELL, 75.0)
    X, Z = np.meshgrid(xs, zs)
    out_d = outside(X, Z, R)
    h = ring_h(X, Z)
    w = np.clip(out_d / 300.0, 0, 1); w = w * w * (3 - 2 * w)
    eh = edge_height(X, Z)
    h = eh * (1 - w) + h * w - curve(out_d)
    inside = (X > x0 + .5) & (X < x1 - .5) & (Z > z0 + .5) & (Z < z1 - .5)
    h[inside] = eh[inside] - 4.0          # the row under the map: lowered out of sight
    ci = np.clip(((X - rw0) / RING_CELL).astype(int), 0, ring_cov.shape[1] - 1); ck = np.clip(((Z - rs0) / RING_CELL).astype(int), 0, ring_cov.shape[0] - 1)
    cov = ring_cov[ck, ci]
    log(f"ring grid {len(xs)} x {len(zs)} = {X.size} points ({(~inside).sum()} outside the map)")

    # ---- trees and buildings over the ring's cover cells
    ny, nx = ring_cov.shape
    PX, PZ = np.meshgrid(rw0 + (np.arange(nx) + .5) * RING_CELL, rs0 + (np.arange(ny) + .5) * RING_CELL)
    pd = outside(PX, PZ, R); usable = (pd > 6) & (pd < RING_W)
    builds = []
    # buildings first (one per 60 m block where developed), trees kept off them
    taken = np.zeros_like(usable)
    for k in range(0, ny - 1, 2):
        for i in range(0, nx - 1, 2):
            c = int(np.bincount(ring_cov[k:k + 2, i:i + 2].ravel(), minlength=100).argmax())
            if c not in BUILD or not usable[k, i]: continue
            p, kind = BUILD[c]
            if rng.random() > p: continue
            bx = PX[k, i] + RING_CELL * .5 + rng.uniform(-14, 14); bz = PZ[k, i] + RING_CELL * .5 + rng.uniform(-14, 14)
            if outside(np.array([bx]), np.array([bz]), R)[0] < 15: continue
            if kind == 0: bw, bd_, bh = rng.uniform(10, 16), rng.uniform(8, 13), rng.uniform(5.5, 7.5) * vs
            elif kind == 1: bw, bd_, bh = rng.uniform(16, 34), rng.uniform(14, 28), rng.uniform(6, 12) * vs
            else: bw, bd_, bh = rng.uniform(25, 55), rng.uniform(20, 45), rng.uniform(9, 22) * vs
            yaw = rng.uniform(0, 1)
            corners = [(bx + sx_ * bw * .5, bz + sz_ * bd_ * .5) for sx_ in (-1, 1) for sz_ in (-1, 1)]
            by = min(float(ring_h(np.array([a]), np.array([b]))[0]) for a, b in corners) - curve(outside(np.array([bx]), np.array([bz]), R)[0])
            builds.append((bx, bz, by, bw, bd_, bh, yaw, kind, int(rng.integers(0, 8))))
            taken[k:k + 2, i:i + 2] = True
    trees = []
    dens = np.zeros(ring_cov.shape, dtype=np.float32)
    for c, t in TREES.items(): dens[ring_cov == c] = t
    dens[~usable] = 0; dens[taken] *= 0.25
    near = pd < NEAR_BAND
    exp_near = dens * (RING_CELL * RING_CELL / 225.0); exp_far = dens * (RING_CELL * RING_CELL / 900.0)   # 15 m and 30 m spacing
    cnt = np.where(near, rng.poisson(exp_near), rng.poisson(exp_far))
    kk, ii = np.nonzero(cnt)
    for k, i in zip(kk, ii):
        c = ring_cov[k, i]; n = cnt[k, i]
        for _ in range(n):
            tx = PX[k, i] + rng.uniform(-15, 15); tz = PZ[k, i] + rng.uniform(-15, 15)
            fc = 1 if c == 42 or (c == 43 and rng.random() < .5) or (c in (21, 22) and rng.random() < .3) else (2 if rng.random() < .04 else 0)
            f = rng.uniform(.75, 1.15)
            trees.append((tx, tz, fc, f, 1 if near[k, i] else 0))
    trees = np.array(trees, dtype=np.float64) if trees else np.zeros((0, 5))
    if len(trees):
        od = outside(trees[:, 0], trees[:, 1], R); keep = od > 4; trees = trees[keep]; od = od[keep]
        ty = ring_h(trees[:, 0], trees[:, 1]) - curve(od)
    else: ty = np.zeros(0)
    log(f"ring: {len(trees)} trees ({int(trees[:, 4].sum()) if len(trees) else 0} in the near band), {len(builds)} buildings")

    # ---- the horizon grid
    fxs, fzs = axis_far(cx, FAR_HALF), axis_far(cz, FAR_HALF)
    FX, FZ = np.meshgrid(fxs, fzs)
    win = lambda m: max(3, int(round(m / 320.0)) | 1)
    low8 = ndi.minimum_filter(ndi.uniform_filter(far_hraw, 3), size=win(8000))
    low3 = ndi.minimum_filter(far_hraw, size=win(3000))
    ffx, ffz = (FX - fw0) / 320.0 - .5, (FZ - fs0) / 320.0 - .5
    fd = outside(FX, FZ, R)
    fh = bilinear(far_hraw, ffx, ffz) - curve(fd)
    prom = np.clip((bilinear(far_hraw, ffx, ffz) - bilinear(low8, ffx, ffz) - 110.0) / 150.0, 0, 1)   # only the mountains (the rolling land stays haze)
    flow = bilinear(low3, ffx, ffz)
    log(f"horizon grid {len(fxs)} x {len(fzs)} = {FX.size} points; prominence > 0.5 at {(prom > .5).mean() * 100:.1f}%")

    # ---- the towers
    from pyproj import Transformer
    tr = Transformer.from_crs(4326, epsg, always_xy=True)
    towers = []
    def place(lat, lon, hgt, fp, top):
        e, n = tr.transform(lon, lat); x, z = e - oE, n - oN
        base = float(bilinear(far_hraw, np.array([(x - fw0) / 320.0 - .5]), np.array([(z - fs0) / 320.0 - .5]))[0]) - curve(float(outside(np.array([x]), np.array([z]), R)[0]))
        towers.append((x, z, base, fp, fp * rng.uniform(.75, 1.0), hgt * vs, rng.uniform(0, 1), top))
    for t in TOWERS: place(*t)
    for lat, lon, n, spread, a, b in FILL:
        for _ in range(n):
            ang = rng.uniform(0, 2 * math.pi); r = spread * math.sqrt(rng.uniform(0, 1))
            place(lat + r * math.cos(ang) / 111000.0, lon + r * math.sin(ang) / (111000.0 * math.cos(math.radians(lat))), rng.uniform(a, b), rng.uniform(22, 40), int(rng.integers(0, 4)) if rng.random() < .3 else 0)
    d0 = math.hypot(towers[0][0] - (-1200), towers[0][1] - (-5800))
    log(f"{len(towers)} towers; the tallest at ({towers[0][0]:.0f}, {towers[0][1]:.0f}), {d0 / 1000:.1f} km from the summit, base {towers[0][2]:.0f} m")

    # ---- write
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    i16 = lambda a: np.clip(np.round(a), -32768, 32767).astype("<i2")
    with open(OUT, "wb") as f:
        f.write(b"CRSC"); f.write(struct.pack("<i", 1))
        f.write(struct.pack("<5f", x0, z0, x1, z1, vs))
        f.write(struct.pack("<ii", len(xs), len(zs))); f.write(xs.astype("<f4").tobytes()); f.write(zs.astype("<f4").tobytes())
        f.write(i16(h * 10).tobytes()); f.write(cov.astype(np.uint8).tobytes())
        f.write(struct.pack("<ii", len(fxs), len(fzs))); f.write(fxs.astype("<f4").tobytes()); f.write(fzs.astype("<f4").tobytes())
        f.write(i16(fh * 10).tobytes()); f.write(np.round(prom * 255).astype(np.uint8).tobytes()); f.write(i16(flow * 10).tobytes())
        f.write(struct.pack("<i", len(trees)))
        if len(trees):
            rec = np.zeros(len(trees), dtype=[("x", "<i2"), ("z", "<i2"), ("y", "<i2"), ("c", "u1"), ("f", "u1"), ("near", "u1"), ("pad", "u1")])
            rec["x"] = i16(trees[:, 0]); rec["z"] = i16(trees[:, 1]); rec["y"] = i16(ty * 10); rec["c"] = trees[:, 2]
            rec["f"] = np.clip(np.round(trees[:, 3] * 100), 1, 255); rec["near"] = trees[:, 4]; f.write(rec.tobytes())
        f.write(struct.pack("<i", len(builds)))
        for b in builds:
            f.write(struct.pack("<3f4B2B", b[0], b[1], b[2], int(min(255, b[3] * 4)), int(min(255, b[4] * 4)), int(min(255, b[5] * 4)), int(b[6] * 255), b[7], b[8]))
        f.write(struct.pack("<i", len(towers)))
        for t in towers: f.write(struct.pack("<7fi", *t))
    log(f"wrote {os.path.relpath(OUT, ROOT)} ({os.path.getsize(OUT) / 1e6:.1f} MB)")

    # ---- a picture for the report
    try:
        from PIL import Image, ImageDraw
        s = 6.0; W = int((x1 - x0 + 2 * RING_W) / s * 1); Hh = int((z1 - z0 + 2 * RING_W) / s)
        colors = {11: (70, 110, 170), 21: (190, 200, 160), 22: (200, 170, 140), 23: (200, 120, 110), 24: (150, 90, 90), 41: (70, 120, 60), 42: (40, 90, 50),
                  43: (80, 125, 70), 90: (90, 130, 110), 81: (180, 190, 110), 71: (170, 180, 130), 82: (170, 140, 90), 52: (150, 150, 100)}
        img = Image.new("RGB", (int(len(xs)), int(len(zs))))
        px = np.zeros((len(zs), len(xs), 3), dtype=np.uint8)
        for c, col in colors.items(): px[cov == c] = col
        shade = np.clip((h - h[~inside].min()) / max(1, np.ptp(h[~inside])), 0, 1)
        px = (px * (0.6 + 0.4 * shade[..., None])).astype(np.uint8); px[inside] = (255, 255, 255)
        img = Image.fromarray(px[::-1]); os.makedirs(os.path.dirname(PIC), exist_ok=True); img.save(PIC)
        log(f"picture {os.path.relpath(PIC, ROOT)}")
    except Exception as ex: log(f"no picture ({ex})")


if __name__ == "__main__":
    main()
