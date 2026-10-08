# Project Context & Rules — Chastain Rush

Sequel to Woodstock Rush (the `Racer` repo). Same kind of game, but the map is
generated from real-world data for the Chastain Glen area of Marietta, GA.

## Tech Stack
- Unity 6000.6.1f1 (same as Racer, confirmed from Racer's ProjectVersion.txt),
  URP 17.6.0, Input System 1.20.0 (Input System only, no old Input Manager).
- Map data: USGS 3DEP 1 m elevation (GeoTIFF, UTM zone 16N, NAD83, metres) and
  OpenStreetMap (roads, building footprints, water, woods). Details and the
  tile URL are in `TODO.md` under Technical Notes.
- Repo: `C:\Users\danmo\chastain_rush`. Racer is a separate repo and is the
  source for reused systems.

## Code Style & Architecture
- **Racer is read-only from this project.** Copy from it, never edit it.
- **Import from Racer selectively.** Bring a system over only when the current
  round needs it, bring only what it depends on, and list what came with it in
  the round report. Don't copy Racer wholesale and don't bring its git history.
- **World scale is real:** 1 unit = 1 metre, no vertical exaggeration.
- **Map generation is a re-runnable script driven by a bounding box**, not
  hand edits to generated output. Hand-made additions live separately from
  generated data so regenerating never destroys them.
- **Raw downloads stay out of git** (the elevation tile is about 313 MB). Keep
  them in an ignored folder; commit only the cropped, processed data.
- **One world, routes laid over it.** Don't build a separate copy of the world
  per course.
- **Licensing:** OpenStreetMap needs a visible "© OpenStreetMap contributors"
  credit. No Google Maps data of any kind.
- **Privacy:** don't commit the owner's street address or house number.

## Commands
- Python deps for map generation: `pip install -r Tools/mapgen/requirements.txt`
- Generate map data (bounds and settings in `Tools/mapgen/config.json`):
  `python Tools/mapgen/mapgen.py` (`--refresh-osm` to re-download OSM).
  Writes `Data/heightmap_1m.tif`, `Data/map.json` (committed) and
  `Assets/Generated/terrain.r16` (ignored).
- Build the scene: Unity menu **Chastain Rush > Build Map**, or batch:
  `Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.MapBuilder.BuildFromCommandLine`
  Output: `Assets/Generated/ChastainPrototype.unity` (ignored, rebuilt every run).
- Play: open `Assets/Generated/ChastainPrototype.unity`, press Play.
  W/S or triggers, A/D or left stick, R or Y to reset.
- Automated drive check (Play mode, no keyboard needed):
  `Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.DriveCheck.Run -driveCheckOut <dir>`
  Writes `drive_log.tsv` and screenshots.
- Owner's house marker: put `{"housenumber": "...", "street": "..."}` in
  `local/owner_house.json` (git-ignored) before running mapgen.
- No player build yet.

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Round 1 (map prototype) is built and passed an automated drive check; it has
not yet been driven by Dan. Next is Dan's verdict (see `TODO.md`). Everything
else from Racer stays out of scope until then.

Imported from Racer so far (Round 1): PrototypeCar prefab, its materials and
physics material (`Assets/Vehicles/`), and ArcadeVehicle, VehicleInput,
ChaseCamera, ShallowWater (trimmed) and VehicleRespawn (rewritten) in
`Assets/Scripts/Vehicle/`. Edits made to the copies are commented in place.

End every round with a per-item status report: done / not done / deferred,
and why.
