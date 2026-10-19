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
**Round 47, REVERSE CHASTAIN LAKES and a driveway fix (Dan, 2026-10-19 01:43; report BUG-001 of session
`2026-10-19_01-29-19-650_77c31b`, read by Claude, after the Round 46 exe: Free Roam at night on the Drifter Twin).** Rounds 43 to 46
are reviewed. One full Build Map at the end (or quick builds if only those steps change); short results.
A. REVERSE CHASTAIN LAKES. Dan: "Can we build the Reverse version of this track. I assume we can just turn it around the other way
   without other changes. I don't expect the current short cuts to work and I want to decide on the shortcuts after I drive it in
   reverse." The same lap driven clockwise, as the reverse Chastain Ridge was added (Round 39): its own lap in the race menu
   (Direction: reverse), checkpoints and arrows the other way, the route's `directions` reverse, `ai_zones_reverse`, its own
   records boards and ghosts, traffic as forward, the same laps. NO shortcuts in reverse: the boat jump's turn-off and the gazebo
   drain are closed in a reverse race (blocked as any road off the lap, the PARTY sign hidden; the drain's cover shut) and leave no
   easier free way; the lake trail, footbridge and dam path driven north to south. Fix only what turning it round breaks (a
   barrier facing the wrong way, a checkpoint on a blind crest, a landing zone in the way); change nothing else. Check: a reverse
   lap each on Street Classic and Drifter Twin; one AI race, all laps, traffic Normal (everyone finishes, 0 missed); the forward
   race unchanged (one lap). Pictures: the start, the footbridge from the north, where each forward shortcut is closed.
B. THE DRIVEWAYS ON CHASTAIN ROAD. BUG-001 (x 1304.53 z 581.52 h 304, Chastain Road Northwest, Free Roam): "These driveways
   shouldn't be here. The driveways would be from inside the neighborhood not on Chastain". The houses along this side of Chastain
   Road at Dan's spot have drives out onto Chastain Road; they belong to the neighborhood behind them, so their drives go to that
   neighborhood's own street, or are dropped where none can reach, and nothing joins Chastain Road there (a lawn and the sidewalk
   instead). Only this row (the houses of that neighborhood that back onto Chastain Road), not a map-wide change. Check: a
   picture from Dan's spot, no drive on Chastain Road; each of those houses' drives meets its own street.
For Dan to check: the reverse Chastain Lakes race (then he picks its shortcuts); the drives at his spot.

## QUEUED rounds
**Round 48, GOOSE EGG HUNT AND EXPLORATION (Dan, 2026-10-18 16:25; start after Round 47).** Dan: "lets go with goose eggs. We can
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

