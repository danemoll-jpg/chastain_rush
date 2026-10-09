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
- **World scale is real:** 1 unit = 1 metre. Vertical exaggeration is off (1.0) by
  default; `vertical_scale` in `Tools/mapgen/config.json` stretches terrain, roads
  (draped on it) and building heights together, about the crop's lowest point.
  It is set to 1.5, and that is the decided working value (Dan drove it and
  kept it). True-scale terrain reads too flat in game. Design rule: the real
  world is the starting point, and fun wins when the two disagree.
- **Road flattening levels across the road's width only.** Along-road smoothing
  (`road_flatten.profile_smoothing_m`) is 0 because it reduced grades. Instead the
  road profile gets a curvature limit (`sag_max_m` 0.06 over +-3 m: removes
  lidar humps, keeps grades) and a junction level match (`junction_blend_m`),
  Round 4. Roads have no colliders; the car drives on the terrain collider.
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
  Writes `Data/heightmap_1m.tif`, `Data/map.json` and `Data/trees.bin` (committed) and
  `Assets/Generated/terrain.r16` (ignored), plus `Data/bridges.txt` (committed).
  Downloads NAIP 4-band imagery into `raw/` (ignored) for tree cover.
  The crop is a rectangle clamped to the elevation tile; terrain is 18 tiles.
  Other checks: `ChastainRush.BumpScan.Run` (batch, no Play mode: rays onto the
  real colliders along every road, writes `Data/bumps.txt`; run it after every
  mapgen + Build Map), `ChastainRush.HudCheck.Run` (windowed, not -nographics:
  checks the debug HUD, bug capture and bump log), `ChastainRush.SeamCheck.Run`, `PerfCheck.Run` (add `-perfGameView`,
  windowed, for the real Game view), `ScreenshotCheck.Run` (Round 3 shots to
  `Data/screenshots/round3/`),
  `DriveCheck.Run -driveBridge <n>`, `python Tools/mapgen/steep_roads.py`,
  `RaceCheck.Run -raceCheck lap|full [-raceDirection reverse] -raceCheckOut <dir>`
  (windowed Editor run, not -batchmode: drives the race route by script, writes
  `race_check_*.txt` and screenshots with the HUD).
- Race route: `python Tools/mapgen/make_route.py` writes
  `Data/routes/chastain_glen_loop.json` from `Data/map.json` (route road pieces
  are listed in the script by OSM id; re-run it after every mapgen run, then
  Build Map). **Direction is the `"direction"` line in that JSON**
  (`"forward"` / `"reverse"`; the Editor reads the file at race start, so no
  rebuild is needed; it survives re-runs of the script). Race keys in Play:
  T start / restart the race, X leave it (pad: D-pad up / down); R / Y still
  resets the car. Best lap and best total: `local/race_records.json`.
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

Rounds 1 to 4 are built and driven by Dan: a 3983 m x 1918 m map from I-575
to past Canton Road, 18 terrain tiles, bridges, `vertical_scale` 1.5, 56,000
trees, roofed and coloured houses, driveways, road paint, sky and fog. Round 4
(see `TODO.md`) found the road bumps with a collider scan and fixed them in
mapgen, and added the on-screen coordinates, bug capture (F3 / F4 / F6) and the
automatic bump log (`local/bump_log.txt`). Round 4 is closed. Round 5 (see
`TODO.md`) is built and scripted-checked, not yet driven by Dan: the first race
route, a 3-lap loop around Chastain Glen Lane with checkpoints and a timer, laid
over the one world as data (`Data/routes/chastain_glen_loop.json`). No rivals,
and Racer's race systems are not imported.

**Commit before reading `TODO.md` at the start of a round.** Claude (planning)
edits `TODO.md` and this file directly in the working folder between rounds,
so commit any uncommitted work first to keep those edits separate from yours.

Imported from Racer so far (Round 1): PrototypeCar prefab, its materials and
physics material (`Assets/Vehicles/`), and ArcadeVehicle, VehicleInput,
ChaseCamera, ShallowWater (trimmed) and VehicleRespawn (rewritten) in
`Assets/Scripts/Vehicle/`. Edits made to the copies are commented in place.

End every round with a per-item status report: done / not done / deferred,
and why.

Imported from Racer in Round 5: nothing was copied. The race is new code in
`Assets/Scripts/Race/` (`RaceRoute`, `RaceController`, `RaceMarkers`), shaped
after Racer's `RaceRoad` (closed centreline), `RaceGate` (directed timing plane
with a half width) and `RaceProgress` (ordered gates, rolling start). Racer's
`RaceDirector`, `RaceFlow`, penalties, menus and AI stay behind. One edit to an
imported file: `VehicleRespawn.PlaceAt` (commented in place).

Imported from Racer in Round 4: the debug report system, `Assets/Scripts/Debug/`:
`DebugReportSession.cs` (copied, Racer's AtomicSave folded in, race fields
replaced by road / vertical speed) and `DeveloperHud.cs` (a rewrite in IMGUI;
Racer's depends on its race, campaign, menu and uGUI code). New here:
`RoadLookup.cs`, `BumpLogger.cs`. Reports go to `local/debug_reports/`.

Imported from Racer in Round 3: the tree kit `SceneryTrees.fbx` (copied unchanged
to `Assets/Trees/`), and Racer's approach to drawing trees (instanced, near and
far kit). `Assets/Scripts/Trees/TreeField.cs`, `Assets/Editor/TreeBuilder.cs` and
`Assets/Shaders/VertexLit.shader` are rewritten from Racer's `SceneryTrees` and
`Racer/Foliage` shader (Racer's lighting include was dropped).
