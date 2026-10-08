Active Development Plan — Chastain Rush

Sequel to Woodstock Rush, built on a real-world map generated from public map
data instead of a hand-built one. Working title: Chastain Rush.

Completed Tasks

* **Repo decision — DECIDED.** Chastain Rush lives in its own clean repo
  (`C:\Users\danmo\chastain_rush`), not a duplicate of Racer. Racer's history
  and one-off files stay behind. Systems are brought over from Racer one at a
  time, only when a round needs them, and each import is a chance to leave
  dead code out. Racer itself is not modified by any Chastain Rush round.
* **Elevation data availability — confirmed by catalog lookup (not yet
  downloaded or opened).** One USGS 3DEP 1 m tile covers the prototype area:
  `USGS_1M_16_x72y377_GA_Statewide_2018_B18_DRRA.tif`, GeoTIFF, about 313 MB,
  10 km x 10 km. See Technical Notes for the URL and projection.

Current Objective (Focus Area)

**Round 1 — map prototype.** Goal: find out whether a map generated from real
data is recognizable and fun to drive, before committing to the approach.
Nothing in this round needs to look good.

Prototype area: a box about 1.5 km on a side centered on the Chastain Glen
neighborhood (Marietta, GA 30066). Starting bounds, approximate:
south 34.0315, north 34.0450, west -84.5433, east -84.5271. Adjust so the
whole neighborhood and its entrance road are inside, and report the final
bounds.

1. Query OpenStreetMap for the box and report what it holds: number of roads
   and their names, number of building footprints, any water and woods.
   **If house footprints are mostly missing, stop and report before going on**
   (the fallback is Microsoft's open US building footprints).
2. Download the USGS 1 m elevation tile, crop it to the box, and save the crop
   as the project's heightmap. The raw tile stays out of git.
3. Build the terrain from the heightmap at true scale: 1 unit = 1 metre, no
   vertical exaggeration.
4. Generate roads from the OSM road lines at realistic widths, and flatten the
   terrain across each road's width so the surface isn't tilted or bumpy.
5. Place buildings as plain extruded boxes on their footprints. Mark the
   owner's house so it's easy to find (Dan gives the house number directly;
   keep the address out of committed files).
6. Bring over one existing vehicle from Racer with its current handling,
   camera and controls, spawning on Chastain Glen Lane. Bring only what that
   vehicle needs, and list every file and system that had to come with it.
7. Keep the generation as a re-runnable script driven by the bounds, so the
   area can be enlarged later by changing numbers rather than redoing work.
8. Report status for every item above: done / not done / deferred, and why.

Out of scope for this round: trees, driveways, textures, traffic, rivals, race
routes, menus, and any other Racer system.

What Dan will judge when driving it:
* Do the streets read as the real neighborhood?
* Do the hills feel right, in steepness and in where they are?
* Are road widths and corners right for the existing vehicle's handling?
* Is the performance acceptable for an area this size?

Round 1 status (2026-10-07) — built, automated drive check passed, NOT yet driven by Dan

* Final bounds: unchanged from the starting bounds (S 34.0315, N 34.0450,
  W -84.5433, E -84.5271). The whole neighbourhood and its New Chastain Road
  entrance are inside. Map is the 1534 m UTM square around them.
* OSM check: 801 buildings (718 houses), 91 addressed houses on Chastain Glen
  Lane, 226 drivable road ways, 8 ponds, 21 streams (Noonday Creek, Little
  Noonday Creek), 2 woods. Footprint coverage is good, so no Microsoft
  footprints are needed for this area.
* Not done this round: performance not measured on Dan's PC; woods are
  reported but not drawn (trees are out of scope).

Next Steps (Do Not Start Yet)

* **Wait for the prototype verdict before deciding anything below.** All of
  these are deferred, not decided against.
* Final map size. Growing north beyond roughly 1 km from Chastain Glen Lane
  needs a second elevation tile.
* One world with race routes laid over it (not a copy of the world per course,
  as in Woodstock Rush).
* Map detail pass: trees, driveways and missing houses placed using NAIP
  aerial imagery as a reference only.
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
  (NAVD88). It is a bare-earth surface, so buildings and trees are already
  removed. Tile covers latitude 33.955 to 34.048 and longitude -84.619 to
  -84.508. Public domain.
* **OpenStreetMap** data is under the ODbL licence. The game needs a visible
  credit, "© OpenStreetMap contributors". OSM coordinates are latitude and
  longitude, so they must be converted to the same UTM metres as the elevation
  before anything is placed.
* **Google Maps data can't be used in a game.** Not for geometry, imagery or
  tracing.
* **NAIP aerial imagery** is a placement reference only, not a texture source
  for this project.
* **OSM coverage for this area has not been checked yet.** Item 1 of Round 1
  is that check.
* **Verification wording:** "built" and "driven by Dan on his PC" are separate
  claims. Record which one applies to each item.
* Process notes for this collaboration are in `collaboration-process-notes.md`.
