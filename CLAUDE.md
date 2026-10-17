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
- **Check the way Dan plays:** in the built Windows exe, driving flat out. An
  Editor or scripted pass is not proof. (Resolution: see "Stay off Dan's
  screen" below; 3840 x 2160 full screen only when the check is about that.)
- **American English** (Dan, 2026-10-10: "use American english...no more
  colour, use color"). Everything Dan can read in the game is American
  spelling: menus, garage, HUD, signs, finish panel, debug panel and report
  text, READ ME files the game writes. color, gray, center, meter, tire,
  curb, neighbor, parking lot, sidewalk, recognized, and so on. New code,
  comments, reports and TODO text are written American too. Do not rename
  existing identifiers, files or saved-data keys just for spelling.
- **Stay off Dan's screen** (Dan, 2026-10-10: "it is annoying with unity
  taking over my screen constantly"; applies to every round from now on).
  Dan uses this PC while a round runs.
  - Editor: run Unity in batch mode with no window. Open the Editor window
    only when a step truly cannot be done otherwise, and say so in the report.
  - Check runs of the exe: a small 1280 x 720 window, minimized, never taking
    focus (`-screen-fullscreen 0 -screen-width 1280 -screen-height 720`, and
    start it minimized / not activated). A check run must keep running in the
    background (the game pauses when unfocused: a check run turns run-in-
    background on; the game Dan launches himself is unchanged). Screenshots
    are captured straight to files, never by bringing the window forward.
  - 3840 x 2160 full screen only when the check is specifically about how
    something looks or performs at that size; say in the report when it was
    done. Frame-rate numbers taken at 1280 x 720 are not 4K numbers: report
    them as "not measured at 3840 x 2160" rather than as proof.
  - Close every Editor or game window opened, as soon as that step is done.
- **Controller first.** Every menu reachable with a controller only, focus
  visible, B goes back; left / right changes a value, A never cycles one.
- **Signs.** Text is one-sided, hidden by terrain like its board, never drawn
  on top, fits the board, readable at 20 to 30 m. Depth-tested lettering,
  never a TextMesh default font material.
- **Never write Dan's real save or records in a check.** Use a copy.
  (Round 15: any `-exe...` run is a check run: `CheckRun` sends its reports, bump log and records to the check's own
  folder. Also pass `-logFile <dir>\Player.log` so Unity does not overwrite Dan's Player.log.)
- **Test quietly** (Dan, 2026-10-10). Every automated run is muted from
  start-up: Editor Play checks, batch runs, and any exe run with a check
  argument. Turn sound on only for the moment something must be heard, then
  off again. The game Dan launches himself is never muted. (Round 15: `CheckRun` does it
  at start-up; `CheckRun.Hear(true/false)` for the moment.)
- **Don't bother Dan** for normal operations (Unity, Git, build). Stop only
  for a real external blocker, with the exact action needed.
- **A fun arcade game, not a serious racer** (Dan, 2026-10-11: "This should
  be fun and arcadey not too serious... The principle core is that this is a
  fun arcade game not a serious racer"). When realism and fun pull apart,
  fun wins: big jumps, generous physics, things that break instead of
  stopping you, nothing that punishes a mistake with a dead stop.
- **Build it, then Dan drives it** (Dan, 2026-10-12: "cant we just build it? I
  mean unless i actually see it I am not going to be able to approve it or
  not"). Once Dan has said what he wants, build it and let him judge it in the
  game; pictures go in the report, not as a gate he has to approve first.
  Proposals and markers only when he asks for options, and changes to houses
  still wait for his yes.
- **How the AI chooses shortcuts** (Dan, 2026-10-12; applies to every race and
  every shortcut, now and later): (1) by default an AI driver usually takes the
  regular path and only sometimes takes a shortcut; (2) it is more likely to
  take one when it falls behind; (3) the further behind, the more likely.
  Implemented in `ShortcutStrategy.Probability(rank, gap)` (ported from
  Racer): about 12 % at the front, up to +34 % for being last and up to +12 %
  for being 200 m or more behind the leader, a small per-driver lean of
  +/-4 %, kept between 6 % and 65 %; decided once per shortcut per lap. A
  fixed chance for one shortcut is allowed only for something that is part of
  the lap rather than a real shortcut (the N5 ford leap); every official
  shortcut follows the rule.
- **The AI is not perfect** (Dan, 2026-10-12: "AI should never drive perfectly
  100% of the time. It should potentially make mistakes just like a human
  might"). Every AI driver makes the occasional human mistake, everywhere, not
  only on shortcuts: comes up short on a jump or into R1's pool, clips a gate
  post or a breakable tree, brakes late and runs wide, misses a turn-off it
  meant to take, bobbles a landing. Not so often that it looks broken, never
  in a way that leaves it stuck, and somewhat more often on Easy than on Hard.
- **Shortcut design** (Dan, 2026-10-11 and 2026-10-12, across Rounds 18 to 21):
  a shortcut is harder than just following the track and rewarding when pulled
  off (a clean run saves real time); a fumble costs more than it would have
  saved, but never traps; it never cuts off too much of the lap (no skipping
  whole sections; Dan dropped candidates that did); it is always fun and
  interesting (big air, a gap, a line through something), never a plain cut
  across grass; it gets a hint at the turn-off; it must not leave an easier
  free way through the same ground (Dan's R1 report: "there is literally
  nothing stopping you"). Propose candidates on markers when Dan asks; build
  once he picks.
- **Build the world to be reused** (Dan, 2026-10-12: "once this game is complete
  ... I can completely reuse the map for another kind of game maybe in post
  apocalyptic world where there would be people living there. so i do want to
  leave it versatile"). Keep the world and the game apart: the map, houses,
  trails, pipes, chambers and the like are world, built from data and the map
  build; races, checkpoints, race gates, switches, barriers, AI racing and HUD
  are a game layer on top that can be swapped out. Do not bake race-only
  assumptions into world pieces.
- **Houses are not set in stone** (Dan, 2026-10-11: "If we need to modify or
  slightly relocate a house to accommodate tracks/shortcuts this could be ok.
  Just let me know and i can do the final decision"). When a track or
  shortcut would work better with a house modified or moved a little, say so
  as an option, with which house, what change and what it buys, and let Dan
  decide. Never do it unasked. Only Dan's own house is locked. House 1763
  is open like any other (this overrides every earlier "nothing attached to
  1763" line here and in TODO.md): Dan had it restored because he "didn't
  like the permanent big hole in the wall", so what to avoid, on any house,
  is a permanent ugly hole or ramp stuck on it, not the house being used.
- **Shortcut hints** (Dan, 2026-10-11; replaces "found, not signposted" in
  the neighborhoods): a shortcut's turn-off gets a clear hint, something
  that could be there (a party sign and balloons, worn tracks, a rock bar),
  never a game arrow or arch.
- **"This should be fun."** Forgiving resets, escapable water and pits, no
  cheap traps.
- **Claims need proof.** Before saying done, look at the result where Dan
  stood.

## Process (Dan, 2026-10-15, carried over from Woodstock Rush)
- **Keep `TODO.md` small.** Code reads it every round, so it holds only the standing rules, the CURRENT round, the queued
  rounds, the last three "Previous delivery" sections and the technical notes. Everything older lives in
  `TODO-ARCHIVE.md`, word for word: search it (grep) when needed, never read it in full. When Dan marks a round reviewed,
  move the oldest delivery to the end of the archive in the same commit. (Woodstock Rush: 878 KB down to 77 KB.)
- **Local build by default.** A round ends with a fresh build Dan can play (`Builds/ChastainRush/`, the root launcher).
  No public release, download / signature checks or updater changes unless the round says "publish" or Dan asks. The
  safety checkpoint, commits and push stay the same. Cleanup stays, but short.
- **Short results.** At most about 20 lines per round in `TODO.md`: one line per item (what changed plus the one check,
  with links to evidence files), decisions made, and a "For Dan to check" list. No build narrative or tables of runs;
  detail, if any, goes in that round's report folder (`Data/screenshots/round<N>/`).
- **Specs only cover what Dan reported.** No "do this everywhere" or audit-the-whole-map additions he did not ask for
  (this restates "World changes only where Dan pointed" above; it binds the planner writing `TODO.md` too).
- **Targeted testing only:** one check per fix, where it changed. Dan drives the full courses himself.
- **Batch small fixes into one round** rather than several, since the build and cleanup happen once per round.
- Dan starts a fresh planning chat every few rounds: `TODO.md` carries the state, so it must always say where things are.

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
  rebuild is needed; it survives re-runs of the script); since Round 16 the
  race setup menu's Direction (saved in `race_settings.json` beside the
  records) overrides it. Races are started, restarted and left from the menus
  (Round 16; the T / X keys and D-pad up / down are gone); R / Y still resets
  the car. Best lap and best total: `local/race_records.json`.
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
  Start it with `Play Chastain Rush.bat` in the repo root. **Standing rule (Dan, Oct 14): the launcher always runs the newest build.** Every round's
  release build overwrites `Builds/ChastainRush/` in place (never a new folder or a renamed exe), and
  `Play Chastain Rush.bat` stays in the repo root pointing there. The `ChastainRushDev/` profiling build is never
  what Dan plays. When a round ends, say the build time from `Builds/ChastainRush/BUILD_INFO.txt` so Dan knows it is fresh.
  In the exe: Start / Esc / Enter open the menus (QUIT GAME is there), F2 frame-rate counter.
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
- Round 15: `ChastainRush.exe -exeRound15 <dir> [-r15Only water|sounds|shortcuts|race] [-soundsRoot <test dir>]
  -logFile <dir>\Player.log`. Sound drop-ins (no rebuild): `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\`
  `Geese\goose_honk*` and `Breaks\wood*|metal*|glass*|water*` (.wav/.ogg/.mp3). Water beds are cut by the map build
  (`MapBuilder.Water.cs`), not mapgen. A code-only exe rebuild during a round: add `-playerBuildKeepScene` to the
  Build Executable batch line (never for the round's last build).
- Round 16: `ChastainRush.GrassScan.Run [-grassOut <file>]` (Editor batch, report only: steps, crests and steep banks of the
  ground across and along every Chastain Glen street, curb to lawn). The shortcut check takes `-shortcutLines 0,1.5,-1.5,a4`
  (off the line / at an angle into it) and writes `shortcut_snags.csv`. Breakable signs: `Signs3D` / `Breakables.Face`.
  (Round 18's temporary proposal markers and their debug-menu rows are gone.)
- Round 18: the Noonday Creek trail and its race route: `python Tools/mapgen/make_creek_track.py [--plot]` writes
  `Data/tracks/noonday_creek.json` (built by `MapBuilder.CreekTrack.cs`) and `Data/routes/noonday_creek.json`; re-run it after
  mapgen and after editing `Data/pipes/pipes.json`, then Build Map. Every `Data/routes/*.json` is a race (the race setup
  menu's Route row; `local/race_settings.json` "route"; a check's `-route <file>`). A route may list `vehicles` (the only
  ones offered, for the player and the AI), `directions`, `trail` (the AI's narrow-trail mode) and `ai_zones` (held speeds).
  Drive-through pipes: `Data/pipes/pipes.json` (hand-made layout), built by `MapBuilder.Pipes.cs` (terrain holes over the
  cuts; `DrainPipe` at run time: resets inside, race lines' heights). R3's play set and trampoline: `make_shortcuts.py`
  (`build_r3`), `MapBuilder.Playground.cs`, `Trampoline.cs`. Check: `ChastainRush.exe -exeRound18 <dir> [-r18Only
  j4|pipe|r3|ai|race|pics] [-r18Vehicles ids] -logFile <dir>\Player.log` (Checks/Round18Runner*.cs).

- Round 33: the Chastain Ridge race on open roads: `python Tools/mapgen/make_ridge_route.py [--plot]` writes
  `Data/routes/chastain_ridge.json` (route `"traffic": true`, `reset_skip`) and `Data/tracks/chastain_ridge.json` (the three dirt
  piles, landing zones, cleared lines, the hollow's ramp; built by `MapBuilder.ChastainRidge.cs`); re-run after mapgen, then Build Map.
  Check: `ChastainRush.exe -exeRound33 <dir> [-r33Only jump,shortcuts,hollow,pics,race] -traffic Normal -logFile <dir>\Player.log`
  (`Checks/Round20Runner.R33.cs`).

## Where things stand
**Always check `TODO.md` for the current objective before starting work.** Rounds before the last three deliveries are in `TODO-ARCHIVE.md` (search it; "see `TODO.md`" below for an old round means there).

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

**Active work is Round 12 in `TODO.md`:** house 1763 restored completely
(Dan has given up on a shortcut through it: nothing may be attached to it
again), shortcut #2 now starts just past it with one sign as the clue, grass
not wood on the ground, an inclined run into 1627's sliding door and a
kicker for big air out of its upstairs windows, sidewalks that stop at
driveways, the pool building swap, no start gate in free roam. Queued after
it, one round each: 13 vehicles with sound, 14 three AI drivers, 15 menus.

**Active work is Round 13 in `TODO.md`:** three shortcut fixes from Dan's
drive (a tree on #2's line, #6's ramp drawn as a flatbed instead of hay
bales, penalties wrongly charged on #6), then Racer's eleven player vehicles
with their sound and a temporary D-pad vehicle switch. Queued: 14 three AI
drivers, 15 menus and the garage.

**Round 33 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Chastain Ridge race on
open roads (Canton Road, Chastain Corner, Chastain Ridge Drive, the Ridge leap off its dead end onto New Chastain Road; S1 Knoll hop, S2
Storage hop; dirt piles with aimed launches, `MapBuilder.ChastainRidge.cs`; `reset_skip` in `CarRecovery`; traffic on the course, `Traffic.Close`).
Imported from Racer: nothing.
**Round 34 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Dan's Round 33 reports
(mapgen `road_flatten.wider_wins_rects`; stall lines only on near-level ground, `StallLevel`; a road's paint stops on the road it joins,
`RoadIndex.CoversPaint`; BUG-010's drive, `MapBuilder.Round34.cs`), a line crossing with no lap driven never ends a lap (`RaceEntrant.LapAway`),
the radio keeps its song on focus loss (`LocalRadio.Hold`), Racer's Top 10 boards (`Race/RecordBoards.cs`: `top-ten-v1.json` beside the
records; the player's name in settings.json) with the RECORDS page and on-screen keyboard (`Menus/GameMenus.Records.cs`). Check:
`ChastainRush.exe -exeRound34 <dir> [-r34Only lap,bumps,radio,records] [-radio] -logFile <dir>\Player.log` (`Checks/Round20Runner.R34*.cs`).
Imported from Racer: RecordBoards, PlayerNames, FinishPresentation, the Records page and on-screen keyboard (rewritten into GameMenus).
**Round 35 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Underground race's line
on M5's straight (`make_underground_route.py` `gate_s`, route `gate_ring_m`, `finish_park_gap_m` / `finish_park_speed`); the radio's voice
priority 0 (`LocalRadio`); Racer's day / night and weather (`Assets/Scripts/World/`: WorldLook (race conditions from `GameMenus.Current`,
Free Roam's in settings.json), WeatherEffects, WeatherSky, SkyClouds (`Lift`), MoonIcon, WeatherGrip; new `NightLamps`, `RoamClock`;
`ChastainRush/VertexLit` reads `_RacerWet`, `_RacerSnow`, `_RacerLamps` and `_SnowCover`). Check: `ChastainRush.exe -exeRound35 <dir>
[-r35Only line,radio,cpics,grip,fps] [-radio] [-r35RadioOld] [-lookOff] [-r35Off clouds,effects,lamps,look,pin,probes] -traffic Normal
-logFile <dir>\Player.log` (`Checks/Round20Runner.R35*.cs`). Imported from Racer: the files above, ArcadeVehicle's weather grip, thunder
ducking, their shaders and materials; left behind: SnowScenes, the gates' Glow, the water probes (cost), split-screen, Trailer Mode.
**Round 36 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the Kroger lot as the Chastain Corner
Stunt Park (`python Tools/mapgen/make_stunt_park.py [--plot]` writes `Data/tracks/stunt_park.json`, re-run after mapgen; built by `MapBuilder.StuntPark.cs` /
`.StuntParkStores.cs`; boards `python Tools/handmade/make_stunt_signs.py`, hand-made, committed; runtime `Assets/Scripts/Stunts/`), Racer's stunt vehicles,
trick scoring, the Stunt event and its Top 10s; B Racer's Free Roam activities (`python Tools/mapgen/make_activities.py` writes `Data/activities/free_roam.json`
from the world's launches and `Data/activities/targets.json`; `MapBuilder.Activities.cs`, game layer; sites alone into the built scene:
`-executeMethod ChastainRush.MapBuilder.RebuildActivitiesFromCommandLine`); C 529 more hydrants and D rigged hydrants and geysers (`MapBuilder.Hydrants.cs`,
`Neighborhood/HydrantLinks.cs`, `Geyser.cs`, `ManholeLid.BlowOpen`; `inRaces` per link). Check: `ChastainRush.exe -exeRound36 <dir> [-r36Only fps,air,tricks,
event,glass,inside,rooframp,pics,measure,acts,mappic,hydrants,cfps,rig,geyser] -logFile <dir>\Player.log` (`Checks/Round20Runner.R36*.cs`; `measure` writes
targets.json). Imported from Racer: StuntPark's builders, ArcadeActivities (+Stunts, +Smash), ActivitySite, ActivityRecords, StuntEvent, StuntProgress, TrickHud,
MedalUi, DisplayUnits, the stunt profiles, Cup livery, air steering, RaceMenus.Stunt (into GameMenus), StuntBike / StuntBuggy models and their Blender scripts.
**Round 37 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the stunt park's ramps and loop keep
your speed (every "Stunt park ..." collider registered with `VehicleSurfaceContacts`, the roofs' and store floors' slab seams, the loop's own normal,
frictionless body contacts on the park, wheels pushing square to a park surface, `ArcadeVehicle`'s carry on the park's ramps and loop; check switches
`-r37ParkContactsOff -r37CarryOff -r37SpringUp` give Round 36's behavior); B Racer's split screen (`Assets/Scripts/Split/`: `SplitScreen`, `SplitSession`,
`SplitHud`; `Menus/GameMenus.Split.cs`; player 2's seat in `RaceController` (`RaceEntrant.Seat`); player 2's vehicle `vehicle2.json`, name / rider / layout
in settings.json; `StuntEvent` riders; per-player `CameraViews`, `MiniMap`, `TrickHud`, `ArcadeActivities`). Check: `ChastainRush.exe -exeRound37 <dir>
[-r37Only parkramps,parkloop,splitmenu,splitrace,splitai,splitroam,splitpark,splitevent,splitpics,splitfps] [-r37Ramp <names>] [-traffic Normal] -logFile
<dir>\Player.log` (`Checks/Round20Runner.R37*.cs`; two virtual controllers, input counted without focus in a check). Imported from Racer: SplitScreen /
SplitRace / SplitRoam / SplitHud / RaceMenus.Split (rewritten for this game's race, menus and IMGUI HUD), StuntEvent's riders, WeatherEffects' second view,
CameraViews' per-player view; left behind: Police Chase, Speed Patrol, Getaway, Stunt Races, the winner shot, SplitBench.
**Round 38 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the abandoned bank is the stunt
park's (`MapBuilder.StuntParkBank.cs`: hollow, parapets, boarded doors that break; K1 turned round as the BANK HOP, an aimed pop-up onto its roof;
the BANK DROP off its north edge, sized in `make_stunt_park.py`; base "bank" for park ramps); B smashing scores as stunts (`ArcadeActivities.SmashStunts.cs`:
`Value(chunk)` the points, a burst is one link, SMASH a trick type in the air; `TreeTrunks.SnappedBy`); C the shop row's rooms, the shop ramp up through
the roof, the pizza window kicker, the bank's teller counter and vault (`MapBuilder.StuntParkShops.cs`); D stunt rivals in the Stunt event
(`Stunts/StuntRival.cs`: the park's authored lines `make_stunt_park.py` AI_LINES -> `StuntPark.aiLines`, Racer's RivalTricks, `ArcadeVehicle.AiAir`;
`StuntEvent.RivalCount` / settings `stuntRivals`, `stuntDifficulty`; split screen's AI player 2 rides them: `SplitSession.StuntRide`); E Racer's
ambient life (`Scripts/World/Life/`: AmbientLife, LifeSchedule, Wildlife, WoodlandAmbience, UndergroundLife, TurkeyVoice; placed by `MapBuilder.Life.cs`;
Racer's wildlife sounds in `Assets/Audio/Wildlife/`). Check: `ChastainRush.exe -exeRound38 <dir> [-r38Only bankhop,bankthrough,bankpics,smash,shoprow,
shopramp,vault,cpics,cfps,rivals,splitai,life,lifepics,lifefps] -logFile <dir>\Player.log` (`Checks/Round20Runner.R38*.cs`). Imported from Racer:
RivalTricks (into StuntRival), ArcadeVehicle's AiAir, AmbientLife, Wildlife, WoodlandAmbience, UndergroundLife, TurkeyVoice, the wildlife recordings;
left behind: HouseholdSchedule (its Dan / Kyle visits), SnowScenes, ScenePeople, the cave bats, the smoking pair.
**Round 29 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Racer's rider page,
GARAGE > Rider... (`Menus/GameMenus.Garage.cs`), saved in settings.json (`GameSettings.Options.rider`). Check: `ChastainRush.exe
-exeRound29 <dir> -logFile <dir>\Player.log`. Imported from Racer: the Rider page (rewritten into GameMenus).
**Round 30 Part A (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** traffic
(`Assets/Scripts/Traffic/`: `TrafficNetwork` built by `MapBuilder.Traffic.cs`, `TrafficDriver`, `Traffic`, Racer's `AmbientVehicle`),
Settings > GAMEPLAY > Traffic; a check run has none unless it passes `-traffic Light|Normal|Heavy`. Check: `ChastainRush.exe
-exeRound30 <dir> -traffic Normal [-r30Only fps,drive,hit,race,pics] -logFile <dir>\Player.log`.
**Round 30 Part B (see `TODO.md`, status there) is built, pictures from the exe, not yet driven by Dan:** the 24 other
neighborhoods dressed, one era each (`python Tools/mapgen/neighborhoods_rest.py` writes `Data/neighborhood/neighborhoods.json`,
re-run after mapgen; `MapBuilder.Neighborhoods.cs`, new styles E to H in `MapBuilder.HouseEras.cs`). Pictures `local/round30/`.
**Round 32 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Canton Road's businesses
face it with walks, lots and drives (`MapBuilder.Round32.cs`; the pizza / comic book corner of building 76, the ice cream shop 86, church
74's wings and steeple, Pine Hollow Elementary, BUG-009 / BUG-016 roads), every road a ground-following grid (`BuildRoads`: no grass in the
streets), traffic knots untied (`TrafficDriver` Jammed, `Traffic.TakesTurn`), Chastain Lakes' amenity area (`MapBuilder.ChastainLakes.cs`;
the name in `neighborhoods_rest.py`). Checks: `ChastainRush.exe -exeShots local/round32/shots.json <dir> -vehicle drifter -logFile
<dir>\Player.log`; `ChastainRush.exe -exeRound32 <dir> -traffic Heavy [-r32Minutes 5] -logFile <dir>\Player.log`. Build Map writes
`Assets/Generated/round32_lots.txt` (the new lots, walks, drives, fronts) for overhead pictures. Imported from Racer: nothing.
**Round 31 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** BUG-002 stepped garage
row, BUG-003 seam paved (`MapBuilder.Round31.cs`), BUG-004 the I-575 / Chastain Road ramps (mapgen keeps `motorway_link`, appended
after the other roads; `parking` lots in map.json; RampSeams), every other building dressed (`MapBuilder.Businesses.cs`; signs
`python Tools/handmade/make_business_signs.py`, hand-made, committed). Check lists: `python Tools/mapgen/round31_shots.py` (after Build
Map), then `ChastainRush.exe -exeShots local/round31/shots_pics.json|shots_ramps.json <dir> -vehicle drifter -logFile <dir>\Player.log`.
**Was active: Round 30 in `TODO.md`:** Round 30 (traffic ported from Racer, on in free roam and races, can block you, Off in Settings; the current race courses
are closed to it; better houses with a look of their own in every neighborhood not yet
dressed).

**Round 28 in `TODO.md` (Dan, 2026-10-15 03:22), built (ad0ed37 to 4e4cd51):** A manhole covers missing (prove all 13 with pictures); B the Underground race:
shortcuts S1 to S3, the hairpin before the line moved down a level, a ramp back into the hub, resets facing forward, bikes not
turned sideways, the camera keeps the player's view; C riders' hands, fist wave and celebrations, hair styles (fix Short and
Medium, add Mohawk, Crew cut, Spiky).

**Round 28 (see `TODO.md`, status there):** Part A (the manhole covers drawn over the street's own triangles) and Part B (the Underground race:
the span replaces the mid-to-top ramp, the line on it, the grid off the lap; S1 to S3 built: J14 / J15 on M5 with the S2 dive pipe, J2 taller
with S3's high opening; resets on the race's line facing forward; bikes gentler on the walls; the camera view never changes by itself) and Part C (riders: hands round the grips, the fist wave on LB / F, AI reactions and the winner's celebration, Short and Medium remodeled, Mohawk / Crew cut / Spiky / Buzz / Curly; the rider is made here now: `blender --background --factory-startup --python Tools/Blender/rider.py`, Blender at `F:\blender\blender.exe`; pictures `Tools/Blender/render_heads.py`) built and
checked in the exe by script, not yet driven by Dan. Checks: `ChastainRush.ManholeProbe.Look -probeOut <file>` and `ChastainRush.RayProfile.Run
-rays "name:x0,z0,y0,x1,z1,y1,step;..." -probeOut <file>` (Editor batch); `ChastainRush.exe -exeRound28 <dir> [-r28Only
race,shortcuts,resets,bikes,bikewall,camera] -vehicle drifter -logFile <dir>\Player.log`; `-exeShots` takes `"view": "first"`. The exe reads
the routes Build Map embedded in the scene: a route change needs a full build, not `-playerBuildKeepScene`.
**Camera view rule (Dan, 2026-10-15):** the camera view only changes when the player changes it; the pipe camera changes how a
chase view follows, never which view it is.

**Round 27 in `TODO.md` (Dan, 2026-10-14 20:25), all four parts built, driven by Dan 2026-10-15 03:22 (his notes are Round 28) (statuses there):** A buried house walls and the New Chastain bridge's west end;
B Racer's radio, its music handled exactly as in Racer (committed and in the build; Dan, 20:29: for himself and friends, he removes it if he ever sells the game) and AI vehicle sounds; C Racer's settings pages; D the
underground race (gates, switches). Rounds 24 to 26 accepted by Dan 20:21.

**Round 27 Part D (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Underground race
(`python Tools/mapgen/make_underground_route.py` writes `Data/routes/underground.json` from the network; re-run after `pipe_network.py`,
then Build Map): the grid on the hub's top ledge, round M5, J1, M1 and the hub's mid ledge, heights from the route (`loop_y`;
`RaceRoute.Surface` while it runs); during it only, seals across the pipes it does not use and the switch-gate mechanism
(`Race/RaceBarriers.Underground.cs`; route `seals`, `rails`, `switch_gates`). The proposal's switch shortcuts S1 to S3 are not built (do
not fit as built; Dan to pick). All proposal markers are gone (`network_markers.py` and `Data/proposals/` removed). Check:
`ChastainRush.exe -exeRound27 <dir> -r27Only race -logFile <dir>\Player.log`.

**Round 27 Part C (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Racer's settings
pages (`Menus/GameMenus.Settings.cs`: AUDIO, DISPLAY, CONTROLS; Q / E or LB / RB step the tabs; the Screen row moved to Display) with the
volumes applied through `Audio/SoundMix.cs` (Effects and Ambience; Vehicle, Music and Master as Racer). Check: `ChastainRush.exe
-exeRound27 <dir> [-r27Only settings] [-radio] -logFile <dir>\Player.log` (`Checks/Round27Runner.cs`).

**Round 27 Part B (see `TODO.md`, status there) is built and checked in the exe by script, not yet heard by Dan:** Racer's radio
(`Audio/LocalRadio.cs`; stations are the folders of `Music/` beside the exe; D-pad down / N station, right / . next, left / , previous,
up / I what is playing) with its settings in `Audio/GameSettings.cs` (`settings.json` beside the records); the music is committed in
`BundleMusic/` (771 MB, plain git like Racer's, no LFS) and copied by the exe build into `Builds/ChastainRush/Music/`; a check run starts
the radio only with `-radio`. Racer's OtherEngines for the AI engines (`Vehicle/OtherEngines.cs`). Docs: `Docs/RADIO.md`.

**Round 27 Part A (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** every styled house
settled on its slope (`MapBuilder.HouseSettle.cs`: buried lots cut, block foundations under raised floors, walk-out basement walls; list
`Data/screenshots/round27/houses_settled.txt`), every bridge end run smoothly onto the road (`MapBuilder.BridgeEnds.cs`: Hermite easing,
chained OSM pieces joined into one deck, abutments; the I-575 ramps cut by the map's edge left as they were), and terrain under a bridge
deck no longer a contact for a vehicle on the deck (`VehicleSurfaceContacts.cs`). Checks: `ChastainRush.BridgeProbe.Run -probeOut <file>`
(Editor batch); `-exeShots` shots take `"trace": true` (a per-step `<name>_trace.tsv` with every body collision, `Checks/ContactTracer.cs`).
Imported from Racer: nothing.

**Round 26 (see `TODO.md`, status there) is built and checked in the exe by script, driven and accepted by Dan 2026-10-14 20:21 (nothing open; new issues come as bug reports):** Dan's log ramp
(RE2) and every take-off's foot fitted flush to the ground (`MapBuilder.BigJumps.cs` footBank; log ramps solid with sloped sides;
`BuildPrism` meets the ground with a 15 degree wedge where an edge stands up to 0.3 m proud); a sweep of every ramp on the built
colliders (`ChastainRush.RampSweep.Run -rampOut <file>`, batch, report only); every pond's bank raised to its water
(`MapBuilder.PondBank.cs`, nine ponds); the pipe express on every run over 100 m (`pipe_network.py`, 37, each with its own top
speed, no ride over about 12 s); driving on the walls and the ceiling (`DrainPipe.cs`: one g into the wall, the nose kept along
the pipe, eased to the floor before an end or an express; `VehicleSurfaceContacts.cs` barrel contacts from the vehicle's up;
`ChaseCamera.PipeUp` rolls). Check: `ChastainRush.exe -exeRound26 <dir> [-r26Only ramps,express,walls] [-wallRideRound25]
-logFile <dir>\Player.log`. Imported from Racer: nothing.

**Round 25 (see `TODO.md`, status there) is built and checked in the exe by script, driven and accepted by Dan 2026-10-14 20:21 (nothing open; new issues come as bug reports):** Part A cleanup: street
manholes are a hatch of the street itself with a 0.9 m cover, the collider the terrain's own surface, the riser 0.3 m under the street
(`MapBuilder.Network.cs`; check `ChastainRush.ManholeProbe.Run -probeOut <file>`); mouths set into banks without terrain blades or sky
gaps (`MapBuilder.Pipes.cs` HoleBehind, RaiseBehindHead), every network pipe under 0.6 m of ground (`MapBuilder.NetworkCover.cs`), a
sweep of every opening in each build (`MapBuilder.MouthSweep.cs`, `Assets/Generated/mouth_sweep.txt`); hub openings off the ramps
(`pipe_network.py` hub_ramp_clamp); glass stretches in a bowl in the pond bed, holes only under water (`MapBuilder.ClearPipe.cs`
BowlY); exit streams end at the apron or run into water; turtles, snakes, ducks and geese at the glass (`PipeAquarium`). Part B: debug
menu "Pipe warp" (`Pipes/PipeWarp.cs`, `MapBuilder.NetworkWarp.cs`) and the pipe express (`Pipes/PipeExpress.cs`, network.json
'express', debug menu "Pipe express: Boost / Warp / Off", free roam only). Exe profile at the glass: `-exeProfile <dir> -profileOnly glass
[-aquariumPlain]`. Imported from Racer: nothing. Follow-ups: RD "Gully leap" and RE "Flood-plain run" built (reverse, official; `make_creek_track.py` 4d), RA and RC kept; R1's
side-yard fences solid only in races (`RaceOnly.any` / `freeRoam`); the Enclave pond's banks raised to its water (`MapBuilder.PondBank.cs`);
lighter manhole covers.

**Round 24 Part B items 4 to 9 (see `TODO.md`, status there) are built and driven by Dan 2026-10-14 (Round 25 notes); was not yet driven
by Dan:** every network mouth reworked (`MapBuilder.Pipes.cs`: exits and BF set into graded banks with short wingwall aprons, `Bank`;
GP `covered`, a box culvert under a berm, its face at the pond; never on roads / drives / sidewalks / the trail or within 2 m of a
building; `python Tools/mapgen/network_sweep.py --regraded` sweeps buildings against `Assets/Generated/regraded_mouths.txt`); the
glass under the lakes in open water (`ClearFrame.TS`); chevrons to the hub on the mains' stripes and HUB signs in the junctions
(`to_hub` / `hub_tube` from `pipe_network.py`); all six mains pour into the hub (M2 via K1 to K3, M3 at the mid ledge, M4 from J6, M6
from Nelson Lake; B27 is the old J4 to J6). Race markers moved, race not built. `-exeShots` takes `y`. Imported from Racer: nothing.

**Round 24 Part A3 (see `TODO.md`, status there) is built; pictures from the exe by script, no drive tests, not yet driven by Dan:**
B0 BUG-002's house was already whole (L_W8 gone, pictures from Dan's spot), no fence over any cut (`MapBuilder.OverCut`), and a
sweep of every building against the network's cuts (`ChastainRush.NetworkSweep.Run -sweepOut <dir>` [-sweepProfile x0,z0,x1,z1],
then `python Tools/mapgen/network_sweep.py <dir>`): none found. B1 full screen at the desktop's resolution, or windowed, from the
menus' "Screen" row (`ScreenSetting`, `display.json` beside the records; never in a check run). B2 #2's worn tracks to 1627's
sliding door. B3 new reverse-only Noonday Creek candidates RD, RE on TEMPORARY markers (`python
Tools/mapgen/creek_reverse_candidates.py`, `Data/proposals/round24_creek_reverse.json`, read with the race's proposal file by
`MapBuilder.Proposals.cs` and `make_proposal_labels.py`). Imported from Racer: nothing. Next: Part B.

**Round 24 Part A2 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the frame
rate (cause: `DrainPipe.FixedUpdate` searching every pipe piece for every piece and vehicle each physics step; races 3 fps before,
134 to 192 fps after), mouths off roads / sidewalks / drives / the trail (`DriveBands`, `ChastainRush.MouthScan.Run -mouthOut <file>`),
mouths closed during races (`PipeMouths`, `RaceBarriers`), the big map clamped, Racer's minimap (`Map/MiniMap.cs`, J / B). Profiling
in the exe: `PlayerBuild` with `-playerBuildDev` builds `Builds/ChastainRushDev/` (development, profiler markers); `ChastainRush.exe
-exeProfile <dir> [-exeProfileRaw] [-profileShots] [-profileOnly name,...]` writes `profile.txt`; `ChastainRush.ProfileDump.Run
-profileRaw <file.raw>` lists a capture's slowest markers. Imported from Racer: RacingMiniMap (rewritten). Next: Part B.

**Was active: Round 24 Part A2 in `TODO.md` (urgent):** frame rate broken
in all races since the pipe network / new cameras (find the cause with the
profiler, back to 60 fps); no mouth, railing or wall on any road, drive,
sidewalk or the trail; every network opening closed during races and the AI
kept out of pipes not in the race; the big map clamped to the world's edges;
Racer's minimap. Then Part B (entrances rework, open-water aquariums, hub
arrows, every main into the hub). Race markers kept, race not built.

**The Round 23 follow-ups (see `TODO.md`, status there) are built; stages 1 and 2 checked in the exe by script, stages 3 and 4
built only (Dan asked to test them himself); none driven by Dan yet.** Part A (e1ba748) the Enclave lot, gate, sidewalk, court
fence, loungers. Part B (d2906dc) and Part C (stages af67ca7, fd784ec, 20b6a64, then stage 4): the whole pipe network as laid out
by `python Tools/mapgen/pipe_network.py --stage N` (writes `Data/pipes/network.json` and `Docs/PIPE-NETWORK.md`; re-run after
mapgen), built by `MapBuilder.Network.cs` / `NetworkHub.cs` / `PipeFlow.cs` (runtime: `NetworkCells`, `ManholeLid`,
`ManholeTrigger`, `NetworkWaterfall`, `DrainPipe` open ends). BF and GP (the Glen pond culvert) are twin entrances whose barrels
split two ways; J1 as a room inside BF is gone. Proposal markers: `python Tools/mapgen/network_markers.py` (what is unbuilt plus
the underground race, kept), then `make_proposal_labels.py`. No race, gates or switches. Exe shots: `-exeShots` now takes
`shot_near`, `fps_near`, `reset_at`. Imported from Racer: nothing.

**Round 23 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan; STOPPED for
Dan's picks on the network:** Part A (2b5cac5) the Enclave lot out to Dan's spot. BF, the network's main entrance
(`Data/pipes/pipes.json` `twin` / `chamber_b` / `chamber`; `MapBuilder.Pipes.cs`, `MapBuilder.NetworkEntrance.cs`): twin 5 m
pipes from the end of the creek channel west of Bells Ferry Road (straight ahead of Dan's spot x 1413.47, z 1176.45, heading
41) down under the road INTO J1, the first junction chamber (22 x 28 x 8 m), whose four onward stubs end at closed grilles
until the network is built; no way out on the far side of the road. It is NOT a culvert under the road. The network
proposal: `python Tools/mapgen/pipe_network_proposal.py` (re-run after mapgen; writes `Data/proposals/round23_network.json`,
`Docs/PIPE-NETWORK-PROPOSAL.md`, `local/round23/network_overhead.png`), then `python Tools/handmade/make_proposal_labels.py`,
then Build Map. TEMPORARY markers (`MapBuilder.Proposals.cs`, `Debug/ProposalMarkers.cs`, `Assets/HandMade/Proposals/`, the
map layer `WorldMap.Proposals`, the debug menu's "Proposal markers" and "Go to" rows; a check shows them with
`-proposalMarkers 1`): remove when Dan has chosen. Imported from Racer: nothing.

**Round 22 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Grasmere Court,
Chastain Commons and Chastain Enclave dressed like Chastain Glen (`python Tools/mapgen/communities_area.py` writes
`Data/neighborhood/communities.json`, re-run after mapgen; `MapBuilder.Communities.cs`: per-community style mix, larger Enclave
houses with three-car garages, the Enclave pool, clubhouse, three tennis courts, pavilion and lot, the two communities'
monuments). Imported from Racer: nothing. Next: Round 23, the pipe network (proposal first).

**Was active: Round 22 in `TODO.md`:** dress Grasmere Court (part of
Chastain Glen), Chastain Commons (same styles) and Chastain Enclave (larger
houses, a community pool and tennis court near its entrance), with entrance
signs. Round 21 and its follow-ups are built and driven by Dan. Queued: 23
the map-wide pipe network (proposal first).

**The Round 21 follow-ups (see `TODO.md`, status there) are built and checked in the exe by script, not yet driven by Dan:**
P1's clear stretch: a pool at the small pond's level dug over P1's middle (the pond itself is too narrow on P1's line), 30 m
of glass tube in a deep-water trench with fish, weeds, light shafts (`MapBuilder.ClearPipe.cs`, `Pipes/PipeAquarium.cs`,
shaders `PipeGlass` / `UnderwaterScroll` / `Fish`; `pipes.json` `clear`, `mouth_lamp_b`). RA "Pipe dive" and RC "Creek leap
back" (reverse, official; `make_creek_track.py` section 4c, looks `rockheap` and `washout` with a back in
`MapBuilder.BigJumps.cs`). Breakable trees 10 m round the trail, 15 m on the named bends, `break_lines` round RA / RC and
hit trunks (`noonday_creek.json` `break_left` / `break_right` / `break_lines`). Proposal markers gone. Check: `ChastainRush.exe
-exeRound21 <dir> -r21Only tube,ra,rc,n2,ramp,fwdlap,revlap -logFile <dir>\Player.log` (`Checks/Round20Runner.R21F.cs`).
Imported from Racer: nothing.

**Round 21 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Part A (03db48a): R1
follows the AI shortcut rule, AI mistakes (`DriverVariation`: late braking, curb clip, short / off-line jumps; R1 sloppy tries into the
pool over a near step, `PoolHold`), Chastain Glen checkpoints 952 / 1056 / 1216 at 12 m against free cuts. Part B: Noonday Creek
reverse (route `directions`, `ai_zones_reverse`, `RaceController.AiZones`): every big jump has a back that is the reverse take-off
(`MapBuilder.BigJumps.cs` `BuildBack`, a second BigJump), RJ4 over the road (`MapBuilder.CreekTrack.cs`). Part C: candidates RA to RC
on TEMPORARY markers (removed in the follow-ups with their script). Check: `ChastainRush.exe -exeRound21
<dir> [-r21Only r1,six,races,revlap,fwdlap,revslow,revrace] -logFile <dir>\Player.log`. Imported from Racer: nothing.

**Round 20's follow-ups (see `TODO.md`, status there) are built and checked in the exe by script, not yet driven by
Dan.** Queued, not started: 21 the map-wide pipe network (proposal first); 22 Chastain Commons, Chastain Enclave and
Grasmere Court.
- Noonday Creek N2 "Low creek leap" (official, forgives CP4) and N5 "Ford leap" (a lap option beside the ford, forgives
  nothing: `lap_option`), both in `make_creek_track.py` (`leap()`), looks "washout" / "logramp" and N2's knocked-over
  barricade in `MapBuilder.BigJumps.cs`. The Round 20 proposal markers, their data, labels, debug-menu rows and map layer are
  gone (`creek_shortcut_candidates.py` and `make_proposal_labels.py` with them).
- R1 on Dan's line (reverse, forgives CP5; `make_shortcuts.py` `build_r1`, replaces the kids'-ramp trial): a big homemade
  ramp over a privacy fence into the 1987 / 1552 side yard (levelled lawn), the flight across #6's open landing strip over an
  ABOVE-GROUND pool in the back yards behind 1572 / 1622 (Dan 03:51; 1.25 m steel wall, 1 m of water; out by a ramp inside
  and a deck on the rim you drop off; `Neighborhood/PoolHold.cs` stops a vehicle in it 2.5 s, then 1 m/s), an open side gate in a fence across the 1572 / 1622 side yard, breakable patio things; solid fences
  across the lobe's other side yards, three of them (in #6's corridor) only during a reverse race (`Race/RaceOnly.cs`).
  Built by `MapBuilder.R1Pool.cs` (terrain: `GradePools` in the regrade). Route fields for the AI: `ai_take` (its own chance
  of taking a shortcut), `ai_slip` / `ai_slip_profile` (a sloppy try; off on R1: it got an AI stuck at the pool), `lap_option` (`RaceRoute.cs`,
  `RoadDriver`, `ShortcutStrategy`).
- `python Tools/mapgen/free_cuts.py` (report only): free cuts through yards on the Chastain Glen loop, both directions
  (`local/round20/free_cuts.txt|png`, git-ignored).
- Trampoline sound drop-in: `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\Trampoline\trampoline*`
  (.wav/.ogg/.mp3; several: one at random per bounce), made with a READ ME on the game's first start (`SoundDropIns`).
- Check: `ChastainRush.exe -exeRound20 <dir> -r20Only fences,tramp,n5,n2,r1 [-soundsRoot <test dir>] -logFile
  <dir>\Player.log` (Checks/Round20Runner.Follow.cs). Imported from Racer: nothing.

**Round 20 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** BUG-001 (BJ1's
railings flare out, smooth solid panels, 7 m deck from the lowest ground) and BUG-002 (P1's outfall apron over the pond,
`BigJump.vmin`), commit a9eb434. The map (`Assets/Scripts/Map/WorldMap.cs`, `WorldMapOverlay.cs`, picture baked by
`MapBuilder.MapPicture.cs` into `Assets/Generated/Resources/WorldMapPicture.png`): View / Select or M opens it and pauses
the game; bug capture moved to the right stick's click (F4 / ] unchanged). `python Tools/mapgen/r1_original_options.py`
(R1 with houses altered, report only; not chosen). Check: `ChastainRush.exe -exeRound20 <dir> [-r20Only bj1,ramp,map,lap]
[-r20Minutes 25] -logFile <dir>\Player.log`. Imported from Racer in Round 20: the map (ExplorationMap and its controls,
WorldMapCourseOverlay; rewritten and trimmed, left-behind list in `WorldMap.cs`).

**Round 19 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Noonday
Creek trail 4.5 m wide (no solid trunk within 3.5 m; the trees left within 6 m, and within 8 m of each big jump's run-out,
breakable: `TreeField.breakable`, `TreeTrunks` trigger capsules, `FallingTree`); four big jumps (`MapBuilder.BigJumps.cs`:
a washed-out footbridge, a step-down off the bank, an earth ramp over a fallen tree, a granite slab; `BigJump.cs` sends a
vehicle off the lip at the lip's angle, capped at 24 to 27 m/s); J2 and J3 gone; P1's rock bar (`pipes.json`
`rock_bar_a`, `DryPatch`); every pipe now round all the way, stained and dim, a stream (`PipeStream`), wall riding and a
rescue in `DrainPipe`, the camera on the pipe's axis (`ChaseCamera.PipeView`), headlights on in pipes (`VehicleLights`),
`ArcadeVehicle.SurfaceUp`; the AI passes on the side away from a slower vehicle on a trail; R3's PARTY sign, balloons,
tracks and play-set bunting (`MapBuilder.PartyHint.cs`, `Tools/handmade/make_party_sign.py`, hand-made, committed); R1 built
as a trial (`make_shortcuts.py` `build_r1`, the kids' ramp, a JUMP! sign). Check: `ChastainRush.exe -exeRound19 <dir>
[-r19Only trail,jumps,bar,pipe,stream,penalty,r3,r1,ai,race,pass] -logFile <dir>\Player.log` (Checks/Round19Runner*.cs).
Pictures: `local/round19/` (git-ignored). Imported from Racer in Round 19: nothing.

**Round 18 items 3 to 9 (see `TODO.md`, status there) are built and checked in the exe by script, not yet driven by
Dan:** the Noonday Creek trail (track B, a narrow woods trail for the motorcycles, the ATV and the mower; J4 over New
Chastain Road), pipe P1 under the ground (terrain holes over its cuts only), the "Noonday Creek" route with a Route row in
the race setup menu and the AI's trail mode, and reverse shortcut R3 (a giant play set's slide onto a trampoline that
throws the vehicle over house 1752). R1 was NOT built: as marked it puts fast vehicles into 1572's wall (numbers and
the one robust layout in `TODO.md`); R2 not chosen. Imported from Racer in Round 18: nothing. Queued: 19 the map-wide
underground pipe network (proposal first); 20 Chastain Commons, Chastain Enclave and Grasmere Court.

**Round 17 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Part A
(b512061, b3bfe9f), then item 5 and Part B: Dan's two junction jumps smoothed in the map build at those spots only
(`MapBuilder.Round17.cs`: BUG-001 at the entrance, BUG-002 at Chastain Corner with the crest on its approach), and the
shopping center (`MapBuilder.ShoppingCenter.cs`): the lot graded and paved (outline by hand, `LotRing`) with stalls,
islands and sidewalks, shop fronts with made-up names (`python Tools/handmade/make_store_signs.py` draws
`Assets/HandMade/ShoppingCenter/StoreSigns.png`, hand-made, committed; its row order matches `SignRows`), the gas
station with breakable pumps that explode for show (`PumpChunk`, `PumpBlast`, sound drop-in `Sounds\Breaks\boom*`), the
abandoned bank. Exe drives: `ExeShotsRunner` now reports the longest spell with no wheel down. Imported from Racer:
nothing.

**Round 16 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** items A to E
(commit a70d0d4: the culvert set into the bank, the solid dirt pile, breakable signs; C and E reported, not changed), then
the menus and the garage (`Assets/Scripts/Menus/`: `GameMenus` from Racer's `RaceMenus`, with Racer's `MenuInput`,
`MenuGlyph`, `MenuActionRegistry`). Title menu at start; Start / Esc pause; race setup (laps, direction, opponents,
difficulty, opponent vehicles); garage with the turning model and stat bars (vehicle and colour saved in `vehicle.json`).
The D-pad vehicle switch and the T / X race keys are gone; the debug menu is F6 / `\` only (Start is the game menu's).
Menus never appear in a check run; `ChastainRush.exe -exeRound16 <dir>` drives them with a virtual gamepad.
Imported from Racer in Round 16: MenuInput, MenuGlyph, MenuActionRegistry (copied), RaceMenus' shell, navigation, focus
frame, garage preview and stat bars (re-written into GameMenus, trimmed). Left behind: RaceFlow, campaign, shop, unlocks,
police, split-screen, playlists, courses, radio, records boards, rider page, settings, on-screen keyboard, loading screen.

**Round 15 (see `TODO.md`, status there) is built, checked in the exe by script (muted), and driven and approved by Dan apart from Round 16 items A to E:** Part A
(commit 1114c66): `CheckRun` mutes every check run and keeps its files in the check's folder; water with a 0.5 m bed under
every pond, lake and stream, cut by the map build (`MapBuilder.Water.cs`), registered with Racer's `ShallowWater` (outline
mode added) and Racer's `WaterFeedback`; the pool is water with a shallow end; break sounds (`BreakSounds`, Racer's
`SmashAudio` way); sound drop-ins `Sounds\Geese\` and `Sounds\Breaks\` beside the records (`SoundDropIns`). Part B: three AI
drivers in every race (T): Racer's `RoadDriver` (trimmed), `RaceRoad`, `ShortcutStrategy`, `DriverVariation`,
`AiFinishEstimate`; every car is a `RaceEntrant` under `RaceController`'s rules; Racer's grid (player at the back), names and
paints (EMBER, GOLD, BLUE); place on the HUD and the finishing order on the finish panel; AI engine sound in 3D
(`VehicleAudio.driver`); tree trunks solid round the AI too (`TreeTrunks.Track`). AI vehicles: random each race, or fixed in
`ai_drivers.json` beside the race records (`local/` in the Editor). Imported from Racer in Round 15: ShallowWater's
`WaterFeedback`, `SmashAudio` (as BreakSounds), `RoadDriver`, `RaceRoad`, `ShortcutStrategy`, `DriverVariation`,
`AiFinishEstimate` (all commented in place); left behind: RaceDirector, RaceFlow, RacerState, RaceProgress, traffic,
split-screen, campaign, name tags.

**Round 14 (see `TODO.md`, status there) is built, checked in the exe by script, and driven and approved by Dan (no complaint from Dan after driving means approved):** the small
vehicles' slow-downs were their BODY contacts with the terrain: speculative CCD (Racer's setting for small vehicles) plus a
TerrainCollider gave contacts with horizontal normals (Needle 600 30 m/s lost in 40 ms). Racer's ground is all mesh and its
`VehicleSurfaceContacts` fix covers it; here that fix now covers the terrain tiles (heightfield normal, gap measured straight down
from the body's corner) and `BridgeDecks`. No profile changed, no hill assist (hills measured: not the cause). #6's kicker is
drawn as a dirt pile (`DirtPileLook`, look only; collider and drive unchanged). Check: `ChastainRush.exe -exeRound14 <dir>
[-r14Vehicles ids] [-r14Tag t]` (add `-noSurfaceContacts` for "before"), plots `python Tools/mapgen/round14_plot.py <before>
<after> <out>`; results in `Data/screenshots/round14/`. Imported from Racer in Round 14: nothing.

**Round 13 (see `TODO.md`, status there) is built and checked in the exe, driven by Dan:** trees moved off
shortcut #2's line (none within 6.5 m of either line); #6's kicker drawn as a flatbed trailer (look only); Dan's penalties were
CP2 (#2's entry plane missed by his line round 1539), fixed by shortcut CORRIDORS (`RaceShortcut.InCorridor`: 7 m either side of
the line, 12 m+ off the lap). Racer's eleven player vehicles with sound; the Street Classic is the default and drives exactly as the
retired prototype. Temporary vehicle switch: D-pad left / right or `,` / `.` while not racing (`VehicleSwitch`, saved in
`vehicle.json` beside the race records). Records are per vehicle (the Street Classic keeps the old key). Checks:
`ChastainRush.exe -exeRound13 <dir>` (every vehicle: load, climb, sound, pond fps), `-exeShortcut <dir> -vehicle <id>
[-shortcutRaceOffset id:m,...] [-shortcutNoEntryPlanes]`; any exe check takes `-vehicle <id>` and never reads or writes the saved
choice. Imported from Racer in Round 13: VehicleProfile (police removed), VehicleConfiguration, VehicleVisual (with
VehicleMeshLifetime, MotorcycleFrontEnd, VehiclePose), VehiclePaint, VehicleLights, VehicleContact, RiderLook, RiderGestures (with
RiderArms), VehicleAudio, VehicleSoundSynthesis (all in `Assets/Scripts/Vehicle/`, trims commented "Chastain Rush" in place), the
models `Assets/Resources/VehicleModels/` (11 vehicles + Rider.fbx) and `Assets/Resources/VehicleGlazing.mat`; ArcadeVehicle took
Racer 0.97's VehicleConfiguration hooks (weather grip and WaterFeedback left behind). Left behind: police, WeatherGrip,
CameraViews, VehicleUnlocks, SmashAudio, Racer's VehicleRespawn and VehicleInput changes.

**Round 12 (see `TODO.md`, status there) is built and checked in the exe, driven by Dan:** 1763 is an ordinary house
again; #2 leaves the lane past it through the side yard (one hand-painted "SHORTCUT" sign at the turn-off, the only
signposted shortcut), runs down held grass (`LandingZone.hold`, RampHold) through an S into 1627's sliding door on a 12 deg
climb, up its stairs to a kicker and out a big upstairs window; roof houses beyond (convex hull colliders); sidewalks stop
at drives; the clubhouse replaces the big pool building; the start gate only in a race; #6's kicker drawn as a board on
hay bales (collider unchanged). Check: `ChastainRush.exe -exeRound12 <dir>` (Checks/Round12Runner.cs). The sign's face:
`python Tools/handmade/make_shortcut_sign.py` (hand-made, committed). Imported from Racer in Round 12: nothing.

**Never build a "nearest version" of something Dan has described.** If what
he asked for does not fit, build nothing of it, leave things clean, and
report the numbers and options. He has rejected shortcut #2 three times for
approximations.

**Round 11 (see `TODO.md`, status there) is built and driven by Dan; its item 1 was rejected:** #2 is now a jump through a
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

**Pull Dan's bug reports yourself (Dan, 2026-10-10).** At the start of every round, read every debug session newer than
the last one handled in `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\DebugReports\` (each `<session>` folder
beside its zip: `BUG_REPORT.md` and `Screenshots\`). Read only; never move or delete them. Last read: session
`2026-10-16_21-19-50-140_b75298` through BUG-001 (Round 38; before it `2026-10-16_01-35-35-872_a132c0` through BUG-010, Round 34).

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
