"""Round 78 (G, Round 77's open item: stage 3's listed bumps, Data/screenshots/round77/stage3_bumps.txt: 23 steps of 10 cm or more, 53 humps of 30 cm or more
outside the I-575 zone): every such spot of a BumpScan report whose ray hit the ground itself (not a building or prop on the road) becomes a smooth spot,
spots within 12 m merged, written to Data/tracks/round78_bumps.json; MapBuilder.Round72.cs blurs the ground there after the levelling (Round 55's blur),
with Round 72's junctions. Usage: python Tools/mapgen/bump_spots.py <bump report> [out]"""
import json
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Data", "screenshots", "round77", "stage3_bumps.txt")
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "Data", "tracks", "round78_bumps.json")
    rows = []
    started = False
    for line in open(src, encoding="utf-8"):
        if line.startswith("kind\tx\tz"):
            started = True; continue
        if not started: continue
        f = line.rstrip("\n").split("\t")
        if len(f) < 10: continue
        kind, x, z, val, road, hit, feat = f[0], float(f[1]), float(f[2]), float(f[4]), f[6], f[8], f[9]
        if "i575-zone" in feat or not hit.startswith("Terrain"): continue
        if (kind == "STEP" and abs(val) >= 0.10) or (kind == "HUMP" and abs(val) >= 0.30):
            rows.append((x, z, kind, val, road))
    spots = []
    for x, z, kind, val, road in sorted(rows, key=lambda r: -abs(r[3])):
        if any(math.dist((x, z), s["at"]) < 12 for s in spots): continue
        spots.append({"id": f"{kind.lower()} {val:+.2f} m, {road or 'unnamed road'}", "at": [round(x, 1), round(z, 1)], "r": 9.0, "fade": 6.0, "sigma": 2.5})
    json.dump({"note": "Round 78 (G): stage 3's serious bumps (STEP >= 10 cm, HUMP >= 30 cm, outside the I-575 zone, ground hits) from " + os.path.relpath(src, ROOT).replace("\\", "/") +
               " (Tools/mapgen/bump_spots.py), blurred after the levelling by MapBuilder.Round72.cs. Map meters.", "smooth_spots": spots}, open(out, "w", encoding="utf-8"), indent=1)
    print(f"{len(rows)} serious rows, {len(spots)} spots -> {out}")


if __name__ == "__main__":
    main()
