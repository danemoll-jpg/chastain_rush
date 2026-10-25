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
**Round 68, KENNESAW MOUNTAIN: REALLY BIG JUMPS (Dan, 2026-10-25 08:35, after driving Round 67).** Dan: "I specifically wanted to do a
mountain trail so I have really big jumps but these are just little bunny hops. I know if you can see it in Woodstock Rush how big the
jumps I had on the Mountain trail, this is what I want just really large jumps. As far as this I had bigger jumps in my own neighborhood
than I got in the mountains...cmon". Claude's brief for Round 67 said "huge" with no numbers; Round 67 built six jumps of 1.1 to 2.3 s and
21 to 59 m. This round gives numbers. Use Round 65's quick builds (the mountain's step) while working; ONE full Build Map at the end.
Standing rules apply (real physics off every lip; nothing slows a car on the approach or the lip; clean landings).
1. THE MEASURE: Woodstock Rush's mountain (`Racer/`: its mountain trail jumps and the summit "homeward" flight, PROJECT_TODO_ARCHIVE.md line
   ~1989: 215 m straight run-up, 36 to 43 m/s at the lip, a flight that cleared the whole mountain to a catch slope). Read how Racer built
   them (run-up, lip angle, landing slope) and, if its build can be run minimized, measure its mountain jumps' air time and distance. Also
   measure this game's biggest neighborhood jumps (the Ridge leap, the boat jump, the kicker / carport, the mall drop). Put both in the
   status. Every mountain jump must beat every neighborhood jump.
2. THE TARGETS (design them in from the start: compute each flight's path from run-up speed and lip angle and shape the landing to it;
   do not tune small jumps up):
   - at least THREE HUGE jumps on the lap: 4 to 7 s in the air, 120 to 250 m long, the car 20 m or more above the ground below at the apex;
   - the rest BIG: 2.5 to 4 s, 60 to 120 m; nothing on the course under 2.5 s counts as one of the mountain's jumps (smaller bumps may stay
     but are not counted);
   - ONE SIGNATURE FLIGHT: off the summit (the overlook / the top of the trail) out over the mountainside, Woodstock Rush's summit flight in
     spirit: the longest air in the game, landing far down the mountain on a long smooth catch slope.
   How: use the mountain's height (about 240 m of drop on the trail; the game's 1.5x relief), long straight run-ups down the fall line so cars
   reach 35 to 45 m/s (cut straight lines through the woods where the trail zig-zags; clear and grade them), steep lips (25 to 35 degrees,
   still real-world things: rock ledges, cut faces, log ramps), and catch slopes that fall away under the flight and meet it at a shallow
   angle (long, smooth, wide, cleared), so the car lands clean and keeps its speed. Air comes from the ground falling away, not just the lip.
3. AT RACE SPEED, ALL VEHICLES: the targets hold for the Drifter Twin and the Street Classic at the speed a player reaches flat out, and the
   AI reaches the lips fast enough to fly big too (no braking before a lip; AI speed zones as needed). Bikes and ATVs land clean as well.
4. Keep Round 67's route, start, checkpoints and closures unless a run-up needs a change (then move checkpoints / AI to suit). Trees: no
   trunk anywhere near a flight's corridor or landing (breakable or gone).
Report: a table per jump: lip speed, air time, distance, apex height above the ground below, landing speed, for the Drifter Twin, the Street
Classic, a bike and the AI; next to Racer's and the neighborhood's numbers. Pictures mid-air on each huge jump and the signature flight.
Tuning limit as usual for fine adjustment, but the targets above are the job: if a target cannot be met somewhere, say where and why.
For Dan to check: race Kennesaw Mountain flat out; the signature flight off the summit.

