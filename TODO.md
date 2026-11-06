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

**Check runs never sit stuck — STANDING RULE (Dan, 2026-11-05 05:05).** Dan, with a picture of a check race's bike wedged upright against a building wall at lap 21:34:
"I dont understand why it does this all the time. I just see it stuck for 20+ minutes doing nothing. this isn't the first time." From now on: (1) the human-like driver and
pilot reset themselves the way a player would (the reset button) after about 5 s with no progress along the course, and the check logs it with a picture; (2) a race in a
check that makes no progress for 2 minutes is ended and reported as STUCK (where, what it was against), never left to the run's long watchdog (150 / 330 minutes); (3) a
check run's total time budget is set from the work (laps x expected lap time x 2), not a blanket hour count; (4) Claude Code watches a running check's log and stops it
as soon as it is stuck or clearly failing, instead of waiting it out. Copy this rule into CLAUDE.md's "Process" in the same words.

## Process (Dan, 2026-10-15; full rules in CLAUDE.md "Process")
This file holds only: the standing rules above, the CURRENT round, QUEUED rounds, the last three "Previous delivery"
sections, and the technical notes at the end. Everything older is in `TODO-ARCHIVE.md`, word for word: search it when
needed, never read it in full. When Dan marks a round reviewed, it becomes "Previous delivery 1"; in the same commit the
oldest one (delivery 4) is appended to the end of `TODO-ARCHIVE.md` word for word under a dated heading.

## CURRENT round
**Round 94, NEW CARS (AND PLANES) WORTH DRIVING, UNLOCKS BEHIND THE GOOSE EGGS, ROUND 93's LEFTOVERS (Dan, 2026-11-05 20:28).** Round 93 is not yet driven by Dan. Item 0, the
one-heavy-job rule, the check-runs-never-sit-stuck rule and the end-of-round clean-up apply. Short results. The campaign is the next round (QUEUED), and its event rewards use the
unlock system built here.
Dan: "I would like to get some new vehicles in general. For example literally none of the cars appeal to me. I would love to have some nicer more realistic looking cars with some
speed and handling. Would love cars looking like 1961 Jaguar E-type, 1965 Shelby Cobra, lambos, porshes etc these could be unlockables from events or eggs. We could also hide some
planes behind the eggs as well".
A. A NEW CAR LINE-UP. At least eight new cars, clearly nicer and more realistic-looking than the current ones (more shape detail, proper proportions, chrome / glass / lights,
   interior seen through the windows with the driver, wheels and tires that read as the real kind), still in the game's look and frame budget (Steam Deck setting included).
   Styled after the classics and supercars Dan named, as OUR OWN designs with invented model names, no real brand names, badges or logos anywhere (no trademarked marks), e.g.:
   a long-hood 1960s British roadster coupe (E-type style); a small 1960s V8 roadster with side pipes (Cobra style); a 1970s wedge supercar and a modern angular V12 supercar
   (Lamborghini style); a rear-engined 1960s / 1970s sports coupe and a modern one (Porsche 911 style); plus two of Claude's choice that fit (e.g. a 1960s muscle car, a modern
   grand tourer). Each its own handling and sound (Woodstock Rush's engine sounds or new ones that fit: a V8 rumble, a flat-six, a V12 scream), fast and grippy as its type
   (supercars quickest, classics lively but slidier), every one fun on the courses, AI able to drive them; colors in the garage (three or more each). Blender generators as the
   existing cars (`Tools/Blender/cars.py` and kit), committed. Pictures of each in the garage and on the road.
B. NEW PLANES. Two or three more planes in the plane garage (e.g. a vintage racing monoplane, a jet trainer, a biplane), each its own speed and handling, windows with the pilot
   visible, colors, Round 93 B's controls; AI pilots can fly them.
C. UNLOCKS. Most new vehicles start locked: a lock icon and "How to unlock" in the garage. The goose egg tiers (10, 25, 60, 125, 190, 250) now unlock vehicles: the planes and some
   cars behind the eggs (the best ones at the top tiers); the rest are left for the campaign's events (next round) and listed for it. The cars and planes that exist today stay
   unlocked. HIGH MODE and split screen: everything unlocked, as now. A banner when something unlocks; Dan's save keeps what he has reached (his 19 eggs: tier 10 already
   gives its vehicle). A debug-menu "unlock all" for testing. Say the table (tier -> vehicle).
