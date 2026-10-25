using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 67 (A, Dan, 2026-10-25 03:01: "I want one with a lot of air so I am wondering if we could do the trails of Kennesaw mountain. I want huge jumps
// on this"): the world pieces of the "Kennesaw Mountain" race, from Data/tracks/kennesaw_mountain.json (Tools/mapgen/make_mountain_route.py). World,
// not race layer: they stand in Free Roam too (the race is Data/routes/kennesaw_mountain.json).
//   the bench      the Kennesaw Mountain Trail's line graded level across 12 m (the path itself stays Round 61's 1.5 m tread: its real width), its
//                  profile the ground's smoothed with the six jumps shaped into it: each a real jump, the ground itself (no ramp, no aimed launch)
//   the trees      none within 8 m of the line (the race's room to pass), breakable to 15 m (TreeTrunks: a wide racer snaps them), solid past that
//   the landings   a landing zone down the slope past each lip (RampHold: the speed kept along the ground on touching down)
//   the jumps' looks (look only; the ground under them is what a vehicle meets):
//     overlook   J1, the summit lot's corner: granite rock face and boulders either side of the lip
//     oak        J2, the ridge drop: a huge fallen oak lying across the trail on the lip's back face, its root plate up on one side
//     berm       J3, an old Civil War earthwork: the berm's face held by revetment logs either side of the trail
//     ledge      J4 and J6, boulder ledges: granite boulders along the lip's line beside the bench and slabs on its back face
//     washout    J5, the old timber trail bridge over a wash: its near abutment's cribbing at the lip, broken stringers and planks down in the wash
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class MountainJump { public string id, look, name; public float[] lip, dir, dig; public float lip_y, angle_deg, height, kicker_m, s_lip; }
        [Serializable] class MountainFile { public MountainJump[] jumps; }
        const string MountainTrackFile = "Data/tracks/kennesaw_mountain.json";
        static Ridge MountainNow;            // its graded bench, cleared and breakable lines, landing zones (the Chastain Ridge race's shape)
        static MountainFile MountainExtra;   // the jumps' looks

        // ------------------------------------------------------------ the planning: the bench (after the airport's trail levelling: it replaces it on the race's line)
        static void PlanMountainRace(TerrainGrid terrain)
        {
            MountainNow = Ridge.Load(MountainTrackFile); MountainExtra = null;
            string path = Path.Combine(ProjectRoot, MountainTrackFile);
            if (MountainNow == null || !File.Exists(path)) { Debug.Log("[Round67] no " + MountainTrackFile + ": no Kennesaw Mountain race pieces"); return; }
            MountainExtra = JsonUtility.FromJson<MountainFile>(File.ReadAllText(path));
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
                    go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                }
            int n = 0;
            foreach (var j in MountainExtra?.jumps ?? new MountainJump[0]) { MountainJumpLook(terrain, mats, root, j); n++; }
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
            const float Bench = 6f;
            switch (j.look)
            {
                case "overlook":
                    // the summit's granite: a rock face down the lip's back (slabs lying on it, their tops just under the ground's line), boulders either side
                    for (int i = 0; i < 6; i++) Boulder(1.2f + i * 1.1f, (Hash01(seed + i, 31) - 0.5f) * 9f, 1.1f + 0.5f * Hash01(seed + i, 33), 0.15f, i);
                    for (int i = 0; i < 10; i++) { float side = i % 2 == 0 ? 1f : -1f; Boulder(-3f + i * 1.3f, side * (Bench + 1.2f + 2.5f * Hash01(seed + i, 35)), 1.0f + 0.9f * Hash01(seed + i, 37), 0.7f + 0.6f * Hash01(seed + i, 39), 20 + i); }
                    break;
                case "oak":
                {
                    // the trunk across the trail on the back face, its crown 5 cm under the lip; the root plate up on the left, the top's branches on the right
                    const float R = 0.75f; var c0 = L + d * 1.1f; float cy = j.lip_y - 0.05f - R;
                    var a = c0 + nl * 10f; var e = c0 - nl * 9f; var e2 = c0 - nl * 16f;
                    LogCyl(mb, new Vector3(a.x, cy, a.y), new Vector3(e.x, cy - 0.15f, e.y), R, 12);
                    LogCyl(mb, new Vector3(e.x, cy - 0.15f, e.y), new Vector3(e2.x, Mathf.Max(cy - 1.2f, Ground(terrain, e2.x, e2.y) + 0.35f), e2.y), 0.45f, 10);
                    foreach (var (at, up) in new[] { (-10.5f, 0.6f), (-12.5f, -0.5f), (-14f, 0.4f), (-8f, -0.8f) })
                    { var p = c0 + nl * at; var bp = new Vector3(p.x, cy + 0.1f, p.y); Beam(mb, bp, bp + new Vector3(d.x * up, 1.1f, d.y * up) * 1.8f, 0.2f, 0.2f, LogBark); }
                    var rc = c0 + nl * 10.4f; float rcy = Ground(terrain, rc.x, rc.y) + 1.6f; var ax = new Vector3(nl.x, 0f, nl.y); var u1 = new Vector3(d.x, 0f, d.y);
                    for (int i = 0; i < 14; i++)
                    {
                        float t0 = Mathf.PI * 2f * i / 14f, t1 = Mathf.PI * 2f * (i + 1) / 14f, r0 = 2.3f + 0.5f * Hash01(seed + i, 41), r1 = 2.3f + 0.5f * Hash01(seed + (i + 1) % 14, 41);
                        var cc = new Vector3(rc.x, rcy, rc.y);
                        Vector3 P0 = cc + (u1 * Mathf.Cos(t0) + Vector3.up * Mathf.Sin(t0)) * r0, P1 = cc + (u1 * Mathf.Cos(t1) + Vector3.up * Mathf.Sin(t1)) * r1;
                        P0.y = Mathf.Max(P0.y, Ground(terrain, P0.x, P0.z) - 0.2f); P1.y = Mathf.Max(P1.y, Ground(terrain, P1.x, P1.z) - 0.2f);
                        mb.Tint = Shade(RootEarth, 0.85f + 0.25f * Hash01(seed + i, 43));
                        TriFace(mb, cc - ax * 0.25f, P0 - ax * 0.25f, P1 - ax * 0.25f, -ax); TriFace(mb, cc + ax * 0.3f, P0 + ax * 0.3f, P1 + ax * 0.3f, ax);
                        Face(mb, P0 - ax * 0.25f, P1 - ax * 0.25f, P1 + ax * 0.3f, P0 + ax * 0.3f, (P0 + P1) * 0.5f - cc);
                    }
                    break;
                }
                case "berm":
                    // the earthwork's face: two courses of revetment logs along the lip's back face beside the bench (the trail crosses the berm through a gap)
                    foreach (float side in new[] { 1f, -1f })
                        for (int k = 0; k < 2; k++)
                        {
                            float lat0 = side * (Bench + 0.5f), lat1 = side * (Bench + 9f);
                            var p0 = W(0.6f + k * 0.5f, lat0, 0f); var p1 = W(0.6f + k * 0.5f, lat1, 0f);
                            float y0 = j.lip_y - 0.3f - k * 0.55f, y1 = p1.y + 0.9f - k * 0.55f;
                            LogCyl(mb, new Vector3(p0.x, Mathf.Min(y0, p0.y + 1.2f), p0.z), new Vector3(p1.x, y1, p1.z), 0.28f, 8);
                        }
                    for (int i = 0; i < 6; i++) Boulder(-2f + i * 1.5f, (i % 2 == 0 ? 1f : -1f) * (Bench + 1f + 3f * Hash01(seed + i, 51)), 0.7f + 0.4f * Hash01(seed + i, 53), 0.45f, 60 + i);
                    break;
                case "ledge":
                case "ledge2":
                    for (int i = 0; i < 12; i++) { float side = i % 2 == 0 ? 1f : -1f; Boulder(-1.5f + (i / 2) * 0.9f, side * (Bench + 0.6f + 3.5f * Hash01(seed + i, 61)), 1.1f + 0.8f * Hash01(seed + i, 63), 0.8f + 0.7f * Hash01(seed + i, 65), 80 + i); }
                    for (int i = 0; i < 5; i++) Boulder(1.5f + i * 1.2f, (Hash01(seed + i, 67) - 0.5f) * 8f, 1.0f + 0.4f * Hash01(seed + i, 69), 0.12f, 100 + i);
                    break;
                case "washout":
                {
                    // the near abutment: timber cribbing down the lip's back face, its top a beam flush with the lip; the stringers broken off and lying down in
                    // the wash, planks scattered; the far abutment's stub on the far bank
                    float dig = j.dig != null && j.dig.Length > 1 ? j.dig[1] : 9f;
                    for (int k = 0; k < 4; k++)
                    {
                        var a = W(0.3f + k * 0.35f, -Bench - 1f, 0f); var b = W(0.3f + k * 0.35f, Bench + 1f, 0f); float y = j.lip_y - 0.18f - k * 0.45f;
                        Beam(mb, new Vector3(a.x, y, a.z), new Vector3(b.x, y, b.z), 0.32f, 0.3f, Shade(OldWood, 0.9f + 0.1f * k));
                    }
                    foreach (float lat in new[] { -2.2f, 2.4f })
                    {
                        var a = W(2.5f, lat, 0.25f); var b = W(dig * 0.8f, lat + 0.8f * Mathf.Sign(lat), 0.2f);
                        Beam(mb, a, b, 0.25f, 0.35f, WetWood);
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        var c = W(2f + dig * Hash01(seed + i, 71), (Hash01(seed + i, 73) - 0.5f) * 9f, 0.06f); var dd = new Vector3(d.x + nl.x * (Hash01(seed + i, 75) - 0.5f) * 2f, 0f, d.y + nl.y * (Hash01(seed + i, 75) - 0.5f) * 2f).normalized;
                        Beam(mb, c - dd * 1.3f, c + dd * 1.3f + Vector3.up * 0.15f, 0.28f, 0.06f, Shade(OldWood, 0.8f + 0.25f * Hash01(seed + i, 77)));
                    }
                    for (int k = 0; k < 3; k++) { var a = W(dig + 4f, -Bench - 1f, -0.2f + k * 0.4f); var b = W(dig + 4f, -Bench - 4f, -0.2f + k * 0.4f); Beam(mb, a, b, 0.3f, 0.3f, Shade(WetWood, 0.9f)); }
                    break;
                }
            }
            if (mb.Count > 0) { var lg = mb.ToObject($"Mountain {j.id} {j.look} look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On; }
            Debug.Log($"[Round67] {j.id} {j.name} ({j.look}): the lip at ({L.x:0.0}, {L.y:0.0}) {j.lip_y:0.00} m, {j.angle_deg:0} degrees, {j.height:0.0} m over the ground's line, a {j.kicker_m:0} m kicker in the bench");
        }
    }
}