**Status (Claude, 2026-10-25): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round68/` (`round68_check_final.txt`, pictures `68_midair_J*.png`, the design `68_design_profiles.png`, `68_layout.png`). No bug reports newer than Round 67's session.
1 THE MEASURE. Racer's mountain (its validation notes, `Racer/Docs/CR112-118/VALIDATION.md`, CR101-104; not re-run): gully flight 42 to 46 m/s, 5.9 to 6.4 s, 190 to 227 m; summit flight 40 to 43 m/s, 6.7 to 7.6 s, 191 to 228 m; its catch slopes a 0.75 grade. This game's neighborhood jumps flown the same way here (Drifter Twin / Street Classic, flat out): Rooftop leap 3.1 / 2.9 s, 59 / 52 m (the biggest); Elaine jump 2.4 s, 70 m; Parkway leap 2.7 / 1.3 s, 74 / 29 m; Creek leap 2.6 / 2.4 s, 56 / 50 m; Boat jump 1.9 s, 31 m; Carroway 1.8 / 1.6 s; the Ridge leap and the Lakes kicker gave no proper flight in the scripted drive (an aimed launch, a path issue): not measured. Every mountain jump beats all of these.
2 THE NEW DESCENT (`make_mountain_route.py` rewritten, `MapBuilder.Mountain.cs`): the climb up the drive unchanged; from the drive's last straight below the summit the race runs on three straight lines cut through the woods (14 m bench, 32 m wide over the catch slopes), each built round one jump designed from its flight (run-up speed from the vehicles' acceleration, lip angle, kicker radius, catch slope). J1 THE SUMMIT FLIGHT (the signature: the drive's straight is its run-up, a granite ledge at the top of the north-east face, 17 degrees), hairpin; J2 THE WEST FACE (a boulder ledge at the shoulder's brink, 14 degrees), hairpin; J3 THE EARTHWORKS (a timber-faced berm, 18 degrees, landing across the drive by the visitor center onto the finish straight). Lap 3526 m (was 3550), 10 checkpoints (CP5 to CP10 moved onto the new lines), closures unchanged. The old trail's bench and six small jumps are gone (the trail itself as Round 61 built it).
   Flat out, the real vehicle (air s / distance m / most height over the ground below m), all landing upright: Drifter Twin J1 5.5 / 223 / 44, J2 3.7 / 100 / 32, J3 4.5 / 171 / 44; Street Classic 4.8 / 174 / 38, 4.2 / 141 / 39, 4.2 / 144 / 40; Needle 600 5.8 / 243 / 45, 4.8 / 189 / 45, 4.5 / 183 / 46; Ridge Scrambler 5.5 / 226, 4.6 / 174, 4.5 / 174; Trail Four 5.6 / 227, 4.6 / 171, 4.5 / 172. Lip speeds 36 to 47 m/s, landing 40 to 57 m/s. At the AI's held speeds (Drifter Twin) 5.6 / 224, 4.4 / 155, 4.4 / 159. The signature is the longest air for every vehicle (it is shorter than Racer's summit flight: 4.8 to 5.8 s against 6.7 to 7.6).
   Not met: the Drifter Twin flat out loses 12 m/s on J2's kicker top (3.7 s; at the AI's speed 4.4 s). Two attempts (kicker radius 280 then 340 m): tuning limit.
3 LANDINGS (new code): the kickers' curve kept within what the suspension carries (radius 300 to 340 m: tighter ones put the body on the ramp and halved the speed); a 75 degree first 3 m behind each lip; on the mountain's catch slopes only, a vehicle touching down is turned to the slope with its pitch stopped (`RampHold`), and the body-contact prediction keeps the slope's own normal (`VehicleSurfaceContacts`: it held bodies 0.7 m over a 30 degree slope with no wheel down); the line's ends take the drive's own levelled height (a 0.7 m step was throwing cars at the departure).
4 AI: two races on Hard, 3 AI each, all 6 finish (5:06 to 6:01, stuck 64 s in all, 49 resets, most "blocked" ones). The lap on all 11 vehicles at the AI's speeds: round, no stop on the new lines. Trees: none within 16 m of the lines, breakable to 32 m and 38 m along each flight.
Found, not changed (not this round's ground): three heavy cars (Longroof GT, Highball Fastback, Skyfin Cruiser) stop hard at 36 to 39 m/s on the drive at (-500, -5320) (also in Round 67's check); the Turf Rocket hits a trunk beside the drive at (-1046, -6164).
Builds: 4 full Build Maps (the first did not compile), 7 quick builds, 4 code-only exes. `Builds/ChastainRush/` 2026-10-25 15:44. Imported from Racer: nothing.
For Dan: (a) race it flat out; the summit flight; (b) J3's kicker stands about 30 m over the ground at its lip (a long gentle curve so cars do not bottom out): its look; (c) the AI's resets.

## QUEUED rounds
**Round 69, A PLANE TO FLY (Dan, 2026-10-25 12:39: chose "planes" for the next round; he is away and has not tested Round 68). START
RIGHT AFTER ROUND 68 IS DONE AND COMMITTED, without waiting for Dan.** Builds on Round 63's helicopter (flight code, camera, map, resets,
far view) and Round 66's edges (warning, headwind, ceiling). Quick builds while working; ONE full Build Map at the end (the airport's
parked planes). Tuning limit. Short results. Dan is not watching: decide, write it down, go on.
1. THE PLANE: a small single-engine high-wing plane of our own (Blender, as the helicopter; no real maker's name or livery), "Sky Hopper"
   or Code's name. Arcade flight, not a simulator: it must be easy and fun. Throttle up / down; pitch, roll (banking turns the plane
   by itself), a little rudder; it holds level flight hands-off at its speed; no punishing stalls (too slow and the nose eases down until
   it has speed again). Cruise fast enough to cross the map in two or three minutes, a top speed above that; engine and wind sound with
   speed; flaps not needed. Pad and keyboard layouts that match the helicopter's where they can (Code picks, says them in the status).
2. TAKE-OFF AND LANDING: it rolls on its wheels, steers on the ground, takes off from Cobb County International's runways (and any long
   straight road or field, Free Roam being Free Roam), lands on them: a soft landing rolls out, a hard one or a wing into the ground is a
   forgiving crash (as the helicopter: a moment, then reset onto open ground nearby, never stuck). Hitting trees, buildings or wires:
   the same.
3. WHERE: two or three planes parked on the airport's apron (drive up and swap, as the helicopter pads: A / H); "plane" in Free Roam's
   vehicle choice (starting lined up on a runway). Free Roam only, never in races. A little airport life if cheap: one plane that now and
   then taxis, takes off, circles and lands (no collisions to worry the player).
4. THE WORLD FROM A PLANE: Round 63's far view and Round 66's scenery ring and horizon (Atlanta) at plane heights and speeds; the edge
   warning and headwind turn the plane back too (a wide gentle turn), and a ceiling. Frame rate at 300 m and 600 m up crossing the map
   against Round 63's numbers. Cameras: chase, far chase, cockpit, and a free look; the readout (height, speed, heading, climb).
5. FUN: low passes over the quarry lake (Viking Quarry, next to the airport) and down the I-75 / I-575 corridor should feel fast; nothing
   blocks flying under the highway bridges if there is room.
Check: take off from each runway, fly to Kennesaw Mountain, Town Center and Chastain Glen and back, land on the runway; crash into a tree,
a building and the ground (reset, not stuck); hands-off level flight 30 s; the edge turn-back; the fps table. Pictures: take-off, over the
quarry, over Town Center, Atlanta on the horizon from 600 m, the apron.
For Dan to check: fly the plane (controls, take-off, landing, speed); the view from up high.

**Status (Claude, 2026-10-25): built and checked in the exe by script, not yet flown by Dan.** Evidence `Data/screenshots/round69/` (`round69_check_final.txt`, `round69_check_fps.txt`, pictures `69_*.png`).
1 DONE. "Sky Hopper" (`Tools/Blender/plane.py`, our own high-wing single-engine plane, 11 m wings, tricycle gear; `VehicleProfile.Plane`, Flies: Free Roam only). Flown by `Vehicle/PlaneFlight.cs` (+ `.Crash.cs`, `.Effects.cs`): on its wheels the vehicle's own drive; in the air arcade flight: the throttle sets the speed (top 115 m/s, cruise about 85 at 75 %: the map in about 2.5 minutes), climbing costs speed and diving gains it, the bank turns it, hands-off it levels its wings and holds its height (30 s: height within 0.1 m, heading 2.2 degrees), slow (under 36 m/s) it sinks and the nose eases down (no stall, no spin). Engine buzz made in code, pitched by the throttle; wind with speed; readout (height, speed, heading, climb, throttle).
   Pad: left stick pitch (pull back: nose up) and bank; RT / LT throttle up / down (LT brakes on the ground); LB / RB rudder; X view; right stick looks round; Y reset; A take / leave a parked plane. Keyboard: W / S throttle (S brakes on the ground); arrows: up / down nose down / up, left / right bank; A / D rudder (and steer on the ground); V view; R reset; H take / leave.
2 DONE. Take-off from runway 09/27 (the airport's one runway, both ways): full throttle, past 30 m/s pull back: off after 157 m (09) / 168 m (27). Any long straight road or field works the same. Landing: touching down under 5 m/s, wings within 18 degrees, it rolls out on its wheels (the check: 32 m/s, 1.7 m/s down, stopped in 25 m). Harder, a wing, the nose or the body into anything, a tree's crown or water: a crash, then after 2.6 s put on the nearest open strip it can take off from again (130 m clear and flat, within 1.5 km; else lined up on the runway). Into a tree, Town Center's front and the ground: crashed, reset, rolled away again (6 of 6).
3 DONE. Three planes parked on the south apron (`Vehicle/PlaneStands.cs`, built at run time: no Build Map step): A / H takes one and leaves it (2 of 2); "Sky Hopper" in Free Roam's garage starts lined up on runway 09. Never in races. Airport life (`Vehicle/AirportCircuit.cs`): one red Sky Hopper taxis out every few minutes, takes off, flies a circuit at 300 m and lands (visual only).
4 PARTLY. The far view and Round 66's ring and horizon work from the plane (Atlanta from 600 m, `69_atlanta_from_600m.png`). The edge: LEAVING THE SURVEY AREA, then a headwind and a wide bank back by itself (the stick's bank overruled): at most 805 m out at full throttle, turned back in 27 s. Ceiling 1700 m as the helicopter. Cameras: chase, far chase, cockpit, the right stick's free look. Frame rate (1280 x 720, not measured at 3840 x 2160; real time, the last 1.5 km to each place): at 300 m Kennesaw Mountain 54 / Town Center 43 / Chastain Glen 82 fps; at 600 m 61 / 56 / 94; 1 % lows 9 to 24 (hitches as the far detail loads in at 95 m/s). Lower than Round 63's helicopter numbers (88 to 164 at 500 m from a still camera): not profiled this round.
5 Low passes over Viking Quarry Lake and the highway corridors: nothing blocks flying under the bridges where the plane fits (its wing tips and nose test what they meet).
Builds: one full Build Map (18:07, nothing in the world changed), then a code-only exe for the frame-rate check's fix (check code only) `Builds/ChastainRush/` 2026-10-25 18:27. Imported from Racer: nothing.
For Dan: (a) the controls and how it flies (speed, turning, the landing); (b) the frame rate at speed over Town Center and the mountain; (c) the plane's look (`69_model.png`, `69_apron.png`).

Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 67
**Reviewed: Dan raced the Kennesaw Mountain trails 2026-10-25: the jumps are far too small ("just little bunny hops"): Round 68. B1 to B3 and the open questions (a) to (e): no answer yet.**
**Round 67, A NEW TRACK: THE KENNESAW MOUNTAIN TRAILS, ALL ABOUT AIR; two Noonday Creek Trail shortcuts and a lip (Dan, 2026-10-25 03:01;
reports BUG-001 to BUG-003 of session `2026-10-25_02-23-36-193_bcac2f`, read by Claude, after the Round 66 exe: the Noonday Creek Trail race both ways).**
Round 66 is reviewed. Use Round 65's quick builds while working; ONE full Build Map at the end. Standing rules apply (shortcuts: real risk,
no free way round, tire tracks / skid marks as the hint; ramps a real physics jump; nothing slows a car on a ramp; not every jump a dirt
pile). Tuning limit. Short results.
A. THE KENNESAW MOUNTAIN TRAILS RACE. Dan: "I want one with a lot of air so I am wondering if we could do the trails of Kennesaw mountain.
   I want huge jumps on this".
   (1) THE ROUTE (Code designs it and says why): a loop on Kennesaw Mountain National Battlefield Park's real trails (OSM, Round 61 built 45
       named trails, 19.6 km, at their real width on levelled ground) and Kennesaw Mountain Drive: e.g. up the mountain road to the summit
       lot, then down the ridge and summit trails (Kennesaw Mountain, Little Kennesaw / Pigeon Hill side) with the big drops the game's 1.5x
       height gives, back round by the visitor center. A lap of a few km; 2 or 3 laps, Code's pick. Start / finish at the visitor center lot
       (or the summit lot), Code says.
   (2) HUGE JUMPS, MANY, VARIED: the trail's own crests and ledges shaped to launch (natural lips, no ramp), plus built launches that
       belong on a wooded mountain: rock slabs and boulder ledges, fallen-tree log ramps, a washed-out trail bridge, the old earthworks'
       berms, the summit overlook's rock face, a switchback cut that sends you off the hillside onto the next leg. Several of them big
       enough for long, high flights (seconds in the air) that land clean on the trail downhill, heading the race's way. Report each jump's
       air time and distance on the Drifter Twin and the Street Classic.
   (3) THE TRAILS RACEABLE: the path at its real width (standing rule) with the woods cleared back enough to race and pass (Dan wanted the
       made-up woods track wider so vehicles could weave); trees solid off the cleared line; breakable where a racer would clip them often
       (as Noonday Creek Extension). All vehicles unless a stretch is too steep for cars: say so (the old woods track's "bikes and ATVs" idea is
       Dan's to decide).
   (4) DIRECTION: FORWARD = the downhill-on-trails way (that is where the air is). REVERSE only if it still plays well (climbing the trails is
       little air): Code says; if not, forward only and Dan decides. No shortcuts this round (Dan picks them after driving it).
   (5) Built like the other races (route, checkpoints, closures, AI that takes the jumps, ghosts, records, replays, menus, loading inset,
       fast travel). Park visitors on the trails in Free Roam (a few in races, who get out of the way).
B. THE NOONDAY CREEK TRAIL (Round 64's track).
B1. A FORWARD SHORTCUT OVER THE CREEK. BUG-002 (x 1726.59 z -671.00 h 271, Bells Ferry Road NE by the trailhead, forward, CP1 next):
   "shortcut entrance (skid mark here...you go down the hill hit a ramp before the river that carries you forward a succesful landing puts
   you heading straight on the track. A poorly angled one will cost you to crash". Skid marks at the turn-off; down the bank; a ramp (that
   belongs by a creek: Code picks, not a dirt pile) just before Noonday Creek that carries you over it; a good landing lands you on the trail
   heading straight down it; a badly angled one crashes you (trees / the bank / the water). Saves time clean, costs it missed.
B2. A REVERSE SHORTCUT OVER CHASTAIN MEADOWS PARKWAY. BUG-003 (x 1328.10 z -755.84 h 123, 19 m off Chastain Meadows Parkway, reverse, CP11
   next): "can we make a shortcut here where it leads to another big jump jumping over the street and clear to the other side". A shortcut
   from here to a big jump clean over Chastain Meadows Parkway, landing on the far side and back on the course. (Directions from Dan's
   headings; confirm against the lap.)
B3. BUG-001 (x 167.16 z -899.63 h 144, Town Center Drive by the mound jump, forward, CP5 next): "it wasn't flush and caused me to lose
   momentum". Make the road's edge / the course's step onto the ground there flush, so nothing catches a car.
Check: the mountain loop on all vehicles (no stop; each jump's air and landing); AI races x2 (each way it has), Hard, all finish; B1 and B2
clean / missed / round them; B3 speed through before / after. Pictures: the start, three biggest jumps mid-air, the summit leg, B1 and B2.
For Dan to check: race the Kennesaw Mountain trails (the jumps!); the creek and the parkway shortcuts; the lip by Town Center Drive.

**Status (Claude, 2026-10-25): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round67/` (pictures `67_*.png`; `round67_check_full.txt`: every vehicle, the AI; `round67_check_last.txt`: the final exe). Bug reports read: session `2026-10-25_02-23-36-193_bcac2f`, BUG-001 to 003.
A DONE. "Kennesaw Mountain" (`make_mountain_route.py`, `MapBuilder.Mountain.cs`): start on Kennesaw Mountain Drive east of the visitor center, UP the drive (2.3 km, about 10 %) round the saddle to the summit lot, across the lot to the overlook, DOWN the Kennesaw Mountain Trail (1.2 km, 240 m down) to the visitor center. Lap 3.55 km, 2 laps, 10 checkpoints. Why: the trail is the downhill (the air) and the drive the climb; going over the summit to Little Kennesaw needs a 27 % climb on a trail, or the drive raced both ways. Forward only: up the trail is a 20 to 30 % climb with nothing to fly off (reverse is yours to decide). All 11 vehicles round the lap by script: nothing too steep for cars.
   The trail: a bench 12 m across (18 m through the hairpins), its 1.5 m path kept; no tree within 10 m, breakable to 25 m and 140 m along each flight.
   Six real jumps, the ground itself shaped: J1 the summit overlook (granite), J2 the ridge drop (a fallen oak; the first switchback cut straight down the ridge), J3 the earthworks berm (revetment logs), J4 the switchback cut (a boulder ledge; the zig-zag cut straight down), J5 the washout (the old trail bridge's abutment over a dug wash), J6 the last ledge. Air, Drifter Twin at the AI's speed / at 26 m/s (Street Classic within 0.1 s): J1 1.1 s 21 m / 1.7 s 43 m; J2 2.0 s 47 m / 2.1 s 54 m; J3 1.3 s 28 m; J4 1.8 s 38 m / 2.3 s 59 m; J5 1.7 s 37 m; J6 1.8 s 42 m / 2.0 s 51 m. All land clean at the AI's speed. Faster: J1 (and the Street Classic's J3) fly on past the next bend and stop in the woods at 26 m/s and more; J3 and J5 are flown off the slope before their lips. Two attempts: tuning limit.
   AI: twice on Hard, 6 of 6 finish (4:52 to 5:18), stuck 37 s in all. Park visitors on the trails in Free Roam (up to 9 groups: joggers, dog walkers, cyclists, couples), 2 at most in a race. Records, ghosts, replays, menus, loading inset as every route; fast travel "Kennesaw Mountain start".
