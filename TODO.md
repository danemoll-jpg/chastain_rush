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

## QUEUED rounds
**Round 64, THE NOONDAY CREEK TRAIL PUT RIGHT: forward and reverse swapped, the route rerouted on the roads, shortcuts through the
mall, glass doors, the trail's signs, fences and walkers, a mound made a jump; the levelling's junction steps (Dan, 2026-10-23 21:06;
reports BUG-001 to BUG-008 of session `2026-10-23_20-28-33-291_9085ef`, read by Claude, after the Round 62 exe; his two map shots
`local/reference_photos/noonday_trail/r64_map_reverse_green_red.jpg` (on the reverse course) and `r64_map_forward_orange_red.jpg` (on the
forward course)). START AFTER ROUND 63 (the helicopter).** ONE full Build Map at the end. Tuning limit. Short results.
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

**Round 65, BUILD TOOLS (Claude's suggestion 2026-10-23 17:40; Dan: "you had wanted a round too. but I want to do the helicopter round
next").** After Round 64, when Dan says. Make track and shortcut work rebuild only its own area (Round 42's quick builds extended to what
the new tracks build: trail decks, boardwalks, bridges, the mall's race pieces) so a round's fix-and-check cycles take minutes, keeping one
full Build Map at the end; trim the full Build Map (Roads and Businesses about 27 of its 62 minutes in Round 62).

Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 62
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

## Previous delivery 2: Round 61
**Reviewed: Dan drove the Round 61 build 2026-10-22 23:37 (free roam in the new land); his only notes are bumps (Round 62). The Build Map speed-up after it is accepted.**
**Round 61, THE MAP EXTENSION, STAGE 2: WEST TO SUMMERBROOKE AND SOUTH OVER KENNESAW MOUNTAIN (airport included), with the work that keeps
it fast; three bumps in the new land (Dan, 2026-10-22 10:04; reports BUG-001 to BUG-003 of session `2026-10-22_09-43-30-831_9bfdff`, read by Claude, after
the Round 60 exe; his map `local/reference_photos/extension/stage2_kennesaw_mountain.png`, Google: reference only, never traced).** Round 60
is reviewed ("Everything looks good in the mall"). Round 59's rules hold (nothing that exists moves; the new land at more negative x / z;
everything that uses the map extended; numbers before / after). Tuning limit. Short results.
A. THE AREA. Dan: "For the further expansion can we include all of this area? This also cover kennesaw mountain which would be super cool
   to have". His map shows Cobb County International Airport (McCollum Field) and Laura Lake's quarry, Smith-Gilbert Gardens, Summerbrooke,
   Stilesboro Road, Kennesaw Due West Road, US 41 / Old 41, Barrett Parkway's west end, Kennesaw Mountain National Battlefield Park and
   the Wellstar Kennestone hospital at the bottom right. Claude's reading of its edges: west about -84.660 (Paul Samuel Road / Summerbrooke),
   south about 33.960 (past the mountain's south end); north and east stay. Code sets the exact bounds from OSM (all of Kennesaw Mountain
   and Little Kennesaw / Pigeon Hill, the airport with its runway ends, a margin) and says what they are. Claude's estimate: about 12.7 x
   9.5 km, about 120 km2: some 4.7 times today's map (25.5 km2), 16 times the original. The USGS 1 m tile in use stops at -84.619 W:
   fetch the next tile(s) west from the same 3DEP project and join them seamlessly.
B. KENNESAW MOUNTAIN: the real terrain at the game's 1.5x (the summit is roughly 250 m above the town around it, so about 375 m in game):
   Kennesaw Mountain Drive up to the summit lot, the park's woods (dense, solid trees), its open fields, the visitor center and its lots as
   generated buildings, the trails as paths. A fast-travel spot at the summit and at the visitor center. No race this round (Dan will
   want to see it first). The airport: the runway, taxiways, aprons and hangars as generated, drivable (no planes needed this round).
