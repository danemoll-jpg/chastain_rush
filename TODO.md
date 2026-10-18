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

**One map rebuild per round, and a limit on tuning — DECIDED (2026-10-18).** Dan, after Round 41 ran 9 hours (Part A alone
7.5 h of 30-minute Build Map runs while tuning the crypt toward a time target it never reached): (1) a round makes all its
world changes first and runs Build Map and the exe build ONCE, at the end (code-only steps in between use
`-playerBuildKeepScene`); each part still gets its own commit and its own targeted check on that one build. (2) Tuning
limit: if a number the round asks for (time saved, air time, height, speed) is not met after two attempts, stop tuning it,
report the numbers in the status and move on; Dan decides. Optional ideas in Dan's notes ("could", "maybe") are built as
the simple version, not as requirements to tune. Copy both rules into CLAUDE.md's "Process" in the same words.

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 43 (Dan, 2026-10-18 16:25; report BUG-001 of session `2026-10-18_16-02-03-115_cda8a3`, read by Claude, after the Round 42
exe: a reverse Chastain Ridge race on the Drifter Twin).** Round 42 is reviewed. Dan picked Racer features to bring over next
(Claude listed what Woodstock Rush has that this game does not): goose eggs, ghosts, the winner shot, free look / Trailer Mode.
The startup polish (spoken title, loading screen, hints) waits until Dan has cover art. The egg hunt is the biggest and is
Round 44 (QUEUED). The one-rebuild and tuning-limit rules (top of this file) apply. Use the quick builds (CLAUDE.md "Quick
builds") for Part A while working; one full Build Map and exe at the end. Short results.

Part A, DAN'S REPORT.
A1. THE PLUME CAN BE PASSED, AND THE RESET PUNISHES YOU. BUG-001 (x 3282.25 z 580.35 h 333, New Chastain Road, lap 3, CP3 next,
   the plume 176 m ahead; reverse Chastain Ridge): "you can go to the left of this and miss the hydrant and then get stuck in the
   road. The reset will put you well past the location and say you miss 3 checkpoints". Round 40's plume covers only the
   northwest-bound carriageway (lanes, shoulder, median); the other carriageway, on the left, is open, so Dan went past it
   into the blocked-off road beyond and chose to reset rather than turn back; the race reset (Round 39's `reset_skip`) put him
   past the landing and charged 3 missed checkpoints. Dan later (16:29): "I didn't technically get stuck, I guess I could have
   turned around and go back to the plume but thought it would be quicker to reset. Just that someone may be inclined to try
   to avoid the water and might go around and get confused since it is blocked off".
   (a) No way past the plume on New Chastain Road, either carriageway: the simple version is a second cover on the other
   carriageway beside the first, blowing with it, its plume thrown the same way onto the dead end (aimed as now, whatever the
   line in). Code may choose another way that keeps Dan's idea ("it will look like you drive straight but won't matter because
   you can't miss the water plume") and says why. No barriers or arrows. Traffic on that carriageway is not thrown, as now.
   (b) A driver who swerves to avoid the water must not end up past it in the blocked-off road wondering where to go: with (a)
   there is no way around; if Code's way leaves any gap, say where and what a driver there sees.
   (c) Resets: a race reset anywhere from the checkpoint before the plume up to the plume, on either carriageway, the median or
   the ground beside the road, puts the vehicle on New Chastain Road short of the cover, facing it (as the Round 40 rule
   "resets short of the climb" said). Only a reset during the flight itself (launched, not yet landed) goes on past the landing.
   (d) A reset never charges missed checkpoints for the ones its own placement skips (they are forgiven, as a shortcut's are).
   Check: in the exe from Dan's spot, Drifter Twin and Street Classic along the left carriageway and the median at race speed:
   thrown onto the dead end; a reset from the left carriageway past the cover: short of the cover, no penalty; a reset mid-flight:
   past the landing, no penalty. Pictures from Dan's spot after it blows.

Part B, GHOSTS (from Racer `CleanLapGhost.cs`, port it).
B1. A ghost of your best clean lap (no reset or teleport in it, as Racer) on every course and direction this game has (normal
   races and the Underground race; Stunt event courses only if Racer's rules give them one), saved per course, direction and
   vehicle rules as in Racer. Shown see-through, never solid, on or off in Race Setup (Racer's preference, off by default as
   there). Records page: the ghost tab GameMenus.Records left behind comes back. Split screen as in Racer. Check: two laps of
   the Chastain Glen loop, the second against the first's ghost (a picture); a lap with a reset makes no ghost.

Part C, THE WINNER SHOT (from Racer `WinnerShot.cs` and `FinishPresentation.cs`, port them).
C1. When a race is won (player or AI), the camera moment and finish presentation as in Woodstock Rush, before the results.
   Check: a finish picture from a race the player wins and one an AI wins.

Part D, FREE LOOK AND TRAILER MODE (from Racer `CameraViews.FreeLook.cs`, `TrailerMode.cs`, `TrailerMode.Panel.cs`,
   `TrailerRecorder.cs`, `RaceMenus.Trailer.cs`; Round 35 left Trailer Mode behind).
D1. The pause menu's Trailer Mode as in Racer (its camera, slow motion, HUD and guides off, Free Roam conditions, screenshots to
   the Screenshots folder), controller and keyboard. Check: pictures from free look in Free Roam and in a race.

For Dan to check (when done): get past the plume on the left (you can't), a reset there; a race against your ghost; the winner
shot; Trailer Mode.

## QUEUED rounds
**Round 44, GOOSE EGG HUNT (Dan, 2026-10-18 16:25; start after Round 43 is reviewed).** Dan: "lets go with goose eggs. We can
have 100 of them spread around with tier unlocks instead of just one unlock. We can worry about what exactly gets unlocked
later". Port Racer's exploration (`ExplorationCollection.cs`, `AcornAreas.cs`, `AcornNotices.cs`, and the WorldMap parts Round 20
left behind: exploration fog and its save, destinations and travel, waypoints, the collectible areas), with goose eggs in place
of acorns. 100 eggs (a few in a straw nest, or one alone; the look Code's choice) across the whole map: every neighborhood, the
shopping center and stunt park (roofs included), the creek trail, along shortcuts, and in the pipe network; a mix of easy ones
by the road and hard ones that take a jump or a find. Free Roam only (hidden in races). Found by driving through; the count shows
for a few seconds when one is found (as Racer 0.79), the total in the pause menu and on the map by area (not exact spots).
TIERS instead of Racer's one reward: 10, 25, 50, 75 and 100 eggs (Claude's proposal; Dan may change them); reaching one shows a
notice and is saved; what each unlocks is decided later (nothing locked or unlocked by them yet). A restart of the hunt as in
Racer (other saves kept). Check: a pick-up, a tier notice (test save), the map's areas, a travel; pictures of a few eggs, easy
and hard.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- STARTUP POLISH (2026-10-18): Racer's spoken title, loading screen and hints; Dan: "i Would want 5 but we don't have a cover yet".

## Previous delivery 1 (newest): Round 42
**Reviewed: driven by Dan 2026-10-18 16:02 (a reverse Chastain Ridge race on the Drifter Twin; his note is Round 43; nothing else reported).**
**Round 42 (Dan, 2026-10-18 10:30; reports BUG-001 to BUG-003 of session `2026-10-18_10-15-07-747_d1c371`, read by Claude,
after the Round 41 exe: a reverse Chastain Ridge race through the crypt, then Getaway in Free Roam at night).** Round 41 is
reviewed. Dan asked whether to do the queued quick-rebuild round first; Claude's answer: yes, and use his three reports as its
first real fixes. Part A, then Part B; the one-rebuild and tuning-limit rules (top of this file) apply. Short results.

Part A, QUICK AREA REBUILDS (queued by Dan 2026-10-18 06:18, word for word below).

**Round 42, QUICK AREA REBUILDS (queued by Dan, 2026-10-18 06:18: "yes"; start after Round 41 is reviewed).** Today any world
   change, even one house's roof, needs the whole Build Map (about 30 min) because the whole world is generated in one pass.
   Woodstock Rush was also generated from a real map, but once its world was accepted, later fixes and shortcuts were focused,
   repeatable operations on the saved scene (Racer `Docs/CR015/VALIDATION.md`, `Docs/PHASE5_VALIDATION.md`,
   `Docs/VEGETATION_VALIDATION.md`: "focused authoring", "refresh only", the whole-environment rebuild guarded) and took minutes.
   Give this project a fast path that fits its generated pipeline (Code decides how and says why): for example Build Map limited
   to an area (a box in map coordinates: its terrain tiles, buildings, props, road pieces and colliders rebuilt, the rest of the
   saved scene untouched), and / or one object rebuilt by id (a house, a pipe exit, a shortcut's pieces). Rules: the full Build
   Map stays the reference and must give the same result as the area builds that came before it (check by comparing the scene
   outside and inside the box); nothing hand-edited in the scene (edits always go through the generator or its data). Target:
   a small world fix (a roof collider, a ramp on a roof, the ground at a pipe exit) from change to a playable exe in under 5
   minutes on Dan's PC. Check: time a full build, an area build and an object build; show the area build of a real fix gives
   the same scene as a full build. Document the commands in CLAUDE.md "Commands" and use them from then on.

   Order: build the fast path first and its checks; then do Part B with it (not with a full Build Map), timing each fix from
   change to playable exe. One full Build Map at the very end proves the area builds matched it.

Part B, DAN'S REPORTS, made with Part A's fast path.
B1. THE CHURCH'S WINDOWS. BUG-001 (x 3545.85 z 570.53 h 295, at the church's broken front doors, reverse race): "cant the
   window breakout too?" The tall windows beside the doors break when driven into (glass as the store windows, back after a
   reset), and a vehicle going in through one reaches the aisle as through the doors. Check: driven in through a window.
B2. SINKING IN THE AISLE. BUG-002 (x 3532.41 z 578.01 h 268, on the aisle just inside, the stair down ahead): "you sink into
   the floor. You should be able to see the stairs". The vehicle runs below the drawn aisle (only the rider's head shows):
   the drawn floor and the solid floor do not match (Round 41's MeshProbe). Make them one surface all the way from the doors
   down the stair into the crypt, so the vehicle drives on what you see and the stair is in view ahead. Check: a picture from
   Dan's spot and one on the stair, the wheels on the drawn floor.
B3. THE PLUME IN FREE ROAM. BUG-003 (x 3234.92 z 652.88 h 151, New Chastain Road at night, Free Roam Getaway): "thought this
   only come on during the race...i am in free roam". Round 40 decided it blows in Free Roam too; Dan expected race only. The
   manhole plume only in the reverse Chastain Ridge race; in Free Roam the cover stays shut. (Code only: no map build.)
   Check: Free Roam past it both ways at speed, shut; a reverse race, it blows.

For Dan to check (when done): the church windows and the aisle, the plume off in Free Roam; from now on, how fast fixes come.

**Status (Claude, 2026-10-18): Parts A and B built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round42/`.
A DONE, target met for a one-builder fix, not for the church. Build Map is now its planning plus named steps; a full build records each step's
   objects and terrain samples (`BuildCache/steps/`); a quick build reruns chosen steps on the saved scene, then the exe (`MapBuilder.Quick.cs`,
   commands in CLAUDE.md "Quick builds"). Decided: steps, not a map box (the builders are map-wide; a box is nearly a full build). Quick-capable:
   Businesses, ChurchCrypt ("church"), Ridge, Pipes. Full Build Map 32 min -> 11 to 18 min, same scene (4 report/grading loops indexed).
   Timed (PC in use): Ridge quick to a playable exe 298 s; church quick to the exe 497 s (its Businesses step is 1 to 2 min). Proof: final full
   build vs the quick church build, 0 differences round the church (`a_full_vs_quick_church.txt`); 3 objects differ between ANY two full builds
   (build noise, `a_full_vs_full_noise.txt`). Times: `a_timings.txt`. Pipes is marked quick-capable but was not compared against a full build.
   Found by the comparison and fixed: a full build left the crypt's 4 colliders and house 1092's roof without their frictionless ramp material.
B1 DONE. A tall window (2.4 x 4.2 m, open to the floor) either side of the doors, glass that breaks, whole again after a reset; the old drawn windows
   on that wall gone; the way in always built (a 0.3 m dip at the threshold stopped a slow car); the stair's top 3 m open at its sides (balustrade
   and pews start 3 m back) so a car turns onto it. Check: Street Classic and Drifter Twin in through each window and down the stair, 4 of 4.
B2 DONE. The business lot's paving was drawn through the church at the old ground (0.3 to 0.5 m over the aisle, across the stairwell:
   `b2_before_mesh_probe.txt`); now cut out. Check: down the aisle and stair the wheels within 0.01 m of the drawn steps; pictures from Dan's spot.
B3 DONE. The plume only in a race that arms it; Free Roam (Getaway included) never. Check: Free Roam both ways shut; reverse race blows at 79 m.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-18 14:19 (BUILD_INFO.txt). Commits: Part A, Part B, this status.
For Dan to check: in through a church window and down the stair; the aisle from the doors; the plume in Free Roam at night; the crypt's walls and
   floor now slide like the other ramps (frictionless body contact) and so does house 1092's roof: say if the crypt or the crest cut feel different.

## Previous delivery 2: Round 41
**Reviewed: driven by Dan 2026-10-18 10:15 (a reverse Chastain Ridge race through the crypt and Getaway; his notes are Round 42).**
**Round 41 (Dan, 2026-10-17 21:17; report BUG-001 of session `2026-10-17_21-06-21-561_3280ac`, read by Claude, after the
Round 40 exe: the reverse Chastain Ridge (best lap 0:59.627) and Free Roam at dusk).** Round 40 is reviewed. Dan: "Getaway seemed
to work as I expected" (no change). Parts A to C in order; rebuild into `Builds/ChastainRush/` and commit after each; short
results (CLAUDE.md "Process"). Decide design questions yourself and say what you decided. First, add the new standing rule
"Real jumps off ramps" (top of this file) to CLAUDE.md's standing rules in the same words.

Part A, THE CHURCH SHORTCUT, REDONE (reverse Chastain Ridge; Round 40 A7's church leap). Dan: "Lets not do fake jumps off of
ramps. If there is a ramp we do actual jumps only. If we do something on a scripted jump path it has to be something creative
and not just a ramp. ... For this particular one, lets make it go downstairs instead to an underground crypt, you emerge out in
graveyard with a jump that is made of the stairs of the exit. This should make it enough to get a few seconds bonus". Remove
the church leap (the climb inside, the gallery, the aimed flight out of the stained-glass window); keep the front doors that
break and the church's outside look. New line: in through the front doors, down the aisle, down a stair (driven as a ramp) into
a crypt under the church (stone vaults, tombs, candles; breakables as the other interiors, simple and readable), along it and
out under the churchyard, up the crypt's exit stair into a graveyard beside the church (new: headstones, a few that break,
an iron fence, old trees; on the side toward New Chastain Road), where the top of the exit stair and its stone surround make the
launch: a REAL jump (no aimed or scripted flight, the rule), over the graveyard, landing on a down-slope sized to it (the
graveyard's slope graded for it, or a landing on New Chastain Road's bank), rejoining New Chastain Road on the race line heading
northwest, forgiving the checkpoints it skips. "A few seconds bonus": taken flat out and clean it saves at least 2 to 3 s
against the road on every vehicle the route offers (Round 40's leap broke even); a botched exit or a crash in the crypt costs
more than it saves. Reverse only. Check: driven through flat out on two vehicles (time saved, air time, landing upright),
one crash; pictures of the doors, the stair down, the crypt, the exit stair from below, the jump, the landing. The AI take it by
ShortcutStrategy with mistakes.

Part B, THE CREST CUT NEEDS ITS OBSTACLE (Round 40 A6, reverse). Dan: "I asked for an obstacle to make it so that you it hit
right and there was no obstacle, you even clip through the roof of the house that you land on. Could easily put a second ramp
on the house you pass through the roof of at a slightly different angle to add challenge". Find the house a vehicle flies
onto off the crest cut's slope (the bump log and a flat-out run on the Drifter Twin and a motorcycle) and why it clipped through
its roof (no roof collider, a gap in it, or the roof below its mesh): its roof is solid. Then Dan's obstacle: a second ramp on
that roof, set at a slightly different angle from the line you arrive on, so you have to land right and line up to take it;
a REAL jump off it (the rule) onto a landing sized to it that rejoins at or past CP4. Built so it could be there (a roofer's
ramp of boards and shingles bundles, or similar; the ramp-look lesson). Miss it and you drop off the roof's side, slower
than the road. Taken right it still saves time. Round 40's boulder wall stays only if it does not block that line; say. Check:
taken right and missed, on the Drifter Twin, the Street Classic and a motorcycle; the time saved; pictures.

Part C, PIPE EXITS. BUG-001 (x 2676.13 z 448.57 h 39, off Joel Drive at the network's Exit W11 Pond, x 2686 z 430, at dusk):
"not sure if it is the same one but it is sloppy". It is a different exit from Round 39's X_SHOPS. Round 39 A2 noted the same
kept, ungraded ground at BF, X_W20, X_W11, X_BRIDGE and X_STREAM; Dan (21:20) chose to clean up all five: the same treatment
Round 39 gave X_SHOPS (a clean headwall and wingwalls, the bank behind them graded tidy, no ragged ground or flaps at the mouth;
houses near them untouched), each still drivable in and out. Check: a picture of each from outside, and BUG-001's spot.

For Dan to check (when done): the crypt shortcut, the crest cut's roof ramp, the five pipe exits. Still his: the X_SHOPS pipe,
Speed Patrol, hidden cruisers (Canton Road's included), Cop vs Runner.

**Status (Claude, 2026-10-18): Parts A to C built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round41/`. The standing rule is in CLAUDE.md.
A PARTLY DONE. THE CRYPT replaces the church leap (climb, gallery, aimed flight gone). One arc off Canton Road square to the front doors (they break), the aisle, a straight
   stair through the nave (north wing and spine hollow: pews, chancel, altar), a vaulted hall (pillars, tombs, candles; stands and urns break), the catacomb under the
   churchyard, the storage row and its lot, a wide bend, a 22 m exit stair out of the bank into the new graveyard (iron fence, gate open where the jump leaves, 36
   headstones of which 3 break, old trees). Real jump off the stair's top and surround: 1.4-1.6 s air, down 0.2-5.3 m from the race line, every vehicle upright.
   Forgives CP1, CP2. Flat out against the road: cars save 1.3 to 2.4 s (Street Classic 2.0), the bikes, ATV and mower 1.1 to 1.9 s. NOT met: "2 to 3 s on every
   vehicle". The bikes come into the exit bend at 40 m/s and run wide. Crash in the hall: Street Classic stopped, Drifter Twin 0.2 s ahead of the road (a crash
   should cost more). AI: 2 races of 3 reverse laps with traffic Normal, all finish, crypt taken by ShortcutStrategy, sloppy tries seen.
   Decided: the graveyard is on the low ground between the storage lot's bank and New Chastain Road. The ground beside the church is S2's landing and exit.
B DONE. CREST CUT: house 1092 (the one you land on) had a box only to its eaves under a roof drawn 2.4 m higher; its own mesh is its collider now. On it a roofer's
   ramp (boards on shingle bundles, 12 m, 27 deg right of the line down the slope; the line arcs square onto it). Real jump onto the lawn beside the drive past CP4,
   about 2.7 s air. Taken right vs the lap: Street Classic saves 2.5 s, Drifter Twin 1.0 s. Needle 600 crashed on landing (lip about 15 m over the lawn). Missed
   (straight on over the ridge): slower than the lap on all three. Boulder wall: middle (over the line) gone, ends kept, side yards filled.
C DONE. BF, X_W20, X_W11 (BUG-001), X_BRIDGE, X_STREAM get X_SHOPS's `Retaining`: wingwalls retain the kept ground, the bank behind them raised to a house's
   base, houses untouched. Pictures of each and BUG-001's spot. Driven in and out: BF, X_W20, X_BRIDGE. X_W11's outer end is its pond (stalls in the water).
   X_STREAM's drive was not a valid test.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-18 07:49 (BUILD_INFO.txt). A commit per part.
For Dan to check: the crypt flat out (time on a bike), its graveyard jump, the crest roof ramp (a bike's landing), the five exits (X_STREAM's mouth up close).

## Previous delivery 3: Round 40
**Reviewed: driven by Dan 2026-10-17 21:06 (the reverse Chastain Ridge and Free Roam; his notes are Round 41; Getaway "worked as I expected").**
**Round 40 (Dan, 2026-10-17 12:31; reports BUG-001 to BUG-011 of session `2026-10-17_12-09-19-449_081d93`, read by Claude,
after the Round 39 exe: a reverse Chastain Ridge race on the Drifter Twin, finished 3rd, then Free Roam).** Round 39 is
reviewed, in part: Dan has not tried the police yet ("I didn't see any police so haven't tested them yet") and has not found
the X_SHOPS pipe again; both are still his to check, no change asked. Dan: "For now lets address these issues." Parts A to C
in order; rebuild into `Builds/ChastainRush/` and commit after each; short results (CLAUDE.md "Process"). Decide design
questions yourself and say what you decided. Look at each report's screenshot (`%USERPROFILE%\AppData\LocalLow\Chastain
Rush\Chastain Rush\DebugReports\2026-10-17_12-09-19-449_081d93\Screenshots\`; ignore its `small\` folder, Claude's copies).
Claude asked Dan three questions (12:35); his answers are quoted below.

Part A, THE REVERSE CHASTAIN RIDGE (all in the reverse race).
A1 / A2. THE CLIMB BECOMES A MANHOLE PLUME YOU CANNOT MISS. BUG-002 (x 3148.31 z 628.20 h 265, on Chastain Ridge Drive just
   past the climb's landing): "dont' like the way the ramp launches you. Lets be a little more creative, instead of using yet
   another dirt ramp (that isnt even a real jump), lets use a fire hydrant water plume instead. we put these ideas in this
   game, lets take advantage of them and put some variety in the game. Not everything needs to be dirt pile jump." BUG-001
   (x 3265.30 z 627.41 h 302, New Chastain Road just before the climb): "if this is the intended route, need to make it more
   obvious. It seems like you go straight here not take the ramp". Dan then changed the idea (12:45): "instead of a fire
   hydrant, how about instead we make it a manhole cover that blows up as the first driver gets close to it and you can't
   bypass the plume. This way it will look like you drive straight but won't matter because you can't miss the water plume".
   So: remove Round 39's road-work fill launch (its dirt, cones and barricade). In New Chastain Road's northwest-bound
   carriageway, on the race line where you drive straight on, a manhole cover (the street's own look, flush, nothing marks
   it). When the first racer (player or AI) comes within launch range, the cover blows off (Round 36 D1's bang, water, the cover
   flying) and a big water plume erupts; it keeps gushing for the rest of the race. The plume covers the whole carriageway
   (every lane and the shoulder, curb to median), so anything driving on along the road goes through it: there is no way past
   it on that carriageway, and no barrier or arrow is needed to steer you in. Driving into it throws the vehicle up and turns it
   toward the dead end (aimed like Round 36's geysers and the climb, whatever the line in): west over the hollow and house 706
   onto the dead end, landing as now on the forward pile's ramp face; every vehicle the route offers makes it at race speed,
   upright, a real flight, not a pop. A racer arriving before it has blown (the first one) still gets it: it blows in time for
   them. Traffic in the race keeps out of that carriageway's last stretch before it (or is not thrown), never stalls on the
   cover. Forward race: the cover never blows (the forward race is on the other carriageway). Free Roam: decide (for example it
   blows the same way when you come at it northwest-bound at speed and shuts again after a while) and say. Keep: the forward
   Ridge leap and its dirt pile exactly as they are (Dan: "Keep the forward leap"), resets short of the climb (on New Chastain
   Road before the cover), the hollow's way out. Check: on all 11 offered vehicles, from the left lane, the right lane and the
   shoulder, each thrown onto the dead end; the first racer's arrival with the cover still shut; pictures from Dan's BUG-001
   spot before and after it blows, the flight, the landing.
A3. BUMPY CORNER. BUG-003 (x 3473.41 z 409.57 h 222, New Chastain Road at the Canton Road junction, the course turning onto
   New Chastain): "fix this spot"; Dan (12:35): "bumpy". Smooth the turn on the race line through the junction. Check: driven
   flat out on the Drifter Twin, no bump-log event.
A4. THE FORWARD S2'S CLUES. BUG-004 (x 3352.09 z 461.53 h 313, New Chastain Road heading northwest): "need to clean up this
   shortcut here. This reads as a shortcut when it isnt going this direction". In the reverse race, hide S2's turn-off clues
   (its tracks and the knocked-over barricade) and anything else that marks it.
A5. NO REVERSE S1. BUG-006 (x 3458.88 z 1069.06 h 148, at the Knoll's pile off Chastain Corner): "lets not count this as an
   official shortcut this way". Remove Round 39's reverse S1 Knoll drop: it forgives nothing in reverse, and its reverse clues
   go. Forward S1 unchanged.
A6. NEW SHORTCUT AT THE CREST. BUG-005 (x 3091.70 z 616.18 h 358, on Chastain Ridge Drive at the crest, the houses ahead, CP4
   82 m on): "lets put a new shortcut here straight through to the next checkpoint. Just make an obstacle so it is a bit of a
   challenge but saves time if you dont crash". Reverse only: straight on from Dan's spot between / through the yards ahead
   to rejoin at or past CP4, forgiving what it skips. One obstacle that is a real challenge (not a jump; for variety: things to
   weave through, a gap to thread, something that moves), crashing costs more than the cut saves, getting it right saves
   time. Houses may be moved slightly if needed (say which); nothing more. A clue at the turn-off that could be there.
   Check: driven clean and crashed; the time saved.
A7. THE CHURCH SHORTCUT. BUG-007 (x 3569.10 z 605.67 h 235, Canton Road by the church, just past the start line): "Lets put a
   shortcut here....you can drive through the front doors of the church, wind you way up top and jump from the steeple area".
   Reverse only: off Canton Road southbound, in through the church's front doors (they break), a way winding up inside (ramps
   or stairs as ramps) to the tower, out of the steeple area in a big aimed jump, landing on New Chastain Road on the race line
   heading northwest (Dan: "New Chastain Road"), forgiving the checkpoints it skips. The church looks like a church outside;
   inside is new (simple, readable: pews, an aisle, the climb). Harder than the road and worth it. Check: driven through flat
   out on two vehicles; air time; pictures of the doors, inside, the take-off, the landing.
Check for Part A: one AI race of 3 laps reverse with traffic Normal, everyone finishes; the AI take A6 and A7 by
ShortcutStrategy with mistakes.

Part B, ROADS.
B1. LINES ACROSS THE ROAD. BUG-008 (x 3564.21 z 148.31 h 156, Canton Road, Free Roam): "These cross lines again in the middle
   of the road. Need to remove any that cross like this" (white lane / edge lines from a joining road painted across the
   through road's lanes). Dan asked for all of them: remove every painted line that runs across another road's lanes,
   map-wide; say how many and the cause.
B2. NEW CHASTAIN ROAD'S MEDIAN. BUG-009 (x 3057.61 z 760.38 h 312): "This median all down this road shouldn't be sloped like it
   is. The ground between the concrete barriers are relatively flat, though we should at least make it smoother. We can plant
   more trees through the medians as well". All along New Chastain Road: the grass between the median's curbs nearly flat
   across (level with the curbs, gently following the road's grade), smooth; trees planted along it (spaced, kept clear of
   turn lanes, crossings and sight lines at junctions; solid like the other trees). Check: pictures from Dan's spot and one
   other stretch.

B3. A HIDDEN COP ON CANTON ROAD. Dan (12:47): "I feel like there should be a hidden cop on canton road as well". Add hidden
   cruisers (Round 39 D's HiddenPolice) on Canton Road: one or two, parked where a speed trap would really sit (a business lot
   or a side street's mouth facing the road, half hidden), off the race lines, the stunt park, the shortcuts' lines and Round
   36's speed-trap camera posts; Free Roam only as the others. Say where. Check: one starts a chase at 10+ mph over on Canton
   Road.

Part C, BURIED HOUSES. BUG-010 (x 1883.40 z 645.06 h 251, off Chastain Trace): "unbury this house". BUG-011 (x 590.21 z 858.85
h 218, by an unnamed road off Chastain Road): "Raise this house". Set each on the ground (its floor just above the ground at
its highest corner, foundation showing on the low side as the others). Check: a picture from each spot.

For Dan to check (when done): the reverse Ridge (the manhole plume, the crest shortcut, the church, the corner), the lines
on Canton Road, New Chastain's median, the hidden cop on Canton Road, the two houses. Still his from Round 39: the police (POLICE on the title and Free Roam
menus; hidden cruisers on Bells Ferry, New Chastain and Chastain Roads and four neighborhood streets) and the X_SHOPS pipe.

**Status (Claude, 2026-10-17): Parts A to C built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round40/`.
A1/A2 DONE. Round 39's climb (dirt, cones, barricade) gone. A manhole cover on the northwest-bound lane 12 m past Dan's BUG-001 spot; it blows 80 m before the
   first racer and gushes all race; the plume covers lanes, shoulder and median; an aimed flight onto the forward pile's ramp face. All 11 vehicles from left
   lane, right lane and shoulder: thrown, 3.9 s air, 28-30 m up, down 3-4 m on the ramp face, upright. Traffic and police cars pass unthrown. Decided: forward
   race never blows; Free Roam blows at 12+ m/s northwest-bound within 45 m, shuts after 25 s. Forward leap re-flown on all 14.
A3 DONE. A 6 cm crest and 9 cm dip where Hilltop Drive and a service road end on the lane: smoothed there only. Drifter Twin flat out: no bump event.
A4/A5 DONE. S1's and S2's tracks and barricades hidden in a reverse race (shown forward and in Free Roam). Reverse S1 removed.
A6 DONE. CREST CUT: through the gap between houses 1245 and 1076 (1245 moved 1.6 m west: 5.5 m gap), then the one gap in a boulder wall terracing the back
   yards (moved below the steepest drop, its gap over the one between 1014 and 1092), onto the drive past CP4 (forgiven). Its line graded smooth (the slope
   kept). Worn tracks at the turn-off. Flat out on the line: the Street Classic saves 1.6 s; the motorcycles break even (they fly on the slope). 1.6 m off
   the line: a crash at the house corner or slowed by the boulders.
A7 DONE. CHURCH LEAP: in the front doors (break), aisle past pews, round the altar, up along the north wall, round onto the gallery under the steeple, out
   the stained-glass window (breaks) on a curving aimed flight (4.7 s) over the south wing onto New Chastain Road heading west, past CP2 (forgives CP1, CP2).
   The two bends inside are held (arcade). Against the road: Drifter Twin +1.4 s, Street Classic -0.5 s. It is a show line that about breaks even: the
   church stands beside the road it skips, so the climb inside costs what the flight saves (see For Dan to check).
   AI: 3 reverse laps x 3 AI, traffic Normal, all finish, no missed checkpoints, both shortcuts taken by ShortcutStrategy; a race taking every shortcut,
   sloppy tries included: all finish. With traffic Normal 2 of 47 scripted runs on Chastain Ridge Drive were stopped by a car turning at the dead end
   (both pass with traffic off).
B1 DONE. Cause: two one-way carriageways of one street where they split (Canton Road at Worley Drive) let the wider one's lines run over the other's lanes.
   Rule fixed map-wide: 14 carriageway pairs (Chastain Road NW 4, Canton Road 1, Chastain Meadows Pkwy 1, Townpark Dr 1, I-575 ramps 7).
B2 DONE. New Chastain Road's median straight across between the carriageways' edges (up to 0.83 m moved), crossings left; 65 trees down it (none within
   35 m of a crossing, the plume or the Ridge leap's landing).
B3 DONE. Two hidden cruisers on Canton Road south of New Chastain: in the lot by building 26's drive (3590, 293) and at Worley Drive's mouth (3600, 118),
   nose to the road; one always in use. Both start a chase at 65 in a 45.
C DONE. House 802 set on its ground (floor over its highest corner, foundation on the low side). Garage row 594 stands on flat ground: its walls were 2.6 m
   under a roof as wide as the building; now 3.7 m.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-17 20:24 (BUILD_INFO.txt). One build for all three parts (a map build is 30 min);
   three commits, the shared map-build files with Part A.
For Dan to check: the plume from BUG-001's spot; the crest cut flat out; the church leap (is a break-even show line OK, or should it land further on?);
   Canton Road's lines at Worley Drive; New Chastain's median; the Canton Road cruisers; houses 802 and 594. Still his: the police, the X_SHOPS pipe.

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