B1 DONE. "Creek leap" (forward): skid marks off Bells Ferry's northbound lane south of the bridge, a heap of gray riprap on the shoulder heading west, over the bank and Noonday Creek onto the flats, landing on the trail's line about 20 degrees off it (the creek and the trail run side by side: no take-off gives a dead-straight landing). Brush along both banks. Drifter: clean 13.2 s against the lap's 18.8 (2.6 s in the air), missed (14 m/s) 19.0 s, round it through the creek 19.7 s. New checkpoint "the trailhead's drive" (a shortcut must skip one).
B2 DONE. "Parkway leap" (reverse): off the trail before its loop under the bridges, down the hollow at your spot, up the parkway's embankment shaped as a kicker, over both carriageways (2.5 s in the air) onto the lap past the bridges; a hedge along the far shoulder. Clean 12.1 s against 13.1 (saves 1 s), missed 15.6, round it over the road 15.8. New checkpoint "under Chastain Meadows Parkway".
B3 PARTLY. Cause: the face rose from the road's edge on a 14 m bend, and a car thrown off the road's far edge (the climb from the red bridge) came down into it. Now a 45 m arc at the face's foot from the road's own level, the climb to the road a 150 m crest, a landing zone on the face. Street Classic flat out (30 m/s) forward: 60 % of its speed lost at the mound before, 12 % now; at 26 m/s still 34 % (Drifter 5 %). Two attempts: tuning limit.
Builds: 3 full Build Maps (the first did not compile; the third for the wider clearing) and 3 quick builds. `Builds/ChastainRush/` 2026-10-25 06:42. Imported from Racer: nothing.
For Dan: (a) the mountain's jumps flat out (J1, J3); (b) reverse on the mountain, yes or no; (c) the creek leap's landing angle; (d) the parkway leap saves 1 s: enough?; (e) the mound flat out in the Street Classic.

