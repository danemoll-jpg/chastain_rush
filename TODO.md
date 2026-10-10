Active Development Plan — Chastain Rush

Sequel to Woodstock Rush, built on a real-world map generated from public map
data instead of a hand-built one. Working title: Chastain Rush.

**Design rule — DECIDED (2026-10-08):** the real world is the starting point,
and fun wins when the two disagree. The game does not need to be a 1:1 match.

**Jumps — DECIDED (2026-10-08):** natural jumps stay. A hill crest that
lifts the car evenly is part of the fun. Only awkward bumps get removed: ones
that tilt the car sideways, hit one wheel track, or come from data spikes and
mismatched junctions, and so spin the car out of control.

**Checkpoints — DECIDED (2026-10-08), corrected the same day.** Keep the
Round 5 LOOK: arch markers, the next checkpoint lit with tall beams, and the
arrow and distance on the HUD. Dan prefers these to Woodstock Rush's arrows
on the ground. Keep Woodstock Rush's RULES: a missed checkpoint costs a time
penalty (the lap still counts), and taking an official shortcut forgives the
checkpoints it skips. (An earlier note here said the Round 5 rules were kept
and Racer's penalties dropped. That was Claude's misreading and is wrong.)

**Races — DECIDED (2026-10-08): the Chastain Glen loop is a real course**, most
likely the starter race because it is short (1247 m a lap).

**Shortcuts — DECIDED (2026-10-08): the same kind of system as Woodstock
Rush.** Official shortcuts run through backyards, with jumps, including over
houses or even through them. They should be challenging, rewarding and fun,
with huge jumps where the land allows. A shortcut does not need checkpoints
of its own. Roads that are not part of the course are blocked off during a
race, unless they are part of an official shortcut.

**Racer imports — DECIDED (2026-10-09): the hold is lifted.** Dan says nearly
all Woodstock Rush systems are finished; anything he adds there will be more
tracks. Take whatever is needed from Racer from now on, and prefer porting
its finished code to writing a new version. The rules still stand: Racer is
read-only, bring only what a round needs, and list what came with it.

**Woodstock Rush handoff — ADOPTED (2026-10-09).** Dan supplied the rules and
notes from the original game: `Docs/FROM-WOODSTOCK-RUSH.md`. Its standing
rules now apply here (the short list is in `CLAUDE.md`). Where this project
has already chosen differently, this project's decision stands: a clean repo
with selective imports (not copy-then-strip), `TODO.md` / `CLAUDE.md` (not
`PROJECT_TODO.md` / `CODEX_RULES.md`), and this game's checkpoint look.

**Shortcuts are found, not signposted — DECIDED (2026-10-09).** Dan: "I
don't think we need the blue arches for for the shortcut maybe not even the
signs. I think the fact that there is a ramp there is evidence enough. In the
Rush series, which half-way inspired this, the shortcuts were not always
obvious and sometimes you had to find them." No arches, beams, signs, trails
or HUD pointers on any shortcut, now or on future courses. The ramp or the
window is the only clue. Taking one still forgives its checkpoints.

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

Round 5 status — built, scripted-checked (forward and reverse), and DRIVEN
by Dan on his PC (2026-10-08). His verdict: fun for a first pass, and he
likes this checkpoint handling better than Woodstock Rush's. One problem: he
crossed the finish line at its very edge and it did not count until he
backed up and drove through nearer the centre (Current Objective).
Original coding-pass note follows. I did not press T / X or use a pad myself; the check calls the
same functions the keys call. The loop matched the road data, so nothing was
changed from the route Dan confirmed.

The lap: the entrance spur (inbound lane, from the map spawn, 78 m) to the
Chastain Trail junction, then Chastain Glen Lane north-east to Glenbarrett Lane
(148 m piece), round the long piece (979 m piece, used for its first 906 m: the
lap leaves it where Chastain Trail starts, so the last 73 m, on to Courtfield
Trace, are not in the lap) and back down Chastain Trail (192 m) to the same
junction. Lap length 1247 m (expected about 1.2 km). Start/finish gate 22 m
past the Chastain Trail junction, on the way to Glenbarrett Lane. Grid to gate
(forward): 100 m.

1. Racer's course shape: DONE (read, nothing imported). A course is a closed
   `RaceRoad` polyline (Repeat-wrapped distance, `At(s)`); gates are `RaceGate`
   directed planes (half width 6 m, half height 3 m, tested on the segment from
   the last step to this one); `RaceProgress` holds the ordered numbered gates
   (0 = start/finish), a rolling start (clock starts at the first crossing of
   gate 0) and a 5 s penalty per missed gate; the grid is placed a fixed
   distance before the gate on the road (`RaceDirector`, about 32 m back,
   alternating sides). Differences here: no penalties (a lap with a missed
   checkpoint does not count, as asked), gate half width 9 m / 11 m for the
   wider streets here, and the route is a JSON file, not scene objects.
   `RaceDirector`, `RaceFlow`, Campaign, menus and AI were not imported.
2. Route as data: DONE. `Tools/mapgen/make_route.py` reads `Data/map.json` and
   writes `Data/routes/chastain_glen_loop.json`: the road pieces by OSM id
   (lead-in 1161774882 + 1360406926; loop 9134741 reversed, 9134737 from 0 to
   906.4 m, 9125364), the resolved centreline (x, z only; heights come from the
   world), junctions, shortcut analysis and checkpoints. Re-run it after mapgen.
   Junctions passed, in lap order from the gate: Courtfield Trace (49 m),
   Glenbarrett Lane (126), Chasefield Way (179), two 6-8 m Chastain Glen Lane
   stubs (286, 437), Chasefield Way again (621), Chastain Glen Lane toward
   Courtfield (710), Chastain Place (947), the entrance / Chastain Trail
   junction (1225). Real shortcuts through the road network: Chasefield Way
   (179 to 621: 236 m against 442 m round the lap, saves 206 m) and Courtfield
   Trace to the Chastain Glen Lane branch at 710 m (253 m against 661 m, saves
   408 m), plus the same two in the other direction.
3. Gate and checkpoints: DONE. A black-and-white chequered gate across the
   road (always standing). 5 checkpoints, in order: CP1 Chasefield Way (179 m),
   CP2 the far bend (400 m), CP3 Chasefield Way (621 m), CP4 the Chastain Glen
   Lane branch (710 m), CP5 on the way back near Chastain Place (979 m). Every
   road shortcut skips at least one. Markers: an arch per checkpoint; the NEXT
   one is bright green with two 80 m tall beams (visible over hills and trees),
   later ones amber, skipped ones red, passed ones hidden. HUD arrow and
   distance to the next gate, top centre. No colliders on any marker. Off-road
   cuts across yards are not blocked: only the gates are checked, in order.
