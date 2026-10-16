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
**Round 32 (Dan, 2026-10-15 17:27; reports BUG-005 to BUG-026 of session `2026-10-15_03-07-51-371_794f57`, read by Claude,
after driving the Round 31 exe).** Rounds 29 to 31 are reviewed (driven by Dan; what he found is below). Fix only what is
reported, one check each, short results (CLAUDE.md "Process"); rebuild into `Builds/ChastainRush/` and commit. Building
numbers are indexes into `Data/map.json` "buildings"; Dan's coordinates are the vehicle's (x, z, heading).

Part A, CANTON ROAD BUSINESSES FACE CANTON ROAD. Dan: "In general not sure why most businesses on Canton road were facing
away from Canton road without any driveways." Why (Claude): Round 31 picks each building's front as the face looking most
toward the nearest OSM parking lot or road, any road, so a service road or side street behind a store beats Canton Road; and
lots were paved only where OSM maps one (9 outside the dressed areas), so most businesses got no lot and no drive.
A1. THE RULE (Dan said "in general", so it applies to every dressed business along Canton Road, not only his spots): a
   shop, office, church, school or other business whose lot touches Canton Road, or that stands between Canton Road and the
   street behind it, has its front toward Canton Road, a paved parking lot between it and the road (stalls, light poles,
   lamps as Round 31's lots), and a drive from Canton Road into the lot (curb cut, no step). Neighbors share one lot where
   they stand in a row. Keep OSM's own lots and service roads; join a service road to the lot it serves. Canopies, sheds
   and garages are not businesses. Check: Dan's spots below, one picture each.
A2. Dan's spots, each by A1: BUG-005 / BUG-017 (x 3532.61 z 1027.29 h 20; x 3588.14 z 1054.82 h 243): building 7, a strip
   with Fit Zone Gym, Pho Saigon, Green Leaf Smoothies, U-Store-It, facing W D Anderson Road ("stores should be facing canton
   and have a parking lot"). BUG-006 (x 3412.71 z 1479.90 h 263): buildings 8, 9, 10 ("business should be facing canton road
   with a parking lot"). BUG-007 (x 3324.50 z 1582.84 h 74, Westchase Drive): building 12, faces the houses behind it ("again
   should be facing canton road"). BUG-008 (x 3380.86 z 1703.75 h 69): building 3 ("Face canton road have parking lot").
   BUG-022 (x 3636.20 z 439.60 h 275): building 88, U-Store-It ("another one turn it around"). BUG-023 (x 3533.79 z 181.78
   h 211): buildings 20 and 40, the strip with Sparkle Car Wash ("should have parking lot").
A3. BUG-019 / BUG-020 (x 3643.87 z 696.83 h 39; x 3649.78 z 692.80 h 111): building 76, one 10,900 m2 retail block shown as
   long blank walls. Dan: "these should all be store fronts with a parking lot here. The corner shop is a pizza place with a
   drive up restaurant. There should also be a comic book store and another store here", "make these all store fronts".
   Storefronts along the whole face toward Canton Road with the lot in front (A1); at the corner nearest Dan's spot a pizza
   place with a drive-up window and its lane round the side; next to it a comic book store, then one more store; the rest
   ordinary storefronts. Made-up names (sign rules); add the new signs to `make_business_signs.py`. (If the corner shop he
   means is the small building 77 beside it, it is the pizza place instead; say which in the status.)
A4. BUG-014 / BUG-015 (x 3489.82 z 1213.04 h 58; x 3488.39 z 1200.49 h 89): building 86 is dressed as a house beside the
   OSM lot at x 3503 z 1217. Dan: "why does a house have a parking lot", "actually I am pretty sure this is an ice cream shop
   in the real world". Dress building 86 as an ice cream shop (storefront, a walk-up window, made-up name), using that lot.
A5. BUG-021 (x 3563.64 z 584.19 h 243): churches 74 and 75 build as overlapping boxes with a steeple in the wrong place
   ("what kind of monstrosity is this?"). Make it read as one church: wings joined cleanly, one roof per wing with no boxes
   poking through, the steeple over the main entrance, the front toward Canton Road with a lot (A1). Check: Dan's spot.
A6. BUG-009 (x 3390.31 z 1772.89 h 49): the unnamed road ends short of the preschool's lot ("this road should connect to the
   parking lot"); join it to the lot. Dan, 17:27: IGNORE the comment's "Make this an elementary school" (it is building 14,
   Little Acorns Preschool, and stays one); instead the middle school becomes an elementary school. Both schools (0 by I-575,
   41 by Canton Road) drew "Noonday Creek Middle School"; make 41 (the one by Canton Road Dan drove past, BUG-018) "Pine
   Hollow Elementary", and leave 0 the middle school so the map has one of each (Dan to say if he meant both).
A7. BUG-016 (x 3499.29 z 1136.58 h 94): "Road to nowhere", an unnamed 5 m road ending in grass. Join it to the lot or building
   it led to (OSM / A1); if it serves nothing, remove it. Check: Dan's spot.

Part B, GRASS IN THE STREETS AND A TRAFFIC JAM.
B1. Grass showing through the asphalt at: BUG-010 (x 3377.17 z 1656.77 h 157, Canton Road junction), BUG-011 (x 3408.69
   z 1504.58 h 34, the unnamed road's mouth onto Canton Road: also two shades of gray, "smooth this"), BUG-012 (x 3406.78
   z 1554.13 h 350), BUG-013 (x 3467.43 z 1242.21 h 161), BUG-018 (x 3556.81 z 825.01 h 21), all on Canton Road, and BUG-024
   (x 1139.84 z 667.91 h 134, Chastain Road Northwest: a ragged strip down the center line). Round 31's BUG-003 was the
   same thing (a gap between carriageways / turn lanes, paved over by hand). Dan has now said it six more times ("need to
   remove grass from streets"), so find the cause in the road build and fix it there, so the road surface is whole and one
   shade at junctions and between parallel lanes; real grass medians, if OSM maps one, stay. Check: a picture from each
   of the six spots.
B2. BUG-010: four traffic cars stopped in a knot in the junction ("Is this a car accident?"). Find why they stopped (each
   yielding to another, or stuck on the grass seam) and fix it so traffic never locks itself at a junction. Check: Heavy
   traffic at that junction for 5 minutes, no car stopped there longer than 10 s.

Part C, CHASTAIN LAKES (BUG-025 x 1312.55 z 1080.73 h 54; BUG-026 x 1314.67 z 1085.28 h 331, off Lakeside Drive).
C1. Round 30 named this neighborhood "Lakeside Drive" (its main street). It is the Chastain Lakes subdivision: name it
   "Chastain Lakes" in `neighborhoods.json` (a name override in `neighborhoods_rest.py`), its houses unchanged.
C2. Its amenity area on East Chastain Lake, between the lake, Bells Ferry Road and Lakeside Drive. Buildings 266 and 267
   (now 10.5 m office blocks, the tall brick and white boxes in BUG-025) are the pool house and the bathhouse. Dan: "there
   should be a parking lot in front of a pool, pool house and tennis courts", "on the right there should be a swinging bench
   in front of the lake, a gazebo in a deck on the water and there are always many geese here". From Dan's two pictures
   (`local/reference_photos/chastain_lakes/`, git-ignored): a rectangular lap pool with lane lines, a concrete deck, lounge
   chairs and a fence; the pool house (one story, hipped roof) between the pool and the courts, a small bathhouse on the
   pool's lake side; four tennis courts in two fenced pairs behind the pool toward Bells Ferry Road; a paved lot off
   Lakeside Drive in front of the pool, pool house and courts; on the lawn toward the lake a porch swing on a frame facing
   the water; a walk from the lot down to a pier and a gazebo on a deck out on the water; a flock of geese on the lawn and
   the shore (the Glen pond's geese, they scatter). The lawn north of the lake along Bells Ferry Road has a paved walking path
   on the shore with a small wooden footbridge where it crosses the stream; Dan: under that bridge and further up the stream
   are the two pipes, the network's main entrance BF (Round 23). Keep BF and its stream as they are; the bridge only spans
   the stream. The pictures are Google imagery: look at them for what is there, never trace or measure them (CLAUDE.md
   licensing); place things from OSM, NAIP and the terrain. Pool house and courts are world, not game layer. Check: Dan's two
   spots, a picture each, and one from the lot toward the pool.

For Dan to check (when done): Canton Road's storefronts and lots, the pizza / comic book corner, the ice cream shop, the
church, the elementary school sign, the grass spots, the junction at BUG-010, Chastain Lakes' pool area.

**Round 32 status (Claude, 2026-10-15): all built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by
Dan. Build 2026-10-15 20:39.** Evidence: `Data/screenshots/round32/` (a picture from each of Dan's spots, `<bug>.png`; overheads `lots_*.png`).
A1 DONE. 46 Canton Road businesses face it (`MapBuilder.Round32.cs`): a walk along each face toward it, a lot out to 3 m short of the
   road (stalls, curbs, poles; neighbors' lots joined into one), a drive where no service road joins it already (13), service roads in
   a lot paved as the lot. Lots never cross a house, a street or a store in front. Check: Dan's spots below, `lots_*.png`.
A2 DONE. 7, 8 to 10, 12, 3, 88, 20 / 40 face Canton Road with lots (`bug005` to `bug008`, `bug022`, `bug023`; `front_12_from_canton`,
   `front_88_from_canton`: from Dan's BUG-007 / 017 / 022 spots you now see their backs).
A3 DONE. 76 is storefronts on every face toward its lot; the corner is 76's own northwest corner (not 77): Lucky Slice Pizza with a
   drive-up window and lane on its side, then Panel & Page Comics, then Hometown Hardware (`bug019`, `bug020`, `pizza_driveup_side`).
A4 DONE. 86 is Cold Spoon Creamery, a storefront and a walk-up window on its lot (`icecream_86_front`, `bug014`, `bug015`).
A5 DONE. 74 is three gabled wings, the door, sign and one steeple at the north wing's east end toward Canton Road; 75 three gabled
   blocks, no steeple (`bug021`, `church_74_steeple`).
A6 DONE. Road 564623219 runs on into the preschool's lot (`bug009`); 41 is Pine Hollow Elementary, 0 stays the middle school (`bug018`).
A7 DONE. Roads 768422193 / 768422194 lead to a lot paved between them (from NAIP), neither ends in grass (`bug016`).
B1 DONE. Cause: every road ribbon had only its two edges on the ground, so the ground under the middle showed through. Every road is
   now a grid on the highest ground near each vertex (Round 11's Glen method); a service road's cells on another road are left to it
   (one shade at mouths). Check: `bug010` to `bug013`, `bug018`, `bug024`.
B2 DONE. Cause: cars across each other's way in a junction each waited for the other forever. One at a time, the first takes its turn
   (`TrafficDriver`, `Traffic.TakesTurn`). Check: Heavy, 5 min at the junction: longest stand 1.3 s (`round32_junction.txt`).
C1 DONE. "Chastain Lakes" in `neighborhoods.json`. C2 DONE (`MapBuilder.ChastainLakes.cs`): 266 pool house, 267 bathhouse, a 21 m
   lap pool, deck, loungers, iron fence, four courts in two fenced pairs, the lot, swing, pier and gazebo, 18 geese, the shore path
   with a footbridge over BF's stream (BF untouched). Check: `bug025`, `bug026`, `lakes_lot_to_pool`, `lakes_courts`.
For Dan to check: the storefronts and lots along Canton Road, whether the corner shop he meant is 76's corner (A3), the church, the
footbridge's look, the courts' places (from NAIP), traffic at junctions. Frame rate not measured. Imported from Racer: nothing.

## QUEUED rounds
None.

## Previous delivery 1 (newest): Round 31
**Reviewed: driven by Dan 2026-10-15 16:55 to 17:11 (his notes are Round 32).**
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

**Round 31 status (Claude, 2026-10-15): all four built and checked in the exe by script (minimized 1280 x 720, muted), not yet
driven by Dan. Build 2026-10-15 16:35.** Evidence: `Data/screenshots/round31/`.
1. DONE. BUG-002's building is a 131 m garage row on ground rising 6 m along it. It is stepped into segments, each with its own
   floor, and has a block foundation where it stands over the ground (`MapBuilder.Businesses.cs` SettleBody). The same rule applies
   to every long dressed building on a slope (12 stepped). Check: `bug002_dan_spot.png`.
2. DONE. The grass was the seam between New Chastain Road's westbound carriageway and its turn lane onto Bells Ferry Road
   (6.3 to 8.4 m apart). The seam is paved over 79 m (`MapBuilder.Round31.cs`). Check: `bug003_dan_spot.png`.
3. DONE. mapgen now keeps OSM's 18 `motorway_link` pieces of this interchange, appended after the other roads so no index
   moves. Width is 6 m or more; parking lots are kept too. Terrain changed only at x 156 to 532; other roads, buildings,
   bridges and trees are unchanged. A ramp overlapping a road it runs alongside takes that road's height (mapgen). The step
   where the NB ramps meet Chastain Road is smoothed (RampSeams). Traffic drives them (561 road links, was 541).
   Check: each ramp driven flat out at 45 m/s in the Drifter (`ramp_drives.txt`). All four end on the road; most tilt 9 deg.
4. DONE. 276 buildings dressed by OSM type (`MapBuilder.Businesses.cs`): shops 37 (storefronts, awnings, made-up names from
   `Tools/handmade/make_business_signs.py`), offices 39, industrial 24, churches 4 (steeple), schools 3 (flagpole), the fire
   station, apartments 92, garages 29, sheds 37, canopies on posts 10. Untagged buildings within 90 m of Canton Road are shops.
   191 more houses outside every neighborhood get the Round 30 house kit, in the look of the nearest neighborhood. 9 OSM
   parking lots are paved, with 452 stalls and 59 breakable light poles; 268 trees. One picture per kind: `kind_*.png`.
   Frame rate at Canton Road by the shops (Round 30 method, standing, 1280 x 720, uncapped; not measured at 3840 x 2160),
   before -> after: traffic Off 368 -> 347, Normal 156 -> 128, Heavy 91 -> 78 (`fps_before.txt`, `fps_after.txt`).
Decisions: no parking lots were invented where OSM maps none (only 9 outside the dressed areas). Sign names are all made up.
For Dan to check: the four ramps. There are two small hops at full speed: 0.18 s where the NB entrance merges onto I-575
(x 252, z 1603), and 0.30 s on the SB exit just after it leaves I-575 (x 231, z 1423). Also check the dressed buildings'
looks, and the frame rate on Canton Road at 4K.
Imported from Racer: nothing.

## Previous delivery 2: Round 30
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

## Previous delivery 3: Round 29
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
