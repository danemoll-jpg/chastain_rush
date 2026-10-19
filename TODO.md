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
(none: waiting for Dan's next notes)

## QUEUED rounds
Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- STARTUP POLISH (2026-10-18): Racer's spoken title, loading screen and hints; Dan: "i Would want 5 but we don't have a cover yet".

## Previous delivery 1 (newest): Round 50
**Reviewed: Dan approved the Round 50 build 2026-10-19 17:04 ("I approve the latest update").**
**Round 50, THE KICKER SHORTCUT MADE A REAL RISK, THE LAKE TRAIL AS IT REALLY LOOKS, no JUMP sign (Dan, 2026-10-19 13:37;
reports BUG-001 to BUG-004 of session `2026-10-19_13-25-47-596_f0f013`, read by Claude, after the Round 49 exe: a reverse Chastain
Lakes race at night on the Drifter Twin).** Round 49 is reviewed (the intake and the map are accepted). The two new standing rules
at the top of this file apply first (no free way round a shortcut; tire tracks are the hint). ONE full Build Map at the end:
plan the whole line on paper first (the run without the stunt included), use quick builds while working; Round 49 took seven full
builds. Tuning limit. Short results.
A. THE KICKER: BLOCK THE FREE WAY, AND A SECOND JUMP. Dan: "the ramp one really did nothing. As it is you could just drive through
   this area and skip the ramp and nothing will slow you down." BUG-003 (x 1207.19 z 1004.53 h 214, the yards on the kicker's line
   between CP4 and CP5, reverse): "as a follow up this issue should have bushes that you can drive through but will slow you down,
   this is to prevent people from just driving through here". BUG-004 (x 1161.78 z 982.28 h 227, at Northlake Point, where the
   kicker's line crosses that cul-de-sac): "There could be a second jump in this area as well again with bushes or something so you
   can't just drive through this way".
   (1) Bushes (dense shrubs / a hedge row, drivable through, slowing you hard: a real cost, as fences do now) across every free way
   through these yards on both sides of Northlake Point, so the only fast way is through the air. (2) The kids' kicker stays; its
   flight carries you over the first bushes. (3) A SECOND JUMP on the same line at Dan's BUG-004 spot: from Northlake Point on over
   the next yards' bushes toward the lap past CP5, landing on or next to the lap heading the race's way. A real physics launch
   (ramp rule), something that could be on a cul-de-sac or in a yard at its true size (not a dirt pile, not another kids' kicker:
   Code picks and says why). (4) Done right (both jumps clean) it must save clearly more than Round 49's 1.1 s; missed (short, slow,
   or round the ramps) you are in the bushes and lose more than you would have saved. The AI takes it and sometimes fails.
