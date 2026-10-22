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
   4.6 m clear. B28's glass bed has the same fault as S2's, inside M2a (sealed in this race, open in free roam): listed for Dan, not changed.
E no change.
F DONE (code only: no map rebuild, as the round says). Every race is recorded: racers 25 times a second (pose, speed, steering, pedal), traffic 10 times a second,
   and breaks, restores, trees, plumes, switch gates and gestures. Engines play from the replay; the radio plays live. Finish screen: WATCH REPLAY (X / V) and SAVE REPLAY (Y / K).
   Viewer: A play / pause, RT fast forward, LT rewind, X slow motion, Y camera (TV, chase, free), LB / RB racer, D-pad lap / restart / HUD (HUD off by default), View save, B leave.
   The REPLAYS page is on the title, free roam and race menus. Each #1 Top 10 lap and race per course and direction is kept, and a new #1 replaces it. The page marks a replay
   recorded on another build or before its course changed. Check, Addison 3 laps, 3 AI, Normal traffic: replay within 0.006 m of the race (416 samples); 12 of 12 breaks on time
   (mailboxes, lamps, the barricade and its restores); every camera, every pad and key control, save, the page, playing from the page: 19 of 19.
   #1s: slow kept, faster replaced, slower left alone (4 of 4). Size: 1.1 to 1.7 MB for a 4 to 5 minute 3-lap race with Normal traffic.
   The Lyndon jump was in an earlier run's replay; in the final run the check's driver got stuck in traffic and was put back past it.
`Builds/ChastainRush/` 2026-10-21 23:15. Imported from Racer: nothing.
For Dan to check: the corner both ways; COMPLETE RACE with A; M5c and the hub's rails; a replay with each camera, the #1s, save one and find it on REPLAYS.

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

## Previous delivery 1 (newest): Round 56
**Reviewed: Dan drove the Round 56 build 2026-10-21 21:15 (Addison Heights, Underground); no complaint about the Elaine shortcut, the barricade, the jerk or Joel Drive; his notes are Round 57.**
**Round 56, THE ELAINE SHORTCUT: LAND ON CRAWFORD CIRCLE, A WAY BACK FROM THERE, A BLOCKED ENTRY WITH TIRE MARKS; a jerk on Elaine;
the things on Joel Drive (Dan, 2026-10-21 17:08; reports BUG-001 to BUG-004 of session `2026-10-21_17-00-50-815_f90718`, read by
Claude, after the Round 55 exe: Addison Heights forward on the Drifter Twin).** Round 55 is reviewed. Dan: "still not quite hitting the
mark on the Elaine shortcut". Standing rules apply. ONE full Build Map at the end (plan the line on paper first; quick builds while
working). Tuning limit. Short results.
A. WHERE A GOOD JUMP LANDS, AND THE WAY BACK FROM THERE. BUG-004 (x 2947.19 z 315.69 h 141, ON Crawford Circle's west leg, the closed
   road between the Hayworth / Crawford corner and Lyndon Drive, a barrier just ahead): "A good jump will tak you here. The path should
   work back from the main road from here. If it is off the main track for a bit as long as it joins back with some time savings".
   So the target is no longer Round 55's bench and hedges on the bank: a GOOD jump off Elaine's west bend (Round 55's launch, natural,
   no ramp) carries you over the bank and down onto Crawford Circle's west leg at Dan's spot. From there the shortcut is a way back to
   the lap, off the main track for a while, joining it heading the race's way with SOME time saved (Dan accepts more than "a few
   seconds" here; Code reports the number). Code picks the way back and says why (e.g. west along Crawford's west leg, then Lyndon
   Drive north to Joel Drive westbound; if so, decide what happens at Joel / Lyndon, where the road-crew shortcut starts straight
   ahead: no accidental chaining unless it is fair; say what was chosen).
   A SHORT jump (not enough speed, off line) comes down on the bank / at the corner and pays for it (hedges or similar, as Round 55),
   so the good jump is the reward. Make the launch able to reach Dan's spot at a good race speed on the Drifter Twin and the Street
   Classic (natural lip only shaped, as before; tuning limit). Remove what Round 55 built that this does not need (bench, gaps, hedges
   that now block the good line) and keep what still makes the short jump costly. Crawford's west-leg barrier moves so the landing and
   the way back are open forward and stay shut in reverse.
B. THE ENTRY: BLOCKED, WITH TIRE MARKS. BUG-003 (x 3205.88 z 303.90 h 287, Suholden Circle at the Elaine turn-off): "there should be an
   indicator that the road to the right is a shortcut. Maybe actually block the road off and make tiremarks right after it". Put a
   breakable road-closed barricade (sawhorses / a striped board, as a road crew would) across Elaine's mouth, with tire marks on the
   road right after it, so it reads as "people go through here". Breaking it must not slow you (as fences); it resets each lap / race.
