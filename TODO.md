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
   race records to `<dir>ace_records_check.json`. Run exe checks with `-logFile <dir>\Player.log` too, or Unity overwrites
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
