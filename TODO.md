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

## QUEUED rounds
Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 77
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

## Previous delivery 2: Round 76
**Reviewed: Dan drove the new land 2026-10-27 21:09; his notes are Round 77.**
**Round 76, MARIETTA SQUARE: ONE MORE SMALL PIECE OF MAP; the Kennesaw cut faces; disk clean-up (Dan, 2026-10-27 16:09, with a Google map of downtown
Marietta: `local/reference_photos/extension/marietta_square_area.png`, reference only, never traced).** Round 75 is reviewed. Item 0 (CHECK IT AS
DAN PLAYS IT) applies. Quick builds while working; ONE full Build Map at the end. Short results.
A. THE AREA. Dan: "there is just a little more area that I want to add. We got so close to it, and I think it would be a great little area to
   have. If it comes down to it, I'd rather have this than some of the expansive wooded areas we have but prefer if we could just add this one
   little section." His map: Marietta Square and the blocks round it: Glover Park in the square, the Marietta Cobb Museum of Art, Marietta
   Square Market, Church / Cherokee / Lemon / Fort / Lawrence / Washington / Roswell Streets, Lawrence Street Park, Fairground Street, the
   120A loop (North / South Marietta Parkway) round the west side, out to about Cobb Parkway / Barnes Mill Road on the east; south to about
   Chester / Waterman Streets. The square sits roughly 33.953 N, -84.550 W, just south of the map's south edge (33.95632). So:
   (1) ONLY THIS SECTION: add the tiles that take in downtown Marietta (Round 59 / 61's way: new land beyond the edge, nothing that exists
       moves, the 3DEP tile(s) south fetched and joined), not a strip along the whole south edge. Code sets the bounds from OSM (the square,
       the blocks round it, the loop, with a margin) and says them; the map's edge goes round the new piece (Round 66's scenery ring,
       closures, the helicopter / plane boundary all follow the new edge).
   (2) If memory or frame rate really needs it, trim the far scenery or an outlying wooded corner of the playable map instead (Dan prefers
       this over some expansive woods), but try first to just add it; say what was traded, if anything.
B. MARIETTA SQUARE MADE TO LOOK LIKE ITSELF (as Town Center, Round 60): Glover Park in the middle (lawn, big trees, paths, the gazebo /
   bandstand, the fountain, benches, lamp posts, a few people); the square's historic two- and three-story brick storefronts all round, with
   awnings, shop windows and signs (names faked), the old theater's marquee on the square (fake name), the railroad along the square's west
   side with its old depot; Marietta Square Market (a food hall), the museum (a classic columned building), churches nearby; on-street
   parking, crosswalks, brick sidewalks; the neighborhoods round it as generated, with the game's houses. Breakables as elsewhere (benches,
   planters, signs, lamp posts, café tables). A fast-travel spot "Marietta Square". Traffic, police, people, the map / minimap / street names,
   the exploration fog, all extended. No race this round (a downtown race could come later).