C. KEEP IT FAST (do this BEFORE adding the land; Dan noticed no slowdown at 3.3x, but Round 59 measured the old neighborhood falling from
   182 to 122 fps average, mostly from things spread over the whole map that run every frame whatever the distance: people groups,
   hydrants, the traffic network, far objects drawn). Make those distance-based (only what is near the player, or near a racer, runs;
   the rest sleeps / is not drawn; trees and buildings beyond view distance not drawn or as cheap far versions), then measure again,
   then extend. Target after the extension: the old neighborhood and the mountain at least as fast as Round 59's numbers (1080p window).
   If that cannot be held, stop with the land added only as far as it holds and report (Dan decides).
D. BUMPS in stage 1's land: BUG-001 (x 1359.21 z -716.10 h 19, Chastain Meadows Parkway): "fix this bump"; BUG-002 (x 649.95 z -1663.21
   h 263, Barrett Parkway): "smooth out riht here"; BUG-003 (x 583.95 z -1673.97 h 262, Barrett Parkway): "also right here". Smooth them;
   the generated roads in the new land likely have more of these: also run Round 54's lap-levelling idea along stage 1 and 2's main roads
   (Barrett Parkway, Chastain Road, Chastain Meadows, Kennesaw Mountain Drive, US 41) and report the jolts before / after.