D. ROUND 93's LEFTOVERS. The small hops left on the quarry's jump pieces and at The Long One's run-up head; the 0.66 s hop at Wings & Wheels' tube mouth; the U-turn at Tower Road's
   west end widened or eased so a bike at race speed gets round without hitting the barrier (Round 87's rules); the planes' lap on Wings & Wheels lengthened with another ring or
   two (no slow rings) until within about 5 % of the cars (two attempts, then report).
Check: item 0 for every item; every new vehicle driven / flown a lap of two courses by the human-like driver / pilot and raced by AI; the unlock table proven on a copy of Dan's save.
PUBLISH through the launcher (prove the update from 0.93.0).
For Dan to check: the new cars and planes (the garage, a race in each); what is locked and how; the Round 93 leftovers.

**Status (Claude, 2026-11-06): A to C built and checked in the exe; D partly (below). 1 full Build Map + 2 code-only exe builds.** Evidence `Data/screenshots/round94/`. No bug report newer than Round 87's.
A DONE. Eight cars of our own design, invented names, no marks (`Tools/Blender/cars94.py`; one parametric body / greenhouse / cockpit builder): Copperhead V8 (60s V8 roadster, side pipes), Marlow Sprint (60s long-bonnet coupe, wire wheels), Falke 2.0 (60s rear-engined coupe), Brawler 440 (60s muscle car), Meridian GT (modern front-V8 GT), Stiletto 400 (70s wedge supercar), Falke R (modern rear-engined), Tempesta V12 (modern V12 supercar). Glass, chrome, lamps, seats / dash / gauges with the driver seen through the windows, brake discs and calipers, wire / alloy / center-lock wheels; 21 to 25k triangles each. Own engine notes (`VehicleSoundSynthesis.Voiced`: v8, six, flat6, v12). Top speed 53.5 to 61 m/s (Tempesta = the Needle's), grip 22.5 (Brawler) to 31 (Tempesta). The garage's 7 colors as every vehicle. Low cars leave the rider's hat off (`VehicleProfile.LowRoof`). Human-like driver, a lap of Chastain Glen and Addison Heights in each: 16 of 16 finished (Glen 0:44 to 0:55); the Tempesta needed 4 resets on Addison Heights, each against a tree trunk off the line (the Copperhead hit the same tree once). AI fields of all eight on Chastain Glen: every AI finishes (laps 0:41 to 0:45).
B DONE. Barnstormer (biplane, open cockpit, top 105 m/s, the most agile), Hornet Racer (30s air racer, 150), Kestrel Jet (jet trainer, 165; jet sound, the biplane and racer a radial) (`Tools/Blender/planes94.py`); Round 93's stick / LB-RB controls. Human-like pilot: Quarry Air Race 2:18 / 1:26 / 1:22, Air Tour 5:53 / 3:56 / 3:41 (the racer and jet missed one Air Tour ring each). AI pilots now fly every plane a route takes (they were held to the Sky Hopper / Dart): all three finished the Quarry Air Race.
C DONE. `Vehicle/VehicleUnlocks.cs`. Table: 10 eggs Copperhead V8; 25 Barnstormer; 60 Marlow Sprint; 125 Hornet Racer; 190 Kestrel Jet; 250 Tempesta V12. Left for Round 95's campaign: Falke 2.0, Brawler 440, Meridian GT, Stiletto 400, Falke R. Every older vehicle stays open; HIGH MODE, split screen and the debug menu's "Unlock all vehicles (testing)" have everything; the AI may race any car. Garage: a locked vehicle is shown in the list with a padlock, its stats and "HOW TO UNLOCK: Find 60 goose eggs in Free Roam (21 / 60)", never put on (`94_garage_locked_*.png`). The egg tier banner names what it unlocks; a vehicle already earned is announced once in Free Roam. GOOSE EGGS page lists the prizes per tier. On a copy of Dan's save (21 eggs, tier 10): Copperhead open, the rest locked, its banner shown (`94_unlock_banner_earlier.png`); eggs taken to 25: "UNLOCKED: Barnstormer" and it opens.
D PARTLY. (1) The Long One's run-up head: smoothed with the lap (`make_round93_smooth.py`); no hop there in 4 laps (was every run). The rest of the quarry's log is landing beside a deck or crawling out of the pit after a short jump (designed): not changed. (2) The tube's way in smoothed to 3 m short of X_KEN's mouth: the 0.66 s step hop at the mouth not seen, but drivers still miss the mouth on the tight turn-off loop and bounce round it (hops 0.4 to 2.4 s at 6 to 25 m/s). (3) The U-turn: its barrier row 6 m further out round the apex (route `u_turns` `room`) and, attempt 2, held out along the exit: (result below). (4) The planes' lap 11.5 -> 12.2 km (NA and WS further out); times below.
For Dan to check: the eight cars and three planes (look, sound, handling; HIGH MODE or the debug menu's Unlock all to try the locked ones); the garage's padlock and the Copperhead banner on your save; the U-turn on a bike; the planes' Wings & Wheels lap against the cars.

## QUEUED rounds
**Round 95: THE CAMPAIGN (Dan, 2026-11-05 20:28: "we can work on adding a campaign. I think we have a fair amount of tracks now that will make this worth while";
held since 2026-10-16 until there were more tracks).** Port Woodstock Rush's campaign (`Racer/Assets/Scripts/Campaign*.cs`, `RaceMenus.Campaign.cs`, its chapters, events, results,
progress saves; Racer read only, port rather than rewrite) and build Chastain Rush's on it: chapters that open the map in an order that makes sense (Chastain Glen first, as the
starter, then the neighborhoods, the creek / trails, Town Center and the Noonday Creek Trail, Kennesaw, Marietta Square, the quarry, the air races and Wings & Wheels), events
of every kind the game has (races forward / reverse, time trials, stunt park and quarry jumps, police chases and Speed Patrol, air races, Wings & Wheels, the Underground race),
medals or placings to pass. STUNT TRACKS (Dan, 2026-11-05 20:33: "for stunts we can use kennesaw and the quarry as stunt tracks (they can be used as standard
tracks as well)"): the Kennesaw Mountain trails and Viking Quarry also run as stunt events (scored on air time, distance, flips / rolls, clean landings, combos, as the stunt
park's scoring; Woodstock Rush's Stunt Races if they fit), on the same courses, alongside their normal races; offered in race setup and in the campaign. And the vehicles Round 94 C left for the campaign as event rewards (and Woodstock Rush's shop / cups / playlists only if they fit: say). The
campaign never changes Free Roam or HIGH MODE's everything-unlocked. Its menu reachable by controller, readable on the Deck. Check: the first two chapters played through by the
human-like driver on a test save; a reward unlocking; the save surviving an update.

## Previous delivery 1 (newest): Round 93
**Not yet driven by Dan (0.93.0 published); he reviews it with Round 94.**
**Round 93, THE QUARRY BUMP AFTER THE BIG ONE, PLANE ROLLS ONLY WITH A SHOULDER BUTTON, WINGS & WHEELS SMOOTHED (Dan, 2026-11-05 00:25, a photo of his screen on 0.92, no report).** Round 92 is reviewed
(the update reached him). Item 0, the one-heavy-job rule and the end-of-round clean-up apply. Short results. (Dan, 00:33: the held quarry notes from 0.90 are irrelevant now: dropped.)
A. THE QUARRY BUMP AFTER THE BIG ONE. Photo (lap 1, "CHECKPOINT 5 / 22" 340 m ahead: between "The Big One's run-out" (CP4, s 1782) and "the pit floor" (CP5, s 2180), so about s 1840;
   the gravel road with a sheet of grass lying across it right in front of the bike (the ground standing up through the gravel), and ahead at the bend a jagged green wall of
   ground spikes): "this needs to be fixed. It is an unwanted bump". The gravel laid over the ground there with no grass through it, one smooth surface, no lip; the spiky wall at
   the bend cut back to a clean bank. Then the whole lap the same way: everywhere the ground pokes through or stands proud of the course's gravel (a probe along the course,
   both edges and the middle), and every unwanted hop over 0.3 s on the human-like driver's line (the course's designed jumps excepted). Check: the bump log of three laps on the
   bike and a car, normal and HIGH: no unwanted hop; a picture from Dan's spot before / after.
