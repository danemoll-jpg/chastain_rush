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
**Round 80, AIR AI THAT MAKES MISTAKES, THE SQUARE AI AT THE DECK, A JUMP AT WHITLOCK, THE EXPRESS LANES' SCHEDULE AND GATES, BUSIER HIGHWAYS,
Round 79's found-not-changed items (Dan, 2026-10-28 17:45; report BUG-001 of session `2026-10-28_17-26-14-935_f93341`, read by Claude, after the Round 79
build; gate photo `local/reference_photos/express_lanes/express_lane_gate.png`).** Round 79 is reviewed. Item 0 and the end-of-round clean-up apply. ONE full
Build Map at the end (world changes: C, D, E). Tuning limit applies. Short results.
A. AIR RACE AI THAT CAN BE CAUGHT. Dan: "On the air race it seems the ai planes never make a mistake so if you make a mistake you can't make up for it".
   AI pilots (`Race/PlanePilot.cs`) fly a perfect line every lap. Give them human mistakes, scaled by difficulty (Easy most, Hard fewest but some): a
   late turn that runs wide, a ring missed and gone back for, a low or underpass ring taken slow and careful, a lap off the pace. Use the same
   catch-up the car AI uses, if it has one. Check: on every air course (Quarry, Highway, Air Tour) and each difficulty, 3 AI x 2 races: their missed
   rings and lap spread; and a human-like pilot that misses one ring and goes back for it can still win on Medium and finish in the pack on Hard
   (fix the check pilot's crash-looping, Round 79, so it can fly the courses). Report the numbers.
B. THE SQUARE AI AT THE PARKING DECK. Dan: "On the marietta square race seems the AI is confused about the parking deck turn. Some times I noticed
   them driving in circles before continuing". Find why (the Lawrence Street deck's turn in or out: a target they overshoot, the wrong level, a
   checkpoint they circle for) and fix it. Check: 5 square races x 3 AI, both a clean race and one with traffic and the train: no AI turns a full
   circle anywhere (trace heading; a loop = more than 300 degrees turned within 60 m), times through the deck before / after.
