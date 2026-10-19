using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 33 (Dan, 2026-10-15 22:49): the world pieces of the "Chastain Ridge" race, from Data/tracks/chastain_ridge.json
// (Tools/mapgen/make_ridge_route.py). World, not race layer: they stand in free roam too (the race itself is
// Data/routes/chastain_ridge.json):
//   RL   the Ridge leap's take-off: a pile of fill dirt at Chastain Ridge Drive's dead end (MapBuilder.BigJumps.cs, look "dirtpile"),
//        an aimed launch over house 706 and the hollow onto New Chastain Road's southeast-bound carriageway
//   S1   a dirt pile in the lot north of the canopy (4) and the shops (6), onto the knoll beside Chastain Corner
//   S2   a dirt pile on the rise south of the self-storage garages (63), over their roof
//   Round 40: the manhole plume on New Chastain Road (the reverse race's way up onto the dead end), the crest cut's boulder wall
//   the landing zones (RampHold's carry), the trees past the landing breakable (a vehicle that runs on across the road at speed
//   snaps them, never a dead stop), the trees and back-yard fences kept off the run-ups, the flights' last meters and the
//   landings, and the way out of the hollow: a ramp graded along the foot of New Chastain Road's embankment (the straight
//   climb up it is 50 % and more), so a vehicle short of the road drives out onto it
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class RidgeClear { public string id; public float[] points; public float half; }
        [Serializable] class RidgeGrade { public string id; public float[] points, y, keep_off, blend_left; public float half, blend, keep_off_m, blend_r; }   // Round 50: blend_left (per point) / blend_r, GradeAlong's sides
        // Round 40 (A): the manhole plume (ManholePlume), the church leap (Round 41: the crypt, MapBuilder.ChurchCrypt.cs), the crest cut's boulder wall
        // (x, z, radius each) and its worn tracks
        [Serializable] class RidgePlume { public string id, group; public float[] at, dir, target, target_dir; public float y, right_m, left_m, half_len_m, angle_deg, trigger_m; }
        // Round 41 (A): the crypt (MapBuilder.ChurchCrypt.cs): its line from the doors to the lip, the floor's heights along it, its stations, the graveyard
        [Serializable] class RidgeCrypt { public int building; public float[] door, line, y, lip_dir, graveyard, gate, graves, trees; public float door_w, door_h, s_top, s_foot, s_west, s_hall, s_bend0, s_bend1, s_portal, lane, hall_w, clear; }
        [Serializable] class RidgeFile { public CtBigJump[] big_jumps; public CtZone[] landing_zones; public RidgeClear[] clear_lines, break_lines; public RidgeGrade[] grades; public RidgePlume[] plumes; public RidgeCrypt crypt; public RidgeRoofRamp roof_ramp; public float[] boulders, crest_tracks; }

        const string RidgeTrackFile = "Data/tracks/chastain_ridge.json";
        static Ridge RidgeNow;   // for the back-yard fences' planning (PlanRestFences: none on a cleared line)

        sealed class Ridge
        {
            public RidgeFile Data;
            public int TreesRemoved;
            // Round 44: any track file of this shape (the Chastain Lakes race's too: MapBuilder.LakesRace.cs)
            public static Ridge Load(string file = RidgeTrackFile)
            {
                string path = Path.Combine(ProjectRoot, file);
                if (!File.Exists(path)) { Debug.Log("[Round33] no " + file + ": no pieces from it"); return null; }
                return new Ridge { Data = JsonUtility.FromJson<RidgeFile>(File.ReadAllText(path)) };
            }
            // on a cleared line (no tree, no back-yard fence)
            public bool Cleared(float x, float z)
            {
                if (Data.clear_lines == null) return false;
                var q = new Vector2(x, z);
                foreach (var cl in Data.clear_lines)
                {
                    var p = ToPoints(cl.points);
                    for (int i = 0; i + 1 < p.Count; i++) if (SegDist(q, p[i], p[i + 1]) < cl.half) return true;
                }
                return false;
            }
            public bool RejectTree(float x, float z) { if (!Cleared(x, z)) return false; TreesRemoved++; return true; }
            // the woods past the Ridge leap's landing: the trees there snap when hit (TreeTrunks), a vehicle that overshoots is never stopped dead
            public bool Breakable(float x, float z)
            {
                if (Data.break_lines == null) return false;
                var q = new Vector2(x, z);
                foreach (var bl in Data.break_lines)
                {
                    var p = ToPoints(bl.points);
                    for (int i = 0; i + 1 < p.Count; i++) if (SegDist(q, p[i], p[i + 1]) < bl.half) return true;
                }
                return false;
            }
        }

        // the grades, before anything samples the ground: along each line the ground set to a straight climb between its two
        // heights across 'half', blending back over 'blend'; never within keep_off_m of the keep_off line (the carriageway)
        static void GradeRidge(TerrainGrid terrain, Ridge ridge)
        {
            if (ridge?.Data.grades == null) return;
            foreach (var gr in ridge.Data.grades)
            {
                if (gr.y != null && gr.y.Length > 2 && gr.y.Length * 2 == gr.points.Length) { GradeAlong(terrain, gr); continue; }   // Round 40: a height per point
                var p = ToPoints(gr.points); var a = p[0]; var b = p[p.Count - 1]; var ab = b - a; float len = ab.magnitude;
                var keep = gr.keep_off != null ? ToPoints(gr.keep_off) : new List<Vector2>();
                float reach = gr.half + gr.blend, most = 0f; int n = 0;
                var area = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - reach, Mathf.Min(a.y, b.y) - reach, Mathf.Max(a.x, b.x) + reach, Mathf.Max(a.y, b.y) + reach);
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    var q = new Vector2(x, z);
                    float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / (len * len)), d = (a + ab * t - q).magnitude;
                    if (d >= reach) return y;
                    float w = d <= gr.half ? 1f : Smooth01(1f - (d - gr.half) / gr.blend);
                    if (keep.Count > 1)
                    {
                        float dk = float.MaxValue; for (int i = 0; i + 1 < keep.Count; i++) dk = Mathf.Min(dk, SegDist(q, keep[i], keep[i + 1]));
                        w *= Smooth01((dk - gr.keep_off_m) / 1.5f);
                    }
                    if (w <= 1e-3f) return y;
                    float target = Mathf.Lerp(gr.y[0], gr.y[1], t), ny = y + w * (target - y);
                    most = Mathf.Max(most, Mathf.Abs(ny - y)); n++;
                    return ny;
                });
                Debug.Log($"[Round33] {gr.id}: graded {len:0} m from {gr.y[0]:0.0} m to {gr.y[1]:0.0} m ({(gr.y[1] - gr.y[0]) / len * 100f:0} %), {gr.half * 2f:0} m wide; {n} samples moved, by up to {most:0.0} m");
            }
        }

        // Round 40 (A6): along a line with a height at each point (the crest cut's smoothed profile): the ground within 'half' of the line set to the
        // line's height there, blending back over 'blend'. Round 50 (B, the lake trail's bench): a blend of its own on each side when given, per
        // point on the left (blend_left: the lake leaves less room), blend_r on the right
        static void GradeAlong(TerrainGrid terrain, RidgeGrade gr)
        {
            var p = ToPoints(gr.points);
            bool sides = gr.blend_left != null && gr.blend_left.Length == p.Count;
            float br = gr.blend_r > 0f ? gr.blend_r : gr.blend;
            float reach = gr.half + Mathf.Max(gr.blend, Mathf.Max(br, sides ? gr.blend_left.Max() : 0f)), most = 0f; int n = 0;
            var area = Rect.MinMaxRect(p.Min(q => q.x) - reach, p.Min(q => q.y) - reach, p.Max(q => q.x) + reach, p.Max(q => q.y) + reach);
            EditTerrain(terrain, area, (x, z, y) =>
            {
                var q = new Vector2(x, z); float best = float.MaxValue, target = y, blend = gr.blend;
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    var ab = p[i + 1] - p[i]; float t = Mathf.Clamp01(Vector2.Dot(q - p[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (p[i] + ab * t - q).magnitude;
                    if (d < best)
                    {
                        best = d; target = Mathf.Lerp(gr.y[i], gr.y[i + 1], t);
                        if (sides) { var v = q - p[i]; blend = ab.x * v.y - ab.y * v.x > 0f ? Mathf.Lerp(gr.blend_left[i], gr.blend_left[i + 1], t) : br; }   // > 0: on the left
                    }
                }
                if (best >= gr.half + blend) return y;
                float w = best <= gr.half ? 1f : Smooth01(1f - (best - gr.half) / blend), ny = y + w * (target - y);
                most = Mathf.Max(most, Mathf.Abs(ny - y)); n++; return ny;
            });
            Debug.Log($"[Round40] {gr.id}: graded along {p.Count} points, {gr.half * 2f:0} m wide; {n} samples moved, by up to {most:0.00} m");
        }

        static void BuildRidge(TerrainGrid terrain, Materials mats, Transform world, Ridge ridge)
        {
            if (ridge == null) return;
            var root = new GameObject("Chastain Ridge").transform; root.SetParent(world, false);
            if (ridge.Data.landing_zones != null)
                foreach (var z in ridge.Data.landing_zones)
                {
                    var go = new GameObject("Landing zone " + z.id); go.transform.SetParent(root, false);
                    go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                }
            BuildBigJumps(terrain, mats, root, ridge.Data.big_jumps, "Dirt piles");
            // Round 40 (A1 / A2): the manhole plume replaces Round 39's road-work climb (its dirt, cones and barricade are gone)
            if (ridge.Data.plumes != null) foreach (var pl in ridge.Data.plumes) BuildPlume(terrain, mats, root, pl);
            BuildCrestCut(terrain, mats, root, ridge);   // Round 40 (A6)
            BuildRoofRamp(terrain, mats, root, ridge);   // Round 41 (B): the roofer's ramp on house 1092 (MapBuilder.Round41.cs)
            Debug.Log($"[Round33] Chastain Ridge: {ridge.Data.big_jumps?.Length ?? 0} dirt piles, {ridge.Data.landing_zones?.Length ?? 0} landing zones; {ridge.TreesRemoved} trees kept off the run-ups, flights' last meters, landings and the way out of the hollow");
        }
        // Round 40 (A1 / A2): a street manhole's cover, flush on the carriageway (the street's own look: a dark iron disc with a rim and a grid, no
        // collider: the road under it is the terrain's), on its own object so it can fly (ManholeLid with no lid collider), and the plume
        static void BuildPlume(TerrainGrid terrain, Materials mats, Transform root, RidgePlume pl)
        {
            var c = new Vector3(pl.at[0], Ground(terrain, pl.at[0], pl.at[1]), pl.at[1]);
            var along = new Vector3(pl.dir[0], 0f, pl.dir[1]).normalized;
            var go = new GameObject("Manhole plume " + pl.id); go.transform.SetParent(root, false); go.transform.position = c + Vector3.up * 0.02f;
            var mb = new MeshBuilder(); var iron = new Color(0.19f, 0.19f, 0.20f); var rim = new Color(0.30f, 0.29f, 0.28f);
            // the street's grade under it (the cover lies in the street's own plane)
            float gx = Ground(terrain, c.x + .5f, c.z) - Ground(terrain, c.x - .5f, c.z), gz = Ground(terrain, c.x, c.z + .5f) - Ground(terrain, c.x, c.z - .5f);
            Vector3 P(float x, float z, float h) => new Vector3(x, x * gx + z * gz + h - 0.02f, z);   // local to the plume (its cover turns about its middle)
            const int N = 20;
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                mb.Tint = rim; Face(mb, P(Mathf.Cos(a0) * .45f, Mathf.Sin(a0) * .45f, .012f), P(Mathf.Cos(a1) * .45f, Mathf.Sin(a1) * .45f, .012f), P(Mathf.Cos(a1) * .52f, Mathf.Sin(a1) * .52f, .006f), P(Mathf.Cos(a0) * .52f, Mathf.Sin(a0) * .52f, .006f), Vector3.up);
                mb.Tint = iron; TriFace(mb, P(0f, 0f, .016f), P(Mathf.Cos(a1) * .45f, Mathf.Sin(a1) * .45f, .014f), P(Mathf.Cos(a0) * .45f, Mathf.Sin(a0) * .45f, .014f), Vector3.up);
            }
            mb.Tint = new Color(0.14f, 0.14f, 0.15f);
            for (float k = -.3f; k <= .31f; k += .15f) Face(mb, P(k - .015f, -.32f, .018f), P(k + .015f, -.32f, .018f), P(k + .015f, .32f, .018f), P(k - .015f, .32f, .018f), Vector3.up);
            var disc = mb.ToObject("Manhole cover " + pl.id, mats.House, go.transform, false, false);
            var lid = go.AddComponent<ManholeLid>(); lid.lid = disc.transform; lid.outward = along; lid.radius = 1f; lid.slope = new Vector2(gx, gz);
            var mp = go.AddComponent<ManholePlume>(); mp.id = pl.id; mp.cover = lid; mp.along = along; mp.right = pl.right_m; mp.left = pl.left_m; mp.halfLength = pl.half_len_m;
            mp.angle = pl.angle_deg; mp.trigger = pl.trigger_m; mp.target = new Vector3(pl.target[0], pl.target[1], pl.target[2]); mp.targetDir = new Vector3(pl.target_dir[0], 0f, pl.target_dir[1]);
            mp.sprayMaterial = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial();
            mp.group = string.IsNullOrEmpty(pl.group) ? pl.id : pl.group;   // Round 43 (A1): covers in one group blow together
            Debug.Log($"[Round40] manhole plume {pl.id} at ({c.x:0.0}, {c.z:0.0}) {c.y:0.00} m: across the carriageway {pl.right_m:0} m right, {pl.left_m:0} m left, {pl.half_len_m * 2f:0} m along; throws at {pl.angle_deg:0} deg to ({pl.target[0]:0.0}, {pl.target[1]:0.0}, {pl.target[2]:0.0}); blows {pl.trigger_m:0} m before the first racer");
        }

        // Round 40 (A6): the crest cut's boulder wall across the back yards (solid: sphere colliders, the boulders sunk a third into the ground)
        // with its one gap, and worn tire tracks from the drive into the gap between houses 1245 and 1076 (the hint)
        static void BuildCrestCut(TerrainGrid terrain, Materials mats, Transform root, Ridge ridge)
        {
            var b = ridge.Data.boulders; if (b == null || b.Length < 3) return;
            var wall = new GameObject("Crest cut boulder wall").transform; wall.SetParent(root, false);
            var mb = new MeshBuilder(); int n = 0;
            for (int i = 0; i + 2 < b.Length; i += 3)
            {
                float x = b[i], z = b[i + 1], r = b[i + 2], g = Ground(terrain, x, z);
                var c = new Vector3(x, g + r * 0.35f, z);
                mb.Tint = Shade(Granite, 0.62f + 0.3f * Hash01(i, 4001));
                Rock(mb, c, r, i);
                var col = new GameObject("boulder"); col.transform.SetParent(wall, false); col.transform.position = c;
                col.AddComponent<SphereCollider>().radius = r * 0.98f; n++;
            }
            var lg = mb.ToObject("boulders", mats.House, wall, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (ridge.Data.crest_tracks != null && ridge.Data.crest_tracks.Length >= 6) WornRuts(terrain, mats, wall, ToPoints(ridge.Data.crest_tracks), "Crest cut worn tracks");
            Debug.Log($"[Round40] crest cut: {n} boulders in the wall across the back yards; worn tracks into the gap between 1245 and 1076");
        }
        // a rough low-poly rock: a squashed ball, its corners pushed in and out a little
        static void Rock(MeshBuilder mb, Vector3 c, float r, int seed)
        {
            const int Rings = 4, Seg = 7;
            var v = new Vector3[Rings + 1, Seg];
            for (int a = 0; a <= Rings; a++)
                for (int k = 0; k < Seg; k++)
                {
                    float th = Mathf.PI * a / Rings, ph = Mathf.PI * 2f * k / Seg + a * 0.4f, j = 0.85f + 0.3f * Hash01(seed * 31 + a * 7 + k, 4003);
                    v[a, k] = c + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * r * j, Mathf.Cos(th) * r * 0.8f * j, Mathf.Sin(th) * Mathf.Sin(ph) * r * j);
                }
            for (int a = 0; a < Rings; a++)
                for (int k = 0; k < Seg; k++)
                {
                    Vector3 p00 = v[a, k], p01 = v[a, (k + 1) % Seg], p10 = v[a + 1, k], p11 = v[a + 1, (k + 1) % Seg];
                    var nrm = ((p00 + p01 + p10 + p11) * 0.25f - c).normalized;
                    TriFace(mb, p00, p10, p01, nrm); TriFace(mb, p01, p10, p11, nrm);
                }
        }
    }
}
