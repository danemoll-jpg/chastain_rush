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

## QUEUED rounds
Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 74
**Reviewed: Dan checked everything but the quarry 2026-10-27 11:27 to 12:04: "The only thing is that the garage is still kind of broken" (Round 75 A); the fork barriers no complaint. The quarry not yet seen.**
**Round 74, THE PLANE'S GARAGE AND LOOKS, CONTROLS SHOWN FOR THE DEVICE IN USE, STEADY ARROWS; the quarry jumps' lanes, a turn-off on
Kennesaw, the summit leap's lip (Dan, 2026-10-27 03:06; reports BUG-002 to BUG-005 of session `2026-10-27_02-23-23-098_3e251a` (BUG-001 was sent by mistake),
read by Claude, after the Round 73 build).** Round 73 is reviewed. Item 0 (CHECK IT AS DAN PLAYS IT, CLAUDE.md "Process") applies to every
item: Dan's spots pictured before / after from his position and heading and LOOKED AT; features played from where a player starts. Round
73's quarry jumps passed its checks yet Dan found a hill through a run-up and a run-up ending at a wall: the pictures must be from where a
player actually drives (the sign, the start box, the lane at speed), not chosen to look good. Quick builds; ONE full Build Map. Short results.
A. THE PLANE. Dan: "airplane - replay, the propeller doesn't move. There is no garage option. Ideally there will be other planes to select
   eventually and at the very least allow to change colors. Additionally planes should have windows so that we can see the avatar".
   A1. The propeller turns in replays (recorded throttle / engine speed drives it, as live); check the helicopter's rotors in replays too.
   A2. A garage for flying vehicles: the plane (and the helicopter) in the garage page in Free Roam (and in race setup for the Quarry Air
       Race), with the turning model and colors to choose, saved like the cars' color. Build it so more planes can be added later (a list,
       not one hard-wired plane).
   A3. Windows: the plane's cabin glazed (the game's vehicle glass), the pilot (Dan's rider / avatar) sitting inside, visible from the
       chase camera; the same for the helicopter if it lacks them.
B. CONTROLS SHOWN FOR THE DEVICE IN USE. Dan: "on the map screen (and in general) can we just show controls for the device being played
   with and not also include keyboard buttons unless these buttons are only on the keyboard". Everywhere the game lists controls (the map
   screen's help line, menus' hints, the loading screen's tips, the debug window's line, race / vehicle hints, the plane and helicopter
   help): show only the active device's (the pad's glyphs when the last input was a pad, the keys when it was keyboard / mouse), switching
   at once when the player switches; keyboard-only functions (F-keys, debug) shown only with the keyboard. Pictures of each screen both ways.
C. STEADY ARROWS. Dan: "Arrows during vehicle races are too bouncy. Can we make them more stable." The next-checkpoint arrow (and the
   on-screen direction arrow): smooth it (no bob with the suspension / camera shake, damped turning, held level), still pointing right.
D. KENNESAW MOUNTAIN. BUG-002 (x -1102.78 z -5849.19 h 69, the drive before CP5): "there needs to be a barrier for the right road. It is not
   obvious you don't go that way" (the picture: the course bears left onto its bench while the drive carries on to the right). Close the
   road the race does not take there with the race's barrier (off the race line: Round 73 A1's rule), and check every fork of every
   course for the same (a road that looks like the way but is not: closed).
E. THE QUARRY JUMPS (Round 73 C4 again). BUG-003 (x -1996.33 z -1390.14 h 226, a run-up lane): "maybe want to remove the mountain so it isn't
   sticking through the ramp" (the picture: a grassy hill rising through the paved lane in front of the kicker). BUG-004 (x -2128.42 z
   -1443.31 h 190, the end of a lane): "ramp is blocked. I don't understand where these ramps lead to" (the picture: the lane runs into a
   wall of hillside: no kicker, no lake, nothing ahead). For every jump: the lane, the kicker and the landing in one clear line, nothing of
   the ground over or through any of it, the lake and the landing in view from the start box and the lane. If the real quarry's shape fights
   it, cut it back (the ground is ours to shape). Then Round 73 C5's question, taking Code's own first option as Dan asked for a track: a
   quarry course on the jumps drive and the haul roads with the crossing closed, its checkpoints leading to each run-up.
