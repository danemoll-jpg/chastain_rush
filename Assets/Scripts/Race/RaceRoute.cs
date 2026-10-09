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
    // (RaceProgress). Racer's penalties, branches and AI are not used.
    [Serializable] public class RouteCheckpointJson { public int id; public float s; public float[] pos; public string label; }
    [Serializable] public class RoutePieceJson { public long osm_id; public string name; public float length_m; public bool reverse; }
    [Serializable] public class RouteJunctionJson { public float s; public string[] roads; public float[] pos; }
    [Serializable]
    public class RouteJson
    {
        public string name, direction;
        public int laps;
        public float lap_length_m, lead_in_length_m, gate_s, gate_half_width_m, checkpoint_half_width_m, reverse_grid_back_m;
        public float[] loop, lead_in;
        public RouteCheckpointJson[] checkpoints;
        public RoutePieceJson[] loop_pieces;
        public RouteJunctionJson[] junctions;
    }

    public sealed class RaceGate
    {
        public int Id;               // 0 = start / finish, 1.. = checkpoints in travel order
        public string Label;
        public Vector3 Position;     // on the ground, in the middle of the road
        public Vector3 Forward;      // horizontal travel direction through the gate
        public Vector3 Right;
        public float HalfWidth, S;   // S: distance along the lap in travel order from the lap's own start

        public const float Below = 4f, Above = 10f;

        // True when the step from -> to passes through the gate's opening in the travel direction; fraction is how far
        // along the step it happened (so lap times are not rounded to physics steps).
        public bool Crossed(Vector3 from, Vector3 to, out float fraction)
        {
            fraction = 0f;
            Vector3 a = from - Position, b = to - Position;
            float za = Vector3.Dot(a, Forward), zb = Vector3.Dot(b, Forward);
            if (!(za < 0f && zb >= 0f)) return false;
            fraction = -za / (zb - za);
            Vector3 hit = Vector3.Lerp(a, b, fraction);
            return Mathf.Abs(Vector3.Dot(hit, Right)) <= HalfWidth && hit.y >= -Below && hit.y <= Above;
        }
    }

    public sealed class RaceRoute
    {
        public string Name, Direction;
        public bool Reverse;
        public int Laps;
        public float LapLength, GateS;     // GateS: where the gate is on the path, from the path start
        public Vector3[] Path;              // closed, in travel order (last point = first point)
        public RaceGate Gate;
        public List<RaceGate> Checkpoints = new List<RaceGate>();
        public Vector3 GridPosition;
        public float GridYaw;
        public float RunInLength;           // grid to the gate, metres
        public List<string> Junctions = new List<string>();
        float[] cum;

        public static RaceRoute Create(RouteJson j, string directionOverride = null)
        {
            string dir = string.IsNullOrEmpty(directionOverride) ? j.direction : directionOverride;
            if (dir != "forward" && dir != "reverse") throw new Exception($"Route direction must be \"forward\" or \"reverse\", not \"{dir}\"");
            var r = new RaceRoute { Name = j.name, Direction = dir, Reverse = dir == "reverse", Laps = Mathf.Max(1, j.laps), LapLength = j.lap_length_m };
            int n = j.loop.Length / 2;
            var pts = new List<Vector3>(n);
            for (int i = 0; i < n; i++) pts.Add(Ground(j.loop[2 * i], j.loop[2 * i + 1]));
            if (r.Reverse) pts.Reverse();
            r.Path = pts.ToArray();
            r.cum = new float[n];
            for (int i = 1; i < n; i++) r.cum[i] = r.cum[i - 1] + Flat(r.Path[i] - r.Path[i - 1]).magnitude;
            r.LapLength = r.cum[n - 1];
            float L = r.LapLength;

            float gateS = r.Reverse ? L - j.gate_s : j.gate_s;
            r.GateS = gateS;
            r.Gate = r.MakeGate(0, "Start / finish", gateS, j.gate_half_width_m);
            var order = new List<(float s, RouteCheckpointJson c)>();
            foreach (var c in j.checkpoints) order.Add((r.Reverse ? L - c.s : c.s, c));
            order.Sort((x, y) => Mathf.Repeat(x.s - gateS, L).CompareTo(Mathf.Repeat(y.s - gateS, L)));
            for (int i = 0; i < order.Count; i++)
            {
                var g = r.MakeGate(i + 1, order[i].c.label, order[i].s, j.checkpoint_half_width_m);
                g.S = Mathf.Repeat(order[i].s - gateS, L);
                r.Checkpoints.Add(g);
            }
            foreach (var jn in j.junctions) r.Junctions.Add($"{Mathf.Repeat((r.Reverse ? L - jn.s : jn.s) - gateS, L):0} m: {string.Join(" / ", jn.roads)}");

            if (!r.Reverse)
            {
                // forward: the grid is the map's spawn point on the entrance lane, and the car drives the lead-in to the loop
                float lx = j.lead_in[0], lz = j.lead_in[1], nx = j.lead_in[2], nz = j.lead_in[3];
                r.GridPosition = Ground(lx, lz);
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

        static Vector3 Ground(float x, float z) { float y = GroundQuery.Height(x, z); return new Vector3(x, float.IsNaN(y) ? 0f : y, z); }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
