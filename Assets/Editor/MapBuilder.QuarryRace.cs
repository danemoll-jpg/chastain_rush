using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 90 (Dan, 2026-11-01 11:37: "The quarry - I wanted a car race with huge jumps over the lake"): the world pieces of the "Viking Quarry" race,
// from Data/tracks/viking_quarry.json (Tools/mapgen/make_quarry_route.py: the lap, why it goes where it goes). World, not race layer, except the pit
// dividers: they stand in Free Roam too (the race is Data/routes/viking_quarry.json; its jumps are Free Roam's three quarry jumps, MapBuilder.QuarryJumps.cs).
//   the benches   the low haul bench (down the north bank, round the west end, up the south cliff, through a cutting in the rim), the haul road up the
//                 east wall, the high haul bench up the north and north-west walls, and the gravel roads across the plateau: graded in the mountain's
//                 planning (their grades merged with it, as the quarry jumps' are), the trees cleared off them
//   the gravel    a crushed-stone surface 16 m wide on each of them
//   the berms     a low rock berm along every edge the ground falls 3 m or more beyond (a haul road's safety berm): a guide wall (WallGlance) that
//                 turns a glancing car along it
//   the dividers  race time only: rows of barriers across the pit floor between the three landings' legs (a car landing one jump cannot drive across
//                 to another leg of the lap)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class QrLine { public string id; public float[] points; public float half, lat; public int side; }
        [Serializable] class QrFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines; public CtZone[] landing_zones; public QrLine[] surfaces, berms, dividers; public string route_name; }
        const string QuarryRaceFile = "Data/tracks/viking_quarry.json";
        static QrFile QuarryRaceNow;
        static readonly Color BermStone = new Color(.47f, .45f, .42f), BermDark = new Color(.36f, .35f, .33f);

        // (planning: after the quarry jumps' pieces, so the race's benches are graded after their run-outs and catches, which they join)
        static void MergeQuarryRace(Ridge mountain)
        {
            QuarryRaceNow = null; string path = Path.Combine(ProjectRoot, QuarryRaceFile);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round90] no " + QuarryRaceFile + ": no Viking Quarry race pieces"); return; }
            var f = QuarryRaceNow = JsonUtility.FromJson<QrFile>(File.ReadAllText(path)); var d = mountain.Data;
            T[] Cat<T>(T[] a, T[] b) => (a ?? new T[0]).Concat(b ?? new T[0]).ToArray();
            d.grades = Cat(d.grades, f.grades); d.clear_lines = Cat(d.clear_lines, f.clear_lines); d.landing_zones = Cat(d.landing_zones, f.landing_zones);
            Debug.Log($"[Round90] the Viking Quarry race: {f.grades?.Length ?? 0} benches and roads, {f.clear_lines?.Length ?? 0} cleared lines merged into the mountain's");
        }

        static void BuildQuarryRace(TerrainGrid terrain, Materials mats, Transform root)
        {
            var f = QuarryRaceNow; if (f == null) return;
            var go = new GameObject("The Viking Quarry race (Round 90)").transform; go.SetParent(root, false);
            // ---- the gravel: a crushed-stone surface on each bench and road (look: the ground under it is the grade's)
            int ns = 0;
            foreach (var s in f.surfaces ?? new QrLine[0])
            {
                var pts = ToPoints(s.points); if (pts.Count < 2) continue;
                var gm = new MeshBuilder(); gm.Tint = Gravel;
                RibbonGrid(gm, pts, Enumerable.Repeat(s.half * 2f, pts.Count).ToArray(), .06f, terrain, 4);
                gm.ToObject($"Viking Quarry race gravel {ns++} {s.id}", mats.Driveway, go, false, false);
            }
            // ---- the berms
            var bt = new GameObject("Berms").transform; bt.SetParent(go, false); int nb = 0, segs = 0;
            foreach (var b in f.berms ?? new QrLine[0])
            {
                var c = ToPoints(b.points); if (c.Count < 2) continue;
                var line = new List<Vector2>();
                for (int i = 0; i < c.Count; i++)
                {
                    var t = (c[Mathf.Min(i + 1, c.Count - 1)] - c[Mathf.Max(i - 1, 0)]).normalized; var nl = new Vector2(-t.y, t.x);
                    line.Add(c[i] + nl * (b.side > 0 ? 1f : -1f) * b.lat);
                }
                segs += BermRow(terrain, mats, bt, $"Quarry berm {nb}", line, 1.3f, .8f, false); nb++;   // (Round 90 check: a car sat up on the 1.6 m wide berm: now a thin wall it slides along)
            }
            // ---- the pit dividers (race time only)
            int nd = 0;
            foreach (var dv in f.dividers ?? new QrLine[0])
            {
                var ro = new GameObject("Quarry divider " + dv.id).transform; ro.SetParent(go, false);
                var only = ro.gameObject.AddComponent<RaceOnly>(); only.reverse = false; only.showInForwardOf = string.IsNullOrEmpty(f.route_name) ? "Viking Quarry" : f.route_name;
                nd += BermRow(terrain, mats, ro, $"Quarry divider {dv.id}", ToPoints(dv.points), 1.0f, .7f, true);
            }
            Debug.Log($"[Round90] the Viking Quarry race: {ns} gravel surfaces, {nb} berm runs ({segs} pieces), {nd} divider barriers (race time only)");
        }

        // a row along a line of low pieces on the ground, each its own box collider named for the guide walls (WallGlance): rough rock (a berm) or
        // red-and-white concrete barriers (a divider); returns the number of pieces
        static int BermRow(TerrainGrid terrain, Materials mats, Transform parent, string name, List<Vector2> line, float height, float thick, bool concrete)
        {
            var mb = new MeshBuilder(); var holder = new GameObject(name + " pieces").transform; holder.SetParent(parent, false); int n = 0;
            const float Piece = 4f;
            var cum = new List<float> { 0f }; for (int i = 1; i < line.Count; i++) cum.Add(cum[i - 1] + (line[i] - line[i - 1]).magnitude);
            float L = cum[cum.Count - 1];
            Vector2 At(float s)
            {
                int i = 1; while (i < cum.Count - 1 && cum[i] < s) i++;
                float t = Mathf.InverseLerp(cum[i - 1], cum[i], s); return Vector2.Lerp(line[i - 1], line[i], t);
            }
            for (float s = 0f; s + .5f < L; s += Piece)
            {
                var a = At(s); var b = At(Mathf.Min(L, s + Piece)); var c = (a + b) * .5f; var dir = (b - a); float len = dir.magnitude; if (len < .3f) continue; dir /= len;
                float gy = Mathf.Min(Ground(terrain, a.x, a.y), Ground(terrain, b.x, b.y), Ground(terrain, c.x, c.y));
                var fr = MakeFrame(new Vector3(c.x, gy, c.y), new Vector2(-dir.y, dir.x), 1f);   // u along the row, v across it
                int h = (int)(c.x * 13.7f + c.y * 7.1f);
                if (concrete) BoxF(mb, fr, -len * .5f + .05f, len * .5f - .05f, -.3f, height, -thick * .5f, thick * .5f, n % 2 == 0 ? new Color(.85f, .85f, .82f) : new Color(.75f, .12f, .1f));
                else
                {
                    // two or three rough stones heaped along the piece (each a little tilted and of its own size), the row's height
                    for (int k = 0; k < 3; k++)
                    {
                        float u0 = -len * .5f + k * len / 3f, u1 = u0 + len / 3f + .4f, hh = height * (.75f + .5f * Hash01(h + k, 1)), w = thick * (.8f + .4f * Hash01(h + k, 2));
                        var col = Hash01(h + k, 3) < .5f ? BermStone : BermDark;
                        BoxF(mb, fr, u0, u1, -.5f, hh, -w * .5f, w * .5f, col);
                        BoxF(mb, fr, u0 + .3f, u1 - .3f, hh - .05f, hh + .25f, -w * .35f, w * .35f, Shade(col, 1.1f));
                    }
                }
                var bc = new GameObject(name).AddComponent<BoxCollider>(); bc.transform.SetParent(holder, false);
                bc.transform.SetPositionAndRotation(new Vector3(c.x, gy + (height - .3f) * .5f, c.y), Quaternion.LookRotation(new Vector3(-dir.y, 0f, dir.x)));
                bc.size = new Vector3(len, height + .3f, thick);
                n++;
            }
            var look = mb.ToObject(name + " look", mats.House, parent, false); look.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            return n;
        }
    }
}
