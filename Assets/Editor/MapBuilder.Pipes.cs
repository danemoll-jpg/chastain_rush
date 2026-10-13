using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 18, item 4: drive-through drainage pipes, from Data/pipes/pipes.json (hand-made layout). Pipes are their own layer
// (Dan's question, answered in TODO.md): the ground is a height field, so it cannot hold a tunnel, but a pipe can be its own
// mesh under it. The terrain is not raised or heaped anywhere; it gets HOLES over the two cuts (Unity terrain holes, which
// the TerrainCollider honors) and nothing else changes: between the cuts the ground, its trees and water carry on over the top.
//   floor     straight from a to b. From the ground at each end it runs down at 'cut_slope' (eased in over the first 6 m) to
//             the depth where the pipe's crown has 'cover_m' of ground over it all the way along the middle; the two slopes
//             and the level meet in rounded dips (a Gaussian of 2.5 m)
//   cuts      from each end to where the ground first covers the crown (the headwall): a concrete-lined trench 5 m wide
//             between walls, its floor a slab, a coping along the wall tops over the hole's ragged edge, a steel railing
//   headwall  a concrete wall across the cut's inner end with the pipe's round opening
//   pipe      round, 'diameter_m' inside, concrete, lamps along the crown. Inside faces only (nobody sees its outside).
//             Round 19 (BUG-002, Dan: "I want the inside to look a little more used and dark, have a small stream of water in it.
//             Also can we have the ability so that you could drive around the walls?"), the standard for every pipe from now on:
//             a TRUE ROUND pipe to drive on (no flat floor: its invert is the floor line; DrainPipe holds a fast vehicle to the
//             wall), its last 6 m of each cut turning from a flat slab into the pipe's own round bottom (no lip at the headwall),
//             stained concrete (a texture made here: section joints, streaks from the crown, grime and moss low down, a tide
//             line, the wet invert), lamps every 16 m and dim (the vehicles' own headlights come on in a pipe: VehicleLights),
//             a thin stream down the invert (PipeStream: moving water, a trickle, splashes; it never slows anyone)
//   rock bar  Round 19 (BUG-001, Dan: "Can we make it so the water doesnt slow you down for the shortcut...maybe put some rocks here
//             or something?"): 'rock_bar_a' makes the way to the inlet cross the creek on a bar of flat creek rocks just out of the
//             water (6 m wide, its top 0.12 m over the surface, water either side; a DryPatch so it does not count as wading)
// Everything is solid (mesh colliders, frictionless to a body like the shortcut ramps), the pipe's inside faces point inward,
// so the chase camera's obstruction cast stops at the pipe's wall and the camera stays in the pipe. DrainPipe (runtime) puts a
// reset inside it back on its floor, and gives race lines through it the floor's heights.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class PipeJson { public string id, name; public float[] a, b, approach_a, approach_b; public float diameter_m = 5f, cover_m = 0.6f, cut_slope = 0.18f; public bool rock_bar_a, mouth_lamp_b, twin, open_b, flow_in, glen_culvert, outfall; public float b_floor; public PipeClearJson clear; }   // Round 21 follow-ups: 'clear' (MapBuilder.ClearPipe.cs); Round 23: 'twin'; Round 23 follow-ups: 'open_b', 'flow_in', 'glen_culvert', 'outfall'
        // Round 23 (BUG-002, Dan: "two large pipes. I want this to be a main entrance the pipe network"): 'twin' makes two barrels side by
        // side (a twin culvert), their centers R + 0.4 m either side of the line, one cut twice as wide with one headwall holding both
        // openings (an 0.8 m pier between them), one stream and one DrainPipe per barrel; and its floor never rises above the lower
        // mouth (a culvert under a road embankment: the floor runs level at the stream's bed, the road high over it).
        // Round 23, items 2b and 2c (Dan: "this was supposed to be the entrance into the pipe network not just two pipes passing under
        // the road"): the pipe ended at b in an underground chamber, J1. Round 23 follow-ups (Dan, BUG-006: "I envision that those two
        // pipes go off in different directions not dump into a big open room right away"): 'open_b' ends the pipe at b with no cut, no
        // headwall and no holes there, its floor at b 'b_floor' (and never below it: no dip under the ground between), and each
        // barrel goes on from b as a pipe of the network (MapBuilder.Network.cs); J1 as a room at BF's end is gone. The network's
        // exits and outfalls are 'open_b' pipes too: a mouth and its cut at a, the network's tube going on from b
        [Serializable] class PipeFile { public PipeJson[] pipes; }
        const string PipeFilePath = "Data/pipes/pipes.json";
        const float PipeFloorRise = 0f, PipeWall = 0.3f, CutHalf = 2.5f, CutWall = 0.35f, CopingOut = 0.95f, PipeStep = 0.5f, Flare = 5f, FlareW = 1.75f;   // Round 19: no flat floor (1.0 m over the invert before)
        const float UTransition = 6f, BarTopOverWater = 0.12f, BarHalf = 3f;   // Round 19

        sealed class PipePlan
        {
            public PipeJson J; public Vector2 A, B, U, Nl; public float L;
            public float[] Floor;        // floor height at each PipeStep station
            public float S1, S2;         // the headwalls (cut 1 is 0..S1, cut 2 is S2..L)
            public List<Vector2> Bar = new List<Vector2>(); public float BarTop;   // Round 19: P1's rock bar (the approach points over the creek)
            public bool Clear; public float C0, C1, Surface; public List<Vector2> Pool; public int PoolRing = -1;   // Round 21 follow-ups: the clear stretch and its pool (MapBuilder.ClearPipe.cs)
            public bool HasFlow; public Vector2 FlowFrom, FlowDir; public float FlowSurf;   // Round 23 follow-ups (BUG-005): where the creek's own water ends, and its way (downstream)
            // Round 23: a twin's barrels sit O either side of the line, its cut is Half wide either side (CutHalf for one barrel)
            public float O => J.twin ? R + 0.4f : 0f;
            public float Half => J.twin ? O + R : CutHalf;
            public float[] Lats => J.twin ? new[] { -O, O } : new[] { 0f };
            // Round 19: the cut's floor across: flat, turning over the last UTransition m before each headwall into the pipe's round bottom
            // (Round 23: a twin's two round bottoms meet at the pier in a ridge that rises over the same 6 m)
            public float FloorAt(float s, float lat)
            {
                float ut = J.twin ? Mathf.Clamp(S1 - 0.5f, UTransition, 10f) : UTransition;   // Round 23: a twin's pier nose rises over 10 m (it steers a vehicle into a barrel; over 6 m it stopped them)
                float m = Mathf.Max(Smooth01(1f - (S1 - s) / ut) * (s <= S1 ? 1f : 0f), Smooth01(1f - (s - S2) / ut) * (s >= S2 ? 1f : 0f));
                if (s > S1 && s < S2) m = 1f;
                float x = Mathf.Min(Mathf.Abs(Mathf.Abs(lat) - O), R * 0.999f);
                return Y(s) + m * (R - Mathf.Sqrt(R * R - x * x));
            }
            public float R => J.diameter_m * 0.5f;
            public float Y(float s) { float f = Mathf.Clamp(s / PipeStep, 0f, Floor.Length - 1.001f); int i = (int)f; return Mathf.Lerp(Floor[i], Floor[i + 1], f - i); }
            public Vector2 P(float s, float lat = 0f) => A + U * s + Nl * lat;
            // the cut's half width: 2.5 m, flaring out to 4.25 m over the last 5 m at each mouth (wingwalls: a vehicle a little off
            // the line is led in, not stopped by a wall's end)
            public float HalfAt(float s) => Half + FlareW * Mathf.Max(Smooth01(1f - s / Flare), J.open_b ? 0f : Smooth01(1f - (L - s) / Flare));
            public bool InCut(Vector2 q, float pad, out float s, out float lat)
            {
                s = Vector2.Dot(q - A, U); lat = Vector2.Dot(q - A, Nl);
                return Mathf.Abs(lat) <= HalfAt(Mathf.Clamp(s, 0f, L)) + CutWall + pad && ((s >= -pad && s <= S1 + 0.3f + pad) || (!J.open_b && s >= S2 - 0.3f - pad && s <= L + pad));   // Round 23: no cut at a chamber end
            }
        }
        sealed class Pipes
        {
            public readonly List<PipePlan> Plans = new List<PipePlan>();
            public int TreesRemoved;
            public bool RejectTree(float x, float z)
            {
                foreach (var p in Plans) if (p.InCut(new Vector2(x, z), 1.5f, out _, out _)) { TreesRemoved++; return true; }
                return false;
            }
        }

        // planned and the holes cut before anything samples the ground (the heights are not changed)
        // Round 23 follow-ups: the Chastain Glen pond's culvert is a network entrance (GP, 'glen_culvert'): the pond's old small headwall
        // and its bank (MapBuilder.Pond.cs) are not built
        static bool GlenCulvertPiped()
        {
            string path = Path.Combine(ProjectRoot, PipeFilePath);
            return File.Exists(path) && JsonUtility.FromJson<PipeFile>(File.ReadAllText(path)).pipes.Any(j => j.glen_culvert);
        }

        static Pipes PlanPipes(TerrainGrid terrain, IEnumerable<PipeJson> extra = null)   // Round 23 follow-ups: 'extra', the network's exits (MapBuilder.Network.cs)
        {
            var result = new Pipes();
            string path = Path.Combine(ProjectRoot, PipeFilePath);
            var all = new List<PipeJson>();
            if (File.Exists(path)) all.AddRange(JsonUtility.FromJson<PipeFile>(File.ReadAllText(path)).pipes);
            if (extra != null) all.AddRange(extra);
            foreach (var j in all)
            {
                var p = new PipePlan { J = j, A = new Vector2(j.a[0], j.a[1]), B = new Vector2(j.b[0], j.b[1]) };
                p.L = Vector2.Distance(p.A, p.B); p.U = (p.B - p.A) / p.L; p.Nl = new Vector2(-p.U.y, p.U.x);
                // the ground leading to each mouth first (the mouths' heights are taken from it)
                if (j.approach_a != null && j.approach_a.Length >= 2) { var l = ToPoints(j.approach_a); l.Add(p.A + p.U * 0.5f); GradeApproach(terrain, l, j.id + " inlet", j.rock_bar_a ? p : null); }
                if (j.approach_b != null && j.approach_b.Length >= 2) { var l = ToPoints(j.approach_b); l.Insert(0, p.B - p.U * 0.5f); GradeApproach(terrain, l, j.id + " outlet", null); }
                int n = Mathf.CeilToInt(p.L / PipeStep) + 1;
                float crownOut = j.diameter_m - PipeFloorRise + PipeWall;   // floor to the top of the pipe's outside
                float yA = Ground(terrain, p.A.x, p.A.y), yB = j.open_b ? j.b_floor : Ground(terrain, p.B.x, p.B.y);   // Round 23: a chamber end's floor is given
                const float Ease = 6f;
                float Ramp(float s) => s < Ease ? s * s / (2f * Ease) : s - Ease * 0.5f;
                float[] FloorFrom(float yLow)
                {
                    var raw = new float[n];
                    for (int i = 0; i < n; i++) { float s = Mathf.Min(i * PipeStep, p.L); raw[i] = Mathf.Max(yLow, Mathf.Max(yA - j.cut_slope * Ramp(s), yB - j.cut_slope * Ramp(p.L - s))); }
                    // round the dips (sigma 2.5 m), keeping the ends on the ground
                    var ff = new float[n]; int r = Mathf.CeilToInt(7.5f / PipeStep);
                    for (int i = 0; i < n; i++)
                    {
                        float acc = 0f, w = 0f;
                        for (int k = -r; k <= r; k++) { int ii = Mathf.Clamp(i + k, 0, n - 1); float ww = Mathf.Exp(-0.5f * (k * PipeStep / 2.5f) * (k * PipeStep / 2.5f)); acc += raw[ii] * ww; w += ww; }
                        float sm = acc / w, s = i * PipeStep, edge = Smooth01(Mathf.Min(s, j.open_b ? float.MaxValue : p.L - s) / 8f);
                        ff[i] = Mathf.Lerp(raw[i], Mathf.Min(raw[i], sm), edge);
                    }
                    return ff;
                }
                // Round 21 follow-ups: a clear stretch under water (MapBuilder.ClearPipe.cs): the floor held down so the crown is
                // 'water_over_m' under the small pond's surface, and the pool dug over it before the ground over the pipe is measured
                float forced = float.MaxValue;
                if (HasClear(j))
                {
                    p.Surface = PondSurfaceNear(p.B + p.U * 12f, out _);
                    if (float.IsNaN(p.Surface)) Debug.LogError($"[Round21+] pipe {j.id}: no pond past its outlet: no clear stretch");
                    else
                    {
                        forced = p.Surface - j.clear.water_over_m - crownOut;
                        var f0 = FloorFrom(forced); ClearStretch(p, f0);
                        if (p.C0 < 0f || p.C1 - p.C0 < 8f) { Debug.LogError($"[Round21+] pipe {j.id}: the crown is never {ClearCrownUnder} m under the surface for 8 m: no clear stretch"); forced = float.MaxValue; }
                        else { p.Clear = true; DigPool(terrain, p, f0); }
                    }
                }
                // the ground over the pipe: the lowest across its outside width, at each station
                var cover = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float s = Mathf.Min(i * PipeStep, p.L), lo = float.MaxValue;
                    for (float lat = -p.O - p.R - PipeWall; lat <= p.O + p.R + PipeWall + 1e-3f; lat += 0.5f) { var q = p.P(s, lat); lo = Mathf.Min(lo, Ground(terrain, q.x, q.y)); }
                    cover[i] = lo;
                }
                int m0 = Mathf.RoundToInt(n * 0.3f), m1 = Mathf.RoundToInt(n * 0.7f);
                float yLow0 = float.MaxValue; for (int i = m0; i <= m1; i++) yLow0 = Mathf.Min(yLow0, cover[i] - j.cover_m - crownOut);
                if (j.twin) yLow0 = Mathf.Min(yLow0, Mathf.Min(yA, yB));   // Round 23: level at the lower mouth under the embankment, never a hump
                if (j.open_b) yLow0 = Mathf.Min(yA, yB);                   // Round 23 follow-ups: down from the mouth to b's level and level on to b (no dip)
                var f = FloorFrom(Mathf.Min(yLow0, forced));
                p.Floor = f;
                // the headwalls: the longest run in the middle where the ground covers the crown by 0.4 m or more
                int bestA = -1, bestB = -1, runA = -1;
                for (int i = 0; i < n; i++)
                {
                    bool ok = cover[i] - (f[i] + crownOut) >= 0.4f || (p.Clear && i * PipeStep >= p.C0 - 1e-3f && i * PipeStep <= p.C1 + 1e-3f);   // Round 21 follow-ups: the clear stretch is under water, not ground
                    if (ok && runA < 0) runA = i;
                    if ((!ok || i == n - 1) && runA >= 0) { int e = ok ? i : i - 1; if (e - runA > bestB - bestA) { bestA = runA; bestB = e; } runA = -1; }
                }
                if ((bestA < 0 || (bestB - bestA) * PipeStep < 8f) && j.open_b)
                {   // Round 23 follow-ups: a network exit on low ground (the launch over the big lake): an open chute all the way to b, its headwall
                    // at b where the network's pipe comes out
                    bestA = bestB = n - 1;
                    Debug.Log($"[Round23+] pipe {j.id}: the ground never covers it: an open cut all the way, its headwall at b");
                }
                else if (bestA < 0 || (bestB - bestA) * PipeStep < 8f) { Debug.LogError($"[Round18] pipe {j.id}: the ground never covers it for 8 m (cover {j.cover_m} m): not built"); continue; }
                p.S1 = bestA * PipeStep; p.S2 = bestB * PipeStep;
                if (j.open_b)   // Round 23: the barrels run right to b, where the network's pipes go on
                {
                    if (bestB < n - 1) Debug.LogError($"[Round23] pipe {j.id}: the ground does not cover it all the way to b ({(n - 1 - bestB) * PipeStep:0.0} m short)");
                    p.S2 = p.L;
                }
                if (j.twin) { GradeTwinAprons(terrain, p); TrimStreamsAtCut(p); }   // Round 23
                if (j.open_b) f[f.Length - 1] = j.b_floor;   // Round 23 follow-ups: exactly the network's level where its pipe goes on
                result.Plans.Add(p);
                CutPipeHoles(terrain, p);
                if (p.Clear) CutTrenchHoles(terrain, p);
                Debug.Log($"[Round18] pipe {j.id}: {p.L:0.0} m from ({p.A.x:0.0}, {p.A.y:0.0}) at {yA:0.00} m to ({p.B.x:0.0}, {p.B.y:0.0}) at {yB:0.00} m; floor down to {f.Min():0.00} m ({j.cut_slope * 100f:0} % in and out); inlet cut {p.S1:0.0} m, pipe {p.S2 - p.S1:0.0} m (crown {j.cover_m:0.0} m or more under the ground), outlet cut {p.L - p.S2:0.0} m");
            }
            return result;
        }

        // the way to a mouth: a band 6 m wide along the line (blending out over 4 m more: gentle sides where it cuts a bank), the ground cut where it is steeper
        // than 22 % (down a creek bank), and across a stream a gravel bar 0.15 m under the water (Round 18: the vehicles stalled
        // in the creek's 0.5 m in front of P1's inlet), as the trail's fords (MapBuilder.CreekTrack.cs)
        static void GradeApproach(TerrainGrid terrain, List<Vector2> line, string what, PipePlan bar)
        {
            var pts = Densify(line, 0.5f); int n = pts.Count;
            var g = pts.Select(q => Ground(terrain, q.x, q.y)).ToArray(); var t = new float[n];
            for (int i = 0; i < n; i++) { float best = g[i]; for (int k = 0; k < n; k++) best = Mathf.Min(best, g[k] + TrailMaxGrade * 0.5f * Mathf.Abs(i - k)); t[i] = best; }
            for (int i = 0; i < n; i++)
                if (Beds != null && Beds.NearStream(pts[i], out float d, out float surf) && d < StreamHalf + 0.5f)
                {
                    if (bar == null) t[i] = Mathf.Max(t[i], surf - TrailShallow);
                    else { t[i] = Mathf.Max(t[i], surf + BarTopOverWater); bar.Bar.Add(pts[i]); bar.BarTop = Mathf.Max(bar.BarTop, surf + BarTopOverWater); }   // Round 19: the rock bar, just out of the water
                }
            if (bar != null && bar.Bar.Count > 0)   // its top level, and a smooth way up onto it and off it (22 % at most)
            {
                var onBar = pts.Select(q => bar.Bar.Contains(q)).ToArray();
                for (int i = 0; i < n; i++)
                {
                    if (onBar[i]) { t[i] = bar.BarTop; continue; }
                    for (int k = 0; k < n; k++) if (onBar[k]) t[i] = Mathf.Max(t[i], bar.BarTop - TrailMaxGrade * 0.5f * Mathf.Abs(i - k));
                }
            }
            float most = 0f;
            EditTerrain(terrain, BoundsOf(pts, 7.5f), (x, z, y) =>
            {
                var q = new Vector2(x, z); float best = float.MaxValue; int bi = 0; float bt = 0f;
                for (int i = 0; i + 1 < n; i++) { var ab = pts[i + 1] - pts[i]; float u = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - pts[i], ab) / ab.sqrMagnitude); float dd = (pts[i] + ab * u - q).magnitude; if (dd < best) { best = dd; bi = i; bt = u; } }
                float w = 1f - Smooth01((best - (bar != null ? BarHalf : 3.0f)) / (bar != null ? 2.5f : 4f)); if (w <= 0f) return y;
                float ny = y + (Mathf.Lerp(t[bi], t[bi + 1], bt) - y) * w; most = Mathf.Max(most, Mathf.Abs(ny - y)); return ny;
            });
            Debug.Log($"[Round18] pipe {what}: the way to it graded over {(n - 1) * 0.5f:0} m (largest change {most:0.00} m)" + (bar != null ? $"; Round 19: a rock bar across the creek, {bar.Bar.Count * 0.5f:0.0} m of it over the water, its top at {bar.BarTop:0.00} m ({BarTopOverWater:0.00} m over the surface), {BarHalf * 2f:0} m wide" : ""));
        }

        // Round 23: a twin's cut is as wide as the creek channel and more, and its floor is level across while the ground beyond each
        // mouth is not (the banks either side of the stream stand higher): the ground in front of each mouth is cut down to the floor's
        // level across the mouth's whole width and blended back to the natural ground over 10 m out and 3 m to the sides, so the floor
        // meets the ground with no step anywhere across it (Round 23's exe check: a slow car stopped against a 0.7 m step at the west
        // mouth). Only ever lowered (the creek's own bed, lower, is kept).
        static void GradeTwinAprons(TerrainGrid terrain, PipePlan p)
        {
            const float Out = 10f, Side = 3f;
            float most = 0f;
            foreach (var (s0, dir) in new[] { (0f, -1f), (p.L, 1f) })
            {
                if (dir > 0f && p.J.open_b) continue;   // no mouth at a chamber end
                float target = p.Y(s0) - 0.05f, half = p.HalfAt(s0);   // just under the floor, which reaches 0.8 m out over it
                var corners = new[] { p.P(s0 - dir * 2f, -half - Side), p.P(s0 - dir * 2f, half + Side), p.P(s0 + dir * Out, -half - Side), p.P(s0 + dir * Out, half + Side) };
                EditTerrain(terrain, BoundsOf(corners, 1f), (x, z, y) =>
                {
                    var q = new Vector2(x, z); float s = Vector2.Dot(q - p.A, p.U), lat = Mathf.Abs(Vector2.Dot(q - p.A, p.Nl));
                    float o = (s - s0) * dir; if (o < -2f || o > Out) return y;   // and 2 m into the cut: the terrain's last unholed cells there lie flat (no spikes up to the bank)
                    float w = (1f - Smooth01(o / Out)) * (1f - Smooth01((lat - half) / Side));
                    // Round 23 follow-ups: and where the ground in front is lower than the floor (GP's mouth over the pond's edge), it is
                    // filled up to a ramp that runs down from the floor's edge at 1 in 4 (no step up onto the floor from the water)
                    float ramp = target - Mathf.Max(0f, o - 0.8f) * 0.25f;
                    if (o > 0f && y < ramp && lat < half + 1f) { float ny2 = Mathf.Lerp(y, ramp, 1f - Smooth01((lat - half) / 1f)); most = Mathf.Max(most, ny2 - y); return ny2; }
                    if (w <= 0f || y <= target) return y;
                    float ny = y - (y - target) * w; most = Mathf.Max(most, y - ny); return ny;
                });
            }
            Debug.Log($"[Round23] pipe {p.J.id}: the ground in front of both mouths cut down to the floor across their width (largest change {most:0.00} m)");
        }

        // Round 23: the streams' water (its surface and its ShallowWater, drawn and registered later from Beds.Streams) stops at a twin's
        // mouths: none of it floats over the cut's floor or slows a vehicle inside the cut (the exe check: a blue sheet above the floor)
        static void TrimStreamsAtCut(PipePlan p)
        {
            if (Beds == null) return;
            int removed = 0;
            // Round 23 follow-ups (BUG-005, Dan: "why would the water stop here? Should be flowing"): with 'flow_in' the creek's flat water
            // also stops where the apron begins (10 m out); from there it is moving water down over the apron and into both barrels
            // (BuildFlowIn). The point where the creek's own water now ends, and its surface there, are kept for that
            float back = p.J.flow_in ? 11f : 0f;
            bool Gone(Vector2 q) { if (p.InCut(q, 0.5f, out _, out _)) return true; float s = Vector2.Dot(q - p.A, p.U), lat = Vector2.Dot(q - p.A, p.Nl); return back > 0f && s > -back && s < 1f && Mathf.Abs(lat) < p.Half + 4f; }
            float best = float.MaxValue;
            foreach (var (pts, surf) in Beds.Streams)
                for (int i = pts.Count - 1; i >= 0; i--)
                    if (Gone(pts[i]))
                    {
                        if (back > 0f)   // its neighbor still standing (the creek's new end) nearest the mouth
                            foreach (int k in new[] { i - 1, i + 1 })
                                if (k >= 0 && k < pts.Count && !Gone(pts[k]) && (pts[k] - p.A).magnitude < best)
                                { best = (pts[k] - p.A).magnitude; p.HasFlow = true; p.FlowFrom = pts[k]; p.FlowSurf = surf[k]; p.FlowDir = (pts[i] - pts[k]).normalized; }
                        pts.RemoveAt(i); surf.RemoveAt(i); removed++;
                    }
            Beds.Streams.RemoveAll(st => st.p.Count < 2);
            Beds.Index();
            Debug.Log($"[Round23] pipe {p.J.id}: the streams' water stops at its mouths ({removed} stream points inside its cuts dropped)" + (p.HasFlow ? $"; Round 23 follow-ups: the creek's flat water now ends at ({p.FlowFrom.x:0.0}, {p.FlowFrom.y:0.0}), {(p.FlowFrom - p.A).magnitude:0.0} m out, moving water on from there into the barrels" : ""));
        }

        // Unity terrain holes over the cuts: every hole cell whose center is in a cut, where the ground stands above the floor
        static void CutPipeHoles(TerrainGrid g, PipePlan p)
        {
            int cells = 0;
            foreach (var tile in g.Tiles)
            {
                var data = tile.terrainData; int hr = data.holesResolution; var size = data.size; var pos = tile.transform.position;
                float cx = size.x / hr, cz = size.z / hr;
                var bounds = BoundsOf(new[] { p.P(-1f, -4f), p.P(-1f, 4f), p.P(p.L + 1f, -4f), p.P(p.L + 1f, 4f) }, 1f);
                int x0 = Mathf.Clamp(Mathf.FloorToInt((bounds.xMin - pos.x) / cx), 0, hr - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((bounds.xMax - pos.x) / cx), 0, hr - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((bounds.yMin - pos.z) / cz), 0, hr - 1), z1 = Mathf.Clamp(Mathf.CeilToInt((bounds.yMax - pos.z) / cz), 0, hr - 1);
                if (bounds.xMax < pos.x || bounds.xMin > pos.x + size.x || bounds.yMax < pos.z || bounds.yMin > pos.z + size.z || x1 <= x0 || z1 <= z0) continue;
                var holes = data.GetHoles(x0, z0, x1 - x0 + 1, z1 - z0 + 1);   // true = surface, false = hole
                bool any = false;
                for (int j = 0; j <= z1 - z0; j++)
                    for (int i = 0; i <= x1 - x0; i++)
                    {
                        var q = new Vector2(pos.x + (x0 + i + 0.5f) * cx, pos.z + (z0 + j + 0.5f) * cz);
                        if (!p.InCut(q, 0f, out float s, out _)) continue;
                        if (p.J.twin && s < 0.5f) continue;   // Round 23: a twin's floor reaches 0.8 m past its mouth over the ground (no gap at the edge)
                        if (!p.J.twin && Ground(g, q.x, q.y) < p.Y(Mathf.Clamp(s, 0f, p.L)) + 0.08f) continue;   // the floor is at the ground here (Round 23: a twin is holed all over its cut, no grass through its floor)
                        holes[j, i] = false; any = true; cells++;
                    }
                if (any) data.SetHoles(x0, z0, holes);
            }
            Debug.Log($"[Round18] pipe {p.J.id}: {cells} terrain hole cells over its cuts");
        }

        static Material pipeConcrete, pipeLamp, pipeWater, pipeSplash;
        static void BuildPipes(TerrainGrid terrain, Materials mats, Transform world, Pipes pipes)
        {
            if (pipes == null || pipes.Plans.Count == 0) return;
            var root = new GameObject("Pipes").transform; root.SetParent(world, false);
            // the pipe's inside: lit by its lamps and the headlights (URP Lit takes point and spot lights; the vertex-lit map shader
            // does not). Round 19: stained, darker concrete (a texture made here, PipeGrimeTexture)
            var grime = PipeGrimeTexture(); AssetDatabase.CreateAsset(grime, GeneratedDir + "/PipeGrime.asset");
            pipeConcrete = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeConcrete" };
            pipeConcrete.SetTexture("_BaseMap", grime); pipeConcrete.SetColor("_BaseColor", new Color(0.66f, 0.65f, 0.62f)); pipeConcrete.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(pipeConcrete, GeneratedDir + "/PipeConcrete.mat");
            pipeLamp = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeLamp" };
            pipeLamp.SetColor("_BaseColor", new Color(0.9f, 0.78f, 0.55f)); pipeLamp.EnableKeyword("_EMISSION"); pipeLamp.SetColor("_EmissionColor", new Color(1.3f, 1.0f, 0.6f));
            AssetDatabase.CreateAsset(pipeLamp, GeneratedDir + "/PipeLamp.mat");
            // Round 19: the stream (moving water: PipeStream scrolls it) and its splashes
            var wt = PipeWaterTexture(); AssetDatabase.CreateAsset(wt, GeneratedDir + "/PipeWater.asset");
            pipeWater = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeWater" };
            pipeWater.SetTexture("_BaseMap", wt); pipeWater.SetColor("_BaseColor", new Color(0.62f, 0.70f, 0.72f)); pipeWater.SetFloat("_Smoothness", 0.93f);   // opaque and glossy (a transparent one did not show in the exe)
            AssetDatabase.CreateAsset(pipeWater, GeneratedDir + "/PipeWater.mat");
            pipeSplash = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "PipeSplash" };
            pipeSplash.SetColor("_BaseColor", new Color(0.78f, 0.84f, 0.86f, 0.7f)); MakeTransparent(pipeSplash);
            AssetDatabase.CreateAsset(pipeSplash, GeneratedDir + "/PipeSplash.mat");
            foreach (var p in pipes.Plans) BuildPipe(terrain, mats, root, p);
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
        }

        // Round 19: one 2.4 m pipe section's inside, unrolled: u along the pipe, v round it from the invert (v 0.5 = the crown).
        // Concrete with a little mottling, a joint at each end, dark streaks run down from the crown, grime and moss below a
        // wavering tide line 0.75 m up, the invert wet and dark. Made for the standard 5 m pipe (heights from R 2.5 m)
        static Texture2D PipeGrimeTexture()
        {
            const int W = 256, H = 512; const float R = 2.5f;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "PipeGrime", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var rnd = new System.Random(19);
            var streaks = new List<(float u, float w, float len, float dark)>();
            for (int i = 0; i < 46; i++) streaks.Add(((float)rnd.NextDouble(), 0.004f + 0.022f * (float)rnd.NextDouble(), 0.12f + 0.38f * (float)rnd.NextDouble(), 0.05f + 0.17f * (float)rnd.NextDouble()));
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)H, th = v * Mathf.PI * 2f, h = R * (1f - Mathf.Cos(th));
                float fromCrown = Mathf.Abs(Mathf.DeltaAngle(th * Mathf.Rad2Deg, 180f)) / 180f;   // 0 at the crown, 1 at the invert
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W;
                    float n = Mathf.PerlinNoise(u * 6f + 3.1f, v * 9f) * 0.6f + Mathf.PerlinNoise(u * 23f, v * 31f + 7f) * 0.4f;
                    float k = 0.84f + 0.18f * n;
                    foreach (var st in streaks)
                    {
                        float du = Mathf.Abs(Mathf.DeltaAngle(u * 360f, st.u * 360f)) / 360f;
                        if (du < st.w && fromCrown < st.len) k -= st.dark * (1f - fromCrown / st.len) * (1f - du / st.w);
                    }
                    var c = new Color(0.64f, 0.63f, 0.59f) * k;
                    float g = Mathf.Clamp01((1.15f - h) / 1.15f);
                    c = Color.Lerp(c, new Color(0.29f, 0.27f, 0.21f), g * 0.78f);
                    float moss = g * Mathf.Clamp01(Mathf.PerlinNoise(u * 11f + 40f, v * 17f) * 1.7f - 0.55f);
                    c = Color.Lerp(c, new Color(0.22f, 0.31f, 0.15f), moss * 0.85f);
                    float tide = 0.75f + 0.04f * Mathf.Sin(u * Mathf.PI * 6f) + 0.02f * (Mathf.PerlinNoise(u * 8f, 1f) - 0.5f);
                    c *= Mathf.Lerp(1f, 0.62f, Smooth01(1f - Mathf.Abs(h - tide) / 0.07f));   // a soft dark line
                    if (h < 0.2f) c *= 0.5f;
                    if (u < 0.012f || u > 0.994f) c *= 0.42f;
                    c.a = 1f; px[y * W + x] = c;
                }
            }
            t.SetPixels(px); t.Apply(true); return t;
        }

        // Round 19: the stream's surface: pale streaks along the flow over a see-through base
        static Texture2D PipeWaterTexture()
        {
            const int W = 64, H = 256;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "PipeWater", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = Mathf.PerlinNoise(x / (float)W * 5f, y / (float)H * 26f), m = Mathf.PerlinNoise(x * 0.21f + 9f, y * 0.06f + 5f);
                    float b = 0.55f + 0.45f * Mathf.Lerp(m, n, 0.6f); px[y * W + x] = new Color(b, b, b * 1.04f, 1f);
                }
            t.SetPixels(px); t.Apply(true); return t;
        }

        static void BuildPipe(TerrainGrid terrain, Materials mats, Transform parent, PipePlan p)
        {
            var root = new GameObject("Pipe " + p.J.id + " (" + p.J.name + ")").transform; root.SetParent(parent, false);
            var concrete = new Color(0.62f, 0.61f, 0.58f);
            Vector3 W(float s, float lat, float y) { var q = p.P(s, lat); return new Vector3(q.x, y, q.y); }
            var up3 = Vector3.up; var u3 = new Vector3(p.U.x, 0f, p.U.y); var n3 = new Vector3(p.Nl.x, 0f, p.Nl.y);
            // the wall tops: the highest ground just outside each wall, + 0.12 m (the coping hides the hole's ragged edge)
            float Top(float s, float side)
            {
                float hi = float.MinValue;
                float h0 = p.HalfAt(s); for (float o = h0; o <= h0 + CopingOut + 0.3f; o += 0.3f) { var q = p.P(s, side * o); hi = Mathf.Max(hi, Ground(terrain, q.x, q.y)); }
                return Mathf.Max(hi, p.Y(s)) + 0.12f;
            }
            // ---- the cuts (floor, walls, coping, railing), the headwalls
            var cut = new MeshBuilder(); var rail = new MeshBuilder(); var lampFace = new List<(Vector3 at, Vector3 n)>();
            var steel = new Color(0.36f, 0.38f, 0.40f);
            int lamps = 0;
            foreach (var (s0, s1, inner) in new[] { (0f, p.S1, p.S1), (p.S2, p.L, p.S2) })
            {
                if (inner == p.S2 && p.J.open_b) continue;   // Round 23 follow-ups: the network's pipes go on from b (MapBuilder.Network.cs)
                int k = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 0.5f));
                float lead = p.J.twin && s0 == 0f ? -0.8f : 0f;   // Round 23: a twin's floor starts 0.8 m out, over the apron
                for (int i = 0; i < k; i++)
                {
                    float a = Mathf.Lerp(s0, s1, i / (float)k), b = Mathf.Lerp(s0, s1, (i + 1) / (float)k);
                    if (i == 0 && lead < 0f)
                        for (int j = 0; j < 24; j++)
                        {
                            float ta = -1f + 2f * j / 24f, tb = -1f + 2f * (j + 1) / 24f, h0 = p.HalfAt(0f);
                            cut.Tint = Shade(concrete, 0.9f);
                            Face(cut, W(lead, ta * h0, p.FloorAt(0f, ta * h0) + 0.02f), W(0f, ta * h0, p.FloorAt(0f, ta * h0) + 0.02f), W(0f, tb * h0, p.FloorAt(0f, tb * h0) + 0.02f), W(lead, tb * h0, p.FloorAt(0f, tb * h0) + 0.02f), up3);
                        }
                    float ha = p.HalfAt(a), hb = p.HalfAt(b);
                    // Round 19: the floor in 12 strips across, flat, turning into the pipe's round bottom near the headwall
                    int across = p.J.twin ? 24 : 12;   // Round 23: a twin's two round bottoms need the finer strips
                    for (int j = 0; j < across; j++)
                    {
                        float ta = -1f + 2f * j / across, tb = -1f + 2f * (j + 1) / across;
                        float low = Mathf.Min(Mathf.Abs(ta), Mathf.Abs(tb));
                        cut.Tint = Shade(concrete, ((Mathf.FloorToInt(a / 3f) & 1) == 0 ? 0.92f : 0.87f) * Mathf.Lerp(0.62f, 1f, low));   // darker down the middle: wet and worn
                        Face(cut, W(a, ta * ha, p.FloorAt(a, ta * ha) + 0.02f), W(b, ta * hb, p.FloorAt(b, ta * hb) + 0.02f), W(b, tb * hb, p.FloorAt(b, tb * hb) + 0.02f), W(a, tb * ha, p.FloorAt(a, tb * ha) + 0.02f), up3);
                    }
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float ta = Top(a, side), tb = Top(b, side);
                        cut.Tint = Shade(concrete, 0.86f);
                        var wn = Vector3.Cross(Vector3.up, W(b, side * hb, 0f) - W(a, side * ha, 0f)).normalized * side;   // the inner face's normal, flared
                        Face(cut, W(a, side * ha, p.Y(a) - 0.2f), W(b, side * hb, p.Y(b) - 0.2f), W(b, side * hb, tb), W(a, side * ha, ta), -wn * Mathf.Sign(Vector3.Dot(wn, n3 * side)));   // the inner face
                        cut.Tint = Shade(concrete, 1.0f);
                        Face(cut, W(a, side * ha, ta), W(b, side * hb, tb), W(b, side * (hb + CopingOut), tb), W(a, side * (ha + CopingOut), ta), up3);   // the coping
                        Face(cut, W(a, side * (ha + CopingOut), ta), W(b, side * (hb + CopingOut), tb), W(b, side * (hb + CopingOut), tb - 0.4f), W(a, side * (ha + CopingOut), ta - 0.4f), n3 * side);
                    }
                }
                // Round 23: a twin's wall ends at the mouths closed with a face (the wall, the coping and the ground under them read as one
                // solid wingwall from outside; the exe check saw through the open end into the terrain hole under the coping)
                if (p.J.twin)
                {
                    float m = inner == p.S1 ? 0f : p.L; var outward = u3 * (inner == p.S1 ? -1f : 1f);
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float hm = p.HalfAt(m), tm = Top(m, side), bm = p.Y(m) - 0.6f;
                        cut.Tint = Shade(concrete, 0.84f);
                        Face(cut, W(m, side * hm, bm), W(m, side * (hm + CopingOut), bm), W(m, side * (hm + CopingOut), tm), W(m, side * hm, tm), outward);
                    }
                }
                // railing on the walls where the drop is over 1 m: posts every 2 m, a top rail and a mid rail
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector3? prevTop = null, prevMid = null;
                    for (float s = s0; s <= s1 + 1e-3f; s += 2f)
                    {
                        float t = Top(s, side), drop = t - p.Y(s);
                        if (drop < 1f) { prevTop = prevMid = null; continue; }
                        var b = W(s, side * (p.HalfAt(s) + 0.25f), t);
                        Post(rail, b, 1.05f, 0.07f, steel);
                        var top = b + up3 * 1.0f; var mid = b + up3 * 0.5f;
                        if (prevTop.HasValue) { Beam(rail, prevTop.Value, top, 0.06f, 0.06f, steel); Beam(rail, prevMid.Value, mid, 0.05f, 0.05f, steel); }
                        prevTop = top; prevMid = mid;
                    }
                }
                // the headwall across the inner end, facing the cut, with the round opening
                float face = inner == p.S1 ? p.S1 : p.S2, sgn = inner == p.S1 ? -1f : 1f;   // the side the cut is on
                float yb = p.Y(face) - 0.6f, yt = Mathf.Max(Mathf.Max(Top(face, 1f), Top(face, -1f)), p.Y(face) + 2f * p.R + 0.3f);
                const int seg = 48; float hw = p.Half + CutWall;
                var normal = u3 * sgn;
                cut.Tint = Shade(concrete, 0.82f);
                foreach (float bl in p.Lats)   // Round 23: a twin's headwall is two halves, each round its own opening, meeting at the pier (lateral 0)
                {
                    var c = W(face, bl, p.Y(face) - PipeFloorRise + p.R);
                    float wPlus = bl > 0f ? hw - bl : bl < 0f ? -bl : hw, wMinus = bl < 0f ? hw + bl : bl > 0f ? bl : hw;   // the half's extent each side of its opening's center
                    for (int i = 0; i < seg; i++)
                    {
                        float t0 = Mathf.PI * 2f * i / seg, t1 = Mathf.PI * 2f * (i + 1) / seg;
                        Vector3 C(float th) => c + n3 * (Mathf.Sin(th) * p.R) - up3 * (Mathf.Cos(th) * p.R);
                        Vector3 Rr(float th)
                        {
                            var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th));   // (lateral, up) from the center
                            float kx = d.x > 1e-4f ? wPlus / d.x : d.x < -1e-4f ? wMinus / -d.x : float.MaxValue;
                            float ky = d.y > 1e-4f ? (yt - c.y) / d.y : d.y < -1e-4f ? (c.y - yb) / -d.y : float.MaxValue;
                            float kk = Mathf.Min(kx, ky); return c + n3 * (d.x * kk) + up3 * (d.y * kk);
                        }
                        Face(cut, C(t0), C(t1), Rr(t1), Rr(t0), normal);
                    }
                }
                var c0 = W(face, 0f, p.Y(face) - PipeFloorRise + p.R);
                // Round 21 follow-ups (RA's hint, 'mouth_lamp_b'): a caged lamp over the outlet's opening, lit, seen across the small pond
                if (p.J.mouth_lamp_b && inner == p.S2)
                {
                    var lc = c0 + up3 * (p.R + 0.55f) + normal * 0.18f; lampFace.Add((lc, normal));
                }
                // its coping, over the ground behind it
                cut.Tint = Shade(concrete, 1.0f);
                Face(cut, W(face, -hw - CopingOut, yt), W(face, hw + CopingOut, yt), W(face - sgn * 1.0f, hw + CopingOut, yt), W(face - sgn * 1.0f, -hw - CopingOut, yt), up3);
                Face(cut, W(face, -hw - CopingOut, yt - 0.35f), W(face, hw + CopingOut, yt - 0.35f), W(face, hw + CopingOut, yt), W(face, -hw - CopingOut, yt), normal);
            }
            var cutGo = cut.ToObject("sc Pipe " + p.J.id + " cuts and headwalls", mats.Concrete, root, false);   // Round 19: "sc", so VehicleSurfaceContacts takes the faces' own normals (a body scraping the round bottom is not stopped)
            AddCollider(cutGo, cut, "Pipe " + p.J.id + " cuts collider");
            if (rail.Count > 0) { var rg = rail.ToObject("Pipe " + p.J.id + " railings", mats.House, root, false); AddCollider(rg, rail, "Pipe " + p.J.id + " railings collider"); }

            foreach (float bl in p.Lats)   // Round 23: one barrel, or a twin's two
            {
                string bid = p.J.id + (p.J.twin ? (bl < 0f ? "R" : "L") : "");   // a twin's barrels: L (left, +lateral) and R
                if (p.S2 - p.S1 < 0.5f) continue;   // Round 23 follow-ups: an open cut all the way (no barrel)
                // ---- the pipe: round all the way (Round 19), inside faces only, UVs for the stained concrete (u along, 2.4 m a section)
                const int arc = 48;
                Vector3 Axis(float s) => W(s, bl, p.Y(s) - PipeFloorRise + p.R);
                Vector3 Ring(float s, float th) => Axis(s) + n3 * (Mathf.Sin(th) * p.R) - up3 * (Mathf.Cos(th) * p.R);
                int rings0 = Mathf.Max(2, Mathf.CeilToInt((p.S2 - p.S1) / 0.5f) + 1);
                var at = new List<float>(); for (int i = 0; i < rings0; i++) at.Add(Mathf.Lerp(p.S1, p.S2, i / (float)(rings0 - 1)));
                if (p.Clear) { at.RemoveAll(x => Mathf.Abs(x - p.C0) < 0.05f || Mathf.Abs(x - p.C1) < 0.05f); at.Add(p.C0); at.Add(p.C1); at.Sort(); }   // Round 21 follow-ups: rings at the glass's ends
                int rings = at.Count;
                var bv = new List<Vector3>(); var buv = new List<Vector2>(); var bt = new List<int>(); var gt = new List<int>();
                for (int i = 0; i < rings; i++)
                {
                    float s = at[i];
                    for (int k = 0; k <= arc; k++) { float th = Mathf.PI * 2f * k / arc; bv.Add(Ring(s, th)); buv.Add(new Vector2((s - p.S1) / 2.4f, k / (float)arc)); }
                }
                for (int i = 0; i + 1 < rings; i++)
                {
                    bool glass = p.Clear && at[i] >= p.C0 - 1e-3f && at[i + 1] <= p.C1 + 1e-3f;   // Round 21 follow-ups: the clear stretch is glass
                    for (int k = 0; k < arc; k++)
                    {
                        int a0 = i * (arc + 1) + k, a1 = a0 + 1, b0 = a0 + arc + 1, b1 = b0 + 1;
                        var l = glass ? gt : bt; l.Add(a0); l.Add(b0); l.Add(a1); l.Add(a1); l.Add(b0); l.Add(b1);
                    }
                }
                {   // wind so the faces look at the axis (inward)
                    var t0 = bt.Count > 0 ? bt : gt;
                    var n0 = Vector3.Cross(bv[t0[1]] - bv[t0[0]], bv[t0[2]] - bv[t0[0]]);
                    if (Vector3.Dot(n0, Axis(p.S1) - bv[t0[0]]) < 0f) foreach (var l in new[] { bt, gt }) for (int i = 0; i < l.Count; i += 3) { int x = l[i + 1]; l[i + 1] = l[i + 2]; l[i + 2] = x; }
                }
                var mesh = new Mesh { name = "Pipe " + bid, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(bv); mesh.SetUVs(0, buv); mesh.subMeshCount = p.Clear ? 2 : 1; mesh.SetTriangles(bt, 0); if (p.Clear) mesh.SetTriangles(gt, 1); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/Pipe {bid}.asset");
                var bgo = new GameObject("sc Pipe " + bid + " barrel"); bgo.transform.SetParent(root, false);   // Round 19: "sc" (as the cuts)
                bgo.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = bgo.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
                if (p.Clear) { ClearMaterials(); mr.sharedMaterials = new[] { pipeConcrete, clearGlass }; } else mr.sharedMaterial = pipeConcrete;
                var mc = bgo.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.sharedMaterial = RampMaterial();
                // lamps along the crown: a caged fitting and a dim point light every 16 m (Round 19: every 8 m and brighter before)
                var lampMb = new MeshBuilder();
                var lampAt = new List<float>(); for (float s = p.S1 + 6f; s < p.S2 - 4f; s += 16f) lampAt.Add(s);
                if (p.Clear)   // Round 21 follow-ups: none over the glass (daylight there); one in the middle of each concrete length either side of it
                {
                    lampAt.RemoveAll(s => s > p.C0 - 3f && s < p.C1 + 3f);
                    if (p.C0 - p.S1 >= 4f) lampAt.Add((p.S1 + p.C0) * 0.5f);
                    if (p.S2 - p.C1 >= 4f) lampAt.Add((p.C1 + p.S2) * 0.5f);
                }
                foreach (float s in lampAt)
                {
                    var top = Ring(s, Mathf.PI); var c = top - up3 * 0.12f;
                    lampMb.Tint = Color.white;
                    BoxAt(lampMb, c, u3, 0.7f, 0.14f, 0.22f);
                    var lg = new GameObject("Pipe lamp"); lg.transform.SetParent(root, false); lg.transform.position = c - up3 * 0.4f;
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.1f; l.color = new Color(1f, 0.82f, 0.58f); l.shadows = LightShadows.None;
                    lamps++;
                }
                foreach (var (lat3, nrm) in lampFace)   // Round 21 follow-ups: the outlet's lamp (RA's hint)
                {
                    lampMb.Tint = Color.white; BoxAt(lampMb, lat3, Vector3.Cross(Vector3.up, nrm), 0.6f, 0.32f, 0.3f);
                    var lg = new GameObject("Pipe outlet lamp (RA's hint)"); lg.transform.SetParent(root, false); lg.transform.position = lat3 + nrm * 0.6f;
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.intensity = 2.2f; l.color = new Color(1f, 0.82f, 0.58f); l.shadows = LightShadows.None;
                    lamps++;
                }
                if (lampMb.Count > 0) { var lgo = lampMb.ToObject("Pipe " + bid + " lamps", pipeLamp, root, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }

                // ---- Round 19: the stream down the middle of the floor, end to end (look and sound only: no water volume, nothing slows)
                if (p.HasFlow) continue;   // Round 23 follow-ups: the creek's own water runs through these barrels (BuildFlowIn)
                var sv = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>(); var line = new List<Vector3>();
                int sn = 0;
                for (float s = 0.6f; s <= p.L - 0.6f + 1e-3f; s += 0.5f)
                {
                    float half = 0.45f + 0.08f * Mathf.Sin(s * 0.7f) + 0.05f * Mathf.Sin(s * 2.3f), wob = bl + 0.06f * Mathf.Sin(s * 0.45f);
                    foreach (float t in new[] { -1f, -0.5f, 0f, 0.5f, 1f }) { float lat = wob + t * half; sv.Add(W(s, lat, p.FloorAt(s, lat) + 0.035f + 0.01f * (1f - Mathf.Abs(t)))); suv.Add(new Vector2(t * 0.5f + 0.5f, s / 3f)); }
                    line.Add(W(s, wob, p.FloorAt(s, wob) + 0.05f));
                    if (sn > 0) for (int k = 0; k < 4; k++) { int a0 = (sn - 1) * 5 + k, b0 = sn * 5 + k; st.Add(a0); st.Add(b0); st.Add(a0 + 1); st.Add(a0 + 1); st.Add(b0); st.Add(b0 + 1); }
                    sn++;
                }
                { var n0 = Vector3.Cross(sv[st[1]] - sv[st[0]], sv[st[2]] - sv[st[0]]); if (n0.y < 0f) for (int i = 0; i < st.Count; i += 3) { int x = st[i + 1]; st[i + 1] = st[i + 2]; st[i + 2] = x; } }
                var smesh = new Mesh { name = "Pipe " + bid + " stream", indexFormat = IndexFormat.UInt32 };
                smesh.SetVertices(sv); smesh.SetUVs(0, suv); smesh.SetTriangles(st, 0); smesh.RecalculateNormals(); smesh.RecalculateBounds();
                AssetDatabase.CreateAsset(smesh, $"{GeneratedDir}/Meshes/Pipe {bid} stream.asset");
                var sgo = new GameObject("Pipe " + bid + " stream"); sgo.transform.SetParent(root, false);
                sgo.AddComponent<MeshFilter>().sharedMesh = smesh;
                var smr = sgo.AddComponent<MeshRenderer>(); smr.sharedMaterial = pipeWater; smr.shadowCastingMode = ShadowCastingMode.Off;
                var ps = sgo.AddComponent<PipeStream>(); ps.line = line.ToArray(); ps.water = pipeWater; ps.splash = pipeSplash;

            }
            // ---- Round 19: the rock bar across the creek to the inlet (look on the graded bar; DryPatch over it)
            if (p.Bar.Count > 0)
            {
                var rocks = new MeshBuilder(); var first = p.Bar[0]; var last = p.Bar[p.Bar.Count - 1];
                var along = (last - first).sqrMagnitude > 1e-4f ? (last - first).normalized : p.U; var across2 = new Vector2(-along.y, along.x);
                float len = Vector2.Distance(first, last) + 3f; var start = first - along * 1.5f;
                var stone = new Color(0.50f, 0.48f, 0.44f); int slabs = 0, boulders = 0;
                for (float a = 0.4f; a < len; a += 1.05f)
                    for (float c2 = -BarHalf + 0.45f; c2 <= BarHalf - 0.3f; c2 += 1.1f)
                    {
                        int h = Mathf.RoundToInt(a * 10f) * 131 + Mathf.RoundToInt((c2 + 9f) * 10f);
                        var q = start + along * (a + 0.3f * (Hash01(h, 961) - 0.5f)) + across2 * (c2 + 0.3f * (Hash01(h, 963) - 0.5f));
                        float r = 0.42f + 0.18f * Hash01(h, 965), rot = Hash01(h, 967) * 6.283f, y = Ground(terrain, q.x, q.y) + 0.02f + 0.02f * Hash01(h, 969);
                        rocks.Tint = Shade(stone, 0.78f + 0.3f * Hash01(h, 971)); int c0 = rocks.Vertex(new Vector3(q.x, y + 0.01f, q.y)); int e0 = rocks.Count;
                        for (int k = 0; k < 6; k++) { float th = rot + k * 1.047f; float rr = r * (0.8f + 0.3f * Hash01(h + k, 973)); var e = q + new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * rr; rocks.Vertex(new Vector3(e.x, Mathf.Max(y - 0.03f, Ground(terrain, e.x, e.y) + 0.005f), e.y)); }
                        for (int k = 0; k < 6; k++) { int i0 = e0 + k, i1 = e0 + (k + 1) % 6; if (Vector3.Cross(rocks.Verts[i0] - rocks.Verts[c0], rocks.Verts[i1] - rocks.Verts[c0]).y >= 0f) rocks.Tri(c0, i0, i1); else rocks.Tri(c0, i1, i0); }
                        slabs++;
                    }
                // bigger creek rocks in the water either side of the bar
                for (int i = 0; i < 10; i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f, a = len * (i / 10f + 0.05f);
                    var q = start + along * a + across2 * side * (BarHalf + 0.9f + 1.4f * Hash01(i, 975)); float r = 0.35f + 0.45f * Hash01(i, 977);
                    float y = p.BarTop - 0.12f + 0.12f + 0.2f * Hash01(i, 979);
                    rocks.Tint = Shade(stone, 0.7f + 0.25f * Hash01(i, 981));
                    var cc = new Vector3(q.x, Ground(terrain, q.x, q.y) - 0.1f, q.y); var apex = new Vector3(q.x, y, q.y);
                    Vector3[] dirs = { Vector3.right, (Vector3.right + Vector3.forward).normalized, Vector3.forward, (Vector3.forward + Vector3.left).normalized, Vector3.left, (Vector3.left + Vector3.back).normalized, Vector3.back, (Vector3.back + Vector3.right).normalized };
                    for (int k = 0; k < 8; k++) TriFace(rocks, cc + dirs[k] * r * (1.1f + 0.3f * Hash01(i * 8 + k, 983)), cc + dirs[(k + 1) % 8] * r * (1.1f + 0.3f * Hash01(i * 8 + (k + 1) % 8, 983)), apex, dirs[k] + dirs[(k + 1) % 8] + Vector3.up);
                    boulders++;
                }
                rocks.ToObject("Pipe " + p.J.id + " rock bar", mats.House, root, false);
                var dry = new GameObject("Pipe " + p.J.id + " rock bar (dry)"); dry.transform.SetParent(root, false);
                var dp2 = dry.AddComponent<DryPatch>(); dp2.top = p.BarTop;
                var e1 = start - along * 0.5f; var e2 = start + along * (len + 0.5f); float hh = BarHalf + 0.2f;
                dp2.outline = new[] { e1 + across2 * hh, e2 + across2 * hh, e2 - across2 * hh, e1 - across2 * hh };
                Debug.Log($"[Round19] pipe {p.J.id}: the rock bar's look: {slabs} flat slabs over {len:0.0} m x {BarHalf * 2f:0} m, {boulders} creek rocks either side; a dry patch over it");
            }

            if (p.Clear) { BuildClearStretch(terrain, mats, root, p); BuildPoolSurface(mats, root, p); }   // Round 21 follow-ups (MapBuilder.ClearPipe.cs)
            if (p.HasFlow) BuildFlowIn(terrain, root, p);   // Round 23 follow-ups (BUG-005)
            if (p.J.outfall) BuildOutfallLip(root, p, mats);  // Round 23 follow-ups: a network outfall's launch

            // the runtime side: the floor's centerline for resets and race lines; Round 19: the barrel for riding the walls
            // (Round 23: a twin has one per barrel, on its own child, each holding its half of the cut)
            foreach (float bl in p.Lats)
            {
                var host = p.J.twin ? new GameObject("Pipe " + p.J.id + (bl < 0f ? "R" : "L") + " runtime") : root.gameObject;
                if (p.J.twin) host.transform.SetParent(root, false);
                var dp = host.AddComponent<DrainPipe>(); dp.id = p.J.id + (p.J.twin ? (bl < 0f ? "R" : "L") : ""); dp.halfWidth = p.J.twin ? p.O : CutHalf; dp.clearHeight = p.J.diameter_m - PipeFloorRise;
                var fl = new List<Vector3>(); for (float s = 0f; s <= p.L + 1e-3f; s += 0.5f) fl.Add(W(s, bl, p.Y(s))); dp.floor = fl.ToArray();
                dp.radius = p.R; dp.barrelFrom = p.S1 / 0.5f; dp.barrelTo = p.S2 / 0.5f;
            }
            Debug.Log($"[Round18] pipe {p.J.id} built: cuts {p.S1:0.0} m and {p.L - p.S2:0.0} m, the pipe {p.S2 - p.S1:0.0} m ({p.J.diameter_m:0.0} m across, round all the way: Round 19), {lamps} lamps; a stream down the floor of each barrel" + (p.J.twin ? $" (a twin: two barrels {p.O * 2f:0.0} m apart, the cut {p.Half * 2f:0.0} m wide)" : ""));
        }

        static void AddCollider(GameObject go, MeshBuilder mb, string name)
        {
            var cm = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            cm.SetVertices(mb.Verts); cm.SetTriangles(mb.Tris, 0); cm.RecalculateNormals(); cm.RecalculateBounds();
            AssetDatabase.CreateAsset(cm, $"{GeneratedDir}/Meshes/{name}.asset");
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = cm; mc.sharedMaterial = RampMaterial();
        }

        // a box centered at c, its length along 'along', width across, height up
        static void BoxAt(MeshBuilder mb, Vector3 c, Vector3 along, float len, float h, float w)
        {
            var f = new Frame { O = c, F = along.normalized, R = Vector3.Cross(Vector3.up, along.normalized), M = 1f };
            BoxF(mb, f, -w / 2f, w / 2f, -h / 2f, h / 2f, -len / 2f, len / 2f, mb.Tint);
        }
    }
}