C. A JUMP AT WHITLOCK. BUG-001 (x 1608.55 z -8524.74 h 102, the square race's Whitlock Avenue crossing, stopped at the gate with the train passing):
   "why no jumps on this side?" Only Mill Street's crossing has a jump (the ballast pile). Give the Whitlock crossing its own way over the train,
   something different that belongs by a railroad (not another pile: e.g. a loading dock, a flatbed on a siding, a stack of ties), real physics,
   clears a train at the speed the course allows, clean landing, no free way round. Check: jumped over a passing train (picture), and driven round it.
D. ROUND 79's FOUND-NOT-CHANGED ITEMS. (1) Express Lanes 13's 780 m deck flies 2.06 s and lands hard (10.7 m/s) driven against its direction: with E
   the lanes run both ways, so smooth it both ways (and every Express Lanes deck: drive each both ways, list air and landings). (2) The replay's
   chase camera can sit inside the ballast pile by Mill Street: keep replay cameras out of solid things. (3) The plane's cockpit view sits behind
   the pilot: put it at the pilot's eyes (both planes). Pictures before / after for each.
E. THE EXPRESS LANES' SCHEDULE AND GATES. Dan: "they are always moving in just one direction. Please use these directions":
   Southbound (toward Atlanta) open 1:00 AM to 11:00 AM; reversal 11:00 AM to 1:30 PM; Northbound (away from Atlanta) open 1:30 PM to 11:00 PM;
   reversal 11:00 PM to 1:00 AM. Run it on the game's clock (`WorldClock`). Every Express Lanes entrance and exit gets a gate like Dan's photo: a
   red and white striped swing arm with red lamps along its top, a red DO NOT ENTER banner hung under it, the green EXPRESS LANE sign and the
   keep-left / keep-right sign by it. Closed entrances (and all of them during a reversal) have the arm down; open ones up. Traffic and police
   use the lanes only in the open direction, none during a reversal (cars already in finish their trip). The gates and the lanes' direction go into
   replays (Round 79's clock). The arms BREAK when hit (Dan, 18:02: "i want them to break, but you would be driving against traffic"): smash a
   closed gate and you are in the lanes the wrong way, with that direction's traffic coming at you. Check: the lanes at 6 AM, noon, 5 PM and midnight: gates
   (pictures, the photo's look), traffic counted each way.
F. BUSIER HIGHWAYS. Dan: "can we have more traffic so it is more realistic. Specifically the highways get busy". More traffic everywhere, the
   highways (I-75, I-575, Barrett Parkway, the Express Lanes when open) much busier: steady traffic in every lane, the AI and the player weaving
   through it. Frame rate at the busiest highway spot, the square and Town Center before / after (4K full screen for that one check, and a Steam
   Deck-sized window); if the cost is noticeable, report and Dan decides. Check: traffic counted on I-75 and I-575 before / after, pictures.
Check: item 0 for every item.
For Dan to check: an air race where you miss a ring; the square race's deck turn; the Whitlock jump; the Express Lanes at different times (gates,
direction); the highways' traffic; the cockpit view; smashing a closed gate.

**Status (Claude, 2026-10-29): built (full Build Map + exe, BUILD_INFO below), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round80/`. Three full Build Maps:
2 found Express Lanes 13's deck laid straight under three banks, 3 found the Whitlock dock 1 to 2 m into two buildings. Disk and build time at the end.
A PARTLY. AI pilots now err by difficulty (`PlanePilot`: a ring lined up wide, then mostly gone back for; a late turn; slow and careful at a low ring; a lap off the pace). 54 races
(3 courses x 3 levels x 2, 2 laps, `80_A_air_ai.csv`), every AI finished: rings missed per AI a race Easy / Normal / Hard: Quarry 2.2 / 1.2 / 0.8, Highway 0.7 / 0.3 / 0.7, Air Tour
4.5 / 3.7 / 2.7; one AI's two laps 4 to 48 s apart on average. The car AI has no catch-up, so neither has this. Found and fixed: a plane put past a ring after its third crash
was sent back before it again, round and round (Easy, Highway). NOT met (stopped at the tuning limit): the check's human-like pilot (the AI's own law flat out, 0.1 s late, noise,
one ring missed and gone back for) came last on every course, 1.5 to 2 min behind (e.g. Highway Normal 4:41 vs 2:47 to 3:04); most of that is the go-back itself (it runs out
650 m slowly before turning, about a minute) and 1 to 2 crashes from its lag. Through the pad it did not hold the line at all. A quicker go-back (for the AI too) is the next step if Dan wants it.
B DONE. The cause: the lap's line folded back on itself at both ends of the deck (Waddell's leg ran 13 m past the deck's lane, Haynes' started north of its exit), and the AI took
the nearest point anywhere: `make_marietta_route.py` legs and a de-kink at the deck; `RoadDriver` looks near its last station. 5 races x 3 AI, clean / Normal traffic, the train
as always: before 11 / 9 loops (all at the deck's entry, 1910, -8367), Waddell to Haynes 18.0 / 15.1 s; after 0 / 0 loops, 11.5 / 11.3 s (`80_B_route_before_after.png`).
C DONE. A stack of old ties west of the tracks (`tiestack`), the freight house's loading dock east of them the landing (`loadingdock`), `make_marietta_route.py`. Dan's place:
the stack beside the road, the train behind the gate (`80_BUG-001_after`). Off it at 21 / 24 m/s while a train crosses: 1.9 / 2.2 s, 2.1 / 2.3 m over the train car, down on the
dock (8 / 12 m/s), no reset (`80_C_wjump_*`). Too slow (under 18 m/s) meets the train. On the road the train stops you (27 to 67 s waited).
D1 PARTLY (tuning limit). EL13 is the I-75 lanes' 1833 m deck: Round 27 laid every chain of deck pieces straight, and there that ran 10 m under the Canton Road Connector and two
ramps (banks across the lanes: driven, stuck both ways). It now keeps its own shape (held over what crosses it, smoothed): through both ways, but 2.6 / 2.7 s in the air and 11 to
12 m/s landings at the connector and a ramp. Options for Dan: lift the lanes over the connector and the ramps as flyovers, or put those roads on decks over the lanes.
Found, not changed: the 463 m deck at I-75 / Barrett (-54, -2128) flies 1.9 s one way and walls at its south end (82, -2565) the other; small hops (0.4 to 0.8 s) on three short decks (`round80_check_ddecks.txt`).
D2 DONE. The chase camera's snap and the replay's own chase kept out of solid things: 0 of 16 moments at the Mill Street pile inside or behind it (`80_D2_*`).
D3 DONE. Both planes' cockpit view at the pilot's eyes: their hands on the stick and the dash ahead (`80_D3_cockpit_*`).
E DONE. `World/ExpressLanes.cs` (schedule on Free Roam's clock; races: Dawn 6:30, Day 3 PM, Dusk 7:30 PM, Night 2 AM; replays: the hour in the Clock events), `ExpressGate`
(4 gates, one per ramp; `make_express_signs.py`), `TrafficNetwork.SetDirection`, police paths `RoadNet.Blocked`. At 6 AM / noon / 5 PM / midnight every gate as scheduled
(`80_E_*_gate2`); at 5 PM 6 cars northbound, none southbound; none in a reversal. A shut gate smashed at 5 PM breaks (replays: `Kind.ExpressGate`); no oncoming car met in 116 m there.
F (numbers below). 10 / 22 / 34 cars (Light / Normal / Heavy, were 7 / 14 / 22); two in five kept for the highways; every lane of a multi-lane road used. Found: a long edge
(the highways', the Express Lanes') could only take a new car when its middle was near the player.
For Dan to check (added): EL13's options; whether the AI's go-back should be quicker.

## QUEUED rounds
Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 79
**Reviewed: Dan raced it 2026-10-28 17:26 ("Only tested the races"); his notes are Round 80.**
**Round 79, THE TRAIN IN REPLAYS, AIR RINGS INSIDE THE MAP, A STEADY AIR ARROW, the rail bridges' loose ends (Dan, 2026-10-28 11:36; report
BUG-001 of session `2026-10-28_11-32-04-219_308d8c`, read by Claude, after the Round 78 build).** Round 78 is reviewed: "I liked the Marietta Sq race", "The air
track was good". Item 0 and the end-of-round clean-up apply. Quick builds; ONE full Build Map only if the world changes. Short results.
A. THE TRAIN IN REPLAYS. Dan: "not sure I understand why the train wont be in the replays. It is a feature of that track." Record the train(s)
   (position along the line, speed, the cars) and the crossings' gates, lights and bells into every replay (as traffic is, Round 57), and
   play them back in step: a replay of the Marietta Square race shows the train arriving, the gates, and the racers beating it, waiting or
   jumping it, exactly as it happened. Check every other moving thing a race meets is recorded too (the airport's circuit plane, police,
   switch gates, plumes): list what is and is not, and add what is missing. Check: race the square, beat the train, watch the replay (TV and
   chase cameras) and compare.
B. AIR RINGS INSIDE THE MAP. BUG-001 (x 1069.84 z -8181.96 h 204, 172 m up over Polk Street NW, the Air Tour, ring 21 next after ring 20 missed):
   "this ring seems to be impossible to hit because it is out of the survey area" (the picture: LEAVING THE SURVEY AREA while heading for
   the ring). Round 76 bent the map's edge round the Marietta piece, so the flight boundary there is close. For EVERY air course (Quarry Air
   Race, Highway Air Race, Air Tour): every ring, and the turn from it to the next at race speed, must lie well inside the flight boundary
   (a margin of at least the plane's turning radius at top speed plus the warning distance); move rings or ease the boundary round the
   Marietta piece (e.g. let the flight boundary follow the outer rectangle of the scenery ring there), whichever keeps the course's intent;
   say which. The edge warning and headwind never fire on a course's line. Check: each air course flown by the human-like pilot, no warning.
C. A STEADY AIR ARROW. Dan: "the arrow is too jittery. One of the other air races also had this problem but I don't remember which". The air
   races' 3D arrow (Round 73 B) still jitters: smooth it properly (its own damped orientation, not tied to the plane's shake or the camera's
   bob; a steady position on screen; aimed at the ring through a filtered target), on all three air courses, in every camera. Measure its
   frame-to-frame wobble on each air course before / after (as Round 74 C did for the car arrow) and look at it in a recorded flight.
D. ROUND 78's FOUND-NOT-CHANGED ITEMS: the Canton Road Connector's bridge over the railroad ends 1 m under its road southbound (a wall): fix it
   (both ways smooth); the 27 % rail grade at Cobb Parkway's rail bridge (rails rarely exceed 2 to 3 %: ease it); the 9 road bridges near the
   rails with 0.3 to 1.2 m steps in their decks (build log): smooth each. Pictures before / after.
Check: item 0 for every item.
For Dan to check: a Marietta Square replay with the train; the Air Tour round Marietta (no edge warning); the arrow in each air race; the
connector's south end and the rail bridges.

**Status (Claude, 2026-10-28): built (full Build Map + exe 2026-10-28 15:57), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round79/`
(`BUG-001_before`, `79_BUG-001_after`; reports `round79_check_build3.txt` (dan, replay, air, arrow), `_rail_build4.txt`, `_arrow_before.txt`, `_final.txt`; air traces).
Five full builds: 2 found Build Map graded the ground to the decks before the railroad step changed them; 3 and 4 found Round 77's embankments carrying the road's end height flat under the deck.
A DONE. Replays keep the world's and the trains' clocks (`ReplayData.Kind.Clock`, every second and when a race moves the trains on); the viewer puts the trains, the crossings'
gates / lamps / bell, the traffic signals and the airport's circuit plane as they were (`WorldClock`, `RailTrains.ReplayAt`). Square race, beat the train, then its replay: 2380
train cars over 122 moments, worst 0.01 m from where they were; gates and lamps right at every moment; TV picture: the car on the crossing, gate down, the train beside it (`79_A_*`).
Recorded already: traffic, switch gates, plumes, breakables, gestures. Not in races: police, geysers / rigged hydrants. Not recorded: the flagger's step back, walkers and geese.
B DONE. The flight boundary (plane and helicopter) is the land's outer rectangle and 650 m (`MapEdge.Flight`): it fills the notch beside Marietta's piece; the ground there is the
edge scenery's (`EdgeScenery.RingGround`). No ring moved. Every course's line 808 m or more inside (Sky Hopper's turn at top speed 490 m + the 300 m warning). Flown with 3 AI
pilots: no plane warned (nearest 2759 / 807 / 985 m inside). The human-like check pilot finished all three but crash-looped (16 / 24 / 26 crashes); the AI's code took 4 / 7 / 8 rings.
At Dan's place, ring 21 next: no warning (`79_BUG-001_after`). Cars keep the old edge walls.
C DONE. The arrow is placed as the camera renders, along a filtered direction with the world's up, drawn over everything (`OverlayUnlit`: the plane's own body hid it in the
cockpit view, before too). Its place on screen frame to frame: 5.0 / 5.1 / 6.8 px before, 0.01 to 0.02 px now; its turn 0.009 to 0.021 deg a frame both ways (`79_arrow_*`).
D DONE. The listed decks smoothed (steps 0.29 to 1.18 m down to 0.11 m or less, ends kept) BEFORE their ends are graded (`RailDecks`). Round 77's embankments no longer carry the
road's end height under a deck (the connector's southbound wall, 1.07 m). The rail bed's banks tight under a road bridge (a 2 m hole before the connector's deck). Cobb Parkway's
27 % grade: its second carriageway had been a level crossing beside the bridge over its first; now one bridge over both. Steepest rail grade 9.4 % (the planes' underpass on South
Marietta Parkway), next 7 %. Driven: connector both ways through (stuck both ways before), an even 0.8 s hop off the deck's crest (jolt 0.5); Cobb Parkway 0.1 s; Cobb Parkway's other deck 0.
Found, not changed: Express Lanes 13 (a 780 m deck, not on Round 78's list) driven against its direction flies 2.06 s and lands hard (10.7 m/s); the replay's chase camera can sit
inside the ballast pile by Mill Street; the plane's cockpit view sits behind the pilot.
For Dan to check: a Marietta Square replay (beat the train); the Air Tour round Marietta; the arrow in each air race (chase and cockpit); the connector's south end both ways;
Cobb Parkway under the railroad.
Disk: 28.9 GB -> 28.5 GB (`cleanup_round.py`: local/round68 to 76 and Library/BuildHistory cleared; the player build kept), after deleting the 4.6 GB exe copy this round made for a parallel check.

## Previous delivery 2: Round 78
**Reviewed: Dan raced it 2026-10-28 11:32 ("I liked the Marietta Sq race"; "The air track was good"); his notes are Round 79.**
**Round 78, A RUNNING TRAIN; TWO NEW RACES (MARIETTA SQUARE, AND AN AIR TOUR OF THE MAP'S PLACES); the square's streets, the rail crossings,
a buried house (Dan, 2026-10-28 04:21; reports BUG-001 to BUG-005 of session `2026-10-28_04-03-25-890_8dd649` (BUG-004 empty), read by Claude, after the
Round 77 build).** Round 77 is reviewed. Item 0 (CHECK IT AS DAN PLAYS IT) and the end-of-round clean-up apply. Quick builds while working;
ONE full Build Map at the end. Do A (the train) before B (the square race uses it). Short results.
A. A RUNNING TRAIN. Dan: "would it be possible to build a running train?" A freight train (a locomotive or two and a string of cars:
   boxcars, tankers, hoppers; our own models, no railroad's name or logo) running the main line (Round 77 C's CSX line through Marietta and
   past Canton Road) edge to edge and back, every few minutes, at a believable speed (about 40 mph through town). Horn at crossings, bells and
   the crossings' gates and lights working with it (down before it arrives, up after), traffic and the AI stopping for them; solid: hitting it
   is a crash (reset), but a car can jump OVER the tracks where something gives a jump. In Free Roam and in races that cross the line. Show
   it on the minimap. Cheap when far: only simulated nearby, the rest by its timetable.
B. A MARIETTA SQUARE RACE (car). Dan: "A car race through Marietta Square taking advantage of the roads and parking decks (I assume there is one
   I can drive through) and also crossing the train tracks which can lead to having to beat the train, jump over or get stuck waiting for it".
   A circuit round the square and its streets (the square's four sides, Church / Cherokee / Roswell / Atlanta / Whitlock / Washington, Code
   designs it), THROUGH a parking deck (in at one level, along it, out another way or down its ramps; Round 77 made 19 decks drivable: pick
   one by the square, e.g. the Waddell Street deck), and ACROSS the railroad at a level crossing near the depot, where the train (A) comes
   through on a schedule that often meets the race: beat it across, wait at the gates, or take a JUMP over the tracks (something that belongs
   by a railroad: a loading dock / ramp, a flatbed, a ballast pile's slope) beside the crossing (real physics, landing clean, no free way
   round per the standing rules). Forward; reverse only if it plays well. No other shortcuts this round (Dan picks after driving it). Built
   like the other courses (closures off the line, AI that waits for or beats the train, records, ghosts, replays, map, loading inset).
C. AN AIR TOUR RACE (plane). Dan: "An air flight race visiting the POIs that we have. My neighborhood, the mall, the hospital, marietta square,
   kennesaw mnt and maybe the quarry again. I would like some underpass cps including under the glass bridge of the hospital." A ring course
   from the airport visiting, in a flowing order, Chastain Glen (low over Dan's neighborhood), Town Center at Cobb, Kennestone hospital, Marietta
   Square, Kennesaw Mountain (round / over the summit), Viking Quarry, and back. UNDERPASS RINGS: under Kennestone's glass skybridge over
   Church Street (check the clearance and the street's width against both planes' wingspans and heights as built: if a plane cannot fit,
   say so and make that ring fit, e.g. lift the skybridge's span or pick the widest gap, rather than drop it), under highway bridges, under a
   rail bridge. Built to Round 73 B's rules (rings that point you at the next, the 3D arrow, beacons, rings count either way, a missed ring can
   be gone back for), AI pilots and time trial, records, ghosts, replays. One lap (it is long); Code says the length and time.
D. THE RAIL CROSSINGS' ROADS. BUG-001 (x 918.88 z -6512.37 h 190, Tower Road NW where it dips under the railroad): "fix the underpass" (the
   picture: the road drops into a narrow trench beside the track with sheer dark walls, the bike stuck on a wall). BUG-002 (x 646.70 z -5541.05 h
   74, Old 41 Highway NW climbing over the railroad): "fix the overpass" (the road waves up to a boxy concrete hump). Make every road under a
   rail bridge a proper underpass (a real bridge span with abutments, the road graded down and back up smoothly, room for a truck) and every road
   over the railroad a proper overpass (an even deck and smooth approaches), all of Round 77's 13 rail bridges and any road-over-rail
   crossings; pictures of each.
E. MARIETTA SQUARE'S STREETS. BUG-003 (x 1720.39 z -8428.27 h 71, West Park Square NW): "can we make the stoplights and cars not so blocky. Also
   the parked cars are blocking the roads. Can we push the parking lots out of the streets so cars can pass by?" (the picture: parked cars as
   gray blocks standing in the travel lanes, plain box traffic lights). The angled parking spaces along the curbs only (the travel lanes
   clear for traffic and racers, two-way / one-way as real); parked cars as the game's real car models (the traffic cars' kinds), not
   blocks; traffic lights with proper housings, backplates and mast arms (as the photo 06 shows), lit by phase. Check the rest of the map's
   generated parked cars and lights for the same blocks.