C. BUG-002 (x 3088.66 z 307.72 h 294, Elaine Circle on the shortcut's run-up): "there is something here that causes my vehicle to jerk to
   the righ. Please check the road in this area". Find it (a ridge, a camber change, an edge, a collider) and fix it; check at speed.
D. BUG-001 (x 2963.10 z 411.11 h 264, Joel Drive west of Hayworth, on the lap): "why is this here? and what is it?" (the picture: orange
   round bases with red tops standing in the road and beside it). Say what they are; if they have no purpose a racer can read, remove them.
Check: the good jump (Drifter Twin and Street Classic: launch speed, where they land, time saved), a short jump (cost), the ways round;
the barricade breaks without slowing; the AI takes it sometimes, forward race x2 traffic Normal, all finish; reverse unchanged.
Pictures: Dan's four spots, the jump from behind, the landing on Crawford, the way back.
For Dan to check: the Elaine shortcut (good and short jumps, the barricade and tire marks); the jerk on Elaine; Joel Drive at his spot.

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round56/` (check reports, pictures).
Bug reports read: session `2026-10-21_17-00-50-815_f90718`, BUG-001 to 004.
A DONE. Round 55's bench and its hedge gap gone. The lip: the bend's crest on Elaine's own line, raised 1 m to an edge (20 %, no ramp), heading 267. Flat out a
   Street Classic leaves it at 30 m/s and comes down 87 m out at (2960, 313), Dan's spot; at 21 m/s 44 m out, on the lower bank (a landing zone). The run on:
   through a gap in the bank's foot hedge, over the Crawford / Hayworth corner, down Crawford's west leg. The way back (Code's pick): from Dan's spot north
   between 884 and 1143, round 1143, up onto Hayworth by CP 5, graded at 22 % with worn tracks; it never reaches Joel / Lyndon, so it cannot run on into
   the road-crew shortcut. Crawford's west-leg barrier moved 56 m past the way back (forward only; reverse unchanged). The short jump comes down in
   brush, 18 to 36 m out, at twice the usual drag (`ShrubBeds` strength). Times against the lap's way (`round56_check_final_a_c.txt`): clean, Street Classic
   saves 6.0 s, Drifter Twin 6.0 s. Against the lap: short (15 m/s off the lip) +0.0 s, a tie; no jump +6.5 s; round the barrier +1.4 s.
   Turning up Hayworth at the corner after the jump: Drifter Twin 17.4 s against 19.1 s round 1143 (a quicker way for a rider who can stop).
   Two layout attempts: the first lip, 2 m north of Elaine's line, turned cars into house 960 (`round56_check_a_try1.txt`, `_try2_probe.txt`).
B DONE. Three breakable type III barricades across Elaine 17 m in from Suholden (ahead on the right from Dan's spot), tire marks up Elaine behind them.
   Forward race only. 22 m/s through them: 21.7 then 21.8 m/s, no speed lost. Whole again 7 s after the car leaves (`RestoreWhenClear`).
C DONE (Dan watched the check drive it: "half way up the hill to the ramp to the right side of the road"). Traced flat out from Suholden: at 26 m/s, where
   the climb steepens from 17 to 31 %, the suspension squeezes until the body's right front corner dips 10 cm into the road (x 3091; the Drifter Twin
   at 3075 and 3098): kicked up 0.3 m, rolled 7 degrees, heading swung right at up to 470 deg/s (`round56_check_c_before.txt`). Fixed there only: on the
   climb the road does not push on a body the right way up (the wheels carry it; `BodyGlide`, `VehicleSurfaceContacts.Glide`, track `body_glide`).
   After: no swing on the climb for either vehicle (`round56_check_final_a_c.txt`). Elaine's valley also made one even vertical curve (x 3155 to 3078).
D DONE. Two flower pots (Round 54's, at the Joel terrace's street corners); no purpose for a racer, removed.
Races: forward x2 and reverse x1, Hard, traffic Normal, 9 of 9 AI finished; the AI took the Elaine jump 5 times (`round56_check_races.txt`).
Builds: seven full builds and one stopped (two layout tries, the barrier move, the barricade moved and drawn in place, the jerk's fix twice).
`Builds/ChastainRush/` 2026-10-21 20:52. Imported from Racer: nothing.
For Dan to check: the Elaine jump flat out (land on Crawford, the way back round 1143) and short (into the brush; a tie with the lap: enough?); the barricade and
tire marks; Elaine's valley at speed (the jerk); Joel Drive at his spot.

## Previous delivery 2: Round 55
**Reviewed: Dan drove the Round 55 build 2026-10-21 17:00 (Addison Heights forward): the Elaine shortcut is "still not quite hitting the mark" (Round 56); B to E no complaint.**
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

**Status (Claude, 2026-10-21): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round55/` (check reports,
pictures). Bug reports read: session `2026-10-21_12-16-40-990_d377b3`, BUG-001 to 003.
A BUILT AS DAN'S OPTION 2 (Dan, 2026-10-21 after the first report: "option 2"); TIME NOT MET, TUNING LIMIT REACHED. Round 54's valley way is gone. Measured
   first (`round55_check_a_b_c.txt`): up Elaine's 26 % cars reach the bend at 14 to 21 m/s and come down on the bank 35 to 46 m out, just above the
   Crawford / Hayworth corner (with nothing there: lap 25.0 s, land and turn at the corner 14.4 s, Drifter Twin). Built: off the bend's own edge (no ramp),
   worn tracks down the bank (the hint), a landing zone, a graded bench curving right off the landing, north between 960 and the hedge, an S through the
   wedge, onto Hayworth through a gap at z 376 to 392 (forgives CP 2 to 5); a 6 m hedge along the bank's foot from Crawford Circle up Hayworth's east side to
   z 373; Elaine barred north of the bend (16 m wide, forward), hedges at its ends, across 960's yard and across the wedge; the 1007 / 2090 bank hedged.
   Numbers (`round55_check_a_option2_*.txt`; the scripted driver lands at 20 m/s about 20 m from the hedge and must turn hard right, so runs vary by 3 s):
   clean, Street Classic saves 8.7 s (try 1) and 4.6 s (try 2), and in the final build ran wide into the hedge; Drifter Twin saves 5.4, 5.0 and 4.9 s.
   Ways round, final build: straight down through the hedge at the corner 18.6 s (cars can bounce over a 6 m hedge off the bank; 8.5 m caught the clean
   run too, tried and reverted); off the bench west through the hedge 25.2 s; over the edge slowly and the bench 20.1 s (the same way, no air); round the
   barrier through 960's drive stopped in the hedges (it was 16.5 s before the barrier was widened). Lap's way 28.3 s (Street Classic), 26.0 s (Drifter).
   The AI took it 5 times in two forward races (try 2's layout), every AI finished.
B DONE. The line on Embry Lane's east end at (3246, 457) (5 to 9 %, straight; every flatter straight is inside a shortcut's skipped part or too short),
   60 m lead-in from the corner; the reverse grid ends short of Joel Drive. Records as "Addison Heights (Embry Lane line)"; a checkpoint on Crawford
   Circle where the line was. Both grids on the road (check); no shortcut end in sight of them (`55_B_*_grid.png`).
C DONE. The bounce house's voice stood at the world's origin (never heard); now at the castle, playing the trampoline's sound (Dan's drop-in
   `trampoline.mp3` or the made one). Check: thrown, playing, 3 m from the arch.
