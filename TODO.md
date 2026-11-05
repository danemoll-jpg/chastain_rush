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

**Real jumps off ramps — DECIDED (2026-10-17).** Dan: "Lets not do fake jumps off of ramps. If there is a ramp we do
actual jumps only. If we do something on a scripted jump path it has to be something creative and not just a ramp. Lets keep
this as a standing rule." From now on a ramp, kicker, pile or stair launches by physics alone: no aimed or scripted flight
off it (the landing is sized to the real flight instead). A scripted or aimed path is allowed only for something that is not a
ramp and is creative (a water plume, a geyser, a trampoline). Launches built before this rule are left as they are until Dan
points at one.

**Shortcuts must be a risk — STANDING RULE (Dan, 2026-10-18 22:19).** Dan, on Round 44's lake jump: "It fails the rule that it
should be challenging. It is just a simple ramp that jumps a narrow part of the lake. Not challenging at all." and "A shortcut has
to be somewhat challenging so that is a risk to take it and to make people wonder if it is worth the risk (which I thought was
there from the beginning)". Every shortcut, now and later: a driver who gets it wrong pays for it (comes up short into the water,
misses the gap, crashes, loses more than it saves), so taking it is a real decision. Never pick the easiest crossing or the
shortest gap to make a check pass; if the risky version cannot be made to work, report that instead of building a safe one.

**Pipes through water are aquarium pipes — STANDING RULE (Dan, 2026-10-18 22:19).** "All pipes that go through a body of water
must be aquarium pipes." Any pipe or tunnel that passes under or through a lake, pond or creek is a glass aquarium run there (as
the network's glass mains: `PipeAquarium`, `MapBuilder.ClearPipe.cs`: the water above, fish, weeds, light shafts), never a plain
concrete pipe.

**No free way round a shortcut — STANDING RULE (Dan, 2026-10-19 13:37).** Dan, after Round 49's kicker: "little frustrating to
wait this long and it still ignores my shortcut rule about making them more challenging. The tube one was fine but the ramp one
really did nothing. As it is you could just drive through this area and skip the ramp and nothing will slow you down." A
shortcut's ground must not be drivable at speed without doing its stunt: block the free ways with things that could be there and
that you can drive through but slow you right down (bushes, hedges, a garden, a woodpile, fences that break), placed so the stunt
carries you over or past them. Every shortcut check from now on includes DRIVING THE SHORTCUT'S GROUND WITHOUT THE STUNT (round
the ramp, beside it, the other yards): that run must be slower than the plain lap; if it is not, the shortcut is not done.

**Tire tracks are the hint — STANDING RULE (Dan, 2026-10-19 13:29).** "why do we have a "jump" sign? the tire tracks are enough to
know it is a shortcut. I think at this point we can stop putting these signs up for this when there are clear tire tracks laid
out". From now on, where a shortcut's turn-off has clear worn tire tracks, no sign (no JUMP, PARTY or other board) is added.

**One map rebuild per round, and a limit on tuning — DECIDED (2026-10-18).** Dan, after Round 41 ran 9 hours (Part A alone
7.5 h of 30-minute Build Map runs while tuning the crypt toward a time target it never reached): (1) a round makes all its
world changes first and runs Build Map and the exe build ONCE, at the end (code-only steps in between use
`-playerBuildKeepScene`); each part still gets its own commit and its own targeted check on that one build. (2) Tuning
limit: if a number the round asks for (time saved, air time, height, speed) is not met after two attempts, stop tuning it,
report the numbers in the status and move on; Dan decides. Optional ideas in Dan's notes ("could", "maybe") are built as
the simple version, not as requirements to tune. Copy both rules into CLAUDE.md's "Process" in the same words.