F. BUG-005 (x 1850.19 z -8843.93 h 146, a house south-east of the square): "unbury this house" (the picture: a two-story house sunk to its
   eaves). Settle it, and every house in stage 3's land, as Round 52 did (floor at the drive's grade, the lot graded, steps at the door).
G. ROUND 77's OPEN ITEMS: BUG-003's stray white block at the connector (find it in the scene by its position and remove it); the Green deck's far
   end running into the physicians' building; the deck roof jump (the Waddell Street deck's roof: a jump off it, if the race / Free Roam
   wants one); and stage 3's listed bumps (`stage3_bumps.txt`: 23 steps of 10 cm or more, 53 humps of 30 cm or more) smoothed.
Check: item 0 for every item; the train seen from a crossing (gates, lights, horn, traffic waiting); the square race raced by the human-like
driver three times (beating the train once, waiting once, jumping once); the air tour flown by the human-like pilot, a cockpit picture under
the skybridge; D, E, F before / after at Dan's spots.
For Dan to check: the train; the Marietta Square race; the air tour (under the skybridge); the underpass and overpass; the square's parking
and lights; the house.

**Status (Claude, 2026-10-28): built (full Build Map + exe 2026-10-28 09:24), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round78/`
(Dan's pictures `BUG-00x_before`, `78_BUG-00x_after`; reports `round78_check_build3.txt` / `_build4.txt`, `air_tour_ai_pilots.txt`). Four full builds: build 1 stopped
(the rail pulled 22 m under the Express Lanes deck), 3 found Old 41's deck rule had evened ten decks by name (the connector's ended 1 m under its road).
A DONE. Freight trains (World/Rail) on two routes through Marietta's double track (Kennesaw 17.4 km, Canton 8.2 km), a train past the square about every 5 min;
seen from Mill Street: lamps 8 s, gates down 19 s, the train 33 s, gates up 62 s; horn blown; 5 traffic cars held; a car on the tracks hit and put back (`78_A_*`).
Round 77 had dropped the second Marietta track's crossings (its rails stood proud of the roads): kept now.
B DONE. Marietta Square race, 1292 m: the square, Waddell, through the Lawrence Street deck's ground floor, Haynes, Lawrence, Mill Street over the tracks,
North Marietta Parkway, Whitlock back over them. Jump: a ballast pile by Mill Street's curb, clears a train from 19 m/s. Raced 3 times (1 lap, 3 AI): beat it
(crossed closed, no reset), waited at the gates, jumped (2.44 s in the air) but one reset after landing beside building 34315; every racer finished.
C BUILT; the AI pilots fly it, my check pilot does not. Air Tour 30.7 km, 23 rings (quarry, Town Center, under Chastain Road NW over I-575, Chastain Glen,
under Kennestone's skybridge, Marietta Square, under the parkway rail bridge, Kennesaw Mountain). Neither plane fitted under the skybridge (5.6 m): lifted to
10.5 m, span 8 m wider, flatter arch; the parkway rail bridge 11 m clear. 2 AI pilots on Hard: 8:20 and 8:35 for 2 laps, no crashes. Laps 4:07 to 4:47.
The human-like check pilot crash-looped at the quarry's first ring (two tries: tuning limit); no cockpit picture from it (a cockpit-height still instead).
D DONE at Dan's spots. BUG-001: a wide span, sloped banks (`78_BUG-001_after`); Tower Road still lifts 0.6 s at 25 m/s on its dip (an even crest).
BUG-002: Old 41's deck even, the rail 5.5 m lower under it: 0.0 / 0.1 s air both ways. 14 rail crossings driven: 11 clean.
E DONE. 61 parked cars round the square, all off the lanes, real models (1342 pieces); decks too. 32 signal heads, backplates, phased (`78_E_*`). No other blocks.
F DONE. Every stage 3 house settled as Round 52's; BUG-005's house on its ground (`78_BUG-005_after`).
G. The stray block: hut 6854 (1.9 x 3.9 m, built 11 m tall) left out. Green deck 20 m shorter, clear of the physicians' building (5 other overlaps listed in the
build log). 48 of stage 3's serious bumps blurred. Not done: the Waddell deck's roof jump.
Found, not changed: the connector's bridge over the railroad ends 1 m under its road southbound (a wall; northbound Round 77's hop); a 27 % rail grade at Cobb
Parkway's rail bridge; 9 other road bridges near the rails with 0.3 to 1.2 m steps in their decks (build log). Trains are not in replays.
For Dan to check: the train at a crossing; the square race (beat / wait / jump); the air tour; Tower Road and Old 41; the square's cars and lights; the house; the
connector's south end (should its deck end be raised?).
Disk: 28.5 GB -> 28.4 GB (`cleanup_round.py`: local/round68, 73, 74, 75 and Library/BuildHistory cleared; the player build kept).

## Previous delivery 3: Round 77
**Reviewed: Dan drove round 2026-10-28 04:03 ("did a quick drive around"); his notes are Round 78.**
**Round 77, THE RAILROAD, KENNESTONE HOSPITAL AND ITS PARKING DECK, THE CANTON ROAD CONNECTOR (Dan, 2026-10-27 21:19; reports BUG-001 to
BUG-007 of session `2026-10-27_21-09-31-163_2b90af`, read by Claude, after the Round 76 build). Dan may add photos of the hospital and Marietta Square to
`local/reference_photos/kennestone/` and `local/reference_photos/marietta_square/`: use them if they are there when you reach B (reference
only, never textures, never traced); if not, work from OSM's footprints and heights and what is known, and say so.** Round 76 is reviewed.
Item 0 (CHECK IT AS DAN PLAYS IT) and the end-of-round clean-up apply. Quick builds; ONE full Build Map at the end. Short results.
A. THE CANTON ROAD CONNECTOR. BUG-001 (x 2292.44 z -5006.93 h 66, beside the connector where it meets the highway): "fix" (the picture:
   torn, jagged spikes of ground and rock standing up both sides of the roads, black faces). BUG-002 (x 1694.78 z -5783.19 h 352, on the
   connector): "fix this too" (steep green banks and odd mounds crowding the ramps). BUG-003 (x 1618.42 z -6110.74 h 29, on the connector):
   "need to fix here as well" (a split with a mound between, the road dipping, a stray white block on the left). Smooth the ground round
   the connector and its ramps into proper graded embankments (no spikes, no black faces, no stray objects), the roads even. Then scan the
   whole of stage 3's new roads and interchanges for the same (jolt and face scans as Rounds 61 / 75) and fix what it finds; list it.
B. KENNESTONE HOSPITAL. BUG-004 (x 1608.76 z -6601.44 h 279, by the hospital): "lets make the hospital resemble the hospital. Do I need to
   provide pics?" (the picture: plain tan boxes). Wellstar Kennestone Regional Medical Center as itself (as Town Center, Round 60): its
   towers at their real heights from OSM, the main entrance with its canopy and drop-off, the emergency department's entrance and ambulance
   bay, glass and brick as the photos show, the rooftop helipad kept (Round 63), signs (generic: HOSPITAL, EMERGENCY; no brand logo),
   ambulances parked, people. BUG-005 (x 1665.13 z -6716.61 h 151, Church Street NW by the hospital): "pretty sure that maybe this is the
   parking deck. So not businesses" (the picture: a building dressed as stores with business signs, three dark openings at street level).
   Make it a parking deck: open levels with low walls and columns, ramps between levels, cars parked, entrances / exits at the street.
   Drivable inside to the roof (fun: a roof to jump off, the game's rules); check OSM for other parking garages (parking=multi-storey)
   dressed as businesses anywhere on the map and make them decks too.
C. THE RAILROAD. BUG-006 (x 1853.58 z -9077.33 h 347, Atlanta Street SE): "this should be a rail bridge" (the picture: the tracks laid flat
   across the road). BUG-007 (x 1620.14 z -8259.97 h 357, downtown Marietta): "rail line shouldn't run into the building. The rail should be
   quite significant I know it runs by canton road (but maybe off this may not sure) still shouldn't stop right here" (the picture: two
   tracks ending in a building's wall). The CSX main line through Marietta (OSM railway=rail): one continuous line (or the real double
   track) through the whole map from edge to edge as OSM has it, on its own graded bed (ballast, ties, rails), never through a building
   (if OSM's building footprint sits on the line, the building gives way), with bridges where it crosses over roads (OSM's bridge=yes /
   layer) and level crossings with gates and lights where it really crosses at grade; the old depot by the square (Round 76) beside it.
   The line ends only at the map's edge (Round 66's scenery carries it on). Optional if cheap: a freight train now and then.
B+. DAN'S HOSPITAL PHOTOS (2026-10-27 21:55, `local/reference_photos/kennestone/`, reference only): 01 an aerial of the campus with the glass
   SKYBRIDGE arching over the road (white steel X-truss on an arch, a round emblem in the middle: use a plain round emblem) between
   tan-brick buildings; 02 the skybridge from the road; 03 the main tower's entrance at dusk: a tall white portico with columns and a glass
   canopy over a curved brick-paved drop-off, red-brick and white towers of 6 to 8 stories behind with bands of windows, an older tall
   brown-brick tower beside; 04 and 05 the EMERGENCY building: tan / beige with darker brick piers, a white canopy with angled struts over the
   ambulance drive, the word EMERGENCY over it, a red-and-white HELIPAD on its roof (the helicopter pad of Round 63 goes there if it is
   not already), a curving drive on a terraced retaining wall with shrubs, the older white tower behind. Build these: the skybridge over
   the street (drivable under it, solid), the portico entrance, the ER with its canopy and roof pad, the terraced drive. The sign on the
   main entrance may read KENNESTONE HOSPITAL (the place's own name, as the game names streets); no company logo.
D. MARIETTA SQUARE TO DAN'S PHOTOS (Round 76 B was built without them; 2026-10-27 21:55, `local/reference_photos/marietta_square/`, reference
   only): 02 the top-down shows the layout: Glover Park a square block with brick paths in a cross and diagonals meeting at a round FOUNTAIN
   (09: a tall tiered cast-iron fountain in a low round brick basin), big shade trees, grass panels, a small green-roofed GAZEBO, and the
   streets round it with ANGLED PARKING on both sides and one-way arrows; 01 an aerial of the park full of people on the lawns; 04 the
   park's PAVILION / bandstand (a cream two-story pavilion with columns on a brick base, steps); 05 a LIBERTY BELL replica on a granite
   plinth with a bronze plaque, black benches round it, brick paving; 07 the children's play area with a W&A RR toy steam locomotive and
   tender kids climb on, behind a low brick wall; 06 the square's storefronts: two- and three-story fronts, an art-deco white THEATER with a
   red / green marquee (fake name, e.g. "THE GRAND"), colored shopfronts with awnings, traffic lights on mast arms; 03 a red-brick office
   building with a tall stone CLOCK TOWER (big round clock) and, behind, the brick courthouse tower with a clock and a pointed spire;
   08 the DEPOT (a one-story brick station with stepped Dutch gables and a slate roof, "WELCOME CENTER" on a green sign) beside the double
   track behind a black iron fence, and the four-story brick hotel next to it ("MUSEUM OF HISTORY" lettering: keep generic). Rebuild the
   square and its edges to these; walkable lawns and paths drivable (breakables: benches, lamp posts, café tables; the fountain, bell,
   locomotive and pavilion solid). Pictures next to each photo from the same view.
Check: item 0; A before / after at Dan's three spots and the scan's list; B pictures of the hospital's sides, the entrance, the ER, the
deck driven to the roof; C the line followed end to end on the map and in pictures (each bridge, each crossing, the square).
For Dan to check: the connector; Kennestone (skybridge, entrance, ER, helipad) and its deck; Marietta Square to the photos; the railroad.

**Status (Claude, 2026-10-28): built (full Build Map + exe 2026-10-28 01:26), checked in the exe from the player's camera, not yet driven by Dan.** Evidence `Data/screenshots/round77/`.
Photos used for B and D (reference only). Two full builds: build 1's checks found the faults fixed in build 2.
A DONE (one item open). Cause: lidar spikes and a jagged trench where the Express Lanes deck runs through the connector's embankment (BUG-001), a lidar mound between the
carriageways with steep raw banks (BUG-002 / 003). `R77Embankments`: each carriageway's profile smoothed, the ground within 45 m relaxed as a membrane between the roads and the
natural ground. Cells over 45 degrees within reach: 15,998 -> 2,217. Pictures `77_BUG-00[123]_after`: no spikes, the median and banks smooth, the road even. The connector driven
flat out by the human-like driver: 1,314 m at 30 m/s, no reset, one 0.56 s hop near BUG-003's deck end. Stage 3 scan (`road_faces.py`): 114 face spots -> 45 after adding its
roads to the pass (`stage3_faces_before/after.txt`); left: unnamed service drives and the new rail bridge's abutments. Bumps listed, not fixed (`stage3_bumps.txt`: 23 steps
>= 10 cm, 53 humps >= 30 cm outside the highways). OPEN: BUG-003's "stray white block" on the left is still there; I could not identify the object.
B DONE except the deck jump. BUG-005's building is OSM's skybridge over Church Street (three floors starting one up), not a parking deck: built as the skybridge of photo 02
(`77_BUG-005_after`). Kennestone from OSM's building parts (47 tower blocks up to 38 m), the portico at the drop-off loop, the ER with its canopy and EMERGENCY, ambulances,
the terraced wall, the red roof helipad. Every OSM parking deck (19, all were dressed as stores) is now a deck with ramps. Driven: the Waddell Street deck to its roof (27 m up,
no reset). Not shown: the roof jump (the scripted driver ended against the roof wall; two attempts, tuning limit). The Green deck's far end runs into the physicians' building.
C DONE. `make_railroad.py`: 10 running lines, 36.8 km edge to edge, 23 level crossings with gates and lights, 13 rail bridges over roads; BUG-006's South Marietta Parkway
crossing is a rail bridge (`77_C_underpass_South_Marietta_Parkway_*`). BUG-007's building gives way (`77_BUG-007_after`). Steepest rail grade 11.7 %, a short rise at the I-75 bridge.
No freight train.
D DONE. Square rebuilt to photos 02 to 09: pictures `77_D_photo0*` from each photo's place. Fountain, bell, train and pavilion solid; signals, fence and play wall break.
Disk: 28.3 GB -> 28.0 GB (`cleanup_round.py`: local/round68, 73, 74 and Library/BuildHistory cleared; the player build kept).
For Dan to check: the connector at his three spots; BUG-003's white block; Kennestone (entrance, ER, skybridge, helipad); a deck's roof gap; the line end to end; the square.

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