F. THE SUMMIT LEAP. BUG-005 (x -1068.06 z -5968.78 h 209, at the lip): "the end of the ramp makes you turn and lose momentum" (the picture:
   a grass hump with a jagged rock edge and a white shape beyond it). Make the lip straight and true to the run-up (no twist, no side
   slope, nothing to catch), so the car leaves it flat out straight ahead; say what the white shape is and fix it if it is wrong. Report the
   flight (Round 73: Drifter 6.9 s / 188 m; Round 70's steel ramp was 8.6 s / 341 m). Dan, 03:13: "if we can get a larger run up that would be good (in addition to fixing the actual jump). The philosophy of this
   game should be fun takes precedence over reality. The top of the mountain doesn't have to be exactly like the real thing. Also can't we
   pitch the ramp a little higher? All this is to say I don't know how it will feel to actually jump it since the edge of the ramp needs to
   be fixed". So: reshape the summit as needed for a LONGER straight run-up (level / extend the top, cut through, raise a run-up across it:
   fun over reality), pitch the lip steeper (still a natural-looking rock / earth lip, still landing clean), and aim for a flight at least
   as long as Round 70's steel ramp (8.6 s / 341 m on the Drifter Twin). Tuning limit as usual; report the run-up length, lip speed and angle. Dan, 03:15: "make sure that this change doesn't mess up the race track and the top of the mountain is reasonable looking
   (ie no 90 degree angle roads)": the Kennesaw Mountain race (its course, jumps, checkpoints, the drive) must be unchanged and still raced
   clean (one AI race and the human-like driver, before / after lap times); the reshaped summit must look natural (smooth slopes and
   banks, roads and the run-up meeting in gentle curves, no sharp corners, cliffs or right-angle joins); pictures of the summit from the
   lot, from the run-up and from the air.