4. Race flow: DONE. 3-2-1 (3.02 s measured) then GO; 3 laps; lap timer, total
   timer, lap count, best lap and best total on screen; finish panel with total,
   best lap and every lap (including ones that did not count). Rolling start as
   in Racer: the clock starts when the car first crosses the start line, so a
   lap is gate to gate (the 100 m grid run-in is not timed). A checkpoint
   passed out of order marks the skipped ones missed at once ("Missed
   checkpoint 2, 3 - this lap will not count"); at the finish line the lap is
   not counted and is run again (a race is 3 COUNTED laps; the clock keeps
   running, so a lap that did not count still adds to the total). Keys: T starts
   or restarts the race (any time), X leaves it; pad D-pad up / down. The car
   is put on the grid facing the way of travel, held until GO.
5. Records: DONE. `local/race_records.json` (git-ignored; a player build uses
   persistentDataPath), one entry per route, direction and lap count; best lap
   is saved when it is set, best total on finish; both shown in the HUD and on
   the finish panel, and before any race in free driving.
6. Free driving and the debug tools: DONE by script, not by hand. Nothing runs
   unless T was pressed, apart from the gate and one hint line. During a race
   the debug panel shows the race state (so bug screenshots carry it). F4 bug
   capture was run mid-race by the check: the race clock stopped while the
   game was paused (246.58 to 246.62 s) and the comment saved. The bump logger
   is present (not checked for a bump during a race). A reset (R / Y) during a
   race counts as a teleport and cannot cross a gate; it returns the car to its
   last stable spot, and a restart makes the grid the reset spot. Race keys are
   ignored while the debug menu or comment box is open (typing a "t" in a bug
   comment must not restart the race).
7. Direction setting: DONE. `"direction": "forward" | "reverse"` in the route
   JSON (the Editor reads the file at each race start, no rebuild; make_route.py
   keeps your value), and `-raceDirection reverse` on the command line. Reverse
   drives the loop the other way with the gate where it is: the grid is 45 m
   before the gate on Chastain Glen Lane (no entrance lead-in), checkpoints are
   renumbered in travel order. Reverse checked for one lap (below).
8. Scripted check: DONE. `RaceCheck.Run` (windowed Editor, 6x time scale), a car
   driven along the route centreline at 15 m/s. Forward, mode full, 22 of 22
   checks passed: grid pose, 3 s countdown, lap 1 with all five checkpoints 1-5
   in order, counted at 1:22.676 (15.1 m/s average); a lap cutting through
   Chasefield Way was NOT counted and named "missed CP2, CP3" (CP3's plane is
   not always crossed when merging at an angle, so both are reported); the race
   then finished after 3 counted laps (total 5:17.360 including the lap that did
   not count; laps 1:22.676, 1:22.751, 1:22.678); records file written with best
   lap 82.676 and best total 317.360; bug capture mid-race; back to free
   driving. Reverse, mode lap, 8 of 8 passed: five checkpoints in order, lap
   1:22.607. Reports and screenshots with the HUD: `Data/screenshots/round5/`
   (01_countdown, 02_next_checkpoint_close, 03_checkpoint_1_passed_next_lit,
   04_missed_checkpoint, 05_results). The screenshots are 886 x 451, the
   Editor's Game view size here, so the HUD looks cramped and the panels overlap.
9. This report: DONE.

Not measured: frame rate with the markers (about 60 cubes, no shadows: expected
negligible, but Round 3 was close to the line at 56 fps 99th percentile); the
HUD at 1080p; real key presses and a pad; a bump during a race.

New tools: `make_route.py`, `RaceCheck` / `RaceCheckRunner`, `RaceRoute`,
`RaceController`, `RaceMarkers`.

Round 6 status — built and scripted-checked (2026-10-08); NOT yet driven by Dan.
Scripted: `RaceCheck.Run -raceCheck round6` 67 of 67 passed, `-raceCheck full` (forward, the real car, 3
laps with a lap by a TEST shortcut) 23 of 23, `-raceCheck lap -raceDirection reverse` 8 of 8. Reports and
screenshots: `Data/screenshots/round6/checks/`. I ran them in a copy of the project (the Unity Editor had
this one open), so the scene in the working folder is stale: the race itself does not need Chastain Rush >
Build Map (the Editor reads the route file), but run it before the next drive anyway. Items done in order;
item 3 was reported in chat before any rule changed.

1. Gate edge bug: FOUND BY READING THE CODE, not reproduced. Round 5 gates were 9 m (finish) and 11 m
   (checkpoints) either side of the route centreline: 18 m and 22 m wide across an 8 m road (5 m and 7 m of
   verge beyond each road edge). The test used one point, the car's body centre (`car.transform.position`),
   on the segment from the last physics step to this one, and the post line was the limit. A car whose
   centre was 9 to 10 m out (body half over the post) was not counted. The finish gate sits 22 m past the
   Chastain Trail junction, where cars arrive at an angle. The height window was 4 m below to 10 m above
   the road surface at the gate. Angle was not the cause: a path crossing the plane counted at any angle.
2. Forgiving gates: DONE. Finish half width 9 -> 16 m (32 m across), checkpoints 11 -> 14 m (28 m
   across); height window 4 below / 10 above -> 6 below / 25 above (a jump over the line counts). The car's
   own half extent across the gate (0.9 to 2.0 m, from its collider, turned to the gate) is added at the
   moment of crossing, so a car touching a post counts. The arches and beams are drawn at the counted half
   width (`RaceMarkers` already used the gate's half width). No other part of the lap or lead-in crosses
   any widened plane (checked by script over the route data).
3. Racer's rules read: DONE, reported in chat first. Penalty: 5 s per missed gate (`RaceProgress.Miss`),
   the lap still counts, added to the lap and the total, resolved when a later gate or the finish is
   crossed. Shortcut: a `WoodlandRoute` (path, entryRoad, exitRoad, halfWidth, bypassedGates); crossing
   its entry plane in the path direction, near the expected gate, grants the bypassed gates; credit is
   final at entry. Blocked roads: no general mechanism, authored barriers per course switched on for the
   race (`BackyardReverseBarriers`) and `CircuitBoundary` for closed corridors.
4. Rules: DONE. A missed checkpoint adds 5 s (`RaceController.MissPenaltySeconds`, Racer's value) to its
   lap and to the total; the lap counts; "this lap will not count" and the re-run lap are gone. HUD: a
   penalty line in the timer panel (orange once there is one), a message when it is given ("Missed
   checkpoint 2  +5 s"), and on the finish panel the penalties line, the total as clock + penalties and
   each lap's penalty with what was missed. Best lap and best total include penalties (as Racer's
   `LastLap` does). NOT ported: Racer's extra +5 s for missing the finish gate (a plain, very wide plane
   here) and its "teleport voids the lap" rule (a reset here is simply not a crossing).
5. Official shortcuts in the route file: DONE. `official_shortcuts` in `Data/routes/*.json` (format in
   `RouteShortcutJson`): `id`, `name`, `direction` (forward / reverse / both), `path` (x, z list, entry to
   exit), `half_width_m`, `entry_inset_m`, `entry_margin_m`, `forgives` (checkpoint ids). Crossing the
   entry plane forwards within its width forgives the checkpoints from the first forgiven one to the last;
   any earlier checkpoint still waiting is missed (penalty); entering with nothing left to forgive does
   nothing. An exit plane ends the "on the shortcut" state. `make_route.py` keeps the hand-made list
   across re-runs. The file has none: the proof was one TEST shortcut on Chasefield Way, defined in
   `Editor/RaceCheck.cs` only (forgives CP1 to CP3).
6. Blocked roads: DONE. `RaceBarriers`: red and white blocks (road width + 6 m, 1.5 m high, with colliders
   and tall end posts) across every road that leaves the course, built when a race starts and removed when
   it ends. The closures are route data (`closures`, from `make_route.py`): every road meeting the lap or
   the lead-in, followed until it is 12 m from the course; a barrier is skipped where an official
   shortcut starts or ends. 9 barriers: Courtfield Trace (lap 71 m), Glenbarrett Lane (148), Chasefield
   Way (201 and 643: the two road shortcuts are closed), the Chastain Glen Lane branch at CP4 (732),
   Chastain Place (969), the 73 m rest of the long Chastain Glen Lane piece towards Courtfield Trace
   (1055), and two across the entrance lanes 12 m behind the grid. Not barred (listed in the route
   file): three dead-end stubs of 6 to 10 m. The car can still go around a barrier through yards
   (nothing closes yards).
7. Shortcut candidates: DONE, report only. `Data/screenshots/round6/shortcut_candidates.{png,md,json}`
   and `shortcut_candidate_profiles.png`, made by `Tools/mapgen/shortcut_candidates.py`. Seven, not
   eight: the lap doubles back near itself in only a few places and the neighbourhood has almost no woods
   (577 of 56,214 trees are within 100 m of the lap), so there is no woods cut. Biggest drops: #3 (21.9 m
   down, 8 m launch drop, two houses) and #2 (16.0 m, 7.4 m, two houses); #4 is the over-a-house jump.
   All are straight lines, so houses and distances are for the straight line. The .md has each one.
8. Scripted check: DONE (above). Per gate, both edges and 45 degrees from each edge pass; a crossing 3.5 m
   outside the post, a backwards crossing and one far outside a shortcut's entry width do not. Missed
   checkpoint: +5 s, the lap counts, lap and total include it (8 missed in one 3-lap race: 40 s). Test
   shortcut: forgives CP1 to CP3, no penalty; the control (no entry) costs 15 s. Physical: the real car
   drives the Chasefield Way lap (CP1 passed, shortcut entered, CP2 and CP3 forgiven, lap 1:09 against
   1:22, no penalty). Barrier: the real car at 14 m/s stopped 2.2 m short of the Courtfield Trace barrier
   and drove straight through the same spot once the race was left.
9. This report: DONE.

Not done / not measured: real key presses and a pad; Dan's drive; the HUD at 1080p (screenshots are
886 x 451 and the panels overlap, as in Round 5); frame rate with the barrier blocks; how a barrier
looks on a steep verge; the reverse race with barriers (same closure list, only forward was driven).

New tools: `RaceBarriers`, `Round6CheckRunner`, `shortcut_candidates.py`; `RaceGate.Crossed` takes the
car's reach; `make_route.py` writes `closures` and keeps `official_shortcuts`.

Round 7 status — built, scripted-checked, and DRIVEN by Dan (2026-10-09). His
feedback: he has no fence in his own backyard (the neighbour to the right and
the one behind, on top of the hill, do); the goose sound is bad and he will
supply one from ElevenLabs; he wants the two recommended shortcuts built and
an executable he can just launch. All four are Round 8. Original note:
built and scripted-checked (2026-10-09). Work was done and checked in a mirrored copy of
the project (`C:\Users\danmo\chastain_rush_r7`) because the Editor had this folder open, so the scene in the working folder is stale:
run Chastain Rush > Build Map before driving. Screenshots (19) and every check report: `Data/screenshots/round7/` (+ `checks/`).

