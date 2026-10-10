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
- **Racer's systems are finished and open to use** (Dan, 2026-10-09). Prefer
  porting Racer's code to writing a new version of something it already has.
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

## Standing rules from Woodstock Rush (adopted 2026-10-09)
Full text: `Docs/FROM-WOODSTOCK-RUSH.md`. Read it once. The ones that bite:
- **KISS.** The smallest change. No unrequested roads, ramps, features,
  scenery or systems.
- **World changes only where Dan pointed.** Fix exactly the reported place;
  list similar problems elsewhere for Dan; do not change them unless the
  round says "all". His coordinates are the authoritative location.
- **Grounding.** When ground changes, everything on it moves with it. No
  floating or buried buildings, fences, signs or trees.
- **Protected geometry.** Roads, jumps (approach, lip, flight, landing,
  run-out) and shortcuts get local fixes only; never fill space another
  route uses; check the neighbours after a change.
- **Verification budget.** One targeted check per change, at the place it
  changed. No race matrices, sweeps or long validation documents unless
  asked. Dan playtests the rest. Report a short list plus "for Dan to check".
- **Check the way Dan plays:** in the built Windows exe, at 3840 x 2160, with
  a controller, driving flat out. An Editor or scripted pass is not proof.
- **Controller first.** Every menu reachable with a controller only, focus
  visible, B goes back; left / right changes a value, A never cycles one.
- **Signs.** Text is one-sided, hidden by terrain like its board, never drawn
  on top, fits the board, readable at 20 to 30 m. Depth-tested lettering,
  never a TextMesh default font material.
- **Never write Dan's real save or records in a check.** Use a copy.
- **Don't bother Dan** for normal operations (Unity, Git, build). Stop only
  for a real external blocker, with the exact action needed.
- **"This should be fun."** Forgiving resets, escapable water and pits, no
  cheap traps.
