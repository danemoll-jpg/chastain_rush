"""Round 76 (A, Dan, 2026-10-27: "there is just a little more area that I want to add ... prefer if we could just add this one little
section"): THE MAP EXTENSION, stage 3, Marietta Square. Unlike stages 1 and 2 (whole rows and columns of tiles west and south of the map so
far) this is ONE PIECE: a block of whole tiles of the same grid below the map's south edge, only under the columns that take in downtown
Marietta (config "extension3": tile_cols [first, last] of the existing grid, tiles_south). The map is no longer a rectangle.

Everything that exists keeps its place and its data, as in Round 59 / 61: map.json's lists keep their order, the new pieces are appended
after them (map.json "extension3": first, bounds, piece); trees appended to trees.bin; terrain_full.r16 / .json are NOT touched (the python
scripts that sample the ground read them as before): the piece's ground is its own file, Assets/Generated/terrain_piece3.r16 / .json, on the
same grid (its tiles continue the existing columns downward, the same sample step and the same height range, so its top row is the full
grid's bottom row sample for sample). map.json "pieces": [[x0, z0, x1, z1]] (the core's frame), the land outside extent_m.

Run by mapgen.py after stage 2 (in memory), or alone on the existing outputs: python Tools/mapgen/mapgen_piece.py [--refresh-osm]
(alone it first takes out what an earlier run of it appended, so it can be run again)."""
import argparse
import json
import math
import os
import struct
import sys

import numpy as np
import rasterio

import mapgen as M
import mapgen_extend as E

log = E.log


def piece_box(cfg, meta):
    ext = cfg["extension3"]
    c0, c1 = ext["tile_cols"]; rows = int(ext["tiles_south"])
    tw, td = meta["tile_w_m"], meta["tile_d_m"]
    px0 = meta["origin_x_m"] + c0 * tw; px1 = meta["origin_x_m"] + (c1 + 1) * tw
    pz1 = meta["origin_z_m"]; pz0 = pz1 - rows * td
    return c0, c1, rows, (px0, pz0, px1, pz1)


