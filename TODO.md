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

**One heavy job at a time, at low priority — STANDING RULE (Dan, 2026-10-29 13:07).** Dan: "CODE Seems to keep locking up my computer with whatever it is doing"
(Round 82 ran its 6th full Build Map with two exe checks (check5a, check5b) at the same moment). From now on: never run a Build Map, a player build and an exe check
(or two exe checks) at the same time; one at a time. Start Unity batch runs and check exes at Below Normal priority (PowerShell `Start-Process ... -PassThru` then
`.PriorityClass = 'BelowNormal'`, or `start /belownormal`) and give Unity's batch runs fewer worker threads (`-job-worker-count` about half the cores), so Dan's
computer stays usable. At most 3 full Build Maps in a round; past that, stop and report. Copy this rule into CLAUDE.md's "Process" in the same words.

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 87, BUMPERS AND BARRIERS THAT NEVER FACE YOU, A HIGH RESET WHEN YOU JUMP OUT, the Addison Heights and Noonday Creek Trail reports (Dan, 2026-10-31 00:38; reports
BUG-001 to BUG-011 of session `2026-10-31_00-06-51-254_769f69` (BUG-009 / 010 / 011 one place), read by Claude, after the Round 86 build; HIGH MODE races of Addison
Heights (BUG-001 to 006) and the Noonday Creek Trail (BUG-007 to 011)).** Round 86 is reviewed. Item 0, the one-heavy-job rule and the end-of-round clean-up apply.
Short results. Round 83's road failures still wait (QUEUED).
Dan: "Some of these are general issues. Mainly that barriers and bumpers shouldn't be at an angle that would cause you to run into them head on when following a course.
Also in high mode if someone jumps outside of the bumpers, they should be reset onto the track". So A and B are rules for EVERY course (forward, reverse, shortcuts),
HIGH bumpers and normal-mode barriers alike; Dan's places are examples, each checked by name.
A. NEVER HEAD ON. Following a course's line, no bumper or barrier end, corner or run ever faces you: every run lies along the course (parallel to its edge, never
   angled across a lane), every end tapers or curves away from the line behind the edge, and where two runs meet they join in one rounded curve (BUG-002 x 3292.64 z
   307.55, Embry Lane: "bumpers shouldn't cross each other like this. when they join they should be roounded", the picture: two runs crossing, an end sticking out into
   the road). Closure barriers stand across the closed road's mouth, set back off the course's edge, in a line that follows the course, never with a corner in the road
   (BUG-008 x 1300.15 z -1671.25, Barrett Parkway: "Why is this barrier like this? It should be out of the way not have an edge sticking int he middle of the road", the
   picture: a zigzag of barrier blocks and a bumper stub in the lanes). Bumpers on the OUTSIDE of every corner, never an opening there (BUG-001 x 3216.77 z 535.67,
   Embry Lane: "why is there no bumper on this corner? It is one of the easiest places to flying off the track"); openings only where a shortcut or the course itself
   leaves or rejoins. Fewer, simpler: no bumpers that do nothing (on medians between the course's own lanes, round mouths of roads that are closed anyway, doubled
   beside a closure barrier) (BUG-009 to 011 x 423.67 z -1494.34, Mall Boulevard / Ring Road: "this is a little confusing all these barriers\bumpers but most are not
   needed", the pictures: bumper and barrier runs on both sides of each carriageway and the median, ends pointing into the lane). Check, by script on every course and
   direction: no bumper or barrier piece within the course's driven width whose face is more than 25 degrees off the line ahead; the human-like driver on the line
   and 3 m either side touches no end; pictures at each of Dan's places before / after.
B. HIGH MODE: OUT OF THE BUMPERS, BACK ON THE TRACK. A HIGH player who lands, or ends up, outside the bumpers (or off the course past an opening that is not a shortcut)
   is reset onto the course at once (after about a second down), placed as Round 86 B places resets (before the next checkpoint not yet passed, facing the course's
   way, no penalty). Check: jumped out over a bumper on three courses: back on the line, next checkpoint ahead.
C. RESETS FROM INSIDE THE MALL. BUG-007 (x 789.60 z -1683.53, Barrett Parkway, the Noonday Creek Trail race, checkpoint 12 of 13 next): "I reset in the mall and ended up
   way over here". Round 86 B's rule failed there (the line through Town Center's two levels, or a checkpoint inside it, missed): a reset in the mall goes back on the
   course's line inside the mall before the next checkpoint. Check: resets at five places through the mall (both levels, both directions of the race): each lands
   inside the mall, next checkpoint ahead.
D. ADDISON HEIGHTS: SLOWED IN THE ROAD, THROWN INTO THE BUMPERS, THE JUMP'S LANDING. BUG-003 (x 2967.67 z 417.02, Joel Drive: "there is something here that slows the
   car down") and BUG-005 (x 2962.48 z 408.83: "the bushes seem to cause slow down in the middle of the road", the picture: the shortcut's slowing bushes beside the
   lane): the slowing bushes and their slow-down never reach the course's road (they block only the free way round a shortcut, as the rule says). BUG-006 (x 2874.80
   z 417.87, Joel Drive: "somthing here caused me to run into the bumers"; the bump log has a JOLT at x 2875 z 411): find the jolt and smooth it. BUG-004 (x 2811.13 z
   498.87, Lyndon Drive, heading 36: "this area should be open so that you don't run into the bumper coming off the jump", the picture: a bumper's end in the landing
   ahead): the jump's landing and run-out open (Round 86 A missed it: say why). Check: Joel Drive and Lyndon Drive driven through Dan's places on the line: no slowing,
   no jolt, no bumper touched.
