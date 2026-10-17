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

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 38 (Dan, 2026-10-16 21:34; report BUG-001 of session `2026-10-16_21-19-50-140_b75298`, read by Claude, after the
Round 37 exe).** Round 37 is reviewed (Dan: the split-screen AI player 2 is only for testing, "No one is going to be splitting
the screen with AI"; he missed START at first, no change asked). Dan picked this round from Claude's list: "AI stunt rivals,
Life in the world, More inside the stores, The Bank part of stunt course, Smashing through things should count for your
stunts". Parts A to E in order; rebuild into `Builds/ChastainRush/` and commit after each; short results (CLAUDE.md
"Process"). Decide design questions yourself and say what you decided. Racer (`C:\Users\danmo\Racer`) is read-only; port its
finished code, list what came.

Part A, THE BANK IN THE STUNT PARK. BUG-001 (x 3413.02 z 1095.12 h 73, the abandoned bank ahead): "Can we make this bank part
of the course as well because otherwise this ramp is kind of useless the way it is positioned" (Round 37 noted K1's foot is
4.7 m from the bank's west wall, no run-up). Bring the abandoned bank into the park: a way up onto its roof and stunts there
(a launch off it, or a gap to the next roof), a line through it (its boarded windows and door break), and K1 moved or turned
so it has a run-up and leads somewhere (onto or over the bank). The bank stays abandoned-looking (boarded, the FOR LEASE
banner). Check: K1 flown flat out on the Clay Hopper and the Skywhip, the bank's roof reached, the line through it driven;
pictures.

