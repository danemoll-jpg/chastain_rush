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

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 58, AI DRIVERS WITH AGENCY (recover, pass, never wrong way), and three fixes (Dan, 2026-10-22 01:20; reports BUG-001 to BUG-003
of session `2026-10-22_00-37-30-154_d7ca87`, read by Claude, after the Round 57 exe: Underground, Chastain Ridge and replays). THEN
ROUND 59 (the map extension, stage 1, below in QUEUED): Dan went to bed and asked for both; do 58, commit, then start 59 without
waiting.** Round 57 is reviewed. Dan is not watching: take the most reasonable reading of anything unclear, write it in the status, go
on (stop only for something that cannot be undone). Tuning limit. Short results.
A. THE AI. Dan watched the AI in replays: "I see some reasons why I beat them so badly usually":
   "1) I have seen them wreck, sideways on the bike lying down. Took them at least 5 seconds to restart themselves. I mean if you are
   your side, that should be an automatic reset
   2) Still saw them not putting much effort getting around cars
   3) Saw one run into a tree. I counted 10-15 seconds before they decided to reset. At the very least they could have tried to back up
   and get around the tree
   4) Literally watched one drive in reverse for a good stretch of the lap
   5) Saw one get in an accident, the drove off in the opposite direction for a bit before turning around
   Seems they have no agency to beat me. And this is is in hard mode I cant even imagine how bad it is in easy."
   For every AI racer, every track, every difficulty (difficulty changes pace and skill, never these recoveries):
   (1) On its side or upside down (a bike or rider down, a car over): reset at once onto the race line facing forward (well under a
       second once it is clearly down; no 5 s wait).
   (2) Passing: Round 55 E was "modest". Pass slower cars (traffic, parked, racers) decisively: pick a side early, use the other lane,
       the shoulder and the grass beside the road; take a gap rather than follow; a racer stuck behind a car more than about 2 s is
       the fault to remove.
   (3) Blocked (a tree, a wall, a car, a fence it cannot break): within about 1 s of no progress, back up and steer round it; if that
       fails within about 3 s more, reset. Never 10 to 15 s against a tree.
   (4) and (5) Never the wrong way: driving backwards along the lap, or forwards against it after a crash, is caught within about 1 s
       (heading against the lap's direction while moving, or reversing beyond a short back-up): turn round on the spot / three-point
       turn, or reset if that is quicker. Reverse only as part of (3)'s back-up.
   (6) Then Hard must race: measure, from the replays' recordings (Round 57 records every racer), per AI per race: time down / stuck /
       blocked / wrong way / behind a car, before and after (Hard, traffic Normal, every track both ways where it has both, at least one
       race each). Hard's lap times against Dan's best laps on each track (the Top 10 records): Hard should be within a few seconds of
       them; report the table. Easy and Normal keep their pace gaps but recover the same.
B. BUG-001 (x 1854.77 z 1132.96 h 87, THE HUB's floor, Underground race): "during the race, it still allows you falll down the pit of you
   are going to fastt, The gate should extend all th way down". Round 57 D's rails and gate still leave a way down: the gate / rails must
   reach all the way down (no gap under them to the pit) wherever the race passes the pit's edge. Check: flat out at every edge on the
   race's line and off it, all vehicles, none falls in during a race.
C. BUG-002 (x 1640.80 z 943.86 h 13, M5c, an aquarium pipe): "plants shouldn't be growing through the grass" (glass: the picture shows
   water plants standing up INSIDE the tube, through its glass and floor). Keep every aquarium pipe's plants, rocks and fish outside the
   glass (all aquarium pipes; Round 57 found the beds cutting tubes: same family).
D. BUG-003 (x 3082.26 z 708.71 h 270, Chastain Ridge Drive, Chastain Ridge race lap 3): "there is a bump somewhere around here that always
   messes me up". Find it on the race's line (both directions) and smooth it.
Check: A's table (before / after), and a replay of an AI recovering from each case (pictures or a short note of times); B, C, D as above.
For Dan to check: watch the AI in replays on Hard (recoveries, passing, no wrong way); race them; the hub's edges; M5c's plants; the Ridge bump.

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round58/`. Bug reports read: session `2026-10-22_00-37-30-154_d7ca87`, BUG-001 to 003.
A DONE (`RoadDriver` AgencyStep / PassLane; `-r58AgencyOff` gives the old driving). Down: put back on the line after 0.35 s. Blocked (wanting to go, not moving 1 s): backs up 1.1 s
   steering to the open side, crawls round; no 5 m of progress 4 s on: put back. Reversing: the brake pedal was reverse below 0.6 m/s, so a car rolling back after a knock kept
   reversing (Dan's "drive in reverse for a good stretch"); now it is on the throttle. Wrong way: past 100 degrees full lock (pure pursuit barely steered with the target behind:
   Dan's "drove off in the opposite direction"), put back after 3.5 s. Passing: either side, the shoulder and the grass, kept until past; behind a car 1.5 s the oncoming look shortens.
   By hand (`round58_check_parts.txt`): on its side going again in 0.9 s, turned round 2.9 s, rolling back 1.3 s, a trunk-size block in front 4.7 s (backed up once, round it).
   Table, Hard, traffic Normal, every course and direction, 33 AI races, from the replays (`r58_ai_before.csv` / `r58_ai_after.csv`): behind a car 342 s (48 spells over 2 s) -> 133 s (14);
   reversing 44 -> 7 s; stuck 163 -> 86 s (longest 3.4 s); down 9.7 -> 5.1 s; wrong way 26 -> 33 s (turning round after a crash, longest 2.1 s); 33 of 33 finish. The first
   "after" run found a driver backing up 185 times in S2's dive pipe (on a shortcut speed alone counted as progress): fixed, measured along the shortcut's line.
   Hard's pace (attempt 1 of the tuning limit; Easy and Normal unchanged): corner use .90 -> 1.0, brake 1.0, steep hills 42 m/s, crests 9.5. AI best flying lap against Dan's best:
   Glen fwd 39.6 / 38.9, rev 39.3 / 36.1; Ridge fwd 57.4 / 59.7, rev 70.8 / 59.2; Underground 65.6 / 75.5; Addison fwd 76.2 / 59.7, rev 69.5 / 69.0; Creek fwd 91.9 / 79.2,
   rev 92.4 / 69.5; Lakes fwd 111.4 / 72.1, rev 94.1 / 77.1 (Lakes is 2 laps: one flying lap each, noisy). Not tuned further: Creek and Lakes are held by their jump zones'
   set speeds and Dan's laps use shortcuts the AI takes only sometimes (the shortcut rule). Dan decides.
B DONE. Round 28 railed only the west and south-west of the pit; the race runs the whole mid-ledge ring and the span. Rails R3 to R7 (`make_underground_route.py`): the south
   and east edges to S1's floor ramp (left open: S1's way up), the north edge either side of the span, the span's two sides over the chasm (heights follow it, route `ys`).
   Four vehicles at 34 m/s into every edge at 90 / 45 / 20 degrees: 120 of 120 held, none in the pit.
C DONE. S2's aquarium (under the Enclave pond) had 22 weed clumps standing in M5c's tube (Dan's picture). Every glass stretch now knows the other tubes near it: weeds, rocks,
   logs and turtle spots keep out of them and out of its own tube on a bend; a fish inside another tube is not drawn (build log: 22 left out at S2, 1 to 5 elsewhere). Picture at his spot.
D DONE. The bump is the valley 50 m before his spot (the drive comes down at -13 %, a 10 m level shelf, then +15 % within 8 m): one vertical curve (as Addison's); the exe
   check also found a jolt 10 m after it at the loop branch's junction (x 3072): blurred. Flat out both ways, on the line and 3.4 m to his side: no jolt over 0.55 m/s, wheels down.
   Left: 3.4 m to the inside of the bend at x 3040, off the asphalt, still lifts a Drifter Twin's wheels (for Dan to judge).
`Builds/ChastainRush/` 2026-10-22 03:17. Imported from Racer: nothing.

## QUEUED rounds
**Round 59, THE MAP EXTENSION, STAGE 1: TOWN CENTER AND KENNESAW STATE (Dan, 2026-10-22 01:20: "can we possibly also add on the first
stage of expanding the map out to what we had written up the other night?"). START RIGHT AFTER ROUND 58 (Dan said go).** One map
rebuild at the end (plus whatever the new data import needs). Dan is not watching: decide, write it down, go on; stop and report only
for something that cannot be undone or if the world becomes unplayable.
What was written up (2026-10-19, Dan's map `local/reference_photos/extension/town_center_ksu_airport.jpg`, Google: reference only, never
traced): extend the map west and south to take in Town Center at Cobb (the mall and the shopping around it) and Kennesaw State
University's Kennesaw campus. NOT the Cobb County airport (stage 2, later). Today's bounds: south 34.0294, north 34.0477, west -84.566,
east -84.5234 (3983 x 1918 m). Claude's estimate for stage 1: about 6.3 x 4.2 km (about 26 km2, 3.5x). The 1 m USGS tile already in use
covers it (to 33.955 S, -84.619 W). Code sets the exact new bounds from OSM (the mall's ring road and the campus with a margin; the
north and east edges stay) and says what they are.
1. KEEP THE EXISTING WORLD WHERE IT IS. Every existing coordinate (hand-placed houses, tracks, routes, shortcuts, pipes, eggs, records,
   replays, Dan's bug positions, CLAUDE.md notes) must still mean the same place: the new land gets coordinates outside the old range
   (e.g. negative x / z, or an offset applied everywhere consistently with every data file converted and checked). Code picks and says
   why; nothing hand-built may move or be lost. Every race still runs as before (lap lengths, checkpoints, records unchanged).
2. THE NEW LAND, GENERATED AS THE REST WAS (OSM roads, buildings, water, woods; USGS terrain at 1.5x): roads and their junctions,
   buildings from their footprints (the mall, the stores, the campus buildings and stadium as big generic buildings; no hand polish),
   parking lots, trees, streams and ponds, bridges where roads cross water. I-75 and I-575 and their interchange through the new area:
   drivable, overpasses where roads cross them, as well as the generator can do (Dan expects highways to be the weak spot; no heroics).
   The old west edge (I-575's collapsed overpass, Round 9) is now inside the map: join it up.
3. EVERYTHING THAT USES THE MAP covers the new land: traffic and police road nets, the map screen and minimap, exploration fog and
   destinations (new ones: Town Center, KSU), fast travel, free roam spawns unchanged, the quick-rebuild areas, the loading screen's inset.
   No new races or eggs this round.
4. MEASURE (Dan wanted this before deciding on stage 2): full Build Map time, exe size, load time, memory, and frame rate at 1080p in
   the old neighborhood and in the new area (Town Center, KSU, the interchange), before and after; tree and building counts. If frame
   rate or load time gets much worse, report it with the cause rather than tune for hours (tuning limit).
Check: every race still runs (one AI race per track); free roam drive from Chastain Glen to Town Center and to KSU without falling
through the world; pictures of the mall, the campus, the interchange, the old I-575 edge joined.
For Dan to check: drive to Town Center and KSU; the highways; the numbers (go on to stage 2, the airport, or not).

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round59/`.
1 DONE. The new land is west and south of the old map, at negative x / z (`Tools/mapgen/mapgen_extend.py`, config `extension`: 4 tiles west, 3 south). Why: nothing that exists has
   to change. The core is generated first exactly as before (map.json's lists, trees.bin's trees and terrain.r16 checked byte for byte), the new pieces are appended after it (house,
   road and bridge numbers unchanged), the terrain grid grows by whole core-size tiles (the same samples on the same spots, the core's pasted in, the new eased onto it over 40 m),
   heights on the core's datum. Unity reads `terrain_full.r16` (origin -2655.33, -1918); the python scripts keep reading the core's terrain.r16. Bounds (from OSM, KSU's and the mall's
   outlines with a margin, the north and east edges kept): south 34.01268, west -84.59575, north 34.04582, east -84.52292; 6638 x 3836 m (25.5 km2, 3.3x).
2 DONE. 2252 road pieces, 40 bridges, 3147 buildings (the mall, stores and campus as Round 31's dressed businesses: 1281 in all now), 43 ponds, 87 streams, 69 parking lots,
   87,614 trees. I-575 and its Chastain Road overpass cross the old south edge with no seam (bridge decks met within 0.2 to 0.8 m and eased); I-75 / I-575 drivable on the generated
   ground (a weak spot as Dan expected: plain decks where OSM tags a bridge, no hand work). A real quarry in the far south-west (floor 166.7 m, about 200 m deep at 1.5x).
3 DONE. Traffic and police nets (1602 traffic nodes, was 507), the map picture (8192 px wide, 0.8 m a pixel), minimap, exploration fog (Dan's saved fog renumbered onto the larger
   grid: on a copy of his save all 3660 cells and 11 places kept, the same ground), fast-travel destinations Town Center and KSU (`add_extension_places.py`), loading inset. Spawns unchanged.
4 Numbers, before / after (1920 x 1080 window, not measured at 3840 x 2160): Build Map 845 s / 1955 s; exe data 626 / 1357 MB; world up 6.5 / 19.4 s after start; Unity memory
   1065 / 1838 MB; terrain tiles 18 / 60; trees in the scene 54,943 / 142,282; buildings 2388 / 5535. Frame rate parked, average / slowest 1 %: old neighborhood (Canton Road) 182 / 67 ->
   122 / 39; Town Center 171 / 51; KSU 154 / 53; the interchange 158 / 53; the old I-575 edge 169 / 57. Not tuned (the round's rule): the old neighborhood lost a third, most likely
   the things spread over the whole map that run every frame whatever the distance (people groups 1765 -> 3512, hydrants 536 -> 1915, traffic network 3.8x) and more far objects drawn.
Check: every race still runs (11 courses and directions, one AI race each on Hard, traffic Normal: 33 of 33 finish, `round59_check_races.txt`); free roam Chastain Glen to Town
   Center (4.9 km) and to KSU (5.2 km) along the roads: through, never under the ground (`round59_check_drives.txt`); pictures of the mall, the campus, the motorways, the old edge.
Not done: the races and eggs were not touched (as the round says); neighborhoods_rest.py and the route scripts were not re-run (they would now see the new roads by name: see Technical notes).
`Builds/ChastainRush/` 2026-10-22 04:20. Imported from Racer: nothing.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- THE MAP EXTENSION, STAGE 2: the Cobb County airport (after Dan has seen stage 1).

## Previous delivery 1 (newest): Round 57
**Reviewed: Dan drove the Round 57 build 2026-10-22 00:37 (Underground, Chastain Ridge, replays): "The replays seem to work fine"; A to E no complaint; his notes are Round 58.**
**Round 57, RACE REPLAYS; fixes on Addison Heights and the Underground; COMPLETE RACE on the pad (Dan, 2026-10-21 21:35; reports BUG-001
to BUG-005 of session `2026-10-21_21-15-52-689_2c460b`, read by Claude, after the Round 56 exe: Addison Heights and the Underground race
on the Drifter Twin).** Round 56 is reviewed. Do A to E first (their one full Build Map, commit), then F (code only, no map rebuild).
Tuning limit. Short results.
A. BUG-001 (x 3279.17 z 308.15 h 184, Embry Lane's foot where it turns onto Suholden Circle, Addison Heights): "flatten this corner".
   Smooth it (grade, crown and camber through the turn) so it rides flat at race speed both ways.
B. BUG-002 (finish screen, Addison Heights): "a button doesn't complete the race". COMPLETE RACE (Round 54 C) shows the A glyph but
   pressing A on the controller does nothing (it worked from the keyboard later: BUG-005's results show "(estimated)"). Make the pad's
   A (and the keyboard key) press it; check both in the exe.
C. THE UNDERGROUND RACE: WEDGED, AND A BLOCKED TUBE. BUG-003 (x 1642.07 z 950.60 h 188, pipes "M5c M5 Enclave Main: middle, 29 m", lap 1,
   CP3 next): "somehow I got wedged here" (the picture: the screen all dark green, the camera inside the pipe wall). BUG-004 (same spot,
   turned round, h 8): "the tube is completley blocked" (the picture: an AI rider on an ATV stopped in the pipe ahead). Find why a car
   wedges at this point of M5c (a seam, a step, a narrowing, a joint between pipe pieces) and fix it; find why the tube is blocked (the
   stopped ATV, or geometry) and fix that: the AI must never sit stuck in a pipe (recover / reset as on roads). Keep the camera out of
   the pipe wall. Drive M5c both ways at race speed on all vehicles, no stop.
D. BUG-005 (x 1854.67 z 1132.51 h 92, THE HUB's floor, Underground race): "this is not enough to prevent yopu from going over the railing or
   under it". The hub's railings must hold a car: solid down to the floor (no gap under) and high enough not to be jumped or climbed at
   race speed (or a wall behind them), all round where the race passes. Check: drive at them flat out from the race line, none get through.
E. THE ELAINE SHORTCUT AND THE REST OF ROUND 56: accepted, no change.
F. RACE REPLAYS (new, not in Racer). Dan: "can we go ahead and create race replays? Would want it to automatically replace top 1 laps
   and races with the ability to save any race you want." (Earlier, 2026-10-20: "be able to watch your race you just ran and move the
   camera around differently".)
   (1) RECORD every race: every racer (player, AI, split-screen, police in the race) with position, rotation, steering, wheel spin and
       suspension, lights, rider animation state; plus the world events a race causes so they play again at the same moment: breakables
       (mailboxes, fences, hydrants and their water, signs, gas pumps exploding, barricades), geese scattering, manhole plumes, bounce
       house / trampoline bounces, shortcut barriers, traffic cars, and the sounds (engine, impacts, horns, the race's sounds). The race
       radio / music is not recorded (the replay plays the radio live, or none: Code picks).
   (2) WATCH: on the finish screen, WATCH REPLAY (pad glyph and key). Controls: play / pause, rewind, fast forward, slow motion, jump to
       a lap, restart; HUD off by default (toggle). Cameras: the chase camera on any racer (switch racer), the free camera (Trailer
       Mode's free look), and TV-style roadside cameras placed along the course that cut automatically as the racers pass (the fun way to
       watch a jump or a shortcut). All controllable with the controller.
   (3) KEEP: automatically, per track and direction, the replay of the #1 TOP 10 lap and of the #1 TOP 10 race; a new #1 replaces the old
       one. The best-lap replay plays just that lap (the others as they were then). SAVE: from the finish screen (and from the replay
       viewer) the player can save any race replay to keep, named by track, direction, date and time.
   (4) A REPLAYS page in the menus: the kept #1 lap / race per track and the saved ones; watch or delete (the #1s cannot be deleted
       from there, only replaced). Replays live beside the records in the save folder; report the size of one 3-lap replay.
   (5) If the map, a track or a shortcut changes later, an old replay may no longer fit the world: mark replays with the build / track
       version and say on the page if one is older (still playable, the world may differ). Records keys as the records use.
   Check (exe, minimized, muted): race Addison Heights forward 3 laps (with the Lyndon jump, a smashed barricade and a mailbox), finish,
   watch the replay: positions match the race within a small error (report it), the breakables break on time, each camera works, the
   controls work by pad and keys; beat a lap and a race record and see the #1 replays replaced; save a race and find it on the page.
   Pictures: the replay from each camera kind, the page.
For Dan to check: the Embry / Suholden corner; COMPLETE RACE with A; the Underground's M5c and the hub's railings; watch a replay with each
camera, then try the #1s and save one.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round57/` (check reports, pictures).
Bug reports read: session `2026-10-21_21-15-52-689_2c460b`, BUG-001 to 005.
A DONE. Embry climbs south at 10 % into the corner; Suholden west of it had been levelled flat, so the turn crossed a 0.45 m step on a diagonal. The turn's arc is now one
   vertical curve, 10 % into level over 40 m, flat across 5.5 m either side, then blurred (ground moved up to 0.71 m). Flat out both ways, Drifter Twin and Street Classic:
   all wheels down, biggest step in vertical speed 0.09 to 0.13 m/s (`round57_check_a_d.txt`, `57_A_*`).
B DONE. The cause: the button listened to the menus' confirm action, which the UI module switches off whenever the menus are closed, so neither A nor Space
   reached it (the "(estimated)" times on BUG-005 came from the 60 s wait). It has its own action now. Pad A and Space each complete the race (`round57_check_b_f.txt`).
C DONE. The wedge and the blockage were S2's glass bed (Round 46): S2 dives under M5c and its bowl's bed cut through M5c's tube in two bands 10 m apart (Dan's
   green screen is that bed). The bed is kept under the tube now (build log: 0 bed triangles inside another tube). The AI was put back between the two bands
   over and over: when it gets stuck within 15 m of its last stuck spot, it is now put back 20 m or more further on. The pipe camera never goes past a wall.
   M5c flat out both ways on all 14 vehicles: through, 27 to 34 m/s at Dan's spot. One exception: the Skyfin Cruiser drops to 2 m/s at the check's own start,
   placed sideways on the bend out of J15, 56 m from his spot. Underground race, 3 AI on Hard: all finish.
D DONE. Rails R1 and R2 and the closed gate SG1 are solid from under the ledge up to 3 m, 0.6 m thick, with a low third bar. Four vehicles hit each one at 30 m/s at
   90, 45 and 20 degrees: none got over or under. The grille that hung 2 m up after S1's switch opened it (it looked like a rail you could drive under) now lifts
   4.6 m clear. B28's glass bed had the same fault as S2's, inside M2a: fixed at Dan's yes (follow-up below).
E no change.
F DONE (code only: no map rebuild, as the round says). Every race is recorded: racers 25 times a second (pose, speed, steering, pedal), traffic 10 times a second,
   and breaks, restores, trees, plumes, switch gates and gestures. Engines play from the replay; the radio plays live. Finish screen: WATCH REPLAY (X / V) and SAVE REPLAY (Y / K).
   Viewer: A play / pause, RT fast forward, LT rewind, X slow motion, Y camera (TV, chase, free), LB / RB racer, D-pad lap / restart / HUD (HUD off by default), View save, B leave.
   The REPLAYS page is on the title, free roam and race menus. Each #1 Top 10 lap and race per course and direction is kept, and a new #1 replaces it. The page marks a replay
   recorded on another build or before its course changed. Check, Addison 3 laps, 3 AI, Normal traffic: replay within 0.006 m of the race (416 samples); 12 of 12 breaks on time
   (mailboxes, lamps, the barricade and its restores); every camera, every pad and key control, save, the page, playing from the page: 19 of 19.
   #1s: slow kept, faster replaced, slower left alone (4 of 4). Size: 1.1 to 1.7 MB for a 4 to 5 minute 3-lap race with Normal traffic.
   The Lyndon jump was in an earlier run's replay; in the final run the check's driver got stuck in traffic and was put back past it.
Follow-up (Dan's yes, 2026-10-22): B28's glass bed under West Chastain Lake had cut through M2a near x 1295 z 1164 (0.07 m from M2a's axis). It is now kept
   under M2a like S2's (`NetGlassAvoid` S2, B28). Build log: 8 bed triangles still flagged, but all are 0.10 to 0.32 m OUTSIDE M2a's wall (the check counts anything
   within 0.35 m); none is inside the tube. Free roam, M2a 70 m either side, flat out, both ways, all 14 vehicles: through, 30 to 38 m/s at the spot, slowest 18.9 m/s;
   the only touch of B28's bed is a flat floor contact with no speed lost (`round57_check_m2a.txt`; `-exeRound57 <dir> -r57Only m2a`).
`Builds/ChastainRush/` 2026-10-21 23:39. Imported from Racer: nothing.
For Dan to check: the corner both ways; COMPLETE RACE with A; M5c and the hub's rails; M2a under West Chastain Lake in free roam; a replay with each camera, the #1s, save one and find it on REPLAYS.

## Previous delivery 2: Round 56
**Reviewed: Dan drove the Round 56 build 2026-10-21 21:15 (Addison Heights, Underground); no complaint about the Elaine shortcut, the barricade, the jerk or Joel Drive; his notes are Round 57.**
**Round 56, THE ELAINE SHORTCUT: LAND ON CRAWFORD CIRCLE, A WAY BACK FROM THERE, A BLOCKED ENTRY WITH TIRE MARKS; a jerk on Elaine;
the things on Joel Drive (Dan, 2026-10-21 17:08; reports BUG-001 to BUG-004 of session `2026-10-21_17-00-50-815_f90718`, read by
Claude, after the Round 55 exe: Addison Heights forward on the Drifter Twin).** Round 55 is reviewed. Dan: "still not quite hitting the
mark on the Elaine shortcut". Standing rules apply. ONE full Build Map at the end (plan the line on paper first; quick builds while
working). Tuning limit. Short results.
A. WHERE A GOOD JUMP LANDS, AND THE WAY BACK FROM THERE. BUG-004 (x 2947.19 z 315.69 h 141, ON Crawford Circle's west leg, the closed
   road between the Hayworth / Crawford corner and Lyndon Drive, a barrier just ahead): "A good jump will tak you here. The path should
   work back from the main road from here. If it is off the main track for a bit as long as it joins back with some time savings".
   So the target is no longer Round 55's bench and hedges on the bank: a GOOD jump off Elaine's west bend (Round 55's launch, natural,
   no ramp) carries you over the bank and down onto Crawford Circle's west leg at Dan's spot. From there the shortcut is a way back to
   the lap, off the main track for a while, joining it heading the race's way with SOME time saved (Dan accepts more than "a few
   seconds" here; Code reports the number). Code picks the way back and says why (e.g. west along Crawford's west leg, then Lyndon
   Drive north to Joel Drive westbound; if so, decide what happens at Joel / Lyndon, where the road-crew shortcut starts straight
   ahead: no accidental chaining unless it is fair; say what was chosen).
   A SHORT jump (not enough speed, off line) comes down on the bank / at the corner and pays for it (hedges or similar, as Round 55),
   so the good jump is the reward. Make the launch able to reach Dan's spot at a good race speed on the Drifter Twin and the Street
   Classic (natural lip only shaped, as before; tuning limit). Remove what Round 55 built that this does not need (bench, gaps, hedges
   that now block the good line) and keep what still makes the short jump costly. Crawford's west-leg barrier moves so the landing and
   the way back are open forward and stay shut in reverse.
B. THE ENTRY: BLOCKED, WITH TIRE MARKS. BUG-003 (x 3205.88 z 303.90 h 287, Suholden Circle at the Elaine turn-off): "there should be an
   indicator that the road to the right is a shortcut. Maybe actually block the road off and make tiremarks right after it". Put a
   breakable road-closed barricade (sawhorses / a striped board, as a road crew would) across Elaine's mouth, with tire marks on the
   road right after it, so it reads as "people go through here". Breaking it must not slow you (as fences); it resets each lap / race.
C. BUG-002 (x 3088.66 z 307.72 h 294, Elaine Circle on the shortcut's run-up): "there is something here that causes my vehicle to jerk to
   the righ. Please check the road in this area". Find it (a ridge, a camber change, an edge, a collider) and fix it; check at speed.
D. BUG-001 (x 2963.10 z 411.11 h 264, Joel Drive west of Hayworth, on the lap): "why is this here? and what is it?" (the picture: orange
   round bases with red tops standing in the road and beside it). Say what they are; if they have no purpose a racer can read, remove them.
Check: the good jump (Drifter Twin and Street Classic: launch speed, where they land, time saved), a short jump (cost), the ways round;
the barricade breaks without slowing; the AI takes it sometimes, forward race x2 traffic Normal, all finish; reverse unchanged.
Pictures: Dan's four spots, the jump from behind, the landing on Crawford, the way back.
For Dan to check: the Elaine shortcut (good and short jumps, the barricade and tire marks); the jerk on Elaine; Joel Drive at his spot.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round56/` (check reports, pictures).
Bug reports read: session `2026-10-21_17-00-50-815_f90718`, BUG-001 to 004.
A DONE. Round 55's bench and its hedge gap gone. The lip: the bend's crest on Elaine's own line, raised 1 m to an edge (20 %, no ramp), heading 267. Flat out a
   Street Classic leaves it at 30 m/s and comes down 87 m out at (2960, 313), Dan's spot; at 21 m/s 44 m out, on the lower bank (a landing zone). The run on:
   through a gap in the bank's foot hedge, over the Crawford / Hayworth corner, down Crawford's west leg. The way back (Code's pick): from Dan's spot north
   between 884 and 1143, round 1143, up onto Hayworth by CP 5, graded at 22 % with worn tracks; it never reaches Joel / Lyndon, so it cannot run on into
   the road-crew shortcut. Crawford's west-leg barrier moved 56 m past the way back (forward only; reverse unchanged). The short jump comes down in
   brush, 18 to 36 m out, at twice the usual drag (`ShrubBeds` strength). Times against the lap's way (`round56_check_final_a_c.txt`): clean, Street Classic
   saves 6.0 s, Drifter Twin 6.0 s. Against the lap: short (15 m/s off the lip) +0.0 s, a tie; no jump +6.5 s; round the barrier +1.4 s.
   Turning up Hayworth at the corner after the jump: Drifter Twin 17.4 s against 19.1 s round 1143 (a quicker way for a rider who can stop).
   Two layout attempts: the first lip, 2 m north of Elaine's line, turned cars into house 960 (`round56_check_a_try1.txt`, `_try2_probe.txt`).
B DONE. Three breakable type III barricades across Elaine 17 m in from Suholden (ahead on the right from Dan's spot), tire marks up Elaine behind them.
   Forward race only. 22 m/s through them: 21.7 then 21.8 m/s, no speed lost. Whole again 7 s after the car leaves (`RestoreWhenClear`).
C DONE (Dan watched the check drive it: "half way up the hill to the ramp to the right side of the road"). Traced flat out from Suholden: at 26 m/s, where
   the climb steepens from 17 to 31 %, the suspension squeezes until the body's right front corner dips 10 cm into the road (x 3091; the Drifter Twin
   at 3075 and 3098): kicked up 0.3 m, rolled 7 degrees, heading swung right at up to 470 deg/s (`round56_check_c_before.txt`). Fixed there only: on the
   climb the road does not push on a body the right way up (the wheels carry it; `BodyGlide`, `VehicleSurfaceContacts.Glide`, track `body_glide`).
   After: no swing on the climb for either vehicle (`round56_check_final_a_c.txt`). Elaine's valley also made one even vertical curve (x 3155 to 3078).
D DONE. Two flower pots (Round 54's, at the Joel terrace's street corners); no purpose for a racer, removed.
Races: forward x2 and reverse x1, Hard, traffic Normal, 9 of 9 AI finished; the AI took the Elaine jump 5 times (`round56_check_races.txt`).
Builds: seven full builds and one stopped (two layout tries, the barrier move, the barricade moved and drawn in place, the jerk's fix twice).
`Builds/ChastainRush/` 2026-10-21 20:52. Imported from Racer: nothing.
For Dan to check: the Elaine jump flat out (land on Crawford, the way back round 1143) and short (into the brush; a tie with the lap: enough?); the barricade and
tire marks; Elaine's valley at speed (the jerk); Joel Drive at his spot.

## Previous delivery 3: Round 55
**Reviewed: Dan drove the Round 55 build 2026-10-21 17:00 (Addison Heights forward): the Elaine shortcut is "still not quite hitting the mark" (Round 56); B to E no complaint.**
**Round 55, THE ELAINE JUMP REDONE WHERE DAN MEANT IT, the start line moved off it, the bounce house's sound, two bumps, AI that passes (Dan,
2026-10-21 12:28; reports BUG-001 to BUG-003 of session `2026-10-21_12-16-40-990_d377b3`, read by Claude, after the Round 54 exe:
Addison Heights forward and reverse on the Drifter Twin).** Round 54 is reviewed. Standing rules apply. ONE full Build Map at the end
(plan the line on paper first, the run without the stunt included; quick builds while working). Tuning limit. Short results.
A. THE ELAINE SHORTCUT (FORWARD), REDONE. Dan: "the new forward one is a fail. It looks like it did literally nothing but mark a line
   on the map, not to mention that it bypasses the feature I wanted to utilize which is the place in the bug report". BUG-002 (x 3053.14
   z 309.33 h 271, ON Elaine Circle heading west, at the west end of its straight where it bends north to Joel): "The shortcut makes no
   sense at all. Not even sure where it wants me to go. Plus where you start you are too close to the end of this so it is really
   confusing because starting from back it looks like a shortcut at the very beginning of the race. I wanted the jump here as it is a
   natural jump."
   Claude's mistake in Round 53 B: the jump is NOT the crest at the Suholden corner (Dan's Round 53 BUG-005 is only where you turn off
   the lap onto Elaine); it is HERE, at Elaine's west bend: drive west along Elaine from the corner, and instead of following the bend
   north, go straight off its edge: the ground drops away west and you fly (Dan, Round 53: "this jump will miss the track most likely.
   At least every time I tried it. Perhaps we can build off where it lands, have the path be a little longer before you join the main
   road"). So:
   (1) The way: off the lap at the Suholden / Elaine corner, west along Elaine's own road to the bend, straight off the edge at Dan's
       spot (a natural launch: keep the ground's own lip, smooth it only so it launches cleanly and straight, no ramp). Elaine north of
       the bend, on to Joel, stays closed (barrier) so there is no plain road way round.
   (2) The landing: drive it at race speed (several vehicles) and find where cars really come down; build there: a clear landing
       (graded, drivable) and a visible way on from it (worn dirt path / tire tracks through the yards and trees, cleared of trunks,
       readable at speed) that is a little longer and joins the lap heading the race's way (Hayworth Drive or Joel Drive northbound /
       westbound, Code picks and says why). It must LOOK like a way: someone seeing it from Elaine knows where to go.
       Dan, 12:33: "that is why before I asked for a landing spot that forced the user to drive through some backyards and stuff to
       get back to the road to get rid of part of the advantage it got, this is especially the case because you will likely overshoot
       your the road anyway." So expect cars to overshoot Hayworth and come down in the yards beyond it: the way back from there winds
       through those backyards (round sheds, between fences, past play sets: breakable fences and the like are fine) before it reaches
       the road. That backyard run is what takes back most of the time the jump gains.
   (3) Throw away Round 54 A1's valley way to Crawford and its beds (the map line Dan saw), and any of its leftovers.
   (4) A clean run saves a few seconds; missed (short, off line) costs more; no free way round (standing rules). Code reports the numbers.
       It skips about 350 m of lap (Claude's estimate); if a sensible-looking way cannot bring it down to a few seconds, stop and report.
B. THE START / FINISH MOVED AGAIN. Round 54 put it on Crawford Circle's east leg (z 245), just before where A rejoins: from the grid it
   looks like a shortcut at the start. Put the line where it is not in the part A skips (Suholden south, Kelly, Crawford, Hayworth up to
   A's rejoin) and not in sight of A's ends from the grid, on as straight and level a stretch as the lap has (Joel / Embry / Suholden
   Circle); Code picks and says why. Both directions; records (keep old times apart as Round 54 did), checkpoints, grid, AI to suit.
C. THE BOUNCE HOUSE'S SOUND. Dan: "the bouncy castle should use the same sound effect as the trampoline instead of making no sound at
   all." Use the trampoline's bounce sound (`Audio/SoundDropIns.cs` Bounces, Dan's drop-in or the made one, one at random per bounce).
D. BUMPS. BUG-001 (x 2977.66 z 308.73 h 14, Hayworth Drive at Crawford Circle): "smooth this bump". BUG-003 (x 3136.36 z 97.41 h 6,
   Kelly Drive at Suholden Circle): "smooth this". Smooth both junctions.
E. THE AI DRIVES ROUND CARS (all tracks). Dan, 12:33: "the AI drivers need to be smart enough to drive around cars. I think this is part
   of the problem. I see them stuck to bumpers as opposed to trying to get around other vehicles". Race AI must overtake: when a traffic
   car, a parked car or another racer is ahead and slower, pull out and pass where the road (or the grass beside it) has room, else
   hold back a car length and take the next gap; never sit pushing a bumper. This is likely part of why the AI is slow on Addison
   Heights (Round 52 and 54's traffic queues). Check on Addison Heights and one other track with traffic Normal: count AI passes, time
   spent stuck behind a car (before / after), all finish.
Check: A clean (time saved, Street Classic and Drifter Twin), short, and the ways round; the AI takes it sometimes; forward and reverse
races x2, traffic Normal, all finish. Pictures: Dan's spot looking west (the drop), the landing, the way on from it, the new start line
from the grid, the two junctions.
For Dan to check: the Elaine jump and its way on through the backyards; the new start / finish; the bounce house's sound; the two bumps;
the AI passing traffic and each other.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round55/` (check reports,
pictures). Bug reports read: session `2026-10-21_12-16-40-990_d377b3`, BUG-001 to 003.
A BUILT AS DAN'S OPTION 2 (Dan, 2026-10-21 after the first report: "option 2"); TIME NOT MET, TUNING LIMIT REACHED. Round 54's valley way is gone. Measured
   first (`round55_check_a_b_c.txt`): up Elaine's 26 % cars reach the bend at 14 to 21 m/s and come down on the bank 35 to 46 m out, just above the
   Crawford / Hayworth corner (with nothing there: lap 25.0 s, land and turn at the corner 14.4 s, Drifter Twin). Built: off the bend's own edge (no ramp),
   worn tracks down the bank (the hint), a landing zone, a graded bench curving right off the landing, north between 960 and the hedge, an S through the
   wedge, onto Hayworth through a gap at z 376 to 392 (forgives CP 2 to 5); a 6 m hedge along the bank's foot from Crawford Circle up Hayworth's east side to
   z 373; Elaine barred north of the bend (16 m wide, forward), hedges at its ends, across 960's yard and across the wedge; the 1007 / 2090 bank hedged.
   Numbers (`round55_check_a_option2_*.txt`; the scripted driver lands at 20 m/s about 20 m from the hedge and must turn hard right, so runs vary by 3 s):
   clean, Street Classic saves 8.7 s (try 1) and 4.6 s (try 2), and in the final build ran wide into the hedge; Drifter Twin saves 5.4, 5.0 and 4.9 s.
   Ways round, final build: straight down through the hedge at the corner 18.6 s (cars can bounce over a 6 m hedge off the bank; 8.5 m caught the clean
   run too, tried and reverted); off the bench west through the hedge 25.2 s; over the edge slowly and the bench 20.1 s (the same way, no air); round the
   barrier through 960's drive stopped in the hedges (it was 16.5 s before the barrier was widened). Lap's way 28.3 s (Street Classic), 26.0 s (Drifter).
   The AI took it 5 times in two forward races (try 2's layout), every AI finished.
B DONE. The line on Embry Lane's east end at (3246, 457) (5 to 9 %, straight; every flatter straight is inside a shortcut's skipped part or too short),
   60 m lead-in from the corner; the reverse grid ends short of Joel Drive. Records as "Addison Heights (Embry Lane line)"; a checkpoint on Crawford
   Circle where the line was. Both grids on the road (check); no shortcut end in sight of them (`55_B_*_grid.png`).
C DONE. The bounce house's voice stood at the world's origin (never heard); now at the castle, playing the trampoline's sound (Dan's drop-in
   `trampoline.mp3` or the made one). Check: thrown, playing, 3 m from the arch.
D DONE. Both were ridges the levelling left on the corners' outsides: Kelly / Suholden (pitch under a car 0, 5, 12, 18, 14, 8 %) now 3 to 8 % with no
   spike; Hayworth / Crawford (0.31 m and 0.6 m crests over +-3 m) blurred, and the 8 % into 22 % grade change made a 24 m vertical curve
   (`MapBuilder.Round55.cs`): no crest left, a sag of 0.17 to 0.20 m over +-3 m remains where the bend and the grade change meet (two tries; `bumps.txt`).
E DONE (modest). The AI pulls out round a slower car once the other lane is clear and goes on past instead of stopping on its bumper; otherwise it waits
   a car length back. Hard, traffic Normal, every AI finished (8 races, 24 drivers): close behind a car under 10 m/s on Addison Heights 23.7 s per driver
   before, 17.1 s after; Chastain Lakes about 2 s before and after. Passes 14 to 18 per driver both ways (`round55_check_e_before.txt`, `_after.txt`).
Builds: nine full builds (the junction curve took two; option 2 took five: layout, two tunings, the barrier, the landing arc). `Builds/ChastainRush/` 2026-10-21 16:11. Imported from Racer: nothing.
For Dan to check: the Elaine jump (land, brake hard, right onto the bench; is the corner through the hedge too easy?); the new start / finish both ways; the bounce house's sound; the two bumps; the AI round traffic.

## Technical notes (standing)
Technical Notes / Blockers

* **The map extension (Round 59).** `python Tools/mapgen/mapgen.py` makes the core (config `bounds`) exactly as before, then, with config `extension`, appends the
  new land (`mapgen_extend.py`): negative x / z, map.json `extent_m` [x0, z0, x1, z1] and `extension.first` (where each list's new entries start), trees appended to
  trees.bin, `Assets/Generated/terrain_full.r16` / `.json` (origin_x_m / origin_z_m) for Unity, `Data/heightmap_ext_1m.tif`. The core's terrain.r16 / terrain.json stay
  for the python scripts. Caches in `raw/`: `osm_ext1.json`, `naip_ext1_*.tif`. Then `python Tools/mapgen/add_extension_places.py` (Town Center and KSU destinations into
  eggs.json; re-run after make_eggs.py). Scripts that pick roads by NAME (route scripts, neighborhoods_rest.py) would now also see the new land's roads (Chastain Road
  runs on west): when re-running one, keep it to `roads[:extension.first.roads]` or check its output's diff. Round 59 check: `ChastainRush.exe -exeRound59 <dir>
  [-r59Only load,fps,drive,pics59] -r59Drives local/round59/r59_places.json` (from `python Tools/mapgen/ext_drives.py`); the races: `-exeRound58 <dir> -r58Only aitable`.

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
