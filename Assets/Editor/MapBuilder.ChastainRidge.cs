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
//   the landing zones (RampHold's carry), the trees past the landing breakable (a vehicle that runs on across the road at speed
//   snaps them, never a dead stop), the trees and back-yard fences kept off the run-ups, the flights' last meters and the
//   landings, and the way out of the hollow: a ramp graded along the foot of New Chastain Road's embankment (the straight
//   climb up it is 50 % and more), so a vehicle short of the road drives out onto it
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class RidgeClear { public string id; public float[] points; public float half; }
        [Serializable] class RidgeGrade { public string id; public float[] points, y, keep_off; public float half, blend, keep_off_m; }
        [Serializable] class RidgeFile { public CtBigJump[] big_jumps; public CtZone[] landing_zones; public RidgeClear[] clear_lines, break_lines; public RidgeGrade[] grades; }

        const string RidgeTrackFile = "Data/tracks/chastain_ridge.json";
        static Ridge RidgeNow;   // for the back-yard fences' planning (PlanRestFences: none on a cleared line)

        sealed class Ridge
        {
            public RidgeFile Data;
            public int TreesRemoved;
            public static Ridge Load()
            {
                string path = Path.Combine(ProjectRoot, RidgeTrackFile);
                if (!File.Exists(path)) { Debug.Log("[Round33] no " + RidgeTrackFile + " (Tools/mapgen/make_ridge_route.py): no Chastain Ridge pieces"); return null; }
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
            // Round 39 (C): THE RIDGE CLIMB is road-work fill: traffic cones along its foot and sides, a road-closed barricade beside it
            var climb = ridge.Data.big_jumps?.FirstOrDefault(j => j.id == "CLIMB");
            if (climb != null) RoadWorkDressing(terrain, mats, root, climb);
            Debug.Log($"[Round33] Chastain Ridge: {ridge.Data.big_jumps?.Length ?? 0} dirt piles, {ridge.Data.landing_zones?.Length ?? 0} landing zones; {ridge.TreesRemoved} trees kept off the run-ups, flights' last meters, landings and the way out of the hollow");
        }
        // Round 39 (C): cones and a standing barricade round a dirt pile (look only, no colliders: nothing to snag a vehicle on its run-up)
        static void RoadWorkDressing(TerrainGrid terrain, Materials mats, Transform parent, CtBigJump b)
        {
            var L = new Vector2(b.lip[0], b.lip[1]); var d = new Vector2(b.dir[0], b.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
            var F = L - d * b.ramp_m; float hw = b.width * 0.5f; var mb = new MeshBuilder(); int cones = 0;
            foreach (float side in new[] { -1f, 1f })
                for (float s = -1.5f; s <= b.ramp_m * 0.6f; s += 2.5f) { Cone(mb, terrain, F + d * s + nl * side * (hw + 1.6f + 0.4f * Hash01(cones, 39))); cones++; }
            var bar = F - d * 1f + nl * (hw + 4.5f); float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            StandingBarricade(mb, terrain, bar, yaw);
            var go = mb.ToObject(b.id + " road work", mats.House, parent, false); go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Debug.Log($"[Round39] {b.id}: road-work fill: {cones} cones along its foot and sides, a road-closed barricade at ({bar.x:0}, {bar.y:0})");
        }
        static void Cone(MeshBuilder mb, TerrainGrid terrain, Vector2 at)
        {
            var c = new Vector3(at.x, Ground(terrain, at.x, at.y), at.y);
            Beam(mb, c, c + Vector3.up * 0.04f, 0.38f, 0.38f, new Color(0.08f, 0.08f, 0.08f));
            float[] w = { 0.26f, 0.2f, 0.15f, 0.1f, 0.06f }; Color[] col = { BarrierOrange, BarrierWhite, BarrierOrange, BarrierWhite, BarrierOrange };
            for (int i = 0; i < w.Length; i++) Beam(mb, c + Vector3.up * (0.04f + 0.13f * i), c + Vector3.up * (0.04f + 0.13f * (i + 1)), w[i], w[i], col[i]);
        }
        // a type III road-closed barricade standing: two striped boards on A-frame legs
        static void StandingBarricade(MeshBuilder mb, TerrainGrid terrain, Vector2 at, float yawDeg)
        {
            float g = Ground(terrain, at.x, at.y), a = yawDeg * Mathf.Deg2Rad;
            var u = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); var n = new Vector3(u.z, 0f, -u.x); var c = new Vector3(at.x, g, at.y);
            foreach (float h in new[] { 0.65f, 1.05f })
            {
                var b0 = c - u * 1.2f + Vector3.up * h;
                for (int i = 0; i < 6; i++) Beam(mb, b0 + u * (0.4f * i), b0 + u * (0.4f * (i + 1)), 0.04f, 0.25f, i % 2 == 0 ? BarrierOrange : BarrierWhite, n);
            }
            foreach (float s in new[] { -0.9f, 0.9f })
                foreach (float f in new[] { -1f, 1f })
                    Beam(mb, c + u * s + n * f * 0.35f, c + u * s + Vector3.up * 1.2f, 0.06f, 0.06f, BarrierWhite);
        }
    }
}