Held by Dan, not to start until he says:
- the CAMPAIGN (2026-10-16 21:34: "I want to hold off on campaign until we get more tracks ... we will have to keep reorganizing
  it otherwise"); the shop, vehicle unlocks and cups / playlists wait with it.
- STARTUP POLISH (2026-10-18): Racer's spoken title, loading screen and hints; Dan: "i Would want 5 but we don't have a cover yet".

## Previous delivery 1 (newest): Round 46
**Reviewed: Dan drove the Round 46 build 2026-10-19 01:29 (Free Roam and the Chastain Lakes race); no reports on this round (his one note is Round 47).**
**Round 46, CHASTAIN LAKES SHORTCUTS REDONE (Dan, 2026-10-18 22:19, after driving Round 44; start after Round 45).** Round 44
is otherwise accepted (Dan: "The only other issue I found is on the other shortcut"). The two new standing rules at the top of
this file apply (shortcuts must be a risk; pipes through water are aquarium pipes). One full Build Map at the end.
A. THE WEST LAKE JUMP, REDONE. Dan: "I would have asked for this Shortcut to be redone anyway. It fails the rule that it should be
   challenging. It is just a simple ramp that jumps a narrow part of the lake. ... My first option would be to make a bigger ramp
   and point further down the track. You will be forced to clear a bigger part of the lake so if you don't hit it correctly you
   fall into the lake. Also can we come up with something other than another dirt pile? I am thinking you come upon a boat off
   the dock that has a large slide on it that can be used for a ramp."
   Remove Round 44's dredging-spoil pile and its narrow crossing. Through the back yards (the 142 / 143 gap or another, Code's
   choice) to a DOCK on West Chastain Lake's north shore, and tied up off its end a party BOAT (a pontoon boat) with a big water
   slide on its top deck: the slide is the ramp (you drive along the dock, onto the boat and up the slide). Its real size must
   fit a boat-top slide (CLAUDE.md lesson on ramp sizes); the boat may be big. A REAL physics jump off the slide's lip (standing
   rule), aimed further down the course than Round 44's (Dan's red line and beyond: over the wide part of the lake, about 80 m
   of water on his line), landing on Lakeside Drive or its shoulder further along the lap, so the climb up Round 44's 2.7 m bank
   (its NOT MET item) is gone. Hit it right (full speed, straight up the slide) and you clear the water; come in slow or crooked
   and you fall into the lake (slowed, the shallow / deep-water rules, a reset puts you on the race's line, never trapped). It
   must still save time when made. The boat and dock can stay in Free Roam as world (the slide drivable there too). A hint at the
   turn-off as the other shortcuts. Tuning limit: if the full-speed clearance or the time saved is not met after two tries, stop
   and report the numbers (distance needed vs reached per vehicle) for Dan.
B. THE DRAIN'S TUNNEL AS AN AQUARIUM PIPE. Dan: "The part that goes under the lake should be an aquarium pipe." Round 44's LD and
   B28 under Chastain Lakes become glass aquarium runs wherever they are under the water (the network's glass mains' look and
   life: fish, weeds, light shafts, the lake surface above), concrete only where they run under land. Because of the new rule:
   list any other pipe in the network that passes under water without glass, convert it the same way, and say which.
Check: the west jump on all 11 offered vehicles at full speed straight up the slide (clears, lands upright, height and air) and
at 80 % speed or 10 degrees off line (falls in the lake); time saved (Street Classic, Drifter Twin); the AI takes it and
sometimes falls in (human mistakes); a drive through the drain's aquarium run. Pictures: the dock and boat from the turn-off, the
slide, the flight, the landing, the aquarium run from inside.
For Dan to check: the boat jump (a clean run and a miss); the aquarium under the lake.

**Status (Claude, 2026-10-19): A and B built and checked in the exe by script (final build: 11 of 12), not yet driven by Dan.** Evidence `Data/screenshots/round46/`.
A DONE. The BOAT JUMP: off North Lakeside Drive by Bankside Point (a PARTY sign with balloons, worn tracks), down between houses 146 and 147 (no house moved)
   to a dock, a 20 m two-deck pontoon party boat, up its water slide (solid chute, low walls, 4.5 m over the water, 23 degrees at the top); a real jump down
   the lake's south arm, 77 m of water, onto Lakeside Drive at Lakeside Point (its barrier 45 m down it for long landings). Spoil pile and narrow crossing gone.
   Run-up measured first: 34 to 38 m/s at the shore flat out. Full speed: 11 of 11 clear and land upright, 90 to 111 m out (1.0 to 1.6 s more air than Round 44,
   8 to 10 m over the top). 80 %: 11 of 11 come down in the lake, 61 to 74 m out (the arm's shallow end: they wade out up the bank). 10 degrees off: 9 hit the
   chute's walls and drop in 13 to 54 m out; the Street Classic and Pebble Coupe +10 never reached the slide in the check's run (FAIL as written). Saves 5.0 s
   (Street Classic) and 4.0 s (Drifter Twin). AI race x2, traffic Normal: all 6 finish, 0 missed; the AI took the boat jump 7 times (laps with it 95 to 138 s).
B DONE. Glass where the drain's tunnel runs under East Chastain Lake: B28's whole 140 m (its dive under M2a included; LD, the drop's room, stays concrete).
   By the new rule also: B12 under the big lake (44 m), S2 under the Enclave pond (13 m), M3c under pond W14 (12 m). No pipe's floor moved. Not converted:
   the 11 pipes that pass under a creek (M5a, M5b, B9a, B10, M3b, M3c, B11, B14, M4a, B27, M6; a creek has no bed to set glass in). Check: through the drain, 5 s in glass.
Found and fixed in the exe (why FOUR full builds, not one): cars stopped dead on the boat (a thin open slide board under 3.5 g; a deck under the slide; upright
   faces at flush joints; the dock sharing the deck's collider), then every AI car fell in (`ai_speed` capped its run at 18 m/s). Tuning: none needed.
Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), full build 2026-10-19 00:37 (BUILD_INFO.txt).
For Dan to check: the boat jump flat out, slow and crooked (is wading out of the arm's shallow end enough of a cost?); B28's glass from the drain; the creek list.

## Previous delivery 2: Round 45
**Reviewed: Dan drove the Round 46 build 2026-10-19 01:29 (Free Roam and the Chastain Lakes race); no reports on this round (his one note is Round 47).**
**Round 45, THE POLICE VOICES (Dan, 2026-10-18 21:53; small, can start now; Rounds 43 and 44 stay as delivered until Dan drives
them).** Dan made two ElevenLabs voices for this game's police radio (Round 40 asked for his own voice, not Woodstock Rush's):
"I created two voices for the police: 1) For the cop cars, voice id - g8CcnC07pMG4qiREyAqr 2) Helicopter pilot, voice id -
TTipgIQvJszYvCktGfAa". No clips exist yet (`SourceArt/Audio/PoliceRadio/` is empty: the calls show as text only).
A1. Who speaks what: every line in `Tools/Audio/police-radio-lines.csv` gets a speaker, "cop" (the cars and dispatch) or "pilot"
   (the helicopter: R07, R08, U7 "Air one," and any other line only the helicopter says; R06 "Air support inbound" is dispatch,
   so cop). A call the game stitches from pieces (unit + phrase + street + ending) for Air one is spoken by the pilot whole: the
   pieces such a call can use get a pilot take too (Code names them, e.g. `<id>_air`), and PoliceRadio picks the pilot set for
   Air one's calls and the cop set for every other unit's. No mixing voices inside one call.
A2. Police Radio Studio (`Tools/Audio/police_radio_studio.py`): two voice boxes, Cop and Helicopter pilot, filled in with Dan's two
   IDs above (voice IDs are not secrets; the API key still never goes in the project); each line made with its speaker's voice;
   "make all missing" makes every clip not yet made, both voices. Keep everything else as it is.
A3. Make the clips: if Dan's API key is on this PC (ELEVENLABS_API_KEY or the studio's settings.json), run the studio's generation
   from the command line for every missing line (do not print or copy the key); otherwise stop after A2 and tell Dan to press
   "make all missing". The pilot's lines through the same radio effect plus a low rotor thump under them (simple; Code's choice).
Check: the clip count per voice against the CSV; in the exe, a Getaway to heat 4 on a test setting: one cop call and one Air one
call heard (sound briefly on, then off) and logged with which voice set played. Exe rebuilt with `-playerBuildKeepScene` (no map
build). For Dan to check: the radio in a Getaway, a cop call and the helicopter's.

**Status (Claude, 2026-10-18): built and checked in the exe by script (3 of 3), not yet heard by Dan.** Evidence `Data/screenshots/round45/round45_check.txt`.
A1 DONE. The csv's new `speaker` column: R07, R08, U7 "pilot", the other 187 "cop" (a "cop+pilot" piece would get a pilot take `<id>_air`). Air one's calls
   today are whole pilot lines (U7+R07, R08), so no piece needs an `_air` take yet. `PoliceRadio`: a call with a pilot line in it is played from the
   pilot's set only (a missing pilot take: text only, never the cop's voice in it); an older scene's csv (no column): "Air one..." lines are the pilot's.
