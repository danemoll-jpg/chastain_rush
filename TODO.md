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
**Round 73, PUT RIGHT WHAT DAN FOUND, AND CHECK IT THE WAY HE PLAYS (Dan, 2026-10-26 19:04; reports BUG-001 to BUG-006 of session `2026-10-26_18-37-35-665_130b58`,
read by Claude, after the Round 70 to 72 build).** Dan: "that's a lot of work for a bunch of junk. Really not happy with anything I tested.
The closest thing to being good is the big ramp off of kennesaw ... It was all like the code was like I did something, here you go and didn't
even verify that any of it worked. Which is really frustrating because I wasn't even home and it had plenty of time to work things out."
Quick builds while working; ONE full Build Map at the end. Short results. No new features this round.
0. A NEW STANDING RULE (copy into CLAUDE.md "Process", in these words): CHECK IT AS DAN PLAYS IT. Scripted perfect lines are not enough.
   (1) Every report of Dan's: a picture from his exact position and heading, before and after, that Code LOOKS AT and describes in the
       status; a report is closed only when the after picture shows the thing gone. Re-read every earlier report on the same ground too.
   (2) Every feature: play it from where a player starts (the title, race setup, Free Roam's spawn or fast travel, a road nearby), with the
       normal controls through the game's input (a human-like driver / pilot: reaction lag, imperfect lines, a missed gate, a crash and a
       reset), and pictures from the player's camera along the way that Code looks at: can a first-time player see where to go? does it
       look right (no blocks, nothing buried, nothing floating, nothing across the road)? List what a first-time player sees and where it
       went wrong.
   (3) The status says, per item, what was looked at and what it showed. "Checked by script" alone is not done.
A. KENNESAW MOUNTAIN.
A1. BARRIERS ACROSS THE COURSE. BUG-001 (x -866.04 z -5208.17 h 56, before the start line): "This was reported. Why is it still here?";
   BUG-002 (x -1084.27 z -5837.29 h 49, the drive, CP5 next): "this too! How was this missed? There were two literal barriers in the middle
   of the track. They were reported as bugs but ignored and not fixed". Both pictures: a red-and-white race barrier straight across the
   road the race drives on. (Claude's fault in part: in Round 70 Claude misread Dan's 2026-10-25 BUG-001 as the start gantry and BUG-003
   "again" as hydrants; both were these barriers.) Remove every barrier standing on the course's own line, here and on EVERY course in both
   directions (check each course's line for any closure, barricade or solid object across it; a picture at each).
A2. THE DRIVE AT THE SUMMIT. BUG-003 (x -933.27 z -5721.73 h 141, Kennesaw Mountain Drive by the summit lot, J1's lip): "this road also
   looks like it wasn't fixed. I mean its changed but the road will be inoperable in free roam. It needs to be fixed" (the picture: the drive
   cut into the jump's hump at the lot's edge, the road broken). The drive must be a whole, normal, drivable road in Free Roam (traffic, a
   car driving up to the summit at normal speed, both ways); the race's jump keeps its flight but must not wreck the road: e.g. the jump on
   its own line beside / off the drive, joining it smoothly. Pictures driving the drive both ways in Free Roam at Dan's spot.
A3. THE BIG RAMP AT THE TOP. Dan: "The closest thing to being good is the big ramp off of kennesaw, but why is it not even at the highest
   point of the mountain." Move it to the summit itself (the mountain's highest ground), with a run-up across the top (cut / graded as
   needed), launching off the mountain where the fall is longest. Dan, 19:11: "for the ramp on the top of the mountain I would prefer it to
   be more realistic. It doesn't have to be some huge structure just a jump off of something that could naturally be there that jumps and
   clears the mountain will be fine": NO steel trestle; the launch is something that belongs on the summit (a rock ledge / outcrop, the edge
   of the overlook, a cut bank at the end of the summit path), the ground itself shaped; a flight longer than Round 70's; a way back to the roads below; fast
   travel to the run-up. Verified as rule 0 (from fast travel, flat out, a short one, a reset).