B. THE LAKE TRAIL, AS THE PHOTOS SHOW IT. BUG-001 (x 1307.56 z 1105.97 h 87, behind the pool, reverse): "why does this look so
   sloppy and why is the sidewalk on hanging on the hill like this. Cant you use the reference photos to see how the real path is
   not slopped down from the hill. Also i really don't think it needs to be widened. vehicles can also drive in the grass so i don't
   see the issue just having it look normal". Rebuild the path's piece from the lot behind the pool to the footbridge to look like
   photos 2 and 3 (`local/reference_photos/chastain_lakes/r44_2*`, `r44_3*`): its REAL width (about 1.5 m concrete walk with timber
   edging past the pool's granite wall, then about 2 m gravel with tan brick edging), lying flat on graded ground that follows the
   hill (no path tilted sideways or hanging off a slope, no sharp-edged cut faces; the bank beside it soft and grassed or pine
   straw as in the photos). The race line is NOT held to the path: vehicles run on the path and the grass beside it, so the grass
   either side is smooth and drivable at race speed. Same treatment on the dam path north of the bridge (real width, flat, grass
   either side). This replaces Round 44's "7 m, wider than real" for this trail only.
C. NO JUMP SIGN. BUG-002 (x 1268.79 z 1040.35 h 187, the kicker's turn-off on Lakeside Drive): take the JUMP! board away; the tire
   tracks stay as the hint (new standing rule).
Check: the kicker line clean (both jumps; time saved, Street Classic and Drifter Twin); the same ground WITHOUT the jumps (round
each ramp, through the bushes, through other yards): slower than the plain lap; a short first jump; the AI race reverse, traffic
Normal, all finish. The trail both ways on all 11 vehicles at race speed, no stop, partly on the grass. Pictures: Dan's BUG-001,
BUG-003 and BUG-004 spots, the second jump, the trail from photo 2's and photo 3's spots next to the photos.
For Dan to check: the kicker line and its second jump (and try to cheat it); the trail behind the pool; the turn-off without the sign.

**Status (Claude, 2026-10-19): A to C built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round50/`.
A DONE, time saved NOT MET. THE CARPORT (second jump, BUG-004): a storm-collapsed two-car metal carport at the end of Northlake Point, its east legs folded, the
   roof sheet (6.7 m wide, 20 degrees, eave 2 m up) the ramp, a real jump west over the shrub bed between 231 and 230 to the lap past CP5 (`MapBuilder.LakesKicker.cs`).
   Why: metal carports are really in these yards and at true size this is the launch the spot needs, wide enough to hit after the first landing (a dump truck's
   raised bed, the other candidate, is 2.4 m wide). SHRUB BEDS (BUG-003): Racer's ShortcutUndergrowth ported as `Neighborhood/ShrubBeds.cs` (drag 2.5/s: hard, never
   stuck); brush down the valley under the kicker, cattails where it meets the lake, planted beds 231 to the lake, between 231 and 230, from 230 and 233 to Northlake
   Point's race barrier, 233 to the valley: the pocket between the jumps can only be crossed fast through the air.
   Clean, both jumps: Street Classic and Drifter Twin each save 0.9 s (Round 49: 1.1 / 0.6). Tuning (one try, stopped): at 30 degrees the carport lobbed cars 70 m
   in 3.1 s and saved 0.4 to 0.5 s; at 20 degrees 0.9 s. Most of the time goes in the turn-off arc onto the kicker's line (Round 49's), which I left as Dan accepted it.
   Without the jumps (Dan's rule), all slower than the plain lap (12.1 s): beside both ramps +7.2 s, kicker then beside the carport +2.6 s, beside the kicker then
   the carport +3.9 s, across the lake +19.7 s, kicker then down Northlake Point round its barrier +3.1 s (times before the 5 s CP5 penalty). Short: off the kicker at
   16 m/s +1.3 s, off the carport at 14 m/s +1.6 s. AI reverse race x2, traffic Normal: all 6 finish, 0 missed; kicker line 4 times, 0 short.
B DONE. The trail at its real width: concrete 1.5 m (timber edging), gravel 2 m (tan brick) to the footbridge, the dam's gravel 1.5 m (grass edges), on a level 4 m
   bench with soft banks (3.5 m uphill, the lake's room on its left: `GradeAlong` blend_left / blend_r). All 11 vehicles both ways 2 m off the path: no stop (slowest 8.0 m/s).
C DONE. The JUMP! board gone; the worn tracks stay. Forward race unchanged (104.1 s, 0 missed, 18 barriers; shut checks pass).
Build: two full builds (the second for the carport's try) plus a final one; `Builds/ChastainRush/` 2026-10-19 15:26. Imported from Racer: ShortcutUndergrowth (as ShrubBeds).
For Dan to check: the kicker line and the carport (try to cheat it); is 0.9 s enough, or move the turn-off or flatten the kicker (your call); the trail behind the pool.

## Previous delivery 2: Round 49
**Reviewed: Dan drove the Round 49 build 2026-10-19 13:25 (a reverse Chastain Lakes race at night); his notes are Round 50 (the intake "was fine"; the kicker, the trail and the sign were not).**
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

**Status (Claude, 2026-10-19): A to D built and checked in the exe by script (14 of 15 on the full set; the AI race on the last build 6 of 6), not yet driven by Dan.** Evidence `Data/screenshots/round49/`.
A DONE. THE INTAKE: off Lakeside Drive 45 m before CP7, right up the point's service track (the lake association's LAKE INTAKE sign, worn ruts), along a 6 m
   concrete jetty 2 m over West Chastain Lake, off its end into a square concrete riser 11.5 m out (walls 1.2 m out of the water, trash racks, buoys), down its shaft
   to LI, B29 in glass under the lake (52 m), under M2b by the north shore, up MH15 onto North Lakeside Drive 30 m past CP9, facing east. Forgives CP7 to CP9 (Dan's line:
   it skips the west loop). Clean: off the jetty at 21.5 / 23.7 m/s, saves 5.2 s (Street Classic) and 4.8 s (Drifter Twin). Off the jetty at 9 m/s: into the riser's wall
   and the lake, not taken; a reset puts you on the race's line (under about 16 m/s you hit the wall; a car short in the lake can not climb in: taken only from the air).
B DONE (time saved small). THE KICKER: just past CP4 a JUMP! board, a 28 m arc through 239's front yard and between 239 and 237, down the back yard to the
   neighborhood kids' plywood kicker (3.5 m, 35 degrees; a kids' ramp because it is truly that shape in a back yard), a real jump west over the creek hollow below
   the lake's tip, between 232 and 246, on through the yards to the lap past CP5 (forgives CP5). Physics can not reach the lap past CP5 itself (165 m); the flight is
   the hollow. Tuning (two tries, stopped): try 1 17.8 / 20.1 m/s at the lip, both short; try 2 (kicker 7 m further down its line, wider turn) 21.1 / 21.2 m/s, 42 m,
   lands upright; at 17 m/s it comes down in the hollow (out along it, or a reset). NOT MET: it saves only 1.1 s (Street Classic) and 0.6 s (Drifter Twin).
C DONE. The trail from the lot behind the bathhouse (concrete), round the pool deck's corner (gravel, tan brick) to the footbridge, now over the spillway where the
   outflow leaves the lake; grass between the pool house and the courts; the lot's 1.1 m step at its corner ramped at 16 %; the porch swing moved 20 m off the line.
   The trail runs 6 m straight off the bridge's north end before it bends onto the dam (a reverse AI hit the rail end; a mower slid off into the outflow).
   All 11 vehicles both ways at the race's speeds, no stop (slowest 9.7 m/s; the Needle 600 7.1 m/s brushing the bridge's rail forward). Round 47's NOT MET is gone.
D DONE. Left stick / WASD pans and carries the pointer on to the map's edges; right stick / mouse is the pointer; A / click chooses under it (a place: travel; an egg
   area: show), X travels, Y sets the waypoint at the pointer; center moved to D-pad left, shortcuts and pipes to D-pad right. Check: from all four corners a place
   chosen with the pointer and traveled to; a waypoint 707 m off the view's center; a mouse click on a place asks to travel.
Both shortcuts are shut in a forward race (barriers B17 / B18, clues hidden, a grate on the intake), open in reverse and Free Roam; the forward lap unchanged (104.0 s, 0
   missed). AI reverse race x2, traffic Normal (last build): all 6 finish, 0 missed; down the intake 3 times (its jetty speed raised to 23 m/s: at 18 every AI hit the
   wall), off the kicker 2 times, 1 short into the hollow. The AI's recovery no longer puts a car under a structure (it put two back under the footbridge, in the
   stream, again and again: `RoadDriver.Covered`), and after four tries with no clear spot it looks on along the lap (never a trap).
Build: seven full builds, not one (each fix was found in the exe: the intake's wall and catch, the two kicker tries, the lot's step, the kicker's entry plane (every car
   on the lap crossed it), the map's controls line, the bridge's north end, the AI's recovery and jetty speed); `Builds/ChastainRush/` 2026-10-19 12:38. Imported from Racer: nothing.
For Dan to check: the intake (clean, and short off the jetty); the kicker (is 1 s enough reward, or move the turn-off or landing?); the trail behind the pool both
   ways; the map's pointer and pan with a controller and the mouse.

## Previous delivery 3: Round 48
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