A2 DONE. The studio: Cop and Helicopter pilot voice boxes (Dan's IDs filled in), each line made with its speaker's voice; `--make-missing` from the command line.
A3 DONE. 190 clips made with Dan's key from the environment (not printed): 187 cop, 3 pilot, 0 failed (`SourceArt/Audio/PoliceRadio/`, committed; also copied
   to the drop-in folder by the studio). The pilot's calls get a rotor thump (about 4.6 a second) under the radio effect, in the game (the clips stay clean).
Check: a Getaway on the Needle 600 to heat 5: cop calls in the cop voice (R01, units + streets), the helicopter's R08 in the pilot's; then R14 and U7+R07 heard.
Build: code only (`-playerBuildKeepScene`), 2026-10-18 22:36; the round's playable build is Round 46's full build. For Dan to check: a Getaway, a cop call
   and the helicopter's (does the rotor thump sit right under the voice?).

## Previous delivery 3: Round 44
**Reviewed: Dan drove the Round 46 build 2026-10-19 01:29 (Free Roam and the Chastain Lakes race); no reports on this round (his one note is Round 47).**
**Round 44, THE CHASTAIN LAKES RACE (Dan, 2026-10-18 18:10; START NOW, before Round 43 is driven: Dan is away and asked for
the next track to be made meanwhile; Round 43 stays as delivered above until he drives it).** Dan: "Can we go ahead and create the
next track? I want to use the Chastain Lakes subdivision ... On it I have the forward trail marked out, please be aware there is a
trail that runs behind the pool across the bridge and by the lake going all the way to bells ferry this race will utilize. ... I
have also added two shortcuts. One is through the gazebo, I want it to jump and land in a hole in the lake that will tunnel through
and pop out of manhole in the road, [the other one] that I marked revolves around going between a couple of houses and hitting a
ramp to take jump drivers over the lake. Most of the track already exists in the game so I am hoping that it will be good."
Dan's map and 7 photos (taken 2026-10-17): `local/reference_photos/chastain_lakes/r44_*` (git-ignored, never commit; look, never
trace). The map is Google satellite with his yellow route, yellow arrows beside it for the direction (Dan, 18:15: "arrows for the
direction of the track") and two red shortcuts, around West Chastain Lake and Chastain Lakes / East Chastain Lake. Claude's reading:
- THE LAP, COUNTERCLOCKWISE (Dan's arrows, and the only direction both shortcuts work): North Lakeside Drive westbound across the top, round the
  west loop on Lakeside Drive, Lakeside Drive east and south past Laurel Green Court along the bottom, on to the Chastain Lakes
  amenity area (Round 32, `MapBuilder.ChastainLakes.cs`), then THE LAKE TRAIL north to Bells Ferry Road and back onto North
  Lakeside Drive at its Bells Ferry end. Roads off the lap are blocked as on every course (the short yellow strokes off the
  route on the map are Dan's direction arrows, not roads).
- THE LAKE TRAIL (photos 2 to 7; Round 32 built part of it from NAIP): from the lot, the concrete walk with timber edging down past
  the pool's granite retaining wall, iron fence on brick pillars and the white bathhouse (photo 2); a gravel path with tan brick
  edging along the shore (photo 3) to the brown wooden footbridge over the concrete spillway with granite wingwalls (photos 6, 7;
  in real life closed with caution tape and 2x4s across it: keep that, breakable, as a fun touch); then north along the gravel
  path on the dam's grassy crest, the lake on the left going north, the lawn falling right to the outflow stream and the double
  box culvert under Bells Ferry Road (photos 4, 5), to North Lakeside Drive. Wide enough for every offered vehicle with room to
  pass (Noonday Creek's lesson: wider than real), keeping its look (gravel, edging, the bridge's color and rails); the bridge
  widened to fit. BF and the stream untouched as Round 32 required, unless the trail cannot pass otherwise (say so).
- SHORTCUT EAST, THE GAZEBO (photo 1: the white gazebo, square hipped gray-green roof, on a white-railed pier off the lawn, riprap
  shore, the porch swing and a bench): coming along Lakeside Drive toward the lot, off onto the walk to the pier, along the pier,
  through the gazebo (its far railing breaks) and off its end, down into a HOLE IN THE LAKE: a round bell-mouth overflow drain in
  the lake (a "morning glory" spillway, a real lake fixture), water pouring over its lip. Into it, a tunnel under the lake north,
  up and out through a manhole that pops in North Lakeside Drive near the Bestower Foundation (Dan's red line), back on the
  course facing west. Skips the lake trail and the Bells Ferry corner; forgives the checkpoints there. Use what the pipe network
  already has (drops, pipes, PipeStream water, pop-out manhole covers, Round 40's plume if it helps the exit); it may join the
  network (the M2 Lakes main and BF are close) but in this race the way through is one clear run (other ways gated as the
  Underground race does). Off the end of the gazebo is a real drop (physics); landing in the drain mouth is the skill (miss it and
  you are in the lake: slowed, the shallow-water rules); the drain's mouth sized so a line straight off the pier at speed makes it.
  A clear hint at the turn-off (CLAUDE.md shortcut hints). Must save time at race speed, and carry some risk.
- SHORTCUT WEST, THE LAKE JUMP: on North Lakeside Drive westbound near the top-left (Dan's red line), off between two houses (the
  gap made to work as Round 40 did with 1245, a house moved slightly or altered only if needed, and said), through the back yards
  down to a ramp on West Chastain Lake's north shore, a jump over the lake's arm, landing on the south / west shore by Lakeside
  Drive, skipping the west loop; forgives its checkpoints. A real physics jump (standing rule): the ramp dressed as something that
  could be there at its real size (CLAUDE.md lesson), using the high bank's height; the landing sized to the real flight. First
  measure the gap on Dan's line: if no offered vehicle can clear it with a real jump there, use the narrowest crossing near his
  line that works and say how far it moved; if none works, build the turn-off and ramp to the shore and stop there, reporting the
  gap and the speed it would need (Dan decides).
- THE RACE: start / finish on a straight of Code's choice not near a shortcut; checkpoints as the other courses; laps so the race
  takes about as long as a Chastain Ridge race; forward only for now (reverse later if Dan wants it). Everything the other courses
  have: the route's directions, AI (3 rivals, `ai_zones`, ShortcutStrategy for both shortcuts, human mistakes), traffic as forward
  Ridge, records boards, ghosts (Round 43), the minimap and map overlay, the Race Setup row, the radio. Name: "Chastain Lakes".
Check: a lap by each shortcut and one by the trail (Street Classic and Drifter Twin), times compared; the west jump's flight on all
offered vehicles (height, air, landing upright); the gazebo drop into the drain from a straight line at speed, and a miss; one AI
race of the full laps with traffic Normal (everyone finishes). Pictures: the trail from photo 3's and photo 4's spots, the
footbridge, the gazebo, the drain, the manhole popping, the west ramp and landing. One full Build Map at the end.
For Dan to check: drive the Chastain Lakes race, both shortcuts and the lake trail.

**Status (Claude, 2026-10-18): built and checked in the exe by script (final build: 6 of 8 checks pass; both fails are the NOT MET item below), not yet driven by Dan.** Evidence `Data/screenshots/round44/`.
- The race: 2770 m lap, 2 laps, forward only, open roads, 9 checkpoints, 16 barriers (`Tools/mapgen/make_lakes_route.py`). AI race, 2 laps, traffic Normal: all 3 finish
  (197 to 234 s; a Chastain Ridge race is about 230 to 240 s), 0 missed, both shortcuts taken.
- The lake trail: 7 m, concrete with timber edging to the footbridge, gravel with tan brick edging along the dam's crest; the footbridge 6.8 m wide, moved 9 m east
  at its south end so the trail meets it square (it still spans the outflow below the spillway; BF and the stream untouched), its deck sloping bank to bank, tape and
  2x4s breakable. Trail by Street Classic / Drifter Twin, lot to North Lakeside Drive past the manhole: 26.3 / 26.5 s.
- E LAKE DRAIN: a hard left (12 m) off the lot's drive onto the pier (4.4 m now), through the gazebo (square as photo 1, far rail breaks), a drain 9 m past it (LakeDrain:
  a scripted drop, not a ramp), LD and B28 under the lake (pipe_network.py), out of MH14 facing west. Both vehicles: down the drain, the cover pops, on; saves 9.2 / 10.7 s.
  A miss (into the lake beside it): slowed, not taken, a reset puts it on the race's line.
- W LAKE JUMP: through the 5.3 m gap between 142 and 143 (no house moved), a 3 m dredging-spoil pile, a real jump over 33 m of water, moved about 20 m west of Dan's line
  (the narrowest crossing; on his line it is about 80 m). All 11 vehicles clear the water and land upright (39 to 55 m, 1.8 to 2.2 s air at 22 to 26 m/s off the pile:
  they do not reach the AI's 28 down the yards). Saves 2.4 / 2.0 s (Street Classic / Drifter Twin; the 2nd build's run, `round44_check_build2.txt`).
- NOT MET: the way back onto Lakeside Drive climbs a 2.7 m bank onto the road (the lake arm's tip and its bed are just east). The AI drives it, but the check's own
  scripted driver ran on across the road and stopped south of it on 6 of 11 vehicles; tried twice, stopped there (tuning limit).
- Process: THREE full builds, not one (the 1st build's checks found the drain sending cars the wrong way, a 50-degree bend onto the footbridge, a 3 m gazebo opening;
  the 2nd, a 2 m step onto the footbridge); code-only exe rebuilds otherwise. Build: `Builds/ChastainRush/` (`Play Chastain Rush.bat`), 2026-10-18 20:16.
For Dan to check (the race above): the lake trail and the footbridge; the drain's drop and the manhole; the lake jump and the bank back onto Lakeside Drive (say if
  it should come out elsewhere); whether the drain saves too much (about 9 s).

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