B. PLANES: ROLL ONLY WITH A SHOULDER BUTTON. Dan: "I don't like the way the planes fly after the update. To roll lets make it so you have to press the shoulder buttons. Shoulder
   buttons in conjunction with the stick will cause you to roll without the shoulder button you turn just like needle". Round 91 B made the stick roll freely. Now:
   - Without a shoulder button: the stick flies the plane as before Round 91 (bank-and-turn, limited bank and pitch, wings level themselves), turning like the Needle
     (the stick left / right turns the plane, as easy as steering a bike).
   - Holding a shoulder button (LB or RB, either; keyboard: its own key) with the stick: the plane rolls all the way round (left / right) and pitches all the way round for
     loops (up / down); let go of the shoulder button and it levels the near way, as Round 91 B's recovery.
   - BARREL ROLL / LOOP notes stay; HIGH's pull-out near the ground stays; AI pilots unchanged. The control hints on screen and in the menus say it (only the device in use).
   - Check what the shoulder buttons did in a plane before (throttle? camera?) and move that elsewhere if needed, saying where.
   Check: controller input in both planes: a turn without the shoulder (bank limited, no roll past it), a roll each way and an inside loop with it, level after letting go.
C. WINGS & WHEELS: SMOOTH ROADS, AN OBVIOUS U-TURN, NO SLOW GATES (Dan, 2026-11-05 00:33, after racing it on the road and winning: "There was one spot on the wing and wheels
   race that also need clean up to be more smooth. So please check this course as well for unsmooth roads. I think on that race since there is a uturn lets make it more obvious.
   I don't like the idea of airplane checkpoints where you have to fly at a different speed. Can't we add a couple more checkpoints to make their course a little longer").
   (1) The car's lap (Cherokee, Tower Road, Church, Hansell, the tube): every unwanted hop or jolt found and smoothed, as A (bump log of three laps, bike and car, normal and HIGH;
   none over 0.3 s, the tube's own entry and exit drives clean too). (2) The U-turn on the car's lap (say where): plain to see coming up: the turn's checkpoint arch placed so it
   faces you, the barriers / bumpers along the turn's outside guiding you round (never across, Round 87), a U-turn arrow board before it, and the minimap's line showing it.
   (3) The planes: no slow gates (remove every ring that must be taken under a speed); instead a couple more rings that make the planes' way a little longer (and the low rings
   under bridges stay); retune toward the car's time (two attempts, then report the car / plane laps per difficulty). Check: three races (cars, planes, mixed) with times;
   the human-like driver takes the U-turn without missing its checkpoint.