E. THE KENNESAW CLIMB'S KICKER (Dan, 2026-10-31 00:44, not captured as a report: "On the way up the hill there is a significant bump that generally causes all
   vehicles to jump off the road over the barriers"; found by Claude in Dan's bump log `%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\bump_log.txt`, his
   Kennesaw race 2026-10-31 00:20 and 00:21, both laps the same): Kennesaw Mountain Drive at x -394 to -402 z -5516 to -5523 (y 469 to 471): JOLT +2.8 m/s in 0.1 s, then
   AIRBORNE at 28 m/s with vy +13.2 / +14.2 m/s (speed falling from 33 to 28: a steep face, not a crest); lap 1 flew 2.16 s, 11.7 m up, 59 m, landing at x -444.5 z -5564.6
   4.1 m off the line; lap 2 never logged a landing (off the road). Smooth that face into the climb (one vertical curve the course's speed stays down on, or, if a jump
   belongs there, one that throws you straight along the road and lands on it), the barriers and bumpers beside it per A. Check: the climb driven at full throttle by
   the human-like driver in several vehicles (the bike, a car, the heaviest), normal and HIGH: no air over 0.5 s there, never off the road; picture of the spot.
Check: item 0 for every item. Quick builds unless a world rebuild is needed. PUBLISH through the launcher.
For Dan to check: HIGH MODE races of Addison Heights and the Noonday Creek Trail (bumpers and barriers, a reset in the mall, jumping out over a bumper); any other
course's barriers; the Kennesaw climb at x -398 z -5520.

