"""Round 75 (B, Dan: street names on the map, "highways by their shields / numbers (I-75, I-575, US 41)"): the route numbers of map.json's roads.

map.json keeps no OSM 'ref'; this reads the raw OSM downloads (raw/osm_*.json, git-ignored) and writes Data/road_refs.json (committed):
{"refs": [{"id": <osm_id>, "r": "I 75"}, ...]} for every road of map.json whose way has a ref. Re-run after mapgen; the map build reads it (MapBuilder.MapPicture.cs).
Usage: python Tools/mapgen/road_refs.py"""
import json, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
M = json.load(open(os.path.join(ROOT, "Data/map.json"), encoding="utf-8"))
ids = {r["osm_id"] for r in M["roads"]}
refs = {}
for f in sorted(os.listdir(os.path.join(ROOT, "raw"))):
    if not (f.startswith("osm") and f.endswith(".json")): continue
    for e in json.load(open(os.path.join(ROOT, "raw", f), encoding="utf-8")).get("elements", []):
        if e.get("type") == "way" and e.get("id") in ids and e.get("tags", {}).get("ref"): refs[str(e["id"])] = e["tags"]["ref"]
out = os.path.join(ROOT, "Data/road_refs.json")
json.dump({"refs": [{"id": int(k), "r": v} for k, v in sorted(refs.items(), key=lambda kv: int(kv[0]))]}, open(out, "w", encoding="utf-8"), separators=(",", ":"))
print(f"{len(refs)} of {len(ids)} roads have a route number; wrote {os.path.relpath(out, ROOT)}")
