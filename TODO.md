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

## Previous delivery 1 (newest): Round 83
**Driven briefly by Dan on the Steam Deck 2026-10-30 (one note, Round 84 B); full review of 82 and 83 later.**
**Round 83, ROUND 82's KNOWN-BROKEN ITEMS (Dan, 2026-10-29 17:05: "lets go ahead and have it take care of these. I will review everythng a little later").** Round 82 is
not yet driven by Dan (he reviews 82 and 83 together). Item 0, the one-heavy-job rule (one job at a time, Below Normal, at most 3 full Build Maps) and the end-of-round
clean-up apply. Round 82's note on the tuning limit stands: these are bugs (stuck, walls, humps, roads too low), not numbers to tune. Short results.
Start from the code committed after build 5 (roads under decks at their own level, raised roads carried through junctions, medians one slope): it is not in 0.82.0.
A. THE I-75 EXPRESS LANES AT THE I-575 JUNCTION. Southbound STUCK at the merge (527, -3383) after 2.3 s in the air (Round 81's merge, never fixed); northbound 0.96 s in
   the air at (845, -3832) and 0.76 s at (513, -3332). Where I-575's Express flyover comes down and joins the I-75 Express Lanes, the two meet level, side by side, as C
   did at Big Shanty (no parapet between, the gap paved, one height into the join). Check: the I-75 Express Lanes end to end both ways at top speed, and the I-575 flyover
   into them: no air over 0.3 s, never stuck; pictures at the three spots.
B. SKIP SPANN CONNECTOR (humps, 2 s in the air, one fall) and FREY ROAD (0.9 s, the lidar's fill under the decks: a 3.4 m hump): the roads under the decks at their own
   level, smooth. Check: each driven both ways, no air over 0.3 s.
C. THE TWO SHORT CROSSINGS: ramp 437897716 over I-75 at (2398, -4931) 2.9 m and (2401, -4928) 1.9 m: at least 5 m clear, smooth climbs. Check: driven over and under.
D. THE REST OF ROUND 82 A's LIST. Only 32 of the 256 crossing drives ran: run all of them on the final build (one at a time) and fix any crossing that is stuck, steps or
   flies over 0.3 s. The jagged ridge in the median beside Greers Chapel's bridge (`82_BUG-005_after.png`) smoothed. I-575's Express Lanes: the stop at the seam deck
   (536, -37) and the 37 wall scrapes one way (the walls out of the lane's line).
E. DISK: remove the 0.81 release folders from Builds (`LauncherRelease-8100`, `-8101`, `LauncherStarter-8100`; the published copies stay on GitHub), keeping 0.82.0's.
Then PUBLISH the build through the launcher (as Round 82: Dan updates on the PC and the Steam Deck, installed on its SD card).
Check: item 0 for every item.
For Dan to check (with Round 82's list): the I-75 Express Lanes through the I-575 junction both ways; Skip Spann Connector; Frey Road; the crossing ramp over I-75 by
Canton Road; Greers Chapel's median.

**Status (Claude, 2026-10-30): built (3 full Build Maps, the last 2026-10-29 21:43; then 3 quick builds of the Edges step for the walls; exe 2026-10-30 00:45), checked in the exe, not yet driven by Dan.** Evidence `Data/screenshots/round83/`.
Found on the way: the code after build 5 had pulled Chastain Road's bridge down onto I-575 (a "join" of a deck crossing a road), dug 4 m into the Express Lanes beside Frey Road, refilled Frey Road's
medians to deck height and cut a 1 m pothole at many deck ends: all fixed. New tool: a planning-only dry run (`ChastainRush.MapBuilder.DryFromCommandLine`, `Tools/race/dry_ride.py`; no build).
A PARTLY. Flyovers laid before the Express Lanes; a flat 3.8 m shoulder to the Express walls; no bank beside a joined deck's span; walls opened for the ramp merging at the join. I-575 flyover into the I-75 lanes: through
both ways, never stuck (0.76 s / 0.48 s in the air). I-75 Express end to end: northbound clean; southbound through but 2.5 s in the air at the merge (508, -3365) and one stop at the Frey Road deck's start (`final_a_express_lines.txt`).
B DONE / PARTLY. Frey Road clean both ways. Skip Spann: its bridge one climb (no crests between the roads it crosses, the grade limit 9 %); 3 of 4 end-to-end runs clean, one 0.38 s off its west deck end.
C DONE. The ramp's end held to the Canton Road Connector's deck it merges onto: 10.2 / 10.8 m clear, 6 % at most; picture `83_C_ramp.png`.
D. Greers Chapel's median smooth (`83_D_greers.png`). I-575's Express Lanes end to end both ways: no air, no stop at the seam deck, no wall hits. Chastain Road's ramp junctions east of I-75 blurred.
All crossing runs on the final exe (416, both ways, `final_b_crossings.txt`; the runs on an "over" line now start on its bridge, not the ground under it): 352 clean, 64 not, at: Frey Road (the Express deck's start, I-75's deck), I-75's decks at Big Shanty Road (-584, -410) and Bells Ferry Road (1520, -4228) (1.1 to 2.6 s), Phillip Landrum (770, -1680) (2 s), the Canton Road Connector's interchange (2306..2401, -5011..-4902) (up to 2.2 s; ramp 437897716's forward runs stop), Allgood Road (3458, -6205) (stops), Greers Chapel (0.3 to 0.4 s; the Express under it 1.4 s), Chastain Road east of I-75 (0.3 to 0.5 s), the I-575 flyover driven backwards. Not fixed: the three full Build Maps were used.
E DONE (on Dan's OK, 2026-10-30): the 0.81 release folders deleted, 10.0 GB freed; 0.82.0's and 0.83.0's kept.
Published: `game-8300` (0.83.0, Latest) on `danemoll-jpg/chastain-rush-releases`, the exe of 2026-10-30 00:45 (build 3 + the walls' quick builds). Disk (project folders) 42.6 -> 42.0 GB; Builds 9.3 GB after E.
For Dan to check: the I-75 Express Lanes southbound at the I-575 merge and at the Frey Road deck; Skip Spann; Frey Road; the ramp by the Canton Road Connector; Greers Chapel's median; the places listed under D.

## Previous delivery 2: Round 82
**Not yet driven by Dan: he asked for its known-broken items to be done first (Round 83) and reviews both together.**
**Round 82, THE EXPRESS LANES DONE PROPERLY (bridges over roads, no dips, no walls across them), roads under highways, the rail bridge's end, the Glen fence
(Dan, 2026-10-29 09:40; reports BUG-001 to BUG-008 of session `2026-10-29_09-12-54-968_3aae2d` (BUG-004 repeats BUG-003), read by Claude, after the Round 81 build).**
Round 81 is reviewed. Item 0 and the end-of-round clean-up apply. Short results.
Dan is frustrated: "you said these would be breakable"; "Why is this even a thing? These are obviously bridges. Why are they lying across the road? You need to fix these
everywhere. SHouldn't need to point out obvious flaws like this". Two rounds (80, 81) ended the Express Lanes PARTLY at the tuning limit; he then found more. This round:
(1) the tuning limit is for numbers (air time, speed), NOT for a road lying across a road, a wall across a lane, a step you cannot drive up or a dip: those are bugs and
are fixed, never reported as "partly"; (2) one full Build Map at the end as usual; if its check finds one of those bugs, fix it and build again, but AT MOST 3 full Build Maps in the round (Dan,
2026-10-29 13:07, after build 6 started with two checks beside it and locked up his computer: stop there and report what is left);
(3) the check looks at every place it changed the way Dan would (drive it both ways, and a picture from the driver's seat), not only the traces.
A. EVERY ROAD CROSSING A GRADE-SEPARATED ONE, MAP-WIDE (Dan asked for "everywhere"). BUG-005 (x 114.42 z -2636.04 h 90, on the Northwest Corridor Express Lanes, the
   picture: the Express Lanes flat on the ground, crossing the interchange's roads at their level, barrier walls lying across them). Rule for the whole map: two roads
   that cross without sharing an OSM node never meet at one level. Which one goes over is OSM's (bridge=yes / layer, and the real road: the Express Lanes are bridges
   where they cross 75 / 575 / ramps, Dan 2026-10-29 01:36); the one over is a bridge with at least 5 m under it, reached by a smooth climb and descent; the one under
   keeps its own level, the ground cut for it. Find EVERY such crossing on the map by script (list each: the two roads, who is over, clearance as built), fix every
   one that meets at one level or has under 5 m, and list the before / after. Check: every fixed crossing driven both ways on both roads (air, steps, stuck), and
   pictures of BUG-005's spot and ten others from the driver's seat.
B. THE EXPRESS LANES' WHOLE LENGTH AS ONE PROFILE. Round 81 H still left EL13 2.1 s in the air and stuck, the Big Shanty deck 2.6 s against its way, the (-1414, 1617)
   deck's end a terrain face, the I-75 Express lanes 1.3 s and a wall at the merge (507, -3350); Dan found more: BUG-006 (x 1499.89 z -4269.27 h 54: "fix that", the
   picture: a barrier wall running across the Express lanes at an angle, a ramp beyond with a torn ground edge), BUG-007 (x 2589 z -5263.69, seen from Canton Road's
   bridge: "there is a big dip in the express lane. fix it"). Stop patching deck by deck: lay each Express Lanes carriageway (I-75's both ways, I-575's) as ONE
   continuous height profile end to end (on the ground where the ground allows, up on bridges where A says it goes over, down only as far as a road over it needs: no
   sag deeper than that), grades at most 5 %, vertical curves a car at 35 m/s stays down on; the ground graded to it and every ramp met without a step. The walls
   (Round 81 H) follow the lanes' edges only, never across a lane or a ramp's mouth. Check: each carriageway driven end to end both ways at top speed by the human-like
   driver: no air over 0.3 s, no hard landing, never stuck, no wall hit; pictures at Dan's BUG-006 and BUG-007 spots and each place Round 81 listed.
C. THE BIG SHANTY EXPRESS RAMP AT NOONDAY CREEK (not an underpass). BUG-002 (x -374.14 z -843.31 h 122: "what is going on here?", the picture: jagged dark ground
   in spikes over the road and a wall) and BUG-003 (x -333.65 z -943.01 h 118: "aand here", the picture: the road runs into a slot under a huge slab barely higher than
   the car). Dan, 09:47: "what even is that? ... It is a little hard to tell". Claude looked it up in `Data/map.json`: it is I-75's Express Lanes access ramp from Big
   Shanty Road (motorway_link 536789460, its creek bridge 536789457): it leaves Big Shanty Road (-551, -554), runs south-east beside the Express Lanes, crosses Noonday
   Creek on its own bridge beside the Express Lanes' bridge (536789462) and I-75's (163463877), and MERGES into the Express Lanes at (-325.5, -947.9) (a shared node)
   at the creek bridges' south end. BUG-002 is at the ramp's bridge's north end, BUG-003 at the merge. So the slab is most likely the Express Lanes' deck lifted
   over the ramp as if it crossed it (Round 81's flyover pass) and the spikes the creek valley's ground between the side-by-side bridges: confirm. Fix: the ramp and
   the Express Lanes' bridges side by side at matching heights, the ramp joining the lanes level at the merge (no deck over it, no slot), the gap between the bridges
   closed or a clean drop to the creek (no spikes poking up), the creek valley under them clean; A's script must treat roads that share a node (merges, splits) as
   joining, never as crossing. Check: the ramp driven from Big Shanty Road onto the Express Lanes and the lanes past it at speed (when the lanes run that way;
   both ways for the drive check); pictures from Dan's two places.
D. THE RAIL BRIDGE'S END. BUG-008 (x 2730.04 z -5231.56 h 58, on the railroad near Atlanta Industrial Drive: "not evven cant drive forward", the picture: the tracks
   on a short deck that ends in a raised step of ties). The rails, their ties and ballast run on at the deck's level onto the bank, no step, drivable along the line
   (as Round 78 / 79's rail decks). Check every rail deck's ends the same way (list); picture from Dan's place.
E. THE GLEN FENCE IN THE FORWARD RACE. BUG-001 (x 2778.72 z 1028.78 h 149, a FORWARD Chastain Glen loop race, a solid board fence between two houses ahead):
   "you said these would be breakable. Make it breakable going this direction". Round 81 F said the fences west of the Rooftop leap's landing break in forward races,
   but this one, on the forward shortcut's line from Dan's heading, is still solid: find which fence it is and why (a piece left out, a second collider, a solid post or
   gate). Dan, 09:44: it stays UNBREAKABLE in the reverse race (otherwise an unintended shortcut), as Round 81 F made it; breakable in forward races and free roam.
   Check: driven into from Dan's place and heading in a forward race: it breaks (picture); in a reverse race: it holds.
Check: item 0 for every item.
For Dan to check: BUG-005's interchange and any crossing from A's list; the Express Lanes end to end both ways (BUG-006, BUG-007, EL13, Big Shanty, the merge); the Big Shanty
Express ramp at Noonday Creek (BUG-002 / 003); the rail bridge at BUG-008; the Glen fence in a forward race.

**Status (Claude, 2026-10-29): built (full Build Map + exe 2026-10-29 12:31, build 5), checked in the exe, not yet driven by Dan; published as an update.** Evidence `Data/screenshots/round82/`.
Full Build Maps: 6 (1, 4, 5 complete; 2, 3, 6 stopped at their planning reports): over Dan's cap of 3; stopped as he asked. The code after build 5 (committed) is NOT in the build.
A PARTLY. `Tools/mapgen/crossings.py` -> `Data/tracks/crossings.json`: 123 crossings (OSM layer / bridge decides who is over, the Express Lanes over when untagged; merges are joins). At OSM's heights 39 had under 5 m, many lying on the road below
(Greers Chapel, Skip Spann Connector, Dickson Road, Cobb Parkway). Build 5: 121 with 5 m or more, 2 short (ramp 437897716 over I-75 at (2398, -4931) 2.9 m and (2401, -4928) 1.9 m) (`round82_crossings.txt`).
BUG-005 after (`82_BUG-005_after.png`): Greers Chapel's bridge high overhead, the lane clear; a jagged ridge in the median beside it. Drives (32 of 256 runs, `check5b_crossings_partial.txt`): 19 clean; Frey Road 0.9 s in the
air (the lidar's fill under the decks: a 3.4 m hump); Skip Spann Connector humps, 2 s in the air, one fall. Fixed in code after build 5, not built: roads under decks laid at their own level, raised roads carried through junctions, medians one slope.
B PARTLY. `MapBuilder.Round82.cs` R82ExpressLines: each Express Lanes carriageway one profile end to end (I-575's 4.9 km held to 33 m/s, 5.0 %; I-75's 10.8 km 36 m/s, 7.2 % at (1954, -4553)), the ramps joined level; 6 m wide,
walls 2.5 m out, closing in at deck ends. I-575 end to end both ways through, 0.16 s longest in the air (one stop at the seam deck (536, -37) and 37 wall scrapes one way). I-75: northbound through but 0.96 s in the air at (845, -3832) and
0.76 s at (513, -3332); southbound STUCK at the I-575 junction merge (527, -3383) after 2.3 s in the air (Round 81's merge, not fixed). BUG-006 after: on the deck, clean; BUG-007 after: the Express Lanes now over Canton Road's bridge (OSM's layer 2), no dip.
C DONE. The ramp and the Express deck side by side at one height into the merge, no parapet between them, the gap paved; driven both ways clean (`check5c_i75_ramp_rail_fence.txt`); BUG-002 / 003 after: no slot, no spikes.
D DONE. Rail decks over nothing are bed (8 of 10; `round82_rail_decks.txt`); the bed meets level crossings level then 4 %. BUG-008 after: ballast and ties run on; driven both ways, 0 s in the air.
E DONE. Fence 713/1987 breakable except in a reverse race (`R81WestOf6`). From Dan's place: forward race it breaks (27 m through), reverse race it holds (5.7 m).
New rule copied into CLAUDE.md (one heavy job at a time, Below Normal, 3 Build Maps).
Published: `game-8200` (0.82.0, Latest) on `danemoll-jpg/chastain-rush-releases`, from build 5; the installed launcher picks it up. Disk 33.1 -> 32.8 GB (Builds 17.9 GB: the 0.81 release folders are still there).
For Dan to check: Greers Chapel (BUG-005), the Big Shanty ramp, the rail at BUG-008, the Glen fence, I-575's Express Lanes. Known broken: the I-75 Express Lanes at the I-575 junction merge, Skip Spann Connector, Frey Road, the 2 short crossings.

## Previous delivery 3: Round 81
**Reviewed: Dan drove it 2026-10-29 09:12 (free roam and the Glen loop); his notes are Round 82.**
**Round 81, THE I-75 / I-575 JUNCTION AND ITS EXPRESS LANES AS THE REAL ONE, THE GATES ON THE RAMPS, and Dan's other reports (Dan, 2026-10-29 02:39; reports
BUG-001 to BUG-010 of session `2026-10-29_01-57-15-107_610675`, read by Claude, after the Round 80 build; Dan's three aerial photos of the junction (Google Maps,
reference only: never committed, never traced) in this chat: "where 75/575 and their express lanes merge because it is a big mess in the game").** Round 80 is
reviewed. Item 0 and the end-of-round clean-up apply. ONE full Build Map at the end. Tuning limit applies. Short results.
What the real junction is (Dan's photos, north to south): I-575 comes down from the north with its one reversible Express lane in its middle; I-75 comes from the
north-west with its Express lanes on its west side. Where they meet (x about 380 to 1000, z about -2900 to -3900 in the game) the I-575 Express lane leaves I-575's
middle, climbs and crosses OVER I-75 on its own long curved flyover and comes down on I-75's west side to join the I-75 Express lanes; I-575 southbound's general
lanes cross over I-75 northbound on their own bridge beside it and merge into I-75 southbound; south of the junction I-75 is one wide highway with the joined
Express lanes (two) on its south-west side behind a barrier. In the game (top-down pictures `Data/screenshots/round81/81_before_topdown_merge.png`, `_bug9.png`, `_express575.png`): the flyover and I-575's
bridge stand at about the height of the roads under them (OSM's deck heights from bare-earth ground), so where they cross I-75 the decks and roads meet in trenches and
humps; the ramps up to them are short and stepped (BUG-009 at x 508 z -3173: "this needs to be smoothed. The bridge obvs should be higher"; BUG-010 at x 497
z -3143: "needs to be smoothed").
A. THE JUNCTION AS THE PHOTOS SHOW IT. The I-575 Express flyover (OSM 624557373, deck 451950526, 451950527) and I-575 southbound's bridges (163463460, 536793083)
   lifted to stand clear over every road they cross (at least 6 m under the deck, as Round 78's rail bridges), each a long smooth climb and descent (one vertical
   curve each way that a car at 35 m/s stays down on), their ends met by the roads they continue without a step; the ground under and beside them cut or built up so
   nothing pokes through and nothing is buried; the junction drivable every way at highway speed (the general lanes and the Express lanes, both directions of the
   Express lanes). World changes here (the box above) and at H's Express Lanes crossings; other similar spots listed for Dan. Check: each road through the junction both ways by the human-like
   driver (air, landings, resets), pictures from Dan's spots BUG-009 / BUG-010 before and after, a top-down picture against Dan's photos.
B. THE EXPRESS LANES' GATES ON THE RAMPS FROM THE GENERAL LANES, NEVER ON THE EXPRESS LANES. BUG-005 (x 741 z -1138): "The express lane is two ways. But the gates
   are on exits from the highway so the road going to the express lane is blocked and never the express lane itself otherwise cars wouldn't be able to travel it";
   BUG-007 (x 644 z -2446): "another example. shouldn't be here"; BUG-008 (x 722 z -1988): "express lane gates should be here instead" (where the ramp leaves
   I-575's general lanes). Each gate at the general road's end of its ramp (where it leaves or joins the general lanes or the local road), none where the ramp meets
   the Express lanes. Check: pictures from Dan's three spots; traffic through each open ramp; a closed one blocks entry from the general lanes only.
C. BUG-006 (x 745 z -1721, I-575 just south of Chastain Road): "whats with this?" A line of cars standing nose to tail on the road beside I-575 (the Express lane: a
   traffic queue that never moves) and lumps of ground in the median. Find why the cars stand (a gate, a closed edge, a dead end at the reversal) and fix it; the median
   ground smooth. Check: the spot at 5 PM and 8 AM with Heavy traffic, cars moving; picture.
D. BUG-001 (the Marietta Square race): "Got hit by the train on the marietta square race and it sent me all the way to the opening point of the game" (his report
   was taken at Chastain Glen, x 2947 z 795, where it sent him). A train's hit puts the player back on the race's line short of the crossing, facing the race's way,
   never at the game's start. Check: hit by the train at Mill Street and Whitlock in a race (and in Free Roam: put back on the road there).
E. BUG-002 (x 1632 z -8518, Whitlock Avenue's crossing, lap 3): "what is this?" (the picture: the crossing's gate arm standing straight up in the road beside the
   car, a black box and big grey triangles hanging over the road: the crossing's post, lamp bar and crossbuck broken into shapes that do not look like anything).
   Broken crossing gear falls as pieces that look like what they were (as the fences' and signs' do), or the post and gear are not breakable; the arm never stands in
   the lane. Check: drive through the crossing's post; pictures.
F. BUG-003 (x 2854 z 1132, Chastain Glen Lane, heading 249, the forward lap's shortcut whose line the minimap draws from here into the yards): "the approach to this
   shortcut needs to be fixed. it is nearly impossible to hit it. For one thing if you try to line it up from this angle something sends you flying and you cant line it
   up because of this. Then after you cut through the yard not all the fences are breakable which leads to you getting stuck". Find what throws a car lining up from
   his place (a curb, a lip, a drive's edge) and smooth it; every fence across the shortcut's line breakable (those that stop free ways stay as the shortcut rule
   wants, but not across its own line). Check: from Dan's place and heading, the human-like driver lines it up and takes it, no launch, no stop.
G. BUG-004 (x 1198 z 1670, beside Bells Ferry Road Northwest, heading 349): "buried house" (the picture: a one-storey house sunk to its window sills in the slope).
   Settle it as Round 52's (floor at its ground, the lot cut). Check: picture from Dan's place.
H. THE EXPRESS LANES AS FLYOVERS, WALLED OFF (queued 2026-10-29 01:36, folded in here as it said). Round 80 D1's options for EL13: Dan: "the express
lanes are usually bridges when they cross 75/575. So they should be that way". Where the Express Lanes cross I-75 / I-575, their ramps or the Canton Road
Connector, the lanes go OVER as a bridge (a flyover: a smooth, gradual climb and descent at highway grades, piers, the crossing road left underneath at
its own level), not humps. EL13 first (the connector and the two ramps), then every other Express Lanes crossing the same way, including the 463 m deck
at I-75 / Barrett (1.9 s air one way, a wall at its south end the other) and the three short decks' hops (`round80_check_ddecks.txt`). Check: every
Express Lanes deck driven both ways at top speed: no air over 0.3 s, no step in the road surface, traffic through it; pictures of each flyover from the road below.
Dan, 01:38: "they generally have walls as they are seperated from the main roads": the Express Lanes are walled off from the main lanes along their length (concrete
barrier walls both sides, on the flyovers as bridge parapets), so you get in and out only at their gated ramps (a smashed gate still lets you in, Round 80 E).
   A covers the I-75 / I-575 junction's flyovers; H is every other Express Lanes crossing and the walls along their whole length, done to the same rules.
I. THE STEAM DECK (Dan, 2026-10-29 03:34: "I also want the steam deck stuff done in this round"; was the installer round's C). Measure the game at the Deck's 1280x800 with Dan's PC held to about the Deck's power (frame rate at Marietta Square in a race,
   I-75 with Round 80's traffic, Town Center, the Kennesaw summit). It opens full screen on the Deck. If any spot is under about 40 fps, a
   "Steam Deck" graphics setting (lower draw distance, traffic and people), on its own when the screen is 1280x800. Dan confirms on the Deck itself.
   Also bring over whatever Woodstock Rush does for the Deck (Racer is read only: search its CLAUDE.md / CODEX_RULES.md / archive for "Steam Deck" and
   full screen). Measure after this round's traffic and world changes. Report each spot's frame rate before / after, Deck setting off / on.
   J and K: the installer round, moved in (Dan, 2026-10-29 03:36: "yes include it"; queued 00:43: "I would also need an installation file like I have for
   Woodstock Rush. Which I would want to use the same update system so I only have to install it once"). THIS ROUND SAYS PUBLISH. Do J and K last, from
   the round's final build, so the first release Dan installs carries everything above; the starter also installs on the Steam Deck (I).
J. THE LAUNCHER AND UPDATER. Bring over Woodstock Rush's launcher and update system (`Racer/Launcher/`, `Tools/Prepare-LauncherRelease.py`,
   `Tools/Publish-LauncherRelease.py`, `Tools/Package-LauncherStarter.py`; read only, as `Docs/FROM-WOODSTOCK-RUSH.md` items 1 to 6 say): Chastain Rush's own
   release repo, its own signing identity made once (never reuse, print or expose Woodstock Rush's key), the launcher titled CHASTAIN RUSH with its
   cover art and icon. Same behavior: on opening it checks, installs a newer game and soundtrack by itself, then starts the game; B / Esc plays now;
   rollback to the previous version; saves and personal music kept.
K. THE INSTALL FILE. A starter download like Woodstock Rush's (launcher + game + soundtrack, with the icon and poster for a Steam shortcut). Dan
   installs it once; after that every published round reaches him through the launcher. Publish the first release and prove it end to end: a
   clean install from the starter, then a second small release picked up and installed by that launcher.
Check: item 0 for every item.
For Dan to check: the junction driven every way (general lanes and Express lanes); the gates on the ramps; I-575 by Chastain Road; a train hit in the square race;
the Whitlock crossing broken; the Chastain Glen shortcut from his spot; the house on Bells Ferry Road; every Express Lanes flyover and its walls;
the game on the Steam Deck (full screen, frame rate in the square, on I-75, at Town Center, at the summit).
Install from the starter on the PC and the Deck; the launcher updating itself to the second release.

**Status (Claude, 2026-10-29): built (full Build Map + exe 2026-10-29 06:13), checked in the exe, not yet driven by Dan; published (K).** Evidence `Data/screenshots/round81/`.
Two full Build Maps: the 2nd for A / H's second try after the first's check (decks stepped, walls climbed). Tuning limit reached on A / H.
A PARTLY. `MapBuilder.Round81.cs` R81Flyovers (after SmoothBridgeEnds): each junction flyover and Express Lanes deck laid with 250 m of road either side as one curve, 6 m under the slab
over every road crossed (report `round81_flyovers.txt`). The junction (`round81_check_final.txt` A): the I-575 Express flyover 0.08 / 0.32 s in the air (was a step), I-575 southbound
and northbound and I-75 both ways no air; BUG-010 after: a smooth climb (`81_BUG-010_after`); BUG-009's spot is now under the raised flyover (the drive over it: `81_A_I575Expressflyoverso`).
NOT met: the I-75 Express lanes through the junction 1.3 s in the air and a wall where I-575's connector merges (507, -3350).
B DONE. Gates at the general road's end of each ramp, one per branch (6, `round80_express_decks.txt`); the whole ramp shut with its gate. Dan's BUG-008 spot: gate 4 35 m on; BUG-005 / 007: none.
C DONE. The queue: in a reversal every ramp was shut, exits too (a dead end). Exits now stay open; drivers re-plan when the lanes turn. BUG-006 at 5 PM / 8 AM / 12:30 Heavy: 0 cars standing
(`round81_check_c_lanes.txt`); the lanes at four hours as scheduled. The median at BUG-006 is still lumpy (the lane's approach raised up to 4.3 m by A's pass): `81_BUG-006_after`.
D DONE. Train hit at Whitlock / Mill Street in the square race: put back 19 / 16 m short of the crossing on the line; Free Roam 110 m on along the road (`CarRecovery`).
E DONE. The gear breaks into post, crossbuck, lamp bar, box; the arm lies on the verge (`81_E_after_*`).
F PARTLY. The fences west of the landing (1572/2065, 2065/1452, 1452/1052, the side gate) breakable in forward races. "Sends you flying" NOT reproduced (6 run-ins from Dan's place, traced:
no launch before the kicker); the nearest solid thing on a wide line-up is race barrier B5 21 m from his place.
G DONE. House 1559 settled (Round 52's); picture `81_BUG-004_after`.
H PARTLY. Every Express deck both ways (`D1`): I-75 / Barrett 463 m now 0 s air both ways (was 1.9 s / a wall); EL13 a flyover over the connector and ramps (up to 15.7 m higher) but still
2.1 s air and stuck; the Big Shanty deck 2.6 s against its way; the (-1414, 1617) deck a terrain face at its end against its way. Walls along the lanes, 24.8 km, 1.8 m out, open at ramps.
I DONE (estimate). Settings > Display > Steam Deck (Auto / On / Off; Auto at 1280 x 800 or the Deck's GPU): view, traffic, people 60 %. PC held to 4 cores at 1280 x 800, Normal traffic,
off -> on: square 104 -> 119 fps, I-75 167 -> 175, Town Center 171 -> 192, summit 77 -> 100. Not measured on a Deck (the minimized window does not load the GPU).
J DONE locally. `Launcher/` (Woodstock Rush's, CHASTAIN RUSH, its own key `Builds/PublisherPrivate/`), `Tools/Build-Launcher.ps1`, `LauncherBridge`, the release tools.
K DONE, PUBLISHED to `danemoll-jpg/chastain-rush-releases` (Dan made the repo): `build-8100` (0.81.0 + soundtrack, Latest then), `starter-8100` (`ChastainRush-0.81.0-Full.zip`,
1.37 GB, no soundtrack: game + soundtrack pass 2 GiB; not Latest), `game-8101` (0.81.1, Latest). Proven from GitHub (`launcher_published/`): the downloaded ZIP (SHA-256 matched)
installed 0.81.0, its first start fetched the soundtrack and started the game (handshake PASS); after 0.81.1 was published the same launcher updated itself (saves backed up, 0.81.0 kept).
For Dan to check: install from starter-8100 on the PC and the Deck (README inside); the junction and the Express Lanes (EL13, Big Shanty, the merge wall); the Glen shortcut; BUG-006's median; the Deck.

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