## QUEUED rounds
**ROUND 83's REMAINING ROAD FAILURES (after Round 84, Dan 2026-10-30 03:21: HIGH MODE first).** From Round 83's status: 64 of 416 crossing runs not clean
(Frey Road's Express deck start and I-75's deck, I-75's decks at Big Shanty Road and Bells Ferry Road, Phillip Landrum, the Canton Road Connector's interchange and ramp
437897716, Allgood Road, Greers Chapel, Chastain Road east of I-75, the I-575 flyover backwards); the I-75 Express Lanes southbound 2.5 s in the air at the I-575 merge
(508, -3365) and a stop at the Frey Road deck; Skip Spann's west deck end. Fix each; the one-heavy-job rule and its 3 Build Maps apply.

Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 86
**Reviewed: Dan raced it in HIGH MODE 2026-10-31 00:06; his notes are Round 87.**
**Round 86, ROOM FOR JUMPS BETWEEN HIGH BUMPERS, RESETS THAT NEVER SKIP A CHECKPOINT, BREAKABLE TREES AS IN WOODSTOCK RUSH (Dan, 2026-10-30 12:04, after playing
0.85.0).** Item 0, the one-heavy-job rule and the end-of-round clean-up apply. Short results. Round 83's road failures still wait (QUEUED).
A. HIGH BUMPERS CLEAR OF JUMPS. Dan: "In high mode, the first jump i keep clipping a bumper which spins me out. Lets leave more space for bumpers at the end of ramps so
   this doesn't happen". On every course in HIGH MODE (forward and reverse, shortcuts too), no bumper stands where a jump's flight or landing can carry you: from each
   ramp's, kicker's, pile's or plume's lip through its whole flight (at the speeds the course allows, left and right of the line) to the end of its landing run-out,
   the bumpers stop short and pick up again once you are down and straight, ending in a rounded, angled-in end that turns you back (Round 85), never a square end you
   can hit. Find Dan's "first jump" (the first jump of the course he raced: say which) and show it. Check: every jump on every course in HIGH, flown by the human-like
   driver on the line and 3 m either side: no bumper touched in the air or on landing; pictures of the first jump's gap.
B. A RESET NEVER SKIPS A CHECKPOINT. Dan: "if a reset is done that the reset does not skip any checkpoints. It should go before the next expected [checkpoint] you
   should hit. Not penalize you for skipping you ahead too far". A reset in a race (yours or the automatic one, any mode, every course, shortcuts, the air races'
   resets too) puts you on the course's line BEFORE the next checkpoint you have not yet passed, facing the course's way, never past it; and no missed-checkpoint
   penalty is ever given for a checkpoint a reset carried you past. Check: resets forced just before, at and just past each checkpoint of three courses (the Glen
   loop, Kennesaw, Marietta Square) and one air race: the next checkpoint is always ahead, no penalty.
C. TREES BREAKABLE AS IN WOODSTOCK RUSH. Dan: "Trees need to be more breakable as we have done in woodstock rush" (this changes the 2026-10-10 rule that trees are
   solid). Port Woodstock Rush 1.3's breakable trees (`Racer/PROJECT_TODO_ARCHIVE.md` "Free Roam world: big ramps ... breakable trees", `SceneryTrees.cs` `Break`,
   `TreeBreaker` in `RoamWorld.cs`): every tree on the map breaks when hit fast enough for its trunk (0.5 m: 7 m/s, 1.0 m: 24 m/s, as there), falls the way it was
   hit with leaf and bark debris and the wood smash sounds, costs 15 to 45 % of the speed (half in HIGH MODE), has no collider once down (never wrecks you), and
   stands again when every player is 160 m away. In races and free roam alike. This game's existing breakable trees (`TreeField.breakable`, `FallingTree`) become this
   one system. Frame rate in the woods (Noonday Creek, Kennesaw) before / after, and on the Steam Deck setting, with a dozen trees down in view.
   Check: a thin trunk at 12 m/s breaks, a big one at 12 m/s holds and at 30 m/s breaks; no wreck; all back after leaving.
Check: item 0 for every item. Quick builds unless a world rebuild is needed. PUBLISH through the launcher.
For Dan to check: HIGH MODE's first jump (and the others); a reset next to a checkpoint; driving through the woods.

