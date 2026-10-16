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
**Round 34 (Dan, 2026-10-16 02:05; reports BUG-001 to BUG-010 of session `2026-10-16_01-35-35-872_a132c0`, read by Claude,
after driving the Round 33 exe, the Chastain Ridge race).** Round 33 is reviewed. Dan is asleep while this runs: a long round
is fine ("it can be pretty long"); do not stop for design questions, decide and say what you decided in the status. Parts A
to C in order; rebuild into `Builds/ChastainRush/` and commit after each part; short results (CLAUDE.md "Process").

Part A, DAN'S REPORTS (Dan's spots are the vehicle's x, z, heading).
A1. BUMPS on Canton Road Northeast's northbound lane, the race's line, where an unnamed 5 m road lies along / over it (a
   lighter slab of asphalt in the lane): BUG-001 (x 3578.24 z 676.43 h 20, "flatten bump"), BUG-002 (x 3575.74 z 744.73
   h 356, "another bump"), BUG-003 (x 3567.94 z 818.99 h 353, "another bump"). Flatten them so the lane is one smooth surface
   at speed, at the three spots and on that same unnamed road's stretch between them. Check: drive the stretch flat out on
   the Drifter Twin, no bump-log event from z 650 to 840.
A2. GRASS: BUG-004 (x 3324.51 z 1038.83 h 171, unnamed 5 m road off Chastain Corner, beside S1's barricade): "remove grass
   from road". Check: a picture from Dan's spot.
A3. DRIVEWAY: BUG-010 (x 3325.65 z 1024.61 h 137): the same unnamed 5 m road stops short of the house ahead. Dan: "assume
   this is a driveway to the house. Please take the driveway to the house." Run it on to the house's garage / front. Check:
   a picture from Dan's spot.
A4. NO STALL LINES ON SLOPES. BUG-005 (x 3556.21 z 673.26 h 359): "can we not put parking spaces on hills? It just looks
   weird. It can be a parking lot just remove the lines"; BUG-006 (x 3585.47 z 641.98 h 159), BUG-008 (x 3558.24 z 348.95
   h 200), BUG-009 (x 3561.59 z 209.83 h 230): the same. Dan said it four times, so as a rule: a lot's stall lines are
   drawn only where the lot is near level (pick the grade, about 5 %, and say it); steeper parts of a lot stay paved with no
   lines. Check: a picture from each of the four spots.
A5. STRAY WHITE LINES: BUG-007 (x 3571.53 z 585.98 h 177, on Canton Road): "lets clean up these weird white lines". Edge and
   lane lines of the roads and lots that meet Canton Road there are painted across Canton Road's lanes. As a rule: a road's
   or lot's paint stops at the edge of the road it joins; only the through road's own paint crosses a junction. Check: a
   picture from Dan's spot.
