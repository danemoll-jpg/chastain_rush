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
- **World scale is real:** 1 unit = 1 metre. Since Round 59 the map reaches west and south of the old map's corner (Round 61: x and z down to -8629.83 / -8311.33);
  everything made before keeps its coordinates. Vertical exaggeration is off (1.0) by
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
- **Shortcuts must be a risk** (Dan, 2026-10-18): "A shortcut has to be somewhat challenging so that is a risk to take it
  and to make people wonder if it is worth the risk (which I thought was there from the beginning)". Round 44's lake jump
  failed this ("just a simple ramp that jumps a narrow part of the lake"). Getting it wrong must cost (into the water, a crash,
  a miss that loses more than it saves). Never choose the easiest crossing or shortest gap to make a check pass; if the risky
  version will not work, report it instead of building a safe one.
- **No free way round a shortcut** (Dan, 2026-10-19, after Round 49's kicker "really did nothing ... you could just drive
  through this area and skip the ramp and nothing will slow you down"). Block every free way through a shortcut's ground with
  things you can drive through that slow you hard (bushes, hedges, gardens, woodpiles, breakable fences), placed so the stunt
  carries you over them. Every shortcut check drives its ground WITHOUT the stunt; that run must be slower than the plain lap.
- **Tire tracks are the hint** (Dan, 2026-10-19: "the tire tracks are enough to know it is a shortcut ... we can stop putting
  these signs up"). Where a turn-off has clear worn tracks, no sign is added.
- **Paths look like the real ones** (Dan, 2026-10-19, the Chastain Lakes trail: "i really don't think it needs to be widened.
  vehicles can also drive in the grass"). A footpath or trail is built at its real width, lying flat on graded ground, with
  smooth drivable grass beside it; the race line may use the grass. (Noonday Creek's trail was widened at Dan's request; that
  stays.)
- **Pipes through water are aquarium pipes** (Dan, 2026-10-18): "All pipes that go through a body of water must be aquarium
  pipes." Under or through a lake, pond or creek, a pipe or tunnel is a glass run (`PipeAquarium`, `MapBuilder.ClearPipe.cs`),
  never plain concrete.
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
- **Real jumps off ramps** (Dan, 2026-10-17). Dan: "Lets not do fake jumps off of ramps. If there is a ramp we do
  actual jumps only. If we do something on a scripted jump path it has to be something creative and not just a ramp. Lets keep
  this as a standing rule." From now on a ramp, kicker, pile or stair launches by physics alone: no aimed or scripted flight
  off it (the landing is sized to the real flight instead). A scripted or aimed path is allowed only for something that is not a
  ramp and is creative (a water plume, a geyser, a trampoline). Launches built before this rule are left as they are until Dan
  points at one.
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
- **One map rebuild per round, and a limit on tuning — DECIDED (2026-10-18).** Dan, after Round 41 ran 9 hours (Part A alone
  7.5 h of 30-minute Build Map runs while tuning the crypt toward a time target it never reached): (1) a round makes all its
  world changes first and runs Build Map and the exe build ONCE, at the end (code-only steps in between use
  `-playerBuildKeepScene`); each part still gets its own commit and its own targeted check on that one build. (2) Tuning
  limit: if a number the round asks for (time saved, air time, height, speed) is not met after two attempts, stop tuning it,
  report the numbers in the status and move on; Dan decides. Optional ideas in Dan's notes ("could", "maybe") are built as
  the simple version, not as requirements to tune.
- **CHECK IT AS DAN PLAYS IT (Dan, 2026-10-26, Round 73).** Scripted perfect lines are not enough.
  (1) Every report of Dan's: a picture from his exact position and heading, before and after, that Code LOOKS AT and describes in the
      status; a report is closed only when the after picture shows the thing gone. Re-read every earlier report on the same ground too.
  (2) Every feature: play it from where a player starts (the title, race setup, Free Roam's spawn or fast travel, a road nearby), with the
      normal controls through the game's input (a human-like driver / pilot: reaction lag, imperfect lines, a missed gate, a crash and a
      reset), and pictures from the player's camera along the way that Code looks at: can a first-time player see where to go? does it
      look right (no blocks, nothing buried, nothing floating, nothing across the road)? List what a first-time player sees and where it
      went wrong.
  (3) The status says, per item, what was looked at and what it showed. "Checked by script" alone is not done.
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

- **Build speed and quick builds (Round 65).** Generated meshes are packed 300 to an asset file as sub-assets (`PackMesh` in `MapBuilder.Quick.cs`; never one
  asset per mesh: about 30 ms a file, `BuildSpeedProbe`). Full Build Map about 15 min, the exe after it about 4 min. Quick builds now take PLANNING changes to the
  ground (gradings, aprons, levelling): the new planning's ground where it changed; refused with the steps named if a step not rebuilt changed the same samples;
  other steps' renderers on the moved ground listed in the log. Quick steps: Businesses, ChurchCrypt, Mall, Ridge, Pipes, Addison, TrailRace, Shortcuts, CreekTrack,
  LakesRace. A quick build is about 10 min (mostly the planning's replay), with its exe about 14. Fix-and-check cycles in a round: quick builds; the round's last build full.
- **Quick builds (Round 42; use them from now on for a world fix).** Build Map is its planning (about a minute) then named steps
  (`MapBuilder.Quick.cs` `Steps`: Roads, Buildings, ..., Businesses, ChurchCrypt, ..., Ridge, Pipes, ...). A full Build Map records
  what each step made and every terrain sample it changed in `BuildCache/steps/` (git-ignored). A quick build reruns only the
  named steps on the saved scene (planning replayed, the ground rewound to before them, later steps' samples put back, other objects'
  references into them set again) and, with the Exe entry, builds the exe from it:
  `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.MapBuilder.QuickExeFromCommandLine -quickParts <steps> -logFile <file>`
  (`QuickFromCommandLine`: the scene only; `-dumpOut <file>`: the scene's fingerprint after). Steps that can be rebuilt alone: Businesses,
  ChurchCrypt (`church` = both), Ridge (the roof ramp included), Pipes (the network's exits' ground). Tree clearing (a track's `clear_lines`) is the Trees
  step's, so a new or moved clear line needs the full build (Round 43). A change to the planning (grading,
  the layout of houses, roads or lots) or to a step not on that list needs the full build, and so does the round's last build (the rule
  above: one full Build Map at the end). If the scene was built or saved by anything else since the record, the quick build refuses.
  Times on Dan's PC (Round 42, with the PC in use): full Build Map 11 to 18 min (was 32); Ridge quick to a playable exe 5 min; church
  quick to the exe about 8 min (its Businesses step is one to two minutes). Step times: `Assets/Generated/build_times.txt`, `quick_times.txt`.
  Proof a quick build matches a full one: `ChastainRush.SceneDump.Run -dumpOut <file> [-dumpDetail <text>]` (Editor batch: one line per
  object and per terrain block, with what its components refer to) of both, then `python Tools/scene_diff.py <a> <b> [--box x0,z0,x1,z1]`.
  `ChastainRush.MeshProbe.Run` takes `-probeTop <y>` (rays from under a roof).
- Check runs of the exe minimized and not activated (Round 45): start it through `Win32_Process.Create` with `Win32_ProcessStartup.ShowWindow
  = 7` (SW_SHOWMINNOACTIVE), `-screen-fullscreen 0 -screen-width 1280 -screen-height 720`, and its own `-logFile`.
- Round 43 check: `ChastainRush.exe -exeRound43 <dir> [-r43Only plume,ghost,winner,trailer] -logFile <dir>\Player.log` (`Checks/Round20Runner.R43*.cs`).
  Trailer Mode: F8 or the pause menu's TRAILER MODE page (`Trailer/TrailerMode.cs`); screenshots to `Screenshots\` beside DebugReports.
  Ghosts: `CleanLapGhosts\` beside the race records (`Race/CleanLapGhost.cs`). A quick build now refreshes the race's route copies.
- Round 42 check: `ChastainRush.exe -exeRound42 <dir> [-r42Only windows,aisle,plume] -logFile <dir>\Player.log` (`Checks/Round20Runner.R42.cs`).

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
**Round 75 (see `TODO.md`, status there) is built and checked in the exe through the game's own input, not yet driven by Dan:** A race setup's RACE TYPE row
(Street / Air) filters the Route row and the garage (`GameMenus.RenderRaceSetup`, `ChooseRoute`: a course chosen after a race leaves it first); each type keeps its
last course (race_settings.json `routeStreet` / `routeAir`) and vehicle (vehicle.json "plane"; `VehicleSwitch.PlaneVehicle`, `RaceVehicleFor(route)`). B the map's
picture also drawn from the built scene (`MapBuilder.MapPicture.cs` DrawBuilt: every visible mesh's upward faces on or over the ground; MapPicture is a quick step and
every quick build runs it); street names on the map (`Map/WorldMap.Streets.cs`, data `Assets/Generated/Resources/MapStreets.json` from the build; route shields from
`Data/road_refs.json`: `python Tools/mapgen/road_refs.py`, re-run after mapgen), the minimap's street (`RoadLookup.StreetAt` / `Short`); the clouds at their own
height again, broken up near the camera (`StylizedClouds.shader`). C the summit leap's run-out eased onto its track; banks sized from the ground at their edges, held
off roads and trails (`Tools/mapgen/bank_room.py`) and off the middle between two legs (`make_summit_leap.py`, `make_mountain_route.py`). Check: `ChastainRush.exe
-exeRound75 <dir> [-r75Only dan,types,mapb] -vehicleSave <file> -logFile <dir>\Player.log` (`Checks/Round20Runner.R75.cs`; `GameMenus.OpenTitleForCheck`).
Imported from Racer: nothing.
**Round 74 (see `TODO.md`, status there) is built and checked in the exe through the game's own input, not yet driven by Dan:** Dan's reports of session
`2026-10-27_02-23-23-098_3e251a` (BUG-002 to 005) and his 03:06 to 04:40 list. A planes: propellers / rotors turn in replays (`ReplayViewer.MakePuppet` keeps
`PlaneFlight` / `HelicopterFlight`, which turn them from the replay's throttle), race setup's Garage for a route with its own vehicles (`garageFor` "route"),
the Sky Hopper's wing skylight (`Tools/Blender/plane.py`). H the Sky Dart (`Tools/Blender/plane_dart.py`, `VehicleProfile.PlaneDart`, id "dart": the profile's
`PlaneTop / PlaneRoll / PlanePitch / PlaneBank / PlaneGain / PlaneSpan`, taken by `PlaneFlight.Begin`; `PlaneStands.PlaneOn`; AI pilots fly either). B control hints
for the device in use only (`Menus/KeyHints.cs` over `MenuInput.Controller`; the map, minimap, race HUD, debug line, loading tips, plane / heli help). C the race
HUD arrow and the air arrow damped. D Kennesaw's B2 past the race's departure below the summit; a race barrier on every fork off every course's line
(`python Tools/race/fork_closures.py`: closures `"fork": true`, ids F1 ...; re-run after any route script; report `Tools/race/fork_audit.py`). E the quarry decks'
cut from where the ground first reaches them, LANDING boards (`make_quarry_signs.py`), a preview camera over the line during the countdown
(`QuarryJumps.Preview`); the Quarry Track tried on ONE lane and NOT built again (`make_quarry_track.py`; output in `local/round74/quarry_track_unbuilt/`). F the summit leap's lip
flush at 27 degrees, then Round 73's climb and lip height pitched to 24 degrees, the kicker 450 m round and 18 m wide, `BodyGlide` over it (now merging every set of rectangles) (`make_summit_leap.py` ANG, R_KICK); the clouds rise over high
ground (`SkyClouds.Raise`: the white shape at the lip was the cloud layer). I the Highway Air Race (`python Tools/mapgen/make_highway_air_route.py [--plot]
[--room <dir>/71_line_room.tsv]`: make_air_route.py with its own course; the probe and flights `-exeRound71 -r71Route highway_air_race.json`; the third crash before one ring puts a plane past it, `RaceController.AirSpot`). J the airport and
parking lots as pavement on the map (`MapBuilder.MapPicture.cs`). G the AI's pace (see TODO.md). Check: `ChastainRush.exe -exeRound74 <dir> [-r74Only
dan,clouds,plane,preplay,controls,arrow,map,qpreview,forks] -logFile <dir>\Player.log` (`Checks/Round20Runner.R74.cs`); Round 73's parts on the new data with
`-r73Lines local/round74/r74_lines.json`; Round 70's `aitrace` now takes every course (`-r70Races`). Imported from Racer: nothing.
**Round 73 (see `TODO.md`, status there) is built and checked in the exe through the game's own input, not yet driven by Dan:** Dan's six reports of session
`2026-10-26_18-37-35-665_130b58`. A1 no race barrier on any course's line (`RaceBarriers`: a closure the line passes through is not built, blocks within 6 m of it
trimmed; `python Tools/race/closure_audit.py`, report only); A2 J1 on its own bench off Kennesaw Mountain Drive (`make_mountain_route.py` OFF_M / K, the drive kept off by
the grade: `RidgeGrade.keep_off` / `keep_off_fade` now in `GradeAlong`); A3 the summit leap (`python Tools/mapgen/make_summit_leap.py [--plot]` writes
`Data/tracks/kennesaw_summit_leap.json`, replaces Round 70's steel ramp; built by `MapBuilder.SummitRamp.cs`; then `add_extension_places.py`). B the Quarry Air Race redrawn
(`make_air_route.py` CTRL), rings either way (`RaceGate.EitherWay`), RING MISSED and the guide on (`RaceEntrant.RingSkipped`, `RaceController.AirAim / AirAfter`), the 3D arrow
`Race/AirGuideArrow.cs`, beacons, the minimap's next rings. C the quarry jumps rebuilt (`make_quarry_jumps.py`: lines, smooth run-up lanes, landing decks over the bank face
on skirted trestles, windows from the exe's measured speeds; signs `python Tools/handmade/make_quarry_signs.py`, hand-made, committed), seated people in the bleachers
(`SeatedPerson`), the quarry jumps drive, a Free Roam reset never back onto a stuck spot (`VehicleRespawn.SafeHistory`, `CarRecovery`, `RoadLookup.NearestPointWithin`),
the Quarry Track NOT built (`make_quarry_track.py` kept, its output moved to `local/round73/quarry_track_unbuilt/`: its lane crossing let racers skip The
Corner and got cars stuck; the divider code is inert without the file), the AI's recovery takes a route's `reset_skip`. Check: `ChastainRush.exe -exeRound73 <dir> [-r73Only
dan,bars,kdrive,leap,qroad,qjump,qcrowd,qreset,qtrack,airprobe,airpilot,allraces] -logFile <dir>\Player.log` (`Checks/Round20Runner.R73.cs`: a human-like driver and pilot
on a virtual controller, pictures from the player's camera). Imported from Racer: nothing.
**Round 72 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Viking Quarry jumps (Free Roam events; `python Tools/mapgen/make_quarry_jumps.py
[--plot]` writes `Data/tracks/quarry_jumps.json` (three jumps designed from the flights: kicker, landing deck, speed window, gantry, bleachers; grades / clear lines / catch zones merged into the
mountain's planning by `MapBuilder.QuarryJumps.cs`, built in the MountainRace step; re-run after mapgen, then `add_extension_places.py`, then a full Build Map); `SteelRamp` in
`MapBuilder.SummitRamp.cs` builds both ramps; event `Stunts/QuarryJumps.cs` (gantry start, speed check, scoring, Top 10 `quarry-jumps-v1.json`, replay via `ReplayViewer.ExtraTv`).
B3 junction blurs `Data/tracks/round72_junctions.json` (`MapBuilder.Round72.cs`, after the levelling); B2 Canton Road closures in `MapBuilder.Edges.cs`. Check: `ChastainRush.exe -exeRound72 <dir>
[-r72Only qjump,qshort,qreplay,qpics] -logFile <dir>\Player.log` (`Checks/Round20Runner.R72.cs`). Imported from Racer: nothing.
**Round 71 (see `TODO.md`, status there) is built and checked in the exe by script, not yet flown by Dan:** the Quarry Air Race (`python Tools/mapgen/make_air_route.py
[--plot] [--room <check dir>/71_line_room.tsv]` writes `Data/routes/quarry_air_race.json` (route `air`: the 3D line `loop` + `loop_y`, checkpoints are rings: `y`, `ring_m` or
`ring_w` / `ring_h`; `gate_y`, `gate_ring_air_m`; `vehicles` ["plane"]) and `Data/tracks/quarry_air_race.json` (its low lanes' `clear_lines`, merged into the mountain's by
`MapBuilder.SummitRamp.cs` MergeAirLanes: a full build). Rings and their flash / chime `RaceMarkers.MakeRing` / `RingPassed`; the race's air parts `Race/RaceController.Air.cs`
(runway grid, AI pilots, `AirReset`: back in the air 150 m before the last ring); AI pilots `Race/PlanePilot.cs` (the plane's own controls); `RaceGate.Ring/RingW/RingH`;
race planes pass through each other; `PlaneFlight.AirStart`. A quick build now takes in new route files too. Check: `ChastainRush.exe -exeRound71 <dir> [-r71Only
aprobe,aline,amiss,acrash,aai,atrial,apics] [-routeDir <dir>] [-planeNoTrees] -logFile <dir>\Player.log` (`Checks/Round20Runner.R71.cs`; `-routeDir`: route files from a folder
over the embedded ones; aprobe writes `71_line_room.tsv`). Imported from Racer: nothing.
**Round 70 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the summit ramp (Free Roam world piece: `python Tools/mapgen/make_summit_ramp.py
[--plot]` writes `Data/tracks/kennesaw_summit_ramp.json`, re-run after mapgen, then `add_extension_places.py`, then Build Map; merged into the mountain's pieces by `MapBuilder.Mountain.cs`, built by
`MapBuilder.SummitRamp.cs` in the MountainRace step: the drive's south-flank straight as the run-up, a steel deck "sc summit ramp deck" (held, ShortcutSurface) off the hairpin's outside at
296 degrees, the catch at the foot, a gravel arrester (ShrubBeds), race-time barriers (RaceOnly)). B the AI's racing line (`RaceRoad.BuildLine`, built by `RaceController.BuildAiLine` once the
barriers stand; `-r70LineOff` drives as before): the lane and the open shoulder probed, bends planned from the line, held zones within its bend limit, standing things off the line ignored,
Easy / Normal by pace; `RoadDriver.Why` (what set its target speed). Tools: `python Tools/race/replay_ai_table.py <replays>` (the AI against the player from race replays),
`ai_trace_why.py <r70_trace_*.tsv>`, `zone_speeds.py <replay> <route>`. C race setup's garage after a finished race; D `vehicle.json` keeps "roam" and "race" (and "color"), race setup puts on
`VehicleSwitch.RaceVehicle`; checks use `-vehicleSave <file>`. E park visitors (`AmbientLife.InPark`, ParkOut / ParkInRace, the park's lots walked). F1 hydrants only with a building within 60 m;
F2 J1's bench as wide as the drive beside it; F3 the gantry's posts on their own ground. Check: `ChastainRush.exe -exeRound70 <dir> [-r70Only hyd,roll,garage,sramp,park,gantry | aitrace
[-r70Races kennesaw,trail,glen,lakes] [-r70Diff 2] [-r70Laps 2] | vsave1|vsave2|vsave3 -vehicleSave <file>] -logFile <dir>\Player.log` (`Checks/Round20Runner.R70.cs`). Imported from Racer: nothing.
**Round 69 (see `TODO.md`, status there) is built and checked in the exe by script, not yet flown by Dan:** the plane, "Sky Hopper" (`Tools/Blender/plane.py`, profile
`VehicleProfile.Plane`, Class "Plane": Flies covers it); `Vehicle/PlaneFlight.cs` (+ `.Crash.cs`, `.Effects.cs`): `ArcadeVehicle.Flown` always on, on its wheels it calls
`ArcadeVehicle.Simulate` with its own controls, in the air arcade flight (`Sink` below 36 m/s); crashes reset onto a take-off strip (`PlaneFlight.StripSpot`, also R / Y);
`ArcadeVehicle.OwnSound` mutes VehicleAudio. Parked planes `Vehicle/PlaneStands.cs` and `Vehicle/AirportCircuit.cs`, both made at run time. Check: `ChastainRush.exe -exeRound69 <dir>
[-r69Only pstand,ptake,plevel,pcrash,pedge,pfly,pfps,ppics] -logFile <dir>\Player.log` (`Checks/Round20Runner.R69.cs`; a scripted pilot, `PlaneFlight.Scripted`). Imported from Racer: nothing.
**Round 68 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Kennesaw Mountain rebuilt for really big air
(`make_mountain_route.py` rewritten: the climb unchanged, then three straight lines cut down the mountain, each built round one jump designed from its flight (a
speed / flight / catch-slope designer in the script; `local/round68/design.json`): J1 the summit flight, J2 the west face, J3 the earthworks, 4 to 6 s). Kicker
curves are limited by the suspension (over about 6 m/s2 at lip speed the body meets the ramp); 75 degree lip edges. Grade fields `blends` (per point), `ground_first` /
`ground_last` (the drive's built height), `open_ends`. Catch slopes (`LandingZone.catchSlope`): `RampHold` turns a vehicle to the slope on touching down,
`VehicleSurfaceContacts.CatchRects` keeps the slope's normal there. Check: `ChastainRush.exe -exeRound68 <dir> [-r68Only jprof,j3test,jfly,jloop,jai,jhood]
[-r68Quick] -logFile <dir>\Player.log` (`Checks/Round20Runner.R68.cs`; `68_trace_*.tsv` per vehicle, `68_built_profile.tsv`). Imported from Racer: nothing.
**Round 67 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the Kennesaw Mountain race (`python Tools/mapgen/make_mountain_route.py
[--plot]` writes `Data/routes/kennesaw_mountain.json` and `Data/tracks/kennesaw_mountain.json`; re-run after mapgen, then Build Map; up Kennesaw Mountain Drive, down the Kennesaw Mountain
Trail on a graded bench with six jumps shaped into its profile; `MapBuilder.Mountain.cs`, quick step MountainRace; trees cleared / breakable in the Trees step: a full build; park visitors:
the park's trails as walkers' trail lines in `MapBuilder.Life.cs`; its fast-travel start added by `add_extension_places.py`). B the Noonday Creek Trail's "Creek leap" (forward) and
"Parkway leap" (reverse) and the mound's face (`make_trail_route.py`; the track file's `big_jumps`, `landing_zones`, `shrub_beds`, `skid_marks` built by `MapBuilder.TrailRace.cs`;
BigJumps look `riprap`). A shortcut that forgives no checkpoint is dropped by `RaceRoute` (unless a lap option): give it one. ShrubBeds act on `ArcadeVehicle.Live` (enabled
vehicles): a scripted check car (DriveLine disables it) needs the drag applied by the check (`R67BedDrag`). Check: `ChastainRush.exe -exeRound67 <dir> [-r67Only
mloop,mjumps,mwalk,mpics,tcreek,tpark,tmound,tpics | mai] -logFile <dir>\Player.log` (`Checks/Round20Runner.R67.cs`). Imported from Racer: nothing.
**Round 66 (see `TODO.md`, status there) is built and checked in the exe by script, not yet seen by Dan:** the map's edges. Scenery past every edge and the horizon
(`python Tools/mapgen/make_scenery.py [--refresh]` writes `Assets/Resources/Scenery/edge_scenery.bytes` from USGS 3DEP heights and NLCD 2021 land cover, downloads in
`raw/scenery/`; re-run after a mapgen run that moves the edge; no Build Map needed: `World/EdgeScenery.cs` makes it at run time, shader `ChastainRush/Horizon`, layer 8
Horizon; `-sceneryOff` leaves it out). FarView opens the view by height over the land's low point too (`FarView.Lift`: the summit) and opens the far clip to 90 km for
the Horizon layer only (`layerCullDistances`; `FarView.WorldFar` is the world's own far clip). Road closures at every road end on the edge (`MapBuilder.Edges.cs`, step
Edges; `RoadClosures` in the scene; signs `python Tools/handmade/make_closure_signs.py`, hand-made, committed); the traffic and police networks end short of them
(`EdgeTrimmed`), two-way ends turn traffic round (`TrafficNetwork.TurnRound`), divided highways get turnarounds. `World/MapEdge.cs`: push-back within 36 m, a wall on
the edge, put back if past it. The helicopter: `HelicopterFlight.Boundary.cs` (warning, headwind, nose swung back). Check: `ChastainRush.exe -exeRound66 <dir>
[-r66Only spics,sfps,hedge,eoff,edrive | etraffic -traffic Heavy] [-sceneryOff] -logFile <dir>\Player.log` (`Checks/Round20Runner.R66.cs`). Imported from Racer: nothing.
**Round 65 (see `TODO.md`, status there) is done (editor tools; the world unchanged):** generated meshes packed as sub-assets (full Build Map 44 -> 15 min, the exe 14 -> 4);
quick builds take planning changes to the ground; Shortcuts, CreekTrack, LakesRace quick steps; the levelling's terrain edit by cell and in parallel (`EditTerrainCells`).
Proof a build is unchanged: `SceneDump` before and after, `Tools/scene_diff.py`. Imported from Racer: nothing.
**Round 64 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Noonday Creek Trail put right: forward and reverse swapped
(`make_trail_route.py` writes geometry B as "forward"; Dan's saved trail times / Top 10s / ghosts / replays swapped once by `Race/NoondayTrailSwap.cs`, marker file beside the records);
the course on Ring Road NW and through Halverson Home (the north anchor) and the north wing, the east escalator rounded over again (Round 62's raffle stage lip gone);
the "Carroway renovation" shortcut each way through the main doors, the center court and Carroway (the south anchor) with its flooded renovation pit (`MapBuilder.MallShortcut.cs`;
the anchors driven through: `MallHollow`, `MallHollowShell`, `MallHollowFronts` in `MapBuilder.Mall.cs`); see-through door glass (`MallSeeThrough`, MallBreak.doors), the door frames
opened (`BoxFHole`), door aprons graded from inside the threshold; the race's grilles (route `seals`, any route: RaceBarriers) at the north wing and the other doors; breakable trail
railings (`BreakableRail`, `RestoreWhenClear`); trail walkers and cyclists (`AmbientLife.streetTrail`, WalkKind.Cyclist); the mound by Town Center Drive (track grade); the levelling
widest first, narrower roads fading before a wider one (`R62LevelMains` DoneW); Barrett at the I-575 ramps one curve per carriageway (no round ends). Check: `ChastainRush.exe
-exeRound64 <dir> [-r64Only xloop,xshort,xdoors,xturn,xmound,xwalk,xbar,xpics | xai -traffic Normal] [-r20Minutes 150] -logFile <dir>\Player.log` (`Checks/Round20Runner.R64.cs`). Imported from Racer: nothing.
**Round 63 (see `TODO.md`, status there) is built and checked in the exe by script, not yet flown by Dan:** the helicopter, "Sky Survey" (profile `heli`,
`VehicleProfile.Flies`: Free Roam only; model `Tools/Blender/helicopter.py`, groups Rotor / TailRotor / Disc on pivots in `VehicleVisual`); flown by `Vehicle/HelicopterFlight.cs`
(+ `.Crash.cs`: crashes, `ResetSpot`, `TreeCrowns` (crowns from TreeField's data); + `.Effects.cs`: rotors, sound, downwash, readout) on the player's own vehicle object
(`ArcadeVehicle.Flown` stops the drive); helipads `MapBuilder.Helipads.cs` (step Helipads, after Airport) and `Vehicle/HelipadStands.cs` (parked helicopters, A / H swap,
fast-travel places, `Exploration.Find`); the view from the air `World/FarView.cs` (fog and far clip open with the camera's height, detail cells past 900 m by CullingGroup,
`TreeField.FarDraw` / thinned far trees / 1 km blocks, `FarTerrain` coarser past 2 km); `CameraViews.View.Down`. Check: `ChastainRush.exe -exeRound63 <dir> [-r63Only
hover,hfly,hpads,hcrash,hfps,hpics,hfpsx] -logFile <dir>\Player.log` (`Checks/Round20Runner.R63.cs`; `hfpsx`, only when named: what the far view costs). Imported from Racer: nothing.
**Round 62 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Noonday Creek Trail race on the real trail round Town Center
(`python Tools/mapgen/make_trail_route.py [--plot [x0,x1,z0,z1]]` writes `Data/routes/noonday_creek_trail.json` and `Data/tracks/noonday_creek_trail.json` from map.json and
raw/osm_ext1.json's trail ways; re-run after mapgen, then Build Map): Bells Ferry, Barrett, Mall Boulevard, Town Center Drive, the lot, through Town Center (in Halverson's east
doors, out the main doors; reverse the other way), the north lot, the trail under I-575 to the trailhead; its own reverse lap. Route heights of its own (`loop_y` / `loop_rev_y`:
codes the build writes in when it embeds the route, `MapBuilder.TrailRace.cs` EmbedRouteHeights: the mall's floors, the east escalator, the red bridge's deck, road decks;
`RaceRoute.OwnY` / `OwnSurface`), the AI's narrower stretches (route `ai_widths`, `RaceRoad.halfZones`). World pieces: `MapBuilder.TrailRace.cs` (the path, yellow dashes,
railings under I-575, the red truss footbridge, winding-road signs; the bench graded in the planning, `RidgeGrade.halves`), `MapBuilder.MallRace.cs` (the raffle stage at the
east escalator's top, the mattress mountain in Halverson's aisle; the escalator rises to the stage's lip, Halverson's front 6.2 m, the balcony and the tower's glass break);
signs `python Tools/handmade/make_trail_signs.py` (hand-made, committed). The made-up woods track is "Noonday Creek Extension" (route `records_as` "Noonday Creek": its records,
ghosts and replays kept). C: every main road of the extension levelled (`MapBuilder.Round62.cs` R62LevelMains, in place of Round 61's five), I-575's decks over Big Shanty
Road laid with their roads as one curve (R62SeamOverpasses); levelled narrowest first so the wider road wins where two overlap. Every mesh made by `MeshBuilder.ToObject` is saved under its object's
name: two objects of one name overwrite each other's mesh (the trail's decks once took the map's "BridgeDecks"; every road bridge lost its deck). Check: `ChastainRush.exe -exeRound62 <dir> [-r62Only tloop,tai,tbar,tpics] [-traffic Normal] -logFile
<dir>\Player.log` (`Checks/Round20Runner.R62.cs`); the jolt scan: `python Tools/mapgen/jolt_shots.py` (writes `local/round62/jolt_shots.json`), `-exeShots local/round62/jolt_shots.json <dir> -vehicle drifter`,
`python Tools/mapgen/jolt_table.py <before> <after>`, `python Tools/mapgen/jolt_spots.py <dir>` (Dan's two spots); `ChastainRush.DeckProbe.Run -probeOut <file> [-probeAt "x,z;..."]` (Editor batch: the scene's bridge decks, rays down). Imported from Racer: nothing.
**Round 61 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the map extension, stage 2 (config `extension2`:
west to -84.662 and south to 33.956, Kennesaw Mountain, Cobb County International Airport; 12613 x 10229 m, 19 x 16 tiles, x -8629.83 to 3983, z -8311.33 to 1918; every
existing list unchanged as a prefix), the airport and the mountain's trails (`MapBuilder.Airport.cs`, map.json `aeroways` / `trails`), fast travel to the summit and the visitor
center; C the frame rate kept (`World/FarTerrain.cs`, `GroundQuery.TileAt`, `ArcadeVehicle.Live`, cells for shrub beds / water / lamps / people, `RoadNet` largest part counted
once); D the new land's main roads levelled (`MapBuilder.Round61.cs`). Full Build Map now about 5.3 h (Businesses and Hydrants the most). Check: `-exeRound59 <dir> -r59Only
load,fps,drive,pics59[,fps61x] -r59Drives local/round61/r61_places.json` (`ext_drives.py --round61`). Imported from Racer: nothing.
**Round 60 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A Town Center at Cobb from its six OSM footprints
(`MapBuilder.Mall.cs` plan, frame (14.75 deg), doors, outside, anchors, skylights, sign atlas `python Tools/handmade/make_mall_signs.py` (made-up names, hand-made, committed);
`MapBuilder.MallInside.cs` floors, escalator ramps, railings, columns, 104 shops, smashables (all `FenceChunk.free`), food court, play area, the east anchor's store;
`MapBuilder.MallLots.cs` the lots OSM lacks; quick step `Mall`; run-time `World/TownCenterMall.cs`: the inside drawn within 140 m, breakables back past 220 m, night lights);
the shoppers via `BuildLife`; "Mall ramp ..." meshes and "Mall floor" boxes in `VehicleSurfaceContacts`; Dan's photos `local/reference_photos/town_center/` (reference only).
B the I-75 overpass's ends (`MapBuilder.Round60.cs` `R60SmoothOverpass`, after SmoothBridgeEnds). Check: `ChastainRush.exe -exeRound60 <dir> [-r60Only mallin,smash,mallfps,overpass,pics60]
-logFile <dir>\Player.log` (`Checks/Round20Runner.R60.cs`); `ChastainRush.MallProbe.Run -probeOut <file>` (Editor batch: the inside's vertices outside the walls). Imported from Racer: nothing.
**Round 59 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the map extension, stage 1 (Town Center at Cobb, KSU):
the new land at negative x / z appended after the core (`Tools/mapgen/mapgen_extend.py`, config `extension`; the core's data unchanged byte for byte), `terrain_full.r16` for Unity
(`MapBuilder` TerrainGrid / tiles take its origin; `MapExtent(map)` for the map picture, life grid and bridge ends), the exploration fog renumbered onto the larger grid
(`Exploration.Data` ox / oz / cols), destinations `add_extension_places.py`. See TODO.md's Technical notes ("The map extension"). The map is 6638 x 3836 m, x -2655.33 to 3983,
z -1918 to 1918; Build Map about 33 min. Imported from Racer: nothing.
**Round 58 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the AI with agency (`RoadDriver` AgencyStep:
down -> put back at once, blocked -> back up and steer round then put back by 4 s, the reversing pedal bug, wrong way -> full lock round; PassLane: either side, shoulder, grass;
Hard's pace; `-r58AgencyOff` drives as before); the hub's pit railed on every edge the race passes and the span's sides (`make_underground_route.py` R3 to R7, route rails `ys`);
aquarium weeds / rocks / logs / fish kept out of every tube (`ClearFrame.Others` for every network glass stretch, `AvoidBed` only NetGlassAvoid's; `PipeAquarium.avoid*`);
Chastain Ridge Drive's valley one vertical curve and its loop-branch junction blurred (the Ridge track's `vertical_curves` / `smooth_spots`, read by `R55SmoothSpots`).
Check: `ChastainRush.exe -exeRound58 <dir> [-r58Only aitable,airecover,hubpit,plants,ridgebump] [-r58Races glen,creek|reverse,...] [-r58AgencyOff] -traffic Normal -logFile
<dir>\Player.log` (`Checks/Round20Runner.R58.cs`; aitable measures each race from its replay). Imported from Racer: nothing.
**Round 57 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the Embry Lane / Suholden Circle
corner one vertical curve (track `vertical_curves` / `smooth_spots`, `make_addison_route.py`); B COMPLETE RACE has its own input action (the menus' confirm
action is off while the menus are closed: the UI module disables the actions it enabled); C S2's glass bed kept out of M5c (`MapBuilder.ClearPipe.cs`
`ClearFrame.Others`, `MapBuilder.Network.cs` `NetGlassAvoid`; B28's bed in M2a too, at Dan's yes; free-roam check `-r57Only m2a`), the AI's recovery goes 20 m+ on when stuck
again where it last stuck (`RoadDriver`), the pipe camera never past a wall (`DrainPipe.View`); D the Underground's rails and switch gates solid to 3 m
(`RaceBarriers.Underground.cs`). F race replays (new, not in Racer): `Assets/Scripts/Race/Replay/` (`RaceReplay.cs` ReplayData and the files,
`ReplayRecorder` on the RaceController, `ReplayViewer`), `Menus/GameMenus.Replays.cs` (WATCH / SAVE REPLAY on the finish screen, X / V and Y / K; the
REPLAYS page); `Replays/best/` and `Replays/saved/` beside the race records; puppets via `ArcadeVehicle.Puppet`; world events through `FenceChunk.ReplayBreak`
/ `ReplayRestore`, `TreeTrunks.SnappedTree` / `ReplaySnap`, `ManholePlume.Erupted`, `RaceBarriers.GateOpened`. Check: `ChastainRush.exe -exeRound57 <dir>
[-r57Only corner,complete,m5c,m5cai,hub,cpics57] [-r57Only replaybest | replay -traffic Normal] [-r57Vehicles ids] -logFile <dir>\Player.log`
(`Checks/Round20Runner.R57*.cs`). Imported from Racer: nothing.
**Round 56 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Addison Heights' Elaine jump off a shaped
lip (the bend's crest raised 1 m on Elaine's own line, track grade "A1 the lip") onto the lower bank, over the Crawford / Hayworth corner and down Crawford's
west leg (BUG-004), back through the yards round 1143 onto Hayworth (grade and smooth line "A1 the way back"); the bench gone; the upper bank's brush twice as
strong (`ShrubBeds.strength`, track `shrub_beds[].strength`); Crawford's west-leg barrier past the way back. C (BUG-002) a body's corner dipped into Elaine's
climb at speed: there the road does not push on an upright body (`Vehicle/BodyGlide.cs`, `VehicleSurfaceContacts.Glide`, track `body_glide`). B the barricade across Elaine
(`MapBuilder.Round56.cs`, track `elaine_mouth`; `FenceChunk.free`: no speed lost; `Neighborhood/RestoreWhenClear.cs`); C Elaine's valley one vertical curve;
D the Joel terrace's flower pots gone. Check: `ChastainRush.exe -exeRound56 <dir> [-r56Only eprobe56,ejump,ebarricade,ejerk,etrace,erace,epics] [-traffic Normal]
-logFile <dir>\Player.log` (`Checks/Round20Runner.R56.cs`). A builder's parent object stays at the origin when its meshes are in world coordinates.
Imported from Racer: nothing.
**Round 55 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Addison Heights: Round 54's Elaine valley way
taken out; the Elaine jump off the bend built as Dan's option 2 (hedges along the bank's foot, a graded bench north past 960 to Hayworth; track `worn_tracks`,
`BuildAddison55`; Elaine barred north of the bend); the line
moved to Embry Lane's east end (`make_addison_route.py` GATE_AT, records_as "Addison Heights (Embry Lane line)"); the bounce house's voice at the castle with the
trampoline's sound; Dan's two bumps (`MapBuilder.Round55.cs`: track `smooth_spots`, a blur; `vertical_curves`); the AI goes round a slower car instead of sitting on
its bumper (`RoadDriver` Passes, BehindSeconds, PassFix). Check: `ChastainRush.exe -exeRound55 <dir> [-r55Only eprobe,etimes,line,bounce,erace,epics] [-r55PassOff]
[-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R55.cs`). Imported from Racer: nothing.
**Round 54 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Addison Heights (`make_addison_route.py`;
`MapBuilder.Round54.cs`, built in the step Addison): the line on Crawford Circle's east leg (route `records_as`: the old times kept apart), the Elaine jump (forward,
its way graded by `GradeRidge(AddisonNow)` in `Round53Plan`), the Joel terrace (reverse, prism `sc A2 terrace`), the bounce house (reverse, `Neighborhood/BounceHouse.cs`,
an aimed throw as the trampoline), house 615's carport, the lap's roads levelled across their width (`R54LevelLap`, track `level_lines`), sharp-curve signs (`python
Tools/handmade/make_curve_sign.py`, hand-made, committed), AI limits per route (`ai_hill_targets`, `ai_crest_g`), the AI passing traffic, breakable trees beside the lap
(track `break_lines`), Lyndon's hedges. B a line missed within 40 m counts the lap with +5 s (`RaceController.LineMissedWide`); C Racer's COMPLETE RACE
(`Menus/GameMenus.Complete.cs`, `RaceController.CompleteRace`); D1 traffic replans and turns round at barriers (`TrafficDriver.Replan / TurnRound`); D2 one material
per party sign face; D3 R3's slide rails; E1 manhole floors refitted after every grading step (step ManholeFloors) and in `VehicleSurfaceContacts`; E2 `ManholeLid`
turns a vehicle along the riser as it clears the street. Tools: `ChastainRush.GroundGrid.Run -gridRects "name:x0,z0,x1,z1,step;..." | -gridLines <file> -gridOut <dir>`
(the built ground), `ChastainRush.MaterialScan.Run -scanOut <file>` (empty or broken material slots: what draws magenta). Check: `ChastainRush.exe -exeRound54 <dir>
[-r54Only linemiss,complete,elaine,terrace,bounce,mh14,a54pics,barriers,aihard,aiscale] [-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R54.cs`).
Imported from Racer: COMPLETE RACE (RaceMenus' button and FinalizeUnfinishedAi, rewritten into GameMenus / RaceController).
**Round 53 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** Addison Heights' forward shortcut up Lyndon
Drive (`make_addison_route.py` f_lyndon_crew; Lyndon's two barriers open forward only): the road crew (`MapBuilder.Round53.cs`, step Addison: the tilt trailer and
planks, a real jump; truck, roller, mixer, cones; `Neighborhood/Flagger.cs` with the paddle from `python Tools/handmade/make_stop_paddle.py`, hand-made, committed;
`Neighborhood/WetConcrete.cs`, the fresh slab: drag, ruts; shrub beds), Lyndon's crew stretch out of the traffic and police graphs (`R53Pieces`); house 1089's carport
(BuildH, `R53Carports`); the Lyndon / Embry junction evened (`R53EvenJunction`). The Elaine jump (B) is not built: the finish on Kelly Drive leaves it no way back
that saves time (options in TODO.md). Check: `ChastainRush.exe -exeRound53 <dir> [-r53Only sclean,scheat,sshort,sflag,sjunction,ahshut,spics,srace,srevrace]
[-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R53.cs`). Imported from Racer: nothing.
**Round 52 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Addison Heights race (forward and reverse, no
shortcuts): `python Tools/mapgen/make_addison_route.py [--plot]` writes `Data/routes/addison_heights.json` and `Data/tracks/addison_heights.json` (a cleared line: no trunk
at the road's edge; read by `MapBuilder.Round52.cs` as `AddisonNow`); re-run after mapgen, then Build Map. Houses on its steep lots settled (`MapBuilder.Round52.cs`:
Dan's 910, 996, 615, 1135 and the houses facing the lap: floor at a drivable drive, the drive graded, the lot cut gently, a stoop and steps; style E's back windows on its
block); list `Data/screenshots/round52/houses_settled.txt`. Check: `ChastainRush.exe -exeRound52 <dir> [-r52Only ahlap,ahshut,ahrace,ahpics,ahhouses] [-traffic Normal]
[-r20Minutes 100] -logFile <dir>\Player.log` (`Checks/Round20Runner.R52.cs`). `RaceRoute.RunInLength` assumes the lead-in ends at the loop's start (the Glen loop's):
for a route whose lead-in ends at the gate use `lead_in_length_m`. Imported from Racer: nothing.
**Round 51 (see `TODO.md`, status there) is built and checked in the exe by script, not yet seen by Dan:** startup polish from Dan's assets
(`SourceArt/Title/`): Racer's StartupTitle and LoadingScreen (`Assets/Scripts/Title/`; cover, voice and theme in `Assets/Resources/Title/`, import settings
`Assets/Editor/TitleArtImport.cs`), the loading tips, the exe icon (`Assets/HandMade/Title/GameIcon.png`, set by PlayerBuild). Both never in a check run except
`ChastainRush.exe -exeRound51 <dir> [-r51Press key|pad] [-r51Only listen,roam,race,tips,pics] -logFile <dir>\Player.log` (`Checks/Round51Runner.cs`). Under the
paused title menu the audio clock (dspTime) stands still: start title sounds with Play, never PlayScheduled. IMGUI draws over every canvas: an IMGUI HUD
hides while `TrailerMode.HudHidden` (now also under the title and the loading screen). Imported from Racer: StartupTitle, LoadingScreen.
**Round 50 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the reverse kicker line's second jump, a
collapsed carport at the end of Northlake Point, and shrub beds across every free way (`MapBuilder.LakesKicker.cs`; `make_lakes_route.py` C_*, SHRUB_BEDS;
runtime `Neighborhood/ShrubBeds.cs`, Racer's ShortcutUndergrowth); the lake trail at its real width on a level bench (`GradeAlong` blend_left / blend_r);
no JUMP sign. Check: `ChastainRush.exe -exeRound50 <dir> [-r50Only kclean,kcheat,kshort,krace,ktrail,kpics] [-traffic Normal] -logFile <dir>\Player.log`
(`Checks/Round20Runner.R50.cs`; kcheat drives the shortcut's ground without its jumps). Imported from Racer: ShortcutUndergrowth (as ShrubBeds).
**Round 49 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** reverse Chastain Lakes shortcuts and the
lake trail moved (`make_lakes_route.py`: the trail behind the pool to a footbridge over the spillway, `TRAIL_CTRL` / `BRIDGE_S`, `CONCRETE_TO`; the reverse
section writes `r_lake_intake` and `r_kicker_jump`, both shut in a forward race by two barriers, `fwd_shut`). A the intake: `pipe_network.py` LI (roof open),
B29 (glass under West Chastain Lake, under M2b), MH15; `MapBuilder.LakesIntake.cs` (jetty, square riser, `LakeDrain` with `shutForwardOf`, `curtain` off; sign
`python Tools/handmade/make_intake_sign.py`, hand-made, committed). B the kids' kicker (`MapBuilder.LakesKicker.cs`, a real jump, landing zone RK).
`RaceOnly.hideInForwardOf` / `showInForwardOf`. D the map's pointer (`WorldMap.cs`: left stick pans and carries the pointer to the edges, right stick / mouse
is the pointer, A / click chooses under it, X / Y act at it; center on the D-pad's left, shortcuts and pipes on its right). Check: `ChastainRush.exe -exeRound49
<dir> [-r49Only lktrail,lkintake,lkkicker,lkshut,lkfwd,lkrace,lkmap,lkpics] [-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R49.cs`).
The AI's recovery never puts a car under a structure (`RoadDriver.Covered`: the lap's height on a bridge is the ground under it) and looks on along
the lap after four failed tries. Imported from Racer: nothing.
**Round 48 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the goose egg hunt and exploration (Racer's acorn
hunt): `python Tools/mapgen/make_eggs.py [--plot]` writes `Data/explore/eggs.json` (100 eggs, 11 areas, tiers, 35 places; re-run after mapgen or a course / network change),
built by `MapBuilder.Eggs.cs` (game layer; eggs alone into the built scene: `-executeMethod ChastainRush.MapBuilder.RebuildEggsFromCommandLine`); runtime `Assets/Scripts/Explore/`
(EggHunt, EggAreas, EggBanner, Exploration, ExplorationPlaces, WaypointGuide), the map's part `Map/WorldMap.Explore.cs`, the pause menu's GOOSE EGGS page
`Menus/GameMenus.Eggs.cs`. Saves `goose-eggs-v1.json`, `exploration-map-v1.json` beside the race records. A MonoBehaviour must be in a file of its own name (one inside
another's file broke the scene in the exe: "level0 is corrupted"). Check: `ChastainRush.exe -exeRound48 <dir> [-r48Only eggpickup,eggtier,eggmap,eggtravel,eggwaypoint,
eggrace,eggpics] -logFile <dir>\Player.log` (`Checks/Round20Runner.R48.cs`). Imported from Racer: ExplorationCollection, AcornAreas, AcornBanner, Synth, ExplorationMap's
fog / destinations / travel / waypoint, WaypointGuide (rewritten into the files above).
**Round 47 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** reverse Chastain Lakes (`make_lakes_route.py`
`loop_rev` and the *_rev fields, no reverse shortcuts; the forward shortcuts shut in a reverse race: two `closures_rev` barriers, `RaceOnly.showInReverseOf` the drain's
grate, `LakeDrain.shutReverseOf`); BUG-001's drives onto Chastain Road dropped (`MapBuilder.Round47.cs`). Check: `ChastainRush.exe -exeRound47 <dir> [-r47Only
lkrevlap,lkfwdlap,lkrevshut,lkrevrace,lkrevpics,drives] [-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R47.cs`). A check part's name must not be one
the runner's other rounds use at its top level (map, race, pics, revlap, revrace ...: they would run too). Imported from Racer: nothing.
**Round 45 (see `TODO.md`, status there) is built and checked in the exe by script, not yet heard by Dan:** the police radio in Dan's two
ElevenLabs voices (cop, helicopter pilot): `police-radio-lines.csv` column `speaker` (cop / pilot / cop+pilot, a pilot take `<id>_air.wav`),
`PoliceRadio` plays a call with a pilot line from the pilot's set only (rotor thump under it); the studio's Cop and Helicopter pilot boxes and
`python Tools/Audio/police_radio_studio.py --make-missing` (no window; the key from ELEVENLABS_API_KEY, never printed); 190 clips in
`SourceArt/Audio/PoliceRadio/`. Check: `ChastainRush.exe -exeRound45 <dir> -logFile <dir>\Player.log` (`Checks/Round20Runner.R45.cs`). Imported from Racer: nothing.
**Round 46 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Chastain Lakes boat jump
(`make_lakes_route.py` W, `lakes.boat` in the track file; `MapBuilder.LakesBoat.cs`: dock, pontoon party boat, solid slide chute, PARTY sign;
Round 44's spoil pile gone; route `CLEAR_OVERRIDE` puts Lakeside Point's barrier 45 m down it); glass under water for B28, B12, S2 and M3c's second
pond (`pipe_network.py` `glass`). Lessons: a shortcut's `ai_speed` caps the AI's whole run; a thin open board ("slide"/"deck" kinds) is not for a
fast compression (use a solid prism); no upright face where two driven surfaces meet flush (`ScPrism.no_face`); every prism its own name (collider
meshes are saved by name). Check: `ChastainRush.exe -exeRound46 <dir> [-r46Only speed,boattrace,boat,boattimes,boatrace,drain,pics] [-traffic Normal]
-logFile <dir>\Player.log` (`Checks/Round20Runner.R46.cs`; run `boat` without traffic). Imported from Racer: nothing.
**Round 44 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** the Chastain Lakes race (forward only, 2 laps,
traffic): `python Tools/mapgen/make_lakes_route.py [--plot [x0,x1,z0,z1]]` writes `Data/routes/chastain_lakes.json` and `Data/tracks/chastain_lakes.json` (re-run after
mapgen and after `pipe_network.py`; it reads network.json), built by `MapBuilder.LakesRace.cs` (the lake trail, the lake drain, the W pile; `LakesNow` is a `Ridge` loaded
from that file: its grades, cleared lines, landing zone) and `MapBuilder.ChastainLakes.cs` (footbridge, pier and gazebo sized from the file). The lake drain: LD, B28,
MH14 in `pipe_network.py` (a chamber's `roof_open`), runtime `Neighborhood/LakeDrain.cs`. A big jump with `"physics": true` gets no BigJump (a real jump). Check:
`ChastainRush.exe -exeRound44 <dir> [-r44Only times,wjump,drain,lakesrace,pics] [-traffic Normal] -logFile <dir>\Player.log` (`Checks/Round20Runner.R44.cs`).
Imported from Racer: nothing.
**Round 43 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the manhole plume on both
carriageways (a second cover RP2 in the southeast-bound one, `ManholePlume.group`), race resets near it short of the cover (route
`reset_skip_rev` `short_area` / `short_s`, `CarRecovery`) and a reset never charges the checkpoints it skips (`RaceController.ForgiveReset`);
B Racer's clean-lap ghost (`Race/CleanLapGhost.cs`, Race Setup's Ghost row, RECORDS > GHOST board); C Racer's winner shot (`Race/WinnerShot.cs`);
D Racer's Trailer Mode (`Trailer/`, `Vehicle/CameraViews.Trailer.cs`, `Menus/GameMenus.Trailer.cs`, `WorldLook.TrailerConditions`; IMGUI HUDs ask
`TrailerMode.HudHidden`; a race in it sets `RaceController.NoRecords`). Imported from Racer: CleanLapGhost, WinnerShot, TrailerMode (+Panel),
CameraViews.Trailer, RaceMenus.Trailer (into GameMenus), WorldLook's Trailer conditions; left behind: TrailerRecorder (Editor only), the waypoint beacon.
**Round 42 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A quick builds
(`MapBuilder.Quick.cs`, "Quick builds" under Commands; the full Build Map 32 -> 11 to 18 min, same scene; `SceneDump`, `Tools/scene_diff.py`);
B1 church 74's windows beside the doors break and lead to the stair (`MapBuilder.ChurchCrypt.cs` ChurchWindows, `Neighborhood/RestoreOnReset.cs`),
B2 the business lot no longer drawn through the church (`DrapePoly` cut), B3 the manhole plume never in Free Roam (`ManholePlume`). The ramp
physics material is only ever the stored asset (a full build had left the crypt's and house 1092's roof colliders without it). Check:
`ChastainRush.exe -exeRound42 <dir> [-r42Only windows,aisle,plume] -logFile <dir>\Player.log`. Imported from Racer: nothing.
**Round 41 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the church leap replaced by THE CRYPT
(reverse Chastain Ridge, shortcut `rch_crypt`: church 74's north wing and spine hollow, the stair, the vaulted hall, the catacomb, the exit stair out of the bank
into the graveyard, a real jump onto New Chastain Road; `MapBuilder.ChurchCrypt.cs`, its line and floor from `make_ridge_route.py` (track `crypt`, route
`path_y`); GuidedLaunch and ChurchTurn removed). B the crest cut over house 1092: its roof solid (its mesh its collider), a roofer's ramp on it
(`MapBuilder.Round41.cs`, track `roof_ramp`). C BF, X_W20, X_W11, X_BRIDGE, X_STREAM in `Retaining` (`MapBuilder.Pipes.cs`). Check: `ChastainRush.exe -exeRound41 <dir>
[-r41Only crypt,cryptin,cryptcrash,cryptpics,ridgerace,crest,crestpics,pipes] [-r20Vehicles ids] [-traffic Normal] -logFile <dir>\Player.log`
(`Checks/Round20Runner.R41*.cs`). `ChastainRush.MeshProbe.Run -meshRects "name:x0,z0,x1,z1,step;..." -probeOut <file>` (Editor batch): what is drawn
against what is solid over a rectangle of the built scene. Imported from Racer: nothing.
**Round 40 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A the reverse Chastain Ridge
(`make_ridge_route.py`): the manhole plume replaces Round 39's climb (`Neighborhood/ManholePlume.cs`, cover via `ManholeLid`; route `plumes` /
`plumes_rev` arm it; `Vehicle/GuidedFlight.cs`, an aimed flight along a Hermite curve from wherever the vehicle is); BUG-003's bump smoothed
(`SeamSpots` in `MapBuilder.Round17.cs`); S1 / S2 clues hidden in a reverse race (`RaceOnly.hideInReverseOf`, CtBigJump `clues_hidden_reverse_of`);
no reverse S1; the crest cut (boulder wall, house 1245 moved 1.6 m west: `HouseMoves` in `MapBuilder.Round40.cs`) and the church leap (church
74's north wing hollow: `MapBuilder.ChurchClimb.cs`; `Neighborhood/GuidedLaunch.cs`, `Neighborhood/ChurchTurn.cs` held bends). B the split
carriageways' paint rule (`RoadIndex.CoversPaint`), New Chastain Road's median and its trees (`MapBuilder.Round40.cs`), two hidden cruisers on
Canton Road (`HiddenPolice.CantonSpots`). C houses 802 and 594 raised (`RaisedBuildings`). Check: `ChastainRush.exe -exeRound40 <dir> [-r40Only
plume,plumewhen,plumepics,fwdleap,corner,clues,crest,church,churchpics,ridgerace,lines,median,cantoncop,houses] [-traffic Normal] [-hiddenPolice]
-logFile <dir>\Player.log` (`Checks/Round20Runner.R40*.cs`). Imported from Racer: nothing.
**Round 39 (see `TODO.md`, status there) is built and checked in the exe by script, not yet driven by Dan:** A Dan's spots: mapgen tilts the last
8 m of every road end that meets a road running on through onto it (`junction_stems`, config `road_flatten.junction_warp_m`; BUG-001 / BUG-005);
`pipe_network.py` drops a fillet that folds back (`dekink`); `MapBuilder.Pipes.cs` `Retaining` (X_SHOPS only): wingwalls hold kept ground, the
ground behind the headwall up to a house's base; the Courson Street pool and courts (`MapBuilder.CoursonSwim.cs`, `CoursonOwns`). B the people's mix
(`MapBuilder.Life.cs`: coffee 1 house in 40, talking, gardening, mailbox, kids) and street walkers (`World/Life/AmbientLife.Walkers.cs`: dog walkers,
families, couples, joggers on the baked streets; `LifeSchedule.Walkers`). C the reverse Chastain Ridge, a lap of its own (route `loop_rev`,
`gate_s_rev`, `checkpoints_rev`, `junctions_rev`, `closures_rev`, `reset_skip_rev`, `ai_zones_reverse`; `RaceRoute.OwnReverse`), THE RIDGE CLIMB
(`make_ridge_route.py`, jump id CLIMB, road-work dressing in `MapBuilder.ChastainRidge.cs`), reverse S1 Knoll drop. D Racer's police
(`Assets/Scripts/Police/`: GetawayChase, CopDriver, RoadNet + PoliceRoads (the map's roads and limits, `MapBuilder.Police.cs`), PoliceChase,
SpeedPatrol, HiddenPolice, PoliceHelicopter, PoliceLights, PoliceRadio + PoliceRadioScript + PoliceRadioVoice, PoliceKit, PoliceSession,
PoliceHud; `Menus/GameMenus.Police.cs`: POLICE on the title and Free Roam menus, results, RECORDS Getaway / Speed Patrol Top 10s
`getaway-v1.json`, `speed-patrol-v1.json`; settings `hiddenPolice`, `hiddenPoliceDifficulty`; `VehicleProfile.Police` / `PoliceBike` kept out of
All; `TrafficDriver.SpeedOverride`). The radio reads `Tools/Audio/police-radio-lines.csv` (embedded by Build Map); clips `<id>.wav` from the
drop-in `Sounds\PoliceRadio\` beside the records, then `ChastainRush_Data\PoliceRadio\` (the exe build copies `SourceArt/Audio/PoliceRadio/`);
none recorded: text only. Check: `ChastainRush.exe -exeRound39 <dir> [-r39Only walkers,walkpics,walkdodge,walkfps,climb,fwdleap,climbreset,ridgerev,
revpics,getaway,escape,patrol,hidden,chase2,radio,policefps] [-hiddenPolice] -traffic Normal -logFile <dir>\Player.log`
(`Checks/Round20Runner.R39*.cs`). Imported from Racer: GetawayChase, CopDriver, RoadNet, PoliceChase, SpeedPatrol (+PatrolContacts), HiddenPolice,
PoliceHelicopter, PoliceLights, PoliceRadio (+voice), the Patrol Car and Patrol Cycle (PatrolCar.fbx, PoliceBike.fbx, `Tools/Blender/police.py`,
`policebike.py`, with `cars.py`, `needle600.py` they import), the police menus and records pages (rewritten into GameMenus); left behind:
PoliceProgress (unlocks), the campaign's police goals, PoliceRadioSync (Racer's editor clip import).
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
beside its zip: `BUG_REPORT.md` and `Screenshots\`). Read only; never move or delete them. Last read: session `2026-10-27_11-27-43-992_53ef4c` through BUG-001 (Round 75); before it `2026-10-27_02-23-23-098_3e251a` through BUG-005 (Round 74; BUG-001 sent by mistake); before it `2026-10-26_18-37-35-665_130b58` through BUG-006 (Round 73); before it `2026-10-25_22-20-32-569_189622` through BUG-004 (Round 70; none newer at Rounds 71 and 72; before it `2026-10-25_02-23-36-193_bcac2f` through BUG-003, Round 67; before it `2026-10-23_20-28-33-291_9085ef` through BUG-008, Round 64; none newer at Round 66; before it `2026-10-22_23-37-02-216_30fa55` through BUG-002, Round 62; before it `2026-10-22_09-43-30-831_9bfdff` through BUG-003, Round 61; before it `2026-10-22_05-11-54-864_7498e6` through BUG-001, Round 60; before it `2026-10-22_00-37-30-154_d7ca87` through BUG-003, Round 58; before it `2026-10-21_21-15-52-689_2c460b` through BUG-005, Round 57; before it `2026-10-21_17-00-50-815_f90718` through BUG-004, Round 56; before it `2026-10-21_12-16-40-990_d377b3` through BUG-003, Round 55; before it `2026-10-21_03-39-54-769_591b8d` through BUG-010, Round 54; before it `2026-10-20_01-26-12-613_8b4dc1` through BUG-005, Round 53; before it `2026-10-19_19-45-10-079_0a133b` through BUG-003, Round 52; before it `2026-10-19_13-25-47-596_f0f013` through BUG-004, Round 50; before it `2026-10-19_01-29-19-650_77c31b` through BUG-001, Round 47; none newer at Round 49; before it `2026-10-18_16-02-03-115_cda8a3` through BUG-001, Round 43; before it `2026-10-18_10-15-07-747_d1c371` through BUG-003, Round 42).

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
