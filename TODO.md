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
**Round 55, THE ELAINE JUMP REDONE WHERE DAN MEANT IT, the start line moved off it, the bounce house's sound, two bumps, AI that passes (Dan,
2026-10-21 12:28; reports BUG-001 to BUG-003 of session `2026-10-21_12-16-40-990_d377b3`, read by Claude, after the Round 54 exe:
Addison Heights forward and reverse on the Drifter Twin).** Round 54 is reviewed. Standing rules apply. ONE full Build Map at the end
(plan the line on paper first, the run without the stunt included; quick builds while working). Tuning limit. Short results.
A. THE ELAINE SHORTCUT (FORWARD), REDONE. Dan: "the new forward one is a fail. It looks like it did literally nothing but mark a line
   on the map, not to mention that it bypasses the feature I wanted to utilize which is the place in the bug report". BUG-002 (x 3053.14
   z 309.33 h 271, ON Elaine Circle heading west, at the west end of its straight where it bends north to Joel): "The shortcut makes no
   sense at all. Not even sure where it wants me to go. Plus where you start you are too close to the end of this so it is really
   confusing because starting from back it looks like a shortcut at the very beginning of the race. I wanted the jump here as it is a
   natural jump."
   Claude's mistake in Round 53 B: the jump is NOT the crest at the Suholden corner (Dan's Round 53 BUG-005 is only where you turn off
   the lap onto Elaine); it is HERE, at Elaine's west bend: drive west along Elaine from the corner, and instead of following the bend
   north, go straight off its edge: the ground drops away west and you fly (Dan, Round 53: "this jump will miss the track most likely.
   At least every time I tried it. Perhaps we can build off where it lands, have the path be a little longer before you join the main
   road"). So:
   (1) The way: off the lap at the Suholden / Elaine corner, west along Elaine's own road to the bend, straight off the edge at Dan's
       spot (a natural launch: keep the ground's own lip, smooth it only so it launches cleanly and straight, no ramp). Elaine north of
       the bend, on to Joel, stays closed (barrier) so there is no plain road way round.
   (2) The landing: drive it at race speed (several vehicles) and find where cars really come down; build there: a clear landing
       (graded, drivable) and a visible way on from it (worn dirt path / tire tracks through the yards and trees, cleared of trunks,
       readable at speed) that is a little longer and joins the lap heading the race's way (Hayworth Drive or Joel Drive northbound /
       westbound, Code picks and says why). It must LOOK like a way: someone seeing it from Elaine knows where to go.
       Dan, 12:33: "that is why before I asked for a landing spot that forced the user to drive through some backyards and stuff to
       get back to the road to get rid of part of the advantage it got, this is especially the case because you will likely overshoot
       your the road anyway." So expect cars to overshoot Hayworth and come down in the yards beyond it: the way back from there winds
       through those backyards (round sheds, between fences, past play sets: breakable fences and the like are fine) before it reaches
       the road. That backyard run is what takes back most of the time the jump gains.
   (3) Throw away Round 54 A1's valley way to Crawford and its beds (the map line Dan saw), and any of its leftovers.
   (4) A clean run saves a few seconds; missed (short, off line) costs more; no free way round (standing rules). Code reports the numbers.
       It skips about 350 m of lap (Claude's estimate); if a sensible-looking way cannot bring it down to a few seconds, stop and report.
B. THE START / FINISH MOVED AGAIN. Round 54 put it on Crawford Circle's east leg (z 245), just before where A rejoins: from the grid it
   looks like a shortcut at the start. Put the line where it is not in the part A skips (Suholden south, Kelly, Crawford, Hayworth up to
   A's rejoin) and not in sight of A's ends from the grid, on as straight and level a stretch as the lap has (Joel / Embry / Suholden
   Circle); Code picks and says why. Both directions; records (keep old times apart as Round 54 did), checkpoints, grid, AI to suit.
C. THE BOUNCE HOUSE'S SOUND. Dan: "the bouncy castle should use the same sound effect as the trampoline instead of making no sound at
   all." Use the trampoline's bounce sound (`Audio/SoundDropIns.cs` Bounces, Dan's drop-in or the made one, one at random per bounce).
D. BUMPS. BUG-001 (x 2977.66 z 308.73 h 14, Hayworth Drive at Crawford Circle): "smooth this bump". BUG-003 (x 3136.36 z 97.41 h 6,
   Kelly Drive at Suholden Circle): "smooth this". Smooth both junctions.
E. THE AI DRIVES ROUND CARS (all tracks). Dan, 12:33: "the AI drivers need to be smart enough to drive around cars. I think this is part
   of the problem. I see them stuck to bumpers as opposed to trying to get around other vehicles". Race AI must overtake: when a traffic
   car, a parked car or another racer is ahead and slower, pull out and pass where the road (or the grass beside it) has room, else
   hold back a car length and take the next gap; never sit pushing a bumper. This is likely part of why the AI is slow on Addison
   Heights (Round 52 and 54's traffic queues). Check on Addison Heights and one other track with traffic Normal: count AI passes, time
   spent stuck behind a car (before / after), all finish.
Check: A clean (time saved, Street Classic and Drifter Twin), short, and the ways round; the AI takes it sometimes; forward and reverse
races x2, traffic Normal, all finish. Pictures: Dan's spot looking west (the drop), the landing, the way on from it, the new start line
from the grid, the two junctions.
For Dan to check: the Elaine jump and its way on through the backyards; the new start / finish; the bounce house's sound; the two bumps;
the AI passing traffic and each other.

## QUEUED rounds
Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- THE MAP EXTENSION (Dan, 2026-10-19 22:12 and 22:23: "Let's save this for Tuesday after my weekly allowance resets"): west and south
  to Town Center mall and Kennesaw State University, maybe the Cobb County airport (his map: `local/reference_photos/extension/
  town_center_ksu_airport.jpg`). Claude's rough sizes: today 4.0 x 1.9 km (7.6 km2); with Town Center and KSU about 6.3 x 4.2 km
  (about 26 km2, 3.5x); with the airport about 8.2 x 4.9 km (about 40 km2, 5x). The 1 m elevation tile already covers all of it.
  Claude suggested two stages (Town Center and KSU first, check frame rate and build time, the airport after); Dan has not decided.
  Claude writes the round when Dan says go.

## Previous delivery 1 (newest): Round 54
**Reviewed: Dan drove the Round 54 build 2026-10-21 12:16 (Addison Heights both ways): "The reverse shortcuts are fine" (A2, A3 accepted; the bounce house needs a sound); A1 (the Elaine jump) "is a fail"; the rest no complaint. His notes are Round 55.**
**Round 54, ADDISON HEIGHTS: THE ELAINE JUMP, TWO REVERSE SHORTCUTS, BUMPS, SIGNS, A HARDER AI; a missed finish line; COMPLETE RACE;
fixes on three other tracks (Dan, 2026-10-21 04:27; reports BUG-001 to BUG-010 of session `2026-10-21_03-39-54-769_591b8d`, read by Claude, after
the Round 53 exe: Addison Heights races, a reverse Chastain Glen loop, Chastain Lakes and Free Roam, on the Drifter Twin).** Round 53 is
reviewed. Standing rules apply (shortcuts: real risk, no free way round, tire tracks as the hint, real physics on ramps, nothing slows a
car on a ramp; every road off the lap closed). ONE full Build Map at the end (plan every line on paper first, the run without the stunt
included; quick builds while working). Tuning limit. Short results.
A. ADDISON HEIGHTS.
A1. START / FINISH MOVED, AND THE ELAINE JUMP BUILT (Round 53 B, FORWARD). Dan picked Code's option (1): move the start / finish to
   Crawford Circle's east leg (straight, about 7 %), so the way back from the Elaine landing can rejoin past the line on Crawford Circle
   / Hayworth Drive and save a few seconds. Then build Round 53 B as briefed there (the natural jump off the crest at Suholden / Elaine,
   no ramp; from where cars really land, a longer winding way back with its own fun; Elaine's own road below the landing not a free
   way back). Move the checkpoints, grid, records keys (old Addison records can be dropped: the lap changed) and AI to suit, both ways.
A2. A REVERSE SHORTCUT JUMP OFF JOEL DRIVE. BUG-003 (x 2921.86 z 410.63 h 141, Joel Drive, reverse race, CP7 next): "can we put a
   shortcut jump through here? Should be able to catch a lot fo air" (the picture: the ground drops away south of Joel toward the
   houses below). Code designs it from there toward the lap ahead (Hayworth / Crawford southbound); a real launch that is at its true
   size and could be there (not another dirt pile: Code picks and says why), big air.
A3. A CREATIVE REVERSE SHORTCUT ON SUHOLDEN. BUG-004 (x 3146.65 z 162.26 h 56, 10 m off Suholden Circle among the yards, reverse race,
   just after the start): "lets put a shortcut through here too. something creative." Code designs it (from Suholden northbound
   through these yards toward Suholden Circle's east end / Embry ahead) using something that belongs in a neighborhood (as the slide
   and trampoline, the boat, the carport, the road crew), not a dirt ramp; says what and why.
   (A2, A3: confirm from the lap which direction each faces; Dan's headings say reverse. Each saves a few seconds clean, loses missed.)
A4. BUG-001 (x 3219.72 z 504.50 h 188, Embry Lane's east corner): "finish this house" (the picture: bare beige walls with no roof or
   windows, open frame). Finish it.
A5. BUG-002 (x 3142.71 z 242.77 h 21, Suholden Circle): "there are lot of these bumps throuhout the course in both idrections. Can we
   flatten these all out". Find the jolts on the Addison Heights lap both ways (bump log / a scripted drive at race speed) and smooth
   them; list where.
A6. BUG-005 (x 2717.40 z 515.53 h 269, Embry Lane before its west bend): "lets put some slow down signs here. Warning of a sharp turn".
   Yellow sharp-curve warning signs with an advisory speed plate, both approaches to the bend (breakable, as all signs).
A7. THE AI IS TOO EASY. Dan: "On the new track. even on hard I beat the AI really bad." Round 52 found why the AI is slow here
   (Embry Lane's 30 % hill, traffic queues at the Kelly / Suholden corner). On Hard the AI should race Dan close (his laps 1:12 to
   1:15 on the Drifter Twin); Normal and Easy scale down as on the other tracks. Report Hard's lap times. Tuning limit.
A8. FEWER SHRUBS AT THE LYNDON SHORTCUT (Round 53 A). Dan, 04:46: "there are far too many shrubs in the Lyndon shortcut. THese aren't
   all necesary just enough to make someone not try to cut through but also look natural." Thin the shrub beds to what still makes
   the yards slower than the jump (keep the cheat check passing), set like real yard plantings (beds along fences, foundations and
   borders, gaps between them), not a carpet.
B. A MISSED FINISH LINE MUST STILL COUNT THE LAP. Dan: "on the trail race [Noonday Creek] that missed the finish cp on my second lap,
   instead of counting it as a missed cp, it didnt advance my lap." Missing the finish / start line is a missed checkpoint like any
   other: the lap advances (on reaching the next checkpoint) and the missed-checkpoint penalty applies. Fix it in the race code for
   every track, the last lap's finish included (Code decides how a missed final line ends the race, and says).
C. COMPLETE RACE, FROM WOODSTOCK RUSH. Dan: "on Woodstock Rush there was the option to simulate the times of the rest of the ai racers
   when you were done with the race. can we implement this here?" Port Racer's COMPLETE RACE (`RaceMenus.Finish.cs`,
   `CanSimulateRemaining` in `RaceMenus.cs`): when the player has finished, a button (with its glyph) fills in the unfinished
   racers' times from their pace and places, straight to the results; records only for real finishes, as Racer.
D. CHASTAIN GLEN LOOP.
D1. BUG-006 (x 3064.10 z 953.77, Chastain Glen Lane, a reverse race): "can we stop having cars trying to go through barriers. They
   should detect the roads are closed and not constantly try to get through them". Traffic must treat a race's closed roads as
   closed: route round them (or turn back), never push at a barrier. Dan, 04:46: "It is a global issue anytime there are barriers,
   cars ignore there are barriers and keep trying to drive throuh them": fix it in the traffic code for every barrier in every race
   (and any other barrier traffic meets), not per track; check at barriers on at least three tracks.
D2. BUG-007 (x 2997.82 z 1209.19, Chastain Glen Lane, Free Roam): "first time I see the sign didn't load." (the picture: the party
   sign with the balloons drawn solid magenta: a missing material / shader in the build). Fix it, and check for any other magenta.
D3. BUG-008 (x 3034.29 z 1185.30 h 98, on the playground slide, R3): "i kept losing momentum on this slide". The slide must not slow
   you (standing rule): find what drags (friction, joints, the lip) and fix it.
E. CHASTAIN LAKES.
E1. BUG-009 (x 684.06 z 1498.46 h 270, North Lakeside Drive just after the start, lap 3): "something right here slows me down". Find
   and remove it.
E2. BUG-010 (x 1166.43 z 1343.72 h 343, airborne 12.7 m off North Lakeside Drive, lap 3, 439 m from the finish): "if this is not a
   natural jump (outside of the shortcut) can we have it land more in line with the track or at least have it jump in that direction".
   If it is not part of a shortcut, shape the take-off so the car flies along the lap and lands on it.
Check: Addison Heights both ways (each shortcut clean / short / round it; the AI on Hard x2 each way with Dan-like lap times; all
finish); B on Noonday Creek (miss the line on lap 2: lap advances, penalty shown; and the final line); C after a finish; D1 traffic at
the Glen loop's barriers; D3 the slide's exit speed before / after; E1 speed through Dan's spot before / after; E2 the flight line.
For Dan to check: Addison Heights both ways (new start, the Elaine jump, A2, A3, Lyndon's shrubs, the signs, the bumps, the AI on Hard); miss a finish
line; COMPLETE RACE; the Glen loop's barriers, the sign and the slide; Chastain Lakes at his two spots.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round54/` (check reports, pictures).
A1 DONE. Line on Crawford Circle's east leg at z 245 (Kelly Drive checkpoint where the line was; old Addison times kept apart, route `records_as`). Elaine jump (forward): off the
   crest down Elaine (Street Classic 1.6 s in the air), then a graded way down the valley's drainage to Crawford 60 m south of the line: 374 m against 507 m of lap, saves
   1.3 s (Street Classic). The flat-out check driver overshoots the T onto Crawford (both vehicles); the AI, which brakes for it, took it 8 times in 4 forward races.
   NOT MET: in the first build rolling down Elaine without flying was as quick as jumping (the jump is the road's own crest); the yard cut toward Crawford is now
   blocked by beds (stops in them).
A2 DONE. Joel terrace (reverse): a timber-walled terrace 10 m out over the 40 % bank, turned to 125 degrees (at 140 a car over Joel's crest could not make the turn).
   Street Classic off at 20 m/s, 1.5 s in the air, saves 0.8 s; Drifter Twin off at 28 m/s lands hard on the slope, 4.4 s slower. Short (12 m/s), and down the bank
   either side: slower than the lap. NOT MET for the Drifter (two tries: heading, the joins).
A3 DONE. Bounce house (reverse): an inflatable castle at the bluff's foot behind 1192 / 1047; in at 10.5 m/s or more it throws you over the bluff and the hill (4.0 s)
   onto Embry's east end: saves 3.5 s (Street Classic), 1.4 s (Drifter Twin); slow (9 m/s) it bounces you back; round it over the hill +9.6 s.
A4 DONE. House 615's carport: floor, ceiling, side beam (as 1089). A5 DONE. The lap's roads levelled across their width to their own smoothed profile (`R54LevelLap`):
   measured twists of 0.2 to 1.4 m in 6 m (lots, drives, junction stems cut into the road edges) and creases up to 0.28 m at Joel/Hayworth, Hayworth/Crawford,
   Suholden/Kelly; ways re-graded onto it. A6 DONE. Turn sign and 15 MPH plate on each approach to Embry's west bend (breakable). A8 DONE. Lyndon's shrub carpet is now
   hedges along the property lines and beds along the houses (cheat check not re-run on them: for Dan).
A7 DONE, NOT MET. AI on Hard, flying laps, 4 races with traffic: before best 82.7 s / median 108.6 s (47 resets in 2 races: traffic, trunks, Joel's crest); after
   best 66.6 s / median 86.4 s. Changes: route `ai_hill_targets` 28/36/44, `ai_crest_g`; the AI passes slower traffic when the other lane is clear; trees beside the lap break.
   Still slow: queues behind traffic stopped at Suholden/Embry and Kelly/Suholden. Easy 93.6 / Normal 95.2 s median (6 laps each: Easy's best 87.6, Normal's 68.7).
B DONE. Past the line outside its arch (within 40 m): the lap counts, +5 s; the last lap ends there. A cut far from the line still counts nothing (so it is no shortcut).
C DONE. Racer's COMPLETE RACE: after you finish, A / Space (glyph shown) or the pause menu row classifies the AI at estimated times. Imported: Racer's button and FinalizeUnfinishedAi.
D1 DONE. Traffic: closures recomputed at every race start (and from the barriers as built), plans re-made off closed roads, a barrier in its lane: stop short, turn round.
   Three races x 90 s: no car stood pushing at a barrier; a car put on a closed road drove at it and turned round.
D2 DONE. The boat's PARTY sign remade the Glen sign's material in the same build: one material per face. Scan of the scene: 1 broken slot before, 0 after.
D3 DONE. R3's slide: the flared foot's rails look only, the ground fitted to the foot, the solid rail ramps in. On its axis 20 m/s loses 3 %, 28 m/s 6 %; 2.5 m off the
   middle at 20 m/s it still stalls (a car's width in the 3.6 m chute).
E1 DONE. MH15's floor collider gave edge contacts (21 m/s to 0): manhole floors now get the true surface normals; 20 and 28 m/s keep 96 %.
E2 DONE. It is the Lake drain shortcut's way out of MH14: a car is turned along the street as it clears it (Drifter Twin: 1 degree off). The Street Classic did not reach MH14 in this check.
Builds: four full builds, not one (after the checks: the slide's foot and rail, the terrace's angle, the bounce house's throw and entry speed, the shortcuts' entry planes
   off the lap). `Builds/ChastainRush/` 2026-10-21 08:49. Tools: `ChastainRush.GroundGrid.Run`, `ChastainRush.MaterialScan.Run` (CLAUDE.md).
For Dan to check: Addison Heights both ways (the new line, Elaine, the terrace on the Drifter, the bounce house, Lyndon's hedges: try to cheat them, the bumps, the signs,
   the AI on Hard); miss a line; COMPLETE RACE; the Glen loop's barriers, the PARTY sign, the slide off its middle; Chastain Lakes at MH15 and out of MH14.

## Previous delivery 2: Round 53
**Reviewed: Dan drove the Round 53 build 2026-10-21 03:39 (Addison Heights both ways, Chastain Glen reverse, Chastain Lakes); no complaint about A (the Lyndon shortcut stands as built), C or D; B: he chose option (1), now Round 54 A1.**
**Round 53, ADDISON HEIGHTS' TWO FORWARD SHORTCUTS, a house and a bump (Dan, 2026-10-20 12:38; reports BUG-001 to BUG-005 of session
`2026-10-20_01-26-12-613_8b4dc1`, read by Claude, after the Round 52 exe: Addison Heights forward and Free Roam on the Drifter Twin).**
Round 52 is reviewed: "the track itself is good". Both shortcuts are FORWARD only. Standing rules apply (real risk; no free way round
a shortcut; tire tracks are the hint, no signs; ramps a real physics jump; nothing slows a car on a ramp). ONE full Build Map at the
end (plan each line on paper first, the run without the stunt included; quick builds while working). Tuning limit. Short results.
A. THE LYNDON DRIVE SHORTCUT: ROAD CREW AND FRESH CONCRETE (Dan's own design). BUG-002 (x 2812.09 z 419.97 h 2, Lyndon Drive at Joel
   Drive, heading north): "Forward shortcut starts here. The challenge is that constructions crews are here working on the road. A whole
   part of the road is all freshly poured concrete if you try to drive through it you are slowed like you would be if you were driving
   in water. A ramp, I am thinking consisting of a trailer that was for the roller and other equipent has its ramp down and there are
   boards leaned up against the truck so as to get some extra height so you can clear the concrete and finish the shortcut". BUG-003
   (x 2810.37 z 511.52 h 25, Lyndon at Embry Lane): "shortcut merges back to the road here". So: off Joel Drive north up Lyndon Drive
   to Embry Lane, skipping the lap's west end. A road-crew site on Lyndon: an equipment trailer (the roller's) with its ramp down and
   boards leaned against the truck for extra height as the launch (real jump), the roller and other equipment parked, cones, then a
   stretch of freshly poured concrete across the whole road that slows you like water (Racer's / Round 9's water drag); clear it in the
   air and you land on firm road and merge onto Embry Lane at Dan's spot. Miss (short, slow, round the trailer) and you are in the wet
   concrete: slower than the plain lap. Note: Lyndon skips about 270 m of the lap (Claude's estimate, about 10 s); the concrete and the
   jump must bring a clean run down to a few seconds saved (as the other shortcuts), Code says the numbers. The barrier at Lyndon (both
   ends) goes; Lyndon south of Joel stays closed. Wet concrete looks wet (darker, glossy) with tire ruts if driven through.
   THE FLAGGER (Dan, 2026-10-21 01:07): "on the Lyndon drive shortcut, I want there to be a man, right before the ramp but to the
   left of it. He should be waving a handheld stop sign, motioning to the driver to stop". A road-crew worker (hard hat, hi-vis vest)
   standing just before the trailer ramp on its left (as you approach northbound), holding a STOP / SLOW paddle with the STOP side
   toward the driver, waving it and his free arm in a stop motion, looping. Clear of the ramp's line so he never blocks the launch;
   if a car does come at him, he gets out of the way as the game's other people do (Claude's addition, not Dan's words).
B. THE ELAINE CIRCLE JUMP. BUG-005 (x 3194.96 z 306.83 h 286, Suholden Circle at Elaine Circle): "Shorcut forward starts here". The
   crest at that corner, then Elaine Circle drops away downhill west: a big natural jump. Dan, asked how to use it (it would cut about
   415 m, some 15 s, so as a plain shortcut everyone would take it, and as an alternate path the whole Kelly / Crawford loop would always
   be skipped): "It does take too much out of the route, but I hate to lose it. ... this jump will miss the track most likely. At least
   every time I tried it. Perhaps we can build off where it lands, have the path be a little longer before you join the main road".
   So: keep the natural jump off the crest as it is (no ramp needed; find where cars really land at race speed, several vehicles);
   from that landing build a longer way back to the lap (through the yards / woods beside Elaine, a winding off-road line with its own
   fun: dips, a small jump, tight between trees), so that a clean run saves only a few seconds. Code picks where it rejoins (the lap
   heading the race's way, e.g. Crawford Circle / Hayworth Drive northbound or Joel Drive westbound) and says why. Elaine Circle's own
   road below the landing must not be a free fast way back (standing rule: brush beds or the like that slow you). The barrier at the
   Elaine corner goes (the crest stays open); the lap's line through that corner is unchanged for those who do not take it.
C. BUG-004 (x 3201.59 z 332.55 h 80, beside Elaine Circle just below the corner, on B's line): "fix this house" (the picture: a huge
   grey wall slab standing up beside the house, the house broken apart). Fix it; B's line passes it, so it must look right from there.
D. BUG-001 (x 2809.71 z 512.68 h 62, Lyndon Drive at Embry Lane, A's merge): "EVEN THIS SPOT OUT" (a crease / bump where Lyndon's crown
   meets Embry's). Smooth the junction so A's landing and merge ride clean.
Check: each shortcut clean (time saved, Street Classic and Drifter Twin), short / missed (slower than the plain lap), and its free
way round (slower); the AI takes each sometimes, forward race x2 traffic Normal, all finish; reverse race unchanged (both shortcuts
shut in reverse). Pictures: Dan's five spots, the crew site with the flagger, the wet concrete, B's landing and its way back, the house.
For Dan to check: both shortcuts forward (try to cheat them); the house at BUG-004; the Lyndon / Embry junction.

**Status (Claude, 2026-10-21): A, C, D built and checked in the exe by script, not yet driven by Dan; B built, driven and taken out (does not fit).** Evidence `Data/screenshots/round53/`.
A DONE, time saved NOT MET. The roller's tilt-deck trailer (2.6 m wide, its true width) with eight planks leaned on its front rack: a real jump, lip 2.35 m at 22 deg
   (`MapBuilder.Round53.cs`). The boards lean on the trailer's rack, not on the truck: a truck in front would stand in the concrete and launch you into its cab.
   So its flatbed truck and the roller are parked on the east lawn, the mixer past Embry. Also: cones, the flagger on the trailer's left with a STOP / SLOW paddle
   (`Neighborhood/Flagger.cs`; he steps off the road if you drive at him), 39.5 m of fresh concrete with ruts (`WetConcrete`), shrub beds in the yards.
   Lyndon's two barriers are open forward only. Clean, flat out: saves 9.8 s (Street Classic) / 4.4 s (Drifter Twin); cars leave the planks at 26 to 28 m/s and fly 44 to 51 m.
   Without the jump, all slower than the lap: round the trailer +0.2 / +0.3 s, through the yards +1.6 s; the east side is blocked by the truck.
   Short off the planks: 15 m/s saves 3.2 s, 18 m/s saves 5.3 s (NOT MET). Tuning (two tries, stopped): first 26 m of concrete at half the drag lost only 3 s round
   the trailer; then 39.5 m, drag 4/s, longer beds. The cause: Lyndon is 123 m against 450 m of lap, so even a fumble saves time.
B NOT BUILT. The finish line is on Kelly Drive, so the way back from Elaine's valley must rejoin Kelly before the line, and Suholden Circle is already the straight way
   there: the woods way was 330 m against 332 m of lap; Drifter Twin 1.7 s slower. Your options: (1) move the start / finish to Crawford Circle's east leg
   (straight, 7 %) so the way can rejoin Crawford or Hayworth and save a few seconds (Code's pick); (2) drop B. Elaine stays barred at the corner.
C DONE. 1089 is a ranch whose carport stood open over the falling yard (no floor, posts in the air). It now has a floor to the ground, a ceiling and a side beam.
D DONE. Junction smoothed along Embry's grade. Merging from Lyndon at 14 m/s: before, all four wheels left the ground (1.24 m/s jolt); now none (0.10 m/s).
   Along Embry at 22 m/s: 1.60 m/s jolt before, 0.14 m/s after.
Races, traffic Normal: forward x2, reverse x1, 9 of 9 AI finish; the AI took Lyndon 4 times, all clean. One missed checkpoint (Ridge Scrambler, no shortcut, as Round 52).
   Barriers: 12 forward and 14 reverse all hold.
Builds: two full builds (the second for A's tuning and B taken out); `Builds/ChastainRush/` 2026-10-21 02:59. Imported from Racer: nothing.
For Dan to check: the Lyndon shortcut (try to cheat it; is a 4 to 10 s saving OK?); pick B's option; the carport at BUG-004; the Lyndon / Embry junction.

## Previous delivery 3: Round 52
**Reviewed: Dan drove the Round 52 build 2026-10-20 01:26 (Addison Heights forward, Free Roam): "the track itself is good"; his notes are Round 53.**
**Round 52, A NEW TRACK: ADDISON HEIGHTS (forward and reverse, no shortcuts yet), and three house fixes there (Dan, 2026-10-19 22:23;
reports BUG-001 to BUG-003 of session `2026-10-19_19-45-10-079_0a133b`, read by Claude, from Free Roam in the Round 51 exe; his route
on a Google map: `local/reference_photos/embry_lane/r52_route_map.jpg`, reference only, never traced or measured).** Round 51 is
reviewed. Dan: "For right now I'd like to add another track. Can we have it built forward and reverse but not worry about the
shortcuts. I want to decide once the track is there. I don't care which way is forward and reverse." ONE full Build Map at the end
(quick builds while working). Tuning limit. Short results.
A. THE TRACK. The neighborhood south of New Chastain Road, west of Canton Road. Dan's red line, read by Claude against `Data/map.json`
   (Dan, 22:38: "The roads matched the game roads, they just arent all visible in the zoomed out map I gave you"; use the
   game's own roads and names: the short leg north from Crawford Circle to Joel Drive is Hayworth Drive in the data):
   Joel Drive west from Hayworth Drive (about x 3000 z 414) to its west end (2645, 423), round the bend into Embry Lane NE and
   east along it (to 3231, 528), down Embry's east end (3245, 470; 3258, 412) to Suholden Circle NE (3284, 300), west on Suholden
   Circle to its bend (3196, 303) and south on it to Kelly Drive NE (about x 3141), west on Kelly Drive to Crawford Circle (2987,
   about z 100), north on Crawford Circle's east leg and Hayworth Drive back to Joel Drive. About 2.0 km a lap (Claude's estimate).
   Forward = that direction, reverse = the other (Dan does not mind which). Built like the other races (`Data/routes/*.json`,
   `Data/tracks/`, menus, records, AI, ghosts, checkpoints, the loading screen's route inset); 3 laps by default. Start / finish on
   a straight Code picks and says where. Name: "Addison Heights" (Dan, 22:38: "I could be wrong but I think this is called
   Addison Heights"; Claude could not confirm it online, so the name is Dan's and easy to change).
   NO SHORTCUTS this round. Dan's yellow marks are his possible shortcut spots, not built: Lyndon Drive between Embry Lane and Joel
   Drive, and Elaine Circle from Joel Drive to Suholden Circle. Every road that leaves the lap is closed in a race (standing rule),
   these two included: Lyndon Drive (both sides of Joel), Elaine Circle, Joel Drive east of Hayworth to Embry, Hayworth north of
   Joel, Crawford Circle's west leg, Kelly Drive west of Crawford and east of Suholden, Suholden Circle east toward Hilltop, and
   Suholden south of Kelly. The red dab at Chance Road / Suholden Drive is a stray mark (Dan, 22:38): ignore it.
B. THE HOUSES (all three on or by the new lap):
   BUG-001 (x 2764.81 z 565.33 h 179, behind the houses between Embry Lane and Weybourne Court): "House on left is buried. House on
   the right has windows hanging in the air". BUG-002 (x 3231.02 z 503.06 h 240, Embry Lane NE at its east corner): "thiss house is
   buried and incomplete". BUG-003 (x 2943.80 z 419.43 h 3, Joel Drive): "this driveway needs to be fixed. Also the house entrance
   is hanging in the air" (the drive climbs the bank to nowhere with a hole in it; the front door floats above the brick base, no steps).
   Find why houses on these steep lots are sunk or floating and fix it at Dan's three spots; the same fault on the houses that face
   the new lap is fixed too, since Dan will drive past them (Claude's scope, the lap's roads only, said here so Dan knows). Drive
   at BUG-003: from the road up to the house's garage, at a drivable grade, no hole; the entrance with steps or a stoop down to the ground.
Check: forward and reverse, AI races x2 each, traffic Normal: all finish, 0 missed; every closure shut (try each road above);
the lap on all 11 vehicles both ways, no stop. Pictures: Dan's three spots before and after, the start / finish, each closure.
For Dan to check: drive Addison Heights both ways (and pick its shortcuts); the houses at his three spots and along the lap.

**Status (Claude, 2026-10-20): A and B built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round52/`.
A DONE. `python Tools/mapgen/make_addison_route.py [--plot]` writes `Data/routes/addison_heights.json` (and `Data/tracks/addison_heights.json`, one cleared line).
   Lap 2040 m forward (Dan's direction), 2065 m reverse (its own lane), 3 laps, traffic, 8 checkpoints. START / FINISH on Kelly Drive at x 3055 (between
   Suholden and Crawford: 155 m straight, 5 to 8 %); not Joel Drive, which falls 40 % and more west of Hayworth. 14 barriers each way, every road in the brief.
   Check (`round52_check.txt`, 62 of 62, build 2): a lap each way on all 14 vehicles, none stops (74 to 82 s); all 28 barrier tries hold (driven at from the lap).
   AI races x2 each way, traffic Normal: every AI finishes all 3 laps in all 8 races (24 of 24), but NOT MET "0 missed": one AI missed one checkpoint in each of the
   last two runs (build 3: in reverse, it hit a trunk in the gutter on Embry Lane's 30 % hill and ended in a yard, CP4; final build: a Needle 600, forward CP8
   on Suholden Circle's downhill, cause not found). One fix tried (no trunk within 1.5 m of the lap's road edge, the cleared line), then stopped (tuning limit).
   The AI is slow here: laps 84 to 174 s against the check driver's 75 s (traffic queues at the Kelly / Suholden corner; AI drivers leave the road on Embry Lane's
   30 % hill). `round52_check_final.txt` (its two lap failures are the check's own driver with traffic left on: behind a traffic van at that corner).
B DONE. Cause: the other neighborhoods' houses stand at the ground at their garage doors and nothing round them was graded (Round 27's settling ran for the Glen
   and the communities only); their drives were mapgen's straight lines draped at any grade (BUG-003: 39 %, the ground bulging through); no steps at any door.
   Style E (split-level) also drew its upstairs back windows across its one-story wing, over the roof (BUG-001's right house, 996). Fixed for Dan's four houses
   (910, 996, 615, 1135) and the 69 that face the lap (list `houses_settled.txt`, `MapBuilder.Round52.cs`): the floor where a drive from the street reaches it at
   20 % or less (23 floors moved, 1135 down 4.3 m), the drive from the street straight to the garage graded under it, the lot cut with a yard that falls to the
   house at about 1 in 3 (Round 27's 40 degree bank still looked sunk from Dan's spots: tried first, changed), a stoop and steps at the door, E's windows on its block.
   Pictures `bug00N_before.png` / `_after.png` / `_after_wide.png`.
Builds: four full builds, not one (the first stopped on a half-written check file; the softer lot cut and the cleared line were both found in the exe).
   `Builds/ChastainRush/` 2026-10-20 00:39. Imported from Racer: nothing.
Not changed (Dan's call): the same floating windows on about 100 other style E houses elsewhere on the map; the same unsettled houses and steep drives in the other
   neighborhoods (Round 30's log counts 501 with ground over their floor).
For Dan to check: drive Addison Heights both ways (and pick its shortcuts); the houses at your three spots and along the lap; is Kelly Drive right for the start?
   Watch the AI on Embry Lane's hill (do you want AI speed zones there, or wider checkpoints?).

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