1. Area: DONE (`Tools/mapgen/neighborhood_area.py` -> `Data/neighborhood/chastain_glen_area.json`). Streets reached from the entrance
   without going out on New Chastain Road: Chastain Glen Lane (11 pieces, 1463 m), Chasefield Way (236 m), Chastain Trail (192 m),
   Glenbarrett Court (227 m), Courtfield Trace (173 m), Glenbarrett Lane (90 m), Chasefield Court (78 m), Chastain Place (25 m), and
   one unnamed 11 m stub (the service crossover through the entrance island). 20 road pieces, 172 houses (+1 shed). The pond is OSM
   water index 8, UNNAMED in the data (10,682 m2, 18 m from the lane, just west of the entrance spur); West and East Chastain Lake are
   about 2 km west and are not in the area. The photos show this unnamed pond (fountain, white rail fence along the road, culvert,
   docks and walls on the back-yard shore). The OSM fountain node is inside its outline. The pool, parking lot and playground are in the
   data too (`Tools/mapgen/neighborhood_extras.py` -> `extras.json`). The owner's house is NOT in the committed area file (privacy): the
   builder reads it from the git-ignored `local/owner_marker.json`.
2. Dan's house: DONE, hand-built. Racer's method (read in `CR015Neighborhood`, `DiscoveryDriveway`): a named, hand-placed object built
   by Editor code outside the rebuilt world. Here: the mesh is `Assets/HandMade/DanHouse/DanHouse.asset` (committed; the map builder
   only instantiates it, so regenerating cannot touch it), placed from `local/owner_marker.json`, named `DanHouse`, with the orange beacon
   kept to find it. Red brick front, front gable over a two-car garage with white panelled doors, one window in the gable with dark
   shutters, white trim, entry gable with a fan window, pedimented door with sidelights, wall lanterns, light siding on the upper side wall,
   grey shingle roof, 14.3 x 10.6 m from the OSM footprint (true height, not vertical_scale). Dark asphalt drive, a large maple in the front
   yard, the real hill behind (5.1 m of relief) under pine straw with five planted trees, a small stained deck fudged from the back photos.
   Sides and back plain (his word). ESTIMATES: roof pitch, window sizes and positions are read off the photos by eye, not measured.
