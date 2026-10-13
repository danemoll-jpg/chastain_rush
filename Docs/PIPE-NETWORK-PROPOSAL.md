# The underground pipe network: proposal (Round 23, Part B)

Made by `Tools/mapgen/pipe_network_proposal.py` (re-run after mapgen). Nothing in this document is built except the main entrance: BF, two pipes from the end of the creek channel west of Bells Ferry Road down into the first junction chamber, J1, under the far side of the road, whose four onward stubs end at closed grilles (no way out on the far side of the road). The network below starts at those grilles. Dan picks and changes; then it is built stage by stage.

Picture: `local/round23/network_overhead.png` (git-ignored). In the game: debug menu (F6 or `\`), **Proposal markers: Shown**; **Go to** puts you at a stop (free roam). The map (M or View) draws every line and its name while the markers exist.

## What it is

- **Mains**: 7 lines, 6044 m, 5 m round (every vehicle fits), a color stripe each, painted signs at their junctions.
- **Maze branches**: 27 lines, 5763 m, unmarked; most 3.5 m round (motorcycles, the ATV and the mower only), a few 5 m.
- **The hub**: a chasm 110 x 70 m under the woods north of the creek valley at x 1900, z 1145; its floor at 252 m (about 32 m under the ground), a mid ledge at 262 m and a top ledge at 272 m round the walls, joined by ramps; every main pours its water in as a waterfall off its ledge into a pool on the floor; lit from below and by shafts of daylight through grates.
- **Junctions**: 9 chambers. **Exits**: 12 secret ways out, 2 outfall launches over water, **manholes**: 13 pop-outs in residential streets.
- Glass (aquarium) stretches wherever a main passes under a pond or lake (as P1), drop shafts, flooded stretches that slow you, the map showing the pipes when you are underground.

## The hub

```
            north wall: B1 in at the top ledge (north maze), its waterfall
   +------------------------------------------------------------+
   | top ledge 272 ........................................     |
   |   mid ledge 262 ...............................            |
 M1| ==> (west wall, mid ledge)        [ POOL ]      floor 252 M3 ==> east (floor)
   |   ramp mid -> floor (north-west)    S/F  <- race           |
   |   ramp top -> floor (east wall)                            |
   | top ledge 272 ..... S1 lip (waterfall leap) ...            |
   +--------------------^---------------------------------------+
            south wall: M5 in at the top ledge (race), B9 out at the mid ledge
```
Waterfalls: M1 (west), M5 (south), B1 (north) pour off their ledges; the pool on the floor takes them; M3 carries it on east to Noonday Creek. Its own space to drive round: the ledges, the ramps, the floor, the pool (shallow: it slows, never traps).

## Mains

| Line | Stage | Length | Passes under | Floor | Deepest under ground | Under water (glass) | Open cut at its ends | |
|---|---|---|---|---|---|---|---|---|
| **BF Entrance** (BUILT) | built | 54 m | roads: Bells Ferry Road Northwest | from the creek bed (283.5 m) down at up to 18 % to J1's floor (277.5 m) | 12 m | 0 m | 12 m (the mouth's cut) | BUILT: two 5 m pipes from the end of the creek channel (Dan's BUG-002 spot, straight ahead) down at up to 18 % under Bells Ferry Road into J1 |
| **M1 Trunk** (red stripe) | 1 | 452 m | water: pond W1 | 262 to 278 m, grade up to 5 % | 23 m | 78 m | 0 m | J1's south-east grille (built) to the hub along the creek valley; under the Enclave's north pond (W1) in glass (J2) |
| **M5 Enclave Main** (orange stripe) | 1 | 691 m | roads: Estates Landing Drive; 6 houses; water: pond W4; streams: a stream | 270 to 279 m, grade up to 10 % | 25 m | 21 m | 0 m | J1's other south-east grille (built) south along the east side of Bells Ferry Road, under the Enclave pond (W4) in glass, J3, into the hub's top ledge |
| **M3 Noonday Main** (green stripe) | 2 | 753 m | water: pond W15; streams: Noonday Creek, a stream | 254 to 276 m, grade up to 10 % | 29 m | 132 m | 64 m | The hub's floor east under the woods to J4, under Noonday Creek's bed (glass), past P1 (a grate in P1's floor drops into it), J5 in a glass dome under the big lake, out over the water |
| **M4 Glen Main** (yellow stripe) | 3 | 1390 m | roads: Chasefield Way, Chastain Trail, Chastain Place, Courtfield Trace, Chastain Glen Lane; 8 houses; water: pond W8; streams: Noonday Creek, a stream | 266 to 294 m, grade up to 10 % | 24 m | 114 m | 0 m | J4 south down the creek's west side to J6 under the New Chastain Road bridges, east to J7, under the Chastain Glen pond (W8) in glass, on to J8 behind the shopping center |
| **M2 Lakes Main** (blue stripe) | 4 | 1391 m | roads: Laurel Green Court Northwest, Lakeside Drive Northwest, North Lakeside Drive Northwest, Bells Ferry Road Northwest, Phillip Landrum Memorial Highway, Northwest Corridor Express Lanes; 4 houses; water: West Chastain Lake, East Chastain Lake | 278 to 315 m, grade up to 10 % | 26 m | 538 m | 26 m | J1's north-west grille (built) back west under Bells Ferry Road (north of the entrance) to East Chastain Lake, along its bottom in glass, J9, under West Chastain Lake in glass, under the interstate to the far-west pond (W5) |
| **M6 Nelson Main** (purple stripe) | 4 | 1310 m | roads: Lakeside Drive Northwest, Chastain Road Northwest, Phillip Landrum Memorial Highway, Northwest Corridor Express Lanes; 5 houses; streams: Overflow Channel | 290 to 300 m, grade up to 10 % | 32 m | 0 m | 39 m | J9 south under Lakeside Drive and Chastain Road, down the I-575 side to Nelson Lake (south-west corner), under the interstate |

Depths: the floor is fitted under the ground with at least 1.5 m over the crown, given levels kept at the junctions, the hub and the glass stretches; grades are at most 10 % on the mains and 16 % in the maze. 'Open cut' is where it comes up to a mouth or an exit between walls, as P1's cuts. 'Passes under' is what is on the ground over it (nothing up there changes).

## Maze branches

| Branch | Stage | Size | Length | Passes under | Deepest | Grade | Open cut at its ends | |
|---|---|---|---|---|---|---|---|---|
| B1 | 1 | 5 m | 382 m | water: pond W1 | 16 m | up to 1 % | 0 m | J2 north and east into the hub's top ledge (north side) |
| B2 | 1 | 5 m | 118 m | 2 houses | 14 m | up to 13 % | 0 m | J1's far grille (built) north to the Parkwood Drive west manhole |
| B2a | 1 | 3.5 m (bikes, ATV, mower) | 63 m | woods and yards only | 6 m | up to 16 % | 0 m | a link from B2 to B1 (the north maze) |
| B3 | 1 | 3.5 m (bikes, ATV, mower) | 76 m | roads: Parkwood Drive; 1 house | 8 m | up to 2 % | 0 m | to the Parkwood Drive east manhole |
| B4 | 1 | 3.5 m (bikes, ATV, mower) | 132 m | woods and yards only | 6 m | up to 9 % | 0 m | a link up from M1 to B1 (a drop shaft on it) |
| B5 | 1 | 3.5 m (bikes, ATV, mower) | 45 m | woods and yards only | 6 m | up to 15 % | 0 m | dead end (the maze) |
| B6 | 1 | 3.5 m (bikes, ATV, mower) | 107 m | roads: Bloombridge Way; 1 house | 16 m | up to 15 % | 0 m | J3 to the Bloombridge Way manhole |
| B7 | 1 | 3.5 m (bikes, ATV, mower) | 207 m | roads: Bloombridge Way, Rotherhithe Landing; 2 houses | 21 m | up to 13 % | 0 m | J3 to the Rotherhithe Landing manhole |
| B8 | 1 | 3.5 m (bikes, ATV, mower) | 357 m | roads: Bloombridge Way, Bells Ferry Road Northwest; 4 houses | 11 m | up to 16 % | 6 m | on under Bells Ferry Road to the pond by Chastain Road (W20) |
| B9 | 1 | 3.5 m (bikes, ATV, mower) | 191 m | 1 house; streams: a stream | 19 m | up to 10 % | 0 m | the hub's mid ledge south-east to the Bloombridge Way east manhole |
| B10 | 1 | 3.5 m (bikes, ATV, mower) | 90 m | streams: a stream | 6 m | up to 1 % | 0 m | dead end (the maze) |
| B11 | 2 | 3.5 m (bikes, ATV, mower) | 121 m | streams: a stream | 11 m | up to 16 % | 28 m | J4 up into the Noonday Creek woods trail |
| B12 | 2 | 3.5 m (bikes, ATV, mower) | 136 m | water: pond W15 | 7 m | up to 16 % | 27 m | J5 to the bank of the pond east of the big lake (W12) |
| B13 | 2 | 3.5 m (bikes, ATV, mower) | 231 m | 1 house; water: pond W15 | 10 m | up to 4 % | 0 m | J5 to the Guidhall Trail manhole |
| B14 | 2 | 3.5 m (bikes, ATV, mower) | 459 m | roads: Guidhall Trail; 1 house; streams: Noonday Creek | 13 m | up to 16 % | 0 m | J4 under the creek and the trail to the Westminister Way manhole |
| B15 | 2 | 3.5 m (bikes, ATV, mower) | 124 m | water: pond W15; streams: a stream | 5 m | up to 16 % | 27 m | M3's end to the pond at the top of the big lake (W7) |
| B16 | 3 | 3.5 m (bikes, ATV, mower) | 341 m | roads: Paxton Court Northeast, New Chastain Road; 1 house | 11 m | up to 16 % | 0 m | J6 under New Chastain Road to the Paxton Court manhole |
| B17 | 3 | 3.5 m (bikes, ATV, mower) | 160 m | roads: Grasmere Court Northeast; 2 houses | 13 m | up to 4 % | 0 m | J7 to the Grasmere Court manhole |
| B18 | 3 | 5 m | 58 m | 1 house; water: pond W8 | 7 m | up to 16 % | 46 m | an outfall launch out over the Chastain Glen pond |
| B19 | 3 | 3.5 m (bikes, ATV, mower) | 585 m | roads: Joel Drive, Embry Lane Northeast, Weybourne Court Northeast, New Chastain Road; 5 houses | 19 m | up to 16 % | 20 m | J7 south under New Chastain Road, the Weybourne Court manhole, on to the ponds south of the Commons (W11) |
| B26 | 3 | 5 m | 45 m | woods and yards only | 10 m | up to 64 % | 20 m | J6 out into the creek bank under the New Chastain Road bridges |
| B20 | 3 | 3.5 m (bikes, ATV, mower) | 225 m | roads: Chastain Glen Lane, Chasefield Court; 3 houses | 15 m | up to 2 % | 0 m | J8 to the Chasefield Way manhole |
| B21 | 3 | 5 m | 111 m | roads: The Willows; 2 houses | 9 m | up to 102 % | 87 m | J8 up a ramp to the storm drain behind the shopping center |
| B22 | 3 | 3.5 m (bikes, ATV, mower) | 675 m | roads: Centerview Drive Northeast, The Willows, Canton Road Northeast; 9 houses | 39 m | up to 16 % | 30 m | J8 under Canton Road to Little Noonday Creek |
| B23 | 4 | 3.5 m (bikes, ATV, mower) | 106 m | roads: Laurel Green Court Northwest; 2 houses | 19 m | up to 14 % | 0 m | J9 to the Laurel Green Court manhole |
| B24 | 4 | 3.5 m (bikes, ATV, mower) | 464 m | roads: Lakeside Drive Northwest, Chastain Lakes Drive Northwest; 3 houses | 18 m | up to 16 % | 53 m | J9 to the Chastain Lakes Drive manhole and the pond by Chastain Road (W21) |
| B25 | 4 | 3.5 m (bikes, ATV, mower) | 156 m | 2 houses | 10 m | up to 16 % | 24 m | M2 to the stream bank north of West Chastain Lake |

## Junctions, exits, manholes and the pieces along the way

| Marker | Kind | Where (x, z) | Stage | |
|---|---|---|---|---|
| BF MAIN ENTRANCE | mouth | 1416, 1183 | 1 | Built: twin pipes down |
| J1 FIRST CHAMBER | junction | 1453, 1226 | 1 | Built: 4 pipes, grilles |
| J2 POND JCT | junction | 1572, 1128 | 1 | Glass roof under pond |
| J3 ENCLAVE JCT | junction | 1785, 1000 | 1 | Chamber, 15 m down |
| J4 NOONDAY JCT | junction | 2280, 1215 | 2 | Chamber, 11 m down |
| J5 LAKE JCT | junction | 2352, 1470 | 2 | Glass dome under lake |
| J6 BRIDGE JCT | junction | 2398, 826 | 3 | Under the bridges |
| J7 GLEN JCT | junction | 2722, 975 | 3 | Chamber, 11 m down |
| J8 SHOPS JCT | junction | 3170, 1185 | 3 | Chamber, 9 m down |
| J9 LAKES JCT | junction | 872, 1240 | 4 | Between the lakes |
| THE HUB | hub | 1900, 1145 | 1 | Chasm 110 x 70 m, 33 m deep |
| GRILLE M5 (CLOSED) | grille | 1467, 1206 | 1 | Built: stub out of J1 |
| GRILLE M1 (CLOSED) | grille | 1474, 1214 | 1 | Built: stub out of J1 |
| GRILLE NORTH (CLOSED) | grille | 1466, 1242 | 1 | Built: stub out of J1 |
| GRILLE M2 (CLOSED) | grille | 1434, 1242 | 4 | Built: stub out of J1 |
| EXIT W20 POND | exit | 1376, 812 | 1 | Bank, by Chastain Rd |
| EXIT CREEK TRAIL | exit | 2342, 1112 | 2 | Up into the woods trail |
| EXIT W12 POND | exit | 2478, 1420 | 2 | Bank, east of big lake |
| EXIT W7 POND | exit | 2420, 1668 | 2 | Bank, top of big lake |
| EXIT UNDER BRIDGES | exit | 2425, 790 | 3 | Creek bank, New Chastain |
| EXIT W11 POND | exit | 2668, 410 | 3 | Bank, south of Commons |
| EXIT BEHIND SHOPS | exit | 3250, 1262 | 3 | Storm drain, north side |
| EXIT LITTLE NOONDAY | exit | 3752, 1468 | 3 | Creek bank, past Canton |
| EXIT W21 POND | exit | 1098, 850 | 4 | Bank, by Chastain Rd |
| EXIT LAKE STREAM | exit | 470, 1425 | 4 | Stream bank, north |
| EXIT FAR WEST POND | exit | 178, 1488 | 4 | Bank, past I-575 (W5) |
| EXIT NELSON LAKE | exit | 262, 110 | 4 | Bank, past I-575 |
| LAUNCH GLEN POND | outfall | 2760, 828 | 3 | Headwall, over the water |
| LAUNCH BIG LAKE | outfall | 2386, 1602 | 2 | Headwall, over the water |
| MH1 MANHOLE | manhole | 1520, 1277 | 1 | Parkwood Drive (pops out in Parkwood Drive) |
| MH2 MANHOLE | manhole | 1760, 1277 | 1 | Parkwood Drive (pops out in Parkwood Drive) |
| MH3 MANHOLE | manhole | 1879, 949 | 1 | Bloombridge Way (pops out in Bloombridge Way) |
| MH4 MANHOLE | manhole | 1708, 812 | 1 | Rotherhithe Landing (pops out in Rotherhithe Landing) |
| MH5 MANHOLE | manhole | 2012, 933 | 1 | Bloombridge Way (pops out in Bloombridge Way) |
| MH6 MANHOLE | manhole | 2550, 1584 | 2 | Guidhall Trail (pops out in Guidhall Trail) |
| MH7 MANHOLE | manhole | 2724, 1197 | 2 | Westminister Way (pops out in Westminister Way) |
| MH8 MANHOLE | manhole | 2127, 619 | 3 | Paxton Court (pops out in Paxton Court Northeast) |
| MH9 MANHOLE | manhole | 2730, 816 | 3 | Grasmere Court (pops out in Grasmere Court Northeast) |
| MH10 MANHOLE | manhole | 2691, 613 | 3 | Weybourne Court (pops out in Weybourne Court Northeast) |
| MH11 MANHOLE | manhole | 2965, 1093 | 3 | Chasefield Way (pops out in Chasefield Way) |
| MH12 MANHOLE | manhole | 801, 1162 | 4 | Laurel Green Court (pops out in Laurel Green Court Northwest) |
| MH13 MANHOLE | manhole | 977, 962 | 4 | Chastain Lakes Drive (pops out in Chastain Lakes Drive Northwest) |
| GLASS UNDER W1 POND | aquarium | 1572, 1128 | 1 | M1, J2's glass roof |
| GLASS UNDER W4 POND | aquarium | 1636, 945 | 1 | M5, along the pond |
| GLASS UNDER CREEK | aquarium | 2330, 1250 | 2 | M3 under Noonday bed |
| GLASS UNDER BIG LAKE | aquarium | 2352, 1500 | 2 | M3, J5's dome |
| GLASS UNDER GLEN POND | aquarium | 2825, 888 | 3 | M4 across the pond |
| GLASS UNDER E CHASTAIN | aquarium | 1160, 1185 | 4 | M2 along the lake bed |
| GLASS UNDER W CHASTAIN | aquarium | 680, 1362 | 4 | M2 along the lake bed |
| SHAFT P1 GRATE | shaft | 2338, 1300 | 2 | P1's floor drops to M3 |
| SHAFT B4 | shaft | 1652, 1150 | 1 | 8 m drop, B1 to M1 |
| SHAFT J7 | shaft | 2700, 940 | 3 | 10 m drop to M4 |
| FLOODED B8 | flooded | 1470, 846 | 1 | Slow, under Bells Ferry |
| FLOODED M4 | flooded | 2318, 980 | 3 | Slow, by the creek |
| FLOODED M2 | flooded | 1300, 1162 | 4 | Slow, low under the lake |
| SIGNS AT J1 | sign | 1444, 1216 | 1 | M1 TRUNK / M5 / M2 |
| SIGNS AT J3 | sign | 1795, 1010 | 1 | M5 ENCLAVE MAIN |

Manholes: the branch ends in a riser shaft under the street with a steep ramp (about 30 degrees) up it; the lid breaks and flies off and the vehicle pops out onto the road with a clear run; from the street the lid is flush (a car drives over it), the way in is the exits. Exits: a small headwall or a grate in a bank, half hidden (secret), each a way in as well.

## The underground race (race layer, stage 1)

A lap of about 1358 m, raced from the menu with the AI (a route file like the others; its own checkpoints): from the hub floor by the pool, up the north-west ramp onto the mid ledge, M1 west under the Enclave's north pond (glass, CP1), into J1 through its M1 grille (open for the race once built), a hairpin round the chamber (CP2), out through the M5 grille beside it, south along the east side of Bells Ferry Road, along the Enclave pond's glass (CP3), J3 (CP4), into the hub on its top ledge (CP5), down the ramps past the waterfalls to the floor.

- **Gates** (barred grilles) close every other way while that race runs (G1 to G10 on the markers): BF's two entrance pipes and J1's M2 and north grilles, the maze branches at J2, J3 and the hub, M3. Out of a race they stand open (free roam, the maze).
- **Switch shortcuts**: each behind a gate that opens when any racer drives over its switch (it stays open for that lap); the AI follows the shortcut rule (CLAUDE.md) and may go for a switch when it falls behind.

  - **S1 Waterfall leap**: off the end of the hub's top ledge, through M5's waterfall, 20 m down onto a long landing ramp on the floor beside the pool; skips the two ramps down (about 220 m). Switch: a pressure plate high on the wall of M5's barrel 80 m before the hub (ride the wall to hit it). Forgives: CP5 is before it: nothing.
  - **S2 Pond tube**: a straight glass tube through the middle of the Enclave pond (W4) where M5 bends round its end; a fish tank you blast through, a gap jump across a flooded sump at its far end. Switch: a lever in a dead-end alcove off M5 just south of J1 (a 50 m detour). Forgives: none.
  - **S3 High line**: a raised pipe off M1 that crosses J2's glass chamber on a gap jump over the floor below, instead of M1's S-bend down through it. Switch: a pressure plate in J1's far corner, off the racing line. Forgives: CP1 (the glass under W1).

## Build stages

1. **Stage 1: the hub, the trunk, the race.** BF and J1 with its four stubs (built in Round 23), M1 with W1's glass, M5 with W4's glass and J3, the hub with its ledges, ramps, waterfalls and pool, the north and Enclave mazes (B1 to B10, five manholes, the W20 exit), the race route with checkpoints, gates and the three switch shortcuts.
2. **Stage 2: Noonday.** M3 to J4 and J5 (glass under the creek and the big lake, P1's grate), the outfall over the big lake, B11 to B15 (the woods trail exit, W12, W7, two manholes).
3. **Stage 3: the Glen and the shops.** M4 to J6 (the exit under the bridges), J7 and J8 (glass under the Chastain Glen pond), B16 to B22 (the launch over the pond, four manholes, behind the shopping center, Little Noonday Creek).
4. **Stage 4: the lakes.** M2 (glass along both Chastain lakes, J9, under the interstate to W5), M6 to Nelson Lake, B23 to B25.

## Frame-rate plan

- Nothing underground is drawn or simulated when you are not near it: the network is cut into cells (the hub, each junction, each 150 m of pipe); a cell's meshes, lamps, streams and waterfalls are switched on only while the player (or the camera) is in it or the next one, or looking into its mouth from the surface within 60 m.
- Lamps: point lights only in the cells switched on (at most about 12 at once); no shadows underground; the vehicles' own headlights as in P1.
- Water: one scrolling-texture stream per pipe (as P1); waterfalls as scrolling sheets with a few particles; glass stretches as P1's (fish only while you are in that cell).
- AI racers underground use the race line (no physics shortcuts); their pipe sounds only within earshot.
- From the surface the network costs nothing but its mouths, exits and manhole lids (flush).

## Versatile (the world, not the race)

The pipes, chambers, hub, exits and manholes are world: data files and the map build, like P1. The race's gates, switches, checkpoints and the route are a race layer that comes and goes with the race (as `RaceBarriers` and `RaceOnly`); nothing in the network assumes it is only a race track.
