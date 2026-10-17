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
**Round 40 (Dan, 2026-10-17 12:31; reports BUG-001 to BUG-011 of session `2026-10-17_12-09-19-449_081d93`, read by Claude,
after the Round 39 exe: a reverse Chastain Ridge race on the Drifter Twin, finished 3rd, then Free Roam).** Round 39 is
reviewed, in part: Dan has not tried the police yet ("I didn't see any police so haven't tested them yet") and has not found
the X_SHOPS pipe again; both are still his to check, no change asked. Dan: "For now lets address these issues." Parts A to C
in order; rebuild into `Builds/ChastainRush/` and commit after each; short results (CLAUDE.md "Process"). Decide design
questions yourself and say what you decided. Look at each report's screenshot (`%USERPROFILE%\AppData\LocalLow\Chastain
Rush\Chastain Rush\DebugReports\2026-10-17_12-09-19-449_081d93\Screenshots\`; ignore its `small\` folder, Claude's copies).
Claude asked Dan three questions (12:35); his answers are quoted below.

Part A, THE REVERSE CHASTAIN RIDGE (all in the reverse race).
A1 / A2. THE CLIMB BECOMES A MANHOLE PLUME YOU CANNOT MISS. BUG-002 (x 3148.31 z 628.20 h 265, on Chastain Ridge Drive just
   past the climb's landing): "dont' like the way the ramp launches you. Lets be a little more creative, instead of using yet
   another dirt ramp (that isnt even a real jump), lets use a fire hydrant water plume instead. we put these ideas in this
   game, lets take advantage of them and put some variety in the game. Not everything needs to be dirt pile jump." BUG-001
   (x 3265.30 z 627.41 h 302, New Chastain Road just before the climb): "if this is the intended route, need to make it more
   obvious. It seems like you go straight here not take the ramp". Dan then changed the idea (12:45): "instead of a fire
   hydrant, how about instead we make it a manhole cover that blows up as the first driver gets close to it and you can't
   bypass the plume. This way it will look like you drive straight but won't matter because you can't miss the water plume".
   So: remove Round 39's road-work fill launch (its dirt, cones and barricade). In New Chastain Road's northwest-bound
   carriageway, on the race line where you drive straight on, a manhole cover (the street's own look, flush, nothing marks
   it). When the first racer (player or AI) comes within launch range, the cover blows off (Round 36 D1's bang, water, the cover
   flying) and a big water plume erupts; it keeps gushing for the rest of the race. The plume covers the whole carriageway
   (every lane and the shoulder, curb to median), so anything driving on along the road goes through it: there is no way past
   it on that carriageway, and no barrier or arrow is needed to steer you in. Driving into it throws the vehicle up and turns it
   toward the dead end (aimed like Round 36's geysers and the climb, whatever the line in): west over the hollow and house 706
   onto the dead end, landing as now on the forward pile's ramp face; every vehicle the route offers makes it at race speed,
   upright, a real flight, not a pop. A racer arriving before it has blown (the first one) still gets it: it blows in time for
   them. Traffic in the race keeps out of that carriageway's last stretch before it (or is not thrown), never stalls on the
   cover. Forward race: the cover never blows (the forward race is on the other carriageway). Free Roam: decide (for example it
   blows the same way when you come at it northwest-bound at speed and shuts again after a while) and say. Keep: the forward
   Ridge leap and its dirt pile exactly as they are (Dan: "Keep the forward leap"), resets short of the climb (on New Chastain
   Road before the cover), the hollow's way out. Check: on all 11 offered vehicles, from the left lane, the right lane and the
   shoulder, each thrown onto the dead end; the first racer's arrival with the cover still shut; pictures from Dan's BUG-001
   spot before and after it blows, the flight, the landing.
A3. BUMPY CORNER. BUG-003 (x 3473.41 z 409.57 h 222, New Chastain Road at the Canton Road junction, the course turning onto
   New Chastain): "fix this spot"; Dan (12:35): "bumpy". Smooth the turn on the race line through the junction. Check: driven
   flat out on the Drifter Twin, no bump-log event.
A4. THE FORWARD S2'S CLUES. BUG-004 (x 3352.09 z 461.53 h 313, New Chastain Road heading northwest): "need to clean up this
   shortcut here. This reads as a shortcut when it isnt going this direction". In the reverse race, hide S2's turn-off clues
   (its tracks and the knocked-over barricade) and anything else that marks it.
A5. NO REVERSE S1. BUG-006 (x 3458.88 z 1069.06 h 148, at the Knoll's pile off Chastain Corner): "lets not count this as an
   official shortcut this way". Remove Round 39's reverse S1 Knoll drop: it forgives nothing in reverse, and its reverse clues
   go. Forward S1 unchanged.
A6. NEW SHORTCUT AT THE CREST. BUG-005 (x 3091.70 z 616.18 h 358, on Chastain Ridge Drive at the crest, the houses ahead, CP4
   82 m on): "lets put a new shortcut here straight through to the next checkpoint. Just make an obstacle so it is a bit of a
   challenge but saves time if you dont crash". Reverse only: straight on from Dan's spot between / through the yards ahead
   to rejoin at or past CP4, forgiving what it skips. One obstacle that is a real challenge (not a jump; for variety: things to
   weave through, a gap to thread, something that moves), crashing costs more than the cut saves, getting it right saves
   time. Houses may be moved slightly if needed (say which); nothing more. A clue at the turn-off that could be there.
   Check: driven clean and crashed; the time saved.
A7. THE CHURCH SHORTCUT. BUG-007 (x 3569.10 z 605.67 h 235, Canton Road by the church, just past the start line): "Lets put a
   shortcut here....you can drive through the front doors of the church, wind you way up top and jump from the steeple area".
   Reverse only: off Canton Road southbound, in through the church's front doors (they break), a way winding up inside (ramps
   or stairs as ramps) to the tower, out of the steeple area in a big aimed jump, landing on New Chastain Road on the race line
   heading northwest (Dan: "New Chastain Road"), forgiving the checkpoints it skips. The church looks like a church outside;
   inside is new (simple, readable: pews, an aisle, the climb). Harder than the road and worth it. Check: driven through flat
   out on two vehicles; air time; pictures of the doors, inside, the take-off, the landing.
Check for Part A: one AI race of 3 laps reverse with traffic Normal, everyone finishes; the AI take A6 and A7 by
ShortcutStrategy with mistakes.

Part B, ROADS.
B1. LINES ACROSS THE ROAD. BUG-008 (x 3564.21 z 148.31 h 156, Canton Road, Free Roam): "These cross lines again in the middle
   of the road. Need to remove any that cross like this" (white lane / edge lines from a joining road painted across the
   through road's lanes). Dan asked for all of them: remove every painted line that runs across another road's lanes,
   map-wide; say how many and the cause.
B2. NEW CHASTAIN ROAD'S MEDIAN. BUG-009 (x 3057.61 z 760.38 h 312): "This median all down this road shouldn't be sloped like it
   is. The ground between the concrete barriers are relatively flat, though we should at least make it smoother. We can plant
   more trees through the medians as well". All along New Chastain Road: the grass between the median's curbs nearly flat
   across (level with the curbs, gently following the road's grade), smooth; trees planted along it (spaced, kept clear of
   turn lanes, crossings and sight lines at junctions; solid like the other trees). Check: pictures from Dan's spot and one
   other stretch.

B3. A HIDDEN COP ON CANTON ROAD. Dan (12:47): "I feel like there should be a hidden cop on canton road as well". Add hidden
   cruisers (Round 39 D's HiddenPolice) on Canton Road: one or two, parked where a speed trap would really sit (a business lot
   or a side street's mouth facing the road, half hidden), off the race lines, the stunt park, the shortcuts' lines and Round
   36's speed-trap camera posts; Free Roam only as the others. Say where. Check: one starts a chase at 10+ mph over on Canton
   Road.

Part C, BURIED HOUSES. BUG-010 (x 1883.40 z 645.06 h 251, off Chastain Trace): "unbury this house". BUG-011 (x 590.21 z 858.85
h 218, by an unnamed road off Chastain Road): "Raise this house". Set each on the ground (its floor just above the ground at
its highest corner, foundation showing on the low side as the others). Check: a picture from each spot.

For Dan to check (when done): the reverse Ridge (the manhole plume, the crest shortcut, the church, the corner), the lines
on Canton Road, New Chastain's median, the hidden cop on Canton Road, the two houses. Still his from Round 39: the police (POLICE on the title and Free Roam
menus; hidden cruisers on Bells Ferry, New Chastain and Chastain Roads and four neighborhood streets) and the X_SHOPS pipe.

## QUEUED rounds
None. Held by Dan (2026-10-16 21:34), not to start until he says: the CAMPAIGN ("I want to hold off on campaign until we get
more tracks ... we will have to keep reorganizing it otherwise"). (The reverse of Chastain Ridge, also held then, is Round 39 C.)

## Previous delivery 1 (newest): Round 39
**Reviewed: driven by Dan 2026-10-17 12:09 to 12:29 (a reverse Chastain Ridge race and Free Roam; his notes are Round 40; the police and the X_SHOPS pipe not yet checked).**
**Round 39 (Dan, 2026-10-17 03:30; reports BUG-001 to BUG-005 of session `2026-10-17_03-04-04-461_b78d3f`, read by Claude,
after the Round 38 exe: Free Roam on the Drifter Twin).** Round 38 is reviewed. Dan is asleep while this runs: a long round
("this can be a long round"). Parts A to D in order; rebuild into `Builds/ChastainRush/` and commit after each; short results
(CLAUDE.md "Process"). Decide design questions yourself and say what you decided; do not stop for them. Racer
(`C:\Users\danmo\Racer`) is read-only; port its finished code, list what came. Look at each report's screenshot
(`%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\DebugReports\2026-10-17_03-04-04-461_b78d3f\Screenshots\`).

Part A, DAN'S SPOTS (fix exactly where he stood; list similar places seen, do not change them).
A1. GRASS ON THE ROAD. BUG-001 (x 3460.23 z 1074.44 h 272, Chastain Corner Northeast, 2.3 m off its centerline, near the Canton
   Road junction): "have i not reported this grass before". Yes, the same kind of thing: Round 34 A2 (x 3324.51 z 1038.83, the
   unnamed road off Chastain Corner) and earlier on Canton Road (archive, "GRASS IN THE STREETS"). This spot is new. Find why
   the earlier fixes did not cover it (git history first) and fix that cause, not only a patch here; say what the cause was
   and if the fix changed other places. Check: a picture from Dan's spot.
A2. THE PIPE EXIT BEHIND THE SHOPS (X_SHOPS, "Exit Behind Shops", x 3153 z 1256). BUG-002 (x 3153.25 z 1254.19 h 150, at its
   mouth): "clean this up". BUG-003 (x 3163.74 z 1238.82 h 112, 3 m inside): "why does th wall stick out like this". Look at
   both pictures; make the mouth and the inside clean (no ragged ground, gaps or a wall standing out of the pipe's shape),
   the pipe still drivable in and out. Check: pictures from both of Dan's spots.
A3. UNEVEN ROAD ON CHASTAIN GLEN LANE. BUG-005 (x 3073.12 z 990.05 h 0, on Chastain Glen Lane, 0.15 m from its centerline):
   "what happened here? Extremely uneven here". This is the starter loop's road and it was smooth before: check git history
   for what changed it (Round 36 hydrants / manholes / geysers, Round 38 life or anything else near there) and restore the
   road (Standing rule 5: restore the known-good first). Check: a picture from Dan's spot; drive it flat out both ways on the
   Drifter Twin, no bump-log event.
A4. POOL AND TENNIS COURTS. BUG-004 (x 2678.77 z 1633.55 h 102, an unnamed 5 m road): "this looks like it should be a pool
   and tennis courts here. I have a screenshot for it". It is the swim and tennis amenity on Courson Street Northeast (the
   "Guidhall Trail" area, n4 in `Data/neighborhood/neighborhoods.json`); Dan's road is the service road OSM 829254243 off
   Courson Street into its lot; OSM's flat-roof buildings at about (2722, 1651) and (2712, 1676) are its clubhouse and pool
   house. Dan's picture is a Google Maps aerial (not in the repo; Google, so it is never traced or measured): place everything
   from OSM and NAIP. Claude's notes on what it shows: on the east side of Courson Street, the clubhouse (two storeys, gray
   gable roof, like the houses) beside the street; behind it, east, a fenced pool deck with lounge chairs all round, a large
   rectangular lap pool with lane lines and a smaller free-form wading pool joined at its east end; a small pool house on the
   deck's south side; south of the clubhouse a paved lot off Courson Street (a crosswalk at its mouth, about 30 angled stalls
   in two rows, trees in islands); a walking path curving from the lot east past the pool house and down to the courts; four
   tennis courts in one row (blue courts on a green surround, nets, one tall fence round all four) southeast of the lot, with
   two small blue shade canopies and benches beside them; woods to the east and south. Build it in the Enclave pool's and
   courts' style (Round 24, `MapBuilder.Communities.cs`): pool water, deck, fences, courts, lot, path; the fence and loungers
   breakable as elsewhere; Round 38's pool-deck people use it as the Enclave's. Check: a picture from Dan's spot and one from
   above.

Part B, MORE VARIETY IN THE PEOPLE (Round 38 E). Dan: "There are far too many people drinking coffee. The whole point of it
before was a specific scripted scene for my friend and I in Woodstock while growing up. I don't mind keeping but it
shouldn't be showing up so much. it would be cooler to have people walking around the neighborhood sometimes families and
sometimes people walking dogs. just at least some variety other than people just standing around drinking coffee."
B1. Coffee pairs become rare: a handful on the whole map (about 1 group in 40 at most), never two in sight at once. Fewer
   people just standing (Round 38 put 1,537 of 1,913 groups on porches); the standing ones do a mix of Racer's other actions
   (whatever Racer's AmbientLife has: kids playing, gardening, getting the mail, washing a car, and so on). Say the mix.
B2. People walking the neighborhoods: dog walkers (one person with one or two dogs on leashes; the dogs sniff, stop, pull),
   families (two adults with one to three kids, sometimes a stroller), couples, joggers. They walk along the streets of the
   Glen, Grasmere Court, the Commons, the Enclave, Chastain Lakes and the other neighborhoods (on a sidewalk where there is one,
   otherwise at the street's edge), turning at corners and walking on, not on a loop you can see repeat. More in the morning
   and evening, a few dog walkers after dark, nobody in rain or snow (LifeSchedule). Dogs are new: simple dog figures in a few
   sizes and colors, made the way the people figures are (a Blender script in `Tools/Blender`). They step out of a vehicle's
   way as the others do (Round 38: no colliders). In Free Roam they may walk any street's edge; during a race none within 5 m
   of the race line (as now). Same pool and culling as Round 38; frame rate on a Glen street before / after (1280 x 720).
Check: pictures of a dog walker, a family and a jogger in the Glen; counts of each kind on the map and of coffee pairs before /
after; a vehicle driven at walkers (they step aside).

Part C, REVERSE CHASTAIN RIDGE. Dan: "come up with a way to reverse chastain ridge" (held since Round 38; the problem is the
Ridge leap, which drops 13 m off Chastain Ridge Drive's dead end onto New Chastain Road and cannot be driven backward).
Claude's design, build it:
C1. The reverse lap: Canton Road SOUTH from Chastain Corner to New Chastain Road; right onto New Chastain Road NORTHWEST,
   downhill; THE RIDGE CLIMB (C2) up onto the dead end; Chastain Ridge Drive backward (west from the dead end, over the crest,
   north down the ridge, east along z 706); straight across New Chastain Road onto Chastain Corner Northeast, uphill; right
   onto Canton Road: the lap. Two-way roads raced on their right-hand lane for this direction. The same gate on Canton Road
   at z 700; the grid behind it on the southbound side; checkpoints in reverse order (one just past the climb's landing);
   the same side-road barriers.
C2. THE RIDGE CLIMB, the leap the other way, an UP jump: off New Chastain Road on the hollow's side, about where the forward
   leap lands (x 3250-3290, z 560-610), a launch west over the hollow and house 706 up onto the dead end. Land on the back
   (west-facing) slope of the forward leap's 2.8 m road-work dirt pile, which becomes the climb's landing ramp, or, if that
   slope is not long enough for the flight, grade a landing slope at the dead end sized to it (the forward take-off must still
   work: check both). An aimed launch like the forward leap's, so every vehicle the route offers makes it flat out; big air,
   over the same houses, none changed. Dressed as more road-work fill (its real size, CLAUDE.md lesson). Coming up short never
   traps: the Round 33 ramp out of the hollow (54 m, 12 %) leads back to New Chastain Road, and a race reset between the
   launch and the landing puts the vehicle on Chastain Ridge Drive past the landing, facing on (`reset_skip` for reverse).
C3. Shortcuts in reverse: S1 (Knoll hop) and S2 (Storage hop) the other way where the land gives each a take-off on its other
   side (the creek track's "every big jump has a back" pattern), built by the shortcut rules (harder than the road, a hint
   that could be there, no free easier way, forgives what it skips); where one cannot work in reverse, it is closed in a
   reverse race and the status says why.
C4. The route's `directions` gets reverse (Route row and the map, as the other routes); `ai_zones_reverse`; the AI take the
   reverse shortcuts by ShortcutStrategy and make human mistakes; traffic on the course as forward ("traffic": true).
Check: the climb flat out in each offered vehicle (height, air, landing upright on the slope); the forward leap re-flown once;
a reset short of the climb; one AI race of 3 laps reverse with traffic Normal (everyone finishes). Pictures: the climb's
take-off from New Chastain Road, the flight, the landing, each reverse shortcut's turn-off. Dan's forward records stay; the
reverse gets its own boards (as the other routes' reverse).

Part D, POLICE FROM WOODSTOCK RUSH. Dan: "Lets add police this round". Port Racer's police as Racer has it (sequel notes
section 4 "Police", `Docs/FROM-WOODSTOCK-RUSH.md`; Round 37 noted none of it is in this game): `GetawayChase` (AI cops chase
you: heat 1 to 5, backups placed ahead out of sight, exits covered when you go off-road, the escape meter, roadblocks, the
helicopter from heat 4), `CopDriver`, `RoadNet` (built from this map's roads, with this map's road and place names),
`PoliceChase` (Cop vs Runner, two players, split screen), `SpeedPatrol` (radar, catch speeders for points), `HiddenPolice`
(parked cruisers in Free Roam that start a chase if you pass 10+ mph over the limit; its setting), `PoliceHelicopter`,
`PoliceLights`, `PoliceRadio`, the Patrol Car and Patrol Cycle (`Tools/Blender/police.py`, `policebike.py`, their profiles),
Racer's police records / Top 10s. Leave behind what is campaign-only (no campaign here).
D1. Dan's rules from Woodstock Rush (sequel notes section 6): police modes are Free Roam only, never in a race; solo play never
   swaps roles; being chased by AI is Getaway; a chase starts immediately with a cop behind you; escalation (heat, backups,
   helicopter) is visible and announced. Put the modes where players look (a POLICE entry on the Free Roam menu: Getaway,
   Speed Patrol, and Police Chase for two; not hidden under Split Screen, Racer's mistake). Controller first, B back.
D2. This map: speed limits from OSM where tagged, otherwise by road type (say the defaults); hidden cruisers on the long fast
   roads and a few in the neighborhoods (say how many and where); cops never follow down into the pipe network: decide what
   going underground does to a chase (for example the helicopter and the cars lose sight, the escape meter fills) and say it;
   the stunt park, the creek trail and the shortcuts are fair ground for a runner (cops follow off-road as Racer's do).
D3. The radio voice. Dan (03:47): "I will create a different voice. You can give me new voice lines and a little app to run
   the lines for. Lets actually use street names instead of landmarks." So: no Sheriff John Paul, no landmark lines, and Code
   generates no audio. Claude wrote the lines and the app (in the repo, uncommitted until this round's first commit):
   `Tools/Audio/police-radio-lines.csv` (id, text, when, map_names): R01-R20 whole lines (Racer's, plus R19 / R20 for going
   into and out of the pipe network), P01-P15 phrase pieces that a STREET follows (P11-P14 "Suspect heading north / south /
   east / west on...", P15 "Suspect turning onto..."), U1-U7 units, S01-S08 Speed Patrol, H01-H04 hidden police, and D001-D136
   every named street on the map spoken without the Northeast / Northwest suffix (`map_names` lists the OSM names each one
   covers; motorways left out). No L (landmark) lines. `Tools/Audio/police_radio_studio.py` (`Police Radio Studio.bat`): Dan's
   app, which writes `<id>.wav` (mono, 24 kHz; extra takes `R05a`, `R05b`) into `SourceArt/Audio/PoliceRadio/` and copies each
   into the drop-in folder `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\PoliceRadio\`.
   The game: PoliceRadio reads these IDs (Racer's piece-joining kept: unit + phrase + street + ending); every place it would name
   is the STREET the suspect or unit is on or heading to (nearest named street from RoadNet / `map_names`; off-road: the nearest
   street ahead; the direction lines from the suspect's heading); it loads clips from the drop-in folder first, then the
   built-in `SourceArt` folder (no rebuild needed for new clips); a missing clip falls back to the call as on-screen text only,
   as Racer, so the round works with no audio yet. Do not change the CSV's IDs; fix a line's text only if the game needs it
   and say so. The radio voice plays alongside the music radio as Racer (music ducking), at a priority under it (Round 35 B).
D4. Split screen (Round 37): Police Chase for two, and Getaway / Speed Patrol for two as Racer has them.
D5. Racer's lesson (sequel notes section 8): Getaway "caught the runner in 114 s" with a scripted runner in the editor but
   never caught Dan in the built game. Check in the exe the way Dan plays: a human-like runner (the fastest road vehicle,
   flat out, taking shortcuts and off-road cuts); the cops must be able to catch him at high heat, and escape must be
   possible too. Say the result of each.
Check in the exe by script: a Getaway run from heat 1 to the helicopter (pictures: a cop behind at the start, a roadblock,
backups, the helicopter), one caught and one escaped; a Speed Patrol run scoring a catch; a hidden cruiser starting a chase
at 10+ mph over; Police Chase for two with two virtual pads; the radio calls shown as text with no clips, and one clip played from the drop-in folder if any exist (sound briefly on, then off).
Frame rate in a heat-5 chase at Canton Road (1280 x 720).

For Dan to check (when done): the spots from his reports, people walking dogs and families out, the reverse Chastain Ridge
race and its climb, Getaway / Speed Patrol / hidden cruisers / Police Chase in Free Roam, at 3840 x 2160.

**Status (Claude, 2026-10-17): Parts A to D built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round39/a` to `d`.
A1 / A3 DONE, one cause (git: Round 34's `wider_wins_rects` only levelled a narrower road ending on a wider one, in two boxes): a side street of equal
   width laid its own level cross-section into the through road's slope. Mapgen now tilts the last 8 m of every road end meeting a road that runs on
   through onto that road (`junction_warp_m`): 442 junctions map-wide, so other places changed (roughness humps 2,422 -> 1,758 samples, sharp steps
   1,493 -> 1,057). Chastain Corner: no grass. Glen Lane: flat out both ways on the Drifter Twin, wheels down, no bump-log event on the road.
A2 DONE. BUG-003: B21's bend folded back on itself (`dekink`), a clean tube. BUG-002: house 2175, 4 m off, kept the bank ungraded: X_SHOPS's
   wingwalls retain it, the ground behind the headwall rises to the house's base; driven in and out. Same kept ground, not changed: BF, X_W20, X_W11,
   X_BRIDGE, X_STREAM.
A4 DONE. Courson Street pool and courts from OSM: clubhouse, deck, lap and wading pool, pool house, 4 courts in one fence, 2 blue canopies, crosswalk,
   path, breakable loungers and fences, pool-deck people; the lot holds 21 angled stalls with its 2 tree islands (OSM's lot is 38 m: not 30).
B DONE. Coffee pairs 1,528 -> 34 of 1,770 groups, one out at a time. Porch groups at a third of the houses (1,537 -> 516). Mix: talking 517, kids
   397, phone 257, football 238, gardening 208, mailbox 115. Walkers on 210 streets (sidewalk where there is one): dog walkers (1 or 2 dogs: sniff,
   stop, pull; 3 sizes, 5 coats), families (a stroller 1 in 3), couples, joggers; 7 near you mornings, 8 evenings, 4 midday, a few dog walkers after
   dark, none in rain or snow or within 5 m of a race line; they step aside. Glen Lane 311 / 313 fps without, 293 / 309 with 7 groups out.
C DONE. Reverse Chastain Ridge, a lap of its own (2,039 m, 6 CPs, own record boards). THE RIDGE CLIMB: road-work fill (cones, a barricade) by the
   southbound carriageway, aimed at 31.4 m/s and 40 deg onto the forward pile's ramp face; resets short of it go past it. Reverse S1 Knoll drop (saves
   45 m); no reverse S2 (the garage row stands where its run-up would be). Climb on all 11 vehicles, forward leap on all 14, 3 AI x 3 laps finish.
D DONE. Racer's police, Free Roam only: POLICE on the title and Free Roam menus (Getaway, Speed Patrol, Cop vs Runner, 1 or 2 players), results,
   RECORDS Top 10s. Limits: OSM where tagged (113 roads), else interstate 65, ramps 45, trunk 55, primary 45, secondary 40, tertiary 35, street 25,
   service 15. Hidden cruisers: 8 on Bells Ferry, New Chastain and Chastain Roads, 4 on Hartley Woods Dr, Blackwell Cir, Suburban Dr, Crestmore Dr.
   Decided: cops never enter the pipes (down there you cannot be busted and escape twice as fast). Radio: CSV IDs, street names, text until Dan's
   clips exist. Checked: heat 1 to 5 with backups, a roadblock, the helicopter, caught on Hard; escape into the pipes; a Speed Patrol catch; a hidden
   cruiser at 68 in a 45; Cop vs Runner for two with roles swapped; radio text and a clip; heat 5 at Canton Road 111 fps (159 without). NOT shown: my
   scripted runner never got away on Easy on the roads (best 59 % of the escape meter in 7 tries; Easy was eased: 92 % cop pace, catch-up 1.08 x,
   seen within 90 m, 12 s hidden). Escape on the roads is for Dan to judge.
Imported from Racer: GetawayChase, CopDriver, RoadNet, PoliceChase, SpeedPatrol, HiddenPolice, PoliceHelicopter, PoliceLights, PoliceRadio, the Patrol
   Car and Cycle with their Blender scripts, the police menus and records (rewritten). Left behind: PoliceProgress, the campaign's police goals.
For Dan to check: Chastain Corner and Glen Lane, the X_SHOPS pipe, the Courson pool, walkers and dogs, the reverse Ridge and its climb, Getaway on
   Easy (can you lose them?), Speed Patrol, hidden cruisers, Cop vs Runner, at 3840 x 2160 (frame rates here are 1280 x 720).
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-17 10:31 (BUILD_INFO.txt); a final smoke run on it passed 7 of 7 (`round39/d/final_check.txt`).

## Previous delivery 2: Round 38
**Reviewed: driven by Dan 2026-10-17 03:04 to 03:19 (Free Roam on the Drifter Twin; his notes are Round 39).**
**Round 38 (Dan, 2026-10-16 21:34; report BUG-001 of session `2026-10-16_21-19-50-140_b75298`, read by Claude, after the
Round 37 exe).** Round 37 is reviewed (Dan: the split-screen AI player 2 is only for testing, "No one is going to be splitting
the screen with AI"; he missed START at first, no change asked). Dan picked this round from Claude's list: "AI stunt rivals,
Life in the world, More inside the stores, The Bank part of stunt course, Smashing through things should count for your
stunts". Parts A to E in order; rebuild into `Builds/ChastainRush/` and commit after each; short results (CLAUDE.md
"Process"). Decide design questions yourself and say what you decided. Racer (`C:\Users\danmo\Racer`) is read-only; port its
finished code, list what came.

Part A, THE BANK IN THE STUNT PARK. BUG-001 (x 3413.02 z 1095.12 h 73, the abandoned bank ahead): "Can we make this bank part
of the course as well because otherwise this ramp is kind of useless the way it is positioned" (Round 37 noted K1's foot is
4.7 m from the bank's west wall, no run-up). Bring the abandoned bank into the park: a way up onto its roof and stunts there
(a launch off it, or a gap to the next roof), a line through it (its boarded windows and door break), and K1 moved or turned
so it has a run-up and leads somewhere (onto or over the bank). The bank stays abandoned-looking (boarded, the FOR LEASE
banner). Check: K1 flown flat out on the Clay Hopper and the Skywhip, the bank's roof reached, the line through it driven;
pictures.

Part B, SMASHING COUNTS FOR STUNTS. Dan: "Smashing through things should count for your stunts". Wherever tricks score (Free
Roam and the Stunt event, every vehicle): breaking something (store and bank windows and doors, the shop row's partitions,
checkouts and shelves, the park's breakable props, fences, mailboxes, lamps, hydrants, pumps, signs, trees that break) adds
stunt points, bigger things more (Dan, 21:37: "fuel pumps should also count...": the gas station's four pumps count, and
   with their explosion they are among the biggest smashes), and links a combo like a trick (on the ground within the combo gap, it keeps the chain
going); a smash in mid-air counts as a trick type of its own for the flight's multiplier ("SMASH"). Shown on the trick panel
("SMASH: STORE WINDOW +150"). The smash runs (Round 36 B) keep their own scoring. Decide the points and say them. Check: a run
through the store windows into a jump, scored with the smashes in its combo; a mid-air smash raising the multiplier.

Part C, MORE INSIDE THE STORES. Round 36 A2b built the supermarket's inside run; the shop row's shops are empty shells
("Next could be: shop interiors, more inside lines"). Give the shop row interiors (simple, readable rooms that fit each
shop's made-up name: counters, racks, tables, chairs, that break) and lines through them: from shop to shop through the
partitions, a ramp up and out through the roof or a back wall, a kicker out through the front window into the park; and the
bank's inside (Part A) with its vault door to burst through. Same reset as the windows (they come back when the player leaves
or resets). Check: driven through end to end on a stunt vehicle; pictures; frame rate inside (1280 x 720) before / after.

Part D, AI STUNT RIVALS. Dan asked "is there the ability to play ai on the stunt course?" (now: no; the split-screen AI
player 2 is the traffic driver, which in BUG-001 "wandered off the course and is just continually running into the wall").
Port Racer's stunt rivals (`RivalTricks` in `StuntRace.cs`, ArcadeVehicle's AI air steering, the rivals' scoring by the same
rule as the player, the AI's mistakes and bails) to the stunt park: the Stunt event gets rivals (a row in its setup: None /
1 / 2 / 3, and difficulty), riding stunt vehicles through the park on lines Code authors (every big feature, the roofs, the
stores, the bank), doing tricks at the launches and smashing as the player does (Part B); live standings on the HUD and a
results table at the end (the player's Top 10 stays the player's own, as Racer). The rivals stay in the park, never out on
the roads, and follow CLAUDE.md "The AI is not perfect". The split-screen AI player 2 in the Stunt event and the Stunt park
uses this stunt AI too (fixes BUG-001's wandering). Check: a Stunt event with 3 rivals run in the exe: every rival scores, none
leaves the park or sticks on a wall; a picture of rivals mid-trick and of the results.

Part E, LIFE IN THE WORLD. Port Racer's ambient life to this map (as Racer has it; leave behind what is tied to Woodstock
places and Racer's scripted Dan / Kyle scenes): `AmbientLife` (residents in yards, driveways, on porches, at the pool at
Chastain Lakes and the Enclave, walking at the shops, doing their actions), `HouseholdSchedule` (who is out depends on the
time of day; fewer at night, none out in rain or snow if Racer does that), `Wildlife` (the kinds of animals Racer has that
live in Georgia suburbs and woods: deer, rabbits, squirrels, birds; in the woods along Noonday Creek and the green spaces),
`WoodlandAmbience` (the woods' sound), `UndergroundLife` (in the pipe network as Racer has it in its storm drain). Nobody and
nothing stands on a road, a race line, a jump or a shortcut; they react as in Racer when a vehicle comes close; never in a
race's way. Day / night and weather aware (Round 35). Same cost rules (a pool round the player, culled out of view); frame
rate at Canton Road and on a Glen street before / after (1280 x 720). Check: pictures of people in the Glen, at Chastain
Lakes and the shops; animals in the woods; a vehicle driving past them.

For Dan to check (when done): the bank in the park, smashing in your combos, the shop and bank interiors, a Stunt event with
rivals, people and animals around the map.

**Round 38 status (Claude, 2026-10-17): built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.**
Evidence: `Data/screenshots/round38/` (`abcd_check.txt`, `e_check.txt`, `38_*.png`, `park_layout.png`). Check: `ChastainRush.exe -exeRound38 <dir>`.
A DONE. The bank is the park's: hollow, boarded doors (west) and drive-up bay (east) that break, a line straight through it from the bus gap's run-out.
   K1 turned round as the BANK HOP: an aimed pop-up (17.2 m/s at 62 deg) 10.7 m from its wall onto its roof, 2.3 s up; the BANK DROP off the roof's north
   edge (16 m/s at 10 deg) onto a 38 deg landing in the lane between the canopy and the half-pipe. Clay Hopper and Skywhip: hop, roof, drop, landing upright.
   Decided: parapet 1.1 m (0.6 m on the hop's side); a berm along the east edge was tried and dropped (it threw a fast vehicle off the roof); the 5th
   container moved out of the bay's run-out.
B DONE. A smash links the combo like a trick (a burst within 1.2 s is one link), keeps the 5 s chain, and in the air is SMASH, a trick type for the
   multiplier (a smash in the last 0.3 s before a takeoff counts in the flight). Points: fence 25, furniture 60, mailbox / sign 75, display 80, shop wall /
   lamp / washer 100, counter 120, window / boarded door / floodlight 150, hydrant / teller counter 200, tree 250, cash cart 300, gas pump 600, vault
   door 1,000. Check: in at a window, through the shops, out of the pizza window: SM · SM · SM; with a backflip BACKFLIP + SMASH x2.
C DONE. 77 breakable things in the ten shops (each its own room: coffee tables, boxes, a wok counter and lanterns, salon chairs and mirrors, pizza
   counter, dental chair, clothes racks, flower tables, washers); the shop ramp (37 m, 16 deg) from the supermarket's north end up through a hatch
   onto the roof gap's run-up; the pizza window kicker (aimed, out through a 3.6 m window onto a sized landing); the bank's teller counter, vault
   (round door) and cash carts. Frame rate inside, 1280 x 720: no change beyond the run-to-run noise (272 / 277 / 317 fps without, 265 / 280 / 302 with).
D DONE. Stunt event setup: Rivals None / 1 / 2 / 3 (default 3) and Easy / Normal / Hard (settings.json). Rivals ride 9 authored lines through every
   big feature (not the car gap: too short a run-up), Racer's tricks and bails, the player's scoring and smashes, live standings on the event panel,
   everyone in the results (never on the Top 10). 180 s check round: EMBER 13,135, GOLD 9,573, BLUE 5,982; none outside the park; 1 to 4 resets each.
   Split screen's AI player 2 rides the same lines in the park and the event (BUG-001), 0 s outside the park.
E DONE. Racer's ambient life on this map (`Scripts/World/Life/`, placed by `MapBuilder.Life.cs`): 1,913 groups of people (porches 1,537, yards 278,
   drives 82, the Lakes' and the Enclave's pool decks 11, the shops' walks 5), lent from a pool of 36 figures to the groups near the view, out of sight;
   Racer's football trio, coffee pairs, walkers. Who is out: new LifeSchedule (Racer's HouseholdSchedule is its Dan / Kyle visits): fewest at night,
   nobody in yards, drives or pools in rain or snow. 3,759 habitats for Racer's songbirds, squirrels, deer, coyotes, turkeys (in threes) and frogs,
   up to 10 out near the view, by their hours; they flee as Racer's. Woods' wind and birds by the tree cover; rats at 6 network entrances, drips in 23
   chambers. Kept 5 m off race and shortcut lines (the street's middle), 30 m off jumps, off pavement, the park, water. Decided (new): people turn to
   watch a vehicle and step out of the way of one coming at them (no colliders). Check: the Glen 5 groups out at noon, 0 at 2 am; one stepped aside;
   a songbird fled; frame rate at Canton Road and a Glen street the same with and without it (269 / 259 vs 295 / 256 fps, run-to-run noise; its
   scripts 0.01 ms a frame). Thin: only 5 groups on the shops' walks (the walks are narrow strips against the stores).
Imported from Racer: RivalTricks (into StuntRival), ArcadeVehicle's AiAir, AmbientLife, Wildlife, WoodlandAmbience, UndergroundLife, TurkeyVoice, the
   wildlife recordings. Left behind: HouseholdSchedule (its Dan / Kyle visits), SnowScenes, ScenePeople, the cave bats, the smoking pair.
Also fixed: the other neighborhoods' mailboxes had the Glen's asset names, so 7 mailbox cells lost their mesh and could not break.
For Dan to check: the bank hop and drop, smashing out of the pizza window, the shop rooms and the shop ramp, a Stunt event with 3 rivals, people in the
   yards / at the pools / at the shops and animals in the woods, at 3840 x 2160.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-17 01:32 (BUILD_INFO.txt).

## Previous delivery 3: Round 37
**Reviewed: driven by Dan 2026-10-16 21:19 (split screen and the stunt park; his notes are Round 38).**
**Round 37 (Dan, 2026-10-16 17:05; reports BUG-001 to BUG-003 of session `2026-10-16_16-59-30-505_bc23a8`, read by Claude,
after the Round 36 exe: free roam in the stunt park on the Skywhip 250, at night in the rain).** Round 36 is reviewed. Dan: "I
don't know where to find the fire hydrants so I will report if i have issues later as i come across the special ones" (no
change to Part D; no hints for them). Parts A and B in order; rebuild into `Builds/ChastainRush/` and commit after each;
short results (CLAUDE.md "Process"). Decide design questions yourself and say what you decided.

Part A, THE STUNT PARK'S RAMPS SLOW YOU DOWN. Standing rule (Dan, Round 12): ramps must not slow the vehicle's progress.
BUG-001 (x 3315.82 z 1131.96 h 307, the teal ramp ahead): "ramp slows". BUG-002 (x 3367.30 z 1183.43 h 53, at the loop by
the shop row): "Loop slows on both sides". BUG-003 (x 3330.24 z 1168.18 h 268, the green ramp ahead): "Green ramp slows".
Find why (a step or seam where the ramp meets the floor, the ramp's collider or physics material, the aimed-launch speed cap
of BigJump on these ramps, the loop's own steering, the grip in rain reading a ramp as grass, the Skywhip's profile) and fix
it for every ramp in the park, the loop's way in and way out (both sides) and the roof and inside ramps too, since all were
built the same way: a vehicle keeps its speed onto and up a ramp, losing only what the climb itself takes. Check in the exe:
each of Dan's three, the loop both ways, and every other launch in the park flown flat out on the Skywhip 250 and the Clay
Hopper, dry and in rain; speed at the foot vs. at the lip against what gravity alone would take (list them).

Part B, SPLIT SCREEN FROM WOODSTOCK RUSH. Dan: "Lets go ahead and add split screen". Port Racer's split screen
(`SplitScreen`, `SplitHud`, `SplitRoam`, `RaceMenus.Split`; Racer at `C:\Users\danmo\Racer`, read-only; prefer porting, list
what came), as Racer has it (sequel notes):
B1. Two players, top / bottom by default or left / right (a setting); each controller its own player (keyboard with player 1
   as Racer); player 2 can be the AI so Dan can test it alone.
B2. Races for two (every route, both directions, with or without AI rivals and traffic), Free Roam for two, the Stunt Park and
   the Stunt event for two (each player their own score, Racer's split-screen stunt rules), the Free Roam activities for two
   (each their own attempt), the Underground race for two.
B3. Each view its own HUD (lap, checkpoints, minimap, trick panel, Free Roam clock), its own camera view (the view only changes
   when that player changes it), its own resets, its own garage pick and rider look (player 2's rider look and vehicle saved
   apart). One shared radio either player controls (Racer); one day / night and weather for both.
B4. Names: the Top 10 takes each player's name as Racer does in split screen (player 2 asked for a name with the on-screen
   keyboard on their own controller); the finish panel shows both.
B5. Traffic, geese, breakables and the rest of the world work round both players (traffic spawns and despawns out of both
   cameras' sight).
B6. Menus: a Split Screen entry on the title menu as Racer has it, controller first (CLAUDE.md), B back.
B7. Cost: frame rate split screen in Free Roam at Canton Road by the shops and in a Chastain Ridge race with traffic, both at
   1280 x 720 (reported as not measured at 3840 x 2160); Dan's target from Woodstock Rush is 60 fps split screen on his PC.
Check in the exe by script with two virtual pads (and player 2 as the AI): a 1-lap race for two finished by both, Free Roam
for two, the stunt park for two with a trick each; pictures of both layouts.

For Dan to check (when done): the park's ramps and the loop at speed; split screen (both layouts, a race, Free Roam, the stunt
park, player 2 as the AI).

**Round 37 status (Claude, 2026-10-16): Part A built and checked in the exe by script (minimized 1280 x 720, muted), not yet driven by Dan.**
Evidence: `Data/screenshots/round37/` (`a_check_after.txt`, `a_check_before_round36.txt`, `a_ramps_*.csv`). Check: `ChastainRush.exe -exeRound37 <dir> -r37Only parkramps,parkloop`.
A DONE. Cause: the park's ramps, landings, tables, loop and inside ramps are mesh colliders named "Stunt park ...", never registered with
   `VehicleSurfaceContacts` (Round 10's fix covers the "sc..." shortcut ramps), so a stunt vehicle's body met PhysX's edge normals, leaning back
   against the travel. Fixed for every park surface, plus what the check found next: on the loop and the 6 m half-pipe / quarter-pipes the body's
   contacts slide without friction and the loop uses its own inward normal; the wheels push square to a park surface; the roofs' and store floors'
   box seams no longer catch a body (8 m/s lost at BIG AIR's foot); and an arcade carry (as RampHold's landings): on the park's ramps with the
   throttle held, and round the loop, a vehicle keeps its speed but for the climb (a knock over 3 m/s, a crash, is never given back).
   Dan's spots on the Skywhip, 30 m/s at the foot: drop landing 18.2 m/s on top before (gravity alone 28.6), tower landing 22.2 (27.8); after,
   every launch, landing, table, pipe, roof and inside ramp on both stunt vehicles, dry and rain, ends within 0.3 m/s of gravity alone. The
   loop: 10.6 m/s over the top before (gravity 29.5), 29.7 after, and out as fast as in; driven backward (the way out) 2 runs of 8 exit 1.9 m/s
   slower or take one knock at the top (given back in the next step).
   BIG AIR, the shop row drop, the roof gap and the tower gap still leave at their designed speed (Round 36's aimed launches, so they land on
   their slopes); that cap is at the lip, not on the ramp.
   For Dan to check, not changed: K1's foot is 4.7 m from the abandoned bank's west wall, so it has no straight run-up (not measured).
**Part B built and checked in the exe by script (two virtual controllers, minimized 1280 x 720, muted; 12 of 12 checks), not yet driven by Dan.**
Evidence: `Data/screenshots/round37/` (`b_check.txt`, `b_fps_runs.txt`, `37b_*.png`). Check: `-r37Only splitmenu,splitrace,splitai,splitroam,splitpark,splitevent,splitpics,splitfps`.
B1 DONE. Top / bottom (player 1 on top) or left / right (the setup's Screen row, settings.json); each controller drives its own player only
   (the keyboard goes with player 1); "Player 2: AI driver": Racer's race AI in a race, the traffic driver cruising the roads in Free Roam.
B2 DONE. Race (any route either way, AI rivals as race setup, traffic as Settings > Gameplay; the Underground race is a route like any), Free Roam,
   Stunt park (Free Roam from the park's start line) and the Stunt event for two (Racer's riders: each their own points). Activities: jumps,
   speed traps and tricks score for each; nothing written to the activity records (Racer). Check: race 1:15.5 / 1:21.0, both finished;
   AI player 2 finished; a backflip each off BIG AIR (1,397 / 1,378 pts); the event for two.
B3 DONE. Each half has its own HUD (lap, place, times, next checkpoint, messages, Free Roam's day and clock, the other player's direction and
   distance), minimap, trick panel, camera view (X / V on their own controller; player 2's remembered apart), resets and garage (player 2's
   vehicle and color in `vehicle2.json`, rider in settings.json). One radio for both, one day / night and weather (player 2's view gets its own
   rain, snow, stars, moon, mist, as Racer).
B4 DONE. Top 10s under each player's name (laps, races, combos, the Stunt event); player 2's name typed on their own controller; the results show both.
B5 DONE. Traffic comes and goes out of both views and round both players; junctions wait for both; geese answer the nearer player; the pipe
   network is drawn round both views.
B6 DONE. SPLIT SCREEN on the title and Free Roam menus; player 1's controller works the setup; A or Start on another controller joins; either
   player's Start pauses; B goes back.
B7 MEASURED, target NOT MET in these runs. The PC was busy (50 to 75 % CPU from OBS, Chrome and Task Manager), so the runs vary; not measured at
   3840 x 2160. Free Roam at Canton Road: split 74 / 37 / 65 fps vs one player 121 / 84 / 81. Chastain Ridge race with traffic: split 34 fps vs one
   player 38 (and 20 vs 32). Racer's split-screen savings are in (detail 0.6, shadows 0.6, 1000 m far plane).
Decided: the Stunt event with player 2 as the AI is player 1's alone on one view (Racer); split screen keeps the race setup's own settings; player
   2 starts the race beside player 1 on the grid.
Imported from Racer: see CLAUDE.md (Round 37). Not built: Racer's winner shot, Police Chase, Speed Patrol, Getaway, Stunt Races (none are in this game).
For Dan to check: split screen at 3840 x 2160 (F2 shows the frame rate); both layouts, a race, Free Roam, the stunt park, player 2 as the AI; the
   Underground race for two (not run by script).
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-16 19:48 (BUILD_INFO.txt).

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