- **Claims need proof.** Before saying done, look at the result where Dan
  stood.

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
  `RaceCheck.Run -raceCheck lap|full|round6 [-raceDirection reverse] -raceCheckOut <dir>`
  (windowed Editor run, not -batchmode: drives the race route by script, writes
  `race_check_*.txt` / `round6_check.txt` and screenshots with the HUD; `round6` moves the car across
  every gate by hand through the real RaceController and tests penalties, a TEST shortcut and barriers;
  these cannot run while the Editor has the project open: use a copy of the folder),
  `python Tools/mapgen/shortcut_candidates.py` (report only: backyard shortcut candidates, overhead image).
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
- Player build (Round 8): Unity menu **Chastain Rush > Build Executable**, or batch:
  `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.PlayerBuild.BuildFromCommandLine`
  It runs Build Map first, then writes `Builds/ChastainRush/ChastainRush.exe` (git-ignored) and `BUILD_INFO.txt`.
  Start it with `Play Chastain Rush.bat` in the repo root. In the exe: Esc twice quits, F2 frame-rate counter.
  Records, debug reports, the bump log and `Player.log` go to
  `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\`. Exe self-check (frame rate at three places,
  a scripted 3-lap race): `ChastainRush.exe -exeCheck <dir>` writes `exe_check.txt` and screenshots, then quits.
- Official shortcuts (Round 8): `python Tools/mapgen/make_shortcuts.py [--plot]` writes
  `Data/shortcuts/chastain_glen_loop.json` (ramps, opened houses, the jumped house, the trail) and the two
  `official_shortcuts` of the route file; re-run it after mapgen (it reads `Assets/Generated/terrain.r16`), then
  Build Map. Checks: `ChastainRush.ShortcutCheck.Run -shortcutCheckOut <dir>` (batch; the real car through both
  shortcuts at 12-36 m/s against the road, plus race laps), `ChastainRush.Round8Check.Run` (screenshots to
  `Data/screenshots/round8/`). Unity empties the project's `Temp/` on exit: send check output elsewhere.
- Goose honks: drop `goose_honk*.wav|ogg|mp3` into `Assets/Audio/Geese/`, then Build Map. None there = silent geese.
- Round 9: `python Tools/mapgen/buried_houses_plot.py` (after Build Map) writes the report-only list and picture of buried
  houses (`Data/screenshots/round9/buried_houses.*`); `python Tools/handmade/make_entrance_sign.py` re-draws the monument
  panel (`Assets/HandMade/Entrance/ChastainGlenSign.png`, hand-made, committed).
- Fence exceptions near Dan's house: `local/owner_fences.json` (git-ignored; format in `MapBuilder.Neighborhood.cs`).

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Rounds 1 to 4 are built and driven by Dan: a 3983 m x 1918 m map from I-575
to past Canton Road, 18 terrain tiles, bridges, `vertical_scale` 1.5, 56,000
trees, roofed and coloured houses, driveways, road paint, sky and fog. Round 4
(see `TODO.md`) found the road bumps with a collider scan and fixed them in
mapgen, and added the on-screen coordinates, bug capture (F3 / F4 / F6) and the
automatic bump log (`local/bump_log.txt`). Round 4 is closed. Round 5 (see
`TODO.md`) is built and driven by Dan: the first race
route, a 3-lap loop around Chastain Glen Lane with checkpoints and a timer, laid
over the one world as data (`Data/routes/chastain_glen_loop.json`). No rivals,
and Racer's race systems are not imported. Dan keeps this game's checkpoint
LOOK (arches, lit next checkpoint with beams, HUD arrow) but wants Woodstock
Rush's RULES: time penalties for missed checkpoints, forgiveness on official
shortcuts, and side roads blocked during a race. The loop is a real course,
likely the starter race. Round 6 (see `TODO.md`) is built and scripted-checked, not yet driven by Dan: wide
forgiving gates, Racer's 5 s penalty per missed checkpoint (the lap still counts), official shortcuts in the
route file (none yet; a test one lives in the check only), barriers across every road that leaves the course
while racing (`RaceBarriers`, closures from `make_route.py`), and a report-only list of seven backyard
shortcut candidates (`Data/screenshots/round6/`).

Round 7 (see `TODO.md`, status there) is built and scripted-checked, not yet driven by Dan: beautify Chastain Glen only (Dan's house
hand-built from photos, four house styles, curbs and sidewalks, the entrance,
the pond with its fountain, and geese that scatter). Reference photos are in
`local/reference_photos/` (git-ignored): never commit them or copy them into
committed files.

**Round 11 (see `TODO.md`, status there) is built and checked in the exe, not yet driven by Dan:** #2 is now a jump through a
ground-floor picture window of 1763 and out a side window (an upstairs window needs a 40 deg ramp); no trails or markers
on shortcuts; #6 lands on the ground (a `LandingZone`); cul-de-sac bulbs, curb returns and lawn-proof road, drive and
sidewalk surfaces across Chastain Glen (`MapBuilder.Round11.cs`); Dan's mailbox and hydrant; the lake walk; the pool road;
the monuments turned in. Checks: `ChastainRush.exe -exeRound11 <dir>`; top-down pictures of the built scene:
`ChastainRush.TopDownShots.Run -topDownOut <dir> -topDownRects "name:x0:z0:x1:z1;..."` (Editor batch, with graphics).
Imported from Racer in Round 11: nothing.

**Round 11 brief (done):** Dan's third debug report (13
entries): shortcut #2's first house becomes a real jump through a large
window, no dirt trails, no landing deck on #6, clean driveways, roads and
sidewalks across Chastain Glen, bigger cul-de-sacs, the walk moved to the
lake, the entrance signs turned. Queued after it, one round each: Round 12
(Racer's vehicles with sound), Round 13 (3 AI drivers), Round 14 (menus and
the garage). Shortcuts are never marked: no arches, signs, trails or HUD
pointers (Dan, 2026-10-09).

**Round 10 (see `TODO.md`, status there) is built and driven by Dan from the exe:** shortcut #2 rebuilt with both houses
whole (a smaller ramp, in and out through breakable upstairs windows of the
first house, then in through a sliding glass door of the second, up its
stairs as a ramp and out through its upstairs windows), plus Dan's second debug report
(deck, house marker, more windows on his house and on every other house),
solid trees, and breakable street lamps and hydrants with a water jet.

Round 10 checks: `ChastainRush.exe -exeRound10 <dir>` (shortcut #2 shots and drives, tree / lamp / hydrant, resets) and
`ChastainRush.exe -exeShortcut <dir> [-shortcutOnly <id>] [-shortcutSpeeds 20,30] [-shortcutLatAcc 22] [-shortcutNoRace]` (the
shortcut check in the exe, with speed on / off every ramp). Shortcut #2 now goes in through 1763's upstairs front and out its
north side, down a landing slope to 1627's sliding door, up its stairs and out its upstairs front; both houses are whole
(breakable wall panels only). Imported from Racer in Round 10: `VehicleSurfaceContacts` (ramp contact normals). New:
`RampHold` (held ramp tops, landing carry), `TreeTrunks` (pooled trunk colliders), `CarRecovery` (reset rule), `HydrantWater`.

Round 9 (see `TODO.md`, status there) is built and driven by Dan from the exe: Dan's debug report
(the buried house 1520 regraded in the terrain itself, the gazebo and playground, the sidewalk with benches, the pool
fence, the entrance rail and two brick "Chastain Glen" monuments, his tree moved, drives that no longer show grass,
breakable mailboxes with most flags down), the back of his house and his yard, and debug keys that need no Fn
(`[` `]` `\` `P`). Item 1 (shortcut #2 as one big ramp over both houses) was stopped and reported: the run-up between
the lane and house 1763 is too short for a clean jump; #2 is still Round 8's drive-through until Dan picks an option.
Imported from Racer in Round 9: nothing. Look at a change in the exe from Dan's report position with
`ChastainRush.exe -exeShots <list.json> <dir>` (Checks/ExeShotsRunner.cs; the list: name, x, z, yaw, optional
drive_speed / drive_seconds / shot_at). Never commit shots that show where Dan's house is.

Round 8 (see `TODO.md`, status there) is built and driven by Dan from the exe: a Windows executable
(`Builds/ChastainRush/ChastainRush.exe`, `Play Chastain Rush.bat`), two official shortcuts on the Chastain Glen
loop (#2 "Two-house drop" through two opened houses, #6 "Rooftop leap" over house 1641), no fence on Dan's lot
(fences for the neighbour on his right and the lot behind him), and goose honks from audio clips (none yet).

Imported from Racer in Round 8: the player build follows Racer's `SoloReleaseBuild` (development / debugging /
profiler off, `BuildPipeline.BuildPlayer`, a build report next to the exe), re-written as `Assets/Editor/PlayerBuild.cs`;
its launcher, signing, versions and licence copying stayed behind. `Play Chastain Rush.bat` is Racer's `Play-Racer.cmd`
pointed at the exe instead of a launcher. Shortcut marking: Racer paints a dirt trail into the ground
(`BackyardForwardAuthoring`, colour 0.38/0.255/0.135); the same trail colour is used here, the blue arch and the
chevron sign are this game's checkpoint look. `WoodlandRoute`'s entry-plane idea was already in Round 6. Nothing
else was copied. New: `GameShell.cs`, `ExeCheckRunner.cs`, `MapBuilder.Shortcuts.cs`, `ShortcutCheck*`, `Round8*`.

**From Round 8 on, end every round by rebuilding the executable** and report
its path, so Dan tests from the exe, not from the Editor.

**Commit before reading `TODO.md` at the start of a round.** Claude (planning)
edits `TODO.md` and this file directly in the working folder between rounds,
so commit any uncommitted work first to keep those edits separate from yours.

Imported from Racer in Round 7: nothing was copied. Dan's house follows Racer's Kyle's-house method (a hand-authored, named object outside
the rebuilt world; its mesh is `Assets/HandMade/DanHouse/DanHouse.asset`, kept apart from generated data). The breakable fences re-implement
the idea of Racer's `BreakableProp` (trigger per prop, no impulse on the car) as `Assets/Scripts/Neighborhood/FenceChunk.cs` + `FencePanel.cs` (was `BreakableFence.cs`), with a speed
cost; the geese's honk is synthesized in code like Racer's `TurkeyVoice`. New: `GooseFlock.cs` (was `Geese.cs`), `FountainSpray.cs`; the area is dressed by
`Assets/Editor/MapBuilder.*.cs` (the builder is now a partial class). Data: `Data/neighborhood/` (made by `Tools/mapgen/neighborhood_area.py`
and `neighborhood_extras.py`, re-run both after a mapgen run). The Unity project now lists the built-in Particle System module.
Checks: `ChastainRush.Round7Check.Run` (19 screenshots to `Data/screenshots/round7/`), `ChastainRush.FenceCheck.Run`, `PerfCheck.Run -perfRound7`.
Run checks in a mirrored copy of the folder (the Editor has this one open); sync it with `robocopy ... /E /XO /XD Generated Library`, not `/MIR`.

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
