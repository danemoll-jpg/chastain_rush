using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 28 (B3): the surface under a line, from the built scene's colliders. Editor batch, no Play mode:
    // ChastainRush.RayProfile.Run -rays "name:x0,z0,y0,x1,z1,y1,step;..." -probeOut <file>. Along each line, every 'step' m, a ray down
    // from 3 m over the line's own height (so a pipe's roof or a ledge above is not what is hit): the height hit, the step from the last
    // ray, and the collider.
    public static class RayProfile
    {
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "ray_profile.txt", spec = Arg("-rays") ?? "";
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            Physics.SyncTransforms();
            var lines = new List<string>(); var ci = CultureInfo.InvariantCulture;
            foreach (var item in spec.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var parts = item.Split(':'); var v = System.Array.ConvertAll(parts[1].Split(','), q => float.Parse(q, ci));
                var a = new Vector3(v[0], v[2], v[1]); var b = new Vector3(v[3], v[5], v[4]); float step = v[6];
                lines.Add($"{parts[0]}: from ({a.x:0.0}, {a.y:0.00}, {a.z:0.0}) to ({b.x:0.0}, {b.y:0.00}, {b.z:0.0})");
                float len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z)); float prev = float.NaN;
                for (float s = 0f; s <= len + 1e-3f; s += step)
                {
                    var p = Vector3.Lerp(a, b, s / Mathf.Max(1e-3f, len));
                    if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        string d = float.IsNaN(prev) ? "" : $" step {hit.point.y - prev:+0.000;-0.000}";
                        lines.Add($"   {s,6:0.00}  y {hit.point.y:0.000}{d}  n.y {hit.normal.y:0.00}  {hit.collider.name}");
                        prev = hit.point.y;
                    }
                    else { lines.Add($"   {s,6:0.00}  MISS"); prev = float.NaN; }
                }
            }
            File.WriteAllLines(outPath, lines);
            EditorApplication.Exit(0);
        }
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