Part B, SMASHING COUNTS FOR STUNTS. Dan: "Smashing through things should count for your stunts". Wherever tricks score (Free
Roam and the Stunt event, every vehicle): breaking something (store and bank windows and doors, the shop row's partitions,
checkouts and shelves, the park's breakable props, fences, mailboxes, lamps, hydrants, pumps, signs, trees that break) adds
stunt points, bigger things more (Dan, 21:37: "fuel pumps should also count...": the gas station's four pumps count, and
   with their explosion they are among the biggest smashes), and links a combo like a trick (on the ground within the combo gap, it keeps the chain
going); a smash in mid-air counts as a trick type of its own for the flight's multiplier ("SMASH"). Shown on the trick panel
("SMASH: STORE WINDOW +150"). The smash runs (Round 36 B) keep their own scoring. Decide the points and say them. Check: a run
through the store windows into a jump, scored with the smashes in its combo; a mid-air smash raising the multiplier.

Part C, MORE INSIDE THE STORES. Round 36 A2b built the supermarket's inside run; the shop row's shops are empty shells
("Next could be: shop interiors, more inside lines"). Give the shop row interiors (simple, readable rooms that fit each
shop's made-up name: counters, racks, tables, chairs, that break) and lines through them: from shop to shop through the
partitions, a ramp up and out through the roof or a back wall, a kicker out through the front window into the park; and the
bank's inside (Part A) with its vault door to burst through. Same reset as the windows (they come back when the player leaves
or resets). Check: driven through end to end on a stunt vehicle; pictures; frame rate inside (1280 x 720) before / after.

Part D, AI STUNT RIVALS. Dan asked "is there the ability to play ai on the stunt course?" (now: no; the split-screen AI
player 2 is the traffic driver, which in BUG-001 "wandered off the course and is just continually running into the wall").
Port Racer's stunt rivals (`RivalTricks` in `StuntRace.cs`, ArcadeVehicle's AI air steering, the rivals' scoring by the same
rule as the player, the AI's mistakes and bails) to the stunt park: the Stunt event gets rivals (a row in its setup: None /
1 / 2 / 3, and difficulty), riding stunt vehicles through the park on lines Code authors (every big feature, the roofs, the
stores, the bank), doing tricks at the launches and smashing as the player does (Part B); live standings on the HUD and a
results table at the end (the player's Top 10 stays the player's own, as Racer). The rivals stay in the park, never out on
the roads, and follow CLAUDE.md "The AI is not perfect". The split-screen AI player 2 in the Stunt event and the Stunt park
uses this stunt AI too (fixes BUG-001's wandering). Check: a Stunt event with 3 rivals run in the exe: every rival scores, none
leaves the park or sticks on a wall; a picture of rivals mid-trick and of the results.

Part E, LIFE IN THE WORLD. Port Racer's ambient life to this map (as Racer has it; leave behind what is tied to Woodstock
places and Racer's scripted Dan / Kyle scenes): `AmbientLife` (residents in yards, driveways, on porches, at the pool at
Chastain Lakes and the Enclave, walking at the shops, doing their actions), `HouseholdSchedule` (who is out depends on the
time of day; fewer at night, none out in rain or snow if Racer does that), `Wildlife` (the kinds of animals Racer has that
live in Georgia suburbs and woods: deer, rabbits, squirrels, birds; in the woods along Noonday Creek and the green spaces),
`WoodlandAmbience` (the woods' sound), `UndergroundLife` (in the pipe network as Racer has it in its storm drain). Nobody and
nothing stands on a road, a race line, a jump or a shortcut; they react as in Racer when a vehicle comes close; never in a
race's way. Day / night and weather aware (Round 35). Same cost rules (a pool round the player, culled out of view); frame
rate at Canton Road and on a Glen street before / after (1280 x 720). Check: pictures of people in the Glen, at Chastain
Lakes and the shops; animals in the woods; a vehicle driving past them.

For Dan to check (when done): the bank in the park, smashing in your combos, the shop and bank interiors, a Stunt event with
rivals, people and animals around the map.

**Round 38 status (Claude, 2026-10-17): built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.**
Evidence: `Data/screenshots/round38/` (`abcd_check.txt`, `e_check.txt`, `38_*.png`, `park_layout.png`). Check: `ChastainRush.exe -exeRound38 <dir>`.
A DONE. The bank is the park's: hollow, boarded doors (west) and drive-up bay (east) that break, a line straight through it from the bus gap's run-out.
   K1 turned round as the BANK HOP: an aimed pop-up (17.2 m/s at 62 deg) 10.7 m from its wall onto its roof, 2.3 s up; the BANK DROP off the roof's north
   edge (16 m/s at 10 deg) onto a 38 deg landing in the lane between the canopy and the half-pipe. Clay Hopper and Skywhip: hop, roof, drop, landing upright.
   Decided: parapet 1.1 m (0.6 m on the hop's side); a berm along the east edge was tried and dropped (it threw a fast vehicle off the roof); the 5th
   container moved out of the bay's run-out.
B DONE. A smash links the combo like a trick (a burst within 1.2 s is one link), keeps the 5 s chain, and in the air is SMASH, a trick type for the
   multiplier (a smash in the last 0.3 s before a takeoff counts in the flight). Points: fence 25, furniture 60, mailbox / sign 75, display 80, shop wall /
   lamp / washer 100, counter 120, window / boarded door / floodlight 150, hydrant / teller counter 200, tree 250, cash cart 300, gas pump 600, vault
   door 1,000. Check: in at a window, through the shops, out of the pizza window: SM · SM · SM; with a backflip BACKFLIP + SMASH x2.
C DONE. 77 breakable things in the ten shops (each its own room: coffee tables, boxes, a wok counter and lanterns, salon chairs and mirrors, pizza
   counter, dental chair, clothes racks, flower tables, washers); the shop ramp (37 m, 16 deg) from the supermarket's north end up through a hatch
   onto the roof gap's run-up; the pizza window kicker (aimed, out through a 3.6 m window onto a sized landing); the bank's teller counter, vault
   (round door) and cash carts. Frame rate inside, 1280 x 720: no change beyond the run-to-run noise (272 / 277 / 317 fps without, 265 / 280 / 302 with).
D DONE. Stunt event setup: Rivals None / 1 / 2 / 3 (default 3) and Easy / Normal / Hard (settings.json). Rivals ride 9 authored lines through every
   big feature (not the car gap: too short a run-up), Racer's tricks and bails, the player's scoring and smashes, live standings on the event panel,
   everyone in the results (never on the Top 10). 180 s check round: EMBER 13,135, GOLD 9,573, BLUE 5,982; none outside the park; 1 to 4 resets each.
   Split screen's AI player 2 rides the same lines in the park and the event (BUG-001), 0 s outside the park.
E DONE. Racer's ambient life on this map (`Scripts/World/Life/`, placed by `MapBuilder.Life.cs`): 1,913 groups of people (porches 1,537, yards 278,
   drives 82, the Lakes' and the Enclave's pool decks 11, the shops' walks 5), lent from a pool of 36 figures to the groups near the view, out of sight;
   Racer's football trio, coffee pairs, walkers. Who is out: new LifeSchedule (Racer's HouseholdSchedule is its Dan / Kyle visits): fewest at night,
   nobody in yards, drives or pools in rain or snow. 3,759 habitats for Racer's songbirds, squirrels, deer, coyotes, turkeys (in threes) and frogs,
   up to 10 out near the view, by their hours; they flee as Racer's. Woods' wind and birds by the tree cover; rats at 6 network entrances, drips in 23
   chambers. Kept 5 m off race and shortcut lines (the street's middle), 30 m off jumps, off pavement, the park, water. Decided (new): people turn to
   watch a vehicle and step out of the way of one coming at them (no colliders). Check: the Glen 5 groups out at noon, 0 at 2 am; one stepped aside;
   a songbird fled; frame rate at Canton Road and a Glen street the same with and without it (269 / 259 vs 295 / 256 fps, run-to-run noise; its
   scripts 0.01 ms a frame). Thin: only 5 groups on the shops' walks (the walks are narrow strips against the stores).
Imported from Racer: RivalTricks (into StuntRival), ArcadeVehicle's AiAir, AmbientLife, Wildlife, WoodlandAmbience, UndergroundLife, TurkeyVoice, the
   wildlife recordings. Left behind: HouseholdSchedule (its Dan / Kyle visits), SnowScenes, ScenePeople, the cave bats, the smoking pair.
Also fixed: the other neighborhoods' mailboxes had the Glen's asset names, so 7 mailbox cells lost their mesh and could not break.
For Dan to check: the bank hop and drop, smashing out of the pizza window, the shop rooms and the shop ramp, a Stunt event with 3 rivals, people in the
   yards / at the pools / at the shops and animals in the woods, at 3840 x 2160.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-17 01:32 (BUILD_INFO.txt).

## QUEUED rounds
None. Held by Dan (2026-10-16 21:34), not to start until he says: the CAMPAIGN ("I want to hold off on campaign until we get
more tracks ... we will have to keep reorganizing it otherwise"); a REVERSE of the Chastain Ridge race ("need to think how to make it work").

## Previous delivery 1 (newest): Round 37
**Reviewed: driven by Dan 2026-10-16 21:19 (split screen and the stunt park; his notes are Round 38).**
**Round 37 (Dan, 2026-10-16 17:05; reports BUG-001 to BUG-003 of session `2026-10-16_16-59-30-505_bc23a8`, read by Claude,
after the Round 36 exe: free roam in the stunt park on the Skywhip 250, at night in the rain).** Round 36 is reviewed. Dan: "I
don't know where to find the fire hydrants so I will report if i have issues later as i come across the special ones" (no
change to Part D; no hints for them). Parts A and B in order; rebuild into `Builds/ChastainRush/` and commit after each;
short results (CLAUDE.md "Process"). Decide design questions yourself and say what you decided.

Part A, THE STUNT PARK'S RAMPS SLOW YOU DOWN. Standing rule (Dan, Round 12): ramps must not slow the vehicle's progress.
BUG-001 (x 3315.82 z 1131.96 h 307, the teal ramp ahead): "ramp slows". BUG-002 (x 3367.30 z 1183.43 h 53, at the loop by
the shop row): "Loop slows on both sides". BUG-003 (x 3330.24 z 1168.18 h 268, the green ramp ahead): "Green ramp slows".
Find why (a step or seam where the ramp meets the floor, the ramp's collider or physics material, the aimed-launch speed cap
of BigJump on these ramps, the loop's own steering, the grip in rain reading a ramp as grass, the Skywhip's profile) and fix
it for every ramp in the park, the loop's way in and way out (both sides) and the roof and inside ramps too, since all were
built the same way: a vehicle keeps its speed onto and up a ramp, losing only what the climb itself takes. Check in the exe:
each of Dan's three, the loop both ways, and every other launch in the park flown flat out on the Skywhip 250 and the Clay
Hopper, dry and in rain; speed at the foot vs. at the lip against what gravity alone would take (list them).

Part B, SPLIT SCREEN FROM WOODSTOCK RUSH. Dan: "Lets go ahead and add split screen". Port Racer's split screen
(`SplitScreen`, `SplitHud`, `SplitRoam`, `RaceMenus.Split`; Racer at `C:\Users\danmo\Racer`, read-only; prefer porting, list
what came), as Racer has it (sequel notes):
B1. Two players, top / bottom by default or left / right (a setting); each controller its own player (keyboard with player 1
   as Racer); player 2 can be the AI so Dan can test it alone.
B2. Races for two (every route, both directions, with or without AI rivals and traffic), Free Roam for two, the Stunt Park and
   the Stunt event for two (each player their own score, Racer's split-screen stunt rules), the Free Roam activities for two
   (each their own attempt), the Underground race for two.
B3. Each view its own HUD (lap, checkpoints, minimap, trick panel, Free Roam clock), its own camera view (the view only changes
   when that player changes it), its own resets, its own garage pick and rider look (player 2's rider look and vehicle saved
   apart). One shared radio either player controls (Racer); one day / night and weather for both.
B4. Names: the Top 10 takes each player's name as Racer does in split screen (player 2 asked for a name with the on-screen
   keyboard on their own controller); the finish panel shows both.
B5. Traffic, geese, breakables and the rest of the world work round both players (traffic spawns and despawns out of both
   cameras' sight).
B6. Menus: a Split Screen entry on the title menu as Racer has it, controller first (CLAUDE.md), B back.
B7. Cost: frame rate split screen in Free Roam at Canton Road by the shops and in a Chastain Ridge race with traffic, both at
   1280 x 720 (reported as not measured at 3840 x 2160); Dan's target from Woodstock Rush is 60 fps split screen on his PC.
Check in the exe by script with two virtual pads (and player 2 as the AI): a 1-lap race for two finished by both, Free Roam
for two, the stunt park for two with a trick each; pictures of both layouts.

For Dan to check (when done): the park's ramps and the loop at speed; split screen (both layouts, a race, Free Roam, the stunt
park, player 2 as the AI).

**Round 37 status (Claude, 2026-10-16): Part A built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.**
Evidence: `Data/screenshots/round37/` (`a_check_after.txt`, `a_check_before_round36.txt`, `a_ramps_*.csv`). Check: `ChastainRush.exe -exeRound37 <dir> -r37Only parkramps,parkloop`.
A DONE. Cause: the park's ramps, landings, tables, loop and inside ramps are mesh colliders named "Stunt park ...", never registered with
   `VehicleSurfaceContacts` (Round 10's fix covers the "sc..." shortcut ramps), so a stunt vehicle's body met PhysX's edge normals, leaning back
   against the travel. Fixed for every park surface, plus what the check found next: on the loop and the 6 m half-pipe / quarter-pipes the body's
   contacts slide without friction and the loop uses its own inward normal; the wheels push square to a park surface; the roofs' and store floors'
   box seams no longer catch a body (8 m/s lost at BIG AIR's foot); and an arcade carry (as RampHold's landings): on the park's ramps with the
   throttle held, and round the loop, a vehicle keeps its speed but for the climb (a knock over 3 m/s, a crash, is never given back).
   Dan's spots on the Skywhip, 30 m/s at the foot: drop landing 18.2 m/s on top before (gravity alone 28.6), tower landing 22.2 (27.8); after,
   every launch, landing, table, pipe, roof and inside ramp on both stunt vehicles, dry and rain, ends within 0.3 m/s of gravity alone. The
   loop: 10.6 m/s over the top before (gravity 29.5), 29.7 after, and out as fast as in; driven backward (the way out) 2 runs of 8 exit 1.9 m/s
   slower or take one knock at the top (given back in the next step).
   BIG AIR, the shop row drop, the roof gap and the tower gap still leave at their designed speed (Round 36's aimed launches, so they land on
   their slopes); that cap is at the lip, not on the ramp.
   For Dan to check, not changed: K1's foot is 4.7 m from the abandoned bank's west wall, so it has no straight run-up (not measured).
**Part B built and checked in the exe by script (two virtual controllers, minimized 1280 x 720, muted; 12 of 12 checks), not yet driven by Dan.**
Evidence: `Data/screenshots/round37/` (`b_check.txt`, `b_fps_runs.txt`, `37b_*.png`). Check: `-r37Only splitmenu,splitrace,splitai,splitroam,splitpark,splitevent,splitpics,splitfps`.
B1 DONE. Top / bottom (player 1 on top) or left / right (the setup's Screen row, settings.json); each controller drives its own player only
   (the keyboard goes with player 1); "Player 2: AI driver": Racer's race AI in a race, the traffic driver cruising the roads in Free Roam.
B2 DONE. Race (any route either way, AI rivals as race setup, traffic as Settings > Gameplay; the Underground race is a route like any), Free Roam,
   Stunt park (Free Roam from the park's start line) and the Stunt event for two (Racer's riders: each their own points). Activities: jumps,
   speed traps and tricks score for each; nothing written to the activity records (Racer). Check: race 1:15.5 / 1:21.0, both finished;
   AI player 2 finished; a backflip each off BIG AIR (1,397 / 1,378 pts); the event for two.
B3 DONE. Each half has its own HUD (lap, place, times, next checkpoint, messages, Free Roam's day and clock, the other player's direction and
   distance), minimap, trick panel, camera view (X / V on their own controller; player 2's remembered apart), resets and garage (player 2's
   vehicle and color in `vehicle2.json`, rider in settings.json). One radio for both, one day / night and weather (player 2's view gets its own
   rain, snow, stars, moon, mist, as Racer).
B4 DONE. Top 10s under each player's name (laps, races, combos, the Stunt event); player 2's name typed on their own controller; the results show both.
B5 DONE. Traffic comes and goes out of both views and round both players; junctions wait for both; geese answer the nearer player; the pipe
   network is drawn round both views.
B6 DONE. SPLIT SCREEN on the title and Free Roam menus; player 1's controller works the setup; A or Start on another controller joins; either
   player's Start pauses; B goes back.
B7 MEASURED, target NOT MET in these runs. The PC was busy (50 to 75 % CPU from OBS, Chrome and Task Manager), so the runs vary; not measured at
   3840 x 2160. Free Roam at Canton Road: split 74 / 37 / 65 fps vs one player 121 / 84 / 81. Chastain Ridge race with traffic: split 34 fps vs one
   player 38 (and 20 vs 32). Racer's split-screen savings are in (detail 0.6, shadows 0.6, 1000 m far plane).
Decided: the Stunt event with player 2 as the AI is player 1's alone on one view (Racer); split screen keeps the race setup's own settings; player
   2 starts the race beside player 1 on the grid.
Imported from Racer: see CLAUDE.md (Round 37). Not built: Racer's winner shot, Police Chase, Speed Patrol, Getaway, Stunt Races (none are in this game).
For Dan to check: split screen at 3840 x 2160 (F2 shows the frame rate); both layouts, a race, Free Roam, the stunt park, player 2 as the AI; the
   Underground race for two (not run by script).
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-16 19:48 (BUILD_INFO.txt).

## Previous delivery 2: Round 36
**Reviewed: driven by Dan 2026-10-16 16:59 (the stunt park; his notes are Round 37).**
**Round 36 (Dan, 2026-10-16 10:36, after the Round 35 exe: "All is good").** Round 35 is reviewed (Dan did not check the
Underground race's start line; he will report it if it is wrong). Parts A to D in order; rebuild into `Builds/ChastainRush/`
and commit after each part; short results (CLAUDE.md "Process"). Decide design questions yourself and say what you decided.
Racer is read-only; port its finished code (CLAUDE.md), list what came.

Part A, THE KROGER LOT BECOMES A STUNT PARK, WITH WOODSTOCK RUSH'S STUNT VEHICLES. Dan: "change the Kroger parking lot into a
stunt course (we could import the stunt vehicles from Woodstock Rush and make a bigger version of the stunt course where you can
get up on top of the building area with stunts up there. Would like to get big air on these".
A1. Racer's stunt vehicles and stunt play, ported: the Clay Hopper, the Skywhip 250 and the Clay Hopper Cup (Racer
   `Tools/Blender/stuntbike.py`, `stuntbuggy.py`, their profiles, air steering in ArcadeVehicle); the trick scoring
   (`ArcadeActivities.Stunts.cs`: flips, rolls, spins, the loop and barrel-roll tricks, landings and bails with Racer's 1.5 s
   recovery window, the multiplier per trick type, the 5 s combo chain), `TrickHud`, the Stunt event (`StuntEvent`) and Top 10
   Best Combos (`StuntProgress`, on the Records page with the other boards). The garage as Racer: stunt vehicles shown when the
   garage is opened for the Stunt event; how Racer offers them in Free Roam, the same here. Leave Stunt Races and the Stunt
   chapter behind (no campaign here) unless the Stunt event needs a piece of them.
A2. The park: Racer's Red Clay Stunt Park (`StuntPark.cs`) is the starting point, made BIGGER and laid on the Kroger center's
   lot (`MapBuilder.ShoppingCenter.cs` LotRing, about 260 x 190 m round the supermarket at x 3270, z 1150 and the shop row
   along its north). Dan (10:57): "The course should seem like someone specifically designed it for the purpose of being a
   stunt course", "it is meant to be inspired by the stunt course in Rush USA" (the stunt arena of Rush 2: Extreme Racing USA).
   So THIS PARK IS THE EXCEPTION to the "ramps look like something that could be there" rule: a purpose-built stunt course,
   built ramps and structures (steel and plywood ramps, painted kickers and tables, quarter-pipes, a half-pipe, the loop, a
   corkscrew / barrel-roll ramp, towers and decks, gaps over cars and a bus), bold course colors, banners and arrows painted as
   a stunt park's own, lit at night. Inspired by Rush 2's arena (its spirit: big ramps everywhere, lines that chain one stunt
   into the next, levels to climb, a lot of air), copied from none: our own layout and look (Racer's rule). The lot's stalls
   and paint go. Every ramp still the size its job needs and landing on a slope sized to its launch (A3).
A2b. INTO THE STORES (Dan: "I could also see smashing through the store windows and having stunts inside as well"): the shop
   row's and the supermarket's front windows break when driven through (glass that shatters, as the other breakables; they
   come back when the player leaves the area or on a reset, so the park works again), and inside at least the supermarket a
   stunt run: aisles turned into lanes, kickers and a ramp up to a mezzanine or out through a back wall or skylight onto the
   roof (A3). The shops' interiors are new (the buildings were shells): simple, readable, enough to drive through. Dan: "We
   can start with something and build from there": build a first version that works end to end and say what could come
   next.
A3. UP ON THE ROOFS (Dan: "get up on top of the building area with stunts up there"): ramps up onto the supermarket's roof
   and the shop row's roof (the buildings stay, their fronts as they are), a stunt area on the roofs (kickers, a gap between
   roofs, quarter-pipes on the parapets), and big drops / launches off the roof edges onto landings in the park below. BIG AIR
   (Dan: "Would like to get big air on these"): the biggest launches give several seconds of air for two or three trick types
   with a stunt vehicle; every jump lands on a down-slope sized to its launch (Racer's ballistic sizing, Tools/Report106
   size.py); one mesh per ramp profile, no seams (Racer's lessons).
A4. Keep: the gas station and its breakable pumps, the abandoned bank, Canton Road and the other roads as they are; the Chastain
   Ridge race's line on Canton Road and its S1 shortcut untouched (protected geometry; the park's edge kept off the road, a
   tire wall or barrier where it meets a road, openings where its drives were). The world / game rule: the park and its ramps
   are world; the Stunt event, its start and its scoring are game layer.
A5. Free Roam: driving into the park scores tricks as Racer's park does (a flight that takes off in the park counts wherever it
   lands); the Stunt event starts at its entrance as in Racer. On the map.
Check in the exe by script: the biggest launch and a roof launch flown flat out on a stunt vehicle (air time, height, landing);
a backflip + roll landed and scored; the Stunt event run once; pictures: the park from above, the roofs, each big feature,
the trick panel. Frame rate at the park (1280 x 720) before and after.

Part B, FREE ROAM EVENTS FROM WOODSTOCK RUSH. Dan: "We can probably add free roam events." Port Racer's free roam activities
(`ArcadeActivities` with jumps, speed traps and smash, `ActivitySite`, `ActivityRecords`, the medals and their displays, the
HUD lines and prompts; leave the acorns and anything tied to Woodstock places behind). Code picks the sites on this map and
lists them: jumps on the existing big jumps and ramps (the Noonday Creek jumps, the Ridge leap, shortcut ramps, the stunt
park's biggest), speed traps on the long fast roads (Canton Road, New Chastain Road, Chastain Road, Bells Ferry Road), smash
runs where breakables stand in a row (mailboxes, fences, the gas pumps). About as many of each as Racer's map had for its
size; each with gold / silver / bronze targets from a scripted run; on the map and the minimap as Racer shows them. Free Roam
only, never in races. Check: one of each kind run in the exe by script, scored and medaled; a picture of the map with the
sites.

Part C, MORE FIRE HYDRANTS. Dan: "We should have more fire hydrants around in general." Breakable hydrants (Racer's, squirting
water, as the Glen's) along every street of the map, at real spacing: about every 150 m on neighborhood streets, about every
100 m on the commercial roads (Canton Road, the shopping streets), at street corners and in front of the schools, churches
and businesses; at the curb, never in a drive, a lot entrance, a shortcut's line, a jump or a race line. Same LOD / batching
as the other street furniture. Say how many. Check: two pictures (a neighborhood street, Canton Road) and the frame rate at
Canton Road before and after.

Part D, HYDRANTS TIED TO THE UNDERGROUND (Dan: "I also feel like the fire hydrants should maybe somehow be tied into the
underground area"; of the ideas Claude offered he picked these two, 10:57: "1 and 2 are kind of what I was thinking so lets go
with those"):
D1. SECRET ENTRANCES: a few hydrants (one beside each street manhole MH1 to MH13, and others where a new manhole fits over a
   network pipe or junction) are rigged: knock one over and the nearby manhole's cover blows off (a bang, water, the cover
   flying) and the way down into the network is open, as the network's manholes already lead in. Nothing marks which hydrants
   are rigged beyond the manhole being near (found, not signposted). The cover is back after a while or a reset.
D2. GEYSERS: breaking a hydrant sends a column of water up out of a manhole or storm grate down the street for several seconds;
   driving into it launches the vehicle high (big air, tricks count as anywhere in Free Roam), roughly upright, landing on the
   street. At least one geyser per neighborhood with hydrants, and some along Canton Road.
Free Roam only; in races hydrants break as before, no cover blows and no geyser (the races' pipes stay sealed). Dan (11:01): "free roam only for now, but this concept could be used as a shortcut idea in races": so build both so a race
   route could switch one on later (a rigged hydrant opening a manhole into a pipe shortcut, a geyser launching over something),
   but use none in a race now. World: the
hydrant-to-manhole links; their effects run in Free Roam. Check in the exe by script: one rigged hydrant knocked over, the
cover off, the vehicle drives down into the network; one geyser launch (height, air time, landing); pictures of both.

For Dan to check (when done): the stunt park and its roofs on the Clay Hopper and the Skywhip, the Stunt event and its Top 10,
smashing into the stores and the run inside, the free roam events, the hydrants, a rigged hydrant and a geyser.

**Round 36 status (Claude, 2026-10-16): Part A built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.**
Evidence: `Data/screenshots/round36/` (`a_*`; layout `a_park_layout.png`). Data: `python Tools/mapgen/make_stunt_park.py` (`Data/tracks/stunt_park.json`), built by `MapBuilder.StuntPark.cs` / `.StuntParkStores.cs`.
A1 DONE. The Clay Hopper, Skywhip 250 and Clay Hopper Cup with air steering; Racer's trick scoring, trick panel, Stunt event (10 min, combo overtime) and both Top 10s
   (Records page). Decided: all three stunt vehicles offered at once (no unlocks here), in the Free Roam garage and the event's; never in a race (a race start
   puts the Street Classic on) or the AI's picks. Check: a backflip + roll off BIG AIR on the Clay Hopper, 4.3 s up, 4,498 pts, Top 10 #1; the event run once.
A2 DONE. "Chastain Corner Stunt Park" (a made-up name): the floor graded to one plane (the lot's 2 % rise kept, level by the store fronts), stalls and paint gone,
   painted pads; K1 kicker, K2 table, barrel roll, bus gap, car gap (5 wrecks), half-pipe, the loop, BIG AIR's landing hill, a tower, catch fence and tire wall
   (openings at the 4 drives), floodlights (lit at night), boards. The knoll by Chastain Corner left as it is, inside the fence. Gas station and bank unchanged.
A2b DONE (first version). 43 store windows and 44 shop-row partition panels break and come back (reset in the park, or 160 m away); inside: checkouts that
   break, 8 shelf aisles with kickers, a mezzanine with a drop-in, a ramp up through a skylight onto the roof. Next could be: shop interiors, more inside lines.
A3 DONE. Roof ramp (42 m, 12 deg); BIG AIR off the supermarket's front, shop row drop, tower gap, roof gap over an atrium (aimed launches, sized landings);
   roof quarter-pipes and a table. Check: BIG AIR 3.0-3.1 s up on both stunt vehicles; every launch lands upright on its slope; inside and roof ramps reach the roof.
A5 DONE. Free Roam scores tricks everywhere (a flight off the park counts wherever it lands); the Stunt event's prompt at the entrance; the park on the map and minimap.
A4 DONE. The Ridge race's line, S1 and Canton Road untouched (grading stops 1 m off every road outside the lot). Frame rate (1280 x 720, not 3840 x 2160) at Round 35's
   Canton Road spot: before 315 fps, after 284-295; in the park 268-275; inside the store 246-278.
Imported from Racer: StuntPark (its ramp, loop and prop builders ported into the map build; its queries at run time), ArcadeActivities (+Stunts, +Smash), ActivitySite,
   ActivityRecords, StuntEvent, StuntProgress, TrickHud, MedalUi, DisplayUnits, the stunt profiles and Cup livery, air steering, RaceMenus.Stunt (rewritten into
   GameMenus), StuntBike.fbx, StuntBuggy.fbx, Tools/Blender stuntbike.py, stuntbuggy.py, bikes.py. Left behind: Stunt Races, the Stunt chapter, split-screen.
**Part B built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round36/b_*`. Sites: `python Tools/mapgen/make_activities.py`
   (`Data/activities/free_roam.json`, targets `Data/activities/targets.json`), built by `MapBuilder.Activities.cs` under their own game-layer root.
B DONE. 17 sites: 8 jumps (BJ1, BJ3, BJ4, P1J, the Ridge leap, S2, BIG AIR, shop row drop), 5 speed traps with camera posts (Canton, New Chastain x2, Chastain,
   Bells Ferry; both ways), 4 smash runs (12 mailboxes, the 4 pumps, 30 shop-row panels, 10 street lamps). On the map and minimap; Start > ACTIVITIES; results on
   the Records page; Free Roam only. Targets: speed traps 95 / 85 / 72 % of each base vehicle's flat-out run; jumps from their sized flights. Check: Bells Ferry
   123 mph GOLD, BIG AIR 226 ft SILVER, pump smash BRONZE, the map picture. Fixed in the port: a landing is judged upright against the slope, not world up.
   Seen in the scripted runs, not changed: on Chastain Road near (300 to 400, 940) and New Chastain Road near (2607, 691) the scripted drive stopped dead.
**Parts C and D built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round36/36c_*`, `36d_*`, `c_*`, `d_*`. Built by `MapBuilder.Hydrants.cs`.
C DONE. 529 more hydrants (568 on the map): 307 along the streets (150 m, 100 m on commercial roads), 153 at corners, 39 in front of schools, churches and
   businesses, 30 for D. Kept off pavement, drives, lots, race and shortcut lines, jumps, trails, the park, water and pipe cuts. Same breakable chunks as the Glen's.
   Frame rate at Canton Road (1280 x 720, not 3840 x 2160): 304 fps before, 293 to 311 after.
D1 DONE. 11 of the 13 street manholes have a rigged hydrant (4 of them the Glen's own); MH at (2966, 1091) and (2010, 935) have no free curb spot. Knocked over:
   a bang, water, the cover flies, the hole stays open 45 s (a reset closes it). Decided: no new manholes (each needs a new riser into the network). Check: the hydrant
   at (1552, 1272) knocked over, the car drove down into the pipe 7.7 m under the street.
D2 DONE. 23 geysers: one per neighborhood with hydrants, the Glen, 3 on Canton Road; a grate in the gutter, its hydrant 22 m up the street. Check: 13.4 m up,
   3.1 s in the air, down on the street 43 m on. Both kinds Free Roam only; each link has `inRaces` (off) so a route could switch one on later.
   For Dan to check: while a hole is open, the street round it shows the grass under it and small gaps (the same look as a burst from below since Round 25).
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-16 16:38 (BUILD_INFO.txt).

## Previous delivery 3: Round 35
**Reviewed: passed by Dan 2026-10-16 10:36 ("All is good"; the Underground start line not checked yet).**
**Round 35 (Dan, 2026-10-16 08:00; report BUG-001 of session `2026-10-16_07-49-24-735_80f94d`, read by Claude, after the
Round 34 exe: an Underground race, finished 3rd, Top 10 working).** Round 34 is reviewed. Parts A to C in order; rebuild into
`Builds/ChastainRush/` and commit after each part; short results (CLAUDE.md "Process"). Decide design questions yourself and
say what you decided in the status.

Part A, THE UNDERGROUND RACE'S FINISH LINE ON A STRAIGHT. BUG-001 (x 1933.29 z 1171.30 h 262, THE HUB floor, at the finish
panel). Dan: "My first complaint was that the finish line is after a hairpin turn. The finish line needs to be on a straight
stretch." (His Round 28 B2 complaint; Round 28 moved the turn-round to the mid ledge, four 90 degree corners, and put the line
13 m up the span, so the line still comes right after the turn.) Put the line (and the start: they are one line) on a
straight stretch of the lap with straight road before it, enough that a racer is lined up and at speed when crossing, e.g.
in a straight run of a pipe; the grid off the racing line as now, or behind the line on that straight, wherever it fits.
Checkpoints, shortcuts' forgiveness and resets as before. Dan's records for the Underground race stay (the lap is the same
loop; only where it is cut changes); say if the lap length changed. Check: a picture of the line from 30 m before it, a
scripted 3-lap race with the AI.

Part B, THE RADIO CUTS OUT. Dan: "there is something wrong with the radio. It seems to cut out especially in races. in the
underground network it didn't play at all." (Round 34 B fixed the song skipping on focus; this is different.) Find why: the
likely cause is Unity's voice limit (more sounds than real voices: AI engines, traffic voices, the pipes' streams, waterfalls
and the hub's water, breaks), dropping the radio's voice because nothing marks it as the most important, or something in
the pipes (a reverb / low-pass zone, the pipe camera, a mute meant for something else). Fix: the radio plays everywhere and
in every race, the underground included, never dropped or muted by other sounds (its voice top priority, 2D; the other
sounds share what is left, as Racer's voice budget does); it goes on through pipes, warps and the express, resets and race
start / finish. Check in the exe by script: a whole Underground race and a whole Chastain Ridge race (traffic Normal) with
the radio on, logging every frame whether its voice is really playing (not virtual); no gap longer than the song changes.
(Sound on only as needed; muted otherwise: CLAUDE.md "Test quietly".)

Part C, DAY / NIGHT AND WEATHER FROM WOODSTOCK RUSH. Dan: "Lets throw Day/night cycle and weather into this round. Use the
same system as Woodstock Rush." Port Racer's system as it is (CLAUDE.md: prefer porting, list what came): `WorldLook`,
`WeatherSky`, `WeatherEffects`, `SkyClouds`, `MoonIcon`, `SnowScenes`, thunder and lightning, `WeatherGrip` (rain, snow and
ice grip on paved / dirt / grass, with its setting to turn it off), and their settings and menu rows as Racer has them
(Free Roam's clock, 1 minute = 1 hour; how a race picks its time and weather; whatever Racer's race setup offers).
C1. Night: the sky, the moon and stars as Racer; vehicles' headlights on (VehicleLights); street lamps and the lots' light
   poles lit; whatever else Racer lights at night (windows, signs) if it has it.
C2. Weather: rain, snow, storms with thunder and lightning, wet / snowy ground and grip as Racer. Never inside the pipe
   network, junctions or the hub (no rain or snow falling underground, the lighting there unchanged by the sky), nor under a
   roof or a bridge if Racer handles that.
C3. World vs. game layer (CLAUDE.md): the day / night and weather are world; a race's chosen conditions are game layer.
C4. Cost: frame rate in free roam at the busiest place (Canton Road at the shops) by day, at night and in rain, before and
   after (1280 x 720, reported as not measured at 3840 x 2160).
Check in the exe by script: pictures of the same three places (Dan's house street, Canton Road at the shops, the hub) at noon,
dusk, night, in rain and in snow; a race started in rain shows lower grip on the HUD / handling as Racer; the setting off
gives full grip.

For Dan to check (when done): the Underground race's new line, the radio through a race and underground, a day in free roam,
rain, snow and a storm, a race at night.

**Round 35 status (Claude, 2026-10-16): built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.
Build 2026-10-16 10:02.** Evidence: `Data/screenshots/round35/` (`a_*`, `b_*`, `c_*`; street pictures in `local/round35/`, not committed).
A DONE. The line (the start: one line) is 95 m into M5 from the hub, on its 80 m straight: 62 m of straight pipe before it (heading
   within 0.1 degree), 19 m after (`make_underground_route.py` `gate_s` 139.8 m). Same loop, cut elsewhere: lap length unchanged
   (1370 m); records kept. Drawn as a checkered hoop round the pipe (route `gate_ring_m`). The grid stays on the mid ledge (202 m run-in).
   Decided: finished AI cars drive on to J3 and park along its far wall, off the lap (`finish_park_gap_m`, `_speed`). Check: picture from
   30 m back; 3-lap race, 3 AI, every shortcut taken, no missed checkpoint (`a_underground_race_check.txt`).
B DONE. Cause: the radio's voice had priority 180 (least important) with 32 real voices; the pipes' streams, waterfalls, AI engines and
   traffic (128 to 160) took it. Now priority 0 and past the glass stretches' low-pass (`LocalRadio`). Check, whole Underground and Ridge
   races (traffic Normal), every frame: before, silent for most of the Underground race (40 s gaps) and up to 8 s on the Ridge; after,
   no gap except the song change (`b_radio_*_check.txt`).
C DONE (Racer's system; Dan's look kept by day). Free Roam runs Racer's clock (1 min = 1 hour, 30-day moon, starts day 12 08:00), its
   Weather on the title and free-roam pause menus; RACE SETUP has Time of Day (Dawn / Day / Dusk / Night) and Weather (race_settings.json,
   the game layer); SETTINGS: Display > Lightning flashes, Gameplay > Weather affects grip; top-left Free Roam clock with the moon.
   Night: moon, stars, headlights; street lamps and light poles glow, the nearest 8 cast light (new: Racer has none); the roads now take
   headlights. Rain / snow: falling weather, clouds, thunder and lightning, wet sheen, snow on ground, roofs and trees, ice on ponds,
   grip 85 % paved / 75 % grass in rain (70 / 62 % snow). Underground: no weather, the light unchanged; no wet grip there.
   Check: three places x noon / dusk / night / rain / snow (`c_*.png`); rain race brakes 9.2 m vs 8.2 m, setting Off 8.2 m.
   Frame rate at the shops, 1280 x 720 (not 3840 x 2160): before 285 fps; after day 266, night 218, rain 214 (`c_fps.txt`).
   Decided: Clear Day is this game's day; other looks are Racer's relative to it. Racer's water reflection probes left out (1.4 ms a frame).
   Left behind: SnowScenes (Racer's sled and pool scenes are at Woodstock places, made of its pedestrians), the gates' night glow (unlit
   here already), split-screen, Trailer Mode. Wet / snow times keep their own Top 10 boards (Racer's record tag).
   Imported from Racer: WorldLook, WeatherEffects, WeatherSky, SkyClouds, MoonIcon, WeatherGrip, ArcadeVehicle's grip lines, thunder
   ducking, 4 shaders, 7 materials and the ice texture (`Assets/Scripts/World/`, `Assets/Shaders/Weather/`, `Assets/Resources/`).
For Dan to check: the new line and its run-in; the radio in a race and underground; a day in free roam; rain, snow, a storm; a night race.

## Technical notes (standing)
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