A6. THE FIRST CROSSING ENDED LAP 1 (found by Claude in Dan's screenshots, not a comment of his): in the Chastain Ridge race,
   Dan stopped at BUG-001 21 s after the start, 24 m short of the line (the grid is on Canton Road behind it); when he then
   crossed the line, lap 1 counted as done with all 6 checkpoints missed (BUG-002: "Lap 2 / 3", "Penalty +30 s (6 missed)"),
   and its 54 s set the route's best lap (0:54.391), and the best total (3:29) holds that short lap too. Fix: from the grid,
   the first crossing of the line starts lap 1 and never ends one (look at why: the grid / lead-in lies on the loop itself
   before the gate). Check the other three routes for the same fault and say. Remove Chastain Ridge's best lap and best total
   from Dan's records (back the file up first, beside it; nothing else in it changed). Check: from the grid, cross the line,
   the HUD says lap 1, no penalty.

Part B, THE RADIO KEEPS ITS SONG WHEN THE GAME LOSES FOCUS. Dan: "sometimes something takes the focus away from the game and
every time click back to the game it skips to the next song. Let it stay on the same song." Losing and getting back focus
(Alt-Tab, a pop-up, a click on another window) never changes the song: it pauses with the game and goes on from the same
place in the same song when focus comes back (or keeps playing if the game keeps running unfocused; Dan's own exe pauses).
Same for the game's own pause menu. Only the player's controls and a song's real end change the song. Find why it skips
(likely the radio reads a stopped AudioSource as "song over"). Check by script in the exe: mid-song, lose and regain focus
five times (and a pause / unpause): same song each time, position within 1 s of where it was. (Sound on only for that
moment, then off: CLAUDE.md "Test quietly".)

Part C, TOP 10 RECORDS FROM WOODSTOCK RUSH. Dan: "Perhaps we can pull in top 10 records as well this round?" Port Racer's
records boards (RecordBoards, RecordView, PlayerNames and what they need, e.g. Racer's on-screen keyboard if it is how a
name is typed with a controller; CLAUDE.md: prefer porting, list what came):
C1. A Top 10 of best totals and a Top 10 of best laps for each race route (all four) and each direction it offers, with the
   lap count as Racer keys its boards, each entry
   with the player's name, time, vehicle and date as Racer shows them.
C2. The player's name as in Racer: asked once (or set in the garage / settings, wherever Racer has it), controller first
   (CLAUDE.md), saved; a new Top 10 time at the finish shows its place on the finish panel as Racer does. AI racers' times go
   on the boards only if Racer puts them there.
C3. A Records page in the menus to browse every board (left / right changes the route / direction, B back), as Racer's.
C4. Dan's existing records (`race_records.json`, minus A6's two) carried onto the boards with his name once he gives it
   (until then as Racer shows an unnamed entry). Saved the way Racer saves (never corrupt; loading never writes).
   A check never writes Dan's real records (CLAUDE.md): check runs use their own folder.
Check in the exe by script (a virtual pad): enter a name with the controller, finish a 1-lap race, the time is on the board
at the right place, the Records page shows every board; pictures of the name entry, the finish panel and a board.

For Dan to check (when done): Canton Road's lane at speed, the lots on slopes, the white lines at BUG-007, the driveway,
lap counting from the grid, the radio after Alt-Tab, his name and the Top 10 boards.

**Round 34 status (Claude, 2026-10-16): built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.
Build 2026-10-16 03:36.** Evidence: `Data/screenshots/round34/` (`round34_check*.txt`; `A_bug*.png` from Dan's spots; Part C `34_*.png`).
A1, A2 DONE. Cause: service roads ending at Canton Road's / Chastain Corner's centerline levelled flat strips across the lanes (steps of
   0.3-0.4 m). Mapgen `road_flatten.wider_wins_rects` (those two places only): a road's level core keeps its height where a narrower road
   ends on it. Canton's worst sag 0.33 m before, 0.02 m after; 40 m/s from z 630 to 860, no bump-log event; Chastain Corner no grass.
A3 DONE. Road 564629063 runs on 20.3 m to building 2158's wall (`MapBuilder.Round34.cs`; OSM calls 2158 retail, not re-dressed).
A4 DONE. Decided: stall lines only where the stall's ground is at most a 5 % grade corner to corner and its middle within 8 cm
   (`StallLevel`), on every business lot: 1,848 of 2,552 stalls now unpainted. Shopping center and Chastain Lakes lots (graded) unchanged.
A5 DONE. Rule: pieces of one street share paint only where one runs straight on from the other at the same width; otherwise a road's
   paint stops on the road it joins (`RoadIndex.CoversPaint`). Canton Road's carriageways no longer paint an X at BUG-007.
A6 DONE. Cause: any forward crossing of the line ended a lap; back over the line and across again counted lap 1 with every checkpoint
   missed (no log of Dan's run; his 54 s lap fits). Now only after a checkpoint or the lap's far half (`RaceEntrant.LapAway`). Shared code:
   all four routes had it and are fixed; none starts past its line. Check: clock starts at the line, lap 1; back and across: still lap 1.
   Dan's `race_records.json`: the Chastain Ridge entry removed, backup `race_records.before-round34.json` beside it.
B DONE. Cause: Racer's radio took any stopped voice for the song's end. Focus lost pauses the song, focus back goes on from there; a voice
   killed by an audio-device reset reloads the same song at the same place (`LocalRadio.Hold`). Check: 5 cycles and a pause, same song and
   position. (Racer has the same bug; not edited.)
C DONE. Racer's Top 10 per route and direction: best laps, best races (each lap count, shown together with a Laps column); name, time,
   vehicle, date; the player's times only (as Racer). Name asked once on the title menu with Racer's on-screen keyboard (A types, X
   deletes, Y space, Start saves, B cancels), changed in SETTINGS > GAMEPLAY; finish panel shows Racer's cards (TOP 10 TIME! #n ...);
   RECORDS on the title, pause and race-over menus (left / right: route and direction, laps / races, vehicle; B back). Dan's bests carried
   as legacy entries shown as "Player" until he gives his name; nothing written until his first new time or name (`top-ten-v1.json`,
   AtomicSave, an unreadable file never overwritten). Check 11 of 11 with a virtual pad in the check's own folder (run `-r34Only records`
   on its own: after the lap part the boards are already open and its test bests are not read, 2 C4 fails in the all-parts run).
Imported from Racer: RecordBoards, PlayerNames, FinishPresentation (`Race/RecordBoards.cs`, trimmed); RaceMenus' Records page, table rows and
   on-screen keyboard (rewritten into `Menus/GameMenus.Records.cs`). Left behind: Describe, RecordView, Migrate, Adopt, the InputField draft.
For Dan to check: the lane at speed, the lots on slopes (fewer lines: right?), BUG-007, the driveway, a race from the grid, the radio
   after Alt-Tab, your name, a new time on the boards. Seen, not changed: BUG-009's lot shows grass on its mound; 7 FenceChunk.Awake
   exceptions in every exe log. Frame rate not measured.

## QUEUED rounds
None.

## Previous delivery 1 (newest): Round 33
**Reviewed: driven by Dan 2026-10-16 01:35 to 01:51 (his notes are Round 34).**
**Round 33 (Dan, 2026-10-15 22:49): a new race on open roads, "Chastain Ridge", from Dan's sketch** (a Google Maps
screenshot with his marker lines, `local/round33/route_sketch.png`, git-ignored: Google, so look at it, never trace or
measure it; lay the course on OSM's roads). Dan: "How about this route... the dotted line would be a jump... i think this
is the highest point on the map". Round 32 is reviewed. Build it, then Dan drives it (CLAUDE.md); short results; rebuild into
`Builds/ChastainRush/` and commit.

The course (Claude read the sketch against `Data/map.json` and the heightmap; game heights; one lap about 1.9 km):
1. Canton Road Northeast NORTH from the New Chastain Road junction (x 3467-3579, z 390) to Chastain Corner Northeast (x 3507,
   z 1086). The sketch's arrows point north on Canton Road.
2. Left onto Chastain Corner Northeast (OSM 9139438), southwest and downhill (322 m to 304 m) to New Chastain Road (x 3152,
   z 760).
3. Straight across both carriageways of New Chastain Road onto Chastain Ridge Drive Northeast (OSM 1360406918, 1360406916,
   1360406917, 243965590), round its loop the way it runs: west along z 706, south up the ridge to the crest (334 m at
   x 3077, z 620), then east, downhill, to the drive's dead end at x 3152, z 632 (323.5 m). This is the high ground Dan
   means. (The map's highest ground is at its south edge by Canton Road, 367 m; this ridge is the highest on the loop.)
4. THE JUMP (the sketch's dotted line): off the dead end, east over houses 923, 706, 1231 and the hollow behind them
   (about 303 m at x 3200-3230, no stream mapped), down onto New Chastain Road's southeast-bound carriageway (OSM 9151940)
   at about x 3250-3290, z 560-610 (about 310 m). Part of the lap, not a shortcut: every vehicle the route offers makes it
   flat out from the crest; say in the status which vehicles it offers and why. Take-off dressed as something that could be
   there and whose real size fits (CLAUDE.md lesson: a dirt pile, a raised dump-truck bed, not hay bales or a flatbed if it
   is bigger than they are). Landing smooth onto the road, no step (the jump rules). Coming up short never traps: a way out
   of the hollow onto New Chastain Road, and a reset puts the vehicle on New Chastain Road past the landing, facing on.
   The houses are jumped over, not changed.
5. New Chastain Road southeast, climbing from 310 m to 326 m, to Canton Road, and left onto Canton Road: the lap.

Shortcuts (the sketch's two red lines; their arrowheads match that direction), built by the shortcut design rules
(CLAUDE.md: harder than the road, rewarding, a hint at the turn-off that could be there, no easier free way through):
S1. Top: off Canton Road short of the Chastain Corner junction, west past the restaurant (Tacos La Villa in real life) and
   the business lots onto Chastain Corner Northeast. Forgives what it skips.
S2. Bottom: off New Chastain Road before the Canton Road junction (about x 3330, z 470), east-northeast through the business
   lots between the churches (74 at x 3520, z 571; 75 at x 3526, z 523) and the shops, to Canton Road (about z 560).
   Forgives what it skips. Code finds each line and says in the status what each one jumps or threads through.

Race layer, as the other routes: `Data/routes/chastain_ridge.json` (a make_ script like the others), checkpoints (one just
past the landing), the grid, the lap's direction only (no reverse unless it works by itself), side roads closed by
barriers. Open roads: traffic stays on the course (route "traffic": true, Round 30 A3), so traffic drives Canton Road,
Chastain Corner and New Chastain Road during the race, and can be in the landing zone; the AI racers deal with it. The AI
take S1 / S2 by ShortcutStrategy and make human mistakes (CLAUDE.md). On the race setup menu's Route row; on the map.
Check: the jump flat out in each offered vehicle (lands on the road, how far, how high), one AI race of 3 laps with
traffic Normal (everyone finishes), a reset in the hollow. Pictures: the take-off from the crest, the flight, the landing,
each shortcut's turn-off.

For Dan to check (when done): the whole lap flat out, the jump, S1 and S2, traffic in the race.

**Round 33 status (Claude, 2026-10-16): built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.
Build 2026-10-16 00:55.** Evidence: `Data/screenshots/round33/` (`round33_check.txt`, 9 of 9 checks; pictures `33_*.png`).
Course DONE. `python Tools/mapgen/make_ridge_route.py` writes `Data/routes/chastain_ridge.json` (on the Route row and the map; "traffic": true)
   and `Data/tracks/chastain_ridge.json` (built by `MapBuilder.ChastainRidge.cs`). Lap 2072 m on OSM's roads, forward only; two-way roads raced on
   their right-hand lane; gate on Canton Road at z 700, CP1 Chastain Corner, CP2 down it, CP3 the ridge's crest, CP4 past the landing, CP5 New
   Chastain, CP6 Canton Road; 23 side-road barriers (none where the leap lands).
The Ridge leap DONE. A 2.8 m dirt pile at Chastain Ridge Drive's dead end (road-work fill, 14 m long: its real size), an aimed launch (32.9 m/s
   at 22 degrees for every vehicle onto the lip at 10 m/s or more, so all eleven vehicles are offered and all make it flat out): 3.5 s in the air,
   8.5 m over the lip, over house 706 (8 m clear; 923 and 1231 are beside the line, not under it; no house changed), down 106 m out within 2.5 m of
   the southeast carriageway's centerline, upright (`33_takeoff_from_the_crest`, `33_flight_chase`, `33_landing_chase`). Landing across the road at
   speed runs on into the woods east of it: those 55 trees snap when hit (2 of 11 stopped dead on trunks before). Short of the road: a ramp graded
   along the embankment out of the hollow (54 m, 12 %; driven out in 7.5 s), and a race reset anywhere from the lip to the landing puts the vehicle on
   New Chastain Road 120 m out, on the line, facing on (route `reset_skip`, `CarRecovery`).
S1 "Knoll hop" DONE. Off Canton Road west across the lot north of the canopy (4) and the shops (6), a dirt pile (2.6 m) onto the top of the knoll
   beside Chastain Corner, across it and off its west edge onto Chastain Corner: 1.3 s of air, saves 49 m, forgives CP1. Threads past 4 and 6, jumps
   nothing built. Hint: worn tracks and a knocked-over road-closed barricade; the pile is in sight from Canton Road (`33_S1_turn_off`).
S2 "Storage hop" DONE. Off New Chastain Road east up the bank, a dirt pile (2.8 m) over the self-storage garage row 63 (6.4 m clear), across the
   open ground and out between the churches (75 passed 5 m off) by their drive onto Canton Road: 2.0 s of air, saves 154 m, forgives CP5 and CP6.
   Hint: tracks and the barricade (`33_S2_turn_off`, `33_S2_pile`). Free cuts round 63 or through the lots miss CP5 / CP6 (5 s each).
Race DONE. 3 laps, 3 AI, traffic Normal: all three finished, 0 missed, 1 AI reset; S1 and S2 taken by ShortcutStrategy. Traffic keeps driving the
   course; only the 18 road pieces at the barriers are closed to it, and the grid is cleared at the start (`Traffic.Close`).
For Dan to check: the whole lap flat out, the leap (how the landing across the road feels), S1 and S2 and their hints, traffic in the race. Frame
rate not measured. Imported from Racer: nothing. Seen in passing, not changed: every exe log has 7 FenceChunk.Awake exceptions (also in Round 32's).

## Previous delivery 2: Round 32
**Reviewed: passed by Dan 2026-10-15 22:35, nothing to report.**
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

## Previous delivery 3: Round 31
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