## Previous delivery 2: Round 66
**Reviewed: Dan played the Round 66 build 2026-10-25 02:23 (the Noonday Creek Trail race, forward and reverse): "Not much there"; his notes (Round 64's trail) are Round 67 B.**
**Round 66, THE MAP'S EDGES: A WORLD THAT KEEPS GOING, NATURAL STOPS, A HELICOPTER BOUNDARY, ATLANTA ON THE HORIZON (Dan, 2026-10-24
11:41: "Speaking of edges, how can I handle this on my map especially now I have helicopters"; 11:44 "Yes" to Claude's three-part outline,
"And I like the idea of Atlanta in the distance"). Dan, 2026-10-24 17:55: "Lets go ahead" (Rounds 64 and 65 done, not yet seen by him). Use Round 65's quick builds while working.** ONE full Build Map at the end. Tuning limit.
1. SCENERY PAST THE EDGE (not playable): a ring of land a few km wide beyond every edge from the same USGS 3DEP elevation (download the
   extra tiles as for the mountain), low detail, no colliders, roads or traffic: terrain, a carpet of cheap far trees (Round 63's far-view
   kind), simple building blocks where OSM / NAIP show towns, water. Haze thickening toward its outer edge so its end is never seen. From the
   summit and from the helicopter at its ceiling the world must look like it keeps going: no cliff, no void.
