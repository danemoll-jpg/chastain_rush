Active Development Plan — Chastain Rush

Sequel to Woodstock Rush, built on a real-world map generated from public map
data instead of a hand-built one. Working title: Chastain Rush.

**Design rule — DECIDED (2026-10-08):** the real world is the starting point,
and fun wins when the two disagree. The game does not need to be a 1:1 match.

Completed Tasks

* **Repo decision — DECIDED.** Chastain Rush lives in its own clean repo
  (`C:\Users\danmo\chastain_rush`), not a duplicate of Racer. Racer's history
  and one-off files stay behind. Systems are brought over from Racer one at a
  time, only when a round needs them. Racer itself is not modified by any
  Chastain Rush round.
* **Round 1, map prototype — built; driven by Dan at true scale (2026-10-07).**
  A 1534 m square around Chastain Glen generated from USGS 1 m elevation and
  OpenStreetMap, drivable with one Racer vehicle. Bounds: S 34.0315,
  N 34.0450, W -84.5433, E -84.5271, including the New Chastain Road entrance.
  Dan's verdict: the layout is recognizable and he could find his way around,
  but the hills felt lower than in real life.
* **OSM coverage — checked, good.** 801 buildings (718 houses), 226 drivable
  road ways, 8 ponds, 21 streams (Noonday Creek, Little Noonday Creek),
  2 woods. No Microsoft footprints needed for this area.
* **"Hills feel flat" — root cause found: perception, not data.** The first
  suspicion was squashed heights. Measurement showed the true-scale terrain
  matches the USGS crop to 6 mm (273.10 to 329.34 m in game against 273.098
  to 329.350 m in the source), with no off-road smoothing (mean difference
  0.2 mm more than 15 m from any road). Terrain is 2049 x 2049 samples, 0.749 m
  per sample, bilinear from the 1 m source. Measured from the data file
  (`terrain.r16`), not from Unity's built TerrainData.
* **Along-road smoothing bug — fixed.** A 20 m smoothing of the road profile
  was cutting grades by 6.5% on average (up to 2.8 points). Now off
  (`profile_smoothing_m` 0); roads level across their width only. Real but
  small: it was not the cause of the flat feeling.
* **Vertical scale setting — added, set to 1.5. DECIDED (2026-10-08): keep
  1.5 as the working value.** `vertical_scale` in `Tools/mapgen/config.json`;
  1.0 is true scale. Scaling is from the lowest point, so only the relief
  grows. Scene needs Chastain Rush > Build Map after a change. Status: built,
  scripted drive passed on Chastain Glen Lane, and CONFIRMED by Dan driving
  it at 1.5 on his PC (2026-10-08). His verdict: keep it.
* **Reference hill measurements** (Claude Code's reading of Dan's directions,
  not yet confirmed by Dan: "right out of the driveway" taken as east, "first
  left the other way" taken as Chasefield Way):
  * Chastain Glen Lane hill, 160 m: true scale +12.5 m, 7.8% average, 12.7%
    steepest. At 1.5: +18.8 m, 11.7% average, 19.0% steepest.
  * Chasefield Way, 235 m: true scale +9.6 m, 4.1% average, 7.3% steepest.
    At 1.5: +14.4 m, 6.1% average, 10.9% steepest.
* **Terrain/OSM alignment offset — DECIDED: leave as is.** Up to 0.37 m near
  the map edges from a half-pixel difference, about 4 cm of height on a 12%
  slope. Not worth fixing.

Current Objective (Focus Area)

1. Commit the current work with the config at 1.5. (Nothing was committed as
   of the last report.)
2. Check Hayworth Drive NE, which reads 33% at 1.5 (about 22% at true scale).
   Is it a real slope, or a road crossing a creek or culvert where the
   bare-earth data drops into the channel? List any other road segments over
   15% at true scale and say which cross water. Report only; don't fix yet.
3. Report status per item: done / not done / deferred, and why.

Open question for Dan: with the map prototype accepted, what comes next? See
the first few items under Next Steps. Recommended default: enlarge the area.

Not yet measured: performance on Dan's PC.

Next Steps (Do Not Start Yet)

* All of these are deferred, not decided against.
* If hills still read flat at a height setting that drives well: a lower chase
  camera and narrower field of view, before raising the scale further.
* Roads that cross water: bare-earth elevation has bridges removed, so these
  may need the road held level across the channel.
* Final map size. Growing north beyond roughly 1 km from Chastain Glen Lane
  needs a second elevation tile.
* One world with race routes laid over it (not a copy of the world per course,
  as in Woodstock Rush).
* Map detail pass: trees (2 woods are in the data but not drawn), driveways
  and missing houses, using NAIP aerial imagery as a reference only.
* Hand-built versions of the houses that matter, from photos, as was done for
  Kyle's house in Woodstock Rush.
* Importing the remaining Racer systems: rival AI, traffic, campaign, Free
  Roam, split-screen, menus and garage, radio, debug-report tool, launcher and
  publishing. Deferred until Woodstock Rush is finished, so the final versions
  are what gets brought over.
* Vehicle list, course list, campaign structure, and the final name.
* When Woodstock Rush is finished: record what to keep, what to build
  differently, and the final state of each reused system.

Technical Notes / Blockers

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
