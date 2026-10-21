using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 54: the built scene's ground over rectangles, for planning shortcuts on the ground as built (house lots cut, roads graded).
    // Editor batch, no Play mode: ChastainRush.GroundGrid.Run -gridRects "name:x0,z0,x1,z1,step;..." -gridOut <dir>. Writes <dir>/<name>.csv:
    // x, z, the terrain's height (TerrainCollider only), and the highest solid surface there (any collider, its name). -gridLines <file>: along lines.
    public static class GroundGrid
    {
        public static void Run()
        {
            string outDir = Arg("-gridOut") ?? "ground_grid", spec = Arg("-gridRects") ?? "";
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            Physics.SyncTransforms();
            var ci = CultureInfo.InvariantCulture; var hits = new RaycastHit[32];
            foreach (var item in spec.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var parts = item.Split(':'); var v = System.Array.ConvertAll(parts[1].Split(','), q => float.Parse(q, ci));
                var sb = new StringBuilder("x,z,terrain,top,top_name\n");
                for (float z = v[1]; z <= v[3] + 1e-3f; z += v[4])
                    for (float x = v[0]; x <= v[2] + 1e-3f; x += v[4])
                    {
                        int n = Physics.RaycastNonAlloc(new Vector3(x, 900f, z), Vector3.down, hits, 1200f, ~0, QueryTriggerInteraction.Ignore);
                        float ter = float.NaN, top = float.NegativeInfinity; string name = "-";
                        for (int i = 0; i < n; i++)
                        {
                            var h = hits[i];
                            if (h.collider is TerrainCollider) { if (float.IsNaN(ter) || h.point.y > ter) ter = h.point.y; }
                            if (h.point.y > top) { top = h.point.y; name = h.collider.name.Replace(',', ' '); }
                        }
                        sb.Append(string.Format(ci, "{0:0.0},{1:0.0},{2:0.000},{3:0.000},{4}\n", x, z, ter, top, name));
                    }
                File.WriteAllText(Path.Combine(outDir, parts[0] + ".csv"), sb.ToString());
            }
            // -gridLines <file>: one line per polyline, "name x,z,x,z,...": the terrain's height every 0.5 m along it, to <dir>/<name>.csv (s, x, z, terrain)
            string linesFile = Arg("-gridLines");
            if (!string.IsNullOrEmpty(linesFile))
                foreach (var row in File.ReadAllLines(linesFile))
                {
                    var sp = row.IndexOf(' '); if (sp < 0) continue;
                    var v = System.Array.ConvertAll(row.Substring(sp + 1).Split(','), q => float.Parse(q, ci));
                    var sb = new StringBuilder("s,x,z,terrain\n"); float s0 = 0f;
                    for (int i = 0; i + 3 < v.Length; i += 2)
                    {
                        var a = new Vector2(v[i], v[i + 1]); var b = new Vector2(v[i + 2], v[i + 3]); float L = (b - a).magnitude; if (L < 1e-4f) continue;
                        for (float t = 0f; t < L; t += 0.5f)
                        {
                            var q = Vector2.Lerp(a, b, t / L); float ter = float.NaN;
                            int n = Physics.RaycastNonAlloc(new Vector3(q.x, 900f, q.y), Vector3.down, hits, 1200f, ~0, QueryTriggerInteraction.Ignore);
                            for (int k = 0; k < n; k++) if (hits[k].collider is TerrainCollider && (float.IsNaN(ter) || hits[k].point.y > ter)) ter = hits[k].point.y;
                            sb.Append(string.Format(ci, "{0:0.0},{1:0.00},{2:0.00},{3:0.000}\n", s0 + t, q.x, q.y, ter));
                        }
                        s0 += L;
                    }
                    File.WriteAllText(Path.Combine(outDir, row.Substring(0, sp) + ".csv"), sb.ToString());
                }
            EditorApplication.Exit(0);
        }
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