def extend_piece(cfg, core_rect, mapdata, full_h, meta, refresh):
    """'core_rect' the core's UTM rectangle (its south-west corner is the frame's origin); 'full_h' the whole grid so far (rows from the south,
    metres) or None to read Assets/Generated/terrain_full.r16; 'meta' terrain_full.json's."""
    ext = cfg["extension3"]; stage = int(ext.get("stage", 3))
    cells = cfg["terrain"]["tile_resolution"] - 1
    c0, c1, rows, (px0, pz0, px1, pz1) = piece_box(cfg, meta)
    ncol = c1 - c0 + 1
    We, He = px1 - px0, pz1 - pz0
    cw0, cs0 = core_rect[0], core_rect[1]
    erect = (cw0 + px0, cs0 + pz0, cw0 + px1, cs0 + pz1)
    tr = M.Transformer.from_crs(cfg["utm_epsg"], 4326, always_xy=True)
    sw, ne = tr.transform(erect[0], erect[1]), tr.transform(erect[2], erect[3])
    log(f"Piece (stage {stage}, {ext.get('name', '')}): tile columns {c0} to {c1}, {rows} rows below the south edge: x {px0:.2f} to {px1:.2f}, "
        f"z {pz0:.2f} to {pz1:.2f} ({We:.0f} x {He:.0f} m); bounds south {sw[1]:.5f}, west {sw[0]:.5f}, north {ne[1]:.5f}, east {ne[0]:.5f}")

    # ---- the existing grid's heights (metres): its bottom rows over the piece's columns (the seam, and the roads that meet it)
    sx, sz = meta["step_x_m"], meta["step_z_m"]
    gx_n, gz_n = ncol * cells + 1, rows * cells + 1
    lo, hi = meta["min_height"], meta["max_height"]
    band = 400   # rows of the existing grid kept (about 250 m north of the seam)
    j_full0 = c0 * cells
    if full_h is None:
        mm = np.memmap(os.path.join(M.GENERATED, "terrain_full.r16"), dtype="<u2", mode="r", shape=(meta["grid_z"], meta["grid_x"]))
        north = (lo + mm[:band, j_full0:j_full0 + gx_n].astype(np.float32) / 65535.0 * (hi - lo)).astype(np.float32)
        del mm
    else:
        north = np.asarray(full_h[:band, j_full0:j_full0 + gx_n], dtype=np.float32)

    # ---- OSM: the piece only (the map so far keeps its own)
    osm = E.fetch_osm_ext(cfg, refresh, erect, stage)
    em = M.process_osm(cfg, osm, erect, owner_marker=False, spawn=False)
    inside = lambda p: -1e-6 <= p[0] <= We + 1e-6 and -1e-6 <= p[1] <= He + 1e-6
    centroid = lambda r: (sum(p[0] for p in r) / len(r), sum(p[1] for p in r) / len(r))
    roads = []
    for r in em["roads"]:
        for run, _ in M.clip_runs(r["points"], list(range(len(r["points"]))), inside):
            if len(run) >= 2 and sum(math.dist(a, b) for a, b in zip(run, run[1:])) > 0.5:
                roads.append(dict(r, points=run))
    em["roads"] = roads
    em["buildings"] = [b for b in em["buildings"] if inside(centroid(b["footprint"]))]
    em["water"] = [w for w in em["water"] if all(inside(p) for p in w["outline"])]
    em["woods"] = [w for w in em["woods"] if all(inside(p) for p in w["outline"])]
    em["parking"] = [a for a in em["parking"] if all(inside(p) for p in a["outline"])]
    for key in ("streams", "driveways"):
        out = []
        for st in em[key]:
            for run, _ in M.clip_runs(st["points"], list(range(len(st["points"]))), inside):
                if len(run) >= 2: out.append(dict(st, points=run))
        em[key] = out
    air, trails = E.aeroways_and_trails(cfg, osm, erect, We, He, lambda p: False)
    log(f"Piece: OSM {len(em['roads'])} road pieces, {len(em['buildings'])} buildings, {len(em['water'])} water areas, {len(em['streams'])} streams, "
        f"{len(em['woods'])} woods, {len(em['driveways'])} OSM driveways, {len(em['parking'])} parking lots, {len(trails)} trail pieces")

    # the map's things near the seam, in the piece's frame
    def sh(pts, dx, dz): return [(round(p[0] + dx, 2), round(p[1] + dz, 2)) for p in pts]
    near = lambda pts: any(px0 - 120 <= p[0] <= px1 + 120 and pz1 - 5 <= p[1] <= pz1 + 200 for p in pts)
    old_roads = [dict(r, points=sh(r["points"], -px0, -pz0)) for r in mapdata["roads"] if near(r["points"])]
    old_bridges = [dict(b, points=sh(b["points"], -px0, -pz0)) for b in mapdata["bridges"] if near(b["points"])]
    old_build = [dict(b, footprint=sh(b["footprint"], -px0, -pz0)) for b in mapdata["buildings"] if near(b["footprint"])]

    # ---- the DEM and the piece's grid (the same sample step as the grid above it)
    drect = (math.floor(erect[0]), math.floor(erect[1]), math.ceil(erect[2]), math.ceil(erect[3]))
    with rasterio.open(E.build_heightmap_ext(cfg, drect, stage)) as src:
        dem = src.read(1)[::-1].astype(np.float32)
    fx0, fz0 = erect[0] - drect[0], erect[1] - drect[1]

    def axis(n, step, off, size):
        p = np.clip(np.arange(n) * step + off - 0.5, 0, size - 1.001)
        i0 = p.astype(int)
        return i0, (p - i0).astype(np.float32)
    j0, fx = axis(gx_n, sx, fx0, dem.shape[1])
    i0, fz = axis(gz_n, sz, fz0, dem.shape[0])
    rws = dem[:, j0] * (1 - fx) + dem[:, j0 + 1] * fx
    h = rws[i0, :] * (1 - fz)[:, None] + rws[i0 + 1, :] * fz[:, None]
    del rws, dem
    k, datum = meta["vertical_scale"], meta["vertical_datum_m"]
    h = (datum + (h - datum) * k).astype(np.float32)
    raw = h.copy()

    def sample(x, z):   # the piece's raw ground; north of the seam the map's built ground (the roads that meet it are levelled there)
        x = np.asarray(x, dtype=np.float64); z = np.asarray(z, dtype=np.float64)
        gx = np.clip(x / sx, 0, gx_n - 1.001); j = gx.astype(int); fx_ = gx - j
        gzp = np.clip(z / sz, 0, gz_n - 1.001); i = gzp.astype(int); fz_ = gzp - i
        a = (raw[i, j] * (1 - fx_) * (1 - fz_) + raw[i, j + 1] * fx_ * (1 - fz_) + raw[i + 1, j] * (1 - fx_) * fz_ + raw[i + 1, j + 1] * fx_ * fz_)
        gzn = np.clip((z - He) / sz, 0, band - 1.001); i = gzn.astype(int); fz_ = gzn - i
        b = (north[i, j] * (1 - fx_) * (1 - fz_) + north[i, j + 1] * fx_ * (1 - fz_) + north[i + 1, j] * (1 - fx_) * fz_ + north[i + 1, j + 1] * fx_ * fz_)
        return np.where(z > He, b, a).astype(np.float64)

    # ---- bridges, then the levelling (the map's roads near the seam too, so a road that crosses it is levelled alike on both sides)
    em["bridges"] = []
    at_grade, _ = M.plan_bridges(cfg, em, sample)
    log(f"Piece: bridges {len(em['bridges'])} built, {len(at_grade)} crossings left at grade")
    E.join_bridges(old_bridges, em["bridges"])
    h = M.flatten_roads(cfg, h, sample, em["roads"] + old_roads, sx, sz, gx_n, gz_n)

    # ---- the seam: the piece's top row is the grid's bottom row; the samples within seam_m eased onto it
    seam = float(ext.get("seam_m", 40.0))
    d_top = north[0, :] - h[-1, :]
    Z = np.arange(gz_n) * sz
    wz = M.smoothstep(1 - np.clip((He - Z) / seam, 0, 1))
    h = (h + d_top[None, :] * wz[:, None]).astype(np.float32)
    h[-1, :] = north[0, :]
    log(f"Piece: the seam eased over {seam:g} m (the most a sample moved: {np.abs(d_top).max():.2f} m)")
    if h.min() < lo or h.max() > hi:
        raise SystemExit(f"Piece: heights {h.min():.1f}-{h.max():.1f} m fall outside the map's range {lo:.1f}-{hi:.1f} m")
    norm = np.round((h - lo) / (hi - lo) * 65535).astype("<u2")
    norm.tofile(os.path.join(M.GENERATED, f"terrain_piece{stage}.r16"))
    pmeta = {"grid_x": gx_n, "grid_z": gz_n, "tiles_x": ncol, "tiles_z": rows, "tile_resolution": cells + 1, "tile_w_m": meta["tile_w_m"],
             "tile_d_m": meta["tile_d_m"], "step_x_m": sx, "step_z_m": sz, "min_height": lo, "max_height": hi, "origin_x_m": px0, "origin_z_m": pz0,
             "full_col0": c0, "vertical_scale": k, "vertical_datum_m": datum}
    with open(os.path.join(M.GENERATED, f"terrain_piece{stage}.json"), "w", encoding="utf-8") as f:
        json.dump(pmeta, f, indent=1)
    log(f"Piece: terrain {gx_n} x {gz_n} samples = {ncol} x {rows} tiles, heights {h.min():.2f}-{h.max():.2f} m -> Assets/Generated/terrain_piece{stage}.r16")

    # ---- driveways and trees
    M.plan_driveways(cfg, dict(em, roads=em["roads"] + old_roads, size_x_m=We, size_z_m=He))
    keep_off = [{"points": t["points"], "width": t["width"] + 1.0} for t in trails]
    ext_box = tuple(mapdata["extent_m"])
    new_trees = E.plan_trees_ext(cfg, em, old_roads + keep_off, old_bridges, old_build, erect, We, He, px0, pz0, ext_box, stage, (1e9, 1e9))

    # ---- into the core's frame, appended after what exists
    for key, fld in (("roads", "points"), ("bridges", "points"), ("buildings", "footprint"), ("water", "outline"), ("woods", "outline"),
                     ("parking", "outline"), ("streams", "points"), ("driveways", "points")):
        for it in em[key]: it[fld] = sh(it[fld], px0, pz0)
    for t in trails: t["points"] = sh(t["points"], px0, pz0)
    n0 = {key: len(mapdata[key]) for key in ("roads", "bridges", "buildings", "water", "streams", "woods", "driveways", "parking")}
    n_first = dict(n0, streams=sum(1 for s_ in mapdata["streams"] if not s_.get("tunnel")), trails=len(mapdata.get("trails", [])))
    for key in n0: mapdata[key] = mapdata[key] + em[key]
    mapdata["trails"] = mapdata.get("trails", []) + trails
    box = [round(px0, 3), round(pz0, 3), round(px1, 3), round(pz1, 3)]
    mapdata["pieces"] = box   # flat: x0, z0, x1, z1 per piece (Unity's JsonUtility reads no nested arrays)
    n_trees = E.append_trees(new_trees)
    mapdata[f"extension{stage}"] = {"stage": stage, "name": ext.get("name", ""), "first": n_first, "first_trees": n_trees, "piece": box,
                                    "bounds": {"south": round(sw[1], 6), "west": round(sw[0], 6), "north": round(ne[1], 6), "east": round(ne[0], 6)}}
    log(f"Piece: appended {', '.join(f'{len(mapdata[key]) - n0[key]} {key}' for key in n0)}, {len(trails)} trails; {len(new_trees[0])} trees")


