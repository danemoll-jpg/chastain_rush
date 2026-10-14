# The underground pipe network, as built

Made by `Tools/mapgen/pipe_network.py` from its layout (Data/pipes/network.json); built by the map build (MapBuilder.Network.cs, MapBuilder.NetworkHub.cs) up to **stage 4**. The plan it follows: Docs/PIPE-NETWORK-PROPOSAL.md. World, not race: there is no underground race, no gates and no switches (the race's proposal markers are kept: debug menu, Proposal markers).

**Ways in**: BF (the creek channel west of Bells Ferry Road) and GP (the Chastain Glen pond's culvert), each two pipes that go off in different directions; every exit is a way in as well. Manholes open only from below.

**Finding your way**: the mains carry a color stripe along both walls, with chevrons on it every 8 m pointing to the hub, their names are painted over their openings in the junctions (M1 red, M5 orange, M3 green, M4 yellow, M2 blue, M6 purple) and a HUB sign with an arrow stands beside the one that leads to the hub; every main ends in the hub; the maze branches are unmarked. Underground, the map (M or View) shows the pipes and the junctions', manholes' and exits' names.

## Part B: the two entrances (BUILT)

| Pipe | | Size | Length | Floor | Notes |
|---|---|---|---|---|---|
| BF-R | BF right pipe (to J1, the trunk and the Enclave main) | 5.0 m | 83 m | 275 to 281 m | BF to J1;  |
| BF-L | BF left pipe (to J10, the lakes main and the north maze) | 5.0 m | 57 m | 281 to 282 m | BF to J10;  |
| GP-L | Glen pond left pipe (north, to J12 and the Glen main) | 5.0 m | 111 m | 283 to 286 m | GP to J12;  |
| GP-R | Glen pond right pipe (south, under New Chastain Road to J13) | 5.0 m | 126 m | 283 to 285 m | GP to J13;  |

Chambers: **J1 SOUTH FORK** (junction, x 1496, z 1148, floor 275 m); **J10 NORTH FORK** (junction, x 1452, z 1256, floor 282 m); **J12 GLEN NORTH** (junction, x 2966, z 908, floor 286 m); **J13 GLEN SOUTH** (junction, x 2908, z 704, floor 285 m)

## Stage 1: the hub, the trunk, the Enclave (BUILT)

| Pipe | | Size | Length | Floor | Notes |
|---|---|---|---|---|---|
| M1a | M1 Trunk (J1 to J2, glass under the pond) | 5.0 m | 142 m | 274 to 275 m | J1 to J2; glass under pond W1 (76 m) |
| M1b | M1 Trunk (J2 to the hub) | 5.0 m | 220 m | 262 to 274 m | J2 to HUB;  |
| M5a | M5 Enclave Main (J1 to J3, glass along the pond) | 5.0 m | 540 m | 273 to 275 m | J1 to J3; glass under pond W4 (79 m) |
| M5b | M5 Enclave Main (J3 to the hub) | 5.0 m | 192 m | 272 to 273 m | J3 to HUB;  |
| B1a | B1 | 5.0 m | 109 m | 274 to 278 m | J2 to K2;  |
| M2g | M2 Lakes Main (K2 to K3) | 5.0 m | 98 m | 278 to 280 m | K2 to K3;  |
| M2h | M2 Lakes Main (K3 to the hub) | 5.0 m | 172 m | 272 to 280 m | K3 to HUB;  |
| M2e | M2 Lakes Main (J10 to K1) | 5.0 m | 43 m | 281 to 282 m | J10 to K1;  |
| B2b | B2 | 3.5 m | 58 m | 281 to 283 m | K1 to MH1; pops out at MH1 |
| M2f | M2 Lakes Main (K1 to K2) | 5.0 m | 62 m | 278 to 281 m | K1 to K2;  |
| B3 | B3 | 3.5 m | 79 m | 280 to 285 m | K3 to MH2; pops out at MH2 |
| B4a | B4 (down the drop shaft) | 3.5 m | 63 m | 277 to 280 m | K3 to S4; drops into S4 |
| B4b | B4 | 3.5 m | 63 m | 269 to 274 m | S4 to J2;  |
| B5 | B5 | 3.5 m | 44 m | 269 to 271 m | S4 to D5;  |
| B6 | B6 | 3.5 m | 114 m | 273 to 284 m | J3 to MH3; pops out at MH3 |
| B7a | B7 | 3.5 m | 114 m | 273 to 286 m | J3 to K7;  |
| B7b | B7 | 3.5 m | 98 m | 286 to 294 m | K7 to MH4; pops out at MH4 |
| B8 | B8 (flooded under Bells Ferry Road) | 3.5 m | 263 m | 284 to 288 m | K7 to X_W20; a flooded sag (slow); comes out at exit w20 pond |
| B9a | B9 | 3.5 m | 125 m | 262 to 273 m | HUB to K8;  |
| B9b | B9 | 3.5 m | 88 m | 273 to 280 m | K8 to MH5; pops out at MH5 |
| B10 | B10 | 3.5 m | 81 m | 273 to 273 m | K8 to D10;  |

Chambers: **J2 POND JCT** (junction, x 1628, z 1096, floor 274 m); **J3 ENCLAVE JCT** (junction, x 1785, z 1000, floor 273 m); **THE HUB** (hub, x 1900, z 1145, floor 252 m); **K1** (junction, x 1512, z 1246, floor 281 m); **K2** (junction, x 1585, z 1214, floor 278 m); **K3** (junction, x 1700, z 1230, floor 280 m); **S4 DROP SHAFT** (shaft, x 1660, z 1166, floor 269 m); **D5** (turn, x 1608, z 1180, floor 271 m); **K7** (junction, x 1720, z 890, floor 286 m); **K8** (junction, x 1985, z 1010, floor 273 m); **D10** (turn, x 2060, z 1060, floor 273 m)

Manholes: **MH1** in Parkwood Drive (x 1548, z 1277; riser 12 m); **MH2** in Parkwood Drive (x 1760, z 1277; riser 6 m); **MH3** in Bloombridge Way (x 1879, z 949; riser 10 m); **MH4** in Rotherhithe Landing (x 1708, z 812; riser 11 m); **MH5** in Bloombridge Way (x 2012, z 933; riser 9 m)

Exits: **Exit W20 Pond** (x 1374, z 800)

## Stage 2: Noonday (BUILT)

| Pipe | | Size | Length | Floor | Notes |
|---|---|---|---|---|---|
| M3a | M3 Noonday Main (J4 to the hub) | 5.0 m | 330 m | 262 to 269 m | J4 to HUB;  |
| M3b | M3 Noonday Main (J4 to J5, under Noonday Creek, west of P1) | 5.0 m | 145 m | 267 to 269 m | J4 to J5;  |
| M3c | M3 Noonday Main (J5 up the big lake in glass, to K15) | 5.0 m | 325 m | 268 to 270 m | J5 to K15; glass under pond W15 (92 m) |
| M3d | M3 Noonday Main (the launch over the big lake) | 5.0 m | 73 m | 270 to 271 m | K15 to L_W15; comes out at launch big lake |
| B11 | B11 | 3.5 m | 117 m | 269 to 272 m | J4 to X_TRAIL; comes out at exit creek trail |
| B12 | B12 | 3.5 m | 218 m | 268 to 273 m | J5 to X_W12; comes out at exit w12 pond |
| B13 | B13 | 3.5 m | 92 m | 270 to 279 m | K15 to MH6; pops out at MH6 |
| B14 | B14 | 3.5 m | 459 m | 269 to 278 m | J4 to MH7; pops out at MH7 |
| B15 | B15 | 3.5 m | 143 m | 270 to 274 m | K15 to X_W7; comes out at exit w7 pond |

Chambers: **J4 NOONDAY JCT** (junction, x 2280, z 1215, floor 269 m); **J5 LAKE JCT** (junction, x 2350, z 1352, floor 268 m); **K15** (junction, x 2472, z 1562, floor 270 m)

Manholes: **MH6** in Guidhall Trail (x 2550, z 1584; riser 6 m); **MH7** in Westminister Way (x 2724, z 1197; riser 9 m)

Exits: **Launch Big Lake** (x 2394, z 1528, a launch over the water); **Exit Creek Trail** (x 2339, z 1112); **Exit W12 Pond** (x 2497, z 1443); **Exit W7 Pond** (x 2446, z 1700)

## Stage 3: the Glen and the shops (BUILT)

| Pipe | | Size | Length | Floor | Notes |
|---|---|---|---|---|---|
| M4a | M4 Glen Main (J6 to the hub) | 5.0 m | 499 m | 262 to 268 m | J6 to HUB;  |
| B27 | B27 (J4 to J6) | 5.0 m | 337 m | 267 to 269 m | J4 to J6; a flooded sag (slow) |
| M4b | M4 Glen Main (J6 to J7) | 5.0 m | 338 m | 268 to 278 m | J6 to J7;  |
| M4c | M4 Glen Main (J7 to J12, glass under the Chastain Glen pond) | 5.0 m | 275 m | 278 to 286 m | J7 to J12; glass under pond W8 (113 m) |
| M4d | M4 Glen Main (J12 to J8) | 5.0 m | 361 m | 286 to 294 m | J12 to J8;  |
| B16 | B16 | 3.5 m | 375 m | 268 to 278 m | J6 to MH8; pops out at MH8 |
| B17 | B17 | 3.5 m | 175 m | 276 to 284 m | J7 to MH9; pops out at MH9 |
| B19a | B19 | 3.5 m | 244 m | 285 to 290 m | J13 to K19;  |
| B19b | B19 | 3.5 m | 69 m | 289 to 290 m | K19 to MH10; pops out at MH10 |
| B19c | B19 | 3.5 m | 89 m | 289 to 290 m | K19 to X_W11; comes out at exit w11 pond |
| B26 | B26 | 3.5 m | 110 m | 268 to 272 m | J6 to X_BRIDGE; comes out at exit under bridges |
| B20 | B20 | 3.5 m | 268 m | 293 to 294 m | J8 to MH11; pops out at MH11 |
| B21 | B21 | 5.0 m | 38 m | 294 to 298 m | J8 to X_SHOPS; comes out at exit behind shops |
| B22 | B22 | 3.5 m | 600 m | 277 to 294 m | J8 to X_LNC; comes out at exit little noonday |

Chambers: **J6 BRIDGE JCT** (junction, x 2384, z 884, floor 268 m); **J7 GLEN JCT** (junction, x 2722, z 975, floor 278 m); **J8 SHOPS JCT** (junction, x 3194, z 1204, floor 294 m); **K19** (junction, x 2700, z 560, floor 290 m)

Manholes: **MH8** in Paxton Court Northeast (x 2127, z 619; riser 9 m); **MH9** in Grasmere Court Northeast (x 2730, z 816; riser 7 m); **MH10** in Weybourne Court Northeast (x 2691, z 613; riser 6 m); **MH11** in Chasefield Way (x 2965, z 1093; riser 6 m)

Exits: **Exit W11 Pond** (x 2686, z 430); **Exit Under Bridges** (x 2422, z 780); **Exit Behind Shops** (x 3153, z 1256); **Exit Little Noonday** (x 3752, z 1468)

## Stage 4: the lakes (BUILT)

| Pipe | | Size | Length | Floor | Notes |
|---|---|---|---|---|---|
| M2a | M2 Lakes Main (J10 to J9, glass along East Chastain Lake) | 5.0 m | 614 m | 282 to 284 m | J10 to J9; glass under pond W9 (406 m) |
| M2b | M2 Lakes Main (J9 to K25, glass along West Chastain Lake) | 5.0 m | 345 m | 284 to 296 m | J9 to K25; glass under pond W0 (107 m) |
| M2c | M2 Lakes Main (K25 under the interstate to the far-west pond) | 5.0 m | 270 m | 296 to 296 m | K25 to X_W5; comes out at exit far west pond |
| M6 | M6 Nelson Main | 5.0 m | 2000 m | 262 to 287 m |  to HUB; comes out at exit nelson lake |
| B23 | B23 | 3.5 m | 112 m | 284 to 294 m | J9 to MH12; pops out at MH12 |
| B24 | B24 | 3.5 m | 308 m | 284 to 301 m | J9 to MH13; pops out at MH13 |
| B25 | B25 | 3.5 m | 56 m | 296 to 296 m | K25 to X_STREAM; comes out at exit lake stream |

Chambers: **J9 LAKES JCT** (junction, x 868, z 1236, floor 284 m); **K25** (junction, x 560, z 1418, floor 296 m)

Manholes: **MH12** in Laurel Green Court Northwest (x 801, z 1162; riser 19 m); **MH13** in Chastain Lakes Drive Northwest (x 977, z 962; riser 8 m)

Exits: **Exit Far West Pond** (x 182, z 1488); **Exit Nelson Lake** (x 262, z 118); **Exit Lake Stream** (x 470, z 1425)

## The pipe express (free roam only)

On every run of pipe longer than about 100 m between two places where you have a choice or a reason to stop (a junction, the hub, an exit, a manhole's foot, the split behind BF or GP, either end of a glass stretch or a flooded sag), an **EXPRESS** band painted round the pipe and arrows on the floor mark each end, 20 m short of those places. Drive in at either end and the water surges: the controls lock and you are carried along the floor's middle (a whoosh, speed streaks), and let go at the far end at 20 m/s, facing on out of it, so every turn is still yours. A long one goes faster, so no ride is longer than about 12 s. Glass stretches are never express. Debug menu, **Pipe express**: Boost (the default), Warp (a short dark fade, then you are at the far end) or Off. During a race it is off and these pipes drive as any other; the AI never uses it. On the map the express stretches are drawn in cyan.

| Express | Pipe | From | To | Length | Top speed | Ride | Driving it (20 m/s) |
|---|---|---|---|---|---|---|---|
| E1 M6 Nelson express | M6 (5.0 m) | Exit Nelson Lake (20 m along) | HUB (1980 m along) | 1960 m | 211 m/s | about 11 s | 98 s |
| E2 M4 Glen express | M4a (5.0 m) | J6 (20 m along) | HUB (479 m along) | 459 m | 45 m/s | about 11 s | 23 s |
| E3 M4 J12 to J8 express | M4d (5.0 m) | J12 (20 m along) | J8 (341 m along) | 321 m | 45 m/s | about 8 s | 16 s |
| E4 M3 Noonday express | M3a (5.0 m) | J4 (20 m along) | HUB (310 m along) | 290 m | 45 m/s | about 7 s | 14 s |
| E5 B22 Little Noonday express | B22 (3.5 m) | J8 (20 m along) | Exit Little Noonday (580 m along) | 560 m | 53 m/s | about 12 s | 28 s |
| E6 GP-L express | GP-L (5.0 m) | GP split (20 m along) | J12 (91 m along) | 71 m | 45 m/s | about 2 s | 4 s |
| E7 GP-R express | GP-R (5.0 m) | GP split (20 m along) | J13 (106 m along) | 86 m | 45 m/s | about 3 s | 4 s |
| E8 M1b express | M1b (5.0 m) | J2 (20 m along) | HUB (200 m along) | 180 m | 45 m/s | about 5 s | 9 s |
| E9 M5a express | M5a (5.0 m) | J1 (20 m along) | glass under W4's start (241 m along) | 221 m | 45 m/s | about 6 s | 11 s |
| E10 M5a express | M5a (5.0 m) | glass under W4's end (360 m along) | J3 (520 m along) | 160 m | 45 m/s | about 4 s | 8 s |
| E11 M5b express | M5b (5.0 m) | J3 (20 m along) | HUB (172 m along) | 152 m | 45 m/s | about 4 s | 8 s |
| E12 B1a express | B1a (5.0 m) | J2 (20 m along) | K2 (89 m along) | 69 m | 45 m/s | about 2 s | 3 s |
| E13 M2h express | M2h (5.0 m) | K3 (20 m along) | HUB (152 m along) | 132 m | 45 m/s | about 4 s | 7 s |
| E14 B6 express | B6 (3.5 m) | J3 (20 m along) | MH3's foot (94 m along) | 74 m | 45 m/s | about 2 s | 4 s |
| E15 B7a express | B7a (3.5 m) | J3 (20 m along) | K7 (94 m along) | 74 m | 45 m/s | about 2 s | 4 s |
| E16 B8 express | B8 (3.5 m) | K7 (20 m along) | the flooded sag's start (218 m along) | 198 m | 45 m/s | about 5 s | 10 s |
| E17 B9a express | B9a (3.5 m) | HUB (20 m along) | K8 (105 m along) | 85 m | 45 m/s | about 3 s | 4 s |
| E18 M3b express | M3b (5.0 m) | J4 (20 m along) | J5 (125 m along) | 105 m | 45 m/s | about 3 s | 5 s |
| E19 M3c express | M3c (5.0 m) | glass under W15's end (141 m along) | K15 (305 m along) | 164 m | 45 m/s | about 4 s | 8 s |
| E20 B11 express | B11 (3.5 m) | J4 (20 m along) | Exit Creek Trail (97 m along) | 77 m | 45 m/s | about 2 s | 4 s |
| E21 B12 express | B12 (3.5 m) | J5 (20 m along) | Exit W12 Pond (198 m along) | 178 m | 45 m/s | about 5 s | 9 s |
| E22 B14 express | B14 (3.5 m) | J4 (20 m along) | MH7's foot (439 m along) | 419 m | 45 m/s | about 10 s | 21 s |
| E23 B15 express | B15 (3.5 m) | K15 (20 m along) | Exit W7 Pond (123 m along) | 103 m | 45 m/s | about 3 s | 5 s |
| E24 B27 express | B27 (5.0 m) | J4 (20 m along) | the flooded sag's start (184 m along) | 164 m | 45 m/s | about 4 s | 8 s |
| E25 B27 express | B27 (5.0 m) | the flooded sag's end (246 m along) | J6 (317 m along) | 71 m | 45 m/s | about 2 s | 4 s |
| E26 M4b express | M4b (5.0 m) | J6 (20 m along) | J7 (318 m along) | 298 m | 45 m/s | about 7 s | 15 s |
| E27 M4c express | M4c (5.0 m) | glass under W8's end (182 m along) | J12 (255 m along) | 73 m | 45 m/s | about 2 s | 4 s |
| E28 B16 express | B16 (3.5 m) | J6 (20 m along) | MH8's foot (355 m along) | 335 m | 45 m/s | about 8 s | 17 s |
| E29 B17 express | B17 (3.5 m) | J7 (20 m along) | MH9's foot (155 m along) | 135 m | 45 m/s | about 4 s | 7 s |
| E30 B19a express | B19a (3.5 m) | J13 (20 m along) | K19 (224 m along) | 204 m | 45 m/s | about 5 s | 10 s |
| E31 B26 express | B26 (3.5 m) | J6 (20 m along) | Exit Under Bridges (90 m along) | 70 m | 45 m/s | about 2 s | 3 s |
| E32 B20 express | B20 (3.5 m) | J8 (20 m along) | MH11's foot (248 m along) | 228 m | 45 m/s | about 6 s | 11 s |
| E33 M2a express | M2a (5.0 m) | J10 (20 m along) | glass under W9's start (118 m along) | 98 m | 45 m/s | about 3 s | 5 s |
| E34 M2b express | M2b (5.0 m) | J9 (20 m along) | glass under W0's start (141 m along) | 121 m | 45 m/s | about 3 s | 6 s |
| E35 M2c express | M2c (5.0 m) | K25 (20 m along) | Exit Far West Pond (250 m along) | 230 m | 45 m/s | about 6 s | 12 s |
| E36 B23 express | B23 (3.5 m) | J9 (20 m along) | MH12's foot (92 m along) | 72 m | 45 m/s | about 2 s | 4 s |
| E37 B24 express | B24 (3.5 m) | J9 (20 m along) | MH13's foot (288 m along) | 268 m | 45 m/s | about 7 s | 13 s |

## What differs from the proposal

- J1 is no longer a big room straight inside BF (Dan, BUG-006): BF's two barrels split just past the headwall; the right one runs to J1, now a junction 75 m further on (M1 and M5 start there), the left one to J10 (M2 and B2), near where the old J1 stood
- The Chastain Glen pond culvert is the second entrance (Dan, BUG-001): two 5 m pipes, the left one north to J12 (on M4, which now passes it), the right one south under New Chastain Road to J13 (B19 now starts there, not at J7)
- M1 runs under the north Enclave pond (W1) in a glass tube along the pond's length; J2 is a plain junction on the pond's east shore instead of a chamber with a glass roof
- Every place where a branch leaves another is a junction chamber (K1 to K25): pipes only ever meet in a room
- B4's drop shaft is a chamber of its own (S4): B4 drops 8 m into it from K3; B5 is a dead end off the shaft's floor with a small turning room (D5); B10 likewise (D10)
- M3: no glass under Noonday Creek (a creek is too narrow to dig round a tube) and no grate from P1's floor (P1 is a finished round pipe); the glass is under the big lake, J5 beside it on the shore
- No drop shaft at J7 (one drop shaft, S4, in the north maze)
- Stage 2: J5 sits on the big lake's south shore (the proposal's J5 was in the water); M3 passes west of P1's inlet (it would have hit P1) and runs up the big lake in glass to K15, a junction on its east side; the launch over the big lake goes out from its east shore; B12 comes out on the north bank of the pond east of the lake
- Stage 3: J6 moved 50 m north of the New Chastain Road bridges (the exit under the bridges needs room for its cut; B26 is 3.5 m); the launch over the Chastain Glen pond comes off J12 into its north-east corner (from K17 it could not climb to the pond's level in time; K17 is gone, B17 runs from J7 to its manhole); J8 sits a little up the slope; the exit behind the shops is in the low strip north-west of the center, its cut into the bank
- Stage 4: J9 sits deeper (its floor 284 m) so M2 stays under East Chastain Lake's bed to its shore; the exit at the pond by Chastain Road (W21) is dropped (the ground there is too low for a pipe to come up under it) and B24 runs from J9 straight to its manhole
- Round 24 Part B (Dan: "shouldn't all main lines intersect here?"): every main now pours into the hub as a waterfall off a ledge. M3 comes in at the mid ledge (it came in at the floor); M4 starts with a new length from J6 straight to the hub's mid ledge (the old J4 to J6 length is B27, an unmarked 5 m branch, with its flooded sag); M2 goes on from J10 through K1, K2 and K3 to the hub's top ledge (the north maze's 5 m line; K1 to K2 widened to 5 m); M6 runs from Nelson Lake to the hub's mid ledge, 2 km (it ran from J9, on M2)
- Round 24 Part B (Dan: "It is very easy to get turned around in the tunnels"): chevrons on the mains' stripes point the way to the hub along the mains, and every junction on a main has a HUB sign with an arrow beside the main that leads there
- Round 24 (Dan): L_W8, the launch over the Chastain Glen pond, and its pipe B18 are out (they left a house floating); GP, 100 m away, is that pond's way in and out