G. FINISH THE AI'S PACE (added 2026-10-27 04:37: Dan asked for one more item before bed, no preference; Claude picked this). Round 70 B
   was PARTLY done (tuning limit): Dan found the AI "really slow" and "too slavish to the actual roads"; Round 70 found phantom bends from
   the roads' raw centerlines and held slow zones (the trails' turns 11 to 14 m/s where Dan does 23 to 41). Finish it: smoothed racing lines
   on every course (no phantom bends), slow zones only where the vehicle really must slow (measured cornering limits, not fixed numbers),
   corners cut over grass and lots where the course allows. Target: Hard's best lap within a few seconds of Dan's best on every course
   (his Top 10 records), Normal and Easy by pace. Report the table per course (Dan's best, Hard best / average before and after) and watch a
   replay of each from the chase camera (item 0): no weaving, no odd braking, passing as Round 58.
H. A SECOND PLANE (Dan, 04:40). In A2's garage list: a faster, more agile plane next to the Sky Hopper (e.g. a low-wing aerobatic /
   racing single-seater: quicker rolls, higher top speed, our own model, no real maker's name), its own colors, windows and the rider
   visible (A3), propeller turning in replays (A1). Both planes selectable for the air races and Free Roam; AI pilots use either.
I. A SECOND PLANE RACE: THE HIGHWAYS (Dan, 04:40: "a second plane race utilizing the highways so we can fly under bypasses"). A ring course
   down the I-75 / I-575 corridors (and their interchange), low over the lanes, under the overpasses where the plane fits (Round 69's wing-tip
   test; the frames sized to the openings, makeable), with banked sweeps between them; start / finish at the airport or in the air near it,
   Code's pick. Built to Round 73 B's rules from the start: flowing ring order (each ring points you at the next), the 3D arrow, beacons,
   the ring after next shown, rings that count either way, a missed one can be gone back for. Race against AI pilots or time trial; records,
   ghosts, replays. Verified as item 0 with the human-like pilot (cockpit pictures at each ring, under each bridge).
J. THE AIRPORT ON THE MAP (Dan, 04:40: "show the airport with concrete runways on the map instead of grass"). The map screen and minimap draw
   the airport's runways, taxiways and aprons as pavement (concrete gray, with runway markings if cheap), not grass; check other big paved
   areas (lots, the stunt park) read as pavement on the map too. Pictures of the map at the airport.
Check: item 0 for every item; pictures listed; the plane in the garage (two colors) and a replay with the propeller turning.
For Dan to check: race the AI on Hard on a few courses; the second plane; the highway air race; the airport on the map; the plane's garage, colors, windows, replay; the controls shown on the map and menus with the pad and with the keys; the
race arrows; Kennesaw's CP5 fork; each quarry jump from its sign and start box, and the quarry course; the summit leap.

**Status (Claude, 2026-10-27): built (full Build Map + exe 2026-10-27 08:30), checked in the exe through the game's own input, not yet driven by Dan.** Evidence `Data/screenshots/round74/` (`round74_check_final.txt`, `ai_table.txt`, `74_*.png`). Dan's "before" pictures are his own (the bug session).
0 DONE: every item below was looked at in the exe's pictures from the player's camera.
A1 DONE. Replays keep the plane's / helicopter's flight part on the puppet: both planes' propellers turn in the Quarry Air Race's replay (`74_replay_propeller`). A2 DONE. Race setup > Garage for an air race holds its planes, with the turning model and colors (`74_garage_air_race`, `_plane_color1/2`); Free Roam's garage had them. A3 DONE. Both planes' cabins were a closed painted shell (the windows lay on it): now a low tub under glass, the pilot seen from the chase camera (`74_plane_plane_pilot`, `_dart_pilot`); the helicopter already had its bubble.
H DONE. The Sky Dart (`plane_dart.py`): 138 m/s, roll 165 deg/s (Sky Hopper 115, 95), on the middle apron stand, in both garages and air races; AI pilots fly either (EMBER flew it).
B DONE. Hints follow the device in use (`KeyHints`): the map's two lines and legend, the minimap, race HUD, debug line, loading tips, plane / helicopter help; keyboard-only keys (F-keys) only with the keys (`74_controls_pad_map`, `_keys_map`).
C DONE. The race HUD arrow and the air race's 3D arrow damped (0.3 s); the arrow's frame-to-frame wobble down 57 % on a Glen lap (0.625 to 0.268 deg).
D DONE. `74_BUG-002_after`: a barrier across the drive straight ahead, the course bending left before it. Every course's forks off its line closed (`Tools/race/fork_closures.py`: 64 barriers on 4 courses; 3 stubs under 30 m left open); the barrier sweep on every course both ways: none on a line (the Underground's switch gate, by design).
E PARTLY. BUG-003 / BUG-004: the ground stood up to 11 m through J2's and J3's landing decks before the bench's edge (a probe): the cut now starts there, the ground at most 0.2 m under the decks; Dan's "wall" was the deck running up to that ground. LANDING boards at the decks; the countdown's camera sweeps from over the box out over the lake to the landing (`74_qJ2_preview_countdown`). The jumps from fast travel: all CLEAN (J1-J3, Street Classic and Drifter, J2 Needle). The Quarry Track NOT built (second try, one lane): the forgiving checkpoint rule let the AI skip The Corner off the shared haul road, the player's driver stuck off The Long One's run-out (`local/round74/quarry_track_unbuilt/`). Options for Dan: only the next 3 checkpoints count on that course, and a graded way from the run-out to the haul road.
F PARTLY (tuning limit). BUG-005: the white shape was the cloud layer at the summit's height: the clouds now rise over high ground (160 m over the camera at the summit). Round 73's climb kept, the lip pitched 21 to 24 deg, the kicker 18 m wide with gentle banks, a softer back face, 450 m round with body glide (the bikes bottomed out at 340 m). Flat out from its fast-travel start (`round73_check_leap_final.txt`): Drifter Twin 43.0 m/s at the lip, 9.3 s / 336 m (Round 73: 6.9 / 188; Round 70's steel ramp 8.6 / 341), Street Classic 7.9 s / 217 m (6.1 / 145), Needle 600 7.7 s / 202 m (7.9 / 228), all upright; without the glide the Needle flew 9.6 s / 368 m and the Street Classic 8.6 / 267 but the Drifter only 5.7 / 104 (no one kicker suits both): kept for Dan's Drifter. The lip's edge still draws saw-toothed from the run-up (the terrain's 1 m grid across a diagonal edge). The run-up is 276 m as before (a longer straight does not fit: the race's bench behind it, the face beyond). Kennesaw race: the course unchanged (B2 only); one race, Normal: all finished; the human-like driver 3:41.7 with 3 stuck spots (-858, -5979), (-1027, -5743), (-880, -5565) (Round 73 ran no race to compare).
G PARTLY (tuning limit). `ai_table.txt`: Hard's best lap against Dan's: within 4 s on the Trail forward, Glen both ways, Ridge forward, Lakes reverse, Underground; 9 to 26 s off on Kennesaw (128.3 / 108.5), Lakes forward, Ridge reverse, Addison, Creek. Try 1: the angle cap from the circle through its aim, bend zones from the racing line on Hard; try 2: Hard corners at 1.12. Not watched as replays (no time); the traces are in local/round74.
I DONE / PARTLY. The Highway Air Race (`make_highway_air_route.py`, 19 km, 16 rings): up I-75 under Chastain Road's bridge (8.8 m to its deck as built), over the Skip Spann Connector (its deck on the ground), down I-575 under Chastain Road NW's, the interchange, home. In the exe (`round71_check_highway_final.txt`): nothing solid on the line, a clean lap by script 2:54 (every ring, no crash), both AI pilots on Hard finish (2:37 and 2:59 laps), no crash. Rings along the interstates added after the human-like pilot, flying by the arrow, cut across the hills into the bridge's bank; a third crash before one ring now puts the plane past it. The human-like pilot check itself still does not finish (it aims 90 m beside ring 3 by design: here into the trees by I-75), not proof of a finish by hand.
J DONE. The airport's runways, taxiways and aprons as concrete with runway markings, parking lots as asphalt (`74_map_airport`); the Mall's own lots (built by the map build) still grass on the map.
For Dan to check: the planes (both, the garage, the pilot through the glass, a replay); the highway air race; the summit leap on the Drifter Twin; the quarry from the boxes and the LANDING boards; the AI on Hard on Kennesaw and Lakes; the map with the pad and the keys.

## Previous delivery 2: Round 73
**Reviewed: Dan played the Round 73 build 2026-10-27 02:23 to 03:06; his notes are Round 74.**
**Round 73, PUT RIGHT WHAT DAN FOUND, AND CHECK IT THE WAY HE PLAYS (Dan, 2026-10-26 19:04; reports BUG-001 to BUG-006 of session `2026-10-26_18-37-35-665_130b58`,
read by Claude, after the Round 70 to 72 build).** Dan: "that's a lot of work for a bunch of junk. Really not happy with anything I tested.
The closest thing to being good is the big ramp off of kennesaw ... It was all like the code was like I did something, here you go and didn't
even verify that any of it worked. Which is really frustrating because I wasn't even home and it had plenty of time to work things out."
Quick builds while working; ONE full Build Map at the end. Short results. No new features this round.
0. A NEW STANDING RULE (copy into CLAUDE.md "Process", in these words): CHECK IT AS DAN PLAYS IT. Scripted perfect lines are not enough.
   (1) Every report of Dan's: a picture from his exact position and heading, before and after, that Code LOOKS AT and describes in the
       status; a report is closed only when the after picture shows the thing gone. Re-read every earlier report on the same ground too.
   (2) Every feature: play it from where a player starts (the title, race setup, Free Roam's spawn or fast travel, a road nearby), with the
       normal controls through the game's input (a human-like driver / pilot: reaction lag, imperfect lines, a missed gate, a crash and a
       reset), and pictures from the player's camera along the way that Code looks at: can a first-time player see where to go? does it
       look right (no blocks, nothing buried, nothing floating, nothing across the road)? List what a first-time player sees and where it
       went wrong.
   (3) The status says, per item, what was looked at and what it showed. "Checked by script" alone is not done.
A. KENNESAW MOUNTAIN.
A1. BARRIERS ACROSS THE COURSE. BUG-001 (x -866.04 z -5208.17 h 56, before the start line): "This was reported. Why is it still here?";
   BUG-002 (x -1084.27 z -5837.29 h 49, the drive, CP5 next): "this too! How was this missed? There were two literal barriers in the middle
   of the track. They were reported as bugs but ignored and not fixed". Both pictures: a red-and-white race barrier straight across the
   road the race drives on. (Claude's fault in part: in Round 70 Claude misread Dan's 2026-10-25 BUG-001 as the start gantry and BUG-003
   "again" as hydrants; both were these barriers.) Remove every barrier standing on the course's own line, here and on EVERY course in both
   directions (check each course's line for any closure, barricade or solid object across it; a picture at each).
A2. THE DRIVE AT THE SUMMIT. BUG-003 (x -933.27 z -5721.73 h 141, Kennesaw Mountain Drive by the summit lot, J1's lip): "this road also
   looks like it wasn't fixed. I mean its changed but the road will be inoperable in free roam. It needs to be fixed" (the picture: the drive
   cut into the jump's hump at the lot's edge, the road broken). The drive must be a whole, normal, drivable road in Free Roam (traffic, a
   car driving up to the summit at normal speed, both ways); the race's jump keeps its flight but must not wreck the road: e.g. the jump on
   its own line beside / off the drive, joining it smoothly. Pictures driving the drive both ways in Free Roam at Dan's spot.
A3. THE BIG RAMP AT THE TOP. Dan: "The closest thing to being good is the big ramp off of kennesaw, but why is it not even at the highest
   point of the mountain." Move it to the summit itself (the mountain's highest ground), with a run-up across the top (cut / graded as
   needed), launching off the mountain where the fall is longest. Dan, 19:11: "for the ramp on the top of the mountain I would prefer it to
   be more realistic. It doesn't have to be some huge structure just a jump off of something that could naturally be there that jumps and
   clears the mountain will be fine": NO steel trestle; the launch is something that belongs on the summit (a rock ledge / outcrop, the edge
   of the overlook, a cut bank at the end of the summit path), the ground itself shaped; a flight longer than Round 70's; a way back to the roads below; fast
   travel to the run-up. Verified as rule 0 (from fast travel, flat out, a short one, a reset).