2. THE HORIZON: real distant landmarks at their true bearings, distances and heights: the Atlanta skyline (downtown and midtown towers,
   about 30 km south-east; generic tower shapes, no brand names, lit at night), and the far ridges / mountains around (Lost Mountain, Pine
   Mountain, Sweat Mountain and the like, from elevation data where it reaches, as silhouettes beyond it). Day / night and weather apply
   (hidden in fog and rain, clear on a clear day).
3. CARS STOP NATURALLY AT THE EDGE: roads that leave the map end in "Road Closed" barricades / a detour (road-crew style), highways (I-75,
   I-575, US 41 and the like) in a jersey-barrier closure where traffic turns round (traffic and police nets end there cleanly); off road,
   the edge runs through dense woods, creeks or fence lines where they exist, with a soft invisible push-back so nothing gets through or
   falls off.
4. THE HELICOPTER'S BOUNDARY: near the edge a "Leaving the survey area" warning; keep going and a strong headwind slows it and swings it
   back (no crash, no teleport). A height ceiling, so the edges of the scenery are never seen from above.
Check: from the summit and from the helicopter at its ceiling, looking out each way (pictures, day and night, Atlanta's skyline in them);
drive every road that leaves the map (each stops at its closure, traffic turns); fly at each edge (turned back); frame rate at the summit
and at the ceiling against Round 63's numbers.
For Dan to check: fly to the edges and look out; Atlanta from the summit; drive off the map on a few roads.

**Status (Claude, 2026-10-24): built and checked in the exe by script, not yet seen by Dan.** Evidence `Data/screenshots/round66/` (pictures `66_*.png`, check texts). No bug reports newer than Round 64's session.
1 DONE. A ring 9 km wide past every edge (`python Tools/mapgen/make_scenery.py`: USGS 3DEP 30 m heights, NLCD 2021 land cover for "towns": the US land-cover map made from the same imagery as NAIP; drawn at run time by `World/EdgeScenery.cs`, no Build Map step): ground by cover, 384,000 trees in the map's far tree shapes, 48,000 house and building blocks, lakes; no colliders. Its inner edge takes the map's own edge heights. The fog closes long before its outer edge, and the land beyond it to 70 km is the haze's color (`66_edge_*`, `66_ceiling_*`).
2 DONE. Atlanta (Downtown, Midtown), Buckhead, Cumberland and Perimeter Center: 169 generic towers at their real places and heights (heights x1.5 like every building and hill here), lit windows and blinking red tops at night, gone in rain (`66_summit_atlanta_*`, `66_ceiling_atlanta_*`). The far ridges come from the elevation data (only land 110 m or more above its surroundings shows as a blue-gray silhouette: Sweat Mountain, Lost Mountain, the Blue Ridge foothills to the north). The view now opens on the summit too (height over the land's low point, not only over the ground).
3 DONE (see the numbers). Every drivable road end on the edge (262): highways (I-75, I-575, US 41, Barrett, North Marietta Parkway, Canton Road; 19 carriageways) get jersey barriers, cones and ROAD CLOSED; the rest get type III barricades, ROAD CLOSED and a DETOUR sign (`make_closure_signs.py`); 38 roads that run along the edge itself get none (the push-back covers them). Closures are solid to 2.6 m (lower ones let a fast car vault them). Traffic and police networks end before them; traffic turns round there (16 turnarounds across divided highways). Off road: pushed back within 36 m, a wall on the edge, anything past it put back.
   Checks: every closure driven into (22 m/s, highways 30): 221 of 224 stop at it; 3 do not (Canton Road at the north edge and the service road beside it, Canton Road at the east edge: on a bend the car gets onto or round the barriers' end, then the edge's push-back stops it short of the edge; third try, tuning limit: left for Dan, `66_closure_016_*`, `66_closure_139_*`); traffic (Heavy, 11 spots): 144 cars turned round, 25 waited at one-way ramps with no turn (moved off unseen, as before); off road at each edge: stopped 1.4 to 6 m short, never past.
4 DONE. LEAVING THE SURVEY AREA from 300 m inside the edge; past it a headwind that holds it within about 215 m out flat out with boost, the nose swung back to the map, no crash (4 of 4 edges, `66_heli_*`). The ceiling stays Round 63's (1,700 m): from there the ring's outer edge is past the fog. Past the edge there is no ground: it stays 30 m over the ground it left.
Frame rate (1280 x 720, a fixed camera; not measured at 3840 x 2160): at the ceiling over Chastain Glen / Town Center / the mountain 100 / 95 / 96 fps with the scenery, 104 / 94 / 93 without (Round 63 at 1080p, 500 m: 164 / 99 / 88). On the summit 65 to 83 fps: its view now opens to 6 km (180 to 196 with the fog closed, as before).
Builds: three full Build Maps, not one (the closures needed two fixes found by the drives: wider rows, then taller colliders; each about 30 min with the exe), code-only exes before them. `Builds/ChastainRush/` 2026-10-24 20:49. Imported from Racer: nothing.
For Dan: (a) the ring and the skyline's look; (b) the summit's frame rate with the view open; (c) the three Canton Road closures (a car can end up on the barriers; R / Y resets).

## Previous delivery 3: Round 65
**Not yet reviewed: editor tools only; Dan moved on to Round 66 on 2026-10-24 17:55 without checking 64 or 65 ("I won't have a chance to check it").**
**Round 65, BUILD TOOLS (Claude's suggestion 2026-10-23 17:40; Dan: "you had wanted a round too. but I want to do the helicopter round
next").** NEXT, straight after Round 64, before any new track or feature (Dan, 2026-10-24 11:09: he meant build tools to come first). Make track and shortcut work rebuild only its own area (Round 42's quick builds extended to what
the new tracks build: trail decks, boardwalks, bridges, the mall's race pieces) so a round's fix-and-check cycles take minutes, keeping one
full Build Map at the end; trim the full Build Map (Roads and Businesses about 27 of its 62 minutes in Round 62).
Dan, 2026-10-24: "When Round 64 is done and committed, do Round 65 in TODO.md (QUEUED) right away." (Round 64 took 6 full builds of about an hour each.)

**Status (Claude, 2026-10-24): built and measured; editor tools only, the world unchanged (every build's scene the same as before by SceneDump). Nothing for Dan to drive.** Detail: `local/round65/` (times, scene dumps; git-ignored).
1. The full build: Build Map 2,616 -> 912 s (44 -> 15 min); the exe after it 822 -> 234 s; a full build with its exe about 60 -> 20 min. Cause: every generated mesh was an asset file of its own, and an asset file costs about 30 ms to write whatever its size (`Assets/Editor/BuildSpeedProbe.cs`: 1,000 meshes 25 s as files, 0.4 s packed); some 40,000 of them. Now packed 300 to a file as sub-assets (`MapBuilder.Quick.cs` PackMesh): Businesses 485 -> 26 s, Roads 480 -> about 25 s, Rest, Neighborhood, Hydrants, Driveways and Network the same way. Also: the manholes' street cut tests only the holes near each mesh; the road levelling's terrain edit runs only over the cells along each road, in parallel (R62LevelMains 165 -> 60 s, the same ground).
2. Quick builds take planning changes now (a track's grading, a door's apron, a road's levelling): the quick build compares the planning's ground with the record's and uses the new one where it changed; it refuses, naming them, if a step not being rebuilt also changed those samples, and it lists other steps' renderers standing on the moved ground (they keep their old heights until the round's full build). Test: the mound by Town Center Drive raised 0.3 m in its data, quick-built (6,337 samples moved), lowered again, quick-built: the scene identical to the full build's. Round 64's six one-hour rebuilds (aprons, the south lot's grading, the pit's ramps, the door frames) would each have been a quick build.
3. More quick steps: Shortcuts, CreekTrack, LakesRace (with TrailRace and Mall already): a quick build of all four is the full build's scene exactly.
Times on Dan's PC (in use): a quick build of the Mall 10 min, of the four track steps 11 min, quick with its exe about 14 min; the full build about 20 min. The quick build's own cost is now mostly the planning's replay (about 7 min).
`Builds/ChastainRush/` 2026-10-24 17:33 (the same world as Round 64's; Round 64's checks on it: doors, the north wing's grille, the mound, 12 of 12). Imported from Racer: nothing.

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