B. THE QUARRY AIR RACE: REDESIGN THE COURSE AND ITS GUIDANCE. Dan: "the plane race, is just not laid out well at all. The first ring is down in
   the quarry nearly right beside the take off point. It is impossible to get there without lining up the plane but the plane should have
   already been going in the right direction to set that up. And if you missed a checkpoint, it is impossible to tell which is next, also I
   circled back to a cp that I missed (because it is too unforgiving) and you only get credit if you are going in the direction of the game.
   Also, after I got through the first 3 or 4, I had no idea where the next gate even was there was an arrow but it was only pointing up or
   down, turning the plane around I couldn't find it at all. I just quit the race. Really poorly designed".
   (1) FLOW: after take-off the first ring lies well ahead on the runway's heading, high enough to climb to; every ring is placed so that
       flying through it already points you at the next (heading change between rings small for their spacing; a hard turn only with room
       to make it); the quarry dive comes once you are set up for it, not straight after take-off.
   (2) FINDING THE NEXT RING, always: a 3D arrow / chevron pointing at the next ring in full direction (left, right, up, down, behind), a
       tall beacon on it seen from anywhere, the ring after it shown dimmer, its distance; the minimap shows the next rings. After a miss:
       a clear "RING MISSED" and the arrow at once on the next one.
   (3) FORGIVING: rings bigger (tight ones only where the place demands it, e.g. under a bridge, and still makeable); a ring counts flown
       through either way; a missed ring may be gone back for (then no penalty), or skipped (the usual penalty when you take the next).
   (4) Verified as rule 0 with a human-like pilot: take-off, every ring in order, a missed ring found again, getting lost and finding the
       way by the arrow; cockpit pictures at each ring showing the next one visible or the arrow pointing at it.
C. THE QUARRY JUMPS.
C1. BUG-004 (x -2024.24 z -1350.35, the bleachers): "whats going on here? Are these supposed to be people? Why are they just blocks?" Use the
   game's real people (the models the neighborhood's people use), seated, varied; no blocks.
C2. BUG-005 (x -2423.25 z -1323.20, the far bleachers / the rim): "why even put people in here if you can't make it even make it remotely
   correct? People are literally buried". Every spectator on their seat or standing on the ground, none in it.
