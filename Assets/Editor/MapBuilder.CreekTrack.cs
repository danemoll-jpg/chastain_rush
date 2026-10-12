using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 18, item 3: the Noonday Creek trail (track B as Dan chose it), from Data/tracks/noonday_creek.json
// (Tools/mapgen/make_creek_track.py). A narrow woods trail, not a road (Dan: "It should feel like you are in the woods on a small
// trail, not on a road"):
// Round 19 (items 1, 2; Dan: "probably needs a little wider track"): the tread 4.5 m (no solid trunk within 3.5 m of the line, more
// on bends and at the jumps), the small trees left within 6 m of the line breakable (TreeField.breakable, TreeTrunks), and four
// big jumps (MapBuilder.BigJumps.cs).
//   ground   graded into the terrain before anything samples it: along the line, the ground smoothed a little (sigma 1.5 m,
//            the dips and rolls kept) and cut where it is steeper than 22 % (the creek banks at the fords, the climb out of
//            the creek bed), level across the 4.5 m tread, blending back to the woods floor 2.4 to 4.6 m from the line. Never
//            within a meter of a road (the road jump is built separately). In a stream or the creek bed, the tread is raised to
//            0.15 m under the water (a gravel bar: a splash, not a crawl); the rest of the channel keeps its depth
//   jumps    earth mounds in the same ground (tabletops J1, J2, J5, J6, a spoil heap J3), 5 m wide so two can go side by
//            side; J2 and J6 have logs under the lip, their ends showing in the mound's sides
//   J4       the road jump over New Chastain Road: a dirt kicker up the north road bank (a prism, its look the dirt pile of
//            shortcut #6), and on the far side a landing slope graded down the south bank from the road's edge. Short of the
//            speed, a vehicle lands on the road and drives on down the same slope: no wall, nothing to reset from
//            Round 21 (the reverse race): RJ4, J4's mirror, up the south bank to a lip on the south verge, an aimed launch (BigJump)
//            north over the road onto J4's slope; both kickers have a 4 m back down to the verge facing the road (a vehicle short
//            of either drives across and over the other: a hump, never a wall) and are landings (RampHold)
//   tread    a dirt ribbon, 4.3 to 4.7 m wide, wavering, darker in the wheel lines; none on pavement or in the water
//   trees    only the trunks within 'clear_half' of the line go (3.5 m on the trail, wider on bends, at the jumps, fords, grid, J4)
//   landing  zones after each jump (RampHold's landing carry, as on #6)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class CtJump { public string id, kind, look; public float s_start, s_lip, height, table, down, width; }
        [Serializable] class CtCrossing { public string id; public float s; }
        [Serializable] class CtRange { public float s0, s1; }
        [Serializable] class CtRoadJump { public string id; public float[] lip, dir; public float s_lip, ramp_m, lip_above_road_m, width_m, slope_start, slope_lip, landing_from_m, landing_slope, landing_half_m, back_m, rev_lip_m, rev_ramp_m, rev_angle, rev_vmin, rev_vmax; }   // Round 21: back_m .. rev_vmax
        [Serializable] class CtZone { public string id; public float[] poly; }
        [Serializable] class CtClear { public float[] points; public float half; }
        [Serializable] class CtBigJump { public string id, look; public float s_lip, height, lip_y, angle_deg, ramp_m, width, land_from, land_to, vmax, design, vmin; public float[] lip, dir, tracks; public CtHint hint; public float back_angle, rev_angle, rev_vmax; }   // Round 20: vmin (P1J); its follow-ups: tracks and a hint (N2, N5); Round 21: the back and the reverse launch
        [Serializable] class CtHint { public string kind; public float x, z, yaw; }
        [Serializable] class CtFile { public float[] line, clear_half; public float tread_half_m = 1.25f, break_half_m; public CtJump[] jumps; public CtBigJump[] big_jumps; public CtCrossing[] crossings; public CtRange creek_bed; public CtRoadJump road_jump; public CtZone[] landing_zones; public CtClear[] clear_lines; }

        const string CreekTrackFile = "Data/tracks/noonday_creek.json";
        const float TrailMaxGrade = 0.22f, TrailBlend0 = 2.4f, TrailBlend1 = 4.6f, TrailShallow = 0.15f;   // Round 19: blend 1.3 to 3.3 m before (a 2.5 m tread)

        sealed class CreekTrack
        {
            public CtFile Data;
            public List<Vector2> P = new List<Vector2>();
            public float[] Clear;
            public float[] Profile;       // the graded height along the line (before the jumps)
            public int TreesRemoved;
            public readonly List<Vector2> TreesRemovedAt = new List<Vector2>();
            readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
            const float Cell = 16f;
            public int N => P.Count;

            public static CreekTrack Load()
            {
                string path = Path.Combine(ProjectRoot, CreekTrackFile);
                if (!File.Exists(path)) { Debug.Log("[Round18] no " + CreekTrackFile + " (Tools/mapgen/make_creek_track.py): no creek trail"); return null; }
                var t = new CreekTrack { Data = JsonUtility.FromJson<CtFile>(File.ReadAllText(path)) };
                t.P = ToPoints(t.Data.line); t.Clear = t.Data.clear_half;
                for (int i = 0; i < t.N; i++)
                {
                    var a = t.P[i]; var b = t.P[(i + 1) % t.N]; float pad = 8f;
                    for (int x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad) / Cell); x <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + pad) / Cell); x++)
                        for (int z = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad) / Cell); z <= Mathf.FloorToInt((Mathf.Max(a.y, b.y) + pad) / Cell); z++)
                        {
                            var k = new Vector2Int(x, z);
                            if (!t.grid.TryGetValue(k, out var l)) t.grid[k] = l = new List<int>();
                            l.Add(i);
                        }
                }
                return t;
            }

            // the nearest point of the closed line within 8 m: its station (meters from the first point; 1 m apart) and the
            // signed distance (+ = left of travel)
            public bool Near(Vector2 q, out float s, out float lat)
            {
                s = 0f; lat = float.MaxValue;
                if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x / Cell), Mathf.FloorToInt(q.y / Cell)), out var l)) return false;
                float best = float.MaxValue;
                foreach (int i in l)
                {
                    var a = P[i]; var ab = P[(i + 1) % N] - a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                    float d = (a + ab * t - q).sqrMagnitude;
                    if (d < best) { best = d; s = i + t; lat = Cross(ab, q - a) >= 0f ? Mathf.Sqrt(d) : -Mathf.Sqrt(d); }
                }
                return best < 64f;
            }
            public float ClearAt(float s) => Clear[Mathf.Clamp(Mathf.RoundToInt(s), 0, N - 1) % N];
            public float At(float[] a, float s) { s = Mathf.Repeat(s, N); int i = (int)s; return Mathf.Lerp(a[i % N], a[(i + 1) % N], s - i); }
            public Vector2 Point(float s) { s = Mathf.Repeat(s, N); int i = (int)s; return Vector2.Lerp(P[i % N], P[(i + 1) % N], s - i); }
            public Vector2 Tangent(float s) => (Point(s + 1f) - Point(s - 1f)).normalized;
            public static float Gap(float a, float b, int n) { float d = Mathf.Repeat(b - a, n); return d > n * 0.5f ? d - n : d; }   // b - a along the loop

            // Round 19 (item 1; Dan: "If we have to make trees breakable that is ok"): the trees left within break_half_m of the line
            // and those within 8 m of a big jump's straight line from its take-off to 25 m past its landing (a vehicle that runs straight on
            // after landing where the trail bends snaps them instead of stopping dead)
            public bool Breakable(float x, float z)
            {
                if (Data.break_half_m <= 0f) return false;
                var q = new Vector2(x, z);
                if (Near(q, out _, out float lat) && Mathf.Abs(lat) < Data.break_half_m) return true;
                if (Data.big_jumps != null)
                    foreach (var b in Data.big_jumps)
                    {
                        var L = new Vector2(b.lip[0], b.lip[1]); var d = new Vector2(b.dir[0], b.dir[1]).normalized;
                        if (SegDist(q, L - d * b.ramp_m, L + d * (b.land_to + 25f)) < 8f) return true;
                    }
                return false;
            }

            public bool RejectTree(float x, float z)
            {
                var q = new Vector2(x, z);
                bool on = Near(q, out float s, out float lat) && Mathf.Abs(lat) < ClearAt(s);
                // Round 18 (item 4): the ways in and out of P1 (no tread: shortcuts are found, not signposted)
                if (!on && Data.clear_lines != null)
                    foreach (var cl in Data.clear_lines)
                    {
                        var p = ToPoints(cl.points);
                        for (int i = 0; i + 1 < p.Count && !on; i++) on = SegDist(q, p[i], p[i + 1]) < cl.half;
                        if (on) break;
                    }
                if (!on) return false;
                TreesRemoved++; TreesRemovedAt.Add(q);
                return true;
            }

            // the jump mounds' height above the graded trail at station s, lateral 'lat'
            public float Mound(float s, float lat)
            {
                float best = 0f;
                if (Data.jumps == null) return 0f;
                foreach (var j in Data.jumps)
                {
                    float u = Gap(j.s_start, s, N); if (u < -1f || u > j.s_lip - j.s_start + j.table + j.down + 1f) continue;
                    float up = j.s_lip - j.s_start, h;
                    if (u <= up) h = j.height * Mathf.Pow(Mathf.Clamp01(u / up), 1.6f);                // the face: steepening to the lip
                    else if (u <= up + j.table) h = j.height;                                         // the table
                    else h = j.height * Smooth01(1f - (u - up - j.table) / Mathf.Max(0.5f, j.down));   // the landing
                    float half = j.width * 0.5f, side = Mathf.Abs(lat) - half;
                    if (side > 0f) h = Mathf.Max(0f, h - side * 0.9f);                                 // the sides fall away about 42 degrees
                    best = Mathf.Max(best, h);
                }
                return best;
            }
        }

        // ------------------------------------------------------------ the ground (before anything samples it)

        static void GradeCreekTrack(MapData map, TerrainGrid terrain, CreekTrack ct)
        {
            if (ct == null) return;
            int n = ct.N;
            var pave = new Pavement(map);   // roads and drives (not bridges: the creek-bed run passes under them, 6 m down)
            var rj = ct.Data.road_jump;
            bool InRoadJump(float s) => rj != null && CreekTrack.Gap(rj.s_lip - rj.ramp_m - 4f, s, n) >= 0f && CreekTrack.Gap(rj.s_lip - rj.ramp_m - 4f, s, n) <= rj.ramp_m + 4f + 75f;
            // 1. the ground along the line, smoothed a little (sigma 1.5 m), then cut where steeper than TrailMaxGrade
            var g = new float[n];
            for (int i = 0; i < n; i++) g[i] = Ground(terrain, ct.P[i].x, ct.P[i].y);
            var gs = new float[n];
            for (int i = 0; i < n; i++)
            {
                float acc = 0f, w = 0f;
                for (int k = -4; k <= 4; k++) { float ww = Mathf.Exp(-0.5f * k * k / 2.25f); acc += g[(i + k + n) % n] * ww; w += ww; }
                gs[i] = acc / w;
            }
            var p = new float[n];
            for (int i = 0; i < n; i++)
            {
                float best = gs[i];
                for (int k = -40; k <= 40; k++) best = Mathf.Min(best, gs[(i + k + n) % n] + TrailMaxGrade * Mathf.Abs(k));
                p[i] = best;
            }
            ct.Profile = p;
            // 2. on the terrain: the tread and its blend, the mounds, the shallow tread in the water
            Rect area = BoundsOf(ct.P, TrailBlend1 + 6f);
            int changed = 0; float cut = 0f, fill = 0f, shallowed = 0f;
            EditTerrain(terrain, area, (x, z, y) =>
            {
                var q = new Vector2(x, z);
                if (!ct.Near(q, out float s, out float lat)) return y;
                float a = Mathf.Abs(lat);
                float mound = ct.Mound(s, lat);
                if (a > TrailBlend1 && mound <= 0f) return y;
                if (InRoadJump(s)) return y;   // J4: its own build below
                float wRoad = Mathf.Clamp01((pave.Clearance(q, 4f) - 1f) / 2f);
                if (wRoad <= 0f) return y;
                float target = ct.At(p, s);
                float w = (1f - Smooth01((a - TrailBlend0) / (TrailBlend1 - TrailBlend0))) * wRoad;
                float ny = y + (target - y) * w;
                if (mound > 0f) ny = Mathf.Max(ny, target + mound * wRoad);
                // in a stream: the tread a gravel bar 0.15 m under the surface
                float th = ct.Data.tread_half_m;
                if (a < th + 0.35f && Beds != null && Beds.NearStream(q, out float sd, out float surf) && sd < StreamHalf && ny < surf - TrailShallow)
                {
                    float wf = 1f - Smooth01((a - th + 0.15f) / 0.5f);
                    float raised = ny + (surf - TrailShallow - ny) * wf;
                    shallowed = Mathf.Max(shallowed, raised - ny); ny = raised;
                }
                if (Mathf.Abs(ny - y) > 1e-3f) { changed++; cut = Mathf.Min(cut, ny - y); fill = Mathf.Max(fill, ny - y); }
                return ny;
            });
            Debug.Log($"[Round18] Noonday Creek trail graded: {n} m, {changed} terrain samples changed (cut up to {-cut:0.00} m, fill up to {fill:0.00} m, the tread raised up to {shallowed:0.00} m in the water); {ct.Data.jumps?.Length ?? 0} mounds");
            GradeRoadJumpLanding(map, terrain, ct, pave);
        }

        // J4's landing: from 'landing_from_m' past the lip (the road's south edge) the ground falls at 'landing_slope' until it
        // meets the woods floor, 'landing_half_m' either side, blending out over 4 m; never on the road
        static void GradeRoadJumpLanding(MapData map, TerrainGrid terrain, CreekTrack ct, Pavement pave)
        {
            var rj = ct.Data.road_jump; if (rj == null) return;
            var L = new Vector2(rj.lip[0], rj.lip[1]); var d = new Vector2(rj.dir[0], rj.dir[1]).normalized; var nrm = new Vector2(-d.y, d.x);
            var top = L + d * (rj.landing_from_m - 3f); float y0 = Ground(terrain, top.x, top.y);   // the road's height at its south edge
            float most = 0f, len = 0f;
            var area = BoundsOf(new[] { L + d * rj.landing_from_m + nrm * 12f, L + d * rj.landing_from_m - nrm * 12f, L + d * (rj.landing_from_m + 70f) + nrm * 12f, L + d * (rj.landing_from_m + 70f) - nrm * 12f }, 1f);
            EditTerrain(terrain, area, (x, z, y) =>
            {
                var q = new Vector2(x, z); float u = Vector2.Dot(q - L, d) - rj.landing_from_m, v = Mathf.Abs(Vector2.Dot(q - L, nrm));
                if (u < -1f) return y;
                float target = y0 - rj.landing_slope * Mathf.Max(0f, u);
                if (target <= y) return y;   // met the woods floor: done (fill only)
                float w = (1f - Smooth01((v - rj.landing_half_m) / 4f)) * Mathf.Clamp01((pave.Clearance(q, 4f) - 0.3f) / 1.2f);
                if (w <= 0f) return y;
                float ny = y + (target - y) * w; most = Mathf.Max(most, ny - y); len = Mathf.Max(len, u);
                return ny;
            });
            Debug.Log($"[Round18] J4 landing: graded down the south bank from the road's edge ({y0:0.00} m) at {rj.landing_slope * 100f:0} %, {len:0} m long, fill up to {most:0.00} m");
        }

        // ------------------------------------------------------------ what stands on it

        static readonly Color TrailDirt = new Color(0.42f, 0.31f, 0.20f), TrailLitter = new Color(0.36f, 0.30f, 0.19f), LogBark = new Color(0.33f, 0.25f, 0.17f), LogEnd = new Color(0.66f, 0.53f, 0.36f);

        static void BuildCreekTrack(MapData map, TerrainGrid terrain, Materials mats, Transform world, CreekTrack ct)
        {
            if (ct == null) return;
            var root = new GameObject("Noonday Creek trail").transform; root.SetParent(world, false);
            var pave = new Pavement(map);
            var rj = ct.Data.road_jump; int n = ct.N;
            // the tread: a wavering dirt ribbon, three strips across (litter at the edges, dirt, a darker wheel line)
            var tread = new Chunks();
            float[] across = { -1f, -0.62f, -0.2f, 0.2f, 0.62f, 1f };   // Round 19: two wheel lines on the 4.5 m tread
            var run = new List<(Vector3[] row, Vector2 at)>();
            void Flush()
            {
                for (int i = 0; i + 1 < run.Count; i++)
                {
                    var mb = tread.At(run[i].at);
                    for (int k = 0; k + 1 < across.Length; k++)
                    {
                        bool edge = k == 0 || k == across.Length - 2;
                        mb.Tint = edge ? Shade(TrailLitter, 0.95f + 0.1f * Hash01(i, 31 + k)) : Shade(TrailDirt, (k == 1 || k == 3 ? 0.86f : 0.95f) + 0.08f * Hash01(i, 37 + k));
                        Face(mb, run[i].row[k], run[i + 1].row[k], run[i + 1].row[k + 1], run[i].row[k + 1], Vector3.up);
                    }
                }
                run.Clear();
            }
            for (float s = 0f; s <= n; s += 0.75f)
            {
                var c = ct.Point(s); var t = ct.Tangent(s); var nl = new Vector2(-t.y, t.x);
                bool skip = pave.Clearance(c, 4f) < 0.6f || (Beds != null && Beds.Wet(c.x, c.y) && Beds.NearStream(c, out float sd, out _) && sd < StreamHalf - 0.3f)
                            || (rj != null && CreekTrack.Gap(rj.s_lip - rj.ramp_m, s, n) >= -0.5f && CreekTrack.Gap(rj.s_lip - rj.ramp_m, s, n) <= rj.ramp_m + rj.landing_from_m);   // J4: the kicker, the road and its median
                if (skip) { Flush(); continue; }
                float half = ct.Data.tread_half_m + 0.17f * Mathf.Sin(s * 0.21f + 1.1f) + 0.09f * Mathf.Sin(s * 0.67f);
                var row = new Vector3[across.Length];
                for (int k = 0; k < across.Length; k++)
                {
                    float o = across[k] * half + (k == 0 || k == across.Length - 1 ? 0.12f * (Hash01((int)(s * 4f), 41 + k) - 0.5f) : 0f);
                    var q = c + nl * o; row[k] = new Vector3(q.x, Ground(terrain, q.x, q.y) + 0.03f, q.y);
                }
                run.Add((row, c));
            }
            Flush();
            tread.ToObjects("Trail tread", mats.Driveway, root, false);

            // logs under J2's and J6's lips, their ends out of the mound's sides; two more lying beside the take-off
            var logs = new MeshBuilder(); int logCount = 0;
            if (ct.Data.jumps != null)
                foreach (var j in ct.Data.jumps.Where(jj => jj.look == "logs"))
                {
                    var c = ct.Point(j.s_lip - 0.3f); var t = ct.Tangent(j.s_lip); var nl = new Vector2(-t.y, t.x);
                    float half = j.width * 0.5f + j.height * 1.1f + 0.3f;
                    for (int k = 0; k < 3; k++)
                    {
                        var cc = c - t * (k * 0.55f); float r = 0.24f + 0.04f * Hash01(k, 51);
                        float top = ct.At(ct.Profile, j.s_lip - 0.3f - k * 0.55f) + ct.Mound(j.s_lip - 0.3f - k * 0.55f, 0f);
                        var a = cc + nl * half; var b = cc - nl * half;
                        float y = top - r - 0.06f;   // just under the surface across the tread: only the sides show
                        LogCyl(logs, new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), r); logCount++;
                    }
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var at = ct.Point(j.s_start - 3f) + nl * side * (j.width * 0.5f + 1.6f); var dir = (t + nl * side * 0.3f).normalized;
                        var a = at - dir * 2.6f; var b = at + dir * 2.6f; float r = 0.22f;
                        LogCyl(logs, new Vector3(a.x, Ground(terrain, a.x, a.y) + r - 0.05f, a.y), new Vector3(b.x, Ground(terrain, b.x, b.y) + r - 0.05f, b.y), r); logCount++;
                    }
                }
            if (logCount > 0) logs.ToObject("Trail logs", mats.House, root, false);

            // J4: the kicker up the north road bank, a dirt pile like #6's (BuildPrism, kind "kicker")
            if (rj != null)
            {
                var L = new Vector2(rj.lip[0], rj.lip[1]); var d = new Vector2(rj.dir[0], rj.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
                var O = L - d * rj.ramp_m; float y0 = Ground(terrain, O.x, O.y) - 0.02f;
                var onRoad = L + d * 6f; float road = Ground(terrain, onRoad.x, onRoad.y) + RoadLift["primary"] * vScale;
                float y1 = road + rj.lip_above_road_m, a = 2f * (y1 - y0) / rj.ramp_m - rj.slope_lip;
                var prof = new List<float>();
                for (float s = 0f; s <= rj.ramp_m + 1e-3f; s += 1f) { prof.Add(s); prof.Add(y0 + a * s + (rj.slope_lip - a) * s * s / (2f * rj.ramp_m)); }
                float hw = rj.width_m * 0.5f;
                // Round 21: the back, 'back_m' down to the verge on the road's side (the pavement starts 5 m past the lip)
                float back = rj.back_m > 0f ? rj.back_m : 0f; var Lb = L + d * back;
                if (back > 0f) { prof.Add(rj.ramp_m + 0.4f); prof.Add(y1 - 0.03f); prof.Add(rj.ramp_m + back); prof.Add(Ground(terrain, Lb.x, Lb.y) - 0.05f); }
                var poly = new[] { O + nl * hw, O - nl * hw, Lb - nl * hw, Lb + nl * hw };
                var pr = new ScPrism { name = "sc J4 road jump kicker", kind = "kicker", poly = poly.SelectMany(q => new[] { q.x, q.y }).ToArray(), origin = new[] { O.x, O.y }, axis = new[] { d.x, d.y },
                                       profile = prof.ToArray(), colour = new[] { 0.40f, 0.28f, 0.18f }, landing = rj.rev_vmax > 0f };
                BuildPrism(pr, terrain, mats, root);
                if (rj.rev_vmax > 0f)
                {
                    // Round 21: RJ4, the mirror: from the south bank up to a lip on the south verge 'rev_lip_m' past J4's, the same curve and
                    // height over the south carriageway, its own back toward the road; an aimed launch north (vmin to vmax)
                    var RL = L + d * rj.rev_lip_m; var rd = -d; var RO = RL - rd * rj.rev_ramp_m; float ry0 = Ground(terrain, RO.x, RO.y) - 0.02f;
                    var onRoadS = RL + rd * 6f; float roadS = Ground(terrain, onRoadS.x, onRoadS.y) + RoadLift["primary"] * vScale;
                    float ry1 = roadS + rj.lip_above_road_m, ra = 2f * (ry1 - ry0) / rj.rev_ramp_m - rj.slope_lip;
                    var rprof = new List<float>();
                    for (float s = 0f; s <= rj.rev_ramp_m + 1e-3f; s += 1f) { rprof.Add(s); rprof.Add(ry0 + ra * s + (rj.slope_lip - ra) * s * s / (2f * rj.rev_ramp_m)); }
                    var RLb = RL + rd * back;
                    if (back > 0f) { rprof.Add(rj.rev_ramp_m + 0.4f); rprof.Add(ry1 - 0.03f); rprof.Add(rj.rev_ramp_m + back); rprof.Add(Ground(terrain, RLb.x, RLb.y) - 0.05f); }
                    var rnl = new Vector2(-rd.y, rd.x);
                    var rpoly = new[] { RO + rnl * hw, RO - rnl * hw, RLb - rnl * hw, RLb + rnl * hw };
                    BuildPrism(new ScPrism { name = "sc RJ4 reverse road jump kicker", kind = "kicker", poly = rpoly.SelectMany(q => new[] { q.x, q.y }).ToArray(), origin = new[] { RO.x, RO.y }, axis = new[] { rd.x, rd.y },
                                             profile = rprof.ToArray(), colour = new[] { 0.40f, 0.28f, 0.18f }, landing = true }, terrain, mats, root);
                    var rg = new GameObject("RJ4 reverse road jump"); rg.transform.SetParent(root, false);
                    var rb = rg.AddComponent<BigJump>(); rb.id = "RJ4"; rb.look = "road"; rb.lip = new Vector3(RL.x, ry1, RL.y); rb.dir = new Vector3(rd.x, 0f, rd.y); rb.halfWidth = hw;
                    rb.angle = rj.rev_angle; rb.vmin = rj.rev_vmin; rb.vmax = rj.rev_vmax;
                    Debug.Log($"[Round21] RJ4 (J4 in reverse): {rj.rev_ramp_m:0} m from {ry0:0.00} m to the lip at {ry1:0.00} m on the south verge ({rj.rev_lip_m:0.0} m past J4's lip, {rj.lip_above_road_m:0.0} m over the south carriageway at {roadS:0.00} m), an aimed launch at {rj.rev_angle:0} degrees, {rj.rev_vmin:0} to {rj.rev_vmax:0} m/s; both kickers with a {back:0} m back toward the road");
                }
                Debug.Log($"[Round18] J4 kicker: {rj.ramp_m:0} m from {y0:0.00} m to the lip at {y1:0.00} m ({rj.lip_above_road_m:0.0} m over New Chastain Road at {road:0.00} m), {Mathf.Atan(a) * Mathf.Rad2Deg:0} to {Mathf.Atan(rj.slope_lip) * Mathf.Rad2Deg:0} degrees, {rj.width_m:0} m wide");
            }
            // landing zones
            if (ct.Data.landing_zones != null)
                foreach (var z in ct.Data.landing_zones)
                {
                    var go = new GameObject("Landing zone " + z.id); go.transform.SetParent(root, false);
                    go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                }
            BuildBigJumps(terrain, mats, root, ct);   // Round 19 (2): MapBuilder.BigJumps.cs
            Debug.Log($"[Round18] Noonday Creek trail: tread drawn, {logCount} logs, {ct.Data.landing_zones?.Length ?? 0} landing zones; {ct.TreesRemoved} trees removed from the trail, the big jumps' flights and the ways in and out of P1 (Round 19: trunks within 3.5 m of the line, wider on bends, at the jumps, fords, grid and J4)");
        }

        // a log: an 8-sided cylinder between two points, its ends lighter
        static void LogCyl(MeshBuilder mb, Vector3 a, Vector3 b, float r, int seg = 8)
        {
            var axis = (b - a).normalized; var s1 = Vector3.Cross(axis, Vector3.up).normalized; var s2 = Vector3.Cross(s1, axis);
            int st = mb.Count;
            mb.Tint = LogBark;
            for (int k = 0; k < seg; k++)
            {
                float th = Mathf.PI * 2f * k / seg; var o = (s1 * Mathf.Cos(th) + s2 * Mathf.Sin(th)) * r;
                mb.Vertex(a + o); mb.Vertex(b + o);
            }
            for (int k = 0; k < seg; k++)
            {
                int i0 = st + k * 2, i1 = st + ((k + 1) % seg) * 2;
                Vector3 mid = (mb.Verts[i0] + mb.Verts[i1 + 1]) * 0.5f, outN = mid - Vector3.Lerp(a, b, 0.5f) - axis * Vector3.Dot(mid - Vector3.Lerp(a, b, 0.5f), axis);
                if (Vector3.Dot(Vector3.Cross(mb.Verts[i0 + 1] - mb.Verts[i0], mb.Verts[i1] - mb.Verts[i0]), outN) >= 0f) { mb.Tri(i0, i0 + 1, i1); mb.Tri(i1, i0 + 1, i1 + 1); }
                else { mb.Tri(i0, i1, i0 + 1); mb.Tri(i1, i1 + 1, i0 + 1); }
            }
            mb.Tint = LogEnd;
            foreach (var (c, sgn) in new[] { (a, -1f), (b, 1f) })
            {
                int c0 = mb.Vertex(c); int e0 = mb.Count;
                for (int k = 0; k < seg; k++) { float th = Mathf.PI * 2f * k / seg; mb.Vertex(c + (s1 * Mathf.Cos(th) + s2 * Mathf.Sin(th)) * r); }
                for (int k = 0; k < seg; k++)
                {
                    int i0 = e0 + k, i1 = e0 + (k + 1) % seg;
                    if (Vector3.Dot(Vector3.Cross(mb.Verts[i0] - c, mb.Verts[i1] - c), axis * sgn) >= 0f) mb.Tri(c0, i0, i1); else mb.Tri(c0, i1, i0);
                }
            }
        }
    }
}
