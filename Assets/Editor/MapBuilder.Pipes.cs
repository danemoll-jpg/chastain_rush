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
//   pipe      round, 'diameter_m' inside, concrete, a flat floor 1.0 m above its invert (4.0 m wide; 4.0 m clear above it: a
//             Round 18 check stalled the 5 m Skyfin on 0.6 m, 3.25 m wide),
//             lamps along the crown. Inside faces only (nobody sees its outside)
// Everything is solid (mesh colliders, frictionless to a body like the shortcut ramps), the pipe's inside faces point inward,
// so the chase camera's obstruction cast stops at the pipe's wall and the camera stays in the pipe. DrainPipe (runtime) puts a
// reset inside it back on its floor, and gives race lines through it the floor's heights.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class PipeJson { public string id, name; public float[] a, b, approach_a, approach_b; public float diameter_m = 5f, cover_m = 0.6f, cut_slope = 0.18f; }
        [Serializable] class PipeFile { public PipeJson[] pipes; }
        const string PipeFilePath = "Data/pipes/pipes.json";
        const float PipeFloorRise = 1.0f, PipeWall = 0.3f, CutHalf = 2.5f, CutWall = 0.35f, CopingOut = 0.95f, PipeStep = 0.5f, Flare = 5f, FlareW = 1.75f;

        sealed class PipePlan
        {
            public PipeJson J; public Vector2 A, B, U, Nl; public float L;
            public float[] Floor;        // floor height at each PipeStep station
            public float S1, S2;         // the headwalls (cut 1 is 0..S1, cut 2 is S2..L)
            public float R => J.diameter_m * 0.5f;
            public float Y(float s) { float f = Mathf.Clamp(s / PipeStep, 0f, Floor.Length - 1.001f); int i = (int)f; return Mathf.Lerp(Floor[i], Floor[i + 1], f - i); }
            public Vector2 P(float s, float lat = 0f) => A + U * s + Nl * lat;
            // the cut's half width: 2.5 m, flaring out to 4.25 m over the last 5 m at each mouth (wingwalls: a vehicle a little off
            // the line is led in, not stopped by a wall's end)
            public float HalfAt(float s) => CutHalf + FlareW * Mathf.Max(Smooth01(1f - s / Flare), Smooth01(1f - (L - s) / Flare));
            public bool InCut(Vector2 q, float pad, out float s, out float lat)
            {
                s = Vector2.Dot(q - A, U); lat = Vector2.Dot(q - A, Nl);
                return Mathf.Abs(lat) <= HalfAt(Mathf.Clamp(s, 0f, L)) + CutWall + pad && ((s >= -pad && s <= S1 + 0.3f + pad) || (s >= S2 - 0.3f - pad && s <= L + pad));
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
        static Pipes PlanPipes(TerrainGrid terrain)
        {
            var result = new Pipes();
            string path = Path.Combine(ProjectRoot, PipeFilePath);
            if (!File.Exists(path)) return result;
            foreach (var j in JsonUtility.FromJson<PipeFile>(File.ReadAllText(path)).pipes)
            {
                var p = new PipePlan { J = j, A = new Vector2(j.a[0], j.a[1]), B = new Vector2(j.b[0], j.b[1]) };
                p.L = Vector2.Distance(p.A, p.B); p.U = (p.B - p.A) / p.L; p.Nl = new Vector2(-p.U.y, p.U.x);
                // the ground leading to each mouth first (the mouths' heights are taken from it)
                if (j.approach_a != null && j.approach_a.Length >= 2) { var l = ToPoints(j.approach_a); l.Add(p.A + p.U * 0.5f); GradeApproach(terrain, l, j.id + " inlet"); }
                if (j.approach_b != null && j.approach_b.Length >= 2) { var l = ToPoints(j.approach_b); l.Insert(0, p.B - p.U * 0.5f); GradeApproach(terrain, l, j.id + " outlet"); }
                int n = Mathf.CeilToInt(p.L / PipeStep) + 1;
                float crownOut = j.diameter_m - PipeFloorRise + PipeWall;   // floor to the top of the pipe's outside
                // the ground over the pipe: the lowest across its outside width, at each station
                var cover = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float s = Mathf.Min(i * PipeStep, p.L), lo = float.MaxValue;
                    for (float lat = -p.R - PipeWall; lat <= p.R + PipeWall + 1e-3f; lat += 0.5f) { var q = p.P(s, lat); lo = Mathf.Min(lo, Ground(terrain, q.x, q.y)); }
                    cover[i] = lo;
                }
                float yA = Ground(terrain, p.A.x, p.A.y), yB = Ground(terrain, p.B.x, p.B.y);
                int m0 = Mathf.RoundToInt(n * 0.3f), m1 = Mathf.RoundToInt(n * 0.7f);
                float yLow = float.MaxValue; for (int i = m0; i <= m1; i++) yLow = Mathf.Min(yLow, cover[i] - j.cover_m - crownOut);
                const float Ease = 6f;
                float Ramp(float s) => s < Ease ? s * s / (2f * Ease) : s - Ease * 0.5f;
                var raw = new float[n];
                for (int i = 0; i < n; i++) { float s = Mathf.Min(i * PipeStep, p.L); raw[i] = Mathf.Max(yLow, Mathf.Max(yA - j.cut_slope * Ramp(s), yB - j.cut_slope * Ramp(p.L - s))); }
                // round the dips (sigma 2.5 m), keeping the ends on the ground
                var f = new float[n]; int r = Mathf.CeilToInt(7.5f / PipeStep);
                for (int i = 0; i < n; i++)
                {
                    float acc = 0f, w = 0f;
                    for (int k = -r; k <= r; k++) { int ii = Mathf.Clamp(i + k, 0, n - 1); float ww = Mathf.Exp(-0.5f * (k * PipeStep / 2.5f) * (k * PipeStep / 2.5f)); acc += raw[ii] * ww; w += ww; }
                    float sm = acc / w, s = i * PipeStep, edge = Smooth01(Mathf.Min(s, p.L - s) / 8f);
                    f[i] = Mathf.Lerp(raw[i], Mathf.Min(raw[i], sm), edge);
                }
                p.Floor = f;
                // the headwalls: the longest run in the middle where the ground covers the crown by 0.4 m or more
                int bestA = -1, bestB = -1, runA = -1;
                for (int i = 0; i < n; i++)
                {
                    bool ok = cover[i] - (f[i] + crownOut) >= 0.4f;
                    if (ok && runA < 0) runA = i;
                    if ((!ok || i == n - 1) && runA >= 0) { int e = ok ? i : i - 1; if (e - runA > bestB - bestA) { bestA = runA; bestB = e; } runA = -1; }
                }
                if (bestA < 0 || (bestB - bestA) * PipeStep < 8f) { Debug.LogError($"[Round18] pipe {j.id}: the ground never covers it for 8 m (cover {j.cover_m} m): not built"); continue; }
                p.S1 = bestA * PipeStep; p.S2 = bestB * PipeStep;
                result.Plans.Add(p);
                CutPipeHoles(terrain, p);
                Debug.Log($"[Round18] pipe {j.id}: {p.L:0.0} m from ({p.A.x:0.0}, {p.A.y:0.0}) at {yA:0.00} m to ({p.B.x:0.0}, {p.B.y:0.0}) at {yB:0.00} m; floor down to {f.Min():0.00} m ({j.cut_slope * 100f:0} % in and out); inlet cut {p.S1:0.0} m, pipe {p.S2 - p.S1:0.0} m (crown {j.cover_m:0.0} m or more under the ground), outlet cut {p.L - p.S2:0.0} m");
            }
            return result;
        }

        // the way to a mouth: a band 6 m wide along the line (blending out over 4 m more: gentle sides where it cuts a bank), the ground cut where it is steeper
        // than 22 % (down a creek bank), and across a stream a gravel bar 0.15 m under the water (Round 18: the vehicles stalled
        // in the creek's 0.5 m in front of P1's inlet), as the trail's fords (MapBuilder.CreekTrack.cs)
        static void GradeApproach(TerrainGrid terrain, List<Vector2> line, string what)
        {
            var pts = Densify(line, 0.5f); int n = pts.Count;
            var g = pts.Select(q => Ground(terrain, q.x, q.y)).ToArray(); var t = new float[n];
            for (int i = 0; i < n; i++) { float best = g[i]; for (int k = 0; k < n; k++) best = Mathf.Min(best, g[k] + TrailMaxGrade * 0.5f * Mathf.Abs(i - k)); t[i] = best; }
            for (int i = 0; i < n; i++) if (Beds != null && Beds.NearStream(pts[i], out float d, out float surf) && d < StreamHalf + 0.5f) t[i] = Mathf.Max(t[i], surf - TrailShallow);
            float most = 0f;
            EditTerrain(terrain, BoundsOf(pts, 7.5f), (x, z, y) =>
            {
                var q = new Vector2(x, z); float best = float.MaxValue; int bi = 0; float bt = 0f;
                for (int i = 0; i + 1 < n; i++) { var ab = pts[i + 1] - pts[i]; float u = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - pts[i], ab) / ab.sqrMagnitude); float dd = (pts[i] + ab * u - q).magnitude; if (dd < best) { best = dd; bi = i; bt = u; } }
                float w = 1f - Smooth01((best - 3.0f) / 4f); if (w <= 0f) return y;
                float ny = y + (Mathf.Lerp(t[bi], t[bi + 1], bt) - y) * w; most = Mathf.Max(most, Mathf.Abs(ny - y)); return ny;
            });
            Debug.Log($"[Round18] pipe {what}: the way to it graded over {(n - 1) * 0.5f:0} m (largest change {most:0.00} m)");
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
                        if (Ground(g, q.x, q.y) < p.Y(Mathf.Clamp(s, 0f, p.L)) + 0.08f) continue;   // the floor is at the ground here
                        holes[j, i] = false; any = true; cells++;
                    }
                if (any) data.SetHoles(x0, z0, holes);
            }
            Debug.Log($"[Round18] pipe {p.J.id}: {cells} terrain hole cells over its cuts");
        }

        static Material pipeConcrete, pipeLamp;
        static void BuildPipes(TerrainGrid terrain, Materials mats, Transform world, Pipes pipes)
        {
            if (pipes == null || pipes.Plans.Count == 0) return;
            var root = new GameObject("Pipes").transform; root.SetParent(world, false);
            // the pipe's inside: lit by its lamps (URP Lit takes the point lights; the vertex-lit map shader does not)
            pipeConcrete = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeConcrete" };
            pipeConcrete.SetColor("_BaseColor", new Color(0.58f, 0.57f, 0.54f)); pipeConcrete.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(pipeConcrete, GeneratedDir + "/PipeConcrete.mat");
            pipeLamp = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeLamp" };
            pipeLamp.SetColor("_BaseColor", new Color(1f, 0.92f, 0.75f)); pipeLamp.EnableKeyword("_EMISSION"); pipeLamp.SetColor("_EmissionColor", new Color(2.4f, 2.1f, 1.5f));
            AssetDatabase.CreateAsset(pipeLamp, GeneratedDir + "/PipeLamp.mat");
            foreach (var p in pipes.Plans) BuildPipe(terrain, mats, root, p);
        }

        static void BuildPipe(TerrainGrid terrain, Materials mats, Transform parent, PipePlan p)
        {
            var root = new GameObject("Pipe " + p.J.id + " (" + p.J.name + ")").transform; root.SetParent(parent, false);
            var concrete = new Color(0.62f, 0.61f, 0.58f); var joint = new Color(0.45f, 0.44f, 0.42f);
            Vector3 W(float s, float lat, float y) { var q = p.P(s, lat); return new Vector3(q.x, y, q.y); }
            var up3 = Vector3.up; var u3 = new Vector3(p.U.x, 0f, p.U.y); var n3 = new Vector3(p.Nl.x, 0f, p.Nl.y);
            // the wall tops: the highest ground just outside each wall, + 0.12 m (the coping hides the hole's ragged edge)
            float Top(float s, float side)
            {
                float hi = float.MinValue;
                float h0 = p.HalfAt(s); for (float o = h0; o <= h0 + CopingOut + 0.3f; o += 0.3f) { var q = p.P(s, side * o); hi = Mathf.Max(hi, Ground(terrain, q.x, q.y)); }
                return Mathf.Max(hi, p.Y(s)) + 0.12f;
            }
            // ---- the cuts (slab, walls, coping, railing), the headwalls
            var cut = new MeshBuilder(); var rail = new MeshBuilder();
            var steel = new Color(0.36f, 0.38f, 0.40f);
            int lamps = 0;
            foreach (var (s0, s1, inner) in new[] { (0f, p.S1, p.S1), (p.S2, p.L, p.S2) })
            {
                int k = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 0.5f));
                for (int i = 0; i < k; i++)
                {
                    float a = Mathf.Lerp(s0, s1, i / (float)k), b = Mathf.Lerp(s0, s1, (i + 1) / (float)k);
                    cut.Tint = Shade(concrete, (Mathf.FloorToInt(a / 3f) & 1) == 0 ? 0.98f : 0.93f);
                    float ha = p.HalfAt(a), hb = p.HalfAt(b);
                    Face(cut, W(a, -ha, p.Y(a) + 0.02f), W(b, -hb, p.Y(b) + 0.02f), W(b, hb, p.Y(b) + 0.02f), W(a, ha, p.Y(a) + 0.02f), up3);
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float ta = Top(a, side), tb = Top(b, side);
                        cut.Tint = concrete;
                        var wn = Vector3.Cross(Vector3.up, W(b, side * hb, 0f) - W(a, side * ha, 0f)).normalized * side;   // the inner face's normal, flared
                        Face(cut, W(a, side * ha, p.Y(a) - 0.2f), W(b, side * hb, p.Y(b) - 0.2f), W(b, side * hb, tb), W(a, side * ha, ta), -wn * Mathf.Sign(Vector3.Dot(wn, n3 * side)));   // the inner face
                        cut.Tint = Shade(concrete, 1.05f);
                        Face(cut, W(a, side * ha, ta), W(b, side * hb, tb), W(b, side * (hb + CopingOut), tb), W(a, side * (ha + CopingOut), ta), up3);   // the coping
                        Face(cut, W(a, side * (ha + CopingOut), ta), W(b, side * (hb + CopingOut), tb), W(b, side * (hb + CopingOut), tb - 0.4f), W(a, side * (ha + CopingOut), ta - 0.4f), n3 * side);
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
                float yb = p.Y(face) - 0.6f, yt = Mathf.Max(Top(face, 1f), Top(face, -1f));
                var c = W(face, 0f, p.Y(face) - PipeFloorRise + p.R);
                const int seg = 48; float hw = CutHalf + CutWall;
                var normal = u3 * sgn;
                cut.Tint = Shade(concrete, 0.96f);
                for (int i = 0; i < seg; i++)
                {
                    float t0 = Mathf.PI * 2f * i / seg, t1 = Mathf.PI * 2f * (i + 1) / seg;
                    Vector3 C(float th) => c + n3 * (Mathf.Sin(th) * p.R) - up3 * (Mathf.Cos(th) * p.R);
                    Vector3 Rr(float th)
                    {
                        var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th));   // (lateral, up) from the center
                        float kx = Mathf.Abs(d.x) > 1e-4f ? hw / Mathf.Abs(d.x) : float.MaxValue;
                        float ky = d.y > 1e-4f ? (yt - c.y) / d.y : d.y < -1e-4f ? (c.y - yb) / -d.y : float.MaxValue;
                        float kk = Mathf.Min(kx, ky); return c + n3 * (d.x * kk) + up3 * (d.y * kk);
                    }
                    Face(cut, C(t0), C(t1), Rr(t1), Rr(t0), normal);
                }
                // its coping, over the ground behind it
                cut.Tint = Shade(concrete, 1.05f);
                Face(cut, W(face, -hw - CopingOut, yt), W(face, hw + CopingOut, yt), W(face - sgn * 1.0f, hw + CopingOut, yt), W(face - sgn * 1.0f, -hw - CopingOut, yt), up3);
                Face(cut, W(face, -hw - CopingOut, yt - 0.35f), W(face, hw + CopingOut, yt - 0.35f), W(face, hw + CopingOut, yt), W(face, -hw - CopingOut, yt), normal);
            }
            var cutGo = cut.ToObject("Pipe " + p.J.id + " cuts and headwalls", mats.Concrete, root, false);
            AddCollider(cutGo, cut, "Pipe " + p.J.id + " cuts collider");
            if (rail.Count > 0) { var rg = rail.ToObject("Pipe " + p.J.id + " railings", mats.House, root, false); AddCollider(rg, rail, "Pipe " + p.J.id + " railings collider"); }

            // ---- the pipe: inside faces only, a flat floor
            var barrel = new MeshBuilder();
            float th0 = Mathf.Acos((p.R - PipeFloorRise) / p.R);   // where the floor meets the wall
            const int arc = 28;
            int rings = Mathf.Max(2, Mathf.CeilToInt((p.S2 - p.S1) / 1f) + 1);
            Vector3 Ring(float s, float th) { var cc = W(s, 0f, p.Y(s) - PipeFloorRise + p.R); return cc + n3 * (Mathf.Sin(th) * p.R) - up3 * (Mathf.Cos(th) * p.R); }
            for (int i = 0; i + 1 < rings; i++)
            {
                float a = Mathf.Lerp(p.S1, p.S2, i / (float)(rings - 1)), b = Mathf.Lerp(p.S1, p.S2, (i + 1) / (float)(rings - 1));
                barrel.Tint = concrete;
                for (int k = 0; k < arc; k++)
                {
                    float t0 = Mathf.Lerp(th0, Mathf.PI * 2f - th0, k / (float)arc), t1 = Mathf.Lerp(th0, Mathf.PI * 2f - th0, (k + 1) / (float)arc);
                    var mid = (Ring(a, t0) + Ring(b, t1)) * 0.5f; var axis = W((a + b) * 0.5f, 0f, p.Y((a + b) * 0.5f) - PipeFloorRise + p.R);
                    Face(barrel, Ring(a, t0), Ring(b, t0), Ring(b, t1), Ring(a, t1), axis - mid);   // facing the axis: inward
                }
                barrel.Tint = Shade(concrete, 0.9f);
                Face(barrel, Ring(a, th0), Ring(b, th0), Ring(b, Mathf.PI * 2f - th0), Ring(a, Mathf.PI * 2f - th0), up3);   // the floor
            }
            // joints: a darker band every 2.4 m, just inside the wall
            for (float s = p.S1 + 1.2f; s < p.S2 - 0.5f; s += 2.4f)
            {
                barrel.Tint = joint;
                for (int k = 0; k < arc; k++)
                {
                    float t0 = Mathf.Lerp(th0, Mathf.PI * 2f - th0, k / (float)arc), t1 = Mathf.Lerp(th0, Mathf.PI * 2f - th0, (k + 1) / (float)arc);
                    var axis = W(s, 0f, p.Y(s) - PipeFloorRise + p.R);
                    Vector3 In(float ss, float th) => Vector3.Lerp(Ring(ss, th), axis, 0.008f);
                    Face(barrel, In(s, t0), In(s + 0.08f, t0), In(s + 0.08f, t1), In(s, t1), axis - Ring(s, (t0 + t1) * 0.5f));
                }
            }
            var bgo = new GameObject("Pipe " + p.J.id); bgo.transform.SetParent(root, false);
            var mesh = barrel.ToMesh("Pipe " + p.J.id); AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/Pipe {p.J.id}.asset");
            bgo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = bgo.AddComponent<MeshRenderer>(); mr.sharedMaterial = pipeConcrete; mr.shadowCastingMode = ShadowCastingMode.Off;
            AddCollider(bgo, barrel, "Pipe " + p.J.id + " collider");
            // lamps along the crown: a caged fitting and a point light every 8 m
            var lampMb = new MeshBuilder();
            for (float s = p.S1 + 4f; s < p.S2 - 2f; s += 8f)
            {
                var top = Ring(s, Mathf.PI); var c = top - up3 * 0.12f;
                lampMb.Tint = Color.white;
                BoxAt(lampMb, c, u3, 0.7f, 0.14f, 0.22f);
                var lg = new GameObject("Pipe lamp"); lg.transform.SetParent(root, false); lg.transform.position = c - up3 * 0.4f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 11f; l.intensity = 2.2f; l.color = new Color(1f, 0.9f, 0.72f); l.shadows = LightShadows.None;
                lamps++;
            }
            if (lampMb.Count > 0) { var lgo = lampMb.ToObject("Pipe " + p.J.id + " lamps", pipeLamp, root, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
            // the runtime side: the floor's centerline for resets and race lines
            var dp = root.gameObject.AddComponent<DrainPipe>(); dp.id = p.J.id; dp.halfWidth = CutHalf; dp.clearHeight = p.J.diameter_m - PipeFloorRise;
            var fl = new List<Vector3>(); for (float s = 0f; s <= p.L + 1e-3f; s += 0.5f) fl.Add(W(s, 0f, p.Y(s))); dp.floor = fl.ToArray();
            Debug.Log($"[Round18] pipe {p.J.id} built: cuts {p.S1:0.0} m and {p.L - p.S2:0.0} m, the pipe {p.S2 - p.S1:0.0} m ({p.J.diameter_m:0.0} m across, floor {2f * p.R * Mathf.Sin(th0):0.00} m wide, {p.J.diameter_m - PipeFloorRise:0.0} m clear), {lamps} lamps");
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