D DONE. Both were ridges the levelling left on the corners' outsides: Kelly / Suholden (pitch under a car 0, 5, 12, 18, 14, 8 %) now 3 to 8 % with no
   spike; Hayworth / Crawford (0.31 m and 0.6 m crests over +-3 m) blurred, and the 8 % into 22 % grade change made a 24 m vertical curve
   (`MapBuilder.Round55.cs`): no crest left, a sag of 0.17 to 0.20 m over +-3 m remains where the bend and the grade change meet (two tries; `bumps.txt`).
E DONE (modest). The AI pulls out round a slower car once the other lane is clear and goes on past instead of stopping on its bumper; otherwise it waits
   a car length back. Hard, traffic Normal, every AI finished (8 races, 24 drivers): close behind a car under 10 m/s on Addison Heights 23.7 s per driver
   before, 17.1 s after; Chastain Lakes about 2 s before and after. Passes 14 to 18 per driver both ways (`round55_check_e_before.txt`, `_after.txt`).
Builds: nine full builds (the junction curve took two; option 2 took five: layout, two tunings, the barrier, the landing arc). `Builds/ChastainRush/` 2026-10-21 16:11. Imported from Racer: nothing.
For Dan to check: the Elaine jump (land, brake hard, right onto the bench; is the corner through the hedge too easy?); the new start / finish both ways; the bounce house's sound; the two bumps; the AI round traffic.

## Previous delivery 3: Round 54
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