D. A WEDGED VEHICLE AND A RESET THAT SKIPPED THE LAP (found 2026-11-05 05:05). (1) Dan's picture: the bike stood upright on its tail against a building's wall beside the
   road, wheels off the ground, and no reset came for 20+ minutes. In a race (normal and HIGH) a player's vehicle that is wedged, on its tail or nose, or not moving along the
   course for about 5 s is offered / given a reset as an AI's is (HIGH: automatic; normal: a "Press Y to reset" prompt, automatic after about 8 s); say which race and
   where. (2) `local/round93/after_hops1/Player.log`, the Viking Quarry race: "[Race] RESET ... placed on from lap 7306 m to 1028 m; checkpoints 3, 4, 5, ... 22 forgiven"
   (four times): a reset near the lap's end put the car back to The Big One's run-up and FORGAVE checkpoints 3 to 22, breaking Round 86 B (a reset never skips a
   checkpoint; it goes before the next one not yet passed). Find why (the lap's wrap at the finish, the lap station measured against the next lap) and fix it for every
   course. Check: resets forced at each checkpoint's approach and just before the finish line on the quarry, Wings & Wheels and two other courses: none forgives a
   checkpoint it did not pass, none moves you a lap.
Check: item 0 for every item. PUBLISH through the launcher (prove the update from 0.92.1 the way Dan gets it).
For Dan to check: the quarry after The Big One; flying both planes (turns, rolls, loops); Wings & Wheels (the smooth lap, the U-turn, the planes' longer way).

**Status (Claude, 2026-11-05): A to D built (3 of 3 full Build Maps), checked in the exe, published as 0.93.0.** Evidence `Data/screenshots/round93/`. No bug report newer than Round 87's.
A DONE (the course); NOT every hop gone (jump pieces, below). The gravel laid on the highest ground near (no grass through it), the ground spikes within 35 m of the lap cut, and the whole lap smoothed along in the planning (`MapBuilder.Round93.cs` R93SmoothLaps, `Tools/mapgen/make_round93_smooth.py`: blur, no crest a car leaves over below 45 m/s, levelled across 8 m; the jumps' run-ups, flights and landings kept). Dan's spot (lap 1840): one clean gravel surface, a smooth bank at the bend (`a_dan_1840_after.png`, `a_dan_1880_after.png`; the "before" is his photo). The three crests found after the first build (lap 2343, 4985, 7505: 0.5 to 0.7 s at 36 to 42 m/s, every lap) are gone. Left (bump log, 3 laps, bike and car, normal and HIGH, `a_b_check_final.txt`), all on jump pieces, not changed: a 0.3 to 0.8 s hop at the head of The Long One's run-up (lap 6790, every run); landing The Corner 30 to 40 m off line, the bike runs off the deck's side (lap 4860 to 4915); coming up short of a jump means the pit (as designed). The landings' checkpoints now count 45 m either side (the arch stays 22 m): landing a jump wide cost +30 s every lap.
B DONE. Without LB / RB the stick banks and turns (bank held at its limit: 70 deg Sky Hopper, 85 deg Sky Dart; 30 and 64 deg/s), wings level by themselves; LB or RB held (keyboard Shift): barrel rolls both ways and inside loops, level again after letting go; both planes, no crash (`b_plane_turn.png`, `b_plane_loop.png`). LB / RB were the rudder before: that extra turn is now on the stick (keyboard A / D keep the rudder). Hints on screen say it for the device in use.
C1 DONE for the road lap; the tube's way in improved, not clean. Wings & Wheels smoothed the same way (crests up to 58 m/s); road laps: no hop on the line in 12 laps (the bike's hops at lap 325 were 9 m off the line, over the curb). The tube's way in (the worn tracks to the X_KEN mouth) smoothed too: hops 0.4 to 0.7 s down to small jolts; one 0.66 s hop left at the mouth's step. Still seen: drivers missing the mouth and running into the building beside it, and catching on the J25 chamber inside (`c_tube_wayin_missed.png`).
C2 DONE. The U-turn (Tower Road's west end, lap 2129): its arch on the approach facing you, a red / white barrier row round the outside, a U-turn board, the minimap's amber turn (`93_uturn_120m.png`, `93_uturn_025m.png`); the human-like driver took it without a miss. But the bike, taken at 10 m/s+, ran wide into the barrier row twice in 2 road laps (`c_uturn_bike_pinned.png`; the turn is 7 to 8 m in radius); D1's reset comes in 8 s.
C3 DONE, timing NOT within 5 % after one attempt (a second needs a 4th full build: stopped). No slow rings; two more high rings (east of Kennestone, east of the square), the planes' lap 10.1 -> 11.5 km. AI best laps cars / planes: Easy 2:17-2:48 / 2:05-2:23; Normal 1:53-2:55 / 2:02-2:19; Hard 1:49-2:21 / 2:01-2:21 (Round 92's planes 1:55 to 2:11); planes about 6 to 8 % quicker (was 5 to 14 %). Human-like: car 2:44 by the roads, 2:00 through the tube; plane 2:09 (was 2:03). One Hard AI Sky Hopper missed the start / finish ring after the last ring and did not finish (1 of 15 plane runs) (`c3_times_final.txt`).
D DONE. (1) Any race, normal and HIGH: a player's car or bike on its end, on its side, hung with no wheel down or going nowhere with the throttle on for 5 s gets "Stuck? Press Y to reset" (R on the keyboard), and the reset by itself at 8 s; HIGH rights it at once and resets it if still pinned (`Race/RaceStuckReset.cs`). Checked on Wings & Wheels before CP3 (the bike held on its tail): prompt 5.0 s, reset 8.0 s; HIGH righted at 1.0 s, reset 6.7 s (`d_prompt.png`). (2) Cause: a point in the quarry's lake lies off the lap, its lap station came out near the lap's end, so 7306 -> 1028 read as a short step forward over CP3 to 22. A reset now forgives only checkpoints between the last one passed and where it puts you. Check: a reset 25 m before every checkpoint and the line on the quarry, Wings & Wheels, Chastain Glen and Kennesaw, and two in the quarry's lake: nothing forgiven, no lap moved (`d_check_all_courses.txt`).
Check runs (the new rule): the driver resets itself after 5 s with no progress (with a picture), a race stuck 2 min is ended STUCK, budgets from the work, logs watched; a run lost on the line after its own reset was found and fixed this round.
PUBLISHED 0.93.0 (`game-9300`, Latest). Disk: repo folders 63.9 -> 51 GB (clean-up plus the update test's install), C: free 119 -> 131 GB. Proven Dan's way from GitHub: the published starter's launcher (launcher 1) installed 0.92.1, whose game put launcher 2 in place; launcher 2 then updated 0.92.1 -> 0.93.0 (1389 MiB) and started it ("Installed: 0.93.0 · launcher 2"; `local/round93/updtest/`). Build `Builds/ChastainRush/BUILD_INFO.txt` 2026-11-05 13:16.
For Dan to check: the quarry after The Big One (and The Long One's run-up head); both planes on the stick and with LB / RB; Wings & Wheels (the road lap, the U-turn on a bike, the tube's way in); the planes' lap length (about 6 to 8 % quicker than the cars: lengthen more?).

## Previous delivery 2: Round 92
**Reviewed: Dan updated to it and played 2026-11-05 (the quarry, the planes); his notes are Round 93.**
**Round 92, THE LAUNCHER UPDATE FIXED, GLASS UNDER THE CREEKS, THE RACE FOR A PLANE OR A CAR (Dan, 2026-11-04 12:37, three photos of his screen, no reports; and the queued race of
2026-11-02 01:40).** Round 91 is reviewed. Item 0, the one-heavy-job rule (at most 3 full Build Maps) and the end-of-round clean-up apply. Short results.
0. FIRST: THE UPDATE THAT NEVER ARRIVED (Dan, 2026-11-04 12:42, a photo of the launcher: "Installed: 0.90.0" and "Manifest size limit exceeded"; "So let's get this fixed and
   ignore my complaints until I can review"). Cause (Claude): Dan's installed launcher refuses any file of 2 GiB or more (`Launcher/store.cpp` `Bytes`: b < 2 GiB, "Manifest size limit
   exceeded"), and 0.91.0's `ChastainRush_Data/sharedassets0.assets` is 2,193,343,600 bytes (level0.resS 1,994,916,288, close behind). Round 91's "proven from GitHub" missed it.
   Fix it so the launcher ALREADY INSTALLED on Dan's PC and Deck (it cannot be changed from here) takes the update: every file in a release under 2 GiB with room to grow (aim
   at most 1.6 GiB each): split the world's data across more files (e.g. the map's areas in their own scenes / asset bundles loaded with the main scene, or Unity's build
   settings that split shared assets), and / or shrink it (texture and mesh compression, the terrain's data) without changing how the game looks or plays; frame rate and load time
   before / after (PC and the Steam Deck setting). Then: (1) the publisher refuses any file over 1.8 GiB, with the file named; (2) every publish is proven the way Dan gets it: the
   PREVIOUS published release installed by the published starter's launcher, then updated by it to the new one from GitHub (not a fresh install); (3) a new launcher build with the
   per-file limit raised (files streamed, 64-bit sizes) AND able to update itself from the releases, shipped inside the next game update so later launchers fix themselves; say
   whether the installed launcher can take it in place, and if not, the one step Dan does once. Publish 0.92.0 with 0 done (and E and F if they are done), and confirm the
   installed 0.90.0 launcher updates to it.
   HELD until Dan has played 0.91 or later (his photos were 0.90.0; Round 91 0 may already cover them): his 2026-11-04 12:37 notes, kept word for word in QUEUED as "QUARRY NOTES
   FROM 0.90". Do not work on them this round.
E. CREEK CROSSINGS IN GLASS. Round 91 C left the new mains in plain concrete where they pass under creeks: the standing rule says every pipe through a body of water is an aquarium
   pipe (creeks included). Make those runs glass (water above, fish, weeds, light). Check: a picture from inside at each.
F. A RACE FOR A PLANE OR A CAR (queued since 2026-11-02; Dan, 2026-11-04: "I had mentioned I wanted a race that was airplanes vs cars. What happened to it?"; it waited for Round 91's
   underground). Dan: "a race that you could race as a plane or a car. The estimated time for both should be about the same and should use much of the same road. But we can do things
   to give the cars an advantage and add disadvantage to the planes so that one whole race should take the car and the plane about the same amount of time. For example planes could
   have things like forcing them to go a longer path and have cps under bridges to force them to fly more precisely and having to lower speed whereas cars could have underground
   shortcuts using the underground tube system". One course, raced in race setup as a car or a plane, and in mixed fields (cars and planes together, AI of both). Cars: the roads, with
   underground shortcuts through the new network (Round 91 C: M7 / M8 and their express runs, entered and left by manholes on the course). Planes: rings that follow much of the
   same road but take longer ways round, low rings under bridges and overpasses (small enough that full speed misses them), slow gates (a ring you must take under a speed). Tune so a
   good car lap and a good plane lap (the human-like driver and pilot, and the AI on each level) come within about 5 % of each other; report both times per difficulty (two tuning
   attempts, then report). Records separate for cars and planes, and a combined board; ghosts and replays of both; HIGH MODE for both (bumpers for cars, Round 84's put-back for planes).
   Check: 3 races each way (all cars, all planes, mixed) with times; the car takes every pipe shortcut, the plane every low ring.
Check: item 0 for every item. PUBLISH through the launcher.
For Dan to check: the launcher updating itself on the PC and the Deck; the glass under the creeks; Wings & Wheels as a car (the tube's turn-off off Tower Road) and as a plane, and whether the planes should be slowed more.

**Status (Claude, 2026-11-04): 0 and E published as 0.92.0 (`game-9200`); F built (2nd full Build Map), checked in the exe and published as 0.92.1 (`game-9201`, Latest).** Evidence `Data/screenshots/round92/`. No bug report newer than Round 87's.
0 DONE. Cause confirmed: the old launcher refuses any file of 2 GiB or more; 0.91.0 had `sharedassets0.assets` 2.19 GB and `level0.resS` (Unity's static batches) 2.0 GB. Now (a) the build starts with a tiny Boot scene that loads the world, with six never-loaded data scenes in between that each hold a share of the meshes / terrain / textures, so Unity writes them to six files (`Editor/WorldSplit.cs`, `WorldBoot.cs`; startup code that waited for "the first scene" now waits for the world, `WorldBoot.Defer`); (b) meshes over 4,000 vertices are left out of static batching (one draw each already; 37,565 small renderers still batched; `MapBuilder.TrimStaticBatching`). Largest file now 1.2 GB (aim 1.6), total 4.96 GB (0.91: 4.82), game zip 1.46 GB. Before / after, 11 places across the map at 1280 x 720 (not measured at 3840 x 2160): PC 56 -> 54 to 57 fps average (run-to-run noise ±3; Dan's PC was in use), load 36.5 -> 29 to 43 s; Steam Deck setting 58 -> 59 fps, load 28.7 -> 26.6 s (`before_*.txt`, `round92_check_*.txt`, `places_after.png`). Tried and dropped: static batching off (PC -10 fps), batching at load time (load +20 to 40 s).
0 (1) DONE. The publisher refuses any file (or package) of 1.8 GiB or more and names it (`Prepare-*.py` `guard`, `Publish-LauncherRelease.py`); it refuses the 0.91.0 draft naming both files. The player build also fails on one (`BUILD_INFO.txt` lists the largest files).
0 (2) DONE. Proven Dan's way, from GitHub: the published starter's launcher (0.81.0-Full, the same launcher Dan has, SHA f5648b52) installed 0.90.0, failed on 0.91.0 with "Manifest size limit exceeded" (Dan's message), then updated 0.90.0 -> 0.92.0 and started it (handshake PASS) (`local/round92/updtest/`).
0 (3) DONE. Launcher 2 (`Launcher/`, LauncherBuild 2): files and packages up to 64 GiB, and it updates itself: each game release carries it (`Launcher/ChastainRushLauncher.exe` + `launcher.json`, copied by PlayerBuild), and a launcher older than the one in the installed game swaps itself on start (hash checked against the signed manifest; the old exe renamed `.old`, removed next start). The installed launcher 1 cannot do that, so 0.92.0's game does it once: when launcher 1 (known hash) starts it, the game renames it and copies launcher 2 in its place (`LauncherBridge.ReplaceOldLauncher`). Proven in the test: after the update, the install's launcher was launcher 2, it started, showed "launcher 2", found 0.92.0 up to date. Nothing for Dan to do: open the launcher as usual; it updates to 0.92.0, starts the game, and the next start is launcher 2. Launcher 2 updating itself from a LATER release is not proven yet (needs the next release).
E DONE. Where the stage-5 mains pass under a creek, a pool at the creek's level is dug round the crossing (34 x 22 m, clear of roads and buildings) and the pipe runs under it in glass, as under a pond (`pipe_network.py` creek pools, `MapBuilder.Network.cs` DigCreekPool): CP1 and CP2 on M7a, CP3 on M7c (Noses Creek), CP4 on M8a (the pipe brought up 1 to 23 m with 10 % ramps). Check: a picture from inside at each (water over, weeds, fish, light) and from the bank (`glass_sheet.png`); M7a, M7c, M8a driven end to end, no stuck spot, jolts as in Round 91 (`pipes_m7a_m7c_m8a.txt`). NOT done (too deep): M7a under a creek at (1838, 1017) 27 m down, M8a under Noonday Creek at (-1305, -1258) 74 m, M8b under a creek at (-2123, -646) 93 m; glass there needs 21 to 87 m of climb each way in the main (network.json `pools_not_built`). For Dan: a deep glass shaft, a hump in the main, or leave them.
F DONE, timing NOT within 5 % after the two tuning attempts (reported, not tuned further). "Wings & Wheels" (race setup, Street or Air; `make_wings_route.py`, `make_wings_air_route.py`; `RaceController.Twin.cs`): one race, two laps over the same streets round Kennestone and the square. A car runs Cherokee, Tower Road, Church and Hansell (4.7 km), with THE TUBE: off Tower Road by worn tracks into the X_KEN mouth, down M7d with the express, out of the Root Street manhole (forgives CP4 to 8; the corridor now checks height, so the ground over the pipe does not count). A plane flies 10.1 km of rings the long way round: two low frames (under the hospital's skybridge, under the railroad), two slow rings (50 and 45 m/s, "TOO FAST" if over). Mixed fields (each racer its own lap; the opponents page offers cars and planes, All cars / All planes), records per kind plus a combined board, pipe express on in this race. Attempt 1 ran the southwest loop off the land (every plane into the edge wall); attempt 2 put it back and lengthened the northwest loop; the tree on Church Street's descent to C1 is cleared (track `wings_and_wheels_air.json`). Check (the exe, `f_final/`): human-like car 2:54 by the roads, 2:23 through the tube (2:10 in a cleaner run; 3 stalls this time near the turn-off and Cherokee), human-like plane 2:03 / 2:03, no crash, every low frame and slow ring taken. AI best laps, cars / planes: Easy 2:12-2:33 / 1:58-2:11; Normal 2:06-2:26 / 1:58-2:08; Hard 2:01-2:16 / 1:55-2:03. All 9 fields (all cars, all planes, mixed x 3 levels) finished; the AI took the tube on its own. Planes are 5 to 14 % quicker. To even it: shorten the car's lap or make the plane's longer (Dan decides). Pictures looked at: the turn-off (the orange shortcut on the minimap), inside the tube, the pop-out onto Root Street toward the line, the low frame under the skybridge. Not checked: HIGH MODE, ghosts and replays in this race (they use the shared race code).
Full Build Maps used: 2 of 3. Build `Builds/ChastainRush/BUILD_INFO.txt` 2026-11-04 21:43. Disk: repo folders 83.3 -> 45 GB (clean-up plus this round's three update-test installs), C: free 106 -> 138 GB.

## Previous delivery 3: Round 91
**Reviewed: Dan drove the quarry 2026-11-04 (photos; his notes are Round 92).**
**Round 91, THE QUARRY RACE READABLE AFTER ITS LANDINGS, SPLIT-SCREEN NOTES AT THE TOP, PLANE ROLLS AND LOOPS, THE UNDERGROUND ACROSS THE WHOLE MAP (Dan, 2026-11-02 01:40
and 16:42; started 2026-11-03).** Rounds 88 to 90 driven only briefly by Dan. Item 0, the one-heavy-job rule and the end-of-round clean-up apply. Short results.
0. THE QUARRY RACE AFTER THE BIG ONE (Dan, 2026-11-02 16:42, a photo of his screen, no report: lap 1, CHECKPOINT 4 / 19 "the pit floor" 320 m ahead, just after
   The Big One's landing (CP3); the picture: a huge plain grey wedge rising steeply into the sky, a green bank wall, red / white barrier ends, no road in sight):
   "Not sure what this is supposed to be and not really obvious where the track goes from here". (1) Say what the grey wedge is (The Big One's landing slope seen from
   below? a kicker? a cut?) and make it look like what it is in a quarry (crushed stone / gravel with edges, texture, a skirt; or remove it if it is leftover). (2) From
   The Big One's landing to the pit floor and on, the course must read at a glance: the gravel road visible ahead from where you land (not hidden behind the slope),
   its edges marked as the other courses' (dividers / berms / bumpers in HIGH along the way you go, never across it), the next checkpoint's arch in view from the
   landing, and the run-out turning you onto it. Check every landing and every bend of the lap the same way (where you land or round a corner, the way on is in
   view), with a picture from the driver's seat at each landing's touchdown and at Dan's spot.
A. SPLIT SCREEN: NOTES AT THE TOP. Dan: "when in split screen, for notes like Jump distance and speed traps can we put at the top of the screens instead of in the middle
   of the screens?" In split screen, every pop-up note (jump distance / air time, speed traps, combos, stunt scores, near misses, egg found, RING MISSED and the like) shows
   at the top of that player's half, out of the middle of the view, readable at 1280 x 800 (the Deck) and 4K; one player's screen unchanged unless it also covers the middle.
   Check: pictures of each note in both halves, side by side and top / bottom split.
B. PLANES: BARREL ROLLS AND LOOPS. Dan: "Allow planes to do barrel roles and loops". Both planes (and new ones later) can roll all the way round and pull a full loop:
   no bank or pitch limit in the air (`PlaneFlight`, `PlaneRoll / PlanePitch / PlaneBank`), still arcade-easy (it levels itself when you let go, as now, unless you hold
   the roll / pitch), and it does not crash itself upside down at height. AI pilots unchanged unless they need it. Free Roam, air races and HIGH MODE (HIGH: a roll or
   loop never ends in a crash; Round 84's put-back stays). Optional if cheap: a small "BARREL ROLL" / "LOOP" note like the car stunt notes. Check: a barrel roll each
   way and an inside loop in each plane by the human-like pilot (controller input), recorded as a picture series; the plane keeps flying.
C. THE UNDERGROUND ACROSS THE WHOLE MAP. Dan: "we need to expand the underground system to cover the larger map. We can for example have at least a couple pipes at the
   bottom of the quarry. We can have one or more leading to other parts of the underground network and one or two that shoot the user up back to the top of the
   quarry". The pipe network (`pipe_network.py`, network.json, `Docs/PIPE-NETWORK.md`; Dan's vision: a maze with secret exits all over, the hub chasm, pop-out manhole
   covers, gates and switches) covers only the first map. Extend it over the expansion: mains from the existing network west and south to Town Center (and under the
   mall), KSU, Kennestone, Marietta Square, Kennesaw Mountain's foot, the airport and Viking Quarry, with secret exits (manholes, drains, creek outfalls, rigged hydrants)
   in each area, the express on long runs, the map's PIPES layer and the Underground's map. At VIKING QUARRY: at least two pipe mouths at the bottom of the quarry
   (in the lake: aquarium pipes, standing rule; or in the quarry floor's dry part), at least one leading into the network, and one or two LAUNCH pipes that shoot you up
   and out back to the top of the quarry (a scripted geyser-style flight is allowed: creative, not a ramp; you land on the rim on drivable ground). Check: every new main
   driven end to end (no stuck spot, no step, walls rideable as the old pipes); each exit reached and left; the quarry's launch pipes from inside: out on the rim, upright.
Check: item 0 for every item. One round's rules (one heavy job at a time, at most 3 full Build Maps). PUBLISH through the launcher.

**Status (Claude, 2026-11-04): built (2 full Build Maps, 2 code-only exes; exe 06:23), checked in the exe, published (0.91.0, `game-9100`), not yet driven by Dan.** Evidence `Data/screenshots/round91/` (`round91_check_1..5.txt`). No bug report newer than Round 87's.
0 DONE. The grey wedge was the real quarry's old pit service road (OSM 1173718687 past its junction at (-1946, -1160), and its branch 1173718688) drawn draped over The Big One's graded landing slope: a road climbing a fill bank into the sky (`before_dan_spot_grey_wedge.png`). Both no longer built (track `drop_roads`, `MapBuilder.Round91.cs`), with their 7 barrier sets. Each run-out now has its own gravel surface, reflector posts along both edges (red left, amber right, not solid) and a checkpoint 10 m past the landing (22 checkpoints). Check: from each touchdown and from Dan's spot the next arch is in view from the driver's seat, normal and HIGH (`91_land_J*_seat*.png`, `91_dan_spot*.png`); 16 bends pictured (`bends_sheet.png`): the way on is in view at each.
A DONE. Split screen: race messages, jump / trap / combo notes, the trick box and HIGH's note at the top of each half (a narrow half: left of its minimap). Check: 4 of 4, side by side and top / bottom (`91_split_*.png`).
B DONE. Both planes roll all the way round and loop (`PlaneFlight.Aerobatics.cs`): no limit while you hold the stick; let go and the wings level the near way and the nose comes back to the horizon; never crashes from being upside down; HIGH pulls out of a dive near the ground by itself; BARREL ROLL / LOOP notes. AI and scripted pilots unchanged. Check (controller input, both planes): a roll each way and an inside loop, flying level 5 s after; a loop comes out 27 to 67 m from where it began at the same heading (`91_plane_*_sheet.png`, `91_dart_*_sheet.png`). HIGH's pull-out not flown by the check.
C DONE. Network stage 5 (`pipe_network.py --stage 5`): M7 South Main (hub, Town Center J20, Kennesaw Mountain J24, Kennestone J25, Marietta Square J26; glass under Christon Lake), M8 West Main (J20, Viking Quarry J23, Kennesaw State J22; glass under the campus pond), 14 branches, express on every long run, 6 manholes (Ring Road NW, Campus Loop Road, Paulding Avenue, Kennesaw Mountain Drive, Campbell Hill Street, Root Street), 5 exits (Town Center, Kennesaw Mountain, Kennestone, Marietta Square, the airport) and at the quarry floor: a shore exit into the network and two LAUNCH pipes (a geyser at the mouth and an aimed flight onto the airport plateau, `Pipes/PipeLaunch.cs`; off during races). Old network unchanged but one new hub opening. Check: every new main and branch driven end to end, no stuck spot or reset; every manhole and exit driven out of (`91_out_*.png`); both launches land on the rim upright, 28 and 35 m from their targets (`91_launch_*_landed.png`).
Not done: where the new mains pass under a creek the pipe is plain concrete (the two ponds they cross are glass). Full Build Maps used: 2 of 3.
Published: game 0.91.0 (`game-9100`, Latest) through the launcher. Clean-up: 44.9 -> 44.6 GB.
For Dan to check: Viking Quarry after each landing (normal and HIGH); split-screen notes; rolls and loops in both planes (and HIGH near the ground); the new pipes from Town Center / KSU / Kennesaw / Kennestone / the Square / the airport, and both quarry launch pipes.

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