**One heavy job at a time, at low priority — STANDING RULE (Dan, 2026-10-29 13:07).** Dan: "CODE Seems to keep locking up my computer with whatever it is doing"
(Round 82 ran its 6th full Build Map with two exe checks (check5a, check5b) at the same moment). From now on: never run a Build Map, a player build and an exe check
(or two exe checks) at the same time; one at a time. Start Unity batch runs and check exes at Below Normal priority (PowerShell `Start-Process ... -PassThru` then
`.PriorityClass = 'BelowNormal'`, or `start /belownormal`) and give Unity's batch runs fewer worker threads (`-job-worker-count` about half the cores), so Dan's
computer stays usable. At most 3 full Build Maps in a round; past that, stop and report. Copy this rule into CLAUDE.md's "Process" in the same words.

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 92, THE LAUNCHER UPDATE FIXED, GLASS UNDER THE CREEKS, THE RACE FOR A PLANE OR A CAR (Dan, 2026-11-04 12:37, three photos of his screen, no reports; and the queued race of
2026-11-02 01:40).** Round 91 is reviewed. Item 0, the one-heavy-job rule (at most 3 full Build Maps) and the end-of-round clean-up apply. Short results.
0. FIRST: THE UPDATE THAT NEVER ARRIVED (Dan, 2026-11-04 12:42, a photo of the launcher: "Installed: 0.90.0" and "Manifest size limit exceeded"; "So let's get this fixed and
   ignore my complaints until I can review"). Cause (Claude): Dan's installed launcher refuses any file of 2 GiB or more (`Launcher/store.cpp` `Bytes`: b < 2 GiB, "Manifest size limit
   exceeded"), and 0.91.0's `ChastainRush_Data/sharedassets0.assets` is 2,193,343,600 bytes (level0.resS 1,994,916,288, close behind). Round 91's "proven from GitHub" missed it.
   Fix it so the launcher ALREADY INSTALLED on Dan's PC and Deck (it cannot be changed from here) takes the update: every file in a release under 2 GiB with room to grow (aim
   at most 1.6 GiB each): split the world's data across more files (e.g. the map's areas in their own scenes / asset bundles loaded with the main scene, or Unity's build
   settings that split shared assets), and / or shrink it (texture and mesh compression, the terrain's data) without changing how the game looks or plays; frame rate and load time
   before / after (PC and the Steam Deck setting). Then: (1) the publisher refuses any file over 1.8 GiB, with the file named; (2) every publish is proven the way Dan gets it: the
   PREVIOUS published release installed by the published starter's launcher, then updated by it to the new one from GitHub (not a fresh install); (3) a new launcher build with the
   per-file limit raised (files streamed, 64-bit sizes) AND able to update itself from the releases, shipped inside the next game update so later launchers fix themselves; say
   whether the installed launcher can take it in place, and if not, the one step Dan does once. Publish 0.92.0 with 0 done (and E and F if they are done), and confirm the
   installed 0.90.0 launcher updates to it.
   HELD until Dan has played 0.91 or later (his photos were 0.90.0; Round 91 0 may already cover them): his 2026-11-04 12:37 notes, kept word for word in QUEUED as "QUARRY NOTES
   FROM 0.90". Do not work on them this round.
E. CREEK CROSSINGS IN GLASS. Round 91 C left the new mains in plain concrete where they pass under creeks: the standing rule says every pipe through a body of water is an aquarium
   pipe (creeks included). Make those runs glass (water above, fish, weeds, light). Check: a picture from inside at each.
F. A RACE FOR A PLANE OR A CAR (queued since 2026-11-02; Dan, 2026-11-04: "I had mentioned I wanted a race that was airplanes vs cars. What happened to it?"; it waited for Round 91's
   underground). Dan: "a race that you could race as a plane or a car. The estimated time for both should be about the same and should use much of the same road. But we can do things
   to give the cars an advantage and add disadvantage to the planes so that one whole race should take the car and the plane about the same amount of time. For example planes could
   have things like forcing them to go a longer path and have cps under bridges to force them to fly more precisely and having to lower speed whereas cars could have underground
   shortcuts using the underground tube system". One course, raced in race setup as a car or a plane, and in mixed fields (cars and planes together, AI of both). Cars: the roads, with
   underground shortcuts through the new network (Round 91 C: M7 / M8 and their express runs, entered and left by manholes on the course). Planes: rings that follow much of the
   same road but take longer ways round, low rings under bridges and overpasses (small enough that full speed misses them), slow gates (a ring you must take under a speed). Tune so a
   good car lap and a good plane lap (the human-like driver and pilot, and the AI on each level) come within about 5 % of each other; report both times per difficulty (two tuning
   attempts, then report). Records separate for cars and planes, and a combined board; ghosts and replays of both; HIGH MODE for both (bumpers for cars, Round 84's put-back for planes).
   Check: 3 races each way (all cars, all planes, mixed) with times; the car takes every pipe shortcut, the plane every low ring.
Check: item 0 for every item. PUBLISH through the launcher.
For Dan to check: the launcher updating itself on the PC and the Deck; the glass under the creeks; Wings & Wheels as a car (the tube's turn-off off Tower Road) and as a plane, and whether the planes should be slowed more.

**Status (Claude, 2026-11-04): 0 and E published as 0.92.0 (`game-9200`); F built (2nd full Build Map), checked in the exe and published as 0.92.1 (`game-9201`, Latest).** Evidence `Data/screenshots/round92/`. No bug report newer than Round 87's.
0 DONE. Cause confirmed: the old launcher refuses any file of 2 GiB or more; 0.91.0 had `sharedassets0.assets` 2.19 GB and `level0.resS` (Unity's static batches) 2.0 GB. Now (a) the build starts with a tiny Boot scene that loads the world, with six never-loaded data scenes in between that each hold a share of the meshes / terrain / textures, so Unity writes them to six files (`Editor/WorldSplit.cs`, `WorldBoot.cs`; startup code that waited for "the first scene" now waits for the world, `WorldBoot.Defer`); (b) meshes over 4,000 vertices are left out of static batching (one draw each already; 37,565 small renderers still batched; `MapBuilder.TrimStaticBatching`). Largest file now 1.2 GB (aim 1.6), total 4.96 GB (0.91: 4.82), game zip 1.46 GB. Before / after, 11 places across the map at 1280 x 720 (not measured at 3840 x 2160): PC 56 -> 54 to 57 fps average (run-to-run noise ±3; Dan's PC was in use), load 36.5 -> 29 to 43 s; Steam Deck setting 58 -> 59 fps, load 28.7 -> 26.6 s (`before_*.txt`, `round92_check_*.txt`, `places_after.png`). Tried and dropped: static batching off (PC -10 fps), batching at load time (load +20 to 40 s).
0 (1) DONE. The publisher refuses any file (or package) of 1.8 GiB or more and names it (`Prepare-*.py` `guard`, `Publish-LauncherRelease.py`); it refuses the 0.91.0 draft naming both files. The player build also fails on one (`BUILD_INFO.txt` lists the largest files).
0 (2) DONE. Proven Dan's way, from GitHub: the published starter's launcher (0.81.0-Full, the same launcher Dan has, SHA f5648b52) installed 0.90.0, failed on 0.91.0 with "Manifest size limit exceeded" (Dan's message), then updated 0.90.0 -> 0.92.0 and started it (handshake PASS) (`local/round92/updtest/`).
0 (3) DONE. Launcher 2 (`Launcher/`, LauncherBuild 2): files and packages up to 64 GiB, and it updates itself: each game release carries it (`Launcher/ChastainRushLauncher.exe` + `launcher.json`, copied by PlayerBuild), and a launcher older than the one in the installed game swaps itself on start (hash checked against the signed manifest; the old exe renamed `.old`, removed next start). The installed launcher 1 cannot do that, so 0.92.0's game does it once: when launcher 1 (known hash) starts it, the game renames it and copies launcher 2 in its place (`LauncherBridge.ReplaceOldLauncher`). Proven in the test: after the update, the install's launcher was launcher 2, it started, showed "launcher 2", found 0.92.0 up to date. Nothing for Dan to do: open the launcher as usual; it updates to 0.92.0, starts the game, and the next start is launcher 2. Launcher 2 updating itself from a LATER release is not proven yet (needs the next release).
E DONE. Where the stage-5 mains pass under a creek, a pool at the creek's level is dug round the crossing (34 x 22 m, clear of roads and buildings) and the pipe runs under it in glass, as under a pond (`pipe_network.py` creek pools, `MapBuilder.Network.cs` DigCreekPool): CP1 and CP2 on M7a, CP3 on M7c (Noses Creek), CP4 on M8a (the pipe brought up 1 to 23 m with 10 % ramps). Check: a picture from inside at each (water over, weeds, fish, light) and from the bank (`glass_sheet.png`); M7a, M7c, M8a driven end to end, no stuck spot, jolts as in Round 91 (`pipes_m7a_m7c_m8a.txt`). NOT done (too deep): M7a under a creek at (1838, 1017) 27 m down, M8a under Noonday Creek at (-1305, -1258) 74 m, M8b under a creek at (-2123, -646) 93 m; glass there needs 21 to 87 m of climb each way in the main (network.json `pools_not_built`). For Dan: a deep glass shaft, a hump in the main, or leave them.
F DONE, timing NOT within 5 % after the two tuning attempts (reported, not tuned further). "Wings & Wheels" (race setup, Street or Air; `make_wings_route.py`, `make_wings_air_route.py`; `RaceController.Twin.cs`): one race, two laps over the same streets round Kennestone and the square. A car runs Cherokee, Tower Road, Church and Hansell (4.7 km), with THE TUBE: off Tower Road by worn tracks into the X_KEN mouth, down M7d with the express, out of the Root Street manhole (forgives CP4 to 8; the corridor now checks height, so the ground over the pipe does not count). A plane flies 10.1 km of rings the long way round: two low frames (under the hospital's skybridge, under the railroad), two slow rings (50 and 45 m/s, "TOO FAST" if over). Mixed fields (each racer its own lap; the opponents page offers cars and planes, All cars / All planes), records per kind plus a combined board, pipe express on in this race. Attempt 1 ran the southwest loop off the land (every plane into the edge wall); attempt 2 put it back and lengthened the northwest loop; the tree on Church Street's descent to C1 is cleared (track `wings_and_wheels_air.json`). Check (the exe, `f_final/`): human-like car 2:54 by the roads, 2:23 through the tube (2:10 in a cleaner run; 3 stalls this time near the turn-off and Cherokee), human-like plane 2:03 / 2:03, no crash, every low frame and slow ring taken. AI best laps, cars / planes: Easy 2:12-2:33 / 1:58-2:11; Normal 2:06-2:26 / 1:58-2:08; Hard 2:01-2:16 / 1:55-2:03. All 9 fields (all cars, all planes, mixed x 3 levels) finished; the AI took the tube on its own. Planes are 5 to 14 % quicker. To even it: shorten the car's lap or make the plane's longer (Dan decides). Pictures looked at: the turn-off (the orange shortcut on the minimap), inside the tube, the pop-out onto Root Street toward the line, the low frame under the skybridge. Not checked: HIGH MODE, ghosts and replays in this race (they use the shared race code).
Full Build Maps used: 2 of 3.

## QUEUED rounds
**QUARRY NOTES FROM 0.90 (Dan, 2026-11-04 12:37; HELD: Dan reviews 0.91+ first, then says which still apply).**
NOTE: Dan's photos show "CHECKPOINT n / 19": that is 0.90.0's count (0.91.0 has 22). FIRST check which build his PC and Deck run (the launcher's installed version) and say it;
if the launcher did not update to 0.91.0, find why and fix it. Each item below is checked on 0.91's world first (some may already be fixed by Round 91 0); map his 0.90 checkpoints
to 0.91's: 0.90 CP4 = "the pit floor", CP11 = "the haul road's hairpin", CP12 = "the east rim".
A. AN UNINTENDED JUMP BEFORE THE HAUL ROAD'S HAIRPIN. Photo 1 (lap 2, 0.90 "CHECKPOINT 11 / 19" 201 m ahead: just after The Corner's landing / run-out, the gravel crest with the lake
   below): "smooth this so it isn't an unintended jump". Find it (the bump log if Dan's PC session has it; else a driven trace) and smooth it into one vertical curve.
B. THE PIT FLOOR AFTER THE BIG ONE: A TRAIL, AND NO JUMP ON IT. Photo 2 (lap 2, 0.90 "CHECKPOINT 4 / 19" 326 m: right after The Big One's landing, an open grass field, the quarry
   walls ahead, no trail): "road removed but still no trail right away to lead the way. Also when do find the trail there was another unintended jump". The gravel course runs from
   the landing's run-out straight on, visible from the touchdown, edged (Round 91's reflector posts), to the pit floor; smooth the jump on it.
C. ROADS TO NOWHERE OUT OF THE QUARRY. Photo 3 (lap 1, 0.90 "CHECKPOINT 12 / 19" 123 m: toward the east rim, a grey road climbing a bank off to the left into nothing, another on the right,
   barriers at their mouths): "this road and general roads like this leading to nowhere in the quarry need to be removed. This is the only event here so roads unrelated to it make no
   sense". Remove EVERY road inside the quarry (pit, benches, walls, rim inside the quarry's outline) that is not the race's course or a way in / out the race or Free Roam needs
   (`drop_roads`, as Round 91 0), with their barriers; list them. Check: a top-down picture of the quarry before / after; the race's closures still close what is left.
D. THE QUARRY'S PIPES AND GEYSERS FINDABLE, AND NO GETTING STUCK. Dan: "outside of the race I didn't find any entrances to the underground nor the geysers to push drivers up. Should be
   clearer so you don't get stuck there". In Free Roam: the shore exit into the network and the two launch pipes are plain to see from the pit floor and the rim (a big concrete
   headwall / culvert mouth that reads as a way in, worn tire tracks to it, the geysers spouting now and then so you see where they are from afar, a little mist); on the map
   (Free Roam's map and PIPES layer) and the minimap as icons; one of each in view from the bottom of each haul road. A car that drives down into the pit always has a way out in sight:
   the haul roads up, or a launch. Check: from the pit floor's middle and from each landing, a picture showing a way out; driven from the rim to each pipe and out by launch.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 91
**Reviewed: Dan drove the quarry 2026-11-04 (photos; his notes are Round 92).**
**Round 91, THE QUARRY RACE READABLE AFTER ITS LANDINGS, SPLIT-SCREEN NOTES AT THE TOP, PLANE ROLLS AND LOOPS, THE UNDERGROUND ACROSS THE WHOLE MAP (Dan, 2026-11-02 01:40
and 16:42; started 2026-11-03).** Rounds 88 to 90 driven only briefly by Dan. Item 0, the one-heavy-job rule and the end-of-round clean-up apply. Short results.
0. THE QUARRY RACE AFTER THE BIG ONE (Dan, 2026-11-02 16:42, a photo of his screen, no report: lap 1, CHECKPOINT 4 / 19 "the pit floor" 320 m ahead, just after
   The Big One's landing (CP3); the picture: a huge plain grey wedge rising steeply into the sky, a green bank wall, red / white barrier ends, no road in sight):
   "Not sure what this is supposed to be and not really obvious where the track goes from here". (1) Say what the grey wedge is (The Big One's landing slope seen from
   below? a kicker? a cut?) and make it look like what it is in a quarry (crushed stone / gravel with edges, texture, a skirt; or remove it if it is leftover). (2) From
   The Big One's landing to the pit floor and on, the course must read at a glance: the gravel road visible ahead from where you land (not hidden behind the slope),
   its edges marked as the other courses' (dividers / berms / bumpers in HIGH along the way you go, never across it), the next checkpoint's arch in view from the
   landing, and the run-out turning you onto it. Check every landing and every bend of the lap the same way (where you land or round a corner, the way on is in
   view), with a picture from the driver's seat at each landing's touchdown and at Dan's spot.
A. SPLIT SCREEN: NOTES AT THE TOP. Dan: "when in split screen, for notes like Jump distance and speed traps can we put at the top of the screens instead of in the middle
   of the screens?" In split screen, every pop-up note (jump distance / air time, speed traps, combos, stunt scores, near misses, egg found, RING MISSED and the like) shows
   at the top of that player's half, out of the middle of the view, readable at 1280 x 800 (the Deck) and 4K; one player's screen unchanged unless it also covers the middle.
   Check: pictures of each note in both halves, side by side and top / bottom split.
B. PLANES: BARREL ROLLS AND LOOPS. Dan: "Allow planes to do barrel roles and loops". Both planes (and new ones later) can roll all the way round and pull a full loop:
   no bank or pitch limit in the air (`PlaneFlight`, `PlaneRoll / PlanePitch / PlaneBank`), still arcade-easy (it levels itself when you let go, as now, unless you hold
   the roll / pitch), and it does not crash itself upside down at height. AI pilots unchanged unless they need it. Free Roam, air races and HIGH MODE (HIGH: a roll or
   loop never ends in a crash; Round 84's put-back stays). Optional if cheap: a small "BARREL ROLL" / "LOOP" note like the car stunt notes. Check: a barrel roll each
   way and an inside loop in each plane by the human-like pilot (controller input), recorded as a picture series; the plane keeps flying.
C. THE UNDERGROUND ACROSS THE WHOLE MAP. Dan: "we need to expand the underground system to cover the larger map. We can for example have at least a couple pipes at the
   bottom of the quarry. We can have one or more leading to other parts of the underground network and one or two that shoot the user up back to the top of the
   quarry". The pipe network (`pipe_network.py`, network.json, `Docs/PIPE-NETWORK.md`; Dan's vision: a maze with secret exits all over, the hub chasm, pop-out manhole
   covers, gates and switches) covers only the first map. Extend it over the expansion: mains from the existing network west and south to Town Center (and under the
   mall), KSU, Kennestone, Marietta Square, Kennesaw Mountain's foot, the airport and Viking Quarry, with secret exits (manholes, drains, creek outfalls, rigged hydrants)
   in each area, the express on long runs, the map's PIPES layer and the Underground's map. At VIKING QUARRY: at least two pipe mouths at the bottom of the quarry
   (in the lake: aquarium pipes, standing rule; or in the quarry floor's dry part), at least one leading into the network, and one or two LAUNCH pipes that shoot you up
   and out back to the top of the quarry (a scripted geyser-style flight is allowed: creative, not a ramp; you land on the rim on drivable ground). Check: every new main
   driven end to end (no stuck spot, no step, walls rideable as the old pipes); each exit reached and left; the quarry's launch pipes from inside: out on the rim, upright.
Check: item 0 for every item. One round's rules (one heavy job at a time, at most 3 full Build Maps). PUBLISH through the launcher.

**Status (Claude, 2026-11-04): built (2 full Build Maps, 2 code-only exes; exe 06:23), checked in the exe, published (0.91.0, `game-9100`), not yet driven by Dan.** Evidence `Data/screenshots/round91/` (`round91_check_1..5.txt`). No bug report newer than Round 87's.
0 DONE. The grey wedge was the real quarry's old pit service road (OSM 1173718687 past its junction at (-1946, -1160), and its branch 1173718688) drawn draped over The Big One's graded landing slope: a road climbing a fill bank into the sky (`before_dan_spot_grey_wedge.png`). Both no longer built (track `drop_roads`, `MapBuilder.Round91.cs`), with their 7 barrier sets. Each run-out now has its own gravel surface, reflector posts along both edges (red left, amber right, not solid) and a checkpoint 10 m past the landing (22 checkpoints). Check: from each touchdown and from Dan's spot the next arch is in view from the driver's seat, normal and HIGH (`91_land_J*_seat*.png`, `91_dan_spot*.png`); 16 bends pictured (`bends_sheet.png`): the way on is in view at each.
A DONE. Split screen: race messages, jump / trap / combo notes, the trick box and HIGH's note at the top of each half (a narrow half: left of its minimap). Check: 4 of 4, side by side and top / bottom (`91_split_*.png`).
B DONE. Both planes roll all the way round and loop (`PlaneFlight.Aerobatics.cs`): no limit while you hold the stick; let go and the wings level the near way and the nose comes back to the horizon; never crashes from being upside down; HIGH pulls out of a dive near the ground by itself; BARREL ROLL / LOOP notes. AI and scripted pilots unchanged. Check (controller input, both planes): a roll each way and an inside loop, flying level 5 s after; a loop comes out 27 to 67 m from where it began at the same heading (`91_plane_*_sheet.png`, `91_dart_*_sheet.png`). HIGH's pull-out not flown by the check.
C DONE. Network stage 5 (`pipe_network.py --stage 5`): M7 South Main (hub, Town Center J20, Kennesaw Mountain J24, Kennestone J25, Marietta Square J26; glass under Christon Lake), M8 West Main (J20, Viking Quarry J23, Kennesaw State J22; glass under the campus pond), 14 branches, express on every long run, 6 manholes (Ring Road NW, Campus Loop Road, Paulding Avenue, Kennesaw Mountain Drive, Campbell Hill Street, Root Street), 5 exits (Town Center, Kennesaw Mountain, Kennestone, Marietta Square, the airport) and at the quarry floor: a shore exit into the network and two LAUNCH pipes (a geyser at the mouth and an aimed flight onto the airport plateau, `Pipes/PipeLaunch.cs`; off during races). Old network unchanged but one new hub opening. Check: every new main and branch driven end to end, no stuck spot or reset; every manhole and exit driven out of (`91_out_*.png`); both launches land on the rim upright, 28 and 35 m from their targets (`91_launch_*_landed.png`).
Not done: where the new mains pass under a creek the pipe is plain concrete (the two ponds they cross are glass). Full Build Maps used: 2 of 3.
Published: game 0.91.0 (`game-9100`, Latest) through the launcher. Clean-up: 44.9 -> 44.6 GB.
For Dan to check: Viking Quarry after each landing (normal and HIGH); split-screen notes; rolls and loops in both planes (and HIGH near the ground); the new pipes from Town Center / KSU / Kennesaw / Kennestone / the Square / the airport, and both quarry launch pipes.

## Previous delivery 2: Round 90
**Driven briefly by Dan 2026-11-02 (one note: Round 91 item 0); not fully reviewed.**
**Round 90, A VIKING QUARRY CAR RACE WITH HUGE JUMPS OVER THE LAKE (Dan, 2026-11-01 11:37: "The quarry - I wanted a car race with huge jumps over the lake").** Rounds 88
and 89 are not yet driven by Dan. Item 0, the one-heavy-job rule (one job at a time, Below Normal, at most 3 full Build Maps) and the end-of-round clean-up apply. Short results.
This settles the long-open quarry question (Laura Lake / Viking Quarry, queued since 2026-10-22). Two earlier tries at a Quarry Track were not built (Rounds 73 / 74,
`make_quarry_track.py`, output in `local/round73/` and `round74/quarry_track_unbuilt/`): the haul road crossed the lane so racers skipped a loop, and the AI and the check
driver got stuck (56 resets). Learn from them; design a new course, not that one.
A. THE QUARRY RACE. A car circuit (race setup "Viking Quarry", forward; reverse only if it plays well) round and through Viking Quarry whose heart is HUGE JUMPS OVER THE
   LAKE, on the course's line, not off to the side: at least three big lake crossings per lap (more if the quarry allows), each flying a real stretch of water (much
   bigger than anything in the neighborhoods: think Kennesaw summit leap scale), plus the rim, the haul roads down the benches, the water's edge.
   - Real physics only (standing rule): every launch off a ramp is a physics jump; size each ramp's angle and run-up so the speed the course gives there clears the water
     with margin for every vehicle class allowed (say which), landings on sloped decks or banks that match the flight (no flat slams), run-outs long and clear.
   - Ramps that belong in a quarry, all different (variety rule, not dirt piles): a rock ledge off a bench, a haul-road ramp of crushed stone, a conveyor gantry, a tipped
     dump-truck bed, a stacked-boulder kicker, the Free Roam steel kickers if one fits the line (they stay in Free Roam as well).
   - Coming up short means the lake: the car splashes, sinks partway (water rules, Woodstock Rush's), and is reset before the jump's checkpoint (Round 86 B), so a bad
     run-up costs time, never the race. No free way round a lake jump (standing rule): the jump is the course.
   - No crossings of the course with itself or with the haul roads at one level (the earlier tries' failure): a crossing is a bridge or is closed.
   - Checkpoints line each run-up up straight; the crowd on the bleachers; signs only where the standing rules allow (tire tracks / LANDING boards).
   - Built like the other courses: AI that takes every jump at the right speed (they slow for the run-up's window, never jump short as a rule), closures off the line,
     records, ghosts, replays (the winner's flight from the TV camera), the map and loading inset, HIGH MODE bumpers (Round 86 / 87 rules: open at every jump's flight and
     landing; out over a bumper resets you).
   Say the lap's length, the number and size of the jumps (gap over water, air time, height) and the lap time.
Check: the human-like driver races 3 laps against 3 AI in three vehicles (the bike, a car, the heaviest allowed), normal and HIGH: every jump cleared at race speed, no
stuck spot, no reset except a forced short jump (it resets before the jump); a deliberately slow run-up at each jump lands in the lake and resets correctly; all AI finish.
Pictures: each jump from the driver's seat at the lip and from the TV camera mid-flight. PUBLISH through the launcher.
For Dan to check: the Viking Quarry race (normal and HIGH), the jumps, a short one into the lake.

**Status (Claude, 2026-11-02): built (3 full Build Maps, the limit, then quick builds of the MountainRace step; exe 06:39), checked in the exe, published (0.90.0, `game-9000`), not yet driven by Dan.** Evidence `Data/screenshots/round90/`. No bug report newer than Round 87's.
A DONE. "Viking Quarry" (`Tools/mapgen/make_quarry_route.py`, world `MapBuilder.QuarryRace.cs`), forward, 2 laps, 8.8 km a lap (AI 4:25 to 5:10). Why this shape: a car clears the lake only from the airport's plateau (320 m) down to the pit's benches (200 to 227 m), and only where they are within about 270 m of the rim; Free Roam's three quarry jumps already sit on those three lines, so the race flies them: The Big One (J3, crushed-stone kicker, about 245 m, 6.3 s, 100 m drop), The Corner (J1, the steel kicker, about 230 m, 6.5 s), The Long One (J2, the old conveyor's gantry, about 260 m, 6.3 s); kickers widened to 18 m, every flight unchanged. Each jump lands in the pit, so the lap climbs out three times without crossing itself: the low haul bench (down the north bank, round the west end at the water, up the south cliff under the kickers, through a cutting in the rim), the quarry's own haul road up the east wall, the high haul bench up the north wall; gravel 16 m wide, rock berms only over 10 m drops, race-only dividers on the pit floor, streams kept off the course (a creek crossed J1's run-up), the course clear of the airport's buildings. Short = the lake: an automatic reset (route `lake_reset_s`) to the run-up's head, no penalty (check: 3 of 3). Route extras: `miss_penalty_s` 30 (the legs pass close across the plateau), `ai_safe_jumps`, `ai_flat_stations` (the AI's stations were 300 m off after a jump: they braked at the lip), `high_half_m` 8 (HIGH's bumpers had stood 3.7 m out on the old 5 m service roads). Replays: Free Roam's TV cameras on the rims and landings. Vehicles: every ground vehicle but the stunt ones.
Dan's 2026-11-02 note (the check car "perched on the edge of a grey stepped structure by a landing deck"): every stuck moment looked at (`stuck_spots_before.txt`, pictures `before_spot*` / `after_spot*`): 7 were wide or short landings caught on a 43 to 66 degree bank beside a deck, against its steel side skirt (the decks stood out over the bank on trestles); 6 were the rock berms ("the grey stepped structure", 1.6 m wide stones a car sat up on: at the haul road's start, the south-west corner, The Corner's run-up head); 1 the high bench's vertical cut face; 1 the conveyor gallery's end. Fixed: each landing is now an earth landing slope 90 m wide at its deck's angle, filled out to the deck's front (the deck lies on it), its banner posts not solid; berms only over 10 m drops, 60 m from landings and run-up heads, a thin wall; banks of about 30 degrees; the gallery and the start gantries' towers 17 / 18 m out. The player's resets per race (button + lake), before -> after: normal Needle 7 -> 1, Street Classic 9 (gave up) -> 4 (finished), Skyfin 9 (gave up) -> 6 (finished); HIGH Needle 3 -> 1, Street Classic 1 -> 2, Skyfin 5 -> 4; stuck moments 18 -> 1 (in the lake 70 m wide of The Big One, outside its reset area). (`race_check_before.txt`, `race_check_after.txt`.)
NOT MET: "no reset": the human-like driver still comes up short on 1 to 7 jumps a race (lake resets), mostly its first try after a run-up's bend (from a run-up's head it makes every window). The AI finish every normal race (8 to 9 resets in 3 laps each); in HIGH MODE the AI were still on lap 3 when the player finished (6 to 7 minute laps) and are not proven to finish. (Tuning limit; Dan decides.)
Published: game 0.90.0 (`game-9000`, Latest) through the launcher. Clean-up: 43.5 -> 42.6 GB. For Dan to check: the race (normal and HIGH), a wide landing at each jump, a short one into the lake, HIGH with AI.

## Previous delivery 3: Round 89
**Not yet driven by Dan; he reviews it with Rounds 88 and 90.**
**Round 89, THE ROADS ROUND 88 MADE WORSE FIRST, THEN THE REST; POLICE RADIO STREET NAMES FOR THE WHOLE MAP; GOOSE EGGS ACROSS THE WHOLE MAP (Dan, 2026-11-01 03:04: "OK
so it is done, but I dont have a chance to test. Can we have continue to fix stuff? Also, don't we need to include more street names for the cops? I would also like to
either spreadout the goose eggs or create more to be found all across the map. We did both these things before the map was expanded").** Round 88 is not yet driven by
Dan. Item 0, the one-heavy-job rule (one job at a time, Below Normal, at most 3 full Build Maps) and the end-of-round clean-up apply; A's and C's world changes in one
Build Map where possible. Short results.
A. ROADS: NOTHING WORSE THAN BEFORE, THEN KEEP FIXING. Round 88 C made four places worse: Canton Road at (1913, -5855) 2.1 s (beside deck 51, whose end its pass changed),
   I-75 at (1880, -4459) 2.5 s, Greers Chapel's 0.2 s flights now 0.4 s, the I-575 Express flyover into I-75 2.2 s forward (was 0.76 s). FIRST each of these is fixed
   properly or put back as it was before Round 88 (two attempts each, then put it back): no place ends this round worse than before Round 88. THEN the rest of Round 88's
   list: I-75's Express Lanes northbound stuck at the Frey Road deck (3.3 s in the air), I-75 southbound 0.94 s at the I-575 merge, Frey Road / I-75, I-75 at Big Shanty,
   I-75 by Greers Chapel and the Express under it, Bells Ferry, the Canton Road Connector interchange. Check: all 416 crossing runs and the Express Lanes end to end both
   ways on the final build: list each place better / same / worse than Round 87's build; none worse.
B. POLICE RADIO STREET NAMES FOR THE EXPANDED MAP. The radio's street lines (D001 to D136, Round 41 / 45, `Tools/Audio/police-radio-lines.csv`, Dan's ElevenLabs cop and
   pilot voices) cover only the first map; since then the map grew west and south (Town Center, KSU, the airport, Kennesaw Mountain, Marietta Square, Kennestone): Claude
   counted about 1,880 named streets on it now, 1,747 without a line, 80 of them main roads (Barrett Parkway, Cobb Parkway, Chastain's neighbors, Church / Cherokee /
   Roswell / Atlanta Streets, Big Shanty, Bells Ferry's ends, Old 41, Polk, Mill Street, Mall Boulevard, Busbee, Frey, Greers Chapel, Allgood, Burnt Hickory, ...).
   Add lines (new D IDs after D136, never changing existing IDs) for: every named trunk / primary / secondary / tertiary road and its links; the highways as people say
   them (I-75, I-575, the Express Lanes; motorways were left out before, but chases now run on them); and the residential and unclassified streets within 300 m of any
   race course, police spawn, hidden cop or fast-travel spot. Say the count before making audio; over 400 new clips: do NOT stop (Dan, 2026-11-01 03:13, overnight): make the main roads' and the
   highways' clips first (and the "Suspect near..." piece), leave the residential / unclassified streets listed for the next round, and carry on. Make the clips with the studio
   (`python Tools/Audio/police_radio_studio.py --make-missing`, cop voice; pilot takes `_air` where the pilot can say them, the key from ELEVENLABS_API_KEY, never printed).
   A street with no line: the radio names the nearest street that has one, with a new phrase piece "Suspect near..." (and its pilot take), never silence or a wrong
   "on". Check: a Getaway on the Needle 600 to heat 5 in Town Center / Barrett Parkway, Marietta Square and Kennesaw: calls name the right streets (log each call's street
   and the player's road).
C. GOOSE EGGS ACROSS THE WHOLE MAP. The 100 eggs (`Data/explore/eggs.json`, `make_eggs.py`, Round 48) all lie in the first map (x 118 to 3910, z 280 to 1879); the map
   now runs to x -8630, z -8311. Keep the 100 (their IDs, so Dan's found eggs stay found) and ADD 150 (250 in all) spread over the expansion by area and size (Town Center
   inside and out, KSU, the airport, the quarry, Kennesaw Mountain, Marietta Square, Kennestone, Noonday Creek Trail, Addison Heights, the neighborhoods, the woods, the
   pipe network's new reaches), the same mix of easy / hidden / stunt-reached as Round 48; new areas and destinations added to the exploration map and fast travel; tiers
   rescaled to 250 (what they unlock still waits for Dan). Check: every new egg reached in the exe (a reachability drive or placed-car probe per egg), none inside
   buildings or under ground, the GOOSE EGGS page and map showing 250, a copy of Dan's save keeping its found eggs.
Check: item 0 for every item. PUBLISH through the launcher.
For Dan to check (with Round 88's list): the places Round 88 made worse; the Express Lanes at Frey Road; a police chase outside the first map (street names); new goose eggs.

**Status (Claude, 2026-11-01): built (3 full Build Maps, the limit; exe 07:56), checked in the exe, published (0.90.0, `game-9000`), not yet driven by Dan.** Evidence `Data/screenshots/round89/`. No bug report newer than Round 87's.
A DONE but one ramp. All 416 crossing runs on the final build (`final_crossings.txt`): 2 not clean, both ramp 879570834 under Greers Chapel's decks at (146, -2643) (1.3 s and 0.5 s). Against Round 87's build (`final_crossings_vs_round87.txt`): 63 better, 352 same, 1 worse (that ramp: Round 83's check started it on another surface; its ground is the same with and without Round 88, two dry runs; no build left to try it). Express Lanes end to end (`final_express.txt`, rerun `final_express_i75_rerun.txt`): I-575 clean (one wall touch one way), Big Shanty ramp clean both ways, the I-575 flyover into I-75 0.10 s (Round 87 0.76 s); I-75 one run each way faulted and the rerun the other way round (northbound a stop on the Big Shanty merge's bridge structure, then clean; southbound clean, then 1.2 s and wall scrapes at the I-575 merge (508, -3365), Round 87 there 2.5 s and 214 hits): the human-like driver's line varies there. The first Express run's last short lines overlapped the rerun's start by a few minutes (two exes at once, my mistake).
  Worse than Round 87 after 0.88.1 (`compare_crossings.py` of Round 83's 416 runs against 0.88.1's): 2 runs (ramp 879570834 under Greers Chapel, Canton Road at deck 51), plus the I-575 Express flyover (2.2 s, Round 87 0.76 s). The rest of Round 88's list was already clean in 0.88.1 (Frey Road, Big Shanty, Bells Ferry, the Connector interchange, the Express Lanes through Frey Road).
  Flyover: PUT BACK. Deck 80's start (577, -3601) lies on ramp 451950527's line; Round 88 made it flush, so a car on the ramp drove up the deck and fell 15 m off its side (`R89Back` "80s"). Now clean both ways; Phillip Landrum over deck 80 clean both ways too (pictures `89A_before_flyover_deck80.png`).
  Canton Road (deck 51): FIXED, attempt 2. Round 88's laying ran 64 m past the deck's end, over the railroad crossing at 49 m (the rails stood out: `89A_before_canton_51.png` shows the crossing). The laying now stops short of any rail crossing (attempt 1, a 21 m curve: 1.7 s in the air; attempt 2, a 40 m curve and a 6 m ease 2 m short of it): all 4 runs clean.
  Greers Chapel: the ramp's ground is the same with and without Round 88 (two dry runs); Round 83's check started that line on another surface. In these builds the Express Lanes run beside it flew 3 s twice: the exit ramp is cut 6 to 9 m under the lanes and the wall stood open beside it (cars fell in). The wall now stays closed there where the ramp is more than 1.5 m below the lanes (`R89Greers`, within 200 m of (130, -2650) only).
B DONE. 160 street lines added (D137 to D296: 4 highways as said (I-75, I-575, the Express Lanes, the Canton Road Connector), 80 main roads, 76 local streets within 300 m of a course, a hidden cop or a fast-travel spot; the bare "Chastain Road" and "Bells Ferry Road" added to D002 / D004), under 400 so all made: 162 clips (cop voice; "Suspect near..." P16 with its pilot take), 2,700 characters. A street with no line: "Suspect near <the nearest street with a line>". Check (`check_c_final.txt`): Getaway on the Needle 600 to heat 5 at Town Center / Barrett Parkway, Marietta Square, Kennesaw: 71 street calls, all voiced, 70 right; 1 "near" said right at a junction on Church Street Extension (two attempts at that edge, left: tuning limit).
C DONE. 150 more eggs (egg-101 to egg-250, `make_eggs_more.py`): 14 new areas (Town Center inside and out, KSU, the airport, Viking Quarry, Kennesaw Mountain, Marietta Square, Kennestone, Noonday Creek Trail, Addison Heights, the highways, west / middle / Marietta neighborhoods, the woods), 77 easy / 73 hard (yards, woods, parking deck roofs, inside the mall, under highway decks, by jump landings); 5 new fast-travel places (the airport, Kennestone, West Kennesaw, the middle neighborhoods, South Marietta); tiers 10, 25, 60, 125, 190, 250 (10 and 25 kept so a reached tier stays). No new pipe-network reaches exist outside the first map, so no eggs there. Check: every new egg reached by a car and taken in the exe (127 first, 23 moved and 18 taken, the last 5 moved by a road and taken); a copy of Dan's save keeps its 19 eggs and tier 10; the map lists 25 areas, 19 / 250.
Published: `game-8900` (0.89.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-11-01 07:56 (543 files: the 162 new radio clips ship with the game). Disk clean-up: 41.3 -> 41.1 GB (these folders).
For Dan to check: Canton Road over Cobb Parkway toward the railroad; the I-575 Express flyover onto the I-75 Express Lanes; the Express Lanes past Greers Chapel's exit; a chase in Marietta or Town Center (street names); new eggs anywhere south and west.

## Technical notes (standing)
Technical Notes / Blockers

* **The map extension (Round 59).** `python Tools/mapgen/mapgen.py` makes the core (config `bounds`) exactly as before, then, with config `extension`, appends the
  new land (`mapgen_extend.py`): negative x / z, map.json `extent_m` [x0, z0, x1, z1] and `extension.first` (where each list's new entries start), trees appended to
  trees.bin, `Assets/Generated/terrain_full.r16` / `.json` (origin_x_m / origin_z_m) for Unity, `Data/heightmap_ext_1m.tif`. The core's terrain.r16 / terrain.json stay
  for the python scripts. Caches in `raw/`: `osm_ext1.json`, `naip_ext1_*.tif`. Then `python Tools/mapgen/add_extension_places.py` (Town Center and KSU destinations into
  eggs.json; re-run after make_eggs.py). Scripts that pick roads by NAME (route scripts, neighborhoods_rest.py) would now also see the new land's roads (Chastain Road
  runs on west): when re-running one, keep it to `roads[:extension.first.roads]` or check its output's diff. Round 59 check: `ChastainRush.exe -exeRound59 <dir>
  [-r59Only load,fps,drive,pics59] -r59Drives local/round59/r59_places.json` (from `python Tools/mapgen/ext_drives.py`); the races: `-exeRound58 <dir> -r58Only aitable`.
  Round 61, stage 2: config `extension2` (appended after stage 1 the same way: `extend(..., ext, prev_box)` per stage); map.json `extension2.first`,
  `aeroways` (runway / taxiway lines, apron outlines) and `trails` (named paths); caches `raw/osm_ext2.json`, `raw/naip_ext2_*`; stage 2's DEM crop is
  `raw/heightmap_ext2_1m.tif` (too big to commit). Check: `-exeRound59 <dir> -r59Only load,fps,drive,pics59[,fps61x] -r59Drives local/round61/r61_places.json`
  (`python Tools/mapgen/ext_drives.py --round61`; `fps61x`: the frame's cost with far things switched off; pictures in daylight with `-lookOff`). Ground heights at run
  time: `GroundQuery.TileAt` (a tile grid; never search Terrain.activeTerrains per call on this map).

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
