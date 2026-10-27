using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 5: a race route is data laid over the one world (Data/routes/*.json, made by Tools/mapgen/make_route.py), not a
    // copy of it. This file turns that data into what the race needs: the lap path in travel order for the chosen direction,
    // a start/finish gate, ordered checkpoints and a grid pose, all with heights from the real ground.
    //
    // Shape borrowed from Racer (read, not copied): a closed centreline (RaceRoad), directed timing planes with a half width
    // (RaceGate: the segment from last step to this step is tested against the plane) and numbered ordered gates
    // (RaceProgress).
    //
    // Round 6: gates are wide and forgiving; official shortcuts (shaped after Racer's WoodlandRoute: a path, an entry plane,
    // the gates it bypasses) and road closures (barriers across roads that leave the course) are part of the route data.
    [Serializable] public class RouteCheckpointJson { public int id; public float s; public float[] pos; public string label; public float arch_m, half_m; public float y, ring_m, ring_w, ring_h; }   // Round 71 (the air race): y the ring's middle, ring_m a ring's radius, ring_w / ring_h a frame's half width and half height   // Round 27: arch_m, the arch's height (0: 7.5 m; low in a pipe); half_m its own half width (0: the route's)

    // Round 6: an OFFICIAL shortcut. A path from entry to exit, an entry plane across it a little way in (crossing that plane
    // in the direction of the path is "taking the shortcut") and the checkpoints it forgives. Heights come from the world.
    // Checkpoint ids are the ids in this file (forward numbering), whatever direction the race runs.
    [Serializable]
    public class RouteShortcutJson
    {
        public string id, name, direction;   // direction: "forward" (default), "reverse", or "both" (reverse driving uses the path backwards)
        public float[] path;                 // x, z, x, z ... from entry to exit (forward direction)
        public float half_width_m, entry_inset_m, entry_margin_m;
        public int[] forgives;               // checkpoint ids skipped by taking it
        public float ai_speed;               // Round 18: the speed an AI driver takes it at (0: RaceController.AiShortcutSpeed)
        public float[] ai_profile;           // Round 18: station along the path, speed, ...: no faster than this there (R3's bend)
        // Round 20 follow-ups: 'lap_option' keeps a line that forgives nothing (N5, a jump beside the lap: for the AI and the map);
        // 'ai_take' is the AI's own chance of taking it (0: Racer's ShortcutStrategy); 'ai_slip' the chance that an AI driver who
        // takes it makes a sloppy try at the speeds of 'ai_slip_profile' instead of 'ai_profile' (R1: sometimes into the pool)
        public bool lap_option;
        public float ai_take, ai_slip;
        public float[] ai_slip_profile;
        public float[] path_y;               // Round 27 (the underground race): the path's heights (none: the ground's, or a pipe's floor)
    }
    // Round 27 (the underground race's race layer, built by RaceBarriers while it runs): a SEAL is a barred grille across a pipe's opening
    // the race does not use; a RAIL a run of railing (the hub's top ledge); a SWITCH GATE a grille across a shortcut that opens, for the
    // rest of the race, when any racer drives over its switch (a plate)
    [Serializable] public class RouteSealJson { public string id, label; public float x, y, z, dx, dz, w, h; }
    [Serializable] public class RouteRailJson { public string id; public float[] points; public float y, height; public float[] ys; }   // points x, z, ...; Round 58: ys, each point's own height (the span's sides)
    [Serializable] public class RouteSwitchGateJson { public string id, shortcut, label; public RouteSealJson gate; public float sx, sy, sz, sr, nx, ny, nz; }   // sx..sz the plate's middle, sr its reach, n its face's normal
    // Round 18: a stretch of the lap where an AI driver holds a set speed (a jump: the crest rule would brake it over every lip)
    [Serializable] public class RouteAiZoneJson { public string id; public float s0, s1, speed; }
    // Round 62: a stretch of the lap where the AI keeps within a narrower half width (a footbridge, an underpass, a mall's aisle)
    [Serializable] public class RouteWidthJson { public string id; public float s0, s1, half; }
    [Serializable] public class RouteResetSkipJson { public string id; public float s0, s1, to_s, short_s; public float[] short_area; }   // Round 43: short_area / short_s (CarRecovery)   // Round 33: stations in the route file's own numbering

    // A road that leaves the course and is not part of an official shortcut: a barrier stands across it during a race.
    [Serializable]
    public class RouteClosureJson
    {
        public string id, label;
        public float x, z, dx, dz;           // barrier centre; unit direction of the closed road, away from the course
        public float width_m, junction_s;
        public float[] contact;              // x, z where the road meets the course
    }

    [Serializable] public class RoutePieceJson { public long osm_id; public string name; public float length_m; public bool reverse; }
    [Serializable] public class RouteJunctionJson { public float s; public string[] roads; public float[] pos; }
    [Serializable]
    public class RouteJson
    {
        public string name, direction;
        // Round 54 (A1): the name its records are kept under when the lap changed and the old times no longer compare (Addison Heights' moved line); none: name
        public string records_as;
        public int laps;
        public float lap_length_m, lead_in_length_m, gate_s, gate_half_width_m, checkpoint_half_width_m, reverse_grid_back_m;
        public float gate_below_m, gate_above_m;   // Round 6: how far under / over the road surface a crossing still counts
        public float[] loop, lead_in;
        public RouteCheckpointJson[] checkpoints;
        public RoutePieceJson[] loop_pieces;
        public RouteJunctionJson[] junctions;
        public RouteShortcutJson[] official_shortcuts;
        public RouteClosureJson[] closures;
        // Round 18: the Noonday Creek trail. directions: the ones this route may be raced in (none listed = both); vehicles: the
        // only vehicle ids allowed on it (none listed = all); trail: a narrow trail (the AI keeps close to the line: its half
        // width, lane and passing lane), ai_zones: held speeds
        public string[] directions, vehicles;
        public bool trail;
        public bool traffic;   // Round 30 (A3): a race on open roads, traffic on its course (none yet: every race closes its roads to traffic)
        public float ai_half_width_m, ai_lane_m, ai_pass_lane_m;
        public RouteAiZoneJson[] ai_zones;
        public RouteAiZoneJson[] ai_zones_reverse;
        // Round 54 (A7, Dan on Addison Heights: "even on hard I beat the AI really bad"): a hilly course's own limits for the AI, Easy / Normal / Hard:
        // the speed it holds on a grade over 14 % (RoadDriver's hillTargets, 26 / 32 / 35 m/s) and how hard a crest it takes at speed (the 'g' of
        // sqrt(g / crest), 6.5 / 7.6 / 8.2: it keeps its wheels on the ground; more lets it fly a crest as a player does). None: RoadDriver's own
        public float[] ai_hill_targets, ai_crest_g;   // Round 21: the reverse race's held speeds (its own jumps), in the reverse lap's stations
        // Round 27 (the underground race): loop_y / lead_in_y the heights of loop / lead_in (none: the ground's); underground: heights come
        // from the route, not the terrain over it, while it is raced (RaceRoute.Surface), and its arches are low with no light beams
        public float[] loop_y, lead_in_y;
        // Round 62 (the Noonday Creek Trail race through Town Center): loop_y may hold CodeGround (the world's ground there) beside real heights (the
        // mall's floors, which the map build writes in when it embeds the route: MapBuilder.BuildRace); loop_rev_y the same for loop_rev. A route with
        // real heights of its own is raced on them where it has them (Surface): the grid, the AI, the resets
        public float[] loop_rev_y;
        public const float CodeGround = -10000f;
        public RouteWidthJson[] ai_widths, ai_widths_rev;   // Round 62: the AI's narrower stretches (RaceRoad.HalfWidth), in the lap's own stations
        public bool underground;
        public float gate_arch_m;   // the start / finish arch's height (0: 7.5 m; under the hub's ceiling)
        public RouteSealJson[] seals;
        public RouteRailJson[] rails;
        public RouteSwitchGateJson[] switch_gates;
        public RouteResetSkipJson[] reset_skip;   // Round 33 (the Ridge leap): a reset between s0 and s1 (forward) puts the vehicle at to_s, past the landing
        // Round 39 (C, the reverse Chastain Ridge): a reverse lap of its own, in travel order, when the reverse is not the loop run backward (other
        // carriageways, right-hand lanes the other way, a jump that only works one way): its gate, checkpoints, junctions, closures and reset skips
        // in its own stations; ai_zones_reverse are in them too
        public float[] loop_rev;
        public float gate_s_rev, lap_length_rev_m;
        public RouteCheckpointJson[] checkpoints_rev;
        public RouteJunctionJson[] junctions_rev;
        public RouteClosureJson[] closures_rev;
        public RouteResetSkipJson[] reset_skip_rev;
        public string[] plumes, plumes_rev;   // Round 40 (A1): the manhole plumes this race arms in each direction (ManholePlume: none listed, it never blows in the race)
        public bool OwnReverse => loop_rev != null && loop_rev.Length >= 8;
        public float[] finish_park, finish_park_y;   // Round 28: where finished AI cars drive and park, off the lap (x, z ... and heights; RoadDriver)
        public float finish_park_gap_m, finish_park_speed;   // Round 35: the parked cars' spacing (0: 9 m) and the drive there (0: 9 m/s)
        public float gate_ring_m;   // Round 35: the line is in a pipe of this radius: a checkered hoop round the pipe's wall instead of the arch
        // Round 71 (the Quarry Air Race, Tools/mapgen/make_air_route.py): an AIR route: flown by the plane (vehicles), its lap a line in the sky (loop +
        // loop_y, every point its own height), its checkpoints rings in the sky (RouteCheckpointJson y, ring_m or ring_w / ring_h), its line a ring too
        // (gate_y, gate_ring_air_m); lead_in the take-off run from the grid on the runway
        public bool air;
        public float gate_y, gate_ring_air_m;
        public bool Allows(string direction) => directions == null || directions.Length == 0 || Array.IndexOf(directions, direction) >= 0;
        public bool AllowsVehicle(string id) => air ? vehicles != null && Array.IndexOf(vehicles, id) >= 0 :   // Round 71: an air route's own (the plane) only
            !Racer.VehicleProfile.Find(id).Stunt && !Racer.VehicleProfile.Find(id).Flies &&   // Round 63: never the helicopter
            (vehicles == null || vehicles.Length == 0 || Array.IndexOf(vehicles, id) >= 0);   // Round 36: never a stunt vehicle (Racer: Free Roam and the Stunt event only)
    }

    public sealed class RaceGate
    {
        public int Id;               // 0 = start / finish, 1.. = checkpoints in travel order
        public int DataId;           // the checkpoint's id in the route file (forward numbering); 0 for the start / finish
        public string Label;
        public Vector3 Position;     // on the ground, in the middle of the road
        public Vector3 Forward;      // horizontal travel direction through the gate
        public Vector3 Right;
        public float HalfWidth, S;   // S: distance along the lap in travel order from the lap's own start
        public float Below = 6f, Above = 25f;   // metres under / over Position.y that still count (a jump over the gate counts)
        public float Arch;                      // Round 27: the arch's height (0: the usual 7.5 m)
        // Round 71 (the air race): a ring in the sky (Ring its radius) or a frame (RingW / RingH its half width and half height) round Position, upright
        // across Forward: the plane's middle passes inside it (reach does not count: the rings are sized for the plane)
        public float Ring, RingW, RingH;
        public bool IsRing => Ring > 0f || RingW > 0f;
        public bool EitherWay;   // Round 73 (B3): an air race's checkpoint ring counts flown through either way (the start / finish ring one way only)

        // True when the step from -> to passes through the gate's opening in the travel direction, at any angle; fraction is
        // how far along the step it happened (so lap times are not rounded to physics steps). reach is the car's own half
        // extent across the gate at that moment: a car that touches a post counts, which is what the markers show.
        public bool Crossed(Vector3 from, Vector3 to, float reach, out float fraction)
        {
            fraction = 0f;
            Vector3 a = from - Position, b = to - Position;
            float za = Vector3.Dot(a, Forward), zb = Vector3.Dot(b, Forward);
            // Round 73 (B3, Dan: "I circled back to a cp that I missed ... you only get credit if you are going in the direction of the game"): an air
            // race's ring counts flown through either way
            if (!(za < 0f && zb >= 0f) && !(EitherWay && za > 0f && zb <= 0f)) return false;
            fraction = -za / (zb - za);
            Vector3 hit = Vector3.Lerp(a, b, fraction);
            if (IsRing)
            {
                float across = Vector3.Dot(hit, Right), up = hit.y;
                return Ring > 0f ? across * across + up * up <= Ring * Ring : Mathf.Abs(across) <= RingW && Mathf.Abs(up) <= RingH;
            }
            return Mathf.Abs(Vector3.Dot(hit, Right)) <= HalfWidth + reach && hit.y >= -Below && hit.y <= Above;
        }
    }

    // An official shortcut, resolved for the direction the race runs.
    public sealed class RaceShortcut
    {
        public string Id, Name;
        public Vector3[] Path;                // entry to exit, in travel order
        public float AiSpeed;                 // Round 18
        public float[] AiProfile;             // Round 18 (station, speed pairs along Path)
        public float AiTake, AiSlip;          // Round 20 follow-ups (RouteShortcutJson)
        public float[] AiSlipProfile;
        public RaceGate Entry;                // directed plane across the path a little way in; crossing it takes the shortcut
        public RaceGate Exit;                 // plane across the path's last metres (ends the "on the shortcut" state)
        public int FirstForgiven, LastForgiven;   // indices into RaceRoute.Checkpoints, inclusive
        // Round 13, item 3: the shortcut's corridor (flat segments of its path from the entry plane to the exit, where the path is
        // CorridorOffLap or more from the lap's centreline) and its half width. A car anywhere in it is on the shortcut, however
        // it got there (Dan was charged for CP2 on all three laps: his line round house 1763 passed outside #2's entry plane)
        public List<(Vector2 a, Vector2 b)> Corridor = new List<(Vector2, Vector2)>();
        public float CorridorHalf;
        public bool InCorridor(Vector3 p)
        {
            var q = new Vector2(p.x, p.z); float r2 = CorridorHalf * CorridorHalf;
            foreach (var (a, b) in Corridor)
            {
                var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                if ((a + ab * t - q).sqrMagnitude <= r2) return true;
            }
            return false;
        }
        public Vector3 Start => Path[0];
        public Vector3 End => Path[Path.Length - 1];
    }

    public sealed class RaceRoute
    {
        public string Name, Direction;
        public string RecordName;   // Round 54 (A1): RouteJson.records_as, or Name
        public bool Reverse;
        public bool OwnReverse;             // Round 39: the reverse runs the route's own reverse lap (loop_rev), not the loop backward
        public int Laps;
        public float LapLength, GateS;     // GateS: where the gate is on the path, from the path start
        public Vector3[] Path;              // closed, in travel order (last point = first point)
        public RaceGate Gate;
        public List<RaceGate> Checkpoints = new List<RaceGate>();
        public Vector3 GridPosition;
        public float GridYaw;
        public float RunInLength;           // grid to the gate, metres
        public List<string> Junctions = new List<string>();
        public List<RaceShortcut> Shortcuts = new List<RaceShortcut>();
        public List<RouteClosureJson> Closures = new List<RouteClosureJson>();
        float[] cum;

        public static RaceRoute Create(RouteJson j, string directionOverride = null)
        {
            string dir = string.IsNullOrEmpty(directionOverride) ? j.direction : directionOverride;
            if (dir != "forward" && dir != "reverse") throw new Exception($"Route direction must be \"forward\" or \"reverse\", not \"{dir}\"");
            var r = new RaceRoute { Name = j.name, RecordName = string.IsNullOrEmpty(j.records_as) ? j.name : j.records_as, Direction = dir, Reverse = dir == "reverse", Laps = Mathf.Max(1, j.laps), LapLength = j.lap_length_m };
            r.OwnReverse = r.Reverse && j.OwnReverse;   // Round 39
            var loop = r.OwnReverse ? j.loop_rev : j.loop;
            int n = loop.Length / 2;
            var pts = new List<Vector3>(n);
            var ly = r.OwnReverse ? j.loop_rev_y : j.loop_y;   // Round 62: the reverse lap's own heights too
            bool hasY = ly != null && ly.Length == n;   // Round 27: the underground race's own heights
            var own = new List<bool>(n);
            for (int i = 0; i < n; i++)
            {
                bool real = hasY && ly[i] > RouteJson.CodeGround + 1f;   // Round 62: a code for the ground there is the ground's
                pts.Add(real ? new Vector3(loop[2 * i], ly[i], loop[2 * i + 1]) : Ground(loop[2 * i], loop[2 * i + 1])); own.Add(real);
            }
            if (r.Reverse && !r.OwnReverse) own.Reverse();
            r.OwnY = own.Contains(true) && !j.underground && !j.air ? own.ToArray() : null;
            r.Underground = j.underground; r.Air = j.air; r.Data = j;
            if (r.Reverse && !r.OwnReverse) pts.Reverse();
            r.Path = pts.ToArray();
            r.cum = new float[n];
            for (int i = 1; i < n; i++) r.cum[i] = r.cum[i - 1] + Flat(r.Path[i] - r.Path[i - 1]).magnitude;
            r.LapLength = r.cum[n - 1];
            float L = r.LapLength;

            float gateS = r.OwnReverse ? j.gate_s_rev : r.Reverse ? L - j.gate_s : j.gate_s;
            r.GateS = gateS;
            r.Gate = r.MakeGate(0, "Start / finish", gateS, j.gate_half_width_m);
            if (j.gate_below_m > 0f) r.Gate.Below = j.gate_below_m;
            if (j.gate_above_m > 0f) r.Gate.Above = j.gate_above_m;
            if (j.air) { r.Gate.Ring = j.gate_ring_air_m; r.Gate.Position.y = j.gate_y; }   // Round 71
            var order = new List<(float s, RouteCheckpointJson c)>();
            foreach (var c in r.OwnReverse ? j.checkpoints_rev : j.checkpoints) order.Add((r.OwnReverse ? c.s : r.Reverse ? L - c.s : c.s, c));
            order.Sort((x, y) => Mathf.Repeat(x.s - gateS, L).CompareTo(Mathf.Repeat(y.s - gateS, L)));
            for (int i = 0; i < order.Count; i++)
            {
                var g = r.MakeGate(i + 1, order[i].c.label, order[i].s, order[i].c.half_m > 0f ? order[i].c.half_m : j.checkpoint_half_width_m);   // Round 27: half_m
                g.S = Mathf.Repeat(order[i].s - gateS, L);
                g.DataId = order[i].c.id; g.Arch = order[i].c.arch_m;
                g.Below = r.Gate.Below; g.Above = r.Gate.Above;
                if (j.air) { g.Ring = order[i].c.ring_m; g.RingW = order[i].c.ring_w; g.RingH = order[i].c.ring_h; g.Position.y = order[i].c.y; g.EitherWay = true; }   // Round 71; Round 73 (B3)
                r.Checkpoints.Add(g);
            }
            foreach (var jn in (r.OwnReverse ? j.junctions_rev : j.junctions) ?? new RouteJunctionJson[0]) r.Junctions.Add($"{Mathf.Repeat((r.OwnReverse ? jn.s : r.Reverse ? L - jn.s : jn.s) - gateS, L):0} m: {string.Join(" / ", jn.roads)}");
            r.BuildShortcuts(j);
            var closures = r.OwnReverse ? j.closures_rev : j.closures; if (closures != null) r.Closures.AddRange(closures);   // Round 39: the reverse lap's own

            if (!r.Reverse)
            {
                // forward: the grid is the map's spawn point on the entrance lane, and the car drives the lead-in to the loop
                float lx = j.lead_in[0], lz = j.lead_in[1], nx = j.lead_in[2], nz = j.lead_in[3];
                r.GridPosition = j.lead_in_y != null && j.lead_in_y.Length > 0 ? new Vector3(lx, j.lead_in_y[0], lz) : Ground(lx, lz);   // Round 27
                if (j.lead_in_y != null && j.lead_in_y.Length * 2 == j.lead_in.Length)   // Round 28: the lead-in's own heights (the grid's ground, Surface)
                {
                    r.LeadIn = new Vector3[j.lead_in_y.Length];
                    for (int i = 0; i < r.LeadIn.Length; i++) r.LeadIn[i] = new Vector3(j.lead_in[2 * i], j.lead_in_y[i], j.lead_in[2 * i + 1]);
                }
                r.GridYaw = Mathf.Atan2(nx - lx, nz - lz) * Mathf.Rad2Deg;
                r.RunInLength = j.lead_in_length_m + gateS;
            }
            else
            {
                // reverse: no lead-in; the grid is on the loop, a little before the gate in the travel direction
                float s = Mathf.Repeat(gateS - j.reverse_grid_back_m, L);
                r.GridPosition = r.PointAt(s, out var f);
                r.GridYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                r.RunInLength = j.reverse_grid_back_m;
            }
            return r;
        }

        // Shortcuts that apply to this direction, with their entry / exit planes and the checkpoint indices they forgive.
        void BuildShortcuts(RouteJson j)
        {
            if (j.official_shortcuts == null) return;
            foreach (var sj in j.official_shortcuts)
            {
                string d = string.IsNullOrEmpty(sj.direction) ? "forward" : sj.direction;
                if (d != "both" && d != Direction) continue;
                if (sj.path == null || sj.path.Length < 4) continue;
                int n = sj.path.Length / 2;
                var pts = new List<Vector3>(n);
                bool py = sj.path_y != null && sj.path_y.Length == n;   // Round 27: the underground race's own heights
                for (int i = 0; i < n; i++) { if (py) { pts.Add(new Vector3(sj.path[2 * i], sj.path_y[i], sj.path[2 * i + 1])); continue; } var g = Ground(sj.path[2 * i], sj.path[2 * i + 1]); g.y = DrainPipe.LineHeight(g.x, g.z, g.y); pts.Add(g); }   // Round 18: through a pipe, its floor
                if (Reverse && d == "both") pts.Reverse();
                var sc = new RaceShortcut { Id = sj.id, Name = string.IsNullOrEmpty(sj.name) ? sj.id : sj.name, Path = pts.ToArray(), AiSpeed = sj.ai_speed, AiProfile = sj.ai_profile, AiTake = sj.ai_take, AiSlip = sj.ai_slip, AiSlipProfile = sj.ai_slip_profile };
                var c = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) c[i] = c[i - 1] + Flat(pts[i] - pts[i - 1]).magnitude;
                float len = c[c.Length - 1];
                float inset = Mathf.Min(sj.entry_inset_m > 0f ? sj.entry_inset_m : 6f, len * 0.5f);
                float half = (sj.half_width_m > 0f ? sj.half_width_m : 6f) + (sj.entry_margin_m > 0f ? sj.entry_margin_m : 4f);
                sc.Entry = PlaneOn(pts, c, inset, half);
                sc.Exit = PlaneOn(pts, c, Mathf.Max(inset, len - 3f), half);
                sc.CorridorHalf = Mathf.Max(half, CorridorHalfMin);
                for (int i = 0; i + 1 < pts.Count; i++)
                    if (c[i] >= inset && c[i + 1] <= len - 3f && DistToLap(pts[i]) >= CorridorOffLap && DistToLap(pts[i + 1]) >= CorridorOffLap)
                        sc.Corridor.Add((new Vector2(pts[i].x, pts[i].z), new Vector2(pts[i + 1].x, pts[i + 1].z)));
                sc.FirstForgiven = int.MaxValue; sc.LastForgiven = -1;
                if (sj.forgives != null)
                    foreach (int id in sj.forgives)
                    {
                        int idx = Checkpoints.FindIndex(g => g.DataId == id);
                        if (idx < 0) continue;
                        sc.FirstForgiven = Mathf.Min(sc.FirstForgiven, idx); sc.LastForgiven = Mathf.Max(sc.LastForgiven, idx);
                    }
                if (sc.LastForgiven < 0 && !sj.lap_option) continue;   // a lap option forgives nothing: its planes and corridor never fire
                Shortcuts.Add(sc);
            }
        }

        // Round 13, item 3: the corridor is 7 m either side of the shortcut's line (at least the entry plane's half width),
        // only where the line is 12 m or more from the lap's centreline, so a car on the lane is never in it
        const float CorridorHalfMin = 7f, CorridorOffLap = 12f;
        float DistToLap(Vector3 p)
        {
            float best = float.MaxValue; var q = new Vector2(p.x, p.z);
            for (int i = 0; i + 1 < Path.Length; i++)
            {
                var a = new Vector2(Path[i].x, Path[i].z); var ab = new Vector2(Path[i + 1].x, Path[i + 1].z) - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - q).magnitude);
            }
            return best;
        }

        static RaceGate PlaneOn(List<Vector3> pts, float[] c, float s, float half)
        {
            int i = 1; while (i < c.Length - 1 && c[i] < s) i++;
            Vector3 p = Vector3.Lerp(pts[i - 1], pts[i], Mathf.InverseLerp(c[i - 1], c[i], s));
            Vector3 f = Flat(pts[i] - pts[i - 1]).normalized;
            return new RaceGate { Position = p, Forward = f, Right = Vector3.Cross(Vector3.up, f).normalized, HalfWidth = half };
        }

        RaceGate MakeGate(int id, string label, float s, float halfWidth)
        {
            Vector3 p = PointAt(s, out var f);
            return new RaceGate { Id = id, Label = label, Position = p, Forward = f, Right = Vector3.Cross(Vector3.up, f).normalized, HalfWidth = halfWidth, S = 0f };
        }

        // Point on the lap at distance s (wraps), with the horizontal travel direction there.
        public Vector3 PointAt(float s, out Vector3 forward)
        {
            float L = LapLength;
            Vector3 p = Sample(Mathf.Repeat(s, L));
            Vector3 a = Sample(Mathf.Repeat(s - 3f, L)), b = Sample(Mathf.Repeat(s + 3f, L));
            forward = Flat(b - a).normalized;
            return p;
        }

        // Points along the lap from s0 to s1 (distances in the lap's own coordinates; they may run past one lap), for checks and guidance.
        public List<Vector3> SamplePath(float s0, float s1, float step)
        {
            var list = new List<Vector3>();
            for (float s = s0; s < s1; s += step) list.Add(PointAt(s, out _));
            list.Add(PointAt(s1, out _));
            return list;
        }

        Vector3 Sample(float s)
        {
            int lo = 0, hi = cum.Length - 1;
            while (lo + 1 < hi) { int mid = (lo + hi) / 2; if (cum[mid] <= s) lo = mid; else hi = mid; }
            float t = Mathf.InverseLerp(cum[lo], cum[lo + 1], s);
            return Vector3.Lerp(Path[lo], Path[lo + 1], t);
        }

        // Round 10 (checks): the lap distance from the gate, in travel order, of the lap point nearest to p (Round 28: on an underground
        // route, where the lap passes over itself, height counts too: VerticalWeight)
        public float LapStationOf(Vector3 p)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < Path.Length; i++)
            {
                Vector3 a = Flat(Path[i]), b = Flat(Path[i + 1]), ab = b - a, q = Flat(p);
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (Underground || Air) { float dy = Mathf.Lerp(Path[i].y, Path[i + 1].y, t) - p.y; d += VerticalWeight * dy * dy; }
                if (d < best) { best = d; bs = cum[i] + t * (cum[i + 1] - cum[i]); }
            }
            return Mathf.Repeat(bs - GateS, LapLength);
        }

        public static Vector3 Ground(float x, float z) { float y = GroundQuery.Height(x, z); return new Vector3(x, float.IsNaN(y) ? 0f : y, z); }

        // Round 27 (the underground race): while an underground route is raced (Active), the race's own idea of "the ground" there is the
        // route's height under the nearest point of its lap (within 30 m), not the terrain far over it: the grid, the AI's passing lane
        // and its fall-recovery, the resets and the barriers ask Surface. Any other time (and away from the lap) it is the terrain's.
        public bool Underground;
        public bool Air;   // Round 71: an air route (RouteJson.air): its line in the sky, its gates rings
        public bool[] OwnY;   // Round 62: the lap's points that have a height of their own (a mall's floors); Surface takes it near them
        public RouteJson Data;   // Round 27: the race layer's seals, railings and switch gates (RaceBarriers)
        public static RaceRoute Active;
        // Round 28: nearY (a height near the place asked about) picks the level where the route passes over itself (the span over the mid
        // ledge); the lead-in counts too (the grid on the top ledge)
        public static Vector3 Surface(float x, float z, float nearY = float.NaN)
        {
            var r = Active;
            if (r != null && !r.Underground && r.OwnY != null) return r.OwnSurface(x, z, nearY);   // Round 62
            if (r == null || !r.Underground) return Ground(x, z);
            var p = new Vector2(x, z); float best = 900f; float y = float.NaN;
            void Scan(Vector3[] path)
            {
                if (path == null) return;
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    Vector2 a = new Vector2(path[i].x, path[i].z), b = new Vector2(path[i + 1].x, path[i + 1].z), ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    float d = (a + ab * t - p).sqrMagnitude, yy = Mathf.Lerp(path[i].y, path[i + 1].y, t);
                    if (!float.IsNaN(nearY)) d += VerticalWeight * (yy - nearY) * (yy - nearY);
                    if (d < best) { best = d; y = yy; }
                }
            }
            Scan(r.Path); Scan(r.LeadIn);
            return float.IsNaN(y) ? Ground(x, z) : new Vector3(x, y, z);
        }
        // Round 62: within 6 m of a lap segment whose ends both have heights of their own, the lap's height there (the nearest such segment, height
        // counting when nearY is given); anywhere else the terrain's
        Vector3 OwnSurface(float x, float z, float nearY)
        {
            var p = new Vector2(x, z); float best = 36f; float y = float.NaN;
            for (int i = 0; i + 1 < Path.Length; i++)
            {
                if (!OwnY[i] || !OwnY[i + 1]) continue;
                Vector2 a = new Vector2(Path[i].x, Path[i].z), b = new Vector2(Path[i + 1].x, Path[i + 1].z), ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                float d = (a + ab * t - p).sqrMagnitude; if (d > 36f) continue;
                float yy = Mathf.Lerp(Path[i].y, Path[i + 1].y, t);
                if (!float.IsNaN(nearY)) d += VerticalWeight * (yy - nearY) * (yy - nearY);
                if (d < best) { best = d; y = yy; }
            }
            return float.IsNaN(y) ? Ground(x, z) : new Vector3(x, y, z);
        }
        public const float VerticalWeight = 4f;   // Round 28: a meter up or down counts as 2 m across
        public Vector3[] LeadIn;

        // Round 28 (B4, Dan: "Reset button during the race leaves me turned in the wrong direction"): the nearest point (height counting) of
        // the lap or of a shortcut's line to p, the way the race goes there, and the line it is on (the lap: null)
        public Vector3 NearestOnRoute(Vector3 p, out Vector3 forward, out RaceShortcut on, out float along)
        {
            float best = float.MaxValue; var at = p; Vector3 fw = Vector3.forward; RaceShortcut onSc = null; float al = 0f;
            void Scan(Vector3[] path, RaceShortcut sc)
            {
                float acc = 0f;
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    Vector3 a = path[i], b = path[i + 1], ab = Flat(b - a); float L = ab.magnitude;
                    float t = L < 1e-4f ? 0f : Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / (L * L));
                    var q = Vector3.Lerp(a, b, t); float dy = q.y - p.y;
                    float d = Flat(q - p).sqrMagnitude + VerticalWeight * dy * dy;
                    if (d < best && L > 1e-4f) { best = d; at = q; fw = ab / L; onSc = sc; al = acc + t * L; }
                    acc += L;
                }
            }
            Scan(Path, null);
            foreach (var sc in Shortcuts) Scan(sc.Path, sc);
            forward = fw; on = onSc; along = al;
            return at;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