**Status (Claude, 2026-10-30): built (code only on Round 84's scene, exe 18:43), checked in the exe, published 0.86.0 (`game-8600`), not yet driven by Dan.** Evidence `Data/screenshots/round86/` (tree pictures in `local/round86/check9/`: Chastain Glen). No bug report newer than Round 82's.
A DONE. `HighBumpers`: every jump on every walled line found from the driven ground along it (the line and 3 m either side, median-filtered) by flights at every speed up to what the course allows there (its bends at 20 m/s2 sideways, 12 m/s2 out of them); a jump is 0.8 m clear for 0.4 s. No bumper from 6 m before a lip to 40 m past the furthest landing, nor within 7 m of another line's flight (a shortcut landing on the lap); then a funnel 8 m further out closing in over 30 m; every bumper end a rounded half dome. Found: 130 jumps on 14 course-directions (Kennesaw 4: a crest before J1, J1, J2, J3). Dan's "first jump": not recorded (he played on the Deck); taken as the Kennesaw Mountain race's first, at (-1083, -5833) before J1, pictures `86_kennesaw_*_first_*`.
  Check (`check_jumps_all.txt`, then `check_jumps_final.txt`): the human-like driver (Street Classic) on the line and 3 m either side, touches in the air or the first second down. All courses: 7 of 320 flown runs touched (shortcuts landing on the lap, a long landing); after the two fixes, the three courses with touches again: 0 of 215. Kennesaw reverse and Marietta reverse are not raced.
B DONE. `CarRecovery`: a reset in a race goes between the last checkpoint passed and 12 m short of the next one (the line when all are met), never past; if that is inside a jump's flight, before its lip. A reset never costs a penalty; the AI's recovery forgives what it skips; in the air races only an AI pilot is put past a ring after three crashes (forgiven). Check: 108 resets on the Glen loop, Kennesaw and Marietta Square (15 m before, at, 15 m and 60 m past each checkpoint): 0 past, 0 penalties; Quarry Air Race, every ring, reset and 3 crashes: 0 wrong.
C DONE. Woodstock Rush 1.3's breakable trees for every tree (`TreeTrunks`: its TreeBreaker and Needed, 7 m/s at 0.5 m, 24 m/s at 1 m, HIGH 45 %; speed -15 to -45 %, half in HIGH; `FallingTree` falls in 1.5 s with a thud and nine pieces of debris, lies with no collider; back when every player is 160 m away; `TreeField` hides any tree). Players and AI break them; the Noonday trail's old trigger trees are this system now; `-treesSolid` in a check is Round 85's way. This map's trunks are 0.28 to 0.73 m (median 0.49 m: 7 m/s). Check: a 0.41 m trunk at 11 m/s broke (-15 %, upright), a 0.72 m one held at 13 m/s and broke at 22 m/s (the run-in topped out there), no wreck; all 3 back 220 m away. Frame rate (1280 x 720, not measured at 3840 x 2160) standing / 12 trees down in view: Noonday woods 75 / 86 fps, Kennesaw 57 / 58; Steam Deck setting 166 / 176 and 121 / 127.
Published: `game-8600` (0.86.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-10-30 18:43. Disk (project folders) 34.4 -> 33.8 GB; Builds 11.3 GB (0.83.0 to 0.86.0's release folders kept).
Decisions: the jump-finding numbers above are first guesses (a jump here is anything that throws you 0.8 m up for 0.4 s, so some crests count); hand-placed kit trees keep their own solid trunks.
For Dan to check: HIGH MODE's jumps (Kennesaw, the Noonday trails, Addison's Elaine jump); a reset next to a checkpoint; driving through the woods, thin trees at 25 km/h and big ones flat out.

## Previous delivery 2: Round 85
**Played by Dan 2026-10-30 (HIGH MODE); his notes are Round 86.**
**Round 85, BUMPERS AND BARRIERS THAT BOUNCE YOU BACK ON COURSE (Dan, 2026-10-30 09:38, after playing Round 84's HIGH MODE).** Item 0, the one-heavy-job rule and the
end-of-round clean-up apply. Short results. Round 83's road failures still wait (QUEUED).
Dan: "i WANT it so that in high mode that you bounce of barriers and they keep you straight on course as opposed to having you got hung up on the wall. Actually in
general I want this for bumpers". Round 84's own check saw it: the bot stalled against a HIGH bumper 6 to 9 times a lap and reset.
A. HIGH MODE BUMPERS. Hitting a gutter bumper at any angle short of head-on turns you back along the course and keeps your speed (a glancing hit loses little; the
   harder the angle, the more it costs, but you leave the bumper pointed down the course, never stopped, spun or riding along it). No snagging: smooth, continuous
   colliders without posts, seams or corners the wheels or body catch; no friction against the vehicle's side; the bumper's push away from it, not up (no climbing,
   no launch). Planes and AI unaffected (AI still pass through).
B. THE SAME FOR BARRIERS IN GENERAL (Dan: "in general I want this for bumpers"), in every mode: the race closure barriers and course barriers, guard rails, the
   Express Lanes' walls and bridge parapets, highway median walls, and the course-edge rails on the trails. A glancing hit slides you along and straightens you
   to the barrier's line instead of hanging you up; a head-on hit still stops you (normal mode keeps its crashes). Breakable things (fences, signs, gates, mailboxes)
   stay breakable, and solid scenery (trees, houses) is not part of this. Say which colliders were changed.
Check: the human-like driver pushed into the edges for a lap of the Kennesaw trails, the Noonday Creek Extension and the Chastain Glen loop in HIGH MODE (stalls
against a bumper before / after: aim 0), and into the walls along the I-75 Express Lanes and a course's closure barriers in normal mode at 10, 20 and 30 degrees
(speed kept and heading after each hit, before / after); a recorded clip or picture series of one glancing hit each.
Build: quick builds only if no world rebuild is needed. PUBLISH through the launcher.
For Dan to check: HIGH MODE on the mountain and the trails, bouncing off the bumpers; scraping the Express Lanes' walls and a race's barriers in normal mode.

**Status (Claude, 2026-10-30): built (code only on Round 84's scene, exe 10:48), checked in the exe, published 0.85.0 (`game-8500`), not yet driven by Dan.** Evidence `Data/screenshots/round85/` (barrier pictures in `local/round85/`: Chastain Glen). No bug report newer than Round 82's.
How: `Vehicle/WallGlance.cs` on the players' vehicles (both split-screen players; AI, planes, helicopter unchanged). A hit on a guide wall is turned along it: speed kept 1 - (1 - cos a) x 0.8 (HIGH 0.6), a sideways push off it (never up), heading and travel held on the wall's line 0.18 s, no friction. Normal mode up to 50 degrees; past that, and for the rest of that touch, a crash as before. HIGH up to 82 degrees, and pinned slow against a bumper 0.6 s: turned down the course at 8 m/s.
Colliders changed (`GuideWalls`): HIGH bumpers (solid fence now down to their buried base, it began 0.3 m up; no bounce, no friction); race closure barriers and pipe-mouth barriers (one box per unbroken run instead of a box per block); the Underground's rails and seals; the Express Lanes' walls; bridge parapets (`BridgeStructure`); map-edge closures. No highway median walls exist. Trail railings stay breakable (Round 64), so not changed.
A: HIGH laps steering into the edges (`check_lap_high.txt`): stalls against a bumper Kennesaw 6 -> 0, Noonday Creek Extension 9 -> 0, Chastain Glen loop 0 (no before); 119 / 57 / 15 glances; one stuck spot on Noonday 5.9 m from any bumper. Pictures: `85_lap_*_glance_0..2` (against the bumper, then pointed down the course beside it).
B: normal mode, Drifter Twin, throttle held (`check_wall_before.txt` / `_after.txt`). Express Lanes wall, speed kept 0.6 s after / heading off the wall's line: 10 deg 18 % / 14 -> 110 % / 1; 20 deg 14 % / 28 -> 106 % / 0; 30 deg 21 % / 9 -> 102 % / 1; 70 deg (head on) 35 % -> 53 % (still a hit, not a slide). Closure barrier B5 (Chastain Glen loop), after: 10 deg 114 % / 10; 20 deg 106 % / 4; 30 deg 100 % / 4. Its before is not a clean comparison: aimed 2 m from the barrier's end, the car slid round the end (two attempts at the barrier site; stopped there per the tuning limit). Pictures: `85_Express_Lanes_wall_20deg_*` (after) and `_before_20deg_*`.
Disk (project folders) 32.9 -> 32.5 GB; Builds 9.9 GB.
Decisions: the angle limits and speed losses above are first guesses; the players only (AI racers not given it).
For Dan to check: HIGH MODE on the mountain and the trails, bouncing off the bumpers; scraping the Express Lanes' walls and a race's barriers in normal mode; whether a 50-degree hit should still glance or crash.

## Previous delivery 3: Round 84
**Played by Dan 2026-10-30 (HIGH MODE); his note is Round 85.**
**Round 84, HIGH MODE FROM WOODSTOCK RUSH; the Express gate in the middle of the highway (Dan, 2026-10-30 03:21, after driving 0.83.0 a bit on the Steam Deck).**
Rounds 82 and 83 were driven briefly; Dan reviews them fully later. Item 0, the one-heavy-job rule and the end-of-round clean-up apply. Short results. Round 83's
remaining road failures wait (QUEUED): Dan: "On this round, before continuing fixing roads can we add high mode from Woodstock rush".
A. HIGH MODE, PORTED. Bring over Woodstock Rush's HIGH MODE (Racer is read only; its spec and results: `Racer/PROJECT_TODO_ARCHIVE.md` "Previous delivery — HIGH MODE"
   and the 1.2.0-review2 addition; code `HighMode.cs`, `HighBumpers.cs`, HighAssist, `RaceMenus.High.cs`, `RaceMenus.Split.cs`, and the later fixes in
   `Racer/PROJECT_TODO.md` that mention HIGH MODE). Port the finished code rather than rewriting; list what came with it. As there:
   1. HIGH MODE: ON / OFF as the main menu's top row (A or left / right), remembered, a HIGH badge on the menu and HUD; switchable from pause (a race asks restart or
      keep racing); split screen: a row per player.
   2. Fewer choices: HIGH MODE, (RESUME DRIVING), FREE ROAM, RACE (pick a track and go: default laps, Easy rivals), CHASES & STUNTS (one page: this game's police modes and
      stunt events, each starting at once with defaults), VEHICLE (one scrolling row with pictures and stat bars, also in pause and on race results), TIME & WEATHER,
      REPLAYS, SETTINGS, QUIT. Everything unlocked; nothing done in HIGH MODE unlocks anything in normal mode, except goose eggs, which count in every mode (as
      Woodstock Rush's acorns ended up).
   3. Forgiving: gutter bumpers along both edges of every car race course and its shortcuts, HIGH races only (openings where a shortcut, the rivals' line or a traffic
      road crosses or joins; drawn and solid only for a HIGH player); no wrecks from landings, upright after 1 s; quicker reset; hits cost half; rivals Easy.
      The air races (this game only, Claude's adaptation, say what was done): rings larger and no crash (a plane that hits the ground or a building is put back
      flying at once).
   4. HIGH records on their own boards, marked HIGH; replays marked HIGH; a HIGH tab on the records screen.
   5. In normal mode, 3 wrecks or resets in 2 minutes or 2 in one lap asks "Would you like to play HIGH MODE?" Yes / No (No: once a session).
   Controller first, only the device being played shown, readable at 1280 x 800 (the Deck).
   Check: the menu by controller at 4K and 1280 x 800 (a picture of each HIGH page); one HIGH lap of the Kennesaw Mountain trails and the Noonday Creek Extension steering
   into the edges: stays on the course; a crooked landing: HIGH upright, normal wrecks; an air race in HIGH flown into the ground: back flying; a HIGH record on the HIGH
   board only; the prompt after forced wrecks; a test save only, never Dan's.
B. THE EXPRESS GATE IN THE MIDDLE OF THE HIGHWAY. Dan saw on the Deck (no bug report possible there): "the express lane gate was in the middle of the highway as opposed
   to the entrance". Find it: for every Express gate, check its arm stands across its own ramp only, at the ramp's general-road end (Round 81 B), never across a lane of
   I-75, I-575, the Express Lanes or any through road; Round 83's wall and ramp changes may have moved a ramp's line under a gate. Fix every one that is wrong. Check: a
   picture of every gate from the driver's seat, and each driven past on the highway beside it at speed (no gate met).
Check: item 0 for every item. Build: one full Build Map only if B needs the world rebuilt (quick builds otherwise). PUBLISH through the launcher (Dan plays on the Deck).
For Dan to check: HIGH MODE on the PC and the Deck (Free Roam, a race, an air race, a chase); the Express gates.

**Status (Claude, 2026-10-30): built (one quick build of the Edges step, exe 06:31), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round84/` (menu, landing and prompt pictures in `local/round84/`: the spawn is behind them). No bug report newer than Round 82's.
A DONE. Ported from Racer: HighMode, HighAssist, HighPrompt (`Assets/Scripts/High/`), HighBumpers, the HIGH pages (`Menus/GameMenus.High.cs`, with RaceMenus.Pick's small turning previews), the HIGH badge (IMGUI here, `HighBadge`), ArcadeVehicle `Forgiving`, the high- boards, StuntProgress's HIGH Top 10s.
- A1/A2 pages by controller, 16/16 at 1280 x 800 and 3840 x 2160 windowed (`check_pages_*.txt`): HIGH MODE top row (A or left / right, saved), HIGH menu, FREE ROAM 1 / 2 players, RACE (every course and direction, then go: its own laps, 3 Easy rivals), CHASES & STUNTS (Getaway, Cop vs Runner, Speed Patrol, Stunt event; Easy, 5 min), VEHICLE (scrolling row, pictures, 3 stat bars; also in a HIGH race's pause / results), TIME & WEATHER; pause row in races (RESTART / KEEP RACING); split screen: a row per player (setup and pause).
- A3 bumpers on every car course and its shortcuts (not the air races, not the Underground's pipes); solid and drawn only for a HIGH player, AI pass through; openings at shortcuts, the lap passing again and any map road. HIGH laps by the human-like bot pushing into the edges: Kennesaw and Noonday Creek Extension completed. Against the same drive in normal mode (`check_lap_*.txt`): Noonday 27.6 m off the line -> 10.3 m; Kennesaw's largest offsets (38 to 44 m) are jump flights in both modes. The bot still stalls against a bumper 6 to 9 times a lap and resets (Racer's check: the same bot limit).
- A3 landing: dropped on its roof, HIGH upright in about 1 s, normal wrecks (Needle 600, Drifter Twin). Quicker reset: the reset is at once here already; a crash's wait halved (trains), 1 s (planes, Free Roam).
- A3 air races (this game only): rings 1.5 times; a crash in a HIGH air race puts the plane back flying at once (nosed into the ground: crash 0.9 s, 0.4 s later flying 19 m up at 75 m/s).
- A4: a HIGH race went on the high- boards only (HIGH 0 -> 2, normal 0 -> 0); its replay "HIGH · ..."; RECORDS has the HIGH boards after the normal ones (opened on this course's HIGH board). HIGH police, quarry and activity runs stay off the normal Top 10s; goose eggs count in every mode.
- A5: 2 wrecks in a lap asked "Would you like to play HIGH MODE?"; B = No; 4 more wrecks: not asked again.
B DONE. Cause: gates 3 and 4 sit on the median slip ramps between I-575 and the Express Lanes; 30 m in from the general end those ramps still overlap I-575, so the arms stood 3 to 4 m into I-575. Now each gate goes at the first place (5 m steps) where its arm and post, shut and open, keep 0.5 m clear of every other road (`BuildExpressGates`): gates 3 and 4 moved to 105 m in; 1, 2, 5, 6 unchanged. Check (`check_gates_prompt.txt`, `84_gate*_overhead.png`, `_driver.png`): no arm over a named road; the road beside each driven at 30 to 33 m/s, no gate met.
Published: `game-8400` (0.84.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-10-30 06:31. Disk (project folders) 33.1 -> 32.5 GB; Builds 9.9 GB (0.82.0's and 0.83.0's release folders kept).
Decisions: everything already unlocked (nothing is locked here); the bumper height and the 50 % speed give-back are Racer's first guesses; split screen's halves show their own HIGH tag.
For Dan to check: HIGH MODE on the PC and the Deck (Free Roam, a race, an air race, a chase, a friend in split screen); do the bumpers feel right on the mountain and trails; the Express gates at I-575 (715, -956) and (712, -2027).

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