Numbers: Build Map time, exe size, load time, memory, frame rate (old neighborhood, Town Center inside, KSU, the airport, the summit, the
mountain's woods), tree and building counts: Round 59 / after C / after A and B.
Check: every race still runs (one AI race each); Town Center inside unchanged; free roam from Chastain Glen to the summit and to the airport
along roads, never under the ground; pictures: the summit looking back east over the map, the mountain road, the airport, the join at
-84.619.
For Dan to check: drive up Kennesaw Mountain and down; the airport and Laura Lake; frame rate everywhere; the three bumps.

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round61/`. Bug reports read: session `2026-10-22_09-43-30-831_9bfdff`, BUG-001 to 003.
C DONE (before the land, `perf_c.txt`; Round 60's scene, two runs each): one vehicle list instead of scene searches, shrub beds / night lamps / people groups / water areas by cell, pipe pieces and streams asleep with no vehicle near, one fence-debris runner and fence meshes copied on first break, far geese wait, terrain tiles past the far clip not drawn (`World/FarTerrain.cs`). After the land: the ground-height lookup searched all 304 tiles per call (now a grid, `GroundQuery.TileAt`) and the police network's log line recounted its largest part per node (749 s at start-up, now 1.1 s).
A DONE. Stage 2 appended after stage 1 as stage 1 after the core (`mapgen_extend.py`, config `extension2`: 9 tiles west, 10 south; three more 3DEP tiles x71y377, x72y376, x71y376, joined). Bounds: south 33.95632, west -84.66198 (north, east kept); 12613 x 10229 m (129 km2, 5x Round 59), x -8629.83 to 3983, z -8311.33 to 1918, 19 x 16 tiles. Every existing list and tree unchanged as a prefix (checked); floors moved about 1 cm (the 16-bit terrain step is 8.8 mm now).
B DONE. Kennesaw Mountain at 1.5x (summit 689.9 m in game), Kennesaw Mountain Drive, the park's woods, the visitor center from OSM; 45 named trails (19.6 km) at their real width on levelled ground (`MapBuilder.Airport.cs`); the airport's 2 runways, 21 taxiways, 3 aprons graded, paved and painted, hangars from OSM; fast travel to the summit and the visitor center. No race.
D DONE (`MapBuilder.Round61.cs`; `round61_bumps.txt`): the new land's Barrett Parkway, Chastain Road, Chastain Meadows, Kennesaw Mountain Drive and Cobb Parkway (US 41) levelled across their width to their blurred centerline. Dan's spots at 25 m/s, before / after: BUG-001 northbound stopped dead / kept 23.7 m/s; BUG-002 and 003 airborne up to 3.9 s and a crash / wheels always down, 24.6 m/s. Sharpest 1 m step along the roads: Barrett 0.94 -> 0.12 m, Cobb Parkway North 0.22 -> 0.05, Chastain Road NW 0.40 -> 0.06 (`round61_build_log.txt`).
Numbers, Round 59 / after C / after A and B (1920 x 1080 window, not measured at 3840 x 2160; PC in use): Build Map 1955 s / (not rebuilt) / 19,026 s (5.3 h: Businesses 5,726 s, Hydrants 2,772 s, Roads 2,059 s); exe data 1357 / 1448 / 4284 MB; world up 19.4 / 9-31 / 33.5 s; Unity memory 1838 / 1838 / 5182 MB; tiles 60 / 60 / 304; trees 142,282 / same / 920,484; buildings 5,535 / same / 34,251. Frame rate average / slowest 1 %: old neighborhood 122 / 39 -> 214 / 72 -> 163 / 71; Town Center 171 / 51 -> 247-278 / 81-126 -> 191 / 97; KSU 154 / 53 -> 226 / 69-73 -> 175 / 81; the airport 196 / 83; the summit 180 / 99; the mountain's woods 174 / 68; the mountain road 191 / 96; the join 192 / 92; inside Town Center 187-207 / 61-100 (the Round 60 exe the same evening 207-217 / 46-68).
Check: free roam Chastain Glen to the summit (12.4 km) and to the airport (7.3 km) along the roads: through, never under the ground (`round61_check_drives.txt`); every race still runs: 11 courses and directions, one AI race each on Hard, traffic Normal, 33 of 33 finish (`round61_check_races.txt`); Town Center inside unchanged, its frame rate above; pictures `61_*.png` (summit, mountain road, mountain woods, airport, the join).
Left for Dan: two stretches still have a 0.5 m step in the bridge-end fades (Chastain Road and Cobb Parkway, not his spots); the full build now takes 5.3 h (a follow-up could make Businesses and Hydrants scale). The summit picture shows the summit only (the fog is solid at 650 m).
`Builds/ChastainRush/` 2026-10-22 19:46 (the exe from the round's one full Build Map scene, 16:44; the runtime fixes after it were code only, the scene's data unchanged). Imported from Racer: nothing.

**Build Map speed-up (after Round 61, Dan's go 2026-10-22; editor code only, no round): full Build Map 19,026 s -> 2,576 s (43 min).**
Cause: every generated mesh's CreateAsset ran an asset refresh over the whole project (48,084 refreshes, 12,389 s); meshes are now imported once per step (`GenAsset` / `FlushAssets`, `MapBuilder.Quick.cs`). The scans over the whole map per item are by cell (`MapBuilder.CellIndex.cs`, candidates in their old order): hydrants, businesses and Canton Road's lots, the per-tree checks, roads' names, the other neighborhoods' houses and fences, Round 52's settling, the chained bridge decks.
Steps, before -> after (s): Businesses 5,726 -> 488; Hydrants 2,772 -> 167; Roads 2,059 -> 521; Rest 1,795 -> 200; Trees 1,439 -> 30; PlanBusinesses 1,279 -> 54; Neighborhood 650 -> 123; Network 593 -> 73; Driveways 588 -> 83; LoadRest 576 -> 7; SmoothBridgeEnds 365 -> 4; the whole planning 2,614 -> 570.
Check: `SceneDump` of the Round 61 scene against the new full build, `scene_diff.py`: 493,613 lines each, 0 terrain blocks differ, 6 objects differ, all from Unity's unsorted `FindObjectsByType` order (same hydrant counts and reasons, same geysers; manholes visited in another order): the hydrant links, one hydrant chunk, and two smash sites. The street-lamp smash site lands at (2960, 896) or (2999, 1072) depending on that order: it already flipped between Round 39's `build_a` and `build_a2` (same code); this build has (2999, 1072). The exe was not rebuilt (scene the same); the next round's build carries it.
Follow-up, same order every build: the street-lamp smash site's FenceChunks, the hydrants' HydrantChunks and ManholeLids are now taken in hierarchy order
(`MapBuilder.InSceneOrder`, `MapBuilder.Activities.cs`), and a tie for the densest lamp row goes to the western panel. Check: two full Build Maps, `SceneDump`
of each, `scene_diff.py`: 0 differences (`local/determinism/`). The lamp site is now always (2960, 896), the one in Dan's Round 61 exe.
For Dan: keep the lamp smash at (2960, 896), or move it to (2999, 1072)? (One line to change; until you say, it stays at (2960, 896).)

## Previous delivery 3: Round 60
**Reviewed: Dan drove the Round 60 build 2026-10-22 09:43: "Everything looks good in the mall"; his notes are Round 61.**
**Round 60, TOWN CENTER AT COBB MADE TO LOOK LIKE ITSELF, DRIVABLE INSIDE ON BOTH LEVELS WITH THINGS TO SMASH; the Chastain Road overpass
over I-75 (Dan, 2026-10-22 05:37; report BUG-001 of session `2026-10-22_05-11-54-864_7498e6`, read by Claude, after the Round 59 exe).**
Rounds 58 and 59 are reviewed ("I didn't notice any frame rate issues"). ONE full Build Map at the end (quick builds while working: the
mall as its own quick-build area). Tuning limit. Short results.
A. THE MALL. Dan: "Can we make town center look like town center. I have a link here with a bunch of pics
   https://rs.locationshub.com/Home/Mosaic?rsLocationId=042-10006196. You can get a good idea what it looks like on the outside and I
   would have to have it generically decorated on the inside but based on reality. It is a two level mall. I would want to be able to
   drive through it and have smashable things throughout."
   PHOTOS: the page is a gallery of about 85 photos (ga.reel-scout.com). Claude could not open them from here (blocked); Code: download
   them into `local/reference_photos/town_center/` and look at them (exterior entrances, anchors, interior courts, food court, levels).
   If Code cannot reach them either, say so in the status and work from what is known; Dan can save some there. Reference only (as the
   Google / NAIP rules): never used as textures, never traced; store and brand names faked (as the Kroger center).
   What is known (malls.com): an enclosed two-level regional mall, about 1.28 million sq ft, between I-75 and I-575; anchors Macy's (and a
   Macy's Men's & Furniture Gallery), Belk, JCPenney (in the old Mervyn's), formerly Sears; a north wing added in 1993; a food court
   (renovated 2008), a children's play area, seating areas. Use the real OSM footprint Round 59 already placed.
   (1) OUTSIDE as the photos show it: the building's shape and height from its footprint, the materials and colors, the entrances (canopies,
       glass doors, entry signs), the anchors' own facades (fake names), parking lots and the ring road as they are.
   (2) INSIDE, generically decorated but based on reality: two levels of concourses along the real wings, center / court atriums open to
       both levels with railings and skylights, escalators, stairs and elevators, storefronts with glass fronts and fake names, kiosks,
       benches, planters, a food court with counters, tables and chairs, the play area. Anchors: a way into at least one anchor's store.
   (3) DRIVE THROUGH IT: in through the entrance doors (breakable glass), along both levels, between levels by ramps a car can take (e.g.
       the escalators / stairs treated as drivable ramps, or a car ramp where a court allows), out through another entrance or an anchor.
       Wide enough for every vehicle; nothing a car wedges in. A drop from the upper level into a court is allowed (big air) where it lands
       cleanly.
   (4) SMASHABLE THROUGHOUT (the shopping center / stunt park's breakables family): store glass, kiosks, benches, planters, signs, trash
       cans, mannequins, food court tables and chairs, displays; breaking them must not stop a car (as fences). Shoppers inside, a few,
       who get out of the way as the game's other people do.
   (5) Lights inside (day and night), sounds (echo inside if simple). Keep the frame rate: the interior drawn only when near / inside.
   Free Roam only this round (no race); a fast-travel spot at the main entrance.
B. BUG-001 (x -1051.61 z 420.74 h 73, Chastain Road on its overpass over I-75): "This needs some smoothing" (Dan: "The overpass over 75 is
   very bumpy"). Smooth the deck, its ends and approaches both ways (and its pair's other carriageway); check at speed, all vehicles.
Check: drive in, through both levels, between levels, out, on all 11 vehicles (no wedge); smash a sample of each breakable without
losing speed; frame rate inside and in the lot (1080p) against Round 59's numbers; the overpass both ways (jolts before / after).
Pictures: the outside from the main entrance and an anchor (next to a photo if Code has them), each level, a court, the food court.
For Dan to check: Town Center inside and out; drive it and smash it; the overpass.

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round60/` (`round60_check.txt`: 113 of 113).
Photos: the locationshub gallery refused Code too (403); Dan's 19 in `local/reference_photos/town_center/` set the look (reference only, not committed, never textures).
A DONE. `MapBuilder.Mall.cs` / `MallInside.cs` / `MallLots.cs` (quick step `Mall`), run-time `World/TownCenterMall.cs`. The real ground has two levels (lower 305.6 m north / west /
   south-west, upper 313.1 m east / south / north-west), so the doors open at grade on both: main (north, lower: the stepped glass tower), south-west (lower), south, north-west wing
   (west), east anchor's east doors (upper). Outside: tan split-face block, curved canopies on stone piers with granite panels and black name boards, barrel-vault skylights and the
   food court's pyramid over the roof, the five anchors' portals (made-up names: Halverson's, Halverson's Home, Belmont, J.C. Carroway; the closed one boarded, "Sterling & Co."
   with letters gone). OSM maps no lots round the mall, so the ground to the ring road is paved (19.9 ha, stall lines where level, 142 light poles). Inside: 104 shops on two
   levels (glass fronts, lit name boards, their things), cream ceilings, tile with diamonds and red bands, the checkerboard court, wood-capped glass railings, columns with mahogany
   capitals, 11 kiosks, benches, planters, the food court (8 counters, 77 tables) upstairs, the play area under it, two escalator banks as 14-degree drivable ramps, the court's
   glass ends break for the 7.5 m drop, and the east anchor's store to drive through and out. 2140 breakables, none costs speed; 34 shopper groups; echo zones; night lights.
   Check: every vehicle (the 11 and the stunt park's 3) x 3 ways in and out (main doors -> east escalators -> store -> east doors; south doors -> the court drop -> south-west doors; main doors -> west escalators ->
   north-west wing -> its doors): 42 of 42 through, never stuck; a sample of every breakable kind at 12 m/s: speed kept (15 of 15); nothing of the inside past the walls (`mall_probe.txt`).
   Frame rate, 1920 x 1080 window (not measured at 3840 x 2160), two runs, the PC in use: inside 117 to 228 fps average, 27 to 70 slowest 1 %; the lot 189 to 205 / 58 to 61
   (Round 59 at Town Center: 171 / 51). The inside is not drawn past 140 m.
B DONE. The decks were straight; the bumps were the ground where both carriageways leave them (rings from overlapping end gradings): both ends blurred and each carriageway laid as
   one curve from its deck (`MapBuilder.Round60.cs`). Sharpest 1 m step along the carriageways: west 0.21 / 0.20 -> 0.02 / 0.015 m, east 0.23 / 0.28 -> 0.013 / 0.016 m. Every vehicle,
   both carriageways, both ways, flat out: never off the wheels, the sharpest jolt 0.09 m/s.
Decisions: the lots added (OSM lacks them; Dan's photos and "parking lots as they are"); columns floor to roof only at the escalator holes' corners (the court's and the long sides'
   upstairs only: cars stopped dead on them); a fast-travel spot moved to the main doors' lot. Two full Build Maps (the first to build the mall on, the round's last as the rule says).
For Dan to check: Town Center inside and out, day and night (the skylights stay bright at night); the drop into the court; the east anchor's store; the overpass both ways.
`Builds/ChastainRush/` 2026-10-22 08:48. Imported from Racer: nothing.

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