3. House styles: DONE. 172 houses by footprint size and shape and a stable hash of the building seed: A 81 (brick-front about half, the
   rest siding), B 9, C 55, D 27. Windows, door, garage doors, trim and shutters on the street side (and one window on A's side wall);
   garage on the side nearest the street, front toward the road. Houses are true height (the rest of the map's buildings still get
   vertical_scale). Colours from the photos: white, cream, grey, sage, light blue, greige, yellow (D), two reds of brick, grey / charcoal /
   brown roofs, dark shutters, white garage doors. One box collider per house.
4. Streets: DONE. 3834 m of concrete curb (18 cm wide, 12 cm proud, NO collider), 3878 m of sidewalk with a 1.25 m grass strip (along
   houses only), 172 drives (28% asphalt, the rest concrete with seams; Dan's asphalt), 146 mailboxes (26 houses had no clear spot),
   49 lamps, 21 hydrants. Nothing but the houses and the fence triggers has a collider. Proof: BumpScan (item 11).
5. Fences: DONE, breakable. 104 runs, 2096 panels: wooden privacy fences on about 60% of lots (rear, and about half the sides),
   low picket on a few pond lots, a white three-rail fence along the road on the pond side, a black iron fence round the pool.
   There is no fence data (the OSM fence nodes in the crop are outside the area): placement is plausible, not real. Racer HAS a
   breakable fence (`BreakableProp`: a trigger per prop that flies the visual away with no impulse on the car, no speed cost, restored by
   `RestoreRace`); the same idea is re-implemented here (nothing copied) as `FenceChunk`/`FencePanel`: one mesh per 128 m cell (one draw
   call), a trigger box per panel, the panel's pieces (boards, post) fly off separately, and a break costs 3.5% of horizontal speed.
   Scripted with the real car at 15 m/s (`FenceCheck.Run`): a wooden panel broke, speed 15.1 -> 14.4 m/s (the same line with the panel
   gone: 14.9) and back to 15 m/s, tilt 9.7 deg (control 9.7), never stopped, no launch (peak vertical speed below the control's); picket: 4
   panels broke, 15.2 -> 13.7 m/s (10%), recovered; 11 broken panels were whole again after `RaceController.StartRace`. The rail fence was
   driven through in the first pass (3% cost) but the stricter second pass found no level, open rail panel, so it has no control run.
   The first wooden-fence test stopped the car: a house stood right behind that panel (the test, not the fence); the test now requires
   14 m of open ground behind the panel.
6. Entrance: DONE with one change from the photo. The island is a 55 m lens, but only 1.3 m wide at its widest in this data (the two
   one-way carriageways are 8 m each here), so the planting is scaled to it: a tall maple at the widest point, three pairs of trimmed
   round shrubs, mulch, the street curb round it, the two no-parking / no-U-turn plates on posts at its ends (drawn from code, my own
   texture), and the white rail fence along the pond side. The brick "Chastain Glen" monument sign in the photos is NOT built (no
   lettering without a font; deferred). The lighter grey patch in `01_spawn.png`: it is OSM service way 1161774881, 10 m long, a crossover
   between the two carriageways at the island's south tip, drawn in the lighter service-road shade (0.30 grey against 0.18) over the
   carriageway edge. Fixed: service ways inside the area are drawn in the street's asphalt. The crossover itself is data and stays.
7. Pond: DONE where the data allows. Fountain at the OSM node (two particle systems: 700 jets/s and a mist skirt, built in Awake; the
   project needed Unity's built-in Particle System module, added to `Packages/manifest.json`), darker green-brown water, 6 weeping trees
   (custom mesh), 2 retaining walls (the brief says 2 or 3), 2 docks, the white rail fence on the road side, 2 pine-straw patches, and a
   culvert headwall with two pipes at the shore nearest the street. ESTIMATES: the data has no docks, walls or culvert; they are placed
   on the shoreline by the rules in `MapBuilder.Pond.cs`, and there is no stream near the pond in the data, so the headwall drains to
   nothing. The banks are the terrain grass.
8. Geese: DONE (needs Dan's eyes). 14 geese (zones for 9 and 5) and one heron live at the pond: bank, road beside it, water. A car at
   speed scatters those within about 6 m + 0.3 m per m/s of speed (9 m at 10 m/s): a short run with flapping, a honk, then flight to
   water or the far bank; they come home once the car has been 38 m away for 6 s. A goose the car reaches costs 2% of speed (once per goose
   per 3 s, 5% a frame at most). No colliders at all: they cannot stop, flip or launch the car. Scripted (`Round7Check`): a car at 11 m/s
   up the lane: 3 of 15 birds scattered, speed cost 2% for one hit in one run and 0 hits in the last (it depends on the line). The honk
   is synthesized in code (as Racer's `TurkeyVoice` is; Racer has no goose sound and its wildlife wavs are deer, bats, birds, frogs and
   squirrels): its sound was NOT heard (batch runs have no audio). The return-home behaviour is coded but not scripted-checked.
9. Pool and clubhouse: DONE (simple). The map data has the pool (OSM way, 8 points), its parking lot and a playground inside the area
   (not carried in map.json; `extras.json`). Built: aqua pool with pale coping on a level deck with lounge chairs and red umbrellas, the
   black iron fence (breakable, white posts), the parking lot, a playground with a swing set. The clubhouse has no footprint in the data:
   a small peach building with a four-column front-gable porch, placed by estimate on the side of the deck away from the pond, with a
   box collider. One blurry photo and one listing photo (`amenities/`) were the only reference.
10. Performance: NOT MET at the 99th percentile; averages are above 60. Offscreen 1080p (Round 3's method) at the entrance, beside the
   pond and outside Dan's house: first run 67 / 68 / 67 fps average, 99th percentile 53 / 52 / 53 fps, 1.6 / 1.6 / 1.3 M triangles,
   about 1,300 draw calls (Round 3: 70 fps, 56 fps at the 99th). What it took: house doors, windows and trim in a no-shadow mesh,
   no shadows from curbs, lamps, mailboxes, shrubs, picket, rail and iron fences, URP shadow distance 150 -> 110 m. After: 66-70 fps
   average, 99th percentile 52-54 fps in the best repeat and worse in later ones (the machine has the Editor and other Unity processes
   running; the same scene's 99th percentile varied 42-55). Where the time goes: the trees (no trees: 99-109 fps, 0.5-0.8 M triangles),
   and shadows about 8 fps. Tree near distance 55 -> 40 m gave 70 / 72 / 70 fps and 55-56 at the 99th; not adopted (Round 3 tuned the
   trees). Next lever if Dan sees stutter: tree draw distance or near distance, or fewer tree draw calls. Details: `checks/perf_*.txt`.
11. Nothing driving-related regressed: DONE. `BumpScan` over the whole map: identical to Round 4 (outside the I-575 zone: COLLIDER 5, HUMP
   76, MISS 4, STEP 149, serious STEP 1; area subset: the same 4 STEP rows, the largest 4.2 cm). `RaceCheck`: lap forward 8 of 8 (lap
   1:22.676, as Round 5/6), full 23 of 23, round6 67 of 67, reverse lap 8 of 8 (1:22.607).
12. Screenshots: DONE, 19 in `Data/screenshots/round7/`: entrance island (from the road and from the north end), Dan's house (street,
   drive, backyard hill), one of each style (A brick, A siding, B, C, D), the pond with the fountain (wide and close), a street with
   sidewalks, the geese calm and scattering. From the street at about the photos' angles, 1920 x 1080.
13. This report: DONE.

Not done / not measured: Dan's drive; real key presses and audio (the honk); the geese's walk home; the brick entrance monument; the
fence check's rail control run; frame rate in the Editor Game view. Dan's house sides and back are plain by design.

New tools: `Tools/mapgen/neighborhood_area.py`, `neighborhood_extras.py`; Editor `MapBuilder.*.cs` (partial class), `Round7Check`,
`FenceCheck`; runtime `Assets/Scripts/Neighborhood/` (`BreakableFence`, `Geese`, `FountainSpray`); `PerfCheck -perfRound7`,
`-perfShadowDist`.

Round 8 status — built, scripted-checked, and DRIVEN by Dan from the exe
(2026-10-09; his best lap 0:43.608). He filed debug report
2026-10-09_14-41-20-911_46947e (8 entries) and sent backyard photos; all of it
is Round 9. Original note: built and scripted-checked (2026-10-09). Everything was run in this working folder
(the Editor was not open). Exe: `Builds/ChastainRush/ChastainRush.exe` (208 MB folder, 206.9 MB of game data; built 2026-10-09 14:10), started by `Play Chastain Rush.bat`.
Screenshots and check reports: `Data/screenshots/round8/` (+ `checks/`).

1. Executable: DONE. `Chastain Rush > Build Executable` / `ChastainRush.PlayerBuild.BuildFromCommandLine` runs Build Map and then
   builds `Builds/ChastainRush/ChastainRush.exe` (git-ignored, with `BUILD_INFO.txt`). Ported from Racer's `SoloReleaseBuild`
   (development, script debugging and profiler off; `BuildPipeline.BuildPlayer`; a report beside the exe); Racer's launcher, signing,
   version numbers and licence copying were not brought over. `Play Chastain Rush.bat` (repo root) is Racer's `Play-Racer.cmd`
   pointed at the exe. Fullscreen borderless at the desktop resolution, Esc twice quits (the first press shows "Press Esc again to
   quit"; ignored while the debug menu or a bug comment is open), F2 shows frames per second, the 1% low over 10 s and frame time
   (`Assets/Scripts/GameShell.cs`, new). Run-time files: the route is embedded in the scene (the Editor still reads `Data/routes`);
   race records, debug reports, the bump log and `Player.log` go to `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\`
   (`race_records.json`, `DebugReports\`, `bump_log.txt`); Dan's house, the geese and the fences are in the scene.
   The exe was run with `-exeCheck` (new `Checks/ExeCheckRunner.cs`: only with that argument), fullscreen at 3840 x 2160 (Dan's desktop resolution on this PC), GTX 1660 Ti, i7-9700, vSync on (so 60 fps is the ceiling):
   9 of 9 checks passed (`Data/screenshots/round8/exe/exe_check.txt`, screenshots beside it). It STARTS (after one fix below),
   loads the route, Dan's house, the geese (1 flock) and the fences (25 chunks), and a 3-lap race started with the T-key call ran to
   the finish: laps 1:22.675, 1:22.617, 1:22.645, no penalties; records went to the check's own file (Dan's are untouched).
   Frame rate, parked 10 s: entrance 59 fps average, median 60, 99th percentile 30 (33 ms frames); beside the pond 60 / 60 / 60;
   outside Dan's house 59 / 60 / 30. Driving lap 1 at 15 m/s: 60 fps average and median, 99th percentile 60, one 351 ms hitch in the
   lap; 60 / 60 / 60 within 60 m of the pond and of Dan's house. So it holds vSync's 60 at 4K with occasional single dropped frames
   (a 33 ms frame is one missed vSync). Fix found by running it: the first exe crashed on load ("level0 is corrupted"): the fence
   panels' and the geese's scripts lived in files of another name (`BreakableFence.cs`, `Geese.cs`), which the Editor tolerates and a
   player build silently drops. Split / renamed to `FenceChunk.cs`, `FencePanel.cs`, `GooseFlock.cs`; Build Executable now fails if
   a build reports a missing script. The exe pauses when it loses focus (runInBackground off, normal for a game); the check turns
   that off for itself.
2. Dan's back yard: DONE. Dan's lot has no fence (also without the exceptions file). Fixed exceptions to the hashed placement live in
   `local/owner_fences.json` (git-ignored, with the owner data; lots by position, so no house numbers here): the neighbour on the
   RIGHT of Dan's house seen from the street now has a privacy fence round its back and both sides (so the side line between it and
   Dan is fenced), and so does the lot BEHIND him at the top of the hill. My reading of "behind, on top of the hill": the highest lot
   behind his back yard, on the street above (about 53 m back, slightly to the left of straight behind), not the lower lot behind his
   right-hand neighbour; easy to switch in the local file if I picked the wrong one. The neighbour on the left keeps whatever the hash
   gives it. Screenshots 10 and 11.
3. Goose sound: DONE (silent until clips are added). The synthesized honk is gone. Build Map loads every
   `Assets/Audio/Geese/goose_honk*.wav|ogg|mp3` into the flock (`GooseFlock.honkClips`, so the clips go into the exe); each honk picks
   one at random, pitch 0.93-1.07, at most 3 playing at once (a fourth is skipped, not cut off). The folder has a README. It is empty,
   so the geese are silent; after dropping clips in, run Build Executable again. Not heard (no clips; batch runs have no audio).
4. Two official shortcuts: DONE, scripted only. Data: `Tools/mapgen/make_shortcuts.py` (hand-made design constants, everything else from
   the terrain and houses; re-run after mapgen) writes `Data/shortcuts/chastain_glen_loop.json` (what the builder builds) and the two
   `official_shortcuts` of the route file. Builder: `Assets/Editor/MapBuilder.Shortcuts.cs`. Plan of both: `shortcut_layout.png`.
   **#2 "Two-house drop"** (forgives CP2). a. The line: off the lane 176 m after the gate (just past CP1, so CP1 is still crossed),
   21 deg left on to a 25 m drive-up ramp (the house sits on the crest, its floor 5.8 m above the ramp's foot), THROUGH house 1763,
   level to the natural lip 10.5 m behind it, down a built landing ramp INTO house 1627 (both houses are on one straight line, so the
   jump lines up with the second door), out of its back, one straight 80 m line through the 6.9 m gap between 1615 and 1459, over the
   crest and between 1602 and 1919 (2.3-3.3 m to the nearest corners), and an arc of radius 13 m (101 deg left) back on to the lane
   at 544 m. Changed from the candidate: it leaves at 176 m, not 204 (the straight line through both houses meets the lane there); the
   second half is not the straight cut (that ran into 1459) but the gap line; it rejoins at 544 m, not 528. Line 249 m against 376 m
   of lap between the same points. b. Both houses opened: a 7.2 m wide passage cut through the house mesh (walls, windows and garage
   doors clipped, triangle by triangle), 3.7 m high in 1763 and 4.6 m in 1627 (a car that flies long off the lip arrives at 1627's
   door in the air), with a floor, side walls, a ceiling with a light, baseboards and a picture on each wall; the single box collider
   is replaced by 0.5 m boxes either side of the passage and one over it. The lip is the real one (8.7 m of fall in 18 m); the
   landing is a concave ramp from 2 m past the lip into 1627's door, shaped from the flight paths (nose-down landings, pitch -7 to -9
   deg). Flights 22-24 m, 1.1-1.2 s at 22-36 m/s; not the huge jump hoped for, because 1627 stands 31 m past the lip.
   **#6 "Rooftop leap"** (forgives CP5). a. Off the lane 729 m after the gate, 25 deg left on to a big kicker in 1641's front yard
   (18 m long: a 3 m easement, a 21 m-radius curve, then straight at 33 deg to a lip 5.6 m up, 2.5 m before the wall), OVER house 1641
   diagonally (17 m across, ridge about 9.4 m), on to a landing deck whose front edge runs along 1641's back wall (4.6 m up, under the
   eaves, so a car that comes off the back of the roof lands on it) and which runs down to the ground 54 m from the house's middle; then
   a bend right through the 11 m gap between 713 and 1052 and one arc (radius 14 m, 159 deg left) on to the lane at 996 m. Jumped:
   1641 (the one by the lane, so the run-up is the lane and the landing is in the open back yards). Driven round: 713, passed on its
   west side, 4.2 m clear. Changed from the candidate: the line is turned 25 deg so the kicker misses 1499 (1.2 m) and the deck misses
   1622 (1.0 m); it rejoins at 996 m, not 987, through the 713/1052 gap. Lowest speed that clears the roof: 18 m/s arriving at the
   kicker (1.4 m between the car's underside and the roof at 18-36 m/s). Below that (12-16 m/s) the car rolls over the ridge on its
   wheels, comes down the roof on to the deck and drives on; it never stopped or got wedged. 1641 has no box collider now: its own mesh
   is its collider (the box stood 0.25 m above the eaves). Flights 40-49 m, 2.5-2.8 s, peak 6 m above the lip at 20-36 m/s.
   Both: c./d. Trees, lamps, hydrants, mailboxes and front-yard shrubs are kept 6.5 m off the off-road parts of both lines (14 trees
   went); fences on the lines stay and break. Barriers: none of the nine stands in a shortcut's way (Chasefield Way's B3 is 12.2 m
   from #2's line and stays); the rule that left a barrier out whenever a shortcut started within 16 m of its junction was replaced by
   "out only if the line passes within 3 m of it" (the old rule opened Chasefield Way next to #2). e. Markers (Racer marks its
   shortcuts with a dirt trail painted into the ground and no arch, BackyardForwardAuthoring): the same dirt trail along both lines,
   plus this game's checkpoint look in blue: a blue arch with two 80 m blue beams over each entry (beams only while the shortcut can
   still be used this lap), and a blue chevron sign on the verge 6 m before each turn-off pointing the way (screenshots 01, 07).
   f. In the route file with `forgives` [2] and [5]; entry planes at 1763's door and at #6's lip (a first plane at the kicker's foot
   reached back across the lane and fired from the road: caught by the race lap check and fixed). g. Scripted with the real car
   (`ShortcutCheck.Run`, `checks/shortcut_check.txt`): every speed 12-36 m/s, against the road from the same start at the same speed.
   #2: clean at all 12 speeds, no bad landing; faster than the road at 12-33 m/s (25.7 against 36.0 s at 12; 15.6 against 18.4 s at
   24; 15.1 against 15.8 at 30), level at 36. #6: clean at 18-24 and 28-36 m/s, faster than the road at every clean speed (13.8 against
   17.3 s at 18; 12.7 against 13.7 at 24; 12.3 against 12.8 at 30); at 12-16 and once at 26 (a different line by the script driver)
   the car rides over the roof and is slower or barely faster; no bad landing (worst tilt 31 deg, rolls never). A one-lap race through
   both: 1:08.8 against 1:11.5 on the road at the same driving speed, CP2 and CP5 forgiven, no penalty. What the script cannot judge:
   whether the lines are findable and fun at speed with a pad, and how the jumps feel. The script driver brakes for bends at 14 m/s2
   of grip; a human can take #2's last arc faster.
5. Re-run against Round 7: DONE. BumpScan: identical outside the I-575 zone (COLLIDER 5, HUMP 76, MISS 4, STEP 149, serious STEP 1);
   only the collider list changed (the new ramps and decks). FenceCheck: the same wooden privacy panel as Round 7, 8% speed cost (two
   panels broke; Round 7: one, 5%), control 2%; picket 3% (a different picket panel: the fence list changed); the car kept moving,
   no launch, panels restored at race start. RaceCheck (run in batch mode: the windowed
   Editor did not advance while it was not focused): lap 8/8, 1:22.675 (Round 7: 1:22.676); reverse lap 8/8, 1:22.607 (same);
   round6 67/67; full 19 of 21, laps 1:22.676, 1:09.256 (TEST shortcut), 1:22.751, identical to Round 7; the two failures are "debug
   HUD present" and "bump logger present", which never start in batch mode (Round 7 ran this one windowed). Two Round 6 checks
   failed until fixed: the Round 6 check took Shortcuts[0] as its test shortcut (now found by id), and my new barrier rule measured
   from path points only, so B4 stayed across the TEST shortcut (now measured along the line). Also fixed while re-running: a
   ramp corner of each shortcut stood on the lane (#6's kicker 1 m from the lane centre, #2's ramp 3.4 m) and stopped a car driving
   the plain lap; every ramp is now kept 5 m from the lane centre, #2's ramp starts rising only past that cut and #6's cut edge has a
   sloped skirt. FenceCheck picked a different panel each run (some starts were inside a house): now a stable pick, and the same
   privacy panel as Round 7.
6. Executable rebuilt last: DONE. `Builds/ChastainRush/ChastainRush.exe`, 208 MB folder, 206.9 MB of game data; built 2026-10-09 14:10.
7. This report: DONE.

Not done / not measured: Dan's drive; real key presses and a pad; any sound (no clips yet); how the shortcuts look to a player at speed
(screenshots only); frame rate on Dan's own machine (the exe check ran on this PC).

New tools: `make_shortcuts.py`, `MapBuilder.Shortcuts.cs`, `PlayerBuild`, `GameShell`, `ExeCheckRunner`, `ShortcutCheck` /
`ShortcutCheckRunner`, `Round8Check` / `Round8ShotsRunner`; `RaceBarriers` skip rule; `RaceMarkers` shortcut arches and signs.

Round 9 status — built, checked in the exe, and DRIVEN by Dan from the exe
(2026-10-09). He filed debug report 2026-10-09_18-04-36-369_c80a61 (2 entries)
and answered item 1: neither (a) nor (b); see Round 10. Original note: built
and checked in the exe (2026-10-09). Each change was looked at in
the built exe (3840 x 2160) from Dan's own report position with the new `ChastainRush.exe -exeShots <list.json> <dir>`
(Checks/ExeShotsRunner.cs: car at the given place and heading, chase camera, screenshot; or a straight drive with what it
broke). Shots of the public places: `Data/screenshots/round9/` (the shots at Dan's house, his yard and his street's mailboxes are
kept out of the repo: they locate his house).

1. BUG-006, shortcut #2 big ramp: NOT DONE, stopped as the item says (the flight cannot be made cleanly). Numbers: the lane
   crosses the jump line about 31 m before house 1763's middle, so the 5 m lane-clear rule leaves 12.5 m of run-up (ramp foot
   23 m before 1763's middle at 311.2 m, lip 10.5 m before it, just short of its front corner). 1763 sits on the crest: its
   ridge is about 324 m, 13 m above the ramp's foot and 10.5 m past the lip; 1627's roof is about 75 m past the lip. With
   the car's real gravity and top speed (38 m/s), the only ramp that fits and clears 1763 is 9.5 m tall, curling up to 54
   degrees on an 8 m radius, met at 30.5 m/s or more; the car would peak about 25 m above the lip (346 m) and fall about
   39 m to the ground past 1627. No ramp of 45 degrees or less clears 1763's roof at any speed from that run-up. Starting
   the run-up on the lane itself (blocking the plain lap) still needs 42 degrees, 11 m tall, 30 m/s, a 30 m fall. Speeds of
   30 to 38 m/s also spread the landings over about 60 m, into houses 1459 / 1615 at the gap. Nearest versions that work, for
   Dan to choose: (a) keep the drive-through of 1763 and replace only the second half with a kicker at the natural lip behind
   it that jumps house 1627 (lip 314.4 m, 1627's roof about 316 m 31 m away over 7 m of fall: a 2 to 3 m kicker clears it at
   about 22 to 26 m/s, landing on a ramp behind 1627 like #6's deck); (b) put the big two-house jump where the lane leaves
   room for a 25 m+ run-up (a different pair of houses). Shortcut #2 is unchanged from Round 8 until Dan decides.
2. BUG-005 buried house: DONE. House 1520 (ranch, Chasefield Way) was buried 3.7 m on its uphill side. Its lot is cut down
   in the terrain itself (Build Map, MapBuilder.Round9.cs CutLot): a level pad 0.12 m under the floor over the house and
   1.4 m round it, then a bank at about 40 degrees back up to the natural ground (cut only, never filled; 940 terrain samples,
   up to 3.8 m). The walls, garage door and front door now stand clear (buried -0.12 m after); the shrubs, drive, mailbox
   and trees sit on the new ground because they are built after it. The bank's uphill edge reaches toward #2's ramp up to
   1763 (its sides go down to the new ground). List of the others, unchanged: `Data/screenshots/round9/buried_houses.txt`
   and `.png` (Tools/mapgen/buried_houses_plot.py): 6 houses buried 0.50 to 0.65 m (1630, 1719, 1557, 806, 860, 1657).
3. Dan's back and yard: DONE. Light grey siding (sides and back); the left part (seen from the yard) steps out 1.2 m with
   its own roof; a siding-clad chimney beside it up through the eave; two windows up and a row of three plus one low on the
   stepped part; a tall window over a lower one, the triple window upstairs, a two-panel glass door, a pair of windows low
   on the right, a lantern; a low grey-brown deck across the right half with blue-grey railings, an opening and two steps.
   The red brick sill band is now on the front only. Yard: mown grass (the terrain's grass) for the flat 7.5 m behind the
   house; pine straw from where the hill rises to the back of the lot, with the five trees on the slope. No gazebo,
   trampoline, loose items, vines; no fence on his lot. A second collider covers the stepped-out part.
4. BUG-001 gazebo and playground: DONE. The pavilion block (OSM building 2224, 9.4 x 6.4 m) is an open hip-roofed wooden
   gazebo on a concrete slab, six posts (with colliders), six picnic tables (three by two). The playground has the swings,
   a slide (platform, ladder, chute) and a climbing frame on its mulch pad.
5. BUG-002 sidewalk: DONE. 148 m from the parking lot's edge where Dan stood, down to New Chastain Road and along its north
   side to the entrance lane's verge, with three benches facing the walk in front of the pond. Trees kept off it.
6. BUG-003 pool fence: DONE. A wooden privacy fence (breakable, as the others) along New Chastain Road in front of the pool
   area (x 2776 to 2860), behind the sidewalk, with a gap where the walk comes down from the parking lot.
7. BUG-004 fence and monuments: DONE. The white rail fence runs on south along the entrance's outbound lane to the west
   monument. Two brick monuments (raised arched middle, sweeping wings, darker coping), at (2923, 770) and (2951, 772),
   facing New Chastain Road, turned 15 degrees outward, with colliders. The panel (black, gold border, white cattails,
   "CHASTAIN GLEN" in gold capitals about 0.3 m tall) is a hand-made texture (Assets/HandMade/Entrance/ChastainGlenSign.png,
   made by Tools/handmade/make_entrance_sign.py) on a one-sided quad with a lit, depth-tested, cut-out URP material: it is
   hidden by anything in front of it, sits inside the brick, and reads clearly from Dan's spot (about 20 m) and from 30 m.
8. BUG-007 tree: DONE. It was Dan's front-yard tree (placed 8.5 m out, on the grass strip); it now stands 4.3 m in front of
   the house, its mulch ring clear of the sidewalk.
9. BUG-008, all of Chastain Glen: DONE. a. Drives: every drive is now a grid (5 vertices across, a row every 0.75 m) that
   follows the ground; 60 of the 172 drives had lawn showing through the old surface. b. Trees: drives are planned before
   the trees; 11 trees from the canopy data stood on a drive and were moved sideways into the yard (0 removed); the
   sidewalk / strip / curb band was already kept clear; the one hand-placed tree on a strip was Dan's (item 8). c. Flags:
   19 of 72 mailboxes have the flag up (a stable hash), the rest down. d. Mailboxes break like the fences (trigger, pieces
   fly, whole again at race start). In the exe, driven into at 15 and 25 m/s: one broke each time, speed 2% and 4% lower,
   tilt 2 and 4 degrees, the car kept going.
10. Debug keys without Fn: DONE. [ panel, ] capture, \ menu, P frame-rate counter, beside F3 / F4 / F6 / F2 (P is ignored
    while a bug comment is being typed). The panel's hint line and the menu buttons show both.
11. Executable rebuilt last: DONE. `Builds/ChastainRush/ChastainRush.exe`.
12. This report: DONE.

For Dan to check: whether (a) or (b) of item 1 (or something else) is wanted for shortcut #2; the cut bank behind house
1520 from the street and from #2's ramp; Dan's back of house and yard against his photos; the monuments from New Chastain
Road at speed; the new keys on his keyboard; mailbox smashing with a pad.

Regression check in the final exe (`-exeCheck`, 3840 x 2160): 9 of 9 passed; 3-lap race 1:22.675 / 1:22.617 / 1:22.645
(the same as Round 8), no penalties; 60 fps average, median and 99th percentile at the entrance, the pond and Dan's house.

Not done / not measured: Dan's drive; real key presses and a pad; shortcut #6 and the Round 8 shortcut checks were not
re-run (nothing on them changed).

New tools: `Checks/ExeShotsRunner.cs` (-exeShots), `Tools/mapgen/buried_houses_plot.py`, `Tools/handmade/make_entrance_sign.py`;
Editor `MapBuilder.Round9.cs`. Imported from Racer: nothing (the sign rule follows Racer's PhysicalSign idea: lit,
depth-tested lettering on a one-sided board).

Round 10 status — built, checked in the exe, and DRIVEN by Dan from the exe
(2026-10-09; best lap 0:42.364). His verdict: "It seems to drive well." He
filed debug report 2026-10-09_21-01-01-659_c29ec3 (13 entries): Round 11. Checks: `ChastainRush.exe -exeRound10 <dir>`
(new, Checks/Round10Runner.cs), `-exeShortcut <dir>` (the Round 8 shortcut check, now also in the exe, with per-ramp speeds),
`-exeCheck`. Reports and shots: `Data/screenshots/round10/` (Dan's house hidden in all of them).

1. Houses 1763 and 1627 whole again: DONE. Both are built as ordinary style A houses (1763 loses its small upstairs porch
   gable so its upstairs front is flat). The only change: where the line crosses a wall, that piece of wall (siding or brick,
   windows, trim, exactly as built) is a breakable panel. From the street and the lane they look like every other house.
2. Shortcut #2 rebuilt: DONE, with two departures from the brief (f), below.
   a/b. A wooden ramp in 1763's front yard (17.5 m, curved foot, 23 deg, rounded top) climbs 5.6 m and goes in through the
   upstairs front windows; the car crosses a plain upstairs room and goes out through the upstairs windows of 1763's NORTH
   SIDE wall, not its back. Why: the lane runs along 1763's front 21 m out, so a ramp can start only 14 m from the wall
   (5 m from the lane) and must meet the wall at an angle a car can turn on to from the lane (about 45 deg); at that angle
   the line reaches the side wall before the back. A square-on line would need a 75 deg turn off the lane on to an even
   shorter, steeper ramp.
   c. Out of 1763 the car comes off the upstairs floor level and lands on a straight 13.8 deg landing slope down the yard
      (a level flight meets it at the same small angle at any speed); the crest behind 1763 is cut down to the slope.
   d. The slope bends 5 deg left 18 m before 1627 and ends at a two-panel sliding glass door in 1627's back wall; inside, a
      staircase (a smooth ramp drawn as treads) climbs to the upstairs and the car leaves through the upstairs front windows.
   e. Then as before: front yard, the gap between 1615 and 1459, rejoin at 544 m. The run out of 1627 is aimed 8 m into the
      gap (where it widens) instead of at a straight before it: that kept the bend before 1627's door small enough for a
      car that lands late on the slope. Blue entry marker, dirt trail, forgives CP2: unchanged.
   g. Slow: at 12-13 m/s the car climbs, drops off each upstairs edge on to the slope / front yard and drives on; never
      wedged (a car that falls short inside a house is on the ground floor between the passage walls and drives out).
   h. Every ramp is 5 m from the lane's centre or more.
   j. Ramps and speed: see item 2j below.
   i. Exe, at racing speed (30 m/s on the lane, 27 m/s at the ramp's foot): 14.46 s against 16.62 s by the road (+2.16 s);
      panels broken at 28.5 / 28.6 / 28.1 / 26.7 m/s; flights 42 m off 1763 (1.6 s), 25 m off 1627, then the old gap hop.
      Slow (13 m/s): 25.5 s against 34.9 s (+9.4 s); flights 11 m and 12 m. Speed needed: none (12 m/s works).
      The full check in the exe with a hard turn-in (`shortcut_check_exe.txt`): clean at 9 of 12 approach speeds, faster than
      the road at all 12; the 3 others still finished but touched 1627's door frame (24 m/s), the gap houses (28) or landed
      hard (30). In the Editor the same check is clean at 12 of 12 with both turn-in models.
   2j. #6 "stuck at the bottom": REPRODUCED in the exe before any change (`sc6_before_fix_exe.txt`): at 16 and 24 m/s the
      car hit the kicker 7.6-7.9 m up its curved foot and dropped from 18.4 to 7.4 m/s in one physics step. Cause: the car's
      body touches the curved ramp and PhysX reports the contact with an internal-edge normal tilted back against the travel
      (the climb itself cost nothing: with throttle the car left the lip faster than it arrived). Fix: Racer's own
      `VehicleSurfaceContacts` (copied; the contact uses the ramp triangle's real face normal). Exe after: kicker 98-100% of
      arrival speed at every speed, normal and hard turn-in. Landings: a car that reaches #6's kicker at 21 m/s+ flies 57 m
      and came down at about 40 deg on the 7 deg deck, losing 40-47%; now on any shortcut landing deck or slope the
      touchdown keeps the speed and turns it along the surface (3% cost). #2's held ramps also cancel gravity on the climb
      (its stairs lost 5-8% at 22-27 m/s). Racer has no speed assist for jumps; these two are new and arcade by design.
3. BUG-001 grass on the deck: DONE. The ground under the deck stood 7 cm above its boards; it is cut to 0.3 m under them,
   skirt boards close the open sides. Checked in the exe from Dan's position (shot not committed).
4. BUG-001 beacon: DONE, removed.
5. BUG-002 three side windows on Dan's house: DONE, the same size and height as the low row of three, continuing it round
   the corner. Checked in the exe from Dan's position (shot not committed).
6. Back and side windows on every other house: DONE (171 houses): a back door or sliding door, a row of two or three low
   windows, one or two more; upstairs a triple or pair and one or two singles (one-storey ranches: the lower row only);
   one or two windows on the side away from the garage, one upstairs. Varied by each house's hash, detail mesh (no shadows).
7. Solid trees: DONE. Racer gives every tree a static trunk box; with 56,000 trees here, a pool of 160 trunk capsules (the
   trunk's own radius and height, no crown) follows the car within 40 m (`TreeTrunks`); a refresh takes about 0.05 ms.
   Hand-placed trees and the pond's weeping trees have their own trunk colliders. 23 trunks on a road or drive anywhere on
   the map were moved 1.5 m clear of it (0 dropped); 0 trunks within 4 m of either shortcut line. Reset (CarRecovery, after
   Racer's RecoverNearest): in a race the nearest lap point facing the race direction, checked clear; otherwise the last
   stable spot unless a tree is there or within 6 m ahead, then the nearest road facing along it.
8. Lamps and hydrants: DONE, breakable like the mailboxes (46 lamps, 20 hydrants): pieces fly and come to rest on the
   ground, 3.5% speed per panel, whole again at race start.
9. Hydrant water: DONE. A 9 m column, spray, mist and a spreading wet patch, particles only, until the race restarts.
10. Exe check: DONE. Tree: approached at 20 m/s, the car stopped 2.0 m from the trunk within 1.2 s, tilt 9 deg; reversed
    9.2 m clear; reset in free roam to the nearest road clear ahead; reset in a race to the lap, 0 deg off the race
    direction. Lamp and hydrant: each broke, tilt 2-3 deg, the car kept going (19.2 m/s before, 16.1 m/s 6 m after while
    coasting; 20 m/s held with throttle); water: 1 jet, 1,804 particles after 4 s, visible from 40 m down the street.
    Frame rate beside the pond with solid trees: 60 fps average, median and 99th percentile at 3840 x 2160 (vsync cap),
    also at the entrance and Dan's house. Regression `-exeCheck`: 9 of 9, laps 1:22.675 / 1:22.617 / 1:22.645 (as Round 9).
    Race lap through both shortcuts (exe): 1:08.1 against 1:10.6 by the road, no penalty.
11. Executable rebuilt last: DONE. `Builds/ChastainRush/ChastainRush.exe` (2026-10-09 20:22).
12. This report: DONE.

For Dan to check: #2 with a pad at racing speed, especially the turn off the lane on to the ramp, the landing in 1627's
front yard and the run into the gap (the scripted hard turn-in touched something in 3 of 12 runs); how the smashed walls and
the plain rooms look; whether in through the front and out through the SIDE of 1763 is close enough; the hold on the
ramp tops and the landing carry (arcade feel); #6 from the lane at speed; Dan's deck and the three side windows; the
back windows on other houses; hitting trees, lamps and hydrants; the reset after a tree.

Not done / not measured: Dan's drive; a real pad; frame rate on Dan's machine. #6 at 12-14 m/s still rides over 1641's
roof (as in Round 8, unchanged). Unity crashed twice at startup during the round (in its file prefetch, before any project
code); re-running worked.

New: `VehicleSurfaceContacts` (copied from Racer), `RampHold`, `ShortcutSurface`, `TreeTrunks`, `CarRecovery`,
`HydrantWater`, `HydrantChunk`, `Checks/Round10Runner.cs`; GameShell `-exeShortcut`, `-exeRound10`. Edits to imported files
(commented): ArcadeVehicle (registers the contact fix), VehicleRespawn (Recover hook). Imported from Racer:
`VehicleSurfaceContacts` only (its test switch dropped, surfaces = the shortcut prisms).

Current Objective (Focus Area)

**Round 11 — Dan's third report: the #2 window jump, no dirt trails, clean
drives, roads and sidewalks, the lake walk. DECIDED (2026-10-09).** Source:
debug report `2026-10-09_21-01-01-659_c29ec3` (13 entries, in
`%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\DebugReports\`;
look at every screenshot before starting). Positions are the car's, from the
report. The standing rules in `CLAUDE.md` apply. Where Dan wrote "make sure"
or gave several examples of one fault, the item covers all of Chastain Glen
and says so; otherwise fix only the place he pointed at.

1. BUG-013 (x 3080, z 1044, the ramp into house 1763). Dan: "I think the
   concept was kind of missed I wanted the jump through a large window and
   out another one, but maybe it isn't possible?" What is there now is a
   ramp that runs right up to the wall and into an opening. What he wants is
   a JUMP: the car leaves a ramp, flies through a large window, crosses the
   room in the air or on its floor, and flies out through another large
   window. So:
   a. The ramp ends at a lip SHORT of the house, with open air between the
      lip and the wall, so the car is airborne when it reaches the glass.
   b. The way in is one LARGE window (a picture window, wide and tall enough
      to hit at speed with margin), with glass that shatters. The wall round
      it stays. With the glass unbroken it must read as a house with a big
      window, not as an opening or a carport.
   c. The way out is another large window that shatters the same way.
   d. Keep what works from Round 10: the landing slope, the sliding glass
      door and stairs of 1627, the rest of the line, `forgives` CP2, ramps
      that keep their speed, glass whole again at race start.
   e. It is possible in principle (the flight is short and nearly level).
      If the numbers say otherwise, stop and report them with the nearest
      version that is still a jump through a window.
   f. Look at it from Dan's spot in the exe before and after the glass
      breaks, and drive it once at racing speed.
2. BUG-005 (x 3053, z 1120) and BUG-007 (x 2782, z 1038). Dan: "remove the
   dirt trail here. It should look normal." and "aggain i don't want the
   dirt path. I dont think it is needed." Remove the dirt trail from BOTH
   shortcuts along their whole length: the ground there is ordinary yard
   again. ALSO remove the blue arches, the blue beams and the chevron signs
   from both shortcuts (Dan, later the same evening; see "Shortcuts are
   found, not signposted" near the top). Nothing marks a shortcut. The
   entry planes, the `forgives` lists and the barrier rule stay exactly as
   they are; only the visible markers go.
3. BUG-007, #6. Dan: "is the reception ramp really necesarry? I think it
   will be ok without it." Remove #6's landing deck: the car lands on the
   ground behind the house. The landing must still keep the car's speed and
   not stop or flip it (extend the Round 10 landing carry to that patch of
   ground). Clear anything solid from the landing area. A slow car that
   comes off the back of the roof must still be able to drive on.
4. Driveways, ALL of Chastain Glen. BUG-002 (x 3168, z 989, Glenbarrett
   Court): "Clean driveways here as well ... They shouldn't be colliding.
   Maybe make the cul-de-sacs larger to prevent this." BUG-005: "make the
   cul-de-sac larger to prevent driveways from merging into each other."
   a. Make the cul-de-sac bulbs larger (Glenbarrett Court, Chasefield Court
      and any other in the area) so each driveway reaches the bulb by
      itself.
   b. No two driveways overlap, cross or merge anywhere, and none has
      broken or shattered-looking pieces where it meets the road.
   c. No grass showing on any driveway (BUG-013's screenshot shows grass
      patches on the asphalt drive at 1763).
5. Roads and sidewalks, ALL of Chastain Glen. BUG-001 (x 3022, z 838):
   "clean the sidewalks hre." BUG-003 (x 3065, z 949): "make sure there is
   no grass in teh road nor sidewalks and the sidewalks arent broken like
   this." BUG-004 (x 3074, z 996): "another example oof what i mean."
   BUG-012 (x 2944, z 758): "Make sure grass is not in the road."
   a. No grass on any road surface or sidewalk, including junctions and the
      entrance at New Chastain Road.
   b. Sidewalks and curbs are continuous: no broken, overlapping, floating
      or torn pieces. At junctions and driveways they stop or turn the
      corner cleanly.
6. BUG-008 (x 2930, z 829, the entrance lane by the island). Dan: "these
   white lines crossing into the road should not be there." Remove the curb
   or sidewalk strips that run across the roadway there.
7. BUG-006 (x 2964, z 1193, Dan's house). Dan: "my house doesn thave a
   mailbox. should be to the right of the driveway. ALso the hydrant should
   be in my yard instead of the neighbors." Add a mailbox to the right of
   his driveway as seen from the street, and move that hydrant on to his
   side of the lot line.
8. The lake walk. BUG-009 (x 2914, z 767): "this sidewalk is in the wrong
   place. It is the neighborhood sidewalk and should be closer to the lake.
   The benches should be facing the lake and not the road." BUG-011 (x 2814,
   z 760): "Again this sidewalk shoult be closer to the lake/pool and inside
   the fence." Move the Round 9 walk off the roadside: it runs inside the
   fences, along the pool area and the lake shore, to the entrance. The
   benches face the lake.
9. BUG-010 (x 2759, z 808). Dan: "This is not a sidewalk, this is a road to
   the parking lot of the pool." The map data has a road there (Grasmere
   Court Northeast). Take the sidewalk off it and show it as the road it
   is, running into the pool parking lot.
10. BUG-012, the brick entrance signs. Dan: "These brickwalls are incorrect,
    they should be slanted the other way so that you see them when you are
    pulling into the neighborhood, not approaching it." They are turned 15
    degrees outward now. Turn each the other way, so both faces angle in
    toward a car turning into the entrance from New Chastain Road. Check
    from a car in the entrance, in the exe.
11. Rebuild the executable last and report its path.
12. Report status for every item as a short list, then "for Dan to check".

Out of scope for this round: vehicles, sound and AI drivers (Rounds 12 and
13, queued below), the six slightly buried houses, goose sounds.

**QUEUED NEXT — Round 12: the real vehicles, with sound. DECIDED
(2026-10-09).** Dan: "It would be interesting to get the actual vehicles in
with sound and some ai drivers." Not started; promote after Round 11. One
round per prompt. Outline:
* Bring over Racer's vehicles as they are: `VehicleProfile`,
  `VehicleConfiguration`, `VehicleVisual`, `VehiclePaint`, `VehicleLights`,
  `VehicleSurfaceContacts`, `VehicleContact`, the built models (from
  `Tools/Blender`; copy the generated assets, and the scripts only if a
  model must be rebuilt), and the rider (`RiderLook`, `RiderGestures`) for
  the bikes and the ATV. All player vehicles: Street Classic, tourer,
  Needle 600, Trail Four, Sundown Roadster, Highball Fastback, Pebble
  Coupe, Skyfin Cruiser, Ridge Scrambler, Drifter Twin, Turf Rocket. Police
  vehicles stay behind until police modes come over.
* Sound: `VehicleAudio` and `VehicleSoundSynthesis` (engine sound made in
  code), per vehicle, plus whatever Racer plays for tyres, landings and
  impacts.
* Handling stays Racer's for each vehicle. Check each one on this map's
  steeper roads (`vertical_scale` 1.5) and on both shortcuts; report any
  that cannot make a jump, do not retune them.
* Choosing a vehicle, DECIDED by Dan (2026-10-09): a temporary switch on
  the shoulder buttons for now (and two keys), showing the vehicle's name
  briefly. It is temporary: the garage view comes with the menus, and Dan
  said the menus "shouldn't wait too long" (Round 14).
* The chase camera, the debug panel, records (per vehicle) and the exe
  build keep working. Check with a controller in the exe.

**QUEUED AFTER THAT — Round 13: AI drivers on the Chastain Glen loop.**
Outline: `RoadDriver`, `RaceRoad`, `ShortcutStrategy`, `AiFinishEstimate`,
the start grid and positions from `RaceDirector` / `RaceProgress`, rival
names. DECIDED by Dan (2026-10-09): start with 3 AI drivers; their vehicles
are either chosen by Dan or picked at random. Until the menus exist, random
is the default, with a simple setting to fix them. Known Racer
lessons to carry: jump run-ups use `aiEntrySpeed`; the obstacle sensor must
not read a ramp as a wall; check AI against a human-like driver in the exe,
not a scripted one. The AI must cope with what this map adds: barriers,
breakable fences, mailboxes, lamps and hydrants, solid trees, geese, the
window jump and the rooftop leap.

**QUEUED AFTER THAT — Round 14: menus and the garage.** Dan: "getting the
menus over shouldn't wait too long." Racer's `RaceMenus.*`, `MenuInput`,
`MenuGlyph`, the garage view (turning model, stat bars), picking the AI
drivers' vehicles, controller first. Replaces the temporary vehicle switch
and the T / X race keys.

Next Steps (Do Not Start Yet)

* All of these are deferred, not decided against.
* **Round 7 reference notes (the round itself is the Current Objective).**
  Dan wants a round that makes his own neighborhood look good, as a start,
  before the rest of the map. Only his house has to be accurate; the others
  can be fudged from a few styles. 20 reference photos are filed (2026-10-09)
  in `local/reference_photos/` (git-ignored; never commit them): `entrance/`
  (2), `house_styles/` (3), `pond/` (9), `streets/` (2), `amenities/` (1,
  blurry), `my_house/` (3, front only). What the photos show, as a draft scope:
  * Dan's house: CONFIRMED as the red-brick style A house in `my_house/`
    (front gable over a two-car garage, red brick with white trim, light
    siding on the upper side wall, a large tree in the front yard, a dark
    asphalt driveway). Build it by hand and accurately.
  * House styles: (A) Dan's style, which recurs through the neighborhood:
    two-storey with a front gable over a two-car garage, sometimes brick
    fronted and sometimes all siding (a grey siding one is in the photos); (B)
    two-storey brick-front colonial with full-height white columns and a side
    garage wing; (C) one-storey ranch with a front-gable two-car garage (sage
    green siding; white with a brick accent); (D) plain two-storey siding box
    with a side gable (white, cream, yellow). Colours: white, cream, grey,
    sage, light blue, red brick; grey to charcoal and brown roofs; white trim
    and garage doors; dark shutters. Apply across Chastain Glen by footprint.
  * The pond: a real feature with a fountain in the middle, grass banks,
    weeping trees, a few low retaining walls and small docks, a white rail
    fence along the road side, and a culvert under the road. Geese and a
    heron live there.
  * Streets: concrete curbs, a sidewalk with a grass strip on at least one
    side, concrete driveways, mailboxes at the curb, street lamps, fire
    hydrants, utility boxes, wooden privacy fences and low picket fences.
  * Entrance: the planted island (one tall tree, trimmed round shrubs, mulch,
    a curb), a white three-rail fence, no-parking and no-U-turn signs.
  * Amenities: a pool with a black metal fence and a small clubhouse (one
    blurry photo; needs a better one and its location).
  * Geese: DECIDED (2026-10-09), yes. Geese around the pond, on the banks
    and on the water, and they often wander into the road near the pond, as
    they do in real life. They are a moving hazard to dodge on the course.
    DECIDED: they scatter when the car reaches them, with flapping and
    honking, and driving through the flock costs a little speed. They are
    not solid obstacles. A heron is optional.
  Dan on his house (2026-10-09): the sides are plain and don't matter. All
  that is in the backyard is the hill, covered with straw, with a few trees
  planted. He may bring backyard photos later for a detail pass; Round 7
  does not wait for them.
* More shortcuts for the Chastain Glen loop after Dan has driven the first
  two (Round 8): the other candidates, reverse-direction versions, and the
  out-and-back jump across the creek ravine east of the loop.
* Brick "Chastain Glen" monument sign at the entrance (not built in Round 7).
* Frame rate: Round 7 measured 66-70 fps average but 52-54 at the 99th
  percentile in the Editor. Trees are the cost. If the exe stutters, the
  lever is tree near distance (55 -> 40 m gave 55-56 at the 99th).
* If hills still read flat at a height setting that drives well: a lower chase
  camera and narrower field of view, before raising the scale further.
* Growing the map further north needs a second elevation tile (the current
  one ends at latitude 34.048).
* Proper handling for major roads: medians, ramps and overpasses. Known
  broken: the Chastain Road overpass at I-575 lies collapsed on the highway,
  and I-575 has no real ramps. Dan expects that edge of the map may not be in
  the final game, so decide whether to trim the west edge east of I-575
  before spending any work on it. Open, not urgent.
* Bump log: it counts the car as on a road up to about 5 m from the
  centreline, so off-road driving beside a road gets logged as a road bump.
  Tighten it to the road's real half-width when the log is next used.
* One world with race routes laid over it (not a copy of the world per course,
  as in Woodstock Rush).
* Houses missing from OSM, found by comparing against NAIP imagery.
* The untagged-bridge logic from Round 2 is untested on real data; check it
  the first time the map grows into an area that has such a crossing.
* Hand-built versions of other houses that matter, from photos, as was done
  for Kyle's house in Woodstock Rush. (Dan's own house is in Round 7.)
* Hand detailing of the Chastain Glen entrance. Dan confirmed from photos
  (2026-10-08) that the divided entrance with a median island is real, so the
  generated layout there is correct. Missing against the photos: trees and
  trimmed shrubs on the island, a white rail fence along one side, sidewalks
  and curbs. Dan has reference photos. Also check the lighter grey patch on
  the road beside the island in `01_spawn.png`, which looks like a glitch.
* **After Rounds 12 and 13 (vehicles, sound, AI): the rest of Racer's finished systems.**
  No longer waiting on Woodstock Rush. Candidates, to be ordered with Dan:
  rival AI, menus and garage, the vehicles (mostly motorcycles and four
  wheelers), campaign, Free Roam, split-screen, traffic, radio, publishing.
  First step of that round: a short written inventory of each Racer system,
  what it depends on, and what in it assumes the Woodstock Rush map.
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