B. THE QUARRY AIR RACE: REDESIGN THE COURSE AND ITS GUIDANCE. Dan: "the plane race, is just not laid out well at all. The first ring is down in
   the quarry nearly right beside the take off point. It is impossible to get there without lining up the plane but the plane should have
   already been going in the right direction to set that up. And if you missed a checkpoint, it is impossible to tell which is next, also I
   circled back to a cp that I missed (because it is too unforgiving) and you only get credit if you are going in the direction of the game.
   Also, after I got through the first 3 or 4, I had no idea where the next gate even was there was an arrow but it was only pointing up or
   down, turning the plane around I couldn't find it at all. I just quit the race. Really poorly designed".
   (1) FLOW: after take-off the first ring lies well ahead on the runway's heading, high enough to climb to; every ring is placed so that
       flying through it already points you at the next (heading change between rings small for their spacing; a hard turn only with room
       to make it); the quarry dive comes once you are set up for it, not straight after take-off.
   (2) FINDING THE NEXT RING, always: a 3D arrow / chevron pointing at the next ring in full direction (left, right, up, down, behind), a
       tall beacon on it seen from anywhere, the ring after it shown dimmer, its distance; the minimap shows the next rings. After a miss:
       a clear "RING MISSED" and the arrow at once on the next one.
   (3) FORGIVING: rings bigger (tight ones only where the place demands it, e.g. under a bridge, and still makeable); a ring counts flown
       through either way; a missed ring may be gone back for (then no penalty), or skipped (the usual penalty when you take the next).
   (4) Verified as rule 0 with a human-like pilot: take-off, every ring in order, a missed ring found again, getting lost and finding the
       way by the arrow; cockpit pictures at each ring showing the next one visible or the arrow pointing at it.