def unflat(mapdata):
    """map.json as written (flat point lists) back to mapgen's lists of points."""
    pair = lambda f: [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
    for key, fld in (("roads", "points"), ("bridges", "points"), ("buildings", "footprint"), ("water", "outline"), ("streams", "points"),
                     ("woods", "outline"), ("driveways", "points"), ("parking", "outline"), ("trails", "points")):
        for it in mapdata.get(key, []): it[fld] = pair(it[fld])
    for a in mapdata.get("aeroways", []):
        for fld in ("points", "outline"):
            if fld in a: a[fld] = pair(a[fld])
    for s_ in mapdata["streams"]: s_.setdefault("tunnel", False)
    for r in mapdata["roads"]: r.setdefault("tunnel", False)
    return mapdata


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--refresh-osm", action="store_true")
    args = ap.parse_args()
    with open(os.path.join(os.path.dirname(__file__), "config.json"), encoding="utf-8") as f:
        cfg = json.load(f)
    with open(os.path.join(M.DATA, "map.json"), encoding="utf-8") as f:
        mapdata = unflat(json.load(f))
    with open(os.path.join(M.GENERATED, "terrain_full.json"), encoding="utf-8") as f:
        meta = json.load(f)
    old = mapdata.pop("extension3", None)
    if old:   # an earlier run's piece taken out first
        for key, n in old["first"].items():
            if key in mapdata and key != "streams": mapdata[key] = mapdata[key][:n]
        mapdata["streams"] = mapdata["streams"][:old["first"]["streams"]]
        mapdata.pop("pieces", None)
        path = os.path.join(M.DATA, "trees.bin")
        with open(path, "rb") as f: data = f.read()
        rec = 12
        with open(path, "wb") as f: f.write(b"CRT1" + struct.pack("<I", old["first_trees"]) + data[8:8 + old["first_trees"] * rec])
        log(f"Piece: the earlier run's piece taken out (trees back to {old['first_trees']})")
    rect, _ = M.utm_rect(cfg)
    extend_piece(cfg, rect, mapdata, None, meta, args.refresh_osm)
    M.write_map(mapdata)


if __name__ == "__main__":
    sys.exit(main())
