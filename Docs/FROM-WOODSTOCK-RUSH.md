# Woodstock Rush sequel: handoff from Woodstock Rush

Updated 2026-10-09, after Woodstock Rush 0.99.0-review1. This is the starting brief for the sequel project: what to reuse, the rules that took 99 rounds to learn, how Dan works with Claude and Claude Code, and the mistakes not to repeat. Give it to the sequel's first Claude (chat) conversation and copy it into the new repo as `Docs/FROM-WOODSTOCK-RUSH.md`.

Source project: `C:\Users\danmo\Racer` (Unity 6000.6.1f1, URP 17.6, Input System 1.20, Windows). Its `PROJECT_TODO.md` (round history and every decision) and `CODEX_RULES.md` (standing rules) are the full record; this file is the summary.

---

## 1. The sequel in one paragraph

A new racing / free-roam game in Dan's current neighborhood (and maybe the area around it), built from real map data, reusing Woodstock Rush's systems so the work goes into the world, not into rebuilding vehicles, menus, campaign, split-screen and police. Dan is already finalizing the map himself (October 2026).

## 2. How to start: copy, then strip

Do not rebuild systems. Start the sequel as a copy of the Racer project's code and tools, then remove Woodstock-specific content.

1. New Git repo and a new release repo (Woodstock Rush's releases live at `danemoll-jpg/woodstock-rush-releases`; the sequel gets its own).
2. Copy `Assets/Scripts` (the runtime systems in section 4), `Tools/Blender` (vehicle, rider and prop generators), the launcher (`Launcher/`, `LauncherSDK`), the publisher (`Tools/Publish-LauncherRelease.py`), the debug-report system, `ProjectSettings` and `Packages`.
3. Leave behind: the eight course scenes and `FreeRoamWorld`, the `Report0xx` / `CR1xx` / `*Validation` / `*Checks` / `*Walk` / `*Probe` / `Activate-*` / `Cleanup-*` one-off round scripts (about half of the 316 scripts), route atlases, round evidence (`Docs/Report0xx`).
4. **Signing:** create a new publisher identity for the sequel once, deliberately. Never reuse, print or expose Woodstock Rush's private key.
5. Rename: game name, window title, save folder (Woodstock Rush saves live in `AppData\LocalLow\DefaultCompany\Racer\…`), launcher name, icon.
6. First round after the copy: compile, strip, build an empty test world, prove the build → publish → launcher → Play cmd pipeline works end to end before any world work.

## 3. The map: one world, real data

- **One world, routes over it.** Woodstock Rush has a separate copy of the world per course (8 race scenes) plus a Free Roam world. Fixes made in one copy did not reach the others; Dan was surprised races were "separate instances"; Free Roam and races drifted apart (a dry lake in Free Roam, a cave only in races, a storm drain that differed). **The sequel uses a single world scene. Races are routes (gates, AI lines, barriers, start grids) switched on over that world.** Race-only changes are made by toggling route objects, not by editing a copy of the ground.
- **Terrain** from USGS 3DEP elevation (1 m DEM; 1/3 arc-second fallback) via apps.nationalmap.gov/downloader. **Roads, building footprints, water, woods** from OpenStreetMap (credit required). **NAIP aerial imagery** (public domain) as a placement reference only; the look is stylized, nothing is draped. Google Maps data and imagery cannot be used.
- **Houses Dan cares about** are built from his photos and description (as Kyle's house was). Others can be generic.
- **Real road names only** as Dan names them. In Woodstock Rush that rule was: three real roads (S Cherokee Ln, Hwy 92, Trickum Rd), nothing invented on signs.
- Dan's lesson from the first game: "I gave too much freedom to build the tracks." Courses follow real roads and places he chooses; shortcuts are designed with him.
- Woodstock Rush's world was about 1.2 km × 0.9 km (an 800 m × 800 m neighborhood block plus a mountain annex). A few km² from real data is several times that; plan streaming / LOD early.

## 4. Systems to reuse (file names are in `Assets/Scripts`)

**Vehicles and driving**
- `ArcadeVehicle` (arcade physics, grip ceiling, braking, traction), `VehicleProfile` (stats per vehicle; ids: original Street Classic, tourer, moto Needle 600, atv Trail Four, roadster Sundown Roadster, fastback Highball Fastback, pebble Pebble Coupe, skyfin Skyfin Cruiser, scrambler Ridge Scrambler, drifter Drifter Twin, mower Turf Rocket, police car Patrol Car, policebike Patrol Cycle), `VehicleConfiguration`, `VehicleVisual`, `VehiclePaint`, `VehicleLights`, `VehicleAudio` + `VehicleSoundSynthesis` (engine sound made in code), `VehicleSurfaceContacts`, `VehicleContact`, `VehicleInput`, `VehicleRespawn`, `WeatherGrip` (rain 0.85/0.78/0.75, snow 0.70/0.65/0.62, ice 0.35 on paved/dirt/grass; setting to turn off).
- Models come from Blender scripts in `Tools/Blender` (`cars.py`, `cars81.py`, `bikes.py`, `needle600.py`, `trailfour.py`, `mower.py`, `police.py`, `policebike.py`, `traffic.py`, `rider.py`, `kit.py`, `render_*.py`, `contact_sheet.py`). Dan likes the vehicle and rider designs.
- `RiderLook`, `RiderGestures` (fist wave on LB, two-fist victory celebration, hands follow the wheel / bars), `ScenePeople` (Dan, Kyle and his brother in scripted scenes).

**Races and AI**
- `RaceDirector`, `RaceFlow`, `RaceGate`, `RaceProgress`, `RaceRoad`, `RoadDriver` (AI driving; jump run-ups use `aiEntrySpeed`, and the obstacle sensor ignores static ground inside a committed run-up), `ShortcutStrategy`, `WrongWayGuidance`, `WaypointGuide`, `AiFinishEstimate`, `WinnerShot`, `FinishPresentation`, `CleanLapGhost`, `RecordBoards` / `RecordView` (Top 10 with names), `PlayerNames`, `RaceNames`.
- Reset (`VehicleRespawn`): Dan's rule: a reset puts you at the closest point on the route, facing the right way, never back over road already cleared; never into the same pit; never into vegetation. "The wreck and the delay is enough punishment. This should be fun."

**Campaign and progression**
- `Campaign`, `CampaignData` (chapters, events: Race, TimeTrial, SpeedTrap, Jump, Smash; championships), `CampaignRun`, `CampaignEventUi`, `RaceMenus.Campaign`, `RaceMenus.Shop` (buy vehicles, three upgrade levels), `VehicleUnlocks`, `RaceMenus.Unlock` (unlock panel), `MedalUi` (drawn medals), `PoliceProgress`, `AtomicSave` / `RacerSave` (saves never corrupt; loading never writes).
- Campaign design Dan approved: start with one course and few vehicles; courses unlock by chapter; money buys vehicles and upgrades (campaign only); each chapter final awards a vehicle; championships per chapter; a solo practice lap on a new course before its first race; events chosen from a list by chapter; defaults to the last driven vehicle; new-player hints point to the race events; locked vehicles locked in Free Roam too; everything unlocked in split-screen; a testing switch "Unlock everything (testing)".

**Free Roam and activities**
- `ExplorationMap` (+ `.Controls`), `WorldMapVisual`, `WorldMapAreaOverlay`, `WorldMapCourseOverlay` (course routes drawn on the one map), `RacingMiniMap` (compass, Free Roam toggle), `ArcadeActivities` + `ActivitySite` (jumps, speed traps, smash; a jump counts if you land it), `ActivityRecords`, `ExplorationCollection` / `AcornAreas` / `AcornNotices` (collectibles with clear pickup banners and an unmissable unlock panel), `Hints`, `WorldEdge` (natural edge plus backstop), `ShallowWater` (escapable water).
- Day/night cycle (Free Roam 1 min = 1 h) and weather: `WorldLook`, `WeatherSky`, `WeatherEffects`, `SkyClouds`, `MoonIcon`, `SnowScenes`, thunder and lightning.
- Life: `AmbientVehicle` (traffic), `AmbientLife`, `HouseholdSchedule`, `Wildlife`, `WoodlandAmbience`, `UndergroundLife`.

**Police**
- `GetawayChase` (AI cops chase you: heat 1–5, backups placed ahead out of sight, exits covered when you go off-road, escape meter, roadblocks, helicopter from heat 4), `CopDriver` (pursuit driving that matches the runner's top speed), `RoadNet` (road graph, A*, place names), `PoliceChase` (Cop vs Runner, two players), `SpeedPatrol` (catch speeders for points; radar), `HiddenPolice` (parked cruisers in Free Roam that start a chase if you pass 10+ mph over the limit; setting), `PoliceHelicopter`, `PoliceLights`, `PoliceRadio` (voice lines from IDs, stitched pieces, radio effect, queue, music ducking).
- Radio voice: Dan's ElevenLabs voice "Sheriff John Paul", voice ID `319AW1BlfYo8QKsZ2K9o`. Script and IDs: `Docs/Audio/police-radio-script.md` (Project doc `claude/police-radio-script.md`); generator `Tools/Audio/generate_police_radio.py` with `Tools/Audio/police-radio-lines.csv`. For the sequel, swap the place and road lines (L, D) for the new map's names and regenerate.

**Split-screen**
- `SplitScreen`, `SplitHud`, `SplitRoam`, `RaceMenus.Split`: two players, top/bottom default or left/right, each controller its own player, player 2 can be the AI (so Dan can test alone), one shared radio either player controls, per-player camera views, Free Roam for two, police modes for two, names on the Top 10.

**Menus, UI, audio, presentation**
- `RaceMenus.*` (Core, Navigation, Shell, Entry, Pick, GaragePreview, GarageStats, Records, Medals…), `MenuInput`, `MenuGlyph` (Xbox / PlayStation button labels), `MenuActionRegistry`, `LoadingScreen` (poster art), `StartupTitle`, `WindowTitle`, `DisplayUnits`, `RaceHud`, `CameraViews` (chase, first person and others), `ChaseCamera`, `TrailerMode` (free camera tools for filming), `TrailerRecorder`.
- `LocalRadio` + `MusicCollection` (the radio plays Dan's own music; **music cannot ship if the game is ever sold**).
- `PhysicalSign`, `StreetSigns`, `RoadPosts` (see the sign rule).

**Tools around the game**
- Debug reports: `DebugReportSession`, `DeveloperLocationHud` (F3 debug mode, F4 capture with comment and screenshot, F6 menu; ZIP with BUG_REPORT.md, bugs.json, screenshots; the session stays open across game exits until Dan zips it). Every report carries version, scene, position, heading, vehicle, conditions and route progress. Keep this exactly; it is how Dan reports everything.
- Launcher / updater (signed catalog and manifests, auto-update from GitHub releases), `Play-Racer.cmd`, `Tools/Publish-LauncherRelease.py` (with `--resume-draft`), the bundled `gh` CLI, `LauncherBridge`.

## 5. Standing rules (from `CODEX_RULES.md` and the TODO's standing workflow)

Copy `CODEX_RULES.md` into the sequel repo and update names and paths. In short:

1. **Safety checkpoint** commit before every round; never discard Dan's work.
2. **Read PROJECT_TODO.md first**; the latest explicit decision wins; do not revive cancelled plans.
3. **KISS**: the smallest change; no unrequested roads, ramps, features, scenery or systems.
4. **Grounding**: when ground changes, everything on it moves with it. No floating or buried buildings, fences, signs, trees.
5. **Regressions**: check Git history first; restore the known-good version before redesigning. Dan's coordinates are the authoritative location.
5A. **Protected geometry**: roads, trails, jumps (approach, lip, flight path, landing, run-out), tunnels and shortcuts are protected; local fixes only; never fill space another route uses; check neighbours after changes.
6. Do not change unrelated systems.
7. **Do not bother Dan** for normal operations (Unity, Git, build, publish); only for real external blockers, with the exact action, then resume.
8–10. Use the existing Unity, the bundled `gh`, the existing signing identity (never regenerate or expose keys).
11–12. **Targeted testing only**; do not tune subjective feel forever. Dan playtests.
13–14. Update the TODO with the actual state; commit and push.
15–19. Fresh build from the completion commit; `Builds\Latest` holds the full new runtime; publish a new release (never overwrite); verify the real launcher path; local and published must match.
20–24. Check disk space; clean build artifacts after release (the project once had 300 GB of old builds); keep the current and one previous runtime; never delete source, docs, keys, saves or launcher; report sizes.
25–26. Definition of done and a short final report, then stop.

**Standing workflow rules added by Dan:**
- **Verification budget (2026-10-05):** "I do want things fixed to have targeted testing. But things like racing all the tracks twice, I can take care of." One targeted check per change at the place it changed; no race matrices, sweeps or long validation documents unless asked; results as a short list plus "for Dan to check".
- **Controller first (2026-10-07):** every new or changed menu checked with a controller only: every control reachable, focus visible, B goes back. **In the built player**, not just the editor.
- **World changes only where Dan pointed (2026-10-08):** fix exactly the reported place; list similar problems elsewhere for Dan; do not change them.
- **Signs (2026-10-09):** sign text is one-sided, hidden by terrain like the board, never drawn on top, fits inside the board, readable at about 20–30 m. Use the depth-tested lettering material, never a TextMesh's default font material.
- **Never write Dan's real save in a check**; work on a copy, pointed at before anything loads.

## 6. Dan's design preferences (apply from day one)

- Plays at 3840×2160 **with a controller** (Xbox; a PS4 pad also connected). GTX 1660 Ti: target 60 fps, split-screen included.
- Menus: scroll through values with left / right; **A never cycles a value**; one row per press; visible focus.
- Choosing a vehicle always goes to the garage view with the turning model and stat bars, never a line of text.
- Locked things are silhouettes with the goal and progress shown.
- Unlocks and pickups must be impossible to miss (a panel that stays until A).
- Medals are drawn medals with the target value, not sentences. Event screens show what is being measured; no lap clock that measures nothing; each attempt gets a clear result.
- Names: the player's name on the Top 10; rival names in the campaign; in split-screen only the other person's name; no names in quick races.
- Police modes are Free Roam only; solo play never swaps roles; being chased by AI is Getaway; chases start immediately with a cop behind you; escalation (heat, backups, helicopter) is visible and announced.
- "This should be fun": forgiving resets, escapable water and pits, no cheap traps.
- Dan points out where signs, barriers and fixes are needed; do not decorate the world on your own.
- The trailer: Dan makes it himself.

## 7. How the work runs (rounds)

- **Claude (chat)** triages Dan's debug ZIPs and messages, writes each round into the `## CURRENT — … — target X — NOT STARTED` section at the top of `PROJECT_TODO.md` on his PC, and gives Dan a task line. Previous rounds become `## Previous delivery — … — REVIEWED BY DAN (…)`. If Code is mid-round, the next one goes in `## QUEUED NEXT` and is promoted later. One round per prompt; two rounds in one prompt does not work.
- **Task line** for Claude Code, pasted after Dan's standard prompt (which tells Code to read `CODEX_RULES.md` first, then the TODO), in a **new Code chat each round**:
  `Carry out the section "CURRENT — <round name> — target <version>" in PROJECT_TODO.md, exactly as scoped there. That section is the full specification. Do nothing outside it.`
- Rounds often run unattended (overnight): every design decision goes in the TODO; "do not stop for design questions"; stop only for real external blockers. Big optional features get their own commit with the revert command, done last.
- Each part quotes Dan's words, gives positions from his reports, says exactly where and what, and says how to check it (built player, controller).
- Claude Code auto mode: allow rules live in `C:\Users\danmo\.claude\settings.json` under `autoMode.allow` (project settings files do not carry auto-mode rules). Add the sequel's repo path and release repo there.
- Versions: `0.NN.0-review1`, builds `NN000`, releases `game-NN000`.

## 8. Mistakes not to repeat

- **Separate world copies per race** (see section 3).
- **Checks that don't match how Dan plays.** Getaway "caught the runner in 114 s" in the editor with a scripted runner; Dan in the built game was never caught. A menu fix verified in the editor still skipped rows in the built player. AI cars "cleared the jump" in an editor event but not in a race. **Check in the built Windows player, with a controller, the way Dan plays**, and test with a human-like driver (fast vehicles, flat out, shortcuts).
- **Over-scoping world fixes.** Asked to block one road, the round blocked every off-route road on every course with oversized, backwards signs. Asked to clean up holes around a storm drain, the round sealed the tunnel with a lid. Asked to clear a jump's landing, the round removed all the bushes. Fix only the reported spot, and say what else was seen.
- **Claims without proof.** "Filled" pits that weren't; an icon "replaced" that wasn't (always read files back after writing them to Dan's PC). Before saying done, look at the result where Dan stood.
- **Text drawn over everything.** TextMesh default font material renders through terrain and on both faces.
- **AI obstacle sensors reading ramps as walls** (cars braked before jumps).
- **Writing Dan's real save** during checks (0.92 rewrote his campaign file once; restored from the game's own backup). Loading must never write; checks use copies.
- **Heavy verification eating time and tokens** (a 5-hour round of sweeps). Targeted checks; Dan plays the rest.
- **Filming before the game is right.** The trailer recorder filmed a broken world for hours. Dan films it himself now.
- **Designs that need too much from the player** to discover: Police Chase hidden under Split Screen; a "Start at" track choice that did nothing. Put modes where players look; remove options that do nothing.

## 9. Decided and open for the sequel

Decided: real neighborhood; real data; reuse the systems; one world with routes; Dan builds the map.
Open: how large an area; the name; which courses and where; campaign structure; which vehicles carry over (all, or a new set); whether any of the Woodstock Rush world appears; selling the game (then no personal music on the radio, and asset rights checked).

## 10. Other ideas from the Woodstock Rush backlog

Online multiplayer; VR for the Quest 3; a Steam Deck / handheld build; Xbox via Dev Mode (UWP; check Unity 6 support first) or a future Windows-based Xbox; a stunt / jump course; weather grip already exists.
