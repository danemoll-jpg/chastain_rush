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

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 31 (Dan, 2026-10-15 14:35; reports BUG-002 to BUG-004 of session `2026-10-15_03-07-51-371_794f57`, read by
Claude).** Rounds 29 and 30 are not yet reviewed. Fix only what is reported, one check each, short results (CLAUDE.md
"Process"); rebuild into `Builds/ChastainRush/` and commit.
1. BURIED BUILDING (BUG-002, vehicle x 3544.56, z 495.74, heading 266, 12 m off an unnamed road): "this looks like maybe it
   is too far in the ground". The building straight ahead shows only its roof above the grass. Set that building on the
   ground at its proper height (foundation as Round 27). Check: a picture from Dan's spot.
2. GRASS ON THE ROAD (BUG-003, vehicle x 1524.43, z 561.77, on New Chastain Road, heading 48): "need to remove grass from
   streets". A ragged strip of grass shows through the road along the center line ahead of Dan's spot. Remove it there so
   the asphalt and its center markings are whole. Check: a picture from Dan's spot.
3. I-575 RAMPS (BUG-004, vehicle x 319.08, z 984.29, on I-575 southbound under the Chastain Road bridge): "Should have
   entrance and exit ramps". The Chastain Road interchange has no ramps (the map build drops OSM's motorway_link ways;
   `Data/map.json` has none). Build this interchange's entrance and exit ramps from OSM's motorway_link ways, joined to
   I-575 and to Chastain Road at their real places, with grades a vehicle drives flat out, and traffic using them. Check:
   drive each ramp on and off once.