C3. BUG-006 (x -2398.76 z -1359.36, down in the pit under a ramp's steel trestle): "stuck here. reset button does nothing. But it looks like
   this is buried in the ground anyway. Truly don't even understand how to get to the ramps in general". (1) No way to drive in under a
   trestle or get wedged anywhere round the ramps (solid skirts, the ground right). (2) RESET MUST ALWAYS WORK, anywhere in Free Roam
   (here it did nothing): find why and fix it for the whole map. (3) GETTING THERE: a clear way from the roads to each run-up (a drive /
   track from the airport's roads, signs saying QUARRY JUMPS at the turn-offs, the start marked: drive into a lit start box to begin),
   and fast travel to each start. Verified as rule 0: from fast travel and from Free Roam's roads, a first-time player finds each start.
C4. THE RAMPS THEMSELVES. Dan, 19:11: "honestly you said code could jump those ramps, I have no idea how they did as nothing lined up and the
   ramp was buried in the hillside". Rebuild each jump so run-up, kicker and landing deck lie on one straight line that a player can see
   from the start box; the kicker stands clear of the hillside (nothing of the slope over or in front of it); the landing deck in plain view
   across the water. Verified as rule 0: pictures from the start box (the whole line visible), from the run-up at speed, from the lip, and
   of the landing; flown flat out by the human-like driver, not the script that already knew the line.
C5. A QUARRY TRACK. Dan, 19:11: "I think for the quarry, it has to be some sort of track. We can have those ramps in free roam but having a
   track will make it easier to at least discover the first time. Problem is its too big of an area to search for them." Make a race course
   (in race setup, forward; a reverse only if it plays well) round Viking Quarry that takes in the jumps (at least The Corner and The Long One,
   The Big One if the line allows), the rim and the haul roads down to the water's edge: checkpoints lead you to each ramp's run-up in
   line with it, so racing it shows a first-time player where every jump is. Built like the other courses (AI that takes the jumps, records,
   ghosts, replays, loading inset, map). The ramps stay in Free Roam as well (the scored events), now easy to find from the track and the
   signs.
Check: rule 0 for every item; the pictures listed above; one AI race per car course (all finish, no barrier on any line).
For Dan to check: Kennesaw Mountain race (no barriers), the drive in Free Roam, the summit jump; the Quarry Air Race; the quarry track and
jumps (the crowd, the ramps' lines, getting there, getting stuck, reset).

## QUEUED rounds
Laura Lake (Dan, 2026-10-22 05:37): "Oddly enough I had no clue about the laura lake which code refered to as a quary. its a very cool
area where we could have some really big jumps". What to make of it (a free-roam jump area, a race, a stunt course) waits for Dan. Dan, 2026-10-25 02:11: "I was mistaken this is actually called 'Viking Quarry Lake overlook'...I have no idea. Maybe if we add planes
this could be a cool area. also maybe there is a car race or stunt races that have large ramps to jump you over evel knievel style". Ideas
so far (not a round yet): planes (later, from the airport next door); a car race round the quarry; stunt events with huge Evel Knievel-style
ramps launching you across the quarry lake. Use the name OSM gives it (check), else "the quarry".

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.

## Previous delivery 1 (newest): Round 72
**Reviewed: Dan tested the Round 70 to 72 build 2026-10-26 18:37: "that's a lot of work for a bunch of junk. Really not happy with anything I tested." His notes are Round 73.**
**Round 72, EVEL KNIEVEL JUMPS AT VIKING QUARRY LAKE, and four known fixes (Dan, 2026-10-26 07:17: picked "Quarry stunt jumps" to run
while he is at jury duty; his idea 2026-10-25 02:11: "stunt races that have large ramps to jump you over evel knievel style"). START RIGHT
AFTER ROUND 71 IS DONE AND COMMITTED, without waiting for Dan; he is away all day: decide, write it down, go on.** Quick builds while
working; ONE full Build Map at the end. Standing rules (real physics off every ramp; nothing slows a car on a ramp; landings clean).
Tuning limit. Short results.
A. THE QUARRY JUMPS (Free Roam events, the stunt park's family). Big steel stunt ramps on the quarry's rim, Evel Knievel style: a long
   straight run-up lane, a tall ramp with a starting gantry (stars and stripes bunting fine, no real names / brands), a landing ramp on the
   far side across the quarry lake, a crowd in bleachers, a PA voice-free fanfare. Three jumps of rising size (Code designs them from the
   flight, as Round 68): a short one across a corner of the lake, a long one across the middle, and THE BIG ONE across the widest part (the
   longest jump in the game after the mountain ramp, Round 70 A). Miss the landing ramp and you're in the lake (the game's water: reset).
   Scored events: distance, height, landing (clean / rough / crash), with Top 10 records per jump; a run-up countdown start; a replay of
   the jump (Round 57's replays: the TV cameras placed for the flight). Rewards / unlocks: none yet (the campaign is held). Fast travel to
   the quarry's start gantry. Reachable by any ground vehicle, including bikes and stunt vehicles; flying vehicles not in the events.
B. KNOWN FIXES (found by Code in earlier rounds, not yet asked for by Dan; small):
   B1. Three heavy cars (Longroof GT, Highball Fastback, Skyfin Cruiser) stop hard at 36 to 39 m/s on Kennesaw Mountain Drive at
       (-500, -5320) (Rounds 67, 68): find and fix.
   B2. Canton Road's three map-edge closures (Round 66): a car can end up on top of the barriers; make them impossible to climb.
   B3. Round 64 C's junction levelling: drives with air over 0.15 s went up; find the worst junctions in the jolt table and smooth them.
   B4. Round 68's J2 kicker: the Drifter Twin flat out loses 12 m/s on its top; ease it without shrinking the jump (no more than two tries).
Check: each jump flown flat out on the Drifter Twin, the Street Classic, a bike and a stunt vehicle (distance, air, landing), short (into
the lake, reset), and the scoring and records; B1 to B4 before / after. Pictures: the three ramps from the run-up, mid-air over the lake,
the crowd, the landing.
For Dan to check: jump the quarry (all three); the four fixes.

**Status (Claude, 2026-10-26): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round72/` (`round72_check_final.txt`, `round72_check_short_replay.txt`, `round72_b*.txt`, `72_*.png`, `72_flights_profile.png`).
A DONE. Three steel kickers on Viking Quarry's south rim, run-ups from the airport's ground (graded smooth, 240 to 380 m), landing decks across the lake sloped a little shallower than the flights (catch zones), run-outs graded, bleachers with 747 people, start gantries with stars-and-stripes bunting and countdown lamps (`Tools/mapgen/make_quarry_jumps.py`, `MapBuilder.QuarryJumps.cs`, event `Stunts/QuarryJumps.cs`). The vehicles' speeds at a lip differ too much (47 to 57 m/s flat out) for one landing deck to catch them all, so each jump has Evel Knievel's speed check: a lip speed window on the HUD (The Corner 82-88 mph, The Long One and The Big One 93-100 mph), green inside it; too slow is short (lake or pit), too fast overshoots. Stop in the gantry's box facing the ramp: fanfare, 3-2-1-GO on the lamps. Scored: distance, height over the lip, air time, landing CLEAN / ROUGH / CRASH / SHORT / SPLASH; Top 10 per jump by distance (`quarry-jumps-v1.json` beside the records); short or crashed: back at the gantry after 3 s; L3 / K watches the jump's replay (TV cameras on the rims, the kicker, the landing deck). Fast travel "Viking Quarry: <jump>" to each gantry. Flying vehicles not in the events.
   Flights at the window's middle (Street Classic, Drifter Twin, Needle 600, Skywhip 250 all CLEAN): The Corner 166-172 m, 5.1 s; The Long One 203-209 m, 5.1 s; The Big One 245-246 m, 6.4 s (the longest after the summit ramp). Flat out (Drifter Twin, outside the window): 244 m rough, 247 m clean, 338 m crash. Short (68 mph at The Corner): into the lake, back at the gantry. Replay opens (5 TV cameras). The air race's line re-probed with the ramps up: clear, a clean lap.
B1 NOT REPRODUCED. Longroof GT, Highball Fastback, Skyfin Cruiser and the Street Classic driven round the Kennesaw lap: no hard stop anywhere (Round 70's widened J1 bench at that spot is the likely cure); nothing changed.
B2 PARTLY. Canton Road's edge closures (and the service road beside one) get 7 m more barrier past each shoulder, solid to 4 m: 221 -> 222 of 224 closures now stop the car (the service road fixed); the check still reads the car 13 m and 50 m past the middle at Canton Road's north and east closures, though the picture shows it stopped at the barricades (`66_closure_*.png`). One try (tuning limit not used up; left for Dan to look at).
B3 PARTLY. The six junctions Round 64's jolt table still threw a car at, blurred after the levelling (`Data/tracks/round72_junctions.json`): air at 28 m/s Barrett at the I-575 ramp 0.96 -> 0.72 s, Frey Road 0.74 -> 0.56 s (jolt 3.9 -> 1.8), Busbee at Townpark 1.12 -> 0.24 s, McCollum at Cobb Parkway 0.54 -> 0.42 s, North Marietta 0.48 -> 0.26 s; unchanged: Chastain Road at Busbee 0.78 s, Cobb Parkway North 1.20 s.
B4 NOT FIXED. The Drifter Twin flat out drops 39.5 -> 28.2 m/s in 0.3 s right at J2's lip, its body pressed into the kicker's top (impulse about 3900). Try 1 (the ground not pushing on an upright body over the kicker, as Elaine's climb): no change, taken out. Stopped at one try; the cause needs a closer trace.
Builds: two full Build Maps (the jumps' first build showed the ground over the kickers; the second after a quick build fixed it, as the round's last) and code-only exes for the event; `Builds/ChastainRush/` 2026-10-26 10:12. Imported from Racer: nothing.
For Dan: (a) the speed check: is a 7 mph window fun or too fiddly (it can be widened, the landing decks lengthened); (b) the bleachers and gantries' look; (c) B2's two Canton Road closures and B4.

## Previous delivery 2: Round 71
**Reviewed: Dan tested the Round 70 to 72 build 2026-10-26 18:37: "that's a lot of work for a bunch of junk. Really not happy with anything I tested." His notes are Round 73.**
**Round 71, A PLANE RACE THROUGH RINGS (Dan, 2026-10-26 03:09: "I am thinking that we could do a plane race. I think just the typical fly
through the rings course. We could utilize the quary right next to the airport and just expand it. Since it is the sky we are pretty open.
I would like to have low spots and maybe have to fly under bridges if possible. It could be a circuit. It could be a race with others or
solo speed run"). Dan is asleep and has not tested Round 70: decide, write it down, go on.** Builds on Round 69's Sky Hopper. Quick builds
while working; ONE full Build Map at the end if anything in the world changes (rings are game objects; likely code / data only).
Tuning limit. Short results.
A. THE COURSE: a circuit of rings in the sky starting and finishing at Cobb County International Airport, built round Viking Quarry Lake next
   door and expanding out from it (Code designs it and says why). Low spots: down into the quarry, low over its lake, skimming fields and
   the woods' tops; under bridges where the plane fits (the I-75 / I-575 overpasses and their ramps near Big Shanty / Chastain Road / Barrett
   Parkway: check the clearance with the plane's wingspan and height, Round 69's wing-tip test); a few high rings to climb for and dive
   from; banked turns between them. A lap of about 2 to 3 minutes at race pace; 2 or 3 laps, Code's pick. Name it (e.g. "Quarry Air Race"),
   Dan may rename.
B. THE RINGS: big readable hoops (a bright color, a beacon / arrow to the next one, a chime and a flash going through), sized to the spot
   (larger in the open, tighter under bridges and in the quarry); a ring missed counts as a missed checkpoint (the game's usual penalty, the
   lap still counts: Round 54 B's rule). Crashing resets the plane in the air just before the last ring passed, heading through it, at
   speed (never on the ground, never stuck).
C. THE START: from the runway, lined up (a standing take-off start: throttle up at GO), or an air start if the runway start plays badly with
   several planes (Code picks, says why).
D. TWO WAYS TO PLAY, both in the race menu (and Free Roam's race list): RACE against AI pilots (3 by default, as the car races; the AI flies
   the ring line with skill by difficulty, Hard close to a good player, no cheating through walls, recovers from crashes as the cars' AI)
   and TIME TRIAL / SPEED RUN solo (best lap and best total, ghost of the best lap, Top 10 records, replays: as every course). The plane only
   (the Sky Hopper; the helicopter is not in this race).
E. Built like the other courses where it applies: records, ghosts, replays (the replay cameras follow planes), the map and minimap showing
   the rings, the loading screen's inset, the finish screen and COMPLETE RACE.
Check: a lap by script flown cleanly through every ring (no ring unreachable, no bridge clipped); a lap with a missed ring (penalty, lap
counts); a crash and its reset; the AI x2 on Hard, all finish; a time trial with its ghost. Pictures: the start, the quarry dive, under a
bridge, a high ring, the whole circuit on the map.
For Dan to check: race the planes and fly the time trial (rings, the quarry, under the bridges, the AI).

**Status (Claude, 2026-10-26): built and checked in the exe by script, not yet flown by Dan.** Evidence `Data/screenshots/round71/` (`round71_check_final.txt`, `71_*.png`, `71_line_trace.tsv`). No bug reports newer than Round 70's session.
A DONE. "Quarry Air Race" (`Tools/mapgen/make_air_route.py`), clockwise, 16.2 km, 2 laps (a lap 2:33 flown by script at full throttle): the start ring 14 m over the runway; Viking Quarry: in over the south-west rim, Q1 halfway down the 200 m pit, Q2 over the lake (35 m up: the dive and the climb out are rounded to the plane's pitch rate, it cannot get lower), Q3 over the east rim; C1 UNDER I-75 southbound where it crosses Noonday Creek (a frame, 9 m from the bank to the deck as built), then C2 straight up OVER I-75 northbound; H1 270 m over Town Center; Barrett interchange; the treetops; H2 over Kennesaw Mountain's summit; the woods; L1 16 m over Daniell Lake; over Cobb Parkway; the runway's line to the finish. Under bridges only where the plane fits: I-75 northbound / express (3 m from bank to deck) and Cobb Parkway's railroad cut (V-shaped, narrower than the wings) were measured and left as rings over them; bridges along roads left out (lamps, gantries, traffic). The two low lanes (I-75 approach, Daniell Lake) are cleared of trees by the map build (`Data/tracks/quarry_air_race.json`); traffic is kept off the roads under the line's low stretches.
B DONE. Rings 24 to 36 m across, frames sized to the opening; the next one green with a beacon, later amber, missed red; through one: a flash and a chime; the HUD arrow says how far up or down. A missed ring is 5 s, the lap counts (check: ring 3 missed, +5 s, lap counted). A crash: 2.6 s, then back in the air 150 m before the last ring, on the line, heading through it, 70 m/s (check: in the air 115 m out, flying on, lap finished); R / Y the same.
C DONE. Standing take-off on runway 09 (the player at the back on the centerline, AI ahead in two lanes). Race planes pass through each other (a mid-air touch would end two runs), so a runway start cannot pile up.
D DONE. Race setup: Route "Quarry Air Race" puts on the Sky Hopper; Opponents "3 AI pilots" or "Solo (time trial)". AI pilots (`Race/PlanePilot.cs`) fly the plane's own controls by difficulty (Hard full throttle, Normal 94 %, Easy 88 %), slow for turns and dips they cannot follow, sometimes line a ring up wide and miss it (more on Easy). 2 AI on Hard, 2 laps: both finished flown (5:11.7 with one ring missed, 5:06.8) against the scripted player's 5:06.7 (same flying code: a human who cuts the rings' corners will be faster). Not in split screen.
E DONE. Records / Top 10 per vehicle (time trial: Top 10 #2), clean-lap ghost saved and flown on the next run, replays (TV cameras placed in the air for a plane), the map and minimap show the rings, the loading screen's inset, finish screen, COMPLETE RACE (generic).
Builds: one full Build Map (06:03), then a quick build for the route's line after C1 and a code-only exe for the AI's dips; `Builds/ChastainRush/` 2026-10-26 07:08. Imported from Racer: nothing.
For Dan: (a) C1 under I-75 is tight on purpose (about 1.5 m to spare at the deck); say if it should be easier; (b) the AI's pace against yours; (c) the lap's length (2:33).

## Previous delivery 3: Round 70
**Not yet driven by Dan: he moved on to Round 71 on 2026-10-26 03:09 ("Don't have a chance to test").**
**Round 70, A RAMP OFF THE MOUNTAIN; AI THAT RACES; the garage and the saved vehicle; park visitors; the mountain road (Dan, 2026-10-25
23:20; reports BUG-001 to BUG-004 of session `2026-10-25_22-20-32-569_189622`, read by Claude, after the Round 68 / 69 build: Kennesaw Mountain on the Trail
Four).** Rounds 68 and 69 are reviewed. Quick builds while working; ONE full Build Map at the end. Tuning limit. Short results.
A. A RAMP THAT CLEARS THE MOUNTAIN (Free Roam; later maybe a point-to-point race). Dan: "I am not sure about a reverse of the kennesaw
   mountain track. But I want a big ramp on the edge that drops all the way off the mountain. This maybe an eventual Point to point race,
   but at the moment just give me a big ramp that will clear the mountain". Woodstock Rush's summit flight in spirit (Racer's CR101-104:
   a 215 m straight run-up from rest, 36 to 43 m/s at the lip, a flight that cleared the whole mountain to a catch slope). Build one big
   ramp at the summit's edge (a built ramp is fine here: steel or timber, a real thing a stunt crew would put up), with a long straight run-up
   on the summit (the lot / the drive's top / a cleared line), aimed where the fall is longest and the landing open (Code picks the side and
   says why), so a car flat out flies off the mountain and comes down far below at its foot on a long smooth catch slope (cleared, graded,
   landing clean and keeping speed), with a way back to the roads. Target: the biggest flight in the game by far: 8 s or more in the air and
   several hundred meters (report air, distance, height for the Drifter Twin, the Street Classic, a bike). A fast-travel spot at the run-up's
   start; visible from the summit lot (no "JUMP" signs needed: the ramp is obvious). Not in the race's lap (Round 68's course unchanged).
B. THE AI IS TOO SLOW. Dan: "The AI is really slow. I watched the replay and they dont seem to be getting hung up. I think their issues
   maybe either they aren't going full throttle or possibly just follow the courses too exactly. For example there are times when you make
   a path for the next checkpoint that doesn't follow the exact roads, I think the ai should be not so slavish to the actual roads".
   (1) Measure first, from the replays' recordings (Round 57): per AI on Hard, throttle use (time at full throttle on straights), speed
       against the player's on the same stretch (Dan's best laps / ghosts where saved), and where the time goes (corners, braking early,
       following the road's curves where the course allows a straighter line).
   (2) Racing lines: the AI drives a racing line between checkpoints (cut corners on grass, lots and open ground where the course allows
       it, use the whole road, apexes), not the road's center line; full throttle wherever it can; braking as late as the vehicle allows.
   (3) Hard races Dan close on every track (within a few seconds of his best laps); Normal and Easy scale down by pace, not by dumb driving.
       Report a table per track: Dan's best lap, Hard's best and average, before / after.
C. THE GARAGE IN RACE SETUP. Dan: "In race setup, I am not able to select a car in the garage some times". Find when it fails (which
   vehicles, after what, pad or keys) and fix it. Dan, 23:27: "I started a race as soon as the game started, it ended and that was when I
   tried to select a vehicle. I hadn't touched the plane at that point": so reproduce exactly that first: launch the game, start a race
   straight from the title (no Free Roam first), finish it, go to race setup's garage from the finish screen / menu and try to change the
   vehicle (pad and keys). Dan, 23:28: "I had to go into free roam to change vehicles so I know that worked. I couldn't do it in race
   setup": Free Roam's vehicle change works; the fault is in race setup's garage (after a finished race). Then also after Free Roam; check every
   vehicle selectable in race setup by pad and keys, after Free Roam in the helicopter and in the plane.
D. THE SAVED VEHICLE. Dan: "why does my vehicle keep getting changed every time I start the game?" (`vehicle.json` holds "plane", Free
   Roam's last pick; the log shows the game starting on the Trail Four). Remember Dan's choice: the last ground vehicle for races and the
   last Free Roam vehicle separately, and start each with what he last chose (never a random or substitute one; a flying vehicle only where
   it is allowed). Also: `vehicle.json`'s key "colour" -> "color" (American English rule), reading the old key once.
E. PARK VISITORS. Dan: "I thought there were supposed to be joggers and walkers on the kennesaw mnt trail". Round 67 added up to 9 groups in
   Free Roam and 2 in a race; Round 68 moved the race onto new lines. Make them clearly there: walkers, joggers, hikers with dogs on the
   park's trails and at the summit and visitor center lots in Free Roam (plenty), and a few beside the race's lines in races (they get
   out of the way). Check by pictures from the summit and the trails.
F. THE MOUNTAIN ROAD.
F1. BUG-002 (x -1097.14 z -6077.09, Kennesaw Mountain Drive): "why so many fire hydrants? You wouldn't have them on a mountian path at alll";
   BUG-003 (x -1072.23 z -5825.67, the drive): "again". No hydrants on the mountain drive, the park's roads and trails, or any road through
   woods / parkland with no buildings near: hydrants only where there are buildings along the street (all the map, by rule, not just here).
F2. BUG-004 (x -949.83 z -5725.88 h 52, the drive near the summit, a jump on the course): "the road shouldn't be doing this. It should be
   flat while still keeping this jump" (the picture: the road rolls steeply sideways over the crest). Keep the jump's lift but make the
   road flat across its width there (no camber / roll); check the jump's flight is unchanged.
F3. BUG-001 (x -872.73 z -5208.19 h 32, the start line on the drive): "why is this on the ground?" (the picture: the start line's checkered
   banner / gantry sits low, its left end down in the ground on the slope). Stand the start gantry up properly over the road (posts on the
   ground both sides, banner level and clear over the road); check every course's start / finish gantry the same way.
Check: A's flight table; B's tables (before / after) and an AI race on Hard on three tracks; C every vehicle in race setup; D restart the
game twice after Free Roam in the plane and after a race (the right vehicle each time); E pictures; F1 hydrant counts on the drive before /
after; F2 the road's roll before / after; F3 pictures of each start gantry.
For Dan to check: fly off the mountain; race the AI on Hard; the garage; restart the game; the park's visitors; the mountain road.

**Status (Claude, 2026-10-26): built and checked in the exe by script, not yet driven by Dan.** Evidence `Data/screenshots/round70/` (`round70_check_before.txt`, `_after.txt`, `_final.txt`, `round70_ai_*.txt`, `round70_vsave1-3.txt`, pictures `70_*.png`). Bug reports read: session `2026-10-25_22-20-32-569_189622`, BUG-001 to 004.
A DONE. The summit ramp (`make_summit_ramp.py`, `MapBuilder.SummitRamp.cs`): the drive ring round the knob leaves no 200 m straight on top and the north-east run is J1, so the run-up is the drive's south-flank straight (258 m, about +9 %, fast travel "Kennesaw Mountain: the big ramp" at its start): straight on past the hairpin round the knob's west end, a steel ramp on trestles (79 m, 21 degrees, its lip 57 m over the falling face) out over the north-west face at 296 degrees, the line threading Greymont Circle's houses (none within 35 m of it for 960 m). Catch on the graded foot, a gravel arrester, a track on to Old Mountain Road NW. Flat out from rest (`70_ramp_*.png`): Drifter Twin 8.6 s / 341 m / 174 m up; Street Classic 8.4 s / 296 m / 154 m; Needle 600 8.9 s / 358 m / 180 m; all land upright and roll to a stop at Old Mountain Road (11 to 14 m/s). In races concrete barriers close its mouth. Visible from the drive and the run-up; from the summit lot itself the knob hides most of it (`70_ramp_from_summit_lot.png`).
B PARTLY (tuning limit, two attempts). Measured first from Dan's replays (`Tools/race/replay_ai_table.py`) and a trace of what set the AI's speed (`ai_trace_why.py`): throttle use was like Dan's; the time went to phantom bends from the road's raw centerline, held slow zones (the trail's turns 11 to 14 m/s where Dan does 23 to 41), Kennesaw's 36 m/s hill cap, barriers and roadside things seen as obstacles, held jump zones that ignored bends. Now: a racing line through the lane and the open shoulder (whole road, apexes, grass and lots where clear), bends from it, zones within its bend limit, off-line things ignored, trail zones raised, Hard's pace .97 to 1.0, Easy / Normal by pace (88 / 95 %). Hard, flying laps, best / average (Dan's best): Kennesaw 157.2 / 166.4 -> 141.8 / 144.5 (113.2); Noonday Creek Trail 230.0 / 234.7 -> 200.9 / 212.9 (169.2); Chastain Glen 39.6 / 44.4 -> 39.1 / 43.9 (38.9); Chastain Lakes 88.8 / 104.0 -> 91.7 / 94.3 (72.1). Not within a few seconds except the Glen; part of the gap is the AI's vehicles (Street Classic, Longroof GT against Dan's Trail Four / Drifter Twin).
C DONE. A finished race counted as "racing", so race setup's garage shut its vehicle row. After a race started at once and finished: all 11 racing vehicles chosen by pad and by keys; also after Free Roam in the helicopter and the plane.
D DONE. `vehicle.json` keeps "roam" (Free Roam's last pick, the plane too) and "race" (the last ground vehicle chosen), "color" (the old "colour" / "vehicle" read once); forced swaps (a route's vehicles, police, stunts) are never saved. Three restarts from Dan's old file: the game starts in the plane, race setup puts on the Drifter Twin each time.
E DONE. Kennesaw's park: up to 12 groups near a view in the park's hours (was the streets' 2 to 8), the summit and visitor center lots walked too, hikers with dogs, joggers, couples, families (no bikes); 5 at most in a race. Measured 12 to 15 groups at the summit lot, visitor center and trails, 5 in a race (`70_park_*.png`).
F1 DONE. Hydrants only where a building stands within 60 m, and none on the park's own roads: Kennesaw Mountain's drive and park 47 -> 0 (7 left in the measuring box are on the neighborhood streets north-east of the visitor center).
F2 DONE. J1's bench widened to the drive's far edge (the drive lay half on it): roll at BUG-004 37.6 -> 3.6 degrees; J1's line and flight unchanged (same profile).
F3 DONE. The start / finish gantry upright, each post on its own ground (in closer where the bank climbs), the banner level over the higher side; every course pictured (`70_gantry_*.png`).
Found, not changed: the Round 20 P1 check (`Round20Runner.Ramp.cs`) no longer matches P1 as rebuilt since (no vehicle leaves its lip in that script); nothing there was touched this round.
Builds: 2 full Build Maps (the second for F1's park roads), 3 code-only exes. `Builds/ChastainRush/` 2026-10-26 02:20. Imported from Racer: nothing.
For Dan: (a) the ramp: the run-up along the drive and off the hairpin; (b) the AI on Hard; (c) the ramp's look (57 m of steel trestle).

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
