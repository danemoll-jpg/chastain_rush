Active Development Plan — Chastain Rush

Sequel to Woodstock Rush, built on a real-world map generated from public map
data instead of a hand-built one. Working title: Chastain Rush.

**Design rule — DECIDED (2026-10-08):** the real world is the starting point,
and fun wins when the two disagree. The game does not need to be a 1:1 match.

**Jumps — DECIDED (2026-10-08):** natural jumps stay. A hill crest that
lifts the car evenly is part of the fun. Only awkward bumps get removed: ones
that tilt the car sideways, hit one wheel track, or come from data spikes and
mismatched junctions, and so spin the car out of control.

Completed Tasks

* **Repo decision — DECIDED.** Chastain Rush lives in its own clean repo
  (`C:\Users\danmo\chastain_rush`), not a duplicate of Racer. Racer's history
  and one-off files stay behind. Systems are brought over from Racer one at a
  time, only when a round needs them. Racer itself is not modified by any
  Chastain Rush round.
* **Round 1, map prototype — built; driven by Dan at true scale (2026-10-07).**
  A 1534 m square around Chastain Glen generated from USGS 1 m elevation and
  OpenStreetMap, drivable with one Racer vehicle. Bounds: S 34.0315,
  N 34.0450, W -84.5433, E -84.5271, including the New Chastain Road entrance.
  Dan's verdict: the layout is recognizable and he could find his way around,
  but the hills felt lower than in real life.
* **OSM coverage — checked, good.** 801 buildings (718 houses), 226 drivable
  road ways, 8 ponds, 21 streams (Noonday Creek, Little Noonday Creek),
  2 woods. No Microsoft footprints needed for this area.
* **"Hills feel flat" — root cause found: perception, not data.** The first
  suspicion was squashed heights. Measurement showed the true-scale terrain
  matches the USGS crop to 6 mm (273.10 to 329.34 m in game against 273.098
  to 329.350 m in the source), with no off-road smoothing (mean difference
  0.2 mm more than 15 m from any road). Terrain is 2049 x 2049 samples, 0.749 m
  per sample, bilinear from the 1 m source. Measured from the data file
  (`terrain.r16`), not from Unity's built TerrainData.
* **Along-road smoothing bug — fixed.** A 20 m smoothing of the road profile
  was cutting grades by 6.5% on average (up to 2.8 points). Now off
  (`profile_smoothing_m` 0); roads level across their width only. Real but
  small: it was not the cause of the flat feeling.
* **Vertical scale setting — added, set to 1.5. DECIDED (2026-10-08): keep
  1.5 as the working value.** `vertical_scale` in `Tools/mapgen/config.json`;
  1.0 is true scale. Scaling is from the lowest point, so only the relief
  grows. Scene needs Chastain Rush > Build Map after a change. Status: built,
  scripted drive passed on Chastain Glen Lane, and CONFIRMED by Dan driving
  it at 1.5 on his PC (2026-10-08). His verdict: keep it.
