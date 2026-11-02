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

## QUEUED rounds
**NEXT ROUND (Round 91), after Round 90: SPLIT-SCREEN NOTES AT THE TOP, PLANE ROLLS AND LOOPS, THE UNDERGROUND ACROSS THE WHOLE MAP (Dan, 2026-11-02 01:40).**
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

**THEN (Round 92): A RACE FOR A PLANE OR A CAR (Dan, 2026-11-02 01:40).** Dan: "I would like to add a race that you could race as a plane or a car. The estimated time for
both should be about the same and should use much of the same road. But we can do things to give the cars an advantage and add disadvantage to the planes so that one
whole race should take the car and the plane about the same amount of time. For example planes could have things like forcing them to go a longer path and have cps under
bridges to force them to fly more precisely and having to lower speed whereas cars could have underground shortcuts using the underground tube system". One course, raced
in race setup as a car or a plane (and mixed fields: cars and planes together, AI of both). Cars: the roads, with underground shortcuts through the expanded network
(Round 91 C) and its express runs. Planes: rings that follow much of the same road but take longer ways round, low rings under bridges and overpasses (precise and slow:
rings small enough that full speed misses them), slow gates (a ring you must take under a speed). Tune so a good car lap and a good plane lap (the human-like driver and
pilot, and the AI on each level) come within about 5 % of each other; report both times per difficulty (two tuning attempts, then report). Records separate for cars and
planes, and a combined board; ghosts and replays of both; HIGH MODE for both. Check: 3 races each way (all cars, all planes, mixed) with times.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 89
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

