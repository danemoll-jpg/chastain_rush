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
**Round 65, BUILD TOOLS (Claude's suggestion 2026-10-23 17:40; Dan: "you had wanted a round too. but I want to do the helicopter round
next").** NEXT, straight after Round 64, before any new track or feature (Dan, 2026-10-24 11:09: he meant build tools to come first). Make track and shortcut work rebuild only its own area (Round 42's quick builds extended to what
the new tracks build: trail decks, boardwalks, bridges, the mall's race pieces) so a round's fix-and-check cycles take minutes, keeping one
full Build Map at the end; trim the full Build Map (Roads and Businesses about 27 of its 62 minutes in Round 62).
Dan, 2026-10-24: "When Round 64 is done and committed, do Round 65 in TODO.md (QUEUED) right away." (Round 64 took 6 full builds of about an hour each.)

**Status (Claude, 2026-10-24): built and measured; editor tools only, the world unchanged (every build's scene the same as before by SceneDump). Nothing for Dan to drive.** Detail: `local/round65/` (times, scene dumps; git-ignored).
1. The full build: Build Map 2,616 -> 912 s (44 -> 15 min); the exe after it 822 -> 234 s; a full build with its exe about 60 -> 20 min. Cause: every generated mesh was an asset file of its own, and an asset file costs about 30 ms to write whatever its size (`Assets/Editor/BuildSpeedProbe.cs`: 1,000 meshes 25 s as files, 0.4 s packed); some 40,000 of them. Now packed 300 to a file as sub-assets (`MapBuilder.Quick.cs` PackMesh): Businesses 485 -> 26 s, Roads 480 -> about 25 s, Rest, Neighborhood, Hydrants, Driveways and Network the same way. Also: the manholes' street cut tests only the holes near each mesh; the road levelling's terrain edit runs only over the cells along each road, in parallel (R62LevelMains 165 -> 60 s, the same ground).
2. Quick builds take planning changes now (a track's grading, a door's apron, a road's levelling): the quick build compares the planning's ground with the record's and uses the new one where it changed; it refuses, naming them, if a step not being rebuilt also changed those samples, and it lists other steps' renderers standing on the moved ground (they keep their old heights until the round's full build). Test: the mound by Town Center Drive raised 0.3 m in its data, quick-built (6,337 samples moved), lowered again, quick-built: the scene identical to the full build's. Round 64's six one-hour rebuilds (aprons, the south lot's grading, the pit's ramps, the door frames) would each have been a quick build.
3. More quick steps: Shortcuts, CreekTrack, LakesRace (with TrailRace and Mall already): a quick build of all four is the full build's scene exactly.
Times on Dan's PC (in use): a quick build of the Mall 10 min, of the four track steps 11 min, quick with its exe about 14 min; the full build about 20 min. The quick build's own cost is now mostly the planning's replay (about 7 min).
`Builds/ChastainRush/` 2026-10-24 17:33 (the same world as Round 64's; Round 64's checks on it: doors, the north wing's grille, the mound, 12 of 12). Imported from Racer: nothing.

## QUEUED rounds
**Round 66, THE MAP'S EDGES: A WORLD THAT KEEPS GOING, NATURAL STOPS, A HELICOPTER BOUNDARY, ATLANTA ON THE HORIZON (Dan, 2026-10-24
11:41: "Speaking of edges, how can I handle this on my map especially now I have helicopters"; 11:44 "Yes" to Claude's three-part outline,
"And I like the idea of Atlanta in the distance"). After Round 65 (uses its faster builds).** ONE full Build Map at the end. Tuning limit.
1. SCENERY PAST THE EDGE (not playable): a ring of land a few km wide beyond every edge from the same USGS 3DEP elevation (download the
   extra tiles as for the mountain), low detail, no colliders, roads or traffic: terrain, a carpet of cheap far trees (Round 63's far-view
   kind), simple building blocks where OSM / NAIP show towns, water. Haze thickening toward its outer edge so its end is never seen. From the
   summit and from the helicopter at its ceiling the world must look like it keeps going: no cliff, no void.
2. THE HORIZON: real distant landmarks at their true bearings, distances and heights: the Atlanta skyline (downtown and midtown towers,
   about 30 km south-east; generic tower shapes, no brand names, lit at night), and the far ridges / mountains around (Lost Mountain, Pine
   Mountain, Sweat Mountain and the like, from elevation data where it reaches, as silhouettes beyond it). Day / night and weather apply
   (hidden in fog and rain, clear on a clear day).
3. CARS STOP NATURALLY AT THE EDGE: roads that leave the map end in "Road Closed" barricades / a detour (road-crew style), highways (I-75,
   I-575, US 41 and the like) in a jersey-barrier closure where traffic turns round (traffic and police nets end there cleanly); off road,
   the edge runs through dense woods, creeks or fence lines where they exist, with a soft invisible push-back so nothing gets through or
   falls off.
4. THE HELICOPTER'S BOUNDARY: near the edge a "Leaving the survey area" warning; keep going and a strong headwind slows it and swings it
   back (no crash, no teleport). A height ceiling, so the edges of the scenery are never seen from above.
Check: from the summit and from the helicopter at its ceiling, looking out each way (pictures, day and night, Atlanta's skyline in them);
drive every road that leaves the map (each stops at its closure, traffic turns); fly at each edge (turned back); frame rate at the summit
and at the ceiling against Round 63's numbers.
For Dan to check: fly to the edges and look out; Atlanta from the summit; drive off the map on a few roads.

Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 64
**Round 64, THE NOONDAY CREEK TRAIL PUT RIGHT: forward and reverse swapped, the route rerouted on the roads, shortcuts through the
mall, glass doors, the trail's signs, fences and walkers, a mound made a jump; the levelling's junction steps (Dan, 2026-10-23 21:06;
reports BUG-001 to BUG-008 of session `2026-10-23_20-28-33-291_9085ef`, read by Claude, after the Round 62 exe; his two map shots
`local/reference_photos/noonday_trail/r64_map_reverse_green_red.jpg` (on the reverse course) and `r64_map_forward_orange_red.jpg` (on the
forward course)).** Round 63 (the helicopter) is done; Dan moved on to this round 2026-10-24 01:35. WHEN THIS ROUND IS DONE (status written, committed), START ROUND 65 (QUEUED) AT ONCE, without waiting for Dan (Dan, 2026-10-24 11:10: he is away for a while). ONE full Build Map at the end. Tuning limit. Short results.
A. FORWARD AND REVERSE SWAPPED. Dan: "I know it's picky but I wanted the reverse track to be the forward track ... I thought I had said
   that when I requested it" (Claude misread his 02:32 note). Today's REVERSE (from the trailhead on Bells Ferry west along the trail, the
   red bridge, into the mall's main doors, ... back by Barrett and Bells Ferry) becomes FORWARD, and today's forward becomes REVERSE: names,
   start grids, checkpoints, records / ghosts / replays swapped with them (keep the times, under the right direction), map and menus.
B. THE ROUTE ON THE ROADS. Dan's key: GREEN (and the orange line on the forward shot, the same meaning) "is where I wanted to reroute the
   track. Not sure why the track was cutting off road when there was road there". Reroute the course along the lot / mall roads he drew:
   on the reverse shot the green line leaves the course north of the mall and runs east and down by the mall's north wing to rejoin the
   course in the concourse; on the forward shot the orange line runs along the east lot's road (inside the course's arc) from the east
   doors' corner down to the junction by CP4. Follow the existing roads there instead of cutting across grass / lot; checkpoints, closures,
   AI to suit, both directions. If a drawn line cannot be followed exactly (a wall, a gap), the nearest drivable way, and say.
C. SHORTCUTS THROUGH THE MALL: RED. Dan: "Red is where i want shortcuts". Reverse shot (the new FORWARD): from the main doors / center
   court south through the mall's south wing and out across the south lot to the course by CP8 (skipping the east loop). Forward shot (the
   new REVERSE): from the trail north of the mall straight south through the mall's middle and south wing to the course by CP4. Each is that
   direction's shortcut (standing rules: a real risk, no free way round, tire tracks / a clear hint at the entry, real physics on any ramp,
   breakables never slow a car; something creative inside, as the mall jumps). Code reports the time each saves clean and missed.
D. THE MALL'S DOORS. BUG-002 (x 498.28 z -1216.56 h 268, a mall entrance on the course, CP5 next): "these should be glass doors" (the
   picture: an open wall with no doors). BUG-006 (x 266.58 z -1217.04 h 164, another entrance on the course): "glass door entrance again".
   Every entrance the course (or a shortcut) uses gets glass doors that break as you drive through (no speed lost) and are whole next lap.
E. INSIDE THE MALL: BUG-005 (x 287.86 z -1278.10 h 336, the concourse before CP6): "there should been something here forcing you to turn,
   it isn't very obvious". Make the turn unmistakable: a barrier of breakable-looking but solid displays / planters / a closed store gate
   across the wrong way, arrows, the checkpoint arch placed where it reads.
F. THE TRAIL. BUG-004 (x 199.09 z -720.91 h 48, the trail by Hidden Forest Court): "This is a walking path not a road. It makes no sense
   to have road signs. Oddly enough, this seems to be the frist time road signs were added when i didn't ask, the one time it makes no sense.
   It would be good to have peopel walking or jogging this trail". Remove Round 62's 8 winding-road signs from the trail. Add people
   walking, jogging, cycling and walking dogs along the trail in Free Roam (and a few in races, who get out of the way as the game's other
   people do). BUG-007 (x 704.75 z -441.54 h 249, the boardwalk under I-575): "make the fences breakable. I saw ai kept getting hung up on
   these" (the boardwalk's black railings). BUG-008 (x 133.44 z -793.43 h 171, by Hidden Forest Court): "these fences should be breakable
   too". Make them breakable (as the game's fences: no speed lost); the AI must not hang on the boardwalk (its line through it).
G. BUG-003 (x 179.83 z -919.54 h 324, beside Town Center Drive, the north lot, CP8 next on the old forward): "dont understand if there is
   supposed to be hill here or none of it should be a hill. regardless lets fill this in and use as a jump" (the picture: odd grassy mounds
   with a flat cut between them). Fill the gap so it is one shaped mound and make it a real jump on the course's line (it may stay on the
   course, not a shortcut), landing clean.
