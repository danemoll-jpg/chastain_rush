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

**Ramps look like something that could be there — DECIDED (2026-10-10).**
Dan wants things to look more natural: a ramp is a board on hay bales, a
dirt mound, a trailer, a pile of something, not a purpose-built wooden
launch structure. Applies to every ramp from now on.

**A ramp's look must fit its real size — LESSON (2026-10-10).** Shortcut #6's
ramp was dressed as hay bales (Round 12) and as a flatbed (Round 13); Dan
rejected both, because a 5.6 m, 33 degree ramp is not the size of either.
Before dressing a ramp as an object, check that the real object is about
that tall and that steep. If it is not, say so and offer objects that are
(a dirt pile, a raised dump-truck bed) instead of stretching the object.

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

Round 11 status — built, checked in the exe, and DRIVEN by Dan (2026-10-09).
**Item 1 is REJECTED by Dan:** "my god. Why did it revert this short cut to
basically what I hated about it before?" A ground-floor window with a ramp
up to it is, once the glass breaks, the Round 8 drive-through again. He
filed debug report 2026-10-09_23-41-20-347_2f94e8 (6 entries): Round 12.
Original note: built and checked in the exe (2026-10-09). Checks: `ChastainRush.exe -exeRound11 <dir>`
(new, Checks/Round11Runner.cs), `-exeShots` at Dan's 13 report positions, `-exeCheck`. Reports and shots:
`Data/screenshots/round11/` (Dan's house hidden or out of view in all of them; the BUG-006 shot was looked at and not committed).

1. BUG-013 window jump: DONE as the nearest version, STOPPED short of an upstairs window (numbers below). The ramp now ends
   at a level lip 2.4 m before 1763's front wall (open air; the wall is at 43 deg to the line, so 0.7 to 4.1 m across the
   ramp), 0.97 m above the yard. The car flies through a 5.6 m x 2.25 m ground-floor PICTURE WINDOW (three panes, white frame,
   sill; glass 0.45 to 2.7 m above the floor), across the living room (5 m), and out through a 6.0 m picture window in the
   north side wall, on to the landing slope. Only the glass and frame break (pieces fly, whole again at race start); the wall
   round each window stays solid. With the glass whole it reads as a house with a big window (shot `r11_1763_from_dan_bug013_before`).
   1763's front door moved round the corner into the nook beside the garage (the window takes the entry front); its small
   upstairs gable is back. Why not upstairs: the lane leaves 13.75 m from the wall for the ramp; a level lip at the upstairs
   floor with air before the wall needs a 5.6 m climb in about 11 m: a ramp of 40 deg or more with a foot radius of 10 m or
   less (about 7 g at racing speed). At the ground floor it is a 3.4 m climb: curved foot 12 m, straight at 30 deg (Round 10:
   23 deg), rounded held top, 0.6 m level lip. Kept from Round 10: the landing slope (it now starts 0.25 m under the room's
   floor, 10.6 deg instead of 13.8; the crest behind 1763 is cut up to 3.2 m, with banks reaching 4.5 m), 1627's sliding door
   and stairs, the rest of the line, `forgives` CP2, held ramps, the landing carry. Exe, at racing speed (30 m/s on the
   lane, 27.8 at the ramp's foot): front window broken at 26.6 m/s, side window at 25.2, flight 31 m (1.3 s) through the
   house, finished 14.08 s against 16.52 s by the road (+2.44 s); nothing of 1763 touched. Slow (13 m/s): finished, +9.40 s.
   (A first try with the glass to 2.35 m let the car's roof clip the top of the front window at 30 m/s: it leaves the lip
   still rising about 1 m. The glass now goes to 2.7 m; the room's ceiling is 2.8 m.)
2. BUG-005, BUG-007 dirt trails and markers: DONE. No dirt trail on either shortcut (the yard is plain grass); no blue arches,
   beams or chevron signs (RaceMarkers no longer builds them). Entry planes, `forgives` and the barrier rule unchanged (#6's
   race data is identical; #2's line moved about 0.1 m with item 1, its entry plane 0.15 m).
3. BUG-007 #6 landing deck: DONE. No deck; the car lands on the ground behind 1641. That ground (from the back wall to 75 m,
   6.5 m either side of the line) is a landing zone (`LandingZone`): RampHold keeps the speed on touchdown there, as on the
   deck. Nothing solid there (trees were already kept off the line; fences there break). Exe: 27 m/s: flight 52 m, down at
   23.5 m/s, finished (-1.2 s on the road); 21 m/s: 47 m, finished; 14 m/s (comes off the back of the roof): finished,
   lowest 11.7 m/s, no roll.
4. Driveways, all of Chastain Glen: DONE. a. Every street end in the area is now a cul-de-sac bulb, 13 m radius (Glenbarrett
   Court both ends, Chasefield Court, Chastain Place), with its curb, edge band and sidewalk round it; drives end on the
   bulb's edge, each by itself. (The two short stubs off Chastain Glen Lane, 6 and 8 m, are not cul-de-sacs.) b. Overlaps
   are measured between every pair of drives: 3 drives were re-aimed (Dan's never), 0 overlaps left; drives that end on a
   bulb do not flare. c. Drives (and roads and sidewalks) are grids whose inner vertices stand on the highest ground near
   them, so no lawn comes through; no drive is drawn under a shortcut ramp (1763's drive; the #2 ramp is narrower on that side).
5. Roads and sidewalks, all of Chastain Glen: DONE. Roads in the area are grids six cells across clear of every bump (the
   grass at junctions came through two-vertex ribbons); every junction corner has a curb return (31 arcs, 6 m, smaller where
   a median or island leaves less room): asphalt fill, edge band, curb and, where the street has one, the sidewalk round the
   corner. Sidewalks: their own material, drawn over drives they cross, 9 cm up on a grid clear of the lawn; "along houses"
   is joined over gaps under 24 m and to a street's end, stretches under 10 m dropped, offsets smoothed on bends. The entrance
   at New Chastain Road: the strip of median between the entrance's two lanes is paved (BUG-012). Paint and curbs stop on
   any other road unless it continues this one.
6. BUG-008 white lines: DONE. They were the two entrance lanes' edge bands and curbs running on across each other where
   the lanes meet beyond the island (they were exempt as "the same street"); now stopped there.
7. BUG-006 mailbox and hydrant: DONE. Dan's mailbox stands just right of his drive (seen from the street); the hydrant that
   stood by his neighbour's drive (beside his own) is now in his grass strip on the other side of his drive, past the
   mailbox. Checked from his report position in the exe (shot not committed).
8. BUG-009, BUG-011 lake walk: DONE. The Round 9 walk no longer runs along New Chastain Road: from the parking lot it runs
   inside the wooden fence, south of the pool's fence and the clubhouse, along the lake shore (4.5 m off the water), and out
   to the entrance lane through a gap in the white rail fence (173 m). The wooden fence along the road is now whole (the gap
   for the old walk is gone). Three benches on the lake side of the walk, facing the lake.
9. BUG-010 road to the pool lot: DONE. The map data has it as a 3 m driveway (pale) from the end of Grasmere Court into the
   lot; it is drawn as a 6 m asphalt road with the pale edge band, into the lot.
10. BUG-012 monuments: DONE. Each turned 15 deg IN toward the entrance's middle (was 15 deg out); checked from a car
    turning in and from Dan's spot in the exe.
11. Executable rebuilt last: DONE. `Builds/ChastainRush/ChastainRush.exe` (2026-10-09 23:32). Regression `-exeCheck`: 9 of 9,
    laps 1:22.675 / 1:22.617 / 1:22.645 (as Rounds 9 and 10), 60 fps average, median and 99th percentile at the entrance, the
    pond and Dan's house (run at 1920 x 1080, not 3840 x 2160).
12. This report: DONE.

For Dan to check: the #2 window jump with a pad at racing speed, and whether a GROUND-floor window is close enough (an
upstairs one needs a 40 deg ramp); 1763's front door now in the nook beside its garage; the steeper #2 ramp (30 deg); the
deeper cut for the landing slope behind 1763 (up to 3.2 m); #6 landing on the bare ground at speed and coming off the roof
slowly; the four cul-de-sac bulbs and the curb returns at every junction; sidewalks crossing drives; the lake walk's line
round the clubhouse; the paved median strip at the entrance; his mailbox and hydrant; the monuments when pulling in.

Not done / not measured: Dan's drive; a real pad; 3840 x 2160. Not changed (seen, outside the items): the two short stubs
off Chastain Glen Lane keep their small end discs.

New: `MapBuilder.Round11.cs` (street graph, curb returns, bulbs, drive overlaps, the pool road, surfaces clear of the lawn),
`LandingZone`, `Checks/Round11Runner.cs` (-exeRound11), Editor `TopDownShots` (straight-down pictures of the built scene:
`ChastainRush.TopDownShots.Run -topDownOut <dir> -topDownRects "name:x0:z0:x1:z1;..."`, with graphics). Edits: RampHold
(landing zones), RaceMarkers (no shortcut markers), make_shortcuts.py (#2 window jump, no trails, no #6 deck). Imported from
Racer: nothing.

Round 12 status — built, checked in the exe, and DRIVEN by Dan (2026-10-10).
He accepted the new shortcut #2 (no complaint about it) and reported three
things, folded into Round 13 at his request: a tree in #2's path, he does
not like the hay bales on #6 (a flatbed instead), and he was penalised for
missed checkpoints when taking #6. Original note: built and checked in the
exe (2026-10-10). Checks: `ChastainRush.exe -exeRound12 <dir>`
(new, Checks/Round12Runner.cs), `-exeShortcut` (7 of 7), `-exeCheck` (9 of 9). Reports and shots: `Data/screenshots/round12/`
(Dan's house hidden in every shot; the BUG-002 shot was looked at and not committed).

1. House 1763 restored: DONE. Nothing of the shortcut is attached to, cut into or aimed at it: an ordinary style A house
   (front door at the front, its own windows and entry gable), one box collider, its drive whole, its side yard natural
   ground (no grading before the crest by 1539's corner). Shot `r12_1763_from_dan_bug003` (Dan's camera).
2. #2 starts past 1763: DONE. The line leaves the lane 210 m after the gate (an arc of radius 21 m, 67 deg left) round 1763's
   north-east corner into the side yard between 1763 and 1539 (2.15 m off each corner), runs 2.2 m past 1539's west corner,
   then an S (right 38 deg, left 20 deg, radius 25 m) on to one straight through 1627 aimed at the gap between 1615 and 1459.
   The S cannot come sooner: seen along that straight, 1539's west corner and 1763's north-east corner line up. Grass all
   the way, no wood on the ground: natural up to the crest, then the terrain graded (a hill shaped as the path of a car
   leaving the crest at 20 m/s, steepest 30 deg; a hollow 0.93 m under 1627's floor; a 12 deg climb into the door); cut up
   to 0.81 m and filled up to 0.62 m along the line (1.04 / 1.57 m at the banks' edges), never below a house's foundation.
   NEW, arcade, say if unwanted: the grass from the lane's edge to 1627's door is "held" like the Round 10 ramps (RampHold,
   `LandingZone.hold`): at racing speed the car otherwise left the yard's crest still in the S and flew 27 m off the line
   into 1627's wall. Entry plane at 1539's corner; `forgives` CP2 (unchanged: CP1 is passed on the lane before the
   turn-off); the barrier rule unchanged. Race check: lap by both shortcuts 1:00.631, CP2 and CP5 forgiven, no penalty.
3. The clue: DONE. One home-made sign, a weathered plywood board 1.6 x 0.8 m on two posts, "SHORTCUT" and an arrow
   pointing left, painted (capitals 0.25 m: readable at 20 to 30 m), in the grass strip on the left just past the turn-off,
   facing the oncoming driver. One-sided lit face, depth-tested (hidden by terrain like its board), no collider (3.5 m or
   more off the line). Face drawn by `Tools/handmade/make_shortcut_sign.py` (`Assets/HandMade/Shortcut/ShortcutSign.png`).
   No worn grass or tracks. Shots `r12_turnoff_sign_from_lane_11m`, `_22m`, `r12_turnoff_side_yard_from_lane`.
4. Into 1627 on an incline, big air out: DONE. A 4.6 m four-panel sliding glass door in its back wall (the line crosses
   its walls 30 deg off square: 4.0 m across the line, 1.08 m either side of the car); the 12 deg climb runs on through it
   and up the stairs (straight 8.1 m, then a kicker of radius 42 m up to 18 deg), the lip at the upstairs front wall at the
   upstairs floor (3.27 m over the front yard), out through a 4.6 x 2.8 m upstairs window (no entry gable on 1627; its two
   small upstairs front windows are where the big one is). Only glass and frames break, the wall round them stays; whole
   again at race start. Exe, racing speed (the scripted driver at 30 m/s braking for the turn-off as its curve needs): on to
   the stairs at 21.3 m/s, off the kicker at 24.1 m/s; FLIGHT 1.50 s, 35 m, up to 5.7 m over the ground, down at 24.7 m/s
   (speed kept) 36 m past 1627's front wall, the gap's narrowest point being 37 m out; touched nothing; finished 14.54 s
   against 15.52 s by the road. Slow (12 m/s): flight 12 m, 4.1 m up, finished. Roofs: 1459, 1615 and 1993 (within 12 m of
   the flight) have a convex roof collider (the house's hull: no valley, chimney pocket or eave lip); a car dropped on each
   roof, two headings, drove off on to the ground within 3.6 s every time. Hiding the open front from the door: the wall
   over the door does it for the chase camera (3.6 m up, 7.5 m back: through the door it looks down at the stairs); no
   extra ceiling was built.
5. Proof in the exe: DONE. `r12_1763_from_dan_bug003`; the turn-off and sign from the lane; `r12_1627_back_sliding_door_before`
   / `_after`; `r12_1627_front_upstairs_window_before` / `_after`; `r12_sc2_flight_side_profile_1..3`; the drives above. The
   drive is the scripted driver, not a person with a pad.
6. BUG-002 sidewalks: DONE, all of Chastain Glen. Every sidewalk (along the streets, round the corner returns and the
   cul-de-sac rings) stops exactly at a drive's drawn edge (flared mouths included) and starts again past it; the drive
   runs unbroken to the road. The lake walk was not changed (not checked against drives).
7. BUG-001 pool: DONE. The big plain building beside the pool (OSM building 841, a 11 m "detached" box) is not built; the
   small clubhouse with the columned porch stands on its footprint, porch toward the pool. Shot `r12_pool_from_dan_bug001`.
8. BUG-006 start line: DONE. The chequered gate shows only while a race runs (checkpoints and barriers already did). Shot
   `r12_no_start_gate_from_dan_bug006`.
9. #6 ramp look: DONE, a wide board on hay bales (a 5.6 m lip makes a tilted trailer unbelievable). Planks running up the
   ramp on exactly the old surface, on a stepped stack of big square bales (three across, columns 1.2 m deep, topped with a
   half bale where a full one does not fit) under and behind it. The collider is the same mesh as before; exe at 27 m/s:
   finished 13.42 s against 14.56 s, flight 52 m (as Round 11). Shots `r12_sc6_kicker_from_lane`, `_side`.
10. Executable rebuilt last: DONE, `Builds/ChastainRush/ChastainRush.exe` (2026-10-10 02:05). `-exeCheck` 9 of 9, laps 1:22.675 / 1:22.617 /
    1:22.645 (as Rounds 9 to 11), 60 fps at the three places (1920 x 1080, not 3840 x 2160).
11. This report: DONE.

For Dan to check: #2 with a pad at racing speed: the turn-off round 1763 into the side yard, the S past 1539's corner, the
held feel of the grass run down (arcade: the car does not leave the grass there), the door, the stairs and the flight out of
1627 (it lands just before the gap); whether the sign is the right kind of clue and in the right place; 1763 as an ordinary
house from the lane; the big upstairs window on 1627's front; landing on a roof beyond the gap and driving off; sidewalks
stopping at drives; the clubhouse in the big building's place; no start gate in free roam; #6's board and hay bales.

Not done / not measured: Dan's drive; a real pad; 3840 x 2160. The Round 10 and Round 11 exe checks still look for 1763's
shortcut windows and now report them missing.

New: `Checks/Round12Runner.cs` (-exeRound12), `Tools/handmade/make_shortcut_sign.py`, `Assets/HandMade/Shortcut/`. Edits:
make_shortcuts.py (#2 rebuilt: line, grade, 1627, roof houses, sign), MapBuilder.Shortcuts.cs (1627's door and window,
roof-hull houses, the sign, #6's board and bales), MapBuilder.Round9.cs (grading), MapBuilder.Houses.cs (upstairs window
reserve), MapBuilder.Streets.cs / Round11.cs (sidewalks stop at drives), MapBuilder.Neighborhood.cs / Pond.cs (clubhouse),
RaceMarkers (gate only in a race), LandingZone + RampHold (held zones). Imported from Racer: nothing.

Round 13 status — built, checked in the exe, and DRIVEN by Dan (2026-10-10).
His verdict, with debug report 2026-10-10_02-19-13-422_fb0237:
* Item 2, the flatbed: REJECTED. "how is this what we described at all?
  Doesn't look like a flatbed with a gate open. completly weird and even
  worse than previous." (BUG-002, x 2808, z 1100: a red steel deck tilted
  steeply in the air on a stack of timber, wheels hanging under it.)
* Vehicles: "The vehicles drove like ass. Especially the cycle and
  lawnmower were getting constant slow down."
* The tree (BUG-001) was from the build before; item 1 fixed it.
Original note: built and checked in the exe (2026-10-10). Checks: `ChastainRush.exe -exeRound13 <dir>`
(new, Checks/Round13Runner.cs: every vehicle), `-exeShortcut <dir> -vehicle <id>` (new switch), `-exeCheck` (9 of 9). Reports:
`Data/screenshots/round13/`.

1. Trees on the shortcut lines: DONE. No trunk within 6.5 m of either line from turn-off to rejoin (flight and landing
   included). 3 moved, none deleted, all on #2's side-yard run (4.5 to 6.5 m off the line): (3067.9, 1061.3) moved 2.4 m,
   (3060.9, 1072.7) moved 0.7 m and (3068.2, 1090.5) moved 1.8 m. The last is the one in Dan's BUG-001. #6: none.
   `trees_on_lines.txt`.
2. #6 as a flatbed: DONE, look only. One big flatbed trailer sized to the ramp (7.6 m wide): full-width steel loading ramps on
   the low 6.7 m, a 9.6 m plank deck in red steel rails with stake pockets, three main beams, a tandem axle of dual wheels where
   the beams clear the ground, crossed-timber cribbing under the raised end. Surface, curve, lip and collider unchanged; no bales.
3. Penalties: DONE. The cause was NOT #6. Dan's own exe log (Player.log) shows #6 entered and CP5 forgiven on all three laps; it
   was CP2 charged every lap, because #2's entry was never registered. #2's entry plane sits in the gap between 1763 and 1539,
   lopsided (1763's corner 6.55 m left of the line, 1539's 2.2 m right), and reaches 4 m either side. His bump log and BUG-001
   put him right of the line and 6.9 m right of it by station 100, so his line went round 1539's far side and missed the
   plane. A Round 12 regression in effect: #2's new line and entry plane. Fix, both shortcuts: a car anywhere in a shortcut's
   corridor (7 m either side of its line, where the line is 12 m or more off the lap, entry to exit) takes it, however it got
   there; the race log says which caught it ("its entry plane" / "its corridor"). Exe check with the entry planes switched off
   (`-shortcutNoEntryPlanes`), #2 driven 1.5 m right and #6 3 m left of the line: both forgiven by the corridor, no penalty.
   Items 1 to 3 committed on their own (2d24b64).
4. Vehicles: DONE. From Racer, as they are: the eleven player vehicles, VehicleProfile (police removed from the list),
   VehicleConfiguration (Racer's swap), VehicleVisual, VehiclePaint, VehicleLights, VehicleContact, RiderLook, RiderGestures (with
   RiderArms), the models and the rider (`Assets/Resources/VehicleModels/`, 12 FBX, 8.9 MB) and `VehicleGlazing.mat`. No Blender
   script was needed. Racer's ArcadeVehicle is newer: its changes are taken (VehicleConfiguration contact mode, wipeout, Recover);
   its weather grip and WaterFeedback stay behind (clear weather: no effect). The Round 10 surface-contact edit is kept.
5. Handling: DONE, Racer's per vehicle, untouched. Street Classic is the default; the blue prototype is retired (its body is hidden).
   The Street Classic is Racer's "original", which keeps the prototype's own tuning: same times as Round 12 on both shortcuts
   and the same 3-lap race to the thousandth.
6. Sound: DONE. VehicleAudio and VehicleSoundSynthesis (all made in code: engine idle and load, road, loose ground, tyre slip,
   body impact, landing). Racer has no audio files for vehicles. No music or radio.
7. Vehicle switch (temporary): DONE. D-pad left / right, `,` / `.`, only while no race runs; swapped where it stands; its name
   shows for 2.5 s. The last vehicle is saved in `vehicle.json` beside the race records (Editor: `local/vehicle.json`). Checks
   neither read nor write it (`-vehicle <id>` picks one for a check).
8. Everything else with every vehicle: DONE as far as checked. Chase camera uses Racer's per-vehicle offset; reset; race and
   penalties; both shortcuts; the debug panel and BUG_REPORT.md name the vehicle. Best lap / total are per vehicle (the Street
   Classic keeps the old key, so Dan's records carry on). Geese, breakables and solid trees were not driven into per vehicle.
9. Check in the exe: DONE by script, not with a controller. All 11 vehicles load, swap in place, climb the steepest 150 m of the
   lap (18.4 m rise, 12%) from 8 m/s at full throttle, and sound: 47 of 47. Pond, parked: 60 fps (99th percentile 57 to 58 fps)
   with the Street Classic, Needle 600, Trail Four and Turf Rocket. Shortcuts at 20 / 26 / 32 m/s, the four named vehicles: every
   run finished and #6 cleared 1641's roof every time. Not clean: the Trail Four on #2 is slower than the road (touched 1763
   at 20 m/s); at 26 and 32 m/s the Street Classic and the Turf Rocket brush 1627's passage walls (5 to 11 m/s). That is the
   scripted driver at speeds Round 12 did not run: the Street Classic at 30 m/s matches Round 12 exactly. Not retuned.
10. Executable rebuilt last: DONE, `Builds/ChastainRush/ChastainRush.exe` (2026-10-10 03:34). `-exeCheck` 9 of 9, laps
    1:22.675 / 1:22.617 / 1:22.645 (as Rounds 9 to 12), 60 fps at the three places (1920 x 1080, not 3840 x 2160).
11. This report: DONE.

For Dan to check: #2 the way he drives it (round 1539's far side): no penalty now, and the moved trees; #6's flatbed from the
lane; each vehicle with a pad (switch with the D-pad while not racing), especially the Needle 600, Trail Four and Turf Rocket on
both shortcuts and up the lane; engine, skid and landing sounds; the vehicle remembered after quitting; best times per vehicle.

Not done / not measured: Dan's drive; a real pad; 3840 x 2160. Noticed, not changed: exe checks append to the player's own
`bump_log.txt` (BumpLogger writes there in every exe run, as in earlier rounds); trees beyond 6.5 m still stand in Dan's wide line
round 1539.

New: `VehicleSwitch`, `Checks/Round13Runner.cs` (-exeRound13). Edits: MapBuilder (trees moved off lines, the flatbed), RaceRoute /
RaceController (corridors, per-vehicle records), ShortcutCheckRunner (-vehicle via VehicleSwitch, -shortcutRaceOffset,
-shortcutNoEntryPlanes), DebugReportSession / DeveloperHud (vehicle), PlayerSetup, ArcadeVehicle, GameShell.

Round 14 status — built and checked in the exe by script (2026-10-10). DRIVEN BY DAN 2026-10-10: approved, no issues (slowdown fix and the dirt pile). Dan's rule: if he drove it and did not complain, it is approved; he reports problems when he hits them. Check: `ChastainRush.exe
-exeRound14 <dir> [-r14Vehicles moto,mower,atv,pebble,original] [-r14Tag t]` (new, Checks/Round14Runner.cs); `-noSurfaceContacts`
gives the "before" numbers. Plots: `python Tools/mapgen/round14_plot.py <before> <after> <out>`. Everything is in
`Data/screenshots/round14/` (`round14_before.txt`, `round14_after.txt`, a TSV per vehicle and run, `round14_*.png`).

1. Racer reference: DONE. Profiles here are Racer's, unchanged (top speed / acceleration: Needle 600 61 / 18, Turf Rocket
   61 / 18, Trail Four 56 / 17, Pebble Coupe 49 / 14, Street Classic 49 / 14.5). ArcadeVehicle, VehicleConfiguration and
   ChaseCamera are Racer's code (the camera byte for byte), physics settings the same but the broadphase. What Racer's world
   gives that this one did not: ALL of Racer's ground is mesh colliders (`Ground_*`, no TerrainCollider in any scene), roads have
   colliders, and Racer's VehicleSurfaceContacts face-normal fix covers every ground mesh. Here the ground is a TerrainCollider
   (heightfield) and the fix only covered #2 / #6's ramps. Racer's own record of the problem: speculative contacts gave "nearly
   backward horizontal normals"; the motorcycle went 51.3 to 15.9 m/s in 40 ms (Racer `Docs/CR028-033/VALIDATION.md`). Racer's
   hills are no gentler (30 to 37% climbs on its courses).
2. Measured in the exe: DONE. (a) New Chastain Road, flattest straight (2915,757 to 2206,761, 700 m, westbound): to 50 m/s
   every vehicle matches the flat-ground formula to 0.1 s; after that, before the fix, the Needle 600 lost 117.7 m/s with the
   throttle held (54 to 6 m/s at x 2594 on plain road, then the bridge end), the Trail Four 45.8 (52 to 6 m/s at the bridge),
   the Turf Rocket 4.4; the two cars 0.1. (b) Steepest 150 m of the lane (12.2%) and the lap: the Needle 600 lost 18.6 m/s on
   the climb and 71.6 on the lap, the Turf Rocket 22.3 on the lap, the Trail Four 20.0; the cars 0.3. What took it, in plain
   words: the BODY of the small vehicles touching the terrain. With speculative CCD (Racer's setting for small vehicles) and 15 to
   22 cm of clearance, their body box had a contact with the terrain in 95 to 99% of all physics steps; most do nothing, but now
   and then PhysX gives one a horizontal normal (normal y 0.00 to 0.02) and the solver stops the vehicle against it: 30 m/s
   lost in 40 ms at (3125,1166), 20 to 22 m/s at (2886,905) for all three. Ruled out by the same traces: the hills (gravity
   took at most 0.5 m/s of the losses), wipeout (never), triggers (none entered), the wheels (all down when it happened). The
   cars' bodies never touched (sweep CCD, 37 cm clearance).
3. Suspects: a (body on the ground) CONFIRMED; b (hills) ruled out; c (wipeout / tip) ruled out; d (geese / breakables) not
   seen on the scripted line (it keeps to the centre); their costs are fixed and the same for every vehicle.
4. Fix: DONE, at the cause, for every vehicle. VehicleSurfaceContacts now covers this map's driving ground as Racer's covers
   its own: the terrain tiles (the heightfield's own surface normal under the contact, from a copy of its heights) and the bridge
   decks. A terrain contact's gap is measured straight down from the body's lowest corner, so a predicted contact with a bank
   further on is not a hit. Same rule as Racer: only contacts from above, faces up to about 57 degrees, never removed, no push.
   No profile changed; no hill assist (on the steepest climb every vehicle can hold 86 to 87% of its top speed: the 15% rule is
   already met, see below).
5. After: DONE. With the throttle held, body contacts now take: Needle 600 2.5 m/s on the straight (117.7 before), 0 on the
   climb (17.6), 0 on the lap (72.2); Turf Rocket 0.2 / 0 / 0 (2.6 / 0 / 22.4); Trail Four 0 / 0 / 0 (45.0 / 0 / 20.0); the
   cars 0 before and after, their traces identical. All finish all runs. Laps by the scripted driver: Needle 600 44.3 s (46.8
   before), Turf Rocket 44.2 (44.8), Trail Four 45.0 (45.4). By script only: a script is not proof of how it feels.
   Hills, the motor's own sums (top speed less what a grade takes, with the body's damping): on 12.2% the Needle 600 / Turf
   Rocket hold 53.4 of 61 m/s, the Trail Four 48.9 of 56, the cars 41.9 to 42.1 of 49 (13 to 14% below top); the steepest 20 m
   of the lap (16.8%, x 3075 z 1003) is too short to slow anything that much.
6. Other differences, listed: the small vehicles' bodies still have contacts with the ground in nearly every step (harmless
   now: no impulse). Racer's cars switch to speculative CCD when a small vehicle is within 20 m, so with AI drivers (Round 15)
   the cars would have hit this too; the fix covers them. Steering, sideways grip, the chase camera's distance / height per vehicle
   and the rider's lean are Racer's code and values, unchanged; the lateral-grip losses before were the after-effect of the body
   hits (the bike yawed), now gone. Nothing else changed.
   Seen, not changed (not on the lap; world change not asked for): the west end of the New Chastain Road bridge (x 2421, z 766):
   the deck ends 0.2 m above the ground and the creek bank falls away under it; the Needle 600 at 56 m/s leaves the deck end with
   its wheels off the ground and loses 3 to 5 m/s there (before the fix it lost 32 m/s, and one run rolled it).
7. #6 as a dirt pile: DONE, look only. Every part of the flatbed is removed (code too). One mound of packed brown earth: the driven
   face is the kicker's own surface (same curve, pitch, height, lip; collider unchanged), the rest falls away round the whole
   outline over a rounded shoulder to the grass, about 40 degrees on the sides and longer behind the lip (up to 10 m), the outline
   wavering, a few clods at the foot. The skirt goes from the top edge to just under the ground (nothing floats, nothing open) and
   has no collider, so it cannot snag a car. Driven: `-exeShortcut -shortcutOnly sc6_over_house` at 20 / 26 / 32 m/s with the
   Street Classic, Needle 600, Turf Rocket and Trail Four: on / lowest / off speeds identical to Round 13 for all twelve runs,
   1641's roof cleared, all checks passed. From Dan's BUG-002 spot (x 2808, z 1100) the camera is INSIDE the heap: that spot is 3 m
   from the lip's corner, and any earth slope off a 5.6 m lip covers it (the skirt has no collider, so a car can stand inside it).
   Pictures: `r14_dirtpile_bug002.png` (his spot), `r14_dirtpile_from_bug002_back.png` (8 m back), `r14_dirtpile_approach.png`.
8. Executable rebuilt last: DONE, `Builds/ChastainRush/ChastainRush.exe` (2026-10-10). `-exeCheck` 9 of 9, 60 fps at the three
   places (1920 x 1080). Laps 1:22.657 / 1:22.665 / 1:22.652 (Round 13: 1:22.675 / 1:22.617 / 1:22.645): the Street Classic's
   scripted race moved by up to 0.05 s, the only change to a car seen; its straight, climb and lap traces are identical.
9. This report: DONE.

For Dan to check: the Needle 600 and the Turf Rocket (and the Trail Four) flat out round the loop and up the lane: no sudden
slow-downs; the dirt pile from the lane and from BUG-002, and driving it; the cars feel as before.

Not done / not measured: Dan's drive; a real pad; 3840 x 2160; driving near the verge into geese and breakables per vehicle.

New: `Checks/Round14Runner.cs` (-exeRound14), `Tools/mapgen/round14_plot.py`. Edits: VehicleSurfaceContacts (terrain and
bridge decks), MapBuilder.Shortcuts.cs (DirtPileLook replaces the flatbed), GameShell (-exeRound14). Imported from Racer: nothing.

Current Objective (Focus Area)

*Round 14 is built and scripted-checked; Dan has not yet said he drove it.
Its status is above.*

**Round 15 — water, break sounds, quiet testing, then three AI drivers.
DECIDED (2026-10-10).** Dan: "Lets fold these into the next round." The
standing rules in `CLAUDE.md` apply. Racer is the reference for all of it:
port what it has, do not write a new version. Do Part A first and commit it
by itself before starting Part B.

Part A: Dan's three requests.

1. Quiet testing, a STANDING RULE from now on (also added to `CLAUDE.md`).
   Dan: "When you are testing - Please mute sounds. Only briefly turn on
   sounds if you need to hear, then quickly turn off." Every automated run
   is silent: Editor Play mode checks, batch runs, and every exe run
   started with a check argument (`-exeCheck`, `-exeShots`, `-exeRound..`,
   `-exeShortcut`). Mute at start-up, before the first frame can make a
   sound. If a check truly needs to hear something, switch sound on for
   that moment only and off again, and say so in the report. The game Dan
   launches himself is never muted. Do this item first, so the rest of the
   round is quiet.
2. Water. Dan: "Vehicles should be submerged in the water but not all the
   way. Driving through water should slow you down. Please look at how
   this is handled by the other project."
   a. Read how Racer does it and report in a few lines: `ShallowWater`
      (a trimmed copy is already here), the immersion drag in
      `ArcadeVehicle`, `WaterFeedback`, and how Racer shapes the ground
      under its water so a vehicle sinks part-way and can always get out.
   b. Here the elevation data is flat across every pond (the survey
      flattens water), so there is no bed to sink into. Give the water a
      bed in the generator: a shallow bowl under each pond and lake and a
      shallow channel along each stream, deep enough that a car sits with
      its wheels and lower body under water and its roof and windows well
      clear, and never deeper. Banks slope gently all the way round:
      "escapable water" is a standing rule, so any vehicle that drives in
      can drive out anywhere, including the bikes and the mower.
   c. Register every water area with `ShallowWater` so the vehicle sinks
      to the bed, slows as Racer's does, and speeds up again on leaving.
      Bring over `WaterFeedback` (splash, wake, sound) as Racer has it.
   d. The neighborhood pond keeps its fountain, docks, walls and geese; the
      pool is water too. A reset from water puts the car on the route or
      the nearest road, never back in the water.
   e. One check in the exe: drive a car and the Needle 600 into the
      neighborhood pond, across, and out; report how deep each sat, the
      speed in and across, and that both got out.
3. Break sounds. Dan: "Lets add in game sounds for smashing mailboxes,
   fences, street lamps, fire hydrants. Metal or wood breaking sound
   whichever is appropriate."
   a. See whether Racer plays a sound when a `BreakableProp` breaks and how
      (a file, or made in code like its vehicle sounds). Use the same way.
   b. A sound for each: wooden privacy and picket fences and the white
      rail fence (wood splintering); the pool's iron fence (metal); a
      mailbox (a metal clang with the wooden post); a street lamp (metal,
      with glass); a hydrant (metal, then the rush of water while it
      runs); the shortcut's windows and sliding glass door (glass
      breaking). Loudness follows how hard it was hit. Heard in 3D from
      where it happened. Limit how many play at once.
4. Geese. Dan: "Geese no longer honk. Give me a space to drop a proper
   geese honk noise." The space exists but only works after a rebuild, and
   Dan may not know where it is. Make it a drop-in:
   a. The game loads, each time it starts, every `goose_honk*` file (.wav,
      .ogg or .mp3) from
      `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\Geese\`
      and uses them at once, with no rebuild. Create that folder on first
      run, with a short READ ME text file in it saying what to drop there.
   b. Keep `Assets/Audio/Geese/` as well (built into the exe); files in
      the drop-in folder are added to those.
   c. The same drop-in idea for the break sounds, so Dan can replace any of
      them: `Sounds\Breaks\` with names such as `wood*.wav`, `metal*.wav`,
      `glass*.wav`. A file there replaces the built-in sound of that kind.
   d. With no goose file anywhere the geese stay silent (no made-up honk).
5. Checks must not write Dan's own files (noticed in Round 13): exe checks
   append to his `bump_log.txt`. Point every check at its own folder.

Part B: three AI drivers on the Chastain Glen loop. Dan: "some ai drivers
... I think we can start with 3 ai drivers", vehicles "either I select or
they pick random ones."

6. Port from Racer: `RoadDriver`, `RaceRoad`, `ShortcutStrategy`,
   `AiFinishEstimate`, the start grid from `RaceDirector`, rival names from
   `RaceNames`, and `VehicleContact` behaviour between vehicles. This
   game's own race logic (`RaceController`: gates, 5 s penalties, shortcut
   corridors, barriers) stays and the AI drivers race under it, the same
   rules as the player. List what came over and what was adapted.
7. Three AI drivers in every race started with T. Each gets a vehicle
   picked at random from the eleven for that race, and a name. Until the
   menus exist (Round 16) a small settings file can fix their vehicles;
   say where it is.
8. A four-place start grid behind the start line, the player on it too,
   all held until GO.
9. They drive the lap properly on this map: the barriers, the breakable
   fences, mailboxes, lamps and hydrants (they may hit them; they must not
   get stuck on them), solid trees, the geese, the water from Part A.
   Racer's lessons apply: a jump run-up uses `aiEntrySpeed`; the obstacle
   sensor must not read a ramp, the dirt pile or 1627's stairs as a wall;
   an AI that is stuck or off the route resets as Racer's does.
10. Shortcuts: with `ShortcutStrategy`, as in Racer, so some AI drivers
    take shortcut #2 (the side yard, the door, the stairs, the jump) and
    shortcut #6 (the dirt pile over the house) some of the time. If an AI
    driver cannot take one reliably, it does not take it; say which.
11. On screen: the player's place (1st to 4th) during the race, and the
    finishing order with names and times, penalties included, on the
    finish panel. AI vehicles make their engine sound, heard in 3D.
12. Their pace is Racer's default. Report their lap times beside Dan's
    best (0:42.4 in the Street Classic) and leave the tuning to Dan.
13. One check in the exe, muted, the way Dan plays: a 3-lap race with the
    three AI drivers and a fast human-like player taking both shortcuts;
    all four finish; no AI driver stuck for more than 5 s; the frame rate
    with four vehicles on screen.
14. Rebuild the executable last and report its path.
15. Report status for every item as a short list, then "for Dan to check".

Out of scope for this round: menus and the garage (Round 16), police,
campaign, traffic, split-screen, the radio and Dan's music.

**Round 15 status (Claude Code, 2026-10-10).** Built and checked in the exe by script (muted). DRIVEN BY DAN 2026-10-10 (session 2026-10-10_14-22-18-410_d2650e): approved apart from what he reported, which is items A to E under Round 16.

Part A (committed on its own):
1. Quiet testing: DONE. `CheckRun` (Assets/Scripts/CheckRun.cs) sets AudioListener.volume to 0 before the first scene loads
   in any exe run with an `-exe...` argument and any Editor run with -batchmode / -executeMethod; `CheckRun.Hear(true/false)`
   for a moment that must be heard (not needed this round). The game Dan starts himself is never muted. Every check in this
   round ran muted (the report prints AudioListener.volume 0).
2. Water: DONE.
   a. Racer, in a few lines: `ShallowWater` is a box (or round) volume whose top is the surface; the bed is the ground under
      it, sculpted per scene (lake beds dug into the terrain, pools with a full-width shallow end, banks 25% or gentler, 0.85).
      `ShallowWater.Sample` measures the depth from 0.15 m under the wheels' ground and is full at 0.65 m; `ArcadeVehicle`
      multiplies the drive by 0.48 and adds a drag of 0.95 x speed at full immersion (both were already here, unused: no water
      was registered). `WaterFeedback` draws ripple rings round the vehicle and plays a soft splash on going in and out.
   b. A bed in the map build (`MapBuilder.Water.cs`, run before anything samples the ground, as the Round 9 regrade): every
      pond and lake a bowl 0.5 m under its surface, reached 1 in 7 (14%) from the shore; every stream a channel 0.5 m under
      its surface for 0.8 m either side of the line, then 20% banks (the stream is now drawn about 6 m wide, level across,
      where the water is). Levels are measured on the untouched lidar first, so every surface stays where it was. Only lowers,
      only where the ground was at the water (fades out 0.6 m above it: no trenches in hillsides), never under roads, bridges,
      drives or buildings. 527,330 terrain samples lowered, by at most 0.50 m. No tree stands in water. Done in the map build,
      not mapgen.py: same inputs, re-runnable, no new download or mapgen run needed.
   c. All 22 ponds and lakes registered by outline (ShallowWater got an outline mode, commented), streams as 906 boxes;
      Racer's `WaterFeedback` copied (its ambience setting left behind; the splash noise copied in).
   d. The pond keeps its fountain, docks, walls and geese (docks' posts now stand in the bed). The pool is water: 0.5 m deep,
      its floor rising 1 in 7 to the deck along its longest side (Racer's shallow end), a hole in the deck, surface 8 cm under
      the coping. A reset never lands in water: the safe spot is not recorded there, and CarRecovery rejects a spot in water.
   e. Check (`ChastainRush.exe -exeRound15 <dir> -r15Only water`): across the pond (82 m of water) at full throttle. Street
      Classic: bed 0.50 m under the surface, water up to the bottom of the doors, in at 24.7 m/s, across 8.7 m/s mean (lowest
      6.3), out on the far bank after 12.0 s. Needle 600: 0.50 m (the rider's shins), in at 26.3, across 10.8 (lowest 7.8), out
      after 10.0 s. A reset in the middle of the pond put each on dry ground 55 m away. 8 of 8 passed. (The first line tried
      ended against a tree trunk on the far bank, out of the water; the check now picks a line clear of trees.)
3. Break sounds: DONE. Racer's `SmashAudio` way (made in code, no files; bounded 3D voices; loudness from the hit speed),
   copied as `BreakSounds`: wooden fences and the white rail = wood; the pool's iron fence = metal (Racer's chain-link);
   mailbox = metal clang plus wood; street lamp = metal plus glass; hydrant = a heavy clang, then a looped rush of water at the
   jet while it runs (at most 3 at once); shortcut windows and the sliding door = glass. Six voices at once, nothing past 75 m.
   Check (`-r15Only sounds`): one of each kind broken, each played (counted, muted).
4. Geese and drop-ins: DONE. Every start, the game reads `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\`:
   `Geese\goose_honk*.wav|ogg|mp3` are added to the built-in ones; `Breaks\wood*|metal*|glass*|water*` replace the made sound of
   that kind. The folders and a READ ME in each are made on the first start of the game Dan runs (a check never writes there).
   `Assets/Audio/Geese/` still works (README updated). No goose file anywhere = silent geese. Check: with a test folder
   (`-soundsRoot`), the READ MEs were made, a test honk reached the geese and a test wood sound replaced the wood break.
5. Checks keep to their own folder: DONE. In a check run the debug reports and bump log go under the check's folder and the
   race records to `<dir>
ace_records_check.json`. Run exe checks with `-logFile <dir>\Player.log` too, or Unity overwrites
   Dan's Player.log. Dan's bump_log.txt was unchanged by every run this round (07:42).
   Also fixed: the trees' material kept its instancing flag only in memory (set after the asset was written); now it is set
   before, so the exe never draws without trees.

Part B:
6. Port: DONE. From Racer: `RoadDriver` (trimmed to racing: pure pursuit, lanes and passing only into a clear lane, speed
   planned from bends, hills and crests with braking judgment, the sphere-cast obstacle sensor, stall back-off, recovery when
   stuck or off the route, finish run-off and parking; traffic, highway, Racer's map-specific run-ups, split-screen and
   campaign left behind), `RaceRoad` (trimmed: no highway / forest / bypass), `ShortcutStrategy`, `DriverVariation` and
   `AiFinishEstimate` (copied), the grid and quick-race names and paints from `RaceDirector.CreateCars`, the grid card idea from
   `RaceNames.GridText`; `VehicleContact` (cars vs small vehicles) was already here and now acts between the racers.
   Adapted: the race is this game's `RaceController`, now with a `RaceEntrant` per car, so the AI race under exactly the
   player's rules (gates, 5 s per missed checkpoint, shortcut corridors and entry planes, barriers). A shortcut is an
   `AiShortcut` (Racer's WoodlandRoute: its line, turn-off and rejoin, an entry speed held through run-up and jump, Racer's
   aiEntrySpeed); on it the AI steers along the line as this game's shortcut check does, and its obstacle sensor ignores the
   static world there. Recovery: back on the line or the lap, clear of everything, never in water (CarRecovery's rule).
7. Three AI drivers in every race started with T: DONE. EMBER, GOLD, BLUE, each a random vehicle from the eleven per race.
   To fix them until Round 16: `ai_drivers.json` beside the race records (`%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain
   Rushi_drivers.json`; made with three "random" lines the first time Dan starts a race; put a vehicle id instead, e.g.
   "moto"). Checks never read it (`-aiVehicles a,b,c`, `-aiCount n`).
8. Grid: DONE. Racer's: the player at the back on the old grid spot, the three ahead at 8, 15 and 22 m, either side of the
   lane where there is room (the entrance lane is narrow beside the island, so they stand nearly in line); all held until GO;
   a STARTING GRID card during the countdown.
9. Driving the lap: DONE by script. They follow the entrance lane to the lap, race 3 laps, take barriers, fences, mailboxes,
   lamps and hydrants as the player does (triggers: they break them and lose a little speed), trees are solid round them too
   (TreeTrunks follows each AI car), geese are not solid (they drive through; the flock reacts only to the player, as before),
   water slows them as it slows the player. No AI stuck in the final runs (longest without progress 0.5 to 0.8 s, no
   recoveries). Found and fixed on the way: on #2's one-car-wide side yard an AI and the scripted player wedged together and
   the AI's recovery kept putting it back behind the stuck car; now an AI does not commit to a shortcut with another car at
   its turn-off or on its first 80 m, and when held up by a vehicle on a shortcut it recovers to the lap.
10. Shortcuts: DONE. ShortcutStrategy decides per lap (more likely when behind). Trial, each vehicle once down each shortcut
    as an AI: #2 Two-house drop 11 of 11 clean, #6 Rooftop leap 11 of 11 clean (no recoveries, worst tilt 29 to 37 deg), so no
    vehicle is barred from either (`RaceController.AiShortcutNever` is empty). In races they take #2 and #6 on some laps.
11. On screen: DONE. The place (1st to 4th) at the top right during the race, the finishing order with names, vehicles and
    times (penalties included, "estimated" if an AI was still racing 60 s after the player finished) on the finish panel. AI
    engines are heard in 3D (VehicleAudio driven by the AI's pedals).
12. Pace: Racer's default (Normal). AI lap times in the scripted races: Needle 600 42.3 to 43.5 s, Turf Rocket 42.4 to 45.0,
    Ridge Scrambler and Drifter Twin 43.8 to 46.5, the cars 47.5 to 50.3 (Street Classic 48.0 to 49.5, Pebble Coupe 47.6 to
    49.8, Sundown Roadster 47.5 to 49.6, Longroof GT 49.9 to 51.8), first laps 1 to 3 s slower from the grid. Dan's best:
    0:42.4 in the Street Classic. Tuning left to Dan (`RoadDriver.Difficulty` 0 / 1 / 2, Racer's Easy / Normal / Hard).
13. Exe check, muted, 3840 x 2160: DONE. `-exeRound15 <dir>`: a 3-lap race, three random AI (Sundown Roadster, Turf Rocket,
    Needle 600), the player scripted flat out taking both shortcuts every lap: all four finished (BLUE 2:09.3, GOLD 2:12.1,
    EMBER 2:23.9, the scripted player 2:30.4), no AI stuck, frame rate 60 fps in the first 10 s after GO with all four on
    screen, 60 average and 60 at the 1% low over the race (vsync). Same race three more times with Street Classic, Needle 600,
    Street Classic: all clean. `-exeCheck`: 9 of 9.
14. Executable rebuilt last: DONE, `Builds/ChastainRush/ChastainRush.exe` (2026-10-10 12:46, full map rebuild).
15. This report: DONE. Results: `Data/screenshots/round15/`.

For Dan to check: a race (T) flat out with a controller at 3840 x 2160: the grid, GO, the AI pace against yours, their
engines, the place readout, the finish panel; an AI going through #2 (side yard, door, stairs, window) and over #6; driving
into the pond, the pool and a creek and out again (how deep it feels, how much it slows); a reset in water; smashing a
privacy fence, the pool's iron fence, a mailbox, a lamp, a hydrant and #2's door (the sounds); a goose honk file dropped into
`Sounds\Geese` after the first start.

Not done / not measured: Dan's drive; a real controller; the sounds heard (all counted, muted); geese do not react to the AI cars.
Imported from Racer in Round 15: WaterFeedback, SmashAudio (as BreakSounds), RoadDriver, RaceRoad, ShortcutStrategy,
DriverVariation, AiFinishEstimate.

**QUEUED NEXT — Round 16: menus and the garage.** Dan: "getting the
menus over shouldn't wait too long." Racer's `RaceMenus.*`, `MenuInput`,
`MenuGlyph`, the garage view (turning model, stat bars), picking the AI
drivers' vehicles, controller first. Replaces the temporary vehicle switch
and the T / X race keys.

**Also in Round 16 (Dan, 2026-10-10: "Can we put these in with the next phase?").** Do A to E first and commit them on their own before starting the menus. Fix exactly where Dan
pointed; list similar spots elsewhere for Dan rather than changing them.

A. BUG-001 (session 2026-10-10_14-22-18-410_d2650e, x 2910.95, z 814.32, by the pond near the entrance): the
   pond's culvert headwall (the concrete wall with the two pipes) stands with its back exposed. Dan: "the
   backside of this shouldn't be exposed. It should be covered by land since this are pipes". Bury its back and
   sides in the bank, only around it; grounding rule for anything nearby.
B. BUG-002 (same session, x 2807.43, z 1101.14, shortcut #6): the dirt pile's skirt has no collider, so a vehicle
   drives into / under it. Dan: "this should be solid so i cant run underneath it". Make the heap solid, without
   changing the kicker's driven face, its lip or #6's drive (re-check #6 with the AI trial and the shortcut check).
C. Grass / sidewalk edges: Dan: "some places the grass/sidewalk causes you to either get stuck or bumps you". He
   may capture spots case by case (F4). This round: fix the spots in his reports; if none come with a position,
   find likely causes (curb and sidewalk edges against the terrain, step heights the wheels or body catch) and
   report them with positions for Dan, changing nothing he has not pointed at.
D. Signs breakable: Dan: "signs should be breakable". The street and neighborhood signs (no-parking, no-U-turn,
   the shortcut sign, others) break like mailboxes (FenceChunk, a sound). Not the brick entrance monuments
   unless Dan says so.
E. Shortcut #6's jump (the dirt pile over house 1641; Dan confirmed it is #6, not #2): Dan: "There is a spot on
   the second jump where it catches you some times. I wasn't able to recreate it but I did experience it." Find
   it: drive #6 with every vehicle across a spread of speeds and lines (the shortcut check, plus lateral offsets
   and approach angles out of the CP4 corner) and look for contacts that stop or snag the body on the dirt pile,
   the kicker's lip, 1641's roof and the landing. Do it together with B (the solid heap must not add a snag).
   Report what was found before changing the jump (protected geometry).

**Round 16 A to E status (Claude Code, 2026-10-10).** Built, checked in the exe (muted, 3840 x 2160), committed on its
own. No new debug session since 2026-10-10_14-22-18-410_d2650e. Results: `Data/screenshots/round16/`.
A. DONE. The headwall (built at x 2909.9, z 805.6, 14 m from the street) had the bank 1.24 m below its coping behind it. The
   map build now raises the bank round it only (`BuryCulvert`, MapBuilder.Pond.cs, right after the water beds, before
   anything samples the ground): level with the wall just under the coping behind it and along its wing walls, then 1 in 3
   down to the ground; never in the pond, fading out 2 to 4 m from roads and drives (the road and its rail fence do not
   move). 89 terrain samples raised, by at most 1.23 m. The pipes' face toward the pond is open as before. Shots from
   Dan's spot and both sides: `A_*.jpg`.
B. DONE. The heap's skirt is solid: the same faces as its look, a collider of its own (frictionless, like the ramp, a child
   "sc6 ... heap" so the Round 14 surface contacts treat it as a ramp surface). The driven face, its curve, lip and
   collider are unchanged. Driving into the skirt at 8 m/s from Dan's side: the car stops at its foot. #6 re-checked
   (item E's sweep): the heap is touched only on runs 3 m off the line (falling off the kicker's side), never on or near
   it. AI trial (`-exeRound15 -r15Only shortcuts`): each vehicle as an AI down #2 and #6, 11 of 11 clean on both, no
   recoveries (`ai_shortcut_trial.txt`).
C. REPORT ONLY (no positions came in Dan's reports, so nothing was changed). New report-only scan
   `ChastainRush.GrassScan.Run` (Editor batch: rays onto the terrain collider across and along every Chastain Glen street,
   curb to lawn; `Data/screenshots/round16/grass_scan.txt`). Curbs, sidewalks and drives have no colliders: the car drives
   on the terrain, so a snag there is the ground under them. Almost every large hit is at a STREET JUNCTION CORNER, where
   the road is cut into the slope and the lawn rises in a short steep bank right behind the curb (seen in the exe):
   * x 3066-3070, z 996-1009 (Chastain Glen Lane / Chasefield Way, both corners, on the lap): steps of 14-15 cm in 10 cm,
     banks of 43-47 deg, a 2.6 m chord crest of 0.48 m and sag of 0.54 m: a car's floor or nose hits.
   * x 2988, z 1070-1073 (Chasefield Court / Chasefield Way): 36 deg, step 8 cm, crest 0.37 m.
   * x 3138, z 1092 (Chastain Glen Lane): step 10 cm, crest 0.32 m, 32 deg.
   * x 2926-2950, z 750-767 (by the entrance island and monuments): steps 7-9 cm, crests 0.26-0.30 m.
   * x 3067, z 952 (Glenbarrett Lane corner): step 10 cm on the sidewalk line, crest 0.24 m.
   * x 2730, z 984 (Chastain Place corner) and x 2990, z 936 (Courtfield Trace corner): 7-8 cm steps, 0.23-0.26 m crests.
   Lawns elsewhere: some 30-33 deg slopes in back of the sidewalk (x 3083 z 998, x 2999 z 1090, x 3137 z 1157, x 3121
   z 929, x 3005 z 1100), no steps. For Dan: drive these corners and say which ones catch; a fix would ease each corner's
   bank in the map build, only where pointed at.
D. DONE. The no-parking and no-U-turn signs at the entrance island (their post and both plates) and the hand-painted
   SHORTCUT sign at #2 break like mailboxes (FenceChunk: a trigger, a little speed lost, whole again at race start); the
   printed faces fly off with the pieces (FenceChunk.faces). Sounds: Racer's sign clang for the street signs (new
   BreakSounds.Kind.Sign), wood for the plywood shortcut sign. The brick monuments are untouched. Checked: the shortcut
   sign smashed at 15 m/s (`D_shortcut_sign_smash_15ms.jpg`).
E. REPORT ONLY (protected geometry: the jump is not changed). The shortcut check now takes `-shortcutLines` (on the line,
   1.5 and 3 m either side, and "a4" / "a-4": 4 m off at the run's start, onto the line by the kicker's foot) and writes
   `shortcut_snags.csv` (body contacts pushing against the motion with more than 1 m/s, and any loss of more than 1.5 m/s in
   one physics step while not braking). Every one of the eleven vehicles, approach 14 to 34 m/s, 7 lines: 462 runs
   (`Data/screenshots/round16/e_sc6/`). Found:
   1. THE LANDING (most likely Dan's catch). x 2795-2803, z 1056-1066, on the grass after 1641: in 85 of 330 runs on or near
      the line the vehicle comes down nose-first (9 to 12 deg) and the front corner of its body hits the ground, losing 3 to
      7 m/s in one step (Street Classic at 18 m/s: 20.3 -> 13.6, an impulse of 14.6 m/s). Every vehicle, most often the
      cars (Pebble Coupe and Highball Fastback 13 of 30, Drifter Twin 12, Street Classic 10; Skyfin Cruiser 1). It depends
      on where the flight ends (short landings at 14 to 26 m/s, some at 30 to 34), which fits "sometimes".
   2. THE KICKER'S FOOT, motorcycles and the ATV. Out of the CP4 corner the bike's lower left corner brushes the kicker's
      left edge at its foot (x 2817-2821, z 1108-1112), 1 to 4 m/s each time (Needle 600 31 contacts, Drifter Twin 25,
      Ridge Scrambler 15, Turf Rocket 11, Trail Four 9; cars 3 to 8). The edge's contact normal is too steep (about 62 deg)
      for the Round 14 surface correction, so the solver's own push applies.
   3. Nothing on 1641's roof on the line (roof clearance 1.3-1.5 m from 18 m/s up; at 14 m/s cars ride the roof, as in
      Round 13). 3 m off the line a vehicle lands on the roof's edge or falls off the kicker's side (rolls, trunks): not a
      snag, a missed line. The runs that "hit a house" at 1.5 m off are roof rides, and House 1052 / 713 after the rejoin
      (the offset carried the line into them: an artifact of the check, not the course).
   Options for Dan (none done): (1) a longer, gentler run-out where the vehicle lands (the landing zone's grade eased over
   x 2790-2805, z 1045-1068) or the landing carry (RampHold) extended to that strip; (2) the kicker's foot corners
   rounded or its skirt made the driving surface at the foot so a bike's corner rides over the edge.

**Round 16 menus and garage status (Claude Code, 2026-10-10).** Built and checked in the exe by script, not yet driven by Dan.
* Ported from Racer: `MenuInput` (the controller / keyboard decision and the release barrier; Racer's DeveloperLocationHud is
  this game's DeveloperHud), `MenuGlyph` (the button pictures), `MenuActionRegistry` (copied), and from `RaceMenus` the card,
  rows, value rows (`‹ value ›`, left / right, held to repeat; A never steps one), the page stack with focus memory, the
  menu's own one-row-per-press navigation, the focus frame, the button prompts, the garage preview (turning model in its own
  panel, sharp at 4K, right stick / Q E / mouse drag, slow turn when left alone) and the five stat bars. All re-written into
  `Assets/Scripts/Menus/GameMenus.cs` and `GameMenus.Garage.cs` around a small flow of this game's own in place of RaceFlow.
  uGUI (`com.unity.ugui` 2.6.0, Racer's version) is added to the project.
* Pages. At start: the title menu (RACE, FREE ROAM, GARAGE, QUIT GAME), the game paused under it. Driving: Start / Esc /
  Enter opens the pause menu (free roam: RESUME, RACE, GARAGE, QUIT GAME; in a race: RESUME, RESTART RACE (asks first), RACE
  SETUP, LEAVE RACE, QUIT GAME; after the finish: RACE AGAIN, RACE SETUP, FREE ROAM, QUIT GAME). B / Esc goes back; on the
  pause menu's first page it resumes. RACE SETUP: Laps 1 to 9, Direction, Opponents (3 AI drivers / Solo), Difficulty (Easy,
  Normal, Hard: Racer's RoadDriver.Difficulty), Opponent vehicles (each of EMBER, GOLD, BLUE: Random or one of the eleven;
  kept in `ai_drivers.json` as before), Garage, START RACE. Settings are kept in `race_settings.json` beside the records.
  GARAGE: the vehicle (left / right through the eleven; locked during a race), its colour (Standard or Racer's seven), the
  stat bars, Done; vehicle and colour kept in `vehicle.json`.
* Replaced: the D-pad / `,` `.` vehicle switch and its hint, the T / X keys and D-pad up / down race keys, Esc-twice-to-quit
  (kept only in a check run, which has no menus). The HUD lines now say "Start / Esc: menu". The debug menu no longer opens
  on the pad's Start (Start is the game menu's; F6 / `\` still open it; the pad's View button still captures a bug).
* Check: `ChastainRush.exe -exeRound16 <dir>`: a virtual gamepad through title, garage (vehicle and colour changed, model
  turned), race setup (laps, difficulty), opponent vehicles, START RACE, pause with the race clock standing still, resume,
  restart's confirmation, LEAVE RACE, the free-roam pause menu: 22 of 22. `-exeCheck` 9 of 9. Screens:
  `Data/screenshots/round16/menus/`.
* Not done / not brought: Racer's settings pages (volume, display, controls), the rider page, records boards, loading
  screen, the results-stage menu (the finish panel stays; Start opens RACE AGAIN / FREE ROAM).
* Checks before the "stay off Dan's screen" rule (added to CLAUDE.md during this round) ran full screen at 3840 x 2160; from
  then on, minimized 1280 x 720, and a check run now turns run-in-background on itself (CheckRun).

**DONE, driven by Dan 2026-10-10 ("mostly it is fine"; his one report is Round 18 Part A) — Round 17: the small follow-ups, then the shopping center (Dan, 2026-10-10: "Since these changes
are small I want to fold them into our next phase").** Round 16 (A to E, menus, garage) is built and committed.
Part A is items (1) to (4) below: do them in order and commit them on their own. Part B is the shopping center
(the "Round 17, the shopping center" notes further down): first the labelled overhead screenshot of the center,
the gas station and the bank for Dan, then the paved parking lot, the shops, the gas station with breakable
pumps that explode for show, the abandoned bank, made-up store names. Then rebuild the exe and report. All
under the standing rules, "Stay off Dan's screen" and American English included. Later rounds are not started.

**(1) American English (Dan, 2026-10-10).** Dan: "use
American english...no more colour, use color. Not sure if there are other words like this but this is the only
one I noticed". Standing rule in CLAUDE.md. This round: fix every British spelling Dan can read in the game.
Found by a search of string literals (not a complete list; search again): `GameMenus.Garage.cs` "Colour:";
`RiderLook.cs` "Hair colour", "Hat colour", "Shirt colour", "Pants colour", "Grey"; `DeveloperHud.cs` "from
centre"; `DebugReportSession.cs` and `BumpLogger.cs` "centreline"; any check that looks for the old text
(`Round16Runner.cs` "Colour:"). Saved-data keys and identifiers keep their names. One check: the garage and the
rider screen read "Color" in the exe.

**(2) The debug panel and debug menu (Dan, 2026-10-10).**
1. Dan: "Be default lets leave the debug window off now". The debug panel (F3 / `[`) starts hidden in the game
   Dan launches. F3 / `[` still shows it; F4 / `]` capture still works with the panel hidden.
2. Dan: "The debug menu can be either a menu option or come up if you press start and the debug window is
   open/ regular menu comes up if debug window is closed". Build both, they do not conflict:
   - Start with the debug panel closed opens the regular menu. The regular menu has a "Debug" entry that opens
     the debug menu (the only controller way in when the panel is hidden), with a row there to show or hide the
     debug panel.
   - Start with the debug panel open opens the debug menu. The debug menu has a "Game menu" row, so the
     regular menu is never out of reach.
   - F6 / `\` still opens the debug menu from the keyboard.
3. Dan: "Debug menu should be controllable via the controller". Same rules as every menu (controller first):
   every row reachable with a controller only, focus visible, B goes back, left / right changes a value, A never
   cycles one. A capture (F4) from the controller too: a row in the debug menu.
   One check in the exe: with a controller only, open the regular menu, reach Debug, show the panel, close,
   press Start, land in the debug menu, B out.

**(3) Dan's reports, session 2026-10-10_16-53-56-509_f5d57e (read by Claude, 2026-10-10).**
- BUG-001 (vehicle x 2744.09, z 879.53, at the pond's west bank): "thats a heron, shouldn't get the goose
  sound". The heron in `GooseFlock` honks with the goose clips (Dan has now dropped honk files in). The heron
  makes no goose sound: silent, unless a file named `heron*` is in `Sounds\Geese\` (same drop-in rule; say so
  in the READ ME). Geese unchanged.
- BUG-002 (Grasmere Court houses) is NOT in this round: Dan moved it to the neighborhoods round (below).

**(4) Seen by Claude in both screenshots, not reported by Dan:** the HUD line at the top left ("Start / Esc
menu: race Chastain Glen loop, 3 laps, forward  best lap ...") is drawn twice, one copy over the other and
slightly offset (an old line still drawn under the new one). Draw it once.

**Round 17 status (Claude Code, 2026-10-10).** No new debug session since 2026-10-10_16-53-56-509_f5d57e.
Part A is built and checked in the exe by a script (muted, minimized 1280 x 720, a virtual gamepad), committed on its own
(b512061, b3bfe9f), and not yet driven by Dan. Check: `ChastainRush.exe -exeRound17 <dir>`, 20 of 20; shots in
`Data/screenshots/round17/partA/`.
1. DONE. Color / Gray / center / centerline in the garage, the RiderLook labels (no rider page exists in the game), the
   debug panel, BUG_REPORT.md and the bump log; the Round 16 check looks for "Color:". Identifiers and saved keys unchanged.
2. DONE. The panel starts hidden in the game Dan plays (a check run keeps it on). F4 / ] / the pad's View button capture
   with it hidden (the panel is drawn on the captured frame anyway). The debug menu is now a page of the game menus
   (controller first): Debug panel Shown / Hidden (left / right), Capture bug, Export bug report ZIP, Open debug report
   folder, Start new debug session (asks first), Game menu, Back / Resume. Reached by DEBUG on both pause menus, by Start
   while the panel is up, and by F6 / `\`. The old IMGUI menu is left only for check runs (no game menus there).
3. DONE. The heron never uses the goose clips; it is silent unless a `heron*` file is in `Sounds\Geese\` (the READ ME
   there now says so and is rewritten once to add that line).
4. DONE. Not a second line: the shadow under the top-left HUD line had its bold tags stripped, so it was narrower than
   the text and showed as an offset copy. Only color tags are stripped now (every shadowed HUD text).
Part B: STOPPED for Dan, as asked. The labelled overhead of the built scene (center, gas station, bank, parking lot,
north up, 50 m bar) is in `local/round17/round17_shopping_center_overhead.png` (git-ignored: Chastain Glen is in the
picture). Nothing is dressed yet.

**(5) Round 17, added from Dan's session 2026-10-10_17-47-11-485_46d8cb (read by Claude, 2026-10-10).**
Dan on the shopping-center overhead: "overhead is fine with the exception of what is the latest bug reports".
So the overhead is APPROVED, with BUG-003 as the correction; go on to dress the center.
- BUG-003 (vehicle x 3257.33, z 1042.52, heading 72, beside the south wall of the supermarket block): "there
  are shops on this side...as well as this is a parking lot tha tmeet s with the main parking lot". Two things,
  both part of Part B:
  * The south face of the big block has shop fronts too (doors, windows, made-up signs), not a blank wall.
  * The ground south of the block, where Dan is standing (now grass), is a paved parking lot, and it joins the
    main parking lot east of the block as one continuous paved area (the map's service roads there, about
    x 3230 to 3340, z 1030 to 1060, run through it).
- BUG-001 (vehicle x 2945.75, z 762.90, heading 98, Chastain Glen Lane where it meets New Chastain Road; bump
  log JOLT at x 2948.4, z 767.4): "unintended jump". There is a visible crease across the lane's mouth by the
  entrance island. Smooth the road surface there so a vehicle stays on the ground; this spot only.
- BUG-002 (vehicle x 3145.88, z 767.01, heading 344, New Chastain Road at the Chastain Corner Northeast
  junction; bump log LANDED at x 3164.3, z 767.2): "unintebded jump". Same: smooth that junction so it does not
  launch a vehicle; this spot only.
  Both are junction seams (the design decision stands: natural jumps stay, awkward ones Dan points at go).
  Check each by driving through flat out in the exe from Dan's heading: wheels stay down. If the bump scan
  shows the same kind of seam at other junctions, list them for Dan, change nothing else.
- The HUD line at the top left: these three screenshots are from the exe built after the item 4 fix
  (Dan confirmed; build 17:28). The line is now drawn once with a drop shadow, as intended. Nothing more to do
  unless Dan reports it.

**Round 17 item 5 and Part B status (Claude Code, 2026-10-10).** No new debug session since
2026-10-10_17-47-11-485_46d8cb. Built and checked in the exe by script (muted, minimized 1280 x 720, check folders
only), not yet driven by Dan. Exe built 19:28. Check lists and results: `local/round17/partB/checks/` (git-ignored);
pictures in `local/round17/partB/` (git-ignored: some show Chastain Glen houses behind the center).
- BUG-001 DONE (`MapBuilder.Round17.cs`). The crease was a terrain step: where the lane's pieces cross New Chastain
  Road's carriageways, mapgen's ground switched from one piece's height to another's, 0.33 m in 1 m at (2937, 764).
  The old exe threw the Drifter Twin up at +12 m/s there. Fix, this junction only: the pavement smoothed (a 4 m
  Gaussian of the road surface's own heights, within 24 m of (2941, 756), largest change 0.27 m), in the map build
  before anything samples the ground. Exe, 35 m/s on Dan's line and three others, Drifter Twin and Street Classic:
  no wheel left the ground.
- BUG-002 DONE. Same kind of seam (up to 0.5 m) where Chastain Corner and Chastain Ridge cross New Chastain Road; the
  old exe hopped 0.24 to 0.60 s there (21 m westbound). Smoothed with 8 m within 30 m of (3152, 756) (4 m left a crest
  that still lifted the car westbound). Dan's LANDED point (3164.3, 767.2) is where a car coming south comes down
  off the crest on Chastain Corner at about (3162, 780), 25 m up from the junction (it lifted off there before and
  after the smoothing), so that crest was rounded too (cut and fill up to 0.11 m, along Chastain Corner only). Exe, 35
  m/s, both vehicles: north- and southbound 0 to 0.06 s, nothing lifts off in the junction. Left as it was: a crest on
  New Chastain Road 31 m east of the junction at (3183, 742), 0.2 to 0.3 s westbound at 35 m/s, the same before.
- Other junctions with the same kind of seam: listed for Dan, nothing changed (a terrain scan: largest crest or step
  against a 3 m chord on the pavement within 15 m of each of 443 junctions; 157 are 0.25 m or more). In or by Chastain
  Glen: Chasefield Way / Chastain Glen Lane at (3071, 1005) 0.51 m (checkpoint 1 is there); Chasefield Court /
  Chasefield Way (2990, 1070) 0.35 m; Chastain Glen Lane (3136, 1090) 0.30 m; Kensington Drive / Westminister Way
  (2816, 1252) 0.29 m; Chastain Corner Northeast / service (3275, 994) 0.28 m; Glenbarrett Lane (3064, 951) 0.25 m;
  Chastain Place (2731, 987) 0.25 m. Largest on the map: Big Shanty Road (609, 17) 1.5 m, Elaine Circle / Hayworth Drive
  (3004, 417) 0.9 m, Brickelberry Court (3305, 1656) 0.8 m. Some of these may be natural crests, not seams.
- BUG-003 DONE, with Part B. The ground south of the supermarket is paved and is one lot with the main one; the
  supermarket's south face has four shop fronts.
- Part B DONE (`MapBuilder.ShoppingCenter.cs`, `Assets/Scripts/ShoppingCenter/`). The lot (26,300 m2, outline by hand
  in `LotRing`): graded first (a 6 m smoothing of its own ground, faded out past its edge, never onto a road outside
  it), asphalt, a curb band where it meets grass, 502 painted stalls in double rows with 26 grass islands at the row
  ends, sidewalks in front of the shops (islands and sidewalks are 3.5 cm up and have no collider: nothing to snag a
  wheel). The map's service roads run through it as its drive aisles. Shop fronts (glass, a door, an awning, a sign)
  on the shop row (10), the supermarket (FRESH ACRES MARKET with a PHARMACY door) and its south face (4). Every name is
  made up (`Tools/handmade/make_store_signs.py` writes `Assets/HandMade/ShoppingCenter/StoreSigns.png`). The gas
  station: the canopy (QUIK FUEL on its fascia) on four columns (solid) over two pump islands on a level pad, four
  pumps that break like hydrants and then explode for show (`PumpBlast`: a flash, a fireball, flames, smoke and a
  bang; particles, a light and a sound only, no force, no damage; a drop-in `Sounds\Breaks\boom*` replaces the bang).
  The bank: drab walls, windows and door boarded up, "F RST CHAS AIN BA K", a FOR LEASE banner. No trees on the lot.
  Exe: the car broke two pumps at 15 m/s (both exploded, 3 % speed lost, kept going); a 30 m/s drive across the south
  lot into the main one: wheels down, 1 % speed lost.
- Imported from Racer: nothing.

**ACTIVE — Round 18: a shopping-center driveway fix, then the creek-woods track with pipe shortcuts (Dan,
2026-10-10: "we can fix these couple of things and pick up the next phase").**

Part A, commit on its own. Dan's session 2026-10-10_20-18-10-199_3f4ac2 (read by Claude):
1. BUG-001 (vehicle x 3263.31, z 1024.44, heading 224, at the south-west corner of the new south parking lot,
   looking south-west): "this drive way should enter the parking lot. the othe driveway should continue all the
   way around the building until it matches the other side of the parking lot". In his screenshot two service
   roads come up to that corner:
   a. The one ahead and to the left stops short of the lot with a strip of grass between its end and the
      asphalt. It should run into the parking lot: paved through, no grass gap, no curb or step across it.
   b. The other one (curving in ahead on the right, where the lot's edge is ragged against it) should carry on
      all the way round the back of the building, west side then north side, and come out into the main parking
      lot on the far side (the north-east end by the shop row), as the real service road does (Dan's picture in
      `local/reference_photos/maps/shopping_center_dan.png`, reference only). Drivable the whole way, on the
      ground, clear of the building and of Chastain Glen's back fences, joined flush to the lot at both ends.
   If it is not clear which road is which, make a labelled overhead and ask Dan; do not guess. Also tidy the
   ragged edge where the lot meets road b (seen by Claude in the screenshot). One check in the exe: drive from
   the south lot round the back of the building into the main lot without leaving pavement or losing a wheel.
   **STATUS (Claude, 2026-10-10): DONE, checked in the exe by script, not yet driven by Dan.** In the map build
   (`MapBuilder.ShoppingCenter.cs`, `JoinServiceRoads`; map.json untouched): a (OSM 564629091) runs 16 m on into the
   lot; b (OSM 564629093) goes on 127 m round the west side and north-west corner on the flat paved ground the
   elevation data shows there, to the road behind the shop row (564629095), and at its south end meets a; the shop
   row's east stub (564629086) runs 25 m down into the main lot. No curb band where a road meets the lot; edge
   paint stops on the lot and where these roads meet. Ragged edge: the lot's four corners along b moved onto b's
   edge and the ground there levelled to b; the corner where a, b and the lot meet smoothed (Round 17's junction
   smoothing, now `SmoothSpot`). a used to climb most of its 3.5 m in its last 15 m into a crest (a 0.1 to 0.3 s
   hop at 16 m/s): it now climbs on one smooth curve (12 % at most) from Chastain Corner's edge to 7 m into the lot.
   Exe check (`-exeShots` drives can now follow a path, `ExeShotsRunner`), Street Classic and Drifter Twin: round
   the back from the south lot into the main lot at up to 12 m/s, all four wheels down the whole way, at most
   1.3 m from the centerline (at the sharp turn at the shop row's east end); road a into the lot at 15 and 25 m/s,
   never airborne (at 25 m/s, slowed to 16 for the bend, two wheels go light for a moment at the top).

Part B, the track through the creek woods with pipe shortcuts. Dan's decisions are in the note "Its own round
after the shopping center" below; read it first. In short: a made-up dirt track along Noonday Creek through the
woods, built for racing (jumps, creek crossings; water slows and splashes), and big drainage pipes a vehicle can
drive through that link ponds / lakes / the creek as underground shortcuts.
2. PROPOSE FIRST, build nothing yet: a labelled overhead picture (git-ignored if it shows Chastain Glen houses)
   for Dan with (a) the track's line: where it leaves a road, where it rejoins, its length, each jump and each
   creek crossing; (b) each pipe: where it goes in, where it comes out, its length, what it cuts off; (c) which
   race it would belong to (a new route using the track, or a shortcut on the Chastain Glen loop). Say what is
   in the way (trees to clear, houses, slopes steeper than a vehicle climbs). Stop for Dan's answer.
   **STATUS (Claude, 2026-10-10): PROPOSED, waiting for Dan. Nothing built.** Overhead:
   `local/round18/creek_track_proposal.png` (git-ignored: it shows houses). A new race, "Noonday Creek", lap
   1.58 km: start / finish on New Chastain Road westbound (175 m, over the creek bridge), off the road west of the
   creek (x 2390) down the 3 m road bank, north up the creek's west side (J1 dirt tabletop at z 920, C1 splash
   across the side stream's mouth at z 1168), hairpin east across Noonday Creek (C2 ford at x 2306 z 1360, banks
   cut to a gentle slope), between the big lake and the small pond (C3 splash over the stream between the ponds),
   south down the east side (J2 log pile at z 1210, J3 dirt spoil heap at z 950), back up the road bank at x 2570.
   Five checkpoints. One pipe, P1: 71 m, 5 m concrete, from a headwall in the creek's east bank (x 2336 z 1300) to
   the small pond's west shore (x 2405 z 1316), coming out into the pond's shallow edge; skips the hairpin (CP3),
   about 300 m of track becomes about 200 m. In the way: 59 trees on the line; no house within 54 m; the woods
   are flat (277 to 279 m), so the only steep places are the road banks and the creek banks; the ground over P1
   (276 to 279 m) is too low for a 5 m pipe from the creek bed (274.6 m), so it needs a grassy bank 1.5 to 3.5 m
   high over it. Questions for Dan in the round report: the loop, the pipe and its bank, a second pipe only if the
   track goes round the big lake, where the race is picked (the race menu has no route choice yet).
**NEXT TO DO in Round 18 (Dan, 2026-10-10 23:21, after seeing the item 2 overhead). Three parts, in this order;
nothing of the track, pipes or new shortcuts is BUILT yet: this pass fixes two reports and puts markers in the
world for Dan to drive round and judge.**

2a. Dan's session 2026-10-10_23-08-26-878_0e77b1 (read by Claude). Fix both, commit on their own.
   - BUG-001 (vehicle x 3145.06, z 768.62, heading 92, on the verge at the New Chastain Road / Chastain Corner
     Northeast junction, the one smoothed in Round 17): "Cant we have this smoothed out?". Dan, asked which part: "There is an uneven spot in the grass
     you see as it is darker right there". It is the dark hollow in the grass verge just ahead and left of his
     vehicle in the screenshot (about 2 to 4 m in front of x 3145, z 769, against the curb of the corner): a
     dip in the ground left when Round 17 smoothed only the pavement. Fill and smooth that hollow so the grass
     runs evenly up to the curb. That spot only: not the bank across the road, not the pavement. Before / after
     picture from Dan's view for the report.
   - BUG-002 (vehicle x 3438.24, z 1229.82, heading 155, north end of the shopping center's main lot): "connect
     this as a roads. The drive ways and darking lots shoutl be the same color si they belind togethr". Two
     things: (i) the service road on his left ends in a rounded stub on the grass short of the lot: carry it
     on as a road into the parking lot, flush, no grass gap; (ii) the shopping center's driveways / service roads
     are a lighter gray than the lot: make them the same asphalt color as the lot so they read as one surface
     (the shopping center's own service roads and driveways only; public roads keep their look). Check every
     service road of the center for another stub that stops short of the lot and join those too (same place,
     same fault); list them.
   **STATUS (Claude, 2026-10-11): DONE, checked in the exe by script, not yet driven by Dan.** BUG-001
   (`MapBuilder.Round17.cs`, `FillVergeHollow`): the verge had a 0.8 m rise in 2 m at x 3148 with a trough at its foot
   and another along Chastain Corner's curb; within 5 m of (3149, 771.5), on the grass only (from 0.5 m past a road's
   edge), the ground is now the smoothest surface meeting the ground round it and the curb (raised up to 0.28 m,
   lowered up to 0.35 m); it rises evenly (about 0.2 to 0.35 m per 2 m) to the curb. Pavement and the bank across the
   road untouched. BUG-002 (`MapBuilder.ShoppingCenter.cs`): (i) the stub (OSM 564629097, the drive in from Canton
   Road) runs 24 m on into the lot's north-east corner, graded on one smooth curve like road a (13 % at most; the
   lot's grading had left a 0.14 s hop at 12 m/s, now none at 12 or 18 m/s); (ii) the center's service roads (the
   joined ones and every one running onto the lot) are drawn in the lot's own asphalt (material and color); public
   roads unchanged. Other stubs: none. Every other center road already reaches the lot; the drives south of Chastain
   Corner (564629063, 814479825 to 814479828) and east of Canton Road (768422193, 768422194, 1164024770) belong to
   other sites and were left alone. Before / after from Dan's two views: `local/round18/2a_bug00*_before|after.png`.

2b. Creek track: PROPOSALS A AND B, MARKED IN THE WORLD. Dan: "can code layout markers for me on the new trail
   proposal and different color markers for where the proposed pipe is. I would also say the river is on both
   sides of the road. We could start loop around jump the road to the other side and do a loop there as well
   potentially. Maybe do an a and b proposal laid out with markers that I can see easily?"
   - Proposal A: the loop already proposed (north of New Chastain Road, 1.58 km, J1 to J3, C1 to C3, pipe P1).
   - Proposal B: a figure of eight. Noonday Creek also runs south of New Chastain Road: a loop north of the
     road and a loop south of it, joined by JUMPING New Chastain Road (a big-air jump over the road from one
     side to the other; say how the lap gets back, a second jump or under the creek bridge). Work out the south
     loop from the ground and the creek there as A was worked out (what is in the way, houses, slopes), with
     its own jumps, crossings and at least one pipe candidate if the ground allows.
   - Markers, easy to see while driving and from a distance: tall thin posts or light beams every 20 to 25 m
     along each line, one color for A, a clearly different color for B (where A and B share a line, both
     colors), a third color for each pipe (a post at each mouth and a row over its run), and a distinct marker
     with a floating label at each jump (J1...), crossing (C1...) and checkpoint. Labels readable at 30 m,
     American spelling, sign rule applies. No colliders, nothing a vehicle can touch, no change to the terrain,
     no trees cleared: markers only.
   - Shown from the debug menu (a row: Proposal markers Off / A / B / Both; off by default), with a row to put
     the vehicle at the start of each proposal so Dan does not have to find it. Marked temporary in code and
     removed when Dan has chosen.
   - Also an updated overhead picture with A and B in the same colors as the markers (git-ignored if it shows
     houses), with lap lengths, and what B costs over A.
   **STATUS (Claude, 2026-10-11): MARKED, waiting for Dan. Nothing built.** Markers: `MapBuilder.Round18Proposals.cs`
   from `Data/proposals/round18_markers.json`, labels `python Tools/handmade/make_proposal_labels.py`
   (`Assets/HandMade/Proposals/ProposalLabels.png`), shown by `ProposalMarkers` (all temporary). Debug menu (F6 / `\`,
   or Start > DEBUG): "Proposal markers" (left / right: Off, A, B, A + B, Reverse shortcuts, All) and "Go to" (left /
   right picks Start of A, Start of B, R1, R2, R3; A goes there; free roam only). Posts every 22 m with a 24 m beam:
   A orange, B cyan, shared banded both, pipe purple; caps: jumps red, crossings blue, checkpoints green, starts
   white. Overhead `local/round18/2b_creek_A_and_B.png` (git-ignored). A as proposed, 1.58 km. B, 2.15 km: A's north
   loop without the road, then J4 jumps New Chastain Road southbound east of the creek bridges (kicker on top of the
   3.5 m road bank: 44 m and 2 s of air at 24 m/s, short of 22 m/s it lands on the road), a south loop down the creek's
   east side (a narrow strip along the hill toe, z 590 to 680, side slope 8 to 11 %), J5, C4 ford at the bottom, up
   the west side, J6, and back north in the creek bed under the two bridges (C5, 62 m in the water, about 6 m clear)
   and up the bank (C6). Not a crossing figure of eight: over the road once, under it once. B costs over A: the road
   jump, 42 more trees, two more cut creek banks, the slow water stretch, houses 29 m from the line (Claybrooke).
   South loop pipe: none the ground allows (under the creek the mouths would be 8 m down; the ponds sit behind a 5 m
   berm by the Lyndon Drive houses). P1 belongs to both.

2c. Reverse Chastain Glen loop: SHORTCUT PROPOSALS, MARKED THE SAME WAY. Dan: "I noticed there is now a reverse
   track of the original... It seemed to work pretty well for stragiht forward race. Can it show me some good
   spots for shortcuts. Since this track is so small the shortcuts shouldn't be huge, but let me emphasize that
   I want jumps with big air or at the very least something interesting and creative".
   - Three to five candidates for the REVERSE direction, short (the lap is 1247 m), each one either a jump with
     big air or something interesting and creative, not a plain cut across grass. Use what is there: the
     hills (the steep one by Dan's house), the pond and its docks, the pool, the creek ravine east of the loop,
     roofs, decks, the shopping center behind the neighborhood, the Round 6 candidate list
     (`Data/screenshots/round6/shortcut_candidates.md`) driven backwards. #2 and #6 are forward-only as built;
     say whether either works in reverse as it is.
   - For each: where it leaves the lap and rejoins, its length and what it saves, the air (height and distance
     at race speed, from a real trajectory, not a guess), what it would look like (the design rules: found not
     signposted, looks like something that could be there, a ramp's look fits its real size, never slows or
     traps a vehicle), which checkpoints it would forgive, and what is in the way. Houses are restored if
     touched; nothing is attached to house 1763.
   - Markers in the world in a fourth color with a label per candidate (R1...), on the same debug-menu row
     (add: Reverse shortcuts), and an overhead picture. Nothing built.
   **STATUS (Claude, 2026-10-11): MARKED (yellow, "Reverse shortcuts" on the same row), waiting for Dan. Nothing
   built.** Overhead `local/round18/2c_reverse_shortcuts.png` (git-ignored). Air is a ballistic flight over the real
   ground (g 9.81, no drag, as the game). Only the west lobe falls in the reverse direction; elsewhere a reverse cut
   climbs. R1 lobe drop: leaves past the bend before CP5, kicker on the natural 2.5 m bank between 1987 and 1552
   (33 m, 4.4 m high at 20 m/s; from 23 m/s it reaches 1572's roof, so sized for 20), out through the 6 m gap between
   2065 and 1572; saves about 84 m, skips CP5. R2 bike ramp: just after CP5, between 713 and 1052 (11 m), a plywood
   bike ramp by 1452, over the fence and a 2 m drop onto the lane (20 m, 2.5 m high at 15 m/s); saves about 23 m.
   R3 house run: off the top street past CP3, between 1602 and 1919, up the hollow, in through 1752's back and out its
   front (no air, climbs 7 m); saves about 30 m, skips CP2. Studied and dropped: a leap over the pond's arm (no shorter,
   and houses 1059 and 1128 block the way to the bank), Dan's hill and the north-east corner (they climb in reverse),
   the creek ravine and the shopping center (out and back, not cuts). #2 and #6 are forward only as built.

2d. Rebuild the exe (stay off Dan's screen, muted) and STOP for Dan: he drives round with the markers on and
   picks A or B, the pipes, and the reverse shortcuts. Report: the two fixes, how to turn the markers on, the
   two overheads, the candidates in a short table, questions.
   **STATUS (Claude, 2026-10-11): DONE; exe rebuilt, STOPPED for Dan's choice** (A or B, the pipe, R1 to R3).

**DAN'S CHOICES (2026-10-11 02:57): BUILD NOW. Items 3 to 9 replace the old items 3 to 6.**
Dan: "I want to go with the b track, we can go ahead and build the pipeline shortcut as well and once it is built
we can look for other potential shortcuts. The trail shortcut isn't tied to pipes." And: "As far as shortcuts on
the reverse I would like r1 built out. For r3 can we get a little more creative? I had this idea where you use a
slide from a play ground as a ramp which would bounce you onto a trampoline which would ultimately bounce you
over the house instead of driving through it". R2 was not chosen: do not build it.

3. Creek track B (2.15 km, as marked in cyan and on `local/round18/2b_creek_A_and_B.png`): build it. Graded into
   the terrain in the map build, a dirt surface, trees cleared off it and solid beside it, no snags; the north
   loop, J4 over New Chastain Road, the south loop, back north in the creek bed under the two bridges. Jumps look
   like something that could be there and their look fits their real size. Crossings are shallow water, always
   escapable. J4 must never punish: a vehicle short of the speed lands on the road and can simply drive on across
   and down to the south loop (no wall, no trap, no reset needed); say what speed each of the eleven vehicles
   reaches at J4 and whether each clears the road. The stretch in the water under the bridges is slow by nature:
   keep the water there shallow enough that it reads as a splash run, not a crawl (report the speed held).
   **A NARROW WOODS TRAIL, NOT A ROAD (Dan, 2026-10-11 03:04; overrides anything above about width).** Dan: "I
   don't want the trails very wide for this new track....perhaps even we restrict it to motorcycles and rtvs. I
   don't want to remove too many trees just enough to give rtvs enough room. It should feel like you are in the
   woods on a small trail, not on a road".
   - Width: sized for the ATV (Trail Four, 1.45 m wide), not for cars: a dirt tread about 2.5 m wide, trees
     standing right up to it on both sides and their crowns closing over it. Clear only the trees whose trunks
     are in that tread or would catch the ATV's body; move the line a meter or two round a tree before cutting
     it. Report how many trees were removed (the proposal's 59 + 42 were for a wide track: expect far fewer).
   - Feel: winding between trunks, roots and dips, short sight lines, dappled shade; no curbs, no verge, no
     painted lines, no cleared corridor. Jumps and crossings sized to the trail (J4 over the road stays big).
   - The "Noonday Creek" race is for the small vehicles only: the three motorcycles (Needle 600, Ridge
     Scrambler, Drifter Twin) and the ATV (Trail Four). Claude added the mower (Turf Rocket, 1.15 m wide)
     because it fits; Dan to say if not. The race menu offers only those for this route, for the player and the
     AI; cars are not offered. In free roam a car simply does not fit between the trees (nothing extra built to
     stop it, and nothing that traps one that tries: it can always back out).
   - Racing four on a single trail (Dan: "Vehicles can pass on jumps though or maybe squeeze around each other
     when possible"): do not widen the trail to make lanes. Passing happens (i) at the jumps: each jump's
     take-off and landing are wide enough for two side by side, so a faster vehicle can go past in the air or
     on the landing; (ii) by squeezing past wherever two small vehicles physically fit (two motorcycles fit in
     the tread; a motorcycle past the ATV where the trees allow), with the vehicle-to-vehicle contact as it is
     (a nudge, not a wreck, nobody shoved into a trunk and stuck); (iii) at the natural wide spots (the fords,
     the creek-bed run). The AI tries these and otherwise follows single file; it must not sit behind a slower
     vehicle for a whole lap, and it must not block the player on purpose.
   - Pipe P1 keeps its full size (all eleven vehicles), because the pipes become a map-wide network (Round 19).
4. Pipe P1 (purple): build it, UNDERGROUND. See "Pipes are their own layer" below: no grassy bank heaped over it;
   the pipe sits below the ground with a mouth at each end. Everything in the old pipe item stands: round concrete,
   wide enough for all eleven vehicles with room to spare, a headwall at each mouth set into the bank (no exposed
   back), lit enough to see, the camera kept inside the pipe, never a dead end or a place to get stuck, a reset
   inside puts the vehicle back in the pipe facing forward, found not signposted, forgives the checkpoint it
   skips, the AI can take it (trial each vehicle once through).
   Dan: other shortcuts on the track come after he has driven it, and they need not be pipes. Do not add any now.
5. A route "Noonday Creek" over track B so Dan can race it from the menu with the AI (one world, routes laid
   over it; the race menu gets a route choice).
6. Reverse shortcut R1 (lobe drop): build it as marked. The kicker on the natural bank, out through the gap
   between the two houses; sized for 20 m/s as proposed, and say what happens faster (it must not put a vehicle
   on a roof it cannot get off, or into a wall). Reverse direction only. Forgives the gate it skips. AI trial.
7. Reverse shortcut R3, REDESIGNED to Dan's idea (not the drive through the house; house 1752 stays whole and
   untouched): on the same line, a backyard playground. The vehicle drives up a playground SLIDE as its ramp,
   comes off the top onto a TRAMPOLINE, and the trampoline BOUNCES it over the house onto the lane beyond.
   - The trampoline is a real bounce: on landing it throws the vehicle up and forward (a bounce pad), with a
     springy look and sound. Tune it so every one of the eleven vehicles, arriving off the slide at race speed,
     clears the roof and comes down on its wheels on the lane, pointing down the lane.
   - It has to read at a glance as a playground slide and a trampoline (a play set tower with the slide, a round
     trampoline with a net or padded rim), but their look must fit their real size (the flatbed lesson): if a
     vehicle needs a slide 3 m wide and a trampoline 6 m across, build a big play set and a big trampoline that
     look right at that size, and report the sizes. Do not build a nearest version: if the slide-then-trampoline-
     then-over-the-house sequence cannot work on this line, stop and tell Dan why, with the numbers.
   - Never a trap: a vehicle that misses the trampoline, or comes in slow, lands in the yard and can drive out
     round the house back to the lap; nothing solid to wedge against.
   - Reverse direction only. Forgives the gate it skips. AI trial, each vehicle once.
   - Before the exe is rebuilt: two pictures for Dan (the play set and trampoline from the approach, and from
     the side with the flight path drawn), git-ignored if houses are in view.
8. Number checkpoints in the order the lap passes them in the direction being raced (reverse: first gate = 1),
   on the reverse markers / overhead and anywhere the game shows a number. Dan was confused by forward numbers
   on the reverse overhead. Remove the proposal markers for what is now built (A's orange markers go; B, P1, R1,
   R3 markers go once built); keep the debug-menu rows only if something is still proposed.
9. One targeted check per piece in the exe (stay off Dan's screen, muted), rebuild the exe, report: short status
   list, "for Dan to check", path to the exe.

**STATUS of items 3 to 9 (Claude, 2026-10-11): 3, 4, 5, 7, 8, 9 DONE, checked in the exe by script (muted, minimized 1280 x
720), not yet driven by Dan. 6 (R1) NOT BUILT: as marked it does not fit (below).** Exe rebuilt from a fresh map build:
`Builds/ChastainRush/ChastainRush.exe`. Check: `ChastainRush.exe -exeRound18 <dir> [-r18Only j4|pipe|r3|ai|race|pics]`.
Pictures for Dan (git-ignored: houses in view): `local/round18/3_*`, `4_*`, `5_*`, `7_*`, `8_*`, `noonday_creek_layout.png`.
- 3. Track B built as a narrow woods trail (`Tools/mapgen/make_creek_track.py` -> `Data/tracks/noonday_creek.json`,
  `MapBuilder.CreekTrack.cs`): the marked B line moved up to 2.5 m round trunks (a cheapest path, at most 0.2 m sideways per
  meter), a 2.5 m dirt tread (wavering, a darker wheel line, no tread on pavement or in water), graded into the ground (no
  steeper than 22 %, level across the tread; in the streams a gravel bar 0.15 m under the water). Trees removed: 64 off the
  trail and P1's ways in and out (trunks within 1.8 m of the line, wider at the jumps, fords and grid) + 5 in P1's cuts =
  69 (the wide proposal's estimate was 101; at 1.5 m the AI drivers kept catching trunks, Round 18 race check). J1, J5
  dirt tabletops, J2, J6 tabletops with logs under the lip, J3 a spoil heap (earth mounds, 5 m wide, two side by side). J4:
  a dirt kicker up the north road bank (28 m, lip 1.6 m over New Chastain Road, 8 to 14 degrees, the #6 dirt-pile look) and
  a landing slope graded down the south bank from the road's edge (9 %, 37 m). Speed at J4's lip from J3's landing flat
  out: Street Classic 28.9 m/s, Longroof GT 28.6, Needle 600 33.1, Trail Four 31.6, Sundown Roadster 29.2, Highball
  Fastback 29.8, Pebble Coupe 28.9, Skyfin Cruiser 27.9, Ridge Scrambler 32.1, Drifter Twin 31.6, Turf Rocket 33.1: all
  eleven clear the road (down 49 to 70 m past the lip). Short of the speed (Trail Four held at 14 m/s) it lands on the road
  and drives on down the slope: no wall, no reset. The creek bed under the bridges: the AI held 14.5 to 16.5 m/s averages
  there (slowest moment 7 to 13 m/s), a splash run.
- 4. P1 built underground (`Data/pipes/pipes.json`, `MapBuilder.Pipes.cs`, `DrainPipe.cs`): terrain holes over the two cuts
  ONLY (they work: the TerrainCollider and the URP terrain shader honor them in the exe), nothing heaped. Inlet cut 10 m
  (flared wingwalls, railings, coping) from a gravel ford across Noonday Creek, a 5 m round concrete pipe 44.5 m long under
  the ground (crown 0.6 m or more under it; floor 4.0 m wide, 4.0 m clear; 5 lamps), outlet cut 19 m up to the small pond's
  west shore (just outside the water). The way back to the trail runs up the dry slope west of the pond. Check: all eleven
  vehicles through the pipe; the camera stays in the pipe; a reset inside puts the vehicle on its floor, along it, the way
  it was facing; the two widest cars (Highball Fastback, Skyfin Cruiser) then stopped on the narrow trail just past the
  rejoin (cars do not fit the trail, as Dan wanted). AI: the five trail vehicles each through P1 cleanly (no recovery).
  Forgives CP2 (the hairpin). South loop pipe, re-judged with pipes as their own layer (a candidate, NOT BUILT): across
  the creek between the loop's two sides the span is 47 to 64 m (z 480 to 640; it would cut 115 to 467 m of lap), but
  the pipe's crown must pass 0.6 m under the creek bed (about 276 m), so its floor is 8 to 9 m under the 279 to 280 m
  banks and each cut is about 45 to 50 m long at 18 %: longer than the crossing. It only works as a long diagonal (for
  example from the east side at z 640 to the west side at z 520, about 125 m, two 45 m cuts and a pipe between). Dan to say.
- 5. "Noonday Creek" route (`Data/routes/noonday_creek.json`, 2145 m, 3 laps, 5 checkpoints, P1 official): the race setup
  menu has a Route row (left / right; saved in race_settings.json), Direction shows "Forward (one way only)" there, the
  Vehicle row offers only the Trail Four, the three motorcycles and the mower (cars are not offered; a player in a car is
  put in the Trail Four), the opponent vehicles page lists only those. The AI has a trail mode (lanes 0.25 m, 0.45 m to
  pass, close line following, trunks ignored by its obstacle sensor, a held speed over each jump). Check: three AI drivers,
  3 laps, all finished (5:21 to 5:46), 1 to 6 recoveries each, every lap through P1, longest right behind another 13 s.
- 6. R1 NOT BUILT. As marked (the kicker on the bank between 1987 and 1552, facing the gap between 2065 and 1572) the flight
  crosses that gap at an angle (the gap runs north-north-west): exactly on the line a vehicle at 24 m/s or more comes down
  into 1572's wall; 1.5 m or 6 degrees off it, from 22 m/s, into 1572 or 2065. Vehicles reach that kicker at 24 to 28 m/s
  (a straight run up from the corner), and the kicker itself has 6.6 m between 1987's and 1552's corners. A taller or steeper
  kicker only moves the band. The one robust lip found (every speed 14 to 32 m/s, 1.5 m and 6 degrees off the line, comes
  down on open grass): in the lobe's middle yard facing west-south-west, reached by a long left bend after the 1987 / 1552
  gap, rejoining on the lobe's west side. Not as marked: Dan to choose (that, or drop R1).
- 7. R3 built to Dan's idea (`build_r3`, `MapBuilder.Playground.cs`, `Trampoline.cs`); 1752 untouched. A giant play set in
  1752's back yard (a wooden tower, deck 4.0 x 3.5 m, striped canopy, ladder, closed underneath with lattice; a yellow slide
  3.6 m wide, its foot flared to 9 m, 16 m long, up 7 m) and a trampoline 13 m across the mat on a 1 m frame (padded blue
  rim, skirt to the grass), 13 m past the deck. The bounce aims every vehicle at one point on the lane past 1752 in 4.0 s
  (about 20 m up, 3 m or more over the roofs), turns it upright and down the lane in the air and catches its landing. Check:
  all eleven up the slide, bounced, down within 4 m of the aim, facing down the lane, upright, four wheels down at about
  17.7 m/s a second later; slow (5 m/s off the deck): lands short in the yard, no throw, drives away. AI: all eleven over it
  cleanly (the line's own speed profile, 11 m/s through the bend from the 1602 / 1919 gap). The two pictures (approach, side
  with the flight path) were sent to Dan before the final exe. Forgives file CP2 = the reverse lap's CP4.
- 8. Checkpoints in the game are numbered in travel order already (reverse: the reverse lap's first gate is CP1); the
  reverse overhead with those numbers and R3: `local/round18/8_reverse_loop_checkpoints.png`. The proposal markers, their
  data, labels and debug-menu rows are gone (nothing is still proposed in the world).

**Pipes are their own layer (Dan's question, 2026-10-11, answered by Claude; confirm in the build).** Dan asked
whether "too low" means the map has a floor we are stuck with, and whether to raise the whole map or treat the
pipe network as something that does not interfere with the world above. Answer: no floor, and no need to raise
the map. The ground is a height field (one height per point), so it cannot have a tunnel IN it, but nothing
stops a pipe being its own mesh with its own collider at any depth UNDER it. What a pipe needs is (a) a hole in
the terrain at each mouth (Unity terrain holes; the TerrainCollider honors them) with a headwall and a short ramp
or cut leading down, and (b) the pipe itself as separate geometry below the surface. The world above is untouched
between the mouths: roads, houses, trees and water carry on over the top. The Round 18 proposal treated the pipe
as something the natural ground had to cover, which is why P1 wanted a bank and the south loop had "no pipe the
ground allows". Build P1 this way, and re-judge the south loop pipe the same way (list it as a candidate; do not
build it). If terrain holes turn out not to work in this project (the map is 18 tiles; nothing uses holes yet),
stop and report before falling back to anything else.

**DONE, driven by Dan 2026-10-11 ("It definitely is a lot a better and fun to drive now"; his two reports and R1 are in Round 20) — Round 19: Noonday Creek and reverse-loop follow-ups (Dan, 2026-10-11 10:39, after driving Round 18).**
Round 18 (trail B, pipe P1, the Noonday Creek route, the R3 trampoline) is driven by Dan; what he did not mention
is approved. The principle for this round and from now on, in Dan's words: "This should be fun and arcadey not
too serious... The principle core is that this is a fun arcade game not a serious racer."

1. Noonday Creek trail, wider. Dan: "probably needs a little wider track, the vehicles aren't tight enough to
   weave through the trees... If we have to make trees breakable that is ok." (This replaces Round 18's 2.5 m
   tread; it is still a trail in the woods, not a road: no curbs, verges or lines, crowns still close overhead.)
   - A tread about 4.5 m wide; no solid trunk within about 3.5 m of the trail's line (wider on the outside of
     bends, at take-offs and landings). Report the width chosen and the trees removed.
   - Breakable trees: the small trees near the trail that are left (within about 6 m of the line) snap when
     hit (a wood crack, the tree falls and fades, a little speed lost, as fences and mailboxes), so clipping one
     is a laugh, not a dead stop. The big trunks further out stay solid. Along this trail only; trees elsewhere
     on the map are unchanged.
   - The race stays for the small vehicles (the three motorcycles, the ATV, the mower) unless Dan says otherwise.
   - The AI's trail mode retuned to the new width: real passing, far fewer recoveries (Round 18: 1 to 6 each).
2. Noonday Creek trail, big jumps. Dan: "I feel like we should also add some big jumps too." Round 18's jumps
   are about 1 m tabletops. Add at least four BIG ones, two seconds of air or more at race speed, spread round
   the lap, each a different idea and each looking like something that could be in those woods at its real size
   (for example: jump the creek instead of fording it, off an earth bank; a huge fallen tree as a take-off; a
   washed-out wooden footbridge with the middle gone; a step-down off the top of a bank; a jump over the small
   pond's arm). J4 over the road stays. Each with a real trajectory for every trail vehicle (lip speed, height,
   distance), a wide take-off and landing (passing happens here), and never a punishment: short or slow lands
   somewhere drivable (shallow water, a slope), never a wall, a pit or a reset. The AI takes them.
   Keep the small ones where they give rhythm. One picture per big jump from the approach for the report.
3. BUG-001, session 2026-10-11_10-16-10-265_c7b248 (vehicle x 2329.60, z 1297.26, heading 76, in Noonday Creek
   facing P1's inlet): "Can we make it so the water doesnt slow you down for the shortcut...maybe put some rocks
   here or something?" The way into P1 crosses the creek and the water drags. Make the crossing to the mouth dry
   and fast: a shelf of flat rocks / a rock bar across the creek on the shortcut's line, its top just above the
   water, wide enough for the ATV with room, smooth to drive (no snags, no bouncing), water either side. It should
   look natural (creek rocks), and it doubles as the hint that something is there.
4. BUG-002, same session (vehicle x 2357.49, z 1304.16, inside P1): "I want the inside to look a little more used
   and dark, have a small stream of water in it. Also can we have the ability so that you could drive around the
   walls?so if you had enough momentum you could even circle them or ride the walls". Three things, and they
   become the standard for every pipe in the network (Round 20):
   a. Look: used and dark. Stained, streaked concrete, grime and moss low on the walls, joints between pipe
      sections, a tide line; fewer and dimmer lamps with dark between them (the vehicle's own lights carry it),
      still enough to see the way. Not clean and evenly lit as now.
   b. A small stream down the middle of the floor: a thin ribbon of moving water with a trickle sound and a small
      splash from the wheels. It never slows a vehicle.
   c. Ride the walls: the inside is a true round pipe to drive on, not a flat floor between walls. Steering up
      the side at speed carries the vehicle up the wall; with enough momentum it can go right over the top and
      round (a full circle), and it comes back down when it slows. Fun first: generous, not a physics exam (hold
      the vehicle to the pipe's surface while it has the speed, let go gently when it does not). It must never
      trap, flip or wreck: a vehicle that falls off the wall or roof lands on its wheels in the bottom and
      keeps going; the camera follows without spinning the player sick and stays inside the pipe; a reset still
      puts it in the bottom facing forward. All eleven vehicles; the AI just drives the bottom.
   Also check, seen by Claude in both screenshots: the HUD shows "Penalty +5 s (1 missed)" while Dan is going
   into P1 on lap 3. P1 is an official shortcut that forgives the hairpin's checkpoint: find out where that
   penalty came from (his lap, not a script) and fix it if P1 or its creek crossing charged it.
5. R3 (the play set and trampoline), a clear hint at the entrance. Dan: "Second shortcut the entrance needs to
   be more obvious. I know I said hidden but I think especially in this neighborhood there is too many places a
   shortcut can be so a clear hint where it is would be good". DESIGN DECISION CHANGED (Dan, 2026-10-11):
   shortcuts in the neighborhoods get a clear hint at the turn-off. Still something that could be there, not a
   game arrow or a blue arch: for R3, a kid's birthday party: a bunch of balloons and a hand-painted "PARTY"
   yard sign with an arrow at the gap between the two houses where the line leaves the street, worn tire tracks
   in the grass leading through the gap, bunting or balloons on the play set so it draws the eye from the lane.
   Readable flat out from 60 m in the reverse direction (sign rule applies). Breakable, never solid.
   Then list, for Dan, what the other shortcuts show at their turn-offs (#2 has its SHORTCUT sign; #6; R1 when
   built) and what hint each could get; change only R3 now.
6. R1: BUILD THE ALTERNATIVE (Dan, 2026-10-11 10:42: "well go ahead and try that one shortcut and we will see").
   Round 18 found the marked line unsafe (the flight comes down into a house wall at race speed) and found one
   robust version: a kicker in the west lobe's middle yard facing west-south-west, reached by a long left bend
   after the 1987 / 1552 gap, landing on open grass at every speed from 14 to 32 m/s, rejoining on the lobe's
   west side. Build that one, reverse direction only. Make it worth taking: as much air as the yard safely
   allows (say the height and distance per vehicle), a look that fits its real size, a clear hint at the
   turn-off (the shortcut-hints rule), never a trap (short, slow or off line lands on grass and drives out).
   Forgives the gate it skips. AI trial, each vehicle once. It is a trial: Dan will drive it and decide, so keep
   it easy to remove. One picture from the approach with the flight path drawn, for the report.
7. One targeted check per piece in the exe (stay off Dan's screen, muted), rebuild the exe, report: short status
   list, "for Dan to check", path to the exe.

**STATUS of Round 19 (Claude, 2026-10-11): items 1 to 7 DONE, checked in the exe by script (muted, minimized 1280 x 720),
not yet driven by Dan.** Exe rebuilt from a fresh map build: `Builds/ChastainRush/ChastainRush.exe`. Check:
`ChastainRush.exe -exeRound19 <dir> [-r19Only trail,jumps,bar,pipe,penalty,r3,r1,ai,race,pass] [-r19Vehicles ids] -logFile <dir>\Player.log`
(Checks/Round19Runner*.cs). Pictures (git-ignored, houses in view): `local/round19/`. Imported from Racer: nothing.
- 1. Trail wider (`make_creek_track.py`, `MapBuilder.CreekTrack.cs`): tread 4.5 m, graded level across it, blending out 2.4 to
  4.6 m from the line; no solid trunk within 3.5 m of the line (more on bends: + 30 / radius, up to 1.5 m; 4.5 m at the jumps,
  fords and grid). Trees removed: 250 off the trail, the big jumps' flights and P1's ways in and out (Round 18: 69). The trees
  left within 6 m of the line (and within 8 m of each big jump's straight run-out) are breakable: 142 (TreeField.breakable,
  TreeTrunks trigger capsules, FallingTree: a wood crack, 5 % of the speed, the tree falls away and shrinks; back at race
  start). Check: 0 solid trunks within 3.5 m (nearest 6.0 m); the ATV snaps one and drives on. The AI's trail settings
  retuned (half width 2.25 m, lanes 0.7 m, passing lane 1.4 m) and its passing fixed: on a trail it now passes on the side away
  from the slower vehicle (it used to pass on the side away from its own lane and never went when that side was taken).
  Race check (3 AI, 3 laps): all finished 5:00 to 5:12 (Round 18: 5:46 to 6:15), 0 recoveries each (Round 18: 6 to 9),
  0 penalties, 6 place changes, longest right behind another 5 s; every lap through P1.
- 2. Four big jumps (`MapBuilder.BigJumps.cs`, `BigJump.cs`), each a different idea, on straights: BJ1 a washed-out wooden
  footbridge over the side gully (the near span's broken end heaved up to 23 degrees, splintered planks; the far span lies
  collapsed down the far bank, so short lands in the gully and drives up it); BJ2 a dirt lip on the top of the bank down to
  the creek flats; BJ3 an earth ramp heaped over a huge fallen tree (1.6 m log, root plate up, top tapering into the woods);
  BJ4 a granite slab tilted out of the hump on the south loop. Crossing the lip a vehicle leaves at the lip's own angle at its
  speed, no faster than 27 m/s (BJ2, BJ4: 24, the trail bends soon after) (BigJump: an aimed launch; flat out the trail
  vehicles arrive at 30 to 39 m/s, too fast for a short curved take-off to throw them at its real angle). Every trail vehicle
  flat out: BJ1 2.30 to 2.37 s in the air, 56 to 58 m, 6.2 to 6.7 m up; BJ2 2.12 to 2.13 s, 48 m, 4 m; BJ3 2.47 to 2.50 s,
  60 m, 7.4 m; BJ4 2.23 to 2.38 s, 49 to 52 m, 5.5 m; all upright and on. Slow (12 m/s) at each: lands 14 to 17 m past the lip
  and drives on. J3 gave way to BJ3; J2 (63 m before BJ2) is gone: flat out it threw vehicles across the bend past BJ2's
  kicker. J1, J5, J6 and J4 stay. Landing zones (RampHold's carry) on all four. The AI holds 24 m/s into them.
- 3. BUG-001: P1's way in crosses Noonday Creek on a rock bar (`pipes.json` `rock_bar_a`, `MapBuilder.Pipes.cs`): 6 m wide,
  its top 0.12 m over the water, water either side, flat creek slabs on it and creek rocks either side; a DryPatch over it
  (ShallowWater: not wading there). Check: every trail vehicle over it at 15.4 to 15.8 m/s (the line's 16), wading 0.
- 4. BUG-002, the standard for every pipe (`MapBuilder.Pipes.cs`, `DrainPipe.cs`, `PipeStream.cs`): a true round pipe (no flat
  floor; its last 6 m of each cut turn into the round bottom, so no lip at the headwall); P1's ramps 28 % (18 % before) to keep
  42 m of round barrel. Stained, darker concrete (a texture made in the build: joints, streaks from the crown, grime and
  moss low down, a tide line, the wet invert), lamps every 16 m and dim, the vehicles' headlights on in a pipe
  (VehicleLights). A stream down the middle (opaque glossy ribbon, its water sliding, a trickle sound from the nearest point,
  splashes at the wheels; nothing slows: slowest 14.7 m/s through at 16; its sound first sat on the ribbon itself and dragged it out of the pipe: fixed in the last build). Riding the walls: in the barrel a vehicle over
  7 m/s is held to the wall (its gravity turns to point out from the axis, fully from 11 m/s), so steering up the side takes
  it up and round; slower it slides down (a push so the tyres' grip cannot hold it parked on a steep wall), is righted in the
  air, and anything lying overturned or parked on the wall for 1.5 s is put back in the bottom facing along the pipe. The
  camera rides the pipe's axis behind the vehicle, world up (ChaseCamera.PipeView): in the pipe in every check. Scripted wall rides (14 m/s in, steering up the side): the motorcycles and the mower climb past the side and right round (360 degrees or more; 2 to 8 of the eleven, run to run), most cars climb 40 to 70 degrees and come back down (turned steeply across a 5 m pipe a 4.5 m car's nose and tail meet the rising wall; on a shallower spiral they climb further). Never a trap: left parked up the wall at 60 degrees with no input, all eleven are back in the bottom on their wheels within 4 s; a reset in the pipe puts the vehicle in its bottom along it.
  The penalty Dan saw: his race log from that session was overwritten, so not found from his lap; P1 checked three ways (on
  its line, a reset in the creek at its mouth, a reset in the pipe): no penalty, CP2 forgiven, P1 counted, CP3 passed. Not P1;
  nothing changed for it.
- 5. R3's hint (`MapBuilder.PartyHint.cs`, `make_party_sign.py` draws `Assets/HandMade/Shortcut/PartySign*.png`, hand-made,
  committed): a 3.2 x 1.6 m hand-painted PARTY yard sign with a big arrow toward the gap (capitals 0.67 m), seven balloons on
  its post, in the grass strip at the turn-off facing reverse-lap drivers; worn tire tracks through the gap to the slide;
  bunting and balloons on the play set. Sign and balloons break (FenceChunk). Pictures (1280 x 720): from about 40 m it reads; from about 100 m the balloons and the bright board show (a true 60 m picture was not managed; not checked at 3840 x 2160).
  Other shortcuts' turn-offs, for Dan (unchanged): #2 has its SHORTCUT sign (could add worn tracks); #6 shows only its dirt
  pile from the lane (could get a "FILL DIRT" landscaper's sign or worn tracks); P1's rock bar is its own hint (could add worn
  tracks off the trail); R1 (new) has a JUMP! sign and worn tracks.
- 6. R1 built as a trial, reverse only (`make_shortcuts.py` `build_r1`, kind "kids_ramp"): out of the 1987 / 1552 gap, 18 m,
  a left arc of 20 m, then straight to the neighborhood kids' giant homemade ramp (plywood on a lumber frame, cinder blocks;
  2.8 m high, 25 degrees at the lip, 11 m long, 6 m wide) at (2750.1, 1029.5) facing west-south-west, 22 m further along the
  same line than Round 18's spot: there the ramp stood across #6's line (1.2 m from it); here 9.2 m clear. Lip speed held to
  20 m/s (faster would carry it over the lap's road toward house 2088). All eleven vehicles: 2.10 to 2.22 s in the air, down
  38 to 41 m past the lip on the yard or the road's near edge, 4.2 to 4.3 m up, upright, on onto the lap; slow (8 m/s) drives
  out. Forgives the reverse lap's CP1 (file CP5). Hint: a JUMP! sign and worn tracks. NOTE for Dan: it lands heading about
  90 degrees to the lap (the road bends there), so a driver must brake and turn right after landing; straight on is house
  2088's front yard. And it is about as long as the lap it replaces (137 m against 140 m): a jump, not a time saver. To remove:
  drop build_r1() from `make_shortcuts.py`'s list, re-run it, Build Map.
- 7. AI trials: each trail vehicle through the new round P1, each of the eleven over R1: 16 of 16 clean. Exe rebuilt.

**ACTIVE — Round 20: two fixes, the map, and two sets of proposals (Dan, 2026-10-11 17:51).**

1. Dan's session 2026-10-11_17-41-40-815_db3e5c (read by Claude). Fix both, commit on their own.
   - BUG-001 (vehicle x 2332.31, z 1168.69, heading 351, on the dirt run-up at the near end of the wooden
     footbridge, BJ1): "there is a catch right here that brings the bike to a stop". Where the dirt meets the
     bridge's first planks something stops a motorcycle dead (a step up onto the deck, a plank end, a rail post
     or a collider edge). Find it by driving Dan's line at race speed on the Drifter Twin, remove it: the trail
     runs onto the deck flush, nothing for a wheel or a body to catch, the rails clear of the line. Then drive
     the whole lap once on each trail vehicle looking for any other dead stop and list what turns up.
   - BUG-002 (vehicle x 2395.32, z 1313.10, heading 85, in P1's outlet cut looking out over the small pond):
     "Lets make a ramp right here so that you don't have to drive through the lake". Coming out of the pipe the
     cut runs into the pond and the water drags. Build a ramp at the pipe's outlet that launches the vehicle out
     of the cut and OVER the pond to the far shore, back onto the trail: big air, fun (it is the reward for
     finding the pipe). It looks like something that could be there at its real size (a concrete spillway lip
     / an outfall apron turned up, or creek rock and earth heaped at the mouth). Every vehicle that can come
     through P1 clears the water at the speed it leaves the pipe (use an aimed launch as BigJump if needed);
     slow or short lands in the shallow edge and drives out: never a trap. A clear landing (no trunks; breakable
     trees as on the trail). Report air time, height and distance per vehicle. The AI takes it.

2. The map, as in Woodstock Rush. Dan: "I also want to look at adding the maps like we have in Woodstock Rush (it
   is mapped to the button that we are currently using for F4) The maps will help me also look for shortcut
   ideas". Read how Racer does its map (the code, the controller button, what it shows, how it opens and closes,
   zoom / pan if any) and port it (prefer porting Racer's code; list what came with it). In Chastain Rush:
   - On the SAME controller button as in Woodstock Rush. That button is the one this game uses now for bug
     capture: the map takes it; bug capture stays on F4 / `]` and on its debug-menu row (and gets another
     controller button only if one is free and it does not clash; say which). Say what the keyboard key is.
   - It shows the whole world from above, north up: roads, the Noonday Creek trail, water (ponds, lakes, creeks),
     woods, buildings, the shopping center, bridges; the player (position and heading), the other racers in a
     race, the current route with start / finish and checkpoints in travel order. Because Dan will use it to hunt
     for shortcut ideas: zoom and pan with the controller, a readout of the map x, z under the cursor or the
     player, and (on a toggle, off by default) the existing shortcuts and pipes. The game pauses in free roam
     while it is open; in a race follow what Racer does.
   - "© OpenStreetMap contributors" visible on the map. American spelling. Controller first, B closes.
   - One check in the exe: open it with the controller in free roam and in a race, zoom, pan, close.

3. R1, the ORIGINAL line, with a house slightly altered: PROPOSE, build nothing. Dan: "Can we suggest going with
   the original idea for the first jump on the reverse slightly altering the house so that it will work?" (Houses
   are not set in stone; only Dan's own is locked; Dan makes the final decision.) The original R1: the kicker on
   the natural bank between 1987 and 1552, out through the gap between 2065 and 1572, 33 m and 4.4 m up at
   20 m/s, saving about 84 m. It failed because vehicles reach the kicker at 24 to 28 m/s and the flight crosses
   the gap at an angle into 1572's (or 2065's) wall. Work out the smallest change to a house that makes it safe
   at every speed a vehicle can arrive at (14 to 32 m/s, 1.5 m and 6 degrees off the line): for example moving
   or turning 1572 a few meters, shortening it, taking off a wing, or lowering its roof so the flight clears
   OVER it. Give two or three options, each with: which house, exactly what changes (meters, degrees), what the
   house looks like afterwards (it must still look like a normal house: no hole, no ramp stuck on it), the air
   and the saving, where it lands. An overhead and one picture from the approach per option (git-ignored). The
   trial R1 (the kids' ramp) stays as it is until Dan chooses; say whether the original would replace it.

4. Noonday Creek shortcuts: PROPOSE, build nothing. Dan: "let it give us some suggestions for short cuts on
   noonday creek". Four to six candidates round the 2.1 km lap besides P1, each big air or creative (arcade, not
   serious), not necessarily pipes: for example a long diagonal pipe on the south loop (Round 18's candidate),
   cutting a loop's corner over the creek by a jump, through the big lake's shallows on a line of rocks, off the
   road bridge instead of under it, a line up and over a bank the trail goes round. For each: where it leaves
   and rejoins, length and seconds saved, the air per trail vehicle from a real trajectory, what it looks like
   and its hint at the turn-off (the shortcut-hints rule), which checkpoint it forgives, what is in the way.
   Mark them in the world the way Round 18 did (posts and labels, a debug-menu row, a "go to" row) AND on the
   new map (item 2), plus an overhead picture. Dan picks.

5. One targeted check per built piece in the exe (stay off Dan's screen, muted), rebuild the exe, STOP for Dan's
   choices on items 3 and 4. Report: short status list, "for Dan to check", the options in short tables, path to
   the exe.

**STATUS (Claude, 2026-10-12): items 1, 2 and 5 built and checked in the exe by script (minimized, muted), not yet driven by
Dan; items 3 and 4 proposed, nothing built. STOPPED for Dan's choices.** Exe rebuilt from a fresh map build. Check:
`ChastainRush.exe -exeRound20 <dir> [-r20Only bj1,ramp,map,marks,lap] -logFile <dir>\Player.log` (Checks/Round20Runner*.cs;
a watchdog ends it after `-r20Minutes`, default 25). Pictures: `local/round20/` (git-ignored). Imported from Racer: the map
(below).
- 1. BUG-001 DONE (commit a9eb434). The catch was the left railing's first post, facing the approach: a vehicle 2 m off the
  line hit it head on and stopped (Trail Four, Turf Rocket). The near ends now flare out (wing rails, 0.8 m wider at the
  deck's end) and only a smooth panel along each railing is solid; the bridge is 7 m wide (rails 3.4 m off the line); the
  deck starts at the lowest ground across its width (it stood up to a few cm proud on one side). Exe: the five trail
  vehicles flat out (31 to 37 m/s), on the line and 1, 2 and 2.5 m either side: all keep their speed.
  BUG-002 DONE (same commit). A concrete outfall apron at P1's mouth (7 m, turned up to 24 degrees, lip 1.5 m over the
  shore, 8.5 m wide, sloped concrete sides and back, creek rock either side) launches along the pipe's axis over the
  small pond (BigJump with 'vmin': 23.5 to 26 m/s off the lip; under 10 m/s it rolls off into the shallow edge). P1's way
  out now goes over the pond and onto the trail's east side (rejoins at s 645; brake and turn right after landing). Exe:
  all eleven 2.28 to 2.43 s in the air, 53 to 57 m, 6.3 to 6.6 m over the lip, dry landings, on to the trail; Trail Four
  at 8 m/s rolls off and drives out; AI 5 of 5 (lip 16 m/s, boosted: 2.0 to 2.1 s, 41 to 43 m).
  The lap on each trail vehicle (scripted, flat out): the Drifter Twin clean (86.6 s). Every other stop was a solid trunk
  8.6 to 13.5 m off the line where the script ran wide on a bend: s 555 (the C3 stream bend), s 725 to 733 (the east leg
  before CP3), s 1549 (the C4 ford); plus a hard landing off J1 at s 2124 (ground, not a catch). NOT changed (not where Dan
  pointed); option for Dan: make the trees within 14 m of those three bends breakable.
- 2. The map DONE. Racer's ExplorationMap (+ .Controls) and WorldMapCourseOverlay ported as `Assets/Scripts/Map/WorldMap.cs`
  and `WorldMapOverlay.cs`: the same button (View / Select; keyboard M), pauses the game (in a race too, as Racer: the race
  clock stops), left stick / WASD pan, triggers / = - zoom (1 to 16x), right stick click / Home centers on you, A / Space
  shows the shortcuts (every route's) and pipes (dashed, off by default), B / Esc or View / M close, mouse wheel and drag.
  The picture is drawn from the map data at Build Map (`MapBuilder.MapPicture.cs`, `Assets/Generated/Resources/
  WorldMapPicture.png`, 4096 x 1972, about 1 m a pixel: relief, woods and trees, water, streams, roads, bridges, drives,
  the shopping center's lot, buildings, the Noonday Creek trail; Racer photographs its scene from above, which here would
  hide the trail under the trees). Over it: the current route with START / FINISH and checkpoints numbered in travel order,
  the player's arrow, the other racers in a race (name and dot), a readout of x, z under the cursor and at the player,
  "© OpenStreetMap contributors", Round 20's proposals. The race HUD and debug panel are hidden while it is open. Left
  behind: exploration fog and its save, destinations and travel, waypoints, acorn areas, track browsing, the action sheet.
  Bug capture moved to the right stick's click (free while driving); F4 / ] and its debug-menu row unchanged. Exe (virtual
  gamepad): free roam and a race, open, zoom, pan, layers, center, close: all pass.
- 3. R1 original line: PROPOSED (table in the round report; `Tools/mapgen/r1_original_options.py`, pictures
  `local/round20/r1_options.png`). Tested at 14 to 32 m/s, 1.5 m and 6 degrees off the line, aimed launch, 30 m straight
  on after landing. No change to one house makes it safe (1572 moved up to 5 m before it reaches 1622, turned up to 40
  degrees either way, cut back; 2065 moved up to 12 m). The one safe layout: 1572 cut back to its north-east half (206 to
  117 m2) AND 2065 moved 8 m south-west. The trial R1 (kids' ramp) stays until Dan chooses.
- 4. Noonday Creek candidates N1 to N5: PROPOSED (`Tools/mapgen/creek_shortcut_candidates.py`, overhead
  `local/round20/creek_candidates.png`). Marked in the world (magenta posts and labels; debug menu "Proposal markers" and
  "Go to", free roam only; `MapBuilder.Proposals.cs`, `Debug/ProposalMarkers.cs`, `Data/proposals/round20_markers.json`,
  labels `Tools/handmade/make_proposal_labels.py`) and on the map. TEMPORARY: remove when Dan has chosen.

**Round 20 item 4 decided (Dan, 2026-10-12 02:03): build N2 and N5; drop N1, N3, N4.** Dan: "N2 is the only one
that even makes sense, the others cut off too much of the track well besides N5, since it is already being skipped,
though we could put in N5 if you miss the pipe shortcut".
- N2 "Low creek leap": build as proposed (`local/round20/creek_candidates.json`): off the washed-out east bank
  (creek rock and earth bulldozed up, lip about 1.8 m, 24 degrees, aimed launch held 23 to 25 m/s, about 2 s and 42
  to 50 m of air), over the creek to the west side; its hint is the knocked-over road-closed barricade and tire
  tracks over the bank. Forgives the checkpoint it skips (CP4). Never a trap: short lands in the shallow creek and
  drives out. AI trial, each trail vehicle once.
- N5 "Ford leap": build it as the jump for those who MISS P1: a log ramp beside the hairpin's ford (C2) that jumps
  Noonday Creek instead of wading it (lip about 1.4 m, 20 degrees, held 18 to 22 m/s, about 1.5 s). It sits on the
  normal lap (P1 skips that stretch), so it is part of the lap, not an official shortcut: it skips nothing and
  must not change how CP2 is counted. Worn tracks to it. The AI takes it when not taking P1.
- N1, N3, N4: not built. Remove all the Round 20 proposal markers, their data, labels and debug-menu rows, and the
  proposals layer on the map, once N2 and N5 are built.
- Still open for Dan (not to be done unasked): making the trees breakable at the three bends where the scripted lap
  hit trunks 9 to 14 m off the line (the stream bend, the east leg before CP3, the south ford).

**DECIDED (Dan, 2026-10-12 02:11): Dan's line becomes R1, but with risk. The R1-over-1572 item below is CANCELLED (do not build it).**
Dan's session 2026-10-12_01-54-20-969_70e57b, reverse Chastain Glen loop, Turf Rocket, lap 1, after the reverse
lap's first gate: BUG-001 (vehicle x 2804.04, z 1018.74, heading 333, on the trial R1's worn tracks between the two
houses): "why not start here...."; BUG-002 (vehicle x 2774.91, z 1079.09, heading 7, coming out onto Chastain Glen
Lane on the lobe's north side): "and come out here? I mean there is ltiterally nothing stopping you and since there
is no cp, whats the motivation of not taking this?" He drove straight on through the gap and across the yards to
the lane, cutting the west lobe, with no barrier and no checkpoint to stop it, and no penalty. Claude has asked
Dan to choose: (A) make his line the official R1 (with a jump on it), replacing the kids'-ramp trial and the
one-story-1572 plan; or (B) keep a planned R1 and stop free cuts like his (a checkpoint inside the lobe, or a fence).
Either way, scan both directions of the Chastain Glen loop for other free cuts through yards that skip no
checkpoint, and list them for Dan.

Dan: "yeah but it is too easy. I don't mind this area as a shortcut, but it has to has some risk". So:
- R1 is Dan's line: from the gap between the two houses after the reverse lap's first gate, across the lobe's
  back yards, out onto Chastain Glen Lane where his BUG-002 was. Reverse only. Official: forgives the gates it
  skips. It replaces the kids'-ramp trial (remove that ramp, its JUMP! sign and its tracks).
- It must carry RISK, so that taking it is a gamble that pays off for a clean run and costs time for a sloppy one.
  Make the yards a real obstacle: the lobe's back yards fenced off with the neighborhood's privacy fences (solid,
  not breakable, as on a property line), so the ONLY way through is the shortcut itself, and the shortcut asks
  for commitment. Default design (Claude's; build it unless it cannot work, then say why): a homemade ramp over
  the first fence into a back yard with an in-ground pool. Hit it right and you clear the pool and land on the
  deck / lawn beyond; come in slow, short or off line and you drop into the pool: deep enough to cost real time
  (well more than the shortcut saves), never a trap (pool steps at the shallow end, drive out, a reset is fine).
  Then out through a narrow gap: an open side gate in the far fence, a little wider than a car, that you have to
  line up for; clip the gate post and you lose speed. Breakable things in the yards (patio chairs, a grill, an
  umbrella) for fun.
- The hint at the turn-off as the shortcut-hints rule (worn tracks and something homemade at the gap).
- Numbers for the report: the time saved on a clean run, the time lost in the pool, the speed window for clearing
  it per vehicle. Every vehicle can make it clean (aimed launch if needed, as BigJump); the risk is in the driver's
  line and speed, not in the vehicle. AI: takes it, makes it clean most of the time, sometimes not.
- One picture from the approach and one overhead with the flight drawn, for the report.
- Then scan both directions of the Chastain Glen loop for other free cuts through yards that skip no checkpoint and
  cost nothing, and list them for Dan (change nothing else).

**R1 POOL CHANGED (Dan, 2026-10-12 03:51, watching the check run; overrides the pool in the R1 decision above).**
Dan: "I can already tell I don't like the pool placement between two houses: 1) It isn't realistic, if someone had
a pool it would be in the back yard not in the area between them and their neighbor 2) If the pool that close to
the jump, it takes out most of the challenge. If the pool is further back it would be riskier (which is what you
want) 3) Also I would prefer it was an above ground pool instead. We can still make the exit the same you would
just not go straight out and be on the ground you would drop out of it".
- The pool moves into a BACK YARD, behind the house, where a pool would really be, not in the side yard between
  two houses.
- It sits further from the kicker, so clearing it takes a committed, well-aimed, fast jump: that is the risk.
  Coming in slow, short or off line lands you in it. Report the speed window and how much a clean run saves
  against how much the pool costs.
- An ABOVE-GROUND pool (a round or oval steel-walled pool, about 1.2 m high, blue liner, a ladder, perhaps a
  small deck on one side), not in-ground.
- The exit is the same (out through the narrow side gate in the far fence). Claude's reading of "you would drop
  out of it": a vehicle that lands in the pool does not drive straight out level onto the grass; it gets out over
  the far side and drops down to the ground (for example up the pool's deck / steps on the side toward the gate
  and off its edge, or the far wall gives way), then on to the gate. Never a trap: every vehicle can always get
  out that way, with no reset needed. A clean jump clears the whole pool and lands on the grass beyond.
- Breakable patio furniture etc. as before.

**Also in this pass (Dan, 2026-10-12 09:22): the AI shortcut rule.** Now a standing rule in CLAUDE.md ("How the AI
chooses shortcuts"): usually the regular path, sometimes a shortcut, more likely when behind, more likely still the
further behind. `ShortcutStrategy.Probability` already does this, but the new R1 has `"ai_take": 0.85` in
`make_shortcuts.py` / the route data, a fixed 85 % that overrides the rule (the AI would take R1 almost every lap).
Remove that override for R1 so it follows the rule; check every other shortcut on every route has no fixed chance,
except the N5 ford leap (part of the lap, 1.0 is right there). Report each shortcut's chance at the front and when
last and 200 m behind.

**Also in this pass (Dan, 2026-10-12 09:26): the AI makes mistakes.** Answer to Code's question whether the AI
should sometimes fail R1: yes, and more broadly, see the new CLAUDE.md rule "The AI is not perfect". For R1: an AI
that takes it sometimes lands in the pool (a sloppy line or a slow run-up), climbs out and carries on. Beyond R1,
if the AI has no mistakes elsewhere yet, list what it would take (per difficulty) and add the cheap ones in this
pass: a small chance of a short or off-line jump, of running wide on a bend, of clipping a breakable. Report how
often each happened in the race checks.

**Also in this pass (Dan, 2026-10-12 02:15): a drop-in for the trampoline sound.** Dan: "I want to replace the
trampolione sound. I can get it from eleven labs". Add a drop-in folder the same way as the geese and break sounds:
`%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\Trampoline\`, files named `trampoline*`
(.wav, .ogg, .mp3), read at start-up, replacing the made bounce sound when present (several files: pick one at random
per bounce). The game makes the folder and a READ ME in it on its first start (a check never writes there). Say the
folder and naming in the report so Dan can drop his file in.
Dan, same message: "I say we go with this for now and forget the others for right now" (the R1 options other than
his line). N2 and N5 stand as decided.

**STATUS of the Round 20 follow-ups (Claude, 2026-10-12): built and checked in the exe by script (minimized, muted), not yet
driven by Dan.** Check: `ChastainRush.exe -exeRound20 <dir> -r20Only fences,tramp,n5,n2,r1 [-soundsRoot <test dir>]`
(Checks/Round20Runner.Follow.cs). Pictures and the free-cut list: `local/round20/followup/`, `local/round20/free_cuts.*`
(git-ignored). Imported from Racer: nothing.
- N2 "Low creek leap" DONE (official, forgives CP4). Not quite as proposed: the proposal's lip (2528, 539) sat 9 m from the
  trail facing west, a 125 degree turn in 10 m that nothing can drive. Built 20 m further in, off a 12 m arc: an earth and
  creek-rock kicker on the bank's edge (lip 1.8 m, 24 degrees), the hint a knocked-over road-closed barricade and worn tracks.
  The loop's two sides are only 35 m apart there, so the launch is held to 15 to 17 m/s (at 17 to 19 three of five ran past
  the turn back onto the trail). Exe: the five trail vehicles flat out 1.57 to 1.62 s in the air, 25 m, dry, back on the
  trail; the Trail Four at 9 m/s rolls into the creek and drives out (12 s); the AI 5 of 5 (CP4 forgiven, no rescue).
  Trees within 8 m of the bend back onto the trail removed (the Turf Rocket hit one 8.5 m past the trail).
- N5 "Ford leap" DONE (part of the lap: a lap option that forgives nothing, CP2 counted as usual). A log ramp 5.5 m beside the
  hairpin's ford, off a 10 m arc, lip 1.4 m at 20 degrees, held 18 to 22 m/s. Exe: 1.52 to 1.67 s, 27 to 32 m, dry, back on
  the trail before CP2; an AI driver that did not take P1 takes it by itself (5 of 5, CP2 counted, no penalty).
- The Round 20 proposals REMOVED: markers, their data, labels, debug-menu rows ("Proposal markers", "Go to"), the map layer,
  `creek_shortcut_candidates.py`, `make_proposal_labels.py`.
- R1 DONE on Dan's line, with the pool as changed at 03:51 (see the numbers below). Kids'-ramp trial removed (ramp, JUMP! sign,
  tracks). A big homemade ramp in the front yard, its lip (2.6 m, 26 degrees) over a privacy fence across the 1987 / 1552
  side yard (levelled lawn behind it); the flight over #6's open landing strip; a 6 m above-ground pool (steel wall 1.25 m,
  1 m of water, ladder) in the back yards behind 1572 / 1622, 42 m past the fence, 7.2 m from #6's line, clear of 1572; its
  far half a wooden ramp up to a deck on the rim, off whose edge you drop toward the gate; a vehicle in the water stops for
  2.5 s, then wades at 1 m/s (PoolHold); a privacy fence across the 1572 / 1622 side yard with an open side gate 3.5 m wide
  (round solid posts); patio chairs, table, umbrella, grill (breakable). The lobe sealed: solid fences across its other side
  yards; the three in #6's corridor (713 / 1052, 1499 / 1641, 1641 / 1622) stand only during a reverse race (RaceOnly).
  The first build's in-ground pool between the houses is gone (Dan, 03:51).
  Exe numbers (local/round20/followup/final, final2): clean, flat out, all 11 vehicles off the lip at the 22 m/s cap, 2.40 to
  2.62 s in the air, 47 to 50 m, upright, through the gate and onto the lane: saves 2.7 to 4.2 s against the lap between the
  same points. The speed window off the lip (the same for every vehicle, aimed launch): 21.5 and up clears it; 19.9 to 20.8
  comes down in the pool and always gets out (up the ramp, off the deck, through the gate); a fall costs 4.8 to 5.8 s (more
  than a clean run saves); 18 and under lands short of it on the lawn and stops against its wall (drive round it: not a
  trap, but a stop). The AI takes it (85% of the time) and makes it clean (3 of 3). NOT MET: "sometimes not" for the AI. A
  sloppy AI try was aimed at the pool and put the car on the pool's near rim and against its wall (11 rescues); the band that
  lands in the pool is under 1 m/s wide, so the AI's sloppy tries are off on R1 (`ai_slip` 0) rather than let it get stuck.
  The 1987 / 1552 fence is skimmed by some cars' tails over the lip (no impulse); cars brush the gate's posts without losing
  speed; the Skyfin clipped a post hard at 3.3 m, so the gate is 3.5 m.
  For Dan to decide: a step or deck on the pool's near side so a short landing ends in the pool instead of against its wall
  (only room for it if it may reach 0.5 to 1 m into #6's corridor); and whether the AI should make mistakes here at all.
- Trampoline sound drop-in DONE: `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\Trampoline\`, files named
  `trampoline*` (.wav / .ogg / .mp3), several = one at random per bounce; the folder and a READ ME are made on the game's first
  start. Exe (a test folder): read and played on a bounce.
- Free cuts (report only, nothing changed): `python Tools/mapgen/free_cuts.py`. Reverse, with R1's fences: (1) round the
  barriers of the side road east of the lobe on the lawns, CP5 to the north lane, saves 68 m (and 34 m to a nearer point);
  (2) front yards inside the lap's west curve, CP5 to the north lane, 27 m; (3) from the start / finish diagonally through
  the back yards to near CP5, 35 m (26 m). Forward: (4) from the north lane round house 1641 and down #6's strip to CP5
  instead of jumping, up to 79 m (open in a forward race: those fences stand in reverse only); (5) the same south back
  yards, 35 m. Options for Dan: make the 1641 fences stand in forward races too (they would be beside #6's kicker), solid
  fences along the side road's verges, a checkpoint inside the lobe.

**CANCELLED (Dan chose his own line for R1, 2026-10-12) — Round 20 item 3 follow-up: R1 OVER a one-story 1572 (Dan, 2026-10-12 01:43).** Dan, on the "cut 1572 in half and
move 2065" result: "So is no matter what my choice going to take a significant chunk of 1572? Can we not make it a
one story house or something". Claude's reading of `local/round20/r1_study.txt`: the option of lowering 1572 was
never tried, and lowering it alone does not help, because almost every failure there is on the ground AFTER landing
(the flight is only 1 to 3.5 m over the lip; vehicles land 16 m out and run into 1572's wall 19 m on). What could
work is jumping OVER the house instead of past it: 1572 rebuilt as a one-story house on its own footprint (a ranch,
style C, looks normal, nothing cut away), and the original R1 kicker made a big aimed launch (as BigJump / the
trampoline: every vehicle thrown on the same arc whatever its speed, 14 to 32 m/s, 1.5 m and 6 degrees off line)
that clears 1572's roof with room and lands on open ground beyond it, upright, pointing down the lap, with a clear
run-out (2065 moved only if needed; say how far). Work this out on paper first (the same check as r1_study.txt:
every speed, offset and angle; the roof clearance, where it lands, the run-out). If it is safe in every case, build
it as R1, reverse only, with its hint at the turn-off, an AI trial, and one picture from the approach with the
flight drawn; leave the kids'-ramp trial in place too so Dan can drive both and pick. If it is not safe, build
nothing and report why with the numbers.
Dan, clarifying (01:46): "i don't mind making changes. It just looks like the options take a significant portion of
the house away and it will look akward". So the objection is the look of a house cut in half, not the change. The
fallback if the jump over a one-story 1572 is not safe: Round 20's working layout (1572 down to its north-east part,
2065 moved 8 m) BUT with 1572 rebuilt as a complete, smaller house designed for that footprint (its own proper roof,
front, garage and porch, one of the neighborhood styles), so it reads as a normal smaller house, not half of one.
Show Dan a picture of that house from the street before building it.

**BUILT, not yet driven by Dan — Round 21: carry-overs, the reverse Noonday Creek race, and its shortcut candidates (Dan, 2026-10-12 10:00:
"Lets fold everything into the next round").** The Round 20 follow-ups are built (commit 755f34e); Dan has not driven
them yet. The two items added after that build (the AI shortcut rule, the AI makes mistakes) were never done: they are
Part A here. The shortcut design rules in CLAUDE.md ("Shortcut design") apply to everything in this round.

Part A, carry-overs (commit on their own):
1. The AI shortcut rule (CLAUDE.md "How the AI chooses shortcuts"): R1 still has a fixed `ai_take` 0.85. Remove it so R1
   follows `ShortcutStrategy`; check every shortcut on every route for a fixed chance (only the N5 ford leap may keep
   one, it is part of the lap). Report each shortcut's chance at the front, and when last and 200 m behind.
2. The AI makes mistakes (CLAUDE.md "The AI is not perfect"; Dan answered Code's question: yes, the AI should fail R1
   sometimes). For R1: build the step / deck on the pool's near side so a short landing ends IN the pool, not against
   its wall (Dan confirmed 10:10: "yes put it in case you are short"; it may reach 0.5 to 1 m into #6's corridor; #6 is forward only and R1 is
   reverse only, so they never meet in a race; check #6 still runs clean). Then turn the AI's sloppy tries back on
   for R1 so it sometimes lands in the pool, climbs out and carries on (never stuck, no rescues). Elsewhere: if the
   AI makes no mistakes yet, add the cheap ones (a short or off-line jump, running wide on a bend, clipping a
   breakable), more on Easy than Hard. Report how often each happened in the race checks.
3. The free cuts listed in the Round 20 follow-ups status (`Tools/mapgen/free_cuts.py`): Dan's answer (10:13): "to
   prevent major shortcuts, we can put a CP". So close them with checkpoints, not fences: add checkpoints to the
   Chastain Glen loop (forward and reverse as needed) so that every free cut saving about 30 m or more skips a
   checkpoint and costs the 5 s penalty, while the official shortcuts (#2, #6, R1, R3) still forgive what they
   skip and the plain lap passes every checkpoint naturally. Keep the count sensible (gates spread round the lap,
   none in an awkward spot such as mid-jump or on a landing). Re-run free_cuts.py afterwards and report what is left
   open (small cuts under 30 m may stay). The AI keeps to the rules as before.

Part B, a reverse Noonday Creek race. Dan: "I would like to see if we can do a straight up reverse version of the
noonday creek trail. Slight variation of the track is expected if needed."
4. A "Noonday Creek (reverse)" option: the same trail driven the other way, chosen from the race menu like the Chastain
   Glen loop's direction (Direction: Forward / Reverse). Same world, routes laid over it. Slight changes to the line
   are fine where a section does not work backwards (a bend too tight to take from the other side, a climb too steep,
   a bank to get up), say each one.
5. The jumps in reverse: every jump on the trail is a one-way kicker. For each (J1, J4 over New Chastain Road, J5,
   J6, BJ1 to BJ4, N2's bank, N5's log ramp, P1's outfall over the pond) say what happens driving it the other way,
   and give the reverse lap its own jumps where the forward kicker does not work backwards: a take-off on the other
   side, looking like something that could be there at its real size, both directions living side by side without
   one spoiling the other (no kicker you hit head on, nothing that catches a vehicle going the other way). The
   reverse lap should have as much big air as the forward one. The road jump over New Chastain Road in reverse too.
6. P1 and the other forward shortcuts (N2): official in the forward race only, unless one works as well backwards
   (say which). In the reverse race the forward-only ones are just part of the world (not barred, not forgiven).
7. Checkpoints numbered in travel order for the reverse race; the AI races it (trail mode, small vehicles only, as
   forward); start / finish and grid placed for the reverse direction.

Part C, shortcut candidates for the REVERSE Noonday Creek race, flagged for Dan to decide. Dan: "I would also like to
see some potential shortcuts flagged out so I can decide. As usual shortcuts should be more challenging than just
following the track but rewarding if pulled off successfully. We also don't want the shortcuts to cut off too much
of the track. Shortcuts should always be fun, and interesting".
8. Three to five candidates, each held to CLAUDE.md "Shortcut design": harder than the trail, a real reward when
   pulled off, a real cost when fumbled, never cutting too much of the lap, fun and interesting (a jump, a gap, a
   line through something), with a hint at the turn-off. For each: where it leaves and rejoins, how much lap it skips
   and the time a clean run saves, what a fumble costs, the air per trail vehicle from a real trajectory, what it
   looks like, which checkpoint it forgives, what is in the way.
9. Mark them in the world (posts and labels, the debug-menu "Proposal markers" / "Go to" rows back, temporary) and on
   the map, plus an overhead picture. Build none of them; Dan picks after driving the reverse race.
10. One targeted check per built piece in the exe (stay off Dan's screen, muted), rebuild the exe, report: short status
    list, "for Dan to check", the candidates in a short table, path to the exe.

**STATUS of Round 21 (Claude, 2026-10-12): Parts A and B built and checked in the exe by script (minimized 1280 x 720, muted),
not yet driven by Dan; Part C proposed on markers, nothing built.** Part A commit 03db48a. Check: `ChastainRush.exe -exeRound21 <dir>
[-r21Only r1,six,races,revlap,fwdlap,revslow,revrace] -logFile <dir>\Player.log` (Checks/Round20Runner.R21.cs, R21B.cs). Pictures and
lists: `local/round21/` (git-ignored). Imported from Racer: nothing.
- 1. AI shortcut rule DONE: R1's fixed 0.85 removed; every shortcut follows ShortcutStrategy except N5 (lap option, 1.0). Chance: 12 %
  at the front, 58 % when last and 200 m behind (each driver +/-4 %, kept to 6 to 65 %), the same for #2, #6, R1, R3, P1, N2.
- 2. AI mistakes DONE. R1: a wooden step deck on the pool's near side (held slope to the rim, 2.2 m out, about 0.9 m into #6's
  corridor); a vehicle off the step is caught over the pool (PoolHold) and drops in. Exe: 15 to 21 m/s off the lip all end in the
  pool and get out; clean from 21.5 (cap 22); a fall costs 5.2 to 5.3 s (clean saves 2.7 to 4.2); #6 clean in all 11 vehicles. The
  Skyfin Cruiser could not climb out of the pool (also before this round): the wading cap now grows after the stall, and a vehicle
  still for 4 s in the water is lifted onto the exit deck (Skyfin: 12.1 s lost). R1 sloppy tries back on (0.3; x1.5 Easy, x0.6
  Hard); AI sloppy x4 into the pool and out, no rescue. Elsewhere (DriverVariation): late braking (runs wide), curb clip on road
  straights, short / off-line jumps at a route's jumps. Race checks (5 races, 15 drivers): wide line 9, early braking 20, jump off
  line 4, late braking 16, short jump 6 (one an R1 sloppy try into the pool, out with no rescue), curb clip 12; all finished.
- 3. Free cuts DONE: Chastain Glen checkpoints at lap stations 952, 1056, 1216 (replacing the filler at 1000.6; 7 in all), checkpoint
  half width 12 m (was 14: cuts through the front yards beside the road passed the gates). free_cuts.py now treats a cut through a
  gate's opening as passing it: 0 free cuts of 25 m or more left, both directions. #6 and R1 forgive CP5 (952), #2 and R3 CP2.
- 4 to 7. Reverse Noonday Creek DONE (race setup: Route Noonday Creek, Direction Reverse). Same line, no variations needed: every
  forward take-off had a wall facing the reverse lap, so each big jump got a back (about 20 degrees, a landing) that is the reverse
  take-off (a BigJump the other way): BJ1R (earth and broken planks heaped against the broken end, meeting the fallen span in a V:
  under the deck was a tunnel), BJ2R, BJ3R (behind the log, 20 instead of 37 degrees), BJ4R (held to 17 m/s: the trail bends 20 m
  behind it); RJ4, J4's mirror on the south bank, an aimed launch north over New Chastain Road onto J4's slope; both road kickers
  have a 4 m back toward the road. J1, J5, J6 work backwards as they are (smaller: the landing face is the take-off). Exe, flat out:
  BJ1R 1.96 s 43 m, BJ2R 1.9 s 42 m, BJ3R 1.9 s 42 m, BJ4R 1.7 s 26 m, RJ4 2.0 to 2.1 s 44 to 49 m; forward unchanged (BJ1 56 m,
  BJ3 60 m). Slow into every reverse take-off: drives on. P1, N2, N5 forward only (P1 backwards ends in the pond; N2 needs a kicker on
  the other bank: candidate RC). Checkpoints in travel order (reverse CP1 = forward CP5), start / finish at the same gate, the grid 45 m
  before it. Reverse race, 3 AI: all finished, 0, 0 and 1 recoveries.
  Found, not changed (as Round 20's list): the scripted flat-out laps still run wide into trunks on some bends (forward: C3 bend,
  east leg, south loop; reverse: lake corner, hairpin, south loop east side), and in reverse the drop into the creek bed under the
  bridges is a big splash (27 to 10 m/s; one AI recovery there). Option for Dan: breakable trees at those bends.
- 8, 9. Candidates PROPOSED (`python Tools/mapgen/reverse_creek_candidates.py`; `local/round21/reverse_candidates.png|txt`): RA Pipe
  dive, RB Hairpin cut, RC Creek leap back (table in the round report; RD, a hop over the pond's outflow, dropped: 6 m). Markers in the
  world (magenta; debug menu "Proposal markers" and "Go to", free roam only) and on the map: TEMPORARY, remove when Dan has chosen
  (`MapBuilder.Proposals.cs`, `Debug/ProposalMarkers.cs`, `Data/proposals/round21_markers.json`, `Assets/HandMade/Proposals/`,
  `Tools/handmade/make_proposal_labels.py`).

**DONE, driven by Dan 2026-10-12 ("everything seems to be fine") — Round 21 follow-ups (Dan, 2026-10-12 14:27).** Dan: "i think the reverse track is fine" (approved). His
picks from the Part C candidates, and one more:
1. Build RA "Pipe dive" (reverse race, official, forgives reverse CP4) as proposed in `local/round21/reverse_candidates.txt`:
   the rock-heap take-off on the small pond's east shore, over the pond, down P1's outfall apron into the pipe, out over
   the ford. Hint as proposed (the pipe's lights seen across the pond, worn tracks). AI trial.
   PLUS, Dan: "can we have the pipe be clear and you can see fish swimming around?" Claude's reading, CONFIRMED by Dan (14:29): part of the pipe
   becomes a CLEAR tube running through water, like an aquarium tunnel, with fish swimming round it, so driving through
   you see water and fish on every side. P1 runs under dry ground now, so: re-route or extend the pipe's run so a good
   stretch of it (as long as the ground allows, at least 25 to 30 m) passes UNDER the small pond (or the creek) as a
   clear tube: glass-like walls, water all round it lit from above (light shafts, a rippling surface overhead), a
   few schools of fish and some bigger fish drifting and darting, weeds and rocks on the pond bed, a sound change
   inside. The concrete parts stay as they are. Everything P1 has keeps working in the clear stretch: round walls you
   can ride, the camera inside, the trickle of water along the bottom, never a trap, a reset puts you in the bottom
   facing along it. Both the forward P1 and the reverse RA go through it. Keep the frame rate (fish are cheap
   animated meshes, no physics). If the pond is too shallow for a tube under it, deepen the pond bed over the tube
   (the pond surface stays where it is) and say by how much.
2. Build RC "Creek leap back" (reverse race, official, forgives reverse CP2) as proposed: N2's twin heap on the west bank,
   over the creek onto N2's heap. AI trial.
3. Breakable trees wider round the trail. Dan: "can we make the breakable trees a little bit wider around the tracks?"
   Now within 6 m of the line (8 m at the big jumps' run-outs). Make it about 10 m both sides everywhere on the
   Noonday Creek trail, both directions, and about 15 m on the outside of the bends where the scripted laps hit trunks
   (forward: C3 bend, east leg, south loop; reverse: lake corner, hairpin, south loop east side). Big trunks beyond stay
   solid. Report the count and re-run the flat-out laps: no dead stops on trunks.
4. Remove the Round 21 proposal markers, their data, labels, debug-menu rows and the map layer (RB not built).
5. One targeted check per piece in the exe (stay off Dan's screen, muted), rebuild the exe, report: short status list, "for
   Dan to check", path to the exe. Two pictures from inside the clear stretch.

**STATUS (Claude, 2026-10-12): items 1 to 5 built and checked in the exe by script (1280 x 720, minimized, muted), not yet
driven by Dan.** Check: `ChastainRush.exe -exeRound21 <dir> -r21Only tube,ra,rc,n2,ramp,fwdlap,revlap -r20Vehicles
atv,moto,scrambler,drifter,mower -logFile <dir>\Player.log` (`Checks/Round20Runner.R21F.cs`; the laps now name each stop's
cause). Pictures and reports: `local/round21f/` (git-ignored). Imported from Racer: nothing.
- 1. RA "Pipe dive" (reverse, official, forgives reverse CP4) and the clear tube. The small pond is only about 38 m across on
  P1's line and its east shore is where P1J lands and RA takes off, so the pipe could not pass under it without moving the
  outlet and losing both. Instead the small pond's water was brought west over P1's middle: a pool at its level (276.12 m),
  30 m along the pipe and 24 m across, dug up to 3.6 m into the swale there (banks 1 in 3; trees in it gone). Under it P1 is
  held deeper (crown 1.35 m under the surface) and 30 m of it is a glass tube (steel ribs every 3 m), in a trench of deep water
  16 m wide and 8 m deep with a silt bed, rocks, swaying weeds, light shafts and the rippling surface overhead, 56 fish (four
  schools, sunfish, three big carp / bass; instanced, no physics); muffled sound and an underwater hum inside; an invisible lid
  at the pool's bed so a wading vehicle never drops in. Concrete parts, mouths, the stream, wall riding, the camera and resets
  unchanged (`MapBuilder.ClearPipe.cs`, `PipeAquarium.cs`, shaders `PipeGlass`, `UnderwaterScroll`, `Fish`; `pipes.json`
  `clear`). Exe: all five trail vehicles through the glass forward without losing speed, out over the pond; a reset in the glass
  lands on the floor; 60 fps average, 58 at the 99th percentile in the glass (1280 x 720, not measured at 3840 x 2160).
  RA: a creek-rock heap on the east shore (lip 1.4 m, 22 degrees, aimed 23.2 to 23.5 m/s), over the pond onto P1J's apron
  (its slope is the landing), through the pipe and the glass, out over the rock bar and a 25 m bend onto the trail. Hint: a lit
  lamp over the outlet's opening and worn tracks. Exe: every vehicle down on the apron (1.85 to 1.92 s, 40 to 42 m), the AI
  takes it on each vehicle (CP forgiven, no recovery), slow into it rolls into the pond and drives out.
- 2. RC "Creek leap back" (reverse, official, forgives reverse CP2): N2's twin heap between the west trail and the gorge (lip
  1.4 m, 24 degrees, aimed 15.5 to 16.3 m/s) onto N2's heap. The west trail runs 12 to 15 m from the gorge there, so the way in
  loops out west (radius 9 to 10 m) and crosses the trail onto the heap; the heap's back is an earth ramp down into the gorge
  (a short forward N2 flight lands on it; a vehicle short into the gorge drives out up it), and N2 got the same back on its side
  (a slow RC fell into the gorge against N2's lip end and stopped): the old washed-out ford now has an earth ramp on each bank.
  Exe: every vehicle onto N2's heap (24 to 27 m), the AI on each vehicle, slow rolls in and gets out (20 s); N2 forward
  unchanged (every vehicle, slow 12 s, the AI).
- 3. Breakable trees: 10 m either side of the trail everywhere (was 6), 15 m both sides on the bends named (the outside only
  left trunks on the other side, where vehicles run wide out of S-bends), 12 m round RA's and RC's lines (25 m round RA's way
  out of the pipe, where a flat-out exit went 24 m wide), and nine trunks the flat-out laps still hit 12 to 20 m out (by
  position, `HIT_TRUNKS`): 492 breakable (was 166). Big trunks beyond stay solid. Final flat-out laps, each trail vehicle, both
  ways: all finish, no stop on a trunk (forward 10 and reverse 21 breakable trees snapped). Left (not trunks, unchanged): the
  water at the C4 ford and under the bridges, J5's take-off face, BJ4's back, two bumps; one bad landing off BJ2R (Needle 600
  at 26.9 m/s, upright 0.66; BJ2R not changed this round).
- 4. Round 21 proposal markers removed: `MapBuilder.Proposals.cs`, `Debug/ProposalMarkers.cs`, `Data/proposals/`,
  `Assets/HandMade/Proposals/`, `make_proposal_labels.py`, `reverse_creek_candidates.py`, the debug-menu rows, the map layer.
- 5. Exe rebuilt: `Builds/ChastainRush/ChastainRush.exe`.

**DONE, driven by Dan 2026-10-12 (session 2026-10-12_20-55-46-538_f31c32; his one fix is in Round 23 Part A) — Round 22: Chastain Commons, Chastain Enclave and Grasmere Court (Dan, 2026-10-12 18:45: "Do we go ahead
and build the other community neighborhoods as discussed prior"; moved ahead of the pipe network).** Dan's words and
the places are in the notes "Round 18, two more neighborhoods" and "Also in the neighborhoods round" further down
(Claude's numbering there is old; this is the round). Build it; Dan judges by driving (CLAUDE.md "Build it, then Dan
drives it").
1. Grasmere Court Northeast (BUG-002 of session 2026-10-10_16-53-56-509_f5d57e, part of Chastain Glen): dress every
   house on it the way the Chastain Glen houses were dressed in Round 7 (the same styles and colors, windows, doors,
   trim, garages, mailboxes, lamps, hydrants, drives, sidewalks, fences, breakables). Then list any other street that
   looks like part of Chastain Glen and is still plain boxes, for Dan.
2. Chastain Commons (Weybourne Court Northeast and its side streets, south of New Chastain Road, about x 2560 to 2945,
   z 600 to 700): same community as Chastain Glen, so the same house styles (Dan: "we can reuse the house styles
   there"), dressed the same way, its own mix so it does not look like a copy.
3. Chastain Enclave (Rotherhithe Landing, Bloombridge Way and side streets, about x 1500 to 2100, z 640 to 960, entered
   by Chastain Crossing at x 1793, z 658): the same styles but LARGER houses (Dan: "Enclave tends to have larger
   houses"): bigger footprints where the lots allow, more two-story, three-car garages, brick fronts, bigger yards and
   driveways. Near the front of the neighborhood (Dan's picture `local/reference_photos/maps/enclave_dan.png`, the
   "Chastain Enclave HOA Pool" pin, inside the Rotherhithe loop just north of the entrance): a community pool (water
   as Round 15, a deck, a black metal fence that breaks, a small clubhouse) and a tennis court (fenced, net, lines)
   beside it. Place them where the pin is; Dan will say if it is off.
   DAN SENT REFERENCES (2026-10-12 19:01), filed in `local/reference_photos/enclave/` (git-ignored; reference only, never
   traced or used as map data: the overhead is a Google Maps screenshot, and the project uses no Google Maps data):
   - `enclave_amenity_overhead.png`: the amenity block sits INSIDE the loop between Rotherhithe Landing (north and west)
     and Sherryforth Drive (east and south), back from both streets. Layout: TWO tennis courts side by side to the
     north (fenced, lights), the pool south of them, the clubhouse on the pool's west side, a small shed / building by
     the courts' south-west corner, a short drive or lot off the west.
   - `enclave_pool_photo.jpg` (looking roughly north-east over the pool): the pool is a big free-form / angled shape
     with wide concrete steps at both ends and a dark tile line at the waterline, a wide concrete deck all round with
     joints; lots of striped lounge chairs, a round table with a red umbrella; the clubhouse is a one-story tan-siding
     pool house with a hip roof and a front gable, white columns along a covered porch, a round vent in the gable,
     restroom doors; the pool fence is black metal with white square posts and low hedges along it; beyond it the
     tennis courts (green surround, blue playing surface, white lines, tall black chain-link fence, light poles) with a
     green windscreen banner on the court fence reading "Chastain Enclave"; a wooden gazebo / pavilion with a deck and
     railings beside the courts; tall pines round it all.
   Build the amenity block to match these as closely as the house-building kit allows (the pool is water as Round 15,
   driveable into and out of; the fence, chairs, umbrella and tables break; the courts' fence is solid chain-link
     except where it would trap a car; the gazebo may be solid). Pictures of it for the report.
4. An entrance sign for each community (Chastain Commons, Chastain Enclave) at its entrance, in the style of Chastain
   Glen's brick monuments (the community names are real and fine to show; nothing else real). Sign rule applies.
5. No new races or shortcuts this round. Check one drive through each neighborhood in the exe (no snags on curbs,
   drives or the new pieces; frame rate in each), stay off Dan's screen, muted. Rebuild the exe. Report: short status
   list, "for Dan to check", pictures from the street in each (git-ignored if houses are close and identifiable as
   Dan's street: these are not, but keep them in local/ anyway), path to the exe.

**STATUS of Round 22 (Claude, 2026-10-12): items 1 to 5 built and checked in the exe by script (1280 x 720, minimized,
muted), not yet driven by Dan.** No new debug sessions since 2026-10-12_01-54-20-969_70e57b. Imported from Racer: nothing.
- Data: `python Tools/mapgen/communities_area.py` writes `Data/neighborhood/communities.json` (each community's street pieces,
  houses, entrance; the Enclave's OSM pool, tennis pitches and the two small buildings by them). Re-run after mapgen. The
  builder merges them into the Chastain Glen area (`MapBuilder.Communities.cs`), so they get Round 7's dressing exactly;
  each community has its own bounds rectangle (roads outside them are untouched; only roads meeting their streets, New
  Chastain Road at the entrances, get the street treatment).
- 1. Grasmere Court: 9 houses dressed with Chastain Glen's own style rule (A 1, B 1, C 4, D 3). Other streets still plain
  boxes near Chastain Glen (report only, `communities_area.py` prints the list): Chastain Ridge Drive NE (x 3086, z 681,
  22 houses), Joel Drive (x 2915, z 416, 21), Embry Lane NE (x 2960, z 473, 40), Lyndon Drive (x 2814, z 441, 8), Hayworth
  Drive NE (x 2998, z 402, 6), Blackwell Circle (x 3419, z 893, 13), Nivera Court (x 3494, z 796, 2), and farther north
  Westminister Way, Kensington Drive, Cambridge Drive, Westchase Drive, Guidhall Trail, The Willows, Coventry Drive.
- 2. Chastain Commons (Weybourne Court, Claybrooke Drive, Henford Court): 61 houses, its own mix (A 26, B 1, C 20, D 14,
  15 brick fronts) and its own color draw from the same palettes.
- 3. Chastain Enclave (Rotherhithe Landing, Bloombridge Way, Sherryforth Drive, Westcote Court, Chastain Crossing): 136
  houses, footprints grown up to 14% where they stay 4 m from neighbors and 5 m from the street (all 136 grew), no ranches
  (A 72, B 58, D 6), 114 brick fronts, 96 three-car garages with 8.4 m drives. Amenity block where the OSM pool is (inside
  the loop, north of the entrance): pool from the OSM outline (water as Round 15, shallow end to drive out, dark tile line),
  deck with joints set level (ground moved up to 2.5 m at its north edge, a 1 in 2 bank down to the courts), 37 striped
  lounge chairs and 2 tables with red umbrellas (breakable), black iron fence with white posts (breakable) and low hedges,
  clubhouse on its OSM footprint (tan siding, hip roof, front gable with a round vent over a porch on four white columns,
  two restroom doors, solid), THREE tennis courts (the map data has three pitches and Dan's overhead shows three blue
  panels; the planning note said two), level, green surround, blue courts, full lines, nets, 3 m chain-link (solid) with
  two open gates so nothing is trapped, "Chastain Enclave" windscreen banner facing the pool (one-sided,
  `Tools/handmade/make_windscreen_sign.py`), 4 breakable light poles; the wooden pavilion on the small building west of
  the courts (raised deck, railings, a step, hip roof, solid); a 16 x 11 m parking lot off Rotherhithe Landing / Sherryforth
  west of the clubhouse.
- 4. Brick monuments in Chastain Glen's style: Chastain Commons one (left of Claybrooke Drive's entrance there is no room:
  the slip road), Chastain Enclave two (both sides of Chastain Crossing). Sign panels from
  `python Tools/handmade/make_entrance_sign.py CHASTAIN COMMONS ChastainCommonsSign.png` (and ENCLAVE).
- 5. Exe drives (`-exeShots`, one along each neighborhood's streets at 20 m/s): Grasmere 105 m, Commons 437 m, Enclave
  248 m: nothing broken, no stop, no air, tilt 3 / 15 / 12 deg; 60 fps average, slowest 1% 53 to 54 fps (1280 x 720, not
  measured at 3840 x 2160). `ExeShotsRunner` now reports the frame rate of each drive. Pictures: `local/round22/`.
- Report only: 16 community houses have a wall buried more than 0.5 m on a slope (worst 636 at x 2684, z 632, 2.7 m;
  1099 at x 2873, z 639, 1.4 m; 361 at x 2013, z 780, 1.3 m); not regraded (Round 9 rule: only where Dan points).

**ACTIVE — Round 23: the Enclave lot fix, then the underground pipe network (Dan, 2026-10-12 21:59).**
Part A (commit on its own), Dan's session 2026-10-12_20-55-46-538_f31c32 (read by Claude):
1. BUG-001 (vehicle x 1767.22, z 771.72, heading 261, on the grass beside the Chastain Enclave amenity block's parking
   lot): "parking lot should extend out to here". Extend the amenity lot's asphalt (with its stalls) out to Dan's spot,
   flush, no step or curb to catch a wheel where it meets the grass. That lot only.
Part B, the pipe network: the notes right below, PROPOSAL FIRST (as Dan asked), markers in the world and on the map,
an overhead; Dan picks; build after. ONE ENTRANCE IS ALREADY DECIDED:
2. BUG-002 (vehicle x 1413.45, z 1176.44, heading 41, in the creek channel just west of Bells Ferry Road, facing the
   bank where the stream goes under the road): "specifically right here there are two large pipes. I want this to
   be a main entrance the pipe network". In the game the stream simply runs into the bank. Make this the network's
   MAIN ENTRANCE: two large side-by-side culvert pipes (a twin culvert) under Bells Ferry Road with a concrete
   headwall and wingwalls set into the bank (no exposed back), big enough for every vehicle, the stream running
   through them; the proposal shows where the network leads from here. Build this entrance now (it is decided; the
   rest waits for Dan's picks), to the pipe standard (round, rideable walls, used and dark, a trickle, lit, never a
   trap, reset in the bottom). Until the network behind it exists, the twin pipes carry you under Bells Ferry Road
   and out the other side.
2b. CORRECTION TO ITEM 2 (Dan, 2026-10-12 23:17, from the work-in-progress exe, standing on his BUG-002 spot, x 1413.47
   z 1176.45, heading 41; his screenshot: `local/round23/dan_culvert_2316.png`): "its already messed up. This should have
   been the entrance". What the screenshot shows: the twin culvert stands off to the RIGHT of where he is looking; straight
   ahead, where the creek channel meets the bank, there is a wingwall with railings and the channel's water is a flat
   blue rectangle sticking out over the bank (it does not follow the channel). The entrance must be exactly where he
   pointed: the twin pipes' mouths straight ahead of his position and heading, at the end of the creek channel he is
   standing in, so the creek runs straight into them and he drives straight in. Move the culvert there (or re-route
   its pipes so the mouths sit there) and fit the headwall and wingwalls to the channel's banks. The channel's water
   follows the channel and goes into the pipes (no flat slab, nothing floating over the bank). Check from Dan's exact
   spot and heading: a picture, and a drive straight in from there in a car and on a bike.
2c. AND IT IS THE WAY INTO THE NETWORK, NOT A ROAD CULVERT (Dan, 23:19: "this was supposed to be the entrance into the
   pipe network not just two pipes passing under the road"). Claude's item 2 wrongly said the twin pipes should carry
   you under Bells Ferry Road and out the other side; that is withdrawn. Build the entrance as the start of the network:
   the twin pipes lead from the creek channel INTO the network, down and away underground (the creek's water goes with
   them), to the network's first junction chamber a little way in, built now as the first piece of the network: a big
   concrete chamber where the twin mains arrive and other pipes leave in the directions the proposal will take them.
   Those onward pipes run a short way and end at closed steel grilles (barred gates, lit, clearly "not open yet",
   never a dead end that traps: room to turn round) until Dan has picked the network and they are built. No exit on the
   far side of Bells Ferry Road from these pipes. Report what the chamber looks like (pictures), and draw where its
   onward pipes would go on the proposal overhead.
3. Rebuild the exe (stay off Dan's screen, muted); report: status list, "for Dan to check", the proposal overhead and
   how to turn its markers on, path to the exe. STOP for Dan's picks on the network.

**DAN'S VISION FOR THE NETWORK (2026-10-12 22:11, after talking it through with Claude). The proposal is built around
this.** Dan, to Claude's list of questions and ideas: "pretty much everything you said. I want a maze, with secret exits
all over but a space for full on race as well. I envision a main hub that is a big open chasm with water flowing in
from all the sources. Love the idea of pop-out manhole covers as well. Pretty much everything you described is things
that I think of." And: "during underground races we can [have] gates sealing off other [areas] and potential shortcuts
would require you to drive over a switch or something to open them."
- A MAZE: many branches and junctions, easy to get lost in, with SECRET EXITS all over the map (out of creek and
  pond banks, behind the shopping center, up into the woods trail, under the bridges, ...).
- THE HUB: a big open underground chasm in the middle of the network where the pipes from every water source pour in
  as waterfalls; the main junction everything leads back to. Its own space to drive round (ledges, ramps, the floor,
  a pool where the water gathers), and a landmark you remember.
- MANHOLE POP-OUTS: some branches end under residential streets and the vehicle bursts up through a manhole cover
  (the lid flies off, the vehicle pops out onto the road). Breakable lid, a clear launch, never a trap.
- Also wanted (Claude's list, Dan: "pretty much everything"): junction chambers, aquarium stretches wherever the
  network passes under a lake or pond (as P1), drop shafts down to a lower level, outfall launches out of headwalls
  over water, flooded stretches that slow you (a reason to pick a route), big mains every vehicle fits and narrower
  branches only bikes / the ATV fit, painted signs at the main junctions and color stripes on the mains (the maze
  branches stay unmarked), and the map showing the pipes when you are underground.
- A FULL UNDERGROUND RACE: a route through the network (through the hub) raced from the menu with the AI, its own
  checkpoints. During that race, GATES (barred grilles / sluice gates) seal off the rest of the maze so the race
  stays on its route; its shortcuts are behind gates that OPEN only when a racer drives over a SWITCH (a pressure
  plate or a lever on the floor somewhere along the route, perhaps off the racing line or up a wall), so a shortcut
  costs a detour or a risk to unlock. The AI follows the shortcut rules (CLAUDE.md) and may hit switches too.
- The main entrance is already decided: the twin culvert under Bells Ferry Road (item 2). East Chastain Lake is just
  west of it.
- The natural spine (Claude): Nelson Lake (south-west corner), West Chastain Lake, East Chastain Lake, the Bells
  Ferry entrance, the Enclave ponds, Noonday Creek and the big lake, the small pond (P1, the aquarium pipe), the
  Chastain Glen pond; about 3 to 4 km, with the maze branching off it. The hub somewhere central on it.
- VERSATILE (CLAUDE.md "Build the world to be reused"): the network is world, not race. Data-driven pieces (pipes,
  chambers, hub, exits, gates, switches) in data files and the map build; race gates and switches are a race-layer
  thing that can be switched off; nothing about the network assumes it is only a race track.
- THE PROPOSAL (Part B): an overhead of the whole network (mains, maze branches, hub, chambers, aquarium stretches,
  drop shafts, every exit and its kind, manholes), what passes under what (roads, houses, lakes), lengths; the
  underground race route with its checkpoints, gates and switch shortcuts drawn; markers in the world at every
  mouth, exit and manhole, and a map layer; build STAGES (stage 1: the hub, the main entrance and the trunk to it,
  and the race route; then the maze in pieces); the frame-rate plan (nothing underground drawn or simulated when
  you are not near it). Dan picks and changes; then build stage by stage.

**Round 23 Part B notes: the underground pipe network (Dan, 2026-10-11).** Dan: "My idea is that we can have a huge
underground network of pipes across the entire maps that link the different bodies of water, not only for race
shortcts." A map-wide network of drive-through pipes joining the ponds, lakes and creeks (22 ponds and lakes, 33
stream lines in `Data/map.json`), for exploring in free roam as well as for race shortcuts. P1 (Round 18) is its
first piece and sets the standard parts (pipe run, mouth with headwall, lighting, reset rule). Round 19 starts
with a PROPOSAL, not a build: an overhead of the whole map with every water body, proposed mouths and runs,
junction chambers where runs meet, lengths, which roads / houses each passes under; markers in the world as in
Round 18; how a player knows where they are underground (a map? signs at junctions?); frame-rate cost. Dan picks.



**QUEUED — Round 17 onward: fill in more of the map (Dan, 2026-10-10).**
Dan: "we need to also fill in some other places on the map as well. I
particularly want the shopping center behind my neighborhood, the
river/wooded area (well mainly I want a track through there) and a couple
of other neighborhoods as well." Not scoped into items yet. Dan's answers
so far, in his words where it matters:

* **Round 17, the shopping center.** Dan: "On the corner of Canton and new
  Chastain corners I think. Currently there is a broad layout of the
  buildings but the parking lot is pretty much all grass and should be
  paved instead. There is a small gas station close to the road but
  separate from building (it is the building in the shopping center further
  north on canton road and an abandoned bank. I want the gas pumps to be
  breakable".
  - PLACED (Claude, 2026-10-10, from Dan's labelled overhead picture matched
    to `Data/map.json`; the picture and a plot of the map data are in
    `local/reference_photos/maps/`, git-ignored, reference only, never a data
    source and never committed). It is NOT at the Canton / New Chastain
    junction; it is 650 m further north, west of Canton Road Northeast and
    north of Chastain Corner Northeast, backing onto Chastain Glen:
    * The center: the one L-shaped retail footprint, x 3209 to 3416, z 1046 to
      1230 (10,653 m2). Its big south-west block is the supermarket (Dan: "The
      biggest part is a kroger"); the long arm running east along the north
      side is the row of small shops, one of them a parcel shop.
    * The gas station: the "roof" footprint at x 3418, z 1164 (379 m2), in
      the car park's north-east. Dan: "The unlabelled other building is were
      the gas pumps are". Canopy and breakable pumps go here.
    * The abandoned bank: the "commercial" footprint at x 3445, z 1113
      (525 m2), by Canton Road. (The picture mislabels it as a parcel shop.)
    * The car park is the ground between them, east and south of the center.
    * Not pointed at by Dan, so not part of this round: the buildings south of
      Chastain Corner Northeast (x 3354 z 993, x 3466 z 1007, the roof at
      x 3492 z 1017) and east of Canton Road.
  - The first build step is still a labelled overhead screenshot from the game
    of these three, for Dan to see before the dressing goes on.
  - Paved car park (not grass) with painted bays, kerbs and islands; the
    buildings read as shops; the gas station with a canopy and pumps that
    break (pumps like hydrants and lamps: knocked over, a sound) and they
    EXPLODE. Dan: "I would like explosions but not really do any damage to
    anything else": a fireball, a bang and smoke for show only; it does not
    break, push or damage anything else, the player's vehicle included; the bank looks abandoned.
  - Shop names and signs are made up, not real brands (Dan: "Store names can be faked").
* **Its own round after the shopping center: a track through the creek
  woods, with pipe shortcuts.** DECIDED by Dan (2026-10-10): "Made up,
  though I didn't realized there was a real paved path trail there...if so
  we can use it but take liberties with it. For example I think it would be
  cool to use pipes connected to various lakes/pounds as underground short
  cuts".
  - A made-up dirt track along Noonday Creek through the woods, built for
    racing: jumps, creek crossings (water slows and splashes, Round 15). There is
    no real trail on this stretch (Dan: the Noonday Creek trail has one end
    in Woodstock and one on Bells Ferry Road off our map, not joined yet), so
    the line is entirely made up.
  - Underground pipe shortcuts: big drainage pipes a vehicle can drive
    through, running between ponds / lakes / the creek (the pond by the
    Chastain Glen entrance already has a culvert headwall with two pipes).
    Enter at a headwall on one bank, come out at another. Shortcut rules as
    #2 and #6: found, not signposted; an official shortcut forgives the
    checkpoints it skips; must never slow or trap a vehicle (every one of the
    eleven fits and gets through, the AI included); a ramp-like thing must
    look like something that could be there.
  - Before building: propose the track's line and each pipe (where it goes
    in, where it comes out, what it cuts off) on an overhead picture for Dan.
    Do not build a nearest version of anything he describes.
* **Round 18, two more neighborhoods: Chastain Commons and Chastain
  Enclave.** Dan: "the easiest as they are part of the same community so we
  can reuse the house styles there. Though Enclave tends to have larger
  houses. It also has a pool and tennis court up towards the front of the
  neighborhood". Dressed the way Chastain Glen was in Round 7 (same house
  styles, larger houses in Enclave, mailboxes, fences, lamps); Enclave gets
  a pool (water, as Round 15) and a tennis court near its entrance.
  PLACED from Dan's two map pictures (same folder):
  - Chastain Enclave: north of New Chastain Road, just east of Bells Ferry
    Road: Rotherhithe Landing and Bloombridge Way and their side streets,
    about x 1500 to 2100, z 640 to 960, entered by Chastain Crossing
    (x 1793, z 658). The pool is inside the loop, just north of the entrance;
    exact spot and the tennis court to be shown to Dan on an overhead first.
  - Chastain Commons (confirmed by Dan): Weybourne Court Northeast and its side streets, south of New
    Chastain Road, about x 2560 to 2945, z 600 to 700, entered from New
    Chastain Road.
* **Also in the neighborhoods round (Dan, 2026-10-10: "grasmere court can easily fit it with the
  neighborhood refresh").** Session 2026-10-10_16-53-56-509_f5d57e:
  - BUG-002 (vehicle x 2767.04, z 823.81, on Grasmere Court Northeast): "these houses here are part of chastain
    glen. Please decorate them as you did the others". The houses on Grasmere Court Northeast are plain boxes
    (no windows, doors, trim; bare drives). Dress every house on Grasmere Court Northeast the way the Chastain
    Glen houses were dressed in Round 7 (the same styles, mailboxes, lamps, drives, sidewalks as there). Only
    that street; if other streets look like part of Chastain Glen and are still plain, list them for Dan.
* Each new area is somewhere a new race can go; one world, routes laid over.

Next Steps (Do Not Start Yet)

* All of these are deferred, not decided against.
* The west end of the New Chastain Road bridge (x 2421, z 766): the deck
  ends 0.2 m above the ground and the creek bank falls away under it, so a
  fast vehicle leaves the deck end with its wheels off the ground (Round 14
  saw it, did not change it). Not on the lap. Waiting on Dan.
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
