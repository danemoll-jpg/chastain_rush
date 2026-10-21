using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Round 56 (Dan, 2026-10-21 17:08; reports BUG-001 to BUG-004 of session 2026-10-21_17-00-50-815_f90718): Addison Heights.
//   A  the Elaine jump lands on the lower bank and runs on over the Crawford / Hayworth corner down Crawford's west leg (BUG-004), then back through the
//      yards round 1143 onto Hayworth: all data (Tools/mapgen/make_addison_route.py: its lip and way back graded, brush on the upper bank, the hedge's gap)
//   B  BUG-003: a road-closed barricade across Elaine Circle's mouth (three type III barricades 17 m along Elaine, ahead on the right from Dan's spot, breakable, no speed lost, whole again for the next car;
//      a forward race only) and tire marks on the road from it up Elaine (track "elaine_mouth"); built in the step Addison
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R56Mouth { public float[] at, dir, marks; public float width; }
        [Serializable] class R56File { public R56Mouth elaine_mouth; }

        static void BuildAddison56(TerrainGrid terrain, Materials mats, Transform root)
        {
            string path = System.IO.Path.Combine(ProjectRoot, AddisonTrackFile);
            if (!System.IO.File.Exists(path)) return;
            var m = JsonUtility.FromJson<R56File>(System.IO.File.ReadAllText(path))?.elaine_mouth;
            if (m?.at == null || m.at.Length < 2) return;
            var go = new GameObject("Elaine barricade (forward race)"); go.transform.SetParent(root, false);
            go.AddComponent<RaceOnly>().showInForwardOf = AddisonRouteName();   // at the origin: its meshes are in world coordinates
            go.AddComponent<RestoreWhenClear>().at = new Vector3(m.at[0], Ground(terrain, m.at[0], m.at[1]), m.at[1]);
            var at = new Vector2(m.at[0], m.at[1]); var d = new Vector2(m.dir[0], m.dir[1]).normalized; var across = new Vector2(d.y, -d.x);
            // three barricades side by side across the road, each its own panel: two striped boards on A-frame legs, 2.4 m long, 1.1 m tall
            var br = new Breakables(); var u3 = new Vector3(across.x, 0f, across.y); var f3 = new Vector3(d.x, 0f, d.y);
            foreach (float off in new[] { -2.65f, 0f, 2.65f })
            {
                var c2 = at + across * off; float g = Ground(terrain, c2.x, c2.y); var c = new Vector3(c2.x, g, c2.y);
                var mb = br.Begin(c2);
                foreach (float h in new[] { 0.62f, 1.0f })
                    br.Piece(() =>
                    {
                        var b0 = c - u3 * 1.2f + Vector3.up * h;
                        for (int i = 0; i < 6; i++) Beam(mb, b0 + u3 * (0.4f * i), b0 + u3 * (0.4f * (i + 1)), 0.04f, 0.22f, i % 2 == 0 ? BarrierOrange : BarrierWhite);
                    }, g);
                br.Piece(() =>
                {
                    foreach (float s in new[] { -0.9f, 0.9f })
                    {
                        var top = c + u3 * s + Vector3.up * 1.12f;
                        Beam(mb, c + u3 * s - f3 * 0.35f, top, 0.05f, 0.05f, BarrierWhite, u3);
                        Beam(mb, c + u3 * s + f3 * 0.35f, top, 0.05f, 0.05f, BarrierWhite, u3);
                    }
                }, g);
                br.End(c + Vector3.up * 0.6f, f3, 2.5f, 1.2f, 0.8f);
            }
            var chunks = br.ToObjects("Elaine barricade", mats.House, go.transform, 0.5f, BreakSounds.Kind.Wood);
            foreach (var ch in chunks.GetComponentsInChildren<FenceChunk>(true)) ch.free = true;
            // the tire marks: two pairs of dark rubber streaks on the road, from just past the barricade up Elaine along the shortcut's line (fading out)
            var marks = new MeshBuilder(); var line = ToPoints(m.marks);
            if (line.Count >= 2)
                foreach (var (side, w) in new[] { (-0.85f, 0.13f), (0.85f, 0.13f), (-0.55f, 0.11f), (1.15f, 0.11f) })
                {
                    Vector3? pa = null, pb = null; float total = 0f;
                    for (int k = 0; k < line.Count; k++)
                    {
                        if (k > 0) total += (line[k] - line[k - 1]).magnitude;
                        var t = (line[Mathf.Min(k + 1, line.Count - 1)] - line[Mathf.Max(k - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x);
                        float wobble = 0.12f * Mathf.Sin(total * 0.35f + side * 3f);
                        var q = line[k] + n * (side + wobble); var a = q + n * w; var b = q - n * w;
                        var A = new Vector3(a.x, Ground(terrain, a.x, a.y) + 0.03f, a.y); var B = new Vector3(b.x, Ground(terrain, b.x, b.y) + 0.03f, b.y);
                        float fade = Mathf.Clamp01(1f - total / 30f);
                        if (pa.HasValue) { marks.Tint = Color.Lerp(new Color(0.36f, 0.36f, 0.37f), new Color(0.07f, 0.07f, 0.08f), fade); Face(marks, pa.Value, A, B, pb.Value, Vector3.up); }
                        pa = A; pb = B;
                    }
                }
            if (marks.Count > 0) { var mg = marks.ToObject("Elaine tire marks", mats.Paint, go.transform, false, false); mg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
            Debug.Log($"[Round56] B: the barricade across Elaine's mouth at ({at.x:0.0}, {at.y:0.0}), 3 x 2.4 m; tire marks {line.Count} points");
        }

        static string AddisonRouteName()
        {
            string path = System.IO.Path.Combine(ProjectRoot, AddisonTrackFile);
            var j = JsonUtility.FromJson<R56Name>(System.IO.File.ReadAllText(path));
            return string.IsNullOrEmpty(j?.route_name) ? "Addison Heights" : j.route_name;
        }
        [Serializable] class R56Name { public string route_name; }
    }
}