C. THE KENNESAW CUT FACES (Round 75 C's open item; Dan flagged the same kind of drop-off as wrong, Claude recommends doing it): the 66 faces of
   5 to 11 m beside the Kennesaw race's lines (`bank_scan_after.txt`) where two legs at different heights pass about 40 m apart or a bank is
   held off the drive: blend the ground between the two lines' heights into smooth slopes (no cliffs), the drive and the trails untouched,
   the race's lines and jumps unchanged (lap times before / after). Pictures of the worst five before / after.
D. DISK SPACE: A CLEAN-UP AT THE END OF EVERY ROUND (Dan, 16:13: "I noticed that this is taking about 40 gbs of space. I know with Woodstock
   Rush, every round we would have some clean up so it didn't take up so much space on the computer. Is this possible here as well?").
   Claude measured (2026-10-27): Builds 10.2 GB (the player build 5.1 GB AND the ChastainRushDev profiling build 5.1 GB), Assets/Generated
   8.1 GB, local/ 5.7 GB (old rounds' check output: local/round17 to round75, r69perf 0.7 GB, determinism ...), raw/ 2.2 GB, .git 1.4 GB,
   Data/screenshots 0.45 GB, Library / Temp too big to measure quickly.
   (1) This round: clean up now and report the space before / after per folder. Delete: Builds/ChastainRushDev (rebuilt only when a round
       needs profiling, then deleted again); local/'s old round folders and scratch (keep the last three rounds' and everything Dan put
       there or a rule names: reference_photos, owner_*.json, debug_reports, race_records, bump_log); Temp/; stale Unity caches in Library
       that Unity safely regenerates (say which; never the import cache unless a rebuild is planned anyway); git's own housekeeping (git gc).
       Keep: raw/ (the map's source data), Assets/Generated (the built world), Data/ (committed), the player build.
   (2) A STANDING RULE (copy into CLAUDE.md "Process"): every round ends with that clean-up after its final build (only the player build
       kept, local/ trimmed to the last three rounds, temp files gone) and its status gives the disk use before / after in one line.
Check: item 0; numbers before / after (build time, memory, frame rate in the old neighborhood, Town Center and the square); drive from
Chastain Glen to the square along the roads; pictures of the square from the park, each side of the square, the depot and the market.
For Dan to check: drive and fly to Marietta Square; Glover Park and the storefronts; frame rate; the Kennesaw slopes; the disk space freed.

**Status (Claude, 2026-10-27): built (full Build Map + exe 2026-10-27 19:38), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round76/`.
0 DONE: every item below was looked at in the exe's pictures from the player's camera.
A DONE. Only one piece: tile columns 15 to 18, two rows below the old south edge (x 1327.7 to 3983, z -9590 to -8311.3; 33.9427 to 33.9537 N, -84.5546 to
-84.5256 W): 8 tiles, 3.4 km2, 1092 road pieces, 1740 buildings, 8467 trees (`mapgen_piece.py`; its ground is its own file, nothing that existed changed: map.json's lists
are the old ones as a prefix, checked). The map is no longer a rectangle: the edge's walls and push-back, the road closures (262 -> 283), the helicopter / plane boundary,
the scenery ring and the map picture follow the land's outline (`MapLand`). Drives at the edge in the exe: stopped 1.3 m inside the piece's west edge and 45 m inside its
south edge; across the old edge into the piece it kept going (`76_A_edge_drives.txt`). Nothing traded away (no woods trimmed, the scenery kept). Numbers (1920 x 1080
window, not measured at 3840 x 2160; `round76_numbers.txt`): Build Map 1088 s (Round 75: 1236 s); world up 28.6 s; Unity reserved 5704 MB (Round 61: 5182); 312 tiles, 927,728
trees; fps average / slowest 1 %: old neighborhood 150 / 64 (Round 61: 163 / 71), Chastain Glen 160 / 114, Town Center 191 / 135 (191 / 97), Marietta Square 175 / 124.
The old exe was not measured this round (it was replaced by the first build), so Round 61's numbers are the before.
B DONE. Glover Park (brick walks, the fountain, the bandstand on OSM's gazebo in the park; its two other OSM structures left out for an open lawn), 16 benches, 16 lamp
posts, 4 planters and 25 café tables that break, 42 old storefronts as two- and three-story brick shops with awnings, upper windows, a cornice and made-up names, the
theater's marquee (THE PEACHTREE PALACE), the food hall, the museum's columned portico, the depot and its platform, 2.5 km of railroad on ties, crosswalks and parking
marks, brick sidewalks; downtown's county buildings as offices (they had the fire-station look); the piece's houses as three craftsman neighborhoods; fast travel
"Marietta Square". Pictures `76_B_*` (from the park, each side, the fountain, the depot, the market, the museum): brick storefronts with awnings and names, the
bandstand and fountain in the park, benches, lamps, people. Drive Chastain Glen -> the square along the roads: 10,918 m, through in 451 s, never under the ground.
Open: two dark boards in front of the museum seen from Anderson Street (1795, -8592, heading 167; `76_B_museum.png`), not from Marietta's code (they stayed when its
sign was taken out): for Dan to check. Kennesaw lap times not measured (the route file and the bench's heights are unchanged).
C PARTLY (tuning limit, two tries). Round 75's scan was wrong twice over: its "other leg" test counted the same leg 41 m on (every bank stopped about 20 m out), and its
face scan compared offset lines by index (they drift apart round bends). Now the banks reach their full width and, between two real legs, the ground takes both legs'
heights (`legs`); the bench, its heights and the jumps are unchanged (route file unchanged, the grade's `y` unchanged). Pictures `76_C_face*_before/after`: the
descent's banks are wider and rounder. A grid scan of the built ground (`bank_faces.py`) still finds faces where a bank is held off Kennesaw Mountain Drive, the worst
13 m beside the summit leap's way back (`76_C_left_cut_by_the_drive.png`): fixing those means regrading the drive's shoulder: Dan to decide.
D DONE. `Tools/cleanup_round.py` and the standing rule in CLAUDE.md. Disk use (these folders) about 41 GB -> 27.9 GB: Builds 10.8 -> 5.7 (ChastainRushDev gone), local 6.1 -> 0.4, Library 10.8 -> 8.4 (BuildHistory), the rest kept (Assets/Generated 9.2, raw 2.3, .git 1.4).
For Dan to check: drive and fly to Marietta Square (fast travel "Marietta Square"); Glover Park, the storefronts, the theater, the depot; the frame rate there; the
Kennesaw banks on the descent; whether to regrade beside Kennesaw Mountain Drive; the disk space freed. Lost in the first clean-up: old check inputs in local/round17 to 72
that scripts regenerate (r59 / r61 places, jolt_shots, round68 design.json) and local/round32/shots.json.

## Previous delivery 3: Round 75
**Reviewed: Dan tested it 2026-10-27 ("I don't have much feedback on the things I tested"); the quarry jumps not yet seen.**
**Round 75, STREET AND AIR RACES IN RACE SETUP (THE GARAGE THAT FOLLOWS), THE MAP SHOWS WHAT IS BUILT, STREET NAMES, CLOUDS BACK, a drop-off
(Dan, 2026-10-27 12:04; report BUG-001 of session `2026-10-27_11-27-43-992_53ef4c`, read by Claude, after the Round 74 build).** Round 74 is reviewed. Item 0
(CHECK IT AS DAN PLAYS IT) applies to every item. Quick builds; ONE full Build Map at the end. Short results.
A. STREET OR AIR, AND THE GARAGE THAT FOLLOWS. Dan: "the garage is still kind of broken. I think there needs to be another line in the menu
   to choose between street and air races. And depending on this, your garage becomes available. For example, I did a street race and then
   selected the new air course, and my only garage choices were street vehicles. I assume I will have the same issue if I try to select a
   road race after doing a flying race. So this also means I didn't actually get to test the new plane since I didn't have enough time to do
   2 races". In race setup: a new first row RACE TYPE: Street / Air. It filters the Route row (street courses or air courses) and the
   Garage (ground vehicles or planes), at once, in either order, without needing a race in between; each type remembers its own last
   course and vehicle (Round 70 D's saves, plus the last plane). Opponents etc. as each type allows. Check by item 0 from the title, by pad
   and keys: street race -> finish -> switch to Air -> pick the Sky Dart -> race; and air -> street the same; and straight from the title to
   Air.
B. THE MAP SHOWS WHAT IS BUILT, STREET NAMES, CLOUDS (the queued small fixes, Dan 2026-10-27 11:16 to 11:24, word for word below):
- The map picture's lots (Dan, 2026-10-27 11:16: "why didn't it fix the mall parking lot?" after Round 74 J). Cause (Claude): MapBuilder.MapPicture
  fills only OSM's parking outlines (map.parking); Town Center's lots come from MapBuilder.MallLots (Round 60: OSM maps none round the mall),
  so the map never sees them. Dan, 11:18: "I would like that stuff fixed in general. For example, I feel like community swimming pools
  should show up as well". So the general fix: the map picture and minimap must show what the BUILT world has, not only OSM's lists:
  every lot and paved area the build makes (the mall's lots, the stunt park, the quarry pads and lanes, the helipads), pools (community and
  backyard), tennis courts, plazas and paths, the trails' boardwalks and bridges, the shortcuts' built pieces where they are ground, water
  as built. Code picks how (e.g. draw from the build's own records of what it placed, or a top-down orthographic render of the built scene
  per tile, styled to the map's look) and says why; whichever it is, a new built thing must show on the map without anyone remembering to
  add it. Pictures of the map at Town Center, a neighborhood pool (Chastain Lakes' and Enclave's), the stunt park and the quarry.
- Clouds back down at the summit (Dan, 2026-10-27 11:24, on Round 74 F raising the cloud layer over high ground: "I thought it was cool
  that you could potentially jump over clouds"). Undo that change: the cloud layer at its old height so the summit and the leap's flight
  are up among / above the clouds again; keep it from looking like a solid white wall at the lip (soft, broken cloud the car flies over
  and through). Picture from the summit leap's run-up and mid-flight.
- Street names on the map (Dan, 2026-10-27 11:19: "when zoomed in, it would be great to get street names as well"). On the map screen, past
  a zoom level, label the streets with their names (along the road, readable, not upside down, not crowding: main roads first, side
  streets at closer zoom, each name once per stretch on screen); highways by their shields / numbers (I-75, I-575, US 41). The minimap may
  show the current street's name (it already knows it). Pictures at three zoom levels over Chastain Glen and Town Center.
C. BUG-001 (x -1268.09 z -6341.39 h 100, a gravel track off Kennesaw Mountain, near the summit leap's way back): "Lets fix this so there isn't a
   drop off for the road but a smooth slope" (the picture: the track runs along the foot of a cut bank, a sheer step of ground beside it).
   Grade the bank into a smooth slope; and check the summit leap's way back and the mountain's new tracks for any other such cut faces.
Check: item 0 for every item; A's three paths; B's pictures; C before / after from Dan's spot.
For Dan to check: race setup Street / Air and the garage both ways; the Sky Dart in an air race; the map (built things, pools, street names);
the clouds at the summit leap; the slope at his spot.

**Status (Claude, 2026-10-27): built (full Build Map + exe 2026-10-27 13:45), checked in the exe through the game's own input, not yet driven by Dan.** Evidence `Data/screenshots/round75/` (`round75_check_final.txt`: 15 of 16, `round73_check_leap.txt`, `75_*.png`, `bank_scan_*.txt`).
0 DONE: each item below was looked at in the exe's pictures from the player's camera.
A DONE. Cause: after a race, race setup's Route row changed the name but not the course (`RaceController.Reload` only in Free), so the garage kept the old course's vehicles. Now RACE TYPE (Street / Air) is the first row and filters the Route row; a course chosen there leaves a finished race first; each type keeps its last course and vehicle (vehicle.json "plane"). The three paths through the menus (`75_street_then_air_*` pad, `75_air_then_street_*` keys, `75_title_air_*`): the garage held only planes / only ground vehicles, the Sky Dart and the Drifter Twin raced, and from the title Air brought back the Quarry Air Race and the Sky Dart by themselves.
B map DONE. The map's picture now also draws the built scene from above: every visible mesh's upward faces on or over the ground. That covers Town Center's lots, the pools, the courts, the stunt park and the quarry's lanes (`75_map_town_center_z14`, `_lakes_pool_z16`, `_enclave_pool_z16`, `_stunt_park_z12`, `_quarry_z9`). Anything built later shows up by itself: every quick build redraws the picture too. One limit: the picture is 1.5 m a pixel, so a pool is a few pixels.
B street names DONE. Names lie along their roads, never upside down: main roads from zoom 2.6, side streets from 6.5, I-75 / I-575 / US 41 as shields (`Data/road_refs.json`, `Tools/mapgen/road_refs.py`). Pictures: `75_map_town_center_names_z2.5/5/10`; the Chastain Glen ones are in `local/round75/glen_pics/` (not committed: Dan's street). The minimap shows the street you are on ("Chastain Glen Ln"). A name can sit under a place's label or the egg list.
B clouds DONE. Round 74's rise is undone; within 260 m of the camera a cloud breaks up (dithered) instead of standing as a white wall. In the leap run (`73_leap_lip`, `_midair`): a cloud at the summit's height off to the side; none came within 260 m, so the break-up was not seen in a picture. The leap is unchanged: Drifter Twin 9.3 s / 336 m, Street Classic 7.9 s, Needle 600 7.7 s.
C DONE at Dan's spot, PARTLY elsewhere (tuning limit). Cause: the summit leap's run-out ended in a 56 m-wide shelf 3.8 m above the start of its track (the grade's open end). Now the run-out eases 46 m down and narrows to the track's width, and the track starts at the run-out's height (`75_BUG-001_after`, `_from_the_track`: a smooth slope; steepest within 40 m 1.25 m in 1 m, at (-1239, -6372)). The banks are sized from the ground at their own edge, held 2 m off every road and trail, and stop at the middle between two legs of the race line. The catch's side faces went from 9 m to 3 to 6 m. Still there: faces of 5 to 11 m beside the Kennesaw race's lines, where two legs at different heights pass about 40 m apart and where the banks are held off the drive (`bank_scan_after.txt`: 66 spots; before: 67). Fixing those means the build blending between two lines' heights (GradeAlong takes the nearest line's), which touches every graded line: Dan to decide.
For Dan to check: race setup Street / Air and the garage both ways, the Sky Dart in an air race; the map at Town Center, the pools and the stunt park, street names zoomed in, the minimap's street; the clouds at the summit leap; the slope at his spot; whether to take on the faces beside the Kennesaw race's lines.

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