H. BUMPS AND THE LEVELLING'S JUNCTION STEPS. BUG-001 (x 687.63 z -1668.31 h 272, Barrett Parkway, the course): "fix bump" (Round 62 noted
   Barrett at x 680 lifts a car flat out). And Round 62 C's open question (Dan, 21:06: "we can put this in the next phase with whatever big
   clean up code wanted to do"): take option (1), level up to each junction and keep the junction's own ground, so the narrower road no longer
   steps into the wider one's level; rerun the jolt scan (drives over 1.5 m/s and with air over 0.15 s should fall well below Round 61's).
Check: both directions (the new forward first) on all vehicles, the reroutes, both shortcuts clean / missed / round them, the doors break
and return, the turn at BUG-005 reads, the mound jump, the boardwalk with AI x2 each way (no hang), the jolt table. Pictures: Dan's spots,
the reroutes, each shortcut, the doors.
For Dan to check: race the new forward and reverse; the reroutes; the two mall shortcuts; doors, the turn, the trail's walkers, the mound
jump; bumps on Barrett and at junctions.

**Status (Claude, 2026-10-24): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round64/`. Bug reports read: session `2026-10-23_20-28-33-291_9085ef`, BUG-001 to 008.
A DONE. Forward is the old reverse (start on Bells Ferry, the trail west first), reverse the old forward (`make_trail_route.py`: geometry B written as "forward"). Your saved trail times, Top 10s, ghosts and replays swap direction once, the first time the game starts (`Race/NoondayTrailSwap.cs`; copies kept as *.before-round64).
B DONE. Orange: Mall Boulevard on up to Ring Road NW, Ring Road to the east lot, across the lot to Halverson's east doors (TCD barred now). Green: through Halverson Home (the north anchor, its lower floor a home store: in its portal, out its front) and the north wing corridor; the turn at the east escalator's foot; the road along Halverson Home's north side to the north lot. The escalator is rounded over at its top again (the raffle stage's ramp and lip gone, its board and balloons kept): reverse comes down it (flat out you fly off its top to the lower floor), forward climbs it.
C DONE. "Carroway renovation", each way: the main doors, the hall, across the center court, through Carroway (the south anchor, opened) and out its portal over the south lot (graded smooth). In Carroway the floor is torn up right across the store: a flooded pit 14 m long, a plywood kicker on each side on the line (each the other's landing, about 18 m/s clears it), plank ramps out at its two ends. Worn tracks off the course are the hint. The south and south-west doors are shut by grilles in this race. Scripted (Drifter, the AI's capped 12 m/s through the hall and court) against the lap between the same points: forward clean 42.5 s vs 44.0, missed 54.2, round the stunt 47.9; reverse 42.8 vs 43.5, 52.6, 47.2. Flat out it saves more; how much is yours to judge.
D DONE. Cause: a frame box in front of each doorway with no hole (the east anchor's portal frame; the main entrance's stepped block behind its glass tower): a wall you drove through. Opened; every door the race or shortcut uses is see-through glass in bronze frames, breaks, no speed lost, back next lap (pictures 64_bug002 / 64_bug006).
E DONE. A grille across the lower concourse just west of the north wing in this race (holds a car at 16 m/s both ways); the north wing's checkpoint arch in the corridor.
F DONE. The 8 road signs gone. Walkers, joggers, dog walkers and cyclists (new, on bikes) on the trail: up to 9 groups at once, at most 2 in a race (they step aside). The boardwalk's and the under-I-575 railings and the red bridge's approach fences break, cost no speed, come back.
G DONE. The berm's notch filled as one mound on the line; a real jump both ways (22 m/s: 1.0 s in the air, lands clean).
H DONE for your spot, PARTLY for the junctions. Option (1) as built: widest roads levelled first through their junctions, a narrower road fades out before a wider one's edge. Your BUG-001: both Barrett carriageways at the I-575 ramps laid as one curve: westbound 0.94 s in the air at 25 m/s -> 0.00, 1.40 s at 43 m/s -> 0.06. The jolt scan (197 drives, `round64_jolt_table.txt`, Round 62's build against this levelling): jolts over 1.5 m/s 50 -> 46, over 3 m/s 19 -> 18, but drives with air over 0.15 s 94 -> 101 (Round 61: 58). Two tries made: tuning limit.
Check (`-exeRound64`): the lap both ways on all 11 vehicles, through the north wing and Halverson Home, no dead stop (22 of 22); AI twice each way on Hard, traffic Normal: 12 of 12 finish, stuck 207 s in all (Round 62: 442), resets 87 (174); every barrier holds (20 forward, 24 reverse); final exe 23 of 23 (`round64_check_final.txt`). Not measured: where the AI's remaining stuck time is (whether any is at the boardwalk).
Builds: 6 full Build Maps, not one (check fixes: Carroway's door threshold, the south lot's tiers, the pit's ramps, the door frames, Barrett's curve twice). `Builds/ChastainRush/` 2026-10-24 12:56. Imported from Racer: nothing.
For Dan to check: race the new forward and reverse; Ring Road and Halverson Home; the Carroway shortcut both ways (worth the risk?); the glass doors; the turn at the north wing; the trail's people; the mound jump; Barrett at your spot; the other junctions (air over 0.15 s went up: your call whether to keep option (1)).

## Previous delivery 2: Round 63
**Reviewed: Dan flew the Round 63 build 2026-10-24 (asked only how to climb / descend) and moved on to Round 64; its two open questions (far view vs frame rate; controls and boost) stay as built until he says.**
**Round 63, A HELICOPTER TO FLY (Dan, 2026-10-23 17:49: "when I was driving around the new area, I came across the airport and was
thinking, why have an airport if we don't have flying vehicles? So how much would it be to add a helicopter as a start? With the area this
big, it would be a good way for me to survey the map more quickly"; 17:51 "yes" to Claude's outline). Dan, 2026-10-23 21:07: "I want to do the helicopter round next".** ONE full Build Map at the end (the helipads). Tuning limit. Short results.
1. FLYING IT: start from the police helicopter (`Police/PoliceHelicopter.cs`: model, rotor, sound). Arcade controls, not a simulator:
   left stick climb / descend and turn (yaw), right stick tilt forward / back / sideways to fly (or the pattern Code finds best on a pad;
   say it), triggers for fine climb / descend if useful; keyboard too. Lets go of the sticks = holds a steady hover at its height. Quick
   enough to cross the map in a minute or two; a boost if needed. Rotor sound, wind, a downwash on the ground (dust / water / grass) near it.
2. LANDING / CRASHING: lands on anything flat enough (ground, lots, roofs, the mall's roof, the summit); rotor or body hitting trees,
   buildings, poles: a crash that is forgiving (spin down and reset after a moment, onto the nearest open spot), never stuck.
3. WHERE: a helipad at Cobb County International Airport and one on Wellstar Kennestone's roof (it has a real one), each with a parked
   helicopter to take (walk-up not needed: drive onto the pad, press a button to swap, as the game's vehicle swap does); and "Helicopter"
   in Free Roam's vehicle choice. Free Roam only, never in races. Police: the chase helicopter is unaffected; the player's is not chased.
4. SEEING FAR (the real work): from the air much of the 129 km2 is in view at once. The fog now closes at about 650 m (Round 61's summit
   picture was solid fog): in the helicopter (and at height in general) the fog pulls back to show the map out to several km. Distant
   trees and buildings as cheap far versions (impostors / merged meshes / lower detail by distance), terrain far detail as needed, so the
   frame rate at 1080p stays in Round 61's range flying low over the old neighborhood and high over the whole map. Report fps at 50 m,
   200 m and 500 m up over Chastain Glen, Town Center and the mountain.
5. SURVEY: a camera option looking straight down (and the chase camera), a height and speed readout, the minimap / map following it,
   exploration fog clearing under the helicopter as it flies (as driving does: Dan wants to survey the map).
Check: take off from each helipad and from Free Roam's pick; fly to the summit, Town Center and Chastain Glen and land on each; crash into
a tree and a building (resets, not stuck); hover hands-off 30 s (drift under a few m); the fps table. Pictures: from 500 m over the map,
the straight-down camera over Town Center, each helipad.
For Dan to check: fly it (controls, hover, landing, crashing); how far you can see and how smooth it is up high.

**Status (Claude, 2026-10-24): built and checked in the exe by script, not yet flown by Dan.** Evidence `Data/screenshots/round63/`. No bug reports newer than Round 64's session.
1 DONE. The "Sky Survey" helicopter (our own Blender model, `Tools/Blender/helicopter.py`; flight `Vehicle/HelicopterFlight*.cs`, the player's own vehicle object so camera, map, minimap and resets work). Pad: left stick up / down climb / descend, left / right turn; right stick flies; RT / LT fine climb / descend; RB (or L3) boost. Keyboard: W / S / Q / E fly, A / D turn, Space / C climb / descend, Shift boost. Let go: it hovers (30 s hands off at 40 m: 0.04 m drift). Top speed 80 m/s (179 mph), boost 140 m/s: the summit from Chastain Glen, 7.8 km, in 85 s. Rotor and wash sound (the police helicopter's), wind, a downwash of dust or spray under 30 m.
2 DONE. Lands on anything flat (it never comes down faster than it can stop). A crash (the body into something at speed, the rotor into a crown, a wall, a pole or the ground, the tail rotor, or set down in water) spins down and after 2.6 s puts it on the nearest open ground clear of trees; Y / R does the same. Checked: into a tree crown and into Town Center's front, both reset and lift off again.
3 DONE. Helipads where OSM has them (`MapBuilder.Helipads.cs`, step Helipads): the airport's on the apron, Kennestone's on its roof (way 1076463453, a deck on the roof); a parked helicopter on each; A / H on a pad takes it, landed on a pad A / H gives back the vehicle you came in (`HelipadStands`). Not asked for: both pads are fast-travel places (the roof pad is not reachable by car). "Sky Survey" is in Free Roam's garage only; never in races, rosters, the Stunt event, split screen or police modes; hidden police ignore it.
4 PARTLY. The view opens with height (`World/FarView.cs`): from 30 m up, 10x the fog by 450 m (6.5 km by day), small detail cells hidden past 900 m, far trees as 8-sided shapes (thinned past 1.8 km, in 1 km blocks past 3.5 km), coarser far terrain. 1080p, fps average / slowest 1 %: Chastain Glen 50 / 200 / 500 m 170/118, 162/111, 164/122; Town Center 140/30, 105/78, 99/75; summit 163/113, 100/74, 88/66 (before tuning: 82 and 51 at 500 m). Two tries made: high over the woods it stays under Round 61's 160 to 190 (tuning limit). The map's edge shows from high up near it.
5 DONE. X / V cycles Chase, Far chase, First person and Straight down (helicopter only); the readout (height over the ground, mph, heading, climb rate); exploration clears wider with height (up to 300 m round).
Check (`-exeRound63`, `Checks/Round20Runner.R63.cs`): 22 of 22 (`round63_check_1080p.txt`): hover, three flights landing on their spots, both pads both ways, both crashes; fps table above; pictures `63_*.png`.
Builds: one full Build Map (1 h 6 min), then code-only exes on its scene; `Builds/ChastainRush/` 2026-10-24 00:34. Imported from Racer: nothing (the police helicopter's two sound clips shared).
For Dan: (a) the far view's cost high over the woods: accept, or trade view distance / far trees for frame rate; (b) the controls' layout, the speeds, and the boost on RB.

## Previous delivery 3: Round 62
**Reviewed: Dan drove the Round 62 build 2026-10-23 20:28 (Noonday Creek Trail both ways); his notes are Round 64 (after the helicopter).**
**Round 62, A NEW TRACK: THE NOONDAY CREEK TRAIL LOOP round Town Center (forward and reverse, no shortcuts yet), and the new land's bumps
(Dan, 2026-10-23 02:23; reports BUG-001 and BUG-002 of session `2026-10-22_23-37-02-216_30fa55`, read by Claude, after the Round 61 exe;
his pictures in `local/reference_photos/noonday_trail/`: Google / Tripadvisor / county map: reference only, never traced or measured).**
Round 61 is reviewed. ONE full Build Map at the end (quick builds while working). Tuning limit. Short results.
A. THE TRACK. Dan: "I want a track based on the green line. The drawn trail gives you a more detailed shape of noonday creek trail and I
   have put in pics so you can see what it looks like."
   THE LINE (`route_green_line.jpg`, Google's green highlight of the trail; `trail_map_cobb.png`, Cobb's trail map, its dark green
   "Existing" line has the trail's true shape): a loop round Town Center at Cobb. From the trail's access point by Hidden Forest Court
   (north of the mall, by Big Shanty), east along Noonday Creek, under I-575, past Chastain Meadows Parkway to the Noonday Creek
   Trailhead by Bells Ferry Road; south along Bells Ferry Road to Barrett Parkway; west along Barrett Parkway (across / under I-575) to the
   mall's ring; north past the mall's west side (Google's green crosses the mall between its buildings, by the Town Center at Cobb pin) and
   through the lot to Town Center Drive; round to Hidden Forest Court. Use the trail's REAL geometry from OSM (`raw/osm_ext1.json` /
   `osm_ext2.json`: 44 ways named "Noonday Creek Trail"; Round 59 did not bring footways / cycleways into map.json) wherever it exists;
   where it runs beside a road (Bells Ferry, Barrett Parkway) race on that road / its side path, Code's pick. Where the line crosses the
   mall: if it really passes through the building, the race goes in one entrance and out another through Round 60's drivable mall (that
   would be a highlight); if it only passes between buildings, follow it. Say which.
   DAN'S CHANGES (2026-10-23 02:32): "I want the forward track to start at Bells Ferry, cut through the parking lot and then until the
   Noonday creek trail. Noonday Creek trail does go on further than this but I want to cut through town center mall, lets create at least
   a jump or two each way in the mall, and be creative with it. And yes since this is the actual trail I do want to call this noonday creek
   trail, though technically our existing trail is a proposed extension that hasn't been built yet".
   Claude's reading: FORWARD starts on Bells Ferry Road (start / finish there, by the Noonday Creek Trailhead), south on Bells Ferry, west on
   Barrett Parkway, then off the road and across the mall's parking lot to the mall, THROUGH the mall building (in one entrance, along the
   concourse(s), out another: Round 60's drivable mall), across the lot on the other side to the trail by Town Center Drive / Hidden Forest
   Court, then the real trail east along the creek, under I-575, back to the trailhead at Bells Ferry. The real trail goes on round the
   mall; the race does not, it cuts through the building. REVERSE: the same loop the other way. If Code finds a reading that fits Dan's
   words better (e.g. which lot, which entrances), say so.
   THE MALL: at least one jump, ideally two, EACH WAY (forward and reverse may share a feature if it works both ways, else their own),
   creative and belonging in a mall, not dirt piles (standing rules: real physics off ramps; nothing slows a car on a ramp). Ideas, Code
   picks and says why: launch off the upper level's edge over a center court; an escalator as a kicker; a car-raffle display stage with its
   ramp; a fashion runway; the fountain's rim; a fallen store sign; the play area's slide. Racers smash the mall's breakables as they pass
   (they must not slow them, Round 60). Doors and glass on the race line break, and are whole again for the next lap (as the barricades).
   No shortcuts this round: Dan will pick them after driving it. 3 laps by default. NAME: "Noonday Creek Trail" (Dan). RENAME the old
   made-up woods track to "Noonday Creek Extension" (Dan, 02:34, yes to Claude's suggestion: it is really the proposed extension that has
   not been built): its name in the menus, records, replays, loading screen, map labels; keep its records and replays (rename, not reset). Built like the other races (route, checkpoints, closures of roads
   that leave the lap, AI, ghosts, records, replays, the loading screen's inset, menus). Traffic on the roads it uses, as other tracks.
B. THE TRAIL, AS THE PHOTOS SHOW IT: a dark asphalt multi-use path about 3 m wide with a dashed yellow center line, grass shoulders, pine
   and hardwood woods close on both sides (`photo3_trail_woods.jpg`); black steel pipe railings where it runs beside drops and under
   I-575's concrete bridge (piers, green steel beams, `photo1_i575_underpass.jpg`); a rust-red steel truss footbridge over the creek with
   a concrete abutment and a grey timber-and-steel railed approach, kudzu and brush along the dark creek (`photo2_red_truss_footbridge.jpg`);
   yellow winding-road warning signs (breakable). The path at its REAL width (standing rule), the race also on the grass beside it: clear
   and grade the shoulders so four vehicles can race and pass (Dan on the made-up Noonday Creek: it had to be wider so vehicles could
   weave); the bridges and the underpass stay real chokepoints but never a place a vehicle wedges (railings solid, no snag). Trees off the
   cleared shoulders solid as elsewhere.
C. BUMPS IN THE NEW LAND. BUG-001 (x 617.23 z 9.33 h 273, Big Shanty Road NW): "level all thesse bumps". BUG-002 (x 512.91 z 18.66 h 161,
   I-575 (Phillip Landrum Memorial Highway)): "flatten bumps". Dan keeps finding these on the generated roads (Rounds 60, 61, 62), so run
   Round 61 D's levelling on ALL the stage 1 and 2 roads of class motorway, trunk, primary, secondary and tertiary (I-75, I-575 and their
   ramps, Big Shanty, Barrett, Chastain, Chastain Meadows, Bells Ferry, US 41, Town Center Drive, Kennesaw Mountain Drive, the rest) and
   the new track's roads; then a jolt scan along each at speed; report the worst before / after and Dan's two spots.
Check: the loop both ways on all vehicles, no stop, no wedge (the bridges, the underpass, the mall if used); AI races x2 each way, Hard,
traffic Normal, all finish; every closure holds; the jolt table. Pictures: the start on Bells Ferry, the underpass and the red bridge next to Dan's
photos, the mall run and each mall jump, Bells Ferry and Barrett Parkway stretches, Dan's two bump spots.
For Dan to check: race the Noonday Creek Trail both ways (the mall and its jumps; then pick its shortcuts); the bumps on Big Shanty, I-575 and the other main roads.

**Status (Claude, 2026-10-23): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round62/`. Bug reports read: session `2026-10-22_23-37-02-216_30fa55`, BUG-001 and 002.
A DONE. "Noonday Creek Trail" (`make_trail_route.py`): start on Bells Ferry by the trailhead, south, west on Barrett, Mall Boulevard and Town Center Drive, the east lot, THROUGH the mall (in Halverson's east doors, along the upper concourse, down the balcony to the main doors; Google's green line does cross the building), the north lot over Town Center Drive, the red bridge, the real trail east under I-575 to the trailhead. Forward 5723 m, reverse its own lap 5767 m, 11 checkpoints, 3 laps, no shortcuts. Mall jumps, physics only: forward the car raffle stage off the east escalator's top over the escalator well, and the mattress mountain in Halverson's aisle; reverse the escalator itself as the kicker (through Halverson's front 3.5 m up at 29 m/s), and the mattress mountain. Glass, doors and breakables cost no speed and are whole the next lap. The old track is "Noonday Creek Extension" (records, ghosts and replays kept: `records_as`).
B DONE. The path 3 m dark asphalt with yellow dashes on a level 11 m graded bench; black pipe railings under I-575; the rust-red truss footbridge; a boardwalk over the creek's three crossings under I-575 (OSM has the trail bridged there); 8 breakable winding-road signs.
C PARTLY. Every stage 1 and 2 main road and its ramps levelled across its width (870 road lines, 316 km; `MapBuilder.Round62.cs`), I-575's decks at the old core's edge and Bells Ferry's bridge over Noonday Creek laid as one curve with 150 m of road each side. Dan's spots on a 25 m/s drive, before / after: BUG-002 I-575 jolt 3.28 m/s and 0.44 s in the air / 0.01, wheels down; BUG-001 Big Shanty under I-575 0.98 to 1.26 s in the air / 0.04 and 0.30 s (`round62_dan_spots.txt`). The jolt scan (197 drives at 28 m/s, `round62_jolt_table.txt`): drives over 3 m/s 26 -> 19 (Old 41 7.4 -> 2.0, McCollum 6.3 -> 2.5, Big Shanty 6.3 -> 3.2, Frey 6.8 -> 4.2), but over 1.5 m/s 37 -> 50 and with air over 0.15 s 58 -> 94: where two levelled roads meet, the narrower one now steps into the wider one's level (Barrett at (634, -1669), near your Round 61 spot, throws a car 1.6 s; Chastain Road, Stilesboro, Kennesaw Mountain Drive worse). Two tries made (the second: wider road wins, fixed a ramp re-grading Barrett's lane); tuning limit reached.
Check (build 6, the exe): the loop on all 11 vehicles both ways 22 of 22 round, no wedge (`round62_check_loop_pics.txt`); AI twice each way on Hard, traffic Normal: 4 of 4 races, every AI finishes, laps 220 to 270 s (`round62_check_ai.txt`); every closure holds, 23 forward, 21 reverse (`round62_check_barriers.txt`); pictures `62_*.png`. Left: the reverse escalator jump's landing costs 24 -> 10.5 m/s; flat out (43 m/s) Barrett at x 680 lifts a car.
Builds: 6 full Build Maps, not one (a mesh-name clash took every road bridge's deck, then fixes from each check). `Builds/ChastainRush/` 2026-10-23 13:57. Imported from Racer: nothing.
For Dan: the levelling's junction steps: (1) level up to a junction and keep the junction's own ground (one more build), or (2) back to Round 61's five roads plus your two spots and the course's roads. Until you say, build 6's levelling stays.

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