* **Reference hill measurements** (Claude Code's reading of Dan's directions,
  not yet confirmed by Dan: "right out of the driveway" taken as east, "first
  left the other way" taken as Chasefield Way):
  * Chastain Glen Lane hill, 160 m: true scale +12.5 m, 7.8% average, 12.7%
    steepest. At 1.5: +18.8 m, 11.7% average, 19.0% steepest.
  * Chasefield Way, 235 m: true scale +9.6 m, 4.1% average, 7.3% steepest.
    At 1.5: +14.4 m, 6.1% average, 10.9% steepest.
* **Round 1 follow-up work — committed** (`28bf044`), with the config at 1.5.
  The files git still lists as modified differ only in line endings.
* **Terrain/OSM alignment offset — DECIDED: leave as is.** Up to 0.37 m near
  the map edges from a half-pixel difference, about 4 cm of height on a 12%
  slope. Not worth fixing. (Round 2 samples the terrain at true positions, so
  the offset is gone in the new generator anyway.)
* **Steep-road check (Round 2 item 1) — recorded.** The original chat report
  was not kept anywhere in the repo, so this is a re-run of the same kind of
  check on the Round 1 data (`Tools/mapgen/steep_roads.py`: 20 m windows, true
  scale). It cannot say what Dan was told about Hayworth Drive NE at the time.
  What the data shows: Hayworth Drive Northeast is one 11-node residential OSM
  way with nothing odd in its tags, and it is simply steep: 94 m over 15% (max
  22.3%) plus another 83 m (max 17.2%), no water. Every stretch over 15% in the
  Round 1 area (residential unless noted): Joel Drive 124 m, max 30.4%, and
  32 m, 16.0%; Hayworth Drive NE 94 m, 22.3%, and 83 m, 17.2%; Embry Lane NE
  163 m, 21.5%, and 50 m, 18.3%; Elaine Circle NE 58 m, 21.2%, and 47 m, 16.9%;
  Blackwell Circle 69 m, 18.3%; Chastain Ridge Drive NE 25 m, 15.4%; one
  unnamed service road 21 m, 15.0%; and New Chastain Road (primary) over
  Noonday Creek, four 21 m windows at 18.8-20.5% (up to 38% over 10 m). The New
  Chastain stretch is the only one that crosses water: it is the creek-channel
  dip, not a real grade, and Round 2 bridges it. The rest are real hills. In
  the Round 2 area the same list holds, plus Kelly Drive NE (21.4%), Daisy
  Circle NE (18.3%), Hilltop Drive NE (17.8%), Suholden Circle NE (15.8%) and
  two service stubs at the south edge by I-575 (31% and 29%, probably highway
  cut walls). Nothing over 15% crosses water any more.

Round 2 status — built, scripted-checked, and DRIVEN by Dan on his PC
(2026-10-08). His verdict: the area is fine, but it is hard to judge until the
map is more built out. Size accepted for now; not final.

Final area: a rectangle in UTM, 3983 m east-west by 1918 m north-south
(SW corner 34.0294, -84.5665; NE corner 34.0458, -84.5229). Landmarks: I-575
inside with about 200 m beyond it; east edge about 400 m past Canton Road at
its easternmost point; south edge about 300 m south of New Chastain Road.
**The north side stops at the elevation tile edge** (lat 34.0467 at the west
end, 34.0458 at the east end): 1 km north of Chastain Glen Lane would have
needed 34.049. The map reaches about 620 m north of the lane and is 1.9 km
north to south, not 2.2. A second tile is the next step for more.

1. Steep-road findings: DONE, recorded above (a re-run; Hayworth's original
   conclusion is not recovered).
2. Tile edge check: DONE. West, east and south are inside the tile; north is cut.
3. Regenerate at vertical_scale 1.5: DONE (592 road pieces, 2388 buildings,
   22 water areas, 33 streams drawn). The crop is a rectangle, not a square.
4. Terrain tiles: DONE. 6 x 3 = 18 tiles of 1025 x 1025 samples, about
   0.65 x 0.62 m per sample (finer than Round 1's 0.749). `SeamCheck.Run` on the
   built scene: 0.00000 m difference across all 27 seams; the largest step from
   a seam sample to the next is 0.84 m, below the 3.0 m steepest step inside
   tiles (real lidar walls). Not yet walked by Dan.
5. Bridges: DONE for what the data holds. 9 bridge pieces, all from OSM
   `bridge` tags (`Data/bridges.txt`): New Chastain Road over Noonday Creek (two
   decks, 33 m each, up to 6.8 m above the creek bed); four Chastain Road NW
   decks at the I-575 interchange (39-72 m, not over mapped water); three tiny
   I-575 / express-lane pieces cut at the south map edge. Crossings with no
   bridge tag over open streams or ponds: none found. Seven crossings of
   streams OSM maps as culverts were left on the ground (dips 0.01-0.38 m).
   The untagged-crossing logic (span search, level deck, 0.4 m minimum dip) is
   built but had no untagged case to act on here, so it is untested on real
   data. Decks have colliders and 0.7 m parapets.
6. Major roads: DONE as a report, no special handling. Widths come from lane
   counts: I-575 carriageways 8 m (one-way) or 11.5 m, the express lane 4.5 m,
   New Chastain Road one-way ways 8 m (11.5-15 m near junctions), Canton Road
   8-22 m, Bells Ferry 8 m. Likely wrong, judged from the data (I have not
   driven them): I-575 and its express lane are separate side-by-side ways with
   no median or barriers (6 of 17 sampled highway points lie within a road
   width of another highway way); the express lane is not flagged one-way; the
   10 ramp (`_link`) ways are plain narrow roads; overpasses are bridged but
   nothing handles the road beneath; dual carriageways of New Chastain and
   Canton Road are separate ways with an unfilled gap. For Dan to judge.
7. Performance: DONE with a caveat. Batch mode does not draw the game view, so
   `PerfCheck.Run` renders the main camera offscreen at 1920 x 1080 each frame
   and waits for the GPU (Editor, GTX 1660 Ti, i7-9700, with another Unity batch
   job running at the time). Round 1 spawn: 136 fps average, 99th-percentile
   frame 9.2 ms (108 fps), 568 draw calls, 0.69 M triangles. Densest part
   (x 3152, z 1610): 135 fps average, 99th percentile 9.3 ms, worst 15.5 ms, 610
   draw calls, 0.71 M triangles. Both above 60, so no optimizing. Within 300 m:
   spawn 179 buildings and 4131 m of road in 42 ways; densest 195 buildings and
   3426 m in 31 ways. Whole map: 2388 buildings, 592 road pieces, 134 renderers.
   Not a player build or a windowed run: Dan's own Game-view frame rate is
   unmeasured.
8. Drive checks: DONE (scripted). Chastain Glen Lane: 1252 m, last waypoint
   reached, max tilt 10.9 deg, never fewer than 2 wheels grounded. Round 1's
   numbers were not recorded, so "matches Round 1" is not checked numerically.
   Bridge route (`-driveBridge 5`, New Chastain Road over Noonday Creek): 178 m,
   crossed the deck with 4 wheels grounded, 6.75 m above the terrain at
   mid-span, max tilt 2.1 deg. It stopped about 1 s after the deck end, short
   of the planned length (a path-walking quirk in the check, not the bridge).
9. This report: DONE.

New tools: `GroundQuery` (tile-aware ground height for checks), Editor
`PerfCheck`, `SeamCheck`, `steep_roads.py`.

Round 3 status — built, scripted-checked, and DRIVEN by Dan on his PC
(2026-10-08), over most of the map. His verdict: generally feels pretty good,
looks good, no lag. Problems he found: random bumps on roads that sometimes
send the car flying (Round 4), and the Chastain Road overpass at I-575 has
collapsed onto the highway, which also has no real ramps (deferred, see Next
Steps).

1. Trees: DONE. NAIP 4-band imagery (tile m_3408460_se_16_060_20231007, flown
   2023-10-07, so autumn leaves; fetched at 1 m from USGS's image service into
   `raw/`). Canopy = NDVI > 0.2 with texture, or dark vegetated pixels; canopy is
   46% of the crop. One tree per 7 m jittered-grid cell (hash-stable, so
   regenerating gives the same file) where there is canopy, none on roads,
   buildings, water, bridges or driveways, thinned to half in yards. **56,213
   trees** (47,822 woods, 8,391 yards): Round 11,619, Oak 9,801, Maple 9,616,
   Pine 14,187, Poplar 10,143, Young 847. Models: Racer's six tree types
   (Bush and Shrub not used). Triangles near / far (crown + trunk): Round 687 / 103,
   Oak 860 / 123, Maple 514 / 83, Pine 155 / 59, Poplar 568 / 83, Young 341 / 83.
   Racer draws about 12,600-13,000 trees per scene; this map has 4.4x that. Tree
   heights get vertical_scale like the houses. No colliders (the car drives
   through trees): not asked for, deferred.
2. Tree performance: DONE. Instanced (`TreeField`): detailed kit within 55 m with
   shadows, far kit (three shapes) beyond with no shadows, trunks to 160 m,
   nothing past 700 m; 48 m cells inside 256 m far cells, frustum culled. In
   view at spawn about 5,300 trees, about 90 tree draw calls.
3. Houses: DONE. 1,335 gable, 846 hip, 50 pyramid (footprint far from a
   rectangle) roofs on detached houses and other house types (also terraces,
   semi-detached, garages, church); 157 other buildings stay flat. 8 wall and 5
   roof colours by a hash of where the building stands. Owner house keeps its
   orange colour and beacon. Roof rise is scaled by vertical_scale.
4. Driveways: DONE. 107 from OSM, 2,088 generated straight to the nearest road
   (not for 18 houses that had an OSM driveway within 6 m, 13 with no road within
   45 m, 57 blocked by another building or water). Visual only.
5. Road markings: DONE. Pale edge strip on every road; primary / secondary /
   tertiary / trunk / motorway and links also get white edge lines; two-way ones a
   double yellow centre line, wide ones white dashed lane lines; one-way and
   motorway carriageways white dashes. Quiet residential, unclassified and service
   roads get only the edge strip. Paint is cut where another road's pavement
   covers it. Also on bridge decks. Not checked: markings at every junction.
6. Sky, sun, fog: DONE. Gradient sky whose horizon colour equals the linear fog
   (clear to 100 m, solid at 650 m); camera far clip 750 m, near 0.25 m.
7. `.gitignore` for `Assets/_Recovery/`: DONE (and the three recovery scenes
   removed from git; the files are still on disk).
8. Performance: DONE, close to the line. Windowed Editor Game view
   (`-perfGameView`) is only 734x293 here, so it flatters: 202 fps spawn, 208
   fps densest. The fair number is the offscreen 1920x1080 render (as Round 2):
   spawn 70 fps average, 14.2 ms median, 99th percentile 17.9 ms (56 fps);
   densest 70 fps, 14.0 ms, 99th 18.0 ms (56 fps); about 1.2 M triangles and
   1,100 draw calls. So the average is over 60 and the worst 1% dips just under.
   First attempt was 44 / 52 fps; it took: roads, paint and driveways split into
   128 m chunks (they were one mesh for the whole map) and shadows off for them;
   far trees reduced to three shapes; near distance 90 -> 55 m; draw distance
   1000 -> 700 m with the fog pulled in to match. No area change needed. Dan's own
   Game-view rate is still unmeasured.
9. Screenshots: DONE, `Data/screenshots/round3/` (01_spawn, 02_hill_past_bend,
   03_new_chastain_bridge, 04_canton_road, 05_i575, 06_overhead, and
   screenshots.txt with the positions). "Hill past the bend" is my pick (first bend
   on Chastain Glen Lane from the spawn, then its steepest 60 m, 15.9%); the
   others are the widest Canton Road piece and the middle of I-575's longest piece.
10. This report: DONE.

New tools: `TreeBuilder`, `TreeField`, `ScreenshotCheck`, `VertexLit` and
`SkyGradient` shaders; `PerfCheck` got `-perfGameView`. Not run this round: the
drive checks (road geometry and colliders were not changed; houses' roofs now
have colliders).

Round 4 status — built, scripted-checked, and DRIVEN by Dan on his PC
(2026-10-08, about 27 minutes of play). His verdict: nothing to report. Items
1-6 were done in order; the scan was reported before anything was fixed.
What `local/bump_log.txt` recorded on that drive (read by Claude, planning):
six times airborne, top speed seen 34 m/s (77 mph). Four were small hops of
0.24 to 0.44 s with almost no height, two of them at the same spot on New
Chastain Road near x 2918, z 750. One was a real jump: New Chastain Road at
x 3079, z 774, 0.82 s and 1.44 m up at 66 mph. So crests still give air with
`sag_max_m` at 0.06, and Dan did not ask for more: leave it. Two entries looked
like hard jolts (New Chastain Road at x 1711, z 649, and Claybrooke Drive NE
at x 2711, z 622, both about 5 m off the centreline). Dan confirmed he was
driving off-road there, so they are not road bumps and nothing is to be
fixed. **Round 4 is closed: bumps fixed, confirmed by Dan's drive.** Bug
capture: a debug session folder was created but no report was filed, so F4
is still untested by a real key press.

1. Road-surface scan: DONE. `Assets/Editor/BumpScan.cs`, run in batch mode (no
   Play mode needed, about 35 s): rays straight down onto the scene's real
   colliders every 0.25 m along the centreline and both wheel tracks (+-0.85 m,
   the car's suspension points), over all 604 road and bridge pieces (78.8 km,
   316,043 stations, 948,129 rays, started 1.5 m above the surface: above the
   car's roof, below house eaves). It flags: STEP (jump between neighbours
   beyond the local grade, over 3 cm), HUMP (crest or dip against the chord
   +-3 m, over 10 cm; a crest of 10 cm launches a car above about 21 m/s, 30 cm
   above about 12 m/s), COLLIDER (first hit is not the road surface), OBSTRUCT
   (solid object in the lane corridor), MISS. Output `Data/bumps.txt` (one row
   per cluster: position, road, size, collider hit, features) and
   `Data/bumps_before_fix.txt` (the same scan on the pre-fix map). The collider
   inventory matters: roads, paint and driveways have NO colliders; the car
   drives on the terrain collider, bridge decks and buildings only.
2. Findings before fixing: DONE (reported in chat before the fixes). Before the
   fix, outside the I-575 zone (within 100 m of a highway-class road or an
   I-575 deck, out of scope): 166 STEP, 296 HUMP, 11 OBSTRUCT. Serious ones
   (STEP >= 10 cm, HUMP >= 30 cm): 13 and 9. Causes found:
   a. A bug in the road flattening (`build_terrain`): a cell took the height of
      whichever road segment pulled on it first, at weight 0.999999, because of
      a 1e-6 tie tolerance, instead of the nearest segment. It left single-
      sample spikes up to about 0.7 m in the middle of ordinary streets (found
      by tracing one: Brickelberry Court, a 34 cm bump at the car's wheel
      track). Fixed with a 1e-3 tolerance (nearest segment wins ties).
   b. Lidar humps in the centreline profile (culverts, embankments), kept
      because along-road smoothing is off; vertical scale 1.5 amplified them.
   c. Junction steps: each road took its own height at a junction, 8-15 cm
      apart on steep through roads.
   d. Unnamed service roads running through or along buildings (parking aisles,
      loops, one building passage tagged tunnel): OBSTRUCT / COLLIDER.
   Not causes: 128 m chunk boundaries (roads have no collider; 5% of clusters
   against 4.6% of road length), terrain tile seams (2 of 628 clusters; 1.2% of
   road length). Junctions and bridge ends are over-represented (35% and 7% of
   clusters against 9.8% and 0.3% of road length), the bridge ends being nearly
   all the I-575 / Chastain Road decks.
3. Fixes at the source (`Tools/mapgen/mapgen.py`, `config.json`): DONE.
   `road_flatten.sag_max_m` 0.06 (curvature limit: no crest or dip sharper than
   0.06 m over a +-3 m chord, crest radius 75 m; straight grades untouched, only
   humps shorter than a car are rounded: 2.5% of stations moved over 2 cm,
   0.47% over 10 cm), `junction_blend_m` 12 (a road meeting a wider one takes its
   height at the junction, fading over 12 m; 530 junction nodes), the tie fix
   above, and `service_building_clearance_m` 1.5 (12 service roads cut, 123 m
   removed). Before and after, same scan, outside the I-575 zone: STEP >= 10 cm
   13 -> 1; STEP >= 5 cm 50 -> 31; HUMP >= 20 cm 52 -> 3; HUMP >= 30 cm 9 -> 0;
   OBSTRUCT 11 -> 0; HUMP >= 10 cm 296 -> 76. What is left: 149 STEPs of 3-5 cm
   (median 4 cm; probably the terrain collider's triangle zigzag on steep
   grades, NOT verified), one 12 cm step at the Clarington / Brickelberry
   junction, 5 COLLIDER rows (New Chastain Road deck ends: the deck meets the
   road, no step), 4 MISS (the map's outer edge). The I-575 zone is untouched:
   12 steps of 1.2-3.0 m remain there (the collapsed Chastain Road overpass, the
   cut-off deck ends at the south edge), plus roads on the I-575 embankment near
   Big Shanty Road. The scan cannot say whether a launch still happens at
   speed; that is Dan's drive (`local/bump_log.txt` will say where).
4. Coordinates and bug capture: DONE, scripted-checked in a windowed Editor Play
   session (`HudCheck.Run`: capture, comment, save, report files, and a bump log
   line from a test ramp). I did NOT press the F3 / F4 / F6 keys myself.
   Brought over from Racer's `DeveloperLocationHud` and `DebugReportSession`
   (`Assets/Scripts/Debug/`): `DebugReportSession.cs` (copied; Racer's
   `AtomicSave` folded in; race fields dropped, road name / distance / vertical
   speed added) and `DeveloperHud.cs` (REWRITTEN: Racer's is built on
   RaceDirector, RaceFlow, Campaign, MenuInput / MenuGlyph, WorldLook,
   GetawayChase, VehicleConfiguration and uGUI, none of which this project has).
   New here: `RoadLookup.cs` (road names, filled into the scene by MapBuilder)
   and `BumpLogger.cs`. CUT: race / lap / checkpoint / course lines, the debug-
   fly camera and its "records disabled" flag, the campaign cheat buttons, the
   Getaway log, all of the uGUI canvas (drawn with IMGUI, like `MapCredits`).
   Nothing needed the launcher or publishing setup; the only build-only thing
   was `Application.persistentDataPath`, now used just in a player build.
   How Dan uses it: press Play. A panel at the lower right shows X Y Z, heading,
   speed, the nearest road and the distance from its centre (on road / off
   road), wheels on the ground and vertical speed. F3 hides / shows it (it is on
   from the start). F4 (or gamepad Select): the game pauses, the screenshot is
   saved with the panel on it, and a box opens: type the problem, Enter saves,
   Shift+Enter is a new line, Esc cancels (gamepad: A saves, B cancels). F6 (or
   Start): menu with Export Bug Report ZIP, Open Debug Report Folder, Start New
   Debug Session. Files land in `local/debug_reports/<session>/` (`bugs.json`,
   `BUG_REPORT.md`, `Screenshots/BUG-001.png` ...; the ZIP beside it),
   git-ignored.
   Bump log: `local/bump_log.txt` (git-ignored). A line is added, with no key
   pressed, when the car leaves the ground on a road for 0.2 s or more (AIRBORNE,
   at the take-off point, then LANDED with air time and peak), or its vertical
   speed changes by 2 m/s or more within 0.1 s while driving on a road (JOLT).
   Each line has time, x y z, road name and distance from its centre, speed. Not
   active in batch mode.
5. Drive checks: DONE (scripted). Chastain Glen Lane: 1252 m, last waypoint
   reached, max tilt 10.9 deg, 0.00 s with fewer than 2 wheels grounded,
   identical to Round 2. Bridge route (`-driveBridge 5`): 178 m, 4 wheels
   grounded across the deck, 6.75 m above the terrain at mid-span, max tilt
   2.1 deg, identical to Round 2. Both drive at 13 m/s and neither covers a road
   that had bumps, so "bumps gone" is Dan's to judge.
6. This report: DONE.

New tools: `BumpScan`, `HudCheck` / `HudCheckRunner`, `RoadLookup`,
`DeveloperHud`, `BumpLogger`, `DebugReportSession`.

Current Objective (Focus Area)

**Round 5 — first race route: the Chastain Glen loop. DECIDED (2026-10-08).**
Dan chose a loop around Chastain Glen Lane. This is the first thing that makes
the map a game, so keep it simple: one route, one car, a clock. No rivals.

The loop, from `Data/map.json` (checked by Claude, planning): Chastain Glen
Lane is not a closed loop by itself. Its long piece (979 m) runs from the
Glenbarrett Lane junction at about x 3060, z 951 round to the Courtfield
Trace junction at about x 2868, z 1048, and Chastain Trail links back from
about x 2808, z 1008 to the junction near the entrance at about x 2940,
z 877. Lap, CONFIRMED by Dan (closing via Chastain Trail): start just inside the entrance, north to the Chastain
Trail junction, along Chastain Glen Lane past Glenbarrett Lane and all the
way round the long piece, then back down Chastain Trail to the same junction.
Expected lap length roughly 1.2 km. If the geometry says otherwise, say so
and propose the nearest sensible loop; do not silently change the route.

1. Look at how Racer defines a course (route, checkpoints, start grid) and
   report it in a few lines. Do not import Racer's race systems this round
   (RaceDirector, RaceFlow, Campaign and the rest stay deferred), but keep
   the route data in a shape they could read later where that is cheap.
2. Define the route as data laid over the one world (an ordered list of road
   pieces or waypoints in a file under `Data/routes/`), not as a copy of the
   map. Report the lap length and the junctions it passes.
3. Start/finish line and checkpoints: a visible start/finish gate and visible
   checkpoint markers at each junction and bend where a shortcut is possible.
   The next checkpoint should be obvious from the driver's seat.
4. Race flow: a 3-2-1 countdown, 3 laps, a lap timer and total timer on
   screen, lap count, and a finish screen with total and best lap. Missing a
   checkpoint must not count the lap; show which one was missed. A key to
   restart the race, and the car starts on the grid facing the right way.
5. Save the best lap and best total to a git-ignored file and show them.
6. Free driving must still work: racing is something Dan starts with a key,
   and the debug panel, bug capture and bump log keep working during a race.
7. Make the direction a setting, so the reverse lap is a one-line change.
8. Scripted check: drive the route by script for one lap and confirm every
   checkpoint registers in order and the lap time is recorded.
9. Report status for every item: done / not done / deferred, and why.

Out of scope for this round: rivals, traffic, multiple routes, menus, tree
collision, and anything on the map itself.

What Dan will judge when driving it:
* Is the loop the right one, and the right direction?
* Is a lap fun, and about the right length?
* Are the checkpoints easy to follow at speed?
* Would he rather race into trees that stop the car (tree collision next)?

Next Steps (Do Not Start Yet)

* All of these are deferred, not decided against.
* If hills still read flat at a height setting that drives well: a lower chase
  camera and narrower field of view, before raising the scale further.
* Growing the map further north needs a second elevation tile (the current
  one ends at latitude 34.048).
* Proper handling for major roads: medians, ramps and overpasses. Known
  broken: the Chastain Road overpass at I-575 lies collapsed on the highway,
  and I-575 has no real ramps. Dan expects that edge of the map may not be in
  the final game, so decide whether to trim the west edge east of I-575
  before spending any work on it. Open, not urgent.
* Tree collision: the car drives through trees.
* Bump log: it counts the car as on a road up to about 5 m from the
  centreline, so off-road driving beside a road gets logged as a road bump.
  Tighten it to the road's real half-width when the log is next used.
* One world with race routes laid over it (not a copy of the world per course,
  as in Woodstock Rush).
* Houses missing from OSM, found by comparing against NAIP imagery.
* The untagged-bridge logic from Round 2 is untested on real data; check it
  the first time the map grows into an area that has such a crossing.
* Hand-built versions of the houses that matter, from photos, as was done for
  Kyle's house in Woodstock Rush.
* Hand detailing of the Chastain Glen entrance. Dan confirmed from photos
  (2026-10-08) that the divided entrance with a median island is real, so the
  generated layout there is correct. Missing against the photos: trees and
  trimmed shrubs on the island, a white rail fence along one side, sidewalks
  and curbs. Dan has reference photos. Also check the lighter grey patch on
  the road beside the island in `01_spawn.png`, which looks like a glitch.
* Importing the remaining Racer systems: rival AI, traffic, campaign, Free
  Roam, split-screen, menus and garage, radio, launcher and publishing. (The
  debug-report tool comes over early, in Round 4.) Deferred until Woodstock Rush is finished, so the final versions
  are what gets brought over.
* Vehicle list, course list, campaign structure, and the final name.
* When Woodstock Rush is finished: record what to keep, what to build
  differently, and the final state of each reused system.

Technical Notes / Blockers

* **Size against Woodstock Rush:** that world is about 1.2 km by 0.9 km,
  roughly 1 km2 (an 800 m neighborhood block plus the mountain area), and its
  courses run 1.5 to 2.7 km by looping inside it. This map is 3983 m by
  1918 m, about 7.6 km2, so roughly seven to eight times the area. Expect
  tree and building counts to scale the same way; whatever Racer does for
  trees has only been proven at about one eighth of this size.
* **Elevation tile:**
  `https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/GA_Statewide_2018_B18_DRRA/TIFF/USGS_1M_16_x72y377_GA_Statewide_2018_B18_DRRA.tif`
  Projection is UTM zone 16N, NAD83, in metres; elevations are metres
  (NAVD88). It is a bare-earth surface, so buildings, trees and bridges are
  removed. Tile covers latitude 33.955 to 34.048 and longitude -84.619 to
  -84.508. Public domain.
* **OpenStreetMap** data is under the ODbL licence. The game needs a visible
  credit, "© OpenStreetMap contributors".
* **Google Maps data can't be used in a game.** Not for geometry, imagery or
  tracing.
* **NAIP aerial imagery** is a placement reference only, not a texture source.
* **True-scale terrain reads flatter in game than in real life.** This is a
  camera and reference-object effect, confirmed here by measurement. Expect it
  again on any new area; check the numbers before suspecting the data.
* **Terrain rendering:** Unity's `heightmapPixelError` = 3 simplifies the
  visible mesh at a distance; collision uses the full-resolution data.
* **`terrain.json` stores a padded height range** (1 m each side), not the
  real extremes.
* **Verification wording:** "built", "scripted drive passed" and "driven by Dan
  on his PC" are separate claims. Record which one applies.
* Process notes for this collaboration are in `collaboration-process-notes.md`.
