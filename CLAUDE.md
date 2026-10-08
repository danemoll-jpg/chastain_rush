# Project Context & Rules — Chastain Rush

Sequel to Woodstock Rush (the `Racer` repo). Same kind of game, but the map is
generated from real-world data for the Chastain Glen area of Marietta, GA.

## Tech Stack
- Same engine and engine version as Racer. Confirm the exact version from the
  Racer project on first session and record it here.
- Map data: USGS 3DEP 1 m elevation (GeoTIFF, UTM zone 16N, NAD83, metres) and
  OpenStreetMap (roads, building footprints, water, woods). Details and the
  tile URL are in `TODO.md` under Technical Notes.
- Repo: `C:\Users\danmo\chastain_rush`. Racer is a separate repo and is the
  source for reused systems.

## Code Style & Architecture
- **Racer is read-only from this project.** Copy from it, never edit it.
- **Import from Racer selectively.** Bring a system over only when the current
  round needs it, bring only what it depends on, and list what came with it in
  the round report. Don't copy Racer wholesale and don't bring its git history.
- **World scale is real:** 1 unit = 1 metre, no vertical exaggeration.
- **Map generation is a re-runnable script driven by a bounding box**, not
  hand edits to generated output. Hand-made additions live separately from
  generated data so regenerating never destroys them.
- **Raw downloads stay out of git** (the elevation tile is about 313 MB). Keep
  them in an ignored folder; commit only the cropped, processed data.
- **One world, routes laid over it.** Don't build a separate copy of the world
  per course.
- **Licensing:** OpenStreetMap needs a visible "© OpenStreetMap contributors"
  credit. No Google Maps data of any kind.
- **Privacy:** don't commit the owner's street address or house number.

## Commands
- To be filled in once the project is set up: how to run the map generation,
  how to open and play the test scene, how to build.

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Nothing is built yet. The repo is clean. Active work is Round 1, the map
prototype: generate about 1.5 km x 1.5 km around Chastain Glen from real data
and make it drivable with one existing Racer vehicle. Everything else from
Racer is deliberately out of scope until Dan has driven the prototype.

End every round with a per-item status report: done / not done / deferred,
and why.