4. DRESS THE BUILDINGS OUTSIDE THE NEIGHBORHOODS (Dan: "I want to have the buildings that aren't part of a neighborhood
   also dressed up. Much of Canton Road would be businesses or schools."). Every building not in a dressed neighborhood or
   the shopping center gets a look that fits what OSM says it is: offices, retail and commercial strips (storefronts,
   signs with made-up names, Dan 2026-10-10: "Store names can be faked"), schools, churches, industrial and warehouses,
   each with a paved parking lot and drive where it has one, lamps, a few trees; untagged ones along Canton Road as small
   businesses. Signs follow the sign rules (CLAUDE.md). Same LOD / batching as the neighborhoods. Check: one picture per
   kind, and the frame rate on Canton Road before and after.

## QUEUED rounds
None.

## Previous delivery 1 (newest): Round 30
**DONE (Parts A and B built, not yet driven by Dan) — Round 30 (Dan, 2026-10-15 09:50), after Round 29. Dan: "I would like to add traffic and maybe we can make
better houses in the remaining neighborhoods. I have no specific styles for other neighborhoods though like I do with my
own community". Part A then Part B; rebuild into `Builds/ChastainRush/` and commit after each, give the build time.**

Part A, TRAFFIC. Racer has traffic (left behind in Round 15 / 16). Port it (CLAUDE.md: prefer porting; list what came):
A1. Cars (and a few trucks / vans, Racer's traffic models) driving the map's roads in their lanes on the right, at road-type
   speeds (neighborhood streets slow, Chastain Road / Bells Ferry / Canton Road faster, I-575 fastest), turning at
   intersections, stopping and yielding where Racer's traffic does, pulling in and out of the neighborhoods; parked cars
   in some driveways if cheap.
A2. Arcade (CLAUDE.md "a fun arcade game"): the player can hit them; they react as in Racer (knocked, honk, swerve).
   They are real traffic and CAN block you (Dan, 09:54: "could in fact block you"): a car stopped at a junction or
   turning in front of you is in the way, as on a real road. They never enter the trail, yards, the pipes or the
   shortcuts.
A3. ON by default everywhere, free roam and races (Dan, 09:54: "cars should not be off in races ... But you can turn
   traffic off in the settings"): a Traffic row in Settings (Off / Light / Normal / Heavy, default Normal), saved. In a
   race traffic drives only on roads open to it: the current races have none (Noonday Creek is a trail, the Underground
   race is in the pipes, the Chastain Glen loop is a blocked-off neighborhood), so traffic stays out behind the race
   barriers and off their routes; a future race on open roads gets traffic on its course. The AI racers deal with
   traffic too (slow, pass, or hit it). World vs. game layer: traffic is game layer.
A4. Sounds through the Round 27 AI engine sounds (OtherEngines) with the voice budget; horns.
A5. Cost: spawn round the player and despawn out of sight; frame rate in free roam at the busiest place (Chastain Road
   at the shops, Normal and Heavy) before and after, reported.
Part A status: done (built and checked in the exe by script, not yet driven by Dan). Racer's traffic (RaceDirector.CreateCars:
copies of the player's vehicle as the Street Classic in Racer's traffic bodies, `AmbientVehicle`, the Blender kit TrafficSedan /
Wagon / Pickup / Van; RoadDriver's traffic driving; the offscreen recycle) now drives the whole road graph: `TrafficNetwork`
(built by the map build, `MapBuilder.Traffic.cs`: 541 road pieces, 496 junctions and ends from every motorway, primary,
secondary, tertiary, link, unclassified and residential road and bridge; no service roads, lots or drives), `TrafficDriver`
(Racer's steering, its traffic corner and braking rules and obstacle query, the lane on the right (one-way roads right of
middle), the road type's speed: residential 11 m/s, tertiary 16, secondary 18, primary 20, I-575 29; the next road picked
at each junction (the same road on most often, into a neighborhood now and then), a stop at a junction onto a bigger road
and a yield to whoever is already in one, a turn round at a dead end; the horn and a moment knocked when the player hits it
(it is 25% lighter than the Street Classic), a swerve from a vehicle coming at it in its lane, the horn at one in its way),
`Traffic` (Off / Light 7 / Normal 14 / Heavy 22 cars; 140 to 400 m from the player out of the camera's sight, gone past
460 m out of sight, stuck or at the map's edge; the pool made while the title menu is up). Settings > GAMEPLAY > Traffic
(saved in settings.json, default Normal). In a race every road near its line or a closure is closed to traffic (the
Chastain Glen loop: 23 road pieces; the Underground race: only its closures; a route with "traffic": true would keep its
roads open); the AI racers slow for traffic ahead with their own obstacle query. Engines: OtherEngines' two traffic voices
(Racer's), also in free roam; the horn is synthesized. Parked cars in drives: not done (left out to keep the cost down).
Check: `ChastainRush.exe -exeRound30 <dir> -traffic Normal [-r30Only fps,drive,hit,race,pics] -logFile <dir>\Player.log`
(10 checks pass; pictures in `local/round30/traffic/`). The busiest place is Canton Road at the shops (the shops are on
Canton Road, not Chastain Road): standing there at 1280 x 720 with VSync and the frame cap off, Off 378 fps, Normal 172,
Heavy 99 (not measured at 3840 x 2160). Imported from Racer: AmbientVehicle (copied, split-screen removed), the traffic
models (TrafficSedan, TrafficWagon, TrafficPickup, TrafficVan .fbx and Tools/Blender/traffic.py), RoadDriver's traffic
driving and CreateCars' way of making the cars (rewritten for the road graph in TrafficDriver / Traffic), OtherEngines'
traffic voices.

Part B, BETTER HOUSES IN THE REMAINING NEIGHBORHOODS. Chastain Glen (Dan's, hand-made styles), Grasmere Court, Chastain
Commons and Chastain Enclave are dressed (Round 7, Round 22); every other neighborhood still has plain boxes. Dan has no
styles for them, so Code picks believable ones (houses are not set in stone; Dan's house stays locked):
B1. Group the remaining houses into neighborhoods by their streets (the subdivision each street belongs to; names from
   OSM where it has them) and list them (name, streets, house count).
B2. Give each neighborhood ONE consistent look of its own, from the kinds of houses typical of this part of Marietta /
   East Cobb, so the map reads as different places: 1970s ranches and split-levels (brick and painted wood siding, low
   roofs, carports); 1980s to 1990s two-story traditionals (brick fronts, siding sides and back, two-car garages, a
   bonus room over the garage); 2000s craftsman-style (stone or brick wainscot, gables, covered porches); townhome rows
   where the footprints are rows. Pick the era per neighborhood from its footprints (size, spacing, lot shape) and say
   why. Within a neighborhood: 3 to 5 variants and a color palette so it is varied but clearly one place.
B3. Same quality and kit as Chastain Glen's houses (roofs, windows, doors, garages, porches, foundations as settled in
   Round 27, drives, mailboxes, a few trees and fences), on the real OSM footprints, reusing the communities code
   (`MapBuilder.Communities.cs`, `communities_area.py`). Breakables as in the other neighborhoods.
B4. Frame rate: the same LOD / batching as the dressed communities; numbers before and after at two views over the
   most built-up area.
B5. Pictures from the exe: one street view per neighborhood, plus an overhead of each, in the status, so Dan can pick
   changes ("build it, then Dan drives it").
Part B status: done (built; pictures from the exe, not yet driven by Dan).
B1: `python Tools/mapgen/neighborhoods_rest.py` (re-run after mapgen) writes `Data/neighborhood/neighborhoods.json`: 24
   neighborhoods, 1539 houses (streets joined without going out onto a bigger road; names from OSM where it has them,
   Canterbury, else the main street). Full list with streets and numbers: `local/round30/neighborhoods/list.txt`.
B2: one era each, from the footprints (OSM footprints include the garage and are coarse, so the spacing between houses, the
   lot width, says the most): ranch (wide lots, 32 m or more apart): Embry Lane 157, Blackwell Circle 13, Suburban Drive
   33, Willard Drive 31, Pine Manor 29, Westmoreland Drive 14, Chastain Trace 7; craftsman (big footprints packed under 26
   m apart): Guidhall Trail 252, Canterbury 7, Estates Landing Drive 21; townhome rows (under 18 m apart): The Willows 31;
   traditional (the rest, 26 to 32 m apart): Centerview Drive 109, Lakeside Drive 219, Kathryn Drive 141, Southwick Drive
   53, Hartley Woods Drive 120, Windridge Drive 18, Station Drive 86, Sellers Way 15, Chastain Crossing 30, Old Farm Walk
   74, Lansing Drive 13, Parkwood Drive 30, Chastain Manor Way 36. New styles (`MapBuilder.HouseEras.cs`): E split-level,
   F craftsman (stone wainscot, carriage garage doors, a porch on stone piers with tapered columns, shingles in the gables),
   G townhome row (units side by side, alternating brick and siding, a door, stoop and one-car garage each), H ranch with a
   carport. Mixes: ranch E / C / H; traditional A / D / B (brick fronts) / C; craftsman F / A; townhomes G. Each
   neighborhood has its own few siding, brick and roof colors (picked by its key) from its era's palette.
B3: `MapBuilder.Neighborhoods.cs`: Chastain Glen's house kit on the real footprints, each house facing down its own OSM
   drive (1507 of 1535; the rest face their street), the garage at the drive; floor at the foot of the garage; Round 27's
   block foundation (1003) and walk-out wall (600) where the ground falls away; shrubs, one box collider, a breakable
   mailbox (799), a breakable privacy fence behind about a third (477, kept off roads, drives, buildings, water, the trail,
   the shortcuts and the pipe cuts); no tree inside a house. Not done: the lot cuts of Round 27 (477 houses have ground more
   than 0.6 m over their floor somewhere, as the plain boxes did; no ground was cut map-wide), curbs, sidewalks and lamps
   on these streets (not asked for).
B4: same chunking as the Glen (128 m mesh chunks, details in a shadowless mesh). Frame rate, traffic off, 1280 x 720, VSync
   and the cap off (not measured at 3840 x 2160): Guidhall Trail (Cambridge Drive) chase 371 -> 342 fps, far chase 377 ->
   344; Lakeside (Chastain Lakes Drive) chase 290 -> 277, far chase 320 -> 262.
B5: pictures in `local/round30/neighborhoods/`: `street_<key>_<name>.png` (one per neighborhood, from the exe) and
   `overhead_<key>_<name>.png` (the built scene from above; magenta is outside the map), `streets.png` / `overheads.png`
   sheets. For Dan to look at: Willard Drive's first house turns its side to the street (its drive comes from the cross
   street). Imported from Racer: nothing.

## Previous delivery 2: Round 29
**DONE (built and checked in the exe by script, not yet driven by Dan) — Round 29 (Dan, 2026-10-15 09:45, after the Round 28 exe). Rebuild into `Builds/ChastainRush/`, commit, give
the build time. Dan drives it.**
Status: done. GARAGE > Rider... (Racer's Rider page: `Menus/GameMenus.Garage.cs` RenderRider, StepRider, RandomizeRider): Randomize,
Back, then a row per RiderLook option (Body, Skin tone, Hair with the Round 28 styles, Hair color, Hat, Hat color, Shirt, Shirt color,
Pants, Pants color: everything Racer's page has; Racer's Classic / New toggle not needed, this game has only the New models), left /
right changes a row, A never does, B back; the turning preview framed on the rider (a car's body hidden; a bike, ATV or mower shown
whole with the rider: Racer framed only the rider, which cut the bike off). Saved at once in settings.json as Racer does
(`GameSettings.Options.rider`), loaded at start before any vehicle is built; the vehicle in the world is put on again with the new
rider when the page is left (`VehicleSwitch.RebuildVisual`), so every vehicle and the first-person hands wear it (skin color on the
hands; the AI riders stay random, never the player's look). Check: `ChastainRush.exe -exeRound29 <dir> -logFile <dir>\Player.log`
(`Checks/Round29Runner.cs`, 14 checks pass; pictures of the page and of three riders on the Needle 600, the Street Classic and the
ATV, outside and first person, in `local/round29/`). Imported from Racer: the Rider page (RenderRider, StepRider, RandomizeRider,
the rider framing of GaragePreview, settings.json's rider), rewritten into GameMenus; RiderLook was already here (Round 13 / 28).
1. RIDER PAGE. Dan: "There is no place to customize your person". Racer's rider page was left behind in Round 16 (CLAUDE.md
   list), so the new hands and hair styles (Round 28 Part C) cannot be picked. Port Racer's rider page into the game menu
   (from the garage, as in Woodstock Rush, or wherever Racer has it): every `RiderLook` option (body, skin, hair with the
   new styles, hair color, hat, hat color, shirt, shirt color, pants, pants color, and anything Racer's page has besides),
   a turning preview of the rider on the chosen vehicle, controller and keyboard as the other menus, saved between runs
   as Racer saves it, and the player's rider in the game uses it (every vehicle, first person hands' skin / gloves too).
   Port Racer's code (CLAUDE.md); list what came. Pictures from the exe of the page and of three different riders.

## Previous delivery 3: Round 28
**DONE (Parts A to C, ad0ed37 to 4e4cd51) — Round 28 (Dan, 2026-10-15 03:22, after driving Round 27 including the Underground race; session
`2026-10-15_03-07-51-371_794f57`, one report, read by Claude). Rebuild into `Builds/ChastainRush/` and commit after each
part, give the build time. Dan drives it.**

Part A, MANHOLE COVERS STILL MISSING. BUG-001 (vehicle x 2710.04, z 1193.05, heading 86, on Westminister Way, 14 m short
of MH7 at x 2724, z 1197): "there is no manhole cover here". The screenshot shows plain asphalt ahead: no cover. Dan, again:
"Still need to make sure it is putting manhole covers in." The Round 25 follow-up's lighter cover is not showing (lost in
Part A's road / bridge rebuild, drawn under the road, culled, or never built for some). Find why and fix it, then PROVE it:
a picture from the exe from a driver's height, a few meters back, at EVERY manhole MH1 to MH13, each showing the cover
(list them). A normal-size cover (about 0.9 m), visibly lighter than the road, flush, no bump (as Round 25 A1).

Part B, UNDERGROUND RACE (Dan's notes, numbered as his):
B1. "Lets try the shortcuts and see what happens." Build all three as Code proposed them in Part D's status: S1 the leap
   off M1's mouth over the waterfall to the floor (gate across M1's mouth on the floor side, plate up M5's wall); S2 the
   dive pipe under M5 between two new junctions (its east junction clear of B8); S3 the high line over J2 (even if it
   saves little: Dan wants to see it). Each behind its switch gate (the mechanism built in D2), shortcut design rules
   (CLAUDE.md), the AI by ShortcutStrategy, on the map. Remove the TEST gate if it is still in.
B2. "The tight hairpin turn right before the finish is a terrible idea. Lets move it down a level where there is some
   open space." The turn at the top of the mid-to-top ramp onto the top ledge, just before the line. Re-route the lap's
   end so the turn-round happens on a lower level with room (the mid ledge or the hub floor), a wide, sweeping turn,
   and the finish line and grid where they fit with it (the grid off the racing line, so a stopped player is not in
   the way). Code proposes the new end of the lap in the status; build it.
B3. "The entrance back into the hub I think should have a ramp or something." Where the lap comes back into the hub
   (M1's mouth onto the mid ledge, or wherever the new route enters): a proper ramp / apron from the pipe onto the
   ledge, no drop, step or lip, drivable flat out on a bike and a car.
B4. "Reset button during the race leaves me turned in the wrong direction. It should be following the rule to reset
   straight forward." In the Underground race a reset (R / Y) must put the vehicle on the route, facing the way the race
   goes, as in every race (CLAUDE.md / DrainPipe "resets"). Find why it does not here (the route's own heights, the pipe
   reset taking the vehicle's heading instead of the race's, the ledges) and fix; check a reset at 20 places round the
   lap (pipes, glass, junctions, ledges, ramps): every one faces forward, on its wheels, on the route.
B5. "I still think don't want the bike to get turned around sideways so often." Bikes (the Drifter Twin) still end up
   across the pipe. Find what turns them (wall riding's hold on two wheels, wall contacts, the lean, junction edges,
   landings) by driving bikes through the whole lap by script with the heading logged; fix so a bike only goes across a
   pipe when the player steers it there. If the wall riding is the cause, make it gentler for bikes (stronger pull back
   along the pipe, a higher speed to stick) rather than off.
B6. "Can the camera stay whatever view you have it in? I drive in first POV and it keeps switching to a follow cam back
   to first POV." The view the player picks is kept everywhere: the pipe camera (`ChaseCamera.PipeView`, PipeUp) must
   only change how a CHASE view follows, never switch a first-person / cockpit / hood view to a chase view. Check every
   view underground, through the express, mouths, the hub, a reset and race start and finish: it never changes by itself.
   Standing rule for CLAUDE.md: the camera view only changes when the player changes it.

Part C, RIDERS (unrelated; Racer's rider is `Tools/Blender/rider.py`, `RiderLook.cs`, `RiderGestures.cs`, `VehicleVisual.cs`):
C1. HANDS: "can we fix the drivers' hands so they don't look so bad?" The hands (seen big in first person on the bike's
   grips) are lumpy blocks. Model proper low-poly hands: a palm, fingers wrapped round the grip / wheel, a thumb, a cuff
   or glove edge, matching the skin color (or gloves), placed so they sit on the grips and the wheel in every vehicle and
   follow the steering. Pictures from first person and from outside.
C2. FIST WAVE AND CELEBRATIONS: "add the waved fist and celebrations from Woodstock Rush". `RiderGestures` is in the code
   already (fist wave on F / LB; an AI rider waves at whoever ran into it; the winner's victory celebration). Check in the
   exe whether they work here; make them work as in Woodstock Rush (bring Racer's latest version and anything it added
   since: more celebrations, the AI's reactions, sounds). Say the buttons.
C3. HAIR: "can we fix some of the hair styles? Especially short and medium don't really work well. Maybe add some new
   ones like mohawk, crewcut and spiky". Remodel Short and Medium so they sit on the head properly (no gaps, no clipping
   through the hat or the head, shaped like real hair), and add Mohawk, Crew cut and Spiky (and Buzz or Curly if cheap),
   all in the garage's hair list, all working with every hat (a hat hides what it should) and every hair color, and the
   AI riders' random looks using them. Pictures of each hair style from front, side and back, with and without a hat.

**STATUS of Round 28 Part C (Claude, 2026-10-15): C1, C2, C3 DONE, exe rebuilt (2026-10-15 07:48), committed; checked in the exe by script (muted,
minimized) and in Blender renders, not yet seen by Dan.**
- The rider is now made in this repo: Racer's `Tools/Blender/rider.py` and `kit.py` copied (Racer unchanged) and changed here; `blender --background
  --factory-startup --python Tools/Blender/rider.py` (Blender 3.6 at `F:\blender\blender.exe`, Racer's) writes `Assets/Resources/VehicleModels/Rider.fbx`
  and `SourceArt/Blender/Rider.blend` (and `Rider-check.txt`: hair split at the hat band, hats covering the head: 0 errors in every pose).
  Pictures: `Tools/Blender/render_heads.py` (new; Racer's render_rider.py predates the arm parts and was left behind).
- C1 DONE. The hand was a capsule, a ball for the fist and a ball for the thumb. Now a closed hand round the grip: a palm block from the wrist to
  the knuckles (over the top of the grip), four fingers curled round the grip's own axis (a bike's bar, the car's rim) and under it, a thumb
  wrapped under from the inside, fingertips, the wrist into the sleeve; skin colored; in the hand's arm group, so it follows the steering and
  the gestures as before. Pictures from first person (bike: the left hand on its grip; the right one sits under the debug panel) and outside:
  `Data/screenshots/round28/c1_c2_hands_gestures.jpg`. Worst-case rider triangles 14,700 (were 8,900).
- C2 DONE. In the exe, three things did not work: the fist button (LB / F) was bound but never asked for (Racer's VehicleInput asks; now here too),
  the AI never reacted (RiderGestures had "no AI drivers yet" from Round 14) and nothing triggered the winner's celebration (Racer's RaceDirector
  does it; now RaceController.Finish: the first finisher with the best total, in a race with others). First person now aims the wave into the
  view (CameraViews), as Racer. Racer's RiderGestures has nothing newer (4 lines differ: ours), no gesture sounds, and its winner camera shot is
  left out (the view only changes when the player changes it). Buttons: LB on the controller, F on the keyboard. Check (`-r28Only riders`, a
  virtual pad, on the Drifter Twin and the Street Classic): LB waves in the chase view and in first person; an AI rammed by the player shakes a
  fist at it; the AI winner of a 1-lap race celebrates: 8 of 8.
- C3 DONE. Short and Medium were one smooth shell with a straight cut (a helmet). Short: closer to the head, a ragged brushed-forward fringe,
  sideburns, small flat locks on the crown. Medium: fuller, a swept fringe, down over the tops of the ears, layered at the back. New: Mohawk (a
  crest over thin sides), Crew cut (short sides, a little longer on top), Spiky, Buzz, Curly; in the garage's hair list after Bald (saved looks
  keep their hair) and in the AI riders' random looks. With every hat the hair above the band is hidden and nothing pokes through (Rider-check).
  Pictures, front / side / back and under each hat: `Data/screenshots/round28/c3_hair_styles.jpg`, `c3_hair_styles_hats_back.jpg`.
- Imported from Racer in Part C: Tools/Blender/rider.py and kit.py (changed here), the input line that asks for the wave, RiderGestures' AI and
  first-person rules, RaceDirector's winner celebration rule.
- For Dan to check: the hands on the grips in first person, the fist (LB / F), the hair styles in the garage.

**STATUS of Round 28 Part B (Claude, 2026-10-15): B1 to B6 DONE, exe rebuilt (2026-10-15 07:01), committed; checked in the exe by script (muted, minimized,
1280 x 720), not yet driven by Dan.**
- B2 DONE (the end of the lap; Code's proposal, built). Why the hairpin: M1 comes into the hub at mid level on the west wall and M5 leaves at top
  level on the south wall (a 20); the mid-to-top ramp arrived on the top ledge pointing west, so every lap ended in a U-turn on the 7 m top ledge.
  Now: that ramp is gone and THE SPAN replaces it (MapBuilder.NetworkHub.cs BuildSpan): a 9 m deck from the mid ledge's north side straight
  south across the chasm onto the top ledge in front of M5's mouth, 21 % at its steepest (eased at both ends), M5's water running down it. The lap
  comes out of M1, turns right and goes round the mid ledge's south, east and north sides (the turn-round, a level down, where there is room:
  four 90 degree corners on a 9 m ledge, AI held to 9 to 11 m/s there), left onto the span, up it and straight into M5. The LINE is on the span,
  13 m up it (9 m wide); the GRID is on the mid ledge's north side west of the span, off the lap (40 m from it): a stopped player is in nobody's
  way. Lap 1370 m (was 1488). Finished AI cars drive on up the span and park on the top ledge west of M5's mouth, off the lap (route
  inish_park, RoadDriver.DriveToPark; stopped on the line they blocked M5's 5 m mouth).
- B3 DONE. At M1's mouth the floor runs smoothly onto the mid ledge on the pipe's center line, but a vehicle off center rode the barrel's curve
  and dropped 1.2 m off its end (probe: ChastainRush.RayProfile.Run, new, Editor batch). An apron (BuildMouthApron): the barrel's lower curve
  carried 5 m out onto the ledge and eased flat, with sloping skirts, 9.5 m wide: no drop, step or lip anywhere across the mouth.
- B1 DONE, all three built as proposed (world pipes and junctions, Tools/mapgen/pipe_network.py; the race layer from
  make_underground_route.py), each behind a striped switch gate (Round 27's mechanism), the AI by ShortcutStrategy, on the map like any route:
  - S1 "Waterfall leap": out of M1's mouth straight over the ledge's edge with M1's water to the hub's floor, round the pool's west end and up the
    floor ramp to the east side (124 m against 141 m of the lap; forgives CP5). Its gate is a section of the railing (race only) along the mid
    ledge's west and south edges, which stop a free fall there taking the same way. Plate: up M5's right-hand wall, 60 m in from the hub (68
    degrees round: ride the wall).
  - S2 "Dive pipe": two new junctions on M5 (J14 at its station 205, J15 at 430: the proposal's 389 was 2 m from B8, here 30 m clear), M5's line
    and floor kept, and a pipe between them diving under M5's bend (to 1.8 m under it; up to 24 % where it climbs back, steeper than the hub's
    20 % ramps) with a flooded sump at the bottom (170 m against 268 m; forgives CP2). Its line in J15 is a 9 m arc (S2 leaves through the side
    wall). Plate: on J3's floor 6.5 m off the line. Saves more than the proposal's 45 m because J15 had to move: Dan to judge.
  - S3 "High line": J1 straight to a high opening 2.5 m up J2's west wall (J2 made 3 m taller and 2 m wider), lined up with M1b across the room:
    a jump over J2's floor (183 m against 203 m; forgives CP3, CP4). Plate: on J1's floor 6.8 m off the line.
  The AI go for the plates on detours (lap options, ShortcutStrategy). Round 27's check-only TEST gate is gone (RaceController.ExtraSwitchGates
  removed).
- B4 DONE. Cause: a reset in a race inside any pipe, junction or the hub's ledges used the pipes' own reset, which faced the vehicle the way it was
  pointing (it knew the race's way only on a shortcut's line). Now in the underground race a reset finds the nearest point of the race's line in
  3D (the lap or a shortcut's; the lap passes over itself at the span), puts the vehicle there on its wheels facing the way the race goes, and
  never back across the line (CarRecovery.UndergroundSpot, RaceRoute.NearestOnRoute). Same for the AI's own resets near the line, and an AI
  reset twice on the same shortcut gives it up for the lap. Check: 20 places evenly round the lap (pipes, glass, junctions, ledges, the span),
  the vehicle put there facing backward then reset: 20 of 20 facing the race's way (0 to 10 degrees off), on its wheels, on the route.
- B5 DONE. Found by script (-r28Only bikes,bikewall): a whole lap at race pace and riding the walls of M1b at 12, 20 and 28 m/s (steered hard up
  each wall, let go). A centered lap never turned the bike across by itself; what did: slowed on the roof (throttle off at 20 m/s), the Drifter
  Twin dropped off and stayed 30 to 37 degrees across for a second, and steered on the wall it went 25 to 37 degrees across. Now for bikes
  (DrainPipe.cs): held to the wall from 2 m/s faster, at most 20 degrees across on the wall (was 35), pulled back along the pipe 1.6 times as
  hard, and dropped off the wall or knocked with the steering let go, turned back along the pipe in the air and on the floor. After: 12 of 12
  wall cases come back along the pipe when let go; the lap: no spell across without steering.
- B6 DONE. Cause: CameraViews (Racer's) eased first person back to the chase view whenever the vehicle tilted past 70 degrees or wiped out, and
  every wall ride in a pipe tilts it that far. Now the view never changes by itself. Check: first person chosen, a whole lap (pipes, glass,
  junctions, M1's mouth, the hub, the span), a reset, the finish and an express ride in free roam: never changed, the camera always at the
  rider's eyes (13,000 frames).
- Check (ChastainRush.exe -exeRound28 <dir> [-r28Only race,shortcuts,resets,bikes,bikewall,camera] -vehicle drifter, Checks/Round28Runner.cs): 25 of
  25. Race: 3 AI (Pebble Coupe, Turf Rocket, Street Classic, taking every open shortcut and switch detour for the check): every switch opened by an
  AI racer, S1 taken 6 times, S2 6, S3 3; all three finish, no missed checkpoint, 3 to 9 AI resets each; 60 fps (vSync, 1280 x 720, not measured
  at 3840 x 2160). Pictures: Data/screenshots/round28/b_hub_span_apron_shortcuts.jpg.
- Left for Dan: the scripted lap at race pace once went off the mid ledge onto the floor at the turn onto the span (a 90 degree corner on a 9 m
  ledge, no railing there: the floor ramp brings you back); say if it wants a railing. Mouths onto ledges other than M1's still end the barrel
  at the ledge (not changed: Dan pointed at M1's). Imported from Racer: nothing.
- For Dan to check: the Underground race (the span, the grid, the line), S1 to S3 and their plates, resets anywhere, bikes on the walls, first
  person underground.

**STATUS of Round 28 Part A (Claude, 2026-10-15): DONE, exe rebuilt (2026-10-15 03:52), committed; pictures from the exe by script (muted,
minimized), not yet driven by Dan.**
- A DONE. Why the covers were missing: every cover was built (13 of 13 in the build log), but it was drawn at the terrain's height + the
  road's lift + 12 mm, while the street's own triangles over the hole (the hatch) stand 0 to 0.1 m higher on the communities' streets and
  on slopes. So the street was drawn OVER the cover on MH4, MH7 (Dan's BUG-001), MH10 and MH11, and the two fought (flicker, mostly street)
  on MH3, MH5 and MH9; only MH1, 2, 6, 8, 12 and 13 showed. Fix (MapBuilder.Network.cs BuildManholeLid): each point of the cover is now set
  12 mm over the highest street triangle under it. Probe of the built scene (ChastainRush.ManholeProbe.Look -probeOut <file>, new, Editor
  batch: every drawn surface over each cover): the cover is 12 to 13 mm over the street at its middle and 0.3 m round it on all 13. The
  cover is a drawing only; what a wheel touches is unchanged since Round 25 (the terrain's own surface), so no bump. Size 0.9 m, lighter
  cast iron as Round 25.
- Pictures from the exe, first person in the Street Classic, 12 m back on each manhole's street (-exeShots takes a new "view": "first"):
  MH1 Parkwood Dr, MH2 Parkwood Dr, MH3 Bloombridge Way, MH4 Rotherhithe Landing, MH5 Bloombridge Way, MH6 Guidhall Trail, MH7 Westminister
  Way, MH8 Paxton Ct, MH9 Grasmere Ct, MH10 Weybourne Ct, MH11 Chasefield Way, MH12 Laurel Green Ct, MH13 Chastain Lakes Dr: the cover
  shows in every one (Data/screenshots/round28/a_manholes_all13.jpg, MH7 full size _MH7_westminister.png).
- This exe also has Part B6's camera change (it compiled into the same build). Imported from Racer: nothing.
- For Dan to check: Westminister Way (MH7) and any other street manhole.

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