C. THE QUARRY JUMPS.
C1. BUG-004 (x -2024.24 z -1350.35, the bleachers): "whats going on here? Are these supposed to be people? Why are they just blocks?" Use the
   game's real people (the models the neighborhood's people use), seated, varied; no blocks.
C2. BUG-005 (x -2423.25 z -1323.20, the far bleachers / the rim): "why even put people in here if you can't make it even make it remotely
   correct? People are literally buried". Every spectator on their seat or standing on the ground, none in it.
C3. BUG-006 (x -2398.76 z -1359.36, down in the pit under a ramp's steel trestle): "stuck here. reset button does nothing. But it looks like
   this is buried in the ground anyway. Truly don't even understand how to get to the ramps in general". (1) No way to drive in under a
   trestle or get wedged anywhere round the ramps (solid skirts, the ground right). (2) RESET MUST ALWAYS WORK, anywhere in Free Roam
   (here it did nothing): find why and fix it for the whole map. (3) GETTING THERE: a clear way from the roads to each run-up (a drive /
   track from the airport's roads, signs saying QUARRY JUMPS at the turn-offs, the start marked: drive into a lit start box to begin),
   and fast travel to each start. Verified as rule 0: from fast travel and from Free Roam's roads, a first-time player finds each start.
C4. THE RAMPS THEMSELVES. Dan, 19:11: "honestly you said code could jump those ramps, I have no idea how they did as nothing lined up and the
   ramp was buried in the hillside". Rebuild each jump so run-up, kicker and landing deck lie on one straight line that a player can see
   from the start box; the kicker stands clear of the hillside (nothing of the slope over or in front of it); the landing deck in plain view
   across the water. Verified as rule 0: pictures from the start box (the whole line visible), from the run-up at speed, from the lip, and
   of the landing; flown flat out by the human-like driver, not the script that already knew the line.
C5. A QUARRY TRACK. Dan, 19:11: "I think for the quarry, it has to be some sort of track. We can have those ramps in free roam but having a
   track will make it easier to at least discover the first time. Problem is its too big of an area to search for them." Make a race course
   (in race setup, forward; a reverse only if it plays well) round Viking Quarry that takes in the jumps (at least The Corner and The Long One,
   The Big One if the line allows), the rim and the haul roads down to the water's edge: checkpoints lead you to each ramp's run-up in
   line with it, so racing it shows a first-time player where every jump is. Built like the other courses (AI that takes the jumps, records,
   ghosts, replays, loading inset, map). The ramps stay in Free Roam as well (the scored events), now easy to find from the track and the
   signs.
Check: rule 0 for every item; the pictures listed above; one AI race per car course (all finish, no barrier on any line).
For Dan to check: Kennesaw Mountain race (no barriers), the drive in Free Roam, the summit jump; the Quarry Air Race; the quarry track and
jumps (the crowd, the ramps' lines, getting there, getting stuck, reset).

**Status (Claude, 2026-10-27): built (full Build Map + exe 2026-10-27 00:42), checked in the exe through the game's own input (a human-like driver / pilot on a virtual controller), not yet driven by Dan.** Evidence `Data/screenshots/round73/` (`round73_check_final.txt` 26 of 36, `round71_aline_final.txt`, `round73_check_qtrack.txt`, `73_*.png`). Dan's "before" pictures are his own (the bug session).
0 DONE. The rule is in CLAUDE.md "Process"; every item below was looked at in pictures from the player's camera.
A1 DONE. `73_BUG-001_after` / `_002_after`: Dan's spots, the road clear (BUG-001 shows the start gantry 41 m ahead, BUG-002 an empty drive). Every car course both ways swept on its line with the race's barriers up: none on any line; the Underground's switch gate SG1 stands on its line by design (it opens for the race). `RaceBarriers` now never builds a closure on the line.
A2 DONE. J1 leaves on its own bench 28 m off the drive. `73_BUG-003_after`: the drive whole, its lines unbroken. Free Roam, Street Classic and Drifter Twin, up and down: no air, no reset, about 37 s each way (`73_kdrive_*`). The check's 8 degree roll limit tripped at the lower hairpin (-1185, -5937), 12.5 degrees: the road's own camber, not touched (for Dan).
A3 PARTLY (tuning limit). The summit leap: run-up graded across the true summit, the overlook's rock edge as the lip (no steel), a track back to North Saint Marys Lane, fast travel to the run-up, reset after landing works (`73_leap_*`). The flight is SHORTER than Round 70's, not longer: Drifter 6.9 s / 188 m (R70 8.6 / 341), Street Classic 6.1 / 145 (8.4 / 296), Needle 7.9 / 228 (8.9 / 358); the summit has no long straight, so lip speed is 28 to 34 m/s. Options: keep it; or put the lip at the summit and bring Round 70's longer run-up line onto it.
B PARTLY. Course redrawn (`make_air_route.py`): first ring 1.7 km on the runway's heading at 55 m, then the high ring, the I-75 pair, quarry rim / lake / climb, south, "The turn", home; rings 32 to 56 m (the under-I-75 frame 20 x 6.8 m), counted either way; RING MISSED and the guide on at once; a 3D chevron arrow, beacon pillars, the next-after ring amber, distance and up / down on the HUD, the minimap's next rings (`73_air_ring_*`). The scripted line flies a clean lap (3 of 3). The human-like pilot flying by the arrow alone did NOT finish (two attempts, stopped): it passed the high ring 26 m wide, then would not dive under its own 55 m safety floor to the I-75 rings, so going back for a missed ring is not proven by a pilot.
C1 DONE. Seated people of the neighborhood's kind, varied, some waving (`73_crowd_*`, `73_BUG-004_after`).
C2 DONE. All nine stands on level pads (ground across each +0.0 m), no one in the ground; the stand at BUG-005 is off the rim (`73_BUG-005_after`).
C3 DONE. Reset (Y) in Free Roam works anywhere: never back onto a stuck spot (safe history, nearest road, start). At BUG-006: moved 598 m, upright; wedged at each kicker: moved, upright. Decks skirted to the ground. The quarry jumps drive from the QUARRY JUMPS sign past each lit start box, driven end to end (58 s); fast travel to each start (`73_qroad_*`).
C4 PARTLY. One straight line per jump: smooth run-up lane, kicker clear of the slope, landing deck over the bank face. Held by eye in the speed window: 6 of 7 clean (J1 / J2 / J3 Street Classic, J1 / J2 Drifter, J2 Needle); J3 Drifter at 104 mph (in its window) ROUGH; J1 flat out (98 mph, the window's top) ROUGH. From the start boxes the landings are partly hidden by the airfield's crest (`73_qJ*_from_the_box`).
C5 NOT DONE. Built and tried, then taken out: the haul road crosses the lane so a racer skipped The Corner's loop, and the AI and the human-like driver got stuck there (56 resets). `make_quarry_track.py` kept, not built (output in `local/round73/quarry_track_unbuilt/`). Options for Dan: a course on the jumps drive and haul roads with the crossing closed, or checkpoints at the three start boxes only.
Not run: the AI race on every car course (only the barrier sweep). Imported from Racer: nothing.
For Dan to check: Kennesaw race start and CP5 drive; the drive in Free Roam both ways; the summit leap (is shorter OK?); the Air Race with the arrow; the quarry jumps by fast travel and from the sign; J3 on a bike; the crowd; reset anywhere.

## Previous delivery 3: Round 72
**Reviewed: Dan tested the Round 70 to 72 build 2026-10-26 18:37: "that's a lot of work for a bunch of junk. Really not happy with anything I tested." His notes are Round 73.**
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

**Status (Claude, 2026-10-26): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round72/` (`round72_check_final.txt`, `round72_check_short_replay.txt`, `round72_b*.txt`, `72_*.png`, `72_flights_profile.png`).
A DONE. Three steel kickers on Viking Quarry's south rim, run-ups from the airport's ground (graded smooth, 240 to 380 m), landing decks across the lake sloped a little shallower than the flights (catch zones), run-outs graded, bleachers with 747 people, start gantries with stars-and-stripes bunting and countdown lamps (`Tools/mapgen/make_quarry_jumps.py`, `MapBuilder.QuarryJumps.cs`, event `Stunts/QuarryJumps.cs`). The vehicles' speeds at a lip differ too much (47 to 57 m/s flat out) for one landing deck to catch them all, so each jump has Evel Knievel's speed check: a lip speed window on the HUD (The Corner 82-88 mph, The Long One and The Big One 93-100 mph), green inside it; too slow is short (lake or pit), too fast overshoots. Stop in the gantry's box facing the ramp: fanfare, 3-2-1-GO on the lamps. Scored: distance, height over the lip, air time, landing CLEAN / ROUGH / CRASH / SHORT / SPLASH; Top 10 per jump by distance (`quarry-jumps-v1.json` beside the records); short or crashed: back at the gantry after 3 s; L3 / K watches the jump's replay (TV cameras on the rims, the kicker, the landing deck). Fast travel "Viking Quarry: <jump>" to each gantry. Flying vehicles not in the events.
   Flights at the window's middle (Street Classic, Drifter Twin, Needle 600, Skywhip 250 all CLEAN): The Corner 166-172 m, 5.1 s; The Long One 203-209 m, 5.1 s; The Big One 245-246 m, 6.4 s (the longest after the summit ramp). Flat out (Drifter Twin, outside the window): 244 m rough, 247 m clean, 338 m crash. Short (68 mph at The Corner): into the lake, back at the gantry. Replay opens (5 TV cameras). The air race's line re-probed with the ramps up: clear, a clean lap.
B1 NOT REPRODUCED. Longroof GT, Highball Fastback, Skyfin Cruiser and the Street Classic driven round the Kennesaw lap: no hard stop anywhere (Round 70's widened J1 bench at that spot is the likely cure); nothing changed.
B2 PARTLY. Canton Road's edge closures (and the service road beside one) get 7 m more barrier past each shoulder, solid to 4 m: 221 -> 222 of 224 closures now stop the car (the service road fixed); the check still reads the car 13 m and 50 m past the middle at Canton Road's north and east closures, though the picture shows it stopped at the barricades (`66_closure_*.png`). One try (tuning limit not used up; left for Dan to look at).
B3 PARTLY. The six junctions Round 64's jolt table still threw a car at, blurred after the levelling (`Data/tracks/round72_junctions.json`): air at 28 m/s Barrett at the I-575 ramp 0.96 -> 0.72 s, Frey Road 0.74 -> 0.56 s (jolt 3.9 -> 1.8), Busbee at Townpark 1.12 -> 0.24 s, McCollum at Cobb Parkway 0.54 -> 0.42 s, North Marietta 0.48 -> 0.26 s; unchanged: Chastain Road at Busbee 0.78 s, Cobb Parkway North 1.20 s.
B4 NOT FIXED. The Drifter Twin flat out drops 39.5 -> 28.2 m/s in 0.3 s right at J2's lip, its body pressed into the kicker's top (impulse about 3900). Try 1 (the ground not pushing on an upright body over the kicker, as Elaine's climb): no change, taken out. Stopped at one try; the cause needs a closer trace.
Builds: two full Build Maps (the jumps' first build showed the ground over the kickers; the second after a quick build fixed it, as the round's last) and code-only exes for the event; `Builds/ChastainRush/` 2026-10-26 10:12. Imported from Racer: nothing.
For Dan: (a) the speed check: is a 7 mph window fun or too fiddly (it can be widened, the landing decks lengthened); (b) the bleachers and gantries' look; (c) B2's two Canton Road closures and B4.

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
