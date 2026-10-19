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
**Round 49, REVERSE CHASTAIN LAKES SHORTCUTS, THE LAKE TRAIL WHERE IT REALLY IS, and the map's pointer (Dan, 2026-10-19 08:37,
after driving the Round 48 build; his two in-game map pictures: `local/reference_photos/chastain_lakes/r49_rev_shortcut1_cp7_cp9_map.png` and
`r49_rev_shortcut2_cp4_cp5_map.png`).** Rounds 47 and 48 are reviewed. The
standing shortcut rules apply (a real risk; a hint at the turn-off; ramps jump by physics only; pipes through water are aquarium
pipes). One full Build Map at the end; tuning limit; short results.
A. REVERSE SHORTCUT 1, THE INTAKE. Dan: "Lets do a similar idea as the gazebo shortcut on the forward race. On the bit of land that
   sticks out before the CP, jump into an intake with into a tunnel across the lake and you come out of a manhole just after the 9
   cp in the correct direction". His picture: reverse Lakeside Drive westbound along the south side of West Chastain Lake toward
   CP7; just before CP7 (east of it) a point of land sticks north into the lake. Off the lap onto that point (a hint at the
   turn-off), off its tip into a lake INTAKE (a structure that could be there: an intake / overflow mouth in the water, its look
   Code's choice, not a copy of the gazebo's drain), down into a tunnel north across the lake (glass aquarium wherever it is under
   the water: standing rule), up and out of a manhole that pops on North Lakeside Drive just past CP9, facing east (the reverse
   direction), back on the lap. Forgives the checkpoints it skips. Risk: miss the intake and you are in the lake (slowed, never
   trapped, a reset on the race's line). As the forward drain, the drop into it may be scripted (it is not a ramp); getting into
   its mouth is the skill. Reverse races only (closed in the forward race as the forward ones are in reverse; open in Free Roam).
B. REVERSE SHORTCUT 2, THE BIG JUMP PAST CP5. Dan: "around CP 4, you go between houses and find a ramp that jumps past 5. It should
   be a fairly large jump, I would prefer something other than a dirt ramp but i don't know". His picture: reverse, the lap comes
   south through CP4, curves round the bottom of a wooded hollow and runs back northwest through CP5. Off the lap near CP4 between
   two houses on the hollow's side (a hint at the turn-off), to a launch that sends you over the hollow and lands on the lap past
   CP5, heading the race's way; forgives CP5. A REAL physics jump (ramp rule), fairly large. NOT a dirt pile: something that could
   be in a back yard and is truly that size (CLAUDE.md lesson on ramp sizes); Code picks and says why, for example the neighborhood
   kids' big wooden skate / BMX kicker, a car hauler with its bed tilted down, or a half-built deck or roof on a house being
   worked on. Risk: come in slow or crooked and you drop into the hollow (trees, the slope; a slow way back up, never trapped).
C. THE LAKE TRAIL WHERE IT REALLY IS (both directions). Dan: "Code complained about the path not working for some vehicles. Part of
   the problem is the path is on the map incorrectly. If you refer to my pics and the map, the trail goes behind the pool and twists
   around to the bridge, not between the pool area and the tennis courts. I drove the correct route where the path should be and
   had no problems". Move the trail's piece from the lot to the footbridge: from the lot, BEHIND the pool (on its lake side, past
   the pool's granite retaining wall, iron fence and the bathhouse: photo 2), then twisting down along the shore to the footbridge
   (photo 3, gravel with tan brick edging); `local/reference_photos/chastain_lakes/r44_2*` and `r44_3*` and Dan's route map
   `r44_route_map.jpg`. The piece between the pool area and the tennis courts goes (grass again; the courts' fence untouched). Both
   races' lines, checkpoints, AI zones and barriers follow it; the gazebo drain and the footbridge stay. This also replaces
   Round 47's NOT MET item (the S-bend by the courts). Keep its width (Round 44: 7 m).
D. THE MAP: A POINTER, AND PAN TO EVERY EDGE. Dan: "The way the map is you can't move it all the way to select any area on the map,
   there needs to be two different controls, 1 to move the map around and a pointer to be able to select things on the map".
   Controller: the left stick pans the map, and it can bring EVERY part of the map (all four edges and corners) under the
   pointer; the RIGHT stick moves a pointer freely over the screen, and A / the action buttons act on what is under the pointer
   (a discovered place to travel to, an egg area, a waypoint where it points), not on the screen's center. The pointer stays
   where it is when you pan. Keyboard / mouse: WASD / arrows pan, the mouse is the pointer (click to select, drag to pan). Whatever
   the right stick's click did on the map moves to another button and the on-screen controls line says so. Check: from each corner
   of the map, select a place there and travel; a waypoint set with the pointer off-center.
Check (all): reverse lap with each shortcut (Street Classic, Drifter Twin), time saved; the intake missed (into the lake) and the
jump short (into the hollow); the AI takes both and sometimes fails; the trail in both directions on all 11 vehicles at race speed
with no stops; forward race unchanged otherwise. Pictures: each turn-off and hint, the intake, the aquarium run, the manhole past
CP9, the launch and landing past CP5, the trail behind the pool from photo 2's spot.
For Dan to check: both reverse shortcuts; the trail behind the pool both ways; the map's pointer.

## QUEUED rounds
Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- STARTUP POLISH (2026-10-18): Racer's spoken title, loading screen and hints; Dan: "i Would want 5 but we don't have a cover yet".

## Previous delivery 1 (newest): Round 48
**Reviewed: Dan drove the Round 48 build 2026-10-19 (reverse Chastain Lakes, the map); his notes are Round 49.**
**Round 48, GOOSE EGG HUNT AND EXPLORATION (Dan, 2026-10-18 16:25; started after Round 47).** Dan: "lets go with goose eggs. We can
have 100 of them spread around with tier unlocks instead of just one unlock. We can worry about what exactly gets unlocked
later".
Dan, 2026-10-19 01:43: he wants ALL of what Claude listed from Woodstock Rush, not only the eggs: "Exploration collectibles.
Woodstock Rush had an acorn hunt with a map that fills in as you explore, discovered destinations, fast travel and waypoints ... I
wanted everything else mentioned here ... Remember I want 100 and different amount of eggs will unlock different things which I
haven't decided that part exactly yet." So the map fog, discovered destinations, fast travel and waypoints below are required, not optional.
Port Racer's exploration (`ExplorationCollection.cs`, `AcornAreas.cs`, `AcornNotices.cs`, and the WorldMap parts Round 20
left behind: exploration fog and its save, destinations and travel, waypoints, the collectible areas), with goose eggs in place
of acorns. 100 eggs (a few in a straw nest, or one alone; the look Code's choice) across the whole map: every neighborhood, the
shopping center and stunt park (roofs included), the creek trail, along shortcuts, and in the pipe network; a mix of easy ones
by the road and hard ones that take a jump or a find. Free Roam only (hidden in races). Found by driving through; the count shows
for a few seconds when one is found (as Racer 0.79), the total in the pause menu and on the map by area (not exact spots).
TIERS instead of Racer's one reward: 10, 25, 50, 75 and 100 eggs (Claude's proposal; Dan may change them); reaching one shows a
notice and is saved; what each unlocks is decided later (nothing locked or unlocked by them yet). A restart of the hunt as in
Racer (other saves kept). Check: a pick-up, a tier notice (test save), the map's areas, a travel; pictures of a few eggs, easy
and hard.

**Status (Claude, 2026-10-19): built and checked in the exe by script (14 of 14), not yet driven by Dan.** Evidence `Data/screenshots/round48/`.
- 100 goose eggs (three in a straw nest) in 11 areas, 52 easy (by a road) and 48 hard: 12 in pipes (5 in the glass under ponds and lakes, the hub), 8 in the air over jumps
  (Noonday Creek's, the Ridge leap, the boat jump), 5 on roofs (the stunt park's market, bank and shop row, the corner's garages and warehouse), 7 on shortcut lines, the
  crypt, the dock, the drain's glass, back yards. `python Tools/mapgen/make_eggs.py [--plot]` writes `Data/explore/eggs.json` (eggs, areas, tiers, 35 places); Build Map's
  game layer (`MapBuilder.Eggs.cs`); the game: `Explore/EggHunt.cs` (Racer's ExplorationCollection), `EggAreas`, `EggBanner`. Free Roam only (no race, stunt event,
  police or split screen). Saves `goose-eggs-v1.json` and `exploration-map-v1.json` beside the race records (a check run's own folder).
- Found by driving through: the banner "GOOSE EGG FOUND 1 / 100" with its area and count for 4 s. TIERS 10, 25, 50, 75, 100: "TIER n REACHED" for 7 s with a fanfare,
  saved; nothing locked or unlocked by them yet. Pause menu: the count, a GOOSE EGGS page (tiers, each area with where it is, SHOW ON THE MAP, RESTART THE HUNT: tiers,
  the map and all other saves kept).
- The map (`Map/WorldMap.Explore.cs`, `Explore/Exploration.cs`, `WaypointGuide.cs`): fog lifting where you have driven (20 m cells, saved); the egg areas outlined with
  counts and a list (D-pad up / down); places discovered within 45 m, LB / RB to choose, X to travel (A confirms, Free Roam only); Y sets a waypoint (a beam and
  "WAYPOINT 392 ft" on the HUD; cleared on arrival).
- Check: a pickup by driving, tier 1 and 2 notices on a test save, a restart (tiers kept), the fog, the areas, a travel across the map (on the spot, facing along its
  road), a waypoint and arrival, no eggs in a race. Pictures of an easy egg and hard ones (roof, glass pipe, over BJ1, the dock, a back yard).
Build: one full Build Map (eggs placed into the scene alone first for the checks), then a code-only exe rebuild (the banner's size); `Builds/ChastainRush/` 2026-10-19 03:35.
Imported from Racer: ExplorationCollection (as EggHunt), AcornAreas (as EggAreas), AcornBanner and Synth (as EggBanner), ExplorationMap's fog, destinations, travel and
  waypoint (into Exploration and WorldMap.Explore), WaypointGuide; left behind: VehicleUnlocks, UnlockNotice, the track browser and action sheet, place-name labels.
For Dan to check: the eggs (easy and hard: are the hard ones hard enough, the air ones over the jumps reachable?), the tiers' notices, the map's fog, areas, travel and
  waypoint; what each tier unlocks (to decide).

## Previous delivery 2: Round 47
**Reviewed: Dan drove the Round 48 build 2026-10-19 (reverse Chastain Lakes, the map); his notes are Round 49.**
**Round 47, REVERSE CHASTAIN LAKES and a driveway fix (Dan, 2026-10-19 01:43; report BUG-001 of session
`2026-10-19_01-29-19-650_77c31b`, read by Claude, after the Round 46 exe: Free Roam at night on the Drifter Twin).** Rounds 43 to 46
are reviewed. One full Build Map at the end (or quick builds if only those steps change); short results.
A. REVERSE CHASTAIN LAKES. Dan: "Can we build the Reverse version of this track. I assume we can just turn it around the other way
   without other changes. I don't expect the current short cuts to work and I want to decide on the shortcuts after I drive it in
   reverse." The same lap driven clockwise, as the reverse Chastain Ridge was added (Round 39): its own lap in the race menu
   (Direction: reverse), checkpoints and arrows the other way, the route's `directions` reverse, `ai_zones_reverse`, its own
   records boards and ghosts, traffic as forward, the same laps. NO shortcuts in reverse: the boat jump's turn-off and the gazebo
   drain are closed in a reverse race (blocked as any road off the lap, the PARTY sign hidden; the drain's cover shut) and leave no
   easier free way; the lake trail, footbridge and dam path driven north to south. Fix only what turning it round breaks (a
   barrier facing the wrong way, a checkpoint on a blind crest, a landing zone in the way); change nothing else. Check: a reverse
   lap each on Street Classic and Drifter Twin; one AI race, all laps, traffic Normal (everyone finishes, 0 missed); the forward
   race unchanged (one lap). Pictures: the start, the footbridge from the north, where each forward shortcut is closed.
B. THE DRIVEWAYS ON CHASTAIN ROAD. BUG-001 (x 1304.53 z 581.52 h 304, Chastain Road Northwest, Free Roam): "These driveways
   shouldn't be here. The driveways would be from inside the neighborhood not on Chastain". The houses along this side of Chastain
   Road at Dan's spot have drives out onto Chastain Road; they belong to the neighborhood behind them, so their drives go to that
   neighborhood's own street, or are dropped where none can reach, and nothing joins Chastain Road there (a lawn and the sidewalk
   instead). Only this row (the houses of that neighborhood that back onto Chastain Road), not a map-wide change. Check: a
   picture from Dan's spot, no drive on Chastain Road; each of those houses' drives meets its own street.
For Dan to check: the reverse Chastain Lakes race (then he picks its shortcuts); the drives at his spot.

**Status (Claude, 2026-10-19): A and B built and checked in the exe by script (11 of 14 over the runs; the 2 lap fails are the NOT MET item), not yet driven by Dan.** Evidence `Data/screenshots/round47/`.
A DONE. Reverse Chastain Lakes is a lap of its own (`make_lakes_route.py`: `loop_rev` on the right-hand lane, 2751 m, 2 laps, 9 checkpoints, `ai_zones_reverse`, 16 road
   barriers plus two that shut the forward shortcuts); Direction: reverse in Race Setup; records and ghosts are per direction as on every course. In a reverse race the
   boat jump's way is barred between houses 146 and 147, the pier is barred at its foot, the drain has a steel grate over it (drivable, the drop off: `LakeDrain.shutReverseOf`),
   the PARTY sign and both sets of worn tracks are hidden (`RaceOnly.hideInReverseOf` / new `showInReverseOf`); all back in Free Roam. Nothing else changed.
   Checks: AI race, 2 laps, traffic Normal: all 3 finish, 0 missed (215 to 274 s). Forward race unchanged (both shortcuts, 16 barriers; a lap 105.9 s, 0 missed). A car dropped
   on the drain sits on the grate.
   NOT MET: the check's own scripted driver can not finish a reverse lap (Street Classic and Drifter Twin): on the downhill S-bend south of the footbridge, between the pool
   house and the tennis courts (x 1375 z 1100), it drifts 2 m wide and drops off the trail's graded middle (the edge falls about 2 m toward the courts) onto the court fence.
   Tried twice (held speed 12, then 8 m/s), stopped (tuning limit). The AI drives it. Picture `47_reverse_original_stopped.png`.
B DONE. The four townhouses of the row on Chastain Road's north side at Dan's spot (buildings 2307, 2310, 2311, 2314) had mapgen drives onto Chastain Road; each
   would have to cross the house behind it to reach the complex's own street, so all four are dropped (lawn and sidewalk there now; `MapBuilder.Round47.cs`, this row only).
   Pictures from Dan's spot: `47_drives_dans_spot*.png`.
Build: one full Build Map (Build 1 crashed on an empty drive, fixed, rebuilt), then code-only exe rebuilds for the check; `Builds/ChastainRush/` 2026-10-19 03:07.
For Dan to check: drive reverse Chastain Lakes (and pick its shortcuts); the downhill bend south of the footbridge in reverse (does the trail's edge by the courts need
   grading?); the drives at his spot.

## Previous delivery 3: Round 46
**Reviewed: Dan drove the Round 46 build 2026-10-19 01:29 (Free Roam and the Chastain Lakes race); no reports on this round (his one note is Round 47).**
**Round 46, CHASTAIN LAKES SHORTCUTS REDONE (Dan, 2026-10-18 22:19, after driving Round 44; start after Round 45).** Round 44
is otherwise accepted (Dan: "The only other issue I found is on the other shortcut"). The two new standing rules at the top of
this file apply (shortcuts must be a risk; pipes through water are aquarium pipes). One full Build Map at the end.
A. THE WEST LAKE JUMP, REDONE. Dan: "I would have asked for this Shortcut to be redone anyway. It fails the rule that it should be
   challenging. It is just a simple ramp that jumps a narrow part of the lake. ... My first option would be to make a bigger ramp
   and point further down the track. You will be forced to clear a bigger part of the lake so if you don't hit it correctly you
   fall into the lake. Also can we come up with something other than another dirt pile? I am thinking you come upon a boat off
   the dock that has a large slide on it that can be used for a ramp."
   Remove Round 44's dredging-spoil pile and its narrow crossing. Through the back yards (the 142 / 143 gap or another, Code's
   choice) to a DOCK on West Chastain Lake's north shore, and tied up off its end a party BOAT (a pontoon boat) with a big water
   slide on its top deck: the slide is the ramp (you drive along the dock, onto the boat and up the slide). Its real size must
   fit a boat-top slide (CLAUDE.md lesson on ramp sizes); the boat may be big. A REAL physics jump off the slide's lip (standing
   rule), aimed further down the course than Round 44's (Dan's red line and beyond: over the wide part of the lake, about 80 m
   of water on his line), landing on Lakeside Drive or its shoulder further along the lap, so the climb up Round 44's 2.7 m bank
   (its NOT MET item) is gone. Hit it right (full speed, straight up the slide) and you clear the water; come in slow or crooked
   and you fall into the lake (slowed, the shallow / deep-water rules, a reset puts you on the race's line, never trapped). It
   must still save time when made. The boat and dock can stay in Free Roam as world (the slide drivable there too). A hint at the
   turn-off as the other shortcuts. Tuning limit: if the full-speed clearance or the time saved is not met after two tries, stop
   and report the numbers (distance needed vs reached per vehicle) for Dan.
B. THE DRAIN'S TUNNEL AS AN AQUARIUM PIPE. Dan: "The part that goes under the lake should be an aquarium pipe." Round 44's LD and
   B28 under Chastain Lakes become glass aquarium runs wherever they are under the water (the network's glass mains' look and
   life: fish, weeds, light shafts, the lake surface above), concrete only where they run under land. Because of the new rule:
   list any other pipe in the network that passes under water without glass, convert it the same way, and say which.
Check: the west jump on all 11 offered vehicles at full speed straight up the slide (clears, lands upright, height and air) and
at 80 % speed or 10 degrees off line (falls in the lake); time saved (Street Classic, Drifter Twin); the AI takes it and
sometimes falls in (human mistakes); a drive through the drain's aquarium run. Pictures: the dock and boat from the turn-off, the
slide, the flight, the landing, the aquarium run from inside.
For Dan to check: the boat jump (a clean run and a miss); the aquarium under the lake.

**Status (Claude, 2026-10-19): A and B built and checked in the exe by script (final build: 11 of 12), not yet driven by Dan.** Evidence `Data/screenshots/round46/`.
A DONE. The BOAT JUMP: off North Lakeside Drive by Bankside Point (a PARTY sign with balloons, worn tracks), down between houses 146 and 147 (no house moved)
   to a dock, a 20 m two-deck pontoon party boat, up its water slide (solid chute, low walls, 4.5 m over the water, 23 degrees at the top); a real jump down
   the lake's south arm, 77 m of water, onto Lakeside Drive at Lakeside Point (its barrier 45 m down it for long landings). Spoil pile and narrow crossing gone.
   Run-up measured first: 34 to 38 m/s at the shore flat out. Full speed: 11 of 11 clear and land upright, 90 to 111 m out (1.0 to 1.6 s more air than Round 44,
   8 to 10 m over the top). 80 %: 11 of 11 come down in the lake, 61 to 74 m out (the arm's shallow end: they wade out up the bank). 10 degrees off: 9 hit the
   chute's walls and drop in 13 to 54 m out; the Street Classic and Pebble Coupe +10 never reached the slide in the check's run (FAIL as written). Saves 5.0 s
   (Street Classic) and 4.0 s (Drifter Twin). AI race x2, traffic Normal: all 6 finish, 0 missed; the AI took the boat jump 7 times (laps with it 95 to 138 s).
B DONE. Glass where the drain's tunnel runs under East Chastain Lake: B28's whole 140 m (its dive under M2a included; LD, the drop's room, stays concrete).
   By the new rule also: B12 under the big lake (44 m), S2 under the Enclave pond (13 m), M3c under pond W14 (12 m). No pipe's floor moved. Not converted:
   the 11 pipes that pass under a creek (M5a, M5b, B9a, B10, M3b, M3c, B11, B14, M4a, B27, M6; a creek has no bed to set glass in). Check: through the drain, 5 s in glass.
Found and fixed in the exe (why FOUR full builds, not one): cars stopped dead on the boat (a thin open slide board under 3.5 g; a deck under the slide; upright
   faces at flush joints; the dock sharing the deck's collider), then every AI car fell in (`ai_speed` capped its run at 18 m/s). Tuning: none needed.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-19 00:37 (BUILD_INFO.txt).
For Dan to check: the boat jump flat out, slow and crooked (is wading out of the arm's shallow end enough of a cost?); B28's glass from the drain; the creek list.

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
