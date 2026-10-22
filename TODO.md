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

## QUEUED rounds
**Round 61, THE MAP EXTENSION, STAGE 2: THE COBB COUNTY AIRPORT (Dan, 2026-10-22 05:37: "I feel comfortable expanding this out" after
stage 1). Start after Round 60 when Dan says (he has to drive 60 first).** As Round 59 (in the archive / Previous delivery): extend west to
take in Cobb County International Airport (McCollum Field: its runway, taxiways, hangars and terminal) with a margin, the same way (new land
at negative x, nothing that exists moves), everything that uses the map extended, and the numbers before / after.
Dan also noticed Laura Lake in the new land (Round 59 calls it "a real quarry", far south-west, about 200 m deep at 1.5x): "Oddly enough I
had no clue about the laura lake which code refered to as a quary. its a very cool area where we could have some really big jumps".
What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan.

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 59
**Reviewed: Dan drove the Round 59 build 2026-10-22 05:11 (free roam in the new land): "I didn't notice any frame rate issues"; his notes are Round 60.**
**Round 59, THE MAP EXTENSION, STAGE 1: TOWN CENTER AND KENNESAW STATE (Dan, 2026-10-22 01:20: "can we possibly also add on the first
stage of expanding the map out to what we had written up the other night?"). START RIGHT AFTER ROUND 58 (Dan said go).** One map
rebuild at the end (plus whatever the new data import needs). Dan is not watching: decide, write it down, go on; stop and report only
for something that cannot be undone or if the world becomes unplayable.
What was written up (2026-10-19, Dan's map `local/reference_photos/extension/town_center_ksu_airport.jpg`, Google: reference only, never
traced): extend the map west and south to take in Town Center at Cobb (the mall and the shopping around it) and Kennesaw State
University's Kennesaw campus. NOT the Cobb County airport (stage 2, later). Today's bounds: south 34.0294, north 34.0477, west -84.566,
east -84.5234 (3983 x 1918 m). Claude's estimate for stage 1: about 6.3 x 4.2 km (about 26 km2, 3.5x). The 1 m USGS tile already in use
covers it (to 33.955 S, -84.619 W). Code sets the exact new bounds from OSM (the mall's ring road and the campus with a margin; the
north and east edges stay) and says what they are.
1. KEEP THE EXISTING WORLD WHERE IT IS. Every existing coordinate (hand-placed houses, tracks, routes, shortcuts, pipes, eggs, records,
   replays, Dan's bug positions, CLAUDE.md notes) must still mean the same place: the new land gets coordinates outside the old range
   (e.g. negative x / z, or an offset applied everywhere consistently with every data file converted and checked). Code picks and says
   why; nothing hand-built may move or be lost. Every race still runs as before (lap lengths, checkpoints, records unchanged).
2. THE NEW LAND, GENERATED AS THE REST WAS (OSM roads, buildings, water, woods; USGS terrain at 1.5x): roads and their junctions,
   buildings from their footprints (the mall, the stores, the campus buildings and stadium as big generic buildings; no hand polish),
   parking lots, trees, streams and ponds, bridges where roads cross water. I-75 and I-575 and their interchange through the new area:
   drivable, overpasses where roads cross them, as well as the generator can do (Dan expects highways to be the weak spot; no heroics).
   The old west edge (I-575's collapsed overpass, Round 9) is now inside the map: join it up.
3. EVERYTHING THAT USES THE MAP covers the new land: traffic and police road nets, the map screen and minimap, exploration fog and
   destinations (new ones: Town Center, KSU), fast travel, free roam spawns unchanged, the quick-rebuild areas, the loading screen's inset.
   No new races or eggs this round.
4. MEASURE (Dan wanted this before deciding on stage 2): full Build Map time, exe size, load time, memory, and frame rate at 1080p in
   the old neighborhood and in the new area (Town Center, KSU, the interchange), before and after; tree and building counts. If frame
   rate or load time gets much worse, report it with the cause rather than tune for hours (tuning limit).
Check: every race still runs (one AI race per track); free roam drive from Chastain Glen to Town Center and to KSU without falling
through the world; pictures of the mall, the campus, the interchange, the old I-575 edge joined.
For Dan to check: drive to Town Center and KSU; the highways; the numbers (go on to stage 2, the airport, or not).

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round59/`.
1 DONE. The new land is west and south of the old map, at negative x / z (`Tools/mapgen/mapgen_extend.py`, config `extension`: 4 tiles west, 3 south). Why: nothing that exists has
   to change. The core is generated first exactly as before (map.json's lists, trees.bin's trees and terrain.r16 checked byte for byte), the new pieces are appended after it (house,
   road and bridge numbers unchanged), the terrain grid grows by whole core-size tiles (the same samples on the same spots, the core's pasted in, the new eased onto it over 40 m),
   heights on the core's datum (the 16-bit terrain's step is now 4.2 mm over the wider range: houses' floors moved up to 1 cm, `houses_settled.txt`). Unity reads `terrain_full.r16` (origin -2655.33, -1918); the python scripts keep reading the core's terrain.r16. Bounds (from OSM, KSU's and the mall's
   outlines with a margin, the north and east edges kept): south 34.01268, west -84.59575, north 34.04582, east -84.52292; 6638 x 3836 m (25.5 km2, 3.3x).
2 DONE. 2252 road pieces, 40 bridges, 3147 buildings (the mall, stores and campus as Round 31's dressed businesses: 1281 in all now), 43 ponds, 87 streams, 69 parking lots,
   87,614 trees. I-575 and its Chastain Road overpass cross the old south edge with no seam (bridge decks met within 0.2 to 0.8 m and eased); I-75 / I-575 drivable on the generated
   ground (a weak spot as Dan expected: plain decks where OSM tags a bridge, no hand work). A real quarry in the far south-west (floor 166.7 m, about 200 m deep at 1.5x).
3 DONE. Traffic and police nets (1602 traffic nodes, was 507), the map picture (8192 px wide, 0.8 m a pixel), minimap, exploration fog (Dan's saved fog renumbered onto the larger
   grid: on a copy of his save all 3660 cells and 11 places kept, the same ground), fast-travel destinations Town Center and KSU (`add_extension_places.py`), loading inset. Spawns unchanged.
4 Numbers, before / after (1920 x 1080 window, not measured at 3840 x 2160): Build Map 845 s / 1955 s; exe data 626 / 1357 MB; world up 6.5 / 19.4 s after start; Unity memory
   1065 / 1838 MB; terrain tiles 18 / 60; trees in the scene 54,943 / 142,282; buildings 2388 / 5535. Frame rate parked, average / slowest 1 %: old neighborhood (Canton Road) 182 / 67 ->
   122 / 39; Town Center 171 / 51; KSU 154 / 53; the interchange 158 / 53; the old I-575 edge 169 / 57. Not tuned (the round's rule): the old neighborhood lost a third, most likely
   the things spread over the whole map that run every frame whatever the distance (people groups 1765 -> 3512, hydrants 536 -> 1915, traffic network 3.8x) and more far objects drawn.
Check: every race still runs (11 courses and directions, one AI race each on Hard, traffic Normal: 33 of 33 finish, `round59_check_races.txt`); free roam Chastain Glen to Town
   Center (4.9 km) and to KSU (5.2 km) along the roads: through, never under the ground (`round59_check_drives.txt`); pictures of the mall, the campus, the motorways, the old edge.
Not done: the races and eggs were not touched (as the round says); neighborhoods_rest.py and the route scripts were not re-run (they would now see the new roads by name: see Technical notes).
`Builds/ChastainRush/` 2026-10-22 04:20. Imported from Racer: nothing.

## Previous delivery 2: Round 58
**Reviewed: Dan drove the Round 58 / 59 build 2026-10-22; no complaint about the AI or B to D.**
**Round 58, AI DRIVERS WITH AGENCY (recover, pass, never wrong way), and three fixes (Dan, 2026-10-22 01:20; reports BUG-001 to BUG-003
of session `2026-10-22_00-37-30-154_d7ca87`, read by Claude, after the Round 57 exe: Underground, Chastain Ridge and replays). THEN
ROUND 59 (the map extension, stage 1, below in QUEUED): Dan went to bed and asked for both; do 58, commit, then start 59 without
waiting.** Round 57 is reviewed. Dan is not watching: take the most reasonable reading of anything unclear, write it in the status, go
on (stop only for something that cannot be undone). Tuning limit. Short results.
A. THE AI. Dan watched the AI in replays: "I see some reasons why I beat them so badly usually":
   "1) I have seen them wreck, sideways on the bike lying down. Took them at least 5 seconds to restart themselves. I mean if you are
   your side, that should be an automatic reset
   2) Still saw them not putting much effort getting around cars
   3) Saw one run into a tree. I counted 10-15 seconds before they decided to reset. At the very least they could have tried to back up
   and get around the tree
   4) Literally watched one drive in reverse for a good stretch of the lap
   5) Saw one get in an accident, the drove off in the opposite direction for a bit before turning around
   Seems they have no agency to beat me. And this is is in hard mode I cant even imagine how bad it is in easy."
   For every AI racer, every track, every difficulty (difficulty changes pace and skill, never these recoveries):
   (1) On its side or upside down (a bike or rider down, a car over): reset at once onto the race line facing forward (well under a
       second once it is clearly down; no 5 s wait).
   (2) Passing: Round 55 E was "modest". Pass slower cars (traffic, parked, racers) decisively: pick a side early, use the other lane,
       the shoulder and the grass beside the road; take a gap rather than follow; a racer stuck behind a car more than about 2 s is
       the fault to remove.
   (3) Blocked (a tree, a wall, a car, a fence it cannot break): within about 1 s of no progress, back up and steer round it; if that
       fails within about 3 s more, reset. Never 10 to 15 s against a tree.
   (4) and (5) Never the wrong way: driving backwards along the lap, or forwards against it after a crash, is caught within about 1 s
       (heading against the lap's direction while moving, or reversing beyond a short back-up): turn round on the spot / three-point
       turn, or reset if that is quicker. Reverse only as part of (3)'s back-up.
   (6) Then Hard must race: measure, from the replays' recordings (Round 57 records every racer), per AI per race: time down / stuck /
       blocked / wrong way / behind a car, before and after (Hard, traffic Normal, every track both ways where it has both, at least one
       race each). Hard's lap times against Dan's best laps on each track (the Top 10 records): Hard should be within a few seconds of
       them; report the table. Easy and Normal keep their pace gaps but recover the same.
B. BUG-001 (x 1854.77 z 1132.96 h 87, THE HUB's floor, Underground race): "during the race, it still allows you falll down the pit of you
   are going to fastt, The gate should extend all th way down". Round 57 D's rails and gate still leave a way down: the gate / rails must
   reach all the way down (no gap under them to the pit) wherever the race passes the pit's edge. Check: flat out at every edge on the
   race's line and off it, all vehicles, none falls in during a race.
C. BUG-002 (x 1640.80 z 943.86 h 13, M5c, an aquarium pipe): "plants shouldn't be growing through the grass" (glass: the picture shows
   water plants standing up INSIDE the tube, through its glass and floor). Keep every aquarium pipe's plants, rocks and fish outside the
   glass (all aquarium pipes; Round 57 found the beds cutting tubes: same family).
D. BUG-003 (x 3082.26 z 708.71 h 270, Chastain Ridge Drive, Chastain Ridge race lap 3): "there is a bump somewhere around here that always
   messes me up". Find it on the race's line (both directions) and smooth it.
Check: A's table (before / after), and a replay of an AI recovering from each case (pictures or a short note of times); B, C, D as above.
For Dan to check: watch the AI in replays on Hard (recoveries, passing, no wrong way); race them; the hub's edges; M5c's plants; the Ridge bump.

**Status (Claude, 2026-10-22): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round58/`. Bug reports read: session `2026-10-22_00-37-30-154_d7ca87`, BUG-001 to 003.
A DONE (`RoadDriver` AgencyStep / PassLane; `-r58AgencyOff` gives the old driving). Down: put back on the line after 0.35 s. Blocked (wanting to go, not moving 1 s): backs up 1.1 s
   steering to the open side, crawls round; no 5 m of progress 4 s on: put back. Reversing: the brake pedal was reverse below 0.6 m/s, so a car rolling back after a knock kept
   reversing (Dan's "drive in reverse for a good stretch"); now it is on the throttle. Wrong way: past 100 degrees full lock (pure pursuit barely steered with the target behind:
   Dan's "drove off in the opposite direction"), put back after 3.5 s. Passing: either side, the shoulder and the grass, kept until past; behind a car 1.5 s the oncoming look shortens.
   By hand (`round58_check_parts.txt`): on its side going again in 0.9 s, turned round 2.9 s, rolling back 1.3 s, a trunk-size block in front 4.7 s (backed up once, round it).
   Table, Hard, traffic Normal, every course and direction, 33 AI races, from the replays (`r58_ai_before.csv` / `r58_ai_after.csv`): behind a car 342 s (48 spells over 2 s) -> 133 s (14);
   reversing 44 -> 7 s; stuck 163 -> 86 s (longest 3.4 s); down 9.7 -> 5.1 s; wrong way 26 -> 33 s (turning round after a crash, longest 2.1 s); 33 of 33 finish. The first
   "after" run found a driver backing up 185 times in S2's dive pipe (on a shortcut speed alone counted as progress): fixed, measured along the shortcut's line.
   Hard's pace (attempt 1 of the tuning limit; Easy and Normal unchanged): corner use .90 -> 1.0, brake 1.0, steep hills 42 m/s, crests 9.5. AI best flying lap against Dan's best:
   Glen fwd 39.6 / 38.9, rev 39.3 / 36.1; Ridge fwd 57.4 / 59.7, rev 70.8 / 59.2; Underground 65.6 / 75.5; Addison fwd 76.2 / 59.7, rev 69.5 / 69.0; Creek fwd 91.9 / 79.2,
   rev 92.4 / 69.5; Lakes fwd 111.4 / 72.1, rev 94.1 / 77.1 (Lakes is 2 laps: one flying lap each, noisy). Not tuned further: Creek and Lakes are held by their jump zones'
   set speeds and Dan's laps use shortcuts the AI takes only sometimes (the shortcut rule). Dan decides.
B DONE. Round 28 railed only the west and south-west of the pit; the race runs the whole mid-ledge ring and the span. Rails R3 to R7 (`make_underground_route.py`): the south
   and east edges to S1's floor ramp (left open: S1's way up), the north edge either side of the span, the span's two sides over the chasm (heights follow it, route `ys`).
   Four vehicles at 34 m/s into every edge at 90 / 45 / 20 degrees: 120 of 120 held, none in the pit.
C DONE. S2's aquarium (under the Enclave pond) had 22 weed clumps standing in M5c's tube (Dan's picture). Every glass stretch now knows the other tubes near it: weeds, rocks,
   logs and turtle spots keep out of them and out of its own tube on a bend; a fish inside another tube is not drawn (build log: 22 left out at S2, 1 to 5 elsewhere). Picture at his spot.
D DONE. The bump is the valley 50 m before his spot (the drive comes down at -13 %, a 10 m level shelf, then +15 % within 8 m): one vertical curve (as Addison's); the exe
   check also found a jolt 10 m after it at the loop branch's junction (x 3072): blurred. Flat out both ways, on the line and 3.4 m to his side: no jolt over 0.55 m/s, wheels down.
   Left: 3.4 m to the inside of the bend at x 3040, off the asphalt, still lifts a Drifter Twin's wheels (for Dan to judge).
`Builds/ChastainRush/` 2026-10-22 03:17. Imported from Racer: nothing.

## Previous delivery 3: Round 57
**Reviewed: Dan drove the Round 57 build 2026-10-22 00:37 (Underground, Chastain Ridge, replays): "The replays seem to work fine"; A to E no complaint; his notes are Round 58.**
**Round 57, RACE REPLAYS; fixes on Addison Heights and the Underground; COMPLETE RACE on the pad (Dan, 2026-10-21 21:35; reports BUG-001
to BUG-005 of session `2026-10-21_21-15-52-689_2c460b`, read by Claude, after the Round 56 exe: Addison Heights and the Underground race
on the Drifter Twin).** Round 56 is reviewed. Do A to E first (their one full Build Map, commit), then F (code only, no map rebuild).
Tuning limit. Short results.
A. BUG-001 (x 3279.17 z 308.15 h 184, Embry Lane's foot where it turns onto Suholden Circle, Addison Heights): "flatten this corner".
   Smooth it (grade, crown and camber through the turn) so it rides flat at race speed both ways.
B. BUG-002 (finish screen, Addison Heights): "a button doesn't complete the race". COMPLETE RACE (Round 54 C) shows the A glyph but
   pressing A on the controller does nothing (it worked from the keyboard later: BUG-005's results show "(estimated)"). Make the pad's
   A (and the keyboard key) press it; check both in the exe.
C. THE UNDERGROUND RACE: WEDGED, AND A BLOCKED TUBE. BUG-003 (x 1642.07 z 950.60 h 188, pipes "M5c M5 Enclave Main: middle, 29 m", lap 1,
   CP3 next): "somehow I got wedged here" (the picture: the screen all dark green, the camera inside the pipe wall). BUG-004 (same spot,
   turned round, h 8): "the tube is completley blocked" (the picture: an AI rider on an ATV stopped in the pipe ahead). Find why a car
   wedges at this point of M5c (a seam, a step, a narrowing, a joint between pipe pieces) and fix it; find why the tube is blocked (the
   stopped ATV, or geometry) and fix that: the AI must never sit stuck in a pipe (recover / reset as on roads). Keep the camera out of
   the pipe wall. Drive M5c both ways at race speed on all vehicles, no stop.
D. BUG-005 (x 1854.67 z 1132.51 h 92, THE HUB's floor, Underground race): "this is not enough to prevent yopu from going over the railing or
   under it". The hub's railings must hold a car: solid down to the floor (no gap under) and high enough not to be jumped or climbed at
   race speed (or a wall behind them), all round where the race passes. Check: drive at them flat out from the race line, none get through.
E. THE ELAINE SHORTCUT AND THE REST OF ROUND 56: accepted, no change.
F. RACE REPLAYS (new, not in Racer). Dan: "can we go ahead and create race replays? Would want it to automatically replace top 1 laps
   and races with the ability to save any race you want." (Earlier, 2026-10-20: "be able to watch your race you just ran and move the
   camera around differently".)
   (1) RECORD every race: every racer (player, AI, split-screen, police in the race) with position, rotation, steering, wheel spin and
       suspension, lights, rider animation state; plus the world events a race causes so they play again at the same moment: breakables
       (mailboxes, fences, hydrants and their water, signs, gas pumps exploding, barricades), geese scattering, manhole plumes, bounce
       house / trampoline bounces, shortcut barriers, traffic cars, and the sounds (engine, impacts, horns, the race's sounds). The race
       radio / music is not recorded (the replay plays the radio live, or none: Code picks).
   (2) WATCH: on the finish screen, WATCH REPLAY (pad glyph and key). Controls: play / pause, rewind, fast forward, slow motion, jump to
       a lap, restart; HUD off by default (toggle). Cameras: the chase camera on any racer (switch racer), the free camera (Trailer
       Mode's free look), and TV-style roadside cameras placed along the course that cut automatically as the racers pass (the fun way to
       watch a jump or a shortcut). All controllable with the controller.
   (3) KEEP: automatically, per track and direction, the replay of the #1 TOP 10 lap and of the #1 TOP 10 race; a new #1 replaces the old
       one. The best-lap replay plays just that lap (the others as they were then). SAVE: from the finish screen (and from the replay
       viewer) the player can save any race replay to keep, named by track, direction, date and time.
   (4) A REPLAYS page in the menus: the kept #1 lap / race per track and the saved ones; watch or delete (the #1s cannot be deleted
       from there, only replaced). Replays live beside the records in the save folder; report the size of one 3-lap replay.
   (5) If the map, a track or a shortcut changes later, an old replay may no longer fit the world: mark replays with the build / track
       version and say on the page if one is older (still playable, the world may differ). Records keys as the records use.
   Check (exe, minimized, muted): race Addison Heights forward 3 laps (with the Lyndon jump, a smashed barricade and a mailbox), finish,
   watch the replay: positions match the race within a small error (report it), the breakables break on time, each camera works, the
   controls work by pad and keys; beat a lap and a race record and see the #1 replays replaced; save a race and find it on the page.
   Pictures: the replay from each camera kind, the page.
For Dan to check: the Embry / Suholden corner; COMPLETE RACE with A; the Underground's M5c and the hub's railings; watch a replay with each
camera, then try the #1s and save one.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round57/` (check reports, pictures).
Bug reports read: session `2026-10-21_21-15-52-689_2c460b`, BUG-001 to 005.
A DONE. Embry climbs south at 10 % into the corner; Suholden west of it had been levelled flat, so the turn crossed a 0.45 m step on a diagonal. The turn's arc is now one
   vertical curve, 10 % into level over 40 m, flat across 5.5 m either side, then blurred (ground moved up to 0.71 m). Flat out both ways, Drifter Twin and Street Classic:
   all wheels down, biggest step in vertical speed 0.09 to 0.13 m/s (`round57_check_a_d.txt`, `57_A_*`).
B DONE. The cause: the button listened to the menus' confirm action, which the UI module switches off whenever the menus are closed, so neither A nor Space
   reached it (the "(estimated)" times on BUG-005 came from the 60 s wait). It has its own action now. Pad A and Space each complete the race (`round57_check_b_f.txt`).
C DONE. The wedge and the blockage were S2's glass bed (Round 46): S2 dives under M5c and its bowl's bed cut through M5c's tube in two bands 10 m apart (Dan's
   green screen is that bed). The bed is kept under the tube now (build log: 0 bed triangles inside another tube). The AI was put back between the two bands
   over and over: when it gets stuck within 15 m of its last stuck spot, it is now put back 20 m or more further on. The pipe camera never goes past a wall.
   M5c flat out both ways on all 14 vehicles: through, 27 to 34 m/s at Dan's spot. One exception: the Skyfin Cruiser drops to 2 m/s at the check's own start,
   placed sideways on the bend out of J15, 56 m from his spot. Underground race, 3 AI on Hard: all finish.
D DONE. Rails R1 and R2 and the closed gate SG1 are solid from under the ledge up to 3 m, 0.6 m thick, with a low third bar. Four vehicles hit each one at 30 m/s at
   90, 45 and 20 degrees: none got over or under. The grille that hung 2 m up after S1's switch opened it (it looked like a rail you could drive under) now lifts
   4.6 m clear. B28's glass bed had the same fault as S2's, inside M2a: fixed at Dan's yes (follow-up below).
E no change.
F DONE (code only: no map rebuild, as the round says). Every race is recorded: racers 25 times a second (pose, speed, steering, pedal), traffic 10 times a second,
   and breaks, restores, trees, plumes, switch gates and gestures. Engines play from the replay; the radio plays live. Finish screen: WATCH REPLAY (X / V) and SAVE REPLAY (Y / K).
   Viewer: A play / pause, RT fast forward, LT rewind, X slow motion, Y camera (TV, chase, free), LB / RB racer, D-pad lap / restart / HUD (HUD off by default), View save, B leave.
   The REPLAYS page is on the title, free roam and race menus. Each #1 Top 10 lap and race per course and direction is kept, and a new #1 replaces it. The page marks a replay
   recorded on another build or before its course changed. Check, Addison 3 laps, 3 AI, Normal traffic: replay within 0.006 m of the race (416 samples); 12 of 12 breaks on time
   (mailboxes, lamps, the barricade and its restores); every camera, every pad and key control, save, the page, playing from the page: 19 of 19.
   #1s: slow kept, faster replaced, slower left alone (4 of 4). Size: 1.1 to 1.7 MB for a 4 to 5 minute 3-lap race with Normal traffic.
   The Lyndon jump was in an earlier run's replay; in the final run the check's driver got stuck in traffic and was put back past it.
Follow-up (Dan's yes, 2026-10-22): B28's glass bed under West Chastain Lake had cut through M2a near x 1295 z 1164 (0.07 m from M2a's axis). It is now kept
   under M2a like S2's (`NetGlassAvoid` S2, B28). Build log: 8 bed triangles still flagged, but all are 0.10 to 0.32 m OUTSIDE M2a's wall (the check counts anything
   within 0.35 m); none is inside the tube. Free roam, M2a 70 m either side, flat out, both ways, all 14 vehicles: through, 30 to 38 m/s at the spot, slowest 18.9 m/s;
   the only touch of B28's bed is a flat floor contact with no speed lost (`round57_check_m2a.txt`; `-exeRound57 <dir> -r57Only m2a`).
`Builds/ChastainRush/` 2026-10-21 23:39. Imported from Racer: nothing.
For Dan to check: the corner both ways; COMPLETE RACE with A; M5c and the hub's rails; M2a under West Chastain Lake in free roam; a replay with each camera, the #1s, save one and find it on REPLAYS.

## Technical notes (standing)
Technical Notes / Blockers

* **The map extension (Round 59).** `python Tools/mapgen/mapgen.py` makes the core (config `bounds`) exactly as before, then, with config `extension`, appends the
  new land (`mapgen_extend.py`): negative x / z, map.json `extent_m` [x0, z0, x1, z1] and `extension.first` (where each list's new entries start), trees appended to
  trees.bin, `Assets/Generated/terrain_full.r16` / `.json` (origin_x_m / origin_z_m) for Unity, `Data/heightmap_ext_1m.tif`. The core's terrain.r16 / terrain.json stay
  for the python scripts. Caches in `raw/`: `osm_ext1.json`, `naip_ext1_*.tif`. Then `python Tools/mapgen/add_extension_places.py` (Town Center and KSU destinations into
  eggs.json; re-run after make_eggs.py). Scripts that pick roads by NAME (route scripts, neighborhoods_rest.py) would now also see the new land's roads (Chastain Road
  runs on west): when re-running one, keep it to `roads[:extension.first.roads]` or check its output's diff. Round 59 check: `ChastainRush.exe -exeRound59 <dir>
  [-r59Only load,fps,drive,pics59] -r59Drives local/round59/r59_places.json` (from `python Tools/mapgen/ext_drives.py`); the races: `-exeRound58 <dir> -r58Only aitable`.

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
