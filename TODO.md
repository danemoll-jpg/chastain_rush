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
**Round 71, A PLANE RACE THROUGH RINGS (Dan, 2026-10-26 03:09: "I am thinking that we could do a plane race. I think just the typical fly
through the rings course. We could utilize the quary right next to the airport and just expand it. Since it is the sky we are pretty open.
I would like to have low spots and maybe have to fly under bridges if possible. It could be a circuit. It could be a race with others or
solo speed run"). Dan is asleep and has not tested Round 70: decide, write it down, go on.** Builds on Round 69's Sky Hopper. Quick builds
while working; ONE full Build Map at the end if anything in the world changes (rings are game objects; likely code / data only).
Tuning limit. Short results.
A. THE COURSE: a circuit of rings in the sky starting and finishing at Cobb County International Airport, built round Viking Quarry Lake next
   door and expanding out from it (Code designs it and says why). Low spots: down into the quarry, low over its lake, skimming fields and
   the woods' tops; under bridges where the plane fits (the I-75 / I-575 overpasses and their ramps near Big Shanty / Chastain Road / Barrett
   Parkway: check the clearance with the plane's wingspan and height, Round 69's wing-tip test); a few high rings to climb for and dive
   from; banked turns between them. A lap of about 2 to 3 minutes at race pace; 2 or 3 laps, Code's pick. Name it (e.g. "Quarry Air Race"),
   Dan may rename.
