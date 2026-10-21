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

## Previous delivery 1 (newest): Round 52
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

## Previous delivery 2: Round 51
**Reviewed: Dan drove the Round 51 build 2026-10-19 19:45 (Free Roam); no reports on it (his three notes are Round 52).**
**Round 51, STARTUP POLISH: THE TITLE, THE LOADING SCREEN, HINTS AND THE ICON (Dan, 2026-10-19 18:24; he supplied the assets).** Round 50 is
reviewed (approved). Dan: "Start up polish: here are the assets to make the Startup. We will use this screen for loading screens as well".
No world changes: NO Build Map this round (code and Resources only, then the exe). Short results.
Dan's assets, in `SourceArt/Title/` (committed by Claude; use them unaltered, convert only as the engine needs):
- `ChastainRushCover.png` (1672 x 941): the cover art. Title lettering upper centre, the jumping car centre, the bike left;
  keep its "(c) OpenStreetMap contributors" credit visible (bottom right) on both screens.
- `TitleVoice_MonsterTruckGuy.mp3` (1.07 s, ElevenLabs): the spoken title, "Chastain Rush".
- `ChastainRushTheme.mp3` (34.4 s): the title theme.
A. THE STARTUP TITLE, as Racer's (`Racer/Assets/Scripts/StartupTitle.cs`, its `Resources/Title/`): at launch the cover full screen
   (cover, not stretched; cropped evenly on other aspect ratios), the voice speaks the title once, the theme plays under it
   (opening then loop, as Racer splits its theme, or the whole track looped if it does not split cleanly: Code says which), and a
   "press to continue" prompt (keyboard and controller glyphs) goes on to the title menu. Skipped in check runs as Racer does.
B. THE LOADING SCREEN, as Racer's (`LoadingScreen.cs`): the same cover as the full-screen picture for every scene load (launch, a
   race, Free Roam, back to the menu), the real progress bar, what is loading (race / route inset on the map as Racer does) in a
   dark band along the bottom, clear of the lettering and the car; fades out once the world is built and the first frames rendered.
C. HINTS: one rotating tip on the loading screen, as Racer's, rewritten for THIS game's real controls and features (check each
   binding in the code: reset, camera, map, waypoint, bug capture, free look / Trailer Mode, egg hunt, shortcuts, police...).
   American English.
D. THE GAME ICON (Dan, 18:29: "can we make this the icon (trim closet to the motorcycle if needed)"): the rider on the bike from
   the cover, cropped square by Claude: `SourceArt/Title/GameIcon.png` (512 x 512) and `GameIcon.ico` (16 to 256). Make it the
   exe's icon (Player Settings default icon) and the window / taskbar icon, and the desktop shortcut's if the build makes one.
Check (exe, minimized, muted except one brief listen to confirm the voice and theme play, then off): launch to the title menu
by keyboard and by controller; a race and Free Roam load behind the cover with the bar moving; the tips cycle. Pictures: the title
screen and a loading screen at 1080p and at 4K (the only full-screen use).
For Dan to check: launch the exe (title, voice, theme); a race and Free Roam loading; read the tips; the icon on the exe and taskbar.

**Status (Claude, 2026-10-19): A to D built and checked in the exe by script, not yet seen by Dan.** Evidence `Data/screenshots/round51/`
(`round51_check_pad.txt` 19/19, `round51_check_key.txt` 11/11, `round51_check_4k_pics.txt`, pictures at 720p, 1080p and 4K). No Build Map.
A DONE. Racer's StartupTitle ported (`Assets/Scripts/Title/StartupTitle.cs`): the cover over the whole screen (cropped evenly), the voice once 0.9 s after
   the loading screen goes, then the theme. THEME: the WHOLE TRACK LOOPED, not split: its two choruses are alike but not the same (waveforms match only
   0.4 to 0.5), so no clean loop point; it ends on its own fade and starts again. Both sounds decoded MP3 to WAV in `Assets/Resources/Title/` (no MP3 gap at
   the loop). Racer schedules on the audio clock; under our paused title menu that clock stands still, so here they start by the real clock. One brief
   listen at the listener: voice RMS 0.097, theme 0.027. Space, and A on a pad, each go to the title menu without also pressing it. Radio silent meanwhile.
B DONE. Racer's LoadingScreen ported (`Title/LoadingScreen.cs`). This game is one scene, so it covers the launch, every race start or restart (route
   inset on the map, the countdown held until it goes) and Free Roam (from the title or leaving a race); there is no "back to the menu" load here. Bar:
   race 0.08 to 0.99 over 0.6 s, Free Roam the same; the launch is one long frame of start-up work (the police road net alone 2.4 s), so its bar jumps
   0.30 to 0.92 in three steps. The race HUD and other IMGUI drawing are hidden under it (`TrailerMode.HudHidden`). The OSM credit: the cover's own on
   the title; written on a plate on the loading screen (the band darkens the painted one); written in when a screen's shape crops it.
C DONE. 16 tips for this game's real controls (each binding checked in the code), changing every 5 s (check: 2 changes in 12 s).
D DONE. `GameIcon.png` (copy in `Assets/HandMade/Title/`) is the exe's icon (PlayerBuild), so the window and taskbar too (`exe_icon_32.png`). The build makes no
   desktop shortcut and a .bat cannot carry an icon.
4K: one run full screen at 3840 x 2160 for the pictures only (about 40 s), as the brief allows. Build `Builds/ChastainRush/` 2026-10-19 19:28. Imported from
Racer: StartupTitle, LoadingScreen (rewritten for one scene and this game's route); left behind: the title's fallback listener, Hints.cs (in-play hints).
For Dan to check: listen to the title (does the whole-track loop sound right?); a race and Free Roam loading; the tips; the icon on the exe and taskbar.

## Previous delivery 3: Round 50
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