## Previous delivery 2: Round 88
**Not yet driven by Dan (2026-11-01: no chance to test yet); he reviews it with Round 89.**
**Round 88, THE KENNESAW CLIMB'S RUN-OFF CARRIED BACK, THE BUMP AT THE KENNESAW RACE'S START, AND THE LEFTOVER ROAD WORK (Dan, 2026-10-31 13:28: "there is also a smaller
bump at the beginning of the race. Lets wrap this into whatever code said it wants to do and then any remaining road work we had leftover").** Round 87 is reviewed.
Item 0, the one-heavy-job rule (one job at a time, Below Normal, at most 3 full Build Maps) and the end-of-round clean-up apply. All world changes of A, B and C batched
into ONE full Build Map where possible. Round 82's note stands: stuck, walls, steps and launches are bugs, not numbers to tune. Short results.
A. THE KENNESAW CLIMB'S RUN-OFF, CARRIED BACK (queued by Code after Round 87 E; Dan: "Yes, next round"). Round 87 E's 18 m level run-off on the bend's outside (left) at
   x -398 z -5520 runs from 20 m before the crest to 60 m past it (`make_mountain_route.py` KC_FROM / KC_TO, its clear line). Normal-mode cars still run 10 to 24 m wide
   on the left 38 to 55 m BEFORE the crest (x -360 to -375, z -5471 to -5491; the Street Classic 7.8 m below the road). Start it about 80 m before the crest (KC_FROM about
   -80), trees cleared with it. Check: `-exeRound87 -r87Only p87climb` (widest point and air, normal and HIGH): never off the road's level ground.
B. THE BUMP AT THE KENNESAW RACE'S START. Dan: "there is also a smaller bump at the beginning of the race". Claude found in Dan's bump log (`%USERPROFILE%\AppData\LocalLow\
   Chastain Rush\Chastain Rush\bump_log.txt`, his Kennesaw race 2026-10-31 00:19:46, the first event of the race): Kennesaw Mountain Drive at x -831.9 z -5187.9 (y 395):
   JOLT +6.2 m/s in 0.1 s at 31 m/s, AIRBORNE 0.90 s, 2.47 m up, 28 m, landing x -806.2 z -5173.7. Confirm it is the one near the start (check the start grid), and smooth
   it into the road (one vertical curve the course's speed stays down on). Leave the trail's designed big jumps alone (the repeated ~1 s flights at x -500 z -5320 and x
   -1188 z -5940 each lap are the course's jumps unless they throw cars off the road). Check: the start to the first bend at full throttle in the bike, a car and the
   heaviest vehicle, normal and HIGH: no air over 0.3 s there.
C. THE LEFTOVER ROAD WORK (queued since Round 83). 64 of 416 crossing runs were not clean: Frey Road's Express deck start and I-75's deck, I-75's decks at Big Shanty Road
   (-584, -410) and Bells Ferry Road (1520, -4228), Phillip Landrum (770, -1680), the Canton Road Connector's interchange (2306..2401, -5011..-4902) and ramp 437897716, Allgood
   Road (3458, -6205), Greers Chapel (and the Express Lanes under it, 1.4 s), Chastain Road east of I-75, the I-575 flyover driven backwards; the I-75 Express Lanes
   southbound 2.5 s in the air at the I-575 merge (508, -3365) and a stop at the Frey Road deck; Skip Spann's west deck end (0.38 s). Fix each. Check: all 416 crossing runs
   on the final build (one at a time, Below Normal): list clean / not; the Express Lanes end to end both ways.
Check: item 0 for every item. PUBLISH through the launcher.
For Dan to check: the Kennesaw race (the start and the climb, normal mode); the Express Lanes through the I-575 merge; any crossing from C's list.

**Status (Claude, 2026-10-31): built (2 full Build Maps, exe 18:47; a 3rd started was stopped in its planning when its deck-end pass was found wrong, and one was cut off by the session ending and re-run), checked in the exe, published (0.90.0, `game-9000`), not yet driven by Dan.** Evidence `Data/screenshots/round88/`. No bug report newer than Round 87's.
A DONE. The run-off from 80 m before the crest (`KC_FROM` -80). Build 1's check: still 0.5 to 1.5 s in the air and the Drifter Twin 52 m wide: the bump log showed the drive's terraces below the bend (16 % then 6 % every 35 m, 57 to 165 m before the crest) kicking cars 0.3 to 0.7 s into it. Attempt 2: one vertical curve over them (`make_mountain_route.py`, laid before the crest's). Check (`check_ab_after.txt`): 3 vehicles, normal and HIGH, 0.00 s in the air, at most 4.1 m off the line, never below its ground. Pictures `88_A_climb_before/after.png` (level grass run-off left of the bend, no trees).
B DONE at Dan's spot. The start line (lap 0, where the descent's grade meets the drive) rose 0.26 m in 2 m (3 % into 13 %): one 80 m vertical curve through it. Check: from the grid and flying through the line, 3 vehicles, normal and HIGH: 0.00 s in the air at the line. One flag: the Drifter Twin at 45 m/s flies 0.5 s and lands 43 m BEFORE the line (the descent's run-out, not changed). Pictures `88_B_start_before.png` (Dan's spot), `88_B_start_after.png`.
C PARTLY (tuning limit, 2 attempts). Probed on the built colliders (`R88Probe`, `Tools/race/probe_ride.py`): nearly every failure was a deck's end not meeting its road (Round 27's end pass follows only one short OSM way; later passes move decks; several decks turn up 0.5 to 3 m over their last 3 m). `MapBuilder.Round88.cs` R88DeckEnds (after every deck pass, on C's 27 decks only): the deck's last 6 m put back on its own grade, the road on (OSM's ways chained) laid as one curve held at 40 m/s (`round88_deck_ends.txt`); Chastain Road's kink east of I-75 one vertical curve (`Data/tracks/round88_roads.json`). All 416 crossing runs (`final_c_crossings.txt`): 64 -> 45 not clean; fixed 28 (Allgood Road, Phillip Landrum at (770, -1680), the I-575 merge ramp and flyover, Skip Spann's west end, Chastain Road east of I-75, part of Bells Ferry); still: Frey Road / I-75, I-75 at Big Shanty, I-75 by Greers Chapel and the Express under it, Bells Ferry, the Canton Road Connector interchange (its ramp's decks overlap: left as they were). NEW faults: Canton Road at (1913, -5855) 2.1 s (beside deck 51, whose end the pass changed), I-75 at (1880, -4459) 2.5 s, Greers Chapel's 0.2 s flights now 0.4 s. Dan decides: revert deck 51 / keep going on the rest next round.
  The Express Lanes end to end (`final_c_express.txt`): I-575's both ways and the Big Shanty ramp clean; I-75 southbound 0.94 s at the I-575 merge (was 2.5 s), no stop; I-75 northbound still stuck at the Frey Road deck, now with 3.3 s in the air there (Round 83: stuck there too); the I-575 flyover into I-75 2.2 s forward (was 0.76 s), clean the other way (was 0.48 s).
Published: `game-8800` (0.88.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-10-31 18:47.
C continued (Dan: "Keep fixing C"; 1 more full Build Map, exe 2026-11-01 01:17; published `game-8801`, 0.88.1). The ground kept under a deck's last 30 m (decks put back on their grade had sunk into it), a deck overhead with 5 m+ clear no longer stops the laying (the Connector interchange), ramp 437897716 merged level with deck 54 (+3.9 m, eased over 60 m), deck ends fitted on 10 to 30 m, the whole easing judged, short roads joined across junctions, I-75 over Dickson Road (bridge 59) added; the crossing check starts each line on its own road's surface. All 416 crossing runs (`c2_crossings.txt`): 45 -> 3 not clean (Express Lanes 623138519 one wall touch the other way, as before; motorway_link 879570834 at (140, -2633) 1.1 s, was 2.2; Canton Road at (1919, -5843) a 8.9 m/s landing, was 2.1 s in the air). Express Lanes end to end (`c2_express.txt`, rerun `c2_express_rerun.txt`): I-75 through both ways, no stop at Frey Road (one wall touch); I-575 clean; the flyover into I-75 still 2.2 s on deck 80 at (656, -3674) (as Round 88; tuning limit reached). The first run had two falls off the line where the probed surfaces match Round 88's exactly; the rerun drove both through.
For Dan to check: the Kennesaw climb and the start line (normal mode, the Drifter Twin flat out); the crossings (Canton Road Connector, Bells Ferry, Big Shanty, Greers Chapel); the Express Lanes at Frey Road and the I-575 flyover's deck 80.

## Previous delivery 3: Round 87
**Reviewed: Dan drove it 2026-10-31 (the Kennesaw race); his notes are Round 88.**
**Round 87, BUMPERS AND BARRIERS THAT NEVER FACE YOU, A HIGH RESET WHEN YOU JUMP OUT, the Addison Heights and Noonday Creek Trail reports (Dan, 2026-10-31 00:38; reports
BUG-001 to BUG-011 of session `2026-10-31_00-06-51-254_769f69` (BUG-009 / 010 / 011 one place), read by Claude, after the Round 86 build; HIGH MODE races of Addison
Heights (BUG-001 to 006) and the Noonday Creek Trail (BUG-007 to 011)).** Round 86 is reviewed. Item 0, the one-heavy-job rule and the end-of-round clean-up apply.
Short results. Round 83's road failures still wait (QUEUED).
Dan: "Some of these are general issues. Mainly that barriers and bumpers shouldn't be at an angle that would cause you to run into them head on when following a course.
Also in high mode if someone jumps outside of the bumpers, they should be reset onto the track". So A and B are rules for EVERY course (forward, reverse, shortcuts),
HIGH bumpers and normal-mode barriers alike; Dan's places are examples, each checked by name.
A. NEVER HEAD ON. Following a course's line, no bumper or barrier end, corner or run ever faces you: every run lies along the course (parallel to its edge, never
   angled across a lane), every end tapers or curves away from the line behind the edge, and where two runs meet they join in one rounded curve (BUG-002 x 3292.64 z
   307.55, Embry Lane: "bumpers shouldn't cross each other like this. when they join they should be roounded", the picture: two runs crossing, an end sticking out into
   the road). Closure barriers stand across the closed road's mouth, set back off the course's edge, in a line that follows the course, never with a corner in the road
   (BUG-008 x 1300.15 z -1671.25, Barrett Parkway: "Why is this barrier like this? It should be out of the way not have an edge sticking int he middle of the road", the
   picture: a zigzag of barrier blocks and a bumper stub in the lanes). Bumpers on the OUTSIDE of every corner, never an opening there (BUG-001 x 3216.77 z 535.67,
   Embry Lane: "why is there no bumper on this corner? It is one of the easiest places to flying off the track"); openings only where a shortcut or the course itself
   leaves or rejoins. Fewer, simpler: no bumpers that do nothing (on medians between the course's own lanes, round mouths of roads that are closed anyway, doubled
   beside a closure barrier) (BUG-009 to 011 x 423.67 z -1494.34, Mall Boulevard / Ring Road: "this is a little confusing all these barriers\bumpers but most are not
   needed", the pictures: bumper and barrier runs on both sides of each carriageway and the median, ends pointing into the lane). Check, by script on every course and
   direction: no bumper or barrier piece within the course's driven width whose face is more than 25 degrees off the line ahead; the human-like driver on the line
   and 3 m either side touches no end; pictures at each of Dan's places before / after.
B. HIGH MODE: OUT OF THE BUMPERS, BACK ON THE TRACK. A HIGH player who lands, or ends up, outside the bumpers (or off the course past an opening that is not a shortcut)
   is reset onto the course at once (after about a second down), placed as Round 86 B places resets (before the next checkpoint not yet passed, facing the course's
   way, no penalty). Check: jumped out over a bumper on three courses: back on the line, next checkpoint ahead.
C. RESETS FROM INSIDE THE MALL. BUG-007 (x 789.60 z -1683.53, Barrett Parkway, the Noonday Creek Trail race, checkpoint 12 of 13 next): "I reset in the mall and ended up
   way over here". Round 86 B's rule failed there (the line through Town Center's two levels, or a checkpoint inside it, missed): a reset in the mall goes back on the
   course's line inside the mall before the next checkpoint. Check: resets at five places through the mall (both levels, both directions of the race): each lands
   inside the mall, next checkpoint ahead.
D. ADDISON HEIGHTS: SLOWED IN THE ROAD, THROWN INTO THE BUMPERS, THE JUMP'S LANDING. BUG-003 (x 2967.67 z 417.02, Joel Drive: "there is something here that slows the
   car down") and BUG-005 (x 2962.48 z 408.83: "the bushes seem to cause slow down in the middle of the road", the picture: the shortcut's slowing bushes beside the
   lane): the slowing bushes and their slow-down never reach the course's road (they block only the free way round a shortcut, as the rule says). BUG-006 (x 2874.80
   z 417.87, Joel Drive: "somthing here caused me to run into the bumers"; the bump log has a JOLT at x 2875 z 411): find the jolt and smooth it. BUG-004 (x 2811.13 z
   498.87, Lyndon Drive, heading 36: "this area should be open so that you don't run into the bumper coming off the jump", the picture: a bumper's end in the landing
   ahead): the jump's landing and run-out open (Round 86 A missed it: say why). Check: Joel Drive and Lyndon Drive driven through Dan's places on the line: no slowing,
   no jolt, no bumper touched.
E. THE KENNESAW CLIMB'S KICKER (Dan, 2026-10-31 00:44, not captured as a report: "On the way up the hill there is a significant bump that generally causes all
   vehicles to jump off the road over the barriers"; found by Claude in Dan's bump log `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\bump_log.txt`, his
   Kennesaw race 2026-10-31 00:20 and 00:21, both laps the same): Kennesaw Mountain Drive at x -394 to -402 z -5516 to -5523 (y 469 to 471): JOLT +2.8 m/s in 0.1 s, then
   AIRBORNE at 28 m/s with vy +13.2 / +14.2 m/s (speed falling from 33 to 28: a steep face, not a crest); lap 1 flew 2.16 s, 11.7 m up, 59 m, landing at x -444.5 z -5564.6
   4.1 m off the line; lap 2 never logged a landing (off the road). Smooth that face into the climb (one vertical curve the course's speed stays down on, or, if a jump
   belongs there, one that throws you straight along the road and lands on it), the barriers and bumpers beside it per A. Check: the climb driven at full throttle by
   the human-like driver in several vehicles (the bike, a car, the heaviest), normal and HIGH: no air over 0.5 s there, never off the road; picture of the spot.
Check: item 0 for every item. Quick builds unless a world rebuild is needed. PUBLISH through the launcher.
For Dan to check: HIGH MODE races of Addison Heights and the Noonday Creek Trail (bumpers and barriers, a reset in the mall, jumping out over a bumper); any other
course's barriers; the Kennesaw climb at x -398 z -5520.

**Status (Claude, 2026-10-31): built (one full Build Map for E, exe 05:31), checked in the exe, published 0.87.0 (`game-8700`), not yet driven by Dan.** Evidence `Data/screenshots/round87/` (`87_BUG-0xx_before/after.png` from Dan's spot and heading; `check_before.txt` = Round 86's layout via `-r87Old`, `check_after.txt`). Reports read: session `2026-10-31_00-06-51-254_769f69` BUG-001 to 011.
A MOSTLY DONE. `HighBumpers`: a bumper keeps 3.5 m past any other line's paved edge (no crossing runs, none on a median between the course's own lanes), opens only for ANOTHER road (the outside of a corner on the course's own road keeps its bumper: BUG-001), no fold on a tight bend's inside, its distance out changes at most 10 degrees, ends curve 2.5 m out over 12 m, runs under 10 m left out, a loop's seam joined. Closures (`RaceBarriers.AlongCourse`): the row stands ALONG the course across the closed road's mouth, past all the pavement there plus 2.2 m, one set-back per row (BUG-008: out of the lanes). Check, every car course both ways: pieces inside a course's driven width more than 25 degrees off the line 109 bumper / 14 barrier -> 37 / 2; bumper ends inside it 182 -> 46 (left: the trail race under I-575 and on Barrett west of I-575, Chastain Ridge at x 3570 z 580 and 3510 z 1080, Marietta Square, a few corner ends). Human-like driver on the line and 3 m either side: Addison lap and the trail race's Ring Road to Barrett stretch, 0 end touches in four of five full runs (one run: the inside end at BUG-001's corner, 3 m right).
B DONE. `HighAssist`: in a HIGH race, down (2 wheels) outside the bumpers' reach (`HighBumpers.Inside`, the lead-in counts) for 1 s: put back as a reset (before the next checkpoint, no penalty). Check: thrown over a bumper 9 times on Addison, the Glen loop and Kennesaw: all put back in 2.2 to 4.4 s, next checkpoint ahead, no penalty.
C DONE. BUG-007: Dan was on the Carroway renovation shortcut (it forgives CP8 to 10), so the reset measured him against the lap and put him 12 m short of CP11 on Barrett (reproduced: (788, -1688)). Now a reset on an active shortcut stays on its line (`CarRecovery.ShortcutSpot`), the lap station counts the mall's floors (`RaceRoute.LapStationNear`). Check: 10 resets in Town Center (both floors, both directions, the shortcut): all within 4 m, same floor, next checkpoint ahead.
D DONE. BUG-003 / 005: the Joel terrace's bed reached 3.5 m over Joel Drive; no shrub drag on any road's pavement now (`ShrubBeds`; the bushes were never drawn there). BUG-006: the ground along and across Joel Drive is smooth; the "jolt" was the bike hitting the bumper where it stepped in at the road's edge (now smoothed). BUG-004: the Lyndon trailer jump was missed by Round 86 because its shortcut line carries the flight in its own heights (no "jump" found): a line in the air now counts as one, and a landing near a shortcut's end leaves it open to the lap; no bumper beside a raised ramp. Check: Joel Drive and the Lyndon jump driven on the line and 2 m either side: 0 bumper touches, no drag at Dan's spots.
E NOT MET (tuning limit, 2 attempts). The climb's crest (15 -> 25 -> 14 % in 30 m) is one 90 m vertical curve now (`R55VerticalCurves` reads the mountain track; ground moved up to 1.4 m), flat 7.5 m either side to take out a 0.5 to 0.75 m berm at the edge of a 3.8 m drop on the bend's outside. HIGH: the Drifter Twin, Street Classic, Skyfin 0.10 / 0.36 / 0.18 s in the air, on the road. Normal mode at full throttle (39 / 36 / 36 m/s): still 1.8 / 1.2 / 0.9 s in the air and 13 to 20 m off the line (was 2.1 s and 90 m): they run wide at the bend and drop off the edge. Dan decides: a barrier along that edge in normal mode, or a wider bench.
E follow-up (Dan 2026-10-31: "Wider bench"; the round's 3rd and last full Build Map, exe 11:30): the climb's level band runs 18 m out on the bend's outside (left) from 20 m before the crest to 60 m past it, tapered over 20 m, banked down over 18 m, trees cleared (vertical curve `half_left` / `blend_left` / `wide_from` / `wide_to` / `taper`, `make_mountain_route.py` KC_*). Normal mode: air 0.60 / 0.82 / 0.72 s (was 1.8 / 1.2 / 0.9) but they still run 10 / 24 / 19 m wide, 38 to 55 m BEFORE the crest (the bend's first part, x -360 to -375 z -5471 to -5491), ahead of the run-off (Street Classic 7.8 m below the road there). HIGH unchanged (on the road). [check_climb_bench.txt](Data/screenshots/round87/check_climb_bench.txt), picture at the crest [87_climb_bench.png](Data/screenshots/round87/87_climb_bench.png) (level grass run-off on the left, no trees, nothing floating). NOT MET: carrying the run-off back to about 80 m before the crest needs a 4th full Build Map (limit 3): next round if Dan wants it.
Published: `game-8700` (0.87.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-10-31 05:31. Disk (project folders) 34.2 -> 33.7 GB; Builds keeps 0.83.0 to 0.87.0's release folders.
Published (E follow-up): `game-8701` (0.87.1, Latest), the exe of 2026-10-31 11:30. Disk (project folders) 35.3 -> 35.3 GB (Builds now also keeps 0.87.1's release folder).
For Dan to check: Addison Heights and the Noonday Creek Trail in HIGH MODE (Embry's corners, the Lyndon landing, Barrett's closures, Ring Road / Mall Boulevard); jumping out over a bumper; a reset in the mall and on the Carroway shortcut; the Kennesaw climb in normal mode (the run-off on the bend's outside; wide runs still leave the road just before it).

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