B. THE RINGS: big readable hoops (a bright color, a beacon / arrow to the next one, a chime and a flash going through), sized to the spot
   (larger in the open, tighter under bridges and in the quarry); a ring missed counts as a missed checkpoint (the game's usual penalty, the
   lap still counts: Round 54 B's rule). Crashing resets the plane in the air just before the last ring passed, heading through it, at
   speed (never on the ground, never stuck).
C. THE START: from the runway, lined up (a standing take-off start: throttle up at GO), or an air start if the runway start plays badly with
   several planes (Code picks, says why).
D. TWO WAYS TO PLAY, both in the race menu (and Free Roam's race list): RACE against AI pilots (3 by default, as the car races; the AI flies
   the ring line with skill by difficulty, Hard close to a good player, no cheating through walls, recovers from crashes as the cars' AI)
   and TIME TRIAL / SPEED RUN solo (best lap and best total, ghost of the best lap, Top 10 records, replays: as every course). The plane only
   (the Sky Hopper; the helicopter is not in this race).
E. Built like the other courses where it applies: records, ghosts, replays (the replay cameras follow planes), the map and minimap showing
   the rings, the loading screen's inset, the finish screen and COMPLETE RACE.
Check: a lap by script flown cleanly through every ring (no ring unreachable, no bridge clipped); a lap with a missed ring (penalty, lap
counts); a crash and its reset; the AI x2 on Hard, all finish; a time trial with its ghost. Pictures: the start, the quarry dive, under a
bridge, a high ring, the whole circuit on the map.
For Dan to check: race the planes and fly the time trial (rings, the quarry, under the bridges, the AI).

## QUEUED rounds
**Round 72, EVEL KNIEVEL JUMPS AT VIKING QUARRY LAKE, and four known fixes (Dan, 2026-10-26 07:17: picked "Quarry stunt jumps" to run
while he is at jury duty; his idea 2026-10-25 02:11: "stunt races that have large ramps to jump you over evel knievel style"). START RIGHT
AFTER ROUND 71 IS DONE AND COMMITTED, without waiting for Dan; he is away all day: decide, write it down, go on.** Quick builds while
working; ONE full Build Map at the end. Standing rules (real physics off every ramp; nothing slows a car on a ramp; landings clean).
Tuning limit. Short results.
A. THE QUARRY JUMPS (Free Roam events, the stunt park's family). Big steel stunt ramps on the quarry's rim, Evel Knievel style: a long
   straight run-up lane, a tall ramp with a starting gantry (stars and stripes bunting fine, no real names / brands), a landing ramp on the
   far side across the quarry lake, a crowd in bleachers, a PA voice-free fanfare. Three jumps of rising size (Code designs them from the
   flight, as Round 68): a short one across a corner of the lake, a long one across the middle, and THE BIG ONE across the widest part (the
   longest jump in the game after the mountain ramp, Round 70 A). Miss the landing ramp and you're in the lake (the game's water: reset).
   Scored events: distance, height, landing (clean / rough / crash), with Top 10 records per jump; a run-up countdown start; a replay of
   the jump (Round 57's replays: the TV cameras placed for the flight). Rewards / unlocks: none yet (the campaign is held). Fast travel to
   the quarry's start gantry. Reachable by any ground vehicle, including bikes and stunt vehicles; flying vehicles not in the events.
B. KNOWN FIXES (found by Code in earlier rounds, not yet asked for by Dan; small):
   B1. Three heavy cars (Longroof GT, Highball Fastback, Skyfin Cruiser) stop hard at 36 to 39 m/s on Kennesaw Mountain Drive at
       (-500, -5320) (Rounds 67, 68): find and fix.
   B2. Canton Road's three map-edge closures (Round 66): a car can end up on top of the barriers; make them impossible to climb.
   B3. Round 64 C's junction levelling: drives with air over 0.15 s went up; find the worst junctions in the jolt table and smooth them.
   B4. Round 68's J2 kicker: the Drifter Twin flat out loses 12 m/s on its top; ease it without shrinking the jump (no more than two tries).
Check: each jump flown flat out on the Drifter Twin, the Street Classic, a bike and a stunt vehicle (distance, air, landing), short (into
the lake, reset), and the scoring and records; B1 to B4 before / after. Pictures: the three ramps from the run-up, mid-air over the lake,
the crowd, the landing.
For Dan to check: jump the quarry (all three); the four fixes.

Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 70
**Not yet driven by Dan: he moved on to Round 71 on 2026-10-26 03:09 ("Don't have a chance to test").**
**Round 70, A RAMP OFF THE MOUNTAIN; AI THAT RACES; the garage and the saved vehicle; park visitors; the mountain road (Dan, 2026-10-25
23:20; reports BUG-001 to BUG-004 of session `2026-10-25_22-20-32-569_189622`, read by Claude, after the Round 68 / 69 build: Kennesaw Mountain on the Trail
Four).** Rounds 68 and 69 are reviewed. Quick builds while working; ONE full Build Map at the end. Tuning limit. Short results.
A. A RAMP THAT CLEARS THE MOUNTAIN (Free Roam; later maybe a point-to-point race). Dan: "I am not sure about a reverse of the kennesaw
   mountain track. But I want a big ramp on the edge that drops all the way off the mountain. This maybe an eventual Point to point race,
   but at the moment just give me a big ramp that will clear the mountain". Woodstock Rush's summit flight in spirit (Racer's CR101-104:
   a 215 m straight run-up from rest, 36 to 43 m/s at the lip, a flight that cleared the whole mountain to a catch slope). Build one big
   ramp at the summit's edge (a built ramp is fine here: steel or timber, a real thing a stunt crew would put up), with a long straight run-up
   on the summit (the lot / the drive's top / a cleared line), aimed where the fall is longest and the landing open (Code picks the side and
   says why), so a car flat out flies off the mountain and comes down far below at its foot on a long smooth catch slope (cleared, graded,
   landing clean and keeping speed), with a way back to the roads. Target: the biggest flight in the game by far: 8 s or more in the air and
   several hundred meters (report air, distance, height for the Drifter Twin, the Street Classic, a bike). A fast-travel spot at the run-up's
   start; visible from the summit lot (no "JUMP" signs needed: the ramp is obvious). Not in the race's lap (Round 68's course unchanged).
B. THE AI IS TOO SLOW. Dan: "The AI is really slow. I watched the replay and they dont seem to be getting hung up. I think their issues
   maybe either they aren't going full throttle or possibly just follow the courses too exactly. For example there are times when you make
   a path for the next checkpoint that doesn't follow the exact roads, I think the ai should be not so slavish to the actual roads".
   (1) Measure first, from the replays' recordings (Round 57): per AI on Hard, throttle use (time at full throttle on straights), speed
       against the player's on the same stretch (Dan's best laps / ghosts where saved), and where the time goes (corners, braking early,
       following the road's curves where the course allows a straighter line).
   (2) Racing lines: the AI drives a racing line between checkpoints (cut corners on grass, lots and open ground where the course allows
       it, use the whole road, apexes), not the road's center line; full throttle wherever it can; braking as late as the vehicle allows.
   (3) Hard races Dan close on every track (within a few seconds of his best laps); Normal and Easy scale down by pace, not by dumb driving.
       Report a table per track: Dan's best lap, Hard's best and average, before / after.
C. THE GARAGE IN RACE SETUP. Dan: "In race setup, I am not able to select a car in the garage some times". Find when it fails (which
   vehicles, after what, pad or keys) and fix it. Dan, 23:27: "I started a race as soon as the game started, it ended and that was when I
   tried to select a vehicle. I hadn't touched the plane at that point": so reproduce exactly that first: launch the game, start a race
   straight from the title (no Free Roam first), finish it, go to race setup's garage from the finish screen / menu and try to change the
   vehicle (pad and keys). Dan, 23:28: "I had to go into free roam to change vehicles so I know that worked. I couldn't do it in race
   setup": Free Roam's vehicle change works; the fault is in race setup's garage (after a finished race). Then also after Free Roam; check every
   vehicle selectable in race setup by pad and keys, after Free Roam in the helicopter and in the plane.
D. THE SAVED VEHICLE. Dan: "why does my vehicle keep getting changed every time I start the game?" (`vehicle.json` holds "plane", Free
   Roam's last pick; the log shows the game starting on the Trail Four). Remember Dan's choice: the last ground vehicle for races and the
   last Free Roam vehicle separately, and start each with what he last chose (never a random or substitute one; a flying vehicle only where
   it is allowed). Also: `vehicle.json`'s key "colour" -> "color" (American English rule), reading the old key once.
E. PARK VISITORS. Dan: "I thought there were supposed to be joggers and walkers on the kennesaw mnt trail". Round 67 added up to 9 groups in
   Free Roam and 2 in a race; Round 68 moved the race onto new lines. Make them clearly there: walkers, joggers, hikers with dogs on the
   park's trails and at the summit and visitor center lots in Free Roam (plenty), and a few beside the race's lines in races (they get
   out of the way). Check by pictures from the summit and the trails.
F. THE MOUNTAIN ROAD.
F1. BUG-002 (x -1097.14 z -6077.09, Kennesaw Mountain Drive): "why so many fire hydrants? You wouldn't have them on a mountian path at alll";
   BUG-003 (x -1072.23 z -5825.67, the drive): "again". No hydrants on the mountain drive, the park's roads and trails, or any road through
   woods / parkland with no buildings near: hydrants only where there are buildings along the street (all the map, by rule, not just here).
F2. BUG-004 (x -949.83 z -5725.88 h 52, the drive near the summit, a jump on the course): "the road shouldn't be doing this. It should be
   flat while still keeping this jump" (the picture: the road rolls steeply sideways over the crest). Keep the jump's lift but make the
   road flat across its width there (no camber / roll); check the jump's flight is unchanged.
F3. BUG-001 (x -872.73 z -5208.19 h 32, the start line on the drive): "why is this on the ground?" (the picture: the start line's checkered
   banner / gantry sits low, its left end down in the ground on the slope). Stand the start gantry up properly over the road (posts on the
   ground both sides, banner level and clear over the road); check every course's start / finish gantry the same way.
Check: A's flight table; B's tables (before / after) and an AI race on Hard on three tracks; C every vehicle in race setup; D restart the
game twice after Free Roam in the plane and after a race (the right vehicle each time); E pictures; F1 hydrant counts on the drive before /
after; F2 the road's roll before / after; F3 pictures of each start gantry.
For Dan to check: fly off the mountain; race the AI on Hard; the garage; restart the game; the park's visitors; the mountain road.

**Status (Claude, 2026-10-26): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round70/` (`round70_check_before.txt`, `_after.txt`, `_final.txt`, `round70_ai_*.txt`, `round70_vsave1-3.txt`, pictures `70_*.png`). Bug reports read: session `2026-10-25_22-20-32-569_189622`, BUG-001 to 004.
A DONE. The summit ramp (`make_summit_ramp.py`, `MapBuilder.SummitRamp.cs`): the drive ring round the knob leaves no 200 m straight on top and the north-east run is J1, so the run-up is the drive's south-flank straight (258 m, about +9 %, fast travel "Kennesaw Mountain: the big ramp" at its start): straight on past the hairpin round the knob's west end, a steel ramp on trestles (79 m, 21 degrees, its lip 57 m over the falling face) out over the north-west face at 296 degrees, the line threading Greymont Circle's houses (none within 35 m of it for 960 m). Catch on the graded foot, a gravel arrester, a track on to Old Mountain Road NW. Flat out from rest (`70_ramp_*.png`): Drifter Twin 8.6 s / 341 m / 174 m up; Street Classic 8.4 s / 296 m / 154 m; Needle 600 8.9 s / 358 m / 180 m; all land upright and roll to a stop at Old Mountain Road (11 to 14 m/s). In races concrete barriers close its mouth. Visible from the drive and the run-up; from the summit lot itself the knob hides most of it (`70_ramp_from_summit_lot.png`).
B PARTLY (tuning limit, two attempts). Measured first from Dan's replays (`Tools/race/replay_ai_table.py`) and a trace of what set the AI's speed (`ai_trace_why.py`): throttle use was like Dan's; the time went to phantom bends from the road's raw centerline, held slow zones (the trail's turns 11 to 14 m/s where Dan does 23 to 41), Kennesaw's 36 m/s hill cap, barriers and roadside things seen as obstacles, held jump zones that ignored bends. Now: a racing line through the lane and the open shoulder (whole road, apexes, grass and lots where clear), bends from it, zones within its bend limit, off-line things ignored, trail zones raised, Hard's pace .97 to 1.0, Easy / Normal by pace (88 / 95 %). Hard, flying laps, best / average (Dan's best): Kennesaw 157.2 / 166.4 -> 141.8 / 144.5 (113.2); Noonday Creek Trail 230.0 / 234.7 -> 200.9 / 212.9 (169.2); Chastain Glen 39.6 / 44.4 -> 39.1 / 43.9 (38.9); Chastain Lakes 88.8 / 104.0 -> 91.7 / 94.3 (72.1). Not within a few seconds except the Glen; part of the gap is the AI's vehicles (Street Classic, Longroof GT against Dan's Trail Four / Drifter Twin).
C DONE. A finished race counted as "racing", so race setup's garage shut its vehicle row. After a race started at once and finished: all 11 racing vehicles chosen by pad and by keys; also after Free Roam in the helicopter and the plane.
D DONE. `vehicle.json` keeps "roam" (Free Roam's last pick, the plane too) and "race" (the last ground vehicle chosen), "color" (the old "colour" / "vehicle" read once); forced swaps (a route's vehicles, police, stunts) are never saved. Three restarts from Dan's old file: the game starts in the plane, race setup puts on the Drifter Twin each time.
E DONE. Kennesaw's park: up to 12 groups near a view in the park's hours (was the streets' 2 to 8), the summit and visitor center lots walked too, hikers with dogs, joggers, couples, families (no bikes); 5 at most in a race. Measured 12 to 15 groups at the summit lot, visitor center and trails, 5 in a race (`70_park_*.png`).
F1 DONE. Hydrants only where a building stands within 60 m, and none on the park's own roads: Kennesaw Mountain's drive and park 47 -> 0 (7 left in the measuring box are on the neighborhood streets north-east of the visitor center).
F2 DONE. J1's bench widened to the drive's far edge (the drive lay half on it): roll at BUG-004 37.6 -> 3.6 degrees; J1's line and flight unchanged (same profile).
F3 DONE. The start / finish gantry upright, each post on its own ground (in closer where the bank climbs), the banner level over the higher side; every course pictured (`70_gantry_*.png`).
Found, not changed: the Round 20 P1 check (`Round20Runner.Ramp.cs`) no longer matches P1 as rebuilt since (no vehicle leaves its lip in that script); nothing there was touched this round.
Builds: 2 full Build Maps (the second for F1's park roads), 3 code-only exes. `Builds/ChastainRush/` 2026-10-26 02:20. Imported from Racer: nothing.
For Dan: (a) the ramp: the run-up along the drive and off the hairpin; (b) the AI on Hard; (c) the ramp's look (57 m of steel trestle).

## Previous delivery 2: Round 69
**Reviewed: Dan drove the Round 68 / 69 build 2026-10-25 22:20 (Kennesaw Mountain); his notes are Round 70.**
**Round 69, A PLANE TO FLY (Dan, 2026-10-25 12:39: chose "planes" for the next round; he is away and has not tested Round 68). START
RIGHT AFTER ROUND 68 IS DONE AND COMMITTED, without waiting for Dan.** Builds on Round 63's helicopter (flight code, camera, map, resets,
far view) and Round 66's edges (warning, headwind, ceiling). Quick builds while working; ONE full Build Map at the end (the airport's
parked planes). Tuning limit. Short results. Dan is not watching: decide, write it down, go on.
1. THE PLANE: a small single-engine high-wing plane of our own (Blender, as the helicopter; no real maker's name or livery), "Sky Hopper"
   or Code's name. Arcade flight, not a simulator: it must be easy and fun. Throttle up / down; pitch, roll (banking turns the plane
   by itself), a little rudder; it holds level flight hands-off at its speed; no punishing stalls (too slow and the nose eases down until
   it has speed again). Cruise fast enough to cross the map in two or three minutes, a top speed above that; engine and wind sound with
   speed; flaps not needed. Pad and keyboard layouts that match the helicopter's where they can (Code picks, says them in the status).
2. TAKE-OFF AND LANDING: it rolls on its wheels, steers on the ground, takes off from Cobb County International's runways (and any long
   straight road or field, Free Roam being Free Roam), lands on them: a soft landing rolls out, a hard one or a wing into the ground is a
   forgiving crash (as the helicopter: a moment, then reset onto open ground nearby, never stuck). Hitting trees, buildings or wires:
   the same.
3. WHERE: two or three planes parked on the airport's apron (drive up and swap, as the helicopter pads: A / H); "plane" in Free Roam's
   vehicle choice (starting lined up on a runway). Free Roam only, never in races. A little airport life if cheap: one plane that now and
   then taxis, takes off, circles and lands (no collisions to worry the player).
4. THE WORLD FROM A PLANE: Round 63's far view and Round 66's scenery ring and horizon (Atlanta) at plane heights and speeds; the edge
   warning and headwind turn the plane back too (a wide gentle turn), and a ceiling. Frame rate at 300 m and 600 m up crossing the map
   against Round 63's numbers. Cameras: chase, far chase, cockpit, and a free look; the readout (height, speed, heading, climb).
5. FUN: low passes over the quarry lake (Viking Quarry, next to the airport) and down the I-75 / I-575 corridor should feel fast; nothing
   blocks flying under the highway bridges if there is room.
Check: take off from each runway, fly to Kennesaw Mountain, Town Center and Chastain Glen and back, land on the runway; crash into a tree,
a building and the ground (reset, not stuck); hands-off level flight 30 s; the edge turn-back; the fps table. Pictures: take-off, over the
quarry, over Town Center, Atlanta on the horizon from 600 m, the apron.
For Dan to check: fly the plane (controls, take-off, landing, speed); the view from up high.

**Status (Claude, 2026-10-25): built and checked in the exe by script, not yet flown by Dan.** Evidence `Data/screenshots/round69/` (`round69_check_final.txt`, `round69_check_fps.txt`, pictures `69_*.png`).
1 DONE. "Sky Hopper" (`Tools/Blender/plane.py`, our own high-wing single-engine plane, 11 m wings, tricycle gear; `VehicleProfile.Plane`, Flies: Free Roam only). Flown by `Vehicle/PlaneFlight.cs` (+ `.Crash.cs`, `.Effects.cs`): on its wheels the vehicle's own drive; in the air arcade flight: the throttle sets the speed (top 115 m/s, cruise about 85 at 75 %: the map in about 2.5 minutes), climbing costs speed and diving gains it, the bank turns it, hands-off it levels its wings and holds its height (30 s: height within 0.1 m, heading 2.2 degrees), slow (under 36 m/s) it sinks and the nose eases down (no stall, no spin). Engine buzz made in code, pitched by the throttle; wind with speed; readout (height, speed, heading, climb, throttle).
   Pad: left stick pitch (pull back: nose up) and bank; RT / LT throttle up / down (LT brakes on the ground); LB / RB rudder; X view; right stick looks round; Y reset; A take / leave a parked plane. Keyboard: W / S throttle (S brakes on the ground); arrows: up / down nose down / up, left / right bank; A / D rudder (and steer on the ground); V view; R reset; H take / leave.
2 DONE. Take-off from runway 09/27 (the airport's one runway, both ways): full throttle, past 30 m/s pull back: off after 157 m (09) / 168 m (27). Any long straight road or field works the same. Landing: touching down under 5 m/s, wings within 18 degrees, it rolls out on its wheels (the check: 32 m/s, 1.7 m/s down, stopped in 25 m). Harder, a wing, the nose or the body into anything, a tree's crown or water: a crash, then after 2.6 s put on the nearest open strip it can take off from again (130 m clear and flat, within 1.5 km; else lined up on the runway). Into a tree, Town Center's front and the ground: crashed, reset, rolled away again (6 of 6).
3 DONE. Three planes parked on the south apron (`Vehicle/PlaneStands.cs`, built at run time: no Build Map step): A / H takes one and leaves it (2 of 2); "Sky Hopper" in Free Roam's garage starts lined up on runway 09. Never in races. Airport life (`Vehicle/AirportCircuit.cs`): one red Sky Hopper taxis out every few minutes, takes off, flies a circuit at 300 m and lands (visual only).
4 PARTLY. The far view and Round 66's ring and horizon work from the plane (Atlanta from 600 m, `69_atlanta_from_600m.png`). The edge: LEAVING THE SURVEY AREA, then a headwind and a wide bank back by itself (the stick's bank overruled): at most 805 m out at full throttle, turned back in 27 s. Ceiling 1700 m as the helicopter. Cameras: chase, far chase, cockpit, the right stick's free look. Frame rate (1280 x 720, not measured at 3840 x 2160; real time, the last 1.5 km to each place): at 300 m Kennesaw Mountain 54 / Town Center 43 / Chastain Glen 82 fps; at 600 m 61 / 56 / 94; 1 % lows 9 to 24 (hitches as the far detail loads in at 95 m/s). Lower than Round 63's helicopter numbers (88 to 164 at 500 m from a still camera): not profiled this round.
5 Low passes over Viking Quarry Lake and the highway corridors: nothing blocks flying under the bridges where the plane fits (its wing tips and nose test what they meet).
Builds: one full Build Map (18:07, nothing in the world changed), then a code-only exe for the frame-rate check's fix (check code only) `Builds/ChastainRush/` 2026-10-25 18:27. Imported from Racer: nothing.
For Dan: (a) the controls and how it flies (speed, turning, the landing); (b) the frame rate at speed over Town Center and the mountain; (c) the plane's look (`69_model.png`, `69_apron.png`).

## Previous delivery 3: Round 68
**Reviewed: Dan drove the Round 68 / 69 build 2026-10-25 22:20 (Kennesaw Mountain); his notes are Round 70.**
**Round 68, KENNESAW MOUNTAIN: REALLY BIG JUMPS (Dan, 2026-10-25 08:35, after driving Round 67).** Dan: "I specifically wanted to do a
mountain trail so I have really big jumps but these are just little bunny hops. I know if you can see it in Woodstock Rush how big the
jumps I had on the Mountain trail, this is what I want just really large jumps. As far as this I had bigger jumps in my own neighborhood
than I got in the mountains...cmon". Claude's brief for Round 67 said "huge" with no numbers; Round 67 built six jumps of 1.1 to 2.3 s and
21 to 59 m. This round gives numbers. Use Round 65's quick builds (the mountain's step) while working; ONE full Build Map at the end.
Standing rules apply (real physics off every lip; nothing slows a car on the approach or the lip; clean landings).
1. THE MEASURE: Woodstock Rush's mountain (`Racer/`: its mountain trail jumps and the summit "homeward" flight, PROJECT_TODO_ARCHIVE.md line
   ~1989: 215 m straight run-up, 36 to 43 m/s at the lip, a flight that cleared the whole mountain to a catch slope). Read how Racer built
   them (run-up, lip angle, landing slope) and, if its build can be run minimized, measure its mountain jumps' air time and distance. Also
   measure this game's biggest neighborhood jumps (the Ridge leap, the boat jump, the kicker / carport, the mall drop). Put both in the
   status. Every mountain jump must beat every neighborhood jump.
2. THE TARGETS (design them in from the start: compute each flight's path from run-up speed and lip angle and shape the landing to it;
   do not tune small jumps up):
   - at least THREE HUGE jumps on the lap: 4 to 7 s in the air, 120 to 250 m long, the car 20 m or more above the ground below at the apex;
   - the rest BIG: 2.5 to 4 s, 60 to 120 m; nothing on the course under 2.5 s counts as one of the mountain's jumps (smaller bumps may stay
     but are not counted);
   - ONE SIGNATURE FLIGHT: off the summit (the overlook / the top of the trail) out over the mountainside, Woodstock Rush's summit flight in
     spirit: the longest air in the game, landing far down the mountain on a long smooth catch slope.
   How: use the mountain's height (about 240 m of drop on the trail; the game's 1.5x relief), long straight run-ups down the fall line so cars
   reach 35 to 45 m/s (cut straight lines through the woods where the trail zig-zags; clear and grade them), steep lips (25 to 35 degrees,
   still real-world things: rock ledges, cut faces, log ramps), and catch slopes that fall away under the flight and meet it at a shallow
   angle (long, smooth, wide, cleared), so the car lands clean and keeps its speed. Air comes from the ground falling away, not just the lip.
3. AT RACE SPEED, ALL VEHICLES: the targets hold for the Drifter Twin and the Street Classic at the speed a player reaches flat out, and the
   AI reaches the lips fast enough to fly big too (no braking before a lip; AI speed zones as needed). Bikes and ATVs land clean as well.
4. Keep Round 67's route, start, checkpoints and closures unless a run-up needs a change (then move checkpoints / AI to suit). Trees: no
   trunk anywhere near a flight's corridor or landing (breakable or gone).
Report: a table per jump: lip speed, air time, distance, apex height above the ground below, landing speed, for the Drifter Twin, the Street
Classic, a bike and the AI; next to Racer's and the neighborhood's numbers. Pictures mid-air on each huge jump and the signature flight.
Tuning limit as usual for fine adjustment, but the targets above are the job: if a target cannot be met somewhere, say where and why.
For Dan to check: race Kennesaw Mountain flat out; the signature flight off the summit.

**Status (Claude, 2026-10-25): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round68/` (`round68_check_final.txt`, pictures `68_midair_J*.png`, the design `68_design_profiles.png`, `68_layout.png`). No bug reports newer than Round 67's session.
1 THE MEASURE. Racer's mountain (its validation notes, `Racer/Docs/CR112-118/VALIDATION.md`, CR101-104; not re-run): gully flight 42 to 46 m/s, 5.9 to 6.4 s, 190 to 227 m; summit flight 40 to 43 m/s, 6.7 to 7.6 s, 191 to 228 m; its catch slopes a 0.75 grade. This game's neighborhood jumps flown the same way here (Drifter Twin / Street Classic, flat out): Rooftop leap 3.1 / 2.9 s, 59 / 52 m (the biggest); Elaine jump 2.4 s, 70 m; Parkway leap 2.7 / 1.3 s, 74 / 29 m; Creek leap 2.6 / 2.4 s, 56 / 50 m; Boat jump 1.9 s, 31 m; Carroway 1.8 / 1.6 s; the Ridge leap and the Lakes kicker gave no proper flight in the scripted drive (an aimed launch, a path issue): not measured. Every mountain jump beats all of these.
2 THE NEW DESCENT (`make_mountain_route.py` rewritten, `MapBuilder.Mountain.cs`): the climb up the drive unchanged; from the drive's last straight below the summit the race runs on three straight lines cut through the woods (14 m bench, 32 m wide over the catch slopes), each built round one jump designed from its flight (run-up speed from the vehicles' acceleration, lip angle, kicker radius, catch slope). J1 THE SUMMIT FLIGHT (the signature: the drive's straight is its run-up, a granite ledge at the top of the north-east face, 17 degrees), hairpin; J2 THE WEST FACE (a boulder ledge at the shoulder's brink, 14 degrees), hairpin; J3 THE EARTHWORKS (a timber-faced berm, 18 degrees, landing across the drive by the visitor center onto the finish straight). Lap 3526 m (was 3550), 10 checkpoints (CP5 to CP10 moved onto the new lines), closures unchanged. The old trail's bench and six small jumps are gone (the trail itself as Round 61 built it).
   Flat out, the real vehicle (air s / distance m / most height over the ground below m), all landing upright: Drifter Twin J1 5.5 / 223 / 44, J2 3.7 / 100 / 32, J3 4.5 / 171 / 44; Street Classic 4.8 / 174 / 38, 4.2 / 141 / 39, 4.2 / 144 / 40; Needle 600 5.8 / 243 / 45, 4.8 / 189 / 45, 4.5 / 183 / 46; Ridge Scrambler 5.5 / 226, 4.6 / 174, 4.5 / 174; Trail Four 5.6 / 227, 4.6 / 171, 4.5 / 172. Lip speeds 36 to 47 m/s, landing 40 to 57 m/s. At the AI's held speeds (Drifter Twin) 5.6 / 224, 4.4 / 155, 4.4 / 159. The signature is the longest air for every vehicle (it is shorter than Racer's summit flight: 4.8 to 5.8 s against 6.7 to 7.6).
   Not met: the Drifter Twin flat out loses 12 m/s on J2's kicker top (3.7 s; at the AI's speed 4.4 s). Two attempts (kicker radius 280 then 340 m): tuning limit.
