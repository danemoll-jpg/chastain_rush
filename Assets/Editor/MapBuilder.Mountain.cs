using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 67 (A, Dan, 2026-10-25 03:01: "I want one with a lot of air so I am wondering if we could do the trails of Kennesaw mountain. I want huge jumps
// on this"): the world pieces of the "Kennesaw Mountain" race, from Data/tracks/kennesaw_mountain.json (Tools/mapgen/make_mountain_route.py). World,
// not race layer: they stand in Free Roam too (the race is Data/routes/kennesaw_mountain.json).
// Round 68 (Dan, 2026-10-25 08:35: "these are just little bunny hops ... I had bigger jumps in my own neighborhood than I got in the mountains"): the
// race comes down the mountain on three straight lines cut through the woods, each built round one huge jump designed from its flight:
//   the bench      the lines graded level across 14 m (22 m through the hairpins, 26 m over the catch slopes), their profile the ground's smoothed with
//                  each jump's kicker and catch slope in it: real jumps, the ground itself (no ramp object, no aimed launch); cut and fill banks 1.25 m
//                  wide for every meter of cut or fill (the grade's per-point blends)
//   the trees      none within 13 m of the lines or 17 m of a flight's corridor, breakable to 32 / 38 m (TreeTrunks), solid past that
//   the landings   a landing zone over each catch slope (RampHold: the speed kept along the ground on touching down)
//   the jumps' looks (look only; the ground under them is what a vehicle meets), dressing each kicker's 40 degree back face and its banks:
//     overlook   J1, the summit flight: the granite of the mountain's top, slabs set into the back face, boulders along the kicker's banks
//     ledge      J2, the west face: a boulder ledge at the shoulder's brink, the same way
//     berm       J3, the earthworks: an old earthwork's timber revetment down the back face, boulders along its banks
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class MountainJump { public string id, look, name; public float[] lip, dir, dig; public float lip_y, angle_deg, height, kicker_m, s_lip; }
        [Serializable] class MountainFile { public MountainJump[] jumps; }
        const string MountainTrackFile = "Data/tracks/kennesaw_mountain.json";
        static Ridge MountainNow;            // its graded lines, cleared and breakable lines, landing zones (the Chastain Ridge race's shape)
        static MountainFile MountainExtra;   // the jumps' looks

        // ------------------------------------------------------------ the planning: the bench (after the airport's trail levelling: it replaces it on the race's line)
        static void PlanMountainRace(TerrainGrid terrain)
        {
            MountainNow = Ridge.Load(MountainTrackFile); MountainExtra = null;
            string path = Path.Combine(ProjectRoot, MountainTrackFile);
            if (MountainNow == null || !File.Exists(path)) { Debug.Log("[Round67] no " + MountainTrackFile + ": no Kennesaw Mountain race pieces"); return; }
            MountainExtra = JsonUtility.FromJson<MountainFile>(File.ReadAllText(path));
            MergeSummitRamp(MountainNow);   // Round 70 (A): the summit ramp's grades, lines and catch zone with the mountain's (MapBuilder.SummitRamp.cs)
            MergeAirLanes(MountainNow);   // Round 71: the air race's low flight lanes cleared of trees (MapBuilder.SummitRamp.cs)
            MergeQuarryJumps(MountainNow);   // Round 72: the Viking Quarry jumps' run-out grades, cleared lines and catch zones (MapBuilder.QuarryJumps.cs)
            // Round 68: where the lines run on or beside the drive (the departure below the summit, the join by the visitor center) the drive's own
            // levelled surface is kept: those points take the ground as built so far (the drive is levelled before this), eased into the line's profile
            foreach (var gr in MountainNow.Data.grades ?? new RidgeGrade[0])
            {
                if (gr.y == null || gr.y.Length * 2 != gr.points.Length || (gr.ground_first <= 0 && gr.ground_last <= 0)) continue;
                var p = ToPoints(gr.points); int n = p.Count; float ease = gr.ground_ease_m > 0f ? gr.ground_ease_m : 40f;
                var built = p.Select(q => Ground(terrain, q.x, q.y)).ToArray(); var y0 = (float[])gr.y.Clone();
                float along = 0f;
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) along += (p[i] - p[i - 1]).magnitude;
                    if (i < gr.ground_first) gr.y[i] = built[i];
                    else if (gr.ground_first > 0)
                    {
                        float d = along - Enumerable.Range(1, gr.ground_first - 1).Sum(k => (p[k] - p[k - 1]).magnitude), w = Smooth01(Mathf.Clamp01(d / ease));
                        if (d < ease) gr.y[i] = Mathf.Lerp(built[i], y0[i], w);
                    }
                }
                along = 0f;
                for (int i = n - 1; i >= 0; i--)
                {
                    if (i < n - 1) along += (p[i] - p[i + 1]).magnitude;
                    int last = n - gr.ground_last;
                    if (gr.ground_last <= 0) break;
                    if (i >= last) gr.y[i] = built[i];
                    else
                    {
                        float d = along - Enumerable.Range(last, n - 1 - last).Sum(k => (p[k + 1] - p[k]).magnitude), w = Smooth01(Mathf.Clamp01(d / ease));
                        if (d < ease) gr.y[i] = Mathf.Lerp(built[i], gr.y[i], w);
                    }
                }
                // and those ends smoothed along the line (a Gaussian of 6 m): the drive's height and the line's met in a crest (the first exe run: 0.55 m
                // over 24 m, a car at 33 m/s and more left the ground there)
                int reach = Mathf.Min(n, gr.ground_first + 60), from = Mathf.Max(0, n - gr.ground_last - 60);
                var cum = new float[n]; for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + (p[i] - p[i - 1]).magnitude;
                var ys = (float[])gr.y.Clone();
                for (int i = 0; i < n; i++)
                {
                    if (!(i < reach && gr.ground_first > 0) && !(i >= from && gr.ground_last > 0)) continue;
                    float sw = 0f, sy = 0f;
                    for (int k = i; k >= 0 && cum[i] - cum[k] < 18f; k--) { float w = Mathf.Exp(-Mathf.Pow((cum[i] - cum[k]) / 6f, 2f) / 2f); sw += w; sy += w * ys[k]; }
                    for (int k = i + 1; k < n && cum[k] - cum[i] < 18f; k++) { float w = Mathf.Exp(-Mathf.Pow((cum[k] - cum[i]) / 6f, 2f) / 2f); sw += w; sy += w * ys[k]; }
                    gr.y[i] = sy / sw;
                }
                Debug.Log($"[Round68] {gr.id}: its first {gr.ground_first} and last {gr.ground_last} points take the drive's built height, eased over {ease:0} m");
            }
            GradeRidge(terrain, MountainNow);
            Debug.Log($"[Round67] Kennesaw Mountain: {MountainNow.Data.grades?.Length ?? 0} graded stretches, {MountainExtra?.jumps?.Length ?? 0} jumps shaped into the bench");
        }

        // ------------------------------------------------------------ the step
        static void BuildMountainRace(TerrainGrid terrain, Materials mats, Transform world)
        {
            if (MountainNow == null) return;
            var root = new GameObject("Kennesaw Mountain race (Round 67)").transform; root.SetParent(world, false);
            if (MountainNow.Data.landing_zones != null)
                foreach (var z in MountainNow.Data.landing_zones)
                {
                    var go = new GameObject("Landing zone " + z.id); go.transform.SetParent(root, false);
                    var lz = go.AddComponent<LandingZone>(); lz.outline = ToPoints(z.poly).ToArray(); lz.catchSlope = true;   // Round 68: catch slopes
                }
            int n = 0;
            foreach (var j in MountainExtra?.jumps ?? new MountainJump[0]) { MountainJumpLook(terrain, mats, root, j); n++; }
            // Round 72 (B4, found by Round 68's check: the Drifter Twin flat out lost 12 m/s on J2's kicker, its body pressed into the kicker's top, impulse
            // about 3900): over J2's kicker the ground does not push on an upright body (BodyGlide, as Elaine's climb, Round 56); the wheels carry it
            foreach (var j in MountainExtra?.jumps ?? new MountainJump[0])
            {
                if (j.id != "J2") continue;
                var lp = new Vector2(j.lip[0], j.lip[1]); var dd = new Vector2(j.dir[0], j.dir[1]).normalized; var nn = new Vector2(-dd.y, dd.x);
                var corners = new[] { lp + dd * 2f + nn * 10f, lp + dd * 2f - nn * 10f, lp - dd * (j.kicker_m + 10f) + nn * 10f, lp - dd * (j.kicker_m + 10f) - nn * 10f };
                var gg = new GameObject("J2 kicker body glide (Round 72)"); gg.transform.SetParent(root, false);
                gg.AddComponent<BodyGlide>().rects = new[] { new Vector4(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y)) };
            }
            BuildSummitRamp(terrain, mats, root);   // Round 70 (A)
            BuildQuarryJumps(terrain, mats, root);   // Round 72 (A): the Viking Quarry jumps
            Debug.Log($"[Round67] Kennesaw Mountain: {n} jumps dressed, {MountainNow.Data.landing_zones?.Length ?? 0} landing zones; {MountainNow.TreesRemoved} trees kept off the bench and the summit lot");
        }

        static void MountainJumpLook(TerrainGrid terrain, Materials mats, Transform root, MountainJump j)
        {
            var L = new Vector2(j.lip[0], j.lip[1]); var d = new Vector2(j.dir[0], j.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
            var go = new GameObject($"{j.id} {j.name}").transform; go.SetParent(root, false);
            var mb = new MeshBuilder(); int seed = j.id.GetHashCode() & 0xffff;
            Vector3 W(float s, float lat, float up) { var q = L + d * s + nl * lat; return new Vector3(q.x, Ground(terrain, q.x, q.y) + up, q.y); }
            // a boulder, sunk most of the way: what shows is never more than 'show' over the ground
            void Boulder(float s, float lat, float r, float show, int k)
            {
                var c = W(s, lat, show - r * 0.8f); mb.Tint = Shade(Granite, 0.62f + 0.3f * Hash01(seed + k, 4011)); Rock(mb, c, r, seed + k);
            }
            const float Bench = 7f;
            // the back face: from the lip down at 40 degrees to the ground (MakeMountain's profile), 'face' m long
            float face = Mathf.Max(1f, j.height / Mathf.Tan(40f * Mathf.Deg2Rad));
            float FaceY(float sx) => j.lip_y - Mathf.Tan(40f * Mathf.Deg2Rad) * sx;
            switch (j.look)
            {
                case "overlook":
                case "ledge":
                    // slabs set into the face (their tops just proud of it), in rows across the bench and onto its banks
                    for (float sx = 0.8f; sx < face; sx += 2.1f)
                        for (float lat = -Bench - 3f; lat <= Bench + 3f; lat += 2.6f)
                        {
                            int k = (int)(sx * 7f) * 31 + (int)(lat * 3f + 40f); float r = 1.0f + 0.6f * Hash01(seed + k, 31);
                            var q = L + d * (sx + (Hash01(seed + k, 33) - 0.5f) * 0.8f) + nl * (lat + (Hash01(seed + k, 35) - 0.5f) * 0.9f);
                            float y = Mathf.Min(FaceY(sx), Ground(terrain, q.x, q.y)) + 0.25f - r * 0.85f;
                            mb.Tint = Shade(Granite, 0.6f + 0.32f * Hash01(seed + k, 37)); Rock(mb, new Vector3(q.x, y, q.y), r, seed + k);
                        }
                    // boulders along the kicker's banks and either side of the face, standing up to 1.5 m
                    for (int i = 0; i < 28; i++)
                    {
                        float side = i % 2 == 0 ? 1f : -1f, sx = -j.kicker_m + (i / 2) * ((j.kicker_m + face + 4f) / 14f);
                        Boulder(sx, side * (Bench + 1.6f + 3f * Hash01(seed + i, 41)), 1.1f + 0.9f * Hash01(seed + i, 43), 0.7f + 0.8f * Hash01(seed + i, 45), 200 + i);
                    }
                    break;
                case "berm":
                {
                    // the revetment: logs laid across the face every 0.6 m of height, a little longer than the bench, their ends into the banks
                    float step = 0.6f / Mathf.Tan(40f * Mathf.Deg2Rad);
                    for (float sx = 0.25f; sx < face; sx += step)
                    {
                        var a = L + d * sx + nl * (Bench + 3.5f); var b = L + d * sx - nl * (Bench + 3.5f); float y = FaceY(sx) + 0.12f;
                        LogCyl(mb, new Vector3(a.x, Mathf.Min(y, Ground(terrain, a.x, a.y) + 0.3f), a.y), new Vector3(b.x, Mathf.Min(y, Ground(terrain, b.x, b.y) + 0.3f), b.y), 0.3f, 8);
                    }
                    // the earthwork's posts at the lip's corners and boulders along its banks
                    foreach (float side in new[] { 1f, -1f }) { var c = W(0.2f, side * (Bench + 0.4f), 0f); Beam(mb, c - Vector3.up * 0.4f, c + Vector3.up * 1.1f, 0.35f, 0.35f, OldWood); }
                    for (int i = 0; i < 20; i++)
                    {
                        float side = i % 2 == 0 ? 1f : -1f, sx = -j.kicker_m + (i / 2) * ((j.kicker_m + face + 4f) / 10f);
                        Boulder(sx, side * (Bench + 1.8f + 3f * Hash01(seed + i, 51)), 0.8f + 0.5f * Hash01(seed + i, 53), 0.5f + 0.4f * Hash01(seed + i, 55), 300 + i);
                    }
                    break;
                }
            }
            if (mb.Count > 0) { var lg = mb.ToObject($"Mountain {j.id} {j.look} look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On; }
            Debug.Log($"[Round68] {j.id} {j.name} ({j.look}): the lip at ({L.x:0.0}, {L.y:0.0}) {j.lip_y:0.00} m, {j.angle_deg:0} degrees, {j.height:0.0} m over the ground's line, a {j.kicker_m:0} m kicker in the bench, its back face {face:0.0} m");
        }
    }
}