3 LANDINGS (new code): the kickers' curve kept within what the suspension carries (radius 300 to 340 m: tighter ones put the body on the ramp and halved the speed); a 75 degree first 3 m behind each lip; on the mountain's catch slopes only, a vehicle touching down is turned to the slope with its pitch stopped (`RampHold`), and the body-contact prediction keeps the slope's own normal (`VehicleSurfaceContacts`: it held bodies 0.7 m over a 30 degree slope with no wheel down); the line's ends take the drive's own levelled height (a 0.7 m step was throwing cars at the departure).
4 AI: two races on Hard, 3 AI each, all 6 finish (5:06 to 6:01, stuck 64 s in all, 49 resets, most "blocked" ones). The lap on all 11 vehicles at the AI's speeds: round, no stop on the new lines. Trees: none within 16 m of the lines, breakable to 32 m and 38 m along each flight.
Found, not changed (not this round's ground): three heavy cars (Longroof GT, Highball Fastback, Skyfin Cruiser) stop hard at 36 to 39 m/s on the drive at (-500, -5320) (also in Round 67's check); the Turf Rocket hits a trunk beside the drive at (-1046, -6164).
Builds: 4 full Build Maps (the first did not compile), 7 quick builds, 4 code-only exes. `Builds/ChastainRush/` 2026-10-25 15:44. Imported from Racer: nothing.
For Dan: (a) race it flat out; the summit flight; (b) J3's kicker stands about 30 m over the ground at its lip (a long gentle curve so cars do not bottom out): its look; (c) the AI's resets.

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
